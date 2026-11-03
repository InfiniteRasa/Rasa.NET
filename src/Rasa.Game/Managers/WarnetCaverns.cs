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
    using Structures;

    using W = Services.Preloader.WarnetCavernsContent;
    using M = Services.Preloader.WarnetCavernsMissions;
    using E = Services.Preloader.WarnetCavernsMissions.Events;

    /// <summary>
    /// What Warnet Caverns does, in each copy of it (map 1384, a squad instance), on what
    /// WarnetCavernsContent places, for WarnetCavernsMissions:
    ///
    ///  - The specialists. Sides, Brumley and Gooding wait at the entrance sandbags. Once the way to
    ///    the Research Base Camp is clear - every pool along it (W.WayPools) empty at once while
    ///    someone in the copy is clearing it (Reset the Research Stations' objective 6) - they walk
    ///    to the camp's mouth, and once the camp is clear (W.CampPools, Chelliak's with them) to
    ///    their posts in it, where the camp's two turrets come on. Both are each player's scenario
    ///    events (393's 1 and 2), and a player who comes in later with the objective open is told.
    ///    In a copy someone enters having done 393, they start at the camp's mouth and the camp
    ///    has to be cleared again (the wiki's "if you leave and return").
    ///  - Brumley's walk (Escort to Remote Station 3, 394). A player who has told him they are
    ///    ready and stands by him takes him along: he follows them on the navmesh, fighting
    ///    nothing, the escort icon over him, and goes back to his post if they die, leave or get
    ///    more than <see cref="LostRadius"/> from him. Within <see cref="ArriveRadius"/> of Remote
    ///    Station 3 he has arrived: event 1 for every player on it in the copy, and he stays there.
    ///  - The Queen Lure (Kill the Queen, 396). The stand at the Nest's mouth takes a player's use
    ///    while they are to place the lure or to kill the Queen and hold the lure; it keeps the
    ///    lure (the mission takes it at the end). Placed, it is event 1 for every player on it in
    ///    the copy; the Queen comes (her scripted pool, once she is not about), and an attack of
    ///    level-19 warnets (<see cref="WaveSize"/>) out of the Nest at whoever is nearest. Her death
    ///    is event 2 for every player on it, and another attack comes.
    ///  - The Hanging Hives in Bug City. Once all six have been brought down, Tozid comes out
    ///    (his scripted pool), and again after that once they have all been brought down again.
    ///  - The metal container at the Dig Site gives one only to a player who wants one for
    ///    Create the Queen Lure (395) and has none.
    /// </summary>
    public static class WarnetCaverns
    {
        public const uint Map = W.Map;

        /// <summary>How near a player who told him they were ready Brumley has to be to go with them.</summary>
        public const float TakeAlongRadius = 12f;

        /// <summary>Brumley this far from the player he follows has lost them.</summary>
        public const float LostRadius = 60f;

        /// <summary>Brumley this near Remote Station 3 has arrived; a player on 394 this near it when he has is told.</summary>
        public const float ArriveRadius = 10f, StationNearRadius = 40f;

        /// <summary>The lure's attacks: this many level-19 warnets for one player, so many more for each other one in the Nest, no more than the pools.</summary>
        public const int WaveBase = 5, WavePerMember = 2;

        /// <summary>The Nest (WarnetCavernsRegions' box), where the squad that a wave is for is counted.</summary>
        public static readonly (float West, float East, float South, float North) Nest = (140f, 393f, 191f, 442f);

        /// <summary>How often the copy's mission state is looked at again for whoever came in.</summary>
        public const long CatchUpMs = 2000;

        public enum Place { Entrance, Mouth, Post }

        public sealed class State
        {
            public bool Set;
            public bool WayCleared, SiteCleared, TurretsOn;
            public Place Specialists = Place.Entrance;

            /// <summary>Where each specialist was last sent, by creature: a new one (respawned) is sent again.</summary>
            public readonly Dictionary<Creature, Vector3> SentTo = new();

            /// <summary>Whom Brumley follows, by character; 0 for nobody.</summary>
            public uint BrumleyFollows;
            public bool BrumleyAtStation;

            /// <summary>The Queen as last seen alive, and whether she was.</summary>
            public bool QueenSeen;

            /// <summary>Wave pools woken whose warnets are still to be sent at the squad.</summary>
            public readonly HashSet<uint> Rushing = new();

            /// <summary>The hives seen down since Tozid last came out.</summary>
            public readonly HashSet<uint> HivesDown = new();
            public int TozidOuts;

            /// <summary>Scenario events already told, by player, assignment and event.</summary>
            public readonly HashSet<string> Told = new();

            public long NextCatchUpAt;
        }

        private static readonly ConditionalWeakTable<MapChannel, State> States = new();

        /// <summary>The clock; a test's to replace.</summary>
        public static Func<long> Now { get; set; } = () => Environment.TickCount64;

        public static bool IsCaverns(MapChannel map) => map?.MapInfo?.MapContextId == Map;

        public static State StateOf(MapChannel map) => IsCaverns(map) ? States.GetValue(map, _ => new State()) : null;

        /// <summary>How many warnets an attack sends for <paramref name="squad"/> players in the Nest.</summary>
        public static int WaveSize(int squad) =>
            (int)Math.Min(W.WavePools, WaveBase + WavePerMember * Math.Max(0, squad - 1));

        /// <summary>From the map channel worker.</summary>
        public static void Worker(MapChannel map)
        {
            var state = StateOf(map);

            if (state == null)
                return;

            var now = Now();
            var members = Members(map);

            if (!state.Set)
            {
                state.Set = true;
                if (members.Any(client => Done(client.Player, M.ResetStations)))
                    state.WayCleared = true;
            }

            Clearing(map, state, members);
            Specialists(map, state);
            Brumley(map, state, members);
            Queen(map, state, members);
            Rush(map, state, members);
            Hives(map, state);

            if (now >= state.NextCatchUpAt)
            {
                state.NextCatchUpAt = now + CatchUpMs;
                CatchUp(map, state, members);
            }
        }

        #region The way and the camp

        private static void Clearing(MapChannel map, State state, List<Client> members)
        {
            if (!state.WayCleared && members.Any(client => Wants(client.Player, M.ResetStations, 6)) && Empty(map, W.WayPools))
            {
                state.WayCleared = true;
                Logger.WriteLog(LogType.Debug, $"Warnet Caverns {map.InstanceId}: the way to the Research Base Camp is clear.");
            }

            if (state.WayCleared && !state.SiteCleared && Empty(map, W.CampPools))
            {
                state.SiteCleared = true;
                Logger.WriteLog(LogType.Debug, $"Warnet Caverns {map.InstanceId}: the Research Base Camp is clear.");
            }

            state.Specialists = state.SiteCleared ? Place.Post : state.WayCleared ? Place.Mouth : Place.Entrance;

            if (state.SiteCleared && !state.TurretsOn)
            {
                state.TurretsOn = true;
                foreach (var id in W.CampTurretPools)
                    if (Pool(map, id) is { } pool)
                        pool.Woken = true;
            }
        }

        /// <summary>Whether every pool named has put its creatures down and has none left about.</summary>
        public static bool Empty(MapChannel map, IEnumerable<uint> ids)
        {
            foreach (var id in ids)
            {
                var pool = Pool(map, id);

                if (pool == null)
                    continue;

                if (!pool.HasSpawned || pool.AliveCreatures > 0 || pool.QueuedCreatures > 0 || pool.DropshipQueue > 0)
                    return false;
            }

            return true;
        }

        #endregion

        #region The specialists

        private static void Specialists(MapChannel map, State state)
        {
            foreach (var creature in Creatures(map).Where(creature => creature.DbId is W.Sides or W.Brumley or W.Gooding))
            {
                if (creature.DbId == W.Brumley && state.BrumleyFollows != 0)
                    continue;

                var spot = SpotOf(creature.DbId, state);

                if (state.SentTo.TryGetValue(creature, out var sent) && sent == spot)
                    continue;

                Send(creature, spot);
                state.SentTo[creature] = spot;
            }

            foreach (var gone in state.SentTo.Keys.Where(creature => !Alive(creature)).ToList())
                state.SentTo.Remove(gone);
        }

        /// <summary>Where a specialist belongs as the copy stands.</summary>
        public static Vector3 SpotOf(uint creature, State state)
        {
            if (creature == W.Brumley && state.BrumleyAtStation)
                return V(W.RemoteStation);

            return (creature, state.Specialists) switch
            {
                (W.Sides, Place.Entrance) => V(W.SidesAtEntrance),
                (W.Brumley, Place.Entrance) => V(W.BrumleyAtEntrance),
                (W.Gooding, Place.Entrance) => V(W.GoodingAtEntrance),
                (W.Sides, Place.Mouth) => V(W.SidesAtMouth),
                (W.Brumley, Place.Mouth) => V(W.BrumleyAtMouth),
                (W.Gooding, Place.Mouth) => V(W.GoodingAtMouth),
                (W.Sides, _) => V(W.SidesAtPost),
                (W.Brumley, _) => V(W.BrumleyAtPost),
                _ => V(W.GoodingAtPost)
            };
        }

        /// <summary>A specialist walks to the spot and stays there; it is home now.</summary>
        private static void Send(Creature creature, Vector3 spot)
        {
            creature.HomePos.Position = spot;
            BehaviorManager.Instance.SetActionAnchor(creature, spot);
        }

        #endregion

        #region Brumley's walk

        private static void Brumley(MapChannel map, State state, List<Client> members)
        {
            var brumley = Creatures(map).FirstOrDefault(creature => creature.DbId == W.Brumley);

            if (brumley == null)
            {
                state.BrumleyFollows = 0;
                return;
            }

            if (state.BrumleyFollows != 0)
            {
                var leader = members.FirstOrDefault(client => client.Player.Id == state.BrumleyFollows);

                if (leader == null || Vector3.Distance(leader.Player.Position, brumley.Position) > LostRadius)
                {
                    LetGo(map, state, brumley, "lost his player");
                    return;
                }

                if (Near(brumley, W.RemoteStation, ArriveRadius))
                {
                    state.BrumleyAtStation = true;
                    LetGo(map, state, brumley, "is at Remote Station 3");
                    Send(brumley, V(W.RemoteStation));
                    state.SentTo[brumley] = V(W.RemoteStation);

                    foreach (var client in members.Where(client => Wants(client.Player, M.EscortToStation, 1)))
                        Emit(client, M.EscortToStation, E.ArrivedAtStation, state);
                }

                return;
            }

            if (state.BrumleyAtStation)
                return;

            var ready = members.FirstOrDefault(client => Wants(client.Player, M.EscortToStation, 1) &&
                Vector3.Distance(client.Player.Position, brumley.Position) <= TakeAlongRadius);

            if (ready != null)
                Follow(map, state, brumley, ready);
        }

        private static void Follow(MapChannel map, State state, Creature brumley, Client leader)
        {
            state.BrumleyFollows = leader.Player.Id;

            if (brumley.SpawnPool != null)
            {
                brumley.SpawnPool.FollowOwnerCharacterId = leader.Player.Id;
                brumley.SpawnPool.ScenarioOwnerCharacterId = leader.Player.Id;
                brumley.SpawnPool.ScenarioMissionId = M.EscortToStation;
            }

            BehaviorManager.Instance.SetActionFollow(brumley, leader.Player.EntityId);
            CreatureManager.PublishEscortStatus(map, brumley, true);
            Logger.WriteLog(LogType.Debug, $"Warnet Caverns {map.InstanceId}: Brumley follows character {leader.Player.Id}.");
        }

        private static void LetGo(MapChannel map, State state, Creature brumley, string why)
        {
            CreatureManager.PublishEscortStatus(map, brumley, false);
            state.BrumleyFollows = 0;
            state.SentTo.Remove(brumley);

            if (brumley.SpawnPool != null)
            {
                brumley.SpawnPool.FollowOwnerCharacterId = 0;
                brumley.SpawnPool.ScenarioOwnerCharacterId = 0;
                brumley.SpawnPool.ScenarioMissionId = 0;
            }

            Logger.WriteLog(LogType.Debug, $"Warnet Caverns {map.InstanceId}: Brumley {why} at {brumley.Position}.");
        }

        #endregion

        #region The Queen

        /// <summary>Why the player cannot use one of the caverns' objects now, or null if they can (PlacedUsables).</summary>
        public static PlayerMessage? RefusalOf(Client client, PlacedUsables.Live live)
        {
            if (!IsCaverns(live?.Map) || client?.Player == null)
                return null;

            var player = client.Player;

            switch (live.Placement.Id)
            {
                case W.LureAcceptor:
                    if (!Wants(player, M.KillTheQueen, 2) && !Wants(player, M.KillTheQueen, 1))
                        return PlayerMessage.PmUseObjectNotUsable;
                    return InventoryManager.Instance.CountItemsByClass(client, (EntityClasses)W.QueenLureClass, includeMissionItems: true) > 0
                        ? null
                        : PlayerMessage.PmMissingReqItem;

                case W.MetalContainerDispenser:
                    if (!Wants(player, M.QueenLure, 1) ||
                        InventoryManager.Instance.CountItemsByClass(client, (EntityClasses)W.MetalContainerClass, includeMissionItems: true) > 0)
                        return PlayerMessage.PmUseObjectNotUsable;
                    return null;
            }

            return null;
        }

        /// <summary>Whether an acceptor of the caverns takes nothing: the lure stays with its mission.</summary>
        public static bool TakesNothing(PlacedUsables.Live live) => IsCaverns(live?.Map) && live.Placement.Id == W.LureAcceptor;

        /// <summary>A use of one of the caverns' objects has gone through (PlacedUsables).</summary>
        public static void Used(Client client, PlacedUsables.Live live, UseObjectState next)
        {
            if (!IsCaverns(live?.Map) || client?.Player == null || live.Placement.Id != W.LureAcceptor)
                return;

            LurePlaced(live.Map, StateOf(live.Map), client);
        }

        /// <summary>The lure placed: told to every player on it, the Queen called and an attack out of the Nest.</summary>
        public static void LurePlaced(MapChannel map, State state, Client by)
        {
            var members = Members(map);

            foreach (var client in members.Where(client => Wants(client.Player, M.KillTheQueen, 2)))
                Emit(client, M.KillTheQueen, E.LurePlaced, state);

            if (!Creatures(map).Any(creature => creature.DbId == W.Queen) && Pool(map, W.QueenPool) is { } queen &&
                queen.AliveCreatures == 0 && queen.QueuedCreatures == 0)
                Wake(queen);

            Wave(map, state, W.FirstLureWavePool, members);
            Logger.WriteLog(LogType.Debug, $"Warnet Caverns {map.InstanceId}: the Queen Lure placed by character {by?.Player?.Id}.");
        }

        private static void Queen(MapChannel map, State state, List<Client> members)
        {
            var queen = Creatures(map).FirstOrDefault(creature => creature.DbId == W.Queen);

            if (queen != null)
            {
                // Out: her pool sleeps again, so that she is not back on its timer.
                if (Pool(map, W.QueenPool) is { } pool)
                    pool.Woken = false;
                state.QueenSeen = true;
                return;
            }

            if (!state.QueenSeen)
                return;

            state.QueenSeen = false;

            foreach (var client in members.Where(client => Wants(client.Player, M.KillTheQueen, 1)))
                Emit(client, M.KillTheQueen, E.QueenKilled, state);

            Wave(map, state, W.FirstDeathWavePool, members);
            Logger.WriteLog(LogType.Debug, $"Warnet Caverns {map.InstanceId}: the Queen Warnet Matriarch is dead.");
        }

        /// <summary>An attack: as many of the wave's pools as the squad in the Nest wants, each one warnet, sent at the nearest player.</summary>
        private static void Wave(MapChannel map, State state, uint firstPool, List<Client> members)
        {
            var squad = Math.Max(1, members.Count(client => InNest(client.Player.Position)));
            var size = WaveSize(squad);
            var woken = 0;

            for (uint i = 0; i < W.WavePools && woken < size; i++)
            {
                var id = firstPool + i;

                if (Pool(map, id) is not { } pool || pool.AliveCreatures > 0 || pool.QueuedCreatures > 0)
                    continue;

                Wake(pool);
                state.Rushing.Add(id);
                woken++;
            }
        }

        /// <summary>A woken wave pool's warnet: its pool to sleep again, and the warnet at the nearest living player.</summary>
        private static void Rush(MapChannel map, State state, List<Client> members)
        {
            foreach (var id in state.Rushing.ToList())
            {
                var pool = Pool(map, id);

                if (pool == null)
                {
                    state.Rushing.Remove(id);
                    continue;
                }

                if (pool.AliveCreatures == 0 && pool.QueuedCreatures == 0)
                    continue;

                pool.Woken = false;

                foreach (var warnet in Creatures(map).Where(creature => creature.SpawnPool == pool))
                {
                    var target = members.OrderBy(client => Vector3.Distance(client.Player.Position, warnet.Position)).FirstOrDefault();

                    if (target != null)
                        BehaviorManager.Instance.TrySetActionFighting(warnet, target.Player.EntityId);
                }

                if (pool.QueuedCreatures == 0)
                    state.Rushing.Remove(id);
            }
        }

        public static bool InNest(Vector3 at) => at.X >= Nest.West && at.X <= Nest.East && at.Z >= Nest.South && at.Z <= Nest.North;

        #endregion

        #region The hives and Tozid

        private static void Hives(MapChannel map, State state)
        {
            foreach (var hive in W.Hives)
                if (PlacedDestructibles.LiveOf(map, hive.Id) is { IsDown: true })
                    state.HivesDown.Add(hive.Id);

            if (Pool(map, W.TozidPool) is not { } tozid)
                return;

            // Out: his pool sleeps again, so that he is not back on its timer.
            if (tozid.AliveCreatures > 0)
            {
                tozid.Woken = false;
                return;
            }

            if (tozid.QueuedCreatures > 0 || tozid.DropshipQueue > 0 || state.HivesDown.Count < W.Hives.Length)
                return;

            Wake(tozid);
            state.HivesDown.Clear();
            state.TozidOuts++;
            Logger.WriteLog(LogType.Debug, $"Warnet Caverns {map.InstanceId}: the Hanging Hives are down; Tozid comes out.");
        }

        #endregion

        #region Catching up

        private static void CatchUp(MapChannel map, State state, List<Client> members)
        {
            foreach (var client in members)
            {
                var player = client.Player;

                if (state.WayCleared && Wants(player, M.ResetStations, 6))
                    Emit(client, M.ResetStations, E.WayCleared, state);

                if (state.SiteCleared && Wants(player, M.ResetStations, 2))
                    Emit(client, M.ResetStations, E.SiteCleared, state);

                if (state.BrumleyAtStation && Wants(player, M.EscortToStation, 1) &&
                    Vector3.Distance(player.Position, V(W.RemoteStation)) <= StationNearRadius)
                    Emit(client, M.EscortToStation, E.ArrivedAtStation, state);
            }
        }

        #endregion

        #region Helpers

        private static List<Client> Members(MapChannel map) =>
            map.ClientList?.Where(client => client?.Player != null && client.State == ClientState.Ingame &&
                    client.Player.MapChannel == map && client.Player.State != CharacterState.Dead &&
                    client.Player.State != CharacterState.Dying).ToList()
            ?? new List<Client>();

        private static List<Creature> Creatures(MapChannel map) =>
            map.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList).Distinct().Where(Alive).ToList();

        private static bool Alive(Creature creature) =>
            creature != null && creature.State != CharacterState.Dead && creature.State != CharacterState.Dying &&
            EntityManager.Instance.Creatures.TryGetValue(creature.EntityId, out var registered) && ReferenceEquals(registered, creature);

        private static SpawnPool Pool(MapChannel map, uint id) => map.SpawnPools.FirstOrDefault(pool => pool.DbId == id);

        /// <summary>A scripted pool set going now, whatever is left of its respawn time.</summary>
        private static void Wake(SpawnPool pool)
        {
            pool.Woken = true;
            pool.UpdateTimer = pool.RespawnTime;
        }

        private static Vector3 V(W.Spot spot) => new((float)spot.X, (float)spot.Y, (float)spot.Z);

        private static bool Near(Creature creature, W.Spot spot, float radius) => Vector3.Distance(creature.Position, V(spot)) <= radius;

        private static bool Done(Manifestation player, uint missionId) =>
            player.MissionHistory.TryGetValue(missionId, out var state) && state is MissionState.Completed or MissionState.Success ||
            player.Missions.TryGetValue(missionId, out var log) && log.State is MissionState.Completed or MissionState.Success;

        /// <summary>Whether the player has the mission with the objective open.</summary>
        public static bool Wants(Manifestation player, uint missionId, uint objectiveId) =>
            player != null && player.Missions.TryGetValue(missionId, out var log) && log.State == MissionState.Active &&
            log.Objectives.TryGetValue(objectiveId, out var objective) && objective.State == MissionObjectiveState.Incomplete;

        /// <summary>A scenario event to the player's mission, once for their assignment.</summary>
        private static void Emit(Client client, uint missionId, uint eventId, State state)
        {
            var player = client.Player;

            if (!player.Missions.TryGetValue(missionId, out var log))
                return;

            var key = $"{player.Id}:{log.AssignmentId}:{missionId}:{eventId}";

            if (state.Told.Contains(key))
                return;

            if (MissionApplication.Instance?.TryEmitScenarioProgressEvent(client, missionId, E.Scenario, eventId) == true)
                state.Told.Add(key);
        }

        #endregion

        #region A GM's

        /// <summary>
        /// .warnet: what this copy has done, and a GM's hand in it.
        ///   .warnet         - the way, the camp, the specialists, Brumley, the Queen, the hives
        ///   .warnet way     - the way to the camp clear
        ///   .warnet camp    - the way and the camp clear
        ///   .warnet lure    - the lure placed (the Queen and an attack)
        ///   .warnet station - Brumley at Remote Station 3
        ///   .warnet reset   - the copy as it was
        /// </summary>
        public static IEnumerable<string> Command(MapChannel map, Client gm, string[] parts)
        {
            var state = StateOf(map);

            if (state == null)
            {
                yield return "Not in Warnet Caverns.";
                yield break;
            }

            switch (parts.Length > 1 ? parts[1].ToLowerInvariant() : string.Empty)
            {
                case "way":
                    state.WayCleared = true;
                    break;

                case "camp":
                    state.WayCleared = state.SiteCleared = true;
                    break;

                case "lure":
                    LurePlaced(map, state, gm);
                    break;

                case "station":
                    state.BrumleyAtStation = true;
                    state.SentTo.Clear();
                    break;

                case "reset":
                    state.WayCleared = state.SiteCleared = state.BrumleyAtStation = false;
                    state.BrumleyFollows = 0;
                    state.SentTo.Clear();
                    state.HivesDown.Clear();
                    state.Told.Clear();
                    break;
            }

            yield return $"Warnet Caverns {map.InstanceId}: way {(state.WayCleared ? "clear" : "held")}, camp {(state.SiteCleared ? "clear" : "held")}, " +
                $"specialists at the {state.Specialists.ToString().ToLowerInvariant()}, turrets {(state.TurretsOn ? "on" : "off")}.";
            yield return $"Brumley {(state.BrumleyFollows != 0 ? $"following character {state.BrumleyFollows}" : state.BrumleyAtStation ? "at Remote Station 3" : "at his post")}; " +
                $"the Queen {(state.QueenSeen ? "out" : "not out")}; hives down {state.HivesDown.Count}/{W.Hives.Length}, Tozid out {state.TozidOuts} time(s).";
        }

        #endregion

        /// <summary>Forgets every copy; for tests.</summary>
        public static void Reset() => States.Clear();
    }
}
