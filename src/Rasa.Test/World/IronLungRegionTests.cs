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

    /// <summary>The Iron Lung, Shadow's Edge Post's tavern (Iron_lung_region, IronLungRegion).</summary>
    [TestClass]
    [DoNotParallelize]
    public class IronLungRegionTests
    {
        private const string Migration = "20270614000000_Iron_lung_region";
        private const string Previous = "20270613000000_Conscription_facility_regions";

        private const int Sky = 1, Env = 2, Ambient = 4, Music = 8, Name = 16;

        /// <summary>Ashen Desert's regions, as the .map has them: priority and the settings they turn on.</summary>
        private static readonly Dictionary<uint, (int Priority, int Flags)> Desert = new()
        {
            [1] = (1, 13), [11] = (2, 24), [14] = (3, 16), [20] = (4, 24), [15] = (5, 24), [16] = (6, 28), [12] = (7, 28), [13] = (8, 16), [4] = (9, 11),
            [5] = (10, 28), [23] = (11, 31), [17] = (12, 31), [3] = (13, 3), [2] = (14, 1), [6] = (15, 19), [8] = (16, 31), [9] = (17, 16), [19] = (18, 28),
            [10] = (19, 16), [18] = (20, 16), [21] = (21, 28), [22] = (22, 28), [24] = (23, 0),
        };

        [TestMethod]
        public void TheTavernIsTheIronLung()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();
            var bar = new Vector3(-175f, 296.9f, -831f);

            migrator.Migrate(Previous);
            Assert.AreEqual(0u, Shown(Volumes(database), bar, Name, underground: false), "nothing named the tavern");

            migrator.Migrate(Migration);
            var after = Volumes(database);
            foreach (var flag in new[] { Sky, Env, Ambient, Name })
                Assert.AreEqual(23u, Shown(after, bar, flag, underground: false), $"The Iron Lung ({flag})");
            Assert.AreEqual(23u, Shown(after, new Vector3(-185f, 297.5f, -838f), Name, underground: false), "at the kegs");
            Assert.AreEqual(23u, Shown(after, new Vector3(-165f, 296.5f, -825f), Name, underground: false), "at the speakers");
            Assert.AreEqual(5u, Shown(after, new Vector3(-150f, 297.2f, -780f), Name, underground: false), "the barracks next door are Shadow's Edge Post");
        }

        [TestMethod]
        public void TheUpRunsTwiceAndTheDownTakesItOut()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            var before = Snapshot(database);

            migrator.Migrate(Migration);
            var once = Snapshot(database);
            Assert.AreEqual(IronLungRegion.Volumes.Length, once.Except(before).Count(), "the new row");

            foreach (var sql in IronLungRegion.Up())
                Assert.AreEqual(0, database.Context.Database.ExecuteSqlRaw(sql.Replace("{", "{{").Replace("}", "}}")), sql);
            CollectionAssert.AreEqual(once, Snapshot(database), "twice");

            migrator.Migrate(Previous);
            CollectionAssert.AreEqual(before, Snapshot(database), "Down");
        }

        [TestMethod]
        public void OnMySqlTheRowGoesInAndComesOut()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();

            foreach (var sql in IronLungRegion.Up())
                StringAssert.Contains(migrator.GenerateScript(Previous, Migration), sql);
            foreach (var sql in IronLungRegion.Down())
                StringAssert.Contains(migrator.GenerateScript(Migration, Previous), sql);
        }

        /// <summary>The highest-priority region here that turns the setting on, 0 for none (the map's own).</summary>
        private static uint Shown(List<MapRegion> volumes, Vector3 at, int flag, bool underground) =>
            volumes.Where(v => v.Enabled && Desert.TryGetValue(v.RegionId, out var r) && (r.Flags & flag) != 0 && v.Contains(at, underground))
                .OrderByDescending(v => Desert[v.RegionId].Priority).Select(v => v.RegionId).FirstOrDefault();

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == IronLungRegion.MapContextId)
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT map_context_id || ' ' || region_id || ' ' || shape || ' ' || printf('%.1f %.1f %.1f %.1f %.1f %.1f %.1f %.1f', pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y) || ' ' || underground || ' ' || enabled || ' ' || comment AS Value " +
                    "FROM map_region")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
