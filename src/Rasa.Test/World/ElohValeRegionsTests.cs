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

    /// <summary>Eloh Vale's caves, gate, pyramid and outflow (Eloh_vale_regions, ElohValeRegions).</summary>
    [TestClass]
    [DoNotParallelize]
    public class ElohValeRegionsTests
    {
        private const string Migration = "20270626000000_Eloh_vale_regions";
        private const string Previous = "20270625000000_Palisades_regions";

        private const int Sky = 1, Ambient = 4, Name = 16;

        /// <summary>Eloh Vale's regions, as the .map has them: priority and the settings they turn on.</summary>
        private static readonly Dictionary<uint, (int Priority, int Flags)> Vale = new()
        {
            [1] = (1, 1), [7] = (2, 23), [22] = (3, 31), [21] = (4, 31), [8] = (5, 28), [18] = (6, 20), [14] = (7, 20), [19] = (8, 1),
            [10] = (9, 20), [11] = (10, 20), [13] = (11, 23), [15] = (12, 20), [16] = (13, 23), [20] = (14, 4),
        };

        [TestMethod]
        public void EachPlaceHasItsName()
        {
            var (before, after) = Migrated();
            var entryCave = new Vector3(-40f, 177.9f, -330f);

            Assert.AreNotEqual(16u, Shown(before, entryCave, Name, underground: true), "the caves had no volume");
            Assert.AreEqual(16u, Shown(after, entryCave, Name, underground: true), "Entry Cave");
            Assert.AreEqual(16u, Shown(after, entryCave, Sky, underground: true), "with its cave sky");
            Assert.AreEqual(16u, Shown(after, new Vector3(-10f, 185.3f, -355f), Name, underground: true), "deep in it");
            Assert.AreEqual(21u, Shown(after, new Vector3(-62f, 165.9f, 100f), Sky, underground: true), "Eloh Shield Gate, under its roof");
            Assert.AreNotEqual(21u, Shown(after, new Vector3(-62f, 165.9f, 140f), Sky, underground: false), "but not the open causeway past it");
            Assert.AreEqual(10u, Shown(after, new Vector3(66f, 185.7f, -233f), Name, underground: false), "Pyramid Temple at the AFS hospital");
            Assert.AreEqual(13u, Shown(after, new Vector3(307f, 131.1f, -217f), Name, underground: true), "Cave of Ancients");
            Assert.AreEqual(13u, Shown(after, new Vector3(307f, 131.1f, -217f), Sky, underground: true), "with its cave sky");
            Assert.AreEqual(15u, Shown(after, new Vector3(-40f, 150.5f, 290f), Name, underground: false), "Waterfall Outflow");
            Assert.AreEqual(19u, Shown(after, new Vector3(-60f, 100f, 400f), Sky, underground: false), "the falling sky in the canyon under the path");
            Assert.AreEqual(19u, Shown(after, new Vector3(45f, 50f, 450f), Sky, underground: false), "down to its floor");
            Assert.AreNotEqual(19u, Shown(after, new Vector3(-60f, 144.9f, 400f), Sky, underground: false), "but not on the path itself");
            Assert.AreEqual(20u, Shown(after, new Vector3(-68f, 157.3f, -430f), Ambient, underground: true), "the waterfall by the way in");
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
        public void TheUpRunsTwiceAndTheDownTakesThemOut()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            var before = Snapshot(database);

            migrator.Migrate(Migration);
            var once = Snapshot(database);
            Assert.AreEqual(ElohValeRegions.Volumes.Length, once.Except(before).Count(), "the new rows");

            foreach (var sql in ElohValeRegions.Up())
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

            foreach (var sql in ElohValeRegions.Up())
                StringAssert.Contains(migrator.GenerateScript(Previous, Migration), sql);
            foreach (var sql in ElohValeRegions.Down())
                StringAssert.Contains(migrator.GenerateScript(Migration, Previous), sql);
        }

        /// <summary>The highest-priority region here that turns the setting on, 0 for none (the map's own).</summary>
        private static uint Shown(List<MapRegion> volumes, Vector3 at, int flag, bool underground) =>
            volumes.Where(v => v.Enabled && Vale.TryGetValue(v.RegionId, out var r) && (r.Flags & flag) != 0 && v.Contains(at, underground))
                .OrderByDescending(v => Vale[v.RegionId].Priority).Select(v => v.RegionId).FirstOrDefault();

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == ElohValeRegions.MapContextId)
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT map_context_id || ' ' || region_id || ' ' || shape || ' ' || printf('%.1f %.1f %.1f %.1f %.1f %.1f %.1f %.1f', pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y) || ' ' || underground || ' ' || enabled || ' ' || comment AS Value " +
                    "FROM map_region")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
