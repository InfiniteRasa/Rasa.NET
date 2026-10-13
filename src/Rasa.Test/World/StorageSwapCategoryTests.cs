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

        [TestMethod]
        public void AnItemSavedInASlotOfAnotherTabIsPutInItsOwnAtLogin()
        {
            // What a swap left behind before it sent the item to its own tab: the row is read
            // back by slot, so the pack was refused everything at every login after it.
            using var fixture = new Fixture(false);
            var ammo = fixture.InPack(Ammo, InventoryCategory.Consumable, 3);
            var rifle = fixture.InPack(Rifle, InventoryCategory.Equipment, 4);
            var medkit = fixture.InPack(Medkit, InventoryCategory.Consumable, Consumables);

            fixture.Login();

            Assert.AreEqual(0UL, fixture.Pack[3]);
            Assert.AreEqual(ammo.Id, fixture.Loaded(Consumables + 1).Id, "the first free slot of its tab");
            Assert.AreEqual((uint)(Consumables + 1), fixture.Loaded(Consumables + 1).OwnerSlotId);
            Assert.AreEqual(rifle.Id, fixture.Loaded(4).Id);
            Assert.AreEqual(medkit.Id, fixture.Loaded(Consumables).Id);
            fixture.AssertPackRow(ammo, Consumables + 1);
            fixture.AssertPackRow(rifle, 4);
            fixture.AssertPackRow(medkit, Consumables);

            // The client is shown it where it now is, once.
            var shown = fixture.Sent().OfType<InventoryAddItemPacket>().Where(packet => packet.EntityId == fixture.Pack[Consumables + 1]).ToArray();

            Assert.AreEqual((uint)(Consumables + 1), shown.Single().SlotId);
            fixture.AssertThePackStillWorks();

            // And it stays there.
            fixture.Login();

            Assert.AreEqual(ammo.Id, fixture.Loaded(Consumables + 1).Id);
            Assert.AreEqual(0UL, fixture.Pack[3]);
        }

        [TestMethod]
        public void TwoItemsSavedInEachOthersTabsAreBothPutRight()
        {
            // The ammunition has no free slot until the rifle has left the one it is in.
            using var fixture = new Fixture(false);
            var ammo = fixture.InPack(Ammo, InventoryCategory.Consumable, 3);
            var rifle = fixture.InPack(Rifle, InventoryCategory.Equipment, Consumables + 10);

            fixture.Fill(Consumables);
            fixture.Login();

            Assert.AreEqual(rifle.Id, fixture.Loaded(Equipment).Id);
            Assert.AreEqual(ammo.Id, fixture.Loaded(Consumables + 10).Id);
            Assert.AreEqual(0UL, fixture.Pack[3]);
            fixture.AssertPackRow(rifle, Equipment);
            fixture.AssertPackRow(ammo, Consumables + 10);
            Assert.AreEqual(TabSize + 1, fixture.Pack.Count(entityId => entityId != 0));
            fixture.AssertThePackStillWorks();
        }

        [TestMethod]
        public void AnItemIsNotPutInTheSlotOfOneThatCannotLeaveIt()
        {
            // The only slot of the ammunition's tab that is not taken is the one the rifle is
            // in, and the rifle's own tab is full.
            using var fixture = new Fixture(false);
            var ammo = fixture.InPack(Ammo, InventoryCategory.Consumable, 120);
            var rifle = fixture.InPack(Rifle, InventoryCategory.Equipment, Consumables + 10);

            fixture.Fill(Equipment);
            fixture.Fill(Consumables);
            fixture.Login();

            Assert.AreEqual(ammo.Id, fixture.Loaded(120).Id);
            Assert.AreEqual(rifle.Id, fixture.Loaded(Consumables + 10).Id);
            fixture.AssertPackRow(ammo, 120);
            fixture.AssertPackRow(rifle, Consumables + 10);
            Assert.AreEqual(2 * TabSize + 1, fixture.Pack.Count(entityId => entityId != 0));
        }

        [TestMethod]
        public void AnItemSavedInASlotOfAnotherTabStaysWhenItsOwnIsFull()
        {
            using var fixture = new Fixture(false);
            var ammo = fixture.InPack(Ammo, InventoryCategory.Consumable, 3);

            fixture.Fill(Consumables);
            fixture.Login();

            Assert.AreEqual(ammo.Id, fixture.Loaded(3).Id);
            Assert.AreEqual(3u, fixture.Loaded(3).OwnerSlotId);
            fixture.AssertPackRow(ammo, 3);
            Assert.AreEqual(TabSize + 1, fixture.Pack.Count(entityId => entityId != 0));
        }

        /// <summary>A character with a footlocker, or with a clan lockbox they lead, and the two requests of each.</summary>
        #region What the client is told (BR-186)

        [TestMethod]
        [DataRow(3, 7, DisplayName = "low to high")]
        [DataRow(7, 3, DisplayName = "high to low")]
        public void APackSwapLeavesBothItemsInTheClientsPack(int from, int to)
        {
            using var fixture = new Fixture(false);
            var rifle = fixture.InPack(Rifle, InventoryCategory.Equipment, from);
            var pistol = fixture.InPack(Pistol, InventoryCategory.Equipment, to);
            var client = new ClientInventory();
            client.Put(InventoryType.Personal, rifle.EntityId, from);
            client.Put(InventoryType.Personal, pistol.EntityId, to);
            fixture.Sent();

            fixture.Inventory.PersonalInventory_MoveItem(fixture.Client, new PersonalInventory_MoveItemPacket { SrcSlot = from, DestSlot = to, Quantity = 1 });

            Assert.AreEqual(rifle.EntityId, fixture.Pack[to]);
            Assert.AreEqual(pistol.EntityId, fixture.Pack[from]);

            var sent = fixture.Sent();
            client.Replay(sent);

            Assert.AreEqual(rifle.EntityId, client.At(InventoryType.Personal, to), "the client still has the rifle");
            Assert.AreEqual(pistol.EntityId, client.At(InventoryType.Personal, from));
            Assert.AreEqual(2, client.Count(InventoryType.Personal));
            ClientInventory.AssertRemovalsFirst(sent);
        }

        [TestMethod]
        [DataRow(false, DisplayName = "footlocker")]
        [DataRow(true, DisplayName = "clan lockbox")]
        public void AStorageSwapEachWayLeavesEachItemInOneContainerOnTheClient(bool clan)
        {
            using var fixture = new Fixture(clan);
            var storage = clan ? InventoryType.ClanInventory : InventoryType.HomeInventory;
            var rifle = fixture.InPack(Rifle, InventoryCategory.Equipment, 3);
            var pistol = fixture.InStorage(Pistol, InventoryCategory.Equipment, 7);
            var client = new ClientInventory();
            client.Put(InventoryType.Personal, rifle.EntityId, 3);
            client.Put(storage, pistol.EntityId, 7);
            fixture.Sent();

            // In: the rifle to the slot the pistol is in, the pistol out to the pack.
            fixture.DropOnStorage(packSlot: 3, storageSlot: 7);
            var sent = fixture.Sent();
            client.Replay(sent);

            Assert.AreEqual(rifle.EntityId, client.At(storage, 7));
            Assert.AreEqual(pistol.EntityId, client.At(InventoryType.Personal, 3));
            Assert.AreEqual((1, 1), (client.Count(storage), client.Count(InventoryType.Personal)), "nothing left behind");
            Assert.IsNull(client.Where(storage, pistol.EntityId), "the pistol is out of storage");
            Assert.IsNull(client.Where(InventoryType.Personal, rifle.EntityId), "the rifle is out of the pack");

            // Out: the rifle back to the pack, the pistol into storage.
            fixture.TakeOut(storageSlot: 7, packSlot: 3);
            client.Replay(fixture.Sent());

            Assert.AreEqual(rifle.EntityId, client.At(InventoryType.Personal, 3));
            Assert.AreEqual(pistol.EntityId, client.At(storage, 7));
            Assert.AreEqual((1, 1), (client.Count(storage), client.Count(InventoryType.Personal)));
            Assert.IsNull(client.Where(storage, rifle.EntityId));
            Assert.IsNull(client.Where(InventoryType.Personal, pistol.EntityId));
        }

        /// <summary>
        /// The client's inventory as client/inventory.py keeps it: per container, an entity by
        /// slot and a slot by entity. Recv_InventoryAddItem takes an entity out of where that
        /// container has it and puts it in the slot (_AddItem); Recv_InventoryRemoveItem deletes
        /// the slot the container has the entity in, whatever that slot now holds
        /// (_RemoveEntityFromMappings).
        /// </summary>
        private sealed class ClientInventory
        {
            private readonly Dictionary<InventoryType, (Dictionary<int, ulong> BySlot, Dictionary<ulong, int> ByEntity)> _containers = new();

            private (Dictionary<int, ulong> BySlot, Dictionary<ulong, int> ByEntity) Of(InventoryType type)
            {
                if (!_containers.TryGetValue(type, out var container))
                    _containers[type] = container = (new Dictionary<int, ulong>(), new Dictionary<ulong, int>());

                return container;
            }

            internal void Put(InventoryType type, ulong entityId, int slot)
            {
                var (bySlot, byEntity) = Of(type);

                if (byEntity.ContainsKey(entityId))
                    Take(type, entityId);

                bySlot[slot] = entityId;
                byEntity[entityId] = slot;
            }

            internal void Take(InventoryType type, ulong entityId)
            {
                var (bySlot, byEntity) = Of(type);

                if (!byEntity.TryGetValue(entityId, out var slot))
                    return;

                byEntity.Remove(entityId);
                bySlot.Remove(slot);
            }

            internal void Replay(IEnumerable<Rasa.Packets.PythonPacket> packets)
            {
                foreach (var packet in packets)
                    switch (packet)
                    {
                        case InventoryAddItemPacket add:
                            Put(add.Type, add.EntityId, (int)add.SlotId);
                            break;
                        case InventoryRemoveItemPacket remove:
                            Take(remove.InventoryType, remove.EntityId);
                            break;
                    }
            }

            internal ulong At(InventoryType type, int slot) => Of(type).BySlot.TryGetValue(slot, out var entityId) ? entityId : 0;

            internal int? Where(InventoryType type, ulong entityId) => Of(type).ByEntity.TryGetValue(entityId, out var slot) ? slot : null;

            /// <summary>The items in the container, counted both ways: a stale entry makes the two disagree.</summary>
            internal int Count(InventoryType type)
            {
                var (bySlot, byEntity) = Of(type);

                Assert.AreEqual(bySlot.Count, byEntity.Count, $"{type}'s two maps disagree");
                return bySlot.Count;
            }

            internal static void AssertRemovalsFirst(List<Rasa.Packets.PythonPacket> packets)
            {
                var lastRemoval = packets.FindLastIndex(packet => packet is InventoryRemoveItemPacket);
                var firstAdd = packets.FindIndex(packet => packet is InventoryAddItemPacket);

                Assert.IsTrue(lastRemoval < 0 || firstAdd < 0 || lastRemoval < firstAdd, "every removal before any addition");
            }
        }

        #endregion

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

            /// <summary>The lists built again from the rows, as at a login.</summary>
            internal void Login()
            {
                Sent();
                Inventory.InitCharacterInventory(Client);
            }

            /// <summary>The item in that pack slot.</summary>
            internal Item Loaded(int slot) => EntityManager.Instance.GetItem(Pack[slot]);

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
