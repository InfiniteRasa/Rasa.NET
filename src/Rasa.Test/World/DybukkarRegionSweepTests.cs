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

    /// <summary>The region sweep on the Dybukkar Garrison (Dybukkar_region_sweep, DybukkarRegionSweep).</summary>
    [TestClass]
    [DoNotParallelize]
    public class DybukkarRegionSweepTests
    {
        private const string Migration = "20270724020000_Dybukkar_region_sweep";
        private const string Previous = "20270718000000_Purgas_station_gap_regions";

        private const int Sky = 1, Env = 2, Ambient = 4, Music = 8, Name = 16;

        /// <summary>The map's regions, as the .map has them: priority and the settings they turn on.</summary>
        private static readonly Dictionary<uint, (int Priority, int Flags)> Regions = new()
        {
            [1] = (1, 13), [20] = (2, 0), [19] = (3, 19), [6] = (4, 20), [7] = (5, 20), [3] = (6, 20), [8] = (7, 20), [4] = (8, 20), [5] = (9, 20),
            [18] = (10, 20), [11] = (11, 16), [10] = (12, 20), [12] = (13, 23), [13] = (14, 20), [14] = (15, 20), [16] = (16, 31), [22] = (17, 31),
            [15] = (18, 20), [17] = (19, 31), [21] = (20, 31),
        };

        /// <summary>Where each new or recut volume shows, and where the places beside it keep theirs.</summary>
        private static readonly (Vector3 At, bool Underground, int Flag, uint Before, uint After, string Why)[] Points =
        {
            (new Vector3(0f, 329.9f, -90f), false, Name, 16u, 12u, "Main Production Chamber north end was Repair Yard"),
            (new Vector3(0f, 329.9f, -90f), false, Sky, 16u, 12u, "chamber gets its arena sky back, not the yard's exterior sky"),
            (new Vector3(-40f, 320.5f, -100f), false, Sky, 16u, 12u, "chamber west balcony"),
            (new Vector3(-122f, 302.7f, 0f), false, Name, 16u, 5u, "Maintenance Area C service tunnel was Repair Yard"),
            (new Vector3(-40f, 279.8f, 60f), false, Name, 16u, 16u, "yard trench (POI label)"),
            (new Vector3(0f, 300.5f, 50f), false, Sky, 16u, 16u, "yard gantry deck"),
            (new Vector3(60f, 285.2f, 70f), false, Name, 16u, 16u, "yard east edge"),
            (new Vector3(-130f, 313.9f, 100f), false, Name, 16u, 16u, "west slope box"),
            (new Vector3(100f, 306.1f, 120f), false, Name, 16u, 16u, "north-east slope box"),
            (new Vector3(-60f, 303.1f, 180f), false, Name, 16u, 16u, "north edge of the yard"),
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
            Assert.AreEqual(DybukkarRegionSweep.Volumes.Length + DybukkarRegionSweep.Recuts.Length, once.Except(before).Count(), "the new rows and the recut ones");

            foreach (var sql in DybukkarRegionSweep.Up())
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

            foreach (var sql in DybukkarRegionSweep.Up())
                StringAssert.Contains(migrator.GenerateScript(Previous, Migration), sql);
            foreach (var sql in DybukkarRegionSweep.Down())
                StringAssert.Contains(migrator.GenerateScript(Migration, Previous), sql);
        }

        /// <summary>The highest-priority region here that turns the setting on, 0 for none (the map's own).</summary>
        private static uint Shown(List<MapRegion> volumes, Vector3 at, int flag, bool underground) =>
            volumes.Where(v => v.Enabled && Regions.TryGetValue(v.RegionId, out var r) && (r.Flags & flag) != 0 && v.Contains(at, underground))
                .OrderByDescending(v => Regions[v.RegionId].Priority).Select(v => v.RegionId).FirstOrDefault();

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == DybukkarRegionSweep.MapContextId)
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT map_context_id || ' ' || region_id || ' ' || shape || ' ' || printf('%.1f %.1f %.1f %.1f %.1f %.1f %.1f %.1f', pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y) || ' ' || underground || ' ' || enabled || ' ' || comment AS Value " +
                    "FROM map_region WHERE map_context_id = 2190")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
