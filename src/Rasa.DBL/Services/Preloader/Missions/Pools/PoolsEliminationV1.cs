using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Pools
{
    /// <summary>
    /// Elimination (native mission 941), Valverde Pools: Colonel 'Snake' Washington at the Snake Pit wants Overseer
    /// Prysiam, the Bane commander of the fort in the west, dead.
    ///
    /// Native: texts 5115-5118 and 7579 (missionconversation); objective 1 "Eliminate Overseer Prysiam" 5130/7580, counter
    /// 5131 "Prysiam Killed" (missionobjective); no objective conversation.
    ///
    /// Sourced: TaRapedia Elimination: giver and receiver Colonel 'Snake' Washington; requires Sabotage the Tower; 60,000
    /// XP, 5,250 credits; one of AccuMax Chaingun level 33, AccuMax Incendiary Net Gun 36, Vitalius Exothermic Polarity
    /// Gun 33, Eclipse Shotgun 32; Prysiam is reached from the wall. TR MMO DB: level 35, related mob Overseer Prysiam.
    /// World DB: Overseer Prysiam 520062 (pool 520062 at (-837, 791, -14), respawn 500 s) is in the fort already.
    ///
    /// Reconstructed (ESTIMATE): the kill of the public Prysiam counts (anyone's kill credit, as the server gives it).
    /// Chaingun read as the Machine Gun, Incendiary as Fire, Exothermic as Fire, Eclipse Shotgun as the physical one; the
    /// items the lowest Uncommon templates of the bands that hold those levels, one to choose.
    /// </summary>
    public static class PoolsEliminationV1
    {
        public const uint MissionId = 941;
        public const uint Level = 35;
        public const uint Snake = PoolsWashingtonPlaceV1.Snake;
        public const uint Kill = 1;
        public const uint Prysiam = 520062;
        public static readonly (double X, double Y, double Z) PrysiamAt = (-837, 791, -14);

        public const uint Experience = 60000;
        public const uint Credits = 5250;
        public const uint Chaingun = 48296;             // Weapon_Avatar_MachineGun_Physical_UNC_33_to_37
        public const uint IncendiaryNetGun = 45812;     // Weapon_Avatar_NetGun_Fire_UNC_36_to_40
        public const uint ExothermicPolarityGun = 48676; // Weapon_Avatar_PolarityGun_Fire_UNC_33_to_37
        public const uint Shotgun = 14815;              // Weapon_Avatar_Shotgun_v1_Physical_UNC_32_to_36

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PoolsMissionDataV1(migration, MissionId, "Elimination", 5115, Snake, Snake, Level);
            mission.Reward(Experience, Credits, new[] { Chaingun, IncendiaryNetGun, ExothermicPolarityGun, Shotgun });
            mission.Prerequisite(PoolsSabotageTheTowerV1.MissionId);

            mission.Objective(Kill, 5130, 7580, 0, MissionObjectiveState.Incomplete, counterTextId: 5131);
            mission.Transition(Kill);
            mission.Progress(Kill, MissionProgressEventKind.CreatureKilled, Prysiam, 1, counter: 0);
            mission.Action(Kill, 1, MissionActionKind.GrantReward, reward: 1);

            mission.Indicator(Kill, 1, PrysiamAt.X, PrysiamAt.Y, PrysiamAt.Z, 15);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/941",
                "Native1 'Eliminate Overseer Prysiam' 5130/7580 counter5131 'Prysiam Killed'; no objectiveconversation. Texts " +
                "5115-5118, 7579.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Elimination",
                "Giver/receiver Colonel 'Snake' Washington; requires Sabotage the Tower; 60000XP, 5250 credits; one of AccuMax " +
                "Chaingun33, AccuMax Incendiary Net Gun36, Vitalius Exothermic Polarity Gun33, Eclipse Shotgun32. TR MMO DB level35, " +
                "related mob Overseer Prysiam.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PoolsEliminationV1",
                "The public Prysiam 520062 (pool 520062) counts. Chaingun as Machine Gun, Incendiary/Exothermic as Fire. Items " +
                "48296, 45812, 48676, 14815, one to choose.", 0.3);

            mission.Enable();
        }

        public static void Down(MigrationBuilder migration) => PoolsMissionDataV1.Remove(migration, MissionId);
    }
}
