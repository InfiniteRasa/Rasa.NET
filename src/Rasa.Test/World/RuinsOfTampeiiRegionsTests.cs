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

    /// <summary>The Ruins of Tampeii's gateway, ruins and lair (Ruins_of_tampeii_regions, RuinsOfTampeiiRegions).</summary>
    [TestClass]
    [DoNotParallelize]
    public class RuinsOfTampeiiRegionsTests
    {
        private const string Migration = "20270618000000_Ruins_of_tampeii_regions";
        private const string Previous = "20270617000000_Fault_lever_regions";

        private const int Sky = 1, Music = 8, Name = 16;

        /// <summary>The Ruins of Tampeii's regions, as the .map has them: priority and the settings they turn on.</summary>
        private static readonly Dictionary<uint, (int Priority, int Flags)> Tampeii = new()
        {
            [1] = (1, 1), [3] = (2, 31), [2] = (3, 23), [4] = (4, 31),
        };

        private static readonly Vector3 Arrival = new(140.5f, 56.1f, 143.3f);
        private static readonly Vector3 Cavern = new(-60f, 2.1f, -20f);
        private static readonly Vector3 ComputerRoom = new(-64f, 2.1f, 330f);

        [TestMethod]
        public void TheComplexIsTheGateway()
        {
            var after = Migrated(out var before);
            Assert.AreEqual(0u, Shown(before, Arrival, Sky, underground: false), "the map had no volumes");

            Assert.AreEqual(2u, Shown(after, Arrival, Sky, underground: false), "at the arrival");
            Assert.AreEqual(2u, Shown(after, new Vector3(188.5f, 40.7f, -23.5f), Name, underground: false), "Security Area 2");
            Assert.AreEqual(2u, Shown(after, new Vector3(60f, 33.1f, -24f), Sky, underground: false), "the west corridor");
            Assert.AreEqual(2u, Shown(after, new Vector3(80f, 48.8f, 40f), Sky, underground: false), "the north rooms");
            Assert.AreEqual(2u, Shown(after, new Vector3(110f, 48.7f, -110f), Sky, underground: false), "the south rooms");
            Assert.AreEqual(2u, Shown(after, new Vector3(188f, 39.7f, -84f), Sky, underground: false), "the greenhouse");
            Assert.AreEqual(2u, Shown(after, new Vector3(204f, 48.7f, 140f), Sky, underground: false), "the east dormitory");
            Assert.AreEqual(0u, Shown(after, new Vector3(60f, 33.1f, -24f), Music, underground: false), "with no music of its own");
        }

        [TestMethod]
        public void TheLavaCavernIsTheRuins()
        {
            var after = Migrated(out _);

            Assert.AreEqual(4u, Shown(after, Cavern, Sky, underground: false), "in the lava fields");
            Assert.AreEqual(4u, Shown(after, Cavern, Music, underground: false), "with their music");
            Assert.AreEqual(4u, Shown(after, new Vector3(-70f, 0.5f, 140f), Sky, underground: false), "before the security door");
            Assert.AreEqual(4u, Shown(after, new Vector3(-180f, 2.3f, 185f), Sky, underground: true), "in the north-west lobe");
        }

        [TestMethod]
        public void BehindTheSecurityDoorIsPatchsLair()
        {
            var after = Migrated(out _);

            Assert.AreEqual(3u, Shown(after, new Vector3(-64f, 1.6f, 220f), Sky, underground: true), "the wardenbot corridor");
            Assert.AreEqual(3u, Shown(after, new Vector3(-110f, 1.5f, 242f), Sky, underground: true), "its west stub");
            Assert.AreEqual(3u, Shown(after, ComputerRoom, Sky, underground: false), "Patch's computer room");
            Assert.AreEqual(3u, Shown(after, ComputerRoom, Music, underground: false), "with its music");
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
            Assert.AreEqual(RuinsOfTampeiiRegions.Volumes.Length, once.Except(before).Count(), "the new rows");

            foreach (var sql in RuinsOfTampeiiRegions.Up())
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

            foreach (var sql in RuinsOfTampeiiRegions.Up())
                StringAssert.Contains(migrator.GenerateScript(Previous, Migration), sql);
            foreach (var sql in RuinsOfTampeiiRegions.Down())
                StringAssert.Contains(migrator.GenerateScript(Migration, Previous), sql);
        }

        private static List<MapRegion> Migrated(out List<MapRegion> before)
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();
            migrator.Migrate(Previous);
            before = Volumes(database);
            migrator.Migrate(Migration);
            return Volumes(database);
        }

        /// <summary>The highest-priority region here that turns the setting on, 0 for none (the map's own).</summary>
        private static uint Shown(List<MapRegion> volumes, Vector3 at, int flag, bool underground) =>
            volumes.Where(v => v.Enabled && Tampeii.TryGetValue(v.RegionId, out var r) && (r.Flags & flag) != 0 && v.Contains(at, underground))
                .OrderByDescending(v => Tampeii[v.RegionId].Priority).Select(v => v.RegionId).FirstOrDefault();

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == RuinsOfTampeiiRegions.MapContextId)
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT map_context_id || ' ' || region_id || ' ' || shape || ' ' || printf('%.1f %.1f %.1f %.1f %.1f %.1f %.1f %.1f', pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y) || ' ' || underground || ' ' || enabled || ' ' || comment AS Value " +
                    "FROM map_region")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
