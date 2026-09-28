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

        internal void CanLootItems(Client client, LootDispenser loot)
        {
            client.CallMethod(loot.EntityId, new CanLootItemsPacket(loot.IsLootable, loot.LootItems));
        }

        internal void GotLoot(Client client, LootDispenser loot)
        {
            client.CallMethod(SysEntity.ClientMethodId, new GotLootPacket(loot));
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
            RetryPendingRetirements();
            var mapChannel = killer.Player.MapChannel;
            var loot = new LootDispenser();
            loot.IsLootable = true;
            loot.AttachedTo = creature.EntityId;
            loot.Owner = killer.Player.EntityId;
            loot.OwnerClient = killer;
            loot.Player = killer.Player;
            loot.Map = mapChannel;
            loot.Corpse = creature;
            loot.CharacterId = killer.Player.Id;
            loot.AccountId = killer.AccountEntry?.Id ?? 0;
            loot.UnitOfWorkFactory = _gameUnitOfWorkFactory;

            CreateLoot(killer, loot, (policy ?? Game.Missions.World.CreatureGameplayRules.Policy(creature)).Loot);

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

        private LootDispenser CreateLoot(Client killer, LootDispenser loot, AuthoredLootProfile profile)
        {
            if (profile != null)
                return CreateAuthoredLoot(killer, loot, profile);

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
                    loot.LootItems.Add(new LootItem(item, killer.Player.EntityId, 0));
            }

            return loot;
        }

        private LootDispenser CreateAuthoredLoot(Client owner, LootDispenser loot, AuthoredLootProfile profile)
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
                loot.LootItems.Add(new LootItem(item, owner.Player.EntityId, 0));
                var quality = (LootQuality)item.ItemTemplate.QualityId;
                if (quality.Rank() > loot.LootQuality.Rank())
                    loot.LootQuality = quality;
            }
            return loot;
        }

        internal void Loot(Client client, Creature creature, ActorGameplayPolicy policy = null)
        {
            var loot = Create(client, creature, policy);

            client.CallMethod(SysEntity.ClientMethodId, new CreatePhysicalEntityPacket(loot.EntityId, loot.EntityClassId));

            AttachInfo(client, loot);
            LootInfo(client, loot);
            OverallQuality(client, loot);
            CanLootItems(client, loot);
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
                owner.CallMethod(obj.EntityId,
                    new UsableInfoPacket(obj.IsEnabled, obj.StateId, 0, obj.WindupTime, obj.ActivateMission));
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
                    notices.Add(Retire(mapChannel, loot, true));
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
                    notices.Add(Retire(mapChannel, loot, true));
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
                    foreach (var loot in mapChannel.LootDispensers.Values
                                 .Where(entry => ReferenceEquals(entry.OwnerClient, client) ||
                                                 entry.Owner == client.Player?.EntityId).ToArray())
                        notices.Add(Retire(mapChannel, loot, true));
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
                    var selected = loot.Remaining()
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
            var currentCredits = client.Player.Credits.GetValueOrDefault(CurencyType.Credits);
            var creditsGranted = includeCredits && loot.Credits != 0;
            int creditsAfter;
            try
            {
                creditsAfter = checked(currentCredits + (includeCredits ? loot.Credits : 0));
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

                    if (includeCredits && loot.Credits != 0)
                        unitOfWork.Characters.UpdateCharacterCredits(
                            client.Player.Id, creditsAfter);

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

            if (creditsGranted)
            {
                loot.Credits = 0;
                client.Player.Credits[CurencyType.Credits] = creditsAfter;
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
                            0)),
                    $"corpse {loot.EntityId} credits");
            MissionApplication.TryPublish(
                () => client.CallMethod(
                    loot.EntityId,
                    new ActorGotLootPacket(loot)),
                $"corpse {loot.EntityId} actor loot result");
            MissionApplication.TryPublish(
                () => client.CallMethod(
                    loot.EntityId,
                    new TakenInfoPacket(
                        client.Player.EntityId,
                        Taken(loot))),
                $"corpse {loot.EntityId} taken state");
            MissionApplication.TryPublish(
                () => CanLootItems(client, loot),
                $"corpse {loot.EntityId} lootability");
            if (!loot.HasLoot)
                MissionApplication.TryPublish(
                    () => GotLoot(client, loot),
                    $"corpse {loot.EntityId} completion");

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
                loot.Owner != player.EntityId ||
                (loot.OwnerClient != null && !ReferenceEquals(loot.OwnerClient, client)) ||
                (loot.Player != null && !ReferenceEquals(loot.Player, player)) ||
                (loot.Map != null && !ReferenceEquals(loot.Map, map)) ||
                (loot.CharacterId != 0 && loot.CharacterId != player.Id) ||
                (loot.AccountId != 0 && loot.AccountId != client.AccountEntry?.Id) ||
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

        private RetirementNotice Retire(MapChannel map, LootDispenser loot, bool notify)
        {
            if (!map.LootDispensers.TryGetValue(loot.EntityId, out var current) ||
                !ReferenceEquals(current, loot))
                return null;

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

            var owner = loot.OwnerClient ??
                        map.ClientList.Find(client => client.Player?.EntityId == loot.Owner);
            var notice = notify && owner != null
                ? new RetirementNotice
                {
                    Client = owner,
                    LootEntityId = loot.EntityId,
                    CanLoot = new CanLootItemsPacket(false, loot.LootItems),
                    Destroy = new DestroyPhysicalEntityPacket(loot.EntityId)
                }
                : null;

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

            return notice;
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
                }
        }

        /// <summary>The rows TakenInfo should mark; the client keys off the ones it is sent.</summary>
        private static List<LootItem> Taken(LootDispenser loot) => loot.LootItems.FindAll(i => i.Taken);
    }
}
