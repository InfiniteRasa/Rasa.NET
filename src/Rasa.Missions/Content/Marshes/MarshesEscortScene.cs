using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Rasa.Data;
using Rasa.Missions.Scenes;

namespace Rasa.Missions.Content.Marshes
{
    /// <summary>
    /// The Marshes escorts (Forean Flu 497, Tread Lightly 888, AWOL Andy 889, Rest and Recreation 1023): one or more legs,
    /// each a group of the run's own creatures that follow the run's owner to a place. The mission data does the rest: an
    /// area at the place completes the leg's objective and starts the leg's arrival scenario, a talk or the scene's start
    /// starts the next leg.
    ///
    /// The legs are read from the scene's names (no code per mission):
    ///  - <c>mission</c>: the mission id; <c>legs</c>: how many;
    ///  - <c>leg.K.objective</c>: the objective leg K fails when its whole group is dead;
    ///  - <c>leg.K.start</c>: the sequence (a mission scenario) that sets the group following; 0: at the scene's start;
    ///  - <c>leg.K.arrived</c>: the sequence the arrival signals: the group stops and is taken away;
    ///  - <c>leg.K.stand</c> (optional): the group stands at its place from the scene's start until its start, and its
    ///    death before then fails this objective (889: Bruncke waits to be found).
    /// The group of leg K is the actors whose role starts with <c>leg-K-</c>. A death comes as the actor's defeat sequence
    /// (a follower is on no route) or as ActorDied. A leg fails only when none of its group is left (1023: "make sure at
    /// least one of them gets to Belfast"). An objective's deadline (MissionDeadlineService) fails it by itself; the
    /// scene then takes the group away.
    /// </summary>
    [MissionScript("marshes.escort", 1)]
    public sealed class MarshesEscortScene : ISceneScript
    {
        public const string Legs = "legs", Mission = "mission";
        public static string Objective(int leg) => $"leg.{leg}.objective";
        public static string Start(int leg) => $"leg.{leg}.start";
        public static string Arrived(int leg) => $"leg.{leg}.arrived";
        public static string Stand(int leg) => $"leg.{leg}.stand";
        public static string RolePrefix(int leg) => $"leg-{leg}-";
        public static string Role(int leg, int member) => $"leg-{leg}-{member}";

