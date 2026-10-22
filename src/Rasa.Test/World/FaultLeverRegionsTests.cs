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

    /// <summary>The Fault Lever's monitoring stations, power transmitters, security yards and Atta caves (Fault_lever_regions, FaultLeverRegions).</summary>
    [TestClass]
    [DoNotParallelize]
    public class FaultLeverRegionsTests
    {
        private const string Migration = "20270617000000_Fault_lever_regions";
        private const string Previous = "20270616000000_Avernus_entrance_region";

        private const int Sky = 1, Env = 2, Ambient = 4, Music = 8, Name = 16;

        /// <summary>The Fault Lever's regions, as the .map has them: priority and the settings they turn on.</summary>
        private static readonly Dictionary<uint, (int Priority, int Flags)> Lever = new()
        {
            [1] = (1, 13), [25] = (2, 0), [26] = (3, 0), [27] = (4, 0), [28] = (5, 0), [29] = (6, 0), [30] = (7, 0), [31] = (8, 0), [32] = (9, 0),
            [33] = (10, 20), [2] = (11, 20), [4] = (12, 20), [3] = (13, 20), [23] = (14, 20), [24] = (15, 20), [8] = (16, 20), [34] = (17, 16),
            [9] = (18, 20), [11] = (19, 20), [21] = (20, 20), [12] = (21, 20), [18] = (22, 20), [14] = (23, 20), [35] = (24, 16), [15] = (25, 20),
            [16] = (26, 20), [20] = (27, 20), [17] = (28, 20), [19] = (29, 20), [22] = (30, 23),
        };

        [TestMethod]
        public void TheMonitoringStationsAreTheirRooms()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();
            var stationA = new Vector3(-294f, 319.6f, 150f);
            var stationB = new Vector3(260f, 295.6f, 60f);

            migrator.Migrate(Previous);
            var before = Volumes(database);
            Assert.AreEqual(0u, Shown(before, stationA, Name, underground: true), "Station A's circle missed its underground room");
            Assert.AreEqual(0u, Shown(before, stationB, Name, underground: true), "and Station B's");

            migrator.Migrate(Migration);
            var after = Volumes(database);
            Assert.AreEqual(16u, Shown(after, stationA, Name, underground: true), "Hammer Monitoring Station A");
            Assert.AreEqual(16u, Shown(after, new Vector3(-294f, 319.6f, 140f), Ambient, underground: true), "at its window");
            Assert.AreEqual(11u, Shown(after, stationB, Name, underground: true), "Hammer Monitoring Station B");
            Assert.AreEqual(0u, Shown(after, stationB, Sky, underground: true), "its gallery under the map's sky");
            Assert.AreEqual(22u, Shown(after, new Vector3(260f, 295.6f, 32f), Sky, underground: false), "its window under the Thunderhead sky");
        }

        [TestMethod]
        public void TransmittersSecurityAndCaves()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Migration);
            var after = Volumes(database);
            uint At(float x, float y, float z, bool underground) => Shown(after, new Vector3(x, y, z), Name, underground);

            Assert.AreEqual(17u, At(-214f, 314.6f, 220f, true), "Power Transmitter A1");
            Assert.AreEqual(19u, At(-374f, 346.6f, 236f, true), "Power Transmitter A2");
            Assert.AreEqual(12u, At(180f, 290.6f, 128f, true), "Power Transmitter B1");
            Assert.AreEqual(18u, At(340f, 290.6f, 128f, true), "Power Transmitter B2");
            Assert.AreEqual(35u, At(-289f, 337.2f, 70f, false), "West Sector Security, at its gate");
            Assert.AreEqual(35u, At(-295f, 337.2f, 95f, false), "inside its yard");
            Assert.AreEqual(34u, At(263f, 312.6f, -40f, false), "East Sector Security, at its gate");
            Assert.AreEqual(34u, At(258f, 314.3f, -20f, false), "inside its yard");
            Assert.AreEqual(33u, At(-212f, 366.7f, 440f, true), "Hidden Cave, by the logos signs");
            Assert.AreEqual(33u, At(-290f, 303.2f, 335f, true), "its lower pocket");
            Assert.AreEqual(33u, At(-246f, 335.3f, 190f, true), "the pocket by the transmitter");
            Assert.AreEqual(33u, At(55f, 341.3f, 440f, true), "and the north one");
            Assert.AreEqual(33u, Shown(after, new Vector3(55f, 341.3f, 440f), Ambient, underground: true), "with the Atta cave's sound");
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
            Assert.AreEqual(FaultLeverRegions.Volumes.Length + FaultLeverRegions.Recuts.Length, once.Except(before).Count(), "the new rows and the recut ones");

            foreach (var sql in FaultLeverRegions.Up())
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

            foreach (var sql in FaultLeverRegions.Up())
                StringAssert.Contains(migrator.GenerateScript(Previous, Migration), sql);
            foreach (var sql in FaultLeverRegions.Down())
                StringAssert.Contains(migrator.GenerateScript(Migration, Previous), sql);
        }

        /// <summary>The highest-priority region here that turns the setting on, 0 for none (the map's own).</summary>
        private static uint Shown(List<MapRegion> volumes, Vector3 at, int flag, bool underground) =>
            volumes.Where(v => v.Enabled && Lever.TryGetValue(v.RegionId, out var r) && (r.Flags & flag) != 0 && v.Contains(at, underground))
                .OrderByDescending(v => Lever[v.RegionId].Priority).Select(v => v.RegionId).FirstOrDefault();

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == FaultLeverRegions.MapContextId)
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT map_context_id || ' ' || region_id || ' ' || shape || ' ' || printf('%.1f %.1f %.1f %.1f %.1f %.1f %.1f %.1f', pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y) || ' ' || underground || ' ' || enabled || ' ' || comment AS Value " +
                    "FROM map_region")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
