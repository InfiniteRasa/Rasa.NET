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

    /// <summary>Timora Mines' Predator Bay (Predator_bay_region, PredatorBayRegion).</summary>
    [TestClass]
    [DoNotParallelize]
    public class PredatorBayRegionTests
    {
        private const string Migration = "20270629000000_Predator_bay_region";
        private const string Previous = "20270628000000_Purgas_station_regions";

        private const int Sky = 1, Env = 2, Name = 16;

        /// <summary>Timora Mines' regions, as the .map has them: priority and the settings they turn on.</summary>
        private static readonly Dictionary<uint, (int Priority, int Flags)> Mines = new()
        {
            [9] = (1, 31), [4] = (2, 31), [13] = (3, 31), [5] = (4, 31), [6] = (5, 31), [7] = (6, 31), [8] = (7, 19), [3] = (8, 23),
            [11] = (9, 23), [12] = (10, 15), [1] = (11, 13), [2] = (12, 15), [10] = (13, 15),
        };

        [TestMethod]
        public void TheLaunchPadsLobeIsPredatorBay()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();
            var pad = new Vector3(-343f, 216.1f, 279f);

            migrator.Migrate(Previous);
            Assert.AreEqual(0u, Shown(Volumes(database), pad, Name, underground: false), "the bay had only the map's name");

            migrator.Migrate(Migration);
            var after = Volumes(database);
            Assert.AreEqual(8u, Shown(after, pad, Name, underground: false), "Predator Bay on the launch pad");
            Assert.AreEqual(8u, Shown(after, pad, Sky, underground: false), "with its sky");
            Assert.AreEqual(8u, Shown(after, pad, Env, underground: false), "and environment map");
            Assert.AreEqual(8u, Shown(after, new Vector3(-380f, 224.5f, 300f), Name, underground: false), "across the lobe");
            Assert.AreEqual(8u, Shown(after, new Vector3(-260f, 219.2f, 290f), Name, underground: false), "to its east side");
            Assert.AreEqual(3u, Shown(after, new Vector3(-215f, 219.9f, 290f), Name, underground: false), "the neck by the pillbox stays the excavation's");
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
            Assert.AreEqual(PredatorBayRegion.Volumes.Length, once.Except(before).Count(), "the new row");

            foreach (var sql in PredatorBayRegion.Up())
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

            foreach (var sql in PredatorBayRegion.Up())
                StringAssert.Contains(migrator.GenerateScript(Previous, Migration), sql);
            foreach (var sql in PredatorBayRegion.Down())
                StringAssert.Contains(migrator.GenerateScript(Migration, Previous), sql);
        }

        /// <summary>The highest-priority region here that turns the setting on, 0 for none (the map's own).</summary>
        private static uint Shown(List<MapRegion> volumes, Vector3 at, int flag, bool underground) =>
            volumes.Where(v => v.Enabled && Mines.TryGetValue(v.RegionId, out var r) && (r.Flags & flag) != 0 && v.Contains(at, underground))
                .OrderByDescending(v => Mines[v.RegionId].Priority).Select(v => v.RegionId).FirstOrDefault();

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == PredatorBayRegion.MapContextId)
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT map_context_id || ' ' || region_id || ' ' || shape || ' ' || printf('%.1f %.1f %.1f %.1f %.1f %.1f %.1f %.1f', pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y) || ' ' || underground || ' ' || enabled || ' ' || comment AS Value " +
                    "FROM map_region")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
