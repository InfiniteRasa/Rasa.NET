using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Wilderness
{
    public static partial class WildernessTwinPillarsV1
    {
        public const uint BloodSampleTemplate = 749;
        public const uint AnalysisResultsTemplate = 750;
        public const uint MachinaRemainsTemplate = 753;
        public const uint StimDustTemplate = 2226;
        public const uint BloodAnalyzerClass = 7123;
        public const uint ForeanMachinaClass = 6236;
        public const uint ForeanMachinaCreatureId = 630100;
        public const double BloodAnalyzerYaw = Math.PI;
        public static readonly ScenePosition BloodAnalyzerRoot = new(-124.8f, 222.04809f, -479.2f);
        public static readonly ScenePosition BloodAnalyzerApproach = new(-124.8f, 220.91783f, -477.2f);

        public static void Up(MigrationBuilder migration)
        {
            Quarantine(migration);
            UnityAmongMen(migration);
            SmugglersBlues(migration);
            SuspiciousMinds(migration);
            TraitorsToTheCause(migration);
            Machinations(migration);
            Logos(migration, 912, "Logos: Self", 4878, 4, 4882, 6698, 38,
                4000, 1500, -448.368, 164.678, -108.112);
            Logos(migration, 1634, "Logos: Target", 15718, 4, 15725, 15726, 49,
                4500, 800, -906.358, 190.186, -649.29);
            Logos(migration, 1635, "Logos: Here", 15740, 2, 15747, 15748, 53,
                4500, 900, -979, 286.096, 744);
            Logos(migration, 908, "Logos: Enemy", 4852, 1, 4856, 6299, 9,
                4000, 600, -436, 188.325, -620, prerequisite: 1069);
        }

        public static void Down(MigrationBuilder migration) =>
            WildernessMissionDataV1.Remove(migration, 442, 444, 623, 791, 570, 574, 912, 1634, 1635, 908);

        private static void Quarantine(MigrationBuilder migration)
        {
            var mission = new WildernessMissionDataV1(migration, 442, "Quarantine", 793, 140, 140, 8);
            mission.Reward(3500, 700, selectableItems: new[] { (3315U, 1U), (3603U, 1U) });
            mission.Objective(1, 1523, 6723, 1, MissionObjectiveState.Incomplete);
            mission.Objective(2, 1525, 6724, 2);
            mission.Transition(1);
            mission.Conversation(1, 218);
            mission.Action(1, 1, MissionActionKind.IssueMissionItem,
                itemIntent: new IssueMissionItemIntent("duncan-blood-sample", 442, "blood-sample", BloodSampleTemplate, 1));
            mission.RevealAndActivate(1, 2, 2);
            mission.Transition(2);
            mission.Conversation(2, 1486);
            mission.Action(2, 1, MissionActionKind.ConsumeMissionItem,
                itemIntent: new ConsumeMissionItemIntent("analyze-blood-sample", 442, "blood-sample", 1,
                    MissionItemScope.AssignmentIssued));
            mission.Action(2, 2, MissionActionKind.GrantReward, reward: 1);
            mission.Indicator(1, 1, -127.99609, 220.76172, -466.9961);
            mission.Indicator(2, 2, BloodAnalyzerApproach.X, BloodAnalyzerApproach.Y, BloodAnalyzerApproach.Z);
            Evidence(mission, 442, "Quarantine",
                "Native Duncan package218 then analyzer package1486. The unjoined terminal/class evidence is reconstructed as " +
                "NPC-capable Corman computer7123 with the existing object-conversation adapter, not a fake creature or NPC " +
                "dialogue sent to non-NPC switch9742. Exact owned sample749 is consumed in the native1486 completion transaction. " +
                "Adopted WorldB places the computer on desk26534 at(-124.8,222.04809,-479.2), yaw pi; the player stands " +
                "at(-124.8,220.91783,-477.2). Never floor-snap the desktop root. Base physical rifle3315 and shotgun3603 replace " +
                "unsupported AccuMax/Eclipse prefixes.");
            mission.Enable(QuarantineScene());
        }

        private static void UnityAmongMen(MigrationBuilder migration)
        {
            var mission = new WildernessMissionDataV1(migration, 444, "Unity Among Men", 801, 140, 94, 8);
            mission.Reward(6000, 1200, selectableItems: new[] { (20399U, 1U), (20697U, 1U) });
            mission.Prerequisite(442);
            mission.Objective(1, 2962, 6790, 1, MissionObjectiveState.Incomplete);
            mission.Transition(1);
            mission.Conversation(1, 117);
            mission.Action(1, 1, MissionActionKind.ConsumeMissionItem,
                itemIntent: new ConsumeMissionItemIntent("deliver-analysis-results", 444, "analysis-results", 1,
                    MissionItemScope.AssignmentIssued));
            mission.Action(1, 2, MissionActionKind.GrantReward, reward: 1);
            mission.Indicator(1, 1, -755.9961, 170.97656, -277.9961);
            Evidence(mission, 444, "Unity_Among_Men",
                "Requires rewarded442. Victor hands over actual Analyzer Results750 on acceptance; Eleanor/package117 consumes " +
                "that assignment's result once. XP6000 is five times sourced1200 credits. Existing Hazmat gloves20399/legs20697 " +
                "are the functional base equipment reconstruction for historical Pulsar rewards.");
            var scene = QualifiedScene();
            scene.Items = new List<MissionItemBinding>
            {
                Issued("analysis-results", AnalysisResultsTemplate, 1)
            };
            scene.AcceptanceItems = new List<CharacterIntent>
            {
                new IssueMissionItemIntent("victor-analysis-results", 444, "analysis-results", AnalysisResultsTemplate, 1)
            };
            mission.Enable(scene);
        }

        private static void SmugglersBlues(MigrationBuilder migration)
        {
            var mission = new WildernessMissionDataV1(migration, 623, "Smuggler's Blues", 2118, 131, 131, 8);
            mission.Reward(7000, 10000, fixedItems: new[] { (44919U, 1U) });
            mission.Objective(1, 2133, 6742, 1, MissionObjectiveState.Incomplete);
            mission.Objective(2, 2135, 6743, 2, MissionObjectiveState.Incomplete);
            mission.Objective(3, 2137, 6744, 3, MissionObjectiveState.Incomplete);
            mission.Objective(4, 2714, 6745, 4, counterTextId: 2715);
            mission.Objective(5, 3993, 6746, 5, MissionObjectiveState.Incomplete);
            foreach (var (objectiveId, packageId) in new[] { (1U, 382U), (2U, 504U), (3U, 505U) })
            {
                mission.Transition(objectiveId);
                mission.Conversation(objectiveId, packageId);
                mission.Action(objectiveId, 1, MissionActionKind.ConsumeMissionItem,
                    itemIntent: new ConsumeMissionItemIntent($"deliver-stim-dust-{objectiveId}", 623, "stim-dust", 1,
                        MissionItemScope.AssignmentIssued));
                mission.Scenario(objectiveId, $"Confirm Stim Dust delivery {objectiveId}");
                mission.Action(objectiveId, 2, MissionActionKind.StartScenario, scenario: objectiveId);
            }
            mission.Transition(4);
            mission.Conversation(4, 219);
            mission.Action(4, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Indicator(1, 1, 764, 293.97656, 409);
            mission.Indicator(2, 2, 189.41016, 167.0039, -94.41016);
            mission.Indicator(3, 3, -696, 170.233, -343);
            mission.Indicator(4, 4, -137.9961, 220.27734, -465.9961);
            Evidence(mission, 623, "Smuggler%27s_Blues",
                "Exactly three assignment-owned Stim Dust2226, one per native recipient382/504/505. Native hidden5 has empty " +
                "body/no-desc name and records the completed delivery batch;4 opens only after all three receipts. " +
                "Canonical supported-reward reconstruction replaces unsupported module123023 with one working medpack44919, " +
                "using existing medical action419/3. This grants healing, not a module, modification recipe or Fire Resistance. " +
                "Adopted WorldB moves existing George510005 to the native hospital at(-698,170.233,-345); the delivery " +
                "indicator names his interaction standpoint(-696,170.233,-343), not the rejected elevated marker.");
            mission.Enable(SmugglingScene());
        }

        private static void TraitorsToTheCause(MigrationBuilder migration)
        {
            var mission = new WildernessMissionDataV1(migration, 570, "Traitors to the Cause", 1719, 129, 106, 8);
            mission.Reward(8000, 1200, selectableItems: new[] { (4019U, 1U), (20846U, 1U) });
            mission.Objective(1, 1723, 6634, 1, MissionObjectiveState.Incomplete);
            mission.Transition(1);
            mission.Conversation(1, 227);
            mission.Action(1, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Indicator(1, 1, -45.804688, 207.33594, 69.21484);
            Evidence(mission, 570, "Traitors_to_the_Cause",
                "Outpost Commander Taylor sends the player to Council Elder Baruhi at the Memory Tree. " +
                "Existing physical chaingun4019 or Hazmat vest20846 implement the sourced AccuMax/Pulsar equipment " +
                "without unsupported manufacturer modifiers. This does not offer instance successor593.");
            mission.Enable(QualifiedScene());
        }

        private static void SuspiciousMinds(MigrationBuilder migration)
        {
            var mission = new WildernessMissionDataV1(migration, 791, "Suspicious Minds", 3839, 129, 129, 8);
            mission.Reward(3500, 700, fixedItems: new[] { (44918U, 1U), (44917U, 1U) });
            mission.Objective(1, 3846, 6760, 1, MissionObjectiveState.Incomplete, required: false);
            mission.Objective(2, 3848, 6761, 2, MissionObjectiveState.Incomplete);
            mission.Objective(3, 4002, 6762, 3, MissionObjectiveState.Incomplete);
            mission.Transition(1);
            mission.Conversation(1, 219);
            mission.Transition(2);
            mission.Conversation(2, 415);
            mission.Action(2, 1, MissionActionKind.FailRelatedMission,
                itemIntent: new FailRelatedMissionIntent("betray-moore", 623));
            mission.Action(2, 2, MissionActionKind.GrantReward, reward: 1);
            mission.Transition(2, transition: 2, result: MissionObjectiveState.Failed, sequence: 2);
            mission.Conversation(2, 415, transition: 2);
            mission.Indicator(1, 1, -137.9961, 220.27734, -465.9961);
            mission.Indicator(2, 2, -91.77344, 220.26953, -503.13272);
            Evidence(mission, 791, "Suspicious_Minds",
                "Native Taylor package415/flag1 has exactly choice1 betray (text3852) and choice2 deny (3853); empty3854 " +
                "is not a third choice. The related-failure action must atomically fail623, clean its exact owned parcels " +
                "and commit791.2; native hidden3 aggregates completed2 in the same transaction. Denial fails791 without any623 cost or reward. Only outstanding assignment-owned " +
                "Stim Dust admits the offer; completed opposite outcomes exclude both rewards. Optional Moore dialogue219 " +
                "does not gate the decision. Canonical supported-reward reconstruction maps111247 to working medpack44918 " +
                "and111136 to working medpack44917, one each, through existing medical action419/2 and419/1. " +
                "These are healing rewards, not EMP, adrenaline, trauma or module effects.");
            mission.Enable(SuspicionScene());
        }

        private static void Machinations(MigrationBuilder migration)
        {
            var mission = new WildernessMissionDataV1(migration, 574, "Machinations", 1748, 106, 119, 9);
            mission.Reward(8000, 1200, selectableItems: new[] { (20548U, 1U), (36083U, 1U) });
            mission.Prerequisite(570);
            mission.Objective(2, 1869, 6631, 1, MissionObjectiveState.Incomplete);
            mission.Objective(3, 2710, 6632, 2);
            mission.Transition(2);
            mission.Progress(2, MissionProgressEventKind.ItemAcquired, 7991, target: 10);
            mission.RevealAndActivate(2, 3);
            mission.Transition(3);
            mission.Conversation(3, 450);
            mission.Action(3, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Indicator(3, 1, -701.8125, 202.5625, 65.83984);
            Evidence(mission, 574, "Machinations",
                "Requires rewarded570. Ten actual Forean Machina Remains753/class7991; source is adopted WorldB creature630100, " +
                "native class6236 and weapon6019, with public spawns630100..630102 around the Memory Tree, never Hominis8. " +
                "One guaranteed eligible corpse item until ten are held; " +
                "the reward transaction consumes ten. Parsons119/package450 finishes outdoors. No593 handoff. " +
                "Hazmat helmet20548 and Reflective vest36083 replace historical Pulsar/Olympia prefixes.");
            var scene = QualifiedScene();
            scene.ExistingFactObjectiveIds = new List<uint> { 2 };
            scene.Items = new List<MissionItemBinding>
            {
                new MissionItemBinding("machina-remains", MachinaRemainsTemplate, MissionItemScope.CharacterOwned, 10,
                    MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                    MissionItemCleanupDisposition.Retain, TurnInQuantity: 10,
                    Drop: new MissionItemDropDefinition(new[] { ForeanMachinaCreatureId }, 2, 1220, 100, 1))
            };
            mission.Enable(scene);
        }

        private static void Logos(MigrationBuilder migration, uint missionId, string name, uint nameTextId,
            uint objectiveId, uint objectiveTextId, uint bodyTextId, uint logosId, uint experience, uint credits,
            double x, double y, double z, uint? prerequisite = null)
        {
            var mission = new WildernessMissionDataV1(migration, missionId, name, nameTextId, 134, 134, 8);
            mission.Reward(experience, credits);
            if (prerequisite.HasValue)
                mission.Prerequisite(prerequisite.Value);
            mission.Objective(objectiveId, objectiveTextId, bodyTextId, 1, MissionObjectiveState.Incomplete);
            mission.Transition(objectiveId);
            mission.Progress(objectiveId, MissionProgressEventKind.LogosAcquired, logosId);
            mission.Action(objectiveId, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Indicator(objectiveId, 1, x, y, z);
            Evidence(mission, missionId, "Wilderness_mission_list",
                $"Outdoor Standley134/package2049 assignment for Logos{logosId}. Existing durable ownership counts. " +
                "No instance-only Logos offers, no inventory Logos duplication, and no bypass of Here shrine access " +
                "requiring Mind56 and Power23. Display level8 is a reconstruction, not an admission minimum.");
            var scene = QualifiedScene();
            scene.ExistingFactObjectiveIds = new List<uint> { objectiveId };
            mission.Enable(scene);
        }

        private static void Evidence(WildernessMissionDataV1 mission, uint id, string sourcePage, string reconstruction)
        {
            mission.Evidence(1, MissionEvidenceSourceKind.Client,
                "trpython:data/generated/client/missionobjective.pyo_dis",
                $"Client1.16.5 native mission{id} objective, text and conversation identities.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation,
                $"https://tabularasa.fandom.com/wiki/{sourcePage}", "Sourced mission route and numeric reward contract.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "docs/wilderness-missions.md", reconstruction);
        }
    }
}
