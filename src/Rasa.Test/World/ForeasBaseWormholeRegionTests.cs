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

    /// <summary>The Foreas Base's wormhole clearing (Foreas_base_wormhole_region, ForeasBaseWormholeRegion).</summary>
    [TestClass]
    [DoNotParallelize]
    public class ForeasBaseWormholeRegionTests
    {
        private const string Migration = "20270223000000_Foreas_base_wormhole_region";
        private const string Previous = "20270222000000_Wardenbot_factory_levels";

        private const int Sky = 1, Env = 2, Ambient = 4, Music = 8, Name = 16;

        /// <summary>Concordia Divide's regions, as the .map has them: priority and the settings they turn on.</summary>
        private static readonly Dictionary<uint, (int Priority, int Flags)> Divide = new()
        {
            [22] = (1, 13), [40] = (2, 1), [23] = (3, 28), [10] = (4, 28), [20] = (5, 28), [21] = (6, 28), [30] = (7, 28), [15] = (8, 4), [16] = (9, 7),
            [17] = (10, 4), [18] = (11, 4), [19] = (12, 4), [14] = (13, 3), [13] = (14, 15), [11] = (15, 15), [32] = (16, 7), [12] = (17, 31), [33] = (18, 7),
            [5] = (19, 12), [6] = (20, 12), [4] = (21, 12), [1] = (22, 28), [2] = (23, 4), [27] = (24, 28), [7] = (25, 7), [3] = (26, 15), [25] = (27, 28),
            [28] = (28, 12), [29] = (29, 15), [8] = (30, 15), [9] = (31, 15), [36] = (32, 11), [31] = (33, 7), [34] = (34, 15), [37] = (35, 4), [35] = (36, 3),
            [24] = (37, 28), [38] = (38, 0),
        };

        [TestMethod]
        public void TheClearingRoundTheWormholeIsForeasBaseWormhole()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();
            var wormhole = new Vector3(-35.1f, 115.7f, 765.4f);

            migrator.Migrate(Previous);
            Assert.AreEqual(0u, Shown(Volumes(database), wormhole, Name, underground: false), "the wormhole was in no region");

            migrator.Migrate(Migration);
            var after = Volumes(database);
            uint At(float x, float y, float z, int flag = Name, bool underground = false) => Shown(after, new Vector3(x, y, z), flag, underground);

            Assert.AreEqual(23u, At(-35.1f, 115.7f, 765.4f), "at the wormhole");
            Assert.AreEqual(23u, At(-35.1f, 115.7f, 765.4f, Music), "under the AFS base's music");
            Assert.AreEqual(23u, At(-70f, 115.6f, 750f), "at the holo fence and crates");
            Assert.AreEqual(23u, At(20f, 112.6f, 770f, Name, underground: true), "at the pond by the Forean shrine");
            Assert.AreEqual(23u, At(-80f, 115.4f, 705f), "at the top of the track from the base");
            Assert.AreEqual(10u, At(-42f, 116.2f, 600f), "Foreas Base is as it was");
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
            Assert.AreEqual(ForeasBaseWormholeRegion.Volumes.Length, once.Except(before).Count());

            foreach (var sql in ForeasBaseWormholeRegion.Up())
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

            foreach (var sql in ForeasBaseWormholeRegion.Up())
                StringAssert.Contains(migrator.GenerateScript(Previous, Migration), sql);
            foreach (var sql in ForeasBaseWormholeRegion.Down())
                StringAssert.Contains(migrator.GenerateScript(Migration, Previous), sql);
        }

        /// <summary>The highest-priority region here that turns the setting on, 0 for none (the map's own).</summary>
        private static uint Shown(List<MapRegion> volumes, Vector3 at, int flag, bool underground) =>
            volumes.Where(v => v.Enabled && Divide.TryGetValue(v.RegionId, out var r) && (r.Flags & flag) != 0 && v.Contains(at, underground))
                .OrderByDescending(v => Divide[v.RegionId].Priority).Select(v => v.RegionId).FirstOrDefault();

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == ForeasBaseWormholeRegion.MapContextId)
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT map_context_id || ' ' || region_id || ' ' || shape || ' ' || printf('%.1f %.1f %.1f %.1f %.1f', pos_x, pos_z, radius, half_x, half_z) || ' ' || enabled AS Value " +
                    "FROM map_region")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
