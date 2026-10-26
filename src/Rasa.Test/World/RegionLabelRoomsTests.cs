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

    /// <summary>Region label circles kept to the floor of the room their label stands in (Region_label_rooms, RegionLabelRooms).</summary>
    [TestClass]
    [DoNotParallelize]
    public class RegionLabelRoomsTests
    {
        private const string Migration = "20270206000000_Region_label_rooms";
        private const string Previous = "20270205000000_Twin_pillars_npcs";

        private const uint OmegaLabs = 2203, Pharmacogenetics = 6;

        [TestMethod]
        public void OmegaLabsPharmacogeneticsStaysOnItsOwnFloor()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();
            var unit = new Vector3(-0.1f, 216.5f, -304.5f);         // in the room, by its label
            var corridorsOver = new Vector3(0f, 264.5f, -308f);      // the corridor two floors up
            var below = new Vector3(-0.1f, 200f, -304.5f);

            migrator.Migrate(Previous);
            var before = Volumes(database);
            Assert.IsTrue(In(before, OmegaLabs, Pharmacogenetics, unit));
            Assert.IsTrue(In(before, OmegaLabs, Pharmacogenetics, corridorsOver), "before: through the floors");
            Assert.IsTrue(In(before, OmegaLabs, Pharmacogenetics, below), "before: through the floors");

            migrator.Migrate(Migration);
            var after = Volumes(database);
            Assert.IsTrue(In(after, OmegaLabs, Pharmacogenetics, unit));
            Assert.IsFalse(In(after, OmegaLabs, Pharmacogenetics, corridorsOver));
            Assert.IsFalse(In(after, OmegaLabs, Pharmacogenetics, below));
        }

        [TestMethod]
        public void EveryRoomsRegionIsInItsRoomAndNotAFloorAboveOrBelow()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();
            migrator.Migrate(Migration);
            var after = Volumes(database);

            foreach (var r in RegionLabelRooms.Rooms)
            {
                // The circle itself: a region may have a minimap rectangle as well (Timora Command Center).
                var circle = after.Where(v => v.Shape == MapRegionShape.Circle && Math.Abs(v.Position.X - r.X) < 0.01 &&
                    Math.Abs(v.Position.Z - r.Z) < 0.01 && Math.Abs(v.Radius - r.Radius) < 0.01).ToList();
                Assert.IsNotEmpty(circle, r.Place);
                var centre = new Vector3((float)r.X, (float)((r.MinY + r.MaxY) / 2), (float)r.Z);
                Assert.IsTrue(In(circle, r.MapContextId, r.RegionId, centre), $"{r.Place}: in the room");
                Assert.IsFalse(In(circle, r.MapContextId, r.RegionId, centre with { Y = (float)r.MinY - 5 }), $"{r.Place}: below");
                Assert.IsFalse(In(circle, r.MapContextId, r.RegionId, centre with { Y = (float)r.MaxY + 5 }), $"{r.Place}: above");
            }
        }

        [TestMethod]
        public void TheUpRunsTwiceTheDownGivesTheFullHeightBackAndAMovedCircleIsLeftAlone()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            database.Context.Database.ExecuteSqlRaw("UPDATE map_region SET pos_x = pos_x + 5 WHERE map_context_id = 2203 AND region_id = 7;");
            var before = Snapshot(database);

            migrator.Migrate(Migration);
            var once = Snapshot(database);
            Assert.AreEqual(RegionLabelRooms.Rooms.Length - 1, before.Except(once).Count(), "every circle as seeded, not the one a GM moved");
            Assert.IsTrue(once.Any(row => row.StartsWith("2203 7 ") && row.EndsWith(" -2000.0 2000.0")), "the moved one keeps its height");

            foreach (var sql in RegionLabelRooms.Up())
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

            foreach (var sql in RegionLabelRooms.Up())
                StringAssert.Contains(up, sql);
            StringAssert.Contains(up, $"'{Migration}'");
            foreach (var sql in RegionLabelRooms.Down())
                StringAssert.Contains(down, sql);
        }

        private static bool In(List<MapRegion> volumes, uint map, uint region, Vector3 position) =>
            volumes.Any(v => v.MapContextId == map && v.RegionId == region && v.Enabled && v.Contains(position, false));

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT map_context_id || ' ' || region_id || ' ' || shape || ' ' || printf('%.1f %.1f %.1f %.1f %.1f', pos_x, pos_z, radius, min_y, max_y) AS Value " +
                    "FROM map_region")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
