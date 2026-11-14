using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// A Bribe for Your Thoughts (native mission 1259), Ligo Ashen Desert: Latan gives the player a package "for Lieutenant
    /// Blood"; Captain Yamamoto at Boneyard Outpost opens it and sees Blood taking bribes from the Labbna.
    ///
    /// Native: texts 10100-10103, 10104 "none" (missionconversation): log "Deliver Latan's Package to Captain Yamamoto so that
    /// he can see the contents. Captain Yamamoto is in Boneyard Outpost in eastern Ashen Desert.", offer "It will be easy to get
    /// proof your Colonel requires. You should talk to Lieutenant Blood. ...", turn-in "Hahahahahahaha! ... What the hell is he
    /// doing taking bribes from those damned Forkheads?" (Yamamoto's); objective 1 "Delivery Latan's Package to Captain
    /// Yamamoto" 10110/10111, no counter (missionobjective); objectiveconversation (1259,1) package 1414 (Yamamoto) variant 251
    /// 10112 ("So, she gave you a present, huh? For Lieutenant Blood?"). Item class 25439 Latan's Package (template 97462).
    ///
    /// Sourced: TaRapedia A Bribe for Your Thoughts: giver Scavenger Vakiel Latan (Ashoka), reward giver Captain Aiko Yamamoto
    /// (Boneyard Outpost); requires The Human Perspective; 45,000 XP, 4,100 credits; no item named; follow-up Court Martial
    /// Arts.
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3):
    ///  - The package at the accept (a bag with no room refuses it), taken by Yamamoto at the talk; gone at an abandon or a
    ///    failure. The offer names Blood, the objective and the client's talk Yamamoto: Yamamoto hears it (the client).
    ///  - Level 41; the stacks of Formal Complaints, one to choose (TaRapedia names no item; triage D6). A marker at Yamamoto.
    /// </summary>
    public static class AshenBribeForYourThoughtsV1
    {
        public const uint MissionId = 1259;
        public const uint Level = 41;
        public const uint Latan = AshenFormalComplaintsNpcsV1.Latan;
        public const uint Yamamoto = AshenLeadInNpcsV1.Yamamoto, YamamotoPackage = AshenLeadInNpcsV1.YamamotoPackage;
        public const uint Deliver = 1;
        public const uint Variant = AshenFormalComplaintsV1.YamamotoFlag;
        public const uint PackageTemplate = 97462, PackageClass = 25439;
        public const string PackageKey = "latans-package";
        public const double MarkerRadius = 8;

        public const uint Experience = 45000;
        public const uint Credits = 4100;

        public static void Up(MigrationBuilder migration)
        {
            var mission = new AshenMissionDataV1(migration, MissionId, "A Bribe for Your Thoughts", 10100, Latan, Yamamoto, Level);
            mission.Reward(Experience, Credits,
                new[]
                {
                    (AshenFormalComplaintsV1.EmpGrenade, AshenFormalComplaintsV1.EmpGrenades), (AshenFormalComplaintsV1.ResTraumaKit, AshenFormalComplaintsV1.ResTraumaKits),
                    (AshenFormalComplaintsV1.ConcussionGrenade, AshenFormalComplaintsV1.ConcussionGrenades), (AshenFormalComplaintsV1.BasicMedPack, AshenFormalComplaintsV1.BasicMedPacks)
                },
                new (uint, uint)[0]);
            mission.Prerequisite(AshenHumanPerspectiveV1.MissionId);

            mission.Objective(Deliver, 10110, 10111, 0, MissionObjectiveState.Incomplete);
            mission.Transition(Deliver);
            mission.Conversation(Deliver, YamamotoPackage, Variant);
            mission.Action(Deliver, 1, MissionActionKind.ConsumeMissionItem,
                itemIntent: new ConsumeMissionItemIntent("yamamoto-takes-latans-package", MissionId, PackageKey, 1, MissionItemScope.AssignmentIssued));
            mission.Action(Deliver, 2, MissionActionKind.GrantReward, reward: 1);

            var yamamoto = AshenLeadInNpcsV1.At(Yamamoto);
            mission.Indicator(Deliver, 1, yamamoto.X, yamamoto.Y, yamamoto.Z, MarkerRadius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1259",
                "Native1 'Delivery Latan's Package to Captain Yamamoto' 10110/10111, no counter; objectiveconversation 1259,1 " +
                "pkg1414 variant251 10112. Texts 10100-10103 (the turn-in is Yamamoto's), 10104 'none'. Item class25439 Latan's " +
                "Package (template97462).");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/A_Bribe_for_Your_Thoughts",
                "Giver Scavenger Vakiel Latan (Ashoka), reward giver Captain Aiko Yamamoto (Boneyard Outpost); requires The Human " +
                "Perspective; 45000XP, 4100 credits; no item named; follow-up Court Martial Arts.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:AshenBribeForYourThoughtsV1",
                "The package at the accept, taken by Yamamoto at the talk, gone at an abandon. Level41 estimated. Stacks of 1254, " +
                "one to choose (zone median). A marker at Yamamoto.", 0.3);

            mission.Enable(Scene());
        }

        /// <summary>Latan's Package at the accept.</summary>
        public static MissionSceneDefinition Scene()
        {
            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                MapContextId = AshenMissionDataV1.MapContextId,
                Items = new()
                {
                    new MissionItemBinding(PackageKey, PackageTemplate, MissionItemScope.AssignmentIssued, 1,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove)
                },
                AcceptanceItems = new() { new IssueMissionItemIntent("latan-gives-her-package", MissionId, PackageKey, PackageTemplate, 1) }
            };
            scene.Sequences[0] = new SceneSequenceDefinition();
            return scene;
        }

        public static void Down(MigrationBuilder migration) => AshenMissionDataV1.Remove(migration, MissionId);
    }
}
