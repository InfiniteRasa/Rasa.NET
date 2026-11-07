using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Definitions;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Crucible
{
    /// <summary>
    /// Minesweeper (native mission 1232), Ligo Crucible: Private Drake wants twenty Bane Smart Mines destroyed in the trench
    /// west of the Fort Intrepid ruins, on the supply road to Staal.
    ///
    /// Native: texts 9807-9810, 9811 (missionconversation): log "Perform minesweeping detail by destroying 2O Bane Smart
    /// Mines near the trenches due west of the Fort Intrepid ruins. Report back to Private Drake when you've completed the
    /// task."; objective 1 "Destroy 20 Smart Mines" 9812/9813, counter 9814 "Smart Mines Destroyed" (missionobjective); no
    /// objective conversation.
    ///
    /// Sourced: TaRapedia Minesweeper: giver and reward giver Private Drake; requires A Fort Called Intrepid; 75,000 XP,
    /// 5,700 credits; one of Pathogex Mech Armor Boots (icon 3), Audiodyne Stealth Armor Boots (2), Audiodyne Graviton Armor
    /// Legs (4), Hellstrom Motor Assist Armor Helmet (6); "The mines don't actually explode and they do very little damage".
    /// TR MMO DB quest list: level 40.
    ///
    /// Reconstructed (ESTIMATE):
    ///  - The mines the new Smart Mines 770014 (a copy of the Necromite, CrucibleMinefieldAndSalvageNpcsV1) in pools
    ///    770014-770016; a kill of one counts (CreatureKilled, counter 0, 20), the mine's self-destruct on the player too
    ///    (CreatureBombs: the kill is its target's). They lie buried until a player is 8 m off (ESTIMATE) and come up with their
    ///    birth 155/7528 (MawLurk, as the Maws).
    ///  - The items the lowest Uncommon templates of the kinds and icon numbers in the band of level 40: Mech V03 Boots
    ///    33342, Stealth V02 Boots 38823, Graviton V04 Legs 25248, Motor Assist V06 Helmet 30474; one to choose. A marker
    ///    (radius 20) on each mine pool.
    /// </summary>
    public static class CrucibleMinesweeperV1
    {
        public const uint MissionId = 1232;
        public const uint Level = 40;
        public const uint Drake = CrucibleOutpostIntrepidNpcsV1.Drake;
        public const uint Destroy = 1;
        public const uint Mines = 20;
        public const uint SmartMine = CrucibleMinefieldAndSalvageNpcsV1.SmartMine;
        public const double MarkerRadius = 20;

        public const uint Experience = 75000;
        public const uint Credits = 5700;
        public const uint MechBoots = 33342;            // Armor_T3_Mech_V03_UNC_Boots_40_to_44
        public const uint StealthBoots = 38823;         // Armor_T3_Stealth_V02_UNC_Boots_40_to_44
        public const uint GravitonLegs = 25248;         // Armor_T3_Graviton_V04_UNC_Legs_40_to_44
        public const uint MotorAssistHelmet = 30474;    // Armor_T1_MotorAssist_V06_UNC_Helmet_38_to_42

        public static void Up(MigrationBuilder migration)
        {
            var mission = new CrucibleMissionDataV1(migration, MissionId, "Minesweeper", 9807, Drake, Drake, Level);
            mission.Reward(Experience, Credits, new[] { MechBoots, StealthBoots, GravitonLegs, MotorAssistHelmet });
            mission.Prerequisite(CrucibleFortCalledIntrepidV1.MissionId);

            mission.Objective(Destroy, 9812, 9813, 0, MissionObjectiveState.Incomplete, counterTextId: 9814);
            mission.Transition(Destroy);
            mission.Progress(Destroy, MissionProgressEventKind.CreatureKilled, SmartMine, Mines, counter: 0);
            mission.Action(Destroy, 1, MissionActionKind.GrantReward, reward: 1);
            var index = 0U;
            foreach (var (_, at) in CrucibleMinefieldAndSalvageNpcsV1.MinePools)
                mission.Indicator(Destroy, ++index, at.X, at.Y, at.Z, MarkerRadius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1232",
                "Native1 'Destroy 20 Smart Mines' 9812/9813 counter9814 'Smart Mines Destroyed'; no objectiveconversation. Texts " +
                "9807-9811 (Bane Smart Mines near the trenches due west of the Fort Intrepid ruins). No Smart Mine name or class; " +
                "creaturenamelanguage 433 Necromite.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Minesweeper",
                "Giver/reward giver Private Drake; requires A Fort Called Intrepid; 75000XP, 5700 credits; one of Mech Boots 3, " +
                "Stealth Boots 2, Graviton Legs 4, Motor Assist Helmet 6; the mines don't explode, very little damage. Smart Mine " +
                "= Necromite (TaRapedia). TR MMO DB level40.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:CrucibleMinesweeperV1",
                "New Smart Mines 770014 (copy of Necromite 570003, level40, 1500 HP) in pools 770014-770016 in the trench west of " +
                "the Fort Intrepid waypoint; CreatureKilled 20, counter 0; they lie buried until a player is 8 m off (ESTIMATE) and come up with their birth 155/7528 (MawLurk). Items 33342, 38823, 25248, 30474, one to " +
                "choose. A marker r20 on each pool.", 0.3);

            mission.Enable();
        }

        public static void Down(MigrationBuilder migration) => CrucibleMissionDataV1.Remove(migration, MissionId);
    }
}
