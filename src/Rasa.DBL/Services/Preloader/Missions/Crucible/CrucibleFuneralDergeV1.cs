using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Definitions;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Crucible
{
    /// <summary>
    /// Funeral Derge (native mission 1241), Ligo Crucible: Captain DeLisi wants Executioner Derge, the Thrax Commander who
    /// led the attack on Fort Intrepid, killed in the ruins.
    ///
    /// Native: texts 9902-9905, 9906 (missionconversation): log "Executioner Derge, the Thrax Commander who spearheaded the
    /// attack on Fort Intrepid, has been spotted in the ruins of Fort Intrepid. Captain DeLisi wants him taken out!";
    /// objective 1 "Kill Executioner Derge" 9907/9908 ("a Thrax Commander patroling inside the Fort Intrepid ruins"), no
    /// counter (missionobjective); no objective conversation. Name 9179 Executioner Derge (creaturenamelanguage).
    ///
    /// Sourced: TaRapedia Funeral Derge: giver and reward giver Captain DeLisi; requires A Fort Called Intrepid; 75,000 XP,
    /// 5,700 credits; one of Teleract Bio Armor Helmet (icon 3), Pathogex Mech Armor Helmet (3), Olympia Graviton Armor Boots
    /// (4), Olympia Reflective Armor Vest (2). TaRapedia Executioner Derge and Ellatha: he patrols the ramparts, up the
    /// stairs. Patch 1.4 (EG-1.4, PN-080129): "'Funeral Derge' - Objective indicator is now in proper location." World DB:
    /// Derge 520013 (level 47) in pool 520013, moved onto the rampart by CrucibleMinefieldAndSalvageNpcsV1.
    ///
    /// Reconstructed (ESTIMATE):
    ///  - Level 40 (no source; the level of 1232 and 1150, the other A Fort Called Intrepid follow-ups with the same XP).
    ///  - The kill of Derge 520013 (CreatureKilled, no counter: the client has no counter text); he stands, the patrol is not built (optional).
    ///  - The items the lowest Uncommon templates of the kinds and icon numbers in the band of level 40: Bio V03 Helmet 19673,
    ///    Mech V03 Helmet 33600, Graviton V04 Boots 24861, Reflective V02 Vest 36188; one to choose. A marker (radius 15) on
    ///    Derge's place on the rampart.
    /// </summary>
    public static class CrucibleFuneralDergeV1
    {
        public const uint MissionId = 1241;
        public const uint Level = 40;
        public const uint DeLisi = CrucibleOutpostIntrepidNpcsV1.DeLisi;
        public const uint Kill = 1;
        public const uint Derge = CrucibleMinefieldAndSalvageNpcsV1.Derge;
        public const double MarkerRadius = 15;

        public const uint Experience = 75000;
        public const uint Credits = 5700;
        public const uint BioHelmet = 19673;            // Armor_T3_Bio_V03_UNC_Helmet_40_to_44
        public const uint MechHelmet = 33600;           // Armor_T3_Mech_V03_UNC_Helmet_40_to_44
        public const uint GravitonBoots = 24861;        // Armor_T3_Graviton_V04_UNC_Boots_40_to_44
        public const uint ReflectiveVest = 36188;       // Armor_T2_Reflective_V02_UNC_Vest_39_to_43

        public static void Up(MigrationBuilder migration)
        {
            var mission = new CrucibleMissionDataV1(migration, MissionId, "Funeral Derge", 9902, DeLisi, DeLisi, Level);
            mission.Reward(Experience, Credits, new[] { BioHelmet, MechHelmet, GravitonBoots, ReflectiveVest });
            mission.Prerequisite(CrucibleFortCalledIntrepidV1.MissionId);

            mission.Objective(Kill, 9907, 9908, 0, MissionObjectiveState.Incomplete);
            mission.Transition(Kill);
            mission.Progress(Kill, MissionProgressEventKind.CreatureKilled, Derge, null);
            mission.Action(Kill, 1, MissionActionKind.GrantReward, reward: 1);
            var at = CrucibleMinefieldAndSalvageNpcsV1.DergeAt;
            mission.Indicator(Kill, 1, at.X, at.Y, at.Z, MarkerRadius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1241",
                "Native1 'Kill Executioner Derge' 9907/9908 (a Thrax Commander patroling inside the Fort Intrepid ruins), no " +
                "counter; no objectiveconversation. Texts 9902-9906. Name 9179 Executioner Derge.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Funeral_Derge",
                "Giver/reward giver Captain DeLisi; requires A Fort Called Intrepid; 75000XP, 5700 credits; one of Bio Helmet 3, " +
                "Mech Helmet 3, Graviton Boots 4, Reflective Vest 2. Derge patrols the ramparts (stairs; TaRapedia, Ellatha); wall " +
                "spawn 859 188 476 (IGN, EG-1.4).");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:CrucibleFuneralDergeV1",
                "Level40 (no source). Kill of upstream Derge 520013, his pool moved from the control point flag onto the rampart " +
                "(859, 188.68, 476); no patrol. Items 19673, 33600, 24861, 36188, one to choose. A marker r15 on him.", 0.3);

            mission.Enable();
        }

        public static void Down(MigrationBuilder migration) => CrucibleMissionDataV1.Remove(migration, MissionId);
    }
}
