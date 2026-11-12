using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Crucible
{
    /// <summary>
    /// Confirmed Kills (native mission 1212), Ligo Crucible: Larai Zupa Madias at Prometheus Outpost challenges the player to
    /// kill 15 Bane foot soldiers in the trenches of Khulago Base.
    ///
    /// Native: texts 9559-9563 (missionconversation): log "Zupa Madias has given you a challenge. Kill 15 Bane foot soldiers in
    /// the Kahlago Base trenches and you will earn his respect.", offer "... I am challenging you to kill 15 of the Crusty Ones
    /// by your own hands ...", turn-in "Ha ha! You are a brave one, Earther! ...", reminder "Fifteen confirmed kills, Earther.
    /// ..."; objective 1 "Kill 15 Bane Soldiers." 9638/9639 ("... in and around the trenches at Khalago Base."), counter 9640
    /// "Bane Foot Soldiers Killed" (missionobjective). No objective conversation.
    ///
    /// Sourced: TaRapedia Confirmed Kills: giver and reward giver Larai Zupa Madias (Prometheus Outpost); requires Business
    /// Proposal; follow-up Iron Giants; 8,000 credits; Olympia Stealth Armor Legs 6, Vitalius Bio Armor Boots 6, CryoGen
    /// Reflective Armor Legs 4, ChiTech Motor Assist Armor Vest 5; "Bane Foot Soldiers Killed x/15". TR MMO DB: level 35.
    /// World DB: Khulago Base's foot soldiers, Thrax Soldier Mark IX 531018 and Thrax Grenadier 531019, in the trench pools
    /// 531099-531112.
    ///
    /// Reconstructed (ESTIMATE):
    ///  - 170,000 XP (TaRapedia's 17,000 read as a typing error, triage decision 3).
    ///  - The foot soldiers: a kill of 531018 or 531019 (CreatureKilled, one trigger each, counter 0, 15). A kill of these
    ///    rows elsewhere (the Fort Intrepid control point pool, Molitor's pack) counts too: a kill trigger takes no area.
    ///  - Two markers (r35) in the west trench (by pool 531099) and the north trench (by pool 531101), on the navmesh and a
    ///    client floor, walked to from the Prometheus Outpost waypoint with no lava on the walk (the navmesh walk to 531099
    ///    straight from the waypoint crosses lava; by 531101 it does not).
    ///  - The items: the lowest Uncommon template of each kind and icon number in the band of level 35, the nearest icon
    ///    number where the band has none (Stealth V04 Legs 40482, Bio V04 Boots 46004, Reflective V03 Legs 36767, MotorAssist
    ///    V05 Vest 29994), one to choose.
    /// </summary>
    public static class CrucibleConfirmedKillsV1
    {
        public const uint MissionId = 1212;
        public const uint Level = 35;
        public const uint Madias = CrucibleKardashAndPrometheusNpcsV1.Madias;
        public const uint Kill = 1;
        public const uint Kills = 15;
        public const double MarkerRadius = 35;

        /// <summary>Thrax Soldier Mark IX and Thrax Grenadier (World DB, Khulago Base's trench pools).</summary>
        public static readonly uint[] FootSoldiers = { 531018, 531019 };

        /// <summary>The markers: the west trench by pool 531099, the north trench by pool 531101.</summary>
        public static readonly (double X, double Y, double Z)[] TrenchesAt = { (320, 165.28, -128.5), (388, 166.71, -26) };

        public const uint Experience = 170000;
        public const uint Credits = 8000;
        public const uint StealthLegs = 40482;          // Armor_T3_Stealth_V04_UNC_Legs_35_to_39
        public const uint BioBoots = 46004;             // Armor_T3_Bio_V04_UNC_Boots_35_to_39
        public const uint ReflectiveLegs = 36767;       // Armor_T2_Reflective_V03_UNC_Legs_34_to_38
        public const uint MotorAssistVest = 29994;      // Armor_T1_MotorAssist_V05_UNC_Vest_33_to_37

        public static void Up(MigrationBuilder migration)
        {
            var mission = new CrucibleMissionDataV1(migration, MissionId, "Confirmed Kills", 9559, Madias, Madias, Level);
            mission.Reward(Experience, Credits, new[] { StealthLegs, BioBoots, ReflectiveLegs, MotorAssistVest });
            mission.Prerequisite(CrucibleBusinessProposalV1.MissionId);

            mission.Objective(Kill, 9638, 9639, 0, MissionObjectiveState.Incomplete, counterTextId: 9640);
            mission.Transition(Kill);
            for (var i = 0; i < FootSoldiers.Length; i++)
                mission.Progress(Kill, MissionProgressEventKind.CreatureKilled, FootSoldiers[i], Kills, counter: 0, trigger: (uint)i + 1);
            mission.Action(Kill, 1, MissionActionKind.GrantReward, reward: 1);
            for (var i = 0; i < TrenchesAt.Length; i++)
                mission.Indicator(Kill, (uint)i + 1, TrenchesAt[i].X, TrenchesAt[i].Y, TrenchesAt[i].Z, MarkerRadius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1212",
                "Native1 'Kill 15 Bane Soldiers.' 9638/9639 counter9640 'Bane Foot Soldiers Killed'; no objectiveconversation. Texts " +
                "9559-9563 (the Kahlago Base trenches). Name 9205 Larai Zupa Madias.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Confirmed_Kills",
                "Giver/reward giver Larai Zupa Madias (Prometheus Outpost); requires Business Proposal; follow-up Iron Giants; 17000XP " +
                "(typing error), 8000 credits; Olympia Stealth Armor Legs 6, Vitalius Bio Armor Boots 6, CryoGen Reflective Armor " +
                "Legs 4, ChiTech Motor Assist Armor Vest 5; x/15. TR MMO DB level35.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:CrucibleConfirmedKillsV1",
                "170000XP (triage decision 3). CreatureKilled 531018 and 531019, counter 0, 15 (any map place). Madias 510029 moved " +
                "to TaRapedia's loc 378.9,168.3,367.6. Two markers r35 by trench pools 531099 and 531101. Items 40482, 46004, 36767, " +
                "29994, one to choose.", 0.3);

            mission.Enable();
        }

        public static void Down(MigrationBuilder migration) => CrucibleMissionDataV1.Remove(migration, MissionId);
    }
}
