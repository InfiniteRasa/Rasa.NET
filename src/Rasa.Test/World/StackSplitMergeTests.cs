using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Packets;
    using Rasa.Packets.Inventory.Client;
    using Rasa.Packets.Inventory.Server;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Structures;
    using Rasa.Test.Missions;

    // The client's quantity chooser drags part of a stack, and a plain drag sends the stack's
    // count (inventorywindow._StartItemDrag, inventory.AddItemToPersonalInventory): the move
    // carries a quantity. The server never read it, and a stack dropped on a stack of the same
    // item swapped places with it. Now part of a stack dropped in an empty slot is a new stack
    // there, and a stack dropped on one it can join goes into it, as much as fits - in the pack
    // (PersonalInventory_MoveItem) and in the footlocker (HomeInventory_MoveItem).
    [TestClass]
    [DoNotParallelize]
    public class StackSplitMergeTests
    {
        private const uint Ammo = 9301, Other = 9302, ItemClass = 3147;
        private const uint FullStack = 50000;   // MissionTestContext.AddRewardTemplate's class
        private const int Tab = 50;             // category 2, where the reward templates go

        #region The pack

        [TestMethod]
        public void PartOfAStackDroppedInAnEmptySlotIsANewStackThere()
        {
            using var f = new Fixture();
            var ammo = f.InPack(Ammo, 10, Tab);
            f.Sent();

            f.MovePack(Tab, Tab + 1, 3);

            Assert.AreEqual(7u, ammo.StackSize);
            Assert.AreEqual(ammo.EntityId, f.Pack[Tab]);
            var split = f.Loaded(Tab + 1);
            Assert.IsNotNull(split);
            Assert.AreNotEqual(ammo.Id, split.Id);
            Assert.AreEqual(Ammo, split.ItemTemplateId);
            Assert.AreEqual(3u, split.StackSize);

            f.AssertPackRow(ammo, Tab, 7);
            f.AssertPackRow(split, Tab + 1, 3);

            var sent = f.SentMessages();
            Assert.AreEqual(7u, sent.Where(m => m.EntityId == ammo.EntityId).Select(m => m.Packet).OfType<SetStackCountPacket>().Single().StackSize);
            Assert.IsTrue(sent.Select(m => m.Packet).OfType<InventoryAddItemPacket>().Any(p => p.EntityId == split.EntityId && p.SlotId == Tab + 1));
            Assert.IsFalse(sent.Select(m => m.Packet).OfType<InventoryRemoveItemPacket>().Any(), "the stack it came from stays where it is");
        }

        [TestMethod]
        public void AStackDroppedOnOneOfTheSameItemGoesIntoIt()
        {
            using var f = new Fixture();
            var ammo = f.InPack(Ammo, 10, Tab);
            var stack = f.InPack(Ammo, 5, Tab + 1);
            f.Sent();

            // A plain drag: the client sends the stack's count.
            f.MovePack(Tab, Tab + 1, 10);

            Assert.AreEqual(15u, stack.StackSize);
            Assert.AreEqual(stack.EntityId, f.Pack[Tab + 1]);
            Assert.AreEqual(0UL, f.Pack[Tab]);
            Assert.IsNull(EntityManager.Instance.GetItem(ammo.EntityId), "joined away");
            f.AssertPackRow(stack, Tab + 1, 15);
            f.AssertGone(ammo);

            var sent = f.SentMessages();
            Assert.AreEqual(15u, sent.Where(m => m.EntityId == stack.EntityId).Select(m => m.Packet).OfType<SetStackCountPacket>().Single().StackSize);
            Assert.AreEqual(ammo.EntityId, sent.Select(m => m.Packet).OfType<InventoryRemoveItemPacket>().Single().EntityId);
            Assert.AreEqual(ammo.EntityId, sent.Select(m => m.Packet).OfType<DestroyPhysicalEntityPacket>().Single().EntityId);
        }

        [TestMethod]
        public void WhatTheStackCannotTakeStaysWhereItWas()
        {
            using var f = new Fixture();
            var ammo = f.InPack(Ammo, 10, Tab);
            var stack = f.InPack(Ammo, FullStack - 4, Tab + 1);

            f.MovePack(Tab, Tab + 1, 10);

            Assert.AreEqual(FullStack, stack.StackSize);
            Assert.AreEqual(6u, ammo.StackSize);
            Assert.AreEqual(ammo.EntityId, f.Pack[Tab]);
            f.AssertPackRow(stack, Tab + 1, FullStack);
            f.AssertPackRow(ammo, Tab, 6);
        }

        [TestMethod]
        public void PartOfAStackOnAnotherItemIsRefused()
        {
            using var f = new Fixture();
            var ammo = f.InPack(Ammo, 10, Tab);
            var other = f.InPack(Other, 1, Tab + 1);
            f.Sent();

            f.MovePack(Tab, Tab + 1, 3);

            Assert.AreEqual(10u, ammo.StackSize);
            Assert.AreEqual(ammo.EntityId, f.Pack[Tab]);
            Assert.AreEqual(other.EntityId, f.Pack[Tab + 1]);
            f.AssertPackRow(ammo, Tab, 10);
            f.AssertPackRow(other, Tab + 1, 1);
            Assert.AreEqual(0, f.Sent().Count);
        }

        [TestMethod]
        public void AWholeStackOnAnotherItemStillSwaps()
        {
            using var f = new Fixture();
            var ammo = f.InPack(Ammo, 10, Tab);
            var other = f.InPack(Other, 1, Tab + 1);

            f.MovePack(Tab, Tab + 1, 10);

            Assert.AreEqual(ammo.EntityId, f.Pack[Tab + 1]);
            Assert.AreEqual(other.EntityId, f.Pack[Tab]);
            f.AssertPackRow(ammo, Tab + 1, 10);
            f.AssertPackRow(other, Tab, 1);
        }

        [TestMethod]
        public void ASplitCarriesWhatTheStackCarries()
        {
            using var f = new Fixture();
            var ammo = f.InPack(Ammo, 10, Tab, crafter: "Ellimist", boundTo: f.Client.Player.Id);

            f.MovePack(Tab, Tab + 1, 4);

            var split = f.Loaded(Tab + 1);
            Assert.AreNotEqual(ammo.Id, split.Id, "a new stack");
            Assert.AreEqual(6u, ammo.StackSize);
            Assert.AreEqual("Ellimist", split.Crafter);
            Assert.AreEqual(f.Client.Player.Id, split.BoundCharacterId, "a bound stack is not split into an unbound one");
            Assert.AreEqual(ammo.Color, split.Color);

            using var unit = f.Context.CreateChar();
            var saved = unit.Items.GetItem(split.Id);
            Assert.AreEqual("Ellimist", saved.CrafterName);
            Assert.AreEqual(f.Client.Player.Id, saved.BoundCharacterId);
        }

        [TestMethod]
        public void StacksThatDifferInWhatTheyCarryDoNotJoin()
        {
            using var f = new Fixture();
            var bound = f.InPack(Ammo, 10, Tab, boundTo: f.Client.Player.Id);
            var loose = f.InPack(Ammo, 5, Tab + 1);

            f.MovePack(Tab, Tab + 1, 10);

            Assert.AreEqual(10u, bound.StackSize);
            Assert.AreEqual(5u, loose.StackSize);
            Assert.AreEqual(bound.EntityId, f.Pack[Tab + 1], "swapped, not joined");
            Assert.AreEqual(loose.EntityId, f.Pack[Tab]);
        }

        [TestMethod]
        public void ANewStackFromASplitIsThereAfterALogin()
        {
            using var f = new Fixture();
            f.InPack(Ammo, 10, Tab);

            f.MovePack(Tab, Tab + 1, 3);
            f.Login();

            Assert.AreEqual(7u, f.Loaded(Tab).StackSize);
            Assert.AreEqual(3u, f.Loaded(Tab + 1).StackSize);
        }

        #endregion

        #region The footlocker

        [TestMethod]
        public void PartOfAFootlockerStackDroppedInAnEmptySlotIsANewStackThere()
        {
            using var f = new Fixture();
            var ammo = f.InHome(Ammo, 10, 0, crafter: "Ellimist");
            f.Sent();

            f.MoveHome(0, 1, 4);

            Assert.AreEqual(6u, ammo.StackSize);
            Assert.AreEqual(ammo.EntityId, f.Home[0]);
            var split = EntityManager.Instance.GetItem(f.Home[1]);
            Assert.IsNotNull(split);
            Assert.AreEqual(4u, split.StackSize);
            Assert.AreEqual("Ellimist", split.Crafter);
            f.AssertHomeRow(ammo, 0, 6);
            f.AssertHomeRow(split, 1, 4);

            var sent = f.SentMessages();
            Assert.AreEqual(6u, sent.Where(m => m.EntityId == ammo.EntityId).Select(m => m.Packet).OfType<SetStackCountPacket>().Single().StackSize);
            Assert.IsTrue(sent.Select(m => m.Packet).OfType<InventoryAddItemPacket>().Any(p => p.Type == InventoryType.HomeInventory && p.EntityId == split.EntityId && p.SlotId == 1));
        }

        [TestMethod]
        public void AFootlockerStackDroppedOnOneOfTheSameItemGoesIntoIt()
        {
            using var f = new Fixture();
            var ammo = f.InHome(Ammo, 10, 0);
            var stack = f.InHome(Ammo, 5, 1);
            f.Sent();

            f.MoveHome(0, 1, 10);

            Assert.AreEqual(15u, stack.StackSize);
            Assert.AreEqual(0UL, f.Home[0]);
            Assert.AreEqual(stack.EntityId, f.Home[1]);
            Assert.IsNull(EntityManager.Instance.GetItem(ammo.EntityId));
            f.AssertHomeRow(stack, 1, 15);
            f.AssertGone(ammo);

            var sent = f.SentMessages();
            Assert.AreEqual(ammo.EntityId, sent.Select(m => m.Packet).OfType<InventoryRemoveItemPacket>().Single().EntityId);
            Assert.AreEqual(15u, sent.Where(m => m.EntityId == stack.EntityId).Select(m => m.Packet).OfType<SetStackCountPacket>().Single().StackSize);
        }

        [TestMethod]
        public void PartOfAFootlockerStackJoinsAnotherAndTheRestStays()
        {
            using var f = new Fixture();
            var ammo = f.InHome(Ammo, 10, 0);
            var stack = f.InHome(Ammo, FullStack - 2, 1);

            f.MoveHome(0, 1, 5);

            Assert.AreEqual(FullStack, stack.StackSize);
            Assert.AreEqual(8u, ammo.StackSize);
            f.AssertHomeRow(stack, 1, FullStack);
            f.AssertHomeRow(ammo, 0, 8);
        }

        [TestMethod]
        public void AWholeFootlockerStackOnAnotherItemStillSwaps()
        {
            using var f = new Fixture();
            var ammo = f.InHome(Ammo, 10, 0);
            var other = f.InHome(Other, 1, 1);

            f.MoveHome(0, 1, 10);

            Assert.AreEqual(ammo.EntityId, f.Home[1]);
            Assert.AreEqual(other.EntityId, f.Home[0]);

            // And part of one on it is refused.
            f.MoveHome(1, 0, 3);
            Assert.AreEqual(10u, ammo.StackSize);
            Assert.AreEqual(ammo.EntityId, f.Home[1]);
        }

        #endregion

        #region Fixture

        private sealed class Fixture : IDisposable
        {
            internal MissionTestContext Context { get; } = MissionTestContext.WithCompletableMission(429);
            internal InventoryManager Inventory { get; }
            internal Client Client => Context.Client;
            internal List<ulong> Pack => Client.Player.Inventory.PersonalInventory;
            internal List<ulong> Home => Client.Player.Inventory.HomeInventory;

            internal Fixture()
            {
                Inventory = new InventoryManager(Context, Context.Manager);
                Client.Player.Inventory.HomeInventory = Enumerable.Repeat(0UL, LockboxTab.TotalSlots).ToList();
                Client.Player.LockboxTabs = 1;
            }

            internal Item InPack(uint templateId, uint quantity, int slot, string crafter = "", uint boundTo = 0)
            {
                var item = Make(templateId, quantity, crafter, boundTo);

                item.OwnerId = Client.Player.Id;
                item.OwnerSlotId = (uint)slot;
                Pack[slot] = item.EntityId;

                using var unit = Context.CreateChar();
                unit.CharacterInventories.AddInvItem(Client.AccountEntry.Id, Client.Player.Id, (uint)InventoryType.Personal, (uint)slot, item.Id);

                return item;
            }

            internal Item InHome(uint templateId, uint quantity, int slot, string crafter = "")
            {
                var item = Make(templateId, quantity, crafter, 0);

                item.OwnerId = 0;
                item.OwnerSlotId = (uint)slot;
                Home[slot] = item.EntityId;

                using var unit = Context.CreateChar();
                unit.CharacterInventories.AddInvItem(Client.AccountEntry.Id, 0, (uint)InventoryType.HomeInventory, (uint)slot, item.Id);

                return item;
            }

            internal void MovePack(int from, int to, int quantity) =>
                Inventory.PersonalInventory_MoveItem(Client, new PersonalInventory_MoveItemPacket { SrcSlot = from, DestSlot = to, Quantity = quantity });

            internal void MoveHome(int from, int to, int quantity) =>
                Inventory.HomeInventory_MoveItem(Client, new HomeInventory_MoveItemPacket { SrcSlot = (uint)from, DestSlot = (uint)to, Quantity = quantity });

            /// <summary>The lists built again from the rows, as at a login.</summary>
            internal void Login()
            {
                Sent();
                Inventory.InitCharacterInventory(Client);
            }

            internal Item Loaded(int slot) => EntityManager.Instance.GetItem(Pack[slot]);

            internal List<CallMethodMessage> SentMessages() => WorldTestContext.Drain(Client)
                .Select(packet => packet.Message).OfType<CallMethodMessage>().ToList();

            internal List<PythonPacket> Sent() => SentMessages().Select(message => message.Packet).ToList();

            internal void AssertPackRow(Item item, int slot, uint quantity)
            {
                using var unit = Context.CreateChar();
                var row = unit.CharacterInventories.FindByItemId(item.Id);

                Assert.IsNotNull(row, $"item {item.Id}'s row");
                Assert.AreEqual((uint)InventoryType.Personal, row.InventoryType);
                Assert.AreEqual(Client.Player.Id, row.CharacterId);
                Assert.AreEqual((uint)slot, row.SlotId);
                Assert.AreEqual(quantity, unit.Items.GetItem(item.Id).StackSize);
            }

            internal void AssertHomeRow(Item item, int slot, uint quantity)
            {
                using var unit = Context.CreateChar();
                var row = unit.CharacterInventories.FindByItemId(item.Id);

                Assert.IsNotNull(row, $"item {item.Id}'s row");
                Assert.AreEqual((uint)InventoryType.HomeInventory, row.InventoryType);
                Assert.AreEqual(0u, row.CharacterId);
                Assert.AreEqual((uint)slot, row.SlotId);
                Assert.AreEqual(quantity, unit.Items.GetItem(item.Id).StackSize);
            }

            internal void AssertGone(Item item)
            {
                using var unit = Context.CreateChar();

                Assert.IsNull(unit.Items.GetItem(item.Id), "the items row");
                Assert.IsNull(unit.CharacterInventories.FindByItemId(item.Id), "the inventory row");
            }

            private Item Make(uint templateId, uint quantity, string crafter, uint boundTo)
            {
                Context.AddRewardTemplate(templateId, ItemClass);

                var template = EntityClassManager.Instance.LoadedEntityClasses[(EntityClasses)ItemClass].ItemTemplates[templateId];
                var item = ItemManager.StageItem(template, quantity, crafter);

                item.BoundCharacterId = boundTo;

                using (var unit = Context.CreateChar())
                    item.Id = unit.Items.CreateItem(item);

                EntityManager.Instance.RegisterEntity(item.EntityId, EntityType.Item);
                EntityManager.Instance.RegisterItem(item.EntityId, item);

                return item;
            }

            public void Dispose() => Context.Dispose();
        }

        #endregion
    }
}
