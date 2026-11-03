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

    /// <summary>The region sweep on the Forbidden Temples (Eloh_temples_region_sweep, ElohTemplesRegionSweep).</summary>
    [TestClass]
    [DoNotParallelize]
    public class ElohTemplesRegionSweepTests
    {
        private const string Migration = "20270725010000_Eloh_temples_region_sweep";
        private const string Previous = "20270718000000_Purgas_station_gap_regions";

        private const int Sky = 1, Env = 2, Ambient = 4, Music = 8, Name = 16;

        /// <summary>The map's regions, as the .map has them: priority and the settings they turn on.</summary>
        private static readonly Dictionary<uint, (int Priority, int Flags)> Regions = new()
        {
            [2] = (1, 13), [11] = (2, 19), [7] = (3, 19), [8] = (4, 19), [9] = (5, 19), [10] = (6, 19), [3] = (7, 19), [4] = (8, 19), [5] = (9, 19),
            [6] = (10, 19),
        };

        /// <summary>Where each new or recut volume shows, and where the places beside it keep theirs.</summary>
        private static readonly (Vector3 At, bool Underground, int Flag, uint Before, uint After, string Why)[] Points =
        {
            (new Vector3(-600f, -7.6f, 132f), false, Name, 4u, 3u, "Raging west spawner tee (mid), was Proud"),
            (new Vector3(-600f, 8.4f, -60f), false, Name, 4u, 3u, "Raging west spawner tee (low), was Proud"),
            (new Vector3(-600f, -23.6f, 324f), false, Sky, 0u, 3u, "Raging west spawner tee (upper), was default"),
            (new Vector3(-425f, -23.6f, 324f), false, Name, 0u, 3u, "Raging east spawner tee (upper)"),
            (new Vector3(-425f, 8.5f, -60f), false, Sky, 0u, 3u, "Raging east spawner tee (low)"),
            (new Vector3(-425f, -7.6f, 132f), false, Env, 0u, 3u, "Raging east spawner tee (mid)"),
            (new Vector3(-610f, -7.6f, 32f), false, Name, 4u, 4u, "Proud east room stays Proud"),
            (new Vector3(-610f, -7.6f, 32f), false, Sky, 4u, 4u, "Proud east room keeps fog-far sky"),
            (new Vector3(-630f, -7.6f, 32f), false, Name, 4u, 4u, "Proud east room door side"),
            (new Vector3(-650f, -7.5f, 40f), false, Name, 4u, 4u, "Proud corridor turn stays Proud"),
            (new Vector3(-740f, -7.8f, 35f), false, Name, 4u, 4u, "Proud round room stays Proud"),
            (new Vector3(-512f, -7.3f, 132f), false, Name, 3u, 3u, "Raging body room unchanged"),
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
            Assert.AreEqual(ElohTemplesRegionSweep.Volumes.Length + ElohTemplesRegionSweep.Recuts.Length, once.Except(before).Count(), "the new rows and the recut ones");

            foreach (var sql in ElohTemplesRegionSweep.Up())
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

            foreach (var sql in ElohTemplesRegionSweep.Up())
                StringAssert.Contains(migrator.GenerateScript(Previous, Migration), sql);
            foreach (var sql in ElohTemplesRegionSweep.Down())
                StringAssert.Contains(migrator.GenerateScript(Migration, Previous), sql);
        }

        /// <summary>The highest-priority region here that turns the setting on, 0 for none (the map's own).</summary>
        private static uint Shown(List<MapRegion> volumes, Vector3 at, int flag, bool underground) =>
            volumes.Where(v => v.Enabled && Regions.TryGetValue(v.RegionId, out var r) && (r.Flags & flag) != 0 && v.Contains(at, underground))
                .OrderByDescending(v => Regions[v.RegionId].Priority).Select(v => v.RegionId).FirstOrDefault();

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == ElohTemplesRegionSweep.MapContextId)
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT map_context_id || ' ' || region_id || ' ' || shape || ' ' || printf('%.1f %.1f %.1f %.1f %.1f %.1f %.1f %.1f', pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y) || ' ' || underground || ' ' || enabled || ' ' || comment AS Value " +
                    "FROM map_region WHERE map_context_id = 1803")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
