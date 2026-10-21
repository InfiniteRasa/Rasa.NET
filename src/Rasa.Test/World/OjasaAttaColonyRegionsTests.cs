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

    /// <summary>The Ojasa Atta Colony and the Queen Chamber (Ojasa_atta_colony_regions, OjasaAttaColonyRegions).</summary>
    [TestClass]
    [DoNotParallelize]
    public class OjasaAttaColonyRegionsTests
    {
        private const string Migration = "20270215000000_Ojasa_atta_colony_regions";
        private const string Previous = "20270214000000_Quasso_station_regions";

        private const int Sky = 1, Ambient = 4, Name = 16;

        /// <summary>Incline's regions, as the .map has them: priority and the settings they turn on.</summary>
        private static readonly Dictionary<uint, (int Priority, int Flags)> Incline = new()
        {
            [1] = (1, 13), [4] = (2, 13), [5] = (3, 17), [6] = (4, 7), [7] = (5, 7), [8] = (6, 7), [3] = (7, 29), [9] = (8, 20),
            [2] = (9, 11), [13] = (10, 24), [11] = (11, 16), [15] = (12, 17), [16] = (13, 24), [17] = (14, 16), [18] = (15, 16),
            [19] = (16, 16), [20] = (17, 16), [21] = (18, 24), [22] = (19, 3), [26] = (20, 15), [24] = (21, 3), [23] = (22, 16),
            [12] = (23, 15), [27] = (24, 1), [25] = (25, 16), [14] = (26, 16), [28] = (27, 0),
        };

        /// <summary>The hive's.</summary>
        private static readonly Dictionary<uint, (int Priority, int Flags)> Hive = new()
        {
            [5] = (1, 31), [3] = (2, 0), [4] = (3, 0), [2] = (4, 1), [1] = (5, 13),
        };

        [TestMethod]
        public void TheHiveMoundIsTheColony()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();
            var door = new Vector3(-549f, 305f, 340f);

            migrator.Migrate(Previous);
            var before = Volumes(database, OjasaAttaColonyRegions.InclineMapContextId);
            Assert.IsFalse(before.Any(v => v.RegionId == OjasaAttaColonyRegions.ColonyRegion), "it had no volume");
            Assert.AreEqual(0u, Shown(before, Incline, door, Name, underground: false), "the hive door was unnamed");

            migrator.Migrate(Migration);
            var after = Volumes(database, OjasaAttaColonyRegions.InclineMapContextId);

            Assert.AreEqual(9u, Shown(after, Incline, door, Name, underground: false), "at the hive door");
            Assert.AreEqual(9u, Shown(after, Incline, door, Ambient, underground: false));
            Assert.AreEqual(9u, Shown(after, Incline, door, Name, underground: true), "under the mound as well");
            Assert.AreEqual(9u, Shown(after, Incline, new Vector3(-582f, 340f, 387f), Name, underground: false), "at the north tower");
            Assert.AreEqual(9u, Shown(after, Incline, new Vector3(-466f, 305f, 383f), Name, underground: false), "at the north-east tower");

            var coupler = new Vector3(-549f, 320f, 260f);
            Assert.AreEqual(17u, Shown(after, Incline, coupler, Name, underground: false), "the Ojasa Highlands still names the south face");
            Assert.AreEqual(9u, Shown(after, Incline, coupler, Ambient, underground: false), "under the colony's ambient");

            Assert.AreNotEqual(9u, Shown(after, Incline, new Vector3(-549f, 300f, 200f), Ambient, underground: false), "south of the towers");
            Assert.AreNotEqual(9u, Shown(after, Incline, new Vector3(-549f, 250f, 340f), Ambient, underground: true), "below the mound");
        }

        [TestMethod]
        public void TheThroneDomeIsTheQueenChamber()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();
            var throne = new Vector3(182f, 133f, -291f);

            migrator.Migrate(Previous);
            var before = Volumes(database, OjasaAttaColonyRegions.HiveMapContextId);
            Assert.IsFalse(before.Any(v => v.RegionId == OjasaAttaColonyRegions.QueenChamberRegion), "it had no volume");
            Assert.IsTrue(before.Any(v => v.RegionId == 3 && v.Contains(throne, underground: true)), "the whole-hive box is over it");

            migrator.Migrate(Migration);
            var after = Volumes(database, OjasaAttaColonyRegions.HiveMapContextId);

            foreach (var flag in new[] { Sky, Ambient, Name })
                Assert.AreEqual(5u, Shown(after, Hive, throne, flag, underground: true), $"at Kardash's throne ({flag}): the whole-hive box sets nothing");

            Assert.AreEqual(5u, Shown(after, Hive, new Vector3(199f, 140f, -348f), Name, underground: true), "on the ledge south of the throne");
            Assert.AreEqual(5u, Shown(after, Hive, new Vector3(282f, 146f, -417f), Name, underground: true), "in the south-east");
            Assert.AreEqual(5u, Shown(after, Hive, new Vector3(214f, 110f, -500f), Name, underground: true), "in the south lobe");
            Assert.AreEqual(5u, Shown(after, Hive, new Vector3(248f, 112f, -112f), Name, underground: true), "at the north door");
            Assert.AreEqual(5u, Shown(after, Hive, new Vector3(80f, 115f, -265f), Name, underground: true), "at the top of the west ramp");

            Assert.AreEqual(0u, Shown(after, Hive, new Vector3(16f, 100f, 12f), Name, underground: true), "the middle cavern");
            Assert.AreEqual(0u, Shown(after, Hive, new Vector3(130f, 110f, -110f), Name, underground: true), "where the middle cavern's outline overlaps");
            Assert.AreEqual(0u, Shown(after, Hive, new Vector3(439f, 73f, 126f), Name, underground: true), "the hive entrance");
            Assert.AreEqual(0u, Shown(after, Hive, new Vector3(30f, 75f, -170f), Name, underground: true), "the tunnels below the west ramp");
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
            Assert.AreEqual(OjasaAttaColonyRegions.Volumes.Length, once.Except(before).Count());

            foreach (var sql in OjasaAttaColonyRegions.Up())
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

            foreach (var sql in OjasaAttaColonyRegions.Up())
                StringAssert.Contains(migrator.GenerateScript(Previous, Migration), sql);
            foreach (var sql in OjasaAttaColonyRegions.Down())
                StringAssert.Contains(migrator.GenerateScript(Migration, Previous), sql);
        }

        /// <summary>The highest-priority region here that turns the setting on, 0 for none (the map's own).</summary>
        private static uint Shown(List<MapRegion> volumes, Dictionary<uint, (int Priority, int Flags)> regions, Vector3 at, int flag, bool underground) =>
            volumes.Where(v => v.Enabled && regions.TryGetValue(v.RegionId, out var r) && (r.Flags & flag) != 0 && v.Contains(at, underground))
                .OrderByDescending(v => regions[v.RegionId].Priority).Select(v => v.RegionId).FirstOrDefault();

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database, uint mapContextId) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == mapContextId)
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT map_context_id || ' ' || region_id || ' ' || shape || ' ' || printf('%.1f %.1f %.1f %.1f %.1f', pos_x, pos_z, radius, half_x, half_z) || ' ' || enabled AS Value " +
                    "FROM map_region")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
