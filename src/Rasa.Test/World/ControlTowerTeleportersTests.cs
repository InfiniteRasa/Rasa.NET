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

    /// <summary>
    /// A local teleporter on each floor of every ArchHumOutpostTowerV02 - the control points' and
    /// outposts' towers - linked both ways (Control_tower_teleporters, ControlTowerTeleporters).
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class ControlTowerTeleportersTests
    {
        private const string Migration = "20270922000000_Control_tower_teleporters";
        private const string Previous = "20270921000000_Lightning_fields_wreck_turret";

        /// <summary>The Torden Plains tower whose ground floor the navmesh leaves a gap in, over the pad.</summary>
        private const uint GapOverPad = 1141;

        [TestMethod]
        public void EachTowerHasAPadOnBothFloorsLinkedBothWays()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            var before = Teleporters(database);
            var linksBefore = Links(database);
            Assert.IsFalse(before.Keys.Any(InRange), "1100 to 1156 free");
            CollectionAssert.IsSubsetOf(new[] { (571u, 573u), (573u, 571u) }, linksBefore, "Research Point's ground pads linked to each other");

            // The original data's rows on nine of the towers' ground pads, where the V03 towers' are.
            foreach (var tower in ControlTowerTeleporters.Towers.Where(t => t.GroundWasThere))
                AssertOnPad(before[tower.Ground], tower, tower.Y + ControlTowerTeleporters.GroundRise, "the original row");

            migrator.Migrate(Migration);
            var teleporters = Teleporters(database);
            var links = Links(database);

            Assert.AreEqual(57, teleporters.Keys.Count(InRange), "fifty-seven new rows");
            Assert.HasCount(linksBefore.Count - 2 + 66, links, "the Research Point pair out, two links a tower in");

            foreach (var tower in ControlTowerTeleporters.Towers)
            {
                AssertOnPad(teleporters[tower.Ground], tower, tower.Y + ControlTowerTeleporters.GroundRise, "ground floor");
                AssertOnPad(teleporters[tower.Upper], tower, tower.Y + ControlTowerTeleporters.UpperRise, "second floor");

                Assert.AreEqual(tower.Upper, links.Single(link => link.From == tower.Ground).To, $"{tower.Ground} up");
                Assert.AreEqual(tower.Ground, links.Single(link => link.From == tower.Upper).To, $"{tower.Upper} down");
            }

            CollectionAssert.DoesNotContain(links, (571u, 573u));
            CollectionAssert.DoesNotContain(links, (573u, 571u));

            migrator.Migrate(Previous);
            var after = Teleporters(database);
            Assert.IsFalse(after.Keys.Any(InRange), "Down takes the new rows out");
            CollectionAssert.AreEquivalent(linksBefore, Links(database), "and puts the links back as they were");
            foreach (var tower in ControlTowerTeleporters.Towers.Where(t => t.GroundWasThere))
                Assert.IsTrue(after.ContainsKey(tower.Ground), $"{tower.Ground}, the original row, stays");
        }

        /// <summary>Over the navmesh, its floor at or within a metre over the row: one lands a metre over the row.</summary>
        [TestMethod]
        public void EveryPadStandsOnItsTowersWalkableFloor()
        {
            using var database = new OrdinaryLootTestDatabase();
            database.Context.GetService<IMigrator>().Migrate(Migration);
            var teleporters = Teleporters(database);
            var maps = database.Context.MapInfoEntries.AsNoTracking().ToDictionary(map => map.Id, map => map.MapName);
            var navmesh = Path.Combine(RepositoryRoot(), "navmesh");

            foreach (var group in ControlTowerTeleporters.Towers.GroupBy(tower => tower.MapContextId))
            {
                var query = TestNavMeshes.Query(NavMeshFile.PathFor(navmesh, maps[group.Key]));

                foreach (var id in group.SelectMany(tower => new[] { tower.Ground, tower.Upper }))
                {
                    var row = teleporters[id];
                    var pad = new Vector3((float)row.PosX, (float)row.PosY, (float)row.PosZ);
                    var floor = query.Nearest(pad);

                    Assert.IsTrue(floor.HasValue, $"{id} off the navmesh");
                    Assert.IsLessThan(id == GapOverPad ? 0.75f : 0.1f, Vector2.Distance(new Vector2(pad.X, pad.Z), new Vector2(floor.Value.X, floor.Value.Z)), $"{id} over walkable ground");
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
            var teleporters = Teleporters(database);
            var ours = ControlTowerTeleporters.Towers.SelectMany(t => new[] { t.Ground, t.Upper }).ToHashSet();
            var links = database.Context.LocalTeleporterLinkEntries.AsNoTracking().ToList()
                .Where(link => ours.Contains(link.FromTeleporterId)).ToList();
            Assert.HasCount(66, links);

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
            database.Context.Database.ExecuteSqlRaw("INSERT INTO local_teleporter_link (from_teleporter_id, to_teleporter_id) VALUES (549, 576)");

            migrator.Migrate(Migration);
            var once = Links(database);
            CollectionAssert.Contains(once, (549u, 576u), "the GM's link stands");
            CollectionAssert.DoesNotContain(once, (549u, 1150u));
            CollectionAssert.Contains(once, (1150u, 549u), "the rest go in");

            foreach (var sql in ControlTowerTeleporters.Up())
                database.Context.Database.ExecuteSqlRaw(sql.Replace("{", "{{").Replace("}", "}}"));
            CollectionAssert.AreEquivalent(once, Links(database), "twice");

            migrator.Migrate(Previous);
            CollectionAssert.Contains(Links(database), (549u, 576u), "Down leaves the GM's link");
        }

        [TestMethod]
        public void OnMySqlTheRowsAndLinksGoInAndComeOut()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(Previous, Migration);
            var down = migrator.GenerateScript(Migration, Previous);

            foreach (var sql in ControlTowerTeleporters.Up())
                StringAssert.Contains(up, sql);
            StringAssert.Contains(up, $"'{Migration}'");
            foreach (var sql in ControlTowerTeleporters.Down())
                StringAssert.Contains(down, sql);
        }

        private static bool InRange(uint id) => id >= ControlTowerTeleporters.FirstTeleporter && id <= ControlTowerTeleporters.LastTeleporter;

        private static void AssertOnPad(TeleporterEntry row, ControlTowerTeleporters.Tower tower, double y, string what)
        {
            Assert.AreEqual((byte)1, row.Type, $"{row.Id} {what}");
            Assert.AreEqual(tower.MapContextId, row.MapContextId, $"{row.Id} {what}");
            // The original rows are as the client's markers put them, up to 0.2 m off the pad's middle.
            var slack = row.Id < ControlTowerTeleporters.FirstTeleporter ? 0.25 : 0.01;
            Assert.IsLessThan(slack, Math.Sqrt(Math.Pow(row.PosX - tower.Pad.X, 2) + Math.Pow(row.PosZ - tower.Pad.Z, 2)), $"{row.Id} {what}: on its pad");
            Assert.AreEqual(y, row.PosY, 0.01, $"{row.Id} {what}: its height");
        }

        private static Dictionary<uint, TeleporterEntry> Teleporters(OrdinaryLootTestDatabase database) =>
            database.Context.TeleporterEntries.AsNoTracking().ToDictionary(row => row.Id);

        private static List<(uint From, uint To)> Links(OrdinaryLootTestDatabase database) =>
            database.Context.LocalTeleporterLinkEntries.AsNoTracking().ToList()
                .Select(link => (link.FromTeleporterId, link.ToTeleporterId)).ToList();

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

        private static string RepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "navmesh")))
                directory = directory.Parent;

            return directory?.FullName ?? AppContext.BaseDirectory;
        }
    }
}
