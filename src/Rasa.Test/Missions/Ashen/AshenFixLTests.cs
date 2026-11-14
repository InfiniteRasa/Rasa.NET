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
using Rasa.Test.Missions.Marshes;
using Rasa.Test.Missions.Plateau;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions.Ashen
{
    using F = Rasa.Test.Missions.Marshes.MarshesClientFloorTests;
    using Outpost = AshenAvernusOutpostTests;
    using Overly = AshenOverlySensitiveV1;
    using X = AshenFixLV1;

    /// <summary>
    /// Ashen fix job L (AshenFixLV1, migration 20271110001920): to-fix row 46 (1473's teleporter sets the player down in Avernus
    /// Outpost's prison, which the real client keeps closed: no way out but /suicide). The one-way local teleporter 923 in the
    /// prison takes anyone onto 924, beside the inbound teleporter.
    ///
    /// Why these tests: a way out that is missing in a squad copy, that a player who never took 1473 cannot use, that stands in
    /// a prop's body or where the inbound arrival sets the player down on it, whose arrival stands on another pad's trigger or in
    /// the inbound teleporter's body, or a Down that leaves a row behind would each leave the player shut in or the database
    /// unlike a set 19 one. And the walk check that called the prison open: the server's collision has the room open, so the
    /// tests pin the room as the client has it (AshenAvernusOutpostTests.ClientClosedRooms) and every room a player is set down
    /// in that the client keeps closed must have a pad out of it.
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class AshenFixLTests
    {
        private const string FixL = "20271110001920_Ashen_fix_l";
        private const uint Map = X.MapContextId;

        /// <summary>The disc of the human teleporter's collision (arch_hum_teleporter.geo: x -1.49 .. 1.50, z -1.51 .. 1.42, 0.6 m high at most; the probe, 2026-11-12).</summary>
        private const float PadDisc = 1.51f;

        /// <summary>How near a local teleporter takes a player (DynamicObjectManager.LocalTeleporterWorker: IsNear2m, 3D).</summary>
        private const float PadReach = 2;

        internal static readonly Vector3 PrisonPad = V(X.PrisonPadAt), OutsidePad = V(X.OutsidePadAt);

        /// <summary>Where 923 sets a player down: 924's place a metre up (DynamicObjectManager.LocalTeleporterWorker).</summary>
        internal static readonly Vector3 OutsideArrival = OutsidePad + new Vector3(0, 1, 0);

        [TestInitialize]
        public void Clock() => PlateauGiantKillerSquadTests.UseClock();

        [TestCleanup]
        public void Restore() => PlateauGiantKillerSquadTests.RestoreClock();

        /// <summary>
        /// The rows: 923 (class 7895, the client's human teleporter, as the inbound one) and 924 (class 0: not drawn), local
        /// teleporters (type 1, rotation 0) on Avernus Outpost at their places; one link, 923 to 924; 924 takes nobody (no link) and
        /// no other link names either. 923 stands in the prison's measured box with a body's and a player's room to its walls,
        /// off the inbound arrival's and the stash's reach; the arrival by the inbound teleporter is off that teleporter's body by a
        /// player's width and off every other pad's trigger on the map (no loop; the tower's lift pad 952 too).
        /// </summary>
        [TestMethod]
        public void ThePrisonPadIsAOneWayWayOut()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            var rows = world.TeleporterEntries.AsNoTracking().Where(row => row.Id == X.PrisonPad || row.Id == X.OutsidePad).OrderBy(row => row.Id).ToList();
            Assert.HasCount(2, rows);
            foreach (var (row, (classId, description, at)) in rows.Zip(new[] { (X.PrisonPadClass, X.PrisonDescription, PrisonPad), (X.OutsidePadClass, X.OutsideDescription, OutsidePad) }))
            {
                Assert.AreEqual((Map, classId, X.LocalTeleporter, description, 0.0), (row.MapContextId, row.ClassId, (uint)row.Type, row.Description, row.Rotation), $"{row.Id}");
                Assert.IsLessThan(0.01f, Vector3.Distance(at, V(row)), $"{row.Id} place");
                Assert.IsLessThanOrEqualTo(64, row.Description.Length, $"{row.Id}: varchar(64)");
            }
            Assert.AreEqual(Overly.TeleporterClass, X.PrisonPadClass, "the inbound teleporter's class");
            int Links(string where) => world.Database.SqlQueryRaw<int>($"SELECT COUNT(*) AS Value FROM local_teleporter_link WHERE {where}").AsEnumerable().Single();
            Assert.AreEqual(1, Links($"from_teleporter_id = {X.PrisonPad} AND to_teleporter_id = {X.OutsidePad}"), "923 to 924");
            Assert.AreEqual(1, Links($"from_teleporter_id IN ({X.PrisonPad}, {X.OutsidePad}) OR to_teleporter_id IN ({X.PrisonPad}, {X.OutsidePad})"), "no other link: 924 takes nobody");

            var box = X.PrisonWalkable;
            Assert.IsGreaterThanOrEqualTo(PadDisc + Outpost.PlayerHalfWidth, (float)Math.Min(Math.Min(PrisonPad.X - box.MinX, box.MaxX - PrisonPad.X), Math.Min(PrisonPad.Z - box.MinZ, box.MaxZ - PrisonPad.Z)),
                "923 in the tester's walkable box, a body and a player from its edges");
            Assert.IsGreaterThan(PadReach + 2, Flat(PrisonPad, ToVector(Overly.PrisonArrival)), "the inbound arrival off 923's reach (and the tester's (-8.75, -46))");
            Assert.IsGreaterThan(PadReach + 2, Flat(PrisonPad, new Vector3(-8.75f, 520.02f, -46f)), "the tester's arrival off 923's reach");
            Assert.IsGreaterThan(PadReach + 2, Flat(PrisonPad, ToVector(Overly.StashAt)), "the stash's shooter off 923's reach");

            var inbound = ToVector(Overly.TeleporterAt);
            Assert.IsGreaterThanOrEqualTo(PadDisc + 2 * Outpost.PlayerHalfWidth + 1, Flat(OutsidePad, inbound), "the arrival off the inbound teleporter's body");
            var others = world.TeleporterEntries.AsNoTracking().Where(row => row.MapContextId == Map && row.Type == 1 && row.Id != X.OutsidePad).AsEnumerable().ToList();
            Assert.IsTrue(others.Any(row => row.Id == 952), "the tower's lift pad is among them");
            foreach (var row in others)
                Assert.IsGreaterThan(PadReach, Vector3.Distance(OutsideArrival, V(row)), $"the arrival off pad {row.Id}'s trigger");
            Assert.IsGreaterThan(PadReach, Vector3.Distance(ToVector(Overly.PrisonArrival), PrisonPad), "the inbound arrival off 923's trigger");
        }

        /// <summary>
        /// The way out is the World DB's: the server's own loader (InitTeleporters, with Avernus Outpost loaded) puts 923 and 924 on
        /// the base map as local teleporters, and a squad's copy made from it (CloneTemplateMap, as MapChannelManager makes one) has
        /// them too.
        /// </summary>
        [TestMethod]
        public void TheWayOutIsOnTheBaseMapAndInASquadsCopy()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var template = harness.Maps.MapChannelArray.TryGetValue(Map, out var loaded) ? loaded : PlateauInstanceMaps.LoadMap(harness, Map);
            // The harness ran the loader before Avernus Outpost was loaded: run it again, as at start-up, with it loaded.
            harness.Objects.Teleporters.Clear();
            foreach (var map in harness.Maps.MapChannelArray.Values)
                map.Teleporters.Clear();
            harness.Objects.InitTeleporters();
            harness.Objects.InitLocalTeleporters();
            var squad = new MapChannel { MapInfo = template.MapInfo, ClientList = new List<Client>(), PlayerLimit = 128, NavMesh = template.NavMesh };
            harness.Objects.CloneTemplateMap(template, squad);
            foreach (var (label, map) in new[] { ("the base map", template), ("the squad's copy", squad) })
                foreach (var (id, classId, at) in new[] { (X.PrisonPad, X.PrisonPadClass, PrisonPad), (X.OutsidePad, X.OutsidePadClass, OutsidePad) })
                {
                    Assert.IsTrue(map.Teleporters.TryGetValue(id, out var pad), $"{label}: {id}");
                    Assert.AreEqual((DynamicObjectType.LocalTeleporter, classId, WaypointType.LocalTeleporter, id), (pad.DynamicObjectType, (uint)pad.EntityClassId,
                        (pad.ObjectData as WaypointInfo)?.WaypointType, (pad.ObjectData as WaypointInfo)?.WaypointId ?? 0), $"{label}: {id}");
                    Assert.IsLessThan(0.01f, Vector3.Distance(at, pad.Position), $"{label}: {id}'s place");
                }
        }

        /// <summary>
        /// In the squad's copy: a player who never took 1473, set down where the inbound teleporter sets one, is not taken (923 is
        /// out of reach); he walks onto 923 and is held and set down by the inbound teleporter (924), and 924 does not send him
        /// back. A squad mate (without 1473 too) is taken the same way.
        /// </summary>
        [TestMethod]
        public void ThePrisonPadTakesAnyoneOut()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var copy = Outpost.IntoOutpost(harness);
            Outpost.Pads(harness, copy);
            var way = copy.Teleporters[X.PrisonPad];
            var arrival = copy.Teleporters[X.OutsidePad];
            Assert.IsFalse(harness.Client.Player.Missions.ContainsKey(Overly.MissionId), "the player never took 1473");

            harness.MoveTo(ToVector(Overly.PrisonArrival));
            harness.Drain();
            harness.Objects.DynamicObjectProximityWorker(copy, way, 0);
            Assert.IsNull(harness.Client.PendingTransfer, "the inbound arrival is not on 923");

            Outpost.Teleport(harness, copy, way);
            Assert.IsLessThan(1.01f, Vector3.Distance(OutsidePad, harness.Client.Player.Position), "set down by the inbound teleporter");
            harness.Objects.DynamicObjectProximityWorker(copy, arrival, 0);
            harness.Objects.DynamicObjectProximityWorker(copy, way, 0);
            Assert.IsNull(harness.Client.PendingTransfer, "924 takes nobody back");

            var karter = Rasa.Test.Missions.Marshes.MarshesPreoDasTests.Giver(copy, AshenAvernusOutpostNpcsV1.Karter);
            using var mate = new AshenIndraFacilityTests.SquadMate(harness, copy, karter, 0, Array.Empty<uint>());
            CellManager.Instance.RemoveFromWorld(mate.Client);
            mate.Beside(way.Position);
            CellManager.Instance.AddToWorld(mate.Client);
            harness.Drain();
            harness.Objects.DynamicObjectProximityWorker(copy, way, 0);
            var transfer = mate.Client.PendingTransfer;
            Assert.IsNotNull(transfer, "the mate is taken");
            Assert.IsLessThan(0.01f, Vector3.Distance(OutsideArrival, transfer.DestinationPosition), "to 924, a metre up");
            mate.Client.PendingTransfer = null;
        }

        /// <summary>
        /// The prison as the walk check sees it: the server's collision (MapGeometry from the client's .map and .geo) has the room
        /// open to the east (stair, corridor, ramp): ClientFloorLine walks the tester's reversed leg out of it, which the real
        /// client stops at once (to-fix row 46). Should this fail one day, the server's geometry has learned the wall, and the
        /// ClientClosedRooms entry can go. The closed-room rule refuses that leg and set 17's leg in; it passes the legs inside the
        /// room. And the rule this asks for: each place the scenes set a player down in a closed room (1473's inbound arrival) has
        /// a local teleporter in that room linked to a pad outside every closed room.
        /// </summary>
        [TestMethod]
        public void ThePrisonIsClosedInTheClientOnly()
        {
            var dataDir = F.ClientData();
            using var harness = WildernessRuntimeTestHarness.Create();
            using var meshes = new MeshLibrary(dataDir, F.MeshCsv());
            var geometry = MapGeometry.Load(Path.Combine(dataDir, "maps", AshenIndraFacilityTests.MapName(harness, Map)), meshes, 1);
            var outward = new List<Vector3> { new(-8f, 520.01f, -46f), new(69f, 515.87f, -37f) };
            Assert.IsNull(AshenFixBTests.ClientFloorLine(geometry, outward, outward[^1], null, Outpost.PlayerHalfWidth),
                "the server's collision has the prison open to the east (if not: the wall is known now, ClientClosedRooms' prison can go)");
            Assert.IsNotNull(Outpost.ClosedRoomCrossing(outward), "the closed-room rule refuses the way out by the east stair");
            var setSeventeen = new List<Vector3> { new(76f, 525.37f, 1f), new(69f, 515.87f, -37f), new(-8f, 520.01f, -46f), new(-6.5f, 520.01f, -50.2f) };
            Assert.IsNotNull(Outpost.ClosedRoomCrossing(setSeventeen), "and set 17's way in");
            foreach (var leg in new[] { "in the prison to the stash", "in the prison to the way out" })
                Assert.IsNull(Outpost.ClosedRoomCrossing(Outpost.Legs[leg]), leg);
            Assert.IsNull(Outpost.ClosedRoomCrossing(Outpost.Legs["from the way out's arrival"]), "the arrival is outside");

            bool In(Vector3 at, (double MinX, double MaxX, double MinZ, double MaxZ, double Y) box) =>
                at.X >= box.MinX && at.X <= box.MaxX && at.Z >= box.MinZ && at.Z <= box.MaxZ && MathF.Abs(at.Y - (float)box.Y) <= 3;
            var pads = harness.World.TeleporterEntries.AsNoTracking().Where(row => row.MapContextId == Map && row.Type == 1).ToList();
            var links = harness.World.LocalTeleporterLinkEntries.AsNoTracking().AsEnumerable().ToDictionary(link => link.FromTeleporterId, link => link.ToTeleporterId);
            foreach (var (label, box) in Outpost.ClientClosedRooms)
            {
                Assert.IsTrue(In(ToVector(Overly.PrisonArrival), box), $"1473's inbound arrival is in {label}");
                Assert.IsTrue(pads.Any(pad => In(V(pad), box) && links.TryGetValue(pad.Id, out var to) &&
                    pads.SingleOrDefault(other => other.Id == to) is { } far && !Outpost.ClientClosedRooms.Any(room => In(V(far), room.Box))),
                    $"{label}: a pad in it takes the player out");
            }
        }

        /// <summary>
        /// Audit R1: 923's body (arch_hum_teleporter.geo, rotation 0; the probe's dump, 2026-11-12): a disc 0.18 m up out to about
        /// 0.95 m from its centre, its rim falling at about 24 degrees to the floor at 1.3 m (local +x and -x) and 1.35 m (local
        /// -z); on local +z a 0.59 m block from 1.0 to 1.4 m out, a step. With the body in the room's geometry, the walk to the way
        /// out comes over the rim on local +x or -x within |z| 0.45 or on -z within |x| 0.45 (its last stretch), passes on the
        /// client floor with a player's width and ends on the disc within 0.3 m of the centre: 1.7 m and more inside the 2 m
        /// trigger. A walk in from the north (+z) is stopped by the step: the profile is the one the route avoids.
        /// </summary>
        [TestMethod]
        public void ThePrisonPadIsWalkedOntoOverItsLowRim()
        {
            var dataDir = F.ClientData();
            using var harness = WildernessRuntimeTestHarness.Create();
            using var meshes = new MeshLibrary(dataDir, F.MeshCsv());
            var geometry = MapGeometry.Load(Path.Combine(dataDir, "maps", AshenIndraFacilityTests.MapName(harness, Map)), meshes, 1);
            var row = harness.World.TeleporterEntries.AsNoTracking().Single(entry => entry.Id == X.PrisonPad);
            Assert.AreEqual(0.0, row.Rotation, "923 at rotation 0: local axes are the map's");
            AshenConscriptionEntryTests.AddObject(geometry, meshes, (int)X.PrisonPadClass, PrisonPad, 0);
            var near = F.Near(geometry, new List<Vector3> { PrisonPad }, 3)[0];
            float? Rise(float dx, float dz) => F.FloorAt(near, PrisonPad + new Vector3(dx, 0, dz), PrisonPad.Y - 1, PrisonPad.Y + 1.2f) - PrisonPad.Y;
            Assert.AreEqual(0.18f, Rise(0, 0) ?? 0, 0.02f, "the disc 0.18 m up");
            foreach (var (dx, dz) in new[] { (1.5f, 0f), (-1.5f, 0f), (0f, -1.5f), (0f, 1.5f) })
                Assert.AreEqual(0f, Rise(dx, dz) ?? 1, 0.02f, FormattableString.Invariant($"the room's floor at ({dx}, {dz}) from the centre"));
            Assert.AreEqual(0.59f, Rise(0, 1.2f) ?? 0, 0.02f, "the north side's step");

            var line = Outpost.Legs["in the prison to the way out"].ToList();
            Assert.IsLessThan(0.01f, Flat(line[^1], PrisonPad), "the leg ends at 923's centre");
            var last = Vector2.Normalize(new Vector2(line[^2].X - PrisonPad.X, line[^2].Z - PrisonPad.Z));
            Assert.IsTrue(MathF.Abs(last.Y * 1.3f) <= 0.45f || last.Y < 0 && MathF.Abs(last.X * 1.35f) <= 0.45f,
                FormattableString.Invariant($"the leg's last stretch crosses the rim at local ({last.X * 1.3f:0.00}, {last.Y * 1.3f:0.00}): on a low side"));
            var track = new List<Vector3>();
            Assert.IsNull(AshenFixBTests.ClientFloorLine(geometry, line, line[^1], null, Outpost.PlayerHalfWidth, track), "the leg onto 923 on the client floor");
            Assert.IsLessThan(0.3f, Flat(track[^1], PrisonPad), "it ends on the disc at the centre");
            Assert.IsGreaterThan(0.15f, track[^1].Y - PrisonPad.Y, "up on the disc");
            var north = new List<Vector3> { PrisonPad + new Vector3(0, 0, 4), PrisonPad };
            Assert.IsNotNull(AshenFixBTests.ClientFloorLine(geometry, north, PrisonPad, null, Outpost.PlayerHalfWidth), "the north side's step stops a walk in");
        }

        /// <summary>
        /// The pads on the client's floor (within 0.3 m of the highest floor under them, nothing within 3 m over them); nothing of
        /// the placed meshes inside 923's body and a player round it (16 lines from its middle out to 2 m, at knee, chest and head
        /// height) nor round the arrival.
        /// </summary>
        [TestMethod]
        public void ThePadsStandFreeOnTheClientsFloor()
        {
            var dataDir = F.ClientData();
            using var harness = WildernessRuntimeTestHarness.Create();
            using var meshes = new MeshLibrary(dataDir, F.MeshCsv());
            var geometry = MapGeometry.Load(Path.Combine(dataDir, "maps", AshenIndraFacilityTests.MapName(harness, Map)), meshes, 1);
            var near = F.Near(geometry, new List<Vector3> { PrisonPad, OutsidePad }, 4);
            var failures = new List<string>();
            foreach (var (label, at, triangles) in new[] { ("923", PrisonPad, near[0]), ("924", OutsidePad, near[1]) })
            {
                if (F.FloorAt(triangles, at, at.Y - 3, at.Y + 1) is not { } floor || MathF.Abs(floor - at.Y) > 0.3f)
                    failures.Add($"{label}: the client's floor");
                if (!F.Clear(triangles, at + new Vector3(0, 0.3f, 0), at + new Vector3(0, 3, 0)))
                    failures.Add($"{label}: something over it");
                var solids = triangles.Where(t => !new[] { t.A, t.B, t.C }.All(v => geometry.Terrain.Height(v.X, v.Z) is { } h && MathF.Abs(h - v.Y) < 0.01f)).ToList();
                for (var k = 0; k < 16; k++)
                {
                    var angle = k * MathF.PI / 8;
                    var out2 = new Vector3(MathF.Sin(angle), 0, MathF.Cos(angle)) * (PadDisc + Outpost.PlayerHalfWidth);
                    foreach (var height in new[] { AshenFixBTests.Knee, AshenFixBTests.Chest, AshenFixBTests.Head })
                        if (!F.Clear(solids, at + new Vector3(0, height, 0), at + out2 + new Vector3(0, height, 0)))
                            failures.Add(FormattableString.Invariant($"{label}: something within {PadDisc + Outpost.PlayerHalfWidth} m at {k * 22.5:0} degrees, {height} m up"));
                }
            }
            Assert.IsEmpty(failures, string.Join("; ", failures));
        }

        /// <summary>
        /// Ashen_fix_l rolls back: Down takes out the two teleporters, the link, 1473's and 1498's evidence rows 4 and puts set 19's
        /// 1498 scene back (row 47), and nothing else; a second
        /// Down changes nothing; Up again, they are back; a second Up changes nothing (the guards).
        /// </summary>
        [TestMethod]
        public void FixLRollsBackWithItsMigration()
        {
            using var harness = WildernessRuntimeTestHarness.Create(targetWorldMigration: FixL);
            var world = harness.World;
            int Count(string sql) => world.Database.SqlQueryRaw<int>(sql).AsEnumerable().Single();
            string Mine() => string.Join(", ",
                $"teleporter {Count($"SELECT COUNT(*) AS Value FROM teleporter WHERE id IN ({X.PrisonPad}, {X.OutsidePad})")}",
                $"link {Count($"SELECT COUNT(*) AS Value FROM local_teleporter_link WHERE from_teleporter_id IN ({X.PrisonPad}, {X.OutsidePad}) OR to_teleporter_id IN ({X.PrisonPad}, {X.OutsidePad})")}",
                $"evidence {Count($"SELECT COUNT(*) AS Value FROM mission_evidence WHERE mission_id IN ({X.Mission}, {X.Blast}) AND content_revision = '{AshenMissionDataV1.Revision}' AND evidence_id = {X.EvidenceId} AND source_uri = '{X.Source}'")}",
                $"1498 scene {Scene()}",
                FormattableString.Invariant($"531066 y {world.SpawnPoolEntries.AsNoTracking().Single(entry => entry.Id == AshenBlastTheBiolabV1.PartsPool).PosY:0.00}"));
            string Scene()
            {
                var bindings = world.Set<MissionSceneBindingEntry>().AsNoTracking().Single(entry => entry.MissionId == X.Blast && entry.ContentRevision == AshenMissionDataV1.Revision).Bindings;
                return bindings == X.BlastSceneNow() ? "fix L's" : bindings == X.BlastSceneBefore() ? "set 19's" : "neither";
            }
            string Others() => string.Join("; ", world.TeleporterEntries.AsNoTracking().Where(row => row.Id != X.PrisonPad && row.Id != X.OutsidePad).OrderBy(row => row.Id).AsEnumerable()
                    .Select(row => FormattableString.Invariant($"{row.Id}:{row.ClassId},{row.Type},{row.Description},{row.PosX:R},{row.PosY:R},{row.PosZ:R},{row.Rotation:R},{row.MapContextId}")))
                + " | " + Count($"SELECT COUNT(*) AS Value FROM local_teleporter_link WHERE from_teleporter_id NOT IN ({X.PrisonPad}, {X.OutsidePad})") + ", "
                + Count($"SELECT COALESCE(SUM(from_teleporter_id * 1000 + to_teleporter_id), 0) AS Value FROM local_teleporter_link WHERE from_teleporter_id NOT IN ({X.PrisonPad}, {X.OutsidePad})")
                + " | " + string.Join("; ", world.MissionEvidenceEntries.AsNoTracking().Where(entry => entry.ContentRevision == AshenMissionDataV1.Revision && entry.SourceUri != X.Source)
                    .OrderBy(entry => entry.MissionId).ThenBy(entry => entry.EvidenceId).AsEnumerable().Select(entry => $"{entry.MissionId}/{entry.EvidenceId}:{entry.SourceUri}"));
            const string All = "teleporter 2, link 1, evidence 2, 1498 scene fix L's, 531066 y 105.27", None = "teleporter 0, link 0, evidence 0, 1498 scene set 19's, 531066 y 103.90";
            var others = Others();
            Assert.AreEqual(All, Mine(), "at its migration");
            var evidence = world.MissionEvidenceEntries.AsNoTracking().Single(entry => entry.MissionId == X.Mission && entry.ContentRevision == AshenMissionDataV1.Revision && entry.EvidenceId == X.EvidenceId);
            Assert.AreEqual((X.Source, X.Note, 0.3, MissionEvidenceSourceKind.Reconstruction), (evidence.SourceUri, evidence.ReconstructionNote, evidence.Confidence, evidence.SourceKind), "1473's evidence 4");

            world.GetService<IMigrator>().Migrate(AshenMissionTests.Previous(world, FixL));
            world.ChangeTracker.Clear();
            Assert.AreEqual(None, Mine(), "Down");
            Assert.AreEqual(others, Others(), "Down: nothing else changes");
            using (var fresh = WildernessRuntimeTestHarness.Create(targetWorldMigration: AshenMissionTests.Previous(world, FixL)))
                Assert.AreEqual(others.Split(" | ")[0], string.Join("; ", fresh.World.TeleporterEntries.AsNoTracking().OrderBy(row => row.Id).AsEnumerable()
                    .Select(row => FormattableString.Invariant($"{row.Id}:{row.ClassId},{row.Type},{row.Description},{row.PosX:R},{row.PosY:R},{row.PosZ:R},{row.Rotation:R},{row.MapContextId}"))),
                    "the teleporters as a database migrated only to before fix L holds them");
            void Run(string sql) => world.Database.ExecuteSqlRaw(sql.Replace("{", "{{").Replace("}", "}}"));
            foreach (var sql in X.DownSql())
                Run(sql);
            Assert.AreEqual(None, Mine(), "a second Down changes nothing");

            world.GetService<IMigrator>().Migrate(FixL);
            world.ChangeTracker.Clear();
            Assert.AreEqual(All, Mine(), "Up again");
            foreach (var sql in X.UpSql())
                Run(sql);
            world.ChangeTracker.Clear();
            Assert.AreEqual(All, Mine(), "a second Up changes nothing");
            Assert.AreEqual(others, Others(), "nothing else changes");
            Assert.IsLessThan(0.01f, Vector3.Distance(PrisonPad, V(world.TeleporterEntries.AsNoTracking().Single(row => row.Id == X.PrisonPad))), "923's place");
        }

        /// <summary>
        /// To-fix row 47: in a squad's copy of the Bane Conscription Facility, 1498's drop pools (531064 bio, 531066 parts), each
        /// killed to the last, come back on their own respawn time (120 s, not before) as the clock runs on: no reset of the pool's cleared
        /// mark, no forced respawn. Set 19's scene left them to the copy's 45 min (SquadInstances.RespawnMinutes).
        /// </summary>
        [TestMethod]
        public void BlastsDropPoolsComeBackOnTheirOwnTimeInASquadsCopy()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var copy = AshenConscriptionEntryTests.IntoFacility(harness);
            Assert.IsNotNull(copy.SquadState, "a squad's copy");
            foreach (var id in new[] { AshenBlastTheBiolabV1.BioPool, AshenBlastTheBiolabV1.PartsPool })
            {
                Assert.IsTrue(SpawnPoolManager.Instance.IsNamedPublicSpawn(id), $"1498's scene names pool {id}");
                AshenIndraFacilityTests.Stand(copy, id);
                foreach (var creature in MarshesPreoDasTests.Here(copy).Where(one => one.SpawnPool?.DbId == id && one.State != CharacterState.Dead).ToArray())
                    MarshesPreoDasTests.Kill(harness, copy, creature);
                bool Standing() => MarshesPreoDasTests.Here(copy).Any(one => one.SpawnPool?.DbId == id && one.State != CharacterState.Dead);
                Assert.IsFalse(Standing(), $"pool {id} down to the last");
                var all = copy.SpawnPools.ToArray();
                var pool = all.Single(candidate => candidate.DbId == id);
                var back = 0;
                try
                {
                    copy.SpawnPools.Clear();
                    copy.SpawnPools.Add(pool);
                    SpawnPoolManager.Instance.SpawnPoolWorker(copy, 0);
                    Assert.AreNotEqual(0L, pool.ClearedAtUtcMs, $"pool {id} marked cleared (the copy's 45 min would apply)");
                    Assert.AreEqual(1200 * 100L, pool.RespawnTime, $"pool {id}: 120 s");
                    for (var second = 1; second <= 125 && !Standing(); second++)
                    {
                        harness.UtcNow = harness.UtcNow.AddSeconds(1);
                        SpawnPoolManager.Instance.SpawnPoolWorker(copy, 1000);
                        harness.Drain();
                        if (Standing())
                            back = second;
                    }
                }
                finally
                {
                    copy.SpawnPools.Clear();
                    copy.SpawnPools.AddRange(all);
                }
                Assert.IsTrue(Standing(), $"pool {id} back on its own 120 s in the squad's copy");
                Assert.IsGreaterThanOrEqualTo(120, back, $"pool {id} not back before its 120 s (back at {back} s)");
            }
        }

        private static float Flat(Vector3 a, Vector3 b) => Vector2.Distance(new(a.X, a.Z), new(b.X, b.Z));

        private static Vector3 ToVector(Rasa.Missions.Scenes.ScenePosition at) => new(at.X, at.Y, at.Z);

        private static Vector3 V((double X, double Y, double Z) at) => new((float)at.X, (float)at.Y, (float)at.Z);

        private static Vector3 V(TeleporterEntry row) => new((float)row.PosX, (float)row.PosY, (float)row.PosZ);
    }
}
