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
    using Rasa.Packets.Communicator.Server;
    using Rasa.Packets.Game.Server;
    using Rasa.Packets.Inventory.Client;
    using Rasa.Packets.Inventory.Server;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Structures;
    using Rasa.Structures.Char;
    using Rasa.Test.Missions;

    // A right-click on a stack in the clan lockbox: clanlockboxwindow.py OnIconRightClicked calls
    // inventory.AddItemToPersonalInventory(entityId, None, quantity), which sends
    // "ClanLockbox_WithdrawItem", (srcSlot, None, quantity). With no slot named the server puts
    // the stack where it fits: into the stacks of the same item the taker carries, and what is
    // left over into a free slot.
    [TestClass]
    [DoNotParallelize]
    public class ClanLockboxWithdrawTests
    {
        private const uint Template = 28, OtherTemplate = 29, ItemClass = 3147;

        // What MissionTestContext.AddRewardTemplate gives the class, and where category 2 starts.
        private const uint FullStack = 50000;
        private const int FirstPackSlot = 50, PackSlots = 50;

        [TestMethod]
        public void AStackThatFitsWhollyIntoACarriedOneIsGoneForEveryMember()
        {
            using var lockbox = new Lockbox();
            var stored = lockbox.Deposit(3);
            var carried = lockbox.Carry(2);

            lockbox.RightClick();

            Assert.AreEqual(5u, carried.StackSize);
            Assert.IsNull(EntityManager.Instance.GetItem(stored.EntityId), "merged away");
            Assert.AreEqual(0UL, lockbox.Taker.Player.Inventory.ClanInventory[0]);
            Assert.AreEqual(0UL, lockbox.Other.Player.Inventory.ClanInventory[0]);

            using (var unit = lockbox.Context.CreateChar())
            {
                Assert.AreEqual(5u, unit.Items.GetItem(carried.Id).StackSize);
                Assert.IsNull(unit.Items.GetItem(stored.Id));
                Assert.AreEqual(0, unit.ClanInventories.GetItems(lockbox.Clan.Id).Count);
            }

            // The taker is told of the merge.
            var taker = Sent(lockbox.Taker);

            Assert.AreEqual(5u, taker.Where(m => m.EntityId == carried.EntityId).Select(m => m.Packet).OfType<SetStackCountPacket>().Single().StackSize);
            Assert.AreEqual(stored.EntityId, taker.Select(m => m.Packet).OfType<DestroyPhysicalEntityPacket>().Single().EntityId);
            Assert.AreEqual(0UL, taker.Select(m => m.Packet).OfType<InventoryReloadPacket>().Single().Items[0]);

            // The other member held the stack too: out of the slot, then gone, then the lockbox
            // as it now is. Nothing is sent to an entity that no longer exists.
            var other = Sent(lockbox.Other);
            var removed = other.FindIndex(m => m.Packet is InventoryRemoveItemPacket remove
                && remove.InventoryType == InventoryType.ClanInventory && remove.EntityId == stored.EntityId);
            var destroyed = other.FindIndex(m => m.EntityId == (ulong)SysEntity.ClientMethodId
                && m.Packet is DestroyPhysicalEntityPacket destroy && destroy.EntityId == stored.EntityId);
            var reloaded = other.FindIndex(m => m.Packet is InventoryReloadPacket reload
                && reload.InventoryType == InventoryType.ClanInventory && reload.Items[0] == 0);

            Assert.IsTrue(removed >= 0, "InventoryRemoveItem");
            Assert.IsTrue(destroyed > removed, "DestroyPhysicalEntity, after it left the slot");
            Assert.IsTrue(reloaded > destroyed, "InventoryReload");
            Assert.IsFalse(other.Any(m => m.EntityId == stored.EntityId), "a call on the entity that is gone");
            Assert.IsFalse(other.Any(m => m.Packet is CreatePhysicalEntityPacket));
        }

        [TestMethod]
        public void AStackThatOnlyPartlyFitsACarriedOneBringsTheRestToThePack()
        {
            using var lockbox = new Lockbox();
            var stored = lockbox.Deposit(3);
            var carried = lockbox.Carry(FullStack - 1);

            lockbox.RightClick();

            Assert.AreEqual(FullStack, carried.StackSize);
            Assert.AreEqual(2u, stored.StackSize);
            Assert.AreSame(stored, EntityManager.Instance.GetItem(stored.EntityId));
            Assert.AreEqual(stored.EntityId, lockbox.Taker.Player.Inventory.PersonalInventory[FirstPackSlot + 1]);
            Assert.AreEqual(0UL, lockbox.Taker.Player.Inventory.ClanInventory[0]);
            Assert.AreEqual(0UL, lockbox.Other.Player.Inventory.ClanInventory[0]);

            using (var unit = lockbox.Context.CreateChar())
            {
                Assert.AreEqual(2u, unit.Items.GetItem(stored.Id).StackSize);
                Assert.AreEqual(0, unit.ClanInventories.GetItems(lockbox.Clan.Id).Count);
            }

            var other = Sent(lockbox.Other);

            Assert.IsTrue(other.Any(m => m.Packet is InventoryRemoveItemPacket remove && remove.EntityId == stored.EntityId));
            Assert.AreEqual(0UL, other.Select(m => m.Packet).OfType<InventoryReloadPacket>().Single().Items[0]);
            Assert.IsFalse(other.Any(m => m.Packet is DestroyPhysicalEntityPacket), "the stack is in the taker's pack");
        }

        [TestMethod]
        public void WithNoSlotForTheRestTheStackStaysInTheLockboxAtItsNewSize()
        {
            using var lockbox = new Lockbox();
            var stored = lockbox.Deposit(3);
            var carried = lockbox.Carry(FullStack - 1);

            lockbox.FillThePack();
            lockbox.RightClick();

            Assert.AreEqual(FullStack, carried.StackSize);
            Assert.AreEqual(2u, stored.StackSize);
            Assert.AreEqual(stored.EntityId, lockbox.Taker.Player.Inventory.ClanInventory[0]);
            Assert.AreEqual(stored.EntityId, lockbox.Other.Player.Inventory.ClanInventory[0]);

            using (var unit = lockbox.Context.CreateChar())
            {
                Assert.AreEqual(2u, unit.Items.GetItem(stored.Id).StackSize);
                Assert.AreEqual(stored.Id, unit.ClanInventories.GetItems(lockbox.Clan.Id).Single().ItemId);
            }

            Assert.AreEqual(PlayerMessage.PmInventoryFull,
                Sent(lockbox.Taker).Select(m => m.Packet).OfType<DisplayClientMessagePacket>().Single().MsgId);

            // The other member sees what is left of it.
            var other = Sent(lockbox.Other);

            Assert.AreEqual(2u, other.Where(m => m.EntityId == stored.EntityId).Select(m => m.Packet).OfType<SetStackCountPacket>().Single().StackSize);
            Assert.AreEqual(stored.EntityId, other.Select(m => m.Packet).OfType<InventoryReloadPacket>().Single().Items[0]);
            Assert.IsFalse(other.Any(m => m.Packet is DestroyPhysicalEntityPacket || m.Packet is InventoryRemoveItemPacket));
        }

        [TestMethod]
        public void AStackWithNoneOfItsKindCarriedGoesToAFreePackSlot()
        {
            using var lockbox = new Lockbox();
            var stored = lockbox.Deposit(3);

            lockbox.RightClick();

            Assert.AreEqual(3u, stored.StackSize);
            Assert.AreEqual(stored.EntityId, lockbox.Taker.Player.Inventory.PersonalInventory[FirstPackSlot]);
            Assert.AreEqual(0UL, lockbox.Taker.Player.Inventory.ClanInventory[0]);
            Assert.AreEqual(0UL, lockbox.Other.Player.Inventory.ClanInventory[0]);

            using (var unit = lockbox.Context.CreateChar())
                Assert.AreEqual(0, unit.ClanInventories.GetItems(lockbox.Clan.Id).Count);

            var other = Sent(lockbox.Other);

            Assert.IsTrue(other.Any(m => m.Packet is InventoryRemoveItemPacket remove && remove.EntityId == stored.EntityId));
            Assert.AreEqual(0UL, other.Select(m => m.Packet).OfType<InventoryReloadPacket>().Single().Items[0]);
            Assert.IsFalse(other.Any(m => m.Packet is DestroyPhysicalEntityPacket), "the stack is in the taker's pack");
        }

        [TestMethod]
        public void ARefreshThatNamesNoItemSendsTheLockboxAndNothingElse()
        {
            using var lockbox = new Lockbox();

            lockbox.Inventory.RefreshClanLockbox(lockbox.Clan.Id, 0, lockbox.Taker.Player.Id, 0, ref lockbox.Taker.Player.Inventory.ClanInventory, true);

            Assert.IsInstanceOfType(Sent(lockbox.Other).Single().Packet, typeof(InventoryReloadPacket));
        }

        private static List<CallMethodMessage> Sent(Client client) => WorldTestContext.Drain(client)
            .Select(packet => packet.Message).OfType<CallMethodMessage>().ToList();

        /// <summary>A clan of two, both in the world with the lockbox open to them: the leader, who takes, and a member.</summary>
        private sealed class Lockbox : IDisposable
        {
            private readonly ClanManager _clans = ClanManager.Instance;
            private readonly System.Collections.Concurrent.ConcurrentDictionary<uint, Lazy<ClanEntry>> _previousClans;
            private readonly System.Collections.Concurrent.ConcurrentDictionary<uint, Lazy<List<ClanMemberEntry>>> _previousMembers;

            internal MissionTestContext Context { get; } = MissionTestContext.WithCompletableMission(429);
            internal InventoryManager Inventory { get; }
            internal ClanEntry Clan { get; }
            internal Client Taker { get; }
            internal Client Other { get; }

            internal Lockbox()
            {
                _previousClans = _clans.Clans;
                _previousMembers = _clans.ClanMembers;

                Inventory = new InventoryManager(Context, Context.Manager);
                Taker = Context.Client;
                Other = Context.CreateAdditionalClient(2);

                // The leader's own membership and the caches, then the second member.
                Clan = Context.CreateClanForPlayer();

                using (var unit = Context.CreateChar())
                    Assert.IsTrue(unit.ClanMembers.InsertClanMemberData(Clan.Id, Other.Player.Id, ClanRank.Member, ""));

                _clans.ClanMembers[Clan.Id].Value.Add(new ClanMemberEntry { ClanId = Clan.Id, CharacterId = Other.Player.Id, Rank = ClanRank.Member, Note = "" });

                Other.Player.ClanId = Clan.Id;

                foreach (var client in new[] { Taker, Other })
                {
                    client.Player.Inventory.ResetClanInventory();

                    lock (Server.Clients)
                        Server.Clients.Add(client);
                }
            }

            /// <summary>A stack the leader puts in the first lockbox slot, as every member then holds it.</summary>
            internal Item Deposit(uint quantity)
            {
                var item = Carry(quantity);

                Inventory.ClanLockbox_DepositItemInSlot(Taker, new ClanLockbox_DepositItemInSlotPacket { SrcSlot = item.OwnerSlotId, DestSlot = 0, Quantity = quantity });

                Assert.AreEqual(item.EntityId, Taker.Player.Inventory.ClanInventory[0]);
                Assert.AreEqual(item.EntityId, Other.Player.Inventory.ClanInventory[0]);
                Assert.AreEqual(0UL, Taker.Player.Inventory.PersonalInventory[FirstPackSlot]);

                Drain();

                return item;
            }

            /// <summary>A stack in the leader's pack.</summary>
            internal Item Carry(uint quantity)
            {
                var item = Context.CreateInventoryItem(Template, ItemClass, quantity);

                Assert.AreSame(item, Inventory.GrantItemToInventory(Taker, item));
                Assert.AreEqual(item.EntityId, Taker.Player.Inventory.PersonalInventory[FirstPackSlot]);

                Drain();

                return item;
            }

            /// <summary>Something else in every slot of the category that is still free.</summary>
            internal void FillThePack()
            {
                var pack = Taker.Player.Inventory.PersonalInventory;

                for (var slot = FirstPackSlot; slot < FirstPackSlot + PackSlots; slot++)
                {
                    if (pack[slot] != 0)
                        continue;

                    var item = Context.CreateInventoryItem(OtherTemplate, ItemClass, 1);

                    item.OwnerSlotId = (uint)slot;
                    pack[slot] = item.EntityId;
                }
            }

            /// <summary>The first lockbox slot, right-clicked by the leader.</summary>
            internal void RightClick()
            {
                Inventory.ClanLockbox_WithdrawItem(Taker, new ClanLockbox_WithdrawItemPacket { SrcSlot = 0, Quantity = 3, ManagePersonalSlot = true });
            }

            private void Drain()
            {
                WorldTestContext.Drain(Taker);
                WorldTestContext.Drain(Other);
            }

            public void Dispose()
            {
                lock (Server.Clients)
                {
                    Server.Clients.Remove(Taker);
                    Server.Clients.Remove(Other);
                }

                _clans.Clans = _previousClans;
                _clans.ClanMembers = _previousMembers;
                Taker.Player.ClanId = 0;
                Context.Dispose();
            }
        }
    }
}
