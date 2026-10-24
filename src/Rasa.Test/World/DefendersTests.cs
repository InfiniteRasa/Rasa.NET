using System.Collections.Generic;
using System.Numerics;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Config;
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Structures;

    /// <summary>
    /// The defenders of a place (Defenders): their notice range by role and a turret's by its gun,
    /// a fight one starts called to the base's others, the post held, and the walk back to it.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class DefendersTests
    {
        private ControlPointDefenceConfig _config;
        private readonly List<Creature> _registered = new();
        private uint _nextPool = 9709000;

        [TestInitialize]
        public void Start()
        {
            _config = ControlPointFields.Config;
            ControlPointFields.Config = new ControlPointDefenceConfig();
        }

        [TestCleanup]
        public void Restore()
        {
            ControlPointFields.Config = _config;

            foreach (var creature in _registered)
            {
                EntityManager.Instance.UnregisterCreature(creature.EntityId);
                EntityManager.Instance.UnregisterActor(creature.EntityId);
                EntityManager.Instance.UnregisterEntity(creature.EntityId);
                EntityManager.Instance.FreeEntity(creature.EntityId);
            }
        }

        [TestMethod]
        public void ADefenderNoticesByItsRoleAndATurretOutToItsGun()
        {
            var config = new ControlPointDefenceConfig();

            Assert.AreEqual(30f, Defenders.NoticeRange(Template(TargetCategory.Friendly, 24), 18f, config), "a rifleman: 24 + 4, at least 30");
            Assert.AreEqual(34f, Defenders.NoticeRange(Template(TargetCategory.Friendly, 30), 18f, config), "a grenadier: 30 + 4");
            Assert.AreEqual(40f, Defenders.NoticeRange(Template(TargetCategory.Friendly, 60), 18f, config), "at most 40");
            Assert.AreEqual(30f, Defenders.NoticeRange(Template(TargetCategory.Friendly, 50, turret: true), 18f, config), "a turret: its gun, up to 30");
            Assert.AreEqual(18f, Defenders.NoticeRange(Template(TargetCategory.Friendly, 10, turret: true), 18f, config), "never under 18");
            Assert.AreEqual(18f, Defenders.NoticeRange(Template(TargetCategory.Hostile, 24), 18f, config), "the Bane keep theirs");
            Assert.AreEqual(18f, Defenders.NoticeRange(Template(TargetCategory.Friendly, 0), 18f, config), "unarmed");
            Assert.AreEqual(18f, Defenders.NoticeRange(Template(TargetCategory.Friendly, 24, walks: false), 18f, config), "one that cannot walk and is no turret");
            Assert.AreEqual(18f, Defenders.NoticeRange(Template(TargetCategory.Friendly, 24), 18f, new ControlPointDefenceConfig { Defenders = false }), "the rule off");
        }

        [TestMethod]
        public void ADefenderIsGivenItsRangeAsItSpawnsAndNothingElseIs()
        {
            using var world = new WorldTestContext();
            var soldier = Creature(world, TargetCategory.Friendly, new Vector3(0, 0, 0), reach: 24);
            var minion = Creature(world, TargetCategory.Friendly, new Vector3(0, 0, 0), reach: 24);
            minion.MasterEntityId = 77;
            var loose = Creature(world, TargetCategory.Friendly, new Vector3(0, 0, 0), reach: 24, pool: false);

            Defenders.Apply(soldier);
            Defenders.Apply(minion);
            Defenders.Apply(loose);

            Assert.AreEqual(30f, soldier.AggroRange);
            Assert.AreEqual(18f, minion.AggroRange);
            Assert.AreEqual(18f, loose.AggroRange);
            Assert.IsTrue(Defenders.IsDefender(soldier));
            Assert.IsFalse(Defenders.IsDefender(minion));
            Assert.IsFalse(Defenders.IsDefender(loose));
        }

        [TestMethod]
        public void ADefendersFightIsCalledToTheOthersOfItsBaseOneStepAndNoFurther()
        {
            using var world = new WorldTestContext();
            var raider = Creature(world, TargetCategory.Hostile, new Vector3(20, 0, 0), reach: 24);
            var first = Creature(world, TargetCategory.Friendly, new Vector3(0, 0, 0), reach: 24);
            var near = Creature(world, TargetCategory.Friendly, new Vector3(0, 0, 30), reach: 24);
            var beyond = Creature(world, TargetCategory.Friendly, new Vector3(0, 0, 60), reach: 24);
            var gun = Creature(world, TargetCategory.Friendly, new Vector3(-10, 0, 0), reach: 50, turret: true);
            var shortGun = Creature(world, TargetCategory.Friendly, new Vector3(-30, 0, 0), reach: 20, turret: true);
            var busy = Creature(world, TargetCategory.Friendly, new Vector3(5, 0, 5), reach: 24);
            busy.Controller.CurrentAction = BehaviorManager.BehaviorActionReturning;

            BehaviorManager.Instance.SetActionFighting(first, raider.EntityId);

            Assert.AreEqual(raider.EntityId, near.Controller.ActionFighting.TargetEntityId, "30 m off: called");
            Assert.AreEqual(BehaviorManager.BehaviorActionFighting, near.Controller.CurrentAction);
            Assert.AreEqual(BehaviorManager.BehaviorActionFighting, gun.Controller.CurrentAction, "a gun that reaches it");
            Assert.AreNotEqual(BehaviorManager.BehaviorActionFighting, shortGun.Controller.CurrentAction, "a gun that cannot reach it: 50 m");
            Assert.AreNotEqual(BehaviorManager.BehaviorActionFighting, beyond.Controller.CurrentAction, "60 m from the first, 30 from the one called: no further than one step");
            Assert.AreEqual(BehaviorManager.BehaviorActionReturning, busy.Controller.CurrentAction, "at something else");
            Assert.AreNotEqual(BehaviorManager.BehaviorActionFighting, raider.Controller.CurrentAction);
        }

        [TestMethod]
        public void TheBaneFightAloneAndTheRuleOffEveryoneDoes()
        {
            using var world = new WorldTestContext();
            var soldier = Creature(world, TargetCategory.Friendly, new Vector3(20, 0, 0), reach: 24);
            var bane = Creature(world, TargetCategory.Hostile, new Vector3(0, 0, 0), reach: 24);
            var otherBane = Creature(world, TargetCategory.Hostile, new Vector3(0, 0, 10), reach: 24);

            BehaviorManager.Instance.SetActionFighting(bane, soldier.EntityId);
            Assert.AreNotEqual(BehaviorManager.BehaviorActionFighting, otherBane.Controller.CurrentAction, "a wild camp is no base");

            ControlPointFields.Config = new ControlPointDefenceConfig { Defenders = false };

            var first = Creature(world, TargetCategory.Friendly, new Vector3(0, 0, 0), reach: 24);
            var second = Creature(world, TargetCategory.Friendly, new Vector3(0, 0, 10), reach: 24);

            BehaviorManager.Instance.SetActionFighting(first, bane.EntityId);
            Assert.AreNotEqual(BehaviorManager.BehaviorActionFighting, second.Controller.CurrentAction);
        }

        [TestMethod]
        public void ADefenderLetsGoATargetGoneTooFarFromItsPost()
        {
            using var world = new WorldTestContext();
            var soldier = Creature(world, TargetCategory.Friendly, new Vector3(0, 0, 0), reach: 24);
            soldier.AggroRange = 30f;

            // Its reach is 30 (its notice range), and 5 more.
            Assert.IsFalse(Defenders.LetsGo(soldier, new Vector3(0, 0, 35)));
            Assert.IsTrue(Defenders.LetsGo(soldier, new Vector3(0, 0, 35.5f)));

            // From its post, not from where it is.
            soldier.Position = new Vector3(0, 0, 30);
            Assert.IsTrue(Defenders.LetsGo(soldier, new Vector3(0, 0, 36)));

            var bane = Creature(world, TargetCategory.Hostile, new Vector3(0, 0, 0), reach: 24);
            Assert.IsFalse(Defenders.LetsGo(bane, new Vector3(0, 0, 500)), "the Bane chase as they always did");

            var gun = Creature(world, TargetCategory.Friendly, new Vector3(0, 0, 0), reach: 50, turret: true);
            Assert.IsFalse(Defenders.LetsGo(gun, new Vector3(0, 0, 500)), "a gun has its own rule");
        }

        [TestMethod]
        public void OffItsPostWhenTheFightIsOverADefenderWalksBackAndFacesOut()
        {
            using var world = new WorldTestContext();
            var soldier = Creature(world, TargetCategory.Friendly, new Vector3(0, 0, 0), reach: 24);
            var raider = Creature(world, TargetCategory.Hostile, new Vector3(20, 0, 0), reach: 24);

            BehaviorManager.Instance.SetActionFighting(soldier, raider.EntityId);
            soldier.Position = new Vector3(12, 0, 0);
            BehaviorManager.Instance.GiveUp(soldier);

            var wander = soldier.Controller.ActionWander;
            Assert.AreEqual(BehaviorManager.BehaviorActionWander, soldier.Controller.CurrentAction);
            Assert.AreEqual(BehaviorManager.WanderMoving, wander.State);
            Assert.IsTrue(wander.Arriving);
            Assert.AreEqual(Vector3.Zero, wander.WanderDestination);

            // There: it faces the way its post does.
            soldier.Position = new Vector3(0.3f, 0, 0);
            Defenders.AtPost(soldier);
            Assert.AreEqual(1.25f, soldier.LastYaw, 0.0001f);
        }

        [TestMethod]
        public void InItsFightThinkADefenderLetsATargetGoRatherThanChaseIt()
        {
            using var world = new WorldTestContext();
            var soldier = Creature(world, TargetCategory.Friendly, new Vector3(0, 0, 0), reach: 24);
            var raider = Creature(world, TargetCategory.Hostile, new Vector3(0, 0, 50), reach: 24);
            soldier.AggroRange = 30f;

            BehaviorManager.Instance.SetActionFighting(soldier, raider.EntityId);
            BehaviorManager.Instance.MapChannelThink(world.Map, 250);

            Assert.AreNotEqual(BehaviorManager.BehaviorActionFighting, soldier.Controller.CurrentAction, "50 m from its post: let go");
            Assert.AreEqual(Vector3.Zero, soldier.Position, "it did not go after it");

            // Near enough, it is fought.
            raider.Position = new Vector3(0, 0, 20);
            BehaviorManager.Instance.SetActionFighting(soldier, raider.EntityId);
            BehaviorManager.Instance.MapChannelThink(world.Map, 250);
            Assert.AreEqual(BehaviorManager.BehaviorActionFighting, soldier.Controller.CurrentAction);
        }

        #region Fixtures

        private static Creature Template(TargetCategory category, double reach, bool turret = false, bool walks = true)
        {
            var creature = new Creature
            {
                TargetCategory = category,
                EntityClass = turret ? (EntityClasses)4064 : EntityClasses.HumanBaseMale,
                RunSpeed = walks && !turret ? 6 : 0,
                WalkSpeed = walks && !turret ? 3 : 0
            };

            if (reach > 0)
                creature.Actions.Add(new CreatureAction { ActionId = ActionId.WeaponAttack, ActionArgId = 133, RangeMin = 1, RangeMax = reach, MinDamage = 5, MaxDamage = 10, Cooldown = 1000 });

            return creature;
        }

        private Creature Creature(WorldTestContext world, TargetCategory category, Vector3 at, double reach, bool turret = false, bool pool = true)
        {
            var creature = Template(category, reach, turret);

            creature.Name = "Fixture";
            creature.MapContextId = world.Map.MapInfo.MapContextId;
            creature.RuntimeMapChannel = world.Map;
            creature.Position = at;
            creature.State = CharacterState.Idle;
            creature.Level = 10;
            creature.AppearanceData = new Dictionary<EquipmentData, AppearanceData>();

            if (pool)
                creature.SpawnPool = new SpawnPool { DbId = _nextPool++, MapContextId = creature.MapContextId, RuntimeMapChannel = world.Map, Position = at, Rotation = 1.25 };

            creature.HomePos.Position = at;
            creature.HomePos.MapContextid = creature.MapContextId;
            creature.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 500, 500, 500, 0, 0);
            creature.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, 0, 0, 0, 0, 0);
            BehaviorManager.StartWandering(creature, false);
            EntityManager.Instance.RegisterEntity(creature.EntityId, EntityType.Creature);
            EntityManager.Instance.RegisterCreature(creature);
            EntityManager.Instance.RegisterActor(creature.EntityId, creature);

            var seed = CellManager.Instance.GetCellSeed(at);
            creature.Cells = CellManager.Instance.CreateCellMatrix(world.Map, seed & 0xFFFF, seed >> 16);
            CellManager.Instance.GetCell(world.Map, seed & 0xFFFF, seed >> 16).CreatureList.Add(creature);
            _registered.Add(creature);

            return creature;
        }

        #endregion
    }
}
