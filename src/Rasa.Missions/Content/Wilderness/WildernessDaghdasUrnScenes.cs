using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Rasa.Data;
using Rasa.Missions.Scenes;

namespace Rasa.Missions.Content.Wilderness
{
    [MissionScript("wilderness.daghdas-rangers", 1)]
    public sealed class WildernessRangerEncounterScene : ISceneScript
    {
        public SceneDecision Handle(SceneContext context, SceneObservation observation)
        {
            var state = context.Run.Checkpoint == "{}"
                ? new Checkpoint("waiting", 0)
                : JsonSerializer.Deserialize<Checkpoint>(context.Run.Checkpoint)
                    ?? throw new InvalidOperationException("Ranger encounter checkpoint is missing.");
            if (state.Defeated > 7 || state.Phase is not ("waiting" or "fighting" or "cleared" or "failed"))
                throw new InvalidOperationException("Ranger encounter checkpoint is invalid.");

            if (observation.Kind == SceneEventKind.Cancelled)
                return new SceneDecision(JsonSerializer.Serialize(state), status: SceneStatus.Ended);
            if (observation.Kind == SceneEventKind.OwnerLost)
                return new SceneDecision(JsonSerializer.Serialize(state), status: SceneStatus.Waiting);
            if (observation.Kind == SceneEventKind.ActorDied && observation.Role == "anjuhi")
                return new SceneDecision(JsonSerializer.Serialize(state with { Phase = "failed" }),
                    characterIntents: new CharacterIntent[]
                    {
                        new ObjectiveIntent("fail-ranger-defense", 682,
                            state.Phase == "waiting" ? 2U : 3U, MissionObjectiveState.Failed)
                    }, status: SceneStatus.Ended);
            if (observation.Kind is SceneEventKind.Started or SceneEventKind.Recovered)
            {
                var sequence = context.Bindings.Sequences[state.Phase == "waiting" ? 0U : 1U];
                return new SceneDecision(JsonSerializer.Serialize(state),
                    sequence.WorldIntents.Where(intent => !DefeatedRole(intent.Role, state.Defeated)));
            }
            if (observation.Kind == SceneEventKind.Signal && observation.SequenceId == 1 && state.Phase == "waiting")
            {
                var sequence = context.Bindings.Sequences[1];
                return new SceneDecision(JsonSerializer.Serialize(state with { Phase = "fighting" }),
                    sequence.WorldIntents, sequence.CharacterIntents, sequence.Signals);
            }
            if (state.Phase != "fighting")
                return new SceneDecision(JsonSerializer.Serialize(state), status: context.Run.Status);

            var number = observation.Kind == SceneEventKind.ActorDied
                ? RoleNumber(observation.Role)
                : observation.Kind == SceneEventKind.Signal && observation.SequenceId is >= 11 and <= 13
                    ? observation.SequenceId - 10 : 0;
            if (number == 0)
                return new SceneDecision(JsonSerializer.Serialize(state), status: context.Run.Status);
            var bit = 1U << ((int)number - 1);
            if ((state.Defeated & bit) != 0)
                return new SceneDecision(JsonSerializer.Serialize(state), status: context.Run.Status);

            var defeated = state.Defeated | bit;
            var world = defeated == 7
                ? new WorldIntent[] { new SetInteractionIntent("anjuhi-defense-cleared", "anjuhi", true) }
                : Array.Empty<WorldIntent>();
            return new SceneDecision(JsonSerializer.Serialize(new Checkpoint(defeated == 7 ? "cleared" : "fighting", defeated)),
                world, signals: new[] { new SceneMissionSignal(682, 2, number) },
                status: defeated == 7 ? SceneStatus.Ended : SceneStatus.Running);
        }

        private static uint RoleNumber(string role) => role switch
        {
            "xanx-1" => 1,
            "xanx-2" => 2,
            "xanx-3" => 3,
            _ => 0
        };

        private static bool DefeatedRole(string role, uint mask)
        {
            var number = RoleNumber(role);
            return number != 0 && (mask & (1U << ((int)number - 1))) != 0;
        }

        public sealed record Checkpoint(string Phase, uint Defeated);
    }

