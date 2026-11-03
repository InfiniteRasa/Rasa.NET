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

    /// <summary>The epic Caves of Donn's places (Caves_of_donn_epic_regions, CavesOfDonnEpicRegions).</summary>
    [TestClass]
    [DoNotParallelize]
    public class CavesOfDonnEpicRegionsTests
    {
        private const string Migration = "20270702000000_Caves_of_donn_epic_regions";
        private const string Previous = "20270701000000_Torcastra_prison_region";

        private const int Sky = 1, Env = 2, Name = 16;

        /// <summary>The epic Caves of Donn's regions, as the .map has them: priority and the settings they turn on.</summary>
        private static readonly Dictionary<uint, (int Priority, int Flags)> Donn = new()
        {
            [13] = (1, 0), [12] = (2, 0), [9] = (3, 6), [15] = (4, 11), [3] = (5, 18), [4] = (6, 27), [2] = (7, 7), [16] = (8, 15),
            [5] = (9, 18), [7] = (10, 23), [10] = (11, 7), [11] = (12, 0), [8] = (13, 15), [6] = (14, 30), [1] = (15, 13), [14] = (16, 13),
        };

        [TestMethod]
        public void EachCavernHasItsName()
        {
            var (before, after) = Migrated();
            var marFend = new Vector3(-322f, 21.8f, -228f);

            Assert.AreEqual(0u, Shown(before, marFend, Name, underground: false), "the caverns had only the map's name");
            Assert.AreEqual(6u, Shown(after, marFend, Name, underground: false), "Mar Fend, on the hill");
            Assert.AreEqual(6u, Shown(after, new Vector3(-298f, 7.5f, -238f), Name, underground: false), "and the burned village below it");
            Assert.AreEqual(7u, Shown(after, new Vector3(-191f, 6.2f, -145f), Name, underground: false), "Spirit Cave");
            Assert.AreEqual(7u, Shown(after, new Vector3(-191f, 6.2f, -145f), Sky, underground: false), "with its cave hall sky");
            Assert.AreEqual(5u, Shown(after, new Vector3(-160f, 36.4f, -325f), Name, underground: false), "the Eloh Chantry");
            Assert.AreEqual(3u, Shown(after, new Vector3(7.5f, 21.6f, -232f), Name, underground: false), "Undone Fairway");
            Assert.AreEqual(4u, Shown(after, new Vector3(160f, 4.8f, -302f), Name, underground: false), "the Vitiated Shrine");
            Assert.AreEqual(4u, Shown(after, new Vector3(160f, 4.8f, -302f), Sky, underground: false), "with its big-room sky");
            Assert.AreEqual(0u, Shown(after, marFend, Sky, underground: false), "the open caverns keep the map's own sky");
        }

        [TestMethod]
        public void TheElohRoomsHaveTheElohSky()
        {
            var (_, after) = Migrated();

            Assert.AreEqual(8u, Shown(after, new Vector3(278f, 5.4f, -302f), Sky, underground: true), "the hidden chamber");
            Assert.AreEqual(15u, Shown(after, new Vector3(-320f, 25.1f, -359f), Sky, underground: true), "the Eloh corridor by the entrance");
            Assert.AreEqual(9u, Shown(after, new Vector3(-275.6f, 25.4f, -363f), Env, underground: true), "the entrance tunnel keeps region 9");
        }

        [TestMethod]
        public void TheTunnelsHaveTheCaveSky()
        {
            var (_, after) = Migrated();

            Assert.AreEqual(2u, Shown(after, new Vector3(-290f, 10.1f, -105f), Sky, underground: true), "west cavern to Spirit Cave");
            Assert.AreEqual(2u, Shown(after, new Vector3(-200f, 11f, -205f), Sky, underground: true), "Spirit Cave to the Chantry");
            Assert.AreEqual(2u, Shown(after, new Vector3(-100f, 16.8f, -296f), Sky, underground: true), "the Chantry to Undone Fairway");
            Assert.AreEqual(16u, Shown(after, new Vector3(95f, 8.3f, -280f), Sky, underground: true), "Undone Fairway down to the shrine");
            Assert.AreEqual(3u, Shown(after, new Vector3(78f, 35f, -232f), Name, underground: false), "over the tunnel, the cavern's ledge is still the cavern's");
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
            Assert.AreEqual(CavesOfDonnEpicRegions.Volumes.Length, once.Except(before).Count(), "the new rows");

            foreach (var sql in CavesOfDonnEpicRegions.Up())
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

            foreach (var sql in CavesOfDonnEpicRegions.Up())
                StringAssert.Contains(migrator.GenerateScript(Previous, Migration), sql);
            foreach (var sql in CavesOfDonnEpicRegions.Down())
                StringAssert.Contains(migrator.GenerateScript(Migration, Previous), sql);
        }

        /// <summary>The highest-priority region here that turns the setting on, 0 for none (the map's own).</summary>
        private static uint Shown(List<MapRegion> volumes, Vector3 at, int flag, bool underground) =>
            volumes.Where(v => v.Enabled && Donn.TryGetValue(v.RegionId, out var r) && (r.Flags & flag) != 0 && v.Contains(at, underground))
                .OrderByDescending(v => Donn[v.RegionId].Priority).Select(v => v.RegionId).FirstOrDefault();

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == CavesOfDonnEpicRegions.MapContextId)
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT map_context_id || ' ' || region_id || ' ' || shape || ' ' || printf('%.1f %.1f %.1f %.1f %.1f %.1f %.1f %.1f', pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y) || ' ' || underground || ' ' || enabled || ' ' || comment AS Value " +
                    "FROM map_region")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
