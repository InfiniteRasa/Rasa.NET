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

    /// <summary>The Rivasa Atta Colony's chambers, green light and Bane Incursion (Rivasa_atta_colony_regions, RivasaAttaColonyRegions).</summary>
    [TestClass]
    [DoNotParallelize]
    public class RivasaAttaColonyRegionsTests
    {
        private const string Migration = "20270711000000_Rivasa_atta_colony_regions";
        private const string Previous = "20270704000000_Guardian_prominence_regions";

        private const int Sky = 1, Env = 2, Music = 8, Name = 16;

        /// <summary>The colony's regions, as the .map has them: priority and the settings they turn on.</summary>
        private static readonly Dictionary<uint, (int Priority, int Flags)> Colony = new()
        {
            [2] = (1, 13), [4] = (3, 5), [5] = (4, 17), [6] = (5, 1), [7] = (6, 13), [8] = (7, 29), [9] = (8, 29), [10] = (9, 27),
            [11] = (10, 1), [12] = (11, 1), [1] = (12, 1),
        };

        [TestMethod]
        public void TheBurialChamberHasItsGreenLight()
        {
            var (before, after) = Migrated();
            var label = new Vector3(110f, 33.6f, -43.5f);
            var underground = new Vector3(101.4f, 44.6f, -119.8f);

            Assert.AreEqual(5u, Shown(before, label, Sky, underground: false), "the Burial Chamber's blue sky");
            Assert.AreEqual(7u, Shown(after, label, Sky, underground: false), "the green sky of its lights");
            Assert.AreEqual(7u, Shown(after, label, Music, underground: false), "with its music");
            Assert.AreEqual(5u, Shown(after, label, Name, underground: false), "still the Rivasa Burial Chamber");
            Assert.AreEqual(0u, Shown(before, underground, Name, underground: true), "the circle missed its floor under the ground");
            Assert.AreEqual(5u, Shown(after, underground, Name, underground: true), "which the cavern's box takes");
            Assert.AreEqual(7u, Shown(after, new Vector3(131.2f, -34.9f, -110.9f), Sky, underground: true), "down to the pit");
            Assert.AreEqual(5u, Shown(before, new Vector3(-64f, 83.1f, 18.4f), Name, underground: false), "the circle named the rooms west of it");
            Assert.AreEqual(0u, Shown(after, new Vector3(-64f, 83.1f, 18.4f), Name, underground: false), "which it no longer does");
        }

        [TestMethod]
        public void TheQueensChamberAndTheFoodStorageAreTheirCaverns()
        {
            var (before, after) = Migrated();
            var passage = new Vector3(-214f, 56.8f, 320f);
            var pipeRoom = new Vector3(-108f, 77.4f, 268f);

            Assert.AreEqual(8u, Shown(after, new Vector3(-39f, 71.4f, 393f), Name, underground: false), "the Queen's Chamber at the throne");
            Assert.AreEqual(8u, Shown(before, pipeRoom, Sky, underground: false), "the circle gave the yellow sky to the room south of it");
            Assert.AreEqual(0u, Shown(after, pipeRoom, Sky, underground: false), "which has the colony's own again");
            Assert.AreEqual(7u, Shown(after, passage, Sky, underground: true), "the passage from the Food Storage is green");
            Assert.AreEqual(7u, Shown(after, new Vector3(-214f, 58.5f, 350f), Sky, underground: true), "along its length");
            Assert.AreEqual(9u, Shown(after, new Vector3(-397f, 42.3f, 279f), Name, underground: false), "the Rivasa Food Storage");
            Assert.AreEqual(9u, Shown(after, new Vector3(-397f, 42.3f, 279f), Sky, underground: false), "with its blue sky");
        }

        [TestMethod]
        public void TheBaneIncursionIsTheChunnel()
        {
            var (before, after) = Migrated();
            var chunnel = new Vector3(-188f, 105.8f, 120f);

            Assert.AreEqual(0u, Shown(before, chunnel, Name, underground: false), "the chunnel had no region");
            Assert.AreEqual(10u, Shown(after, chunnel, Name, underground: false), "Bane Incursion");
            Assert.AreEqual(10u, Shown(after, chunnel, Sky, underground: false), "with the Bane interior");
            Assert.AreEqual(10u, Shown(after, new Vector3(-192f, 113.6f, 135f), Env, underground: false), "along it");
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
            Assert.AreEqual(RivasaAttaColonyRegions.Volumes.Length + RivasaAttaColonyRegions.Recuts.Length, once.Except(before).Count(), "the new rows and the recut ones");

            foreach (var sql in RivasaAttaColonyRegions.Up())
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

            foreach (var sql in RivasaAttaColonyRegions.Up())
                StringAssert.Contains(migrator.GenerateScript(Previous, Migration), sql);
            foreach (var sql in RivasaAttaColonyRegions.Down())
                StringAssert.Contains(migrator.GenerateScript(Migration, Previous), sql);
        }

        /// <summary>The highest-priority region here that turns the setting on, 0 for none (the map's own).</summary>
        private static uint Shown(List<MapRegion> volumes, Vector3 at, int flag, bool underground) =>
            volumes.Where(v => v.Enabled && Colony.TryGetValue(v.RegionId, out var r) && (r.Flags & flag) != 0 && v.Contains(at, underground))
                .OrderByDescending(v => Colony[v.RegionId].Priority).Select(v => v.RegionId).FirstOrDefault();

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == RivasaAttaColonyRegions.MapContextId)
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT map_context_id || ' ' || region_id || ' ' || shape || ' ' || printf('%.1f %.1f %.1f %.1f %.1f %.1f %.1f %.1f', pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y) || ' ' || underground || ' ' || enabled || ' ' || comment AS Value " +
                    "FROM map_region WHERE map_context_id = 2112")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
