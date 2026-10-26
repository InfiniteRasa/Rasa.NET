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
    /// A scene set split over the client's counters (MissionProgressRule.SplitIntoCounters, MissionSceneDefinition.CounterGroups):
    /// Sabotage the Tower's (937) objective 1 counts 6 generators on counter 0 and 10 lockers on counter 1, and completes
    /// when both are full.
    ///
    /// Why these tests: the engine had one counter per objective; here two move from one rule. A group that counts the
    /// other's events, a completion at the first full group, a batch that moves only one of two counters, or a split that
    /// leaves an event out would each break the mission.
    /// </summary>
    [TestClass]
    public class MissionCounterGroupsTests
    {
        private const uint MissionId = 937, Scenario = 1;
        private static readonly uint[] Generators = Enumerable.Range(1, 6).Select(id => (uint)id).ToArray();
        private static readonly uint[] Lockers = Enumerable.Range(7, 10).Select(id => (uint)id).ToArray();

        [TestMethod]
        public void TheEventsSplitIntoOneGroupPerCounterAndNothingElse()
        {
            var rule = Split();
            Assert.IsNotNull(rule);
            Assert.HasCount(2, rule.CounterGroups);
            CollectionAssert.AreEqual(Generators, rule.CounterGroups[0].ToArray());
            CollectionAssert.AreEqual(Lockers, rule.CounterGroups[1].ToArray());
            Assert.AreEqual(16, rule.Subjects.Count);

            var all = Set(16);
            foreach (var (groups, why) in new (uint[][], string)[]
            {
                (new[] { Generators.Concat(Lockers).ToArray() }, "one group is no split"),
                (new[] { Generators, Lockers.Take(9).ToArray() }, "an event left out"),
                (new[] { Generators, Lockers.Append(6U).ToArray() }, "an event in both"),
                (new[] { Generators, Lockers.Append(17U).ToArray() }, "an event the set does not have"),
                (new[] { new uint[] { 1 }, new uint[] { 2 }, new uint[] { 3 }, Enumerable.Range(4, 13).Select(id => (uint)id).ToArray() },
                    "more counters than the client has"),
                (new[] { Generators, Array.Empty<uint>(), Lockers }, "an empty group"),
            })
                Assert.IsNull(all.SplitIntoCounters(groups.Select(group => (IReadOnlyList<uint>)group.ToList()).ToArray()), why);
            Assert.IsNull(Set(16, target: 10).SplitIntoCounters(Groups()), "a set that completes before all its events");
            Assert.IsNull(MissionProgressRule.CompleteOnExactSubject(MissionProgressEventKind.CreatureKilled, 1)
                .SplitIntoCounters(Groups()), "no scene set");
        }

        /// <summary>
        /// The loader's split (MissionObjectiveRuntimeAnalyzer.SplitCounters): good groups give the objective one counter per
        /// group; bad ones, or an objective with no scene set, give the "invalid-counter-groups" diagnostic and no rule.
        /// </summary>
        [TestMethod]
        public void TheLoaderSplitsAGoodSceneSetAndReportsABadSplit()
        {
            var transition = new Rasa.Structures.Missions.MissionObjectiveTransitionDefinition(
                new Rasa.Structures.World.MissionObjectiveTransitionEntry
                {
                    MissionId = MissionId, ContentRevision = "pools_1_16_5", ObjectiveId = 1, TransitionId = 1, Sequence = 1,
                    FromState = (byte)MissionObjectiveState.Incomplete, ToState = (byte)MissionObjectiveState.Completed
                },
                Enumerable.Range(1, 16).Select(id => new Rasa.Structures.Missions.MissionTriggerDefinition(new Rasa.Structures.World.MissionTriggerEntry
                {
                    MissionId = MissionId, ContentRevision = "pools_1_16_5", ObjectiveId = 1, TransitionId = 1, TriggerId = (uint)id,
                    Requirement = Rasa.Structures.World.MissionContentRequirement.Required, Kind = Rasa.Structures.World.MissionTriggerKind.ProgressEvent,
                    Sequence = (uint)id, EventKind = (byte)MissionProgressEventKind.ScenarioEvent, SubjectId = (uint)id,
                    CounterId = Scenario, InitialValue = 0, TargetValue = 16, Comment = $"Piece {id}"
                })).ToArray(),
                Array.Empty<Rasa.Structures.Missions.MissionActionDefinition>());
            var analysis = Rasa.Managers.MissionObjectiveRuntimeAnalyzer.Analyze(1, new[] { transition });
            Assert.IsEmpty(analysis.Diagnostics);

            var split = Rasa.Managers.MissionObjectiveRuntimeAnalyzer.SplitCounters(analysis,
                new List<List<uint>> { Generators.ToList(), Lockers.ToList() });
            Assert.IsEmpty(split.Diagnostics);
            Assert.AreEqual(((0U, 6U), (1U, 10U)), ((split.Counters[0].CounterId, split.Counters[0].TargetValue),
                (split.Counters[1].CounterId, split.Counters[1].TargetValue)));
            Assert.AreSame(split.ProgressRule, split.ExecutableTransitions.Single().ProgressRule);
            Assert.HasCount(2, split.ProgressRule.CounterGroups);

            foreach (var (runtime, groups, why) in new[]
            {
                (analysis, new List<List<uint>> { Generators.ToList(), Lockers.Skip(1).ToList() }, "an event left out"),
                (Rasa.Managers.MissionObjectiveRuntimeAnalyzer.Analyze(1, Array.Empty<Rasa.Structures.Missions.MissionObjectiveTransitionDefinition>()),
                    new List<List<uint>> { Generators.ToList(), Lockers.ToList() }, "no scene set")
            })
            {
                var bad = Rasa.Managers.MissionObjectiveRuntimeAnalyzer.SplitCounters(runtime, groups);
                Assert.AreEqual("invalid-counter-groups", bad.Diagnostics.Single().Code, why);
                Assert.IsNull(bad.ProgressRule, why);
                Assert.IsEmpty(bad.Counters, why);
            }
        }

        [TestMethod]
        public void TheObjectiveNeedsOneCounterPerGroupWithItsOwnTarget()
        {
            var rule = Split();
            Assert.IsTrue(rule.IsCompatible(Objective(rule, (0, 6), (1, 10))));
            Assert.IsFalse(rule.IsCompatible(Objective(rule, (0, 16))), "one counter for all");
            Assert.IsFalse(rule.IsCompatible(Objective(rule, (0, 10), (1, 6))), "targets swapped");
            Assert.IsFalse(rule.IsCompatible(Objective(rule, (0, 6), (1, 10), (2, 1))), "a third counter");
        }

        [TestMethod]
        public void EachCounterCountsItsOwnGroupAndTheObjectiveCompletesWhenBothAreFull()
        {
            var definition = Mission();
            var runtime = new MissionRuntime(new[] { definition });

            // A generator: counter 0 moves, counter 1 does not.
            var decision = Decide(runtime, definition, (0, 0), new uint[] { 3 }, 3);
            Assert.AreEqual((0U, 1U), (decision.CounterId.Value, decision.CounterValue.Value));
            Assert.IsNull(decision.OtherCounters);
            Assert.IsNull(decision.State);

            // A locker: counter 1.
            decision = Decide(runtime, definition, (1, 0), new uint[] { 3, 9 }, 9);
            Assert.AreEqual((1U, 1U), (decision.CounterId.Value, decision.CounterValue.Value));
            Assert.IsNull(decision.OtherCounters);

            // One already seen: nothing moves.
            decision = Decide(runtime, definition, (1, 1), new uint[] { 3, 9 }, 9);
            Assert.IsNull(decision.CounterId);
            Assert.IsNull(decision.State);

            // Both groups in one batch: both move, the last one first-class.
            decision = Decide(runtime, definition, (1, 1), new uint[] { 3, 9, 4, 10 }, 4, 10);
            Assert.AreEqual((1U, 2U), (decision.CounterId.Value, decision.CounterValue.Value));
            CollectionAssert.AreEqual(new[] { (0U, 2U) }, decision.OtherCounters.ToArray());

            // All six generators: full, but the lockers are not.
            decision = Decide(runtime, definition, (5, 9), Generators.Concat(Lockers.Take(9)).ToArray(), 6);
            Assert.AreEqual((0U, 6U), (decision.CounterId.Value, decision.CounterValue.Value));
            Assert.IsNull(decision.State, "the lockers are not all down");

            // The tenth locker: both full, done.
            decision = Decide(runtime, definition, (6, 9), Generators.Concat(Lockers).ToArray(), 16);
            Assert.AreEqual((1U, 10U), (decision.CounterId.Value, decision.CounterValue.Value));
            Assert.AreEqual(MissionObjectiveState.Completed, decision.State);
        }

        [TestMethod]
        public void AStaleCounterIsRefused()
        {
            var definition = Mission();
            var runtime = new MissionRuntime(new[] { definition });
            var log = Log(definition, 1, 0);
            var candidate = runtime.SelectCandidates(new Dictionary<uint, MissionLog> { [MissionId] = log },
                new[] { MissionProgressEvent.Scenario(MissionId, Scenario, 2) }).Single();
            var durable = new MissionObjectiveLog(1, MissionObjectiveState.Incomplete,
                new Dictionary<uint, uint> { [0] = 2, [1] = 0 }, null);

            Assert.ThrowsExactly<MissionRuleException>(() => MissionRuntime.Evaluate(candidate, durable, new HashSet<uint> { 1, 2 }),
                "the runtime says 1, the save 2");
            durable = new MissionObjectiveLog(1, MissionObjectiveState.Incomplete, new Dictionary<uint, uint> { [0] = 1, [1] = 0 }, null);
            Assert.ThrowsExactly<MissionRuleException>(() => MissionRuntime.Evaluate(candidate, durable, new HashSet<uint>()),
                "fewer seen than counted");
        }

        private static ObjectiveDecision Decide(MissionRuntime runtime, Mission definition, (uint Generators, uint Lockers) counted,
            uint[] seen, params uint[] events)
        {
            var log = Log(definition, counted.Generators, counted.Lockers);
            var candidate = runtime.SelectCandidates(new Dictionary<uint, MissionLog> { [MissionId] = log },
                events.Select(id => MissionProgressEvent.Scenario(MissionId, Scenario, id)).ToArray()).Single();
            CollectionAssert.AreEquivalent(events, candidate.DistinctSubjects.ToArray(), "one candidate for the batch");
            return MissionRuntime.Evaluate(candidate, log.Objectives[1], seen.ToHashSet());
        }

        private static MissionLog Log(Mission definition, uint generators, uint lockers) =>
            new(MissionId, MissionState.Active, false, new Dictionary<uint, MissionObjectiveLog>
            {
                [1] = new(1, MissionObjectiveState.Incomplete, new Dictionary<uint, uint> { [0] = generators, [1] = lockers }, null)
            });

        private static MissionProgressRule Set(uint events, uint? target = null) =>
            MissionProgressRule.CompleteOnDistinctScenarioEvents(MissionId, Scenario,
                Enumerable.Range(1, (int)events).Select(id => (uint)id).ToHashSet(), target ?? events);

        private static IReadOnlyList<IReadOnlyList<uint>> Groups() =>
            new IReadOnlyList<uint>[] { Generators.ToList(), Lockers.ToList() };

        private static MissionProgressRule Split() => Set(16).SplitIntoCounters(Groups());

        private static MissionObjectiveDefinition Objective(MissionProgressRule rule, params (uint Id, uint Target)[] counters) =>
            new(1, 5050, 7606, new uint?[] { 5059, 5058, null }, 0, MissionObjectiveState.Incomplete, true,
                counters.ToDictionary(counter => counter.Id, counter => new MissionObjectiveCounterDefinition(counter.Id, 0, counter.Target)),
                new Dictionary<uint, MissionObjectiveItemCounterDefinition>(),
                Array.Empty<MissionObjectiveConversation>(), Array.Empty<uint>(), Array.Empty<uint>(),
                Array.Empty<MissionIndicator>(), rule);

        private static Mission Mission() =>
            new(MissionId, "Sabotage the Tower", 5046, 1, 1, 1, 1, 1, false, false,
                new[] { Objective(Split(), (0, 6), (1, 10)) }, true);
    }
}
