using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Structures;

    /// <summary>
    /// KNOCKBACK_HEADING turns a creature's knockback off straight away from it, read against the
    /// client's KNOCKBACK_DEFAULT_HEADING of 180 (CrowdControl.TurnOf): the Boargar's 105 is 75
    /// degrees off, mostly to the side and still a little away.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class CreatureKnockbackHeadingTests
    {
        private ActionInfo _previous;
        private bool _seeded;
        private Creature _boargar;

        [TestCleanup]
        public void Cleanup()
        {
            if (_boargar != null)
            {
                EntityManager.Instance.UnregisterCreature(_boargar.EntityId);
                EntityManager.Instance.UnregisterActor(_boargar.EntityId);
                EntityManager.Instance.UnregisterEntity(_boargar.EntityId);
                _boargar = null;
            }

            if (_seeded)
            {
                if (_previous != null)
                    Actions()[ActionId.CrBoargarKnockback] = _previous;
                else
                    Actions().Remove(ActionId.CrBoargarKnockback);

                _seeded = false;
            }
        }

        [TestMethod]
        public void ADirectionTurnsByItsHeadingInTheGroundPlane()
        {
            var away = new Vector3(0, 0, 1);

            Assert.AreEqual(away, CrowdControl.Turned(away, 0));

            var side = CrowdControl.Turned(away, 90);
            Assert.AreEqual(-1f, side.X, 0.0001f);
            Assert.AreEqual(0f, side.Z, 0.0001f);

            var boargar = CrowdControl.Turned(away, -75);
            Assert.AreEqual(1f, boargar.Length(), 0.0001f);
            Assert.AreEqual(0.9659f, boargar.X, 0.0001f);
            Assert.AreEqual(0.2588f, boargar.Z, 0.0001f);
        }

        [TestMethod]
        public void AHeadingIsReadAgainstTheClientsDefaultOfStraightAway()
        {
            var info = new ActionLevelInfo { ActionId = ActionId.CrBoargarKnockback, Level = 1 };

            Assert.AreEqual(0f, CrowdControl.TurnOf(info), "no heading: straight away");
            Assert.AreEqual(0f, CrowdControl.TurnOf(null));

            info.Properties[AbilityProperty.KnockbackHeading] = 180;
            Assert.AreEqual(0f, CrowdControl.TurnOf(info), "KNOCKBACK_DEFAULT_HEADING: straight away");

            info.Properties[AbilityProperty.KnockbackHeading] = 105;
            Assert.AreEqual(-75f, CrowdControl.TurnOf(info), "the Boargar's");
            Assert.AreEqual(180f, CrowdControl.DefaultKnockbackHeading);
        }

        [TestMethod]
        public void TheBoargarThrowsItsTenMetresSeventyFiveDegreesOffStraightAway()
        {
            _seeded = true;
            var actions = Actions();
            actions.TryGetValue(ActionId.CrBoargarKnockback, out _previous);

            var info = new ActionLevelInfo { ActionId = ActionId.CrBoargarKnockback, Level = 1 };
            info.Properties[AbilityProperty.DamageAmountMin] = 31;
            info.Properties[AbilityProperty.DamageAmountMax] = 63;
            info.Properties[AbilityProperty.DamageScaleType] = 2;
            info.Properties[AbilityProperty.KnockbackHeading] = 105;
            info.Properties[AbilityProperty.KnockbackDistance] = 10;
            var action = new ActionInfo { ActionId = ActionId.CrBoargarKnockback, Name = "CR_BOARGAR_KNOCKBACK", Module = "abilities.knockback" };
            action.Levels[1] = info;
            actions[ActionId.CrBoargarKnockback] = action;

            using var world = new WorldTestContext();
            var client = world.CreateClient(z: 5);
            var player = client.Player;
            player.State = CharacterState.Normal;
            foreach (var attribute in new[] { Attributes.Health, Attributes.Armor, Attributes.Power, Attributes.Regen })
                player.Attributes[attribute] = new ActorAttributes(attribute, 1000, 1000, 1000, 0, 0);

            _boargar = new Creature
            {
                Name = "Boargar",
                TargetCategory = TargetCategory.Hostile,
                MapContextId = world.Map.MapInfo.MapContextId,
                RuntimeMapChannel = world.Map,
                Position = Vector3.Zero,
                EntityClass = EntityClasses.HumanBaseMale,
                State = CharacterState.Idle,
                Level = 1,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>()
            };
            _boargar.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 1000, 1000, 1000, 0, 0);
            EntityManager.Instance.RegisterEntity(_boargar.EntityId, EntityType.Creature);
            EntityManager.Instance.RegisterCreature(_boargar);
            EntityManager.Instance.RegisterActor(_boargar.EntityId, _boargar);

            PlayerCrowdControl.CreatureActionHit(world.Map, _boargar, player, ActionId.CrBoargarKnockback, 1);

            Assert.IsTrue(player.ActiveEffects.Values.Any(e => e.TypeId == CrowdControl.KnockbackTypeId), "knocked back");
            Assert.AreEqual(9.659f, player.Position.X, 0.05f, "mostly to the side");
            Assert.AreEqual(5f + 2.588f, player.Position.Z, 0.05f, "and still a little away: 75 degrees off straight away");
        }

        private static Dictionary<ActionId, ActionInfo> Actions() =>
            (Dictionary<ActionId, ActionInfo>)typeof(AbilityManager)
                .GetField("_actions", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(AbilityManager.Instance)!;
    }
}
