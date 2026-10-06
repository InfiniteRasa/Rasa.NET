using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Packets.Communicator.Server;
    using Rasa.Packets.Inventory.Client;
    using Rasa.Packets.Inventory.Server;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Structures;
    using Rasa.Structures.Char;
    using Rasa.Test.Missions;

    // A pack item put in the clan lockbox with no slot named: a right-click on it with the
    // lockbox open (inventorywindow.OnIconRightClicked), or a drop on one of the lockbox's tab
    // buttons (clanlockboxwindow.OnDNDDropTab). inventory.AddItemToClanInventoryTab sends
    // "ClanLockbox_DepositItemInTab", (srcSlot, the tab the window shows, quantity) without
    // looking for room in the tab, so the server is asked this for a tab that is full. The item
    // goes into stacks of the same item anywhere in the lockbox, and what is left into a free
    // slot of the tab.
    [TestClass]
    [DoNotParallelize]
    public class ClanLockboxDepositTests
    {
        private const uint Ammo = 9201, Filler = 9202, ItemClass = 3147;

        // What MissionTestContext.AddRewardTemplate gives the class, and where category 2 starts.
        private const uint FullStack = 50000;
        private const int PackSlot = 50;

        [TestMethod]
        [DataRow(false, DisplayName = "the tab the window shows")]
        [DataRow(true, DisplayName = "no tab named")]
        public void AnItemThatAFullTabCannotTakeStaysInThePack(bool noTabNamed)
        {
            using var fixture = new Fixture();
            var ammo = fixture.InPack(Ammo, 5, PackSlot);

            fixture.FillTab(1);
            fixture.Sent();
            fixture.Deposit(PackSlot, 1, noTabNamed);

            Assert.AreEqual(ammo.EntityId, fixture.Pack[PackSlot]);
            Assert.AreEqual((uint)PackSlot, ammo.OwnerSlotId);
            Assert.AreEqual(5u, ammo.StackSize);
            Assert.IsFalse(fixture.Lockbox.Contains(ammo.EntityId));
            fixture.AssertPackRow(ammo, PackSlot, 5);

            // Told why, and nothing else: the item has not moved on the client either.
            Assert.AreEqual(PlayerMessage.PmInventoryFull, ((DisplayClientMessagePacket)fixture.Sent().Single()).MsgId);
            fixture.AssertThePackStillWorks(ammo);
        }

        [TestMethod]
        public void ATabThatIsFullIsNotPassedOverForOneWithRoom()
        {
            // Two tabs bought: the first is full, and the deposit names it.
            using var fixture = new Fixture(tabs: 2);
            var ammo = fixture.InPack(Ammo, 5, PackSlot);

            fixture.FillTab(1);
            fixture.Deposit(PackSlot, 1);

            Assert.AreEqual(ammo.EntityId, fixture.Pack[PackSlot]);
            Assert.IsFalse(fixture.Lockbox.Contains(ammo.EntityId));
            fixture.AssertPackRow(ammo, PackSlot, 5);

            // Named, the second takes it.
            fixture.Deposit(PackSlot, 2);

            Assert.AreEqual(0UL, fixture.Pack[PackSlot]);
            Assert.AreEqual(ammo.EntityId, fixture.Lockbox[100]);
            fixture.AssertLockboxRow(ammo, 100);
        }

        [TestMethod]
        public void WhatFitsAStackGoesInAndTheRestStaysInThePack()
        {
            using var fixture = new Fixture();
            var stored = fixture.InLockbox(Ammo, FullStack - 2, 0);
            var ammo = fixture.InPack(Ammo, 5, PackSlot);

            fixture.FillTab(1);
            fixture.Sent();
            fixture.Deposit(PackSlot, 1);

            Assert.AreEqual(FullStack, stored.StackSize);
            Assert.AreEqual(3u, ammo.StackSize);
            Assert.AreEqual(ammo.EntityId, fixture.Pack[PackSlot]);
            Assert.IsFalse(fixture.Lockbox.Contains(ammo.EntityId));
            fixture.AssertPackRow(ammo, PackSlot, 3);

            using (var unit = fixture.Context.CreateChar())
                Assert.AreEqual(FullStack, unit.Items.GetItem(stored.Id).StackSize);

            // Both counts, and why the rest is still there.
            var sent = fixture.SentMessages();

            Assert.AreEqual(FullStack, sent.Where(message => message.EntityId == stored.EntityId).Select(message => message.Packet).OfType<SetStackCountPacket>().Single().StackSize);
            Assert.AreEqual(3u, sent.Where(message => message.EntityId == ammo.EntityId).Select(message => message.Packet).OfType<SetStackCountPacket>().Single().StackSize);
            Assert.AreEqual(PlayerMessage.PmInventoryFull, sent.Select(message => message.Packet).OfType<DisplayClientMessagePacket>().Single().MsgId);
            Assert.IsFalse(sent.Any(message => message.Packet is InventoryRemoveItemPacket || message.Packet is InventoryAddItemPacket));
            fixture.AssertThePackStillWorks(ammo);
        }

        [TestMethod]
        public void AStackThatFitsAnotherWhollyIsGoneFromThePack()
        {
            using var fixture = new Fixture();
            var stored = fixture.InLockbox(Ammo, 10, 0);
            var ammo = fixture.InPack(Ammo, 5, PackSlot);

            fixture.FillTab(1);
            fixture.Sent();
            fixture.Deposit(PackSlot, 1);

            Assert.AreEqual(15u, stored.StackSize);
            Assert.AreEqual(0UL, fixture.Pack[PackSlot]);
            Assert.IsNull(EntityManager.Instance.GetItem(ammo.EntityId), "merged away");

            using (var unit = fixture.Context.CreateChar())
            {
                Assert.AreEqual(15u, unit.Items.GetItem(stored.Id).StackSize);
                Assert.IsNull(unit.Items.GetItem(ammo.Id));
                Assert.IsNull(unit.CharacterInventories.FindByItemId(ammo.Id));
            }

            var sent = fixture.Sent();

            Assert.AreEqual(ammo.EntityId, sent.OfType<DestroyPhysicalEntityPacket>().Single().EntityId);
            Assert.AreEqual(ammo.EntityId, sent.OfType<InventoryRemoveItemPacket>().Single(packet => packet.InventoryType == InventoryType.Personal).EntityId);
            Assert.IsFalse(sent.Any(packet => packet is DisplayClientMessagePacket));
        }

        [TestMethod]
        public void ATabWithRoomTakesTheItemInItsFirstFreeSlot()
        {
            using var fixture = new Fixture();
            var first = fixture.InLockbox(Filler, 1, 0);
            var ammo = fixture.InPack(Ammo, 5, PackSlot);

            fixture.Sent();
            fixture.Deposit(PackSlot, 1);

            Assert.AreEqual(0UL, fixture.Pack[PackSlot]);
            Assert.AreEqual(first.EntityId, fixture.Lockbox[0]);
            Assert.AreEqual(ammo.EntityId, fixture.Lockbox[1]);
            Assert.AreEqual(5u, ammo.StackSize);
            fixture.AssertLockboxRow(ammo, 1);

            var sent = fixture.Sent();

            Assert.AreEqual(ammo.EntityId, sent.OfType<InventoryRemoveItemPacket>().Single(packet => packet.InventoryType == InventoryType.Personal).EntityId);
            Assert.IsTrue(sent.OfType<InventoryAddItemPacket>().Any(packet => packet.Type == InventoryType.ClanInventory && packet.EntityId == ammo.EntityId && packet.SlotId == 1));
            Assert.IsFalse(sent.Any(packet => packet is DisplayClientMessagePacket));
        }

        [TestMethod]
        public void TwoItemsSavedInOnePackSlotAreBothInThePackAfterLogin()
        {
            // Where a deposit into a full tab used to lead: the item's row stayed in its pack
            // slot with the list empty there, a purchase was given the slot and a row of its
            // own, and from the next login one of the two was in no slot.
            using var fixture = new Fixture();
            var ammo = fixture.InPack(Ammo, 5, PackSlot);
            var bought = fixture.InPack(Filler, 1, PackSlot);
            var other = fixture.InPack(Filler + 1, 1, PackSlot + 1);

            fixture.Login();

            var slots = fixture.SlotsByItem();

            Assert.AreEqual(3, slots.Count);
            Assert.AreEqual(PackSlot + 1, slots[other.Id]);
            CollectionAssert.AreEquivalent(new[] { PackSlot, PackSlot + 2 }, new[] { slots[ammo.Id], slots[bought.Id] }, "one keeps the slot, the other has the first free one of the tab");
            fixture.AssertPackRow(ammo, slots[ammo.Id], 5);
            fixture.AssertPackRow(bought, slots[bought.Id], 1);
            fixture.AssertPackRow(other, PackSlot + 1, 1);

            // The client is shown each once, where it is.
            var shown = fixture.Sent().OfType<InventoryAddItemPacket>().Where(packet => packet.Type == InventoryType.Personal).ToArray();

            CollectionAssert.AreEquivalent(new[] { (uint)PackSlot, (uint)PackSlot + 1, (uint)PackSlot + 2 }, shown.Select(packet => packet.SlotId).ToArray());
            fixture.AssertThePackStillWorks(fixture.Loaded(PackSlot + 2));

            // And they stay where they are.
            var before = fixture.SlotsByItem();

            fixture.Login();

            CollectionAssert.AreEquivalent(before.ToArray(), fixture.SlotsByItem().ToArray());
        }

        [TestMethod]
        public void ASecondItemInOnePackSlotIsLeftWhenTheTabIsFull()
        {
            using var fixture = new Fixture();
            var ammo = fixture.InPack(Ammo, 5, PackSlot);
            var bought = fixture.InPack(Filler, 1, PackSlot);

            for (var slot = PackSlot + 1; slot < PackSlot + 50; slot++)
                fixture.InPack(Filler + 1, 1, slot);

            fixture.Login();

            // As it was: one of the two is shown, and both rows are as they were.
            Assert.AreEqual(50, fixture.SlotsByItem().Count);
            fixture.AssertPackRow(ammo, PackSlot, 5);
            fixture.AssertPackRow(bought, PackSlot, 1);
        }

        /// <summary>The leader of a clan, online, with its lockbox open to them.</summary>
        private sealed class Fixture : IDisposable
        {
            private readonly ClanManager _clans = ClanManager.Instance;
            private readonly System.Collections.Concurrent.ConcurrentDictionary<uint, Lazy<ClanEntry>> _previousClans;
            private readonly System.Collections.Concurrent.ConcurrentDictionary<uint, Lazy<List<ClanMemberEntry>>> _previousMembers;

            internal MissionTestContext Context { get; } = MissionTestContext.WithCompletableMission(429);
            internal InventoryManager Inventory { get; }
            internal Client Client => Context.Client;
            internal List<ulong> Pack => Client.Player.Inventory.PersonalInventory;
            internal List<ulong> Lockbox => Client.Player.Inventory.ClanInventory;

            internal Fixture(uint tabs = 1)
            {
                _previousClans = _clans.Clans;
                _previousMembers = _clans.ClanMembers;

                Inventory = new InventoryManager(Context, Context.Manager);
                Client.Player.Inventory.ResetClanInventory();
                Context.CreateClanForPlayer().PurashedTabs = tabs;

                // Online, as the lockbox's changes go to every member who is.
                lock (Server.Clients)
                    Server.Clients.Add(Client);
            }

            internal Item InPack(uint templateId, uint quantity, int slot)
            {
                var item = Make(templateId, quantity);

                item.OwnerId = Client.Player.Id;
                item.OwnerSlotId = (uint)slot;
                Pack[slot] = item.EntityId;

                using var unit = Context.CreateChar();
                unit.CharacterInventories.AddInvItem(Client.AccountEntry.Id, Client.Player.Id, (uint)InventoryType.Personal, (uint)slot, item.Id);

                return item;
            }

            internal Item InLockbox(uint templateId, uint quantity, int slot)
            {
                var item = Make(templateId, quantity);

                item.OwnerId = 0;
                item.OwnerSlotId = (uint)slot;
                Lockbox[slot] = item.EntityId;

                using var unit = Context.CreateChar();
                unit.ClanInventories.AddInvItem(Client.Player.ClanId, (uint)slot, item.Id);

                return item;
            }

            /// <summary>Something else in every free slot of the tab.</summary>
            internal void FillTab(uint tabId)
            {
                var (first, last) = ClanLockboxTab.SlotRange(tabId);

                for (var slot = (int)first; slot < last; slot++)
                    if (Lockbox[slot] == 0)
                        InLockbox(Filler, 1, slot);
            }

            /// <summary>A pack item right-clicked with the lockbox open, or dropped on a tab's button.</summary>
            internal void Deposit(int packSlot, int tabId, bool noTabNamed = false)
            {
                Inventory.ClanLockbox_DepositItemInTab(Client, new ClanLockbox_DepositItemInTabPacket
                {
                    SrcSlot = packSlot,
                    DestTab = noTabNamed ? 0 : tabId,
                    NoTabNamed = noTabNamed,
                    Quantity = 1
                });
            }

            /// <summary>The lists built again from the rows, as at a login.</summary>
            internal void Login()
            {
                Sent();
                Inventory.InitCharacterInventory(Client);
            }

            /// <summary>The item in that pack slot.</summary>
            internal Item Loaded(int slot) => EntityManager.Instance.GetItem(Pack[slot]);

            /// <summary>Each item in the pack, by its item id, and the slot it is in.</summary>
            internal Dictionary<uint, int> SlotsByItem() => Enumerable.Range(0, Pack.Count).Where(slot => Pack[slot] != 0)
                .ToDictionary(slot => Loaded(slot).Id, slot => slot);

            internal List<CallMethodMessage> SentMessages() => WorldTestContext.Drain(Client)
                .Select(packet => packet.Message).OfType<CallMethodMessage>().ToList();

            internal List<Rasa.Packets.PythonPacket> Sent() => SentMessages().Select(message => message.Packet).ToList();

            internal void AssertPackRow(Item item, int slot, uint quantity)
            {
                using var unit = Context.CreateChar();
                var row = unit.CharacterInventories.FindByItemId(item.Id);

                Assert.IsNotNull(row);
                Assert.AreEqual((uint)InventoryType.Personal, row.InventoryType);
                Assert.AreEqual(Client.Player.Id, row.CharacterId);
                Assert.AreEqual((uint)slot, row.SlotId);
                Assert.AreEqual(quantity, unit.Items.GetItem(item.Id).StackSize);
                Assert.AreEqual(0, unit.ClanInventories.GetItems(Client.Player.ClanId).Count(entry => entry.ItemId == item.Id));
            }

            internal void AssertLockboxRow(Item item, int slot)
            {
                using var unit = Context.CreateChar();

                Assert.AreEqual((uint)slot, unit.ClanInventories.GetItems(Client.Player.ClanId).Single(entry => entry.ItemId == item.Id).SlotId);
                Assert.IsNull(unit.CharacterInventories.FindByItemId(item.Id));
            }

            /// <summary>
            /// The item can be moved in the pack: a pack move is planned over the whole pack, as
            /// loot, consumables and mission items are, and is refused while a slot the lists
            /// have empty has a row.
            /// </summary>
            internal void AssertThePackStillWorks(Item item)
            {
                var from = (int)item.OwnerSlotId;

                Assert.AreEqual(item.EntityId, Pack[from]);
                Inventory.PersonalInventory_MoveItem(Client, new PersonalInventory_MoveItemPacket { SrcSlot = from, DestSlot = from + 1, Quantity = 1 });

                Assert.AreEqual(item.EntityId, Pack[from + 1], "a pack move is refused");
                Assert.AreEqual(0UL, Pack[from]);
            }

            private Item Make(uint templateId, uint quantity)
            {
                Context.AddRewardTemplate(templateId, ItemClass);

                var template = EntityClassManager.Instance.LoadedEntityClasses[(EntityClasses)ItemClass].ItemTemplates[templateId];
                var item = ItemManager.StageItem(template, quantity, "");

                using (var unit = Context.CreateChar())
                    item.Id = unit.Items.CreateItem(item);

                EntityManager.Instance.RegisterEntity(item.EntityId, EntityType.Item);
                EntityManager.Instance.RegisterItem(item.EntityId, item);

                return item;
            }

            public void Dispose()
            {
                lock (Server.Clients)
                    Server.Clients.Remove(Client);

                _clans.Clans = _previousClans;
                _clans.ClanMembers = _previousMembers;
                Client.Player.ClanId = 0;
                Context.Dispose();
            }
        }
    }
}
