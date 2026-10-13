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
    /// A Hunter's shield (HunterShield): up while it has its weapon drawn for a fight, down when it
    /// puts it away, and a third of every hit it takes from in front of it sent back at whoever
    /// struck; a hit from behind goes round it.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class HunterShieldTests
    {
        private const EntityClasses Hunter = (EntityClasses)10166;

        /// <summary>Rotation that faces +Z, towards a shooter at the origin from a Hunter at z -5.</summary>
        private const double FacingShooter = Math.PI;

        private readonly List<Creature> _creatures = new List<Creature>();

        [TestCleanup]
        public void Cleanup()
        {
            foreach (var creature in _creatures)
            {
                EntityManager.Instance.UnregisterCreature(creature.EntityId);
                EntityManager.Instance.UnregisterActor(creature.EntityId);
                EntityManager.Instance.UnregisterEntity(creature.EntityId);
            }

            _creatures.Clear();
        }

        [TestMethod]
        public void TheShieldIsUpWhileTheHunterHasItsWeaponDrawnAndDownWhenItPutsItAway()
        {
            using var world = new WorldTestContext();
            var hunter = Spawn(world, Hunter, armed: true, FacingShooter);

            CreatureWeaponDraw.Draw(world.Map, hunter);
            Assert.IsTrue(HunterShield.IsUp(hunter));
            var shield = hunter.ActiveEffects.Values.Single(e => e.TypeId == HunterShield.TypeId);
            Assert.AreEqual(long.MaxValue, shield.ExpiresTick, "for the whole fight, no timer");

            CreatureWeaponDraw.Stow(world.Map, hunter);
            Assert.IsFalse(HunterShield.IsUp(hunter));
        }

        [TestMethod]
        public void OnlyAHunterRaisesOne()
        {
            using var world = new WorldTestContext();
            var other = Spawn(world, EntityClasses.HumanBaseMale, armed: true, FacingShooter);

            CreatureWeaponDraw.Draw(world.Map, other);

            Assert.IsFalse(HunterShield.IsUp(other));
        }

        [TestMethod]
        public void AHitFromTheFrontSendsAThirdBack()
        {
            using var world = new WorldTestContext();
            var shooter = Watch(world, 0);
            var hunter = Spawn(world, Hunter, armed: true, FacingShooter);
            CreatureWeaponDraw.Draw(world.Map, hunter);
            Sent(shooter);
            var before = Total(shooter.Player);

            Shoot(world, shooter, hunter, 90);

            Assert.AreEqual(10000 - 90, hunter.Attributes[Attributes.Health].Current, "the Hunter takes the hit");
            Assert.AreEqual(before - 29, Total(shooter.Player), "a third of 90, rounded down");
            var reflect = Sent(shooter).OfType<ShieldDroneReflectPacket>().Single();
            Assert.AreEqual(shooter.Player.EntityId, reflect.DamageTargetId);
            Assert.AreEqual(hunter.ActiveEffects.Values.Single(e => e.TypeId == HunterShield.TypeId).EffectId, reflect.EffectId);
        }

        [TestMethod]
        public void AHitFromBehindGoesRoundTheShield()
        {
            using var world = new WorldTestContext();
            var shooter = Watch(world, 0);
            var hunter = Spawn(world, Hunter, armed: true, rotation: 0);
            CreatureWeaponDraw.Draw(world.Map, hunter);
            Sent(shooter);
            var before = Total(shooter.Player);

            Shoot(world, shooter, hunter, 90);

            Assert.AreEqual(10000 - 90, hunter.Attributes[Attributes.Health].Current);
            Assert.AreEqual(before, Total(shooter.Player));
            Assert.IsFalse(Sent(shooter).OfType<ShieldDroneReflectPacket>().Any());
        }

        [TestMethod]
        public void TheHitThatStartsTheFightMeetsTheShield()
        {
            using var world = new WorldTestContext();
            var shooter = Watch(world, 0);
            var hunter = Spawn(world, Hunter, armed: true, FacingShooter);
            var before = Total(shooter.Player);

            // Struck out of a fight, it turns on its attacker and draws: the shield is up as the
            // hit lands.
            Shoot(world, shooter, hunter, 90);

            Assert.IsTrue(HunterShield.IsUp(hunter));
            Assert.AreEqual(before - 29, Total(shooter.Player));
        }

        [TestMethod]
        public void AHunterWithoutItsShieldReflectsNothing()
        {
            using var world = new WorldTestContext();
            var shooter = Watch(world, 0);
            var hunter = Spawn(world, Hunter, armed: false, FacingShooter);
            var before = Total(shooter.Player);

            Shoot(world, shooter, hunter, 90);

            Assert.IsFalse(HunterShield.IsUp(hunter));
            Assert.AreEqual(before, Total(shooter.Player));
        }

        [TestMethod]
        public void TheFrontIsTheHalfTheHunterFaces()
        {
            var hunter = new Creature { Position = Vector3.Zero, Rotation = FacingShooter };

            Assert.IsTrue(HunterShield.InFront(hunter, new Vector3(0, 0, 5)), "straight ahead");
            Assert.IsTrue(HunterShield.InFront(hunter, new Vector3(5, 0, 0.1f)), "just in front of its side");
            Assert.IsFalse(HunterShield.InFront(hunter, new Vector3(5, 0, -0.1f)), "just behind its side");
            Assert.IsFalse(HunterShield.InFront(hunter, new Vector3(0, 0, -5)), "behind");
        }

        private static int Total(Manifestation player) =>
            player.Attributes[Attributes.Health].Current + player.Attributes[Attributes.Armor].Current;

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

        private Creature Spawn(WorldTestContext world, EntityClasses entityClass, bool armed, double rotation)
        {
            var creature = new Creature
            {
                Name = "Fixture",
                TargetCategory = TargetCategory.Hostile,
                MapContextId = world.Map.MapInfo.MapContextId,
                RuntimeMapChannel = world.Map,
                Position = new Vector3(0, 0, -5),
                Rotation = rotation,
                EntityClass = entityClass,
                State = CharacterState.Idle,
                Level = 1,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>()
            };

            if (armed)
                creature.AppearanceData[EquipmentData.Weapon] = new AppearanceData { SlotId = EquipmentData.Weapon, Class = HunterShield.ShieldWeaponClass };

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
            client.Player.Cells = CellManager.Instance.CreateCellMatrix(world.Map, seed & 0xFFFF, seed >> 16);
            CellManager.Instance.GetCell(world.Map, seed & 0xFFFF, seed >> 16).ClientList.Add(client);
            client.Player.State = CharacterState.Normal;
            foreach (var attribute in new[] { Attributes.Health, Attributes.Armor, Attributes.Power, Attributes.Regen })
                client.Player.Attributes[attribute] = new ActorAttributes(attribute, 1000, 1000, 1000, 0, 0);
            WorldTestContext.Drain(client);
            return client;
        }

        private static List<PythonPacket> Sent(Client client) =>
            WorldTestContext.Drain(client)
                .Select(packet => packet.Message)
                .OfType<CallMethodMessage>()
                .Select(message => message.Packet)
                .ToList();
    }
}
