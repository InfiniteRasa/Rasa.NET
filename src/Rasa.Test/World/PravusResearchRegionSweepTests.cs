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

    /// <summary>The region sweep on Pravus Research (Pravus_research_region_sweep, PravusResearchRegionSweep).</summary>
    [TestClass]
    [DoNotParallelize]
    public class PravusResearchRegionSweepTests
    {
        private const string Migration = "20270725000000_Pravus_research_region_sweep";
        private const string Previous = "20270718000000_Purgas_station_gap_regions";

        private const int Sky = 1, Env = 2, Ambient = 4, Music = 8, Name = 16;

        /// <summary>The map's regions, as the .map has them: priority and the settings they turn on.</summary>
        private static readonly Dictionary<uint, (int Priority, int Flags)> Regions = new()
        {
            [5] = (1, 31), [17] = (2, 0), [6] = (3, 15), [8] = (4, 28), [2] = (5, 31), [9] = (6, 28), [10] = (7, 28), [11] = (8, 28), [12] = (9, 28),
            [4] = (10, 31), [13] = (11, 31), [16] = (12, 31), [15] = (13, 31), [14] = (14, 31),
        };

        /// <summary>Where each new or recut volume shows, and where the places beside it keep theirs.</summary>
        private static readonly (Vector3 At, bool Underground, int Flag, uint Before, uint After, string Why)[] Points =
        {
            (new Vector3(224f, 9.9f, -8f), true, Name, 0u, 4u, "hub tunnel cross"),
            (new Vector3(270f, 9.9f, -8f), true, Name, 0u, 4u, "east tunnel"),
            (new Vector3(272f, 17.9f, -24f), true, Sky, 0u, 4u, "upper service room"),
            (new Vector3(192f, 38.2f, -19f), true, Name, 0u, 4u, "top of entry spiral ramp"),
            (new Vector3(312f, 9.9f, 90f), true, Name, 0u, 4u, "tunnel between cells and tee"),
            (new Vector3(340f, 11.2f, 128f), true, Name, 0u, 4u, "east chunnel endcap"),
            (new Vector3(280f, 17.9f, 40f), true, Name, 0u, 4u, "overlook coupler (stays Halls)"),
            (new Vector3(315f, 9.6f, 40f), true, Name, 0u, 13u, "prison chunnel floor"),
            (new Vector3(303f, 17.9f, 40f), true, Ambient, 0u, 13u, "prison overlook"),
            (new Vector3(120f, 17.3f, 96f), true, Name, 0u, 16u, "west extraction room"),
            (new Vector3(152f, 17.3f, 80f), true, Ambient, 0u, 16u, "south extraction room"),
            (new Vector3(136f, 17.3f, 112f), true, Name, 0u, 16u, "north extraction room"),
            (new Vector3(224f, 8.7f, 170f), true, Name, 0u, 15u, "round chamber floor"),
            (new Vector3(224f, 17.9f, 95f), true, Name, 0u, 15u, "chamber console gallery"),
            (new Vector3(140f, 12f, 168f), true, Name, 0u, 15u, "west teleporter bay"),
            (new Vector3(160f, 17.3f, 97f), true, Name, 0u, 15u, "gallery west corridor mouth"),
            (new Vector3(200f, 10.5f, 45f), true, Name, 0u, 14u, "control room west"),
            (new Vector3(224f, 8.4f, 20f), true, Name, 0u, 14u, "control room south"),
            (new Vector3(248f, 10.6f, 40f), true, Sky, 0u, 14u, "control room east"),
            (new Vector3(-193.5f, 10.6f, -257.4f), true, Sky, 0u, 6u, "arrival point in cave"),
            (new Vector3(-185f, 13.4f, -252f), true, Music, 0u, 6u, "cave tunnel"),
            (new Vector3(-178f, 8.6f, -228f), false, Sky, 0u, 0u, "cave mouth stays outdoors"),
            (new Vector3(200f, 40.5f, -8f), false, Name, 12u, 12u, "courtyard over entry stays Pravus Research Facility"),
            (new Vector3(140f, 33.3f, -40f), true, Name, 0u, 0u, "pond floor above facility untouched"),
            (new Vector3(-70f, 34f, 165f), false, Name, 0u, 9u, "Northwest Fortification bunker"),
            (new Vector3(-55f, 34.6f, -235f), false, Name, 0u, 10u, "Southwest Fortification"),
            (new Vector3(1f, 32.9f, 267f), false, Name, 0u, 11u, "North Fortification label"),
            (new Vector3(-116f, 26.5f, 1.6f), false, Name, 11u, 2u, "Frontlines label now shows Frontlines"),
            (new Vector3(-116f, 26.5f, 1.6f), false, Sky, 2u, 2u, "Frontlines sky unchanged"),
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
            Assert.AreEqual(PravusResearchRegionSweep.Volumes.Length + PravusResearchRegionSweep.Recuts.Length, once.Except(before).Count(), "the new rows and the recut ones");

            foreach (var sql in PravusResearchRegionSweep.Up())
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

            foreach (var sql in PravusResearchRegionSweep.Up())
                StringAssert.Contains(migrator.GenerateScript(Previous, Migration), sql);
            foreach (var sql in PravusResearchRegionSweep.Down())
                StringAssert.Contains(migrator.GenerateScript(Migration, Previous), sql);
        }

        /// <summary>The highest-priority region here that turns the setting on, 0 for none (the map's own).</summary>
        private static uint Shown(List<MapRegion> volumes, Vector3 at, int flag, bool underground) =>
            volumes.Where(v => v.Enabled && Regions.TryGetValue(v.RegionId, out var r) && (r.Flags & flag) != 0 && v.Contains(at, underground))
                .OrderByDescending(v => Regions[v.RegionId].Priority).Select(v => v.RegionId).FirstOrDefault();

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == PravusResearchRegionSweep.MapContextId)
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT map_context_id || ' ' || region_id || ' ' || shape || ' ' || printf('%.1f %.1f %.1f %.1f %.1f %.1f %.1f %.1f', pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y) || ' ' || underground || ' ' || enabled || ' ' || comment AS Value " +
                    "FROM map_region WHERE map_context_id = 1430")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
