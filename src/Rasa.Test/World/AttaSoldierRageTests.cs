using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Structures;

    /// <summary>
    /// An Atta Soldier below a third of its health rages (AttaSoldierRage): ATTA_SOLDIER_RAGE on
    /// it, and its attacks do double; healed back above the line it calms down.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class AttaSoldierRageTests
    {
        private const EntityClasses Soldier = (EntityClasses)7081;
        private const EntityClasses SoldierBoss = (EntityClasses)22627;

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
        [DataRow(10000, false)]
        [DataRow(3300, false)]
        [DataRow(3299, true)]
        [DataRow(1, true)]
        [DataRow(0, false)]
        public void ASoldierRagesBelowAThirdOfItsHealth(int health, bool rages)
        {
            Assert.AreEqual(rages, AttaSoldierRage.Rages(health, 10000));
        }

        [TestMethod]
        public void BelowTheLineItsAttacksDoDoubleAndHealedAboveItTheyDoNot()
        {
            using var world = new WorldTestContext();
            var soldier = Spawn(world, Soldier);

            Assert.AreEqual(50, GameEffectManager.ApplyDamageDealt(soldier, 50));

            soldier.Attributes[Attributes.Health].Current = 3000;
            AttaSoldierRage.Observe(world.Map, soldier);

            var rage = soldier.ActiveEffects.Values.Single(e => e.TypeId == AttaSoldierRage.TypeId);
            Assert.AreEqual(1u, rage.EffectLevel, "EFFECT_CREATURE_ATTA_SOLDIER_RAGE_V01");
            Assert.AreEqual(100, GameEffectManager.ApplyDamageDealt(soldier, 50));

            AttaSoldierRage.Observe(world.Map, soldier);
            Assert.AreEqual(1, soldier.ActiveEffects.Values.Count(e => e.TypeId == AttaSoldierRage.TypeId), "once");

            soldier.Attributes[Attributes.Health].Current = 5000;
            AttaSoldierRage.Observe(world.Map, soldier);

            Assert.IsFalse(soldier.ActiveEffects.Values.Any(e => e.TypeId == AttaSoldierRage.TypeId));
            Assert.AreEqual(50, GameEffectManager.ApplyDamageDealt(soldier, 50));
        }

        [TestMethod]
        public void ABossSoldierRagesTooAndNoOtherCreatureDoes()
        {
            using var world = new WorldTestContext();
            var boss = Spawn(world, SoldierBoss);
            var other = Spawn(world, EntityClasses.HumanBaseMale);
            boss.Attributes[Attributes.Health].Current = 1000;
            other.Attributes[Attributes.Health].Current = 1000;

            AttaSoldierRage.Observe(world.Map, boss);
            AttaSoldierRage.Observe(world.Map, other);

            Assert.IsTrue(boss.ActiveEffects.Values.Any(e => e.TypeId == AttaSoldierRage.TypeId));
            Assert.IsFalse(other.ActiveEffects.Values.Any(e => e.TypeId == AttaSoldierRage.TypeId));
        }

        [TestMethod]
        public void TheCreaturesThinkPutsItOn()
        {
            using var world = new WorldTestContext();
            var soldier = Spawn(world, Soldier);
            soldier.HomePos.Position = soldier.Position;
            soldier.Attributes[Attributes.Health].Current = 2000;

            BehaviorManager.Instance.MapChannelThink(world.Map, 250);

            Assert.IsTrue(soldier.ActiveEffects.Values.Any(e => e.TypeId == AttaSoldierRage.TypeId));
        }

        private Creature Spawn(WorldTestContext world, EntityClasses entityClass)
        {
            var creature = new Creature
            {
                Name = "Fixture",
                TargetCategory = TargetCategory.Hostile,
                MapContextId = world.Map.MapInfo.MapContextId,
                RuntimeMapChannel = world.Map,
                Position = new Vector3(_creatures.Count * 3, 0, 0),
                EntityClass = entityClass,
                State = CharacterState.Idle,
                Level = 1,
                RunSpeed = 4,
                WalkSpeed = 2,
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
    }
}
