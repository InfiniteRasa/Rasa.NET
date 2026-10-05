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

            var flags = EntityClassManager.Instance.LoadedEntityClasses
                .TryGetValue(creature.EntityClass, out var entityClass) && entityClass != null
                ? entityClass.CreatureFlags.ConvertAll(f => (int)f)
                : new List<int>();

            if (creature.ExtraFlags != null)
                foreach (var extra in creature.ExtraFlags)
                    if (!flags.Contains((int)extra))
                        flags.Add((int)extra);

            return flags;
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

            // A summoned minion wears what it was given: a clone of an unarmed player stays unarmed.
            if (creature.MasterEntityId != 0)
                return;

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

        /// <summary>
        /// A dead creature's health and armour: at zero, with nothing coming back. The clients are
        /// told its health when it was not at zero already - a caller that brought it there has
        /// sent that itself.
        /// </summary>
        private static void ZeroVitals(MapChannel mapChannel, Creature creature)
        {
            if (creature.Attributes.TryGetValue(Attributes.Health, out var health))
            {
                var hadHealth = health.Current != 0;

                health.Current = 0;
                health.RefreshAmount = 0;
                health.RefreshPeriod = 0;

                if (hadHealth)
                    CellManager.Instance.CellCallMethod(mapChannel, creature, new UpdateHealthPacket(health, creature.EntityId));
            }

            if (creature.Attributes.TryGetValue(Attributes.Armor, out var armor))
            {
                armor.Current = 0;
                armor.RefreshAmount = 0;
                armor.RefreshPeriod = 0;
            }
        }

        /// <param name="critKill">A Critical Death finish: the experience and adrenaline are paid twice over, the client is told how the second award was earned, and the body cannot be revived.</param>
        internal void HandleCreatureKill(MapChannel mapChannel, Creature creature, Actor killedBy, CritKill critKill = CritKill.None)
        {
            if (creature.State == CharacterState.Dead || Game.Missions.World.CreatureGameplayRules.IsInvulnerable(creature))
                return; // creature already dead

            // A crab mine killed: it goes off where it fell, and is nobody's kill or loot.
            if (creature.IsScripted && AbilityManager.IsCrabMine(creature))
            {
                AbilityManager.Instance.CrabMineKilled(mapChannel, creature);
                return;
            }

            // A trap destroyed strikes back at whoever did it; a turret is simply gone. Neither is
            // anybody's kill or loot.
            if (creature.IsScripted && AbilityManager.IsTrap(creature))
            {
                AbilityManager.Instance.TrapKilled(mapChannel, creature, killedBy);
                return;
            }

            // A reality ripper destroyed: it closes and lets everything go.
            if (creature.IsScripted && AbilityManager.IsRealityRipper(creature))
            {
                AbilityManager.Instance.RealityRipperKilled(mapChannel, creature);
                return;
            }

            // A finishing move destroys the body. Recorded before anything else looks at the
            // death, so the self revive below and a Caretaker's revive later both see it.
            creature.CritKilled = critKill != CritKill.None;

            // A Machina's first death in a life is not its end (CreatureSupport): it goes down,
            // and gets up again - unless it was finished, which is its end whatever it had left.
            if (creature.CritKilled)
                CreatureSupport.ForgetSelfRevive(creature);
            else if (CreatureSupport.DefersDeath(mapChannel, creature))
                return;

            // Killed by something fighting for a player - a trap's shot, a creature turned by
            // Traitor, a minion: the kill is that player's, experience, adrenaline, loot and
            // harvest rights alike. The blow stays the creature's for threat.
            if (killedBy is Creature planted && planted.MasterEntityId != 0
                && EntityManager.Instance.Players.TryGetValue(planted.MasterEntityId, out var master)
                && MapInstanceScope.Contains(mapChannel, master))
                killedBy = master;

            var policy = Game.Missions.World.CreatureGameplayRules.Policy(creature);
            var participant = creature.CombatParticipant;
            var canReward = creature.TargetCategory != TargetCategory.Friendly &&
                Game.Missions.World.CreatureGameplayRules.RewardsKills(creature);

            // kill creature
            var stateIds = new List<CharacterState> { CharacterState.Dead };

            creature.State = CharacterState.Dead;
            creature.KnockbackTo = null;

            // Dead is at zero health, for every caller. All but Critical Death arrive with that
            // done. A creature killed out of its window - left to die, or finished - arrives
            // with the 1 to 8 percent the window opened on, and was left with it: a body that
            // BehaviorManager.CreatureThink, which knew a corpse by its health, never put on the
            // corpse clock and went on thinking for.
            ZeroVitals(mapChannel, creature);

            Game.Missions.World.CreatureGameplayRules.ClearRole(creature);
            CellManager.Instance.CellCallMethod(mapChannel, creature, new StateChangePacket(stateIds));
            if (creature.SpawnPool?.FollowOwnerCharacterId > 0)
                PublishEscortStatus(mapChannel, creature, false);

            // A turret blows up into its wreck; a Predator or Ravager is its wreck once it has
            // fallen (AlternateMesh).
            AlternateMesh.OnDeath(mapChannel, creature);

            // A debuff does not outlive what it was on: a Ruin still ticking on a corpse would
            // try to damage it every second until it expired.
            GameEffectManager.Instance.ClearEffects(mapChannel, creature);

            // Nor does a grudge.
            creature.Hate.Clear();

            // A creature that explodes when it dies does so now (CreatureBombs): a Warden's
            // blast, a Howler's or a Predator's bomb.
            CreatureBombs.OnDeath(mapChannel, creature);

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

            // get client if it's killed by player
            foreach (var cell in CellManager.CellsIn(mapChannel, killedBy?.Cells))
                foreach (var candidate in cell.ClientList)
                    if (candidate.Player == killedBy && MapInstanceScope.Contains(mapChannel, killedBy))
                    {
                        client = candidate;
                        break;
                    }

            // A mission escort's kill is its owner's, and a scene defender's is the player who
            // fought beside it.
            if (client == null && killedBy is Creature killer && killer.TargetCategory != creature.TargetCategory)
                client = FindEscortOwner(mapChannel, killer);
            if (client == null && killedBy is Creature defender &&
                Game.Missions.World.CreatureGameplayRules.IsDefender(defender) && policy.TrackParticipation &&
                defender.TargetCategory != creature.TargetCategory && IsLivingOnMap(mapChannel, defender))
                client = FindCombatPlayer(mapChannel, participant);
            creature.CombatParticipant = null;
            canReward &= client != null &&
                (!mapChannel.IsPrivateInstance || mapChannel.OwnerCharacterId == client.Player.Id);

            if (client != null && canReward)
            {
                // give experience
                var experience = creature.Level * 100; // base experience
                var experienceRange = creature.Level * 10;
                experience += (uint)(Random.Shared.Next() % (experienceRange * 2 + 1)) - experienceRange;

                // todo: Depending on level difference reduce experience
                _manifestationManager.GainExperience(client, experience);

                // A finishing move pays the kill over again: "You get full experience for killing
                // the enemy, and you get full experience again at the end of the Finishing Move.
                // This means you get double the experience and Adrenaline for the kill" (the
                // strategy guide). Paid as a second award flagged as the crit kill, so the client
                // prints the ordinary line and then its "by Crit Killing" line, one for each.
                if (critKill != CritKill.None)
                    _manifestationManager.GainExperience(client, experience, critKill);

                // Adrenaline is earned here and nowhere else: it does not regenerate. See
                // ManifestationManager.AdrenalinePerKillPercent. Doubled for a finish, in one
                // award rather than two, so the bar shows one number rather than two on top of
                // each other.
                var adrenaline = _manifestationManager.AdrenalineForKill(client);

                _manifestationManager.GainAdrenaline(client, critKill != CritKill.None ? adrenaline * 2 : adrenaline);

                // One of a control point's Bane garrison is worth prestige as well (ControlPoints).
                ControlPoints.Instance.CreatureKilled(creature, client);
            }

            // The corpse is harvestable by whoever earned it, a fixed number of times. Set here
            // rather than at the first harvest so that a creature that died without a player
            // behind it - a minion's kill, a fall, a despawn - is left at zero and nobody can
            // harvest it at all.
            //
            // Written on every kill and not only on a claimed one: a spawn pool puts the same
            // Creature back on its feet, so a claim left over from a previous life would still be
            // sitting there the next time it died to something that was not a player, and that
            // player would be handed a corpse they did not earn.
            // A turret or a pet another creature brought in is nothing to loot or harvest
            // (CreatureSummons): only its experience is earned.
            var summoned = CreatureSummons.IsSummoned(creature);
            var earned = canReward && client != null && !summoned;

            creature.HarvestOwnerEntityId = earned ? client.Player.EntityId : 0;
            creature.HarvestAttemptsLeft = earned ? Harvest.AttemptsPerCorpse : 0;

            // spawn loot
            if (killedBy != null && earned)
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
            var missions = _missionManager ?? MissionApplication.Instance;
            if (creature.SpawnPool?.SceneRunId != null)
            {
                var credited = progressClient != null && CanCreditScenarioProgress(mapChannel, creature, progressClient);
                missions.Scenes.RecordDefeat(mapChannel, creature, credited ? progressClient : null);

                // The scene has the kill of its own actor; what kind of creature it was is
                // anyone's count.
                if (credited)
                    missions.Credit.RecordKill(progressClient, KillEvents(creature, false), creature.Position);
            }
            else
            {
                missions.Scenes.PublicActorDied(mapChannel, creature);
                if (progressClient != null && CanCreditScenarioProgress(mapChannel, creature, progressClient))
                    missions.Credit.RecordKill(progressClient, KillEvents(creature), creature.Position);
            }

            // A boss of a battlefield's title is recorded for whoever has the kill (BossTitles).
            if (progressClient != null && CanCreditScenarioProgress(mapChannel, creature, progressClient))
                BossTitles.Killed(progressClient, mapChannel, creature);
        }

        /// <summary>
        /// The names a kill goes by in mission progress: the creature row ("Defeat Tizzik"), its
        /// entity class, and each creature flag of its class - its species among them, which is
        /// what "Kill 40 Xanx" counts.
        /// </summary>
        internal static IReadOnlyList<MissionProgressEvent> KillEvents(Creature creature, bool byCreature = true)
        {
            var events = new List<MissionProgressEvent>();

            if (byCreature && creature.DbId != 0)
                events.Add(MissionProgressEvent.Creature(creature.DbId));

            if (creature.EntityClass != 0)
                events.Add(MissionProgressEvent.CreatureClass((uint)creature.EntityClass));

            foreach (var flag in CreatureFlagsOf(creature))
                if (flag > 0)
                    events.Add(MissionProgressEvent.CreatureFlag((uint)flag));

            return events;
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
            actor != null && actor.State is not (CharacterState.Dead or CharacterState.Dying) &&
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
            Game.Missions.World.CreatureGameplayRules.CanParticipateInCombat(target) &&
            (source is Creature attacker
                ? BehaviorManager.MayFight(attacker, target.EntityId)
                : TargetCategories.PlayerMayAttack(target.TargetCategory));

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
                leases.BindCombatGate(creature, spawnPool);
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
                // Armour regenerates (CreatureArmor): its rate is sent from the start.
                creature.Attributes.Add(Attributes.Armor, new ActorAttributes(Attributes.Armor, creatureStats.Armor, creatureStats.Armor, creatureStats.Armor, CreatureArmor.BaseRegen(creatureStats.Armor), CombatRegen.RegenPeriodSeconds));
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
                creature.Attributes.Add(Attributes.Armor, new ActorAttributes(Attributes.Armor, 100, 100, 100, CreatureArmor.BaseRegen(100), CombatRegen.RegenPeriodSeconds));
                creature.Attributes.Add(Attributes.Speed, new ActorAttributes(Attributes.Speed, 1, 1, 1, 0, 0));
                creature.Attributes.Add(Attributes.Regen, new ActorAttributes(Attributes.Regen, 0, 0, 0, 0, 0));
            }

            BehaviorManager.StartWandering(creature, true);

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

            // remove creature for player that are not in visibility range anymore - unless it is
            // who one of their missions sends them to speak to, which their client keeps (MissionContacts)
            foreach (var cellSeed in needDelete)
                foreach (var client in mapChannel.MapCellInfo.Cells[cellSeed].ClientList)
                    if (!MissionContacts.Keep(client, creature))
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

            // One this client was given from afar is taken off it first: a second
            // CreatePhysicalEntity would be an update, and leave it without its overhead icon
            // (MissionContacts.Entering).
            MissionContacts.Entering(client, creature);

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
                // HOSTILE to an enemy of the player it belongs to (Pvp), its own category otherwise.
                new TargetCategoryPacket(client?.Player != null ? Pvp.CategoryFor(creature, client.Player) : creature.TargetCategory),
                new UpdateAttributesPacket(creature.Attributes, 0),
                new IsRunningPacket(creature.IsRunning)
            };
            if (creature.State != CharacterState.Dead &&
                creature.SpawnPool?.FollowOwnerCharacterId is > 0 &&
                creature.SpawnPool?.FollowOwnerCharacterId == client.Player?.Id)
                entityData.Add(new UpdateEscortStatusPacket(true));

            // A clone's "Clone of %s" takes its master's name from here, and an NPC with no
            // creature name id (creature_actor_name) shows it as its whole name - which the
            // client's SetText only takes as unicode.
            if (creature.ActorName != null)
                entityData.Add(new ActorNamePacket(creature.ActorName, true));

            // What it is fighting: a turret's gun, a Stalker's or a Strider's comes round to it.
            if (Targets.Current(creature) is var target && target != 0)
                entityData.Add(new TargetIdPacket(target));

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

            // What is on it - a DoT, a mark, a minion's or a risen corpse's effect, a turret's
            // look - went out before this client was here.
            GameEffectManager.ShowEffectsTo(client, creature);

            // A turret or vehicle with a wreck: the effect that swaps its model, as the wreck
            // already when that is what it is now.
            AlternateMesh.ShowTo(client, creature);
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
            var vendorPrices = new Dictionary<uint, int>();
            foreach (var entry in unitOfWork.Creatures.GetVendorPrices())
                vendorPrices[entry.Id] = entry.ItemPrice;
            var actorNames = new Dictionary<uint, string>();
            foreach (var entry in unitOfWork.Creatures.GetActorNames())
                actorNames[entry.Id] = entry.ActorName;
            var greetings = new Dictionary<uint, uint>();
            foreach (var entry in unitOfWork.Creatures.GetNpcGreetings())
                greetings[entry.Id] = entry.GreetingId;

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

                // An NPC named by the server rather than by the client's creaturenamelanguage.
                if (actorNames.TryGetValue(data.Id, out var actorName) && !string.IsNullOrWhiteSpace(actorName))
                    creature.ActorName = actorName;

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

                // The line it greets a player with, if it has one of its own (NpcGreetings).
                if (isNpc != null && greetings.TryGetValue(data.Id, out var greetingId) && NpcGreetings.IsLine(greetingId))
                    isNpc.GreetingId = greetingId;

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

                            if (vendorPrices.TryGetValue(data.Id, out var itemPrice))
                            {
                                if (itemPrice >= 0)
                                    creature.Npc.Vendor.ItemPrice = itemPrice;
                                else
                                    Logger.WriteLog(LogType.Error, $"vendor_price for vendor {data.Id} is {itemPrice}; its stock sells at buy_price.");
                            }

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

        /// <summary>
        /// Checks that every creature_action row names an attack the client can actually draw.
        ///
        /// A row states its attack as an (action, argument) pair, and that pair is what the client
        /// resolves to decide the animation, the FX families and the timings. A pair the client has
        /// no row for does not fail: client/actions/__init__.py logs to its own console and hands
        /// back a default ActorActionInfo - no windup animation, no FX - so the attack still lands
        /// its damage and draws nothing whatsoever. Eight of the forty-four shipped rows were in
        /// that state, and the only way to find out was to stand in front of one and notice that a
        /// creature hitting you was doing it in silence.
        ///
        /// The client's own table of valid pairs is already in the database as action_level, which
        /// is what AbilityManager loads, so the check is a single lookup per row. That is also why
        /// this runs from Server after AbilityInit rather than from CreatureInit: the table it
        /// checks against does not exist yet at the point the creatures are read.
        ///
        /// Reported once per row rather than once per creature - one bad row is usually shared by
        /// a whole family - and server-wide, because a creature is not tied to a map until it is
        /// spawned and MapErrorManager shows server-wide entries on every map anyway.
        /// </summary>
        public void ValidateActions()
        {
            var seen = new HashSet<uint>();
            var bad = 0;

            foreach (var creature in LoadedCreatures.Values)
            {
                foreach (var action in creature.Actions)
                {
                    if (!seen.Add(action.Id))
                        continue;

                    if (AbilityManager.Instance.TryGetLevel(action.ActionId, action.ActionArgId, out _))
                        continue;

                    bad++;

                    Logger.WriteLog(LogType.Error,
                        $"creature_action {action.Id} ({action.Description}) performs {(uint)action.ActionId}/{action.ActionArgId}, " +
                        "which the client has no action data for: it will deal its damage and draw nothing.");

                    MapErrorManager.Instance.Record(
                        $"creature_action {action.Id} ({action.Description}) names {(uint)action.ActionId}/{action.ActionArgId}, which the client cannot draw.");
                }
            }

            Logger.WriteLog(LogType.Initialize,
                $"CreatureActions = {seen.Count}, undrawable = {bad}");
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
