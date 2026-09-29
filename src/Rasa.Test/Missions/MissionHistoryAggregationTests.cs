using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Packets.Mission.Server;
using Rasa.Structures;
using Rasa.Structures.Char;

namespace Rasa.Test.Missions
{
    [TestClass]
    [DoNotParallelize]
    public class MissionHistoryAggregationTests
    {
        private const uint Target = 65004;
        private const uint First = 50001;
        private const uint Alternative = 50002;
        private const uint Pending = 50003;
        private const uint Instance = 50004;

        [TestMethod]
        public void RealCompletionsAndLegacyRewardSettlementCreditEachOrGroupOnce()
        {
            using var context = CreateContext();
            context.SeedMission(1, First, (uint)MissionState.Active, true);
            context.SeedMission(1, Alternative, (uint)MissionState.Active, true);
            context.SeedMission(1, Pending, (uint)MissionState.Success, false);
            context.SeedMission(1, Instance, (uint)MissionState.Failed, false);
            context.ReloadPlayerMissions();
            var giver = context.AddNpc(77);
            var receiver = context.AddNpc(88);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, Target));
            AssertCounter(context, 0, complete: false);
            context.Drain();

            Assert.IsTrue(context.Manager.CompleteOfferedMission(context.Client, receiver.EntityId, First, null));

            AssertCounter(context, 1, complete: false);
            Assert.AreEqual(1U, context.Drain().OfType<UpdateObjectiveCounterPacket>()
                .Single(packet => packet.MissionId == Target).CounterValue);
            Assert.IsTrue(context.Manager.CompleteOfferedMission(context.Client, receiver.EntityId, Alternative, null));
            AssertCounter(context, 1, complete: false);
            Assert.AreEqual(0, context.Drain().OfType<UpdateObjectiveCounterPacket>().Count(packet => packet.MissionId == Target));
            Assert.IsFalse(context.Manager.RecordProgress(context.Client, MissionProgressEvent.Mission(Pending)));
            Assert.IsFalse(context.Manager.RecordProgress(context.Client, MissionProgressEvent.Mission(Instance)));
            AssertCounter(context, 1, complete: false);

            Assert.IsTrue(context.Manager.RewardOfferedMission(context.Client, receiver.EntityId, Pending, null, null));

