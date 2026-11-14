using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// Court Martial Arts (native mission 1261), Ligo Ashen Desert: Captain Yamamoto hands Latan's package on to Colonel Mason
    /// Linier at Shadow's Edge Post, who has heard it all by radio.
    ///
    /// Native: texts 10113-10116, 10117 "none" (missionconversation): log "Deliver Latan's Package to Colonel Linier at Shadow's
    /// Edge Post. ...", offer "Take this package to Colonel Linier. ..." (Yamamoto's), turn-in "The Labbna have been competing
    /// with us for salvage rights since we got here. ..." (Linier's); objective 1 "Deliver Latan's Package to Colonel Linier"
    /// 10118/10119, no counter (missionobjective); objectiveconversation (1261,1) package 1389 (Mason Linier) variant 251 10120
    /// ("Captain Yamamoto radioed ahead about the situation, and I thank you for all your help."). Item class 25439 Latan's
    /// Package (template 97462).
    ///
    /// Sourced: TaRapedia Court Martial Arts: giver Captain Aiko Yamamoto (Boneyard Outpost), reward giver Colonel Mason Linier
    /// (Shadow's Edge Post); requires A Bribe for Your Thoughts; 45,000 XP, 4,100 credits; one of Class VIII Frag Grenade x3,
    /// EMP Bomb, Cryogenic Grenade x3, Standard Med Pack x3; follow-up Dearly Departed. Items by their client names: 22964 Class
    /// VIII Fragmentation Grenade (template 45447), 26543 EMP Bomb (111233), 26299 Cryogenic Grenade (111049), 22547 Standard Med
    /// Pack (45064).
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3): the package at the accept again (Yamamoto keeps it after 1259 and gives it
    /// here; a bag with no room refuses it), taken by Linier at the talk; gone at an abandon. Level 41. A marker at Linier.
    /// </summary>
    public static class AshenCourtMartialArtsV1
    {
        public const uint MissionId = 1261;
        public const uint Level = 41;
        public const uint Yamamoto = AshenLeadInNpcsV1.Yamamoto;
        public const uint MasonLinier = AshenLeadInNpcsV1.MasonLinier, MasonLinierPackage = AshenLeadInNpcsV1.MasonLinierPackage;
        public const uint Deliver = 1;
        public const uint Variant = AshenFormalComplaintsV1.YamamotoFlag;
        public const uint PackageTemplate = AshenBribeForYourThoughtsV1.PackageTemplate, PackageClass = AshenBribeForYourThoughtsV1.PackageClass;
        public const string PackageKey = AshenBribeForYourThoughtsV1.PackageKey;
        public const double MarkerRadius = 8;

        public const uint Experience = 45000;
        public const uint Credits = 4100;
        public const uint FragGrenade = 45447, FragGrenades = 3;        // Class VIII Fragmentation Grenade (class 22964)
        public const uint EmpBomb = 111233, EmpBombs = 1;               // Class VIII EMP Bomb (class 26543)
        public const uint CryoGrenade = 111049, CryoGrenades = 3;       // Class VIII Cryogenic Grenade (class 26299)
        public const uint StandardMedPack = 45064, StandardMedPacks = 3; // Class VIII Standard Med Pack (class 22547)

        public static void Up(MigrationBuilder migration)
        {
            var mission = new AshenMissionDataV1(migration, MissionId, "Court Martial Arts", 10113, Yamamoto, MasonLinier, Level);
            mission.Reward(Experience, Credits,
                new[] { (FragGrenade, FragGrenades), (EmpBomb, EmpBombs), (CryoGrenade, CryoGrenades), (StandardMedPack, StandardMedPacks) },
                new (uint, uint)[0]);
            mission.Prerequisite(AshenBribeForYourThoughtsV1.MissionId);

            mission.Objective(Deliver, 10118, 10119, 0, MissionObjectiveState.Incomplete);
            mission.Transition(Deliver);
            mission.Conversation(Deliver, MasonLinierPackage, Variant);
            mission.Action(Deliver, 1, MissionActionKind.ConsumeMissionItem,
                itemIntent: new ConsumeMissionItemIntent("linier-takes-latans-package", MissionId, PackageKey, 1, MissionItemScope.AssignmentIssued));
            mission.Action(Deliver, 2, MissionActionKind.GrantReward, reward: 1);

            var linier = AshenLeadInNpcsV1.At(MasonLinier);
            mission.Indicator(Deliver, 1, linier.X, linier.Y, linier.Z, MarkerRadius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1261",
                "Native1 'Deliver Latan's Package to Colonel Linier' 10118/10119, no counter; objectiveconversation 1261,1 pkg1389 " +
                "variant251 10120. Texts 10113-10116, 10117 'none'. Item class25439 (template97462). Items 22964/45447, 26543/111233, " +
                "26299/111049, 22547/45064 by their client names.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Court_Martial_Arts",
                "Giver Captain Aiko Yamamoto (Boneyard Outpost), reward giver Colonel Mason Linier (Shadow's Edge Post); requires A " +
                "Bribe for Your Thoughts; 45000XP, 4100 credits; Class VIII Frag Grenade x3, EMP Bomb, Cryogenic Grenade x3, " +
                "Standard Med Pack x3; follow-up Dearly Departed.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:AshenCourtMartialArtsV1",
                "The package at the accept, taken by Linier at the talk, gone at an abandon. Level41 estimated. Stacks 45447x3, " +
                "111233x1, 111049x3, 45064x3, one to choose. A marker at Linier.", 0.3);

            mission.Enable(Scene());
        }

        /// <summary>Latan's Package at the accept, from Yamamoto.</summary>
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
                AcceptanceItems = new() { new IssueMissionItemIntent("yamamoto-gives-latans-package", MissionId, PackageKey, PackageTemplate, 1) }
            };
            scene.Sequences[0] = new SceneSequenceDefinition();
            return scene;
        }

        public static void Down(MigrationBuilder migration) => AshenMissionDataV1.Remove(migration, MissionId);
    }
}
