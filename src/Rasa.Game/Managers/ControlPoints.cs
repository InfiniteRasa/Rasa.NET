using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.Communicator.Server;
    using Packets.MapChannel.Server;
    using Repositories.UnitOfWork;
    using Structures;
    using Structures.Char;
    using Structures.World;

    /// <summary>
    /// The control points of the open world: the Eloh nodes the AFS and the Bane fight over.
    ///
    /// From the client:
    ///  - the object is a usable with the CONTROLPOINT augmentation, used with use-object
    ///    argument 7 as an interruptible use; its states are USE_CPOINT_STATE_FACTION_A_OWNED
    ///    (176, the human effect), FACTION_B_OWNED (179, the Bane's) and UNCLAIMED (181);
    ///  - its map and radar marker takes (ownerTypeId, ownerId) - FACTION_OWNED with True for the
    ///    AFS and False for the Bane - and a hospital's or waypoint's takes isFriendly;
    ///  - "%(faction)s claiming Control Point %(cpName)s" and "%(faction)s took Control Point
    ///    %(cpName)s" (PM_CONTROLPOINT_CLAIMING, PM_CONTROLPOINT_OWNED);
    ///  - the help text: "Losing a Control Point to the Bane means that the hospital, waypoints,
    ///    vendors, access to NPCs and base defenses are lost", and prestige is earned for
    ///    "defeating a control point minion" and "a control point boss"
    ///    (ReceivedCreatureKillPrestige: "You received N prestige points for killing X").
    ///
    /// Which side starts with a point, what stands there for each, and the fight itself were the
    /// server's, and are ours:
    ///  - A point and what belongs to it are world data (control_point, control_point_link):
    ///    the spawn pools of the Bane's garrison and of the AFS's, its hospital and waypoint, and
    ///    the creatures that are bosses.
    ///  - Whoever holds the point has their garrison there: their pools run and the other
    ///    side's do not (SpawnPool.Suspended), what was still alive of the other side's is taken
    ///    off - the dead are left to whoever killed them - and the new garrison is set down at
    ///    once.
    ///  - The garrison must be dead before the point can be used. While any of the Bane's
    ///    stands, the object is out of service (SetUsable); with all of it dead at once a player
    ///    may use it, <see cref="CaptureMs"/> of an interruptible use, and the point is the AFS's
    ///    if the garrison is still down when the use ends. A garrison of no pools has nobody to
    ///    kill.
    ///  - A claim under way is shown by the use itself (DynamicObjectManager): the object's
    ///    contested effect in place of its owner's while it is locked to the claimant, and the
    ///    claimant's windup to everyone in range. The client has no claiming state for it.
    ///  - A Bane garrison comes back together: a pool of it that has been killed waits
    ///    (<see cref="HoldsBack"/>) until the whole garrison has been down for the shortest of
    ///    its pools' respawn times, and then all of it returns at once. So a garrison can be worn
    ///    down group by group by however few there are to do it, and the point is open for that
    ///    long once the last of it falls.
    ///  - The Bane take a point back by killing its garrison: an AFS point every one of whose
    ///    garrison is dead at once is the Bane's. Nothing sends the Bane against a point yet, so
    ///    until something does only a game master's kills - or ".cp" - make it so.
    ///  - While the Bane hold a point its hospital is not offered to the dead, is not gained by
    ///    walking up to it, and its waypoint neither lists nor answers (WaypointInfo.Contested).
    ///    Nothing is taken from a player who had them: they are back when the point is.
    ///  - Killing one of a Bane garrison is worth <see cref="MinionPrestige"/> prestige, a boss
    ///    <see cref="BossPrestige"/>, to whoever has the kill and to every member of their squad
    ///    near enough to share in its loot (PartyManager.SharersOf) - the whole amount each,
    ///    whatever the squad's loot method.
    ///  - Who holds each point is kept (<see cref="IStore"/>, control_point_state) and read back
    ///    when the server starts.
    ///
    /// Control points are the open world's alone: they stand on a map's own channel, and a
    /// copy of the map - private or shared - has the objects out of service. The points of a map
    /// that has a match (Battlegrounds) are in the same tables and are its teams' to fight over:
    /// they are listed and moved from here (".cp"), and nothing else here touches them.
    /// </summary>
    public class ControlPoints
    {
        private static ControlPoints _instance;
        private static readonly object InstanceLock = new object();

        public static ControlPoints Instance
        {
            get
            {
                if (_instance == null)
                    lock (InstanceLock)
                        _instance ??= new ControlPoints();

                return _instance;
            }
        }

        public const byte Bane = ControlPointEntry.OwnerBane;
        public const byte Afs = ControlPointEntry.OwnerAfs;

        /// <summary>How long a capture's use takes.</summary>
        public const uint CaptureMs = 10000;

        /// <summary>Prestige for killing one of a control point's Bane garrison.</summary>
        public const int MinionPrestige = 30;

        /// <summary>Prestige for killing a boss of a control point's Bane garrison.</summary>
        public const int BossPrestige = 100;

        /// <summary>The creature flags that make a boss of any creature, control point or not.</summary>
        private static readonly int[] BossFlags =
        {
            (int)CreatureFlag.Powerlevel6Boss, (int)CreatureFlag.Powerlevel7Superboss
        };

        public sealed class Point
        {
            public uint Id { get; set; }
            public uint MapContextId { get; set; }
            public string Name { get; set; }
            public EntityClasses ClassId { get; set; }
            public Vector3 Position { get; set; }
            public double Rotation { get; set; }
            public ulong MarkerEntityId { get; set; }
            public byte DefaultOwner { get; set; }
            public byte Owner { get; set; }

            public HashSet<uint> BanePools { get; } = new HashSet<uint>();
            public HashSet<uint> AfsPools { get; } = new HashSet<uint>();
            public HashSet<uint> Hospitals { get; } = new HashSet<uint>();
            public HashSet<uint> Waypoints { get; } = new HashSet<uint>();
            public HashSet<uint> Bosses { get; } = new HashSet<uint>();

            /// <summary>The usable on its map's own channel, once the map has it.</summary>
            public DynamicObject Object { get; set; }

            /// <summary>When the worker first saw the Bane garrison all down (<see cref="Now"/>), or null while any of it stands.</summary>
            public long? DownSince { get; set; }

            /// <summary>The Bane garrison has been let back and has yet to arrive: its pools are not held.</summary>
            public bool Returning { get; set; }

            /// <summary>
            /// On a map that has a match (Battlegrounds): its teams fight over it there, on every
            /// channel of the map, and nothing here touches it but its row.
            /// </summary>
            public bool IsBattleground { get; set; }

            public bool HeldByAfs => Owner == Afs;
            public HashSet<uint> PoolsOf(byte owner) => owner == Afs ? AfsPools : BanePools;
        }

        /// <summary>How a point's garrison stands.</summary>
        public enum Garrison
        {
            /// <summary>The side has no pools there: nobody to kill.</summary>
            None,

            /// <summary>Somebody of it is alive, or on the way.</summary>
            Standing,

            /// <summary>All of it is dead at once.</summary>
            Down
        }

        /// <summary>Where the owners are kept through a restart.</summary>
        public interface IStore
        {
            List<ControlPointStateEntry> Load();
            void Save(uint controlPointId, byte owner, long changedAt);
        }

        private readonly Dictionary<uint, Point> _points = new Dictionary<uint, Point>();
        private readonly Dictionary<uint, Point> _byTeleporter = new Dictionary<uint, Point>();
        private readonly Dictionary<uint, Point> _byPool = new Dictionary<uint, Point>();
        private readonly Dictionary<ulong, Point> _byObject = new Dictionary<ulong, Point>();

        /// <summary>Where the owners are kept; null keeps nothing. Set by <see cref="Load"/>.</summary>
        public IStore Store { get; private set; }

        /// <summary>The wall clock a change of hands is dated by: Unix milliseconds, UTC. Replaceable for tests.</summary>
        public Func<long> UtcNow { get; set; } = () => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        /// <summary>The clock a garrison's time down is measured on, in milliseconds. Replaceable for tests.</summary>
        public Func<long> Now { get; set; } = () => Environment.TickCount64;

        /// <summary>Who shares in a kill with the player who has it: them and their squad near the corpse. Replaceable for tests.</summary>
        public Func<Client, Vector3, List<Client>> SharersOf { get; set; } = (killer, corpse) => PartyManager.Instance.SharersOf(killer, corpse);

        /// <summary>Whether a creature id is one the server has loaded: a pool of none never spawns. Replaceable for tests.</summary>
        public Func<uint, bool> KnownCreature { get; set; } = id => CreatureManager.Instance.LoadedCreatures.ContainsKey(id);

        public IReadOnlyCollection<Point> Points => _points.Values;

        #region Loading

        /// <summary>
        /// At startup, after the maps, their spawn pools and the teleporters: the control points
        /// from the world database, each with whoever held it when the server stopped, set down
        /// on their maps.
        /// </summary>
        public void Init(IGameUnitOfWorkFactory factory)
        {
            List<ControlPointEntry> points;
            List<ControlPointLinkEntry> links;

            using (var unitOfWork = factory.CreateWorld())
            {
                points = unitOfWork.ControlPoints.GetControlPoints();
                links = unitOfWork.ControlPoints.GetLinks();
            }

            Load(points, links, new ServerStore(factory));

            foreach (var mapChannel in MapChannelManager.Instance.MapChannelArray.Values)
                Place(mapChannel);

            Logger.WriteLog(LogType.Initialize, $"Loaded {_points.Count} control points, {_points.Values.Count(p => p.HeldByAfs)} held by the AFS.");
        }

        /// <summary>The points and their links, with the owners <paramref name="store"/> has kept; a point it has none for is with its default owner.</summary>
        public void Load(IEnumerable<ControlPointEntry> points, IEnumerable<ControlPointLinkEntry> links, IStore store)
        {
            _points.Clear();
            _byTeleporter.Clear();
            _byPool.Clear();
            _byObject.Clear();
            Store = store;

            foreach (var entry in points)
                _points[entry.Id] = new Point
                {
                    Id = entry.Id,
                    MapContextId = entry.MapContextId,
                    Name = entry.Name ?? "",
                    ClassId = (EntityClasses)entry.ClassId,
                    Position = entry.Position,
                    Rotation = entry.Rotation,
                    MarkerEntityId = entry.MarkerEntityId,
                    DefaultOwner = entry.DefaultOwner == Afs ? Afs : Bane,
                    Owner = entry.DefaultOwner == Afs ? Afs : Bane,
                    IsBattleground = Battlegrounds.Instance.IsBattleground(entry.MapContextId)
                };

            foreach (var link in links)
            {
                if (!_points.TryGetValue(link.ControlPointId, out var point))
                {
                    Logger.WriteLog(LogType.Error, $"control_point_link names control point {link.ControlPointId}, which there is none of.");
                    continue;
                }

                // A battleground's point has its links and is found by none of them: its pools,
                // hospital and waypoint are its match's to run (Battlegrounds).
                if (point.IsBattleground)
                {
                    switch (link.Kind)
                    {
                        case ControlPointLinkEntry.KindBanePool: point.BanePools.Add(link.ObjectId); break;
                        case ControlPointLinkEntry.KindAfsPool: point.AfsPools.Add(link.ObjectId); break;
                        case ControlPointLinkEntry.KindHospital: point.Hospitals.Add(link.ObjectId); break;
                        case ControlPointLinkEntry.KindWaypoint: point.Waypoints.Add(link.ObjectId); break;
                        case ControlPointLinkEntry.KindBoss: point.Bosses.Add(link.ObjectId); break;
                    }

                    continue;
                }

                switch (link.Kind)
                {
                    case ControlPointLinkEntry.KindBanePool:
                        point.BanePools.Add(link.ObjectId);
                        _byPool[link.ObjectId] = point;
                        break;

                    case ControlPointLinkEntry.KindAfsPool:
                        point.AfsPools.Add(link.ObjectId);
                        _byPool[link.ObjectId] = point;
                        break;

                    case ControlPointLinkEntry.KindHospital:
                        point.Hospitals.Add(link.ObjectId);
                        _byTeleporter[link.ObjectId] = point;
                        break;

                    case ControlPointLinkEntry.KindWaypoint:
                        point.Waypoints.Add(link.ObjectId);
                        _byTeleporter[link.ObjectId] = point;
                        break;

                    case ControlPointLinkEntry.KindBoss:
                        point.Bosses.Add(link.ObjectId);
                        break;

                    default:
                        Logger.WriteLog(LogType.Error, $"control_point_link of control point {link.ControlPointId} has kind {link.Kind}, which nothing reads.");
                        break;
                }
            }

            if (store == null)
                return;

            try
            {
                foreach (var state in store.Load())
                    if (_points.TryGetValue(state.ControlPointId, out var point) && !point.IsBattleground)
                        point.Owner = state.Owner == Afs ? Afs : Bane;
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, $"Control points: the owners could not be read back, so each is with its default: {e.Message}");
            }
        }

        /// <summary>
        /// Sets a map's control points down on it, each as its owner has it: the object in its
        /// state, the garrison pools of the holder running and the other side's not, and the
        /// hospital and waypoint open or shut. On the map's own channel only.
        /// </summary>
        public void Place(MapChannel mapChannel)
        {
            if (mapChannel?.MapInfo == null || mapChannel.IsCopy)
                return;

            foreach (var point in _points.Values.Where(p => p.MapContextId == mapChannel.MapInfo.MapContextId && !p.IsBattleground))
            {
                if (point.Object == null)
                {
                    // Out of service until the worker has looked at its garrison.
                    point.Object = new DynamicObject
                    {
                        Position = NavMeshManager.SnapToGround(mapChannel, point.Position),
                        Rotation = point.Rotation,
                        MapContextId = point.MapContextId,
                        RuntimeMapChannel = mapChannel,
                        EntityClassId = point.ClassId,
                        DynamicObjectType = DynamicObjectType.ControlPoint,
                        Comment = $"Control Point: {point.Name}",
                        WindupTime = CaptureMs,
                        IsEnabled = false
                    };

                    _byObject[point.Object.EntityId] = point;
                    mapChannel.ControlPoints[point.Id] = point.Object;
                }

                ShowOwner(point);
                ApplyPools(mapChannel, point, takeOff: false);
                ApplyTeleporters(point);
            }
        }

        #endregion

        #region Lookups

        public Point ById(uint id) => _points.TryGetValue(id, out var point) ? point : null;

        public Point PointOf(DynamicObject obj) => obj != null && _byObject.TryGetValue(obj.EntityId, out var point) ? point : null;

        public IEnumerable<Point> OnMap(uint mapContextId) => _points.Values.Where(p => p.MapContextId == mapContextId);

        /// <summary>
        /// Whether a hospital or waypoint (a teleporter row) is the AFS's to use: one that belongs
        /// to no control point always is, one that does while the AFS hold the point.
        /// </summary>
        public bool IsOpen(uint teleporterId) => !_byTeleporter.TryGetValue(teleporterId, out var point) || point.HeldByAfs;

        /// <summary>How the garrison of <paramref name="owner"/> at a point stands on its map.</summary>
        public Garrison GarrisonOf(MapChannel mapChannel, Point point, byte owner)
        {
            var pools = PoolsOn(mapChannel, point.PoolsOf(owner)).Where(CanSpawn).ToList();

            if (pools.Count == 0)
                return Garrison.None;

            return pools.Any(Stands) ? Garrison.Standing : Garrison.Down;
        }

        /// <summary>
        /// Whether a pool that has been killed waits for the rest of its garrison
        /// (SpawnPoolManager.SpawnPoolWorker): a Bane garrison's, from its first spawn until the
        /// worker lets the whole garrison back. Its own respawn time does not run.
        /// </summary>
        public bool HoldsBack(SpawnPool pool)
        {
            return pool != null && pool.HasSpawned && _byPool.TryGetValue(pool.DbId, out var point)
                   && !point.HeldByAfs && !point.Returning && point.BanePools.Contains(pool.DbId);
        }

        /// <summary>How long a Bane garrison stays down once the last of it has fallen: the shortest of its pools' respawn times, in milliseconds.</summary>
        public long ReturnMs(MapChannel mapChannel, Point point)
        {
            var pools = PoolsOn(mapChannel, point.BanePools).Where(CanSpawn).ToList();

            return pools.Count == 0 ? 0 : pools.Min(p => p.RespawnTime);
        }

        /// <summary>Whether a player may start to capture the point now: the Bane hold it, and none of their garrison stands.</summary>
        public bool MayCapture(MapChannel mapChannel, Point point)
        {
            return point != null && !point.HeldByAfs && GarrisonOf(mapChannel, point, Bane) != Garrison.Standing;
        }

        #endregion

        #region The fight

        /// <summary>
        /// Each of a map's points, as its garrison stands now: a Bane point is in service while
        /// none of its garrison stands, and an AFS point whose garrison is all dead is the Bane's.
        /// From the map channel worker.
        /// </summary>
        public void Worker(MapChannel mapChannel)
        {
            if (mapChannel?.MapInfo == null || mapChannel.IsCopy || _points.Count == 0)
                return;

            foreach (var point in _points.Values.Where(p => p.MapContextId == mapChannel.MapInfo.MapContextId && !p.IsBattleground).ToList())
            {
                if (point.Object == null)
                    continue;

                // Whatever of the other side's has arrived since the point changed hands: a
                // dropship that was already on its way.
                TakeOffLiving(mapChannel, PoolsOn(mapChannel, point.PoolsOf(point.HeldByAfs ? Bane : Afs)));

                if (point.HeldByAfs)
                {
                    if (GarrisonOf(mapChannel, point, Afs) == Garrison.Down)
                        SetOwner(point, Bane, null);

                    continue;
                }

                var garrison = GarrisonOf(mapChannel, point, Bane);

                if (garrison == Garrison.Down)
                {
                    var now = Now();

                    point.DownSince ??= now;

                    // Down for as long as it stays down: all of it comes back, on the spawn
                    // worker's next pass.
                    if (now - point.DownSince.Value >= ReturnMs(mapChannel, point))
                    {
                        point.DownSince = null;
                        point.Returning = true;

                        foreach (var pool in PoolsOn(mapChannel, point.BanePools))
                            pool.UpdateTimer = pool.RespawnTime;
                    }
                }
                else
                {
                    point.DownSince = null;

                    // Back: every pool of it has somebody alive or on the way.
                    if (point.Returning && PoolsOn(mapChannel, point.BanePools).Where(CanSpawn)
                            .All(p => p.AliveCreatures + p.QueuedCreatures + p.DropshipQueue > 0))
                        point.Returning = false;
                }

                DynamicObjectManager.Instance.SetEnabled(point.Object, garrison != Garrison.Standing);
            }
        }

        /// <summary>A player has begun the use that captures a point: everyone on the map is told.</summary>
        public void Claiming(MapChannel mapChannel, Point point)
        {
            Announce(mapChannel, PlayerMessage.PmControlpointClaiming, point, Afs);
        }

        /// <summary>
        /// A player's use of a control point has run its time (DynamicObjectManager): the point
        /// is the AFS's, unless it is not the Bane's any more or its garrison is back on its
        /// feet. Returns whether it changed hands.
        /// </summary>
        public bool Captured(MapChannel mapChannel, Client client, Point point)
        {
            if (!MayCapture(mapChannel, point))
            {
                Logger.WriteLog(LogType.Debug, $"Control point {point?.Id} ({point?.Name}): {client?.Player?.FamilyName}'s capture came to nothing; the garrison stands or the point is not the Bane's.");
                return false;
            }

            SetOwner(point, Afs, client);
            return true;
        }

        /// <summary>
        /// Gives a point to a side: kept, the garrisons changed over, the hospital and waypoint
        /// opened or shut, and everyone on the map shown and told. Nothing if that side has it.
        /// </summary>
        public bool SetOwner(Point point, byte owner, Client by)
        {
            owner = owner == Afs ? Afs : Bane;

            if (point == null || point.IsBattleground || point.Owner == owner)
                return false;

            point.Owner = owner;
            point.DownSince = null;
            point.Returning = false;

            Logger.WriteLog(LogType.Debug, $"Control point {point.Id} ({point.Name}) is the {FactionName(owner)}'s{(by?.Player != null ? $", taken by {by.Player.FamilyName}" : "")}.");

            var store = Store;

            if (store != null)
                try
                {
                    store.Save(point.Id, owner, UtcNow());
                }
                catch (Exception e)
                {
                    Logger.WriteLog(LogType.Error, $"Control point {point.Id} ({point.Name}): its owner was not saved: {e.Message}");
                }

            ApplyTeleporters(point);

            var mapChannel = point.Object?.RuntimeMapChannel;

            if (mapChannel == null)
                return true;

            ApplyPools(mapChannel, point, takeOff: true);
            ShowOwner(point);

            // Out of service until the new garrison has been dealt with - or for good, if it is the AFS's.
            DynamicObjectManager.Instance.SetEnabled(point.Object, false);

            // One change of state, with the time a capture takes (ForceState sets both): the
            // owner's effect is put on once. UsableInfo after it would start the state over and
            // put the effect on a second time; it says nothing SetUsable and this have not.
            if (point.Object.IsInWorld)
                CellManager.Instance.CellCallMethod(mapChannel, point.Object,
                    new ForceStatePacket(point.Object.StateId, (int)point.Object.WindupTime));

            MapMarkerManager.Instance.ControlPointChanged(mapChannel, point);
            Announce(mapChannel, PlayerMessage.PmControlpointOwned, point, owner);

            return true;
        }

        /// <summary>
        /// A creature a player has the kill of: one of a Bane garrison is worth prestige, a boss
        /// more, to them and to each of their squad who shares in the kill
        /// (CreatureManager.HandleCreatureKill). Returns what the player with the kill was given.
        /// </summary>
        public int CreatureKilled(Creature creature, Client client)
        {
            var pool = creature?.SpawnPool;

            if (pool == null || client?.Player == null || !_byPool.TryGetValue(pool.DbId, out var point) || !point.BanePools.Contains(pool.DbId))
                return 0;

            var amount = IsBoss(point, creature) ? BossPrestige : MinionPrestige;
            var given = 0;

            foreach (var sharer in SharersOf(client, creature.Position) ?? new List<Client> { client })
            {
                if (sharer?.Player == null || !Give(point, creature, sharer, amount, sharer == client))
                    continue;

                if (sharer == client)
                    given = amount;
            }

            return given;
        }

        /// <summary>
        /// One player's prestige for a garrison kill, and their being told: by the creature's
        /// name if their client has the creature - the client takes the name from it and says
        /// nothing without it - and by the amount alone if they are too far off to.
        /// </summary>
        private static bool Give(Point point, Creature creature, Client client, int amount, bool hasTheKill)
        {
            try
            {
                if (!PvpPrestige.Change(client, amount))
                    return false;

                if (hasTheKill || Sees(client, creature))
                    client.CallMethod(SysEntity.ClientPrestigeSystemId, new ReceivedCreatureKillPrestigePacket(creature.EntityId, amount));
                else
                    client.CallMethod(SysEntity.CommunicatorId, new DisplayClientMessagePacket(PlayerMessage.PmPrestigePointsReceived,
                        new Dictionary<string, string> { { "amount", amount.ToString() } }, MsgFilterId.PrestigeGainLose));

                return true;
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, $"Control point {point.Id} ({point.Name}): {amount} prestige for {client.Player.FamilyName} failed: {e.Message}");
                return false;
            }
        }

        /// <summary>Whether a client has been given a creature: it stands in the cells the creature is shown to.</summary>
        private static bool Sees(Client client, Creature creature)
        {
            var mapChannel = creature.RuntimeMapChannel;

            return mapChannel != null && creature.Cells != null
                   && CellManager.CellsIn(mapChannel, creature.Cells).Any(cell => cell.ClientList.Contains(client));
        }

        /// <summary>Whether a creature of a point's garrison is one of its bosses: named so in its links, or a boss by its class.</summary>
        public static bool IsBoss(Point point, Creature creature)
        {
            if (point.Bosses.Contains(creature.DbId))
                return true;

            return CreatureManager.CreatureFlagsOf(creature).Any(flag => BossFlags.Contains(flag));
        }

        /// <summary>
        /// Stands a point's object somewhere else, kept in the world database: where the client's
        /// marker puts a point is not always where its object stood. For a game master (.cp).
        /// </summary>
        public bool Move(Point point, Vector3 position, double rotation, IGameUnitOfWorkFactory factory)
        {
            if (point == null)
                return false;

            try
            {
                using var unitOfWork = factory.CreateWorld();

                if (!unitOfWork.ControlPoints.UpdatePosition(point.Id, position.X, position.Y, position.Z, rotation))
                    return false;
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, $"Control point {point.Id} ({point.Name}) was not moved: {e.Message}");
                return false;
            }

            point.Position = position;
            point.Rotation = rotation;

            if (point.IsBattleground)
            {
                Battlegrounds.Instance.PointMoved(point);
                return true;
            }

            var obj = point.Object;
            var mapChannel = obj?.RuntimeMapChannel;

            if (obj == null || mapChannel == null)
                return true;

            // Off and on again: an object has no way to be told it has moved.
            if (obj.IsInWorld)
                CellManager.Instance.RemoveFromWorld(mapChannel, obj);

            obj.Position = position;
            obj.Rotation = rotation;

            if (obj.IsInWorld)
                CellManager.Instance.AddToWorld(mapChannel, obj);

            return true;
        }

        #endregion

        #region A point's own

        /// <summary>The object's state and side, as its owner has it.</summary>
        private static void ShowOwner(Point point)
        {
            if (point.Object == null)
                return;

            point.Object.StateId = point.HeldByAfs ? UseObjectState.CpointStateFactionAOwned : UseObjectState.CpointStateFactionBOwned;
            point.Object.TargetCategory = point.HeldByAfs ? TargetCategory.Friendly : TargetCategory.Hostile;
        }

        /// <summary>
        /// The holder's garrison pools run, from a fresh start, and the other side's do not; with
        /// <paramref name="takeOff"/> whatever of the other side's is alive on the map goes.
        /// </summary>
        private void ApplyPools(MapChannel mapChannel, Point point, bool takeOff)
        {
            var idle = PoolsOn(mapChannel, point.PoolsOf(point.HeldByAfs ? Bane : Afs));

            foreach (var pool in idle)
            {
                pool.IsGarrison = true;
                pool.Suspended = true;
            }

            if (takeOff)
                TakeOffLiving(mapChannel, idle);

            foreach (var pool in PoolsOn(mapChannel, point.PoolsOf(point.Owner)))
            {
                pool.IsGarrison = true;
                pool.Suspended = false;

                // Set down at once, on the spawn worker's next pass, and where it stands rather
                // than by its arrival point.
                pool.HasSpawned = false;
                pool.UpdateTimer = pool.RespawnTime;
            }
        }

        /// <summary>
        /// Takes the living creatures of these pools off the map, and whatever is theirs in turn.
        /// The dead stay: a corpse and what it holds is its killer's, and goes in its own time.
        /// </summary>
        internal static void TakeOffLiving(MapChannel mapChannel, List<SpawnPool> pools)
        {
            if (pools.Count == 0 || pools.All(p => p.AliveCreatures <= 0) || mapChannel?.MapCellInfo?.Cells == null)
                return;

            var all = mapChannel.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList).Where(c => c != null).Distinct().ToList();
            var going = all.Where(c => c.State != CharacterState.Dead && c.SpawnPool != null && pools.Contains(c.SpawnPool)).ToList();
            var ids = new HashSet<ulong>(going.Select(c => c.EntityId));
            var added = true;

            while (added)
            {
                added = false;

                foreach (var creature in all)
                {
                    if (creature.MasterEntityId == 0 || creature.State == CharacterState.Dead
                        || ids.Contains(creature.EntityId) || !ids.Contains(creature.MasterEntityId))
                        continue;

                    going.Add(creature);
                    ids.Add(creature.EntityId);
                    added = true;
                }
            }

            foreach (var creature in going)
            {
                // Counted out of its pool as one that was alive: MapReset.Remove takes it off as
                // a corpse, which the pool's dead count answers for.
                if (creature.SpawnPool != null)
                {
                    creature.SpawnPool.AliveCreatures = Math.Max(0, creature.SpawnPool.AliveCreatures - 1);
                    creature.SpawnPool.DeadCreatures++;
                }

                MapReset.Remove(mapChannel, creature);
            }

            // Nothing of a pool that spawns nothing is alive, whatever its count had drifted to.
            foreach (var pool in pools)
                pool.AliveCreatures = 0;
        }

        /// <summary>The point's hospital and waypoint are the AFS's to use, or not.</summary>
        private static void ApplyTeleporters(Point point)
        {
            foreach (var id in point.Hospitals.Concat(point.Waypoints))
                if (DynamicObjectManager.Instance.Teleporters.TryGetValue(id, out var teleporter) && teleporter.ObjectData is WaypointInfo info)
                    info.Contested = !point.HeldByAfs;
        }

        private static List<SpawnPool> PoolsOn(MapChannel mapChannel, HashSet<uint> ids)
        {
            if (mapChannel?.SpawnPools == null || ids.Count == 0)
                return new List<SpawnPool>();

            return mapChannel.SpawnPools.Where(p => p != null && ids.Contains(p.DbId)).ToList();
        }

        /// <summary>Whether the spawn worker would ever set anyone of this pool down (SpawnPoolManager.SpawnPoolWorker, CreateListOfCreatures).</summary>
        private bool CanSpawn(SpawnPool pool)
        {
            return pool.SpawnPolicy != MissionSpawnGroupPolicy.ScenarioControlled
                   && (pool.Mode == SpawnPoolManager.ModeAutomatic || pool.Mode == SpawnPoolManager.ModeControlPoint)
                   && pool.AnimType >= 0 && pool.AnimType <= 2
                   && pool.SpawnSlot != null
                   && pool.SpawnSlot.Any(s => s != null && s.CreatureId != 0 && s.CountMin >= 0 && s.CountMax > 0
                                              && s.CountMax >= s.CountMin && KnownCreature(s.CreatureId));
        }

        /// <summary>Somebody of the pool is alive or on the way; one that has yet to set anyone down is on its way.</summary>
        private static bool Stands(SpawnPool pool) =>
            !pool.HasSpawned || pool.AliveCreatures + pool.QueuedCreatures + pool.DropshipQueue > 0;

        private static void Announce(MapChannel mapChannel, PlayerMessage message, Point point, byte faction)
        {
            if (mapChannel?.ClientList == null)
                return;

            var args = new Dictionary<string, string> { { "faction", FactionName(faction) }, { "cpName", point.Name } };

            foreach (var client in mapChannel.ClientList.ToArray())
                if (client?.Player != null && client.State == ClientState.Ingame)
                    client.CallMethod(SysEntity.CommunicatorId, new DisplayClientMessagePacket(message, args, MsgFilterId.GeneralSystemMessages));
        }

        public static string FactionName(byte owner) => owner == Afs ? "AFS" : "Bane";

        #endregion

        /// <summary>The live server's store: the character database's control_point_state table.</summary>
        public sealed class ServerStore : IStore
        {
            private readonly IGameUnitOfWorkFactory _factory;

            public ServerStore(IGameUnitOfWorkFactory factory)
            {
                _factory = factory;
            }

            public List<ControlPointStateEntry> Load()
            {
                using var unitOfWork = _factory.CreateChar();
                return unitOfWork.ControlPointStates.GetStates();
            }

            public void Save(uint controlPointId, byte owner, long changedAt)
            {
                using var unitOfWork = _factory.CreateChar();
                unitOfWork.ControlPointStates.SaveState(controlPointId, owner, changedAt);
            }
        }
    }
}
