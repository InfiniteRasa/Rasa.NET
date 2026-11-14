using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.ClientData;
using Rasa.Missions.Content;
using Rasa.Services.Preloader.Missions.Ashen;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions.Ashen
{
    using F = Rasa.Test.Missions.Marshes.MarshesClientFloorTests;
    using X = AshenFixIV1;
    using Threshold = AshenThresholdV1;
    using Facility = AshenIndraFacilityTests;
    using P = Rasa.Test.Missions.Marshes.MarshesPreoDasTests;

    /// <summary>
    /// Ashen fix job I (AshenFixIV1, migration 20271110001520): to-fix row 34 (no set 15 place in Indra Caverns was reached in
    /// the real client). The survey's stops are the server navmesh's island ends (upstream N29), not walls our client collision
    /// misses; Bendal's body moves into the pillbox and 1360's facility door to the Security Check Point entrance's door place
    /// as the entrance's own door (class 23160).
    ///
    /// Why these tests: a survey stop our collision could not explain would mean a wall we miss, and every client-floor check
    /// a guess; a door that does not close the only way in, or one in the wrong place or turn, lets a player past the key
    /// card or draws a door in a wall; a Down that leaves the scene or the marker unlike set 15's leaves the database unlike
    /// a set 15 one.
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class AshenFixITests
    {
        private const string FixI = "20271110001520_Ashen_fix_i";

        private const string MapName = "adv_arieki_ligo_ashendesert_indracaverns";

        /// <summary>The pillbox's floor where the real client stood in it (to-fix row 34), and the Watch Post's way past it (set 15's leg).</summary>
        internal static readonly Vector3 PillboxFloor = new(11.5f, 18.6f, -342.7f), WatchPost = new(-5, 10.68f, -351);

        /// <summary>
        /// The real client's survey (to-fix row 34, 2026-11-11 23:15-23:35, the tester's place.py: a walk on the server navmesh's
        /// path, cwalk.py): the place, where the player stopped, and where that walk set out from for the check (the pillbox's
        /// floor; for Bendal, the Watch Post).
        /// </summary>
        internal static readonly (string Label, Vector3 Target, Vector3 Stop, Vector3 From)[] Survey =
        {
            ("Bendal (set 15's place)", new(5, 16, -347), new(11.1f, 18.5f, -343.3f), WatchPost),
            ("the facility door (set 15's)", new(82, 16.5f, -328), new(55.5f, 16.0f, -323.1f), PillboxFloor),
            ("Kuutk", new(59, -7.42f, -403), new(55.3f, 17.0f, -339.2f), PillboxFloor),
            ("Aksrah", new(153, 8.5f, -296), new(54.1f, 16.2f, -320.8f), PillboxFloor),
            ("the Science Lab door", new(36, 8.5f, -362), new(38.7f, 19.4f, -342.0f), PillboxFloor),
            ("the Supply Crate", new(24, 8.5f, -284), new(24.0f, 12.4f, -302.9f), PillboxFloor),
            ("Drof", new(-5, 0.58f, -221), new(-22.2f, 10.3f, -299.9f), PillboxFloor),
            ("Naimi", new(10, 24.6f, -244), new(12.1f, 10.2f, -302.4f), PillboxFloor)
        };

        [TestInitialize]
        public void Clock() => Rasa.Test.Missions.Plateau.PlateauGiantKillerSquadTests.UseClock();

        [TestCleanup]
        public void Restore() => Rasa.Test.Missions.Plateau.PlateauGiantKillerSquadTests.RestoreClock();

        /// <summary>
        /// The cause of row 34: every stop of the survey is where the server navmesh's walk to the place ends (a partial path,
        /// its end within 2 m across and 2 m up of the stop: the navmesh of 2055 is islands, N29), and the collision we read
        /// stops the straight way on from the stop to the place too (the wall or the climb the camera showed); and the client
        /// floor's walk from the Watch Post (set 15's legs from the arrival end there) into the pillbox on the navmesh's way, which
        /// the real client walked, passes. So no
        /// wall of the client's is missing from the collision we read at any of these places.
        /// </summary>
        [TestMethod]
        public void TheSurveysStopsAreTheNavmeshsIslandEnds()
        {
            var dataDir = F.ClientData();
            using var harness = WildernessRuntimeTestHarness.Create();
            var nav = Facility.IntoCaverns(harness).NavMesh;
            using var meshes = new MeshLibrary(dataDir, F.MeshCsv());
            var geometry = MapGeometry.Load(Path.Combine(dataDir, "maps", MapName), meshes, 1);
            var failures = new List<string>();
            foreach (var (label, target, stop, from) in Survey)
            {
                var path = nav.FindPath(nav.Nearest(from) ?? from, target, out var complete);
                if (path == null || complete)
                    failures.Add($"{label}: the navmesh's walk is {(path == null ? "none" : "complete")}");
                else if (Vector2.Distance(new(path[^1].X, path[^1].Z), new(stop.X, stop.Z)) > 2 || MathF.Abs(path[^1].Y - stop.Y) > 2)
                    failures.Add(FormattableString.Invariant($"{label}: the navmesh's walk ends at ({path[^1].X:0.0}, {path[^1].Y:0.0}, {path[^1].Z:0.0}), not at the stop"));
                if (AshenFixBTests.ClientFloorLine(geometry, new[] { stop, target }, target, null, AshenFormalComplaintsTests.PlayerHalfWidth) == null)
                    failures.Add($"{label}: the collision we read lets the straight way on from the stop through");
            }
            var navStart = nav.Nearest(WatchPost) ?? WatchPost;
            var navWay = nav.FindPath(navStart, PillboxFloor, out var joined);
            Assert.IsTrue(navWay != null && joined, "the navmesh joins the Watch Post to the pillbox");
            var into = navWay.Prepend(navStart).ToList();
            if (AshenFixBTests.ClientFloorLine(geometry, into, PillboxFloor, null, AshenFormalComplaintsTests.PlayerHalfWidth) is { } why)
                failures.Add($"into the pillbox on the navmesh's way, as the real client: {why}");
            Assert.IsEmpty(failures, $"{failures.Count}: " + string.Join("; ", failures));
        }

        /// <summary>
        /// Fix I's facility door: the client's map has the Security Check Point entrance (ArchBrannCorridorEntrance01V01, 9775)
        /// at the door's place and turn; the door's collision box (23160's, arch_brann_corridor_entrance_01_door_v01.geo)
        /// placed there stands across the entrance's 3.3 m tunnel (x 59.8-61.7, z -329.7..-326.3) on its floor. The walk in
        /// (the arrival to behind the facility door, set 15's legs) crosses its leaf; its last leg (the Watch Post to behind the
        /// door) passes on the client floor with the door open and is stopped with the door's box in (shut), and so is the way
        /// along the tunnel's middle. The
        /// door is not on the navmesh (the tunnel has no polygon past x 56: N29), so no navmesh check.
        /// </summary>
        [TestMethod]
        public void TheFacilityDoorClosesTheEntranceTunnel()
        {
            var dataDir = F.ClientData();
            using var meshes = new MeshLibrary(dataDir, F.MeshCsv());
            var path = Path.Combine(dataDir, "maps", MapName);
            var at = P.ToVector(X.DoorAt);
            var entrance = MapFile.Load(Path.Combine(path, MapName + ".map")).Entities.Where(entity => entity.ClassId == 9775 && Vector3.Distance(entity.Position, at) < 20).ToList();
            Assert.HasCount(1, entrance, "the Security Check Point's entrance");
            Assert.IsLessThan(0.01f, Vector3.Distance(at, entrance[0].Position), "the door at the entrance's origin");
            var turned = Quaternion.CreateFromYawPitchRoll((float)X.DoorOrientation, 0, 0);
            Assert.IsGreaterThan(0.9999f, MathF.Abs(Quaternion.Dot(turned, entrance[0].Rotation)), "turned as the entrance");
            Assert.IsTrue(meshes.TryGetMeshName((int)X.DoorClass, out var doorMesh) && doorMesh == "arch_brann_corridor_entrance_01_door_v01.geo", "the entrance's door mesh");
            Assert.IsTrue(meshes.TryGetMeshName(9775, out var entranceMesh) && entranceMesh == "arch_brann_corridor_entrance_01_v01.geo", "the entrance's mesh");

            var door = meshes.GetForClass((int)X.DoorClass);
            var box = door.Vertices.Select(v => Vector3.Transform(v, turned) + at).ToList();
            var (min, max) = (box.Aggregate(Vector3.Min), box.Aggregate(Vector3.Max));
            Assert.AreEqual((59.8f, 61.7f, -329.7f, -326.3f), (MathF.Round(min.X, 1), MathF.Round(max.X, 1), MathF.Round(min.Z, 1), MathF.Round(max.Z, 1)), "the leaf's box across the tunnel");

            var open = MapGeometry.Load(path, meshes, 1);
            var shut = MapGeometry.Load(path, meshes, 1);
            var first = shut.Vertices.Count / 3;
            foreach (var v in box)
            {
                shut.Vertices.Add(v.X);
                shut.Vertices.Add(v.Y);
                shut.Vertices.Add(v.Z);
            }
            foreach (var index in door.Triangles)
                shut.Triangles.Add(first + index);

            var middle = at + Vector3.Transform(new Vector3(0, 0, (X.LeafNear + X.LeafFar) / 2), turned);
            var near = F.Near(open, new List<Vector3> { at, middle }, 2);
            Assert.IsTrue(F.FloorAt(near[0], at, at.Y - 1, at.Y + 1) is { } floor && MathF.Abs(floor - at.Y) <= 1, "the door on the client floor");
            Assert.IsTrue(F.FloorAt(near[1], middle, at.Y - 1, at.Y + 1) is { } leafFloor && MathF.Abs(leafFloor - at.Y) <= 0.6f, "a floor under the leaf");

            var walk = Facility.Line(new[] { "the arrival to Diren", "Diren to the Watch Post", "the Watch Post to behind the facility door" });
            Assert.IsTrue(Facility.Crosses(walk, at, X.DoorOrientation, (X.LeafNear + X.LeafFar) / 2, X.LeafHalfWidth), "the walk in crosses the leaf");
            var leg = Facility.Legs["the Watch Post to behind the facility door"].ToList();
            Assert.IsNull(AshenFixBTests.ClientFloorLine(open, leg, leg[^1], null, AshenFormalComplaintsTests.PlayerHalfWidth), "the walk in, the door open");
            var stopped = AshenFixBTests.ClientFloorLine(shut, leg, leg[^1], null, AshenFormalComplaintsTests.PlayerHalfWidth);
            Assert.IsTrue(stopped?.StartsWith("something in the way", StringComparison.Ordinal) == true, $"the walk in, the door shut: {stopped}");
            var middleLine = new List<Vector3> { new(48, 16.05f, -328), new(52, 16.07f, -328), new(62, 16.5f, -328), new(70, 16.5f, -328) };
            Assert.IsNull(AshenFixBTests.ClientFloorLine(open, middleLine, middleLine[^1], null, AshenFormalComplaintsTests.PlayerHalfWidth), "the tunnel's middle, open");
            Assert.IsNotNull(AshenFixBTests.ClientFloorLine(shut, middleLine, middleLine[^1], null, AshenFormalComplaintsTests.PlayerHalfWidth), "the tunnel's middle, shut");
        }

        /// <summary>
        /// Bendal's body in the pillbox: on the pillbox's floor (ArchHumOutpostTrenchBunkerV01's, within 0.1 m), nothing of the
        /// placed meshes within 2 m of it across from knee to head height (the tester's 2 m off the wall), and the navmesh's
        /// walk to it from the Watch Post complete (the real client walked into the pillbox on that navmesh). The rest of its
        /// checks (navmesh ground, client floor, headroom, the 16-place ring, 34 m off every hostile pool, the walk from the
        /// arrival): AshenIndraFacilityTests.ThePlacesStandOnTheClientsFloor and ThePlacesAreWalkedToOnTheClientsFloor.
        /// </summary>
        [TestMethod]
        public void BendalLiesInThePillbox()
        {
            var dataDir = F.ClientData();
            using var harness = WildernessRuntimeTestHarness.Create();
            var nav = Facility.IntoCaverns(harness).NavMesh;
            using var meshes = new MeshLibrary(dataDir, F.MeshCsv());
            var geometry = MapGeometry.Load(Path.Combine(dataDir, "maps", MapName), meshes, 1);
            var at = P.ToVector(X.BendalAt);
            var near = F.Near(geometry, new List<Vector3> { at }, 3)[0];
            Assert.IsTrue(F.FloorAt(near, at, at.Y - 1, at.Y + 1) is { } floor && MathF.Abs(floor - at.Y) < 0.1f, "on the pillbox's floor");
            bool OnTerrain((Vector3 A, Vector3 B, Vector3 C) t) =>
                new[] { t.A, t.B, t.C }.All(v => geometry.Terrain.Height(v.X, v.Z) is { } h && MathF.Abs(h - v.Y) < 0.01f);
            var walls = near.Where(t => !OnTerrain(t)).ToList();
            for (var k = 0; k < 32; k++)
            {
                var angle = k * MathF.PI / 16;
                var out2 = new Vector3(MathF.Sin(angle) * 2, 0, MathF.Cos(angle) * 2);
                foreach (var height in new[] { AshenFixBTests.Knee, AshenFixBTests.Chest, AshenFixBTests.Head })
                    Assert.IsTrue(F.Clear(walls, at + new Vector3(0, height, 0), at + out2 + new Vector3(0, height, 0)), $"nothing within 2 m at {k * 11.25} degrees, {height} m up");
            }
            Assert.IsTrue(nav.FindPath(nav.Nearest(WatchPost) ?? WatchPost, at, out var complete) != null && complete, "the navmesh's walk from the Watch Post");
        }

        /// <summary>
        /// The rows: 1360's scene holds fix I's Bendal and door and is otherwise set 15's (the corpse's and the door's other
        /// fields, the sequences, items and objectives); marker 224 on the new place, radius 8; fix I's evidence row.
        /// </summary>
        [TestMethod]
        public void TheRowsAreFixIs()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            var stored = world.Set<Rasa.Structures.World.MissionSceneBindingEntry>().AsNoTracking()
                .Single(entry => entry.MissionId == X.Mission && entry.ContentRevision == AshenMissionDataV1.Revision).Bindings;
            Assert.AreEqual(X.SceneNow(), stored, "fix I's scene");
            var scene = JsonSerializer.Deserialize<MissionSceneDefinition>(stored, MissionContentCodec.Options);
            var (corpse, door) = (scene.Actors[Threshold.CorpseRole], scene.Actors[Threshold.DoorRole]);
            Assert.AreEqual((Threshold.CorpseClass, X.BendalAt), (corpse.TemplateId, corpse.Position), "Bendal");
            Assert.AreEqual((X.DoorClass, X.DoorAt, X.DoorOrientation, Threshold.CardTemplate, Threshold.DoorClosed),
                (door.TemplateId, door.Position, door.Orientation, door.KeyItemTemplateId, door.InitialObjectState), "the door");
            scene.Actors[Threshold.CorpseRole] = corpse with { Position = X.BendalBefore };
            scene.Actors[Threshold.DoorRole] = door with { TemplateId = X.DoorClassBefore, Position = X.DoorBefore, Orientation = X.DoorOrientationBefore };
            Assert.AreEqual(X.SceneBefore(), JsonSerializer.Serialize(scene, MissionContentCodec.Options), "set 15's but the two");

            var marker = world.MissionIndicatorEntries.AsNoTracking().Single(entry => entry.MissionId == X.Mission &&
                entry.ContentRevision == AshenMissionDataV1.Revision && entry.ObjectiveId == X.Objective);
            Assert.AreEqual((X.Marker, 8.0), (marker.IndicatorId, marker.Radius), "marker 224");
            Assert.IsLessThan(0.01f, Vector3.Distance(P.ToVector(X.BendalAt), new((float)marker.PosX, (float)marker.PosY, (float)marker.PosZ)), "on Bendal");
            Assert.AreEqual(1, world.MissionEvidenceEntries.AsNoTracking().Count(entry => entry.MissionId == X.Mission &&
                entry.ContentRevision == AshenMissionDataV1.Revision && entry.EvidenceId == X.EvidenceId && entry.SourceUri == X.Source), "the evidence row");
        }

        /// <summary>
        /// Fix I's migration rolled back and forth, baselined at its own migration: Down gives back set 15's scene, marker and
        /// evidence (as a database migrated only to before fix I holds them) and nothing else changes; a second Down and a
        /// second Up change nothing.
        /// </summary>
        [TestMethod]
        public void FixIRollsBackToSet15s()
        {
            using var harness = WildernessRuntimeTestHarness.Create(targetWorldMigration: FixI);   // baselined at its own migration
            var world = harness.World;
            int Count(string sql) => world.Database.SqlQueryRaw<int>(sql).AsEnumerable().Single();
            string Scene() => world.Set<Rasa.Structures.World.MissionSceneBindingEntry>().AsNoTracking()
                .Single(entry => entry.MissionId == X.Mission && entry.ContentRevision == AshenMissionDataV1.Revision).Bindings;
            string Marker() => world.MissionIndicatorEntries.AsNoTracking().Where(entry => entry.MissionId == X.Mission &&
                    entry.ContentRevision == AshenMissionDataV1.Revision && entry.ObjectiveId == X.Objective && entry.IndicatorId == X.Marker).AsEnumerable()
                .Select(entry => FormattableString.Invariant($"{entry.PosX:R},{entry.PosY:R},{entry.PosZ:R},{entry.Radius:R}")).Single();
            string Mine() => string.Join(", ", Scene() == X.SceneNow() ? "scene fix I" : Scene() == X.SceneBefore() ? "scene set 15" : "scene other",
                $"marker {Marker()}",
                $"evidence {Count($"SELECT COUNT(*) AS Value FROM mission_evidence WHERE mission_id = {X.Mission} AND content_revision = '{AshenMissionDataV1.Revision}' AND evidence_id = {X.EvidenceId}")}");
            string Others() => string.Join("; ", world.Set<Rasa.Structures.World.MissionSceneBindingEntry>().AsNoTracking()
                    .Where(entry => !(entry.MissionId == X.Mission && entry.ContentRevision == AshenMissionDataV1.Revision))
                    .OrderBy(entry => entry.MissionId).ThenBy(entry => entry.ContentRevision).AsEnumerable()
                    .Select(entry => $"{entry.MissionId}/{entry.ContentRevision}:{entry.Bindings.Length}:{entry.Bindings.GetHashCode()}"))
                + " | " + string.Join("; ", world.MissionIndicatorEntries.AsNoTracking().Where(entry => !(entry.MissionId == X.Mission &&
                        entry.ContentRevision == AshenMissionDataV1.Revision && entry.ObjectiveId == X.Objective && entry.IndicatorId == X.Marker))
                    .OrderBy(entry => entry.MissionId).ThenBy(entry => entry.ContentRevision).ThenBy(entry => entry.ObjectiveId).ThenBy(entry => entry.IndicatorId).AsEnumerable()
                    .Select(entry => FormattableString.Invariant($"{entry.MissionId}.{entry.ObjectiveId}/{entry.IndicatorId}:{entry.PosX:R},{entry.PosY:R},{entry.PosZ:R},{entry.Radius:R}")))
                + " | " + string.Join("; ", world.MissionEvidenceEntries.AsNoTracking().Where(entry => entry.SourceUri != X.Source)
                    .OrderBy(entry => entry.MissionId).ThenBy(entry => entry.ContentRevision).ThenBy(entry => entry.EvidenceId).AsEnumerable()
                    .Select(entry => $"{entry.MissionId}/{entry.ContentRevision}/{entry.EvidenceId}"));
            var at = P.ToVector(X.BendalAt);
            var all = $"scene fix I, marker {FormattableString.Invariant($"{(double)at.X:R},{(double)at.Y:R},{(double)at.Z:R}")},8, evidence 1";
            const string None = "scene set 15, marker 5,16,-347,8, evidence 0";
            var others = Others();
            Assert.AreEqual(all, Mine(), "at its migration");

            var before = AshenMissionTests.Previous(world, FixI);
            world.GetService<IMigrator>().Migrate(before);
            world.ChangeTracker.Clear();
            Assert.AreEqual(None, Mine(), "Down");
            Assert.AreEqual(others, Others(), "Down: nothing else changes");
            using (var fresh = WildernessRuntimeTestHarness.Create(targetWorldMigration: before))
            {
                Assert.AreEqual(X.SceneBefore(), fresh.World.Set<Rasa.Structures.World.MissionSceneBindingEntry>().AsNoTracking()
                    .Single(entry => entry.MissionId == X.Mission && entry.ContentRevision == AshenMissionDataV1.Revision).Bindings, "the scene as a database migrated only to before fix I holds it");
                Assert.AreEqual("5,16,-347,8", fresh.World.MissionIndicatorEntries.AsNoTracking().Where(entry => entry.MissionId == X.Mission &&
                        entry.ContentRevision == AshenMissionDataV1.Revision && entry.ObjectiveId == X.Objective && entry.IndicatorId == X.Marker).AsEnumerable()
                    .Select(entry => FormattableString.Invariant($"{entry.PosX:R},{entry.PosY:R},{entry.PosZ:R},{entry.Radius:R}")).Single(), "the marker likewise");
            }
            void Run(string sql) => world.Database.ExecuteSqlRaw(sql.Replace("{", "{{").Replace("}", "}}"));
            foreach (var sql in X.DownSql())
                Run(sql);
            world.ChangeTracker.Clear();
            Assert.AreEqual(None, Mine(), "a second Down changes nothing");

            world.GetService<IMigrator>().Migrate(FixI);
            world.ChangeTracker.Clear();
            Assert.AreEqual(all, Mine(), "Up again");
            foreach (var sql in X.UpSql())
                Run(sql);
            world.ChangeTracker.Clear();
            Assert.AreEqual(all, Mine(), "a second Up changes nothing");
            Assert.AreEqual(others, Others(), "nothing else changes");
        }
    }
}
