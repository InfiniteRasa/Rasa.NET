using System.Collections.Generic;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Structures;

    /// <summary>
    /// A chase stopped three metres from its target, aiming for a point two and a half short of
    /// it, whatever the creature could reach. The Filcher's only attack (CR_FILCHER_MELEE) reaches
    /// two metres, as do the Amoeboid's slime and the Atta Grub's bite: the creature came into the
    /// fight, stood a metre short and never struck, until the stall gave the fight up. Now a chase
    /// comes inside the creature's longest reach when that is under three metres.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class ShortReachChaseTests
    {
        private const long Tick = 250;

        [TestMethod]
        public void AFilcherClosesInsideItsTwoMetresAndStrikes()
        {
            using var world = new WorldTestContext();
            var enemy = Watch(world, x: 2.8f);
            var filcher = Spawn(world, x: 0, rangeMin: 1.0, rangeMax: 2.0);
            var bite = filcher.Actions[0];

            Assert.IsTrue(BehaviorManager.Instance.TrySetActionFighting(filcher, enemy.Player.EntityId));
            filcher.Controller.ActionFighting.Opened = true;   // no waiting on the opening roll

            var struck = false;

            for (var think = 0; think < 40 && !struck; think++)
            {
                BehaviorManager.Instance.MapChannelThink(world.Map, Tick);
                struck = bite.CooldownTimer > 0;
            }

            Assert.IsTrue(struck, $"it stood {Vector3.Distance(filcher.Position, enemy.Player.Position):0.##} m off and never struck");
            Assert.IsTrue(Vector3.Distance(filcher.Position, enemy.Player.Position) < 2.0f);
            Assert.IsTrue(Vector3.Distance(filcher.Position, enemy.Player.Position) >= 1.0f, "and not inside its minimum");
            Assert.AreEqual(BehaviorManager.BehaviorActionFighting, filcher.Controller.CurrentAction);
        }

        [TestMethod]
        public void OnlyAShortReachChangesWhereAChaseStops()
        {
            Creature With(params CreatureAction[] actions)
            {
                var creature = new Creature();
                creature.Actions.AddRange(actions);
                return creature;
            }

            CreatureAction Attack(double max) => new CreatureAction { ActionId = ActionId.CrFilcherMelee, ActionArgId = 1, RangeMin = 0.5, RangeMax = max };

            var filcher = With(Attack(2.0));
            Assert.AreEqual(2.0f, BehaviorManager.ReachOf(filcher));
            Assert.AreEqual(1.75f, BehaviorManager.NearEnoughFor(filcher), 1e-6);
            Assert.AreEqual(1.25f, BehaviorManager.ChaseStandoffFor(filcher), 1e-6);

            var meleeThree = With(Attack(3.0));
            Assert.AreEqual(2.75f, BehaviorManager.NearEnoughFor(meleeThree), 1e-6);
            Assert.AreEqual(2.25f, BehaviorManager.ChaseStandoffFor(meleeThree), 1e-6);

            // A reach of three and a half or more, a gun, or no attack at all: as it always was.
            foreach (var creature in new[] { With(Attack(3.5)), With(Attack(2.0), Attack(30)), With() })
            {
                Assert.AreEqual(BehaviorManager.NearEnoughDistance, BehaviorManager.NearEnoughFor(creature));
                Assert.AreEqual(BehaviorManager.ChaseStandoffDistance, BehaviorManager.ChaseStandoffFor(creature));
            }

            // A habit with no range does not count as the creature's reach.
            var looter = With(Attack(2.0), new CreatureAction { ActionId = CreatureHabits.FilcherLoot, ActionArgId = 1, RangeMax = 0 });
            Assert.AreEqual(2.0f, BehaviorManager.ReachOf(looter));

            // A very short reach still stands somewhere, at half of it.
            var tiny = With(Attack(0.5));
            Assert.AreEqual(0.25f, BehaviorManager.NearEnoughFor(tiny), 1e-6);
            Assert.AreEqual(0.25f, BehaviorManager.ChaseStandoffFor(tiny), 1e-6);
        }

        private static Creature Spawn(WorldTestContext world, float x, double rangeMin, double rangeMax)
        {
            var creature = new Creature
            {
                Name = "Filcher",
                TargetCategory = TargetCategory.Hostile,
                MapContextId = world.Map.MapInfo.MapContextId,
                RuntimeMapChannel = world.Map,
                Position = new Vector3(x, 0, 0),
                EntityClass = EntityClasses.HumanBaseMale,
                State = CharacterState.Idle,
                Level = 1,
                AggroRange = 0,
                RunSpeed = 6,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>()
            };

            creature.HomePos.Position = creature.Position;
            creature.HomePos.MapContextid = creature.MapContextId;
            creature.Attributes[Attributes.Health] = CreatureHealth.NewAttribute(1000);
            creature.Actions.Add(new CreatureAction { ActionId = ActionId.CrFilcherMelee, ActionArgId = 1, RangeMin = rangeMin, RangeMax = rangeMax, MinDamage = 1, MaxDamage = 1, Cooldown = 4000 });
            BehaviorManager.StartWandering(creature, false);
            EntityManager.Instance.RegisterEntity(creature.EntityId, EntityType.Creature);
            EntityManager.Instance.RegisterCreature(creature);
            EntityManager.Instance.RegisterActor(creature.EntityId, creature);
            var seed = CellManager.Instance.GetCellSeed(creature.Position);
            creature.Cells = CellsAt(world, creature.Position);
            CellManager.Instance.GetCell(world.Map, seed & 0xFFFF, seed >> 16).CreatureList.Add(creature);
            return creature;
        }

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
    }
}
