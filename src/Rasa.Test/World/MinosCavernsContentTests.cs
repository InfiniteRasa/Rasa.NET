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
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Navigation;
    using Rasa.Services.Preloader;
    using Rasa.Test.Database;
    using Rasa.Test.Missions.Wilderness;

    /// <summary>Minos Caverns populated (Minos_caverns_content, MinosCavernsContent).</summary>
    [TestClass]
    [DoNotParallelize]
    public class MinosCavernsContentTests
    {
        private const string Migration = "20270309000000_Minos_caverns_content";
        private const string Previous = "20270308000000_Eir_crater_pirolja_midja_regions";

        [TestMethod]
        public void TheMigrationPutsEverythingInLeavesWhatAGmChangedAndTakesItOutAgain()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();
            migrator.Migrate(Previous);

            // A GM had already moved the entrance hall's Thrax pool a little and set one of the stood-down pools to hold four.
            Exec(database, $"UPDATE spawnpool SET pos_x = pos_x + 1 WHERE id = {MinosCavernsContent.EntrancePool}");
            Exec(database, $"UPDATE spawnpool SET creature_1_max_count = 4 WHERE id = {MinosCavernsContent.StoodDownPools[0]}");
            var before = Snapshot(database);
            var hallWas = Pool(database, MinosCavernsContent.EntrancePool);

            migrator.Migrate(Migration);

            foreach (var c in MinosCavernsContent.Creatures)
            {
                Assert.AreEqual((long)c.NameId, Count(database, $"SELECT name_id AS Value FROM creature WHERE id = {c.Id} AND class_id = {c.ClassId} AND level = {c.Level} AND max_hp = {c.MaxHp}"), c.Comment);
                Assert.AreEqual((long)c.Health, Count(database, $"SELECT health AS Value FROM creature_stat WHERE id = {c.Id} AND armor = {c.Armor}"), $"{c.Comment}: its stat row, without which it has 100 health");
                if (c.AppearanceFrom != 0)
                    Assert.AreEqual(Count(database, $"SELECT COUNT(*) AS Value FROM creature_appearance WHERE id = {c.AppearanceFrom}"),
                        Count(database, $"SELECT COUNT(*) AS Value FROM creature_appearance WHERE id = {c.Id}"), $"{c.Comment}: dressed");
                if (c.Package != 0)
                    Assert.AreEqual((long)c.Package, Count(database, $"SELECT package_id AS Value FROM npc_package WHERE id = {c.Id}"), c.Comment);
            }

            foreach (var a in MinosCavernsContent.Actions)
                Assert.AreEqual((long)a.MaxDamage, Count(database, $"SELECT max_damage AS Value FROM creature_action WHERE id = {a.Id} AND action_id = {a.ActionId}"), a.Description);

            Assert.AreEqual((long)MinosCavernsContent.MedicalVendorPackage, Count(database, $"SELECT package_id AS Value FROM vendor WHERE id = {MinosCavernsContent.MedicalVendor}"));
            Assert.AreEqual(Count(database, $"SELECT COUNT(*) AS Value FROM vendor_item WHERE id = {MinosCavernsContent.StockFrom}"),
                Count(database, $"SELECT COUNT(*) AS Value FROM vendor_item WHERE id = {MinosCavernsContent.MedicalVendor}"), "the medical vendor's stock");
            Assert.IsTrue(Count(database, $"SELECT COUNT(*) AS Value FROM vendor_item WHERE id = {MinosCavernsContent.MedicalVendor}") >= 10);

            foreach (var p in MinosCavernsContent.AllPools())
            {
                Assert.AreEqual((p.Creature, R(p.X), R(p.Y), R(p.Z), R(p.Rotation)), Pool(database, p.Id), p.Comment);
                Assert.AreEqual((long)p.Mode, Count(database, $"SELECT mode AS Value FROM spawnpool WHERE id = {p.Id} AND map_context_id = {p.MapContextId}"), p.Comment);
                Assert.AreEqual((long)p.Max, Count(database, $"SELECT creature_1_max_count AS Value FROM spawnpool WHERE id = {p.Id} AND creature_1_min_count = {p.Min}"), p.Comment);
                if (p.Pose != 0)
                    Assert.AreEqual((long)p.Pose, Count(database, $"SELECT pose AS Value FROM spawnpool_pose WHERE id = {p.Id}"), p.Comment);
            }

            foreach (var t in MinosCavernsContent.Teleporters)
                Assert.AreEqual((long)t.EntityId, Count(database, $"SELECT entity_id AS Value FROM spawnpool_arrival WHERE id = {(t.Row != 0 ? t.Row : t.Pool)} AND pool_id = {t.Pool} AND kind = 2"), $"teleporter of pool {t.Pool}");

            // PR105's pools: the hall's holds three and arrives through the hall's three teleporters only, where the GM left it; the other two stand down, as the GM left them.
            var hall = Pool(database, MinosCavernsContent.EntrancePool);
            Assert.AreEqual(hallWas, hall, "left where the GM put it");
            Assert.AreEqual(3L, Count(database, $"SELECT creature_1_min_count AS Value FROM spawnpool WHERE id = {MinosCavernsContent.EntrancePool} AND creature_1_max_count = 3"));
            Assert.AreEqual(3L, Count(database, $"SELECT COUNT(*) AS Value FROM spawnpool_arrival WHERE pool_id = {MinosCavernsContent.EntrancePool}"));
            Assert.AreEqual(2L, Count(database, $"SELECT COUNT(*) AS Value FROM spawnpool_arrival WHERE id IN (35, 36) AND pool_id IN (568004, 568005)"), "PR105's rows, re-pointed");
            foreach (var id in MinosCavernsContent.StoodDownPools)
                Assert.AreEqual((long)MinosCavernsContent.Scripted, Count(database, $"SELECT mode AS Value FROM spawnpool WHERE id = {id}"), $"pool {id} stood down");
            Assert.AreEqual(4L, Count(database, $"SELECT creature_1_max_count AS Value FROM spawnpool WHERE id = {MinosCavernsContent.StoodDownPools[0]}"), "the GM's count kept");

            var destructibles = MinosCavernsContent.Destructibles().ToList();
            Assert.AreEqual(17, destructibles.Count, "nine nests, four cameras, four databanks");
            foreach (var d in destructibles)
                Assert.AreEqual((long)d.ClassId, Count(database, $"SELECT class_id AS Value FROM world_destructible WHERE id = {d.Id} AND map_context_id = 1347 AND pos_x = {d.X} AND pos_z = {d.Z}"), d.Comment);
            foreach (var u in MinosCavernsContent.Usables)
                Assert.AreEqual((long)u.Usable_, Count(database, $"SELECT usable AS Value FROM world_usable WHERE id = {u.Id} AND map_context_id = 1347 AND class_id = {u.ClassId} AND item_class_id = {u.ItemClass} AND item_template_id = {u.ItemTemplate}"), u.Comment);
            foreach (var c in MinosCavernsContent.Crates)
                Assert.AreEqual(1L, Count(database, $"SELECT COUNT(*) AS Value FROM world_container WHERE id = {c.Id} AND map_context_id = 1347 AND class_id = {c.ClassId} AND loot_profile_key = '{MinosCavernsContent.CrateLoot}'"), c.Comment);

            // Twice changes nothing.
            var once = Snapshot(database);
            foreach (var sql in MinosCavernsContent.Up())
                Exec(database, sql);
            CollectionAssert.AreEqual(once, Snapshot(database), "twice");

            migrator.Migrate(Previous);
            CollectionAssert.AreEqual(before, Snapshot(database), "Down puts back what it changed");
        }

        /// <summary>Everything stands on walkable ground of its map, at the floor's height.</summary>
        [TestMethod]
        public void EverythingStandsOnTheNavmesh()
        {
            var navmesh = Path.Combine(RepositoryRoot(), "navmesh");
            var minos = TestNavMeshes.Query(NavMeshFile.PathFor(navmesh, "adv_foreas_concordia_divide_minoscaverns"));
            var divide = TestNavMeshes.Query(NavMeshFile.PathFor(navmesh, "adv_foreas_concordia_divide"));

            var spots = new List<(string Who, uint Map, Vector3 At)>();
            spots.AddRange(MinosCavernsContent.AllPools().Select(p => (p.Comment, p.MapContextId, new Vector3((float)p.X, (float)p.Y, (float)p.Z))));
            spots.AddRange(MinosCavernsContent.Destructibles().Select(d => (d.Comment, MinosCavernsContent.Map, new Vector3((float)d.X, (float)d.Y, (float)d.Z))));
            spots.AddRange(MinosCavernsContent.Usables.Select(u => (u.Comment, MinosCavernsContent.Map, new Vector3((float)u.X, (float)u.Y, (float)u.Z))));
            spots.AddRange(MinosCavernsContent.Crates.Select(c => (c.Comment, MinosCavernsContent.Map, new Vector3((float)c.X, (float)c.Y, (float)c.Z))));
            spots.AddRange(MinosCavernsContent.Teleporters.Select(t => ($"teleporter of pool {t.Pool}", MinosCavernsContent.Map, new Vector3((float)t.X, (float)t.Y, (float)t.Z))));
            Assert.IsTrue(spots.Count > 70);

            foreach (var (who, map, at) in spots)
            {
                var query = map == MinosCavernsContent.Map ? minos : divide;
                var ground = query.Nearest(at + new Vector3(0, 0.2f, 0));

                Assert.IsTrue(ground.HasValue, $"{who} off the navmesh");
                Assert.IsTrue(new Vector2(ground.Value.X - at.X, ground.Value.Z - at.Z).Length() < 0.6f, $"{who} at {at}, nearest walkable {ground}");
                Assert.AreEqual(ground.Value.Y, at.Y, 0.5f, $"{who}: its floor");
            }

            // Every alarm pool stands at its own teleporter, and the exit pools' are not the cameras' - bar the chunnel turn, which both use.
            foreach (var t in MinosCavernsContent.Teleporters)
            {
                var pool = MinosCavernsContent.AllPools().Single(p => p.Id == t.Pool);
                Assert.AreEqual(0.0, Vector3.Distance(new Vector3((float)pool.X, (float)pool.Y, (float)pool.Z), new Vector3((float)t.X, (float)t.Y, (float)t.Z)), 0.01, $"pool {t.Pool}");
            }
            var cameraTeleporters = MinosCavernsContent.Teleporters.Where(t => t.Pool <= MinosCavernsContent.LastCameraPool).Select(t => t.EntityId).ToHashSet();
            var shared = MinosCavernsContent.Teleporters.Where(t => t.Pool >= MinosCavernsContent.FirstExitPool).Count(t => cameraTeleporters.Contains(t.EntityId));
            Assert.AreEqual(1, shared);
            // Ten of the .map's eleven running teleporters: the eleventh (0x7987000011D3) stands beside the one over the control room, and is left to PR105's stood-down pool.
            Assert.AreEqual(10, MinosCavernsContent.Teleporters.Select(t => t.EntityId).Distinct().Count());
            Assert.IsFalse(MinosCavernsContent.Teleporters.Any(t => t.EntityId == 0x7987000011D3UL));
        }

        /// <summary>The cameras watch the way players come, the charges reach their doors, and the Thrax pools the alarm names are these.</summary>
        [TestMethod]
        public void TheCamerasWatchTheWayInAndTheChargesReachTheirDoors()
        {
            var cameras = MinosCavernsContent.Destructibles().Where(d => d.ClassId == MinosCavernsContent.CameraClass).ToList();
            Assert.AreEqual(4, cameras.Count);

            // Where a player stands coming up: the approach to the base from the caverns, the hall
            // from the crossroads, and the north door's threshold.
            var approach = new Vector3(135f, 5f, 50f);
            var hall = new Vector3(190f, 3.5f, 72f);
            var threshold = new Vector3(204f, 2.3f, 88f);
            Assert.IsTrue(Sees(cameras[0], approach) && Sees(cameras[1], approach), "the two at the entrance watch the approach");
            Assert.IsTrue(Sees(cameras[2], hall), "the crossroads camera watches the hall");
            Assert.IsTrue(Sees(cameras[3], threshold), "the north door's camera watches its threshold");
            Assert.IsFalse(Sees(cameras[0], hall) || Sees(cameras[2], approach), "and not the other way");

            foreach (var (door, charge) in new[] { (MinosCavernsContent.NorthDoor, MinosCavernsContent.NorthCharge), (MinosCavernsContent.SouthDoor, MinosCavernsContent.SouthCharge) })
            {
                var d = MinosCavernsContent.Usables.Single(u => u.Id == door);
                var c = MinosCavernsContent.Usables.Single(u => u.Id == charge);
                var apart = Vector3.Distance(new Vector3((float)d.X, (float)d.Y, (float)d.Z), new Vector3((float)c.X, (float)c.Y, (float)c.Z));
                Assert.IsTrue(apart <= Math.Min(PlacedUsables.BlowOpenRadius, c.Radius), $"door {door} is {apart:F2} m from its charge");
                Assert.AreEqual(0u, d.Usable_, "shut to players");
                Assert.AreEqual(0u, d.Reset, "and stays open once blown");
                Assert.AreEqual(MinosCavernsContent.DetonatorClass, c.ItemClass);
                Assert.AreEqual(1u, c.ItemCount);
            }

            var site = BaneAlarms.Sites.Single(s => s.MapContextId == MinosCavernsContent.Map);
            CollectionAssert.AreEquivalent(MinosCavernsContent.Teleporters.Where(t => t.Pool <= MinosCavernsContent.LastCameraPool).Select(t => t.Pool).ToList(), site.CameraPools.ToList());
            CollectionAssert.AreEquivalent(MinosCavernsContent.Teleporters.Where(t => t.Pool >= MinosCavernsContent.FirstExitPool).Select(t => t.Pool).ToList(), site.ExitPools.ToList());
            CollectionAssert.Contains(site.DatabankClasses.ToList(), MinosCavernsContent.DatabankClass);
            Assert.AreEqual(BaneAlarms.CameraClass, MinosCavernsContent.CameraClass);
            Assert.AreEqual(BaneAlarms.AlarmLightClass, MinosCavernsContent.AlarmLightClass);
            Assert.AreEqual(BaneAlarms.SirenClass, MinosCavernsContent.SirenClass);
            foreach (var p in MinosCavernsContent.ReinforcementPools())
                Assert.AreEqual(MinosCavernsContent.Scripted, p.Mode, "woken by the alarm, never on their own");

            // The nine nests: two on the upper tunnel to the left, none in the tunnel from the medic (z over 90) or on the way to the Bane base (x over 100).
            var nests = MinosCavernsContent.Nests;
            Assert.AreEqual(9, nests.Length);
            Assert.AreEqual(2, nests.Count(n => n.Y > 25), "the upper tunnel's two");
            Assert.IsFalse(nests.Any(n => n.Z > 90 || n.X > 100));
        }

        /// <summary>The server takes every row as meant: the pools with their arrivals and poses, the objects as their kinds.</summary>
        [TestMethod]
        public void TheServerLoadsThemAsTheyAreMeant()
        {
            using var harness = WildernessRuntimeTestHarness.Create(mapContextId: MinosCavernsContent.Map);
            var map = harness.Map;

            var pools = harness.Maps.MapChannelArray.Values.SelectMany(channel => channel.SpawnPools).Concat(map.SpawnPools).DistinctBy(pool => pool.DbId).ToDictionary(pool => pool.DbId);
            foreach (var p in MinosCavernsContent.AllPools().Where(p => p.MapContextId == MinosCavernsContent.Map))
            {
                Assert.IsTrue(pools.TryGetValue(p.Id, out var pool), p.Comment);
                Assert.AreEqual((short)p.Mode, pool.Mode, p.Comment);
                Assert.AreEqual((NpcPose)p.Pose, pool.Pose, p.Comment);
                Assert.AreEqual(p.Id >= MinosCavernsContent.FirstCameraPool && p.Id <= MinosCavernsContent.LastExitPool ? 1 : 0, pool.Arrivals.Count, p.Comment);
            }
            Assert.AreEqual(3, pools[MinosCavernsContent.EntrancePool].Arrivals.Count, "the hall's Thrax through the hall's teleporters");
            Assert.AreEqual(SpawnPoolManager.ModeScripted, pools[MinosCavernsContent.StoodDownPools[0]].Mode);

            var creatures = harness.Creatures.LoadedCreatures;
            foreach (var c in MinosCavernsContent.Creatures)
            {
                Assert.IsTrue(creatures.TryGetValue(c.Id, out var creature), c.Comment);
                Assert.AreEqual(c.Level, creature.Level, c.Comment);
            }
            Assert.AreEqual(creatures[MinosCavernsContent.StockFrom].Npc.Vendor.VendorItems.Count, creatures[MinosCavernsContent.MedicalVendor].Npc.Vendor.VendorItems.Count);
            Assert.AreEqual(177u, creatures[MinosCavernsContent.Tyler].Npc.NpcPackageId, "Tyler talks");

            // Brought into the world: with their health, Tyler sitting on his spot, the warnets about their nest.
            harness.SpawnWorld(568041, MinosCavernsContent.FirstNestPool, 568032);
            var living = map.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList).Distinct().ToList();
            var tyler = living.Single(c => c.SpawnPool?.DbId == 568041);
            Assert.AreEqual(1000, tyler.Attributes[Attributes.Health].CurrentMax);
            Assert.IsTrue(Vector3.Distance(new Vector3(58f, -30.41f, -49f), tyler.Position) < 0.01f, $"Tyler at {tyler.Position}");
            Assert.AreEqual(NpcPose.Sitting, tyler.SpawnPool.Pose);
            var warnets = living.Where(c => c.SpawnPool?.DbId == MinosCavernsContent.FirstNestPool).ToList();
            Assert.IsTrue(warnets.Count is 3 or 4, $"{warnets.Count} warnets");
            Assert.IsTrue(warnets.All(w => w.Attributes[Attributes.Health].CurrentMax == 600 && w.Attributes[Attributes.Armor].CurrentMax == 50));
            Assert.IsTrue(warnets.All(w => Vector2.Distance(new Vector2(w.Position.X, w.Position.Z), new Vector2(-8f, 7f)) < 12f), "about their nest: " + string.Join("; ", warnets.Select(w => w.Position)));
            var kael = living.Where(c => c.SpawnPool?.DbId == 568032).ToList();
            Assert.AreEqual(2, kael.Count);
            Assert.IsTrue(kael.All(k => k.Attributes[Attributes.Health].CurrentMax == 1051));

            try
            {
                Assert.AreEqual(17, PlacedDestructibles.Load(harness.World.WorldDestructibleEntries.AsNoTracking().Where(row => row.MapContextId == MinosCavernsContent.Map).ToList()));
                Assert.AreEqual(17, PlacedDestructibles.Place(map));
                Assert.AreEqual(11, PlacedUsables.Load(harness.World.WorldUsableEntries.AsNoTracking().Where(row => row.MapContextId == MinosCavernsContent.Map).ToList()));
                Assert.AreEqual(11, PlacedUsables.Place(map));
                Assert.AreEqual(3, WorldContainers.Load(harness.World.WorldContainerEntries.AsNoTracking().Where(row => row.MapContextId == MinosCavernsContent.Map).ToList(), Array.Empty<Rasa.Structures.World.WorldContainerItemEntry>()));
                Assert.AreEqual(3, WorldContainers.Place(map));

                Assert.AreEqual(4, BaneAlarms.StandingCameras(map).Count);
                Assert.AreEqual(4, PlacedDestructibles.On(map).Count(live => BaneAlarms.IsDatabank(BaneAlarms.SiteOf(MinosCavernsContent.Map), live.Object)));
                Assert.AreEqual(5, BaneAlarms.Lights(map).Count, "four lights and the siren");

                var usables = PlacedUsables.On(map).ToDictionary(live => live.Placement.Id);
                Assert.AreEqual(PlacedUsables.Kind.Door, usables[MinosCavernsContent.NorthDoor].Placement.Kind);
                Assert.IsFalse(usables[MinosCavernsContent.NorthDoor].Placement.Usable);
                Assert.AreEqual(PlacedUsables.Kind.Bomb, usables[MinosCavernsContent.NorthCharge].Placement.Kind);
                Assert.IsTrue(usables[MinosCavernsContent.NorthCharge].Placement.Usable);
                Assert.AreEqual((uint)MinosCavernsContent.DetonatorClass, usables[MinosCavernsContent.NorthCharge].Placement.ItemClassId);
                Assert.AreEqual(MinosCavernsContent.ChargeFuseSeconds * 1000L, usables[MinosCavernsContent.NorthCharge].Placement.IntervalMinMs);
                Assert.AreEqual((float)MinosCavernsContent.ChargeRadius, usables[MinosCavernsContent.NorthCharge].Placement.Radius);
                Assert.AreEqual(PlacedUsables.Kind.Door, usables[MinosCavernsContent.PrisonDoor].Placement.Kind);
                Assert.IsTrue(usables[MinosCavernsContent.PrisonDoor].Placement.Usable);
                Assert.AreEqual(PlacedUsables.Kind.Dispenser, usables[MinosCavernsContent.MapChest].Placement.Kind);
                Assert.IsFalse(usables[MinosCavernsContent.MapChest].Placement.Usable);
            }
            finally
            {
                PlacedDestructibles.Load(null);
                PlacedDestructibles.Reset();
                PlacedUsables.Load(null);
                PlacedUsables.Reset();
                WorldContainers.Load(null, null);
                WorldContainers.Reset();
                BaneAlarms.Forget(map);
            }
        }

        [TestMethod]
        public void OnMySqlTheChangesGoInAndComeOut()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(Previous, Migration);
            var down = migrator.GenerateScript(Migration, Previous);

            foreach (var sql in MinosCavernsContent.Up())
                StringAssert.Contains(up, sql);
            StringAssert.Contains(up, $"'{Migration}'");
            foreach (var sql in MinosCavernsContent.Down())
                StringAssert.Contains(down, sql);
        }

        private static bool Sees(MinosCavernsContent.Destructible camera, Vector3 player)
        {
            var yaw = camera.Rotation;
            var facing = new Vector3((float)-Math.Sin(yaw), 0f, (float)-Math.Cos(yaw));
            return AbilityManager.InCone(new Vector3((float)camera.X, (float)camera.Y, (float)camera.Z), facing, player, BaneAlarms.CameraRange, BaneAlarms.CameraHalfAngle);
        }

        private static double R(double value) => Math.Round(value, 3);

        private static (uint Creature, double X, double Y, double Z, double Rotation) Pool(OrdinaryLootTestDatabase database, uint pool) =>
            database.Context.Database.SqlQueryRaw<PoolRow>(
                    $"SELECT creature_1_Id AS Creature, pos_x AS X, pos_y AS Y, pos_z AS Z, rotation AS Rotation FROM spawnpool WHERE id = {pool}")
                .AsEnumerable().Select(row => ((uint)row.Creature, R(row.X), R(row.Y), R(row.Z), R(row.Rotation))).Single();

        private static long Count(OrdinaryLootTestDatabase database, string sql) =>
            database.Context.Database.SqlQueryRaw<long>(sql).AsEnumerable().Single();

        private static void Exec(OrdinaryLootTestDatabase database, string sql) =>
            database.Context.Database.ExecuteSqlRaw(sql.Replace("{", "{{").Replace("}", "}}"));

        private static List<string> Snapshot(OrdinaryLootTestDatabase database)
        {
            var rows = new List<string>();
            foreach (var table in new[] { "creature", "creature_stat", "creature_action", "creature_appearance", "npc_package", "vendor", "vendor_item",
                         "spawnpool", "spawnpool_pose", "spawnpool_arrival", "world_destructible", "world_usable", "world_container" })
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

        private sealed class PoolRow
        {
            public long Creature { get; set; }
            public double X { get; set; }
            public double Y { get; set; }
            public double Z { get; set; }
            public double Rotation { get; set; }
        }
    }
}
