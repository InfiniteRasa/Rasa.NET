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
using Rasa.Managers;
using Rasa.Services.Preloader.Missions.Ashen;
using Rasa.Structures;
using Rasa.Test.Missions.Marshes;
using Rasa.Test.Missions.Plateau;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions.Ashen
{
    using F = MarshesClientFloorTests;
    using A = AshenFixAV1;

    /// <summary>
    /// Ashen fix job A (to-fix row 1): Fithik Infestation's (1274) Stonewall Ridge pool 540067 is a point pool on the open east
    /// slope, and 1274's marker on it.
    ///
    /// Why these tests: in the real client its respawned Fithik stood on the ground under a rock shelf, 6 m under the player
    /// on it, and could not be shot; its 15 m radius (Detour's random points) took in that ground, and the ones put there,
    /// never killed, filled the pool. A Fithik set down or come back off the open ground, out of reach of the waypoints,
    /// outside the marker, or a stroll that leads under a surface, would block 1274 again; a Down that does not put upstream's
    /// pool and set 1's marker back, or an Up that moves another row, would break the rollback.
    ///
    /// The map: Ligo Ashen Desert (1734), loaded as AshenLeadInsTests loads it. The client floor checks read the client's files
    /// (as MarshesClientFloorTests; inconclusive without them).
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class AshenFixATests
    {
        private const string FixA = "20271110000300_Ashen_fix_a";

        /// <summary>Kill-and-respawn rounds: every other one kills all of the pool (a full spawn), the others one (a top-up).</summary>
        private const int Rounds = 40;

        private static Vector3 V((double X, double Y, double Z) at) => new((float)at.X, (float)at.Y, (float)at.Z);

        /// <summary>
        /// The pool as the server loads it: a point (radius 0) at its new place, upstream's creature, counts and respawn time;
        /// the marker (r30) on it; its point on the navmesh, walked to from the Stonewall Ridge waypoint.
        /// </summary>
        [TestMethod]
        public void ThePoolIsAPointOnTheOpenSlopeWithTheMarkerOnIt()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var row = harness.World.SpawnPoolEntries.AsNoTracking().Single(entry => entry.Id == A.Pool);
            Assert.AreEqual((A.Fithik, (byte)2, (byte)4, 0U, 1200U, 0, AshenMissionDataV1.MapContextId),
                (row.Creature1Id, row.Creature1MinCount, row.Creature1MaxCount, row.Creature2Id, row.RespawnTime, (int)row.Mode, row.MapContextId), "upstream's pool");
            Assert.AreEqual(A.RadiusNow, row.Radius, 0.001, "a point");
            Assert.IsLessThan(0.01, Vector3.Distance(V(A.PoolNow), V((row.PosX, row.PosY, row.PosZ))), $"at {A.PoolNow}");

            var markers = harness.World.MissionIndicatorEntries.AsNoTracking()
                .Where(entry => entry.MissionId == A.MissionId && entry.ContentRevision == AshenMissionDataV1.Revision).ToList();
            Assert.HasCount(1, markers, "1274's one marker");
            Assert.AreEqual((A.Marker.Objective, A.Marker.Indicator, AshenFithikInfestationV1.MarkerRadius), (markers[0].ObjectiveId, markers[0].IndicatorId, markers[0].Radius));
            Assert.IsLessThan(0.01, Vector3.Distance(V(A.MarkerNow), V((markers[0].PosX, markers[0].PosY, markers[0].PosZ))), "the marker on the point");

            var desert = AshenLeadInsTests.EnterAshen(harness, AshenLeadInsTests.StonewallWaypoint);
            var point = V(A.PoolNow);
            var ground = desert.NavMesh.GroundHeight(point);
            Assert.IsNotNull(ground, "navmesh at the point");
            Assert.AreEqual(A.MarkerNow.Y, ground.Value, 0.05, "the marker at the navmesh ground");
            Assert.IsTrue(desert.NavMesh.FindPath(desert.NavMesh.Nearest(AshenLeadInsTests.StonewallWaypoint).Value, point with { Y = ground.Value },
                out var complete) != null && complete, "the point walked to from the Stonewall Ridge waypoint");
        }

        /// <summary>
        /// The play's case: the pool's Fithik, set down, killed and come back over <see cref="Rounds"/> rounds (full spawns and
        /// top-ups): each on the navmesh ground, on the point or its rings (within PointSpread), walked to from the Stonewall
        /// Ridge waypoint (a complete path), inside the marker.
        /// </summary>
        [TestMethod]
        public void ItsFithikComeDownOnThePointAndItsRingsThroughRespawns()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var seen = SpawnKillRespawn(harness, out var desert);
            var from = desert.NavMesh.Nearest(AshenLeadInsTests.StonewallWaypoint).Value;
            var point = new Vector2((float)A.PoolNow.X, (float)A.PoolNow.Z);
            var marker = new Vector2((float)A.MarkerNow.X, (float)A.MarkerNow.Z);
            Assert.IsGreaterThan(Rounds * 2, seen.Count, "Fithik seen");
            foreach (var at in seen.Distinct())
            {
                MarshesPaludosTests.AssertOnGround(desert, at, $"a Fithik at {at}");
                Assert.IsLessThanOrEqualTo((float)A.PointSpread + 0.5f, Vector2.Distance(point, new(at.X, at.Z)), $"{at} on the point or its rings");
                Assert.IsLessThanOrEqualTo((float)AshenFithikInfestationV1.MarkerRadius, Vector2.Distance(marker, new(at.X, at.Z)), $"{at} inside the marker");
                Assert.IsTrue(desert.NavMesh.FindPath(from, at, out var complete) != null && complete, $"{at} walked to from the Stonewall Ridge waypoint");
            }
        }

        /// <summary>
        /// The ground a Fithik of the pool can come down on or stroll to: every navmesh floor (a 1 m grid, all floors from 8 m
        /// under to 12 m over the point's height) within ClearRadius of the point is joined to the Stonewall Ridge waypoint's
        /// ground, and each spot has one floor only: no floor over another, as the shelf over upstream's ground (this without
        /// the client's files; ItsFithikAndTheirGroundStandOnAClientFloorUnderOpenSky checks the client's surfaces). The same
        /// check round set 1's marker point, upstream's spawn ground, finds the shelf.
        /// </summary>
        [TestMethod]
        public void TheGroundRoundThePointIsJoinedToTheWaypoints()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var desert = AshenLeadInsTests.EnterAshen(harness, AshenLeadInsTests.StonewallWaypoint);
            var region = desert.NavMesh.RegionArea(desert.NavMesh.Nearest(AshenLeadInsTests.StonewallWaypoint).Value);
            Assert.IsNotNull(region);
            var columns = Columns(desert, V(A.PoolNow));
            Assert.IsGreaterThan(500, columns.Count, "the floors round the point are read");
            var off = columns.SelectMany(column => column).Where(at => desert.NavMesh.RegionArea(at) != region).ToList();
            Assert.IsEmpty(off, "not joined: " + string.Join("; ", off.Take(10)));
            var stacked = columns.Where(column => column.Count != 1).ToList();
            Assert.IsEmpty(stacked, "more than one floor: " + string.Join("; ", stacked.Take(10).Select(column => string.Join(" / ", column))));

            var upstream = Columns(desert, V(A.MarkerBefore));
            Assert.IsGreaterThan(50, upstream.Count(column => column.Count > 1), "round upstream's spawn ground the shelf stands over the ground");
        }

        /// <summary>
        /// The play's cause, with the client's collision: every Fithik of the rounds stands within 1 m of a client floor with no
        /// client surface over it (40 m up), and so does every navmesh floor within ClearRadius of the point (where a stroll
        /// can take one; up to 1.5 m over its floor). The upstream pool's draws did not: 15 % stood under the shelf, 3.5-7 m under a surface.
        /// </summary>
        [TestMethod]
        public void ItsFithikAndTheirGroundStandOnAClientFloorUnderOpenSky()
        {
            var dataDir = F.ClientData();
            using var harness = WildernessRuntimeTestHarness.Create();
            var seen = SpawnKillRespawn(harness, out var desert).Distinct().ToList();
            var places = seen.Concat(Columns(desert, V(A.PoolNow)).SelectMany(column => column)).ToList();
            var mapName = harness.World.MapInfoEntries.AsNoTracking().Single(map => map.Id == AshenMissionDataV1.MapContextId).MapName;
            using var meshes = new MeshLibrary(dataDir, F.MeshCsv());
            var geometry = MapGeometry.Load(Path.Combine(dataDir, "maps", mapName), meshes, 1);
            var near = F.Near(geometry, places, 0.5f);
            var failures = new List<string>();
            for (var i = 0; i < places.Count; i++)
            {
                var at = places[i];
                var label = i < seen.Count ? "Fithik" : "floor";
                var floor = F.FloorAt(near[i], at, at.Y - 2, at.Y + 1.5f);
                // A Fithik within 1 m of its floor; a navmesh floor up to 1.5 m over it (the mesh rides up over a stone, 0.9 m
                // steps) and not more than 1 m under it.
                if (floor is not { } y || y - at.Y > 1 || at.Y - y > (i < seen.Count ? 1 : 1.5f))
                    failures.Add(FormattableString.Invariant($"{label} at ({at.X:0.0}, {at.Y:0.00}, {at.Z:0.0}): client floor {floor:0.00}"));
                if (near[i].Any(t => F.Hit(at + new Vector3(0, 1, 0), Vector3.UnitY, t.A, t.B, t.C) is { } up && up < 40))
                    failures.Add(FormattableString.Invariant($"{label} at ({at.X:0.0}, {at.Y:0.00}, {at.Z:0.0}): a client surface over it"));
            }
            Assert.IsEmpty(failures, $"{failures.Count}: " + string.Join("; ", failures.Take(10)));
        }

        /// <summary>
        /// Ashen_fix_a rolls back: Down puts upstream's pool (radius 15, its point) and set 1's marker back and takes out its
        /// evidence row, and nothing else of the rows it writes changes; Up again, the fix is back.
        /// </summary>
        [TestMethod]
        public void FixARollsBackWithItsMigration()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            ((double X, double Y, double Z) At, double Radius) Pool()
            {
                var row = world.SpawnPoolEntries.AsNoTracking().Single(entry => entry.Id == A.Pool);
                return ((row.PosX, row.PosY, row.PosZ), row.Radius);
            }
            (double X, double Y, double Z) Marker()
            {
                var row = world.MissionIndicatorEntries.AsNoTracking().Single(entry => entry.MissionId == A.MissionId &&
                    entry.ContentRevision == AshenMissionDataV1.Revision && entry.ObjectiveId == A.Marker.Objective && entry.IndicatorId == A.Marker.Indicator);
                return (row.PosX, row.PosY, row.PosZ);
            }
            bool Evidence() => world.MissionEvidenceEntries.AsNoTracking().Any(entry => entry.MissionId == A.MissionId &&
                entry.ContentRevision == AshenMissionDataV1.Revision && entry.EvidenceId == A.Evidence);
            void Is((double X, double Y, double Z) expected, (double X, double Y, double Z) actual, string what) =>
                Assert.IsLessThan(0.01, Vector3.Distance(V(expected), V(actual)), $"{what}: {expected}, is {actual}");
            void Fixed()
            {
                var (at, radius) = Pool();
                Is(A.PoolNow, at, "the pool's point");
                Assert.AreEqual(A.RadiusNow, radius, 0.001);
                Is(A.MarkerNow, Marker(), "the marker");
                Assert.IsTrue(Evidence(), "fix A's evidence");
            }
            var otherPools = world.SpawnPoolEntries.AsNoTracking().Where(entry => entry.MapContextId == AshenMissionDataV1.MapContextId && entry.Id != A.Pool)
                .Select(entry => new { entry.Id, entry.PosX, entry.PosY, entry.PosZ, entry.Radius }).OrderBy(entry => entry.Id).ToList();

            Fixed();
            world.GetService<IMigrator>().Migrate(AshenMissionTests.Previous(world, FixA));
            world.ChangeTracker.Clear();
            var (before, beforeRadius) = Pool();
            Is(A.PoolBefore, before, "upstream's point");
            Assert.AreEqual(A.RadiusBefore, beforeRadius, 0.001, "upstream's radius");
            Is(A.MarkerBefore, Marker(), "set 1's marker");
            Assert.IsFalse(Evidence(), "no fix A evidence");
            Assert.IsTrue(world.MissionContentDefinitionEntries.Any(entry => entry.MissionId == A.MissionId && entry.ContentRevision == AshenMissionDataV1.Revision && entry.Enabled),
                "1274 stays (set 1)");

            // Up again to the last migration: the Down to fix A's predecessor took the later sets' pools out too (set 4's).
            world.GetService<IMigrator>().Migrate();
            world.ChangeTracker.Clear();
            Fixed();
            CollectionAssert.AreEqual(otherPools.Select(entry => entry.ToString()).ToList(),
                world.SpawnPoolEntries.AsNoTracking().Where(entry => entry.MapContextId == AshenMissionDataV1.MapContextId && entry.Id != A.Pool)
                    .Select(entry => new { entry.Id, entry.PosX, entry.PosY, entry.PosZ, entry.Radius }).OrderBy(entry => entry.Id).ToList()
                    .Select(entry => entry.ToString()).ToList(), "no other Ashen pool moves");
        }

        /// <summary>The pool spawned, then <see cref="Rounds"/> rounds of kills and respawns: where each of its Fithik stood, every round.</summary>
        private static List<Vector3> SpawnKillRespawn(WildernessRuntimeTestHarness harness, out MapChannel desert)
        {
            desert = AshenLeadInsTests.EnterAshen(harness, AshenLeadInsTests.StonewallWaypoint);
            var map = desert;
            PlateauCampResistanceTests.Spawn(desert, A.Pool);
            var seen = new List<Vector3>();
            int? toppedUp = null;
            for (var round = 0; round < Rounds; round++)
            {
                var fithik = MarshesPaludosTests.Alive(map).Where(creature => creature.SpawnPool?.DbId == A.Pool).ToList();
                Assert.IsTrue(fithik.Count is >= 2 and <= 4, $"round {round}: 2 to 4 Fithik, {fithik.Count}");
                // After a round that killed one, the pool topped up: as many as before the kill.
                if (toppedUp is { } before)
                    Assert.AreEqual(before, fithik.Count, $"round {round}: the one killed came back (TopUp)");
                toppedUp = round % 2 == 0 ? null : fithik.Count;
                seen.AddRange(fithik.Select(creature => creature.Position));
                foreach (var creature in fithik.Take(round % 2 == 0 ? fithik.Count : 1))
                {
                    harness.MoveTo(creature.Position);
                    creature.Attributes[Attributes.Health].Current = 0;
                    harness.Creatures.HandleCreatureKill(map, creature, harness.Client.Player);
                }
                MarshesPaludosTests.Respawn(map, A.Pool);
            }
            return seen;
        }

        /// <summary>
        /// The navmesh floors on a 1 m grid within ClearRadius of <paramref name="centre"/>, from 8 m under to 12 m over its height:
        /// one list per spot, its floors from the lowest up (heights less than 2 m apart, an agent's height, are one floor).
        /// </summary>
        private static List<List<Vector3>> Columns(MapChannel desert, Vector3 centre)
        {
            var columns = new List<List<Vector3>>();
            var reach = (int)Math.Ceiling(A.ClearRadius);
            for (var dz = -reach; dz <= reach; dz++)
                for (var dx = -reach; dx <= reach; dx++)
                {
                    if (dx * dx + dz * dz > A.ClearRadius * A.ClearRadius)
                        continue;
                    var heights = new SortedSet<float>();
                    for (var h = -8; h <= 12; h++)
                    {
                        var probe = new Vector3(centre.X + dx, centre.Y + h, centre.Z + dz);
                        if (desert.NavMesh.Nearest(probe) is { } nearest && MathF.Abs(nearest.X - probe.X) < 0.3f && MathF.Abs(nearest.Z - probe.Z) < 0.3f &&
                            desert.NavMesh.GroundHeight(probe) is { } y)
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
            return columns;
        }
    }
}
