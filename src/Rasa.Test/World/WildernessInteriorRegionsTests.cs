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

    /// <summary>Concordia Wilderness's unnamed interior regions (Wilderness_interior_regions, WildernessInteriorRegions).</summary>
    [TestClass]
    [DoNotParallelize]
    public class WildernessInteriorRegionsTests
    {
        private const string Migration = "20270715000000_Wilderness_interior_regions";
        private const string Previous = "20270714000000_Palisades_fithik_trench_region";

        private const int Sky = 1, Env = 2, Ambient = 4, Music = 8, Name = 16;

        /// <summary>Concordia Wilderness's regions, as the .map has them: priority and the settings they turn on.</summary>
        private static readonly Dictionary<uint, (int Priority, int Flags)> Wilderness = new()
        {
            [5] = (1, 13), [35] = (2, 0), [37] = (3, 30), [13] = (4, 4), [31] = (5, 16), [25] = (6, 14), [21] = (7, 14), [22] = (8, 14),
            [23] = (9, 14), [24] = (10, 14), [19] = (11, 28), [18] = (12, 28), [50] = (13, 28), [56] = (14, 4), [17] = (15, 28), [16] = (16, 28),
            [6] = (17, 31), [7] = (18, 18), [34] = (19, 31), [1] = (20, 15), [32] = (21, 31), [57] = (22, 11), [53] = (23, 0), [54] = (24, 0),
            [55] = (25, 0), [33] = (26, 31), [66] = (27, 15), [9] = (28, 22), [49] = (29, 28), [12] = (30, 28), [47] = (31, 18), [10] = (32, 20),
            [36] = (33, 31), [45] = (34, 0), [46] = (35, 0), [14] = (36, 28), [48] = (37, 28), [38] = (38, 31), [39] = (39, 15), [15] = (40, 31),
            [8] = (41, 31), [11] = (42, 20), [51] = (43, 28), [26] = (44, 7), [27] = (45, 7), [30] = (46, 7),
        };

        [TestMethod]
        public void TheRanjaGorgeBuildingsAreLitIndoors()
        {
            var (before, after) = Migrated();
            var hospital = new Vector3(-683f, 170.2f, -343f);
            var researchHq = new Vector3(-745f, 171.2f, -276f);

            Assert.AreEqual(9u, Shown(before, hospital, Env, underground: false), "the hospital had the gorge's foggy outdoor light");
            Assert.AreEqual(0u, Shown(before, hospital, Sky, underground: false), "and the map's sky");
            Assert.AreEqual(26u, Shown(after, hospital, Sky, underground: false), "the hospital's interior, as Daghda's Urn's");
            Assert.AreEqual(26u, Shown(after, hospital, Env, underground: false), "and its light");
            Assert.AreEqual(26u, Shown(after, new Vector3(-683f, 186.2f, -343f), Sky, underground: false), "upstairs too");
            Assert.AreEqual(30u, Shown(after, researchHq, Sky, underground: false), "the research HQ's interior");
            Assert.AreEqual(30u, Shown(after, new Vector3(-745f, 188.9f, -276f), Env, underground: false), "upstairs too");
            Assert.AreEqual(9u, Shown(after, new Vector3(-700f, 171.3f, -380f), Env, underground: false), "the gorge floor outside, as before");
        }

        [TestMethod]
        public void TheElohCorridorsHaveTheElohInterior()
        {
            var (before, after) = Migrated();
            var enigma = new Vector3(-829f, 141.7f, -738f);
            var daghda = new Vector3(-960f, 284.3f, 744f);
            var logos = new Vector3(832f, 160.5f, 960f);
            var gate = new Vector3(835f, 286.2f, 730f);

            Assert.AreEqual(36u, Shown(before, enigma, Sky, underground: true), "the Earth logos corridor had Enigma Cavern's cave sky");
            Assert.AreEqual(39u, Shown(after, enigma, Sky, underground: true), "the Eloh interior");
            Assert.AreEqual(36u, Shown(after, enigma, Name, underground: true), "still in Enigma Cavern");
            Assert.AreEqual(36u, Shown(after, new Vector3(-868f, 140.9f, -705f), Sky, underground: true), "the cave outside it, as before");
            Assert.AreEqual(0u, Shown(before, daghda, Sky, underground: true), "the Here logos corridor had none");
            Assert.AreEqual(39u, Shown(after, daghda, Sky, underground: true), "the Eloh interior");
            Assert.AreNotEqual(39u, Shown(after, new Vector3(-905f, 283.7f, 744f), Sky, underground: true), "not the cave outside it");
            Assert.AreEqual(0u, Shown(before, logos, Sky, underground: false), "the Enhance logos room had none");
            Assert.AreEqual(39u, Shown(after, logos, Sky, underground: false), "the logos rooms");
            Assert.AreEqual(39u, Shown(after, new Vector3(832f, -223.5f, 960f), Sky, underground: true), "down to the lowest");
            Assert.AreEqual(39u, Shown(after, gate, Sky, underground: true), "the corridor behind the Eloh gate");
            Assert.AreNotEqual(39u, Shown(after, new Vector3(835f, 285.9f, 687f), Sky, underground: true), "not Alia Caverns in front of it");
        }

        [TestMethod]
        public void TheTunnelsToTheLinksOutAreCaves()
        {
            var (before, after) = Migrated();
            var tunnels = new[]
            {
                new Vector3(-20f, 255.4f, 690f), new Vector3(-782f, 231.4f, 60f), new Vector3(-432f, 169.3f, 250f), new Vector3(-400f, 272.2f, -760f),
                new Vector3(-78f, 219.5f, -640f), new Vector3(898f, 273.5f, 37f), new Vector3(288f, 147.3f, -575f),
            };

            foreach (var at in tunnels)
            {
                Assert.AreEqual(0u, Shown(before, at, Sky, underground: true), $"{at} had the map's sky");
                Assert.AreEqual(1u, Shown(after, at, Sky, underground: true), $"{at} is a cave");
            }

            Assert.AreEqual(0u, Shown(after, new Vector3(-40f, 246.3f, 665f), Sky, underground: false), "the Guardian Prominence tunnel's mouth stays in the open");
            Assert.AreEqual(0u, Shown(after, new Vector3(283f, 149.1f, -555f), Sky, underground: false), "and the south Divide tunnel's");
            Assert.AreEqual(0u, Shown(after, new Vector3(-141f, 232.3f, -619f), Sky, underground: false), "and the open platform at the Manhattan link");
        }

        [TestMethod]
        public void AliaDassTentsAreLitIndoors()
        {
            var (before, after) = Migrated();
            var medic = new Vector3(787f, 294.5f, 366f);
            var tents = new (Vector3 At, uint Region)[]
            {
                (new Vector3(757f, 294.5f, 366f), 21), (new Vector3(756f, 294.6f, 382f), 21), (new Vector3(758f, 294.1f, 397f), 21), (new Vector3(758f, 294.5f, 407f), 21),
                (new Vector3(798f, 294.5f, 363f), 22), (new Vector3(864f, 294.3f, 388f), 23), (new Vector3(869f, 294.6f, 403f), 24),
            };

            Assert.AreEqual(0u, Shown(before, medic, Env, underground: false), "the medic tent had the open sky's light");
            Assert.AreEqual(25u, Shown(after, medic, Env, underground: false), "the hospital interior");
            Assert.AreEqual(50u, Shown(after, medic, Ambient, underground: false), "Alia Das's own ambience over it");
            Assert.AreEqual(50u, Shown(after, medic, Music, underground: false), "and music");
            Assert.AreEqual(50u, Shown(after, medic, Name, underground: false), "still Alia Das");

            foreach (var (at, region) in tents)
            {
                Assert.AreEqual(0u, Shown(before, at, Env, underground: false), $"{at} had the open sky's light");
                Assert.AreEqual(region, Shown(after, at, Env, underground: false), $"{at} is indoors");
            }

            Assert.AreEqual(0u, Shown(after, new Vector3(780f, 299.1f, 366f), Env, underground: false), "the ground between the tents stays outdoors");
        }

        [TestMethod]
        public void AliaDassForeanVillageSoundsLikeOne()
        {
            var (before, after) = Migrated();
            var huts = new Vector3(812f, 302.8f, 506f);
            var platform = new Vector3(760f, 302.2f, 440f);

            Assert.AreEqual(50u, Shown(before, huts, Ambient, underground: false), "the huts had the AFS field base's ambience");
            Assert.AreEqual(56u, Shown(after, huts, Ambient, underground: false), "the Forean village's");
            Assert.AreEqual(56u, Shown(after, platform, Ambient, underground: false), "on the platforms too");
            Assert.AreEqual(50u, Shown(after, huts, Name, underground: false), "still Alia Das");
            Assert.AreEqual(50u, Shown(after, new Vector3(787f, 294.5f, 366f), Ambient, underground: false), "the AFS tents keep the field base's");
        }

        [TestMethod]
        public void AliaCavernsShrineHasTheElohLight()
        {
            var (before, after) = Migrated();
            var shrine = new Vector3(835f, 269.7f, 663f);

            Assert.AreEqual(32u, Shown(before, shrine, Env, underground: true), "the shrine room had Alia Caverns' cave light");
            Assert.AreEqual(57u, Shown(after, shrine, Env, underground: true), "the Eloh light");
            Assert.AreEqual(57u, Shown(after, shrine, Sky, underground: true), "under the same cave sky");
            Assert.AreEqual(32u, Shown(after, shrine, Name, underground: true), "still Alia Caverns");
            Assert.AreEqual(32u, Shown(after, shrine, Ambient, underground: true), "with its water ambience");
            Assert.AreEqual(32u, Shown(after, new Vector3(835f, 286f, 663f), Env, underground: true), "not the upper level over it");
        }

        [TestMethod]
        public void DaghdasUrnsCaveReachesItsWestEnd()
        {
            var (before, after) = Migrated();
            var split = new Vector3(-905f, 283.7f, 744f);
            var ramp = new Vector3(-885f, 273.6f, 737f);

            Assert.AreEqual(0u, Shown(before, split, Sky, underground: true), "the west end had the map's sky");
            Assert.AreEqual(15u, Shown(after, split, Sky, underground: true), "Daghda's Urn's cave");
            Assert.AreEqual(15u, Shown(after, ramp, Name, underground: true), "the ramp down to it too");
            Assert.AreEqual(0u, Shown(after, new Vector3(-880f, 288.1f, 752f), Sky, underground: false), "the cave mouth stays in the open");
            Assert.AreEqual(39u, Shown(after, new Vector3(-960f, 284.3f, 744f), Sky, underground: true), "the Eloh corridor beyond keeps the Eloh interior");
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
            Assert.AreEqual(WildernessInteriorRegions.Volumes.Length, once.Except(before).Count(), "the new rows");

            foreach (var sql in WildernessInteriorRegions.Up())
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

            foreach (var sql in WildernessInteriorRegions.Up())
                StringAssert.Contains(migrator.GenerateScript(Previous, Migration), sql);
            foreach (var sql in WildernessInteriorRegions.Down())
                StringAssert.Contains(migrator.GenerateScript(Migration, Previous), sql);
        }

        /// <summary>The highest-priority region here that turns the setting on, 0 for none (the map's own).</summary>
        private static uint Shown(List<MapRegion> volumes, Vector3 at, int flag, bool underground) =>
            volumes.Where(v => v.Enabled && Wilderness.TryGetValue(v.RegionId, out var r) && (r.Flags & flag) != 0 && v.Contains(at, underground))
                .OrderByDescending(v => Wilderness[v.RegionId].Priority).Select(v => v.RegionId).FirstOrDefault();

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == WildernessInteriorRegions.MapContextId)
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT map_context_id || ' ' || region_id || ' ' || shape || ' ' || printf('%.1f %.1f %.1f %.1f %.1f %.1f %.1f %.1f', pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y) || ' ' || underground || ' ' || enabled || ' ' || comment AS Value " +
                    "FROM map_region WHERE map_context_id = 1220")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
