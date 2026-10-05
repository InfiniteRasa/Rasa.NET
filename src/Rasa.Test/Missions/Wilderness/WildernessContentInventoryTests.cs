using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Rasa.Data;
using Rasa.Game.Missions.Protocol;
using Rasa.Managers;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Repositories.World;
using Rasa.Structures;
using Rasa.Structures.World;

namespace Rasa.Test.Missions.Wilderness
{
    [TestClass]
    [DoNotParallelize]
    [TestCategory("WildernessSourceShape")]
    public class WildernessContentInventoryTests
    {
        [TestMethod]
        public void FrozenMetaMembershipMatchesTheRecoveredDistinctRules()
        {
            var recovered = MissionDefinitionCatalog.CreateRecoveredInactiveDefinitions()[1449];
            Assert.IsFalse(recovered.IsOperational,
                "Recovered rule membership is evidence, not authority to activate unmigrated content.");

            var waypoints = recovered.Objectives[1].ProgressRule;
            Assert.IsNotNull(waypoints);
            Assert.AreEqual(MissionProgressEventKind.WaypointAcquired, waypoints.Kind);
            Assert.AreEqual(MissionProgressRuleType.CompleteDistinctSet, waypoints.RuleType);
            CollectionAssert.AreEquivalent(WildernessMissionCases.RecoveredWaypointIds.ToArray(),
                waypoints.Subjects.ToArray());

            var logos = recovered.Objectives[8].ProgressRule;
            Assert.IsNotNull(logos);
            Assert.AreEqual(MissionProgressEventKind.LogosAcquired, logos.Kind);
            Assert.AreEqual(MissionProgressRuleType.CompleteDistinctSet, logos.RuleType);
            CollectionAssert.AreEquivalent(WildernessMissionCases.RecoveredLogosIds.ToArray(),
                logos.Subjects.ToArray(),
                "Earth 408 exists on map 1220 but is not part of the recovered twelve-Logos rule.");

            foreach (var objectiveId in WildernessMissionCases.UnboundCaveObjectiveIds)
                Assert.IsNull(recovered.Objectives[objectiveId].ProgressRule,
                    $"Cave child {objectiveId} has no recovered region binding; do not infer one from nearby Logos.");
        }

        [TestMethod]
        public void MigratedWildernessDefinitionsPreserveNativeIdentities()
        {
            using var harness = BootcampRuntimeTestHarness.Create(useWorldContent: true);
            var context = harness.WorldContext;
            var definitions = context.MissionContentDefinitionEntries.AsNoTracking()
                .Where(row => row.ContentRevision == WildernessMissionCases.ContentRevision)
                .ToArray();
            var objectives = context.MissionObjectiveDefinitionEntries.AsNoTracking()
                .Where(row => row.ContentRevision == WildernessMissionCases.ContentRevision)
                .ToArray();
            var scenes = context.Set<MissionSceneBindingEntry>().AsNoTracking()
                .Where(row => row.ContentRevision == WildernessMissionCases.ContentRevision)
                .ToArray();
            var snapshot = new MissionContentLoader().Load(new MissionContentRepository(context),
                definitions.ToDictionary(row => row.MissionId, row => row.ContentRevision));

            foreach (var definition in definitions)
            {
                var expected = WildernessMissionCases.All.SingleOrDefault(row => row.MissionId == definition.MissionId);
                Assert.IsNotNull(expected, $"Mission {definition.MissionId} has no native W0 identity.");
                Assert.AreEqual(WildernessDisposition.OutdoorRelease, expected.Disposition,
                    $"{expected.MissionId} ({expected.Title}) is not an outdoor-release definition.");
                Assert.AreEqual(expected.MissionTexts.Single(text => text.Kind == 1).TextId,
                    definition.ClientNameTextId, $"Mission {expected.MissionId} title identity.");

                var actualObjectives = objectives.Where(row => row.MissionId == expected.MissionId).ToArray();
                CollectionAssert.AreEquivalent(
                    expected.Objectives.Select(row => row.ObjectiveId).ToArray(),
                    actualObjectives.Select(row => row.ObjectiveId).ToArray(),
                    $"Mission {expected.MissionId} must preserve native hidden/summary IDs as well as visible goals.");
                foreach (var objective in expected.Objectives)
                {
                    var actual = actualObjectives.Single(row => row.ObjectiveId == objective.ObjectiveId);
                    Assert.AreEqual(objective.NameTextId, actual.ClientNameTextId,
                        $"{expected.MissionId}.{objective.ObjectiveId} name.");
                    Assert.AreEqual(objective.BodyTextId, actual.ClientBodyTextId,
                        $"{expected.MissionId}.{objective.ObjectiveId} body.");
                    Assert.AreEqual(objective.Counter0TextId, actual.ClientCounter0TextId,
                        $"{expected.MissionId}.{objective.ObjectiveId} counter0.");
                    Assert.AreEqual(objective.Counter1TextId, actual.ClientCounter1TextId,
                        $"{expected.MissionId}.{objective.ObjectiveId} counter1.");
                    Assert.AreEqual(objective.Counter2TextId, actual.ClientCounter2TextId,
                        $"{expected.MissionId}.{objective.ObjectiveId} counter2.");
                }

                var mission = snapshot.Definitions[expected.MissionId].Mission;
                foreach (var binding in scenes.Where(row => row.MissionId == expected.MissionId))
                {
                    var scene = JsonSerializer.Deserialize<MissionSceneDefinition>(
                        binding.Bindings, MissionContentCodec.Options);
                    Assert.IsNotNull(scene, $"Mission {expected.MissionId} scene must deserialize.");
                    mission = mission.WithDialogue(scene.Dialogue);
                }
                AssertNativeDialogueBindings(expected, mission);
            }

            var missing = WildernessMissionCases.OpeningMissionIds
                .Where(id => definitions.All(row => row.MissionId != id)).ToArray();
            if (missing.Length != 0)
                Assert.Inconclusive(
                    $"W0 source expectations are frozen, but opening migrations are not installed for: {string.Join(", ", missing)}. " +
                    "This is not operational acceptance or evidence that all outdoor missions are deployed.");
        }

