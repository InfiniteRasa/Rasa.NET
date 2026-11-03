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

    /// <summary>The drop below Torden Mires' southern cliffs (Torden_mires_dark_fall_region, TordenMiresDarkFallRegion).</summary>
    [TestClass]
    [DoNotParallelize]
    public class TordenMiresDarkFallRegionTests
    {
        private const string Migration = "20270708000000_Torden_mires_dark_fall_region";
        private const string Previous = "20270704000000_Guardian_prominence_regions";

        private const int Sky = 1, Name = 16;

        /// <summary>Torden Mires' regions, as the .map has them: priority and the settings they turn on.</summary>
        private static readonly Dictionary<uint, (int Priority, int Flags)> Mires = new()
        {
            [1] = (1, 13), [10] = (2, 29), [12] = (3, 28), [17] = (4, 20), [13] = (5, 16), [15] = (6, 28), [16] = (7, 28), [14] = (8, 28),
            [5] = (9, 1), [21] = (10, 15), [22] = (11, 15), [4] = (12, 31), [23] = (13, 24), [9] = (14, 4), [8] = (15, 23), [3] = (16, 1),
            [6] = (17, 1), [18] = (18, 31), [20] = (19, 16), [19] = (20, 16),
        };

        [TestMethod]
        public void BelowTheCliffsIsTheDarkFallingSky()
        {
            var (before, after) = Migrated();
            var floor = new Vector3(0f, 8.5f, -900f);

            Assert.AreEqual(0u, Shown(before, floor, Sky, underground: false), "the abyss floor had the Mires' sky");
            Assert.AreEqual(5u, Shown(after, floor, Sky, underground: false), "the dark falling sky");
            Assert.AreEqual(5u, Shown(after, new Vector3(-800f, 3.6f, -620f), Sky, underground: false), "under Baylor Base's cliffs");
            Assert.AreEqual(5u, Shown(after, new Vector3(553f, 145.8f, -400f), Sky, underground: true), "on a rock pillar under Iapyx");
            Assert.AreEqual(5u, Shown(after, new Vector3(0f, 120f, -820f), Sky, underground: false), "falling past the clouds");
            Assert.AreEqual(0u, Shown(after, new Vector3(-195.1f, 223.4f, -716.7f), Sky, underground: false), "not the cliff top");
            Assert.AreEqual(0u, Shown(after, new Vector3(153.5f, 188.6f, -715.8f), Sky, underground: false), "nor its lowest stretch");
            Assert.AreEqual(0u, Shown(after, new Vector3(-805f, 251.1f, -457.2f), Sky, underground: false), "nor the edge by Baylor Base");
            Assert.AreEqual(10u, Shown(after, new Vector3(57.3f, 184.6f, 127.4f), Sky, underground: false), "the Orsa basin keeps its close fog");
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
            Assert.AreEqual(1, once.Except(before).Count(), "the new row");

            foreach (var sql in TordenMiresDarkFallRegion.Up())
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

            foreach (var sql in TordenMiresDarkFallRegion.Up())
                StringAssert.Contains(migrator.GenerateScript(Previous, Migration), sql);
            foreach (var sql in TordenMiresDarkFallRegion.Down())
                StringAssert.Contains(migrator.GenerateScript(Migration, Previous), sql);
        }

        /// <summary>The highest-priority region here that turns the setting on, 0 for none (the map's own).</summary>
        private static uint Shown(List<MapRegion> volumes, Vector3 at, int flag, bool underground) =>
            volumes.Where(v => v.Enabled && Mires.TryGetValue(v.RegionId, out var r) && (r.Flags & flag) != 0 && v.Contains(at, underground))
                .OrderByDescending(v => Mires[v.RegionId].Priority).Select(v => v.RegionId).FirstOrDefault();

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == TordenMiresDarkFallRegion.MapContextId)
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT map_context_id || ' ' || region_id || ' ' || shape || ' ' || printf('%.1f %.1f %.1f %.1f %.1f %.1f %.1f %.1f', pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y) || ' ' || underground || ' ' || enabled || ' ' || comment AS Value " +
                    "FROM map_region WHERE map_context_id = 1759")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
