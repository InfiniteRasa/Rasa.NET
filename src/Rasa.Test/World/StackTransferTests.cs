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
    using Rasa.Structures.Char;
    using Rasa.Test.Missions;

    // Between the pack, the footlocker and the clan lockbox, and inside the lockbox, a move took
    // the whole stack whatever quantity came with it, and a stack dropped on one of the same item
    // swapped with it (or, in the lockbox, did nothing). The client sends what its quantity
    // chooser dragged, one for a ctrl+right-click, and the stack's count for a plain drag or
    // right-click (inventorywindow._GetQuantity, lockboxwindow, clanlockboxwindow). Now part of a
    // stack is split off into an empty slot, and a stack goes into one it can join, as much as
    // fits (InventoryManager.Stacks) - and every member holding the lockbox sees it.
    [TestClass]
    [DoNotParallelize]
    public class StackTransferTests
    {
        private const uint Ammo = 9401, Other = 9402, ItemClass = 3147;
        private const uint FullStack = 50000;   // MissionTestContext.AddRewardTemplate's class
        private const int Tab = 50;             // the pack tab of category 2, where the reward templates go

        #region Pack and footlocker

        [TestMethod]
        public void PartOfAPackStackIntoAnEmptyFootlockerSlotIsANewStackThere()
        {
            using var f = new Fixture();
            var ammo = f.InPack(Ammo, 10, Tab, crafter: "Ellimist");

            f.Inventory.RequestMoveItemToHomeInventory(f.Taker, new RequestMoveItemToHomeInventoryPacket { SrcSlot = Tab, DestSlot = 2, Quantity = 4 });

            Assert.AreEqual(6u, ammo.StackSize);
            Assert.AreEqual(ammo.EntityId, f.Pack[Tab]);
            var stored = EntityManager.Instance.GetItem(f.Home[2]);
            Assert.AreEqual(4u, stored.StackSize);
            Assert.AreEqual("Ellimist", stored.Crafter);
            f.AssertRow(ammo, InventoryType.Personal, Tab, 6);
            f.AssertRow(stored, InventoryType.HomeInventory, 2, 4);
            f.AssertThePackStillWorks();
        }

        [TestMethod]
        public void APackStackDroppedOnTheSameItemInTheFootlockerGoesIntoIt()
        {
            using var f = new Fixture();
            var ammo = f.InPack(Ammo, 10, Tab);
            var stored = f.InHome(Ammo, 5, 2);

            f.Inventory.RequestMoveItemToHomeInventory(f.Taker, new RequestMoveItemToHomeInventoryPacket { SrcSlot = Tab, DestSlot = 2, Quantity = 10 });

            Assert.AreEqual(15u, stored.StackSize);
            Assert.AreEqual(0UL, f.Pack[Tab]);
            Assert.IsNull(EntityManager.Instance.GetItem(ammo.EntityId));
            f.AssertRow(stored, InventoryType.HomeInventory, 2, 15);
            f.AssertGone(ammo);
            f.AssertThePackStillWorks(empty: true);
        }

        [TestMethod]
        public void PartOfAFootlockerStackIntoThePackIsANewStackThere()
        {
            using var f = new Fixture();
            var stored = f.InHome(Ammo, 10, 2);

            f.Inventory.RequestTakeItemFromHomeInventory(f.Taker, new RequestTakeItemFromHomeInventoryPacket { SrcSlot = 2, DestSlot = Tab + 3, Quantity = 3 });

            Assert.AreEqual(7u, stored.StackSize);
            var taken = EntityManager.Instance.GetItem(f.Pack[Tab + 3]);
            Assert.AreEqual(3u, taken.StackSize);
            Assert.AreEqual(f.Taker.Player.Id, taken.OwnerId);
            Assert.AreEqual((uint)(Tab + 3), taken.OwnerSlotId);
            f.AssertRow(stored, InventoryType.HomeInventory, 2, 7);
            f.AssertRow(taken, InventoryType.Personal, Tab + 3, 3);
            f.AssertThePackStillWorks();
        }

        [TestMethod]
        public void AFootlockerStackDroppedOnTheSameItemInThePackGoesIntoIt()
        {
            using var f = new Fixture();
            var stored = f.InHome(Ammo, 10, 2);
            var carried = f.InPack(Ammo, FullStack - 4, Tab);

            f.Inventory.RequestTakeItemFromHomeInventory(f.Taker, new RequestTakeItemFromHomeInventoryPacket { SrcSlot = 2, DestSlot = Tab, Quantity = 10 });

            Assert.AreEqual(FullStack, carried.StackSize, "as much as fits");
            Assert.AreEqual(6u, stored.StackSize, "the rest stays");
            Assert.AreEqual(stored.EntityId, f.Home[2]);
            f.AssertRow(carried, InventoryType.Personal, Tab, FullStack);
            f.AssertRow(stored, InventoryType.HomeInventory, 2, 6);
            f.AssertThePackStillWorks();
        }

        [TestMethod]
        public void PartOfAStackOnAnotherItemIsRefusedEitherWay()
        {
            using var f = new Fixture();
            var ammo = f.InPack(Ammo, 10, Tab);
            var other = f.InHome(Other, 1, 2);

            f.Inventory.RequestMoveItemToHomeInventory(f.Taker, new RequestMoveItemToHomeInventoryPacket { SrcSlot = Tab, DestSlot = 2, Quantity = 4 });

            Assert.AreEqual(10u, ammo.StackSize);
            Assert.AreEqual(ammo.EntityId, f.Pack[Tab]);
            Assert.AreEqual(other.EntityId, f.Home[2]);

            var stored = f.InHome(Ammo, 10, 3);
            var carried = f.InPack(Other, 1, Tab + 1);

            f.Inventory.RequestTakeItemFromHomeInventory(f.Taker, new RequestTakeItemFromHomeInventoryPacket { SrcSlot = 3, DestSlot = Tab + 1, Quantity = 4 });

            Assert.AreEqual(10u, stored.StackSize);
            Assert.AreEqual(carried.EntityId, f.Pack[Tab + 1]);
        }

        #endregion

        #region Clan lockbox

        [TestMethod]
        public void PartOfAPackStackIntoALockboxSlotIsANewStackEveryMemberSees()
        {
            using var f = new Fixture();
            var ammo = f.InPack(Ammo, 10, Tab);

            f.Inventory.ClanLockbox_DepositItemInSlot(f.Taker, new ClanLockbox_DepositItemInSlotPacket { SrcSlot = Tab, DestSlot = 5, Quantity = 4 });

            Assert.AreEqual(6u, ammo.StackSize);
            var stored = EntityManager.Instance.GetItem(f.Lockbox(f.Taker)[5]);
            Assert.AreEqual(4u, stored.StackSize);
            Assert.AreEqual(stored.EntityId, f.Lockbox(f.Member)[5], "the other member holds it too");
            Assert.IsTrue(f.Sent(f.Member).OfType<InventoryAddItemPacket>().Any(p => p.Type == InventoryType.ClanInventory && p.EntityId == stored.EntityId && p.SlotId == 5));
            f.AssertRow(ammo, InventoryType.Personal, Tab, 6);
            f.AssertLockboxRow(stored, 5, 4);
            f.AssertThePackStillWorks();
        }

        [TestMethod]
        public void OneTakenWithACtrlRightClickGoesIntoALockboxStackItJoins()
        {
            using var f = new Fixture();
            var ammo = f.InPack(Ammo, 10, Tab);
            var stored = f.InLockbox(Ammo, 5, 20);

            f.Inventory.ClanLockbox_DepositItemInTab(f.Taker, new ClanLockbox_DepositItemInTabPacket { SrcSlot = Tab, DestTab = 1, Quantity = 1 });

            Assert.AreEqual(9u, ammo.StackSize);
            Assert.AreEqual(6u, stored.StackSize);
            Assert.AreEqual(6u, f.Sent(f.Member).OfType<SetStackCountPacket>().Single().StackSize, "the other member's count");
            f.AssertRow(ammo, InventoryType.Personal, Tab, 9);
            f.AssertLockboxRow(stored, 20, 6);
        }

        [TestMethod]
        public void PartOfALockboxStackIntoThePackIsANewStack()
        {
            using var f = new Fixture();
            var stored = f.InLockbox(Ammo, 10, 5);

            f.Inventory.ClanLockbox_WithdrawItem(f.Taker, new ClanLockbox_WithdrawItemPacket { SrcSlot = 5, DestSlot = Tab + 2, Quantity = 3 });

            Assert.AreEqual(7u, stored.StackSize);
            var taken = EntityManager.Instance.GetItem(f.Pack[Tab + 2]);
            Assert.AreEqual(3u, taken.StackSize);
            Assert.AreEqual(7u, f.Sent(f.Member).OfType<SetStackCountPacket>().Single().StackSize);
            f.AssertLockboxRow(stored, 5, 7);
            f.AssertRow(taken, InventoryType.Personal, Tab + 2, 3);
            f.AssertThePackStillWorks();
        }

        [TestMethod]
        public void OneTakenOutWithACtrlRightClickJoinsTheStackCarried()
        {
            using var f = new Fixture();
            var stored = f.InLockbox(Ammo, 10, 5);
            var carried = f.InPack(Ammo, 2, Tab + 7);

            f.Inventory.ClanLockbox_WithdrawItem(f.Taker, new ClanLockbox_WithdrawItemPacket { SrcSlot = 5, DestSlot = 0, ManagePersonalSlot = true, Quantity = 1 });

            Assert.AreEqual(9u, stored.StackSize);
            Assert.AreEqual(3u, carried.StackSize);
            f.AssertRow(carried, InventoryType.Personal, Tab + 7, 3);
            f.AssertLockboxRow(stored, 5, 9);
        }

        [TestMethod]
        public void ALockboxStackDroppedOnTheSameItemInTheLockboxGoesIntoIt()
        {
            using var f = new Fixture();
            var stored = f.InLockbox(Ammo, 10, 5);
            var stack = f.InLockbox(Ammo, 5, 6);

            f.Inventory.ClanLockbox_MoveItem(f.Taker, new ClanLockbox_MoveItemPacket { SrcSlot = 5, DestSlot = 6, Quantity = 10 });

            Assert.AreEqual(15u, stack.StackSize);
            Assert.AreEqual(0UL, f.Lockbox(f.Taker)[5]);
            Assert.AreEqual(0UL, f.Lockbox(f.Member)[5], "gone for the other member too");
            Assert.AreEqual(stored.EntityId, f.Sent(f.Member).OfType<InventoryRemoveItemPacket>().Single().EntityId);
            f.AssertLockboxRow(stack, 6, 15);
            f.AssertGone(stored);
        }

        [TestMethod]
        public void PartOfALockboxStackMovedInsideTheLockboxIsANewStack()
        {
            using var f = new Fixture();
            var stored = f.InLockbox(Ammo, 10, 5);

            f.Inventory.ClanLockbox_MoveItem(f.Taker, new ClanLockbox_MoveItemPacket { SrcSlot = 5, DestSlot = 9, Quantity = 4 });

            Assert.AreEqual(6u, stored.StackSize);
            var split = EntityManager.Instance.GetItem(f.Lockbox(f.Taker)[9]);
            Assert.AreEqual(4u, split.StackSize);
            Assert.AreEqual(split.EntityId, f.Lockbox(f.Member)[9]);
            f.AssertLockboxRow(stored, 5, 6);
            f.AssertLockboxRow(split, 9, 4);
        }

        #endregion

        #region Fixture

        private sealed class Fixture : IDisposable
        {
            private readonly ClanManager _clans = ClanManager.Instance;
            private readonly System.Collections.Concurrent.ConcurrentDictionary<uint, Lazy<ClanEntry>> _previousClans;
            private readonly System.Collections.Concurrent.ConcurrentDictionary<uint, Lazy<List<ClanMemberEntry>>> _previousMembers;

            internal MissionTestContext Context { get; } = MissionTestContext.WithCompletableMission(429);
            internal InventoryManager Inventory { get; }
            internal Client Taker => Context.Client;
            internal Client Member { get; }
            internal ClanEntry Clan { get; }
            internal List<ulong> Pack => Taker.Player.Inventory.PersonalInventory;
            internal List<ulong> Home => Taker.Player.Inventory.HomeInventory;
            internal List<ulong> Lockbox(Client client) => client.Player.Inventory.ClanInventory;

            internal Fixture()
            {
                _previousClans = _clans.Clans;
                _previousMembers = _clans.ClanMembers;

                Inventory = new InventoryManager(Context, Context.Manager);
                Taker.Player.Inventory.HomeInventory = Enumerable.Repeat(0UL, LockboxTab.TotalSlots).ToList();
                Taker.Player.LockboxTabs = 1;

                Member = Context.CreateAdditionalClient(2);
                Clan = Context.CreateClanForPlayer();

                using (var unit = Context.CreateChar())
                    Assert.IsTrue(unit.ClanMembers.InsertClanMemberData(Clan.Id, Member.Player.Id, ClanRank.Member, ""));

                _clans.ClanMembers[Clan.Id].Value.Add(new ClanMemberEntry { ClanId = Clan.Id, CharacterId = Member.Player.Id, Rank = ClanRank.Member, Note = "" });
                Member.Player.ClanId = Clan.Id;
                Taker.Player.Inventory.ResetClanInventory();
                Member.Player.Inventory.ResetClanInventory();

                lock (Server.Clients)
                    Server.Clients.AddRange(new[] { Taker, Member });

                WorldTestContext.Drain(Taker);
                WorldTestContext.Drain(Member);
            }

            internal Item InPack(uint templateId, uint quantity, int slot, string crafter = "")
            {
                var item = Make(templateId, quantity, crafter);

                item.OwnerId = Taker.Player.Id;
                item.OwnerSlotId = (uint)slot;
                Pack[slot] = item.EntityId;

                using var unit = Context.CreateChar();
                unit.CharacterInventories.AddInvItem(Taker.AccountEntry.Id, Taker.Player.Id, (uint)InventoryType.Personal, (uint)slot, item.Id);

                return item;
            }

            internal Item InHome(uint templateId, uint quantity, int slot)
            {
                var item = Make(templateId, quantity, "");

                item.OwnerId = 0;
                item.OwnerSlotId = (uint)slot;
                Home[slot] = item.EntityId;

                using var unit = Context.CreateChar();
                unit.CharacterInventories.AddInvItem(Taker.AccountEntry.Id, 0, (uint)InventoryType.HomeInventory, (uint)slot, item.Id);

                return item;
            }

            /// <summary>An item in a lockbox slot, as both members hold it.</summary>
            internal Item InLockbox(uint templateId, uint quantity, int slot)
            {
                var item = Make(templateId, quantity, "");

                item.OwnerId = 0;
                item.OwnerSlotId = (uint)slot;
                Lockbox(Taker)[slot] = item.EntityId;
                Lockbox(Member)[slot] = item.EntityId;

                using var unit = Context.CreateChar();
                unit.ClanInventories.AddInvItem(Clan.Id, (uint)slot, item.Id);

                return item;
            }

            internal List<PythonPacket> Sent(Client client) => WorldTestContext.Drain(client)
                .Select(packet => packet.Message).OfType<CallMethodMessage>().Select(message => message.Packet).ToList();

            internal void AssertRow(Item item, InventoryType type, int slot, uint quantity)
            {
                using var unit = Context.CreateChar();
                var row = unit.CharacterInventories.FindByItemId(item.Id);

                Assert.IsNotNull(row, $"item {item.Id}'s row");
                Assert.AreEqual((uint)type, row.InventoryType);
                Assert.AreEqual(type == InventoryType.Personal ? Taker.Player.Id : 0u, row.CharacterId);
                Assert.AreEqual((uint)slot, row.SlotId);
                Assert.AreEqual(quantity, unit.Items.GetItem(item.Id).StackSize);
                Assert.AreEqual(0, unit.ClanInventories.GetItems(Clan.Id).Count(entry => entry.ItemId == item.Id));
            }

            internal void AssertLockboxRow(Item item, int slot, uint quantity)
            {
                using var unit = Context.CreateChar();

                Assert.AreEqual((uint)slot, unit.ClanInventories.GetItems(Clan.Id).Single(entry => entry.ItemId == item.Id).SlotId);
                Assert.IsNull(unit.CharacterInventories.FindByItemId(item.Id));
                Assert.AreEqual(quantity, unit.Items.GetItem(item.Id).StackSize);
            }

            internal void AssertGone(Item item)
            {
                using var unit = Context.CreateChar();

                Assert.IsNull(unit.Items.GetItem(item.Id), "the items row");
                Assert.IsNull(unit.CharacterInventories.FindByItemId(item.Id));
                Assert.AreEqual(0, unit.ClanInventories.GetItems(Clan.Id).Count(entry => entry.ItemId == item.Id));
            }

            /// <summary>The pack still takes a planned move: every stack in it agrees with its rows (InventoryPlan).</summary>
            internal void AssertThePackStillWorks(bool empty = false)
            {
                var from = Enumerable.Range(Tab, 50).FirstOrDefault(slot => Pack[slot] != 0, -1);

                if (from < 0)
                {
                    Assert.IsTrue(empty, "something in the pack");
                    return;
                }

                var moved = Pack[from];
                var to = Enumerable.Range(Tab, 50).Last(slot => Pack[slot] == 0);

                Inventory.PersonalInventory_MoveItem(Taker, new PersonalInventory_MoveItemPacket
                {
                    SrcSlot = from, DestSlot = to, Quantity = (int)EntityManager.Instance.GetItem(moved).StackSize
                });

                Assert.AreEqual(moved, Pack[to], "a pack move is refused");
            }

            private Item Make(uint templateId, uint quantity, string crafter)
            {
                Context.AddRewardTemplate(templateId, ItemClass);

                var template = EntityClassManager.Instance.LoadedEntityClasses[(EntityClasses)ItemClass].ItemTemplates[templateId];
                var item = ItemManager.StageItem(template, quantity, crafter);

                using (var unit = Context.CreateChar())
                    item.Id = unit.Items.CreateItem(item);

                EntityManager.Instance.RegisterEntity(item.EntityId, EntityType.Item);
                EntityManager.Instance.RegisterItem(item.EntityId, item);

                return item;
            }

            public void Dispose()
            {
                lock (Server.Clients)
                {
                    Server.Clients.Remove(Taker);
                    Server.Clients.Remove(Member);
                }

                _clans.Clans = _previousClans;
                _clans.ClanMembers = _previousMembers;
                Taker.Player.ClanId = 0;
                Context.Dispose();
            }
        }

        #endregion
    }
}
