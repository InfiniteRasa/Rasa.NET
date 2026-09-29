using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Missions.Scenes;
using Rasa.Packets.Mission.Server;
using Rasa.Structures;
using Rasa.Structures.World;

namespace Rasa.Test.Missions
{
    [TestClass]
    [DoNotParallelize]
    public class MissionScopedSceneProgressTests
    {
        [TestMethod]
        public void DistinctSceneSignalsPublishAndPersistTheNativeCounterWithoutDuplicateCredit()
        {
            var fixture = CreateFixture();
            var definition = new MissionContentLoader().Load(fixture.CreateRepository()).Definitions[321].Mission;
            Assert.IsTrue(definition.IsOperational, definition.OperationalDiagnostic);
            using var context = MissionTestContext.WithCustomDefinitions(
                new Dictionary<uint, Mission> { [321] = definition });
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, context.AddNpc(101).EntityId, 321));
            context.Drain();

            Assert.IsFalse(context.Manager.RecordProgress(context.Client, MissionProgressEvent.Scenario(322, 9, 1)));
            Assert.IsFalse(context.Manager.RecordProgress(context.Client, MissionProgressEvent.Scenario(321, 8, 1)));
            Assert.IsFalse(context.Manager.RecordProgress(context.Client, MissionProgressEvent.Scenario(321, 9, 4)));
            Assert.IsTrue(context.Manager.RecordProgress(context.Client, MissionProgressEvent.Scenario(321, 9, 1)));
            Assert.IsFalse(context.Manager.RecordProgress(context.Client, MissionProgressEvent.Scenario(321, 9, 1)));
            var partial = context.Drain().OfType<UpdateObjectiveCounterPacket>().Single();
            Assert.AreEqual(0U, partial.CounterId);
            Assert.AreEqual(1U, partial.CounterValue);
            Assert.AreEqual(3U, partial.TargetValue);
            Assert.AreEqual(MissionObjectiveState.Incomplete, context.Client.Player.Missions[321].Objectives[10].State);

            context.AfterSave = _ => throw new DbUpdateException("Injected scene counter rollback.");
            Assert.ThrowsExactly<DbUpdateException>(() => CommitSecond());
            context.AfterSave = null;
            Assert.AreEqual(1U, context.ReadProgress(321).Missions[321].Objectives[10].Counters[0]);
            Assert.AreEqual(1U, context.Client.Player.Missions[321].Objectives[10].Counters[0]);
            Assert.AreEqual(0, context.Drain().Count);

            CommitSecond();
            context.ReloadPlayerMissions();
            Assert.AreEqual(2U, context.Client.Player.Missions[321].Objectives[10].Counters[0]);
            Assert.IsFalse(context.Manager.RecordProgress(context.Client, MissionProgressEvent.Scenario(321, 9, 2)));
            Assert.IsTrue(context.Manager.RecordProgress(context.Client, MissionProgressEvent.Scenario(321, 9, 3)));
            Assert.AreEqual(3U, context.ReadProgress(321).Missions[321].Objectives[10].Counters[0]);
            Assert.AreEqual(MissionObjectiveState.Completed, context.Client.Player.Missions[321].Objectives[10].State);
            Assert.IsTrue(context.Client.Player.Missions[321].Completeable);

