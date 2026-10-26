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

    /// <summary>Avernus Outpost's entrance corridor (Avernus_entrance_region, AvernusEntranceRegion).</summary>
    [TestClass]
    [DoNotParallelize]
    public class AvernusEntranceRegionTests
    {
        private const string Migration = "20270616000000_Avernus_entrance_region";
        private const string Previous = "20270615000000_Chaukas_facility_regions";

        private const int Sky = 1, Env = 2;

        /// <summary>Avernus Outpost's regions, as the .map has them: priority and the settings they turn on.</summary>
        private static readonly Dictionary<uint, (int Priority, int Flags)> Avernus = new()
        {
            [1] = (1, 13), [6] = (2, 0), [9] = (3, 0), [7] = (4, 0), [8] = (5, 0), [2] = (6, 27), [10] = (7, 31), [11] = (8, 31), [12] = (9, 27),
            [13] = (10, 31), [5] = (11, 31), [3] = (12, 3), [4] = (13, 3),
        };

        [TestMethod]
        public void TheCorridorFromTheWayInIsUnderTheHumanInteriorSky()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();
            var wayIn = new Vector3(84f, 528.8f, 107f);

            migrator.Migrate(Previous);
            Assert.AreEqual(0u, Shown(Volumes(database), wayIn, Sky, underground: true), "the corridor had the map's desert sky");

            migrator.Migrate(Migration);
            var after = Volumes(database);
            Assert.AreEqual(3u, Shown(after, wayIn, Sky, underground: true), "at the way in");
            Assert.AreEqual(3u, Shown(after, wayIn, Env, underground: true), "with the human interior environment map");
            Assert.AreEqual(3u, Shown(after, new Vector3(100f, 528.8f, 85f), Sky, underground: true), "round its turns");
            Assert.AreEqual(4u, Shown(after, new Vector3(100f, 528.7f, 70f), Sky, underground: true), "its mouth on the fort is the fort's");
            Assert.AreEqual(4u, Shown(after, new Vector3(154f, 529.8f, -130f), Sky, underground: false), "and the fort as it was");
        }

        [TestMethod]
        public void TheUpRunsTwiceAndTheDownTakesItOut()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            var before = Snapshot(database);

            migrator.Migrate(Migration);
            var once = Snapshot(database);
            Assert.AreEqual(AvernusEntranceRegion.Volumes.Length, once.Except(before).Count(), "the new row");

            foreach (var sql in AvernusEntranceRegion.Up())
                Assert.AreEqual(0, database.Context.Database.ExecuteSqlRaw(sql.Replace("{", "{{").Replace("}", "}}")), sql);
            CollectionAssert.AreEqual(once, Snapshot(database), "twice");

            migrator.Migrate(Previous);
            CollectionAssert.AreEqual(before, Snapshot(database), "Down");
        }

        [TestMethod]
        public void OnMySqlTheRowGoesInAndComesOut()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();

            foreach (var sql in AvernusEntranceRegion.Up())
                StringAssert.Contains(migrator.GenerateScript(Previous, Migration), sql);
            foreach (var sql in AvernusEntranceRegion.Down())
                StringAssert.Contains(migrator.GenerateScript(Migration, Previous), sql);
        }

        /// <summary>The highest-priority region here that turns the setting on, 0 for none (the map's own).</summary>
        private static uint Shown(List<MapRegion> volumes, Vector3 at, int flag, bool underground) =>
            volumes.Where(v => v.Enabled && Avernus.TryGetValue(v.RegionId, out var r) && (r.Flags & flag) != 0 && v.Contains(at, underground))
                .OrderByDescending(v => Avernus[v.RegionId].Priority).Select(v => v.RegionId).FirstOrDefault();

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == AvernusEntranceRegion.MapContextId)
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT map_context_id || ' ' || region_id || ' ' || shape || ' ' || printf('%.1f %.1f %.1f %.1f %.1f %.1f %.1f %.1f', pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y) || ' ' || underground || ' ' || enabled || ' ' || comment AS Value " +
                    "FROM map_region")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