            AssertCounter(context, 2, complete: false);
            Assert.AreEqual(2U, context.Drain().OfType<UpdateObjectiveCounterPacket>()
                .Single(packet => packet.MissionId == Target).CounterValue);
            Assert.IsFalse(context.Manager.RecordProgress(context.Client, MissionProgressEvent.Mission(Pending)));
            AssertCounter(context, 2, complete: false);
            using var unit = context.CreateChar();
            Assert.IsFalse(unit.CharacterMissions.Runtime.History(1).Any(entry => entry.MissionId == Target));
        }

        [TestMethod]
        public void LegacyRowsReconcileAndSourceRollbackPreservesHistoryCountersAndRewards()
        {
            using var context = CreateContext();
            context.SeedMission(1, First, (uint)MissionState.Completed, true);
            context.SeedMission(1, Pending, (uint)MissionState.Active, true);
            context.SeedMission(1, Instance, (uint)MissionState.Active, true);
            context.ReloadPlayerMissions();
            var giver = context.AddNpc(77);
            var receiver = context.AddNpc(88);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, Target));
            var assignment = context.Client.Player.Missions[Target].AssignmentId;
            AssertCounter(context, 1, complete: false);
            var credits = context.Client.Player.Credits[CurencyType.Credits];
            context.Drain();
            context.AfterSave = database =>
            {
                if (database.ChangeTracker.Entries<CharacterMissionObjectiveCounterEntry>()
                    .Any(entry => entry.Entity.MissionId == Target && entry.Entity.CounterValue == 2))
                    throw new DbUpdateException("Injected story-counter commit failure.");
            };

            Assert.IsFalse(context.Manager.CompleteOfferedMission(context.Client, receiver.EntityId, Pending, null));

            context.AfterSave = null;
            AssertCounter(context, 1, complete: false);
            Assert.AreEqual(credits, context.Client.Player.Credits[CurencyType.Credits]);
            Assert.AreEqual((uint)MissionState.Active, context.ReadMission(Pending).MissionState);
            Assert.AreEqual(0, context.Drain().Count(packet => packet is UpdateObjectiveCounterPacket or MissionCompletedPacket));
            Assert.IsTrue(context.Manager.CompleteOfferedMission(context.Client, receiver.EntityId, Pending, null));
            AssertCounter(context, 2, complete: false);
            Assert.AreEqual(credits + 7, context.Client.Player.Credits[CurencyType.Credits]);
            context.Drain();
            using (var unit = context.CreateChar())
                unit.CharacterMissions.SetState(1, Instance, (uint)MissionState.Completed);
            context.ReloadPlayerMissions();

            context.Manager.PublishInitialState(context.Client);

            AssertCounter(context, 3, complete: true);
            Assert.AreEqual(assignment, context.Client.Player.Missions[Target].AssignmentId);
            Assert.AreEqual(credits + 7, context.Client.Player.Credits[CurencyType.Credits]);
            var packets = context.Drain();
            var info = packets.OfType<MissionStatusInfoPacket>().Single().MissionStatusDict[Target];
            Assert.AreEqual(3U, info.ObjectivesList.Single(objective => objective.ObjectiveId == 55).Counters[0].CounterValue);
            Assert.AreEqual(0, packets.Count(packet => packet is UpdateObjectiveCounterPacket or ObjectiveCompletedPacket));
            using var verify = context.CreateChar();
            Assert.IsFalse(verify.CharacterMissions.Runtime.History(1).Any(entry => entry.MissionId == Target));
        }

        [TestMethod]
        public void HistoryGroupsAreImmutableNonemptyAndAbsentFromDefaultSceneJson()
        {
            var alternatives = new List<uint> { First, Alternative };
            var groups = new List<IReadOnlyList<uint>> { alternatives, new uint[] { Pending } };
            var definition = new MissionHistoryAggregation(groups, 0);
            alternatives.Clear();
            groups.Clear();
            Assert.AreEqual(2U, definition.TargetCount);
            CollectionAssert.AreEqual(new[] { First, Alternative }, definition.Groups[0].ToArray());
            Assert.ThrowsExactly<ArgumentException>(() =>
                new MissionHistoryAggregation(Array.Empty<IReadOnlyList<uint>>()));
            Assert.ThrowsExactly<ArgumentException>(() =>
                new MissionHistoryAggregation(new IReadOnlyList<uint>[] { Array.Empty<uint>() }));
            Assert.ThrowsExactly<ArgumentException>(() =>
                new MissionHistoryAggregation(new IReadOnlyList<uint>[] { new uint[] { First }, new uint[] { First } }));
            var json = JsonSerializer.Serialize(new MissionSceneDefinition(), MissionContentCodec.Options);
            Assert.IsFalse(json.Contains("objectiveHistoryAggregations", StringComparison.Ordinal));
        }

        private static MissionTestContext CreateContext()
        {
            var definitions = new Dictionary<uint, Mission>();
            var rewards = new Dictionary<uint, MissionRewardDefinition>();
            foreach (var id in new[] { First, Alternative, Pending, Instance })
            {
                definitions[id] = CreateMission(id, Objective(1));
                rewards[id] = new MissionRewardDefinition(0,
                    new Dictionary<CurencyType, int> { [CurencyType.Credits] = 7 },
                    Array.Empty<MissionRewardItem>(), Array.Empty<MissionRewardItem>());
            }
            definitions[Target] = CreateMission(Target,
                new MissionObjectiveDefinition(55, 13213, 13214, new uint?[] { 13685 }, 0,
                    MissionObjectiveState.Incomplete, true, null, null, Array.Empty<MissionObjectiveConversation>(),
                    Array.Empty<uint>(), Array.Empty<uint>(), Array.Empty<MissionIndicator>(),
                    historyAggregation: new MissionHistoryAggregation(new IReadOnlyList<uint>[]
                    {
                        new uint[] { First, Alternative }, new uint[] { Pending }, new uint[] { Instance }
                    }, 0)),
                new MissionObjectiveDefinition(58, 17853, 17854, Array.Empty<uint?>(), 1,
                    MissionObjectiveState.Incomplete, true, null, null, Array.Empty<MissionObjectiveConversation>(),
                    Array.Empty<uint>(), Array.Empty<uint>(), Array.Empty<MissionIndicator>(),
                    aggregation: new MissionObjectiveAggregation(new uint[] { 55 }, 1)));
            return MissionTestContext.WithCustomDefinitions(definitions, rewards);
        }

        private static Mission CreateMission(uint id, params MissionObjectiveDefinition[] objectives) =>
            new(id, $"History fixture {id}", id, 77, 88, 1, 1, 1, false, false, objectives, true);

        private static MissionObjectiveDefinition Objective(uint id) =>
            new(id, 1001, 1002, Array.Empty<uint?>(), 0, MissionObjectiveState.Incomplete, true,
                null, null, Array.Empty<MissionObjectiveConversation>(), Array.Empty<uint>(), Array.Empty<uint>(),
                Array.Empty<MissionIndicator>());

        private static void AssertCounter(MissionTestContext context, uint value, bool complete)
        {
            var runtime = context.Client.Player.Missions[Target];
            var durable = context.ReadProgress(Target).Missions[Target];
            Assert.AreEqual(value, runtime.Objectives[55].Counters[0]);
            Assert.AreEqual(value, durable.Objectives[55].Counters[0]);
            Assert.AreEqual(complete ? MissionObjectiveState.Completed : MissionObjectiveState.Incomplete, runtime.Objectives[55].State);
            Assert.AreEqual(complete ? MissionObjectiveState.Completed : MissionObjectiveState.Incomplete, runtime.Objectives[58].State);
            Assert.AreEqual(complete, runtime.Completeable);
            Assert.AreEqual(complete, context.ReadMission(Target).Completeable);
        }
    }
}
