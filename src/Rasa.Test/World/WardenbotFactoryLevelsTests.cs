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

    /// <summary>The The Wardenbot Factory's levels and access ramps (Wardenbot_factory_levels, WardenbotFactoryLevels).</summary>
    [TestClass]
    [DoNotParallelize]
    public class WardenbotFactoryLevelsTests
    {
        private const string Migration = "20270222000000_Wardenbot_factory_levels";
        private const string Previous = "20270221000000_Convict_pass_regions";

        private const int Sky = 1, Env = 2, Ambient = 4, Music = 8, Name = 16;

        /// <summary>The factory's regions, as the .map has them: priority and the settings they turn on.</summary>
        private static readonly Dictionary<uint, (int Priority, int Flags)> Factory = new()
        {
            [1] = (1, 13), [12] = (2, 1), [2] = (3, 23), [3] = (4, 23), [8] = (5, 23), [5] = (6, 16), [11] = (7, 19), [9] = (8, 16), [6] = (9, 16),
            [10] = (10, 16), [7] = (11, 16), [18] = (12, 31), [21] = (13, 0), [19] = (14, 0), [22] = (15, 0), [20] = (16, 0),
        };

        [TestMethod]
        public void EachFloorAndEachRampDownIsNamed()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            Assert.AreEqual(0u, Shown(Volumes(database), new Vector3(-272f, 104.7f, 80f), Name, underground: false), "the entrance was unnamed");

            migrator.Migrate(Migration);
            var after = Volumes(database);
            uint At(float x, float y, float z, int flag = Name) => Shown(after, new Vector3(x, y, z), flag, underground: false);

            Assert.AreEqual(11u, At(-272f, 104.7f, 80f), "Level 1 at the entrance hub");
            Assert.AreEqual(11u, At(-272f, 104.7f, 80f, Sky), "under its WBRoom sky");
            Assert.AreEqual(11u, At(-456f, 97f, 74f), "Level 1's north corridors");
            Assert.AreEqual(11u, At(-360f, 97f, 52f), "and those at the dome's north-east");
            Assert.AreEqual(11u, At(-424f, 80.3f, 64f), "Raw Material Handling's north door");
            Assert.AreEqual(2u, At(-424f, 71.7f, -12f), "Raw Material Handling itself is as it was");

            Assert.AreEqual(9u, At(-424f, 80.6f, -88f), "Level 2 Access Ramp from the dome's south door");
            Assert.AreEqual(9u, At(-430f, 71.1f, -120f), "down the first ramp");
            Assert.AreEqual(9u, At(-440f, 59.3f, -150f), "at the switchback");
            Assert.AreEqual(9u, At(-440f, 43.2f, -100f), "at the foot of the last");
            Assert.AreEqual(6u, At(-420f, 40.9f, 0f), "Level 2 under Raw Material Handling");

            Assert.AreEqual(10u, At(-224f, 19.3f, -110f), "Level 3 Access Ramp");
            Assert.AreEqual(10u, At(-224f, 6.5f, -70f), "down to the bottom");
            Assert.AreEqual(7u, At(-280f, 0.3f, 12f), "Level 3");
            Assert.AreEqual(7u, At(-320f, 1.4f, -10f), "Level 3's west corridors");
            Assert.AreEqual(0u, At(-288f, 31.7f, -44f), "the Bot Programming dome is left to its own region");
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
            Assert.AreEqual(WardenbotFactoryLevels.Volumes.Length, once.Except(before).Count());

            foreach (var sql in WardenbotFactoryLevels.Up())
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

            foreach (var sql in WardenbotFactoryLevels.Up())
                StringAssert.Contains(migrator.GenerateScript(Previous, Migration), sql);
            foreach (var sql in WardenbotFactoryLevels.Down())
                StringAssert.Contains(migrator.GenerateScript(Migration, Previous), sql);
        }

        /// <summary>The highest-priority region here that turns the setting on, 0 for none (the map's own).</summary>
        private static uint Shown(List<MapRegion> volumes, Vector3 at, int flag, bool underground) =>
            volumes.Where(v => v.Enabled && Factory.TryGetValue(v.RegionId, out var r) && (r.Flags & flag) != 0 && v.Contains(at, underground))
                .OrderByDescending(v => Factory[v.RegionId].Priority).Select(v => v.RegionId).FirstOrDefault();

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == WardenbotFactoryLevels.MapContextId)
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT map_context_id || ' ' || region_id || ' ' || shape || ' ' || printf('%.1f %.1f %.1f %.1f %.1f', pos_x, pos_z, radius, half_x, half_z) || ' ' || enabled AS Value " +
                    "FROM map_region")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
