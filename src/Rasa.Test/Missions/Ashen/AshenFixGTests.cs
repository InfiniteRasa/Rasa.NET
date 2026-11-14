using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.ClientData;
using Rasa.Services.Preloader.Missions.Ashen;
using Rasa.Structures;
using Rasa.Structures.World;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions.Ashen
{
    using static Rasa.Test.Missions.Marshes.MarshesPaludosTests;
    using static Rasa.Test.Missions.Plateau.PlateauCampResistanceTests;
    using F = Rasa.Test.Missions.Marshes.MarshesClientFloorTests;
    using X = AshenFixGV1;

    /// <summary>
    /// Ashen fix job G (AshenFixGV1, migration 20271110001320): to-fix row 24 (the real client could not climb set 13's way to
    /// the Indra Pass waypoint; the client-floor walk check passed it) and the Atta pack tuning (real client 2026-11-10 22:46).
    ///
    /// Why these tests: the walk check must fail the leg the real client could not climb (the client's 55 degree slope limit,
    /// read from tabula_rasa.exe), else a place nobody reaches passes again; set 13's NPCs, four resistors and their markers
    /// must stand where fix G puts them; the seven Atta pools must put down one creature; and Down must put set 13's, set 2's
    /// and fix F's exact rows back.
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class AshenFixGTests
    {
        private const string FixG = "20271110001320_Ashen_fix_g";

        /// <summary>To-fix row 24: the leg the real client could not climb, and the places it stopped at instead.</summary>
        private static readonly Vector3 LegFoot = new(99f, 294.49f, 482f), LegTop = new(86f, 299.74f, 469f);
        private static readonly Vector3[] RealClientStops =
        {
            new(89.4f, 293.9f, 468.0f), new(92.9f, 292.6f, 472.0f), new(92.4f, 293.2f, 472.0f), new(89.2f, 293.9f, 467.6f), new(88.5f, 294.1f, 466.9f)
        };

        /// <summary>
        /// The client's climb limit, from tabula_rasa.exe 1.16.5.0 (file offsets; the code is at image base 0x400000 + offset):
        /// the setting's name "Client.Developer.MaxAngleToClimb" at 0x7db5e0, the code that reads it pushes that string's address
        /// (0x4cdb9f: push 0xbdb5e0) and sets the default 0x37 = 55 (0x4cdbcb: mov [esp+0x20], 0x37) before it divides by 180 and
        /// multiplies by pi. AshenFixBTests.MaxClimbAngle is that value.
        /// </summary>
        [TestMethod]
        public void TheWalkCheckClimbsWhatTheClientClimbs()
        {
            var exe = Path.Combine(F.ClientData(), "..", "tabula_rasa.exe");
            if (!File.Exists(exe))
                Assert.Inconclusive($"no client executable at {exe}");
            var bytes = File.ReadAllBytes(exe);
            Assert.AreEqual("Client.Developer.MaxAngleToClimb", Encoding.ASCII.GetString(bytes, 0x7db5e0, 32), "the setting's name");
            CollectionAssert.AreEqual(new byte[] { 0x68, 0xe0, 0xb5, 0xbd, 0x00 }, bytes.Skip(0xcdb9f).Take(5).ToArray(), "push the name's address");
            CollectionAssert.AreEqual(new byte[] { 0xc7, 0x44, 0x24, 0x20, 0x37, 0x00, 0x00, 0x00 }, bytes.Skip(0xcdbcb).Take(8).ToArray(), "the default, 55");
            Assert.AreEqual(55f, AshenFixBTests.MaxClimbAngle, "the check's limit");
            Assert.AreEqual(MathF.Cos(55 * MathF.PI / 180), AshenFixBTests.MinClimbNormal, 1e-6, "as a floor normal");
        }

        /// <summary>
        /// The calibration (to-fix row 24): the leg (99, 294.49, 482) -> (86, 299.74, 469) fails as too steep (52-59 degree
        /// terrain across the line; it rises only 0.3-0.6 m a metre along it, which the 1.1 m step rule passes); a line from
        /// each of the real client's five stops to the leg's top fails too (the rock face); down the leg passes (it slides).
        /// </summary>
        [TestMethod]
        public void TheLegTheRealClientCouldNotClimbFails()
        {
            var geometry = Geometry();
            var up = AshenFixBTests.ClientFloorLine(geometry, new[] { LegFoot, LegTop }, LegTop);
            Assert.IsTrue(up?.StartsWith("too steep to climb", StringComparison.Ordinal) == true, $"up the leg: {up}");
            var wide = AshenFixBTests.ClientFloorLine(geometry, new[] { LegFoot, LegTop }, LegTop, null, AshenFormalComplaintsTests.PlayerHalfWidth);
            Assert.IsTrue(wide?.StartsWith("too steep to climb", StringComparison.Ordinal) == true, $"up the leg, a player's width: {wide}");
            foreach (var stop in RealClientStops)
                Assert.IsNotNull(AshenFixBTests.ClientFloorLine(geometry, new[] { stop, LegTop }, LegTop), $"from the real client's stop {stop}");
            Assert.IsNull(AshenFixBTests.ClientFloorLine(geometry, new[] { LegTop, LegFoot }, LegFoot), "down the leg");
        }

        /// <summary>
        /// The places: Odesha's and Endala's pools at fix G's places (their own creature, one each); 1281's scene holds the ten
        /// resistors at fix G's places (four moved, six as set 13 left them) and its markers 1-10 stand on them; 1282's Endala
        /// marker on her; nothing of set 13 on the Indra Pass waypoint's floor (within 120 m of the waypoint).
        /// </summary>
        [TestMethod]
        public void SetThirteensPlacesAreInTheValley()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            foreach (var (pool, at) in new[] { (AshenIndraPassNpcsV1.Odesha, X.OdeshaAt), (AshenIndraPassNpcsV1.Endala, X.EndalaAt) })
            {
                var row = world.SpawnPoolEntries.AsNoTracking().Single(entry => entry.Id == pool);
                Assert.AreEqual((pool, (byte)1, (byte)1, at.X, at.Y, at.Z), (row.Creature1Id, row.Creature1MinCount, row.Creature1MaxCount, row.PosX, row.PosY, row.PosZ), $"pool {pool}");
            }
            var resistors = X.ResistorsAt();
            for (var number = 1; number <= resistors.Length; number++)
            {
                var moved = X.MovedResistors.Any(entry => entry.Number == number);
                var set13 = AshenGodOfStormsV1.ResistorsAt[number - 1];
                Assert.AreEqual(moved, Vector3.Distance(new(set13.X, set13.Y, set13.Z), new(resistors[number - 1].X, resistors[number - 1].Y, resistors[number - 1].Z)) > 1, $"resistor {number} moved: {moved}");
                var marker = Marker(world, AshenGodOfStormsV1.MissionId, (uint)number);
                Assert.IsLessThan(0.01f, Vector3.Distance(new((float)marker.PosX, (float)marker.PosY, (float)marker.PosZ), new(resistors[number - 1].X, resistors[number - 1].Y, resistors[number - 1].Z)), $"resistor {number}'s marker");
            }
            Assert.AreEqual(X.StormsNow(), Scene(world), "1281's scene");
            StringAssert.Contains(Scene(world), "\"position\":{\"x\":201,\"y\":317.56,\"z\":525}", "resistor 3's new place in the JSON");
            var endala = Marker(world, AshenBondsOfBloodV1.MissionId, X.EndalaMarker);
            Assert.AreEqual((X.EndalaAt.X, X.EndalaAt.Y, X.EndalaAt.Z), (endala.PosX, endala.PosY, endala.PosZ), "Endala's marker");
            var waypoint = new Vector2(AshenIndraPassTests.IndraWaypoint.X, AshenIndraPassTests.IndraWaypoint.Z);
            foreach (var (label, at) in AshenIndraPassTests.Spots())
                Assert.IsGreaterThan(120f, Vector2.Distance(waypoint, new(at.X, at.Z)), $"{label} off the waypoint's floor");
        }

        /// <summary>
        /// The Atta pools (set 2's six, fix F's 790378): one creature each, min and max, their creature, set 2's 120 s and radius
        /// 0, at their places; spawned, each puts down one living Atta of its kind.
        /// </summary>
        [TestMethod]
        public void TheAttaPoolsPutDownOne()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            Assert.HasCount(7, X.AttaPools, "seven pools");
            foreach (var (pool, creature) in X.AttaPools)
            {
                var row = harness.World.SpawnPoolEntries.AsNoTracking().Single(entry => entry.Id == pool);
                Assert.AreEqual((creature, (byte)1, (byte)1, 1200U, 0.0), (row.Creature1Id, row.Creature1MinCount, row.Creature1MaxCount, row.RespawnTime, row.Radius), $"pool {pool}");
            }
            var desert = AshenLeadInsTests.EnterAshen(harness, AshenLeadInsTests.ShadowsEdgeWaypoint);
            Spawn(desert, X.AttaPools.Select(pool => pool.Pool).ToArray());
            foreach (var (pool, creature) in X.AttaPools)
            {
                var here = Alive(desert).Where(candidate => candidate.SpawnPool?.DbId == pool).Distinct().ToArray();
                Assert.HasCount(1, here, $"pool {pool}: one");
                Assert.AreEqual(creature, here[0].DbId, $"pool {pool}: its creature");
            }
        }

        /// <summary>
        /// Ashen_fix_g rolls back: Down puts set 13's NPC places, resistors (the scene's exact JSON) and markers back, set 2's
        /// and fix F's three Atta a pool, and takes fix G's evidence rows out; Up again, fix G's are back; a second Up changes
        /// nothing; a row holding neither value is left alone both ways; nothing else changes.
        /// </summary>
        [TestMethod]
        public void FixGRollsBackWithItsMigration()
        {
            // At its migration (a later set's rows, set 14's on, are not this one's "nothing else"), as the set 12 and 13 tests.
            using var harness = WildernessRuntimeTestHarness.Create(targetWorldMigration: FixG);
            var world = harness.World;
            (double X, double Y, double Z) Pool(uint id)
            {
                var row = world.SpawnPoolEntries.AsNoTracking().Single(entry => entry.Id == id);
                return (row.PosX, row.PosY, row.PosZ);
            }
            byte Count(uint id) => world.SpawnPoolEntries.AsNoTracking().Single(entry => entry.Id == id).Creature1MaxCount;
            int Evidence() => world.MissionEvidenceEntries.AsNoTracking().Count(entry => entry.ContentRevision == AshenMissionDataV1.Revision && entry.SourceUri == X.Source);
            Vector3 MarkerAt(uint mission, uint number)
            {
                var row = Marker(world, mission, number);
                return new((float)row.PosX, (float)row.PosY, (float)row.PosZ);
            }
            var moved = X.AttaPools.Select(pool => pool.Pool).Append(AshenIndraPassNpcsV1.Odesha).Append(AshenIndraPassNpcsV1.Endala).ToArray();
            string Others() => string.Join("; ", world.SpawnPoolEntries.AsNoTracking()
                    .Where(entry => entry.MapContextId == AshenMissionDataV1.MapContextId && !moved.Contains(entry.Id)).OrderBy(entry => entry.Id).AsEnumerable()
                    .Select(entry => FormattableString.Invariant($"{entry.Id}:{entry.PosX:R},{entry.PosY:R},{entry.PosZ:R},{entry.Creature1Id},{entry.Creature1MaxCount},{entry.RespawnTime}")))
                + " | " + string.Join("; ", world.MissionIndicatorEntries.AsNoTracking().Where(entry => entry.ContentRevision == AshenMissionDataV1.Revision
                        && entry.MissionId != AshenGodOfStormsV1.MissionId && entry.MissionId != AshenBondsOfBloodV1.MissionId)
                    .OrderBy(entry => entry.MissionId).ThenBy(entry => entry.ObjectiveId).ThenBy(entry => entry.IndicatorId).AsEnumerable()
                    .Select(entry => FormattableString.Invariant($"{entry.MissionId}.{entry.ObjectiveId}/{entry.IndicatorId}:{entry.PosX:R},{entry.PosY:R},{entry.PosZ:R}")))
                + " | " + string.Join("; ", world.MissionEvidenceEntries.AsNoTracking().Where(entry => entry.ContentRevision == AshenMissionDataV1.Revision && entry.SourceUri != X.Source)
                    .OrderBy(entry => entry.MissionId).ThenBy(entry => entry.EvidenceId).AsEnumerable().Select(entry => $"{entry.MissionId}/{entry.EvidenceId}"));
            void Fixed(string when)
            {
                Assert.AreEqual(X.OdeshaAt, Pool(AshenIndraPassNpcsV1.Odesha), $"{when}: Odesha");
                Assert.AreEqual(X.EndalaAt, Pool(AshenIndraPassNpcsV1.Endala), $"{when}: Endala");
                Assert.AreEqual(X.StormsNow(), Scene(world), $"{when}: 1281's scene");
                foreach (var (number, at) in X.MovedResistors)
                    Assert.IsLessThan(0.01f, Vector3.Distance(new(at.X, at.Y, at.Z), MarkerAt(AshenGodOfStormsV1.MissionId, (uint)number)), $"{when}: marker {number}");
                Assert.IsLessThan(0.01f, Vector3.Distance(new((float)X.EndalaAt.X, (float)X.EndalaAt.Y, (float)X.EndalaAt.Z), MarkerAt(AshenBondsOfBloodV1.MissionId, X.EndalaMarker)), $"{when}: Endala's marker");
                foreach (var (pool, _) in X.AttaPools)
                    Assert.AreEqual((byte)X.AttaCount, Count(pool), $"{when}: pool {pool} one");
                Assert.AreEqual(X.Evidences.Length, Evidence(), $"{when}: fix G's evidence rows");
            }
            var others = Others();
            Fixed("at its migration");

            world.GetService<IMigrator>().Migrate(AshenMissionTests.Previous(world, FixG));
            world.ChangeTracker.Clear();
            Assert.AreEqual(AshenIndraPassNpcsV1.OdeshaAt, Pool(AshenIndraPassNpcsV1.Odesha), "set 13's Odesha, exact");
            Assert.AreEqual(AshenIndraPassNpcsV1.EndalaAt, Pool(AshenIndraPassNpcsV1.Endala), "set 13's Endala, exact");
            Assert.AreEqual(X.StormsBefore(), Scene(world), "set 13's scene, exact");
            using (var fresh = WildernessRuntimeTestHarness.Create(targetWorldMigration: AshenMissionTests.Previous(world, FixG)))
                Assert.AreEqual(Scene(fresh.World), Scene(world), "the Down's scene is a database's migrated only to before fix G");
            foreach (var (number, _) in X.MovedResistors)
            {
                var set13 = AshenGodOfStormsV1.ResistorsAt[number - 1];
                Assert.IsLessThan(0.01f, Vector3.Distance(new(set13.X, set13.Y, set13.Z), MarkerAt(AshenGodOfStormsV1.MissionId, (uint)number)), $"set 13's marker {number}");
            }
            Assert.IsLessThan(0.01f, Vector3.Distance(new((float)AshenIndraPassNpcsV1.EndalaAt.X, (float)AshenIndraPassNpcsV1.EndalaAt.Y, (float)AshenIndraPassNpcsV1.EndalaAt.Z),
                MarkerAt(AshenBondsOfBloodV1.MissionId, X.EndalaMarker)), "set 13's Endala marker");
            foreach (var (pool, _) in X.AttaPools)
                Assert.AreEqual((byte)X.AttaCountBefore, Count(pool), $"pool {pool}: three again");
            Assert.AreEqual(0, Evidence(), "no fix G evidence");
            Assert.AreEqual(others, Others(), "nothing else changes");

            world.GetService<IMigrator>().Migrate(FixG);
            world.ChangeTracker.Clear();
            Fixed("Up again");
            void Run(string sql) => world.Database.ExecuteSqlRaw(sql.Replace("{", "{{").Replace("}", "}}"));
            foreach (var sql in X.UpSql())
                Run(sql);
            world.ChangeTracker.Clear();
            Fixed("a second Up");
            // The guard: a pool holding another count or place is left alone both ways.
            var first = X.AttaPools[0].Pool;
            world.Database.ExecuteSqlRaw($"UPDATE spawnpool SET creature_1_min_count = 2, creature_1_max_count = 2 WHERE id = {first};");
            world.Database.ExecuteSqlRaw($"UPDATE spawnpool SET pos_x = 1 WHERE id = {AshenIndraPassNpcsV1.Odesha};");
            foreach (var sql in X.DownSql().Concat(X.UpSql()).Where(sql => sql.StartsWith("UPDATE spawnpool", StringComparison.Ordinal)))
                Run(sql);
            world.ChangeTracker.Clear();
            Assert.AreEqual((byte)2, Count(first), "a pool at another count is not touched");
            Assert.AreEqual(1.0, Pool(AshenIndraPassNpcsV1.Odesha).X, "an NPC pool at another place is not touched");
            world.Database.ExecuteSqlRaw($"UPDATE spawnpool SET creature_1_min_count = 1, creature_1_max_count = 1 WHERE id = {first};");
            world.Database.ExecuteSqlRaw(FormattableString.Invariant($"UPDATE spawnpool SET pos_x = {X.OdeshaAt.X} WHERE id = {AshenIndraPassNpcsV1.Odesha};"));
            world.ChangeTracker.Clear();
            Fixed("restored");
            Assert.AreEqual(others, Others(), "nothing else changes");
        }

        private static MapGeometry Geometry()
        {
            var dataDir = F.ClientData();
            using var world = WildernessRuntimeTestHarness.Create();
            using var meshes = new MeshLibrary(dataDir, F.MeshCsv());
            return MapGeometry.Load(Path.Combine(dataDir, "maps", world.World.MapInfoEntries.AsNoTracking()
                .Single(map => map.Id == AshenMissionDataV1.MapContextId).MapName), meshes, 1);
        }

        private static MissionIndicatorEntry Marker(DbContext world, uint mission, uint number) => world.Set<MissionIndicatorEntry>().AsNoTracking()
            .Single(entry => entry.MissionId == mission && entry.ContentRevision == AshenMissionDataV1.Revision && entry.IndicatorId == MissionIndicator.UnnamedFrom + number);

        private static string Scene(DbContext world) => world.Set<MissionSceneBindingEntry>().AsNoTracking()
            .Single(entry => entry.MissionId == AshenGodOfStormsV1.MissionId && entry.ContentRevision == AshenMissionDataV1.Revision).Bindings;
    }
}