    [MissionScript("wilderness.daghdas-herbs", 1)]
    public sealed class WildernessTinctuHerbsScene : ISceneScript
    {
        public SceneDecision Handle(SceneContext context, SceneObservation observation)
        {
            var state = context.Run.Checkpoint == "{}"
                ? new Checkpoint(false, 0)
                : JsonSerializer.Deserialize<Checkpoint>(context.Run.Checkpoint)
                    ?? throw new InvalidOperationException("Herb collection checkpoint is missing.");
            if (state.Harvested > 31 || !state.Introduced && state.Harvested != 0)
                throw new InvalidOperationException("Herb collection checkpoint is invalid.");
            if (observation.Kind == SceneEventKind.Cancelled)
                return new SceneDecision(JsonSerializer.Serialize(state), status: SceneStatus.Ended);
            if (observation.Kind == SceneEventKind.OwnerLost)
                return new SceneDecision(JsonSerializer.Serialize(state), status: SceneStatus.Waiting);
            if (observation.Kind == SceneEventKind.Started)
                return new SceneDecision(JsonSerializer.Serialize(state));
            if (observation.Kind == SceneEventKind.Recovered && state.Introduced)
            {
                var world = new List<WorldIntent>();
                for (var number = 1; number <= 5; number++)
                    if ((state.Harvested & (1U << (number - 1))) == 0)
                        world.Add(new EnsureActorIntent($"introduce-tinctu-{number}", $"tinctu-{number}"));
                return new SceneDecision(JsonSerializer.Serialize(state), world);
            }
            if (observation.Kind != SceneEventKind.Signal)
                return new SceneDecision(JsonSerializer.Serialize(state), status: context.Run.Status);
            if (observation.SequenceId == 1 && !state.Introduced)
                return new SceneDecision(JsonSerializer.Serialize(state with { Introduced = true }),
                    context.Bindings.Sequences[1].WorldIntents);
            if (!state.Introduced || observation.SequenceId is < 11 or > 15)
                return new SceneDecision(JsonSerializer.Serialize(state), status: context.Run.Status);

            var numberPicked = observation.SequenceId - 10;
            var bit = 1U << ((int)numberPicked - 1);
            if ((state.Harvested & bit) != 0)
                return new SceneDecision(JsonSerializer.Serialize(state), status: context.Run.Status);
            var harvested = state.Harvested | bit;
            var sequence = context.Bindings.Sequences[observation.SequenceId];
            return new SceneDecision(JsonSerializer.Serialize(state with { Harvested = harvested }),
                sequence.WorldIntents, sequence.CharacterIntents,
                signals: harvested == 31
                    ? new[] { new SceneMissionSignal(695, 2, 1) }
                    : Array.Empty<SceneMissionSignal>(),
                status: harvested == 31 ? SceneStatus.Ended : SceneStatus.Running);
        }

        public sealed record Checkpoint(bool Introduced, uint Harvested);
    }

    [MissionScript("wilderness.corman-first-batch", 1)]
    public sealed class WildernessFirstVaccineBatchScene : ISceneScript
    {
        public SceneDecision Handle(SceneContext context, SceneObservation observation) =>
            WildernessVaccineDelivery.Handle(context, observation);
    }

    [MissionScript("wilderness.corman-replacement", 1)]
    public sealed class WildernessReplacementVaccineScene : ISceneScript
    {
        public SceneDecision Handle(SceneContext context, SceneObservation observation) =>
            WildernessVaccineDelivery.Handle(context, observation);
    }

    internal static class WildernessVaccineDelivery
    {
        internal static SceneDecision Handle(SceneContext context, SceneObservation observation)
        {
            if (context.Run.MissionId is not (700 or 820))
                throw new InvalidOperationException("Vaccine delivery belongs only to the two native medicine missions.");
            var state = context.Run.Checkpoint == "{}"
                ? new Checkpoint(0)
                : JsonSerializer.Deserialize<Checkpoint>(context.Run.Checkpoint)
                    ?? throw new InvalidOperationException("Vaccine delivery checkpoint is missing.");
            if (state.Recipients > 7)
                throw new InvalidOperationException("Vaccine delivery checkpoint contains an unknown recipient.");
            if (observation.Kind is SceneEventKind.Cancelled or SceneEventKind.MissionRewarded)
                return new SceneDecision(JsonSerializer.Serialize(state), status: SceneStatus.Ended);
            if (observation.Kind == SceneEventKind.OwnerLost)
                return new SceneDecision(JsonSerializer.Serialize(state), status: SceneStatus.Waiting);
            if (observation.Kind == SceneEventKind.ObjectiveDeadlineElapsed && context.Run.MissionId == 820)
                return new SceneDecision(JsonSerializer.Serialize(state), status: SceneStatus.Ended);
            if (observation.Kind is SceneEventKind.Started or SceneEventKind.Recovered)
                return new SceneDecision(JsonSerializer.Serialize(state),
                    characterIntents: state.Recipients != 7
                        ? context.Bindings.Sequences[0].CharacterIntents : Array.Empty<CharacterIntent>());
            if (observation.Kind == SceneEventKind.Signal && observation.SequenceId == 5 && state.Recipients == 7)
                return new SceneDecision(JsonSerializer.Serialize(state), status: SceneStatus.Ended);
            if (observation.Kind != SceneEventKind.Signal || observation.SequenceId is < 1 or > 3)
                return new SceneDecision(JsonSerializer.Serialize(state), status: context.Run.Status);

            var bit = 1U << ((int)observation.SequenceId - 1);
            if ((state.Recipients & bit) != 0)
                return new SceneDecision(JsonSerializer.Serialize(state), status: context.Run.Status);
            var recipients = state.Recipients | bit;
            return new SceneDecision(JsonSerializer.Serialize(new Checkpoint(recipients)),
                characterIntents: recipients == 7
                    ? new CharacterIntent[]
                    {
                        new ObjectiveIntent("reveal-eleanor-report", context.Run.MissionId, 5, MissionObjectiveState.NotAssigned),
                        new ObjectiveIntent("activate-eleanor-report", context.Run.MissionId, 5, MissionObjectiveState.Incomplete)
                    }
                    : Array.Empty<CharacterIntent>());
        }

        public sealed record Checkpoint(uint Recipients);
    }
}
