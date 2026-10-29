using System;
using System.Text.Json;
using Rasa.Data;
using Rasa.Missions.Scenes;

namespace Rasa.Missions.Content.Plateau
{
    /// <summary>
    /// Clean Slate (1983), Plateau: Wainwright's amnesiac soldier walks to Special Agent Perdu at the Fort
    /// Defiance gate (objective 1), goes berserk there (4), and once he is dead Perdu talks to the player (5).
    /// The server cannot turn a friendly creature hostile, so the berserk soldier is a hostile copy that
    /// takes the walker's place at the route's end and attacks the run's owner.
    /// </summary>
    [MissionScript("plateau.clean-slate", 1)]
    public sealed class PlateauCleanSlateScene : ISceneScript
    {
        public const uint MissionId = 1983;
        public const uint Escort = 1, Kill = 4, Report = 5;
        public const uint BerserkDefeated = 1;

        public SceneDecision Handle(SceneContext context, SceneObservation observation)
        {
            var phase = context.Run.Checkpoint == "{}"
                ? "new"
                : JsonSerializer.Deserialize<Checkpoint>(context.Run.Checkpoint)?.Phase
                    ?? throw new InvalidOperationException("Clean Slate checkpoint has no phase.");
            if (phase is not ("new" or "escorting" or "berserk" or "done" or "failed" or "cancelled"))
                throw new InvalidOperationException($"Clean Slate checkpoint has an invalid phase {phase}.");

            if (observation.Kind == SceneEventKind.Started && phase == "new" ||
                observation.Kind == SceneEventKind.Recovered && phase == "escorting")
                return Decide("escorting", new WorldIntent[]
                {
                    new EnsureActorIntent("ensure-soldier", "soldier"),
                    new SetInteractionIntent("soldier-walks", "soldier", false),
                    QuestSceneIntents.Escort("escort", "soldier", "to-perdu")
                });
            if (observation.Kind == SceneEventKind.Recovered && phase == "berserk")
                return Decide("berserk", Berserk());

            if (observation.Kind == SceneEventKind.RouteCompleted && observation.Role == "soldier" &&
                observation.OperationKey == "escort" && phase == "escorting")
                return Decide("berserk", Berserk(), new CharacterIntent[]
                {
                    new ObjectiveIntent("escort-arrived", MissionId, Escort, MissionObjectiveState.Completed),
                    new ObjectiveIntent("reveal-kill", MissionId, Kill, MissionObjectiveState.NotAssigned),
                    new ObjectiveIntent("activate-kill", MissionId, Kill, MissionObjectiveState.Incomplete)
                });

            if (phase == "berserk" &&
                (observation.Kind == SceneEventKind.Signal && observation.SequenceId == BerserkDefeated ||
                 observation.Kind == SceneEventKind.ActorDied && observation.Role == "berserk"))
                return Decide("done", character: new CharacterIntent[]
                {
                    new ObjectiveIntent("berserk-killed", MissionId, Kill, MissionObjectiveState.Completed),
                    new ObjectiveIntent("reveal-report", MissionId, Report, MissionObjectiveState.NotAssigned),
                    new ObjectiveIntent("activate-report", MissionId, Report, MissionObjectiveState.Incomplete)
                }, status: SceneStatus.Ended);

            if (phase == "escorting" &&
                (observation.Kind == SceneEventKind.ActorDied && observation.Role == "soldier" ||
                 observation.Kind == SceneEventKind.Cancelled && observation.Name == "route-blocked"))
                return Decide("failed", character: new CharacterIntent[]
                {
                    new ObjectiveIntent("escort-failed", MissionId, Escort, MissionObjectiveState.Failed)
                }, status: SceneStatus.Ended);

            if (observation.Kind == SceneEventKind.Cancelled)
                return Decide("cancelled", status: SceneStatus.Ended);
            return new SceneDecision(context.Run.Checkpoint, status: context.Run.Status);
        }

        /// <summary>The walker goes; his berserk copy stands where he stopped and attacks the run's owner.</summary>
        private static WorldIntent[] Berserk() => new WorldIntent[]
        {
            new RemoveActorIntent("remove-soldier", "soldier"),
            new EnsureActorIntent("ensure-berserk", "berserk"),
            QuestSceneIntents.Attack("berserk-attacks", "berserk")
        };

        private static SceneDecision Decide(string phase, WorldIntent[] world = null, CharacterIntent[] character = null,
            SceneStatus status = SceneStatus.Running) =>
            new(JsonSerializer.Serialize(new Checkpoint(phase)), world, character, status: status);

        public sealed record Checkpoint(string Phase);
    }
}