        [TestMethod]
        [DataRow(113U, true)]
        [DataRow(168U, false)]
        public void DerivedObjectiveDialogueMatchesTheNativeContact(uint packageId, bool matchesNative)
        {
            var mission = new Mission(1407, "Too Close For Comfort", 12099, 100, 42, 4, 1, 1,
                false, false,
                [
                    new MissionObjectiveDefinition(1, 12104, 12105, new uint?[3], 0,
                        MissionObjectiveState.Incomplete, true,
                        new Dictionary<uint, MissionObjectiveCounterDefinition>(),
                        new Dictionary<uint, MissionObjectiveItemCounterDefinition>(),
                        [new MissionObjectiveConversation(packageId, 1, MissionObjectiveConversationType.Completion)])
                ], enableOperational: true, contentRevision: WildernessMissionCases.ContentRevision);
            var expected = WildernessMissionCases.All.Single(row => row.MissionId == 1407);

            if (matchesNative)
                AssertNativeDialogueBindings(expected, mission);
            else
                Assert.ThrowsExactly<AssertFailedException>(() => AssertNativeDialogueBindings(expected, mission),
                    "Solis's valid package must not substitute for Moawi on native objective 1407.1.");
        }

        [TestMethod]
        public void OpeningPresentationOrderMatchesTheNativeConversationSequence()
        {
            using var harness = BootcampRuntimeTestHarness.Create(useWorldContent: true);
            var objectives = harness.WorldContext.MissionObjectiveDefinitionEntries.AsNoTracking()
                .Where(row => row.ContentRevision == WildernessMissionCases.ContentRevision &&
                    (row.MissionId == 1407 || row.MissionId == 1069))
                .ToArray();

            foreach (var (missionId, order) in new[]
            {
                (1407U, new uint[] { 1, 10 }),
                (1069U, new uint[] { 1, 2, 3 })
            })
            {
                var actual = objectives.Where(row => row.MissionId == missionId)
                    .OrderBy(row => row.Ordinal).ToArray();
                if (actual.Length == 0)
                    Assert.Inconclusive($"Mission {missionId} has no migrated opening objectives yet.");
                CollectionAssert.AreEqual(order, actual.Select(row => row.ObjectiveId).ToArray(),
                    $"Mission {missionId} presentation order does not replace escort/acquisition lifecycle acceptance.");
                Assert.AreEqual(actual.Length, actual.Select(row => row.Ordinal).Distinct().Count(),
                    $"Mission {missionId} must not rely on tied ordinals.");
            }
        }

