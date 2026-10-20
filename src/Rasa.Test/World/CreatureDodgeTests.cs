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
    using Rasa.Packets.MapChannel.Server.PerformRecovery;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Structures;

    /// <summary>
    /// A creature whose skeleton has the dodge animations sidesteps a shot that misses it
    /// (CreatureDodges): PerformRecovery(DODGE, left or right) on it, and a dodge (misstype 2)
    /// where it was a plain miss, at most once in 5 s.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class CreatureDodgeTests
    {
        private const EntityClasses Thrax = (EntityClasses)990101;      // mesh 13798, skeleton 13867: a Bane soldier
        private const EntityClasses Other = (EntityClasses)990102;      // mesh 12591, skeleton 13868: no dodge

        private readonly List<Creature> _creatures = new();
        private readonly List<EntityClasses> _classes = new();
        private long _now;

        [TestInitialize]
        public void Start()
        {
            _now = 1_000_000;
            CreatureDodges.Now = () => _now;
            CreatureDodges.Side = () => CreatureDodges.LeftArg;
            AddClass(Thrax, 13798);
            AddClass(Other, 12591);
        }

        [TestCleanup]
        public void Cleanup()
        {
            CreatureDodges.Reset();

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
        public void ABaneSoldierSidestepsAMissAndNotAgainForFiveSeconds()
        {
            using var world = new WorldTestContext();
            var shooter = Watch(world, x: 30);
            var soldier = Spawn(world, Thrax);

            Shoot(world, shooter, soldier, missed: true);
            var sent = Sent(shooter);
            var dodge = On(sent, soldier).OfType<PerformRecoveryPacket>().Single();
            Assert.AreEqual((PerformType.TwoArgs, ActionId.Dodge, CreatureDodges.LeftArg), (dodge.PerformType, dodge.ActionId, dodge.ActionArgId),
                "VM - Dodge - Left");
            CollectionAssert.AreEqual(new[] { MissileManager.MissTypeDodge }, Recovery(sent).Missile.Args.Missdata, "Dodged");
            Assert.IsTrue(sent.FindIndex(entry => entry.Packet is PerformRecoveryPacket) < sent.FindIndex(entry => entry.Packet is WeaponAttackRecovery));

            _now += CreatureDodges.ReuseMs - 1;
            Shoot(world, shooter, soldier, missed: true);
            sent = Sent(shooter);
            Assert.IsEmpty(On(sent, soldier).OfType<PerformRecoveryPacket>(), "its 5 s reuse");
            CollectionAssert.AreEqual(new[] { MissileManager.MissTypeMiss }, Recovery(sent).Missile.Args.Missdata);

            _now += 1;
            CreatureDodges.Side = () => CreatureDodges.RightArg;
            Shoot(world, shooter, soldier, missed: true);
            sent = Sent(shooter);
            Assert.AreEqual(CreatureDodges.RightArg, On(sent, soldier).OfType<PerformRecoveryPacket>().Single().ActionArgId, "VM - Dodge - Right");
            CollectionAssert.AreEqual(new[] { MissileManager.MissTypeDodge }, Recovery(sent).Missile.Args.Missdata);
        }

        [TestMethod]
        public void AHitIsNoDodge()
        {
            using var world = new WorldTestContext();
            var shooter = Watch(world, x: 30);
            var soldier = Spawn(world, Thrax);

            Shoot(world, shooter, soldier, missed: false);

            var sent = Sent(shooter);
            Assert.IsEmpty(On(sent, soldier).OfType<PerformRecoveryPacket>());
            Assert.IsEmpty(Recovery(sent).Missile.Args.Missdata);
        }

        [TestMethod]
        public void ACreatureWithoutTheAnimationsIsPlainlyMissed()
        {
            using var world = new WorldTestContext();
            var shooter = Watch(world, x: 30);

            foreach (var creature in new[] { Spawn(world, Other), Spawn(world, EntityClasses.HumanBaseMale) })
            {
                Assert.IsFalse(CreatureDodges.CanDodge(creature));

                Shoot(world, shooter, creature, missed: true);
                var sent = Sent(shooter);
                Assert.IsEmpty(On(sent, creature).OfType<PerformRecoveryPacket>());
                CollectionAssert.AreEqual(new[] { MissileManager.MissTypeMiss }, Recovery(sent).Missile.Args.Missdata);
            }
        }

        [TestMethod]
        public void ASoldierWindingUpOrDeadDoesNotDodge()
        {
            using var world = new WorldTestContext();
            var shooter = Watch(world, x: 30);
            var winding = Spawn(world, Thrax);
            winding.Controller.WindupUntil = Environment.TickCount64 + 60_000;

            Assert.IsFalse(CreatureDodges.TryDodge(world.Map, winding), "a recovery would cancel its windup on the client");

            var dead = Spawn(world, Thrax);
            dead.State = CharacterState.Dead;
            Assert.IsFalse(CreatureDodges.TryDodge(world.Map, dead));

            Assert.IsEmpty(Sent(shooter).Where(entry => entry.Packet is PerformRecoveryPacket));

            winding.Controller.WindupUntil = 0;
            Assert.IsTrue(CreatureDodges.TryDodge(world.Map, winding));
        }

        private void AddClass(EntityClasses id, int mesh)
        {
            var classes = EntityClassManager.Instance.LoadedEntityClasses;

            if (classes.ContainsKey(id))
                return;

            classes.Add(id, new EntityClass((uint)id, "fixture", (uint)mesh, 0, new List<AugmentationType>(), true));
            _classes.Add(id);
        }

        private static void Shoot(WorldTestContext world, Client shooter, Creature target, bool missed)
        {
            MissileManager.Instance.MissileTrigger(world.Map, new Missile
            {
                Source = shooter.Player,
                TargetActor = target,
                TargetEntityId = target.EntityId,
                DamageA = 10,
                ActionId = ActionId.WeaponAttack,
                ActionArgId = 1,
                Missed = missed
            });
        }

        private static WeaponAttackRecovery Recovery(List<(ulong EntityId, PythonPacket Packet)> sent) =>
            sent.Select(entry => entry.Packet).OfType<WeaponAttackRecovery>().Single();

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
            creature.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, 0, 0, 0, 0, 0);
            creature.Controller.CurrentAction = BehaviorManager.BehaviorActionWander;
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
