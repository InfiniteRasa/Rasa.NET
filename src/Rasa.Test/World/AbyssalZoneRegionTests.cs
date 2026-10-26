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

    /// <summary>Torden Abyss's Abyssal Zone chasm (Abyssal_zone_region, AbyssalZoneRegion).</summary>
    [TestClass]
    [DoNotParallelize]
    public class AbyssalZoneRegionTests
    {
        private const string Migration = "20270619000000_Abyssal_zone_region";
        private const string Previous = "20270618000000_Ruins_of_tampeii_regions";

        private const int Sky = 1, Name = 16;
        private const uint Zone = AbyssalZoneRegion.ZoneRegion;

        /// <summary>Torden Abyss's regions, as the .map has them: priority and the settings they turn on.</summary>
        private static readonly Dictionary<uint, (int Priority, int Flags)> Abyss = new()
        {
            [1] = (1, 13), [5] = (2, 31), [13] = (3, 29), [14] = (4, 29), [16] = (5, 29), [6] = (6, 31), [44] = (7, 31), [33] = (8, 31), [34] = (9, 31),
            [28] = (10, 20), [29] = (11, 20), [18] = (12, 31), [22] = (13, 31), [23] = (14, 31), [24] = (15, 31), [25] = (16, 31), [26] = (17, 31),
            [40] = (18, 31), [41] = (19, 31), [42] = (20, 31), [45] = (21, 29), [35] = (22, 25), [37] = (23, 17), [36] = (24, 17), [43] = (25, 31),
            [20] = (26, 29), [21] = (27, 7), [17] = (28, 31), [31] = (29, 24), [32] = (30, 24), [38] = (31, 0), [39] = (32, 19), [15] = (33, 31),
        };

        [TestMethod]
        public void OverTheEdgeIsTheAbyssalZone()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();
            var chasmFloor = new Vector3(0f, 0.2f, -800f);

            migrator.Migrate(Previous);
            Assert.AreNotEqual(Zone, Shown(Volumes(database), chasmFloor, Sky, underground: false), "the chasm had no volume");

            migrator.Migrate(Migration);
            var after = Volumes(database);
            Assert.AreEqual(Zone, Shown(after, chasmFloor, Sky, underground: false), "at the bottom");
            Assert.AreEqual(Zone, Shown(after, chasmFloor, Name, underground: false), "by name");
            Assert.AreEqual(Zone, Shown(after, new Vector3(25f, 450f, -650f), Sky, underground: false), "falling past the watchtower spit");
            Assert.AreEqual(Zone, Shown(after, new Vector3(900f, 0.2f, -400f), Sky, underground: false), "in the south-east corner");
        }

        [TestMethod]
        public void TheShelfAndTheCavesKeepTheirOwn()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();
            migrator.Migrate(Migration);
            var after = Volumes(database);

            Assert.AreNotEqual(Zone, Shown(after, new Vector3(25f, 536.2f, -650f), Sky, underground: false), "on the spit to the watchtower");
            Assert.AreNotEqual(Zone, Shown(after, new Vector3(-300f, 518.8f, -540f), Sky, underground: false), "at the shelf's lowest point");
            Assert.AreNotEqual(Zone, Shown(after, new Vector3(212f, 516.2f, -360f), Sky, underground: true), "in the cave north of the edge");
            Assert.AreNotEqual(Zone, Shown(after, new Vector3(-325f, 183f, -425f), Sky, underground: true), "in the lower cave");
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
            Assert.AreEqual(AbyssalZoneRegion.Volumes.Length, once.Except(before).Count(), "the new rows");

            foreach (var sql in AbyssalZoneRegion.Up())
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

            foreach (var sql in AbyssalZoneRegion.Up())
                StringAssert.Contains(migrator.GenerateScript(Previous, Migration), sql);
            foreach (var sql in AbyssalZoneRegion.Down())
                StringAssert.Contains(migrator.GenerateScript(Migration, Previous), sql);
        }

        /// <summary>The highest-priority region here that turns the setting on, 0 for none (the map's own).</summary>
        private static uint Shown(List<MapRegion> volumes, Vector3 at, int flag, bool underground) =>
            volumes.Where(v => v.Enabled && Abyss.TryGetValue(v.RegionId, out var r) && (r.Flags & flag) != 0 && v.Contains(at, underground))
                .OrderByDescending(v => Abyss[v.RegionId].Priority).Select(v => v.RegionId).FirstOrDefault();

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == AbyssalZoneRegion.MapContextId)
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT map_context_id || ' ' || region_id || ' ' || shape || ' ' || printf('%.1f %.1f %.1f %.1f %.1f %.1f %.1f %.1f', pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y) || ' ' || underground || ' ' || enabled || ' ' || comment AS Value " +
                    "FROM map_region")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
