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

    /// <summary>The region sweep on the Valverde Plateau (Valverde_plateau_region_sweep, ValverdePlateauRegionSweep).</summary>
    [TestClass]
    [DoNotParallelize]
    public class ValverdePlateauRegionSweepTests
    {
        private const string Migration = "20270722000000_Valverde_plateau_region_sweep";
        private const string Previous = "20270718000000_Purgas_station_gap_regions";

        private const int Sky = 1, Env = 2, Ambient = 4, Music = 8, Name = 16;

        /// <summary>The map's regions, as the .map has them: priority and the settings they turn on.</summary>
        private static readonly Dictionary<uint, (int Priority, int Flags)> Regions = new()
        {
            [6] = (1, 13), [24] = (2, 16), [10] = (3, 28), [23] = (4, 24), [14] = (5, 12), [18] = (6, 28), [15] = (7, 28), [13] = (8, 15),
            [30] = (9, 25), [22] = (10, 25), [20] = (11, 25), [29] = (12, 13), [28] = (13, 29), [27] = (14, 29), [11] = (15, 25), [31] = (16, 29),
            [16] = (17, 29), [19] = (18, 31), [17] = (19, 25), [9] = (20, 28), [32] = (21, 31), [7] = (22, 29), [2] = (23, 15), [41] = (24, 31),
            [4] = (25, 31), [25] = (26, 7), [39] = (27, 7), [1] = (28, 31), [21] = (29, 5), [38] = (30, 15), [33] = (31, 16), [35] = (32, 16),
            [36] = (33, 16), [37] = (34, 16), [40] = (35, 0),
        };

        /// <summary>Where each new or recut volume shows, and where the places beside it keep theirs.</summary>
        private static readonly (Vector3 At, bool Underground, int Flag, uint Before, uint After, string Why)[] Points =
        {
            (new Vector3(160f, 313.9f, -240f), true, Name, 4u, 4u, "Derak Prison chunnel keeps its name"),
            (new Vector3(200f, 342.7f, -200f), false, Sky, 4u, 0u, "open ground above Derak Prison: no interior sky"),
            (new Vector3(243f, 313.9f, -240f), false, Name, 7u, 4u, "Derak Prison label at the tunnel entrance (was Desolation Quarry)"),
            (new Vector3(-456f, 285.3f, 100f), true, Sky, 0u, 2u, "region 2 cave north ramp"),
            (new Vector3(-570f, 381.3f, 0f), true, Sky, 0u, 2u, "region 2 cave west tunnel"),
            (new Vector3(-500f, 320f, 20f), true, Sky, 2u, 2u, "region 2 cave by the dispenser"),
            (new Vector3(-159f, 399.1f, 1040f), true, Name, 0u, 1u, "Fort Defiance north annex"),
            (new Vector3(-159f, 403.5f, 992f), true, Name, 0u, 1u, "Fort Defiance north corridor"),
            (new Vector3(-120f, 402.5f, 754f), true, Name, 0u, 1u, "Fort Defiance gate passage"),
            (new Vector3(-201f, 424.9f, 750f), true, Name, 0u, 1u, "west cave tunnel into Fort Defiance"),
            (new Vector3(-300f, 401.5f, 900f), true, Name, 0u, 1u, "Fort Defiance west wall (underground navmesh)"),
            (new Vector3(-225f, 421.3f, 935f), false, Name, 0u, 1u, "AFS Criminal Investigations Division"),
            (new Vector3(-120f, 400.3f, 859f), false, Ambient, 1u, 21u, "fort cavern hall by the auction house"),
            (new Vector3(-68f, 401.5f, 930f), false, Name, 1u, 1u, "AFS High Command keeps Fort Defiance"),
            (new Vector3(-120f, 469.6f, 760f), false, Ambient, 1u, 1u, "surface above the gate keeps Fort Defiance"),
            (new Vector3(-120f, 469.6f, 760f), false, Name, 1u, 1u, "surface label point"),
            (new Vector3(-443f, 380.5f, 865f), true, Sky, 0u, 38u, "west Eloh shrine cross room"),
            (new Vector3(-428f, 380.3f, 865f), true, Music, 0u, 38u, "west shrine east arm"),
            (new Vector3(-120f, 408.5f, -340f), true, Sky, 0u, 38u, "Maligo-front Eloh shrine"),
            (new Vector3(-443f, 375.5f, 770f), true, Sky, 0u, 39u, "cave tunnel room to the west shrine"),
            (new Vector3(-443f, 375.8f, 810f), true, Env, 0u, 39u, "cave room end"),
            (new Vector3(-120f, 405.3f, -380f), true, Sky, 0u, 39u, "ramp to the Maligo-front shrine"),
            (new Vector3(-445f, 378.9f, 715f), false, Sky, 0u, 0u, "west cave mouth stays outdoors"),
            (new Vector3(-400f, 288.3f, -760f), true, Name, 0u, 16u, "Maligo Creek west"),
            (new Vector3(-550f, 288.3f, -745f), false, Ambient, 0u, 16u, "Maligo Creek far west"),
            (new Vector3(-300f, 306.3f, -730f), false, Name, 0u, 16u, "creek pool west of the cave"),
            (new Vector3(44f, 313.3f, -728f), true, Name, 0u, 16u, "creek pool east of the cave"),
            (new Vector3(130f, 322.1f, -770f), false, Ambient, 0u, 16u, "swamp pond"),
            (new Vector3(-120f, 381.1f, -830f), false, Name, 31u, 31u, "Martyr's Canyon terrace keeps its name"),
            (new Vector3(-230f, 306.3f, -720f), true, Name, 19u, 19u, "Maligo Creek cave keeps region 19"),
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
            Assert.AreEqual(ValverdePlateauRegionSweep.Volumes.Length + ValverdePlateauRegionSweep.Recuts.Length, once.Except(before).Count(), "the new rows and the recut ones");

            foreach (var sql in ValverdePlateauRegionSweep.Up())
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

            foreach (var sql in ValverdePlateauRegionSweep.Up())
                StringAssert.Contains(migrator.GenerateScript(Previous, Migration), sql);
            foreach (var sql in ValverdePlateauRegionSweep.Down())
                StringAssert.Contains(migrator.GenerateScript(Migration, Previous), sql);
        }

        /// <summary>The highest-priority region here that turns the setting on, 0 for none (the map's own).</summary>
        private static uint Shown(List<MapRegion> volumes, Vector3 at, int flag, bool underground) =>
            volumes.Where(v => v.Enabled && Regions.TryGetValue(v.RegionId, out var r) && (r.Flags & flag) != 0 && v.Contains(at, underground))
                .OrderByDescending(v => Regions[v.RegionId].Priority).Select(v => v.RegionId).FirstOrDefault();

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == ValverdePlateauRegionSweep.MapContextId)
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT map_context_id || ' ' || region_id || ' ' || shape || ' ' || printf('%.1f %.1f %.1f %.1f %.1f %.1f %.1f %.1f', pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y) || ' ' || underground || ' ' || enabled || ' ' || comment AS Value " +
                    "FROM map_region WHERE map_context_id = 1497")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
