using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Services.Preloader;
    using Rasa.Structures;
    using Rasa.Test.Database;

    /// <summary>Purgas Station's three levels (Purgas_station_regions, PurgasStationRegions).</summary>
    [TestClass]
    [DoNotParallelize]
    public class PurgasStationRegionsTests
    {
        private const string Migration = "20270628000000_Purgas_station_regions";
        private const string Previous = "20270627000000_Hellas_atta_regions";

        private const int Sky = 1, Ambient = 4, Name = 16;

        /// <summary>Purgas Station's regions, as the .map has them: priority and the settings they turn on.</summary>
        private static readonly Dictionary<uint, (int Priority, int Flags)> Station = new()
        {
            [1] = (1, 13), [6] = (2, 16), [22] = (3, 0), [23] = (4, 0), [24] = (5, 0), [9] = (6, 23), [10] = (7, 19), [8] = (8, 23), [11] = (9, 23),
            [7] = (10, 23), [12] = (11, 19), [13] = (12, 19), [15] = (13, 23), [16] = (14, 23), [14] = (15, 23), [17] = (16, 23), [18] = (17, 19),
            [20] = (18, 23), [21] = (19, 23), [19] = (20, 23),
        };

        [TestMethod]
        public void LevelOneIsTheTopFloor()
        {
            var (before, after) = Migrated();
            var overlook = new Vector3(152.25f, 73.8f, 128.5f);

            Assert.AreEqual(19u, Shown(before, overlook, Name, underground: false), "Level 01's control room was Level 03's");
            Assert.AreEqual(7u, Shown(after, overlook, Name, underground: false), "Level 01: Control Room, at its hospital");
            Assert.AreEqual(7u, Shown(after, overlook, Sky, underground: false), "with its sky");
            Assert.AreEqual(0u, Shown(after, new Vector3(87.9f, 65.9f, 215.4f), Name, underground: false), "the Entrance Hall, the map's own name");
            Assert.AreEqual(6u, Shown(after, new Vector3(330f, 64.3f, 125f), Name, underground: false), "Juggernaut Bay");
            Assert.AreEqual(10u, Shown(after, new Vector3(-130f, 68.6f, 0f), Name, underground: false), "Level 01: Main Corridor");
            Assert.AreEqual(9u, Shown(after, new Vector3(-104f, 65.2f, 24f), Name, underground: false), "Level 01: Service Corridor");
            Assert.AreEqual(21u, Shown(before, new Vector3(16f, 83.1f, 0f), Name, underground: false), "the reactor's top ring was Level 03's");
            Assert.AreEqual(11u, Shown(after, new Vector3(16f, 83.1f, 0f), Name, underground: false), "Level 01: Reactor Core");
        }

        [TestMethod]
        public void LevelTwoIsTheMiddleFloor()
        {
            var (before, after) = Migrated();

            Assert.AreEqual(21u, Shown(before, new Vector3(16f, 49.1f, 16f), Name, underground: false), "the middle ring was Level 03's");
            Assert.AreEqual(16u, Shown(after, new Vector3(16f, 49.1f, 16f), Name, underground: false), "Level 02: Reactor Core");
            Assert.AreEqual(16u, Shown(after, new Vector3(32f, 31f, 32f), Name, underground: false), "and its lower walkway");
            Assert.AreEqual(0u, Shown(before, new Vector3(0f, 31.8f, 88f), Name, underground: false), "its control room had no name");
            Assert.AreEqual(14u, Shown(after, new Vector3(0f, 31.8f, 88f), Name, underground: false), "Level 02: Control Room");
            Assert.AreEqual(13u, Shown(after, new Vector3(130f, 36.6f, 48f), Name, underground: false), "Level 02: Main Corridor");
            Assert.AreEqual(15u, Shown(after, new Vector3(-240f, 31.8f, -144f), Ambient, underground: false), "Level 02: Power Chamber");
            Assert.AreEqual(12u, Shown(after, new Vector3(-215f, 49.1f, 10f), Name, underground: false), "Level 02: Service Corridor");
            Assert.AreEqual(12u, Shown(after, new Vector3(-285f, 18.6f, 75f), Name, underground: false), "a fuel pool pit of its west rooms");
        }

        [TestMethod]
        public void LevelThreeIsTheBottomFloor()
        {
            var (_, after) = Migrated();

            Assert.AreEqual(21u, Shown(after, new Vector3(-16f, 1.3f, 16f), Name, underground: false), "Level 03: Reactor Core");
            Assert.AreEqual(19u, Shown(after, new Vector3(48f, 7.8f, -152f), Name, underground: false), "Level 03: Control Room");
            Assert.AreEqual(20u, Shown(after, new Vector3(-160f, 8.4f, -216f), Name, underground: false), "the west fuel basins");
            Assert.AreEqual(17u, Shown(after, new Vector3(-40f, 8.2f, -216f), Name, underground: false), "the east fuel basin, clear of the corridor");
            Assert.AreEqual(17u, Shown(after, new Vector3(-40f, 8.2f, -216f), Ambient, underground: false), "with its ambience");
            Assert.AreEqual(18u, Shown(after, new Vector3(-90f, 11.8f, -200f), Name, underground: false), "Level 03: Main Corridor between them");
            Assert.AreEqual(18u, Shown(after, new Vector3(80f, 12f, -60f), Name, underground: false), "and east");
        }

        private static (List<MapRegion> Before, List<MapRegion> After) Migrated()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();
            migrator.Migrate(Previous);
            var before = Volumes(database);
            migrator.Migrate(Migration);
            return (before, Volumes(database));
        }

        [TestMethod]
        public void TheUpRunsTwiceAndTheDownTakesThemOut()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            var before = Snapshot(database);

            migrator.Migrate(Migration);
            var once = Snapshot(database);
            Assert.AreEqual(PurgasStationRegions.Volumes.Length + PurgasStationRegions.Recuts.Length, once.Except(before).Count(), "the new rows and the recut ones");

            foreach (var sql in PurgasStationRegions.Up())
                Assert.AreEqual(0, database.Context.Database.ExecuteSqlRaw(sql.Replace("{", "{{").Replace("}", "}}")), sql);
            CollectionAssert.AreEqual(once, Snapshot(database), "twice");

            migrator.Migrate(Previous);
            CollectionAssert.AreEqual(before, Snapshot(database), "Down");
        }

        [TestMethod]
        public void OnMySqlTheRowsGoInAndComeOut()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();

            foreach (var sql in PurgasStationRegions.Up())
                StringAssert.Contains(migrator.GenerateScript(Previous, Migration), sql);
            foreach (var sql in PurgasStationRegions.Down())
                StringAssert.Contains(migrator.GenerateScript(Migration, Previous), sql);
        }

        /// <summary>The highest-priority region here that turns the setting on, 0 for none (the map's own).</summary>
        private static uint Shown(List<MapRegion> volumes, Vector3 at, int flag, bool underground) =>
            volumes.Where(v => v.Enabled && Station.TryGetValue(v.RegionId, out var r) && (r.Flags & flag) != 0 && v.Contains(at, underground))
                .OrderByDescending(v => Station[v.RegionId].Priority).Select(v => v.RegionId).FirstOrDefault();

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == PurgasStationRegions.MapContextId)
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT map_context_id || ' ' || region_id || ' ' || shape || ' ' || printf('%.1f %.1f %.1f %.1f %.1f %.1f %.1f %.1f', pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y) || ' ' || underground || ' ' || enabled || ' ' || comment AS Value " +
                    "FROM map_region")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
