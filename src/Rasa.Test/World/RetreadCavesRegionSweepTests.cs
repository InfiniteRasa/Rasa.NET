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

    /// <summary>The region sweep on the Retread Caves (Retread_caves_region_sweep, RetreadCavesRegionSweep).</summary>
    [TestClass]
    [DoNotParallelize]
    public class RetreadCavesRegionSweepTests
    {
        private const string Migration = "20270723020000_Retread_caves_region_sweep";
        private const string Previous = "20270718000000_Purgas_station_gap_regions";

        private const int Sky = 1, Env = 2, Ambient = 4, Music = 8, Name = 16;

        /// <summary>The map's regions, as the .map has them: priority and the settings they turn on.</summary>
        private static readonly Dictionary<uint, (int Priority, int Flags)> Regions = new()
        {
            [2] = (1, 13), [8] = (2, 16), [9] = (3, 24), [5] = (4, 16), [6] = (5, 20), [3] = (6, 28), [4] = (7, 28), [7] = (8, 24),
        };

        /// <summary>Where each new or recut volume shows, and where the places beside it keep theirs.</summary>
        private static readonly (Vector3 At, bool Underground, int Flag, uint Before, uint After, string Why)[] Points =
        {
            (new Vector3(19.3f, 82.9f, -4.4f), false, Name, 8u, 5u, "zone-in point of the link from Pools"),
            (new Vector3(0f, 80.8f, -5f), false, Name, 8u, 5u, "arrival chamber floor"),
            (new Vector3(-20f, 76.2f, -5f), true, Name, 0u, 5u, "arrival chamber underground middle"),
            (new Vector3(10.1f, 35.6f, -66.5f), true, Name, 0u, 8u, "Drop Off floor hole"),
            (new Vector3(32.7f, 10.6f, -66.9f), false, Name, 8u, 8u, "Drop Off surface floor unchanged"),
            (new Vector3(-146.7f, 58.3f, -33.7f), true, Name, 0u, 7u, "Western Grotto floor hole"),
            (new Vector3(-146.7f, 58.3f, -33.7f), true, Music, 0u, 7u, "Western Grotto music in the hole"),
            (new Vector3(-124.2f, 58.3f, -35.6f), false, Name, 7u, 7u, "grotto label point unchanged"),
            (new Vector3(-51.9f, 46.9f, -278.5f), true, Ambient, 0u, 3u, "Retread Camp floor hole ambient"),
            (new Vector3(-37.6f, 74.9f, -143.7f), true, Name, 0u, 3u, "camp north exit hole"),
            (new Vector3(-37.3f, 48.5f, -214.2f), false, Name, 3u, 3u, "camp label point unchanged"),
            (new Vector3(148.1f, 47.9f, -290.1f), true, Name, 0u, 4u, "Miasma Lair floor hole"),
            (new Vector3(121.1f, 61f, -292f), false, Name, 4u, 4u, "lair label point unchanged"),
            (new Vector3(-105.3f, 66.2f, -116.5f), true, Name, 0u, 0u, "tunnel between grotto and camp untouched"),
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
            Assert.AreEqual(RetreadCavesRegionSweep.Volumes.Length + RetreadCavesRegionSweep.Recuts.Length, once.Except(before).Count(), "the new rows and the recut ones");

            foreach (var sql in RetreadCavesRegionSweep.Up())
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

            foreach (var sql in RetreadCavesRegionSweep.Up())
                StringAssert.Contains(migrator.GenerateScript(Previous, Migration), sql);
            foreach (var sql in RetreadCavesRegionSweep.Down())
                StringAssert.Contains(migrator.GenerateScript(Migration, Previous), sql);
        }

        /// <summary>The highest-priority region here that turns the setting on, 0 for none (the map's own).</summary>
        private static uint Shown(List<MapRegion> volumes, Vector3 at, int flag, bool underground) =>
            volumes.Where(v => v.Enabled && Regions.TryGetValue(v.RegionId, out var r) && (r.Flags & flag) != 0 && v.Contains(at, underground))
                .OrderByDescending(v => Regions[v.RegionId].Priority).Select(v => v.RegionId).FirstOrDefault();

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == RetreadCavesRegionSweep.MapContextId)
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT map_context_id || ' ' || region_id || ' ' || shape || ' ' || printf('%.1f %.1f %.1f %.1f %.1f %.1f %.1f %.1f', pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y) || ' ' || underground || ' ' || enabled || ' ' || comment AS Value " +
                    "FROM map_region WHERE map_context_id = 1694")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
