using System;
using System.Linq;
using System.Text.Json;
using Rasa.Data;
using Rasa.Missions.Scenes;

namespace Rasa.Missions.Content.Plateau
{
    /// <summary>
    /// Out of Thin Air (1190), Ustor Yard: the small Thrax patrol that carries the stim pack stands from the start; the
    /// prisoner's talk (scenario <see cref="EscortStarts"/>) sets her, the public NPC who gave the mission, on her route
    /// to the west exit (objective 1). Her death, a blocked route or the owner's loss fails 1. At the exit the scene ends:
    /// as upstream's Sgt. Pierre, the public NPC's lease reset walks her back to her rock, for the next player.
    /// </summary>
    [MissionScript("plateau.out-of-thin-air", 1)]
    public sealed class PlateauOutOfThinAirScene : ISceneScript
    {
        public const uint MissionId = 1190;
        public const uint Escort = 1;
        public const uint EscortStarts = 1, OutOfTheYard = 2, Out = 1;
        public const int PatrolSize = 3;
        public const string Prisoner = "prisoner", Route = "to-west-exit", RouteOperation = "escort";

        public static string PatrolRole(int index) => $"patrol-{index + 1}";

        public SceneDecision Handle(SceneContext context, SceneObservation observation)
        {
            var phase = context.Run.Checkpoint == "{}"
                ? "new"
                : JsonSerializer.Deserialize<Checkpoint>(context.Run.Checkpoint)?.Phase
                    ?? throw new InvalidOperationException("Out of Thin Air checkpoint has no phase.");
            if (phase is not ("new" or "hunting" or "escorting" or "done" or "failed" or "cancelled"))
                throw new InvalidOperationException($"Out of Thin Air checkpoint has an invalid phase {phase}.");

            if (observation.Kind == SceneEventKind.Started && phase == "new" ||
                observation.Kind == SceneEventKind.Recovered && phase == "hunting")
                return Decide("hunting", Enumerable.Range(0, PatrolSize)
                    .Select(index => (WorldIntent)new EnsureActorIntent($"ensure-{PatrolRole(index)}", PatrolRole(index)))
                    // She is the run's from the accept (busy for others), and waits on her rock for the stim pack.
                    .Append(new EnsureActorIntent("ensure-prisoner", Prisoner))
                    .Append(new SetInteractionIntent("prisoner-waits", Prisoner, true)).ToArray());

            if (observation.Kind == SceneEventKind.Signal && observation.SequenceId == EscortStarts && phase == "hunting" ||
                observation.Kind == SceneEventKind.Recovered && phase == "escorting")
                return Decide("escorting", new WorldIntent[]
                {
                    new EnsureActorIntent("ensure-prisoner", Prisoner),
                    new SetInteractionIntent("prisoner-escorted", Prisoner, false),
                    QuestSceneIntents.Escort(RouteOperation, Prisoner, Route)
                });

            if (phase == "escorting" && observation.Kind == SceneEventKind.RouteCompleted &&
                observation.Role == Prisoner && observation.OperationKey == RouteOperation)
                return Decide("done", signals: new[] { new SceneMissionSignal(MissionId, OutOfTheYard, Out) }, status: SceneStatus.Ended);

            if (phase == "escorting" &&
                (observation.Kind == SceneEventKind.ActorDied && observation.Role == Prisoner ||
                 observation.Kind == SceneEventKind.OwnerLost ||
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
            SceneMissionSignal[] signals = null, SceneStatus status = SceneStatus.Running) =>
            new(JsonSerializer.Serialize(new Checkpoint(phase)), world, character, signals, status: status);

        public sealed record Checkpoint(string Phase);
    }
}
