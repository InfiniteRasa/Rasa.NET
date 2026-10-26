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

    /// <summary>Treeback Camp's places (Treeback_camp_regions, TreebackCampRegions).</summary>
    [TestClass]
    [DoNotParallelize]
    public class TreebackCampRegionsTests
    {
        private const string Migration = "20270607000000_Treeback_camp_regions";
        private const string Previous = "20270606000000_Dybukkar_garrison_regions";

        private const int Sky = 1, Env = 2, Ambient = 4, Music = 8, Name = 16;

        /// <summary>Treeback Camp's regions, as the .map has them: priority and the settings they turn on.</summary>
        private static readonly Dictionary<uint, (int Priority, int Flags)> Treeback = new()
        {
            [9] = (1, 13), [8] = (2, 16), [4] = (3, 20), [5] = (4, 28), [6] = (5, 16), [7] = (6, 16),
        };

        [TestMethod]
        public void TheCampsPlacesHaveTheirNames()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            Assert.AreEqual(0, Volumes(database).Count, "no region of the camp had a volume");

            migrator.Migrate(Migration);
            var after = Volumes(database);
            uint At(float x, float y, float z, bool underground) => Shown(after, new Vector3(x, y, z), Name, underground);

            Assert.AreEqual(4u, At(-337.5f, 103.9f, 398.7f, true), "Cave to Palisades at the way in");
            Assert.AreEqual(4u, At(-325f, 103.8f, 385f, true), "down the chunnel");
            Assert.AreEqual(4u, At(-334f, 103.8f, 350f, true), "at its mouth");
            Assert.AreEqual(4u, Shown(after, new Vector3(-334f, 103.8f, 350f), Ambient, underground: true), "with its cave echo");
            Assert.AreEqual(5u, At(-375f, 106.6f, 278f, false), "the AFS Staging Area's overrun camp");
            Assert.AreEqual(5u, At(-281f, 100.5f, 287f, false), "the field medic");
            Assert.AreEqual(5u, At(-280f, 100.2f, 190f, false), "the sandbags down the gully");
            Assert.AreEqual(5u, Shown(after, new Vector3(-281f, 100.5f, 287f), Music, underground: false), "with its music");
            Assert.AreEqual(6u, At(60f, 125.2f, 380f, false), "the Treeback Pen");
            Assert.AreEqual(6u, At(150f, 123f, 400f, false), "inside the sonic towers");
            Assert.AreEqual(7u, At(300f, 151.1f, 240f, false), "the Howler Kennels");
            Assert.AreEqual(7u, At(320f, 151.8f, 265f, false), "inside the gate");
            Assert.AreEqual(8u, At(200f, 121.2f, -130f, false), "the Bane Construction Site by the ponds");
            Assert.AreEqual(8u, At(248f, 146.4f, -267f, false), "at the drill");
            Assert.AreEqual(8u, At(180f, 125.2f, -60f, false), "inside its wall");
            Assert.AreEqual(0u, At(60f, 108.8f, 100f, false), "the Bane base in the middle is the map's");
            Assert.AreEqual(0u, At(-300f, 102.3f, 320f, false), "and the ground between the tunnel and the camp");
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
            Assert.AreEqual(TreebackCampRegions.Volumes.Length, once.Except(before).Count(), "the new rows");

            foreach (var sql in TreebackCampRegions.Up())
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

            foreach (var sql in TreebackCampRegions.Up())
                StringAssert.Contains(migrator.GenerateScript(Previous, Migration), sql);
            foreach (var sql in TreebackCampRegions.Down())
                StringAssert.Contains(migrator.GenerateScript(Migration, Previous), sql);
        }

        /// <summary>The highest-priority region here that turns the setting on, 0 for none (the map's own).</summary>
        private static uint Shown(List<MapRegion> volumes, Vector3 at, int flag, bool underground) =>
            volumes.Where(v => v.Enabled && Treeback.TryGetValue(v.RegionId, out var r) && (r.Flags & flag) != 0 && v.Contains(at, underground))
                .OrderByDescending(v => Treeback[v.RegionId].Priority).Select(v => v.RegionId).FirstOrDefault();

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == TreebackCampRegions.MapContextId)
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT map_context_id || ' ' || region_id || ' ' || shape || ' ' || printf('%.1f %.1f %.1f %.1f %.1f %.1f %.1f %.1f', pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y) || ' ' || underground || ' ' || enabled || ' ' || comment AS Value " +
                    "FROM map_region")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
