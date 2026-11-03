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

    /// <summary>Staal Junkyard's Melthouse Sectors and Labbna Outpost (Staal_junkyard_regions, StaalJunkyardRegions).</summary>
    [TestClass]
    [DoNotParallelize]
    public class StaalJunkyardRegionsTests
    {
        private const string Migration = "20270710000000_Staal_junkyard_regions";
        private const string Previous = "20270704000000_Guardian_prominence_regions";

        private const int Sky = 1, Env = 2, Name = 16;

        /// <summary>Staal Junkyard's regions, as the .map has them: priority and the settings they turn on.</summary>
        private static readonly Dictionary<uint, (int Priority, int Flags)> Junkyard = new()
        {
            [7] = (1, 1), [2] = (2, 13), [4] = (4, 23), [5] = (5, 23), [6] = (6, 23),
        };

        [TestMethod]
        public void TheJunkyardHasItsOwnSky()
        {
            var (before, after) = Migrated();
            var west = new Vector3(-250f, 242.7f, -150f);
            var east = new Vector3(250f, 246f, -150f);

            Assert.AreEqual(0u, Shown(before, west, Sky, underground: false), "the junkyard had the map's Foreas yard sky");
            Assert.AreEqual(4u, Shown(after, west, Sky, underground: false), "the western lava field has the junkyard's");
            Assert.AreEqual(4u, Shown(after, west, Name, underground: false), "West Melthouse Sector");
            Assert.AreEqual(4u, Shown(after, new Vector3(-150f, 267f, 0f), Env, underground: false), "and its brown fog");
            Assert.AreEqual(5u, Shown(after, east, Sky, underground: false), "the eastern lava field");
            Assert.AreEqual(5u, Shown(after, east, Name, underground: false), "East Melthouse Sector");
            Assert.AreEqual(5u, Shown(after, new Vector3(29.7f, 241.4f, -336.9f), Name, underground: false), "the entrance from Burning Steps");
            Assert.AreEqual(5u, Shown(after, new Vector3(18f, 335.2f, 337f), Name, underground: false), "the prison on the top terrace");
            Assert.AreEqual(4u, Shown(after, new Vector3(-100f, 328.8f, 250f), Sky, underground: false), "the upper terraces too");
        }

        [TestMethod]
        public void LabbnaOutpostIsTheBrannOutpost()
        {
            var (before, after) = Migrated();
            var outpost = new Vector3(-392.3f, 265.6f, 144.9f);

            Assert.AreEqual(6u, Shown(after, outpost, Name, underground: false), "Labbna Outpost, at its label");
            Assert.AreEqual(6u, Shown(after, outpost, Sky, underground: false), "with the junkyard's sky");
            Assert.AreEqual(6u, Shown(after, new Vector3(-331f, 265.1f, 166f), Name, underground: false), "by its eastern dome");
            Assert.AreEqual(4u, Shown(after, new Vector3(-100f, 328.8f, 250f), Name, underground: false), "not the terraces east of it");
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
            Assert.AreEqual(StaalJunkyardRegions.Volumes.Length, once.Except(before).Count(), "the new rows");

            foreach (var sql in StaalJunkyardRegions.Up())
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

            foreach (var sql in StaalJunkyardRegions.Up())
                StringAssert.Contains(migrator.GenerateScript(Previous, Migration), sql);
            foreach (var sql in StaalJunkyardRegions.Down())
                StringAssert.Contains(migrator.GenerateScript(Migration, Previous), sql);
        }

        /// <summary>The highest-priority region here that turns the setting on, 0 for none (the map's own).</summary>
        private static uint Shown(List<MapRegion> volumes, Vector3 at, int flag, bool underground) =>
            volumes.Where(v => v.Enabled && Junkyard.TryGetValue(v.RegionId, out var r) && (r.Flags & flag) != 0 && v.Contains(at, underground))
                .OrderByDescending(v => Junkyard[v.RegionId].Priority).Select(v => v.RegionId).FirstOrDefault();

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == StaalJunkyardRegions.MapContextId)
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT map_context_id || ' ' || region_id || ' ' || shape || ' ' || printf('%.1f %.1f %.1f %.1f %.1f %.1f %.1f %.1f', pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y) || ' ' || underground || ' ' || enabled || ' ' || comment AS Value " +
                    "FROM map_region WHERE map_context_id = 2138")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
