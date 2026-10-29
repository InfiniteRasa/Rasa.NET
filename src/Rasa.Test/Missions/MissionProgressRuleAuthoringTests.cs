using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Structures.Missions;
    using Rasa.Structures.World;

    [TestClass]
    public class MissionProgressRuleAuthoringTests
    {
        [TestMethod]
        public void CounterRuleRejectsEqualRangeBeforeRuntime()
        {
            var success = MissionProgressRuleAuthoring.TryBuild(
                new[] { CreateCounterTrigger(initialValue: 5, targetValue: 5) },
                out var rule,
                out var counters,
                out var itemCounters,
                out var diagnostic);

            Assert.IsFalse(success);
            Assert.IsNull(rule);
            Assert.AreEqual(0, counters.Count);
            Assert.AreEqual(0, itemCounters.Count);
            Assert.AreEqual(
                "counter progress rule counter_id 1 has initial_value 5 and target_value 5; target_value must be greater than initial_value so runtime monotonic progress can advance.",
                diagnostic);
        }

        [TestMethod]
        public void CounterRuleRejectsDecreasingRangeBeforeRuntime()
        {
            var success = MissionProgressRuleAuthoring.TryBuild(
                new[] { CreateCounterTrigger(initialValue: 6, targetValue: 5) },
                out var rule,
                out var counters,
                out var itemCounters,
                out var diagnostic);

            Assert.IsFalse(success);
            Assert.IsNull(rule);
            Assert.AreEqual(0, counters.Count);
            Assert.AreEqual(0, itemCounters.Count);
            Assert.AreEqual(
                "counter progress rule counter_id 1 has initial_value 6 and target_value 5; target_value must be greater than initial_value so runtime monotonic progress can advance.",
                diagnostic);
        }

        [TestMethod]
        public void CounterRuleAcceptsMonotonicRange()
        {
            var success = MissionProgressRuleAuthoring.TryBuild(
                new[] { CreateCounterTrigger(initialValue: 2, targetValue: 5) },
                out var rule,
                out var counters,
                out var itemCounters,
                out var diagnostic);

            Assert.IsTrue(success);
            Assert.IsNotNull(rule);
            Assert.AreEqual(1, counters.Count);
            Assert.AreEqual(0, itemCounters.Count);
            Assert.AreEqual((uint)2, counters[1].InitialValue);
            Assert.AreEqual((uint)5, counters[1].TargetValue);
            Assert.IsNull(diagnostic);
        }

        [TestMethod]
        public void CounterRuleAcceptsUIntBoundaryRange()
        {
            var success = MissionProgressRuleAuthoring.TryBuild(
                new[] { CreateCounterTrigger(initialValue: uint.MaxValue - 1, targetValue: uint.MaxValue) },
                out var rule,
                out var counters,
                out var itemCounters,
                out var diagnostic);

            Assert.IsTrue(success);
            Assert.IsNotNull(rule);
            Assert.AreEqual(1, counters.Count);
            Assert.AreEqual(0, itemCounters.Count);
            Assert.AreEqual(uint.MaxValue - 1, counters[1].InitialValue);
            Assert.AreEqual(uint.MaxValue, counters[1].TargetValue);
            Assert.IsNull(diagnostic);
        }

        [TestMethod]
        public void ItemCounterRuleRejectsEqualRangeBeforeRuntime()
        {
            var success = MissionProgressRuleAuthoring.TryBuild(
                new[] { CreateItemCounterTrigger(initialValue: 5, targetValue: 5) },
                out var rule,
                out var counters,
                out var itemCounters,
                out var diagnostic);

            Assert.IsFalse(success);
            Assert.IsNull(rule);
            Assert.AreEqual(0, counters.Count);
            Assert.AreEqual(0, itemCounters.Count);
            Assert.AreEqual(
                "item counter progress rule subject_id 3147 has initial_value 5 and target_value 5; target_value must be greater than initial_value so runtime monotonic progress can advance.",
                diagnostic);
        }

        [TestMethod]
        public void ItemCounterRuleRejectsDecreasingRangeBeforeRuntime()
        {
            var success = MissionProgressRuleAuthoring.TryBuild(
                new[] { CreateItemCounterTrigger(initialValue: 6, targetValue: 5) },
                out var rule,
                out var counters,
                out var itemCounters,
                out var diagnostic);

            Assert.IsFalse(success);
            Assert.IsNull(rule);
            Assert.AreEqual(0, counters.Count);
            Assert.AreEqual(0, itemCounters.Count);
            Assert.AreEqual(
                "item counter progress rule subject_id 3147 has initial_value 6 and target_value 5; target_value must be greater than initial_value so runtime monotonic progress can advance.",
                diagnostic);
        }

        [TestMethod]
        public void ItemCounterRuleAcceptsMonotonicRange()
        {
            var success = MissionProgressRuleAuthoring.TryBuild(
                new[] { CreateItemCounterTrigger(initialValue: 2, targetValue: 5) },
                out var rule,
                out var counters,
                out var itemCounters,
                out var diagnostic);

            Assert.IsTrue(success);
            Assert.IsNotNull(rule);
            Assert.AreEqual(0, counters.Count);
            Assert.AreEqual(1, itemCounters.Count);
            Assert.AreEqual((uint)2, itemCounters[3147].InitialValue);
            Assert.AreEqual((uint)5, itemCounters[3147].TargetValue);
            Assert.IsNull(diagnostic);
        }

        [TestMethod]
        public void ItemCounterRuleAcceptsUIntBoundaryRange()
        {
            var success = MissionProgressRuleAuthoring.TryBuild(
                new[] { CreateItemCounterTrigger(initialValue: uint.MaxValue - 1, targetValue: uint.MaxValue) },
                out var rule,
                out var counters,
                out var itemCounters,
                out var diagnostic);

            Assert.IsTrue(success);
            Assert.IsNotNull(rule);
            Assert.AreEqual(0, counters.Count);
            Assert.AreEqual(1, itemCounters.Count);
            Assert.AreEqual(uint.MaxValue - 1, itemCounters[3147].InitialValue);
            Assert.AreEqual(uint.MaxValue, itemCounters[3147].TargetValue);
            Assert.IsNull(diagnostic);
        }

        [DataTestMethod]
        [DataRow(false, 7000U)]
        [DataRow(true, 2800U)]
        public void ItemEquippedRuleAcceptsClassOrTemplateSelection(
            bool matchTemplateId,
            uint subjectId)
        {
            var success = MissionProgressRuleAuthoring.TryBuild(
                new[]
                {
                    new MissionTriggerDefinition(new MissionTriggerEntry
                    {
                        MissionId = 321,
                        ContentRevision = "deployment_11",
                        ObjectiveId = 10,
                        TransitionId = 20,
                        TriggerId = 1,
                        Requirement = MissionContentRequirement.Required,
                        Kind = MissionTriggerKind.ProgressEvent,
                        Sequence = 1,
                        EventKind = (byte)MissionProgressEventKind.ItemEquipped,
                        SubjectId = subjectId,
                        SourceSpawnResolved = matchTemplateId ? true : null,
                        Comment = "Equip the configured item"
                    })
                },
                out var rule,
                out var counters,
                out var itemCounters,
                out var diagnostic);

            Assert.IsTrue(success);
            Assert.IsNotNull(rule);
            Assert.AreEqual(0, counters.Count);
            Assert.AreEqual(0, itemCounters.Count);
            Assert.AreEqual(matchTemplateId, rule.SourceSpawnResolved == true);
            Assert.IsNull(diagnostic);
        }

        [TestMethod]
        public void AbilityHitRuleRequiresTargetCreatureId()
        {
            var success = MissionProgressRuleAuthoring.TryBuild(
                new[]
                {
                    new MissionTriggerDefinition(new MissionTriggerEntry
                    {
                        MissionId = 321,
                        ContentRevision = "deployment_11",
                        ObjectiveId = 10,
                        TransitionId = 20,
                        TriggerId = 1,
                        Requirement = MissionContentRequirement.Required,
                        Kind = MissionTriggerKind.ProgressEvent,
                        Sequence = 1,
                        EventKind = (byte)MissionProgressEventKind.AbilityHit,
                        SubjectId = 194,
                        Comment = "Missing target creature"
                    })
                },
                out var rule,
                out var counters,
                out var itemCounters,
                out var diagnostic);

            Assert.IsFalse(success);
            Assert.IsNull(rule);
            Assert.AreEqual(0, counters.Count);
            Assert.AreEqual(0, itemCounters.Count);
            Assert.AreEqual(
                "ability hit progress rules must declare subject_id as action_id and counter_id as target creature_id, without counter ranges or source_spawn_resolved.",
                diagnostic);
        }

        [TestMethod]
        public void ScenarioEventRuleCapturesMissionScenarioAndStep()
        {
            var success = MissionProgressRuleAuthoring.TryBuild(
                new[]
                {
                    new MissionTriggerDefinition(new MissionTriggerEntry
                    {
                        MissionId = 321,
                        ContentRevision = "deployment_11",
                        ObjectiveId = 10,
                        TransitionId = 20,
                        TriggerId = 1,
                        Requirement = MissionContentRequirement.Required,
                        Kind = MissionTriggerKind.ProgressEvent,
                        Sequence = 1,
                        EventKind = (byte)MissionProgressEventKind.ScenarioEvent,
                        SubjectId = 1,
                        CounterId = 60,
                        Comment = "Scenario 60 step 1"
                    })
                },
                out var rule,
                out var counters,
                out var itemCounters,
                out var diagnostic);

            Assert.IsTrue(success);
            Assert.IsNotNull(rule);
            Assert.AreEqual((uint)321, rule.ScopeId);
            Assert.AreEqual((uint)60, rule.DetailId);
            Assert.AreEqual(0, counters.Count);
            Assert.AreEqual(0, itemCounters.Count);
            Assert.IsNull(diagnostic);
        }

        /// <summary>
        /// A scoped scene set counts to its target, from 1 to its event count: "any 16 of the 22 bodies" (Death Patrol,
        /// PoolsDeathPatrolV1). A target of 0 or above the event count can never be reached as authored.
        /// </summary>
        [TestMethod]
        public void ScopedSceneSetCountsAnyTargetOfItsEvents()
        {
            Assert.IsTrue(MissionProgressRuleAuthoring.TryBuild(SceneSet(events: 22, target: 16),
                out var rule, out var counters, out _, out var diagnostic), diagnostic);
            Assert.AreEqual(22, rule.Subjects.Count);
            Assert.AreEqual(16U, rule.TargetValue);
            Assert.AreEqual(16U, counters[0].TargetValue);

            Assert.IsTrue(MissionProgressRuleAuthoring.TryBuild(SceneSet(events: 22, target: 22),
                out rule, out _, out _, out _));
            Assert.AreEqual(22U, rule.TargetValue, "all of them");

            foreach (var target in new[] { 0U, 23U })
            {
                Assert.IsFalse(MissionProgressRuleAuthoring.TryBuild(SceneSet(events: 22, target: target),
                    out rule, out _, out _, out diagnostic), $"target {target}");
                Assert.IsNull(rule);
                Assert.AreEqual("scoped scene counters require distinct events 1..N in one mission/scenario, " +
                    "initial_value 0 and target_value from 1 to the authored event count.", diagnostic);
            }
        }

        private static MissionTriggerDefinition[] SceneSet(uint events, uint target)
        {
            var triggers = new MissionTriggerDefinition[events];
            for (var id = 1U; id <= events; id++)
                triggers[id - 1] = new(new MissionTriggerEntry
                {
                    MissionId = 1315,
                    ContentRevision = "pools_1_16_5",
                    ObjectiveId = 5,
                    TransitionId = 1,
                    TriggerId = id,
                    Requirement = MissionContentRequirement.Required,
                    Kind = MissionTriggerKind.ProgressEvent,
                    Sequence = id,
                    EventKind = (byte)MissionProgressEventKind.ScenarioEvent,
                    SubjectId = id,
                    CounterId = 1,
                    InitialValue = 0,
                    TargetValue = target,
                    Comment = $"Body {id}"
                });
            return triggers;
        }

        private static MissionTriggerDefinition CreateCounterTrigger(
            uint initialValue,
            uint targetValue,
            uint counterId = 1,
            uint subjectId = 501) =>
            new(new MissionTriggerEntry
            {
                MissionId = 321,
                ContentRevision = "deployment_11",
                ObjectiveId = 10,
                TransitionId = 20,
                TriggerId = 1,
                Requirement = MissionContentRequirement.Required,
                Kind = MissionTriggerKind.ProgressEvent,
                Sequence = 1,
                EventKind = (byte)MissionProgressEventKind.CreatureKilled,
                SubjectId = subjectId,
                CounterId = counterId,
                InitialValue = initialValue,
                TargetValue = targetValue,
                Comment = "Counter progress trigger"
            });

        private static MissionTriggerDefinition CreateItemCounterTrigger(
            uint initialValue,
            uint targetValue,
            MissionProgressEventKind kind = MissionProgressEventKind.ItemAcquired,
            uint itemClassId = 3147) =>
            new(new MissionTriggerEntry
            {
                MissionId = 321,
                ContentRevision = "deployment_11",
                ObjectiveId = 10,
                TransitionId = 20,
                TriggerId = 1,
                Requirement = MissionContentRequirement.Required,
                Kind = MissionTriggerKind.ProgressEvent,
                Sequence = 1,
                EventKind = (byte)kind,
                SubjectId = itemClassId,
                InitialValue = initialValue,
                TargetValue = targetValue,
                Comment = "Item counter progress trigger"
            });
    }
}
