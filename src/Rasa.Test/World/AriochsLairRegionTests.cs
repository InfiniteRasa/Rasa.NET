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

    /// <summary>Arioch's Lair in the Wilderness (Ariochs_lair_region, AriochsLairRegion).</summary>
    [TestClass]
    [DoNotParallelize]
    public class AriochsLairRegionTests
    {
        private const string Migration = "20270620000000_Ariochs_lair_region";
        private const string Previous = "20270619000000_Abyssal_zone_region";

        private const int Ambient = 4, Music = 8, Name = 16;
        private const uint Lair = AriochsLairRegion.LairRegion;

        /// <summary>Concordia Wilderness's regions, as the .map has them: priority and the settings they turn on.</summary>
        private static readonly Dictionary<uint, (int Priority, int Flags)> Wilderness = new()
        {
            [5] = (1, 13), [35] = (2, 0), [37] = (3, 30), [13] = (4, 4), [31] = (5, 16), [25] = (6, 14), [21] = (7, 14), [22] = (8, 14), [23] = (9, 14),
            [24] = (10, 14), [19] = (11, 28), [18] = (12, 28), [50] = (13, 28), [56] = (14, 4), [17] = (15, 28), [16] = (16, 28), [6] = (17, 31),
            [7] = (18, 18), [34] = (19, 31), [1] = (20, 15), [32] = (21, 31), [57] = (22, 11), [53] = (23, 0), [54] = (24, 0), [55] = (25, 0),
            [33] = (26, 31), [66] = (27, 15), [9] = (28, 22), [49] = (29, 28), [12] = (30, 28), [47] = (31, 18), [10] = (32, 20), [36] = (33, 31),
            [45] = (34, 0), [46] = (35, 0), [14] = (36, 28), [48] = (37, 28), [38] = (38, 31), [39] = (39, 15), [15] = (40, 31), [8] = (41, 31),
            [11] = (42, 20), [51] = (43, 28), [26] = (44, 7), [27] = (45, 7), [30] = (46, 7), [58] = (47, 0), [59] = (48, 0), [60] = (49, 0),
            [61] = (50, 0), [62] = (51, 0), [63] = (52, 0), [64] = (53, 0), [65] = (54, 0),
        };

        [TestMethod]
        public void TheCanyonWhereAriochWaitsIsHisLair()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();
            var pool = new Vector3(125.4f, 247f, 733.1f);

            migrator.Migrate(Previous);
            Assert.AreNotEqual(Lair, Shown(Volumes(database), pool, Name, underground: false), "the lair had no volume");

            migrator.Migrate(Migration);
            var after = Volumes(database);
            Assert.AreEqual(Lair, Shown(after, pool, Name, underground: false), "at Arioch's spawn pool");
            Assert.AreEqual(Lair, Shown(after, pool, Music, underground: false), "with its music");
            Assert.AreEqual(Lair, Shown(after, pool, Ambient, underground: false), "and its rocky ambience");
            Assert.AreEqual(Lair, Shown(after, new Vector3(140f, 250.5f, 760f), Name, underground: false), "at the canyon's head");
            Assert.AreEqual(Lair, Shown(after, new Vector3(75f, 247.5f, 680f), Name, underground: false), "down the channel");
            Assert.AreNotEqual(Lair, Shown(after, new Vector3(40f, 235.1f, 645f), Name, underground: false), "but not the lowland past its mouth");
            Assert.AreNotEqual(Lair, Shown(after, new Vector3(200f, 301.3f, 720f), Name, underground: false), "nor the plateau above");
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
            Assert.AreEqual(AriochsLairRegion.Volumes.Length, once.Except(before).Count(), "the new rows");

            foreach (var sql in AriochsLairRegion.Up())
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

            foreach (var sql in AriochsLairRegion.Up())
                StringAssert.Contains(migrator.GenerateScript(Previous, Migration), sql);
            foreach (var sql in AriochsLairRegion.Down())
                StringAssert.Contains(migrator.GenerateScript(Migration, Previous), sql);
        }

        /// <summary>The highest-priority region here that turns the setting on, 0 for none (the map's own).</summary>
        private static uint Shown(List<MapRegion> volumes, Vector3 at, int flag, bool underground) =>
            volumes.Where(v => v.Enabled && Wilderness.TryGetValue(v.RegionId, out var r) && (r.Flags & flag) != 0 && v.Contains(at, underground))
                .OrderByDescending(v => Wilderness[v.RegionId].Priority).Select(v => v.RegionId).FirstOrDefault();

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == AriochsLairRegion.MapContextId)
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT map_context_id || ' ' || region_id || ' ' || shape || ' ' || printf('%.1f %.1f %.1f %.1f %.1f %.1f %.1f %.1f', pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y) || ' ' || underground || ' ' || enabled || ' ' || comment AS Value " +
                    "FROM map_region")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
