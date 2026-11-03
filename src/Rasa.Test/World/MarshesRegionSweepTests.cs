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

    /// <summary>The region sweep on the Valverde Marshes (Marshes_region_sweep, MarshesRegionSweep).</summary>
    [TestClass]
    [DoNotParallelize]
    public class MarshesRegionSweepTests
    {
        private const string Migration = "20270719000000_Marshes_region_sweep";
        private const string Previous = "20270718000000_Purgas_station_gap_regions";

        private const int Sky = 1, Env = 2, Ambient = 4, Music = 8, Name = 16;

        /// <summary>The map's regions, as the .map has them: priority and the settings they turn on.</summary>
        private static readonly Dictionary<uint, (int Priority, int Flags)> Regions = new()
        {
            [1] = (1, 13), [2] = (2, 15), [7] = (3, 16), [8] = (4, 29), [9] = (5, 16), [10] = (6, 16), [11] = (7, 16), [12] = (8, 16), [13] = (9, 29),
            [14] = (10, 16), [15] = (11, 24), [16] = (12, 28), [28] = (13, 1), [17] = (14, 28), [18] = (15, 28), [19] = (16, 28), [20] = (17, 28),
            [21] = (18, 24), [22] = (19, 16), [23] = (20, 16), [29] = (21, 3), [4] = (22, 1), [25] = (23, 29), [24] = (24, 29), [26] = (25, 28),
            [27] = (26, 29), [30] = (27, 0), [31] = (28, 0), [32] = (29, 0),
        };

        /// <summary>Where each new or recut volume shows, and where the places beside it keep theirs.</summary>
        private static readonly (Vector3 At, bool Underground, int Flag, uint Before, uint After, string Why)[] Points =
        {
            (new Vector3(-141f, 219.4f, 615f), false, Name, 22u, 11u, "Falcon Hold hospital, was Northern Mud Pit"),
            (new Vector3(-120f, 220.6f, 590f), false, Name, 22u, 11u, "Falcon Hold tower"),
            (new Vector3(-180f, 219.4f, 600f), false, Name, 0u, 11u, "Falcon Hold west yard"),
            (new Vector3(-200f, 226.2f, 620f), false, Name, 0u, 11u, "Falcon Hold catwalk"),
            (new Vector3(22.9f, 217f, 525.5f), false, Name, 22u, 22u, "Northern Mud Pit label keeps"),
            (new Vector3(210f, 214.2f, 350f), false, Name, 0u, 22u, "Northern Mud Pit box corner"),
            (new Vector3(-85f, 216.3f, 700f), false, Name, 0u, 22u, "Northern Mud Pit north-west corner"),
            (new Vector3(196f, 219.8f, -669f), false, Name, 0u, 17u, "Bane Refueling Station label"),
            (new Vector3(150f, 219.2f, -700f), false, Ambient, 0u, 17u, "refueling compound west"),
            (new Vector3(250f, 220.8f, -600f), false, Music, 0u, 17u, "refueling compound north-east"),
            (new Vector3(384f, 222.9f, -752f), true, Name, 18u, 18u, "Repair Station keeps its box"),
            (new Vector3(-160f, 112.2f, -868f), true, Sky, 0u, 29u, "Eloh spirit room"),
            (new Vector3(-200f, 112.2f, -850f), true, Env, 0u, 29u, "Eloh spirit room west"),
            (new Vector3(-160f, 213.8f, -868f), false, Sky, 13u, 13u, "surface above keeps Retread City sky"),
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
            Assert.AreEqual(MarshesRegionSweep.Volumes.Length + MarshesRegionSweep.Recuts.Length, once.Except(before).Count(), "the new rows and the recut ones");

            foreach (var sql in MarshesRegionSweep.Up())
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

            foreach (var sql in MarshesRegionSweep.Up())
                StringAssert.Contains(migrator.GenerateScript(Previous, Migration), sql);
            foreach (var sql in MarshesRegionSweep.Down())
                StringAssert.Contains(migrator.GenerateScript(Migration, Previous), sql);
        }

        /// <summary>The highest-priority region here that turns the setting on, 0 for none (the map's own).</summary>
        private static uint Shown(List<MapRegion> volumes, Vector3 at, int flag, bool underground) =>
            volumes.Where(v => v.Enabled && Regions.TryGetValue(v.RegionId, out var r) && (r.Flags & flag) != 0 && v.Contains(at, underground))
                .OrderByDescending(v => Regions[v.RegionId].Priority).Select(v => v.RegionId).FirstOrDefault();

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == MarshesRegionSweep.MapContextId)
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT map_context_id || ' ' || region_id || ' ' || shape || ' ' || printf('%.1f %.1f %.1f %.1f %.1f %.1f %.1f %.1f', pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y) || ' ' || underground || ' ' || enabled || ' ' || comment AS Value " +
                    "FROM map_region WHERE map_context_id = 1454")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
