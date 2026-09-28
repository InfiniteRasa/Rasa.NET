extern alias RasaGame;

using System.Linq;
using System.Numerics;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using ClientState = RasaGame::Rasa.Data.ClientState;
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Models;
    using Rasa.Packets.Game.Server;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;

    [TestClass]
    [DoNotParallelize]
    public class MovementTests
    {
        [TestMethod]
        public void AddingTheSamePlayerTwiceDoesNotDuplicateMembership()
        {
            using var world = new WorldTestContext();
            var client = world.CreateClient();

            CellManager.Instance.AddToWorld(client);
            CellManager.Instance.AddToWorld(client);

            var center = world.Map.MapCellInfo.Cells[client.Player.Cells[2, 2]];
            Assert.AreEqual(1, center.ClientList.Count(c => c == client));
        }

        [TestMethod]
        public void LeavingVisibilityNotifiesBothPlayersWithoutUnregisteringEither()
        {
            using var world = new WorldTestContext();
            var stationary = world.CreateClient();
            var moving = world.CreateClient();
            CellManager.Instance.AddToWorld(stationary);
            CellManager.Instance.AddToWorld(moving);
            WorldTestContext.Drain(stationary);
            WorldTestContext.Drain(moving);
            moving.Player.Position = new Vector3(200, 0, 0);

            CellManager.Instance.UpdateVisibility(world.Map);

            Assert.AreEqual(1, WorldTestContext.Drain(stationary).Select(p => p.Message)
                .OfType<CallMethodMessage>().Count(p => p.Packet is DestroyPhysicalEntityPacket d && d.EntityId == moving.Player.EntityId));
            Assert.AreEqual(1, WorldTestContext.Drain(moving).Select(p => p.Message)
                .OfType<CallMethodMessage>().Count(p => p.Packet is DestroyPhysicalEntityPacket d && d.EntityId == stationary.Player.EntityId));
            Assert.IsTrue(EntityManager.Instance.Players.ContainsKey(stationary.Player.EntityId));
            Assert.IsTrue(EntityManager.Instance.Players.ContainsKey(moving.Player.EntityId));
        }

        [TestMethod]
        public void MovementFanoutDeduplicatesRecipients()
        {
            using var world = new WorldTestContext();
            var receiver = world.CreateClient();
            var sender = world.CreateClient();
            CellManager.Instance.AddToWorld(receiver);
            CellManager.Instance.AddToWorld(sender);
            WorldTestContext.Drain(receiver);
            world.Map.MapCellInfo.Cells[sender.Player.Cells[0, 0]].ClientList.Add(receiver);
            var movement = new Movement(Vector3.Zero, Vector2.Zero);

            sender.CellMoveObject(sender, new MoveObjectMessage(sender.Player.EntityId, movement), true);

            Assert.AreEqual(1, WorldTestContext.Drain(receiver).Count(p => p.Message is MoveObjectMessage));
        }

        [TestMethod]
        [DataRow(ClientState.Teleporting)]
        [DataRow(ClientState.CharacterSelection)]
        [DataRow(ClientState.Disconnected)]
        public void PlayerMovementTargetsOnlyActiveWorldRecipients(ClientState state)
        {
            using var world = new WorldTestContext();
            var receiver = world.CreateClient();
            var sender = world.CreateClient();
            CellManager.Instance.AddToWorld(receiver);
            CellManager.Instance.AddToWorld(sender);
            WorldTestContext.Drain(receiver);
            receiver.State = state;

            sender.CellMoveObject(sender, new MoveObjectMessage(sender.Player.EntityId,
                new Movement(Vector3.Zero, Vector2.Zero)), true);

            Assert.AreEqual(0, WorldTestContext.Drain(receiver).Count);
        }

        [TestMethod]
        [DataRow(ClientState.Teleporting)]
        [DataRow(ClientState.CharacterSelection)]
        public void CreatureMovementTargetsOnlyActiveWorldRecipients(ClientState state)
        {
            using var world = new WorldTestContext();
            var receiver = world.CreateClient();
            CellManager.Instance.AddToWorld(receiver);
            WorldTestContext.Drain(receiver);
            receiver.State = state;
            var creature = new Rasa.Structures.Creature
            {
                Position = Vector3.Zero,
                MapContextId = world.Map.MapInfo.MapContextId
            };
            var maps = MapChannelManager.Instance;
            maps.MapChannelArray.TryGetValue(world.Map.MapInfo.MapContextId, out var previous);
            maps.MapChannelArray[world.Map.MapInfo.MapContextId] = world.Map;
            try
            {
                CellManager.Instance.CellMoveObject(creature, new Movement(Vector3.Zero, Vector2.Zero));

                Assert.AreEqual(0, WorldTestContext.Drain(receiver).Count);
            }
            finally
            {
                EntityManager.Instance.FreeEntity(creature.EntityId);
                if (previous == null)
                    maps.MapChannelArray.Remove(world.Map.MapInfo.MapContextId);
                else
                    maps.MapChannelArray[world.Map.MapInfo.MapContextId] = previous;
            }
        }

        [TestMethod]
        [DataRow(ClientState.Loading)]
        [DataRow(ClientState.CharacterSelection)]
        [DataRow(ClientState.Teleporting)]
        [DataRow(ClientState.Disconnected)]
        public void MovementIsRejectedOutsideTheActiveWorldState(ClientState state)
        {
            using var world = new WorldTestContext();
            var client = world.CreateClient();
            CellManager.Instance.AddToWorld(client);
            WorldTestContext.Drain(client);
            client.State = state;

            Assert.IsFalse(client.HandleMovement(new Movement(Vector3.One, Vector2.Zero)));
            Assert.AreEqual(Vector3.Zero, client.Player.Position);
        }

        [TestMethod]
        [DataRow(float.NaN)]
        [DataRow(float.PositiveInfinity)]
        [DataRow(float.NegativeInfinity)]
        public void NonfiniteMovementDoesNotMutateThePlayer(float coordinate)
        {
            using var world = new WorldTestContext();
            var client = world.CreateClient();
            CellManager.Instance.AddToWorld(client);

            Assert.IsFalse(client.HandleMovement(new Movement(new Vector3(coordinate, 0, 0), Vector2.Zero)));
            Assert.AreEqual(Vector3.Zero, client.Player.Position);
        }

        [TestMethod]
        public void MovementDistanceValidationRejectsAVisibilitySkippingJump()
        {
            using var world = new WorldTestContext();
            var oldNeighbor = world.CreateClient();
            var moving = world.CreateClient();
            var newNeighbor = world.CreateClient(200);
            foreach (var client in world.Map.ClientList)
                CellManager.Instance.AddToWorld(client);
            foreach (var client in world.Map.ClientList)
                WorldTestContext.Drain(client);

            Assert.IsFalse(moving.HandleMovement(new Movement(new Vector3(200, 0, 0), Vector2.Zero)));

            var oldPackets = WorldTestContext.Drain(oldNeighbor);
            Assert.IsFalse(oldPackets.Any(p => p.Message is MoveObjectMessage));
            var newPackets = WorldTestContext.Drain(newNeighbor);
            Assert.IsFalse(newPackets.Any(p => p.Message is MoveObjectMessage));
            Assert.AreEqual(Vector3.Zero, moving.Player.Position);
        }

        [TestMethod]
        public void CellKeysRejectCoordinatesThatWouldAliasAnotherCell()
        {
            using var world = new WorldTestContext();

            Assert.ThrowsExactly<InvalidDataException>(() =>
                CellManager.Instance.GetCell(world.Map, ushort.MaxValue + 1U, 0));
            Assert.ThrowsExactly<InvalidDataException>(() =>
                CellManager.Instance.GetCellSeed(new Vector3(float.MaxValue, 0, 0)));
            Assert.AreEqual(0, world.Map.MapCellInfo.Cells.Count);
        }
    }
}
