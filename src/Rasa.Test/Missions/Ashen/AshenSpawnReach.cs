using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using DotRecast.Core;
using Rasa.Managers;
using Rasa.Navigation;
using Rasa.Structures;

namespace Rasa.Test.Missions.Ashen
{
    /// <summary>
    /// Where a hostile pool's creatures can stand, drawn as its spawns draw (fix K's check, generalised in fix M, to-fix rows 44
    /// and 48), and how near a place or a walk comes to that ground.
    ///
    /// Why: a creature notices a player within its AggroRange (18 m, Creature.AggroRange; 3D, BehaviorManager.ScanCell, no line
    /// of sight) of where it stands. A pool with a circle puts each creature at NavMeshManager.RandomPointAround(pool, radius)
    /// (SpawnPoolManager.SpawnPoint, its Anchor when the point is off the mesh); Detour's FindRandomPointAroundCircle takes whole
    /// polygons that touch the circle, so a draw can land far outside it (531092, r 26.1: 36.7 m from its middle). A creature
    /// then strolls inside its circle (BehaviorManager.WanderZoneOf). A pool without a circle stands its creatures on its point
    /// or its patrol's steps; one that moves strolls up to WanderZoneFallbackRadius (10 m) from where it spawned (its home,
    /// BehaviorManager.WanderZoneOf). A group of two or more at one point (SpawnPoint, count != 1) does not all spawn on it: the
    /// first stands on it, the rest on Spacing.OnRings' two rings round it (GroupRings, 3.3 and 6.6 m), or, where the rings are
    /// taken, scattered by up to PointScatter (2 m) on each axis; each then strolls 10 m from its own home, so a group's ground
    /// reaches up to 16.6 m from its point (12.8 m by the scatter alone). The ground of such a pool is its homes and navmesh
    /// draws within 10 m of each (ESTIMATE: the draws stand in for the strolls). The circle on paper protects nobody: the draws
    /// are measured.
    ///
    /// The draws use a fixed seed per pool (the navmesh query's random swapped for a seeded one while drawing, put back after),
    /// so a run gives the same ground each time.
    /// </summary>
    internal static class AshenSpawnReach
    {
        /// <summary>A creature's AggroRange unless something raises it (Creature.AggroRange; 3D).</summary>
        public const float AggroRange = 18;

        /// <summary>The clearance a place or a walk keeps from a non-target pool's ground: AggroRange and 2 m (fix K's ReachClearance).</summary>
        public const float Clearance = AggroRange + 2;

        /// <summary>The draws per pool with a circle (each a spawn's choice, SpawnPoolManager.SpawnPoint).</summary>
        public const int Draws = 1500;

        /// <summary>How far SpawnPoint scatters a group's creature from a radius-0 pool's point on each axis (Random % 5 - 2: -2..2 m).</summary>
        public const int PointScatter = 2;

        /// <summary>The rings Spacing.OnRings puts a group's creatures on round a radius-0 pool's point: radius, places, angle offset.</summary>
        public static readonly (float Radius, int Count, double Offset)[] GroupRings =
            Enumerable.Range(1, 2).Select(ring => (ring * Spacing.Distance * 1.1f, 6 * ring, ring * 0.5)).ToArray();

        /// <summary>The seed of a pool's draws: this and the pool's id.</summary>
        public const int Seed = 20271110;

