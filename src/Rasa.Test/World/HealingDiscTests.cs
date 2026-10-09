using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Structures;

    // The healing disc (TOOL_HEALING_DISC) and what it may be used on. healdisc.py's own check,
    // ExtraCheckAction: a creature that has neither the BIOLOGICAL flag nor the MACHINA one is
    // TARGET_INVALID. The client asks that of the flags CreatureInfo gave it, and the server of
    // the same list. The repair tool replaces the check with its own and is not held to it.
    [TestClass]
    [DoNotParallelize]
    public class HealingDiscTests
    {
        private const EntityClasses SoldierClass = (EntityClasses)990791;
        private const EntityClasses TurretClass = (EntityClasses)990792;

        private readonly List<Creature> _creatures = new List<Creature>();

        [TestCleanup]
        public void ForgetTheCreatures()
        {
            foreach (var creature in _creatures)
                RepairToolTests.Forget(creature);

            _creatures.Clear();
        }

        [TestMethod]
        public void ABiologicalCreatureAndAMachinaAreHealed()
        {
            using var world = new WorldTestContext();
            var medic = RepairToolTests.Arm(world, world.CreateClient(), ActionId.ToolHealingDisc, healing: 0);
            var soldier = Creature(world, 5, dead: false, CreatureFlag.Biological);
            // As the world's twelve Machina classes are flagged: MACHINA and MECHANICAL both.
            var machina = Creature(world, 6, dead: false, CreatureFlag.Machina, CreatureFlag.Mechanical);

            foreach (var hurt in new[] { soldier, machina })
            {
                var landed = RepairToolTests.Use(world, medic, ActionId.ToolHealingDisc, hurt.EntityId);

                Assert.AreEqual(hurt.EntityId, landed.Hits.Single().EntityId);
                Assert.AreEqual(100 + RepairToolTests.Amount, hurt.Attributes[Attributes.Health].Current);
            }
        }

        [TestMethod]
        public void ACreatureThatIsNeitherIsNoTargetForTheDisc()
        {
            using var world = new WorldTestContext();
            var medic = RepairToolTests.Arm(world, world.CreateClient(), ActionId.ToolHealingDisc, healing: 0);
            var turret = Creature(world, 5, dead: false, CreatureFlag.Mechanical);
            // No flag of any kind on its class: the client has nothing to say yes to either.
            var unknown = Creature(world, 6, dead: false);
            var plant = Creature(world, 7, dead: false, CreatureFlag.KingdomPlant, CreatureFlag.SpeciesTreeback);

            foreach (var other in new[] { turret, unknown, plant })
            {
                Assert.AreEqual(PlayerMessage.PmTargetInvalid, RepairToolTests.Refused(world, medic, ActionId.ToolHealingDisc, other.EntityId));
                Assert.AreEqual(100, other.Attributes[Attributes.Health].Current);
            }
        }

        [TestMethod]
        public void TheFlagsOfTheCreaturesClassCountAsItsOwnDo()
        {
            using var world = new WorldTestContext();
            var medic = RepairToolTests.Arm(world, world.CreateClient(), ActionId.ToolHealingDisc, healing: 0);

            world.AddClass(SoldierClass);
            EntityClassManager.Instance.LoadedEntityClasses[SoldierClass].CreatureFlags =
                new List<CreatureFlag> { CreatureFlag.SpeciesHuman, CreatureFlag.Biological };
            world.AddClass(TurretClass);
            EntityClassManager.Instance.LoadedEntityClasses[TurretClass].CreatureFlags =
                new List<CreatureFlag> { CreatureFlag.SpeciesTurret, CreatureFlag.Mechanical };

            var soldier = Creature(world, 5, dead: false);
            soldier.EntityClass = SoldierClass;
            var turret = Creature(world, 6, dead: false);
            turret.EntityClass = TurretClass;

            // What the client is sent for each is what the server goes by.
            CollectionAssert.Contains(CreatureManager.CreatureFlagsOf(soldier), (int)CreatureFlag.Biological);
            Assert.IsTrue(ToolActionManager.DiscHeals(soldier));
            Assert.IsFalse(ToolActionManager.DiscHeals(turret));

            RepairToolTests.Use(world, medic, ActionId.ToolHealingDisc, soldier.EntityId);
            Assert.AreEqual(100 + RepairToolTests.Amount, soldier.Attributes[Attributes.Health].Current);

            Assert.AreEqual(PlayerMessage.PmTargetInvalid, RepairToolTests.Refused(world, medic, ActionId.ToolHealingDisc, turret.EntityId));
        }

        [TestMethod]
        public void ADeadCreatureThatIsNeitherIsRefusedForBeingDeadFirst()
        {
            using var world = new WorldTestContext();
            var medic = RepairToolTests.Arm(world, world.CreateClient(), ActionId.ToolHealingDisc, healing: 3);
            var wreck = Creature(world, 5, dead: true, CreatureFlag.Mechanical);
            var soldier = Creature(world, 6, dead: true, CreatureFlag.Biological);

            // At Healing 3 the disc may be aimed at the dead, and at this one still not.
            Assert.AreEqual(PlayerMessage.PmTargetInvalid, RepairToolTests.Refused(world, medic, ActionId.ToolHealingDisc, wreck.EntityId));
            RepairToolTests.Use(world, medic, ActionId.ToolHealingDisc, soldier.EntityId);

            // Below it the client's first check answers, as for any dead target (CheckAction).
            medic.Player.Skills[SkillId.SpecialistTools] = new SkillsData(SkillId.SpecialistTools, -1, 2);

            Assert.AreEqual(PlayerMessage.PmActionFailedTargetDead, RepairToolTests.Refused(world, medic, ActionId.ToolHealingDisc, wreck.EntityId));
        }

        [TestMethod]
        public void APlayerIsHealedAsBefore()
        {
            using var world = new WorldTestContext();
            var medic = RepairToolTests.Arm(world, world.CreateClient(), ActionId.ToolHealingDisc, healing: 0);
            var trooper = world.CreateClient(5, 0);
            trooper.Player.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 500, 500, 100, 0, 0);

            var landed = RepairToolTests.Use(world, medic, ActionId.ToolHealingDisc, trooper.Player.EntityId);

            Assert.AreEqual(trooper.Player.EntityId, landed.Hits.Single().EntityId);
            Assert.AreEqual(100 + RepairToolTests.Amount, trooper.Player.Attributes[Attributes.Health].Current);
        }

        [TestMethod]
        public void TheRepairToolIsNotHeldToTheDiscsRule()
        {
            using var world = new WorldTestContext();
            var engineer = RepairToolTests.Arm(world, world.CreateClient(), ActionId.ToolFieldRepair, healing: 0);
            var unknown = Creature(world, 5, dead: false);

            // repairtool.py's ExtraCheckAction stands in place of the disc's, and asks only about the dead.
            RepairToolTests.Use(world, engineer, ActionId.ToolFieldRepair, unknown.EntityId);

            Assert.AreEqual(100 + RepairToolTests.Amount, unknown.Attributes[Attributes.Health].Current);
        }

        private Creature Creature(WorldTestContext world, float x, bool dead, params CreatureFlag[] flags)
        {
            var creature = RepairToolTests.NewCreature(world, x, dead, flags);

            _creatures.Add(creature);
            return creature;
        }
    }
}
