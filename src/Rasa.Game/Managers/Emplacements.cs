using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Rasa.Managers
{
    using Config;
    using Data;
    using Structures;

    /// <summary>
    /// Emplacements: the guns the server stands on mounts the map builds for them. The mount is
    /// scenery the client draws on its own - the standard turret's platform
    /// (ArchHumanBaseTurretPlatformV01), the light turret's tripod or track
    /// (Creature_Human_Base_Turret_Mini_Base_Tripod / _Track) - and the gun on top is a creature
    /// of an Emplacement_* class. No map places one: they were always the server's to spawn.
    ///
    /// A gun bolted to a platform does not walk. An emplacement:
    /// - stands exactly where its pool puts it: the pool's height is the mount's top, and
    ///   snapped to the navmesh a light turret would drop into its tripod (SpawnPoolManager);
    /// - never chases, wanders, runs home or is pushed about by a crowd (BehaviorManager);
    /// - is not carried by a knockback or a pull (CrowdControl);
    /// - turns to face what it shoots, which is all the movement it sends - and, between its
    ///   shots, stays turned to it as it moves (<see cref="KeepsFacing"/>): told its facing only as
    ///   it fired, the clients swung it back to its mount's way between rounds.
    /// It notices a fight as any creature does (Creature.AggroRange; an AFS turret out to its
    /// gun's reach, Defenders) and shoots out to its weapon's reach once it is in one, or at
    /// whatever shoots it.
    ///
    /// The AFS's own turrets fire in bursts (<see cref="ControlPointDefenceConfig.TurretBursts"/>):
    /// 3 or 5 rounds (<see cref="ControlPointDefenceConfig.BurstSizes"/>)
    /// <see cref="ControlPointDefenceConfig.BurstGapMs"/> apart, then a pause as long as those rounds
    /// would have taken one at a time - so a turret fires as many rounds a minute as it did, in
    /// bursts as a machine gun does, rather than one round each cooldown (<see cref="Cooldown"/>).
    /// </summary>
    public static class Emplacements
    {
        /// <summary>The client's Emplacement_* creature classes.</summary>
        public static readonly HashSet<EntityClasses> Classes = new HashSet<EntityClasses>
        {
            (EntityClasses)4064,    // Emplacement_AFS_Turret_Standard, "AFS Turret"
            (EntityClasses)11302,   // Emplacement_AFS_Turret_Mini, "AFS Light Turret"
            (EntityClasses)23902,   // Emplacement_AFS_Turret_Brann, "Brann Turret"
            (EntityClasses)7482,    // Emplacement_Bane_Turret_Standard
            (EntityClasses)10509,   // Emplacement_Bane_Turret_Mini
            (EntityClasses)20359    // Ability_Bane_Turret, a Technician's (CreatureSummons): it stands where it was set down
        };

        /// <summary>The AFS's own turrets: the ones that fire in bursts.</summary>
        public static readonly HashSet<EntityClasses> AfsTurrets = new HashSet<EntityClasses>
        {
            (EntityClasses)4064, (EntityClasses)11302, (EntityClasses)23902
        };

        /// <summary>The sizes of a burst when the settings give none.</summary>
        public static readonly int[] DefaultBurstSizes = { 3, 5 };

        /// <summary>The least turn worth telling the clients about between shots, in radians.</summary>
        public const float TurnThreshold = 0.05f;

        /// <summary>Whether this creature is an emplacement: it is if its class is one.</summary>
        public static bool Is(Creature creature) => creature != null && Classes.Contains(creature.EntityClass);

        /// <summary>Whether this creature is one of the AFS's own turrets (Standard, Light, Brann).</summary>
        public static bool IsAfsTurret(Creature creature) => creature != null && AfsTurrets.Contains(creature.EntityClass);

        /// <summary>Where a turret is in its burst: the rounds the burst has and those still to go.</summary>
        public sealed class Burst
        {
            public int Size { get; set; }
            public int Left { get; set; }
        }

        private static readonly ConditionalWeakTable<Creature, Burst> Bursts = new ConditionalWeakTable<Creature, Burst>();

        private static readonly Random BurstRandom = new Random();

        /// <summary>The burst a turret is in (a new creature has none under way).</summary>
        public static Burst BurstOf(Creature creature) => Bursts.GetOrCreateValue(creature);

        /// <summary>
        /// When a creature that has just fired an attack may fire it again (BehaviorManager, after the
        /// shot): <paramref name="cooldown"/> - the attack's own, with its wait on top - for anything but an
        /// AFS turret with bursts on; for that, <see cref="BurstCooldown"/>.
        /// </summary>
        public static long Cooldown(Creature creature, long cooldown)
        {
            var config = ControlPointFields.Config;

            if (config == null || !config.TurretBursts || !IsAfsTurret(creature))
                return cooldown;

            double roll;

            lock (BurstRandom)
                roll = BurstRandom.NextDouble();

            var sizes = config.BurstSizes is { Length: > 0 } ? config.BurstSizes : DefaultBurstSizes;

            return BurstCooldown(BurstOf(creature), cooldown, roll, sizes, config.BurstGapMs);
        }

        /// <summary>
        /// The wait after one round of a burst: a new burst's size picked from <paramref name="sizes"/> by
        /// <paramref name="roll"/> in [0, 1), the gap while rounds of it are left, and after its last round
        /// the rest of the time those rounds would have taken one at a time (<paramref name="cooldown"/>
        /// each), so the rounds a minute stay as they were. Sizes of 1 or less, or none, fire singly.
        /// </summary>
        public static long BurstCooldown(Burst burst, long cooldown, double roll, IReadOnlyList<int> sizes, long gapMs)
        {
            gapMs = Math.Max(1, gapMs);

            if (burst.Left <= 0)
            {
                var size = sizes == null || sizes.Count == 0 ? 1 : sizes[Math.Clamp((int)(roll * sizes.Count), 0, sizes.Count - 1)];

                burst.Size = Math.Max(1, size);
                burst.Left = burst.Size;
            }

            burst.Left--;

            if (burst.Size <= 1)
                return cooldown;

            if (burst.Left > 0)
                return gapMs;

            return Math.Max(gapMs, burst.Size * cooldown - (burst.Size - 1) * gapMs);
        }

        /// <summary>
        /// Whether an emplacement between its shots should be told to face its target anew (BehaviorManager's
        /// fight think, with its attack cooling): it has turned more than <see cref="TurnThreshold"/> from the
        /// way it was last sent facing.
        /// </summary>
        public static bool KeepsFacing(Creature creature, double difX, double difZ)
        {
            if (!Is(creature) || difX * difX + difZ * difZ < 1e-6)
                return false;

            var yaw = Math.Atan2(-difX, -difZ);

            return Math.Abs(Math.IEEERemainder(yaw - creature.LastYaw, 2 * Math.PI)) > TurnThreshold;
        }
    }
}
