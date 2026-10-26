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

    /// <summary>The Brann Communications Tower's levels and control room (Comm_tower_regions, CommTowerRegions).</summary>
    [TestClass]
    [DoNotParallelize]
    public class CommTowerRegionsTests
    {
        private const string Migration = "20270624000000_Comm_tower_regions";
        private const string Previous = "20270623000000_Phanin_research_regions";

        private const int Sky = 1, Music = 8, Name = 16;

        /// <summary>The Comm Tower's regions, as the .map has them: priority and the settings they turn on.</summary>
        private static readonly Dictionary<uint, (int Priority, int Flags)> Tower = new()
        {
            [11] = (1, 13), [6] = (2, 19), [7] = (3, 19), [8] = (4, 19), [9] = (5, 19), [10] = (6, 3), [4] = (7, 27),
        };

        [TestMethod]
        public void EachLevelHasItsName()
        {
            var (before, after) = Migrated();
            var levelOne = new Vector3(-10f, 224.6f, 50f);

            Assert.AreEqual(4u, Shown(before, levelOne, Name, underground: true), "the control room's rectangle named the floors under it");
            Assert.AreEqual(8u, Shown(before, new Vector3(-40f, 224.6f, 80f), Name, underground: true), "and Level 3's named Level 1");
            Assert.AreEqual(6u, Shown(after, levelOne, Name, underground: true), "Level 1 under the control room");
            Assert.AreEqual(6u, Shown(after, new Vector3(16f, 224.9f, 180f), Name, underground: true), "Level 1, north rooms");
            Assert.AreEqual(7u, Shown(after, new Vector3(98.5f, 247.2f, -84f), Name, underground: true), "Level 2 where the instance puts you");
            Assert.AreEqual(7u, Shown(after, new Vector3(8f, 248.7f, 90f), Name, underground: true), "Level 2");
            Assert.AreEqual(7u, Shown(after, new Vector3(-92f, 246.5f, 30f), Name, underground: true), "Level 2, west rooms");
            Assert.AreEqual(8u, Shown(after, new Vector3(8f, 261.8f, -48f), Name, underground: true), "Level 3");
            Assert.AreEqual(8u, Shown(after, new Vector3(-30f, 264.6f, 100f), Name, underground: true), "Level 3, dormitories");
            Assert.AreEqual(9u, Shown(after, new Vector3(48f, 285.7f, 20f), Name, underground: true), "Level 4, top room");
            Assert.AreEqual(9u, Shown(after, new Vector3(-76f, 272.6f, -56f), Name, underground: true), "Level 4, dormitory");
        }

        [TestMethod]
        public void TheControlRoomIsOnTheSummit()
        {
            var (before, after) = Migrated();
            var room = new Vector3(-10f, 402f, 41f);

            Assert.AreEqual(0u, Shown(before, room, Name, underground: false), "the room had no name");
            Assert.AreEqual(4u, Shown(after, room, Name, underground: false), "Radio Control Room");
            Assert.AreEqual(4u, Shown(after, room, Sky, underground: false), "with its sky");
            Assert.AreEqual(4u, Shown(after, room, Music, underground: false), "and music");
            Assert.AreEqual(4u, Layer(after, room, underground: false), "and minimap");
        }

        [TestMethod]
        public void TheFacilityHasItsInteriorSky()
        {
            var (_, after) = Migrated();

            Assert.AreEqual(10u, Shown(after, new Vector3(8f, 248.7f, 90f), Sky, underground: true), "region 10 over the levels");
            Assert.AreEqual(4u, Shown(after, new Vector3(-10f, 402f, 41f), Sky, underground: false), "but not the control room");
        }

        [TestMethod]
        public void EachLevelHasItsMinimap()
        {
            var (before, after) = Migrated();

            Assert.AreEqual(4u, Layer(before, new Vector3(-10f, 224.6f, 50f)), "the control room's minimap showed under it");
            Assert.AreEqual(6u, Layer(after, new Vector3(-10f, 224.6f, 50f)), "Level 1");
            Assert.AreEqual(7u, Layer(after, new Vector3(8f, 248.7f, 90f)), "Level 2");
            Assert.AreEqual(8u, Layer(after, new Vector3(8f, 261.8f, -48f)), "Level 3");
        }

        /// <summary>The highest-priority region with a minimap of its own here, 0 for the map's own.</summary>
        private static uint Layer(List<MapRegion> volumes, Vector3 at, bool underground = true) =>
            volumes.Where(v => v.Enabled && Layers.Contains(v.RegionId) && v.Contains(at, underground))
                .OrderByDescending(v => Tower[v.RegionId].Priority).Select(v => v.RegionId).FirstOrDefault();

        private static readonly uint[] Layers = { 4, 6, 7, 8 };

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
            Assert.AreEqual(CommTowerRegions.Volumes.Length + CommTowerRegions.Recuts.Length, once.Except(before).Count(), "the new rows and the recut ones");

            foreach (var sql in CommTowerRegions.Up())
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

            foreach (var sql in CommTowerRegions.Up())
                StringAssert.Contains(migrator.GenerateScript(Previous, Migration), sql);
            foreach (var sql in CommTowerRegions.Down())
                StringAssert.Contains(migrator.GenerateScript(Migration, Previous), sql);
        }

        /// <summary>The highest-priority region here that turns the setting on, 0 for none (the map's own).</summary>
        private static uint Shown(List<MapRegion> volumes, Vector3 at, int flag, bool underground) =>
            volumes.Where(v => v.Enabled && Tower.TryGetValue(v.RegionId, out var r) && (r.Flags & flag) != 0 && v.Contains(at, underground))
                .OrderByDescending(v => Tower[v.RegionId].Priority).Select(v => v.RegionId).FirstOrDefault();

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == CommTowerRegions.MapContextId)
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT map_context_id || ' ' || region_id || ' ' || shape || ' ' || printf('%.1f %.1f %.1f %.1f %.1f %.1f %.1f %.1f', pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y) || ' ' || underground || ' ' || enabled || ' ' || comment AS Value " +
                    "FROM map_region")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
