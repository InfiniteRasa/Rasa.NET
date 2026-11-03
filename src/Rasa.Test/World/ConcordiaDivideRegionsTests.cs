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

    /// <summary>Concordia Divide's Foreas Base interiors, Eloh corridors, caves and Purgas tunnel (Concordia_divide_regions, ConcordiaDivideRegions).</summary>
    [TestClass]
    [DoNotParallelize]
    public class ConcordiaDivideRegionsTests
    {
        private const string Migration = "20270712000000_Concordia_divide_regions";
        private const string Previous = "20270704000000_Guardian_prominence_regions";

        private const int Sky = 1, Env = 2, Ambient = 4, Music = 8, Name = 16;

        /// <summary>Concordia Divide's regions, as the .map has them: priority and the settings they turn on.</summary>
        private static readonly Dictionary<uint, (int Priority, int Flags)> Divide = new()
        {
            [22] = (1, 13), [40] = (2, 1), [23] = (3, 28), [10] = (4, 28), [20] = (5, 28), [21] = (6, 28), [30] = (7, 28), [15] = (8, 4),
            [16] = (9, 7), [17] = (10, 4), [18] = (11, 4), [19] = (12, 4), [14] = (13, 3), [13] = (14, 15), [11] = (15, 15), [32] = (16, 7),
            [12] = (17, 31), [33] = (18, 7), [5] = (19, 12), [6] = (20, 12), [4] = (21, 12), [1] = (22, 28), [2] = (23, 4), [27] = (24, 28),
            [7] = (25, 7), [3] = (26, 15), [25] = (27, 28), [28] = (28, 12), [29] = (29, 15), [8] = (30, 15), [9] = (31, 15), [36] = (32, 11),
            [31] = (33, 7), [34] = (34, 15), [37] = (35, 4), [35] = (36, 3), [24] = (37, 28),
        };

        [TestMethod]
        public void ForeasBasesBuildingsAreLitIndoors()
        {
            var (before, after) = Migrated();
            var hospital = new Vector3(5.8f, 124.2f, 532f);
            var headquarters = new Vector3(-38.9f, 118.3f, 447.8f);
            var tavern = new Vector3(63.6f, 125.1f, 535.9f);
            var armor = new Vector3(12.8f, 116.6f, 486.5f);

            Assert.AreEqual(0u, Shown(before, hospital, Sky, underground: false), "the hospital had the open sky");
            Assert.AreEqual(13u, Shown(after, hospital, Sky, underground: false), "the hospital's interior");
            Assert.AreEqual(13u, Shown(after, hospital, Ambient, underground: false), "and its sound");
            Assert.AreEqual(10u, Shown(after, hospital, Name, underground: false), "still Foreas Base");
            Assert.AreEqual(33u, Shown(after, headquarters, Sky, underground: false), "the headquarters");
            Assert.AreEqual(33u, Shown(after, headquarters, Ambient, underground: false), "with the command room sound");
            Assert.AreEqual(10u, Shown(before, tavern, Name, underground: false), "the tavern was Foreas Base");
            Assert.AreEqual(12u, Shown(after, tavern, Name, underground: false), "The Swamp");
            Assert.AreEqual(12u, Shown(after, tavern, Ambient, underground: false), "with the bar");
            Assert.AreEqual(14u, Shown(after, armor, Sky, underground: false), "the armor shop is lit indoors");
            Assert.AreEqual(18u, Shown(after, armor, Ambient, underground: false), "with the general store sound");
            Assert.AreEqual(18u, Shown(after, new Vector3(-92.9f, 116.7f, 486.9f), Ambient, underground: false), "and the weapon shop");
            Assert.AreEqual(0u, Shown(after, new Vector3(-33.6f, 116.2f, 564.4f), Sky, underground: false), "not the base's yard by its waypoint");
            Assert.AreEqual(37u, Shown(after, new Vector3(-44.2f, 69f, 502.4f), Ambient, underground: true), "the lower level's office");
            Assert.AreEqual(34u, Shown(after, new Vector3(-44.2f, 69f, 502.4f), Sky, underground: true), "under the lower level's sky");
        }

        [TestMethod]
        public void TheElohCorridorsAndTheCavesAreLitAsSuch()
        {
            var (before, after) = Migrated();

            foreach (var at in new[] { new Vector3(-796.2f, 121.9f, 463.9f), new Vector3(357.2f, 87f, 614.6f), new Vector3(840.3f, 156.2f, 515.4f) })
            {
                Assert.AreEqual(0u, Shown(before, at, Sky, underground: true), $"{at} had the open sky");
                Assert.AreEqual(36u, Shown(after, at, Sky, underground: true), $"{at} is an Eloh corridor");
            }

            foreach (var at in new[]
            {
                new Vector3(-981.2f, 181.2f, 668.1f), new Vector3(472.7f, 177.9f, 1204.5f), new Vector3(793.1f, 137.1f, -491.2f), new Vector3(831f, 81.8f, -747.6f),
                new Vector3(656.3f, 173.5f, -123.4f), new Vector3(-788.3f, 121.3f, 527.9f), new Vector3(372.5f, 92.9f, 674.3f), new Vector3(776f, 162.5f, 497.6f),
                new Vector3(722.5f, 82.8f, 9.9f),
            })
            {
                Assert.AreEqual(0u, Shown(before, at, Sky, underground: true), $"{at} had the open sky");
                Assert.AreEqual(9u, Shown(after, at, Sky, underground: true), $"{at} is a cave");
            }

            Assert.AreEqual(0u, Shown(after, new Vector3(776f, 162.5f, 497.6f), Sky, underground: false), "not where the cave is open");
        }

        [TestMethod]
        public void TheTunnelToPurgasStationIsTheBaneInterior()
        {
            var (before, after) = Migrated();
            var tunnel = new Vector3(-868.1f, 121.8f, -890.4f);

            Assert.AreEqual(0u, Shown(before, tunnel, Sky, underground: true), "the tunnel had the open sky");
            Assert.AreEqual(8u, Shown(after, tunnel, Sky, underground: true), "the Bane interior");
            Assert.AreEqual(8u, Shown(after, tunnel, Music, underground: true), "with the Bane music");
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
            Assert.AreEqual(ConcordiaDivideRegions.Volumes.Length, once.Except(before).Count(), "the new rows");

            foreach (var sql in ConcordiaDivideRegions.Up())
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

            foreach (var sql in ConcordiaDivideRegions.Up())
                StringAssert.Contains(migrator.GenerateScript(Previous, Migration), sql);
            foreach (var sql in ConcordiaDivideRegions.Down())
                StringAssert.Contains(migrator.GenerateScript(Migration, Previous), sql);
        }

        /// <summary>The highest-priority region here that turns the setting on, 0 for none (the map's own).</summary>
        private static uint Shown(List<MapRegion> volumes, Vector3 at, int flag, bool underground) =>
            volumes.Where(v => v.Enabled && Divide.TryGetValue(v.RegionId, out var r) && (r.Flags & flag) != 0 && v.Contains(at, underground))
                .OrderByDescending(v => Divide[v.RegionId].Priority).Select(v => v.RegionId).FirstOrDefault();

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == ConcordiaDivideRegions.MapContextId)
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT map_context_id || ' ' || region_id || ' ' || shape || ' ' || printf('%.1f %.1f %.1f %.1f %.1f %.1f %.1f %.1f', pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y) || ' ' || underground || ' ' || enabled || ' ' || comment AS Value " +
                    "FROM map_region WHERE map_context_id = 1148")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
