extern alias RasaGame;

using System;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Managers;
using Rasa.Packets.Game.Client;
using Rasa.Packets.Game.Server;
using Rasa.Packets.Inventory.Server;
using Rasa.Packets.MapChannel.Client;
using Rasa.Packets.MapChannel.Server;
using Rasa.Packets.Protocol;
using Rasa.Structures;
using Rasa.Test.Missions;
using Rasa.Test.World;

namespace Rasa.Test.Gameplay
{
    [TestClass]
    [DoNotParallelize]
    public class BackpackRelogTests
    {
        private object _characters;
        private Rasa.Repositories.UnitOfWork.IGameUnitOfWorkFactory _factory;
        private static readonly FieldInfo Characters = typeof(CharacterManager).GetField("_instance", BindingFlags.NonPublic | BindingFlags.Static);

        [TestInitialize]
        public void SaveManagers()
        {
            _characters = Characters.GetValue(null);
            _factory = Server.GameUnitOfWorkFactory;
        }
        [TestCleanup]
        public void RestoreManagers()
        {
            Characters.SetValue(null, _characters);
            Server.GameUnitOfWorkFactory = _factory;
        }

        [TestMethod]
        public void VendorPurchaseSurvivesRepeatedLogoutToSelectionOnTheSameConnection()
        {
            using var h = BootcampRuntimeTestHarness.CreateFromPendingSelection();
            Server.GameUnitOfWorkFactory = h.Context;
            Characters.SetValue(null, new CharacterManager(h.Context, h.Manager));
            var client = h.Client;
            var npc = h.AddNpc(BootcampRuntimeTestHarness.CorporalHartmannCreatureId,
                position: client.Player.Position + new System.Numerics.Vector3(0, 0, 2));
            npc.Npc ??= new Npc();
            npc.Npc.Vendor = new Vendor(0) { ItemPrice = 1, VendorItems = { 44917 } };
            Assert.IsTrue(ManifestationManager.Instance.GainCredits(client, 1000));
            var npcs = new NpcManager(h.Context, h.Manager);
            npcs.RequestNPCVending(client, new RequestNPCVendingPacket { EntityId = npc.EntityId });
            npcs.RequestVendorPurchase(client, new RequestVendorPurchasePacket
            {
                VendorEntityId = npc.EntityId,
                ItemEntityId = EntityManager.Instance.VendorItems[npc.EntityId].Single(),
                Quantity = 2
            });
            var item = client.Player.Inventory.PersonalInventory.Where(id => id != 0).Select(EntityManager.Instance.GetItem)
                .Single(entry => entry.ItemTemplateId == 44917);
            var itemId = item.Id;
            var slot = item.OwnerSlotId;
            var characterId = client.Player.Id;
            var credits = client.Player.Credits[CurencyType.Credits];
            Assert.AreEqual(2U, item.StackSize);
            var rows = Rows(h);
            for (var attempt = 0; attempt < 2; attempt++)
            {
                h.Maps.RequestLogout(client);
                client.Player.LogoutRequestedTick = Environment.TickCount64 - MapChannelManager.LogoutDelayMs;
                h.Maps.CharacterLogout(client);
                Assert.IsTrue(client.Player.RemoveFromMap);
                h.Maps.RemovePlayer(client, true);
                Assert.AreEqual(RasaGame::Rasa.Data.ClientState.CharacterSelection, client.State);
                CollectionAssert.AreEqual(rows, Rows(h), "Logout releases runtime entities, not saved inventory rows.");
                h.Drain();
                new CharacterManager(h.Context, h.Manager).RequestSwitchToCharacterInSlot(client,
                    new RequestSwitchToCharacterInSlotPacket { SlotNum = client.AccountEntry.SelectedSlot });
                h.RouteMapLoaded();
                Assert.AreEqual(characterId, client.Player.Id);
                Assert.AreEqual(credits, client.Player.Credits[CurencyType.Credits]);
                var loaded = client.Player.Inventory.PersonalInventory.Where(id => id != 0).Select(EntityManager.Instance.GetItem)
                    .Single(entry => entry.Id == itemId);
                Assert.AreEqual(2U, loaded.StackSize);
                Assert.AreEqual(slot, loaded.OwnerSlotId);
                Assert.AreEqual(characterId, loaded.OwnerId);
                CollectionAssert.AreEqual(rows, Rows(h));
                AssertSnapshots(h, loaded.EntityId);
            }
        }

        [TestMethod]
        public void AssignPlayerPublishesAnEmptyBackpackToClearThePreviousCharactersSlots()
        {
            using var h = BootcampRuntimeTestHarness.CreateFromPendingSelection();
            Assert.IsTrue(h.Client.Player.Inventory.PersonalInventory.All(id => id == 0));
            h.Drain();
            ManifestationManager.Instance.AssignPlayer(h.Client);
            AssertSnapshots(h, 0);
        }

        static void AssertSnapshots(BootcampRuntimeTestHarness.Harness h, ulong bought)
        {
            var packets = WorldTestContext.Drain(h.Client).Select(packet => packet.Message).OfType<CallMethodMessage>()
                .Select(message => message.Packet).ToList();
            var controlled = packets.FindIndex(packet => packet is SetControlledActorIdPacket);
            Assert.IsTrue(controlled >= 0);
            var pack = packets.FindIndex(packet => packet is InventoryCreatePacket snapshot && snapshot.InventoryType == InventoryType.Personal);
            Assert.IsTrue(pack > controlled, "The backpack needs a complete snapshot after the controlled actor initializes its UI.");
            var snapshot = (InventoryCreatePacket)packets[pack];
            Assert.AreEqual(250, snapshot.InventorySize);
            CollectionAssert.AreEqual(h.Client.Player.Inventory.PersonalInventory, snapshot.ListOfItems);
            if (bought != 0)
            {
                Assert.IsTrue(snapshot.ListOfItems.Contains(bought));
                Assert.IsTrue(packets.Take(pack).OfType<CreatePhysicalEntityPacket>().Any(entity => entity.EntityId == bought),
                    "Every saved item must exist on the client before the backpack snapshot refers to it.");
                Assert.AreEqual(1, packets.OfType<CreatePhysicalEntityPacket>().Count(entity => entity.EntityId == bought),
                    "The final snapshot must use the loaded item rather than loading or registering it twice.");
            }
            else Assert.IsTrue(snapshot.ListOfItems.All(id => id == 0), "An empty snapshot clears stale slots rather than leaving the previous character's inventory.");
        }
        static string[] Rows(BootcampRuntimeTestHarness.Harness h)
        {
            using var unit = h.Context.CreateChar();
            return unit.CharacterInventories.GetItems(h.Client.AccountEntry.Id)
                .OrderBy(row => row.ItemId).Select(row => $"{row.ItemId}:{row.CharacterId}:{row.InventoryType}:{row.SlotId}").ToArray();
        }
    }
}
