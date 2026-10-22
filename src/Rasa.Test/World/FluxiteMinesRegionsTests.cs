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

    /// <summary>The Bane Fluxite Mines' places (Fluxite_mines_regions, FluxiteMinesRegions).</summary>
    [TestClass]
    [DoNotParallelize]
    public class FluxiteMinesRegionsTests
    {
        private const string Migration = "20270612000000_Fluxite_mines_regions";
        private const string Previous = "20270611000000_Eloh_temples_regions";

        private const int Sky = 1, Env = 2, Ambient = 4, Music = 8, Name = 16;

        /// <summary>The mines' regions, as the .map has them: priority and the settings they turn on.</summary>
        private static readonly Dictionary<uint, (int Priority, int Flags)> Mines = new()
        {
            [1] = (1, 1), [17] = (2, 1), [15] = (3, 0), [16] = (4, 0), [10] = (5, 20), [2] = (6, 23), [9] = (7, 23), [12] = (8, 20), [13] = (9, 20),
            [3] = (10, 20), [5] = (11, 23), [4] = (12, 20), [14] = (13, 19), [8] = (14, 20),
        };

        [TestMethod]
        public void TheCavernsHaveTheirNamesWhereTheCirclesMissed()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();
            var miningLabel = new Vector3(98f, 142.2f, 261f);
            var vaporLabel = new Vector3(294.75f, 89.6f, 254f);
            var vents = new Vector3(428f, 107.6f, 436f);

            migrator.Migrate(Previous);
            var before = Volumes(database);
            Assert.AreEqual(0u, Shown(before, miningLabel, Name, underground: false), "the Mining Chamber's underground-only circle missed its own label");
            Assert.AreEqual(0u, Shown(before, vaporLabel, Name, underground: false), "and Vapor Chasm's");
            Assert.AreEqual(4u, Shown(before, vents, Name, underground: false), "the Ore Prep Chamber's circle reached the vents");

            migrator.Migrate(Migration);
            var after = Volumes(database);
            uint At(float x, float y, float z, bool underground) => Shown(after, new Vector3(x, y, z), Name, underground);

            Assert.AreEqual(14u, Shown(after, miningLabel, Name, underground: false), "Mining Chamber at its label");
            Assert.AreEqual(14u, Shown(after, miningLabel, Sky, underground: false), "under its cave sky");
            Assert.AreEqual(14u, At(60f, 132.6f, 400f, true), "its north");
            Assert.AreEqual(14u, At(150f, 142.8f, 150f, true), "its south");
            Assert.AreEqual(5u, Shown(after, vaporLabel, Name, underground: false), "Vapor Chasm at its label");
            Assert.AreEqual(5u, At(250f, 95.8f, 150f, true), "its south end");
            Assert.AreEqual(4u, At(220f, 104.3f, 466f, true), "the Ore Prep Chamber");
            Assert.AreEqual(4u, At(204f, 113.6f, 420f, true), "its galleries");
            Assert.AreEqual(4u, At(268f, 107.6f, 470f, false), "its machine hall");
            Assert.AreEqual(8u, Shown(after, vents, Name, underground: false), "Ventilation, at the vent controls");
            Assert.AreEqual(8u, At(412f, 106.3f, 484f, false), "its tee");
        }

        [TestMethod]
        public void TheWayInItsCorridorsFlightControlAndTheScan()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Migration);
            var after = Volumes(database);
            uint At(float x, float y, float z, bool underground) => Shown(after, new Vector3(x, y, z), Name, underground);

            Assert.AreEqual(10u, At(-30f, 144.8f, -15f, false), "Main Passage at the way in");
            Assert.AreEqual(10u, At(50f, 145f, 20f, false), "along the chunnel");
            Assert.AreEqual(10u, At(100f, 144.7f, 60f, false), "to the mine");
            Assert.AreEqual(12u, At(52f, 152.1f, -20f, true), "Maintenance, the corridors south");
            Assert.AreEqual(12u, At(76f, 152.2f, -36f, true));
            Assert.AreEqual(12u, At(28f, 152.1f, 36f, false), "and north");
            Assert.AreEqual(9u, At(120f, 152.6f, -60f, false), "Flight Control at its window");
            Assert.AreEqual(9u, Shown(after, new Vector3(120f, 152.6f, -60f), Sky, underground: false), "under the Mires sky");
            Assert.AreEqual(3u, At(-12f, 143.8f, 124f, false), "Deep Planet Scan");
            Assert.AreEqual(2u, At(186f, 106.4f, -67f, false), "the Dropship Area is as it was");
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
            Assert.AreEqual(FluxiteMinesRegions.Volumes.Length + FluxiteMinesRegions.Recuts.Length, once.Except(before).Count(), "the new rows and the recut ones");

            foreach (var sql in FluxiteMinesRegions.Up())
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

            foreach (var sql in FluxiteMinesRegions.Up())
                StringAssert.Contains(migrator.GenerateScript(Previous, Migration), sql);
            foreach (var sql in FluxiteMinesRegions.Down())
                StringAssert.Contains(migrator.GenerateScript(Migration, Previous), sql);
        }

        /// <summary>The highest-priority region here that turns the setting on, 0 for none (the map's own).</summary>
        private static uint Shown(List<MapRegion> volumes, Vector3 at, int flag, bool underground) =>
            volumes.Where(v => v.Enabled && Mines.TryGetValue(v.RegionId, out var r) && (r.Flags & flag) != 0 && v.Contains(at, underground))
                .OrderByDescending(v => Mines[v.RegionId].Priority).Select(v => v.RegionId).FirstOrDefault();

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == FluxiteMinesRegions.MapContextId)
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT map_context_id || ' ' || region_id || ' ' || shape || ' ' || printf('%.1f %.1f %.1f %.1f %.1f %.1f %.1f %.1f', pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y) || ' ' || underground || ' ' || enabled || ' ' || comment AS Value " +
                    "FROM map_region")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
