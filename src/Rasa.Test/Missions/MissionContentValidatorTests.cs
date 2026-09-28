using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Missions.Content;
    using Rasa.Missions.Definitions;
    using Rasa.Missions.Runtime;
    using Rasa.Missions.Scenes;
    using Rasa.Structures.Char;
    using Rasa.Structures.Missions;
    using Rasa.Structures.World;

    [TestClass]
    public class MissionContentValidatorTests
    {
        [TestMethod]
        [DataRow("missing-index")]
        [DataRow("index-zero")]
        [DataRow("index-four")]
        [DataRow("transition")]
        [DataRow("objective")]
        [DataRow("package")]
        [DataRow("flag")]
        [DataRow("native-id")]
        [DataRow("duplicate")]
        [DataRow("unknown-kind")]
        [DataRow("read-only-transition")]
        [DataRow("read-only-choices")]
        [DataRow("nonterminal")]
        [DataRow("unknown-action")]
        public void DialogueValidationRejectsAmbiguousUnmappedAndNonExecutableTopics(string invalid)
        {
            var fixture = MissionContentFixture.CreateValid();
            if (invalid == "unknown-action")
                fixture.Actions[0].Kind = (MissionActionKind)255;
            if (invalid == "nonterminal")
                fixture.Transitions[0].ToState = (byte)MissionObjectiveState.Incomplete;
            var choices = new Dictionary<int, uint> { [1] = 20, [2] = 20, [3] = 20 };
            if (invalid == "missing-index") choices.Remove(2);
            if (invalid == "index-zero") { choices.Remove(1); choices[0] = 20; }
            if (invalid == "index-four") { choices.Remove(3); choices[4] = 20; }
            if (invalid == "transition") choices[2] = 999;
            var kind = invalid == "unknown-kind" ? (MissionDialogueKind)99 :
                invalid.StartsWith("read-only") ? MissionDialogueKind.Reminder : MissionDialogueKind.Choice;
            var topic = new MissionDialogueTopicDefinition(invalid == "objective" ? 999U : 10U,
                invalid == "package" ? 999U : 77U, invalid == "flag" ? 12U : 11U, kind,
                transitionId: invalid == "read-only-transition" ? 20U : null,
                choices: invalid == "read-only-transition" ? null : choices,
                dialogObjectiveId: invalid == "native-id" ? uint.MaxValue : null);
            var original = new MissionContentLoader().Load(fixture.CreateRepository()).Definitions[321].Mission;

            var mission = original.WithDialogue(invalid == "duplicate" ? new[] { topic, topic } : new[] { topic });

            Assert.IsFalse(mission.IsOperational);
            StringAssert.Contains(mission.OperationalDiagnostic, "dialogue");
        }

        [TestMethod]
        public void TypedDialogueMetadataRoundTripsWithoutChangingHistoricalSceneShapes()
        {
            var topic = new MissionDialogueTopicDefinition(8, 586, 1, MissionDialogueKind.Choice,
                choices: new Dictionary<int, uint> { [1] = 101, [2] = 102, [3] = 103 });
            var json = JsonSerializer.Serialize(topic, MissionContentCodec.Options);
            var decoded = JsonSerializer.Deserialize<MissionDialogueTopicDefinition>(json, MissionContentCodec.Options);
            Assert.AreEqual(MissionDialogueKind.Choice, decoded.Kind);
            Assert.AreEqual(102U, decoded.Choices[2]);
            Assert.AreEqual(
                "{\"missionId\":1995,\"objectiveId\":3,\"npcPackageId\":2584,\"dialogObjectiveId\":2,\"playerFlagId\":1}",
                JsonSerializer.Serialize(new SceneObjectConversation(1995, 3, 2584, 2), MissionContentCodec.Options));
            Assert.IsFalse(JsonSerializer.Serialize(new MissionSceneDefinition(), MissionContentCodec.Options)
                .Contains("\"dialogue\""));
            Assert.ThrowsExactly<JsonException>(() => JsonSerializer.Deserialize<MissionDialogueTopicDefinition>(
                "{\"objectiveId\":8,\"npcPackageId\":586,\"playerFlagId\":1,\"choiceLabels\":[\"invented\"]}",
                MissionContentCodec.Options));
        }

        [TestMethod]
        public void DialogueAliasCannotCollideWithAnUnchangedCompletionTopic()
        {
            var fixture = MissionContentFixture.CreateValid();
            fixture.Transitions.Add(new MissionObjectiveTransitionEntry
            {
                MissionId = 321, ContentRevision = "deployment_11", ObjectiveId = 11, TransitionId = 21,
                FromState = (byte)MissionObjectiveState.Incomplete, ToState = (byte)MissionObjectiveState.Completed
            });
            fixture.Triggers.Add(new MissionTriggerEntry
            {
                MissionId = 321, ContentRevision = "deployment_11", ObjectiveId = 11, TransitionId = 21,
                TriggerId = 1, Kind = MissionTriggerKind.Conversation, NpcPackageId = 77, PlayerFlagId = 11
            });
            var original = new MissionContentLoader().Load(fixture.CreateRepository()).Definitions[321].Mission;

            var mission = original.WithDialogue(new[]
            {
                new MissionDialogueTopicDefinition(10, 77, 11, transitionId: 20, dialogObjectiveId: 11)
            });

            Assert.IsFalse(mission.IsOperational);
            StringAssert.Contains(mission.OperationalDiagnostic, "duplicate native topic");
        }

        [TestMethod]
        [DoNotParallelize]
        public void MigratedChoiceObjectMustHaveAnAuthoredBranchMap()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var scene = MissionDialogueTestContent.Install(harness.WorldContext, conversationObject: true);
            scene.Dialogue = null;
            MissionDialogueTestContent.SaveScene(harness.WorldContext, scene);

            Assert.ThrowsExactly<MissionRuleException>(() => harness.Manager.LoadMissions());
        }

        [TestMethod]
        [DoNotParallelize]
        public void MigratedReadOnlyDialogueMustReferenceAnExistingNpcPackage()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var scene = MissionDialogueTestContent.Install(harness.WorldContext, kind: MissionDialogueKind.Reminder);
            scene.Dialogue[0] = new(8, 9999999, 1, MissionDialogueKind.Reminder);
            MissionDialogueTestContent.SaveScene(harness.WorldContext, scene);

            Assert.ThrowsExactly<MissionRuleException>(() => harness.Manager.LoadMissions());
        }

        [TestMethod]
        [DoNotParallelize]
        public void MigratedChoiceMustNotQueueAnUnboundSceneSequence()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var scene = MissionDialogueTestContent.Install(harness.WorldContext);
            scene.Sequences.Remove(202);
            MissionDialogueTestContent.SaveScene(harness.WorldContext, scene);

            Assert.ThrowsExactly<MissionRuleException>(() => harness.Manager.LoadMissions());
        }

        [TestMethod]
        public void ValidatorAcceptsACompleteOperationalGraph()
        {
            var fixture = MissionContentFixture.CreateValid();
            var snapshot = new MissionContentLoader().Load(fixture.CreateRepository());
            var report = new MissionContentValidator().Validate(
                snapshot,
                fixture.CreateWorldUnitOfWork());

            Assert.AreEqual(0, report.Diagnostics.Count);
            Assert.IsFalse(report.BlocksReadiness);
        }

        [TestMethod]
        public void ValidatorAcceptsSpawnGroupIndicatorAndPlayerFlagActionsReferencingRealContent()
        {
            var fixture = MissionContentFixture.CreateValid();
            fixture.Actions.AddRange(
                new MissionActionEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ObjectiveId = 10,
                    TransitionId = 20,
                    ActionId = 5,
                    Requirement = MissionContentRequirement.Required,
                    Kind = MissionActionKind.ActivateSpawnGroup,
                    Sequence = 5,
                    SpawnGroupId = 50,
                    Comment = "Activate the authored spawn group"
                },
                new MissionActionEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ObjectiveId = 10,
                    TransitionId = 20,
                    ActionId = 6,
                    Requirement = MissionContentRequirement.Required,
                    Kind = MissionActionKind.ShowIndicator,
                    Sequence = 6,
                    IndicatorId = 70,
                    Comment = "Show the authored indicator"
                },
                new MissionActionEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ObjectiveId = 10,
                    TransitionId = 20,
                    ActionId = 7,
                    Requirement = MissionContentRequirement.Required,
                    Kind = MissionActionKind.SetPlayerFlag,
                    Sequence = 7,
                    PlayerFlagId = 7,
                    PlayerFlagValue = 2,
                    Comment = "Set a player flag"
                });

            var snapshot = new MissionContentLoader().Load(fixture.CreateRepository());
            var report = new MissionContentValidator().Validate(
                snapshot,
                fixture.CreateWorldUnitOfWork());

            Assert.AreEqual(0, report.Diagnostics.Count);
            Assert.IsFalse(report.BlocksReadiness);
        }

        [TestMethod]
        public void ValidatorAcceptsApprovedScenarioStepVocabularyIncludingAlternativeTargets()
        {
            var fixture = MissionContentFixture.CreateValid();
            ConfigureApprovedScenarioVocabulary(fixture);
            var snapshot = new MissionContentLoader().Load(fixture.CreateRepository());
            var report = new MissionContentValidator().Validate(
                snapshot,
                fixture.CreateWorldUnitOfWork());

            Assert.AreEqual(0, report.Diagnostics.Count);
            Assert.IsFalse(report.BlocksReadiness);
        }

        [TestMethod]
        public void ValidatorRejectsSelectableRewardPackagesForScenarioGrants()
        {
            var fixture = MissionContentFixture.CreateValid();
            ConfigureApprovedScenarioVocabulary(fixture);
            fixture.ScenarioSteps.Single(step => step.StepId == 13).RewardId = 40;
            var snapshot = new MissionContentLoader().Load(fixture.CreateRepository());
            var report = new MissionContentValidator().Validate(
                snapshot,
                fixture.CreateWorldUnitOfWork());

            CollectionAssert.Contains(
                report.Diagnostics.Select(diagnostic => diagnostic.Code).ToArray(),
                "invalid-scenario-reward-selection");
            Assert.IsTrue(report.BlocksReadiness);
        }

        [TestMethod]
        [DynamicData(nameof(GetFailureCases))]
        public void ValidatorReportsEachDiagnosticClass(
            string _,
            Action<MissionContentFixture> mutate,
            string expectedCode)
        {
            var fixture = MissionContentFixture.CreateValid();
            mutate(fixture);
            var snapshot = new MissionContentLoader().Load(fixture.CreateRepository());
            var report = new MissionContentValidator().Validate(
                snapshot,
                fixture.CreateWorldUnitOfWork());

            CollectionAssert.Contains(
                report.Diagnostics.Select(diagnostic => diagnostic.Code).ToArray(),
                expectedCode);
            Assert.IsTrue(report.BlocksReadiness);
        }

        [TestMethod]
        public void ValidatorSortsDiagnosticsDeterministicallyForOperators()
        {
            var fixture = MissionContentFixture.CreateValid();
            fixture.Objectives[0].ClientBodyTextId = 0;
            fixture.NpcPackageIds.Clear();
            fixture.Actions[0].Kind = (MissionActionKind)99;
            fixture.Indicators[0].ObjectiveId = 10;
            fixture.Indicators[0].Radius = 0;
            fixture.ScenarioSteps[0].Kind = (MissionScenarioStepKind)99;

            var snapshot = new MissionContentLoader().Load(fixture.CreateRepository());
            var report = new MissionContentValidator().Validate(
                snapshot,
                fixture.CreateWorldUnitOfWork());

            CollectionAssert.AreEqual(
                new[]
                {
                    "missing-client-text",
                    "invalid-radius",
                    "unsupported-action",
                    "missing-npc-package",
                    "unsupported-scenario-step"
                },
                report.Diagnostics.Select(diagnostic => diagnostic.Code).ToArray());
        }

        [TestMethod]
        public void ValidatorPropagatesRequiredChainInvalidationTransitivelyInMissionOrder()
        {
            var fixture = MissionContentFixture.CreateValid();
            fixture.Objectives[0].ClientBodyTextId = 0;
            AddSingleObjectiveConversationMission(fixture, 322, prerequisiteMissionId: 321);
            AddSingleObjectiveConversationMission(fixture, 323, prerequisiteMissionId: 322);

            var snapshot = new MissionContentLoader().Load(fixture.CreateRepository());
            var report = new MissionContentValidator().Validate(
                snapshot,
                fixture.CreateWorldUnitOfWork());

            CollectionAssert.AreEqual(
                new[]
                {
                    "321:missing-client-text",
                    "322:required-chain-inactive",
                    "323:required-chain-inactive"
                },
                report.Diagnostics
                    .Select(diagnostic => $"{diagnostic.MissionId}:{diagnostic.Code}")
                    .ToArray());
            StringAssert.Contains(report.Diagnostics[1].Message, "321");
            StringAssert.Contains(report.Diagnostics[2].Message, "322");
        }

        [TestMethod]
        public void ValidatorAcceptsObjectivesWithMultipleExecutableProgressPaths()
        {
            var fixture = CreatePureProgressFixture();
            AddProgressTransition(
                fixture,
                objectiveId: 10,
                transitionId: 21,
                triggerId: 2,
                subjectId: 502);

            var snapshot = new MissionContentLoader().Load(fixture.CreateRepository());
            var report = new MissionContentValidator().Validate(
                snapshot,
                fixture.CreateWorldUnitOfWork());

            Assert.IsFalse(report.BlocksReadiness);
            CollectionAssert.DoesNotContain(
                report.Diagnostics.Select(diagnostic => diagnostic.Code).ToArray(),
                "multiple-executable-transition-paths");
        }

        [TestMethod]
        public void ValidatorAcceptsConversationAndProgressBranchCombination()
        {
            var fixture = MissionContentFixture.CreateValid();
            AddProgressTransition(
                fixture,
                objectiveId: 10,
                transitionId: 21,
                triggerId: 2,
                subjectId: 501);

            var snapshot = new MissionContentLoader().Load(fixture.CreateRepository());
            var report = new MissionContentValidator().Validate(
                snapshot,
                fixture.CreateWorldUnitOfWork());

            Assert.IsFalse(report.BlocksReadiness);
            CollectionAssert.DoesNotContain(
                report.Diagnostics.Select(diagnostic => diagnostic.Code).ToArray(),
                "multiple-executable-transition-paths");
        }

        [TestMethod]
        public void ValidatorRejectsNonMonotonicCounterRangesWithDeterministicActionableDiagnostics()
        {
            var fixture = CreatePureProgressFixture();
            fixture.Triggers[0].CounterId = 1;
            fixture.Triggers[0].InitialValue = 5;
            fixture.Triggers[0].TargetValue = 5;
            fixture.Transitions.Add(new MissionObjectiveTransitionEntry
            {
                MissionId = 321,
                ContentRevision = "deployment_11",
                ObjectiveId = 10,
                TransitionId = 21,
                Requirement = MissionContentRequirement.Required,
                Sequence = 2,
                FromState = (byte)MissionObjectiveState.Incomplete,
                ToState = (byte)MissionObjectiveState.Completed,
                Comment = "Progress transition 21"
            });
            fixture.Triggers.Add(new MissionTriggerEntry
            {
                MissionId = 321,
                ContentRevision = "deployment_11",
                ObjectiveId = 10,
                TransitionId = 21,
                TriggerId = 2,
                Requirement = MissionContentRequirement.Required,
                Kind = MissionTriggerKind.ProgressEvent,
                Sequence = 1,
                EventKind = (byte)MissionProgressEventKind.ItemAcquired,
                SubjectId = 3147,
                InitialValue = 7,
                TargetValue = 6,
                Comment = "Invalid item counter range"
            });

            var snapshot = new MissionContentLoader().Load(fixture.CreateRepository());
            var report = new MissionContentValidator().Validate(
                snapshot,
                fixture.CreateWorldUnitOfWork());
            var diagnostics = report.Diagnostics
                .Where(entry => entry.Code == "invalid-progress-event")
                .ToArray();

            Assert.IsTrue(report.BlocksReadiness);
            Assert.AreEqual(2, diagnostics.Length);
            CollectionAssert.AreEqual(
                new[]
                {
                    "counter progress rule counter_id 1 has initial_value 5 and target_value 5; target_value must be greater than initial_value so runtime monotonic progress can advance.",
                    "item counter progress rule subject_id 3147 has initial_value 7 and target_value 6; target_value must be greater than initial_value so runtime monotonic progress can advance."
                },
                diagnostics.Select(diagnostic => diagnostic.Message).ToArray());
        }

        public static IEnumerable<object[]> GetFailureCases()
        {
            yield return Case("duplicate mission", fixture =>
            {
                fixture.Definitions.Add(new MissionContentDefinitionEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_12",
                    Requirement = MissionContentRequirement.Required,
                    ClientNameTextId = 3001,
                    GiverId = 101,
                    ReceiverId = 102,
                    Level = 9,
                    GroupType = 2,
                    CategoryId = 3,
                    Shareable = false,
                    RadioCompleteable = false,
                    Comment = "Competing authored revision"
                });
            }, "duplicate-mission-id");

            yield return Case("duplicate objective", fixture =>
            {
                fixture.Objectives.Add(new MissionObjectiveDefinitionEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ObjectiveId = 10,
                    Requirement = MissionContentRequirement.Required,
                    ClientNameTextId = 9999,
                    ClientBodyTextId = 9998,
                    Ordinal = 9,
                    InitialState = (byte)MissionObjectiveState.Inactive,
                    IsRequired = true,
                    Comment = "Duplicate objective"
                });
            }, "duplicate-objective-id");

            yield return Case("missing objective", fixture => fixture.Objectives.Clear(), "missing-objective-id");

            yield return Case("transition cycle", fixture =>
            {
                fixture.Objectives.Add(new MissionObjectiveDefinitionEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ObjectiveId = 12,
                    Requirement = MissionContentRequirement.Required,
                    ClientNameTextId = 2301,
                    ClientBodyTextId = 2302,
                    Ordinal = 3,
                    InitialState = (byte)MissionObjectiveState.Inactive,
                    IsRequired = true,
                    Comment = "Cycle objective"
                });
                fixture.Transitions.Add(new MissionObjectiveTransitionEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ObjectiveId = 11,
                    TransitionId = 21,
                    Requirement = MissionContentRequirement.Required,
                    Sequence = 2,
                    FromState = (byte)MissionObjectiveState.Incomplete,
                    ToState = (byte)MissionObjectiveState.Completed,
                    Comment = "Cycle transition"
                });
                fixture.Triggers.Add(new MissionTriggerEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ObjectiveId = 11,
                    TransitionId = 21,
                    TriggerId = 21,
                    Requirement = MissionContentRequirement.Required,
                    Kind = MissionTriggerKind.Conversation,
                    Sequence = 1,
                    NpcPackageId = 77,
                    PlayerFlagId = 12,
                    Comment = "Conversation for objective 11"
                });
                fixture.Actions.AddRange(
                    new MissionActionEntry
                    {
                        MissionId = 321,
                        ContentRevision = "deployment_11",
                        ObjectiveId = 11,
                        TransitionId = 21,
                        ActionId = 21,
                        Requirement = MissionContentRequirement.Required,
                        Kind = MissionActionKind.RevealObjective,
                        Sequence = 1,
                        TargetObjectiveId = 12,
                        Comment = "Reveal 12"
                    },
                    new MissionActionEntry
                    {
                        MissionId = 321,
                        ContentRevision = "deployment_11",
                        ObjectiveId = 12,
                        TransitionId = 22,
                        ActionId = 22,
                        Requirement = MissionContentRequirement.Required,
                        Kind = MissionActionKind.RevealObjective,
                        Sequence = 1,
                        TargetObjectiveId = 10,
                        Comment = "Reveal 10"
                    });
                fixture.Transitions.Add(new MissionObjectiveTransitionEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ObjectiveId = 12,
                    TransitionId = 22,
                    Requirement = MissionContentRequirement.Required,
                    Sequence = 3,
                    FromState = (byte)MissionObjectiveState.Incomplete,
                    ToState = (byte)MissionObjectiveState.Completed,
                    Comment = "Cycle back"
                });
                fixture.Triggers.Add(new MissionTriggerEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ObjectiveId = 12,
                    TransitionId = 22,
                    TriggerId = 22,
                    Requirement = MissionContentRequirement.Required,
                    Kind = MissionTriggerKind.Conversation,
                    Sequence = 1,
                    NpcPackageId = 77,
                    PlayerFlagId = 13,
                    Comment = "Conversation for objective 12"
                });
            }, "transition-cycle");

            yield return Case("missing target", fixture =>
            {
                fixture.Actions[1].TargetObjectiveId = 999;
            }, "missing-target");

            yield return Case("invalid graph", fixture =>
            {
                fixture.Actions.RemoveAll(action => action.Kind == MissionActionKind.RevealObjective);
            }, "invalid-objective-graph");

            yield return Case("unsupported trigger", fixture =>
            {
                fixture.Triggers[0].Kind = (MissionTriggerKind)99;
            }, "unsupported-trigger");

            yield return Case("unsupported action", fixture =>
            {
                fixture.Actions[0].Kind = (MissionActionKind)99;
            }, "unsupported-action");

            yield return Case("spawn action targets missing spawn group", fixture =>
            {
                fixture.Actions[0].Kind = MissionActionKind.ActivateSpawnGroup;
                fixture.Actions[0].TargetObjectiveId = null;
                fixture.Actions[0].ObjectiveState = null;
                fixture.Actions[0].SpawnGroupId = 999;
            }, "missing-spawn-group");

            yield return Case("indicator action targets missing indicator", fixture =>
            {
                fixture.Actions[0].Kind = MissionActionKind.ShowIndicator;
                fixture.Actions[0].TargetObjectiveId = null;
                fixture.Actions[0].ObjectiveState = null;
                fixture.Actions[0].IndicatorId = 999;
            }, "missing-indicator");

            yield return Case("player flag action missing value", fixture =>
            {
                fixture.Actions[0].Kind = MissionActionKind.SetPlayerFlag;
                fixture.Actions[0].TargetObjectiveId = null;
                fixture.Actions[0].ObjectiveState = null;
                fixture.Actions[0].PlayerFlagId = 7;
                fixture.Actions[0].PlayerFlagValue = null;
            }, "missing-player-flag-binding");

            yield return Case("missing npc package", fixture =>
            {
                fixture.NpcPackageIds.Clear();
            }, "missing-npc-package");

            yield return Case("missing item template", fixture =>
            {
                fixture.ItemTemplateClasses.Clear();
            }, "missing-item-template");

            yield return Case("missing entity class", fixture =>
            {
                fixture.EntityClassIds.Clear();
            }, "missing-entity-class");

            yield return Case("missing equipped item template", fixture =>
            {
                ConfigurePureProgressObjective(fixture);
                fixture.Triggers[0].EventKind = (byte)MissionProgressEventKind.ItemEquipped;
                fixture.Triggers[0].SubjectId = 28;
                fixture.Triggers[0].CounterId = null;
                fixture.Triggers[0].InitialValue = null;
                fixture.Triggers[0].TargetValue = null;
                fixture.Triggers[0].SourceSpawnResolved = true;
                fixture.ItemTemplateClasses.Clear();
            }, "missing-item-template");

            yield return Case("missing ability target creature", fixture =>
            {
                ConfigurePureProgressObjective(fixture);
                fixture.Triggers[0].EventKind = (byte)MissionProgressEventKind.AbilityHit;
                fixture.Triggers[0].SubjectId = 194;
                fixture.Triggers[0].CounterId = 999;
                fixture.Triggers[0].InitialValue = null;
                fixture.Triggers[0].TargetValue = null;
                fixture.Triggers[0].SourceSpawnResolved = null;
            }, "missing-creature");

            yield return Case("missing scenario step", fixture =>
            {
                ConfigurePureProgressObjective(fixture);
                fixture.Triggers[0].EventKind = (byte)MissionProgressEventKind.ScenarioEvent;
                fixture.Triggers[0].SubjectId = 999;
                fixture.Triggers[0].CounterId = 60;
                fixture.Triggers[0].InitialValue = null;
                fixture.Triggers[0].TargetValue = null;
                fixture.Triggers[0].SourceSpawnResolved = null;
            }, "missing-scenario-step");

            yield return Case("missing map context", fixture =>
            {
                fixture.MapContextIds.Clear();
            }, "missing-map-context");

            yield return Case("missing area", fixture =>
            {
                fixture.Areas.Clear();
                fixture.SpawnGroups[0].AreaId = 30;
            }, "missing-area");

            yield return Case("missing spawn group", fixture =>
            {
                fixture.SpawnGroups.Clear();
                fixture.Actions.Add(new MissionActionEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ObjectiveId = 10,
                    TransitionId = 20,
                    ActionId = 99,
                    Requirement = MissionContentRequirement.Required,
                    Kind = MissionActionKind.ActivateSpawnGroup,
                    Sequence = 99,
                    SpawnGroupId = 50,
                    Comment = "Missing spawn group"
                });
            }, "missing-spawn-group");

            yield return Case("unsupported scenario step", fixture =>
            {
                fixture.ScenarioSteps[0].Kind = (MissionScenarioStepKind)99;
            }, "unsupported-scenario-step");

            yield return Case("invalid scenario step shape", fixture =>
            {
                fixture.ScenarioSteps[0].Kind = MissionScenarioStepKind.StartDeadline;
                fixture.ScenarioSteps[0].ScenarioEventId = null;
                fixture.ScenarioSteps[0].DelayMilliseconds = null;
            }, "invalid-scenario-step-shape");

            yield return Case("missing scenario step objective", fixture =>
            {
                fixture.ScenarioSteps[0].Kind = MissionScenarioStepKind.RevealObjective;
                fixture.ScenarioSteps[0].ScenarioEventId = null;
                fixture.ScenarioSteps[0].TargetObjectiveId = 999;
            }, "missing-target");

            yield return Case("missing scenario step spawn", fixture =>
            {
                fixture.ScenarioSteps[0].Kind = MissionScenarioStepKind.DisableInteraction;
                fixture.ScenarioSteps[0].ScenarioEventId = null;
                fixture.ScenarioSteps[0].SpawnGroupId = 50;
                fixture.ScenarioSteps[0].SpawnId = 999;
            }, "missing-spawn");

            yield return Case("escort scenario step missing spawn group", fixture =>
            {
                fixture.ScenarioSteps[0].Kind = MissionScenarioStepKind.EscortSpawnGroup;
                fixture.ScenarioSteps[0].ScenarioEventId = null;
                fixture.ScenarioSteps[0].SpawnGroupId = null;
            }, "invalid-scenario-step-shape");

            yield return Case("escort scenario step requires scenario controlled spawn group", fixture =>
            {
                fixture.ScenarioSteps[0].Kind = MissionScenarioStepKind.EscortSpawnGroup;
                fixture.ScenarioSteps[0].ScenarioEventId = null;
                fixture.ScenarioSteps[0].SpawnGroupId = 50;
                fixture.SpawnGroups[0].SpawnPolicy = MissionSpawnGroupPolicy.OrdinaryRespawn;
                fixture.SpawnGroups[0].RespawnSeconds = 30;
            }, "invalid-spawn-policy");

            yield return Case("invalid scenario step tutorial", fixture =>
            {
                fixture.ScenarioSteps[0].Kind = MissionScenarioStepKind.PlayTutorial;
                fixture.ScenarioSteps[0].ScenarioEventId = null;
                fixture.ScenarioSteps[0].TutorialId = uint.MaxValue;
            }, "invalid-tutorial");

            yield return Case("invalid scenario step qualification", fixture =>
            {
                fixture.ScenarioSteps[0].Kind = MissionScenarioStepKind.SetQualification;
                fixture.ScenarioSteps[0].ScenarioEventId = null;
                fixture.ScenarioSteps[0].QualificationKey = CharacterQualificationKey.BootcampComplete;
                fixture.ScenarioSteps[0].QualificationValue = 2;
            }, "invalid-qualification");

            yield return Case("spawn dynamic object missing authored key", fixture =>
            {
                fixture.ScenarioSteps[0].Kind = MissionScenarioStepKind.SpawnDynamicObject;
                fixture.ScenarioSteps[0].ScenarioEventId = null;
                fixture.ScenarioSteps[0].DynamicObjectKey = null;
                fixture.ScenarioSteps[0].EntityClassId = 3147;
                fixture.ScenarioSteps[0].PosX = 8;
                fixture.ScenarioSteps[0].PosY = 9;
                fixture.ScenarioSteps[0].PosZ = 10;
                fixture.ScenarioSteps[0].Orientation = 0.5;
                fixture.ScenarioSteps[0].InitialInteractionEnabled = false;
            }, "invalid-scenario-step-shape");

            yield return Case("despawn dynamic object missing authored key", fixture =>
            {
                fixture.ScenarioSteps[0].Kind = MissionScenarioStepKind.DespawnDynamicObject;
                fixture.ScenarioSteps[0].ScenarioEventId = null;
                fixture.ScenarioSteps[0].DynamicObjectKey = string.Empty;
            }, "invalid-scenario-step-shape");

            yield return Case("missing scenario", fixture =>
            {
                fixture.Scenarios.Clear();
                fixture.Actions.Add(new MissionActionEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ObjectiveId = 10,
                    TransitionId = 20,
                    ActionId = 98,
                    Requirement = MissionContentRequirement.Required,
                    Kind = MissionActionKind.StartScenario,
                    Sequence = 98,
                    ScenarioId = 60,
                    Comment = "Missing scenario"
                });
            }, "missing-scenario");

            yield return Case("missing reward", fixture =>
            {
                fixture.Rewards.Clear();
                fixture.RewardItems.Clear();
            }, "missing-reward");

            yield return Case("missing reward reference", fixture =>
            {
                fixture.Actions.RemoveAll(action => action.Kind == MissionActionKind.GrantReward);
            }, "missing-reward-reference");

            yield return Case("ambiguous reward reference", fixture =>
            {
                fixture.Rewards.Add(new MissionRewardDefinitionEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    RewardId = 41,
                    Requirement = MissionContentRequirement.Required,
                    Experience = 10,
                    Credits = 1,
                    Prestige = 0,
                    SelectionCount = 0,
                    Comment = "Alternate reward"
                });
                fixture.RewardItems.Add(new MissionRewardItemEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    RewardId = 41,
                    ItemId = 1,
                    Kind = MissionRewardItemKind.Fixed,
                    ItemTemplateId = 28,
                    Quantity = 1
                });
                fixture.Actions.Add(new MissionActionEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ObjectiveId = 10,
                    TransitionId = 20,
                    ActionId = 97,
                    Requirement = MissionContentRequirement.Required,
                    Kind = MissionActionKind.GrantReward,
                    Sequence = 97,
                    RewardId = 41,
                    Comment = "Grant alternate reward"
                });
            }, "ambiguous-reward-reference");

            yield return Case("missing client text", fixture =>
            {
                fixture.Objectives[0].ClientBodyTextId = 0;
            }, "missing-client-text");

            yield return Case("missing counter text binding", fixture =>
            {
                ConfigurePureProgressObjective(fixture);
                fixture.Triggers[0].EventKind = (byte)MissionProgressEventKind.CreatureKilled;
                fixture.Triggers[0].SubjectId = 501;
                fixture.Triggers[0].CounterId = 0;
                fixture.Triggers[0].InitialValue = 0;
                fixture.Triggers[0].TargetValue = 3;
                fixture.Triggers[0].SourceSpawnResolved = null;
            }, "invalid-counter-text-binding");

            yield return Case("out of range counter text binding", fixture =>
            {
                ConfigurePureProgressObjective(fixture);
                fixture.Objectives[0].ClientCounter0TextId = 9100;
                fixture.Objectives[0].ClientCounter1TextId = 9101;
                fixture.Objectives[0].ClientCounter2TextId = 9102;
                fixture.Triggers[0].EventKind = (byte)MissionProgressEventKind.CreatureKilled;
                fixture.Triggers[0].SubjectId = 501;
                fixture.Triggers[0].CounterId = 3;
                fixture.Triggers[0].InitialValue = 0;
                fixture.Triggers[0].TargetValue = 3;
                fixture.Triggers[0].SourceSpawnResolved = null;
            }, "invalid-counter-text-binding");

            yield return Case("invalid radius", fixture =>
            {
                fixture.Indicators[0].Radius = 0;
            }, "invalid-radius");

            yield return Case("invalid quantity", fixture =>
            {
                fixture.RewardItems[0].Quantity = 0;
            }, "invalid-quantity");

            yield return Case("invalid delay", fixture =>
            {
                fixture.Triggers[0].Kind = MissionTriggerKind.TimerElapsed;
                fixture.Triggers[0].NpcPackageId = null;
                fixture.Triggers[0].PlayerFlagId = null;
                fixture.Triggers[0].DurationSeconds = 0;
            }, "invalid-delay");

            yield return Case("invalid reward selection", fixture =>
            {
                fixture.Rewards[0].SelectionCount = 1;
                fixture.RewardItems.RemoveAll(item => item.Kind == MissionRewardItemKind.Selectable);
            }, "invalid-reward-selection");

            yield return Case("invalid progress event", fixture =>
            {
                fixture.Triggers[0].Kind = MissionTriggerKind.ProgressEvent;
                fixture.Triggers[0].NpcPackageId = null;
                fixture.Triggers[0].PlayerFlagId = null;
                fixture.Triggers[0].EventKind = (byte)MissionProgressEventKind.CreatureKilled;
                fixture.Triggers[0].SubjectId = 501;
                fixture.Triggers[0].CounterId = null;
                fixture.Triggers[0].InitialValue = null;
                fixture.Triggers[0].TargetValue = null;
                fixture.Triggers[0].SourceSpawnResolved = null;
                fixture.Triggers.Add(new MissionTriggerEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ObjectiveId = 10,
                    TransitionId = 20,
                    TriggerId = 2,
                    Requirement = MissionContentRequirement.Required,
                    Kind = MissionTriggerKind.ProgressEvent,
                    Sequence = 2,
                    EventKind = (byte)MissionProgressEventKind.CreatureKilled,
                    SubjectId = 502,
                    Comment = "Invalid mixed multi-trigger exact rule"
                });
            }, "invalid-progress-event");

            yield return Case("cross revision reference", fixture =>
            {
                fixture.Definitions.Add(new MissionContentDefinitionEntry
                {
                    MissionId = 322,
                    ContentRevision = "legacy",
                    Requirement = MissionContentRequirement.Optional,
                    ClientNameTextId = 0,
                    GiverId = 1,
                    ReceiverId = 2,
                    Level = 1,
                    GroupType = 1,
                    CategoryId = 1,
                    Shareable = false,
                    RadioCompleteable = false,
                    Comment = "Legacy chain member"
                });
                fixture.Prerequisites.Add(new MissionPrerequisiteEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    PrerequisiteId = 1,
                    Requirement = MissionContentRequirement.Required,
                    Kind = MissionPrerequisiteKind.MissionCompleted,
                    RequiredMissionId = 322,
                    RequiredMissionState = (byte)MissionState.Completed,
                    Comment = "Cross revision prerequisite"
                });
            }, "cross-revision-reference");

            yield return Case("required chain inactive", fixture =>
            {
                fixture.Definitions.Add(new MissionContentDefinitionEntry
                {
                    MissionId = 322,
                    ContentRevision = "deployment_11",
                    Requirement = MissionContentRequirement.Required,
                    ClientNameTextId = 3201,
                    GiverId = 1,
                    ReceiverId = 2,
                    Level = 1,
                    GroupType = 1,
                    CategoryId = 1,
                    Shareable = false,
                    RadioCompleteable = false,
                    Comment = "Required predecessor"
                });
                fixture.Prerequisites.Add(new MissionPrerequisiteEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    PrerequisiteId = 1,
                    Requirement = MissionContentRequirement.Required,
                    Kind = MissionPrerequisiteKind.MissionCompleted,
                    RequiredMissionId = 322,
                    RequiredMissionState = (byte)MissionState.Completed,
                    Comment = "Required prerequisite"
                });
            }, "required-chain-inactive");

        }

        private static object[] Case(
            string name,
            Action<MissionContentFixture> mutate,
            string expectedCode) =>
            new object[] { name, mutate, expectedCode };

        private static MissionContentFixture CreatePureProgressFixture()
        {
            var fixture = MissionContentFixture.CreateValid();
            ConfigurePureProgressObjective(fixture);
            return fixture;
        }

        private static void ConfigurePureProgressObjective(
            MissionContentFixture fixture)
        {
            fixture.Triggers.Clear();
            fixture.Actions.Clear();
            fixture.Transitions.Clear();
            fixture.Rewards.Clear();
            fixture.RewardItems.Clear();
            fixture.Indicators.Clear();
            fixture.Objectives.RemoveAll(objective => objective.ObjectiveId == 11);
            AddProgressTransition(
                fixture,
                objectiveId: 10,
                transitionId: 20,
                triggerId: 1,
                subjectId: 501);
        }

        private static void ConfigureApprovedScenarioVocabulary(MissionContentFixture fixture)
        {
            ReplaceScenarioRewardWithNoSelectionPackage(fixture, 41);
            fixture.ScenarioSteps.Clear();
            fixture.ScenarioSteps.AddRange(new[]
            {
                new MissionScenarioStepEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ScenarioId = 60,
                    StepId = 1,
                    Requirement = MissionContentRequirement.Required,
                    Kind = MissionScenarioStepKind.SpawnGroup,
                    Sequence = 1,
                    SpawnGroupId = 50,
                    Comment = "Spawn group"
                },
                new MissionScenarioStepEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ScenarioId = 60,
                    StepId = 2,
                    Requirement = MissionContentRequirement.Required,
                    Kind = MissionScenarioStepKind.DespawnGroup,
                    Sequence = 2,
                    SpawnGroupId = 50,
                    Comment = "Despawn group"
                },
                new MissionScenarioStepEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ScenarioId = 60,
                    StepId = 3,
                    Requirement = MissionContentRequirement.Required,
                    Kind = MissionScenarioStepKind.SpawnDynamicObject,
                    Sequence = 3,
                    DynamicObjectKey = "bootcamp-crate",
                    EntityClassId = 3147,
                    PosX = 8,
                    PosY = 9,
                    PosZ = 10,
                    Orientation = 0.5,
                    InitialInteractionEnabled = false,
                    Comment = "Spawn dynamic object"
                },
                new MissionScenarioStepEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ScenarioId = 60,
                    StepId = 4,
                    Requirement = MissionContentRequirement.Required,
                    Kind = MissionScenarioStepKind.DespawnDynamicObject,
                    Sequence = 4,
                    DynamicObjectKey = "bootcamp-crate",
                    Comment = "Despawn dynamic object"
                },
                new MissionScenarioStepEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ScenarioId = 60,
                    StepId = 5,
                    Requirement = MissionContentRequirement.Required,
                    Kind = MissionScenarioStepKind.EnableInteraction,
                    Sequence = 5,
                    EntityClassId = 3147,
                    Comment = "Enable interaction by entity class"
                },
                new MissionScenarioStepEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ScenarioId = 60,
                    StepId = 6,
                    Requirement = MissionContentRequirement.Required,
                    Kind = MissionScenarioStepKind.DisableInteraction,
                    Sequence = 6,
                    SpawnGroupId = 50,
                    SpawnId = 1,
                    Comment = "Disable interaction by spawn identity"
                },
                new MissionScenarioStepEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ScenarioId = 60,
                    StepId = 7,
                    Requirement = MissionContentRequirement.Required,
                    Kind = MissionScenarioStepKind.RevealObjective,
                    Sequence = 7,
                    TargetObjectiveId = 11,
                    Comment = "Reveal objective"
                },
                new MissionScenarioStepEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ScenarioId = 60,
                    StepId = 8,
                    Requirement = MissionContentRequirement.Required,
                    Kind = MissionScenarioStepKind.ActivateObjective,
                    Sequence = 8,
                    TargetObjectiveId = 11,
                    Comment = "Activate objective"
                },
                new MissionScenarioStepEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ScenarioId = 60,
                    StepId = 9,
                    Requirement = MissionContentRequirement.Required,
                    Kind = MissionScenarioStepKind.CompleteObjective,
                    Sequence = 9,
                    TargetObjectiveId = 10,
                    Comment = "Complete objective"
                },
                new MissionScenarioStepEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ScenarioId = 60,
                    StepId = 10,
                    Requirement = MissionContentRequirement.Required,
                    Kind = MissionScenarioStepKind.FailObjective,
                    Sequence = 10,
                    TargetObjectiveId = 11,
                    Comment = "Fail objective"
                },
                new MissionScenarioStepEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ScenarioId = 60,
                    StepId = 11,
                    Requirement = MissionContentRequirement.Required,
                    Kind = MissionScenarioStepKind.StartDeadline,
                    Sequence = 11,
                    DelayMilliseconds = 30000,
                    Comment = "Start deadline"
                },
                new MissionScenarioStepEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ScenarioId = 60,
                    StepId = 12,
                    Requirement = MissionContentRequirement.Required,
                    Kind = MissionScenarioStepKind.CancelDeadline,
                    Sequence = 12,
                    Comment = "Cancel deadline"
                },
                new MissionScenarioStepEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ScenarioId = 60,
                    StepId = 13,
                    Requirement = MissionContentRequirement.Required,
                    Kind = MissionScenarioStepKind.GrantRewardPackage,
                    Sequence = 13,
                    RewardId = 41,
                    Comment = "Grant reward"
                },
                new MissionScenarioStepEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ScenarioId = 60,
                    StepId = 14,
                    Requirement = MissionContentRequirement.Required,
                    Kind = MissionScenarioStepKind.GrantSkillAbility,
                    Sequence = 14,
                    SkillId = 901,
                    AbilityId = 194,
                    SkillLevel = 2,
                    AbilitySlot = 3,
                    Comment = "Grant skill ability"
                },
                new MissionScenarioStepEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ScenarioId = 60,
                    StepId = 15,
                    Requirement = MissionContentRequirement.Required,
                    Kind = MissionScenarioStepKind.PlayTutorial,
                    Sequence = 15,
                    TutorialId = (uint)TutorialId.Tutmissiongiver,
                    AudioSetId = 88,
                    Comment = "Play tutorial"
                },
                new MissionScenarioStepEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ScenarioId = 60,
                    StepId = 16,
                    Requirement = MissionContentRequirement.Required,
                    Kind = MissionScenarioStepKind.ScheduleScenario,
                    Sequence = 16,
                    TargetScenarioId = 60,
                    DelayMilliseconds = 5000,
                    Comment = "Schedule scenario"
                },
                new MissionScenarioStepEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ScenarioId = 60,
                    StepId = 17,
                    Requirement = MissionContentRequirement.Required,
                    Kind = MissionScenarioStepKind.ResetAttempt,
                    Sequence = 17,
                    AttemptKey = "bootcamp-scout",
                    Comment = "Reset attempt by key"
                },
                new MissionScenarioStepEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ScenarioId = 60,
                    StepId = 18,
                    Requirement = MissionContentRequirement.Required,
                    Kind = MissionScenarioStepKind.ResetAttempt,
                    Sequence = 18,
                    TargetScenarioId = 60,
                    Comment = "Reset attempt by scenario"
                },
                new MissionScenarioStepEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ScenarioId = 60,
                    StepId = 19,
                    Requirement = MissionContentRequirement.Required,
                    Kind = MissionScenarioStepKind.EmitScenarioEvent,
                    Sequence = 19,
                    ScenarioEventId = 7,
                    Comment = "Emit scenario event"
                },
                new MissionScenarioStepEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ScenarioId = 60,
                    StepId = 20,
                    Requirement = MissionContentRequirement.Required,
                    Kind = MissionScenarioStepKind.TransferPlayer,
                    Sequence = 20,
                    MapContextId = 1220,
                    PosX = 1,
                    PosY = 2,
                    PosZ = 3,
                    Orientation = 1.5,
                    Comment = "Transfer player"
                },
                new MissionScenarioStepEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ScenarioId = 60,
                    StepId = 21,
                    Requirement = MissionContentRequirement.Required,
                    Kind = MissionScenarioStepKind.SetQualification,
                    Sequence = 21,
                    QualificationKey = CharacterQualificationKey.BootcampComplete,
                    QualificationValue = MissionScenarioStepEntry.RemovedQualificationValue,
                    Comment = "Set qualification"
                },
                new MissionScenarioStepEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ScenarioId = 60,
                    StepId = 22,
                    Requirement = MissionContentRequirement.Required,
                    Kind = MissionScenarioStepKind.SetAccountSkipEntitlement,
                    Sequence = 22,
                    AccountSkipEntitlement = true,
                    Comment = "Set account skip entitlement"
                }});
        }

        private static void ReplaceScenarioRewardWithNoSelectionPackage(
            MissionContentFixture fixture,
            uint rewardId)
        {
            fixture.Rewards.Add(new MissionRewardDefinitionEntry
            {
                MissionId = 321,
                ContentRevision = "deployment_11",
                RewardId = rewardId,
                Requirement = MissionContentRequirement.Required,
                Experience = 125,
                Credits = 75,
                Prestige = 10,
                SelectionCount = 0,
                Comment = "Scenario-safe reward"
            });
            fixture.RewardItems.AddRange(
                new MissionRewardItemEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    RewardId = rewardId,
                    ItemId = 41,
                    Kind = MissionRewardItemKind.Fixed,
                    ItemTemplateId = 28,
                    Quantity = 2
                },
                new MissionRewardItemEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    RewardId = rewardId,
                    ItemId = 42,
                    Kind = MissionRewardItemKind.Fixed,
                    ItemTemplateId = 29,
                    Quantity = 1
                });
        }

        private static void AddProgressTransition(
            MissionContentFixture fixture,
            uint objectiveId,
            uint transitionId,
            uint triggerId,
            uint subjectId)
        {
            fixture.Transitions.Add(new MissionObjectiveTransitionEntry
            {
                MissionId = 321,
                ContentRevision = "deployment_11",
                ObjectiveId = objectiveId,
                TransitionId = transitionId,
                Requirement = MissionContentRequirement.Required,
                Sequence = transitionId - 19,
                FromState = (byte)MissionObjectiveState.Incomplete,
                ToState = (byte)MissionObjectiveState.Completed,
                Comment = $"Progress transition {transitionId}"
            });
            fixture.Triggers.Add(new MissionTriggerEntry
            {
                MissionId = 321,
                ContentRevision = "deployment_11",
                ObjectiveId = objectiveId,
                TransitionId = transitionId,
                TriggerId = triggerId,
                Requirement = MissionContentRequirement.Required,
                Kind = MissionTriggerKind.ProgressEvent,
                Sequence = 1,
                EventKind = (byte)MissionProgressEventKind.CreatureKilled,
                SubjectId = subjectId,
                Comment = $"Kill creature {subjectId}"
            });
        }

        private static void AddSingleObjectiveConversationMission(
            MissionContentFixture fixture,
            uint missionId,
            uint prerequisiteMissionId)
        {
            fixture.Definitions.Add(new MissionContentDefinitionEntry
            {
                MissionId = missionId,
                ContentRevision = "deployment_11",
                Requirement = MissionContentRequirement.Required,
                ClientNameTextId = 3000 + missionId,
                GiverId = 100 + missionId,
                ReceiverId = 200 + missionId,
                Level = 9,
                GroupType = 2,
                CategoryId = 3,
                Shareable = false,
                RadioCompleteable = false,
                Comment = $"Mission {missionId}"
            });
            fixture.Objectives.Add(new MissionObjectiveDefinitionEntry
            {
                MissionId = missionId,
                ContentRevision = "deployment_11",
                ObjectiveId = 1,
                Requirement = MissionContentRequirement.Required,
                ClientNameTextId = 4000 + missionId,
                ClientBodyTextId = 5000 + missionId,
                Ordinal = 1,
                InitialState = (byte)MissionObjectiveState.Incomplete,
                IsRequired = true,
                Comment = $"Mission {missionId} objective"
            });
            fixture.Transitions.Add(new MissionObjectiveTransitionEntry
            {
                MissionId = missionId,
                ContentRevision = "deployment_11",
                ObjectiveId = 1,
                TransitionId = 1,
                Requirement = MissionContentRequirement.Required,
                Sequence = 1,
                FromState = (byte)MissionObjectiveState.Incomplete,
                ToState = (byte)MissionObjectiveState.Completed,
                Comment = $"Mission {missionId} completion"
            });
            fixture.Triggers.Add(new MissionTriggerEntry
            {
                MissionId = missionId,
                ContentRevision = "deployment_11",
                ObjectiveId = 1,
                TransitionId = 1,
                TriggerId = 1,
                Requirement = MissionContentRequirement.Required,
                Kind = MissionTriggerKind.Conversation,
                Sequence = 1,
                NpcPackageId = 77,
                PlayerFlagId = 11,
                Comment = $"Mission {missionId} conversation"
            });
            fixture.Actions.Add(new MissionActionEntry
            {
                MissionId = missionId,
                ContentRevision = "deployment_11",
                ObjectiveId = 1,
                TransitionId = 1,
                ActionId = 1,
                Requirement = MissionContentRequirement.Required,
                Kind = MissionActionKind.CompleteObjective,
                Sequence = 1,
                TargetObjectiveId = 1,
                ObjectiveState = (byte)MissionObjectiveState.Completed,
                Comment = $"Mission {missionId} complete"
            });
            fixture.Prerequisites.Add(new MissionPrerequisiteEntry
            {
                MissionId = missionId,
                ContentRevision = "deployment_11",
                PrerequisiteId = 1,
                Requirement = MissionContentRequirement.Required,
                Kind = MissionPrerequisiteKind.MissionCompleted,
                RequiredMissionId = prerequisiteMissionId,
                RequiredMissionState = (byte)MissionState.Completed,
                Comment = $"Requires mission {prerequisiteMissionId}"
            });
        }
    }
}
