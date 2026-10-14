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
    using Rasa.Packets;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Structures;

    /// <summary>
    /// A Warden Bot below half its health turtles, once a fight: CR_WARDEN_BOT_TURTLE's windup, then
    /// for its 5 s recovery nothing hurts it (WardenBotTurtle).
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class WardenBotTurtleTests
    {
        private const EntityClasses Warden = (EntityClasses)6961;

        private readonly List<Creature> _creatures = new List<Creature>();

        [TestCleanup]
        public void Cleanup()
        {
            foreach (var creature in _creatures)
            {
                WardenBotTurtle.Forget(creature);
                EntityManager.Instance.UnregisterCreature(creature.EntityId);
                EntityManager.Instance.UnregisterActor(creature.EntityId);
                EntityManager.Instance.UnregisterEntity(creature.EntityId);
            }

            _creatures.Clear();
        }

        [TestMethod]
        [DataRow(10000, false)]
        [DataRow(5000, false)]
        [DataRow(4999, true)]
        [DataRow(1, true)]
        [DataRow(0, false)]
        public void ItTurtlesBelowHalfItsHealth(int health, bool below)
        {
            Assert.AreEqual(below, WardenBotTurtle.Below(health, 10000));
        }

        [TestMethod]
        public void BelowTheLineInAFightItWindsTheTurtleUpAndShutItTakesNothing()
        {
            using var world = new WorldTestContext();
            var player = Watch(world);
            var warden = Spawn(world, Warden);
            Fight(warden, player);
            warden.Attributes[Attributes.Health].Current = 4000;

            WardenBotTurtle.Observe(world.Map, warden);

            var windup = Sent(player, warden).OfType<PerformWindupPacket>().Single();
            Assert.AreEqual((ActionId.CrWardenBotTurtle, 1u), (windup.ActionId, windup.ActionArgId));
            Assert.IsTrue(WardenBotTurtle.HasTurtled(warden));
            Assert.IsTrue(CreatureWindups.HasPending(warden));
            Assert.IsFalse(WardenBotTurtle.IsShut(warden), "not until the windup is done");

            Shoot(world, player, warden, 100);
            Assert.AreEqual(3900, warden.Attributes[Attributes.Health].Current, "a hit in the windup lands");

            WardenBotTurtle.Shut(world.Map, warden);

            Assert.IsTrue(WardenBotTurtle.IsShut(warden));
            Assert.IsTrue(CreatureWindups.IsWindingUp(warden), "it stands and does nothing else");
            var shut = warden.ActiveEffects.Values.Single(e => e.TypeId == WardenBotTurtle.TypeId);
            Assert.IsTrue(shut.ImmuneToAllDamage);
            Assert.IsTrue(shut.ServerOnly, "the client has no class for WARDEN_BOT_TURTLE");
            Assert.IsTrue(shut.ExpiresTick <= Environment.TickCount64 + WardenBotTurtle.ShutMs);
            var recovery = Sent(player, warden).OfType<AbilityRecoveryPacket>().Single();
            Assert.AreEqual(ActionId.CrWardenBotTurtle, recovery.ActionId);

            Shoot(world, player, warden, 100);
            Assert.AreEqual(3900, warden.Attributes[Attributes.Health].Current, "shut, it takes nothing");
            Assert.IsTrue(DamageImmunity.IsImmune(warden, DamageType.Fire));
        }

        [TestMethod]
        public void TheWindupRunsOutIntoTheTurtle()
        {
            using var world = new WorldTestContext();
            var player = Watch(world);
            var warden = Spawn(world, Warden);
            Fight(warden, player);
            warden.Attributes[Attributes.Health].Current = 4000;

            WardenBotTurtle.Observe(world.Map, warden);
            Thread.Sleep(WardenBotTurtle.WindupMs + 50);
            CreatureWindups.Worker(world.Map);

            Assert.IsTrue(WardenBotTurtle.IsShut(warden));
        }

        [TestMethod]
        public void OnceAFightAndAgainOnlyInAFightAfterItIsBackAboveTheLine()
        {
            using var world = new WorldTestContext();
            var player = Watch(world);
            var warden = Spawn(world, Warden);
            Fight(warden, player);
            warden.Attributes[Attributes.Health].Current = 4000;

            WardenBotTurtle.Observe(world.Map, warden);
            Thread.Sleep(WardenBotTurtle.WindupMs + 50);
            CreatureWindups.Worker(world.Map);
            Assert.IsTrue(WardenBotTurtle.IsShut(warden));
            Over(warden);
            Sent(player, warden);

            WardenBotTurtle.Observe(world.Map, warden);
            Assert.IsFalse(Sent(player, warden).OfType<PerformWindupPacket>().Any(), "not twice in a fight");

            // Out of the fight and still low: it keeps it.
            BehaviorManager.Instance.StopFighting(warden);
            WardenBotTurtle.Observe(world.Map, warden);
            Assert.IsTrue(WardenBotTurtle.HasTurtled(warden));

            // Healed above the line out of a fight: ready again.
            warden.Attributes[Attributes.Health].Current = 9000;
            WardenBotTurtle.Observe(world.Map, warden);
            Assert.IsFalse(WardenBotTurtle.HasTurtled(warden));

            Fight(warden, player);
            warden.Attributes[Attributes.Health].Current = 3000;
            Sent(player, warden);
            WardenBotTurtle.Observe(world.Map, warden);
            Assert.IsTrue(Sent(player, warden).OfType<PerformWindupPacket>().Any(), "a new fight, a new turtle");
        }

        [TestMethod]
        public void NotOutOfAFightNorAnyOtherCreature()
        {
            using var world = new WorldTestContext();
            var player = Watch(world);
            var idle = Spawn(world, Warden);
            idle.Attributes[Attributes.Health].Current = 1000;
            var other = Spawn(world, EntityClasses.HumanBaseMale);
            Fight(other, player);
            other.Attributes[Attributes.Health].Current = 1000;

            WardenBotTurtle.Observe(world.Map, idle);
            WardenBotTurtle.Observe(world.Map, other);

            Assert.IsFalse(WardenBotTurtle.HasTurtled(idle));
            Assert.IsFalse(WardenBotTurtle.HasTurtled(other));
        }

        [TestMethod]
        public void TheCreaturesThinkStartsIt()
        {
            using var world = new WorldTestContext();
            var player = Watch(world);
            var warden = Spawn(world, Warden);
            Fight(warden, player);
            warden.Attributes[Attributes.Health].Current = 2000;

            BehaviorManager.Instance.MapChannelThink(world.Map, 250);

            Assert.IsTrue(WardenBotTurtle.HasTurtled(warden));
        }

        private static void Fight(Creature creature, Client player) =>
            Assert.IsTrue(BehaviorManager.Instance.TrySetActionFighting(creature, player.Player.EntityId));

        /// <summary>The shut is over: its effect runs out and the creature can act again.</summary>
        private static void Over(Creature creature)
        {
            foreach (var effect in creature.ActiveEffects.Values.Where(e => e.TypeId == WardenBotTurtle.TypeId))
                effect.ExpiresTick = Environment.TickCount64 - 1;
            creature.Controller.WindupUntil = 0;
        }

        private static void Shoot(WorldTestContext world, Client shooter, Creature target, int damage)
        {
            MissileManager.Instance.MissileTrigger(world.Map, new Missile
            {
                Source = shooter.Player,
                TargetActor = target,
                TargetEntityId = target.EntityId,
                DamageA = damage,
                DamageType = DamageType.Physical,
                ActionId = ActionId.WeaponAttack,
                ActionArgId = 1,
                CritChance = 0
            });
        }

        private static List<PythonPacket> Sent(Client client, Creature about) =>
            WorldTestContext.Drain(client)
                .Select(packet => packet.Message)
                .OfType<CallMethodMessage>()
                .Where(message => message.EntityId == about.EntityId)
                .Select(message => message.Packet)
                .ToList();

        private Creature Spawn(WorldTestContext world, EntityClasses entityClass)
        {
            var creature = new Creature
            {
                Name = "Fixture",
                TargetCategory = TargetCategory.Hostile,
                MapContextId = world.Map.MapInfo.MapContextId,
                RuntimeMapChannel = world.Map,
                Position = new Vector3(0, 0, 5 + _creatures.Count * 3),
                EntityClass = entityClass,
                State = CharacterState.Idle,
                Level = 1,
                AggroRange = 0,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>()
            };
            creature.HomePos.Position = creature.Position;
            creature.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 10000, 10000, 10000, 0, 0);
            creature.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, 0, 0, 0, 0, 0);
            BehaviorManager.StartWandering(creature, false);
            EntityManager.Instance.RegisterEntity(creature.EntityId, EntityType.Creature);
            EntityManager.Instance.RegisterCreature(creature);
            EntityManager.Instance.RegisterActor(creature.EntityId, creature);
            var seed = CellManager.Instance.GetCellSeed(creature.Position);
            creature.Cells = CellManager.Instance.CreateCellMatrix(world.Map, seed & 0xFFFF, seed >> 16);
            CellManager.Instance.GetCell(world.Map, seed & 0xFFFF, seed >> 16).CreatureList.Add(creature);
            _creatures.Add(creature);
            return creature;
        }

        private static Client Watch(WorldTestContext world)
        {
            var client = world.CreateClient();
            var seed = CellManager.Instance.GetCellSeed(client.Player.Position);
            client.Player.Cells = CellManager.Instance.CreateCellMatrix(world.Map, seed & 0xFFFF, seed >> 16);
            CellManager.Instance.GetCell(world.Map, seed & 0xFFFF, seed >> 16).ClientList.Add(client);
            client.Player.State = CharacterState.Normal;
            foreach (var attribute in new[] { Attributes.Health, Attributes.Armor, Attributes.Power, Attributes.Regen })
                client.Player.Attributes[attribute] = new ActorAttributes(attribute, 1000, 1000, 1000, 0, 0);
            WorldTestContext.Drain(client);
            return client;
        }
    }
}
