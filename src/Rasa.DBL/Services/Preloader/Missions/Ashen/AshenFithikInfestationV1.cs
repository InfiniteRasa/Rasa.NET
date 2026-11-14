using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// Fithik Infestation (native mission 1274), Ligo Ashen Desert: Captain Marcus Emmons on Stonewall Ridge wants ten Fithik
    /// killed: the Bane drop them on the Ridge to sabotage the radar, and the anti-air batteries cannot hit them.
    ///
    /// Native: texts 10240-10244 (missionconversation): log "Captain Emmons of Stonewall Ridge wants you to kill 10 Fithik.
    /// Report back to him once you are finished.", offer "Our anti-air batteries are ineffective against small targets like
    /// Fithik ... After setting up here on Stonewall Ridge ... they're trying to sabotage our radar defense system.", turn-in
    /// "They may be able to avoid our anti-air batteries, but they're no match for a good old fashioned shotgun blast to the
    /// head, eh?", reminder "I've still got bugs crawling up to my eyeballs. ..."; objective 1 "Kill 10 Fithik" 10245/10246,
    /// counter 10247 "Kill Fithik" (missionobjective). No objective conversation. Name 9170 Fithik Guardian (the Ashen
    /// Desert's Fithik, creature 540018, World DB).
    ///
    /// Sourced: TaRapedia Fithik Infestation: giver and reward giver Captain Marcus Emmons (Stonewall Ridge); no requirement;
    /// 90,000 XP, 6,150 credits; one of Modification: Physical Resistance, Threat Reduction, Debuff to Electric Resist, Fire
    /// Resistance. World DB: the Fithik Guardian 540018 (L39) in fifteen Ashen Desert pools, two automatic ones on Stonewall
    /// Ridge (540066, 540067; 540068 is scripted).
    ///
    /// Reconstructed (ESTIMATE):
    ///  - Level 40 (triage D5).
    ///  - The kill: any creature with the Fithik species flag (CreatureFlag.SpeciesFithik 72, TargetsOfOpportunitySeed
    ///    Flag.Fithik; CreatureFlagKilled, counter 0, 10), as "Kill Fithik" names the species, as Junkyard Dogs counts its Flare
    ///    Gashers. A Fithik killed off the Ridge counts too.
    ///  - One marker (r30) on the Ridge's automatic Fithik pool 540067, at the navmesh ground by its centre (the pool row
    ///    stands 4 m under it; the spawn puts the Fithik on the ground). Not on 540066, the Ridge's other automatic pool: its
    ///    Fithik always come down in an isolated navmesh pocket (lowest of three layers at 72.99, -581.11: y 325.3, 330.6,
    ///    336.5) with no path to any waypoint (NOTES-upstream-bugs N-row; the pool row is kept).
    ///  - "Modification" read as the grade [4] modules (as the Crucible's Salvage Duty and Rehabilitation at Gunpoint): Armor
    ///    Module: Resist Physical [4] 123046, Tool Module: Threat Reduction [4] 123141, Weapon Module: Debuff Electric Resist [4]
    ///    123166, Armor Module: Resist Fire [4] 123026 (client itemtemplatelanguage). One to choose.
    /// </summary>
    public static class AshenFithikInfestationV1
    {
        public const uint MissionId = 1274;
        public const uint Level = 40;
        public const uint Emmons = AshenLeadInNpcsV1.Emmons;
        public const uint Kill = 1;
        public const uint Kills = 10;
        public const uint FithikFlag = 72;              // CreatureFlag.SpeciesFithik (TargetsOfOpportunitySeed.Flag.Fithik)
        public const uint Fithik = 540018;              // Fithik Guardian, the Ashen Desert's (World DB)
        public const double MarkerRadius = 30;

        /// <summary>The Stonewall Ridge Fithik pool a player reaches (World DB, automatic) and its marker's place by its centre.</summary>
        public static readonly (uint Pool, (double X, double Y, double Z) At)[] Pools =
        {
            (540067, (234.8, 320.05, -651.2))
        };

        /// <summary>The Ridge's other automatic Fithik pool: its Fithik stand in a navmesh pocket no player reaches; no marker.</summary>
        public const uint PocketPool = 540066;

        public const uint Experience = 90000;
        public const uint Credits = 6150;
        public const uint ResistPhysical = 123046;      // Armor Module: Resist Physical [4]
        public const uint ThreatReduction = 123141;     // Tool Module: Threat Reduction [4]
        public const uint DebuffElectric = 123166;      // Weapon Module: Debuff Electric Resist [4]
        public const uint ResistFire = 123026;          // Armor Module: Resist Fire [4]

        public static void Up(MigrationBuilder migration)
        {
            var mission = new AshenMissionDataV1(migration, MissionId, "Fithik Infestation", 10240, Emmons, Emmons, Level);
            mission.Reward(Experience, Credits, new[] { ResistPhysical, ThreatReduction, DebuffElectric, ResistFire });

            mission.Objective(Kill, 10245, 10246, 0, MissionObjectiveState.Incomplete, counterTextId: 10247);
            mission.Transition(Kill);
            mission.Progress(Kill, MissionProgressEventKind.CreatureFlagKilled, FithikFlag, Kills, counter: 0);
            mission.Action(Kill, 1, MissionActionKind.GrantReward, reward: 1);
            for (var index = 0; index < Pools.Length; index++)
                mission.Indicator(Kill, (uint)index + 1, Pools[index].At.X, Pools[index].At.Y, Pools[index].At.Z, MarkerRadius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1274",
                "Native1 'Kill 10 Fithik' 10245/10246 counter10247 'Kill Fithik'; no objectiveconversation. Texts 10240-10244 " +
                "(Stonewall Ridge, the radar). Name 9300 Emmons; 9170 Fithik Guardian.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Fithik_Infestation",
                "Giver/reward giver Captain Marcus Emmons (Stonewall Ridge); no requirement; 90000XP, 6150 credits; Modification: " +
                "Physical Resistance, Threat Reduction, Debuff to Electric Resist, Fire Resistance.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:AshenFithikInfestationV1",
                "Level40 estimated. CreatureFlagKilled 72 (Fithik), counter 0, 10 (any place). One marker r30 on the Stonewall " +
                "Fithik pool 540067 (540066's Fithik stand in an unreachable navmesh pocket). Modules [4] 123046, 123141, 123166, 123026, one to choose.", 0.3);

            mission.Enable();
        }

        public static void Down(MigrationBuilder migration) => AshenMissionDataV1.Remove(migration, MissionId);
    }
}
