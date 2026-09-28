extern alias RasaGame;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using ClientState = RasaGame::Rasa.Data.ClientState;
    using Rasa.Data;
    using Rasa.Game.Handlers;
    using Rasa.Managers;
    using Rasa.Packets;
    using Rasa.Packets.MapChannel.Client;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Structures;
    using Rasa.Structures.Char;

    [TestClass]
    [DoNotParallelize]
    public class DropshipTravelTests
    {
        [TestMethod]
        public void BoardingUsesDestinationMetadataAndDepartsFromTheSourceMap()
        {
            using var world = new WorldTestContext();
            world.AddClass(EntityClasses.UsableCrSpawnerHumDropshipV01);
            var client = world.CreateClient();
            CellManager.Instance.AddToWorld(client);
            var destination = CreateDestination();
            var maps = new MapChannelManager(null);
            maps.MapChannelArray.Add(1220, world.Map);
            maps.MapChannelArray.Add(1300, destination);
            var saved = 0;
            var manager = new DynamicObjectManager(null, maps, () => 1000, (_, update, _) =>
            {
                if (update == CharacterUpdate.Position)
                    saved++;
            });
            WaypointTravelTests.AddWaypoint(manager, world.Map, 10, Vector3.Zero, type: WaypointType.Dropship);
            WaypointTravelTests.AddWaypoint(manager, destination, 20, new Vector3(400, 5, 0), type: WaypointType.Dropship);
            client.Player.GainedWaypoints.Add(new CharacterTeleporterEntry(client.Player.Id, 20, (byte)WaypointType.Dropship));

            manager.SelectWaypoint(client, new SelectWaypointPacket { MapInstanceId = 1, WaypointId = 20 });

            Assert.IsNotNull(client.PendingTransfer);
            Assert.IsTrue(client.PendingTransfer.IsDropship);
            Assert.AreEqual(1300U, client.LoadingMap);
            Assert.IsFalse(client.HandleMovement(new Rasa.Models.Movement(Vector3.One, Vector2.Zero)));
            var ship = manager.Dropships[client.PendingTransfer.DropshipId];
            try
            {
                for (var step = 0; step < 6; step++)
                    manager.DropshipsWorker(world.Map, Math.Max(0, ship.PhaseTimeleft));

                Assert.IsTrue(client.PendingTransfer.HasDeparted);
                Assert.AreSame(destination, client.Player.MapChannel);
                Assert.AreEqual(1300U, client.Player.MapContextId);
                Assert.AreEqual(new Vector3(400, 5, 0), client.Player.Position);
                Assert.AreEqual(ClientState.Teleporting, client.State);
                Assert.IsFalse(world.Map.ClientList.Contains(client));
                Assert.IsFalse(manager.Dropships.ContainsKey(ship.EntityId));
                Assert.IsTrue(manager.IsExpectedMapLoad(client));
                Assert.AreEqual(0, saved);

                destination.ClientList.Add(client);
                CellManager.Instance.AddToWorld(client);
                manager.CompleteMapLoadTransfer(client);
                Assert.IsNull(client.PendingTransfer);
                Assert.AreEqual(1, saved);
                var arrival = new Dropship(
                    Factions.AFS,
                    DropshipType.Teleporter,
                    client,
                    DropshipRole.Arrival);
                CellManager.Instance.AddToWorld(destination, arrival);
                manager.Dropships.Add(arrival.EntityId, arrival);
                for (var step = 0; step < 6; step++)
                    manager.DropshipsWorker(destination, Math.Max(0, arrival.PhaseTimeleft));
                Assert.AreEqual(ClientState.Ingame, client.State);
                Assert.AreEqual(1, destination.ClientList.Count);
                Assert.IsFalse(manager.IsExpectedMapLoad(client));
            }
            finally
            {
                foreach (var remaining in new List<Dropship>(manager.Dropships.Values))
                {
                    var map = remaining.MapContextId == 1220 ? world.Map : destination;
                    CellManager.Instance.RemoveFromWorld(map, remaining);
                    manager.Dropships.Remove(remaining.EntityId);
                }
                destination.ClientList.Clear();
                destination.MapCellInfo.Cells.Clear();
            }
        }

        [TestMethod]
        public void DepartedDropshipAcceptsMapLoadedThroughTheProductionHandler()
        {
            using var world = new WorldTestContext();
            world.AddClass(EntityClasses.UsableCrSpawnerHumDropshipV01);
            var client = world.CreateClient();
            CellManager.Instance.AddToWorld(client);
            var destination = CreateDestination();
            var saved = 0;
            var maps = CreateMaps(world, destination);
            var manager = CreateDropshipManager(maps, (_, update, _) =>
            {
                if (update == CharacterUpdate.Position)
                    saved++;
            });
            using var managers = new ManagerInstances(maps, manager);
            StartDropshipTransfer(world, destination, client, manager);

            Depart(world.Map, manager, client);

            Assert.IsTrue(client.AwaitingMapLoaded);
            RouteMapLoaded(client);

            Assert.IsNull(client.PendingTransfer);
            Assert.AreEqual(1, saved);
            Assert.AreEqual(1, destination.ClientList.Count(member => member == client));
            Assert.IsTrue(CellManager.Instance.IsInWorld(client));

            LandArrival(destination, manager);
            Assert.AreEqual(ClientState.Ingame, client.State);
        }

        [TestMethod]
        public void StaleMapLoadedDoesNotPublishASecondDropshipArrival()
        {
            using var world = new WorldTestContext();
            world.AddClass(EntityClasses.UsableCrSpawnerHumDropshipV01);
            var client = world.CreateClient();
            CellManager.Instance.AddToWorld(client);
            var destination = CreateDestination();
            var maps = CreateMaps(world, destination);
            var manager = CreateDropshipManager(maps);
            using var managers = new ManagerInstances(maps, manager);
            StartDropshipTransfer(world, destination, client, manager);
            Depart(world.Map, manager, client);
            RouteMapLoaded(client);
            var arrivals = manager.Dropships.Count;
            WorldTestContext.Drain(client);

            RouteMapLoaded(client);

            Assert.AreEqual(arrivals, manager.Dropships.Count);
            Assert.AreEqual(0, WorldTestContext.Drain(client).Count);
            Assert.AreEqual(1, destination.ClientList.Count(member => member == client));
        }

        [TestMethod]
        public void MismatchedMapLoadedDoesNotPublishDropshipArrival()
        {
            using var world = new WorldTestContext();
            world.AddClass(EntityClasses.UsableCrSpawnerHumDropshipV01);
            var client = world.CreateClient();
            CellManager.Instance.AddToWorld(client);
            var destination = CreateDestination();
            var maps = CreateMaps(world, destination);
            var manager = CreateDropshipManager(maps);
            using var managers = new ManagerInstances(maps, manager);
            StartDropshipTransfer(world, destination, client, manager);
            Depart(world.Map, manager, client);
            Assert.IsTrue(client.AwaitingMapLoaded);
            client.LoadingMap = world.Map.MapInfo.MapContextId;
            WorldTestContext.Drain(client);

            RouteMapLoaded(client);

            Assert.IsNotNull(client.PendingTransfer);
            Assert.AreEqual(ClientState.Teleporting, client.State);
            Assert.AreEqual(0, destination.ClientList.Count);
            Assert.IsFalse(CellManager.Instance.IsInWorld(client));
            Assert.AreEqual(0, WorldTestContext.Drain(client).Count);
        }

        [TestMethod]
        public void DropshipPersistenceFailurePublishesNoDestinationArrival()
        {
            using var world = new WorldTestContext();
            world.AddClass(EntityClasses.UsableCrSpawnerHumDropshipV01);
            var client = world.CreateClient();
            CellManager.Instance.AddToWorld(client);
            var destination = CreateDestination();
            var observer = world.CreateClient();
            world.Map.ClientList.Remove(observer);
            observer.Player.MapChannel = destination;
            observer.Player.MapContextId = destination.MapInfo.MapContextId;
            destination.ClientList.Add(observer);
            CellManager.Instance.AddToWorld(observer);
            var maps = CreateMaps(world, destination);
            var manager = CreateDropshipManager(maps,
                (_, _, _) => throw new DbUpdateException("Fixture persistence failure."),
                current => current.State = ClientState.Disconnected);
            using var managers = new ManagerInstances(maps, manager);
            StartDropshipTransfer(world, destination, client, manager);
            Depart(world.Map, manager, client);
            WorldTestContext.Drain(client);
            WorldTestContext.Drain(observer);

            RouteMapLoaded(client);

            Assert.AreEqual(ClientState.Disconnected, client.State);
            Assert.IsNull(client.PendingTransfer);
            Assert.AreSame(world.Map, client.Player.MapChannel);
            Assert.AreEqual(Vector3.Zero, client.Player.Position);
            Assert.IsFalse(destination.ClientList.Contains(client));
            Assert.IsFalse(destination.MapCellInfo.Cells.Values.Any(cell => cell.ClientList.Contains(client)));
            Assert.IsFalse(WorldTestContext.Drain(client).Any(packet =>
                packet.Message is CallMethodMessage call && call.Packet is TeleportArrivalPacket));
            Assert.AreEqual(0, WorldTestContext.Drain(observer).Count);
        }

        [TestMethod]
        public void AMapTickDoesNotAdvanceAnotherMapsDropship()
        {
            using var world = new WorldTestContext();
            var client = world.CreateClient();
            var ship = new Dropship(
                Factions.AFS,
                DropshipType.Teleporter,
                client,
                DropshipRole.Departure);
            var manager = new DynamicObjectManager(null);
            manager.Dropships.Add(ship.EntityId, ship);
            try
            {
                manager.DropshipsWorker(CreateDestination(), 1000);

                Assert.AreEqual(5000L, ship.PhaseTimeleft);
                Assert.AreEqual((byte)0, ship.Phase);
            }
            finally
            {
                manager.Dropships.Remove(ship.EntityId);
                EntityManager.Instance.FreeEntity(ship.EntityId);
            }
        }

        internal static MapChannel CreateDestination()
        {
            return new MapChannel
            {
                MapInfo = new MapInfo(1300, "adv_foreas_fixture_destination", 1, 0),
                ClientList = new List<Rasa.Game.Client>(),
                PlayerLimit = 128
            };
        }

        private static MapChannelManager CreateMaps(WorldTestContext world, MapChannel destination,
            Action<Rasa.Game.Client, CharacterUpdate, object> update = null)
        {
            var maps = new MapChannelManager(null, () => 1000,
                updateCharacter: update ?? ((_, _, _) => { }),
                refreshStats: (_, _) => { }, assignPlayer: _ => { }, enterMapChannels: _ => { });
            maps.MapChannelArray.Add(world.Map.MapInfo.MapContextId, world.Map);
            maps.MapChannelArray.Add(destination.MapInfo.MapContextId, destination);
            return maps;
        }

        private static DynamicObjectManager CreateDropshipManager(MapChannelManager maps,
            Action<Rasa.Game.Client, CharacterUpdate, object> update = null,
            Action<Rasa.Game.Client> disconnect = null) =>
            new(null, maps, () => 1000, update ?? ((_, _, _) => { }),
                disconnect ?? (current => current.State = ClientState.Disconnected));

        private static void StartDropshipTransfer(WorldTestContext world, MapChannel destination,
            Rasa.Game.Client client, DynamicObjectManager manager)
        {
            WaypointTravelTests.AddWaypoint(manager, world.Map, 10, Vector3.Zero, type: WaypointType.Dropship);
            WaypointTravelTests.AddWaypoint(manager, destination, 20, new Vector3(400, 5, 0),
                type: WaypointType.Dropship);
            client.Player.GainedWaypoints.Add(
                new CharacterTeleporterEntry(client.Player.Id, 20, (byte)WaypointType.Dropship));
            manager.SelectWaypoint(client,
                new SelectWaypointPacket { MapInstanceId = 1, WaypointId = 20 });
        }

        private static void Depart(MapChannel origin, DynamicObjectManager manager, Rasa.Game.Client client)
        {
            var ship = manager.Dropships[client.PendingTransfer.DropshipId];
            for (var step = 0; step < 6; step++)
                manager.DropshipsWorker(origin, Math.Max(0, ship.PhaseTimeleft));
        }

        private static void LandArrival(MapChannel destination, DynamicObjectManager manager)
        {
            var arrival = manager.Dropships.Values.Single();
            for (var step = 0; step < 6; step++)
                manager.DropshipsWorker(destination, Math.Max(0, arrival.PhaseTimeleft));
        }

        private static void RouteMapLoaded(Rasa.Game.Client client)
        {
            var handler = new ClientPacketHandler();
            handler.RegisterClient(client);
            new PacketRouter<ClientPacketHandler, GameOpcode>().RoutePacket(handler, new MapLoadedPacket());
        }

        private sealed class ManagerInstances : IDisposable
        {
            private readonly FieldInfo _mapsField = typeof(MapChannelManager)
                .GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
            private readonly FieldInfo _objectsField = typeof(DynamicObjectManager)
                .GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
            private readonly object _previousMaps;
            private readonly object _previousObjects;
            private readonly MapChannelManager _maps;
            private readonly DynamicObjectManager _objects;

            internal ManagerInstances(MapChannelManager maps, DynamicObjectManager objects)
            {
                _maps = maps;
                _objects = objects;
                _previousMaps = _mapsField.GetValue(null);
                _previousObjects = _objectsField.GetValue(null);
                _mapsField.SetValue(null, maps);
                _objectsField.SetValue(null, objects);
            }

            public void Dispose()
            {
                foreach (var dropship in _objects.Dropships.Values.ToArray())
                {
                    if (_maps.MapChannelArray.TryGetValue(dropship.MapContextId, out var map))
                        CellManager.Instance.RemoveFromWorld(map, dropship);
                    else
                        EntityManager.Instance.FreeEntity(dropship.EntityId);
                    _objects.Dropships.Remove(dropship.EntityId);
                }
                _mapsField.SetValue(null, _previousMaps);
                _objectsField.SetValue(null, _previousObjects);
            }
        }
    }
}
