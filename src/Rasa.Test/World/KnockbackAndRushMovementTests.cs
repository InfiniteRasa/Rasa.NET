using System;
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
    using Rasa.Packets.Protocol;
    using Rasa.Structures;

    /// <summary>
    /// A knockback is a movement of its own type (MovementType.Knockback): the client flies the
    /// arc itself, and looks at nothing else sent for the entity until it is back on its feet. So
    /// the server says where it ends once, and nothing while it is under way.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class KnockbackAndRushMovementTests
    {
        #region Knockback

        [TestMethod]
        public void AKnockedBackPlayerIsThrownWithOneKnockbackMovement()
        {
            using var world = new WorldTestContext();
            var victim = Watch(world, x: 10);
            var watcher = Watch(world, x: 30);
            var attacker = Spawn(world, "Kicker", 0);
            Drain(victim, watcher);

            Assert.IsTrue(PlayerCrowdControl.Knockback(world.Map, victim.Player, attacker, 10f));

            // Straight away from the attacker, and the server has them there from the start.
            Assert.AreEqual(new Vector3(20, 0, 0), victim.Player.Position);

            foreach (var client in new[] { victim, watcher })
            {
                var moves = MovesOf(client, victim.Player.EntityId);

                Assert.AreEqual(1, moves.Count);
                Assert.AreEqual(MovementType.Knockback, moves[0].Type);
                Assert.AreEqual(new Vector3(20, 0, 0), moves[0].Position);
            }
        }

        [TestMethod]
        public void AKnockedBackCreatureIsThrownWithOneKnockbackMovementAndNothingAfter()
        {
            using var world = new WorldTestContext();
            var attacker = world.CreateClient(x: 0);
            var watcher = Watch(world, x: 30);
            var creature = Spawn(world, "Thrown", 10);
            Drain(watcher);

            Assert.IsTrue(CrowdControl.Knockback(world.Map, creature, attacker.Player, 10f, CrowdControl.KnockbackTypeId, DamageType.Physical));

            Assert.AreEqual(new Vector3(20, 0, 0), creature.Position, "It is where the knockback ends from the start.");
            Assert.IsNull(creature.KnockbackTo, "Nothing is left to carry it.");
            Assert.IsTrue(Stuns.IsStunned(creature));

            var moves = MovesOf(watcher, creature.EntityId);

            Assert.AreEqual(1, moves.Count);
            Assert.AreEqual(MovementType.Knockback, moves[0].Type);
            Assert.AreEqual(new Vector3(20, 0, 0), moves[0].Position);

            // Faces back the way it came: towards the attacker.
            Assert.AreEqual((float)Math.Atan2(1, 0), moves[0].ViewDirection.X, 0.001f);

            // Down for the flight and the getup: the clients are sent nothing more for it.
            Think(world, 1000);

            Assert.AreEqual(0, MovesOf(watcher, creature.EntityId).Count);
            Assert.AreEqual(new Vector3(20, 0, 0), creature.Position);
        }

        [TestMethod]
        public void AKnockbackLastsAsLongAsTheClientsOwn()
        {
            // KnockbackState: the distance at 15 m/s, then 0.85 s and 1.15 s on the ground.
            Assert.AreEqual(15f, CrowdControl.KnockbackSpeed);
            Assert.AreEqual(2000, CrowdControl.GetupMs);
            Assert.AreEqual(1000 + 2000, PlayerCrowdControl.KnockdownMs(15f));
        }

        [TestMethod]
        public void ACreatureImmuneToTheKnockbackIsNotThrown()
        {
            using var world = new WorldTestContext();
            var attacker = world.CreateClient(x: 0);
            var watcher = Watch(world, x: 30);
            var creature = Spawn(world, "RunningHome", 10);

            // Running home after a leash: no debuff lands on it.
            creature.Controller.CurrentAction = BehaviorManager.BehaviorActionReturning;
            Drain(watcher);

            CrowdControl.Knockback(world.Map, creature, attacker.Player, 10f, CrowdControl.KnockbackTypeId, DamageType.Physical);

            Assert.IsFalse(Stuns.IsStunned(creature));
            Assert.AreEqual(new Vector3(10, 0, 0), creature.Position);
            Assert.AreEqual(0, MovesOf(watcher, creature.EntityId).Count);
        }

        [TestMethod]
        public void SomethingRunFromOutsideItsOwnBehaviourIsNotThrown()
        {
            using var world = new WorldTestContext();
            var attacker = world.CreateClient(x: 0);
            var watcher = Watch(world, x: 30);
            var trap = Spawn(world, "Trap", 10);

            trap.IsScripted = true;
            Drain(watcher);

            CrowdControl.Knockback(world.Map, trap, attacker.Player, 10f, CrowdControl.KnockbackTypeId, DamageType.Physical);

            Assert.AreEqual(new Vector3(10, 0, 0), trap.Position);
            Assert.AreEqual(0, MovesOf(watcher, trap.EntityId).Count);
        }

        [TestMethod]
        public void ACreatureWhoseKnockbackOpensItsCriticalDeathWindowStaysWhereItStands()
        {
            using var world = new WorldTestContext();
            var attacker = world.CreateClient(x: 0);
            var watcher = Watch(world, x: 30);
            var creature = Spawn(world, "NearDeath", 10, health: 5);
            Drain(watcher);

            CrowdControl.Knockback(world.Map, creature, attacker.Player, 10f, CrowdControl.KnockbackTypeId, DamageType.Physical);

            Assert.AreEqual(CharacterState.Dying, creature.State, "The window opens.");
            Assert.AreEqual(new Vector3(10, 0, 0), creature.Position);
            Assert.IsFalse(MovesOf(watcher, creature.EntityId).Any(move => move.Type == MovementType.Knockback));
        }

        [TestMethod]
        public void ACarryStopsAtTheLastStepBeforeAForceField()
        {
            using var world = new WorldTestContext();
            var creature = Spawn(world, "Carried", 10);
            var from = new Vector3(10, 0, -5);
            var to = new Vector3(10, 0, 5);

            // No field on the map: the whole way.
            Assert.AreEqual(to, CrowdControl.BeforeFields(world.Map, creature, from, to));
            Assert.AreEqual(from, CrowdControl.BeforeFields(world.Map, creature, from, from));

            // An AFS gate across the line stops the hostile on the near side of it.
            var field = ForceFields.Place(world.Map, ForceFields.ClassOf("humgate"), ForceFields.Side.A, new Vector3(10, 0, 0), 0f, 100);

            try
            {
                var stopped = CrowdControl.BeforeFields(world.Map, creature, from, to);

                Assert.IsTrue(stopped.Z < 0f && stopped.Z >= -5f, $"Stopped at {stopped}.");
                Assert.IsFalse(ForceFields.Stops(world.Map, creature, from, stopped));

                // A Bane one is no bar to it.
                ForceFields.SetSide(field, ForceFields.Side.B);
                Assert.AreEqual(to, CrowdControl.BeforeFields(world.Map, creature, from, to));
            }
            finally
            {
                ForceFields.Remove(field);
            }
        }

        [TestMethod]
        public void AKnockbackDoesNotThrowACreatureThroughAForceField()
        {
            using var world = new WorldTestContext();
            var attacker = world.CreateClient(x: 10);
            var watcher = Watch(world, x: 30);
            var creature = Spawn(world, "Thrown", 10);
            var field = ForceFields.Place(world.Map, ForceFields.ClassOf("humgate"), ForceFields.Side.A, new Vector3(10, 0, 0), 0f, 100);

            try
            {
                attacker.Player.Position = new Vector3(10, 0, -10);
                creature.Position = new Vector3(10, 0, -4);
                Drain(watcher);

                CrowdControl.Knockback(world.Map, creature, attacker.Player, 10f, CrowdControl.KnockbackTypeId, DamageType.Physical);

                Assert.IsTrue(creature.Position.Z > -4f && creature.Position.Z < 0f, $"Thrown to {creature.Position}.");

                var move = MovesOf(watcher, creature.EntityId).Single();

                Assert.AreEqual(MovementType.Knockback, move.Type);
                Assert.AreEqual(creature.Position.Z, move.Position.Z, 0.01f);
            }
            finally
            {
                ForceFields.Remove(field);
            }
        }

        #endregion

        #region Fixture

        /// <summary>The movements sent to this client for the entity since it was last drained, in order.</summary>
        private static List<Movement> MovesOf(Client client, ulong entityId)
        {
            return WorldTestContext.Drain(client)
                .Select(packet => packet.Message)
                .OfType<MoveObjectMessage>()
                .Where(message => message.EntityId == entityId)
                .Select(message => message.Movement)
                .ToList();
        }

        private static void Drain(params Client[] clients)
        {
            foreach (var client in clients)
                WorldTestContext.Drain(client);
        }

        private static void Think(WorldTestContext world, long milliseconds)
        {
            for (var elapsed = 0L; elapsed < milliseconds; elapsed += 250)
                BehaviorManager.Instance.MapChannelThink(world.Map, 250);
        }

        /// <summary>A hostile creature in the map's cells.</summary>
        private static Creature Spawn(WorldTestContext world, string name, float x, int health = 100)
        {
            var creature = new Creature
            {
                Name = name,
                TargetCategory = TargetCategory.Hostile,
                MapContextId = world.Map.MapInfo.MapContextId,
                RuntimeMapChannel = world.Map,
                Position = new Vector3(x, 0, 0),
                EntityClass = EntityClasses.HumanBaseMale,
                State = CharacterState.Idle,
                Level = 1,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>()
            };
            creature.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 100, 100, health, 0, 0);
            creature.Controller.CurrentAction = BehaviorManager.BehaviorActionWander;
            EntityManager.Instance.RegisterEntity(creature.EntityId, EntityType.Creature);
            EntityManager.Instance.RegisterCreature(creature);
            EntityManager.Instance.RegisterActor(creature.EntityId, creature);
            var seed = CellManager.Instance.GetCellSeed(creature.Position);
            creature.Cells = CellsAt(world, creature.Position);
            CellManager.Instance.GetCell(world.Map, seed & 0xFFFF, seed >> 16).CreatureList.Add(creature);
            return creature;
        }

        /// <summary>A client with health, standing in the map's cells, so what is sent about what is near it reaches it.</summary>
        private static Client Watch(WorldTestContext world, float x)
        {
            var client = world.CreateClient(x: x);
            var seed = CellManager.Instance.GetCellSeed(client.Player.Position);
            client.Player.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 1000, 1000, 1000, 0, 0);
            client.Player.Cells = CellsAt(world, client.Player.Position);
            CellManager.Instance.GetCell(world.Map, seed & 0xFFFF, seed >> 16).ClientList.Add(client);
            return client;
        }

        private static uint[,] CellsAt(WorldTestContext world, Vector3 position)
        {
            var seed = CellManager.Instance.GetCellSeed(position);
            return CellManager.Instance.CreateCellMatrix(world.Map, seed & 0xFFFF, seed >> 16);
        }

        #endregion
    }
}
