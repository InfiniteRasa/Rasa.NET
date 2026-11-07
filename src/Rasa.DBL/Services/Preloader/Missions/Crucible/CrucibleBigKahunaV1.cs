using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Crucible
{
    /// <summary>
    /// The Big Kahuna (native mission 1216), Ligo Crucible: Larai Zupa Madias sends the player to kill Maxxum Ugoretz, the
    /// commander of Khulago Base, whose name the player found in Who's In Charge Here? (1215).
    ///
    /// Native: texts 9579-9583 (missionconversation): log "Zupa Madias at Prometheus Outpost wants you to kill Maxxum Ugoretz,
    /// supreme leader of the Bane troops at Kahlago Base.", offer "So, the big boss of Khalago Base is the one called Maxxum
    /// Ugoretz? ...", turn-in "You have killed the great Maxxum Ugoretz! ...", reminder "The one called Ugoretz. He must die!";
    /// objective 1 "Kill Maxxum Ugoretz." 9803/9804, no counter (missionobjective). No objective conversation.
    ///
    /// Sourced: TaRapedia The Big Kahuna: giver and reward giver Larai Zupa Madias; requires Who's In Charge Here?; 85,000 XP,
    /// 2,000 credits (TR MMO DB: 0 credits); one of Vextronics Incendiary Blade, Astra Radial Healing Disc, Vextronics Leech
    /// Gun, Vextronics Laser Chaingun; walkthrough "patrolling the northwest area of Kahlugo Base". TaRapedia Maxxum Ugoretz:
    /// "roaming in front of and to the west of Khulago Base, /loc approx 340 x -130". TR MMO DB: level 35.
    ///
    /// Reconstructed (ESTIMATE):
    ///  - Ugoretz the new row 770042 (CruciblePrometheusOutpostNpcsV1), alone in a point pool at TaRapedia's /loc, standing
    ///    (his patrol is not built); a kill of him completes 1 (CreatureKilled, no counter, as Funeral Derge). A marker (r15) on him.
    ///  - 2,000 credits (TaRapedia over TR MMO DB's 0).
    ///  - The items: the lowest template id of each kind whose Uncommon band holds level 35 (Blade_Fire 47628,
    ///    Tool_Avatar_Healing_Disc_Area_UNC 120030, LeechGun_Physical 47808, MachineGun_Laser 48256), one to choose; the Leech
    ///    Gun's name gives no damage kind: physical.
    /// </summary>
    public static class CrucibleBigKahunaV1
    {
        public const uint MissionId = 1216;
        public const uint Level = 35;
        public const uint Madias = CrucibleKardashAndPrometheusNpcsV1.Madias;
        public const uint Kill = 1;
        public const double MarkerRadius = 15;

        public const uint Experience = 85000;
        public const uint Credits = 2000;
        public const uint IncendiaryBlade = 47628;      // Blade_Fire
        public const uint RadialHealingDisc = 120030;   // Tool_Avatar_Healing_Disc_Area_UNC
        public const uint LeechGun = 47808;             // LeechGun_Physical
        public const uint LaserChaingun = 48256;        // MachineGun_Laser

        public static void Up(MigrationBuilder migration)
        {
            var mission = new CrucibleMissionDataV1(migration, MissionId, "The Big Kahuna", 9579, Madias, Madias, Level);
            mission.Reward(Experience, Credits, new[] { IncendiaryBlade, RadialHealingDisc, LeechGun, LaserChaingun });
            mission.Prerequisite(CrucibleWhosInChargeHereV1.MissionId);

            mission.Objective(Kill, 9803, 9804, 0, MissionObjectiveState.Incomplete);
            mission.Transition(Kill);
            mission.Progress(Kill, MissionProgressEventKind.CreatureKilled, CruciblePrometheusOutpostNpcsV1.Ugoretz, null);
            mission.Action(Kill, 1, MissionActionKind.GrantReward, reward: 1);
            var at = CruciblePrometheusOutpostNpcsV1.UgoretzAt;
            mission.Indicator(Kill, 1, at.X, at.Y, at.Z, MarkerRadius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1216",
                "Native1 'Kill Maxxum Ugoretz.' 9803/9804, no counter; no objectiveconversation. Texts 9579-9583. Name 9317 Maxxum " +
                "Ugoretz.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/The_Big_Kahuna",
                "Giver/reward giver Larai Zupa Madias; requires Who's In Charge Here?; 85000XP, 2000 credits (TR MMO DB 0); one of " +
                "Vextronics Incendiary Blade, Astra Radial Healing Disc, Vextronics Leech Gun, Vextronics Laser Chaingun; patrolling " +
                "the northwest area of Kahlugo Base. TaRapedia Maxxum Ugoretz: /loc approx 340 x -130. TR MMO DB level35.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:CrucibleBigKahunaV1",
                "CreatureKilled 770042 (Ugoretz, a copy of Derge 520013, level 43), no counter; alone in a point pool at (340,165.12,-130) (no navmesh lava within 80 m), " +
                "standing (no patrol). A marker r15 on him. Items 47628, 120030, 47808, 48256 (lowest template of the kind with an " +
                "Uncommon band at 35), one to choose.", 0.3);

            mission.Enable();
        }

        public static void Down(MigrationBuilder migration) => CrucibleMissionDataV1.Remove(migration, MissionId);
    }
}
