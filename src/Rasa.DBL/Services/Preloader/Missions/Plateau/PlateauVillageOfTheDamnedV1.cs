using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Definitions;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// Village Of The Damned (native mission 1981), Plateau: Luminary Sampei at New Velon Village asks
    /// for the Infected Foreans in the Ruins of Velon Das to be put out of their misery.
    ///
    /// Native: name/log/offer/turn-in texts 20858-20861, reminder 20862 "none" (missionconversation);
    /// objective 1 "Kill 10 Infected Foreans." 20863/20864, counter 20865 (missionobjective). Sampei
    /// is creature 510186 (name 8608). The client's indicator name 317 "Infected Foreans"
    /// (missionobjectiveindicatorlanguage) names no mission, so the indicator here has no name.
    ///
    /// Sourced (TaRapedia Village Of The Damned): giver and receiver Sampei, level 30, no requirement,
    /// 40,000 XP, no credits; one of Vitalius Bio Armor Legs 6, Wellcare Graviton Armor Legs 4,
    /// Teleract Mech Armor Helmet 2, Vitalius Stealth Armor Helmet 4.
    ///
    /// Reconstructed (ESTIMATE): the Infected Foreans of PlateauInfectedForeansV1; the rewards read as the
    /// 30-34 band of those types and versions (Bio V06 is Rare, the others Uncommon), no module.
    /// </summary>
    public static class PlateauVillageOfTheDamnedV1
    {
        public const uint MissionId = 1981;
        public const uint Level = 30;
        public const uint Sampei = 510186;
        public const uint Kills = 10;

        public const uint Experience = 40000;
        public const uint Credits = 0;
        public const uint BioLegs = 46107;              // Armor_T3_Bio_V06_RAR_Legs_30_to_34
        public const uint GravitonLegs = 25208;         // Armor_T3_Graviton_V04_UNC_Legs_30_to_34
        public const uint MechHelmet = 32915;           // Armor_T3_Mech_V02_UNC_Helmet_30_to_34
        public const uint StealthHelmet = 40333;        // Armor_T3_Stealth_V04_UNC_Helmet_30_to_34

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PlateauMissionDataV1(migration, MissionId, "Village Of The Damned", 20858, Sampei, Sampei, Level);
            mission.Reward(Experience, Credits, new[] { BioLegs, GravitonLegs, MechHelmet, StealthHelmet });

            mission.Objective(1, 20863, 20864, 0, MissionObjectiveState.Incomplete, counterTextId: 20865);
            mission.Transition(1);
            mission.Progress(1, MissionProgressEventKind.CreatureKilled, PlateauInfectedForeansV1.Infected, Kills, counter: 0);
            mission.Action(1, 1, MissionActionKind.GrantReward, reward: 1);

            mission.Indicator(1, 1, -475, 393, -5, 60);        // the Velon Das ruins

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1981",
                "Native1 'Kill 10 Infected Foreans' 20863/20864 counter20865; texts 20858-20862; indicator name317 not used (no mission). Infected Forean " +
                "name9859, class25607 Redshirt_Forean_Gunner_Infected. Velon Das Ruins marker68 (-475,393,0).");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Village_Of_The_Damned",
                "Giver/receiver Luminary Sampei, New Velon Village; level30; no requirement; 40000XP, no credits; Vitalius Bio " +
                "Legs6 / Wellcare Graviton Legs4 / Teleract Mech Helmet2 / Vitalius Stealth Helmet4. Little health, hit hard.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PlateauVillageOfTheDamnedV1",
                "Infected Forean650024 in the ruins (PlateauInfectedForeansV1: level, strength, pools estimated). Rewards read as " +
                "the 30-34 band: 46107 (Bio V06 RAR legs), 25208, 32915, 40333; brands not checked.", 0.3);

            mission.Enable(new());
        }

        public static void Down(MigrationBuilder migration) => PlateauMissionDataV1.Remove(migration, MissionId);
    }
}
