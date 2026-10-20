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

    /// <summary>The Proving Grounds' outdoors under the Caldera's sky (Bootcamp_caldera_sky, BootcampCalderaSky).</summary>
    [TestClass]
    [DoNotParallelize]
    public class BootcampCalderaSkyTests
    {
        private const string Migration = "20270115000000_Bootcamp_caldera_sky";
        private const string Previous = "20270113000000_Wilderness_reward_consumables";

        /// <summary>adv_bootcamp.map's regions: priority, and the sky the client draws for each.</summary>
        private static readonly Dictionary<uint, (int Priority, string Sky)> Regions = new()
        {
            [0] = (0, "sky_foreas_bootcamp_lunacave"),
            [1] = (1, "Molder"),
            [2] = (2, "sky_foreas_cave_bigroom"),
            [10] = (3, "sky_foreas_bootcamp_d11"),
            [9] = (4, "sky_foreas_bootcamp_d11"),
            [4] = (5, "sky_foreas_bootcamp_d11"),
            [5] = (6, "sky_foreas_general_human_interior"),
        };

        private const string Daylight = "sky_foreas_bootcamp_d11";

        [TestMethod]
        public void TheOutdoorsHasTheCalderasDaylightAndTheCavernKeepsItsOwn()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            // Before: dark wherever one stands outside.
            migrator.Migrate(Previous);
            var before = Volumes(database);
            Assert.AreEqual("sky_foreas_general_human_interior", SkyAt(before, new Vector3(112.5312f, 109.4336f, 111.2734f)), "Medic Schramm at the fort");
            Assert.AreEqual("sky_foreas_bootcamp_lunacave", SkyAt(before, new Vector3(-223f, 101.27f, -76f)), "Van Valkenberg at the crash site");

            migrator.Migrate(Migration);
            var after = Volumes(database);

            // The outdoors: the fort, the lakes, the crash site, the Caldera's label.
            foreach (var (where, position) in new[]
                     {
                         ("Medic Schramm at the fort", new Vector3(112.5312f, 109.4336f, 111.2734f)),
                         ("Van Valkenberg at the crash site", new Vector3(-223f, 101.27f, -76f)),
                         ("the Caldera's label", new Vector3(8.8f, 120.7f, 245.8f)),
                         ("the west edge of the map", new Vector3(-500f, 110f, 0f)),
                         ("just short of the Eloh cavern entrance", new Vector3(200f, 112f, 96f)),
                     })
                Assert.AreEqual(Daylight, SkyAt(after, position), where);

            // The cavern: the start, the firing range, the front behind the cavern entrance.
            foreach (var (where, position) in new[]
                     {
                         ("the start", new Vector3(389.8047f, 136.7852f, -80.6641f)),
                         ("the firing range", new Vector3(384.7f, 119.4f, 186.8f)),
                         ("the front's Rifleman", new Vector3(297.2773f, 120.5312f, 68.6289f)),
                         ("between the two entrances", new Vector3(250f, 120f, 80f)),
                     })
                StringAssert.Contains(SkyAt(after, position), "cave", where);

            // Under the ground the cavern's sky, outdoors or not.
            Assert.AreEqual("sky_foreas_bootcamp_lunacave", SkyAt(after, new Vector3(112.5312f, 109.4336f, 111.2734f), underground: true));

            var interior = after.Single(region => region.RegionId == BootcampCalderaSky.InteriorRegion);
            Assert.IsFalse(interior.Enabled, "the interior sky is a game master's to place");
        }

        [TestMethod]
        public void TheUpRunsTwiceAndTheDownPutsTheCirclesBack()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            var before = Snapshot(database);

            migrator.Migrate(Migration);
            var once = Snapshot(database);
            CollectionAssert.AreNotEqual(before, once);

            foreach (var sql in BootcampCalderaSky.Up())
                database.Context.Database.ExecuteSqlRaw(sql.Replace("{", "{{").Replace("}", "}}"));
            CollectionAssert.AreEqual(once, Snapshot(database), "twice");

            migrator.Migrate(Previous);
            CollectionAssert.AreEqual(before, Snapshot(database), "Down puts the seed back");
        }

        [TestMethod]
        public void AVolumeAGameMasterMovedIsLeftAlone()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            database.Context.Database.ExecuteSqlRaw("UPDATE map_region SET pos_x = 20 WHERE map_context_id = 1985 AND region_id IN (4, 5);");
            var moved = Snapshot(database);

            migrator.Migrate(Migration);
            CollectionAssert.AreEqual(moved, Snapshot(database));
        }

        [TestMethod]
        public void OnMySqlTheRowsChangeAndChangeBack()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(Previous, Migration);
            var down = migrator.GenerateScript(Migration, Previous);

            foreach (var sql in BootcampCalderaSky.Up())
                StringAssert.Contains(up, sql);
            StringAssert.Contains(up, $"'{Migration}'");
            foreach (var sql in BootcampCalderaSky.Down())
                StringAssert.Contains(down, sql);
        }

        /// <summary>The sky the client draws: the highest-priority region the server would send, or region 0's (regionmanager.py).</summary>
        private static string SkyAt(List<MapRegion> volumes, Vector3 position, bool underground = false)
        {
            var sent = volumes.Where(volume => volume.Enabled && volume.Contains(position, underground))
                .Select(volume => volume.RegionId).Distinct().Append(0u);

            return Regions[sent.OrderByDescending(id => Regions[id].Priority).First()].Sky;
        }

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == BootcampCalderaSky.BootcampMapContextId)
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT region_id || ' ' || shape || ' ' || printf('%.2f %.2f %.2f %.2f %.2f', pos_x, pos_z, radius, half_x, half_z) || ' ' || " +
                    "underground || ' ' || enabled || ' ' || comment AS Value FROM map_region WHERE map_context_id = 1985")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
