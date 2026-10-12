using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Structures;

    /// <summary>A creature attack's ARMOR_PIERCE_PERCENT goes past armour, as a weapon's bypass does: the Flaregasher's melee, 25.</summary>
    [TestClass]
    [DoNotParallelize]
    public class CreatureArmorPierceTests
    {
        private ActionInfo _previous;

        [TestCleanup]
        public void Cleanup()
        {
            if (_previous != null)
                Actions()[ActionId.CrFlaregasherMelee] = _previous;
            else
                Actions().Remove(ActionId.CrFlaregasherMelee);
        }

        [TestMethod]
        public void TheFlaregashersMeleePiercesAQuarterOfArmour()
        {
            Actions().TryGetValue(ActionId.CrFlaregasherMelee, out _previous);

            var action = new ActionInfo { ActionId = ActionId.CrFlaregasherMelee, Name = "CR_FLAREGASHER_MELEE", Module = "abilities.ai.flaregashermeleeability" };
            for (var level = 1u; level <= 4; level++)
            {
                var info = new ActionLevelInfo { ActionId = ActionId.CrFlaregasherMelee, Level = level };
                info.Properties[AbilityProperty.DamageAmountMin] = 63;
                info.Properties[AbilityProperty.DamageAmountMax] = 88;
                info.Properties[AbilityProperty.ArmorPiercePercent] = 25;
                action.Levels[level] = info;
            }
            Actions()[ActionId.CrFlaregasherMelee] = action;

            Assert.AreEqual(25, CreatureAttacks.ArmorPierceOf(new CreatureAction { ActionId = ActionId.CrFlaregasherMelee, ActionArgId = 1 }));
            Assert.AreEqual(25, CreatureAttacks.ArmorPierceOf(new CreatureAction { ActionId = ActionId.CrFlaregasherMelee, ActionArgId = 4 }));
            Assert.AreEqual(0, CreatureAttacks.ArmorPierceOf(new CreatureAction { ActionId = ActionId.CrFlaregasherMelee, ActionArgId = 9 }), "an argument the data has not got");
            Assert.AreEqual(0, CreatureAttacks.ArmorPierceOf(new CreatureAction { ActionId = ActionId.WeaponAttack, ActionArgId = 79 }), "a weapon pair");
            Assert.AreEqual(0, CreatureAttacks.ArmorPierceOf(null));
        }

        private static Dictionary<ActionId, ActionInfo> Actions() =>
            (Dictionary<ActionId, ActionInfo>)typeof(AbilityManager)
                .GetField("_actions", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(AbilityManager.Instance)!;
    }
}
