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
    /// CR_AMOEBOID_V1_VOMIT (274) and CR_AMOEBOID_V2_VOMIT (436): an Amoeboid regurgitates
    /// another Amoeboid.
    ///
    /// The client's own class says what this is in one line - AmoeboidVomitAbility, "Causes an
    /// Amoeboid to regurgitate another Amoeboid" - and it is TARGET_NONE with a range of 0, so it
    /// is cast on nothing and aimed nowhere. The strategy guide has the same thing from the
    /// player's side: Amoeboids "often appear in groups" and "attack by closing to short range
    /// and spitting various slimes at their targets".
    ///
    /// What comes out is Bane_Amoeboid_v1_Child (24092) or _v2_Child (24093), and four separate
    /// things say so:
    ///  - they are the only Amoeboid classes named Child, and there are exactly two of them for
    ///    exactly two vomit actions, v1 and v2;
    ///  - every adult Amoeboid class carries augmentations CREATURE and HARVESTABLE; the children
    ///    carry CREATURE alone, which is what a summon looks like - nothing to loot off it;
    ///  - CREATURE_BIRTH (155) is keyed by entity class rather than by an argument, and among its
    ///    113 entries are 24092 and 24093, with a 4000 ms recovery and animation family 622. The
    ///    client ships a birth animation for precisely these two classes;
    ///  - creaturenamelanguage 10177 is "Amoeboid Spawn".
    /// The variant table CREATURE_VARIANT_ID indexes is not in anything we have (it was server
    /// data, as the Spotter's and the bot's were), so the mapping is by action rather than by
    /// variant id: v1's four variants are 1246, 1315, 1317 and 1318 and v2's are 1247, 1320, 1322
    /// and 1323, and across all four the only thing that changes is CREATURE_LIFETIME_MS, so they
    /// are one creature at four durations rather than four creatures.
    ///
    /// The child is the parent's own model - class 16734 for v1, 20456 for v2, the same media id
    /// the adult carries - born small: CreatureBirthAction.LocalDoAction sets the body dimension
    /// back to eDefault, so it grows to full size over the birth's recovery.
    ///
    /// Two numbers are ours. The child keeps a quarter of its parent's maximum health, so two of
    /// them add half a parent to the fight without being a second one, and it scales with
    /// whatever spat it. Its attack is CR_AMOEBOID_SLIME at the same argument the parent uses -
    /// the ranged spit the guide describes, which also keeps a child from out-ranging the thing
    /// that made it. Deliberately NOT the vomit: a child that could regurgitate would fill the
    /// map.
    ///
    /// When its CREATURE_LIFETIME_MS is up a child expires rather than vanishing:
    /// AmoeboidExpireAbility (CR_AMOEBOID_EXPIRE 435), "Handles death of a Amoeboid" - its own
    /// "Amoeboid - Expire" windup (4.5 s, ABILITY_CREATURE_AMEOBOID_EXPIRE_WINDUP FX), standing
    /// still and doing nothing else, then its resolve, and it lies dead - no kill, nothing to
    /// loot - for the recovery (3 s) before it is gone. One killed while it expires just dies.
    /// The argument's RADIUS_AROUND_SOURCE (5) and PERCENTAGE_CHANCE (50) name no damage, heal or
    /// creature for them to act on, and are not used.
    /// </summary>
    public static class AmoeboidVomit
    {
        public const ActionId VomitV1 = (ActionId)274;
        public const ActionId VomitV2 = (ActionId)436;

        /// <summary>CR_AMOEBOID_EXPIRE at its one argument: a child's end.</summary>
        public const ActionId ExpireAction = (ActionId)435;
        public const uint ExpireArg = 1;

        /// <summary>CR_AMOEBOID_SLIME, the spit a child is given.</summary>
        public const ActionId SlimeAction = (ActionId)211;

        /// <summary>CREATURE_BIRTH, keyed by the born creature's own entity class.</summary>
        public const ActionId BirthAction = (ActionId)155;

        /// <summary>The CREATURE_BIRTH game effect a child wears while it is growing.</summary>
        public const int BirthTypeId = 10;

        public const uint ChildClassV1 = 24092;      // Bane_Amoeboid_v1_Child
        public const uint ChildClassV2 = 24093;      // Bane_Amoeboid_v2_Child

        /// <summary>creaturenamelanguage 10177, "Amoeboid Spawn".</summary>
        public const uint ChildNameId = 10177;

        /// <summary>Ours: the share of its parent's maximum health a child is born with.</summary>
        public const int ChildHealthPercent = 25;

        private sealed class Spawn
        {
            public MapChannel MapChannel;
            public Creature Child;
            public ulong ParentId;
            public long RemoveAt;

            /// <summary>Expiring: when the windup is done (it dies), and when it is gone; 0 until then.</summary>
            public long ExpiresAt;
            public long GoneAt;
        }

        private static readonly List<Spawn> Spawns = new List<Spawn>();
        private static readonly object SpawnsLock = new object();
        private static readonly Random Random = new Random();

        /// <summary>Whether this action regurgitates rather than attacks.</summary>
        public static bool IsVomit(CreatureAction action) =>
            action != null && (action.ActionId == VomitV1 || action.ActionId == VomitV2);

        private static uint ChildClassOf(ActionId actionId) =>
            actionId == VomitV2 ? ChildClassV2 : ChildClassV1;

        private static bool Alive(Actor actor) =>
            actor != null && actor.State != CharacterState.Dead && actor.State != CharacterState.Dying;

        /// <summary>How many of this parent's children are still about.</summary>
        private static int CountFor(ulong parentId)
        {
            lock (SpawnsLock)
                return Spawns.Count(s => s.ParentId == parentId && Alive(s.Child));
        }

        /// <summary>
        /// Regurgitates, if the parent is under its CREATURE_MAX_COUNT. The caller sets the
        /// cooldown whether or not anything comes out, so a parent at its limit is not asking
        /// again on the next tick.
        /// </summary>
        public static void Perform(MapChannel mapChannel, Creature parent, CreatureAction action)
        {
            if (!AbilityManager.Instance.TryGetLevel(action.ActionId, action.ActionArgId, out var level))
                return;

            // Everyone who can see it gets the regurgitation itself, whether or not a child
            // follows: the animation is the telegraph. The child comes when the windup is done
            // (CreatureWindups).
            CellManager.Instance.CellCallMethod(mapChannel, parent,
                new PerformWindupPacket(PerformType.TwoArgs, action.ActionId, action.ActionArgId));

            CreatureWindups.After(mapChannel, parent, CreatureWindups.WindupMsOf(action, level),
                () => Regurgitate(mapChannel, parent, action, level), action);
        }

        private static void Regurgitate(MapChannel mapChannel, Creature parent, CreatureAction action, ActionLevelInfo level)
        {
            CellManager.Instance.CellCallMethod(mapChannel, parent,
                new AbilityRecoveryPacket(action.ActionId, action.ActionArgId, AbilityRecoveryPacket.HitDataKind.None));

            var max = level.Get(AbilityProperty.CreatureMaxCount);

            if (max > 0 && CountFor(parent.EntityId) >= max)
                return;

            var child = Birth(mapChannel, parent, action, level);

            if (child == null)
                return;

            var lifetime = level.Get(AbilityProperty.CreatureLifetimeMs);

            lock (SpawnsLock)
                Spawns.Add(new Spawn
                {
                    MapChannel = mapChannel,
                    Child = child,
                    ParentId = parent.EntityId,
                    RemoveAt = lifetime > 0 ? Environment.TickCount64 + lifetime : 0
                });
        }

        private static Creature Birth(MapChannel mapChannel, Creature parent, CreatureAction action, ActionLevelInfo level)
        {
            var classId = ChildClassOf(action.ActionId);

            if (!EntityClassManager.Instance.LoadedEntityClasses.ContainsKey((EntityClasses)classId))
            {
                Logger.WriteLog(LogType.Error, $"AmoeboidVomit: entity class {classId} is not loaded, nothing to be born");
                return null;
            }

            var levelDifference = level.Get(AbilityProperty.CreatureLevelDifference);
            var childLevel = (uint)Math.Max(1, (int)parent.Level + levelDifference);
            var health = Math.Max(1, (int)parent.Attributes[Attributes.Health].CurrentMax * ChildHealthPercent / 100);

            var child = new Creature
            {
                EntityClass = (EntityClasses)classId,
                TargetCategory = parent.TargetCategory,
                Level = childLevel,
                MaxHitPoints = (uint)health,
                NameId = ChildNameId,
                RunSpeed = parent.RunSpeed,
                WalkSpeed = parent.WalkSpeed,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>(),
                State = CharacterState.Idle,
                MasterEntityId = parent.EntityId,
                AggroRange = parent.AggroRange
            };

            foreach (var attribute in new[] { Attributes.Body, Attributes.Mind, Attributes.Spirit,
                                              Attributes.Chi, Attributes.Power, Attributes.Aware,
                                              Attributes.Armor, Attributes.Regen })
                child.Attributes.Add(attribute, new ActorAttributes(attribute, 0, 0, 0, 0, 0));

            child.Attributes.Add(Attributes.Health, new ActorAttributes(Attributes.Health, health, health, health, 0, 0));
            child.Attributes.Add(Attributes.Speed, new ActorAttributes(Attributes.Speed, 1, 1, 1, 0, 0));

            var spit = Spit(action.ActionArgId, childLevel);

            if (spit != null)
                child.Actions.Add(spit);

            // Beside the parent rather than under it, so two children do not stand in each other.
            var angle = Random.NextDouble() * Math.PI * 2;
            var beside = parent.Position + new Vector3((float)Math.Cos(angle) * 2f, 0, (float)Math.Sin(angle) * 2f);

            CreatureManager.Instance.SetLocation(child, NavMeshManager.SnapToGround(mapChannel, beside),
                parent.Rotation, parent.MapContextId);
            child.LastYaw = (float)parent.Rotation;

            CellManager.Instance.AddToWorld(mapChannel, child);

            // Born: the effect is what makes the client draw it small, and CREATURE_BIRTH - keyed
            // by the child's own entity class - is what grows it back to size on its recovery.
            GameEffectManager.Instance.Attach(mapChannel, child, new GameEffect
            {
                TypeId = BirthTypeId,
                EffectLevel = 1,
                SourceId = parent.EntityId,
                Source = parent,
                SourceLevel = (int)childLevel,
                IsBuff = true,
                AllowDetach = false,
                AnnounceOnAttach = true,
                ExpiresTick = Environment.TickCount64 + BirthMs(classId)
            });

            CellManager.Instance.CellCallMethod(mapChannel, child,
                new PerformWindupPacket(PerformType.TwoArgs, BirthAction, classId));

            CellManager.Instance.CellCallMethod(mapChannel, child,
                new AbilityRecoveryPacket(BirthAction, classId, AbilityRecoveryPacket.HitDataKind.None));

            return child;
        }

        /// <summary>The birth's own recovery for this class, which is how long it spends growing.</summary>
        private static int BirthMs(uint classId)
        {
            return AbilityManager.Instance.TryGetLevel(BirthAction, classId, out var birth) && birth.RecoveryMs > 0
                ? birth.RecoveryMs
                : 4000;
        }

        /// <summary>
        /// The child's attack, built from CR_AMOEBOID_SLIME's own data at the argument its parent
        /// uses, so a v1 child spits what a v1 amoeboid spits.
        /// </summary>
        private static CreatureAction Spit(uint argId, uint childLevel)
        {
            if (!AbilityManager.Instance.TryGetLevel(SlimeAction, argId, out var slime))
                return null;

            var min = AbilityManager.Scale((int)childLevel, slime.Get(AbilityProperty.DamageAmountMin), slime.Get(AbilityProperty.DamageScaleType));
            var max = AbilityManager.Scale((int)childLevel, slime.Get(AbilityProperty.DamageAmountMax), slime.Get(AbilityProperty.DamageScaleType));

            return new CreatureAction
            {
                Description = "Amoeboid Spawn slime",
                ActionId = SlimeAction,
                ActionArgId = argId,
                RangeMin = 0.5,
                RangeMax = Math.Max(2, slime.MaxRange),
                Cooldown = Math.Max(1000, slime.ReuseMs),
                MinDamage = (uint)Math.Max(1, min),
                MaxDamage = (uint)Math.Max(1, Math.Max(min, max))
            };
        }

        /// <summary>Takes away the children whose time is up, and forgets the ones already gone.</summary>
        public static void Worker(MapChannel mapChannel)
        {
            List<Spawn> here;

            lock (SpawnsLock)
                here = Spawns.Where(s => s.MapChannel == mapChannel).ToList();

            if (here.Count == 0)
                return;

            var now = Environment.TickCount64;

            foreach (var spawn in here)
            {
                var child = spawn.Child;

                var inWorld = EntityManager.Instance.Creatures.TryGetValue(child.EntityId, out var registered) && registered == child;

                // Expired and lain its time: gone.
                if (spawn.GoneAt != 0)
                {
                    if (now < spawn.GoneAt)
                        continue;

                    lock (SpawnsLock)
                        Spawns.Remove(spawn);

                    if (inWorld)
                        CellManager.Instance.RemoveCreatureFromWorld(mapChannel, child);

                    continue;
                }

                // Killed, or taken out of the world by something else: no longer ours to count.
                if (!inWorld || child.State == CharacterState.Dead || child.State == CharacterState.Dying)
                {
                    lock (SpawnsLock)
                        Spawns.Remove(spawn);

                    continue;
                }

                if (spawn.ExpiresAt != 0)
                {
                    if (now >= spawn.ExpiresAt)
                        Die(mapChannel, spawn, now);

                    continue;
                }

                if (spawn.RemoveAt == 0 || now < spawn.RemoveAt)
                    continue;

                if (!StartExpiring(mapChannel, spawn, now))
                {
                    lock (SpawnsLock)
                        Spawns.Remove(spawn);

                    CellManager.Instance.RemoveCreatureFromWorld(mapChannel, child);
                }
            }
        }

        /// <summary>A child's time is up: its expire's windup, standing still. Whether there was one to play.</summary>
        private static bool StartExpiring(MapChannel mapChannel, Spawn spawn, long now)
        {
            if (!AbilityManager.Instance.TryGetLevel(ExpireAction, ExpireArg, out var expire))
                return false;

            var child = spawn.Child;
            var windupMs = Math.Max(0, expire.WindupMs);

            BehaviorManager.Instance.StopMoving(child);
            child.Controller.Path.Clear();
            child.Controller.PathIndex = 0;
            child.Controller.WindupUntil = now + windupMs + Math.Max(0, expire.RecoveryMs);

            CellManager.Instance.CellCallMethod(mapChannel, child,
                new PerformWindupPacket(PerformType.TwoArgs, ExpireAction, ExpireArg));

            spawn.ExpiresAt = now + Math.Max(1, windupMs);

            return true;
        }

        /// <summary>The expire's windup is done: its resolve, and it lies dead for the recovery. Nobody's kill.</summary>
        private static void Die(MapChannel mapChannel, Spawn spawn, long now)
        {
            var child = spawn.Child;
            var recoveryMs = AbilityManager.Instance.TryGetLevel(ExpireAction, ExpireArg, out var expire) ? Math.Max(0, expire.RecoveryMs) : 0;

            CellManager.Instance.CellCallMethod(mapChannel, child,
                new AbilityRecoveryPacket(ExpireAction, ExpireArg, AbilityRecoveryPacket.HitDataKind.None));

            child.State = CharacterState.Dead;
            child.HarvestAttemptsLeft = 0;

            if (child.Attributes.TryGetValue(Attributes.Health, out var health))
            {
                health.Current = 0;
                CellManager.Instance.CellCallMethod(mapChannel, child, new UpdateHealthPacket(health, child.EntityId));
            }

            CellManager.Instance.CellCallMethod(mapChannel, child, new StateChangePacket(new List<CharacterState> { CharacterState.Dead }));

            spawn.GoneAt = now + Math.Max(1, recoveryMs);
        }
    }
}
