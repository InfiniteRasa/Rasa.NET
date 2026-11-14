using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// The Human Perspective (native mission 1258), Ligo Ashen Desert: Shinichiro Ito in Ashoka Settlement overheard the
    /// player's talk with Latan and sends them to hear Captain Yamamoto at Boneyard Outpost; Yamamoto laughs it off and wants
    /// proof; back at Ashoka, Latan has her answer ready.
    ///
    /// Native: texts 10092-10095, 10096 "none" (missionconversation): log "Travel to the Boneyard Outpost and speak with Captain
    /// Yamamoto. Once you have spoke with Captain Yamamoto, return to Ashoka Settlement and see what Vakiel Latan has to say.",
    /// offer "I overheard your conversation with Latan. ... I'm glad you chose to speak with the captain.", turn-in "So you've
    /// changed your mind? Good, good I'm glad to see you come around." (Latan's); objective 1 "Speak with Captain Yamamoto"
    /// 10097/10098, no counter (missionobjective); objectiveconversation (1258,1) package 1414 (Yamamoto) variant 251 10099
    /// ("Latan did what? She asked you to report me to the Colonel. ..."): the variant of Formal Complaints' choice 2.
    ///
    /// Sourced: TaRapedia The Human Perspective: giver Shinichiro Ito, reward giver Scavenger Vakiel Latan (Ashoka); requires
    /// Formal Complaints (option 2); level 41 or less; 45,000 XP, 4,100 credits; no item named; follow-up A Bribe for Your
    /// Thoughts.
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3):
    ///  - Offered after Formal Complaints is done with flag 251 (its choice 2) set; a player of Latan's side (flag 250, 251 at
    ///    0) never gets it (triage D1).
    ///  - Level 41 (TaRapedia's bound). Rewards: TaRapedia names no item; the stacks of the zone's other 45,000 XP talks
    ///    (Formal Complaints'), one to choose (triage D6, zone median).
    ///  - A marker at Yamamoto.
    /// </summary>
    public static class AshenHumanPerspectiveV1
    {
        public const uint MissionId = 1258;
        public const uint Level = 41;
        public const uint Ito = AshenFormalComplaintsNpcsV1.Ito, Latan = AshenFormalComplaintsNpcsV1.Latan;
        public const uint Yamamoto = AshenLeadInNpcsV1.Yamamoto, YamamotoPackage = AshenLeadInNpcsV1.YamamotoPackage;
        public const uint Speak = 1;
        public const uint Variant = AshenFormalComplaintsV1.YamamotoFlag;
        public const double MarkerRadius = 8;

        public const uint Experience = 45000;
        public const uint Credits = 4100;

        public static void Up(MigrationBuilder migration)
        {
            var mission = new AshenMissionDataV1(migration, MissionId, "The Human Perspective", 10092, Ito, Latan, Level);
            mission.Reward(Experience, Credits,
                new[]
                {
                    (AshenFormalComplaintsV1.EmpGrenade, AshenFormalComplaintsV1.EmpGrenades), (AshenFormalComplaintsV1.ResTraumaKit, AshenFormalComplaintsV1.ResTraumaKits),
                    (AshenFormalComplaintsV1.ConcussionGrenade, AshenFormalComplaintsV1.ConcussionGrenades), (AshenFormalComplaintsV1.BasicMedPack, AshenFormalComplaintsV1.BasicMedPacks)
                },
                new (uint, uint)[0]);
            mission.Prerequisite(AshenFormalComplaintsV1.MissionId, 1);
            mission.FlagPrerequisite(AshenFormalComplaintsV1.YamamotoFlag, AshenFormalComplaintsV1.Chosen,
                "Formal Complaints choice 2, Yamamoto first (flag 251)", 2);

            mission.Objective(Speak, 10097, 10098, 0, MissionObjectiveState.Incomplete);
            mission.Transition(Speak);
            mission.Conversation(Speak, YamamotoPackage, Variant);
            mission.Action(Speak, 1, MissionActionKind.GrantReward, reward: 1);

            var yamamoto = AshenLeadInNpcsV1.At(Yamamoto);
            mission.Indicator(Speak, 1, yamamoto.X, yamamoto.Y, yamamoto.Z, MarkerRadius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1258",
                "Native1 'Speak with Captain Yamamoto' 10097/10098, no counter; objectiveconversation 1258,1 pkg1414 variant251 " +
                "10099. Texts 10092-10095 (the turn-in is Latan's), 10096 'none'. Name 9272 Ito.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/The_Human_Perspective",
                "Giver Shinichiro Ito, reward giver Scavenger Vakiel Latan (Ashoka); requires Formal Complaints (option 2); level " +
                "41 or less; 45000XP, 4100 credits; no item named; follow-up A Bribe for Your Thoughts.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:AshenHumanPerspectiveV1",
                "Ito 790201 new (lower Ashoka). Offered after 1254 with flag251 set. Level41 (TaRapedia bound). Stacks of 1254 " +
                "(111029x3, 47600x1, 111019x3, 45063x6), one to choose (zone median). A marker at Yamamoto.", 0.3);

            mission.Enable();
        }

        public static void Down(MigrationBuilder migration) => AshenMissionDataV1.Remove(migration, MissionId);
    }
}
