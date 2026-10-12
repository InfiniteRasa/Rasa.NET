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
    /// A creature's attack that throws without a KNOCKBACK_DISTANCE throws out of its area - the
    /// Strider ground pulse to the edge of its 20 m - or, with no area, 10 m: the Atta Soldier's
    /// rock throw, held down for its DURATION_KNOCK_BACK (PlayerCrowdControl).
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class CreatureKnockbackTests
    {
        private const string GroundPulse = "abilities.ai.stridergroundpulseability";
        private const string RockThrow = "abilities.ai.attarockthrowability";

        private ActionInfo _previous;
        private bool _seeded;
        private Creature _strider;

        [TestCleanup]
        public void Cleanup()
        {
            if (_strider != null)
            {
                EntityManager.Instance.UnregisterCreature(_strider.EntityId);
                EntityManager.Instance.UnregisterActor(_strider.EntityId);
                EntityManager.Instance.UnregisterEntity(_strider.EntityId);
                _strider = null;
            }

            if (_seeded)
            {
                if (_previous != null)
                    Actions()[ActionId.CrStriderGroundPulse] = _previous;
                else
                    Actions().Remove(ActionId.CrStriderGroundPulse);

                _seeded = false;
            }
        }

        [TestMethod]
        public void AThrowWithoutADistanceGoesOutOfItsAreaOrTenMetresWithoutOne()
        {
            var origin = Vector3.Zero;

            // The ground pulse: RADIUS_AROUND_SOURCE 20 and a class that throws.
            Assert.AreEqual(15f, PlayerCrowdControl.KnockbackDistanceOf(GroundPulse, Pulse(), origin, new Vector3(5, 0, 0)), 0.001f);
            Assert.AreEqual(1f, PlayerCrowdControl.KnockbackDistanceOf(GroundPulse, Pulse(), origin, new Vector3(0, 0, 19)), 0.001f);
            Assert.AreEqual(0f, PlayerCrowdControl.KnockbackDistanceOf(GroundPulse, Pulse(), origin, new Vector3(25, 0, 0)), "outside already");

            // The rock throw: a chance, no distance, no area.
            Assert.AreEqual(CrowdControl.DefaultKnockbackDistance, PlayerCrowdControl.KnockbackDistanceOf(RockThrow, Rock(2), origin, new Vector3(30, 0, 0)));
            Assert.AreEqual(10f, CrowdControl.DefaultKnockbackDistance, "the client's KNOCKBACK_DEFAULT_DISTANCE");

            // What the data gives stands: a distance, and an explicit 0 (CR_THRAX_LIGHTNING).
            var pound = new ActionLevelInfo { ActionId = ActionId.CrKaelGroundPound, Level = 1 };
            pound.Properties[AbilityProperty.RadiusAroundSource] = 5;
            pound.Properties[AbilityProperty.KnockbackDistance] = 10;
            Assert.AreEqual(10f, PlayerCrowdControl.KnockbackDistanceOf("abilities.ai.kaelgroundpoundability", pound, origin, new Vector3(2, 0, 0)));

            var lightning = new ActionLevelInfo { ActionId = ActionId.CrThraxLightning, Level = 1 };
            lightning.Properties[AbilityProperty.KnockbackDistance] = 0;
            Assert.AreEqual(0f, PlayerCrowdControl.KnockbackDistanceOf("abilities.knockback", lightning, origin, new Vector3(2, 0, 0)));

            // A plain hit throws nobody.
            Assert.AreEqual(0f, PlayerCrowdControl.KnockbackDistanceOf("abilities.ai.aidirectdamageability",
                new ActionLevelInfo { ActionId = ActionId.CrWardenBotLaser, Level = 1 }, origin, new Vector3(2, 0, 0)));
        }

        [TestMethod]
        public void TheRockThrowKeepsThemDownForItsKnockbackDuration()
        {
            Assert.AreEqual(2000, PlayerCrowdControl.ExtraDownMs(RockThrow, Rock(1), 0), "argument 1: 2 s");
            Assert.AreEqual(3000, PlayerCrowdControl.ExtraDownMs(RockThrow, Rock(2), 0), "argument 2: 3 s");
            Assert.AreEqual(0, PlayerCrowdControl.ExtraDownMs(GroundPulse, Pulse(), 0));
            Assert.AreEqual(8000, PlayerCrowdControl.ExtraDownMs("abilities.tectonicstrike", Pulse(), 8000), "a Tectonic Strike's stun, as before");
        }

        [TestMethod]
        public void AStridersGroundPulseThrowsAPlayerOutToTheEdgeOfIt()
        {
            _seeded = true;
            var actions = Actions();
            actions.TryGetValue(ActionId.CrStriderGroundPulse, out _previous);
            var action = new ActionInfo { ActionId = ActionId.CrStriderGroundPulse, Name = "CR_STRIDER_GROUND_PULSE", Module = GroundPulse };
            action.Levels[1] = Pulse();
            actions[ActionId.CrStriderGroundPulse] = action;

            using var world = new WorldTestContext();
            var client = world.CreateClient(x: 5);
            var player = client.Player;
            player.State = CharacterState.Normal;
            foreach (var attribute in new[] { Attributes.Health, Attributes.Armor, Attributes.Power, Attributes.Regen })
                player.Attributes[attribute] = new ActorAttributes(attribute, 1000, 1000, 1000, 0, 0);

            _strider = new Creature
            {
                Name = "Strider",
                TargetCategory = TargetCategory.Hostile,
                MapContextId = world.Map.MapInfo.MapContextId,
                RuntimeMapChannel = world.Map,
                Position = Vector3.Zero,
                EntityClass = EntityClasses.HumanBaseMale,
                State = CharacterState.Idle,
                Level = 1,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>()
            };
            _strider.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 1000, 1000, 1000, 0, 0);
            EntityManager.Instance.RegisterEntity(_strider.EntityId, EntityType.Creature);
            EntityManager.Instance.RegisterCreature(_strider);
            EntityManager.Instance.RegisterActor(_strider.EntityId, _strider);

            PlayerCrowdControl.CreatureActionHit(world.Map, _strider, player, ActionId.CrStriderGroundPulse, 1);

            Assert.IsTrue(player.ActiveEffects.Values.Any(e => e.TypeId == CrowdControl.KnockbackTypeId), "knocked back");
            Assert.AreEqual(20f, player.Position.X, 0.05f, "to the edge of the 20 m pulse");
        }

        /// <summary>CR_STRIDER_GROUND_PULSE argument 1.</summary>
        private static ActionLevelInfo Pulse()
        {
            var info = new ActionLevelInfo { ActionId = ActionId.CrStriderGroundPulse, Level = 1 };
            info.Properties[AbilityProperty.RadiusAroundSource] = 20;
            info.Properties[AbilityProperty.DamageAmountMin] = 113;
            info.Properties[AbilityProperty.DamageAmountMax] = 150;
            info.Properties[AbilityProperty.DamageScaleType] = 2;
            info.Properties[AbilityProperty.DamageType] = 7;
            return info;
        }

        /// <summary>CR_ATTA_SOLDIER_ROCK_THROW: argument 1 (10%, 2 s) or 2 (20%, 3 s).</summary>
        private static ActionLevelInfo Rock(uint level)
        {
            var info = new ActionLevelInfo { ActionId = ActionId.CrAttaSoldierRockThrow, Level = level };
            info.Properties[AbilityProperty.ChanceKnockBack] = level == 1 ? 10 : 20;
            info.Properties[AbilityProperty.DurationKnockBack] = level == 1 ? 2 : 3;
            return info;
        }

        private static Dictionary<ActionId, ActionInfo> Actions() =>
            (Dictionary<ActionId, ActionInfo>)typeof(AbilityManager)
                .GetField("_actions", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(AbilityManager.Instance)!;
    }
}
