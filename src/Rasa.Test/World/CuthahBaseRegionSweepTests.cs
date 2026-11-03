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

    /// <summary>The region sweep on Cuthah Base (Cuthah_base_region_sweep, CuthahBaseRegionSweep).</summary>
    [TestClass]
    [DoNotParallelize]
    public class CuthahBaseRegionSweepTests
    {
        private const string Migration = "20270720010000_Cuthah_base_region_sweep";
        private const string Previous = "20270718000000_Purgas_station_gap_regions";

        private const int Sky = 1, Env = 2, Ambient = 4, Music = 8, Name = 16;

        /// <summary>The map's regions, as the .map has them: priority and the settings they turn on.</summary>
        private static readonly Dictionary<uint, (int Priority, int Flags)> Regions = new()
        {
            [2] = (1, 13), [5] = (2, 31), [33] = (3, 31), [31] = (4, 19), [11] = (5, 19), [9] = (6, 27), [7] = (7, 18), [10] = (8, 18), [8] = (9, 18),
            [12] = (10, 19), [14] = (11, 18), [27] = (12, 19), [15] = (13, 19), [22] = (14, 19), [16] = (15, 18), [28] = (16, 19), [32] = (17, 18),
            [17] = (18, 18), [19] = (19, 18), [20] = (20, 18), [29] = (21, 19), [21] = (22, 18), [18] = (23, 18), [23] = (24, 18), [24] = (25, 19),
            [30] = (26, 19), [25] = (27, 19), [26] = (28, 23),
        };

        /// <summary>Where each new or recut volume shows, and where the places beside it keep theirs.</summary>
        private static readonly (Vector3 At, bool Underground, int Flag, uint Before, uint After, string Why)[] Points =
        {
            (new Vector3(5f, 181.9f, -570f), true, Name, 33u, 31u, "entrance tunnel tee y181"),
            (new Vector3(5f, 181.9f, -570f), true, Sky, 33u, 31u, "entrance tunnel tee gets interior sky"),
            (new Vector3(117f, 174f, -488f), true, Name, 33u, 31u, "tunnel north end before L1 chunnel"),
            (new Vector3(60f, 181.9f, -560f), true, Env, 33u, 31u, "tunnel turn"),
            (new Vector3(5f, 213.9f, -520f), false, Name, 33u, 33u, "courtyard at ramp mouth stays courtyard"),
            (new Vector3(64f, 205.1f, -531.5f), false, Sky, 33u, 33u, "courtyard entrance hospital keeps outdoor sky"),
            (new Vector3(3.7f, 178.3f, -448.4f), true, Name, 12u, 11u, "Service Sector label (was Level 2)"),
            (new Vector3(3.7f, 178.3f, -448.4f), true, Ambient, 33u, 5u, "service tunnels lose courtyard ambient"),
            (new Vector3(37f, 181.3f, -400f), true, Name, 12u, 11u, "service room by L1 hall"),
            (new Vector3(-27f, 173.3f, -464f), true, Env, 33u, 11u, "service corridor west"),
            (new Vector3(23f, 165.9f, -511.5f), true, Name, 33u, 9u, "Rat Hole hospital"),
            (new Vector3(23f, 165.9f, -511.5f), true, Music, 33u, 9u, "Rat Hole safe-zone music"),
            (new Vector3(5f, 165.5f, -535f), true, Name, 33u, 9u, "Rat Hole tee south"),
            (new Vector3(146.6f, 181.3f, -264.1f), true, Name, 12u, 7u, "Barracks label"),
            (new Vector3(146.6f, 181.3f, -330f), true, Name, 12u, 7u, "south barracks hall over the Intelligence room"),
            (new Vector3(5f, 181.9f, -301.8f), true, Name, 12u, 10u, "Security Checkpoint booth"),
            (new Vector3(-75f, 173.9f, -290f), true, Name, 12u, 10u, "checkpoint ring chunnel west turn"),
            (new Vector3(5f, 171.9f, -200f), true, Name, 12u, 8u, "Interchange room"),
            (new Vector3(5f, 171.9f, -200f), true, Env, 12u, 8u, "Interchange env (same file, region changes)"),
            (new Vector3(-60f, 142.1f, -230f), true, Name, 12u, 12u, "Level 2 hub chunnel keeps Level 2"),
            (new Vector3(5f, 175.9f, -360f), true, Name, 12u, 5u, "L1 hall: Level 1 not Level 2"),
            (new Vector3(5f, 175.9f, -385f), true, Name, 12u, 5u, "L1 hall south (was L2 over courtyard rect)"),
            (new Vector3(5f, 175.9f, -385f), true, Music, 33u, 5u, "L1 hall south loses courtyard music"),
            (new Vector3(117f, 173.9f, -420f), true, Name, 12u, 5u, "east L1 chunnel"),
            (new Vector3(85f, 174.2f, -344f), true, Name, 12u, 5u, "L1 hall east tee"),
            (new Vector3(-187f, 139.9f, -224f), true, Name, 12u, 14u, "Bioresearch room v09"),
            (new Vector3(-203f, 142f, -294.5f), true, Name, 12u, 14u, "Prison Block"),
            (new Vector3(-290f, 141.5f, -262f), true, Name, 12u, 14u, "west service tunnels"),
            (new Vector3(-45f, 163.4f, -200f), true, Name, 12u, 27u, "west ramp"),
            (new Vector3(-187.3f, 149.9f, -270f), true, Name, 12u, 15u, "VEW label"),
            (new Vector3(-187f, 141.9f, -297f), true, Name, 12u, 14u, "prison corridor below the ward stays Bio wing"),
            (new Vector3(-187f, 140.9f, -100f), true, Name, 12u, 22u, "VED centre"),
            (new Vector3(-140f, 140.3f, -70f), true, Name, 24u, 22u, "VED east edge (was under Sublevel rect)"),
            (new Vector3(-27f, 141.9f, -136f), true, Name, 12u, 16u, "tunnel loop"),
            (new Vector3(-75f, 149.9f, -272f), true, Name, 12u, 16u, "west walkway"),
            (new Vector3(5f, 154.7f, -140f), true, Name, 12u, 28u, "middle ramp"),
            (new Vector3(5f, 139.9f, -296f), true, Name, 12u, 32u, "command control floor"),
            (new Vector3(53f, 150.1f, -296f), true, Name, 12u, 17u, "beds pod"),
            (new Vector3(-43f, 149.9f, -296f), true, Name, 12u, 19u, "workstation pod"),
            (new Vector3(149f, 139.9f, -330f), true, Name, 20u, 21u, "Intelligence core room"),
            (new Vector3(149f, 139.9f, -410f), true, Name, 12u, 20u, "south v13 room gets Intelligence Wing"),
            (new Vector3(229f, 139.9f, -328f), true, Name, 12u, 20u, "east v13 room gets Intelligence Wing"),
            (new Vector3(133f, 141.9f, -250f), true, Name, 12u, 20u, "wing approach turn"),
            (new Vector3(55f, 163.4f, -200f), true, Name, 12u, 29u, "east ramp"),
            (new Vector3(5f, 131.4f, -70f), true, Name, 24u, 30u, "sublevel ramp"),
            (new Vector3(5f, 109.1f, 64f), true, Name, 24u, 25u, "reactor room"),
            (new Vector3(5f, 109.1f, 64f), true, Sky, 24u, 25u, "reactor room cave sky"),
            (new Vector3(100f, 110.1f, 64f), true, Name, 24u, 24u, "sublevel ring corridor stays Sublevel"),
            (new Vector3(-55.8f, 108.9f, -32f), true, Name, 24u, 24u, "Sublevel Medic Station stays Sublevel"),
            (new Vector3(-187f, 140.9f, -100f), true, Sky, 12u, 22u, "VED gets its interior sky (L2 sky set before)"),
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
            Assert.AreEqual(CuthahBaseRegionSweep.Volumes.Length + CuthahBaseRegionSweep.Recuts.Length, once.Except(before).Count(), "the new rows and the recut ones");

            foreach (var sql in CuthahBaseRegionSweep.Up())
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

            foreach (var sql in CuthahBaseRegionSweep.Up())
                StringAssert.Contains(migrator.GenerateScript(Previous, Migration), sql);
            foreach (var sql in CuthahBaseRegionSweep.Down())
                StringAssert.Contains(migrator.GenerateScript(Migration, Previous), sql);
        }

        /// <summary>The highest-priority region here that turns the setting on, 0 for none (the map's own).</summary>
        private static uint Shown(List<MapRegion> volumes, Vector3 at, int flag, bool underground) =>
            volumes.Where(v => v.Enabled && Regions.TryGetValue(v.RegionId, out var r) && (r.Flags & flag) != 0 && v.Contains(at, underground))
                .OrderByDescending(v => Regions[v.RegionId].Priority).Select(v => v.RegionId).FirstOrDefault();

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == CuthahBaseRegionSweep.MapContextId)
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT map_context_id || ' ' || region_id || ' ' || shape || ' ' || printf('%.1f %.1f %.1f %.1f %.1f %.1f %.1f %.1f', pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y) || ' ' || underground || ' ' || enabled || ' ' || comment AS Value " +
                    "FROM map_region WHERE map_context_id = 2162")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
