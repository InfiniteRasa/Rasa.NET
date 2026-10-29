using System;
using System.Linq;
using System.Text.Json;
using Rasa.Data;
using Rasa.Missions.Scenes;

namespace Rasa.Missions.Content.Plateau
{
    /// <summary>
    /// These Old Bones (1976), the squad's copy of Velon Hollow: Elder Hathen's escort copy stands in the public Hathen's
    /// place from the start and follows the run's owner (objective 4); the public one is not shown to the owner while
    /// the copy stands (squad mates still see him). Reaching a temple (its hidden objective's scenario,
    /// <see cref="TempleReached"/> + the temple) brings its infected Foreans and Bane against the owner; when all of them
    /// are dead the temple's objective (1-3) is done. With the third temple Hathen stops following, walks about 100 m of
    /// his way back towards the hospital (<see cref="WayBack"/>) and is gone there, the public Hathen shown again; objective 4
    /// is done (<see cref="Outcome"/>). His death before that fails 4.
    /// </summary>
    [MissionScript("plateau.these-old-bones", 1)]
    public sealed class PlateauTheseOldBonesScene : ISceneScript
    {
        public const uint MissionId = 1976;
        public const uint Protect = 4;

        /// <summary>Scenarios 1-3: temple 1-3 reached. Scenario 4: the three temples secured, one step.</summary>
        public const uint TempleReached = 0, Outcome = 4, Secured = 1;
        public const int Temples = 3, GroupSize = 5;

        /// <summary>The sequence a temple creature's defeat signals: <see cref="DefeatedFrom"/> + 10 x temple + its place (1-5).</summary>
        public const uint DefeatedFrom = 100;

        /// <summary>The sequence Hathen's death signals (a follower is on no route, so his death comes as his defeat sequence).</summary>
        public const uint HathenDown = 99;
        public const string Hathen = "hathen", HomeOperation = "hathen-goes-back";

        /// <summary>
        /// The walk back from a temple (1-3): one waypoint on the local navmesh path from the temple towards the hospital,
        /// about 100 m along it (ESTIMATE 100 m, user 2026-10-25: the whole way was too long).
        /// </summary>
        public static string WayBack(uint temple) => $"back-from-temple-{temple}";

        /// <summary>
        /// The walk back's end however it goes (no route, stuck): Hathen is gone after this (ESTIMATE: 100 m at the route's
        /// 2.5 m/s is 40 s, and 20 s to spare).
        /// </summary>
        public const string WalkBackTimeout = "hathen-walk-back";
        public const uint WalkBackMilliseconds = 60000;

        /// <summary>
        /// The public Elder Hathen's spawn pool (510152, the giver at the hospital): not shown to the run's owner while his
        /// escort copy stands in for him (HideSpawnPoolsIntent, held by the copy), shown again when the copy is gone.
        /// </summary>
        public const uint PublicHathenPool = 510152;

        public static string Role(uint temple, int index) => $"temple-{temple}-{index + 1}";
        public static uint DefeatSequence(uint temple, int index) => DefeatedFrom + 10 * temple + (uint)index + 1;

