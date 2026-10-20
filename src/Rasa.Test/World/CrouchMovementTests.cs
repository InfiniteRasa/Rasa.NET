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
    using Rasa.Structures;

    // Only the posture message (SetDesiredCrouchState) and an introduction set Player.IsCrouching, so a crouched player who moved without sending STANDING first kept
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

            // Their own client is sent nothing: a StateCorrection([STANDING]) is undone by it a
            // frame later, and nothing else the server has changes its desired posture.
            var own = Drain(mover);
            Assert.IsFalse(own.Select(Method).OfType<StateCorrectionPacket>().Any(), "no correction their client would undo");
            Assert.IsFalse(own.Select(Method).OfType<SetDesiredCrouchStatePacket>().Any(), "and not the message their client asserts on");

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

        /// <summary>
        /// The client keeps a crouch as its desired posture and shows it whenever its control state
        /// is normal; lying down while stunned or dead. A crouch that reaches the server when it has
        /// the player stunned, dead or under a camera script is the client's all the same, shown on
        /// every client once that is over: the server takes it and tells everyone, rather than
        /// leaving the player crouched on their own screen and standing on everyone else's.
        /// </summary>
        [TestMethod]
        [DataRow("stunned")]
        [DataRow("dead")]
        [DataRow("camera script")]
        public void ACrouchTheServerHasThemUnableToTakeIsTheirsAllTheSame(string why)
        {
            using var world = new WorldTestContext();
            var crouching = Player(world, 0, 0);
            var watcher = PlayerDeathTests.Player(world, 5, 0);
            Drain(watcher);

            switch (why)
            {
                case "stunned":
                    crouching.Player.ActiveEffects[900001] = new GameEffect { EffectId = 900001, TypeId = Stuns.StunTypeId, IsStun = true, ExpiresTick = System.Environment.TickCount64 + 60_000 };
                    Assert.IsTrue(Stuns.IsStunned(crouching.Player));
                    break;
                case "dead":
                    crouching.Player.State = CharacterState.Dead;
                    break;
                default:
                    crouching.Player.CameraScriptId = 1;
                    crouching.Player.CameraScriptUntil = System.Environment.TickCount64 + 60_000;
                    Assert.IsTrue(CameraScripts.IsWatching(crouching.Player));
                    break;
            }

            ManifestationManager.Instance.SetDesiredCrouchState(crouching, CharacterState.Crouched);

            Assert.IsTrue(crouching.Player.IsCrouching, "the server agrees with their client");
            Assert.IsFalse(Drain(crouching).Select(Method).OfType<StateCorrectionPacket>().Any(), "no correction their client would undo");
            Assert.AreEqual(CharacterState.Crouched, Drain(watcher).Select(Method).OfType<SetDesiredCrouchStatePacket>().Single().DesiredCrouchState,
                "and everyone else's copy of them crouches with it");
        }

        /// <summary>
        /// A posture from a correction does not stay on a client: it shows the desired posture, and
        /// puts it back a frame later. A creature's goes out as its desired posture too, as NpcPoses
        /// sends one, so it stays.
        /// </summary>
        [TestMethod]
        public void ACreaturesCrouchFromAStateCorrectionGoesOutAsItsDesiredPosture()
        {
            using var world = new WorldTestContext();
            var watcher = PlayerDeathTests.Player(world, 5, 0);
            var creature = Creature(world, new Vector3(0, 0, 3));
            Drain(watcher);

            var sent = ActorManager.CorrectState(world.Map, creature, new[] { CharacterState.Crouched });

            CollectionAssert.AreEqual(new[] { CharacterState.Crouched }, sent);
            Assert.IsTrue(creature.IsCrouching);

            var seen = Drain(watcher).Select(Method).ToList();
            Assert.AreEqual(CharacterState.Crouched, seen.OfType<StateCorrectionPacket>().Single().StateIds.Single());
            Assert.AreEqual(CharacterState.Crouched, seen.OfType<SetDesiredCrouchStatePacket>().Single().DesiredCrouchState, "the posture that stays");

            ActorManager.CorrectState(world.Map, creature, new[] { CharacterState.Standing });
            Assert.IsFalse(creature.IsCrouching);
            Assert.AreEqual(CharacterState.Standing, Drain(watcher).Select(Method).OfType<SetDesiredCrouchStatePacket>().Single().DesiredCrouchState);
        }

        /// <summary>
        /// A player's posture is their own client's: the server cannot change it there, so a
        /// correction leaves crouched and standing out for a player and sends the rest.
        /// </summary>
        [TestMethod]
        public void APlayersCrouchOrStandIsLeftOutOfACorrection()
        {
            using var world = new WorldTestContext();
            var player = Player(world, 0, 0);
            var watcher = PlayerDeathTests.Player(world, 5, 0);
            Drain(watcher);

            Assert.HasCount(0, ActorManager.CorrectState(world.Map, player.Player, new[] { CharacterState.Crouched }));
            Assert.IsFalse(player.Player.IsCrouching);
            Assert.IsFalse(Drain(watcher).Select(Method).Any(packet => packet is StateCorrectionPacket || packet is SetDesiredCrouchStatePacket), "nothing sent");

            ManifestationManager.Instance.SetDesiredCrouchState(player, CharacterState.Crouched);
            Drain(watcher);

            var sent = ActorManager.CorrectState(world.Map, player.Player, new[] { CharacterState.Standing, CharacterState.CombatEngaged });

            CollectionAssert.AreEqual(new[] { CharacterState.CombatEngaged }, sent, "the rest still goes");
            Assert.IsTrue(player.Player.IsCrouching, "still crouched, as on their own screen");
            var seen = Drain(watcher).Select(Method).ToList();
            CollectionAssert.AreEqual(new[] { CharacterState.CombatEngaged }, seen.OfType<StateCorrectionPacket>().Single().StateIds.ToArray());
            Assert.IsFalse(seen.OfType<SetDesiredCrouchStatePacket>().Any());
        }

        #region Fixture

        private static Client Player(WorldTestContext world, float x, float z)
        {
            var client = PlayerDeathTests.Player(world, x, z);
            client.Player.MoveBudget = 60;
            return client;
        }

        private static List<ProtocolPacket> Drain(Client client) => WorldTestContext.Drain(client);

        private static Creature Creature(WorldTestContext world, Vector3 position)
        {
            var creature = new Creature
            {
                Name = "Fixture",
                TargetCategory = TargetCategory.Hostile,
                MapContextId = world.Map.MapInfo.MapContextId,
                RuntimeMapChannel = world.Map,
                Position = position,
                EntityClass = EntityClasses.HumanBaseMale,
                State = CharacterState.Idle,
                Level = 1,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>()
            };
            EntityManager.Instance.RegisterEntity(creature.EntityId, EntityType.Creature);
            EntityManager.Instance.RegisterCreature(creature);
            EntityManager.Instance.RegisterActor(creature.EntityId, creature);
            var seed = CellManager.Instance.GetCellSeed(creature.Position);
            creature.Cells = CellManager.Instance.CreateCellMatrix(world.Map, seed & 0xFFFF, seed >> 16);
            CellManager.Instance.GetCell(world.Map, seed & 0xFFFF, seed >> 16).CreatureList.Add(creature);
            return creature;
        }

        private static PythonPacket Method(ProtocolPacket packet) => (packet.Message as CallMethodMessage)?.Packet;

        #endregion
    }
}
