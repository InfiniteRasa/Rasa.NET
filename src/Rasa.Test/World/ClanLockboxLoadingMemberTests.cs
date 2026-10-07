extern alias RasaGame;

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Packets.Clan.Server;
    using Rasa.Packets.Inventory.Client;
    using Rasa.Packets.Inventory.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Structures;
    using Rasa.Structures.Char;
    using Rasa.Test.Missions;
    using ClientState = RasaGame::Rasa.Data.ClientState;

    // Each member's connection has its own copy of the clan lockbox: a list of five hundred
    // slots, built from the clan's rows when the member arrives in the world
    // (MapChannelManager.MapLoaded, InventoryManager.InitClanInventory). A change one member
    // makes is made in every other member's copy as well.
    //
    // A member who has chosen a character and is still on the loading screen is on the
    // connection list with that character's id (CharacterManager sets client.Player there) and
    // a lockbox list with no slots at all. They are also on the clan's cached roster if this is
    // their first login since the server started: ClansInit caches every clan's whole roster,
    // and ClanManager.RemovePlayer takes a member off it only when they log out.
    [TestClass]
    [DoNotParallelize]
    public class ClanLockboxLoadingMemberTests
    {
        private const uint Ammo = 9301, Medkit = 9302, ItemClass = 3147;
        private const int PackSlot = 50;

        [TestMethod]
        [DataRow(false, DisplayName = "onto a pack slot")]
        [DataRow(true, DisplayName = "right-click")]
        public void AWithdrawGoesThroughWhileAMemberIsLoadingIn(bool rightClick)
        {
            using var fixture = new Fixture();
            var stored = fixture.InLockbox(Ammo, 3, 0);

            fixture.Inventory.ClanLockbox_WithdrawItem(fixture.Taker, new ClanLockbox_WithdrawItemPacket
            {
                SrcSlot = 0,
                DestSlot = rightClick ? 0u : PackSlot,
                ManagePersonalSlot = rightClick,
                Quantity = 3
            });

            Assert.AreEqual(stored.EntityId, fixture.Taker.Player.Inventory.PersonalInventory[PackSlot]);
            fixture.AssertHolders(0, 0);
            fixture.AssertPackRow(stored, PackSlot);
            fixture.AssertNoLockboxRows();

            // The member in the world is told; the one loading in is not, and has nothing changed.
            var other = fixture.Sent(fixture.Other);

            Assert.IsTrue(other.Any(packet => packet is InventoryRemoveItemPacket remove && remove.InventoryType == InventoryType.ClanInventory && remove.EntityId == stored.EntityId));
            Assert.AreEqual(0UL, other.OfType<InventoryReloadPacket>().Single().Items[0]);
            fixture.AssertLoadingMemberUntouched();

            // They are given the lockbox as it now is when they arrive.
            fixture.Arrive();

            Assert.IsTrue(fixture.Loading.Player.Inventory.ClanInventory.All(entityId => entityId == 0));
        }

        [TestMethod]
        public void ADepositOntoALockboxItemGoesThroughWhileAMemberIsLoadingIn()
        {
            using var fixture = new Fixture();
            var stored = fixture.InLockbox(Ammo, 3, 0);
            var carried = fixture.InPack(Medkit, 1, PackSlot);

            fixture.Inventory.ClanLockbox_DepositItemInSlot(fixture.Taker, new ClanLockbox_DepositItemInSlotPacket { SrcSlot = PackSlot, DestSlot = 0, Quantity = 1 });

            Assert.AreEqual(stored.EntityId, fixture.Taker.Player.Inventory.PersonalInventory[PackSlot]);
            fixture.AssertHolders(0, carried.EntityId);
            fixture.AssertPackRow(stored, PackSlot);
            fixture.AssertLockboxRow(carried, 0);
            fixture.AssertLoadingMemberUntouched();

            fixture.Arrive();

            Assert.AreEqual(carried.EntityId, fixture.Loading.Player.Inventory.ClanInventory[0]);
            Assert.AreEqual(1, fixture.Loading.Player.Inventory.ClanInventory.Count(entityId => entityId != 0));
        }

        [TestMethod]
        public void AMoveInTheLockboxGoesThroughWhileAMemberIsLoadingIn()
        {
            using var fixture = new Fixture();
            var stored = fixture.InLockbox(Ammo, 3, 0);

            fixture.Inventory.ClanLockbox_MoveItem(fixture.Taker, new ClanLockbox_MoveItemPacket { SrcSlot = 0, DestSlot = 5, Quantity = 3 });

            fixture.AssertHolders(0, 0);
            fixture.AssertHolders(5, stored.EntityId);
            fixture.AssertLockboxRow(stored, 5);
            fixture.AssertLoadingMemberUntouched();

            fixture.Arrive();

            Assert.AreEqual(stored.EntityId, fixture.Loading.Player.Inventory.ClanInventory[5]);
            Assert.AreEqual(1, fixture.Loading.Player.Inventory.ClanInventory.Count(entityId => entityId != 0));
        }

        [TestMethod]
        public void ADestroyInTheLockboxGoesThroughWhileAMemberIsLoadingIn()
        {
            using var fixture = new Fixture();
            var stored = fixture.InLockbox(Ammo, 3, 0);

            fixture.Inventory.ClanLockbox_DestroyItem(fixture.Taker, new ClanLockbox_DestroyItemPacket { EntityId = stored.EntityId, Quantity = 3 });

            fixture.AssertHolders(0, 0);
            fixture.AssertNoLockboxRows();

            using (var unit = fixture.Context.CreateChar())
                Assert.IsNull(unit.Items.GetItem(stored.Id));

            fixture.AssertLoadingMemberUntouched();

            fixture.Arrive();

            Assert.IsTrue(fixture.Loading.Player.Inventory.ClanInventory.All(entityId => entityId == 0));
        }

        [TestMethod]
        public void ADepositThatJoinsAStackGoesThroughWhileAMemberIsLoadingIn()
        {
            using var fixture = new Fixture();
            var stored = fixture.InLockbox(Ammo, 3, 0);
            var carried = fixture.InPack(Ammo, 2, PackSlot);

            fixture.Inventory.ClanLockbox_DepositItemInTab(fixture.Taker, new ClanLockbox_DepositItemInTabPacket { SrcSlot = PackSlot, DestTab = 1, Quantity = 2 });

            Assert.AreEqual(5u, stored.StackSize);
            Assert.AreEqual(0UL, fixture.Taker.Player.Inventory.PersonalInventory[PackSlot]);
            Assert.IsNull(EntityManager.Instance.GetItem(carried.EntityId), "merged away");
            fixture.AssertHolders(0, stored.EntityId);
            Assert.AreEqual(5u, fixture.Sent(fixture.Other).OfType<Rasa.Packets.MapChannel.Server.SetStackCountPacket>().Single().StackSize);
            fixture.AssertLoadingMemberUntouched();

            fixture.Arrive();

            Assert.AreEqual(stored.EntityId, fixture.Loading.Player.Inventory.ClanInventory[0]);
        }

        [TestMethod]
        [DataRow(false, DisplayName = "a withdraw")]
        [DataRow(true, DisplayName = "a deposit onto it")]
        public void AnItemTakenOutHasARowInOneTableOnlyIfTheRestIsCutShort(bool swap)
        {
            // Not a state a connection is ever in: a copy of the lockbox that cannot be changed
            // at all, to stop the handler where it goes through the other members' copies. The
            // pack row of the item that came out is in by then, and its lockbox row must be out.
            using var fixture = new Fixture();
            var stored = fixture.InLockbox(Ammo, 3, 0);

            if (swap)
                fixture.InPack(Medkit, 1, PackSlot);

            fixture.Other.Player.Inventory.ClanInventory = null;

            Assert.ThrowsExactly<NullReferenceException>(() =>
            {
                if (swap)
                    fixture.Inventory.ClanLockbox_DepositItemInSlot(fixture.Taker, new ClanLockbox_DepositItemInSlotPacket { SrcSlot = PackSlot, DestSlot = 0, Quantity = 1 });
                else
                    fixture.Inventory.ClanLockbox_WithdrawItem(fixture.Taker, new ClanLockbox_WithdrawItemPacket { SrcSlot = 0, DestSlot = PackSlot, Quantity = 3 });
            });

            fixture.AssertPackRow(stored, PackSlot);
            fixture.AssertNoLockboxRows();
        }

        [TestMethod]
        [DataRow(true, DisplayName = "its owner in the world")]
        [DataRow(false, DisplayName = "its owner gone")]
        public void ALockboxRowForAnItemSomeoneCarriesIsDroppedWhenTheLockboxLoads(bool ownerInWorld)
        {
            // What a withdraw cut short used to leave: the item's pack row in, its lockbox row
            // not yet out. The pack row is the half that was done, so the item is its owner's.
            using var fixture = new Fixture();
            var taken = fixture.InPack(Ammo, 3, PackSlot);

            using (var unit = fixture.Context.CreateChar())
                unit.ClanInventories.AddInvItem(fixture.Clan.Id, 0, taken.Id);

            // Disconnected, their pack's entities are released (MapChannelManager.CleanupDisconnected).
            if (!ownerInWorld)
            {
                fixture.Taker.Player.Inventory.PersonalInventory[PackSlot] = 0;
                EntityManager.Instance.ReleaseEntity(taken.EntityId, EntityType.Item);
            }

            fixture.Arrive();

            Assert.IsTrue(fixture.Loading.Player.Inventory.ClanInventory.All(entityId => entityId == 0));
            Assert.IsFalse(fixture.Sent(fixture.Loading).Any(packet => packet is InventoryAddItemPacket));
            fixture.AssertPackRow(taken, PackSlot);
            fixture.AssertNoLockboxRows();
        }

        [TestMethod]
        public void ALockboxItemWithNoEntityIsLoadedWithTheLockbox()
        {
            // A lockbox row whose item has a row of its own and nothing registered for it.
            using var fixture = new Fixture();
            var stored = fixture.InLockbox(Ammo, 3, 0);

            fixture.Taker.Player.Inventory.ClanInventory[0] = 0;
            fixture.Other.Player.Inventory.ClanInventory[0] = 0;
            EntityManager.Instance.ReleaseEntity(stored.EntityId, EntityType.Item);

            fixture.Arrive();

            var loaded = EntityManager.Instance.GetItem(fixture.Loading.Player.Inventory.ClanInventory[0]);

            Assert.IsNotNull(loaded);
            Assert.AreEqual(stored.Id, loaded.Id);
            Assert.AreEqual(3u, loaded.StackSize);
            Assert.AreEqual(0u, loaded.OwnerSlotId);
            Assert.IsTrue(fixture.Sent(fixture.Loading).OfType<InventoryAddItemPacket>().Any(packet =>
                packet.Type == InventoryType.ClanInventory && packet.EntityId == loaded.EntityId && packet.SlotId == 0));
            fixture.AssertLockboxRow(stored, 0);

            // The next member to arrive is given the same one.
            fixture.Inventory.InitClanInventory(fixture.Other);

            Assert.AreEqual(loaded.EntityId, fixture.Other.Player.Inventory.ClanInventory[0]);
        }

        /// <summary>
        /// A clan of three, in the order its members are gone through: the leader, who acts; a
        /// member on the loading screen of their login; and a member in the world.
        /// </summary>
        private sealed class Fixture : IDisposable
        {
            private readonly ClanManager _clans = ClanManager.Instance;
            private readonly System.Collections.Concurrent.ConcurrentDictionary<uint, Lazy<ClanEntry>> _previousClans;
            private readonly System.Collections.Concurrent.ConcurrentDictionary<uint, Lazy<List<ClanMemberEntry>>> _previousMembers;

            internal MissionTestContext Context { get; } = MissionTestContext.WithCompletableMission(429);
            internal InventoryManager Inventory { get; }
            internal ClanEntry Clan { get; }
            internal Client Taker { get; }
            internal Client Loading { get; }
            internal Client Other { get; }

            internal Fixture()
            {
                _previousClans = _clans.Clans;
                _previousMembers = _clans.ClanMembers;

                Inventory = new InventoryManager(Context, Context.Manager);
                Taker = Context.Client;
                Loading = Context.CreateAdditionalClient(2);
                Other = Context.CreateAdditionalClient(3);
                Clan = Context.CreateClanForPlayer();

                foreach (var member in new[] { Loading, Other })
                {
                    using (var unit = Context.CreateChar())
                        Assert.IsTrue(unit.ClanMembers.InsertClanMemberData(Clan.Id, member.Player.Id, ClanRank.Member, ""));

                    _clans.ClanMembers[Clan.Id].Value.Add(new ClanMemberEntry { ClanId = Clan.Id, CharacterId = member.Player.Id, Rank = ClanRank.Member, Note = "" });
                    member.Player.ClanId = Clan.Id;
                }

                Taker.Player.Inventory.ResetClanInventory();
                Other.Player.Inventory.ResetClanInventory();

                // As the character is when it has been chosen and the world has not loaded.
                Loading.Player.Inventory.ClanInventory = new List<ulong>();
                Loading.State = ClientState.Loading;

                lock (Server.Clients)
                    Server.Clients.AddRange(new[] { Taker, Loading, Other });

                foreach (var client in new[] { Taker, Loading, Other })
                    WorldTestContext.Drain(client);
            }

            /// <summary>An item in a lockbox slot, as the two members in the world hold it.</summary>
            internal Item InLockbox(uint templateId, uint quantity, int slot)
            {
                var item = Make(templateId, quantity);

                item.OwnerId = 0;
                item.OwnerSlotId = (uint)slot;
                Taker.Player.Inventory.ClanInventory[slot] = item.EntityId;
                Other.Player.Inventory.ClanInventory[slot] = item.EntityId;

                using var unit = Context.CreateChar();
                unit.ClanInventories.AddInvItem(Clan.Id, (uint)slot, item.Id);

                return item;
            }

            /// <summary>An item in the leader's pack.</summary>
            internal Item InPack(uint templateId, uint quantity, int slot)
            {
                var item = Make(templateId, quantity);

                item.OwnerId = Taker.Player.Id;
                item.OwnerSlotId = (uint)slot;
                Taker.Player.Inventory.PersonalInventory[slot] = item.EntityId;

                using var unit = Context.CreateChar();
                unit.CharacterInventories.AddInvItem(Taker.AccountEntry.Id, Taker.Player.Id, (uint)InventoryType.Personal, (uint)slot, item.Id);

                return item;
            }

            /// <summary>The loading member's world has loaded: MapLoaded builds their lockbox list.</summary>
            internal void Arrive()
            {
                Loading.State = ClientState.Ingame;
                Inventory.InitClanInventory(Loading);
            }

            internal List<Rasa.Packets.PythonPacket> Sent(Client client) => WorldTestContext.Drain(client)
                .Select(packet => packet.Message).OfType<CallMethodMessage>().Select(message => message.Packet).ToList();

            /// <summary>What both members in the world have in that lockbox slot.</summary>
            internal void AssertHolders(int slot, ulong entityId)
            {
                Assert.AreEqual(entityId, Taker.Player.Inventory.ClanInventory[slot], "the leader's list");
                Assert.AreEqual(entityId, Other.Player.Inventory.ClanInventory[slot], "the list of the member in the world");
            }

            /// <summary>
            /// Nothing of the lockbox's contents is sent to the member who has no copy of it. The
            /// line for its history is a clan message, sent as the roster's are.
            /// </summary>
            internal void AssertLoadingMemberUntouched()
            {
                Assert.AreEqual(0, Loading.Player.Inventory.ClanInventory.Count);

                var sent = Sent(Loading).Where(packet => packet is not ClanLockboxLogsPacket).ToArray();

                Assert.AreEqual(0, sent.Length, "sent to a member who has no lockbox yet: " + string.Join(", ", sent.Select(packet => packet.GetType().Name)));
            }

            internal void AssertPackRow(Item item, int slot)
            {
                using var unit = Context.CreateChar();
                var row = unit.CharacterInventories.FindByItemId(item.Id);

                Assert.IsNotNull(row);
                Assert.AreEqual((uint)InventoryType.Personal, row.InventoryType);
                Assert.AreEqual(Taker.Player.Id, row.CharacterId);
                Assert.AreEqual((uint)slot, row.SlotId);
                Assert.AreEqual(0, unit.ClanInventories.GetItems(Clan.Id).Count(entry => entry.ItemId == item.Id), "still a lockbox row");
            }

            internal void AssertLockboxRow(Item item, int slot)
            {
                using var unit = Context.CreateChar();

                Assert.AreEqual((uint)slot, unit.ClanInventories.GetItems(Clan.Id).Single(entry => entry.ItemId == item.Id).SlotId);
                Assert.IsNull(unit.CharacterInventories.FindByItemId(item.Id));
            }

            internal void AssertNoLockboxRows()
            {
                using var unit = Context.CreateChar();

                Assert.AreEqual(0, unit.ClanInventories.GetItems(Clan.Id).Count);
            }

            internal Item Make(uint templateId, uint quantity)
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
                    foreach (var client in new[] { Taker, Loading, Other })
                        Server.Clients.Remove(client);

                _clans.Clans = _previousClans;
                _clans.ClanMembers = _previousMembers;
                Taker.Player.ClanId = 0;
                Context.Dispose();
            }
        }
    }
}