        private static readonly FieldInfo QueryRandom = typeof(NavMeshQuery).GetField("_random", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("NavMeshQuery._random: the seeded draws need it");

        /// <summary>OK at Clearance or more, RISK from AggroRange, FAIL inside it.</summary>
        public static string Verdict(float near) => near >= Clearance ? "OK" : near >= AggroRange ? "RISK" : "FAIL";

        /// <summary>The ground of each of <paramref name="pools"/> in the copy (pools not in it are left out).</summary>
        public static Dictionary<uint, List<Vector3>> Ground(MapChannel copy, IEnumerable<uint> pools, int draws = Draws)
        {
            var wanted = pools.ToHashSet();
            var ground = new Dictionary<uint, List<Vector3>>();
            var query = copy.NavMesh ?? throw new InvalidOperationException($"map {copy.MapInfo.MapContextId} has no navmesh");
            var before = QueryRandom.GetValue(query);
            try
            {
                foreach (var pool in copy.SpawnPools.Where(pool => wanted.Contains(pool.DbId)).GroupBy(pool => pool.DbId).Select(group => group.First()))
                {
                    QueryRandom.SetValue(query, new RcRand(new Random(unchecked(Seed + (int)pool.DbId))));
                    ground[pool.DbId] = Of(copy, pool, draws);
                }
            }
            finally
            {
                QueryRandom.SetValue(query, before);
            }
            return ground;
        }

        private static List<Vector3> Of(MapChannel copy, SpawnPool pool, int draws)
        {
            var points = new List<Vector3> { pool.Position };
            if (pool.Radius > 0)
            {
                for (var k = 0; k < draws; k++)
                {
                    var draw = NavMeshManager.RandomPointAround(copy, pool.Position, pool.Radius);
                    if (draw == null && SpawnPoolManager.Anchor(copy, pool) is { } anchor)
                        draw = NavMeshManager.RandomPointAround(copy, anchor, pool.Radius) ?? anchor;
                    if (draw is { } point)
                        points.Add(point);
                }
                return points;
            }
            if (pool.Patrol is { Count: > 0 } patrol)
            {
                var steps = patrol.Select(step => step.Position).Prepend(pool.Position).ToList();
                points.AddRange(Samples(steps.Append(steps[0]).ToList(), 1));
                return points;
            }
            var homes = Homes(copy, pool);
            points.AddRange(homes.Skip(1));
            var each = Math.Max(20, draws / 3 / homes.Count);
            foreach (var home in homes)
                for (var k = 0; k < each; k++)
                    if (NavMeshManager.RandomPointAround(copy, home, BehaviorManager.WanderZoneFallbackRadius) is { } point
                        && Vector2.Distance(new(point.X, point.Z), new(home.X, home.Z)) <= BehaviorManager.WanderZoneFallbackRadius)
                        points.Add(point);
            return points;
        }

        /// <summary>
        /// Where a radius-0 pool's creatures can spawn (SpawnPoolManager.SpawnPoint): its point; for a pool that can spawn two or
        /// more, also the ring places (Spacing.OnRings, on the navmesh by Spacing.Placed) and the scatter grid (PointScatter).
        /// </summary>
        internal static List<Vector3> Homes(MapChannel copy, SpawnPool pool)
        {
            var homes = new List<Vector3> { pool.Position };
            if ((pool.SpawnSlot?.Sum(slot => Math.Max(0, (int)slot.CountMax)) ?? 0) < 2)
                return homes;
            foreach (var (radius, count, offset) in GroupRings)
                for (var i = 0; i < count; i++)
                {
                    var angle = (i + offset) * 2 * Math.PI / count;
                    var probe = new Vector3(pool.Position.X + (float)(Math.Cos(angle) * radius), pool.Position.Y, pool.Position.Z + (float)(Math.Sin(angle) * radius));
                    if (Spacing.Placed(copy, probe) is { } spot)
                        homes.Add(spot);
                }
            for (var dx = -PointScatter; dx <= PointScatter; dx++)
                for (var dz = -PointScatter; dz <= PointScatter; dz++)
                    if ((dx, dz) != (0, 0))
                        homes.Add(NavMeshManager.SnapToGround(copy, pool.Position + new Vector3(dx, 0, dz)));
            return homes;
        }

        /// <summary>A line's points every <paramref name="step"/> metres (its corners too).</summary>
        public static List<Vector3> Samples(IReadOnlyList<Vector3> line, float step = 0.5f)
        {
            var points = new List<Vector3>();
            if (line.Count == 1)
                points.Add(line[0]);
            for (var k = 0; k + 1 < line.Count; k++)
            {
                var steps = Math.Max(1, (int)MathF.Ceiling(Vector3.Distance(line[k], line[k + 1]) / step));
                for (var s = 0; s <= steps; s++)
                    points.Add(Vector3.Lerp(line[k], line[k + 1], s / (float)steps));
            }
            return points;
        }

        /// <summary>How near the points come to each pool's ground (3D): pool, the least distance, the point it is at.</summary>
        public static List<(uint Pool, float Near, Vector3 At)> Reach(IReadOnlyList<Vector3> points, IReadOnlyDictionary<uint, List<Vector3>> ground)
        {
            var result = new List<(uint, float, Vector3)>();
            if (points.Count == 0)
                return result;
            var lo = points.Aggregate(Vector3.Min);
            var hi = points.Aggregate(Vector3.Max);
            foreach (var (pool, draws) in ground)
            {
                var near = float.MaxValue;
                var at = points[0];
                foreach (var draw in draws)
                {
                    // Far outside the points' box: no nearer than the box allows.
                    var gap = Vector3.Max(Vector3.Max(lo - draw, draw - hi), Vector3.Zero).Length();
                    if (gap >= near)
                        continue;
                    foreach (var point in points)
                    {
                        var distance = Vector3.Distance(point, draw);
                        if (distance < near)
                            (near, at) = (distance, point);
                    }
                }
                result.Add((pool, near, at));
            }
            return result.OrderBy(row => row.Item2).ToList();
        }
    }
}
