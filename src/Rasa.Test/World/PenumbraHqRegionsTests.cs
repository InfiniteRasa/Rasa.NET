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

    /// <summary>Penumbra Headquarters and its Sections in Tantalus Base, and Eloh Space cut to its shrines (Penumbra_hq_regions, PenumbraHqRegions).</summary>
    [TestClass]
    [DoNotParallelize]
    public class PenumbraHqRegionsTests
    {
        private const string Migration = "20270213000000_Penumbra_hq_regions";
        private const string Previous = "20270212000000_Outpost_tower_teleporters";

        /// <summary>The Abyss's regions here, as the .map has them: priority and sky (null: none).</summary>
        private static readonly Dictionary<uint, (int Priority, string Sky)> Regions = new()
        {
            [0] = (0, "sky_arieki_torden_abyss_cave_dark"),
            [44] = (7, "sky_arieki_cave_abyss_default"),
            [18] = (12, "sky_arieki_general_human_interior"),
            [22] = (13, "sky_arieki_general_human_interior"), [23] = (14, "sky_arieki_general_human_interior"), [24] = (15, "sky_arieki_general_human_interior"),
            [25] = (16, "sky_arieki_general_human_interior_dark"), [26] = (17, "sky_arieki_general_human_interior"),
            [15] = (33, "sky_eloh_interior_default"),
        };

        private static readonly (string Place, Vector3 At, uint Region)[] Places =
        {
            ("Penumbra Headquarters' label, in the lobby", new(-542f, 537f, -152f), 18),
            ("the lobby's Section 1 teleporter", new(-508.5f, 541f, -181f), 18),
            ("Penumbra's crafting stations", new(-600f, 537.5f, -105f), 18),
            ("Section 1's teleporter", new(-580f, 537.7f, -236f), 22),
            ("Section 2's teleporter", new(-578.6f, 538.5f, -62.4f), 23),
            ("Section 3's teleporter", new(-644.4f, 537.7f, -201.5f), 24),
            ("Section 4's teleporter", new(-669.3f, 545.5f, -24.5f), 25),
            ("Section 5's teleporter", new(-713.9f, 546f, -152f), 26),
            ("Section 5 at the Omega Labs door", new(-866f, 545.5f, -82f), 26),
        };

        [TestMethod]
        public void EachSectionHasItsOwnSkyAndElohSpaceIsOffTheBase()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            var before = Volumes(database);
            Assert.AreEqual("sky_eloh_interior_default", SkyAt(before, new Vector3(-520f, 537f, -152f)), "Eloh Space's rectangle over the lobby, before");

            migrator.Migrate(Migration);
            var after = Volumes(database);

            foreach (var p in Places)
            {
                Assert.AreEqual(p.Region, Top(after, p.At), p.Place);
                Assert.AreEqual(Regions[p.Region].Sky, SkyAt(after, p.At), p.Place);
            }

            Assert.AreEqual(44u, Top(after, new Vector3(-400f, 533f, -176f), surface: true), "the base yard is Tantalus Base's");
            Assert.AreEqual(15u, Top(after, new Vector3(-315f, 190f, -400f)), "the Eloh shrine under the base, south");
            Assert.AreEqual(15u, Top(after, new Vector3(-465f, 505f, 224f)), "and north");
            Assert.AreNotEqual(15u, Top(after, new Vector3(-400f, 533f, -100f), surface: true), "but not the base over them");
        }

        [TestMethod]
        public void NoTwoRegionsBoxesOverlap()
        {
            var boxes = PenumbraHqRegions.Volumes;

            foreach (var a in boxes)
                foreach (var b in boxes)
                    if (!ReferenceEquals(a, b) && a.RegionId != b.RegionId)
                        Assert.IsFalse(Math.Abs(a.X - b.X) < a.HalfX + b.HalfX - 0.01 && Math.Abs(a.Z - b.Z) < a.HalfZ + b.HalfZ - 0.01,
                            $"{a.Comment} and {b.Comment}");
        }

        [TestMethod]
        public void TheUpRunsTwiceAndTheDownPutsEverythingBack()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            var before = Snapshot(database);

            migrator.Migrate(Migration);
            var once = Snapshot(database);
            Assert.AreEqual(PenumbraHqRegions.Volumes.Length + 2, once.Except(before).Count(), "the new boxes, Eloh Space's second, and its first, moved");

            foreach (var sql in PenumbraHqRegions.Up())
                Assert.AreEqual(0, database.Context.Database.ExecuteSqlRaw(sql.Replace("{", "{{").Replace("}", "}}")), sql);
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

            foreach (var sql in PenumbraHqRegions.Up())
                StringAssert.Contains(up, sql);
            foreach (var sql in PenumbraHqRegions.Down())
                StringAssert.Contains(down, sql);
        }

        /// <summary>The highest-priority region the player is sent here (of those listed), 0 for none.</summary>
        private static uint Top(List<MapRegion> volumes, Vector3 at, bool surface = false) =>
            volumes.Where(v => v.Enabled && Regions.ContainsKey(v.RegionId) && v.Contains(at, underground: !surface))
                .Select(v => v.RegionId).Append(0u).OrderByDescending(id => Regions[id].Priority).First();

        /// <summary>The sky the client draws: the highest-priority region it is sent that has one (regionmanager.py).</summary>
        private static string SkyAt(List<MapRegion> volumes, Vector3 at) =>
            Regions[volumes.Where(v => v.Enabled && Regions.ContainsKey(v.RegionId) && Regions[v.RegionId].Sky != null && v.Contains(at, underground: true))
                .Select(v => v.RegionId).Append(0u).OrderByDescending(id => Regions[id].Priority).First()].Sky;

        private static List<MapRegion> Volumes(OrdinaryLootTestDatabase database) =>
            ((Rasa.Context.World.WorldContext)database.Context).MapRegionEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == PenumbraHqRegions.MapContextId)
                .AsEnumerable().Select(MapRegion.FromEntry).ToList();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    "SELECT map_context_id || ' ' || region_id || ' ' || shape || ' ' || printf('%.1f %.1f %.1f %.1f %.1f %.1f %.1f', pos_x, pos_z, radius, half_x, half_z, min_y, max_y) || ' ' || enabled || ' ' || comment AS Value " +
                    "FROM map_region")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();
    }
}
