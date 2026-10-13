using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Structures;

    /// <summary>
    /// A creature's rushing blow is a charge (KaelRushingBlow): the Kael's falls on whoever stands
    /// in its EFFECT_RADIUS where its target stood, and the AFS soldiers' CR_HUMAN_RUSHING_BLOW,
    /// with no radius, on its target alone.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class CreatureChargeTests
    {
        private readonly List<Creature> _creatures = new List<Creature>();
        private readonly Dictionary<ActionId, ActionInfo> _previous = new Dictionary<ActionId, ActionInfo>();

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

            foreach (var (id, previous) in _previous)
            {
                if (previous != null)
                    Actions()[id] = previous;
                else
                    Actions().Remove(id);
            }

            _previous.Clear();
        }

        [TestMethod]
        public void BothRushingBlowsAreChargesAndOnlyTheKaelsHasAnAreaWithoutOneInItsData()
        {
            Assert.IsTrue(KaelRushingBlow.Is(new CreatureAction { ActionId = ActionId.CrKaelRushingBlow }));
            Assert.IsTrue(KaelRushingBlow.Is(new CreatureAction { ActionId = ActionId.CrHumanRushingBlow }));
            Assert.IsFalse(KaelRushingBlow.Is(new CreatureAction { ActionId = ActionId.CrKaelSmash }));
            Assert.IsFalse(KaelRushingBlow.Is(null));

            var kael = new ActionLevelInfo { ActionId = ActionId.CrKaelRushingBlow, Level = 1 };
            kael.Properties[AbilityProperty.EffectRadius] = 15;

            Assert.AreEqual(15f, KaelRushingBlow.RadiusOf(ActionId.CrKaelRushingBlow, kael));
            Assert.AreEqual(KaelRushingBlow.DefaultRadius, KaelRushingBlow.RadiusOf(ActionId.CrKaelRushingBlow, Human()));
            Assert.AreEqual(0f, KaelRushingBlow.RadiusOf(ActionId.CrHumanRushingBlow, Human()), "the AFS soldier's: its target alone");
        }

        [TestMethod]
        public void AnAfsSoldierChargesItsTargetAndStrikesItAloneWhereverItHasGot()
        {
            Seed(ActionId.CrHumanRushingBlow, Human());

            using var world = new WorldTestContext();
            var soldier = Place(world, TargetCategory.Friendly, new Vector3(0, 0, 0));
            var bane = Place(world, TargetCategory.Hostile, new Vector3(0, 0, -15));
            var beside = Place(world, TargetCategory.Hostile, new Vector3(1, 0, -15));
            var row = new CreatureAction { ActionId = ActionId.CrHumanRushingBlow, ActionArgId = 1, RangeMin = 1, RangeMax = 20, Cooldown = 5000, WindupTime = 1598, MinDamage = 34, MaxDamage = 46 };

            KaelRushingBlow.Start(world.Map, soldier, row, bane, 40);

            Assert.IsTrue(KaelRushingBlow.IsCharging(soldier));
            Assert.IsTrue(soldier.IsRushing, "sent running");
            Assert.AreEqual(13f, Vector3.Distance(soldier.Position, soldier.RushTo.Value), 0.05f, "to 2 m short of where the Bane stood");

            // 15 m at 70 m/s is 214 ms, not the row's 1598.
            Assert.AreEqual(214, KaelRushingBlow.WindupMs(15, KaelRushingBlow.DefaultVelocity));

            // The Bane steps aside while the soldier runs; the blow is still its own.
            bane.Position = new Vector3(3, 0, -15);
            Thread.Sleep(300);

            KaelRushingBlow.Worker(world.Map);
            MissileManager.Instance.DoWork(world.Map, 250);

            Assert.IsFalse(KaelRushingBlow.IsCharging(soldier));
            Assert.IsFalse(soldier.IsRushing);
            Assert.AreEqual(-13f, soldier.Position.Z, 0.05f, "where the run ends");
            Assert.IsLessThan(10000, bane.Attributes[Attributes.Health].Current, "the blow on its target");
            Assert.AreEqual(10000, beside.Attributes[Attributes.Health].Current, "and nobody beside where it stood");

            world.Map.QueuedMissiles.Clear();
        }

        /// <summary>CR_HUMAN_RUSHING_BLOW argument 1 as the world database has it.</summary>
        private static ActionLevelInfo Human()
        {
            var info = new ActionLevelInfo { ActionId = ActionId.CrHumanRushingBlow, Level = 1, WindupMs = 1598, MaxRange = 20 };

            info.Properties[AbilityProperty.DamageAmountMin] = 75;
            info.Properties[AbilityProperty.DamageAmountMax] = 100;
            info.Properties[AbilityProperty.Duration] = 1;
            info.Properties[AbilityProperty.DamageScaleType] = 2;
            info.Properties[AbilityProperty.KnockbackDistance] = 3;

            return info;
        }

        private void Seed(ActionId id, ActionLevelInfo info)
        {
            var actions = Actions();

            if (!_previous.ContainsKey(id))
            {
                actions.TryGetValue(id, out var previous);
                _previous[id] = previous;
            }

            var action = new ActionInfo { ActionId = id, Name = "CR_HUMAN_RUSHING_BLOW", Module = AbilityManager.RushingBlowModule };
            action.Levels[info.Level] = info;
            actions[id] = action;
        }

        private static Dictionary<ActionId, ActionInfo> Actions() =>
            (Dictionary<ActionId, ActionInfo>)typeof(AbilityManager)
                .GetField("_actions", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(AbilityManager.Instance)!;

        private Creature Place(WorldTestContext world, TargetCategory category, Vector3 position)
        {
            var creature = new Creature
            {
                Name = "Fixture",
                TargetCategory = category,
                MapContextId = world.Map.MapInfo.MapContextId,
                RuntimeMapChannel = world.Map,
                Position = position,
                EntityClass = EntityClasses.HumanBaseMale,
                State = CharacterState.Idle,
                Level = 1,
                RunSpeed = 9,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>()
            };
            creature.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 10000, 10000, 10000, 0, 0);
            creature.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, 0, 0, 0, 0, 0);
            creature.Controller.CurrentAction = BehaviorManager.BehaviorActionWander;
            EntityManager.Instance.RegisterEntity(creature.EntityId, EntityType.Creature);
            EntityManager.Instance.RegisterCreature(creature);
            EntityManager.Instance.RegisterActor(creature.EntityId, creature);
            var seed = CellManager.Instance.GetCellSeed(position);
            creature.Cells = CellManager.Instance.CreateCellMatrix(world.Map, seed & 0xFFFF, seed >> 16);
            CellManager.Instance.GetCell(world.Map, seed & 0xFFFF, seed >> 16).CreatureList.Add(creature);
            _creatures.Add(creature);
            return creature;
        }
    }
}
