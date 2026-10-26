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

    /// <summary>Interior minimap rectangles kept below the bases on top of them (Region_floor_ceilings, RegionFloorCeilings).</summary>
    [TestClass]
    [DoNotParallelize]
    public class RegionFloorCeilingsTests
    {
        private const string Migration = "20270204000000_Region_floor_ceilings";
        private const string Previous = "20270203000000_Lightbender_charged_shot";

        private static readonly (string Place, uint Map, uint Region, Vector3 Where, bool Before, bool After)[] Places =
        {
            // Where the sky went dark: Fort Virgil's landing pad, over the cargo bay.
            ("Fort Virgil, by the landing pad", 2047, 23, new(-328.4375f, 737.1797f, -637.3867f), true, false),
            ("Fort Virgil's cargo bay", 2047, 23, new(-350f, 710f, -643f), true, true),
            ("Foreas Base, on the ground", 1148, 34, new(-42.3f, 116.3f, 478.7f), true, false),
            ("Foreas Base's lower level", 1148, 34, new(-63f, 48.5f, 485f), true, true),
            ("Foreas Base, east, on the ground", 1148, 35, new(112f, 124.5f, 444f), true, false),
            ("Foreas Base's east lower level", 1148, 35, new(112f, 56f, 444f), true, true),
        };

        [TestMethod]
        public void TheInteriorsSkyStaysInTheRoomsBelowTheBase()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            var before = Volumes(database);
            foreach (var p in Places)
                Assert.AreEqual(p.Before, In(before, p.Map, p.Region, p.Where), $"{p.Place}, before");

            migrator.Migrate(Migration);
            var after = Volumes(database);
            foreach (var p in Places)
                Assert.AreEqual(p.After, In(after, p.Map, p.Region, p.Where), $"{p.Place}, after");
        }

        [TestMethod]
        public void TheUpRunsTwiceTheDownGivesTheFullHeightBackAndAMovedRectangleIsLeftAlone()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            database.Context.Database.ExecuteSqlRaw("UPDATE map_region SET pos_x = pos_x + 5 WHERE map_context_id = 1148 AND region_id = 35;");
            var before = Snapshot(database);

            migrator.Migrate(Migration);
            var once = Snapshot(database);
            Assert.AreEqual(RegionFloorCeilings.Bounds.Length - 1, before.Except(once).Count(), "every rectangle as seeded, not the one a GM moved");
            Assert.IsTrue(once.Any(row => row.StartsWith("1148 35 ") && row.EndsWith(" -2000.0 2000.0")), "the moved one keeps its height");

            foreach (var sql in RegionFloorCeilings.Up())
                database.Context.Database.ExecuteSqlRaw(sql.Replace("{", "{{").Replace("}", "}}"));
            CollectionAssert.AreEqual(once, Snapshot(database), "twice");

            migrator.Migrate(Previous);
            CollectionAssert.AreEqual(before, Snapshot(database), "Down");
        }

        [TestMethod]
        public void OnMySqlTheRowsChangeAndChangeBack()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(Previous, Migration);
            var down = migrator.GenerateScript(Migration, Previous);

            foreach (var sql in RegionFloorCeilings.Up())
                StringAssert.Contains(up, sql);
            StringAssert.Contains(up, $"'{Migration}'");
            foreach (var sql in RegionFloorCeilings.Down())
                StringAssert.Contains(down, sql);
        }

        private static bool In(List<MapRegion> volumes, uint map, uint region, Vector3 position) =>
            volumes.Any(v => v.MapContextId == map && v.RegionId == region && v.Enabled && v.Contains(position, false));

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == 2047 || entry.MapContextId == 1148)
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT map_context_id || ' ' || region_id || ' ' || shape || ' ' || printf('%.1f %.1f %.1f %.1f', pos_x, pos_z, min_y, max_y) AS Value " +
                    "FROM map_region")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
