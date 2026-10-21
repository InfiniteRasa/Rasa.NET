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

    /// <summary>The Irendas Penal Colony's rooms (Irendas_penal_colony_regions, IrendasPenalColonyRegions).</summary>
    [TestClass]
    [DoNotParallelize]
    public class IrendasPenalColonyRegionsTests
    {
        private const string Migration = "20270218000000_Irendas_penal_colony_regions";
        private const string Previous = "20270217000000_Caves_of_donn";

        private const int Sky = 1, Env = 2, Ambient = 4, Music = 8, Name = 16;

        /// <summary>Torden Plains' regions, as the .map has them: priority and the settings they turn on.</summary>
        private static readonly Dictionary<uint, (int Priority, int Flags)> Plains = new()
        {
            [1] = (1, 13), [8] = (2, 5), [4] = (3, 29), [14] = (4, 21), [20] = (5, 29), [15] = (6, 17), [17] = (7, 16), [26] = (8, 17),
            [19] = (9, 17), [21] = (10, 31), [18] = (11, 25), [12] = (12, 7), [2] = (13, 29), [3] = (14, 21), [7] = (15, 31), [13] = (16, 29),
            [5] = (17, 21), [32] = (18, 3), [25] = (19, 22), [6] = (20, 23), [9] = (21, 31), [22] = (22, 0), [23] = (23, 0), [24] = (24, 0),
            [10] = (25, 17), [11] = (26, 17), [16] = (27, 1), [31] = (28, 16), [27] = (29, 16), [29] = (30, 16), [34] = (31, 16), [33] = (32, 0),
        };

        [TestMethod]
        public void TheCorridorsUnderThePrisonAreThePenalColonysInterior()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();
            var corridor = new Vector3(208f, 426f, -199f);

            migrator.Migrate(Previous);
            var before = Volumes(database);
            Assert.IsFalse(before.Any(v => v.Enabled && v.RegionId is 6 or 9 or 10 or 11 or 25), "none had a volume in use");
            Assert.AreNotEqual(6u, Shown(before, corridor, Name, underground: true));

            migrator.Migrate(Migration);
            var after = Volumes(database);

            foreach (var underground in new[] { true, false })
            {
                Assert.AreEqual(6u, Shown(after, corridor, Name, underground), "the corridor at the foot of the dome's ramp");
                Assert.AreEqual(6u, Shown(after, corridor, Sky, underground), "under the Brann interior sky");
                Assert.AreEqual(6u, Shown(after, new Vector3(208f, 394f, -170f), Ambient, underground), "the greenhouse");
                Assert.AreEqual(6u, Shown(after, new Vector3(208f, 402f, -240f), Name, underground), "the lower level");
                Assert.AreEqual(6u, Shown(after, new Vector3(80f, 402f, -279f), Name, underground), "the west rooms");
                Assert.AreEqual(6u, Shown(after, new Vector3(56f, 402f, -279f), Name, underground), "at the Penal Research door");
            }

            Assert.AreEqual(5u, Shown(after, new Vector3(208f, 433.3f, -100f), Name, underground: false), "the compound's open ground is still region 5");
            Assert.AreEqual(5u, Shown(after, new Vector3(208f, 433.3f, -100f), Sky, underground: false));
            Assert.AreEqual(5u, Shown(after, new Vector3(130f, 433.3f, -280f), Name, underground: false), "over the west rooms");
        }

        [TestMethod]
        public void TheWardIsTheHospitalAndTheSideRoomsTheDormitories()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Migration);
            var after = Volumes(database);

            var ward = new Vector3(208f, 426f, -218f);
            foreach (var flag in new[] { Sky, Env, Ambient, Music, Name })
                Assert.AreEqual(9u, Shown(after, ward, flag, underground: true), $"at the hospital's revive point ({flag})");
            Assert.AreEqual(6u, Shown(after, new Vector3(208f, 418f, -243f), Name, underground: true), "the corridor south of the ward");

            Assert.AreEqual(10u, Shown(after, new Vector3(160f, 418f, -262f), Name, underground: true), "the west dorm, with its class trainers");
            Assert.AreEqual(11u, Shown(after, new Vector3(262f, 418f, -263f), Name, underground: true), "the east dorm");
            Assert.AreEqual(6u, Shown(after, new Vector3(160f, 418f, -262f), Ambient, underground: true), "the corridors' ambient in both");
            Assert.AreEqual(5u, Shown(after, new Vector3(150f, 433.3f, -270f), Name, underground: false), "the ground over the west dorm");
        }

        [TestMethod]
        public void TheTopOfTheHqTowerIsTheCommandCenter()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Migration);
            var after = Volumes(database);
            var room = new Vector3(320f, 499f, -30f);

            Assert.AreEqual(25u, Shown(after, room, Name, underground: false), "among the workstations");
            Assert.AreEqual(25u, Shown(after, room, Env, underground: false));
            Assert.AreEqual(25u, Shown(after, room, Ambient, underground: false));
            Assert.AreEqual(5u, Shown(after, room, Sky, underground: false), "its own sky is none: the colony's outdoor one");

            Assert.AreEqual(5u, Shown(after, new Vector3(295.5f, 433.2f, -71f), Name, underground: false), "the armor vendor at its foot");
            Assert.AreEqual(5u, Shown(after, new Vector3(320f, 434f, -40f), Name, underground: false), "the ground under the tower");
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
            Assert.AreEqual(IrendasPenalColonyRegions.Volumes.Length, once.Except(before).Count());

            foreach (var sql in IrendasPenalColonyRegions.Up())
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

            foreach (var sql in IrendasPenalColonyRegions.Up())
                StringAssert.Contains(migrator.GenerateScript(Previous, Migration), sql);
            foreach (var sql in IrendasPenalColonyRegions.Down())
                StringAssert.Contains(migrator.GenerateScript(Migration, Previous), sql);
        }

        /// <summary>The highest-priority region here that turns the setting on, 0 for none (the map's own).</summary>
        private static uint Shown(List<MapRegion> volumes, Vector3 at, int flag, bool underground) =>
            volumes.Where(v => v.Enabled && Plains.TryGetValue(v.RegionId, out var r) && (r.Flags & flag) != 0 && v.Contains(at, underground))
                .OrderByDescending(v => Plains[v.RegionId].Priority).Select(v => v.RegionId).FirstOrDefault();

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == IrendasPenalColonyRegions.MapContextId)
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT map_context_id || ' ' || region_id || ' ' || shape || ' ' || printf('%.1f %.1f %.1f %.1f %.1f', pos_x, pos_z, radius, half_x, half_z) || ' ' || enabled AS Value " +
                    "FROM map_region")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
