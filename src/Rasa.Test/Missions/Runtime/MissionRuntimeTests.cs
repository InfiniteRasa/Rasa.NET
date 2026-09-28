using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions.Runtime
{
    using Rasa.Data;
    using Rasa.Missions.Runtime;
    using Rasa.Structures;

    [TestClass]
    public class MissionRuntimeTests
    {
        [TestMethod]
        public void ThousandUnrelatedDefinitionsAddNoHitOrNpcEvaluations()
        {
            var definitions = Enumerable.Range(1, 1000)
                .Select(id => Definition((uint)id, (uint)id)).ToArray();
            var runtime = new MissionRuntime(definitions);
            var mission = definitions[42];
            var journal = new Dictionary<uint, MissionLog>
            {
                [mission.MissionId] = new(mission.MissionId, MissionState.Active, false,
                    mission.CreateInitialObjectiveLogs())
            };

            var matches = runtime.SelectCandidates(journal,
                new[] { MissionProgressEvent.Creature(43) });

            Assert.AreEqual(1, matches.Count);
            Assert.AreEqual(1, runtime.LastRuleEvaluations);
            Assert.AreEqual(1, runtime.ForNpc(43, 0).Count);
            Assert.AreEqual(0, runtime.SelectCandidates(journal,
                new[] { MissionProgressEvent.Creature(2000) }).Count);
            Assert.AreEqual(0, runtime.LastRuleEvaluations);
        }

        [TestMethod]
        public void CounterDecisionClampsAndDoesNotMutateBeforeCommit()
        {
            var definition = Definition(1, 50, target: 3);
            var log = new MissionLog(1, MissionState.Active, false,
                definition.CreateInitialObjectiveLogs());
            var runtime = new MissionRuntime(new[] { definition });
            var candidate = runtime.SelectCandidates(
                new Dictionary<uint, MissionLog> { [1] = log },
                new[] { MissionProgressEvent.ItemAcquired(50, uint.MaxValue) }).Single();
            var decision = MissionRuntime.Evaluate(candidate, log.Objectives[1]);

            Assert.AreEqual(3U, decision.CounterValue);
            Assert.AreEqual(MissionObjectiveState.Completed, decision.State);
            Assert.AreEqual(0U, log.Objectives[1].ItemCounters[50]);
            Assert.AreEqual(MissionObjectiveState.Incomplete, log.Objectives[1].State);
        }

        [TestMethod]
        public void CounterDecisionRejectsStaleDurableState()
        {
            var definition = Definition(1, 50, target: 3);
            var log = new MissionLog(1, MissionState.Active, false,
                definition.CreateInitialObjectiveLogs());
            var runtime = new MissionRuntime(new[] { definition });
            var candidate = runtime.SelectCandidates(
                new Dictionary<uint, MissionLog> { [1] = log },
                new[] { MissionProgressEvent.ItemAcquired(50, 1) }).Single();
            var durable = new MissionObjectiveLog(1, MissionObjectiveState.Incomplete,
                null, new Dictionary<uint, uint> { [50] = 1 });

            Assert.ThrowsExactly<MissionRuleException>(() => MissionRuntime.Evaluate(candidate, durable));
        }

        [TestMethod]
        public void AdmissionAndTurnInArePersonalDecisions()
        {
            Assert.AreEqual(MissionRejection.AlreadyRewarded,
                MissionRuntime.Admit(true, false, true, 0).Rejection);
            Assert.AreEqual(MissionRejection.JournalFull,
                MissionRuntime.Admit(true, false, false, 30).Rejection);
            Assert.IsTrue(MissionRuntime.Admit(true, false, false, 29).Accepted);
            Assert.IsFalse(MissionRuntime.CanTurnIn(
                new MissionLog(1, MissionState.Active, false), MissionState.Active, true).Accepted);
        }

        private static Mission Definition(uint id, uint subject, uint? target = null)
        {
            var objective = new MissionObjectiveDefinition(1, 1, 1,
                new uint?[] { null, null, null }, 0, MissionObjectiveState.Incomplete, true,
                new Dictionary<uint, MissionObjectiveCounterDefinition>(),
                target.HasValue
                    ? new Dictionary<uint, MissionObjectiveItemCounterDefinition>
                    {
                        [subject] = new(subject, 0, target.Value)
                    }
                    : new Dictionary<uint, MissionObjectiveItemCounterDefinition>(),
                Array.Empty<MissionObjectiveConversation>(), Array.Empty<uint>(), Array.Empty<uint>(),
                Array.Empty<MissionIndicator>(),
                target.HasValue
                    ? MissionProgressRule.IncrementItemCounterOnExactSubject(
                        MissionProgressEventKind.ItemAcquired, subject, 0, target.Value)
                    : MissionProgressRule.CompleteOnExactSubject(MissionProgressEventKind.CreatureKilled, subject));
            return new Mission(id, $"Mission {id}", id, id, id, 1, 1, 1, false, false,
                new[] { objective }, true);
        }
    }
}
