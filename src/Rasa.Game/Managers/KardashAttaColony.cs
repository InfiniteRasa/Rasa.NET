using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Missions;
    using Missions.Scenes;
    using Structures;

    using K = Services.Preloader.KardashAttaColonyContent;

    /// <summary>
    /// What the Kardash Atta Colony (1773) does that its rows do not, in each copy of it - a
    /// squad's instance, or the map's own channel - for the missions KardashAttaColonyContent
    /// authors, as Ten Ton Hammer's guide tells it:
    ///
    ///  - The console beside Researcher Kranix in Research Outpost Alpha, which Into the Hive is
    ///    reported from: an object the player speaks to (package 1213).
    ///  - Atta Control: one Atta Mind Control Transducer beside the mind control console of each
    ///    of the five Brann outposts. Each destroyed is its objective for everyone on it, and stays
    ///    down in the copy. A member who has all five done is told so (the hidden counter), which
    ///    shows the report to Captain Reyko. The mission is offered by radio to a member who has
    ///    done Into the Hive, again <see cref="OfferAgainMs"/> later if not taken.
    ///  - Bug Hunt: a member on Find the Kardash Atta Colony has found it, being in it.
    ///  - Bug Hugger: a member in the Harvest Grotto, the Inner Nest, the Royal Nest or the Queen's
    ///    Chamber has explored it. A member on Protect the Kardash Atta Queen in the chamber brings
    ///    four Lightbender Infiltrators in before her throne; all of them dead, she is protected,
    ///    for everyone in the copy on it from then on. A member with everything but Lieutenant
    ///    Donners done is told so (the hidden counter), which shows Talk to Lieutenant Donners.
    /// What the copy has done goes to every member whose mission is waiting on it, so a member who
    /// takes a mission up later, or joins the squad, is caught up. Kept by the copy and not saved.
    /// </summary>
    public static class KardashAttaColony
    {
        public const long OfferAgainMs = 60_000;
        public const int UseWindupMs = 1_000;

        internal sealed class State
        {
            public bool Set;
            public DynamicObject Console;
            public readonly PlacedDestructibles.Live[] Transducers = new PlacedDestructibles.Live[K.Transducers.Length];
            public readonly List<Creature> Raiders = new();
            public bool QueenProtected;
            public readonly Dictionary<uint, long> OfferedAt = new();

            /// <summary>The events already recorded: the assignment, the scenario, the event.</summary>
            public readonly HashSet<(string Assignment, uint Mission, uint Scenario, uint Event)> Told = new();
        }

        private static readonly ConditionalWeakTable<MapChannel, State> States = new();

        /// <summary>The clock; a test's to replace.</summary>
        public static Func<long> Now { get; set; } = () => Environment.TickCount64;

        public static bool IsColony(MapChannel map) => map?.MapInfo?.MapContextId == K.Colony;

        internal static State StateOf(MapChannel map) => IsColony(map) ? States.GetValue(map, _ => new State()) : null;

        /// <summary>The copy's transducers, in the guide's order; for a test or a GM.</summary>
        public static IReadOnlyList<PlacedDestructibles.Live> Transducers(MapChannel map) => StateOf(map)?.Transducers ?? Array.Empty<PlacedDestructibles.Live>();

        /// <summary>The copy's console; for a test.</summary>
        public static DynamicObject Console(MapChannel map) => StateOf(map)?.Console;

        /// <summary>The Bane brought in at the Queen; for a test.</summary>
        public static IReadOnlyList<Creature> Raiders(MapChannel map) => StateOf(map)?.Raiders ?? (IReadOnlyList<Creature>)Array.Empty<Creature>();

        /// <summary>From the map channel worker.</summary>
        public static void Worker(MapChannel map)
        {
            var state = StateOf(map);

            if (state == null)
                return;

            if (!state.Set)
                SetUp(map, state);

            KeepDown(state);

            var members = Members(map);

            if (members.Count == 0)
                return;

            var now = Now();

            foreach (var client in members)
            {
                var player = client.Player;

                if (Wants(player, K.BugHunt, K.FindColony))
                    Record(state, client, K.BugHunt, K.ColonyScenario, K.ColonyFound);

                Transducers(state, client);
                Explore(state, client);
                OfferAttaControl(state, client, now);
            }

            Queen(map, state, members);

            // The hidden counters, once the rest is done.
            foreach (var client in members)
            {
                var player = client.Player;

                if (Wants(player, K.AttaControl, K.TransducerCounter) &&
                    K.Transducers.All(t => Completed(player, K.AttaControl, t.Objective)))
                    Record(state, client, K.AttaControl, K.AllTransducersScenario, K.AllTransducersDown);

                if (Wants(player, K.BugHugger, K.HuggerCounter) &&
                    new[] { K.ExploreGrotto, K.ExploreInnerNest, K.ExploreRoyalNest, K.ExploreQueensChamber, K.ProtectQueen, K.TalkToReshi }
                        .All(objective => Completed(player, K.BugHugger, objective)))
                    Record(state, client, K.BugHugger, K.HuggerDoneScenario, K.HuggerDone);
            }
        }

        #region Set up

        /// <summary>The copy's console and transducers, put down once.</summary>
        private static void SetUp(MapChannel map, State state)
        {
            state.Set = true;

            state.Console = map.DynamicObjects.FirstOrDefault(obj => obj.ScenarioKey == "kardash:console");

            if (state.Console == null)
            {
                var console = DynamicObjectManager.Instance.CreateScenarioDynamicObject(map, (EntityClasses)K.ConsoleClass, K.Console, K.ConsoleYaw,
                    "kardash:console", true, UseWindupMs);
                console.StateId = UseObjectState.TsState0;
                console.MissionConversation = new SceneObjectConversation(K.IntoTheHive, 1, K.ConsolePackage, 1);
                map.DynamicObjects.Add(console);
                CellManager.Instance.AddToWorld(map, console);
                console.IsInWorld = true;
                state.Console = console;
            }

            for (var i = 0; i < K.Transducers.Length; i++)
                state.Transducers[i] = PlacedDestructibles.PutDown(map, (EntityClasses)K.TransducerClass,
                    NavMeshManager.SnapToGround(map, K.Transducers[i].At), 0, K.TransducerHitPoints);

            Logger.WriteLog(LogType.Debug, $"Kardash Atta Colony {map.InstanceId}: the console and " +
                $"{state.Transducers.Count(live => live != null)} transducers put down.");
        }

        /// <summary>A transducer destroyed stays so: it is never due back.</summary>
        private static void KeepDown(State state)
        {
            foreach (var live in state.Transducers)
                if (live != null && live.IsDown && live.RespawnAt != long.MaxValue)
                    live.RespawnAt = long.MaxValue;
        }

        #endregion

        #region The missions

        private static void Transducers(State state, Client client)
        {
            for (var i = 0; i < K.Transducers.Length; i++)
                if (state.Transducers[i] is { IsDown: true } && Wants(client.Player, K.AttaControl, K.Transducers[i].Objective))
                    Record(state, client, K.AttaControl, K.TransducerScenario, K.Transducers[i].Objective);
        }

        private static void Explore(State state, Client client)
        {
            var player = client.Player;
            var objectives = new[] { K.ExploreGrotto, K.ExploreInnerNest, K.ExploreRoyalNest, K.ExploreQueensChamber };

            for (var i = 0; i < K.Places.Length; i++)
                if (Wants(player, K.BugHugger, objectives[i]) && K.Places[i].Contains(player.Position))
                    Record(state, client, K.BugHugger, K.ExploreScenario, K.Places[i].Event);
        }

        private static void OfferAttaControl(State state, Client client, long now)
        {
            var player = client.Player;

            if (!Done(player, K.IntoTheHive) || player.Missions.ContainsKey(K.AttaControl) || Done(player, K.AttaControl) ||
                state.OfferedAt.TryGetValue(player.Id, out var at) && now - at < OfferAgainMs)
                return;

            state.OfferedAt[player.Id] = now;
            MissionApplication.Instance?.Offers.TryOffer(client, K.AttaControl,
                global::Rasa.Missions.Definitions.MissionOfferSourceIdentity.ServerEvent(K.AttaControlOfferKey));
        }

        /// <summary>The Bane troubling the Queen: brought in by a member come to protect her, dead, her protection.</summary>
        private static void Queen(MapChannel map, State state, List<Client> members)
        {
            var chamber = K.Places[3];

            if (!state.QueenProtected)
            {
                var protecting = members.Where(client => Wants(client.Player, K.BugHugger, K.ProtectQueen) &&
                    chamber.Contains(client.Player.Position)).ToList();

                if (state.Raiders.Count == 0 && protecting.Count > 0)
                {
                    foreach (var spot in K.RaiderSpots)
                        if (Spawn(map, K.QueenRaider, spot, K.Throne) is { } raider)
                        {
                            raider.Hate.Ensure(Nearest(protecting, raider.Position).Player.EntityId, Threat.NoticedThreat);
                            state.Raiders.Add(raider);
                        }

                    Logger.WriteLog(LogType.Debug, $"Kardash Atta Colony {map.InstanceId}: {state.Raiders.Count} Bane come at the Queen.");
                }
                else if (state.Raiders.Count > 0 && state.Raiders.All(raider => !Alive(raider)))
                {
                    state.QueenProtected = true;
                    Logger.WriteLog(LogType.Debug, $"Kardash Atta Colony {map.InstanceId}: the Bane at the Queen are dead.");
                }
            }

            if (!state.QueenProtected)
                return;

            foreach (var client in members.Where(client => Wants(client.Player, K.BugHugger, K.ProtectQueen)))
                Record(state, client, K.BugHugger, K.ExploreScenario, K.QueenProtected);
        }

        #endregion

        #region Helpers

        /// <summary>The living players in the copy.</summary>
        private static List<Client> Members(MapChannel map) =>
            map.ClientList?.Where(client => client?.Player != null && client.State == ClientState.Ingame &&
                    client.Player.MapChannel == map && client.Player.State != CharacterState.Dead &&
                    client.Player.State != CharacterState.Dying).ToList()
            ?? new List<Client>();

        private static Client Nearest(List<Client> clients, Vector3 to) =>
            clients.OrderBy(client => Vector3.Distance(client.Player.Position, to)).First();

        private static bool Done(Manifestation player, uint missionId) =>
            player.MissionHistory.TryGetValue(missionId, out var state) && state is MissionState.Completed or MissionState.Success ||
            player.Missions.TryGetValue(missionId, out var log) && log.State is MissionState.Completed or MissionState.Success;

        /// <summary>Whether the player has the mission with the objective open.</summary>
        public static bool Wants(Manifestation player, uint missionId, uint objectiveId) =>
            player.Missions.TryGetValue(missionId, out var log) && log.State == MissionState.Active &&
            log.Objectives.TryGetValue(objectiveId, out var objective) && objective.State == MissionObjectiveState.Incomplete;

        private static bool Completed(Manifestation player, uint missionId, uint objectiveId) =>
            player.Missions.TryGetValue(missionId, out var log) && log.State == MissionState.Active &&
            log.Objectives.TryGetValue(objectiveId, out var objective) && objective.State == MissionObjectiveState.Completed;

        /// <summary>An event of a scenario, once for each assignment of the mission.</summary>
        private static void Record(State state, Client client, uint missionId, uint scenarioId, uint eventId)
        {
            if (!client.Player.Missions.TryGetValue(missionId, out var log))
                return;

            var key = (log.AssignmentId ?? string.Empty, missionId, scenarioId, eventId);

            if (state.Told.Contains(key))
                return;

            if (MissionApplication.Instance.TryEmitScenarioProgressEvent(client, missionId, scenarioId, eventId))
                state.Told.Add(key);
        }

        /// <summary>A creature of the row, put on the ground at the spot, facing the other.</summary>
        private static Creature Spawn(MapChannel map, uint dbId, Vector3 spot, Vector3 facingTo)
        {
            var creature = CreatureManager.Instance.CreateCreature(dbId, null);

            if (creature == null)
                return null;

            var at = NavMeshManager.SnapToGround(map, spot);
            var facing = Math.Atan2(facingTo.X - at.X, facingTo.Z - at.Z);

            CreatureManager.Instance.SetLocation(creature, at, facing, map.MapInfo.MapContextId);
            creature.LastYaw = (float)facing;
            CellManager.Instance.AddToWorld(map, creature);
            return creature;
        }

        /// <summary>Alive and still in the world as itself.</summary>
        private static bool Alive(Creature creature) =>
            creature != null && creature.State != CharacterState.Dead && creature.State != CharacterState.Dying &&
            EntityManager.Instance.Creatures.TryGetValue(creature.EntityId, out var registered) && ReferenceEquals(registered, creature);

        /// <summary>Forgets every copy; for tests.</summary>
        public static void Reset() => States.Clear();

        #endregion
    }
}
