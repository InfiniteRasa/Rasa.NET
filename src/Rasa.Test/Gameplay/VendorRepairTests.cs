using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Gameplay
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Packets;
    using Rasa.Packets.MapChannel.Client;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Structures;
    using Rasa.Test.Missions;
    using Rasa.Test.World;

    /// <summary>
    /// A vendor repair as the client sees it. The vendor window's Repair page is rebuilt on
    /// UI_UPDATE_ITEM_REPAIRED, which only Recv_ItemStatus posts, and only when the hit points it
    /// carries are above the ones the item had (augmentations/item.py). The drawer's broken icon
    /// is cleared the same way, when the old hit points were 0.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class VendorRepairTests
    {
        // Motor Assist Armor Vests, levels 1-2 and 3-7.
        private const uint VestTemplate = 13186;
        private const uint OtherVestTemplate = 11565;

        /// <summary>What Recv_ItemInfo and Recv_ItemStatus do with an item's hit points.</summary>
        private sealed class ClientItem
        {
            public int HitPoints;
            public bool RepairedPosted;
            public bool BrokenStatusPosted;

            public void Receive(PythonPacket packet)
            {
                switch (packet)
                {
                    case ItemInfoPacket info:
                        HitPoints = info.Item.CurrentHitPoints;
                        break;

                    case ItemStatusPacket status:
                        var old = HitPoints;
                        HitPoints = status.CurrentHitPoints;

                        if ((old <= 0 && HitPoints > 0) || (old > 0 && HitPoints <= 0))
                            BrokenStatusPosted = true;

                        if (old < HitPoints && HitPoints == status.MaxHitPoints)
                            RepairedPosted = true;
                        break;
                }
            }
        }

        private static (NpcManager Npcs, ulong VendorId) Vendor(BootcampRuntimeTestHarness.Harness harness)
        {
            var npc = harness.AddNpc(BootcampRuntimeTestHarness.CorporalHartmannCreatureId,
                position: harness.Client.Player.Position + new System.Numerics.Vector3(0, 0, 2));
            npc.Npc ??= new Npc();
            npc.Npc.Vendor = new Vendor(0) { ItemPrice = 1, VendorItems = { VestTemplate } };

            var npcs = new NpcManager(harness.Context, harness.Manager);
            npcs.RequestNPCVending(harness.Client, new RequestNPCVendingPacket { EntityId = npc.EntityId });

            return (npcs, npc.EntityId);
        }

        private static Dictionary<ulong, ClientItem> Play(Client client, params (Item Item, int HitPoints)[] items)
        {
            var seen = items.ToDictionary(i => i.Item.EntityId, i => new ClientItem { HitPoints = i.HitPoints });

            foreach (var message in WorldTestContext.Drain(client).Select(p => p.Message).OfType<CallMethodMessage>())
                if (seen.TryGetValue(message.EntityId, out var item))
                    item.Receive(message.Packet);

            return seen;
        }

        [TestMethod]
        [DataRow(0, DisplayName = "broken")]
        [DataRow(40, DisplayName = "worn")]
        public void ARepairedItemLeavesTheRepairListAtOnce(int wornHitPoints)
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var vest = ToyTests.Grant(harness, VestTemplate);
            var max = Durability.MaxHitPointsOf(vest);
            Assert.IsGreaterThan(wornHitPoints, max);
            vest.CurrentHitPoints = wornHitPoints;
            Assert.IsTrue(ManifestationManager.Instance.GainCredits(harness.Client, 1000));
            var (npcs, vendorId) = Vendor(harness);
            var cost = Durability.RepairCost(vest);
            var credits = harness.Client.Player.Credits[CurencyType.Credits];
            harness.Drain();

            npcs.RequestVendorRepair(harness.Client, new RequestVendorRepairPacket
            {
                VendorEntityId = vendorId,
                ItemEntitesId = { vest.EntityId }
            });

            Assert.AreEqual(max, vest.CurrentHitPoints);
            Assert.AreEqual(credits - cost, harness.Client.Player.Credits[CurencyType.Credits]);

            var seen = Play(harness.Client, (vest, wornHitPoints))[vest.EntityId];
            Assert.AreEqual(max, seen.HitPoints);
            Assert.IsTrue(seen.RepairedPosted, "UI_UPDATE_ITEM_REPAIRED is what rebuilds the vendor's repair list");
            Assert.AreEqual(wornHitPoints == 0, seen.BrokenStatusPosted, "a broken item's icon is cleared, and only a broken item's");

            using var unit = harness.Context.CreateChar();
            Assert.AreEqual(max, unit.Items.GetItem(vest.Id).CurrentHitPoints);
        }

        [TestMethod]
        public void RepairAllTakesEveryItemOffTheList()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var first = ToyTests.Grant(harness, VestTemplate);
            var second = ToyTests.Grant(harness, OtherVestTemplate);
            first.CurrentHitPoints = 10;
            second.CurrentHitPoints = 0;
            Assert.IsTrue(ManifestationManager.Instance.GainCredits(harness.Client, 1000));
            var (npcs, vendorId) = Vendor(harness);
            harness.Drain();

            npcs.RequestVendorRepair(harness.Client, new RequestVendorRepairPacket
            {
                VendorEntityId = vendorId,
                ItemEntitesId = { first.EntityId, second.EntityId }
            });

            var seen = Play(harness.Client, (first, 10), (second, 0));
            Assert.IsTrue(seen[first.EntityId].RepairedPosted);
            Assert.IsTrue(seen[second.EntityId].RepairedPosted);
            Assert.IsFalse(seen[first.EntityId].BrokenStatusPosted);
            Assert.IsTrue(seen[second.EntityId].BrokenStatusPosted);
        }

        [TestMethod]
        public void AnItemAtFullIsNotRepairedOrCharged()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var vest = ToyTests.Grant(harness, VestTemplate);
            Assert.IsTrue(ManifestationManager.Instance.GainCredits(harness.Client, 1000));
            var (npcs, vendorId) = Vendor(harness);
            var credits = harness.Client.Player.Credits[CurencyType.Credits];
            harness.Drain();

            npcs.RequestVendorRepair(harness.Client, new RequestVendorRepairPacket
            {
                VendorEntityId = vendorId,
                ItemEntitesId = { vest.EntityId }
            });

            Assert.AreEqual(credits, harness.Client.Player.Credits[CurencyType.Credits]);
            Assert.IsFalse(harness.Drain().OfType<ItemStatusPacket>().Any());
        }
    }
}
