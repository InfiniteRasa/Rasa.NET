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

    /// <summary>Mount Hellas Outpost and Atta Territory on Torden Plains (Hellas_atta_regions, HellasAttaRegions).</summary>
    [TestClass]
    [DoNotParallelize]
    public class HellasAttaRegionsTests
    {
        private const string Migration = "20270627000000_Hellas_atta_regions";
        private const string Previous = "20270626000000_Eloh_vale_regions";

        private const int Sky = 1, Ambient = 4, Name = 16;

        /// <summary>Torden Plains' regions, as the .map has them: priority and the settings they turn on.</summary>
        private static readonly Dictionary<uint, (int Priority, int Flags)> Plains = new()
        {
            [1] = (1, 13), [8] = (2, 5), [4] = (3, 29), [14] = (4, 21), [20] = (5, 29), [15] = (6, 17), [17] = (7, 16), [26] = (8, 17),
            [19] = (9, 17), [21] = (10, 31), [18] = (11, 25), [12] = (12, 7), [2] = (13, 29), [3] = (14, 21), [7] = (15, 31), [13] = (16, 29),
            [5] = (17, 21), [32] = (18, 3), [25] = (19, 22), [6] = (20, 23), [9] = (21, 31), [22] = (22, 0), [23] = (23, 0), [24] = (24, 0),
            [10] = (25, 17), [11] = (26, 17), [16] = (27, 1), [31] = (28, 16), [27] = (29, 16), [29] = (30, 16), [34] = (31, 16), [33] = (32, 0),
        };

        [TestMethod]
        public void TheHallKeepsItsInteriorUnderTheMountain()
        {
            var (before, after) = Migrated();
            var hall = new Vector3(-560f, 438.3f, 449f);
            var mountain = new Vector3(-504f, 473.4f, 428f);

            Assert.AreEqual(7u, Shown(after, hall, Sky, underground: true), "the hall's interior sky");
            Assert.AreEqual(7u, Shown(after, hall, Name, underground: true), "and its name");
            Assert.AreEqual(7u, Shown(after, new Vector3(-480f, 442.6f, 449f), Name, underground: true), "the corridor to the entrance");
            Assert.AreEqual(7u, Layer(after, hall), "and its minimap");
            Assert.AreEqual(7u, Shown(before, mountain, Sky, underground: false), "the interior sky was on the mountain over it");
            Assert.AreEqual(7u, Layer(before, mountain, underground: false), "and its minimap");
            Assert.AreEqual(3u, Shown(after, mountain, Sky, underground: false), "now the outpost's outdoor sky, by the bunkers");
            Assert.AreEqual(0u, Layer(after, mountain, underground: false), "and the map's own minimap");
        }

        [TestMethod]
        public void TheOutpostOutsideHasItsName()
        {
            var (before, after) = Migrated();
            var pad = new Vector3(-385f, 441.9f, 410f);

            Assert.AreEqual(0u, Shown(before, pad, Name, underground: false), "the yard had no name");
            Assert.AreEqual(3u, Shown(after, pad, Name, underground: false), "Mount Hellas Outpost by the dropship pad");
            Assert.AreEqual(3u, Shown(after, pad, Ambient, underground: false), "with its distant battle");
            Assert.AreEqual(3u, Shown(after, new Vector3(-368f, 452.6f, 490f), Name, underground: false), "on the north-east watchtower");
            Assert.AreEqual(3u, Shown(after, new Vector3(-477f, 473f, 449f), Name, underground: false), "on the watchtower over the entrance");
            Assert.AreEqual(0u, Shown(after, new Vector3(-279f, 431.5f, 469f), Name, underground: false), "but not the trench line east of it");
        }

        [TestMethod]
        public void AttaTerritoryIsWhereTheBridgesAre()
        {
            var (before, after) = Migrated();
            var hiveDoor = new Vector3(-505f, 432.9f, -62f);

            Assert.AreEqual(0u, Shown(before, hiveDoor, Name, underground: true), "it had no volume");
            Assert.AreEqual(14u, Shown(after, hiveDoor, Name, underground: true), "the cave down to the hive entrance");
            Assert.AreEqual(14u, Shown(after, new Vector3(-420f, 433.9f, -100f), Ambient, underground: false), "the queen's growths, with the Atta ambience");
            Assert.AreEqual(14u, Shown(after, new Vector3(-338f, 443.2f, -96f), Name, underground: false), "the east bridges");
            Assert.AreEqual(14u, Shown(after, new Vector3(-449f, 460.1f, 126f), Name, underground: false), "the north arm");
            Assert.AreEqual(14u, Shown(after, new Vector3(-475f, 443.1f, -333f), Name, underground: false), "the south ramp");
            Assert.AreEqual(14u, Shown(after, new Vector3(-778f, 452.2f, -66f), Name, underground: false), "the west ramp");
            Assert.AreEqual(14u, Shown(after, new Vector3(-206.5f, 434.9f, 84.5f), Name, underground: false), "at the Atta sign");
            Assert.AreEqual(0u, Shown(after, new Vector3(-150f, 430.1f, 60f), Name, underground: false), "not the crossroads east of it");
            Assert.AreEqual(0u, Shown(after, new Vector3(-856f, 432.8f, -279f), Name, underground: false), "nor the Bane Drill Control Outpost");
        }

        /// <summary>The highest-priority region with a minimap of its own here, 0 for the map's own.</summary>
        private static uint Layer(List<MapRegion> volumes, Vector3 at, bool underground = true) =>
            volumes.Where(v => v.Enabled && Layers.Contains(v.RegionId) && v.Contains(at, underground))
                .OrderByDescending(v => Plains[v.RegionId].Priority).Select(v => v.RegionId).FirstOrDefault();

        private static readonly uint[] Layers = { 7, 12, 22, 23, 24 };

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
            Assert.AreEqual(HellasAttaRegions.Volumes.Length + HellasAttaRegions.Recuts.Length, once.Except(before).Count(), "the new rows and the recut ones");

            foreach (var sql in HellasAttaRegions.Up())
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

            foreach (var sql in HellasAttaRegions.Up())
                StringAssert.Contains(migrator.GenerateScript(Previous, Migration), sql);
            foreach (var sql in HellasAttaRegions.Down())
                StringAssert.Contains(migrator.GenerateScript(Migration, Previous), sql);
        }

        /// <summary>The highest-priority region here that turns the setting on, 0 for none (the map's own).</summary>
        private static uint Shown(List<MapRegion> volumes, Vector3 at, int flag, bool underground) =>
            volumes.Where(v => v.Enabled && Plains.TryGetValue(v.RegionId, out var r) && (r.Flags & flag) != 0 && v.Contains(at, underground))
                .OrderByDescending(v => Plains[v.RegionId].Priority).Select(v => v.RegionId).FirstOrDefault();

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == HellasAttaRegions.MapContextId)
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT map_context_id || ' ' || region_id || ' ' || shape || ' ' || printf('%.1f %.1f %.1f %.1f %.1f %.1f %.1f %.1f', pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y) || ' ' || underground || ' ' || enabled || ' ' || comment AS Value " +
                    "FROM map_region")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
