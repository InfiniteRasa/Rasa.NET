using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Rasa.Managers
{
    using Config;
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
    /// Clan-owned control points. The client has the parts of them and not the whole: a usable
    /// class with the CLANCONTROLPOINT augmentation (TEST_ClanControlPoint_PvE, 29329, the Eloh
    /// point's own mesh) whose effect is by its owner - "your clan owns", "other clan owns", the
    /// AFS's, the Bane's - and whose in-use effect is by the two of them; the clan lockbox, which
    /// the help text has "in cities and in clan-owned control points", where "only the clan that
    /// owns a Control Point may access the lockbox contained within it"; and clan prestige
    /// spent "towards rewards such as ownership of a Control Point". The challenge board that
    /// would have had clans bid for one cannot open, and a clan-owned point's map marker text is
    /// a placeholder. How a clan comes by a point, loses it, and what it has of it are ours:
    ///  - A point taken from the Bane by a player in a clan is that clan's (<see cref="Point.ClanId"/>),
    ///    and by a player in none the AFS's, as it always was. It is the AFS's point either way:
    ///    their garrison, hospital and waypoint, and their colour on the map.
    ///  - While a clan holds it the point's object is the clan class, in its clan controlled
    ///    state, named "Central Dispatch Unit - Control Point" (the class's own name is a
    ///    placeholder); with the AFS or the Bane it is the class of its row, as before. The one
    ///    is taken off the map and the other set down as the point changes hands.
    ///  - A clan loses a point when the Bane take it back; when a member of a clan at feud with
    ///    it uses it - <see cref="ControlPointConfig.ClanCaptureSeconds"/> of an interruptible
    ///    use, in service to those players alone, and the point is their clan's; when the clan
    ///    disbands; and at the weekly reset, when every clan's point goes back to the AFS.
    ///  - Each point a clan holds pays <see cref="ControlPointConfig.ClanPrestige"/> prestige
    ///    into its lockbox every <see cref="ControlPointConfig.ClanPrestigeMinutes"/>, by the
    ///    clock on the wall, and has a line of the lockbox's history for it.
    ///  - A point may have a clan lockbox (control_point_link, a footlocker row; ".cp &lt;id&gt;
    ///    lockbox"): on the map only while a clan holds the point, and that clan's alone to open.
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

        /// <summary>shared/gameconstants.py VIRTUAL_CLAN_AFS: the AFS, where the client's clan control point takes a clan.</summary>
        public const int VirtualClanAfs = -1;

        /// <summary>shared/gameconstants.py VIRTUAL_CLAN_BANE: the Bane, likewise.</summary>
        public const int VirtualClanBane = -2;

        /// <summary>The class a point's object is while a clan holds it: the client's one clan control point.</summary>
        public const EntityClasses ClanPointClass = DynamicObjectManager.PveClanControlPointClass;

        /// <summary>
        /// usablenameoverridelanguage 338, "Central Dispatch Unit - Control Point": what a clan's
        /// point is called. Its class's own name is "TEST_ClanControlPoint has no display text.".
        /// </summary>
        public const uint ClanPointNameOverride = 338;

        /// <summary>The first name of the line a point's pay has in a clan lockbox's history; the point's name is the second.</summary>
        public const string PayerName = "Control Point";

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

            /// <summary>The clan that holds the point for the AFS, or 0: only ever with the AFS as owner.</summary>
            public uint ClanId { get; set; }

            /// <summary>When the point last changed hands, from one side or one clan to another (<see cref="UtcNow"/>); 0 if it never has.</summary>
            public long ChangedAt { get; set; }

            /// <summary>Up to when the clan has been paid for holding the point (<see cref="UtcNow"/>); 0 with no clan.</summary>
            public long PaidAt { get; set; }

            /// <summary>When "claiming" was last announced for the point (<see cref="Now"/>); 0 if it never was. See <see cref="ClaimAnnounceGapMs"/>.</summary>
            public long ClaimAnnouncedAt { get; set; }

            /// <summary>The footlocker row of the point's clan lockbox, or 0 while it has none.</summary>
            public uint LockboxId { get; set; }

            public Vector3 LockboxPosition { get; set; }
            public double LockboxRotation { get; set; }

            /// <summary>The clan lockbox on the map: there while a clan holds the point, and null otherwise.</summary>
            public DynamicObject Lockbox { get; set; }

            /// <summary>The map's own channel, once the point has been set down on it.</summary>
            public MapChannel Map { get; set; }

            /// <summary>Whose clients have the point's object in service: the players who may take it from the clan that holds it.</summary>
            internal HashSet<Client> ToldUsable { get; } = new HashSet<Client>();

            /// <summary>Whose clients have the lockbox in service: the members of the clan that holds the point.</summary>
            internal HashSet<Client> ToldOpen { get; } = new HashSet<Client>();

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

            /// <summary>A clan holds it, for the AFS.</summary>
            public bool HeldByClan => Owner == Afs && ClanId != 0;

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

            /// <summary>The point's row as it now is: its owner, its clan and how far that clan has been paid.</summary>
            void Save(ControlPointStateEntry state);
        }

        private readonly Dictionary<uint, Point> _points = new Dictionary<uint, Point>();
        private readonly Dictionary<uint, Point> _byTeleporter = new Dictionary<uint, Point>();
        private readonly Dictionary<uint, Point> _byPool = new Dictionary<uint, Point>();
        private readonly Dictionary<ulong, Point> _byObject = new Dictionary<ulong, Point>();
        private readonly Dictionary<ulong, Point> _byLockbox = new Dictionary<ulong, Point>();

        private string _resetSetting;
        private bool _missedResetLookedFor;

        /// <summary>appsettings.json's ControlPoints. Replaced whole when the file changes.</summary>
        public ControlPointConfig Config { get; set; } = new ControlPointConfig();

        /// <summary>A clan's name, or null if there is no such clan. Replaceable for tests.</summary>
        public Func<uint, string> ClanName { get; set; } = id => ClanManager.Instance.ClanNameOf(id);

        /// <summary>Whether two clans are at feud. Replaceable for tests.</summary>
        public Func<uint, uint, bool> AtFeud { get; set; } = (a, b) => ClanFeuds.Instance.AreFeuding(a, b);

        /// <summary>Pays prestige into a clan's lockbox for a point it holds: the clan, the amount, the point's name. Replaceable for tests.</summary>
        public Func<uint, int, string, bool> PayClan { get; set; } =
            (clanId, amount, pointName) => InventoryManager.Instance.PayClanPrestige(clanId, (uint)amount, PayerName, pointName);

        /// <summary>The server's own clock, which the weekly reset is set by. Replaceable for tests.</summary>
        public Func<DateTime> WallClock { get; set; } = () => DateTime.Now;

        /// <summary>Where the day and time of the weekly reset are read from: the squad instances' settings. Replaceable for tests.</summary>
        public Func<SquadInstanceConfig> ResetSchedule { get; set; } = () => SquadInstancePolicies.Current;

        /// <summary>When the clans' points next go back to the AFS, by <see cref="WallClock"/>; null with the reset off.</summary>
        public DateTime? NextClanReset { get; private set; }

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
            List<FootlockerEntry> lockboxes;

            using (var unitOfWork = factory.CreateWorld())
            {
                points = unitOfWork.ControlPoints.GetControlPoints();
                links = unitOfWork.ControlPoints.GetLinks();
                lockboxes = unitOfWork.Footlockers.GetFootlockers();
            }

            Load(points, links, new ServerStore(factory), lockboxes);

            foreach (var mapChannel in MapChannelManager.Instance.MapChannelArray.Values)
                Place(mapChannel);

            Logger.WriteLog(LogType.Initialize,
                $"Loaded {_points.Count} control points, {_points.Values.Count(p => p.HeldByAfs)} held by the AFS, {_points.Values.Count(p => p.HeldByClan)} of those by a clan.");
        }

        /// <summary>
        /// The points and their links, with the owners <paramref name="store"/> has kept; a point
        /// it has none for is with its default owner. <paramref name="lockboxes"/> are the
        /// footlocker rows, of which a point's clan lockbox is one.
        /// </summary>
        public void Load(IEnumerable<ControlPointEntry> points, IEnumerable<ControlPointLinkEntry> links, IStore store, IEnumerable<FootlockerEntry> lockboxes = null)
        {
            _points.Clear();
            _byTeleporter.Clear();
            _byPool.Clear();
            _byObject.Clear();
            _byLockbox.Clear();
            _resetSetting = null;
            _missedResetLookedFor = false;
            NextClanReset = null;
            Store = store;

            var rows = (lockboxes ?? Enumerable.Empty<FootlockerEntry>()).GroupBy(row => row.Id).ToDictionary(g => g.Key, g => g.First());

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

                    case ControlPointLinkEntry.KindClanLockbox:
                        if (!rows.TryGetValue(link.ObjectId, out var row) || row.MapContextId != point.MapContextId)
                        {
                            Logger.WriteLog(LogType.Error, $"control_point_link gives control point {link.ControlPointId} the clan lockbox of footlocker {link.ObjectId}, which there is none of on its map.");
                            break;
                        }

                        point.LockboxId = row.Id;
                        point.LockboxPosition = row.Position;
                        point.LockboxRotation = row.Rotation;
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
                    {
                        point.Owner = state.Owner == Afs ? Afs : Bane;
                        point.ChangedAt = state.ChangedAt;
                        point.ClanId = point.HeldByAfs ? state.ClanId : 0;
                        point.PaidAt = point.ClanId != 0 ? state.ClanPaidAt : 0;
                    }
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
                point.Map = mapChannel;

                // Its clan lockbox is the point's to set down, while a clan holds it: the row is
                // a footlocker's, and the map has it as one of those (InitFootlockers).
                if (point.LockboxId != 0 && mapChannel.FootLockers.Remove(point.LockboxId, out var asFootlocker))
                {
                    if (asFootlocker.IsInWorld)
                        CellManager.Instance.RemoveFromWorld(mapChannel, asFootlocker);
                    else
                        EntityManager.Instance.FreeEntity(asFootlocker.EntityId);
                }

                if (point.Object == null)
                {
                    point.Object = NewObject(mapChannel, point, null);

                    _byObject[point.Object.EntityId] = point;
                    mapChannel.ControlPoints[point.Id] = point.Object;
                }

                ShowOwner(point);
                ApplyPools(mapChannel, point, takeOff: false);
                ApplyTeleporters(point);
                ShowLockbox(point);
            }
        }

        /// <summary>
        /// The point's object as whoever holds it has it: the clan class while a clan does, in
        /// service to whoever may take it from them (each client is told for itself,
        /// <see cref="ShownUsable"/>); the class of its row otherwise, out of service until the
        /// worker has looked at its garrison. It stands where <paramref name="before"/> stood.
        /// </summary>
        private DynamicObject NewObject(MapChannel mapChannel, Point point, DynamicObject before)
        {
            var clan = point.HeldByClan;

            return new DynamicObject
            {
                Position = before?.Position ?? NavMeshManager.SnapToGround(mapChannel, point.Position),
                Rotation = before?.Rotation ?? point.Rotation,
                MapContextId = point.MapContextId,
                RuntimeMapChannel = mapChannel,
                EntityClassId = clan ? ClanPointClass : point.ClassId,
                DynamicObjectType = DynamicObjectType.ControlPoint,
                Comment = $"Control Point: {point.Name}",
                WindupTime = clan ? ClanCaptureMs : CaptureMs,
                NameOverrideId = clan ? ClanPointNameOverride : 0,
                IsEnabled = clan
            };
        }

        #endregion

        #region Lookups

        public Point ById(uint id) => _points.TryGetValue(id, out var point) ? point : null;

        public Point PointOf(DynamicObject obj) => obj != null && _byObject.TryGetValue(obj.EntityId, out var point) ? point : null;

        /// <summary>The point an object is the clan lockbox of, or null.</summary>
        public Point LockboxOf(DynamicObject obj) => obj != null && _byLockbox.TryGetValue(obj.EntityId, out var point) ? point : null;

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

        /// <summary>
        /// Whether this player may start to capture the point now: from the Bane as anyone may
        /// (<see cref="MayCapture(MapChannel, Point)"/>), and from a clan if theirs is at feud
        /// with it.
        /// </summary>
        public bool MayCapture(MapChannel mapChannel, Point point, Client client)
        {
            if (point == null)
                return false;

            return point.HeldByClan ? MayTakeFromClan(client, point) : MayCapture(mapChannel, point);
        }

        /// <summary>Whether a player may take a point from the clan that holds it: they are in a clan at feud with it.</summary>
        public bool MayTakeFromClan(Client client, Point point)
        {
            var clanId = client?.Player?.ClanId ?? 0;

            return point != null && point.HeldByClan && Config.ClanOwnership && clanId != 0 && clanId != point.ClanId && AtFeud(point.ClanId, clanId);
        }

        /// <summary>Whether a player may open a point's clan lockbox: a clan holds the point, and it is theirs.</summary>
        public bool MayOpenLockbox(Client client, Point point)
        {
            return point != null && point.HeldByClan && client?.Player != null && client.Player.ClanId == point.ClanId;
        }

        /// <summary>
        /// Whether a player may open a lockbox: one that is no control point's as they always
        /// could, a point's while their clan holds the point.
        /// </summary>
        public bool MayOpenLockbox(Client client, DynamicObject lockbox)
        {
            var point = LockboxOf(lockbox);

            return point == null || MayOpenLockbox(client, point);
        }

        /// <summary>How long the use takes that captures a point, as its object has told the clients.</summary>
        public uint CaptureMsOf(Point point) => point?.Object != null && point.Object.WindupTime != 0 ? point.Object.WindupTime : CaptureMs;

        /// <summary>How long the use takes that takes a point from a clan (<see cref="ControlPointConfig.ClanCaptureSeconds"/>).</summary>
        public uint ClanCaptureMs => (uint)Math.Clamp(Config.ClanCaptureSeconds, 1, 600) * 1000;

        /// <summary>The clan a player's capture is for: their own, or none - they have none, or clans hold no points.</summary>
        public uint ClanFor(Client client) => Config.ClanOwnership ? client?.Player?.ClanId ?? 0 : 0;

        /// <summary>Who holds a point, as the messages name them: the clan's name, "AFS" or "Bane".</summary>
        public string HolderName(Point point) => point.HeldByClan ? ClanName(point.ClanId) ?? $"clan {point.ClanId}" : FactionName(point.Owner);

        /// <summary>
        /// What the client's clan control point takes for its owner: the clan, or the AFS or the
        /// Bane as a clan of their own. Null for an object that is not a point's in the clan
        /// class: it is told nothing, and shows its state's own effect.
        /// </summary>
        public int? ClanShownBy(DynamicObject obj)
        {
            var point = PointOf(obj);

            if (point == null || obj.EntityClassId != ClanPointClass)
                return null;

            return point.HeldByClan ? (int)point.ClanId : point.HeldByAfs ? VirtualClanAfs : VirtualClanBane;
        }

        /// <summary>
        /// Whether an object of a point's is in service to this client, noted as what it has been
        /// told (<see cref="TellUsable"/>): a clan's point to those who may take it from the
        /// clan, its lockbox to the clan's members. Null for any other object, which is in
        /// service to everyone or to nobody. From DynamicObjectManager, as the object is made on
        /// the client.
        /// </summary>
        public bool? ShownUsable(Client client, DynamicObject obj)
        {
            if (obj == null || client == null)
                return null;

            if (_byObject.TryGetValue(obj.EntityId, out var point) && point.HeldByClan)
                return Note(point.ToldUsable, client, MayTakeFromClan(client, point));

            if (_byLockbox.TryGetValue(obj.EntityId, out point))
                return Note(point.ToldOpen, client, MayOpenLockbox(client, point));

            return null;
        }

        private static bool Note(HashSet<Client> told, Client client, bool usable)
        {
            if (usable)
                told.Add(client);
            else
                told.Remove(client);

            return usable;
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
                    else if (point.HeldByClan)
                        TellUsable(mapChannel, point);

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

        /// <summary>
        /// A player has begun the use that captures a point: everyone on the map is told, by the
        /// clan it would be for if it is for one.
        /// </summary>
        /// <summary>
        /// The least time between two "claiming" announcements for one point, in ms. Every use
        /// of a point that was begun announced it to the whole map, and a use can be begun as
        /// often as the player likes - start, step away to break it, start again - so one player
        /// could fill everyone's chat with it.
        /// </summary>
        public const long ClaimAnnounceGapMs = 10_000;

        /// <summary>Whether a "claiming" of the point may be announced now, and marks it announced if so.</summary>
        internal static bool MayAnnounceClaim(Point point, long now)
        {
            if (point.ClaimAnnouncedAt != 0 && now - point.ClaimAnnouncedAt < ClaimAnnounceGapMs)
                return false;

            point.ClaimAnnouncedAt = now;
            return true;
        }

        public void Claiming(MapChannel mapChannel, Point point, Client by = null)
        {
            if (!MayAnnounceClaim(point, Now()))
                return;

            var clanId = ClanFor(by);

            Announce(mapChannel, PlayerMessage.PmControlpointClaiming, point, clanId != 0 ? ClanName(clanId) ?? FactionName(Afs) : FactionName(Afs));
        }

        /// <summary>
        /// A player's use of a control point has run its time (DynamicObjectManager): the point
        /// is their clan's, or the AFS's if they are in none, unless it is not the Bane's any
        /// more or its garrison is back on its feet - or, taken from a clan, their own is not at
        /// feud with it any more. Returns whether it changed hands.
        /// </summary>
        public bool Captured(MapChannel mapChannel, Client client, Point point)
        {
            if (!MayCapture(mapChannel, point, client))
            {
                Logger.WriteLog(LogType.Debug, $"Control point {point?.Id} ({point?.Name}): {client?.Player?.FamilyName}'s capture came to nothing; the garrison stands, or the point is not theirs to take any more.");
                return false;
            }

            SetHolder(point, Afs, ClanFor(client), client);
            return true;
        }

        /// <summary>
        /// Gives a point to a side, and to no clan: kept, the garrisons changed over, the
        /// hospital and waypoint opened or shut, and everyone on the map shown and told. Nothing
        /// if that side has it and no clan does.
        /// </summary>
        public bool SetOwner(Point point, byte owner, Client by) => SetHolder(point, owner, 0, by);

        /// <summary>
        /// Gives a point to a side and, with the AFS, to a clan or to none: kept; if the side
        /// changed, the garrisons changed over and the hospital and waypoint opened or shut; if
        /// a clan took it or lost it, its object changed for the one of the other class and its
        /// clan lockbox set down or taken off; and everyone on the map shown and told. Nothing
        /// if they have it already.
        /// </summary>
        public bool SetHolder(Point point, byte owner, uint clanId, Client by)
        {
            owner = owner == Afs ? Afs : Bane;

            if (owner != Afs)
                clanId = 0;

            if (point == null || point.IsBattleground || point.Owner == owner && point.ClanId == clanId)
                return false;

            var sideChanged = point.Owner != owner;
            var wasClans = point.HeldByClan;
            var now = UtcNow();

            point.Owner = owner;
            point.ClanId = clanId;
            point.ChangedAt = now;
            point.PaidAt = clanId != 0 ? now : 0;

            if (sideChanged)
            {
                point.DownSince = null;
                point.Returning = false;
            }

            Logger.WriteLog(LogType.Debug, $"Control point {point.Id} ({point.Name}) is {(point.HeldByClan ? $"clan {clanId}'s ({HolderName(point)})" : $"the {FactionName(owner)}'s")}{(by?.Player != null ? $", taken by {by.Player.FamilyName}" : "")}.");

            Save(point);

            if (sideChanged)
                ApplyTeleporters(point);

            var mapChannel = point.Map ?? point.Object?.RuntimeMapChannel;

            if (mapChannel == null || point.Object == null)
                return true;

            if (sideChanged)
                ApplyPools(mapChannel, point, takeOff: true);

            if (wasClans != point.HeldByClan)
            {
                // A clan's point is an object of another class: the one is taken off the map and
                // the other set down where it stood, as its holder has it.
                Rebuild(mapChannel, point);
            }
            else if (point.HeldByClan)
            {
                // From one clan to another. The state is the one it is in, and Use to it is the
                // clan class's way of being told its owner: it keeps the clan and makes the state
                // over, with that clan's effect. Who may take it from them is told anew.
                if (point.Object.IsInWorld)
                    CellManager.Instance.CellCallMethod(mapChannel, point.Object,
                        new UsePacket(by?.Player?.EntityId ?? 0, point.Object.StateId, (int)point.Object.WindupTime, (int)point.ClanId));

                TellUsable(mapChannel, point);
            }
            else
            {
                ShowOwner(point);

                // Out of service until the new garrison has been dealt with - or for good, if it is the AFS's.
                DynamicObjectManager.Instance.SetEnabled(point.Object, false);

                // One change of state, with the time a capture takes (ForceState sets both): the
                // owner's effect is put on once. UsableInfo after it would start the state over and
                // put the effect on a second time; it says nothing SetUsable and this have not.
                if (point.Object.IsInWorld)
                    CellManager.Instance.CellCallMethod(mapChannel, point.Object,
                        new ForceStatePacket(point.Object.StateId, (int)point.Object.WindupTime));
            }

            // The lockbox is the clan's that holds the point: made anew for a new one.
            ShowLockbox(point);

            if (sideChanged)
                MapMarkerManager.Instance.ControlPointChanged(mapChannel, point);

            Announce(mapChannel, PlayerMessage.PmControlpointOwned, point, HolderName(point));

            return true;
        }

        /// <summary>The point's row, as the point now is.</summary>
        private void Save(Point point)
        {
            var store = Store;

            if (store == null)
                return;

            try
            {
                store.Save(new ControlPointStateEntry
                {
                    ControlPointId = point.Id,
                    Owner = point.Owner,
                    ChangedAt = point.ChangedAt,
                    ClanId = point.ClanId,
                    ClanPaidAt = point.PaidAt
                });
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, $"Control point {point.Id} ({point.Name}): its owner was not saved: {e.Message}");
            }
        }

        /// <summary>
        /// Takes the point's object off the map and sets down one of the class its holder has it
        /// in, in its place. A client is given the class with the entity and cannot be told it
        /// has changed, so the new object is a new entity - made before the old one's id goes
        /// back to the pool, as it does on leaving the world, so that it is not given that id:
        /// a client is not told to make the entity it is being told to destroy.
        /// </summary>
        private void Rebuild(MapChannel mapChannel, Point point)
        {
            var old = point.Object;
            var inWorld = old.IsInWorld;
            var made = NewObject(mapChannel, point, old);

            _byObject.Remove(old.EntityId);
            point.ToldUsable.Clear();

            if (inWorld)
            {
                CellManager.Instance.RemoveFromWorld(mapChannel, old);
                old.IsInWorld = false;
            }
            else
                EntityManager.Instance.FreeEntity(old.EntityId);

            point.Object = made;
            _byObject[point.Object.EntityId] = point;
            mapChannel.ControlPoints[point.Id] = point.Object;

            ShowOwner(point);

            // One that was not on the map yet is set down by the object worker, as the first was to be.
            if (inWorld)
            {
                CellManager.Instance.AddToWorld(mapChannel, point.Object);
                point.Object.IsInWorld = true;
            }
        }

        /// <summary>
        /// The point's clan lockbox as the point now is: on the map while a clan holds it, a new
        /// object each time - the members of one clan are not left with the lockbox of another
        /// open - and off it otherwise.
        /// </summary>
        private void ShowLockbox(Point point)
        {
            var mapChannel = point.Map;

            if (mapChannel == null)
                return;

            // In service to the clan's members: each client is told for itself (ShownUsable).
            // Made before the one there is goes, so that it is not given that one's id (Rebuild).
            var made = point.LockboxId == 0 || !point.HeldByClan
                ? null
                : new DynamicObject
                {
                    Position = point.LockboxPosition,
                    Rotation = point.LockboxRotation,
                    MapContextId = point.MapContextId,
                    RuntimeMapChannel = mapChannel,
                    EntityClassId = EntityClasses.UsableClanLockboxV01,
                    DynamicObjectType = DynamicObjectType.Lockbox,
                    Comment = $"Clan Lockbox: {point.Name}",
                    StateId = UseObjectState.ClanlockboxState0,
                    IsEnabled = true
                };

            if (point.Lockbox != null)
            {
                _byLockbox.Remove(point.Lockbox.EntityId);
                CellManager.Instance.RemoveFromWorld(mapChannel, point.Lockbox);
                point.Lockbox.IsInWorld = false;
            }

            point.Lockbox = made;
            point.ToldOpen.Clear();

            if (made == null)
                return;

            _byLockbox[made.EntityId] = point;
            CellManager.Instance.AddToWorld(mapChannel, made);
            made.IsInWorld = true;
        }

        /// <summary>
        /// Each client that has an object of a clan's point is told whether it is in service to
        /// it, when that is not what it was last told: the point's object to those who may take
        /// it from the clan, the lockbox to the clan's members. A feud begun or ended, a player
        /// joining or leaving a clan: nothing else tells the client, and the object is made on it
        /// as things stood when it came into view (<see cref="ShownUsable"/>).
        /// </summary>
        private void TellUsable(MapChannel mapChannel, Point point)
        {
            Tell(mapChannel, point.Object, point.ToldUsable, client => MayTakeFromClan(client, point));
            Tell(mapChannel, point.Lockbox, point.ToldOpen, client => MayOpenLockbox(client, point));
        }

        private static void Tell(MapChannel mapChannel, DynamicObject obj, HashSet<Client> told, Func<Client, bool> may)
        {
            if (obj == null || !obj.IsInWorld)
            {
                told.Clear();
                return;
            }

            var seeing = CellManager.Instance.ClientsSeeing(mapChannel, obj);

            // Whoever has gone out of view has it made again when they are back.
            told.RemoveWhere(client => !seeing.Contains(client));

            foreach (var client in seeing)
            {
                var usable = may(client);

                if (usable == told.Contains(client))
                    continue;

                client.CallMethod(obj.EntityId, new SetUsablePacket(usable));
                Note(told, client, usable);
            }
        }

        #endregion

        #region The clans

        /// <summary>
        /// Once a second, from the map channel worker: the weekly reset, when its time has come;
        /// a point whose clan is no more, or held with clans holding none any longer, goes back
        /// to the AFS; and each point a clan holds pays it.
        /// </summary>
        public void ClanWorker()
        {
            if (_points.Count == 0)
                return;

            WeeklyResetWorker();

            foreach (var point in _points.Values.Where(p => p.HeldByClan).ToList())
            {
                if (!Config.ClanOwnership || ClanName(point.ClanId) == null)
                {
                    SetHolder(point, Afs, 0, null);
                    continue;
                }

                Pay(point);
            }
        }

        /// <summary>
        /// A point's pay, when it is due: <see cref="ControlPointConfig.ClanPrestige"/> for every
        /// <see cref="ControlPointConfig.ClanPrestigeMinutes"/> the clan has held it, one payment
        /// a pass. Time more than that behind - the server was down, the interval was made
        /// shorter - is paid once and not made up.
        /// </summary>
        private void Pay(Point point)
        {
            var amount = Config.ClanPrestige;
            var interval = Config.ClanPrestigeMinutes * 60000L;

            if (amount <= 0 || interval <= 0)
                return;

            var now = UtcNow();

            if (point.PaidAt <= 0 || point.PaidAt > now)
            {
                point.PaidAt = now;
                Save(point);
                return;
            }

            if (now - point.PaidAt < interval)
                return;

            // Kept before it is paid: a payment that fails is lost, and none is made twice.
            point.PaidAt = now - point.PaidAt >= 2 * interval ? now : point.PaidAt + interval;
            Save(point);

            try
            {
                if (!PayClan(point.ClanId, amount, point.Name))
                    Logger.WriteLog(LogType.Error, $"Control point {point.Id} ({point.Name}): clan {point.ClanId} was not paid its {amount} prestige.");
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, $"Control point {point.Id} ({point.Name}): clan {point.ClanId} was not paid its {amount} prestige: {e.Message}");
            }
        }

        /// <summary>
        /// The weekly reset of the clans' points, on the day and at the time the squad instances
        /// have theirs: when it comes round every point a clan holds goes back to the AFS. On
        /// the first pass after the server starts, the reset that came round while it was down:
        /// the points held from before it.
        /// </summary>
        private void WeeklyResetWorker()
        {
            if (!Config.ClanWeeklyReset || !SquadInstancePolicies.TryWeeklyReset(ResetSchedule?.Invoke(), out var day, out var time))
            {
                NextClanReset = null;
                _resetSetting = null;
                return;
            }

            var now = WallClock();
            var setting = $"{day} {time}";

            // The first pass, or the day or time changed in the file: from now on.
            if (NextClanReset == null || _resetSetting != setting)
            {
                NextClanReset = SquadInstancePolicies.NextReset(now, day, time);
                _resetSetting = setting;

                if (!_missedResetLookedFor)
                {
                    _missedResetLookedFor = true;

                    var last = NextClanReset.Value.AddDays(-7);
                    var missed = ReturnClanPoints(UtcNow() - (long)(now - last).TotalMilliseconds);

                    if (missed > 0)
                        Logger.WriteLog(LogType.Initialize, $"Weekly control point reset of {last:yyyy-MM-dd HH:mm}, missed: {missed} taken back from clans.");
                }

                return;
            }

            if (now < NextClanReset.Value)
                return;

            var returned = ReturnClanPoints();

            NextClanReset = SquadInstancePolicies.NextReset(now, day, time);

            Logger.WriteLog(LogType.Initialize, $"Weekly control point reset: {returned} taken back from clans. The next is {NextClanReset.Value:yyyy-MM-dd HH:mm}.");
        }

        /// <summary>
        /// Every point a clan holds goes back to the AFS - with a time given, only those held
        /// from before it. Returns how many did.
        /// </summary>
        public int ReturnClanPoints(long? heldBeforeUtcMs = null)
        {
            return _points.Values.Where(p => p.HeldByClan && (heldBeforeUtcMs == null || p.ChangedAt < heldBeforeUtcMs)).ToList()
                .Count(point => SetHolder(point, Afs, 0, null));
        }

        /// <summary>A clan has disbanded (ClanManager): the points it held are the AFS's. Returns how many there were.</summary>
        public int ClanDisbanded(uint clanId)
        {
            if (clanId == 0)
                return 0;

            return _points.Values.Where(p => p.HeldByClan && p.ClanId == clanId).ToList()
                .Count(point => SetHolder(point, Afs, 0, null));
        }

        /// <summary>
        /// Sets a point's clan lockbox down, or stands it somewhere else: a footlocker row of
        /// the clan lockbox's class and the link that makes it the point's, kept in the world
        /// database. On the map at once if a clan holds the point. For a game master (.cp).
        /// </summary>
        public bool SetLockbox(Point point, Vector3 position, double rotation, IGameUnitOfWorkFactory factory)
        {
            if (point == null || point.IsBattleground)
                return false;

            try
            {
                using var unitOfWork = factory.CreateWorld();

                if (point.LockboxId != 0)
                {
                    if (!unitOfWork.Footlockers.UpdatePosition(point.LockboxId, position.X, position.Y, position.Z, rotation))
                        return false;
                }
                else
                {
                    var comment = $"Clan Lockbox {point.Name}";

                    var id = unitOfWork.Footlockers.AddFootlocker(new FootlockerEntry
                    {
                        ClassId = (uint)EntityClasses.UsableClanLockboxV01,
                        MapContextId = point.MapContextId,
                        PosX = position.X,
                        PosY = position.Y,
                        PosZ = position.Z,
                        Rotation = rotation,
                        Comment = comment.Length > 64 ? comment.Substring(0, 64) : comment
                    });

                    if (id == 0)
                        return false;

                    unitOfWork.ControlPoints.AddLink(point.Id, ControlPointLinkEntry.KindClanLockbox, id);
                    point.LockboxId = id;
                }
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, $"Control point {point.Id} ({point.Name}): its clan lockbox was not set down: {e.Message}");
                return false;
            }

            point.LockboxPosition = position;
            point.LockboxRotation = rotation;
            ShowLockbox(point);

            return true;
        }

        /// <summary>Takes a point's clan lockbox away, off the map and out of the world database. For a game master (.cp).</summary>
        public bool RemoveLockbox(Point point, IGameUnitOfWorkFactory factory)
        {
            if (point == null || point.LockboxId == 0)
                return false;

            try
            {
                using var unitOfWork = factory.CreateWorld();

                unitOfWork.ControlPoints.RemoveLink(point.Id, ControlPointLinkEntry.KindClanLockbox, point.LockboxId);
                unitOfWork.Footlockers.DeleteFootlocker(point.LockboxId);
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, $"Control point {point.Id} ({point.Name}): its clan lockbox was not taken away: {e.Message}");
                return false;
            }

            point.LockboxId = 0;
            ShowLockbox(point);

            return true;
        }

        #endregion

        #region Garrison kills, and a game master's move

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

        /// <summary>The object's state and side, as its owner has it: a clan's is of the clan class, which has states of its own.</summary>
        private static void ShowOwner(Point point)
        {
            if (point.Object == null)
                return;

            if (point.Object.EntityClassId == ClanPointClass)
                point.Object.StateId = point.HeldByClan ? UseObjectState.CcpStateClanControlled
                    : point.HeldByAfs ? UseObjectState.CcpStateAfsControlled : UseObjectState.CcpStateBaneControlled;
            else
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

            // A destroyed turret of the side that has lost the point is not left on its mount for
            // the other side's to be set down in.
            TakeOffWrecks(mapChannel, idle);

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

        /// <summary>
        /// Takes the destroyed emplacements of these pools off the map: an AFS turret's wreck its
        /// pool was keeping to put back in service (AlternateMesh), and a Bane turret's corpse.
        /// The two sides' turrets of a point stand on the same spots (ControlPointTurrets), so
        /// the side that now holds it would be set down in the other's wreckage. One with loot
        /// on it is left for its killer.
        /// </summary>
        internal static void TakeOffWrecks(MapChannel mapChannel, List<SpawnPool> pools)
        {
            if (pools.Count == 0 || mapChannel?.MapCellInfo?.Cells == null)
                return;

            var wrecks = mapChannel.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList)
                .Where(c => c != null && c.State == CharacterState.Dead && c.SpawnPool != null && pools.Contains(c.SpawnPool)
                    && Emplacements.Is(c) && c.LootDispenserObjectEntityId == 0)
                .Distinct().ToList();

            foreach (var wreck in wrecks)
            {
                AlternateMesh.Forget(wreck);
                MapReset.Remove(mapChannel, wreck);
            }
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

        /// <summary>"%(faction)s claiming / took Control Point %(cpName)s", to everyone on the map: the faction is "AFS", "Bane" or a clan's name.</summary>
        private static void Announce(MapChannel mapChannel, PlayerMessage message, Point point, string faction)
        {
            if (mapChannel?.ClientList == null)
                return;

            var args = new Dictionary<string, string> { { "faction", faction }, { "cpName", point.Name } };

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

            public void Save(ControlPointStateEntry state)
            {
                using var unitOfWork = _factory.CreateChar();
                unitOfWork.ControlPointStates.SaveState(state);
            }
        }
    }
}
