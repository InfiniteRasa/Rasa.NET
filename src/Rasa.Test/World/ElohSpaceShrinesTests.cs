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

    /// <summary>Eloh Space's boxes over the whole of its two shrines (Eloh_space_shrines, ElohSpaceShrines).</summary>
    [TestClass]
    [DoNotParallelize]
    public class ElohSpaceShrinesTests
    {
        private const string Migration = "20270630000000_Eloh_space_shrines";
        private const string Previous = "20270629000000_Predator_bay_region";

        private const int Sky = 1, Music = 8, Name = 16;

        /// <summary>Torden Abyss' regions, as the .map has them: priority and the settings they turn on.</summary>
        private static readonly Dictionary<uint, (int Priority, int Flags)> Abyss = new()
        {
            [1] = (1, 13), [5] = (2, 31), [13] = (3, 29), [14] = (4, 29), [16] = (5, 29), [6] = (6, 31), [44] = (7, 31), [33] = (8, 31), [34] = (9, 31),
            [28] = (10, 20), [29] = (11, 20), [18] = (12, 31), [22] = (13, 31), [23] = (14, 31), [24] = (15, 31), [25] = (16, 31), [26] = (17, 31),
            [40] = (18, 31), [41] = (19, 31), [42] = (20, 31), [45] = (21, 29), [35] = (22, 25), [37] = (23, 17), [36] = (24, 17), [43] = (25, 31),
            [20] = (26, 29), [21] = (27, 7), [17] = (28, 31), [31] = (29, 24), [32] = (30, 24), [38] = (31, 0), [39] = (32, 19), [15] = (33, 31),
        };

        [TestMethod]
        public void TheSouthShrineIsElohSpaceToItsWalls()
        {
            var (before, after) = Migrated();
            var southEnd = new Vector3(-320f, 183f, -470f);
            var westSide = new Vector3(-352f, 183.2f, -425f);

            Assert.AreNotEqual(15u, Shown(before, southEnd, Sky, underground: true), "the room's south end was left out");
            Assert.AreNotEqual(15u, Shown(before, westSide, Sky, underground: true), "and its west side");
            Assert.AreEqual(15u, Shown(after, southEnd, Sky, underground: true), "the south end has the Eloh sky");
            Assert.AreEqual(15u, Shown(after, southEnd, Music, underground: true), "and music");
            Assert.AreEqual(15u, Shown(after, westSide, Name, underground: true), "the west side");
            Assert.AreEqual(15u, Shown(after, new Vector3(-300f, 182.9f, -440f), Name, underground: true), "by the obelisks, as before");
            Assert.AreEqual(15u, Shown(after, new Vector3(-315f, 199.3f, -345f), Name, underground: true), "the corridor's end");
        }

        [TestMethod]
        public void TheNorthShrineIsElohSpaceFromItsRoomToTheCave()
        {
            var (before, after) = Migrated();
            var westEnd = new Vector3(-530f, 500.1f, 225f);
            var coupler = new Vector3(-400f, 516f, 225f);

            Assert.AreNotEqual(15u, Shown(before, westEnd, Sky, underground: true), "the room's west end was left out");
            Assert.AreNotEqual(15u, Shown(before, coupler, Sky, underground: true), "and the coupler");
            Assert.AreEqual(15u, Shown(after, westEnd, Name, underground: true), "the west end");
            Assert.AreEqual(15u, Shown(after, coupler, Name, underground: true), "the coupler");
            Assert.AreNotEqual(15u, Shown(after, new Vector3(-380f, 516f, 225f), Name, underground: true), "but not the rocky cave past it");
            Assert.AreNotEqual(15u, Shown(after, new Vector3(-516f, 546.7f, 200f), Name, underground: true), "nor the cave over the room");
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
        public void TheUpRunsTwiceAndTheDownPutsThemBack()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            var before = Snapshot(database);

            migrator.Migrate(Migration);
            var once = Snapshot(database);
            Assert.AreEqual(ElohSpaceShrines.Resizes.Length, once.Except(before).Count(), "the two boxes, widened");

            foreach (var sql in ElohSpaceShrines.Up())
                Assert.AreEqual(0, database.Context.Database.ExecuteSqlRaw(sql.Replace("{", "{{").Replace("}", "}}")), sql);
            CollectionAssert.AreEqual(once, Snapshot(database), "twice");

            migrator.Migrate(Previous);
            CollectionAssert.AreEqual(before, Snapshot(database), "Down");
        }

        [TestMethod]
        public void OnMySqlTheRowsChangeAndChangeBack()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();

            foreach (var sql in ElohSpaceShrines.Up())
                StringAssert.Contains(migrator.GenerateScript(Previous, Migration), sql);
            foreach (var sql in ElohSpaceShrines.Down())
                StringAssert.Contains(migrator.GenerateScript(Migration, Previous), sql);
        }

        /// <summary>The highest-priority region here that turns the setting on, 0 for none (the map's own).</summary>
        private static uint Shown(List<MapRegion> volumes, Vector3 at, int flag, bool underground) =>
            volumes.Where(v => v.Enabled && Abyss.TryGetValue(v.RegionId, out var r) && (r.Flags & flag) != 0 && v.Contains(at, underground))
                .OrderByDescending(v => Abyss[v.RegionId].Priority).Select(v => v.RegionId).FirstOrDefault();

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == ElohSpaceShrines.MapContextId)
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT map_context_id || ' ' || region_id || ' ' || shape || ' ' || printf('%.1f %.1f %.1f %.1f %.1f %.1f %.1f %.1f', pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y) || ' ' || underground || ' ' || enabled || ' ' || comment AS Value " +
                    "FROM map_region")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
