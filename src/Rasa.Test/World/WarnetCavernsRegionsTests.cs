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

    /// <summary>Warnet Caverns' places (Warnet_caverns_regions, WarnetCavernsRegions).</summary>
    [TestClass]
    [DoNotParallelize]
    public class WarnetCavernsRegionsTests
    {
        private const string Migration = "20270609000000_Warnet_caverns_regions";
        private const string Previous = "20270608000000_Kardash_atta_colony_regions";

        private const int Sky = 1, Env = 2, Ambient = 4, Music = 8, Name = 16;

        /// <summary>Warnet Caverns' regions, as the .map has them: priority and the settings they turn on.</summary>
        private static readonly Dictionary<uint, (int Priority, int Flags)> Warnet = new()
        {
            [1] = (1, 13), [7] = (2, 16), [9] = (3, 16), [6] = (4, 16), [3] = (5, 29), [5] = (6, 16), [4] = (7, 16), [2] = (8, 25), [10] = (9, 0),
        };

        [TestMethod]
        public void FromTheWayInToTheNest()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            Assert.AreEqual(0u, Shown(Volumes(database), new Vector3(30f, 98.7f, -83f), Name, underground: false), "nothing named the caverns");

            migrator.Migrate(Migration);
            var after = Volumes(database);
            uint At(float x, float y, float z, bool underground = false) => Shown(after, new Vector3(x, y, z), Name, underground);

            Assert.AreEqual(9u, At(-120f, 97.4f, 45f), "Upper Passages, at the first aid station by the way in");
            Assert.AreEqual(9u, At(-80f, 92.5f, 46f), "down the tunnel");
            Assert.AreEqual(6u, At(20f, 71.4f, 75f), "Upper Chambers, the first");
            Assert.AreEqual(6u, At(60f, 72.5f, 85f, underground: true));
            Assert.AreEqual(6u, At(110f, 63.4f, 120f), "the second");
            Assert.AreEqual(6u, At(150f, 69.4f, 130f));
            Assert.AreEqual(9u, At(190f, 60.5f, 155f), "Upper Passages, over to the tee");
            Assert.AreEqual(9u, At(214f, 59.4f, 125f));
            Assert.AreEqual(7u, At(214f, 41.1f, 85f), "The Lower Passage, down from the tee");
            Assert.AreEqual(7u, At(240f, 49f, 78f), "towards Bug City");
            Assert.AreEqual(7u, At(262f, 36f, 185f), "and north to the Nest");
            Assert.AreEqual(4u, At(175f, 33.2f, 40f), "The Dig Site");
            Assert.AreEqual(4u, At(200f, 34.7f, 30f));
            Assert.AreEqual(5u, At(300f, 47.3f, 80f), "Bug City");
            Assert.AreEqual(5u, At(270f, 32.7f, 120f));
            Assert.AreEqual(5u, At(285f, 51f, 30f), "its south rooms");
            Assert.AreEqual(2u, At(300f, 1.5f, 330f), "The Nest");
            Assert.AreEqual(2u, At(250f, 5.3f, 280f));
            Assert.AreEqual(2u, Shown(after, new Vector3(300f, 1.5f, 330f), Sky, underground: false), "under the giant-room sky");
            Assert.AreEqual(2u, Shown(after, new Vector3(300f, 1.5f, 330f), Music, underground: false));
        }

        [TestMethod]
        public void TheResearchBaseCampIsTheBigCavern()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Migration);
            var after = Volumes(database);
            var camp = new Vector3(30f, 98.7f, -83f);

            foreach (var flag in new[] { Sky, Ambient, Music, Name })
                Assert.AreEqual(3u, Shown(after, camp, flag, underground: false), $"the camp's upper platforms ({flag})");
            Assert.AreEqual(3u, Shown(after, new Vector3(60f, 82.9f, -40f), Name, underground: true), "its lower platforms");
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
            Assert.AreEqual(WarnetCavernsRegions.Volumes.Length, once.Except(before).Count(), "the new rows");

            foreach (var sql in WarnetCavernsRegions.Up())
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

            foreach (var sql in WarnetCavernsRegions.Up())
                StringAssert.Contains(migrator.GenerateScript(Previous, Migration), sql);
            foreach (var sql in WarnetCavernsRegions.Down())
                StringAssert.Contains(migrator.GenerateScript(Migration, Previous), sql);
        }

        /// <summary>The highest-priority region here that turns the setting on, 0 for none (the map's own).</summary>
        private static uint Shown(List<MapRegion> volumes, Vector3 at, int flag, bool underground) =>
            volumes.Where(v => v.Enabled && Warnet.TryGetValue(v.RegionId, out var r) && (r.Flags & flag) != 0 && v.Contains(at, underground))
                .OrderByDescending(v => Warnet[v.RegionId].Priority).Select(v => v.RegionId).FirstOrDefault();

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == WarnetCavernsRegions.MapContextId)
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT map_context_id || ' ' || region_id || ' ' || shape || ' ' || printf('%.1f %.1f %.1f %.1f %.1f %.1f %.1f %.1f', pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y) || ' ' || underground || ' ' || enabled || ' ' || comment AS Value " +
                    "FROM map_region")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
