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

    /// <summary>The region sweep on the Proving Grounds (Proving_grounds_region_sweep, ProvingGroundsRegionSweep).</summary>
    [TestClass]
    [DoNotParallelize]
    public class ProvingGroundsRegionSweepTests
    {
        private const string Migration = "20270726030000_Proving_grounds_region_sweep";
        private const string Previous = "20270718000000_Purgas_station_gap_regions";

        private const int Sky = 1, Env = 2, Ambient = 4, Music = 8, Name = 16;

        /// <summary>The map's regions, as the .map has them: priority and the settings they turn on.</summary>
        private static readonly Dictionary<uint, (int Priority, int Flags)> Regions = new()
        {
            [2] = (1, 13), [16] = (2, 23), [7] = (3, 0), [8] = (4, 0), [9] = (5, 0), [1] = (6, 28), [3] = (7, 23), [4] = (8, 23), [12] = (9, 16),
            [15] = (10, 16), [13] = (11, 16), [10] = (12, 16), [14] = (13, 16), [11] = (14, 16),
        };

        /// <summary>Where each new or recut volume shows, and where the places beside it keep theirs.</summary>
        private static readonly (Vector3 At, bool Underground, int Flag, uint Before, uint After, string Why)[] Points =
        {
            (new Vector3(-402f, 420.6f, 164f), false, Sky, 0u, 4u, "Blue HQ cross, lower floor"),
            (new Vector3(-405f, 428.6f, 164f), false, Ambient, 0u, 4u, "Blue HQ upper floor"),
            (new Vector3(-425f, 420.6f, 162f), false, Env, 0u, 4u, "Blue HQ west corridor"),
            (new Vector3(-402f, 420.6f, 174f), false, Sky, 0u, 4u, "Blue Team hospital arm"),
            (new Vector3(-402f, 418.8f, 148f), false, Sky, 0u, 0u, "ground just south of Blue HQ"),
            (new Vector3(-360f, 421.5f, 164f), false, Sky, 0u, 0u, "arena in front of Blue HQ keeps default sky"),
            (new Vector3(282f, 420.6f, 172f), false, Sky, 0u, 3u, "Red HQ cross, lower floor"),
            (new Vector3(284f, 428.6f, 172f), false, Ambient, 0u, 3u, "Red HQ upper floor"),
            (new Vector3(296f, 420.6f, 172f), false, Env, 0u, 3u, "Red HQ east corridor"),
            (new Vector3(282f, 420.6f, 163f), false, Sky, 0u, 3u, "Red Team hospital arm"),
            (new Vector3(240f, 421.8f, 172f), false, Sky, 0u, 0u, "arena in front of Red HQ keeps default sky"),
            (new Vector3(40f, 359.8f, -20f), true, Name, 0u, 16u, "Staging Area hall floor"),
            (new Vector3(40f, 359.8f, -20f), true, Sky, 0u, 16u, "Staging Area hall floor"),
            (new Vector3(34f, 376.2f, 9f), true, Ambient, 0u, 16u, "Staging Area office ring"),
            (new Vector3(0f, 422.8f, 100f), false, Name, 0u, 1u, "open arena"),
            (new Vector3(-60f, 430.1f, 168f), false, Music, 0u, 1u, "Control Point Charlie"),
            (new Vector3(-402f, 420.6f, 164f), false, Music, 0u, 1u, "Blue HQ takes the arena music"),
            (new Vector3(40f, 359.8f, -20f), true, Music, 0u, 0u, "staging hall keeps map music"),
            (new Vector3(216f, 428.6f, 345f), false, Name, 0u, 10u, "Red north tunnel"),
            (new Vector3(277f, 428.6f, 36f), false, Name, 0u, 11u, "Red south tunnel"),
            (new Vector3(282f, 420.6f, 172f), false, Name, 0u, 12u, "Red HQ name like Blue HQ"),
            (new Vector3(-397f, 428.6f, 299f), false, Name, 10u, 13u, "Blue north tunnel shows Blue name"),
            (new Vector3(-336f, 428.6f, -9f), false, Name, 11u, 14u, "Blue south tunnel shows Blue name"),
            (new Vector3(-402f, 420.6f, 164f), false, Name, 15u, 15u, "Blue HQ name unchanged"),
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
            Assert.AreEqual(ProvingGroundsRegionSweep.Volumes.Length + ProvingGroundsRegionSweep.Recuts.Length, once.Except(before).Count(), "the new rows and the recut ones");

            foreach (var sql in ProvingGroundsRegionSweep.Up())
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

            foreach (var sql in ProvingGroundsRegionSweep.Up())
                StringAssert.Contains(migrator.GenerateScript(Previous, Migration), sql);
            foreach (var sql in ProvingGroundsRegionSweep.Down())
                StringAssert.Contains(migrator.GenerateScript(Migration, Previous), sql);
        }

        /// <summary>The highest-priority region here that turns the setting on, 0 for none (the map's own).</summary>
        private static uint Shown(List<MapRegion> volumes, Vector3 at, int flag, bool underground) =>
            volumes.Where(v => v.Enabled && Regions.TryGetValue(v.RegionId, out var r) && (r.Flags & flag) != 0 && v.Contains(at, underground))
                .OrderByDescending(v => Regions[v.RegionId].Priority).Select(v => v.RegionId).FirstOrDefault();

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == ProvingGroundsRegionSweep.MapContextId)
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT map_context_id || ' ' || region_id || ' ' || shape || ' ' || printf('%.1f %.1f %.1f %.1f %.1f %.1f %.1f %.1f', pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y) || ' ' || underground || ' ' || enabled || ' ' || comment AS Value " +
                    "FROM map_region WHERE map_context_id = 2361")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
