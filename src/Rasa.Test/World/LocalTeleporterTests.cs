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
    using ClientState = RasaGame::Rasa.Data.ClientState;
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Navigation;
    using Rasa.Packets;
    using Rasa.Packets.ClientMethod.Server;
    using Rasa.Packets.MapChannel.Client;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Services.Preloader;
    using Rasa.Structures;
    using Rasa.Structures.World;
    using Rasa.Test.Database;

    /// <summary>
    /// Local teleporters (teleporter rows of type 1): one takes whoever steps onto it to the one
    /// its local_teleporter_link row names, on the same map; one with no row takes nobody
    /// anywhere (DynamicObjectManager.LocalTeleporterWorker, LocalTeleporterLinks).
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class LocalTeleporterTests
    {
        private const string Migration = "20270106000000_Local_teleporter_links";
        private const string Before = "20270105000000_Alia_das_second_look";

        private static readonly Vector3 Near = Vector3.Zero;
        private static readonly Vector3 Far = new(200, 5, 0);

        [TestMethod]
        public void SteppingOntoALinkedPadTakesThePlayerToTheOtherEndAfterTheEffect()
        {
            using var world = new WorldTestContext();
            var client = Player(world);
            long now = 1000;
            var manager = WaypointTravelTests.CreateManager(world, clock: () => now);
            var near = Pad(manager, world.Map, 10, Near, 0);
            var far = Pad(manager, world.Map, 20, Far, 1.5);
            manager.LoadLocalTeleporterLinks(new[] { Link(10, 20), Link(20, 10) });

            manager.DynamicObjectProximityWorker(world.Map, near, 0);

            Assert.IsNotNull(client.PendingTransfer, "taken");
            Assert.AreEqual(Near, client.Player.Position, "held in the effect first");
            var held = Sent(client);
            Assert.IsTrue(held.Any(packet => packet is PreTeleportPacket), "the teleport-out effect");
            Assert.IsFalse(held.Any(packet => packet is TeleportPacket), "moved before the effect has played");
            Assert.IsFalse(held.Any(packet => packet is EnteredWaypointPacket || packet is WaypointGainedPacket), "no window, nothing gained");

            now += DynamicObjectManager.PassageHoldMs;
            manager.PassagesWorker(world.Map);
            Assert.AreEqual(Far + new Vector3(0, 1, 0), client.Player.Position, "onto the far pad");
            Assert.AreEqual(1.5, client.Player.Rotation, 1e-6, "facing as its row does");
            Assert.IsFalse(Sent(client).Any(packet => packet is ExitedWaypointPacket), "no window to close");

            manager.TeleportAcknowledge(client);
            Assert.AreEqual(ClientState.Ingame, client.State);
            Assert.IsNull(client.PendingTransfer);
            Assert.HasCount(0, client.Player.GainedWaypoints);

            // The far pad does not send them straight back.
            manager.DynamicObjectProximityWorker(world.Map, far, 0);
            manager.DynamicObjectProximityWorker(world.Map, near, 0);
            Assert.IsNull(client.PendingTransfer, "landed on, not stepped onto");
            CollectionAssert.Contains(far.TriggeredByPlayers, client);
            CollectionAssert.DoesNotContain(near.TriggeredByPlayers, client);
        }

        [TestMethod]
        public void SteppingOffTheFarPadAndOnAgainTakesThemBack()
        {
            using var world = new WorldTestContext();
            var client = Player(world);
            long now = 1000;
            var manager = WaypointTravelTests.CreateManager(world, clock: () => now);
            var near = Pad(manager, world.Map, 10, Near, 0);
            var far = Pad(manager, world.Map, 20, Far, 0);
            manager.LoadLocalTeleporterLinks(new[] { Link(10, 20), Link(20, 10) });

            manager.DynamicObjectProximityWorker(world.Map, near, 0);
            now += DynamicObjectManager.PassageHoldMs;
            manager.PassagesWorker(world.Map);
            manager.TeleportAcknowledge(client);

            client.Player.Position = Far + new Vector3(3, 1, 0);
            manager.DynamicObjectProximityWorker(world.Map, far, 0);
            Assert.IsNull(client.PendingTransfer, "beside it");
            CollectionAssert.DoesNotContain(far.TriggeredByPlayers, client, "off it");

            client.Player.Position = Far + new Vector3(0.5f, 1, 0);
            manager.DynamicObjectProximityWorker(world.Map, far, 0);
            Assert.IsNotNull(client.PendingTransfer, "on it again");

            now += DynamicObjectManager.PassageHoldMs;
            manager.PassagesWorker(world.Map);
            Assert.AreEqual(Near + new Vector3(0, 1, 0), client.Player.Position, "back");
        }

        [TestMethod]
        public void StandingOnThePadAfterBeingTakenDoesNotTakeThemAgain()
        {
            using var world = new WorldTestContext();
            var client = Player(world);
            long now = 1000;
            var manager = WaypointTravelTests.CreateManager(world, clock: () => now);
            var near = Pad(manager, world.Map, 10, Near, 0);
            Pad(manager, world.Map, 20, Far, 0);
            manager.LoadLocalTeleporterLinks(new[] { Link(10, 20) });

            manager.DynamicObjectProximityWorker(world.Map, near, 0);
            var transfer = client.PendingTransfer;
            manager.DynamicObjectProximityWorker(world.Map, near, 0);
            Assert.AreSame(transfer, client.PendingTransfer, "one teleport while the effect plays");

            // Died in the effect: called off, and still standing on it, which takes nobody again.
            var alive = client.Player.State;
            client.Player.State = CharacterState.Dead;
            now += DynamicObjectManager.PassageHoldMs;
            manager.PassagesWorker(world.Map);
            Assert.IsNull(client.PendingTransfer);
            client.Player.State = alive;
            manager.DynamicObjectProximityWorker(world.Map, near, 0);
            Assert.IsNull(client.PendingTransfer);
            Assert.AreEqual(Near, client.Player.Position);
        }

        [TestMethod]
        public void APadWithNoLinkTakesNobodyAnywhere()
        {
            using var world = new WorldTestContext();
            var client = Player(world);
            var manager = WaypointTravelTests.CreateManager(world);
            var pad = Pad(manager, world.Map, 10, Near, 0);
            Pad(manager, world.Map, 20, Far, 0);
            manager.LoadLocalTeleporterLinks(new[] { Link(20, 10) });

            manager.DynamicObjectProximityWorker(world.Map, pad, 0);

            Assert.IsNull(client.PendingTransfer);
            Assert.HasCount(0, pad.TriggeredByPlayers);
            Assert.HasCount(0, Sent(client));
        }

        [TestMethod]
        public void ALinkThatIsNotFromOneLocalTeleporterToAnotherOnItsMapIsLeftOut()
        {
            using var world = new WorldTestContext();
            var client = Player(world);
            var manager = WaypointTravelTests.CreateManager(world);
            var pad = Pad(manager, world.Map, 10, Near, 0);
            WaypointTravelTests.AddWaypoint(manager, world.Map, 30, Far);
            var elsewhere = new DynamicObject
            {
                Position = Far,
                MapContextId = world.Map.MapInfo.MapContextId + 1,
                DynamicObjectType = DynamicObjectType.LocalTeleporter,
                ObjectData = new WaypointInfo(40, false, WaypointType.LocalTeleporter)
            };
            manager.Teleporters.Add(40, elsewhere);

            foreach (var to in new uint[] { 10, 30, 40, 50 })
            {
                manager.LoadLocalTeleporterLinks(new[] { Link(10, to) });
                manager.DynamicObjectProximityWorker(world.Map, pad, 0);
                Assert.IsNull(client.PendingTransfer, $"to {to}");
            }
        }

        [TestMethod]
        public void TheElevenPairsAreLinkedBothWaysAndAshokaUpperIsMovedToItsMarker()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Before);
            var was = Teleporter(database, LocalTeleporterLinks.AshokaUpper);
            Assert.AreEqual(LocalTeleporterLinks.AshokaUpperWas, (was.PosX, was.PosY, was.PosZ));

            migrator.Migrate(Migration);
            var links = database.Context.LocalTeleporterLinkEntries.AsNoTracking().ToList();
            Assert.HasCount(22, links);
            Assert.HasCount(11, LocalTeleporterLinks.Pairs);

            var teleporters = database.Context.TeleporterEntries.AsNoTracking().ToDictionary(row => row.Id);
            foreach (var link in links)
            {
                Assert.IsTrue(links.Any(back => back.FromTeleporterId == link.ToTeleporterId && back.ToTeleporterId == link.FromTeleporterId),
                    $"{link.FromTeleporterId} -> {link.ToTeleporterId} goes both ways");

                var from = teleporters[link.FromTeleporterId];
                var to = teleporters[link.ToTeleporterId];
                Assert.AreEqual((byte)1, from.Type, $"{from.Id} is a local teleporter");
                Assert.AreEqual((byte)1, to.Type, $"{to.Id} is a local teleporter");
                Assert.AreEqual(from.MapContextId, to.MapContextId, $"{from.Id} and {to.Id} on one map");
                Assert.IsTrue(LocalTeleporterLinks.Pairs.Any(pair => pair.MapContextId == from.MapContextId), $"{from.Id} on the pair's map");
            }

            var upper = teleporters[LocalTeleporterLinks.AshokaUpper];
            Assert.AreEqual(LocalTeleporterLinks.AshokaUpperIs, (upper.PosX, upper.PosY, upper.PosZ));
            Assert.AreEqual(1734u, upper.MapContextId);

            // Thunderhead's, Stonewall's, Devil's Den's and Falcon Hold's are not linked.
            foreach (var unlinked in new uint[] { 537, 538, 539, 540, 542, 543, 544, 545, 546, 547, 548, 549, 550, 567, 568, 569, 570, 576 })
                Assert.IsFalse(links.Any(link => link.FromTeleporterId == unlinked || link.ToTeleporterId == unlinked), $"{unlinked}");

            migrator.Migrate(Before);
            Assert.AreEqual(0, database.Context.Database
                .SqlQueryRaw<int>($"select count(*) as Value from sqlite_master where type = 'table' and name = '{LocalTeleporterLinkEntry.TableName}'").AsEnumerable().Single());
            var back = Teleporter(database, LocalTeleporterLinks.AshokaUpper);
            Assert.AreEqual(LocalTeleporterLinks.AshokaUpperWas, (back.PosX, back.PosY, back.PosZ));
        }

        [TestMethod]
        public void AnAshokaUpperAGmHasMovedIsLeftWhereTheyPutIt()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Before);
            database.Context.Database.ExecuteSqlRaw($"update teleporter set pos_x = 500 where id = {LocalTeleporterLinks.AshokaUpper}");

            migrator.Migrate(Migration);
            Assert.AreEqual(500, Teleporter(database, LocalTeleporterLinks.AshokaUpper).PosX);

            migrator.Migrate(Before);
            Assert.AreEqual(500, Teleporter(database, LocalTeleporterLinks.AshokaUpper).PosX);
        }

        [TestMethod]
        public void EveryLinkedPadStandsOnItsMapsWalkableGround()
        {
            using var database = new OrdinaryLootTestDatabase();
            database.Context.GetService<IMigrator>().Migrate(Migration);
            var maps = database.Context.MapInfoEntries.AsNoTracking().ToDictionary(map => map.Id, map => map.MapName);
            var navmesh = Path.Combine(RepositoryRoot(), "navmesh");

            foreach (var group in LocalTeleporterLinks.Pairs.GroupBy(pair => pair.MapContextId))
            {
                var query = TestNavMeshes.Query(NavMeshFile.PathFor(navmesh, maps[group.Key]));

                foreach (var id in group.SelectMany(pair => new[] { pair.A, pair.B }))
                {
                    var row = Teleporter(database, id);
                    var pad = new Vector3((float)row.PosX, (float)row.PosY, (float)row.PosZ);
                    var ground = query.Nearest(pad);

                    Assert.IsTrue(ground.HasValue, $"{id} off the navmesh");
                    Assert.IsTrue(Vector2.Distance(new Vector2(pad.X, pad.Z), new Vector2(ground.Value.X, ground.Value.Z)) < 0.1f, $"{id} over walkable ground");
                    Assert.IsTrue(ground.Value.Y - pad.Y is >= 0 and < 1, $"{id}: the ground is {ground.Value.Y - pad.Y} m above the row; one lands from a metre above the row, and is within 2 m of it standing");
                }
            }
        }

        [TestMethod]
        public void OnMySqlTheTableAndItsRowsGoInAndComeOut()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(Before, Migration);
            var down = migrator.GenerateScript(Migration, Before);

            StringAssert.Contains(up, "CREATE TABLE `local_teleporter_link`");
            StringAssert.Contains(up, "PRIMARY KEY (`from_teleporter_id`)");
            StringAssert.Contains(up, "INSERT INTO local_teleporter_link (from_teleporter_id, to_teleporter_id) VALUES (551, 560);");
            StringAssert.Contains(up, "UPDATE teleporter SET pos_x = 495.26932, pos_y = 264.5, pos_z = -443.25302");
            StringAssert.Contains(down, "DROP TABLE `local_teleporter_link`");
            StringAssert.Contains(down, "UPDATE teleporter SET pos_x = 497.4414, pos_y = 286.6914, pos_z = -431.41406");
        }

        private static TeleporterEntry Teleporter(OrdinaryLootTestDatabase database, uint id) =>
            database.Context.TeleporterEntries.AsNoTracking().Single(row => row.Id == id);

        private static LocalTeleporterLinkEntry Link(uint from, uint to) =>
            new() { FromTeleporterId = from, ToTeleporterId = to };

        private static DynamicObject Pad(DynamicObjectManager manager, MapChannel map, uint id, Vector3 position, double rotation)
        {
            var pad = new DynamicObject
            {
                Position = position,
                Rotation = rotation,
                MapContextId = map.MapInfo.MapContextId,
                RuntimeMapChannel = map,
                DynamicObjectType = DynamicObjectType.LocalTeleporter,
                ObjectData = new WaypointInfo(id, false, WaypointType.LocalTeleporter)
            };
            manager.Teleporters.Add(id, pad);
            map.Teleporters.Add(id, pad);
            return pad;
        }

        private static Client Player(WorldTestContext world)
        {
            var client = world.CreateClient();
            CellManager.Instance.AddToWorld(client);
            WorldTestContext.Drain(client);
            return client;
        }

        private static List<PythonPacket> Sent(Client client) =>
            WorldTestContext.Drain(client).Select(p => p.Message).OfType<CallMethodMessage>().Select(p => p.Packet).ToList();

        private static string RepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "navmesh")))
                directory = directory.Parent;

            return directory?.FullName ?? AppContext.BaseDirectory;
        }
    }
}
