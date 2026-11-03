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

    /// <summary>The region sweep on Velon Hollow (the Death Burrow) (Velon_hollow_region_sweep, VelonHollowRegionSweep).</summary>
    [TestClass]
    [DoNotParallelize]
    public class VelonHollowRegionSweepTests
    {
        private const string Migration = "20270720020000_Velon_hollow_region_sweep";
        private const string Previous = "20270718000000_Purgas_station_gap_regions";

        private const int Sky = 1, Env = 2, Ambient = 4, Music = 8, Name = 16;

        /// <summary>The map's regions, as the .map has them: priority and the settings they turn on.</summary>
        private static readonly Dictionary<uint, (int Priority, int Flags)> Regions = new()
        {
            [1] = (1, 13), [4] = (2, 31), [5] = (3, 31), [8] = (4, 23), [9] = (5, 23), [10] = (6, 23), [11] = (7, 23), [12] = (8, 23), [6] = (9, 31),
            [7] = (10, 31), [3] = (11, 31), [14] = (12, 31),
        };

        /// <summary>Where each new or recut volume shows, and where the places beside it keep theirs.</summary>
        private static readonly (Vector3 At, bool Underground, int Flag, uint Before, uint After, string Why)[] Points =
        {
            (new Vector3(365f, 64.5f, 126f), true, Name, 0u, 3u, "upper lab room"),
            (new Vector3(360f, 34.8f, 45f), false, Sky, 0u, 3u, "lab entry chunnel"),
            (new Vector3(455f, 33.7f, 103f), true, Name, 0u, 14u, "lower lab room"),
            (new Vector3(455f, 33.7f, 103f), true, Music, 0u, 14u, "lower lab room music"),
            (new Vector3(-10f, 63.6f, -490f), true, Name, 0u, 8u, "tunnel by Mausoleum tee"),
            (new Vector3(15f, 64.1f, -470f), true, Sky, 0u, 8u, "crystal room on the Beholden path"),
            (new Vector3(10f, 70.9f, -420f), true, Name, 0u, 9u, "crystal tunnel north of split"),
            (new Vector3(30f, 87.7f, -360f), true, Name, 0u, 9u, "Penitent tunnel turn"),
            (new Vector3(-8f, 80.1f, -300f), true, Name, 0u, 9u, "tunnel before Shrine entrance"),
            (new Vector3(520f, 56.1f, -345f), true, Name, 0u, 11u, "Renewal tunnel"),
            (new Vector3(100f, 68.1f, -440f), true, Name, 0u, 0u, "first chamber stays default"),
            (new Vector3(180f, 71.4f, -496.4f), true, Name, 10u, 10u, "entry (Velon Hollow circle) unchanged"),
            (new Vector3(265.8f, 32.1f, 14.7f), false, Name, 0u, 4u, "Bane Outpost POI"),
            (new Vector3(200f, 42.1f, 60f), true, Sky, 0u, 4u, "outpost cavern west, underground poly"),
            (new Vector3(287f, 47.5f, -80f), true, Name, 0u, 4u, "outpost cavern south mouth"),
            (new Vector3(165.2f, 78.1f, -520.3f), true, Name, 10u, 10u, "AFS hospital keeps Velon Hollow"),
            (new Vector3(-115.2f, 60.8f, -488.8f), false, Name, 0u, 5u, "Mausoleum POI"),
            (new Vector3(-170f, 64.1f, -450f), false, Music, 0u, 5u, "Mausoleum west hall"),
            (new Vector3(-119.3f, 82.3f, -279.5f), false, Name, 0u, 6u, "Shrine POI"),
            (new Vector3(-180f, 85.2f, -280f), false, Name, 0u, 6u, "Shrine west hall"),
            (new Vector3(-35f, 78.4f, -280f), false, Name, 0u, 6u, "Shrine entrance"),
            (new Vector3(631.6f, 50.1f, -343.2f), false, Name, 0u, 7u, "Sanctuary POI"),
            (new Vector3(540f, 56.5f, -345f), true, Name, 0u, 11u, "Renewal tunnel just outside the Sanctuary"),
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
            Assert.AreEqual(VelonHollowRegionSweep.Volumes.Length + VelonHollowRegionSweep.Recuts.Length, once.Except(before).Count(), "the new rows and the recut ones");

            foreach (var sql in VelonHollowRegionSweep.Up())
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

            foreach (var sql in VelonHollowRegionSweep.Up())
                StringAssert.Contains(migrator.GenerateScript(Previous, Migration), sql);
            foreach (var sql in VelonHollowRegionSweep.Down())
                StringAssert.Contains(migrator.GenerateScript(Migration, Previous), sql);
        }

        /// <summary>The highest-priority region here that turns the setting on, 0 for none (the map's own).</summary>
        private static uint Shown(List<MapRegion> volumes, Vector3 at, int flag, bool underground) =>
            volumes.Where(v => v.Enabled && Regions.TryGetValue(v.RegionId, out var r) && (r.Flags & flag) != 0 && v.Contains(at, underground))
                .OrderByDescending(v => Regions[v.RegionId].Priority).Select(v => v.RegionId).FirstOrDefault();

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == VelonHollowRegionSweep.MapContextId)
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT map_context_id || ' ' || region_id || ' ' || shape || ' ' || printf('%.1f %.1f %.1f %.1f %.1f %.1f %.1f %.1f', pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y) || ' ' || underground || ' ' || enabled || ' ' || comment AS Value " +
                    "FROM map_region WHERE map_context_id = 2136")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
