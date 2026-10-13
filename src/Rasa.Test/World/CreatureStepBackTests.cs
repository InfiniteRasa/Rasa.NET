using System;
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
    /// CreatureStepBack: a creature whose target stands inside the minimum range of every attack
    /// it has backs off, facing the target, until it can strike - if it can walk backwards
    /// (CAN_WALK_BACKWARDS). Before, the branch that was to move it off was never reached: it
    /// stood, in the fight and never striking, until the stall gave the fight up.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class CreatureStepBackTests
    {
        private const long Tick = 250;

        [TestMethod]
        public void OneThatCanWalkBackwardsBacksOffFacingItsTargetAndStrikes()
        {
            using var world = new WorldTestContext();
            var enemy = Watch(world, x: 2f);
            var gunner = Spawn(world, rangeMin: 5.0, rangeMax: 20.0, backwards: true);
            var shot = gunner.Actions[0];

            try
            {
                Assert.IsTrue(BehaviorManager.Instance.TrySetActionFighting(gunner, enemy.Player.EntityId));
                gunner.Controller.ActionFighting.Opened = true;   // no waiting on the opening roll

                var backed = false;
                var struck = false;

                for (var think = 0; think < 60 && !struck; think++)
                {
                    BehaviorManager.Instance.MapChannelThink(world.Map, Tick);
                    struck = shot.CooldownTimer > 0;

                    var movement = gunner.Controller.LastMovement;

                    if (!struck && movement is { Velocity: > 0 })
                    {
                        backed = true;
                        Assert.AreEqual(CreatureStepBack.BackwardDirection, movement.Flags & 0x07, "told to the clients as a walk backwards");
                        Assert.AreEqual(gunner.WalkSpeed, movement.Velocity, 1e-4, "at its walking speed");

                        // Facing its target: the yaw of the direction to it (UpdateEntityMovement).
                        var toTarget = enemy.Player.Position - gunner.Position;
                        Assert.AreEqual(Math.Atan2(-toTarget.X, -toTarget.Z), movement.ViewDirection.X, 1e-3, "facing its target");
                    }
                }

                var distance = Vector3.Distance(gunner.Position, enemy.Player.Position);

                Assert.IsTrue(backed, "it never moved");
                Assert.IsTrue(struck, $"it stood {distance:0.##} m off and never struck");
                Assert.IsTrue(distance >= 5.0f, $"out of its minimum: {distance:0.##} m");
                Assert.IsTrue(distance <= 5.0f + CreatureStepBack.Margin + 0.01f, $"and no further than it had to: {distance:0.##} m");
                Assert.IsTrue(gunner.Position.X < 0, "away from its target, which is on the other side");
                Assert.AreEqual(BehaviorManager.BehaviorActionFighting, gunner.Controller.CurrentAction);
            }
            finally
            {
                Remove(world, gunner);
            }
        }

        [TestMethod]
        public void OneThatCannotStandsWhereItIs()
        {
            using var world = new WorldTestContext();
            var enemy = Watch(world, x: 2f);
            var gunner = Spawn(world, rangeMin: 5.0, rangeMax: 20.0, backwards: false);
            var shot = gunner.Actions[0];

            try
            {
                Assert.IsTrue(BehaviorManager.Instance.TrySetActionFighting(gunner, enemy.Player.EntityId));
                gunner.Controller.ActionFighting.Opened = true;

                for (var think = 0; think < 20; think++)
                    BehaviorManager.Instance.MapChannelThink(world.Map, Tick);

                Assert.AreEqual(Vector3.Zero, gunner.Position, "it does not back off");
                Assert.IsTrue(shot.CooldownTimer <= 0, "nor strike");
            }
            finally
            {
                Remove(world, gunner);
            }
        }

        [TestMethod]
        public void TheMinimumIsTheNearestOfItsAimedAttacks()
        {
            CreatureAction Attack(double min, double max) => new CreatureAction { ActionId = ActionId.CrFilcherMelee, ActionArgId = 1, RangeMin = min, RangeMax = max };

            var creature = new Creature();
            creature.Actions.Add(Attack(8, 30));
            creature.Actions.Add(Attack(5, 20));
            Assert.AreEqual(5f, CreatureStepBack.MinimumRangeOf(creature));
            Assert.AreEqual(5.5f, CreatureStepBack.BackOffDistanceOf(CreatureStepBack.NearestAttack(creature)));

            // A habit with no range is not an attack.
            creature.Actions.Add(new CreatureAction { ActionId = CreatureHabits.FilcherLoot, ActionArgId = 1, RangeMax = 0 });
            Assert.AreEqual(5f, CreatureStepBack.MinimumRangeOf(creature));

            // An attack it can use however near: nothing to back off for.
            creature.Actions.Add(Attack(0, 2));
            Assert.AreEqual(0f, CreatureStepBack.MinimumRangeOf(creature));
            Assert.IsNull(CreatureStepBack.Destination(creature, Vector3.Zero));

            // No further than the middle of a narrow window.
            Assert.AreEqual(5.25f, CreatureStepBack.BackOffDistanceOf(Attack(5, 5.5)), 1e-6);

            Assert.AreEqual(0f, CreatureStepBack.MinimumRangeOf(new Creature()), "no attack at all");
        }

        [TestMethod]
        public void ItWantsToOnlyInsideItsMinimumAndOnlyWithTheFlag()
        {
            var creature = new Creature { WalkSpeed = 2 };
            creature.Actions.Add(new CreatureAction { ActionId = ActionId.CrFilcherMelee, ActionArgId = 1, RangeMin = 5, RangeMax = 20 });

            Assert.IsFalse(CreatureStepBack.Wants(creature, 2), "no flag");

            creature.ExtraFlags.Add(CreatureFlag.CanWalkBackwards);
            Assert.IsTrue(CreatureStepBack.CanWalkBackwards(creature));
            Assert.IsTrue(CreatureStepBack.Wants(creature, 2));
            Assert.IsTrue(CreatureStepBack.Wants(creature, 4.99f));
            Assert.IsFalse(CreatureStepBack.Wants(creature, 5), "on its minimum it can strike");
            Assert.IsFalse(CreatureStepBack.Wants(creature, 12));

            creature.WalkSpeed = 0;
            Assert.IsFalse(CreatureStepBack.Wants(creature, 2), "it cannot walk");
        }

        [TestMethod]
        public void ItBacksStraightAwayOrTheWayItFacesFrom()
        {
            var creature = new Creature { Position = new Vector3(0, 3, 2) };
            creature.Actions.Add(new CreatureAction { ActionId = ActionId.CrFilcherMelee, ActionArgId = 1, RangeMin = 5, RangeMax = 20 });

            var away = CreatureStepBack.Destination(creature, Vector3.Zero).Value;
            Assert.AreEqual(0f, away.X, 1e-5);
            Assert.AreEqual(5.5f, away.Z, 1e-5);
            Assert.AreEqual(3f, away.Y, "at its own height, for the ground to take");

            // On top of it, facing +X (yaw atan2(-1, 0)): back towards -X.
            creature.Position = new Vector3(0.05f, 0, 0);
            creature.LastYaw = (float)Math.Atan2(-1, 0);
            var behind = CreatureStepBack.Destination(creature, Vector3.Zero).Value;
            Assert.AreEqual(-5.5f, behind.X, 1e-4);
            Assert.AreEqual(0f, behind.Z, 1e-4);

            Assert.IsTrue(CreatureStepBack.IsClear(new MapChannel(), Vector3.Zero, behind), "no navmesh: open ground");
        }

        private static Creature Spawn(WorldTestContext world, double rangeMin, double rangeMax, bool backwards)
        {
            var creature = new Creature
            {
                Name = "Gunner",
                TargetCategory = TargetCategory.Hostile,
                MapContextId = world.Map.MapInfo.MapContextId,
                RuntimeMapChannel = world.Map,
                Position = Vector3.Zero,
                EntityClass = EntityClasses.HumanBaseMale,
                State = CharacterState.Idle,
                Level = 1,
                AggroRange = 0,
                WalkSpeed = 2,
                RunSpeed = 6,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>()
            };

            if (backwards)
                creature.ExtraFlags.Add(CreatureFlag.CanWalkBackwards);

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

        private static void Remove(WorldTestContext world, Creature creature)
        {
            foreach (var cell in world.Map.MapCellInfo.Cells.Values)
                cell.CreatureList.Remove(creature);

            EntityManager.Instance.UnregisterActor(creature.EntityId);
            EntityManager.Instance.UnregisterCreature(creature.EntityId);
            EntityManager.Instance.UnregisterEntity(creature.EntityId);
            EntityManager.Instance.FreeEntity(creature.EntityId);
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
