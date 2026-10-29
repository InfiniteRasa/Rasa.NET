using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// A Mystery Unearthed (native mission 2016): Archaeologist Wynne Topper at Twin Pillars (Wilderness) sends the
    /// player to General Thaddeus T. Bailey at Fort Defiance (Plateau) with news of the Eloh ruins by the new
    /// wormhole. It opens Symbolism (2017).
    ///
    /// Native: name/log/offer/turn-in/reminder texts 21909-21913 (missionconversation); objective 1 "Report to
    /// General Thadeus T. Bailey (Plateau - Fort Defiance)" 21914/21915, no counter (missionobjective); Bailey's
    /// line on package 636 (21947, objectiveconversation).
    ///
    /// Sourced: TaRapedia A Mystery Unearthed: giver Archeologist Wynne Topper, Twin Pillars Outpost; reward from
    /// General Thaddeus T. Bailey; requirement none; follow-up Symbolism; 7500 XP, no credits. RGTR patch notes
    /// (Deployment 14): "new level 50 mission offered by NPC Archaeologist Wynne Topper at Twin Pillars".
    ///
    /// Reconstructed: a marker on Bailey (PlateauBaileyNpcsV1.BaileyAt) on the Plateau. The mission starts on the
    /// Wilderness and a marker carries no map (MissionInfo), so it also shows at Bailey's coordinates there (fixed later).
    /// </summary>
    public static class PlateauMysteryUnearthedV1
    {
        public const uint MissionId = 2016;
        public const uint Level = 50;
        public const uint Report = 1;
        public const uint Topper = 510001, TopperPackage = 2595;
        public const uint Bailey = PlateauBaileyNpcsV1.Bailey;

        public const uint Experience = 7500;
        public const uint Credits = 0;

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PlateauMissionDataV1(migration, MissionId, "A Mystery Unearthed", 21909, Topper, Bailey, Level);
            mission.Reward(Experience, Credits, new uint[0]);

            mission.Objective(Report, 21914, 21915, 0, MissionObjectiveState.Incomplete);
            mission.Transition(Report);
            mission.Conversation(Report, PlateauBaileyNpcsV1.BaileyPackage);
            mission.Indicator(Report, 1, PlateauBaileyNpcsV1.BaileyAt.X, PlateauBaileyNpcsV1.BaileyAt.Y, PlateauBaileyNpcsV1.BaileyAt.Z);
            mission.Action(Report, 1, MissionActionKind.GrantReward, reward: 1);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/2016",
                "Native1 'Report to General Thadeus T. Bailey (Plateau - Fort Defiance)' 21914/21915, no counter; " +
                "objectiveconversation 2016,1,636 text21947. Texts 21909-21913. Bailey name6769, Topper package2595.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/A_Mystery_Unearthed",
                "Giver Archeologist Wynne Topper, Twin Pillars Outpost; reward from General Thaddeus T. Bailey; requirement " +
                "none; follow-up Symbolism; 7500XP, no credits. RGTR Deployment 14 patch notes: new level 50 mission.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PlateauMysteryUnearthedV1",
                "Marker on Bailey at Fort Defiance. Bailey new (PlateauBaileyNpcsV1).", 0.3);

            mission.Enable();
        }

        public static void Down(MigrationBuilder migration) => PlateauMissionDataV1.Remove(migration, MissionId);
    }
}
