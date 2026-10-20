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
    using Rasa.Packets;
    using Rasa.Packets.MapChannel.Client;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;

    // Only the posture message (SetDesiredCrouchState), a state correction and an introduction
    // set Player.IsCrouching, so a crouched player who moved without sending STANDING first kept
    // every crouched bonus wherever they went. The client stands its own actor up before it moves
    // (playermovementmgr._Move), so a Move that takes a crouched player away from where they
    // crouched now stands them up on the server, their own client and everyone else's.
    [TestClass]
    [DoNotParallelize]
    public class CrouchMovementTests
    {
        [TestMethod]
        public void AMoveAwayFromWhereTheyCrouchedStandsThemUp()
        {
            using var world = new WorldTestContext();
            var mover = Player(world, 0, 0);
            var watcher = PlayerDeathTests.Player(world, 5, 0);

            ManifestationManager.Instance.SetDesiredCrouchState(mover, CharacterState.Crouched);
            Assert.IsTrue(mover.Player.IsCrouching);
            Drain(mover);
            Drain(watcher);

            Assert.IsTrue(mover.HandleMovement(new Movement(new Vector3(2, 0, 0), Vector2.Zero)));

            Assert.IsFalse(mover.Player.IsCrouching);

            var own = Drain(mover);
            Assert.AreEqual(CharacterState.Standing,
                own.Select(Method).OfType<StateCorrectionPacket>().Single().StateIds.Single(), "their own client is put back standing");

            var seen = Drain(watcher);
            var stood = seen.FindIndex(packet => Method(packet) is SetDesiredCrouchStatePacket crouch && crouch.DesiredCrouchState == CharacterState.Standing);
            var moved = seen.FindIndex(packet => packet.Message is MoveObjectMessage);

            Assert.IsTrue(stood >= 0, "everyone else sees them stand");
            Assert.IsTrue(moved > stood, "before they see them move");
        }

        [TestMethod]
        public void ATurnOnTheSpotOrTheBodySettlingIsNotAMove()
        {
            using var world = new WorldTestContext();
            var mover = Player(world, 0, 0);
            var watcher = PlayerDeathTests.Player(world, 5, 0);

            ManifestationManager.Instance.SetDesiredCrouchState(mover, CharacterState.Crouched);
            Drain(mover);
            Drain(watcher);

            Assert.IsTrue(mover.HandleMovement(new Movement(Vector3.Zero, new Vector2(1.5f, 0))));
            Assert.IsTrue(mover.HandleMovement(new Movement(new Vector3(0.3f, 0, 0.3f), new Vector2(1.5f, 0))));

            Assert.IsTrue(mover.Player.IsCrouching);
            Assert.IsFalse(Drain(mover).Select(Method).OfType<StateCorrectionPacket>().Any());
            Assert.IsFalse(Drain(watcher).Select(Method).OfType<SetDesiredCrouchStatePacket>().Any());
        }

        [TestMethod]
        public void AClientThatStoodUpFirstIsToldNothingMore()
        {
            using var world = new WorldTestContext();
            var mover = Player(world, 0, 0);
            var watcher = PlayerDeathTests.Player(world, 5, 0);

            ManifestationManager.Instance.SetDesiredCrouchState(mover, CharacterState.Crouched);
            Drain(mover);
            Drain(watcher);

            // As the client does it: STANDING, then the Move.
            ManifestationManager.Instance.SetDesiredCrouchState(mover, CharacterState.Standing);
            Assert.IsTrue(mover.HandleMovement(new Movement(new Vector3(2, 0, 0), Vector2.Zero)));

            Assert.IsFalse(mover.Player.IsCrouching);
            Assert.IsFalse(Drain(mover).Select(Method).OfType<StateCorrectionPacket>().Any());
            Assert.AreEqual(1, Drain(watcher).Select(Method).OfType<SetDesiredCrouchStatePacket>().Count());
        }

        [TestMethod]
        public void ACrouchCountsFromWhereItWasTaken()
        {
            using var world = new WorldTestContext();
            var mover = Player(world, 0, 0);

            ManifestationManager.Instance.SetDesiredCrouchState(mover, CharacterState.Crouched);
            ManifestationManager.Instance.SetDesiredCrouchState(mover, CharacterState.Standing);
            Assert.IsTrue(mover.HandleMovement(new Movement(new Vector3(3, 0, 0), Vector2.Zero)));

            // Crouched again here: a little settling from here is still crouched.
            ManifestationManager.Instance.SetDesiredCrouchState(mover, CharacterState.Crouched);
            Assert.IsTrue(mover.HandleMovement(new Movement(new Vector3(3.2f, 0, 0), Vector2.Zero)));

            Assert.IsTrue(mover.Player.IsCrouching);
        }

        [TestMethod]
        public void ACrouchFromAStateCorrectionStandsThemUpTheSameWay()
        {
            using var world = new WorldTestContext();
            var mover = Player(world, 0, 0);

            ActorManager.CorrectState(world.Map, mover.Player, new[] { CharacterState.Crouched });
            Assert.IsTrue(mover.Player.IsCrouching);

            Assert.IsTrue(mover.HandleMovement(new Movement(new Vector3(0.2f, 0, 0), Vector2.Zero)));
            Assert.IsTrue(mover.Player.IsCrouching);

            Assert.IsTrue(mover.HandleMovement(new Movement(new Vector3(1.5f, 0, 0), Vector2.Zero)));
            Assert.IsFalse(mover.Player.IsCrouching);
        }

        #region Fixture

        private static Client Player(WorldTestContext world, float x, float z)
        {
            var client = PlayerDeathTests.Player(world, x, z);
            client.Player.MoveBudget = 60;
            return client;
        }

        private static List<ProtocolPacket> Drain(Client client) => WorldTestContext.Drain(client);

        private static PythonPacket Method(ProtocolPacket packet) => (packet.Message as CallMethodMessage)?.Packet;

        #endregion
    }
}
