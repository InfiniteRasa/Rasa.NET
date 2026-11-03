using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Managers;
    using Rasa.Navigation;
    using Rasa.Services.Preloader;
    using Rasa.Test.Database;
    using Rasa.Test.Missions.Wilderness;

    using W = Rasa.Services.Preloader.WarnetCavernsContent;

    /// <summary>Warnet Caverns peopled and fitted out (Warnet_caverns, WarnetCavernsContent).</summary>
    [TestClass]
    [DoNotParallelize]
    public class WarnetCavernsContentTests
    {
        private const string Migration = "20271014000000_Warnet_caverns";

        private static readonly string[] Tables =
        {
            "creature", "creature_stat", "creature_action", "creature_appearance", "npc_package", "npc_greeting", "vendor", "vendor_item",
            "spawnpool", "world_destructible", "world_usable", "world_container", "map_link"
        };

        [TestMethod]
        public void TheMigrationPutsEverythingInOnceAndDownTakesItOutExactly()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();
            var all = database.Context.Database.GetMigrations().ToList();
            var previous = all[all.IndexOf(Migration) - 1];
            migrator.Migrate(previous);
            var before = Snapshot(database);
            Assert.AreEqual(0L, Count(database, $"SELECT COUNT(*) AS Value FROM map_link WHERE map_context_id = {W.Map} OR dest_map_context_id = {W.Map}"),
                "no way in from the Palisades before");

            migrator.Migrate(Migration);

            foreach (var c in W.Creatures)
            {
                Assert.AreEqual((long)c.NameId, Count(database, $"SELECT name_id AS Value FROM creature WHERE id = {c.Id} AND class_id = {c.ClassId} AND level = {c.Level} AND max_hp = {c.MaxHp}"), c.Comment);
                Assert.AreEqual((long)c.MaxHp, Count(database, $"SELECT health AS Value FROM creature_stat WHERE id = {c.Id} AND armor = {c.Armor}"), c.Comment);
                if (c.AppearanceFrom != 0)
                    Assert.AreEqual(Count(database, $"SELECT COUNT(*) AS Value FROM creature_appearance WHERE id = {c.AppearanceFrom}"),
                        Count(database, $"SELECT COUNT(*) AS Value FROM creature_appearance WHERE id = {c.Id}"), $"{c.Comment}: dressed");
            }

            foreach (var a in W.Actions)
                Assert.AreEqual((long)a.MaxDamage, Count(database, $"SELECT max_damage AS Value FROM creature_action WHERE id = {a.Id} AND action_id = {a.ActionId}"), a.Description);

            Assert.AreEqual((long)W.BrumleyPackage, Count(database, $"SELECT package_id AS Value FROM npc_package WHERE id = {W.Brumley}"));
            Assert.AreEqual((long)W.LaFontaineGreeting, Count(database, $"SELECT greeting_id AS Value FROM npc_greeting WHERE id = {W.LaFontaine}"));
            Assert.AreEqual((long)W.LaFontaineGreeting, Count(database, $"SELECT greeting_id AS Value FROM npc_greeting WHERE id = 510135"), "Mullen keeps it");
            Assert.AreEqual(Count(database, $"SELECT COUNT(*) AS Value FROM vendor_item WHERE id = {W.CavernsVendor}"),
                Count(database, $"SELECT COUNT(*) AS Value FROM vendor_item WHERE id = {W.Grant}"), "Grant's stock");
            Assert.IsTrue(Count(database, $"SELECT COUNT(*) AS Value FROM vendor_item WHERE id = {W.Grant}") >= 5);

            foreach (var p in W.AllPools())
                Assert.AreEqual((long)p.Creature, Count(database, $"SELECT creature_1_Id AS Value FROM spawnpool WHERE id = {p.Id} AND map_context_id = {p.MapContextId} AND mode = {p.Mode} AND creature_1_max_count = {p.Max}"), p.Comment);
            Assert.AreEqual(24L, Count(database, $"SELECT COUNT(*) AS Value FROM spawnpool WHERE id BETWEEN {W.FirstLureWavePool} AND {W.FirstDeathWavePool + W.WavePools - 1} AND mode = {W.Scripted} AND creature_1_Id = {W.WarnetL19}"));
            foreach (var id in W.CampTurretPools)
                Assert.AreEqual((long)W.Scripted, Count(database, $"SELECT mode AS Value FROM spawnpool WHERE id = {id}"), $"turret {id} waits for the camp");

            foreach (var h in W.Hives)
                Assert.AreEqual((long)W.HiveBrood, Count(database, $"SELECT creature_id AS Value FROM world_destructible WHERE id = {h.Id} AND class_id = {W.HiveClass} AND creature_count = {W.HiveBroodCount}"), h.Where);
            foreach (var u in W.Usables)
                Assert.AreEqual(1L, Count(database, $"SELECT usable AS Value FROM world_usable WHERE id = {u.Id} AND map_context_id = {W.Map} AND class_id = {u.ClassId} AND item_class_id = {u.ItemClass} AND item_template_id = {u.ItemTemplate} AND state = {u.State}"), u.Comment);
            foreach (var c in W.Crates)
                Assert.AreEqual(1L, Count(database, $"SELECT COUNT(*) AS Value FROM world_container WHERE id = {c.Id} AND class_id = {c.ClassId} AND loot_profile_key = '{W.CrateLoot}'"), c.Comment);

            Assert.AreEqual((long)W.Map, Count(database, $"SELECT dest_map_context_id AS Value FROM map_link WHERE id = {W.EntryLink} AND map_context_id = {W.Palisades} AND kind = 1 AND enabled = 1"));
            Assert.AreEqual((long)W.Palisades, Count(database, $"SELECT dest_map_context_id AS Value FROM map_link WHERE id = {W.ExitLink} AND map_context_id = {W.Map} AND kind = 1 AND enabled = 1"));

            var once = Snapshot(database);
            foreach (var sql in W.Up())
                Exec(database, sql);
            CollectionAssert.AreEqual(once, Snapshot(database), "twice is once");

            migrator.Migrate(previous);
            CollectionAssert.AreEqual(before, Snapshot(database), "Down puts back what it changed");
        }

        /// <summary>Everything stands on walkable ground of its map, at the floor's height; the way in and out lands clear of the other trigger.</summary>
        [TestMethod]
        public void EverythingStandsOnTheNavmeshAndTheWaysInAndOutAreClearOfEachOther()
        {
            var navmesh = Path.Combine(RepositoryRoot(), "navmesh");
            var caverns = TestNavMeshes.Query(NavMeshFile.PathFor(navmesh, "adv_foreas_concordia_palisades_warnetcaverns"));
            var palisades = TestNavMeshes.Query(NavMeshFile.PathFor(navmesh, "adv_foreas_concordia_palisades"));

            var spots = new List<(string Who, uint Map, Vector3 At)>();
            spots.AddRange(W.AllPools().Select(p => (p.Comment, p.MapContextId, V(p.X, p.Y, p.Z))));
            spots.AddRange(W.Usables.Select(u => (u.Comment, W.Map, V(u.At))));
            spots.AddRange(W.Crates.Select(c => (c.Comment, W.Map, V(c.X, c.Y, c.Z))));
            spots.AddRange(W.Stations.Select((s, i) => ($"station {i + 1}", W.Map, V(s))));
            foreach (var (who, spot) in new[]
            {
                ("Remote Station 3", W.RemoteStation), ("the bio-bomb", W.BioBomb), ("the Queen", W.QueenSpot),
                ("Sides at the mouth", W.SidesAtMouth), ("Brumley at the mouth", W.BrumleyAtMouth), ("Gooding at the mouth", W.GoodingAtMouth),
                ("Sides at his post", W.SidesAtPost), ("Brumley at his post", W.BrumleyAtPost), ("Gooding at her post", W.GoodingAtPost),
                ("the caverns' arrival", W.CavernsArrival), ("the caverns' mouth", W.CavernsMouth)
            })
                spots.Add((who, W.Map, V(spot)));
            spots.Add(("the Palisades' arrival", W.Palisades, V(W.PalisadesArrival)));
            spots.Add(("the Palisades' cave mouth", W.Palisades, V(W.PalisadesMouth)));
            Assert.IsTrue(spots.Count > 80);

            foreach (var (who, map, at) in spots)
            {
                var query = map == W.Map ? caverns : palisades;
                var ground = query.Nearest(at + new Vector3(0, 0.2f, 0));

                Assert.IsTrue(ground.HasValue, $"{who} off the navmesh");
                Assert.IsTrue(new Vector2(ground.Value.X - at.X, ground.Value.Z - at.Z).Length() < 0.6f, $"{who} at {at}, nearest walkable {ground}");
                Assert.AreEqual(ground.Value.Y, at.Y, 0.5f, $"{who}: its floor");
            }

            Assert.IsTrue(Vector3.Distance(V(W.CavernsArrival), V(W.CavernsMouth)) > W.LinkRadius + 5, "arriving in the caverns is not leaving them");
            Assert.IsTrue(Vector3.Distance(V(W.PalisadesArrival), V(W.PalisadesMouth)) > W.LinkRadius + 2, "arriving on the Palisades is not going back in");
        }

        /// <summary>The walks the story takes: in to the camp, Brumley's to Remote Station 3, Bug City and the Nest; the caverns rebuilt with a 1.5 m climb are one.</summary>
        [TestMethod]
        public void TheWalksTheStoryTakesAreOnTheNavmesh()
        {
            var caverns = TestNavMeshes.Query(NavMeshFile.PathFor(Path.Combine(RepositoryRoot(), "navmesh"), "adv_foreas_concordia_palisades_warnetcaverns"));

            foreach (var (from, to, what) in new[]
            {
                (W.CavernsArrival, W.SidesAtEntrance, "from the way in to the sandbags"),
                (W.SidesAtEntrance, W.SidesAtMouth, "the specialists to the camp's mouth"),
                (W.BrumleyAtMouth, W.BrumleyAtPost, "and in to their posts"),
                (W.BrumleyAtPost, W.RemoteStation, "Brumley to Remote Station 3"),
                (W.GoodingAtPost, W.MetalContainerSpot, "the camp to the metal container"),
                (W.GoodingAtPost, W.JoahgSpot, "the camp to Joahg"),
                (W.RemoteStation, W.LureStand, "the Dig Site to the lure stand"),
                (W.LureStand, W.QueenSpot, "the lure stand to the Queen"),
                (W.LureStand, W.BioBomb, "the lure stand to the bio-bomb")
            })
            {
                var path = caverns.FindPath(V(from), V(to), out var complete);
                Assert.IsTrue(complete && path.Count > 0 && Vector3.Distance(path[^1], V(to)) < 2f, $"{what}: {V(from)} to {V(to)}");
            }
        }

        /// <summary>The hives hang under the roof, low enough to be shot and for a player on the floor to stir them.</summary>
        [TestMethod]
        public void TheHivesHangUnderBugCitysRoofWithinReach()
        {
            var directory = Path.Combine(RepositoryRoot(), "navmesh");
            var cover = CoverMesh.Read(CoverMesh.PathFor(directory, "adv_foreas_concordia_palisades_warnetcaverns"));
            var caverns = TestNavMeshes.Query(NavMeshFile.PathFor(directory, "adv_foreas_concordia_palisades_warnetcaverns"));

            foreach (var hive in W.Hives)
            {
                var floor = caverns.Nearest(new Vector3((float)hive.X, (float)hive.Floor + 0.2f, (float)hive.Z));
                Assert.IsTrue(floor.HasValue && Math.Abs(floor.Value.Y - hive.Floor) < 0.5f, $"the floor under the hive in {hive.Where}: {floor}");

                var hangs = hive.Y - hive.Floor;
                Assert.IsTrue(hangs <= PlacedDestructibles.SpawnerTriggerHeight, $"{hive.Where}: {hangs:F1} m up, out of a player's stirring");
                Assert.IsTrue(hangs >= 6.6, $"{hive.Where}: {hangs:F1} m up, its 6.5 m pod through the floor");

                var under = new Vector3((float)hive.X, (float)hive.Floor + 0.5f, (float)hive.Z);
                Assert.IsFalse(cover.Blocked(under, new Vector3((float)hive.X, (float)hive.Y - 0.05f, (float)hive.Z)), $"{hive.Where}: in the rock");
                Assert.IsTrue(cover.Blocked(under, new Vector3((float)hive.X, (float)hive.Y + 1f, (float)hive.Z)), $"{hive.Where}: the roof is just above it");
            }
        }

        /// <summary>The server takes every row as meant: the people, their stock and package, the objects as their kinds.</summary>
        [TestMethod]
        public void TheServerLoadsThemAsTheyAreMeant()
        {
            using var harness = WildernessRuntimeTestHarness.Create(mapContextId: W.Map);
            var map = harness.Map;

            var creatures = harness.Creatures.LoadedCreatures;
            foreach (var c in W.Creatures)
            {
                Assert.IsTrue(creatures.TryGetValue(c.Id, out var creature), c.Comment);
                Assert.AreEqual(c.Level, creature.Level, c.Comment);
            }
            Assert.AreEqual(creatures[W.CavernsVendor].Npc.Vendor.VendorItems.Count, creatures[W.Grant].Npc.Vendor.VendorItems.Count);
            Assert.AreEqual(W.BrumleyPackage, creatures[W.Brumley].Npc.NpcPackageId, "Brumley talks");

            var pools = map.SpawnPools.ToDictionary(pool => pool.DbId);
            foreach (var p in W.AllPools().Where(p => p.MapContextId == W.Map))
            {
                Assert.IsTrue(pools.TryGetValue(p.Id, out var pool), p.Comment);
                Assert.AreEqual((short)p.Mode, pool.Mode, p.Comment);
            }

            harness.SpawnWorld(W.ChelliakPool, W.SidesPool);
            var living = map.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList).Distinct().ToList();
            Assert.AreEqual(1790, living.Single(c => c.DbId == W.Chelliak).Attributes[Rasa.Data.Attributes.Health].CurrentMax);

            try
            {
                Assert.AreEqual(W.Hives.Length, PlacedDestructibles.Load(harness.World.WorldDestructibleEntries.AsNoTracking().Where(row => row.MapContextId == W.Map).ToList()));
                Assert.AreEqual(W.Hives.Length, PlacedDestructibles.Place(map));
                Assert.IsTrue(PlacedDestructibles.On(map).All(live => live.Placement.Kind == PlacedDestructibles.Kind.Spawner && live.CreatureId == W.HiveBrood && live.CreatureCount == W.HiveBroodCount));

                Assert.AreEqual(W.Usables.Length, PlacedUsables.Load(harness.World.WorldUsableEntries.AsNoTracking().Where(row => row.MapContextId == W.Map).ToList()));
                Assert.AreEqual(W.Usables.Length, PlacedUsables.Place(map));
                var usables = PlacedUsables.On(map).ToDictionary(live => live.Placement.Id);
                Assert.AreEqual(PlacedUsables.Kind.Dispenser, usables[W.MetalContainerDispenser].Placement.Kind);
                Assert.AreEqual(W.MetalContainerTemplate, usables[W.MetalContainerDispenser].Placement.ItemTemplateId);
                Assert.AreEqual(PlacedUsables.Kind.Acceptor, usables[W.LureAcceptor].Placement.Kind);
                Assert.AreEqual(W.QueenLureClass, usables[W.LureAcceptor].Placement.ItemClassId);
                Assert.IsTrue(WarnetCaverns.TakesNothing(usables[W.LureAcceptor]), "the lure stays with its mission");

                Assert.AreEqual(W.Crates.Length, WorldContainers.Load(harness.World.WorldContainerEntries.AsNoTracking().Where(row => row.MapContextId == W.Map).ToList(), Array.Empty<Rasa.Structures.World.WorldContainerItemEntry>()));
            }
            finally
            {
                PlacedDestructibles.Load(null);
                PlacedDestructibles.Reset();
                PlacedUsables.Load(null);
                PlacedUsables.Reset();
                WorldContainers.Load(null, null);
                WorldContainers.Reset();
            }
        }

        [TestMethod]
        public void OnMySqlTheChangesGoInAndComeOut()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var all = context.Database.GetMigrations().ToList();
            var previous = all[all.IndexOf(Migration) - 1];
            var up = migrator.GenerateScript(previous, Migration);
            var down = migrator.GenerateScript(Migration, previous);

            foreach (var sql in W.Up().Concat(WarnetCavernsMissions.Up()))
                StringAssert.Contains(up, sql);
            StringAssert.Contains(up, $"'{Migration}'");
            foreach (var sql in W.Down().Concat(WarnetCavernsMissions.Down()))
                StringAssert.Contains(down, sql);
        }

        private static Vector3 V(W.Spot spot) => V(spot.X, spot.Y, spot.Z);

        private static Vector3 V(double x, double y, double z) => new((float)x, (float)y, (float)z);

        private static long Count(OrdinaryLootTestDatabase database, string sql) =>
            database.Context.Database.SqlQueryRaw<long>(sql).AsEnumerable().Single();

        private static void Exec(OrdinaryLootTestDatabase database, string sql) =>
            database.Context.Database.ExecuteSqlRaw(sql.Replace("{", "{{").Replace("}", "}}"));

        private static List<string> Snapshot(OrdinaryLootTestDatabase database)
        {
            var rows = new List<string>();
            foreach (var table in Tables)
            {
                using var command = database.Context.Database.GetDbConnection().CreateCommand();
                command.CommandText = $"SELECT * FROM {table}";
                database.Context.Database.OpenConnection();
                using var reader = command.ExecuteReader();
                while (reader.Read())
                    rows.Add(table + ":" + string.Join("|", Enumerable.Range(0, reader.FieldCount).Select(i => reader.IsDBNull(i) ? "" : Convert.ToString(reader.GetValue(i), System.Globalization.CultureInfo.InvariantCulture))));
            }
            rows.Sort(StringComparer.Ordinal);
            return rows;
        }

        private static string RepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "navmesh")))
                directory = directory.Parent;
            return directory?.FullName ?? AppContext.BaseDirectory;
        }
    }
}
