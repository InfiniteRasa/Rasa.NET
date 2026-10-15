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
    using Rasa.Packets.ClientMethod.Server;
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

            // Their client plays its own arrival when the Teleport's delay is up, and resets its
            // movement controller there; one sent back for the answer could beat it to it.
            var answered = WorldTestContext.Drain(client).Select(p => p.Message).OfType<CallMethodMessage>().Select(p => p.Packet).ToList();
            Assert.IsFalse(answered.OfType<TeleportArrivalPacket>().Any());
            Assert.AreEqual(1, answered.OfType<UnrequestMovementBlockPacket>().Count());

            manager.TeleportAcknowledge(client);
            Assert.AreEqual(1, saved);
        }

        // The client acknowledges a Teleport only when a BeginTeleport has queued the answer
        // (actor.py BeginTeleport -> _TeleportAckQueue, Recv_Teleport -> _TeleportAck), so the
        // BeginTeleport has to come first or the transfer is never completed.
        [TestMethod]
        public void BeginTeleportComesBeforeTheTeleportSoTheClientAcknowledgesIt()
        {
            using var world = new WorldTestContext();
            var client = world.CreateClient();
            CellManager.Instance.AddToWorld(client);
            WorldTestContext.Drain(client);
            var manager = CreateManager(world);
            AddWaypoint(manager, world.Map, 10, Vector3.Zero);
            AddWaypoint(manager, world.Map, 20, new Vector3(200, 5, 0));
            client.Player.GainedWaypoints.Add(new CharacterTeleporterEntry(client.Player.Id, 20, (byte)WaypointType.Waypoint));

            manager.SelectWaypoint(client, new SelectWaypointPacket { MapInstanceId = world.Map.MapInfo.MapContextId, WaypointId = 20 });

            var messages = WorldTestContext.Drain(client).Select(p => p.Message).ToList();
            var sent = messages.OfType<CallMethodMessage>().Select(p => p.Packet).ToList();
            var begin = sent.FindIndex(packet => packet is BeginTeleportPacket);
            var teleport = sent.FindIndex(packet => packet is TeleportPacket);
            Assert.IsTrue(begin >= 0, "BeginTeleport sent");
            Assert.IsLessThan(teleport, begin, "BeginTeleport first");

            // And the body before either: the client finishes the Teleport by where the body is
            // when the delay is up (DynamicObjectManager.TeleportDelayMs).
            var moved = messages.FindIndex(message => message is MoveObjectMessage move && move.EntityId == client.Player.EntityId);
            var beginAt = messages.FindIndex(message => message is CallMethodMessage call && call.Packet is BeginTeleportPacket);
            Assert.IsTrue(moved >= 0 && moved < beginAt, "the MoveObject before the BeginTeleport");
            Assert.AreEqual(DynamicObjectManager.TeleportDelayMs, ((TeleportPacket)sent[teleport]).Delay);
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

        /// <summary>
        /// The travel window keys each row by the map id it was listed under - the map's context
        /// id (MapInstanceInfo) - and sends that back as the instance: what a real client sends
        /// when a waypoint is picked. It selects the instance that row was listed for.
        /// </summary>
        [TestMethod]
        public void TheListedMapIdSelectsTheWaypointsOwnInstance()
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

            Assert.AreEqual(ClientState.Teleporting, client.State);
            Assert.IsNotNull(client.PendingTransfer);
            Assert.AreEqual(new Vector3(200, 1, 0), client.Player.Position);
        }

        /// <summary>A map id that is neither the waypoint's map nor its instance number is still refused.</summary>
        [TestMethod]
        public void AnotherMapsIdIsNotAnInstanceOfThisOne()
        {
            using var world = new WorldTestContext();
            var client = world.CreateClient();
            CellManager.Instance.AddToWorld(client);
            WorldTestContext.Drain(client);
            var manager = CreateManager(world);
            AddWaypoint(manager, world.Map, 10, Vector3.Zero);
            AddWaypoint(manager, world.Map, 20, new Vector3(200, 0, 0));
            client.Player.GainedWaypoints.Add(new CharacterTeleporterEntry(client.Player.Id, 20, (byte)WaypointType.Waypoint));

            manager.SelectWaypoint(client, new SelectWaypointPacket { MapInstanceId = 1148, WaypointId = 20 });

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
            Assert.AreEqual(1220U, menu.CurrentMapId);
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

        // BR-204: the origin's window used to be closed by its own proximity worker once the
        // player was seen to be gone, and when the destination came first in the map's list that
        // was after the destination had opened its window: ExitedWaypoint, which names no
        // waypoint, closed the window the player had just arrived to.
        [TestMethod]
        public void ArrivingAtAWaypointListedBeforeTheOriginLeavesItsWindowOpen()
        {
            using var world = new WorldTestContext();
            var client = world.CreateClient();
            CellManager.Instance.AddToWorld(client);
            var manager = CreateManager(world);
            var destination = AddWaypoint(manager, world.Map, 20, new Vector3(200, 5, 0));
            var origin = AddWaypoint(manager, world.Map, 10, Vector3.Zero);
            foreach (var waypoint in new[] { destination, origin })
            {
                waypoint.RuntimeMapChannel = world.Map;
                waypoint.DynamicObjectType = DynamicObjectType.Waypoint;
            }
            client.Player.GainedWaypoints.Add(new CharacterTeleporterEntry(client.Player.Id, 20, (byte)WaypointType.Waypoint));

            void Proximity()
            {
                foreach (var waypoint in world.Map.Teleporters.Values)
                    manager.DynamicObjectProximityWorker(world.Map, waypoint, 1000);
            }

            Proximity();
            Assert.IsTrue(origin.TriggeredByPlayers.Contains(client), "the window at the origin");
            WorldTestContext.Drain(client);

            manager.SelectWaypoint(client, new SelectWaypointPacket { MapInstanceId = world.Map.MapInfo.MapContextId, WaypointId = 20 });

            // Closed as they go, once.
            Assert.IsFalse(origin.TriggeredByPlayers.Contains(client));
            Assert.AreEqual(1, Sent(client).OfType<ExitedWaypointPacket>().Count());

            manager.TeleportAcknowledge(client);
            WorldTestContext.Drain(client);

            // The destination's worker before the origin's, as the map lists them.
            Proximity();

            var arrived = Sent(client);
            Assert.AreEqual(20u, arrived.OfType<EnteredWaypointPacket>().Single().CurrentWaypointId);
            Assert.IsFalse(arrived.OfType<ExitedWaypointPacket>().Any(), "the window just opened stays open");
            Assert.IsTrue(destination.TriggeredByPlayers.Contains(client));
        }

        private static System.Collections.Generic.List<Rasa.Packets.PythonPacket> Sent(Client client) =>
            WorldTestContext.Drain(client).Select(p => p.Message).OfType<CallMethodMessage>().Select(p => p.Packet).ToList();

        /// <summary>A manager on the context's map. Without holdMs it moves a traveller at once, as the tests of where travel takes them want.</summary>
        internal static DynamicObjectManager CreateManager(WorldTestContext world,
            Action<Client, CharacterUpdate, object> update = null, Func<long> clock = null, int holdMs = 0)
        {
            var maps = new MapChannelManager(null);
            maps.MapChannelArray.Add(world.Map.MapInfo.MapContextId, world.Map);
            return new DynamicObjectManager(null, maps, clock ?? (() => 1000),
                update ?? ((_, _, _) => { }), client => client.State = ClientState.Disconnected,
                localTravelHoldMs: holdMs);
        }

        // In footage the teleport-out effect plays out, the body fading away, before the player
        // appears at the far waypoint. Teleported at once, the client cut it short.
        [TestMethod]
        public void AWaypointHoldsThePlayerInTheTeleportOutEffectBeforeMovingThem()
        {
            using var world = new WorldTestContext();
            var client = world.CreateClient();
            CellManager.Instance.AddToWorld(client);
            WorldTestContext.Drain(client);
            long now = 1000;
            var manager = CreateManager(world, clock: () => now, holdMs: DynamicObjectManager.WaypointHoldMs);
            AddWaypoint(manager, world.Map, 10, Vector3.Zero);
            AddWaypoint(manager, world.Map, 20, new Vector3(200, 5, 0));
            client.Player.GainedWaypoints.Add(new CharacterTeleporterEntry(client.Player.Id, 20, (byte)WaypointType.Waypoint));

            manager.SelectWaypoint(client, new SelectWaypointPacket { MapInstanceId = 1, WaypointId = 20 });

            Assert.AreEqual(ClientState.Ingame, client.State);
            Assert.IsNotNull(client.PendingTransfer);
            Assert.AreEqual(Vector3.Zero, client.Player.Position);
            var held = Sent(client);
            Assert.IsTrue(held.Any(packet => packet is RequestMovementBlockPacket), "held where they stand");
            Assert.IsTrue(held.Any(packet => packet is PreTeleportPacket), "the teleport-out effect");
            Assert.IsFalse(held.Any(packet => packet is TeleportPacket || packet is BeginTeleportPacket), "moved before the effect has played");

            // Picking again while it plays changes nothing.
            var transfer = client.PendingTransfer;
            manager.SelectWaypoint(client, new SelectWaypointPacket { MapInstanceId = 1, WaypointId = 20 });
            Assert.AreSame(transfer, client.PendingTransfer);

            now += DynamicObjectManager.WaypointHoldMs - 1;
            manager.PassagesWorker(world.Map);
            Assert.AreEqual(Vector3.Zero, client.Player.Position, "moved before the effect has played");

            now++;
            manager.PassagesWorker(world.Map);
            Assert.AreEqual(new Vector3(200, 6, 0), client.Player.Position);
            Assert.AreEqual(ClientState.Teleporting, client.State);
            var sent = Sent(client);
            var begin = sent.FindIndex(packet => packet is BeginTeleportPacket);
            var teleport = sent.FindIndex(packet => packet is TeleportPacket);
            Assert.IsTrue(begin >= 0 && teleport > begin, "BeginTeleport, Teleport: the order the client answers");

            manager.TeleportAcknowledge(client);
            Assert.AreEqual(ClientState.Ingame, client.State);
            Assert.IsNull(client.PendingTransfer);
        }

        [TestMethod]
        public void ThoseAtTheFarWaypointSeeAHeldTravellerArrive()
        {
            using var world = new WorldTestContext();
            var client = world.CreateClient();
            var watcher = world.CreateClient(200, 0);
            CellManager.Instance.AddToWorld(client);
            CellManager.Instance.AddToWorld(watcher);
            long now = 1000;
            var manager = CreateManager(world, clock: () => now, holdMs: DynamicObjectManager.WaypointHoldMs);
            AddWaypoint(manager, world.Map, 10, Vector3.Zero);
            AddWaypoint(manager, world.Map, 20, new Vector3(200, 5, 0));
            client.Player.GainedWaypoints.Add(new CharacterTeleporterEntry(client.Player.Id, 20, (byte)WaypointType.Waypoint));

            manager.SelectWaypoint(client, new SelectWaypointPacket { MapInstanceId = 1, WaypointId = 20 });
            now += DynamicObjectManager.WaypointHoldMs;
            manager.PassagesWorker(world.Map);
            WorldTestContext.Drain(watcher);

            manager.TeleportAcknowledge(client);

            Assert.IsTrue(Sent(watcher).Any(packet => packet is TeleportArrivalPacket), "seen arriving at the far waypoint");
            Assert.IsFalse(Sent(client).Any(packet => packet is TeleportArrivalPacket), "their own client plays its own");
        }

        [TestMethod]
        public void ATravellerWhoDiesWhileHeldStaysWhereTheyStood()
        {
            using var world = new WorldTestContext();
            var client = world.CreateClient();
            CellManager.Instance.AddToWorld(client);
            long now = 1000;
            var manager = CreateManager(world, clock: () => now, holdMs: DynamicObjectManager.WaypointHoldMs);
            AddWaypoint(manager, world.Map, 10, Vector3.Zero);
            AddWaypoint(manager, world.Map, 20, new Vector3(200, 5, 0));
            client.Player.GainedWaypoints.Add(new CharacterTeleporterEntry(client.Player.Id, 20, (byte)WaypointType.Waypoint));

            manager.SelectWaypoint(client, new SelectWaypointPacket { MapInstanceId = 1, WaypointId = 20 });
            WorldTestContext.Drain(client);
            client.Player.State = CharacterState.Dead;
            now += DynamicObjectManager.WaypointHoldMs;
            manager.PassagesWorker(world.Map);

            Assert.IsNull(client.PendingTransfer);
            Assert.AreEqual(Vector3.Zero, client.Player.Position);
            var sent = Sent(client);
            Assert.IsTrue(sent.Any(packet => packet is TeleportFailedPacket), "the effect stopped");
            Assert.IsTrue(sent.Any(packet => packet is UnrequestMovementBlockPacket), "let go");
        }

        // A waypoint on the map they are on: those around see the fade, then the flash where the
        // traveller stood, before the body moves to the far waypoint.
        [TestMethod]
        public void ThoseAroundSeeTheDepartureEffectWhereTheTravellerStood()
        {
            using var world = new WorldTestContext();
            var client = world.CreateClient();
            var watcher = world.CreateClient(5, 0);
            CellManager.Instance.AddToWorld(client);
            CellManager.Instance.AddToWorld(watcher);
            WorldTestContext.Drain(watcher);
            var manager = CreateManager(world, (_, _, _) => { });
            AddWaypoint(manager, world.Map, 10, Vector3.Zero);
            AddWaypoint(manager, world.Map, 20, new Vector3(30, 5, 0));
            client.Player.GainedWaypoints.Add(new CharacterTeleporterEntry(client.Player.Id, 20, (byte)WaypointType.Waypoint));

            manager.SelectWaypoint(client, new SelectWaypointPacket { MapInstanceId = 1, WaypointId = 20 });

            var seen = DropshipTravelTests.Departure(watcher, client.Player.EntityId);
            CollectionAssert.AreEqual(new[] { "pre", "post", "move" }, seen.Where(kind => kind != "destroy").Take(3).ToArray());
            Assert.IsFalse(DropshipTravelTests.Departure(client, client.Player.EntityId).Contains("post"), "their own client plays it from the Teleport");
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
