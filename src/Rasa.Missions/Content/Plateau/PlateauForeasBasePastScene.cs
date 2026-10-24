using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Rasa.Data;
using Rasa.Missions.Scenes;

namespace Rasa.Missions.Content.Plateau
{
    /// <summary>
    /// Foreas Base Past (1178), the Temporal Chamber's past Foreas Base: the AFS defenders, Villaran and the drop-ship
    /// stand from the start; Fonseca's first talk (scenario <see cref="EscortStarts"/>) sends Villaran along his route to
    /// the drop-ship (objective 3) while the first Bane wave goes for him; his death or a blocked route fails 3. His
    /// arrival (the first wave's survivors withdraw) opens Fonseca's second talk, which (scenario <see cref="AssaultStarts"/>) brings Xykryl and his strike
    /// force against the run's owner; each of their deaths, whoever kills them, is one step of objective 5's count.
    /// </summary>
    [MissionScript("plateau.foreas-base-past", 1)]
    public sealed class PlateauForeasBasePastScene : ISceneScript
    {
        public const uint MissionId = 1178;
        public const uint Escort = 3, SecondTalk = 4, Kill = 5;
        public const uint EscortStarts = 1, AssaultStarts = 2, Kills = 3;

        /// <summary>The sequence a strike force member's defeat signals: <see cref="DefeatedFrom"/> + its step (1 Xykryl, 2-5 the strike force).</summary>
        public const uint DefeatedFrom = 10;
        public const int StrikeForce = 4;
        public const string Villaran = "villaran", Xykryl = "xykryl", Route = "to-dropship", RouteOperation = "escort";

        public static readonly string[] Statics = { "defender-1", "defender-2", "defender-3", "defender-4", "landing-pad", "dropship" };
        public static readonly string[] FirstWave = { "recon-1", "recon-2", "recon-3", "recon-4", "cavalryman-1", "cavalryman-2" };
        public static string StrikeForceRole(int index) => $"special-forces-{index + 1}";

        /// <summary>The strike force's roles in step order: Xykryl (step 1), then the special forces (steps 2-5).</summary>
        public static IEnumerable<string> SecondWave() =>
            new[] { Xykryl }.Concat(Enumerable.Range(0, StrikeForce).Select(StrikeForceRole));

        public SceneDecision Handle(SceneContext context, SceneObservation observation)
        {
            var state = context.Run.Checkpoint == "{}"
                ? new Checkpoint("new", Array.Empty<uint>())
                : JsonSerializer.Deserialize<Checkpoint>(context.Run.Checkpoint)
                    ?? throw new InvalidOperationException("Foreas Base Past checkpoint is missing.");
            if (state.Phase is not ("new" or "waiting" or "escorting" or "escorted" or "assault" or "failed" or "cancelled") ||
                state.Killed == null)
                throw new InvalidOperationException($"Foreas Base Past checkpoint has an invalid phase {state.Phase}.");

            if (observation.Kind == SceneEventKind.Started && state.Phase == "new" ||
                observation.Kind == SceneEventKind.Recovered && state.Phase == "waiting")
                return Decide(state with { Phase = "waiting" }, Standing(withVillaran: true));
            if (observation.Kind == SceneEventKind.Recovered && state.Phase == "escorting")
                return Decide(state, Standing(withVillaran: true).Append(Walk()).ToArray());
            if (observation.Kind == SceneEventKind.Recovered && state.Phase is "escorted" or "assault")
                return Decide(state, Standing(withVillaran: false));

            if (observation.Kind == SceneEventKind.Cancelled && observation.Name != "route-blocked")
                return Decide(state with { Phase = "cancelled" }, status: SceneStatus.Ended);

            if (state.Phase == "waiting" && observation.Kind == SceneEventKind.Signal && observation.SequenceId == EscortStarts)
                return Decide(state with { Phase = "escorting" }, FirstWave
                    .SelectMany(role => new WorldIntent[]
                    {
                        new EnsureActorIntent($"ensure-{role}", role),
                        new AttackActorIntent($"{role}-attacks", role, Villaran)
                    })
                    .Prepend(Walk()).ToArray());

            if (state.Phase == "escorting" && observation.Kind == SceneEventKind.RouteCompleted &&
                observation.Role == Villaran && observation.OperationKey == RouteOperation)
                return Decide(state with { Phase = "escorted" },
                    // The first wave's survivors withdraw: their attack has no target left to restore.
                    FirstWave.Select(role => (WorldIntent)new RemoveActorIntent($"withdraw-{role}", role))
                        .Prepend(new RemoveActorIntent("villaran-boards", Villaran)).ToArray(),
                    new CharacterIntent[]
                    {
                        new ObjectiveIntent("escort-arrived", MissionId, Escort, MissionObjectiveState.Completed),
                        new ObjectiveIntent("reveal-second-talk", MissionId, SecondTalk, MissionObjectiveState.NotAssigned),
                        new ObjectiveIntent("activate-second-talk", MissionId, SecondTalk, MissionObjectiveState.Incomplete)
                    });

            if (state.Phase == "escorting" &&
                (observation.Kind == SceneEventKind.ActorDied && observation.Role == Villaran ||
                 observation.Kind == SceneEventKind.Cancelled && observation.Name == "route-blocked"))
                return Decide(state with { Phase = "failed" }, character: new CharacterIntent[]
                {
                    new ObjectiveIntent("escort-failed", MissionId, Escort, MissionObjectiveState.Failed)
                }, status: SceneStatus.Ended);

            if (state.Phase == "escorted" && observation.Kind == SceneEventKind.Signal && observation.SequenceId == AssaultStarts)
                return Decide(state with { Phase = "assault" }, SecondWave()
                    .SelectMany(role => new WorldIntent[]
                    {
                        new EnsureActorIntent($"ensure-{role}", role),
                        new AttackActorIntent($"{role}-attacks", role)
                    }).ToArray());

            if (state.Phase == "assault" && observation.Kind == SceneEventKind.Signal &&
                observation.SequenceId is > DefeatedFrom and <= DefeatedFrom + 1 + StrikeForce)
            {
                var step = observation.SequenceId - DefeatedFrom;
                if (state.Killed.Contains(step))
                    return new SceneDecision(context.Run.Checkpoint, status: context.Run.Status);
                return Decide(state with { Killed = state.Killed.Append(step).OrderBy(id => id).ToArray() },
                    signals: new[] { new SceneMissionSignal(MissionId, Kills, step) });
            }

            return new SceneDecision(context.Run.Checkpoint, status: context.Run.Status);
        }

        /// <summary>What stands from the start: the defenders, the landing pad and the drop-ship; Villaran until he boards.</summary>
        private static WorldIntent[] Standing(bool withVillaran)
        {
            var intents = Statics.Select(role => (WorldIntent)new EnsureActorIntent($"ensure-{role}", role)).ToList();
            if (withVillaran)
            {
                intents.Add(new EnsureActorIntent("ensure-villaran", Villaran));
                intents.Add(new SetInteractionIntent("villaran-escorted", Villaran, false));
            }
            return intents.ToArray();
        }

        private static WorldIntent Walk() => new RunRouteIntent(RouteOperation, Villaran, Route, ResumeAfterCombat: true);

        private static SceneDecision Decide(Checkpoint state, WorldIntent[] world = null, CharacterIntent[] character = null,
            SceneMissionSignal[] signals = null, SceneStatus status = SceneStatus.Running) =>
            new(JsonSerializer.Serialize(state), world, character, signals, status: status);

        public sealed record Checkpoint(string Phase, uint[] Killed);
    }
}
