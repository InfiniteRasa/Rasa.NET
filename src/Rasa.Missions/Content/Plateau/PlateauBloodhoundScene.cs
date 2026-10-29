using System;
using System.Text.Json;
using Rasa.Data;
using Rasa.Missions.Scenes;

namespace Rasa.Missions.Content.Plateau
{
    /// <summary>
    /// The Stainless Steel Bloodhound (867), Plateau: when the player tells Field Sgt. Garde they are ready
    /// (objective 3, its transition starts scenario <see cref="Activate"/>), Garde activates his Modified Bane
    /// Sentinel (client 4542: "I'm activating the Sentinel now") and it backtracks the Thrax patrol route
    /// along the chasm's stream to the hidden cave. At the cave it is gone and the escort (2) is done
    /// (scenario <see cref="Arrived"/>, event 1); its death on the way or a blocked route fails the escort.
    /// It appears only at Garde's word: a scene creature off a route reports no death, so one that waited
    /// could die unseen.
    /// </summary>
    [MissionScript("plateau.bloodhound", 1)]
    public sealed class PlateauBloodhoundScene : ISceneScript
    {
        public const uint MissionId = 867;
        public const uint Escort = 2, Ready = 3;
        public const uint Arrived = 1, Activate = 2;

        public SceneDecision Handle(SceneContext context, SceneObservation observation)
        {
            var phase = context.Run.Checkpoint == "{}"
                ? "new"
                : JsonSerializer.Deserialize<Checkpoint>(context.Run.Checkpoint)?.Phase
                    ?? throw new InvalidOperationException("Bloodhound checkpoint has no phase.");
            if (phase is not ("new" or "waiting" or "tracking" or "done" or "failed" or "cancelled"))
                throw new InvalidOperationException($"Bloodhound checkpoint has an invalid phase {phase}.");

            if (observation.Kind == SceneEventKind.Started && phase == "new" ||
                observation.Kind == SceneEventKind.Recovered && phase == "waiting")
                return Decide("waiting");
            if (observation.Kind == SceneEventKind.Signal && observation.SequenceId == Activate && phase == "waiting" ||
                observation.Kind == SceneEventKind.Recovered && phase == "tracking")
                return Decide("tracking", new WorldIntent[]
                {
                    new EnsureActorIntent("ensure-sentinel", "sentinel"),
                    new SetInteractionIntent("sentinel-busy", "sentinel", false),
                    QuestSceneIntents.Escort("track", "sentinel", "to-cave")
                });

            if (observation.Kind == SceneEventKind.RouteCompleted && observation.Role == "sentinel" &&
                observation.OperationKey == "track" && phase == "tracking")
                return new SceneDecision(JsonSerializer.Serialize(new Checkpoint("done")),
                    new WorldIntent[] { new RemoveActorIntent("sentinel-in-the-cave", "sentinel") },
                    signals: new[] { new SceneMissionSignal(MissionId, Arrived, 1) }, status: SceneStatus.Ended);

            if (phase == "tracking" &&
                (observation.Kind == SceneEventKind.ActorDied && observation.Role == "sentinel" ||
                 observation.Kind == SceneEventKind.Cancelled && observation.Name == "route-blocked"))
                return Decide("failed", character: new CharacterIntent[]
                {
                    new ObjectiveIntent("escort-failed", MissionId, Escort, MissionObjectiveState.Failed)
                }, status: SceneStatus.Ended);

            if (observation.Kind == SceneEventKind.Cancelled)
                return Decide("cancelled", status: SceneStatus.Ended);
            return new SceneDecision(context.Run.Checkpoint, status: context.Run.Status);
        }

        private static SceneDecision Decide(string phase, WorldIntent[] world = null, CharacterIntent[] character = null,
            SceneStatus status = SceneStatus.Running) =>
            new(JsonSerializer.Serialize(new Checkpoint(phase)), world, character, status: status);

        public sealed record Checkpoint(string Phase);
    }
}
