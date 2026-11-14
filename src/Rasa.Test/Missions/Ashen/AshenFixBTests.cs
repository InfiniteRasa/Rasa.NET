using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.ClientData;
using Rasa.Data;
using Rasa.Game;
using Rasa.Navigation;
using Rasa.Services.Preloader.Missions.Ashen;
using Rasa.Structures;
using Rasa.Test.Missions.Marshes;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions.Ashen
{
    using F = MarshesClientFloorTests;
    using X = AshenFixBV1;

    /// <summary>
    /// Ashen fix job B (to-fix row 5): Chemical Oversight's (1244) Thrax Overseer Exing, pool 520007, stands on the flat at the
    /// mesa's south foot, and 1244's marker on him.
    ///
    /// Why these tests: in the real client no walk reached him at set 2's spot on the mesa's slope: every navmesh route ended on
    /// a flank the client does not climb. A spot that only the navmesh reaches, a walk with a step over 1.1 m (0.9 m in fix B) or something in
    /// the way at chest height, a marker off him, or a Down that does not put set 2's pool and marker back would block 1244 again.
    ///
    /// The map: Ligo Ashen Desert (1734), loaded as AshenLeadInsTests loads it. The client floor checks read the client's files
    /// (as MarshesClientFloorTests).
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class AshenFixBTests
    {
        private const string FixB = "20271110000610_Ashen_fix_b";

        /// <summary>
        /// The most the client's floor rises from one metre to the next on a walk. 1.1 m: the real client (tester 2026-11-09
        /// 23:41-23:54) walked into the Boneyard Thrax pool 531041, stood at (477.5, 272.1, -226.1) and walked out to (439.0,
        /// 288.3, -298.7). On that walk this check measures 0.99 m over a metre in (from the Ashoka Settlement waypoint) and,
        /// out, 1.01 m (from the pool's point) to 1.05 m (from the tester's spot) at (470.6, -231.1). 0.9 m before (2026-11-09
        /// probe). The Exing slopes the fail-proofs use rise 6-9 m, far above it.
        /// </summary>
        internal const float MaxClientStep = 1.1f;

        /// <summary>The height over the floor that must be clear from one metre to the next.</summary>
        internal const float Chest = 1.2f;

        /// <summary>The heights over the floor at knee and head that must be clear as well (fix B review): an obstacle 0.9-1.2 m high passes under the chest line.</summary>
        internal const float Knee = 0.5f, Head = 1.8f;

        /// <summary>The walk is sampled every half metre along its corners as one line.</summary>
        internal const float Spacing = 0.5f;

        /// <summary>The places the real client reached on its own feet near the mesa (1242's Atta pool 790051, 1247's camp pools 790087, 790085).</summary>
        internal static readonly (string Name, Vector3 At)[] RealClientPlaces =
        {
            ("the Mauna Barrens basin (pool 790051)", new Vector3(-405f, 300.97f, -272f)),
            ("the Sheitan Highway camp (pool 790087)", new Vector3(-160f, 304.19f, -260f)),
            ("the Sheitan Highway camp (pool 790085)", new Vector3(-180f, 306.57f, -305f))
        };

        private static Vector3 V((double X, double Y, double Z) at) => new((float)at.X, (float)at.Y, (float)at.Z);

        /// <summary>
        /// The pool as the server loads it: Exing's row (creature, count, radius 0, respawn as set 2 left it) at the new place;
        /// 1244's one marker (r15) on it at the navmesh ground; Exing comes down within 3 m of it, on the ground.
        /// </summary>
        [TestMethod]
        public void ExingStandsAtTheMesasFootWithTheMarkerOnHim()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var row = harness.World.SpawnPoolEntries.AsNoTracking().Single(entry => entry.Id == X.Pool);
            Assert.AreEqual((X.Exing, 0.0), (row.Creature1Id, row.Radius), "Exing's point pool");
            Assert.IsLessThan(0.01, Vector3.Distance(V(X.PoolNow), V((row.PosX, row.PosY, row.PosZ))), $"at {X.PoolNow}");
            var markers = harness.World.MissionIndicatorEntries.AsNoTracking()
                .Where(entry => entry.MissionId == X.MissionId && entry.ContentRevision == AshenMissionDataV1.Revision).ToList();
            Assert.HasCount(1, markers, "1244's one marker");
            Assert.AreEqual((X.Marker.Objective, X.Marker.Indicator, AshenChemicalOversightV1.MarkerRadius), (markers[0].ObjectiveId, markers[0].IndicatorId, markers[0].Radius));
            Assert.IsLessThan(0.01, Vector3.Distance(V(X.MarkerNow), V((markers[0].PosX, markers[0].PosY, markers[0].PosZ))), "the marker on the point");

            var desert = AshenLeadInsTests.EnterAshen(harness, AshenLeadInsTests.ShadowsEdgeWaypoint);
            Assert.AreEqual(X.MarkerNow.Y, desert.NavMesh.GroundHeight(V(X.PoolNow)) ?? float.NaN, 0.05, "the marker at the navmesh ground");
            Plateau.PlateauCampResistanceTests.Spawn(desert, X.Pool);
            var exing = MarshesPaludosTests.Alive(desert).Single(candidate => candidate.DbId == X.Exing && candidate.SpawnPool?.DbId == X.Pool);
            Assert.IsLessThan(3f, Vector2.Distance(new((float)X.PoolNow.X, (float)X.PoolNow.Z), new(exing.Position.X, exing.Position.Z)), "Exing at the point");
            MarshesPaludosTests.AssertOnGround(desert, exing.Position, "Exing");
        }

        /// <summary>
        /// The play's case, on the client's floor: the navmesh's walks to Exing from the Shadow's Edge Post waypoint and from the
        /// places the real client reached near the mesa (the Barrens basin, the highway camp) keep a client floor all the way,
        /// one metre at a time, never more than MaxClientStep (1.1 m) up, with nothing of the placed meshes in the way at chest height; the
        /// Stonewall Ridge waypoint's navmesh walk is complete (the real client walked it to the mesa's foot, 2026-11-09). Set 2's
        /// spot on the mesa is not reached on the client floor from the basin or the camp (why it moved).
        /// </summary>
        [TestMethod]
        public void ExingIsWalkedToOnTheClientsFloor()
        {
            var dataDir = F.ClientData();
            using var harness = WildernessRuntimeTestHarness.Create();
            var desert = AshenLeadInsTests.EnterAshen(harness, AshenLeadInsTests.ShadowsEdgeWaypoint);
            var nav = desert.NavMesh;
            using var meshes = new MeshLibrary(dataDir, F.MeshCsv());
            var geometry = MapGeometry.Load(Path.Combine(dataDir, "maps", harness.World.MapInfoEntries.AsNoTracking()
                .Single(map => map.Id == AshenMissionDataV1.MapContextId).MapName), meshes, 1);
            var exing = nav.Nearest(V(X.PoolNow)).Value;
            foreach (var (name, from) in RealClientPlaces.Prepend(("the Shadow's Edge Post waypoint", AshenLeadInsTests.ShadowsEdgeWaypoint)))
            {
                var failure = ClientFloorWalk(nav, geometry, from, exing);
                Assert.IsNull(failure, $"Exing from {name}: {failure}");
            }
            var back = ClientFloorWalk(nav, geometry, exing, AshenLeadInsTests.ShadowsEdgeWaypoint);
            Assert.IsNull(back, $"from Exing back to the Shadow's Edge Post waypoint: {back}");
            Assert.IsTrue(nav.FindPath(nav.Nearest(AshenLeadInsTests.StonewallWaypoint).Value, exing, out var complete) != null && complete,
                "Exing walked to from the Stonewall Ridge waypoint on the navmesh");

            var before = nav.Nearest(V(X.PoolBefore)).Value;
            foreach (var (name, from) in RealClientPlaces.Take(2))
            {
                var failure = ClientFloorWalk(nav, geometry, from, before);
                Assert.IsTrue(failure?.StartsWith("no client floor within", StringComparison.Ordinal) == true,
                    $"set 2's spot from {name}: the client floor does not climb the flank ({failure})");
            }
        }

        /// <summary>
        /// The ground round the point (fix A's ClearRadius, where Exing strolls and fights): every navmesh floor on a 1 m grid
        /// is one floor, joined to the Shadow's Edge Post waypoint's ground, within 1 m of a client floor (up to 1.5 m over it)
        /// with no client surface over it.
        /// </summary>
        [TestMethod]
        public void TheGroundRoundExingIsOpenAndJoined()
        {
            var dataDir = F.ClientData();
            using var harness = WildernessRuntimeTestHarness.Create();
            var desert = AshenLeadInsTests.EnterAshen(harness, AshenLeadInsTests.ShadowsEdgeWaypoint);
            var nav = desert.NavMesh;
            var region = nav.RegionArea(nav.Nearest(AshenLeadInsTests.ShadowsEdgeWaypoint).Value);
            var centre = V(X.PoolNow);
            var columns = new List<List<Vector3>>();
            var reach = (int)Math.Ceiling(X.ClearRadius);
            for (var dz = -reach; dz <= reach; dz++)
                for (var dx = -reach; dx <= reach; dx++)
                {
                    if (dx * dx + dz * dz > X.ClearRadius * X.ClearRadius)
                        continue;
                    var heights = new SortedSet<float>();
                    for (var h = -8; h <= 12; h++)
                    {
                        var probe = new Vector3(centre.X + dx, centre.Y + h, centre.Z + dz);
                        if (nav.Nearest(probe) is { } nearest && MathF.Abs(nearest.X - probe.X) < 0.3f && MathF.Abs(nearest.Z - probe.Z) < 0.3f &&
                            nav.GroundHeight(probe) is { } y)
                            heights.Add(MathF.Round(y, 1));
                    }
                    if (heights.Count == 0)
                        continue;
                    var column = new List<Vector3>();
                    foreach (var y in heights)
                        if (column.Count == 0 || y - column[^1].Y >= 2)
                            column.Add(new Vector3(centre.X + dx, y, centre.Z + dz));
                    columns.Add(column);
                }
            Assert.IsGreaterThan(500, columns.Count, "the floors round the point are read");
            Assert.IsEmpty(columns.Where(column => column.Count != 1).Take(5).ToList(), "one floor in each spot");
            var ground = columns.SelectMany(column => column).ToList();
            Assert.IsEmpty(ground.Where(at => nav.RegionArea(at) != region).Take(5).ToList(), "joined to the waypoints");

            using var meshes = new MeshLibrary(dataDir, F.MeshCsv());
            var geometry = MapGeometry.Load(Path.Combine(dataDir, "maps", harness.World.MapInfoEntries.AsNoTracking()
                .Single(map => map.Id == AshenMissionDataV1.MapContextId).MapName), meshes, 1);
            var near = F.Near(geometry, ground, 0.5f);
            var failures = new List<string>();
            for (var i = 0; i < ground.Count; i++)
            {
                var at = ground[i];
                if (F.FloorAt(near[i], at, at.Y - 2, at.Y + 1.5f) is not { } y || y - at.Y > 1 || at.Y - y > 1.5f)
                    failures.Add(FormattableString.Invariant($"no client floor at ({at.X:0.0}, {at.Y:0.00}, {at.Z:0.0})"));
                if (near[i].Any(t => F.Hit(at + new Vector3(0, 1, 0), Vector3.UnitY, t.A, t.B, t.C) is { } up && up < 40))
                    failures.Add(FormattableString.Invariant($"a client surface over ({at.X:0.0}, {at.Y:0.00}, {at.Z:0.0})"));
            }
            Assert.IsEmpty(failures, $"{failures.Count}: " + string.Join("; ", failures.Take(10)));
        }

        /// <summary>
        /// The lone steep sample rule (SteepBand): on a made-up floor (flat, a 57 degree band climbed along x, flat again) the
        /// steep stretch round one sample in a 0.8 m band is over SteepBand (a band SteepRun alone let pass, one sample in it), and
        /// round one in a 0.3 m band (a seam's sliver) is not. The seams themselves count as their flat side (FloorNormal). A steep
        /// skin SteepLip or less over a climbable floor is a lip (0 m), one higher a band.
        /// </summary>
        [TestMethod]
        public void LoneSteepSampleIsASeamOnlyWhenItsStretchIsShort()
        {
            var rise = MathF.Tan(57 * MathF.PI / 180);
            static void Quad(List<(Vector3 A, Vector3 B, Vector3 C)> list, float x0, float y0, float x1, float y1)
            {
                Vector3 a = new(x0, y0, -5), b = new(x1, y1, -5), c = new(x1, y1, 5), d = new(x0, y0, 5);
                list.Add((a, b, c));
                list.Add((a, c, d));
            }
            (float Length, Vector3 From, Vector3 To) Stretch(float band)
            {
                var floor = new List<(Vector3 A, Vector3 B, Vector3 C)>();
                Quad(floor, -5, 0, 0, 0);
                Quad(floor, 0, 0, band, band * rise);
                Quad(floor, band, band * rise, 5, band * rise);
                var here = new Vector3(band / 2, band / 2 * rise, 0);
                Assert.IsLessThan(MinClimbNormal, FloorNormal(floor, here) ?? 1, $"{band} m band: the sample is steep");
                return SteepStretch(floor, new Vector3(band / 2 - Spacing, 0, 0), here, new Vector3(band / 2 + Spacing, band * rise, 0));
            }

            var wide = Stretch(0.8f);
            Assert.IsGreaterThan(SteepBand, wide.Length, $"0.8 m band: {wide}");
            Assert.IsGreaterThan(0.65f, wide.Length, $"0.8 m band measured: {wide}");
            var seam = Stretch(0.3f);
            Assert.IsLessThanOrEqualTo(SteepBand, seam.Length, $"0.3 m band: {seam}");
            Assert.IsGreaterThan(0.15f, seam.Length, $"0.3 m band measured: {seam}");

            // A lip (SteepLip): a 57 degree skin walked along its contour (z) 1 m, over a level floor. 0.1 m over it the player
            // stands on the floor under it; 0.4 m over it the skin is a band.
            (float Length, Vector3 From, Vector3 To) Skin(float over)
            {
                var floor = new List<(Vector3 A, Vector3 B, Vector3 C)>();
                Quad(floor, -5, 0, 5, 0);
                Quad(floor, -0.1f, over - 0.1f * rise, 0.1f, over + 0.1f * rise);
                var here = new Vector3(0, over, 0);
                Assert.IsLessThan(MinClimbNormal, FloorNormal(floor, here) ?? 1, $"skin {over} m over the floor: the sample is steep");
                return SteepStretch(floor, new Vector3(0, over, -Spacing), here, new Vector3(0, over, Spacing));
            }
            var lip = Skin(0.1f);
            Assert.AreEqual(0f, lip.Length, $"skin 0.1 m over the floor: a lip, {lip}");
            var high = Skin(0.4f);
            Assert.IsGreaterThan(SteepBand, high.Length, $"skin 0.4 m over the floor: a band, {high}");
        }

        /// <summary>
        /// Ashen_fix_b rolls back: Down puts set 2's pool spot and marker back and takes out its evidence row; no other Ashen pool
        /// moves; Up again, the fix is back.
        /// </summary>
        [TestMethod]
        public void FixBRollsBackWithItsMigration()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            (double X, double Y, double Z) Pool()
            {
                var row = world.SpawnPoolEntries.AsNoTracking().Single(entry => entry.Id == X.Pool);
                return (row.PosX, row.PosY, row.PosZ);
            }
            (double X, double Y, double Z) Marker()
            {
                var row = world.MissionIndicatorEntries.AsNoTracking().Single(entry => entry.MissionId == X.MissionId &&
                    entry.ContentRevision == AshenMissionDataV1.Revision && entry.ObjectiveId == X.Marker.Objective && entry.IndicatorId == X.Marker.Indicator);
                return (row.PosX, row.PosY, row.PosZ);
            }
            bool Evidence() => world.MissionEvidenceEntries.AsNoTracking().Any(entry => entry.MissionId == X.MissionId &&
                entry.ContentRevision == AshenMissionDataV1.Revision && entry.EvidenceId == X.Evidence);
            void Is((double X, double Y, double Z) expected, (double X, double Y, double Z) actual, string what) =>
                Assert.IsLessThan(0.01, Vector3.Distance(V(expected), V(actual)), $"{what}: {expected}, is {actual}");
            // The pools of later migrations (set 7's on): they go down and up with the Down to fix B's predecessor.
            var later = AshenMissionTests.PoolsOfLaterMigrations(world, FixB);
            string Others() => string.Join("; ", world.SpawnPoolEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == AshenMissionDataV1.MapContextId && entry.Id != X.Pool && !later.Contains(entry.Id)).OrderBy(entry => entry.Id)
                .Select(entry => new { entry.Id, entry.PosX, entry.PosY, entry.PosZ, entry.Radius }).AsEnumerable().Select(entry => entry.ToString()));
            void Fixed()
            {
                Is(X.PoolNow, Pool(), "the pool");
                Is(X.MarkerNow, Marker(), "the marker");
                Assert.IsTrue(Evidence(), "fix B's evidence");
            }
            var others = Others();

            Fixed();
            world.GetService<IMigrator>().Migrate(AshenMissionTests.Previous(world, FixB));
            world.ChangeTracker.Clear();
            Assert.AreEqual(AshenMaunaBarrensNpcsV1.ExingAt, Pool(), "set 2's spot, as set 2 wrote it");
            Assert.AreEqual(AshenMaunaBarrensNpcsV1.ExingAt, Marker(), "set 2's marker, as set 2 wrote it");
            Assert.IsFalse(Evidence(), "no fix B evidence");
            Assert.IsTrue(world.MissionContentDefinitionEntries.Any(entry => entry.MissionId == X.MissionId && entry.ContentRevision == AshenMissionDataV1.Revision && entry.Enabled),
                "1244 stays (set 2)");
            Assert.AreEqual(others, Others(), "no other Ashen pool moves");

            world.GetService<IMigrator>().Migrate();
            world.ChangeTracker.Clear();
            Fixed();
            Assert.AreEqual(others, Others(), "no other Ashen pool moves");
        }

        /// <summary>
        /// The navmesh's walk from <paramref name="from"/> to <paramref name="to"/> on the client's floor: one metre at a time
        /// along its corners, the floor at each never more than MaxClientStep over the one before (down to 2.5 m under it), and
        /// nothing of the placed meshes in the way at Chest height from one to the next (the terrain heightmap is a surface to
        /// stand on, not a solid: TerrainHeightmap.AppendTriangles; its steps are the floor's). Null when it gets there, else why not.
        /// </summary>
        internal static string ClientFloorWalk(NavMeshQuery nav, MapGeometry geometry, Vector3 from, Vector3 to)
        {
            var start = nav.Nearest(from) ?? from;
            var path = nav.FindPath(start, to, out var complete);
            if (path == null || !complete)
                return "no complete navmesh walk";
            return ClientFloorLine(geometry, path.Prepend(start).ToList(), to);
        }

        /// <summary>
        /// The walk along <paramref name="corners"/> (in order; the first one's height is the start's floor) on the client's floor,
        /// with ClientFloorWalk's checks: for a route the navmesh does not join (Ashen fix C: Ashoka's corridor, upstream N18).
        /// Where <paramref name="meshFloorOnly"/> holds, only the placed meshes are floor, not the heightmap (a Brann corridor
        /// the terrain runs through, AshenFormalComplaintsTests.InAshokaCorridor). With <paramref name="halfWidth"/> the knee, chest
        /// and head lines are checked that far to each side as well (a player's body, not a line: doorways and corners).
        /// <paramref name="track"/> gets the floor of each sample. With <paramref name="rampFoot"/> (Ashen fix J, to-fix row 42:
        /// the real client stayed on 1451's lower deck beside the ramp up to life support 1) the walk must start and end on its
        /// corners' own deck (DeckMatch, not the highest floor within 1 m / 1.5 m): a leg that ends on another deck has to climb
        /// to it on the client floor from a start on the lower one, at the ramp's foot (MaxClientStep keeps it off the ramp's
        /// side but for its last metre at the foot), not from a start that snapped onto the ramp beside it. With
        /// <paramref name="edgeMargin"/> (Ashen fix N, row 50: the real client fell off 1452's pad trench rim where the line kept it)
        /// every sample keeps that far (2D) from a drop deeper than EdgeDrop (DropEdge). Every walk refuses a climb on RefusedSlopes.
        /// </summary>
        internal static string ClientFloorLine(MapGeometry geometry, IReadOnlyList<Vector3> corners, Vector3 to, Func<Vector3, bool> meshFloorOnly = null, float halfWidth = 0, List<Vector3> track = null, bool rampFoot = false, float edgeMargin = 0)
        {
            // Points every Spacing along the corners as one line (a short leg between corners is sampled too), and the last corner.
            var points = new List<Vector3> { corners[0] };
            var carried = 0f;
            for (var k = 1; k < corners.Count; k++)
            {
                var length = Vector2.Distance(new(corners[k - 1].X, corners[k - 1].Z), new(corners[k].X, corners[k].Z));
                var along = Spacing - carried;
                for (; along <= length; along += Spacing)
                    points.Add(Vector3.Lerp(corners[k - 1], corners[k], along / length));
                carried = length - (along - Spacing);
            }
            if (Vector2.Distance(new(points[^1].X, points[^1].Z), new(corners[^1].X, corners[^1].Z)) > 0.01f)
                points.Add(corners[^1]);
            var near = F.Near(geometry, points, 2);
            var edgeNear = edgeMargin > 0 ? F.Near(geometry, points, edgeMargin + 1) : null;
            var slopeMap = SlopeMapOf(geometry);
            bool OnTerrain((Vector3 A, Vector3 B, Vector3 C) t) =>
                new[] { t.A, t.B, t.C }.All(v => geometry.Terrain.Height(v.X, v.Z) is { } h && MathF.Abs(h - v.Y) < 0.01f);
            List<(Vector3 A, Vector3 B, Vector3 C)> Floor(int i) => meshFloorOnly?.Invoke(points[i]) == true ? near[i].Where(t => !OnTerrain(t)).ToList() : near[i];
            var y = F.FloorAt(Floor(0), points[0], points[0].Y - 2, points[0].Y + 1) ?? points[0].Y;
            if (rampFoot && MathF.Abs(y - points[0].Y) > DeckMatch)
                return FormattableString.Invariant($"starts on the floor at {y:0.00}, not on the corner's {points[0].Y:0.00} (another deck)");
            var floors = new List<Vector3>();
            Vector3? steep = null;
            for (var i = 0; i < points.Count; i++)
            {
                if (F.FloorAt(Floor(i), points[i] with { Y = y }, y - 2.5f, y + MaxClientStep) is not { } floor)
                    return FormattableString.Invariant($"no client floor within {MaxClientStep} m up at ({points[i].X:0.0}, {points[i].Z:0.0}) from {y:0.00} (navmesh {points[i].Y:0.00})");
                var here = points[i] with { Y = floor };
                // edgeMargin (fix N, row 50): the client walks off a deck's edge that the line keeps by less than the margin.
                if (edgeMargin > 0 && DropEdge(edgeNear[i], here, edgeMargin) is { } edge)
                    return FormattableString.Invariant($"within {edge.Distance:0.00} m of a {here.Y - edge.At.Y:0.0} m drop at ({here.X:0.0}, {here.Y:0.00}, {here.Z:0.0}) to ({edge.At.X:0.0}, {edge.At.Y:0.00}, {edge.At.Z:0.0}): an edge the client walks off (margin {edgeMargin} m)");
                // Fix N (row 50): a terrain slope the real client refused though it is under MaxClimbAngle here (RefusedSlopes).
                if (floors.Count > 0 && here.Y - floors[^1].Y > 0.02f && RefusedSlopeAt(slopeMap, here) is { } refused && Under(Floor(i), here) is { } ground && OnTerrain(ground)
                    && FloorNormal(Floor(i), here) is { } slope && slope < RefusedSlopeNormal)
                    return FormattableString.Invariant($"climbs {refused} at ({here.X:0.0}, {here.Y:0.00}, {here.Z:0.0}): the real client does not climb it");
                // The client's slope limit (fix G): a floor steeper than MaxClimbAngle is climbed nowhere, also not across its
                // fall line; only a walk that goes down on it (it slides) passes. One steep sample alone (one triangle of the 1 m
                // terrain grid) is let pass; a second within SteepRun of it is a slope the player slides off. The lone sample
                // passes only as a sampling artefact (a seam, a sliver or a triangle's corner, or a lip over a climbable floor:
                // SteepLip): the steep floor round it, probed every SteepProbe along the line back to the sample before and on to
                // the next, is no longer than SteepBand. A longer stretch is a steep band the samples straddle (0.5-1 m holds one
                // sample only).
                if (floors.Count > 0 && here.Y - floors[^1].Y > -SlideDrop && FloorNormal(Floor(i), here) is { } normal && normal < MinClimbNormal)
                {
                    if (steep is { } last && Vector2.Distance(new(last.X, last.Z), new(here.X, here.Z)) <= SteepRun)
                        return FormattableString.Invariant($"too steep to climb at ({here.X:0.0}, {here.Y:0.00}, {here.Z:0.0}): the floor is {MathF.Acos(normal) * 180 / MathF.PI:0} degrees, over the client's {MaxClimbAngle}, from ({last.X:0.0}, {last.Z:0.0})");
                    var (band, bandFrom, bandTo) = SteepStretch(Floor(i), floors[^1], here, i + 1 < points.Count ? points[i + 1] : here);
                    if (band > SteepBand)
                        return FormattableString.Invariant($"too steep to climb at ({here.X:0.0}, {here.Y:0.00}, {here.Z:0.0}): the floor is {MathF.Acos(normal) * 180 / MathF.PI:0} degrees, over the client's {MaxClimbAngle}, for {band:0.00} m from ({bandFrom.X:0.0}, {bandFrom.Y:0.00}, {bandFrom.Z:0.0}) to ({bandTo.X:0.0}, {bandTo.Y:0.00}, {bandTo.Z:0.0}) (over {SteepBand} m: a band, not a seam)");
                    steep = here;
                }
                // rampFoot (fix J, row 44): a step of more than TerrainEdge off the terrain onto a placed mesh is a sill the client
                // does not climb: the client's ground there lies lower than this 1 m heightmap (1452's pad trench rim at (-341, -304):
                // 1.09 m here, the client stood at 174.9, 0.4 m under the heightmap, and stopped).
                if (rampFoot && floors.Count > 0 && here.Y - floors[^1].Y > TerrainEdge
                    && Under(Floor(i - 1), floors[^1]) is { } low && OnTerrain(low) && Under(Floor(i), here) is { } high && !OnTerrain(high))
                    return FormattableString.Invariant($"steps {here.Y - floors[^1].Y:0.00} m off the terrain onto a mesh at ({here.X:0.0}, {here.Y:0.00}, {here.Z:0.0}): a sill the client does not climb (over {TerrainEdge} m)");
                // The rise over the last metre of the walk (the samples back to 1 m behind), not only from the sample before.
                for (var back = floors.Count - 1; back >= 0; back--)
                {
                    var run = Vector2.Distance(new(floors[back].X, floors[back].Z), new(here.X, here.Z));
                    if (run > 1.001f)
                        break;
                    if (here.Y - floors[back].Y > MaxClientStep)
                        return FormattableString.Invariant($"no client floor within {MaxClientStep} m up at ({here.X:0.0}, {here.Z:0.0}): {here.Y - floors[back].Y:0.00} m up over {run:0.00} m");
                }
                if (floors.Count > 0)
                {
                    var before = floors[^1];
                    var solids = near[i - 1].Concat(near[i]).Where(t => !OnTerrain(t)).ToList();
                    var across = Vector3.Normalize(new Vector3(before.Z - here.Z, 0, here.X - before.X)) * halfWidth;
                    var sides = halfWidth > 0 ? new[] { Vector3.Zero, across, -across } : new[] { Vector3.Zero };
                    foreach (var (height, name) in new[] { (Knee, "knee"), (Chest, "chest"), (Head, "head") })
                        foreach (var side in sides)
                            if (!F.Clear(solids, before + side + new Vector3(0, height, 0), here + side + new Vector3(0, height, 0)))
                                return FormattableString.Invariant($"something in the way at {name} height from ({before.X:0.0}, {before.Y:0.00}, {before.Z:0.0}) to ({here.X:0.0}, {here.Y:0.00}, {here.Z:0.0}){(side == Vector3.Zero ? "" : " beside the line")}");
                }
                y = floor;
                floors.Add(here);
                track?.Add(here);
            }
            return MathF.Abs(y - to.Y) > (rampFoot ? DeckMatch : 1.5f) ? FormattableString.Invariant($"ends on the floor at {y:0.00}, {to.Y:0.00} wanted") : null;
        }

        /// <summary>
        /// The steepest floor the real client climbs, in degrees: tabula_rasa.exe 1.16.5.0 reads Client.Developer.MaxAngleToClimb
        /// (default 55, not set in client.cfg) at start (code at 0x4cdbc9: 55 / 180 * pi, cos, kept at 0xd02e50 for the movement
        /// code at 0x517fd0-0x518660; 1.0 for the value, ESTIMATE that the movement applies it as a floor-normal limit).
        /// Calibrated (fix G) on to-fix row 24: the real client could not climb (99, 482) -> (86, 469) on Indra Pass terrain of 52-59
        /// degrees across the line (stops at (89.4, 293.9, 468.0), (92.9, 292.6, 472.0), (92.4, 293.2, 472.0), (89.2, 293.9, 467.6),
        /// (88.5, 294.1, 466.9)), a line the 1.1 m step rule passes (it rises 0.3-0.7 m per metre along the line). 50 degrees
        /// (tried, 2026-11-11) failed walks the real client made: Exing from the Mauna Barrens basin (fix B), the Sheitan Highway
        /// camp's Ravager pool 790085, Stonewall Ridge's towers (1277, 1278 PASS); 55 fails only set 13's walks. The tester's stop
        /// at (394.2, 301.2, -185.5) on Maiek's walk (terrain of 50-53 degrees) is not explained by it (ESTIMATE: the helper's line
        /// or the client's contact on the crease); Maiek's walk keeps 9 m off it (fix G).
        /// </summary>
        internal const float MaxClimbAngle = 55f;

        /// <summary>cos(MaxClimbAngle): the least floor normal's y the client climbs on.</summary>
        internal static readonly float MinClimbNormal = MathF.Cos(MaxClimbAngle * MathF.PI / 180);

        /// <summary>Two climbing samples on a floor steeper than MaxClimbAngle this close (2D) fail a walk; one alone passes (ESTIMATE: the client steps over one steep triangle of the 1 m terrain grid).</summary>
        internal const float SteepRun = 1.001f;

        /// <summary>
        /// The longest steep stretch (2D, along the line) round a lone steep sample that still passes as a sampling artefact: the
        /// sample spacing. A longer one is a band the 0.5 m samples straddle with one sample (a 0.5-1 m band slipped through
        /// SteepRun alone). ESTIMATE: no real-client stop calibrates it; it closes the gap between the samples.
        /// </summary>
        internal const float SteepBand = Spacing;

        /// <summary>The probe step of SteepStretch along the line.</summary>
        internal const float SteepProbe = 0.05f;

        /// <summary>
        /// A steep skin no higher than this over a climbable floor (normal's y at least MinClimbNormal) right under it is a lip,
        /// not a slope: the player stands on the floor under it and steps over it (it is under Knee, the lowest line the walk
        /// checks for obstacles). Measured (2026-11-14): Indra Caverns' "Diren to the Watch Post" leg at (-39.4, -358.1) to
        /// (-39.2, -357.6) runs 0.6 m along a 60 degree rock mesh face 0.06-0.13 m over 7 degree terrain. ESTIMATE: 0.25 m.
        /// </summary>
        internal const float SteepLip = 0.25f;

        /// <summary>Whether a climbable floor (normal's y at least MinClimbNormal) lies under <paramref name="at"/> by at most SteepLip.</summary>
        internal static bool OverClimbableFloor(List<(Vector3 A, Vector3 B, Vector3 C)> triangles, Vector3 at)
        {
            var from = at with { Y = at.Y - 0.01f };
            foreach (var (a, b, c) in triangles)
            {
                var n = Vector3.Cross(b - a, c - a);
                if (n.LengthSquared() < 1e-12f || MathF.Abs(Vector3.Normalize(n).Y) < MinClimbNormal)
                    continue;
                if (F.Hit(from, -Vector3.UnitY, a, b, c) is { } down && down <= SteepLip - 0.01f)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// The steep floor (normal's y under MinClimbNormal) round <paramref name="here"/>, a steep sample: probed every SteepProbe
        /// along the line back to <paramref name="before"/> (the sample before, its floor) and on to <paramref name="next"/> (the
        /// next sample's point), each probe's floor found from the one beside it as the walk does (2.5 m down, MaxClientStep up).
        /// A lip (OverClimbableFloor) is not steep. Returns the stretch's 2D length between its outermost steep probes, and those
        /// probes (0 when <paramref name="here"/> is a lip).
        /// </summary>
        internal static (float Length, Vector3 From, Vector3 To) SteepStretch(List<(Vector3 A, Vector3 B, Vector3 C)> triangles, Vector3 before, Vector3 here, Vector3 next)
        {
            Vector3 Reach(Vector3 toward)
            {
                var end = here;
                var length = Vector2.Distance(new(here.X, here.Z), new(toward.X, toward.Z));
                var y = here.Y;
                for (var along = SteepProbe; along <= length + 0.001f; along += SteepProbe)
                {
                    var probe = Vector3.Lerp(here, toward, MathF.Min(along / length, 1));
                    if (F.FloorAt(triangles, probe with { Y = y }, y - 2.5f, y + MaxClientStep) is not { } floor)
                        break;
                    probe.Y = y = floor;
                    if (FloorNormal(triangles, probe) is not { } normal || normal >= MinClimbNormal || OverClimbableFloor(triangles, probe))
                        break;
                    end = probe;
                }
                return end;
            }
            if (OverClimbableFloor(triangles, here))
                return (0, here, here);
            var from = Reach(before);
            var to = Reach(next);
            return (Vector2.Distance(new(from.X, from.Z), new(to.X, to.Z)), from, to);
        }

        /// <summary>How far the floor must drop from one sample to the next for a walk to go down a steep floor (it slides).</summary>
        internal const float SlideDrop = 0.1f;

        /// <summary>ClientFloorLine's rampFoot rule (fix J, row 42): a walk's start and end floor within this of its corners'
        /// heights (the corners are the client floor or the navmesh over it; the decks at 1451's life support 1 lie 5.5 m
        /// apart, the ramp's side 1.9 m over the floor beside it).</summary>
        internal const float DeckMatch = 0.5f;

        /// <summary>
        /// ClientFloorLine's rampFoot terrain edge (fix J, row 44): the highest step off the terrain onto a placed mesh. The real
        /// client did not climb 1452's pad trench rim (1.09 m on this heightmap, about 1.5 m from where it stood). ESTIMATE.
        /// </summary>
        internal const float TerrainEdge = 0.6f;

        /// <summary>
        /// ClientFloorLine's edge rule (Ashen fix N, to-fix row 50): a drop deeper than this beside the walk is a fall the walk
        /// must keep its edge margin from. Fix K's way round 1452's landing pad kept the pad's trench rim (176.4) 0.25-0.5 m from
        /// the pit 5.1-5.3 m below it (171.1-171.3); the real client fell into the pit there (2026-11-13). 2 m: over
        /// MaxClientStep, a drop the client does not step back up (ESTIMATE).
        /// </summary>
        internal const float EdgeDrop = 2f;

        /// <summary>
        /// The edge margin the 1452 pad deck walks keep (fix N): 2 m from a drop deeper than EdgeDrop. The body's half width
        /// (0.5 m) and 1.5 m for the client's walk off its line (ESTIMATE: the tester's drop was on a line 0.5 m from the pit's
        /// edge, on the rim's 30-40 degree slope down to it; the client turns at a corner in an arc, not on the point).
        /// </summary>
        internal const float PadDeckEdgeMargin = 2f;

        /// <summary>
        /// The nearest drop deeper than EdgeDrop within <paramref name="margin"/> (2D) of <paramref name="at"/> (its floor): a
        /// spot whose floor and the floor 0.5 m (a body's half width) further out both lie EdgeDrop or more under it, reached at
        /// knee height without a wall or rail in the way. Null if none. It sees through a wall whose collision mesh has a gap at knee
        /// height: in set 16's bridge tunnel (leg 23) it finds drops of 28-35 m to the rooms under the tunnel 0.25-1 m from the
        /// line, where the real client walked; so the rule is ClientFloorLine's opt-in edgeMargin, set on the pad deck legs only.
        /// </summary>
        internal static (float Distance, Vector3 At)? DropEdge(List<(Vector3 A, Vector3 B, Vector3 C)> triangles, Vector3 at, float margin)
        {
            const int Directions = 16;
            for (var r = 0.25f; r <= margin + 0.001f; r += 0.25f)
                for (var k = 0; k < Directions; k++)
                {
                    var direction = new Vector3(MathF.Cos(k * 2 * MathF.PI / Directions), 0, MathF.Sin(k * 2 * MathF.PI / Directions));
                    var spot = at + direction * r;
                    var further = spot + direction * 0.5f;
                    if (F.FloorAt(triangles, spot, at.Y - 40, at.Y + MaxClientStep) is { } low && at.Y - low > EdgeDrop
                        && F.FloorAt(triangles, further, at.Y - 40, at.Y + MaxClientStep) is { } beyond && at.Y - beyond > EdgeDrop
                        && F.Clear(triangles, at + new Vector3(0, Knee, 0), further with { Y = at.Y + Knee }))
                        return (r, spot with { Y = low });
                }
            return null;
        }

        /// <summary>
        /// Terrain slopes the real client refused to climb that the 55 degree rule passes (fix N, to-fix row 50): 1452's landing pad
        /// pit's north-east slope on Indra Caverns (2055), from the pit floor (170.8-171.3) up to the deck's ground (175.8). On the 1 m
        /// heightmap its floor is 44-51 degrees along the climb (one or two triangles of 56-58), 47-48 degrees over 2 m; the tester's
        /// climb on the navmesh's way out ((-343.5, 171.2, -276.8) > (-340.3, 174.8, -274.0) > (-339.5, 176.4, -268.8)) stopped, two
        /// tries (2026-11-13). Not a lower global limit: walks of the Ashen tests climb up to 47.6 degrees over 2 m (all 1231 passing walks, measured
        /// 2026-11-13) and 50 degrees failed walks the real client made (fix G). ESTIMATE: the client's own terrain triangles or
        /// contact differ from this heightmap's here. A sample counts when it climbs on terrain steeper than 35 degrees (RefusedSlopeNormal) inside the box (x, z) and height band: the
        /// slope's steep part (its foot to 0.3 m under the deck's ground). Leg 24 (fix K's and fix N's) passes through the box's
        /// north-east corner on the deck's ground (175.81-175.82, 0-7 degrees, measured 2026-11-13): over the band and under 35 degrees.
        /// </summary>
        internal static readonly (string Map, string Name, Vector2 Lo, Vector2 Hi, float Bottom, float Top)[] RefusedSlopes =
        {
            (IndraCaverns, "the landing pad pit's north-east slope (Indra Caverns, row 50)", new(-345, -279), new(-337.5f, -268), 170, 175.5f)
        };

        /// <summary>Indra Caverns (2055), the client map of 1451/1452's squad copy.</summary>
        internal const string IndraCaverns = "adv_arieki_ligo_ashendesert_indracaverns";

        /// <summary>Each RefusedSlopes map's terrain, read once: a walk's geometry is that map when its terrain is the same.</summary>
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, Lazy<TerrainHeightmap>> SlopeMaps = new();

        /// <summary>The map key a geometry's walks are on, cached per geometry (MapGeometry has no name).</summary>
        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MapGeometry, string> GeometryMaps = new();

        /// <summary>
        /// The RefusedSlopes map <paramref name="geometry"/> was loaded from, or "" when none: its terrain has that map's bounds,
        /// grid and heights (at its middle and four points round it). A walk on any other map never meets a RefusedSlopes entry.
        /// </summary>
        internal static string SlopeMapOf(MapGeometry geometry) => GeometryMaps.GetValue(geometry, g =>
        {
            if (g.Terrain is not { } terrain)
                return "";
            foreach (var map in RefusedSlopes.Select(entry => entry.Map).Distinct())
            {
                var known = SlopeMaps.GetOrAdd(map, name => new Lazy<TerrainHeightmap>(() =>
                    new TerrainHeightmap(Directory.EnumerateFiles(Path.Combine(F.ClientData(), "maps", name), "*_terrain.glm").First()))).Value;
                if (known.BoundsMin != terrain.BoundsMin || known.BoundsMax != terrain.BoundsMax || known.Columns != terrain.Columns || known.Rows != terrain.Rows)
                    continue;
                var middle = (known.BoundsMin + known.BoundsMax) / 2;
                var quarter = (known.BoundsMax - known.BoundsMin) / 4;
                if (new[] { Vector3.Zero, quarter, -quarter, quarter with { Z = -quarter.Z }, quarter with { X = -quarter.X } }
                    .All(offset => known.Height(middle.X + offset.X, middle.Z + offset.Z) == terrain.Height(middle.X + offset.X, middle.Z + offset.Z)))
                    return map;
            }
            return "";
        });

        /// <summary>The floor normal's y under which a climbing sample on a RefusedSlopes entry counts: steeper than 35 degrees (the
        /// slope's climb is 39-51 degrees from its foot up; the pit's floor beside it 7-24, walked across).</summary>
        internal static readonly float RefusedSlopeNormal = MathF.Cos(35 * MathF.PI / 180);

        /// <summary>The RefusedSlopes entry of map <paramref name="map"/> (SlopeMapOf) <paramref name="at"/> stands in, or null.</summary>
        internal static string RefusedSlopeAt(string map, Vector3 at)
        {
            foreach (var (on, name, lo, hi, bottom, top) in RefusedSlopes)
                if (on == map && at.X >= lo.X && at.X <= hi.X && at.Z >= lo.Y && at.Z <= hi.Y && at.Y >= bottom && at.Y <= top)
                    return name;
            return null;
        }

        /// <summary>The floor triangle right under <paramref name="at"/> (within 2 cm), if any.</summary>
        private static (Vector3 A, Vector3 B, Vector3 C)? Under(List<(Vector3 A, Vector3 B, Vector3 C)> triangles, Vector3 at)
        {
            var from = at with { Y = at.Y + 0.5f };
            foreach (var t in triangles)
                if (F.Hit(from, -Vector3.UnitY, t.A, t.B, t.C) is { } down && MathF.Abs(from.Y - down - at.Y) < 0.02f)
                    return t;
            return null;
        }

        /// <summary>The floor normal's y (the most level of the triangles at that height: an edge counts as its flatter side) under <paramref name="at"/>, or null.</summary>
        internal static float? FloorNormal(List<(Vector3 A, Vector3 B, Vector3 C)> triangles, Vector3 at)
        {
            float? best = null;
            var from = at with { Y = at.Y + 0.5f };
            foreach (var (a, b, c) in triangles)
            {
                var n = Vector3.Cross(b - a, c - a);
                if (n.LengthSquared() < 1e-12f)
                    continue;
                var ny = MathF.Abs(Vector3.Normalize(n).Y);
                if (ny < F.Walkable)
                    continue;
                if (F.Hit(from, -Vector3.UnitY, a, b, c) is { } down && MathF.Abs(from.Y - down - at.Y) < 0.01f && (best == null || ny > best))
                    best = ny;
            }
            return best;
        }
    }
}
