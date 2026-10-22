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

    /// <summary>The four Eloh Temples (Eloh_temples_regions, ElohTemplesRegions).</summary>
    [TestClass]
    [DoNotParallelize]
    public class ElohTemplesRegionsTests
    {
        private const string Migration = "20270611000000_Eloh_temples_regions";
        private const string Previous = "20270610000000_Energy_weapon_center_regions";

        private const int Sky = 1, Env = 2, Ambient = 4, Music = 8, Name = 16;

        /// <summary>The temples' regions, as the .map has them: priority and the settings they turn on.</summary>
        private static readonly Dictionary<uint, (int Priority, int Flags)> Temples = new()
        {
            [2] = (1, 13), [11] = (2, 19), [7] = (3, 19), [8] = (4, 19), [9] = (5, 19), [10] = (6, 19), [3] = (7, 19), [4] = (8, 19), [5] = (9, 19),
            [6] = (10, 19),
        };

        [TestMethod]
        public void TheCaveSkyWasInsideTheTemplesAndIsNowAtTheirMouths()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();
            var ragingEntry = new Vector3(-512f, 24.2f, -204f);

            migrator.Migrate(Previous);
            var before = Volumes(database);
            Assert.AreEqual(7u, Shown(before, ragingEntry, Sky, underground: false), "the Raging Patriarch's entry room was under the cave sky");
            Assert.AreEqual(7u, Shown(before, new Vector3(-512f, 8f, -100f), Sky, underground: false), "and its first body room");

            migrator.Migrate(Migration);
            var after = Volumes(database);
            Assert.AreEqual(3u, Shown(after, ragingEntry, Sky, underground: false), "the entry room is the temple's");
            Assert.AreEqual(7u, Shown(after, new Vector3(-512f, 24.3f, -246f), Sky, underground: false), "the cave mouth keeps the cave sky");
            Assert.AreEqual(8u, Shown(after, new Vector3(-740f, 24.8f, -314f), Sky, underground: false), "the Proud Patriarch's");
            Assert.AreEqual(9u, Shown(after, new Vector3(-992f, 23.9f, -294f), Sky, underground: false), "the Bowed Patriarch's");
            Assert.AreEqual(10u, Shown(after, new Vector3(-1000f, 24.8f, 150f), Sky, underground: false), "and the Patriarch Protector's");
        }

        [TestMethod]
        public void EachTempleIsItsOwn()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Migration);
            var after = Volumes(database);
            uint At(float x, float y, float z) => Shown(after, new Vector3(x, y, z), Name, underground: false);

            Assert.AreEqual(3u, At(-512f, 8f, -60f), "Raging Patriarch, the first body room");
            Assert.AreEqual(3u, At(-512f, -7.6f, 130f), "the second");
            Assert.AreEqual(3u, At(-512f, -23.9f, 320f), "the third");
            Assert.AreEqual(3u, At(-512f, -23.9f, 450f), "the end room");
            Assert.AreEqual(4u, At(-740f, 24.1f, -272f), "Proud Patriarch, its entry room");
            Assert.AreEqual(4u, At(-740f, 8.5f, -128f), "the first mind room");
            Assert.AreEqual(4u, At(-740f, -8.1f, 64f), "the second");
            Assert.AreEqual(4u, At(-868f, -7.7f, 96f), "the west side room");
            Assert.AreEqual(4u, At(-612f, -7.9f, 32f), "the east side room");
            Assert.AreEqual(4u, At(-772f, -7.7f, 192f), "the end room");
            Assert.AreEqual(5u, At(-992f, 24.1f, -252f), "Bowed Patriarch, its entry room");
            Assert.AreEqual(11u, At(-960f, 8.3f, -60f), "its spirit room");
            Assert.AreEqual(11u, Shown(after, new Vector3(-1030f, 8.3f, -150f), Sky, underground: false), "under the far-fog sky");
            Assert.AreEqual(5u, At(-992f, -7.7f, 36f), "its end room");
            Assert.AreEqual(6u, At(-992f, 24f, 234f), "the Patriarch Protector's room");
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
            Assert.AreEqual(ElohTemplesRegions.Volumes.Length + ElohTemplesRegions.Recuts.Length, once.Except(before).Count(), "the new rows and the recut ones");

            foreach (var sql in ElohTemplesRegions.Up())
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

            foreach (var sql in ElohTemplesRegions.Up())
                StringAssert.Contains(migrator.GenerateScript(Previous, Migration), sql);
            foreach (var sql in ElohTemplesRegions.Down())
                StringAssert.Contains(migrator.GenerateScript(Migration, Previous), sql);
        }

        /// <summary>The highest-priority region here that turns the setting on, 0 for none (the map's own).</summary>
        private static uint Shown(List<MapRegion> volumes, Vector3 at, int flag, bool underground) =>
            volumes.Where(v => v.Enabled && Temples.TryGetValue(v.RegionId, out var r) && (r.Flags & flag) != 0 && v.Contains(at, underground))
                .OrderByDescending(v => Temples[v.RegionId].Priority).Select(v => v.RegionId).FirstOrDefault();

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == ElohTemplesRegions.MapContextId)
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT map_context_id || ' ' || region_id || ' ' || shape || ' ' || printf('%.1f %.1f %.1f %.1f %.1f %.1f %.1f %.1f', pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y) || ' ' || underground || ' ' || enabled || ' ' || comment AS Value " +
                    "FROM map_region")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
