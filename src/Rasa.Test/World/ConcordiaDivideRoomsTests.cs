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

    /// <summary>Foreas Base's other buildings and the Bane Forward Command's rooms (Concordia_divide_rooms, ConcordiaDivideRooms).</summary>
    [TestClass]
    [DoNotParallelize]
    public class ConcordiaDivideRoomsTests
    {
        private const string Migration = "20270713000000_Concordia_divide_rooms";
        private const string Previous = "20270712000000_Concordia_divide_regions";

        private const int Sky = 1, Env = 2, Ambient = 4, Music = 8, Name = 16;

        /// <summary>Concordia Divide's regions, as the .map has them: priority and the settings they turn on.</summary>
        private static readonly Dictionary<uint, (int Priority, int Flags)> Divide = new()
        {
            [22] = (1, 13), [40] = (2, 1), [23] = (3, 28), [10] = (4, 28), [20] = (5, 28), [21] = (6, 28), [30] = (7, 28), [15] = (8, 4),
            [16] = (9, 7), [17] = (10, 4), [18] = (11, 4), [19] = (12, 4), [14] = (13, 3), [13] = (14, 15), [11] = (15, 15), [32] = (16, 7),
            [12] = (17, 31), [33] = (18, 7), [5] = (19, 12), [6] = (20, 12), [4] = (21, 12), [1] = (22, 28), [2] = (23, 4), [27] = (24, 28),
            [7] = (25, 7), [3] = (26, 15), [25] = (27, 28), [28] = (28, 12), [29] = (29, 15), [8] = (30, 15), [9] = (31, 15), [36] = (32, 11),
            [31] = (33, 7), [34] = (34, 15), [37] = (35, 4), [35] = (36, 3), [24] = (37, 28),
        };

        [TestMethod]
        public void TheOtherBuildingsTakeTheRemainingInteriorsBySize()
        {
            var (before, after) = Migrated();
            var garage = new Vector3(-115.6f, 116.4f, 460.8f);
            var prison = new Vector3(42.9f, 125.3f, 451.2f);
            var barracks = new Vector3(-130.9f, 125.2f, 525.9f);

            Assert.AreEqual(0u, Shown(before, garage, Sky, underground: false), "the garage had the open sky");
            Assert.AreEqual(11u, Shown(after, garage, Sky, underground: false), "the largest: the heavy room tone interior");
            Assert.AreEqual(11u, Shown(after, garage, Ambient, underground: false), "with its sound");
            Assert.AreEqual(32u, Shown(after, prison, Sky, underground: false), "the next: the medium room tone interior");
            Assert.AreEqual(32u, Shown(after, prison, Ambient, underground: false), "with its sound");
            Assert.AreEqual(16u, Shown(after, barracks, Sky, underground: false), "the barracks are lit indoors");
            Assert.AreEqual(19u, Shown(after, barracks, Ambient, underground: false), "with the guild hall sound");
            Assert.AreEqual(19u, Shown(after, new Vector3(-128.2f, 125f, 557.8f), Ambient, underground: false), "both of them");
            Assert.AreEqual(0u, Shown(after, new Vector3(-128.3f, 132.9f, 526f), Sky, underground: false), "not their roofs");
        }

        [TestMethod]
        public void TheBaneForwardCommandsRectangleIsItsRooms()
        {
            var (before, after) = Migrated();
            var room = new Vector3(-23.9f, 72.1f, -551.6f);
            var ground = new Vector3(-27.4f, 111.3f, -589.9f);

            Assert.AreEqual(29u, Shown(before, ground, Sky, underground: false), "the Bane interior sky was over the camp");
            Assert.AreEqual(0u, Shown(after, ground, Sky, underground: false), "which has the open sky again");
            Assert.AreEqual(29u, Shown(after, room, Sky, underground: true), "the rooms under it keep the Bane interior");
            Assert.AreEqual(29u, Shown(after, room, Music, underground: true), "and its music");
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
            Assert.AreEqual(ConcordiaDivideRooms.Volumes.Length + 1, once.Except(before).Count(), "the new rows and the rectangle");

            foreach (var sql in ConcordiaDivideRooms.Up())
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

            foreach (var sql in ConcordiaDivideRooms.Up())
                StringAssert.Contains(migrator.GenerateScript(Previous, Migration), sql);
            foreach (var sql in ConcordiaDivideRooms.Down())
                StringAssert.Contains(migrator.GenerateScript(Migration, Previous), sql);
        }

        /// <summary>The highest-priority region here that turns the setting on, 0 for none (the map's own).</summary>
        private static uint Shown(List<MapRegion> volumes, Vector3 at, int flag, bool underground) =>
            volumes.Where(v => v.Enabled && Divide.TryGetValue(v.RegionId, out var r) && (r.Flags & flag) != 0 && v.Contains(at, underground))
                .OrderByDescending(v => Divide[v.RegionId].Priority).Select(v => v.RegionId).FirstOrDefault();

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == ConcordiaDivideRooms.MapContextId)
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT map_context_id || ' ' || region_id || ' ' || shape || ' ' || printf('%.1f %.1f %.1f %.1f %.1f %.1f %.1f %.1f', pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y) || ' ' || underground || ' ' || enabled || ' ' || comment AS Value " +
                    "FROM map_region WHERE map_context_id = 1148")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
