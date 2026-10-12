using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Memory;
    using Rasa.Packets;
    using Rasa.Packets.Clan.Client;
    using Rasa.Packets.Communicator.Server;
    using Rasa.Packets.Inventory.Client;
    using Rasa.Packets.Inventory.Server;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Wargame.Server;
    using Rasa.Structures;
    using Rasa.Structures.Char;
    using Rasa.Test.Missions;

    // Item wagering (InventoryManager.Wager): the wager slot of the prestige window, what may go in
    // it, the lock that entering combat puts on it, and what the losers of a clan feud forfeit.
    [TestClass]
    [DoNotParallelize]
    public class WagerTests
    {
        private const uint GearClass = 990101;
        private const uint StackClass = 990102;

        private static readonly FieldInfo Singleton = typeof(InventoryManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);

        private InventoryManager _singleton;
        private readonly List<Client> _online = new List<Client>();

        [TestInitialize]
        public void KeepTheSingleton()
        {
            _singleton = (InventoryManager)Singleton.GetValue(null);
        }

        [TestCleanup]
        public void Restore()
        {
            Singleton.SetValue(null, _singleton);

            lock (Server.Clients)
                foreach (var client in _online)
                    Server.Clients.Remove(client);

            _online.Clear();
        }

        #region The slot

        [TestMethod]
        public void TheClientsCallsAreReadAsItSendsThem()
        {
            Assert.AreEqual(3u, Read(new WagerItemPacket(), w => { w.WriteTuple(1); w.WriteInt(3); }).Slot);
            Assert.AreEqual(200u, Read(new WagerItemPacket(), w => { w.WriteTuple(1); w.WriteInt(200); }).Slot);
            Assert.IsNull(Read(new WagerItemPacket(), w => { w.WriteTuple(1); w.WriteNoneStruct(); }).Slot, "dropped on a tab that is not the item's");
            Assert.IsNull(Read(new WagerItemPacket(), w => { w.WriteTuple(1); w.WriteInt(-1); }).Slot);

            Read(new RemoveWageredItemPacket(), w => w.WriteTuple(0));

            Assert.AreEqual(GameOpcode.WagerItem, new WagerItemPacket().Opcode);
            Assert.AreEqual(GameOpcode.RemoveWageredItem, new RemoveWageredItemPacket().Opcode);
            Assert.AreEqual(typeof(WagerItemPacket), Client.GetPacketType(GameOpcode.WagerItem));
            Assert.AreEqual(typeof(RemoveWageredItemPacket), Client.GetPacketType(GameOpcode.RemoveWageredItem));

            // What goes back: one entity id each, and nothing for the reset and the notice.
            Assert.IsTrue(MissionTestContext.Encode(new AddWagerItemPacket(77)).Length > 0);
            Assert.IsTrue(MissionTestContext.Encode(new RemoveWagerItemPacket(77)).Length > 0);
            Assert.AreEqual(MissionTestContext.Encode(new ResetWagerInventoryPacket()).Length, MissionTestContext.Encode(new WageredItemOutLeveledPacket()).Length);
        }

        [TestMethod]
        public void AnItemGoesFromTheBackpackToTheWagerSlotAndBack()
        {
            using var context = Context(out var client, out var inventory);
            var item = Gear(context, client, 9001, 3);

            inventory.WagerItem(client, new WagerItemPacket { Slot = 3 });

            Assert.AreEqual(item.EntityId, client.Player.Inventory.WagerItem);
            Assert.AreEqual(0UL, client.Player.Inventory.PersonalInventory[3]);
            AssertRow(context, item, InventoryType.WagerInventory, 0);
            Assert.AreEqual(35, InventoryManager.WagerBonusOf(client.Player), "Rare: Experimental to the client");
            Assert.IsFalse(client.Player.WagerLocked);

            var packets = context.Drain();
            Assert.AreEqual(item.EntityId, packets.OfType<InventoryRemoveItemPacket>().Single(p => p.InventoryType == InventoryType.Personal).EntityId);
            Assert.AreEqual(item.EntityId, packets.OfType<AddWagerItemPacket>().Single().EntityId);

            // The right click: back to the first free slot of its tab.
            inventory.RemoveWageredItem(client);

            Assert.AreEqual(0UL, client.Player.Inventory.WagerItem);
            Assert.AreEqual(item.EntityId, client.Player.Inventory.PersonalInventory[0]);
            AssertRow(context, item, InventoryType.Personal, 0);
            Assert.AreEqual(0, InventoryManager.WagerBonusOf(client.Player));

            packets = context.Drain();
            Assert.AreEqual(item.EntityId, packets.OfType<RemoveWagerItemPacket>().Single().EntityId);
            Assert.AreEqual(0u, packets.OfType<InventoryAddItemPacket>().Single(p => p.Type == InventoryType.Personal).SlotId);

            // Dragged back onto a free slot: the same call, naming where it goes.
            inventory.WagerItem(client, new WagerItemPacket { Slot = 0 });
            inventory.WagerItem(client, new WagerItemPacket { Slot = 7 });

            Assert.AreEqual(0UL, client.Player.Inventory.WagerItem);
            Assert.AreEqual(item.EntityId, client.Player.Inventory.PersonalInventory[7]);
            AssertRow(context, item, InventoryType.Personal, 7);

            // And onto a tab that is not its own: None, wherever it fits.
            inventory.WagerItem(client, new WagerItemPacket { Slot = 7 });
            inventory.WagerItem(client, new WagerItemPacket { Slot = null });

            Assert.AreEqual(item.EntityId, client.Player.Inventory.PersonalInventory[0]);
            AssertRow(context, item, InventoryType.Personal, 0);
        }

        [TestMethod]
        public void WageringOverAnItemSwapsTheTwo()
        {
            using var context = Context(out var client, out var inventory);
            var first = Gear(context, client, 9001, 3);
            var second = Gear(context, client, 9002, 4, LootQuality.Uncommon, level: 7);

            inventory.WagerItem(client, new WagerItemPacket { Slot = 3 });
            inventory.WagerItem(client, new WagerItemPacket { Slot = 4 });

            Assert.AreEqual(second.EntityId, client.Player.Inventory.WagerItem, "not locked: any wagerable item may take its place");
            Assert.AreEqual(first.EntityId, client.Player.Inventory.PersonalInventory[4]);
            AssertRow(context, second, InventoryType.WagerInventory, 0);
            AssertRow(context, first, InventoryType.Personal, 4);
            Assert.AreEqual(20, InventoryManager.WagerBonusOf(client.Player));
        }

        [TestMethod]
        public void WhatMayNotBeWageredIsRefusedWithTheClientsMessage()
        {
            using var context = Context(out var client, out var inventory);
            uint next = 0;

            DisplayClientMessagePacket Refused(Item item)
            {
                context.Drain();
                inventory.WagerItem(client, new WagerItemPacket { Slot = item.OwnerSlotId });

                Assert.AreEqual(0UL, client.Player.Inventory.WagerItem);
                Assert.AreEqual(item.EntityId, client.Player.Inventory.PersonalInventory[(int)item.OwnerSlotId]);
                AssertRow(context, item, InventoryType.Personal, item.OwnerSlotId);

                return context.Drain().OfType<DisplayClientMessagePacket>().Single();
            }

            Item Next(LootQuality quality = LootQuality.Rare, int level = 10, uint classId = GearClass, Action<ItemTemplate> shape = null)
            {
                next++;
                return Gear(context, client, 9100 + next, next, quality, level, classId, shape);
            }

            Assert.AreEqual(PlayerMessage.PmWagerItemInvalidQuality, Refused(Next(LootQuality.Normal)).MsgId);
            Assert.AreEqual(PlayerMessage.PmWagerItemInvalidQuality, Refused(Next(LootQuality.Legendary)).MsgId);

            var level = Refused(Next(level: 15));
            Assert.AreEqual(PlayerMessage.PmWagerItemInvalidLevel, level.MsgId);
            Assert.AreEqual("4", level.Args["level"]);
            Assert.AreEqual(PlayerMessage.PmWagerItemInvalidLevel, Refused(Next(level: 5)).MsgId);

            Assert.AreEqual(PlayerMessage.PmWagerItemCanNotWagerStackable, Refused(Next(classId: StackClass)).MsgId);
            Assert.AreEqual(PlayerMessage.PmWagerItemCanNotWagerNonTradable, Refused(Next(shape: t => t.ItemInfo.Tradable = true)).MsgId);
            Assert.AreEqual(PlayerMessage.PmWagerItemCanNotWagerNonEquipable, Refused(Next(shape: t => t.EquipableInfo = null)).MsgId);
            Assert.AreEqual(PlayerMessage.PmWagerItemCanNotWagerNonEquipable, Refused(Next(shape: t => t.InventoryCategory = InventoryCategory.Misc)).MsgId);

            var bound = Next();
            bound.BoundCharacterId = client.Player.Id;
            Assert.AreEqual(PlayerMessage.PmWagerItemCanNotWagerCharacterBound, Refused(bound).MsgId);

            var broken = Next();
            broken.CurrentHitPoints = 0;
            Assert.AreEqual(PlayerMessage.PmWagerItemNeedsRepair, Refused(broken).MsgId);

            var good = Next(level: 14);

            client.Player.InCombat = true;
            Assert.AreEqual(PlayerMessage.PmWagerItemCanNotWagerInCombat, Refused(good).MsgId);
            client.Player.InCombat = false;

            client.Player.State = CharacterState.Dead;
            Assert.AreEqual(PlayerMessage.PmCannotDoThatWhileDead, Refused(good).MsgId);
            client.Player.State = CharacterState.Idle;

            // Four levels either way is in range.
            inventory.WagerItem(client, new WagerItemPacket { Slot = good.OwnerSlotId });
            Assert.AreEqual(good.EntityId, client.Player.Inventory.WagerItem);

            var low = Next(level: 6);
            inventory.WagerItem(client, new WagerItemPacket { Slot = low.OwnerSlotId });
            Assert.AreEqual(low.EntityId, client.Player.Inventory.WagerItem);
        }

        [TestMethod]
        public void TheSlotIsShownAgainOnEveryArrival()
        {
            using var context = Context(out var client, out var inventory);
            var item = Gear(context, client, 9001, 3);

            inventory.WagerItem(client, new WagerItemPacket { Slot = 3 });
            context.Drain();

            inventory.ShowWager(client, true);

            var packets = context.Drain();
            var reset = packets.FindIndex(p => p is ResetWagerInventoryPacket);
            var add = packets.FindIndex(p => p is AddWagerItemPacket shown && shown.EntityId == item.EntityId);
            Assert.IsTrue(reset >= 0 && add > reset, "emptied, then the item");

            // An entity that is gone leaves an empty slot rather than a dangling id.
            EntityManager.Instance.ReleaseEntity(item.EntityId, EntityType.Item);
            inventory.ShowWager(client, true);

            Assert.AreEqual(0UL, client.Player.Inventory.WagerItem);
            Assert.IsFalse(context.Drain().OfType<AddWagerItemPacket>().Any());
        }

        [TestMethod]
        public void TheWageredItemIsLoadedWithTheCharacter()
        {
            using var context = Context(out var client, out var inventory);
            var item = Gear(context, client, 9001, 3);

            inventory.WagerItem(client, new WagerItemPacket { Slot = 3 });
            context.Drain();

            // A login: the lists are built again from the rows.
            inventory.InitCharacterInventory(client);

            var loaded = InventoryManager.WageredItemOf(client.Player);
            Assert.IsNotNull(loaded);
            Assert.AreEqual(item.Id, loaded.Id);
            Assert.IsFalse(client.Player.Inventory.PersonalInventory.Contains(loaded.EntityId));
            Assert.AreEqual(loaded.EntityId, context.Drain().OfType<AddWagerItemPacket>().Single().EntityId);
            Assert.IsTrue(inventory.HoldsTemplate(client.Player, 9001), "a wagered item is still theirs");
        }

        #endregion

        #region The lock

        [TestMethod]
        public void EnteringCombatLocksTheItemInItsSlot()
        {
            using var context = Context(out var client, out var inventory);
            var bystander = context.CreateAdditionalClient(2);
            var item = Gear(context, client, 9001, 3);

            Singleton.SetValue(null, inventory);
            WithVitals(client);

            // Nothing wagered: nothing to lock.
            new ManifestationManager(context).EnterCombat(client);
            Assert.IsFalse(client.Player.WagerLocked);
            new ManifestationManager(context).ExitCombat(client);

            inventory.WagerItem(client, new WagerItemPacket { Slot = 3 });
            context.Drain();
            MissionTestContext.Drain(bystander);

            new ManifestationManager(context).EnterCombat(client);

            Assert.IsTrue(client.Player.WagerLocked);
            Assert.IsTrue(Stored(context, 1).WagerLocked, "kept on the character: a relog is no way out");

            var effect = client.Player.ActiveEffects.Values.Single(e => e.TypeId == InventoryManager.LockWageredItemEffectTypeId);
            Assert.IsTrue(effect.OwnerOnly);
            Assert.IsFalse(effect.HasDuration);
            Assert.AreEqual(1, context.Drain().OfType<GameEffectAttachedPacket>().Count(p => p.EffectTypeId == InventoryManager.LockWageredItemEffectTypeId));
            Assert.IsFalse(MissionTestContext.Drain(bystander).OfType<GameEffectAttachedPacket>().Any(p => p.EffectTypeId == InventoryManager.LockWageredItemEffectTypeId),
                "the padlock is the wagerer's own");
            Assert.IsFalse(GameEffectManager.EffectsForNewcomer(client.Player, bystander.Player).Any(p => p.EffectTypeId == InventoryManager.LockWageredItemEffectTypeId));

            // Out of combat it stays locked.
            new ManifestationManager(context).ExitCombat(client);
            context.Drain();

            inventory.RemoveWageredItem(client);
            Assert.AreEqual(item.EntityId, client.Player.Inventory.WagerItem);
            Assert.AreEqual(PlayerMessage.PmWagerItemCanNotBeRemovedInCombat, context.Drain().OfType<DisplayClientMessagePacket>().Single().MsgId);

            inventory.WagerItem(client, new WagerItemPacket { Slot = 9 });
            Assert.AreEqual(item.EntityId, client.Player.Inventory.WagerItem, "nor dragged out");

            // A map change or a death takes every effect off; an arrival and a revive put this one back.
            GameEffectManager.Instance.ClearEffects(context.Map, client.Player);
            InventoryManager.SyncWagerLock(client.Player);
            Assert.AreEqual(1, client.Player.ActiveEffects.Values.Count(e => e.TypeId == InventoryManager.LockWageredItemEffectTypeId));

            InventoryManager.SyncWagerLock(client.Player);
            Assert.AreEqual(1, client.Player.ActiveEffects.Values.Count(e => e.TypeId == InventoryManager.LockWageredItemEffectTypeId), "once");
        }

        [TestMethod]
        public void ALockedItemGivesWayOnlyToABetterOne()
        {
            using var context = Context(out var client, out var inventory);
            var wagered = Gear(context, client, 9001, 0, LootQuality.Rare, level: 10);
            var lesser = Gear(context, client, 9002, 1, LootQuality.Uncommon, level: 12);
            var lower = Gear(context, client, 9003, 2, LootQuality.Rare, level: 9);
            var equal = Gear(context, client, 9004, 3, LootQuality.Rare, level: 10);
            var better = Gear(context, client, 9005, 4, LootQuality.Epic, level: 8);

            inventory.WagerItem(client, new WagerItemPacket { Slot = 0 });
            inventory.SetWagerLocked(client, true);
            context.Drain();

            inventory.WagerItem(client, new WagerItemPacket { Slot = 1 });
            Assert.AreEqual(wagered.EntityId, client.Player.Inventory.WagerItem);
            Assert.AreEqual(PlayerMessage.PmWagerItemCanNotBeReplacedByLowerQuality, context.Drain().OfType<DisplayClientMessagePacket>().Single().MsgId);

            inventory.WagerItem(client, new WagerItemPacket { Slot = 2 });
            Assert.AreEqual(wagered.EntityId, client.Player.Inventory.WagerItem);
            Assert.AreEqual(PlayerMessage.PmWagerItemCanNotBeReplacedByLowerLevel, context.Drain().OfType<DisplayClientMessagePacket>().Single().MsgId);

            inventory.WagerItem(client, new WagerItemPacket { Slot = 3 });
            Assert.AreEqual(equal.EntityId, client.Player.Inventory.WagerItem, "the same quality and level");
            Assert.AreEqual(wagered.EntityId, client.Player.Inventory.PersonalInventory[3]);

            inventory.WagerItem(client, new WagerItemPacket { Slot = 4 });
            Assert.AreEqual(better.EntityId, client.Player.Inventory.WagerItem, "a higher quality, whatever its level");
            Assert.AreEqual(equal.EntityId, client.Player.Inventory.PersonalInventory[4]);
            AssertRow(context, better, InventoryType.WagerInventory, 0);
            AssertRow(context, equal, InventoryType.Personal, 4);

            Assert.IsTrue(client.Player.WagerLocked, "the one that takes its place is locked in it");
            Assert.IsTrue(Stored(context, 1).WagerLocked);
            Assert.AreEqual(1, client.Player.ActiveEffects.Values.Count(e => e.TypeId == InventoryManager.LockWageredItemEffectTypeId));
            Assert.AreEqual(lesser.EntityId, client.Player.Inventory.PersonalInventory[1]);
            Assert.AreEqual(lower.EntityId, client.Player.Inventory.PersonalInventory[2]);
        }

        [TestMethod]
        public void OutLevellingTheItemEndsItsBonusAndFreesIt()
        {
            using var context = Context(out var client, out var inventory);
            var item = Gear(context, client, 9001, 0, LootQuality.Epic, level: 10);

            Singleton.SetValue(null, inventory);

            inventory.WagerItem(client, new WagerItemPacket { Slot = 0 });
            inventory.SetWagerLocked(client, true);
            context.Drain();

            client.Player.Level = 14;
            InventoryManager.WagerLevelChanged(client, 10);

            Assert.IsTrue(client.Player.WagerLocked, "four above is in range");
            Assert.AreEqual(50, InventoryManager.WagerBonusOf(client.Player));
            Assert.IsFalse(context.Drain().OfType<WageredItemOutLeveledPacket>().Any());

            client.Player.Level = 15;
            InventoryManager.WagerLevelChanged(client, 14);

            Assert.AreEqual(1, context.Drain().OfType<WageredItemOutLeveledPacket>().Count());
            Assert.IsFalse(client.Player.WagerLocked);
            Assert.IsFalse(Stored(context, 1).WagerLocked);
            Assert.AreEqual(0, client.Player.ActiveEffects.Values.Count(e => e.TypeId == InventoryManager.LockWageredItemEffectTypeId));
            Assert.AreEqual(0, InventoryManager.WagerBonusOf(client.Player));

            client.Player.Level = 16;
            InventoryManager.WagerLevelChanged(client, 15);
            Assert.IsFalse(context.Drain().OfType<WageredItemOutLeveledPacket>().Any(), "told once, on leaving the range");

            // Combat does not lock an item that gives nothing.
            InventoryManager.WagerEnteredCombat(client);
            Assert.IsFalse(client.Player.WagerLocked);

            inventory.RemoveWageredItem(client);
            Assert.AreEqual(0UL, client.Player.Inventory.WagerItem);
            Assert.AreEqual(item.EntityId, client.Player.Inventory.PersonalInventory[0]);
        }

        [TestMethod]
        public void AWageredItemStaysWhileItsOwnerIsFightingPlayers()
        {
            using var context = Context(out var client, out var inventory);
            var enemy = context.CreateAdditionalClient(2);
            var item = Gear(context, client, 9001, 0);
            var now = Pvp.Now;

            try
            {
                long tick = 1_000_000;
                Pvp.Now = () => tick;

                inventory.WagerItem(client, new WagerItemPacket { Slot = 0 });
                Pvp.RecordEngagement(enemy.Player, client.Player);
                context.Drain();

                inventory.RemoveWageredItem(client);
                Assert.AreEqual(item.EntityId, client.Player.Inventory.WagerItem);
                Assert.AreEqual(PlayerMessage.PmWagerItemCanNotBeRemovedDuringWarGame, context.Drain().OfType<DisplayClientMessagePacket>().Single().MsgId);

                tick += CombatRegen.CombatTimeoutMs;
                inventory.RemoveWageredItem(client);
                Assert.AreEqual(0UL, client.Player.Inventory.WagerItem);
            }
            finally
            {
                Pvp.Now = now;
            }
        }

        #endregion

        #region Forfeits

        [TestMethod]
        public void TheLosersOfAFeudForfeitTheirWageredItemsToTheWinnersLockbox()
        {
            using var context = Context(out var loser, out var inventory);
            var winner = context.CreateAdditionalClient(2);
            context.SeedCharacter(3, 0, 3);

            var losers = Clan(context, "Losers", (1, ClanRank.Leader), (3, ClanRank.Member));
            var winners = Clan(context, "Winners", (2, ClanRank.Leader));

            Join(loser, losers);
            Join(winner, winners);

            var online = Gear(context, loser, 9001, 0);
            inventory.WagerItem(loser, new WagerItemPacket { Slot = 0 });
            inventory.SetWagerLocked(loser, true);
            var offline = WageredOffline(context, 3, 3, 9002);
            var kept = Gear(context, winner, 9003, 0);
            inventory.WagerItem(winner, new WagerItemPacket { Slot = 0 });

            context.Drain();
            MissionTestContext.Drain(winner);

            var result = inventory.ForfeitWagers(losers.Id, winners.Id, 2, "Losers", "Winners");

            Assert.AreEqual(2, result.ToLockbox);
            Assert.AreEqual(0, result.Mailed);
            Assert.AreEqual(0, result.Kept);

            // Item by item, for the feud's record.
            CollectionAssert.AreEqual(new uint[] { 1, 3 }, result.Items.Select(i => i.CharacterId).ToArray());
            CollectionAssert.AreEqual(new[] { online.Id, offline }, result.Items.Select(i => i.ItemId).ToArray());
            CollectionAssert.AreEqual(new uint[] { 9001, 9002 }, result.Items.Select(i => i.ItemTemplateId).ToArray());
            Assert.IsTrue(result.Items.All(i => i.Result == (byte)PvpWagerResult.ClanLockbox && i.RecipientClanId == winners.Id && i.RecipientCharacterId == 0));
            Assert.IsTrue(result.Items.All(i => i.QualityId == (int)LootQuality.Rare && i.StackSize == 1));

            using (var unit = context.CreateChar())
            {
                var lockbox = unit.ClanInventories.GetItems(winners.Id).OrderBy(row => row.SlotId).ToList();
                CollectionAssert.AreEqual(new uint[] { 0, 1 }, lockbox.Select(row => row.SlotId).ToArray(), "the lowest free slots");
                CollectionAssert.AreEquivalent(new[] { online.Id, offline }, lockbox.Select(row => row.ItemId).ToArray());
                Assert.IsNull(unit.CharacterInventories.FindByItemId(online.Id));
                Assert.IsNull(unit.CharacterInventories.FindByItemId(offline));
                Assert.IsFalse(unit.Characters.Find(1).WagerLocked);

                var log = unit.ClanLockboxLogs.Get(winners.Id, 10);
                Assert.AreEqual(2, log.Count);
                Assert.IsTrue(log.All(entry => entry.TransactionType == InventoryTransactionType.Deposit));
                CollectionAssert.AreEquivalent(new uint[] { 1, 3 }, log.Select(entry => entry.CharacterId).ToArray(), "deposited by whoever wagered them");
            }

            // The loser in the world: the slot emptied and unlocked, and told.
            Assert.AreEqual(0UL, loser.Player.Inventory.WagerItem);
            Assert.IsFalse(loser.Player.WagerLocked);
            Assert.AreEqual(0, loser.Player.ActiveEffects.Values.Count(e => e.TypeId == InventoryManager.LockWageredItemEffectTypeId));

            var lost = context.Drain();
            Assert.AreEqual(online.EntityId, lost.OfType<RemoveWagerItemPacket>().Single().EntityId);
            Assert.IsTrue(lost.OfType<SystemMessagePacket>().Any(p => p.TextMessage.Contains("forfeit")));

            // The winner in the world: both in their lockbox list, shown, and told. Their own wager is theirs still.
            var lockboxList = winner.Player.Inventory.ClanInventory;
            Assert.AreEqual(online.EntityId, lockboxList[0], "the loser's own entity carries on as the lockbox's");
            Assert.AreEqual(offline, EntityManager.Instance.GetItem(lockboxList[1]).Id);
            Assert.AreEqual(0u, EntityManager.Instance.GetItem(lockboxList[1]).OwnerId);
            Assert.AreEqual(1u, EntityManager.Instance.GetItem(lockboxList[1]).OwnerSlotId);
            Assert.AreEqual(kept.EntityId, winner.Player.Inventory.WagerItem);

            var won = MissionTestContext.Drain(winner).ToList();
            Assert.AreEqual(2, won.OfType<InventoryAddItemPacket>().Count(p => p.Type == InventoryType.ClanInventory));
            Assert.AreEqual(1, won.OfType<InventoryReloadPacket>().Count(p => p.InventoryType == InventoryType.ClanInventory));
            Assert.IsTrue(won.OfType<SystemMessagePacket>().Any(p => p.TextMessage.Contains("2 wagered items") && p.TextMessage.Contains("clan lockbox")));
        }

        [TestMethod]
        public void AForfeitsLockboxHistoryGoesToTheWinnersOnTheLockbox()
        {
            using var context = Context(out var loser, out var inventory);
            var winner = context.CreateAdditionalClient(2);
            var lockbox = new DynamicObject { EntityClassId = EntityClasses.UsableClanLockboxV01, DynamicObjectType = DynamicObjectType.Lockbox };

            var losers = Clan(context, "Losers", (1, ClanRank.Leader));
            var winners = Clan(context, "Winners", (2, ClanRank.Leader));

            Join(loser, losers);
            Join(winner, winners);

            Gear(context, loser, 9001, 0);
            inventory.WagerItem(loser, new WagerItemPacket { Slot = 0 });
            inventory.SetWagerLocked(loser, true);

            EntityManager.Instance.RegisterDynamicObject(lockbox);

            try
            {
                context.Drain();
                WorldTestContext.Drain(winner);

                Assert.AreEqual(1, inventory.ForfeitWagers(losers.Id, winners.Id, 2, "Losers", "Winners").ToLockbox);

                // The client handles the history only on the lockbox's entity (clanlockbox.py).
                var logs = WorldTestContext.Drain(winner).Select(p => p.Message).OfType<Rasa.Packets.Protocol.CallMethodMessage>()
                    .Where(c => c.Packet is Rasa.Packets.Clan.Server.ClanLockboxLogsPacket).ToList();

                Assert.AreEqual(1, logs.Count, "one line, once");
                Assert.AreEqual(lockbox.EntityId, logs[0].EntityId);
                Assert.AreEqual(GameOpcode.UpdateClanLockboxLogs, logs[0].Packet.Opcode);
            }
            finally
            {
                EntityManager.Instance.UnregisterDynamicObject(lockbox.EntityId);
                EntityManager.Instance.FreeEntity(lockbox.EntityId);
            }
        }

        [TestMethod]
        public void ACharacterUniqueItemIsNotMailedToARecipientWhoHoldsOne()
        {
            using var context = Context(out var loser, out var inventory);
            var winner = context.CreateAdditionalClient(2);
            context.SeedCharacter(3, 0, 3);
            context.SeedCharacter(4, 0, 4);

            var losers = Clan(context, "Losers", (1, ClanRank.Leader), (3, ClanRank.Member));
            var winners = Clan(context, "Winners", (2, ClanRank.Leader), (4, ClanRank.Member));

            Join(loser, losers);
            Join(winner, winners);
            FillLockbox(context, winners.Id);

            // Character 4, not in the world, has one in their pack already.
            using (var unit = context.CreateChar())
                unit.CharacterInventories.AddInvItem(4, 4, (uint)InventoryType.Personal, 0, Stored(context, unit, 9070));

            var wagered = Gear(context, loser, 9070, 0, shape: template => template.HasCharacterUniqueFlag = true);
            inventory.WagerItem(loser, new WagerItemPacket { Slot = 0 });
            context.Drain();

            var result = inventory.ForfeitWagers(losers.Id, winners.Id, 4, "Losers", "Winners");

            Assert.AreEqual(0, result.Mailed);
            Assert.AreEqual(1, result.Kept, "left with its owner, as one with nowhere to go is");

            using var check = context.CreateChar();
            Assert.AreEqual((uint)InventoryType.WagerInventory, check.CharacterInventories.FindByItemId(wagered.Id).InventoryType);
            Assert.AreEqual(wagered.EntityId, loser.Player.Inventory.WagerItem);
        }

        [TestMethod]
        public void WithTheLockboxFullTheyAreMailedToTheWinnersCharacterOfTheChallenge()
        {
            using var context = Context(out var loser, out var inventory);
            var winner = context.CreateAdditionalClient(2);
            context.SeedCharacter(3, 0, 3);
            context.SeedCharacter(4, 0, 4);

            var losers = Clan(context, "Losers", (1, ClanRank.Leader), (3, ClanRank.Member));
            var winners = Clan(context, "Winners", (2, ClanRank.Leader), (4, ClanRank.Member));

            Join(loser, losers);
            Join(winner, winners);
            FillLockbox(context, winners.Id);

            var online = Gear(context, loser, 9001, 0);
            inventory.WagerItem(loser, new WagerItemPacket { Slot = 0 });
            var offline = WageredOffline(context, 3, 3, 9002);

            context.Drain();
            MissionTestContext.Drain(winner);

            // Character 4 made the challenge and is not in the world: rows only.
            var result = inventory.ForfeitWagers(losers.Id, winners.Id, 4, "Losers", "Winners");

            Assert.AreEqual(0, result.ToLockbox);
            Assert.AreEqual(2, result.Mailed);
            Assert.AreEqual(4u, result.RecipientCharacterId);
            Assert.AreEqual(2, result.Items.Count);
            Assert.IsTrue(result.Items.All(i => i.Result == (byte)PvpWagerResult.PickUpBox && i.RecipientClanId == winners.Id && i.RecipientCharacterId == 4));

            using (var unit = context.CreateChar())
            {
                Assert.AreEqual(100, unit.ClanInventories.GetItems(winners.Id).Count, "the lockbox is as it was");

                foreach (var itemId in new[] { online.Id, offline })
                {
                    var row = unit.CharacterInventories.FindByItemId(itemId);
                    Assert.AreEqual(4u, row.CharacterId);
                    Assert.AreEqual(4u, row.AccountId);
                    Assert.AreEqual((uint)InventoryType.InboxInventory, row.InventoryType);
                }
            }

            Assert.AreEqual(0UL, loser.Player.Inventory.WagerItem);
            Assert.IsNull(EntityManager.Instance.GetItem(online.EntityId), "nobody in the world holds it now");
            Assert.AreEqual(0, winner.Player.Inventory.InboxItems.Count);
            Assert.IsTrue(MissionTestContext.Drain(winner).OfType<SystemMessagePacket>().Any(p => p.TextMessage.Contains("Character 4's pick-up box")));

            // A character who is not one of the winners: the clan's highest rank instead, here in the world.
            var second = Gear(context, loser, 9003, 1);
            inventory.WagerItem(loser, new WagerItemPacket { Slot = 1 });
            MissionTestContext.Drain(winner);

            result = inventory.ForfeitWagers(losers.Id, winners.Id, 77, "Losers", "Winners");

            Assert.AreEqual(1, result.Mailed);
            Assert.AreEqual(2u, result.RecipientCharacterId);
            Assert.AreEqual(second.EntityId, winner.Player.Inventory.InboxItems.Single());
            Assert.AreEqual(2u, second.OwnerId);

            var mail = MissionTestContext.Drain(winner).ToList();
            Assert.AreEqual(second.EntityId, mail.OfType<AddInboxItemPacket>().Single().EntityId);
            Assert.IsTrue(mail.OfType<SystemMessagePacket>().Any(p => p.TextMessage.Contains("your pick-up box")));

            using (var unit = context.CreateChar())
            {
                var row = unit.CharacterInventories.FindByItemId(second.Id);
                Assert.AreEqual(2u, row.CharacterId);
                Assert.AreEqual((uint)InventoryType.InboxInventory, row.InventoryType);
            }
        }

        [TestMethod]
        public void LeavingOrBeingKickedDuringAFeudSavesNothing()
        {
            using var context = Context(out var leaver, out var inventory);
            var winner = context.CreateAdditionalClient(2);
            var leader = context.CreateAdditionalClient(5);
            context.SeedCharacter(3, 0, 3);

            var losers = Clan(context, "Losers", (5, ClanRank.Leader), (1, ClanRank.Member), (3, ClanRank.Member));
            var winners = Clan(context, "Winners", (2, ClanRank.Leader));

            Join(leaver, losers);
            Join(leader, losers);
            Join(winner, winners);

            var walked = Gear(context, leaver, 9001, 0);
            inventory.WagerItem(leaver, new WagerItemPacket { Slot = 0 });
            inventory.SetWagerLocked(leaver, true);
            var kicked = WageredOffline(context, 3, 3, 9002);

            Assert.IsTrue(inventory.HoldsWager(1), "in the world");
            Assert.IsTrue(inventory.HoldsWager(3), "from their row");
            Assert.IsFalse(inventory.HoldsWager(5));
            Assert.IsFalse(inventory.HoldsWager(0));

            WithLiveClans(context, inventory, new[] { losers, winners }, (clans, feuds) =>
            {
                var feud = feuds.Start(losers, winners, 0, 5, 2);
                Assert.IsNotNull(feud);
                context.Drain();

                // The one in the world walks out; the other is kicked while away.
                clans.LeaveClan(leaver, new LeaveClanPacket { CharacterId = 1, ClanId = losers.Id });
                clans.KickPlayerFromClan(leader, new KickPlayerFromClanPacket(3, losers.Id));

                using (var unit = context.CreateChar())
                    CollectionAssert.AreEqual(new uint[] { 5 }, unit.ClanMembers.GetAllClanMembersByClanId(losers.Id).Select(m => m.CharacterId).ToArray());

                Assert.AreEqual(0u, leaver.Player.ClanId);
                Assert.AreEqual(losers.Id, feud.Departed[1]);
                Assert.AreEqual(losers.Id, feud.Departed[3]);
                Assert.IsTrue(context.Drain().OfType<SystemMessagePacket>().Any(p => p.TextMessage.Contains("at stake")));
                Assert.AreEqual(walked.EntityId, leaver.Player.Inventory.WagerItem, "theirs until the feud is decided");
                Assert.IsTrue(leaver.Player.WagerLocked);

                MissionTestContext.Drain(winner);
                feuds.End(feud, ClanFeuds.Outcome.Won, winners.Id);

                using (var unit = context.CreateChar())
                    CollectionAssert.AreEquivalent(new[] { walked.Id, kicked }, unit.ClanInventories.GetItems(winners.Id).Select(row => row.ItemId).ToArray());

                Assert.AreEqual(0UL, leaver.Player.Inventory.WagerItem);
                Assert.IsFalse(leaver.Player.WagerLocked);

                var lost = context.Drain();
                Assert.AreEqual(walked.EntityId, lost.OfType<RemoveWagerItemPacket>().Single().EntityId);
                Assert.IsTrue(lost.OfType<SystemMessagePacket>().Any(p => p.TextMessage.Contains("which you left")));
                Assert.AreEqual(2, MissionTestContext.Drain(winner).OfType<InventoryAddItemPacket>().Count(p => p.Type == InventoryType.ClanInventory));
            });
        }

        [TestMethod]
        public void ADisbandedClanForfeitsItsWagersToTheClanItWasFighting()
        {
            using var context = Context(out var leader, out var inventory);
            var winner = context.CreateAdditionalClient(2);
            context.SeedCharacter(3, 0, 3);

            var losers = Clan(context, "Losers", (1, ClanRank.Leader), (3, ClanRank.Member));
            var winners = Clan(context, "Winners", (2, ClanRank.Leader));

            Join(leader, losers);
            Join(winner, winners);

            var held = Gear(context, leader, 9001, 0);
            inventory.WagerItem(leader, new WagerItemPacket { Slot = 0 });
            inventory.SetWagerLocked(leader, true);
            var away = WageredOffline(context, 3, 3, 9002);

            WithLiveClans(context, inventory, new[] { losers, winners }, (clans, feuds) =>
            {
                var feud = feuds.Start(losers, winners, 0, 1, 2);
                Assert.IsNotNull(feud);

                // Ahead on kills: disbanding loses the feud all the same.
                feud.ChallengerKills = 3;
                context.Drain();
                MissionTestContext.Drain(winner);

                clans.DisbandClan(leader, new DisbandClanPacket { ClanId = losers.Id });

                Assert.IsFalse(feuds.Feuds.Any(f => f.Id == feud.Id));
                Assert.AreEqual(0u, leader.Player.ClanId);

                using (var unit = context.CreateChar())
                {
                    Assert.IsNull(unit.Clans.GetClanById(losers.Id), "the clan is gone");
                    Assert.AreEqual(0, unit.ClanMembers.GetAllClanMembersByClanId(losers.Id).Count);
                    CollectionAssert.AreEquivalent(new[] { held.Id, away }, unit.ClanInventories.GetItems(winners.Id).Select(row => row.ItemId).ToArray());
                    Assert.IsNull(unit.CharacterInventories.FindByItemId(held.Id));
                    Assert.IsNull(unit.CharacterInventories.FindByItemId(away));
                }

                Assert.AreEqual(0UL, leader.Player.Inventory.WagerItem);
                Assert.IsFalse(leader.Player.WagerLocked);

                var lost = context.Drain();
                Assert.AreEqual(held.EntityId, lost.OfType<RemoveWagerItemPacket>().Single().EntityId);
                Assert.IsTrue(lost.OfType<SystemMessagePacket>().Any(p => p.TextMessage.Contains("forfeit")));
                Assert.IsTrue(lost.OfType<DisplayWargameMessagePacket>().Any(p => p.Message == PlayerMessage.PmWargameFeudYourClanLost));

                var won = MissionTestContext.Drain(winner).ToList();
                Assert.AreEqual(2, won.OfType<InventoryAddItemPacket>().Count(p => p.Type == InventoryType.ClanInventory));
                Assert.IsTrue(won.OfType<DisplayWargameMessagePacket>().Any(p => p.Message == PlayerMessage.PmWargameFeudYourClanWon));
            });
        }

        [TestMethod]
        public void WhoLeftBeforeTheFeudEndedIsForfeitWithTheMembers()
        {
            using var context = Context(out var leaver, out var inventory);
            context.SeedCharacter(4, 0, 4);

            var losers = Clan(context, "Losers", (4, ClanRank.Leader));
            var winners = Clan(context, "Winners");

            // In no clan now, and named as having left the losers.
            var item = Gear(context, leaver, 9001, 0);
            inventory.WagerItem(leaver, new WagerItemPacket { Slot = 0 });
            context.Drain();

            Assert.AreEqual(0, inventory.ForfeitWagers(losers.Id, winners.Id, 0, "Losers", "Winners").Taken, "not a member, and not named");
            Assert.AreEqual(item.EntityId, leaver.Player.Inventory.WagerItem);

            // Named twice, with a character who is gone: once, and nothing for the other.
            var result = inventory.ForfeitWagers(losers.Id, winners.Id, 0, "Losers", "Winners", new uint[] { 1, 1, 0, 404 });

            Assert.AreEqual(1, result.ToLockbox);
            Assert.AreEqual(0, result.Kept);
            Assert.AreEqual(0UL, leaver.Player.Inventory.WagerItem);

            using var unit = context.CreateChar();
            Assert.AreEqual(item.Id, unit.ClanInventories.GetItems(winners.Id).Single().ItemId);
        }

        [TestMethod]
        public void WithNoRoomAnywhereTheItemStaysWithItsOwner()
        {
            using var context = Context(out var loser, out var inventory);
            context.SeedCharacter(4, 0, 4);

            var losers = Clan(context, "Losers", (1, ClanRank.Leader));
            var winners = Clan(context, "Winners", (4, ClanRank.Leader));

            Join(loser, losers);
            FillLockbox(context, winners.Id);

            using (var unit = context.CreateChar())
                for (uint slot = 0; slot < Inventory.MaxInboxItems; slot++)
                    unit.CharacterInventories.AddInvItem(4, 4, (uint)InventoryType.InboxInventory, slot, Stored(context, unit, 9050));

            var item = Gear(context, loser, 9001, 0);
            inventory.WagerItem(loser, new WagerItemPacket { Slot = 0 });
            inventory.SetWagerLocked(loser, true);
            context.Drain();

            var result = inventory.ForfeitWagers(losers.Id, winners.Id, 4, "Losers", "Winners");

            Assert.AreEqual(0, result.Taken);
            Assert.AreEqual(1, result.Kept);
            Assert.AreEqual((item.Id, (byte)PvpWagerResult.NotMoved), (result.Items.Single().ItemId, result.Items.Single().Result), "forfeit, and still where it was");
            Assert.AreEqual((winners.Id, 0u), (result.Items.Single().RecipientClanId, result.Items.Single().RecipientCharacterId));
            Assert.AreEqual(item.EntityId, loser.Player.Inventory.WagerItem);
            Assert.IsTrue(loser.Player.WagerLocked);
            AssertRow(context, item, InventoryType.WagerInventory, 0);
            Assert.IsFalse(context.Drain().OfType<RemoveWagerItemPacket>().Any());

            // Nothing wagered, nothing forfeited; and a clan forfeits nothing to itself.
            Assert.AreEqual(0, inventory.ForfeitWagers(winners.Id, losers.Id, 1, "Winners", "Losers").Taken);
            Assert.AreEqual(0, inventory.ForfeitWagers(winners.Id, losers.Id, 1, "Winners", "Losers").Items.Count);
            Assert.AreEqual(0, inventory.ForfeitWagers(losers.Id, losers.Id, 1, "Losers", "Losers").Kept);
        }

        [TestMethod]
        public void WhatWasWageredInAFeudAndWhatBecameOfItIsOnTheFeudsRecord()
        {
            using var context = Context(out var loser, out var inventory);
            var winner = context.CreateAdditionalClient(2);
            context.SeedCharacter(3, 0, 3);

            var losers = Clan(context, "Losers", (1, ClanRank.Leader), (3, ClanRank.Member));
            var winners = Clan(context, "Winners", (2, ClanRank.Leader));

            Join(loser, losers);
            Join(winner, winners);

            var online = Gear(context, loser, 9001, 0);
            inventory.WagerItem(loser, new WagerItemPacket { Slot = 0 });
            var offline = WageredOffline(context, 3, 3, 9002);
            var kept = Gear(context, winner, 9003, 0, LootQuality.Epic);
            inventory.WagerItem(winner, new WagerItemPacket { Slot = 0 });

            // What each has wagered, as a record reads it: from the slot of one in the world, from the row of one who is not.
            var wagered = inventory.WageredItemsOf(new uint[] { 3, 2, 1, 1, 0, 404 });

            CollectionAssert.AreEquivalent(new uint[] { 1, 2, 3 }, wagered.Select(w => w.CharacterId).ToArray(), "one row each, and none for who has nothing or is nobody");
            Assert.AreEqual((online.Id, 9001u, (int)LootQuality.Rare, 1u),
                (wagered.Single(w => w.CharacterId == 1).ItemId, wagered.Single(w => w.CharacterId == 1).ItemTemplateId, wagered.Single(w => w.CharacterId == 1).QualityId, wagered.Single(w => w.CharacterId == 1).StackSize));
            Assert.AreEqual((offline, 9002u, (int)LootQuality.Rare, 1u),
                (wagered.Single(w => w.CharacterId == 3).ItemId, wagered.Single(w => w.CharacterId == 3).ItemTemplateId, wagered.Single(w => w.CharacterId == 3).QualityId, wagered.Single(w => w.CharacterId == 3).StackSize));
            Assert.AreEqual((kept.Id, (int)LootQuality.Epic), (wagered.Single(w => w.CharacterId == 2).ItemId, wagered.Single(w => w.CharacterId == 2).QualityId));
            Assert.AreEqual(0, inventory.WageredItemsOf(null).Count);

            var store = new MemoryPvpStore();
            var records = new PvpRecords { WageredItems = inventory.WageredItemsOf };

            records.Load(store);

            WithLiveClans(context, inventory, new[] { losers, winners }, (clans, feuds) =>
            {
                var previous = feuds.Records;

                feuds.Records = records;

                try
                {
                    var feud = feuds.Start(losers, winners, 0, 1, 2);
                    Assert.IsNotNull(feud);

                    feuds.End(feud, ClanFeuds.Outcome.Won, winners.Id, "gm");

                    var match = store.Matches.Single();

                    Assert.AreEqual((byte)2, match.WinnerSide);
                    Assert.AreEqual(3, store.Wagers[match.Id].Count, "everything that was at stake, lost or not");

                    foreach (var (characterId, itemId) in new[] { (1u, online.Id), (3u, offline) })
                    {
                        var lost = store.Wager(match.Id, characterId);

                        Assert.AreEqual((byte)1, lost.Side, "the challengers, who lost");
                        Assert.AreEqual(itemId, lost.ItemId);
                        Assert.AreEqual((byte)PvpWagerResult.ClanLockbox, lost.Result);
                        Assert.AreEqual((winners.Id, 0u), (lost.RecipientClanId, lost.RecipientCharacterId));
                    }

                    var won = store.Wager(match.Id, 2);

                    Assert.AreEqual((byte)2, won.Side);
                    Assert.AreEqual((kept.Id, 9003u, (int)LootQuality.Epic), (won.ItemId, won.ItemTemplateId, won.QualityId));
                    Assert.AreEqual((byte)PvpWagerResult.Kept, won.Result);
                    Assert.AreEqual(0u, won.RecipientClanId);

                    // And it went where the record says.
                    using var unit = context.CreateChar();
                    CollectionAssert.AreEquivalent(new[] { online.Id, offline }, unit.ClanInventories.GetItems(winners.Id).Select(row => row.ItemId).ToArray());
                    Assert.AreEqual(kept.EntityId, winner.Player.Inventory.WagerItem);
                }
                finally
                {
                    feuds.Records = previous;
                }
            });
        }

        #endregion

        #region Fixture

        /// <summary>
        /// The live server's clan manager and feuds over the fixture's database, for the length of
        /// <paramref name="body"/>: the clans cached as the server holds them, and the feuds'
        /// forfeits and wager checks going to <paramref name="inventory"/>. Feuds still running
        /// when the body is done are cancelled.
        /// </summary>
        private static void WithLiveClans(MissionTestContext context, InventoryManager inventory, ClanEntry[] entries, Action<ClanManager, ClanFeuds> body)
        {
            var clanInstance = typeof(ClanManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
            var previousClans = clanInstance.GetValue(null);
            var feuds = ClanFeuds.Instance;
            var forfeit = feuds.Forfeit;
            var holds = feuds.HoldsWager;
            var before = feuds.Feuds.Select(f => f.Id).ToHashSet();

            try
            {
                var clans = (ClanManager)typeof(ClanManager).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null,
                    new[] { typeof(Rasa.Repositories.UnitOfWork.IGameUnitOfWorkFactory) }, null).Invoke(new object[] { context });

                using (var unit = context.CreateChar())
                    foreach (var clan in entries)
                    {
                        var entry = clan;
                        var members = unit.ClanMembers.GetAllClanMembersByClanId(clan.Id);

                        clans.Clans[clan.Id] = new Lazy<ClanEntry>(() => entry);
                        clans.ClanMembers[clan.Id] = new Lazy<List<ClanMemberEntry>>(() => members);
                    }

                clanInstance.SetValue(null, clans);
                feuds.Forfeit = (loser, victor, character, loserName, winnerName, departed) =>
                    inventory.ForfeitWagers(loser, victor, character, loserName, winnerName, departed).Items;
                feuds.HoldsWager = inventory.HoldsWager;

                body(clans, feuds);
            }
            finally
            {
                feuds.Forfeit = null;

                foreach (var feud in feuds.Feuds.Where(f => !before.Contains(f.Id)))
                    feuds.End(feud, ClanFeuds.Outcome.Cancelled);

                feuds.Forfeit = forfeit;
                feuds.HoldsWager = holds;
                clanInstance.SetValue(null, previousClans);
            }
        }

        private MissionTestContext Context(out Client client, out InventoryManager inventory)
        {
            var context = MissionTestContext.WithCompletableMission(429);

            client = context.Client;
            client.Player.Level = 10;
            inventory = new InventoryManager(context);

            Online(client);

            return context;
        }

        private void Online(Client client)
        {
            lock (Server.Clients)
                Server.Clients.Add(client);

            _online.Add(client);
        }

        private void Join(Client client, ClanEntry clan)
        {
            client.Player.ClanId = clan.Id;
            client.Player.Inventory.ResetClanInventory();

            if (!_online.Contains(client))
                Online(client);
        }

        private static void WithVitals(Client client)
        {
            client.Player.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 1000, 1000, 1000, 0, 0);
            client.Player.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, 0, 0, 0, 0, 0);
        }

        private static ItemTemplate Template(MissionTestContext context, uint templateId, LootQuality quality, int level, uint classId, Action<ItemTemplate> shape)
        {
            context.AddRewardTemplate(templateId, classId);

            var classInfo = EntityClassManager.Instance.LoadedEntityClasses[(EntityClasses)classId];
            classInfo.ItemClassInfo.StackSize = classId == StackClass ? 50u : 1u;
            classInfo.ItemClassInfo.MaxHitPoints = 100;

            var template = classInfo.ItemTemplates[templateId];
            template.InventoryCategory = InventoryCategory.Equipment;
            template.EquipableInfo = new EquipableInfo(0, 0);
            template.QualityId = (int)quality;
            template.ItemInfo.Requirements[RequirementsType.ReqXpLevel] = level;
            shape?.Invoke(template);

            return template;
        }

        /// <summary>A piece of equipment in the client's backpack slot: row, entity and list.</summary>
        private static Item Gear(MissionTestContext context, Client owner, uint templateId, uint slot, LootQuality quality = LootQuality.Rare, int level = 10,
            uint classId = GearClass, Action<ItemTemplate> shape = null)
        {
            var item = ItemManager.StageItem(Template(context, templateId, quality, level, classId, shape), 1, "");

            item.OwnerId = owner.Player.Id;
            item.OwnerSlotId = slot;

            using (var unit = context.CreateChar())
            {
                item.Id = unit.Items.CreateItem(item);
                unit.CharacterInventories.AddInvItem(owner.AccountEntry.Id, owner.Player.Id, (uint)InventoryType.Personal, slot, item.Id);
            }

            EntityManager.Instance.RegisterEntity(item.EntityId, EntityType.Item);
            EntityManager.Instance.RegisterItem(item.EntityId, item);
            owner.Player.Inventory.PersonalInventory[(int)slot] = item.EntityId;

            return item;
        }

        /// <summary>An item row with no entity, of a template made for it; its id.</summary>
        private static uint Stored(MissionTestContext context, Repositories.Char.ICharUnitOfWork unit, uint templateId)
        {
            var item = ItemManager.StageItem(Template(context, templateId, LootQuality.Rare, 10, GearClass, null), 1, "");
            var id = unit.Items.CreateItem(item);

            EntityManager.Instance.FreeEntity(item.EntityId);

            return id;
        }

        /// <summary>The wagered item of a character who is not in the world: rows only. Its item id.</summary>
        private static uint WageredOffline(MissionTestContext context, uint accountId, uint characterId, uint templateId)
        {
            using var unit = context.CreateChar();
            var id = Stored(context, unit, templateId);

            unit.CharacterInventories.AddInvItem(accountId, characterId, (uint)InventoryType.WagerInventory, InventoryManager.WagerSlot, id);

            return id;
        }

        private static ClanEntry Clan(MissionTestContext context, string name, params (uint CharacterId, byte Rank)[] members)
        {
            using var unit = context.CreateChar();
            var clan = unit.Clans.CreateClan(name, true);

            foreach (var (characterId, rank) in members)
                Assert.IsTrue(unit.ClanMembers.InsertClanMemberData(clan.Id, characterId, rank, ""));

            return clan;
        }

        /// <summary>Every slot of the clan's one unlocked tab taken.</summary>
        private static void FillLockbox(MissionTestContext context, uint clanId)
        {
            using var unit = context.CreateChar();

            for (uint slot = 0; slot < ClanLockboxTab.SlotsPerTab; slot++)
                unit.ClanInventories.AddInvItem(clanId, slot, Stored(context, unit, 9060));
        }

        private static T Read<T>(T packet, Action<PythonWriter> write) where T : ClientPythonPacket
        {
            using var stream = new MemoryStream();
            using (var binary = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
            using (var writer = new PythonWriter(binary))
                write(writer);

            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            packet.Read(reader);
            return packet;
        }

        private static CharacterEntry Stored(MissionTestContext context, uint characterId)
        {
            using var unit = context.CreateChar();
            return unit.Characters.Find(characterId);
        }

        private static void AssertRow(MissionTestContext context, Item item, InventoryType inventoryType, uint slot)
        {
            using var unit = context.CreateChar();
            var row = unit.CharacterInventories.FindByItemId(item.Id);

            Assert.IsNotNull(row, "the item has its row");
            Assert.AreEqual((uint)inventoryType, row.InventoryType);
            Assert.AreEqual(slot, row.SlotId);
            Assert.AreEqual(item.OwnerId, row.CharacterId);
        }

        #endregion
    }
}
