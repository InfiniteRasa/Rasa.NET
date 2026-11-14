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
    /// Save the Queen (native mission 1501), the Bane Conscription Facility (squad instance 1988) to Ashen Desert and
    /// Thunderhead: an Atta queen kept alive in the Bane's Information Extraction begs the player to end her suffering and save
    /// her egg from the Enslaver; the egg goes to Captain Yamamoto and on to Commander Jawan at Thunderhead Base.
    ///
    /// Native: texts 13399-13403 (missionconversation; 13403 "none"); objectives (missionobjective) 1 "Go to Information
    /// Extraction to Find the Atta Queen" 14284/14285, 2 "Protect the Egg and Kill the Enslaver" 14287/14288, 3 "Interact with
    /// the Atta Queen" 17309/17310, 4 "End the Suffering of the Atta Queen" 17312/17313, 5 "Secure and Prepare the Queen Egg For
    /// Transport" 17323/17324, 6 "Take Egg to Captain Yamamoto in Ashen Desert." 17326/17327, 7 "Take Queen Egg to Commander
    /// Jawan in Thunderhead." 17337/17338; objective conversations (1501,3) package 1937 variant 1, 17311 (the queen: "stop the
    /// life support systems and take my queen egg"), (1501,5) package 1937, 17325, (1501,6) package 1414 Yamamoto, 17336,
    /// (1501,7) package 1648 Jawan, 17339. Classes (entityclass): 26716 UsableNPCInvisibleHumanoid_AttaQueen, 26423
    /// UsableThreeStateAttaQueenControlDeviceV01, 25386 UsableInertDestAttaQueeneggV01. Use name 153 "Life Support System"
    /// (usablenameoverridelanguage). Names 9708 Enslaver Xahn, 9525 Commander Jawan.
    ///
    /// Sourced: TaRapedia Save the Queen (0.5): Headquarters (radio) gives after Into the Lion's Den, Commander Jawan (Thunderhead
    /// Base) rewards; 200,000 XP, 12,000 credits; Modification: Debuff to Ice Resist, Steal Power, Virulent Resistance, Regen
    /// Power Bonus; "Killing the enslaver completes the objective", "The Bane can and will destroy the egg".
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3):
    ///  - Level 44; offered by radio on the 1499 reward and on arrival in the facility; requires 1499; turned in at Jawan
    ///    (AshenConscriptionQueenNpcsV1 790671, Thunderhead, the receiver; Thunderhead is not built: his row and pool only, D13).
    ///    Bound to the squad's copy.
    ///  - 1 Information Extraction (an area round its floor, r35). 2 Enslaver Xahn's kill (790666, his pool among the cells).
    ///    The egg (25386) stands from the arrival, the member's own, not attackable: the egg's loss is not built (M3, D17,
    ///    deviation).
    ///  - 3 the talk with the queen (26716, package 1937) by her life support: the client map's queen is not an entity (the
    ///    talk object is invisible), so the queen is the map's. 4 the use of the member's own Life Support System (26423, on
    ///    10000003, off 10000001 after the use; argument 1), usable once 3 is done; the queen's talk object goes.
    ///  - 5 the talk at the egg (a second 26716 by it, package 1937, 17325); the egg goes with the player (no egg item in the
    ///    client: an objective only). 6 Yamamoto's talk (Ashen Desert, package 1414, 17336). 7 Jawan's talk (package 1648,
    ///    17339): the reward; the turn-in at Jawan.
    ///  - Markers (unnamed): 1 the room's floor, r35; 2 Xahn, r10; 3 the queen, 4 the Life Support System, 5 the egg; 6 Yamamoto
    ///    (Ashen Desert); 7 Jawan (Thunderhead).
    ///  - The items by TaRapedia's names, grade 4 (as 1274's and 1269's): Weapon Module: Debuff Ice Resist [4] 123181, Weapon
    ///    Module: Steal Power [4] 123276, Armor Module: Resist Virulent [4] 123071, Armor Module: Power Regen Bonus [4] 123006;
    ///    one to choose.
    /// </summary>
    public static class AshenSaveTheQueenV1
    {
        public const uint MissionId = 1501;
        public const uint Level = 44;
        public const uint Facility = AshenConscriptionQueenNpcsV1.Facility, Thunderhead = AshenConscriptionQueenNpcsV1.Thunderhead;
        public const uint Xahn = AshenConscriptionQueenNpcsV1.Xahn;
        public const uint Yamamoto = AshenLeadInNpcsV1.Yamamoto, YamamotoPackage = AshenLeadInNpcsV1.YamamotoPackage;
        public const uint Jawan = AshenConscriptionQueenNpcsV1.Jawan, JawanPackage = AshenConscriptionQueenNpcsV1.JawanPackage;
        public const uint QueenPackage = 1937;          // objectiveconversation 1501,3 and 1501,5
        public const uint Variant = 1;

        public const uint FindTheQueen = 1, KillTheEnslaver = 2, TalkToTheQueen = 3, EndTheSuffering = 4, SecureTheEgg = 5,
            ToYamamoto = 6, ToJawan = 7;
        public const uint ExtractionArea = 1;

        public const uint QueenClass = 26716;           // UsableNPCInvisibleHumanoid_AttaQueen
        public const uint DeviceClass = 26423;          // UsableThreeStateAttaQueenControlDeviceV01
        public const uint DeviceOn = 10000003, DeviceOff = 10000001;     // ESTIMATE, as the Bane water converter tower's
        public const uint DeviceName = 153;             // usablenameoverridelanguage "Life Support System"
        public const uint EggClass = 25386;             // UsableInertDestAttaQueeneggV01
        public const uint UseArgument = 1;              // usabledata lookup[26423]: None
        // Xahn's pool named as a public-spawn actor: in a squad's copy it comes back on its own respawn time (900 s), not 45 min
        // after his death (SpawnPoolManager ReturnsOnOwnTime), as Kretas (1452) and Kuutk (1395).
        public const string XahnPoolRole = "bcf-xahn-pool";
        public const string QueenRole = "bcf-atta-queen", DeviceRole = "bcf-life-support", EggRole = "bcf-queen-egg", EggTalkRole = "bcf-queen-egg-talk";

        /// <summary>Scenarios: the device used (4's event), the device usable (3 done), the queen at rest (4 done), the egg taken (5 done).</summary>
        public const uint DeviceUsed = 1, DeviceReady = 2, QueenAtRest = 3, EggTaken = 4;
        public const uint DeviceSequence = 11;

        /// <summary>Information Extraction: its floor round the platform's hole, a cylinder.</summary>
        public static readonly (double X, double Y, double Z) ExtractionAt = (6, 76, 150);
        public const double ExtractionRadius = 35, ExtractionHalfHeight = 10;
        public static readonly ScenePosition ExtractionMarkerAt = new(6.5f, 78.11f, 145f);
        public const double NpcMarkerRadius = 8, ObjectMarkerRadius = 6, BossMarkerRadius = 10;     // ESTIMATE

        public static readonly ScenePosition QueenAt = new(6.5f, 78.11f, 159.5f);
        public static readonly ScenePosition DeviceAt = new(6.5f, 78.11f, 162.5f);
        public static readonly ScenePosition EggAt = new(11.5f, 78.11f, 155f);

        public const uint Experience = 200000;
        public const uint Credits = 12000;
        public const uint DebuffIceResist = 123181;     // Weapon Module: Debuff Ice Resist [4]
        public const uint StealPower = 123276;          // Weapon Module: Steal Power [4]
        public const uint ResistVirulent = 123071;      // Armor Module: Resist Virulent [4]
        public const uint PowerRegenBonus = 123006;     // Armor Module: Power Regen Bonus [4]

        public static void Up(MigrationBuilder migration)
        {
            var xahn = AshenConscriptionQueenNpcsV1.XahnAt;
            var yamamoto = AshenLeadInNpcsV1.YamamotoAt;
            var jawan = AshenConscriptionQueenNpcsV1.JawanAt;
            var mission = new AshenMissionDataV1(migration, MissionId, "Save the Queen", 13399, null, Jawan, Level);
            mission.RadioOffer(MissionOfferSourceDefinition.MissionRewarded(AshenIntoTheLionsDenV1.MissionId),
                new MissionOfferSourceDefinition(MissionOfferSourceKind.ServerEvent, MissionOfferSourceDefinition.MapArrivalKey, Facility));
            mission.Reward(Experience, Credits, new[] { DebuffIceResist, StealPower, ResistVirulent, PowerRegenBonus });
            mission.Prerequisite(AshenIntoTheLionsDenV1.MissionId);
            mission.Scenario(DeviceUsed, "The Life Support System used", 1);
            mission.Scenario(DeviceReady, "The Life Support System usable (3 done)", 0);
            mission.Scenario(QueenAtRest, "The queen at rest (4 done)", 0);
            mission.Scenario(EggTaken, "The egg taken (5 done)", 0);

            mission.Objective(FindTheQueen, 14284, 14285, 0, MissionObjectiveState.Incomplete);
            mission.Objective(KillTheEnslaver, 14287, 14288, 1, MissionObjectiveState.Inactive);
            mission.Objective(TalkToTheQueen, 17309, 17310, 2, MissionObjectiveState.Inactive);
            mission.Objective(EndTheSuffering, 17312, 17313, 3, MissionObjectiveState.Inactive);
            mission.Objective(SecureTheEgg, 17323, 17324, 4, MissionObjectiveState.Inactive);
            mission.Objective(ToYamamoto, 17326, 17327, 5, MissionObjectiveState.Inactive);
            mission.Objective(ToJawan, 17337, 17338, 6, MissionObjectiveState.Inactive);

            // 1: Information Extraction.
            mission.Area(ExtractionArea, Facility, ExtractionAt.X, ExtractionAt.Y, ExtractionAt.Z, ExtractionRadius, ExtractionHalfHeight);
            mission.Transition(FindTheQueen);
            mission.EnterArea(FindTheQueen, ExtractionArea);
            AshenRaidTheConscriptionFacilityV1.Open(mission, FindTheQueen, 1, KillTheEnslaver);

            // 2: Xahn dead.
            mission.Transition(KillTheEnslaver);
            mission.Progress(KillTheEnslaver, MissionProgressEventKind.CreatureKilled, Xahn, null);
            AshenRaidTheConscriptionFacilityV1.Open(mission, KillTheEnslaver, 1, TalkToTheQueen);

            // 3: the queen's talk: the Life Support System usable.
            mission.Transition(TalkToTheQueen);
            mission.Conversation(TalkToTheQueen, QueenPackage, Variant);
            AshenRaidTheConscriptionFacilityV1.Open(mission, TalkToTheQueen, 1, EndTheSuffering);
            mission.Action(TalkToTheQueen, 3, MissionActionKind.StartScenario, scenario: DeviceReady);

            // 4: the device used: the queen at rest.
            mission.Transition(EndTheSuffering);
            mission.Progress(EndTheSuffering, MissionProgressEventKind.ScenarioEvent, 1, null, counter: DeviceUsed);
            AshenRaidTheConscriptionFacilityV1.Open(mission, EndTheSuffering, 1, SecureTheEgg);
            mission.Action(EndTheSuffering, 3, MissionActionKind.StartScenario, scenario: QueenAtRest);

            // 5: the egg's talk: the egg taken.
            mission.Transition(SecureTheEgg);
            mission.Conversation(SecureTheEgg, QueenPackage, Variant);
            AshenRaidTheConscriptionFacilityV1.Open(mission, SecureTheEgg, 1, ToYamamoto);
            mission.Action(SecureTheEgg, 3, MissionActionKind.StartScenario, scenario: EggTaken);

            // 6: Yamamoto (Ashen Desert); 7: Jawan (Thunderhead): the reward; the turn-in at Jawan.
            mission.Transition(ToYamamoto);
            mission.Conversation(ToYamamoto, YamamotoPackage, Variant);
            AshenRaidTheConscriptionFacilityV1.Open(mission, ToYamamoto, 1, ToJawan);
            mission.Transition(ToJawan);
            mission.Conversation(ToJawan, JawanPackage, Variant);
            mission.Action(ToJawan, 1, MissionActionKind.GrantReward, reward: 1);

            mission.Indicator(FindTheQueen, 1, ExtractionMarkerAt.X, ExtractionMarkerAt.Y, ExtractionMarkerAt.Z, ExtractionRadius, Facility);
            mission.Indicator(KillTheEnslaver, 2, xahn.X, xahn.Y, xahn.Z, BossMarkerRadius, Facility);
            mission.Indicator(TalkToTheQueen, 3, QueenAt.X, QueenAt.Y, QueenAt.Z, ObjectMarkerRadius, Facility);
            mission.Indicator(EndTheSuffering, 4, DeviceAt.X, DeviceAt.Y, DeviceAt.Z, ObjectMarkerRadius, Facility);
            mission.Indicator(SecureTheEgg, 5, EggAt.X, EggAt.Y, EggAt.Z, ObjectMarkerRadius, Facility);
            mission.Indicator(ToYamamoto, 6, yamamoto.X, yamamoto.Y, yamamoto.Z, NpcMarkerRadius, AshenMissionDataV1.MapContextId);
            mission.Indicator(ToJawan, 7, jawan.X, jawan.Y, jawan.Z, NpcMarkerRadius, Thunderhead);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1501",
                "Native1 'Go to Information Extraction to Find the Atta Queen' 14284/14285, 2 'Protect the Egg and Kill the Enslaver' " +
                "14287/14288, 3 'Interact with the Atta Queen' 17309/17310, 4 'End the Suffering of the Atta Queen' 17312/17313, 5 'Secure " +
                "and Prepare the Queen Egg For Transport' 17323/17324, 6 'Take Egg to Captain Yamamoto in Ashen Desert.' 17326/17327, 7 " +
                "'Take Queen Egg to Commander Jawan in Thunderhead.' 17337/17338; objectiveconversation 1501,3 pkg1937 17311, 1501,5 " +
                "pkg1937 17325, 1501,6 pkg1414 17336, 1501,7 pkg1648 17339. Texts 13399-13403. Classes 26716, 26423, 25386; use name 153.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Save_the_Queen",
                "Headquarters (radio) gives after Into the Lion's Den; Commander Jawan (Thunderhead Base) rewards; 200000XP, 12000 " +
                "credits; Modification: Debuff to Ice Resist, Steal Power, Virulent Resistance, Regen Power Bonus; killing the enslaver " +
                "completes the objective; the Bane can destroy the egg.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:AshenSaveTheQueenV1",
                "Level44; radio offer (1499 rewarded, arrival in 1988), turn-in at Jawan 790671 (Thunderhead 1911, row and pool only); " +
                "bound to the squad's copy. 1 Information Extraction (6,76,150) r35; 2 Xahn 790666's kill (pool 790667); 3 the member's " +
                "own queen talk object 26716 (pkg1937); 4 his own Life Support System 26423 (10000003 -> 10000001, name 153); 5 the " +
                "egg's talk object (pkg1937), the egg 25386 goes (not attackable: M3); 6 Yamamoto (pkg1414); 7 Jawan (pkg1648). Items " +
                "123181, 123276, 123071, 123006.", 0.3);

            mission.Enable(Scene());
        }

        /// <summary>The member's own queen talk, Life Support System, egg and egg talk, in the squad's copy.</summary>
        public static MissionSceneDefinition Scene()
        {
            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                BoundMapContextId = Facility,
                MapContextId = Facility,
                Dialogue = new List<MissionDialogueTopicDefinition>
                {
                    new(TalkToTheQueen, QueenPackage, Variant, transitionId: 1),
                    new(SecureTheEgg, QueenPackage, Variant, transitionId: 1)
                }
            };
            scene.Actors[QueenRole] = new SceneActorDefinition(QueenRole, SceneActorKind.Object, QueenClass, QueenAt,
                Conversation: new SceneObjectConversation(MissionId, TalkToTheQueen, QueenPackage, TalkToTheQueen));
            scene.Actors[DeviceRole] = new SceneActorDefinition(DeviceRole, SceneActorKind.Object, DeviceClass, DeviceAt,
                InitiallyInteractable: false, InitialObjectState: DeviceOn, NameOverrideId: DeviceName,
                UseAction: new SceneObjectAction(MissionId, EndTheSuffering, DeviceSequence, UseArgument));
            scene.Actors[EggRole] = new SceneActorDefinition(EggRole, SceneActorKind.Object, EggClass, EggAt, InitiallyInteractable: false);
            scene.Actors[EggTalkRole] = new SceneActorDefinition(EggTalkRole, SceneActorKind.Object, QueenClass, EggAt,
                Conversation: new SceneObjectConversation(MissionId, SecureTheEgg, QueenPackage, SecureTheEgg));
            scene.Actors[XahnPoolRole] = new SceneActorDefinition(XahnPoolRole, SceneActorKind.PublicSpawn, AshenConscriptionQueenNpcsV1.XahnPool);
            scene.Sequences[0] = new SceneSequenceDefinition
            {
                World =
                {
                    new EnsureActorIntent($"create-{QueenRole}", QueenRole),
                    new EnsureActorIntent($"create-{DeviceRole}", DeviceRole),
                    new EnsureActorIntent($"create-{EggRole}", EggRole),
                    new EnsureActorIntent($"create-{EggTalkRole}", EggTalkRole)
                }
            };
            // The use: the event of 4 only (the world changes after 4's own steps).
            scene.Sequences[DeviceSequence] = new SceneSequenceDefinition { Signals = { new SceneMissionSignal(MissionId, DeviceUsed, 1) } };
            // Made in sequence 0 only (one Ensure per actor: the validator forbids one key in two sequences, and a second Ensure
            // journaled after this SetInteraction would make a rebuild drop the use, set 19's 1498 audit). 3 is the talk to the
            // queen's object, which sequence 0 makes too, so sequence 0 always runs before this one.
            scene.Sequences[DeviceReady] = new SceneSequenceDefinition
            {
                World = { new SetInteractionIntent("life-support-usable", DeviceRole, true) }
            };
            scene.Sequences[QueenAtRest] = new SceneSequenceDefinition
            {
                World =
                {
                    new TransitionObjectStateIntent("life-support-off", DeviceRole, DeviceOff),
                    new SetInteractionIntent("life-support-done", DeviceRole, false),
                    new RemoveActorIntent("the-queen-at-rest", QueenRole)
                }
            };
            scene.Sequences[EggTaken] = new SceneSequenceDefinition
            {
                World =
                {
                    new RemoveActorIntent("the-egg-taken", EggRole),
                    new RemoveActorIntent("the-egg-talk-goes", EggTalkRole)
                }
            };
            return scene;
        }

        public static void Down(MigrationBuilder migration) => AshenMissionDataV1.Remove(migration, MissionId);
    }
}
