using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// Robotic Salvage Recovery (native mission 1264), Ligo Ashen Desert: Scavenger Vakiel Latan in Ashoka Settlement, on her
    /// side after Formal Complaints, pays for intact salvage off the Bane robots of the Boneyard: 15 off Nitroglazers and 15
    /// off Hominis Machina.
    ///
    /// Native: texts 10143-10146, 10147 (missionconversation): log "Find Nitroglazers and Hominis Machina in the Ashen Desert and
    /// recover Intact Salvage for the Labbna. When you have collected 15 Intact Machina Salvage and 15 Intact Nitroglazer Salvage,
    /// return them to Vakiel Latan in Ashoka Settlement.", offer "I appreciate your help, even if things turned bad. The Labbna
    /// requires salvage ... you will have to salvage from the robots directly.", turn-in "Very good, very good. These parts are
    /// good ... Here is your fee. You are interested in more work, yes?", reminder "... We have collected much of the robotic
    /// salvage from Boneyard, so you must kill them for parts."; objective 1 "Recover 15 Nitroglazer Salvage" 10173/10174, 2
    /// "Recover 15 Machina Salvage" 10175/10176, no counters (missionobjective); no objective conversation. Items class 25444
    /// Intact Nitroglazer Salvage (template 97465), 25445 Intact Machina Salvage (97466). Names 9277 Latan, 434 Nitroglazer,
    /// 7716 Hominis Machina Mk XI.
    ///
    /// Sourced: TaRapedia Robotic Salvage Recovery: giver and reward giver Latan; requirement "??"; follow-up Weapon Salvage
    /// Recovery; 95,000 XP, 6,300 credits; one of Titan Graviton Armor Gloves (icon 2), Dynamo Motor Assist Armor Legs (2),
    /// Wellcare Hazmat Armor Legs (2), Wellcare Reflective Armor Legs (2); the robots "spawn throughout The Boneyard". TaRapedia
    /// 1580: Scavenger (Latan's missions) or Logistician (Ito's), only one, by the side taken in Formal Complaints.
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3):
    ///  - Offered after Formal Complaints is done with flag 250 (its choice 1) at 1 (triage D1): both, as The Human Perspective
    ///    needs 1254 and 251. A flag alone is not enough: the flags stay at an abandon of 1254, so a player who chose 1 and
    ///    abandoned would get 1264 and could then finish 1254 by choice 2 and have both sides.
    ///  - Level 40 (D5). A salvage a personal 75% drop (no source gives a rate; the Crucible's 50-75%) off the set's Nitroglazer
    ///    790240 or Hominis Machina 790241 on Ashen Desert while its objective is open; 15 of each at most; Latan takes them at
    ///    the turn-in; an abandon keeps them (as Harmful Mutations' samples). A marker (r20) on each of the set's four pools.
    ///  - The items: the lowest Uncommon template of TaRapedia's kind and icon 2 in the band of level 40: Graviton V02 Gloves
    ///    23698, Hazmat V02 Legs 20802, Reflective V02 Legs 36039; Motor Assist Legs have no Uncommon below V04: V04 29093 (as
    ///    Dearly Departed); one to choose.
    /// </summary>
    public static class AshenRoboticSalvageV1
    {
        public const uint MissionId = 1264;
        public const uint Level = 40;
        public const uint Latan = AshenFormalComplaintsNpcsV1.Latan;
        public const uint NitroSalvage = 1, MachinaSalvage = 2;
        public const uint Count = 15;
        public const int Chance = 75;
        public const uint NitroTemplate = 97465, NitroClass = 25444;
        public const uint MachinaTemplate = 97466, MachinaClass = 25445;
        public const string NitroKey = "intact-nitroglazer-salvage", MachinaKey = "intact-machina-salvage";
        public const double MarkerRadius = 20;

        public const uint Experience = 95000;
        public const uint Credits = 6300;
        public const uint GravitonGloves = 23698;       // Armor_T3_Graviton_V02_UNC_Gloves_40_to_44
        public const uint MotorAssistLegs = 29093;      // Armor_T1_MotorAssist_V04_UNC_Legs_38_to_42
        public const uint HazmatLegs = 20802;           // Armor_T2_Hazmat_V02_UNC_Legs_39_to_43
        public const uint ReflectiveLegs = 36039;       // Armor_T2_Reflective_V02_UNC_Legs_39_to_43

        public static void Up(MigrationBuilder migration)
        {
            var mission = new AshenMissionDataV1(migration, MissionId, "Robotic Salvage Recovery", 10143, Latan, Latan, Level);
            mission.Reward(Experience, Credits, new[] { GravitonGloves, MotorAssistLegs, HazmatLegs, ReflectiveLegs });
            mission.Prerequisite(AshenFormalComplaintsV1.MissionId, 1);
            mission.FlagPrerequisite(AshenFormalComplaintsV1.LatanFlag, AshenFormalComplaintsV1.Chosen,
                "Formal Complaints choice 1, Latan's side (flag 250)", 2);

            mission.Objective(NitroSalvage, 10173, 10174, 0, MissionObjectiveState.Incomplete);
            mission.Transition(NitroSalvage);
            mission.Progress(NitroSalvage, MissionProgressEventKind.ItemAcquired, NitroClass, Count);
            mission.Action(NitroSalvage, 1, MissionActionKind.GrantReward, reward: 1);

            mission.Objective(MachinaSalvage, 10175, 10176, 1, MissionObjectiveState.Incomplete);
            mission.Transition(MachinaSalvage);
            mission.Progress(MachinaSalvage, MissionProgressEventKind.ItemAcquired, MachinaClass, Count);

            var marker = 1U;
            foreach (var (_, at) in AshenScavengerNpcsV1.NitroglazerPools)
                mission.Indicator(NitroSalvage, marker++, at.X, at.Y, at.Z, MarkerRadius);
            foreach (var (_, at) in AshenScavengerNpcsV1.MachinaPools)
                mission.Indicator(MachinaSalvage, marker++, at.X, at.Y, at.Z, MarkerRadius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1264",
                "Native1 'Recover 15 Nitroglazer Salvage' 10173/10174, 2 'Recover 15 Machina Salvage' 10175/10176, no counters; no " +
                "objectiveconversation. Texts 10143-10147 (Latan's; the Boneyard's robots). Items class25444 Intact Nitroglazer " +
                "Salvage (template97465), class25445 Intact Machina Salvage (template97466). Names 9277 Latan, 434 Nitroglazer, 7716 " +
                "Hominis Machina Mk XI.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Robotic_Salvage_Recovery",
                "Giver/reward giver Scavenger Vakiel Latan (Ashoka); requirement '??'; follow-up Weapon Salvage Recovery; 95000XP, " +
                "6300 credits; Titan Graviton Armor Gloves 2, Dynamo Motor Assist Armor Legs 2, Wellcare Hazmat Armor Legs 2, " +
                "Wellcare Reflective Armor Legs 2; Nitroglazers and Hominus Machina Mark XI throughout The Boneyard.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:AshenRoboticSalvageV1",
                "Offered after 1254 done with flag250 at 1 (Latan's side). Level40 estimated. New Nitroglazer 790240 and Hominis " +
                "Machina Mk XI 790241 (L40) in Boneyard pools 790243-790246; a salvage a personal 75% drop, 15 of each, taken at the " +
                "turn-in, kept at an abandon; a marker r20 on each pool. Items 23698, 29093, 20802, 36039, one to choose.", 0.3);

            var scene = new MissionSceneDefinition
            {
                ExistingFactObjectiveIds = new() { NitroSalvage, MachinaSalvage },
                Items = new()
                {
                    new MissionItemBinding(NitroKey, NitroTemplate, MissionItemScope.CharacterOwned, Count,
                        MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                        TurnInQuantity: Count,
                        Drop: new MissionItemDropDefinition(new[] { AshenScavengerNpcsV1.Nitroglazer }, NitroSalvage, AshenMissionDataV1.MapContextId, Chance, 1)),
                    new MissionItemBinding(MachinaKey, MachinaTemplate, MissionItemScope.CharacterOwned, Count,
                        MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                        TurnInQuantity: Count,
                        Drop: new MissionItemDropDefinition(new[] { AshenScavengerNpcsV1.Machina }, MachinaSalvage, AshenMissionDataV1.MapContextId, Chance, 1))
                }
            };
            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => AshenMissionDataV1.Remove(migration, MissionId);
    }
}
