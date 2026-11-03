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

    /// <summary>The Bane shard's fog down the Fithik Trench (Palisades_fithik_trench_region, PalisadesFithikTrenchRegion).</summary>
    [TestClass]
    [DoNotParallelize]
    public class PalisadesFithikTrenchRegionTests
    {
        private const string Migration = "20270714000000_Palisades_fithik_trench_region";
        private const string Previous = "20270704000000_Guardian_prominence_regions";

        private const int Ambient = 4, Music = 8, Name = 16;

        /// <summary>Palisades' regions, as the .map has them: priority and the settings they turn on.</summary>
        private static readonly Dictionary<uint, (int Priority, int Flags)> Palisades = new()
        {
            [1] = (1, 13), [31] = (2, 31), [30] = (3, 31), [29] = (4, 16), [28] = (5, 15), [26] = (6, 23), [21] = (7, 30), [22] = (8, 30),
            [23] = (9, 30), [18] = (10, 22), [25] = (11, 30), [19] = (12, 31), [20] = (13, 18), [17] = (14, 30), [16] = (15, 30), [2] = (16, 30),
            [3] = (17, 30), [4] = (18, 22), [5] = (19, 22), [6] = (20, 22), [7] = (21, 22), [8] = (22, 22), [9] = (23, 18), [27] = (24, 18),
            [14] = (25, 14), [10] = (26, 30), [11] = (27, 30), [35] = (28, 16), [12] = (29, 15), [13] = (30, 15), [34] = (31, 15), [32] = (32, 16),
            [33] = (33, 16),
        };

        [TestMethod]
        public void TheTrenchFloorHasTheShardsSound()
        {
            var (before, after) = Migrated();
            var north = new Vector3(240.7f, 82.6f, -293.2f);
            var middle = new Vector3(199.2f, 78.8f, -448f);
            var den = new Vector3(161.9f, 77.6f, -574.4f);

            Assert.AreEqual(4u, Shown(before, north, Ambient, underground: false), "the trench had the wind");
            Assert.AreEqual(14u, Shown(after, north, Ambient, underground: false), "it has the Bane shard's territory");
            Assert.AreEqual(14u, Shown(after, north, Music, underground: false), "and its music");
            Assert.AreEqual(4u, Shown(after, north, Name, underground: false), "still the Fithik Trench");
            Assert.AreEqual(0u, Shown(before, middle, Ambient, underground: false), "the middle of the trench had nothing");
            Assert.AreEqual(14u, Shown(after, middle, Ambient, underground: false), "and has the shard's sound");
            Assert.AreEqual(14u, Shown(after, den, Ambient, underground: false), "down to the tunnel into the Devil's Den");
            Assert.AreEqual(16u, Shown(after, den, Name, underground: false), "under the Bane Shard's name");
            Assert.AreEqual(14u, Shown(after, new Vector3(167.9f, 66.5f, -529.6f), Ambient, underground: true), "under the shard too");
            Assert.AreEqual(4u, Shown(after, new Vector3(297.9f, 134.8f, -348.9f), Ambient, underground: false), "not the ground above the trench");
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
            Assert.AreEqual(PalisadesFithikTrenchRegion.Volumes.Length, once.Except(before).Count(), "the new rows");

            foreach (var sql in PalisadesFithikTrenchRegion.Up())
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

            foreach (var sql in PalisadesFithikTrenchRegion.Up())
                StringAssert.Contains(migrator.GenerateScript(Previous, Migration), sql);
            foreach (var sql in PalisadesFithikTrenchRegion.Down())
                StringAssert.Contains(migrator.GenerateScript(Migration, Previous), sql);
        }

        /// <summary>The highest-priority region here that turns the setting on, 0 for none (the map's own).</summary>
        private static uint Shown(List<MapRegion> volumes, Vector3 at, int flag, bool underground) =>
            volumes.Where(v => v.Enabled && Palisades.TryGetValue(v.RegionId, out var r) && (r.Flags & flag) != 0 && v.Contains(at, underground))
                .OrderByDescending(v => Palisades[v.RegionId].Priority).Select(v => v.RegionId).FirstOrDefault();

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == PalisadesFithikTrenchRegion.MapContextId)
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT map_context_id || ' ' || region_id || ' ' || shape || ' ' || printf('%.1f %.1f %.1f %.1f %.1f %.1f %.1f %.1f', pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y) || ' ' || underground || ' ' || enabled || ' ' || comment AS Value " +
                    "FROM map_region WHERE map_context_id = 1244")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
