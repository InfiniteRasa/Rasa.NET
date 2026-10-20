using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// Giant Killer (native mission 939), Plateau: Colonel Bosley at Camp Resistance sends the
    /// player to kill Goliath, a giant Kael east of the camp, and bring back his head.
    ///
    /// Native: name/log/offer/turn-in/reminder texts 5104-5107 and 6353 (missionconversation),
    /// objective 1 "Get Goliath's Head." 5108/6354 and objective 2 "Return to Bosley." 5109/6355
    /// (missionobjective), Bosley's hand-in line 5110 on package 1001, flag 1
    /// (objectiveconversation 939,2,1001,1,1). The head is item class 21769, template 42654.
    ///
    /// Sourced (TaRapedia Giant_Killer): giver and receiver Colonel Bosley, level 30, 50,000 XP,
    /// 4,950 credits, one of four Vextronics weapons; Goliath patrols east of Camp Resistance
    /// with two to four other Kael. Reconstructed: the weapons are the Uncommon base templates
    /// of the level band that holds 30 (no module, as the Wilderness Vextronics rewards); the
    /// head is a personal 100% drop picked up from Goliath's corpse; the guards are two to four
    /// "Kael - Plateau" (531088) added to Goliath's pool, which wanders, not patrols; the
    /// objective 1 indicator is an area over Goliath's spawn, objective 2's is on Bosley.
    ///
    /// Goliath (520051, pool 520051) and Bosley (510176, pool 510176, package 1001) already exist.
    /// Rows are plain SQL where no model maps the table (spawnpool), otherwise EF data operations.
    /// </summary>
    public static class PlateauGiantKillerV1
    {
        public const uint MissionId = 939;
        public const string Revision = PlateauMissionDataV1.Revision;
        public const uint MapContextId = PlateauMissionDataV1.MapContextId;
        public const uint CategoryId = PlateauMissionDataV1.CategoryId;
        public const uint Level = 30;

        public const uint Bosley = 510176;              // creature, "Colonel Bosley - Camp Resistance"
        public const uint BosleyPackage = 1001;
        public const uint Goliath = 520051;             // creature, "Goliath - Plateau"
        public const uint GoliathPool = 520051;
        public const uint KaelGuard = 531088;           // creature, "Kael - Plateau"
        public const uint GuardsMin = 2;
        public const uint GuardsMax = 4;

        public const uint HeadTemplate = 42654;
        public const uint HeadClass = 21769;
        public const string HeadItemKey = "goliath-head";

        public const uint Experience = 50000;
        public const uint Credits = 4950;
        public const uint CryogenicBlade = 47644;       // Weapon_Avatar_Blade_Ice_UNC_30_to_30
        public const uint CryogenicDisperser = 48764;   // Weapon_Avatar_PropellantGun_Ice_UNC_30_to_31
        public const uint VirulentInjectorGun = 120733; // Weapon_Avatar_InjectionGun_Virulent_UNC_27_to_31
        public const uint VirulentLeechGun = 16597;     // Weapon_Avatar_LeechGun_Virulent_UNC_29_to_33

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PlateauMissionDataV1(migration, MissionId, "Giant Killer", 5104, Bosley, Bosley, Level);
            mission.Reward(Experience, Credits,
                new[] { CryogenicBlade, CryogenicDisperser, VirulentInjectorGun, VirulentLeechGun });

            mission.Objective(1, 5108, 6354, 0, MissionObjectiveState.Incomplete);
            mission.Objective(2, 5109, 6355, 1, MissionObjectiveState.Inactive);

            // 1: picking up Goliath's head (item class, not template) completes it and opens 2.
            mission.Transition(1);
            mission.Progress(1, MissionProgressEventKind.ItemAcquired, HeadClass, 1);
            mission.Action(1, 1, MissionActionKind.RevealObjective, targetObjective: 2);
            mission.Action(1, 2, MissionActionKind.ActivateObjective, targetObjective: 2,
                state: MissionObjectiveState.Incomplete);

            // 2: Bosley's hand-in topic (package 1001, flag 1); the reward is paid at turn-in.
            mission.Transition(2);
            mission.Conversation(2, BosleyPackage);
            mission.Action(2, 1, MissionActionKind.GrantReward, reward: 1);

            mission.Indicator(1, 1, -440.0, 442.9, -230.0, 60);    // Goliath's area (spawn of pool 520051)
            mission.Indicator(2, 2, -632.3, 447.95, -232.5);       // Colonel Bosley (pool 510176)

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/939",
                "Native1 'Get Goliath's Head' (5108/6354), class21769/template42654; 2 'Return to Bosley' (5109/6355), " +
                "objectiveconversation 939,2,1001,1,1 text5110. Texts 5104-5107, 6353.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Giant_Killer",
                "Giver/receiver Colonel Bosley, Camp Resistance, Plateau; level30; 50000XP, 4950 credits; one of Vextronics " +
                "Cryogenic Blade/Cryogenic Disperser/Virulent Injector Gun/Virulent Leech Gun; follow-up Incommunicado. " +
                "Goliath patrols east of Camp Resistance with two to four other Kael.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PlateauGiantKillerV1",
                "Uncommon base weapons of the level-30 band, no module (as the Wilderness Vextronics rewards): 47644, 48764, " +
                "120733, 16597. One personal 100% head drop from Goliath520051 on map1497, actual pickup, one turn-in cost. " +
                "Two to four Kael531088 added to pool520051, which wanders (no group patrol). Indicators: area r60 on " +
                "Goliath's spawn (-440,-230; Ellatha), Bosley's pool.");

            mission.Enable(new MissionSceneDefinition
            {
                ExistingFactObjectiveIds = new() { 1 },
                Items = new()
                {
                    new MissionItemBinding(HeadItemKey, HeadTemplate, MissionItemScope.CharacterOwned, 1,
                        MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                        MissionItemCleanupDisposition.Retain, TurnInQuantity: 1,
                        Drop: new MissionItemDropDefinition(new[] { Goliath }, 1, MapContextId, 100, 1))
                }
            });

            // Goliath's guards: slot 2 of his pool, which holds only him (slots 2-6 empty).
            migration.Sql($"UPDATE spawnpool SET creature_2_Id = {KaelGuard}, creature_2_min_count = {GuardsMin}, " +
                          $"creature_2_max_count = {GuardsMax} WHERE id = {GoliathPool};");
        }

        public static void Down(MigrationBuilder migration)
        {
            migration.Sql("UPDATE spawnpool SET creature_2_Id = 0, creature_2_min_count = 0, " +
                          $"creature_2_max_count = 0 WHERE id = {GoliathPool};");
            PlateauMissionDataV1.Remove(migration, MissionId);
        }
    }
}
