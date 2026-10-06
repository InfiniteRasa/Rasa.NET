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
    using Rasa.Packets.Protocol;
    using Rasa.Structures;
    using Rasa.Structures.Char;
    using Rasa.Test.Missions;

    // The pack is five tabs of fifty slots, one per inventory category, and an item is kept in a
    // slot of its own tab: every inventory plan (a pack move, corpse loot, a consumable, a mission
    // item or reward) checks that of every stack before it does anything, and refuses if one is
    // out of place. The footlocker and the clan lockbox have no tabs of that kind.
    //
    // The client sends an item dropped into the pack to a slot of its own tab
    // (inventorywindow.OnDNDDrop, inventory.AddItemToPersonalInventory). A drop on a storage
    // slot it sends as it is, whatever the slot holds (lockboxwindow.OnDNDDrop,
    // clanlockboxwindow.OnDNDDrop): "RequestMoveItemToHomeInventory" or
    // "ClanLockbox_DepositItemInSlot", (srcSlot, destSlot, quantity). What the slot held comes out
    // to the pack, and the only pack slot the request names is the one the dragged item left.
    [TestClass]
    [DoNotParallelize]
    public class StorageSwapCategoryTests
    {
        private const uint Rifle = 9101, Pistol = 9102, Ammo = 9103, Medkit = 9104, Filler = 9105, ItemClass = 3147;
        private const int Equipment = 0, Consumables = 50, TabSize = 50;

        [TestMethod]
        [DataRow(false, DisplayName = "footlocker")]
        [DataRow(true, DisplayName = "clan lockbox")]
        public void AnItemSwappedOutOfStorageGoesToItsOwnTab(bool clan)
        {
            using var fixture = new Fixture(clan);
            var rifle = fixture.InPack(Rifle, InventoryCategory.Equipment, 3);
            var ammo = fixture.InStorage(Ammo, InventoryCategory.Consumable, 7);

            fixture.DropOnStorage(packSlot: 3, storageSlot: 7);

            Assert.AreEqual(rifle.EntityId, fixture.Storage[7]);
            Assert.AreEqual(0UL, fixture.Pack[3], "the slot the rifle left is not one of the ammunition's tab");
            Assert.AreEqual(ammo.EntityId, fixture.Pack[Consumables]);
            Assert.AreEqual((uint)Consumables, ammo.OwnerSlotId);
            fixture.AssertPackRow(ammo, Consumables);
            fixture.AssertStorageRow(rifle, 7);

            var added = fixture.Sent().OfType<InventoryAddItemPacket>().ToArray();

            Assert.IsTrue(added.Any(packet => packet.Type == InventoryType.Personal && packet.EntityId == ammo.EntityId && packet.SlotId == Consumables));
            fixture.AssertThePackStillWorks();
        }

        [TestMethod]
        [DataRow(false, DisplayName = "footlocker")]
        [DataRow(true, DisplayName = "clan lockbox")]
        public void ItemsOfOneTabStillTradePlaces(bool clan)
        {
            using var fixture = new Fixture(clan);
            var rifle = fixture.InPack(Rifle, InventoryCategory.Equipment, 3);
            var pistol = fixture.InStorage(Pistol, InventoryCategory.Equipment, 7);

            fixture.DropOnStorage(packSlot: 3, storageSlot: 7);

            Assert.AreEqual(rifle.EntityId, fixture.Storage[7]);
            Assert.AreEqual(pistol.EntityId, fixture.Pack[3]);
            fixture.AssertPackRow(pistol, 3);
            fixture.AssertStorageRow(rifle, 7);
            fixture.AssertThePackStillWorks();
        }

        [TestMethod]
        [DataRow(false, DisplayName = "footlocker")]
        [DataRow(true, DisplayName = "clan lockbox")]
        public void ASwapIsRefusedWhenTheOtherItemsTabIsFull(bool clan)
        {
            using var fixture = new Fixture(clan);
            var rifle = fixture.InPack(Rifle, InventoryCategory.Equipment, 3);
            var ammo = fixture.InStorage(Ammo, InventoryCategory.Consumable, 7);

            fixture.Fill(Consumables);
            fixture.Sent();
            fixture.DropOnStorage(packSlot: 3, storageSlot: 7);

            Assert.AreEqual(rifle.EntityId, fixture.Pack[3]);
            Assert.AreEqual(ammo.EntityId, fixture.Storage[7]);
            fixture.AssertPackRow(rifle, 3);
            fixture.AssertStorageRow(ammo, 7);

            // Told why, and nothing else: no item has moved on the client either.
            var sent = fixture.Sent();

            Assert.AreEqual(PlayerMessage.PmInventoryFull, ((DisplayClientMessagePacket)sent.Single()).MsgId);
            fixture.AssertThePackStillWorks();
        }

        [TestMethod]
        [DataRow(false, false, DisplayName = "footlocker, onto a free slot")]
        [DataRow(false, true, DisplayName = "footlocker, onto an item")]
        [DataRow(true, false, DisplayName = "clan lockbox, onto a free slot")]
        [DataRow(true, true, DisplayName = "clan lockbox, onto an item")]
        public void AnItemTakenOutToASlotOfAnotherTabGoesToItsOwn(bool clan, bool occupied)
        {
            // Not something the client sends: it works the slot out from the item's own tab.
            using var fixture = new Fixture(clan);
            var ammo = fixture.InStorage(Ammo, InventoryCategory.Consumable, 7);
            var rifle = occupied ? fixture.InPack(Rifle, InventoryCategory.Equipment, 3) : null;

            fixture.TakeOut(storageSlot: 7, packSlot: 3);

            Assert.AreEqual(0UL, fixture.Storage[7]);
            Assert.AreEqual(rifle?.EntityId ?? 0UL, fixture.Pack[3], "nothing is swapped into storage for a slot the item does not take");
            Assert.AreEqual(ammo.EntityId, fixture.Pack[Consumables]);
            fixture.AssertPackRow(ammo, Consumables);

            if (occupied)
                fixture.AssertPackRow(rifle, 3);

            fixture.AssertThePackStillWorks();
        }

        [TestMethod]
        [DataRow(false, DisplayName = "footlocker")]
        [DataRow(true, DisplayName = "clan lockbox")]
        public void AnItemTakenOutToAnotherTabStaysWhenItsOwnIsFull(bool clan)
        {
            using var fixture = new Fixture(clan);
            var ammo = fixture.InStorage(Ammo, InventoryCategory.Consumable, 7);

            fixture.Fill(Consumables);
            fixture.Sent();
            fixture.TakeOut(storageSlot: 7, packSlot: 3);

            Assert.AreEqual(ammo.EntityId, fixture.Storage[7]);
            Assert.AreEqual(0UL, fixture.Pack[3]);
            fixture.AssertStorageRow(ammo, 7);
            Assert.AreEqual(PlayerMessage.PmInventoryFull, ((DisplayClientMessagePacket)fixture.Sent().Single()).MsgId);
        }

        [TestMethod]
        [DataRow(false, DisplayName = "footlocker")]
        [DataRow(true, DisplayName = "clan lockbox")]
        public void AnItemTakenOutOntoOneOfItsOwnTabStillSwaps(bool clan)
        {
            using var fixture = new Fixture(clan);
            var ammo = fixture.InStorage(Ammo, InventoryCategory.Consumable, 7);
            var medkit = fixture.InPack(Medkit, InventoryCategory.Consumable, Consumables + 4);

            fixture.TakeOut(storageSlot: 7, packSlot: Consumables + 4);

            Assert.AreEqual(medkit.EntityId, fixture.Storage[7]);
            Assert.AreEqual(ammo.EntityId, fixture.Pack[Consumables + 4]);
            fixture.AssertPackRow(ammo, Consumables + 4);
            fixture.AssertStorageRow(medkit, 7);
            fixture.AssertThePackStillWorks();
        }

        /// <summary>A character with a footlocker, or with a clan lockbox they lead, and the two requests of each.</summary>
        private sealed class Fixture : IDisposable
        {
            private readonly bool _clan;
            private readonly ClanManager _clans = ClanManager.Instance;
            private readonly System.Collections.Concurrent.ConcurrentDictionary<uint, Lazy<ClanEntry>> _previousClans;
            private readonly System.Collections.Concurrent.ConcurrentDictionary<uint, Lazy<List<ClanMemberEntry>>> _previousMembers;

            internal MissionTestContext Context { get; } = MissionTestContext.WithCompletableMission(429);
            internal InventoryManager Inventory { get; }
            internal Client Client => Context.Client;
            internal List<ulong> Pack => Client.Player.Inventory.PersonalInventory;
            internal List<ulong> Storage => _clan ? Client.Player.Inventory.ClanInventory : Client.Player.Inventory.HomeInventory;

            internal Fixture(bool clan)
            {
                _clan = clan;
                _previousClans = _clans.Clans;
                _previousMembers = _clans.ClanMembers;

                Inventory = new InventoryManager(Context, Context.Manager);
                Client.Player.Inventory.HomeInventory = Enumerable.Repeat(0UL, LockboxTab.TotalSlots).ToList();
                Client.Player.LockboxTabs = 1;
                Client.Player.Inventory.ResetClanInventory();

                if (!clan)
                    return;

                Context.CreateClanForPlayer();

                // Online, as the lockbox's changes go to every member who is.
                lock (Server.Clients)
                    Server.Clients.Add(Client);
            }

            internal Item InPack(uint templateId, InventoryCategory category, int slot)
            {
                var item = Make(templateId, category);

                item.OwnerId = Client.Player.Id;
                item.OwnerSlotId = (uint)slot;
                Pack[slot] = item.EntityId;

                using var unit = Context.CreateChar();
                unit.CharacterInventories.AddInvItem(Client.AccountEntry.Id, Client.Player.Id, (uint)InventoryType.Personal, (uint)slot, item.Id);

                return item;
            }

            internal Item InStorage(uint templateId, InventoryCategory category, int slot)
            {
                var item = Make(templateId, category);

                item.OwnerId = 0;
                item.OwnerSlotId = (uint)slot;
                Storage[slot] = item.EntityId;

                using var unit = Context.CreateChar();

                if (_clan)
                    unit.ClanInventories.AddInvItem(Client.Player.ClanId, (uint)slot, item.Id);
                else
                    unit.CharacterInventories.AddInvItem(Client.AccountEntry.Id, 0, (uint)InventoryType.HomeInventory, (uint)slot, item.Id);

                return item;
            }

            /// <summary>Something else in every free slot of the tab that starts there.</summary>
            internal void Fill(int tab)
            {
                for (var slot = tab; slot < tab + TabSize; slot++)
                    if (Pack[slot] == 0)
                        InPack(Filler + (uint)slot, (InventoryCategory)(tab / TabSize + 1), slot);
            }

            /// <summary>A pack item dropped on a storage slot.</summary>
            internal void DropOnStorage(int packSlot, int storageSlot)
            {
                if (_clan)
                    Inventory.ClanLockbox_DepositItemInSlot(Client, new ClanLockbox_DepositItemInSlotPacket { SrcSlot = (uint)packSlot, DestSlot = (uint)storageSlot, Quantity = 1 });
                else
                    Inventory.RequestMoveItemToHomeInventory(Client, new RequestMoveItemToHomeInventoryPacket { SrcSlot = (uint)packSlot, DestSlot = (uint)storageSlot, Quantity = 1 });
            }

            /// <summary>A storage item dropped on a pack slot.</summary>
            internal void TakeOut(int storageSlot, int packSlot)
            {
                if (_clan)
                    Inventory.ClanLockbox_WithdrawItem(Client, new ClanLockbox_WithdrawItemPacket { SrcSlot = (uint)storageSlot, DestSlot = (uint)packSlot, Quantity = 1 });
                else
                    Inventory.RequestTakeItemFromHomeInventory(Client, new RequestTakeItemFromHomeInventoryPacket { SrcSlot = (uint)storageSlot, DestSlot = (uint)packSlot, Quantity = 1 });
            }

            internal List<Rasa.Packets.PythonPacket> Sent() => WorldTestContext.Drain(Client)
                .Select(packet => packet.Message).OfType<CallMethodMessage>().Select(message => message.Packet).ToList();

            internal void AssertPackRow(Item item, int slot)
            {
                using var unit = Context.CreateChar();
                var row = unit.CharacterInventories.FindByItemId(item.Id);

                Assert.IsNotNull(row);
                Assert.AreEqual((uint)InventoryType.Personal, row.InventoryType);
                Assert.AreEqual(Client.Player.Id, row.CharacterId);
                Assert.AreEqual((uint)slot, row.SlotId);
                Assert.AreEqual(0, unit.ClanInventories.GetItems(Client.Player.ClanId).Count(entry => entry.ItemId == item.Id));
            }

            internal void AssertStorageRow(Item item, int slot)
            {
                using var unit = Context.CreateChar();

                if (_clan)
                {
                    Assert.AreEqual((uint)slot, unit.ClanInventories.GetItems(Client.Player.ClanId).Single(entry => entry.ItemId == item.Id).SlotId);
                    Assert.IsNull(unit.CharacterInventories.FindByItemId(item.Id));
                    return;
                }

                var row = unit.CharacterInventories.FindByItemId(item.Id);

                Assert.IsNotNull(row);
                Assert.AreEqual((uint)InventoryType.HomeInventory, row.InventoryType);
                Assert.AreEqual(0u, row.CharacterId);
                Assert.AreEqual((uint)slot, row.SlotId);
            }

            /// <summary>
            /// Every item in the pack is in its own tab, and one of them can be moved: a pack move
            /// is planned over the whole pack, as loot, consumables and mission items are.
            /// </summary>
            internal void AssertThePackStillWorks()
            {
                // One that is where it belongs: a move is only ever within a tab.
                var from = Enumerable.Range(0, Pack.Count).First(slot => Pack[slot] != 0
                    && (int)EntityManager.Instance.GetItem(Pack[slot]).ItemTemplate.InventoryCategory - 1 == slot / TabSize);
                var moved = Pack[from];
                var tab = from / TabSize * TabSize;
                var to = Enumerable.Range(tab, TabSize).Last(slot => Pack[slot] == 0);

                Inventory.PersonalInventory_MoveItem(Client, new PersonalInventory_MoveItemPacket { SrcSlot = from, DestSlot = to, Quantity = 1 });

                Assert.AreEqual(moved, Pack[to], "a pack move is refused");
                Assert.AreEqual(0UL, Pack[from]);

                for (var slot = 0; slot < Pack.Count; slot++)
                    if (Pack[slot] != 0)
                        Assert.AreEqual(slot / TabSize, (int)EntityManager.Instance.GetItem(Pack[slot]).ItemTemplate.InventoryCategory - 1, $"pack slot {slot}");
            }

            private Item Make(uint templateId, InventoryCategory category)
            {
                Context.AddRewardTemplate(templateId, ItemClass);

                var template = EntityClassManager.Instance.LoadedEntityClasses[(EntityClasses)ItemClass].ItemTemplates[templateId];

                template.InventoryCategory = category;

                var item = ItemManager.StageItem(template, 1, "");

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
