using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.MapChannel.Client;
    using Packets.MapChannel.Server;
    using Structures;

    /// <summary>
    /// Hortimonculus (AA_EXOBIOLOGIST_HORTIMUNCULUS 185, abilities.hortimonculus): "Creates a plant
    /// lifeform from a single enemy corpse that will heal the user and nearby squad members at a
    /// set interval for as long as the plant is alive. Plant will increase player resistances
    /// based on highest resistance type of the target creature. Plant will also directly heal any
    /// player with direct use over a set time. Plant will decay over time" (uielement 1242).
    ///
    /// From the client and its data:
    /// - HortimonculusAction sets canTargetDead and allows only a dead player or a dead BIOLOGICAL
    ///   creature;
    /// - the plant is UsableAbilityHortimonculus (10000053), a DestroyableStatelessSwitch: a
    ///   usable with hit points (Recv_DamageInfo / Recv_UpdateHitPoints) whose states are
    ///   POWER_DOWN (171) → POWER_UP (172, the birth: creature_birth_hortimunculus) → DESTROYED
    ///   (2, state_death_hortimunculus). Only its owner and the owner's squad may use it
    ///   (IsUsable); the owner is the source of HORTIMONCULUS_SOURCE (226) on it, which the effect
    ///   hands to SetOwnerId;
    /// - HortimonculusSourceEffect.OnTick(buffIds, healData) announces HORTIMONCULUS_BUFF (225,
    ///   "Hortimonculus Protection") on buffIds and floats healData as healing from the plant;
    /// - using the plant is USE_OBJECT arg 1 (its usabledata row), answered by the plant performing
    ///   OBJ_EFFECT (10000003) with HORTIMONCULUS_USE (10000058), a HealOverTime, on the user;
    ///   OBJ_EFFECT's GAME_EFFECT_ARG1/2 are 15 and 3;
    /// - per pump: HEALTH_PERCENTAGE 100-500 (the plant's health, of the corpse's maximum),
    ///   DECAY_PERCENTAGE 20-4 (what it heals, of its own maximum, every INTERVAL of 5 s),
    ///   RESIST_PERCENTAGE 10-50, EFFECT_RADIUS 20 m. The tooltip's lifetimes - about 20 s, 25 s,
    ///   35 s, 1 min, 2 min - are what the health lasts at that decay.
    ///
    /// The server's part:
    /// - the corpse must be a dead, BIOLOGICAL, HOSTILE or NEUTRAL creature that nothing else has claimed;
    ///   it is given up for despawn as the plant grows from it, as Cadaver Immolation's is. Or a
    ///   dead enemy player across a wargame (Pvp): the plant grows where they lie, at their
    ///   health, resisting their weapon's damage type, and they are sent to their nearest
    ///   hospital (PlayerDeath.ForceToHospital);
    /// - every INTERVAL the plant heals the owner and their squad within EFFECT_RADIUS by
    ///   DECAY_PERCENTAGE of its maximum health each, and loses that much health itself;
    /// - while in reach they carry HORTIMONCULUS_BUFF: +RESIST_PERCENTAGE resistance to the
    ///   corpse's damage type (CreatureAttacks.DamageTypeOf its first attack - creatures carry no
    ///   resistances of their own, and the type they deal is the one they are built around);
    /// - using it heals the user by up to the plant's current health, what they are missing,
    ///   over GAME_EFFECT_ARG1 seconds in ticks every GAME_EFFECT_ARG2 seconds, and takes that
    ///   from the plant;
    /// - at no health, or when its owner leaves the map, it dies (DESTROYED) and is taken away
    ///   PlantRemoveMs later.
    ///
    /// Attacked (DestroyableStatelessSwitch: "a Usable that does nothing, but which can be
    /// destroyed"; to anyone but its owner and their squad it is of the owner's category, and
    /// it is damageable): every client is told canBeDamaged, so an enemy can target it.
    /// - a player who is an enemy of its owner's across a wargame (Pvp), or a creature of such a
    ///   player's, may hit it: a weapon's shot or swing (MissileManager) and a constant-fire
    ///   weapon's pulse (ConstantFire) land on its hit points as they are, as on a Personal
    ///   Waypoint's, and the attacker's PvP Safety comes off;
    /// - a creature fights it by its hate table (Threat): the plant's healing - its ticks and a
    ///   direct use - draws the hate of the creatures that hate whoever it healed to the plant,
    ///   not to its owner, and a creature that hates the plant most fights it, walking up to it
    ///   as to a Personal Waypoint. Any combatant creature that could fight its owner may; none
    ///   goes looking for it unprovoked (no aggro scan finds it);
    /// - at no hit points it dies as it does when it has decayed to none.
    /// </summary>
    public partial class AbilityManager
    {
        public const string HortimonculusModule = "abilities.hortimonculus";
        public const EntityClasses HortimonculusClass = (EntityClasses)10000053;   // UsableAbilityHortimonculus
        public const int HortimonculusBuffTypeId = 225;                           // HORTIMONCULUS_BUFF
        public const int HortimonculusSourceTypeId = 226;                         // HORTIMONCULUS_SOURCE
        public const int HortimonculusUseTypeId = 10000058;                       // HORTIMONCULUS_USE
        public const uint HortimonculusUseArgId = 1;                              // usabledata: USE_OBJECT arg

        /// <summary>How long the dead plant stays for its death to play.</summary>
        public const int PlantRemoveMs = 3000;

        private sealed class Plant
        {
            public MapChannel MapChannel;
            public DynamicObject Object;
            public Manifestation Owner;
            public Actor Corpse;
            public GameEffect Source;
            public int MaxHealth;
            public int Health;
            public int HealAmount;
            public float Radius;
            public int IntervalMs;
            public int ResistPercent;
            public DamageType ResistType;
            public uint Level;
            public long NextTickAt;
            public long RemoveAt;
            public int UseDurationMs;
            public int UseIntervalMs;
            public readonly Dictionary<Manifestation, GameEffect> Buffed = new Dictionary<Manifestation, GameEffect>();
        }

        private static readonly List<Plant> Plants = new List<Plant>();
        private static readonly object PlantsLock = new object();

        /// <summary>The client modules whose target is a corpse (canTargetDead).</summary>
        private static readonly HashSet<string> CorpseModules = new HashSet<string> { "abilities.corpseexplode", HortimonculusModule, ReanimationModule };

        /// <summary>The plant's health: HEALTH_PERCENTAGE of the corpse's maximum.</summary>
        public static int PlantHealth(int corpseMaxHealth, int healthPercent)
        {
            return Math.Max(1, (int)Math.Round(Math.Max(1, corpseMaxHealth) * Math.Max(1, healthPercent) / 100.0));
        }

        /// <summary>What it heals, and loses, each interval: DECAY_PERCENTAGE of its maximum health.</summary>
        public static int PlantHeal(int plantMaxHealth, int decayPercent)
        {
            return Math.Max(1, (int)Math.Round(plantMaxHealth * Math.Max(1, decayPercent) / 100.0));
        }

        /// <summary>A direct use's healing: what the user is missing, up to what the plant has left.</summary>
        public static int PlantUseHeal(int plantHealth, int missingHealth)
        {
            return Math.Max(0, Math.Min(plantHealth, missingHealth));
        }

        /// <summary>A share of the remaining pool for this tick: the pool spread over the ticks left.</summary>
        public static int PlantUseTick(int pool, int ticksLeft)
        {
            return pool <= 0 ? 0 : ticksLeft <= 1 ? pool : (int)Math.Ceiling(pool / (double)ticksLeft);
        }

        /// <summary>
        /// Whether a corpse ability may be aimed at this body: dead, biological, an enemy's, and not
        /// already used. A player's body is one when they are dead and an enemy of the one using it
        /// across a wargame (Pvp) - the client's checks allow any dead player.
        /// </summary>
        internal static bool IsUsableCorpse(Actor target, Manifestation by = null)
        {
            if (target is Manifestation fallen)
                return by != null && fallen.State == CharacterState.Dead && fallen.MapChannel != null
                       && Pvp.AreEnemies(by, fallen) && !IsCorpseInUse(fallen);

            if (!(target is Creature corpse) || !IsBiologicalCorpse(corpse) || !TargetCategories.PlayerMayAttack(corpse.TargetCategory) || corpse.IsScripted)
                return false;

            return !IsCorpseInUse(corpse) && !CreatureBombs.IsCorpseClaimed(corpse);
        }

        /// <summary>Whether a player's corpse ability has this body: Cadaver Immolation burning it, a Hortimonculus growing from it.</summary>
        internal static bool IsCorpseInUse(Actor corpse)
        {
            lock (BurningCorpsesLock)
                if (BurningCorpses.Any(c => c.Corpse == corpse))
                    return true;

            lock (PlantsLock)
                return Plants.Any(p => p.Corpse == corpse);
        }

        /// <summary>The plant this object is, or null.</summary>
        private static Plant PlantOf(ulong entityId)
        {
            lock (PlantsLock)
                return Plants.FirstOrDefault(p => p.Object.EntityId == entityId);
        }

        public static bool IsHortimonculus(ulong entityId) => PlantOf(entityId) != null;

        /// <summary>Grows the plant from the corpse targeted.</summary>
        private void GrowHortimonculus(MapChannel mapChannel, Manifestation player, ActionLevelInfo info, ActionData action)
        {
            var recovery = new AbilityRecoveryPacket(action.ActionId, action.ActionArgId, AbilityRecoveryPacket.HitDataKind.None);
            var corpse = action.TargetId != 0 ? ResolveTarget(mapChannel, action.TargetId) : null;

            Plant grown = null;

            if (IsUsableCorpse(corpse, player))
            {
                grown = Grow(mapChannel, player, corpse, info);
                Hit(recovery, corpse);
            }

            CellManager.Instance.CellCallMethod(mapChannel, player, recovery);

            // An enemy player's body taken: they are sent to their nearest hospital (PlayerDeath),
            // and the plant holds no body of theirs after.
            if (grown != null && corpse is Manifestation fallen)
            {
                grown.Corpse = null;
                PlayerDeath.ForceToHospital(mapChannel, fallen);
            }
        }

        private Plant Grow(MapChannel mapChannel, Manifestation player, Actor corpse, ActionLevelInfo info)
        {
            var now = Environment.TickCount64;
            var level = Math.Max(1u, info.Level);
            var corpseMax = corpse.Attributes.TryGetValue(Attributes.Health, out var corpseHealth) ? corpseHealth.CurrentMax
                : corpse is Creature creatureCorpse ? (int)creatureCorpse.MaxHitPoints : 1;
            var maxHealth = PlantHealth(corpseMax, info.Get(AbilityProperty.HealthPercentage, 100));
            var resistType = DamageTypeOfBody(mapChannel, corpse);

            var source = new GameEffect
            {
                TypeId = HortimonculusSourceTypeId,
                EffectId = GameEffectManager.Instance.NextEffectId(mapChannel),
                EffectLevel = level,
                ActionId = ActionId.AaExobiologistHortimunculus,
                // HortimonculusSourceEffect.OnAnnounceAttach: SetOwnerId(sourceId) - who may use it.
                SourceId = player.EntityId,
                Source = player,
                SourceLevel = player.Level,
                AnnounceOnAttach = true
            };

            var useInfo = TryGetLevel(ActionId.ObjEffect, 1, out var objEffect) ? objEffect : null;

            var plant = new Plant
            {
                MapChannel = mapChannel,
                Owner = player,
                Corpse = corpse,
                Source = source,
                MaxHealth = maxHealth,
                Health = maxHealth,
                HealAmount = PlantHeal(maxHealth, info.Get(AbilityProperty.DecayPercentage, 20)),
                Radius = info.Get(AbilityProperty.EffectRadius, 20),
                IntervalMs = Math.Max(1, info.Get(AbilityProperty.Interval, 5)) * 1000,
                ResistPercent = info.Get(AbilityProperty.ResistPercentage, 10),
                ResistType = resistType == 0 ? DamageType.Physical : resistType,
                Level = level,
                UseDurationMs = Math.Max(1, useInfo?.Get(AbilityProperty.GameEffectArg1, 15) ?? 15) * 1000,
                UseIntervalMs = Math.Max(1, useInfo?.Get(AbilityProperty.GameEffectArg2, 3) ?? 3) * 1000
            };

            plant.NextTickAt = now + plant.IntervalMs;

            plant.Object = new DynamicObject
            {
                EntityClassId = HortimonculusClass,
                DynamicObjectType = DynamicObjectType.Hortimonculus,
                Position = corpse.Position,
                Rotation = corpse.Rotation,
                MapContextId = corpse.MapContextId,
                TargetCategory = TargetCategory.Object,
                StateId = UseObjectState.StatePowerDown,
                IsInWorld = true,
                ObjectData = plant,
                Comment = "Hortimonculus"
            };

            // Listed before it goes into the world, so its introduction (ShowPlantTo) finds it.
            lock (PlantsLock)
                Plants.Add(plant);

            CellManager.Instance.AddToWorld(mapChannel, plant.Object);

            // The birth: POWER_DOWN → POWER_UP.
            plant.Object.StateId = UseObjectState.StatePowerUp;
            CellManager.Instance.CellCallMethod(plant.Object, new UsePacket(player.EntityId, UseObjectState.StatePowerUp, 0));

            // What grew from the body takes its place; a creature's goes the usual way, loot and all.
            if (corpse is Creature grownFrom && grownFrom.Controller != null)
                grownFrom.Controller.DeadTime = long.MaxValue / 2;

            return plant;
        }

        /// <summary>
        /// The damage type a body was built around: a creature's first attack's, a player's
        /// weapon's in hand. Physical when there is none.
        /// </summary>
        private static DamageType DamageTypeOfBody(MapChannel mapChannel, Actor corpse)
        {
            var type = DamageType.Physical;

            if (corpse is Creature creature && creature.Actions.Count > 0)
                type = CreatureAttacks.DamageTypeOf(creature.Actions[0]);
            else if (corpse is Manifestation fallen)
            {
                var client = mapChannel?.ClientList.Find(c => c?.Player == fallen);
                var weapon = client != null ? EntityClassManager.Instance.GetWeaponClassInfo(InventoryManager.Instance.CurrentWeapon(client)) : null;

                if (weapon != null)
                    type = (DamageType)weapon.DamageType;
            }

            return type == 0 ? DamageType.Physical : type;
        }

        /// <summary>
        /// What a client meeting the plant is told besides its creation: its owner, through
        /// HORTIMONCULUS_SOURCE, and its hit points. Called from DynamicObjectManager's
        /// CreateDynamicObjectOnClient, so the growth and a late arrival are told alike.
        /// </summary>
        internal static void ShowPlantTo(Client client, DynamicObject obj)
        {
            if (!(obj.ObjectData is Plant plant))
                return;

            client.CallMethod(obj.EntityId, GameEffectManager.AttachedPacket(plant.Source, plant.Object.StateId == UseObjectState.StatePowerDown));
            client.CallMethod(obj.EntityId, new UsableDamageInfoPacket(true, false, plant.MaxHealth, plant.Health));
        }

        /// <summary>The owner, and their squad, alive on this map within the plant's reach.</summary>
        private static List<Manifestation> SquadNearPlant(Plant plant)
        {
            var owner = plant.Owner;
            var found = new List<Manifestation>();

            foreach (var client in plant.MapChannel.ClientList)
            {
                var member = client?.Player;

                if (member == null || member.MapContextId != plant.Object.MapContextId || member.State == CharacterState.Dead)
                    continue;

                if (member != owner && (owner.PartyId == 0 || member.PartyId != owner.PartyId))
                    continue;

                if (!member.Attributes.TryGetValue(Attributes.Health, out var health) || health.Current <= 0)
                    continue;

                if (Vector3.Distance(member.Position, plant.Object.Position) <= plant.Radius)
                    found.Add(member);
            }

            return found;
        }

        /// <summary>Runs the plants on this map: heal, protect, decay, die, go.</summary>
        internal void HortimonculusWorker(MapChannel mapChannel)
        {
            List<Plant> plants;

            lock (PlantsLock)
                plants = Plants.Where(p => p.MapChannel == mapChannel).ToList();

            if (plants.Count == 0)
                return;

            var now = Environment.TickCount64;

            foreach (var plant in plants)
            {
                try
                {
                    if (plant.RemoveAt != 0)
                    {
                        if (now >= plant.RemoveAt)
                            RemovePlant(plant);

                        continue;
                    }

                    // Its owner has left the map, or the game.
                    if (plant.Owner.MapChannel != mapChannel || plant.Owner.MapContextId != plant.Object.MapContextId
                        || !mapChannel.ClientList.Any(c => c?.Player == plant.Owner))
                    {
                        Wither(plant, now);
                        continue;
                    }

                    if (now < plant.NextTickAt)
                        continue;

                    plant.NextTickAt += plant.IntervalMs;
                    PlantTick(plant, now);
                }
                catch (Exception e)
                {
                    Logger.WriteLog(LogType.Error, $"Hortimonculus {plant.Object.EntityId} on map {mapChannel.MapInfo.MapContextId} threw and was removed: {e}");
                    RemovePlant(plant);
                }
            }
        }

        private void PlantTick(Plant plant, long now)
        {
            var mapChannel = plant.MapChannel;
            var inReach = SquadNearPlant(plant);
            var tick = new GameEffectTickPacket(plant.Source.EffectId, GameEffectTickPacket.TickKind.Hortimonculus);

            // Out of reach, or gone: the protection comes off.
            foreach (var (member, buff) in plant.Buffed.ToList())
                if (!inReach.Contains(member) || !member.ActiveEffects.ContainsKey(buff.EffectId))
                {
                    if (member.ActiveEffects.ContainsKey(buff.EffectId))
                        GameEffectManager.Instance.DettachEffect(mapChannel, member, buff);

                    plant.Buffed.Remove(member);
                }

            foreach (var member in inReach)
            {
                // One plant's protection at a time; another's, already on them, stands.
                if (!plant.Buffed.ContainsKey(member) && !member.ActiveEffects.Values.Any(e => e.TypeId == HortimonculusBuffTypeId))
                {
                    var buff = new GameEffect
                    {
                        TypeId = HortimonculusBuffTypeId,
                        EffectId = GameEffectManager.Instance.NextEffectId(mapChannel),
                        EffectLevel = plant.Level,
                        ActionId = ActionId.AaExobiologistHortimunculus,
                        SourceId = plant.Owner.EntityId,
                        Source = plant.Owner,
                        SourceLevel = plant.Owner.Level,
                        IsBuff = true,
                        // Announced by the plant's tick (buffIds), as the client expects.
                        AnnounceOnAttach = false,
                        ResistModifier = plant.ResistPercent,
                        ResistDamageType = plant.ResistType
                    };

                    GameEffectManager.Instance.Attach(mapChannel, member, buff);
                    plant.Buffed[member] = buff;
                    tick.BuffIds.Add(member.EntityId);
                }

                // Credited to its owner, whose side of a fight decides who it may heal (Pvp); the
                // hate it draws goes to the plant (Threat.FromHealing).
                var healed = ActorManager.Instance.Heal(member, plant.HealAmount, plant.Owner.EntityId, plant.Object.EntityId);

                if (healed > 0)
                    tick.Entries.Add(new TickEntry { EntityId = member.EntityId, Amount = healed });
            }

            CellManager.Instance.CellCallMethod(plant.Object, tick);

            // It decays whether or not anyone needed it.
            Damage(plant, plant.HealAmount, now);
        }

        #region Attacked

        /// <summary>
        /// Whether this actor may harm the plant: a player who is an enemy of its owner's across a
        /// wargame, a creature of such a player's, and a free creature that could fight its owner
        /// - which a creature only gets round to when the plant is on its hate table (Threat).
        /// A dying plant is harmed by nobody.
        /// </summary>
        private static bool MayHarmPlant(Actor attacker, Plant plant)
        {
            if (attacker == null || plant == null || plant.RemoveAt != 0 || plant.Health <= 0
                || !MapInstanceScope.Contains(plant.MapChannel, attacker))
                return false;

            return attacker switch
            {
                Manifestation player => Pvp.AreEnemies(player, plant.Owner),
                Creature creature when creature.MasterEntityId != 0 => Pvp.AreEnemies(Pvp.Controller(creature), plant.Owner),
                Creature creature => TargetCategories.MayFightPlayer(creature.TargetCategory, plant.Owner.CombatCategory),
                _ => false
            };
        }

        /// <summary>Whether the entity is a Hortimonculus this actor may attack: a shot at it is let go (MissileManager.MissileLaunch).</summary>
        public static bool PlantMayBeAttackedBy(Actor attacker, ulong entityId) => MayHarmPlant(attacker, PlantOf(entityId));

        /// <summary>Whether the entity is a Hortimonculus this creature may fight and keep at (BehaviorManager.MayFight, Threat.CanFight).</summary>
        public static bool PlantMayBeFoughtBy(Creature creature, ulong entityId) => PlantMayBeAttackedBy(creature, entityId);

        /// <summary>Where a Hortimonculus stands, alive or dying: what a creature fighting it walks up to. False for anything else.</summary>
        public static bool TryGetPlantPosition(ulong entityId, out Vector3 position)
        {
            var plant = PlantOf(entityId);

            position = plant?.Object.Position ?? default;

            return plant != null;
        }

        /// <summary>Its hit points now; 0 for anything that is not a Hortimonculus.</summary>
        public static int PlantHealthOf(ulong entityId) => PlantOf(entityId)?.Health ?? 0;

        /// <summary>
        /// A missile at an object landing (MissileManager.MissileTrigger). False when the object
        /// is no Hortimonculus. Its damage goes on the plant's hit points as it is, and what it
        /// took is what the hit shows.
        /// </summary>
        internal static bool PlantTakeHit(Missile missile)
        {
            var plant = PlantOf(missile.TargetEntityId);

            if (plant == null)
                return false;

            missile.DamageA = MayHarmPlant(missile.Source, plant) ? Struck(plant, missile.DamageA, missile.Source) : 0;

            return true;
        }

        /// <summary>
        /// A hit that is no missile - a constant-fire weapon's pulse (ConstantFire). What the
        /// plant took, or null when the entity is not a Hortimonculus the attacker may harm.
        /// </summary>
        public static int? PlantTakeDamage(Actor attacker, ulong entityId, int amount)
        {
            var plant = PlantOf(entityId);

            if (plant == null || !MayHarmPlant(attacker, plant))
                return null;

            return Struck(plant, amount, attacker);
        }

        private static int Struck(Plant plant, int amount, Actor attacker)
        {
            var taken = Math.Min(Math.Max(0, amount), plant.Health);

            if (taken <= 0)
                return 0;

            // An attack on an enemy's is an attack on an enemy: the attacker's PvP Safety comes off (Pvp.Attack).
            if (Pvp.Controller(attacker) is Manifestation player)
                Pvp.EndSafety(plant.MapChannel, player);

            Damage(plant, taken, Environment.TickCount64);

            return taken;
        }

        #endregion

        /// <summary>Takes health off the plant, and kills it at none.</summary>
        private static void Damage(Plant plant, int amount, long now)
        {
            plant.Health = Math.Max(0, plant.Health - amount);
            CellManager.Instance.CellCallMethod(plant.Object, new UpdateHitPointsPacket(plant.Health));

            if (plant.Health <= 0)
                Wither(plant, now);
        }

        /// <summary>The plant dies: its protection comes off, its death plays, and it is taken away shortly.</summary>
        private static void Wither(Plant plant, long now)
        {
            if (plant.RemoveAt != 0)
                return;

            plant.RemoveAt = now + PlantRemoveMs;

            foreach (var (member, buff) in plant.Buffed)
                if (member.ActiveEffects.ContainsKey(buff.EffectId))
                    GameEffectManager.Instance.DettachEffect(plant.MapChannel, member, buff);

            plant.Buffed.Clear();

            plant.Object.StateId = UseObjectState.StateDestroyed;
            CellManager.Instance.CellCallMethod(plant.Object, new UsePacket(plant.Owner.EntityId, UseObjectState.StateDestroyed, 0));
            DynamicObjectManager.Instance.SetEnabled(plant.Object, false);
        }

        private static void RemovePlant(Plant plant)
        {
            lock (PlantsLock)
                if (!Plants.Remove(plant))
                    return;

            foreach (var (member, buff) in plant.Buffed)
                if (member.ActiveEffects.ContainsKey(buff.EffectId))
                    GameEffectManager.Instance.DettachEffect(plant.MapChannel, member, buff);

            plant.Buffed.Clear();

            CellManager.Instance.RemoveFromWorld(plant.MapChannel, plant.Object);
        }

        /// <summary>Whether this player may use the plant: its owner or one of their squad, alive, and it alive.</summary>
        private static bool MayUsePlant(Plant plant, Manifestation player)
        {
            if (plant == null || player == null || plant.RemoveAt != 0 || player.State == CharacterState.Dead)
                return false;

            return player == plant.Owner || (plant.Owner.PartyId != 0 && player.PartyId == plant.Owner.PartyId);
        }

        /// <summary>A request to use the plant (USE_OBJECT): the windup, as the client plays it.</summary>
        internal void RequestUseHortimonculus(Client client, DynamicObject obj, RequestUseObjectPacket packet)
        {
            var player = client.Player;
            var plant = PlantOf(obj.EntityId);

            if (!MayUsePlant(plant, player))
                return;

            var windupMs = TryGetLevel(ActionId.UseObject, packet.ActionArgId, out var use) ? use.WindupMs : 500;

            client.CallMethod(player.EntityId, new PerformWindupPacket(PerformType.TwoArgs, packet.ActionId, packet.ActionArgId));
            player.MapChannel.PerformRecovery.Add(new ActionData(player, packet.ActionId, packet.ActionArgId, windupMs) { SourceId = obj.EntityId });
        }

        /// <summary>
        /// The use lands: the plant performs OBJ_EFFECT with HORTIMONCULUS_USE on the user, which
        /// heals them by what they are missing, up to what the plant has left, over the effect's
        /// time; the plant loses that much.
        /// </summary>
        internal void UseHortimonculusRecovery(MapChannel mapChannel, ActionData action)
        {
            var plant = PlantOf(action.SourceId);

            if (!(action.Actor is Manifestation player) || !MayUsePlant(plant, player) || plant.MapChannel != mapChannel)
                return;

            if (Vector3.Distance(player.Position, plant.Object.Position) > DynamicObjectManager.MaxUseDistance)
                return;

            if (!player.Attributes.TryGetValue(Attributes.Health, out var health))
                return;

            var pool = PlantUseHeal(plant.Health, health.CurrentMax - health.Current);
            var now = Environment.TickCount64;
            var perform = new PerformObjectAbilityPacket(ActionId.ObjEffect, 1);

            perform.Hits.Add(player.EntityId);
            perform.Args.Add(HortimonculusUseTypeId);
            CellManager.Instance.CellCallMethod(plant.Object, perform);

            if (pool <= 0)
                return;

            var remaining = pool;
            var ticksLeft = Math.Max(1, plant.UseDurationMs / plant.UseIntervalMs);

            var heal = new GameEffect
            {
                TypeId = HortimonculusUseTypeId,
                EffectId = GameEffectManager.Instance.NextEffectId(mapChannel),
                EffectLevel = plant.Level,
                ActionId = ActionId.ObjEffect,
                // HealOverTime floats each tick as healing from its source: the plant.
                SourceId = plant.Object.EntityId,
                Source = plant.Owner,
                SourceLevel = plant.Owner.Level,
                IsBuff = true,
                AnnounceOnAttach = false,
                ExpiresTick = now + plant.UseDurationMs + 250,
                TickIntervalMs = plant.UseIntervalMs,
                NextTickTick = now + plant.UseIntervalMs,
                OnTick = (map, holder, effect) =>
                {
                    var amount = PlantUseTick(remaining, ticksLeft);

                    ticksLeft--;
                    remaining -= amount;

                    // Named as healing itself: the plant is no actor to credit it to. The hate it
                    // draws goes to the plant, as its ticks' does.
                    var healed = ActorManager.Instance.Heal(holder, amount, holder.EntityId, plant.Object.EntityId);
                    var tick = new GameEffectTickPacket(effect.EffectId, GameEffectTickPacket.TickKind.Heal);

                    if (healed > 0)
                        tick.Entries.Add(new TickEntry { EntityId = holder.EntityId, Amount = healed });

                    CellManager.Instance.CellCallMethod(map, holder, tick);
                }
            };

            GameEffectManager.Instance.Attach(mapChannel, player, heal);

            Damage(plant, pool, now);
        }
    }
}
