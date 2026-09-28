using System;
using System.Collections.Generic;
using System.Numerics;
using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Models;
    using Packets;
    using Packets.Game.Server;
    using Packets.MapChannel.Server;
    using Repositories.UnitOfWork;
    using Structures;


    public class CreatureManager
    {
        /* Actor: AI bodies     (CreatureClass)
         * 
         *  - CreatureInfo
         *  - BattlecryNotification
         *  - Bark
         *  - UpdateEscortStatus
         */

        private static CreatureManager _instance;
        private static readonly object InstanceLock = new object();
        public const long CreatureLocationUpdateTime = 1500;
        public Dictionary<uint, Creature> LoadedCreatures = new();
        private readonly IGameUnitOfWorkFactory _gameUnitOfWorkFactory;
        private readonly ManifestationManager _manifestationManager;
        private readonly MissionApplication _missionManager;
        public static CreatureManager Instance
        {
            get
            {
                // ReSharper disable once InvertIf
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                            _instance = new CreatureManager(Server.GameUnitOfWorkFactory);
                    }
                }

                return _instance;
            }
        }

        private CreatureManager(IGameUnitOfWorkFactory gameUnitOfWorkFactory)
            : this(gameUnitOfWorkFactory, new ManifestationManager(gameUnitOfWorkFactory))
        {
        }

        internal CreatureManager(
            IGameUnitOfWorkFactory gameUnitOfWorkFactory,
            ManifestationManager manifestationManager,
            MissionApplication missionManager = null)
        {
            _gameUnitOfWorkFactory = gameUnitOfWorkFactory;
            _manifestationManager = manifestationManager;
            _missionManager = missionManager;
        }

        /// <summary>
        /// What this creature is, for CreatureInfo. Flags belong to the entity class, so every
        /// creature of a species carries the same list and a class with none - anything that is
        /// not a creature, and the 128 creature classes no species could be matched to - sends
        /// an empty one, which is what the client got for every creature before.
        /// </summary>
        public static List<int> CreatureFlagsOf(Creature creature)
        {
            if (creature == null)
                return new List<int>();

            return EntityClassManager.Instance.LoadedEntityClasses
                .TryGetValue(creature.EntityClass, out var entityClass) && entityClass != null
                ? entityClass.CreatureFlags.ConvertAll(f => (int)f)
                : new List<int>();
        }

        // 1 creature to n client's
        public void CellIntroduceCreatureToClients(MapChannel mapChannel, Creature creature, List<Client> clientList)
        {
            foreach (var client in clientList)
                CreateCreatureOnClient(client, creature);
        }

        // n creatures to 1 client
        public void CellIntroduceCreaturesToClient(Client client, List<Creature> creaturList)
        {
            foreach (var creature in creaturList)
                CreateCreatureOnClient(client, creature);
        }

        private void GiveWeapon(Creature creature)
        {
            var haveWeapon = creature.AppearanceData.ContainsKey(EquipmentData.Weapon);

            if (!haveWeapon)
            {
                var weapon = new AppearanceData
                {
                    SlotId = EquipmentData.Weapon,
                    Color = Color.RandomColor(),
                    Hue2 = Color.RandomColor()
                };

                switch (creature.EntityClass)
                {
                    case (EntityClasses)20757:
                        weapon.Class = 3878;
                        break;
                    case (EntityClasses)9244:
                        weapon.Class = 3782;
                        break;
                    case (EntityClasses)3846:
                        weapon.Class = 27131;
                        break;
                    case (EntityClasses)3848:
                        weapon.Class = 6443;
                        break;
                    default:
                        return;
                }

                creature.AppearanceData.Add(EquipmentData.Weapon, weapon);
                UpdateCreatureAppearance(creature);
            }
        }

        internal void HandleCreatureKill(MapChannel mapChannel, Creature creature, Actor killedBy)
        {
            if (creature.State == CharacterState.Dead || Game.Missions.World.CreatureGameplayRules.IsInvulnerable(creature))
                return; // creature already dead

            var policy = Game.Missions.World.CreatureGameplayRules.Policy(creature);
            var participant = creature.CombatParticipant;
            var canReward = creature.Faction != Factions.AFS &&
                Game.Missions.World.CreatureGameplayRules.RewardsKills(creature);

            // kill creature
            var stateIds = new List<CharacterState> { CharacterState.Dead };

            creature.State = CharacterState.Dead;
            Game.Missions.World.CreatureGameplayRules.ClearRole(creature);
            CellManager.Instance.CellCallMethod(mapChannel, creature, new StateChangePacket(stateIds));
            if (creature.SpawnPool?.FollowOwnerCharacterId > 0)
                PublishEscortStatus(mapChannel, creature, false);

            // A debuff does not outlive what it was on: a Ruin still ticking on a corpse would
            // try to damage it every second until it expired.
            GameEffectManager.Instance.ClearEffects(mapChannel, creature);

            // tell spawnpool if set
            if (creature.SpawnPool != null)
            {
                SpawnPoolManager.Instance.DecreaseAliveCreatureCount(mapChannel, creature.SpawnPool);
                SpawnPoolManager.Instance.IncreaseDeadCreatureCount(creature.SpawnPool);
                if (creature.SpawnPool.SceneRunId == null)
                    (_missionManager ?? MissionApplication.Instance).RecordScenarioCreatureDeath(creature.SpawnPool);
            }

            // todo: How were credits and experience calculated when multiple players attacked the same creature? Did only the player with the first strike get experience?

            Client client = null;
            foreach (var cell in CellManager.CellsIn(mapChannel, killedBy?.Cells))
                foreach (var candidate in cell.ClientList)
                    if (candidate.Player == killedBy && MapInstanceScope.Contains(mapChannel, killedBy))
                    {
                        client = candidate;
                        break;
                    }

            if (client == null && killedBy is Creature killer && killer.Faction != creature.Faction)
                client = FindEscortOwner(mapChannel, killer);
            if (client == null && killedBy is Creature defender &&
                Game.Missions.World.CreatureGameplayRules.IsDefender(defender) && policy.TrackParticipation &&
                defender.Faction != creature.Faction && IsLivingOnMap(mapChannel, defender))
                client = FindCombatPlayer(mapChannel, participant);
            creature.CombatParticipant = null;
            canReward &= client != null &&
                (!mapChannel.IsPrivateInstance || mapChannel.OwnerCharacterId == client.Player.Id);

            if (client != null)
            {
                if (canReward)
                {
                    // give experience
                    var experience = creature.Level * 100; // base experience
                    var experienceRange = creature.Level * 10;
                    experience += (uint)(new Random().Next() % (experienceRange * 2 + 1)) - experienceRange;

                    // todo: Depending on level difference reduce experience
                    _manifestationManager.GainExperience(client, experience);

                    // Adrenaline is earned here and nowhere else: it does not regenerate. See
                    // ManifestationManager.AdrenalinePerKillPercent.
                    _manifestationManager.GainAdrenaline(
                        client,
                        _manifestationManager.AdrenalineForKill(client));
                }
            }

            // Only the credited player may harvest; unowned deaths clear any previous claim.
            creature.HarvestOwnerEntityId = canReward && client != null
                ? client.Player.EntityId
                : 0;
            creature.HarvestAttemptsLeft = canReward && client != null
                ? Harvest.AttemptsPerCorpse
                : 0;

            // spawn loot
            if (killedBy != null && client != null && canReward)
            {
                try
                {
                    LootDispenserManager.Instance.Loot(client, creature, policy);
                }
                catch (Exception error) when (GameplayRejectionException.IsExpected(error))
                {
                    Logger.WriteLog(LogType.Error,
                        $"Corpse loot generation failed for creature {creature.EntityId} ({creature.DbId}), " +
                        $"character {client.Player.Id}; recording kill progress without loot: {error}");
                }
            }

            var progressClient = client;
            if (creature.SpawnPool?.SceneRunId != null)
                (_missionManager ?? MissionApplication.Instance).Scenes.RecordDefeat(mapChannel, creature,
                    progressClient != null && CanCreditScenarioProgress(mapChannel, creature, progressClient)
                        ? progressClient : null);
            else if (progressClient != null && CanCreditScenarioProgress(mapChannel, creature, progressClient))
                (_missionManager ?? MissionApplication.Instance).Credit.Record(
                    progressClient,
                    MissionProgressEvent.Creature(creature.DbId),
                    creature.Position);
        }

        internal static Client FindEscortOwner(MapChannel mapChannel, Creature escort)
        {
            var pool = escort?.SpawnPool;
            if (pool?.FollowOwnerCharacterId is not > 0 || escort.MasterEntityId != 0 ||
                pool.ScenarioOwnerCharacterId != pool.FollowOwnerCharacterId ||
                mapChannel.IsPrivateInstance && mapChannel.OwnerCharacterId != pool.FollowOwnerCharacterId ||
                !IsLivingOnMap(mapChannel, escort))
                return null;

            return mapChannel.ClientList.FirstOrDefault(client =>
                client?.State == ClientState.Ingame && client.PendingTransfer == null &&
                client.Player?.Id == pool.FollowOwnerCharacterId &&
                IsLivingOnMap(mapChannel, client.Player) &&
                CellManager.Instance.IsInWorld(client) &&
                client.Player.Missions.TryGetValue(pool.ScenarioMissionId, out var mission) &&
                mission.State == MissionState.Active);
        }

        internal static bool IsLivingOnMap(MapChannel map, Actor actor) =>
            actor != null && actor.State != CharacterState.Dead &&
            actor.Attributes.TryGetValue(Attributes.Health, out var health) && health.Current > 0 &&
            MapInstanceScope.Contains(map, actor) &&
            (actor is Creature creature
                ? EntityManager.Instance.GetEntityType(actor.EntityId) == EntityType.Creature &&
                  (creature.RuntimeMapChannel == null || ReferenceEquals(creature.RuntimeMapChannel, map)) &&
                  EntityManager.Instance.Creatures.TryGetValue(actor.EntityId, out var registered) &&
                  ReferenceEquals(registered, creature)
                : actor is Manifestation player &&
                  EntityManager.Instance.GetEntityType(actor.EntityId) == EntityType.Character &&
                  EntityManager.Instance.Players.TryGetValue(actor.EntityId, out var registeredPlayer) &&
                  ReferenceEquals(registeredPlayer, player));

        internal static bool IsHostileTarget(MapChannel map, Actor source, Creature target) =>
            IsLivingOnMap(map, source) && IsLivingOnMap(map, target) &&
            target.Faction != (source is Creature creature ? creature.Faction : Factions.AFS);

        internal static void RecordOwnerAttack(MapChannel map, Actor source, Creature target)
        {
            if (source is not Manifestation player ||
                map?.SpawnPools?.Any(pool =>
                    pool.FollowOwnerCharacterId != 0 && pool.FollowOwnerCharacterId == player.Id) != true ||
                FindCombatPlayer(map, player) == null ||
                !IsHostileTarget(map, player, target))
                return;

            foreach (var escort in map.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList).Distinct())
                if (FindEscortOwner(map, escort)?.Player == player &&
                    IsHostileTarget(map, escort, target))
                    escort.Controller.ActionFollow.OwnerAttackTarget = target;
        }

        private static Client FindCombatPlayer(MapChannel map, Manifestation player) =>
            IsLivingOnMap(map, player) ? map.ClientList.FirstOrDefault(client =>
                client?.Player == player && client.State == ClientState.Ingame &&
                client.PendingTransfer == null && CellManager.Instance.IsInWorld(client) &&
                (!map.IsPrivateInstance || map.OwnerCharacterId == player.Id)) : null;

        internal static void RecordCombatDamage(MapChannel map, Creature target, Actor source, int damage)
        {
            if (damage <= 0 || !Game.Missions.World.CreatureGameplayRules.TracksParticipation(target) || !IsHostileTarget(map, source, target))
                return;
            var client = source is Manifestation player
                ? FindCombatPlayer(map, player)
                : FindEscortOwner(map, source as Creature);
            if (client != null)
                target.CombatParticipant = client.Player;
        }

        internal static void PublishEscortStatus(MapChannel mapChannel, Creature creature, bool isEscort)
        {
            foreach (var client in CellManager.Instance.GetClientsInCells(mapChannel, creature.Cells))
                if (client.Player?.Id == creature.SpawnPool?.FollowOwnerCharacterId)
                    client.CallMethod(creature.EntityId, new UpdateEscortStatusPacket(isEscort));
        }

        private static bool CanCreditScenarioProgress(
            MapChannel mapChannel,
            Creature creature,
            Client client)
        {
            if (client?.Player == null ||
                creature?.SpawnPool?.ScenarioKey == null)
                return client != null;

            if (!mapChannel.IsPrivateInstance || mapChannel.OwnerCharacterId == 0)
                return true;

            return mapChannel.OwnerCharacterId == client.Player.Id;
        }

        public Creature CreateCreature(uint dbId, SpawnPool spawnPool)
        {
            // check is creature in database
            if (!LoadedCreatures.TryGetValue(dbId, out var creatureEntry) || creatureEntry == null)
            {
                Logger.WriteLog(LogType.Error, $"Creature with dbId={dbId}, isn't in database");

                MapErrorManager.Instance.Record(spawnPool?.MapContextId ?? MapErrorManager.ServerWide,
                    $"Spawn pool {spawnPool?.DbId.ToString() ?? "?"} wants creature {dbId}, which is not in the database.");

                return null;
            }

            if (!EntityClassManager.Instance.LoadedEntityClasses.TryGetValue(creatureEntry.EntityClass, out var entityClass) ||
                entityClass == null)
            {
                Logger.WriteLog(LogType.Error, $"Creature with dbId={dbId} references missing entity class {creatureEntry.EntityClass}");
                return null;
            }

            if (entityClass.Augmentations == null || !entityClass.Augmentations.Contains(AugmentationType.Creature))
            {
                Logger.WriteLog(LogType.Error, $"Creature with dbId = {dbId}, don't have creature Augmentation");

                MapErrorManager.Instance.Record(spawnPool?.MapContextId ?? MapErrorManager.ServerWide,
                    $"Creature {dbId} (class {LoadedCreatures[dbId].EntityClass}) has no Creature augmentation, so it cannot be spawned.");

                return null;
            }

            // create creature
            var creature = (Creature)creatureEntry.Clone();

            creature.SpawnPool = spawnPool;
            if (spawnPool?.RuntimeMapChannel != null)
            {
                var leases = (_missionManager ?? MissionApplication.Instance).PublicActors;
                leases.Recover(spawnPool.RuntimeMapChannel);
                if (leases.IsReserved(spawnPool.RuntimeMapChannel, spawnPool.DbId))
                    creature.IsInteractable = false;
            }

            creature.State = CharacterState.Idle;
            creature.Name = entityClass.ClassName;

            // set creature stats
            using var unitOfWork = _gameUnitOfWorkFactory.CreateWorld();
            var creatureStats = unitOfWork.Creatures.GetCreatureStats(dbId);

            if (creatureStats != null)
            {
                creature.Attributes.Add(Attributes.Body, new ActorAttributes(Attributes.Body, creatureStats.Body, creatureStats.Body, creatureStats.Body, 5, 1000));
                creature.Attributes.Add(Attributes.Mind, new ActorAttributes(Attributes.Mind, creatureStats.Mind, creatureStats.Mind, creatureStats.Mind, 5, 1000));
                creature.Attributes.Add(Attributes.Spirit, new ActorAttributes(Attributes.Spirit, creatureStats.Spirit, creatureStats.Spirit, creatureStats.Spirit, 5, 1000));
                creature.Attributes.Add(Attributes.Health, new ActorAttributes(Attributes.Health, creatureStats.Health, creatureStats.Health, creatureStats.Health, 5, 1000));
                creature.Attributes.Add(Attributes.Chi, new ActorAttributes(Attributes.Chi, 0, 0, 0, 0, 0));
                creature.Attributes.Add(Attributes.Power, new ActorAttributes(Attributes.Power, 0, 0, 0, 0, 0));
                creature.Attributes.Add(Attributes.Aware, new ActorAttributes(Attributes.Aware, 0, 0, 0, 0, 0));
                creature.Attributes.Add(Attributes.Armor, new ActorAttributes(Attributes.Armor, creatureStats.Armor, creatureStats.Armor, creatureStats.Armor, 5, 1000));
                creature.Attributes.Add(Attributes.Speed, new ActorAttributes(Attributes.Speed, 1, 1, 1, 0, 0));
                creature.Attributes.Add(Attributes.Regen, new ActorAttributes(Attributes.Regen, 0, 0, 0, 0, 0));
            }
            else
            {
                creature.Attributes.Add(Attributes.Body, new ActorAttributes(Attributes.Body, 15, 15, 15, 5, 1000));
                creature.Attributes.Add(Attributes.Mind, new ActorAttributes(Attributes.Mind, 15, 15, 15, 5, 1000));
                creature.Attributes.Add(Attributes.Spirit, new ActorAttributes(Attributes.Spirit, 15, 15, 15, 5, 1000));
                creature.Attributes.Add(Attributes.Health, new ActorAttributes(Attributes.Health, 100, 100, 100, 10, 1000));
                creature.Attributes.Add(Attributes.Chi, new ActorAttributes(Attributes.Chi, 0, 0, 0, 0, 0));
                creature.Attributes.Add(Attributes.Power, new ActorAttributes(Attributes.Power, 0, 0, 0, 0, 0));
                creature.Attributes.Add(Attributes.Aware, new ActorAttributes(Attributes.Aware, 0, 0, 0, 0, 0));
                creature.Attributes.Add(Attributes.Armor, new ActorAttributes(Attributes.Armor, 100, 100, 100, 5, 1000));
                creature.Attributes.Add(Attributes.Speed, new ActorAttributes(Attributes.Speed, 1, 1, 1, 0, 0));
                creature.Attributes.Add(Attributes.Regen, new ActorAttributes(Attributes.Regen, 0, 0, 0, 0, 0));
            }

            creature.Controller.CurrentAction = BehaviorManager.BehaviorActionWander;
            creature.Controller.ActionWander.State = BehaviorManager.WanderIdle; //wanderstate: calc new position

            if (spawnPool != null)
                SpawnPoolManager.Instance.IncreaseAliveCreatureCount(spawnPool);

            return creature;
        }

        internal void CellUpdateLocation(MapChannel mapChannel, Creature creature, uint newLocX, uint newLocZ)
        {
            // get old and new cell matrix
            var oldCellMatrix = creature.Cells;
            var newCellMatrix = CellManager.Instance.CreateCellMatrix(mapChannel, newLocX, newLocZ);

            // get info about cell we need to update
            var needUpdate = new List<uint>();
            var needDelete = new List<uint>();

            CellManager.Instance.GetCellMatrixDiff(oldCellMatrix, newCellMatrix, out needUpdate, out needDelete);

            mapChannel.MapCellInfo.Cells[oldCellMatrix[2, 2]].CreatureList.Remove(creature);

            // remove creature for player that are not in visibility range anymore
            foreach (var cellSeed in needDelete)
                foreach (var client in mapChannel.MapCellInfo.Cells[cellSeed].ClientList)
                    client.CallMethod(SysEntity.ClientMethodId, new DestroyPhysicalEntityPacket(creature.EntityId));

            // add creature to new cell
            mapChannel.MapCellInfo.Cells[newCellMatrix[2, 2]].CreatureList.Add(creature);

            // set new creature visibility
            creature.Cells = newCellMatrix;

            // notify clients about creature
            foreach (var cellSeed in needUpdate)
                foreach (var client in mapChannel.MapCellInfo.Cells[cellSeed].ClientList)
                    CreateCreatureOnClient(client, creature);
        }

        public void CreateCreatureOnClient(Client client, Creature creature)
        {
            if (creature == null)
                return;

            // random colors for now
            var hue = Color.RandomColor();
            var hue2 = Color.RandomColor();

            var entityData = new List<PythonPacket>
            {
                // PhysicalEntity
                new IsTargetablePacket(EntityClassManager.Instance.GetClassInfo(EntityManager.Instance.GetEntityClassId(creature.EntityId)).TargetFlag),
                new WorldLocationDescriptorPacket(creature.Position, creature.Rotation),
                new BodyAttributesPacket(creature.Scale, hue, 0, 0, hue2),
                // Creature augmentation
                new CreatureInfoPacket(creature.NameId, false, CreatureFlagsOf(creature)),
                // Actor augmentation
                new ActorInfoPacket(creature),
                new AppearanceDataPacket(creature.AppearanceData),
                new LevelPacket(creature.Level),
                new AttributeInfoPacket(creature.Attributes),
                new TargetCategoryPacket(creature.Faction),
                new UpdateAttributesPacket(creature.Attributes, 0),
                new IsRunningPacket(creature.IsRunning)
            };
            if (creature.State != CharacterState.Dead &&
                creature.SpawnPool?.FollowOwnerCharacterId is > 0 &&
                creature.SpawnPool?.FollowOwnerCharacterId == client.Player?.Id)
                entityData.Add(new UpdateEscortStatusPacket(true));

            client.CallMethod(SysEntity.ClientMethodId, new CreatePhysicalEntityPacket(creature.EntityId, creature.EntityClass, entityData));

            // NPC  & Vendor augmentation
            if (creature.Npc != null)
            {
                // npc.py's own NPC.__init__ leaves npcPackageId at None until Recv_NPCInfo sets
                // it - with nothing ever sending this packet, every NPC's client-side package id
                // stayed None forever, which is invisible until something builds a lookup keyed
                // on it (BuildObjectiveConversationText's (mission, objective, npcPackageId,
                // playerFlagId, convoType) tuple), and then surfaces as ID_ERR_MISSING_TRANSLATION
                // with "None" in the key even though the server-side binding is correct.
                client.CallMethod(creature.EntityId, new NPCInfoPacket(creature.Npc.NpcPackageId));
                NpcManager.Instance.UpdateConversationStatus(client, creature);
            }

            // give some weapon to creature's
            GiveWeapon(creature);
        }

        internal Creature CreateScenarioCreature(
            SpawnPool spawnPool,
            uint creatureId,
            Vector3 position,
            double rotation)
        {
            if (!LoadedCreatures.TryGetValue(creatureId, out var template) ||
                template == null)
                return null;

            if (!EntityClassManager.Instance.LoadedEntityClasses.TryGetValue(
                    template.EntityClass,
                    out var entityClass) ||
                entityClass == null ||
                entityClass.Augmentations == null ||
                !entityClass.Augmentations.Contains(AugmentationType.Creature))
                return null;

            var creature = (Creature)template.Clone();
            creature.SpawnPool = spawnPool;
            creature.State = CharacterState.Idle;
            creature.Name = entityClass.ClassName;
            EnsureScenarioAttributes(creature);
            SetLocation(creature, position, rotation, spawnPool?.MapContextId ?? creature.MapContextId);
            creature.Controller.CurrentAction = BehaviorManager.BehaviorActionWander;
            creature.Controller.ActionWander.State = BehaviorManager.WanderIdle;
            if (spawnPool != null)
                SpawnPoolManager.Instance.IncreaseAliveCreatureCount(spawnPool);
            return creature;
        }

        internal void SetScenarioInteractionEnabled(
            MapChannel mapChannel,
            Creature creature,
            bool enabled)
        {
            if (mapChannel == null || creature == null)
                return;

            creature.IsInteractable = enabled;
            if (creature.Npc == null)
                return;

            foreach (var client in mapChannel.ClientList
                         .Where(client => client?.Player?.MapChannel == mapChannel &&
                             client.State == ClientState.Ingame)
                         .ToArray())
                NpcManager.Instance.UpdateConversationStatus(client, creature);
        }

        private static void EnsureScenarioAttributes(Creature creature)
        {
            if (creature.Attributes.Count != 0)
                return;

            var body = (int)Math.Max(15, creature.Level * 3);
            var health = (int)Math.Max(100, creature.MaxHitPoints);
            creature.Attributes.Add(Attributes.Body, new ActorAttributes(Attributes.Body, body, body, body, 5, 1000));
            creature.Attributes.Add(Attributes.Mind, new ActorAttributes(Attributes.Mind, body, body, body, 5, 1000));
            creature.Attributes.Add(Attributes.Spirit, new ActorAttributes(Attributes.Spirit, body, body, body, 5, 1000));
            creature.Attributes.Add(Attributes.Health, new ActorAttributes(Attributes.Health, health, health, health, 10, 1000));
            creature.Attributes.Add(Attributes.Chi, new ActorAttributes(Attributes.Chi, 0, 0, 0, 0, 0));
            creature.Attributes.Add(Attributes.Power, new ActorAttributes(Attributes.Power, 0, 0, 0, 0, 0));
            creature.Attributes.Add(Attributes.Aware, new ActorAttributes(Attributes.Aware, 0, 0, 0, 0, 0));
            creature.Attributes.Add(Attributes.Armor, new ActorAttributes(Attributes.Armor, 100, 100, 100, 5, 1000));
            creature.Attributes.Add(Attributes.Speed, new ActorAttributes(Attributes.Speed, 1, 1, 1, 0, 0));
            creature.Attributes.Add(Attributes.Regen, new ActorAttributes(Attributes.Regen, 0, 0, 0, 0, 0));
        }

        public void CreatureInit()
        {
            using var unitOfWork = _gameUnitOfWorkFactory.CreateWorld();
            var creatureList = unitOfWork.Creatures.Get();
            var creatureActions = unitOfWork.Creatures.GetCreatureActions();

            var vendorsList = unitOfWork.Creatures.GetVendors();
            var vendorItemList = unitOfWork.Creatures.GetVendorItems();

            foreach (var data in creatureList)
            {
                var appearanceData = unitOfWork.Creatures.GetCreatureAppearances(data.Id);
                var tempAppearanceData = new Dictionary<EquipmentData, AppearanceData>();
                var augmentationsList = EntityClassManager.Instance.LoadedEntityClasses[(EntityClasses)data.ClassId].Augmentations;
                var actions = new List<CreatureAction>();
                Npc isNpc = null;
                var isVendor = false;
                var isAuctioneer = false;
                var isClanManager = data.NameId == 8838 ? true : false;

                foreach (var aug in augmentationsList)
                {
                    switch (aug)
                    {
                        case AugmentationType.Creature:
                            break;
                        case AugmentationType.NPC:
                            isNpc = new Npc();
                            break;
                        case AugmentationType.Vendor:
                            isVendor = true;
                            break;
                        case AugmentationType.Auctioneer:
                            isAuctioneer = true;
                            break;
                        case AugmentationType.Harvestable:
                            /* can be looted???
                             * ToDo
                             */
                            break;
                        default:
                            Logger.WriteLog(LogType.Error, $"Unsuported Augmentation {aug}");
                            break;
                    }
                }

                if (appearanceData != null && appearanceData.Count > 0)
                    foreach (var t in appearanceData)
                        tempAppearanceData.Add((EquipmentData)t.SlotId, new AppearanceData { SlotId = (EquipmentData)t.SlotId, Class = t.ClassId, Color = new Color(t.Color), Hue2 = new Color(2139062144) });

                var creature = new Creature(data)
                {
                    AppearanceData = tempAppearanceData,
                };

                // load Creature Actions
                if (data.Action1 != 0)
                    creature.Actions.Add(new CreatureAction(creatureActions[data.Action1]));
                if (data.Action2 != 0)
                    creature.Actions.Add(new CreatureAction(creatureActions[data.Action2]));
                if (data.Action3 != 0)
                    creature.Actions.Add(new CreatureAction(creatureActions[data.Action3]));
                if (data.Action4 != 0)
                    creature.Actions.Add(new CreatureAction(creatureActions[data.Action4]));
                if (data.Action5 != 0)
                    creature.Actions.Add(new CreatureAction(creatureActions[data.Action5]));
                if (data.Action6 != 0)
                    creature.Actions.Add(new CreatureAction(creatureActions[data.Action6]));
                if (data.Action7 != 0)
                    creature.Actions.Add(new CreatureAction(creatureActions[data.Action7]));
                if (data.Action8 != 0)
                    creature.Actions.Add(new CreatureAction(creatureActions[data.Action8]));

                if (isNpc != null)
                    creature.Npc = isNpc;

                if (isAuctioneer)
                    creature.Npc.NpcIsAuctioneer = true;

                if (isNpc != null && ClassTrainers.Trains(data.Id))
                    creature.Npc.NpcIsTrainer = true;

                if (isClanManager)
                    creature.Npc.NpcIsClanMaster = true;

                if (isVendor)
                {
                    // Load vendorPackageId
                    foreach (var vendor in vendorsList)
                        if (vendor.Id == data.Id)
                        {
                            creature.Npc.Vendor = new Vendor(vendor.PackageId);
                            break;
                        }

                    // Load vendorItems
                    foreach (var vendorItem in vendorItemList)
                        if (vendorItem.Id == data.Id)
                            creature.Npc.Vendor.VendorItems.Add(vendorItem.ItemTemplateId);
                }

                // add mission data to npc's
                foreach (var entry in MissionApplication.Instance.LoadedMissions)
                {
                    var mission = entry.Value;

                    if (mission.MissionGiver == data.Id || mission.MissionReciver == data.Id)
                    {
                        if (creature.Npc.NpcMissionIds != null)
                        {
                            creature.Npc.NpcMissionIds.Add(mission.MissionId);
                        }
                        else
                        {
                            creature.Npc.NpcMissionIds = new List<uint>
                                {
                                    mission.MissionId
                                };
                        }
                    }
                }
                LoadedCreatures.Add(creature.DbId, creature);
            }

            var npcPackages = unitOfWork.NpcPackages.Get();
            foreach (var package in npcPackages)
            {
                if (LoadedCreatures.ContainsKey(package.Id))
                {
                    foreach (var aug in EntityClassManager.Instance.LoadedEntityClasses[LoadedCreatures[package.Id].EntityClass].Augmentations)
                    {
                        if (aug == AugmentationType.NPC)
                        {
                            LoadedCreatures[package.Id].Npc.NpcPackageId = package.PackageId;
                            break;
                        }
                    }
                }
                else
                {
                    Logger.WriteLog(LogType.Error, $"LoadNPCPackages: unknown creatureDbId = {package.Id}");

                    MapErrorManager.Instance.Record(
                        $"NPC package {package.PackageId} names creature {package.Id}, which is not in the database.");
                }
            }
        }

        public void CellDiscardCreaturesToClient(Client client, List<Creature> discardCreatures)
        {
            foreach (var creature in discardCreatures)
                client.CallMethod(SysEntity.ClientMethodId, new DestroyPhysicalEntityPacket(creature.EntityId));
        }

        public void SetLocation(Creature creature, Vector3 position, double orientation, uint mapContextId)
        {
            // set spawnlocation
            creature.Position = position;
            creature.Rotation = orientation;
            creature.MapContextId = mapContextId;
            // set home location
            creature.HomePos.Position = position;
            creature.HomePos.MapContextid = mapContextId;
            //allocate pathnodes
            //creature->pathnodes = (baseBehavior_baseNode*)malloc(sizeof(baseBehavior_baseNode));
            //memset(creature->pathnodes, 0x00, sizeof(baseBehavior_baseNode));
            //creature->lastattack = GetTickCount();
            //creature->lastresttime = GetTickCount();
        }

        public void UpdateCreatureAppearance(Creature creature)
        {
            var mapChannel = creature?.RuntimeMapChannel;
            if (mapChannel == null)
                return;
            CellManager.Instance.CellCallMethod(mapChannel, creature, new AppearanceDataPacket(creature.AppearanceData));
        }

        internal void CreateOrUpdateAppearance(Creature creature, AppearanceData appearanceData)
        {
            using var unitOfWork = _gameUnitOfWorkFactory.CreateWorld();

            if (!creature.AppearanceData.ContainsKey(appearanceData.SlotId))
                creature.AppearanceData.Add(appearanceData.SlotId, appearanceData);

            creature.AppearanceData[appearanceData.SlotId].Color = appearanceData.Color;

            if (appearanceData.Class > 0)
                creature.AppearanceData[appearanceData.SlotId].Class = appearanceData.Class;

            // update creature appearance on client's
            CellManager.Instance.CellCallMethod(creature, new AppearanceDataPacket(creature.AppearanceData));

            // update creature in database
            unitOfWork.Creatures.CreateOrUpdateAppearance(creature.DbId, (uint)appearanceData.SlotId, appearanceData.Class, appearanceData.Color.Hue);

            Logger.WriteLog(LogType.Debug, "Creature Look updated");
        }
    }
}
