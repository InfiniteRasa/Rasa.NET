using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Linq;

namespace Rasa.Missions.Scenes
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class MissionScriptAttribute : Attribute
    {
        public string Key { get; }
        public int StateVersion { get; }
        public MissionScriptAttribute(string key, int stateVersion) { Key = key; StateVersion = stateVersion; }
    }

    public interface ISceneScript
    {
        SceneDecision Handle(SceneContext context, SceneObservation observation);
    }

    public sealed class SceneScriptRegistry
    {
        private readonly Dictionary<string, (int Version, ISceneScript Script)> _scripts = new(StringComparer.Ordinal);
        public SceneScriptRegistry() => Discover(typeof(SceneScriptRegistry).Assembly);
        public void Discover(Assembly assembly)
        {
            foreach (var type in assembly.GetTypes().OrderBy(type => type.FullName, StringComparer.Ordinal))
            {
                var registration = type.GetCustomAttribute<MissionScriptAttribute>();
                if (registration == null)
                    continue;
                if (type.IsAbstract || !typeof(ISceneScript).IsAssignableFrom(type))
                    throw new ArgumentException($"Script factory {type.FullName} does not implement ISceneScript.");
                Register(registration.Key, registration.StateVersion, (ISceneScript)Activator.CreateInstance(type));
            }
        }
        public void Register(string key, int version, ISceneScript script)
        {
            if (string.IsNullOrWhiteSpace(key) || key.Length > 64 || version < 1 || script == null)
                throw new ArgumentException("A script requires a stable key and a positive state schema version.");
            _scripts.Add(key, (version, script));
        }
        public bool TryResolve(string key, int version, out ISceneScript script)
        {
            script = null;
            if (!_scripts.TryGetValue(key, out var found) || found.Version != version)
                return false;
            script = found.Script;
            return true;
        }
    }

    [MissionScript("data.sequence", 1)]
    public sealed class DataSequenceScript : ISceneScript
    {
        public SceneDecision Handle(SceneContext context, SceneObservation observation)
        {
            if (observation.Kind is not (SceneEventKind.Signal or SceneEventKind.TimerElapsed or SceneEventKind.Started) ||
                !context.Bindings.Sequences.TryGetValue(observation.SequenceId, out var sequence))
                return new SceneDecision(context.Run.Checkpoint);
            return new SceneDecision(JsonSerializer.Serialize(new { sequence = observation.SequenceId }),
                sequence.WorldIntents, sequence.CharacterIntents, sequence.Signals,
                sequence.Timers.Select(timer => new SceneTimerChange(timer.Name, timer.ClockPolicy,
                    timer.Cancel ? null : context.UtcNow.AddMilliseconds(timer.Milliseconds),
                    timer.SequenceId, timer.Cancel)));
        }
    }
}
