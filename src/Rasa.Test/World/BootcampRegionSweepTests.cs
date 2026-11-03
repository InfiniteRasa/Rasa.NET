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

    /// <summary>The region sweep on Bootcamp (Luna) (Bootcamp_region_sweep, BootcampRegionSweep).</summary>
    [TestClass]
    [DoNotParallelize]
    public class BootcampRegionSweepTests
    {
        private const string Migration = "20270726040000_Bootcamp_region_sweep";
        private const string Previous = "20270718000000_Purgas_station_gap_regions";

        private const int Sky = 1, Env = 2, Ambient = 4, Music = 8, Name = 16;

        /// <summary>The map's regions, as the .map has them: priority and the settings they turn on.</summary>
        private static readonly Dictionary<uint, (int Priority, int Flags)> Regions = new()
        {
            [1] = (1, 15), [2] = (2, 31), [10] = (3, 31), [9] = (4, 31), [4] = (5, 31), [5] = (6, 31),
        };

        /// <summary>Where each new or recut volume shows, and where the places beside it keep theirs.</summary>
        private static readonly (Vector3 At, bool Underground, int Flag, uint Before, uint After, string Why)[] Points =
        {
            (new Vector3(79f, 110.1f, 180f), false, Sky, 4u, 5u, "barracks interior"),
            (new Vector3(79f, 110.1f, 180f), false, Ambient, 4u, 5u, "barracks interior room tone"),
            (new Vector3(86f, 109.2f, 189f), false, Sky, 4u, 5u, "barracks back corner"),
            (new Vector3(115f, 109.5f, 108f), false, Sky, 4u, 5u, "hospital interior"),
            (new Vector3(110f, 109.5f, 118f), false, Env, 4u, 5u, "hospital near door"),
            (new Vector3(95f, 109.6f, 140f), false, Sky, 4u, 4u, "outpost courtyard stays outdoors"),
            (new Vector3(70f, 109.2f, 160f), false, Sky, 4u, 4u, "just south of the barracks"),
            (new Vector3(79f, 117.9f, 178f), false, Sky, 4u, 4u, "barracks roof stays outdoors"),
            (new Vector3(116f, 115.3f, 109f), false, Sky, 4u, 4u, "hospital roof stays outdoors"),
            (new Vector3(-200f, 79.1f, 60f), true, Name, 0u, 4u, "west caldera lake (flagged)"),
            (new Vector3(100f, 79.1f, 20f), true, Sky, 0u, 4u, "east caldera pond (flagged)"),
            (new Vector3(-100f, 79.1f, -80f), true, Ambient, 0u, 4u, "drop-zone lake (flagged)"),
            (new Vector3(450f, 116.1f, 50f), true, Name, 0u, 0u, "Luna Cavern pond keeps the map default"),
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
            Assert.AreEqual(BootcampRegionSweep.Volumes.Length + BootcampRegionSweep.Recuts.Length, once.Except(before).Count(), "the new rows and the recut ones");

            foreach (var sql in BootcampRegionSweep.Up())
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

            foreach (var sql in BootcampRegionSweep.Up())
                StringAssert.Contains(migrator.GenerateScript(Previous, Migration), sql);
            foreach (var sql in BootcampRegionSweep.Down())
                StringAssert.Contains(migrator.GenerateScript(Migration, Previous), sql);
        }

        /// <summary>The highest-priority region here that turns the setting on, 0 for none (the map's own).</summary>
        private static uint Shown(List<MapRegion> volumes, Vector3 at, int flag, bool underground) =>
            volumes.Where(v => v.Enabled && Regions.TryGetValue(v.RegionId, out var r) && (r.Flags & flag) != 0 && v.Contains(at, underground))
                .OrderByDescending(v => Regions[v.RegionId].Priority).Select(v => v.RegionId).FirstOrDefault();

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == BootcampRegionSweep.MapContextId)
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT map_context_id || ' ' || region_id || ' ' || shape || ' ' || printf('%.1f %.1f %.1f %.1f %.1f %.1f %.1f %.1f', pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y) || ' ' || underground || ' ' || enabled || ' ' || comment AS Value " +
                    "FROM map_region WHERE map_context_id = 1985")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
