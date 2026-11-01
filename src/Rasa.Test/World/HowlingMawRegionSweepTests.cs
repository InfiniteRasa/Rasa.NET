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

    /// <summary>The region sweep on the Howling Maw (Howling_maw_region_sweep, HowlingMawRegionSweep).</summary>
    [TestClass]
    [DoNotParallelize]
    public class HowlingMawRegionSweepTests
    {
        private const string Migration = "20270720000000_Howling_maw_region_sweep";
        private const string Previous = "20270718000000_Purgas_station_gap_regions";

        private const int Sky = 1, Env = 2, Ambient = 4, Music = 8, Name = 16;

        /// <summary>The map's regions, as the .map has them: priority and the settings they turn on.</summary>
        private static readonly Dictionary<uint, (int Priority, int Flags)> Regions = new()
        {
            [1] = (1, 13), [32] = (2, 7), [42] = (3, 31), [37] = (4, 31), [33] = (5, 7), [30] = (6, 0), [36] = (7, 29), [35] = (8, 29), [10] = (9, 29),
            [22] = (10, 29), [20] = (11, 25), [24] = (12, 25), [25] = (13, 29), [3] = (14, 31), [4] = (15, 29), [46] = (16, 29), [9] = (17, 29),
            [13] = (18, 29), [6] = (19, 29), [23] = (20, 29), [14] = (21, 29), [15] = (22, 29), [16] = (23, 25), [17] = (24, 29), [7] = (25, 29),
            [18] = (26, 29), [26] = (27, 1), [27] = (28, 1), [28] = (29, 1), [29] = (30, 17), [5] = (31, 29), [39] = (32, 25), [21] = (33, 25),
            [38] = (34, 15), [40] = (35, 27), [31] = (36, 3), [19] = (37, 31), [47] = (38, 31), [41] = (39, 31), [8] = (40, 29), [11] = (41, 17),
            [12] = (42, 29), [43] = (43, 24), [44] = (44, 28), [45] = (45, 24), [48] = (46, 0), [51] = (47, 2), [52] = (48, 15),
        };

        /// <summary>Where each new or recut volume shows, and where the places beside it keep theirs.</summary>
        private static readonly (Vector3 At, bool Underground, int Flag, uint Before, uint After, string Why)[] Points =
        {
            (new Vector3(-760.7f, 197.7f, -447.5f), false, Name, 0u, 14u, "Devil's Cauldron label"),
            (new Vector3(-700f, 205.3f, -500f), false, Music, 0u, 14u, "plain east of the label"),
            (new Vector3(-479.7f, 199.5f, -364.7f), false, Name, 44u, 44u, "Power Station control point keeps its name"),
            (new Vector3(-1013.7f, 207.1f, -124.4f), false, Name, 0u, 4u, "west Dead Zone label"),
            (new Vector3(-1013.7f, 207.1f, -124.4f), false, Ambient, 0u, 4u, "volcanic ambient"),
            (new Vector3(-106.3f, 207.3f, 213.1f), false, Name, 0u, 46u, "east Dead Zone label"),
            (new Vector3(-150f, 204.6f, 300f), false, Music, 0u, 46u, "north of the label"),
            (new Vector3(61.8f, 206.6f, -124.6f), false, Name, 8u, 8u, "Toma Springs unchanged"),
            (new Vector3(-482f, 123.5f, -490f), true, Sky, 0u, 52u, "logos room under Power Station"),
            (new Vector3(232f, 250.2f, -470f), true, Music, 0u, 52u, "logos room R2"),
            (new Vector3(1200f, 294.8f, -375f), false, Sky, 0u, 52u, "logos room R3"),
            (new Vector3(-550f, 213.2f, 230f), false, Sky, 0u, 3u, "volcano crater tower area"),
            (new Vector3(-480f, 202f, 300f), true, Name, 0u, 3u, "goo lake (underground-flagged)"),
            (new Vector3(-618.5f, 200f, -1051.4f), false, Env, 3u, 0u, "Gangus dropship pad loses the volcano env"),
            (new Vector3(-618.5f, 200f, -1051.4f), false, Name, 12u, 12u, "Gangus Outpost name unchanged"),
        };

        [TestMethod]
        public void EachPlaceShowsItsRegion()
        {
            var (before, after) = Migrated();

            foreach (var (at, underground, flag, was, now, why) in Points)
            {
                Assert.AreEqual(was, Shown(before, at, flag, underground), $"{why}: before");
                Assert.AreEqual(now, Shown(after, at, flag, underground), $"{why}: after");
            }
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
        public void TheUpRunsTwiceAndTheDownPutsItBack()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            var before = Snapshot(database);

            migrator.Migrate(Migration);
            var once = Snapshot(database);
            Assert.AreEqual(HowlingMawRegionSweep.Volumes.Length + HowlingMawRegionSweep.Recuts.Length, once.Except(before).Count(), "the new rows and the recut ones");

            foreach (var sql in HowlingMawRegionSweep.Up())
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

            foreach (var sql in HowlingMawRegionSweep.Up())
                StringAssert.Contains(migrator.GenerateScript(Previous, Migration), sql);
            foreach (var sql in HowlingMawRegionSweep.Down())
                StringAssert.Contains(migrator.GenerateScript(Migration, Previous), sql);
        }

        /// <summary>The highest-priority region here that turns the setting on, 0 for none (the map's own).</summary>
        private static uint Shown(List<MapRegion> volumes, Vector3 at, int flag, bool underground) =>
            volumes.Where(v => v.Enabled && Regions.TryGetValue(v.RegionId, out var r) && (r.Flags & flag) != 0 && v.Contains(at, underground))
                .OrderByDescending(v => Regions[v.RegionId].Priority).Select(v => v.RegionId).FirstOrDefault();

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == HowlingMawRegionSweep.MapContextId)
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT map_context_id || ' ' || region_id || ' ' || shape || ' ' || printf('%.1f %.1f %.1f %.1f %.1f %.1f %.1f %.1f', pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y) || ' ' || underground || ' ' || enabled || ' ' || comment AS Value " +
                    "FROM map_region WHERE map_context_id = 2051")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
