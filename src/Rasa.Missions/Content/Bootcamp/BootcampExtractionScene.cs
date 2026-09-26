using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Rasa.Missions.Scenes;

namespace Rasa.Missions.Content.Bootcamp
{
    internal static class BootcampExtractionScene
    {
        internal static SceneDecision Handle(SceneContext context, SceneObservation observation)
        {
            if (!context.Bindings.Names.TryGetValue("assault", out var assault))
                return BootcampSequence.Apply(context, observation);
            var state = JsonSerializer.Deserialize<Checkpoint>(context.Run.Checkpoint);
            var world = new List<WorldIntent>();
            var character = new List<CharacterIntent>();
            var signals = new List<SceneMissionSignal>();
            var timers = new List<SceneTimerChange>();

            if (observation.Kind == SceneEventKind.RouteCompleted &&
                observation.Role != null && context.Bindings.DefeatSequences.ContainsKey(observation.Role))
                world.Add(new AttackActorIntent($"engage-{observation.Role}", observation.Role));

            if (observation.Kind is SceneEventKind.Signal or SceneEventKind.TimerElapsed or SceneEventKind.Started)
            {
                var sequence = observation.SequenceId;
                state.Sequence = sequence;
                if (context.Bindings.DefeatSequences.Values.Contains(sequence))
                {
                    if (state.AssaultStarted)
                        state.Defeated.Add(sequence);
                }
                else
                {
                    Append(sequence);
                    if (!state.AssaultStarted &&
                        (sequence == context.Bindings.Names["fuse"] ||
                         sequence == context.Bindings.Names["detonation"] ||
                         sequence == context.Bindings.Names["exit"]))
                    {
                        state.AssaultStarted = true;
                        Append(assault);
                    }
                    if (sequence == context.Bindings.Names["exit"])
                        state.Arrived = true;
                }
                if (state.Arrived && !state.Cleared &&
                    context.Bindings.DefeatSequences.Values.All(state.Defeated.Contains))
                {
                    state.Cleared = true;
                    Append(context.Bindings.Names["assault-cleared"]);
                }
            }
            return new SceneDecision(JsonSerializer.Serialize(state), world, character, signals, timers);

            void Append(uint sequence)
            {
                var decision = BootcampSequence.Apply(context,
                    new SceneObservation(SceneEventKind.Signal, context.Run.Generation, SequenceId: sequence));
                world.AddRange(decision.WorldIntents);
                character.AddRange(decision.CharacterIntents);
                signals.AddRange(decision.Signals);
                timers.AddRange(decision.Timers);
            }
        }

        private sealed class Checkpoint
        {
            [JsonPropertyName("sequence")] public uint Sequence { get; set; }
            public bool AssaultStarted { get; set; }
            public bool Arrived { get; set; }
            public bool Cleared { get; set; }
            public HashSet<uint> Defeated { get; set; } = new();
        }
    }
}
