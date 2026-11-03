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

    /// <summary>Ashen Desert's unnamed interior regions (Ashen_desert_interior_regions, AshenDesertInteriorRegions).</summary>
    [TestClass]
    [DoNotParallelize]
    public class AshenDesertInteriorRegionsTests
    {
        private const string Migration = "20270707000000_Ashen_desert_interior_regions";
        private const string Previous = "20270704000000_Guardian_prominence_regions";

        private const int Sky = 1, Env = 2, Music = 8, Name = 16;

        /// <summary>Ashen Desert's regions, as the .map has them: priority and the settings they turn on.</summary>
        private static readonly Dictionary<uint, (int Priority, int Flags)> Desert = new()
        {
            [1] = (1, 13), [11] = (2, 24), [14] = (3, 16), [20] = (4, 24), [15] = (5, 24), [16] = (6, 28), [12] = (7, 28), [13] = (8, 16),
            [4] = (9, 11), [5] = (10, 28), [23] = (11, 31), [17] = (12, 31), [3] = (13, 3), [2] = (14, 1), [6] = (15, 19), [8] = (16, 31),
            [9] = (17, 16), [19] = (18, 28), [10] = (19, 16), [18] = (20, 16), [21] = (21, 28), [22] = (22, 28),
        };

        [TestMethod]
        public void ShadowsEdgePostsBuildingsAreLitIndoors()
        {
            var (before, after) = Migrated();
            var barracks = new Vector3(-96f, 297.1f, -792f);

            Assert.AreEqual(0u, Shown(before, barracks, Sky, underground: false), "the barracks had the desert's sky");
            Assert.AreEqual(4u, Shown(after, barracks, Sky, underground: false), "the human interior");
            Assert.AreEqual(4u, Shown(after, barracks, Env, underground: false), "and its light");
            Assert.AreEqual(5u, Shown(after, barracks, Name, underground: false), "still Shadow's Edge Post");
            Assert.AreEqual(5u, Shown(after, barracks, Music, underground: false), "with the post's music");
            Assert.AreEqual(4u, Shown(after, new Vector3(-160f, 297.1f, -792f), Sky, underground: false), "the west barracks");
            Assert.AreEqual(4u, Shown(after, new Vector3(-93f, 302.5f, -825f), Sky, underground: false), "the hospital");
            Assert.AreEqual(4u, Shown(after, new Vector3(-86f, 296.3f, -710f), Sky, underground: false), "the armor shop");
            Assert.AreEqual(4u, Shown(after, new Vector3(-172f, 296.5f, -709f), Sky, underground: false), "the weapon shop");
            Assert.AreEqual(0u, Shown(after, new Vector3(-130f, 295.3f, -765f), Sky, underground: false), "not the yard between them");
            Assert.AreEqual(23u, Shown(after, new Vector3(-173f, 297.1f, -831f), Sky, underground: false), "the Iron Lung, as before");
        }

        [TestMethod]
        public void TheTunnelsToTheLinksOutAreSulfurCaves()
        {
            var (before, after) = Migrated();
            var tunnels = new[]
            {
                new Vector3(95f, 318.5f, -952f), new Vector3(81f, 319.3f, -955f), new Vector3(300f, 321.6f, 328f), new Vector3(314f, 324.7f, 300f),
                new Vector3(-661f, 323.9f, -261.7f), new Vector3(-675f, 327.7f, -256f),
            };

            foreach (var at in tunnels)
            {
                Assert.AreEqual(0u, Shown(before, at, Sky, underground: true), $"{at} had the desert's sky");
                Assert.AreEqual(3u, Shown(after, at, Sky, underground: true), $"{at} is a sulfur cave");
                Assert.AreEqual(3u, Shown(after, at, Env, underground: true), $"{at} has its brown fog");
            }

            Assert.AreEqual(0u, Shown(after, new Vector3(150f, 312.4f, -948f), Sky, underground: false), "the Thunderhead tunnel's mouth stays in the open");
            Assert.AreEqual(0u, Shown(after, new Vector3(258f, 316f, 335f), Sky, underground: false), "and the Indra Caverns tunnel's");
            Assert.AreEqual(0u, Shown(after, new Vector3(-615f, 319.9f, -262f), Sky, underground: false), "and the Avernus Outpost tunnel's");
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
            Assert.AreEqual(AshenDesertInteriorRegions.Volumes.Length, once.Except(before).Count(), "the new rows");

            foreach (var sql in AshenDesertInteriorRegions.Up())
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

            foreach (var sql in AshenDesertInteriorRegions.Up())
                StringAssert.Contains(migrator.GenerateScript(Previous, Migration), sql);
            foreach (var sql in AshenDesertInteriorRegions.Down())
                StringAssert.Contains(migrator.GenerateScript(Migration, Previous), sql);
        }

        /// <summary>The highest-priority region here that turns the setting on, 0 for none (the map's own).</summary>
        private static uint Shown(List<MapRegion> volumes, Vector3 at, int flag, bool underground) =>
            volumes.Where(v => v.Enabled && Desert.TryGetValue(v.RegionId, out var r) && (r.Flags & flag) != 0 && v.Contains(at, underground))
                .OrderByDescending(v => Desert[v.RegionId].Priority).Select(v => v.RegionId).FirstOrDefault();

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == AshenDesertInteriorRegions.MapContextId)
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT map_context_id || ' ' || region_id || ' ' || shape || ' ' || printf('%.1f %.1f %.1f %.1f %.1f %.1f %.1f %.1f', pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y) || ' ' || underground || ' ' || enabled || ' ' || comment AS Value " +
                    "FROM map_region WHERE map_context_id = 1734")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
