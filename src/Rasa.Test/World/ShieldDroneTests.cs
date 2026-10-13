using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Structures;

    /// <summary>
    /// Every Shield Drone is one (ShieldDrone): the zone drones and Cavalon carry the strike
    /// alone, and still raise the shield, cover and heal the Bane under it and hold their ground;
    /// and a drone that does carry the heal casts it from its shield, never as a shot.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class ShieldDroneTests
    {
        private const EntityClasses DroneClass = (EntityClasses)7233;
        private const EntityClasses BossClass = (EntityClasses)24084;

        private readonly List<Creature> _creatures = new List<Creature>();
        private ActionInfo _previousHeal;
        private bool _seeded;

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

            if (_seeded)
            {
                if (_previousHeal != null)
                    Actions()[ShieldDrone.HealAction] = _previousHeal;
                else
                    Actions().Remove(ShieldDrone.HealAction);
            }
        }

        [TestMethod]
        public void ADroneIsADroneByItsClassOrItsHeal()
        {
            Assert.IsTrue(ShieldDrone.Is(Drone(DroneClass, withHeal: false)), "a zone drone: the strike alone");
            Assert.IsTrue(ShieldDrone.Is(Drone(BossClass, withHeal: false)), "Cavalon");
            Assert.IsTrue(ShieldDrone.Is(Drone(EntityClasses.HumanBaseMale, withHeal: true)), "anything seeded with the heal");
            Assert.IsFalse(ShieldDrone.Is(Drone(EntityClasses.HumanBaseMale, withHeal: false)));
            Assert.IsFalse(ShieldDrone.Is(null));

            Assert.IsTrue(ShieldDrone.HoldsGround(Drone(DroneClass, withHeal: false)));
        }

        [TestMethod]
        public void AZoneDroneRaisesItsShieldCoversTheBaneUnderItAndHealsThemOnTheHealsSchedule()
        {
            SeedHeal();

            using var world = new WorldTestContext();
            var drone = Place(world, Drone(DroneClass, withHeal: false), new Vector3(0, 0, 0));
            var bane = Place(world, Creature(TargetCategory.Hostile), new Vector3(10, 0, 0));
            var outside = Place(world, Creature(TargetCategory.Hostile), new Vector3(70, 0, 0));
            var other = Place(world, Creature(TargetCategory.Friendly), new Vector3(5, 0, 0));

            bane.Attributes[Attributes.Health].Current = 1000;
            drone.Attributes[Attributes.Health].Current = 1000;

            ShieldDrone.Worker(world.Map);

            Assert.IsTrue(drone.ActiveEffects.Values.Any(e => e.TypeId == ShieldDrone.SourceTypeId), "its shield");
            Assert.IsTrue(bane.ActiveEffects.Values.Any(e => e.TypeId == ShieldDrone.ShieldTypeId), "over the Bane 10 m off");
            Assert.IsFalse(outside.ActiveEffects.Values.Any(e => e.TypeId == ShieldDrone.ShieldTypeId), "70 m is outside the 60");
            Assert.IsFalse(other.ActiveEffects.Values.Any(e => e.TypeId == ShieldDrone.ShieldTypeId), "not the other side");

            Assert.AreEqual(1500, bane.Attributes[Attributes.Health].Current, "HEAL_AMOUNT 500");
            Assert.AreEqual(1500, drone.Attributes[Attributes.Health].Current, "and the drone itself");

            // The next pass is well inside the five seconds.
            ShieldDrone.Worker(world.Map);
            Assert.AreEqual(1500, bane.Attributes[Attributes.Health].Current);
        }

        [TestMethod]
        public void ADronesHealIsNotTakenForAnAttack()
        {
            using var world = new WorldTestContext();
            var client = world.CreateClient(x: 20);
            var player = client.Player;
            var seed = CellManager.Instance.GetCellSeed(player.Position);
            player.Cells = CellManager.Instance.CreateCellMatrix(world.Map, seed & 0xFFFF, seed >> 16);
            CellManager.Instance.GetCell(world.Map, seed & 0xFFFF, seed >> 16).ClientList.Add(client);
            player.State = CharacterState.Normal;
            foreach (var attribute in new[] { Attributes.Health, Attributes.Armor, Attributes.Power, Attributes.Regen })
                player.Attributes[attribute] = new ActorAttributes(attribute, 1000, 1000, 1000, 0, 0);

            var drone = Place(world, Drone(DroneClass, withHeal: true), new Vector3(0, 0, 0));
            var heal = drone.Actions.Single(a => a.ActionId == ShieldDrone.HealAction);
            drone.HomePos.Position = drone.Position;
            BehaviorManager.StartWandering(drone, false);
            drone.Hate.Ensure(player.EntityId, 100);
            BehaviorManager.Instance.SetActionFighting(drone, player.EntityId);
            drone.Controller.ActionFighting.Opened = true;

            for (var tick = 0; tick < 8; tick++)
                BehaviorManager.Instance.MapChannelThink(world.Map, 250);

            Assert.AreEqual(BehaviorManager.BehaviorActionFighting, drone.Controller.CurrentAction, "it is in the fight");
            Assert.IsFalse(world.Map.QueuedMissiles.Any(m => m.ActionId == ShieldDrone.HealAction), "no heal shot at the player 20 m off");
            Assert.IsTrue(heal.CooldownTimer <= 0, "and its cooldown is the shield's to start");
            Assert.AreEqual(Vector3.Zero, drone.Position, "holding its ground");

            world.Map.QueuedMissiles.Clear();
        }

        private void SeedHeal()
        {
            var actions = Actions();
            actions.TryGetValue(ShieldDrone.HealAction, out _previousHeal);
            _seeded = true;

            var info = new ActionLevelInfo { ActionId = ShieldDrone.HealAction, Level = 1 };
            info.Properties[AbilityProperty.RadiusAroundSource] = 60;
            info.Properties[AbilityProperty.HealAmountMin] = 500;
            info.Properties[AbilityProperty.HealAmountMax] = 500;

            var action = new ActionInfo { ActionId = ShieldDrone.HealAction, Name = "CR_SHIELD_DRONE_HEAL", Module = "abilities.newactorability" };
            action.Levels[1] = info;
            actions[ShieldDrone.HealAction] = action;
        }

        private static Dictionary<ActionId, ActionInfo> Actions() =>
            (Dictionary<ActionId, ActionInfo>)typeof(AbilityManager)
                .GetField("_actions", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(AbilityManager.Instance)!;

        /// <summary>A drone of the class, with the strike, and the heal if asked: as the creature rows give them.</summary>
        private static Creature Drone(EntityClasses entityClass, bool withHeal)
        {
            var drone = Creature(TargetCategory.Hostile);
            drone.EntityClass = entityClass;
            drone.Actions.Add(new CreatureAction { ActionId = ShieldDrone.AttackAction, ActionArgId = 1, RangeMin = 0.5, RangeMax = 3, Cooldown = 1000, MinDamage = 15, MaxDamage = 20 });

            if (withHeal)
                drone.Actions.Add(new CreatureAction { ActionId = ShieldDrone.HealAction, ActionArgId = 1, RangeMin = 0, RangeMax = 60, Cooldown = 5000 });

            return drone;
        }

        private static Creature Creature(TargetCategory category)
        {
            var creature = new Creature
            {
                Name = "Fixture",
                TargetCategory = category,
                EntityClass = EntityClasses.HumanBaseMale,
                State = CharacterState.Idle,
                Level = 1,
                RunSpeed = 4,
                WalkSpeed = 2,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>()
            };
            creature.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 10000, 10000, 10000, 0, 0);
            creature.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, 0, 0, 0, 0, 0);
            return creature;
        }

        private Creature Place(WorldTestContext world, Creature creature, Vector3 position)
        {
            creature.MapContextId = world.Map.MapInfo.MapContextId;
            creature.RuntimeMapChannel = world.Map;
            creature.Position = position;
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
