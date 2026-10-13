using System;
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
    /// A Howler's or a Predator's death bomb (CreatureBombs.OnDeath) goes on its body as it dies
    /// and goes off from there. The effect worker used to clear every effect on anything dead, the
    /// bomb with them, so the explosion found nothing to go off (BR-215): the Predator's, 3 s on,
    /// never, and the Howler's, due at once, only when the bombs ran before the effect worker.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class DeathBombTests
    {
        private static readonly FieldInfo Actions = typeof(AbilityManager).GetField("_actions", BindingFlags.Instance | BindingFlags.NonPublic);

        [TestMethod]
        public void AHowlersBombGoesOffAfterTheEffectWorkerHasPassedItsBody()
        {
            using var world = new WorldTestContext();
            var victim = Victim(world, x: 4);

            using (Level(CreatureBombs.HowlerDeath, delayMs: 0))
            {
                var howler = Spawn(world, CreatureBombs.HowlerDeath);

                Kill(world, howler);
                Assert.IsTrue(howler.ActiveEffects.Values.Any(CreatureBombs.IsDeathBomb), "the bomb on the body");

                // The effect worker's pass comes before the bombs' this tick.
                GameEffectManager.Instance.DoWork(world.Map, 500);
                CreatureBombs.Worker(world.Map);

                Assert.IsTrue(victim.Player.Attributes[Attributes.Health].Current < 1000, "the blast landed");
                Assert.IsFalse(howler.ActiveEffects.Values.Any(CreatureBombs.IsDeathBomb), "and the bomb is spent");
            }
        }

        [TestMethod]
        public void APredatorsBombStaysOnItsBodyUntilItGoesOff()
        {
            using var world = new WorldTestContext();
            Victim(world, x: 4);

            using (Level(CreatureBombs.PredatorDeath, delayMs: 3000))
            {
                var predator = Spawn(world, CreatureBombs.PredatorDeath);

                Kill(world, predator);

                // Something put on the body after the death is not kept.
                var later = new GameEffect { TypeId = 999001, EffectId = GameEffectManager.Instance.NextEffectId(world.Map), ExpiresTick = Environment.TickCount64 + 60000 };
                GameEffectManager.Instance.Attach(world.Map, predator, later);

                for (var pass = 0; pass < 3; pass++)
                    GameEffectManager.Instance.DoWork(world.Map, 1000);

                var bomb = predator.ActiveEffects.Values.Single(CreatureBombs.IsDeathBomb);
                Assert.AreEqual(CreatureBombs.PredatorDeathTypeId, bomb.TypeId);
                Assert.IsFalse(predator.ActiveEffects.ContainsKey(later.EffectId), "only the bomb stays on a body");

                // Past its backstop, with nothing having set it off: taken off.
                bomb.ExpiresTick = Environment.TickCount64 - 1;
                GameEffectManager.Instance.DoWork(world.Map, 1000);

                Assert.AreEqual(0, predator.ActiveEffects.Count);
            }
        }

        private static Client Victim(WorldTestContext world, float x)
        {
            var client = world.CreateClient(x: x);
            client.Player.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 1000, 1000, 1000, 0, 0);
            return client;
        }

        private static Creature Spawn(WorldTestContext world, ActionId deathAction)
        {
            var creature = new Creature
            {
                Name = deathAction.ToString(),
                TargetCategory = TargetCategory.Hostile,
                MapContextId = world.Map.MapInfo.MapContextId,
                RuntimeMapChannel = world.Map,
                Position = Vector3.Zero,
                EntityClass = EntityClasses.HumanBaseMale,
                State = CharacterState.Idle,
                Level = 1,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>(),
                Actions = new List<CreatureAction> { new CreatureAction { ActionId = deathAction, ActionArgId = 1, MinDamage = 50, MaxDamage = 50 } }
            };
            creature.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 100, 100, 100, 0, 0);
            EntityManager.Instance.RegisterEntity(creature.EntityId, EntityType.Creature);
            EntityManager.Instance.RegisterCreature(creature);
            EntityManager.Instance.RegisterActor(creature.EntityId, creature);
            var seed = CellManager.Instance.GetCellSeed(creature.Position);
            creature.Cells = CellManager.Instance.CreateCellMatrix(world.Map, seed & 0xFFFF, seed >> 16);
            CellManager.Instance.GetCell(world.Map, seed & 0xFFFF, seed >> 16).CreatureList.Add(creature);
            return creature;
        }

        private static void Kill(WorldTestContext world, Creature creature)
        {
            creature.Attributes[Attributes.Health].Current = 0;
            CreatureManager.Instance.HandleCreatureKill(world.Map, creature, null);
            Assert.AreEqual(CharacterState.Dead, creature.State);
        }

        /// <summary>The death action's level, as the client data has it, for as long as the test runs.</summary>
        private static IDisposable Level(ActionId actionId, int delayMs)
        {
            var actions = (Dictionary<ActionId, ActionInfo>)Actions.GetValue(AbilityManager.Instance);
            actions.TryGetValue(actionId, out var previous);

            var info = new ActionInfo { ActionId = actionId };
            var level = new ActionLevelInfo { ActionId = actionId, Level = 1 };
            level.Properties[AbilityProperty.DelayTimeMs] = delayMs;
            level.Properties[AbilityProperty.EffectRadius] = 10;
            info.Levels[1] = level;
            actions[actionId] = info;

            return new Restore(() =>
            {
                if (previous != null)
                    actions[actionId] = previous;
                else
                    actions.Remove(actionId);
            });
        }

        private sealed class Restore : IDisposable
        {
            private readonly Action _undo;

            public Restore(Action undo) => _undo = undo;

            public void Dispose() => _undo();
        }
    }
}
