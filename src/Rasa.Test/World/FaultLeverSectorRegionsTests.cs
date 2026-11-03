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

    /// <summary>The Fault Lever's sectors, generator hall, operations room and transfer station (Fault_lever_sector_regions, FaultLeverSectorRegions).</summary>
    [TestClass]
    [DoNotParallelize]
    public class FaultLeverSectorRegionsTests
    {
        private const string Migration = "20270717000000_Fault_lever_sector_regions";
        private const string Previous = "20270716000000_Burning_steps_interior_regions";

        private const int Ambient = 4, Name = 16;

        /// <summary>The Fault Lever's regions, as the .map has them: priority and the settings they turn on.</summary>
        private static readonly Dictionary<uint, (int Priority, int Flags)> Lever = new()
        {
            [1] = (1, 13), [25] = (2, 0), [26] = (3, 0), [27] = (4, 0), [28] = (5, 0), [29] = (6, 0), [30] = (7, 0), [31] = (8, 0), [32] = (9, 0),
            [33] = (10, 20), [2] = (11, 20), [4] = (12, 20), [3] = (13, 20), [23] = (14, 20), [24] = (15, 20), [8] = (16, 20), [34] = (17, 16),
            [9] = (18, 20), [11] = (19, 20), [21] = (20, 20), [12] = (21, 20), [18] = (22, 20), [14] = (23, 20), [35] = (24, 16), [15] = (25, 20),
            [16] = (26, 20), [20] = (27, 20), [17] = (28, 20), [19] = (29, 20), [22] = (30, 23),
        };

        [TestMethod]
        public void EachWingHasItsSectorsNameAndSound()
        {
            var (before, after) = Migrated();
            var wings = new (Vector3 At, uint Region)[]
            {
                (new Vector3(-341.5f, 340f, 203.9f), 14), (new Vector3(-177.5f, 353.3f, 379.3f), 14),
                (new Vector3(-139.3f, 354f, 331.8f), 14), (new Vector3(-238.9f, 352.9f, 352.5f), 14), (new Vector3(-303.7f, 313.1f, 211.1f), 14),
                (new Vector3(130.6f, 329.3f, 216.6f), 8), (new Vector3(194.5f, 319.2f, 99.1f), 8), (new Vector3(277.9f, 287.8f, 176.7f), 8),
                (new Vector3(-22.9f, 347.9f, 366.5f), 3), (new Vector3(46.5f, 345.8f, 354f), 3), (new Vector3(-95.1f, 347.6f, 425.8f), 3),
                (new Vector3(-93.6f, 353.1f, 510.6f), 3),
            };

            foreach (var (at, region) in wings)
            {
                Assert.AreEqual(0u, Shown(before, at, Name, underground: true), $"{at} had no name");
                Assert.AreEqual(region, Shown(after, at, Name, underground: true), $"{at}: its sector");
                Assert.AreEqual(region, Shown(after, at, Ambient, underground: true), $"{at}: the tunnel sound");
            }
        }

        [TestMethod]
        public void TheRoomsUnderTheSectorsKeepTheirs()
        {
            var (before, after) = Migrated();
            var kept = new (Vector3 At, uint Region, string Where)[]
            {
                (new Vector3(-234.3f, 343.1f, 430.6f), 33, "the west Atta pocket with the logos signs"), (new Vector3(-298.9f, 304.1f, 335.3f), 33, "the lower west pocket"),
                (new Vector3(-252.8f, 327.9f, 169.3f), 33, "the pocket by transmitter A1"), (new Vector3(67.3f, 341.4f, 440.3f), 33, "the north pocket"),
                (new Vector3(-381.1f, 342.1f, 174.4f), 19, "Power Transmitter A2"), (new Vector3(-300f, 319.6f, 160f), 16, "Hammer Monitoring Station A"),
                (new Vector3(340f, 290.6f, 130f), 18, "Power Transmitter B2"), (new Vector3(260f, 295.6f, 60f), 11, "Hammer Monitoring Station B"),
            };

            foreach (var (at, region, where) in kept)
            {
                Assert.AreEqual(region, Shown(before, at, Name, underground: true), $"{where} before");
                Assert.AreEqual(region, Shown(after, at, Name, underground: true), $"{where} after");
            }
        }

        [TestMethod]
        public void TheNorthComplexHasItsGeneratorHallOperationsAndTransferStation()
        {
            var (before, after) = Migrated();
            var hall = new[] { new Vector3(-50f, 347.6f, 499f), new Vector3(-50f, 347.6f, 540f) };

            foreach (var at in hall)
            {
                Assert.AreEqual(0u, Shown(before, at, Name, underground: true), $"{at}: its label circle missed the hall");
                Assert.AreEqual(2u, Shown(after, at, Name, underground: true), $"{at}: Main Generators");
            }

            var surface = new Vector3(-120f, 401.6f, 470f);
            Assert.AreEqual(2u, Shown(before, surface, Name, underground: false), "the ground over it had the name");
            Assert.AreEqual(0u, Shown(after, surface, Name, underground: false), "it has none");

            Assert.AreEqual(0u, Shown(before, new Vector3(-5f, 353.6f, 482f), Name, underground: true), "the command gallery had no name");
            Assert.AreEqual(4u, Shown(after, new Vector3(-5f, 353.6f, 482f), Name, underground: true), "Operations");
            Assert.AreEqual(4u, Shown(after, new Vector3(-5f, 353.6f, 482f), Ambient, underground: true), "with its room sound");

            foreach (var at in new[] { new Vector3(0f, 325.8f, 140f), new Vector3(0f, 326f, 125f), new Vector3(0f, 326f, 170f) })
            {
                Assert.AreEqual(0u, Shown(before, at, Name, underground: true), $"{at} had no name");
                Assert.AreEqual(24u, Shown(after, at, Name, underground: true), $"{at}: the Transfer Station");
            }
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
            Assert.AreEqual(FaultLeverSectorRegions.Volumes.Length + FaultLeverSectorRegions.Recuts.Length, once.Except(before).Count(), "the new rows and the recut ones");

            foreach (var sql in FaultLeverSectorRegions.Up())
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

            foreach (var sql in FaultLeverSectorRegions.Up())
                StringAssert.Contains(migrator.GenerateScript(Previous, Migration), sql);
            foreach (var sql in FaultLeverSectorRegions.Down())
                StringAssert.Contains(migrator.GenerateScript(Migration, Previous), sql);
        }

        /// <summary>The highest-priority region here that turns the setting on, 0 for none (the map's own).</summary>
        private static uint Shown(List<MapRegion> volumes, Vector3 at, int flag, bool underground) =>
            volumes.Where(v => v.Enabled && Lever.TryGetValue(v.RegionId, out var r) && (r.Flags & flag) != 0 && v.Contains(at, underground))
                .OrderByDescending(v => Lever[v.RegionId].Priority).Select(v => v.RegionId).FirstOrDefault();

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == FaultLeverSectorRegions.MapContextId)
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT map_context_id || ' ' || region_id || ' ' || shape || ' ' || printf('%.1f %.1f %.1f %.1f %.1f %.1f %.1f %.1f', pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y) || ' ' || underground || ' ' || enabled || ' ' || comment AS Value " +
                    "FROM map_region WHERE map_context_id = 2103")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
