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

    /// <summary>The Eir Crater Field Hospital and the Pirolja Midja Terrorist Base (Eir_crater_pirolja_midja_regions, EirCraterPiroljaMidjaRegions).</summary>
    [TestClass]
    [DoNotParallelize]
    public class EirCraterPiroljaMidjaRegionsTests
    {
        private const string Migration = "20270308000000_Eir_crater_pirolja_midja_regions";
        private const string Previous = "20270307000000_Wilderness_urn_enigma_regions";

        private const int Sky = 1, Ambient = 4, Music = 8, Name = 16;

        /// <summary>Torden Plains' regions, as the .map has them: priority and the settings they turn on.</summary>
        private static readonly Dictionary<uint, (int Priority, int Flags)> Plains = new()
        {
            [1] = (1, 13), [8] = (2, 5), [4] = (3, 29), [14] = (4, 21), [20] = (5, 29), [15] = (6, 17), [17] = (7, 16), [26] = (8, 17), [19] = (9, 17),
            [21] = (10, 31), [18] = (11, 25), [12] = (12, 7), [2] = (13, 29), [3] = (14, 21), [7] = (15, 31), [13] = (16, 29), [5] = (17, 21), [32] = (18, 3),
            [25] = (19, 22), [6] = (20, 23), [9] = (21, 31), [22] = (22, 0), [23] = (23, 0), [24] = (24, 0), [10] = (25, 17), [11] = (26, 17), [16] = (27, 1),
            [31] = (28, 16), [27] = (29, 16), [29] = (30, 16), [34] = (31, 16), [33] = (32, 0),
        };

        /// <summary>Torden Incline's.</summary>
        private static readonly Dictionary<uint, (int Priority, int Flags)> Incline = new()
        {
            [1] = (1, 13), [4] = (2, 13), [5] = (3, 17), [6] = (4, 7), [7] = (5, 7), [8] = (6, 7), [3] = (7, 29), [9] = (8, 20),
            [2] = (9, 11), [13] = (10, 24), [11] = (11, 16), [15] = (12, 17), [16] = (13, 24), [17] = (14, 16), [18] = (15, 16),
            [19] = (16, 16), [20] = (17, 16), [21] = (18, 24), [22] = (19, 3), [26] = (20, 15), [24] = (21, 3), [23] = (22, 16),
            [12] = (23, 15), [27] = (24, 1), [25] = (25, 16), [14] = (26, 16), [28] = (27, 0),
        };

        [TestMethod]
        public void TheBunkerIsTheFieldHospital()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();
            var revive = new Vector3(690.3f, 413.7f, 235.9f);

            migrator.Migrate(Previous);
            Assert.AreEqual(0u, Shown(Volumes(database, EirCraterPiroljaMidjaRegions.PlainsMapContextId), Plains, revive, Name, underground: true), "the bunker was in no region");

            migrator.Migrate(Migration);
            var after = Volumes(database, EirCraterPiroljaMidjaRegions.PlainsMapContextId);

            foreach (var flag in new[] { Sky, Ambient, Music, Name })
                Assert.AreEqual(21u, Shown(after, Plains, revive, flag, underground: true), $"at the hospital's revive point ({flag})");
            Assert.AreEqual(21u, Shown(after, Plains, new Vector3(715f, 414f, 236f), Name, underground: true), "at its door");
            Assert.AreEqual(19u, Shown(after, Plains, new Vector3(743.7f, 412.7f, 236.2f), Name, underground: false), "Eir Crater Outpost is as it was");
        }

        [TestMethod]
        public void TheBrannCampNorthEastOfOrthoIsTheTerroristBase()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Migration);
            var after = Volumes(database, EirCraterPiroljaMidjaRegions.InclineMapContextId);

            Assert.AreEqual(20u, Shown(after, Incline, new Vector3(903f, 310.2f, 521f), Name, underground: false), "among the tents");
            Assert.AreEqual(20u, Shown(after, Incline, new Vector3(950f, 310.2f, 483f), Name, underground: false), "at the lockers and beer kegs");
            Assert.AreEqual(20u, Shown(after, Incline, new Vector3(890f, 311.2f, 470f), Name, underground: false), "at the wrecked outpost's fences");
            Assert.AreNotEqual(20u, Shown(after, Incline, new Vector3(540.6f, 291.8f, 61f), Name, underground: false), "the Labbna Syndicate Camp is not it");
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
            Assert.AreEqual(EirCraterPiroljaMidjaRegions.Volumes.Length, once.Except(before).Count());

            foreach (var sql in EirCraterPiroljaMidjaRegions.Up())
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

            foreach (var sql in EirCraterPiroljaMidjaRegions.Up())
                StringAssert.Contains(migrator.GenerateScript(Previous, Migration), sql);
            foreach (var sql in EirCraterPiroljaMidjaRegions.Down())
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
