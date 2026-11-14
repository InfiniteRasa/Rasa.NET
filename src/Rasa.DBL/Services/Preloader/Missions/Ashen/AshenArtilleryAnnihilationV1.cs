using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// Artillery Annihilation (native mission 1249), Ligo Ashen Desert: Lt. Colonel Dania Caldwell at Shadow's Edge Post wants the
    /// four Bane mortars along the northern Sheitan Highway that shell the post destroyed.
    ///
    /// Native: texts 9984-9988 (missionconversation): log "Travel to the northern part of the Sheitan Highway and take out the 4
    /// Bane Heavy Mortar that are firing at Shadow's Edge Post. Report back to Lieutenant Colonel Caldwell once you are
    /// finished.", offer "I need someone to destroy those mortar batteries the Thrax set up along the northern roadway ... you'll
    /// find some mortar emplacements along the side of the road. These 4 emplacements need to be destroyed. ... you may want to
    /// infiltrate the area by coming around through the Mauna Barrens.", turn-in "I like meeting someone that can get the job
    /// done."; objectives (missionobjective) 1-4 "Destroy Mortar 1" .. "4" 9989/9990, 9991/9992, 9993/9994, 9995/9996, no
    /// counters. No objective conversation. Marker names (missionobjectiveindicatorlanguage) 160-163 "Bane Heavy Mortar 1" ..
    /// "4". Name 9262 Caldwell.
    ///
    /// Sourced: TaRapedia Artillery Annihilation: giver and reward giver Lt. Colonel Dania Caldwell (Shadow's Edge Post); no
    /// requirement; 4 emplacements; 90,000 XP, 6,150 credits; one of CryoGen Graviton Armor Vest (icon 1), Pulsar Hazmat Armor
    /// Gloves (1), Voltrox Reflective Armor Vest (1), Pathogex Bio Armor Vest (1). World DB: the four Bane Mortar pools by the
    /// highway's Bane camp 592191-592194 (BaneMortars, on the client map's mortar pads).
    ///
    /// Reconstructed (ESTIMATE):
    ///  - Level 41 (triage D5: the zone band; no chain).
    ///  - Mortar n: the kill of the copy AshenSheitanHighwayNpcsV1 put in pool n (south to north along the road), once
    ///    (CreatureKilled, no counter); objectives in any order. A marker r10 on each mortar, keyed by the client's names 160-163.
    ///  - The items: the lowest Uncommon template of each kind and the nearest icon number in the band of level 41 (no V01
    ///    Uncommon at 39-44): Graviton V02 Vest 24086 (40-44), Hazmat V02 Gloves 20504 (39-43), Reflective V02 Vest 36188
    ///    (39-43), Bio V02 Vest 19286 (40-44); one to choose.
    /// </summary>
    public static class AshenArtilleryAnnihilationV1
    {
        public const uint MissionId = 1249;
        public const uint Level = 41;
        public const uint Caldwell = AshenLeadInNpcsV1.Caldwell;
        public const uint Mortars = 4;
        public const uint FirstMortarName = 160;        // missionobjectiveindicatorlanguage 160 "Bane Heavy Mortar 1"
        public const double MarkerRadius = 10;

        public const uint Experience = 90000;
        public const uint Credits = 6150;
        public const uint GravitonVest = 24086;         // Armor_T3_Graviton_V02_UNC_Vest_40_to_44
        public const uint HazmatGloves = 20504;         // Armor_T2_Hazmat_V02_UNC_Gloves_39_to_43
        public const uint ReflectiveVest = 36188;       // Armor_T2_Reflective_V02_UNC_Vest_39_to_43
        public const uint BioVest = 19286;              // Armor_T3_Bio_V02_UNC_Vest_40_to_44

        /// <summary>The objective of Mortar <paramref name="number"/> (1-4): the client's objective of the same number.</summary>
        public static uint Destroy(int number) => (uint)number;

        public static void Up(MigrationBuilder migration)
        {
            var mission = new AshenMissionDataV1(migration, MissionId, "Artillery Annihilation", 9984, Caldwell, Caldwell, Level);
            mission.Reward(Experience, Credits, new[] { GravitonVest, HazmatGloves, ReflectiveVest, BioVest });

            for (var number = 1; number <= Mortars; number++)
            {
                var (creature, _, at) = AshenSheitanHighwayNpcsV1.Mortars[number - 1];
                var objective = Destroy(number);
                mission.Objective(objective, 9987 + 2 * (uint)number, 9988 + 2 * (uint)number, (uint)number - 1, MissionObjectiveState.Incomplete);
                mission.Transition(objective);
                mission.Progress(objective, MissionProgressEventKind.CreatureKilled, creature, null);
                mission.Action(objective, 1, MissionActionKind.GrantReward, reward: 1);
                mission.NamedIndicator(objective, FirstMortarName + (uint)number - 1, at.X, at.Y, at.Z, MarkerRadius);
            }

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1249",
                "Native1-4 'Destroy Mortar 1..4' 9989/9990 .. 9995/9996, no counters; no objectiveconversation. Texts 9984-9988 " +
                "(4 Bane Heavy Mortar on the northern Sheitan Highway). Indicator names 160-163 'Bane Heavy Mortar 1..4'. Name " +
                "9262 Caldwell.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Artillery_Annihilation",
                "Giver/reward giver Lt. Colonel Dania Caldwell (Shadow's Edge Post); no requirement; 4 emplacements; 90000XP, 6150 " +
                "credits; CryoGen Graviton Armor Vest 1, Pulsar Hazmat Armor Gloves 1, Voltrox Reflective Armor Vest 1, Pathogex " +
                "Bio Armor Vest 1.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:AshenArtilleryAnnihilationV1",
                "Level41 estimated. The highway mortar pools 592191-592194 hold copies 790081-790084 of 592011; Mortar n = the " +
                "kill of copy n, once; a marker r10 on each, keyed 160-163. Items 24086, 20504, 36188, 19286 (nearest icon, band " +
                "of level 41), one to choose.", 0.3);

            mission.Enable();
        }

        public static void Down(MigrationBuilder migration) => AshenMissionDataV1.Remove(migration, MissionId);
    }
}
