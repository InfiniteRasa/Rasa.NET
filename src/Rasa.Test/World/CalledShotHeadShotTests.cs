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
    using Rasa.Structures;

    /// <summary>
    /// Called Shot 5: Head Shot, "Damage Received: +50%". The hit that sets the aim off is a head
    /// shot as well as the hits in the wound's five seconds after it; it was the one hit the pump
    /// left alone, which with a rifle that kills in two or three shots is most of what it could
    /// have done.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class CalledShotHeadShotTests
    {
        private const int Shot = 4500;

        [TestMethod]
        public void TheShotThatSetsItOffIsAHeadShotAndSoAreTheShotsInTheWound()
        {
            using var world = new WorldTestContext();
            var shooter = Shooter(world);
            var creature = Spawn(world, shooter.Player.Position + new Vector3(0, 0, -10));

            AbilityManager.Instance.ArmCalledShot(world.Map, shooter, shooter.Player, creature, Pump(5));

            Assert.AreEqual(6750, Fire(world, shooter, creature), "the shot that takes the aim off");

            var wound = creature.ActiveEffects.Values.Single();
            Assert.AreEqual(295, wound.TypeId, "CALLED_SHOT_HEAD in the aim's place");
            Assert.AreEqual(-50, wound.ResistModifier);
            Assert.IsTrue(Math.Abs(wound.ExpiresTick - Environment.TickCount64 - 5000) < 1000, "for EFFECT_DURATION_MS");

            Assert.AreEqual(6750, Fire(world, shooter, creature));
            Assert.AreEqual(6750, Fire(world, shooter, creature));

            // Its five seconds over: back to the plain shot.
            wound.ExpiresTick = Environment.TickCount64 - 1;
            GameEffectManager.Instance.DoWork(world.Map, 100);

            Assert.IsEmpty(creature.ActiveEffects.Values.ToList());
            Assert.AreEqual(Shot, Fire(world, shooter, creature));
        }

        [TestMethod]
        public void ItIsNeverHalfAgainTwice()
        {
            using var world = new WorldTestContext();
            var shooter = Shooter(world);
            var creature = Spawn(world, shooter.Player.Position + new Vector3(0, 0, -10));

            AbilityManager.Instance.ArmCalledShot(world.Map, shooter, shooter.Player, creature, Pump(5));
            Fire(world, shooter, creature);

            // Called again while the wound is on (pump 5 has no cooldown): the new aim and the
            // wound are both on until a hit lands, and count once.
            AbilityManager.Instance.ArmCalledShot(world.Map, shooter, shooter.Player, creature, Pump(5));

            Assert.HasCount(2, creature.ActiveEffects.Values.ToList());
            Assert.AreEqual(-50, GameEffectManager.ResistModifierOf(creature, DamageType.Physical), "not -100");
            Assert.AreEqual(6750, Fire(world, shooter, creature), "half again, not twice");
            Assert.AreEqual(295, creature.ActiveEffects.Values.Single().TypeId, "the old wound replaced by the new");

            // Anything else's vulnerability still adds to it (a Polarity Field, a resist debuff).
            creature.ActiveEffects[9001] = new GameEffect { EffectId = 9001, ResistModifier = -10 };
            Assert.AreEqual(-60, GameEffectManager.ResistModifierOf(creature, DamageType.Physical));
        }

        [TestMethod]
        public void TheOtherPumpsLeaveTheShotThatSetsThemOffAlone()
        {
            for (uint pump = 1; pump <= 4; pump++)
            {
                using var world = new WorldTestContext();
                var shooter = Shooter(world);
                var creature = Spawn(world, shooter.Player.Position + new Vector3(0, 0, -10));

                AbilityManager.Instance.ArmCalledShot(world.Map, shooter, shooter.Player, creature, Pump(pump));

                Assert.AreEqual(0, creature.ActiveEffects.Values.Single().ResistModifier, $"pump {pump}'s aim");
                Assert.AreEqual(Shot, Fire(world, shooter, creature), $"pump {pump}");
                Assert.AreNotEqual(290, creature.ActiveEffects.Values.First().TypeId, $"pump {pump} went off");
            }
        }

        [TestMethod]
        public void OnlyThePumpWithAnAddedDamageModifierIsTheHead()
        {
            Assert.IsTrue(AbilityManager.IsHeadShot(Pump(5)));

            for (uint pump = 1; pump <= 4; pump++)
                Assert.IsFalse(AbilityManager.IsHeadShot(Pump(pump)), $"pump {pump}");
        }

        /// <summary>
        /// Called Shot's pumps as the client's action data has them (generated.client.actiondata,
        /// action 430; ActionPropertyPreloader rows 3028-3043).
        /// </summary>
        private static ActionLevelInfo Pump(uint level)
        {
            var info = new ActionLevelInfo { ActionId = ActionId.AaSniperCalledShot, Level = level };
            var properties = info.Properties;

            switch (level)
            {
                case 1:
                    properties[AbilityProperty.Duration] = 20;
                    properties[AbilityProperty.EffectMovementModifier] = 50;
                    properties[AbilityProperty.EffectDurationMs] = 20000;
                    break;
                case 2:
                    properties[AbilityProperty.Duration] = 20;
                    properties[AbilityProperty.EffectModifier] = 200;
                    properties[AbilityProperty.EffectDurationMs] = 20000;
                    break;
                case 3:
                    properties[AbilityProperty.Duration] = 10;
                    properties[AbilityProperty.Interval] = 1;
                    properties[AbilityProperty.EffectDurationMs] = 10000;
                    properties[AbilityProperty.DamageModifierPercent] = 10;
                    break;
                case 4:
                    properties[AbilityProperty.Duration] = 20;
                    properties[AbilityProperty.EffectDurationMs] = 20000;
                    properties[AbilityProperty.DamageModifierPercent] = -50;
                    break;
                default:
                    properties[AbilityProperty.Duration] = 20;
                    properties[AbilityProperty.EffectDurationMs] = 5000;
                    properties[AbilityProperty.DamageModifierPercent] = 50;
                    break;
            }

            return info;
        }

        /// <summary>A shot from the player that lands now and always hits; what the creature took.</summary>
        private static int Fire(WorldTestContext world, Client shooter, Creature target)
        {
            var before = target.Attributes[Attributes.Health].Current;
            var missile = new Missile
            {
                Source = shooter.Player,
                TargetActor = target,
                TargetEntityId = target.EntityId,
                DamageA = Shot,
                DamageType = DamageType.Physical,
                ActionId = ActionId.WeaponAttack,
                ActionArgId = 1,
                Missed = false
            };

            MissileManager.Instance.MissileTrigger(world.Map, missile);
            WorldTestContext.Drain(shooter);

            var taken = before - target.Attributes[Attributes.Health].Current;
            Assert.AreEqual(taken, (int)missile.Args.HitData.Single().FinalAmt, "the number shown is the damage done");

            return taken;
        }

        private static Client Shooter(WorldTestContext world)
        {
            var client = world.CreateClient(x: 0);
            var seed = CellManager.Instance.GetCellSeed(client.Player.Position);
            client.Player.Cells = CellsAt(world, client.Player.Position);
            CellManager.Instance.GetCell(world.Map, seed & 0xFFFF, seed >> 16).ClientList.Add(client);
            client.Player.State = CharacterState.Normal;
            foreach (var attribute in new[] { Attributes.Health, Attributes.Armor, Attributes.Power, Attributes.Regen })
                client.Player.Attributes[attribute] = new ActorAttributes(attribute, 100, 100, 100, 0, 0);
            WorldTestContext.Drain(client);
            return client;
        }

        /// <summary>A hostile creature with a million health and no armour, in the map's cells.</summary>
        private static Creature Spawn(WorldTestContext world, Vector3 position)
        {
            var creature = new Creature
            {
                Name = "Fixture",
                TargetCategory = TargetCategory.Hostile,
                MapContextId = world.Map.MapInfo.MapContextId,
                RuntimeMapChannel = world.Map,
                Position = position,
                EntityClass = EntityClasses.HumanBaseMale,
                State = CharacterState.Idle,
                Level = 1,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>()
            };
            creature.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 1000000, 1000000, 1000000, 0, 0);
            creature.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, 0, 0, 0, 0, 0);
            creature.Controller.CurrentAction = BehaviorManager.BehaviorActionWander;
            EntityManager.Instance.RegisterEntity(creature.EntityId, EntityType.Creature);
            EntityManager.Instance.RegisterCreature(creature);
            EntityManager.Instance.RegisterActor(creature.EntityId, creature);
            var seed = CellManager.Instance.GetCellSeed(creature.Position);
            creature.Cells = CellsAt(world, creature.Position);
            CellManager.Instance.GetCell(world.Map, seed & 0xFFFF, seed >> 16).CreatureList.Add(creature);
            return creature;
        }

        private static uint[,] CellsAt(WorldTestContext world, Vector3 position)
        {
            var seed = CellManager.Instance.GetCellSeed(position);
            return CellManager.Instance.CreateCellMatrix(world.Map, seed & 0xFFFF, seed >> 16);
        }
    }
}
