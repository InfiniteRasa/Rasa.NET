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

    /// <summary>The region sweep on Omega Labs (Omega_labs_region_sweep, OmegaLabsRegionSweep).</summary>
    [TestClass]
    [DoNotParallelize]
    public class OmegaLabsRegionSweepTests
    {
        private const string Migration = "20270724010000_Omega_labs_region_sweep";
        private const string Previous = "20270718000000_Purgas_station_gap_regions";

        private const int Sky = 1, Env = 2, Ambient = 4, Music = 8, Name = 16;

        /// <summary>The map's regions, as the .map has them: priority and the settings they turn on.</summary>
        private static readonly Dictionary<uint, (int Priority, int Flags)> Regions = new()
        {
            [2] = (1, 13), [10] = (2, 16), [11] = (3, 16), [4] = (4, 16), [12] = (5, 16), [5] = (6, 20), [16] = (7, 16), [9] = (8, 16), [13] = (9, 16),
            [7] = (10, 16), [14] = (11, 16), [6] = (12, 16), [15] = (13, 16), [8] = (14, 24), [1] = (15, 1), [3] = (16, 31),
        };

        /// <summary>Where each new or recut volume shows, and where the places beside it keep theirs.</summary>
        private static readonly (Vector3 At, bool Underground, int Flag, uint Before, uint After, string Why)[] Points =
        {
            (new Vector3(0f, 269f, -300f), false, Name, 14u, 10u, "F5 corridor over the F4/F1 rects was F1"),
            (new Vector3(0f, 268.9f, -400f), false, Name, 10u, 10u, "F5 entrance cross unchanged"),
            (new Vector3(20f, 260.9f, -260f), false, Name, 14u, 11u, "F4 260 corridor was F1"),
            (new Vector3(-110f, 260.9f, -260f), false, Name, 12u, 11u, "F4 west corridor was F3"),
            (new Vector3(-64f, 264.3f, -240f), false, Name, 12u, 11u, "ward north platform outside the ward circle"),
            (new Vector3(-64f, 256.3f, -320f), false, Name, 4u, 4u, "Commitment Ward keeps its name"),
            (new Vector3(-70f, 272.3f, -300f), false, Name, 4u, 4u, "Commitment Ward top tier"),
            (new Vector3(60f, 244.9f, -150f), false, Name, 15u, 12u, "F3 corridor was Sublevel"),
            (new Vector3(-13f, 244.9f, -120f), false, Name, 15u, 16u, "Test Subject Holding no longer under Sublevel"),
            (new Vector3(-100f, 251.6f, -270f), false, Name, 12u, 12u, "F3 west stair stays F3"),
            (new Vector3(-20f, 244.3f, -200f), false, Name, 14u, 5u, "Bioweapons Unit was F1, now its own name"),
            (new Vector3(150f, 240.3f, -150f), false, Name, 9u, 9u, "NOC lower tier unchanged"),
            (new Vector3(104f, 229f, -165f), false, Name, 15u, 13u, "Reeducation ramp was Sublevel"),
            (new Vector3(80f, 223.6f, -300f), false, Name, 14u, 13u, "Reeducation west entry ramp was F1"),
            (new Vector3(50f, 220.9f, -300f), false, Name, 14u, 14u, "F1 corridor to Reeducation"),
            (new Vector3(0f, 204.9f, -200f), false, Name, 14u, 14u, "F1 corridor down"),
            (new Vector3(0f, 204.9f, -160f), false, Name, 15u, 14u, "same flat corridor, north end, now F1"),
            (new Vector3(0f, 188.9f, -100f), false, Name, 15u, 15u, "Sublevel corridor grid"),
            (new Vector3(0f, 180.6f, 30f), false, Name, 3u, 3u, "missile silo unchanged"),
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
            Assert.AreEqual(OmegaLabsRegionSweep.Volumes.Length + OmegaLabsRegionSweep.Recuts.Length, once.Except(before).Count(), "the new rows and the recut ones");

            foreach (var sql in OmegaLabsRegionSweep.Up())
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

            foreach (var sql in OmegaLabsRegionSweep.Up())
                StringAssert.Contains(migrator.GenerateScript(Previous, Migration), sql);
            foreach (var sql in OmegaLabsRegionSweep.Down())
                StringAssert.Contains(migrator.GenerateScript(Migration, Previous), sql);
        }

        /// <summary>The highest-priority region here that turns the setting on, 0 for none (the map's own).</summary>
        private static uint Shown(List<MapRegion> volumes, Vector3 at, int flag, bool underground) =>
            volumes.Where(v => v.Enabled && Regions.TryGetValue(v.RegionId, out var r) && (r.Flags & flag) != 0 && v.Contains(at, underground))
                .OrderByDescending(v => Regions[v.RegionId].Priority).Select(v => v.RegionId).FirstOrDefault();

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == OmegaLabsRegionSweep.MapContextId)
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT map_context_id || ' ' || region_id || ' ' || shape || ' ' || printf('%.1f %.1f %.1f %.1f %.1f %.1f %.1f %.1f', pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y) || ' ' || underground || ' ' || enabled || ' ' || comment AS Value " +
                    "FROM map_region WHERE map_context_id = 2203")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
