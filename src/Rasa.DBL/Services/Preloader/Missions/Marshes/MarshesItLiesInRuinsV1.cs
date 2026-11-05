using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Structures;
using Rasa.Services.Preloader.Missions.Plateau;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// It Lies in Ruins (native mission 1673), Valverde Marshes: Retread Vincent in Retread City knows nothing of the Eloh
    /// Amee Corman sent the player about, and sends them north to Lieutenant Morrison at the Falcon Hold Outpost.
    ///
    /// Native: texts 16101-16104 and 16105 (missionconversation): log "Report in to Lieutenant Morrison at the Falcon Hold
    /// Outpost to learn more about the situation in the Marshes.", offer "Corman sent you over here about an Eloh? ... one of
    /// the AFS types up in Falcon Hold knows a bit of top secret info", turn-in "Alright, soldier. It's good to have you around
    /// at a time like this"; objective 1 "Report in to Lieutenant Morrison in Falcon Hold" 16106/16107, no counter
    /// (missionobjective); objectiveconversation (1673,1) package 299 text 16108 "The Retreads sent you up here?
    /// Interesting.". Names 8552 Retread Vincent, 3113 Lieutenant Morrison.
    ///
    /// Sourced: TaRapedia It Lies in Ruins: giver Retread Vincent, receiver Lieutenant Morrison (Falcon Hold); requires
    /// Retreads to Retreads; no follow-up; level 35; 75,000 XP, 3,800 credits; one of Wellcare Hazmat Armor Vest 3 lvl 39,
    /// ChiTech Mech Armor Vest 4 lvl 35, Prodigy Motor Assist Armor Vest 6 lvl 38, Titan Reflective Armor Vest 3 lvl 39 (Ten
    /// Ton Hammer names the same four vests). TR MMO DB: level 33, 3,800 credits. World DB: Retread Vincent 510168 (package
    /// 951, Retread City), Lieutenant Morrison 510162 (package 299, pool at -142.7, 219.11, 618 by the Falcon Hold hospital,
    /// an upstream placeholder spot).
    ///
    /// Revision plateau_1_16_5 (PlateauMissionDataV1), as Retreads to Retreads (1751) before it: a required chain stays on
    /// one revision (MissionContentValidator, "cross-revision-reference"); its category is the Marshes' 10000051
    /// "Battlefield (Marshes)" (missioncategorylanguage), set after the row, and its marker names the Marshes map, as the
    /// Marshes rows (MarshesMissionDataV1.Indicator).
    ///
    /// Reconstructed (ESTIMATE): Morrison's talk completes 1, turn-in at Morrison. The items the lowest Uncommon templates of
    /// the bands that hold those levels (the manufacturer names and TaRapedia's numbers after "Vest" are not in the client
    /// tables), one to choose. A marker at Morrison.
    /// </summary>
    public static class MarshesItLiesInRuinsV1
    {
        public const uint MissionId = 1673;
        public const uint Level = 35;
        public const uint Vincent = PlateauRetreadsToRetreadsV1.Vincent;
        public const uint Morrison = 510162, MorrisonPackage = 299;
        public const uint Report = 1;

        /// <summary>Morrison's spawn pool 510162 (MissionNpcSpawnpoolPreloader), the marker.</summary>
        public static readonly (double X, double Y, double Z) MorrisonAt = (-142.7, 219.11, 618);

        public const uint Experience = 75000;
        public const uint Credits = 3800;
        public const uint HazmatVest = 20951;           // Armor_T2_Hazmat_V02_UNC_Vest_39_to_43
        public const uint MechVest = 33193;             // Armor_T3_Mech_V02_UNC_Vest_35_to_39
        public const uint MotorAssistVest = 29247;      // Armor_T1_MotorAssist_V04_UNC_Vest_38_to_42
        public const uint ReflectiveVest = 36188;       // Armor_T2_Reflective_V02_UNC_Vest_39_to_43

        public static void Up(MigrationBuilder migration)
        {
            // Plateau's revision (the chain it ends), the Marshes category: the Marshes rows in plain SQL (MarshesSql).
            var mission = new MarshesMissionDataV1(migration, MissionId, PlateauMissionDataV1.Revision, "It Lies in Ruins", 16101,
                Vincent, Morrison, Level);
            mission.Reward(Experience, Credits, new[] { HazmatVest, MechVest, MotorAssistVest, ReflectiveVest });
            mission.Prerequisite(PlateauRetreadsToRetreadsV1.MissionId);

            mission.Objective(Report, 16106, 16107, 0, MissionObjectiveState.Incomplete);
            mission.Transition(Report);
            mission.Conversation(Report, MorrisonPackage);
            mission.Action(Report, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Indicator(Report, 1, MorrisonAt.X, MorrisonAt.Y, MorrisonAt.Z);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1673",
                "Native1 'Report in to Lieutenant Morrison in Falcon Hold' 16106/16107, no counter; objectiveconversation 1673,1 " +
                "package299 text16108. Texts 16101-16105. Names 8552 Retread Vincent, 3113 Lieutenant Morrison.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/It_Lies_in_Ruins",
                "Giver Retread Vincent, receiver Lieutenant Morrison (Falcon Hold); requires Retreads to Retreads; level35; 75000XP, " +
                "3800 credits; one of Wellcare Hazmat Armor Vest 3 lvl39, ChiTech Mech Armor Vest 4 lvl35, Prodigy Motor Assist " +
                "Armor Vest 6 lvl38, Titan Reflective Armor Vest 3 lvl39. TR MMO DB level33.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:MarshesItLiesInRuinsV1",
                "Revision plateau_1_16_5 with 1751 (one chain, one revision), Marshes category. Morrison's talk (510162, package299) " +
                "completes 1; turn-in at Morrison. Items 20951, 33193, 29247, 36188 (lowest Uncommon of each kind and band), one " +
                "to choose. A marker at Morrison on the Marshes.", 0.3);

            // The marker is on the Marshes, not the Plateau of the mission's revision: a scene that holds only its map
            // (MissionSceneDefinition.IndicatorMaps, MissionMarkerProjection.ResolveMap).
            mission.Enable(new MissionSceneDefinition
            {
                IndicatorMaps = new Dictionary<uint, Dictionary<uint, uint>>
                {
                    [Report] = new() { [MissionIndicator.UnnamedFrom + 1] = MarshesMissionDataV1.MapContextId }
                }
            });
        }

        public static void Down(MigrationBuilder migration) => PlateauMissionDataV1.Remove(migration, MissionId);
    }
}
