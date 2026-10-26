extern alias RasaGame;

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
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Navigation;
    using Rasa.Services.Preloader;
    using Rasa.Structures;
    using Rasa.Structures.World;
    using Rasa.Test.Database;

    /// <summary>A local teleporter on each floor of every other outpost tower, linked both ways (Outpost_tower_teleporters, OutpostTowerTeleporters).</summary>
    [TestClass]
    [DoNotParallelize]
    public class OutpostTowerTeleportersTests
    {
        private const string Migration = "20270212000000_Outpost_tower_teleporters";
        private const string Previous = "20270205000000_Twin_pillars_npcs";

        /// <summary>The fourteen ArchHumOutpostTowerV03 the maps place outside the Wilderness: map, origin and yaw.</summary>
        private static readonly (uint Ground, string Map, double X, double Y, double Z, double Yaw)[] MapTowers =
        {
            (952, "adv_arieki_ligo_ashendesert_avernusoutpost", 22.408802032470703, 528.0, -92.89087677001953, -3.1415925025939906),
            (954, "adv_arieki_torden_abyss", 23.261749267578125, 538.3184814453125, -702.1138305664062, 0.0),
            (956, "adv_arieki_torden_plains", -41.100399017333984, 423.3533935546875, -101.1126480102539, 1.5707964953822935),
            (958, "adv_foreas_concordia_palisades", -722.0, 138.0, 588.0, -1.5707961582074996),
            (960, "adv_foreas_concordia_palisades", -704.0, 138.0, 680.0, -1.5707961582074996),
            (962, "adv_foreas_concordia_palisades", -969.3230590820312, 167.91018676757812, 629.6338500976562, -1.5707959896201027),
            (964, "adv_foreas_howlingmaw1", -563.5458984375, 226.93209838867188, -1212.46142578125, 0.0),
            (966, "adv_foreas_howlingmaw1", -710.5999755859375, 230.9099884033203, -1171.199951171875, 0.0),
            (968, "adv_wargame_edmundrange2", 151.97055053710938, 420.3587951660156, 149.96446228027344, 0.0),
            (970, "adv_wargame_edmundrange2", -268.9996337890625, 420.40264892578125, 189.49964904785156, -3.14158678054822),
            (972, "adv_wargame_edmundrange2", 151.9705810546875, 420.3587951660156, -106.03511047363281, 0.0),
            (974, "adv_wargame_edmundrange2", -268.9996337890625, 420.40264892578125, -66.50035095214844, -3.14158678054822),
            (976, "adv_wargame_provinggroundsv002", 151.97061157226562, 420.3589172363281, 149.96450805664062, 0.0),
            (978, "adv_wargame_provinggroundsv002", -268.9996337890625, 420.40264892578125, 189.49964904785156, -3.14158678054822),
        };

        /// <summary>The tower sunk in its hill, whose ground floor row is on the ground inside it.</summary>
        private const uint SunkTower = 966;

        [TestMethod]
        public void EachTowerHasAPadOnBothFloorsLinkedBothWays()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            var linksBefore = database.Context.LocalTeleporterLinkEntries.AsNoTracking().ToList();
            Assert.IsFalse(database.Context.TeleporterEntries.AsNoTracking().Any(row => row.Id >= OutpostTowerTeleporters.FirstTeleporter && row.Id <= OutpostTowerTeleporters.LastTeleporter), "952 to 979 free");

            migrator.Migrate(Migration);
            var maps = database.Context.MapInfoEntries.AsNoTracking().ToDictionary(map => map.MapName, map => map.Id);
            var teleporters = database.Context.TeleporterEntries.AsNoTracking().ToDictionary(row => row.Id);
            var links = database.Context.LocalTeleporterLinkEntries.AsNoTracking().ToList();
            Assert.HasCount(linksBefore.Count + 28, links);

            foreach (var tower in MapTowers)
            {
                // On the tower's pad: (-0.045, 0.57) from its origin, turned as the tower is.
                var (sin, cos) = Math.SinCos(tower.Yaw);
                var padX = tower.X + (-0.045 * cos + 0.57 * sin);
                var padZ = tower.Z + (0.045 * sin + 0.57 * cos);
                var ground = teleporters[tower.Ground];
                var upper = teleporters[tower.Ground + 1];

                foreach (var row in new[] { ground, upper })
                {
                    Assert.AreEqual((byte)1, row.Type, $"{row.Id}");
                    Assert.AreEqual((uint)maps[tower.Map], row.MapContextId, $"{row.Id}");
                    Assert.AreEqual(OutpostTowerTeleporters.TeleporterClass, row.ClassId, $"{row.Id}");
                    Assert.IsLessThan(0.15, Math.Sqrt(Math.Pow(row.PosX - padX, 2) + Math.Pow(row.PosZ - padZ, 2)), $"{row.Id} on its pad");
                }

                // As high over the origin as the Wilderness towers' rows: 1.0 m and 14.4 m.
                Assert.AreEqual(tower.Y + 14.4, upper.PosY, 0.01, $"{upper.Id} on the second floor");
                if (tower.Ground != SunkTower)
                    Assert.AreEqual(tower.Y + 1.0, ground.PosY, 0.01, $"{ground.Id} on the ground floor");
                else
                    Assert.AreEqual(2.4, ground.PosY - tower.Y, 0.1, "the sunk tower's ground floor row on the ground inside it");

                Assert.AreEqual(upper.Id, links.Single(link => link.FromTeleporterId == ground.Id).ToTeleporterId, $"{ground.Id} up");
                Assert.AreEqual(ground.Id, links.Single(link => link.FromTeleporterId == upper.Id).ToTeleporterId, $"{upper.Id} down");
            }

            migrator.Migrate(Previous);
            Assert.IsFalse(database.Context.TeleporterEntries.AsNoTracking().Any(row => row.Id >= OutpostTowerTeleporters.FirstTeleporter && row.Id <= OutpostTowerTeleporters.LastTeleporter), "Down takes the rows out");
            CollectionAssert.AreEquivalent(
                linksBefore.Select(link => (link.FromTeleporterId, link.ToTeleporterId)).ToList(),
                database.Context.LocalTeleporterLinkEntries.AsNoTracking().ToList().Select(link => (link.FromTeleporterId, link.ToTeleporterId)).ToList(),
                "and the links");
        }

        /// <summary>Over the navmesh, its floor at or within a metre over the row: one lands a metre over the row.</summary>
        [TestMethod]
        public void EveryPadStandsOnItsTowersWalkableFloor()
        {
            using var database = new OrdinaryLootTestDatabase();
            database.Context.GetService<IMigrator>().Migrate(Migration);
            var teleporters = database.Context.TeleporterEntries.AsNoTracking().ToDictionary(row => row.Id);
            var navmesh = Path.Combine(RepositoryRoot(), "navmesh");

            foreach (var group in MapTowers.GroupBy(tower => tower.Map))
            {
                var query = TestNavMeshes.Query(NavMeshFile.PathFor(navmesh, group.Key));

                foreach (var id in group.SelectMany(tower => new[] { tower.Ground, tower.Ground + 1 }))
                {
                    var row = teleporters[id];
                    var pad = new Vector3((float)row.PosX, (float)row.PosY, (float)row.PosZ);
                    var floor = query.Nearest(pad);

                    Assert.IsTrue(floor.HasValue, $"{id} off the navmesh");
                    Assert.IsLessThan(0.1f, Vector2.Distance(new Vector2(pad.X, pad.Z), new Vector2(floor.Value.X, floor.Value.Z)), $"{id} over walkable ground");
                    Assert.IsTrue(floor.Value.Y - pad.Y is >= 0 and < 1, $"{id}: the floor is {floor.Value.Y - pad.Y} m above the row");
                }
            }
        }

        /// <summary>The server keeps every link the migration made, and each takes a player to the other floor's pad.</summary>
        [TestMethod]
        public void SteppingOntoEachPadTakesThePlayerToTheOtherFloor()
        {
            using var database = new OrdinaryLootTestDatabase();
            database.Context.GetService<IMigrator>().Migrate(Migration);
            var teleporters = database.Context.TeleporterEntries.AsNoTracking().ToDictionary(row => row.Id);
            var links = database.Context.LocalTeleporterLinkEntries.AsNoTracking().ToList()
                .Where(link => link.FromTeleporterId >= OutpostTowerTeleporters.FirstTeleporter && link.FromTeleporterId <= OutpostTowerTeleporters.LastTeleporter).ToList();
            Assert.HasCount(28, links);

            foreach (var link in links)
            {
                using var world = new WorldTestContext();
                var client = world.CreateClient();
                CellManager.Instance.AddToWorld(client);
                WorldTestContext.Drain(client);
                long now = 1000;
                var manager = WaypointTravelTests.CreateManager(world, clock: () => now);

                // The pad the player stands on at the world's origin, the other floor's where it is from it.
                var origin = teleporters[link.FromTeleporterId];
                var from = Pad(manager, world.Map, origin, origin);
                Pad(manager, world.Map, teleporters[link.ToTeleporterId], origin);

                manager.LoadLocalTeleporterLinks(links);
                manager.DynamicObjectProximityWorker(world.Map, from, 0);
                Assert.IsNotNull(client.PendingTransfer, $"{link.FromTeleporterId} takes them");

                now += DynamicObjectManager.PassageHoldMs;
                manager.PassagesWorker(world.Map);

                var expected = Offset(teleporters[link.ToTeleporterId], origin) + new Vector3(0, 1, 0);
                Assert.IsLessThan(0.01f, Vector3.Distance(expected, client.Player.Position), $"{link.FromTeleporterId} onto {link.ToTeleporterId}");
            }
        }

        [TestMethod]
        public void TheUpRunsTwiceAndLeavesALinkAGmMade()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            database.Context.Database.ExecuteSqlRaw("INSERT INTO local_teleporter_link (from_teleporter_id, to_teleporter_id) VALUES (958, 960)");

            migrator.Migrate(Migration);
            var once = Snapshot(database);
            Assert.Contains("958 960", once, "the GM's link stands");
            Assert.DoesNotContain("958 959", once);
            Assert.Contains("959 958", once, "the rest go in");

            foreach (var sql in OutpostTowerTeleporters.Up())
                database.Context.Database.ExecuteSqlRaw(sql.Replace("{", "{{").Replace("}", "}}"));
            CollectionAssert.AreEqual(once, Snapshot(database), "twice");

            migrator.Migrate(Previous);
            Assert.Contains("958 960", Snapshot(database), "Down leaves the GM's link");
        }

        [TestMethod]
        public void OnMySqlTheRowsAndLinksGoInAndComeOut()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(Previous, Migration);
            var down = migrator.GenerateScript(Migration, Previous);

            foreach (var sql in OutpostTowerTeleporters.Up())
                StringAssert.Contains(up, sql);
            StringAssert.Contains(up, $"'{Migration}'");
            foreach (var sql in OutpostTowerTeleporters.Down())
                StringAssert.Contains(down, sql);
        }

        private static Vector3 Offset(TeleporterEntry row, TeleporterEntry origin) =>
            new((float)(row.PosX - origin.PosX), (float)(row.PosY - origin.PosY), (float)(row.PosZ - origin.PosZ));

        private static DynamicObject Pad(DynamicObjectManager manager, MapChannel map, TeleporterEntry row, TeleporterEntry origin)
        {
            var pad = new DynamicObject
            {
                Position = Offset(row, origin),
                Rotation = row.Rotation,
                MapContextId = map.MapInfo.MapContextId,
                RuntimeMapChannel = map,
                DynamicObjectType = DynamicObjectType.LocalTeleporter,
                ObjectData = new WaypointInfo(row.Id, false, WaypointType.LocalTeleporter)
            };
            manager.Teleporters.Add(row.Id, pad);
            map.Teleporters.Add(row.Id, pad);
            return pad;
        }

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>("SELECT from_teleporter_id || ' ' || to_teleporter_id AS Value FROM local_teleporter_link")
                .AsEnumerable().OrderBy(s => s, StringComparer.Ordinal).ToList();

        private static string RepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "navmesh")))
                directory = directory.Parent;

            return directory?.FullName ?? AppContext.BaseDirectory;
        }
    }
}
