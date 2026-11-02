using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// A Butcher, a Baker, and a Caretaker (native mission 968), Valverde Marshes: Ranger Taavik at Paludos wants the three
    /// Bane leaders who order the assaults on Paludos' eastern flank dead.
    ///
    /// Native: texts 5285-5288 and 9373 (missionconversation): log "Locate Caretaker Fik, Commander Sto, and Commander Barzak
    /// and assassinate them. Report back to Ranger Taavik in Paludos.", offer "Fik, Sto, and Barzak. Those are the 'leaders'
    /// who organize the main assaults on the eastern flanks of Paludos ... they just bark orders from the safety of their
    /// bunkers along the Long Road.", turn-in "Ahhhh, their deaths bring a smile to my face"; objectives 1 "Assassinate
    /// Caretaker Fik" 5289/9374 counter 5290 "Caretaker Fik:", 2 "Assassinate Commander Sto" 5291/9375 counter 5292, 3
    /// "Assassinate Commander Barzak" 5293/9376 counter 5294 (missionobjective); no objective conversation. Names 8641 Ranger
    /// Taavik, 8756 Caretaker Fik, 8758 Commander Sto, 8757 Commander Barzak.
    ///
    /// Sourced: TaRapedia A Butcher, a Baker, and a Caretaker: giver and receiver Ranger Taavik (Paludos); requires Reaping
    /// the Harvest; level 36; 160,000 XP, 7,800 credits; one of Vitalius Electric Blade lvl 36, Phoenix Conical Healing Disc
    /// lvl 40, ChiTech Shotgun lvl 37, Vextronics Incendiary Staff lvl 40 (an old revision; the current page names none).
    /// TR MMO DB: level 33. World DB: Taavik 510164 (pool at -322, 218, -2, an upstream placeholder spot); the bosses Caretaker
    /// Fik 520028 (pool 520028 at 459, 218, 11), Commander Barzak 520029 (490, 218, 413), Commander Sto 520030 (362, 218,
    /// -168), all level 38, respawn 500 s.
    ///
    /// Reconstructed (ESTIMATE): the kill of each public boss counts (anyone's kill credit, as the server gives it, Pools 941
    /// precedent), any order; their pools stay where upstream has them (Ellatha's map-view values, upstream data bug 8 in the
    /// triage). Level TaRapedia's. A marker on each boss's pool. The items the lowest Uncommon templates of the kinds and
    /// bands that hold those levels ("Conical" disc the Direct one: the Pools read "Radial" as Area; "Incendiary" Fire; the
    /// shotgun names no element, so the lowest of the band), one to choose.
    /// </summary>
    public static class MarshesButcherBakerCaretakerV1
    {
        public const uint MissionId = 968;
        public const uint Level = 36;
        public const uint Taavik = 510164;
        public const uint KillFik = 1, KillSto = 2, KillBarzak = 3;
        public const uint Fik = 520028, Barzak = 520029, Sto = 520030;
        public const double BossRadius = 15;

        /// <summary>The bosses: objective, name, body, counter, the boss and its pool's place (World DB).</summary>
        public static readonly (uint Objective, uint Name, uint Body, uint Counter, uint Boss, double X, double Y, double Z)[] Bosses =
        {
            (KillFik, 5289, 9374, 5290, Fik, 459, 218.3, 11),
            (KillSto, 5291, 9375, 5292, Sto, 362, 217.6, -168),
            (KillBarzak, 5293, 9376, 5294, Barzak, 490, 218.2, 413),
        };

        public const uint Experience = 160000;
        public const uint Credits = 7800;
        public const uint ElectricBlade = 15475;        // Weapon_Avatar_Blade_Electric_UNC_36_to_40
        public const uint HealingDisc = 18367;          // Tool_Avatar_Healing_Disc_Direct_UNC_40_to_44
        public const uint Shotgun = 14820;              // Weapon_Avatar_Shotgun_v1_Physical_UNC_37_to_41
        public const uint IncendiaryStaff = 49496;      // Weapon_Avatar_Staff_Fire_UNC_40_to_44

        public static void Up(MigrationBuilder migration)
        {
            var mission = new MarshesMissionDataV1(migration, MissionId, "A Butcher, a Baker, and a Caretaker", 5285, Taavik, Taavik, Level);
            mission.Reward(Experience, Credits, new[] { ElectricBlade, HealingDisc, Shotgun, IncendiaryStaff });
            mission.Prerequisite(MarshesReapingTheHarvestV1.MissionId);

            foreach (var (objective, name, body, counter, boss, x, y, z) in Bosses)
            {
                mission.Objective(objective, name, body, objective - 1, MissionObjectiveState.Incomplete, counterTextId: counter);
                mission.Transition(objective);
                mission.Progress(objective, MissionProgressEventKind.CreatureKilled, boss, 1, counter: 0);
                mission.Indicator(objective, objective, x, y, z, BossRadius);
            }
            mission.Action(KillFik, 1, MissionActionKind.GrantReward, reward: 1);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/968",
                "Native1 'Assassinate Caretaker Fik' 5289/9374 counter5290, 2 'Assassinate Commander Sto' 5291/9375 counter5292, " +
                "3 'Assassinate Commander Barzak' 5293/9376 counter5294; no objectiveconversation. Texts 5285-5288, 9373. Names " +
                "8641 Ranger Taavik, 8756 Caretaker Fik, 8758 Commander Sto, 8757 Commander Barzak.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/A_Butcher%2C_a_Baker%2C_and_a_Caretaker",
                "Giver/receiver Ranger Taavik (Paludos); requires Reaping the Harvest; level36; 160000XP, 7800 credits; one of " +
                "Vitalius Electric Blade36, Phoenix Conical Healing Disc40, ChiTech Shotgun37, Vextronics Incendiary Staff40 (old " +
                "revision). TR MMO DB level33; Barzak and Sto named mobs.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:MarshesButcherBakerCaretakerV1",
                "The public bosses 520028, 520030, 520029 (upstream pools and places) count, any order. Level TaRapedia's. Items " +
                "15475, 18367, 14820, 49496 (lowest Uncommon of each kind and band), one to choose. A marker on each pool.", 0.3);

            mission.Enable();
        }

        public static void Down(MigrationBuilder migration) => MarshesMissionDataV1.Remove(migration, MissionId);
    }
}
