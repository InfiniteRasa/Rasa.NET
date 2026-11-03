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

    /// <summary>Ligo Thunderhead's Eloh chambers, fault chasm, Brimstone passage and Brann camps (Thunderhead_regions, ThunderheadRegions).</summary>
    [TestClass]
    [DoNotParallelize]
    public class ThunderheadRegionsTests
    {
        private const string Migration = "20270709000000_Thunderhead_regions";
        private const string Previous = "20270704000000_Guardian_prominence_regions";

        private const int Sky = 1, Env = 2, Ambient = 4, Music = 8, Name = 16;

        /// <summary>Ligo Thunderhead's regions, as the .map has them: priority and the settings they turn on.</summary>
        private static readonly Dictionary<uint, (int Priority, int Flags)> Thunderhead = new()
        {
            [1] = (1, 13), [9] = (2, 21), [25] = (3, 31), [39] = (4, 23), [17] = (5, 31), [10] = (6, 16), [11] = (7, 16), [30] = (8, 24),
            [12] = (9, 20), [31] = (10, 24), [16] = (11, 28), [26] = (12, 31), [20] = (13, 12), [19] = (14, 12), [15] = (15, 16), [14] = (16, 24),
            [18] = (17, 28), [28] = (18, 20), [6] = (19, 7), [27] = (20, 31), [7] = (21, 7), [29] = (22, 15), [38] = (23, 31), [37] = (24, 11),
            [32] = (25, 28), [33] = (26, 28), [36] = (27, 28), [35] = (28, 28), [13] = (29, 1),
        };

        [TestMethod]
        public void TheElohChambersHaveTheElohInterior()
        {
            var (before, after) = Migrated();
            var vashtar = new Vector3(1040f, 445.2f, 251f);
            var northWest = new Vector3(-907f, 395f, 410f);

            Assert.AreEqual(38u, Shown(before, vashtar, Sky, underground: true), "the Vashtar Pass chamber had the pass's cave sky");
            Assert.AreEqual(37u, Shown(after, vashtar, Sky, underground: true), "the Eloh interior");
            Assert.AreEqual(37u, Shown(after, vashtar, Music, underground: true), "with the Eloh music");
            Assert.AreEqual(38u, Shown(after, vashtar, Name, underground: true), "still in Vashtar Pass");
            Assert.AreEqual(38u, Shown(after, new Vector3(930f, 430.2f, 251f), Sky, underground: true), "the pass's cave outside it, as before");
            Assert.AreEqual(0u, Shown(before, northWest, Sky, underground: true), "the north-west chamber had none");
            Assert.AreEqual(37u, Shown(after, northWest, Sky, underground: true), "the Eloh interior");
            Assert.AreEqual(0u, Shown(after, new Vector3(-907f, 394.7f, 345f), Sky, underground: false), "not outside its door");
        }

        [TestMethod]
        public void TheFaultChasmIsTheDarkFallingSky()
        {
            var (before, after) = Migrated();
            var floor = new Vector3(50f, 172.8f, 0f);

            Assert.AreEqual(0u, Shown(before, floor, Sky, underground: true), "the chasm floor had the map's sky");
            Assert.AreEqual(13u, Shown(after, floor, Sky, underground: true), "the dark falling sky");
            Assert.AreEqual(13u, Shown(after, new Vector3(0f, 172.8f, 50f), Sky, underground: false), "on the open floor too");
            Assert.AreEqual(13u, Shown(after, new Vector3(-250.2f, 240.6f, 117.5f), Sky, underground: false), "and in the western arm");
            Assert.AreEqual(0u, Shown(after, new Vector3(259f, 295.6f, 40f), Sky, underground: true), "not the Bane overlook on its wall");
            Assert.AreEqual(0u, Shown(after, new Vector3(88.6f, 323.1f, -62.5f), Sky, underground: false), "nor the Fault Lever control point");
            Assert.AreEqual(0u, Shown(after, new Vector3(336.5f, 301.5f, 26f), Sky, underground: false), "nor a Fault Hammer");
        }

        [TestMethod]
        public void ThePassageToBrimstoneCavernIsACave()
        {
            var (before, after) = Migrated();
            var passage = new Vector3(612f, 367f, -700f);

            Assert.AreEqual(0u, Shown(before, passage, Sky, underground: true), "the passage had the map's sky");
            Assert.AreEqual(39u, Shown(after, passage, Sky, underground: true), "the cave sky");
            Assert.AreEqual(39u, Shown(after, passage, Name, underground: true), "named for Thunderhead Base");
            Assert.AreEqual(39u, Shown(after, new Vector3(615f, 370.6f, -675f), Env, underground: true), "from its top");
            Assert.AreEqual(39u, Shown(after, new Vector3(632f, 367.6f, -747f), Sky, underground: true), "to its foot");
            Assert.AreEqual(0u, Shown(after, new Vector3(670f, 367.7f, -750f), Sky, underground: false), "not the cavern beyond its mouth");
            Assert.AreEqual(25u, Shown(after, new Vector3(660f, 377.2f, -620f), Sky, underground: false), "the base's hospital, as before");
        }

        [TestMethod]
        public void TheBrannCampsAreTheirSyndicates()
        {
            var (before, after) = Migrated();
            var viddia = new Vector3(-1.8f, 337.4f, -431.4f);
            var larai = new Vector3(-536f, 365.5f, -245f);

            Assert.AreEqual(0u, Shown(before, viddia, Name, underground: false), "Viddia Outpost had no name");
            Assert.AreEqual(30u, Shown(after, viddia, Name, underground: false), "Viddia Syndicate Camp");
            Assert.AreEqual(30u, Shown(after, viddia, Music, underground: false), "with the Brann safe zone music");
            Assert.AreEqual(30u, Shown(after, new Vector3(-20f, 338.6f, -470f), Name, underground: false), "to its waypoint");
            Assert.AreEqual(0u, Shown(before, larai, Name, underground: false), "Larai Outpost had no name");
            Assert.AreEqual(31u, Shown(after, larai, Name, underground: false), "Larai Syndicate Camp");
            Assert.AreEqual(31u, Shown(after, larai, Music, underground: false), "with the Brann safe zone music");
            Assert.AreEqual(12u, Shown(after, larai, Ambient, underground: false), "and the distant battle");
            Assert.AreEqual(31u, Shown(after, new Vector3(-560f, 367.1f, -200f), Name, underground: false), "to its tower and teleporter");
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
            Assert.AreEqual(ThunderheadRegions.Volumes.Length, once.Except(before).Count(), "the new rows");

            foreach (var sql in ThunderheadRegions.Up())
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

            foreach (var sql in ThunderheadRegions.Up())
                StringAssert.Contains(migrator.GenerateScript(Previous, Migration), sql);
            foreach (var sql in ThunderheadRegions.Down())
                StringAssert.Contains(migrator.GenerateScript(Migration, Previous), sql);
        }

        /// <summary>The highest-priority region here that turns the setting on, 0 for none (the map's own).</summary>
        private static uint Shown(List<MapRegion> volumes, Vector3 at, int flag, bool underground) =>
            volumes.Where(v => v.Enabled && Thunderhead.TryGetValue(v.RegionId, out var r) && (r.Flags & flag) != 0 && v.Contains(at, underground))
                .OrderByDescending(v => Thunderhead[v.RegionId].Priority).Select(v => v.RegionId).FirstOrDefault();

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == ThunderheadRegions.MapContextId)
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT map_context_id || ' ' || region_id || ' ' || shape || ' ' || printf('%.1f %.1f %.1f %.1f %.1f %.1f %.1f %.1f', pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y) || ' ' || underground || ' ' || enabled || ' ' || comment AS Value " +
                    "FROM map_region WHERE map_context_id = 1911")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
