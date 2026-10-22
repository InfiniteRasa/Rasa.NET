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

    /// <summary>Indra Caverns' Brann storage facility and Bane plant rooms (Indra_caverns_regions, IndraCavernsRegions).</summary>
    [TestClass]
    [DoNotParallelize]
    public class IndraCavernsRegionsTests
    {
        private const string Migration = "20270605000000_Indra_caverns_regions";
        private const string Previous = "20270604000000_Brann_water_refinery_regions";

        private const int Sky = 1, Env = 2, Ambient = 4, Music = 8, Name = 16;

        /// <summary>Indra Caverns' regions, as the .map has them: priority and the settings they turn on.</summary>
        private static readonly Dictionary<uint, (int Priority, int Flags)> Indra = new()
        {
            [2] = (1, 13), [5] = (2, 19), [33] = (3, 0), [34] = (4, 0), [36] = (5, 0), [37] = (6, 0), [38] = (7, 0), [35] = (8, 0), [6] = (9, 23),
            [7] = (10, 23), [8] = (11, 23), [9] = (12, 23), [10] = (13, 23), [11] = (14, 23), [12] = (15, 23), [13] = (16, 23), [14] = (17, 23),
            [15] = (18, 23), [16] = (19, 23), [17] = (20, 23), [18] = (21, 23), [19] = (22, 31), [20] = (23, 31), [21] = (24, 31), [22] = (25, 31),
            [23] = (26, 31), [24] = (27, 31), [25] = (28, 31), [26] = (29, 31), [27] = (30, 23), [28] = (31, 31), [29] = (32, 31), [30] = (33, 31),
            [1] = (34, 23), [32] = (35, 31),
        };

        private static readonly uint[] Gotha = { 19, 20, 25, 26, 27, 28, 29, 30 };

        [TestMethod]
        public void TheGothaCirclesWereOverTheMountainAndBecomeThePlant()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();
            var pad = new Vector3(-359f, 173.6f, -288f);
            var ridge = new Vector3(-310f, 129f, -30f);
            var octagon = new Vector3(-156f, 127.8f, -344f);
            var alphaGas = new Vector3(-188f, 127.9f, -150f);

            migrator.Migrate(Previous);
            var before = Volumes(database);
            Assert.IsTrue(Gotha.Contains(Shown(before, pad, Sky, underground: false)), "the plant's interior sky was over the landing pad");
            Assert.IsTrue(Gotha.Contains(Shown(before, ridge, Sky, underground: false)), "and the ridge at its north door");
            Assert.AreEqual(0u, Shown(before, octagon, Sky, underground: true), "and nowhere in the plant");

            migrator.Migrate(Migration);
            var after = Volumes(database);
            Assert.AreEqual(21u, Shown(after, pad, Sky, underground: false), "the landing pad has its own outdoor sky");
            Assert.AreEqual(21u, Shown(after, pad, Name, underground: false));
            Assert.AreEqual(0u, Shown(after, ridge, Sky, underground: false), "the ridge the map's");
            Assert.AreEqual(19u, Shown(after, octagon, Sky, underground: true), "the guard octagon is the plant");
            Assert.AreEqual(19u, Shown(after, alphaGas, Name, underground: true), "and the Alpha Gas hall");
            Assert.AreEqual(1, after.Count(v => Gotha.Contains(v.RegionId) && v.Enabled), "one Gotha volume left on");
        }

        [TestMethod]
        public void TheBayItsGalleryTheBarracksAndTheTowerRoom()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Migration);
            var after = Volumes(database);
            uint At(float x, float y, float z, bool underground = true) => Shown(after, new Vector3(x, y, z), Name, underground);

            Assert.AreEqual(24u, At(-370f, 128.3f, -264f), "Juggernaut Bay's floor");
            Assert.AreEqual(24u, At(-310f, 128.3f, -240f), "its east side");
            Assert.AreEqual(24u, Shown(after, new Vector3(-370f, 128.3f, -264f), Sky, underground: true), "under the arena sky");
            Assert.AreEqual(32u, At(-400f, 137.8f, -264f), "Juggernaut Operations, the console gallery");
            Assert.AreEqual(32u, At(-397f, 137.8f, -230f), "its north end");
            Assert.AreEqual(32u, At(-397f, 137.8f, -296f), "its south end");
            Assert.AreEqual(23u, At(-95f, 137.2f, -360f), "the Barracks' east pod hall, on the catwalk");
            Assert.AreEqual(23u, At(-90f, 130f, -375f), "and its floor");
            Assert.AreEqual(23u, At(-172f, 137.2f, -400f), "the south pod hall");
            Assert.AreEqual(23u, At(-160f, 129.9f, -420f), "and its floor");
            Assert.AreEqual(22u, At(-364f, 167.9f, -432f, underground: false), "Bane Central Administration, the tower room");
            Assert.AreEqual(22u, At(-364f, 168.3f, -455f, underground: false), "at Kretas' console");
            Assert.AreEqual(19u, At(-440f, 137.8f, -264f), "Beta Gas Processing, west of the bay, is the plant");
        }

        [TestMethod]
        public void TheBrannFacilityItsCheckPointAndItsLabs()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();
            var quarter = new Vector3(-12f, 0.6f, -232f);

            migrator.Migrate(Previous);
            Assert.AreEqual(0u, Shown(Volumes(database), quarter, Name, underground: true), "the Maintenance Lab's box had one of its four quarters");

            migrator.Migrate(Migration);
            var after = Volumes(database);
            uint At(float x, float y, float z, bool underground = true) => Shown(after, new Vector3(x, y, z), Name, underground);

            Assert.AreEqual(7u, At(72.9f, 16.6f, -328f), "the Security Check Point at its label");
            Assert.AreEqual(7u, At(60f, 17f, -328f), "and the entrance");
            Assert.AreEqual(8u, At(76f, 4f, -330f), "the corridor under it is the facility");
            Assert.AreEqual(8u, At(124f, 8.6f, -300f), "the facility's corridors");
            Assert.AreEqual(8u, At(60f, 8.6f, -288f));
            Assert.AreEqual(8u, At(36f, -7.4f, -340f));
            Assert.AreEqual(15u, At(52f, -7.4f, -396f), "the Science Lab at its label");
            Assert.AreEqual(15u, At(30f, -4.9f, -415f), "and in its far quarter");
            Assert.AreEqual(16u, At(-12f, 0.6f, -232f), "the Maintenance Lab's quarter it was without");
            Assert.AreEqual(16u, At(-44f, 0.6f, -232f), "and another");
            Assert.AreEqual(14u, At(16.7f, 8.5f, -281.8f), "the Maintenance Dorm is as it was");
            Assert.AreEqual(9u, At(167.6f, 9.6f, -296f), "the Brann Sanctuary is as it was");
            Assert.AreEqual(5u, At(-40.7f, 9.4f, -421.6f), "the way in is the Watch Post still");
            Assert.AreEqual(6u, At(-18f, 9.6f, -333f, underground: false), "and the cavern at its label");
        }

        [TestMethod]
        public void TheCavernBehindTheFacilityAndTheTunnelWestAreTheCaverns()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();
            var cavern = new Vector3(100f, 22.9f, -150f);
            var tunnel = new Vector3(-150f, 55.2f, -120f);

            migrator.Migrate(Previous);
            var before = Volumes(database);
            Assert.AreEqual(0u, Shown(before, cavern, Sky, underground: false), "the cavern had the map's own sky");
            Assert.AreEqual(0u, Shown(before, tunnel, Sky, underground: true), "and the tunnel");

            migrator.Migrate(Migration);
            var after = Volumes(database);
            uint At(float x, float y, float z, bool underground) => Shown(after, new Vector3(x, y, z), Name, underground);

            foreach (var flag in new[] { Sky, Env, Ambient, Name })
                Assert.AreEqual(17u, Shown(after, cavern, flag, underground: false), $"the cavern behind the rear exit ({flag})");
            Assert.AreEqual(17u, At(50f, 31.9f, -100f, underground: false), "its north");
            Assert.AreEqual(17u, At(150f, 27f, -120f, underground: true), "by the smokestack, where the floor is mesh");
            Assert.AreEqual(17u, At(140f, 16.4f, -205f, underground: false), "outside the rear exit");
            Assert.AreEqual(17u, At(-45f, 60.1f, -110f, underground: false), "the cave mouth west");
            Assert.AreEqual(17u, Shown(after, tunnel, Sky, underground: true), "the tunnel");
            Assert.AreEqual(17u, At(-100f, 63.7f, -125f, underground: true), "its east");
            Assert.AreEqual(17u, At(-240f, 61f, -105f, underground: true), "its west");
            Assert.AreEqual(17u, At(-255f, 66.3f, -70f, underground: true), "its turn north to the ridge");
            Assert.AreEqual(16u, At(-44f, 0.6f, -200f, underground: true), "the Maintenance Lab beside the cavern is its own");
            Assert.AreEqual(12u, At(28f, 16f, -210f, underground: true), "and the Security Station");
            Assert.AreEqual(19u, At(-188f, 127.9f, -150f, underground: true), "the plant over the tunnel is the plant");
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
            Assert.AreEqual(IndraCavernsRegions.Volumes.Length + IndraCavernsRegions.Recuts.Length + IndraCavernsRegions.DisabledGothaRegions.Length,
                once.Except(before).Count(), "the new rows, the recut ones and the circles turned off");

            foreach (var sql in IndraCavernsRegions.Up())
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

            foreach (var sql in IndraCavernsRegions.Up())
                StringAssert.Contains(migrator.GenerateScript(Previous, Migration), sql);
            foreach (var sql in IndraCavernsRegions.Down())
                StringAssert.Contains(migrator.GenerateScript(Migration, Previous), sql);
        }

        /// <summary>The highest-priority region here that turns the setting on, 0 for none (the map's own).</summary>
        private static uint Shown(List<MapRegion> volumes, Vector3 at, int flag, bool underground) =>
            volumes.Where(v => v.Enabled && Indra.TryGetValue(v.RegionId, out var r) && (r.Flags & flag) != 0 && v.Contains(at, underground))
                .OrderByDescending(v => Indra[v.RegionId].Priority).Select(v => v.RegionId).FirstOrDefault();

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == IndraCavernsRegions.MapContextId)
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT map_context_id || ' ' || region_id || ' ' || shape || ' ' || printf('%.1f %.1f %.1f %.1f %.1f %.1f %.1f %.1f', pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y) || ' ' || underground || ' ' || enabled || ' ' || comment AS Value " +
                    "FROM map_region")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
