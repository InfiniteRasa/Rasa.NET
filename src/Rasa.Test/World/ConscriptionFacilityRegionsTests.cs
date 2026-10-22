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

    /// <summary>The Bane Conscription Facility's way in, cell blocks, central rooms and bridge (Conscription_facility_regions, ConscriptionFacilityRegions).</summary>
    [TestClass]
    [DoNotParallelize]
    public class ConscriptionFacilityRegionsTests
    {
        private const string Migration = "20270613000000_Conscription_facility_regions";
        private const string Previous = "20270612000000_Fluxite_mines_regions";

        private const int Sky = 1, Env = 2, Ambient = 4, Music = 8, Name = 16;

        /// <summary>The facility's regions, as the .map has them: priority and the settings they turn on.</summary>
        private static readonly Dictionary<uint, (int Priority, int Flags)> Facility = new()
        {
            [36] = (1, 1), [35] = (2, 23), [34] = (3, 0), [33] = (4, 0), [32] = (5, 0), [17] = (6, 20), [1] = (7, 1), [28] = (8, 23), [22] = (9, 23),
            [7] = (10, 23), [29] = (11, 23), [24] = (12, 23), [25] = (13, 23), [23] = (14, 23), [26] = (15, 23), [27] = (16, 23), [2] = (17, 23),
            [30] = (18, 23), [3] = (19, 23), [31] = (20, 23), [13] = (21, 23), [14] = (22, 23), [18] = (23, 23), [19] = (24, 23), [9] = (25, 23),
            [10] = (26, 23),
        };

        [TestMethod]
        public void FromTheGateToTheRoundHall()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();
            var entry = new Vector3(7f, 153.5f, -645f);

            migrator.Migrate(Previous);
            Assert.AreEqual(0u, Shown(Volumes(database), entry, Name, underground: true), "nothing named the way in");

            migrator.Migrate(Migration);
            var after = Volumes(database);
            uint At(float x, float y, float z, bool underground) => Shown(after, new Vector3(x, y, z), Name, underground);

            Assert.AreEqual(28u, At(7f, 153.5f, -645f, true), "Entry, through the gate");
            Assert.AreEqual(28u, At(7f, 153.4f, -625f, false), "at the chunnel's mouth");
            Assert.AreEqual(35u, At(-125f, 142.3f, -575f, true), "Hidden Cave, its mouth on the yard");
            Assert.AreEqual(35u, At(-120f, 120.6f, -630f, true), "down its cavern");
            Assert.AreEqual(35u, At(-113f, 114.3f, -602f, false), "at the logos dispenser");
            Assert.AreEqual(35u, Shown(after, new Vector3(-113f, 114.3f, -602f), Sky, underground: false), "under the cave sky");
            Assert.AreEqual(22u, At(7f, 81.1f, -360f, true), "Central Service Passage");
            Assert.AreEqual(22u, At(20f, 96f, -380f, true));
            Assert.AreEqual(7u, At(7f, 104.9f, -320f, true), "Central Operations");
            Assert.AreEqual(7u, Shown(after, new Vector3(7f, 104.9f, -320f), Sky, underground: true), "under the Bane interior sky");
            Assert.AreEqual(29u, Shown(after, new Vector3(7f, 109.7f, -285f), Sky, underground: false), "and the desert sky at its window");
            Assert.AreEqual(17u, At(7f, 69.3f, -200f, false), "Immolation Bridge");
            Assert.AreEqual(17u, At(7f, 69.3f, -130f, false));
            Assert.AreEqual(17u, At(30f, 69.2f, -100f, false), "the landing at the far end");
            Assert.AreEqual(17u, Shown(after, new Vector3(7f, 69.3f, -200f), Ambient, underground: false), "with the volcano's roar");
            Assert.AreEqual(9u, At(7f, 78.3f, 150f, true), "Information Extraction");
            Assert.AreEqual(9u, At(-26f, 77.7f, 130f, true), "at an extraction chair");
            Assert.AreEqual(9u, At(40f, 77.7f, 172f, true), "and another");
        }

        [TestMethod]
        public void TheTwoMirroredCellBlocks()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Migration);
            var after = Volumes(database);
            uint At(float x, float y, float z) => Shown(after, new Vector3(x, y, z), Name, underground: true);

            Assert.AreEqual(26u, At(-257f, 97.8f, -320f), "Cell Block - West");
            Assert.AreEqual(26u, At(-237f, 101.9f, -340f));
            Assert.AreEqual(27u, At(271f, 97.8f, -320f), "Cell Block - East");
            Assert.AreEqual(27u, At(223f, 98f, -350f));
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
            Assert.AreEqual(ConscriptionFacilityRegions.Volumes.Length, once.Except(before).Count(), "the new rows");

            foreach (var sql in ConscriptionFacilityRegions.Up())
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

            foreach (var sql in ConscriptionFacilityRegions.Up())
                StringAssert.Contains(migrator.GenerateScript(Previous, Migration), sql);
            foreach (var sql in ConscriptionFacilityRegions.Down())
                StringAssert.Contains(migrator.GenerateScript(Migration, Previous), sql);
        }

        /// <summary>The highest-priority region here that turns the setting on, 0 for none (the map's own).</summary>
        private static uint Shown(List<MapRegion> volumes, Vector3 at, int flag, bool underground) =>
            volumes.Where(v => v.Enabled && Facility.TryGetValue(v.RegionId, out var r) && (r.Flags & flag) != 0 && v.Contains(at, underground))
                .OrderByDescending(v => Facility[v.RegionId].Priority).Select(v => v.RegionId).FirstOrDefault();

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == ConscriptionFacilityRegions.MapContextId)
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT map_context_id || ' ' || region_id || ' ' || shape || ' ' || printf('%.1f %.1f %.1f %.1f %.1f %.1f %.1f %.1f', pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y) || ' ' || underground || ' ' || enabled || ' ' || comment AS Value " +
                    "FROM map_region")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
