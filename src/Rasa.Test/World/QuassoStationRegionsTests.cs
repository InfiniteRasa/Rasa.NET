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

    /// <summary>Quasso Station's Dropship Zone and Prison (Quasso_station_regions, QuassoStationRegions).</summary>
    [TestClass]
    [DoNotParallelize]
    public class QuassoStationRegionsTests
    {
        private const string Migration = "20270214000000_Quasso_station_regions";
        private const string Previous = "20270213000000_Penumbra_hq_regions";

        /// <summary>The station's regions, as the .map has them: priority.</summary>
        private static readonly Dictionary<uint, int> Priority = new()
        {
            [0] = 0, [13] = 2, [15] = 3, [14] = 4, [3] = 5, [8] = 6, [9] = 7, [11] = 8, [12] = 9, [10] = 10,
        };

        [TestMethod]
        public void TheYardIsTheDropshipZoneAndTheRingIsThePrison()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            var before = Volumes(database);
            Assert.IsFalse(before.Any(v => v.RegionId is QuassoStationRegions.DropshipZoneRegion or QuassoStationRegions.PrisonRegion), "neither had a volume");
            Assert.AreEqual(11u, Top(before, new Vector3(-423f, -48f, 188f)), "the prison ring was the Subterranean Command's, before");

            migrator.Migrate(Migration);
            var after = Volumes(database);

            Assert.AreEqual(3u, Top(after, new Vector3(8f, 11f, 13f)), "on the middle landing pad");
            Assert.AreEqual(3u, Top(after, new Vector3(63f, 11f, 57f)), "on the east pad");
            Assert.AreEqual(3u, Top(after, new Vector3(-37f, 11f, 50f)), "on the west pad");
            Assert.AreEqual(14u, Top(after, new Vector3(-1f, 15f, -60f)), "the Access Floor south of the yard is still its own");
            Assert.AreEqual(8u, Top(after, new Vector3(2f, 20f, 316f)), "and the Supply Warehouse");

            Assert.AreEqual(10u, Top(after, new Vector3(-479f, -48f, 190f)), "the west prison corridor");
            Assert.AreEqual(10u, Top(after, new Vector3(-367f, -48f, 220f)), "the east prison corridor");
            Assert.AreEqual(10u, Top(after, new Vector3(-423f, -48f, 188f)), "the information extraction room, the Conscription Chamber");
            Assert.AreNotEqual(10u, Top(after, new Vector3(-523f, -17f, 218f)), "Bane Stasis Room 02 over it");
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
            Assert.AreEqual(QuassoStationRegions.Volumes.Length, once.Except(before).Count());

            foreach (var sql in QuassoStationRegions.Up())
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

            foreach (var sql in QuassoStationRegions.Up())
                StringAssert.Contains(migrator.GenerateScript(Previous, Migration), sql);
            foreach (var sql in QuassoStationRegions.Down())
                StringAssert.Contains(migrator.GenerateScript(Migration, Previous), sql);
        }

        /// <summary>The highest-priority region the player is sent here, 0 for none.</summary>
        private static uint Top(List<MapRegion> volumes, Vector3 at) =>
            volumes.Where(v => v.Enabled && Priority.ContainsKey(v.RegionId) && v.Contains(at, underground: true))
                .Select(v => v.RegionId).Append(0u).OrderByDescending(id => Priority[id]).First();

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == QuassoStationRegions.MapContextId)
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT map_context_id || ' ' || region_id || ' ' || shape || ' ' || printf('%.1f %.1f %.1f %.1f %.1f', pos_x, pos_z, radius, half_x, half_z) || ' ' || enabled AS Value " +
                    "FROM map_region")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
