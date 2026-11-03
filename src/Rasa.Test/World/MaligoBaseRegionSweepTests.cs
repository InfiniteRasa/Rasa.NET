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

    /// <summary>The region sweep on Maligo Base (Maligo_base_region_sweep, MaligoBaseRegionSweep).</summary>
    [TestClass]
    [DoNotParallelize]
    public class MaligoBaseRegionSweepTests
    {
        private const string Migration = "20270722010000_Maligo_base_region_sweep";
        private const string Previous = "20270718000000_Purgas_station_gap_regions";

        private const int Sky = 1, Env = 2, Ambient = 4, Music = 8, Name = 16;

        /// <summary>The map's regions, as the .map has them: priority and the settings they turn on.</summary>
        private static readonly Dictionary<uint, (int Priority, int Flags)> Regions = new()
        {
            [1] = (1, 13), [14] = (2, 21), [13] = (3, 21), [9] = (4, 21), [8] = (5, 21), [10] = (6, 21), [11] = (7, 21), [4] = (8, 21), [12] = (9, 21),
            [6] = (10, 21), [5] = (11, 21), [7] = (12, 21), [15] = (13, 16), [3] = (14, 23), [17] = (15, 0), [18] = (16, 0), [19] = (17, 0),
            [16] = (18, 0),
        };

        /// <summary>Where each new or recut volume shows, and where the places beside it keep theirs.</summary>
        private static readonly (Vector3 At, bool Underground, int Flag, uint Before, uint After, string Why)[] Points =
        {
            (new Vector3(-120f, 23.3f, 250f), true, Sky, 0u, 3u, "tower dig pit"),
            (new Vector3(-120f, 44.9f, 340f), false, Name, 0u, 3u, "crater north rim by the Eloh Dig Site label"),
            (new Vector3(-124f, 46.5f, 140f), false, Env, 0u, 3u, "surface tunnel entrance"),
            (new Vector3(-124f, 48.6f, 40f), false, Name, 15u, 3u, "crater surface above Construction Control"),
            (new Vector3(-200f, 50.6f, 100f), false, Sky, 0u, 3u, "crater west slope"),
            (new Vector3(-124f, 12f, 44f), true, Name, 15u, 15u, "Construction Control room keeps its name"),
            (new Vector3(-124f, 12f, 44f), true, Sky, 0u, 0u, "Construction Control keeps the interior sky"),
            (new Vector3(-124f, 34.9f, 100f), true, Name, 15u, 15u, "Construction Control ramp"),
            (new Vector3(-14f, -135.1f, 128f), true, Name, 0u, 5u, "L1 reactor floor"),
            (new Vector3(-14f, -135.1f, 128f), true, Ambient, 0u, 5u, "L1 reactor ambient"),
            (new Vector3(-14f, -130.1f, 80f), true, Name, 0u, 5u, "L1 reactor south arm"),
            (new Vector3(82f, -132.1f, 128f), true, Name, 0u, 7u, "L1 east control room"),
            (new Vector3(-110f, -132.1f, 128f), true, Name, 15u, 7u, "L1 west control room (was Construction Control)"),
            (new Vector3(0f, -130.1f, 72f), true, Name, 0u, 6u, "L1 chunnel hall"),
            (new Vector3(-120f, -129.9f, 75f), true, Name, 15u, 6u, "L1 chunnel hall west (was Construction Control)"),
            (new Vector3(80f, -130f, 72f), true, Ambient, 0u, 6u, "L1 chunnel hall east"),
            (new Vector3(0f, -130.9f, 300f), true, Name, 0u, 12u, "L1 north security wing"),
            (new Vector3(-46f, -138.7f, 320f), true, Name, 0u, 12u, "L1 north wing lower room"),
            (new Vector3(-14f, -130.1f, 220f), true, Name, 0u, 0u, "L1 north tunnel stays Service Corridor"),
            (new Vector3(150f, -130.5f, 128f), true, Name, 0u, 0u, "L1 entrance stays Service Corridor"),
            (new Vector3(-240f, -99.1f, 120f), true, Name, 0u, 13u, "wormhole chamber floor"),
            (new Vector3(-280f, -99.7f, 150f), true, Ambient, 0u, 13u, "wormhole chamber floor north"),
            (new Vector3(-250f, -90.1f, 55f), true, Name, 0u, 14u, "overlook walkway south"),
            (new Vector3(-170f, -90.1f, 130f), true, Name, 15u, 14u, "overlook walkway east (was Construction Control)"),
            (new Vector3(-118f, -100.1f, 128f), true, Name, 15u, 11u, "L2 west control room (was Construction Control)"),
            (new Vector3(90f, -99.9f, 128f), true, Name, 0u, 11u, "L2 east control room"),
            (new Vector3(-350f, -100.1f, 128f), true, Name, 0u, 11u, "L2 far west control room"),
            (new Vector3(-350f, -89.1f, 158f), true, Name, 0u, 11u, "far west control room overlook loop"),
            (new Vector3(-43.1f, -102.6f, 105.4f), true, Name, 0u, 10u, "L2 reactor walkway at -104"),
            (new Vector3(-40.2f, -75.3f, 111.6f), true, Ambient, 0u, 10u, "L2 reactor ring at -76"),
            (new Vector3(2f, -90.5f, 184f), true, Name, 0u, 8u, "L2 service corridor north"),
            (new Vector3(-13f, -90.1f, 70f), true, Name, 0u, 8u, "Level 02: Barricade hospital"),
            (new Vector3(-6f, -90.7f, 232f), true, Name, 0u, 8u, "L2 service room north"),
            (new Vector3(-46f, -90.9f, 184f), true, Ambient, 0u, 8u, "L2 service corridor NW"),
            (new Vector3(146f, -97.9f, 128f), true, Name, 0u, 0u, "east prison chunnels unplaced"),
        };

        [TestMethod]
        public void EachPlaceShowsItsRegion()
        {
            var (before, after) = Migrated();

            foreach (var (at, underground, flag, was, now, why) in Points)
            {
                Assert.AreEqual(was, Shown(before, at, flag, underground), $"{why}: before");
                Assert.AreEqual(now, Shown(after, at, flag, underground), $"{why}: after");
            }
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
        public void TheUpRunsTwiceAndTheDownPutsItBack()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            var before = Snapshot(database);

            migrator.Migrate(Migration);
            var once = Snapshot(database);
            Assert.AreEqual(MaligoBaseRegionSweep.Volumes.Length + MaligoBaseRegionSweep.Recuts.Length, once.Except(before).Count(), "the new rows and the recut ones");

            foreach (var sql in MaligoBaseRegionSweep.Up())
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

            foreach (var sql in MaligoBaseRegionSweep.Up())
                StringAssert.Contains(migrator.GenerateScript(Previous, Migration), sql);
            foreach (var sql in MaligoBaseRegionSweep.Down())
                StringAssert.Contains(migrator.GenerateScript(Migration, Previous), sql);
        }

        /// <summary>The highest-priority region here that turns the setting on, 0 for none (the map's own).</summary>
        private static uint Shown(List<MapRegion> volumes, Vector3 at, int flag, bool underground) =>
            volumes.Where(v => v.Enabled && Regions.TryGetValue(v.RegionId, out var r) && (r.Flags & flag) != 0 && v.Contains(at, underground))
                .OrderByDescending(v => Regions[v.RegionId].Priority).Select(v => v.RegionId).FirstOrDefault();

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == MaligoBaseRegionSweep.MapContextId)
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT map_context_id || ' ' || region_id || ' ' || shape || ' ' || printf('%.1f %.1f %.1f %.1f %.1f %.1f %.1f %.1f', pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y) || ' ' || underground || ' ' || enabled || ' ' || comment AS Value " +
                    "FROM map_region WHERE map_context_id = 1830")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
