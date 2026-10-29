using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// Blueprint For Victory (native mission 1046), Plateau: Beta Squad Commander Petrie in
    /// Martyr's Canyon sends the player after the Maligo Elite Guardmasters (PlateauMaligoGuardsV1),
    /// one of whom carries the blueprints of Maligo Base, and on to Colonel Bosley with them.
    ///
    /// Native: name/log/offer/turn-in/reminder texts 5881-5884 and 6310 (missionconversation);
    /// objective 1 "Acquire the Maligo Blueprints." 5922/6311 and objective 3 "Bring the
    /// blueprints to Col. Bosley." 6024/6312 (missionobjective), no objective 2 and no counter;
    /// Bosley's hand-in line 6025 on package 1001, flag 1 (objectiveconversation 1046,3,1001,1,1).
    /// The blueprints are item class 22113, template 45021.
    ///
    /// Sourced (TaRapedia Blueprint For Victory): giver Petrie, 55,000 XP, 5,100 credits, one of
    /// four green level-35 armor pieces - Teleract Bio Legs, Wellcare Graviton Legs, Prodigy Mech
    /// Helmet, Prodigy Stealth Helmet; the blueprints "drop randomly"; requirement "Incommunicado
    /// (one objective)". Petrie: "We've already taken down a few, but so far, no luck" (5883).
    ///
    /// Reconstructed: level 30, as Incommunicado; offered once the player delivered Petrie's
    /// communicator (flag <see cref="DeliveredFlag"/>, ESTIMATE); receiver Bosley, whose line is
    /// the hand-in; the blueprints a personal 25% drop from a guardmaster (no source gives the
    /// rate), picked up from the corpse, one taken at turn-in; the armor pieces are the Uncommon
    /// V04 base templates of the 35-39 band (V04 as the Wilderness Motor Assist rewards), no
    /// module; objective 1's indicator over the canyon floor, objective 3's on Bosley.
    /// </summary>
    public static class PlateauBlueprintForVictoryV1
    {
        public const uint MissionId = 1046;
        public const uint Level = 30;
        public const uint Petrie = PlateauReconSquadsV1.Petrie;
        public const uint Bosley = PlateauGiantKillerV1.Bosley;
        public const uint BosleyPackage = PlateauGiantKillerV1.BosleyPackage;
        public const uint DeliveredFlag = PlateauIncommunicadoFlagsV1.PetrieFlag;

        public const uint BlueprintsTemplate = 45021;
        public const uint BlueprintsClass = 22113;
        public const string BlueprintsKey = "maligo-blueprints";
        public const int DropChance = 25;

        public const uint Experience = 55000;
        public const uint Credits = 5100;
        public const uint BioLegs = 46028;              // Armor_T3_Bio_V04_UNC_Legs_35_to_39
        public const uint GravitonLegs = 25229;         // Armor_T3_Graviton_V04_UNC_Legs_35_to_39
        public const uint MechHelmet = 34225;           // Armor_T3_Mech_V04_UNC_Helmet_35_to_39
        public const uint StealthHelmet = 40353;        // Armor_T3_Stealth_V04_UNC_Helmet_35_to_39

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PlateauMissionDataV1(migration, MissionId, "Blueprint For Victory", 5881, Petrie, Bosley, Level);
            mission.Reward(Experience, Credits, new[] { BioLegs, GravitonLegs, MechHelmet, StealthHelmet });
            mission.FlagPrerequisite(DeliveredFlag, 1, "Petrie's communicator delivered (Incommunicado objective 2)");

            mission.Objective(1, 5922, 6311, 0, MissionObjectiveState.Incomplete);
            mission.Objective(3, 6024, 6312, 1, MissionObjectiveState.Inactive);

            // 1: picking up the blueprints (item class) completes it and opens 3.
            mission.Transition(1);
            mission.Progress(1, MissionProgressEventKind.ItemAcquired, BlueprintsClass, 1);
            mission.Action(1, 1, MissionActionKind.RevealObjective, targetObjective: 3);
            mission.Action(1, 2, MissionActionKind.ActivateObjective, targetObjective: 3,
                state: MissionObjectiveState.Incomplete);

            // 3: Bosley's hand-in topic (package 1001, flag 1); the reward is paid at turn-in.
            mission.Transition(3);
            mission.Conversation(3, BosleyPackage);
            mission.Action(3, 1, MissionActionKind.GrantReward, reward: 1);

            mission.Indicator(1, 1, -125, 381, -830, 230);     // the guardmasters on the canyon floor
            mission.Indicator(3, 2, -632.3, 447.95, -232.5);   // Colonel Bosley (pool 510176)

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1046",
                "Native1 'Acquire the Maligo Blueprints' (5922/6311), class22113/template45021; 3 'Bring the blueprints to " +
                "Col. Bosley' (6024/6312), objectiveconversation 1046,3,1001,1,1 text6025; no objective2, no counter. " +
                "Texts 5881-5884, 6310.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Blueprint_For_Victory",
                "Giver Beta Squad Commander Petrie; requirement 'Incommunicado (one objective)'; 55000XP, 5100 credits; one " +
                "of green level35 Teleract Bio Legs/Wellcare Graviton Legs/Prodigy Mech Helmet/Prodigy Stealth Helmet; " +
                "the blueprints drop randomly (no rate).", 0.4);
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PlateauBlueprintForVictoryV1",
                "Level30. Offered after Petrie's delivery (flag1040002). Receiver Bosley. Personal 25% blueprints drop from " +
                "guardmaster650013 on map1497, actual pickup, one turn-in cost; rate and the guardmasters themselves are " +
                "estimates (PlateauMaligoGuardsV1). Uncommon V04 base armor of 35-39, no module: 46028, 25229, 34225, 40353.", 0.3);

            mission.Enable(new MissionSceneDefinition
            {
                ExistingFactObjectiveIds = new() { 1 },
                Items = new()
                {
                    new MissionItemBinding(BlueprintsKey, BlueprintsTemplate, MissionItemScope.CharacterOwned, 1,
                        MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                        MissionItemCleanupDisposition.Retain, TurnInQuantity: 1,
                        Drop: new MissionItemDropDefinition(new[] { PlateauMaligoGuardsV1.Guardmaster }, 1,
                            PlateauMissionDataV1.MapContextId, DropChance, 1))
                }
            });
        }

        public static void Down(MigrationBuilder migration) => PlateauMissionDataV1.Remove(migration, MissionId);
    }
}
