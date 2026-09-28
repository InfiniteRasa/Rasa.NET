using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions.Runtime
{
    using Rasa.Data;
    using Rasa.Missions.Runtime;

    [TestClass]
    public class MissionRequirementTests
    {
        [TestMethod]
        public void AllAnyNotUseHistoryWithoutTreatingItAsAnActiveAssignment()
        {
            var facts = new MissionRequirementFacts(5, new Dictionary<uint, MissionState>(),
                new Dictionary<uint, MissionState> { [1990] = MissionState.Completed }, new Dictionary<uint, uint>());
            var requirement = new AllRequirements(new MissionRequirement[]
            {
                new LevelRequirement(4), new MissionStateRequirement(1990),
                new NotRequirement(new MissionStateRequirement(1990, Accepted: true)),
                new AnyRequirement(new MissionRequirement[] { new LevelRequirement(50), new LevelRequirement(5) })
            });
            Assert.IsTrue(new MissionRequirementEvaluator().Evaluate(requirement, facts));
        }

        [TestMethod]
        public void MissingCustomRequirementsFailExplicitlyInsteadOfDefaultingToEligible()
        {
            Assert.ThrowsExactly<MissionRuleException>(() => new MissionRequirementEvaluator().Evaluate(
                new CustomRequirement("missing"), new MissionRequirementFacts(1,
                    new Dictionary<uint, MissionState>(), new Dictionary<uint, MissionState>(), new Dictionary<uint, uint>())));
        }

        [TestMethod]
        public void MalformedRequirementTreesFailValidationInsteadOfBecomingEligible()
        {
            var evaluator = new MissionRequirementEvaluator();
            foreach (var requirement in new MissionRequirement[]
            {
                new AllRequirements(null), new AnyRequirement(new MissionRequirement[] { null }),
                new NotRequirement(null), new CustomRequirement("")
            })
                Assert.ThrowsExactly<MissionRuleException>(() => evaluator.RequiredFacts(requirement));
        }
    }
}
