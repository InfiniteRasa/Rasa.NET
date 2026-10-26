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

    /// <summary>Tahrendra Base's rooms, prison blocks and levels (Tahrendra_base_regions, TahrendraBaseRegions).</summary>
    [TestClass]
    [DoNotParallelize]
    public class TahrendraBaseRegionsTests
    {
        private const string Migration = "20270622000000_Tahrendra_base_regions";
        private const string Previous = "20270621000000_Torden_mires_regions";

        private const int Ambient = 4, Name = 16;

        /// <summary>Tahrendra Base's regions, as the .map has them: priority and the settings they turn on.</summary>
        private static readonly Dictionary<uint, (int Priority, int Flags)> Tahrendra = new()
        {
            [1] = (1, 5), [24] = (2, 4), [17] = (3, 0), [19] = (4, 0), [20] = (5, 0), [22] = (6, 0), [23] = (7, 0), [2] = (8, 16), [3] = (9, 16),
            [4] = (10, 16), [5] = (11, 16), [6] = (12, 16), [7] = (13, 20), [8] = (14, 20), [9] = (15, 16), [10] = (16, 16), [11] = (17, 16),
            [12] = (18, 16), [13] = (19, 16), [14] = (20, 16), [15] = (21, 16), [16] = (22, 16),
        };

        [TestMethod]
        public void EachRoomHasItsName()
        {
            var (before, after) = Migrated();

            Assert.AreEqual(10u, Shown(before, new Vector3(-76f, 120.4f, 47f), Name, underground: false), "the power plant's circle named the Wormhole Security Station");
            Assert.AreEqual(2u, Shown(after, new Vector3(196f, 120.6f, 10.5f), Name, underground: false), "Prisoner Shipping & Supply where the instance puts you");
            Assert.AreEqual(2u, Shown(after, new Vector3(124f, 112.6f, 28f), Name, underground: false), "and its hall");
            Assert.AreEqual(3u, Shown(after, new Vector3(16f, 112.2f, 11f), Name, underground: false), "Prisoner Check Point");
            Assert.AreEqual(4u, Shown(after, new Vector3(35f, 120.6f, 52f), Name, underground: false), "Barracks, north dormitory");
            Assert.AreEqual(4u, Shown(after, new Vector3(53f, 120.6f, -28f), Name, underground: false), "Barracks, south dormitory");
            Assert.AreEqual(5u, Shown(after, new Vector3(24f, 120.6f, -71f), Name, underground: false), "Mess Hall");
            Assert.AreEqual(6u, Shown(after, new Vector3(-76f, 120.4f, 47f), Name, underground: false), "Wormhole Security Station");
            Assert.AreEqual(9u, Shown(after, new Vector3(-166f, 112.2f, 11f), Name, underground: false), "Wormhole");
            Assert.AreEqual(10u, Shown(after, new Vector3(-232f, 112.6f, -28f), Name, underground: false), "Wormhole Power Plant");
            Assert.AreEqual(7u, Shown(after, new Vector3(-134f, 144.2f, -64f), Name, underground: false), "Upper Prison Block Security Station");
            Assert.AreEqual(7u, Shown(after, new Vector3(-134f, 144.2f, -64f), Ambient, underground: false), "with the command ambience");
            Assert.AreEqual(8u, Shown(after, new Vector3(-4f, 80.2f, 72f), Name, underground: false), "Lower Prison Block Security Station");
        }

        [TestMethod]
        public void EachPrisonBlockLevelHasItsName()
        {
            var (_, after) = Migrated();

            Assert.AreEqual(11u, Shown(after, new Vector3(-76f, 112.6f, -60f), Name, underground: false), "Upper Level One");
            Assert.AreEqual(12u, Shown(after, new Vector3(-72f, 128.6f, -60f), Name, underground: false), "Upper Level Two");
            Assert.AreEqual(13u, Shown(after, new Vector3(-72f, 144.8f, -60f), Name, underground: false), "Upper Level Three");
            Assert.AreEqual(14u, Shown(after, new Vector3(-76f, 112.6f, 80f), Name, underground: false), "Lower Level One");
            Assert.AreEqual(15u, Shown(after, new Vector3(-76f, 96.7f, 70f), Name, underground: false), "Lower Level Two");
            Assert.AreEqual(16u, Shown(after, new Vector3(-76f, 80.7f, 70f), Name, underground: false), "Lower Level Three");
        }

        [TestMethod]
        public void EachLevelHasItsMinimapLayer()
        {
            var (before, after) = Migrated();
            var lowerLevelOne = new Vector3(-76f, 112.6f, 80f);

            Assert.AreEqual(23u, Layer(before, lowerLevelOne), "the lower block's last layer covered all its levels");
            Assert.AreEqual(17u, Layer(after, lowerLevelOne), "Level One of the lower block");
            Assert.AreEqual(17u, Layer(after, new Vector3(-76f, 112.6f, -60f)), "and of the upper");
            Assert.AreEqual(19u, Layer(after, new Vector3(-72f, 128.6f, -60f)), "upper Level Two");
            Assert.AreEqual(20u, Layer(after, new Vector3(-72f, 144.8f, -60f)), "upper Level Three");
            Assert.AreEqual(20u, Layer(after, new Vector3(-134f, 144.2f, -64f)), "with its security station");
            Assert.AreEqual(22u, Layer(after, new Vector3(-76f, 96.7f, 70f)), "lower Level Two");
            Assert.AreEqual(23u, Layer(after, new Vector3(-76f, 80.7f, 70f)), "lower Level Three");
            Assert.AreEqual(23u, Layer(after, new Vector3(-4f, 80.2f, 72f)), "with its security station");
            Assert.AreEqual(24u, Layer(after, new Vector3(-76f, 120.4f, 47f)), "the Wormhole Security Station room above");
            Assert.AreEqual(24u, Shown(after, new Vector3(-76f, 120.4f, 47f), Ambient, underground: false), "with its command ambience");
            Assert.AreEqual(0u, Shown(after, lowerLevelOne, Ambient, underground: false), "which no longer reaches the cells below");
        }

        /// <summary>The highest-priority minimap layer (a region that sets nothing but its own minimap) here, 0 for the map's own.</summary>
        private static uint Layer(List<MapRegion> volumes, Vector3 at) =>
            volumes.Where(v => v.Enabled && Layers.Contains(v.RegionId) && v.Contains(at, false))
                .OrderByDescending(v => Tahrendra[v.RegionId].Priority).Select(v => v.RegionId).FirstOrDefault();

        private static readonly uint[] Layers = { 17, 19, 20, 22, 23, 24 };

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
            Assert.AreEqual(TahrendraBaseRegions.Volumes.Length + TahrendraBaseRegions.Recuts.Length, once.Except(before).Count(), "the new rows and the recut ones");

            foreach (var sql in TahrendraBaseRegions.Up())
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

            foreach (var sql in TahrendraBaseRegions.Up())
                StringAssert.Contains(migrator.GenerateScript(Previous, Migration), sql);
            foreach (var sql in TahrendraBaseRegions.Down())
                StringAssert.Contains(migrator.GenerateScript(Migration, Previous), sql);
        }

        /// <summary>The highest-priority region here that turns the setting on, 0 for none (the map's own).</summary>
        private static uint Shown(List<MapRegion> volumes, Vector3 at, int flag, bool underground) =>
            volumes.Where(v => v.Enabled && Tahrendra.TryGetValue(v.RegionId, out var r) && (r.Flags & flag) != 0 && v.Contains(at, underground))
                .OrderByDescending(v => Tahrendra[v.RegionId].Priority).Select(v => v.RegionId).FirstOrDefault();

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == TahrendraBaseRegions.MapContextId)
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT map_context_id || ' ' || region_id || ' ' || shape || ' ' || printf('%.1f %.1f %.1f %.1f %.1f %.1f %.1f %.1f', pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y) || ' ' || underground || ' ' || enabled || ' ' || comment AS Value " +
                    "FROM map_region")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
