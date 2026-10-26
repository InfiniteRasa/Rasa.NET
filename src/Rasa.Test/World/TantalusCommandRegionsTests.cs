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

    /// <summary>The Tantalus High Command and the C.I.D. Headquarters (Tantalus_command_regions, TantalusCommandRegions).</summary>
    [TestClass]
    [DoNotParallelize]
    public class TantalusCommandRegionsTests
    {
        private const string Migration = "20270602000000_Tantalus_command_regions";
        private const string Previous = "20270601000000_Wilderness_urn_enigma_regions";

        private const int Sky = 1, Env = 2, Ambient = 4, Music = 8, Name = 16;

        /// <summary>Torden Abyss's regions, as the .map has them: priority and the settings they turn on.</summary>
        private static readonly Dictionary<uint, (int Priority, int Flags)> Abyss = new()
        {
            [1] = (1, 13), [5] = (2, 31), [13] = (3, 29), [14] = (4, 29), [16] = (5, 29), [6] = (6, 31), [44] = (7, 31), [33] = (8, 31), [34] = (9, 31),
            [28] = (10, 20), [29] = (11, 20), [18] = (12, 31), [22] = (13, 31), [23] = (14, 31), [24] = (15, 31), [25] = (16, 31), [26] = (17, 31),
            [40] = (18, 31), [41] = (19, 31), [42] = (20, 31), [45] = (21, 29), [35] = (22, 25), [37] = (23, 17), [36] = (24, 17), [43] = (25, 31),
            [20] = (26, 29), [21] = (27, 7), [17] = (28, 31), [31] = (29, 24), [32] = (30, 24), [38] = (31, 0), [39] = (32, 19), [15] = (33, 31),
        };

        [TestMethod]
        public void TheOfficeGroundFloorIsTheCidAndItsUpperFloorHighCommand()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();
            var ground = new Vector3(-430f, 536.1f, -300f);
            var upper = new Vector3(-430f, 544.9f, -280f);

            migrator.Migrate(Previous);
            Assert.AreEqual(44u, Shown(Volumes(database), ground, Name, underground: false), "the office was Tantalus Base's");

            migrator.Migrate(Migration);
            var after = Volumes(database);

            Assert.AreEqual(29u, Shown(after, ground, Name, underground: false), "on the ground floor, by the C.I.D. sign");
            Assert.AreEqual(29u, Shown(after, new Vector3(-436f, 537.9f, -284f), Ambient, underground: false), "under the command ambient");
            Assert.AreEqual(28u, Shown(after, upper, Name, underground: false), "on the upper floor, by the Tantalus Command sign");
            Assert.AreEqual(28u, Shown(after, new Vector3(-435f, 544.1f, -300f), Name, underground: false), "at the base flag");
            Assert.AreEqual(44u, Shown(after, new Vector3(-397f, 538f, -224f), Name, underground: false), "the dropship yard is Tantalus Base's still");
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
            Assert.AreEqual(TantalusCommandRegions.Volumes.Length, once.Except(before).Count());

            foreach (var sql in TantalusCommandRegions.Up())
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

            foreach (var sql in TantalusCommandRegions.Up())
                StringAssert.Contains(migrator.GenerateScript(Previous, Migration), sql);
            foreach (var sql in TantalusCommandRegions.Down())
                StringAssert.Contains(migrator.GenerateScript(Migration, Previous), sql);
        }

        /// <summary>The highest-priority region here that turns the setting on, 0 for none (the map's own).</summary>
        private static uint Shown(List<MapRegion> volumes, Vector3 at, int flag, bool underground) =>
            volumes.Where(v => v.Enabled && Abyss.TryGetValue(v.RegionId, out var r) && (r.Flags & flag) != 0 && v.Contains(at, underground))
                .OrderByDescending(v => Abyss[v.RegionId].Priority).Select(v => v.RegionId).FirstOrDefault();

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == TantalusCommandRegions.MapContextId)
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT map_context_id || ' ' || region_id || ' ' || shape || ' ' || printf('%.1f %.1f %.1f %.1f %.1f', pos_x, pos_z, radius, half_x, half_z) || ' ' || enabled AS Value " +
                    "FROM map_region")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
