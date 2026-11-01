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

    /// <summary>The region sweep on the Bane Supply Depot (Bane_supply_depot_region_sweep, BaneSupplyDepotRegionSweep).</summary>
    [TestClass]
    [DoNotParallelize]
    public class BaneSupplyDepotRegionSweepTests
    {
        private const string Migration = "20270719040000_Bane_supply_depot_region_sweep";
        private const string Previous = "20270718000000_Purgas_station_gap_regions";

        private const int Sky = 1, Env = 2, Ambient = 4, Music = 8, Name = 16;

        /// <summary>The map's regions, as the .map has them: priority and the settings they turn on.</summary>
        private static readonly Dictionary<uint, (int Priority, int Flags)> Regions = new()
        {
            [1] = (1, 15), [6] = (2, 0), [3] = (3, 15), [4] = (4, 15), [5] = (5, 24), [2] = (6, 7), [7] = (7, 3), [8] = (8, 3), [9] = (9, 31),
        };

        /// <summary>Where each new or recut volume shows, and where the places beside it keep theirs.</summary>
        private static readonly (Vector3 At, bool Underground, int Flag, uint Before, uint After, string Why)[] Points =
        {
            (new Vector3(130f, 125.8f, 313f), false, Name, 0u, 5u, "Survivor's Camp by the wreck"),
            (new Vector3(115f, 129.3f, 335f), false, Music, 0u, 5u, "crashed dropship debris"),
            (new Vector3(0f, 127.2f, 0f), false, Sky, 7u, 0u, "open compound loses the interior sky"),
            (new Vector3(-150f, 122.9f, 100f), false, Env, 7u, 0u, "open swamp loses the interior env"),
            (new Vector3(-60f, 81f, -56f), true, Sky, 7u, 7u, "round chunnel hall keeps"),
            (new Vector3(-60f, 79.8f, -160f), true, Sky, 7u, 7u, "control room keeps"),
            (new Vector3(-12f, 99.8f, 220f), true, Env, 7u, 7u, "north chunnel keeps"),
            (new Vector3(208f, 131.8f, -148f), false, Sky, 8u, 0u, "ground above Depot HQ loses the interior sky"),
            (new Vector3(208f, 95.8f, -148f), true, Sky, 8u, 8u, "Depot HQ room keeps"),
            (new Vector3(208f, 97.6f, -215f), true, Env, 8u, 8u, "HQ service rooms keep"),
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
            Assert.AreEqual(BaneSupplyDepotRegionSweep.Volumes.Length + BaneSupplyDepotRegionSweep.Recuts.Length, once.Except(before).Count(), "the new rows and the recut ones");

            foreach (var sql in BaneSupplyDepotRegionSweep.Up())
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

            foreach (var sql in BaneSupplyDepotRegionSweep.Up())
                StringAssert.Contains(migrator.GenerateScript(Previous, Migration), sql);
            foreach (var sql in BaneSupplyDepotRegionSweep.Down())
                StringAssert.Contains(migrator.GenerateScript(Migration, Previous), sql);
        }

        /// <summary>The highest-priority region here that turns the setting on, 0 for none (the map's own).</summary>
        private static uint Shown(List<MapRegion> volumes, Vector3 at, int flag, bool underground) =>
            volumes.Where(v => v.Enabled && Regions.TryGetValue(v.RegionId, out var r) && (r.Flags & flag) != 0 && v.Contains(at, underground))
                .OrderByDescending(v => Regions[v.RegionId].Priority).Select(v => v.RegionId).FirstOrDefault();

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == BaneSupplyDepotRegionSweep.MapContextId)
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT map_context_id || ' ' || region_id || ' ' || shape || ' ' || printf('%.1f %.1f %.1f %.1f %.1f %.1f %.1f %.1f', pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y) || ' ' || underground || ' ' || enabled || ' ' || comment AS Value " +
                    "FROM map_region WHERE map_context_id = 1451")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
