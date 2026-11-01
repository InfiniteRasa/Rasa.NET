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

    /// <summary>The region sweep on Manhattan (shared) (Manhattan_shared_region_sweep, ManhattanSharedRegionSweep).</summary>
    [TestClass]
    [DoNotParallelize]
    public class ManhattanSharedRegionSweepTests
    {
        private const string Migration = "20270726010000_Manhattan_shared_region_sweep";
        private const string Previous = "20270718000000_Purgas_station_gap_regions";

        private const int Sky = 1, Env = 2, Ambient = 4, Music = 8, Name = 16;

        /// <summary>The map's regions, as the .map has them: priority and the settings they turn on.</summary>
        private static readonly Dictionary<uint, (int Priority, int Flags)> Regions = new()
        {
            [1] = (1, 13), [23] = (2, 8), [11] = (3, 16), [8] = (4, 9), [9] = (5, 0), [2] = (6, 23), [5] = (7, 16), [12] = (8, 16), [13] = (9, 17),
            [14] = (10, 17), [6] = (11, 20), [7] = (12, 24), [15] = (13, 25), [4] = (14, 25), [21] = (15, 0), [22] = (16, 0),
        };

        /// <summary>Where each new or recut volume shows, and where the places beside it keep theirs.</summary>
        private static readonly (Vector3 At, bool Underground, int Flag, uint Before, uint After, string Why)[] Points =
        {
            (new Vector3(10f, 184.7f, 120f), true, Sky, 0u, 2u, "main line between stations"),
            (new Vector3(10f, 184.7f, 120f), true, Name, 0u, 2u, "main line between stations"),
            (new Vector3(-36f, 185.6f, 100f), true, Ambient, 0u, 2u, "west track tunnel"),
            (new Vector3(-35f, 185.5f, 230f), true, Sky, 0u, 2u, "west tunnel beside the crater"),
            (new Vector3(25f, 198.3f, 225f), false, Sky, 0u, 14u, "crater floor, open sky"),
            (new Vector3(25f, 198.3f, 225f), false, Name, 0u, 14u, "crater floor"),
            (new Vector3(0f, 185.2f, 220f), true, Name, 0u, 14u, "crater rubble (flagged)"),
            (new Vector3(-2.6f, 184.7f, -10.2f), true, Name, 0u, 12u, "28th St platform level"),
            (new Vector3(-2.6f, 184.7f, -10.2f), true, Sky, 0u, 2u, "28th St keeps subway sky"),
            (new Vector3(-24f, 205.1f, -19f), true, Name, 0u, 12u, "28th St stair head"),
            (new Vector3(-50f, 199.9f, -57f), true, Name, 0u, 12u, "28th St south ramp"),
            (new Vector3(-2.6f, 209.7f, -10.2f), false, Name, 12u, 0u, "street above 28th St"),
            (new Vector3(-2f, 184.7f, 300f), true, Sky, 0u, 13u, "33rd St platform dark sky"),
            (new Vector3(-2f, 184.7f, 340f), true, Name, 0u, 13u, "33rd St north end"),
            (new Vector3(-2f, 209.2f, 302f), false, Sky, 13u, 0u, "street above 33rd St no longer dark"),
            (new Vector3(-2f, 196.1f, 398f), false, Name, 4u, 4u, "Outpost E34 unchanged"),
            (new Vector3(-2f, 203.6f, 360f), false, Name, 4u, 4u, "rubble ramp to E34 unchanged"),
            (new Vector3(-276f, 185.6f, -493f), true, Name, 0u, 5u, "23rd St medic"),
            (new Vector3(-276f, 185.6f, -493f), true, Sky, 0u, 2u, "23rd St medic subway sky"),
            (new Vector3(-302f, 189.5f, -519f), true, Name, 0u, 5u, "23rd St platform"),
            (new Vector3(-302f, 208.9f, -552f), false, Name, 5u, 0u, "street above 23rd St"),
            (new Vector3(-276f, 185.6f, -630f), false, Name, 0u, 2u, "unflagged tunnel to E23"),
            (new Vector3(-276f, 185.6f, -630f), false, Ambient, 0u, 2u, "unflagged tunnel to E23"),
            (new Vector3(-285f, 185.1f, -675f), false, Name, 0u, 11u, "Wormhole E23 pit"),
            (new Vector3(-285f, 185.1f, -675f), false, Sky, 0u, 0u, "E23 pit keeps the outdoor sky"),
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
            Assert.AreEqual(ManhattanSharedRegionSweep.Volumes.Length + ManhattanSharedRegionSweep.Recuts.Length, once.Except(before).Count(), "the new rows and the recut ones");

            foreach (var sql in ManhattanSharedRegionSweep.Up())
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

            foreach (var sql in ManhattanSharedRegionSweep.Up())
                StringAssert.Contains(migrator.GenerateScript(Previous, Migration), sql);
            foreach (var sql in ManhattanSharedRegionSweep.Down())
                StringAssert.Contains(migrator.GenerateScript(Migration, Previous), sql);
        }

        /// <summary>The highest-priority region here that turns the setting on, 0 for none (the map's own).</summary>
        private static uint Shown(List<MapRegion> volumes, Vector3 at, int flag, bool underground) =>
            volumes.Where(v => v.Enabled && Regions.TryGetValue(v.RegionId, out var r) && (r.Flags & flag) != 0 && v.Contains(at, underground))
                .OrderByDescending(v => Regions[v.RegionId].Priority).Select(v => v.RegionId).FirstOrDefault();

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == ManhattanSharedRegionSweep.MapContextId)
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT map_context_id || ' ' || region_id || ' ' || shape || ' ' || printf('%.1f %.1f %.1f %.1f %.1f %.1f %.1f %.1f', pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y) || ' ' || underground || ' ' || enabled || ' ' || comment AS Value " +
                    "FROM map_region WHERE map_context_id = 2375")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
