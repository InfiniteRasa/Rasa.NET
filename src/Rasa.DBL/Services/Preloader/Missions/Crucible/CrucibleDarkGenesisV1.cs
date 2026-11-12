using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Crucible
{
    /// <summary>
    /// Dark Genesis (native mission 1856), Ligo Crucible, the first of the Thrax hybrid arc: Special Agent Stark at Outpost
    /// Intrepid sends the player to Special Agent Capriulo in Staal, who sends him on to Zupa Eugin.
    ///
    /// Native: texts 18911-18915 (missionconversation); objective 1 "Locate Special Agent Capriulo." 19042/19043, 2 "Speak to
    /// Zupa Eugin." 19184/19185, no counters (missionobjective); objective conversations (1856,1) package 2335 (Capriulo) text
    /// 19044, (1856,2) package 2337 (Eugin) text 19186.
    ///
    /// Sourced: TaRapedia Dark Genesis: giver Special Agent Stark (Outpost Intrepid, 1020, 145, 64), reward from Viddea
    /// Scientist Eugin (Staal); requires level 36; follow-up The Gene Machine; 37,500 XP, 3,800 credits; Class VII Advanced Med
    /// Pack x2, Power Charger x2, Cryogenic Grenade x4 or Incendiary Grenade x4 ("choice of consumable", TTH; the stack sizes
    /// are TaRapedia's, NOTES-crucible-research-client.md 745-746). TR MMO DB level 36. "Thrax
    /// Hybrid Story Arc: Begins in Crucible Wormhole" (FF-080201, 0.5).
    ///
    /// Reconstructed (ESTIMATE):
    ///  - The consumables' rows: Consumable_Medpack_07C_35 45062, Consumable_Restore_Power_Kit_35_to_39 118907,
    ///    Consumable_Grenade_Ice_07_35 111048, Consumable_Grenade_Fire_07_35 111038, one to choose (the band of level 35, the
    ///    client's own class VII and 07 rows; the stack sizes are TaRapedia's, above).
    ///  - Capriulo's talk opens 2; Eugin's completes it. A marker at each NPC.
    /// </summary>
    public static class CrucibleDarkGenesisV1
    {
        public const uint MissionId = 1856;
        public const uint Level = 36;
        public const uint Stark = CrucibleHybridArcNpcsV1.Stark;
        public const uint Eugin = CrucibleHybridArcNpcsV1.Eugin;
        public const uint LocateCapriulo = 1, SpeakToEugin = 2;
        public const double MarkerRadius = 8;

        public const uint Experience = 37500;
        public const uint Credits = 3800;
        public const uint MedPack = 45062;              // Consumable_Medpack_07C_35
        public const uint PowerCharger = 118907;        // Consumable_Restore_Power_Kit_35_to_39
        public const uint CryogenicGrenade = 111048;    // Consumable_Grenade_Ice_07_35
        public const uint IncendiaryGrenade = 111038;   // Consumable_Grenade_Fire_07_35
        public const uint MedPacks = 2, PowerChargers = 2, Grenades = 4;

        public static void Up(MigrationBuilder migration)
        {
            var mission = new CrucibleMissionDataV1(migration, MissionId, "Dark Genesis", 18911, Stark, Eugin, Level);
            mission.Reward(Experience, Credits,
                new[] { (MedPack, MedPacks), (PowerCharger, PowerChargers), (CryogenicGrenade, Grenades), (IncendiaryGrenade, Grenades) },
                new (uint, uint)[0]);
            mission.LevelPrerequisite(Level);

            mission.Objective(LocateCapriulo, 19042, 19043, 0, MissionObjectiveState.Incomplete);
            mission.Objective(SpeakToEugin, 19184, 19185, 1, MissionObjectiveState.Inactive);

            mission.Transition(LocateCapriulo);
            mission.Conversation(LocateCapriulo, CrucibleHybridArcNpcsV1.CapriuloPackage);
            CrucibleCalendarGirlV1.Open(mission, LocateCapriulo, 1, 1, SpeakToEugin);

            mission.Transition(SpeakToEugin);
            mission.Conversation(SpeakToEugin, CrucibleHybridArcNpcsV1.EuginPackage);
            mission.Action(SpeakToEugin, 1, MissionActionKind.GrantReward, reward: 1);

            var capriulo = CrucibleHybridArcNpcsV1.CapriuloAt;
            var eugin = CrucibleHybridArcNpcsV1.EuginAt;
            mission.Indicator(LocateCapriulo, 1, capriulo.X, capriulo.Y, capriulo.Z, MarkerRadius);
            mission.Indicator(SpeakToEugin, 2, eugin.X, eugin.Y, eugin.Z, MarkerRadius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1856",
                "Native1 'Locate Special Agent Capriulo.' 19042/19043, 2 'Speak to Zupa Eugin.' 19184/19185, no counters; " +
                "objectiveconversation 1856,1 package2335 text19044, 1856,2 package2337 text19186. Texts 18911-18915. Names 10300 " +
                "Special Agent Stark, 10301 Special Agent Capriulo, 10303 Viddea Scientist Eugin.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Dark_Genesis",
                "Giver Special Agent Stark (Outpost Intrepid, 1020, 145, 64), reward from Viddea Scientist Eugin (Staal, -232, 196, " +
                "473); Capriulo (-304, 197, 379); requires level 36; follow-up The Gene Machine; 37500XP, 3800 credits; Class VII " +
                "Advanced Med Pack x2, Power Charger x2, Cryogenic Grenade x4, Incendiary Grenade x4. TR MMO DB level36.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:CrucibleDarkGenesisV1",
                "Stark 770060 a new row; Capriulo and Eugin moved to TaRapedia's places. Capriulo's talk opens 2, Eugin's completes. " +
                "Item rows 45062, 118907, 111048, 111038 (stacks from TaRapedia), one to choose. A marker at each NPC.", 0.3);

            mission.Enable();
        }

        public static void Down(MigrationBuilder migration) => CrucibleMissionDataV1.Remove(migration, MissionId);
    }
}
