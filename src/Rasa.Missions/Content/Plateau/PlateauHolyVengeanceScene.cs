using System;
using System.Linq;
using System.Text.Json;
using Rasa.Data;
using Rasa.Missions.Scenes;

namespace Rasa.Missions.Content.Plateau
{
    /// <summary>
    /// Holy Vengeance (1977), the squad's copy of Velon Hollow: from the accept, Warrior Algona's escort copy and two
    /// Forean warriors stand at the hospital and follow the run's owner (hidden objective 4); their deaths fail nothing.
    /// The Law of Asperion lies in a Bane crate in the outpost's control room, which gives it only while objective 3 is
    /// open (its use is <see cref="BookTaken"/>). With the book taken the three stop following, walk back to the
    /// hospital and are gone there.
    /// </summary>
    [MissionScript("plateau.holy-vengeance", 1)]
    public sealed class PlateauHolyVengeanceScene : ISceneScript
    {
        public const uint MissionId = 1977;
        public const uint Recover = 3;

        /// <summary>Scenario 1: the book taken, one step. Its sequence is the crate's use.</summary>
        public const uint Book = 1, Taken = 1, BookTaken = 1;

        /// <summary>The sequence a follower's death signals: <see cref="DownFrom"/> + its place (1 Algona, 2-3 the warriors).</summary>
        public const uint DownFrom = 10;
        public const string Crate = "law-crate", BookKey = "law-of-asperion";

        /// <summary>The crate's states and its use argument, as Weapons Trafficking's (class 11116).</summary>
        public const uint ReadyState = 81, TakenState = 82, UseArgument = 3;

        public static readonly string[] Followers = { "algona", "warrior-1", "warrior-2" };
        public static string Home(int index) => $"home-{index + 1}";
        public static string HomeOperation(int index) => $"{Followers[index]}-goes-back";

        public SceneDecision Handle(SceneContext context, SceneObservation observation)
        {
            var state = context.Run.Checkpoint == "{}"
                ? new Checkpoint("new", Array.Empty<int>(), Array.Empty<int>())
                : JsonSerializer.Deserialize<Checkpoint>(context.Run.Checkpoint)
                    ?? throw new InvalidOperationException("Holy Vengeance checkpoint is missing.");
            if (state.Phase is not ("new" or "following" or "returning" or "done" or "cancelled") || state.Dead == null || state.Home == null)
                throw new InvalidOperationException($"Holy Vengeance checkpoint has an invalid phase {state.Phase}.");

            if (observation.Kind == SceneEventKind.Started && state.Phase == "new")
                return Decide(state with { Phase = "following" }, Follow(state).Concat(CrateStands(taken: false)).ToArray());
            if (observation.Kind == SceneEventKind.Recovered && state.Phase == "following")
                return Decide(state, Follow(state).Concat(CrateStands(taken: false)).ToArray());
            if (observation.Kind == SceneEventKind.Recovered && state.Phase == "returning")
                return Decide(state, Returning(state).Concat(CrateStands(taken: true)).ToArray());

            if (observation.Kind == SceneEventKind.Cancelled && state.Phase == "returning" && observation.Name == "route-blocked")
                return Decide(state with { Phase = "done" }, Remaining(state)
                    .Select(index => (WorldIntent)new RemoveActorIntent($"{Followers[index]}-gone", Followers[index])).ToArray(),
                    status: SceneStatus.Ended);
            if (observation.Kind == SceneEventKind.Cancelled)
                return Decide(state with { Phase = "cancelled" }, status: SceneStatus.Ended);

            // A follower's death: he stays dead, nothing fails.
            if (observation.Kind == SceneEventKind.Signal && observation.SequenceId is > DownFrom and <= DownFrom + 3)
            {
                var index = (int)(observation.SequenceId - DownFrom - 1);
                if (state.Dead.Contains(index))
                    return Unchanged(context);
                var next = state with { Dead = state.Dead.Append(index).OrderBy(i => i).ToArray() };
                return state.Phase == "returning" && !Remaining(next).Any()
                    ? Decide(next with { Phase = "done" }, status: SceneStatus.Ended)
                    : Decide(next);
            }

            // The book: the crate is emptied, the book given; the three walk back.
            if (state.Phase == "following" && observation.Kind == SceneEventKind.Signal && observation.SequenceId == BookTaken)
            {
                var next = state with { Phase = "returning" };
                return Decide(next,
                    new WorldIntent[]
                        {
                            new TransitionObjectStateIntent("crate-opened", Crate, TakenState),
                            new SetInteractionIntent("crate-emptied", Crate, false)
                        }
                        .Concat(Remaining(next).SelectMany(index => new WorldIntent[]
                        {
                            new FollowActorIntent($"{Followers[index]}-stops", Followers[index], 0, Enabled: false),
                            GoHome(index)
                        })).ToArray(),
                    new CharacterIntent[] { new IssueMissionItemIntent("law-from-crate", MissionId, BookKey, BookTemplateId, 1) },
                    new[] { new SceneMissionSignal(MissionId, Book, Taken) },
                    Remaining(next).Any() ? SceneStatus.Running : SceneStatus.Ended);
            }

            if (state.Phase == "returning" && observation.Kind == SceneEventKind.RouteCompleted)
            {
                var index = Array.IndexOf(Followers, observation.Role);
                if (index < 0 || observation.OperationKey != HomeOperation(index) || state.Home.Contains(index))
                    return Unchanged(context);
                var next = state with { Home = state.Home.Append(index).OrderBy(i => i).ToArray() };
                return Decide(next, new WorldIntent[] { new RemoveActorIntent($"{Followers[index]}-home", Followers[index]) },
                    status: Remaining(next).Any() ? SceneStatus.Running : SceneStatus.Ended);
            }

            return Unchanged(context);
        }

