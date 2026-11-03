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

    /// <summary>The region sweep on the Live Target Pens (Live_target_pens_region_sweep, LiveTargetPensRegionSweep).</summary>
    [TestClass]
    [DoNotParallelize]
    public class LiveTargetPensRegionSweepTests
    {
        private const string Migration = "20270723010000_Live_target_pens_region_sweep";
        private const string Previous = "20270718000000_Purgas_station_gap_regions";

        private const int Sky = 1, Env = 2, Ambient = 4, Music = 8, Name = 16;

        /// <summary>The map's regions, as the .map has them: priority and the settings they turn on.</summary>
        private static readonly Dictionary<uint, (int Priority, int Flags)> Regions = new()
        {
            [2] = (1, 15), [11] = (2, 20), [12] = (3, 20), [13] = (4, 20), [14] = (5, 21), [15] = (6, 20), [16] = (7, 20), [17] = (8, 20),
            [18] = (9, 20), [19] = (10, 20), [20] = (11, 29), [21] = (12, 20), [22] = (13, 20), [23] = (14, 20),
        };

        /// <summary>Where each new or recut volume shows, and where the places beside it keep theirs.</summary>
        private static readonly (Vector3 At, bool Underground, int Flag, uint Before, uint After, string Why)[] Points =
        {
            (new Vector3(-100f, 152f, -56f), false, Name, 20u, 20u, "arena floor keeps Main Test Pit"),
            (new Vector3(-100f, 152f, -56f), false, Sky, 20u, 20u, "arena keeps the arena sky"),
            (new Vector3(-63f, 51f, -85f), false, Name, 20u, 20u, "arena pit bottom keeps Main Test Pit"),
            (new Vector3(-165f, 152.5f, -168f), false, Name, 20u, 19u, "SW creature pit floor"),
            (new Vector3(-165f, 152.5f, -168f), false, Ambient, 20u, 19u, "SW creature pit ambient"),
            (new Vector3(-165f, 152.5f, -168f), false, Sky, 20u, 0u, "SW pit: no arena sky, map's interior sky"),
            (new Vector3(-120f, 161.8f, -168f), false, Name, 20u, 19u, "SW pit overlook"),
            (new Vector3(-165f, 152.5f, 56f), false, Name, 20u, 19u, "NW creature pit floor"),
            (new Vector3(-120f, 161.8f, 56f), false, Name, 20u, 19u, "NW pit overlook"),
            (new Vector3(40f, 152.5f, -168f), false, Name, 20u, 19u, "SE creature pit floor"),
            (new Vector3(-8f, 161.8f, -168f), false, Name, 20u, 19u, "SE pit overlook"),
            (new Vector3(100f, 152.9f, 55f), false, Name, 20u, 14u, "hangar floor"),
            (new Vector3(100f, 152.9f, 55f), false, Sky, 20u, 14u, "hangar arena-style sky"),
            (new Vector3(30f, 161.8f, 60f), false, Name, 20u, 14u, "hangar west gallery"),
            (new Vector3(56f, 154.1f, -104f), false, Name, 20u, 15u, "east prison chunnel"),
            (new Vector3(56f, 154.1f, -104f), false, Ambient, 20u, 15u, "east prison ambient"),
            (new Vector3(14f, 152.2f, -318f), false, Name, 0u, 11u, "Guardstation hospital in the guard room"),
            (new Vector3(-8f, 153f, -304f), false, Ambient, 0u, 11u, "guard room ambient"),
            (new Vector3(-64f, 152f, -168f), false, Name, 20u, 0u, "south control room no longer called Main Test Pit"),
            (new Vector3(-64f, 152f, 56f), false, Name, 20u, 0u, "north control room no longer called Main Test Pit"),
            (new Vector3(-184f, 154f, -104f), false, Name, 20u, 0u, "SW prison (unresolved West/Central) falls back to map name"),
            (new Vector3(-150f, 154f, -56f), false, Name, 20u, 0u, "west tee outside the arena door"),
            (new Vector3(-143f, 186f, -314f), false, Name, 0u, 0u, "entrance tunnel unchanged"),
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
            Assert.AreEqual(LiveTargetPensRegionSweep.Volumes.Length + LiveTargetPensRegionSweep.Recuts.Length, once.Except(before).Count(), "the new rows and the recut ones");

            foreach (var sql in LiveTargetPensRegionSweep.Up())
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

            foreach (var sql in LiveTargetPensRegionSweep.Up())
                StringAssert.Contains(migrator.GenerateScript(Previous, Migration), sql);
            foreach (var sql in LiveTargetPensRegionSweep.Down())
                StringAssert.Contains(migrator.GenerateScript(Migration, Previous), sql);
        }

        /// <summary>The highest-priority region here that turns the setting on, 0 for none (the map's own).</summary>
        private static uint Shown(List<MapRegion> volumes, Vector3 at, int flag, bool underground) =>
            volumes.Where(v => v.Enabled && Regions.TryGetValue(v.RegionId, out var r) && (r.Flags & flag) != 0 && v.Contains(at, underground))
                .OrderByDescending(v => Regions[v.RegionId].Priority).Select(v => v.RegionId).FirstOrDefault();

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == LiveTargetPensRegionSweep.MapContextId)
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT map_context_id || ' ' || region_id || ' ' || shape || ' ' || printf('%.1f %.1f %.1f %.1f %.1f %.1f %.1f %.1f', pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y) || ' ' || underground || ' ' || enabled || ' ' || comment AS Value " +
                    "FROM map_region WHERE map_context_id = 1763")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
