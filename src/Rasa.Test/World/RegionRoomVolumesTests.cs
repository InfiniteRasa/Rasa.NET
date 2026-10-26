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

    /// <summary>Interior regions given the room their map label stands in (Region_room_volumes, RegionRoomVolumes).</summary>
    [TestClass]
    [DoNotParallelize]
    public class RegionRoomVolumesTests
    {
        private const string Migration = "20270131000000_Region_room_volumes";
        private const string Previous = "20270129000000_Wilderness_respawn_in_tenths";

        [TestMethod]
        public void EachRegionHadNoVolumeAndNowHasItsRoomFromFloorToCeiling()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            var before = Volumes(database);

            foreach (var r in RegionRoomVolumes.Rooms)
                Assert.IsFalse(before.Any(v => v.MapContextId == r.MapContextId && v.RegionId == r.RegionId), $"{r.Comment}: had a volume already");

            migrator.Migrate(Migration);
            var after = Volumes(database);

            Assert.AreEqual(17, RegionRoomVolumes.Rooms.Length);
            Assert.AreEqual(before.Count + RegionRoomVolumes.Rooms.Length, after.Count);

            foreach (var r in RegionRoomVolumes.Rooms)
            {
                var mid = (float)((r.MinY + r.MaxY) / 2);
                var room = after.Single(v => v.MapContextId == r.MapContextId && v.RegionId == r.RegionId);

                Assert.IsTrue(room.Enabled, r.Comment);
                Assert.IsTrue(room.Contains(new Vector3((float)r.X, mid, (float)r.Z), underground: false), $"{r.Comment}: in the room");
                Assert.IsTrue(room.Contains(new Vector3((float)r.X, mid, (float)r.Z), underground: true), $"{r.Comment}: under the ground too");
                Assert.IsTrue(room.Contains(new Vector3((float)r.X, (float)r.Y, (float)r.Z), underground: false), $"{r.Comment}: at its label");
                Assert.IsFalse(room.Contains(new Vector3((float)r.X, (float)r.MaxY + 5, (float)r.Z), underground: false), $"{r.Comment}: over the roof");
                Assert.IsFalse(room.Contains(new Vector3((float)(r.X + r.HalfX + 1), mid, (float)r.Z), underground: false), $"{r.Comment}: beside the room");
            }
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
            Assert.AreEqual(RegionRoomVolumes.Rooms.Length, once.Except(before).Count());

            foreach (var sql in RegionRoomVolumes.Up())
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

            foreach (var sql in RegionRoomVolumes.Up())
                StringAssert.Contains(up, sql);
            StringAssert.Contains(up, $"'{Migration}'");
            foreach (var sql in RegionRoomVolumes.Down())
                StringAssert.Contains(down, sql);
        }

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT map_context_id || ' ' || region_id || ' ' || shape || ' ' || printf('%.1f %.1f %.1f %.1f %.1f', pos_x, pos_z, radius, half_x, half_z) || ' ' || enabled AS Value " +
                    "FROM map_region")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
