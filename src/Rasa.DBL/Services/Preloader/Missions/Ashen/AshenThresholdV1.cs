using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Runtime;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// Threshold (native mission 1360), Indra Caverns (squad instance 2055): Major Darkua Diren sends the player past the Indra
    /// Watch Post to the Brann storage facility: Guard Rolim Bendal's body holds the facility's key card, the Bane hold the
    /// Watch Post, and Sergeant Jonas Drof's recon team waits inside, barricaded for two weeks.
    ///
    /// Native: texts 11183-11187 (missionconversation; 6 empty); objectives (missionobjective) 1 "Find Guard Rolim Bendal"
    /// 11331/11332 ("Search his body for a set of key cards ... the Brann storage facility behind the guard post"), 2 "Find the
    /// Missing Recon Team" 11339/11340, 3 "Take Back Indra Watch Post" 11707/11708 ("Kill all of the Bane manning Indra Watch
    /// Post"), no counters; objective conversation (1360, 2) package 1544 (Drof) 11348 ("barricaded in this room for two weeks").
    /// Items: class 25628 Supply Package (template 97516), class 25630 Indra Facility Key Card (template 97518). Usable name 121
    /// (Bendal's corpse, usablenameoverride). Indicator names 224 (1), 222 (2), 223 (3). Map: regions Indra Watch Post (-18, 9,
    /// -333), Security Check Point (73, 16, -328), the Storage Facility.
    ///
    /// Sourced: TaRapedia Threshold (0.5): Major Darkua Diren, reward from Sergeant Jonas Drof; requires Indra Caverns; follow-up
    /// Vindication; 120,000 XP, 9,000 credits; "the keycard on the body in the pillbox in front of the facility door, enter the
    /// facility, and wipe out the thrax attacking Drof"; "Do NOT kill ahead of this mission".
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3):
    ///  - Level 43; taken at Diren in the squad's copy (the scene bound to 2055). The Supply Package at the accept, taken by Drof
    ///    at the talk (2); gone at an abandon.
    ///  - 1 Bendal's body: a personal human corpse (UsableTreasureDispHumCorpseMaleV01 24995, closed 200, opened 201; usabledata
    ///    None: the client sends 1; name 121) 9 m south-west of the pillbox (ArchHumOutpostTrenchBunkerV01 at (14, 16, -343)) by
    ///    the way past the Watch Post, under open sky, 40 m and more off both Watch Post pools (TaRapedia: "in the pillbox"; the
    ///    pillbox's inside has 1.8 m of headroom and its trench no way in on the client floor); its search gives the key card
    ///    (AssignmentIssued) and completes 1 (a scene result: in the squad's copy each member on 1360 whose 1 is open; 1's
    ///    action gives each of them his own card, as Crucible 1401's cards).
    ///  - The facility door: a personal Brann door (UsableDoorBrannCorridor04mV01 10749, closed 31, open 91, argument 1) in the
    ///    Check Point's jamb (ArchBrannCorridorJamb014mV01 at (84, 16.5, -328), yaw -90 degrees: the door 2 m along its local -z),
    ///    locked to the key card (KeyItemTemplateId: without it the server refuses the use). It is the only way into the facility
    ///    and the caves behind it on the client floor (flood of 2026-11-11). Its use sends the event of hidden 4; 4 takes the
    ///    card of each member on 1360 whose 4 completes and opens his own door; a mate away at the use (or whose card holder
    ///    left) uses his own door with his own card.
    ///  - 3 the Watch Post's two Bane pools (AshenIndraFacilityNpcsV1, one each) cleared at once (SpawnPoolCleared, an all-pools
    ///    set, as Plateau 1977 and Crucible 1529); kills before the accept count (existing facts) - against TaRapedia's "do NOT
    ///    kill ahead", which the server's pools can not keep (a pool is the copy's, not the mission's). No Thrax attack on Drof
    ///    (the cautious option: no scene fight in a closed room).
    ///  - 2 the talk at Drof (package 1544) in the Control Room, opened once 1 and 3 are done (hidden 5 and 6), the turn-in there.
    ///  - The items: the lowest Uncommon template of the lowest version of each kind in the band of level 43 (client entityclass
    ///    names): Graviton V02 Legs 23957, Hazmat V02 Helmet 20653, Reflective V02 Gloves 35740, MotorAssist V04 Helmet 28960 (43-47: the lowest MotorAssist helmet whose band holds 43).
    ///  - Markers: 224 on Bendal, 223 over the Watch Post (both pools), 222 on Drof; all in Indra Caverns.
    /// </summary>
    public static class AshenThresholdV1
    {
        public const uint MissionId = 1360;
        public const uint Level = 43;
        public const uint Caverns = AshenIndraFacilityNpcsV1.Caverns;
        public const uint Diren = AshenIndraFacilityNpcsV1.Diren;
        public const uint Drof = AshenIndraFacilityNpcsV1.Drof, DrofPackage = AshenIndraFacilityNpcsV1.DrofPackage;
        public const uint FindBendal = 1, FindReconTeam = 2, WatchPost = 3, DoorUsed = 4, Tracker = 5, Opener = 6;

        public const uint PackageTemplate = 97516, PackageClass = 25628;
        public const uint CardTemplate = 97518, CardClass = 25630;
        public const string PackageKey = "indra-supply-package", CardKey = "indra-facility-key-card";

        public const uint CorpseClass = 24995;          // UsableTreasureDispHumCorpseMaleV01
        public const uint CorpseName = 121;             // usablenameoverride: Guard Rolim Bendal's corpse
        public const uint CorpseClosed = 200, CorpseOpened = 201;
        public const uint DoorClass = 10749;            // UsableDoorBrannCorridor04mV01
        public const uint DoorClosed = 31, DoorOpen = 91;
        public const uint UseArgument = 1;              // usabledata lookup[24995] and [10749]: all None
        public const string CorpseRole = "indra-bendal", DoorRole = "indra-facility-door";

        public static readonly ScenePosition BendalAt = new(5f, 16f, -347f);
        /// <summary>The Check Point's jamb (84, 16.5, -328), yaw -pi/2: the door 2 m along the jamb's local -z.</summary>
        public static readonly ScenePosition DoorAt = new(82f, 16.5f, -328f);
        public const double DoorOrientation = -System.Math.PI / 2;

        /// <summary>Scenarios: Bendal searched (1's event), the door used (4's event), the door opens (4 done).</summary>
        public const uint BendalSearched = 1, DoorOpened = 2, DoorOpens = 3;
        public const uint BendalSequence = 11, DoorSequence = 12;

        public const uint BendalIndicator = 224, ReconTeamIndicator = 222, WatchPostIndicator = 223;
        /// <summary>Over the Watch Post: between its two pools, wide enough for both.</summary>
        public static readonly (double X, double Y, double Z) WatchPostMarkerAt = (4, 16.2, -345.5);
        public const double WatchPostMarkerRadius = 45, CorpseMarkerRadius = 8, NpcMarkerRadius = 8;

        public const uint Experience = 120000;
        public const uint Credits = 9000;
        public const uint GravitonLegs = 23957;         // Armor_T3_Graviton_V02_UNC_Legs_40_to_44
        public const uint HazmatHelmet = 20653;         // Armor_T2_Hazmat_V02_UNC_Helmet_39_to_43
        public const uint ReflectiveGloves = 35740;     // Armor_T2_Reflective_V02_UNC_Gloves_39_to_43
        public const uint MotorAssistHelmet = 28960;    // Armor_T1_MotorAssist_V04_UNC_Helmet_43_to_47

        public static void Up(MigrationBuilder migration)
        {
            var mission = new AshenMissionDataV1(migration, MissionId, "Threshold", 11183, Diren, Drof, Level);
            mission.Reward(Experience, Credits, new[] { GravitonLegs, HazmatHelmet, ReflectiveGloves, MotorAssistHelmet });
            mission.Prerequisite(AshenIndraCavernsV1.MissionId);
            mission.Scenario(BendalSearched, "Guard Rolim Bendal's body searched", 1);
            mission.Scenario(DoorOpened, "The facility door used with the key card", 1);
            mission.Scenario(DoorOpens, "The facility door opens (4 done)", 0);

            mission.Objective(FindBendal, 11331, 11332, 0, MissionObjectiveState.Incomplete);
            mission.Objective(FindReconTeam, 11339, 11340, 1, MissionObjectiveState.Inactive);
            mission.Objective(WatchPost, 11707, 11708, 2, MissionObjectiveState.Incomplete);
            mission.Objective(DoorUsed, 11331, 11332, 3, MissionObjectiveState.Incomplete, required: false);    // hidden
            mission.Objective(Tracker, 11331, 11332, 4, MissionObjectiveState.Incomplete, required: false);     // hidden
            mission.Objective(Opener, 11331, 11332, 5, MissionObjectiveState.Incomplete, required: false);      // hidden

            // 1: Bendal's body searched: each member on 1360 whose 1 completes gets his own card (as Crucible 1401's cards: a
            // squad mate away from the searcher, or one whose card holder leaves, still has a card for his own door).
            mission.Transition(FindBendal);
            mission.Progress(FindBendal, MissionProgressEventKind.ScenarioEvent, 1, null, counter: BendalSearched);
            mission.Action(FindBendal, 1, MissionActionKind.IssueMissionItem,
                itemIntent: new IssueMissionItemIntent("bendal-gives-the-key-card", MissionId, CardKey, CardTemplate, 1));

            // 3: both Watch Post pools cleared at once.
            mission.Transition(WatchPost);
            var trigger = 1U;
            foreach (var pool in AshenIndraFacilityNpcsV1.WatchPostPools)
                mission.Progress(WatchPost, MissionProgressEventKind.SpawnPoolCleared, pool, null, trigger: trigger++);

            // 4, hidden: the door used (its event): each member whose 4 completes spends his own card and his own door opens; a
            // mate away at the use opens his own door later with his own card.
            mission.Transition(DoorUsed);
            mission.Progress(DoorUsed, MissionProgressEventKind.ScenarioEvent, 1, null, counter: DoorOpened);
            mission.Action(DoorUsed, 1, MissionActionKind.ConsumeMissionItem,
                itemIntent: new ConsumeMissionItemIntent("facility-door-takes-the-card", MissionId, CardKey, 1, MissionItemScope.AssignmentIssued));
            mission.Action(DoorUsed, 2, MissionActionKind.StartScenario, scenario: DoorOpens);

            // 6, hidden: 5 (1 and 3 done) opens 2.
            mission.Transition(Opener);
            mission.ObjectiveState(Opener, Tracker, MissionObjectiveState.Completed);
            mission.Action(Opener, 1, MissionActionKind.RevealObjective, targetObjective: FindReconTeam);
            mission.Action(Opener, 2, MissionActionKind.ActivateObjective, targetObjective: FindReconTeam, state: MissionObjectiveState.Incomplete);

            // 2: the talk at Drof takes the Supply Package; the reward.
            mission.Transition(FindReconTeam);
            mission.Conversation(FindReconTeam, DrofPackage);
            mission.Action(FindReconTeam, 1, MissionActionKind.ConsumeMissionItem,
                itemIntent: new ConsumeMissionItemIntent("drof-takes-the-supply-package", MissionId, PackageKey, 1, MissionItemScope.AssignmentIssued));
            mission.Action(FindReconTeam, 2, MissionActionKind.GrantReward, reward: 1);

            var drof = AshenIndraFacilityNpcsV1.DrofAt;
            mission.NamedIndicator(FindBendal, BendalIndicator, BendalAt.X, BendalAt.Y, BendalAt.Z, CorpseMarkerRadius, Caverns);
            mission.NamedIndicator(FindReconTeam, ReconTeamIndicator, drof.X, drof.Y, drof.Z, NpcMarkerRadius, Caverns);
            mission.NamedIndicator(WatchPost, WatchPostIndicator, WatchPostMarkerAt.X, WatchPostMarkerAt.Y, WatchPostMarkerAt.Z,
                WatchPostMarkerRadius, Caverns);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1360",
                "Native1 'Find Guard Rolim Bendal' 11331/11332, 2 'Find the Missing Recon Team' 11339/11340, 3 'Take Back Indra Watch " +
                "Post' 11707/11708, no counters; objectiveconversation 1360,2 pkg1544 11348. Texts 11183-11187. Items class25628 Supply " +
                "Package (97516), class25630 Indra Facility Key Card (97518). Usable name 121. Indicators 222-224. Names 9727 Diren, " +
                "9427 Drof. Regions Indra Watch Post (-18,9,-333), Security Check Point (73,16,-328).");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Threshold",
                "Major Darkua Diren, reward from Sergeant Jonas Drof; requires Indra Caverns; follow-up Vindication; 120000XP, 9000 " +
                "credits; the keycard on the body in the pillbox in front of the facility door; enter the facility and wipe out the " +
                "thrax attacking Drof; do NOT kill ahead of this mission.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:AshenThresholdV1",
                "Level43; bound to the squad's copy 2055; Diren gives, Drof rewards. Package at the accept, taken at Drof's talk. " +
                "Bendal a personal corpse 24995 (name121, arg1, 200->201) by the pillbox; its search completes 1, which gives each member his card. " +
                "Personal door 10749 at (82,16.5,-328) yaw -pi/2 keyed 97518: the use completes hidden 4, which takes each " +
                "member's card and opens his door. 3 both Watch Post pools 790565/790566 cleared (existing facts count). Hidden 5/6 open 2 (Drof " +
                "pkg1544) after 1 and 3. No Thrax attack on Drof. Items 23957, 20653, 35740, 28960, one to choose.", 0.3);

            mission.Enable(Scene());
        }

        /// <summary>The package at the accept, the player's own Bendal and facility door, in the squad's copy.</summary>
        public static MissionSceneDefinition Scene()
        {
            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                BoundMapContextId = Caverns,    // taken at Diren in the squad's copy
                MapContextId = Caverns,
                HiddenObjectiveIds = new() { DoorUsed, Tracker, Opener },
                ExistingFactObjectiveIds = new() { WatchPost },
                ObjectiveAggregations = new()
                {
                    [Tracker] = new MissionObjectiveAggregation(new[] { FindBendal, WatchPost }, 2)
                },
                Items = new()
                {
                    new MissionItemBinding(PackageKey, PackageTemplate, MissionItemScope.AssignmentIssued, 1,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove),
                    new MissionItemBinding(CardKey, CardTemplate, MissionItemScope.AssignmentIssued, 1,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove)
                },
                AcceptanceItems = new() { new IssueMissionItemIntent("diren-gives-the-supply-package", MissionId, PackageKey, PackageTemplate, 1) }
            };
            scene.Sequences[0] = new SceneSequenceDefinition();
            scene.Actors[CorpseRole] = new SceneActorDefinition(CorpseRole, SceneActorKind.Object, CorpseClass, BendalAt,
                InitialObjectState: CorpseClosed, NameOverrideId: CorpseName,
                UseAction: new SceneObjectAction(MissionId, FindBendal, BendalSequence, UseArgument));
            scene.Actors[DoorRole] = new SceneActorDefinition(DoorRole, SceneActorKind.Object, DoorClass, DoorAt, Orientation: DoorOrientation,
                InitialObjectState: DoorClosed, UseAction: new SceneObjectAction(MissionId, DoorUsed, DoorSequence, UseArgument),
                KeyItemTemplateId: CardTemplate);
            scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{CorpseRole}", CorpseRole));
            scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{DoorRole}", DoorRole));

            scene.Sequences[BendalSequence] = new SceneSequenceDefinition
            {
                World =
                {
                    new TransitionObjectStateIntent($"{CorpseRole}-searched", CorpseRole, CorpseOpened),
                    new SetInteractionIntent($"{CorpseRole}-done", CorpseRole, false)
                },
                Signals = { new SceneMissionSignal(MissionId, BendalSearched, 1) }
            };
            // The use: the event (4 takes each member's card and opens his door).
            scene.Sequences[DoorSequence] = new SceneSequenceDefinition
            {
                Signals = { new SceneMissionSignal(MissionId, DoorOpened, 1) }
            };
            scene.Sequences[DoorOpens] = new SceneSequenceDefinition
            {
                World =
                {
                    new TransitionObjectStateIntent($"{DoorRole}-opens", DoorRole, DoorOpen),
                    new SetInteractionIntent($"{DoorRole}-done", DoorRole, false)
                }
            };
            return scene;
        }

        public static void Down(MigrationBuilder migration) => AshenMissionDataV1.Remove(migration, MissionId);
    }
}
