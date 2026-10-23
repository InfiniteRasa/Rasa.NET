using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// New Orders From The General (native mission 1541): Colonel Bosley passes on General Bailey's orders: find Amee
    /// Corman, the Retreads' leader at the Retread Camp in Valverde Pools, and ask her about the missing Eloh.
    ///
    /// Native: texts 13931-13935 (missionconversation): offer "there's a group of Retreads in Valverde Pools that might
    /// know something. They've got a human leader, Amee Corman", turn-in "If I knew the location of any Eloh alive here
    /// on Foreas I certainly wouldn't just tell you"; objective 1 "Question Amee Corman." 13936/13937 (missionobjective),
    /// no counter; its talk (objectiveconversation) on package 329, text 13938 "Yes, what is it?".
    ///
    /// Sourced: TaRapedia New Orders From The General: giver Colonel Bosley, reward giver Amee Corman (Retread Camp),
    /// requires One Way Trip, follow-up Weapons Trafficking; 55,000 XP, 5,100 credits, one of Dynamo Hazmat Armor
    /// Gloves lvl 34, Teleract Motor Assist Armor Gloves lvl 33, Astra Reflective Armor Gloves lvl 34, Titan Stealth Armor
    /// Legs lvl 35. World DB: Amee Corman 510195 (package 329) at the Retread Camp, Valverde Pools (-144.7, 861.59, 520.5).
    ///
    /// Reconstructed (ESTIMATE, the user's choices of 2026-10-23): level 33 (no source; as One Way Trip before it); the
    /// four items as the nearest Uncommon templates of their kind (the manufacturer names are not in the client tables):
    /// Bio V04 30-34 gloves 120354, Mech V04 30-34 gloves 120356, Reflective V02 29-33 gloves 35700, Stealth V04 35-39 legs
    /// 120421. Amee's talk completes 1; turn-in at Amee. No indicator: the goal is on another map (as 2016 / 2017).
    /// </summary>
    public static class PlateauNewOrdersFromTheGeneralV1
    {
        public const uint MissionId = 1541;
        public const uint Level = 33;
        public const uint Bosley = PlateauGiantKillerV1.Bosley;
        public const uint Amee = 510195, AmeePackage = 329;   // creature and pool, "Amee Corman", Valverde Pools
        public const uint Question = 1;

        public const uint Experience = 55000;
        public const uint Credits = 5100;
        public const uint BioGloves = 120354;           // Armor_T3_Bio_V04_UNC_Gloves_30_to_34
        public const uint MechGloves = 120356;          // Armor_T3_Mech_V04_UNC_Gloves_30_to_34
        public const uint ReflectiveGloves = 35700;     // Armor_T2_Reflective_V02_UNC_Gloves_29_to_33
        public const uint StealthLegs = 120421;         // Armor_T3_Stealth_V04_UNC_Legs_35_to_39

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PlateauMissionDataV1(migration, MissionId, "New Orders From The General", 13931, Bosley, Amee, Level);
            mission.Reward(Experience, Credits, new[] { BioGloves, MechGloves, ReflectiveGloves, StealthLegs });
            mission.Prerequisite(PlateauOneWayTripV1.MissionId);

            mission.Objective(Question, 13936, 13937, 0, MissionObjectiveState.Incomplete);
            mission.Transition(Question);
            mission.Conversation(Question, AmeePackage);
            mission.Action(Question, 1, MissionActionKind.GrantReward, reward: 1);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1541",
                "Native1 'Question Amee Corman.' 13936/13937, no counter; objectiveconversation 1541,1 on package 329 text13938. " +
                "Texts 13931-13935.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/New_Orders_From_The_General",
                "Giver Colonel Bosley, reward giver Amee Corman (Retread Camp); requires One Way Trip; follow-up Weapons Trafficking; " +
                "55000XP, 5100 credits; Dynamo Hazmat gloves 34, Teleract Motor Assist gloves 33, Astra Reflective gloves 34, Titan " +
                "Stealth legs 35. No level.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PlateauNewOrdersFromTheGeneralV1",
                "Level33. Amee Corman's talk (510195, Valverde Pools) completes 1; turn-in at Amee. Items 120354, 120356, 35700, " +
                "120421 (nearest Uncommon of each kind). No indicator (another map).", 0.3);

            mission.Enable();
        }

        public static void Down(MigrationBuilder migration) => PlateauMissionDataV1.Remove(migration, MissionId);
    }
}
