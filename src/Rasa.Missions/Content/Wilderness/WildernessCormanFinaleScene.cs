using System;
using System.Collections.Generic;
using System.Text.Json;
using Rasa.Data;
using Rasa.Missions.Scenes;

namespace Rasa.Missions.Content.Wilderness
{
    [MissionScript("wilderness.corman-finale", 1)]
    public sealed class WildernessCormanFinaleScene : ISceneScript
    {
        public SceneDecision Handle(SceneContext context, SceneObservation observation)
        {
            var state = context.Run.Checkpoint == "{}"
                ? new Checkpoint("burke", 0)
                : JsonSerializer.Deserialize<Checkpoint>(context.Run.Checkpoint)
                    ?? throw new InvalidOperationException("Skeev encounter checkpoint is missing.");
            var valid = state.Phase switch
            {
                "burke" or "ultimatum" or "failed" => state.Outcome == 0,
                "fight" or "victory" => state.Outcome is 1 or 27,
                "surrendered" => state.Outcome == 26,
                _ => false
            };
            if (!valid)
                throw new InvalidOperationException("Skeev encounter checkpoint is invalid.");
            if (observation.Kind == SceneEventKind.Cancelled)
                return new SceneDecision(JsonSerializer.Serialize(state), status: SceneStatus.Ended);
            if (observation.Kind == SceneEventKind.OwnerLost)
                return new SceneDecision(JsonSerializer.Serialize(state), status: SceneStatus.Waiting);
            if (observation.Kind == SceneEventKind.Started)
                return Apply(context, state, 0);
            if (observation.Kind == SceneEventKind.Recovered)
                return state.Phase == "fight"
                    ? new SceneDecision(JsonSerializer.Serialize(state),
                        context.Bindings.Sequences[state.Outcome == 27 ? 4U : 3U].WorldIntents)
                    : new SceneDecision(JsonSerializer.Serialize(state));
            if (observation.Kind == SceneEventKind.ActorDied && observation.Role == "skeev" ||
                observation.Kind == SceneEventKind.Signal && observation.SequenceId == 5)
            {
                if (state.Phase == "fight")
                    return new SceneDecision(JsonSerializer.Serialize(state with { Phase = "victory" }),
                        signals: new[] { new SceneMissionSignal(701, 5, 1) }, status: SceneStatus.Ended);
                return new SceneDecision(JsonSerializer.Serialize(state with { Phase = "failed" }),
                    characterIntents: new CharacterIntent[]
                    {
                        new ObjectiveIntent("skeev-died-before-decision", 701,
                            state.Phase == "burke" ? 1U : 2U, MissionObjectiveState.Failed)
                    }, status: SceneStatus.Ended);
            }
            if (observation.Kind != SceneEventKind.Signal)
                return new SceneDecision(JsonSerializer.Serialize(state), status: context.Run.Status);

            if (observation.SequenceId == 1 && state.Phase == "burke")
                return Apply(context, state with { Phase = "ultimatum" }, 1);
            if (state.Phase != "ultimatum")
                return new SceneDecision(JsonSerializer.Serialize(state), status: context.Run.Status);
            if (observation.SequenceId == 2)
                return Apply(context, new Checkpoint("surrendered", 26), 2, SceneStatus.Ended);
            if (observation.SequenceId == 3)
                return Apply(context, new Checkpoint("fight", 1), 3);
            if (observation.SequenceId == 4)
            {
                var sequence = context.Bindings.Sequences[4];
                var character = new List<CharacterIntent>(sequence.CharacterIntents)
                {
                    new ObjectiveIntent("timeout-reveals-fight", 701, 3, MissionObjectiveState.NotAssigned),
                    new ObjectiveIntent("timeout-activates-fight", 701, 3, MissionObjectiveState.Incomplete),
                    new ObjectiveIntent("timeout-closes-skeev-choice", 701, 2, MissionObjectiveState.Completed)
                };
                return new SceneDecision(JsonSerializer.Serialize(new Checkpoint("fight", 27)),
                    sequence.WorldIntents, character, sequence.Signals);
            }
            return new SceneDecision(JsonSerializer.Serialize(state), status: context.Run.Status);
        }

        private static SceneDecision Apply(SceneContext context, Checkpoint state, uint id,
            SceneStatus status = SceneStatus.Running)
        {
            var sequence = context.Bindings.Sequences[id];
            return new SceneDecision(JsonSerializer.Serialize(state), sequence.WorldIntents,
                sequence.CharacterIntents, sequence.Signals, status: status);
        }

        public sealed record Checkpoint(string Phase, uint Outcome);
    }
}
