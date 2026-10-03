using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.Communicator.Server;
    using Packets.LootDispenser.Server;
    using Packets.MapChannel.Client;
    using Packets.MapChannel.Server;
    using Rasa.Packets.ClientMethod.Server;
    using Rasa.Packets.Game.Server;
    using Rasa.Packets.LootDispenser.Client;
    using Rasa.Missions.Definitions;
    using Repositories.UnitOfWork;
    using Structures;
    using Structures.Char;

    public class LootDispenserManager
    {
        /*      LootDispenser Packets
         * - LootInfo(self, lootItems):
         * - AttachInfo(self, attachedEntityId):
         * - OverallQuality(self, overallQualityId):
         * - CanLootItems(self, isLootable, canLootPerItem):
         * - TakenInfo(self, takenItems):
         * - ActorGotLoot(self, actorId, lootEntityIds):
         * - Use(self, actorId, curStateId, * args):
         * - LootCorpse(self, actorId, lootItems):
         * 
         *      LootDispenser Handlers:
         * - RequestCorpseLooting (self.entityId,)
         * - CancelCorpseLooting (self.entityId,)
         * - RequestLootAllFromCorpse (self.entityId, autoLootOnly)
         * - RequestLootItemFromCorpse (self.entityId, itemId, destSlot)
         */

        private static LootDispenserManager _instance;
        private static readonly object InstanceLock = new object();
        private readonly IGameUnitOfWorkFactory _gameUnitOfWorkFactory;
        private readonly MissionApplication _missionManager;
        private readonly Func<Client, double> _distance;
        private readonly Action<Item> _beforeItemPublication;
        private readonly Func<int, int, int> _lootRoll;
        private readonly object _retirementSyncRoot = new object();
        private readonly Dictionary<ulong, PendingRetirement> _pendingRetirements = new();

        private sealed class PendingRetirement
        {
            internal IGameUnitOfWorkFactory Factory;
            internal uint[] ItemIds;
            internal ulong[] EntityIds;
            internal bool InProgress;
        }

        private sealed class RetirementNotice
        {
            internal Client Client;
            internal ulong LootEntityId;
            internal CanLootItemsPacket CanLoot;
            internal DestroyPhysicalEntityPacket Destroy;

            /// <summary>The rolled items nobody took, whose entities go from the client with the corpse.</summary>
            internal List<ulong> ItemEntityIds = new();
        }

        public static LootDispenserManager Instance
        {
            get
            {
                // ReSharper disable once InvertIf
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                            _instance = new LootDispenserManager(Server.GameUnitOfWorkFactory);
                    }
                }

                return _instance;
            }
        }

        internal LootDispenserManager(
            IGameUnitOfWorkFactory gameUnitOfWorkFactory,
            Func<Client, double> distance = null,
            MissionApplication missionManager = null,
            Action<Item> beforeItemPublication = null,
            Func<int, int, int> lootRoll = null)
        {
            _gameUnitOfWorkFactory = gameUnitOfWorkFactory;
            _missionManager = missionManager;
            _distance = distance ?? (client =>
                client.Server?.Config.GameConfig.CorpseLootDistance ??
                Config.GameConfig.DefaultCorpseLootDistance);
            _beforeItemPublication = beforeItemPublication;
            _lootRoll = lootRoll ?? Random.Shared.Next;
        }

        internal void AttachInfo(Client client, LootDispenser loot)
        {
            client.CallMethod(loot.EntityId, new AttachInfoPacket(loot.AttachedTo));
        }

        internal void LootInfo(Client client, LootDispenser loot)
        {
            client.CallMethod(loot.EntityId, new LootInfoPacket(loot.LootItems));
        }

        internal void OverallQuality(Client client, LootDispenser loot)
        {
            client.CallMethod(loot.EntityId, new OverallQualityPacket(loot.LootQuality));
        }

        /// <summary>
        /// What this looter may take: the corpse window draws only the items named here
        /// (IsItemLootable), so a squad mate's rolled item, or under Rotation the holder's, is
        /// left out of everyone else's.
        /// </summary>
        internal void CanLootItems(Client client, LootDispenser loot)
        {
            client.CallMethod(loot.EntityId, new CanLootItemsPacket(loot.IsLootable, LootableBy(loot, client.Player.EntityId)));
        }

        /// <summary>The corpse's items this manifestation may take (LootItem.MayTake).</summary>
        public static List<LootItem> LootableBy(LootDispenser loot, ulong entityId) =>
            loot.LootItems.FindAll(i => i.MayTake(entityId));

        /// <summary>What one take gave this player - the items they took and their share of the credits - if anything.</summary>
        internal void GotLoot(Client client, LootDispenser loot, List<LootItem> items, int credits)
        {
            if ((items == null || items.Count == 0) && credits <= 0)
                return;

            client.CallMethod(SysEntity.ClientMethodId, new GotLootPacket(loot.AttachedTo, items, credits));
        }

        /// <summary>
        /// A corpse's credits divided among those sharing them: an equal share each, the odd
        /// credits one apiece from the first on (the looter who emptied the corpse is first).
        /// Nobody is left out of a share for being last; a pot smaller than the squad gives the
        /// first ones a credit each and the rest nothing.
        /// </summary>
        public static List<int> SplitCredits(int credits, int recipients)
        {
            var shares = new List<int>();

            if (recipients <= 0)
                return shares;

            var each = Math.Max(0, credits) / recipients;
            var odd = Math.Max(0, credits) % recipients;

            for (var i = 0; i < recipients; i++)
                shares.Add(each + (i < odd ? 1 : 0));

            return shares;
        }

        /// <summary>
        /// Who the credits of an emptied corpse go to: the looter who emptied it, first, and the
        /// rest of the squad that shared in the kill (CreditSharers) who are still in the world on
        /// this map. A corpse that was one player's goes to that player.
        /// </summary>
        public static List<Client> CreditRecipients(Client taker, LootDispenser loot)
        {
            var recipients = new List<Client> { taker };
            var mapChannel = taker.Player?.MapChannel;

            if (mapChannel == null || loot.CreditSharers.Count == 0)
                return recipients;

            foreach (var entityId in loot.CreditSharers)
            {
                if (entityId == taker.Player.EntityId)
                    continue;

                var sharer = mapChannel.ClientList.Find(c => c?.Player != null && c.Player.EntityId == entityId
                    && c.State == ClientState.Ingame && c.Player.MapChannel == mapChannel);

                if (sharer != null && !recipients.Contains(sharer))
                    recipients.Add(sharer);
            }

            return recipients;
        }

        /// <summary>The looters of a dispenser who are on this map now.</summary>
        private static List<Client> LootersHere(MapChannel mapChannel, LootDispenser loot)
        {
            return mapChannel?.ClientList.FindAll(c => c?.Player != null
                && (loot.Looters.Contains(c.Player.EntityId) || c.Player.EntityId == loot.Owner)) ?? new List<Client>();
        }

        /// <summary>How long a corpse with nothing left on it stays in the world.</summary>
        public const long EmptyCorpseMs = 20000;

        /// <summary>How long a non-lootable mission scenario corpse stays before its spawn pool may rebuild it.</summary>
        public const long ScenarioActorCorpseMs = 1000;

        /// <summary>How long a corpse that still has something on it stays.</summary>
        public const long LootableCorpseMs = 120000;

        /// <summary>How long a corpse someone has the window open on stays, whatever else is true.</summary>
        public const long BeingLootedCorpseMs = 300000;

        /// <summary>
        /// Whether a dead creature can leave the world yet.
        ///
        /// The rule was twenty seconds from the moment its health hit zero, full stop - no regard
        /// for loot still on it or for a player standing over it with the window open. Twenty
        /// seconds is about one more fight, so a corpse routinely vanished between the kill and
        /// the looting, and the window went with it.
        /// </summary>
        internal bool MayDespawn(MapChannel mapChannel, Creature creature, long deadTime)
        {
            RetryPendingRetirements();

            if (mapChannel == null || creature == null)
                return true;

            lock (mapChannel.LootSyncRoot)
                return MayDespawnLocked(mapChannel, creature, deadTime);
        }

        internal bool AdvanceCorpseLifetime(
            MapChannel mapChannel,
            Creature creature,
            long delta)
        {
            RetryPendingRetirements();

            if (mapChannel == null || creature?.Controller == null)
                return true;

            lock (mapChannel.LootSyncRoot)
            {
                if (delta > 0)
                    creature.Controller.DeadTime =
                        creature.Controller.DeadTime > long.MaxValue - delta
                            ? long.MaxValue
                            : creature.Controller.DeadTime + delta;

                return MayDespawnLocked(
                    mapChannel, creature, creature.Controller.DeadTime);
            }
        }

        private static bool MayDespawnLocked(
            MapChannel mapChannel,
            Creature creature,
            long deadTime)
        {
            if (creature.CorpseLootEntityId != 0 &&
                mapChannel.LootDispensers.TryGetValue(creature.CorpseLootEntityId, out var loot))
                return HasExpired(loot, creature, deadTime);

            if (creature?.SpawnPool?.ScenarioKey != null)
            {
                var lifetime = Math.Max(
                    ScenarioActorCorpseMs,
                    creature.SpawnPool.RespawnTime > 0
                        ? creature.SpawnPool.RespawnTime
                        : ScenarioActorCorpseMs);
                return Math.Max(deadTime, creature.Controller?.DeadTime ?? 0) >= lifetime;
            }

            return Math.Max(deadTime, creature.Controller?.DeadTime ?? 0) >= EmptyCorpseMs;
        }

        internal LootDispenser Create(Client killer, Creature creature, ActorGameplayPolicy policy = null)
        {
            return Create(killer, creature, new List<Client> { killer }, 0, policy);
        }

        /// <summary>
        /// The dispenser for a corpse, owned by the first of the looters - the killer, or the squad
        /// member whose turn it is - and open to all of them. partyId marks its items as the
        /// squad's (Free For All).
        /// </summary>
        internal LootDispenser Create(Client killer, Creature creature, List<Client> looters, uint partyId, ActorGameplayPolicy policy = null)
        {
            RetryPendingRetirements();
            var mapChannel = killer.Player.MapChannel;
            var owner = looters.Count > 0 ? looters[0] : killer;
            var loot = new LootDispenser();
            loot.IsLootable = true;
            loot.AttachedTo = creature.EntityId;
            loot.Owner = owner.Player.EntityId;
            loot.OwnerClient = owner;
            loot.Player = owner.Player;
            loot.Map = mapChannel;
            loot.Corpse = creature;
            loot.CharacterId = owner.Player.Id;
            loot.AccountId = owner.AccountEntry?.Id ?? 0;
            loot.UnitOfWorkFactory = _gameUnitOfWorkFactory;

            foreach (var looter in looters)
                loot.Looters.Add(looter.Player.EntityId);

            loot.Looters.Add(loot.Owner);

            CreateLoot(owner, loot, (policy ?? Game.Missions.World.CreatureGameplayRules.Policy(creature)).Loot, partyId);

            lock (mapChannel.LootSyncRoot)
            {
                mapChannel.LootDispensers.Add(loot.EntityId, loot);
                creature.CorpseLootEntityId = loot.EntityId;
            }

            return loot;
        }

        /// <summary>
        /// One Random, not one per call. Three `new Random()` in a row seeded from the clock gave
        /// three values from the same tick, so the quality tracked the item count.
        /// </summary>
        private static readonly Random Roll = new Random();

        private LootDispenser CreateLoot(Client killer, LootDispenser loot, AuthoredLootProfile profile, uint partyId = 0)
        {
            if (profile != null)
                return CreateAuthoredLoot(killer, loot, profile, partyId);

            int giveLoot;

            lock (Roll)
            {
                giveLoot = Roll.Next(0, 2);
                loot.Credits = Roll.Next(1, 10);
                loot.LootQuality = (LootQuality)Roll.Next(1, 7);
            }

            if (giveLoot > 0)
            {
                // A real item, made now rather than at the moment it is taken. The corpse window
                // resolves every row to an entity and silently drops the ones it cannot
                // (corpselootwindow: GetEntity(itemId), continue on None), so a row without an
                // item behind it is an empty window. It also means what is taken is what was
                // rolled, rather than a second item built from the same template.
                var item = ItemManager.Instance.CreateFromTemplateId(28, (uint)giveLoot * 3);

                if (item != null)
                    loot.LootItems.Add(new LootItem(item, killer.Player.EntityId, partyId));
            }

            return loot;
        }

        private LootDispenser CreateAuthoredLoot(Client owner, LootDispenser loot, AuthoredLootProfile profile, uint partyId = 0)
        {
            var staged = new List<Item>();
            var committed = false;
            try
            {
                using var unit = _gameUnitOfWorkFactory.CreateChar();
                unit.ExecuteTransaction(() =>
                {
                    foreach (var drop in profile.Roll(_lootRoll))
                    {
                        var template = ItemManager.Instance.GetItemTemplateById(drop.TemplateId);
                        var itemClass = template == null ? null :
                            EntityClassManager.Instance.GetClassInfo(template.Class)?.ItemClassInfo;
                        if (itemClass == null || drop.Quantity > itemClass.StackSize)
                            throw new GameplayRejectionException($"Invalid authored loot template {drop.TemplateId}.");
                        var item = ItemManager.StageItem(template, drop.Quantity, string.Empty);
                        staged.Add(item);
                        item.Id = unit.Items.CreateItem(item);
                        if (item.Id == 0)
                            throw new GameplayRejectionException($"Authored loot item {drop.TemplateId} was not persisted.");
                    }
                });
                committed = true;
            }
            finally
            {
                if (!committed)
                    foreach (var item in staged)
                        EntityManager.Instance.FreeEntity(item.EntityId);
            }

            loot.Credits = _lootRoll(1, 10);
            loot.LootQuality = LootQuality.Junk;
            foreach (var item in staged)
            {
                EntityManager.Instance.RegisterEntity(item.EntityId, EntityType.Item);
                EntityManager.Instance.RegisterItem(item.EntityId, item);
                loot.LootItems.Add(new LootItem(item, owner.Player.EntityId, partyId));
                var quality = (LootQuality)item.ItemTemplate.QualityId;
                if (quality.Rank() > loot.LootQuality.Rank())
                    loot.LootQuality = quality;
            }
            return loot;
        }

        /// <summary>
        /// The corpse's loot, for whoever the killer's squad loot method gives it to
        /// (PartyManager.LootersFor): each of them is shown the dispenser. A mission's authored
        /// loot is the killer's alone.
        /// </summary>
        internal void Loot(Client client, Creature creature, ActorGameplayPolicy policy = null)
        {
            policy ??= Game.Missions.World.CreatureGameplayRules.Policy(creature);

            List<Client> looters, eligible;
            uint partyId;
            Party party;

            if (policy.Loot != null)
            {
                looters = new List<Client> { client };
                eligible = looters;
                partyId = 0;
                party = null;
            }
            else
                (looters, partyId, party, eligible) = PartyManager.Instance.LootersFor(client, creature.Position);

            var loot = Create(client, creature, looters, partyId, policy);

            // Everyone who shared in the kill shares in its credits, whatever the method does
            // with the items.
            if (party != null && eligible.Count > 1)
                foreach (var member in eligible)
                    loot.CreditSharers.Add(member.Player.EntityId);

            // What is at or over the squad's threshold is rolled for among everyone sharing in
            // the corpse; a winner the method had left out is shown it too.
            var shownTo = new List<Client>(looters);

            foreach (var winner in LootRolls.Distribute(loot, party, eligible))
                if (!shownTo.Contains(winner))
                {
                    shownTo.Add(winner);
                    loot.Looters.Add(winner.Player.EntityId);
                }

            foreach (var looter in shownTo)
            {
                looter.CallMethod(SysEntity.ClientMethodId, new CreatePhysicalEntityPacket(loot.EntityId, loot.EntityClassId));

                AttachInfo(looter, loot);
                LootInfo(looter, loot);
                OverallQuality(looter, loot);
                CanLootItems(looter, loot);
            }
        }

        internal void AttachRewardLoot(Client owner, MapChannel mapChannel, DynamicObject obj)
        {
            if (owner?.Player == null || mapChannel == null || obj?.MissionLootSource == null)
            {
                Logger.WriteLog(LogType.Error, "Cannot attach reward loot without its owner, map, and mission source.");
                return;
            }

            lock (owner.SyncRoot)
            lock (mapChannel.LootSyncRoot)
            {
                if (obj.LootDispenserEntityId != 0)
                    return;

                var source = obj.MissionLootSource;
                var loot = new LootDispenser
                {
                    AttachedTo = obj.EntityId,
                    AttachedObject = obj,
                    Owner = owner.Player.EntityId,
                    Looters = { owner.Player.EntityId },
                    OwnerClient = owner,
                    Player = owner.Player,
                    Map = mapChannel,
                    CharacterId = owner.Player.Id,
                    AccountId = owner.AccountEntry?.Id ?? 0,
                    UnitOfWorkFactory = _gameUnitOfWorkFactory,
                    LootQuality = LootQuality.Mission
                };
                var stagedItems = new List<Item>();
                var hasClaimedItems = false;
                try
                {
                    if (!MapInstanceScope.Contains(mapChannel, obj) ||
                        !mapChannel.DynamicObjects.Contains(obj) ||
                        !EntityManager.Instance.TryGetObject(obj.EntityId, out var registered) ||
                        !ReferenceEquals(obj, registered) ||
                        _gameUnitOfWorkFactory == null || owner.AccountEntry == null ||
                        !(_missionManager ?? MissionApplication.Instance).GetRewardPackages(source.MissionId)
                            .TryGetValue(source.RewardId, out var reward))
                        throw new GameplayRejectionException("Reward loot source is unavailable.");

                    if (reward.FixedItems.Count == 0 ||
                        reward.FixedItems.Select(item => item.ItemTemplateId).Distinct().Count() != reward.FixedItems.Count)
                        throw new GameplayRejectionException("Reward loot requires distinct, nonempty item rows.");

                    using var unit = _gameUnitOfWorkFactory.CreateChar();
                    unit.ExecuteTransaction(() =>
                    {
                        var character = unit.Characters.Find(owner.Player.Id);
                        var objective = unit.CharacterMissionProgress.Get(
                            owner.Player.Id, source.MissionId, source.ObjectiveId);
                        if (character?.AccountId != owner.AccountEntry.Id || objective == null)
                            throw new GameplayRejectionException("Reward loot ownership or objective is missing.");

                        // This also recognizes characters that received the old all-at-once grant.
                        if (objective.ObjectiveState == (byte)MissionObjectiveState.Completed)
                        {
                            hasClaimedItems = true;
                            return;
                        }
                        if (objective.ObjectiveState != (byte)MissionObjectiveState.Incomplete)
                            throw new GameplayRejectionException("Reward loot objective is not active.");

                        foreach (var row in reward.FixedItems)
                        {
                            if (unit.CharacterMissionScenario.HasStep(
                                owner.Player.Id, source.MissionId, source.ClaimKey(row.ItemTemplateId)))
                            {
                                hasClaimedItems = true;
                                continue;
                            }

                            var template = ItemManager.Instance.GetItemTemplateById(row.ItemTemplateId);
                            var info = template == null ? null :
                                EntityClassManager.Instance.GetClassInfo(template.Class)?.ItemClassInfo;
                            if (info == null || row.Quantity == 0 || row.Quantity > info.StackSize)
                                throw new GameplayRejectionException($"Invalid reward loot template {row.ItemTemplateId}.");

                            var item = ItemManager.StageItem(template, row.Quantity, string.Empty);
                            stagedItems.Add(item);
                            item.Id = unit.Items.CreateItem(item);
                            if (item.Id == 0)
                                throw new GameplayRejectionException("Reward loot item was not persisted.");
                            loot.LootItems.Add(new LootItem(item, owner.Player.EntityId, 0));
                        }
                    });
                }
                catch (Exception error) when (GameplayRejectionException.IsExpected(error))
                {
                    foreach (var item in stagedItems)
                        EntityManager.Instance.FreeEntity(item.EntityId);
                    Logger.WriteLog(LogType.Error,
                        $"Unable to attach reward loot for character {owner.Player.Id}: {error}");
                    return;
                }

                foreach (var item in stagedItems)
                {
                    EntityManager.Instance.RegisterEntity(item.EntityId, EntityType.Item);
                    EntityManager.Instance.RegisterItem(item.EntityId, item);
                }
                loot.IsLootable = loot.HasLoot;
                loot.FullyLooted = !loot.HasLoot;
                mapChannel.LootDispensers.Add(loot.EntityId, loot);
                obj.LootDispenserEntityId = loot.EntityId;
                obj.StateId = hasClaimedItems || loot.FullyLooted
                    ? UseObjectState.TdStateOpened
                    : UseObjectState.TdStateClosed;
                obj.IsEnabled = loot.IsLootable;
                owner.CallMethod(obj.EntityId, new ForceStatePacket(obj.StateId, 0));
                // With the mission that activates the container for its owner (MissionObjects).
                owner.CallMethod(obj.EntityId,
                    MissionObjects.InfoFor(owner, obj, obj.IsEnabled, obj.WindupTime, _missionManager));
                owner.CallMethod(SysEntity.ClientMethodId,
                    new CreatePhysicalEntityPacket(loot.EntityId, loot.EntityClassId));
                AttachInfo(owner, loot);
                LootInfo(owner, loot);
                OverallQuality(owner, loot);
                CanLootItems(owner, loot);
            }
        }

        internal void RemoveForObject(MapChannel mapChannel, DynamicObject obj)
        {
            if (mapChannel == null || obj == null || obj.LootDispenserEntityId == 0)
                return;

            RetryPendingRetirements();
            var notices = new List<RetirementNotice>();
            lock (mapChannel.LootSyncRoot)
                if (mapChannel.LootDispensers.TryGetValue(obj.LootDispenserEntityId, out var loot) &&
                    ReferenceEquals(loot.AttachedObject, obj))
                    notices.AddRange(Retire(mapChannel, loot, true));
            Publish(notices);
        }

        /// <summary>
        /// Drops every dispenser attached to a creature that is leaving the world: out of the
        /// map's table, off the owner's screen if they are still here, and its entity id freed.
        /// </summary>
        internal void RemoveForCreature(MapChannel mapChannel, Creature creature)
        {
            if (mapChannel == null || creature == null)
                return;

            RetryPendingRetirements();
            var notices = new List<RetirementNotice>();
            lock (mapChannel.LootSyncRoot)
                foreach (var loot in mapChannel.LootDispensers.Values
                             .Where(entry => ReferenceEquals(entry.Corpse, creature) ||
                                             entry.AttachedTo == creature.EntityId).ToArray())
                    notices.AddRange(Retire(mapChannel, loot, true));
            Publish(notices);
        }

        internal void RemoveForOwner(MapChannel mapChannel, Client client)
        {
            if (mapChannel == null || client == null)
                return;

            RetryPendingRetirements();
            var notices = new List<RetirementNotice>();
            lock (client.SyncRoot)
            {
                lock (mapChannel.LootSyncRoot)
                    foreach (var loot in mapChannel.LootDispensers.Values.ToArray())
                    {
                        var owned = ReferenceEquals(loot.OwnerClient, client) || loot.Owner == client.Player?.EntityId;

                        // A squad mate's corpse the player could also loot: they are simply no
                        // longer among its looters.
                        if (!owned)
                        {
                            if (client.Player != null)
                                loot.Looters.Remove(client.Player.EntityId);
                            continue;
                        }

                        // Shared with a squad still here: the next of them holds it.
                        var heir = loot.AttachedObject == null
                            ? LootersHere(mapChannel, loot).Find(c => c != client && c.State == ClientState.Ingame)
                            : null;

                        if (heir != null)
                        {
                            loot.Looters.Remove(client.Player?.EntityId ?? 0);
                            loot.Owner = heir.Player.EntityId;
                            loot.OwnerClient = heir;
                            loot.Player = heir.Player;
                            loot.CharacterId = heir.Player.Id;
                            loot.AccountId = heir.AccountEntry?.Id ?? 0;
                            if (loot.CurrentLooter == client.Player?.EntityId)
                                loot.CurrentLooter = 0;
                            continue;
                        }

                        notices.AddRange(Retire(mapChannel, loot, true));
                    }
                Publish(notices);
            }
        }

        /// <summary>
        /// The client has used a corpse and wants the window. Answering with LootCorpse is what
        /// opens it; while this was a stub, nothing ever did, which is why the two per-item
        /// methods had never been reachable.
        /// </summary>
        internal void RequestCorpseLooting(Client client, RequestCorpseLootingPacket packet)
        {
            if (client == null || packet == null)
                return;

            lock (client.SyncRoot)
            {
                var map = client.Player?.MapChannel;
                if (map == null)
                    return;

                lock (map.LootSyncRoot)
                {
                    if (!TryGetLoot(client, packet.EntityId, out var loot))
                        return;

                    if (loot.AttachedObject is { } obj && obj.StateId != UseObjectState.TdStateOpened)
                    {
                        obj.StateId = UseObjectState.TdStateOpened;
                        CellManager.Instance.CellCallMethod(obj, new ForceStatePacket(obj.StateId, 0));
                    }

                    var remaining = loot.Remaining();
                    foreach (var lootItem in remaining)
                        if (lootItem.Item != null)
                            ItemManager.Instance.SendItemDataToClient(client, lootItem.Item, false);

                    loot.CurrentLooter = client.Player.EntityId;

                    LootInfo(client, loot);
                    CanLootItems(client, loot);
                    client.CallMethod(loot.EntityId,
                        new LootCorpsePacket(client.Player.EntityId, remaining));
                }
            }
        }

        /// <summary>
        /// The window has closed. The client sends this on any close, not only a deliberate
        /// cancel, and expects no answer - so this only lets go of the corpse.
        /// </summary>
        internal void CancelCorpseLooting(Client client, CancelCorpseLootingPacket packet)
        {
            if (client == null || packet == null)
                return;

            lock (client.SyncRoot)
            {
                var map = client.Player?.MapChannel;
                if (map == null)
                    return;

                lock (map.LootSyncRoot)
                    if (map.LootDispensers.TryGetValue(packet.EntityId, out var loot) &&
                        loot.CurrentLooter == client.Player.EntityId)
                        loot.CurrentLooter = 0;
            }
        }

        /// <summary>Takes one item off a corpse.</summary>
        internal void RequestLootItemFromCorpse(Client client, RequestLootItemFromCorpsePacket packet)
        {
            if (client == null || packet == null)
                return;

            lock (client.SyncRoot)
            {
                var map = client.Player?.MapChannel;
                if (map == null)
                    return;

                lock (map.LootSyncRoot)
                {
                    if (!TryGetLoot(client, packet.EntityId, out var loot))
                        return;

                    var lootItem = loot.Find(packet.ItemId);
                    if (lootItem == null || lootItem.Taken)
                    {
                        client.CallMethod(loot.EntityId,
                            new TakenInfoPacket(client.Player.EntityId, Taken(loot)));
                        return;
                    }

                    // Someone else's: a squad mate won it, or Rotation gave the corpse to someone
                    // else. The window never showed it; the list of what this player may take goes again.
                    if (!lootItem.MayTake(client.Player.EntityId))
                    {
                        CanLootItems(client, loot);
                        return;
                    }

                    Claim(client, loot, new[] { lootItem }, packet.DestSlot, false);
                }
            }
        }

        /// <summary>
        /// The client sends this two ways and they are not the same request.
        ///
        /// The Loot All button sends autoLootOnly false: take everything.
        ///
        /// Walking near a corpse sends it with autoLootOnly **true**, from lootdispenser's
        /// _UpdateTick, which runs every frame while a dispenser is attached and fires the
        /// moment the player is inside the corpse's auto-loot radius. Nobody clicked anything.
        /// That one means "take what I said I would pick up automatically", which is items at or
        /// below the player's auto-loot threshold - Junk by default, since that is what the
        /// client's own option defaults to.
        ///
        /// Treating them alike is why looting felt random: walking over a body silently emptied
        /// it, so the window either never opened or opened onto a corpse that had already been
        /// cleared out from under it.
        /// </summary>
        internal void RequestLootAllFromCorpse(Client client, RequestLootAllFromCorpsePacket packet)
        {
            if (client == null || packet == null)
                return;

            lock (client.SyncRoot)
            {
                var map = client.Player?.MapChannel;
                if (map == null)
                    return;

                lock (map.LootSyncRoot)
                {
                    if (!TryGetLoot(client, packet.EntityId, out var loot))
                        return;

                    if (loot.AttachedObject != null && packet.AutoLootOnly)
                        return;

                    var threshold = client.Player.AutoLootThreshold;
                    // Only what is theirs to take: a squad mate's rolled item stays for them.
                    var selected = loot.Remaining()
                        .Where(item => item.MayTake(client.Player.EntityId))
                        .Where(item => !packet.AutoLootOnly || WithinThreshold(item, threshold))
                        .ToArray();
                    Claim(client, loot, selected, null, true);
                }
            }
        }

        /// <summary>Whether walking past a corpse should pick this item up unasked.</summary>
        private static bool WithinThreshold(LootItem lootItem, LootQuality threshold)
        {
            var quality = (LootQuality)(lootItem.Item?.ItemTemplate?.QualityId ?? 0);

            // Rank, not the raw id: the ids are the client's and Junk is the largest of them.
            return quality.Rank() <= threshold.Rank();
        }

        /// <summary>
        /// The best quality this player's client will pick up by walking over a corpse. Sent at
        /// login and whenever the option changes; it was a logged ToDo, so every player was
        /// treated as if they had asked for everything.
        /// </summary>
        internal void SetAutoLootThreshold(Client client, SetAutoLootThresholdPacket packet)
        {
            if (client.Player == null)
                return;

            var threshold = (LootQuality)packet.LootLevel;

            // The client only ever sends one of its five option values, but the packet is the
            // client's word: an unknown one would rank as int.MaxValue and auto-loot everything.
            if (!Enum.IsDefined(typeof(LootQuality), threshold) || threshold == LootQuality.Mission)
            {
                Logger.WriteLog(LogType.Security,
                    $"AccountId = {client.AccountEntry?.Id} sent auto-loot threshold {packet.LootLevel}, which is not a quality.");
                return;
            }

            client.Player.AutoLootThreshold = threshold;
        }

        private void Claim(
            Client client,
            LootDispenser loot,
            IReadOnlyList<LootItem> items,
            uint? destSlot,
            bool includeCredits)
        {
            // The credits go when the corpse's last item does, split among everyone sharing the
            // kill (CreditRecipients); a corpse that is one player's pays them on Loot All too.
            var emptiesItems = loot.LootItems.All(item => item.Taken || items.Contains(item));
            includeCredits = loot.Credits > 0 &&
                ((includeCredits && loot.CreditSharers.Count == 0) || emptiesItems);

            var recipients = includeCredits ? CreditRecipients(client, loot) : new List<Client> { client };
            var shares = includeCredits ? SplitCredits(loot.Credits, recipients.Count) : new List<int> { 0 };
            var currentCredits = client.Player.Credits.GetValueOrDefault(CurencyType.Credits);
            var ownShare = shares[0];
            var creditsGranted = includeCredits && ownShare != 0;
            var others = new List<(Client Recipient, int Share, int Before, int After)>();
            int creditsAfter;
            try
            {
                creditsAfter = checked(currentCredits + ownShare);

                for (var i = 1; i < recipients.Count; i++)
                {
                    if (shares[i] <= 0)
                        continue;

                    var before = recipients[i].Player.Credits.GetValueOrDefault(CurencyType.Credits);
                    others.Add((recipients[i], shares[i], before, checked(before + shares[i])));
                }
            }
            catch (OverflowException)
            {
                return;
            }

            var grant = new InventoryManager.LootGrant(_beforeItemPublication);
            var missionManager = _missionManager ?? MissionApplication.Instance;
            var progressPlan =
                MissionProgressPublicationPlan.Empty;
            try
            {
                var factory = loot.UnitOfWorkFactory ?? _gameUnitOfWorkFactory;
                if (factory == null)
                    throw new GameplayRejectionException(
                        "No character persistence factory is available for loot claim.");
                using var unitOfWork = factory.CreateChar();
                unitOfWork.ExecuteTransaction(() =>
                {
                    var character = unitOfWork.Characters.Find(client.Player.Id);
                    if (character == null ||
                        character.AccountId != client.AccountEntry.Id ||
                        character.Credit != currentCredits)
                        throw new GameplayRejectionException(
                            "Durable character ownership or credits changed.");

                    var source = loot.AttachedObject?.MissionLootSource;
                    CharacterMissionObjectiveEntry rewardObjective = null;
                    if (loot.AttachedObject != null)
                    {
                        if (source == null ||
                            unitOfWork.CharacterMissions.GetByCharacterAndMission(
                                client.Player.Id, source.MissionId)?.MissionState != (uint)MissionState.Active ||
                            !unitOfWork.CharacterMissionProgress.GetTracked(
                                client.Player.Id, source.MissionId).TryGetValue(source.ObjectiveId, out rewardObjective) ||
                            rewardObjective.ObjectiveState != (byte)MissionObjectiveState.Incomplete)
                            throw new GameplayRejectionException("Reward loot objective is stale.");

                        foreach (var item in items)
                            if (unitOfWork.CharacterMissionScenario.HasStep(
                                client.Player.Id, source.MissionId, source.ClaimKey(item.ItemTemplateId)))
                                throw new GameplayRejectionException("Reward loot item was already claimed.");
                    }

                    grant.PlanAndSave(client, items, unitOfWork, destSlot);
                    var events = items.GroupBy(item => item.ItemClassId)
                        .Select(group => MissionProgressEvent.ItemAcquired(
                            group.Key,
                            group.Aggregate(0U, (total, item) => checked(total + item.ItemQuantity))))
                        .ToList();
                    var completesReward = source != null && loot.Remaining().All(items.Contains);
                    if (source != null)
                    {
                        foreach (var item in items)
                            unitOfWork.CharacterMissionScenario.Add(new CharacterMissionScenarioStepEntry(
                                client.Player.Id, source.MissionId, source.ClaimKey(item.ItemTemplateId)));
                        if (completesReward)
                            events.Add(MissionProgressEvent.Interaction((uint)loot.AttachedObject.EntityClassId));
                    }
                    progressPlan = missionManager.PlanProgress(client, events, unitOfWork);
                    if (completesReward && rewardObjective.ObjectiveState != (byte)MissionObjectiveState.Completed)
                        throw new GameplayRejectionException("Reward loot did not complete its objective.");

                    if (creditsGranted)
                        unitOfWork.Characters.UpdateCharacterCredits(
                            client.Player.Id, creditsAfter);

                    // The rest of the squad's shares, in the same write: each one's purse as the
                    // server has it, or none of it happens.
                    foreach (var other in others)
                    {
                        var row = unitOfWork.Characters.Find(other.Recipient.Player.Id);
                        if (row == null || row.Credit != other.Before)
                            throw new GameplayRejectionException(
                                "A squad member's durable credits changed.");
                        unitOfWork.Characters.UpdateCharacterCredits(other.Recipient.Player.Id, other.After);
                    }

                    if (!TryGetLoot(client, loot.EntityId, out var current) ||
                        !ReferenceEquals(current, loot))
                        throw new GameplayRejectionException(
                            "Corpse state changed during the claim.");
                });
            }
            catch (Exception error) when (
                error is GameplayRejectionException ||
                error is DbUpdateException ||
                error is DbException)
            {
                Logger.WriteLog(LogType.Error,
                    $"Corpse loot claim failed for character {client.Player.Id}: {error.Message}");
                return;
            }

            grant.Publish(client);

            if (includeCredits)
            {
                loot.Credits = 0;

                if (creditsGranted)
                    client.Player.Credits[CurencyType.Credits] = creditsAfter;

                foreach (var other in others)
                    other.Recipient.Player.Credits[CurencyType.Credits] = other.After;
            }

            if (!loot.HasLoot)
            {
                loot.FullyLooted = true;
                loot.IsLootable = false;
                loot.CurrentLooter = 0;
                if (loot.AttachedObject is { } obj)
                {
                    obj.StateId = UseObjectState.TdStateOpened;
                    obj.IsEnabled = false;
                    MissionApplication.TryPublish(
                        () => CellManager.Instance.CellCallMethod(obj, new ForceStatePacket(obj.StateId, 0)),
                        $"object {obj.EntityId} opened state");
                    MissionApplication.TryPublish(
                        () => DynamicObjectManager.Instance.SetScenarioInteractionEnabled(loot.Map, obj, false),
                        $"object {obj.EntityId} empty state");
                }
            }

            if (creditsGranted)
                MissionApplication.TryPublish(
                    () => client.CallMethod(
                        client.Player.EntityId,
                        new UpdateCreditsPacket(
                            CurencyType.Credits,
                            creditsAfter,
                            ownShare)),
                    $"corpse {loot.EntityId} credits");
            foreach (var other in others)
                MissionApplication.TryPublish(
                    () => other.Recipient.CallMethod(
                        other.Recipient.Player.EntityId,
                        new UpdateCreditsPacket(CurencyType.Credits, other.After, other.Share)),
                    $"corpse {loot.EntityId} squad credits");

            // No ActorGotLoot: its only effect is the pick-up sound, which the GotLoot below plays
            // as well. Everyone sharing the corpse sees the rows go, not only the one who took them.
            var lootersHere = LootersHere(client.Player.MapChannel, loot);
            if (!lootersHere.Contains(client))
                lootersHere.Add(client);

            foreach (var looter in lootersHere)
            {
                MissionApplication.TryPublish(
                    () => looter.CallMethod(
                        loot.EntityId,
                        new TakenInfoPacket(
                            client.Player.EntityId,
                            Taken(loot))),
                    $"corpse {loot.EntityId} taken state");

                if (looter == client || !loot.HasLoot)
                    MissionApplication.TryPublish(
                        () => CanLootItems(looter, loot),
                        $"corpse {loot.EntityId} lootability");
            }

            // What the take gave the looter - the items and their share - and each other sharer
            // their share; the rest of the squad hears what was taken and who got the credits.
            var takenItems = items.ToList();
            MissionApplication.TryPublish(
                () => GotLoot(client, loot, takenItems, ownShare),
                $"corpse {loot.EntityId} loot result");

            var paid = new List<(Client Recipient, int Share)>();
            if (creditsGranted)
                paid.Add((client, ownShare));

            foreach (var other in others)
            {
                paid.Add((other.Recipient, other.Share));
                MissionApplication.TryPublish(
                    () => GotLoot(other.Recipient, loot, null, other.Share),
                    $"corpse {loot.EntityId} squad share");
            }

            MissionApplication.TryPublish(
                () => PartyManager.Instance.AnnounceLoot(client, loot.AttachedTo, takenItems, ownShare),
                $"corpse {loot.EntityId} squad loot notice");

            if (loot.CreditSharers.Count > 0 && paid.Count > 0)
                MissionApplication.TryPublish(
                    () => PartyManager.Instance.AnnounceCredits(paid),
                    $"corpse {loot.EntityId} squad credits notice");

            progressPlan.Publish(client);
        }

        private bool TryGetLoot(Client client, ulong entityId, out LootDispenser loot)
        {
            loot = null;
            var player = client?.Player;
            var map = player?.MapChannel;
            if (client == null || player == null || map == null ||
                client.State != ClientState.Ingame ||
                player.State == CharacterState.Dead ||
                !player.Attributes.TryGetValue(Attributes.Health, out var health) ||
                health.Current <= 0 ||
                EntityManager.Instance.GetEntityType(player.EntityId) != EntityType.Character ||
                !EntityManager.Instance.Players.TryGetValue(player.EntityId, out var registeredPlayer) ||
                !ReferenceEquals(player, registeredPlayer) ||
                !map.LootDispensers.TryGetValue(entityId, out loot) ||
                !loot.IsLootable || loot.FullyLooted ||
                (loot.Owner != player.EntityId && !loot.Looters.Contains(player.EntityId)) ||
                (loot.Owner == player.EntityId && loot.OwnerClient != null && !ReferenceEquals(loot.OwnerClient, client)) ||
                (loot.Owner == player.EntityId && loot.Player != null && !ReferenceEquals(loot.Player, player)) ||
                (loot.Map != null && !ReferenceEquals(loot.Map, map)) ||
                (loot.Owner == player.EntityId && loot.CharacterId != 0 && loot.CharacterId != player.Id) ||
                (loot.Owner == player.EntityId && loot.AccountId != 0 && loot.AccountId != client.AccountEntry?.Id) ||
                !IsFinite(player.Position))
                return false;

            // A scripted prop (a mission reward crate) instead of something killed - no corpse,
            // no kill-ownership semantics, just: is it still the same object, still in the world,
            // still in range.
            if (loot.AttachedObject != null)
            {
                var attachedObject = loot.AttachedObject;
                return attachedObject.LootDispenserEntityId == loot.EntityId &&
                    attachedObject.IsEnabled &&
                    attachedObject.IsInWorld &&
                    client.PendingTransfer == null &&
                    MapInstanceScope.Contains(map, attachedObject) &&
                    EntityManager.Instance.TryGetObject(loot.AttachedTo, out var registeredObject) &&
                    ReferenceEquals(attachedObject, registeredObject) &&
                    map.DynamicObjects.Contains(attachedObject) &&
                    IsFinite(attachedObject.Position) &&
                    Vector3.Distance(player.Position, attachedObject.Position) <= DynamicObjectManager.MaxUseDistance;
            }

            var limit = _distance(client);
            if (!double.IsFinite(limit) || limit <= 0 ||
                !EntityManager.Instance.Creatures.TryGetValue(loot.AttachedTo, out var corpse) ||
                EntityManager.Instance.GetEntityType(corpse.EntityId) != EntityType.Creature ||
                (loot.Corpse != null && !ReferenceEquals(loot.Corpse, corpse)) ||
                corpse.CorpseLootEntityId != loot.EntityId ||
                corpse.State != CharacterState.Dead ||
                !corpse.Attributes.TryGetValue(Attributes.Health, out var corpseHealth) ||
                corpseHealth.Current > 0 ||
                corpse.MapContextId != player.MapContextId ||
                (corpse.RuntimeMapChannel != null && !ReferenceEquals(corpse.RuntimeMapChannel, map)) ||
                !map.MapCellInfo.Cells.Values.Any(cell => cell.CreatureList.Contains(corpse)) ||
                !IsFinite(corpse.Position) ||
                HasExpired(loot, corpse))
                return false;

            var distance = Vector3.Distance(player.Position, corpse.Position);
            if (distance > limit)
            {
                Logger.WriteLog(LogType.Debug,
                    $"Corpse loot request rejected for character {player.Id}: distance {distance:F3} exceeds limit {limit:F3} for corpse {corpse.EntityId}.");
                return false;
            }
            return true;
        }

        private static bool IsFinite(Vector3 value) =>
            float.IsFinite(value.X) &&
            float.IsFinite(value.Y) &&
            float.IsFinite(value.Z);

        private List<RetirementNotice> Retire(MapChannel map, LootDispenser loot, bool notify)
        {
            if (!map.LootDispensers.TryGetValue(loot.EntityId, out var current) ||
                !ReferenceEquals(current, loot))
                return new List<RetirementNotice>();

            var unclaimed = loot.LootItems
                .Where(item => !item.Taken && item.Item?.Id > 0)
                .Select(item => (ItemId: item.Item.Id, EntityId: item.EntityId))
                .Distinct()
                .ToArray();

            var durableCleanupSucceeded = true;
            if (unclaimed.Length > 0)
            {
                try
                {
                    var factory = loot.UnitOfWorkFactory ?? _gameUnitOfWorkFactory;
                    if (factory == null)
                        throw new GameplayRejectionException(
                            "No character persistence factory is available for loot cleanup.");
                    using var unitOfWork = factory.CreateChar();
                    unitOfWork.ExecuteTransaction(() =>
                    {
                        foreach (var item in unclaimed)
                        {
                            unitOfWork.CharacterInventories.DeleteInvItemByItemId(item.ItemId);
                            unitOfWork.Items.DeleteItem(item.ItemId);
                        }
                    });
                }
                catch (Exception error) when (
                    error is GameplayRejectionException ||
                    error is DbUpdateException ||
                    error is DbException)
                {
                    durableCleanupSucceeded = false;
                    Logger.WriteLog(LogType.Error,
                        $"Could not delete {unclaimed.Length} unclaimed loot item row(s): {error.Message}");
                }
            }

            loot.IsLootable = false;
            loot.FullyLooted = true;
            loot.CurrentLooter = 0;

            // Everyone it was shown to: the owner, and a squad sharing it.
            var notices = new List<RetirementNotice>();

            if (notify)
            {
                var shownTo = LootersHere(map, loot);

                if (loot.OwnerClient != null && !shownTo.Contains(loot.OwnerClient))
                    shownTo.Add(loot.OwnerClient);

                var untaken = loot.LootItems.Where(item => !item.Taken && item.Item != null).Select(item => item.EntityId).ToList();

                foreach (var looter in shownTo)
                    notices.Add(new RetirementNotice
                    {
                        Client = looter,
                        LootEntityId = loot.EntityId,
                        CanLoot = new CanLootItemsPacket(false, loot.LootItems),
                        Destroy = new DestroyPhysicalEntityPacket(loot.EntityId),
                        ItemEntityIds = untaken
                    });
            }

            foreach (var item in loot.LootItems)
                if (!item.Taken && item.Item != null)
                {
                    EntityManager.Instance.UnregisterEntity(item.EntityId);
                    EntityManager.Instance.UnregisterItem(item.EntityId);
                    if (durableCleanupSucceeded)
                        EntityManager.Instance.FreeEntity(item.EntityId);
                }

            if (!durableCleanupSucceeded)
                QueueRetirement(loot.EntityId, loot.UnitOfWorkFactory ?? _gameUnitOfWorkFactory,
                    unclaimed.Select(item => item.ItemId).ToArray(),
                    unclaimed.Select(item => item.EntityId).ToArray());

            loot.LootItems.Clear();
            loot.Credits = 0;
            map.LootDispensers.Remove(loot.EntityId);

            if (loot.Corpse?.CorpseLootEntityId == loot.EntityId)
                loot.Corpse.CorpseLootEntityId = 0;
            if (loot.AttachedObject?.LootDispenserEntityId == loot.EntityId)
                loot.AttachedObject.LootDispenserEntityId = 0;

            return notices;
        }

        private static long LifetimeLimit(LootDispenser loot)
        {
            if (loot.CurrentLooter != 0)
                return BeingLootedCorpseMs;
            return loot.HasLoot ? LootableCorpseMs : EmptyCorpseMs;
        }

        private static bool HasExpired(
            LootDispenser loot,
            Creature corpse,
            long observedDeadTime = 0) =>
            loot == null ||
            corpse?.Controller == null ||
            Math.Max(observedDeadTime, corpse.Controller.DeadTime) >=
            LifetimeLimit(loot);

        private void QueueRetirement(
            ulong lootEntityId,
            IGameUnitOfWorkFactory factory,
            uint[] itemIds,
            ulong[] entityIds)
        {
            lock (_retirementSyncRoot)
                _pendingRetirements[lootEntityId] = new PendingRetirement
                {
                    Factory = factory,
                    ItemIds = itemIds,
                    EntityIds = entityIds
                };
        }

        private void RetryPendingRetirements()
        {
            PendingRetirement[] pending;
            lock (_retirementSyncRoot)
            {
                pending = _pendingRetirements.Values
                    .Where(entry => !entry.InProgress)
                    .ToArray();
                foreach (var entry in pending)
                    entry.InProgress = true;
            }

            foreach (var entry in pending)
            {
                var succeeded = false;
                try
                {
                    if (entry.Factory == null)
                        throw new GameplayRejectionException(
                            "No character persistence factory is available for loot cleanup retry.");
                    using var unitOfWork = entry.Factory.CreateChar();
                    unitOfWork.ExecuteTransaction(() =>
                    {
                        foreach (var itemId in entry.ItemIds)
                        {
                            unitOfWork.CharacterInventories.DeleteInvItemByItemId(itemId);
                            unitOfWork.Items.DeleteItem(itemId);
                        }
                    });
                    succeeded = true;
                }
                catch (Exception error) when (
                    error is GameplayRejectionException ||
                    error is DbUpdateException ||
                    error is DbException)
                {
                    Logger.WriteLog(LogType.Error,
                        $"Could not retry unclaimed loot cleanup: {error.Message}");
                }

                lock (_retirementSyncRoot)
                {
                    var pair = _pendingRetirements.FirstOrDefault(candidate =>
                        ReferenceEquals(candidate.Value, entry));
                    if (succeeded && pair.Value != null)
                    {
                        _pendingRetirements.Remove(pair.Key);
                        foreach (var entityId in entry.EntityIds)
                            EntityManager.Instance.FreeEntity(entityId);
                    }
                    else
                    {
                        entry.InProgress = false;
                    }
                }
            }
        }

        private static void Publish(IEnumerable<RetirementNotice> notices)
        {
            foreach (var notice in notices.Where(entry => entry?.Client != null))
                if (notice.Client.State == ClientState.Ingame)
                {
                    notice.Client.CallMethod(notice.LootEntityId, notice.CanLoot);
                    notice.Client.CallMethod(SysEntity.ClientMethodId, notice.Destroy);

                    // The rolled items went with it; the client was given each one.
                    foreach (var itemEntityId in notice.ItemEntityIds)
                        notice.Client.CallMethod(SysEntity.ClientMethodId, new DestroyPhysicalEntityPacket(itemEntityId));
                }
        }

        /// <summary>The rows TakenInfo should mark; the client keys off the ones it is sent.</summary>
        private static List<LootItem> Taken(LootDispenser loot) => loot.LootItems.FindAll(i => i.Taken);
    }
}
