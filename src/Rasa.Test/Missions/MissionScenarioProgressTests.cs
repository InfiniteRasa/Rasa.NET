using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Packets.Mission.Server;
    using Rasa.Repositories.Char;
    using Rasa.Repositories.UnitOfWork;
    using Rasa.Repositories.World;
    using Rasa.Structures;
    using Rasa.Structures.World;

    [TestClass]
    [DoNotParallelize]
    public class MissionScenarioProgressTests
    {
        [TestMethod]
        public void DurableScenarioStepCommitsBeforeScenarioProgressAndDoesNotReplay()
        {
            using var context = MissionTestContext.WithCustomDefinitions(
                new Dictionary<uint, Mission>());
            var fixture = CreateScenarioFixture();
            var manager = LoadManager(context, fixture);
            var giver = context.AddNpc(101);

            Assert.IsTrue(manager.AcceptOfferedMission(
                context.Client,
                giver.EntityId,
                321));

            Assert.IsFalse(manager.TryRecordScenarioEvent(
                context.Client,
                321,
                60,
                999));
            Assert.IsTrue(manager.TryRecordScenarioEvent(
                context.Client,
                321,
                60,
                1));
            Assert.IsFalse(manager.TryRecordScenarioEvent(
                context.Client,
                321,
                60,
                1));

            Assert.AreEqual(
                MissionObjectiveState.Completed,
                context.Client.Player.Missions[321].Objectives[10].State);
            Assert.AreEqual(
                1,
                context.Drain().OfType<ObjectiveCompletedPacket>().Count());
            using var unit = context.CreateChar();
            CollectionAssert.AreEqual(
                new[] { "scenario:60:step:1" },
                unit.CharacterMissionScenario.Get(1, 321)
                    .Select(entry => entry.StepKey)
                    .ToArray());
        }

        [TestMethod]
        public void ConcurrentScenarioEventsCommitOneDurableStep()
        {
            using var context = MissionTestContext.WithCustomDefinitions(
                new Dictionary<uint, Mission>());
            var fixture = CreateScenarioFixture();
            var manager = LoadManager(context, fixture);
            var giver = context.AddNpc(101);

            Assert.IsTrue(manager.AcceptOfferedMission(
                context.Client,
                giver.EntityId,
                321));
            var competitor = context.CreateCompetingClient(manager);
            context.Drain();

            using var start = new ManualResetEventSlim();
            var results = Task.WhenAll(
                Task.Run(() =>
                {
                    start.Wait();
                    return manager.TryRecordScenarioEvent(
                        context.Client,
                        321,
                        60,
                        1);
                }),
                Task.Run(() =>
                {
                    start.Wait();
                    return manager.TryRecordScenarioEvent(
                        competitor,
                        321,
                        60,
                        1);
                }));
            start.Set();

            Assert.AreEqual(1, results.GetAwaiter().GetResult().Count(result => result));
            Assert.AreEqual(
                1,
                context.Drain()
                    .Concat(MissionTestContext.Drain(competitor))
                    .OfType<ObjectiveCompletedPacket>()
                    .Count());
        }

        [TestMethod]
        public void ScenarioStepEmissionAdvancesMatchingObjectiveOnceWithoutRecordingEventAsSecondStep()
        {
            using var context = MissionTestContext.WithCustomDefinitions(
                new Dictionary<uint, Mission>());
            var fixture = CreateScenarioFixture(stepId: 10, scenarioEventId: 501);
            var manager = LoadManager(context, fixture);
            var giver = context.AddNpc(101);

            Assert.IsTrue(manager.AcceptOfferedMission(
                context.Client,
                giver.EntityId,
                321));
            context.Drain();

            Assert.IsTrue(manager.TryExecuteScenario(context.Client, 321, 60));
            Assert.IsFalse(manager.TryExecuteScenario(context.Client, 321, 60));

            Assert.AreEqual(
                MissionObjectiveState.Completed,
                context.Client.Player.Missions[321].Objectives[10].State);
            Assert.AreEqual(
                1,
                context.Drain().OfType<ObjectiveCompletedPacket>().Count());

            using var unit = context.CreateChar();
            var scene = unit.CharacterMissions.Runtime.Scenes(1, 321).Single();
            Assert.AreEqual(1, unit.CharacterMissions.Runtime.Messages(scene.RunId).Count(message => message.Status == "Handled"));
        }

        [TestMethod]
        public void ProgressCompletionStartsAuthoredScenarioOncePostCommit()
        {
            using var context = MissionTestContext.WithCustomDefinitions(
                new Dictionary<uint, Mission>());
            var fixture = CreateProgressStartScenarioFixture();
            var manager = LoadManager(context, fixture);
            var giver = context.AddNpc(101);

            Assert.IsTrue(manager.AcceptOfferedMission(
                context.Client,
                giver.EntityId,
                321));
            context.Drain();

            Assert.IsTrue(manager.RecordProgress(
                context.Client,
                MissionProgressEvent.Interaction(3147)));
            Assert.IsFalse(manager.RecordProgress(
                context.Client,
                MissionProgressEvent.Interaction(3147)));

            Assert.AreEqual(
                MissionObjectiveState.Completed,
                context.Client.Player.Missions[321].Objectives[10].State);
            Assert.AreEqual(
                MissionObjectiveState.Incomplete,
                context.Client.Player.Missions[321].Objectives[11].State);
            using var unit = context.CreateChar();
            var scene = unit.CharacterMissions.Runtime.Scenes(1, 321).Single();
            Assert.AreEqual(1, unit.CharacterMissions.Runtime.Messages(scene.RunId).Count(message => message.Status == "Handled"));
        }

        private static MissionContentFixture CreateScenarioFixture(
            uint stepId = 1,
            uint scenarioEventId = 1)
        {
            var fixture = MissionContentFixture.CreateValid();
            fixture.Triggers.Clear();
            fixture.Actions.Clear();
            fixture.Transitions.Clear();
            fixture.Rewards.Clear();
            fixture.RewardItems.Clear();
            fixture.Indicators.Clear();
            fixture.Objectives.RemoveAll(objective => objective.ObjectiveId == 11);
            fixture.ScenarioSteps.Clear();
            fixture.ScenarioSteps.Add(new MissionScenarioStepEntry
            {
                MissionId = 321,
                ContentRevision = "deployment_11",
                ScenarioId = 60,
                StepId = stepId,
                Requirement = MissionContentRequirement.Required,
                Kind = MissionScenarioStepKind.EmitScenarioEvent,
                Sequence = 1,
                ScenarioEventId = scenarioEventId,
                Comment = "Emit scenario event"
            });
            fixture.Transitions.Add(new MissionObjectiveTransitionEntry
            {
                MissionId = 321,
                ContentRevision = "deployment_11",
                ObjectiveId = 10,
                TransitionId = 20,
                Requirement = MissionContentRequirement.Required,
                Sequence = 1,
                FromState = (byte)MissionObjectiveState.Incomplete,
                ToState = (byte)MissionObjectiveState.Completed,
                Comment = "Scenario step"
            });
            fixture.Triggers.Add(new MissionTriggerEntry
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
                SubjectId = scenarioEventId,
                CounterId = 60,
                Comment = $"Scenario 60 event {scenarioEventId}"
            });
            return fixture;
        }

        private static MissionContentFixture CreateProgressStartScenarioFixture()
        {
            var fixture = MissionContentFixture.CreateValid();
            fixture.Triggers.Clear();
            fixture.Actions.Clear();
            fixture.Transitions.Clear();
            fixture.Rewards.Clear();
            fixture.RewardItems.Clear();
            fixture.Indicators.Clear();
            fixture.ScenarioSteps.Clear();
            fixture.Transitions.Add(new MissionObjectiveTransitionEntry
            {
                MissionId = 321,
                ContentRevision = "deployment_11",
                ObjectiveId = 10,
                TransitionId = 20,
                Requirement = MissionContentRequirement.Required,
                Sequence = 1,
                FromState = (byte)MissionObjectiveState.Incomplete,
                ToState = (byte)MissionObjectiveState.Completed,
                Comment = "Use interaction"
            });
            fixture.Triggers.Add(new MissionTriggerEntry
            {
                MissionId = 321,
                ContentRevision = "deployment_11",
                ObjectiveId = 10,
                TransitionId = 20,
                TriggerId = 1,
                Requirement = MissionContentRequirement.Required,
                Kind = MissionTriggerKind.ProgressEvent,
                Sequence = 1,
                EventKind = (byte)MissionProgressEventKind.InteractionUsed,
                SubjectId = 3147,
                Comment = "Use interaction"
            });
            fixture.Actions.Add(new MissionActionEntry
            {
                MissionId = 321,
                ContentRevision = "deployment_11",
                ObjectiveId = 10,
                TransitionId = 20,
                ActionId = 1,
                Requirement = MissionContentRequirement.Required,
                Kind = MissionActionKind.StartScenario,
                Sequence = 1,
                ScenarioId = 60,
                Comment = "Start follow-up scenario"
            });
            fixture.ScenarioSteps.AddRange(
                new MissionScenarioStepEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ScenarioId = 60,
                    StepId = 1,
                    Requirement = MissionContentRequirement.Required,
                    Kind = MissionScenarioStepKind.RevealObjective,
                    Sequence = 1,
                    TargetObjectiveId = 11,
                    Comment = "Reveal objective 11"
                },
                new MissionScenarioStepEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ScenarioId = 60,
                    StepId = 2,
                    Requirement = MissionContentRequirement.Required,
                    Kind = MissionScenarioStepKind.ActivateObjective,
                    Sequence = 2,
                    TargetObjectiveId = 11,
                    Comment = "Activate objective 11"
                });
            return fixture;
        }

        private static MissionApplication LoadManager(
            MissionTestContext context,
            MissionContentFixture fixture)
        {
            var manager = new MissionApplication(
                new MissionContentLoadingFactory(context, fixture.CreateWorldUnitOfWork()),
                new Dictionary<uint, Mission>());
            var report = manager.LoadMissions();
            Assert.IsFalse(
                report.BlocksReadiness,
                string.Join(" | ", report.Diagnostics.Select(diagnostic => diagnostic.Code)));
            Content.SceneFixtureBindings.Bind(manager, fixture);
            return manager;
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
