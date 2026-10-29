using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Definitions;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Pools
{
    /// <summary>
    /// Spoils of War (native mission 1326), Valverde Pools: Lieutenant Chester sends the player back to Colonel 'Snake'
    /// Washington for a reward, the end of the supply arc.
    ///
    /// Native: texts 10832-10836 (missionconversation): log "Report to Colonel Snake Washington at the Snake Pit", turn-in
    /// (Snake) "I've been saving something special as a reward for someone like you."; objective 1 "Report to Snake"
    /// 10837/10838, no counters (missionobjective); talk 1326,1 on package 331 (Snake, text 10839 "At ease soldier.").
    ///
    /// Sourced: TaRapedia Spoils of War: giver Lieutenant Chester, reward from Colonel 'Snake' Washington; requires Smash
    /// and Grab; no follow-up; 32,500 XP, 3,600 credits, 2 Class VII Advanced Med Packs, 2 Class VII Power Chargers, 4 Class
    /// VII Cryogenic Grenades, 4 Class VII Incendiary Grenades.
    ///
    /// Reconstructed (ESTIMATE): talk only, no scene. Level 35 (no source; as Smash and Grab, the mission before it).
    /// Cryogenic and Incendiary read as the Ice and Fire grenades; the items the lowest template of each, all given. The
    /// marker on Snake.
    /// </summary>
    public static class PoolsSpoilsOfWarV1
    {
        public const uint MissionId = 1326;
        public const uint Level = 35;
        public const uint Chester = PoolsSupplyNpcsV1.Chester;
        public const uint Snake = PoolsWashingtonPlaceV1.Snake, SnakePackage = Plateau.PlateauSnakePitV1.SnakePackage;
        public const uint Report = 1;

        public const uint Experience = 32500;
        public const uint Credits = 3600;
        public const uint MedPack = PoolsThirstyHermitV1.MedPack, MedPacks = 2;
        public const uint PowerCharger = 118907, PowerChargers = 2;    // Consumable_Restore_Power_Kit_35_to_39
        public const uint IceGrenade = PoolsCultDividedV1.IceGrenade, IceGrenades = 4;
        public const uint FireGrenade = PoolsCultDividedV1.FireGrenade, FireGrenades = 4;

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PoolsMissionDataV1(migration, MissionId, "Spoils of War", 10832, Chester, Snake, Level);
            mission.Reward(Experience, Credits, new (uint, uint)[0], new[]
            {
                (MedPack, MedPacks), (PowerCharger, PowerChargers), (IceGrenade, IceGrenades), (FireGrenade, FireGrenades)
            });
            mission.Prerequisite(PoolsSmashAndGrabV1.MissionId);

            mission.Objective(Report, 10837, 10838, 0, MissionObjectiveState.Incomplete);
            mission.Transition(Report);
            mission.Conversation(Report, SnakePackage);
            mission.Action(Report, 1, MissionActionKind.GrantReward, reward: 1);
            var at = PoolsWashingtonPlaceV1.At;
            mission.Indicator(Report, 1, at.X, at.Y, at.Z);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1326",
                "Native1 'Report to Snake' 10837/10838, no counters; objectiveconversation 1326,1 package331 text10839. Texts " +
                "10832-10836.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Spoils_of_War",
                "Giver Lieutenant Chester, reward from Colonel 'Snake' Washington; requires Smash and Grab; 32500XP, 3600 credits, " +
                "2 Class VII Advanced Med Pack, 2 Class VII Power Charger, 4 Class VII Cryogenic Grenade, 4 Class VII Incendiary " +
                "Grenade.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PoolsSpoilsOfWarV1",
                "Talk only; level35 (estimated). Items 45062x2, 118907x2, 111048x4, 111038x4.", 0.3);

            mission.Enable();
        }

        public static void Down(MigrationBuilder migration) => PoolsMissionDataV1.Remove(migration, MissionId);
    }
}
