using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Missions.Content;
    using Rasa.Missions.Definitions;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Mission.Server;
    using Rasa.Structures;
    using Rasa.Structures.World;

    [TestClass]
    [DoNotParallelize]
    public class MissionAggregateProgressTests
    {
        [TestMethod]
        public void AuthoredCreatureSetBatchPersistsEveryEligibleSubjectOnce()
        {
            var fixture = CreateCreatureCounterFixture();
            var snapshot = new MissionContentLoader().Load(fixture.CreateRepository());
            using (var world = fixture.CreateWorldUnitOfWork())
            {
                var report = new MissionContentValidator().Validate(snapshot, world);
                Assert.IsFalse(report.BlocksReadiness,
                    string.Join(" | ", report.Diagnostics.Select(diagnostic => diagnostic.Code)));
            }

            var definition = snapshot.Definitions[321].Mission;
            Assert.IsTrue(definition.IsOperational, definition.OperationalDiagnostic);
            using var context = MissionTestContext.WithCustomDefinitions(
                new Dictionary<uint, Mission> { [321] = definition });
            var giver = context.AddNpc(101);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(
                context.Client, giver.EntityId, 321));
            context.Drain();

            var batch = new[]
            {
                MissionProgressEvent.Creature(501),
                MissionProgressEvent.Creature(502),
                MissionProgressEvent.Creature(501),
                MissionProgressEvent.Creature(599)
            };
            context.BeforeSave = _ =>
            {
                Assert.AreEqual(0U, context.Client.Player.Missions[321].Objectives[10].Counters[0]);
                Assert.AreEqual(MissionObjectiveState.Incomplete,
                    context.Client.Player.Missions[321].Objectives[10].State);
                Assert.IsFalse(context.Client.Player.Missions[321].Completeable);
                Assert.AreEqual(0, context.Drain().Count);
            };
            context.AfterSave = _ => throw new DbUpdateException("Injected creature-set progress failure.");

            Assert.ThrowsExactly<DbUpdateException>(() => CommitBatch(batch));

            context.AfterSave = null;
            Assert.AreEqual(0U, context.Client.Player.Missions[321].Objectives[10].Counters[0]);
            var rolledBack = context.ReadProgress(321).Missions[321].Objectives[10];
            Assert.AreEqual(0U, rolledBack.Counters[0]);
            Assert.AreEqual((byte)MissionObjectiveState.Incomplete, rolledBack.State);
            Assert.IsFalse(context.ReadMission(321).Completeable);
            Assert.AreEqual(0, context.Drain().Count);

            Assert.IsTrue(CommitBatch(batch));

            context.BeforeSave = null;
            Assert.AreEqual(2U, context.Client.Player.Missions[321].Objectives[10].Counters[0]);
            Assert.AreEqual(MissionObjectiveState.Incomplete,
                context.Client.Player.Missions[321].Objectives[10].State);
            Assert.IsFalse(context.Client.Player.Missions[321].Completeable);
            var partial = context.ReadProgress(321).Missions[321].Objectives[10];
            Assert.AreEqual(2U, partial.Counters[0]);
            Assert.AreEqual((byte)MissionObjectiveState.Incomplete, partial.State);
            Assert.IsFalse(context.ReadMission(321).Completeable);
            var partialPackets = context.Drain()
                .Where(packet => packet is not NPCConversationStatusPacket).ToArray();
            CollectionAssert.AreEqual(
                new[] { typeof(UpdateObjectiveCounterPacket) },
                partialPackets.Select(packet => packet.GetType()).ToArray());
            var partialCounter = partialPackets.OfType<UpdateObjectiveCounterPacket>().Single();
            Assert.AreEqual(321U, partialCounter.MissionId);
            Assert.AreEqual(10U, partialCounter.ObjectiveId);
            Assert.AreEqual(0U, partialCounter.CounterId);
            Assert.AreEqual(2U, partialCounter.CounterValue);
            Assert.AreEqual(0U, partialCounter.InitialValue);
            Assert.AreEqual(3U, partialCounter.TargetValue);

            context.ReloadPlayerMissions();
            Assert.AreEqual(2U, context.Client.Player.Missions[321].Objectives[10].Counters[0]);
            Assert.AreEqual(MissionObjectiveState.Incomplete,
                context.Client.Player.Missions[321].Objectives[10].State);
            Assert.IsFalse(context.Client.Player.Missions[321].Completeable);
            Assert.IsFalse(context.Manager.RecordProgress(
                context.Client, MissionProgressEvent.Creature(599)));
            Assert.AreEqual(0, context.Drain().Count);

            Assert.IsTrue(context.Manager.RecordProgress(
                context.Client, MissionProgressEvent.Creature(501)));
            Assert.IsFalse(context.Manager.RecordProgress(
                context.Client, MissionProgressEvent.Creature(502)));
            Assert.AreEqual(3U, context.Client.Player.Missions[321].Objectives[10].Counters[0]);
            Assert.AreEqual(MissionObjectiveState.Completed,
                context.Client.Player.Missions[321].Objectives[10].State);
            Assert.IsTrue(context.Client.Player.Missions[321].Completeable);

            var completedPackets = context.Drain()
                .Where(packet => packet is not NPCConversationStatusPacket).ToArray();
            CollectionAssert.AreEqual(
                new[]
                {
                    typeof(UpdateObjectiveCounterPacket),
                    typeof(ObjectiveCompletedPacket),
                    typeof(MissionCompleteablePacket)
                },
                completedPackets.Select(packet => packet.GetType()).ToArray());
            Assert.AreEqual(3U,
                completedPackets.OfType<UpdateObjectiveCounterPacket>().Single().CounterValue);
            var completed = context.ReadProgress(321).Missions[321].Objectives[10];
            Assert.AreEqual(3U, completed.Counters[0]);
            Assert.AreEqual((byte)MissionObjectiveState.Completed, completed.State);
            Assert.IsTrue(context.ReadMission(321).Completeable);

            context.ReloadPlayerMissions();
            Assert.AreEqual(3U, context.Client.Player.Missions[321].Objectives[10].Counters[0]);
            Assert.AreEqual(MissionObjectiveState.Completed,
                context.Client.Player.Missions[321].Objectives[10].State);
            Assert.IsTrue(context.Client.Player.Missions[321].Completeable);
            Assert.AreEqual(0, context.Drain().Count);

            bool CommitBatch(MissionProgressEvent[] events)
            {
                MissionProgressPublicationPlan publication = null;
                using (var unit = context.CreateChar())
                    unit.ExecuteTransaction(() =>
                        publication = context.Manager.PlanProgress(context.Client, events, unit));
                Assert.IsNotNull(publication);
                publication.Publish(context.Client);
                return publication.HasChanges;
            }
        }

        [TestMethod]
        public void ComposedHiddenChildrenCompleteNestedAggregatesAtomically()
        {
            const uint missionId = 65000;
            using var harness = BootcampRuntimeTestHarness.Create();
            InstallAggregateFixture(harness, missionId);
            harness.ReconnectFresh();
            var report = harness.Manager.LatestValidationReport;
            Assert.IsFalse(report.BlocksReadiness,
                string.Join(" | ", report.Diagnostics.Select(diagnostic => diagnostic.Code)));
            var giver = harness.AddNpc(BootcampRuntimeTestHarness.MajorMcAllisterCreatureId);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, giver.EntityId, missionId));
            var gained = harness.Drain().OfType<MissionGainedPacket>().Single();
            AssertNative(gained.MissionInfo, complete: false);
            AssertState(complete: false);

            harness.Context.BeforeSave = _ =>
            {
                var runtime = harness.Client.Player.Missions[missionId];
                Assert.IsTrue(runtime.Objectives.Values.All(objective =>
                    objective.State == MissionObjectiveState.Incomplete));
                Assert.AreEqual(0U, runtime.Objectives[3].Counters[0]);
                Assert.IsFalse(runtime.Completeable);
                Assert.AreEqual(0, harness.Drain().Count);
            };
            harness.Context.AfterSave = _ => throw new DbUpdateException("Injected aggregate progress failure.");

            Assert.ThrowsExactly<DbUpdateException>(() => CompleteChildren());

            harness.Context.AfterSave = null;
            AssertState(complete: false);
            Assert.AreEqual(0, harness.Drain().Count);

            CompleteChildren();

            harness.Context.BeforeSave = null;
            AssertState(complete: true);
            var packets = harness.Drain()
                .Where(packet => packet is UpdateObjectiveCounterPacket or
                    ObjectiveCompletedPacket or MissionCompleteablePacket).ToArray();
            CollectionAssert.AreEqual(
                new[]
                {
                    typeof(UpdateObjectiveCounterPacket),
                    typeof(ObjectiveCompletedPacket),
                    typeof(ObjectiveCompletedPacket),
                    typeof(MissionCompleteablePacket)
                },
                packets.Select(packet => packet.GetType()).ToArray());
            var counter = packets.OfType<UpdateObjectiveCounterPacket>().Single();
            Assert.AreEqual(missionId, counter.MissionId);
            Assert.AreEqual(3U, counter.ObjectiveId);
            Assert.AreEqual(0U, counter.CounterId);
            Assert.AreEqual(2U, counter.CounterValue);
            Assert.AreEqual(0U, counter.InitialValue);
            Assert.AreEqual(2U, counter.TargetValue);
            CollectionAssert.AreEqual(new[] { 3U, 4U },
                packets.OfType<ObjectiveCompletedPacket>().Select(packet => packet.ObjectiveId).ToArray());
            Assert.AreEqual(missionId, packets.OfType<MissionCompleteablePacket>().Single().MissionId);
            Assert.IsFalse(harness.Manager.RecordProgress(harness.Client, MissionProgressEvent.Creature(39)));
            Assert.IsFalse(harness.Manager.RecordProgress(harness.Client, MissionProgressEvent.Creature(50)));
            Assert.AreEqual(0, harness.Drain().Count);

            harness.ReconnectFresh(drainPackets: false);
            harness.Manager.PublishInitialState(harness.Client);

            AssertState(complete: true);
            var restoredPackets = harness.Drain();
            var restored = restoredPackets.OfType<MissionStatusInfoPacket>().Single();
            AssertNative(restored.MissionStatusDict[missionId], complete: true);
            Assert.AreEqual(0, restoredPackets.OfType<UpdateObjectiveCounterPacket>().Count());
            Assert.AreEqual(0, restoredPackets.OfType<ObjectiveCompletedPacket>().Count());

            void CompleteChildren()
            {
                using var publication = new MissionScenarioPlan();
                using (var unit = harness.Context.CreateChar())
                    unit.ExecuteTransaction(() =>
                    {
                        publication.AddProgressPlan(harness.Manager.PlanProgress(harness.Client,
                            new[] { MissionProgressEvent.Creature(39) }, unit));
                        publication.AddProgressPlan(harness.Manager.PlanProgress(harness.Client,
                            new[] { MissionProgressEvent.Creature(50) }, unit));
                    });
                Assert.IsTrue(publication.HasChanges);
                publication.ApplyRuntime(harness.Client, new ManifestationManager(harness.Context), harness.Manager);
            }

            void AssertState(bool complete)
            {
                var state = complete ? MissionObjectiveState.Completed : MissionObjectiveState.Incomplete;
                var runtime = harness.Client.Player.Missions[missionId];
                var durable = harness.Context.ReadProgress(missionId).Missions[missionId];
                CollectionAssert.AreEquivalent(new[] { 1U, 2U, 3U, 4U }, runtime.Objectives.Keys.ToArray());
                CollectionAssert.AreEquivalent(new[] { 1U, 2U, 3U, 4U }, durable.Objectives.Keys.ToArray());
                foreach (var id in new[] { 1U, 2U, 3U, 4U })
                {
                    Assert.AreEqual(state, runtime.Objectives[id].State);
                    Assert.AreEqual((byte)state, durable.Objectives[id].State);
                }
                foreach (var id in new[] { 1U, 2U })
                {
                    Assert.AreEqual(complete ? 1U : 0U, runtime.Objectives[id].Counters[0]);
                    Assert.AreEqual(complete ? 1U : 0U, durable.Objectives[id].Counters[0]);
                }
                Assert.AreEqual(complete ? 2U : 0U, runtime.Objectives[3].Counters[0]);
                Assert.AreEqual(complete ? 2U : 0U, durable.Objectives[3].Counters[0]);
                Assert.AreEqual(0, runtime.Objectives[4].Counters.Count);
                Assert.AreEqual(0, durable.Objectives[4].Counters.Count);
                Assert.AreEqual(complete, runtime.Completeable);
                Assert.AreEqual(complete, harness.Context.ReadMission(missionId).Completeable);
                AssertNative(harness.Manager.BuildStatusSnapshot(harness.Client.Player)[missionId], complete);
            }

            static void AssertNative(MissionInfo info, bool complete)
            {
                CollectionAssert.AreEqual(new[] { 3U, 4U },
                    info.ObjectivesList.Select(objective => objective.ObjectiveId).ToArray());
                Assert.IsTrue(info.ObjectivesList.All(objective => objective.State ==
                    (complete ? MissionObjectiveState.Completed : MissionObjectiveState.Incomplete)));
                Assert.AreEqual(complete ? 2U : 0U, info.ObjectivesList[0].Counters[0].CounterValue);
                Assert.AreEqual(0, info.ObjectivesList[1].Counters.Count);
                Assert.AreEqual(complete, info.Completeable);
            }
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void AggregateCountsCompletedChildrenAndKeepsHiddenFailuresOffTheWire(bool sceneCompletion)
        {
            var subjects = new List<uint> { 1, 2, 3 };
            var aggregation = new MissionObjectiveAggregation(subjects, 2, 0);
            subjects[0] = 999;
            var definition = new Mission(321, "Aggregate threshold", 1000, 77, 88, 1, 1, 1, false, false,
                new[]
                {
                    Child(1, 75), Child(2, 76), Child(3, 77),
                    new MissionObjectiveDefinition(4, 1004, 2004, new uint?[] { 9004 }, 4,
                        MissionObjectiveState.Incomplete, true, null, null,
                        Array.Empty<MissionObjectiveConversation>(), Array.Empty<uint>(), Array.Empty<uint>(),
                        Array.Empty<MissionIndicator>(), aggregation: aggregation)
                }, true);
            Assert.IsTrue(definition.IsOperational, definition.OperationalDiagnostic);
            using var context = MissionTestContext.WithCustomDefinitions(new Dictionary<uint, Mission> { [321] = definition });
            var giver = context.AddNpc(77);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
            CollectionAssert.AreEqual(new[] { 4U }, context.Drain().OfType<MissionGainedPacket>().Single()
                .MissionInfo.ObjectivesList.Select(objective => objective.ObjectiveId).ToArray());

            Assert.IsTrue(context.Manager.RecordProgress(context.Client, MissionProgressEvent.Creature(75)));
            Assert.AreEqual(1U, context.ReadProgress(321).Missions[321].Objectives[4].Counters[0]);
            var partial = context.Drain().Where(packet => packet is not NPCConversationStatusPacket).ToArray();
            Assert.AreEqual(1, partial.Length);
            Assert.AreEqual(1U, ((UpdateObjectiveCounterPacket)partial[0]).CounterValue);

            Assert.IsTrue(context.Manager.TryFailObjective(context.Client, 321, 2));
            Assert.AreEqual(MissionObjectiveState.Failed, context.Client.Player.Missions[321].Objectives[2].State);
            Assert.AreEqual(1U, context.Client.Player.Missions[321].Objectives[4].Counters[0]);
            Assert.AreEqual(0, context.Drain().Count(packet => packet is not NPCConversationStatusPacket));

            if (sceneCompletion)
            {
                using var publication = new MissionScenarioPlan();
                using (var unit = context.CreateChar())
                    unit.ExecuteTransaction(() => Assert.IsTrue(context.Manager.TryPlanScenarioObjectiveAction(
                        context.Client, 321, 3, MissionScenarioStepKind.CompleteObjective, unit, publication)));
                publication.ApplyRuntime(context.Client, new ManifestationManager(context), context.Manager);
            }
            else
                Assert.IsTrue(context.Manager.RecordProgress(context.Client, MissionProgressEvent.Creature(77)));

            var completed = context.Drain().Where(packet => packet is not NPCConversationStatusPacket).ToArray();
            CollectionAssert.AreEqual(new[]
            {
                typeof(UpdateObjectiveCounterPacket), typeof(ObjectiveCompletedPacket), typeof(MissionCompleteablePacket)
            }, completed.Select(packet => packet.GetType()).ToArray());
            Assert.AreEqual(2U, completed.OfType<UpdateObjectiveCounterPacket>().Single().CounterValue);
            Assert.AreEqual(4U, completed.OfType<ObjectiveCompletedPacket>().Single().ObjectiveId);
            Assert.IsTrue(context.Client.Player.Missions[321].Completeable);
            context.ReloadPlayerMissions();
            Assert.IsTrue(context.Client.Player.Missions[321].Completeable);
            Assert.AreEqual(MissionObjectiveState.Failed, context.Client.Player.Missions[321].Objectives[2].State);
            Assert.AreEqual(2U, context.Client.Player.Missions[321].Objectives[4].Counters[0]);

            static MissionObjectiveDefinition Child(uint id, uint creature) =>
                new(id, null, null, Array.Empty<uint?>(), id, MissionObjectiveState.Incomplete, false,
                    null, null, Array.Empty<MissionObjectiveConversation>(), Array.Empty<uint>(), Array.Empty<uint>(),
                    Array.Empty<MissionIndicator>(), MissionProgressRule.CompleteOnExactSubject(
                        MissionProgressEventKind.CreatureKilled, creature), isVisible: false);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void InvalidAggregateContractsCannotBeAccepted(bool mixedProgress)
        {
            var definition = new Mission(321, "Invalid aggregate", 1000, 77, 88, 1, 1, 1, false, false,
                new[]
                {
                    Objective(1, new MissionObjectiveAggregation(new[] { 2U }, 1),
                        mixedProgress ? MissionProgressRule.CompleteOnExactSubject(MissionProgressEventKind.CreatureKilled, 75) : null),
                    Objective(2, mixedProgress ? null : new MissionObjectiveAggregation(new[] { 1U }, 1),
                        mixedProgress ? MissionProgressRule.CompleteOnExactSubject(MissionProgressEventKind.CreatureKilled, 76) : null)
                }, true);
            using var context = MissionTestContext.WithCustomDefinitions(new Dictionary<uint, Mission> { [321] = definition });
            var giver = context.AddNpc(77);
            context.Drain();

            Assert.IsFalse(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));

            Assert.AreEqual(0, context.MissionCount(context.Client.Player.Id));
            Assert.AreEqual(0, context.Client.Player.Missions.Count);
            Assert.AreEqual(0, context.Drain().OfType<MissionGainedPacket>().Count());

            static MissionObjectiveDefinition Objective(uint id, MissionObjectiveAggregation aggregation, MissionProgressRule rule) =>
                new(id, 1000 + id, 2000 + id, Array.Empty<uint?>(), id, MissionObjectiveState.Incomplete, true,
                    null, null, Array.Empty<MissionObjectiveConversation>(), Array.Empty<uint>(), Array.Empty<uint>(),
                    Array.Empty<MissionIndicator>(), rule, aggregation: aggregation);
        }

        private static void InstallAggregateFixture(BootcampRuntimeTestHarness.Harness harness, uint missionId)
        {
            const string revision = "aggregate-test";
            var world = harness.WorldContext;
            world.Add(new MissionContentDefinitionEntry
            {
                MissionId = missionId, ContentRevision = revision, Enabled = true,
                Requirement = MissionContentRequirement.Required,
                AbandonmentPolicy = MissionAbandonmentPolicy.Allowed,
                ClientNameTextId = 1000,
                GiverId = BootcampRuntimeTestHarness.MajorMcAllisterCreatureId,
                ReceiverId = BootcampRuntimeTestHarness.MajorMcAllisterCreatureId,
                Level = 1, GroupType = 1, CategoryId = 1, Shareable = false,
                Comment = "Isolated local aggregate fixture"
            });
            foreach (var id in new[] { 1U, 2U, 3U, 4U })
            {
                world.Add(new MissionObjectiveDefinitionEntry
                {
                    MissionId = missionId, ContentRevision = revision, ObjectiveId = id,
                    Requirement = MissionContentRequirement.Required,
                    ClientNameTextId = 1000 + id, ClientBodyTextId = 2000 + id,
                    ClientCounter0TextId = id == 4 ? null : 9000U + id,
                    Ordinal = id, InitialState = (byte)MissionObjectiveState.Incomplete,
                    IsRequired = id == 4, Comment = "Local child or summary"
                });
                if (id > 2)
                    continue;
                world.Add(new MissionObjectiveTransitionEntry
                {
                    MissionId = missionId, ContentRevision = revision, ObjectiveId = id,
                    TransitionId = 1, Sequence = 1, Requirement = MissionContentRequirement.Required,
                    FromState = (byte)MissionObjectiveState.Incomplete,
                    ToState = (byte)MissionObjectiveState.Completed, Comment = "Complete one named child"
                });
                world.Add(new MissionTriggerEntry
                {
                    MissionId = missionId, ContentRevision = revision, ObjectiveId = id,
                    TransitionId = 1, TriggerId = 1, Sequence = 1,
                    Requirement = MissionContentRequirement.Required,
                    Kind = MissionTriggerKind.ProgressEvent,
                    EventKind = (byte)MissionProgressEventKind.CreatureKilled,
                    SubjectId = id == 1 ? 39U : 50U,
                    CounterId = 0, InitialValue = 0, TargetValue = 1,
                    Comment = "Named child with a native bookkeeping counter"
                });
            }
            var bindings = JsonSerializer.SerializeToNode(
                new MissionSceneDefinition(), MissionContentCodec.Options).AsObject();
            bindings["objectiveAggregations"] = new JsonObject
            {
                ["3"] = new JsonObject
                {
                    ["childObjectiveIds"] = new JsonArray(1, 2),
                    ["targetCount"] = 2,
                    ["counterId"] = 0
                },
                ["4"] = new JsonObject
                {
                    ["childObjectiveIds"] = new JsonArray(3),
                    ["targetCount"] = 1
                }
            };
            bindings["hiddenObjectiveIds"] = new JsonArray(1, 2);
            world.Add(new MissionSceneBindingEntry
            {
                MissionId = missionId, ContentRevision = revision,
                Bindings = bindings.ToJsonString(MissionContentCodec.Options)
            });
            world.SaveChanges();
            world.ChangeTracker.Clear();
        }

        private static MissionContentFixture CreateCreatureCounterFixture()
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
            fixture.Objectives.Single().ClientCounter0TextId = 9000;
            fixture.Objectives.Single().Comment = "Defeat three eligible creatures";
            fixture.Transitions.Single().Comment = "Complete the creature counter";
            fixture.CreatureClasses.Add(502, 4001);
            uint sequence = 1;
            foreach (var subjectId in new[] { 501U, 502U })
            {
                fixture.Triggers.Add(new MissionTriggerEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ObjectiveId = 10,
                    TransitionId = 20,
                    TriggerId = sequence,
                    Requirement = MissionContentRequirement.Required,
                    Kind = MissionTriggerKind.ProgressEvent,
                    Sequence = sequence,
                    EventKind = (byte)MissionProgressEventKind.CreatureKilled,
                    SubjectId = subjectId,
                    CounterId = 0,
                    InitialValue = 0,
                    TargetValue = 3,
                    Comment = "Count an eligible creature"
                });
                sequence++;
            }
            return fixture;
        }
    }
}
