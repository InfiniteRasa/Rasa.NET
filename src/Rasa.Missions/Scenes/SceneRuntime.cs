using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Rasa.Missions.Scenes
{
    public sealed record SceneEvaluation(bool Accepted, SceneDecision Decision, string Rejection);

    public sealed class SceneRuntime
    {
        private readonly SceneScriptRegistry _scripts;
        public SceneRuntime(SceneScriptRegistry scripts) => _scripts = scripts;
        public bool Supports(string key, int version) => _scripts.TryResolve(key, version, out _);

        public SceneEvaluation Evaluate(SceneRun run, SceneBindings bindings, SceneObservation observation, DateTime utcNow)
        {
            if (run.Generation != observation.Generation)
                return Reject($"Run {run.Id} rejected stale generation {observation.Generation}; current {run.Generation}.");
            if (run.Release != bindings.Release)
                return Reject($"Run {run.Id} pins unavailable release {run.Release}.");
            if (!_scripts.TryResolve(run.ScriptKey, run.StateVersion, out var script))
                return Reject($"Run {run.Id} cannot load script {run.ScriptKey} state version {run.StateVersion}.");
            if (run.Status is SceneStatus.Ended or SceneStatus.Faulted or SceneStatus.Resetting)
                return Reject($"Run {run.Id} is {run.Status}.");
            if (utcNow.Kind != DateTimeKind.Utc)
                return Reject("Scene clocks must be UTC.");

            SceneDecision decision;
            try
            {
                decision = script.Handle(new SceneContext(run, bindings, utcNow), observation)
                    ?? throw new InvalidOperationException("Script returned no decision.");
            }
            catch (Exception error)
            {
                return new SceneEvaluation(true,
                    new SceneDecision(run.Checkpoint, status: SceneStatus.Faulted,
                        fault: $"Run {run.Id}, script {run.ScriptKey}/{run.StateVersion}: {error}"), null);
            }

            if (decision.Checkpoint == null || Encoding.UTF8.GetByteCount(decision.Checkpoint) > 16384)
                return Reject("Scene checkpoint must be a JSON object of at most 16384 bytes.");
            try
            {
                using var state = JsonDocument.Parse(decision.Checkpoint);
                if (state.RootElement.ValueKind != JsonValueKind.Object)
                    return Reject("Scene checkpoint must be a JSON object.");
            }
            catch (JsonException error) { return Reject($"Invalid scene checkpoint: {error.Message}"); }

            var keys = new HashSet<string>(StringComparer.Ordinal);
            if (decision.WorldIntents.Count + decision.CharacterIntents.Count + decision.Signals.Count > 64)
                return Reject("Scene decision exceeds the 64-operation transition bound.");
            foreach (var key in decision.WorldIntents.Select(intent => intent.OperationKey)
                .Concat(decision.CharacterIntents.Select(intent => intent.OperationKey)))
                if (string.IsNullOrWhiteSpace(key) || key.Length > 96 || !keys.Add(key))
                    return Reject($"Scene operation key is invalid or duplicated: {key}.");
            foreach (var intent in decision.WorldIntents)
            {
                if (intent.Role != null && !bindings.Actors.ContainsKey(intent.Role))
                    return Reject($"Scene operation {intent.OperationKey} references missing actor role {intent.Role}.");
                if (intent is TransitionObjectStateIntent state &&
                    (string.IsNullOrWhiteSpace(state.Role) || !bindings.Actors.TryGetValue(state.Role, out var actor) ||
                     actor.Kind is not (SceneActorKind.Object or SceneActorKind.PracticeTarget) ||
                     state.State == 0 || state.State > int.MaxValue || state.WindupMilliseconds > int.MaxValue))
                    return Reject($"Scene operation {intent.OperationKey} has an invalid object-state transition.");
                if (intent is RunRouteIntent route &&
                    (!bindings.Routes.TryGetValue(route.Route, out var definition) ||
                     definition.Points.Count == 0 || route.StartWaypoint < 0 || route.StartWaypoint >= definition.Points.Count ||
                     bindings.Actors[route.Role].Kind is SceneActorKind.Object or SceneActorKind.PracticeTarget))
                    return Reject($"Scene operation {intent.OperationKey} references an invalid route or actor.");
                if (intent is AttackActorIntent attack &&
                    (attack.Role == null || !bindings.Actors.TryGetValue(attack.Role, out var attacker) ||
                     attacker.Kind is not (SceneActorKind.Creature or SceneActorKind.PublicSpawn) ||
                     attack.TargetRole != null && (!bindings.Actors.TryGetValue(attack.TargetRole, out var target) ||
                         target.Kind is not (SceneActorKind.Creature or SceneActorKind.PublicSpawn) ||
                         attack.TargetRole == attack.Role)))
                    return Reject($"Scene operation {intent.OperationKey} references an invalid combat actor.");
            }
            var timerNames = new HashSet<string>(StringComparer.Ordinal);
            if (decision.CharacterIntents.OfType<SetCharacterFlagIntent>().Any(intent => intent.FlagId == 0))
                return Reject("Character flag IDs must be nonzero.");
            if (decision.CharacterIntents.OfType<OfferRadioMissionIntent>().Any(intent => intent.MissionId is 0 or > int.MaxValue))
                return Reject("Radio offer intents require a native mission ID.");
            foreach (var timer in decision.Timers)
                if (string.IsNullOrWhiteSpace(timer.Name) || timer.Name.Length > 64 || !timerNames.Add(timer.Name) ||
                    !timer.Cancel && (!timer.DueAtUtc.HasValue || timer.DueAtUtc.Value.Kind != DateTimeKind.Utc))
                    return Reject($"Scene timer is invalid or duplicated: {timer.Name}.");
            return new SceneEvaluation(true, decision, null);
        }

        private static SceneEvaluation Reject(string message) => new(false, null, message);
    }
}
