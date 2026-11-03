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

    /// <summary>The region sweep on the Valverde Pools (Valverde_pools_region_sweep, ValverdePoolsRegionSweep).</summary>
    [TestClass]
    [DoNotParallelize]
    public class ValverdePoolsRegionSweepTests
    {
        private const string Migration = "20270723000000_Valverde_pools_region_sweep";
        private const string Previous = "20270718000000_Purgas_station_gap_regions";

        private const int Sky = 1, Env = 2, Ambient = 4, Music = 8, Name = 16;

        /// <summary>The map's regions, as the .map has them: priority and the settings they turn on.</summary>
        private static readonly Dictionary<uint, (int Priority, int Flags)> Regions = new()
        {
            [1] = (1, 31), [24] = (2, 1), [22] = (3, 1), [23] = (4, 1), [9] = (5, 24), [5] = (6, 23), [6] = (7, 16), [2] = (8, 1), [8] = (9, 16),
            [3] = (10, 23), [10] = (11, 23), [11] = (12, 28), [29] = (13, 28), [13] = (14, 28), [12] = (15, 28), [27] = (16, 13), [4] = (17, 19),
            [21] = (18, 28), [14] = (19, 16), [15] = (20, 28), [16] = (21, 16), [17] = (22, 24), [20] = (23, 31), [18] = (24, 23), [19] = (25, 23),
            [7] = (26, 28), [25] = (27, 20), [26] = (28, 16), [28] = (29, 0),
        };

        /// <summary>Where each new or recut volume shows, and where the places beside it keep theirs.</summary>
        private static readonly (Vector3 At, bool Underground, int Flag, uint Before, uint After, string Why)[] Points =
        {
            (new Vector3(240f, 664.8f, -590f), true, Name, 0u, 3u, "south post label/hub"),
            (new Vector3(240f, 664.8f, -590f), true, Sky, 22u, 3u, "south post: Molder.sky -> human underground sky"),
            (new Vector3(212f, 668.9f, -588f), true, Env, 0u, 3u, "south post ramp env"),
            (new Vector3(174f, 673f, -604f), true, Name, 0u, 3u, "south post west entrance"),
            (new Vector3(240f, 686.1f, -590f), false, Sky, 22u, 0u, "ground above south post: no Molder.sky"),
            (new Vector3(240f, 664.7f, -614f), false, Name, 0u, 0u, "south entrance mouth stays outdoors"),
            (new Vector3(147f, 672.2f, -607f), false, Name, 0u, 4u, "dropship pad"),
            (new Vector3(147f, 672.2f, -607f), false, Sky, 0u, 4u, "dropship pad swamp sky"),
            (new Vector3(950f, 656.8f, 332f), true, Name, 0u, 10u, "east post label"),
            (new Vector3(950f, 656.8f, 332f), true, Sky, 23u, 10u, "east post: Molder.sky -> human underground sky"),
            (new Vector3(996f, 657f, 309f), true, Ambient, 0u, 10u, "east post hospital"),
            (new Vector3(1020f, 656.8f, 372f), true, Env, 0u, 10u, "east post north cross"),
            (new Vector3(940.5f, 665.3f, 275.5f), false, Sky, 23u, 0u, "trench outside east post: no Molder.sky"),
            (new Vector3(924f, 658.6f, 314f), false, Name, 0u, 0u, "east entrance mouth stays outdoors"),
            (new Vector3(346f, 684.2f, 640f), true, Sky, 24u, 27u, "Thrax control room: bane interior sky"),
            (new Vector3(346f, 684.2f, 640f), true, Music, 0u, 27u, "Thrax control room music"),
            (new Vector3(470f, 686f, 584f), true, Ambient, 0u, 27u, "Thrax entry tunnel"),
            (new Vector3(378f, 685.4f, 720f), true, Sky, 24u, 27u, "Thrax north service room"),
            (new Vector3(333.2f, 989f, 704.5f), false, Sky, 24u, 0u, "ground above Thrax interior: no Molder.sky"),
            (new Vector3(480f, 686.1f, 584f), false, Music, 12u, 12u, "barracks yard at the tunnel mouth keeps Thrax Barracks"),
            (new Vector3(-445f, 888.1f, 744f), true, Name, 0u, 20u, "Retread Caves entrance tee"),
            (new Vector3(-445f, 888.1f, 744f), true, Sky, 0u, 20u, "cave sky in the entrance"),
            (new Vector3(-437f, 886.6f, 705f), false, Name, 6u, 6u, "cave mouth stays Splitfinger Forest"),
            (new Vector3(-720f, 968.2f, 765f), true, Name, 0u, 19u, "Plateau Pass cave"),
            (new Vector3(-715f, 962.1f, 750f), false, Sky, 0u, 0u, "Plateau Pass mouth stays outdoors"),
            (new Vector3(708f, 679.3f, 725f), true, Name, 0u, 18u, "Marshes Pass cave"),
            (new Vector3(700f, 674.7f, 715f), false, Sky, 0u, 0u, "Marshes Pass mouth stays outdoors"),
            (new Vector3(-230f, 686f, -288f), true, Name, 0u, 13u, "armory entrance ramp inside"),
            (new Vector3(-215f, 686f, -288f), false, Music, 0u, 13u, "armory entrance ramp mouth"),
            (new Vector3(-215f, 692.5f, -320f), false, Name, 0u, 29u, "machine yard south of the ramp"),
            (new Vector3(-190f, 682.9f, -300f), false, Ambient, 0u, 29u, "machine yard east"),
            (new Vector3(-300f, 695f, -300f), false, Name, 0u, 0u, "trench line west of the yard untouched"),
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
            Assert.AreEqual(ValverdePoolsRegionSweep.Volumes.Length + ValverdePoolsRegionSweep.Recuts.Length, once.Except(before).Count(), "the new rows and the recut ones");

            foreach (var sql in ValverdePoolsRegionSweep.Up())
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

            foreach (var sql in ValverdePoolsRegionSweep.Up())
                StringAssert.Contains(migrator.GenerateScript(Previous, Migration), sql);
            foreach (var sql in ValverdePoolsRegionSweep.Down())
                StringAssert.Contains(migrator.GenerateScript(Migration, Previous), sql);
        }

        /// <summary>The highest-priority region here that turns the setting on, 0 for none (the map's own).</summary>
        private static uint Shown(List<MapRegion> volumes, Vector3 at, int flag, bool underground) =>
            volumes.Where(v => v.Enabled && Regions.TryGetValue(v.RegionId, out var r) && (r.Flags & flag) != 0 && v.Contains(at, underground))
                .OrderByDescending(v => Regions[v.RegionId].Priority).Select(v => v.RegionId).FirstOrDefault();

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == ValverdePoolsRegionSweep.MapContextId)
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT map_context_id || ' ' || region_id || ' ' || shape || ' ' || printf('%.1f %.1f %.1f %.1f %.1f %.1f %.1f %.1f', pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y) || ' ' || underground || ' ' || enabled || ' ' || comment AS Value " +
                    "FROM map_region WHERE map_context_id = 1304")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
