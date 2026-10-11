using System;
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
    /// A Xanx's meal (CR_XANX_DEVOUR) is XANX_FORTIFY: for the argument's DURATION it raises the
    /// Xanx's maximum health by part of the heal amount - at most a quarter of its maximum - and its
    /// health with it, and the rest of the meal heals it up to the raised maximum
    /// (CreatureHabits.Fortify).
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class XanxFortifyTests
    {
        private Creature _xanx;

        [TestCleanup]
        public void Cleanup()
        {
            if (_xanx == null)
                return;

            EntityManager.Instance.UnregisterCreature(_xanx.EntityId);
            EntityManager.Instance.UnregisterActor(_xanx.EntityId);
            EntityManager.Instance.UnregisterEntity(_xanx.EntityId);
            _xanx = null;
        }

        [TestMethod]
        public void AMealRaisesTheXanxsMaximumAndItsHealthForFiveMinutesAndNoMore()
        {
            using var world = new WorldTestContext();
            var xanx = Xanx(world, 400);
            var info = Devour();
            var amount = CreatureHabits.MealAmount(xanx, info);

            Assert.IsGreaterThan(0, amount);

            CreatureHabits.Fortify(world.Map, xanx, info);

            var health = xanx.Attributes[Attributes.Health];
            var fortify = xanx.ActiveEffects.Values.Single(e => e.TypeId == CreatureHabits.XanxFortifyTypeId);

            Assert.IsGreaterThan(250, amount, "a level 10 Xanx's meal is more than the raise can take");
            Assert.AreEqual(1250, health.CurrentMax, "its maximum raised by a quarter, not by the meal's 2,181");
            Assert.AreEqual(1250, health.Current, "and the rest of the meal heals it to the raised maximum");
            Assert.IsTrue(fortify.ExpiresTick - Environment.TickCount64 is > 299000 and <= 300000, "DURATION 300 s");

            // A second meal replaces the first: one raise, not two, and not a quarter of the raised maximum.
            health.Current = 900;
            CreatureHabits.Fortify(world.Map, xanx, info);
            Assert.AreEqual(1250, health.CurrentMax);
            Assert.AreEqual(1250, health.Current);
            Assert.AreEqual(1, xanx.ActiveEffects.Values.Count(e => e.TypeId == CreatureHabits.XanxFortifyTypeId));

            // When it ends the maximum goes back, and the health is capped to it.
            var last = xanx.ActiveEffects.Values.Single(e => e.TypeId == CreatureHabits.XanxFortifyTypeId);
            GameEffectManager.Instance.DettachEffect(world.Map, xanx, last);

            Assert.AreEqual(1000, health.CurrentMax);
            Assert.IsTrue(health.Current <= 1000);
        }

        [TestMethod]
        public void AHighLevelXanxsMealRaisesItsMaximumByAQuarterNotByTheScaledHealAmount()
        {
            // Level 30: the heal amount scales to 12,337, which all went onto the maximum.
            using var world = new WorldTestContext();
            var xanx = Xanx(world, 1000, level: 30, maximum: 2652);
            var info = Devour();

            Assert.AreEqual(12337, CreatureHabits.MealAmount(xanx, info));

            CreatureHabits.Fortify(world.Map, xanx, info);

            var health = xanx.Attributes[Attributes.Health];

            Assert.AreEqual(2652 + 663, health.CurrentMax, "a quarter of 2,652");
            Assert.AreEqual(2652 + 663, health.Current, "healed to it");
        }

        [TestMethod]
        public void AMealSmallerThanAQuarterGoesOnTheMaximumWhole()
        {
            Assert.AreEqual(200, CreatureHabits.FortifyRaise(200, 1000));
            Assert.AreEqual(250, CreatureHabits.FortifyRaise(2181, 1000));
            Assert.AreEqual(0, CreatureHabits.FortifyRaise(500, 0));
        }

        [TestMethod]
        public void AXanxWhoseHealingIsBlockedGainsNothingFromAMeal()
        {
            using var world = new WorldTestContext();
            var xanx = Xanx(world, 400);

            xanx.ActiveEffects[999001] = new GameEffect
            {
                TypeId = 1, EffectId = 999001, BlocksHealing = true, IsBuff = false,
                ExpiresTick = Environment.TickCount64 + 60000
            };

            CreatureHabits.Fortify(world.Map, xanx, Devour());

            Assert.AreEqual(1000, xanx.Attributes[Attributes.Health].CurrentMax);
            Assert.AreEqual(400, xanx.Attributes[Attributes.Health].Current);
            Assert.IsFalse(xanx.ActiveEffects.Values.Any(e => e.TypeId == CreatureHabits.XanxFortifyTypeId));
        }

        /// <summary>CR_XANX_DEVOUR argument 1 as the world database has it.</summary>
        private static ActionLevelInfo Devour()
        {
            var info = new ActionLevelInfo { ActionId = CreatureHabits.XanxDevour, Level = 1, WindupMs = 1000, RecoveryMs = 5300 };
            info.Properties[AbilityProperty.Duration] = 300;
            info.Properties[AbilityProperty.HealAmountMin] = 1000;
            info.Properties[AbilityProperty.HealAmountMax] = 1000;
            info.Properties[AbilityProperty.DamageScaleType] = 2;
            return info;
        }

        private Creature Xanx(WorldTestContext world, int health, uint level = 10, int maximum = 1000)
        {
            _xanx = new Creature
            {
                Name = "Xanx",
                TargetCategory = TargetCategory.Hostile,
                MapContextId = world.Map.MapInfo.MapContextId,
                RuntimeMapChannel = world.Map,
                Position = Vector3.Zero,
                EntityClass = EntityClasses.HumanBaseMale,
                State = CharacterState.Idle,
                Level = level,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>()
            };
            _xanx.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, maximum, maximum, health, 0, 0);
            EntityManager.Instance.RegisterEntity(_xanx.EntityId, EntityType.Creature);
            EntityManager.Instance.RegisterCreature(_xanx);
            EntityManager.Instance.RegisterActor(_xanx.EntityId, _xanx);
            var seed = CellManager.Instance.GetCellSeed(_xanx.Position);
            _xanx.Cells = CellManager.Instance.CreateCellMatrix(world.Map, seed & 0xFFFF, seed >> 16);
            CellManager.Instance.GetCell(world.Map, seed & 0xFFFF, seed >> 16).CreatureList.Add(_xanx);
            return _xanx;
        }
    }
}
