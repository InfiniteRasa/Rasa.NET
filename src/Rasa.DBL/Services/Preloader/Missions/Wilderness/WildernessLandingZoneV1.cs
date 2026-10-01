using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Wilderness
{
    public static class WildernessLandingZoneV1
    {
        public static void Up(MigrationBuilder migration)
        {
            CacheOfTheDay(migration);
            MortarByNumbers(migration);
            FailureToLaunch(migration);
            SoldiersBlood(migration);
            LightbenderGlands(migration);
            DroningOn(migration);
            InShortSupply(migration);
            EscapeVelocity(migration);
        }

        public static void Down(MigrationBuilder migration) =>
            WildernessMissionDataV1.Remove(migration, 665, 430, 549, 776, 795, 771, 441, 666);

        private static void CacheOfTheDay(MigrationBuilder migration)
        {
            var mission = new WildernessMissionDataV1(migration, 665, "Cache of the Day",
                2658, 97, 97, 6);
            mission.Objective(1, 2662, 6648, 0, MissionObjectiveState.Incomplete, counterTextId: 2663);
            mission.Reward(7000, 350, selectableItems: new (uint, uint)[] { (13744, 1), (28692, 1) });
            mission.Scenario(1, "Destroy ten distinct Bane fuel containers");
            mission.Transition(1);
            mission.Action(1, 1, MissionActionKind.GrantReward, reward: 1);
            var positions = new ScenePosition[]
            {
                new(-269, 170.413573f, 76),
                new(-265, 170.413573f, 76),
                new(-261, 169.925284f, 76),
                new(-257, 170.169429f, 76),
                new(-269, 170.108392f, 98),
                new(-265, 170.230465f, 98),
                new(-261, 169.98632f, 98),
                new(-257, 169.803212f, 98),
                new(-267, 170.230465f, 81),
                new(-259, 170.108392f, 81)
            };
            var scene = new MissionSceneDefinition { Script = "data.sequence" };
            scene.Sequences[0] = new SceneSequenceDefinition();
            for (var index = 0; index < positions.Length; index++)
            {
                var id = (uint)index + 1;
                var role = $"fuel-{id}";
                EmitScenarioEvent(migration, 665, 1, id, id);
                mission.Progress(1, MissionProgressEventKind.ScenarioEvent, id, counter: 1, target: 10, trigger: id);
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Object, 9260,
                    positions[index], InitialObjectState: 110,
                    Destruction: new SceneObjectDestruction(665, 1, id, 100));
                scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{role}", role));
                scene.Sequences[id] = new SceneSequenceDefinition
                {
                    World = new()
                    {
                        new TransitionObjectStateIntent($"destroy-{role}", role, 2),
                        new SetInteractionIntent($"disable-{role}", role, false)
                    },
                    Signals = new() { new SceneMissionSignal(665, 1, id) }
                };
            }
            mission.Indicator(1, 1, -262, 170, 87, radius: 20);
            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/665",
                "Native objective1/counter2663. Chemical Container class9260 is an augmentation41 object, never a creature or an interaction-count proxy.");
            mission.Evidence(2, MissionEvidenceSourceKind.Reconstruction, "repository:docs/wilderness-missions.md",
                "Ten grounded 100HP containers in the measured Bane courtyard use distinct source-scoped destruction receipts. Native corridor stays clear. Motor Assist legs13744/gloves28692 replace unsupported Olympia modifiers.");
            mission.Enable(scene);
        }

        private static void MortarByNumbers(MigrationBuilder migration)
        {
            var mission = new WildernessMissionDataV1(migration, 430, "Mortar By Numbers",
                745, 104, 104, 6);
            mission.Reward(6000, 900, selectableItems: new (uint, uint)[] { (26940, 1), (13739, 1) });
            var targets = new[]
            {
                (Objective: 3U, Name: 7581U, Body: 7582U, Creature: 630076U, X: 360.8660888671875, Y: 218.5, Z: 95.262939453125),
                (Objective: 4U, Name: 7583U, Body: 7584U, Creature: 630077U, X: 212.7183837890625, Y: 227.7, Z: 286.189453125),
                (Objective: 5U, Name: 7585U, Body: 7586U, Creature: 630078U, X: 185.026123046875, Y: 238.3, Z: 399.9229736328125),
                (Objective: 6U, Name: 7587U, Body: 7588U, Creature: 630079U, X: 99.7130355834961, Y: 232.5, Z: 551.562255859375)
            };
            foreach (var target in targets)
            {
                mission.Objective(target.Objective, target.Name, target.Body, target.Objective - 3,
                    MissionObjectiveState.Incomplete);
                mission.Transition(target.Objective);
                mission.Progress(target.Objective, MissionProgressEventKind.CreatureKilled, target.Creature);
                mission.Indicator(target.Objective, target.Objective, target.X, target.Y, target.Z);
            }
            mission.Action(6, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/430",
                "All four native objectives3/4/5/6 are required. Damageable mortar class7482 is distinct from static base7478.");
            mission.Evidence(2, MissionEvidenceSourceKind.Reconstruction, "repository:docs/wilderness-missions.md",
                "Four coordinator-owned mortar identities630076..630079 map the measured creek/ridge bases south-to-north, excluding the Imperial Valley bases. Base Leech Gun26940/Motor Assist boots13739 replace unsupported ChiTech/Olympia modifiers.");
            mission.Enable();
        }

        private static void SoldiersBlood(MigrationBuilder migration)
        {
            var mission = new WildernessMissionDataV1(migration, 776, "Soldier's Blood",
                3764, 110, 110, 5);
            mission.Objective(2, 3768, 7816, 0, MissionObjectiveState.Incomplete);
            mission.Reward(4000, 300);
            mission.Transition(2);
            mission.Progress(2, MissionProgressEventKind.ItemAcquired, 11150, target: 10);
            mission.Action(2, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/776",
                "Ojy's objective2 requires ten Thrax blood samples, class11150/template2524; not Hansen's mission777.");
            mission.Evidence(2, MissionEvidenceSourceKind.Reconstruction, "repository:docs/wilderness-missions.md",
                "One guaranteed personal sample per eligible ordinary Thrax soldier corpse until ten are held; pickup advances progress and turn-in consumes ten.");
            mission.Enable(Collection("blood", 2524, 10, 2, 3));
        }

        private static void LightbenderGlands(MigrationBuilder migration)
        {
            var mission = new WildernessMissionDataV1(migration, 795, "Lightbender Glands",
                3881, 110, 110, 6);
            mission.Prerequisite(776);
            mission.Objective(3, 3887, 7804, 0, MissionObjectiveState.Incomplete);
            mission.Reward(4000, 300);
            mission.Transition(3);
            mission.Progress(3, MissionProgressEventKind.ItemAcquired, 11317, target: 4);
            mission.Action(3, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/795",
                "Native objective3 requires four Lightbender Glands, class11317/template2557, after Soldier's Blood776.");
            mission.Evidence(2, MissionEvidenceSourceKind.Reconstruction, "repository:docs/wilderness-missions.md",
                "Eligible ordinary Lightbender630071/class7120 has four coordinator-owned grounded camp spawns630071..630074. Fulgor and other class variants are not substituted; one personal gland per corpse.");
            mission.Enable(Collection("glands", 2557, 4, 3, 630071));
        }

        private static void DroningOn(MigrationBuilder migration)
        {
            var mission = new WildernessMissionDataV1(migration, 771, "Droning On",
                3737, 110, 110, 6);
            mission.Prerequisite(795);
            mission.Objective(2, 3741, 7812, 0, MissionObjectiveState.Incomplete);
            mission.Reward(4000, 300);
            mission.Transition(2);
            mission.Progress(2, MissionProgressEventKind.ItemAcquired, 11153, target: 6);
            mission.Action(2, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/771",
                "Objective2 requires six Shield Drone Scraps, class11153/template2527, after Lightbender Glands.");
            mission.Evidence(2, MissionEvidenceSourceKind.Reconstruction, "repository:docs/wilderness-missions.md",
                "4000XP follows Ojy's two sourced predecessor rewards; six guaranteed personal scraps come from actual Shield Drone creature85 corpses.");
            mission.Enable(Collection("drone-scraps", 2527, 6, 2, 85));
        }

        private static MissionSceneDefinition Collection(string key, uint template, uint quantity,
            uint objective, params uint[] creatures) => new()
        {
            ExistingFactObjectiveIds = new() { objective },
            Items = new()
            {
                new MissionItemBinding(key, template, MissionItemScope.CharacterOwned, quantity,
                    MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                    MissionItemCleanupDisposition.Retain, TurnInQuantity: quantity,
                    Drop: new MissionItemDropDefinition(creatures, objective, 1220, 100, 1))
            }
        };

        private static void FailureToLaunch(MigrationBuilder migration)
        {
            var mission = new WildernessMissionDataV1(migration, 549, "Failure to Launch",
                1486, 104, 104, 5);
            mission.Prerequisite(430);
            mission.Objective(1, 1499, 6669, 0, MissionObjectiveState.Incomplete);
            mission.Reward(3000, 600, selectableItems: new (uint, uint)[] { (47593, 1), (45125, 1) });
            mission.Transition(1);
            mission.Conversation(1, 382);
            mission.Action(1, 1, MissionActionKind.IssueMissionItem,
                itemIntent: new IssueMissionItemIntent("salter-catalyzer", 549, "catalyzer", 747, 1));
            mission.Action(1, 2, MissionActionKind.GrantReward, reward: 1);
            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:objectiveconversation/549/1/382/1",
                "Salter package382 supplies the native catalyzer, class7981/template747; Wagner receives it.");
            mission.Evidence(2, MissionEvidenceSourceKind.Reconstruction, "repository:docs/wilderness-missions.md",
                "The single catalyzer is assignment-issued, consumed once at turn-in and removed on failure or abandonment. Choose one Class I Res Trauma Kit or Fragmentation Grenade.");
            mission.Enable(new MissionSceneDefinition
            {
                Items = new()
                {
                    new MissionItemBinding("catalyzer", 747, MissionItemScope.AssignmentIssued, 1,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove,
                        MissionItemCleanupDisposition.Remove, TurnInQuantity: 1)
                }
            });
        }

        private static void InShortSupply(MigrationBuilder migration)
        {
            var mission = new WildernessMissionDataV1(migration, 441, "In Short Supply",
                789, 130, 125, 6);
            mission.Prerequisite(549);
            mission.Objective(1, 1065, 6804, 0, MissionObjectiveState.Incomplete);
            mission.Reward(3500, 700, selectableItems: new (uint, uint)[] { (45125, 1), (44917, 1) });
            mission.Transition(1);
            mission.Conversation(1, 218);
            mission.Action(1, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:objectiveconversation/441/1",
                "Randolph package212 dispatches the request; Duncan package218 receives it at Twin Pillars. Randolph's ambient text2705 is read-only.");
            mission.Evidence(2, MissionEvidenceSourceKind.Reconstruction, "repository:docs/wilderness-missions.md",
                "No physical report template is established; preserve the dialogue delivery without inventing one. Choose one Class I Fragmentation Grenade or Basic Med Pack.");
            mission.Enable(new MissionSceneDefinition
            {
                Dialogue = new()
                {
                    new MissionDialogueTopicDefinition(1, 212, 1, MissionDialogueKind.Ambient)
                }
            });
        }

        private static void EscapeVelocity(MigrationBuilder migration)
        {
            var mission = new WildernessMissionDataV1(migration, 666, "Escape Velocity",
                2666, 630070, 97, 7);
            mission.Objective(1, 2670, 6618, 0, MissionObjectiveState.Incomplete);
            mission.Reward(14000, 1400, selectableItems: new (uint, uint)[] { (13744, 1), (28692, 1) });
            mission.Scenario(1, "Pierre boards at the Wilderness L.Z.");
            EmitScenarioEvent(migration, 666, 1, 1, 2);
            mission.Transition(1);
            mission.Progress(1, MissionProgressEventKind.ScenarioEvent, 2, counter: 1);
            mission.Action(1, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Transition(1, transition: 2, result: MissionObjectiveState.Failed, sequence: 2);
            migration.InsertData("mission_trigger",
                new[]
                {
                    "mission_id", "content_revision", "objective_id", "transition_id", "trigger_id",
                    "requirement", "kind", "sequence", "duration_seconds", "comment"
                },
                new object[]
                {
                    666U, WildernessMissionDataV1.Revision, 1U, 2U, 1U,
                    (byte)MissionContentRequirement.Required, (byte)MissionTriggerKind.TimerElapsed,
                    1U, 420U, "Pierre must reach the raised boarding pad before departure."
                });
            mission.Indicator(1, 1, 192.2, 171.1, -100.5, radius: 0.6);
            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/666",
                "Native female Sgt. Pierre/name3097 and objective1. Forcefield20000003/augmentation62 is destroyed by damage, not a use action; native states196/199.");
            mission.Evidence(2, MissionEvidenceSourceKind.Reconstruction, "https://tabularasa.fandom.com/wiki/Escape_Velocity",
                "420-second durable deadline. Discover Pierre at the cache without requiring completed665. Captive or escort death and owner loss fail; the exclusive public actor returns before reuse. No zone invasion simulation.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:docs/wilderness-missions.md",
                "Grounded559m approach and84.4m raised-pad leg are source/geometry-qualified. Boarding is spatial within0.6m horizontal/0.35m footY at(192.2,171.1,-100.5), beside native ship7331/pad10374; no passenger socket or attachment is claimed. Base Motor Assist legs13744/gloves28692 replace unsupported Olympia/Pulsar modifiers.");
            mission.Enable(new MissionSceneDefinition
            {
                Script = "wilderness.escape-velocity",
                Actors = new()
                {
                    ["pierre"] = new SceneActorDefinition("pierre", SceneActorKind.PublicSpawn, 630070),
                    ["forcefield"] = new SceneActorDefinition("forcefield", SceneActorKind.Object, 20000003,
                        new ScenePosition(-273, 170, 91), Orientation: -1.570796327,
                        InitialObjectState: 196,
                        Destruction: new SceneObjectDestruction(666, 1, 1, HitPoints: 100, DestroyedState: 199))
                },
                Routes = new()
                {
                    ["landing-zone"] = new SceneRoute("landing-zone", new SceneWaypoint[]
                    {
                        new(new ScenePosition(-279, 170.1f, 87)),
                        new(new ScenePosition(-238.5f, 170.7f, 81.2f)),
                        new(new ScenePosition(-124.1f, 183.7f, 1.2f)),
                        new(new ScenePosition(-26.5f, 168.9f, -42)),
                        new(new ScenePosition(-10.1f, 167.3f, -45.6f)),
                        new(new ScenePosition(-0.9f, 166.5f, -47.6f)),
                        new(new ScenePosition(31.5f, 164.3f, -51.2f)),
                        new(new ScenePosition(53.5f, 163.5f, -60)),
                        new(new ScenePosition(55.1f, 163.5f, -60)),
                        new(new ScenePosition(62.3f, 164.1f, -57.2f)),
                        new(new ScenePosition(82.7f, 165.1f, -51.2f)),
                        new(new ScenePosition(114.3f, 165.5f, -46.8f)),
                        new(new ScenePosition(115.9f, 165.7f, -46.4f)),
                        new(new ScenePosition(140.7f, 165.3f, -30.8f)),
                        new(new ScenePosition(155.9f, 163.7f, -23.2f)),
                        new(new ScenePosition(159.1f, 163.5f, -22.8f)),
                        new(new ScenePosition(177.9f, 164.1f, -28)),
                        new(new ScenePosition(179.5f, 163.7f, -28.4f)),
                        new(new ScenePosition(190.7f, 163.5f, -29.2f)),
                        new(new ScenePosition(193.1f, 163.9f, -30.4f)),
                        new(new ScenePosition(196.3f, 163.3f, -66.8f)),
                        new(new ScenePosition(195.1f, 163.1f, -68.4f))
                    }, Speed: 2.5f),
                    ["boarding"] = new SceneRoute("boarding", new SceneWaypoint[]
                    {
                        new(new ScenePosition(195.1f, 163.1f, -68.4f)),
                        new(new ScenePosition(195.1f, 163.1f, -74)),
                        new(new ScenePosition(177.1f, 163.1f, -91.2f)),
                        new(new ScenePosition(177.5f, 163.1f, -93.2f)),
                        new(new ScenePosition(185.1f, 167.1f, -95.2f)),
                        new(new ScenePosition(189.1f, 167.1f, -98.4f)),
                        new(new ScenePosition(190.7f, 167.1f, -99.6f)),
                        new(new ScenePosition(193.1f, 167.1f, -99.6f)),
                        new(new ScenePosition(197.1f, 167.1f, -96.4f)),
                        new(new ScenePosition(198.7f, 171.1f, -89.6f)),
                        new(new ScenePosition(200.3f, 171.1f, -89.2f)),
                        new(new ScenePosition(201.1f, 171.1f, -89.6f)),
                        new(new ScenePosition(201.1f, 171.1f, -98)),
                        new(new ScenePosition(200.3f, 171.1f, -98.8f)),
                        new(new ScenePosition(192.2f, 171.1f, -100.5f), Orientation: 2.856263233407658)
                    }, Speed: 2.5f)
                },
                Sequences = new()
                {
                    [0] = new SceneSequenceDefinition(),
                    [1] = new SceneSequenceDefinition()
                },
                Names = new() { ["forcefield-open-state"] = 199 },
                PublicEncounter = new PublicEncounterBinding(666, 630070, "pierre", "wilderness.escape-velocity",
                    OwnerLossPolicy: "Fail")
            });
        }

        private static void EmitScenarioEvent(MigrationBuilder migration, uint missionId, uint scenarioId,
            uint stepId, uint eventId) =>
            migration.InsertData("mission_scenario_step",
                new[]
                {
                    "mission_id", "content_revision", "scenario_id", "step_id", "kind", "sequence",
                    "requirement", "scenario_event_id", "comment"
                },
                new object[]
                {
                    missionId, WildernessMissionDataV1.Revision, scenarioId, stepId,
                    (byte)MissionScenarioStepKind.EmitScenarioEvent, stepId,
                    (byte)MissionContentRequirement.Required, eventId, "Source-scoped Wilderness scene result"
                });
    }
}
