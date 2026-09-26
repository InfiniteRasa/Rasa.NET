using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Structures;

    [TestClass]
    public class MissionDefinitionCatalogTests
    {
        [TestMethod]
        public void RecoveredCatalogContainsOnlySourceBackedInactiveDefinitions()
        {
            var catalog = MissionDefinitionCatalog.CreateRecoveredInactiveDefinitions();

            CollectionAssert.AreEquivalent(new uint[] { 1069, 1407, 1449 },
                new List<uint>(catalog.Keys));
            Assert.AreEqual("Receptive Reception", catalog[1069].Name);
            Assert.AreEqual(6038U, catalog[1069].ClientNameTextId);
            Assert.AreEqual(6042U, catalog[1069].Objectives[1].ClientNameTextId);
            Assert.AreEqual(13794U, catalog[1069].Objectives[2].ClientBodyTextId);
            Assert.IsFalse(catalog[1069].IsOperational);
            Assert.IsNull(catalog[1069].Objectives[1].Ordinal);
            Assert.IsNull(catalog[1069].Objectives[1].InitialState);
            Assert.IsNull(catalog[1069].Objectives[1].IsRequired);

            var solis = catalog[1069].Objectives[2].Conversations[0];
            Assert.AreEqual(168U, solis.NpcPackageId);
            Assert.AreEqual(1U, solis.PlayerFlagId);
            Assert.AreEqual(MissionObjectiveConversationType.Completion, solis.Type);
            Assert.AreEqual(112U, catalog[1069].Objectives[3].Conversations[0].NpcPackageId);
            AssertProgressRule(
                catalog[1069].Objectives[1].ProgressRule,
                MissionProgressEventKind.LogosAcquired,
                new uint[] { 10 });
            Assert.AreEqual(113U, catalog[1407].Objectives[1].Conversations[0].NpcPackageId);
            Assert.AreEqual(168U, catalog[1407].Objectives[10].Conversations[0].NpcPackageId);
            Assert.IsNull(catalog[1407].Objectives[1].ProgressRule);
            Assert.IsNull(catalog[1407].Objectives[10].ProgressRule);
            CollectionAssert.AreEquivalent(
                new uint[] { 1, 3, 4, 5, 6, 7, 8, 20, 21, 22, 23, 24, 25, 40, 41, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 58 },
                new List<uint>(catalog[1449].Objectives.Keys));
            Assert.AreEqual(12789U, catalog[1449].Objectives[1].ClientCounterTextIds[0]);
            Assert.AreEqual(13685U, catalog[1449].Objectives[55].ClientCounterTextIds[0]);
            AssertProgressRule(
                catalog[1449].Objectives[1].ProgressRule,
                MissionProgressEventKind.WaypointAcquired,
                new uint[] { 49, 50, 51, 57, 61, 73, 156 });
            AssertProgressRule(
                catalog[1449].Objectives[8].ProgressRule,
                MissionProgressEventKind.LogosAcquired,
                new uint[] { 1, 2, 6, 9, 10, 23, 24, 28, 38, 49, 53, 56 });
            var officerSubjects = new Dictionary<uint, uint>
            {
                [20] = 82,
                [21] = 83,
                [22] = 84,
                [23] = 79,
                [24] = 80,
                [25] = 75
            };
            foreach (var subject in officerSubjects)
            {
                AssertProgressRule(
                    catalog[1449].Objectives[subject.Key].ProgressRule,
                    MissionProgressEventKind.CreatureKilled,
                    new[] { subject.Value });
                if (subject.Key == 23)
                    Assert.IsFalse(catalog[1449].Objectives[subject.Key]
                        .ProgressRule.SourceSpawnResolved.Value);
                else
                    Assert.IsNull(catalog[1449].Objectives[subject.Key]
                        .ProgressRule.SourceSpawnResolved);
            }

            var permittedRules = new HashSet<(uint MissionId, uint ObjectiveId)>
            {
                (1069, 1),
                (1449, 1),
                (1449, 8),
                (1449, 20),
                (1449, 21),
                (1449, 22),
                (1449, 23),
                (1449, 24),
                (1449, 25)
            };
            foreach (var mission in catalog.Values)
            foreach (var objective in mission.Objectives.Values)
            {
                if (!permittedRules.Contains((mission.MissionId, objective.ObjectiveId)))
                    Assert.IsNull(objective.ProgressRule,
                        $"Mission {mission.MissionId} objective {objective.ObjectiveId} has an unsupported progress rule.");
            }
            Assert.IsFalse(catalog[1449].Objectives.Values
                .Any(objective => objective.Counters.Count > 0 || objective.ItemCounters.Count > 0));
        }

        [TestMethod]
        public void DefinitionCollectionsAreDefensiveAndOperationalRequiresCompleteMetadata()
        {
            var sourceObjectives = new[]
            {
                new MissionObjectiveDefinition(
                    1, 10, 11, Array.Empty<uint?>(), 0,
                    MissionObjectiveState.Incomplete, true,
                    new Dictionary<uint, MissionObjectiveCounterDefinition>(),
                    new Dictionary<uint, MissionObjectiveItemCounterDefinition>(),
                    Array.Empty<MissionObjectiveConversation>(),
                    Array.Empty<uint>(), Array.Empty<uint>(), Array.Empty<MissionIndicator>())
            };
            var mission = new Mission(
                1, "test", 9, 7, 8, 5, 1, 2, true, false, sourceObjectives, true);

            sourceObjectives[0] = null;

            Assert.IsTrue(mission.IsOperational);
            Assert.IsNotNull(mission.Objectives[1]);
            Assert.ThrowsExactly<NotSupportedException>(() =>
                ((IDictionary<uint, MissionObjectiveDefinition>)mission.Objectives).Add(2, null));
        }

        [TestMethod]
        public void InitialObjectiveLogFactoryUsesImmutableDefinitionValues()
        {
            using var context = MissionTestContext.WithObjectiveMission();
            var mission = context.Manager.LoadedMissions[321];

            var first = mission.CreateInitialObjectiveLogs();
            first[5].State = MissionObjectiveState.Completed;
            first[5].SetCounter(0, 9);
            first[5].SetItemCounter(200, 7);
            var second = mission.CreateInitialObjectiveLogs();

            Assert.AreEqual(MissionObjectiveState.Incomplete, second[5].State);
            Assert.AreEqual(2U, second[5].Counters[0]);
            Assert.AreEqual(1U, second[5].ItemCounters[200]);
            Assert.AreEqual(MissionObjectiveState.Inactive, second[9].State);
        }

        private static void AssertProgressRule(
            MissionProgressRule rule,
            MissionProgressEventKind kind,
            uint[] subjects)
        {
            Assert.IsNotNull(rule);
            Assert.AreEqual(kind, rule.Kind);
            CollectionAssert.AreEquivalent(subjects, new List<uint>(rule.Subjects));
        }
    }
}
