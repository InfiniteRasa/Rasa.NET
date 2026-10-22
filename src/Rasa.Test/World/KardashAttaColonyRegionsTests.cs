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

    /// <summary>The Kardash Atta Colony's Queen's Chamber, Outer Junction and Eloh Cache (Kardash_atta_colony_regions, KardashAttaColonyRegions).</summary>
    [TestClass]
    [DoNotParallelize]
    public class KardashAttaColonyRegionsTests
    {
        private const string Migration = "20270608000000_Kardash_atta_colony_regions";
        private const string Previous = "20270607000000_Treeback_camp_regions";

        private const int Sky = 1, Env = 2, Ambient = 4, Music = 8, Name = 16;

        /// <summary>The colony's regions, as the .map has them: priority and the settings they turn on.</summary>
        private static readonly Dictionary<uint, (int Priority, int Flags)> Colony = new()
        {
            [23] = (1, 0), [22] = (2, 23), [17] = (3, 23), [18] = (4, 23), [19] = (5, 23), [21] = (6, 23), [12] = (7, 0), [14] = (8, 0), [13] = (9, 0),
            [2] = (10, 23), [3] = (11, 23), [4] = (12, 23), [5] = (13, 23), [6] = (14, 23), [20] = (15, 23), [7] = (16, 31), [8] = (17, 23),
            [24] = (18, 3), [25] = (19, 3), [9] = (20, 23), [10] = (21, 19), [11] = (22, 23), [15] = (23, 19), [16] = (24, 0), [1] = (25, 13),
        };

        private static readonly Vector3 Throne = new(-349f, 42.4f, 222.5f), Junction = new(-352f, 42.1f, -24f), ElohRoom = new(-110f, 192f, 324f);

        [TestMethod]
        public void TheThreePlacesHadNothingAndHaveTheirRegions()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            var before = Volumes(database);
            Assert.AreEqual(0u, Shown(before, Throne, Name, underground: false), "nothing named the throne cavern");
            Assert.AreEqual(0u, Shown(before, Junction, Name, underground: false), "or the west tunnel");
            Assert.AreEqual(0u, Shown(before, ElohRoom, Name, underground: true), "or the Eloh room");

            migrator.Migrate(Migration);
            var after = Volumes(database);
            uint At(float x, float y, float z, bool underground) => Shown(after, new Vector3(x, y, z), Name, underground);

            foreach (var flag in new[] { Sky, Env, Ambient, Music, Name })
                Assert.AreEqual(7u, Shown(after, Throne, flag, underground: false), $"the Queen's Chamber at the throne ({flag})");
            Assert.AreEqual(7u, At(-380f, 41.7f, 150f, false), "its south");
            Assert.AreEqual(7u, At(-300f, 39.2f, 280f, true), "its east, under the rock");
            Assert.AreEqual(8u, At(-352f, 42.1f, -24f, false), "Outer Junction, its chamber");
            Assert.AreEqual(8u, At(-352f, 42.5f, -60f, false));
            Assert.AreEqual(8u, At(-380f, 41.8f, -100f, true), "its south end towards the Royal Nest");
            Assert.AreEqual(8u, At(-340f, 47.2f, 45f, true), "its north end towards the Queen's Chamber");
            Assert.AreEqual(8u, Shown(after, Junction, Sky, underground: false), "under the corridor sky");
            Assert.AreEqual(15u, At(-110f, 192f, 324f, true), "the Eloh Cache");
            Assert.AreEqual(15u, At(-40f, 192.1f, 330f, true), "and its corridor to the door");
            Assert.AreEqual(15u, Shown(after, ElohRoom, Sky, underground: true), "under the Eloh sky");
            Assert.AreEqual(4u, At(-140.8f, 201.7f, 80f, false), "the Shining Cavern is as it was");
            Assert.AreEqual(10u, At(-297.8f, 44.6f, -314.1f, false), "and the Royal Nest");
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
            Assert.AreEqual(KardashAttaColonyRegions.Volumes.Length, once.Except(before).Count(), "the new rows");

            foreach (var sql in KardashAttaColonyRegions.Up())
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

            foreach (var sql in KardashAttaColonyRegions.Up())
                StringAssert.Contains(migrator.GenerateScript(Previous, Migration), sql);
            foreach (var sql in KardashAttaColonyRegions.Down())
                StringAssert.Contains(migrator.GenerateScript(Migration, Previous), sql);
        }

        /// <summary>The highest-priority region here that turns the setting on, 0 for none (the map's own).</summary>
        private static uint Shown(List<MapRegion> volumes, Vector3 at, int flag, bool underground) =>
            volumes.Where(v => v.Enabled && Colony.TryGetValue(v.RegionId, out var r) && (r.Flags & flag) != 0 && v.Contains(at, underground))
                .OrderByDescending(v => Colony[v.RegionId].Priority).Select(v => v.RegionId).FirstOrDefault();

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == KardashAttaColonyRegions.MapContextId)
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT map_context_id || ' ' || region_id || ' ' || shape || ' ' || printf('%.1f %.1f %.1f %.1f %.1f %.1f %.1f %.1f', pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y) || ' ' || underground || ' ' || enabled || ' ' || comment AS Value " +
                    "FROM map_region")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
