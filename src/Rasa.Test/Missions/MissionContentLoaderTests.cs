using System;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions
{
    using Rasa.Managers;
    using Rasa.Repositories.Char;
    using Rasa.Repositories.UnitOfWork;
    using Rasa.Repositories.World;
    using Rasa.Structures;
    using Rasa.Structures.Missions;

    [TestClass]
    public class MissionContentLoaderTests
    {
        [TestMethod]
        public void LoaderPrefersAuthoredRevisionAndBuildsImmutableRuntimeDefinitions()
        {
            var fixture = MissionContentFixture.CreateValid();
            var snapshot = new MissionContentLoader().Load(fixture.CreateRepository());

            Assert.AreEqual(1, snapshot.Definitions.Count);
            var content = snapshot.Definitions[321];
            Assert.AreEqual("deployment_11", content.ContentRevision);
            Assert.AreEqual(2, content.Mission.Objectives.Count);
            Assert.AreEqual(1, content.Rewards.Count);
            Assert.AreEqual(1, content.Scenarios.Count);

            var firstObjective = content.Mission.Objectives[10];
            Assert.AreEqual(1, firstObjective.Conversations.Count);
            Assert.AreEqual(77U, firstObjective.Conversations[0].NpcPackageId);
            CollectionAssert.AreEqual(new uint[] { 11 }, firstObjective.RevealedObjectiveIds.ToArray());
            CollectionAssert.AreEqual(new uint[] { 11 }, firstObjective.ActivatedObjectiveIds.ToArray());

            var reward = content.Rewards[40];
            Assert.AreEqual(75U, reward.Credits);
            Assert.AreEqual(1, reward.FixedItems.Count);
            Assert.AreEqual(1, reward.SelectableItems.Count);

            var firstStep = content.Scenarios[60].Steps.Single();
            Assert.AreEqual(Rasa.Structures.World.MissionScenarioStepKind.EmitScenarioEvent, firstStep.Kind);
            Assert.AreEqual(1U, firstStep.ScenarioEventId);

            var transition = content.Transitions.Values.Single();
            Assert.AreEqual(1, transition.Triggers.Count);
            Assert.AreEqual(4, transition.Actions.Count);
        }

        [TestMethod]
        public void LoaderBuildsImmutableScenarioStepDefinitionsForApprovedVocabulary()
        {
            var fixture = MissionContentFixture.CreateValid();
            ConfigureApprovedScenarioVocabulary(fixture);

            var snapshot = new MissionContentLoader().Load(fixture.CreateRepository());
            var steps = snapshot.Definitions[321].Scenarios[60].Steps;
            var spawnGroup = snapshot.Definitions[321].SpawnGroups[50];

            Assert.AreEqual(23, steps.Count);
            Assert.AreEqual(Rasa.Structures.World.MissionSpawnGroupPolicy.ScenarioControlled, spawnGroup.SpawnPolicy);
            Assert.AreEqual(Rasa.Structures.World.MissionScenarioStepKind.SpawnGroup, steps[0].Kind);
            Assert.AreEqual(50U, steps[0].SpawnGroupId);
            Assert.AreEqual(Rasa.Structures.World.MissionScenarioStepKind.SpawnDynamicObject, steps[2].Kind);
            Assert.AreEqual("bootcamp-crate", steps[2].DynamicObjectKey);
            Assert.AreEqual(3147U, steps[2].EntityClassId);
            Assert.AreEqual(8D, steps[2].PosX);
            Assert.AreEqual(9D, steps[2].PosY);
            Assert.AreEqual(10D, steps[2].PosZ);
            Assert.AreEqual(0.5D, steps[2].Orientation);
            Assert.AreEqual(false, steps[2].InitialInteractionEnabled);
            Assert.AreEqual(Rasa.Structures.World.MissionScenarioStepKind.DespawnDynamicObject, steps[3].Kind);
            Assert.AreEqual("bootcamp-crate", steps[3].DynamicObjectKey);
            Assert.AreEqual(Rasa.Structures.World.MissionScenarioStepKind.EnableInteraction, steps[4].Kind);
            Assert.AreEqual(3147U, steps[4].EntityClassId);
            Assert.AreEqual(Rasa.Structures.World.MissionScenarioStepKind.DisableInteraction, steps[5].Kind);
            Assert.AreEqual(1U, steps[5].SpawnId);
            Assert.AreEqual(Rasa.Structures.World.MissionScenarioStepKind.StartDeadline, steps[10].Kind);
            Assert.AreEqual(30000U, steps[10].DelayMilliseconds);
            Assert.AreEqual(Rasa.Structures.World.MissionScenarioStepKind.GrantSkillAbility, steps[13].Kind);
            Assert.AreEqual(901U, steps[13].SkillId);
            Assert.AreEqual(194U, steps[13].AbilityId);
            Assert.AreEqual((byte)2, steps[13].SkillLevel);
            Assert.AreEqual((byte)3, steps[13].AbilitySlot);
            Assert.AreEqual(Rasa.Structures.World.MissionScenarioStepKind.PlayTutorial, steps[14].Kind);
            Assert.AreEqual((uint)Rasa.Data.TutorialId.Tutmissiongiver, steps[14].TutorialId);
            Assert.AreEqual(88U, steps[14].AudioSetId);
            Assert.AreEqual(Rasa.Structures.World.MissionScenarioStepKind.ScheduleScenario, steps[15].Kind);
            Assert.AreEqual(60U, steps[15].TargetScenarioId);
            Assert.AreEqual(5000U, steps[15].DelayMilliseconds);
            Assert.AreEqual(Rasa.Structures.World.MissionScenarioStepKind.ResetAttempt, steps[16].Kind);
            Assert.AreEqual("bootcamp-scout", steps[16].AttemptKey);
            Assert.AreEqual(Rasa.Structures.World.MissionScenarioStepKind.ResetAttempt, steps[17].Kind);
            Assert.AreEqual(60U, steps[17].TargetScenarioId);
            Assert.AreEqual(Rasa.Structures.World.MissionScenarioStepKind.TransferPlayer, steps[19].Kind);
            Assert.AreEqual(1220U, steps[19].MapContextId);
            Assert.AreEqual(1D, steps[19].PosX);
            Assert.AreEqual(2D, steps[19].PosY);
            Assert.AreEqual(3D, steps[19].PosZ);
            Assert.AreEqual(1.5D, steps[19].Orientation);
            Assert.AreEqual(Rasa.Structures.World.MissionScenarioStepKind.SetQualification, steps[20].Kind);
            Assert.AreEqual(Rasa.Structures.Char.CharacterQualificationKey.BootcampComplete, steps[20].QualificationKey);
            Assert.AreEqual(Rasa.Structures.World.MissionScenarioStepEntry.RemovedQualificationValue, steps[20].QualificationValue);
            Assert.AreEqual(Rasa.Structures.World.MissionScenarioStepKind.SetAccountSkipEntitlement, steps[21].Kind);
            Assert.AreEqual(true, steps[21].AccountSkipEntitlement);
            Assert.AreEqual(Rasa.Structures.World.MissionScenarioStepKind.EscortSpawnGroup, steps[22].Kind);
            Assert.AreEqual(50U, steps[22].SpawnGroupId);

            fixture.ScenarioSteps[0].SpawnGroupId = 999;
            Assert.AreEqual(50U, steps[0].SpawnGroupId);
        }

        [TestMethod]
        public void LoaderBuildsProgressRulesFromSupportedProgressTransitions()
        {
            var fixture = MissionContentFixture.CreateValid();
            fixture.Triggers.Clear();
            fixture.Actions.Clear();
            fixture.Transitions.Clear();
            fixture.Objectives.RemoveAll(objective => objective.ObjectiveId == 11);
            fixture.Transitions.Add(new Rasa.Structures.World.MissionObjectiveTransitionEntry
            {
                MissionId = 321,
                ContentRevision = "deployment_11",
                ObjectiveId = 10,
                TransitionId = 20,
                Requirement = Rasa.Structures.World.MissionContentRequirement.Required,
                Sequence = 1,
                FromState = (byte)Rasa.Data.MissionObjectiveState.Incomplete,
                ToState = (byte)Rasa.Data.MissionObjectiveState.Completed,
                Comment = "Kill one creature"
            });
            fixture.Triggers.Add(new Rasa.Structures.World.MissionTriggerEntry
            {
                MissionId = 321,
                ContentRevision = "deployment_11",
                ObjectiveId = 10,
                TransitionId = 20,
                TriggerId = 1,
                Requirement = Rasa.Structures.World.MissionContentRequirement.Required,
                Kind = Rasa.Structures.World.MissionTriggerKind.ProgressEvent,
                Sequence = 1,
                EventKind = (byte)Rasa.Data.MissionProgressEventKind.CreatureKilled,
                SubjectId = 501,
                Comment = "Kill creature 501"
            });

            var snapshot = new MissionContentLoader().Load(fixture.CreateRepository());
            var rule = snapshot.Definitions[321].Mission.Objectives[10].ProgressRule;

            Assert.IsNotNull(rule);
            Assert.AreEqual(Rasa.Data.MissionProgressEventKind.CreatureKilled, rule.Kind);
            CollectionAssert.AreEqual(new uint[] { 501 }, rule.Subjects.ToArray());
        }

        [TestMethod]
        public void LoaderBuildsAreaAndDeadlineRulesFromSupportedTriggerKinds()
        {
            var areaFixture = CreatePureProgressFixture();
            areaFixture.Triggers[0].Kind = Rasa.Structures.World.MissionTriggerKind.AreaEntered;
            areaFixture.Triggers[0].EventKind = null;
            areaFixture.Triggers[0].SubjectId = null;
            areaFixture.Triggers[0].AreaId = 30;

            var areaSnapshot = new MissionContentLoader().Load(areaFixture.CreateRepository());
            var areaRule = areaSnapshot.Definitions[321].Mission.Objectives[10].ProgressRule;

            Assert.IsNotNull(areaRule);
            Assert.AreEqual(Rasa.Data.MissionProgressEventKind.AreaEntered, areaRule.Kind);
            Assert.AreEqual((uint)321, areaRule.ScopeId);
            CollectionAssert.AreEqual(new uint[] { 30 }, areaRule.Subjects.ToArray());

            var timerFixture = CreatePureProgressFixture();
            timerFixture.Triggers[0].Kind = Rasa.Structures.World.MissionTriggerKind.TimerElapsed;
            timerFixture.Triggers[0].EventKind = null;
            timerFixture.Triggers[0].SubjectId = null;
            timerFixture.Triggers[0].DurationSeconds = 5;

            var timerSnapshot = new MissionContentLoader().Load(timerFixture.CreateRepository());
            var timerRule = timerSnapshot.Definitions[321].Mission.Objectives[10].ProgressRule;

            Assert.IsNotNull(timerRule);
            Assert.AreEqual(Rasa.Data.MissionProgressEventKind.DeadlineElapsed, timerRule.Kind);
            Assert.AreEqual((uint)321, timerRule.ScopeId);
            Assert.AreEqual((uint)10, timerRule.Subjects.Single());
            Assert.AreEqual((uint)5, timerRule.DurationSeconds);
        }

        [TestMethod]
        public void LoaderBuildsMultipleExecutableProgressPathsInSequenceOrder()
        {
            var fixture = CreatePureProgressFixture();
            AddProgressTransition(
                fixture,
                objectiveId: 10,
                transitionId: 21,
                triggerId: 2,
                subjectId: 502);

            var snapshot = new MissionContentLoader().Load(fixture.CreateRepository());
            var mission = snapshot.Definitions[321].Mission;
            var objective = mission.Objectives[10];

            Assert.IsTrue(mission.IsOperational);
            Assert.AreEqual(2, objective.ExecutableTransitions.Count);
            CollectionAssert.AreEqual(
                new uint[] { 20, 21 },
                objective.ExecutableTransitions.Select(transition => transition.TransitionId).ToArray());
        }

        [TestMethod]
        public void LoaderScopesTransitionDefinitionsByObjectiveAndTransitionId()
        {
            var fixture = MissionContentFixture.CreateValid();
            fixture.Transitions[0].TransitionId = 1;
            fixture.Triggers[0].TransitionId = 1;
            fixture.Actions.ForEach(action => action.TransitionId = 1);
            fixture.Transitions.Add(new Rasa.Structures.World.MissionObjectiveTransitionEntry
            {
                MissionId = 321,
                ContentRevision = "deployment_11",
                ObjectiveId = 11,
                TransitionId = 1,
                Requirement = Rasa.Structures.World.MissionContentRequirement.Required,
                Sequence = 2,
                FromState = (byte)Rasa.Data.MissionObjectiveState.Incomplete,
                ToState = (byte)Rasa.Data.MissionObjectiveState.Completed,
                Comment = "Objective-local transition id"
            });
            fixture.Triggers.Add(new Rasa.Structures.World.MissionTriggerEntry
            {
                MissionId = 321,
                ContentRevision = "deployment_11",
                ObjectiveId = 11,
                TransitionId = 1,
                TriggerId = 2,
                Requirement = Rasa.Structures.World.MissionContentRequirement.Required,
                Kind = Rasa.Structures.World.MissionTriggerKind.ProgressEvent,
                Sequence = 1,
                EventKind = (byte)Rasa.Data.MissionProgressEventKind.CreatureKilled,
                SubjectId = 501,
                Comment = "Kill creature 501"
            });
            var snapshot = new MissionContentLoader().Load(fixture.CreateRepository());
            var content = snapshot.Definitions[321];
            var objectiveTenTransition = content.Transitions[(10U, 1U)];
            var objectiveElevenTransition = content.Transitions[(11U, 1U)];

            Assert.AreEqual(2, content.Transitions.Count);
            Assert.AreEqual(Rasa.Structures.World.MissionTriggerKind.Conversation,
                objectiveTenTransition.Triggers.Single().Kind);
            Assert.AreEqual(4, objectiveTenTransition.Actions.Count);
            Assert.AreEqual(Rasa.Structures.World.MissionTriggerKind.ProgressEvent,
                objectiveElevenTransition.Triggers.Single().Kind);
            Assert.AreEqual(0, objectiveElevenTransition.Actions.Count);
            Assert.IsNotNull(content.Mission.Objectives[11].ProgressRule);
            Assert.AreEqual(Rasa.Data.MissionProgressEventKind.CreatureKilled,
                content.Mission.Objectives[11].ProgressRule.Kind);
        }

        [TestMethod]
        public void MissionManagerUsesReferencedGrantRewardInsteadOfLowestRewardId()
        {
            var fixture = MissionContentFixture.CreateValid();
            fixture.Rewards.Add(new Rasa.Structures.World.MissionRewardDefinitionEntry
            {
                MissionId = 321,
                ContentRevision = "deployment_11",
                RewardId = 39,
                Requirement = Rasa.Structures.World.MissionContentRequirement.Required,
                Experience = 1,
                Credits = 5,
                Prestige = 0,
                SelectionCount = 0,
                Comment = "Lower id reward"
            });
            fixture.RewardItems.Add(new Rasa.Structures.World.MissionRewardItemEntry
            {
                MissionId = 321,
                ContentRevision = "deployment_11",
                RewardId = 39,
                ItemId = 1,
                Kind = Rasa.Structures.World.MissionRewardItemKind.Fixed,
                ItemTemplateId = 28,
                Quantity = 1
            });
            using var context = MissionTestContext.WithCustomDefinitions(
                new System.Collections.Generic.Dictionary<uint, Mission>());
            var manager = new MissionApplication(
                new MissionContentLoadingFactory(context, fixture.CreateWorldUnitOfWork()),
                new System.Collections.Generic.Dictionary<uint, Mission>());

            var report = manager.LoadMissions();

            Assert.IsFalse(report.BlocksReadiness);
            Assert.IsTrue(manager.TryGetRewardInfo(321, out var rewardInfo));
            Assert.AreEqual(75U, rewardInfo.FixedReward.Credits[Rasa.Data.CurencyType.Credits]);
            Assert.AreEqual(10U, rewardInfo.FixedReward.Credits[Rasa.Data.CurencyType.Prestige]);
            Assert.AreEqual(1, rewardInfo.SelectableReward.Count);
        }

        [TestMethod]
        public void MissionManagerLoadsOperationalDatabaseMissionFromMissionContent()
        {
            var fixture = MissionContentFixture.CreateValid();
            using var context = MissionTestContext.WithCustomDefinitions(
                new System.Collections.Generic.Dictionary<uint, Mission>());
            var manager = new MissionApplication(
                new MissionContentLoadingFactory(context, fixture.CreateWorldUnitOfWork()),
                new System.Collections.Generic.Dictionary<uint, Mission>());

            var report = manager.LoadMissions();
            var giver = context.AddNpc(101);

            Assert.IsFalse(report.BlocksReadiness);
            Assert.IsTrue(manager.LoadedMissions[321].IsOperational);
            Assert.IsTrue(manager.TryGetRewardInfo(321, out var rewardInfo));
            Assert.AreEqual(1, rewardInfo.SelectableReward.Count);
            Assert.IsTrue(manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
        }

        [TestMethod]
        public void MissionManagerReturnsBlockingReportForRequiredInvalidMissionContent()
        {
            var fixture = MissionContentFixture.CreateValid();
            fixture.Objectives[0].ClientBodyTextId = 0;
            using var context = MissionTestContext.WithCustomDefinitions(
                new System.Collections.Generic.Dictionary<uint, Mission>());
            var manager = new MissionApplication(
                new MissionContentLoadingFactory(context, fixture.CreateWorldUnitOfWork()),
                new System.Collections.Generic.Dictionary<uint, Mission>());

            var report = manager.LoadMissions();

            Assert.IsTrue(report.BlocksReadiness);
            Assert.IsFalse(manager.LoadedMissions[321].IsOperational);
            StringAssert.Contains(
                manager.LoadedMissions[321].OperationalDiagnostic,
                "objective text bindings are incomplete");
        }

        [TestMethod]
        public void MissionManagerLoadsOperationalProgressTransitionsWithActionsAndMultipleExecutableRules()
        {
            var fixture = CreatePureProgressFixture();
            fixture.Actions.Add(new Rasa.Structures.World.MissionActionEntry
            {
                MissionId = 321,
                ContentRevision = "deployment_11",
                ObjectiveId = 10,
                TransitionId = 20,
                ActionId = 2,
                Requirement = Rasa.Structures.World.MissionContentRequirement.Required,
                Kind = Rasa.Structures.World.MissionActionKind.StartScenario,
                Sequence = 2,
                ScenarioId = 60,
                Comment = "Start follow-up scenario"
            });
            fixture.Transitions.Add(new Rasa.Structures.World.MissionObjectiveTransitionEntry
            {
                MissionId = 321,
                ContentRevision = "deployment_11",
                ObjectiveId = 10,
                TransitionId = 21,
                Requirement = Rasa.Structures.World.MissionContentRequirement.Required,
                Sequence = 2,
                FromState = (byte)Rasa.Data.MissionObjectiveState.Incomplete,
                ToState = (byte)Rasa.Data.MissionObjectiveState.Completed,
                Comment = "Timeout transition"
            });
            fixture.Triggers.Add(new Rasa.Structures.World.MissionTriggerEntry
            {
                MissionId = 321,
                ContentRevision = "deployment_11",
                ObjectiveId = 10,
                TransitionId = 21,
                TriggerId = 2,
                Requirement = Rasa.Structures.World.MissionContentRequirement.Required,
                Kind = Rasa.Structures.World.MissionTriggerKind.TimerElapsed,
                Sequence = 1,
                DurationSeconds = 5,
                Comment = "Timeout"
            });
            using var context = MissionTestContext.WithCustomDefinitions(
                new System.Collections.Generic.Dictionary<uint, Mission>());
            var manager = new MissionApplication(
                new MissionContentLoadingFactory(context, fixture.CreateWorldUnitOfWork()),
                new System.Collections.Generic.Dictionary<uint, Mission>());

            var report = manager.LoadMissions();
            var giver = context.AddNpc(101);

            Assert.IsFalse(report.BlocksReadiness);
            Assert.IsTrue(manager.LoadedMissions[321].IsOperational);
            Assert.AreEqual(2, manager.LoadedMissions[321].Objectives[10].ExecutableTransitions.Count);
            Assert.IsTrue(manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
        }

        [TestMethod]
        public void MissionManagerLoadsOperationalPureProgressCompletionMission()
        {
            var fixture = CreatePureProgressFixture();
            using var context = MissionTestContext.WithCustomDefinitions(
                new System.Collections.Generic.Dictionary<uint, Mission>());
            var manager = new MissionApplication(
                new MissionContentLoadingFactory(context, fixture.CreateWorldUnitOfWork()),
                new System.Collections.Generic.Dictionary<uint, Mission>());

            var report = manager.LoadMissions();
            var giver = context.AddNpc(101);

            Assert.IsFalse(report.BlocksReadiness);
            Assert.IsTrue(manager.LoadedMissions[321].IsOperational);
            Assert.IsTrue(manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
        }

        private static MissionContentFixture CreatePureProgressFixture()
        {
            var fixture = MissionContentFixture.CreateValid();
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
            return fixture;
        }

        private static void ConfigureApprovedScenarioVocabulary(MissionContentFixture fixture)
        {
            fixture.ScenarioSteps.Clear();
            fixture.ScenarioSteps.AddRange(new[]
            {
                new Rasa.Structures.World.MissionScenarioStepEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ScenarioId = 60,
                    StepId = 1,
                    Requirement = Rasa.Structures.World.MissionContentRequirement.Required,
                    Kind = Rasa.Structures.World.MissionScenarioStepKind.SpawnGroup,
                    Sequence = 1,
                    SpawnGroupId = 50,
                    Comment = "Spawn bootcamp group"
                },
                new Rasa.Structures.World.MissionScenarioStepEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ScenarioId = 60,
                    StepId = 2,
                    Requirement = Rasa.Structures.World.MissionContentRequirement.Required,
                    Kind = Rasa.Structures.World.MissionScenarioStepKind.DespawnGroup,
                    Sequence = 2,
                    SpawnGroupId = 50,
                    Comment = "Despawn bootcamp group"
                },
                new Rasa.Structures.World.MissionScenarioStepEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ScenarioId = 60,
                    StepId = 3,
                    Requirement = Rasa.Structures.World.MissionContentRequirement.Required,
                    Kind = Rasa.Structures.World.MissionScenarioStepKind.SpawnDynamicObject,
                    Sequence = 3,
                    DynamicObjectKey = "bootcamp-crate",
                    EntityClassId = 3147,
                    PosX = 8,
                    PosY = 9,
                    PosZ = 10,
                    Orientation = 0.5,
                    InitialInteractionEnabled = false,
                    Comment = "Spawn a tracked dynamic object"
                },
                new Rasa.Structures.World.MissionScenarioStepEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ScenarioId = 60,
                    StepId = 4,
                    Requirement = Rasa.Structures.World.MissionContentRequirement.Required,
                    Kind = Rasa.Structures.World.MissionScenarioStepKind.DespawnDynamicObject,
                    Sequence = 4,
                    DynamicObjectKey = "bootcamp-crate",
                    Comment = "Despawn the tracked dynamic object"
                },
                new Rasa.Structures.World.MissionScenarioStepEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ScenarioId = 60,
                    StepId = 5,
                    Requirement = Rasa.Structures.World.MissionContentRequirement.Required,
                    Kind = Rasa.Structures.World.MissionScenarioStepKind.EnableInteraction,
                    Sequence = 5,
                    EntityClassId = 3147,
                    Comment = "Enable interaction on the stable world object"
                },
                new Rasa.Structures.World.MissionScenarioStepEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ScenarioId = 60,
                    StepId = 6,
                    Requirement = Rasa.Structures.World.MissionContentRequirement.Required,
                    Kind = Rasa.Structures.World.MissionScenarioStepKind.DisableInteraction,
                    Sequence = 6,
                    SpawnGroupId = 50,
                    SpawnId = 1,
                    Comment = "Disable interaction on the spawned actor"
                },
                new Rasa.Structures.World.MissionScenarioStepEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ScenarioId = 60,
                    StepId = 7,
                    Requirement = Rasa.Structures.World.MissionContentRequirement.Required,
                    Kind = Rasa.Structures.World.MissionScenarioStepKind.RevealObjective,
                    Sequence = 7,
                    TargetObjectiveId = 11,
                    Comment = "Reveal follow-up objective"
                },
                new Rasa.Structures.World.MissionScenarioStepEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ScenarioId = 60,
                    StepId = 8,
                    Requirement = Rasa.Structures.World.MissionContentRequirement.Required,
                    Kind = Rasa.Structures.World.MissionScenarioStepKind.ActivateObjective,
                    Sequence = 8,
                    TargetObjectiveId = 11,
                    Comment = "Activate follow-up objective"
                },
                new Rasa.Structures.World.MissionScenarioStepEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ScenarioId = 60,
                    StepId = 9,
                    Requirement = Rasa.Structures.World.MissionContentRequirement.Required,
                    Kind = Rasa.Structures.World.MissionScenarioStepKind.CompleteObjective,
                    Sequence = 9,
                    TargetObjectiveId = 10,
                    Comment = "Complete current objective"
                },
                new Rasa.Structures.World.MissionScenarioStepEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ScenarioId = 60,
                    StepId = 10,
                    Requirement = Rasa.Structures.World.MissionContentRequirement.Required,
                    Kind = Rasa.Structures.World.MissionScenarioStepKind.FailObjective,
                    Sequence = 10,
                    TargetObjectiveId = 11,
                    Comment = "Fail follow-up objective"
                },
                new Rasa.Structures.World.MissionScenarioStepEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ScenarioId = 60,
                    StepId = 11,
                    Requirement = Rasa.Structures.World.MissionContentRequirement.Required,
                    Kind = Rasa.Structures.World.MissionScenarioStepKind.StartDeadline,
                    Sequence = 11,
                    DelayMilliseconds = 30000,
                    Comment = "Start a 30 second deadline"
                },
                new Rasa.Structures.World.MissionScenarioStepEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ScenarioId = 60,
                    StepId = 12,
                    Requirement = Rasa.Structures.World.MissionContentRequirement.Required,
                    Kind = Rasa.Structures.World.MissionScenarioStepKind.CancelDeadline,
                    Sequence = 12,
                    Comment = "Cancel active deadline"
                },
                new Rasa.Structures.World.MissionScenarioStepEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ScenarioId = 60,
                    StepId = 13,
                    Requirement = Rasa.Structures.World.MissionContentRequirement.Required,
                    Kind = Rasa.Structures.World.MissionScenarioStepKind.GrantRewardPackage,
                    Sequence = 13,
                    RewardId = 40,
                    Comment = "Grant the authored reward package"
                },
                new Rasa.Structures.World.MissionScenarioStepEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ScenarioId = 60,
                    StepId = 14,
                    Requirement = Rasa.Structures.World.MissionContentRequirement.Required,
                    Kind = Rasa.Structures.World.MissionScenarioStepKind.GrantSkillAbility,
                    Sequence = 14,
                    SkillId = 901,
                    AbilityId = 194,
                    SkillLevel = 2,
                    AbilitySlot = 3,
                    Comment = "Grant a skill and tray slot"
                },
                new Rasa.Structures.World.MissionScenarioStepEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ScenarioId = 60,
                    StepId = 15,
                    Requirement = Rasa.Structures.World.MissionContentRequirement.Required,
                    Kind = Rasa.Structures.World.MissionScenarioStepKind.PlayTutorial,
                    Sequence = 15,
                    TutorialId = (uint)Rasa.Data.TutorialId.Tutmissiongiver,
                    AudioSetId = 88,
                    Comment = "Play the mission giver tutorial"
                },
                new Rasa.Structures.World.MissionScenarioStepEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ScenarioId = 60,
                    StepId = 16,
                    Requirement = Rasa.Structures.World.MissionContentRequirement.Required,
                    Kind = Rasa.Structures.World.MissionScenarioStepKind.ScheduleScenario,
                    Sequence = 16,
                    TargetScenarioId = 60,
                    DelayMilliseconds = 5000,
                    Comment = "Schedule the follow-up scenario"
                },
                new Rasa.Structures.World.MissionScenarioStepEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ScenarioId = 60,
                    StepId = 17,
                    Requirement = Rasa.Structures.World.MissionContentRequirement.Required,
                    Kind = Rasa.Structures.World.MissionScenarioStepKind.ResetAttempt,
                    Sequence = 17,
                    AttemptKey = "bootcamp-scout",
                    Comment = "Reset by attempt key"
                },
                new Rasa.Structures.World.MissionScenarioStepEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ScenarioId = 60,
                    StepId = 18,
                    Requirement = Rasa.Structures.World.MissionContentRequirement.Required,
                    Kind = Rasa.Structures.World.MissionScenarioStepKind.ResetAttempt,
                    Sequence = 18,
                    TargetScenarioId = 60,
                    Comment = "Reset by scenario id"
                },
                new Rasa.Structures.World.MissionScenarioStepEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ScenarioId = 60,
                    StepId = 19,
                    Requirement = Rasa.Structures.World.MissionContentRequirement.Required,
                    Kind = Rasa.Structures.World.MissionScenarioStepKind.EmitScenarioEvent,
                    Sequence = 19,
                    ScenarioEventId = 7,
                    Comment = "Emit a scenario event"
                },
                new Rasa.Structures.World.MissionScenarioStepEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ScenarioId = 60,
                    StepId = 20,
                    Requirement = Rasa.Structures.World.MissionContentRequirement.Required,
                    Kind = Rasa.Structures.World.MissionScenarioStepKind.TransferPlayer,
                    Sequence = 20,
                    MapContextId = 1220,
                    PosX = 1,
                    PosY = 2,
                    PosZ = 3,
                    Orientation = 1.5,
                    Comment = "Transfer to the authored map marker"
                },
                new Rasa.Structures.World.MissionScenarioStepEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ScenarioId = 60,
                    StepId = 21,
                    Requirement = Rasa.Structures.World.MissionContentRequirement.Required,
                    Kind = Rasa.Structures.World.MissionScenarioStepKind.SetQualification,
                    Sequence = 21,
                    QualificationKey = Rasa.Structures.Char.CharacterQualificationKey.BootcampComplete,
                    QualificationValue = Rasa.Structures.World.MissionScenarioStepEntry.RemovedQualificationValue,
                    Comment = "Clear the qualification"
                },
                new Rasa.Structures.World.MissionScenarioStepEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ScenarioId = 60,
                    StepId = 22,
                    Requirement = Rasa.Structures.World.MissionContentRequirement.Required,
                    Kind = Rasa.Structures.World.MissionScenarioStepKind.SetAccountSkipEntitlement,
                    Sequence = 22,
                    AccountSkipEntitlement = true,
                    Comment = "Grant the account skip entitlement"
                },
                new Rasa.Structures.World.MissionScenarioStepEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ScenarioId = 60,
                    StepId = 23,
                    Requirement = Rasa.Structures.World.MissionContentRequirement.Required,
                    Kind = Rasa.Structures.World.MissionScenarioStepKind.EscortSpawnGroup,
                    Sequence = 23,
                    SpawnGroupId = 50,
                    Comment = "Escort the spawned group"
                }});
        }

        private static void AddProgressTransition(
            MissionContentFixture fixture,
            uint objectiveId,
            uint transitionId,
            uint triggerId,
            uint subjectId)
        {
            fixture.Transitions.Add(new Rasa.Structures.World.MissionObjectiveTransitionEntry
            {
                MissionId = 321,
                ContentRevision = "deployment_11",
                ObjectiveId = objectiveId,
                TransitionId = transitionId,
                Requirement = Rasa.Structures.World.MissionContentRequirement.Required,
                Sequence = transitionId - 19,
                FromState = (byte)Rasa.Data.MissionObjectiveState.Incomplete,
                ToState = (byte)Rasa.Data.MissionObjectiveState.Completed,
                Comment = $"Progress transition {transitionId}"
            });
            fixture.Triggers.Add(new Rasa.Structures.World.MissionTriggerEntry
            {
                MissionId = 321,
                ContentRevision = "deployment_11",
                ObjectiveId = objectiveId,
                TransitionId = transitionId,
                TriggerId = triggerId,
                Requirement = Rasa.Structures.World.MissionContentRequirement.Required,
                Kind = Rasa.Structures.World.MissionTriggerKind.ProgressEvent,
                Sequence = 1,
                EventKind = (byte)Rasa.Data.MissionProgressEventKind.CreatureKilled,
                SubjectId = subjectId,
                Comment = $"Kill creature {subjectId}"
            });
        }

        private sealed class MissionContentLoadingFactory : IGameUnitOfWorkFactory
        {
            private readonly MissionTestContext _charFactory;
            private readonly IWorldUnitOfWork _worldUnit;

            internal MissionContentLoadingFactory(
                MissionTestContext charFactory,
                IWorldUnitOfWork worldUnit)
            {
                _charFactory = charFactory;
                _worldUnit = worldUnit;
            }

            public ICharUnitOfWork CreateChar() => _charFactory.CreateChar();
            public IWorldUnitOfWork CreateWorld() => _worldUnit;
        }
    }
}
