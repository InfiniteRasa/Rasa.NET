using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Runtime;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Wilderness
{
    public static partial class WildernessRanjaGorgeV1
    {
        public const uint MatthewCreatureId = 123;
        public const uint MatthewSpawnId = 201;
        public const uint QuincyCreatureId = 124;
        public const uint QuincySpawnId = 202;
        public const uint EggClusterClassId = 10180;
        public const uint PredatorCreatureId = 530120;
        public const uint PredatorPrimarySpawnId = 530120;
        public const uint PredatorSecondarySpawnId = 530121;

        public static void Up(MigrationBuilder migration) =>
            Up(migration, CandidateMatthewRoute(), ProvisionalEggClusters(),
                new uint[] { PredatorCreatureId });

        public static void Up(MigrationBuilder migration, SceneRoute woundedRoute,
            IReadOnlyList<SceneActorDefinition> eggClusters, IReadOnlyList<uint> predatorCreatureIds)
        {
            ValidateWoundedRoute(woundedRoute);
            ValidateEggClusters(eggClusters);
            ValidatePredators(predatorCreatureIds);
            UpIndependent(migration);
            UpWalkingWounded(migration, woundedRoute);
            UpEggClusters(migration, eggClusters);
            UpPredatory(migration, predatorCreatureIds);
        }

        public static void UpIndependent(MigrationBuilder migration)
        {
            ContentsUnderPressure(migration);
            GoingNative(migration);
            VisitToTheElders(migration);
            FithikallyChallenged(migration);
            XanxForTheHelp(migration);
        }

        public static void Down(MigrationBuilder migration) =>
            WildernessMissionDataV1.Remove(migration, 425, 679, 451, 697, 860, 758, 787, 769);

        private static void ContentsUnderPressure(MigrationBuilder migration)
        {
            var mission = new WildernessMissionDataV1(migration, 425, "Contents Under Pressure",
                725, 94, 94, 10);
            mission.Prerequisite(444);
            mission.Objective(1, 1076, 6708, 0, MissionObjectiveState.Incomplete);
            mission.Objective(2, 1078, 6709, 1);
            mission.Objective(5, 2839, 6710, 2);
            mission.Objective(3, 1079, 6711, 3);
            mission.Transition(1);
            mission.Conversation(1, 120);
            mission.RevealAndActivate(1, 2);
            mission.Transition(2);
            mission.Progress(2, MissionProgressEventKind.ItemAcquired, 7574, target: 1);
            mission.RevealAndActivate(2, 5);
            mission.Transition(5);
            mission.Conversation(5, 120);
            mission.Action(5, 1, MissionActionKind.ConsumeMissionItem,
                itemIntent: new ConsumeMissionItemIntent("samuel-takes-samples", 425,
                    "recovered-samples", 1, MissionItemScope.CharacterOwned));
            mission.Action(5, 2, MissionActionKind.IssueMissionItem,
                itemIntent: new IssueMissionItemIntent("samuel-combines-samples", 425,
                    "combined-samples", 622, 1));
            mission.RevealAndActivate(5, 3, firstAction: 3);
            mission.Transition(3);
            mission.Conversation(3, 117);
            mission.Reward(7000, 1350,
                selectableItems: new (uint, uint)[] { (12827, 1), (12855, 1) });
            mission.Action(3, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Indicator(1, 1, -381.9961, 172.17188, -367.9961);
            mission.Indicator(2, 2, -323.88672, 166.57031, -208.28906, 30);
            mission.Indicator(5, 3, -381.9961, 172.17188, -367.9961);
            mission.Indicator(3, 4, -755.9961, 170.97656, -277.9961);
            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/425",
                "Native order 1,2,5,3. Samuel package120 precedes Graal sample recovery and the return to Eleanor package117; objective2/package120 is a reminder, not completion.");
            mission.Evidence(2, MissionEvidenceSourceKind.Reconstruction, "repository:docs/wilderness-missions.md",
                "Existing vial620/class7574 comes from Graal creature90 on map1220, one guaranteed corpse pickup. Samuel atomically exchanges it for assignment-owned combined vial622/class7576; Eleanor consumes it once at turn-in.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:docs/wilderness-missions.md",
                "XP7000 rounds five times sourced credits1350. Display level10 has no extra level prerequisite. Pathogex modifiers are unsupported; choose functional level9-13 Hazmat Boots12827 or Gloves12855, not cosmetic-only substitutes.");
            var scene = QualifiedScene();
            scene.ExistingFactObjectiveIds = new() { 2 };
            var combinedSamples = new AssignmentItemRequirement(425, "combined-samples");
            scene.ObjectiveRequirements[3] = combinedSamples;
            scene.Dialogue = new()
            {
                new MissionDialogueTopicDefinition(1, 120, 1, transitionId: 1, sourceCreatureId: 95),
                new MissionDialogueTopicDefinition(2, 120, 1, MissionDialogueKind.Ambient, sourceCreatureId: 95),
                new MissionDialogueTopicDefinition(5, 120, 1, transitionId: 1, sourceCreatureId: 95),
                new MissionDialogueTopicDefinition(3, 117, 1, transitionId: 1, requirement: combinedSamples)
            };
            scene.Items = new()
            {
                new MissionItemBinding("recovered-samples", 620, MissionItemScope.CharacterOwned, 1,
                    MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                    MissionItemCleanupDisposition.Retain,
                    Drop: new MissionItemDropDefinition(new uint[] { 90 }, 2, 1220, 100, 1)),
                new MissionItemBinding("combined-samples", 622, MissionItemScope.AssignmentIssued, 1,
                    MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove,
                    MissionItemCleanupDisposition.Remove, TurnInQuantity: 1)
            };
            mission.Enable(scene);
        }

        private static void GoingNative(MigrationBuilder migration)
        {
            var mission = new WildernessMissionDataV1(migration, 679, "Going Native",
                2867, 94, 93, 10);
            mission.Prerequisite(425);
            mission.Objective(1, 2875, 6672, 0, MissionObjectiveState.Incomplete);
            mission.Transition(1);
            mission.Conversation(1, 111);
            mission.Reward(4500, 900);
            mission.Action(1, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Indicator(1, 1, -708.9961, 170.09375, -334.9961);
            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:objectiveconversation/679/1/111/1/1",
                "Eleanor sends the player to canonical Ranja Nula creature93, native package111.");
            mission.Evidence(2, MissionEvidenceSourceKind.Reconstruction, "repository:docs/wilderness-missions.md",
                "Completed425 prerequisite, sourced4500XP/900credits, no recorded inventory reward. Display level10 is not an eligibility gate; no unproven Council Mark delivery is added.");
            mission.Enable(QualifiedScene());
        }

        private static void VisitToTheElders(MigrationBuilder migration)
        {
            var mission = new WildernessMissionDataV1(migration, 451, "A Visit To The Elders",
                829, 93, 91, 10);
            mission.Prerequisite(679);
            mission.Objective(3, 2910, 6799, 0, MissionObjectiveState.Incomplete, required: false);
            mission.Objective(1, 2906, 6800, 1, MissionObjectiveState.Incomplete);
            mission.Objective(2, 2908, 6801, 2);
            mission.Transition(1);
            mission.Conversation(1, 566);
            mission.RevealAndActivate(1, 2);
            mission.Transition(2);
            mission.Conversation(2, 567);
            mission.Reward(10000, 1500,
                selectableItems: new (uint, uint)[] { (631, 1), (97037, 1) });
            mission.Action(2, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Indicator(1, 1, -790.9258, 263.6289, 740.5039);
            mission.Indicator(2, 2, -790.6211, 263.6289, 743.5508);
            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/451",
                "Objective3/name2910/body6799 is the council header/reminder. Executable conversations are objective1 Doyan/package566 then objective2 Todae/package567. Native reminder2911/package569 is not an invented third conversation.");
            mission.Evidence(2, MissionEvidenceSourceKind.Reconstruction, "repository:docs/wilderness-missions.md",
                "Sourced10000XP/1500credits. Astra/Dynamo modifiers are unsupported; choose real Direct Healing Disc631/class7615 or Salvage Tool97037/class25830. Display level10 adds no gate.");
            mission.Enable(QualifiedScene());
        }

        public static void UpWalkingWounded(MigrationBuilder migration, SceneRoute route)
        {
            var scene = WalkingWounded(route);
            var mission = new WildernessMissionDataV1(migration, 697, "The Walking Wounded",
                3102, MatthewCreatureId, QuincyCreatureId, 10);
            mission.Objective(1, 3106, 6766, 0, MissionObjectiveState.Incomplete);
            mission.Objective(2, 3107, 6767, 1);
            mission.Scenario(1, "Living Matthew reaches the Ranja infirmary");
            ScenarioEvents(migration, 697, 1);
            mission.Transition(1);
            mission.Progress(1, MissionProgressEventKind.ScenarioEvent, 1, counter: 1);
            mission.RevealAndActivate(1, 2);
            mission.Transition(2);
            mission.Conversation(2, 121);
            mission.Reward(8000, 1600,
                selectableItems: new (uint, uint)[] { (11567, 1), (3061, 1) });
            mission.Action(2, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Indicator(1, 1, -691.9922, 170.13672, -339.1875);
            mission.Indicator(2, 2, -691.9922, 170.13672, -339.1875);
            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/697",
                "Native order1,2 requires living Matthew followed by Quincy package121. No native numeric time limit or instance prerequisite exists.");
            mission.Evidence(2, MissionEvidenceSourceKind.Reconstruction, "repository:docs/wilderness-missions.md",
                "Public Matthew creature123/spawn201 uses the coordinator-supplied grounded Ranja Caverns route to Quincy124/202. No fallback route, personal copy, teleport or injected arrival. Route-blocked/death/owner-loss fails the escort; terminal scenes release the public reservation.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:docs/wilderness-missions.md",
                "XP8000 is five times sourced credits1600. Luminar/AccuMax modifiers are unsupported; choose Motor Assist Boots11567/class6495 or Pistol3061/class11893. No invented urgency deadline; display level10 adds no gate.");
            mission.Evidence(4, MissionEvidenceSourceKind.Reconstruction, "session:wilderness-candidate-matthew-route.json",
                "The adopted 60-point route has59 bidirectionally complete legs on candidate C1623440A8B4D6219B7DAF27C4E15052038409AAD9647D7EC6D2D22F04AACBF3. Runtime walking speed5 is reconstruction. This evidence does not assert that the unchanged repository navmesh or actual public-actor traversal passes.");
            mission.Enable(scene);
        }

        public static void UpEggClusters(MigrationBuilder migration,
            IReadOnlyList<SceneActorDefinition> clusters)
        {
            var scene = EggClusters(clusters);
            var mission = new WildernessMissionDataV1(migration, 860, "Sacs And Violence",
                4475, 128, 128, 10);
            mission.Objective(1, 4479, 6740, 0, MissionObjectiveState.Incomplete, counterTextId: 4480);
            mission.Scenario(1, "Four distinct Ranja egg clusters destroyed");
            ScenarioEvents(migration, 860, 1, 2, 3, 4);
            mission.Transition(1);
            for (uint cluster = 1; cluster <= 4; cluster++)
                mission.Progress(1, MissionProgressEventKind.ScenarioEvent, cluster,
                    counter: 1, target: 4, trigger: cluster);
            mission.Reward(18000, 800,
                selectableItems: new (uint, uint)[] { (3315, 1), (11568, 1) });
            mission.Action(1, 1, MissionActionKind.GrantReward, reward: 1);
            for (var cluster = 0; cluster < clusters.Count; cluster++)
            {
                var position = clusters[cluster].Position;
                mission.Indicator(1, (uint)cluster + 1, position.X, position.Y, position.Z);
            }
            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/860/1",
                "Active objective body6740 requires FOUR egg clusters and counter text4480. Native log4476/opening4477 and the historical guide instead say SIX; those are conflicting evidence, not equal counts.");
            mission.Evidence(2, MissionEvidenceSourceKind.Reconstruction, "repository:docs/wilderness-missions.md",
                "Explicit adjudication selects four distinct authentic class10180 Egg Cluster objects at coordinator-supplied outdoor cave poses. Real0HP destruction emits one scoped event per role; repeated use/destruction cannot replace missing clusters. Object100HP is reconstruction, not recovered retail health.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:docs/wilderness-missions.md",
                "Sourced18000XP/800credits. Vextronics/Luminar modifiers are unsupported; choose Rifle3315/class12147 or Motor Assist Legs11568/class6498. Display level10 adds no gate.");
            mission.Evidence(4, MissionEvidenceSourceKind.Reconstruction, "session:w6-geometry-evidence.json",
                "The supplied geometry diagnosis provides four provisional class10180 poses with complete LOCAL Sten/Quincy paths. They are not recovered retail placements or proof of Matthew access; full-volume placement and native-client destruction remain integration gates.");
            mission.Enable(scene);
        }

        private static void FithikallyChallenged(MigrationBuilder migration)
        {
            var mission = new WildernessMissionDataV1(migration, 758, "Fithikally Challenged",
                3672, 111, 111, 10);
            mission.Prerequisite(771);
            mission.Objective(3, 3676, 6511, 0, MissionObjectiveState.Incomplete);
            mission.Transition(3);
            mission.Progress(3, MissionProgressEventKind.ItemAcquired, 11161, target: 10);
            mission.Reward(4000, 600, new (uint, uint)[] { (44920, 2), (45047, 4) });
            mission.Action(3, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/758/3",
                "Dr. Soji's native objective3 collects ten Fithik spleens/class11161 after771, not a kill count.");
            mission.Evidence(2, MissionEvidenceSourceKind.Reconstruction, "repository:docs/wilderness-missions.md",
                "Template2533, one guaranteed ordinary Fithik creature1 corpse pickup until ten are held; consume ten once at turn-in. Source promises two EMP Bombs plus four Advanced Med Packs. Approved R substitute111227->44920 grants two Class II Basic Med Packs; four Advanced Med Packs45047 remain unchanged. This is healing/consumption, not a bomb-effect claim.");
            mission.Enable(Collection("fithik-spleens", 2533, 10, 3, new uint[] { 1 }));
        }

        private static void XanxForTheHelp(MigrationBuilder migration)
        {
            var mission = new WildernessMissionDataV1(migration, 787, "Xanx For the Help",
                3819, 111, 111, 10);
            mission.Prerequisite(758);
            mission.Objective(3, 3823, 7832, 0, MissionObjectiveState.Incomplete);
            mission.Transition(3);
            mission.Progress(3, MissionProgressEventKind.ItemAcquired, 11160, target: 4);
            mission.Reward(4000, 600, new (uint, uint)[] { (44919, 2), (44920, 6) });
            mission.Action(3, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/787/3",
                "Dr. Soji's native objective3 collects four Xanx pincers/class11160 after758.");
            mission.Evidence(2, MissionEvidenceSourceKind.Reconstruction, "repository:docs/wilderness-missions.md",
                "Template2532, one guaranteed Xanx Wanderer creature87 corpse pickup until four are held; consume four once at turn-in. Approved R substitutes47594->44919 and45441->44920 preserve the two/six quantities as Class I Advanced and Class II Basic Med Packs. Original Res Trauma Kit/Fragmentation Grenade promises remain source evidence, not implemented trauma/grenade effects.");
            mission.Enable(Collection("xanx-pincers", 2532, 4, 3, new uint[] { 87 }));
        }

        public static void UpPredatory(MigrationBuilder migration, IReadOnlyList<uint> predatorCreatureIds)
        {
            ValidatePredators(predatorCreatureIds);
            var mission = new WildernessMissionDataV1(migration, 769, "Predatory",
                3727, 111, 111, 10);
            mission.Prerequisite(787);
            mission.Objective(2, 3731, 7810, 0, MissionObjectiveState.Incomplete);
            mission.Transition(2);
            mission.Progress(2, MissionProgressEventKind.ItemAcquired, 11159, target: 3);
            mission.Reward(4000, 600, new (uint, uint)[] { (44921, 6), (44920, 2) });
            mission.Action(2, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/769/2",
                "Native objective2 collects three Predator parts/class11159 after787.");
            mission.Evidence(2, MissionEvidenceSourceKind.Reconstruction, "repository:docs/wilderness-missions.md",
                "Template2531 from coordinator-supplied ordinary outdoor Daghda Predator creature IDs/class3902, map1220. One guaranteed eligible corpse pickup until three are held, consumed once. No instance gate or substitute boss from another map; Crater Lake is only an optional historical source.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:docs/wilderness-missions.md",
                "Six Standard Med Packs44921 remain unchanged. Approved R substitute111227->44920 preserves the other quantity as two Class II Basic Med Packs instead of the sourced EMP Bombs, without a bomb-effect claim. Display level10 reconstructed; sourced4000XP/600credits.");
            mission.Evidence(4, MissionEvidenceSourceKind.Reconstruction, "repository:WildernessAdditionalWorldV1",
                "The adopted outdoor source is creature530120/native class3902 in spawn pools530120 and530121. These are coordinator-owned terrain population rows, not the named Incline boss520022. Walking access and return to Soji require the final navigation acceptance gate.");
            mission.Enable(Collection("predator-parts", 2531, 3, 2, predatorCreatureIds));
        }

        private static MissionSceneDefinition Collection(string key, uint template, uint quantity,
            uint objective, IReadOnlyList<uint> creatures)
        {
            var scene = QualifiedScene();
            scene.ExistingFactObjectiveIds = new() { objective };
            scene.Items = new()
            {
                new MissionItemBinding(key, template, MissionItemScope.CharacterOwned, quantity,
                    MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                    MissionItemCleanupDisposition.Retain, TurnInQuantity: quantity,
                    Drop: new MissionItemDropDefinition(creatures, objective, 1220, 100, 1))
            };
            return scene;
        }

        private static void ValidatePredators(IReadOnlyList<uint> creatures)
        {
            if (creatures == null || creatures.Count == 0 || creatures.Any(id => id == 0) ||
                creatures.Distinct().Count() != creatures.Count || creatures.Contains(520022U))
                throw new ArgumentException(
                    "Predatory requires allocated ordinary outdoor Wilderness Predator creature IDs, not the Incline boss.",
                    nameof(creatures));
        }

        private static void ScenarioEvents(MigrationBuilder migration, uint mission, params uint[] events)
        {
            foreach (var eventId in events)
                migration.InsertData("mission_scenario_step",
                    new[]
                    {
                        "mission_id", "content_revision", "scenario_id", "step_id", "kind", "sequence",
                        "requirement", "scenario_event_id", "comment"
                    },
                    new object[]
                    {
                        mission, WildernessMissionDataV1.Revision, 1U, eventId,
                        (byte)MissionScenarioStepKind.EmitScenarioEvent, eventId,
                        (byte)MissionContentRequirement.Required, eventId, "Native W6 scene result"
                    });
        }
    }
}
