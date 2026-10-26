using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions.Runtime
{
    using Rasa.Data;
    using Rasa.Missions.Runtime;
    using Rasa.Structures;

    /// <summary>
    /// A scoped scene set that completes at a target below its event count (MissionProgressRule.CompleteOnDistinctScenarioEvents
    /// with a target): "any 16 of the 22 bodies" (Death Patrol, 1315). Each new event moves the counter by one, and the 16th
    /// completes the objective, whichever 16 they are.
    ///
    /// Why these tests: the set counted all its events before; a target of 16 with the old "all of them" rule would never
    /// complete, and a counter that runs past its target would be refused as stale on the next event.
    /// </summary>
    [TestClass]
    public class MissionSceneSetTargetTests
    {
        private const uint MissionId = 1315, Scenario = 1, Bodies = 22, Target = 16;

        [TestMethod]
        public void TheTargetIsFromOneToTheEventCount()
        {
            Assert.AreEqual(Target, Set(Target).TargetValue);
            Assert.AreEqual(Bodies, Set(null).TargetValue, "no target: all of them");
            Assert.AreEqual(Bodies, Set(Bodies).TargetValue);
            foreach (var target in new[] { 0U, Bodies + 1 })
                Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Set(target), $"target {target}");
        }

        [TestMethod]
        public void EachNewEventCountsOneAndTheTargetCompletesTheObjective()
        {
            var definition = Mission();
            var runtime = new MissionRuntime(new[] { definition });

            var decision = Decide(runtime, definition, 0, new uint[] { 22 }, 22);
            Assert.AreEqual((0U, 1U), (decision.CounterId.Value, decision.CounterValue.Value));
            Assert.IsNull(decision.State);

            // Fifteen bodies, any of them, then the sixteenth: done, though six are left.
            var fifteen = Enumerable.Range(3, 15).Select(id => (uint)id).ToArray();
            decision = Decide(runtime, definition, 15, fifteen.Append(20U).ToArray(), 20);
            Assert.AreEqual((0U, Target), (decision.CounterId.Value, decision.CounterValue.Value));
            Assert.AreEqual(MissionObjectiveState.Completed, decision.State);

            // Two new ones in one batch at 15: the counter stops at the target.
            decision = Decide(runtime, definition, 15, fifteen.Concat(new uint[] { 20, 21 }).ToArray(), 20, 21);
            Assert.AreEqual((0U, Target), (decision.CounterId.Value, decision.CounterValue.Value));
            Assert.AreEqual(MissionObjectiveState.Completed, decision.State);
        }

        private static ObjectiveDecision Decide(MissionRuntime runtime, Mission definition, uint counted, uint[] seen,
            params uint[] events)
        {
            var log = new MissionLog(MissionId, MissionState.Active, false, new Dictionary<uint, MissionObjectiveLog>
            {
                [1] = new(1, MissionObjectiveState.Incomplete, new Dictionary<uint, uint> { [0] = counted }, null)
            });
            var candidate = runtime.SelectCandidates(new Dictionary<uint, MissionLog> { [MissionId] = log },
                events.Select(id => MissionProgressEvent.Scenario(MissionId, Scenario, id)).ToArray()).Single();
            return MissionRuntime.Evaluate(candidate, log.Objectives[1], seen.ToHashSet());
        }

        private static MissionProgressRule Set(uint? target) =>
            MissionProgressRule.CompleteOnDistinctScenarioEvents(MissionId, Scenario,
                Enumerable.Range(1, (int)Bodies).Select(id => (uint)id).ToHashSet(), target);

        private static Mission Mission() =>
            new(MissionId, "Death Patrol", 5046, 1, 1, 1, 1, 1, false, false,
                new[]
                {
                    new MissionObjectiveDefinition(1, 5050, 7606, new uint?[] { 5059, null, null }, 0, MissionObjectiveState.Incomplete, true,
                        new Dictionary<uint, MissionObjectiveCounterDefinition> { [0] = new(0, 0, Target) },
                        new Dictionary<uint, MissionObjectiveItemCounterDefinition>(),
                        Array.Empty<MissionObjectiveConversation>(), Array.Empty<uint>(), Array.Empty<uint>(),
                        Array.Empty<MissionIndicator>(), Set(Target))
                }, true);
    }
}
