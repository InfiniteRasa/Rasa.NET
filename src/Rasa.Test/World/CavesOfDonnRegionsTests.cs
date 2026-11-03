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
    /// The Caves of Donn given the epic copy's boxes, and the epic copy's caverns widened
    /// (Caves_of_donn_regions, CavesOfDonnRegions).
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class CavesOfDonnRegionsTests
    {
        private const string Migration = "20270703000000_Caves_of_donn_regions";
        private const string Previous = "20270702000000_Caves_of_donn_epic_regions";

        private const int Sky = 1, Name = 16;

        /// <summary>The Caves of Donn's regions, the same on both maps: priority and the settings they turn on.</summary>
        private static readonly Dictionary<uint, (int Priority, int Flags)> Donn = new()
        {
            [13] = (1, 0), [12] = (2, 0), [9] = (3, 6), [15] = (4, 11), [3] = (5, 18), [4] = (6, 27), [2] = (7, 7), [16] = (8, 15),
            [5] = (9, 18), [7] = (10, 23), [10] = (11, 7), [11] = (12, 0), [8] = (13, 15), [6] = (14, 30), [1] = (15, 13), [14] = (16, 13),
        };

        [TestMethod]
        public void TheCavernsUnderTheGroundHaveTheirNames()
        {
            var (before, after) = Migrated(CavesOfDonnRegions.Donn);
            var spirit = new Vector3(-220f, 5f, -140f);
            var chantry = new Vector3(-205f, 10.4f, -230f);
            var fairway = new Vector3(40f, 18.8f, -290f);

            Assert.AreEqual(0u, Shown(before, spirit, Name, underground: true), "the surface-only circle missed Spirit Cave's floor under the ground");
            Assert.AreEqual(7u, Shown(after, spirit, Name, underground: true), "Spirit Cave");
            Assert.AreEqual(7u, Shown(after, spirit, Sky, underground: true), "with its sky");
            Assert.AreEqual(0u, Shown(before, chantry, Name, underground: true), "the north of the Chantry cavern had no name");
            Assert.AreEqual(5u, Shown(after, chantry, Name, underground: true), "the Eloh Chantry");
            Assert.AreEqual(0u, Shown(before, fairway, Name, underground: true), "nor the Fairway under the ground");
            Assert.AreEqual(3u, Shown(after, fairway, Name, underground: true), "Undone Fairway");
            Assert.AreEqual(6u, Shown(after, new Vector3(-322f, 21.6f, -228f), Name, underground: false), "Mar Fend, as before");
            Assert.AreEqual(0u, Shown(before, new Vector3(-360f, 12.3f, -320f), Name, underground: false), "the south of the village was left out");
            Assert.AreEqual(6u, Shown(after, new Vector3(-360f, 12.3f, -320f), Name, underground: false), "and is Mar Fend's");
            Assert.AreEqual(7u, Shown(before, new Vector3(-170f, 36f, -205f), Name, underground: false), "Spirit Cave's circle ran into the Chantry cavern");
            Assert.AreEqual(5u, Shown(after, new Vector3(-170f, 36f, -205f), Name, underground: false), "which is the Chantry's");
        }

        [TestMethod]
        public void TheShrineTheElohRoomsAndTheTunnelsAreAsOnTheEpicCopy()
        {
            var (_, after) = Migrated(CavesOfDonnRegions.Donn);

            Assert.AreEqual(4u, Shown(after, new Vector3(160f, 4.8f, -302f), Name, underground: false), "the Vitiated Shrine");
            Assert.AreEqual(8u, Shown(after, new Vector3(278f, 5f, -302f), Sky, underground: true), "the hidden chamber");
            Assert.AreEqual(15u, Shown(after, new Vector3(-320f, 25.1f, -359f), Sky, underground: true), "the Eloh corridor by the entrance");
            Assert.AreEqual(2u, Shown(after, new Vector3(-290f, 10.1f, -105f), Sky, underground: true), "west cavern to Spirit Cave");
            Assert.AreEqual(2u, Shown(after, new Vector3(-200f, 11f, -205f), Sky, underground: true), "Spirit Cave to the Chantry");
            Assert.AreEqual(2u, Shown(after, new Vector3(-100f, 16.8f, -296f), Sky, underground: true), "the Chantry to Undone Fairway");
            Assert.AreEqual(16u, Shown(after, new Vector3(105f, 6.6f, -300f), Sky, underground: true), "Undone Fairway down to the shrine");
            Assert.AreEqual(7u, Shown(after, new Vector3(-236f, 5f, -140f), Name, underground: true), "Spirit Cave's west bay");
        }

        [TestMethod]
        public void TheEpicCopysCavernsReachTheirEdges()
        {
            var (before, after) = Migrated(CavesOfDonnRegions.Epic);

            Assert.AreEqual(0u, Shown(before, new Vector3(-236f, 5f, -140f), Name, underground: true), "Spirit Cave's west bay was left out");
            Assert.AreEqual(7u, Shown(after, new Vector3(-236f, 5f, -140f), Name, underground: true), "and is Spirit Cave's");
            Assert.AreEqual(0u, Shown(before, new Vector3(-168f, 11.4f, -170f), Name, underground: false), "and its east end");
            Assert.AreEqual(7u, Shown(after, new Vector3(-168f, 11.4f, -170f), Name, underground: false), "Spirit Cave");
            Assert.AreEqual(0u, Shown(before, new Vector3(-396f, 12.6f, -220f), Name, underground: false), "Mar Fend's west end was left out");
            Assert.AreEqual(6u, Shown(after, new Vector3(-396f, 12.6f, -220f), Name, underground: false), "Mar Fend Village");
            Assert.AreEqual(5u, Shown(after, new Vector3(-120f, 21.4f, -252f), Name, underground: false), "the Chantry to its east end");
        }

        private static (List<MapRegion> Before, List<MapRegion> After) Migrated(uint map)
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();
            migrator.Migrate(Previous);
            var before = Volumes(database, map);
            migrator.Migrate(Migration);
            return (before, Volumes(database, map));
        }

        [TestMethod]
        public void TheUpRunsTwiceAndTheDownPutsEverythingBack()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            var before = Snapshot(database);

            migrator.Migrate(Migration);
            var once = Snapshot(database);
            Assert.AreEqual(CavesOfDonnRegions.Volumes.Length + CavesOfDonnRegions.Recuts.Length + CavesOfDonnRegions.Resizes.Length, once.Except(before).Count(), "the new rows and the changed ones");

            foreach (var sql in CavesOfDonnRegions.Up())
                Assert.AreEqual(0, database.Context.Database.ExecuteSqlRaw(sql.Replace("{", "{{").Replace("}", "}}")), sql);
            CollectionAssert.AreEqual(once, Snapshot(database), "twice");

            migrator.Migrate(Previous);
            CollectionAssert.AreEqual(before, Snapshot(database), "Down");
        }

        [TestMethod]
        public void OnMySqlTheRowsChangeAndChangeBack()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();

            foreach (var sql in CavesOfDonnRegions.Up())
                StringAssert.Contains(migrator.GenerateScript(Previous, Migration), sql);
            foreach (var sql in CavesOfDonnRegions.Down())
                StringAssert.Contains(migrator.GenerateScript(Migration, Previous), sql);
        }

        /// <summary>The highest-priority region here that turns the setting on, 0 for none (the map's own).</summary>
        private static uint Shown(List<MapRegion> volumes, Vector3 at, int flag, bool underground) =>
            volumes.Where(v => v.Enabled && Donn.TryGetValue(v.RegionId, out var r) && (r.Flags & flag) != 0 && v.Contains(at, underground))
                .OrderByDescending(v => Donn[v.RegionId].Priority).Select(v => v.RegionId).FirstOrDefault();

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database, uint map) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == map)
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT map_context_id || ' ' || region_id || ' ' || shape || ' ' || printf('%.1f %.1f %.1f %.1f %.1f %.1f %.1f %.1f', pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y) || ' ' || underground || ' ' || enabled || ' ' || comment AS Value " +
                    "FROM map_region")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
