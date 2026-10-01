using System;
using System.Collections.Generic;
using System.Text.Json;
using Rasa.Data;
using Rasa.Missions.Scenes;

namespace Rasa.Missions.Content.Wilderness
{
    [MissionScript("wilderness.alia-branch", 1)]
    public sealed class WildernessAliaBranchScene : ISceneScript
    {
        public SceneDecision Handle(SceneContext context, SceneObservation observation)
        {
            var phase = context.Run.Checkpoint == "{}"
                ? "new"
                : JsonSerializer.Deserialize<Checkpoint>(context.Run.Checkpoint)?.Phase
                    ?? throw new InvalidOperationException("Alia branch checkpoint has no phase.");

            if (observation.Kind == SceneEventKind.Started && phase == "new")
                return new SceneDecision(Serialize("waiting"), new WorldIntent[]
                {
                    new EnsureActorIntent("reserve-milpas", "milpas"),
                    new SetInteractionIntent("reserve-milpas-interaction", "milpas", false)
                });
            if (observation.Kind == SceneEventKind.Signal && phase == "waiting" &&
                observation.SequenceId is 1 or 2)
            {
                var branch = observation.SequenceId == 1 ? "release" : "arrest";
                return new SceneDecision(Serialize(branch), new WorldIntent[]
                {
                    new RunRouteIntent("escort-" + branch, "milpas", branch, ResumeAfterCombat: true)
                });
            }
            if (observation.Kind == SceneEventKind.RouteCompleted && observation.Role == "milpas" &&
                phase is "release" or "arrest" && observation.OperationKey == "escort-" + phase)
            {
                var escort = phase == "release" ? 8U : 4U;
                var report = phase == "release" ? 11U : 10U;
                return new SceneDecision(Serialize("arrived"), characterIntents: new CharacterIntent[]
                {
                    new ObjectiveIntent("complete-escort", 1390, escort, MissionObjectiveState.Completed),
                    new ObjectiveIntent("reveal-apirka-report", 1390, report, MissionObjectiveState.NotAssigned),
                    new ObjectiveIntent("activate-apirka-report", 1390, report, MissionObjectiveState.Incomplete)
                }, status: SceneStatus.Ended);
            }
            if ((observation.Kind == SceneEventKind.ActorDied && observation.Role == "milpas" ||
                 observation.Kind == SceneEventKind.Cancelled && observation.Name == "route-blocked") &&
                phase is "waiting" or "release" or "arrest")
            {
                var failure = new List<CharacterIntent>();
                if (phase is "release" or "arrest")
                    failure.Add(new ObjectiveIntent("fail-escort", 1390,
                        phase == "release" ? 8U : 4U, MissionObjectiveState.Failed));
                failure.Add(new ObjectiveIntent("fail-branch", 1390, 12, MissionObjectiveState.Failed));
                return new SceneDecision(Serialize("failed"), characterIntents: failure, status: SceneStatus.Ended);
            }
            if (observation.Kind == SceneEventKind.Cancelled)
                return new SceneDecision(Serialize("cancelled"), status: SceneStatus.Ended);
            return new SceneDecision(context.Run.Checkpoint, status: context.Run.Status);
        }

        private static string Serialize(string phase) => JsonSerializer.Serialize(new Checkpoint(phase));

        public sealed record Checkpoint(string Phase);
    }
}
