using System;
using System.Text.Json;
using Rasa.Data;
using Rasa.Missions.Scenes;

namespace Rasa.Missions.Content.Wilderness
{
    [MissionScript("wilderness.escape-velocity", 1)]
    public sealed class WildernessEscapeVelocityScene : ISceneScript
    {
        public SceneDecision Handle(SceneContext context, SceneObservation observation)
        {
            var state = context.Run.Checkpoint == "{}"
                ? new Checkpoint("new", default)
                : JsonSerializer.Deserialize<Checkpoint>(context.Run.Checkpoint)
                    ?? throw new InvalidOperationException("Pierre's rescue checkpoint is missing.");
            if (state.Phase is not ("new" or "captive" or "escorting" or "boarding" or "rescued" or "failed" or "cancelled") ||
                state.Phase != "new" && state.DueAtUtc.Kind != DateTimeKind.Utc)
                throw new InvalidOperationException("Pierre's rescue checkpoint has an invalid phase or deadline.");

            if (observation.Kind == SceneEventKind.Started && state.Phase == "new")
                return Decide(new Checkpoint("captive", context.UtcNow.AddSeconds(420)),
                    new WorldIntent[]
                    {
                        new EnsureActorIntent("ensure-pierre", "pierre"),
                        new SetInteractionIntent("reserve-pierre", "pierre", false),
                        new EnsureActorIntent("ensure-forcefield", "forcefield")
                    });

            if (state.Phase is not ("captive" or "escorting" or "boarding"))
                return new SceneDecision(context.Run.Checkpoint, status: context.Run.Status);
            if (observation.Kind == SceneEventKind.ObjectiveDeadlineElapsed)
                return Fail(state, "departure", failObjective: false);
            if (context.UtcNow >= state.DueAtUtc)
                return Fail(state, "late-arrival");
            if (observation.Kind == SceneEventKind.OwnerLost ||
                observation.Kind == SceneEventKind.ActorDied && observation.Role == "pierre" ||
                observation.Kind == SceneEventKind.Cancelled && observation.Name == "route-blocked")
                return Fail(state, observation.Name ?? observation.Kind.ToString());
            if (observation.Kind == SceneEventKind.Cancelled)
                return Decide(state with { Phase = "cancelled" }, status: SceneStatus.Ended);

            if (observation.Kind == SceneEventKind.Signal && observation.SequenceId == 1 && state.Phase == "captive")
                return Decide(state with { Phase = "escorting" },
                    new WorldIntent[]
                    {
                        new TransitionObjectStateIntent("release-forcefield", "forcefield",
                            context.Bindings.Names["forcefield-open-state"]),
                        new SetInteractionIntent("disable-forcefield", "forcefield", false),
                        new RunRouteIntent("escort-to-lz", "pierre", "landing-zone", ResumeAfterCombat: true)
                    });

            if (observation.Kind == SceneEventKind.RouteCompleted && observation.Role == "pierre")
            {
                if (observation.OperationKey == "escort-to-lz" && state.Phase == "escorting")
                    return AtDestination(context, observation, "landing-zone")
                        ? Decide(state with { Phase = "boarding" },
                            new WorldIntent[]
                            {
                                new RunRouteIntent("board-dropship", "pierre", "boarding", ResumeAfterCombat: true)
                            })
                        : Fail(state, "invalid-lz-arrival");
                if (observation.OperationKey == "board-dropship" && state.Phase == "boarding")
                    return AtDestination(context, observation, "boarding")
                        ? Decide(state with { Phase = "rescued" },
                            character: new CharacterIntent[]
                            {
                                new MissionDeadlineIntent("satisfy-departure", 666, DeadlineIntentKind.Satisfy)
                            }, signals: new[] { new SceneMissionSignal(666, 1, 2) }, status: SceneStatus.Ended)
                        : Fail(state, "invalid-boarding-arrival");
            }
            return new SceneDecision(context.Run.Checkpoint, status: context.Run.Status);
        }

        private static bool AtDestination(SceneContext context, SceneObservation observation, string routeKey)
        {
            if (observation.Position == null)
                return false;
            var route = context.Bindings.Routes[routeKey];
            var destination = route.Points[route.Points.Count - 1].Position;
            var x = observation.Position.X - destination.X;
            var y = observation.Position.Y - destination.Y;
            var z = observation.Position.Z - destination.Z;
            return routeKey == "boarding"
                ? x * x + z * z <= 0.36f && Math.Abs(y) <= 0.35f
                : x * x + y * y + z * z <= 0.25f;
        }

        private static SceneDecision Fail(Checkpoint state, string reason, bool failObjective = true) =>
            Decide(state with { Phase = "failed", Failure = reason },
                character: failObjective
                    ? new CharacterIntent[]
                    {
                        new ObjectiveIntent("fail-pierre-rescue", 666, 1, MissionObjectiveState.Failed),
                        new MissionDeadlineIntent("cancel-departure", 666, DeadlineIntentKind.Cancel)
                    }
                    : Array.Empty<CharacterIntent>(),
                status: SceneStatus.Ended);

        private static SceneDecision Decide(Checkpoint state, WorldIntent[] world = null,
            CharacterIntent[] character = null, SceneMissionSignal[] signals = null,
            SceneStatus status = SceneStatus.Running) =>
            new(JsonSerializer.Serialize(state), world, character, signals, status: status);

        public sealed record Checkpoint(string Phase, DateTime DueAtUtc, string Failure = null);
    }
}
