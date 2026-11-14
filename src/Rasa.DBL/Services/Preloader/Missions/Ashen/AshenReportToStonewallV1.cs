using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// Report to Stonewall Ridge (native mission 1273), Ligo Ashen Desert: Captain Flynn Harris at the Ashoka Settlement sends
    /// the player up to Captain Marcus Emmons on Stonewall Ridge.
    ///
    /// Native: texts 10232-10235, 10236 "none" (missionconversation): log "Report to Captain Marcus Emmons on Stonewall
    /// Ridge.", offer "Captain Emmons up on Stonewall Ridge just sent out a call for assistance. ... the Bane have started
    /// dropping Fithik on the Ridge ...", turn-in "Great to meet you! I'm glad you answered the call."; objective 1 "Report to
    /// Captain Emmons" 10237/10238, no counter (missionobjective); objectiveconversation (1273,1) package 1411 (Emmons) text
    /// 10239 "Captain Harris told you about the situation?", variant 1. Names 9263 Captain Flynn Harris, 9300 Captain Marcus
    /// Emmons.
    ///
    /// Sourced: TaRapedia Report to Stonewall Ridge: giver Captain Flynn Harris (Ashoka Settlement), reward giver Captain
    /// Marcus Emmons (Stonewall Ridge); no requirement; 45,000 XP, 4,100 credits; one of Class VIII Incendiary Grenade x3,
    /// Advanced Med Pack x2, EMP Grenade x3, Res Trauma Kit. TR MMO DB: the same XP and credits (level 30: MMODB default).
    /// Items by their client names: 26289 Class VIII Incendiary Grenade (template 111039), 22548 Advanced Med Pack (45065),
    /// 26279 EMP Grenade (111029), 23924 Res Trauma Kit (47600).
    ///
    /// Reconstructed (ESTIMATE): level 40 (triage D5); Harris moved 8 m off the Ashoka waypoint (AshenLeadInNpcsV1; upstream had
    /// him 4 m from it, under 2 m from the navmesh edge by the crates); the four stacks one to choose; a marker at Emmons.
    /// </summary>
    public static class AshenReportToStonewallV1
    {
        public const uint MissionId = 1273;
        public const uint Level = 40;
        public const uint Harris = AshenLeadInNpcsV1.Harris;
        public const uint Emmons = AshenLeadInNpcsV1.Emmons;
        public const uint EmmonsPackage = AshenLeadInNpcsV1.EmmonsPackage;
        public const uint Report = 1;
        public const double MarkerRadius = 8;

        public const uint Experience = 45000;
        public const uint Credits = 4100;
        public const uint IncendiaryGrenade = 111039, IncendiaryGrenades = 3;   // Class VIII Incendiary Grenade (class 26289)
        public const uint AdvancedMedPack = 45065, AdvancedMedPacks = 2;        // Class VIII Advanced Med Pack (class 22548)
        public const uint EmpGrenade = 111029, EmpGrenades = 3;                 // Class VIII EMP Grenade (class 26279)
        public const uint ResTraumaKit = 47600, ResTraumaKits = 1;              // Class VIII Res Trauma Kit (class 23924)

        public static void Up(MigrationBuilder migration)
        {
            var mission = new AshenMissionDataV1(migration, MissionId, "Report to Stonewall Ridge", 10232, Harris, Emmons, Level);
            mission.Reward(Experience, Credits,
                new[] { (IncendiaryGrenade, IncendiaryGrenades), (AdvancedMedPack, AdvancedMedPacks), (EmpGrenade, EmpGrenades), (ResTraumaKit, ResTraumaKits) },
                new (uint, uint)[0]);

            mission.Objective(Report, 10237, 10238, 0, MissionObjectiveState.Incomplete);
            mission.Transition(Report);
            mission.Conversation(Report, EmmonsPackage);
            mission.Action(Report, 1, MissionActionKind.GrantReward, reward: 1);

            var emmons = AshenLeadInNpcsV1.At(Emmons);
            mission.Indicator(Report, 1, emmons.X, emmons.Y, emmons.Z, MarkerRadius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1273",
                "Native1 'Report to Captain Emmons' 10237/10238, no counter; objectiveconversation 1273,1 package1411 text10239. " +
                "Texts 10232-10235, 10236 'none'. Names 9263 Harris, 9300 Emmons. Items 26289/111039, 22548/45065, 26279/111029, " +
                "23924/47600 by their client names.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Report_to_Stonewall_Ridge",
                "Giver Captain Flynn Harris (Ashoka Settlement), reward giver Captain Marcus Emmons (Stonewall Ridge); no requirement; " +
                "45000XP, 4100 credits; Class VIII Incendiary Grenade x3, Advanced Med Pack x2, EMP Grenade x3, Res Trauma Kit. TR MMO " +
                "DB the same XP and credits.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:AshenReportToStonewallV1",
                "Level40 estimated. Harris 510008 moved 8 m off the Ashoka waypoint; Emmons 510009 moved to the Stonewall Command Tower. Stacks " +
                "111039x3, 45065x2, 111029x3, 47600x1, one to choose. A marker at Emmons. NPC facing rotation0 estimated.", 0.3);

            mission.Enable();
        }

        public static void Down(MigrationBuilder migration) => AshenMissionDataV1.Remove(migration, MissionId);
    }
}
