using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Packets.Communicator.Server;
    using Rasa.Packets.MapChannel.Client;
    using Rasa.Structures;

    // A vendor of one of the client's prestige packages (vendordata.vendorpackages, credit type
    // PRESTIGE) is paid in prestige, as its window shows its prices; every other vendor in
    // credits (VendorPackages).
    [TestClass]
    [DoNotParallelize]
    public class PrestigeVendorTests
    {
        private const uint SnowballTemplate = 131481;
        private const uint PrestigeSupplyPackage = 139;     // "Prestige Supply Vendor", "Prestige Vendor: ..."

        [TestMethod]
        public void TheClientsPrestigePackagesArePaidInPrestigeAndTheRestInCredits()
        {
            Assert.AreEqual(13, VendorPackages.Prestige.Count);

            foreach (var package in new uint[] { 138, 139, 144, 151, 158, 160, 10000001 })
                Assert.AreEqual(CurencyType.Prestige, VendorPackages.CurrencyOf(package));

            // Weapons, armour, a voucher counter, the control points' token counters, none at all.
            foreach (var package in new uint[] { 10, 12, 106, 136, 137, 0 })
                Assert.AreEqual(CurencyType.Credits, VendorPackages.CurrencyOf(package));
        }

        [TestMethod]
        public void APrestigeVendorTakesPrestigeAndLeavesTheCredits()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var (npcs, vendor) = Counter(harness, PrestigeSupplyPackage);
            Fund(harness, credits: 1000, prestige: 100);
            var credits = harness.Client.Player.Credits[CurencyType.Credits];

            Buy(harness, npcs, vendor, 5);

            Assert.AreEqual(85, harness.Client.Player.Credits[CurencyType.Prestige], "five at 3");
            Assert.AreEqual(credits, harness.Client.Player.Credits[CurencyType.Credits]);
            Assert.AreEqual(5U, Snowballs(harness));

            using var unit = harness.Context.CreateChar();
            Assert.AreEqual(85, unit.Characters.Find(harness.Client.Player.Id).Prestige, "and kept");
        }

        [TestMethod]
        public void TooLittlePrestigeIsInsufficientFundsWhateverTheCredits()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var (npcs, vendor) = Counter(harness, PrestigeSupplyPackage);
            Fund(harness, credits: 100000, prestige: 10);
            harness.Drain();

            Buy(harness, npcs, vendor, 5);

            Assert.AreEqual(10, harness.Client.Player.Credits[CurencyType.Prestige]);
            Assert.AreEqual(0U, Snowballs(harness));
            Assert.IsTrue(harness.Drain().OfType<DisplayClientMessagePacket>().Any(m => m.MsgId == PlayerMessage.PmInsufficientFunds));
        }

        [TestMethod]
        public void AnyOtherVendorStillTakesCredits()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var (npcs, vendor) = Counter(harness, 0);
            Fund(harness, credits: 1000, prestige: 100);
            var credits = harness.Client.Player.Credits[CurencyType.Credits];

            Buy(harness, npcs, vendor, 5);

            Assert.AreEqual(credits - 15, harness.Client.Player.Credits[CurencyType.Credits]);
            Assert.AreEqual(100, harness.Client.Player.Credits[CurencyType.Prestige]);
        }

        [TestMethod]
        public void PrestigeIsTakenAndGivenBackAsCreditsAre()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var client = harness.Client;
            Fund(harness, credits: 0, prestige: 50);

            Assert.IsFalse(ManifestationManager.Instance.LossCurrency(client, CurencyType.Prestige, 51), "not more than they have");
            Assert.IsFalse(ManifestationManager.Instance.LossCurrency(client, CurencyType.Prestige, -1));
            Assert.AreEqual(50, client.Player.Credits[CurencyType.Prestige]);

            Assert.IsTrue(ManifestationManager.Instance.LossCurrency(client, CurencyType.Prestige, 20));
            Assert.IsTrue(ManifestationManager.Instance.RefundCurrency(client, CurencyType.Prestige, 5));
            Assert.AreEqual(35, client.Player.Credits[CurencyType.Prestige]);
        }

        private static (NpcManager Npcs, Creature Vendor) Counter(BootcampRuntimeTestHarness.Harness harness, uint package)
        {
            ToyTests.LoadTemplate(harness, SnowballTemplate);
            var npc = harness.AddNpc(BootcampRuntimeTestHarness.CorporalHartmannCreatureId,
                position: harness.Client.Player.Position + new System.Numerics.Vector3(0, 0, 2));
            npc.Npc ??= new Npc();
            npc.Npc.Vendor = new Vendor(package) { ItemPrice = 3, VendorItems = { SnowballTemplate } };

            var npcs = new NpcManager(harness.Context, harness.Manager);
            npcs.RequestNPCVending(harness.Client, new RequestNPCVendingPacket { EntityId = npc.EntityId });

            return (npcs, npc);
        }

        /// <summary>Credits added, and prestige set to exactly this much.</summary>
        private static void Fund(BootcampRuntimeTestHarness.Harness harness, int credits, int prestige)
        {
            if (credits > 0)
                Assert.IsTrue(ManifestationManager.Instance.GainCredits(harness.Client, credits));

            var held = harness.Client.Player.Credits[CurencyType.Prestige];

            if (held > prestige)
                Assert.IsTrue(ManifestationManager.Instance.LossCurrency(harness.Client, CurencyType.Prestige, held - prestige));
            else if (held < prestige)
                Assert.IsTrue(ManifestationManager.Instance.RefundCurrency(harness.Client, CurencyType.Prestige, prestige - held));

            Assert.AreEqual(prestige, harness.Client.Player.Credits[CurencyType.Prestige]);
        }

        private static void Buy(BootcampRuntimeTestHarness.Harness harness, NpcManager npcs, Creature vendor, uint quantity)
        {
            npcs.RequestVendorPurchase(harness.Client, new RequestVendorPurchasePacket
            {
                VendorEntityId = vendor.EntityId,
                ItemEntityId = EntityManager.Instance.VendorItems[vendor.EntityId].Single(),
                Quantity = quantity
            });
        }

        private static uint Snowballs(BootcampRuntimeTestHarness.Harness harness) =>
            (uint)harness.Client.Player.Inventory.PersonalInventory
                .Select(id => EntityManager.Instance.GetItem(id))
                .Where(item => item?.ItemTemplate?.ItemTemplateId == SnowballTemplate)
                .Sum(item => item.StackSize);
    }
}
