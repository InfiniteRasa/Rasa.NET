using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Crucible
{
    /// <summary>
    /// Get to Chaukas (native mission 1577), Ligo Crucible and the Chaukas Robotics Facility (squad instance 2146): Information
    /// Analyst Ruben at Outpost Intrepid sends the player into Chaukas to find Cipher Specialist Brian.
    ///
    /// Native: texts 14453-14457 (missionconversation; 14457 "none"); objective 1 "Get to Chaukas" 14458/14459, no counter
    /// (missionobjective); objective conversation (1577,1) package 1800 (Brian) text 14808 (its type-2 text 14807 empty).
    ///
    /// Sourced: TaRapedia Get to Chaukas: giver Information Analyst Ruben (Outpost Intrepid), reward from Cipher Specialist
    /// Brian (Chaukas); no requirement; follow-up Control the Brain; 90,000 XP, 8,200 credits; Olympia Mech Armor Vest 3,
    /// Dynamo Graviton Armor Vest 1, Hellstrom Hazmat Armor Vest 5, Audiodyne Motor Assist Armor Helmet 11 (cache). TR MMO DB:
    /// level 40 (its "The Last Man Standing" row holds 1577's content, NOTES-crucible-research-group2.md; triage decision 3).
    ///
    /// Reconstructed (ESTIMATE):
    ///  - The talk with Brian completes it; the turn-in at Brian in the squad's Chaukas.
    ///  - The items: the lowest Uncommon template of each kind and icon number in the band of level 40, the nearest icon
    ///    number where the band has none (Mech V03 Vest 33858, Graviton V02 Vest 24086, Hazmat V03 Vest 21699 (39-43),
    ///    MotorAssist V07 Helmet 31241 (38-42; the client has no V08-V11)), one to choose.
    ///  - Markers: on the floor before the Chaukas door on the Crucible and on Brian in Chaukas. The scene's map the
    ///    Crucible's (no actor), as Sadistic Sabotage.
    /// </summary>
    public static class CrucibleGetToChaukasV1
    {
        public const uint MissionId = 1577;
        public const uint Level = 40;
        public const uint Ruben = CrucibleChaukasNpcsV1.Ruben;
        public const uint Brian = CrucibleChaukasNpcsV1.Brian, BrianPackage = CrucibleChaukasNpcsV1.BrianPackage;
        public const uint MapContextId = CrucibleChaukasNpcsV1.Chaukas;
        public const uint GetThere = 1;
        public const double BrianRadius = 8, DoorMarkerRadius = 6;

        public const uint Experience = 90000;
        public const uint Credits = 8200;
        public const uint MechVest = 33858;             // Armor_T3_Mech_V03_UNC_Vest_40_to_44
        public const uint GravitonVest = 24086;         // Armor_T3_Graviton_V02_UNC_Vest_40_to_44
        public const uint HazmatVest = 21699;           // Armor_T2_Hazmat_V03_UNC_Vest_39_to_43
        public const uint MotorAssistHelmet = 31241;    // Armor_T1_MotorAssist_V07_UNC_Helmet_38_to_42

        public static void Up(MigrationBuilder migration)
        {
            var mission = new CrucibleMissionDataV1(migration, MissionId, "Get to Chaukas", 14453, Ruben, Brian, Level);
            mission.Reward(Experience, Credits, new[] { MechVest, GravitonVest, HazmatVest, MotorAssistHelmet });

            mission.Objective(GetThere, 14458, 14459, 0, MissionObjectiveState.Incomplete);
            mission.Transition(GetThere);
            mission.Conversation(GetThere, BrianPackage);
            mission.Action(GetThere, 1, MissionActionKind.GrantReward, reward: 1);

            var door = CrucibleChaukasNpcsV1.DoorWayAt;
            var brian = CrucibleChaukasNpcsV1.BrianAt;
            mission.Indicator(GetThere, 1, door.X, door.Y, door.Z, DoorMarkerRadius, CrucibleMissionDataV1.MapContextId);
            mission.Indicator(GetThere, 2, brian.X, brian.Y, brian.Z, BrianRadius, MapContextId);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1577",
                "Native1 'Get to Chaukas' 14458/14459, no counter; objectiveconversation 1577,1 package1800 text14808. Texts " +
                "14453-14457. Names 9724 Information Analyst Ruben, 9725 Cipher Specialist Brian.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Get_to_Chaukas",
                "Giver Information Analyst Ruben (Outpost Intrepid), reward from Cipher Specialist Brian (Chaukas); no requirement; " +
                "follow-up Control the Brain; 90000XP, 8200 credits; Olympia Mech Armor Vest 3, Dynamo Graviton Armor Vest 1, " +
                "Hellstrom Hazmat Armor Vest 5, Audiodyne Motor Assist Armor Helmet 11. TR MMO DB level40.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:CrucibleGetToChaukasV1",
                "Brian's talk (package1800) in the squad's Chaukas 2146 completes 1. Ruben 770058 and Brian 770059 new rows. " +
                "Items 33858, 24086, 21699, 31241, one to choose. Markers: before the Chaukas door (Crucible), Brian.", 0.3);

            // The scene's map is the Crucible's (the giver's), so the door marker is sent; Brian's goes to Chaukas through the
            // scene's IndicatorMaps (CrucibleMissionDataV1.Enable). As Sadistic Sabotage 1445.
            var scene = new MissionSceneDefinition { Script = "data.sequence", MapContextId = CrucibleMissionDataV1.MapContextId };
            scene.Sequences[0] = new SceneSequenceDefinition();
            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => CrucibleMissionDataV1.Remove(migration, MissionId);
    }
}
