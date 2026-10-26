using System.Collections.Generic;
using System.Text.Json;
using Rasa.Data;
using Rasa.Missions.Scenes;

namespace Rasa.Missions.Content.Wilderness
{
    [MissionScript(WildernessSmugglerDeliveryScene.ScriptKey, 1)]
    public sealed class WildernessSmugglerDeliveryScene : ISceneScript, ISceneRecoveryObjectiveFacts
    {
        public const string ScriptKey = "wilderness.smuggler-deliveries.v1";

        public SceneDecision Handle(SceneContext context, SceneObservation observation)
        {
            if (observation.Kind == SceneEventKind.Cancelled)
                return new SceneDecision(context.Run.Checkpoint, status: SceneStatus.Ended);
            if (observation.Kind == SceneEventKind.Recovered && context.ObjectiveStates is { } objectives)
            {
                var deliveries = 0U;
                for (uint objective = 1; objective <= 3; objective++)
                    if (objectives.TryGetValue(objective, out var completed) && completed == MissionObjectiveState.Completed)
                        deliveries |= 1U << (int)(objective - 1);
                var recovered = new DeliveryState { Deliveries = deliveries, Ready = deliveries == 7 };
                var intents = new List<CharacterIntent>();
                if (recovered.Ready && objectives.TryGetValue(4, out var moore) &&
                    objectives.TryGetValue(5, out var batch) && (batch is MissionObjectiveState.Incomplete or MissionObjectiveState.Completed) &&
                    (moore is MissionObjectiveState.Inactive or MissionObjectiveState.NotAssigned or
                        MissionObjectiveState.Incomplete or MissionObjectiveState.Completed))
                {
                    // Recovery has its own receipts: an old delivery receipt must not hide a
                    // missing return stage. It never consumes parcels or grants a reward.
                    if (batch != MissionObjectiveState.Completed)
                        intents.Add(new ObjectiveIntent("recover-smuggler-batch-v1", 623, 5, MissionObjectiveState.Completed));
                    if (moore == MissionObjectiveState.Inactive)
                        intents.Add(new ObjectiveIntent("recover-smuggler-reveal-moore-v1", 623, 4, MissionObjectiveState.NotAssigned));
                    if (moore is MissionObjectiveState.Inactive or MissionObjectiveState.NotAssigned)
                        intents.Add(new ObjectiveIntent("recover-smuggler-activate-moore-v1", 623, 4, MissionObjectiveState.Incomplete));
                }
                return new SceneDecision(JsonSerializer.Serialize(recovered), characterIntents: intents);
            }
            if (observation.Kind != SceneEventKind.Signal || observation.SequenceId is < 1 or > 3)
                return new SceneDecision(context.Run.Checkpoint);

            var state = JsonSerializer.Deserialize<DeliveryState>(context.Run.Checkpoint) ?? new DeliveryState();
            var flag = 1U << (int)(observation.SequenceId - 1);
            if ((state.Deliveries & flag) != 0 || state.Ready)
                return new SceneDecision(context.Run.Checkpoint);

            state.Deliveries |= flag;
            state.Ready = state.Deliveries == 7;
            var sequence = state.Ready ? context.Bindings.Sequences[4] : null;
            return new SceneDecision(JsonSerializer.Serialize(state),
                characterIntents: sequence?.CharacterIntents);
        }

        public sealed class DeliveryState
        {
            public uint Deliveries { get; set; }
            public bool Ready { get; set; }
        }
    }
}
