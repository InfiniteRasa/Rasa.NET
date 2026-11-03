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

    /// <summary>The region sweep on the Brann Water Refinery (Brann_water_refinery_region_sweep, BrannWaterRefineryRegionSweep).</summary>
    [TestClass]
    [DoNotParallelize]
    public class BrannWaterRefineryRegionSweepTests
    {
        private const string Migration = "20270724070000_Brann_water_refinery_region_sweep";
        private const string Previous = "20270718000000_Purgas_station_gap_regions";

        private const int Sky = 1, Env = 2, Ambient = 4, Music = 8, Name = 16;

        /// <summary>The map's regions, as the .map has them: priority and the settings they turn on.</summary>
        private static readonly Dictionary<uint, (int Priority, int Flags)> Regions = new()
        {
            [32] = (1, 3), [27] = (2, 0), [26] = (3, 0), [28] = (4, 0), [29] = (5, 0), [30] = (6, 0), [31] = (7, 0), [24] = (8, 1), [23] = (9, 1),
            [1] = (10, 3), [2] = (11, 23), [13] = (12, 20), [3] = (13, 20), [4] = (14, 20), [5] = (15, 20), [7] = (16, 20), [9] = (17, 23),
            [10] = (18, 20), [25] = (19, 19), [11] = (20, 22), [15] = (21, 20), [21] = (22, 20), [22] = (23, 20), [16] = (24, 20), [17] = (25, 20),
            [19] = (26, 20), [18] = (27, 20), [20] = (28, 20),
        };

        /// <summary>Where each new or recut volume shows, and where the places beside it keep theirs.</summary>
        private static readonly (Vector3 At, bool Underground, int Flag, uint Before, uint After, string Why)[] Points =
        {
            (new Vector3(-200f, 428.3f, -100f), false, Sky, 0u, 32u, "plains surface west of the canyon"),
            (new Vector3(-200f, 428.3f, -100f), false, Env, 0u, 32u, "plains env map"),
            (new Vector3(400f, 422.1f, 300f), false, Sky, 0u, 32u, "plains surface far east"),
            (new Vector3(-60f, 437.7f, -470f), false, Sky, 0u, 32u, "plains above the reservoir"),
            (new Vector3(-34f, 431.6f, -65f), false, Sky, 0u, 0u, "entrance dome keeps interior sky"),
            (new Vector3(-34f, 431.6f, -65f), false, Name, 13u, 13u, "entrance dome stays Main Access"),
            (new Vector3(-154f, 399.8f, -241f), false, Sky, 9u, 9u, "Skybridge unchanged"),
            (new Vector3(-104f, 399.8f, -153f), true, Sky, 2u, 2u, "Central Operations unchanged"),
            (new Vector3(-14f, 401.9f, -230f), true, Sky, 0u, 0u, "interior corridor keeps Brann sky"),
            (new Vector3(-86f, 407.8f, -330f), true, Name, 17u, 17u, "PumpStation 1 unchanged"),
            (new Vector3(-145.2f, 152f, -208.6f), false, Sky, 0u, 24u, "turbine canyon floor"),
            (new Vector3(89.6f, 92.3f, -476.1f), false, Sky, 0u, 24u, "abyss off the south-east edge"),
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
            Assert.AreEqual(BrannWaterRefineryRegionSweep.Volumes.Length + BrannWaterRefineryRegionSweep.Recuts.Length, once.Except(before).Count(), "the new rows and the recut ones");

            foreach (var sql in BrannWaterRefineryRegionSweep.Up())
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

            foreach (var sql in BrannWaterRefineryRegionSweep.Up())
                StringAssert.Contains(migrator.GenerateScript(Previous, Migration), sql);
            foreach (var sql in BrannWaterRefineryRegionSweep.Down())
                StringAssert.Contains(migrator.GenerateScript(Migration, Previous), sql);
        }

        /// <summary>The highest-priority region here that turns the setting on, 0 for none (the map's own).</summary>
        private static uint Shown(List<MapRegion> volumes, Vector3 at, int flag, bool underground) =>
            volumes.Where(v => v.Enabled && Regions.TryGetValue(v.RegionId, out var r) && (r.Flags & flag) != 0 && v.Contains(at, underground))
                .OrderByDescending(v => Regions[v.RegionId].Priority).Select(v => v.RegionId).FirstOrDefault();

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == BrannWaterRefineryRegionSweep.MapContextId)
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT map_context_id || ' ' || region_id || ' ' || shape || ' ' || printf('%.1f %.1f %.1f %.1f %.1f %.1f %.1f %.1f', pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y) || ' ' || underground || ' ' || enabled || ' ' || comment AS Value " +
                    "FROM map_region WHERE map_context_id = 2093")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
