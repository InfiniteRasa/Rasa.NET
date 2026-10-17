using System.Linq;

namespace Rasa.Test.Missions
{
    using Rasa.Managers;
    using Rasa.Services.Preloader;
    using Rasa.Structures;

    /// <summary>
    /// A battlefield's Targets of Opportunity from the officer who gives it
    /// (TargetsOfOpportunityGivers), stood beside the player on the map they are on: the
    /// harness has the battlefields as empty maps, without their pools.
    /// </summary>
    internal static class TargetsOfOpportunityGiverDriver
    {
        internal static TargetsOfOpportunityGivers.Giver GiverOf(uint missionId) =>
            TargetsOfOpportunityGivers.Givers.Single(giver => giver.MissionId == missionId);

        internal static Creature StandGiver(this BootcampRuntimeTestHarness.Harness harness, uint missionId)
        {
            var player = harness.Client.Player;
            var npc = harness.Context.AddNpc(GiverOf(missionId).Id, player.MapChannel, position: player.Position);
            npc.Npc.NpcMissionIds = harness.Manager.LoadedMissions.Values
                .Where(mission => mission.MissionGiver == npc.DbId || mission.MissionReciver == npc.DbId)
                .Select(mission => mission.MissionId).ToList();
            return npc;
        }

        /// <summary>The mission taken from its officer.</summary>
        internal static bool TakeFromGiver(this BootcampRuntimeTestHarness.Harness harness, uint missionId) =>
            harness.Manager.AcceptOfferedMission(harness.Client, harness.StandGiver(missionId).EntityId, missionId);
    }
}
