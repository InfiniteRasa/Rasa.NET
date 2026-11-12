using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Crucible
{
    /// <summary>
    /// Business Proposal (native mission 1229), Ligo Crucible: Kappa Grupa Lohen gives the player his "business proposal"
    /// for an unofficial alliance with the AFS, for Captain DeLisi at Outpost Intrepid.
    ///
    /// Native: texts 9768-9771, 9772 (missionconversation; 9772 is empty): log "deliver Lohen's "business proposal" to
    /// Captain Delisi at Outpost Intrepid.", offer "I am giving to you a business proposal ... Take it back to your
    /// Captain.", turn-in "Holy crap. This thing's thicker than "War and Peace"!"; objective 1 "Deliver the Proposal to
    /// DeLisi." 9778/9779, no counter (missionobjective); objectiveconversation (1229,1) package 1219 (DeLisi) 9780, variant
    /// 1. Item class 25305 Kappa Grupa Lohen's Business Proposal (template 73215).
    ///
    /// Sourced: TaRapedia Business Proposal: giver Kappa Grupa Lohen (Staal Detention Center), reward from Captain DeLisi;
    /// requires Earning Respect; follow-ups Prometheus Rising (1210), Confirmed Kills (1212) and Careful Negotiation (1519)
    /// (their pages); 40,000 XP, 3,900 credits; Class VII EMP Grenade x3, Class VII Res Trauma Kit, Class VII Concussion
    /// Grenade x3, Class VII Basic Med Pack x6. The items by their client names (physicalentityclassnamelanguage): 26278
    /// Class VII EMP Grenade (template 111028), 23923 Class VII Res Trauma Kit (47599), 26268 Class VII Concussion Grenade
    /// (111018), 22543 Class VII Basic Med Pack (45060).
    ///
    /// Reconstructed (ESTIMATE):
    ///  - Level 35 (no source: the level of Earning Respect).
    ///  - The proposal at the accept; DeLisi takes it at the talk. The four items one to choose, in TaRapedia's stacks. A
    ///    marker at DeLisi.
    /// </summary>
    public static class CrucibleBusinessProposalV1
    {
        public const uint MissionId = 1229;
        public const uint Level = 35;
        public const uint Lohen = CrucibleGrupaChiefsNpcsV1.Lohen;
        public const uint DeLisi = CrucibleOutpostIntrepidNpcsV1.DeLisi;
        public const uint DeLisiPackage = CrucibleOutpostIntrepidNpcsV1.DeLisiPackage;
        public const uint Deliver = 1;

        public const uint ProposalTemplate = 73215, ProposalClass = 25305;
        public const string ProposalKey = "lohens-business-proposal";
        public const double MarkerRadius = 8;

        public const uint Experience = 40000;
        public const uint Credits = 3900;
        public const uint EmpGrenade = 111028, EmpGrenades = 3;                 // Class VII EMP Grenade (class 26278)
        public const uint ResTraumaKit = 47599, ResTraumaKits = 1;              // Class VII Res Trauma Kit (class 23923)
        public const uint ConcussionGrenade = 111018, ConcussionGrenades = 3;   // Class VII Concussion Grenade (class 26268)
        public const uint BasicMedPack = 45060, BasicMedPacks = 6;              // Class VII Basic Med Pack (class 22543)

        public static void Up(MigrationBuilder migration)
        {
            var mission = new CrucibleMissionDataV1(migration, MissionId, "Business Proposal", 9768, Lohen, DeLisi, Level);
            mission.Reward(Experience, Credits,
                new[] { (EmpGrenade, EmpGrenades), (ResTraumaKit, ResTraumaKits), (ConcussionGrenade, ConcussionGrenades), (BasicMedPack, BasicMedPacks) },
                new (uint, uint)[0]);
            mission.Prerequisite(CrucibleEarningRespectV1.MissionId);

            mission.Objective(Deliver, 9778, 9779, 0, MissionObjectiveState.Incomplete);
            mission.Transition(Deliver);
            mission.Conversation(Deliver, DeLisiPackage);
            mission.Action(Deliver, 1, MissionActionKind.ConsumeMissionItem,
                itemIntent: new ConsumeMissionItemIntent("delisi-takes-the-proposal", MissionId, ProposalKey, 1, MissionItemScope.AssignmentIssued));
            mission.Action(Deliver, 2, MissionActionKind.GrantReward, reward: 1);

            var delisi = CrucibleOutpostIntrepidNpcsV1.At(DeLisi);
            mission.Indicator(Deliver, 1, delisi.X, delisi.Y, delisi.Z, MarkerRadius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1229",
                "Native1 'Deliver the Proposal to DeLisi.' 9778/9779, no counter; objectiveconversation 1229,1 package1219 9780. " +
                "Texts 9768-9771, 9772 empty. Item class25305 Kappa Grupa Lohen's Business Proposal (template73215). Items " +
                "26278/111028, 23923/47599, 26268/111018, 22543/45060 by their client names.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Business_Proposal",
                "Giver Kappa Grupa Lohen (Staal), reward from Captain DeLisi; requires Earning Respect; follow-ups 1210, 1212, " +
                "1519; 40000XP, 3900 credits; Class VII EMP Grenade x3, Res Trauma Kit, Concussion Grenade x3, Basic Med Pack x6.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:CrucibleBusinessProposalV1",
                "Level35 estimated. The proposal issued at the accept, taken at DeLisi's talk (package1219). Stacks 111028x3, " +
                "47599x1, 111018x3, 45060x6, one to choose. A marker at DeLisi.", 0.3);

            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                MapContextId = CrucibleMissionDataV1.MapContextId,
                Items = new()
                {
                    new MissionItemBinding(ProposalKey, ProposalTemplate, MissionItemScope.AssignmentIssued, 1,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove)
                },
                AcceptanceItems = new() { new IssueMissionItemIntent("issue-business-proposal", MissionId, ProposalKey, ProposalTemplate, 1) }
            };
            scene.Sequences[0] = new SceneSequenceDefinition();

            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => CrucibleMissionDataV1.Remove(migration, MissionId);
    }
}
