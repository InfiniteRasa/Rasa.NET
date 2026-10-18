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

    /// <summary>Label circles that put an interior or cave sky over open ground (Region_sky_circles, RegionSkyCircles).</summary>
    [TestClass]
    [DoNotParallelize]
    public class RegionSkyCirclesTests
    {
        private const string Migration = "20270116000000_Region_sky_circles";
        private const string Previous = "20270115000000_Named_npc_barks";

        /// <summary>The regions of each map that cover the places, as the .map has them: priority and sky (null: none).</summary>
        private static readonly Dictionary<uint, Dictionary<uint, (int Priority, string Sky)>> Maps = new()
        {
            [1759] = new() { [0] = (0, "sky_arieki_torden_mires"), [4] = (12, "sky_arieki_general_human_interior"), [16] = (7, null), [18] = (18, "sky_bane_interior_default") },
            [1764] = new() { [0] = (0, "sky_arieki_torden_plains"), [5] = (17, "sky_arieki_torden_plains"), [6] = (20, "sky_arieki_brann_interior_default") },
            [1911] = new()
            {
                [0] = (0, "sky_arieki_ligo_thunderhead_bright"), [9] = (2, "sky_arieki_ligo_thunderhead_bright"), [16] = (11, null),
                [25] = (3, "sky_arieki_general_human_interior"), [26] = (12, "sky_arieki_atta_interior_corridors"),
                [27] = (20, "sky_arieki_general_human_interior"), [28] = (18, null), [39] = (4, "sky_arieki_torden_abyss_cave")
            },
            [2047] = new()
            {
                [0] = (0, "sky_foreas_valverde_descent"), [17] = (19, "sky_foreas_valverde_descent_tier3"), [21] = (23, "sky_foreas_cave_short"),
                [22] = (24, "sky_foreas_cave_short"), [26] = (27, "sky_bane_interior_default"), [27] = (22, "sky_foreas_valverde_descent_tier4"),
                [28] = (32, "sky_bane_interior_default")
            },
            [1244] = new() { [0] = (0, "sky_foreas_palisades_default"), [30] = (3, "sky_foreas_cave_hall") },
            [1220] = new() { [0] = (0, "sky_foreas_wilderness_default_tranz"), [14] = (36, null), [38] = (38, "sky_foreas_cave_short"), [48] = (37, null) },
            [2374] = new() { [0] = (0, "sky_foreas_valverde_edmundrange"), [9] = (5, null), [16] = (2, "sky_foreas_general_human_interior") },
            [2361] = new() { [0] = (0, "sky_foreas_valverde_edmundrange"), [16] = (2, "sky_foreas_general_human_interior") },
        };

        private static readonly (string Place, uint Map, Vector3 Where, string Was, string Is)[] Places =
        {
            ("Fort Haroun", 1759, new(612.2f, 0, 403.8f), "sky_arieki_general_human_interior", "sky_arieki_torden_mires"),
            ("Bane Occupied Mires", 1759, new(-542.7f, 0, 278.0f), "sky_bane_interior_default", "sky_arieki_torden_mires"),
            ("Irendas Penal Colony", 1764, new(208.0f, 0, -115.8f), "sky_arieki_brann_interior_default", "sky_arieki_torden_plains"),
            ("Thunderhead Base, outside its interior rectangle", 1911, new(624.5f, 0, -380f), "sky_arieki_torden_abyss_cave", "sky_arieki_ligo_thunderhead_bright"),
            ("AFS Substation E-104, outside its rectangle", 1911, new(953.0f, 0, 640.0f), "sky_arieki_general_human_interior", "sky_arieki_ligo_thunderhead_bright"),
            ("Rivasa Atta Colony, above ground", 1911, new(-777.0f, 0, 5.0f), "sky_arieki_atta_interior_corridors", "sky_arieki_ligo_thunderhead_bright"),
            ("Mal Dys", 2047, new(540.2f, 0, 401.5f), "sky_bane_interior_default", "sky_foreas_valverde_descent_tier4"),
            ("Antaeus Hollow, away from the Medic Station", 2047, new(101.2f, 0, 400.0f), "sky_foreas_cave_short", "sky_foreas_valverde_descent"),
            ("Staging Point, outside its rectangle", 1244, new(650f, 0, -267.6f), "sky_foreas_cave_hall", "sky_foreas_palisades_default"),
            ("Daghda's Urn Village", 1220, new(-662.9f, 0, 799.5f), "sky_foreas_cave_short", "sky_foreas_wilderness_default_tranz"),
            ("Edmund Range's Staging Area", 2374, new(-64.0f, 0, -365.8f), "sky_foreas_general_human_interior", "sky_foreas_valverde_edmundrange"),
            ("the wargame Proving Grounds' Staging Area", 2361, new(40.0f, 0, -20.0f), "sky_foreas_general_human_interior", "sky_foreas_valverde_edmundrange"),
        };

        [TestMethod]
        public void TheOpenGroundRoundEachLabelHasItsOutdoorSky()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            var before = Volumes(database);
            foreach (var p in Places)
                Assert.AreEqual(p.Was, SkyAt(before, p.Map, p.Where), $"{p.Place}, before");

            migrator.Migrate(Migration);
            var after = Volumes(database);
            foreach (var p in Places)
                Assert.AreEqual(p.Is, SkyAt(after, p.Map, p.Where), $"{p.Place}, after");

            // The rectangles of the interiors keep theirs.
            Assert.AreEqual("sky_arieki_general_human_interior", SkyAt(after, 1911, new Vector3(681f, 0, -540f)), "Thunderhead Base's interior rectangle");
            Assert.AreEqual("sky_arieki_general_human_interior", SkyAt(after, 1911, new Vector3(966f, 0, 474f)), "the Substation's rectangle");
            Assert.AreEqual("sky_bane_interior_default", SkyAt(after, 2047, new Vector3(713f, 0, 276f)), "Mal Dys's interior rectangle");
            Assert.AreEqual("sky_foreas_cave_hall", SkyAt(after, 1244, new Vector3(865f, 0, -255f)), "Staging Point's rectangle");
            Assert.AreEqual("sky_arieki_atta_interior_corridors", SkyAt(after, 1911, new Vector3(-918f, 0, 57f), underground: true), "the Atta colony's tunnels");
            Assert.AreEqual("sky_foreas_cave_short", SkyAt(after, 2047, new Vector3(187f, 0, 560f), underground: true), "Antaeus Hollow's cavern");
        }

        [TestMethod]
        public void TheUpRunsTwiceTheDownEnablesThemAgainAndAMovedCircleIsLeftAlone()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            database.Context.Database.ExecuteSqlRaw("UPDATE map_region SET pos_x = pos_x + 5 WHERE map_context_id = 1220 AND region_id = 38;");
            var before = Snapshot(database);

            migrator.Migrate(Migration);
            var once = Snapshot(database);
            Assert.AreEqual(RegionSkyCircles.Circles.Length - 1, before.Except(once).Count(), "every circle as seeded, not the one a GM moved");
            Assert.IsTrue(once.Any(row => row.StartsWith("1220 38 ") && row.EndsWith(" 1")), "Daghda's Urn's, moved, still on");

            foreach (var sql in RegionSkyCircles.Up())
                database.Context.Database.ExecuteSqlRaw(sql.Replace("{", "{{").Replace("}", "}}"));
            CollectionAssert.AreEqual(once, Snapshot(database), "twice");

            migrator.Migrate(Previous);
            CollectionAssert.AreEqual(before, Snapshot(database), "Down");
        }

        [TestMethod]
        public void OnMySqlTheRowsChangeAndChangeBack()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(Previous, Migration);
            var down = migrator.GenerateScript(Migration, Previous);

            foreach (var sql in RegionSkyCircles.Up())
                StringAssert.Contains(up, sql);
            StringAssert.Contains(up, $"'{Migration}'");
            foreach (var sql in RegionSkyCircles.Down())
                StringAssert.Contains(down, sql);
        }

        /// <summary>The sky the client draws: the highest-priority region it is sent that has one, else region 0's (regionmanager.py).</summary>
        private static string SkyAt(List<MapRegion> volumes, uint map, Vector3 position, bool underground = false)
        {
            var regions = Maps[map];
            var sent = volumes.Where(v => v.MapContextId == map && v.Enabled && v.Contains(position, underground))
                .Select(v => v.RegionId).Append(0u).Distinct()
                .Where(id => regions.TryGetValue(id, out var r) && r.Sky != null);

            return regions[sent.OrderByDescending(id => regions[id].Priority).First()].Sky;
        }

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .Where(entry => Maps.Keys.Contains(entry.MapContextId))
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT map_context_id || ' ' || region_id || ' ' || shape || ' ' || printf('%.1f %.1f %.1f', pos_x, pos_z, radius) || ' ' || enabled AS Value " +
                    "FROM map_region")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
