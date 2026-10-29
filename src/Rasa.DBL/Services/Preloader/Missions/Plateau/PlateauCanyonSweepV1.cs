using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// Canyon Sweep (native mission 1045), Plateau: Alpha Squad Commander Carvelle in Martyr's
    /// Canyon asks the player to kill twelve Maligo Elite Guards (PlateauMaligoGuardsV1).
    ///
    /// Native: name/log/offer/turn-in/reminder texts 5875-5878 and 6332 (missionconversation);
    /// objective 1 "Kill 12 Maligo Elite Guards." 5879/6333, counter 5880 "Maligo Elite Guards
    /// killed" (missionobjective). The log names "Commander Carville", the NPC is Carvelle.
    ///
    /// Sourced (TaRapedia Canyon Sweep): giver and receiver Carvelle, level "32 (possibly
    /// less)", 55,000 XP, 5,100 credits, one of four green weapons - Eclipse Electric Injector Gun
    /// 32, Teleract Electric Chaingun 33, Animatics Sonic Cannon 34, Shinobi Sonic Shotgun 32;
    /// requirement "Incommunicado (one objective)".
    ///
    /// Reconstructed: level 30, as Incommunicado; offered once the player delivered Carvelle's
    /// communicator (Incommunicado objective 1 sets flag <see cref="DeliveredFlag"/>), the reading
    /// of "one objective" (ESTIMATE); a guardmaster kill counts too (the guards' commanders, of
    /// the same Elite Guard); the weapons are the Uncommon base templates of the bands that hold
    /// those levels, no module; one indicator over the canyon floor.
    /// </summary>
    public static class PlateauCanyonSweepV1
    {
        public const uint MissionId = 1045;
        public const uint Level = 30;
        public const uint Carvelle = PlateauReconSquadsV1.Carvelle;
        public const uint DeliveredFlag = PlateauIncommunicadoFlagsV1.CarvelleFlag;
        public const uint Kills = 12;

        public const uint Experience = 55000;
        public const uint Credits = 5100;
        public const uint ElectricInjectorGun = 15582;  // Weapon_Avatar_InjectionGun_Electric_UNC_32_to_36
        public const uint ElectricChaingun = 15105;     // Weapon_Avatar_MachineGun_Electric_UNC_33_to_37
        public const uint SonicCannon = 49168;          // Weapon_Avatar_RocketLauncher_Sonic_UNC_34_to_38
        public const uint SonicShotgun = 14933;         // Weapon_Avatar_Shotgun_v1_Sonic_UNC_32_to_36

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PlateauMissionDataV1(migration, MissionId, "Canyon Sweep", 5875, Carvelle, Carvelle, Level);
            mission.Reward(Experience, Credits, new[] { ElectricInjectorGun, ElectricChaingun, SonicCannon, SonicShotgun });
            mission.FlagPrerequisite(DeliveredFlag, 1, "Carvelle's communicator delivered (Incommunicado objective 1)");

            mission.Objective(1, 5879, 6333, 0, MissionObjectiveState.Incomplete, counterTextId: 5880);
            mission.Transition(1);
            mission.Progress(1, MissionProgressEventKind.CreatureKilled, PlateauMaligoGuardsV1.Guard, Kills, counter: 0, trigger: 1);
            mission.Progress(1, MissionProgressEventKind.CreatureKilled, PlateauMaligoGuardsV1.Guardmaster, Kills, counter: 0, trigger: 2);
            mission.Action(1, 1, MissionActionKind.GrantReward, reward: 1);

            mission.Indicator(1, 1, -125, 381, -830, 230);     // the four packs on the canyon floor

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1045",
                "Native1 'Kill 12 Maligo Elite Guards' (5879/6333), counter5880; texts 5875-5878, 6332. Guard name " +
                "creaturenamelanguage8853, guardmaster 8854; no creature row, class or flag for either.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Canyon_Sweep",
                "Giver/receiver Alpha Squad Commander Carvelle; level '32 (possibly less)'; requirement 'Incommunicado (one " +
                "objective)'; 55000XP, 5100 credits; one of green Eclipse Electric Injector Gun32/Teleract Electric Chaingun33/" +
                "Animatics Sonic Cannon34/Shinobi Sonic Shotgun32. Guards: patrols, most south towards the Pools zone line.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PlateauCanyonSweepV1",
                "Level30. Offered after Carvelle's delivery (flag1040001). Guards and guardmasters (750012/750013) both count; " +
                "their level, strength, number, look and places are estimates (PlateauMaligoGuardsV1). Uncommon base weapons, " +
                "no module: 15582, 15105, 49168, 14933. Indicator r230 over the canyon floor.", 0.3);

            mission.Enable(new());
        }

        public static void Down(MigrationBuilder migration) => PlateauMissionDataV1.Remove(migration, MissionId);
    }
}
