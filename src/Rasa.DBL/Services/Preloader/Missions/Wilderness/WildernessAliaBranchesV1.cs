using System.Collections.Generic;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Runtime;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Wilderness
{
    public static class WildernessAliaBranchesV1
    {
        private const uint BranchOutcomeFlag = 530002;

        /// <summary>Supplies On The Double (428)'s crate: UsableTreasureDispHumCrateV05, by the Lower Eloh bridge.</summary>
        public const uint SuppliesCrateClass = 26721;
        public static readonly ScenePosition SuppliesCratePosition = new ScenePosition(456, 233.9f, 193);
        public const double SuppliesCrateOrientation = 0;

        public static void Up(MigrationBuilder migration)
        {
            ConscientiousObjector(migration, MilpasEscort());
            ConscientiousReports(migration);
            SuppliesOnTheDouble(migration, SuppliesCrateClass, SuppliesCratePosition, SuppliesCrateOrientation);
            FathersGoodbye(migration);
            LurkingInTheShadows(migration, 76);
            MinerDifficulties(migration);
            TrainingDay(migration);
            ClassEquipment(migration);
            LogosDispatches(migration);
            ReportToStandley(migration);
        }

        public static void Down(MigrationBuilder migration) =>
            WildernessMissionDataV1.Remove(migration, 1390, 1392, 1393, 428, 421, 427, 422,
                1526, 2010, 2011, 1638, 1640, 921, 907, 909, 1639, 1633, 911, 1741);

        private static void ConscientiousObjector(MigrationBuilder migration, MissionSceneDefinition escort)
        {
            var mission = new WildernessMissionDataV1(migration, 1390, "Conscientious Objector",
                11928, 43, 43, 5);
            mission.Prerequisite(479);
            mission.Objective(1, 11950, 11951, 0, MissionObjectiveState.Incomplete);
            mission.Objective(2, 11956, 11957, 1, required: false);
            mission.Objective(3, 11959, 11960, 2, required: false);
            mission.Objective(4, 11962, 11963, 3, required: false);
            mission.Objective(8, 11970, 11971, 4, required: false);
            mission.Objective(10, 11974, 11975, 5, required: false);
            mission.Objective(11, 11976, 11977, 6, required: false);
            mission.Objective(12, 12000, 12001, 7, MissionObjectiveState.Incomplete);
            mission.Reward(2000, 400, selectableItems: new (uint, uint)[] { (111247, 1), (44917, 1) });
            mission.Scenario(1, "Escort Milpas to the Wilderness side of Divide", activatesObjective: 8);
            mission.Scenario(2, "Escort Milpas to Apirka", activatesObjective: 4);
            mission.Transition(1);
            mission.Conversation(1, 1646);
            mission.RevealAndActivate(1, 2);
            mission.Transition(1, 2, sequence: 2);
            mission.Conversation(1, 1646, transition: 2);
            mission.RevealAndActivate(1, 3, transition: 2);
            mission.Transition(2);
            mission.Conversation(2, 1646);
            mission.RevealAndActivate(2, 8);
            mission.Action(2, 3, MissionActionKind.StartScenario, scenario: 1);
            mission.Transition(3);
            mission.Conversation(3, 1646);
            mission.RevealAndActivate(3, 4);
            mission.Action(3, 3, MissionActionKind.StartScenario, scenario: 2);
            foreach (var report in new[] { (Objective: 10U, Outcome: 2U), (Objective: 11U, Outcome: 1U) })
            {
                mission.Transition(report.Objective);
                mission.Conversation(report.Objective, 112);
                migration.InsertData("mission_action",
                    new[]
                    {
                        "mission_id", "content_revision", "requirement", "objective_id", "transition_id",
                        "action_id", "kind", "sequence", "player_flag_id", "player_flag_value", "comment"
                    },
                    new object[]
                    {
                        1390U, WildernessMissionDataV1.Revision, (byte)MissionContentRequirement.Required,
                        report.Objective, 1U, 1U, (byte)MissionActionKind.SetPlayerFlag, 1U,
                        BranchOutcomeFlag, report.Outcome, "Persist the reported Milpas outcome"
                    });
                mission.Action(report.Objective, 2, MissionActionKind.GrantReward, reward: 1);
            }
            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1390",
                "Native Quillas package1646 choice1 releases, choice2 arrests; follow-up Quillas dialogue starts the respective escort; Apirka reports use package112. Native12 is hidden bookkeeping.");
            mission.Evidence(2, MissionEvidenceSourceKind.Reconstruction, "repository:docs/wilderness-missions.md",
                "Public Milpas630010 uses native name9519 and reconstructed unarmed Forean body26833. Grounded routes use native1.65 m/s walk speed; release stops outside map-link11 in Wilderness. Departure fails and releases the public encounter for retry. Hidden12 aggregates reports; flag530002 persists the outcome. Class I consumable tiers are reconstructed.");
            escort.Script = "wilderness.alia-branch";
            escort.HiddenObjectiveIds = new() { 12 };
            escort.ObjectiveAggregations = new()
            {
                [12] = new MissionObjectiveAggregation(new uint[] { 10, 11 }, 1)
            };
            escort.Dialogue = new()
            {
                new MissionDialogueTopicDefinition(1, 1646, 1, MissionDialogueKind.Choice,
                    choices: new Dictionary<int, uint> { [1] = 1, [2] = 2 })
            };
            escort.Sequences = new()
            {
                [0] = new SceneSequenceDefinition(),
                [1] = new SceneSequenceDefinition(),
                [2] = new SceneSequenceDefinition()
            };
            mission.Enable(escort);
        }

        private static void ConscientiousReports(MigrationBuilder migration)
        {
            foreach (var entry in new[]
            {
                (Id: 1392U, Other: 1393U, Outcome: 2U, Text: 12002U, Label: 12007U, Body: 12008U),
                (Id: 1393U, Other: 1392U, Outcome: 1U, Text: 12010U, Label: 12015U, Body: 12016U)
            })
            {
                var mission = new WildernessMissionDataV1(migration, entry.Id,
                    "Conscientious Objector - Part Two", entry.Text, 43, 100, 5);
                mission.Prerequisite(1390);
                mission.Objective(1, entry.Label, entry.Body, 0, MissionObjectiveState.Incomplete);
                mission.Reward(8000, 800, selectableItems: new (uint, uint)[] { (20250, 1), (35486, 1) });
                mission.Transition(1);
                mission.Conversation(1, 116);
                mission.Action(1, 1, MissionActionKind.GrantReward, reward: 1);
                mission.Evidence(1, MissionEvidenceSourceKind.Client, $"native:missionobjective/{entry.Id}",
                    "Native objective1 reports Apirka's chosen outcome to Rogers, package116; release1393 and arrest1392 are exclusive.");
                mission.Evidence(2, MissionEvidenceSourceKind.Reconstruction, "repository:docs/wilderness-missions.md",
                    "Both outcome titles use the sourced8000 XP/800-credit reward. Functional low-level Hazmat/Reflective boots replace unsupported Dynamo/Prodigy modifiers.");
                var requirement = new AllRequirements(new MissionRequirement[]
                {
                    new FlagRequirement(BranchOutcomeFlag, entry.Outcome),
                    new NotRequirement(new AnyRequirement(new MissionRequirement[]
                    {
                        new MissionStateRequirement(entry.Other, Accepted: true),
                        new MissionStateRequirement(entry.Other)
                    }))
                });
                mission.Enable(new MissionSceneDefinition { Requirement = requirement, TurnInRequirement = requirement });
            }
        }

        private static void SuppliesOnTheDouble(
            MigrationBuilder migration, uint crateClassId, ScenePosition position, double orientation)
        {
            var mission = new WildernessMissionDataV1(migration, 428, "Supplies On The Double",
                737, 510004, 510004, 5);
            mission.Prerequisite(479);
            mission.Objective(1, 1938, 6748, 0, MissionObjectiveState.Incomplete);
            mission.Objective(3, 2641, 6749, 1, required: false);
            mission.Objective(2, 1939, 6750, 2);
            mission.Reward(5000, 750, selectableItems: new (uint, uint)[] { (3869, 1), (97328, 1) });
            mission.Scenario(3, "Refrigerate the recovered medical supplies", activatesObjective: 2);
            mission.Transition(2);
            mission.Conversation(2, 130);
            mission.Action(2, 1, MissionActionKind.StartScenario, scenario: 3);
            mission.Action(2, 2, MissionActionKind.GrantReward, reward: 1);
            mission.Transition(3, result: MissionObjectiveState.Failed);
            migration.InsertData("mission_trigger",
                new[]
                {
                    "mission_id", "content_revision", "requirement", "objective_id", "transition_id",
                    "trigger_id", "kind", "sequence", "duration_seconds", "comment"
                },
                new object[]
                {
                    428U, WildernessMissionDataV1.Revision, (byte)MissionContentRequirement.Required,
                    3U, 1U, 1U, (byte)MissionTriggerKind.TimerElapsed, 1U, 300U, "Breached supplies spoil after300 seconds"
                });
            mission.Indicator(1, 1, position.X, position.Y, position.Z);
            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/428",
                "Native1 retrieves supplies; optional3 exposes a breached seal and spoilage;2 delivers to Elise/package130. Template686/class7706 is the physical shipment.");
            mission.Evidence(2, MissionEvidenceSourceKind.Reconstruction, "repository:docs/wilderness-missions.md",
                "Active native crate26721 shares mesh20300 with deleted duplicate7707; its dry-bank pose by the Lower Eloh bridge is reconstructed, not a recovered retail spawn. Native use recovers intact supplies; actual100-HP destruction breaches them for300 seconds. Base level5 tools replace unsupported modifiers; cipher97328 retains class25828 with functional weapon/ammo data.");
            mission.Enable(SuppliesScene(crateClassId, position, orientation));
        }

        /// <summary>Supplies On The Double (428)'s scene as the mission was first enabled with it (the crate in state 0).</summary>
        public static MissionSceneDefinition SuppliesScene(uint crateClassId, ScenePosition position, double orientation) =>
            new MissionSceneDefinition
            {
                Script = "wilderness.supply-delivery",
                Actors = new()
                {
                    ["supplies"] = new SceneActorDefinition("supplies", SceneActorKind.Object,
                        crateClassId, position, orientation,
                        UseAction: new SceneObjectAction(428, 1, 1),
                        Destruction: new SceneObjectDestruction(428, 1, 2, 100))
                },
                Items = new() { DeliveryItem("supplies", 686) },
                Sequences = new()
                {
                    [0] = new SceneSequenceDefinition
                    {
                        World = new() { new EnsureActorIntent("ensure-supplies", "supplies") }
                    },
                    [1] = SupplyPickup(false),
                    [2] = SupplyPickup(true),
                    [3] = new SceneSequenceDefinition()
                }
            };

        private static SceneSequenceDefinition SupplyPickup(bool damaged)
        {
            var key = damaged ? "damaged" : "intact";
            var sequence = new SceneSequenceDefinition
            {
                World = new() { new RemoveActorIntent("take-" + key + "-crate", "supplies") },
                Character = new()
                {
                    new IssueMissionItemIntent("issue-" + key + "-supplies", 428, "supplies", 686, 1),
                    new ObjectiveIntent("reveal-" + key + "-delivery", 428, 2, MissionObjectiveState.NotAssigned),
                    new ObjectiveIntent("activate-" + key + "-delivery", 428, 2, MissionObjectiveState.Incomplete)
                }
            };
            if (damaged)
            {
                sequence.Character.Add(new ObjectiveIntent("reveal-spoilage", 428, 3, MissionObjectiveState.NotAssigned));
                sequence.Character.Add(new ObjectiveIntent("activate-spoilage", 428, 3, MissionObjectiveState.Incomplete));
            }
            sequence.Character.Add(new ObjectiveIntent("recover-" + key + "-supplies", 428, 1, MissionObjectiveState.Completed));
            return sequence;
        }

        private static void LurkingInTheShadows(MigrationBuilder migration, uint fulgorCreatureId)
        {
            var mission = new WildernessMissionDataV1(migration, 427, "Lurking In The Shadows",
                733, 132, 132, 5);
            mission.Prerequisite(479);
            mission.Objective(1, 1021, 6703, 0, MissionObjectiveState.Incomplete);
            mission.Objective(6, 17243, 17244, 1);
            mission.Objective(7, 17245, 17246, 2);
            mission.Reward(4000, 400, new (uint, uint)[] { (20697, 1) });
            mission.Transition(1);
            mission.Conversation(1, 254);
            mission.RevealAndActivate(1, 6);
            mission.Transition(6);
            mission.Progress(6, MissionProgressEventKind.ItemAcquired, 12714, target: 1);
            mission.RevealAndActivate(6, 7);
            mission.Transition(7);
            mission.Conversation(7, 133);
            mission.Action(7, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/427",
                "Native1/package254 talks to Oliver;6 retrieves the shipment from named Lightbender Proctor Fulgor;7/package133 delivers to Caufield.");
            mission.Evidence(2, MissionEvidenceSourceKind.Reconstruction, "repository:docs/wilderness-missions.md",
                "Reuse Fulgor76/spawn580010/class10857/effective name10100. Its grounded pillbox does not match native southeast-of-Oliver wording relative to Oliver's seed pose; retail placement is not claimed. Ammunition3786/class12714 drops at reconstructed100 percent, costs one at turn-in and survives retry. Base Hazmat legs replace Phoenix modifiers.");
            mission.Enable(new MissionSceneDefinition
            {
                ExistingFactObjectiveIds = new() { 6 },
                Items = new()
                {
                    new MissionItemBinding("shipment", 3786, MissionItemScope.CharacterOwned, 1,
                        MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                        MissionItemCleanupDisposition.Retain, TurnInQuantity: 1,
                        Drop: new MissionItemDropDefinition(creatureIds: new[] { fulgorCreatureId },
                            objectiveId: 6, mapContextId: 1220, chancePercent: 100, quantity: 1))
                }
            });
        }

        private static void FathersGoodbye(MigrationBuilder migration)
        {
            var mission = new WildernessMissionDataV1(migration, 421, "A Father's Goodbye",
                709, 120, 116, 5);
            mission.Prerequisite(428);
            mission.Objective(3, 2613, 6642, 0, MissionObjectiveState.Incomplete);
            mission.Reward(6000, 900, selectableItems: new (uint, uint)[] { (4019, 1), (26996, 1) });
            mission.Transition(3);
            mission.Conversation(3, 210);
            mission.Action(3, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/421",
                "Native objective3/package210; Lt. Saviours gives his dogtags to deliver to Information Specialist Saviours.");
            mission.Evidence(2, MissionEvidenceSourceKind.Reconstruction, "repository:docs/wilderness-missions.md",
                "Existing dogtags613/class7547 are assignment-owned and consumed at turn-in. Functional level5 chaingun and early Motor Assist boots replace unsupported ChiTech/Pulsar modifiers.");
            mission.Enable(new MissionSceneDefinition
            {
                Items = new() { DeliveryItem("dogtags", 613) },
                AcceptanceItems = new() { new IssueMissionItemIntent("issue-dogtags", 421, "dogtags", 613, 1) }
            });
        }

        private static void MinerDifficulties(MigrationBuilder migration)
        {
            var mission = new WildernessMissionDataV1(migration, 422, "Miner Difficulties",
                713, 100, 126, 5);
            mission.Prerequisite(1069);
            mission.Objective(1, 1275, 6683, 0, MissionObjectiveState.Incomplete);
            mission.Reward(5000, 215, new (uint, uint)[] { (111247, 1), (44918, 1) });
            mission.Transition(1);
            mission.Conversation(1, 213);
            mission.Action(1, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missiontext/714",
                "Native objective1/package213; deliver two crates, medical supplies and mining equipment, from Rogers to Richards.");
            mission.Evidence(2, MissionEvidenceSourceKind.Reconstruction, "repository:docs/wilderness-missions.md",
                "Adopt sourced uncertain5000 XP; existing medical97/class4044 and mining697/class7746 are issued once and consumed together. One Class I EMP Bomb and one Class I Standard Med Pack implement the sourced consumable reward.");
            mission.Enable(new MissionSceneDefinition
            {
                Items = new() { DeliveryItem("medical", 97), DeliveryItem("mining", 697) },
                AcceptanceItems = new()
                {
                    new IssueMissionItemIntent("issue-mining-medicine", 422, "medical", 97, 1),
                    new IssueMissionItemIntent("issue-mining-equipment", 422, "mining", 697, 1)
                }
            });
        }

        private static void TrainingDay(MigrationBuilder migration)
        {
            var mission = new WildernessMissionDataV1(migration, 1526, "Training Day",
                13606, 100, 510006, 5);
            mission.Objective(1, 13617, 13618, 0, MissionObjectiveState.Incomplete);
            mission.Reward(1000, 200);
            mission.Transition(1);
            mission.Conversation(1, 2588);
            mission.Action(1, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missiontext/13607",
                "Native objective1/package2588 introduces Trainer Kincaid near the Alia barracks; existing training remains optional.");
            mission.Evidence(2, MissionEvidenceSourceKind.Reconstruction, "repository:docs/wilderness-missions.md",
                "Rogers gives the level5 introduction; Training Officer Kincaid510006 retains the existing generic trainer service. Already-specialized characters may report. Introductory1000 XP/200 credits are reconstructed.");
            mission.Enable(new MissionSceneDefinition { Requirement = new LevelRequirement(5) });
        }

        private static void ClassEquipment(MigrationBuilder migration)
        {
            foreach (var entry in new[]
            {
                (Id: 2010U, Other: 2011U, Name: "Soldier", Text: 21618U, Objective: 21623U,
                    Body: 21624U, Fact: "character.soldier-family", Weapon: 4019U, Boots: 35486U),
                (Id: 2011U, Other: 2010U, Name: "Specialist", Text: 21626U, Objective: 21631U,
                    Body: 21632U, Fact: "character.specialist-family", Weapon: 3869U, Boots: 20250U)
            })
            {
                var family = new CustomRequirement(entry.Fact);
                var requirement = new AllRequirements(new MissionRequirement[]
                {
                    family,
                    new NotRequirement(new AnyRequirement(new MissionRequirement[]
                    {
                        new MissionStateRequirement(entry.Other, Accepted: true),
                        new MissionStateRequirement(entry.Other)
                    }))
                });
                var mission = new WildernessMissionDataV1(migration, entry.Id,
                    "Getting It In Gear: " + entry.Name + " Class", entry.Text, null, 132, 5);
                mission.Objective(1, entry.Objective, entry.Body, 0, MissionObjectiveState.Incomplete);
                mission.Reward(1000, 200, new (uint, uint)[] { (entry.Weapon, 1), (entry.Boots, 1) });
                mission.Transition(1);
                mission.Conversation(1, 133);
                mission.Action(1, 1, MissionActionKind.GrantReward, reward: 1);
                migration.InsertData("mission_channel_policy",
                    new[] { "mission_id", "content_revision", "acceptance_channel", "completion_channel", "radio_sources" },
                    new object[]
                    {
                        entry.Id, WildernessMissionDataV1.Revision, (int)MissionChannel.Radio,
                        (int)MissionChannel.Npc,
                        // Channel policies use the loader's default JSON contract, not the scene codec.
                        JsonSerializer.Serialize(new[]
                        {
                            new MissionOfferSourceDefinition(MissionOfferSourceKind.ServerEvent,
                                "class.qualification", 1220, Requirement: family)
                        })
                    });
                mission.Evidence(1, MissionEvidenceSourceKind.Client, $"native:missionobjective/{entry.Id}",
                    "Native class announcement directs the qualified character to Quartermaster Caufield, objective1/package133.");
                mission.Evidence(2, MissionEvidenceSourceKind.Reconstruction, "repository:docs/wilderness-missions.md",
                    "Authoritative existing class-family facts qualify the once-only radio offer. Reconstructed1000 XP/200 credits plus Soldier chaingun/Reflective boots or Specialist repair tool/Hazmat boots fulfill the native gear promise without another progression system.");
                mission.Enable(new MissionSceneDefinition
                {
                    Requirement = requirement,
                    TurnInRequirement = requirement
                });
            }
        }

        private static void LogosDispatches(MigrationBuilder migration)
        {
            foreach (var entry in new[]
            {
                (Id: 1638U, Name: "Enhance", Text: 15767U, Objective: 2U, Label: 15774U,
                    Body: 15775U, Logos: 10U, Experience: 2500U, Credits: 1500U, Prerequisite: false),
                (Id: 1640U, Name: "Area", Text: 15789U, Objective: 4U, Label: 15796U,
                    Body: 15797U, Logos: 1U, Experience: 3000U, Credits: 1500U, Prerequisite: true),
                (Id: 921U, Name: "Projectile", Text: 4915U, Objective: 5U, Label: 4919U,
                    Body: 6696U, Logos: 24U, Experience: 3500U, Credits: 1500U, Prerequisite: true),
                (Id: 907U, Name: "Damage", Text: 4847U, Objective: 1U, Label: 4851U,
                    Body: 6692U, Logos: 6U, Experience: 3000U, Credits: 600U, Prerequisite: true),
                (Id: 909U, Name: "Time", Text: 4857U, Objective: 1U, Label: 4861U,
                    Body: 6301U, Logos: 28U, Experience: 3000U, Credits: 600U, Prerequisite: true),
                (Id: 1639U, Name: "Power", Text: 15776U, Objective: 6U, Label: 15783U,
                    Body: 15784U, Logos: 23U, Experience: 2500U, Credits: 600U, Prerequisite: true),
                (Id: 1633U, Name: "Attack", Text: 15709U, Objective: 3U, Label: 15716U,
                    Body: 15717U, Logos: 2U, Experience: 4000U, Credits: 100U, Prerequisite: false),
                (Id: 911U, Name: "Mind", Text: 4873U, Objective: 3U, Label: 4877U,
                    Body: 6694U, Logos: 56U, Experience: 4000U, Credits: 1500U, Prerequisite: false)
            })
            {
                var mission = new WildernessMissionDataV1(migration, entry.Id, "Logos: " + entry.Name,
                    entry.Text, 133, 133, 5);
                if (entry.Prerequisite)
                    mission.Prerequisite(1069);
                mission.Objective(entry.Objective, entry.Label, entry.Body, 0, MissionObjectiveState.Incomplete);
                mission.Reward(entry.Experience, entry.Credits);
                mission.Transition(entry.Objective);
                mission.Progress(entry.Objective, MissionProgressEventKind.LogosAcquired, entry.Logos);
                mission.Action(entry.Objective, 1, MissionActionKind.GrantReward, reward: 1);
                mission.Evidence(1, MissionEvidenceSourceKind.Client, $"native:missionobjective/{entry.Id}",
                    $"Native objective{entry.Objective} acquires {entry.Name}, Logos{entry.Logos}; use the existing shrine.");
                mission.Evidence(2, MissionEvidenceSourceKind.Reconstruction, "repository:docs/wilderness-missions.md",
                    "Langerman dispatch and sourced XP/credits; already-owned Logos satisfies the objective. No duplicate Logos grant in the mission reward and no invented predecessor.");
                mission.Enable(new MissionSceneDefinition
                {
                    ExistingFactObjectiveIds = new() { entry.Objective }
                });
            }
        }

        private static void ReportToStandley(MigrationBuilder migration)
        {
            var mission = new WildernessMissionDataV1(migration, 1741, "Report to Liaison Standley",
                16980, 133, 134, 5);
            mission.Objective(1, 16985, 16986, 0, MissionObjectiveState.Incomplete);
            mission.Reward(4000, 600);
            mission.Transition(1);
            mission.Conversation(1, 2049);
            mission.Action(1, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1741",
                "Native objective1/package2049 sends Langerman's recruit to the actual Receptive Liaison Standley at Twin Pillars.");
            mission.Evidence(2, MissionEvidenceSourceKind.Reconstruction, "repository:docs/wilderness-missions.md",
                "Sourced4000 XP; reconstructed600 credits match the sourced liaison handoff1742.");
            mission.Enable();
        }

        private static MissionItemBinding DeliveryItem(string key, uint template) =>
            new(key, template, MissionItemScope.AssignmentIssued, 1,
                MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove,
                MissionItemCleanupDisposition.Remove, TurnInQuantity: 1);

        private static MissionSceneDefinition MilpasEscort() => new()
        {
            Actors = new()
            {
                ["milpas"] = new SceneActorDefinition("milpas", SceneActorKind.PublicSpawn, 630010)
            },
            PublicEncounter = new PublicEncounterBinding(1390, 630010, "milpas",
                "wilderness.alia-branch", OwnerLossPolicy: "Fail"),
            Routes = new()
            {
                ["arrest"] = new SceneRoute("arrest", new SceneWaypoint[]
                {
                    new(new ScenePosition(778, 303.32f, 127)),
                    new(new ScenePosition(775.5f, 303.3f, 123.2f)),
                    new(new ScenePosition(771.5f, 302.5f, 121.2f)),
                    new(new ScenePosition(766.3f, 300.3f, 118.8f)),
                    new(new ScenePosition(761.5f, 298.5f, 116.8f)),
                    new(new ScenePosition(759.5f, 297.7f, 116.4f)),
                    new(new ScenePosition(756.3f, 297.3f, 116)),
                    new(new ScenePosition(754.7f, 296.5f, 116)),
                    new(new ScenePosition(753.1f, 294.7f, 117.6f)),
                    new(new ScenePosition(753.1f, 293.9f, 118.8f)),
                    new(new ScenePosition(753.1f, 278.19789f, 158.8f)),
                    new(new ScenePosition(745.1f, 284.5f, 194)),
                    new(new ScenePosition(744.3f, 291.5f, 263.2f)),
                    new(new ScenePosition(744.7f, 291.5f, 265.2f)),
                    new(new ScenePosition(777.5f, 294.5f, 307.2f)),
                    new(new ScenePosition(806.3f, 293.1f, 354.8f)),
                    new(new ScenePosition(836.7f, 293.5f, 409.6f)),
                    new(new ScenePosition(837.5f, 293.5f, 413.2f)),
                    new(new ScenePosition(834.7f, 296.419734f, 443.6f)),
                    new(new ScenePosition(825.9f, 301.5f, 503.2f)),
                    new(new ScenePosition(825, 301.5f, 503.8f))
                }, Speed: 1.65f),
                ["release"] = new SceneRoute("release", new SceneWaypoint[]
                {
                    new(new ScenePosition(778, 303.32f, 127)),
                    new(new ScenePosition(775.5f, 303.3f, 123.2f)),
                    new(new ScenePosition(771.5f, 302.5f, 121.2f)),
                    new(new ScenePosition(766.3f, 300.3f, 118.8f)),
                    new(new ScenePosition(761.5f, 298.5f, 116.8f)),
                    new(new ScenePosition(759.5f, 297.7f, 116.4f)),
                    new(new ScenePosition(756.3f, 297.3f, 116)),
                    new(new ScenePosition(754.7f, 296.5f, 116)),
                    new(new ScenePosition(753.1f, 294.7f, 117.6f)),
                    new(new ScenePosition(753.1f, 293.9f, 118.8f)),
                    new(new ScenePosition(753.9f, 291.824777f, 122.4f)),
                    new(new ScenePosition(768.3f, 280.3f, 145.2f)),
                    new(new ScenePosition(783.9f, 273.5f, 149.2f)),
                    new(new ScenePosition(787.9f, 273.5f, 148.8f)),
                    new(new ScenePosition(801.5f, 273.7f, 144.8f)),
                    new(new ScenePosition(806.7f, 272.9f, 142)),
                    new(new ScenePosition(854.7f, 275.3f, 116)),
                    new(new ScenePosition(889.5f, 281.3f, 97.6f)),
                    new(new ScenePosition(893.5f, 279.3f, 94.4f)),
                    new(new ScenePosition(895.5f, 278.3f, 92.4f)),
                    new(new ScenePosition(901.5f, 273.7f, 79.6f)),
                    new(new ScenePosition(903.1f, 273.9f, 57.6f)),
                    new(new ScenePosition(900.3f, 277.1f, 44)),
                    new(new ScenePosition(899.45f, 276, 39.9f))
                }, Speed: 1.65f)
            }
        };
    }
}
