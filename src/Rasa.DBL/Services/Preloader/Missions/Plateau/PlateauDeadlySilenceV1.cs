using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// Deadly Silence (native mission 1974), Plateau: Luminary Sampei at New Velon Village sends the player
    /// into Velon Hollow (squad instance 2136) to find the four leaders of the Order of Velon. Elder Hathen
    /// lives; the other three are corrupted by the infection (PlateauVelonElderBossesV1).
    ///
    /// Native: texts 20798-20801 and 20802 (missionconversation); objectives (missionobjective) 1 "Find Elder
    /// Hathen" 20849/20850, 2 "Find Guardian Minash" 20852/20853, 3 "Find Shaman Taosari" 20854/20855,
    /// 4 "Find Seer Kadali" 20856/20857, no counters; objectiveconversation 1974,1,2263 (Hathen, creature
    /// 510152 in Velon Hollow) only.
    ///
    /// Sourced: TaRapedia Deadly Silence: giver and receiver Luminary Sampei, New Velon Village;
    /// requirement Village Of The Damned; 90,000 XP, 6,400 credits, "Prodigy/Titan Boots" (Uncommon);
    /// "Find the 4 elders in Velon Hollow".
    ///
    /// Reconstructed (ESTIMATE, the user's choices of 2026-10-22): level 32 (the instance's top, the
    /// bosses' level; no source); objective 1 by the talk to Hathen, 2-4 by the kill of the named boss; any
    /// order. The boots as one Uncommon level 30-34 boot of each armor type (as Going Postal's), the
    /// Prodigy/Titan lines not matched. Indicators (in the instance too): 1 at Hathen (pool 510152), 2-4 at the
    /// boss's site (PlateauVelonElderBossesV1's pool, radius 15).
    /// </summary>
    public static class PlateauDeadlySilenceV1
    {
        public const uint MissionId = 1974;
        public const uint Level = 32;
        public const uint Sampei = PlateauVillageOfTheDamnedV1.Sampei;
        public const uint HathenPackage = PlateauOrderOfVelonV1.HathenPackage;

        /// <summary>Elder Hathen in Velon Hollow (pool 510152, MissionNpcSpawnpoolPreloader): 1's indicator.</summary>
        public static readonly (double X, double Y, double Z) HathenAt = (165.2, 77.64, -520.3);
        public const double HathenRadius = 8, BossRadius = 15;

        public const uint Experience = 90000;
        public const uint Credits = 6400;
        public const uint BioBoots = PlateauGoingPostalV1.BioBoots, GravitonBoots = PlateauGoingPostalV1.GravitonBoots,
            MechBoots = PlateauGoingPostalV1.MechBoots, StealthBoots = PlateauGoingPostalV1.StealthBoots;

        /// <summary>Objectives 2-4 and the boss each one is: (objective, boss, name text, body text).</summary>
        public static readonly (uint Objective, PlateauVelonElderBossesV1.Boss Boss, uint Name, uint Body)[] Leaders =
        {
            (2, PlateauVelonElderBossesV1.Minash, 20852, 20853),
            (3, PlateauVelonElderBossesV1.Taosari, 20854, 20855),
            (4, PlateauVelonElderBossesV1.Kadali, 20856, 20857),
        };

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PlateauMissionDataV1(migration, MissionId, "Deadly Silence", 20798, Sampei, Sampei, Level);
            mission.Reward(Experience, Credits, new[] { BioBoots, GravitonBoots, MechBoots, StealthBoots });
            mission.Prerequisite(PlateauVillageOfTheDamnedV1.MissionId);

            mission.Objective(1, 20849, 20850, 0, MissionObjectiveState.Incomplete);
            mission.Transition(1);
            mission.Conversation(1, HathenPackage);
            mission.Action(1, 1, MissionActionKind.GrantReward, reward: 1);   // as All Along The Watchtowers: the turn-in waits for all four
            mission.Indicator(1, 1, HathenAt.X, HathenAt.Y, HathenAt.Z, HathenRadius);
            foreach (var leader in Leaders)
            {
                mission.Objective(leader.Objective, leader.Name, leader.Body, leader.Objective - 1, MissionObjectiveState.Incomplete);
                mission.Transition(leader.Objective);
                mission.Progress(leader.Objective, MissionProgressEventKind.CreatureKilled, leader.Boss.Id, null);
                mission.Indicator(leader.Objective, leader.Objective, leader.Boss.X, leader.Boss.Y, leader.Boss.Z, BossRadius);
            }

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1974",
                "Native1 'Find Elder Hathen' 20849/20850, 2 Minash 20852/20853, 3 Taosari 20854/20855, 4 Kadali 20856/20857, no " +
                "counters; objectiveconversation 1974,1,2263 only. Texts 20798-20802. Names 10196 Corrupted Seer Kadali, 10197 " +
                "Decayed Guardian Minash, 10198 Taosari the Mad; turn-in: the leaders corrupted by the infection.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Deadly_Silence",
                "Giver/receiver Luminary Sampei, New Velon Village; requirement Village Of The Damned; 90000XP, 6400 credits, " +
                "Prodigy/Titan Boots (Uncommon); find the 4 elders in Velon Hollow.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PlateauDeadlySilenceV1",
                "Level32. Hathen by talk; Minash, Taosari, Kadali killed as named level32 bosses at the three named sites " +
                "(PlateauVelonElderBossesV1, which where is a guess). Boots 46003, 24821, 33947, 40074. Indicators at Hathen and the three sites.", 0.3);

            mission.Enable();
        }

        public static void Down(MigrationBuilder migration) => PlateauMissionDataV1.Remove(migration, MissionId);
    }
}
