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

    /// <summary>
    /// Daghda's Urn Village's three AFS buildings under the Wilderness's human interiors, not its
    /// cave (Daghdas_urn_building_regions, DaghdasUrnBuildingRegions): walking into the hospital
    /// gave cave light and the cave's echo.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class DaghdasUrnBuildingRegionsTests
    {
        private const string Migration = "20270923000000_Daghdas_urn_building_regions";
        private const string Previous = "20270922000000_Control_tower_teleporters";

        private const int Sky = 1, Env = 2, Ambient = 4, Music = 8, Name = 16;

        /// <summary>The Wilderness's regions, as the .map has them: priority and the settings they turn on.</summary>
        private static readonly Dictionary<uint, (int Priority, int Flags)> Wilderness = new()
        {
            [5] = (1, 13), [35] = (2, 0), [37] = (3, 30), [13] = (4, 4), [31] = (5, 16), [25] = (6, 14), [21] = (7, 14), [22] = (8, 14), [23] = (9, 14),
            [24] = (10, 14), [19] = (11, 28), [18] = (12, 28), [50] = (13, 28), [56] = (14, 4), [17] = (15, 28), [16] = (16, 28), [6] = (17, 31), [7] = (18, 18),
            [34] = (19, 31), [1] = (20, 15), [32] = (21, 31), [57] = (22, 11), [53] = (23, 0), [54] = (24, 0), [55] = (25, 0), [33] = (26, 31), [66] = (27, 15),
            [9] = (28, 22), [49] = (29, 28), [12] = (30, 28), [47] = (31, 18), [10] = (32, 20), [36] = (33, 31), [45] = (34, 0), [46] = (35, 0), [14] = (36, 28),
            [48] = (37, 28), [38] = (38, 31), [39] = (39, 15), [15] = (40, 31), [8] = (41, 31), [11] = (42, 20), [51] = (43, 28), [26] = (44, 7), [27] = (45, 7),
            [30] = (46, 7), [58] = (47, 0), [59] = (48, 0), [60] = (49, 0), [61] = (50, 0), [62] = (51, 0), [63] = (52, 0), [64] = (53, 0), [65] = (54, 0),
        };

        /// <summary>Inside each building: the hospital's two floors, the housing, the research HQ.</summary>
        private static readonly (Vector3 At, uint Region, string Where)[] Inside =
        {
            (new Vector3(-650f, 283.7f, 885f), DaghdasUrnBuildingRegions.HospitalRegion, "the hospital's ground floor, by the revive point"),
            (new Vector3(-657.2f, 283.6f, 880.5f), DaghdasUrnBuildingRegions.HospitalRegion, "the hospital's revive point"),
            (new Vector3(-640f, 292.1f, 900f), DaghdasUrnBuildingRegions.HospitalRegion, "the hospital's upper floor"),
            (new Vector3(-608f, 282.3f, 862f), DaghdasUrnBuildingRegions.HousingRegion, "the housing"),
            (new Vector3(-555f, 283.3f, 885f), DaghdasUrnBuildingRegions.ResearchHqRegion, "the research HQ"),
        };

        [TestMethod]
        public void TheBuildingsHaveAHumanInteriorsSkyAndSoundAndTheVillagesNameAndMusic()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            var before = Volumes(database);
            foreach (var (at, _, where) in Inside)
            {
                Assert.AreEqual(38u, Shown(before, at, Sky), $"{where}: the cave's sky, as reported");
                Assert.AreEqual(38u, Shown(before, at, Ambient), $"{where}: and its echo");
            }

            migrator.Migrate(Migration);
            var after = Volumes(database);

            foreach (var (at, region, where) in Inside)
            {
                Assert.AreEqual(region, Shown(after, at, Sky), $"{where}: the human interior's sky");
                Assert.AreEqual(region, Shown(after, at, Env), $"{where}: and its lighting");
                Assert.AreEqual(region, Shown(after, at, Ambient), $"{where}: and its sound");
                Assert.AreEqual(48u, Shown(after, at, Music), $"{where}: the safe zone's music");
                Assert.AreEqual(48u, Shown(after, at, Name), $"{where}: in Daghda's Urn Village");
            }

            Assert.AreEqual(0u, Shown(after, new Vector3(-628f, 278.1f, 799f), Sky), "the open village keeps the Wilderness's sky");
            Assert.AreEqual(0u, Shown(after, new Vector3(-645f, 283f, 865f), Sky), "and so does the ground outside the hospital's door");
            Assert.IsFalse(after.Any(v => v.Enabled && v.RegionId == DaghdasUrnBuildingRegions.CaveRegion), "the cave is nowhere");
        }

        [TestMethod]
        public void TheUpRunsTwiceAndTheDownPutsTheCaveBack()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            var before = Snapshot(database);

            migrator.Migrate(Migration);
            var once = Snapshot(database);
            Assert.AreEqual(3, once.Except(before).Count(), "the three boxes");

            foreach (var sql in DaghdasUrnBuildingRegions.Up())
                Assert.AreEqual(0, database.Context.Database.ExecuteSqlRaw(sql.Replace("{", "{{").Replace("}", "}}")), sql);
            CollectionAssert.AreEqual(once, Snapshot(database), "twice");

            migrator.Migrate(Previous);
            CollectionAssert.AreEqual(before, Snapshot(database), "Down");
        }

        [TestMethod]
        public void ABoxAGmHasMovedIsLeftAlone()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            database.Context.Database.ExecuteSqlRaw("UPDATE map_region SET pos_x = pos_x + 2 WHERE map_context_id = 1220 AND region_id = 38 AND comment LIKE '%(housing)'");

            migrator.Migrate(Migration);
            var after = Volumes(database);
            Assert.AreEqual(1, after.Count(v => v.Enabled && v.RegionId == 38), "the moved housing box stays the GM's");
            Assert.AreEqual(1, after.Count(v => v.RegionId == DaghdasUrnBuildingRegions.HospitalRegion && v.Comment.Contains("Daghda's Urn Village")), "the hospital's box");
        }

        [TestMethod]
        public void OnMySqlTheRegionsChangeAndChangeBack()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();

            foreach (var sql in DaghdasUrnBuildingRegions.Up())
                StringAssert.Contains(migrator.GenerateScript(Previous, Migration), sql);
            foreach (var sql in DaghdasUrnBuildingRegions.Down())
                StringAssert.Contains(migrator.GenerateScript(Migration, Previous), sql);
        }

        /// <summary>The highest-priority region here that turns the setting on, 0 for none (the map's own).</summary>
        private static uint Shown(List<MapRegion> volumes, Vector3 at, int flag) =>
            volumes.Where(v => v.Enabled && Wilderness.TryGetValue(v.RegionId, out var r) && (r.Flags & flag) != 0 && v.Contains(at, false))
                .OrderByDescending(v => Wilderness[v.RegionId].Priority).Select(v => v.RegionId).FirstOrDefault();

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == DaghdasUrnBuildingRegions.MapContextId)
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT map_context_id || ' ' || region_id || ' ' || shape || ' ' || printf('%.1f %.1f %.1f %.1f %.1f', pos_x, pos_z, radius, half_x, half_z) || ' ' || enabled AS Value " +
                    "FROM map_region")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
