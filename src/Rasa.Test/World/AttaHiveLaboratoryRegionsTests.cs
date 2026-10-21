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

    /// <summary>The Avernus Outpost's Atta Hives and the Conscription Facility's laboratory, maintenance and processing areas (Atta_hive_laboratory_regions, AttaHiveLaboratoryRegions).</summary>
    [TestClass]
    [DoNotParallelize]
    public class AttaHiveLaboratoryRegionsTests
    {
        private const string Migration = "20270219000000_Atta_hive_laboratory_regions";
        private const string Previous = "20270218000000_Irendas_penal_colony_regions";

        private const int Sky = 1, Env = 2, Ambient = 4, Music = 8, Name = 16;

        /// <summary>Avernus Outpost's regions, as the .map has them: priority and the settings they turn on.</summary>
        private static readonly Dictionary<uint, (int Priority, int Flags)> Avernus = new()
        {
            [1] = (1, 13), [6] = (2, 0), [9] = (3, 0), [7] = (4, 0), [8] = (5, 0), [2] = (6, 27), [10] = (7, 31), [11] = (8, 31), [12] = (9, 27),
            [13] = (10, 31), [5] = (11, 31), [3] = (12, 3), [4] = (13, 3),
        };

        /// <summary>The Conscription Facility's.</summary>
        private static readonly Dictionary<uint, (int Priority, int Flags)> Conscription = new()
        {
            [36] = (1, 1), [35] = (2, 23), [34] = (3, 0), [33] = (4, 0), [32] = (5, 0), [17] = (6, 20), [1] = (7, 1), [28] = (8, 23), [22] = (9, 23),
            [7] = (10, 23), [29] = (11, 23), [24] = (12, 23), [25] = (13, 23), [23] = (14, 23), [26] = (15, 23), [27] = (16, 23), [2] = (17, 23),
            [30] = (18, 23), [3] = (19, 23), [31] = (20, 23), [13] = (21, 23), [14] = (22, 23), [18] = (23, 23), [19] = (24, 23), [9] = (25, 23),
            [10] = (26, 23),
        };

        [TestMethod]
        public void TheTwoUnlabelledCavernsAreTheAttaHives()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();
            var upper = new Vector3(250f, 216f, -330f);
            var east = new Vector3(646f, 269f, -540f);

            migrator.Migrate(Previous);
            var before = Volumes(database, AttaHiveLaboratoryRegions.AvernusMapContextId);
            Assert.IsFalse(before.Any(v => v.RegionId is 2 or 13), "neither had a volume");
            Assert.AreEqual(10u, Shown(before, Avernus, new Vector3(250f, 216f, -400f), Name, underground: false), "the Queen's Chamber's circle was over the upper hive");
            Assert.AreEqual(5u, Shown(before, Avernus, east, Name, underground: false), "and the Eloh Sanctuary's over the small one");

            migrator.Migrate(Migration);
            var after = Volumes(database, AttaHiveLaboratoryRegions.AvernusMapContextId);

            foreach (var underground in new[] { false, true })
            {
                Assert.AreEqual(2u, Shown(after, Avernus, upper, Name, underground), "among the upper hive's obelisks");
                Assert.AreEqual(2u, Shown(after, Avernus, upper, Sky, underground), "under its blueV02 sky");
                Assert.AreEqual(2u, Shown(after, Avernus, new Vector3(250f, 216f, -400f), Name, underground), "its south, where the Queen's Chamber's circle was");
                Assert.AreEqual(2u, Shown(after, Avernus, new Vector3(380f, 226f, -280f), Name, underground), "its east, where Food Storage's was");
                Assert.AreEqual(13u, Shown(after, Avernus, east, Name, underground), "the cavern at the Eloh door");
                Assert.AreEqual(13u, Shown(after, Avernus, east, Ambient, underground));

                Assert.AreEqual(5u, Shown(after, Avernus, new Vector3(628f, 276f, -660f), Name, underground), "the Eloh temple past the door");
                Assert.AreEqual(10u, Shown(after, Avernus, new Vector3(281f, 171f, -588f), Name, underground), "the Queen's Chamber");
                Assert.AreEqual(12u, Shown(after, Avernus, new Vector3(556f, 377f, -296f), Name, underground), "Food Storage");
                Assert.AreEqual(11u, Shown(after, Avernus, new Vector3(909f, 274f, -246f), Name, underground: false), "the AFS Graveyard is as it was");
                Assert.AreEqual(0u, Shown(after, Avernus, new Vector3(200f, 159f, -425f), Name, underground), "the tunnel down from the hive to the Queen's Chamber");
            }
        }

        [TestMethod]
        public void TheLaboratoryRoomHasItsOwnSkyAndTheOverlooksTheDeserts()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();
            var lab = new Vector3(-250f, 97f, -145f);

            migrator.Migrate(Previous);
            var before = Volumes(database, AttaHiveLaboratoryRegions.ConscriptionMapContextId);
            Assert.AreEqual(0u, Shown(before, Conscription, lab, Sky, underground: true), "the label circle is surface only and the facility under the terrain: nothing was there");
            Assert.AreEqual(30u, Shown(before, Conscription, lab, Sky, underground: false), "and had it applied, the desert sky would have been over the whole room");

            migrator.Migrate(Migration);
            var after = Volumes(database, AttaHiveLaboratoryRegions.ConscriptionMapContextId);

            foreach (var flag in new[] { Sky, Env, Ambient, Name })
                Assert.AreEqual(2u, Shown(after, Conscription, lab, flag, underground: true), $"on the laboratory floor ({flag})");
            Assert.AreEqual(30u, Shown(after, Conscription, new Vector3(-175f, 106f, -145f), Sky, underground: true), "at its overlook's window");
            Assert.AreEqual(3u, Shown(after, Conscription, new Vector3(250f, 81f, -145f), Sky, underground: true), "the observatory floor under its own sky as well");
            Assert.AreEqual(31u, Shown(after, Conscription, new Vector3(175f, 106f, -145f), Sky, underground: true), "and the desert's at its overlook");
        }

        [TestMethod]
        public void TheServiceTunnelsAreMaintenanceAndTheChunnelsUnderThemProcessing()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Migration);
            var after = Volumes(database, AttaHiveLaboratoryRegions.ConscriptionMapContextId);
            uint At(float x, float y, float z) => Shown(after, Conscription, new Vector3(x, y, z), Name, underground: true);

            Assert.AreEqual(13u, At(-162f, 89f, -250f), "Maintenance - West's service room");
            Assert.AreEqual(13u, At(-176f, 97f, -300f), "its upper branch");
            Assert.AreEqual(13u, At(-186f, 105f, -375f), "its south end");
            Assert.AreEqual(14u, At(167f, 89f, -250f), "Maintenance - East's service room");
            Assert.AreEqual(14u, At(185f, 97f, -300f), "its upper branch");
            Assert.AreEqual(19u, At(239f, 81f, -90f), "Maintenance - North's service room");
            Assert.AreEqual(19u, At(200f, 89f, -40f), "its tee");
            Assert.AreEqual(19u, At(183f, 102f, -75f), "its turn west");
            Assert.AreEqual(18u, At(-60f, 65f, -481f), "Maintenance - South's service room");
            Assert.AreEqual(18u, At(-40f, 65f, -497f), "its long room");
            Assert.AreEqual(18u, At(-95f, 65f, -470f), "its tee");

            Assert.AreEqual(25u, At(-217f, 66f, -337f), "Processing - West's hall");
            Assert.AreEqual(25u, At(-125f, 66f, -395f), "its corridor south");
            Assert.AreEqual(24u, At(160f, 63f, -330f), "Processing - East's hall");
            Assert.AreNotEqual(25u, At(-233f, 98f, -290f), "the cell block level over Processing - West");
            Assert.AreEqual(23u, At(7f, 96f, -465f), "Sentient Conditioning is as it was");
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
            Assert.AreEqual(AttaHiveLaboratoryRegions.Volumes.Length + AttaHiveLaboratoryRegions.Recuts.Length, once.Except(before).Count(), "the new rows and the recut ones");

            foreach (var sql in AttaHiveLaboratoryRegions.Up())
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

            foreach (var sql in AttaHiveLaboratoryRegions.Up())
                StringAssert.Contains(migrator.GenerateScript(Previous, Migration), sql);
            foreach (var sql in AttaHiveLaboratoryRegions.Down())
                StringAssert.Contains(migrator.GenerateScript(Migration, Previous), sql);
        }

        /// <summary>The highest-priority region here that turns the setting on, 0 for none (the map's own).</summary>
        private static uint Shown(List<MapRegion> volumes, Dictionary<uint, (int Priority, int Flags)> regions, Vector3 at, int flag, bool underground) =>
            volumes.Where(v => v.Enabled && regions.TryGetValue(v.RegionId, out var r) && (r.Flags & flag) != 0 && v.Contains(at, underground))
                .OrderByDescending(v => regions[v.RegionId].Priority).Select(v => v.RegionId).FirstOrDefault();

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database, uint mapContextId) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == mapContextId)
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT map_context_id || ' ' || region_id || ' ' || shape || ' ' || printf('%.1f %.1f %.1f %.1f %.1f', pos_x, pos_z, radius, half_x, half_z) || ' ' || enabled AS Value " +
                    "FROM map_region")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
