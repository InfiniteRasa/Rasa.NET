using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Pools
{
    /// <summary>
    /// Turpis Transport (native mission 1305), Valverde Pools: Corporal Hash by the Turpis Refinery door can call a
    /// transport to take the player to Turpis Refinery.
    ///
    /// Native: texts 10581-10585 (missionconversation): log "Talk to Corporal hash when you are ready to be transported to
    /// Turpis Refinery.", offer "If you're headed to Turpis Refinery, I can call a transport to do a quick touchdown to
    /// pick you up.", turn-in "Keep that head down! You never know when some hotshot pilot will come in too hot! Go go
    /// go!"; objective 1 "Complete" 10586/10587 (missionobjective); no objective conversation.
    ///
    /// Sourced: TaRapedia Turpis Transport: giver and receiver Corporal Hash (Turpis Refinery Dropship Pad); no
    /// requirement, no follow-up, no reward. Ten Ton Hammer Turpis Refinery guide: "Head up a short ramp and talk to
    /// Corporal Hash. After a ride on the Turpis Transport, youll see four mission icons". IGN TR Vault: Corporal Hash
    /// (159.8, 671.7, -603.1), "Turpis Transport". Not in the TR MMO DB lists.
    ///
    /// Reconstructed (ESTIMATE; the user's choice of 2026-10-25: it completes when the player enters Turpis Refinery, no
    /// transport mechanic): Hash (PoolsTurpisRefineryNpcsV1) offers it on the Pools 12.6 m from the Turpis door; 1
    /// completes when the player arrives in Turpis Refinery (its whole map, the squad's copy too), through the door. The
    /// mission is then completed from the mission log (radio), as no one there takes it back. An empty reward (no
    /// experience, credits or items: a turn-in needs a reward row). Level 33, as the refinery's missions.
    /// </summary>
    public static class PoolsTurpisTransportV1
    {
        public const uint MissionId = 1305;
        public const uint Level = 33;
        public const uint Hash = PoolsTurpisRefineryNpcsV1.Hash;
        public const uint Complete = 1, Refinery = 1;
        /// <summary>The Turpis door on the Pools (map_link 26, MapLinkPreloader) and its radius, the marker.</summary>
        public static readonly (double X, double Y, double Z) DoorAt = (147.8214, 672.8627, -607.3924);
        public const double DoorRadius = 6;

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PoolsMissionDataV1(migration, MissionId, "Turpis Transport", 10581, Hash, null, Level);
            mission.Reward(0, 0, new uint[0]);
            mission.RadioCompletion();

            mission.Objective(Complete, 10586, 10587, 0, MissionObjectiveState.Incomplete);
            mission.MapArea(Refinery, PoolsTurpisRefineryNpcsV1.MapContextId);
            mission.Transition(Complete);
            mission.EnterArea(Complete, Refinery);
            mission.Action(Complete, 1, MissionActionKind.GrantReward, reward: 1);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1305",
                "Native1 'Complete' 10586/10587; no objectiveconversation. Texts 10581-10585 (call a transport).");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Turpis_Transport",
                "Giver/receiver Corporal Hash (Turpis Refinery Dropship Pad); no requirement, no reward. Ten Ton Hammer: Hash up a " +
                "short ramp from Suresh, then the ride. IGN TR Vault: Hash 159.8, 671.7, -603.1.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PoolsTurpisTransportV1",
                "No transport mechanic (user): 1 completes on arrival in Turpis Refinery (map 1429) through the door; completed " +
                "by radio. An empty reward. Level33 an estimate.", 0.3);

            mission.Indicator(Complete, 1, DoorAt.X, DoorAt.Y, DoorAt.Z, DoorRadius);

            mission.Enable();
        }

        public static void Down(MigrationBuilder migration) => PoolsMissionDataV1.Remove(migration, MissionId);
    }
}
