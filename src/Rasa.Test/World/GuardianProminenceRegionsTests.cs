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

    /// <summary>Guardian Prominence's caves, ruins and the drop below it (Guardian_prominence_regions, GuardianProminenceRegions).</summary>
    [TestClass]
    [DoNotParallelize]
    public class GuardianProminenceRegionsTests
    {
        private const string Migration = "20270704000000_Guardian_prominence_regions";
        private const string Previous = "20270703000000_Caves_of_donn_regions";

        private const int Sky = 1, Name = 16;

        /// <summary>Guardian Prominence's regions, as the .map has them: priority and the settings they turn on.</summary>
        private static readonly Dictionary<uint, (int Priority, int Flags)> Prominence = new()
        {
            [1] = (1, 13), [9] = (2, 16), [10] = (3, 16), [4] = (4, 0), [5] = (5, 0), [6] = (6, 0), [7] = (7, 0), [8] = (8, 0),
            [3] = (9, 31), [2] = (10, 15), [11] = (11, 31), [12] = (12, 17),
        };

        [TestMethod]
        public void UnderTheCloudsIsTheDarkFallingSky()
        {
            var (before, after) = Migrated();
            var west = new Vector3(-296f, 170.8f, 56f);

            Assert.AreEqual(0u, Shown(before, west, Sky, underground: false), "the flanks had the mountain's sky");
            Assert.AreEqual(12u, Shown(after, west, Sky, underground: false), "the dark falling sky down the west flank");
            Assert.AreEqual(12u, Shown(after, west, Name, underground: false), "still named Guardian Prominence");
            Assert.AreEqual(12u, Shown(after, new Vector3(248f, 263.9f, -100f), Sky, underground: false), "and under Mount Epoch");
            Assert.AreEqual(9u, Shown(after, new Vector3(174f, 349.5f, 14f), Name, underground: false), "Mount Epoch on top, as before");
            Assert.AreEqual(0u, Shown(after, new Vector3(174f, 349.5f, 14f), Sky, underground: false), "under the mountain's sky");
            Assert.AreEqual(10u, Shown(after, new Vector3(-56f, 340.3f, 19f), Name, underground: false), "the Temple of the Ages, as before");
            Assert.AreEqual(2u, Shown(after, new Vector3(-55f, 303.7f, 79f), Sky, underground: true), "not Professor Anand's cave, the lowest floor");
        }

        [TestMethod]
        public void TheRuinsAreTheRoofedCavern()
        {
            var (before, after) = Migrated();
            var plateau = new Vector3(220f, 343.5f, 180f);

            Assert.AreEqual(11u, Shown(after, new Vector3(185f, 326f, 244f), Name, underground: false), "Prominence Ruins");
            Assert.AreEqual(11u, Shown(after, new Vector3(185f, 326f, 244f), Sky, underground: false), "under the cave's sky");
            Assert.AreEqual(11u, Shown(before, plateau, Sky, underground: false), "the circle put the cave's sky on the plateau");
            Assert.AreEqual(0u, Shown(after, plateau, Sky, underground: false), "which has the mountain's again");
            Assert.AreEqual(0u, Shown(after, plateau, Name, underground: false), "and its name");
            Assert.AreEqual(3u, Shown(after, new Vector3(342f, 333.2f, 256f), Name, underground: false), "the Chamber of Eternity, as before");
        }

        [TestMethod]
        public void TheCavesHaveTheCaveSky()
        {
            var (before, after) = Migrated();
            var wayIn = new Vector3(163.5f, 314.6f, 200.3f);

            Assert.AreEqual(0u, Shown(before, wayIn, Sky, underground: true), "the cave of the way in had the mountain's sky");
            Assert.AreEqual(2u, Shown(after, wayIn, Sky, underground: true), "where the instance puts you");
            Assert.AreEqual(2u, Shown(after, new Vector3(108f, 331.1f, 235f), Sky, underground: true), "the passage up to the ruins");
            Assert.AreEqual(2u, Shown(after, new Vector3(-55f, 303.7f, 79f), Sky, underground: true), "Professor Anand's cave");
            Assert.AreEqual(2u, Shown(after, new Vector3(-120f, 309.3f, 136f), Sky, underground: true), "and its mouth");
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
            Assert.AreEqual(GuardianProminenceRegions.Volumes.Length + GuardianProminenceRegions.Recuts.Length, once.Except(before).Count(), "the new rows and the recut ones");

            foreach (var sql in GuardianProminenceRegions.Up())
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

            foreach (var sql in GuardianProminenceRegions.Up())
                StringAssert.Contains(migrator.GenerateScript(Previous, Migration), sql);
            foreach (var sql in GuardianProminenceRegions.Down())
                StringAssert.Contains(migrator.GenerateScript(Migration, Previous), sql);
        }

        /// <summary>The highest-priority region here that turns the setting on, 0 for none (the map's own).</summary>
        private static uint Shown(List<MapRegion> volumes, Vector3 at, int flag, bool underground) =>
            volumes.Where(v => v.Enabled && Prominence.TryGetValue(v.RegionId, out var r) && (r.Flags & flag) != 0 && v.Contains(at, underground))
                .OrderByDescending(v => Prominence[v.RegionId].Priority).Select(v => v.RegionId).FirstOrDefault();

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == GuardianProminenceRegions.MapContextId)
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT map_context_id || ' ' || region_id || ' ' || shape || ' ' || printf('%.1f %.1f %.1f %.1f %.1f %.1f %.1f %.1f', pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y) || ' ' || underground || ' ' || enabled || ' ' || comment AS Value " +
                    "FROM map_region")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
