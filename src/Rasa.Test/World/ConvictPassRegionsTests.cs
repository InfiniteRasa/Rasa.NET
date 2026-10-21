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

    /// <summary>The Convict Pass's lava tube and ravine (Convict_pass_regions, ConvictPassRegions).</summary>
    [TestClass]
    [DoNotParallelize]
    public class ConvictPassRegionsTests
    {
        private const string Migration = "20270221000000_Convict_pass_regions";
        private const string Previous = "20270220000000_Burning_steps_regions";

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
        public void TheTubeIsTheCaveAndTheRavineAtItsNorthMouthThePass()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();
            var roof = new Vector3(232f, 240f, 500f);
            var plateau = new Vector3(280f, 247f, 500f);
            var ledge = new Vector3(300f, 225f, 560f);

            migrator.Migrate(Previous);
            var before = Volumes(database);
            foreach (var outside in new[] { roof, plateau, ledge })
                Assert.AreEqual(13u, Shown(before, outside, Sky, underground: false), $"the minimap rectangle put the cave over {outside}");

            migrator.Migrate(Migration);
            var after = Volumes(database);

            Assert.AreEqual(13u, Shown(after, new Vector3(232f, 211f, 432f), Sky, underground: false), "at the south mouth");
            Assert.AreEqual(13u, Shown(after, new Vector3(232f, 208.5f, 504f), Sky, underground: false), "in the tube rooms");
            Assert.AreEqual(13u, Shown(after, new Vector3(232f, 194.5f, 590f), Sky, underground: false), "on the ramp");
            Assert.AreEqual(13u, Shown(after, new Vector3(265f, 176.5f, 650f), Sky, underground: false), "on the offset ramp to the north mouth");
            Assert.AreEqual(13u, Shown(after, new Vector3(264f, 174.6f, 680f), Name, underground: false), "in the north mouth");

            foreach (var outside in new[] { roof, plateau, ledge })
                Assert.AreNotEqual(13u, Shown(after, outside, Sky, underground: false), $"{outside} is outdoors");

            Assert.AreEqual(19u, Shown(after, new Vector3(290f, 164f, 720f), Name, underground: false), "in the ravine");
            Assert.AreEqual(19u, Shown(after, new Vector3(290f, 164f, 720f), Sky, underground: false), "under the sky");
            Assert.AreEqual(19u, Shown(after, new Vector3(320f, 165f, 780f), Name, underground: false), "at its north-east end");
            Assert.AreEqual(5u, Shown(after, new Vector3(215f, 212f, 360f), Name, underground: false), "the south mouth's side is the Staal Overlook");
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
            Assert.AreEqual(ConvictPassRegions.Volumes.Length + 1, once.Except(before).Count(), "the new rows and the minimap rectangle");

            foreach (var sql in ConvictPassRegions.Up())
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

            foreach (var sql in ConvictPassRegions.Up())
                StringAssert.Contains(migrator.GenerateScript(Previous, Migration), sql);
            foreach (var sql in ConvictPassRegions.Down())
                StringAssert.Contains(migrator.GenerateScript(Migration, Previous), sql);
        }

        /// <summary>The highest-priority region here that turns the setting on, 0 for none (the map's own).</summary>
        private static uint Shown(List<MapRegion> volumes, Vector3 at, int flag, bool underground) =>
            volumes.Where(v => v.Enabled && Crucible.TryGetValue(v.RegionId, out var r) && (r.Flags & flag) != 0 && v.Contains(at, underground))
                .OrderByDescending(v => Crucible[v.RegionId].Priority).Select(v => v.RegionId).FirstOrDefault();

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == ConvictPassRegions.MapContextId)
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT map_context_id || ' ' || region_id || ' ' || shape || ' ' || printf('%.1f %.1f %.1f %.1f %.1f', pos_x, pos_z, radius, half_x, half_z) || ' ' || enabled AS Value " +
                    "FROM map_region")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
