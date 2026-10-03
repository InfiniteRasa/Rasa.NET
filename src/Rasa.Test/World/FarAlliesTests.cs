using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Models;
    using Rasa.Packets.Game.Server;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Structures;

    // A player's squad on their client from anywhere on the map (FarAllies): given beyond the
    // cells, kept as the two walk apart, told what the cells are told, and taken away when they
    // are squad mates no longer or one of them leaves the map. The client draws a squad member's
    // map marker and radar arrow from the member's entity and from nothing else.
    [TestClass]
    [DoNotParallelize]
    public class FarAlliesTests
    {
        private const uint Squad = 7;

        [TestMethod]
        public void SquadMatesAcrossTheMapAreGivenToEachOtherOnce()
        {
            using var world = new WorldTestContext();
            var one = Player(world, 0, 0, Squad);
            var other = Player(world, 600, 0, Squad);
            var stranger = Player(world, 0, 600, 0);

            FarAllies.Sync(one);
            FarAllies.Sync(other);
            FarAllies.Sync(stranger);

            Assert.AreEqual(other.Player.EntityId, Made(one).Single(), "the squad mate, and not the stranger");
            Assert.AreEqual(one.Player.EntityId, Made(other).Single());
            Assert.AreEqual(0, Made(stranger).Count);

            Assert.IsTrue(FarAllies.Holds(one, other));
            Assert.IsTrue(FarAllies.Holds(other, one));
            Assert.IsFalse(FarAllies.Holds(one, stranger));
            CollectionAssert.AreEqual(new[] { one }, other.Player.FarWatchers);

            FarAllies.Sync(one);
            FarAllies.Sync(other);
            Assert.AreEqual(0, Made(one).Count + Made(other).Count, "once");
        }

        [TestMethod]
        public void SquadMatesInViewAreTheCellsOwn()
        {
            using var world = new WorldTestContext();
            var one = Player(world, 0, 0, Squad);
            var other = Player(world, 10, 0, Squad);

            Assert.AreEqual(other.Player.EntityId, Made(one).Single(), "the cells gave them");

            FarAllies.Sync(one);
            FarAllies.Sync(other);

            Assert.AreEqual(0, Made(one).Count + Made(other).Count, "and that is all");
            Assert.IsFalse(FarAllies.Holds(one, other));
            Assert.AreEqual(0, other.Player.FarWatchers.Count);
        }

        [TestMethod]
        public void WhatTheCellsAreToldOfAPlayerTheirFarHoldersAreToldToo()
        {
            using var world = new WorldTestContext();
            var one = Player(world, 0, 0, Squad);
            var other = Player(world, 600, 0, Squad);
            var stranger = Player(world, 0, 600, 0);

            FarAllies.Sync(one);
            FarAllies.Sync(other);
            Drain(one, other, stranger);

            // A move.
            var movement = new Movement(new Vector3(601, 0, 0), new Vector2(0f, 0f));
            other.CellMoveObject(other, new MoveObjectMessage(other.Player.EntityId, movement), true);

            var move = WorldTestContext.Drain(one).Select(p => p.Message).OfType<MoveObjectMessage>().Single();
            Assert.AreEqual(other.Player.EntityId, move.EntityId);
            Assert.AreSame(movement, move.Movement);
            Assert.AreEqual(0, WorldTestContext.Drain(other).Count, "not their own");
            Assert.AreEqual(0, WorldTestContext.Drain(stranger).Count);

            // Their health, as every hit and heal sends it.
            var health = other.Player.Attributes[Attributes.Health];
            CellManager.Instance.CellCallMethod(world.Map, other.Player, new UpdateHealthPacket(health, 0));

            var told = Calls(one).Single();
            Assert.AreEqual(other.Player.EntityId, told.EntityId);
            Assert.IsInstanceOfType(told.Packet, typeof(UpdateHealthPacket));
            Assert.AreEqual(1, Calls(other).Count, "they are in their own cells");
            Assert.AreEqual(0, Calls(stranger).Count);

            // And what the player's own client has sent about them to those around.
            other.CellIgnoreSelfCallMethod(other, new UpdateHealthPacket(health, 0));
            other.CellCallMethod(other, other.Player.EntityId, new UpdateHealthPacket(health, 0));
            Assert.AreEqual(2, Calls(one).Count);
            Assert.AreEqual(1, Calls(other).Count, "the second is theirs too");
        }

        [TestMethod]
        public void WalkingApartKeepsEachOtherAndWalkingBackMakesNeitherAgain()
        {
            using var world = new WorldTestContext();
            var one = Player(world, 0, 0, Squad);
            var other = Player(world, 10, 0, Squad);
            var stranger = Player(world, 10, 5, 0);

            Drain(one, other, stranger);

            Walk(other, 600, 0);

            Assert.AreEqual(0, Destroyed(one).Count, "the squad mate stays");
            CollectionAssert.AreEqual(new[] { stranger.Player.EntityId }, Destroyed(other), "the stranger goes with the cells");
            CollectionAssert.AreEqual(new[] { other.Player.EntityId }, Destroyed(stranger));
            Assert.IsTrue(FarAllies.Holds(one, other));
            Assert.IsTrue(FarAllies.Holds(other, one));

            // Still there as far as either client knows: moves keep coming.
            other.CellMoveObject(other, new MoveObjectMessage(other.Player.EntityId, new Movement(other.Player.Position, new Vector2(0f, 0f))), true);
            Assert.AreEqual(1, Moves(one));

            Drain(one, other, stranger);
            Walk(other, 10, 0);

            Assert.AreEqual(0, Made(one).Count, "the same entity as before");
            CollectionAssert.AreEqual(new[] { stranger.Player.EntityId }, Made(other), "the stranger is made again");
            Assert.IsFalse(FarAllies.Holds(one, other));
            Assert.IsFalse(FarAllies.Holds(other, one));
            Assert.AreEqual(0, one.Player.FarWatchers.Count + other.Player.FarWatchers.Count);

            // In the cells again: one copy of each move.
            Drain(one, other, stranger);
            other.CellMoveObject(other, new MoveObjectMessage(other.Player.EntityId, new Movement(other.Player.Position, new Vector2(0f, 0f))), true);
            Assert.AreEqual(1, Moves(one));
        }

        [TestMethod]
        public void LeavingTheSquadTakesThemAway()
        {
            using var world = new WorldTestContext();
            var one = Player(world, 0, 0, Squad);
            var other = Player(world, 600, 0, Squad);

            FarAllies.Sync(one);
            FarAllies.Sync(other);
            Drain(one, other);

            other.Player.PartyId = 0;
            FarAllies.Sync(one);
            FarAllies.Sync(other);

            CollectionAssert.AreEqual(new[] { other.Player.EntityId }, Destroyed(one));
            CollectionAssert.AreEqual(new[] { one.Player.EntityId }, Destroyed(other));
            Assert.IsFalse(FarAllies.Holds(one, other));
            Assert.IsFalse(FarAllies.Holds(other, one));

            // Nothing of theirs comes any more.
            CellManager.Instance.CellCallMethod(world.Map, other.Player, new UpdateHealthPacket(other.Player.Attributes[Attributes.Health], 0));
            Assert.AreEqual(0, Calls(one).Count);
        }

        [TestMethod]
        public void LeavingTheMapTakesThemOffEveryoneWhoHeldThem()
        {
            using var world = new WorldTestContext();
            var one = Player(world, 0, 0, Squad);
            var other = Player(world, 600, 0, Squad);

            FarAllies.Sync(one);
            FarAllies.Sync(other);
            Drain(one, other);

            CellManager.Instance.RemoveFromWorld(other);

            CollectionAssert.AreEqual(new[] { other.Player.EntityId }, Destroyed(one));
            Assert.AreEqual(0, one.FarAllies.Count + other.FarAllies.Count);
            Assert.AreEqual(0, one.Player.FarWatchers.Count + other.Player.FarWatchers.Count);

            // Gone from the map: a pass gives nothing back.
            FarAllies.Sync(one);
            Assert.AreEqual(0, Made(one).Count);
        }

        [TestMethod]
        public void TheWorkerLooksAtEachClientOnceASecond()
        {
            using var world = new WorldTestContext();
            var one = Player(world, 0, 0, Squad);
            var other = Player(world, 600, 0, Squad);

            FarAllies.Worker(world.Map, 10_000);
            Assert.IsTrue(FarAllies.Holds(one, other));
            Assert.IsTrue(FarAllies.Holds(other, one));

            other.Player.PartyId = 0;
            FarAllies.Worker(world.Map, 10_000 + FarAllies.SyncIntervalMs - 1);
            Assert.IsTrue(FarAllies.Holds(one, other), "not yet due");

            FarAllies.Worker(world.Map, 10_000 + FarAllies.SyncIntervalMs);
            Assert.IsFalse(FarAllies.Holds(one, other));
            Assert.IsFalse(FarAllies.Holds(other, one));
        }

        #region Fixture

        private static Client Player(WorldTestContext world, float x, float z, uint squad)
        {
            var client = PlayerDeathTests.Player(world, x, z);

            client.Player.PartyId = squad;

            return client;
        }

        /// <summary>The player steps to a place and the cells catch up, as a Move has them do.</summary>
        private static void Walk(Client client, float x, float z)
        {
            client.Player.Position = new Vector3(x, 0, z);
            CellManager.Instance.UpdateVisibility(client);
        }

        private static void Drain(params Client[] clients)
        {
            foreach (var client in clients)
                WorldTestContext.Drain(client);
        }

        private static List<CallMethodMessage> Calls(Client client) =>
            WorldTestContext.Drain(client).Select(p => p.Message).OfType<CallMethodMessage>().ToList();

        private static int Moves(Client client) =>
            WorldTestContext.Drain(client).Select(p => p.Message).OfType<MoveObjectMessage>().Count();

        /// <summary>The entities the client has been told to make since it was last read.</summary>
        private static List<ulong> Made(Client client) =>
            Calls(client).Select(m => m.Packet).OfType<CreatePhysicalEntityPacket>().Select(p => p.EntityId).ToList();

        /// <summary>The entities the client has been told to destroy since it was last read.</summary>
        private static List<ulong> Destroyed(Client client) =>
            Calls(client).Select(m => m.Packet).OfType<DestroyPhysicalEntityPacket>().Select(p => p.EntityId).ToList();

        #endregion
    }
}
