using System;
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
    public static partial class WildernessDaghdasUrnV1
    {
        public const uint CodexTemplate = 2328;
        public const uint HerbTemplate = 2326;
        public const uint EssenceTemplate = 2355;
        public const uint VaccineTemplate = 2356;
        public const string VaccineItemKey = "corman-vaccines";
        public const uint RecoverySeconds = 900;
        public const uint UltimatumSeconds = 60;
        public const uint HelmetReward = 12887;
        public const uint BootsReward = 12831;
        public const uint VestReward = 12943;
        public const uint LegsReward = 12915;
        public const uint ReflectiveHelmetReward = 13388;
        public const uint PistolReward = 164;
        public const uint ShotgunReward = 166;

        public static void Up(MigrationBuilder migration) => Up(migration, AdoptedBindings());

        public static void Up(MigrationBuilder migration, WildernessDaghdasUrnBindings bindings)
        {
            if (bindings == null)
                throw new ArgumentNullException(nameof(bindings));
            bindings.Validate();
            XanxForTheMemories(migration);
            ChildhoodsEnd(migration, bindings.XanxPositions, bindings.XanxHeadings);
            HerbalRemedy(migration, bindings.HerbObjectClassId, bindings.HerbActionArgId,
                bindings.HerbReadyState, bindings.HerbPositions);
            ElixirVitae(migration);
            TravelingMedicineShow(migration, false);
            TravelingMedicineShow(migration, true);
            OrdersFromHighCommand(migration, bindings.SkeevSpawnId, bindings.OutcomeFlagId);
        }

        public static void Down(MigrationBuilder migration) =>
            WildernessMissionDataV1.Remove(migration, 696, 682, 695, 698, 700, 820, 701);

        public static void XanxForTheMemories(MigrationBuilder migration)
        {
            var mission = new WildernessMissionDataV1(migration, 696, "Xanx For The Memories",
                3071, 102, 102, 15);
            mission.Reward(7500, 1500,
                selectableItems: new (uint, uint)[] { (HelmetReward, 1), (PistolReward, 1) });
            mission.Objective(1, 3099, 6806, 0, MissionObjectiveState.Incomplete);
            mission.Objective(2, 3100, 6807, 1);
            mission.Transition(1);
            mission.Progress(1, MissionProgressEventKind.ItemAcquired, 10520, target: 1);
            mission.RevealAndActivate(1, 2);
            mission.Transition(2);
            mission.Conversation(2, 590);
            mission.Action(2, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Indicator(1, 1, 125.37109, 246.76562, 733.0625);
            mission.Indicator(2, 2, -624.85156, 276.21484, 850.21094);
            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/696",
                "Native1 recovers the Codex of Voynich, class10520/template2328;2 reports to Juvak/package590.");
            mission.Evidence(2, MissionEvidenceSourceKind.Reconstruction, "repository:docs/wilderness-missions.md",
                "Arioch77/spawn158 remains an outdoor target. One personal Codex drop, actual pickup and one turn-in cost;7500XP is five times sourced1500 credits. Level15 and base Hazmat Helmet12887/Pistol164 replace unsupported Olympia/Vextronics modifiers.");
            mission.Enable(new MissionSceneDefinition
            {
                ExistingFactObjectiveIds = new() { 1 },
                Items = new()
                {
                    new MissionItemBinding("codex", CodexTemplate, MissionItemScope.CharacterOwned, 1,
                        MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                        MissionItemCleanupDisposition.Retain, TurnInQuantity: 1,
                        Drop: new MissionItemDropDefinition(new uint[] { 77 }, 1, 1220, 100, 1))
                }
            });
        }

        public static void ChildhoodsEnd(MigrationBuilder migration, IReadOnlyList<ScenePosition> xanxPositions,
            IReadOnlyList<double> xanxHeadings = null)
        {
            var scene = RangerEncounter(xanxPositions, xanxHeadings);
            var mission = new WildernessMissionDataV1(migration, 682, "Childhood's End",
                2912, 91, 92, 13);
            mission.Reward(10000, 1500,
                selectableItems: new (uint, uint)[] { (BootsReward, 1), (ShotgunReward, 1) });
            mission.Prerequisite(451);
            mission.Objective(2, 2918, 6654, 0, MissionObjectiveState.Incomplete);
            mission.Objective(3, 2938, 6655, 1, counterTextId: 2939);
            mission.Objective(5, 2940, 6656, 2);
            mission.Objective(4, 2942, 6657, 3);
            mission.Objective(6, 2944, 6658, 4);
            mission.Scenario(1, "Defend Anjuhi from three Xanx");
            mission.Scenario(2, "Distinct encounter Xanx defeats");
            ScenarioEvents(migration, 682, 2, 1, 2, 3);
            mission.Transition(2);
            mission.Conversation(2, 102);
            mission.RevealAndActivate(2, 3);
            mission.Action(2, 3, MissionActionKind.StartScenario, scenario: 1);
            mission.Transition(3);
            for (var eventId = 1U; eventId <= 3; eventId++)
                mission.Progress(3, MissionProgressEventKind.ScenarioEvent, eventId,
                    counter: 2, target: 3, trigger: eventId);
            mission.RevealAndActivate(3, 5);
            mission.Transition(5);
            mission.Conversation(5, 102);
            mission.RevealAndActivate(5, 4);
            mission.Transition(4);
            mission.Conversation(4, 570);
            mission.RevealAndActivate(4, 6);
            mission.Transition(6);
            mission.Conversation(6, 566);
            mission.Action(6, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Indicator(2, 1, -474.76953, 193.64453, 519.91016);
            mission.Indicator(3, 2, -474.76953, 193.64453, 519.91016, 20);
            mission.Indicator(5, 3, -474.76953, 193.64453, 519.91016);
            mission.Indicator(4, 4, -495.22656, 216.10156, 531.72266);
            mission.Indicator(6, 5, -790.9258, 263.6289, 740.5039);
            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/682",
                "Native order2>3>5>4>6: Anjuhi102, attack, Anjuhi102, Tirna570, Doyan566.");
            mission.Evidence(2, MissionEvidenceSourceKind.Reconstruction, "repository:docs/wilderness-missions.md",
                "Three distinct scene-owned Xanx87 replace the missing native attack count. Ordinary public Xanx deaths do not count. Reserve the existing public Anjuhi176; release after the attack. Level13 and base Hazmat Boots12831/Shotgun166 replace Luminar/Vextronics modifiers.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "coordinator:w5-w7-bindings.json/xanx682",
                "Adopted three exact grounded camp-front roots and headings for existing87/class7510 near the real Anjuhi. These are bounded local reconstruction, not recovered retail spawn poses.");
            mission.Enable(scene);
        }

        public static void HerbalRemedy(MigrationBuilder migration, uint herbObjectClassId, uint herbActionArgId,
            uint herbReadyState, IReadOnlyList<ScenePosition> herbPositions)
        {
            var scene = Herbs(herbObjectClassId, herbActionArgId, herbReadyState, herbPositions);
            var mission = new WildernessMissionDataV1(migration, 695, "Herbal Remedy",
                3022, 92, 113, 15);
            mission.Reward(22000, 2200,
                selectableItems: new (uint, uint)[] { (HelmetReward, 1), (VestReward, 1) });
            mission.Prerequisite(682);
            mission.Objective(4, 3029, 6674, 0, MissionObjectiveState.Incomplete);
            mission.Objective(5, 3036, 6675, 1, counterTextId: 3037);
            mission.Objective(1, 3026, 6676, 2);
            mission.Objective(6, 19785, 19786, 3, required: false);
            mission.Objective(7, 19787, 19788, 4, required: false);
            mission.Objective(8, 19789, 19790, 5, required: false);
            mission.Scenario(1, "Gather Tinctu herbs after consulting Gadfly");
            mission.Scenario(2, "All five Tinctu herbs recovered");
            ScenarioEvents(migration, 695, 2, 1);
            mission.Transition(4);
            mission.Conversation(4, 577);
            var action = 1U;
            foreach (var objective in new uint[] { 5, 1, 6, 7, 8 })
            {
                mission.RevealAndActivate(4, objective, action);
                action += 2;
            }
            mission.Action(4, action, MissionActionKind.StartScenario, scenario: 1);
            mission.Transition(1);
            mission.Progress(1, MissionProgressEventKind.ScenarioEvent, 1, counter: 2);
            mission.Action(1, 1, MissionActionKind.GrantReward, reward: 1);
            foreach (var (objective, creature) in new (uint, uint)[] { (6, 80), (7, 79), (8, 75) })
            {
                mission.Transition(objective);
                mission.Progress(objective, MissionProgressEventKind.CreatureKilled, creature);
            }
            mission.Indicator(4, 1, -542.2578, 278.86328, 884.9219);
            mission.Indicator(5, 2, 615, 284.2578, 737, 35);
            var indicator = 3U;
            foreach (var actor in scene.Actors.Values)
                mission.Indicator(1, indicator++,
                    actor.Position.X, actor.Position.Y, actor.Position.Z);
            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/695",
                "Gadfly577 precedes native objectives5/1 and hidden6/7/8. Require all three named Devils and five physical Tinctu herbs, template2326/class10513.");
            mission.Evidence(2, MissionEvidenceSourceKind.Reconstruction, "repository:docs/wilderness-missions.md",
                "Hidden6=Old Scratch80,7=Horntail79,8=Archfiend75 in source listing order. Five assignment-owned herb pickups use the centrally approved World bindings and existing receipt/cost contracts. Level15 and base Hazmat Helmet12887/Vest12943 replace Olympia modifiers.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "coordinator:w5-w7-bindings.json/tinctu695",
                "Explicit approved deprecated-asset reconstruction: ItemDispenser10512 uses native action80/arg3 and states81/82 at five exact dry-shore roots. Inactive82 stops targeting; no automatic mesh disappearance is assumed.");
            mission.Evidence(4, MissionEvidenceSourceKind.Reconstruction, "native:missionobjective/695/1",
                "Herb objective1 has no native counter text. One counterless scene result completes it only after five distinct real herb issuances; five held items are still consumed at turn-in. Devil objective5 retains native counter3037 and hidden6/7/8.");
            mission.Enable(scene);
        }

        public static void ElixirVitae(MigrationBuilder migration)
        {
            var mission = new WildernessMissionDataV1(migration, 698, "Elixir Vitae",
                3115, 113, 94, 15);
            mission.Reward(11000, 550,
                selectableItems: new (uint, uint)[] { (BootsReward, 1), (VestReward, 1) });
            mission.Prerequisite(695);
            mission.Objective(1, 3130, 6667, 0, MissionObjectiveState.Incomplete);
            mission.Transition(1);
            mission.Conversation(1, 117);
            mission.Action(1, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Indicator(1, 1, -755.9961, 170.97656, -277.9961);
            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/698",
                "Gadfly gives the essence for Eleanor/package117. Use Tinctu essence2355/class10608, not similar Tincta class10518.");
            mission.Evidence(2, MissionEvidenceSourceKind.Reconstruction, "repository:docs/wilderness-missions.md",
                "One assignment-owned essence is issued on acceptance and consumed at turn-in. Failure/abandonment clean only this assignment. Level15 and base Hazmat Boots12831/Vest12943 replace Olympia/Luminar modifiers.");
            mission.Enable(new MissionSceneDefinition
            {
                Items = new() { IssuedItem("tinctu-essence", EssenceTemplate, 1, 1) },
                AcceptanceItems = new()
                {
                    new IssueMissionItemIntent("issue-tinctu-essence", 698, "tinctu-essence", EssenceTemplate, 1)
                }
            });
        }

        public static void TravelingMedicineShow(MigrationBuilder migration, bool replacement)
        {
            var id = replacement ? 820U : 700U;
            var mission = new WildernessMissionDataV1(migration, id, "Traveling Medicine Show",
                replacement ? 4080U : 3135U, 94, 94, 15);
            mission.Reward(11000, 550,
                selectableItems: new (uint, uint)[] { (ReflectiveHelmetReward, 1), (LegsReward, 1) });
            mission.Prerequisite(698);
            var names = replacement ? new uint[] { 4084, 4086, 4088 } : new uint[] { 3163, 3165, 3167 };
            var bodies = replacement ? new uint[] { 6783, 6784, 6785 } : new uint[] { 6769, 6770, 6771 };
            var packages = new uint[] { 218, 130, 602 };
            for (var index = 0; index < 3; index++)
            {
                var objective = (uint)index + 1;
                mission.Objective(objective, names[index], bodies[index], (uint)index, MissionObjectiveState.Incomplete);
                mission.Scenario(objective, $"Record vaccine delivered to package{packages[index]}");
                mission.Transition(objective);
                mission.Conversation(objective, packages[index]);
                mission.Action(objective, 1, MissionActionKind.ConsumeMissionItem,
                    itemIntent: new ConsumeMissionItemIntent($"vaccinate-{packages[index]}", id,
                        VaccineItemKey, 1, MissionItemScope.AssignmentIssued));
                mission.Action(objective, 2, MissionActionKind.StartScenario, scenario: objective);
            }
            mission.Objective(5, replacement ? 4090U : 3171U, replacement ? 6786U : 6772U, 3,
                counterTextId: replacement ? 4091U : 3172U);
            mission.Objective(6, replacement ? 4093U : 4079U, replacement ? 6787U : 6773U, 4, required: false);
            mission.Scenario(5, "Report the completed vaccination round to Eleanor");
            mission.Transition(5);
            mission.Conversation(5, 117);
            mission.Action(5, 1, MissionActionKind.StartScenario, scenario: 5);
            mission.Action(5, 2, MissionActionKind.GrantReward, reward: 1);
            if (replacement)
            {
                mission.Objective(7, 4099, 6788, 5, MissionObjectiveState.Incomplete);
                mission.Transition(7, result: MissionObjectiveState.Failed);
                Timer(migration, id, 7, RecoverySeconds);
                mission.Transition(7, 2, sequence: 2);
                ObjectiveState(migration, id, 7, 5, MissionObjectiveState.Completed, 2);
                migration.InsertData("mission_repeat_policy",
                    new[] { "mission_id", "content_revision", "repeat_kind", "cooldown_seconds", "reset_second_utc" },
                    new object[] { id, WildernessMissionDataV1.Revision, (int)MissionRepeatKind.Immediate, null, null });
            }
            mission.Indicator(1, 1, -127.99609, 220.76172, -466.9961);
            mission.Indicator(2, 2, 810.9, 294.71, 391.7);
            mission.Indicator(3, 3, -603.9961, 276.61328, 892);
            mission.Indicator(5, 4, -755.9961, 170.97656, -277.9961);
            mission.Evidence(1, MissionEvidenceSourceKind.Client, $"native:missionobjective/{id}",
                replacement
                    ? "820 opening4082 explicitly supplies another batch; objective7/name4099 is timed. Recipients218/130/602 and Eleanor117 retain the replacement text identities."
                    : "700 opening3137 supplies the first batch; there is no native timed objective. Deliver one vaccine to each of218/130/602, then report to Eleanor117.");
            mission.Evidence(2, MissionEvidenceSourceKind.Reconstruction, "repository:docs/wilderness-missions.md",
                replacement
                    ? "Recovery after failed700;900 seconds is reconstructed because no native duration survives. Retry owns a fresh batch/deadline;700 and820 cannot both pay."
                    : "701 interrupts the initial batch by authorized radio while its assignment-owned vaccines are carried. Surrender fails700 and permits820; refusal preserves the batch.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:docs/wilderness-missions.md",
                "Three assignment-owned2356/class10611 vaccines, distinct recipient costs and a single11000XP/550-credit payout across variants. Level15 and base Reflective Helmet13388/Hazmat Legs12915 replace Teleract/Olympia modifiers.");
            mission.Enable(Vaccines(id));
        }

        public static void OrdersFromHighCommand(MigrationBuilder migration, uint skeevSpawnId, uint outcomeFlagId)
        {
            var scene = Finale(skeevSpawnId, outcomeFlagId);
            var mission = new WildernessMissionDataV1(migration, 701, "Orders From High Command",
                3159, null, 510002, 15);
            mission.Reward(22000, 2200);
            mission.Objective(1, 3173, 6713, 0, MissionObjectiveState.Incomplete);
            mission.Objective(7, 3322, 6714, 1, required: false);
            mission.Objective(2, 3178, 6715, 2);
            mission.Objective(3, 3183, 6716, 3, required: false);
            mission.Objective(8, 3341, 6718, 4);
            mission.Objective(6, 3243, 6719, 5);
            mission.Objective(5, 3186, 6717, 6, required: false);
            for (var sequence = 1U; sequence <= 5; sequence++)
                mission.Scenario(sequence, $"Skeev encounter sequence{sequence}");
            ScenarioEvents(migration, 701, 5, 1);
            mission.Transition(1);
            mission.Conversation(1, 609);
            mission.RevealAndActivate(1, 7);
            mission.RevealAndActivate(1, 2, 3);
            mission.Action(1, 5, MissionActionKind.StartScenario, scenario: 1);
            mission.Transition(2);
            mission.Conversation(2, 595);
            mission.Action(2, 1, MissionActionKind.FailRelatedMission,
                itemIntent: new FailRelatedMissionIntent("surrender-original-vaccines", 700));
            mission.Action(2, 2, MissionActionKind.StartScenario, scenario: 2);
            mission.Transition(2, 2, sequence: 2);
            mission.Conversation(2, 595, transition: 2);
            mission.RevealAndActivate(2, 3, transition: 2);
            mission.Action(2, 3, MissionActionKind.StartScenario, scenario: 3, transition: 2);
            mission.Transition(7);
            Timer(migration, 701, 7, UltimatumSeconds);
            mission.Action(7, 1, MissionActionKind.StartScenario, scenario: 4);
            mission.Transition(7, 2, sequence: 2);
            ObjectiveState(migration, 701, 7, 2, MissionObjectiveState.Completed, 2);
            mission.Transition(3);
            mission.Progress(3, MissionProgressEventKind.ScenarioEvent, 1, counter: 5);
            mission.RevealAndActivate(3, 8);
            var transition = 1U;
            foreach (var flag in new uint[] { 1, 26, 27 })
            {
                mission.Transition(8, transition, sequence: transition);
                mission.Conversation(8, 609, flag, transition);
                mission.RevealAndActivate(8, 6, transition: transition);
                transition++;
            }
            mission.Transition(6);
            mission.Conversation(6, 593);
            mission.Action(6, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Indicator(1, 1, -395.91797, 174.32031, 181.73047);
            mission.Indicator(8, 2, -395.91797, 174.32031, 181.73047);
            mission.Indicator(6, 3, 812.9, 294.69, 388.2);
            mission.Indicator(2, 4, -400, 173.679004, 178);
            mission.Indicator(3, 5, -400, 173.679004, 178);
            migration.InsertData("mission_channel_policy",
                new[] { "mission_id", "content_revision", "acceptance_channel", "completion_channel", "radio_sources" },
                new object[]
                {
                    701U, WildernessMissionDataV1.Revision, (int)MissionChannel.Radio, (int)MissionChannel.Npc,
                    JsonSerializer.Serialize(new[]
                    {
                        new MissionOfferSourceDefinition(MissionOfferSourceKind.Scene,
                            "wilderness.corman-first-batch", 1220, Requirement: FirstBatchCarried())
                    })
                });
            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:objectiveconversation/701",
                "Real radio offer; Burke609, Skeev595 choice1/2 with empty3182, Burke objective8 flags1/26/27, Beacham593 turn-in. Native ultimatum60 seconds.");
            mission.Evidence(2, MissionEvidenceSourceKind.Reconstruction, "repository:docs/wilderness-missions.md",
                "Only the original carried700 batch qualifies. Surrender consumes its remaining stock via atomic failure/cleanup and unlocks820. Refusal and timeout fight Skeev without surrender. Burke1=refusal victory,26=surrender,27=timeout victory; level15 reconstructed.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "coordinator:w5-w7-bindings.json/skeev701",
                "Canonical public Skeev530130 retains native class28589/name6734/package595 at(-400,173.679004,178), heading-1.19028995 near Burke. Storage flag530003 is centrally allocated; native Burke topic flags remain1/26/27.");
            mission.Enable(scene);
        }

        private static MissionItemBinding IssuedItem(string key, uint template, uint maximum, uint turnIn = 0) =>
            new(key, template, MissionItemScope.AssignmentIssued, maximum,
                MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove,
                MissionItemCleanupDisposition.Remove, TurnInQuantity: turnIn);

        private static MissionRequirement NoMedicinePayout() => new NotRequirement(
            new AnyRequirement(new MissionRequirement[]
            {
                new MissionStateRequirement(700, MissionState.Completed),
                new MissionStateRequirement(820, MissionState.Completed)
            }));

        private static void ScenarioEvents(MigrationBuilder migration, uint mission, uint scenario, params uint[] events)
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
                        mission, WildernessMissionDataV1.Revision, scenario, eventId,
                        (byte)MissionScenarioStepKind.EmitScenarioEvent, eventId,
                        (byte)MissionContentRequirement.Required, eventId, "Authored W7 scene result"
                    });
        }

        private static void Timer(MigrationBuilder migration, uint mission, uint objective, uint seconds) =>
            migration.InsertData("mission_trigger",
                new[]
                {
                    "mission_id", "content_revision", "requirement", "objective_id", "transition_id",
                    "trigger_id", "kind", "sequence", "duration_seconds", "comment"
                },
                new object[]
                {
                    mission, WildernessMissionDataV1.Revision, (byte)MissionContentRequirement.Required,
                    objective, 1U, 1U, (byte)MissionTriggerKind.TimerElapsed, 1U, seconds, "Durable native objective deadline"
                });

        private static void ObjectiveState(MigrationBuilder migration, uint mission, uint objective,
            uint related, MissionObjectiveState state, uint transition) =>
            migration.InsertData("mission_trigger",
                new[]
                {
                    "mission_id", "content_revision", "requirement", "objective_id", "transition_id",
                    "trigger_id", "kind", "sequence", "related_objective_id", "related_state", "comment"
                },
                new object[]
                {
                    mission, WildernessMissionDataV1.Revision, (byte)MissionContentRequirement.Required,
                    objective, transition, 1U, (byte)MissionTriggerKind.ObjectiveState, 1U, related, (byte)state,
                    "Stop the deadline after its required work"
                });
    }
}
