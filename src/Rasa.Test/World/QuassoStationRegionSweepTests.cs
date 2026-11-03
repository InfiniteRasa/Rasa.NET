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

    /// <summary>The region sweep on Quasso Station (Quasso_station_region_sweep, QuassoStationRegionSweep).</summary>
    [TestClass]
    [DoNotParallelize]
    public class QuassoStationRegionSweepTests
    {
        private const string Migration = "20270724080000_Quasso_station_region_sweep";
        private const string Previous = "20270718000000_Purgas_station_gap_regions";

        private const int Sky = 1, Env = 2, Ambient = 4, Music = 8, Name = 16;

        /// <summary>The map's regions, as the .map has them: priority and the settings they turn on.</summary>
        private static readonly Dictionary<uint, (int Priority, int Flags)> Regions = new()
        {
            [1] = (1, 13), [13] = (2, 16), [15] = (3, 17), [14] = (4, 24), [3] = (5, 31), [8] = (6, 24), [9] = (7, 24), [11] = (8, 24), [12] = (9, 24),
            [10] = (10, 28),
        };

        /// <summary>Where each new or recut volume shows, and where the places beside it keep theirs.</summary>
        private static readonly (Vector3 At, bool Underground, int Flag, uint Before, uint After, string Why)[] Points =
        {
            (new Vector3(11f, 44.5f, -90f), true, Sky, 0u, 15u, "window room: outdoor sky"),
            (new Vector3(11f, 44.5f, -90f), true, Name, 14u, 15u, "window room no longer named Access Floor"),
            (new Vector3(11f, 45.7f, -120f), true, Name, 13u, 15u, "back of the window room"),
            (new Vector3(-38f, 13.8f, -77f), true, Name, 14u, 14u, "west access-floor room keeps its name"),
            (new Vector3(-38f, 13.8f, -77f), true, Music, 14u, 14u, "access floor music kept"),
            (new Vector3(-38f, 13.8f, -77f), true, Sky, 0u, 0u, "access floor stays interior sky"),
            (new Vector3(10f, 46.5f, -157f), true, Name, 13u, 13u, "upper tunnel cross keeps level name"),
            (new Vector3(10f, 46.5f, -157f), true, Sky, 0u, 0u, "upper tunnels stay interior"),
            (new Vector3(30f, 12.6f, 29f), false, Name, 3u, 3u, "dropship yard unchanged"),
            (new Vector3(-371.6f, -15.8f, 218.8f), false, Name, 11u, 9u, "Bane Stasis Room 01 is barracks"),
            (new Vector3(-371.6f, -15.8f, 218.8f), false, Music, 11u, 9u, "barracks music in stasis room"),
            (new Vector3(-523.6f, -15.8f, 217.8f), false, Name, 11u, 9u, "Bane Stasis Room 02 is barracks"),
            (new Vector3(-102f, 15.9f, 355.4f), true, Name, 11u, 8u, "Juggernaut Warehouse 02 is the warehouse"),
            (new Vector3(-300f, -48.6f, 300f), false, Name, 11u, 11u, "lower level keeps Subterranean Command"),
            (new Vector3(-421.2f, -49f, 184.8f), false, Name, 10u, 10u, "Conscription Chamber keeps Prison"),
            (new Vector3(-215f, -57f, 531f), false, Name, 12u, 12u, "Central Command unchanged"),
            (new Vector3(-622.6f, -13f, 236.6f), false, Name, 9u, 9u, "Barracks Storeroom unchanged"),
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
            Assert.AreEqual(QuassoStationRegionSweep.Volumes.Length + QuassoStationRegionSweep.Recuts.Length, once.Except(before).Count(), "the new rows and the recut ones");

            foreach (var sql in QuassoStationRegionSweep.Up())
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

            foreach (var sql in QuassoStationRegionSweep.Up())
                StringAssert.Contains(migrator.GenerateScript(Previous, Migration), sql);
            foreach (var sql in QuassoStationRegionSweep.Down())
                StringAssert.Contains(migrator.GenerateScript(Migration, Previous), sql);
        }

        /// <summary>The highest-priority region here that turns the setting on, 0 for none (the map's own).</summary>
        private static uint Shown(List<MapRegion> volumes, Vector3 at, int flag, bool underground) =>
            volumes.Where(v => v.Enabled && Regions.TryGetValue(v.RegionId, out var r) && (r.Flags & flag) != 0 && v.Contains(at, underground))
                .OrderByDescending(v => Regions[v.RegionId].Priority).Select(v => v.RegionId).FirstOrDefault();

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == QuassoStationRegionSweep.MapContextId)
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT map_context_id || ' ' || region_id || ' ' || shape || ' ' || printf('%.1f %.1f %.1f %.1f %.1f %.1f %.1f %.1f', pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y) || ' ' || underground || ' ' || enabled || ' ' || comment AS Value " +
                    "FROM map_region WHERE map_context_id = 2105")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
