extern alias RasaGame;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Packets.Game.Client;
    using Rasa.Packets.Game.Server;
    using Rasa.Packets.MapChannel.Client;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Mission.Server;
    using Rasa.Repositories;
    using Rasa.Repositories.Char;
    using Rasa.Repositories.UnitOfWork;
    using Rasa.Repositories.World;
    using Rasa.Structures;
    using Rasa.Structures.Char;
    using Rasa.Structures.World;

    [TestClass]
    [DoNotParallelize]
    public class MissionLifecycleTests
    {
        [TestMethod]
        public void MissionRowsAreScopedByPersistentCharacterAndSupportMultipleMissions()
        {
            using var db = new MissionTestContext();
            db.SeedCharacter(10, 0, 100);
            db.SeedCharacter(10, 1, 101);
            db.SeedCharacter(20, 0, 200);
            db.SeedMission(100, 321, 0, false);
            db.SeedMission(100, 429, 0, true);
            db.SeedMission(101, 321, 4, false);
            db.SeedMission(200, 429, 0, false);

            using var unit = db.CreateChar();
            CollectionAssert.AreEquivalent(new uint[] { 321, 429 },
                unit.CharacterMissions.Get(100).Select(x => x.MissionId).ToArray());
            CollectionAssert.AreEquivalent(new uint[] { 321 },
                unit.CharacterMissions.Get(101).Select(x => x.MissionId).ToArray());
            CollectionAssert.AreEquivalent(new uint[] { 429 },
                unit.CharacterMissions.Get(200).Select(x => x.MissionId).ToArray());
        }

        [TestMethod]
        public void MissionStateMutationsPersistAndRemoveOnlyTheSelectedMission()
        {
            using var db = new MissionTestContext();
            db.SeedCharacter(10, 0, 100);

            using (var unit = db.CreateChar())
            {
                unit.CharacterMissions.Add(new CharacterMissionEntry(100, 321, 0));
                unit.CharacterMissions.Add(new CharacterMissionEntry(100, 429, 1));
                unit.CharacterMissions.SetState(100, 321, 4);
                unit.CharacterMissions.SetCompletable(100, 321, true);
            }

            using (var unit = db.CreateChar())
            {
                var mission = unit.CharacterMissions.GetByCharacterAndMission(100, 321);
                Assert.AreEqual(4U, mission.MissionState);
                Assert.IsTrue(mission.Completeable);
                Assert.AreSame(
                    mission,
                    unit.CharacterMissions.GetByCharacterAndMission(100, 321));
                unit.CharacterMissions.Remove(100, 321);
            }

            using var reopened = db.CreateChar();
            Assert.IsNull(
                reopened.CharacterMissions.GetByCharacterAndMission(100, 321));
            CollectionAssert.AreEqual(new uint[] { 429 },
                reopened.CharacterMissions.Get(100).Select(x => x.MissionId).ToArray());
        }

        [TestMethod]
        public void InitialSnapshotMapsOnlySupportedDurableCharacterRows()
        {
            using var context = MissionTestContext.WithDefinitions(321, 429, 666, 777, 888);
            context.SeedMission(context.Client.Player.Id, 321, (uint)MissionState.Active, false);
            context.SeedMission(context.Client.Player.Id, 429, (uint)MissionState.Active, true);
            context.SeedMission(context.Client.Player.Id, 666, (uint)MissionState.Failed, true);
            context.SeedMission(context.Client.Player.Id, 777, (uint)MissionState.Completed, false);
            context.SeedMission(context.Client.Player.Id, 888, (uint)MissionState.Success, false);
            context.SeedMission(context.Client.Player.Id, 999, (uint)MissionState.Failed, false);
            context.ReloadPlayerMissions();

            context.Manager.PublishInitialState(context.Client);

            var packet = context.Drain().OfType<MissionStatusInfoPacket>().Single();
            CollectionAssert.AreEquivalent(new uint[] { 321, 429, 666, 777, 888 },
                packet.MissionStatusDict.Keys.ToArray());
            Assert.AreEqual(MissionState.Active, packet.MissionStatusDict[321].MissionState);
            Assert.IsFalse(packet.MissionStatusDict[321].Completeable);
            Assert.AreEqual(MissionState.Active, packet.MissionStatusDict[429].MissionState);
            Assert.IsTrue(packet.MissionStatusDict[429].Completeable);
            Assert.AreEqual(MissionState.Failed, packet.MissionStatusDict[666].MissionState);
            Assert.IsFalse(packet.MissionStatusDict[666].Completeable);
            Assert.AreEqual(MissionState.Completed, packet.MissionStatusDict[777].MissionState);
            Assert.IsFalse(packet.MissionStatusDict[777].Completeable);
            Assert.AreEqual(MissionState.Success, packet.MissionStatusDict[888].MissionState);
            Assert.IsFalse(packet.MissionStatusDict[888].Completeable);
        }

        [TestMethod]
        public void ObjectiveCompletionPersistsTransitionBeforePublishingOrderedPackets()
        {
            using var context = MissionTestContext.WithObjectiveMission();
            var giver = context.AddNpc(77);
            var objectiveNpc = context.AddNpc(500, npcPackageId: 700);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
            context.Drain();
            Assert.IsTrue(context.Manager.OpenNpcConversation(context.Client, objectiveNpc.EntityId));
            var saveAttempts = context.SaveAttempts;
            var queries = new List<string>();
            context.BeforeCommand = command =>
            {
                if (command.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
                    queries.Add(command);
            };
            context.BeforeSave = _ =>
            {
                Assert.AreEqual(MissionObjectiveState.Incomplete,
                    context.Client.Player.Missions[321].Objectives[5].State);
                Assert.AreEqual(0, context.Drain().Count);
            };

            Assert.IsTrue(context.Manager.TryCompleteNpcObjective(
                context.Client, objectiveNpc.EntityId, 321, 5, 11));

            Assert.AreEqual(saveAttempts + 1, context.SaveAttempts);
            Assert.AreEqual(8, queries.Count,
                "Dialogue admission and planning each read ownership/assignment; final guards reread ownership and the exact " +
                "assignment twice, plus one objective aggregate. No deadline reads are needed here.\n" +
                string.Join("\n\n", queries));
            Assert.AreEqual(3, queries.Count(query => query.Contains("FROM \"character\"")));
            Assert.AreEqual(4, queries.Count(query => query.Contains("FROM \"character_mission\"")));
            Assert.AreEqual(1, queries.Count(query => query.Contains("FROM \"character_mission_objective\"")));
            Assert.IsFalse(queries.Any(query => query.Contains("character_mission_deadline")));
            context.BeforeCommand = null;
            var progress = context.ReadProgress(321).Missions[321];
            Assert.AreEqual((byte)MissionObjectiveState.Completed, progress.Objectives[5].State);
            Assert.AreEqual((byte)MissionObjectiveState.Incomplete, progress.Objectives[9].State);
            Assert.IsFalse(context.ReadMission(321).Completeable);
            var packets = context.Drain();
            CollectionAssert.AreEqual(
                new[]
                {
                    typeof(ObjectiveCompletedPacket),
                    typeof(ObjectiveRevealedPacket),
                    typeof(ObjectiveActivatedPacket)
                },
                packets.Where(packet => packet is not NPCConversationStatusPacket)
                    .Select(packet => packet.GetType()).ToArray());
        }

        [TestMethod]
        public void CharacterHydrationNeverCombinesStaleMissionRowsWithNewObjectiveProgress()
        {
            using var context = MissionTestContext.WithDefinitions(321);
            context.SeedMission(context.Client.Player.Id, 321, (uint)MissionState.Active, false);
            context.Client.Player.Missions.Clear();
            var queryCount = 0;
            var completionAttempted = false;
            var readUsedSerializableTransaction = false;
            Task<bool> completion = null;
            context.BeforeQuery = database =>
            {
                queryCount++;
                if (queryCount != 2)
                    return;

                completionAttempted = true;
                readUsedSerializableTransaction =
                    database.Database.CurrentTransaction?.GetDbTransaction().IsolationLevel ==
                    System.Data.IsolationLevel.Serializable;
                completion = Task.Run(() => context.TryCompleteMissionAggregate(321, 1));
                if (!readUsedSerializableTransaction)
                    completion.GetAwaiter().GetResult();
            };
            var singleton = typeof(MissionApplication).GetField(
                "_instance", BindingFlags.Static | BindingFlags.NonPublic)!;
            var previous = singleton.GetValue(null);
            singleton.SetValue(null, context.Manager);
            try
            {
                using var unit = context.CreateChar();
                new CharacterManager(context).HydrateMissions(context.Client.Player, unit);
            }
            finally
            {
                context.BeforeQuery = null;
                singleton.SetValue(null, previous);
            }

            Assert.AreEqual(3, queryCount, "Hydration reads completion history, assignments and objective progress in one transaction.");
            Assert.IsTrue(completionAttempted);
            Assert.IsTrue(readUsedSerializableTransaction);
            Assert.IsTrue(context.Client.Player.Missions.TryGetValue(321, out var mission),
                "A concurrent completion must not make a valid mission aggregate disappear.");
            var objective = mission.Objectives[1];
            completion.GetAwaiter().GetResult();
            Assert.IsTrue(
                (!mission.Completeable && objective.State == MissionObjectiveState.Incomplete) ||
                (mission.Completeable && objective.State == MissionObjectiveState.Completed),
                "Hydration must observe either the complete old aggregate or the complete new aggregate.");
        }

        [TestMethod]
        public void ObjectiveCompletionReconcilesDurableRevealWithoutPublishingDuplicatePacket()
        {
            using var context = MissionTestContext.WithObjectiveMission(activateSuccessor: false);
            var giver = context.AddNpc(77);
            var objectiveNpc = context.AddNpc(500, npcPackageId: 700);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
            context.Drain();
            using (var unit = context.CreateChar())
                unit.CharacterMissionProgress.SetObjectiveState(
                    1,
                    321,
                    9,
                    (byte)MissionObjectiveState.Inactive,
                    (byte)MissionObjectiveState.NotAssigned);

            Assert.IsTrue(context.Manager.CompleteOfferedObjective(
                context.Client, objectiveNpc.EntityId, 321, 5, 11));

            Assert.AreEqual(MissionObjectiveState.NotAssigned,
                context.Client.Player.Missions[321].Objectives[9].State);
            Assert.AreEqual((byte)MissionObjectiveState.NotAssigned,
                context.ReadProgress(321).Missions[321].Objectives[9].State);
            CollectionAssert.AreEqual(
                new[] { typeof(ObjectiveCompletedPacket) },
                context.Drain().Where(packet => packet is not NPCConversationStatusPacket)
                    .Select(packet => packet.GetType()).ToArray());
        }

        [TestMethod]
        public void StaleClientActivatesPreRevealedSuccessorWithoutRevealingItAgain()
        {
            using var context = MissionTestContext.WithObjectiveMission();
            var giver = context.AddNpc(77);
            var objectiveNpc = context.AddNpc(500, npcPackageId: 700);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
            context.Drain();
            var staleClient = context.CreateCompetingClient();
            using (var unit = context.CreateChar())
                unit.CharacterMissionProgress.SetObjectiveState(
                    1,
                    321,
                    9,
                    (byte)MissionObjectiveState.Inactive,
                    (byte)MissionObjectiveState.NotAssigned);

            Assert.IsTrue(context.Manager.CompleteOfferedObjective(
                staleClient, objectiveNpc.EntityId, 321, 5, 11));

            var durable = context.ReadProgress(321).Missions[321];
            Assert.AreEqual((byte)MissionObjectiveState.Completed, durable.Objectives[5].State);
            Assert.AreEqual((byte)MissionObjectiveState.Incomplete, durable.Objectives[9].State);
            Assert.AreEqual(MissionObjectiveState.Completed,
                staleClient.Player.Missions[321].Objectives[5].State);
            Assert.AreEqual(MissionObjectiveState.Incomplete,
                staleClient.Player.Missions[321].Objectives[9].State);
            CollectionAssert.AreEqual(
                new[]
                {
                    typeof(ObjectiveCompletedPacket),
                    typeof(ObjectiveActivatedPacket)
                },
                MissionTestContext.Drain(staleClient)
                    .Where(packet => packet is not NPCConversationStatusPacket)
                    .Select(packet => packet.GetType()).ToArray());
        }

        [TestMethod]
        public void StaleClientCompletesPredecessorWhenSuccessorIsAlreadyCompleted()
        {
            using var context = MissionTestContext.WithObjectiveMission();
            var giver = context.AddNpc(77);
            var objectiveNpc = context.AddNpc(500, npcPackageId: 700);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
            context.Drain();
            var staleClient = context.CreateCompetingClient();
            using (var unit = context.CreateChar())
                unit.CharacterMissionProgress.SetObjectiveState(
                    1,
                    321,
                    9,
                    (byte)MissionObjectiveState.Inactive,
                    (byte)MissionObjectiveState.Completed);

            Assert.IsTrue(context.Manager.CompleteOfferedObjective(
                staleClient, objectiveNpc.EntityId, 321, 5, 11));

            var durable = context.ReadProgress(321).Missions[321];
            Assert.AreEqual((byte)MissionObjectiveState.Completed, durable.Objectives[5].State);
            Assert.AreEqual((byte)MissionObjectiveState.Completed, durable.Objectives[9].State);
            Assert.IsTrue(context.ReadMission(321).Completeable);
            Assert.AreEqual(MissionObjectiveState.Completed,
                staleClient.Player.Missions[321].Objectives[5].State);
            Assert.AreEqual(MissionObjectiveState.Completed,
                staleClient.Player.Missions[321].Objectives[9].State);
            Assert.IsTrue(staleClient.Player.Missions[321].Completeable);
            CollectionAssert.AreEqual(
                new[]
                {
                    typeof(ObjectiveCompletedPacket),
                    typeof(MissionCompleteablePacket)
                },
                MissionTestContext.Drain(staleClient)
                    .Where(packet => packet is not NPCConversationStatusPacket)
                    .Select(packet => packet.GetType()).ToArray());
        }

        [TestMethod]
        public void ObjectiveCompletionRejectsWrongBindingStaleStateAndDuplicateClick()
        {
            using var context = MissionTestContext.WithObjectiveMission();
            var giver = context.AddNpc(77);
            var wrongPackage = context.AddNpc(500, npcPackageId: 999);
            var correct = context.AddNpc(501, npcPackageId: 700);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
            context.Drain();

            Assert.IsFalse(context.Manager.CompleteOfferedObjective(
                context.Client, wrongPackage.EntityId, 321, 5, 11));
            Assert.IsFalse(context.Manager.CompleteOfferedObjective(
                context.Client, correct.EntityId, 321, 5, 12));
            Assert.IsTrue(context.Manager.CompleteOfferedObjective(
                context.Client, correct.EntityId, 321, 5, 11));
            context.Drain();
            Assert.IsFalse(context.Manager.CompleteOfferedObjective(
                context.Client, correct.EntityId, 321, 5, 11));
            Assert.AreEqual(0, context.Drain().Count);
        }

        [TestMethod]
        public void ObjectiveCompletionRejectsNpcFromAnotherMapAndStaleDurableState()
        {
            using var context = MissionTestContext.WithObjectiveMission();
            var giver = context.AddNpc(77);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
            context.Drain();
            var otherMap = new MapChannel
            {
                MapInfo = context.Map.MapInfo,
                ClientList = new List<Rasa.Game.Client>(),
                PlayerLimit = 128,
                InstanceId = context.Map.InstanceId + 1
            };
            var remote = context.AddNpc(500, otherMap, 700);
            Assert.IsFalse(context.Manager.CompleteOfferedObjective(
                context.Client, remote.EntityId, 321, 5, 11));

            using (var unit = context.CreateChar())
                unit.CharacterMissionProgress.SetObjectiveState(
                    1, 321, 5,
                    (byte)MissionObjectiveState.Incomplete,
                    (byte)MissionObjectiveState.Completed);
            var local = context.AddNpc(501, npcPackageId: 700);
            Assert.IsFalse(context.Manager.CompleteOfferedObjective(
                context.Client, local.EntityId, 321, 5, 11));
            Assert.AreEqual(MissionObjectiveState.Incomplete,
                context.Client.Player.Missions[321].Objectives[5].State);
            Assert.AreEqual(0, context.Drain().Count);
        }

        [TestMethod]
        public void ObjectiveCompletionFailureRollsBackAndReconnectHydratesPersistedProgress()
        {
            using var context = MissionTestContext.WithObjectiveMission();
            var giver = context.AddNpc(77);
            var objectiveNpc = context.AddNpc(500, npcPackageId: 700);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
            context.Drain();
            context.BeforeSave = _ => throw new DbUpdateException("Injected save failure.");

            Assert.IsFalse(context.Manager.CompleteOfferedObjective(
                context.Client, objectiveNpc.EntityId, 321, 5, 11));
            Assert.AreEqual(MissionObjectiveState.Incomplete,
                context.Client.Player.Missions[321].Objectives[5].State);
            Assert.AreEqual((byte)MissionObjectiveState.Incomplete,
                context.ReadProgress(321).Missions[321].Objectives[5].State);
            Assert.AreEqual(0, context.Drain().Count);

            context.BeforeSave = null;
            Assert.IsTrue(context.Manager.CompleteOfferedObjective(
                context.Client, objectiveNpc.EntityId, 321, 5, 11));
            context.Drain();
            context.Client.Player.Missions.Clear();
            context.ReloadPlayerMissions();
            Assert.AreEqual(MissionObjectiveState.Completed,
                context.Client.Player.Missions[321].Objectives[5].State);
            Assert.AreEqual(MissionObjectiveState.Incomplete,
                context.Client.Player.Missions[321].Objectives[9].State);
        }

        [TestMethod]
        public void CompetingClientsCannotCompleteTheSameObjectiveTwice()
        {
            using var context = MissionTestContext.WithObjectiveMission();
            var giver = context.AddNpc(77);
            var objectiveNpc = context.AddNpc(500, npcPackageId: 700);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
            context.Drain();
            var competitor = context.CreateCompetingClient();
            context.ResetCharUnitCount();
            using var start = new System.Threading.ManualResetEventSlim();

            var results = Task.WhenAll(
                Task.Run(() =>
                {
                    start.Wait();
                    return context.Manager.CompleteOfferedObjective(
                        context.Client, objectiveNpc.EntityId, 321, 5, 11);
                }),
                Task.Run(() =>
                {
                    start.Wait();
                    return context.Manager.CompleteOfferedObjective(
                        competitor, objectiveNpc.EntityId, 321, 5, 11);
                }));
            start.Set();
            var completed = results.GetAwaiter().GetResult();

            Assert.AreEqual(1, completed.Count(result => result));
            Assert.AreEqual((byte)MissionObjectiveState.Completed,
                context.ReadProgress(321).Missions[321].Objectives[5].State);
            Assert.AreEqual(1, context.Drain().Concat(MissionTestContext.Drain(competitor))
                .OfType<ObjectiveCompletedPacket>().Count());
        }

        [TestMethod]
        public void CompetingClientReconcilesAlreadyAppliedSuccessorWithoutDuplicatePackets()
        {
            using var context = MissionTestContext.WithObjectiveMission(
                includeCompetingObjective: true);
            var giver = context.AddNpc(77);
            var firstObjectiveNpc = context.AddNpc(500, npcPackageId: 700);
            var secondObjectiveNpc = context.AddNpc(501, npcPackageId: 702);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
            context.Drain();
            var competitor = context.CreateCompetingClient();

            Assert.IsTrue(context.Manager.CompleteOfferedObjective(
                context.Client, firstObjectiveNpc.EntityId, 321, 5, 11));
            context.Drain();
            Assert.AreEqual(MissionObjectiveState.Incomplete,
                context.Client.Player.Missions[321].Objectives[9].State);
            Assert.AreEqual(MissionObjectiveState.Inactive,
                competitor.Player.Missions[321].Objectives[9].State);
            using (var unit = context.CreateChar())
            {
                unit.CharacterMissionProgress.SetCounter(1, 321, 9, 0, 3, 7);
                unit.CharacterMissionProgress.SetItemCounter(1, 321, 9, 201, 2, 6);
            }

            Assert.IsTrue(context.Manager.CompleteOfferedObjective(
                competitor, secondObjectiveNpc.EntityId, 321, 6, 13));

            var durable = context.ReadProgress(321).Missions[321];
            Assert.AreEqual(
                (MissionObjectiveState)durable.Objectives[9].State,
                context.Client.Player.Missions[321].Objectives[9].State);
            Assert.AreEqual(
                (MissionObjectiveState)durable.Objectives[9].State,
                competitor.Player.Missions[321].Objectives[9].State);
            Assert.AreEqual(
                (MissionObjectiveState)durable.Objectives[6].State,
                competitor.Player.Missions[321].Objectives[6].State);
            Assert.AreEqual(
                durable.Objectives[9].Counters[0],
                competitor.Player.Missions[321].Objectives[9].Counters[0]);
            Assert.AreEqual(
                durable.Objectives[9].ItemCounters[201],
                competitor.Player.Missions[321].Objectives[9].ItemCounters[201]);
            var packets = MissionTestContext.Drain(competitor);
            Assert.AreEqual(1, packets.OfType<ObjectiveCompletedPacket>().Count());
            Assert.AreEqual(0, packets.OfType<ObjectiveRevealedPacket>().Count());
            Assert.AreEqual(0, packets.OfType<ObjectiveActivatedPacket>().Count());
        }

        [TestMethod]
        public void HydrationCombinesDurableValuesWithImmutableDefinitionMetadata()
        {
            using var context = MissionTestContext.WithObjectiveMission();
            var giver = context.AddNpc(77);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
            context.Drain();
            using (var unit = context.CreateChar())
            {
                unit.CharacterMissionProgress.SetCounter(1, 321, 5, 0, 2, 6);
                unit.CharacterMissionProgress.SetItemCounter(1, 321, 5, 200, 1, 4);
            }
            context.ReloadPlayerMissions();

            var info = context.Manager.BuildStatusSnapshot(context.Client.Player)[321];
            var objective = info.ObjectivesList.Single(item => item.ObjectiveId == 5);
            Assert.AreEqual(7U, objective.Ordinal);
            Assert.IsTrue(objective.IsRequired);
            Assert.AreEqual(6U, objective.Counters[0].CounterValue);
            Assert.AreEqual(2U, objective.Counters[0].InitialValue);
            Assert.AreEqual(10U, objective.Counters[0].TargetValue);
            Assert.AreEqual(4U, objective.ItemCounters[200].CounterValue);
            Assert.AreEqual(8U, objective.ItemCounters[200].TargetValue);
            Assert.AreEqual(new System.Numerics.Vector3(1.25f, 2.5f, 3.75f),
                objective.IndicatorList.Single().Position);
        }

        [TestMethod]
        public void NpcConversationReflectsMissionObjectiveCompletionAndRewardStates()
        {
            using var context = MissionTestContext.WithObjectiveMission(selectableReward: false);
            var giver = context.AddNpc(77);
            var firstObjectiveNpc = context.AddNpc(500, npcPackageId: 700);
            var secondObjectiveNpc = context.AddNpc(501, npcPackageId: 701);
            var receiver = context.AddNpc(88);
            var npcManager = CreateNpcManager(context, out var singleton, out var previous);
            try
            {
                npcManager.RequestNpcConverse(context.Client,
                    new RequestNPCConversePacket { EntityId = giver.EntityId });
                Assert.IsTrue(context.Drain().OfType<ConversePacket>().Single()
                    .ConvoDataDict.ContainsKey(ConversationType.MissionDispense));

                Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
                context.Drain();
                npcManager.RequestNpcConverse(context.Client,
                    new RequestNPCConversePacket { EntityId = firstObjectiveNpc.EntityId });
                Assert.IsTrue(context.Drain().OfType<ConversePacket>().Single()
                    .ConvoDataDict.ContainsKey(ConversationType.ObjectiveComplete));

                Assert.IsTrue(context.Manager.CompleteOfferedObjective(
                    context.Client, firstObjectiveNpc.EntityId, 321, 5, 11));
                context.Drain();
                Assert.IsTrue(context.Manager.CompleteOfferedObjective(
                    context.Client, secondObjectiveNpc.EntityId, 321, 9, 12));
                Assert.IsTrue(context.Client.Player.Missions[321].Completeable);
                context.Drain();
                npcManager.RequestNpcConverse(context.Client,
                    new RequestNPCConversePacket { EntityId = receiver.EntityId });
                Assert.IsTrue(context.Drain().OfType<ConversePacket>().Single()
                    .ConvoDataDict.ContainsKey(ConversationType.MissionComplete));

                using (var unit = context.CreateChar())
                    unit.CharacterMissions.SetState(1, 321, (uint)MissionState.Success);
                context.ReloadPlayerMissions();
                npcManager.RequestNpcConverse(context.Client,
                    new RequestNPCConversePacket { EntityId = receiver.EntityId });
                Assert.IsTrue(context.Drain().OfType<ConversePacket>().Single()
                    .ConvoDataDict.ContainsKey(ConversationType.MissionComplete));
            }
            finally
            {
                singleton.SetValue(null, previous);
            }
        }

        [TestMethod]
        public void NpcConversationIgnoresNpcInAnotherPrivate1985Instance()
        {
            using var context = MissionTestContext.WithObjectiveMission(selectableReward: false);
            CreatePrivateBootcampMaps(context, out _, out var second);
            var npc = context.AddNpc(88, second);
            var npcManager = CreateNpcManager(context, out var singleton, out var previous);
            try
            {
                npcManager.RequestNpcConverse(context.Client,
                    new RequestNPCConversePacket { EntityId = npc.EntityId });

                Assert.AreEqual(0, context.Drain().Count);
            }
            finally
            {
                singleton.SetValue(null, previous);
            }
        }

        [TestMethod]
        public void CompletingNpcObjectiveRequiresNpcFromPlayersPrivate1985Instance()
        {
            using var context = MissionTestContext.WithObjectiveMission();
            CreatePrivateBootcampMaps(context, out var first, out var second);
            var giver = context.AddNpc(77, first);
            var objectiveNpc = context.AddNpc(500, second, npcPackageId: 700);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
            context.Drain();

            Assert.IsFalse(context.Manager.CompleteOfferedObjective(
                context.Client,
                objectiveNpc.EntityId,
                321,
                5,
                11));

            Assert.AreEqual(MissionObjectiveState.Incomplete,
                context.Client.Player.Missions[321].Objectives[5].State);
            Assert.AreEqual((byte)MissionObjectiveState.Incomplete,
                context.ReadProgress(321).Missions[321].Objectives[5].State);
            Assert.AreEqual(0, context.Drain().Count);
        }

        [TestMethod]
        public void MissionConversationClassificationProjectsDataAndStatusFromOneResult()
        {
            using var context = MissionTestContext.WithObjectiveMission(selectableReward: false);
            var giver = context.AddNpc(77);
            var objectiveNpc = context.AddNpc(500, npcPackageId: 700);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
            context.Drain();

            var classification = context.Manager.ClassifyNpcConversation(
                context.Client.Player,
                objectiveNpc);
            var data = classification.CreateConversationData();

            Assert.IsTrue(data.ContainsKey(ConversationType.ObjectiveComplete));
            Assert.IsTrue(classification.TryGetStatus(out var status, out var missionIds));
            Assert.AreEqual(ConversationStatus.ObjectivComplete, status);
            CollectionAssert.AreEqual(new uint[] { 321 }, missionIds);
        }

        [TestMethod]
        public void CompletableMissionConversationDoesNotRequireARewardDefinition()
        {
            using var context = MissionTestContext.WithDefinitions(321);
            context.SeedMission(1, 321, (uint)MissionState.Active, true);
            context.ReloadPlayerMissions();
            var receiver = context.AddNpc(88);
            var classification = context.Manager.ClassifyNpcConversation(
                context.Client.Player,
                receiver);

            Assert.IsTrue(classification.TryGetStatus(out var status, out var missionIds));
            Assert.AreEqual(ConversationStatus.MissionComplete, status);
            CollectionAssert.AreEqual(new uint[] { 321 }, missionIds);
            var classifiedMissions =
                (Dictionary<uint, RewardInfo>)classification.CreateConversationData()[
                    ConversationType.MissionComplete];
            Assert.AreEqual(0, classifiedMissions[321].FixedReward.Credits.Count);
            Assert.AreEqual(0, classifiedMissions[321].FixedReward.FixedItems.Count);
            Assert.AreEqual(0, classifiedMissions[321].SelectableReward.Count);

            var npcManager = CreateNpcManager(context, out var singleton, out var previous);
            try
            {
                npcManager.RequestNpcConverse(context.Client,
                    new RequestNPCConversePacket { EntityId = receiver.EntityId });
                var conversation = context.Drain().OfType<ConversePacket>().Single();
                var publishedMissions =
                    (Dictionary<uint, RewardInfo>)conversation.ConvoDataDict[
                        ConversationType.MissionComplete];
                Assert.AreEqual(0, publishedMissions[321].FixedReward.Credits.Count);
                Assert.AreEqual(0, publishedMissions[321].FixedReward.FixedItems.Count);
                Assert.AreEqual(0, publishedMissions[321].SelectableReward.Count);
            }
            finally
            {
                singleton.SetValue(null, previous);
            }
        }

        [TestMethod]
        public void RecoveredSourceOnlyMissionsNeverAppearInNpcConversation()
        {
            using var context = MissionTestContext.WithRecoveredDefinitions();
            var npc = context.AddNpc(88, npcPackageId: 168);
            npc.Npc.NpcMissionIds = new List<uint> { 1069, 1407, 1449 };
            var npcManager = CreateNpcManager(context, out var singleton, out var previous);
            try
            {
                npcManager.RequestNpcConverse(context.Client,
                    new RequestNPCConversePacket { EntityId = npc.EntityId });
                var conversation = context.Drain().OfType<ConversePacket>().Single();
                Assert.IsFalse(conversation.ConvoDataDict.Keys.Any(type =>
                    type is ConversationType.MissionDispense or
                        ConversationType.ObjectiveComplete or
                        ConversationType.MissionComplete or
                        ConversationType.MissionReward));

                npcManager.UpdateConversationStatus(context.Client, npc);
                Assert.AreEqual(ConversationStatus.None,
                    context.Drain().OfType<NPCConversationStatusPacket>().Single().ConvoStatusId);
            }
            finally
            {
                singleton.SetValue(null, previous);
            }
        }

        [TestMethod]
        public void AcceptingNpcMissionPersistsActiveStateBeforePublishingTheDelta()
        {
            using var context = MissionTestContext.WithDefinitions(321);
            var npc = context.AddNpc(77);

            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, npc.EntityId, 321));

            var runtime = context.Client.Player.Missions[321];
            Assert.AreEqual(MissionState.Active, runtime.State);
            Assert.IsFalse(runtime.Completeable);
            using (var unit = context.CreateChar())
            {
                var durable = unit.CharacterMissions.GetByCharacterAndMission(
                    context.Client.Player.Id, 321);
                Assert.IsNotNull(durable);
                Assert.AreEqual((uint)MissionState.Active, durable.MissionState);
                Assert.IsFalse(durable.Completeable);
            }
            var gained = context.Drain().OfType<MissionGainedPacket>().Single();
            Assert.AreEqual(MissionState.Active, gained.MissionInfo.MissionState);
            Assert.IsFalse(gained.MissionInfo.Completeable);
        }

        [TestMethod]
        public void AcceptanceRejectsWrongNpcAndNpcFromAnotherMapInstance()
        {
            using var context = MissionTestContext.WithDefinitions(321);
            var wrongNpc = context.AddNpc(66);
            var otherMap = new MapChannel
            {
                MapInfo = context.Map.MapInfo,
                ClientList = new List<Rasa.Game.Client>(),
                PlayerLimit = 128,
                InstanceId = context.Map.InstanceId + 1
            };
            var remoteNpc = context.AddNpc(77, otherMap);

            Assert.IsFalse(context.Manager.AcceptOfferedMission(context.Client, wrongNpc.EntityId, 321));
            Assert.IsFalse(context.Manager.AcceptOfferedMission(context.Client, remoteNpc.EntityId, 321));

            Assert.AreEqual(0, context.Client.Player.Missions.Count);
            Assert.AreEqual(0, context.Drain().OfType<MissionGainedPacket>().Count());
        }

        [TestMethod]
        public void DuplicateAcceptanceDoesNotPersistOrPublishTwice()
        {
            using var context = MissionTestContext.WithDefinitions(321);
            var npc = context.AddNpc(77);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, npc.EntityId, 321));
            context.Drain();

            Assert.IsFalse(context.Manager.AcceptOfferedMission(context.Client, npc.EntityId, 321));

            using var unit = context.CreateChar();
            Assert.AreEqual(1, unit.CharacterMissions.Get(context.Client.Player.Id).Count);
            Assert.AreEqual(0, context.Drain().OfType<MissionGainedPacket>().Count());
        }

        [TestMethod]
        public void MissionLogAtExactlyThirtyRejectsAnotherMission()
        {
            using var context = MissionTestContext.WithDefinitions(321);
            var npc = context.AddNpc(77);
            for (uint id = 1; id <= 30; id++)
            {
                context.Client.Player.Missions.Add(id, new MissionLog(id, MissionState.Active, false));
                context.SeedMission(context.Client.Player.Id, id, (uint)MissionState.Active, false);
            }

            Assert.IsFalse(context.Manager.AcceptOfferedMission(context.Client, npc.EntityId, 321));

            Assert.AreEqual(30, context.Client.Player.Missions.Count);
            Assert.AreEqual(0, context.Drain().OfType<MissionGainedPacket>().Count());
        }

        [TestMethod]
        public void DurableUnknownMissionsCountTowardAcceptanceCapacity()
        {
            using var context = MissionTestContext.WithDefinitions(321);
            var npc = context.AddNpc(77);
            for (uint id = 1000; id < 1030; id++)
                context.SeedMission(context.Client.Player.Id, id, (uint)MissionState.Active, false);

            Assert.AreEqual(0, context.Client.Player.Missions.Count);
            Assert.IsFalse(context.Manager.AcceptOfferedMission(context.Client, npc.EntityId, 321));

            using var unit = context.CreateChar();
            Assert.AreEqual(30, unit.CharacterMissions.Get(context.Client.Player.Id).Count);
            Assert.IsNull(unit.CharacterMissions.GetByCharacterAndMission(
                context.Client.Player.Id, 321));
            Assert.AreEqual(0, context.Drain().OfType<MissionGainedPacket>().Count());
        }

        [TestMethod]
        public void CompetingDistinctClientsCannotExceedDurableMissionCapacity()
        {
            using var context = MissionTestContext.WithDefinitions(321, 429);
            var npc = context.AddNpc(77);
            for (uint id = 1000; id < 1029; id++)
                context.SeedMission(context.Client.Player.Id, id, (uint)MissionState.Active, false);
            var competitor = context.CreateCompetingClient();

            var results = Task.WhenAll(
                Task.Factory.StartNew(
                    () => context.Manager.AcceptOfferedMission(context.Client, npc.EntityId, 321),
                    TaskCreationOptions.LongRunning),
                Task.Factory.StartNew(
                    () => context.Manager.AcceptOfferedMission(competitor, npc.EntityId, 429),
                    TaskCreationOptions.LongRunning))
                .GetAwaiter().GetResult();
            Assert.AreEqual(1, results.Count(result => result));
            using var unit = context.CreateChar();
            Assert.AreEqual(30, unit.CharacterMissions.Get(context.Client.Player.Id).Count);
            Assert.AreEqual(results[0], context.Client.Player.Missions.ContainsKey(321));
            Assert.AreEqual(results[1], competitor.Player.Missions.ContainsKey(429));
        }

        [TestMethod]
        public void AcceptancePersistenceFailureLeavesRuntimeAndPacketsUnchanged()
        {
            using var context = MissionTestContext.WithDefinitions(321);
            var npc = context.AddNpc(77);
            context.BeforeSave = _ => throw new DbUpdateException("fixture failure");

            Assert.IsFalse(context.Manager.AcceptOfferedMission(context.Client, npc.EntityId, 321));

            Assert.AreEqual(0, context.Client.Player.Missions.Count);
            Assert.AreEqual(0, context.Drain().OfType<MissionGainedPacket>().Count());
        }

        [TestMethod]
        public void AcceptanceDoesNotHideProgrammingErrors()
        {
            using var context = MissionTestContext.WithDefinitions(321);
            var npc = context.AddNpc(77);
            var expected = new InvalidOperationException("fixture programming error");
            context.BeforeSave = _ => throw expected;

            Assert.AreSame(expected, Assert.ThrowsExactly<InvalidOperationException>(
                () => context.Manager.AcceptOfferedMission(context.Client, npc.EntityId, 321)));

            Assert.AreEqual(0, context.Client.Player.Missions.Count);
            Assert.AreEqual(0, context.Drain().OfType<MissionGainedPacket>().Count());
        }

        [TestMethod]
        public void AbandoningActiveMissionRemovesDurableAndRuntimeStateBeforePublishing()
        {
            using var context = MissionTestContext.WithDefinitions(321);
            var npc = context.AddNpc(77);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, npc.EntityId, 321));
            context.Drain();

            Assert.IsTrue(context.Manager.TryAbandon(context.Client, 321));

            Assert.IsFalse(context.Client.Player.Missions.ContainsKey(321));
            using (var unit = context.CreateChar())
                Assert.IsNull(unit.CharacterMissions.GetByCharacterAndMission(
                    context.Client.Player.Id, 321));
            Assert.AreEqual(321U, context.Drain().OfType<MissionDiscardedPacket>().Single().MissionId);

            context.ReloadPlayerMissions();
            Assert.IsFalse(context.Client.Player.Missions.ContainsKey(321));
        }

        [TestMethod]
        public void StaleClientCannotAbandonAnotherClientsCompletedMission()
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            var staleClient = context.CreateCompetingClient();

            Assert.IsTrue(context.Manager.CompleteOfferedMission(
                context.Client, context.Receiver.EntityId, 429, 0));
            var rewarded = context.ReadRewardTotals();

            Assert.IsFalse(context.Manager.TryAbandon(staleClient, 429));
            Assert.IsFalse(context.Manager.CompleteOfferedMission(
                staleClient, context.Receiver.EntityId, 429, 0));

            var durable = context.ReadMission(429);
            Assert.AreEqual((uint)MissionState.Completed, durable.MissionState);
            Assert.IsFalse(durable.Completeable);
            Assert.AreEqual(rewarded, context.ReadRewardTotals());
            Assert.AreEqual(0, MissionTestContext.Drain(staleClient)
                .OfType<MissionDiscardedPacket>().Count());
        }

        [TestMethod]
        public void CharacterDeletionRemovesRestrictiveMissionRowsInTheSameOperation()
        {
            using var context = MissionTestContext.WithDefinitions(321);
            context.SeedMission(context.Client.Player.Id, 321, (uint)MissionState.Completed, false);
            context.Client.State = RasaGame::Rasa.Data.ClientState.LoggedIn;

            new Rasa.Managers.CharacterManager(context).RequestDeleteCharacterInSlot(
                context.Client, new RequestDeleteCharacterInSlotPacket { Slot = 0 });

            using var unit = context.CreateChar();
            Assert.AreEqual(0, unit.CharacterMissions.Get(context.Client.Player.Id).Count);
            Assert.AreEqual(0, unit.CharacterMissionProgress.Get(context.Client.Player.Id).Missions.Count);
            Assert.ThrowsExactly<EntityNotFoundException>(() =>
                unit.Characters.Get(context.Client.Player.Id));
            Assert.AreEqual(1, context.Drain().OfType<CharacterDeleteSuccessPacket>().Count());
        }

        [TestMethod]
        public void ExplicitlyDisabledDatabaseDefinitionsAreNeitherAdvertisedNorAccepted()
        {
            using var context = MissionTestContext.WithDatabaseDefinitions(321, 429);
            var npc = context.AddNpc(77);
            npc.Npc.NpcMissionIds = new List<uint> { 321, 429 };
            var singleton = typeof(MissionApplication).GetField(
                "_instance", BindingFlags.Static | BindingFlags.NonPublic)!;
            var previous = singleton.GetValue(null);
            singleton.SetValue(null, context.Manager);
            try
            {
                var npcManager = (NpcManager)Activator.CreateInstance(
                    typeof(NpcManager),
                    BindingFlags.Instance | BindingFlags.NonPublic,
                    binder: null,
                    args: new object[] { context },
                    culture: null)!;

                npcManager.UpdateConversationStatus(context.Client, npc);
                var status = context.Drain().OfType<NPCConversationStatusPacket>().Single();
                Assert.AreEqual(ConversationStatus.None, status.ConvoStatusId);
                Assert.AreEqual(0, status.Data.Count);

                npcManager.RequestNpcConverse(context.Client,
                    new RequestNPCConversePacket { EntityId = npc.EntityId });
                var conversation = context.Drain().OfType<ConversePacket>().Single();
                Assert.IsFalse(conversation.ConvoDataDict.ContainsKey(ConversationType.MissionDispense));
                Assert.IsFalse(conversation.ConvoDataDict.ContainsKey(ConversationType.MissionComplete));
                Assert.IsFalse(context.Manager.AcceptOfferedMission(
                    context.Client, npc.EntityId, 321));
            }
            finally
            {
                singleton.SetValue(null, previous);
            }
        }

        [TestMethod]
        public void MissionDefinitionsAreImmutableAndNeverCarryCharacterState()
        {
            using var context = MissionTestContext.WithDefinitions(321);
            var definition = context.Manager.LoadedMissions[321];
            var npc = context.AddNpc(77);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, npc.EntityId, 321));
            var gained = context.Drain().OfType<MissionGainedPacket>().Single();

            gained.MissionInfo.MissionState = MissionState.Completed;

            Assert.IsFalse(typeof(MissionInfo).IsAssignableFrom(typeof(Mission)));
            Assert.IsFalse(typeof(Mission).GetProperty(nameof(Mission.MissionId))!.CanWrite);
            Assert.AreEqual(MissionState.Active,
                context.Manager.BuildStatusSnapshot(context.Client.Player)[321].MissionState);
            Assert.AreEqual(77U, definition.MissionGiver);
        }

        [TestMethod]
        [DynamicData(nameof(GetPrerequisiteKinds))]
        public void NpcConversationAdvertisingHonorsMissionPrerequisites(
            MissionPrerequisiteKind kind)
        {
            var fixture = CreateSinglePrerequisiteFixture(kind);
            using var context = MissionTestContext.WithCustomDefinitions(
                new Dictionary<uint, Mission>());
            var manager = CreateMissionContentManager(context, fixture);
            var giver = context.AddNpc(101);

            var blocked = manager.ClassifyNpcConversation(context.Client.Player, giver);
            Assert.IsFalse(blocked.TryGetStatus(out var blockedStatus, out var blockedIds));
            Assert.AreEqual(ConversationStatus.None, blockedStatus);
            CollectionAssert.AreEqual(Array.Empty<uint>(), blockedIds);

            SatisfyPrerequisite(context, kind);

            var allowed = manager.ClassifyNpcConversation(context.Client.Player, giver);
            Assert.IsTrue(allowed.TryGetStatus(out var allowedStatus, out var allowedIds));
            Assert.AreEqual(ConversationStatus.Available, allowedStatus);
            CollectionAssert.AreEqual(new uint[] { 321 }, allowedIds);
        }

        [TestMethod]
        [DynamicData(nameof(GetPrerequisiteKinds))]
        public void MissionAcceptanceHonorsMissionPrerequisites(
            MissionPrerequisiteKind kind)
        {
            var fixture = CreateSinglePrerequisiteFixture(kind);
            using var blockedContext = MissionTestContext.WithCustomDefinitions(
                new Dictionary<uint, Mission>());
            var blockedManager = CreateMissionContentManager(blockedContext, fixture);
            var blockedGiver = blockedContext.AddNpc(101);

            Assert.IsFalse(blockedManager.AcceptOfferedMission(
                blockedContext.Client,
                blockedGiver.EntityId,
                321));
            Assert.IsFalse(blockedContext.Client.Player.Missions.ContainsKey(321));

            using var allowedContext = MissionTestContext.WithCustomDefinitions(
                new Dictionary<uint, Mission>());
            var allowedManager = CreateMissionContentManager(allowedContext, fixture);
            var allowedGiver = allowedContext.AddNpc(101);
            SatisfyPrerequisite(allowedContext, kind);

            Assert.IsTrue(allowedManager.AcceptOfferedMission(
                allowedContext.Client,
                allowedGiver.EntityId,
                321));
            Assert.IsTrue(allowedContext.Client.Player.Missions.ContainsKey(321));
        }

        [TestMethod]
        public void MissionAcceptanceRechecksDurablePrerequisitesBeforePersisting()
        {
            var fixture = CreateSinglePrerequisiteFixture(MissionPrerequisiteKind.MissionCompleted);
            using var context = MissionTestContext.WithCustomDefinitions(
                new Dictionary<uint, Mission>());
            var manager = CreateMissionContentManager(context, fixture);
            var giver = context.AddNpc(101);
            context.Client.Player.Missions[322] = new MissionLog(
                322,
                MissionState.Completed,
                false);

            Assert.IsTrue(manager.ClassifyNpcConversation(context.Client.Player, giver)
                .CreateConversationData()
                .ContainsKey(ConversationType.MissionDispense));
            Assert.IsFalse(manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
            Assert.IsFalse(context.Client.Player.Missions.ContainsKey(321));
        }

        public static IEnumerable<object[]> GetPrerequisiteKinds()
        {
            yield return new object[] { MissionPrerequisiteKind.MissionCompleted };
            yield return new object[] { MissionPrerequisiteKind.MissionAccepted };
            yield return new object[] { MissionPrerequisiteKind.PlayerLevelAtLeast };
            yield return new object[] { MissionPrerequisiteKind.PlayerFlagValue };
        }

        private static MissionContentFixture CreateSinglePrerequisiteFixture(
            MissionPrerequisiteKind kind)
        {
            var fixture = MissionContentFixture.CreateValid();
            var prerequisite = new MissionPrerequisiteEntry
            {
                MissionId = 321,
                ContentRevision = "deployment_11",
                PrerequisiteId = 1,
                Requirement = MissionContentRequirement.Required,
                Kind = kind,
                Comment = $"Prerequisite {kind}"
            };

            switch (kind)
            {
                case MissionPrerequisiteKind.MissionCompleted:
                    prerequisite.RequiredMissionId = 322;
                    prerequisite.RequiredMissionState = (byte)MissionState.Completed;
                    break;
                case MissionPrerequisiteKind.MissionAccepted:
                    prerequisite.RequiredMissionId = 322;
                    break;
                case MissionPrerequisiteKind.PlayerLevelAtLeast:
                    prerequisite.RequiredLevel = 2;
                    break;
                case MissionPrerequisiteKind.PlayerFlagValue:
                    prerequisite.PlayerFlagId = 9;
                    prerequisite.PlayerFlagValue = 3;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind));
            }

            fixture.Prerequisites.Add(prerequisite);
            return fixture;
        }

        private static void SatisfyPrerequisite(
            MissionTestContext context,
            MissionPrerequisiteKind kind)
        {
            switch (kind)
            {
                case MissionPrerequisiteKind.MissionCompleted:
                    context.SeedMission(
                        context.Client.Player.Id,
                        322,
                        (uint)MissionState.Completed,
                        false);
                    context.Client.Player.Missions[322] = new MissionLog(
                        322,
                        MissionState.Completed,
                        false);
                    break;
                case MissionPrerequisiteKind.MissionAccepted:
                    context.SeedMission(
                        context.Client.Player.Id,
                        322,
                        (uint)MissionState.Active,
                        false);
                    context.Client.Player.Missions[322] = new MissionLog(
                        322,
                        MissionState.Active,
                        false);
                    break;
                case MissionPrerequisiteKind.PlayerLevelAtLeast:
                    using (var unit = context.CreateChar())
                        unit.Characters.UpdateCharacterLevel(context.Client.Player.Id, 2);
                    context.Client.Player.Level = 2;
                    break;
                case MissionPrerequisiteKind.PlayerFlagValue:
                    using (var unit = context.CreateChar())
                        unit.CharacterFlags.Set(context.Client.Player.Id, 9, 3);
                    context.Client.Player.PlayerFlags[9] = 3;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }

        private static MissionApplication CreateMissionContentManager(
            MissionTestContext context,
            MissionContentFixture fixture)
        {
            var manager = new MissionApplication(
                new MissionContentLoadingFactory(context, fixture.CreateWorldUnitOfWork()),
                new Dictionary<uint, Mission>());
            manager.LoadMissions();
            return manager;
        }

        private static NpcManager CreateNpcManager(
            MissionTestContext context,
            out FieldInfo singleton,
            out object previous)
        {
            singleton = typeof(MissionApplication).GetField(
                "_instance", BindingFlags.Static | BindingFlags.NonPublic)!;
            previous = singleton.GetValue(null);
            singleton.SetValue(null, context.Manager);
            return (NpcManager)Activator.CreateInstance(
                typeof(NpcManager),
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                args: new object[] { context, context.Manager },
                culture: null)!;
        }

        private static MapChannelManager CreatePrivateBootcampMaps(
            MissionTestContext context,
            out MapChannel first,
            out MapChannel second)
        {
            context.Map.MapInfo = new MapInfo(1985, "bootcamp_fixture", 1556, 0);
            context.Client.Player.MapContextId = 1985;
            var maps = new MapChannelManager(null, privateInstances: new PrivateMapInstanceService());
            maps.MapChannelArray.Add(1985, context.Map);
            first = maps.GetOrCreatePrivateInstance(1985, context.Client.Player.Id);
            second = maps.GetOrCreatePrivateInstance(1985, context.Client.Player.Id + 1);

            CellManager.Instance.RemoveFromWorld(context.Client);
            context.Map.ClientList.Remove(context.Client);
            context.Client.Player.MapChannel = first;
            context.Client.Player.MapContextId = 1985;
            first.ClientList.Add(context.Client);
            CellManager.Instance.AddToWorld(context.Client);
            context.Drain();
            return maps;
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
