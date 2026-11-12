using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Crucible
{
    /// <summary>
    /// Prometheus Rising (native mission 1210), Ligo Crucible: Captain DeLisi sends the player to Major Keplinger, the CO at
    /// Prometheus Outpost, where the Brann battle syndicate helps the AFS against Khulago Base.
    ///
    /// Native: texts 9549-9552, 9553 (missionconversation; 9553 is empty): log "The Brann battle syndicate is assisting an AFS
    /// assault on Khulago Base. Report to Major Keplinger at Prometheus Outpost, northeast of Staal, to provide assistance.",
    /// offer "... Their CO at Prometheus is Major Keplinger. I'll radio and tell him to expect you.", turn-in "Sure, we can use
    /// you! ..."; objective 1 "Report to Keplinger." 9635/9636, no counter (missionobjective); objectiveconversation (1210,1)
    /// package 1337 (Keplinger) 9637 "Are you the trooper Delisi said he was sending over?".
    ///
    /// Sourced: TaRapedia Prometheus Rising: giver Captain DeLisi, reward from Major Keplinger (Prometheus Outpost); requires
    /// Business Proposal; 42,500 XP, 4,000 credits; Class VIII Res Trauma Kit, Class VIII Concussion Grenade x3, Class VIII
    /// Basic Med Pack x6, Frag Grenade x3. The items by their client names (physicalentityclassnamelanguage): 23924 Class VIII
    /// Res Trauma Kit (template 47600), 26269 Class VIII Concussion Grenade (111019), 22546 Class VIII Basic Med Pack (45063),
    /// 22964 Class VIII Fragmentation Grenade (45447; TaRapedia's "Frag Grenade", the client has no other).
    ///
    /// Reconstructed (ESTIMATE):
    ///  - Level 35 (no source: Business Proposal's).
    ///  - Keplinger moved off the control point (CrucibleKardashAndPrometheusNpcsV1). One talk; the four items one to choose,
    ///    in TaRapedia's stacks. A marker at Keplinger.
    /// </summary>
    public static class CruciblePrometheusRisingV1
    {
        public const uint MissionId = 1210;
        public const uint Level = 35;
        public const uint DeLisi = CrucibleOutpostIntrepidNpcsV1.DeLisi;
        public const uint Keplinger = CrucibleKardashAndPrometheusNpcsV1.Keplinger;
        public const uint KeplingerPackage = CrucibleKardashAndPrometheusNpcsV1.KeplingerPackage;
        public const uint Report = 1;
        public const double MarkerRadius = 8;

        public const uint Experience = 42500;
        public const uint Credits = 4000;
        public const uint ResTraumaKit = 47600, ResTraumaKits = 1;              // Class VIII Res Trauma Kit (class 23924)
        public const uint ConcussionGrenade = 111019, ConcussionGrenades = 3;   // Class VIII Concussion Grenade (class 26269)
        public const uint BasicMedPack = 45063, BasicMedPacks = 6;              // Class VIII Basic Med Pack (class 22546)
        public const uint FragGrenade = 45447, FragGrenades = 3;                // Class VIII Fragmentation Grenade (class 22964)

        public static void Up(MigrationBuilder migration)
        {
            var mission = new CrucibleMissionDataV1(migration, MissionId, "Prometheus Rising", 9549, DeLisi, Keplinger, Level);
            mission.Reward(Experience, Credits,
                new[] { (ResTraumaKit, ResTraumaKits), (ConcussionGrenade, ConcussionGrenades), (BasicMedPack, BasicMedPacks), (FragGrenade, FragGrenades) },
                new (uint, uint)[0]);
            mission.Prerequisite(CrucibleBusinessProposalV1.MissionId);

            mission.Objective(Report, 9635, 9636, 0, MissionObjectiveState.Incomplete);
            mission.Transition(Report);
            mission.Conversation(Report, KeplingerPackage);
            mission.Action(Report, 1, MissionActionKind.GrantReward, reward: 1);

            var keplinger = CrucibleKardashAndPrometheusNpcsV1.At(Keplinger);
            mission.Indicator(Report, 1, keplinger.X, keplinger.Y, keplinger.Z, MarkerRadius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1210",
                "Native1 'Report to Keplinger.' 9635/9636, no counter; objectiveconversation 1210,1 package1337 9637. Texts 9549-9552, " +
                "9553 empty. Items 23924/47600, 26269/111019, 22546/45063, 22964/45447 by their client names.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Prometheus_Rising",
                "Giver Captain DeLisi, reward from Major Keplinger (Prometheus Outpost); requires Business Proposal; 42500XP, 4000 " +
                "credits; Class VIII Res Trauma Kit, Concussion Grenade x3, Basic Med Pack x6, Frag Grenade x3.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:CruciblePrometheusRisingV1",
                "Level35 estimated. One talk at Keplinger 510031 (package1337), moved off the control point to 379,168.47,384. Stacks " +
                "47600x1, 111019x3, 45063x6, 45447x3, one to choose. A marker at Keplinger.", 0.3);

            mission.Enable();
        }

        public static void Down(MigrationBuilder migration) => CrucibleMissionDataV1.Remove(migration, MissionId);
    }
}
