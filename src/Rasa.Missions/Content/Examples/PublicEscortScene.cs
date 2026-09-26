using System.Text.Json;
using Rasa.Missions.Scenes;

namespace Rasa.Missions.Content.Examples
{
    [MissionScript("example.escort", 1)]
    public sealed class PublicEscortScene : ISceneScript
    {
        public SceneDecision Handle(SceneContext context, SceneObservation observation) =>
            observation.Kind switch
            {
                SceneEventKind.Started => new SceneDecision("{\"phase\":\"escorting\"}",
                    new WorldIntent[]
                    {
                        new EnsureActorIntent("ensure-guide", "guide"),
                        new SetInteractionIntent("busy-guide", "guide", false),
                        new RunRouteIntent("escort-route", "guide", "outbound")
                    }),
                SceneEventKind.WaypointReached => new SceneDecision(
                    JsonSerializer.Serialize(new { phase = "escorting", waypoint = observation.Waypoint })),
                SceneEventKind.RouteCompleted => new SceneDecision("{\"phase\":\"complete\"}",
                    signals: new[] { new SceneMissionSignal(context.Run.MissionId, 1, 1) },
                    status: SceneStatus.Ended),
                SceneEventKind.ActorDied or SceneEventKind.Cancelled =>
                    new SceneDecision("{\"phase\":\"failed\"}", status: SceneStatus.Ended),
                _ => new SceneDecision(context.Run.Checkpoint)
            };
    }
}
