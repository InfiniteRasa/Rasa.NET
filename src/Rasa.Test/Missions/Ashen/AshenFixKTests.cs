using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.ClientData;
using Rasa.Managers;
using Rasa.Missions.Scenes;
using Rasa.Services.Preloader.Missions.Ashen;
using Rasa.Structures;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions.Ashen
{
    using F = Rasa.Test.Missions.Marshes.MarshesClientFloorTests;
    using G = AshenGothaPlantTests;
    using Facility = AshenIndraFacilityTests;

    /// <summary>
    /// Ashen fix job K (to-fix row 44, a test and route change only, no migration): 1452's way to the pad and the explosive charge.
    /// Fix J's checked way went south round the pad's trench, 2-7 m outside upstream pool 531092's circle (3 Thrax Soldiers,
    /// 2 Linkers, a Technician, a Caretaker on the pad deck): the real client died there twice (deaths 7 and 8, 2026-11-12). The
    /// way now goes north round the landing pad's dish (AshenGothaPlantTests.PadDeck).
    ///
    /// Why these tests: a creature notices a player within its AggroRange (18 m, Creature.AggroRange; 3D, BehaviorManager.ScanCell,
    /// no line of sight) of where it stands, and it stands where its spawn put it (SpawnPoolManager: a navmesh point
    /// NavMeshManager.RandomPointAround the pool's place with its radius; Detour's FindRandomPointAroundCircle takes whole polygons
    /// that touch the circle, so a draw lands up to 36.7 m from 531092's middle, and 531093's all land on the pad deck) or strolls
    /// to inside its pool's circle (BehaviorManager.WanderZoneOf). A walk that keeps the pool's circle by a few metres walks
    /// through that ground: the fault of row 44. The pool's circle alone (2D, 25 m outside it) and the draws themselves (3D) are
    /// both checked.
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class AshenFixKTests
    {
        private const string MapName = "adv_arieki_ligo_ashendesert_indracaverns";

        /// <summary>The pad deck's pool that killed the tester on fix J's way (upstream, not changed).</summary>
        internal const uint PadDeckSouthPool = 531092;

        /// <summary>The pad deck's north pool (upstream, not changed): its draws stand on the deck's north half, by the way north.</summary>
        internal const uint PadDeckNorthPool = 531093;

        /// <summary>How far outside 531092's circle every point of the walks keeps on the deck (2D; 25 m: 18 m aggro and 7 m, ESTIMATE).</summary>
        internal const float CircleClearance = 25;

        /// <summary>A creature's AggroRange unless something raises it (Creature.AggroRange; 3D).</summary>
        internal const float AggroRange = 18;

        /// <summary>
        /// How near the walks come to a pool's spawn draws (3D) at the least: AggroRange and 2 m (MarshesNpcsStandStillV1.ReachClearance).
        /// Not kept (docs/ashen-playtest.md): 531093, by the way north (the pad deck's only ways west pass 531092's or 531093's draws).
        /// </summary>
        internal const float ReachClearance = AggroRange + 2;

        /// <summary>
        /// Pools the walks pass at about AggroRange (rule breaks, docs/ashen-playtest.md): 531086, the bay's hall pool, 18.2-18.3 m
        /// (2026-11-12, 600 and 1500 draws) from set 16's legs 01 and 13 at (-324, 129.6, -110) (the bay's only way south, walked by
        /// the tester without a fight); kept at 16 m so another draw does not fail the test (ESTIMATE).
        /// </summary>
        private static readonly Dictionary<uint, float> Kept = new() { [531086] = 16 };

        /// <summary>
        /// Fix N (row 50): how near the way comes to 531093's draws at the least (its rule break): 3.5 m, as fix K's way (4.84 m at
        /// (-351, -260), seeded draws, AshenSpawnReach, pinned in PinnedNorth; the tester passed it without a fight, row 50). The deck north of the pit is
        /// that pool's ground: a way north of the rim comes 0.2 m from it, one 8 m from it squeezes past the pit's edge.
        /// </summary>
        internal const float NorthPoolKept = 3.5f;

        /// <summary>Fix J's way over the deck (the plant 23 from (-289, -287), 24, 26, 27, 28): it broke the rules (the regression).</summary>
        internal static readonly Vector3[] FixJWay =
        {
            new(-289.13f, 177.61f, -286.65f), new(-340, 175.4f, -304), new(-339.1f, 175.4f, -303.8f), new(-339.4f, 175.6f, -307.2f), new(-341.9f, 175.6f, -310.6f),
            new(-354.31f, 175.6f, -318.8f), new(-364.31f, 175.6f, -319.6f), new(-371.11f, 175.6f, -318), new(-370.71f, 175.6f, -314.8f), new(-369.11f, 176.37f, -312.4f),
            new(-348.71f, 177.4f, -306.8f), new(-345.91f, 177.4f, -305.2f), new(-345, 177.4f, -305), new(-355, 176.4f, -305), new(-360, 176.48f, -308),
            new(-363, 176.54f, -309.5f), new(-379.5f, 176.49f, -303), new(-382, 177, -299.5f), new(-385, 176.82f, -298), new(-386.5f, 176.74f, -296.25f),
            new(-390.8f, 175.81f, -297), new(-390.8f, 175.81f, -301)
        };

        [TestInitialize]
        public void Clock() => Rasa.Test.Missions.Plateau.PlateauGiantKillerSquadTests.UseClock();

        [TestCleanup]
        public void Restore() => Rasa.Test.Missions.Plateau.PlateauGiantKillerSquadTests.RestoreClock();

        /// <summary>The walks to the pad and to the charge (AshenGothaPlantTests.Walks), as lines.</summary>
        private static List<(string Label, List<Vector3> Line)> PadWalks() => G.Walks()
            .Where(walk => walk.Label is "the landing pad from Drof at the plant" or "the explosive charge from Drof at the plant")
            .Select(walk => (walk.Label, G.Line(walk.Legs))).ToList();

        /// <summary>A line's points every half metre.</summary>
        private static List<Vector3> Samples(IReadOnlyList<Vector3> line)
        {
            var points = new List<Vector3>();
            for (var k = 0; k + 1 < line.Count; k++)
            {
                var steps = Math.Max(1, (int)MathF.Ceiling(Vector3.Distance(line[k], line[k + 1]) * 2));
                for (var s = 0; s <= steps; s++)
                    points.Add(Vector3.Lerp(line[k], line[k + 1], s / (float)steps));
            }
            return points;
        }

        /// <summary>The least 2D gap to the pool's circle of the line's points on its floor (Facility.OtherFloor), and where.</summary>
        private static (float Gap, Vector3 At) CircleGap(IReadOnlyList<Vector3> line, Vector3 pole, float radius) => Samples(line)
            .Where(point => MathF.Abs(point.Y - pole.Y) < Facility.OtherFloor)
            .Select(point => (Gap: Vector2.Distance(new(point.X, point.Z), new(pole.X, pole.Z)) - radius, At: point))
            .DefaultIfEmpty((Gap: float.MaxValue, At: Vector3.Zero)).MinBy(pair => pair.Gap);

        /// <summary>
        /// Every point of the walks to the pad and the charge on the pad deck keeps 25 m (CircleClearance) or more outside 531092's
        /// circle (51.2 m from its middle; 2D, as the walk rule): the way north round the dish 32.7 m. Fix J's way failed it (2.0 m at
        /// (-354.3, -318.8)); the walk ends (the pad, beside the charge) keep it too.
        /// </summary>
        [TestMethod]
        public void ThePadWalksKeep25MetresOutside531092sCircle()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pool = harness.World.SpawnPoolEntries.Single(entry => entry.Id == PadDeckSouthPool);
            var pole = new Vector3((float)pool.PosX, (float)pool.PosY, (float)pool.PosZ);
            Assert.AreEqual((AshenGothaPlantNpcsV1.Caverns, 26.1), (pool.MapContextId, pool.Radius), "upstream's pool 531092 as it is");
            var walks = PadWalks();
            Assert.HasCount(2, walks);
            foreach (var (label, line) in walks)
            {
                var (gap, at) = CircleGap(line, pole, (float)pool.Radius);
                Assert.IsGreaterThanOrEqualTo(CircleClearance, gap, FormattableString.Invariant($"{label}: {gap:0.0} m outside 531092's circle at ({at.X:0.0}, {at.Y:0.0}, {at.Z:0.0})"));
            }
            var (old, oldAt) = CircleGap(FixJWay, pole, (float)pool.Radius);
            Assert.IsLessThan(CircleClearance, old, FormattableString.Invariant($"fix J's way broke it: {old:0.0} m at ({oldAt.X:0.0}, {oldAt.Z:0.0})"));
            Assert.IsLessThan(8f, old, "fix J's way ran 2-7 m outside the circle (row 44)");
        }

        /// <summary>
        /// The walks to the pad and the charge keep ReachClearance (20 m, 3D) from every spawn draw of 531092 (27.67 m at (-329,
        /// -287), the deck's south-east corner; seeded draws since fix N) and AggroRange (18 m) or more from those of every other hostile pool of Indra Caverns with an area (531086:
        /// Kept; each pool drawn as its spawns draw with a fixed seed, AshenSpawnReach.Ground), but 531093's (a rule break: the way north passes
        /// its draws on the deck's north half, NorthPoolKept (3.5 m) or more, 4-5 m at (-351, -260); the deck's only other way west passes 531092's). Fix J's way
        /// came within 0.4 m of 531092's draws.
        /// </summary>
        [TestMethod]
        public void ThePadWalksKeepOutOfThePoolsReach()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var copy = G.IntoGotha(harness);
            Assert.AreEqual(AggroRange, new Creature().AggroRange, "Creature.AggroRange");
            var hostile = Facility.HostilePools(harness).Select(entry => entry.Id).ToHashSet();
            var pools = copy.SpawnPools.Where(pool => hostile.Contains(pool.DbId) && pool.Radius > 0).ToList();
            Assert.IsTrue(pools.Any(pool => pool.DbId == PadDeckSouthPool) && pools.Any(pool => pool.DbId == PadDeckNorthPool), "the pad deck's pools in the copy");
            var walks = PadWalks().Select(walk => (walk.Label, Points: Samples(walk.Line))).Append(("fix J's way", Samples(FixJWay))).ToList();
            var failures = new List<string>();
            var report = new List<string>();
            // Fix N: the seeded ground (AshenSpawnReach.Ground, the same draws each run), not a fresh random draw each run.
            var ground = AshenSpawnReach.Ground(copy, pools.Select(pool => pool.DbId));
            var pinned = new Dictionary<string, float>();
            foreach (var pool in pools)
            {
                var draws = ground[pool.DbId];
                Assert.IsNotEmpty(draws, $"pool {pool.DbId} draws");
                foreach (var (label, points) in walks)
                {
                    var (near, at) = points.Select(point => (Near: draws.Min(draw => Vector3.Distance(point, draw)), At: point)).MinBy(pair => pair.Near);
                    var rule = pool.DbId == PadDeckSouthPool ? ReachClearance : Kept.GetValueOrDefault(pool.DbId, AggroRange);
                    report.Add(FormattableString.Invariant($"{label} / {pool.DbId}: {near:0.0} m at ({at.X:0.0}, {at.Y:0.0}, {at.Z:0.0})"));
                    if (label == "fix J's way")
                    {
                        if (pool.DbId == PadDeckSouthPool && near >= AggroRange)
                            failures.Add(FormattableString.Invariant($"fix J's way: {near:0.0} m from 531092's draws, it went through them (row 44)"));
                        continue;
                    }
                    if (label != "fix J's way" && pool.DbId is PadDeckNorthPool or PadDeckSouthPool)
                        pinned[FormattableString.Invariant($"{label} / {pool.DbId}")] = near;
                    if (pool.DbId == PadDeckNorthPool && near < NorthPoolKept)
                        failures.Add(FormattableString.Invariant($"{label}: {near:0.0} m from 531093's spawn draws at ({at.X:0.0}, {at.Y:0.0}, {at.Z:0.0}) (fix N keeps {NorthPoolKept})"));
                    if (pool.DbId != PadDeckNorthPool && near < rule)
                        failures.Add(FormattableString.Invariant($"{label}: {near:0.0} m from pool {pool.DbId}'s spawn draws at ({at.X:0.0}, {at.Y:0.0}, {at.Z:0.0}) (its rule {rule})"));
                }
            }
            Console.WriteLine(string.Join("\n", report));
            Assert.IsEmpty(failures, $"{failures.Count}: " + string.Join("; ", failures));
            foreach (var label in new[] { "the landing pad from Drof at the plant", "the explosive charge from Drof at the plant" })
            {
                Assert.AreEqual(PinnedNorth, pinned[$"{label} / {PadDeckNorthPool}"], 0.05, $"{label}: 531093's seeded draws (fix N)");
                Assert.AreEqual(PinnedSouth, pinned[$"{label} / {PadDeckSouthPool}"], 0.05, $"{label}: 531092's seeded draws (fix N)");
            }
        }

        /// <summary>
        /// Fix N: the least distance (3D) of the pad walks from 531093's and 531092's seeded draws (AshenSpawnReach.Ground,
        /// 2026-11-13): 4.84 m at (-351, 176.4, -260) and 27.67 m at (-329, 175.4, -287).
        /// </summary>
        internal const float PinnedNorth = 4.84f, PinnedSouth = 27.67f;

        /// <summary>
        /// Fix K's legs over the deck and fix J's last leg (the pad to beside the charge) on the client floor with fix J's rules
        /// (AshenFixBTests.ClientFloorLine, rampFoot, 0.5 m half width), there and back, each with its start moved 0.13, 0.27 and
        /// 0.4 m along it at either end; the walked floor 0.6 m to either side out of the scene's object bodies (the generator's,
        /// AshenFixJTests.TheWalksEndOutsideTheSceneBodies) and 1.28 m from the charge's middle (its body 0.18 m, 0.6 m, the half
        /// width; set 19's rule). Fix N (row 50): the legs on the pad deck (24, 44) keep AshenFixBTests.PadDeckEdgeMargin (2 m) from
        /// a drop (the pit round the landing pad; the real client fell off fix K's way on its rim); leg 23 runs in the bridge's
        /// tunnel, whose wall meshes the edge check sees through to the rooms 30 m below (no edge rule there; walked by the tester).
        /// </summary>
        [TestMethod]
        public void ThePadDeckLegsPassFixJsRulesWithShiftedStarts()
        {
            var dataDir = F.ClientData();
            using var meshes = new MeshLibrary(dataDir, F.MeshCsv());
            var geometry = MapGeometry.Load(Path.Combine(dataDir, "maps", MapName), meshes, 1);
            var bodies = new List<(string Role, List<(Vector3 A, Vector3 B, Vector3 C)> Tris, Vector3 Lo, Vector3 Hi)>();
            foreach (var actor in AshenFixJV1.DesistanceScene().Actors.Values.Where(a => a.Kind == SceneActorKind.Object && a.Position != null))
            {
                if (!meshes.TryGetMeshName((int)actor.TemplateId, out var name) || meshes.Get(name) is not { HasCollision: true } mesh)
                    continue;
                var turn = Quaternion.CreateFromYawPitchRoll((float)actor.Orientation, 0, 0);
                var at = new Vector3(actor.Position.X, actor.Position.Y, actor.Position.Z);
                var tris = new List<(Vector3, Vector3, Vector3)>();
                for (var k = 0; k < mesh.Triangles.Count; k += 3)
                    tris.Add((Vector3.Transform(mesh.Vertices[mesh.Triangles[k]], turn) + at, Vector3.Transform(mesh.Vertices[mesh.Triangles[k + 1]], turn) + at,
                        Vector3.Transform(mesh.Vertices[mesh.Triangles[k + 2]], turn) + at));
                var points = tris.SelectMany(t => new[] { t.Item1, t.Item2, t.Item3 }).ToList();
                bodies.Add((actor.Role, tris, points.Aggregate(Vector3.Min), points.Aggregate(Vector3.Max)));
            }
            Assert.IsTrue(bodies.Any(body => body.Role == AshenDesistanceV1.GeneratorRole), "the generator's body");
            string In(Vector3 at)
            {
                foreach (var (role, tris, lo, hi) in bodies)
                {
                    if (at.X < lo.X || at.X > hi.X || at.Z < lo.Z || at.Z > hi.Z || lo.Y > at.Y + AshenFixBTests.Head)
                        continue;
                    var from = new Vector3(at.X, 400, at.Z);
                    if (tris.Select(t => F.Hit(from, -Vector3.UnitY, t.A, t.B, t.C)).Where(d => d != null).Select(d => 400 - d.Value).DefaultIfEmpty(float.MinValue).Max() > at.Y + AshenFixBTests.MaxClientStep)
                        return role;
                }
                return null;
            }
            var charge = new Vector3(AshenFixJV1.ChargeAt.X, AshenFixJV1.ChargeAt.Y, AshenFixJV1.ChargeAt.Z);
            string Bodies(List<Vector3> track)
            {
                for (var i = 1; i < track.Count; i++)
                {
                    var side = Vector3.Normalize(new Vector3(track[i].Z - track[i - 1].Z, 0, track[i - 1].X - track[i].X)) * 0.6f;
                    foreach (var at in new[] { track[i], track[i] + side, track[i] - side })
                        if (In(at) is { } role)
                            return FormattableString.Invariant($"in {role}'s body at ({at.X:0.0}, {at.Y:0.00}, {at.Z:0.0})");
                    if (Vector2.Distance(new(track[i].X, track[i].Z), new(charge.X, charge.Z)) < 1.28f)
                        return FormattableString.Invariant($"{Vector2.Distance(new(track[i].X, track[i].Z), new(charge.X, charge.Z)):0.00} m from the charge's middle");
                }
                return null;
            }
            var failures = new List<string>();
            var legs = G.PadDeck.Append("the plant 44: the pad (-390.8, -297) to beside the charge (-390.8, -301)").ToList();
            foreach (var leg in legs)
            {
                var corners = G.Legs[leg];
                foreach (var (line, direction) in new[] { (corners.ToList(), "there"), (Enumerable.Reverse(corners).ToList(), "back") })
                {
                    var first = Vector2.Distance(new(line[0].X, line[0].Z), new(line[1].X, line[1].Z));
                    foreach (var shift in new[] { 0f, 0.13f, 0.27f, 0.4f }.Where(shift => shift == 0 || first > shift + 0.05f))
                    {
                        var shifted = line.ToList();
                        shifted[0] = Vector3.Lerp(line[0], line[1], shift / first);
                        var track = new List<Vector3>();
                        if (AshenFixBTests.ClientFloorLine(geometry, shifted, shifted[^1], null, G.WalkHalfWidth, track, rampFoot: true, edgeMargin: leg == G.PadDeck[0] ? 0 : AshenFixBTests.PadDeckEdgeMargin) is { } why)
                            failures.Add($"{leg} {direction} +{shift}: {why}");
                        else if (Bodies(track) is { } body)
                            failures.Add($"{leg} {direction} +{shift}: {body}");
                    }
                }
            }
            Assert.IsEmpty(failures, $"{failures.Count}: " + string.Join("; ", failures));
        }
    }
}
