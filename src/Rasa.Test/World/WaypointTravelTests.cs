extern alias RasaGame;

using System;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using ClientState = RasaGame::Rasa.Data.ClientState;
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Packets.MapChannel.Client;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Structures;
    using Rasa.Structures.Char;

    [TestClass]
    [DoNotParallelize]
    public class WaypointTravelTests
    {
        [TestMethod]
        public void DiscoveryPersistsBeforeUpdatingRuntimeOrSendingTheGrant()
        {
            using var world = new WorldTestContext();
            var client = world.CreateClient();
            var manager = new DynamicObjectManager(null, updateCharacter: (_, _, _) =>
                throw new InvalidOperationException("Persistence unavailable."));

            Assert.ThrowsExactly<InvalidOperationException>(() =>
                manager.CheckPlayerWaypoint(client, new WaypointInfo(10, false, WaypointType.Waypoint)));

            Assert.AreEqual(0, client.Player.GainedWaypoints.Count);
            Assert.AreEqual(0, WorldTestContext.Drain(client).Count);
        }

        [TestMethod]
        public void LocalTravelUsesTheAdvertisedInstanceAndOneAuthoritativePosition()
        {
            using var world = new WorldTestContext();
            var client = world.CreateClient();
            CellManager.Instance.AddToWorld(client);
            WorldTestContext.Drain(client);
            var saved = 0;
            var manager = CreateManager(world, (_, update, _) =>
            {
                if (update == CharacterUpdate.Position)
                    saved++;
            });
            AddWaypoint(manager, world.Map, 10, Vector3.Zero);
            AddWaypoint(manager, world.Map, 20, new Vector3(200, 5, 0));
            client.Player.GainedWaypoints.Add(new CharacterTeleporterEntry(client.Player.Id, 20, (byte)WaypointType.Waypoint));

            manager.SelectWaypoint(client, new SelectWaypointPacket { MapInstanceId = 1, WaypointId = 20 });

            Assert.AreEqual(ClientState.Teleporting, client.State);
            Assert.IsNotNull(client.PendingTransfer);
            Assert.AreEqual(new Vector3(200, 6, 0), client.Player.Position);
            Assert.AreEqual(client.Player.Position, client.Movement.Position);
            var teleport = WorldTestContext.Drain(client).Select(p => p.Message)
                .OfType<CallMethodMessage>().Select(p => p.Packet).OfType<TeleportPacket>().Single();
            Assert.AreEqual(client.Player.Position, teleport.Position);
            Assert.AreEqual(0, saved);

            manager.TeleportAcknowledge(client);

            Assert.AreEqual(ClientState.Ingame, client.State);
            Assert.IsNull(client.PendingTransfer);
            Assert.AreEqual(1, saved);
            manager.TeleportAcknowledge(client);
            Assert.AreEqual(1, saved);
        }

        [TestMethod]
        [DataRow(false, false)]
        [DataRow(true, true)]
        public void UndiscoveredOrContestedDestinationsAreRejected(bool discovered, bool contested)
        {
            using var world = new WorldTestContext();
            var client = world.CreateClient();
            CellManager.Instance.AddToWorld(client);
            WorldTestContext.Drain(client);
            var manager = CreateManager(world);
            AddWaypoint(manager, world.Map, 10, Vector3.Zero);
            AddWaypoint(manager, world.Map, 20, new Vector3(200, 0, 0), contested);
            if (discovered)
                client.Player.GainedWaypoints.Add(new CharacterTeleporterEntry(client.Player.Id, 20, (byte)WaypointType.Waypoint));

            manager.SelectWaypoint(client, new SelectWaypointPacket { MapInstanceId = 1, WaypointId = 20 });

            Assert.AreEqual(Vector3.Zero, client.Player.Position);
            Assert.IsNull(client.PendingTransfer);
            Assert.IsTrue(WorldTestContext.Drain(client).Any(p => p.Message is CallMethodMessage c &&
                c.Packet is TeleportFailedPacket));
        }

        [TestMethod]
        public void ADiscoveredDestinationStillRequiresProximityToAStation()
        {
            using var world = new WorldTestContext();
            var client = world.CreateClient();
            CellManager.Instance.AddToWorld(client);
            WorldTestContext.Drain(client);
            var manager = CreateManager(world);
            AddWaypoint(manager, world.Map, 20, new Vector3(200, 0, 0));
            client.Player.GainedWaypoints.Add(new CharacterTeleporterEntry(client.Player.Id, 20, (byte)WaypointType.Waypoint));

            manager.SelectWaypoint(client, new SelectWaypointPacket { MapInstanceId = 1, WaypointId = 20 });

            Assert.AreEqual(Vector3.Zero, client.Player.Position);
            Assert.IsNull(client.PendingTransfer);
        }

        [TestMethod]
        public void MapContextCannotBeUsedAsAnInstanceAlias()
        {
            using var world = new WorldTestContext();
            var client = world.CreateClient();
            CellManager.Instance.AddToWorld(client);
            WorldTestContext.Drain(client);
            var manager = CreateManager(world);
            AddWaypoint(manager, world.Map, 10, Vector3.Zero);
            AddWaypoint(manager, world.Map, 20, new Vector3(200, 0, 0));
            client.Player.GainedWaypoints.Add(new CharacterTeleporterEntry(client.Player.Id, 20, (byte)WaypointType.Waypoint));

            manager.SelectWaypoint(client, new SelectWaypointPacket { MapInstanceId = 1220, WaypointId = 20 });

            Assert.AreEqual(Vector3.Zero, client.Player.Position);
            Assert.IsNull(client.PendingTransfer);
            Assert.IsTrue(WorldTestContext.Drain(client).Any(p => p.Message is CallMethodMessage c &&
                c.Packet is TeleportFailedPacket));
        }

        [TestMethod]
        public void UnsolicitedAcknowledgementDoesNotCompleteTravel()
        {
            using var world = new WorldTestContext();
            var client = world.CreateClient();

            CreateManager(world).TeleportAcknowledge(client);

            Assert.AreEqual(0, WorldTestContext.Drain(client).Count);
            Assert.AreEqual(ClientState.Ingame, client.State);
        }

        [TestMethod]
        public void TransferExpiresAtTheConfiguredDeadlineAndRestoresItsOrigin()
        {
            using var world = new WorldTestContext();
            var client = world.CreateClient();
            CellManager.Instance.AddToWorld(client);
            long now = 1000;
            var manager = CreateManager(world, clock: () => now);
            AddWaypoint(manager, world.Map, 10, Vector3.Zero);
            AddWaypoint(manager, world.Map, 20, new Vector3(200, 5, 0));
            client.Player.GainedWaypoints.Add(new CharacterTeleporterEntry(client.Player.Id, 20, (byte)WaypointType.Waypoint));
            manager.SelectWaypoint(client, new SelectWaypointPacket { MapInstanceId = 1, WaypointId = 20 });

            now = 60999;
            Assert.IsFalse(manager.CheckTransferTimeout(client));
            Assert.AreEqual(ClientState.Teleporting, client.State);
            now = 61000;
            Assert.IsTrue(manager.CheckTransferTimeout(client));

            Assert.AreEqual(ClientState.Disconnected, client.State);
            Assert.AreEqual(Vector3.Zero, client.Player.Position);
            Assert.AreEqual(Vector3.Zero, client.Movement.Position);
            Assert.IsNull(client.PendingTransfer);
        }

        [TestMethod]
        public void WaypointMenusListTheMapInstanceOnlyOnce()
        {
            using var world = new WorldTestContext();
            var client = world.CreateClient();
            var manager = CreateManager(world);
            AddWaypoint(manager, world.Map, 10, Vector3.Zero);
            AddWaypoint(manager, world.Map, 20, new Vector3(200, 0, 0));
            foreach (var id in new uint[] { 10, 20 })
                client.Player.GainedWaypoints.Add(new CharacterTeleporterEntry(client.Player.Id, id, (byte)WaypointType.Waypoint));

            var menu = manager.CreateListOfWaypoints(client, WaypointType.Waypoint)[1220];

            Assert.AreEqual(2, menu.Waypoints.Count);
            Assert.AreEqual(1, menu.MapInstanceList.Count);
            Assert.AreEqual(1U, menu.MapInstanceList[0].MapInstanceId);
            Assert.AreEqual(1220U, menu.MapInstanceList[0].MapContextId);
        }

        [TestMethod]
        public void DropshipMenusContainOnlyDiscoveredUncontestedStations()
        {
            using var world = new WorldTestContext();
            var client = world.CreateClient();
            var manager = CreateManager(world);
            AddWaypoint(manager, world.Map, 10, new Vector3(1, 2, 3), type: WaypointType.Dropship);
            AddWaypoint(manager, world.Map, 20, new Vector3(4, 5, 6), type: WaypointType.Dropship);
            AddWaypoint(manager, world.Map, 30, new Vector3(7, 8, 9), true, WaypointType.Dropship);
            foreach (var id in new uint[] { 10, 30 })
                client.Player.GainedWaypoints.Add(new CharacterTeleporterEntry(client.Player.Id, id, (byte)WaypointType.Dropship));

            var menu = manager.CreateListOfDropships(client)[1220];

            Assert.AreEqual(1, menu.Waypoints.Count);
            Assert.AreEqual(10U, menu.Waypoints[0].WaypointId);
            Assert.AreEqual(new Vector3(1, 2, 3), menu.Waypoints[0].Position);
        }

        [TestMethod]
        public void DropshipDiscoveryWorksAcrossACellBoundaryAndExitClearsTheTrigger()
        {
            using var world = new WorldTestContext();
            var client = world.CreateClient(25);
            CellManager.Instance.AddToWorld(client);
            WorldTestContext.Drain(client);
            var manager = CreateManager(world);
            var position = new Vector3(27, 0, 0);
            AddWaypoint(manager, world.Map, 10, position, type: WaypointType.Dropship);
            var trigger = new MapTrigger(10, "fixture-station", position, 0, 1220);
            CellManager.Instance.AddToWorld(world.Map, trigger);
            var triggers = new MapTriggerManager(manager);

            triggers.TriggersProximityWorker(world.Map);

            Assert.IsTrue(client.Player.GainedWaypoints.Any(known => known.WaypointId == 10));
            Assert.IsTrue(trigger.TriggeredBy.Contains(client));
            var menu = WorldTestContext.Drain(client).Select(p => p.Message).OfType<CallMethodMessage>()
                .Select(p => p.Packet).OfType<EnteredWaypointPacket>().Single();
            Assert.AreEqual(1U, menu.CurrentMapId);
            client.Player.Position = new Vector3(200, 0, 0);
            CellManager.Instance.UpdateVisibility(client);
            triggers.TriggersProximityWorker(world.Map);
            Assert.IsFalse(trigger.TriggeredBy.Contains(client));
        }

        [TestMethod]
        public void FailedTravelCommitRestoresOriginAndDisconnectsInsteadOfReportingSuccess()
        {
            using var world = new WorldTestContext();
            var client = world.CreateClient();
            CellManager.Instance.AddToWorld(client);
            var manager = CreateManager(world, (_, update, _) =>
            {
                if (update == CharacterUpdate.Position)
                    throw new DbUpdateException("Fixture write failure.");
            });
            AddWaypoint(manager, world.Map, 10, Vector3.Zero);
            AddWaypoint(manager, world.Map, 20, new Vector3(200, 5, 0));
            client.Player.GainedWaypoints.Add(new CharacterTeleporterEntry(client.Player.Id, 20, (byte)WaypointType.Waypoint));
            manager.SelectWaypoint(client, new SelectWaypointPacket { MapInstanceId = 1, WaypointId = 20 });
            WorldTestContext.Drain(client);

            manager.TeleportAcknowledge(client);

            Assert.AreEqual(ClientState.Disconnected, client.State);
            Assert.AreEqual(Vector3.Zero, client.Player.Position);
            Assert.IsNull(client.PendingTransfer);
            Assert.IsFalse(WorldTestContext.Drain(client).Any(packet =>
                packet.Message is CallMethodMessage call && call.Packet is TeleportArrivalPacket));
        }

        internal static DynamicObjectManager CreateManager(WorldTestContext world,
            Action<Client, CharacterUpdate, object> update = null, Func<long> clock = null)
        {
            var maps = new MapChannelManager(null);
            maps.MapChannelArray.Add(world.Map.MapInfo.MapContextId, world.Map);
            return new DynamicObjectManager(null, maps, clock ?? (() => 1000),
                update ?? ((_, _, _) => { }), client => client.State = ClientState.Disconnected);
        }

        internal static DynamicObject AddWaypoint(DynamicObjectManager manager, MapChannel map,
            uint id, Vector3 position, bool contested = false, WaypointType type = WaypointType.Waypoint)
        {
            var waypoint = new DynamicObject
            {
                Position = position,
                MapContextId = map.MapInfo.MapContextId,
                ObjectData = new WaypointInfo(id, contested, type)
            };
            manager.Teleporters.Add(id, waypoint);
            map.Teleporters.Add(id, waypoint);
            return waypoint;
        }
    }
}
