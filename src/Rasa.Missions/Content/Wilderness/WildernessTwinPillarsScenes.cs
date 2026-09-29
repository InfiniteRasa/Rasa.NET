using System.Text.Json;
using Rasa.Missions.Scenes;

namespace Rasa.Missions.Content.Wilderness
{
    [MissionScript(WildernessSmugglerDeliveryScene.ScriptKey, 1)]
    public sealed class WildernessSmugglerDeliveryScene : ISceneScript
    {
        public const string ScriptKey = "wilderness.smuggler-deliveries.v1";

        public SceneDecision Handle(SceneContext context, SceneObservation observation)
        {
            if (observation.Kind == SceneEventKind.Cancelled)
                return new SceneDecision(context.Run.Checkpoint, status: SceneStatus.Ended);
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
