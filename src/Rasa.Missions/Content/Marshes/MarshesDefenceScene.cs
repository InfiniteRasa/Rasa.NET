using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Rasa.Data;
using Rasa.Missions.Scenes;

namespace Rasa.Missions.Content.Marshes
{
    /// <summary>
    /// A Marshes defence (Trapped! 1052): a group of the run's own friendly creatures (the wards) stands from the scene's
    /// start, its first one (<see cref="Leader"/>) the one the player finds; once the defence starts, waves of the run's own
    /// hostiles come in and attack the wards, and when the hold time is over the wards are free: the mission hears the
    /// outcome signal and the wards and what is left of the waves go.
    ///
    /// Read from the scene's names (no code per mission):
    ///  - <c>mission</c>; <c>locate</c>: the objective the leader's death fails before the defence; <c>defend</c>: the one it
    ///    fails during it;
    ///  - <c>start</c>: the sequence (a mission scenario) that starts the defence;
    ///  - <c>waves</c>: how many; <c>wave.K.after</c>: milliseconds from the start to wave K;
    ///  - <c>hold</c>: milliseconds from the start to the end of the defence; <c>outcome</c> and <c>step</c>: the mission
    ///    signal sent then (a scenario and its event);
    ///  - <c>hide</c> (optional): a public spawn pool the leader stands in for, hidden from the owner while he stands.
    /// The wards are the actors <c>ward-M</c> (<c>ward-1</c> the leader), wave K's are <c>wave-K-M</c>. A death comes as the
    /// actor's defeat sequence or as ActorDied; only the leader's matters. The other wards' deaths, and the waves', change
    /// nothing.
    /// </summary>
    [MissionScript("marshes.defence", 1)]
    public sealed class MarshesDefenceScene : ISceneScript
    {
        public const string Mission = "mission", Locate = "locate", Defend = "defend", Start = "start", Waves = "waves",
            Hold = "hold", Outcome = "outcome", Step = "step", Hide = "hide", Spare = "spare";
        public const string Leader = "ward-1";
        public const uint HoldTimerSequence = 999;
        public static string After(int wave) => $"wave.{wave}.after";
        public static string Ward(int member) => $"ward-{member}";
        public static string WaveRole(int wave, int member) => $"wave-{wave}-{member}";
        public static uint WaveTimerSequence(int wave) => 900 + (uint)wave;

        public SceneDecision Handle(SceneContext context, SceneObservation observation)
        {
            var names = context.Bindings.Names;
            uint Name(string key) => names.TryGetValue(key, out var value) ? value : 0;
            var state = context.Run.Checkpoint == "{}"
                ? new Checkpoint("new", 0, Array.Empty<string>())
                : JsonSerializer.Deserialize<Checkpoint>(context.Run.Checkpoint)
                    ?? throw new InvalidOperationException("Marshes defence checkpoint is missing.");
            if (state.Phase is not ("new" or "standing" or "defending" or "done" or "failed" or "cancelled") || state.Dead == null)
                throw new InvalidOperationException($"Marshes defence checkpoint has an invalid phase {state.Phase}.");
            var wards = Roles(context, "ward-");

            if (observation.Kind == SceneEventKind.Started && state.Phase == "new")
                return Decide(state with { Phase = "standing" }, Stand(context, wards));

            if (observation.Kind == SceneEventKind.Cancelled)
                return Decide(state with { Phase = state.Phase is "done" or "failed" ? state.Phase : "cancelled" }, status: SceneStatus.Ended);

            if (state.Phase is "done" or "failed" or "cancelled" or "new")
                return Unchanged(context);

            if (observation.Kind == SceneEventKind.Recovered)
            {
                var world = Stand(context, wards.Where(role => !state.Dead.Contains(role))).ToList();
                if (state.Phase == "defending")
                    world.AddRange(Enumerable.Range(1, state.Waves).SelectMany(wave => Attackers(context, wave, state)));
                return Decide(state, world.ToArray());
            }

            var died = observation.Kind == SceneEventKind.ActorDied ? observation.Role
                : observation.Kind == SceneEventKind.Signal
                    ? (context.Bindings.DefeatSequences ?? new Dictionary<string, uint>())
                        .FirstOrDefault(entry => entry.Value == observation.SequenceId && observation.SequenceId != 0).Key
                    : null;
            if (died != null)
            {
                if (state.Dead.Contains(died))
                    return Unchanged(context);
                var dead = state.Dead.Append(died).OrderBy(role => role, StringComparer.Ordinal).ToArray();
                if (died != Leader)
                {
                    var next = state with { Dead = dead };
                    // A ward down: with spare its attackers go for the wards left, the leader last.
                    return state.Phase == "defending" && Name(Spare) != 0 && wards.Contains(died)
                        ? Decide(next, Enumerable.Range(1, next.Waves).SelectMany(wave => Attackers(context, wave, next)).ToArray())
                        : Decide(next);
                }
                var objective = state.Phase == "standing" ? Name(Locate) : Name(Defend);
                return Decide(state with { Phase = "failed", Dead = dead }, Away(context, state, wards),
                    new CharacterIntent[] { new ObjectiveIntent("leader-lost", Name(Mission), objective, MissionObjectiveState.Failed) },
                    status: SceneStatus.Ended);
            }

            if (state.Phase == "standing" && observation.Kind == SceneEventKind.Signal && observation.SequenceId != 0 &&
                observation.SequenceId == Name(Start))
            {
                var timers = Enumerable.Range(1, (int)Name(Waves))
                    .Where(wave => Name(After(wave)) > 0)
                    .Select(wave => new SceneTimerChange($"wave-{wave}", SceneClockPolicy.WallClock,
                        context.UtcNow.AddMilliseconds(Name(After(wave))), WaveTimerSequence(wave)))
                    .Append(new SceneTimerChange("hold", SceneClockPolicy.WallClock, context.UtcNow.AddMilliseconds(Name(Hold)),
                        HoldTimerSequence))
                    .ToArray();
                var now = Enumerable.Range(1, (int)Name(Waves)).Where(wave => Name(After(wave)) == 0).ToArray();
                var next = state with { Phase = "defending", Waves = now.Length == 0 ? 0 : now.Max() };
                return Decide(next, now.SelectMany(wave => Attackers(context, wave, next)).ToArray(), timers: timers);
            }

            if (state.Phase == "defending" && observation.Kind == SceneEventKind.TimerElapsed)
            {
                if (observation.SequenceId == HoldTimerSequence)
                    return Decide(state with { Phase = "done" }, Away(context, state, wards),
                        signals: new[] { new SceneMissionSignal(Name(Mission), Name(Outcome), Name(Step)) },
                        status: SceneStatus.Ended);
                var wave = Enumerable.Range(1, (int)Name(Waves)).FirstOrDefault(candidate => WaveTimerSequence(candidate) == observation.SequenceId);
                if (wave > state.Waves)
                {
                    var next = state with { Waves = wave };
                    return Decide(next, Attackers(context, wave, next).ToArray());
                }
            }

            return Unchanged(context);
        }

