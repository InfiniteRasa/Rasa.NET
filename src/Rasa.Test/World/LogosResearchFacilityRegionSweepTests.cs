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

    /// <summary>The region sweep on the Logos Research Facility (Logos_research_facility_region_sweep, LogosResearchFacilityRegionSweep).</summary>
    [TestClass]
    [DoNotParallelize]
    public class LogosResearchFacilityRegionSweepTests
    {
        private const string Migration = "20270719020000_Logos_research_facility_region_sweep";
        private const string Previous = "20270718000000_Purgas_station_gap_regions";

        private const int Sky = 1, Env = 2, Ambient = 4, Music = 8, Name = 16;

        /// <summary>The map's regions, as the .map has them: priority and the settings they turn on.</summary>
        private static readonly Dictionary<uint, (int Priority, int Flags)> Regions = new()
        {
            [1] = (1, 1), [43] = (2, 0), [44] = (3, 0), [45] = (4, 0), [46] = (5, 0), [2] = (6, 20), [36] = (7, 20), [13] = (8, 20), [20] = (9, 20),
            [16] = (10, 20), [17] = (11, 20), [18] = (12, 20), [19] = (13, 20), [37] = (14, 20), [38] = (15, 20), [27] = (16, 23), [39] = (17, 20),
            [40] = (18, 20), [41] = (19, 20), [42] = (20, 20), [29] = (21, 20), [30] = (22, 20), [23] = (23, 20), [15] = (24, 20),
        };

        /// <summary>Where each new or recut volume shows, and where the places beside it keep theirs.</summary>
        private static readonly (Vector3 At, bool Underground, int Flag, uint Before, uint After, string Why)[] Points =
        {
            (new Vector3(608f, 251.9f, 0f), false, Name, 0u, 42u, "north wing hall"),
            (new Vector3(570f, 257.6f, 18f), false, Name, 30u, 42u, "north wing, was under the B2 circle"),
            (new Vector3(650f, 249.3f, -20f), false, Ambient, 0u, 42u, "north wing east side"),
            (new Vector3(608f, 249.9f, -250f), false, Name, 23u, 39u, "south wing hall, was under the Analysis Lab circle"),
            (new Vector3(500f, 251.9f, -128f), false, Ambient, 0u, 41u, "west wing"),
            (new Vector3(720f, 257.9f, -128f), false, Name, 0u, 40u, "east wing"),
            (new Vector3(742f, 257.9f, -162f), false, Name, 15u, 40u, "east wing tunnel, was under Specimen Control"),
            (new Vector3(608f, 257.9f, -295f), false, Name, 23u, 38u, "corridor to the Analysis Lab"),
            (new Vector3(830f, 264.8f, -160f), false, Ambient, 0u, 38u, "east tunnel ramp foot"),
            (new Vector3(600f, 248.7f, -128f), false, Name, 27u, 27u, "Test Chamber keeps its name"),
            (new Vector3(700f, 247.9f, 56f), false, Name, 29u, 29u, "Bio Lab B1 room keeps"),
            (new Vector3(660f, 257.9f, 50f), false, Name, 29u, 29u, "Bio Lab B1 west edge keeps"),
            (new Vector3(520f, 247.9f, 40f), false, Name, 30u, 30u, "Bio Lab B2 room keeps"),
            (new Vector3(608f, 248.1f, -350f), false, Name, 23u, 23u, "Analysis Lab room keeps"),
            (new Vector3(740f, 291.7f, -250f), false, Ambient, 0u, 2u, "holding cells chunnel"),
            (new Vector3(728f, 297.9f, -176f), false, Name, 15u, 15u, "Specimen Control overlook keeps"),
            (new Vector3(790f, 321.5f, -136f), false, Name, 0u, 16u, "Security room outer ring"),
            (new Vector3(760f, 321.1f, -136f), false, Name, 16u, 16u, "Security room centre keeps"),
            (new Vector3(680f, 289.9f, -200f), false, Name, 0u, 0u, "middle-floor SW room untouched"),
            (new Vector3(820f, 321.9f, -104f), false, Name, 0u, 0u, "entrance tunnel untouched"),
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
            Assert.AreEqual(LogosResearchFacilityRegionSweep.Volumes.Length + LogosResearchFacilityRegionSweep.Recuts.Length, once.Except(before).Count(), "the new rows and the recut ones");

            foreach (var sql in LogosResearchFacilityRegionSweep.Up())
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

            foreach (var sql in LogosResearchFacilityRegionSweep.Up())
                StringAssert.Contains(migrator.GenerateScript(Previous, Migration), sql);
            foreach (var sql in LogosResearchFacilityRegionSweep.Down())
                StringAssert.Contains(migrator.GenerateScript(Migration, Previous), sql);
        }

        /// <summary>The highest-priority region here that turns the setting on, 0 for none (the map's own).</summary>
        private static uint Shown(List<MapRegion> volumes, Vector3 at, int flag, bool underground) =>
            volumes.Where(v => v.Enabled && Regions.TryGetValue(v.RegionId, out var r) && (r.Flags & flag) != 0 && v.Contains(at, underground))
                .OrderByDescending(v => Regions[v.RegionId].Priority).Select(v => v.RegionId).FirstOrDefault();

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == LogosResearchFacilityRegionSweep.MapContextId)
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT map_context_id || ' ' || region_id || ' ' || shape || ' ' || printf('%.1f %.1f %.1f %.1f %.1f %.1f %.1f %.1f', pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y) || ' ' || underground || ' ' || enabled || ' ' || comment AS Value " +
                    "FROM map_region WHERE map_context_id = 1700")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
