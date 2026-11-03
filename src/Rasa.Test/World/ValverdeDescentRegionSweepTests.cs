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

    /// <summary>The region sweep on the Valverde Descent (Valverde_descent_region_sweep, ValverdeDescentRegionSweep).</summary>
    [TestClass]
    [DoNotParallelize]
    public class ValverdeDescentRegionSweepTests
    {
        private const string Migration = "20270721000000_Valverde_descent_region_sweep";
        private const string Previous = "20270718000000_Purgas_station_gap_regions";

        private const int Sky = 1, Env = 2, Ambient = 4, Music = 8, Name = 16;

        /// <summary>The map's regions, as the .map has them: priority and the settings they turn on.</summary>
        private static readonly Dictionary<uint, (int Priority, int Flags)> Regions = new()
        {
            [2] = (1, 13), [12] = (2, 24), [23] = (3, 15), [51] = (4, 15), [52] = (5, 15), [20] = (6, 7), [24] = (7, 15), [4] = (8, 0), [3] = (9, 29),
            [5] = (10, 29), [25] = (11, 15), [10] = (12, 29), [11] = (13, 29), [13] = (14, 31), [40] = (15, 29), [6] = (16, 29), [42] = (17, 29),
            [41] = (18, 29), [17] = (19, 31), [16] = (20, 31), [7] = (21, 31), [27] = (22, 31), [21] = (23, 23), [22] = (24, 31), [43] = (25, 31),
            [8] = (26, 31), [26] = (27, 31), [38] = (28, 31), [39] = (29, 31), [44] = (30, 31), [45] = (31, 0), [28] = (32, 31), [35] = (33, 31),
            [30] = (34, 31), [32] = (35, 0), [34] = (36, 0), [9] = (37, 15), [15] = (38, 15), [18] = (39, 7), [46] = (40, 4), [47] = (41, 28),
            [48] = (42, 28), [49] = (43, 28), [50] = (44, 28), [19] = (45, 13), [53] = (46, 0),
        };

        /// <summary>Where each new or recut volume shows, and where the places beside it keep theirs.</summary>
        private static readonly (Vector3 At, bool Underground, int Flag, uint Before, uint After, string Why)[] Points =
        {
            (new Vector3(-350f, 710.4f, -650f), true, Name, 0u, 12u, "cargo bay: was the map name (surface-only circle), now Fort Virgil"),
            (new Vector3(-350f, 710.4f, -650f), true, Ambient, 23u, 23u, "cargo bay keeps region 23"),
            (new Vector3(-300f, 737.5f, -640f), false, Name, 12u, 12u, "fort plaza stays Fort Virgil"),
            (new Vector3(-411f, 740.7f, -633f), false, Name, 0u, 12u, "HQ west half (Class Trainer) was outside the circle"),
            (new Vector3(-350f, 714.2f, -730f), false, Name, 12u, 12u, "wormhole platform keeps Fort Virgil (fort box stops at z -712)"),
            (new Vector3(-239f, 738.4f, -614f), false, Ambient, 0u, 51u, "hospital ward"),
            (new Vector3(-239f, 738.4f, -614f), false, Name, 12u, 12u, "hospital keeps the Fort Virgil name"),
            (new Vector3(-411f, 740.7f, -633f), false, Ambient, 0u, 52u, "HQ (Class Trainer)"),
            (new Vector3(-261f, 737.5f, -577f), false, Ambient, 0u, 0u, "courtyard by the north gate stays outdoors"),
            (new Vector3(-290f, 674.5f, -352f), true, Sky, 0u, 24u, "tunnel north end near Camp Cato"),
            (new Vector3(-300f, 695.7f, -450f), true, Sky, 0u, 24u, "tunnel south mouth"),
            (new Vector3(-280f, 672.9f, -400f), true, Sky, 24u, 24u, "tunnel middle unchanged"),
            (new Vector3(-295f, 696.4f, -440f), false, Sky, 0u, 0u, "open ground at the south mouth stays outdoors"),
            (new Vector3(130f, 392.7f, 680f), true, Name, 0u, 21u, "north half of the hollow"),
            (new Vector3(126f, 382.9f, 589.5f), true, Name, 21u, 22u, "medic station (hospital marker)"),
            (new Vector3(126f, 382.9f, 589.5f), true, Music, 0u, 22u, "medic station music"),
            (new Vector3(123f, 392.9f, 617f), true, Name, 21u, 21u, "armor vendor platform stays Antaeus Hollow"),
            (new Vector3(101f, 421.1f, 542f), false, Name, 22u, 0u, "surface at the hollow mouth no longer reads Medic Station"),
            (new Vector3(230f, 274.7f, 120f), true, Sky, 0u, 30u, "control room floor"),
            (new Vector3(270f, 242.5f, 37f), true, Name, 0u, 30u, "lower tunnels"),
            (new Vector3(176f, 282.5f, 117f), false, Sky, 0u, 35u, "west overlook window bay"),
            (new Vector3(176f, 282.5f, 117f), false, Name, 0u, 35u, "window bay named"),
            (new Vector3(230f, 274.7f, 120f), true, Name, 0u, 30u, "control room name"),
            (new Vector3(160f, 241.5f, 37f), false, Sky, 0u, 0u, "pit floor outside the tunnel mouth stays outdoors"),
            (new Vector3(410f, 378.1f, 305f), true, Sky, 0u, 44u, "quarters floor"),
            (new Vector3(410f, 378.1f, 305f), true, Name, 0u, 44u, "quarters name"),
            (new Vector3(380f, 387.9f, 282f), false, Sky, 27u, 39u, "window bay (was the Mal Dys label circle)"),
            (new Vector3(380f, 387.9f, 282f), false, Name, 27u, 39u, "window bay name"),
            (new Vector3(543f, 408.2f, 220f), true, Sky, 38u, 38u, "cell block floor unchanged"),
            (new Vector3(560f, 476.1f, 243f), false, Sky, 38u, 27u, "surface above: was interior sky, now Mal Dys outdoors"),
            (new Vector3(551f, 417.9f, 165f), false, Name, 0u, 43u, "window bay"),
            (new Vector3(551f, 417.9f, 165f), false, Sky, 0u, 43u, "window bay sky"),
            (new Vector3(660f, 422.1f, 254f), true, Sky, 26u, 26u, "east interior room unchanged"),
            (new Vector3(720f, 456.1f, 254f), true, Sky, 26u, 26u, "east tunnel unchanged"),
            (new Vector3(757f, 470.5f, 307f), false, Sky, 26u, 27u, "east surface: interior sky to Mal Dys outdoor"),
            (new Vector3(757f, 470.5f, 307f), false, Name, 26u, 27u, "east surface keeps Mal Dys"),
            (new Vector3(525f, 326f, -6f), true, Sky, 28u, 28u, "lower control room unchanged"),
            (new Vector3(440f, 294.1f, -38f), true, Sky, 28u, 28u, "lower tunnels unchanged"),
            (new Vector3(500f, 343.8f, 50f), false, Sky, 28u, 27u, "lower city surface: interior sky to Mal Dys outdoor"),
            (new Vector3(470f, 356.5f, -90f), false, Name, 28u, 27u, "lower city surface keeps Mal Dys"),
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
            Assert.AreEqual(ValverdeDescentRegionSweep.Volumes.Length + ValverdeDescentRegionSweep.Recuts.Length, once.Except(before).Count(), "the new rows and the recut ones");

            foreach (var sql in ValverdeDescentRegionSweep.Up())
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

            foreach (var sql in ValverdeDescentRegionSweep.Up())
                StringAssert.Contains(migrator.GenerateScript(Previous, Migration), sql);
            foreach (var sql in ValverdeDescentRegionSweep.Down())
                StringAssert.Contains(migrator.GenerateScript(Migration, Previous), sql);
        }

        /// <summary>The highest-priority region here that turns the setting on, 0 for none (the map's own).</summary>
        private static uint Shown(List<MapRegion> volumes, Vector3 at, int flag, bool underground) =>
            volumes.Where(v => v.Enabled && Regions.TryGetValue(v.RegionId, out var r) && (r.Flags & flag) != 0 && v.Contains(at, underground))
                .OrderByDescending(v => Regions[v.RegionId].Priority).Select(v => v.RegionId).FirstOrDefault();

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == ValverdeDescentRegionSweep.MapContextId)
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT map_context_id || ' ' || region_id || ' ' || shape || ' ' || printf('%.1f %.1f %.1f %.1f %.1f %.1f %.1f %.1f', pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y) || ' ' || underground || ' ' || enabled || ' ' || comment AS Value " +
                    "FROM map_region WHERE map_context_id = 2047")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
