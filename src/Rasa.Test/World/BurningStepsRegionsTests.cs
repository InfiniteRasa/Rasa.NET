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

    /// <summary>The Staal's buildings, the Eloh places and the Chaukus Robotics Facility on Burning Steps (Burning_steps_regions, BurningStepsRegions).</summary>
    [TestClass]
    [DoNotParallelize]
    public class BurningStepsRegionsTests
    {
        private const string Migration = "20270220000000_Burning_steps_regions";
        private const string Previous = "20270219000000_Atta_hive_laboratory_regions";

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
        public void StaalsRoomsUnderTheCraterFloorHaveTheirRegions()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();
            var centrum = new Vector3(-192f, 181f, 264f);

            migrator.Migrate(Previous);
            var before = Volumes(database);
            Assert.AreEqual(0u, Shown(before, centrum, Name, underground: true), "nothing was named in the panopticon");

            migrator.Migrate(Migration);
            var after = Volumes(database);
            uint At(float x, float y, float z, bool underground = true) => Shown(after, new Vector3(x, y, z), Name, underground);

            Assert.AreEqual(2u, At(-192f, 181f, 264f), "the Centrum's crafting stations");
            Assert.AreEqual(2u, At(-164f, 189f, 354f), "its vendors");
            Assert.AreEqual(2u, At(-232f, 181f, 320f), "the Detention Center's waypoint, on the panopticon floor");
            Assert.AreEqual(3u, At(-215f, 245.4f, 319f), "Kappa Gruppa's office at the top of the tower is the Detention Center's");
            Assert.AreEqual(3u, At(-100f, 188.7f, 320f), "and so is the corridor out under the gatehouse");
            Assert.AreEqual(24u, At(-216f, 181f, 405f), "at the Incurables Ward's door");
            Assert.AreEqual(9u, At(-216f, 196f, 472f), "the Viddia Quarter is as it was");
            Assert.AreEqual(20u, At(-348f, 172f, 328f), "in the Larai Arena");
            Assert.AreEqual(28u, At(-296f, 197f, 372f), "at the nightclub's speakers");
            Assert.AreEqual(28u, At(-306f, 198f, 368f), "and its bar");
            Assert.AreEqual(8u, At(-300f, 181f, 320f), "the Larai Quarter is as it was");

            Assert.AreEqual(27u, At(-21f, 188f, 323f, underground: false), "the gate yard, under the sky");
            Assert.AreEqual(27u, Shown(after, new Vector3(-21f, 188f, 323f), Sky, underground: false));
            Assert.AreNotEqual(3u, At(-377f, 253f, 329f, underground: false), "the junkyard bridge over the arena is outdoors");
            Assert.AreNotEqual(3u, At(-250f, 248f, 420f, underground: false), "and so is the ground over the city");
            Assert.AreEqual(10u, At(-414f, 251f, 256f, underground: false), "the Labbna Quarter is as it was");
        }

        [TestMethod]
        public void TheElohCorridorsAreElohSpaceAndThePodsCraterTheCrashSite()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();
            var pod = new Vector3(575f, 149f, 232f);

            migrator.Migrate(Previous);
            Assert.AreEqual(17u, Shown(Volumes(database), pod, Name, underground: false), "the Devil's Pitchfork's circle was over the crater");

            migrator.Migrate(Migration);
            var after = Volumes(database);

            Assert.AreEqual(33u, Shown(after, new Vector3(-268f, 181f, 228f), Sky, underground: true), "at the logos by the Top Khana Quarter");
            Assert.AreEqual(33u, Shown(after, new Vector3(32f, 232f, 20f), Sky, underground: true), "behind the Eloh door north of the Overlook");
            Assert.AreEqual(33u, Shown(after, new Vector3(600f, 180f, -225f), Sky, underground: true), "by Khulago Base");

            Assert.AreEqual(21u, Shown(after, pod, Name, underground: false), "at the escape pod");
            Assert.AreEqual(21u, Shown(after, new Vector3(600f, 154.4f, 260f), Music, underground: false), "in its crater");
            Assert.AreEqual(17u, Shown(after, new Vector3(444.7f, 167f, 253.8f), Name, underground: false), "the Pitchfork keeps its own lava");
        }

        [TestMethod]
        public void TheCompoundAtTheRoboticsFacilitysDoorIsTheFacility()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Migration);
            var after = Volumes(database);

            Assert.AreEqual(29u, Shown(after, new Vector3(-528.6f, 249.7f, 581.6f), Name, underground: false), "at the door");
            Assert.AreEqual(29u, Shown(after, new Vector3(-528f, 239.4f, 650f), Name, underground: false), "inside the walls");
            Assert.AreEqual(29u, Shown(after, new Vector3(-528f, 255.3f, 760f), Name, underground: false), "at the facility's entrance structure");
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
            Assert.AreEqual(BurningStepsRegions.Volumes.Length + 2, once.Except(before).Count(), "the new rows, the yard and the Pitchfork");

            foreach (var sql in BurningStepsRegions.Up())
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

            foreach (var sql in BurningStepsRegions.Up())
                StringAssert.Contains(migrator.GenerateScript(Previous, Migration), sql);
            foreach (var sql in BurningStepsRegions.Down())
                StringAssert.Contains(migrator.GenerateScript(Migration, Previous), sql);
        }

        /// <summary>The highest-priority region here that turns the setting on, 0 for none (the map's own).</summary>
        private static uint Shown(List<MapRegion> volumes, Vector3 at, int flag, bool underground) =>
            volumes.Where(v => v.Enabled && Crucible.TryGetValue(v.RegionId, out var r) && (r.Flags & flag) != 0 && v.Contains(at, underground))
                .OrderByDescending(v => Crucible[v.RegionId].Priority).Select(v => v.RegionId).FirstOrDefault();

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == BurningStepsRegions.MapContextId)
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT map_context_id || ' ' || region_id || ' ' || shape || ' ' || printf('%.1f %.1f %.1f %.1f %.1f', pos_x, pos_z, radius, half_x, half_z) || ' ' || enabled AS Value " +
                    "FROM map_region")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
