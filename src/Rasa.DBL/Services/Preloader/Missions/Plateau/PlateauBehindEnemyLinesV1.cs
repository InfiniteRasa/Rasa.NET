using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// Behind Enemy Lines (native mission 555), Plateau: Headquarters calls by radio when the player has
    /// turned in The Stainless Steel Bloodhound (867) and sends them through the secret tunnel to Colonel
    /// Bosley at Camp Resistance.
    ///
    /// Native: name/log/offer/turn-in/reminder texts 1548-1551 and 6302 (missionconversation); objective 1
    /// "Report to Col. Bosley." 3388/6303, no counter (missionobjective); Bosley's line 4965 on package
    /// 1001, flag 1 (objectiveconversation).
    ///
    /// Sourced (TaRapedia Behind Enemy Lines, a stub): giver Headquarters, at Radio; requires The Stainless
    /// Steel Bloodhound; reward from Colonel Bosley; 40,000 XP, 4,850 credits, items "lvl 30; lvl 29; lvl 28;
    /// lvl 29". TaRapedia Fort Defiance mission list: given by Headquarters.
    ///
    /// Reconstructed (ESTIMATE): level 30 (the item levels; no source gives the mission's); offered by
    /// radio at 867's turn-in (MissionOfferSourceDefinition.MissionRewarded), and again on arrival on the
    /// Plateau if the player turned it down; the four items read as the Uncommon V04 vests of the 30-34
    /// band, one of each armor type (as Bomb Voyage's); the marker on Bosley, as Giant Killer's.
    /// </summary>
    public static class PlateauBehindEnemyLinesV1
    {
        public const uint MissionId = 555;
        public const uint Level = 30;
        public const uint Report = 1;
        public const uint Bosley = PlateauGiantKillerV1.Bosley;
        public const uint Bloodhound = PlateauBloodhoundV1.MissionId;

        public const uint Experience = 40000;
        public const uint Credits = 4850;
        public const uint BioVest = PlateauBombVoyageV1.BioVest, GravitonVest = PlateauBombVoyageV1.GravitonVest,
            MechVest = PlateauBombVoyageV1.MechVest, StealthVest = PlateauBombVoyageV1.StealthVest;

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PlateauMissionDataV1(migration, MissionId, "Behind Enemy Lines", 1548, null, Bosley, Level);
            mission.RadioOffer(MissionOfferSourceDefinition.MissionRewarded(Bloodhound),
                new MissionOfferSourceDefinition(MissionOfferSourceKind.ServerEvent, MissionOfferSourceDefinition.MapArrivalKey,
                    PlateauMissionDataV1.MapContextId));
            mission.Reward(Experience, Credits, new[] { BioVest, GravitonVest, MechVest, StealthVest });
            mission.Prerequisite(Bloodhound);

            mission.Objective(Report, 3388, 6303, 0, MissionObjectiveState.Incomplete);
            mission.Transition(Report);
            mission.Conversation(Report, PlateauGiantKillerV1.BosleyPackage);
            mission.Action(Report, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Indicator(Report, 1, -632.3, 447.95, -232.5);       // Bosley

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/555",
                "Native1 'Report to Col. Bosley.' 3388/6303, no counter; objectiveconversation 555,1,1001 text4965. Texts " +
                "1548-1551, 6302. Bosley 510176 package1001.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Behind_Enemy_Lines",
                "Stub. Giver Headquarters, at Radio; requires The Stainless Steel Bloodhound; reward from Colonel Bosley; " +
                "40000XP, 4850 credits, items lvl 30/29/28/29. Fort Defiance mission list: given by Headquarters.", 0.4);
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PlateauBehindEnemyLinesV1",
                "Level30 from the item levels; radio offer at 867's turn-in, again on arrival on the Plateau; items read as " +
                "Uncommon V04 vests 30-34: 46035, 25337, 34463, 40591; marker on Bosley.", 0.3);

            mission.Enable();
        }

        public static void Down(MigrationBuilder migration) => PlateauMissionDataV1.Remove(migration, MissionId);
    }
}
