using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// What Makes Them Tick (native mission 1551), the Bane Conscription Facility (squad instance 1988): Headquarters wants the
    /// enraged Atta of the facility's east wing and a DNA sample of their queen; the player brings them to Captain Yamamoto in
    /// the Boneyard Outpost (Ashen Desert).
    ///
    /// Native: texts 14075-14079 (missionconversation; 14079 empty); objectives (missionobjective) 1 "Eliminate Enraged Atta"
    /// 14080/14081 counter 14082 "Enraged Atta Eliminated", 2 "Eliminate Atta Queen" 14083/14084 counter 14085 "Atta Queen
    /// Eliminated"; objective conversation (1551,2) package 1937 variant 1, 14283 "You cafeully obtain a DNA sample from the dead
    /// Atta queen." The log: "return them to Cpt. Yamamoto in the Boneyard Outpost".
    ///
    /// Sourced: TaRapedia What Makes Them Tick (0.5): Headquarters gives (radio) after Blast the Biolab, Yamamoto rewards; 100,000
    /// XP, 6,450 credits; "Killing any of the red Atta qualifies, you don't even need to loot them".
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3):
    ///  - Level 43; offered by radio as soon as Blast the Biolab is rewarded and on arrival in the facility (as 1498's), each
    ///    member on his own reward; requires 1498; turned in at Yamamoto (AshenLeadInNpcsV1 790003, the receiver). Bound to the
    ///    squad's copy.
    ///  - 1 ten kills (D16, no source gives the count) of the Enraged Atta (AshenConscriptionQueenNpcsV1 790662: four in one
    ///    pool in the arena ring's east hall).
    ///  - Hidden 3 (ours) the queen's kill (790664, alone in her pool at the ring's south end): it puts the member's own DNA
    ///    sample (the client's invisible queen talk object, UsableNPCInvisibleHumanoid_AttaQueen 26716, package 1937) by her
    ///    body; 2 completes by its talk (14283). The client's "Atta Queen Eliminated" counter (14085) is not written (deviation).
    ///  - Markers (unnamed): 1 on the Enraged Atta's pool, r20; 2 on the queen, r10. 3 hidden, none.
    ///  - The items: the lowest Uncommon template of each kind in the band of level 43 with TaRapedia's version numbers (client
    ///    entityclass names): Bio V03 Boots 19415 (40-44), Stealth V03 Legs 39856 (40-44), Hazmat V02 Legs 20802 (39-43),
    ///    Reflective V05 RAR Legs 38283 (39-43); one to choose.
    /// </summary>
    public static class AshenWhatMakesThemTickV1
    {
        public const uint MissionId = 1551;
        public const uint Level = 43;
        public const uint Facility = AshenConscriptionQueenNpcsV1.Facility;
        public const uint Yamamoto = AshenLeadInNpcsV1.Yamamoto;
        public const uint EnragedAtta = AshenConscriptionQueenNpcsV1.EnragedAtta, Queen = AshenConscriptionQueenNpcsV1.Queen;
        public const uint Variant = 1;

        public const uint KillTheAtta = 1, KillTheQueen = 2, QueenDown = 3;     // 3: server only, hidden
        public const uint AttaKills = 10;               // D16, ESTIMATE

        public const uint SampleClass = 26716;          // UsableNPCInvisibleHumanoid_AttaQueen
        public const uint SamplePackage = 1937;         // objectiveconversation 1551,2
        public const string SampleRole = "bcf-queen-dna-sample";
        // The pools named as public-spawn actors: in a squad's copy they come back on their own respawn time (SpawnPoolManager
        // ReturnsOnOwnTime), not 45 min after the last death (SquadInstanceConfig), as Kretas (1452) and Kuutk (1395).
        public const string AttaPoolRole = "bcf-enraged-atta-pool", QueenPoolRole = "bcf-atta-queen-pool";

        /// <summary>Scenarios: the queen down (3 done): the sample by her body.</summary>
        public const uint SampleReady = 1;

        public const double PoolMarkerRadius = 20, QueenMarkerRadius = 10;     // ESTIMATE

        /// <summary>The sample, 2.5 m off the queen's place (her body's loot is clicked apart from it).</summary>
        public static readonly ScenePosition SampleAt = new(271.5f, 97.61f, -333f);

        public const uint Experience = 100000;
        public const uint Credits = 6450;
        public const uint BioBoots = 19415;             // Armor_T3_Bio_V03_UNC_Boots_40_to_44
        public const uint StealthLegs = 39856;          // Armor_T3_Stealth_V03_UNC_Legs_40_to_44
        public const uint HazmatLegs = 20802;           // Armor_T2_Hazmat_V02_UNC_Legs_39_to_43
        public const uint ReflectiveLegs = 38283;       // Armor_T2_Reflective_V05_RAR_Legs_39_to_43

        public static void Up(MigrationBuilder migration)
        {
            var at = AshenConscriptionQueenNpcsV1.EnragedAttaAt;
            var queen = AshenConscriptionQueenNpcsV1.QueenAt;
            var mission = new AshenMissionDataV1(migration, MissionId, "What Makes Them Tick", 14075, null, Yamamoto, Level);
            mission.RadioOffer(MissionOfferSourceDefinition.MissionRewarded(AshenBlastTheBiolabV1.MissionId),
                new MissionOfferSourceDefinition(MissionOfferSourceKind.ServerEvent, MissionOfferSourceDefinition.MapArrivalKey, Facility));
            mission.Reward(Experience, Credits, new[] { BioBoots, StealthLegs, HazmatLegs, ReflectiveLegs });
            mission.Prerequisite(AshenBlastTheBiolabV1.MissionId);
            mission.Scenario(SampleReady, "The queen down: her DNA sample (3 done)", 0);

            mission.Objective(KillTheAtta, 14080, 14081, 0, MissionObjectiveState.Incomplete, counterTextId: 14082);
            mission.Objective(KillTheQueen, 14083, 14084, 1, MissionObjectiveState.Incomplete);
            mission.Objective(QueenDown, 14083, 14084, 2, MissionObjectiveState.Incomplete, required: false);    // hidden

            // 1: ten Enraged Atta.
            mission.Transition(KillTheAtta);
            mission.Progress(KillTheAtta, MissionProgressEventKind.CreatureKilled, EnragedAtta, AttaKills, counter: 0);

            // 3, hidden: the queen killed: the member's own sample by her body.
            mission.Transition(QueenDown);
            mission.Progress(QueenDown, MissionProgressEventKind.CreatureKilled, Queen, null);
            mission.Action(QueenDown, 1, MissionActionKind.StartScenario, scenario: SampleReady);

            // 2: the sample's talk; the reward (the turn-in at Yamamoto).
            mission.Transition(KillTheQueen);
            mission.Conversation(KillTheQueen, SamplePackage, Variant);
            mission.Action(KillTheQueen, 1, MissionActionKind.GrantReward, reward: 1);

            mission.Indicator(KillTheAtta, 1, at.X, at.Y, at.Z, PoolMarkerRadius, Facility);
            mission.Indicator(KillTheQueen, 2, queen.X, queen.Y, queen.Z, QueenMarkerRadius, Facility);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1551",
                "Native1 'Eliminate Enraged Atta' 14080/14081 c14082, 2 'Eliminate Atta Queen' 14083/14084 c14085; " +
                "objectiveconversation 1551,2 pkg1937 14283 (the DNA sample). Texts 14075-14079. Names 8094 Atta Soldier, 8092 Atta Hive Queen.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/What_Makes_Them_Tick",
                "Headquarters (radio) gives after Blast the Biolab; Yamamoto rewards; 100000XP, 6450 credits; any red Atta kill counts.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:AshenWhatMakesThemTickV1",
                "Level43; radio offer (1498 rewarded, arrival in 1988), turn-in at Yamamoto 790003; bound to the squad's copy. 1 ten " +
                "kills of 790662 (pool 790663, four, (350,97.61,-104)); hidden 3 the queen 790664's kill (pool 790665, (270,97.61,-335)) " +
                "puts the member's own sample 26716 (pkg1937) by her body; 2 its talk. Items 19415, 39856, 20802, 38283.", 0.3);

            mission.Enable(Scene());
        }

        /// <summary>The member's own DNA sample by the queen's body, in the squad's copy.</summary>
        public static MissionSceneDefinition Scene()
        {
            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                BoundMapContextId = Facility,
                MapContextId = Facility,
                HiddenObjectiveIds = new() { QueenDown },
                Dialogue = new List<MissionDialogueTopicDefinition> { new(KillTheQueen, SamplePackage, Variant, transitionId: 1) }
            };
            scene.Actors[SampleRole] = new SceneActorDefinition(SampleRole, SceneActorKind.Object, SampleClass, SampleAt,
                Conversation: new SceneObjectConversation(MissionId, KillTheQueen, SamplePackage, KillTheQueen));
            scene.Actors[AttaPoolRole] = new SceneActorDefinition(AttaPoolRole, SceneActorKind.PublicSpawn, AshenConscriptionQueenNpcsV1.EnragedAttaPool);
            scene.Actors[QueenPoolRole] = new SceneActorDefinition(QueenPoolRole, SceneActorKind.PublicSpawn, AshenConscriptionQueenNpcsV1.QueenPool);
            scene.Sequences[0] = new SceneSequenceDefinition();
            scene.Sequences[SampleReady] = new SceneSequenceDefinition
            {
                World = { new EnsureActorIntent($"create-{SampleRole}", SampleRole) }
            };
            return scene;
        }

        public static void Down(MigrationBuilder migration) => AshenMissionDataV1.Remove(migration, MissionId);
    }
}
