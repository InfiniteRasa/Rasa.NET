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
    using Packets;
    using Packets.MapChannel.Server;
    using Models;
    using Structures;

    using D = Services.Preloader.DevilsDenContent;

    /// <summary>
    /// What the Devil's Den (1394) does that its rows do not: each copy of it - a squad's
    /// instance, or the map's own channel - runs it for whoever is in it (DevilsDenContent has
    /// the people, the creatures, the objects and the missions).
    ///
    ///  - What is destroyed and counts: each egg cluster (Operation Devil's Strike) and each
    ///    power node, landing pad machine and the fuel depot (Sever the Devil's Head), going down,
    ///    records its own event for every member of the copy on the objective. The machines and
    ///    the depot stay down <see cref="MachineryDownMs"/>; the clusters come back as any
    ///    spawner does.
    ///  - The Bane teleporters on the Den's platform: each standing one sends a Bane every
    ///    <see cref="TeleporterIntervalMs"/> while a player is within
    ///    <see cref="TeleporterRange"/>, no more than <see cref="TeleporterLimit"/> of its own
    ///    about at once, after the nearest player; destroyed, it sends none, and stays down
    ///    <see cref="TeleporterDownMs"/>.
    ///  - The bombs (at the generator, the laser door and the fuel depot), set with a detonator
    ///    from the platform's crate: going off, each destroys what is placed within its radius.
    ///  - The detention center's laser door is down while its console is switched on or the
    ///    generator is down, and comes back when neither is so; it can be shot down too.
    ///  - The howitzer panels in the bunkers: switched on, the howitzer nearer the panel is
    ///    turned on the Bane (Friendly, after the Bane within its reach); off, it is the Bane's
    ///    again.
    ///  - Captain Fransisco: found (Missing Strike Team's first objective done), he follows the
    ///    one who found him while they have the mission - fighting with them - and is put beside
    ///    them when they are more than <see cref="FransiscoWarpDistance"/> away or where he
    ///    cannot walk to them (the Den's caves are not all joined on its navmesh). Killed, he is
    ///    back in his alcove when his pool brings him back, and follows whoever on the mission,
    ///    their first objective done, comes within <see cref="FransiscoFindRadius"/> of him.
    ///  - Shot down or blown up, the laser door stays down <see cref="DoorDownMs"/>; the
    ///    generator <see cref="GeneratorDownMs"/>.
    /// </summary>
    public static class DevilsDen
    {
        public const long MachineryDownMs = 600_000;
        public const long TeleporterIntervalMs = 40_000;
        public const float TeleporterRange = 60f;
        public const int TeleporterLimit = 2;
        public const long TeleporterDownMs = 1_800_000;
        public const long GeneratorDownMs = 300_000;
        public const long DoorDownMs = 300_000;
        public const float HowitzerReach = 60f;
        public const float FransiscoWarpDistance = 40f;
        public const float FransiscoFindRadius = 10f;

        /// <summary>One copy of the Den, as it stands.</summary>
        internal sealed class State
        {
            /// <summary>The destructibles seen down, by row: each going down is recorded once.</summary>
            public readonly HashSet<uint> Down = new();

            /// <summary>The bombs that have gone off and been dealt with, by row, until they are disarmed again.</summary>
            public readonly HashSet<uint> Blown = new();

            public readonly Dictionary<uint, long> NextTeleport = new();
            public readonly Dictionary<uint, List<Creature>> Teleported = new();

            /// <summary>Whom Fransisco follows, by character; 0 for nobody.</summary>
            public uint Follows;

            /// <summary>His pool, as last seen with him in it: let go of when he is dead.</summary>
            public SpawnPool Pool;
        }

        private static readonly ConditionalWeakTable<MapChannel, State> States = new();
        private static readonly Random Random = new();

        /// <summary>The clock; a test's to replace.</summary>
        public static Func<long> Now { get; set; } = () => Environment.TickCount64;

        public static bool IsDen(MapChannel map) => map?.MapInfo?.MapContextId == D.Den;

        internal static State StateOf(MapChannel map) => IsDen(map) ? States.GetValue(map, _ => new State()) : null;

        /// <summary>Whom Fransisco follows in the copy, by character; 0 for nobody.</summary>
        public static uint FransiscoFollows(MapChannel map) => StateOf(map)?.Follows ?? 0;

        /// <summary>From the map channel worker.</summary>
        public static void Worker(MapChannel map)
        {
            if (!IsDen(map))
                return;

            var state = StateOf(map);
            var now = Now();
            var members = Members(map);

            Bombs(map, state);
            Destroyed(map, state, members);
            LaserDoor(map);

            if (members.Count == 0)
                return;

            Teleporters(map, state, members, now);
            Howitzers(map);
            Fransisco(map, state, members);
        }

        #region What is destroyed

        /// <summary>The event a destroyed row records: its mission, scenario and event; null for one that records none.</summary>
        public static (uint Mission, uint Objective, uint Scenario, uint Event)? EventOf(uint rowId)
        {
            if (rowId >= D.FirstEggCluster && rowId < D.FirstEggCluster + D.EggClusters.Length)
                return (D.DevilsStrike, 2, D.EggScenario, rowId - D.FirstEggCluster + 1);

            if (rowId >= D.FirstNode && rowId < D.FirstNode + 3)
                return (D.SeverTheHead, 3, D.NodeScenario, rowId - D.FirstNode + 1);

            if (rowId >= D.FirstPad && rowId < D.FirstPad + 3)
                return (D.SeverTheHead, 4, D.PadScenario, rowId - D.FirstPad + 1);

            if (rowId == D.FuelDepot)
                return (D.SeverTheHead, 5, D.DepotScenario, 1);

            return null;
        }

        private static void Destroyed(MapChannel map, State state, List<Client> members)
        {
            foreach (var live in PlacedDestructibles.On(map))
            {
                var id = live.Placement.Id;

                if (live.Placement.IsPutDown)
                    continue;

                if (!live.IsDown)
                {
                    state.Down.Remove(id);
                    continue;
                }

                if (!state.Down.Add(id))
                    continue;

                // Down, and newly so: what stays down a while, and what it counts for.
                if (id is >= D.FirstNode and <= D.FuelDepot)
                    HoldDown(live, MachineryDownMs);
                else if (id == D.LaserDoor)
                    HoldDown(live, DoorDownMs);
                else if (id >= D.FirstTeleporter && id < D.FirstTeleporter + 4)
                    HoldDown(live, TeleporterDownMs);
                else if (id == D.GeneratorRow)
                    HoldDown(live, GeneratorDownMs);

                if (EventOf(id) is not { } counted)
                    continue;

                foreach (var client in members.Where(client => Wants(client.Player, counted.Mission, counted.Objective)))
                    MissionApplication.Instance.TryEmitScenarioProgressEvent(client, counted.Mission, counted.Scenario, counted.Event);
            }
        }

        private static void HoldDown(PlacedDestructibles.Live live, long ms) =>
            live.RespawnAt = PlacedDestructibles.Now() + ms;

        #endregion

        #region Bombs, the generator and the laser door

        private static readonly uint[] BombRows = { D.GeneratorBomb, D.DoorBomb, D.DepotBomb };

        private static void Bombs(MapChannel map, State state)
        {
            foreach (var row in BombRows)
            {
                var bomb = PlacedUsables.LiveOf(map, row);

                if (bomb == null)
                    continue;

                if (bomb.State != UseObjectState.BombStateDetonated)
                {
                    state.Blown.Remove(row);
                    continue;
                }

                if (!state.Blown.Add(row))
                    continue;

                var by = bomb.ArmedBy != 0 && EntityManager.Instance.Players.TryGetValue(bomb.ArmedBy, out var player) ? player : null;

                foreach (var live in PlacedDestructibles.On(map).Where(live => !live.IsDown && !live.Placement.IsPutDown &&
                             Vector3.Distance(live.Object.Position, bomb.Object.Position) <= bomb.Placement.Radius))
                    PlacedDestructibles.Destroy(live, by);
            }
        }

        /// <summary>Down while its console is on or the generator is down; back as any force field comes back otherwise.</summary>
        private static void LaserDoor(MapChannel map)
        {
            var door = PlacedDestructibles.LiveOf(map, D.LaserDoor);

            if (door == null)
                return;

            var console = PlacedUsables.LiveOf(map, D.DoorConsole);
            var generator = PlacedDestructibles.LiveOf(map, D.GeneratorRow);
            var open = console?.State == UseObjectState.TsState1 || generator?.IsDown == true;

            if (!open)
                return;

            if (!door.IsDown)
                PlacedDestructibles.Destroy(door, null);

            HoldDown(door, PlacedDestructibles.RespawnMaxMs);
        }

        /// <summary>Whether the copy's laser door stands.</summary>
        public static bool DoorStands(MapChannel map) => PlacedDestructibles.LiveOf(map, D.LaserDoor) is { IsDown: false };

        #endregion

        #region The Bane teleporters

        private static void Teleporters(MapChannel map, State state, List<Client> members, long now)
        {
            for (var row = D.FirstTeleporter; row < D.FirstTeleporter + 4; row++)
            {
                var live = PlacedDestructibles.LiveOf(map, row);

                if (live == null || live.IsDown)
                    continue;

                var at = live.Object.Position;
                var near = members.Select(client => client.Player)
                    .Where(player => Vector3.Distance(player.Position, at) <= TeleporterRange)
                    .OrderBy(player => Vector3.Distance(player.Position, at)).FirstOrDefault();

                if (near == null)
                    continue;

                if (!state.Teleported.TryGetValue(row, out var sent))
                    state.Teleported[row] = sent = new List<Creature>();

                sent.RemoveAll(creature => !Alive(creature));

                if (state.NextTeleport.TryGetValue(row, out var next) && now < next || sent.Count >= TeleporterLimit)
                    continue;

                state.NextTeleport[row] = now + TeleporterIntervalMs;

                var bane = Spawn(map, D.TeleportedBane[Random.Next(D.TeleportedBane.Length)], at, 3f, near.Position);

                if (bane == null)
                    continue;

                bane.Hate.Ensure(near.EntityId, Threat.NoticedThreat);
                sent.Add(bane);
            }
        }

        /// <summary>What a teleporter has sent that is still about, for a test.</summary>
        public static IReadOnlyList<Creature> SentBy(MapChannel map, uint row) =>
            StateOf(map)?.Teleported.TryGetValue(row, out var sent) == true ? sent.Where(Alive).ToList() : new List<Creature>();

        #endregion

        #region The howitzers

        private static void Howitzers(MapChannel map)
        {
            var howitzers = Creatures(map).Where(creature => creature.DbId == D.Howitzer).ToList();

            foreach (var (row, mortar) in new[] { (D.WestPanel, D.MortarWest), (D.EastPanel, D.MortarEast) })
            {
                var panel = PlacedUsables.LiveOf(map, row);
                var howitzer = howitzers.OrderBy(creature => Vector3.Distance(creature.Position, mortar)).FirstOrDefault();

                if (panel == null || howitzer == null || Vector3.Distance(howitzer.Position, mortar) > 10f)
                    continue;

                if (panel.State != UseObjectState.TsState1)
                {
                    SetSide(howitzer, TargetCategory.Hostile);
                    continue;
                }

                SetSide(howitzer, TargetCategory.Friendly);

                var target = Creatures(map)
                    .Where(creature => creature != howitzer && creature.TargetCategory == TargetCategory.Hostile &&
                        Vector3.Distance(creature.Position, howitzer.Position) <= HowitzerReach)
                    .OrderBy(creature => Vector3.Distance(creature.Position, howitzer.Position)).FirstOrDefault();

                if (target != null && !howitzer.Hate.Contains(target.EntityId))
                    howitzer.Hate.Ensure(target.EntityId, Threat.NoticedThreat);
            }
        }

        private static void SetSide(Creature creature, TargetCategory category)
        {
            if (creature.TargetCategory == category)
                return;

            creature.TargetCategory = category;
            creature.Hate.Clear();
            CellManager.Instance.CellCallMethod(creature, new TargetCategoryPacket(category));
            BehaviorManager.Instance.StopFighting(creature);
        }

        #endregion

        #region Fransisco

        /// <summary>Fransisco, alive and in this copy; null if he is not.</summary>
        public static Creature FransiscoIn(MapChannel map) => Creatures(map).FirstOrDefault(creature => creature.DbId == D.Fransisco);

        /// <summary>Whether a player has found him and is still on the mission.</summary>
        public static bool HasFound(Manifestation player) =>
            player.Missions.TryGetValue(D.MissingStrikeTeam, out var log) && log.State == MissionState.Active &&
            log.Objectives.TryGetValue(1, out var found) && found.State == MissionObjectiveState.Completed;

        private static void Fransisco(MapChannel map, State state, List<Client> members)
        {
            var fransisco = FransiscoIn(map);
            var pool = fransisco?.SpawnPool;

            if (fransisco == null || pool == null)
            {
                // Dead, or not yet out: no one's, and back in his alcove when his pool brings him back.
                if (state.Pool != null)
                    Release(state.Pool);

                state.Pool = null;
                state.Follows = 0;
                return;
            }

            state.Pool = pool;

            var owner = members.FirstOrDefault(client => client.Player.Id == state.Follows && HasFound(client.Player));

            if (owner == null)
            {
                owner = members.FirstOrDefault(client => HasFound(client.Player) &&
                    (!map.IsPrivateInstance || map.OwnerCharacterId == client.Player.Id) &&
                    Vector3.Distance(client.Player.Position, fransisco.Position) <= FransiscoFindRadius);

                if (owner == null)
                {
                    if (state.Follows != 0)
                        Release(pool);

                    state.Follows = 0;
                    return;
                }

                state.Follows = owner.Player.Id;
                pool.ScenarioMissionId = D.MissingStrikeTeam;
                pool.ScenarioOwnerCharacterId = owner.Player.Id;
                pool.FollowOwnerCharacterId = owner.Player.Id;
                CreatureManager.PublishEscortStatus(map, fransisco, true);
                Logger.WriteLog(LogType.Debug, $"Devil's Den {map.InstanceId}: Captain Fransisco follows {owner.Player.FamilyName}.");
            }

            var to = owner.Player.Position;
            var gap = Vector3.Distance(fransisco.Position, to);

            if (gap <= FransiscoWarpDistance && map.NavMesh?.Connected(fransisco.Position, to) != false)
                return;

            // Too far, or where he cannot walk: put beside them.
            var beside = NavMeshManager.RandomPointAround(map, to, 3f) is { } near && Vector3.Distance(near, to) <= 5f ? near : to;
            beside = NavMeshManager.SnapToGround(map, beside);
            var facing = Math.Atan2(to.X - beside.X, to.Z - beside.Z);

            if (BehaviorManager.Instance.RestoreScriptedPose(map, fransisco, beside, facing))
            {
                fransisco.Controller.ScriptedMove = null;
                fransisco.Controller.CurrentAction = BehaviorManager.BehaviorActionIdle;
            }
        }

        private static void Release(SpawnPool pool)
        {
            pool.FollowOwnerCharacterId = 0;
            pool.ScenarioOwnerCharacterId = 0;
            pool.ScenarioMissionId = 0;
        }

        #endregion

        #region Helpers

        /// <summary>Whether the player has the mission with the objective open.</summary>
        public static bool Wants(Manifestation player, uint missionId, uint objectiveId) =>
            player.Missions.TryGetValue(missionId, out var log) && log.State == MissionState.Active &&
            log.Objectives.TryGetValue(objectiveId, out var objective) && objective.State == MissionObjectiveState.Incomplete;

        private static List<Client> Members(MapChannel map) =>
            map.ClientList?.Where(client => client?.Player != null && client.State == ClientState.Ingame &&
                    client.Player.MapChannel == map && client.Player.State != CharacterState.Dead).ToList()
            ?? new List<Client>();

        private static IEnumerable<Creature> Creatures(MapChannel map) =>
            map.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList).Distinct().Where(Alive).ToList();

        private static Creature Spawn(MapChannel map, uint dbId, Vector3 spot, float spread, Vector3 facingTo)
        {
            var creature = CreatureManager.Instance.CreateCreature(dbId, null);

            if (creature == null)
                return null;

            var at = spot;

            if (spread > 0 && NavMeshManager.RandomPointAround(map, spot, spread) is { } walkable &&
                Vector2.Distance(new Vector2(walkable.X, walkable.Z), new Vector2(spot.X, spot.Z)) <= spread + 1f)
                at = walkable;

            at = NavMeshManager.SnapToGround(map, at);

            var facing = Math.Atan2(facingTo.X - at.X, facingTo.Z - at.Z);

            CreatureManager.Instance.SetLocation(creature, at, facing, map.MapInfo.MapContextId);
            creature.LastYaw = (float)facing;
            CellManager.Instance.AddToWorld(map, creature);
            return creature;
        }

        private static bool Alive(Creature creature) =>
            creature != null && creature.State != CharacterState.Dead && creature.State != CharacterState.Dying &&
            EntityManager.Instance.Creatures.TryGetValue(creature.EntityId, out var registered) && ReferenceEquals(registered, creature);

        /// <summary>Forgets every copy; for tests.</summary>
        public static void Reset() => States.Clear();

        #endregion
    }
}
