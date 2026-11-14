using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// Meet Sergeant Drof (native mission 1532), Indra Caverns (squad instance 2055): after Digging for the Truth, Headquarters
    /// calls: Sergeant Drof and Scout Naimi were flown in by an AFS dropship and wait at the top of the ridge in the Bane mining area,
    /// by the entrance to Gotha Processing Plant.
    ///
    /// Native: texts 13691-13695 (missionconversation; 6 empty); objective (missionobjective) 1 "Proceed to Drof's New Location"
    /// 13701/13702 ("Head towards the entrance to the Bane Processing Plant, at the top of the ridge in the mining area"), no
    /// counter; objective conversation (1532, 1) package 1544 (Drof) 15175 ("The AFS was kind enough to give us a lift, so I
    /// brought some help."). Indicator name 222 "Sergeant Drof" (missionobjectiveindicatorlanguage). Name 9427 Drof.
    ///
    /// Sourced: TaRapedia Meet Sergeant Drof (0.5): Headquarters (Radio) gives, Drof rewards; follow-up Subversion; 60,000 XP, 6,750
    /// credits; one of Class IX Incendiary Grenade x3, Advanced Med Pack x2, EMP Grenade x3, Res Trauma Kit.
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3):
    ///  - Level 43; offered by radio when Digging for the Truth (1447) is turned in (MissionChannels' mission-rewarded source, as
    ///    the triage's M8), and again on arriving in Indra Caverns for one who let the call pass (as Marshes 1904's map arrival);
    ///    requires 1447 (TaRapedia names none; the story's order).
    ///  - 1 the talk at Drof's second row (AshenGothaPlantNpcsV1, package 1544: set 15's Drof in the facility has the same package
    ///    and so hears it too); the turn-in at the second row.
    ///  - The items: the Class IX (09_45) consumables of the four kinds (client entityclass names): Incendiary Grenade 111040 x3,
    ///    Advanced Med Pack 45068 (Medpack_09C; 09A is 1559's Basic) x2, EMP Grenade 111030 x3, Res Sickness Kit 47601 x1 (the stack
    ///    sizes TaRapedia's, the kit 1); one to choose.
    ///  - Marker: 222 on Drof (Indra Caverns, through the scene's IndicatorMaps).
    /// </summary>
    public static class AshenMeetSergeantDrofV1
    {
        public const uint MissionId = 1532;
        public const uint Level = 43;
        public const uint Drof = AshenGothaPlantNpcsV1.Drof, DrofPackage = AshenGothaPlantNpcsV1.DrofPackage;
        public const uint Caverns = AshenGothaPlantNpcsV1.Caverns;
        public const uint Proceed = 1;
        public const uint DrofIndicator = 222;
        public const double NpcMarkerRadius = 8;   // ESTIMATE

        public const uint Experience = 60000;
        public const uint Credits = 6750;
        public const uint IncendiaryGrenade = 111040;   // Consumable_Grenade_Fire_09_45
        public const uint AdvancedMedPack = 45068;      // Consumable_Medpack_09C_45
        public const uint EmpGrenade = 111030;          // Consumable_Grenade_EMP_09_45
        // TaRapedia's "Res Trauma Kit" has no client template of that name: mapped to 47601 "Res Sickness Kit" (ESTIMATE).
        public const uint ResKit = 47601;               // Consumable_Res_Sickness_Kit_09_45

        public static void Up(MigrationBuilder migration)
        {
            var mission = new AshenMissionDataV1(migration, MissionId, "Meet Sergeant Drof", 13691, null, Drof, Level);
            mission.Reward(Experience, Credits,
                new (uint, uint)[] { (IncendiaryGrenade, 3), (AdvancedMedPack, 2), (EmpGrenade, 3), (ResKit, 1) }, new (uint, uint)[0]);
            mission.Prerequisite(AshenDiggingForTheTruthV1.MissionId);
            mission.RadioOffer(MissionOfferSourceDefinition.MissionRewarded(AshenDiggingForTheTruthV1.MissionId),
                new MissionOfferSourceDefinition(MissionOfferSourceKind.ServerEvent, MissionOfferSourceDefinition.MapArrivalKey, Caverns));

            mission.Objective(Proceed, 13701, 13702, 0, MissionObjectiveState.Incomplete);
            mission.Transition(Proceed);
            mission.Conversation(Proceed, DrofPackage);
            mission.Action(Proceed, 1, MissionActionKind.GrantReward, reward: 1);
            var drof = AshenGothaPlantNpcsV1.DrofAt;
            mission.NamedIndicator(Proceed, DrofIndicator, drof.X, drof.Y, drof.Z, NpcMarkerRadius, Caverns);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1532",
                "Native1 'Proceed to Drof's New Location' 13701/13702, no counter; objectiveconversation 1532,1 pkg1544 15175. Texts " +
                "13691-13695 (the radio call: top of the ridge in the Bane mining area, near the entrance to Gotha). Indicator 222. Name 9427 Drof.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Meet_Sergeant_Drof",
                "Headquarters (Radio) gives, Sergeant Jonas Drof rewards; follow-up Subversion; 60000XP, 6750 credits; Class IX Incendiary " +
                "Grenade x3, Advanced Med Pack x2, EMP Grenade x3, Res Trauma Kit; meet me at the top of the ridge in the Bane mining area.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:AshenMeetSergeantDrofV1",
                "Level43; radio offer at 1447's reward and on arrival in Indra Caverns; requires 1447 (estimated). 1 the talk at Drof's " +
                "second row 790600 (pkg1544) by the Gotha plant's entrance (-309,-53); the turn-in there. Items 111040x3, 45068x2, 111030x3, " +
                "47601x1, one to choose. Marker 222 on Drof (IC).", 0.3);

            mission.Enable();
        }

        public static void Down(MigrationBuilder migration) => AshenMissionDataV1.Remove(migration, MissionId);
    }
}
