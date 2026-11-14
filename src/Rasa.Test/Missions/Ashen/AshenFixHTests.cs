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
using Rasa.Structures.World;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions.Ashen
{
    using F = Rasa.Test.Missions.Marshes.MarshesClientFloorTests;
    using WO = AshenWhiteOasisTests;
    using X = AshenFixHV1;

    /// <summary>
    /// Ashen fix job H (AshenFixHV1, migration 20271110001510): to-fix row 33 (no player reaches the Indra Caverns door, map
    /// link 58: its island has no walk-in, upstream N23/N26). A local teleporter pair takes the player from the Indra Pass valley
    /// (by Surveyor Odesha) onto the island, from where the door is walked to (AshenIndraFacilityTests.TheCavernsDoorIsWalkedToFromTheUpperPad).
    ///
    /// Why these tests: a pad a player cannot walk to, one that stands in a hostile pool's reach, one whose arrival sends the
    /// player straight back, a missing link, or a Down that leaves a row behind would each leave set 15 unstartable or the
    /// database unlike a set 15 one.
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class AshenFixHTests
    {
        private const string FixH = "20271110001510_Ashen_fix_h";

        /// <summary>How far outside every hostile pool's circle the pads stand (2D; 30 m walk clearance and a margin, as the sets' places).</summary>
        private const float PlaceClearance = 34;

        /// <summary>How far outside link 58's circle the upper pad stands (a player stepping off it does not walk into the door).</summary>
        private const float LinkClearance = 8;

        internal static readonly Vector3 LowerPad = V(X.LowerPadAt), UpperPad = V(X.UpperPadAt);

        /// <summary>The walk from the valley's south entry (the end of set 10's and fix G's way from the White Oasis Post waypoint) to the lower pad, its corners (probe 2026-11-11, ESTIMATE of a player's line).</summary>
        internal static readonly Vector3[] ToLowerPad = { AshenIndraPassTests.Arrival, LowerPad };

        /// <summary>From the upper pad to the Indra Pass waypoint (198), its corners.</summary>
        internal static readonly Vector3[] ToWaypoint = { UpperPad, AshenIndraPassTests.IndraWaypoint };

        [TestInitialize]
        public void Clock() => Rasa.Test.Missions.Plateau.PlateauGiantKillerSquadTests.UseClock();

        [TestCleanup]
        public void Restore() => Rasa.Test.Missions.Plateau.PlateauGiantKillerSquadTests.RestoreClock();

        /// <summary>
        /// The rows: two local teleporters (type 1, class 29648 ArchHumTeleporterV02) on Ashen Desert at the pads' places, linked
        /// both ways and to nothing else; 1559's objective 1 has its door marker 261 and the unnamed marker on the lower pad. Each
        /// pad's arrival (the far pad's place, a metre up: DynamicObjectManager.LocalTeleporterWorker) stands outside the other
        /// pad's 2 m trigger (upstream D23: an arrival on the return trigger).
        /// </summary>
        [TestMethod]
        public void ThePadsAreAPairLinkedBothWays()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            var rows = world.TeleporterEntries.AsNoTracking().Where(row => row.Id == X.LowerPad || row.Id == X.UpperPad).OrderBy(row => row.Id).ToList();
            Assert.HasCount(2, rows);
            foreach (var (row, at, description) in rows.Zip(new[] { (LowerPad, X.LowerDescription), (UpperPad, X.UpperDescription) }, (row, pad) => (row, pad.Item1, pad.Item2)))
            {
                Assert.AreEqual((AshenMissionDataV1.MapContextId, X.PadClass, X.LocalTeleporter, description, 0.0),
                    (row.MapContextId, row.ClassId, (uint)row.Type, row.Description, row.Rotation), $"{row.Id}");
                Assert.IsLessThan(0.01f, Vector3.Distance(at, new((float)row.PosX, (float)row.PosY, (float)row.PosZ)), $"{row.Id} place");
            }
            int Links(string where) => world.Database.SqlQueryRaw<int>($"SELECT COUNT(*) AS Value FROM local_teleporter_link WHERE {where}").AsEnumerable().Single();
            Assert.AreEqual(1, Links($"from_teleporter_id = {X.LowerPad} AND to_teleporter_id = {X.UpperPad}"), "lower to upper");
            Assert.AreEqual(1, Links($"from_teleporter_id = {X.UpperPad} AND to_teleporter_id = {X.LowerPad}"), "upper to lower");
            Assert.AreEqual(2, Links($"from_teleporter_id IN ({X.LowerPad}, {X.UpperPad}) OR to_teleporter_id IN ({X.LowerPad}, {X.UpperPad})"), "no other link");

            var markers = world.MissionIndicatorEntries.AsNoTracking().Where(entry => entry.MissionId == X.Mission &&
                entry.ContentRevision == AshenMissionDataV1.Revision && entry.ObjectiveId == X.Objective).OrderBy(entry => entry.IndicatorId).ToList();
            Assert.HasCount(2, markers, "1559.1: the door's and the lower pad's");
            Assert.AreEqual(AshenIndraCavernsV1.DoorIndicator, markers[0].IndicatorId, "the door's, kept");
            Assert.AreEqual((MissionIndicator.UnnamedFrom + X.Marker, X.MarkerRadius), (markers[1].IndicatorId, markers[1].Radius), "the pad's, unnamed");
            Assert.IsLessThan(0.01f, Vector3.Distance(LowerPad, new((float)markers[1].PosX, (float)markers[1].PosY, (float)markers[1].PosZ)), "on the lower pad");

            var up = new Vector3(0, 1, 0);
            Assert.IsGreaterThan(2f, Vector3.Distance(UpperPad + up, LowerPad), "the arrival on the upper pad is off the lower pad's trigger");
            Assert.IsGreaterThan(2f, Vector3.Distance(LowerPad + up, UpperPad), "the arrival on the lower pad is off the upper pad's trigger");
        }

        /// <summary>
        /// A player who steps onto the lower pad is held and set down on the upper one, not sent straight back; off the pad (the
        /// door walk's first corner) and on again, he is taken back down to the valley.
        /// </summary>
        [TestMethod]
        public void TheValleyPadTakesThePlayerUpAndBack()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var desert = AshenLeadInsTests.EnterAshen(harness, AshenIndraPassTests.Arrival);
            Pads(harness, desert);
            Assert.IsTrue(desert.Teleporters.TryGetValue(X.LowerPad, out var lower), "the valley pad");
            Assert.IsTrue(desert.Teleporters.TryGetValue(X.UpperPad, out var upper), "the island pad");

            Teleport(harness, desert, lower);
            Assert.IsLessThan(3f, Vector3.Distance(UpperPad, harness.Client.Player.Position), "on the island");
            harness.Objects.DynamicObjectProximityWorker(desert, upper, 0);
            Assert.IsNull(harness.Client.PendingTransfer, "not sent straight back");

            harness.MoveTo(AshenIndraFacilityTests.DoorWalk[1]);
            harness.Drain();
            harness.Objects.DynamicObjectProximityWorker(desert, upper, 0);
            Assert.IsNull(harness.Client.PendingTransfer, "off the pad");
            Teleport(harness, desert, upper);
            Assert.IsLessThan(3f, Vector3.Distance(LowerPad, harness.Client.Player.Position), "back in the valley");
            harness.Objects.DynamicObjectProximityWorker(desert, lower, 0);
            Assert.IsNull(harness.Client.PendingTransfer, "not sent straight back up");
        }

        /// <summary>
        /// The pads stand on the client's floor (within 0.3 m of the highest floor under them, nothing within 3 m over them),
        /// PlaceClearance outside every hostile Ashen Desert pool's circle; the upper pad LinkClearance outside link 58's circle.
        /// The lower pad is walked to from the valley's south entry, there and back, on the client's floor
        /// (AshenFixBTests.ClientFloorLine with a player's half width) and HostileClearance (30 m, 2D) outside every hostile pool's
        /// circle. From the upper pad the Indra Pass waypoint (198) is walked to the same way (it is gained by a player there).
        /// </summary>
        [TestMethod]
        public void ThePadsStandOnTheClientsFloorAndAreWalkedTo()
        {
            var dataDir = F.ClientData();
            using var harness = WildernessRuntimeTestHarness.Create();
            using var meshes = new MeshLibrary(dataDir, F.MeshCsv());
            var geometry = MapGeometry.Load(Path.Combine(dataDir, "maps", harness.World.MapInfoEntries.AsNoTracking()
                .Single(map => map.Id == AshenMissionDataV1.MapContextId).MapName), meshes, 1);
            var hostile = WO.HostilePools(harness);
            Assert.IsGreaterThan(50, hostile.Count, "the Ashen hostile pools are read");
            var failures = new List<string>();
            var near = F.Near(geometry, new List<Vector3> { LowerPad, UpperPad }, 2);
            foreach (var (label, at, triangles) in new[] { ("the lower pad", LowerPad, near[0]), ("the upper pad", UpperPad, near[1]) })
            {
                var floor = F.FloorAt(triangles, at, at.Y - 3, at.Y + 1);
                if (floor is not { } y || MathF.Abs(y - at.Y) > 0.3f)
                    failures.Add($"{label}: the client's floor at {floor}");
                if (!F.Clear(triangles, at + new Vector3(0, 0.3f, 0), at + new Vector3(0, 3, 0)))
                    failures.Add($"{label}: something over it");
                var gap = hostile.Min(pool => Vector2.Distance(new(at.X, at.Z), new((float)pool.PosX, (float)pool.PosZ)) - (float)pool.Radius);
                if (gap < PlaceClearance)
                    failures.Add(FormattableString.Invariant($"{label}: {gap:0.0} m outside a hostile pool's circle"));
            }
            var link = harness.World.Set<MapLinkEntry>().AsNoTracking().Single(entry => entry.MapContextId == AshenMissionDataV1.MapContextId && entry.DestMapContextId == AshenIndraFacilityNpcsV1.Caverns);
            Assert.IsGreaterThan(LinkClearance, Vector2.Distance(new(UpperPad.X, UpperPad.Z), new((float)link.PosX, (float)link.PosZ)) - (float)link.Radius, "the upper pad off the door's circle");
            foreach (var (label, there) in new[] { ("the lower pad from the valley's south entry", ToLowerPad.ToList()), ("the Indra Pass waypoint from the upper pad", ToWaypoint.ToList()) })
            {
                var back = Enumerable.Reverse(there).ToList();
                foreach (var (line, direction) in new[] { (there, "there"), (back, "back") })
                    if (AshenFixBTests.ClientFloorLine(geometry, line, line[^1], null, AshenFormalComplaintsTests.PlayerHalfWidth) is { } why)
                        failures.Add($"{label} {direction}: {why}");
                if (WO.Clearance(there, hostile) is { } close)
                    failures.Add(FormattableString.Invariant($"{label}: {close.Gap:0.0} m outside hostile pool {close.Pool}'s circle at ({close.At.X:0.0}, {close.At.Y:0.0})"));
            }
            Assert.IsEmpty(failures, $"{failures.Count}: " + string.Join("; ", failures));
        }

        /// <summary>
        /// The lower pad on the server's navmesh: joined to the White Oasis Post waypoint (set 10's and fix G's way into the
        /// valley), as Odesha is. The upper pad's island is joined to neither (upstream N23): when it is, the pads may go.
        /// </summary>
        [TestMethod]
        public void TheLowerPadIsOnTheWayFromWhiteOasisAndTheIslandIsNot()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var desert = AshenLeadInsTests.EnterAshen(harness, AshenIndraPassTests.Arrival);
            var nav = desert.NavMesh;
            var start = nav.Nearest(AshenLeadInsTests.WhiteOasisWaypoint) ?? AshenLeadInsTests.WhiteOasisWaypoint;
            Assert.IsTrue(nav.FindPath(start, nav.Nearest(LowerPad) ?? LowerPad, out var lower) != null && lower, "the lower pad from White Oasis Post");
            Assert.IsFalse(nav.FindPath(start, nav.Nearest(UpperPad) ?? UpperPad, out var upper) != null && upper, "the island is joined now (upstream N23 fixed?)");
        }

        /// <summary>
        /// Ashen_fix_h rolls back: Down takes out the two teleporters, their two links, 1559.1's pad marker and fix H's evidence
        /// row, and nothing else; Up again, they are back; a second Up changes nothing (the guards).
        /// </summary>
        [TestMethod]
        public void FixHRollsBackWithItsMigration()
        {
            // At its migration (set 16's rows are not this one's "nothing else").
            using var harness = WildernessRuntimeTestHarness.Create(targetWorldMigration: FixH);
            var world = harness.World;
            int Count(string sql) => world.Database.SqlQueryRaw<int>(sql).AsEnumerable().Single();
            string Mine() => string.Join(", ",
                $"teleporter {Count($"SELECT COUNT(*) AS Value FROM teleporter WHERE id IN ({X.LowerPad}, {X.UpperPad})")}",
                $"link {Count($"SELECT COUNT(*) AS Value FROM local_teleporter_link WHERE from_teleporter_id IN ({X.LowerPad}, {X.UpperPad})")}",
                $"marker {Count($"SELECT COUNT(*) AS Value FROM mission_indicator WHERE mission_id = {X.Mission} AND content_revision = '{AshenMissionDataV1.Revision}' AND indicator_id = {MissionIndicator.UnnamedFrom + X.Marker}")}",
                $"evidence {Count($"SELECT COUNT(*) AS Value FROM mission_evidence WHERE mission_id = {X.Mission} AND content_revision = '{AshenMissionDataV1.Revision}' AND evidence_id = {X.EvidenceId}")}");
            string Others() => string.Join("; ", world.TeleporterEntries.AsNoTracking().Where(row => row.Id != X.LowerPad && row.Id != X.UpperPad).OrderBy(row => row.Id).AsEnumerable()
                    .Select(row => FormattableString.Invariant($"{row.Id}:{row.ClassId},{row.Type},{row.Description},{row.PosX:R},{row.PosY:R},{row.PosZ:R},{row.Rotation:R},{row.MapContextId}")))
                + " | " + Count($"SELECT COUNT(*) AS Value FROM local_teleporter_link WHERE from_teleporter_id NOT IN ({X.LowerPad}, {X.UpperPad})") + ", "
                + Count($"SELECT COALESCE(SUM(from_teleporter_id * 1000 + to_teleporter_id), 0) AS Value FROM local_teleporter_link WHERE from_teleporter_id NOT IN ({X.LowerPad}, {X.UpperPad})")
                + " | " + string.Join("; ", world.MissionIndicatorEntries.AsNoTracking().Where(entry => entry.ContentRevision == AshenMissionDataV1.Revision &&
                        !(entry.MissionId == X.Mission && entry.IndicatorId == MissionIndicator.UnnamedFrom + X.Marker))
                    .OrderBy(entry => entry.MissionId).ThenBy(entry => entry.ObjectiveId).ThenBy(entry => entry.IndicatorId).AsEnumerable()
                    .Select(entry => FormattableString.Invariant($"{entry.MissionId}.{entry.ObjectiveId}/{entry.IndicatorId}:{entry.PosX:R},{entry.PosY:R},{entry.PosZ:R},{entry.Radius:R}")))
                + " | " + string.Join("; ", world.MissionEvidenceEntries.AsNoTracking().Where(entry => entry.ContentRevision == AshenMissionDataV1.Revision && entry.SourceUri != X.Source)
                    .OrderBy(entry => entry.MissionId).ThenBy(entry => entry.EvidenceId).AsEnumerable().Select(entry => $"{entry.MissionId}/{entry.EvidenceId}"));
            const string All = "teleporter 2, link 2, marker 1, evidence 1", None = "teleporter 0, link 0, marker 0, evidence 0";
            var others = Others();
            Assert.AreEqual(All, Mine(), "at its migration");

            world.GetService<IMigrator>().Migrate(AshenMissionTests.Previous(world, FixH));
            world.ChangeTracker.Clear();
            Assert.AreEqual(None, Mine(), "Down");
            Assert.AreEqual(others, Others(), "Down: nothing else changes");
            using (var fresh = WildernessRuntimeTestHarness.Create(targetWorldMigration: AshenMissionTests.Previous(world, FixH)))
                Assert.AreEqual(others.Split(" | ")[0], string.Join("; ", fresh.World.TeleporterEntries.AsNoTracking().OrderBy(row => row.Id).AsEnumerable()
                    .Select(row => FormattableString.Invariant($"{row.Id}:{row.ClassId},{row.Type},{row.Description},{row.PosX:R},{row.PosY:R},{row.PosZ:R},{row.Rotation:R},{row.MapContextId}"))),
                    "the teleporters as a database migrated only to before fix H holds them");
            void Run(string sql) => world.Database.ExecuteSqlRaw(sql.Replace("{", "{{").Replace("}", "}}"));
            foreach (var sql in X.DownSql())
                Run(sql);
            Assert.AreEqual(None, Mine(), "a second Down changes nothing");

            world.GetService<IMigrator>().Migrate(FixH);
            world.ChangeTracker.Clear();
            Assert.AreEqual(All, Mine(), "Up again");
            foreach (var sql in X.UpSql())
                Run(sql);
            world.ChangeTracker.Clear();
            Assert.AreEqual(All, Mine(), "a second Up changes nothing");
            Assert.AreEqual(others, Others(), "nothing else changes");
            Assert.IsLessThan(0.01f, Vector3.Distance(UpperPad, V(world.TeleporterEntries.AsNoTracking().Single(row => row.Id == X.UpperPad))), "the upper pad's place");
        }

        /// <summary>The two pad rows on the test's Ashen Desert channel, as InitTeleporters puts them on a loaded map (CrucibleGrupaChiefsTests.OfficePads), and the links.</summary>
        private static void Pads(WildernessRuntimeTestHarness harness, MapChannel desert)
        {
            foreach (var row in harness.World.TeleporterEntries.AsNoTracking().Where(row => row.Id == X.LowerPad || row.Id == X.UpperPad).ToList())
            {
                var loaded = new DynamicObject
                {
                    Position = new Vector3((float)row.PosX, (float)row.PosY, (float)row.PosZ),
                    Rotation = row.Rotation,
                    MapContextId = row.MapContextId,
                    RuntimeMapChannel = desert,
                    EntityClassId = (EntityClasses)row.ClassId,
                    Comment = row.Description,
                    ObjectData = new WaypointInfo(row.Id, false, WaypointType.LocalTeleporter),
                    DynamicObjectType = DynamicObjectType.LocalTeleporter
                };
                desert.Teleporters.TryAdd(row.Id, loaded);
                harness.Objects.Teleporters.TryAdd(row.Id, loaded);
            }
            harness.Objects.InitLocalTeleporters();
            harness.Objects.DynamicObjectWorker(desert, 0);
        }

        /// <summary>Through the pad onto the far one (CrucibleGrupaChiefsTests.Teleport).</summary>
        private static void Teleport(WildernessRuntimeTestHarness harness, MapChannel map, DynamicObject pad)
        {
            harness.MoveTo(pad.Position);
            harness.Drain();
            harness.Objects.DynamicObjectProximityWorker(map, pad, 0);
            Assert.IsNotNull(harness.Client.PendingTransfer, "taken by the pad");
            System.Threading.Thread.Sleep(DynamicObjectManager.PassageHoldMs + 50);
            harness.Objects.PassagesWorker(map);
            harness.Objects.TeleportAcknowledge(harness.Client);
            harness.Drain();
            Assert.IsNull(harness.Client.PendingTransfer);
        }

        private static Vector3 V((double X, double Y, double Z) at) => new((float)at.X, (float)at.Y, (float)at.Z);

        private static Vector3 V(TeleporterEntry row) => new((float)row.PosX, (float)row.PosY, (float)row.PosZ);
    }
}
