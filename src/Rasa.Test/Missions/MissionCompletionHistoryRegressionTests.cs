using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions
{
    using Rasa.Data;
    using Rasa.Packets.Mission.Server;
    using Rasa.Structures;

    [TestClass]
    [DoNotParallelize]
    public class MissionCompletionHistoryRegressionTests
    {
        [TestMethod]
        public void DismissingFailureRetainsAnOutcomeWithoutClaimingTheReward()
        {
            using var context = MissionTestContext.WithDefinitions(321);
            context.SeedMission(1, 321, (uint)MissionState.Failed, false);
            context.ReloadPlayerMissions();
            Assert.IsTrue(context.Manager.TryClear(context.Client, 321));
            using (var unit = context.CreateChar())
            {
                var outcomes = unit.CharacterMissions.Runtime.History(1);
                Assert.AreEqual(1, outcomes.Count, "Failure facts can be prerequisites for authored retry missions.");
                Assert.IsFalse(outcomes[0].Rewarded);
            }
            var giver = context.AddNpc(77);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 321),
                "A failed outcome is not a nonrepeatable reward claim.");
        }

        [TestMethod]
        public void ClearingRewardedMissionCannotReacceptOrRegrantIt()
        {
            using var context = MissionTestContext.WithCompletableMission(321);
            Assert.IsTrue(context.Manager.CompleteOfferedMission(
                context.Client, context.Receiver.EntityId, 321, 0, null));
            var paid = context.ReadRewardTotals();
            Assert.IsTrue(context.Manager.TryClear(context.Client, 321));
            var giver = context.AddNpc(77);
            context.Drain();

            Assert.IsFalse(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 321),
                "Removing a journal projection must not remove a nonrepeatable reward claim.");
            Assert.AreEqual(paid, context.ReadRewardTotals());
            Assert.AreEqual(0, context.Drain().OfType<MissionGainedPacket>().Count());
        }

        [TestMethod]
        public void RewardedMissionHistoryDoesNotConsumeActiveMissionSlots()
        {
            using var context = MissionTestContext.WithDefinitions(321, 429);
            var giver = context.AddNpc(77);

            for (uint id = 1000; id < 1200; id++)
                context.SeedMission(context.Client.Player.Id, id, (uint)MissionState.Completed, false);

            for (uint id = 1; id <= 29; id++)
            {
                context.SeedMission(context.Client.Player.Id, id, (uint)MissionState.Active, false);
                context.Client.Player.Missions.Add(id, new MissionLog(id, MissionState.Active, false));
            }
            context.Drain();

            Assert.IsTrue(
                context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 321),
                "Two hundred rewarded missions must not consume the remaining active journal slot.");
            Assert.AreEqual(
                30,
                context.Client.Player.Missions.Values.Count(mission => mission.State == MissionState.Active));
            Assert.AreEqual(1, context.Drain().OfType<MissionGainedPacket>().Count());

            Assert.IsFalse(
                context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 429),
                "Separating completion history must still enforce the thirty active mission limit.");
            Assert.IsFalse(context.Client.Player.Missions.ContainsKey(429));
            Assert.AreEqual(0, context.Drain().OfType<MissionGainedPacket>().Count());
        }
    }
}
