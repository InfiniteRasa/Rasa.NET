using System;
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
    using Rasa.Packets.ClientMethod.Server;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Mission.Server;
    using Rasa.Structures;
    using Rasa.Structures.Char;
    using Rasa.Structures.Missions;
    using Rasa.Structures.World;

    [TestClass]
    [DoNotParallelize]
    public class MissionExistingFactProgressTests
    {
        private const uint FactMissionId = 65001;
        private const uint SuccessfulHistoryId = 65002;
        private const uint FailedHistoryId = 65003;

        [TestMethod]
        public void ExistingFactOptInRejectsHistoricalKillsAtStartup()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            InstallExistingFactFixture(harness, invalidKillOptIn: true);
            var error = Assert.ThrowsExactly<InvalidOperationException>(() => harness.ReconnectFresh());
            StringAssert.Contains(error.Message, "invalid-objective-progress-metadata");
        }

        [TestMethod]
        public void AcceptanceRecognizesOnlyOptedInDurableFactsAndRollsBackAtomically()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            InstallExistingFactFixture(harness);
            harness.ReconnectFresh();
            SeedExistingFacts(harness);
            var giver = harness.AddNpc(BootcampRuntimeTestHarness.MajorMcAllisterCreatureId);
            harness.Drain();
            harness.Context.BeforeSave = database =>
            {
                if (database.ChangeTracker.Entries<CharacterMissionEntry>()
                    .Any(entry => entry.Entity.MissionId == FactMissionId))
                    Assert.IsFalse(harness.Client.Player.Missions.ContainsKey(FactMissionId));
            };
            harness.Context.AfterSave = _ => throw new DbUpdateException("Injected existing-fact acceptance failure.");

            Assert.IsFalse(harness.Manager.AcceptOfferedMission(harness.Client, giver.EntityId, FactMissionId));

            harness.Context.AfterSave = null;
            Assert.IsFalse(harness.Client.Player.Missions.ContainsKey(FactMissionId));
            using (var unit = harness.Context.CreateChar())
            {
                Assert.IsNull(unit.CharacterMissions.GetByCharacterAndMission(harness.Client.Player.Id, FactMissionId));
                Assert.IsFalse(unit.CharacterMissionProgress.Get(harness.Client.Player.Id).Missions.ContainsKey(FactMissionId));
            }
            Assert.AreEqual(0, harness.Drain().Count(packet =>
                packet is MissionGainedPacket or UpdateObjectiveCounterPacket or ObjectiveCompletedPacket));

            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, giver.EntityId, FactMissionId));

            harness.Context.BeforeSave = null;
            AssertExistingFactState(harness, recovered: false, killCount: 2);
            var packets = harness.Drain();
            var gained = packets.OfType<MissionGainedPacket>().Single();
            AssertExistingFactInfo(gained.MissionInfo, recovered: false, killCount: 2);
            Assert.AreEqual(0, packets.Count(packet => packet is UpdateObjectiveCounterPacket or
                ObjectiveCompletedPacket or LogosStoneAddedPacket or WaypointGainedPacket));
            AssertHistoryUnchanged(harness);
        }

        [TestMethod]
        public void ReconnectRecognizesNewDurableFactsWithoutResettingAcceptedProgress()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            InstallExistingFactFixture(harness);
            harness.ReconnectFresh();
            SeedExistingFacts(harness);
            var giver = harness.AddNpc(BootcampRuntimeTestHarness.MajorMcAllisterCreatureId);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, giver.EntityId, FactMissionId));
            var assignment = harness.Client.Player.Missions[FactMissionId].AssignmentId;
            var generation = harness.Client.Player.Missions[FactMissionId].Generation;
            harness.Drain();
            Assert.IsTrue(harness.Manager.RecordProgress(harness.Client, MissionProgressEvent.Creature(75)));
            Assert.AreEqual(3U, harness.Drain().OfType<UpdateObjectiveCounterPacket>().Single().CounterValue);
            using (var unit = harness.Context.CreateChar())
                unit.ExecuteTransaction(() => unit.CharacterTeleporters.Add(
                    new CharacterTeleporterEntry(harness.Client.Player.Id, 51, (byte)WaypointType.Waypoint)));
            var rejectedWrite = false;
            harness.Context.BeforeSave = database =>
            {
                if (database.ChangeTracker.Entries<CharacterMissionObjectiveCounterEntry>()
                    .Any(entry => entry.Entity.MissionId == FactMissionId && entry.State == EntityState.Modified))
                {
                    rejectedWrite = true;
                    throw new DbUpdateException("Injected durable-fact recovery failure.");
                }
            };

            harness.ReconnectFresh(drainPackets: false);
            harness.Manager.PublishInitialState(harness.Client);

            AssertExistingFactState(harness, recovered: false, killCount: 3);
            var failed = harness.Drain().OfType<MissionStatusInfoPacket>().Single();
            AssertExistingFactInfo(failed.MissionStatusDict[FactMissionId], recovered: false, killCount: 3);
            harness.Context.BeforeSave = null;

            harness.Manager.PublishInitialState(harness.Client);

            AssertExistingFactState(harness, recovered: true, killCount: 3);
            Assert.IsTrue(rejectedWrite);
            Assert.AreEqual(assignment, harness.Client.Player.Missions[FactMissionId].AssignmentId);
            Assert.AreEqual(generation, harness.Client.Player.Missions[FactMissionId].Generation);
            var packets = harness.Drain();
            AssertExistingFactInfo(packets.OfType<MissionStatusInfoPacket>().Single().MissionStatusDict[FactMissionId],
                recovered: true, killCount: 3);
            Assert.AreEqual(0, packets.Count(packet => packet is UpdateObjectiveCounterPacket or ObjectiveCompletedPacket or
                MissionGainedPacket or LogosStoneAddedPacket or WaypointGainedPacket));
            AssertHistoryUnchanged(harness);

            harness.Manager.PublishInitialState(harness.Client);

            AssertExistingFactState(harness, recovered: true, killCount: 3);
            Assert.AreEqual(0, harness.Drain().Count(packet => packet is UpdateObjectiveCounterPacket or ObjectiveCompletedPacket));
            AssertHistoryUnchanged(harness);
        }

        [TestMethod]
        [DataRow(MissionProgressEventKind.WaypointAcquired)]
        [DataRow(MissionProgressEventKind.LogosAcquired)]
        public void PartialDistinctCountersPersistOnlyEligibleDurableAcquisitions(
            MissionProgressEventKind kind)
        {
            var subjects = kind == MissionProgressEventKind.WaypointAcquired
                ? new[] { 49U, 50U, 51U }
                : new[] { 1U, 2U, 6U };
            var unrelated = kind == MissionProgressEventKind.WaypointAcquired ? 57U : 9U;
            var authored = MissionProgressRuleAuthoring.TryBuild(
                subjects.Select((subject, index) => new MissionTriggerDefinition(new MissionTriggerEntry
                {
                    MissionId = 321,
                    ContentRevision = "distinct-counter-test",
                    ObjectiveId = 1,
                    TransitionId = 1,
                    TriggerId = (uint)index + 1,
                    Requirement = MissionContentRequirement.Required,
                    Kind = MissionTriggerKind.ProgressEvent,
                    Sequence = (uint)index + 1,
                    EventKind = (byte)kind,
                    SubjectId = subject,
                    CounterId = 0,
                    InitialValue = 0,
                    TargetValue = 3,
                    Comment = "Count distinct durable acquisitions"
                })).ToArray(),
                out var rule, out var counters, out _, out var diagnostic);
            Assert.IsTrue(authored, diagnostic);

            using var context = MissionTestContext.WithProgressMission(rule, counters: counters);
            var giver = context.AddNpc(77);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(
                context.Client, giver.EntityId, 321));
            context.Drain();
            var character = new CharacterManager(context, context.Manager);
            var objects = new DynamicObjectManager(
                context,
                updateCharacter: (client, update, value) => character.UpdateCharacter(client, update, value),
                missionManager: context.Manager);
            var grantPacketType = kind == MissionProgressEventKind.WaypointAcquired
                ? typeof(WaypointGainedPacket)
                : typeof(LogosStoneAddedPacket);

            Acquire(subjects[0]);

            AssertProgress(1);
            AssertPackets(1, grantPacketType, typeof(UpdateObjectiveCounterPacket));

            Acquire(subjects[0], expectedNew: false);
            Assert.IsFalse(context.Manager.RecordProgress(context.Client, Progress(subjects[0])));
            AssertProgress(1);
            AssertPackets(null);

            Acquire(unrelated);

            AssertProgress(1);
            AssertPackets(null, grantPacketType);

            context.BeforeSave = database =>
            {
                if (database.ChangeTracker.Entries<CharacterMissionObjectiveCounterEntry>()
                    .Any(entry => entry.State == EntityState.Modified))
                {
                    Assert.AreEqual(1U, context.Client.Player.Missions[321].Objectives[1].Counters[0]);
                    throw new DbUpdateException("Injected distinct-counter progress failure.");
                }
            };

            Acquire(subjects[1]);

            context.BeforeSave = null;
            AssertProgress(1);
            AssertPackets(null, grantPacketType);
            using (var unit = context.CreateChar())
            {
                var durableSubjects = kind == MissionProgressEventKind.WaypointAcquired
                    ? unit.CharacterTeleporters.Get(1).Select(entry => entry.WaypointId).ToArray()
                    : unit.CharacterLogoses.GetLogos(1).ToArray();
                CollectionAssert.AreEquivalent(
                    new[] { subjects[0], subjects[1], unrelated }, durableSubjects);
            }

            Assert.IsTrue(context.Manager.RecordProgress(context.Client, Progress(subjects[1])));

            AssertProgress(2);
            AssertPackets(2, typeof(UpdateObjectiveCounterPacket));
            Assert.IsFalse(context.Manager.RecordProgress(context.Client, Progress(subjects[1])));
            Assert.IsFalse(context.Manager.RecordProgress(context.Client, Progress(subjects[2])));
            AssertProgress(2);
            AssertPackets(null);

            context.ReloadPlayerMissions();

            AssertProgress(2);
            AssertPackets(null);

            Acquire(subjects[2]);

            AssertProgress(3, complete: true);
            AssertPackets(3, grantPacketType, typeof(UpdateObjectiveCounterPacket),
                typeof(ObjectiveCompletedPacket), typeof(MissionCompleteablePacket));
            Assert.IsFalse(context.Manager.RecordProgress(context.Client, Progress(subjects[2])));
            AssertPackets(null);

            context.ReloadPlayerMissions();

            AssertProgress(3, complete: true);
            AssertPackets(null);

            MissionProgressEvent Progress(uint subject) =>
                kind == MissionProgressEventKind.WaypointAcquired
                    ? MissionProgressEvent.Waypoint(subject)
                    : MissionProgressEvent.Logos(subject);

            void Acquire(uint subject, bool expectedNew = true)
            {
                if (kind == MissionProgressEventKind.WaypointAcquired)
                    objects.CheckPlayerWaypoint(
                        context.Client, new WaypointInfo(subject, false, WaypointType.Waypoint));
                else
                    Assert.AreEqual(expectedNew, character.TryAddLogos(context.Client, subject));
            }

            void AssertProgress(uint value, bool complete = false)
            {
                var state = complete ? MissionObjectiveState.Completed : MissionObjectiveState.Incomplete;
                var runtime = context.Client.Player.Missions[321];
                var durable = context.ReadProgress(321).Missions[321].Objectives[1];
                Assert.AreEqual(value, runtime.Objectives[1].Counters[0]);
                Assert.AreEqual(value, durable.Counters[0]);
                Assert.AreEqual(state, runtime.Objectives[1].State);
                Assert.AreEqual((byte)state, durable.State);
                Assert.AreEqual(complete, runtime.Completeable);
                Assert.AreEqual(complete, context.ReadMission(321).Completeable);
                var native = context.Manager.BuildStatusSnapshot(context.Client.Player)[321];
                var objective = native.ObjectivesList.Single();
                Assert.AreEqual(1U, objective.ObjectiveId);
                Assert.AreEqual(value, objective.Counters[0].CounterValue);
                Assert.AreEqual(state, objective.State);
                Assert.AreEqual(complete, native.Completeable);
            }

            void AssertPackets(uint? value, params Type[] expectedTypes)
            {
                var packets = context.Drain()
                    .Where(packet => packet is not NPCConversationStatusPacket).ToArray();
                CollectionAssert.AreEqual(expectedTypes, packets.Select(packet => packet.GetType()).ToArray());
                if (!value.HasValue)
                    return;
                var counter = packets.OfType<UpdateObjectiveCounterPacket>().Single();
                Assert.AreEqual(321U, counter.MissionId);
                Assert.AreEqual(1U, counter.ObjectiveId);
                Assert.AreEqual(0U, counter.CounterId);
                Assert.AreEqual(value.Value, counter.CounterValue);
                Assert.AreEqual(0U, counter.InitialValue);
                Assert.AreEqual(3U, counter.TargetValue);
            }
        }

        private static void InstallExistingFactFixture(BootcampRuntimeTestHarness.Harness harness, bool invalidKillOptIn = false)
        {
            const string revision = "existing-fact-test";
            var world = harness.WorldContext;
            world.Add(new MissionContentDefinitionEntry
            {
                MissionId = FactMissionId, ContentRevision = revision, Enabled = true,
                ClientNameTextId = 1000,
                GiverId = BootcampRuntimeTestHarness.MajorMcAllisterCreatureId,
                ReceiverId = BootcampRuntimeTestHarness.MajorMcAllisterCreatureId,
                Level = 1, GroupType = 1, CategoryId = 1, Shareable = false,
                Comment = "Isolated durable-fact fixture"
            });
            for (uint id = 1; id <= 7; id++)
            {
                var counter = id is 1 or 2 or 4;
                world.Add(new MissionObjectiveDefinitionEntry
                {
                    MissionId = FactMissionId, ContentRevision = revision, ObjectiveId = id,
                    ClientNameTextId = 1000 + id, ClientBodyTextId = 2000 + id,
                    ClientCounter0TextId = counter || id == 7 ? 9000 + id : null,
                    Ordinal = id, InitialState = (byte)MissionObjectiveState.Incomplete,
                    IsRequired = id is 4 or 7, Comment = "Existing fact or live-only progress"
                });
                if (id == 7)
                    continue;
                world.Add(new MissionObjectiveTransitionEntry
                {
                    MissionId = FactMissionId, ContentRevision = revision, ObjectiveId = id,
                    TransitionId = 1, Sequence = 1,
                    FromState = (byte)MissionObjectiveState.Incomplete,
                    ToState = (byte)MissionObjectiveState.Completed, Comment = "Recognize progress"
                });
                var kind = id switch
                {
                    1 => MissionProgressEventKind.WaypointAcquired,
                    2 or 5 => MissionProgressEventKind.LogosAcquired,
                    4 => MissionProgressEventKind.CreatureKilled,
                    _ => MissionProgressEventKind.MissionCompleted
                };
                var subjects = id switch
                {
                    1 => new[] { 49U, 50U, 51U },
                    2 => new[] { 1U, 2U },
                    3 => new[] { SuccessfulHistoryId },
                    4 => new[] { 75U },
                    5 => new[] { 6U },
                    _ => new[] { FailedHistoryId }
                };
                uint sequence = 0;
                foreach (var subject in subjects)
                {
                    sequence++;
                    world.Add(new MissionTriggerEntry
                    {
                        MissionId = FactMissionId, ContentRevision = revision, ObjectiveId = id,
                        TransitionId = 1, TriggerId = sequence, Sequence = sequence,
                        Kind = MissionTriggerKind.ProgressEvent, EventKind = (byte)kind, SubjectId = subject,
                        CounterId = counter ? 0U : null,
                        InitialValue = counter ? id == 4 ? 2U : 0U : null,
                        TargetValue = counter ? id == 4 ? 5U : (uint)subjects.Length : null,
                        Comment = "Durable acquisition or live event"
                    });
                }
            }
            var bindings = JsonSerializer.SerializeToNode(new MissionSceneDefinition
            {
                Credit = new()
                {
                    [1] = Rasa.Missions.Runtime.MissionCreditPolicy.Personal,
                    [7] = Rasa.Missions.Runtime.MissionCreditPolicy.Personal
                },
                ObjectiveAggregations = new()
                {
                    [7] = new MissionObjectiveAggregation(new[] { 1U, 2U, 3U }, 3, 0)
                }
            }, MissionContentCodec.Options).AsObject();
            bindings["existingFactObjectiveIds"] = invalidKillOptIn
                ? new JsonArray(1, 2, 3, 4, 6)
                : new JsonArray(1, 2, 3, 6);
            world.Add(new MissionSceneBindingEntry
            {
                MissionId = FactMissionId, ContentRevision = revision,
                Bindings = bindings.ToJsonString(MissionContentCodec.Options)
            });
            world.SaveChanges();
            world.ChangeTracker.Clear();
        }

        private static void SeedExistingFacts(BootcampRuntimeTestHarness.Harness harness)
        {
            var characterId = harness.Client.Player.Id;
            using var unit = harness.Context.CreateChar();
            unit.ExecuteTransaction(() =>
            {
                unit.CharacterTeleporters.Add(new CharacterTeleporterEntry(characterId, 49, (byte)WaypointType.Waypoint));
                unit.CharacterTeleporters.Add(new CharacterTeleporterEntry(characterId, 50, (byte)WaypointType.Waypoint));
                foreach (var id in new[] { 1U, 2U, 6U })
                    unit.CharacterLogoses.SetLogos(characterId, id);
                foreach (var entry in new[]
                {
                    new CharacterMissionEntry(characterId, SuccessfulHistoryId, (uint)MissionState.Completed),
                    new CharacterMissionEntry(characterId, FailedHistoryId, (uint)MissionState.Failed)
                })
                {
                    entry.AssignmentId = Guid.NewGuid().ToString("N");
                    entry.Generation = 1;
                    entry.ContentRevision = "existing-fact-history";
                    unit.CharacterMissions.Runtime.Archive(entry, new DateTime(2026, 9, 28, 12, 0, 0, DateTimeKind.Utc));
                }
            });
            harness.Client.Player.GainedWaypoints.Clear();
            harness.Client.Player.Logos.Clear();
            harness.Client.Player.MissionSuccessHistory.Clear();
            harness.Client.Player.MissionHistory.Clear();
        }

        private static void AssertExistingFactState(BootcampRuntimeTestHarness.Harness harness, bool recovered, uint killCount)
        {
            var runtime = harness.Client.Player.Missions[FactMissionId];
            var durable = harness.Context.ReadProgress(FactMissionId).Missions[FactMissionId];
            foreach (var id in new[] { 1U, 2U, 3U, 4U, 5U, 6U, 7U })
                Assert.AreEqual((byte)runtime.Objectives[id].State, durable.Objectives[id].State);
            Assert.AreEqual(recovered ? 3U : 2U, durable.Objectives[1].Counters[0]);
            Assert.AreEqual(2U, durable.Objectives[2].Counters[0]);
            Assert.AreEqual(killCount, durable.Objectives[4].Counters[0]);
            Assert.AreEqual(recovered ? 3U : 2U, durable.Objectives[7].Counters[0]);
            Assert.AreEqual(MissionObjectiveState.Completed, runtime.Objectives[2].State);
            Assert.AreEqual(MissionObjectiveState.Completed, runtime.Objectives[3].State);
            Assert.AreEqual(MissionObjectiveState.Incomplete, runtime.Objectives[4].State);
            Assert.AreEqual(MissionObjectiveState.Incomplete, runtime.Objectives[5].State);
            Assert.AreEqual(MissionObjectiveState.Incomplete, runtime.Objectives[6].State);
            Assert.IsFalse(runtime.Completeable);
            Assert.AreEqual((uint)MissionState.Active, harness.Context.ReadMission(FactMissionId).MissionState);
            AssertExistingFactInfo(harness.Manager.BuildStatusSnapshot(harness.Client.Player)[FactMissionId], recovered, killCount);
        }

        private static void AssertExistingFactInfo(MissionInfo info, bool recovered, uint killCount)
        {
            var objectives = info.ObjectivesList.ToDictionary(objective => objective.ObjectiveId);
            Assert.AreEqual(7, objectives.Count);
            Assert.AreEqual(recovered ? 3U : 2U, objectives[1].Counters[0].CounterValue);
            Assert.AreEqual(2U, objectives[2].Counters[0].CounterValue);
            Assert.AreEqual(killCount, objectives[4].Counters[0].CounterValue);
            Assert.AreEqual(recovered ? 3U : 2U, objectives[7].Counters[0].CounterValue);
            Assert.AreEqual(recovered ? MissionObjectiveState.Completed : MissionObjectiveState.Incomplete, objectives[1].State);
            Assert.AreEqual(recovered ? MissionObjectiveState.Completed : MissionObjectiveState.Incomplete, objectives[7].State);
            Assert.AreEqual(MissionObjectiveState.Completed, objectives[2].State);
            Assert.AreEqual(MissionObjectiveState.Completed, objectives[3].State);
            Assert.AreEqual(MissionObjectiveState.Incomplete, objectives[4].State);
            Assert.AreEqual(MissionObjectiveState.Incomplete, objectives[5].State);
            Assert.AreEqual(MissionObjectiveState.Incomplete, objectives[6].State);
            Assert.IsFalse(info.Completeable);
        }

        private static void AssertHistoryUnchanged(BootcampRuntimeTestHarness.Harness harness)
        {
            using var unit = harness.Context.CreateChar();
            var history = unit.CharacterMissions.Runtime.History(harness.Client.Player.Id);
            CollectionAssert.AreEquivalent(new[] { SuccessfulHistoryId, FailedHistoryId },
                history.Select(entry => entry.MissionId).ToArray());
            Assert.IsTrue(history.Single(entry => entry.MissionId == SuccessfulHistoryId).Rewarded);
            Assert.IsFalse(history.Single(entry => entry.MissionId == FailedHistoryId).Rewarded);
            Assert.IsFalse(history.Any(entry => entry.MissionId == FactMissionId));
        }
    }
}
