using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Structures;
    using Rasa.Test.Missions.Wilderness;

    /// <summary>
    /// A creature row's run 9 / walk 5 is the seed's placeholder, above every class's gait gate:
    /// such a creature moves at its class's own rates (EntityMovementRates), so the client plays
    /// its walk when it walks and its run at its natural rate when it runs.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class CreatureGaitTests
    {
        private const uint ThraxSoldierPistol = 3762;
        private const uint Boargar = 6031;
        private const uint HumanMale = 3846;
        private const uint Maw = 6332;
        private const uint Filcher = 6421;

        [TestMethod]
        [DataRow(ThraxSoldierPistol, 2f, 6f, DisplayName = "Thrax: 2 and 6")]
        [DataRow(Boargar, 1.35f, 7f, DisplayName = "Boargar: 1.35 and 7")]
        [DataRow(HumanMale, 2.5f, 6.5f, DisplayName = "human: 2.5 and 6.5")]
        [DataRow(Maw, 0f, 0f, DisplayName = "Maw: stands")]
        public void ThePlaceholdersBecomeTheClassesRates(uint classId, float walk, float run)
        {
            var (resolvedWalk, resolvedRun) = EntityMovementRates.Resolve(classId, EntityMovementRates.PlaceholderWalk, EntityMovementRates.PlaceholderRun);

            Assert.AreEqual(walk, resolvedWalk, 1e-4f);
            Assert.AreEqual(run, resolvedRun, 1e-4f);

            // The client walks it below (walk + run) / 2 and runs it above: walking is now below the gate.
            Assert.IsTrue(resolvedWalk <= (walk + run) / 2);
        }

        [TestMethod]
        public void AnAuthoredSpeedIsKeptAndEachPlaceholderGoesOnItsOwn()
        {
            Assert.AreEqual((1f, 0f), EntityMovementRates.Resolve(ThraxSoldierPistol, 1f, 0f), "a patrol's walk, never running");
            Assert.AreEqual((2f, 7f), EntityMovementRates.Resolve(HumanMale, 2f, 7f), "a squad's own speeds");
            Assert.AreEqual((0f, 6f), EntityMovementRates.Resolve(ThraxSoldierPistol, 0f, 9f), "a boss that never walks runs at its class's rate");
            Assert.AreEqual((5f, 9f), EntityMovementRates.Resolve(999_999, 5f, 9f), "a class the client's table does not have keeps what it had");
        }

        [TestMethod]
        public void TheWorldsCreaturesComeOutOfTheDatabaseAtTheirClassesRates()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var loaded = harness.Creatures.LoadedCreatures.Values.ToList();

            Assert.IsFalse(loaded.Any(creature => creature.RunSpeed == EntityMovementRates.PlaceholderRun && EntityMovementRates.TryGet((uint)creature.EntityClass, out _, out _)),
                "no creature of a known class is left on the placeholder run");

            // Creature 60, the Filcher, a run 9 / walk 5 row: a Filcher walks at 2 and runs at 7.
            var filcher = harness.Creatures.LoadedCreatures[60];
            Assert.AreEqual(Filcher, (uint)filcher.EntityClass);
            Assert.AreEqual(2f, filcher.WalkSpeed, 1e-4f);
            Assert.AreEqual(7f, filcher.RunSpeed, 1e-4f);
        }
    }
}
