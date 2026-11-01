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

    /// <summary>The region sweep on the Wetland Refinery (Wetland_refinery_region_sweep, WetlandRefineryRegionSweep).</summary>
    [TestClass]
    [DoNotParallelize]
    public class WetlandRefineryRegionSweepTests
    {
        private const string Migration = "20270719010000_Wetland_refinery_region_sweep";
        private const string Previous = "20270718000000_Purgas_station_gap_regions";

        private const int Sky = 1, Env = 2, Ambient = 4, Music = 8, Name = 16;

        /// <summary>The map's regions, as the .map has them: priority and the settings they turn on.</summary>
        private static readonly Dictionary<uint, (int Priority, int Flags)> Regions = new()
        {
            [1] = (1, 13), [16] = (2, 15), [27] = (3, 15), [7] = (4, 31), [8] = (5, 27), [9] = (6, 27), [22] = (7, 16), [23] = (8, 16), [24] = (9, 16),
            [25] = (10, 16), [26] = (11, 16), [10] = (12, 19), [11] = (13, 27), [12] = (14, 11), [13] = (15, 3), [14] = (16, 3), [15] = (17, 27),
            [4] = (18, 19), [5] = (19, 27), [18] = (20, 16), [19] = (21, 16), [20] = (22, 16), [21] = (23, 16), [17] = (24, 0), [2] = (25, 3),
            [3] = (26, 3), [6] = (27, 19),
        };

        /// <summary>Where each new or recut volume shows, and where the places beside it keep theirs.</summary>
        private static readonly (Vector3 At, bool Underground, int Flag, uint Before, uint After, string Why)[] Points =
        {
            (new Vector3(-288f, 111.3f, 40f), true, Name, 0u, 19u, "west refinery dome floor"),
            (new Vector3(104f, 111.3f, 0f), true, Name, 0u, 19u, "east refinery dome floor"),
            (new Vector3(104f, 105.9f, -96f), true, Sky, 0u, 8u, "east power supply room"),
            (new Vector3(-288f, 105.9f, -64f), true, Sky, 0u, 9u, "west power supply room"),
            (new Vector3(-260f, 106.8f, -60f), true, Name, 0u, 9u, "west power supply room east cap"),
            (new Vector3(30f, 110.6f, -224f), true, Sky, 0u, 27u, "Eloh shrine room"),
            (new Vector3(0f, 110.7f, -224f), true, Ambient, 0u, 27u, "Eloh corridor coupler"),
            (new Vector3(-96f, 97.9f, 176f), true, Sky, 0u, 15u, "experimentation control room"),
            (new Vector3(-125f, 109.3f, 176f), true, Name, 0u, 15u, "control room west ring"),
            (new Vector3(-72f, 91.3f, 186f), true, Name, 0u, 0u, "extraction-chair exam room stays out"),
            (new Vector3(-112f, 125.9f, 8f), true, Sky, 0u, 10u, "upper barracks chunnel"),
            (new Vector3(-112f, 131.5f, 20f), true, Name, 0u, 10u, "upper barracks mezzanine"),
            (new Vector3(-80f, 93.9f, -20f), true, Sky, 0u, 10u, "lower barracks chunnel"),
            (new Vector3(-221f, 107.5f, -213f), false, Sky, 0u, 7u, "base camp in the clearing"),
            (new Vector3(-200f, 107.5f, -230f), false, Music, 0u, 7u, "clearing by the tent"),
            (new Vector3(-280f, 117.7f, -168f), true, Sky, 0u, 16u, "cave tunnel"),
            (new Vector3(-290f, 128.6f, -195f), true, Ambient, 0u, 16u, "cave tunnel turn"),
            (new Vector3(-210f, 107.9f, -128f), true, Sky, 0u, 0u, "facility chunnel north of the clearing unchanged"),
            (new Vector3(-180f, 158.3f, -170f), false, Sky, 0u, 0u, "terrain over the facility stays out of LZ X-Ray"),
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
            Assert.AreEqual(WetlandRefineryRegionSweep.Volumes.Length + WetlandRefineryRegionSweep.Recuts.Length, once.Except(before).Count(), "the new rows and the recut ones");

            foreach (var sql in WetlandRefineryRegionSweep.Up())
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

            foreach (var sql in WetlandRefineryRegionSweep.Up())
                StringAssert.Contains(migrator.GenerateScript(Previous, Migration), sql);
            foreach (var sql in WetlandRefineryRegionSweep.Down())
                StringAssert.Contains(migrator.GenerateScript(Migration, Previous), sql);
        }

        /// <summary>The highest-priority region here that turns the setting on, 0 for none (the map's own).</summary>
        private static uint Shown(List<MapRegion> volumes, Vector3 at, int flag, bool underground) =>
            volumes.Where(v => v.Enabled && Regions.TryGetValue(v.RegionId, out var r) && (r.Flags & flag) != 0 && v.Contains(at, underground))
                .OrderByDescending(v => Regions[v.RegionId].Priority).Select(v => v.RegionId).FirstOrDefault();

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == WetlandRefineryRegionSweep.MapContextId)
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT map_context_id || ' ' || region_id || ' ' || shape || ' ' || printf('%.1f %.1f %.1f %.1f %.1f %.1f %.1f %.1f', pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y) || ' ' || underground || ' ' || enabled || ' ' || comment AS Value " +
                    "FROM map_region WHERE map_context_id = 1429")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
