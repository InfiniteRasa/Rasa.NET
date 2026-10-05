using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Wilderness
{
    public static class WildernessElohPinholeV1
    {
        private const uint HarvesterIntactState = 110;
        private const uint DestroyedState = 2;
        private const uint SurveyReadyState = 55;
        private const uint SurveyCollectedState = 56;
        private const uint EmanatorReadyState = 119;
        private const uint EmanatorPlacedState = 121;

        private static readonly ScenePosition[] Harvesters =
        {
            new(347, 233.380158210f, 178),
            new(338, 230.731414425f, 251),
            new(313.5f, 233.363021230f, 339)
        };

        private static readonly ScenePosition[] Surveys =
        {
            new(442.5f, 203.9f, 561.3f),
            new(332.2f, 203.9f, 522.7f),
            new(318.6f, 212.5f, 555.2f),
            new(364.7f, 196.9f, 582.3f),
            new(315.3f, 196.2f, 571.1f)
        };

        public static void Up(MigrationBuilder migration)
        {
            RiverRecon(migration, new ScenePosition(309.512878f, 271.17935f, 436.969f), 6, 2);
            FieldReports(migration);
            SnipeHunt(migration, 630046, new ScenePosition[]
            {
                new(243.340721f, 227.445074f, 241.396381f),
                new(255.207849f, 230.380794f, 263.403659f),
                new(345.332512f, 230.320493f, 177.36105f),
                new(327.942408f, 233.363417f, 184.94948f),
                new(265.933766f, 239.813626f, 297.786076f),
                new(259.940889f, 230.19304f, 246.037072f)
            });
            GasHarvesters(migration);
            Rendezvous(migration);
            MamaMiasma(migration, 630040, new ScenePosition[]
            {
                new(330, 203.8f, 526),
                new(338.5f, 213.1f, 552),
                new(364, 212, 535)
            });
            TroubleWithTreebacks(migration, new ScenePosition(460, 288.54892f, 589));
            SurveySays(migration);
        }

        public static void Down(MigrationBuilder migration) =>
            WildernessMissionDataV1.Remove(migration, 429, 431, 433, 432, 434, 506, 436, 508);

        private static void RiverRecon(MigrationBuilder migration, ScenePosition position, double radius, double height)
        {
            var mission = new WildernessMissionDataV1(migration, 429, "River Recon",
                741, 100, 101, 5);
            mission.Prerequisite(479);
            mission.Objective(5, 3885, 6726, 0, MissionObjectiveState.Incomplete);
            mission.Objective(4, 1971, 6727, 1);
            mission.Reward(10000, 1000, selectableItems: new (uint, uint)[] { (630, 1), (3315, 1) });
            mission.Area(5, position.X, position.Y, position.Z, radius, height);
            mission.Transition(5);
            mission.EnterArea(5, 5);
            mission.RevealAndActivate(5, 4);
            mission.Transition(4);
            mission.Conversation(4, 208);
            mission.Action(4, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Indicator(5, 1, position.X, position.Y, position.Z, radius);
            mission.Indicator(4, 2, 505, 238.757, 223);
            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/429",
                "Native5 reconnoiters the lost Forean patrol at the top of Pinhole Falls before4 reports to Witherspoon208; Rogers116 only reminds.");
            mission.Evidence(2, MissionEvidenceSourceKind.Reconstruction, "repository:docs/wilderness-missions.md",
                "Requires479. A6m-radius/2m-halfheight volume on native waterfall rock3590 replaces the ambiguous dying-patrol/package726 conversation without inventing an NPC or counting the pond/caverns below. Witherspoon uses the approved creek-side correction.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:docs/wilderness-missions.md",
                "Historical10000XP/1000credits; existing base Healing Disc630 and Rifle3315 omit unsupported Astra/Vitalius prefixes. Level5 is reconstructed.");
            mission.Enable(new MissionSceneDefinition
            {
                Dialogue = new()
                {
                    new MissionDialogueTopicDefinition(4, 116, 1, MissionDialogueKind.Ambient)
                }
            });
        }

        private static void FieldReports(MigrationBuilder migration)
        {
            var mission = new WildernessMissionDataV1(migration, 431, "Distress On The River",
                749, 101, 101, 6);
            mission.Prerequisite(429);
            mission.Objective(1, 2472, 6663, 0, MissionObjectiveState.Incomplete);
            mission.Objective(2, 2474, 6664, 1);
            mission.Objective(3, 2476, 6665, 2);
            mission.Reward(2500, 500, selectableItems: new (uint, uint)[] { (20548, 1), (4019, 1) });
            mission.Transition(1);
            mission.Conversation(1, 251);
            mission.RevealAndActivate(1, 2);
            mission.Transition(2);
            mission.Conversation(2, 252);
            mission.RevealAndActivate(2, 3);
            mission.Transition(3);
            mission.Conversation(3, 253);
            mission.Action(3, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Indicator(1, 1, 297.02734, 228.3125, 471.21875);
            mission.Indicator(2, 2, 413.91797, 242.11328, 388.64062);
            mission.Indicator(3, 3, 389.1289, 233.21094, 260.90625);
            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/431",
                "Hugh Corman251, Oingin252 and Wood253 supply three ordered reports; Witherspoon receives the debrief.");
            mission.Evidence(2, MissionEvidenceSourceKind.Reconstruction, "repository:docs/wilderness-missions.md",
                "River Recon precedes the reports. Level6 and existing base Hazmat Helmet20548/Chaingun4019 replace unsupported historical loot prefixes.");
            mission.Enable();
        }

        private static void SnipeHunt(MigrationBuilder migration, uint sniperCreatureId, ScenePosition[] positions)
        {
            var mission = new WildernessMissionDataV1(migration, 433, "Snipe Hunt",
                757, 101, 101, 6);
            mission.Prerequisite(431);
            mission.Objective(1, 2579, 6700, 0, MissionObjectiveState.Incomplete, counterTextId: 2580);
            mission.Objective(7, 2584, 6701, 1, MissionObjectiveState.Incomplete, counterTextId: 2586);
            SecondaryCounterText(migration, 433, 7, 2585);
            mission.Reward(6000, 900, selectableItems: new (uint, uint)[] { (20399, 1), (3603, 1) });
            mission.Transition(1);
            mission.Progress(1, MissionProgressEventKind.CreatureKilled, sniperCreatureId, counter: 0, target: 6);
            mission.Transition(7);
            mission.Progress(7, MissionProgressEventKind.ItemAcquired, 10211, target: 1);
            mission.Action(7, 1, MissionActionKind.GrantReward, reward: 1);
            for (var index = 0; index < positions.Length; index++)
                mission.Indicator(1, (uint)index + 1, positions[index].X, positions[index].Y, positions[index].Z);
            mission.Indicator(7, 10, 238.35938, 227.17578, 246.73828);
            mission.Indicator(7, 11, 266.72266, 233.74219, 347.1172);
            mission.Indicator(7, 12, 130.09766, 233.0039, 437.28906);
            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missiontext/758/759/6701",
                "Six snipers and one physically retrieved datapad. Native text explicitly permits Glognar82, Phlegg83 or Rankash84 despite the Glognar-only counter label.");
            mission.Evidence(2, MissionEvidenceSourceKind.Reconstruction, "repository:docs/wilderness-missions.md",
                $"Agent reconstruction: sniper template{sniperCreatureId}, native body7120/name406/weapon7119, dedicated server action630052 using compatible native1/149 with60m range, and six qualified pillbox/ridge poses. This is not a recovered retail class/stat association; ordinary Lightbender630071 is ineligible.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:docs/wilderness-missions.md",
                "Existing datapad2239/class10211 drops from one eligible overseer and costs one item at turn-in. Historical6000XP/900credits; base Hazmat Gloves20399 and Shotgun3603 omit unsupported Olympia/ChiTech prefixes. Level6 is reconstructed.");
            mission.Enable(new MissionSceneDefinition
            {
                Items = new()
                {
                    new MissionItemBinding("overseer-datapad", 2239, MissionItemScope.CharacterOwned, 1,
                        MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                        MissionItemCleanupDisposition.Retain, TurnInQuantity: 1,
                        Drop: new MissionItemDropDefinition(creatureIds: new uint[] { 82, 83, 84 },
                            objectiveId: 7, mapContextId: 1220, chancePercent: 100, quantity: 1))
                }
            });
        }

        private static void GasHarvesters(MigrationBuilder migration)
        {
            var mission = new WildernessMissionDataV1(migration, 432, "Hitting 'em Where It Hurts",
                753, 135, 135, 6);
            mission.Objective(1, 1105, 6678, 0, MissionObjectiveState.Incomplete,
                counterTextId: 20000008);
            mission.Reward(3000, 600, new (uint, uint)[] { (44917, 1), (111022, 1) });
            mission.Scenario(1, "Destroy each gas harvester");
            DeclareSceneEvents(migration, 432, 1, 2, 3);
            mission.Transition(1);
            mission.Action(1, 1, MissionActionKind.GrantReward, reward: 1);
            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                Sequences = new() { [0] = new SceneSequenceDefinition() }
            };
            for (var index = 0; index < Harvesters.Length; index++)
            {
                var eventId = (uint)index + 1;
                var role = $"gas-harvester-{eventId}";
                var position = Harvesters[index];
                mission.Progress(1, MissionProgressEventKind.ScenarioEvent, eventId,
                    counter: 1, target: 3, trigger: eventId);
                mission.Indicator(1, eventId, position.X, position.Y, position.Z);
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Object, 7906,
                    position, InitialObjectState: HarvesterIntactState,
                    Destruction: new SceneObjectDestruction(432, 1, eventId, HitPoints: 100));
                scene.Sequences[0].World.Add(new EnsureActorIntent($"introduce-{role}", role));
                scene.Sequences[eventId] = new SceneSequenceDefinition
                {
                    World = new()
                    {
                        new TransitionObjectStateIntent($"destroy-{role}", role, DestroyedState),
                        new SetInteractionIntent($"disable-{role}", role, false)
                    },
                    Signals = new() { new SceneMissionSignal(432, 1, eventId) }
                };
            }
            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/432",
                "Native objective1 requires three destroyed harvesters; class7906 is an InertDestroyable, not a use-button objective.");
            mission.Evidence(2, MissionEvidenceSourceKind.Reconstruction,
                "https://tabularasa.fandom.com/wiki/Hitting_%27em_Where_It_Hurts",
                "Native class7906 roots shift5.408m/1m/7.762m from guide XZ onto measured dry footprints. RootY is physical support+2.974437236785889+0.1, not an NPC foot position. Heading0, level6 and100HP are reconstructed; each role counts once.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:docs/wilderness-missions.md",
                "Historical3000XP/600credits with existing Class I Basic Med Pack44917 and EMP Grenade111022, one each.");
            mission.Enable(scene);
        }

        private static void Rendezvous(MigrationBuilder migration)
        {
            var mission = new WildernessMissionDataV1(migration, 434, "Rendezvous At The LZ",
                761, 101, 130, 6);
            mission.Prerequisite(432);
            mission.Objective(1, 1055, 6730, 0, MissionObjectiveState.Incomplete);
            mission.Reward(3000, 600, new (uint, uint)[] { (44918, 1), (111247, 1) });
            mission.Transition(1);
            mission.Conversation(1, 212);
            mission.Action(1, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Indicator(1, 1, 165, 163.0039, -99.99219);
            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:objectiveconversation/434",
                "Witherspoon208 sends the player to Randolph212. Native dialogue takes precedence over the contradictory Jennings infobox.");
            mission.Evidence(2, MissionEvidenceSourceKind.Reconstruction, "repository:docs/wilderness-missions.md",
                "Historical prerequisite432 and rewards3000XP/600credits; existing Class I Standard Med Pack44918 and EMP Bomb111247, one each. Level6 is reconstructed.");
            mission.Enable(new MissionSceneDefinition
            {
                Dialogue = new()
                {
                    new MissionDialogueTopicDefinition(1, 208, 1, MissionDialogueKind.Ambient)
                }
            });
        }

        private static void MamaMiasma(MigrationBuilder migration, uint eggLayerCreatureId, ScenePosition[] positions)
        {
            var mission = new WildernessMissionDataV1(migration, 506, "Mama Miasma!",
                1289, 126, 126, 6);
            mission.Prerequisite(422);
            mission.Objective(1, 1293, 6706, 0, MissionObjectiveState.Incomplete, counterTextId: 2190);
            mission.Reward(5000, 750, selectableItems: new (uint, uint)[] { (35784, 1), (20250, 1) });
            mission.Transition(1);
            mission.Progress(1, MissionProgressEventKind.CreatureKilled, eggLayerCreatureId, counter: 0, target: 3);
            mission.Action(1, 1, MissionActionKind.GrantReward, reward: 1);
            for (var index = 0; index < positions.Length; index++)
                mission.Indicator(1, (uint)index + 1, positions[index].X, positions[index].Y, positions[index].Z);
            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missiontext/1290/1293/6706",
                "Three larger female egg-layers inside Pinhole Falls Caverns, not the older guide's five and not ordinary Bog Miasma88.");
            mission.Evidence(2, MissionEvidenceSourceKind.Reconstruction, "repository:docs/wilderness-missions.md",
                $"Approved World egg-layer template{eggLayerCreatureId}, native class10240/name0 and connected interior floors supply the missing population. Historical5000XP/750credits; level6 and base Reflective Helmet35784/Hazmat Boots20250 omit unsupported Pulsar/Olympia prefixes.");
            mission.Enable();
        }

        private static void TroubleWithTreebacks(MigrationBuilder migration, ScenePosition position)
        {
            var mission = new WildernessMissionDataV1(migration, 436, "The Trouble With Treebacks",
                769, 126, 126, 6);
            mission.Prerequisite(506);
            mission.Objective(1, 1294, 6661, 0, MissionObjectiveState.Incomplete);
            SecondaryCounterText(migration, 436, 1, 1538);
            mission.Reward(2500, 500, new (uint, uint)[] { (44919, 1), (111022, 1) });
            mission.Scenario(1, "Place the sonic emanator without harming the herd");
            DeclareSceneEvents(migration, 436, 1);
            mission.Transition(1);
            mission.Progress(1, MissionProgressEventKind.ScenarioEvent, 1, counter: 1);
            mission.Action(1, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Indicator(1, 1, position.X, position.Y, position.Z);
            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missiontext/770/771/6661",
                "Place and activate the Sonic Emanator among the Treebacks north of the falls. This is explicitly nonlethal; creature deaths cannot advance the objective.");
            mission.Evidence(2, MissionEvidenceSourceKind.Reconstruction, "repository:docs/wilderness-missions.md",
                "Approved herd-side position uses native ItemAcceptor10127/states119->121. Assignment-issued item2230/class10128 is consumed once on placement and cleaned up on every terminal path.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:docs/wilderness-missions.md",
                "Historical2500XP/500credits and existing Class I Advanced Med Pack44919/EMP Grenade111022, one each. Level6 and heading0 are reconstructed.");
            mission.Enable(new MissionSceneDefinition
            {
                Script = "data.sequence",
                Items = new()
                {
                    new MissionItemBinding("sonic-emanator", 2230, MissionItemScope.AssignmentIssued, 1,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove,
                        MissionItemCleanupDisposition.Remove)
                },
                AcceptanceItems = new()
                {
                    new IssueMissionItemIntent("issue-sonic-emanator", 436, "sonic-emanator", 2230, 1)
                },
                Actors = new()
                {
                    ["sonic-emanator"] = new SceneActorDefinition("sonic-emanator", SceneActorKind.Object, 10127,
                        position, InitialObjectState: EmanatorReadyState,
                        UseAction: new SceneObjectAction(436, 1, 1))
                },
                Sequences = new()
                {
                    [0] = new SceneSequenceDefinition
                    {
                        World = new() { new EnsureActorIntent("introduce-sonic-emanator", "sonic-emanator") }
                    },
                    [1] = new SceneSequenceDefinition
                    {
                        World = new()
                        {
                            new TransitionObjectStateIntent("activate-sonic-emanator", "sonic-emanator", EmanatorPlacedState),
                            new SetInteractionIntent("disable-sonic-emanator", "sonic-emanator", false)
                        },
                        Character = new()
                        {
                            new ConsumeMissionItemIntent("place-sonic-emanator", 436, "sonic-emanator", 1,
                                MissionItemScope.AssignmentIssued)
                        },
                        Signals = new() { new SceneMissionSignal(436, 1, 1) }
                    }
                }
            });
        }

        private static void SurveySays(MigrationBuilder migration)
        {
            var mission = new WildernessMissionDataV1(migration, 508, "Survey Says",
                1300, 127, 100, 6);
            mission.Prerequisite(506);
            for (var index = 0U; index < 5; index++)
                mission.Objective(index + 2, index + 1310, index + 6753, index, MissionObjectiveState.Incomplete);
            mission.Objective(7, 2219, 6752, 5, MissionObjectiveState.Incomplete, counterTextId: 2220);
            mission.Objective(8, 2240, 6758, 6);
            mission.Reward(5000, 750, selectableItems: new (uint, uint)[] { (20697, 1), (35933, 1) });
            mission.Scenario(1, "Retrieve each survey data disk");
            DeclareSceneEvents(migration, 508, 2, 3, 4, 5, 6);
            mission.Transition(8);
            mission.Conversation(8, 116);
            mission.Action(8, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Indicator(8, 6, 870, 294.21, 385.5);
            var scene = new MissionSceneDefinition
            {
                Script = "wilderness.eloh-pinhole-survey",
                ObjectiveAggregations = new()
                {
                    [7] = new MissionObjectiveAggregation(new uint[] { 2, 3, 4, 5, 6 }, 5, 0)
                },
                Items = new()
                {
                    new MissionItemBinding("survey-data", 741, MissionItemScope.AssignmentIssued, 5,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove,
                        MissionItemCleanupDisposition.Remove, TurnInQuantity: 5)
                },
                Dialogue = new()
                {
                    new MissionDialogueTopicDefinition(7, 214, 1, MissionDialogueKind.Ambient),
                    new MissionDialogueTopicDefinition(8, 214, 1, MissionDialogueKind.Ambient)
                },
                Sequences = new()
                {
                    [0] = new SceneSequenceDefinition(),
                    [7] = new SceneSequenceDefinition
                    {
                        Character = new()
                        {
                            new ObjectiveIntent("reveal-rogers-delivery", 508, 8, MissionObjectiveState.NotAssigned),
                            new ObjectiveIntent("activate-rogers-delivery", 508, 8, MissionObjectiveState.Incomplete)
                        }
                    }
                }
            };
            for (var index = 0; index < Surveys.Length; index++)
            {
                var objective = (uint)index + 2;
                var role = $"survey-unit-{index + 1}";
                var position = Surveys[index];
                mission.Transition(objective);
                mission.Progress(objective, MissionProgressEventKind.ScenarioEvent, objective, counter: 1);
                mission.Indicator(objective, (uint)index + 1, position.X, position.Y, position.Z);
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Object, 7827,
                    position, InitialObjectState: SurveyReadyState,
                    UseAction: new SceneObjectAction(508, objective, objective, ActionArgId: 3));
                scene.Sequences[0].World.Add(new EnsureActorIntent($"introduce-{role}", role));
                scene.Sequences[objective] = new SceneSequenceDefinition
                {
                    World = new()
                    {
                        new TransitionObjectStateIntent($"retrieve-{role}", role, SurveyCollectedState),
                        new SetInteractionIntent($"disable-{role}", role, false)
                    },
                    Character = new()
                    {
                        new IssueMissionItemIntent($"disk-from-{role}", 508, "survey-data", 741, 1)
                    },
                    Signals = new() { new SceneMissionSignal(508, 1, objective) }
                };
            }
            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/508",
                "Native units2..6, summary7 and real Rogers116 delivery8. Five assignment-owned Survey Data disks use template741/class7826; each native class7827 unit uses action80/arg3.");
            mission.Evidence(2, MissionEvidenceSourceKind.Reconstruction,
                "https://tabularasa.fandom.com/wiki/Survey_Says_(Wilderness)",
                "Five sourced cavern positions retain numbered identities. Heading0 and level6 are reconstructed; base Hazmat Legs20697 and Reflective Legs35933 omit unsupported Luminar modifiers.");
            mission.Enable(scene);
        }

        private static void DeclareSceneEvents(MigrationBuilder migration, uint missionId, params uint[] eventIds)
        {
            foreach (var eventId in eventIds)
                migration.InsertData("mission_scenario_step",
                    new[]
                    {
                        "mission_id", "content_revision", "scenario_id", "step_id", "kind", "sequence",
                        "requirement", "scenario_event_id", "comment"
                    },
                    new object[]
                    {
                        missionId, WildernessMissionDataV1.Revision, 1U, eventId,
                        (byte)MissionScenarioStepKind.EmitScenarioEvent, eventId,
                        (byte)MissionContentRequirement.Required, eventId, "Declared W3 source-scoped scene result"
                    });
        }

        private static void SecondaryCounterText(MigrationBuilder migration, uint mission, uint objective, uint text) =>
            migration.UpdateData("mission_objective_definition",
                new[] { "mission_id", "content_revision", "objective_id" },
                new object[] { mission, WildernessMissionDataV1.Revision, objective },
                "client_counter_1_text_id", text);
    }
}
