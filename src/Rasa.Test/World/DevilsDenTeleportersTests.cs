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
    /// The Devil's Den's local teleporters (Devils_den_teleporters, DevilsDenTeleporters): the pad
    /// in the entrance chamber and the one in Caritas's cell both ways, the comm tower's to the
    /// cell. None was linked, so none took anyone anywhere.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class DevilsDenTeleportersTests
    {
        private const string Migration = "20271004000000_Devils_den_teleporters";
        private const string Previous = "20271003000000_Torcastra_keymaster_computer";

        private static readonly uint[] Pads = { DevilsDenTeleporters.FithikHatchery, DevilsDenTeleporters.DetentionCenter, DevilsDenTeleporters.CommTower };

        [TestMethod]
        public void ThePadsAreLinkedAsTheWalkthroughHasThemAndDownTakesTheLinksOut()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            var before = Links(database);
            Assert.IsFalse(before.Any(link => Pads.Contains(link.From) || Pads.Contains(link.To)), "the Den's pads unlinked");

            migrator.Migrate(Migration);
            var after = Links(database);
            Assert.HasCount(before.Count + 3, after);
            CollectionAssert.Contains(after, (567u, 568u), "the entrance chamber to Caritas's cell");
            CollectionAssert.Contains(after, (568u, 567u), "and back");
            CollectionAssert.Contains(after, (569u, 568u), "the comm tower back to the cell");
            Assert.IsFalse(after.Any(link => link.To == DevilsDenTeleporters.CommTower), "nothing goes to the tower");

            migrator.Migrate(Previous);
            CollectionAssert.AreEquivalent(before, Links(database), "Down takes them out");
        }

        [TestMethod]
        public void TheUpRunsTwiceAndLeavesALinkAGmMade()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            database.Context.Database.ExecuteSqlRaw("INSERT INTO local_teleporter_link (from_teleporter_id, to_teleporter_id) VALUES (569, 567)");

            migrator.Migrate(Migration);
            var once = Links(database);
            CollectionAssert.Contains(once, (569u, 567u), "the GM's link stands");
            CollectionAssert.DoesNotContain(once, (569u, 568u));
            CollectionAssert.Contains(once, (567u, 568u));

            foreach (var sql in DevilsDenTeleporters.Up())
                database.Context.Database.ExecuteSqlRaw(sql);
            CollectionAssert.AreEquivalent(once, Links(database), "twice");

            migrator.Migrate(Previous);
            CollectionAssert.Contains(Links(database), (569u, 567u), "Down leaves the GM's link");
        }

        [TestMethod]
        public void OnMySqlTheLinksGoInAndComeOut()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(Previous, Migration);
            var down = migrator.GenerateScript(Migration, Previous);

            foreach (var sql in DevilsDenTeleporters.Up())
                StringAssert.Contains(up, sql);
            StringAssert.Contains(up, $"'{Migration}'");
            foreach (var sql in DevilsDenTeleporters.Down())
                StringAssert.Contains(down, sql);
        }

        /// <summary>Each pad on the Den's floor, so a player can step onto it and is set down on it.</summary>
        [TestMethod]
        public void EachPadStandsOnTheDensFloor()
        {
            using var database = new OrdinaryLootTestDatabase();
            database.Context.GetService<IMigrator>().Migrate(Migration);
            var rows = database.Context.TeleporterEntries.AsNoTracking().Where(t => Pads.Contains(t.Id)).ToList();
            var nav = TestNavMeshes.Query(NavMeshFile.PathFor(NavmeshDirectory(), "adv_foreas_concordia_palisades_devilsden"));

            Assert.HasCount(3, rows);
            foreach (var row in rows)
            {
                var pad = new Vector3((float)row.PosX, (float)row.PosY, (float)row.PosZ);
                var floor = nav.Nearest(pad);

                Assert.AreEqual((byte)1, row.Type, row.Description);
                Assert.AreEqual(DevilsDenTeleporters.MapContextId, row.MapContextId, row.Description);
                Assert.IsTrue(floor.HasValue, $"{row.Description} off the floor");
                Assert.IsLessThan(0.5f, Vector2.Distance(new Vector2(pad.X, pad.Z), new Vector2(floor.Value.X, floor.Value.Z)), $"{row.Description} over walkable ground");
                Assert.IsLessThan(0.5f, Math.Abs(floor.Value.Y - pad.Y), $"{row.Description}: the floor {floor.Value.Y - pad.Y} m from the row");
            }
        }

        /// <summary>Onto each pad: to the other end of its link, and not straight back.</summary>
        [TestMethod]
        public void SteppingOntoEachPadTakesThePlayerWhereItsLinkGoes()
        {
            using var database = new OrdinaryLootTestDatabase();
            database.Context.GetService<IMigrator>().Migrate(Migration);
            var rows = database.Context.TeleporterEntries.AsNoTracking().Where(t => Pads.Contains(t.Id)).ToDictionary(t => t.Id);
            var links = database.Context.LocalTeleporterLinkEntries.AsNoTracking().ToList().Where(l => Pads.Contains(l.FromTeleporterId)).ToList();
            Assert.HasCount(3, links);

            foreach (var link in links)
            {
                using var world = new WorldTestContext();
                var client = world.CreateClient();
                CellManager.Instance.AddToWorld(client);
                WorldTestContext.Drain(client);
                long now = 1000;
                var manager = WaypointTravelTests.CreateManager(world, clock: () => now);

                // The pad the player stands on at the world's origin; the others where they are from it.
                var origin = rows[link.FromTeleporterId];
                var placed = rows.Values.ToDictionary(row => row.Id, row => Place(manager, world.Map, row, origin));
                manager.LoadLocalTeleporterLinks(links);

                manager.DynamicObjectProximityWorker(world.Map, placed[link.FromTeleporterId], 0);
                Assert.IsNotNull(client.PendingTransfer, $"{link.FromTeleporterId} takes nobody");
                now += DynamicObjectManager.PassageHoldMs;
                manager.PassagesWorker(world.Map);

                var arrived = placed[link.ToTeleporterId].Position + new Vector3(0, 1, 0);
                Assert.IsLessThan(0.01f, Vector3.Distance(arrived, client.Player.Position), $"{link.FromTeleporterId} onto {link.ToTeleporterId}");
                Assert.IsTrue(placed[link.ToTeleporterId].TriggeredByPlayers.Contains(client), $"arrived on {link.ToTeleporterId}'s list, so it does not send them straight back");
            }
        }

        private static DynamicObject Place(DynamicObjectManager manager, MapChannel map, TeleporterEntry row, TeleporterEntry origin)
        {
            var obj = new DynamicObject
            {
                Position = new Vector3((float)(row.PosX - origin.PosX), (float)(row.PosY - origin.PosY), (float)(row.PosZ - origin.PosZ)),
                Rotation = row.Rotation,
                MapContextId = map.MapInfo.MapContextId,
                RuntimeMapChannel = map,
                DynamicObjectType = DynamicObjectType.LocalTeleporter,
                ObjectData = new WaypointInfo(row.Id, false, WaypointType.LocalTeleporter)
            };
            manager.Teleporters.Add(row.Id, obj);
            map.Teleporters.Add(row.Id, obj);
            return obj;
        }

        private static List<(uint From, uint To)> Links(OrdinaryLootTestDatabase database) =>
            database.Context.LocalTeleporterLinkEntries.AsNoTracking().ToList()
                .Select(link => (link.FromTeleporterId, link.ToTeleporterId)).ToList();

        private static string NavmeshDirectory()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "navmesh")))
                directory = directory.Parent;

            Assert.IsNotNull(directory, "navmesh not found");
            return Path.Combine(directory.FullName, "navmesh");
        }
    }
}
