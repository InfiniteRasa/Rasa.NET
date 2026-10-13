using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
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
    /// A knockback and a rush are movements of their own type (MovementType.Knockback, Rush): the
    /// client flies the arc or makes the run itself, and looks at nothing else sent for the entity
    /// until it is done. So the server says where it ends once, and nothing while it is under way.
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

        #region A creature's rush

        [TestMethod]
        public void ARushingCreatureIsSentOnceAndSteppedInSilence()
        {
            using var world = new WorldTestContext();
            var watcher = Watch(world, x: 30);
            var creature = Spawn(world, "Kael", 0);
            Drain(watcher);

            BehaviorManager.Instance.Rush(creature, new Vector3(20, 0, 0), 20f);

            var sent = MovesOf(watcher, creature.EntityId);

            Assert.AreEqual(1, sent.Count);
            Assert.AreEqual(MovementType.Rush, sent[0].Type);
            Assert.AreEqual(new Vector3(20, 0, 0), sent[0].Position);
            Assert.AreEqual(20f, sent[0].Velocity, 0.001f);
            Assert.IsTrue(creature.IsRushing);

            // Half a second at 20 m/s: half way, on the server alone.
            Think(world, 500);

            Assert.AreEqual(0, MovesOf(watcher, creature.EntityId).Count, "The clients are making the run themselves.");
            Assert.AreEqual(10f, creature.Position.X, 0.01f);
            Assert.IsTrue(creature.IsRushing);

            // There: told it stands where the run ends, with an ordinary movement.
            Think(world, 750);

            Assert.IsFalse(creature.IsRushing);
            Assert.IsNull(creature.KnockbackTo);
            Assert.AreEqual(20f, creature.Position.X, 0.01f);

            var after = MovesOf(watcher, creature.EntityId);

            Assert.IsTrue(after.Count >= 1);
            Assert.IsTrue(after.All(move => move.Type == MovementType.Normal));
            Assert.AreEqual(20f, after[0].Position.X, 0.01f);
        }

        [TestMethod]
        public void ARushIsHeldToTheSpeedAMovementCanCarry()
        {
            using var world = new WorldTestContext();
            var watcher = Watch(world, x: 30);
            var creature = Spawn(world, "Fast", 0);
            Drain(watcher);

            BehaviorManager.Instance.Rush(creature, new Vector3(20, 0, 0), 200f);

            Assert.AreEqual(Movement.MaxVelocity, creature.KnockbackSpeed, 0.001f, "The server runs it at what the clients are told.");
            Assert.AreEqual(Movement.MaxVelocity, MovesOf(watcher, creature.EntityId).Single().Velocity, 0.001f);
        }

        [TestMethod]
        public void ARushCutShortEndsWhereItsClientsHaveIt()
        {
            using var world = new WorldTestContext();
            var killer = world.CreateClient(x: 200);
            var watcher = Watch(world, x: 30);
            var creature = Spawn(world, "Kael", 0);

            BehaviorManager.Instance.Rush(creature, new Vector3(20, 0, 0), 20f);
            Think(world, 250);
            Assert.IsTrue(creature.Position.X < 20f);
            Drain(watcher);

            // Killed part-way: its clients run it to the end, so that is where the body is.
            creature.Attributes[Attributes.Health].Current = 0;
            CreatureManager.Instance.HandleCreatureKill(world.Map, creature, killer.Player);

            Assert.AreEqual(CharacterState.Dead, creature.State);
            Assert.AreEqual(new Vector3(20, 0, 0), creature.Position);
            Assert.IsFalse(creature.IsRushing);
            Assert.IsNull(creature.KnockbackTo);
        }

        [TestMethod]
        public void ACarryThatIsNotARushEndsWhereItHasGotTo()
        {
            using var world = new WorldTestContext();
            var creature = Spawn(world, "Pulled", 0);

            creature.KnockbackTo = new Vector3(20, 0, 0);
            creature.KnockbackSpeed = 20f;
            creature.KnockbackIsPull = true;
            Think(world, 250);

            var reached = creature.Position;

            Assert.IsTrue(reached.X > 0f && reached.X < 20f);

            BehaviorManager.Instance.EndCarry(world.Map, creature);

            Assert.AreEqual(reached, creature.Position);
            Assert.IsNull(creature.KnockbackTo);
        }

        [TestMethod]
        public void ACarryElsewhereIsNoLongerTheRush()
        {
            using var world = new WorldTestContext();
            var watcher = Watch(world, x: 30);
            var creature = Spawn(world, "Kael", 0);

            BehaviorManager.Instance.Rush(creature, new Vector3(20, 0, 0), 20f);

            // A Vortex takes hold of it mid-run: an ordinary carry, a step at a time, to the clients.
            creature.KnockbackTo = new Vector3(0, 0, 20);
            Drain(watcher);

            Assert.IsFalse(creature.IsRushing);

            Think(world, 250);

            Assert.IsTrue(MovesOf(watcher, creature.EntityId).Count >= 1);
        }

        #endregion

        #region Rushing Blow

        [TestMethod]
        public void AChargeIsSentOnceToEveryoneAndCarriedInSilence()
        {
            using var world = new WorldTestContext();
            var performer = Watch(world, x: 0);
            var watcher = Watch(world, x: 10);
            var target = Spawn(world, "Target", 30);
            Drain(performer, watcher);

            var windupMs = AbilityManager.ChargeWindupMs(30f);

            Pend(world, performer, windupMs);
            AbilityManager.StartCharge(world.Map, performer, performer.Player, target, ActionId.AaCommandoRushingBlow, windupMs);

            // ChargeStopShort short of the target, there as the windup ends.
            var end = new Vector3(30 - AbilityManager.ChargeStopShort, 0, 0);
            var speed = (30 - AbilityManager.ChargeStopShort) / (windupMs / 1000f);

            foreach (var client in new[] { performer, watcher })
            {
                var moves = MovesOf(client, performer.Player.EntityId);

                Assert.AreEqual(1, moves.Count);
                Assert.AreEqual(MovementType.Rush, moves[0].Type);
                Assert.AreEqual(end, moves[0].Position);
                Assert.AreEqual(Math.Min(speed, Movement.MaxVelocity), moves[0].Velocity, 0.01f);
            }

            Assert.AreEqual(Vector3.Zero, performer.Player.Position, "The server's carry has not begun.");
            Assert.IsTrue(AbilityManager.IsCharging(performer.Player));

            // Carried on the server a tick at a time, and nobody told.
            Thread.Sleep(100);
            AbilityManager.Instance.ChargeWorker(world.Map);

            Assert.IsTrue(performer.Player.Position.X > 0f);
            Assert.AreEqual(0, MovesOf(performer, performer.Player.EntityId).Count);
            Assert.AreEqual(0, MovesOf(watcher, performer.Player.EntityId).Count);

            // The blow lands with the target where it was: they are where they were sent.
            AbilityManager.FinishCharge(performer.Player);

            Assert.AreEqual(end, performer.Player.Position);
            Assert.IsFalse(AbilityManager.IsCharging(performer.Player));
            Assert.AreEqual(0, MovesOf(performer, performer.Player.EntityId).Count);
            Assert.AreEqual(0, MovesOf(watcher, performer.Player.EntityId).Count);
        }

        [TestMethod]
        public void AChargeAtATargetThatMovedIsSentOnToWhereItEnds()
        {
            using var world = new WorldTestContext();
            var performer = Watch(world, x: 0);
            var watcher = Watch(world, x: 10);
            var target = Spawn(world, "Target", 30);
            var windupMs = AbilityManager.ChargeWindupMs(30f);

            Pend(world, performer, windupMs);
            AbilityManager.StartCharge(world.Map, performer, performer.Player, target, ActionId.AaCommandoRushingBlow, windupMs);

            var first = MovesOf(watcher, performer.Player.EntityId).Single();

            Drain(performer);

            target.Position = new Vector3(40, 0, 0);
            AbilityManager.FinishCharge(performer.Player);

            var end = new Vector3(40 - AbilityManager.ChargeStopShort, 0, 0);

            Assert.AreEqual(end, performer.Player.Position);

            foreach (var client in new[] { performer, watcher })
            {
                var moves = MovesOf(client, performer.Player.EntityId);

                Assert.AreEqual(1, moves.Count);
                Assert.AreEqual(MovementType.Rush, moves[0].Type);
                Assert.AreEqual(end, moves[0].Position);
                Assert.AreEqual(first.Velocity, moves[0].Velocity, 0.001f);
            }
        }

        [TestMethod]
        public void AChargeCutShortTellsItsClientsWhereTheServerHasThePerformer()
        {
            using var world = new WorldTestContext();
            var performer = Watch(world, x: 0);
            var watcher = Watch(world, x: 10);
            var target = Spawn(world, "Target", 30);
            var windupMs = AbilityManager.ChargeWindupMs(30f);
            var pending = Pend(world, performer, windupMs);

            AbilityManager.StartCharge(world.Map, performer, performer.Player, target, ActionId.AaCommandoRushingBlow, windupMs);
            Thread.Sleep(100);
            AbilityManager.Instance.ChargeWorker(world.Map);

            var reached = performer.Player.Position;

            Assert.IsTrue(reached.X > 0f && reached.X < 28f);
            Drain(performer, watcher);

            // Interrupted: no blow, and the carry stops where it is.
            pending.IsInrerrupted = true;
            AbilityManager.Instance.ChargeWorker(world.Map);

            Assert.IsFalse(AbilityManager.IsCharging(performer.Player));
            Assert.AreEqual(reached, performer.Player.Position);

            foreach (var client in new[] { performer, watcher })
            {
                var moves = MovesOf(client, performer.Player.EntityId);

                Assert.AreEqual(1, moves.Count);
                Assert.AreEqual(MovementType.Normal, moves[0].Type, "An ordinary placement, applied when their run is over.");
                Assert.AreEqual(reached.X, moves[0].Position.X, 0.01f);
            }
        }

        [TestMethod]
        public void AChargeEndsWithThePerformerLeavingTheMap()
        {
            using var world = new WorldTestContext();
            var performer = Watch(world, x: 0);
            var watcher = Watch(world, x: 10);
            var target = Spawn(world, "Target", 30);
            var windupMs = AbilityManager.ChargeWindupMs(30f);

            Pend(world, performer, windupMs);
            AbilityManager.StartCharge(world.Map, performer, performer.Player, target, ActionId.AaCommandoRushingBlow, windupMs);
            Drain(performer, watcher);

            Assert.IsTrue(AbilityManager.IsCharging(performer.Player));

            // A map change, a logout, a dropped connection: RemovePlayerCharacter says so.
            AbilityManager.PlayerLeaving(performer.Player);

            Assert.IsFalse(AbilityManager.IsCharging(performer.Player), "their Moves on the next map are their own again");
            Assert.AreEqual(0, MovesOf(performer, performer.Player.EntityId).Count, "nothing is sent for a performer who is leaving");
            Assert.AreEqual(0, MovesOf(watcher, performer.Player.EntityId).Count);
        }

        [TestMethod]
        public void TheWorkerDropsAChargeWhosePerformerIsOnAnotherMap()
        {
            using var world = new WorldTestContext();
            var performer = Watch(world, x: 0);
            var target = Spawn(world, "Target", 30);
            var windupMs = AbilityManager.ChargeWindupMs(30f);

            Pend(world, performer, windupMs);
            AbilityManager.StartCharge(world.Map, performer, performer.Player, target, ActionId.AaCommandoRushingBlow, windupMs);
            Drain(performer);

            // Gone to another map by a route that did not say so.
            performer.Player.MapChannel = new MapChannel { MapInfo = world.Map.MapInfo, ClientList = new List<Client>() };

            AbilityManager.Instance.ChargeWorker(world.Map);

            Assert.IsFalse(AbilityManager.IsCharging(performer.Player));
            Assert.AreEqual(0, MovesOf(performer, performer.Player.EntityId).Count, "not told where a charge they are no longer on ended");
        }

        [TestMethod]
        public void ACreatureThatGivesUpAChaseIsToldToStand()
        {
            using var world = new WorldTestContext();
            var watcher = Watch(world, x: 10);
            var creature = Spawn(world, "Chaser", 0);

            // Its last think stepped after its target: what the clients were last sent moves.
            creature.Controller.CurrentAction = BehaviorManager.BehaviorActionFighting;
            creature.Controller.LastMovement = new Movement(creature.Position, 6f, 0x08, new Vector2(creature.LastYaw, 0f));
            Drain(watcher);

            BehaviorManager.Instance.StopFighting(creature);

            var sent = MovesOf(watcher, creature.EntityId);

            Assert.AreEqual(1, sent.Count, "a stop, where it is");
            Assert.AreEqual(MovementType.Normal, sent[0].Type);
            Assert.AreEqual(0f, sent[0].Velocity, 0.001f);
            Assert.AreEqual(creature.Position, sent[0].Position);

            // Already standing as far as they know: nothing more is sent for standing again.
            // (MovesOf drains what the watcher was sent, so this counts only what came since.)
            BehaviorManager.Instance.StopFighting(creature);

            Assert.AreEqual(0, MovesOf(watcher, creature.EntityId).Count);
        }

        [TestMethod]
        public void ACreatureAlreadyStandingIsNotToldAgain()
        {
            using var world = new WorldTestContext();
            var watcher = Watch(world, x: 10);
            var creature = Spawn(world, "Stander", 0);

            creature.Controller.CurrentAction = BehaviorManager.BehaviorActionFighting;
            creature.Controller.LastMovement = new Movement(creature.Position, 0f, 0x08, new Vector2(creature.LastYaw, 0f));
            Drain(watcher);

            BehaviorManager.Instance.StopFighting(creature);

            Assert.AreEqual(0, MovesOf(watcher, creature.EntityId).Count);
        }

        [TestMethod]
        public void AChargeFromArmsLengthSendsNobodyAnywhere()
        {
            using var world = new WorldTestContext();
            var performer = Watch(world, x: 0);
            var watcher = Watch(world, x: 10);
            var target = Spawn(world, "Target", 1.5f);
            var windupMs = AbilityManager.ChargeWindupMs(1.5f);

            Drain(performer, watcher);
            Pend(world, performer, windupMs);
            AbilityManager.StartCharge(world.Map, performer, performer.Player, target, ActionId.AaCommandoRushingBlow, windupMs);
            AbilityManager.FinishCharge(performer.Player);

            Assert.AreEqual(Vector3.Zero, performer.Player.Position);
            Assert.AreEqual(0, MovesOf(performer, performer.Player.EntityId).Count);
            Assert.AreEqual(0, MovesOf(watcher, performer.Player.EntityId).Count);
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

        /// <summary>The performer's Rushing Blow, wound up and waiting to land.</summary>
        private static ActionData Pend(WorldTestContext world, Client performer, long windupMs)
        {
            var pending = new ActionData(performer.Player, ActionId.AaCommandoRushingBlow, 1, windupMs);

            world.Map.PerformRecovery.Add(pending);

            return pending;
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
