using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Missions.Definitions;
using Rasa.Structures;

namespace Rasa.Test.Missions
{
    // The log holds thirty missions (gameconstants MAX_MISSION_COUNT), and the client counts
    // the ones it lists: active, and successful with the reward still to collect
    // (missionlog.py GetCurrentMissionCount). A failed mission is not among them - the client
    // took it out of its log when it failed - and neither is a rewarded one. The server counts
    // the same, so that "Mission log is full." is said when the log the player sees is full.
    [TestClass]
    [DoNotParallelize]
    public class MissionJournalCapacityTests
    {
        private const uint First = 400, Other = 429, Taken = 430;

        [TestMethod]
        [DataRow(MissionState.Failed, true, DisplayName = "29 active and a failed one: the next is taken")]
        [DataRow(MissionState.Completed, true, DisplayName = "29 active and a rewarded one: the next is taken")]
        [DataRow(MissionState.Success, false, DisplayName = "29 active and one with its reward to collect: full")]
        [DataRow(MissionState.Active, false, DisplayName = "30 active: full")]
        public void AMissionTheClientDoesNotListTakesNoPlaceInTheLog(MissionState other, bool taken)
        {
            using var context = Context(active: 29);
            context.SeedMission(1, Other, (uint)other, false);
            context.ReloadPlayerMissions();
            var giver = context.AddNpc(77);

            Assert.AreEqual(taken, context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, Taken));

            Assert.AreEqual(taken, context.Client.Player.Missions.ContainsKey(Taken));
        }

        [TestMethod]
        [DataRow(29, true, DisplayName = "29 active: the failed mission is taken again")]
        [DataRow(30, false, DisplayName = "30 active: it is not, the log is full")]
        public void TakingAFailedMissionAgainNeedsAPlaceLikeAnyOther(int active, bool taken)
        {
            using var context = Context(active);
            context.SeedMission(1, Taken, (uint)MissionState.Failed, false);
            context.ReloadPlayerMissions();
            var giver = context.AddNpc(77);

            Assert.AreEqual(taken, context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, Taken));

            Assert.AreEqual(taken ? MissionState.Active : MissionState.Failed, context.Client.Player.Missions[Taken].State);
        }

        /// <summary>Thirty-two missions of one giver, the first <paramref name="active"/> of them in the character's log.</summary>
        private static MissionTestContext Context(int active)
        {
            var context = MissionTestContext.WithCustomDefinitions(Enumerable.Range(0, 32)
                .Select(index => new Mission(First + (uint)index, "Capacity fixture", First + (uint)index, 77, 88, 1, 1, 2, false, false,
                    Array.Empty<MissionObjectiveDefinition>(), true))
                .ToDictionary(mission => mission.MissionId));

            // The one in another state and the one taken stay out of the active ones.
            foreach (var id in Enumerable.Range(0, 32).Select(index => First + (uint)index)
                .Where(id => id != Other && id != Taken).Take(active))
                context.SeedMission(1, id, (uint)MissionState.Active, false);

            return context;
        }
    }
}
