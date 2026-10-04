using System;
using System.Text.Json;
using Rasa.Data;
using Rasa.Missions.Scenes;

namespace Rasa.Missions.Content.Wilderness
{
    [MissionScript("wilderness.solis-escort", 1)]
    public sealed class SolisEscortScene : ISceneScript
    {
        public SceneDecision Handle(SceneContext context, SceneObservation observation)
        {
            var phase = context.Run.Checkpoint == "{}"
                ? "new"
                : JsonSerializer.Deserialize<Checkpoint>(context.Run.Checkpoint)?.Phase
                    ?? throw new InvalidOperationException("Solis escort checkpoint has no phase.");

            if (observation.Kind == SceneEventKind.Started && phase == "new")
                return new SceneDecision(Serialize("waiting"),
                    new WorldIntent[]
                    {
                        new EnsureActorIntent("reserve-ranger", "ranger"),
                        new SetInteractionIntent("reserve-ranger-interaction", "ranger", false)
                    });
            if (observation.Kind == SceneEventKind.Signal && observation.SequenceId == 1 && phase == "waiting")
                return new SceneDecision(Serialize("walking"),
                    new WorldIntent[] { new RunRouteIntent("walk-to-solis", "ranger", "solis", ResumeAfterCombat: true) });
            if (observation.Kind == SceneEventKind.RouteCompleted && observation.Role == "ranger" &&
                observation.OperationKey == "walk-to-solis" && phase == "walking")
                return new SceneDecision(Serialize("arrived"),
                    characterIntents: new CharacterIntent[] { new ObjectiveIntent("solis-check-in", 1407, 10, MissionObjectiveState.Incomplete) },
                    status: SceneStatus.Ended);
            if ((observation.Kind == SceneEventKind.ActorDied && observation.Role == "ranger" ||
                 observation.Kind == SceneEventKind.Cancelled && observation.Name == "route-blocked") &&
                phase is "waiting" or "walking")
                return new SceneDecision(Serialize("failed"),
                    characterIntents:
                    new CharacterIntent[]
                    {
                        new ObjectiveIntent("reveal-failed-escort", 1407, 10, MissionObjectiveState.NotAssigned),
                        new ObjectiveIntent("activate-failed-escort", 1407, 10, MissionObjectiveState.Incomplete),
                        new ObjectiveIntent("fail-escort", 1407, 10, MissionObjectiveState.Failed)
                    }, status: SceneStatus.Ended);
            if (observation.Kind == SceneEventKind.Cancelled)
                return new SceneDecision(Serialize("cancelled"), status: SceneStatus.Ended);
            return new SceneDecision(context.Run.Checkpoint, status: context.Run.Status);
        }

        private static string Serialize(string phase) => JsonSerializer.Serialize(new Checkpoint(phase));

        public sealed record Checkpoint(string Phase);
    }
}
