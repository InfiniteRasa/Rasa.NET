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

    /// <summary>The Phanin Research Facility's rooms and levels (Phanin_research_regions, PhaninResearchRegions).</summary>
    [TestClass]
    [DoNotParallelize]
    public class PhaninResearchRegionsTests
    {
        private const string Migration = "20270623000000_Phanin_research_regions";
        private const string Previous = "20270622000000_Tahrendra_base_regions";

        private const int Sky = 1, Name = 16;

        /// <summary>The Phanin Research Facility's regions, as the .map has them: priority and the settings they turn on.</summary>
        private static readonly Dictionary<uint, (int Priority, int Flags)> Phanin = new()
        {
            [1] = (1, 13), [18] = (2, 15), [13] = (3, 16), [11] = (4, 0), [12] = (5, 0), [2] = (6, 16), [14] = (7, 16), [15] = (8, 16), [16] = (9, 16),
            [3] = (10, 16), [4] = (11, 16), [5] = (12, 16), [17] = (13, 16), [6] = (14, 16), [7] = (15, 16), [9] = (16, 0), [10] = (17, 16),
        };

        [TestMethod]
        public void EachRoomHasItsName()
        {
            var (before, after) = Migrated();
            var mindControl = new Vector3(-90f, -47.9f, 20f);

            Assert.AreEqual(3u, Shown(before, mindControl, Name, underground: false), "the warehouse's circle reached the floor below");
            Assert.AreEqual(4u, Shown(after, mindControl, Name, underground: false), "Atta Mind Control Room");
            Assert.AreEqual(3u, Shown(after, new Vector3(-91f, -23.5f, -13f), Name, underground: false), "Phanin Warehouse");
            Assert.AreEqual(13u, Shown(after, new Vector3(-56f, -15.8f, -100f), Name, underground: false), "Security Alpha");
            Assert.AreEqual(14u, Shown(after, new Vector3(-176f, -23.9f, 56f), Name, underground: false), "Security Beta");
            Assert.AreEqual(15u, Shown(after, new Vector3(-180f, -39.9f, -75f), Name, underground: false), "Security Gamma");
            Assert.AreEqual(16u, Shown(after, new Vector3(-220f, -39.9f, -12f), Name, underground: false), "Security Delta");
            Assert.AreEqual(6u, Shown(after, new Vector3(0f, -47.3f, 36f), Name, underground: false), "Mind Control Power Plant");
            Assert.AreEqual(10u, Shown(after, new Vector3(-56f, -39.3f, 93f), Name, underground: false), "Mind Control Transmission Center");
            Assert.AreEqual(7u, Shown(after, new Vector3(-104f, -39.3f, 79f), Name, underground: false), "Phanin's Private Quarters");
            Assert.AreEqual(5u, Shown(after, new Vector3(-18f, -29.1f, -56f), Name, underground: false), "Neural Lab");
            Assert.AreEqual(17u, Shown(after, new Vector3(-107f, -47.3f, -69f), Name, underground: false), "Neuron Processing");
            Assert.AreEqual(0u, Shown(after, new Vector3(-9f, -7.3f, 13f), Name, underground: false), "the way in keeps the facility's name");
            Assert.AreEqual(0u, Shown(after, new Vector3(-116f, -14.3f, -64f), Name, underground: false), "and so does the guard dormitory over Neuron Processing");
            Assert.AreEqual(18u, Shown(after, new Vector3(50f, -17.2f, -380f), Sky, underground: false), "the Atta cave has its cave sky");
        }

        [TestMethod]
        public void EachLayerHasItsMinimap()
        {
            var (before, after) = Migrated();
            var warehouse = new Vector3(-91f, -23.5f, -13f);

            Assert.AreEqual(12u, Layer(before, warehouse), "the lower layer covered the upper floors too");
            Assert.AreEqual(11u, Layer(after, warehouse), "the warehouse is on the upper layer");
            Assert.AreEqual(11u, Layer(after, new Vector3(-56f, -15.8f, -100f)), "with Security Alpha");
            Assert.AreEqual(11u, Layer(after, new Vector3(-176f, -23.9f, 56f)), "and Security Beta");
            Assert.AreEqual(11u, Layer(after, new Vector3(-9f, -7.3f, 13f)), "and the way in");
            Assert.AreEqual(12u, Layer(after, new Vector3(-90f, -47.9f, 20f)), "the mind control room is on the lower layer");
            Assert.AreEqual(12u, Layer(after, new Vector3(-18f, -29.1f, -56f)), "with the Neural Lab");
            Assert.AreEqual(12u, Layer(after, new Vector3(-220f, -39.9f, -12f)), "and Security Delta");
            Assert.AreEqual(12u, Layer(after, new Vector3(-56f, -39.3f, 93f)), "and the transmission center");
        }

        /// <summary>The highest-priority minimap layer (a region that sets nothing but its own minimap) here, 0 for the map's own.</summary>
        private static uint Layer(List<MapRegion> volumes, Vector3 at) =>
            volumes.Where(v => v.Enabled && Layers.Contains(v.RegionId) && v.Contains(at, false))
                .OrderByDescending(v => Phanin[v.RegionId].Priority).Select(v => v.RegionId).FirstOrDefault();

        private static readonly uint[] Layers = { 11, 12 };

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
            Assert.AreEqual(PhaninResearchRegions.Volumes.Length + PhaninResearchRegions.Recuts.Length, once.Except(before).Count(), "the new rows and the recut ones");

            foreach (var sql in PhaninResearchRegions.Up())
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

            foreach (var sql in PhaninResearchRegions.Up())
                StringAssert.Contains(migrator.GenerateScript(Previous, Migration), sql);
            foreach (var sql in PhaninResearchRegions.Down())
                StringAssert.Contains(migrator.GenerateScript(Migration, Previous), sql);
        }

        /// <summary>The highest-priority region here that turns the setting on, 0 for none (the map's own).</summary>
        private static uint Shown(List<MapRegion> volumes, Vector3 at, int flag, bool underground) =>
            volumes.Where(v => v.Enabled && Phanin.TryGetValue(v.RegionId, out var r) && (r.Flags & flag) != 0 && v.Contains(at, underground))
                .OrderByDescending(v => Phanin[v.RegionId].Priority).Select(v => v.RegionId).FirstOrDefault();

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == PhaninResearchRegions.MapContextId)
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT map_context_id || ' ' || region_id || ' ' || shape || ' ' || printf('%.1f %.1f %.1f %.1f %.1f %.1f %.1f %.1f', pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y) || ' ' || underground || ' ' || enabled || ' ' || comment AS Value " +
                    "FROM map_region")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
