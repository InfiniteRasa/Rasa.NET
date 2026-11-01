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

    /// <summary>The region sweep on the Kardash Atta Colony (Kardash_atta_colony_region_sweep, KardashAttaColonyRegionSweep).</summary>
    [TestClass]
    [DoNotParallelize]
    public class KardashAttaColonyRegionSweepTests
    {
        private const string Migration = "20270724060000_Kardash_atta_colony_region_sweep";
        private const string Previous = "20270718000000_Purgas_station_gap_regions";

        private const int Sky = 1, Env = 2, Ambient = 4, Music = 8, Name = 16;

        /// <summary>The map's regions, as the .map has them: priority and the settings they turn on.</summary>
        private static readonly Dictionary<uint, (int Priority, int Flags)> Regions = new()
        {
            [23] = (1, 0), [22] = (2, 23), [17] = (3, 23), [18] = (4, 23), [19] = (5, 23), [21] = (6, 23), [12] = (7, 0), [14] = (8, 0), [13] = (9, 0),
            [2] = (10, 23), [3] = (11, 23), [4] = (12, 23), [5] = (13, 23), [6] = (14, 23), [20] = (15, 23), [7] = (16, 31), [8] = (17, 23),
            [24] = (18, 3), [25] = (19, 3), [9] = (20, 23), [10] = (21, 19), [11] = (22, 23), [15] = (23, 19), [16] = (24, 0), [1] = (25, 13),
        };

        /// <summary>Where each new or recut volume shows, and where the places beside it keep theirs.</summary>
        private static readonly (Vector3 At, bool Underground, int Flag, uint Before, uint After, string Why)[] Points =
        {
            (new Vector3(8f, 272.9f, -200f), true, Name, 0u, 17u, "Alpha corridor cross"),
            (new Vector3(8f, 272.9f, -180f), true, Sky, 0u, 17u, "Alpha airlock"),
            (new Vector3(0f, 274f, -37f), false, Name, 3u, 3u, "Outer Nest unchanged"),
            (new Vector3(-164f, 224.9f, 216f), false, Name, 0u, 19u, "Gamma cross group"),
            (new Vector3(-172f, 225.3f, 180f), false, Name, 19u, 19u, "Gamma front, already covered"),
            (new Vector3(-141f, 201.9f, 80f), false, Name, 4u, 4u, "Shining Cavern unchanged"),
            (new Vector3(340f, 100.9f, -436f), true, Name, 0u, 20u, "Delta west corridors"),
            (new Vector3(344f, 100.9f, -448f), true, Ambient, 0u, 20u, "Delta corridor ambient"),
            (new Vector3(419f, 109.1f, -414f), false, Name, 6u, 6u, "Inner Nest unchanged"),
            (new Vector3(436f, 188.9f, 290f), true, Name, 0u, 18u, "Beta rooms under the dome"),
            (new Vector3(445f, 188.9f, 300f), true, Sky, 0u, 18u, "Beta rooms sky"),
            (new Vector3(408f, 198.7f, 282f), false, Name, 18u, 18u, "Beta dome unchanged"),
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
            Assert.AreEqual(KardashAttaColonyRegionSweep.Volumes.Length + KardashAttaColonyRegionSweep.Recuts.Length, once.Except(before).Count(), "the new rows and the recut ones");

            foreach (var sql in KardashAttaColonyRegionSweep.Up())
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

            foreach (var sql in KardashAttaColonyRegionSweep.Up())
                StringAssert.Contains(migrator.GenerateScript(Previous, Migration), sql);
            foreach (var sql in KardashAttaColonyRegionSweep.Down())
                StringAssert.Contains(migrator.GenerateScript(Migration, Previous), sql);
        }

        /// <summary>The highest-priority region here that turns the setting on, 0 for none (the map's own).</summary>
        private static uint Shown(List<MapRegion> volumes, Vector3 at, int flag, bool underground) =>
            volumes.Where(v => v.Enabled && Regions.TryGetValue(v.RegionId, out var r) && (r.Flags & flag) != 0 && v.Contains(at, underground))
                .OrderByDescending(v => Regions[v.RegionId].Priority).Select(v => v.RegionId).FirstOrDefault();

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == KardashAttaColonyRegionSweep.MapContextId)
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT map_context_id || ' ' || region_id || ' ' || shape || ' ' || printf('%.1f %.1f %.1f %.1f %.1f %.1f %.1f %.1f', pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y) || ' ' || underground || ' ' || enabled || ' ' || comment AS Value " +
                    "FROM map_region WHERE map_context_id = 1773")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
