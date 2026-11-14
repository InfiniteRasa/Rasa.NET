using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// Chemical Oversight (native mission 1244), Ligo Ashen Desert: Lieutenant Simon Kuang has photos of a specialised Thrax
    /// moving among the Atta unscathed, and wants Thrax Overseer Exing dead.
    ///
    /// Native: texts 9932-9936 (missionconversation): log "Infiltrate the Sheitan Outpost, at the north end of the Sheitan
    /// Highway. Kill Handler Overseer Exing, then report your success to Lieutenant Kuang.", offer "We have compelling evidence
    /// that the Bane are tampering with the Atta ... a specialized Thrax unit moving among the Atta unscathed ...", turn-in
    /// "Great, so you managed to kill off that ugly son of a ..."; objective 1 "Kill Thrax Overseer Exing" 9937/9938, no counter
    /// (missionobjective). No objective conversation. Names 9291 Kuang, 9325 Thrax Overseer Exing (World DB creature 520007, L44).
    ///
    /// Sourced: TaRapedia Chemical Oversight: giver and reward giver Lieutenant Simon Kuang (Shadow's Edge Post); requires Angry
    /// Angry Atta; 90,000 XP, 6,150 credits; one of Olympia Motor Assist Armor Boots (icon 1), CryoGen Stealth Armor Gloves (1),
    /// ChiTech Bio Armor Boots (1), Voltrox Reflective Armor Gloves (1); "bring some friends". World DB: Exing's pool 520007, the
    /// one place Ellatha, TaRapedia's boss page and Targets of Opportunity 1580 give him (triage D10: the client log's "Sheitan
    /// Outpost" is not followed).
    ///
    /// Reconstructed (ESTIMATE):
    ///  - Level 41 (triage D5: after Angry Angry Atta).
    ///  - The kill: Exing's creature (CreatureKilled 520007, once). His pool moved 14 m to ground a player walks to
    ///    (AshenMaunaBarrensNpcsV1; upstream had him on a navmesh island). A marker (r15) on him.
    ///  - The items: the lowest Uncommon template of each kind and icon number in the band of level 41, the nearest icon number
    ///    where the band has none (no V01 Uncommon at 40-44): Motor Assist V04 Boots 28634 (38-42), Stealth V02 Gloves 38952,
    ///    Bio V02 Boots 18770, Reflective V02 Gloves 35740 (39-43); one to choose.
    /// </summary>
    public static class AshenChemicalOversightV1
    {
        public const uint MissionId = 1244;
        public const uint Level = 41;
        public const uint Kuang = AshenMaunaBarrensNpcsV1.Kuang;
        public const uint Kill = 1;
        public const uint Exing = AshenMaunaBarrensNpcsV1.Exing;
        public const double MarkerRadius = 15;

        public const uint Experience = 90000;
        public const uint Credits = 6150;
        public const uint MotorAssistBoots = 28634;     // Armor_T1_MotorAssist_V04_UNC_Boots_38_to_42
        public const uint StealthGloves = 38952;        // Armor_T3_Stealth_V02_UNC_Gloves_40_to_44
        public const uint BioBoots = 18770;             // Armor_T3_Bio_V02_UNC_Boots_40_to_44
        public const uint ReflectiveGloves = 35740;     // Armor_T2_Reflective_V02_UNC_Gloves_39_to_43

        public static void Up(MigrationBuilder migration)
        {
            var mission = new AshenMissionDataV1(migration, MissionId, "Chemical Oversight", 9932, Kuang, Kuang, Level);
            mission.Reward(Experience, Credits, new[] { MotorAssistBoots, StealthGloves, BioBoots, ReflectiveGloves });
            mission.Prerequisite(AshenAngryAngryAttaV1.MissionId);

            mission.Objective(Kill, 9937, 9938, 0, MissionObjectiveState.Incomplete);
            mission.Transition(Kill);
            mission.Progress(Kill, MissionProgressEventKind.CreatureKilled, Exing, null);
            mission.Action(Kill, 1, MissionActionKind.GrantReward, reward: 1);
            var exing = AshenMaunaBarrensNpcsV1.ExingAt;
            mission.Indicator(Kill, 1, exing.X, exing.Y, exing.Z, MarkerRadius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1244",
                "Native1 'Kill Thrax Overseer Exing' 9937/9938, no counter; no objectiveconversation. Texts 9932-9936 (a Thrax among " +
                "the Atta; the log names the Sheitan Outpost). Names 9291 Kuang, 9325 Exing.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Chemical_Oversight",
                "Giver/reward giver Lieutenant Simon Kuang (Shadow's Edge Post); requires Angry Angry Atta; 90000XP, 6150 credits; " +
                "Olympia Motor Assist Armor Boots 1, CryoGen Stealth Armor Gloves 1, ChiTech Bio Armor Boots 1, Voltrox Reflective " +
                "Armor Gloves 1. Exing at -281, -252 (Ellatha; World DB pool 520007).");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:AshenChemicalOversightV1",
                "Level41 estimated. CreatureKilled 520007 once. Exing's pool moved 14 m off its navmesh island to ground walked to " +
                "from the Stonewall Ridge waypoint; a marker r15 on him. Items 28634, 38952, 18770, 35740 (nearest icon, band of " +
                "level 41), one to choose.", 0.3);

            mission.Enable();
        }

        public static void Down(MigrationBuilder migration) => AshenMissionDataV1.Remove(migration, MissionId);
    }
}