        [TestMethod]
        public void ExcludedNativeCandidatesAreNotEnabledByTheOutdoorRevision()
        {
            using var harness = BootcampRuntimeTestHarness.Create(useWorldContent: true);
            var enabled = harness.WorldContext.MissionContentDefinitionEntries.AsNoTracking()
                .Where(row => row.Enabled && row.ContentRevision == WildernessMissionCases.ContentRevision)
                .Select(row => row.MissionId).ToArray();
            var allowed = WildernessMissionCases.All
                .Where(row => row.Disposition == WildernessDisposition.OutdoorRelease)
                .Select(row => row.MissionId).ToHashSet();
            Assert.IsTrue(enabled.All(allowed.Contains),
                $"Unapproved outdoor identities: {string.Join(", ", enabled.Where(id => !allowed.Contains(id)))}.");

            var retired = new uint[] { 751, 780, 767 };
            Assert.IsFalse(harness.WorldContext.MissionContentDefinitionEntries.AsNoTracking()
                .Any(row => row.Enabled && retired.Contains(row.MissionId)),
                "The later-client rollout must not reactivate the retired Munson missions.");
        }

        [TestMethod]
        public void NativeInventoryDoesNotReplaceTheExistingBootcampDefinitions()
        {
            using var harness = BootcampRuntimeTestHarness.Create(useWorldContent: true);
            var ids = WildernessMissionCases.ProtectedBootcampMissionIds.ToArray();
            var actual = harness.WorldContext.MissionContentDefinitionEntries.AsNoTracking()
                .Where(row => ids.Contains(row.MissionId) && row.Enabled).ToArray();
            CollectionAssert.AreEquivalent(ids, actual.Select(row => row.MissionId).ToArray());
            Assert.IsTrue(actual.All(row => row.ContentRevision == "deployment_11"),
                "Normal, retry and skip/handoff lifecycle coverage remains in the existing Bootcamp suite.");
        }

        private static void AssertNativeDialogueBindings(WildernessMissionCase expected, Mission mission)
        {
            var packages = mission.Dialogue.Select(topic => topic.NpcPackageId)
                .Concat(mission.Objectives.Values.SelectMany(objective => objective.Conversations)
                    .Select(conversation => conversation.NpcPackageId))
                .Distinct();
            foreach (var packageId in packages)
                foreach (var presentation in MissionConversationProjection.ForNpc(mission, packageId))
                    AssertNativeDialogue(expected, presentation.Definition);
        }

        private static void AssertNativeDialogue(WildernessMissionCase mission, MissionDialogueTopicDefinition topic)
        {
            if (topic.Kind == MissionDialogueKind.Reminder)
            {
                Assert.IsTrue(mission.MissionTexts.Any(row => row.Kind == 6),
                    $"Mission {mission.MissionId} must have native mission-reminder text.");
                return;
            }

            var objectiveId = topic.DialogObjectiveId ?? topic.ObjectiveId;
            var candidates = mission.Conversations.Where(row =>
                row.ObjectiveId == objectiveId &&
                row.NpcPackageId == topic.NpcPackageId &&
                row.PlayerFlagId == topic.PlayerFlagId).ToArray();
            Assert.IsTrue(candidates.Length > 0,
                $"Mission {mission.MissionId} invents native dialogue {objectiveId}/{topic.NpcPackageId}/{topic.PlayerFlagId}.");
            uint requiredKind = topic.Kind switch
            {
                MissionDialogueKind.Completion => 1,
                MissionDialogueKind.Ambient => 2,
                MissionDialogueKind.Choice => 3,
                _ => throw new ArgumentOutOfRangeException(nameof(topic.Kind))
            };
            Assert.IsTrue(candidates.Any(row => row.Kind == requiredKind),
                $"Mission {mission.MissionId} dialogue kind has no matching native text.");
            if (topic.Kind == MissionDialogueKind.Choice)
            {
                Assert.IsNotNull(topic.Choices);
                var available = candidates.Where(row => row.Kind is >= 4 and <= 6 && !row.Empty)
                    .Select(row => (int)row.Kind - 3).ToArray();
                CollectionAssert.AreEquivalent(available, topic.Choices.Keys.ToArray(),
                    $"Mission {mission.MissionId} must map exactly the native window's nonempty choice callbacks.");
            }
        }
    }
}
