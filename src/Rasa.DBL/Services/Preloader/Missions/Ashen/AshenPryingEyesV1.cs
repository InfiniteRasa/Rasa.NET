using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// Prying Eyes (native mission 1303), Ligo Ashen Desert: Captain Daris Linier at White Oasis Post wants the Bane technician
    /// who runs his own diagnostic on Maji Field's Water Harvesters dead.
    ///
    /// Native: texts 10533-10536, 6 = 10537 (missionconversation): log "Find Overseer Ulovik in Maji Field, who appears to be
    /// analyzing the processing units of the four Water Harvesters. Report back to Captain Daris Linier at White Oasis Outpost
    /// once he is dead.", offer "The Bane sent their own big nasty technician out to Maji Field to run their own diagnostic.
    /// ...", turn-in "Great job soldier. You definitely aced that technician in a hurry. I can see why my father has a high
    /// opinion of you. ...", reminder "There's a big ugly Thrax Technician out there ..."; objective 1 "Kill Overseer Ulovik"
    /// 10538/10539, no counter (missionobjective). No objective conversation. Name 9363 Overseer Ulovik (World DB creature
    /// 520006, L44, 4900 HP, its own pool 520006 of one, respawn 50 s).
    ///
    /// Sourced: TaRapedia Prying Eyes: report to Captain Daris Linier at White Oasis Outpost; kill Overseer Ulovik; the infobox
    /// is empty (no giver, requirement, XP, credits, item). TaRapedia Overseer Ulovik: "Boss Thrax Technician", "Near the first
    /// harvester strip in the southern part of Maji Field. /loc -235 280"; Ellatha -235, 335 = the World DB pool (used).
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3):
    ///  - Giver and receiver Daris Linier (the client's log and turn-in, "my father": Colonel Linier's son). No prerequisite
    ///    (none named). Level 41 (D5, the White Oasis missions).
    ///  - The kill: Ulovik's creature (CreatureKilled 520006, once), as Chemical Oversight's Exing (the same L44 boss row against
    ///    a level 41 mission). His pool stays where upstream has it: on the client's floor, seen from 16 of 16 places 2 m round
    ///    him, walked to from the White Oasis Post waypoint and back on the client's floor, 46 m from the nearest other pool
    ///    (Fithik 540075's circle), the walk 39 m or more outside every hostile pool's circle (probe of 2026-11-10). A marker
    ///    (r15) on him.
    ///  - Rewards (D6: no source): 95,000 XP and 6,300 credits (the zone median, as Robotic Salvage Acquisitions) and the stacks of
    ///    the zone's talks (Formal Complaints'), one to choose.
    /// </summary>
    public static class AshenPryingEyesV1
    {
        public const uint MissionId = 1303;
        public const uint Level = 41;
        public const uint DarisLinier = AshenWhiteOasisNpcsV1.DarisLinier;
        public const uint Kill = 1;
        public const uint Ulovik = 520006;              // creature and pool, upstream (name 9363)
        public const double MarkerRadius = 15;
        public static readonly (double X, double Y, double Z) UlovikAt = (-235, 291.20001220703125, 335);

        public const uint Experience = 95000;
        public const uint Credits = 6300;
        public const uint EmpGrenade = AshenFormalComplaintsV1.EmpGrenade, EmpGrenades = AshenFormalComplaintsV1.EmpGrenades;
        public const uint ResTraumaKit = AshenFormalComplaintsV1.ResTraumaKit, ResTraumaKits = AshenFormalComplaintsV1.ResTraumaKits;
        public const uint ConcussionGrenade = AshenFormalComplaintsV1.ConcussionGrenade, ConcussionGrenades = AshenFormalComplaintsV1.ConcussionGrenades;
        public const uint BasicMedPack = AshenFormalComplaintsV1.BasicMedPack, BasicMedPacks = AshenFormalComplaintsV1.BasicMedPacks;

        public static void Up(MigrationBuilder migration)
        {
            var mission = new AshenMissionDataV1(migration, MissionId, "Prying Eyes", 10533, DarisLinier, DarisLinier, Level);
            mission.Reward(Experience, Credits,
                new[] { (EmpGrenade, EmpGrenades), (ResTraumaKit, ResTraumaKits), (ConcussionGrenade, ConcussionGrenades), (BasicMedPack, BasicMedPacks) },
                new (uint, uint)[0]);

            mission.Objective(Kill, 10538, 10539, 0, MissionObjectiveState.Incomplete);
            mission.Transition(Kill);
            mission.Progress(Kill, MissionProgressEventKind.CreatureKilled, Ulovik, null);
            mission.Action(Kill, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Indicator(Kill, 1, UlovikAt.X, UlovikAt.Y, UlovikAt.Z, MarkerRadius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1303",
                "Native1 'Kill Overseer Ulovik' 10538/10539, no counter; no objectiveconversation. Texts 10533-10537 (a Bane " +
                "technician in Maji Field; report to Captain Daris Linier at White Oasis Outpost). Name 9363 Ulovik.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Prying_Eyes",
                "Report to Captain Daris Linier at White Oasis Outpost; kill Overseer Ulovik; empty infobox. Overseer Ulovik: boss " +
                "Thrax Technician near the first harvester strip, /loc -235 280 (Ellatha -235, 335 = World DB pool 520006).");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:AshenPryingEyesV1",
                "Giver and receiver Daris Linier 510007; no prerequisite; level41 estimated. CreatureKilled 520006 once; his pool " +
                "kept (walked to from the White Oasis waypoint); a marker r15 on him. 95000XP, 6300 credits (zone median); stacks " +
                "111029x3, 47600x1, 111019x3, 45063x6, one to choose.", 0.3);

            mission.Enable();
        }

        public static void Down(MigrationBuilder migration) => AshenMissionDataV1.Remove(migration, MissionId);
    }
}
