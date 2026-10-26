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

    /// <summary>The The Incurables Ward's Maintenance Corridors (Incurables_ward_maintenance_region, IncurablesWardMaintenanceRegion).</summary>
    [TestClass]
    [DoNotParallelize]
    public class IncurablesWardMaintenanceRegionTests
    {
        private const string Migration = "20270603000000_Incurables_ward_maintenance_region";
        private const string Previous = "20270602000000_Tantalus_command_regions";

        private const int Sky = 1, Env = 2, Ambient = 4, Music = 8, Name = 16;

        /// <summary>The ward's regions, as the .map has them: priority and the settings they turn on.</summary>
        private static readonly Dictionary<uint, (int Priority, int Flags)> Ward = new()
        {
            [3] = (1, 31), [4] = (2, 31), [5] = (3, 31), [6] = (4, 23), [7] = (5, 19), [2] = (6, 0),
        };

        [TestMethod]
        public void TheWestWingBehindTheThroneRoomIsTheMaintenanceCorridors()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();
            var tunnels = new Vector3(-234f, 86.6f, 222f);

            migrator.Migrate(Previous);
            Assert.AreEqual(0u, Shown(Volumes(database), tunnels, Name, underground: false), "the wing was in no region");

            migrator.Migrate(Migration);
            var after = Volumes(database);
            uint At(float x, float y, float z, int flag = Name) => Shown(after, new Vector3(x, y, z), flag, underground: false);

            Assert.AreEqual(6u, At(-234f, 86.6f, 222f), "at the Maintenance Tunnels label");
            Assert.AreEqual(6u, At(-234f, 86.6f, 222f, Ambient), "under the Brann corridors' ambient");
            Assert.AreEqual(6u, At(-366f, 86.6f, 212f), "at the water processing system");
            Assert.AreEqual(6u, At(-170f, 86f, 260f), "in the crosses by the throne room");
            Assert.AreEqual(6u, At(-120f, 86.7f, 270f), "in the airlock towards it");
            Assert.AreEqual(0u, At(-103f, 94.6f, 330f), "Jaikev's throne room is not in it");
            Assert.AreEqual(3u, At(-98f, 70.5f, 46f), "the Arsonist's Ward is as it was");
            Assert.AreEqual(7u, At(-18f, 62.6f, -100f), "and the Incurables Ward's entrance");
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
            Assert.AreEqual(IncurablesWardMaintenanceRegion.Volumes.Length, once.Except(before).Count());

            foreach (var sql in IncurablesWardMaintenanceRegion.Up())
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

            foreach (var sql in IncurablesWardMaintenanceRegion.Up())
                StringAssert.Contains(migrator.GenerateScript(Previous, Migration), sql);
            foreach (var sql in IncurablesWardMaintenanceRegion.Down())
                StringAssert.Contains(migrator.GenerateScript(Migration, Previous), sql);
        }

        /// <summary>The highest-priority region here that turns the setting on, 0 for none (the map's own).</summary>
        private static uint Shown(List<MapRegion> volumes, Vector3 at, int flag, bool underground) =>
            volumes.Where(v => v.Enabled && Ward.TryGetValue(v.RegionId, out var r) && (r.Flags & flag) != 0 && v.Contains(at, underground))
                .OrderByDescending(v => Ward[v.RegionId].Priority).Select(v => v.RegionId).FirstOrDefault();

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == IncurablesWardMaintenanceRegion.MapContextId)
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT map_context_id || ' ' || region_id || ' ' || shape || ' ' || printf('%.1f %.1f %.1f %.1f %.1f', pos_x, pos_z, radius, half_x, half_z) || ' ' || enabled AS Value " +
                    "FROM map_region")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
