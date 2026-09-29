using System;
using System.Text.Json;
using Rasa.Data;
using Rasa.Missions.Scenes;

namespace Rasa.Missions.Content.Wilderness
{
    [MissionScript("wilderness.walking-wounded", 1)]
    public sealed class WildernessWalkingWoundedScene : ISceneScript
    {
        public SceneDecision Handle(SceneContext context, SceneObservation observation)
        {
            var phase = context.Run.Checkpoint == "{}"
                ? "new"
                : JsonSerializer.Deserialize<Checkpoint>(context.Run.Checkpoint)?.Phase
                    ?? throw new InvalidOperationException("Matthew's checkpoint has no phase.");
            if (observation.Generation != context.Run.Generation)
                return Unchanged(context);
            if (observation.Kind == SceneEventKind.Started && phase == "new")
                return new SceneDecision(Serialize("walking"),
                    new WorldIntent[]
                    {
                        new EnsureActorIntent("reserve-matthew", "matthew"),
                        new SetInteractionIntent("busy-matthew", "matthew", false),
                        new RunRouteIntent("escort-to-quincy", "matthew", "ranja-caverns",
                            ResumeAfterCombat: true)
                    });
            if (phase == "walking" && observation.Kind == SceneEventKind.RouteCompleted &&
                observation.Role == "matthew" && observation.OperationKey == "escort-to-quincy")
                return new SceneDecision(Serialize("arrived"),
                    signals: new[] { new SceneMissionSignal(697, 1, 1) },
                    status: SceneStatus.Ended);
            if ((phase is "new" or "walking") &&
                (observation.Kind == SceneEventKind.ActorDied && observation.Role == "matthew" ||
                 observation.Kind == SceneEventKind.OwnerLost ||
                 observation.Kind == SceneEventKind.Cancelled && observation.Name == "route-blocked"))
                return new SceneDecision(Serialize("failed"),
                    characterIntents: new CharacterIntent[]
                    {
                        new ObjectiveIntent("fail-wounded-escort", 697, 1, MissionObjectiveState.Failed)
                    }, status: SceneStatus.Ended);
            if (observation.Kind == SceneEventKind.Cancelled)
                return new SceneDecision(Serialize("cancelled"), status: SceneStatus.Ended);
            return Unchanged(context);
        }

        private static SceneDecision Unchanged(SceneContext context) =>
            new(context.Run.Checkpoint, status: context.Run.Status);

        private static string Serialize(string phase) => JsonSerializer.Serialize(new Checkpoint(phase));

        public sealed record Checkpoint(string Phase);
    }

    [MissionScript("wilderness.ranja-egg-clusters", 1)]
    public sealed class WildernessRanjaEggClustersScene : ISceneScript
    {
        public SceneDecision Handle(SceneContext context, SceneObservation observation)
        {
            if (observation.Generation != context.Run.Generation)
                return new SceneDecision(context.Run.Checkpoint, status: context.Run.Status);
            if (observation.Kind is SceneEventKind.Cancelled or SceneEventKind.MissionRewarded)
                return new SceneDecision(context.Run.Checkpoint, status: SceneStatus.Ended);
            if (observation.Kind == SceneEventKind.Started && observation.SequenceId == 0 ||
                observation.Kind == SceneEventKind.Signal && observation.SequenceId is >= 1 and <= 4)
                return new DataSequenceScript().Handle(context, observation);
            return new SceneDecision(context.Run.Checkpoint, status: context.Run.Status);
        }
    }
}
