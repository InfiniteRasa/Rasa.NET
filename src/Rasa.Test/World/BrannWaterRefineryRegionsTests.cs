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

    /// <summary>The Brann Water Refinery's rooms (Brann_water_refinery_regions, BrannWaterRefineryRegions).</summary>
    [TestClass]
    [DoNotParallelize]
    public class BrannWaterRefineryRegionsTests
    {
        private const string Migration = "20270604000000_Brann_water_refinery_regions";
        private const string Previous = "20270603000000_Incurables_ward_maintenance_region";

        private const int Sky = 1, Env = 2, Ambient = 4, Music = 8, Name = 16;

        /// <summary>The refinery's regions, as the .map has them: priority and the settings they turn on.</summary>
        private static readonly Dictionary<uint, (int Priority, int Flags)> Refinery = new()
        {
            [32] = (1, 3), [27] = (2, 0), [26] = (3, 0), [28] = (4, 0), [29] = (5, 0), [30] = (6, 0), [31] = (7, 0), [24] = (8, 1), [23] = (9, 1),
            [1] = (10, 3), [2] = (11, 23), [13] = (12, 20), [3] = (13, 20), [4] = (14, 20), [5] = (15, 20), [7] = (16, 20), [9] = (17, 23),
            [10] = (18, 20), [25] = (19, 19), [11] = (20, 22), [15] = (21, 20), [21] = (22, 20), [22] = (23, 20), [16] = (24, 20), [17] = (25, 20),
            [19] = (26, 20), [18] = (27, 20), [20] = (28, 20),
        };

        private static readonly Vector3 CentralOperations = new(-101.5f, 399.6f, -153f), Sediment = new(-38f, 391.4f, -257f),
            Skybridge = new(-154f, 399.5f, -241f), Greenhouse = new(-238f, 399.2f, -241f), Reservoir = new(-82f, 383.6f, -377f);

        [TestMethod]
        public void TheLabelCirclesWereOverTheGroundAndBecomeTheRooms()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();
            var labels = new[] { (CentralOperations, 2u), (Sediment, 5u), (Skybridge, 9u), (Greenhouse, 11u), (Reservoir, 20u) };

            migrator.Migrate(Previous);
            var before = Volumes(database);
            foreach (var (at, region) in labels)
            {
                Assert.AreEqual(region, Shown(before, at, Name, underground: false), $"{region}'s circle was there over the ground");
                Assert.AreEqual(0u, Shown(before, at, Name, underground: true), $"and nowhere in the refinery under it ({region})");
            }

            migrator.Migrate(Migration);
            var after = Volumes(database);
            foreach (var underground in new[] { false, true })
                foreach (var (at, region) in labels)
                    Assert.AreEqual(region, Shown(after, at, Name, underground), $"in the room ({region})");

            foreach (var flag in new[] { Sky, Env, Ambient, Name })
                Assert.AreEqual(2u, Shown(after, CentralOperations, flag, underground: true), $"Central Operations ({flag})");
            Assert.AreEqual(0u, Shown(after, new Vector3(-101.5f, 399.6f, -200f), Name, underground: true), "but not past the operations room's south wall, where the circle reached");
        }

        [TestMethod]
        public void TheWayInTheHallsThePumpRoomsAndTheDorms()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Migration);
            var after = Volumes(database);
            uint At(float x, float y, float z) => Shown(after, new Vector3(x, y, z), Name, underground: true);

            Assert.AreEqual(13u, At(-34f, 423.6f, -85f), "Main Access, at the hospital");
            Assert.AreEqual(13u, At(-34f, 435f, -65f), "under the entrance dome");
            Assert.AreEqual(3u, At(-38f, 408.4f, -137f), "Water Collection 1, the north hall");
            Assert.AreEqual(4u, At(-38f, 408.4f, -209f), "Water Collection 2, the south hall");
            Assert.AreEqual(0u, At(-38f, 408.4f, -173f), "the corridor between the halls");
            Assert.AreEqual(21u, At(-110f, 391.6f, -300f), "Final Processing's grated floor");
            Assert.AreEqual(21u, At(-110f, 377.6f, -300f), "and the machinery hall under it");
            Assert.AreEqual(22u, At(-146f, 383.6f, -360f), "the Concourse");
            Assert.AreEqual(17u, At(-86f, 407.6f, -325f), "PumpStation 1, over Final Processing");
            Assert.AreEqual(21u, At(-86f, 391.6f, -315f), "Final Processing under it");
            Assert.AreEqual(18u, At(-170f, 391.6f, -385f), "PumpStation 2");
            Assert.AreEqual(10u, At(-194f, 407.6f, -173f), "the north dorm");
            Assert.AreEqual(10u, At(-194f, 407.6f, -309f), "the south dorm");
            Assert.AreEqual(25u, At(-330f, 401f, -215f), "Bane Entrypoint");
            Assert.AreEqual(25u, Shown(after, new Vector3(-330f, 401f, -215f), Sky, underground: true), "under the Bane sky");
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
            Assert.AreEqual(BrannWaterRefineryRegions.Volumes.Length + BrannWaterRefineryRegions.Recuts.Length, once.Except(before).Count(), "the new rows and the recut ones");

            foreach (var sql in BrannWaterRefineryRegions.Up())
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

            foreach (var sql in BrannWaterRefineryRegions.Up())
                StringAssert.Contains(migrator.GenerateScript(Previous, Migration), sql);
            foreach (var sql in BrannWaterRefineryRegions.Down())
                StringAssert.Contains(migrator.GenerateScript(Migration, Previous), sql);
        }

        /// <summary>The highest-priority region here that turns the setting on, 0 for none (the map's own).</summary>
        private static uint Shown(List<MapRegion> volumes, Vector3 at, int flag, bool underground) =>
            volumes.Where(v => v.Enabled && Refinery.TryGetValue(v.RegionId, out var r) && (r.Flags & flag) != 0 && v.Contains(at, underground))
                .OrderByDescending(v => Refinery[v.RegionId].Priority).Select(v => v.RegionId).FirstOrDefault();

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == BrannWaterRefineryRegions.MapContextId)
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT map_context_id || ' ' || region_id || ' ' || shape || ' ' || printf('%.1f %.1f %.1f %.1f %.1f %.1f %.1f', pos_x, pos_z, radius, half_x, half_z, min_y, max_y) || ' ' || underground || ' ' || enabled || ' ' || comment AS Value " +
                    "FROM map_region")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
