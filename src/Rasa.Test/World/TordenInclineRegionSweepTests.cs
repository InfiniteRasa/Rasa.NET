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

    /// <summary>The region sweep on the Torden Incline (Torden_incline_region_sweep, TordenInclineRegionSweep).</summary>
    [TestClass]
    [DoNotParallelize]
    public class TordenInclineRegionSweepTests
    {
        private const string Migration = "20270724030000_Torden_incline_region_sweep";
        private const string Previous = "20270718000000_Purgas_station_gap_regions";

        private const int Sky = 1, Env = 2, Ambient = 4, Music = 8, Name = 16;

        /// <summary>The map's regions, as the .map has them: priority and the settings they turn on.</summary>
        private static readonly Dictionary<uint, (int Priority, int Flags)> Regions = new()
        {
            [1] = (1, 13), [4] = (2, 13), [5] = (3, 17), [6] = (4, 7), [7] = (5, 7), [8] = (6, 7), [3] = (7, 29), [9] = (8, 20), [2] = (9, 11),
            [13] = (10, 24), [11] = (11, 16), [15] = (12, 17), [16] = (13, 24), [17] = (14, 16), [18] = (15, 16), [19] = (16, 16), [20] = (17, 16),
            [21] = (18, 24), [22] = (19, 3), [26] = (20, 15), [24] = (21, 3), [23] = (22, 16), [12] = (23, 15), [27] = (24, 1), [25] = (25, 16),
            [14] = (26, 16), [28] = (27, 0),
        };

        /// <summary>Where each new or recut volume shows, and where the places beside it keep theirs.</summary>
        private static readonly (Vector3 At, bool Underground, int Flag, uint Before, uint After, string Why)[] Points =
        {
            (new Vector3(5f, 231.1f, -300f), true, Sky, 0u, 24u, "Brann corridor ramp"),
            (new Vector3(-45f, 213.9f, -335f), true, Env, 0u, 24u, "Brann corridor lower ramp by the instance door"),
            (new Vector3(5f, 233.9f, -285f), false, Sky, 0u, 0u, "Brann entrance mouth stays outdoors"),
            (new Vector3(-800f, 218.5f, -282f), true, Sky, 0u, 26u, "west Eloh shrine room"),
            (new Vector3(-775f, 227.5f, -282f), true, Music, 0u, 26u, "west Eloh ramp"),
            (new Vector3(-752f, 234.4f, -282f), false, Sky, 0u, 0u, "Eloh door mouth stays outdoors"),
            (new Vector3(1180f, 296.5f, 400f), true, Ambient, 0u, 26u, "east Eloh corridor"),
            (new Vector3(1024f, 281.5f, 400f), true, Sky, 0u, 22u, "east rocky cave"),
            (new Vector3(1072f, 295.5f, 392f), true, Env, 0u, 22u, "east cave branch"),
            (new Vector3(1110f, 297.1f, 410f), true, Sky, 0u, 22u, "east cave split before the Eloh room"),
            (new Vector3(1000f, 307.7f, 488f), false, Sky, 0u, 0u, "north cave mouth outside"),
            (new Vector3(100f, 226.5f, -393f), true, Sky, 0u, 22u, "south rocky cave"),
            (new Vector3(-360f, 217.5f, -540f), true, Sky, 0u, 22u, "Mires Post rocky cave"),
            (new Vector3(360f, 239.3f, -275f), false, Sky, 0u, 27u, "Wardenbot entry corridor"),
            (new Vector3(392f, 279.3f, 373f), false, Sky, 12u, 0u, "Control Point Badlands surface was Bane interior"),
            (new Vector3(430f, 279.5f, 445f), false, Music, 12u, 0u, "badlands surface over the complex"),
            (new Vector3(392f, 248.1f, 472f), true, Sky, 12u, 12u, "Bane complex tunnels keep region 12"),
            (new Vector3(392f, 246.9f, 640f), true, Ambient, 12u, 12u, "Bane chunnel room keeps region 12"),
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
            Assert.AreEqual(TordenInclineRegionSweep.Volumes.Length + TordenInclineRegionSweep.Recuts.Length, once.Except(before).Count(), "the new rows and the recut ones");

            foreach (var sql in TordenInclineRegionSweep.Up())
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

            foreach (var sql in TordenInclineRegionSweep.Up())
                StringAssert.Contains(migrator.GenerateScript(Previous, Migration), sql);
            foreach (var sql in TordenInclineRegionSweep.Down())
                StringAssert.Contains(migrator.GenerateScript(Migration, Previous), sql);
        }

        /// <summary>The highest-priority region here that turns the setting on, 0 for none (the map's own).</summary>
        private static uint Shown(List<MapRegion> volumes, Vector3 at, int flag, bool underground) =>
            volumes.Where(v => v.Enabled && Regions.TryGetValue(v.RegionId, out var r) && (r.Flags & flag) != 0 && v.Contains(at, underground))
                .OrderByDescending(v => Regions[v.RegionId].Priority).Select(v => v.RegionId).FirstOrDefault();

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == TordenInclineRegionSweep.MapContextId)
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT map_context_id || ' ' || region_id || ' ' || shape || ' ' || printf('%.1f %.1f %.1f %.1f %.1f %.1f %.1f %.1f', pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y) || ' ' || underground || ' ' || enabled || ' ' || comment AS Value " +
                    "FROM map_region WHERE map_context_id = 1761")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
