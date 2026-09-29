using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Packets.MapChannel.Client;
using Rasa.Structures;
using static Rasa.Test.Missions.Wilderness.WildernessAliaBranchesTests;

namespace Rasa.Test.Missions.Wilderness
{
    [TestClass]
    [DoNotParallelize]
    public class WildernessLogosTests
    {
        [TestMethod]
        [DataRow(1638U, 2U, 10U, 2500U, 1500)]
        [DataRow(1640U, 4U, 1U, 3000U, 1500)]
        [DataRow(921U, 5U, 24U, 3500U, 1500)]
        [DataRow(907U, 1U, 6U, 3000U, 600)]
        [DataRow(909U, 1U, 28U, 3000U, 600)]
        [DataRow(1639U, 6U, 23U, 2500U, 600)]
        [DataRow(1633U, 3U, 2U, 4000U, 100)]
        [DataRow(911U, 3U, 56U, 4000U, 1500)]
        public void EachDispatchUsesItsRealShrineAndRewardsOnlyOnce(
            uint missionId, uint objectiveId, uint logosId, uint experience, int credits)
        {
            using var harness = CreateHarness();
            harness.SpawnWorld(211);
            SeedCompletedHistory(harness, 1407, 1069);
            var npcs = new NpcManager(harness, harness.Manager);
            Accept(harness, npcs, 211, missionId);
            Assert.AreEqual(MissionObjectiveState.Incomplete,
                harness.Client.Player.Missions[missionId].Objectives[objectiveId].State);
            Acquire(harness, logosId);
            Assert.AreEqual(MissionObjectiveState.Completed,
                harness.Client.Player.Missions[missionId].Objectives[objectiveId].State);
            var before = harness.Client.Player.Credits[CurencyType.Credits];
            var priorExperience = harness.Client.Player.Experience;
            TurnIn(harness, npcs, 211, missionId);
            ReplayTurnIn(harness, npcs, 211, missionId);
            Assert.AreEqual(before + credits, harness.Client.Player.Credits[CurencyType.Credits]);
            Assert.AreEqual(priorExperience + experience, harness.Client.Player.Experience);
            Acquire(harness, logosId);
            using var unit = harness.CreateChar();
            Assert.AreEqual(1, unit.CharacterLogoses.GetLogos(harness.Client.Player.Id).Count(id => id == logosId));
            Assert.AreEqual(1, harness.Client.Player.Logos.Count(id => id == logosId));
        }

        [TestMethod]
        [DataRow(1638U, 2U, 10U)]
        [DataRow(1640U, 4U, 1U)]
        [DataRow(921U, 5U, 24U)]
        [DataRow(907U, 1U, 6U)]
        [DataRow(909U, 1U, 28U)]
        [DataRow(1639U, 6U, 23U)]
        [DataRow(1633U, 3U, 2U)]
        [DataRow(911U, 3U, 56U)]
        public void ShrineKnowledgeAcquiredBeforeTheDispatchSatisfiesTheNativeObjective(
            uint missionId, uint objectiveId, uint logosId)
        {
            using var harness = CreateHarness();
            harness.SpawnWorld(211);
            SeedCompletedHistory(harness, 1407, 1069);
            Acquire(harness, logosId);
            var npcs = new NpcManager(harness, harness.Manager);
            Accept(harness, npcs, 211, missionId);
            Assert.AreEqual(MissionObjectiveState.Completed,
                harness.Client.Player.Missions[missionId].Objectives[objectiveId].State);
            TurnIn(harness, npcs, 211, missionId);
            using var unit = harness.CreateChar();
            Assert.AreEqual(1, unit.CharacterLogoses.GetLogos(harness.Client.Player.Id).Count(id => id == logosId));
        }

        [TestMethod]
        public void LangermanDoesNotBypassReceptionOrOfferRetiredMunsonMissions()
        {
            using var harness = CreateHarness();
            harness.SpawnWorld(211);
            var npc = harness.Npc(211);
            harness.MoveTo(npc.Position);
            var npcs = new NpcManager(harness, harness.Manager);
            var conversation = Open(harness, npcs, npc.EntityId);
            foreach (var missionId in new uint[] { 1640, 921, 907, 909, 1639, 751, 780, 767 })
                Assert.IsFalse(Offers(conversation, missionId), $"Unexpected native offer {missionId}.");
            foreach (var missionId in new uint[] { 1638, 1633, 911 })
                Assert.IsTrue(Offers(conversation, missionId));
        }

        private static void Acquire(WildernessRuntimeTestHarness harness, uint logosId)
        {
            var shrine = harness.Map.DynamicObjects.OfType<Logos>().Single(logos => logos.Id == logosId);
            harness.MoveTo(shrine.Position);
            Assert.IsTrue(EntityManager.Instance.TryGetObject(shrine.EntityId, out var visible) &&
                ReferenceEquals(shrine, visible), "The actual migrated shrine must be visible and registered.");
            harness.Objects.RequestUseObjectPacket(harness.Client, new RequestUseObjectPacket
            {
                EntityId = shrine.EntityId, ActionId = ActionId.UseObject,
                ActionArgId = DynamicObjectManager.LogosUseArgId
            });
            ActorActionManager.Instance.DoWork(harness.Map, 10001);
            Assert.IsTrue(harness.Client.Player.Logos.Contains(logosId));
        }
    }
}
