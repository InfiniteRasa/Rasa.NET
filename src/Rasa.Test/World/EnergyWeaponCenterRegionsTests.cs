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

    /// <summary>The Energy Weapon Center's rooms (Energy_weapon_center_regions, EnergyWeaponCenterRegions).</summary>
    [TestClass]
    [DoNotParallelize]
    public class EnergyWeaponCenterRegionsTests
    {
        private const string Migration = "20270610000000_Energy_weapon_center_regions";
        private const string Previous = "20270609000000_Warnet_caverns_regions";

        private const int Sky = 1, Env = 2, Ambient = 4, Music = 8, Name = 16;

        /// <summary>The center's regions, as the .map has them: priority and the settings they turn on.</summary>
        private static readonly Dictionary<uint, (int Priority, int Flags)> Center = new()
        {
            [3] = (1, 16), [4] = (2, 16), [5] = (3, 16), [6] = (4, 16), [2] = (5, 1), [1] = (6, 13),
        };

        [TestMethod]
        public void TheTubesTheWarehouseTheControlRoomAndTheLab()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            Assert.AreEqual(0, Volumes(database).Count, "no region of the center had a volume");

            migrator.Migrate(Migration);
            var after = Volumes(database);
            uint At(float x, float y, float z) => Shown(after, new Vector3(x, y, z), Name, underground: true);

            Assert.AreEqual(3u, At(192f, 100.5f, -24f), "Reactor Tubes, the north tube");
            Assert.AreEqual(3u, At(192f, 100.5f, -72f), "the south tube");
            Assert.AreEqual(3u, At(200f, 97.5f, -48f), "the service rooms between them");
            Assert.AreEqual(5u, At(192f, 96.9f, 88f), "the Warehouse");
            Assert.AreEqual(4u, At(296f, 97.3f, -48f), "the Tesla Control Room");
            Assert.AreEqual(6u, At(88f, 95.8f, 0f), "the Research Lab");
            Assert.AreEqual(6u, At(100f, 105.3f, 60f), "its north gallery");
            Assert.AreEqual(0u, At(296f, 95.7f, 88f), "the empty north control room is the map's");
            Assert.AreEqual(0u, At(320f, 97.7f, 0f), "and the tunnel by the way in");
        }

        [TestMethod]
        public void TheYardOutsideTheWindowIsUnderTheOpenSky()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Migration);
            var after = Volumes(database);

            Assert.AreEqual(2u, Shown(after, new Vector3(-40f, 92.7f, 0f), Sky, underground: false), "the yard's trenches");
            Assert.AreEqual(2u, Shown(after, new Vector3(-40f, 101.3f, 20f), Sky, underground: false), "its catwalks");
            Assert.AreEqual(0u, Shown(after, new Vector3(-40f, 92.7f, 0f), Name, underground: false), "with no name of its own");
            Assert.AreEqual(0u, Shown(after, new Vector3(88f, 95.8f, 0f), Sky, underground: true), "the lab inside keeps the map's Bane interior sky");
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
            Assert.AreEqual(EnergyWeaponCenterRegions.Volumes.Length, once.Except(before).Count(), "the new rows");

            foreach (var sql in EnergyWeaponCenterRegions.Up())
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

            foreach (var sql in EnergyWeaponCenterRegions.Up())
                StringAssert.Contains(migrator.GenerateScript(Previous, Migration), sql);
            foreach (var sql in EnergyWeaponCenterRegions.Down())
                StringAssert.Contains(migrator.GenerateScript(Migration, Previous), sql);
        }

        /// <summary>The highest-priority region here that turns the setting on, 0 for none (the map's own).</summary>
        private static uint Shown(List<MapRegion> volumes, Vector3 at, int flag, bool underground) =>
            volumes.Where(v => v.Enabled && Center.TryGetValue(v.RegionId, out var r) && (r.Flags & flag) != 0 && v.Contains(at, underground))
                .OrderByDescending(v => Center[v.RegionId].Priority).Select(v => v.RegionId).FirstOrDefault();

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == EnergyWeaponCenterRegions.MapContextId)
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT map_context_id || ' ' || region_id || ' ' || shape || ' ' || printf('%.1f %.1f %.1f %.1f %.1f %.1f %.1f %.1f', pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y) || ' ' || underground || ' ' || enabled || ' ' || comment AS Value " +
                    "FROM map_region")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
