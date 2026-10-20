using System;
using System.Collections.Generic;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Structures;

    /// <summary>
    /// How creatures of one side keep apart: a Bane squad, a camp, a mission's escorts. Without it a
    /// group came out of its pool on top of itself (a pool of no radius put its creatures within
    /// a metre or two of its point, on whole metres), all chased a player to the same spot short
    /// of them - each aims at the point the standoff away along its own line, and a group that
    /// arrives together comes along one line - and a mission's escorts all walked to their owner
    /// and stopped where the first of them did.
    ///
    /// Now each keeps <see cref="Distance"/> from the others of its side where it can:
    ///  - spawning, its point is drawn clear of the ones already standing round the pool, from
    ///    the pool's area, or from rings round its point when it has no radius
    ///    (SpawnPoolManager.SpawnPoint);
    ///  - chasing, the spot it aims for round its target is one no other of its side stands on
    ///    or is making for, turned round the target until it is clear; and one that has reached
    ///    its target, or holds its ground to shoot, with another of its side closer than
    ///    <see cref="Crowded"/> steps off to such a spot - the one of the two with the greater
    ///    entity id, so the pair do not both move (BehaviorManager);
    ///  - following, each escort takes its own place round its owner rather than the owner's
    ///    spot (BehaviorManager.AdvanceEscort).
    ///
    /// Only hostile creatures and mission escorts (<see cref="Keeps"/>): friendly NPCs and our
    /// own squads stand where they are put.
    ///
    /// A stroll already keeps <see cref="BehaviorManager.WanderSpacing"/> from everyone around
    /// (BehaviorManager.PickStroll). An authored place - a scene's route or pose, a post, a
    /// patrol's beat - is the author's and is left as it is.
    /// </summary>
    public static class Spacing
    {
        /// <summary>
        /// Who keeps apart: hostile creatures - the Bane, Thrax, animals - and mission escorts.
        /// Friendly NPCs, a base's staff and our own squads stand where they are put.
        /// </summary>
        internal static bool Keeps(Creature creature) =>
            creature != null && (creature.TargetCategory == TargetCategory.Hostile || BehaviorManager.IsMissionEscort(creature));

        /// <summary>How far apart the creatures of one side keep: "a few meters".</summary>
        public const float Distance = 3f;

        /// <summary>Nearer than this to another of its side, a creature that stands steps off.</summary>
        public const float Crowded = 2.4f;

        /// <summary>How long after stepping off a creature is left where it got to, so a crowd settles rather than shuffles.</summary>
        public const long SettleMs = 2000;

        /// <summary>How many places round a point are tried before the preferred one is taken anyway.</summary>
        private const int Turns = 12;

        /// <summary>How near to the place asked for the navmesh's nearest point must be to stand in for it.</summary>
        private const float OnMesh = 1.5f;

        /// <summary>
        /// Where the other living creatures of <paramref name="creature"/>'s side stand around
        /// <paramref name="near"/>, and where each that is walking is making for: the cells
        /// round the point, so 25 m and more each way.
        /// </summary>
        internal static List<Vector3> Taken(MapChannel map, Creature creature, Vector3 near) =>
            Taken(map, creature, creature.TargetCategory, near);

        /// <summary>As <see cref="Taken(MapChannel, Creature, Vector3)"/>, for a side: a creature not yet made, or none.</summary>
        internal static List<Vector3> Taken(MapChannel map, Creature creature, TargetCategory side, Vector3 near)
        {
            var spots = new List<Vector3>();

            foreach (var other in Around(map, near))
            {
                if (other == creature || other.TargetCategory != side || !Standing(other))
                    continue;

                spots.Add(other.Position);

                // Where one on the move is making for. One that has stopped short of the end of its
                // route - a chaser near enough its target - is where it stands, not where it was going.
                var path = other.Controller?.Path;

                if (path != null && path.Count > 0 && other.Controller.PathIndex < path.Count
                    && other.Controller.LastMovement is { Velocity: > 0 })
                    spots.Add(path[^1]);
            }

            return spots;
        }

        /// <summary>The nearest living creature of its side closer than <see cref="Crowded"/> that it should step off from: one with a smaller entity id.</summary>
        internal static Creature CrowdedBy(MapChannel map, Creature creature)
        {
            Creature nearest = null;
            var best = Crowded;

            foreach (var other in Around(map, creature.Position))
            {
                if (other == creature || other.TargetCategory != creature.TargetCategory || !Standing(other)
                    || other.EntityId > creature.EntityId)
                    continue;

                var gap = AcrossGround(other.Position, creature.Position);

                if (gap >= best)
                    continue;

                best = gap;
                nearest = other;
            }

            return nearest;
        }

        /// <summary>Whether <paramref name="spot"/> is <see cref="Distance"/> from every one of <paramref name="taken"/>, across the ground.</summary>
        internal static bool IsClear(Vector3 spot, IEnumerable<Vector3> taken)
        {
            foreach (var other in taken)
                if (AcrossGround(spot, other) < Distance)
                    return false;

            return true;
        }

        /// <summary>
        /// The place nearest <paramref name="preferred"/> on its circle round <paramref name="centre"/>
        /// that is clear of <paramref name="taken"/>: the preferred place itself, then turned a
        /// spacing at a time each way round. On the navmesh when the map has one. Null when none
        /// of them is clear.
        /// </summary>
        internal static Vector3? AroundClear(MapChannel map, Vector3 centre, Vector3 preferred, IReadOnlyList<Vector3> taken)
        {
            var radius = AcrossGround(preferred, centre);

            if (radius < 0.5f)
                return OnRings(map, preferred, taken);

            return AroundClear(map, centre, preferred, radius, taken);
        }

        /// <summary>
        /// As <see cref="AroundClear(MapChannel, Vector3, Vector3, IReadOnlyList{Vector3})"/>, on the
        /// circle of <paramref name="radius"/> round the centre, from the preferred place's side of it.
        /// </summary>
        internal static Vector3? AroundClear(MapChannel map, Vector3 centre, Vector3 preferred, float radius, IReadOnlyList<Vector3> taken)
        {
            if (radius < 0.5f)
                return null;

            var start = Math.Atan2(preferred.Z - centre.Z, preferred.X - centre.X);
            // A little over a spacing between neighbouring places, so two of them are clear of each other.
            var step = 2 * Math.Asin(Math.Min(1.0, Distance * 1.05 / (2 * radius)));

            for (var turn = 0; turn <= Turns; turn++)
            {
                // 0, +1, -1, +2, -2 ... spacings round the circle.
                var k = (turn + 1) / 2 * (turn % 2 == 1 ? 1 : -1);

                if (Math.Abs(k * step) > Math.PI)
                    break;

                var angle = start + k * step;
                var probe = new Vector3(centre.X + (float)(Math.Cos(angle) * radius), preferred.Y, centre.Z + (float)(Math.Sin(angle) * radius));

                if (Placed(map, probe) is Vector3 spot && IsClear(spot, taken))
                    return spot;
            }

            return null;
        }

        /// <summary>
        /// <paramref name="point"/> when it is clear of <paramref name="taken"/>, otherwise the
        /// first clear place on rings of one and two spacings round it. On the navmesh when the
        /// map has one. Null when none is clear.
        /// </summary>
        internal static Vector3? OnRings(MapChannel map, Vector3 point, IReadOnlyList<Vector3> taken)
        {
            if (Placed(map, point) is Vector3 here && IsClear(here, taken))
                return here;

            for (var ring = 1; ring <= 2; ring++)
            {
                // A little over a spacing: the six on the first ring are as far from each other as from the point.
                var radius = ring * Distance * 1.1f;
                var count = 6 * ring;
                var offset = ring * 0.5;

                for (var i = 0; i < count; i++)
                {
                    var angle = (i + offset) * 2 * Math.PI / count;
                    var probe = new Vector3(point.X + (float)(Math.Cos(angle) * radius), point.Y, point.Z + (float)(Math.Sin(angle) * radius));

                    if (Placed(map, probe) is Vector3 spot && IsClear(spot, taken))
                        return spot;
                }
            }

            return null;
        }

        /// <summary>The point on the navmesh, when the map has one and it is near the point; the point itself on the ground when the map has none.</summary>
        internal static Vector3? Placed(MapChannel map, Vector3 probe)
        {
            var navMesh = map?.NavMesh;

            if (navMesh == null)
                return NavMeshManager.SnapToGround(map, probe);

            var nearest = navMesh.Nearest(probe);

            if (nearest == null || AcrossGround(nearest.Value, probe) > OnMesh || Math.Abs(nearest.Value.Y - probe.Y) > 4f)
                return null;

            return nearest;
        }

        /// <summary>The creatures in the cells round <paramref name="near"/>: 25 m and more each way.</summary>
        internal static IEnumerable<Creature> Around(MapChannel map, Vector3 near)
        {
            var cells = map?.MapCellInfo?.Cells;

            if (cells == null || !CellManager.TryGetCellCoordinates(near, out var x, out var z))
                yield break;

            for (var dx = -1; dx <= 1; dx++)
                for (var dz = -1; dz <= 1; dz++)
                {
                    var seed = ((uint)(x + dx) & 0xFFFF) | ((uint)(z + dz) << 16);

                    if (!cells.TryGetValue(seed, out var cell))
                        continue;

                    foreach (var other in cell.CreatureList)
                        yield return other;
                }
        }

        internal static bool Standing(Creature creature) =>
            creature.State != CharacterState.Dead && creature.State != CharacterState.Dying &&
            creature.Attributes.TryGetValue(Attributes.Health, out var health) && health.Current > 0;

        internal static float AcrossGround(Vector3 a, Vector3 b)
        {
            var dx = a.X - b.X;
            var dz = a.Z - b.Z;

            return (float)Math.Sqrt(dx * dx + dz * dz);
        }
    }
}
