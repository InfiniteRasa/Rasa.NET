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

    /// <summary>The Daghda's Urn Village, Enigma Falls and Enigma Cavern (Wilderness_urn_enigma_regions, WildernessUrnEnigmaRegions).</summary>
    [TestClass]
    [DoNotParallelize]
    public class WildernessUrnEnigmaRegionsTests
    {
        private const string Migration = "20270306000000_Wilderness_urn_enigma_regions";
        private const string Previous = "20270305000000_Devils_den";

        private const int Sky = 1, Env = 2, Ambient = 4, Music = 8, Name = 16;

        /// <summary>The Wilderness's regions, as the .map has them: priority and the settings they turn on.</summary>
        private static readonly Dictionary<uint, (int Priority, int Flags)> Wilderness = new()
        {
            [5] = (1, 13), [35] = (2, 0), [37] = (3, 30), [13] = (4, 4), [31] = (5, 16), [25] = (6, 14), [21] = (7, 14), [22] = (8, 14), [23] = (9, 14),
            [24] = (10, 14), [19] = (11, 28), [18] = (12, 28), [50] = (13, 28), [56] = (14, 4), [17] = (15, 28), [16] = (16, 28), [6] = (17, 31), [7] = (18, 18),
            [34] = (19, 31), [1] = (20, 15), [32] = (21, 31), [57] = (22, 11), [53] = (23, 0), [54] = (24, 0), [55] = (25, 0), [33] = (26, 31), [66] = (27, 15),
            [9] = (28, 22), [49] = (29, 28), [12] = (30, 28), [47] = (31, 18), [10] = (32, 20), [36] = (33, 31), [45] = (34, 0), [46] = (35, 0), [14] = (36, 28),
            [48] = (37, 28), [38] = (38, 31), [39] = (39, 15), [15] = (40, 31), [8] = (41, 31), [11] = (42, 20), [51] = (43, 28), [26] = (44, 7), [27] = (45, 7),
            [30] = (46, 7), [58] = (47, 0), [59] = (48, 0), [60] = (49, 0), [61] = (50, 0), [62] = (51, 0), [63] = (52, 0), [64] = (53, 0), [65] = (54, 0),
        };

        [TestMethod]
        public void TheVillageIsTheSafeZoneItsBuildingsTheInteriorAndItsSurroundingsTheRest()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();
            var surroundings = new Vector3(-790f, 279f, 855f);

            migrator.Migrate(Previous);
            Assert.AreEqual(48u, Shown(Volumes(database), surroundings, Music, underground: false), "48's circle was over 14's everywhere");

            migrator.Migrate(Migration);
            var after = Volumes(database);
            uint At(float x, float y, float z, int flag) => Shown(after, new Vector3(x, y, z), flag, underground: false);

            Assert.AreEqual(48u, At(-628f, 278.1f, 799f, Music), "the safe zone's music at the big tent");
            Assert.AreEqual(48u, At(-700f, 276.3f, 770f, Music), "and on the platforms");
            Assert.AreEqual(14u, At(-790f, 279f, 855f, Music), "14's in the village's surroundings");

            Assert.AreEqual(38u, At(-608f, 282.3f, 862f, Sky), "the housing under the interior sky");
            Assert.AreEqual(38u, At(-555f, 283.3f, 885f, Ambient), "the research HQ");
            Assert.AreEqual(38u, At(-650f, 283.7f, 885f, Name), "the hospital's ground floor");
            Assert.AreEqual(38u, At(-640f, 292.1f, 900f, Name), "and its upper floor");
            Assert.AreNotEqual(38u, At(-628f, 278.1f, 799f, Sky), "the open village keeps its own sky");
        }

        [TestMethod]
        public void TheWaterfallIsEnigmaFallsAndTheCavesBehindItEnigmaCavern()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Migration);
            var after = Volumes(database);

            Assert.AreEqual(10u, Shown(after, new Vector3(-860f, 169.9f, -585f), Name, underground: false), "at the foot of the falls");
            Assert.AreEqual(10u, Shown(after, new Vector3(-830f, 168.9f, -545f), Name, underground: false), "at the pool's outflow");
            Assert.AreEqual(10u, Shown(after, new Vector3(-870f, 210.6f, -590f), Name, underground: false), "at the top of the falls");

            Assert.AreEqual(36u, Shown(after, new Vector3(-870f, 172.2f, -593f), Name, underground: true), "in the cavern entrance behind the falls");
            Assert.AreEqual(36u, Shown(after, new Vector3(-911f, 188.4f, -634f), Sky, underground: true), "in the Eloh cavern");
            Assert.AreEqual(36u, Shown(after, new Vector3(-960f, 157.1f, -580f), Name, underground: true), "on the cavern floor");
            Assert.AreEqual(36u, Shown(after, new Vector3(-848f, 141f, -718f), Name, underground: true), "at the Eloh corridor's logos");
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
            Assert.AreEqual(WildernessUrnEnigmaRegions.Volumes.Length + 1, once.Except(before).Count(), "the new rows and the village");

            foreach (var sql in WildernessUrnEnigmaRegions.Up())
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

            foreach (var sql in WildernessUrnEnigmaRegions.Up())
                StringAssert.Contains(migrator.GenerateScript(Previous, Migration), sql);
            foreach (var sql in WildernessUrnEnigmaRegions.Down())
                StringAssert.Contains(migrator.GenerateScript(Migration, Previous), sql);
        }

        /// <summary>The highest-priority region here that turns the setting on, 0 for none (the map's own).</summary>
        private static uint Shown(List<MapRegion> volumes, Vector3 at, int flag, bool underground) =>
            volumes.Where(v => v.Enabled && Wilderness.TryGetValue(v.RegionId, out var r) && (r.Flags & flag) != 0 && v.Contains(at, underground))
                .OrderByDescending(v => Wilderness[v.RegionId].Priority).Select(v => v.RegionId).FirstOrDefault();

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == WildernessUrnEnigmaRegions.MapContextId)
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT map_context_id || ' ' || region_id || ' ' || shape || ' ' || printf('%.1f %.1f %.1f %.1f %.1f', pos_x, pos_z, radius, half_x, half_z) || ' ' || enabled AS Value " +
                    "FROM map_region")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