        public SceneDecision Handle(SceneContext context, SceneObservation observation)
        {
            var names = context.Bindings.Names;
            if (!names.TryGetValue(Legs, out var legs) || legs == 0)
                throw new InvalidOperationException("A Marshes escort needs its legs.");
            var state = context.Run.Checkpoint == "{}"
                ? new Checkpoint("new", 1, Array.Empty<string>())
                : JsonSerializer.Deserialize<Checkpoint>(context.Run.Checkpoint)
                    ?? throw new InvalidOperationException("Marshes escort checkpoint is missing.");
            if (state.Phase is not ("new" or "standing" or "waiting" or "escorting" or "done" or "failed" or "cancelled") ||
                state.Leg < 1 || state.Leg > legs || state.Dead == null)
                throw new InvalidOperationException($"Marshes escort checkpoint has an invalid phase {state.Phase}.");

            var group = Group(context, state.Leg);
            uint Name(string key) => names.TryGetValue(key, out var value) ? value : 0;

            if (observation.Kind == SceneEventKind.Started && state.Phase == "new")
                return Enter(context, state with { Phase = "waiting" }, state.Leg);

            if (observation.Kind == SceneEventKind.Recovered)
                return state.Phase switch
                {
                    "escorting" => Decide(state, Living(group, state).SelectMany(role => Follow(role, context)).ToArray()),
                    "standing" => Decide(state, Living(group, state).Select(role => (WorldIntent)new EnsureActorIntent($"ensure-{role}", role)).ToArray()),
                    _ => Unchanged(context)
                };

            if (observation.Kind == SceneEventKind.Cancelled)
                return Decide(state with { Phase = state.Phase is "done" or "failed" ? state.Phase : "cancelled" }, status: SceneStatus.Ended);

            if (state.Phase is "done" or "failed" or "cancelled")
                return Unchanged(context);

            if (observation.Kind == SceneEventKind.ObjectiveDeadlineElapsed && state.Phase == "escorting")
                return Decide(state with { Phase = "failed" }, TakeAway(Living(group, state)), status: SceneStatus.Ended);

            // A death: the actor's defeat sequence, or ActorDied.
            var died = observation.Kind == SceneEventKind.ActorDied ? observation.Role
                : observation.Kind == SceneEventKind.Signal
                    ? context.Bindings.DefeatSequences.FirstOrDefault(entry => entry.Value == observation.SequenceId).Key
                    : null;
            if (died != null)
            {
                if (!group.Contains(died) || state.Dead.Contains(died) || state.Phase is not ("escorting" or "standing"))
                    return Unchanged(context);
                var dead = state.Dead.Append(died).OrderBy(role => role, StringComparer.Ordinal).ToArray();
                if (group.Any(role => !dead.Contains(role)))
                    return Decide(state with { Dead = dead });
                var objective = state.Phase == "standing" ? Name(Stand(state.Leg)) : Name(Objective(state.Leg));
                return Decide(state with { Phase = "failed", Dead = dead }, character: new CharacterIntent[]
                {
                    new ObjectiveIntent($"leg-{state.Leg}-lost", Name(Mission), objective, MissionObjectiveState.Failed)
                }, status: SceneStatus.Ended);
            }

            if (observation.Kind != SceneEventKind.Signal)
                return Unchanged(context);

            if (state.Phase is "waiting" or "standing" && observation.SequenceId == Name(Start(state.Leg)) && observation.SequenceId != 0)
                return Decide(state with { Phase = "escorting" }, Living(group, state).SelectMany(role => Follow(role, context)).ToArray());

            if (state.Phase == "escorting" && observation.SequenceId == Name(Arrived(state.Leg)))
            {
                var away = TakeAway(Living(group, state));
                if (state.Leg == legs)
                    return Decide(state with { Phase = "done" }, away, status: SceneStatus.Ended);
                var next = Enter(context, state with { Phase = "waiting", Leg = state.Leg + 1, Dead = Array.Empty<string>() }, state.Leg + 1);
                return new SceneDecision(next.Checkpoint, away.Concat(next.WorldIntents), status: next.Status);
            }

            return Unchanged(context);
        }

        /// <summary>A leg comes up: following at once (start 0), standing (stand), or waiting for its start.</summary>
        private SceneDecision Enter(SceneContext context, Checkpoint state, int leg)
        {
            var names = context.Bindings.Names;
            var group = Group(context, leg);
            if (!names.TryGetValue(Start(leg), out var start) || start == 0)
                return Decide(state with { Phase = "escorting" }, group.SelectMany(role => Follow(role, context)).ToArray());
            if (names.ContainsKey(Stand(leg)))
                return Decide(state with { Phase = "standing" },
                    group.Select(role => (WorldIntent)new EnsureActorIntent($"ensure-{role}", role)).ToArray());
            return Decide(state);
        }

        private static IEnumerable<WorldIntent> Follow(string role, SceneContext context) => new WorldIntent[]
        {
            new EnsureActorIntent($"ensure-{role}", role),
            new SetInteractionIntent($"{role}-escorted", role, false),
            new FollowActorIntent($"{role}-follows", role, 0)
        };

        private static WorldIntent[] TakeAway(IEnumerable<string> roles) => roles.SelectMany(role => new WorldIntent[]
        {
            new FollowActorIntent($"{role}-stops", role, 0, Enabled: false),
            new RemoveActorIntent($"{role}-gone", role)
        }).ToArray();

        private static string[] Group(SceneContext context, int leg) => context.Bindings.Actors.Keys
            .Where(role => role.StartsWith(RolePrefix(leg), StringComparison.Ordinal))
            .OrderBy(role => role, StringComparer.Ordinal).ToArray();

        private static IEnumerable<string> Living(IEnumerable<string> group, Checkpoint state) =>
            group.Where(role => !state.Dead.Contains(role));

        private static SceneDecision Unchanged(SceneContext context) => new(context.Run.Checkpoint, status: context.Run.Status);

        private static SceneDecision Decide(Checkpoint state, WorldIntent[] world = null, CharacterIntent[] character = null,
            SceneStatus status = SceneStatus.Running) =>
            new(JsonSerializer.Serialize(state), world, character, status: status);

        public sealed record Checkpoint(string Phase, int Leg, string[] Dead);
    }
}
