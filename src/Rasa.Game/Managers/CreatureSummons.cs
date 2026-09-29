using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Packets.MapChannel.Server;
    using Structures;

    /// <summary>
    /// The creature actions that bring another creature into the fight:
    ///
    ///  - Turret (TechnicianTurretAbility, CR_TECHNICIAN_TURRET 276 - "Causes a Technician to
    ///    place a turret on the ground"): an Ability_Bane_Turret (20359), the class the client
    ///    ships for it - its own weapon (Weapon_Creature_Ability_Bane_Turret, fired as
    ///    WEAPON_ATTACK_ABILITYTURRET_TECHNICIAN 1/245, 40 m) and its own birth (CREATURE_BIRTH at
    ///    its class, 3 s). Set down a few metres towards the Technician's target, and an
    ///    emplacement from then on (Emplacements): it turns and shoots, it does not walk.
    ///  - Pet (HunterPetAbility, CR_HUNTER_PET 277 - "Causes a Hunter to summon a Howler"): a
    ///    Bane_Howler (7336) at the Hunter's level plus CREATURE_LEVEL_DIFFERENCE, for
    ///    CREATURE_LIFETIME_MS (two minutes), with the Howler's melee and sonic attacks.
    ///  - Necromite (ThraxNecromiteAbility, CR_THRAX_NECROMITE 488 - "Causes a Thrax to summon a
    ///    Necromite"): an Ability_Bane_Necromite (7528), the summoned Necromite class, with the
    ///    Necromite's bite (Weapon_Creature_Necromite, WEAPON_ATTACK_NECROMITE 1/109) and its
    ///    self-destruct (CR_NECROMITE_SELF_DESTRUCT 489): it runs at the Thrax's target and blows
    ///    up on them (CreatureBombs). It grows in as the turret does (CREATURE_BIRTH at its
    ///    class). It stands for NecromiteLifetimeMs if it has not spent itself by then.
    ///  - Cocoon (AttaGrubCocoonAbility, CR_ATTA_GRUB_COCOON 500 - "Causes a atta grub to cocoon
    ///    and transform to an adult atta"): the grub stops, winds up (8 s, the client's own
    ///    animation), taking EFFECT_DAMAGE_ABSORPTION_PERCENT less damage meanwhile, and comes out
    ///    as an adult - _SOLDIER (argument 1) an Atta Soldier, _HARVESTER (2) an Atta Harvester:
    ///    the one of the world's that is nearest the grub's level, at the grub's level, with the
    ///    grub's grudges and its place in its spawn pool. One killed in its cocoon dies a grub.
    ///    The adult hatches: CR_BIRTH 495 at CR_BIRTH_ATTA_COCOON (114) - the client's own
    ///    "Creature Birth - Atta Cocoon" animation (1455) and CREATURE_BIRTH_ATTA_COCOON FX
    ///    (1857), 1.4 s - standing where the cocoon was and doing nothing else until it is out
    ///    (HatchMs). It comes out full size: the growing birth (CREATURE_BIRTH at its class) is
    ///    the turret's and the pet's.
    ///
    /// CREATURE_VARIANT_ID is an index into a server table we do not have, so what comes out is
    /// chosen by the class the client names for the job. The turret, the pet and the Necromite
    /// are creature rows of their own (570001-570003) at TemplateLevel, scaled to the summoner's level as
    /// every creature row is - health, armour and damage x2 every 8 levels.
    ///
    /// Ours, the data giving none: a summoner has one turret and one pet up at a time
    /// (MaxEach); a turret stands for TurretLifetimeMs, which is its reuse, so a Technician
    /// always has one while it fights; a Necromite lasts NecromiteLifetimeMs; a grub cocoons once, at CocoonHealthPercent of its health.
    /// The cocoon's damage reduction is the server's alone: the client's AttaGrubCocoonEffect
    /// (ATTA_GRUB_COCOON_EFFECT 401) names a module 1.16.5.0 does not ship.
    ///
    /// What a summoner brings in joins its fight - its grudges, its target - and hunts on its
    /// own besides (Aggressive). It is nobody's loot and nothing to harvest: a turret or a pet
    /// killed gives experience, not a corpse to pick.
    /// </summary>
    public static class CreatureSummons
    {
        public const ActionId TechnicianTurret = (ActionId)276;
        public const ActionId HunterPet = (ActionId)277;
        public const ActionId AttaGrubCocoon = (ActionId)500;
        public const ActionId ThraxNecromite = (ActionId)488;

        /// <summary>CREATURE_BIRTH, keyed by the born creature's own entity class, and the effect it wears while growing.</summary>
        public const ActionId BirthAction = (ActionId)155;
        public const int BirthTypeId = 10;

        /// <summary>CR_BIRTH (CreatureBirthAbility) at CR_BIRTH_ATTA_COCOON: an adult Atta hatching out of a grub's cocoon.</summary>
        public const ActionId HatchAction = (ActionId)495;
        public const uint HatchArg = 114;

        /// <summary>How long a hatching adult stands still when the action's data gives no recovery.</summary>
        public const int DefaultHatchMs = 1433;

        public const uint TurretTemplateId = 570001;        // Ability_Bane_Turret
        public const uint HowlerPetTemplateId = 570002;     // Bane_Howler
        public const uint NecromiteTemplateId = 570003;     // Ability_Bane_Necromite

        /// <summary>The level the template rows are written at; a summon is scaled from it.</summary>
        public const uint TemplateLevel = 42;

        public const uint AttaSoldierClass = 7081;          // Creature_Atta_Soldier_Standard
        public const uint AttaHarvesterClass = 7078;        // Creature_Atta_Harvester_Standard

        /// <summary>Ours: how many of each summon a creature has up at once.</summary>
        public const int MaxEach = 1;

        /// <summary>Ours: how long a Technician's turret stands - its reuse, the argument giving no lifetime.</summary>
        public const long TurretLifetimeMs = 60000;

        /// <summary>Ours: how long a Necromite that has not blown itself up stands - the argument gives no lifetime.</summary>
        public const long NecromiteLifetimeMs = 60000;

        /// <summary>Ours: how far towards its target a Technician sets its turret down.</summary>
        public const float TurretDistance = 3f;

        /// <summary>Ours: the share of its health a grub has left when it cocoons.</summary>
        public const int CocoonHealthPercent = 50;

        private sealed class Summoned
        {
            public MapChannel MapChannel;
            public Creature Creature;
            public ulong SummonerId;
            public ActionId ActionId;
            public long RemoveAt;
        }

        private sealed class Cocoon
        {
            public MapChannel MapChannel;
            public Creature Grub;
            public CreatureAction Action;
            public GameEffect Shell;
            public long EmergeAt;
        }

        private static readonly List<Summoned> Summons = new List<Summoned>();
        private static readonly List<Cocoon> Cocoons = new List<Cocoon>();
        private static readonly Dictionary<Creature, long> Hatching = new Dictionary<Creature, long>();
        private static readonly object SummonsLock = new object();
        private static readonly Random Random = new Random();

        public static bool IsSummon(CreatureAction action) =>
            action != null && (action.ActionId == TechnicianTurret || action.ActionId == HunterPet || action.ActionId == ThraxNecromite);

        /// <summary>The creature row a summon action brings in.</summary>
        public static uint TemplateOf(ActionId actionId) =>
            actionId == TechnicianTurret ? TurretTemplateId
            : actionId == ThraxNecromite ? NecromiteTemplateId
            : HowlerPetTemplateId;

        /// <summary>How long a summon stands: the turret and the Necromite ours, the pet its CREATURE_LIFETIME_MS; 0 for as long as it lives.</summary>
        public static long LifetimeOf(ActionId actionId, ActionLevelInfo info) =>
            actionId == TechnicianTurret ? TurretLifetimeMs
            : actionId == ThraxNecromite ? NecromiteLifetimeMs
            : info?.Get(AbilityProperty.CreatureLifetimeMs) ?? 0;

        public static bool IsCocoon(CreatureAction action) => action != null && action.ActionId == AttaGrubCocoon;

        /// <summary>Whether a grub at this health should cocoon.</summary>
        public static bool ShouldCocoon(int health, int maxHealth) => maxHealth > 0 && health > 0 && health * 100 <= maxHealth * CocoonHealthPercent;

        /// <summary>Whether this creature was brought in by another: nobody's loot, nothing to harvest.</summary>
        public static bool IsSummoned(Creature creature)
        {
            lock (SummonsLock)
                return Summons.Any(s => s.Creature == creature);
        }

        /// <summary>Whether the creature is in its cocoon, or hatching out of one: it does nothing else.</summary>
        public static bool IsBusy(Creature creature)
        {
            // A loop, not Any(): this is asked of every creature on every think, and Any with a
            // lambda that captures the creature allocates a closure and a delegate each time.
            lock (SummonsLock)
            {
                foreach (var cocoon in Cocoons)
                    if (cocoon.Grub == creature)
                        return true;

                return Hatching.TryGetValue(creature, out var until) && Environment.TickCount64 < until;
            }
        }

        /// <summary>How long hatching takes: CR_BIRTH_ATTA_COCOON's recovery.</summary>
        public static int HatchMs()
        {
            return AbilityManager.Instance != null && AbilityManager.Instance.TryGetLevel(HatchAction, HatchArg, out var hatch) && hatch.RecoveryMs > 0
                ? hatch.RecoveryMs
                : DefaultHatchMs;
        }

        /// <summary>What a creature row scales by between two levels: x2 every 8.</summary>
        public static double ScaleFor(uint fromLevel, uint toLevel) => Math.Pow(2, ((int)toLevel - (int)fromLevel) / 8.0);

        /// <summary>The resistance that stands for all of a hit absorbed (a Linker's chest blast windup): nothing gets through that rounds to a point.</summary>
        public const int AbsorbAllResist = 1000000;

        /// <summary>
        /// The resistance that takes absorbPercent off every hit, whatever its type: the inverse
        /// of GameEffectManager.ResistMultiplier, 1 / (1 + 2r/100) = 1 - p/100. 100% is
        /// AbsorbAllResist.
        /// </summary>
        public static int ResistFor(int absorbPercent)
        {
            if (absorbPercent >= 100)
                return AbsorbAllResist;

            var share = Math.Max(0, absorbPercent) / 100.0;

            return (int)Math.Round((100.0 / (1 - share) - 100) / 2);
        }

        /// <summary>A creature made from a row written at another level, brought to this one: health, armour and its actions' damage.</summary>
        public static void Rescale(Creature creature, uint level)
        {
            level = Math.Max(1, level);

            var scale = ScaleFor(creature.Level, level);

            int Scaled(int value) => Math.Max(value > 0 ? 1 : 0, (int)Math.Round(value * scale));

            creature.Level = level;
            creature.MaxHitPoints = (uint)Scaled((int)creature.MaxHitPoints);

            foreach (var id in new[] { Attributes.Health, Attributes.Armor })
                if (creature.Attributes.TryGetValue(id, out var attribute))
                {
                    attribute.NormalMax = Scaled(attribute.NormalMax);
                    attribute.CurrentMax = Scaled(attribute.CurrentMax);
                    attribute.Current = Scaled(attribute.Current);
                }

            foreach (var action in creature.Actions)
            {
                action.MinDamage = (uint)Scaled((int)action.MinDamage);
                action.MaxDamage = (uint)Scaled((int)action.MaxDamage);
            }
        }

        /// <summary>Of these creature rows, the one of this class nearest this level; 0 for none.</summary>
        public static uint NearestOfClass(IEnumerable<KeyValuePair<uint, Creature>> rows, uint classId, uint level)
        {
            return rows
                .Where(r => r.Value != null && (uint)r.Value.EntityClass == classId)
                .OrderBy(r => Math.Abs((int)r.Value.Level - (int)level))
                .ThenBy(r => r.Key)
                .Select(r => r.Key)
                .FirstOrDefault();
        }

        private static bool Alive(Actor actor) =>
            actor != null && actor.State != CharacterState.Dead && actor.State != CharacterState.Dying
            && actor.Attributes.TryGetValue(Attributes.Health, out var health) && health.Current > 0;

        private static int CountFor(ulong summonerId, ActionId actionId)
        {
            lock (SummonsLock)
                return Summons.Count(s => s.SummonerId == summonerId && s.ActionId == actionId && Alive(s.Creature));
        }

        /// <summary>A turret, a pet or a Necromite, if the summoner has fewer than MaxEach up; whether it came.</summary>
        public static bool Perform(MapChannel mapChannel, Creature summoner, CreatureAction action, Actor target)
        {
            if (mapChannel == null || summoner == null || !IsSummon(action) || AbilityManager.Instance == null
                || !AbilityManager.Instance.TryGetLevel(action.ActionId, action.ActionArgId, out var info))
                return false;

            if (CountFor(summoner.EntityId, action.ActionId) >= MaxEach)
                return false;

            var templateId = TemplateOf(action.ActionId);

            if (!CreatureManager.Instance.LoadedCreatures.ContainsKey(templateId))
            {
                Logger.WriteLog(LogType.Error, $"CreatureSummons: creature {templateId} is not in the database, nothing to summon");
                return false;
            }

            CellManager.Instance.CellCallMethod(mapChannel, summoner,
                new PerformWindupPacket(PerformType.TwoArgs, action.ActionId, action.ActionArgId));

            // What it summons comes when the windup is done (CreatureWindups): the turret set
            // down, the Howler called.
            CreatureWindups.After(mapChannel, summoner, CreatureWindups.WindupMsOf(action, info),
                () => Bring(mapChannel, summoner, action, info, templateId, target), action);

            return true;
        }

        private static void Bring(MapChannel mapChannel, Creature summoner, CreatureAction action, ActionLevelInfo info, uint templateId, Actor target)
        {
            var turret = action.ActionId == TechnicianTurret;

            if (target != null && (target.State == CharacterState.Dead || target.MapContextId != summoner.MapContextId))
                target = null;

            var summon = CreatureManager.Instance.CreateCreature(templateId, null);

            // Nothing to bring: the windup the clients were shown is called off.
            if (summon == null)
            {
                CreatureWindups.Interrupt(mapChannel, summoner, action.ActionId, action.ActionArgId);
                return;
            }

            CellManager.Instance.CellCallMethod(mapChannel, summoner,
                new AbilityRecoveryPacket(action.ActionId, action.ActionArgId, AbilityRecoveryPacket.HitDataKind.None));

            Rescale(summon, (uint)Math.Max(1, (int)summoner.Level + info.Get(AbilityProperty.CreatureLevelDifference)));

            summon.TargetCategory = summoner.TargetCategory;
            summon.MasterEntityId = summoner.EntityId;
            summon.Stance = MinionStance.Aggressive;
            summon.AggroRange = Math.Max(summon.AggroRange, summoner.AggroRange);

            Vector3 spot;

            if (turret && target != null && Vector3.DistanceSquared(target.Position, summoner.Position) > 0.01f)
                spot = summoner.Position + Vector3.Normalize(target.Position - summoner.Position) * TurretDistance;
            else
            {
                // Beside the summoner on the walkable surface, where the map has one: a random
                // point two metres off could be inside a rock or over an edge, where the summon can
                // neither see its target nor find a path to it.
                var angle = Random.NextDouble() * Math.PI * 2;
                spot = NavMeshManager.RandomPointAround(mapChannel, summoner.Position, 2f)
                       ?? summoner.Position + new Vector3((float)Math.Cos(angle) * 2f, 0, (float)Math.Sin(angle) * 2f);
            }

            CreatureManager.Instance.SetLocation(summon, NavMeshManager.SnapToGround(mapChannel, spot), summoner.Rotation, summoner.MapContextId);
            summon.LastYaw = (float)summoner.Rotation;

            CellManager.Instance.AddToWorld(mapChannel, summon);
            Birth(mapChannel, summon, summoner);

            var lifetime = LifetimeOf(action.ActionId, info);

            lock (SummonsLock)
                Summons.Add(new Summoned
                {
                    MapChannel = mapChannel,
                    Creature = summon,
                    SummonerId = summoner.EntityId,
                    ActionId = action.ActionId,
                    RemoveAt = lifetime > 0 ? Environment.TickCount64 + lifetime : 0
                });

            JoinFight(summon, summoner, target);
        }

        /// <summary>
        /// A grub in a fight brought to CocoonHealthPercent: it stops and cocoons, if it has a
        /// cocoon and is not in one already. Whether it did.
        /// </summary>
        public static bool TryCocoon(MapChannel mapChannel, Creature grub)
        {
            if (mapChannel == null || grub == null || AbilityManager.Instance == null || IsBusy(grub))
                return false;

            var action = grub.Actions.FirstOrDefault(IsCocoon);

            if (action == null || !grub.Attributes.TryGetValue(Attributes.Health, out var health)
                || !ShouldCocoon(health.Current, health.CurrentMax)
                || !AbilityManager.Instance.TryGetLevel(action.ActionId, action.ActionArgId, out var info))
                return false;

            var windupMs = Math.Max(1000, info.WindupMs);
            var now = Environment.TickCount64;

            BehaviorManager.Instance.StopMoving(grub);
            grub.Controller.Path.Clear();

            CellManager.Instance.CellCallMethod(mapChannel, grub,
                new PerformWindupPacket(PerformType.TwoArgs, action.ActionId, action.ActionArgId));

            // Hardened: the server's alone (see the class).
            var shell = new GameEffect
            {
                TypeId = 0,
                EffectId = GameEffectManager.Instance.NextEffectId(mapChannel),
                EffectLevel = info.Level,
                ActionId = info.ActionId,
                SourceId = grub.EntityId,
                Source = grub,
                SourceLevel = (int)grub.Level,
                IsBuff = true,
                ServerOnly = true,
                ResistModifier = ResistFor(info.Get(AbilityProperty.EffectDamageAbsorptionPercent)),
                ExpiresTick = now + windupMs + 1000
            };

            GameEffectManager.Instance.Attach(mapChannel, grub, shell);

            lock (SummonsLock)
                Cocoons.Add(new Cocoon { MapChannel = mapChannel, Grub = grub, Action = action, Shell = shell, EmergeAt = now + windupMs });

            // Once: whatever comes of it, a grub does not cocoon twice.
            grub.Actions.Remove(action);

            return true;
        }

        /// <summary>Takes away the summons whose time is up, forgets the ones already gone, and lets the grubs whose cocoon is done out.</summary>
        public static void Worker(MapChannel mapChannel)
        {
            List<Summoned> summons;
            List<Cocoon> due;
            var now = Environment.TickCount64;

            lock (SummonsLock)
            {
                summons = Summons.Where(s => s.MapChannel == mapChannel).ToList();
                due = Cocoons.Where(c => c.MapChannel == mapChannel && now >= c.EmergeAt).ToList();

                foreach (var cocoon in due)
                    Cocoons.Remove(cocoon);

                // A grub killed in its cocoon is done with it.
                Cocoons.RemoveAll(c => c.MapChannel == mapChannel && !Alive(c.Grub));

                foreach (var hatched in Hatching.Where(h => now >= h.Value).Select(h => h.Key).ToList())
                    Hatching.Remove(hatched);
            }

            foreach (var summon in summons)
            {
                var creature = summon.Creature;

                if (!EntityManager.Instance.Creatures.TryGetValue(creature.EntityId, out var registered) || registered != creature)
                {
                    lock (SummonsLock)
                        Summons.Remove(summon);

                    continue;
                }

                // Dead: the corpse goes as any other does; it is still counted as summoned so
                // that it is nobody's loot.
                if (creature.State == CharacterState.Dead || summon.RemoveAt == 0 || now < summon.RemoveAt)
                    continue;

                lock (SummonsLock)
                    Summons.Remove(summon);

                GameEffectManager.Instance.ClearEffects(mapChannel, creature);
                CellManager.Instance.RemoveCreatureFromWorld(mapChannel, creature);
            }

            foreach (var cocoon in due)
                Emerge(mapChannel, cocoon);
        }

        /// <summary>The cocoon is done: the grub is gone, and an adult stands where it was.</summary>
        private static void Emerge(MapChannel mapChannel, Cocoon cocoon)
        {
            var grub = cocoon.Grub;

            GameEffectManager.Instance.DettachEffect(mapChannel, grub, cocoon.Shell);

            if (!Alive(grub) || grub.MapContextId != mapChannel.MapInfo.MapContextId)
                return;

            var classId = cocoon.Action.ActionArgId == 2 ? AttaHarvesterClass : AttaSoldierClass;
            var templateId = NearestOfClass(CreatureManager.Instance.LoadedCreatures, classId, grub.Level);

            if (templateId == 0)
            {
                Logger.WriteLog(LogType.Error, $"CreatureSummons: no creature of class {classId} to come out of a cocoon");
                return;
            }

            var adult = CreatureManager.Instance.CreateCreature(templateId, grub.SpawnPool);

            if (adult == null)
                return;

            CellManager.Instance.CellCallMethod(mapChannel, grub,
                new AbilityRecoveryPacket(cocoon.Action.ActionId, cocoon.Action.ActionArgId, AbilityRecoveryPacket.HitDataKind.None));

            Rescale(adult, grub.Level);

            CreatureManager.Instance.SetLocation(adult, grub.Position, grub.Rotation, grub.MapContextId);
            adult.HomePos.Position = grub.HomePos.Position;
            adult.LastYaw = grub.LastYaw;

            var target = EntityManager.Instance.Actors.TryGetValue(grub.Controller.ActionFighting.TargetEntityId, out var fought) ? fought : null;

            // The grub goes first, out of its pool as the adult comes into it.
            GameEffectManager.Instance.ClearEffects(mapChannel, grub);
            CellManager.Instance.RemoveCreatureFromWorld(mapChannel, grub);

            if (grub.SpawnPool != null)
                SpawnPoolManager.Instance.DecreaseAliveCreatureCount(mapChannel, grub.SpawnPool);

            CellManager.Instance.AddToWorld(mapChannel, adult);
            Hatch(mapChannel, adult);

            JoinFight(adult, grub, target);
        }

        /// <summary>
        /// Out of the cocoon: CR_BIRTH_ATTA_COCOON on the adult, which plays the hatching and its
        /// FX, and the adult held where it is for as long.
        /// </summary>
        private static void Hatch(MapChannel mapChannel, Creature adult)
        {
            lock (SummonsLock)
                Hatching[adult] = Environment.TickCount64 + HatchMs();

            BehaviorManager.Instance.StopMoving(adult);
            adult.Controller.Path.Clear();

            CellManager.Instance.CellCallMethod(mapChannel, adult, new PerformWindupPacket(PerformType.TwoArgs, HatchAction, HatchArg));
            CellManager.Instance.CellCallMethod(mapChannel, adult, new AbilityRecoveryPacket(HatchAction, HatchArg, AbilityRecoveryPacket.HitDataKind.None));
        }

        /// <summary>What came in takes up its maker's grudges and its target.</summary>
        private static void JoinFight(Creature joining, Creature from, Actor target)
        {
            foreach (var grudge in from.Hate.Ranked())
                joining.Hate.Ensure(grudge.Key, grudge.Value);

            if (target != null && Alive(target))
            {
                joining.Hate.Ensure(target.EntityId, Threat.NoticedThreat);
                BehaviorManager.Instance.SetActionFighting(joining, target.EntityId);
            }
        }

        /// <summary>
        /// Born: CREATURE_BIRTH at the creature's own class, which the client plays as it growing
        /// to size over the birth's recovery, and the CREATURE_BIRTH effect for as long.
        /// </summary>
        private static void Birth(MapChannel mapChannel, Creature born, Creature source)
        {
            var classId = (uint)born.EntityClass;

            if (!AbilityManager.Instance.TryGetLevel(BirthAction, classId, out var birth))
                return;

            GameEffectManager.Instance.Attach(mapChannel, born, new GameEffect
            {
                TypeId = BirthTypeId,
                EffectId = GameEffectManager.Instance.NextEffectId(mapChannel),
                EffectLevel = 1,
                SourceId = source.EntityId,
                Source = source,
                SourceLevel = (int)born.Level,
                IsBuff = true,
                AllowDetach = false,
                AnnounceOnAttach = true,
                ExpiresTick = Environment.TickCount64 + Math.Max(1000, birth.RecoveryMs)
            });

            CellManager.Instance.CellCallMethod(mapChannel, born, new PerformWindupPacket(PerformType.TwoArgs, BirthAction, classId));
            CellManager.Instance.CellCallMethod(mapChannel, born, new AbilityRecoveryPacket(BirthAction, classId, AbilityRecoveryPacket.HitDataKind.None));
        }
    }
}
