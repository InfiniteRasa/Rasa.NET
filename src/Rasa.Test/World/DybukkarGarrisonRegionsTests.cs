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

    /// <summary>Dybukkar Garrison's rooms (Dybukkar_garrison_regions, DybukkarGarrisonRegions).</summary>
    [TestClass]
    [DoNotParallelize]
    public class DybukkarGarrisonRegionsTests
    {
        private const string Migration = "20270606000000_Dybukkar_garrison_regions";
        private const string Previous = "20270605000000_Indra_caverns_regions";

        private const int Sky = 1, Env = 2, Ambient = 4, Music = 8, Name = 16;

        /// <summary>Dybukkar Garrison's regions, as the .map has them: priority and the settings they turn on.</summary>
        private static readonly Dictionary<uint, (int Priority, int Flags)> Dybukkar = new()
        {
            [1] = (1, 13), [20] = (2, 0), [19] = (3, 19), [6] = (4, 20), [7] = (5, 20), [3] = (6, 20), [8] = (7, 20), [4] = (8, 20), [5] = (9, 20),
            [18] = (10, 20), [11] = (11, 16), [10] = (12, 20), [12] = (13, 23), [13] = (14, 20), [14] = (15, 20), [16] = (16, 31), [22] = (17, 31),
            [15] = (18, 20), [17] = (19, 31), [21] = (20, 31),
        };

        private static readonly Vector3 SecurityRoom = new(-104f, 320.8f, -512f), OperationsRoom = new(0f, 319.8f, -384f), WindowRoom = new(32f, 329.6f, -16f);

        [TestMethod]
        public void SecurityAndOperationsGetTheirNamesBack()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            var before = Volumes(database);
            Assert.AreEqual(13u, Shown(before, SecurityRoom, Name, underground: false), "the Security room was named Security Overlook");
            Assert.AreEqual(18u, Shown(before, OperationsRoom, Name, underground: false), "and Operations Super Soldier Incubation");
            Assert.AreEqual(0u, Shown(before, WindowRoom, Sky, underground: true), "the window room had no window sky");

            migrator.Migrate(Migration);
            var after = Volumes(database);
            Assert.AreEqual(6u, Shown(after, SecurityRoom, Name, underground: false), "Security");
            Assert.AreEqual(8u, Shown(after, OperationsRoom, Name, underground: false), "Operations");
            Assert.AreEqual(8u, Shown(after, new Vector3(0f, 329.6f, -425f), Name, underground: false), "its command chairs upstairs");
            Assert.AreEqual(18u, Shown(after, new Vector3(52f, 320.2f, -296f), Name, underground: false), "Super Soldier Incubation");
            Assert.AreEqual(18u, Shown(after, new Vector3(150f, 320.2f, -296f), Name, underground: false), "its east ore chamber");
            Assert.AreEqual(18u, Shown(after, new Vector3(-20f, 320.2f, -296f), Name, underground: false), "its west one");
            Assert.AreEqual(13u, Shown(after, WindowRoom, Name, underground: true), "Security Overlook at the window");
            Assert.AreEqual(19u, Shown(after, WindowRoom, Sky, underground: true), "under the window's sky");
            Assert.AreEqual(13u, Shown(after, WindowRoom, Ambient, underground: true));
            Assert.AreEqual(13u, Shown(after, new Vector3(45f, 330.4f, -50f), Name, underground: true), "and down the room to the chamber");
            Assert.AreEqual(12u, Shown(after, new Vector3(32f, 322.4f, -152f), Name, underground: false), "the Main Production Chamber is as it was");
        }

        [TestMethod]
        public void TheGuicheWingAndTheConcourses()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Migration);
            var after = Volumes(database);
            uint At(float x, float y, float z, bool underground) => Shown(after, new Vector3(x, y, z), Name, underground);

            Assert.AreEqual(10u, At(190f, 321.9f, -150f, false), "the Harvesting Chamber's corridor");
            Assert.AreEqual(10u, At(215f, 321.9f, -125f, false), "a guiche room north");
            Assert.AreEqual(10u, At(165f, 321.6f, -180f, false), "and south");
            Assert.AreEqual(7u, At(-40f, 321.8f, -540f, false), "the South Concourse");
            Assert.AreEqual(7u, At(-40f, 321.9f, -584f, false), "by the medical team");
            Assert.AreEqual(7u, At(-66f, 322f, -568f, false), "at the way in");
            Assert.AreEqual(14u, At(120f, 329.8f, -40f, true), "the North Concourse");
            Assert.AreEqual(14u, At(152f, 329.8f, 0f, true), "its crosses");
            Assert.AreEqual(14u, At(184f, 329.8f, 60f, true), "its turn");
            Assert.AreEqual(14u, At(150f, 305.4f, 75f, true), "down the ramp to the Repair Yard");
            Assert.AreEqual(16u, At(-40f, 279.5f, 58f, false), "the Repair Yard is as it was");
        }

        [TestMethod]
        public void TheServiceCorridorsAreMaintenanceAreasAToCFromTheWayInToTheBack()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Migration);
            var after = Volumes(database);
            uint At(float x, float y, float z, bool underground) => Shown(after, new Vector3(x, y, z), Name, underground);

            Assert.AreEqual(3u, At(-64f, 329.2f, -480f, false), "Maintenance Area A, the service room behind Security");
            Assert.AreEqual(3u, At(-80f, 329.2f, -465f, false), "its west room");
            Assert.AreEqual(3u, At(-56f, 329f, -408f, false), "the block west of Operations");
            Assert.AreEqual(3u, At(-104f, 321.4f, -410f, false), "its corridor north from Security");
            Assert.AreEqual(3u, At(-90f, 321.2f, -385f, false), "its lower turn");
            Assert.AreEqual(6u, At(-104f, 320.8f, -512f, false), "Security is its own");
            Assert.AreEqual(6u, At(-75f, 320.8f, -485f, false), "to its corner");
            Assert.AreEqual(8u, At(0f, 319.8f, -384f, false), "and Operations");

            Assert.AreEqual(4u, At(60f, 328.6f, -345f, false), "Maintenance Area B, south of the ore chambers");
            Assert.AreEqual(4u, At(100f, 329.2f, -340f, false));
            Assert.AreEqual(4u, At(8f, 329.3f, -256f, false), "north of them");
            Assert.AreEqual(4u, At(128f, 329.6f, -235f, false), "past the machines");
            Assert.AreEqual(4u, At(150f, 320.9f, -205f, false), "down the ramp");
            Assert.AreEqual(18u, At(52f, 320.2f, -296f, false), "the chambers between are Incubation's");
            Assert.AreEqual(10u, At(165f, 321.6f, -180f, false), "the Harvesting Chamber its own");

            Assert.AreEqual(5u, At(-135f, 293.2f, -30f, true), "Maintenance Area C, the room by the yard doors");
            Assert.AreEqual(5u, At(-135f, 293.2f, -15f, true), "its cross");
            Assert.AreEqual(5u, At(-111f, 293.3f, -14f, true), "the service entrance");
            Assert.AreEqual(5u, At(-135f, 301.6f, 20f, true), "the upper service entrance");
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
            Assert.AreEqual(DybukkarGarrisonRegions.Volumes.Length + DybukkarGarrisonRegions.Recuts.Length, once.Except(before).Count(), "the new rows and the recut ones");

            foreach (var sql in DybukkarGarrisonRegions.Up())
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

            foreach (var sql in DybukkarGarrisonRegions.Up())
                StringAssert.Contains(migrator.GenerateScript(Previous, Migration), sql);
            foreach (var sql in DybukkarGarrisonRegions.Down())
                StringAssert.Contains(migrator.GenerateScript(Migration, Previous), sql);
        }

        /// <summary>The highest-priority region here that turns the setting on, 0 for none (the map's own).</summary>
        private static uint Shown(List<MapRegion> volumes, Vector3 at, int flag, bool underground) =>
            volumes.Where(v => v.Enabled && Dybukkar.TryGetValue(v.RegionId, out var r) && (r.Flags & flag) != 0 && v.Contains(at, underground))
                .OrderByDescending(v => Dybukkar[v.RegionId].Priority).Select(v => v.RegionId).FirstOrDefault();

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == DybukkarGarrisonRegions.MapContextId)
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT map_context_id || ' ' || region_id || ' ' || shape || ' ' || printf('%.1f %.1f %.1f %.1f %.1f %.1f %.1f %.1f', pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y) || ' ' || underground || ' ' || enabled || ' ' || comment AS Value " +
                    "FROM map_region")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
