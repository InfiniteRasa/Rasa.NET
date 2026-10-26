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
    using Rasa.Packets;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Structures;

    /// <summary>
    /// A creature that turns where it stands plays the turn (CreatureTurns): PerformWindup of the
    /// locked turn its way round as it starts, and the recovery, carrying the new yaw, when the
    /// turn has had its time.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class CreatureTurnTests
    {
        private const EntityClasses Thrax = (EntityClasses)990201;      // mesh 13798, skeleton 13867: has the turns
        private const EntityClasses Other = (EntityClasses)990202;      // mesh 14024, skeleton 13878: has none

        private readonly List<Creature> _creatures = new();
        private readonly List<EntityClasses> _classes = new();
        private Func<long> _clock;
        private long _now;

        [TestInitialize]
        public void Start()
        {
            _clock = CreatureTurns.Now;
            _now = 1_000_000;
            CreatureTurns.Now = () => _now;
            AddClass(Thrax, 13798);
            AddClass(Other, 14024);
        }

        [TestCleanup]
        public void Cleanup()
        {
            CreatureTurns.Now = _clock;

            foreach (var creature in _creatures)
            {
                EntityManager.Instance.UnregisterCreature(creature.EntityId);
                EntityManager.Instance.UnregisterActor(creature.EntityId);
                EntityManager.Instance.UnregisterEntity(creature.EntityId);
            }

            foreach (var id in _classes)
                EntityClassManager.Instance.LoadedEntityClasses.Remove(id);

            _creatures.Clear();
            _classes.Clear();
        }

        [TestMethod]
        public void ATurnPlaysAsItStartsAndIsEndedWithItsYawWhenItHasHadItsTime()
        {
            using var world = new WorldTestContext();
            var watcher = Watch(world, x: 30);
            var soldier = Spawn(world, Thrax);

            Assert.IsTrue(CreatureTurns.Begin(soldier, 1.5f, 2.0f, 400));
            var windup = On(Sent(watcher), soldier).OfType<PerformWindupPacket>().Single();
            Assert.AreEqual((PerformType.TwoArgs, CreatureTurns.TurnRight, CreatureTurns.TurnArg), (windup.PerformType, windup.ActionId, windup.ActionArgId),
                "more yaw: State - Turn Right");

            _now += 399;
            CreatureTurns.Finish(soldier);
            Assert.IsEmpty(On(Sent(watcher), soldier).ToList(), "still turning");
            Assert.IsTrue(CreatureTurns.IsTurning(soldier));

            _now += 1;
            CreatureTurns.Finish(soldier);
            var recovery = On(Sent(watcher), soldier).OfType<PerformRecoveryPacket>().Single();
            Assert.AreEqual((PerformType.ThreeArgs, CreatureTurns.TurnRight, CreatureTurns.TurnArg), (recovery.PerformType, recovery.ActionId, recovery.ActionArgId));
            Assert.AreEqual(2.0, recovery.Yaw.Value, 1e-6, "LockedTurn.DoAction takes the new yaw");
            Assert.IsFalse(CreatureTurns.IsTurning(soldier));

            CreatureTurns.Finish(soldier);
            Assert.IsEmpty(On(Sent(watcher), soldier).ToList(), "once");
        }

        [TestMethod]
        public void LessYawIsTheOtherWayAndASmallTurnOrAnotherSkeletonPlaysNothing()
        {
            using var world = new WorldTestContext();
            var watcher = Watch(world, x: 30);
            var soldier = Spawn(world, Thrax);
            var other = Spawn(world, Other);

            Assert.IsTrue(CreatureTurns.Begin(soldier, -1.5f, 0.5f, 400));
            Assert.AreEqual(CreatureTurns.TurnLeft, On(Sent(watcher), soldier).OfType<PerformWindupPacket>().Single().ActionId, "State - Turn Left");

            Assert.IsFalse(CreatureTurns.Begin(Spawn(world, Thrax), Patrols.TurnInPlace / 2, 0.1f, 50), "a heading change made on the move");
            Assert.IsFalse(CreatureTurns.Begin(other, 1.5f, 2.0f, 400), "no turn animations on its skeleton");
            Assert.IsEmpty(Sent(watcher).Where(entry => entry.EntityId != soldier.EntityId).ToList());
        }

        [TestMethod]
        public void AWalkerTurningAboutOnItsBeatPlaysTheTurnAndEndsItAsItWalksOn()
        {
            using var world = new WorldTestContext();
            var watcher = Watch(world, x: 30);
            var sentry = Spawn(world, Thrax);
            const float east = -MathF.PI / 2f, west = MathF.PI / 2f;
            sentry.Rotation = sentry.LastYaw = east;
            sentry.AggroRange = 0;
            sentry.WalkSpeed = 2;
            sentry.RunSpeed = 4;
            sentry.HomePos.Position = sentry.Position;
            sentry.HomePos.MapContextid = sentry.MapContextId;
            BehaviorManager.StartWandering(sentry, false);
            sentry.Patrol = new[] { new PatrolStep(new Vector3(10, 0, 0)), new PatrolStep(new Vector3(12, 0, 0)) };

            var seen = new List<(int Tick, PythonPacket Packet)>();
            for (var tick = 0; tick < 16; tick++)
            {
                _now += 250;
                BehaviorManager.Instance.MapChannelThink(world.Map, 250);
                seen.AddRange(On(Sent(watcher), sentry).Select(packet => (tick, packet)));
            }

            var windups = seen.Where(entry => entry.Packet is PerformWindupPacket).ToList();
            var recoveries = seen.Where(entry => entry.Packet is PerformRecoveryPacket).ToList();
            // About at the far step, and about again back at the first.
            Assert.HasCount(2, windups);
            Assert.HasCount(2, recoveries);
            Assert.IsLessThan(recoveries[0].Tick, windups[0].Tick);
            Assert.IsLessThan(windups[1].Tick, recoveries[0].Tick, "each ended before the next");

            var recovery = (PerformRecoveryPacket)recoveries[0].Packet;
            Assert.AreEqual(((PerformWindupPacket)windups[0].Packet).ActionId, recovery.ActionId);
            Assert.AreEqual(west, recovery.Yaw.Value, 1e-4);
            Assert.AreEqual(east, ((PerformRecoveryPacket)recoveries[1].Packet).Yaw.Value, 1e-4);
            Assert.AreEqual(windups[0].Tick + 2, recoveries[0].Tick, "half a turn is half a second: two thinks");
        }

        private void AddClass(EntityClasses id, int mesh)
        {
            var classes = EntityClassManager.Instance.LoadedEntityClasses;

            if (classes.ContainsKey(id))
                return;

            classes.Add(id, new EntityClass((uint)id, "fixture", (uint)mesh, 0, new List<AugmentationType>(), true));
            _classes.Add(id);
        }

        private static IEnumerable<PythonPacket> On(List<(ulong EntityId, PythonPacket Packet)> sent, Creature about) =>
            sent.Where(entry => entry.EntityId == about.EntityId).Select(entry => entry.Packet);

        private static List<(ulong EntityId, PythonPacket Packet)> Sent(Client client) =>
            WorldTestContext.Drain(client).Select(packet => packet.Message).OfType<CallMethodMessage>()
                .Select(message => (message.EntityId, message.Packet)).ToList();

        private Creature Spawn(WorldTestContext world, EntityClasses entityClass)
        {
            var creature = new Creature
            {
                Name = "Fixture",
                TargetCategory = TargetCategory.Hostile,
                MapContextId = world.Map.MapInfo.MapContextId,
                RuntimeMapChannel = world.Map,
                Position = new Vector3(10, 0, 0),
                EntityClass = entityClass,
                State = CharacterState.Idle,
                Level = 1,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>()
            };

            creature.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 10000, 10000, 10000, 0, 0);
            EntityManager.Instance.RegisterEntity(creature.EntityId, EntityType.Creature);
            EntityManager.Instance.RegisterCreature(creature);
            EntityManager.Instance.RegisterActor(creature.EntityId, creature);
            var seed = CellManager.Instance.GetCellSeed(creature.Position);
            creature.Cells = CellManager.Instance.CreateCellMatrix(world.Map, seed & 0xFFFF, seed >> 16);
            CellManager.Instance.GetCell(world.Map, seed & 0xFFFF, seed >> 16).CreatureList.Add(creature);
            _creatures.Add(creature);
            return creature;
        }

        private static Client Watch(WorldTestContext world, float x)
        {
            var client = world.CreateClient(x: x);
            var seed = CellManager.Instance.GetCellSeed(client.Player.Position);
            client.Player.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 1000, 1000, 1000, 0, 0);
            client.Player.Cells = CellManager.Instance.CreateCellMatrix(world.Map, seed & 0xFFFF, seed >> 16);
            CellManager.Instance.GetCell(world.Map, seed & 0xFFFF, seed >> 16).ClientList.Add(client);
            WorldTestContext.Drain(client);
            return client;
        }
    }
}