        public SceneDecision Handle(SceneContext context, SceneObservation observation)
        {
            var state = context.Run.Checkpoint == "{}"
                ? new Checkpoint("new", Array.Empty<uint>(), Array.Empty<uint>())
                : JsonSerializer.Deserialize<Checkpoint>(context.Run.Checkpoint)
                    ?? throw new InvalidOperationException("These Old Bones checkpoint is missing.");
            if (state.Phase is not ("new" or "escorting" or "returning" or "failed" or "done" or "cancelled") ||
                state.Reached == null || state.Killed == null)
                throw new InvalidOperationException($"These Old Bones checkpoint has an invalid phase {state.Phase}.");

            if (observation.Kind == SceneEventKind.Started && state.Phase == "new")
                return Decide(state with { Phase = "escorting" }, Escort());
            if (observation.Kind == SceneEventKind.Recovered && state.Phase == "escorting")
                return Decide(state, Escort().Concat(Standing(state)).ToArray());
            // Back after a reload or a way back in on the walk back: he cannot be put back where he was on it, so he is gone.
            if (observation.Kind == SceneEventKind.Recovered && state.Phase == "returning")
                return Gone(state, "hathen-gone");
            if (observation.Kind == SceneEventKind.TimerElapsed && state.Phase == "returning" && observation.Name == WalkBackTimeout)
                return Gone(state, "hathen-walked");

            if (observation.Kind == SceneEventKind.Cancelled && state.Phase == "returning")
                return Gone(state, "hathen-gone");
            if (observation.Kind == SceneEventKind.Cancelled)
                return Decide(state with { Phase = "cancelled" }, status: SceneStatus.Ended);

            if (state.Phase == "escorting" && (observation.Kind == SceneEventKind.Signal && observation.SequenceId == HathenDown ||
                                               observation.Kind == SceneEventKind.ActorDied && observation.Role == Hathen))
                return Decide(state with { Phase = "failed" }, character: new CharacterIntent[]
                {
                    new ObjectiveIntent("hathen-died", MissionId, Protect, MissionObjectiveState.Failed)
                }, status: SceneStatus.Ended);

            if (state.Phase == "escorting" && observation.Kind == SceneEventKind.Signal &&
                observation.SequenceId is > TempleReached and <= Temples)
            {
                var temple = observation.SequenceId;
                if (state.Reached.Contains(temple))
                    return Unchanged(context);
                return Decide(state with { Reached = state.Reached.Append(temple).OrderBy(id => id).ToArray() }, Group(temple, state));
            }

            if (state.Phase == "escorting" && observation.Kind == SceneEventKind.Signal &&
                observation.SequenceId > DefeatedFrom && observation.SequenceId <= DefeatSequence(Temples, GroupSize - 1))
            {
                var step = observation.SequenceId;
                var temple = (step - DefeatedFrom - 1) / 10;
                if (temple is < 1 or > Temples || state.Killed.Contains(step) || !state.Reached.Contains(temple))
                    return Unchanged(context);
                var killed = state.Killed.Append(step).OrderBy(id => id).ToArray();
                if (!Enumerable.Range(0, GroupSize).All(index => killed.Contains(DefeatSequence(temple, index))))
                    return Decide(state with { Killed = killed });

                var secured = new CharacterIntent[] { new ObjectiveIntent($"temple-{temple}-secured", MissionId, temple, MissionObjectiveState.Completed) };
                var cleared = Enumerable.Range(1, Temples).Count(other =>
                    Enumerable.Range(0, GroupSize).All(index => killed.Contains(DefeatSequence((uint)other, index))));
                if (cleared < Temples)
                    return Decide(state with { Killed = killed }, character: secured);
                // The third: Hathen stops following and walks a way back towards the hospital; 4 is done.
                return Decide(state with { Phase = "returning", Killed = killed },
                    new WorldIntent[] { new FollowActorIntent("hathen-stops", Hathen, 0, Enabled: false), GoHome(temple) },
                    secured, new[] { new SceneMissionSignal(MissionId, Outcome, Secured) },
                    timers: new[] { new SceneTimerChange(WalkBackTimeout, SceneClockPolicy.WallClock, context.UtcNow.AddMilliseconds(WalkBackMilliseconds)) });
            }

            if (state.Phase == "returning" && (observation.Kind == SceneEventKind.Signal && observation.SequenceId == HathenDown ||
                                               observation.Kind == SceneEventKind.ActorDied && observation.Role == Hathen))
                return Decide(state with { Phase = "done" }, status: SceneStatus.Ended);

            if (state.Phase == "returning" && observation.Kind == SceneEventKind.RouteCompleted &&
                observation.Role == Hathen && observation.OperationKey == HomeOperation)
                return Gone(state, "hathen-home");

            return Unchanged(context);
        }

        private static WorldIntent[] Escort() => new WorldIntent[]
        {
            new EnsureActorIntent("ensure-hathen", Hathen),
            HidePublicHathen(),
            new SetInteractionIntent("hathen-escorted", Hathen, false),
            new FollowActorIntent("hathen-follows", Hathen, 0)
        };

        /// <summary>A reached temple's creatures not yet killed, back after a recovery.</summary>
        private static WorldIntent[] Standing(Checkpoint state) =>
            state.Reached.SelectMany(temple => Group(temple, state)).ToArray();

        private static WorldIntent[] Group(uint temple, Checkpoint state) =>
            Enumerable.Range(0, GroupSize)
                .Where(index => !state.Killed.Contains(DefeatSequence(temple, index)))
                .SelectMany(index => new WorldIntent[]
                {
                    new EnsureActorIntent($"ensure-{Role(temple, index)}", Role(temple, index)),
                    QuestSceneIntents.Attack($"{Role(temple, index)}-attacks", Role(temple, index))
                }).ToArray();

        private static WorldIntent GoHome(uint temple) => new RunRouteIntent(HomeOperation, Hathen, WayBack(temple));

        /// <summary>The walk back over: Hathen removed (the public Hathen shown again with him), the run ended.</summary>
        private static SceneDecision Gone(Checkpoint state, string operation) =>
            Decide(state with { Phase = "done" }, new WorldIntent[] { new RemoveActorIntent(operation, Hathen) },
                timers: new[] { new SceneTimerChange(WalkBackTimeout, SceneClockPolicy.WallClock, null, Cancel: true) },
                status: SceneStatus.Ended);

        private static WorldIntent HidePublicHathen() => new HideSpawnPoolsIntent("public-hathen-hidden", Hathen, new[] { PublicHathenPool });

        private static SceneDecision Unchanged(SceneContext context) => new(context.Run.Checkpoint, status: context.Run.Status);

        private static SceneDecision Decide(Checkpoint state, WorldIntent[] world = null, CharacterIntent[] character = null,
            SceneMissionSignal[] signals = null, SceneTimerChange[] timers = null, SceneStatus status = SceneStatus.Running) =>
            new(JsonSerializer.Serialize(state), world, character, signals, timers, status: status);

        public sealed record Checkpoint(string Phase, uint[] Reached, uint[] Killed);
    }
}
