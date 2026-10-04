using System;
using System.Linq;
using System.Text.Json;
using Rasa.Missions.Scenes;

namespace Rasa.Missions.Content.Wilderness
{
    [MissionScript("wilderness.eloh-pinhole-survey", 1)]
    public sealed class WildernessElohPinholeScene : ISceneScript
    {
        public SceneDecision Handle(SceneContext context, SceneObservation observation)
        {
            var checkpoint = context.Run.Checkpoint == "{}"
                ? new Checkpoint(0)
                : JsonSerializer.Deserialize<Checkpoint>(context.Run.Checkpoint)
                    ?? throw new InvalidOperationException("Survey checkpoint is missing.");
            if (checkpoint.Units > 31)
                throw new InvalidOperationException("Survey checkpoint contains an unknown unit.");
            if (observation.Kind == SceneEventKind.Cancelled)
                return new SceneDecision(context.Run.Checkpoint, status: SceneStatus.Ended);
            if (observation.Kind == SceneEventKind.Started)
            {
                var start = context.Bindings.Sequences[0];
                return new SceneDecision(JsonSerializer.Serialize(checkpoint), start.WorldIntents,
                    start.CharacterIntents, start.Signals);
            }
            if (observation.Kind != SceneEventKind.Signal || observation.SequenceId is < 2 or > 6)
                return new SceneDecision(context.Run.Checkpoint, status: context.Run.Status);

            var unit = 1U << ((int)observation.SequenceId - 2);
            if ((checkpoint.Units & unit) != 0)
                return new SceneDecision(context.Run.Checkpoint, status: context.Run.Status);

            var next = checkpoint.Units | unit;
            var sequence = context.Bindings.Sequences[observation.SequenceId];
            var character = sequence.CharacterIntents.AsEnumerable();
            if (next == 31)
                character = character.Concat(context.Bindings.Sequences[7].CharacterIntents);
            return new SceneDecision(JsonSerializer.Serialize(new Checkpoint(next)),
                sequence.WorldIntents, character, sequence.Signals);
        }

        public sealed record Checkpoint(uint Units);
    }
}
