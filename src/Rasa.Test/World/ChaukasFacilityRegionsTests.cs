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

    /// <summary>The Chaukas Robotics Facility's three rooms (Chaukas_facility_regions, ChaukasFacilityRegions).</summary>
    [TestClass]
    [DoNotParallelize]
    public class ChaukasFacilityRegionsTests
    {
        private const string Migration = "20270615000000_Chaukas_facility_regions";
        private const string Previous = "20270614000000_Iron_lung_region";

        private const int Sky = 1, Name = 16;

        /// <summary>The facility's regions, as the .map has them: priority and the settings they turn on.</summary>
        private static readonly Dictionary<uint, (int Priority, int Flags)> Facility = new()
        {
            [1] = (1, 15), [2] = (2, 17), [3] = (3, 17), [4] = (4, 17),
        };

        [TestMethod]
        public void TheBrainTheAssemblyRoomAndOreProcessing()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();
            var brain = new Vector3(-16f, 16f, 115f);

            migrator.Migrate(Previous);
            Assert.AreEqual(0, Volumes(database).Count, "no region of the facility had a volume");

            migrator.Migrate(Migration);
            var after = Volumes(database);
            uint At(float x, float y, float z) => Shown(after, new Vector3(x, y, z), Name, underground: false);

            Assert.AreEqual(2u, At(-16f, 16f, 115f), "the Chaukus Brain, at its computer");
            Assert.AreEqual(2u, At(-16f, 18f, 60f), "its middle");
            Assert.AreEqual(2u, Shown(after, brain, Sky, underground: false), "under the Wardenbot room sky");
            Assert.AreEqual(3u, At(60f, 16f, -80f), "Robotics Assembly");
            Assert.AreEqual(3u, At(130f, 16f, -160f));
            Assert.AreEqual(4u, At(-152f, 9.1f, -80f), "Ore Processing, round its machine");
            Assert.AreEqual(4u, At(-110f, 16.4f, -45f), "at its computers");
            Assert.AreEqual(0u, At(-26f, 17.4f, -273f), "the entrance is the map's");
            Assert.AreEqual(0u, At(-96f, 17.4f, -240f), "and the corridors");
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
            Assert.AreEqual(ChaukasFacilityRegions.Volumes.Length, once.Except(before).Count(), "the new rows");

            foreach (var sql in ChaukasFacilityRegions.Up())
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

            foreach (var sql in ChaukasFacilityRegions.Up())
                StringAssert.Contains(migrator.GenerateScript(Previous, Migration), sql);
            foreach (var sql in ChaukasFacilityRegions.Down())
                StringAssert.Contains(migrator.GenerateScript(Migration, Previous), sql);
        }

        /// <summary>The highest-priority region here that turns the setting on, 0 for none (the map's own).</summary>
        private static uint Shown(List<MapRegion> volumes, Vector3 at, int flag, bool underground) =>
            volumes.Where(v => v.Enabled && Facility.TryGetValue(v.RegionId, out var r) && (r.Flags & flag) != 0 && v.Contains(at, underground))
                .OrderByDescending(v => Facility[v.RegionId].Priority).Select(v => v.RegionId).FirstOrDefault();

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == ChaukasFacilityRegions.MapContextId)
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT map_context_id || ' ' || region_id || ' ' || shape || ' ' || printf('%.1f %.1f %.1f %.1f %.1f %.1f %.1f %.1f', pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y) || ' ' || underground || ' ' || enabled || ' ' || comment AS Value " +
                    "FROM map_region")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