            void CommitSecond()
            {
                MissionProgressPublicationPlan publication = null;
                using (var unit = context.CreateChar())
                    unit.ExecuteTransaction(() => publication = context.Manager.PlanProgress(context.Client,
                        new[] { MissionProgressEvent.Scenario(321, 9, 2) }, unit));
                publication.Publish(context.Client);
            }
        }

        [TestMethod]
        public void OneSceneDecisionBatchesDistinctCounterSignalsAtomically()
        {
            var fixture = CreateFixture();
            var definition = new MissionContentLoader().Load(fixture.CreateRepository()).Definitions[321].Mission;
            using var context = MissionTestContext.WithCustomDefinitions(
                new Dictionary<uint, Mission> { [321] = definition });
            context.Manager.Scenes.Bind(321, "data.sequence", new SceneBindings("deployment_11",
                new Dictionary<string, SceneActorDefinition>(), new Dictionary<string, SceneRoute>(),
                new Dictionary<uint, SceneSequence>
                {
                    [0] = new(),
                    [1] = new(signals: new[] { new SceneMissionSignal(321, 9, 1), new SceneMissionSignal(321, 9, 2) }),
                    [2] = new(signals: new[] { new SceneMissionSignal(321, 9, 3) })
                }));
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, context.AddNpc(101).EntityId, 321));
            context.Drain();
            context.AfterSave = _ => throw new DbUpdateException("Injected scene batch rollback.");
            Assert.IsFalse(context.Manager.Scenes.Submit(
                context.Client.Player.Missions[321].AssignmentId, new SceneObservation(SceneEventKind.Signal, 1)),
                "An assignment ID is not scene authority.");
            string runId;
            using (var unit = context.CreateChar())
                runId = unit.CharacterMissions.Runtime.AssignmentScene(context.ReadMission(321).AssignmentId).RunId;
            Assert.IsFalse(context.Manager.Scenes.Submit(runId,
                new SceneObservation(SceneEventKind.Signal, 1, "batch", SequenceId: 1)));
            context.AfterSave = null;
            Assert.AreEqual(0U, context.ReadProgress(321).Missions[321].Objectives[10].Counters[0]);
            Assert.AreEqual(0, context.Drain().OfType<UpdateObjectiveCounterPacket>().Count());

            Assert.IsTrue(context.Manager.Scenes.Submit(runId,
                new SceneObservation(SceneEventKind.Signal, 1, "batch", SequenceId: 1)));

            Assert.AreEqual(2U, context.ReadProgress(321).Missions[321].Objectives[10].Counters[0]);
            Assert.AreEqual(2U, context.Drain().OfType<UpdateObjectiveCounterPacket>().Single().CounterValue);
            context.ReloadPlayerMissions();
            Assert.IsTrue(context.Manager.Scenes.Submit(runId,
                new SceneObservation(SceneEventKind.Signal, 1, "replay", SequenceId: 1)));
            Assert.AreEqual(2U, context.Client.Player.Missions[321].Objectives[10].Counters[0]);
            Assert.IsTrue(context.Manager.Scenes.Submit(runId,
                new SceneObservation(SceneEventKind.Signal, 1, "last", SequenceId: 2)));
            Assert.AreEqual(MissionObjectiveState.Completed, context.Client.Player.Missions[321].Objectives[10].State);
        }

        [TestMethod]
        [DataRow("scope")]
        [DataRow("initial")]
        [DataRow("target")]
        [DataRow("duplicate")]
        [DataRow("gap")]
        [DataRow("out-of-range")]
        public void AmbiguousScopedSceneCounterSetsAreNotOperational(string invalid)
        {
            var fixture = CreateFixture();
            if (invalid == "scope") fixture.Triggers[1].CounterId = 8;
            if (invalid == "initial") fixture.Triggers[1].InitialValue = 1;
            if (invalid == "target") fixture.Triggers[1].TargetValue = 4;
            if (invalid == "duplicate") fixture.Triggers[1].SubjectId = 1;
            if (invalid == "gap") fixture.Triggers[2].SubjectId = 4;
            if (invalid == "out-of-range") fixture.Triggers[2].SubjectId = uint.MaxValue;

            var definition = new MissionContentLoader().Load(fixture.CreateRepository()).Definitions[321].Mission;

            Assert.IsFalse(definition.IsOperational);
        }

        private static MissionContentFixture CreateFixture()
        {
            var fixture = MissionContentFixture.CreateValid();
            fixture.Triggers.Clear();
            fixture.Actions.Clear();
            fixture.Rewards.Clear();
            fixture.RewardItems.Clear();
            fixture.Indicators.Clear();
            fixture.Areas.Clear();
            fixture.SpawnGroups.Clear();
            fixture.Spawns.Clear();
            fixture.Scenarios.Clear();
            fixture.ScenarioSteps.Clear();
            fixture.Objectives.RemoveAll(objective => objective.ObjectiveId == 11);
            fixture.Objectives.Single().ClientCounter0TextId = 2663;
            for (uint subject = 1; subject <= 3; subject++)
                fixture.Triggers.Add(new MissionTriggerEntry
                {
                    MissionId = 321, ContentRevision = "deployment_11", ObjectiveId = 10, TransitionId = 20,
                    TriggerId = subject, Sequence = subject, Requirement = MissionContentRequirement.Required,
                    Kind = MissionTriggerKind.ProgressEvent, EventKind = (byte)MissionProgressEventKind.ScenarioEvent,
                    SubjectId = subject, CounterId = 9, InitialValue = 0, TargetValue = 3,
                    Comment = "One distinct, assignment-scoped source"
                });
            return fixture;
        }
    }
}
