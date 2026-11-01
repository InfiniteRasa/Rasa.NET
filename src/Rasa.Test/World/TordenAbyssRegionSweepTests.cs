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

    /// <summary>The region sweep on Torden Abyss (Torden_abyss_region_sweep, TordenAbyssRegionSweep).</summary>
    [TestClass]
    [DoNotParallelize]
    public class TordenAbyssRegionSweepTests
    {
        private const string Migration = "20270724000000_Torden_abyss_region_sweep";
        private const string Previous = "20270718000000_Purgas_station_gap_regions";

        private const int Sky = 1, Env = 2, Ambient = 4, Music = 8, Name = 16;

        /// <summary>The map's regions, as the .map has them: priority and the settings they turn on.</summary>
        private static readonly Dictionary<uint, (int Priority, int Flags)> Regions = new()
        {
            [1] = (1, 13), [5] = (2, 31), [13] = (3, 29), [14] = (4, 29), [16] = (5, 29), [6] = (6, 31), [44] = (7, 31), [33] = (8, 31), [34] = (9, 31),
            [28] = (10, 20), [29] = (11, 20), [18] = (12, 31), [22] = (13, 31), [23] = (14, 31), [24] = (15, 31), [25] = (16, 31), [26] = (17, 31),
            [40] = (18, 31), [41] = (19, 31), [42] = (20, 31), [45] = (21, 29), [35] = (22, 25), [37] = (23, 17), [36] = (24, 17), [43] = (25, 31),
            [20] = (26, 29), [21] = (27, 7), [17] = (28, 31), [31] = (29, 24), [32] = (30, 24), [38] = (31, 0), [39] = (32, 19), [15] = (33, 31),
        };

        /// <summary>Where each new or recut volume shows, and where the places beside it keep theirs.</summary>
        private static readonly (Vector3 At, bool Underground, int Flag, uint Before, uint After, string Why)[] Points =
        {
            (new Vector3(-308f, 538.8f, -41f), false, Name, 0u, 34u, "The Pitchfork tavern"),
            (new Vector3(-308f, 538.8f, -41f), false, Ambient, 0u, 34u, "tavern bar ambience"),
            (new Vector3(-320f, 538.4f, -48f), false, Sky, 44u, 34u, "tavern west tables (were Tantalus Base)"),
            (new Vector3(-400f, 535.8f, 100f), false, Name, 0u, 6u, "north end of the Tantalus lobe"),
            (new Vector3(-400f, 545.3f, -400f), false, Music, 0u, 6u, "south end of the Tantalus lobe"),
            (new Vector3(-300f, 545.5f, -100f), false, Name, 44u, 44u, "Tantalus Base keeps its circle"),
            (new Vector3(-398f, 532.4f, -176f), false, Music, 44u, 44u, "base centre keeps safe-zone music"),
            (new Vector3(-421.5f, 537f, -14f), false, Name, 33u, 33u, "hospital unchanged"),
            (new Vector3(-330f, 535.4f, 120f), true, Name, 0u, 6u, "lobe north, flagged underground"),
            (new Vector3(400f, 554.2f, -200f), false, Name, 0u, 40u, "Tampeii Cavern south"),
            (new Vector3(220f, 531.1f, -50f), false, Sky, 0u, 40u, "Tampeii Cavern west"),
            (new Vector3(300f, 537.1f, -30f), false, Sky, 42u, 40u, "open cave over the settlement rooms"),
            (new Vector3(290f, 505.8f, -77f), true, Name, 42u, 42u, "settlement medical station"),
            (new Vector3(344f, 529.9f, -80f), false, Sky, 42u, 42u, "settlement dome entrance"),
            (new Vector3(507f, 573.3f, 60f), false, Sky, 43u, 41u, "Ruins of Tampeii north half was Dybukkar"),
            (new Vector3(400f, 554.1f, 100f), false, Name, 43u, 40u, "Tampeii lobe north was Dybukkar"),
            (new Vector3(445.9f, 506f, 201.2f), true, Name, 43u, 43u, "Dybukkar Underground complex unchanged"),
            (new Vector3(560f, 574.3f, 70f), true, Sky, 0u, 21u, "Brann gateway dome ramp"),
            (new Vector3(574f, 569.8f, 79f), true, Ambient, 0u, 21u, "gateway instance door"),
            (new Vector3(553f, 577.9f, 60f), false, Sky, 43u, 41u, "gateway entrance mouth stays outdoors"),
            (new Vector3(-121f, 554.9f, 292f), false, Name, 0u, 37u, "Archeron Outpost camp"),
            (new Vector3(-137f, 554.8f, 308f), false, Name, 0u, 37u, "Archeron Outpost infirmary"),
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
            Assert.AreEqual(TordenAbyssRegionSweep.Volumes.Length + TordenAbyssRegionSweep.Recuts.Length, once.Except(before).Count(), "the new rows and the recut ones");

            foreach (var sql in TordenAbyssRegionSweep.Up())
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

            foreach (var sql in TordenAbyssRegionSweep.Up())
                StringAssert.Contains(migrator.GenerateScript(Previous, Migration), sql);
            foreach (var sql in TordenAbyssRegionSweep.Down())
                StringAssert.Contains(migrator.GenerateScript(Migration, Previous), sql);
        }

        /// <summary>The highest-priority region here that turns the setting on, 0 for none (the map's own).</summary>
        private static uint Shown(List<MapRegion> volumes, Vector3 at, int flag, bool underground) =>
            volumes.Where(v => v.Enabled && Regions.TryGetValue(v.RegionId, out var r) && (r.Flags & flag) != 0 && v.Contains(at, underground))
                .OrderByDescending(v => Regions[v.RegionId].Priority).Select(v => v.RegionId).FirstOrDefault();

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == TordenAbyssRegionSweep.MapContextId)
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT map_context_id || ' ' || region_id || ' ' || shape || ' ' || printf('%.1f %.1f %.1f %.1f %.1f %.1f %.1f %.1f', pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y) || ' ' || underground || ' ' || enabled || ' ' || comment AS Value " +
                    "FROM map_region WHERE map_context_id = 2028")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
