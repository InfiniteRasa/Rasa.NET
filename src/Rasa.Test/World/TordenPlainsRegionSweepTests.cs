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

    /// <summary>The region sweep on the Torden Plains (Torden_plains_region_sweep, TordenPlainsRegionSweep).</summary>
    [TestClass]
    [DoNotParallelize]
    public class TordenPlainsRegionSweepTests
    {
        private const string Migration = "20270724050000_Torden_plains_region_sweep";
        private const string Previous = "20270718000000_Purgas_station_gap_regions";

        private const int Sky = 1, Env = 2, Ambient = 4, Music = 8, Name = 16;

        /// <summary>The map's regions, as the .map has them: priority and the settings they turn on.</summary>
        private static readonly Dictionary<uint, (int Priority, int Flags)> Regions = new()
        {
            [1] = (1, 13), [8] = (2, 5), [4] = (3, 29), [14] = (4, 21), [20] = (5, 29), [15] = (6, 17), [17] = (7, 16), [26] = (8, 17), [19] = (9, 17),
            [21] = (10, 31), [18] = (11, 25), [12] = (12, 7), [2] = (13, 29), [3] = (14, 21), [7] = (15, 31), [13] = (16, 29), [5] = (17, 21),
            [32] = (18, 3), [25] = (19, 22), [6] = (20, 23), [9] = (21, 31), [22] = (22, 0), [23] = (23, 0), [24] = (24, 0), [10] = (25, 17),
            [11] = (26, 17), [16] = (27, 1), [31] = (28, 16), [27] = (29, 16), [29] = (30, 16), [34] = (31, 16), [33] = (32, 0),
        };

        /// <summary>Where each new or recut volume shows, and where the places beside it keep theirs.</summary>
        private static readonly (Vector3 At, bool Underground, int Flag, uint Before, uint After, string Why)[] Points =
        {
            (new Vector3(400f, 88.7f, -800f), false, Sky, 0u, 16u, "abyss floor south-east"),
            (new Vector3(0f, 88.8f, -700f), false, Sky, 15u, 16u, "abyss under the Abyssal Zone circle"),
            (new Vector3(0f, 88.8f, -700f), false, Name, 34u, 34u, "name unchanged (CP Abyssal Zone)"),
            (new Vector3(-960.1f, 93.2f, -429.6f), false, Sky, 26u, 16u, "foot of the Firefall lava falls"),
            (new Vector3(-125f, 369f, -625f), false, Sky, 15u, 15u, "Abyssal Zone shelf above 330 kept"),
            (new Vector3(182.5f, 433.8f, 249f), false, Sky, 4u, 4u, "Geyser Chimney Basin unchanged"),
            (new Vector3(-1150f, 380.7f, -280f), true, Sky, 12u, 12u, "lava cave unchanged"),
            (new Vector3(689f, 381.4f, 440f), true, Sky, 0u, 0u, "Brann room by the canyon unchanged"),
            (new Vector3(786f, 430.6f, 566f), false, Sky, 0u, 20u, "Support Facility yard gets its sky"),
            (new Vector3(786f, 430.6f, 566f), false, Music, 0u, 20u, "Support Facility music"),
            (new Vector3(786f, 430.6f, 566f), false, Name, 31u, 31u, "control point name still on top"),
            (new Vector3(430f, 428.2f, -91f), false, Name, 20u, 0u, "ground east of the colony no longer misnamed"),
            (new Vector3(375f, 434.1f, -91f), false, Music, 20u, 0u, "auction house loses the stray music"),
            (new Vector3(375f, 434.1f, -91f), false, Name, 5u, 5u, "auction house stays Irendas Penal Colony"),
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
            Assert.AreEqual(TordenPlainsRegionSweep.Volumes.Length + TordenPlainsRegionSweep.Recuts.Length, once.Except(before).Count(), "the new rows and the recut ones");

            foreach (var sql in TordenPlainsRegionSweep.Up())
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

            foreach (var sql in TordenPlainsRegionSweep.Up())
                StringAssert.Contains(migrator.GenerateScript(Previous, Migration), sql);
            foreach (var sql in TordenPlainsRegionSweep.Down())
                StringAssert.Contains(migrator.GenerateScript(Migration, Previous), sql);
        }

        /// <summary>The highest-priority region here that turns the setting on, 0 for none (the map's own).</summary>
        private static uint Shown(List<MapRegion> volumes, Vector3 at, int flag, bool underground) =>
            volumes.Where(v => v.Enabled && Regions.TryGetValue(v.RegionId, out var r) && (r.Flags & flag) != 0 && v.Contains(at, underground))
                .OrderByDescending(v => Regions[v.RegionId].Priority).Select(v => v.RegionId).FirstOrDefault();

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == TordenPlainsRegionSweep.MapContextId)
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT map_context_id || ' ' || region_id || ' ' || shape || ' ' || printf('%.1f %.1f %.1f %.1f %.1f %.1f %.1f %.1f', pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y) || ' ' || underground || ' ' || enabled || ' ' || comment AS Value " +
                    "FROM map_region WHERE map_context_id = 1764")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
