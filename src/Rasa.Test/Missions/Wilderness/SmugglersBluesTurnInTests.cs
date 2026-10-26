using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Packets.MapChannel.Server;
using Rasa.Structures;

namespace Rasa.Test.Missions.Wilderness
{
    [TestClass, DoNotParallelize]
    public class SmugglersBluesTurnInTests
    {
        [TestMethod]
        [DataRow("123")]
        [DataRow("132")]
        [DataRow("213")]
        [DataRow("231")]
        [DataRow("312")]
        [DataRow("321")]
        public void ThreeDeliveriesWithoutRelogOfferMooresReturnDialogueAndReward(string order)
        {
            using var h = WildernessTwinPillarsTests.Create();
            h.SpawnWorld(209, 193, 182, 510005);
            var moore = h.Npc(209);
            WildernessTwinPillarsTests.Accept(h, moore, 623);
            foreach (var delivery in order)
            {
                var (spawn, objective) = delivery switch
                {
                    '1' => (193U, 1U), '2' => (182U, 2U), '3' => (510005U, 3U),
                    _ => throw new System.InvalidOperationException()
                };
                WildernessTwinPillarsTests.Talk(h, h.Npc(spawn), 623, objective);
                h.Tick();
            }
            Assert.AreEqual(MissionObjectiveState.Completed, h.Client.Player.Missions[623].Objectives[5].State);
            Assert.AreEqual(MissionObjectiveState.Incomplete, h.Client.Player.Missions[623].Objectives[4].State);
            h.MoveTo(moore.Position);
            h.Drain();
            new NpcManager(h, h.Manager).UpdateConversationStatus(h.Client, moore);
            var status = h.Drain().OfType<NPCConversationStatusPacket>().Single();
            Assert.AreEqual(ConversationStatus.ObjectivComplete, status.ConvoStatusId);
            Assert.IsTrue(status.Data.Contains(623));
            var conversation = WildernessTwinPillarsTests.OpenNativeNpcConversation(h, moore);
            Assert.IsTrue(conversation.ConvoDataDict.TryGetValue(ConversationType.ObjectiveComplete, out var topics));
            Assert.IsTrue(((List<CompleteableObjectives>)topics).Any(topic => topic.MissionId == 623 && topic.ObjectiveId == 4));
            WildernessTwinPillarsTests.Talk(h, moore, 623, 4);
            WildernessTwinPillarsTests.Reward(h, moore, 623, null, 44919, 7000, 10000);
            Assert.AreEqual(MissionState.Completed, h.Client.Player.Missions[623].State);
        }

        [TestMethod]
        [DataRow(false, false)]
        [DataRow(true, false)]
        [DataRow(true, true)]
        public void ReconnectRecoversCompletedDeliveriesWhoseMooreStageWasNotSaved(bool readyCheckpoint, bool returnAlreadyCompleted)
        {
            using var h = WildernessTwinPillarsTests.Create();
            h.SpawnWorld(209, 193, 182, 510005);
            var moore = h.Npc(209);
            WildernessTwinPillarsTests.Accept(h, moore, 623);
            foreach (var (spawn, objective) in new[] { (193U, 1U), (182U, 2U), (510005U, 3U) })
                WildernessTwinPillarsTests.Talk(h, h.Npc(spawn), 623, objective);
            h.Manager.Scenes.Detach(h.Client, h.Map);
            // Simulate a stuck saved shape: real deliveries are complete, but the
            // hidden batch/return stage was lost. Existing operation receipts are retained.
            using (var unit = h.CreateChar())
                unit.ExecuteTransaction(() =>
                {
                    var scene = unit.CharacterMissions.Runtime.Scenes(h.Client.Player.Id, 623).Single();
                    scene.Checkpoint = readyCheckpoint ? "{\"Deliveries\":7,\"Ready\":true}" : "{\"Deliveries\":3,\"Ready\":false}";
                    scene.Status = "Running"; scene.Version++;
                    unit.CharacterMissionProgress.Get(h.Client.Player.Id, 623, 4).ObjectiveState =
                        (byte)(returnAlreadyCompleted ? MissionObjectiveState.Completed : MissionObjectiveState.Inactive);
                    unit.CharacterMissionProgress.Get(h.Client.Player.Id, 623, 5).ObjectiveState = (byte)MissionObjectiveState.Incomplete;
                    unit.CharacterMissions.GetByCharacterAndMission(h.Client.Player.Id, 623).Completeable = false;
                });
            var before = h.Context.ReadRewardTotals();
            WildernessTwinPillarsTests.Reload(h);
            Assert.AreEqual(before, h.Context.ReadRewardTotals(), "Recovery must not pay the reward automatically.");
            Assert.AreEqual(MissionObjectiveState.Completed, h.Client.Player.Missions[623].Objectives[5].State);
            Assert.AreEqual(returnAlreadyCompleted ? MissionObjectiveState.Completed : MissionObjectiveState.Incomplete,
                h.Client.Player.Missions[623].Objectives[4].State);
            h.Manager.Scenes.Detach(h.Client, h.Map);
            WildernessTwinPillarsTests.Reload(h);
            Assert.AreEqual(before, h.Context.ReadRewardTotals(), "Repeated recovery must not duplicate or prepay a reward.");
            if (!returnAlreadyCompleted) WildernessTwinPillarsTests.Talk(h, moore, 623, 4);
            WildernessTwinPillarsTests.Reward(h, moore, 623, null, 44919, 7000, 10000);
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(1)]
        [DataRow(2)]
        public void RecoveryRequiresRealCompletedDeliveriesEvenIfTheCheckpointClaimsReady(int delivered)
        {
            using var h = WildernessTwinPillarsTests.Create();
            h.SpawnWorld(209, 193, 182, 510005);
            var moore = h.Npc(209);
            var recipients = new[] { 193U, 182U, 510005U };
            WildernessTwinPillarsTests.Accept(h, moore, 623);
            for (var i = 0; i < delivered; i++)
                WildernessTwinPillarsTests.Talk(h, h.Npc(recipients[i]), 623, (uint)i + 1);
            h.Manager.Scenes.Detach(h.Client, h.Map);
            using (var unit = h.CreateChar())
                unit.ExecuteTransaction(() =>
                {
                    var scene = unit.CharacterMissions.Runtime.Scenes(h.Client.Player.Id, 623).Single();
                    scene.Checkpoint = "{\"Deliveries\":7,\"Ready\":true}";
                    scene.Status = "Running"; scene.Version++;
                });
            var before = h.Context.ReadRewardTotals();
            WildernessTwinPillarsTests.Reload(h);
            Assert.AreEqual(before, h.Context.ReadRewardTotals());
            Assert.AreEqual(3 - delivered, WildernessTwinPillarsTests.Held(h, 2226));
            Assert.AreEqual(MissionObjectiveState.Inactive, h.Client.Player.Missions[623].Objectives[4].State);
            Assert.AreEqual(MissionObjectiveState.Incomplete, h.Client.Player.Missions[623].Objectives[5].State);
            Assert.IsFalse(h.Manager.CompleteOfferedMission(h.Client, moore.EntityId, 623, null));
            for (var i = delivered; i < 3; i++)
                WildernessTwinPillarsTests.Talk(h, h.Npc(recipients[i]), 623, (uint)i + 1);
            WildernessTwinPillarsTests.Talk(h, moore, 623, 4);
            WildernessTwinPillarsTests.Reward(h, moore, 623, null, 44919, 7000, 10000);
        }
    }
}
