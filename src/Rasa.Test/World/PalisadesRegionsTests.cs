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

    /// <summary>Palisades' temple passages, interiors, ruins and fields (Palisades_regions, PalisadesRegions).</summary>
    [TestClass]
    [DoNotParallelize]
    public class PalisadesRegionsTests
    {
        private const string Migration = "20270625000000_Palisades_regions";
        private const string Previous = "20270624000000_Comm_tower_regions";

        private const int Sky = 1, Ambient = 4, Name = 16;

        /// <summary>Palisades' regions, as the .map has them: priority and the settings they turn on.</summary>
        private static readonly Dictionary<uint, (int Priority, int Flags)> Palisades = new()
        {
            [1] = (1, 13), [31] = (2, 31), [30] = (3, 31), [29] = (4, 16), [28] = (5, 15), [26] = (6, 23), [21] = (7, 30), [22] = (8, 30),
            [23] = (9, 30), [18] = (10, 22), [25] = (11, 30), [19] = (12, 31), [20] = (13, 18), [17] = (14, 30), [16] = (15, 30), [2] = (16, 30),
            [3] = (17, 30), [4] = (18, 22), [5] = (19, 22), [6] = (20, 22), [7] = (21, 22), [8] = (22, 22), [9] = (23, 18), [27] = (24, 18),
            [14] = (25, 14), [10] = (26, 30), [11] = (27, 30), [35] = (28, 16), [12] = (29, 15), [13] = (30, 15), [34] = (31, 15), [32] = (32, 16),
            [33] = (33, 16), [36] = (34, 0),
        };

        [TestMethod]
        public void EachTemplePassageHasItsTemple()
        {
            var (before, after) = Migrated();
            var raging = new Vector3(-440f, 175.5f, 125f);

            Assert.AreNotEqual(25u, Shown(before, raging, Name, underground: true), "the passages had no volume");
            Assert.AreEqual(25u, Shown(after, raging, Name, underground: true), "Temple of the Raging Patriarch");
            Assert.AreEqual(25u, Shown(after, raging, Ambient, underground: true), "with its cave echo");
            Assert.AreEqual(23u, Shown(after, new Vector3(-760f, 187.2f, -90f), Name, underground: true), "Temple of the Bowed Patriarch");
            Assert.AreEqual(21u, Shown(after, new Vector3(-770f, 190.2f, 49f), Name, underground: true), "Temple of the Patriarch Protector");
            Assert.AreEqual(22u, Shown(after, new Vector3(-675f, 175.3f, -162f), Name, underground: true), "Temple of the Proud Patriarch");
        }

        [TestMethod]
        public void TheInteriorsHaveTheirSkies()
        {
            var (_, after) = Migrated();

            Assert.AreEqual(34u, Shown(after, new Vector3(-260f, 160.7f, 984f), Sky, underground: true), "the Eloh rooms behind the north cave");
            Assert.AreEqual(34u, Shown(after, new Vector3(-180f, 64.1f, -120f), Sky, underground: true), "the Eloh room under the Waterfall Cave");
            Assert.AreEqual(28u, Shown(after, new Vector3(600f, 145f, -880f), Sky, underground: true), "the Skive Base tunnels");
            Assert.AreEqual(28u, Shown(after, new Vector3(572f, 131.6f, -760f), Sky, underground: true), "the Skive Base control room");
            Assert.AreEqual(12u, Shown(after, new Vector3(-100f, 68.2f, -90f), Sky, underground: true), "the Waterfall Cave's lower tunnels");
            Assert.AreEqual(13u, Shown(after, new Vector3(-150f, 81.2f, -50f), Sky, underground: true), "its upper level");
            Assert.AreEqual(13u, Shown(after, new Vector3(-180f, 95.8f, -20f), Sky, underground: true), "up to its top floor");
            Assert.AreEqual(13u, Shown(after, new Vector3(-72f, 108.6f, -8f), Sky, underground: true), "and the ramp to the mouth");
            Assert.AreEqual(26u, Shown(after, new Vector3(-100f, 68.2f, -90f), Name, underground: true), "all of it still the Waterfall Cave");
            Assert.AreEqual(34u, Shown(after, new Vector3(-180f, 64.1f, -120f), Sky, underground: true), "but the Eloh room off the tunnels keeps its own sky");
            Assert.AreNotEqual(13u, Shown(after, new Vector3(-50f, 109.7f, -5f), Sky, underground: false), "and the waterfall outside is open sky");
        }

        [TestMethod]
        public void TheRuinsFieldsAndFacilityHaveTheirNames()
        {
            var (before, after) = Migrated();
            var town = new Vector3(-790f, 138.4f, 600f);

            Assert.AreEqual(0u, Shown(before, town, Name, underground: false), "the facility's circle missed the town");
            Assert.AreEqual(11u, Shown(after, town, Name, underground: false), "Cumbria Research Facility in the town");
            Assert.AreEqual(11u, Shown(after, new Vector3(-528f, 142.4f, 671f), Name, underground: false), "and at the wrecked research building");
            Assert.AreEqual(35u, Shown(after, new Vector3(-790f, 138.1f, 690f), Name, underground: false), "Cumbria Fields in the town's crop beds");
            Assert.AreEqual(35u, Shown(after, new Vector3(-665f, 138f, 700f), Name, underground: false), "and the burnt cornfield");
            Assert.AreEqual(27u, Shown(after, new Vector3(434f, 110.2f, 367f), Name, underground: false), "Forean Ruins on the south-west island");
            Assert.AreEqual(27u, Shown(after, new Vector3(477f, 111.8f, 524f), Name, underground: false), "and the middle ones");
            Assert.AreEqual(3u, Shown(after, new Vector3(400f, 108f, 520f), Name, underground: false), "Lake Elinor round them");
            Assert.AreEqual(20u, Shown(after, new Vector3(-376f, 215.2f, -360f), Name, underground: false), "Eye of the Needle on the land bridge");
            Assert.AreEqual(20u, Shown(after, new Vector3(-352f, 174.2f, -388f), Name, underground: true), "and under it");
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
            Assert.AreEqual(PalisadesRegions.Volumes.Length + PalisadesRegions.Recuts.Length, once.Except(before).Count(), "the new rows and the recut one");

            foreach (var sql in PalisadesRegions.Up())
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

            foreach (var sql in PalisadesRegions.Up())
                StringAssert.Contains(migrator.GenerateScript(Previous, Migration), sql);
            foreach (var sql in PalisadesRegions.Down())
                StringAssert.Contains(migrator.GenerateScript(Migration, Previous), sql);
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

        /// <summary>The highest-priority region here that turns the setting on, 0 for none (the map's own).</summary>
        private static uint Shown(List<MapRegion> volumes, Vector3 at, int flag, bool underground) =>
            volumes.Where(v => v.Enabled && Palisades.TryGetValue(v.RegionId, out var r) && (r.Flags & flag) != 0 && v.Contains(at, underground))
                .OrderByDescending(v => Palisades[v.RegionId].Priority).Select(v => v.RegionId).FirstOrDefault();

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == PalisadesRegions.MapContextId)
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT map_context_id || ' ' || region_id || ' ' || shape || ' ' || printf('%.1f %.1f %.1f %.1f %.1f %.1f %.1f %.1f', pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y) || ' ' || underground || ' ' || enabled || ' ' || comment AS Value " +
                    "FROM map_region")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