        /// <summary>The book's item template (Hollow_Mission_Law_of_Asperion).</summary>
        public const uint BookTemplateId = 122242;

        private static int[] Remaining(Checkpoint state) =>
            Enumerable.Range(0, Followers.Length).Where(index => !state.Dead.Contains(index) && !state.Home.Contains(index)).ToArray();

        private static WorldIntent[] Follow(Checkpoint state) =>
            Remaining(state).SelectMany(index => new WorldIntent[]
            {
                new EnsureActorIntent($"ensure-{Followers[index]}", Followers[index]),
                new SetInteractionIntent($"{Followers[index]}-escorted", Followers[index], false),
                new FollowActorIntent($"{Followers[index]}-follows", Followers[index], 0)
            }).ToArray();

        private static WorldIntent[] Returning(Checkpoint state) =>
            Remaining(state).SelectMany(index => new WorldIntent[]
            {
                new EnsureActorIntent($"ensure-{Followers[index]}", Followers[index]),
                GoHome(index)
            }).ToArray();

        private static WorldIntent[] CrateStands(bool taken) => taken
            ? new WorldIntent[]
            {
                new EnsureActorIntent("ensure-crate", Crate), new TransitionObjectStateIntent("crate-opened", Crate, TakenState),
                new SetInteractionIntent("crate-emptied", Crate, false)
            }
            : new WorldIntent[] { new EnsureActorIntent("ensure-crate", Crate) };

        private static WorldIntent GoHome(int index) => new RunRouteIntent(HomeOperation(index), Followers[index], Home(index));

        private static SceneDecision Unchanged(SceneContext context) => new(context.Run.Checkpoint, status: context.Run.Status);

        private static SceneDecision Decide(Checkpoint state, WorldIntent[] world = null, CharacterIntent[] character = null,
            SceneMissionSignal[] signals = null, SceneStatus status = SceneStatus.Running) =>
            new(JsonSerializer.Serialize(state), world, character, signals, status: status);

        public sealed record Checkpoint(string Phase, int[] Dead, int[] Home);
    }
}
