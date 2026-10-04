using System;
using System.Collections.Generic;
using System.Text.Json;
using Rasa.Data;
using Rasa.Missions.Scenes;

namespace Rasa.Missions.Content.Wilderness
{
    [MissionScript("wilderness.supply-delivery", 1)]
    public sealed class WildernessSupplyDeliveryScene : ISceneScript
    {
        public SceneDecision Handle(SceneContext context, SceneObservation observation)
        {
            var phase = context.Run.Checkpoint == "{}"
                ? "new"
                : JsonSerializer.Deserialize<Checkpoint>(context.Run.Checkpoint)?.Phase
                    ?? throw new InvalidOperationException("Supply delivery checkpoint has no phase.");

            if (observation.Kind == SceneEventKind.Started && phase == "new")
                return Apply(context, 0, "waiting");
            if (observation.Kind == SceneEventKind.Signal && phase == "waiting" &&
                observation.SequenceId is 1 or 2)
                return Apply(context, observation.SequenceId,
                    observation.SequenceId == 1 ? "carrying" : "damaged");
            if (observation.Kind == SceneEventKind.ObjectiveDeadlineElapsed &&
                observation.Name == "objective-3-deadline" && phase == "damaged")
                return new SceneDecision(Serialize("spoiled"), characterIntents: new CharacterIntent[]
                {
                    new ObjectiveIntent("fail-spoiled-delivery", 428, 2, MissionObjectiveState.Failed)
                }, status: SceneStatus.Ended);
            if (observation.Kind == SceneEventKind.Signal && observation.SequenceId == 3 &&
                phase is "carrying" or "damaged")
            {
                var intents = new List<CharacterIntent>();
                if (phase == "damaged")
                    intents.Add(new ObjectiveIntent("supplies-refrigerated", 428, 3, MissionObjectiveState.Completed));
                return new SceneDecision(Serialize("delivered"), characterIntents: intents, status: SceneStatus.Ended);
            }
            if (observation.Kind is SceneEventKind.Cancelled or SceneEventKind.MissionRewarded)
                return new SceneDecision(Serialize("ended"), status: SceneStatus.Ended);
            return new SceneDecision(context.Run.Checkpoint, status: context.Run.Status);
        }

        private static SceneDecision Apply(SceneContext context, uint sequenceId, string phase)
        {
            var sequence = context.Bindings.Sequences[sequenceId];
            return new SceneDecision(Serialize(phase), sequence.WorldIntents,
                sequence.CharacterIntents, sequence.Signals);
        }

        private static string Serialize(string phase) => JsonSerializer.Serialize(new Checkpoint(phase));

        public sealed record Checkpoint(string Phase);
    }
}
