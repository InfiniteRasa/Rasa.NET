using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Rasa.Context.World;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Test.Missions
{
    internal static class MissionDialogueTestContent
    {
        internal const string Revision = "dialogue-test";

        internal static MissionSceneDefinition Install(SqliteWorldContext world, bool conversationObject = false,
            MissionDialogueKind kind = MissionDialogueKind.Choice)
        {
            world.Add(new MissionContentDefinitionEntry
            {
                MissionId = 339, ContentRevision = Revision, Enabled = true, ClientNameTextId = 4318,
                GiverId = 77, ReceiverId = 88, Level = 1, GroupType = 1, CategoryId = 1,
                Comment = "Isolated native three-choice fixture"
            });
            world.Add(new MissionObjectiveDefinitionEntry
            {
                MissionId = 339, ContentRevision = Revision, ObjectiveId = 8, ClientNameTextId = 4318,
                ClientBodyTextId = 4318, Ordinal = 1, InitialState = (byte)MissionObjectiveState.Incomplete,
                IsRequired = true, Comment = "Native objectiveconversation fixture"
            });
            if (!world.Set<NpcPackageEntry>().Any(entry => entry.PackageId == 586))
                world.Add(new NpcPackageEntry { PackageId = 586, Comment = "Native choice test package" });
            world.Add(new MissionRewardDefinitionEntry
            {
                MissionId = 339, ContentRevision = Revision, RewardId = 400, Comment = "Empty native turn-in package"
            });
            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                Dialogue = new()
                {
                    new(8, 586, 1, kind,
                        transitionId: kind == MissionDialogueKind.Completion ? 101U : null,
                        choices: kind == MissionDialogueKind.Choice
                            ? new Dictionary<int, uint> { [1] = 101, [2] = 102, [3] = 103 } : null)
                },
                Sequences = new() { [0] = new() }
            };
            for (uint branch = 1; branch <= 3; branch++)
            {
                var transition = 100 + branch;
                var sequence = 200 + branch;
                var reward = 400 + branch;
                world.Add(new MissionObjectiveTransitionEntry
                {
                    MissionId = 339, ContentRevision = Revision, ObjectiveId = 8, TransitionId = transition,
                    Sequence = branch, FromState = (byte)MissionObjectiveState.Incomplete,
                    ToState = (byte)MissionObjectiveState.Completed, Comment = $"Choice {branch}"
                });
                world.Add(new MissionTriggerEntry
                {
                    MissionId = 339, ContentRevision = Revision, ObjectiveId = 8, TransitionId = transition,
                    TriggerId = 1, Kind = MissionTriggerKind.Conversation, NpcPackageId = 586,
                    PlayerFlagId = 1, Comment = "Source-backed native tuple"
                });
                world.Add(new MissionActionEntry
                {
                    MissionId = 339, ContentRevision = Revision, ObjectiveId = 8, TransitionId = transition,
                    ActionId = 1, Sequence = 1, Kind = MissionActionKind.SetPlayerFlag,
                    PlayerFlagId = 700 + branch, PlayerFlagValue = 1, Comment = "Record selected branch"
                });
                world.Add(new MissionActionEntry
                {
                    MissionId = 339, ContentRevision = Revision, ObjectiveId = 8, TransitionId = transition,
                    ActionId = 2, Sequence = 2, Kind = MissionActionKind.StartScenario,
                    ScenarioId = sequence, Comment = "Queue selected scene input"
                });
                world.Add(new MissionActionEntry
                {
                    MissionId = 339, ContentRevision = Revision, ObjectiveId = 8, TransitionId = transition,
                    ActionId = 3, Sequence = 3, Kind = MissionActionKind.GrantReward,
                    RewardId = 400, Comment = "Declare the turn-in package"
                });
                world.Add(new MissionScenarioEntry
                {
                    MissionId = 339, ContentRevision = Revision, ScenarioId = sequence, Name = $"Choice {branch}",
                    StartPolicy = MissionScenarioStartPolicy.Automatic, Comment = "Native choice test sequence"
                });
                world.Add(new MissionScenarioStepEntry
                {
                    MissionId = 339, ContentRevision = Revision, ScenarioId = sequence, StepId = 1,
                    Kind = MissionScenarioStepKind.EmitScenarioEvent, ScenarioEventId = branch,
                    Comment = "Authored sequence reference"
                });
                world.Add(new MissionRewardDefinitionEntry
                {
                    MissionId = 339, ContentRevision = Revision, RewardId = reward, Credits = 10 * branch,
                    Comment = "Isolated choice reward"
                });
                scene.Sequences[sequence] = new()
                {
                    Character = new()
                    {
                        new GrantRewardIntent($"choice-reward-{branch}", 339, reward),
                        new SetCharacterFlagIntent($"choice-scene-flag-{branch}", 800 + branch, 1)
                    }
                };
            }
            if (conversationObject)
            {
                scene.Actors["dialogue-object"] = new("dialogue-object", SceneActorKind.Object, 21081,
                    new ScenePosition(0, 0, 0), Conversation: new SceneObjectConversation(339, 8, 586, 8, 1, kind));
                scene.Sequences[0].World.Add(new EnsureActorIntent("dialogue-object-create", "dialogue-object"));
            }
            world.Add(new MissionSceneBindingEntry
            {
                MissionId = 339, ContentRevision = Revision, ScriptKey = scene.Script,
                Bindings = JsonSerializer.Serialize(scene, MissionContentCodec.Options)
            });
            world.SaveChanges();
            world.ChangeTracker.Clear();
            return scene;
        }

        internal static void SaveScene(SqliteWorldContext world, MissionSceneDefinition scene)
        {
            world.Set<MissionSceneBindingEntry>().Find(339U, Revision).Bindings =
                JsonSerializer.Serialize(scene, MissionContentCodec.Options);
            world.SaveChanges();
            world.ChangeTracker.Clear();
        }
    }
}
