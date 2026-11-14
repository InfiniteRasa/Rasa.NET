using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// Xanxless Job (native mission 1262), Ligo Ashen Desert: Lieutenant Jan Blood at Boneyard Outpost wants the Xanx that dig
    /// tunnels and ambush his men in the Boneyard thinned: six of them.
    ///
    /// Native: texts 10121-10125 (missionconversation): log "Travel to the Boneyard and kill 6 Xanx. Report to Lieutenant Jan
    /// Blood at Boneyard Outpost when you are finished.", offer "There are Xanx out there, digging tunnels and ambushing our
    /// boys ...", turn-in "What is it with all the giant bugs? ...", reminder "So, have you killed your daily quota of bugs
    /// today? ..."; objective (missionobjective) 1 "Kill 6 Xanx" 10126/10127 counter 10128 "Xanx Killed". No objective
    /// conversation. Names 9305 Blood, 471 Xanx Burrower (World DB 540020).
    ///
    /// Sourced: TaRapedia Xanxless Job: giver and reward giver Lieutenant Jan Blood (Boneyard Outpost); no requirement; 95,000
    /// XP, 6,300 credits; one of Hellstrom Bio Armor Legs (icon 4), Hellstrom Mech Armor Legs (4), Voltrox Graviton Armor Boots
    /// (1), Wellcare Motor Assist Armor Helmet (9). TR MMO DB: the same XP and credits (its level 30 is its default, triage D5).
    ///
    /// Reconstructed (ESTIMATE):
    ///  - Level 40 (triage D5: the zone band, the first of Blood's line).
    ///  - The Xanx are the World DB's Xanx - Ashen Desert 540020 ("Xanx Burrower"), the only Xanx row of the zone: a kill of it
    ///    counts (CreatureKilled, counter 0, 6), wherever it stands; three new pools of three in the Boneyard
    ///    (AshenBoneyardNpcsV1), a marker (r20) on each.
    ///  - The items: the lowest Uncommon template of each kind and the nearest icon number in the band of level 40 (client
    ///    entityclass names): Bio V04 Legs 46029, Mech V04 Legs 34374, Graviton Boots 23569 (V02: no Uncommon V01 at 40-44),
    ///    Motor Assist Helmet 31241 (V07: no Uncommon V08 or V09 at 38-42); one to choose.
    /// </summary>
    public static class AshenXanxlessJobV1
    {
        public const uint MissionId = 1262;
        public const uint Level = 40;
        public const uint Blood = AshenBoneyardNpcsV1.Blood;
        public const uint Kill = 1;
        public const uint Kills = 6;
        public const uint Xanx = AshenBoneyardNpcsV1.Xanx;
        public const double MarkerRadius = 20;

        public const uint Experience = 95000;
        public const uint Credits = 6300;
        public const uint BioLegs = 46029;              // Armor_T3_Bio_V04_UNC_Legs_40_to_44
        public const uint MechLegs = 34374;             // Armor_T3_Mech_V04_UNC_Legs_40_to_44
        public const uint GravitonBoots = 23569;        // Armor_T3_Graviton_V02_UNC_Boots_40_to_44
        public const uint MotorAssistHelmet = 31241;    // Armor_T1_MotorAssist_V07_UNC_Helmet_38_to_42

        public static void Up(MigrationBuilder migration)
        {
            var mission = new AshenMissionDataV1(migration, MissionId, "Xanxless Job", 10121, Blood, Blood, Level);
            mission.Reward(Experience, Credits, new[] { BioLegs, MechLegs, GravitonBoots, MotorAssistHelmet });

            mission.Objective(Kill, 10126, 10127, 0, MissionObjectiveState.Incomplete, counterTextId: 10128);
            mission.Transition(Kill);
            mission.Progress(Kill, MissionProgressEventKind.CreatureKilled, Xanx, Kills, counter: 0);
            mission.Action(Kill, 1, MissionActionKind.GrantReward, reward: 1);
            var marker = 1U;
            foreach (var (_, at) in AshenBoneyardNpcsV1.XanxPools)
                mission.Indicator(Kill, marker++, at.X, at.Y, at.Z, MarkerRadius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1262",
                "Native1 'Kill 6 Xanx' 10126/10127 counter10128 'Xanx Killed'; no objectiveconversation. Texts 10121-10125 (the " +
                "Boneyard; report to Lieutenant Jan Blood at Boneyard Outpost). Names 9305 Blood, 471 Xanx Burrower.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Xanxless_Job",
                "Giver/reward giver Lieutenant Jan Blood (Boneyard Outpost); no requirement; 95000XP, 6300 credits; Hellstrom Bio " +
                "Armor Legs 4, Hellstrom Mech Armor Legs 4, Voltrox Graviton Armor Boots 1, Wellcare Motor Assist Armor Helmet 9.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:AshenXanxlessJobV1",
                "Level40 estimated. Xanx = World DB 540020 (Xanx Burrower); CreatureKilled, counter 0, 6; new pools 790122-790124 " +
                "of three in the Boneyard, a marker r20 on each. Items 46029, 34374, 23569, 31241 (lowest Uncommon, nearest icon, " +
                "band of level 40), one to choose.", 0.3);

            mission.Enable();
        }

        public static void Down(MigrationBuilder migration) => AshenMissionDataV1.Remove(migration, MissionId);
    }
}
