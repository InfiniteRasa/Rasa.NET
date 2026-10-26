using System;
using System.Text.Json;
using Rasa.Data;
using Rasa.Missions.Scenes;

namespace Rasa.Missions.Content
{
    /// <summary>
    /// Cave Extraction (392): Recon Officer Tyler, the public NPC in the vault, walks the player
    /// out of Minos Caverns to its entrance (route "minos-caverns") and waits there, his
    /// conversation open again, for the turn-in; the reward retires the scene
    /// (SceneApplication.MissionChanged) and the lease takes him home. The escort fails if he
    /// dies, the owner is lost or the route is blocked. The route is walked through navigation: it
    /// resumes after a fight, and never teleports him.
    /// </summary>
    [MissionScript("minos.cave-extraction", 1)]
    public sealed class MinosCaveExtractionScene : ISceneScript
    {
        public const uint Mission = 392;
        public const string Role = "tyler";
        public const string Route = "minos-caverns";
        public const string Walk = "escort-to-entrance";

        public SceneDecision Handle(SceneContext context, SceneObservation observation)
        {
            var phase = context.Run.Checkpoint == "{}"
                ? "new"
                : JsonSerializer.Deserialize<Checkpoint>(context.Run.Checkpoint)?.Phase
                    ?? throw new InvalidOperationException("Tyler's checkpoint has no phase.");
            if (observation.Generation != context.Run.Generation)
                return Unchanged(context);
            if (observation.Kind == SceneEventKind.Started && phase == "new")
                return new SceneDecision(Serialize("walking"),
                    new WorldIntent[]
                    {
                        new EnsureActorIntent("reserve-tyler", Role),
                        new SetInteractionIntent("busy-tyler", Role, false),
                        new RunRouteIntent(Walk, Role, Route, ResumeAfterCombat: true)
                    });
            if (phase == "walking" && observation.Kind == SceneEventKind.RouteCompleted &&
                observation.Role == Role && observation.OperationKey == Walk)
                return new SceneDecision(Serialize("arrived"),
                    new WorldIntent[] { new SetInteractionIntent("tyler-at-the-entrance", Role, true) },
                    signals: new[] { new SceneMissionSignal(Mission, 1, 1) });
            if ((phase is "new" or "walking" or "arrived") &&
                (observation.Kind == SceneEventKind.ActorDied && observation.Role == Role ||
                 observation.Kind == SceneEventKind.OwnerLost ||
                 observation.Kind == SceneEventKind.Cancelled && observation.Name == "route-blocked"))
                return new SceneDecision(Serialize("failed"),
                    characterIntents: new CharacterIntent[]
                    {
                        new ObjectiveIntent("fail-cave-extraction", Mission, 1, MissionObjectiveState.Failed)
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
}
