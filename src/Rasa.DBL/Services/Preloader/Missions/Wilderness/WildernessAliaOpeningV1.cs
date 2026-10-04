using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Runtime;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Wilderness
{
    public static partial class WildernessAliaOpeningV1
    {
        public static void Up(MigrationBuilder migration)
        {
            TooClose(migration);
            Reception(migration);
            FormingAlliances(migration);
            Targets(migration);
        }

        public static void Down(MigrationBuilder migration) =>
            WildernessMissionDataV1.Remove(migration, 1407, 1069, 479, 1449);

        private static void TooClose(MigrationBuilder migration)
        {
            var mission = new WildernessMissionDataV1(migration, 1407, "Too Close For Comfort",
                12099, 100, 42, 4);
            mission.Objective(1, 12104, 12105, 0, MissionObjectiveState.Incomplete);
            mission.Objective(10, 12128, 12129, 1);
            mission.Reward(4000, 600, new (uint, uint)[] { (641, 1), (45072, 1) });
            mission.Scenario(1, "Escort the ranger to Solis", activatesObjective: 10);
            mission.Transition(1);
            mission.Conversation(1, 113);
            mission.Action(1, 1, MissionActionKind.RevealObjective, 10);
            mission.Action(1, 2, MissionActionKind.StartScenario, scenario: 1);
            mission.Transition(10);
            mission.Conversation(10, 168);
            mission.Action(10, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Indicator(1, 1, 812, 302.11786, 505.1953);
            mission.Indicator(10, 2, 784.7, 287.33997, 581.1);
            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1407",
                "Native objectives 1 and 10; completion packages 113 and 168.");
            mission.Evidence(2, MissionEvidenceSourceKind.Reconstruction, "repository:docs/wilderness-missions.md",
                "Generic ranger and navmesh route reconstructed; obsolete recipe rewards replaced with working ammo and medical schematics.");
            mission.Enable(SolisEscort());
        }

        private static void Reception(MigrationBuilder migration)
        {
            var mission = new WildernessMissionDataV1(migration, 1069, "Receptive Reception",
                6038, 42, 43, 4);
            mission.Prerequisite(1407);
            mission.Objective(1, 6042, 6620, 0, MissionObjectiveState.Incomplete);
            mission.Objective(2, 13793, 13794, 1);
            mission.Objective(3, 13796, 13797, 2);
            mission.Reward(4000, 600, selectableItems: new (uint, uint)[] { (20399, 1), (35486, 1) });
            mission.Transition(1);
            mission.Progress(1, MissionProgressEventKind.LogosAcquired, 10);
            mission.RevealAndActivate(1, 2);
            mission.Transition(2);
            mission.Conversation(2, 168);
            mission.RevealAndActivate(2, 3);
            mission.Transition(3);
            mission.Conversation(3, 112);
            mission.Action(3, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Indicator(1, 1, 832, 161.838, 960);
            mission.Indicator(2, 2, 784.7, 287.33997, 581.1);
            mission.Indicator(3, 3, 826.85156, 301.51782, 502.27734);
            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1069",
                "Native objectives 1,2,3 and packages168/112; Enhance is Logos10.");
            mission.Evidence(2, MissionEvidenceSourceKind.Reconstruction, "repository:docs/wilderness-missions.md",
                "Already-owned Enhance recognized; base armor rewards omit unsupported historical loot modifiers.");
            mission.Enable(new MissionSceneDefinition { ExistingFactObjectiveIds = new() { 1 } });
        }

        private static void FormingAlliances(MigrationBuilder migration)
        {
            var mission = new WildernessMissionDataV1(migration, 479, "Forming Alliances",
                1011, 43, 43, 4);
            mission.Prerequisite(1069);
            mission.Objective(1, 2592, 6525, 0, MissionObjectiveState.Incomplete);
            mission.Reward(4000, 400, new (uint, uint)[] { (20846, 1) });
            mission.Transition(1);
            mission.Progress(1, MissionProgressEventKind.ItemAcquired, 10346, target: 12);
            mission.Action(1, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Indicator(1, 1, 560.95703, 288.1328, 460.1914, radius: 60);
            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/479",
                "Native objective1 collects12 Thrax Hearts; class10346 stack12.");
            mission.Evidence(2, MissionEvidenceSourceKind.Reconstruction, "repository:docs/wilderness-missions.md",
                "Existing heart template2285; ordinary soldier source; base Hazmat Vest replaces unsupported historical loot modifiers.");
            mission.Enable(new MissionSceneDefinition
            {
                Items = new()
                {
                    new MissionItemBinding("hearts", 2285, MissionItemScope.CharacterOwned, 12,
                        MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                        MissionItemCleanupDisposition.Retain, TurnInQuantity: 12,
                        Drop: new MissionItemDropDefinition(new uint[] { 3 }, 1, 1220, 100, 1))
                }
            });
        }

        public static MissionSceneDefinition SolisEscort() => new()
        {
            Script = "wilderness.solis-escort",
            Actors = new()
            {
                ["ranger"] = new SceneActorDefinition("ranger", SceneActorKind.PublicSpawn,
                    WildernessOpeningWorldV1.RangerSpawnId)
            },
            Routes = new()
            {
                ["solis"] = new SceneRoute("solis",
                new SceneWaypoint[]
                {
                    new(new ScenePosition(807.8571f, 301.9178f, 498.8f)),
                    new(new ScenePosition(814.657f, 302.1179f, 498.8f)),
                    new(new ScenePosition(817.457f, 302.1179f, 499.6f)),
                    new(new ScenePosition(820.657f, 301.9178f, 501.2f)),
                    new(new ScenePosition(820.657f, 301.9178f, 510.8f)),
                    new(new ScenePosition(802.257f, 301.5178f, 530)),
                    new(new ScenePosition(787.457f, 297.3179f, 543.6f)),
                    new(new ScenePosition(786.657f, 296.7178f, 544.4f)),
                    new(new ScenePosition(782.657f, 291.9178f, 554)),
                    new(new ScenePosition(782.657f, 291.5178f, 560.4f)),
                    new(new ScenePosition(784.7f, 287.33997f, 581.1f), Orientation: 3)
                }, Speed: 4.5f)
            },
            Sequences = new()
            {
                [0] = new SceneSequenceDefinition(),
                [1] = new SceneSequenceDefinition()
            },
            PublicEncounter = new PublicEncounterBinding(1407, WildernessOpeningWorldV1.RangerSpawnId,
                "ranger", "wilderness.solis-escort", OwnerLossPolicy: "Wait")
        };
    }
}
