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

    /// <summary>Purgas Station's corridor ends left without a region (Purgas_station_gap_regions, PurgasStationGapRegions).</summary>
    [TestClass]
    [DoNotParallelize]
    public class PurgasStationGapRegionsTests
    {
        private const string Migration = "20270718000000_Purgas_station_gap_regions";
        private const string Previous = "20270717000000_Fault_lever_sector_regions";

        private const int Ambient = 4, Name = 16;

        /// <summary>Purgas Station's regions, as the .map has them: priority and the settings they turn on.</summary>
        private static readonly Dictionary<uint, (int Priority, int Flags)> Station = new()
        {
            [1] = (1, 13), [6] = (2, 16), [22] = (3, 0), [23] = (4, 0), [24] = (5, 0), [9] = (6, 23), [10] = (7, 19), [8] = (8, 23), [11] = (9, 23),
            [7] = (10, 23), [12] = (11, 19), [13] = (12, 19), [15] = (13, 23), [16] = (14, 23), [14] = (15, 23), [17] = (16, 23), [18] = (17, 19),
            [20] = (18, 23), [21] = (19, 23), [19] = (20, 23),
        };

        [TestMethod]
        public void EachCommentFitsTheMySqlColumn()
        {
            foreach (var volume in PurgasStationGapRegions.Volumes)
                Assert.IsLessThanOrEqualTo(PurgasStationGapRegions.CommentLength, volume.Comment.Length, volume.Comment);
        }

        [TestMethod]
        public void LevelThreesBendAndServiceTubeAreItsMainCorridor()
        {
            var (before, after) = Migrated();
            var places = new[]
            {
                new Vector3(-7.4f, 9.8f, -213.1f), new Vector3(-13f, 11.2f, -220.9f), new Vector3(0.1f, 11.8f, -204.5f),
                new Vector3(24f, 16.8f, -192.2f), new Vector3(13.6f, 17.4f, -194.7f), new Vector3(37.4f, 10.9f, -191.9f),
            };

            foreach (var at in places)
            {
                Assert.AreEqual(0u, Shown(before, at, Name, underground: false), $"{at} was the Level 01 entrance hall");
                Assert.AreEqual(18u, Shown(after, at, Name, underground: false), $"{at}: Level 03: Main Corridor");
            }

            Assert.AreEqual(17u, Shown(after, new Vector3(-40f, 8.2f, -224f), Name, underground: false), "the east basin room keeps its Power Chamber");
            Assert.AreEqual(19u, Shown(after, new Vector3(48f, 7.8f, -152f), Name, underground: false), "the control room keeps its own");
        }

        [TestMethod]
        public void TheRampsTopIsALevelOneServiceCorridor()
        {
            var (before, after) = Migrated();
            var top = new Vector3(-168.3f, 64.5f, 17f);

            Assert.AreEqual(0u, Shown(before, top, Name, underground: false), "the top of the ramp was the Level 01 entrance hall");
            Assert.AreEqual(9u, Shown(after, top, Name, underground: false), "Level 01: Service Corridor");
            Assert.AreEqual(9u, Shown(after, top, Ambient, underground: false), "with its tunnel sound");
            Assert.AreEqual(12u, Shown(after, new Vector3(-168f, 53.9f, 40f), Name, underground: false), "lower down the ramp is Level 02's");
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
            Assert.AreEqual(PurgasStationGapRegions.Volumes.Length, once.Except(before).Count(), "the new rows");

            foreach (var sql in PurgasStationGapRegions.Up())
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

            foreach (var sql in PurgasStationGapRegions.Up())
                StringAssert.Contains(migrator.GenerateScript(Previous, Migration), sql);
            foreach (var sql in PurgasStationGapRegions.Down())
                StringAssert.Contains(migrator.GenerateScript(Migration, Previous), sql);
        }

        /// <summary>The highest-priority region here that turns the setting on, 0 for none (the map's own).</summary>
        private static uint Shown(List<MapRegion> volumes, Vector3 at, int flag, bool underground) =>
            volumes.Where(v => v.Enabled && Station.TryGetValue(v.RegionId, out var r) && (r.Flags & flag) != 0 && v.Contains(at, underground))
                .OrderByDescending(v => Station[v.RegionId].Priority).Select(v => v.RegionId).FirstOrDefault();

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == PurgasStationGapRegions.MapContextId)
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT map_context_id || ' ' || region_id || ' ' || shape || ' ' || printf('%.1f %.1f %.1f %.1f %.1f %.1f %.1f %.1f', pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y) || ' ' || underground || ' ' || enabled || ' ' || comment AS Value " +
                    "FROM map_region WHERE map_context_id = 1806")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
