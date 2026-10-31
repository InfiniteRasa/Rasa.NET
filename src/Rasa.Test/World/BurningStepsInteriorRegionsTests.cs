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

    /// <summary>Outpost Intrepid's and Khulago Base's interiors on Burning Steps (Burning_steps_interior_regions, BurningStepsInteriorRegions).</summary>
    [TestClass]
    [DoNotParallelize]
    public class BurningStepsInteriorRegionsTests
    {
        private const string Migration = "20270716000000_Burning_steps_interior_regions";
        private const string Previous = "20270715000000_Wilderness_interior_regions";

        private const int Sky = 1, Env = 2, Ambient = 4, Music = 8, Name = 16;

        /// <summary>Burning Steps' regions, as the .map has them: priority and the settings they turn on.</summary>
        private static readonly Dictionary<uint, (int Priority, int Flags)> Crucible = new()
        {
            [1] = (1, 13), [31] = (2, 23), [30] = (3, 16), [21] = (4, 31), [27] = (5, 31), [3] = (6, 31), [24] = (7, 27), [9] = (8, 31), [2] = (9, 31),
            [4] = (10, 31), [29] = (11, 31), [6] = (12, 31), [7] = (13, 27), [17] = (14, 31), [8] = (15, 31), [20] = (16, 27), [28] = (17, 27),
            [15] = (18, 16), [16] = (19, 24), [18] = (20, 27), [14] = (21, 19), [10] = (22, 31), [11] = (23, 31), [12] = (24, 27), [19] = (25, 19),
            [13] = (26, 23), [22] = (27, 23), [5] = (28, 31), [23] = (29, 23), [33] = (30, 31), [25] = (31, 0), [26] = (32, 0), [32] = (33, 0),
        };

        [TestMethod]
        public void OutpostIntrepidIsLitAndNamedUnderground()
        {
            var (before, after) = Migrated();
            var rooms = new (Vector3 At, string Where)[]
            {
                (new Vector3(968f, 140.1f, 60f), "the hall"), (new Vector3(945.9f, 144.1f, 60.3f), "the waypoint"), (new Vector3(920f, 148.9f, 60f), "the shops"),
                (new Vector3(1016f, 144.7f, 46f), "the vendors"), (new Vector3(1039.6f, 144.7f, 60f), "the crafting station"),
                (new Vector3(968f, 144.7f, -4f), "the south corridor"), (new Vector3(987.6f, 144.9f, 108f), "the hospital"),
                (new Vector3(1000f, 152.7f, 180f), "the north corridor"), (new Vector3(1032f, 152.7f, 228f), "further north"),
                (new Vector3(1048f, 160.7f, 320f), "up the first ramp"), (new Vector3(1064f, 164.7f, 360f), "up the second"),
                (new Vector3(1048f, 168.7f, 420f), "the corridors at the top"), (new Vector3(1048f, 168.7f, 470f), "by the north exit"),
            };

            foreach (var (at, where) in rooms)
            {
                Assert.AreEqual(0u, Shown(before, at, Sky, underground: true), $"{where} had the map's sky");
                Assert.AreEqual(4u, Shown(after, at, Sky, underground: true), $"{where}: the outpost's underground sky");
                Assert.AreEqual(4u, Shown(after, at, Music, underground: true), $"{where}: its music");
                Assert.AreEqual(4u, Shown(after, at, Name, underground: true), $"{where}: Outpost Intrepid");
            }

            var label = new Vector3(942f, 152.3f, 180f);
            Assert.AreEqual(4u, Shown(before, label, Sky, underground: false), "the lava field over it had the underground sky");
            Assert.AreEqual(0u, Shown(after, label, Sky, underground: false), "it has the map's");
            Assert.AreEqual(0u, Shown(after, new Vector3(950f, 165.7f, 180f), Sky, underground: false), "and the north-west exit's mouth");
            Assert.AreEqual(0u, Shown(after, new Vector3(968f, 144.3f, -40f), Sky, underground: false), "and the dropship pad");
        }

        [TestMethod]
        public void KhulagoBasesTunnelsAndLaboratoryAreItsInterior()
        {
            var (before, after) = Migrated();
            var tunnels = new[] { new Vector3(114f, 139.9f, -370f), new Vector3(186f, 141.8f, -370f), new Vector3(230f, 143.1f, -370f), new Vector3(250f, 153.9f, -370f) };
            var laboratory = new[] { new Vector3(-144f, 199.7f, -286f), new Vector3(-80f, 199.7f, -286f), new Vector3(-16f, 201.7f, -286f) };

            foreach (var at in tunnels.Concat(laboratory))
            {
                Assert.AreEqual(0u, Shown(before, at, Sky, underground: true), $"{at} had the map's sky");
                Assert.AreEqual(31u, Shown(after, at, Sky, underground: true), $"{at}: Khulago's Bane interior");
                Assert.AreEqual(31u, Shown(after, at, Ambient, underground: true), $"{at}: the tunnel ambience");
                Assert.AreEqual(31u, Shown(after, at, Name, underground: true), $"{at}: Khulago Base");
            }

            var outdoors = new Vector3(382f, 174.5f, -188f);
            Assert.AreEqual(31u, Shown(before, outdoors, Ambient, underground: false), "the open base had the tunnel ambience");
            Assert.AreEqual(0u, Shown(after, outdoors, Ambient, underground: false), "it has the map's");
            Assert.AreEqual(7u, Shown(after, outdoors, Sky, underground: false), "under Khulago's own sky");
            Assert.AreEqual(7u, Shown(after, outdoors, Name, underground: false), "and name");
            Assert.AreEqual(0u, Shown(after, new Vector3(262f, 157.8f, -370f), Sky, underground: false), "the service entrance's mouth stays outdoors");
        }

        [TestMethod]
        public void TheCaveBehindTheOverlookReachesItsEastEnd()
        {
            var (before, after) = Migrated();
            var east = new Vector3(82f, 224.5f, 92f);

            Assert.AreEqual(0u, Shown(before, east, Sky, underground: true), "the east split had the map's sky");
            Assert.AreEqual(23u, Shown(after, east, Sky, underground: true), "the cave's");
            Assert.AreEqual(23u, Shown(after, new Vector3(0f, 231.5f, 92f), Sky, underground: true), "as the rest of the cave");
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
            Assert.AreEqual(BurningStepsInteriorRegions.Volumes.Length + BurningStepsInteriorRegions.Recuts.Length, once.Except(before).Count(), "the new rows and the recut ones");

            foreach (var sql in BurningStepsInteriorRegions.Up())
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

            foreach (var sql in BurningStepsInteriorRegions.Up())
                StringAssert.Contains(migrator.GenerateScript(Previous, Migration), sql);
            foreach (var sql in BurningStepsInteriorRegions.Down())
                StringAssert.Contains(migrator.GenerateScript(Migration, Previous), sql);
        }

        /// <summary>The highest-priority region here that turns the setting on, 0 for none (the map's own).</summary>
        private static uint Shown(List<MapRegion> volumes, Vector3 at, int flag, bool underground) =>
            volumes.Where(v => v.Enabled && Crucible.TryGetValue(v.RegionId, out var r) && (r.Flags & flag) != 0 && v.Contains(at, underground))
                .OrderByDescending(v => Crucible[v.RegionId].Priority).Select(v => v.RegionId).FirstOrDefault();

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == BurningStepsInteriorRegions.MapContextId)
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT map_context_id || ' ' || region_id || ' ' || shape || ' ' || printf('%.1f %.1f %.1f %.1f %.1f %.1f %.1f %.1f', pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y) || ' ' || underground || ' ' || enabled || ' ' || comment AS Value " +
                    "FROM map_region WHERE map_context_id = 1993")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
