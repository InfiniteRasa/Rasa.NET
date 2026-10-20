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

    /// <summary>Omega Labs' missile silo and Doctor Sandorf's Quarters (Omega_labs_regions, OmegaLabsRegions).</summary>
    [TestClass]
    [DoNotParallelize]
    public class OmegaLabsRegionsTests
    {
        private const string Migration = "20270201000000_Omega_labs_regions";
        private const string Previous = "20270131000000_Region_room_volumes";

        private static readonly Vector3 Missile = new(0, 185, 60);
        private static readonly Vector3 QuartersUpstairs = new(-40, 197, 46);
        private static readonly Vector3 QuartersDownstairs = new(-45, 182, 40);
        private static readonly Vector3 CavernSouthWest = new(-50, 178, 18);
        private static readonly Vector3 CavernNorthWest = new(-30, 180, 85);
        private static readonly Vector3 ProjectControl = new(0, 188, -38);
        private static readonly Vector3 Pharmacogenetics = new(0, 216, -304);

        [TestMethod]
        public void TheSiloIsTheCavernAndTheQuartersAreTheRoomItLeavesOut()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            var before = Volumes(database);
            Assert.IsFalse(before.Any(v => v.RegionId is OmegaLabsRegions.SiloRegion or OmegaLabsRegions.QuartersRegion), "neither had a volume");

            migrator.Migrate(Migration);
            var after = Volumes(database);

            CollectionAssert.AreEquivalent(new[] { 3u }, In(after, Missile), "at the missile");
            CollectionAssert.AreEquivalent(new[] { 3u }, In(after, CavernSouthWest), "the cavern west of the room, to the south");
            CollectionAssert.AreEquivalent(new[] { 3u }, In(after, CavernNorthWest), "and to the north");
            CollectionAssert.AreEquivalent(new[] { 8u }, In(after, QuartersUpstairs), "the room's upper storey, with the footlocker");
            CollectionAssert.AreEquivalent(new[] { 8u }, In(after, QuartersDownstairs), "its ground floor");
            Assert.AreEqual(0, In(after, ProjectControl).Count, "the Omega Project Control office before the cavern");
            Assert.AreEqual(0, In(after, Pharmacogenetics).Count, "the labs above");
            Assert.AreEqual(0, In(after, Missile with { Y = 300 }).Count, "over the cavern roof");
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
            Assert.AreEqual(OmegaLabsRegions.Volumes.Length, once.Except(before).Count());

            foreach (var sql in OmegaLabsRegions.Up())
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
            var up = migrator.GenerateScript(Previous, Migration);
            var down = migrator.GenerateScript(Migration, Previous);

            foreach (var sql in OmegaLabsRegions.Up())
                StringAssert.Contains(up, sql);
            foreach (var sql in OmegaLabsRegions.Down())
                StringAssert.Contains(down, sql);
        }

        private static List<uint> In(List<MapRegion> volumes, Vector3 at) =>
            volumes.Where(v => v.Enabled && v.RegionId is OmegaLabsRegions.SiloRegion or OmegaLabsRegions.QuartersRegion && v.Contains(at, underground: false))
                .Select(v => v.RegionId).Distinct().ToList();

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == OmegaLabsRegions.MapContextId)
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT map_context_id || ' ' || region_id || ' ' || shape || ' ' || printf('%.1f %.1f %.1f %.1f %.1f', pos_x, pos_z, radius, half_x, half_z) || ' ' || enabled AS Value " +
                    "FROM map_region")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