        /// <summary>The wards stand (and the leader hides the public one he stands in for).</summary>
        private static IEnumerable<WorldIntent> Stand(SceneContext context, IEnumerable<string> wards)
        {
            foreach (var role in wards)
            {
                yield return new EnsureActorIntent($"ensure-{role}", role);
                if (role == Leader && context.Bindings.Names.TryGetValue(Hide, out var pool) && pool != 0)
                    yield return new HideSpawnPoolsIntent("public-leader-hidden", Leader, new[] { pool });
            }
        }

        /// <summary>Wave <paramref name="wave"/>'s living members, there and attacking the wards in turn.</summary>
        private static IEnumerable<WorldIntent> Attackers(SceneContext context, int wave, Checkpoint state)
        {
            var wards = Roles(context, "ward-");
            var targets = wards.Where(role => !state.Dead.Contains(role)).ToArray();
            var spare = context.Bindings.Names.TryGetValue(Spare, out var spared) && spared != 0;
            if (spare && targets.Any(role => role != Leader))
                targets = targets.Where(role => role != Leader).ToArray();
            // With spare a ward's death sends the attackers again: each new order its own operation.
            var down = spare ? wards.Count(state.Dead.Contains) : 0;
            var members = Roles(context, $"wave-{wave}-").Where(role => !state.Dead.Contains(role)).ToArray();
            for (var index = 0; index < members.Length; index++)
            {
                yield return new EnsureActorIntent($"ensure-{members[index]}", members[index]);
                if (targets.Length > 0)
                    yield return QuestSceneIntents.Attack(down == 0 ? $"{members[index]}-attacks" : $"{members[index]}-attacks-{down}",
                        members[index], targets[index % targets.Length]);
            }
        }

        /// <summary>The wards and the waves come out alive go.</summary>
        private static WorldIntent[] Away(SceneContext context, Checkpoint state, IEnumerable<string> wards) =>
            wards.Concat(Enumerable.Range(1, state.Waves).SelectMany(wave => Roles(context, $"wave-{wave}-")))
                .Where(role => !state.Dead.Contains(role))
                .Select(role => (WorldIntent)new RemoveActorIntent($"{role}-gone", role)).ToArray();

        private static string[] Roles(SceneContext context, string prefix) => context.Bindings.Actors.Keys
            .Where(role => role.StartsWith(prefix, StringComparison.Ordinal))
            .OrderBy(role => role, StringComparer.Ordinal).ToArray();

        private static SceneDecision Unchanged(SceneContext context) => new(context.Run.Checkpoint, status: context.Run.Status);

        private static SceneDecision Decide(Checkpoint state, IEnumerable<WorldIntent> world = null, CharacterIntent[] character = null,
            SceneMissionSignal[] signals = null, SceneTimerChange[] timers = null, SceneStatus status = SceneStatus.Running) =>
            new(JsonSerializer.Serialize(state), world, character, signals, timers, status: status);

        /// <summary>The phase, the last wave sent, the dead roles.</summary>
        public sealed record Checkpoint(string Phase, int Waves, string[] Dead);
    }
}
