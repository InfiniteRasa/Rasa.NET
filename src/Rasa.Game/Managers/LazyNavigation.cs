using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Rasa.Managers
{
    using Config;
    using Game;
    using Navigation;
    using Structures;

    /// <summary>
    /// A map's navmesh and cover read when a player is first on the way to it, in place of every
    /// map's at the start (appsettings.json's Navigation.Lazy), and let go of again once nobody
    /// has been on it for Navigation.UnloadAfterSeconds.
    ///
    /// Read at the start, the 77 maps' files are about 2.8 GB of the heap (1.2 GB navmesh, 1.7 GB
    /// cover), and most of it is maps nobody is on: a map with nobody on its list is not
    /// simulated (MapChannelManager.MapChannelWorker), so nothing asks its navigation.
    ///
    /// With the key on the start only notes which maps have files (<see cref="Note"/>). Once a
    /// pass (<see cref="Worker"/>, on the loop's thread) every client's map, and the map a client
    /// is being sent to, has its files read from then on, on a thread of their own, while the
    /// client loads the map; a MapLoaded that comes before they are read waits for them
    /// (<see cref="Hold"/>), so the player is behind the loading screen that much longer, and no
    /// player is in the world of a map without the navigation it would have had. Whoever asks a
    /// map's navigation that is not read all the same - something nobody thought of, on a map
    /// with nobody on it - has it read there and then (<see cref="Demand"/>), and waits: "no
    /// navmesh" never means only "not read yet".
    ///
    /// The copies of a map (private, shared, squad: PrivateMapInstanceService) hold the map's own
    /// <see cref="MapNavigation"/> and so its one navmesh, as they held its one NavMeshQuery
    /// before. A file is kept while anyone is on the map or a copy, on the way to one, or while a
    /// dropship is still over one: a spawner's troops are put down on the navmesh
    /// (SpawnPoolManager.SpawnCreatures) and a ship lands after the last player has gone.
    /// </summary>
    public static class LazyNavigation
    {
        public static NavigationConfig Config { get; set; } = new NavigationConfig();

        /// <summary>What reads a map's files: NavMeshManager.LoadFiles, kept as a call so that a test can put a slow or a failing one in its place.</summary>
        internal static Func<string, string, NavMeshManager.MapFiles> Reader { get; set; } = NavMeshManager.LoadFiles;

        /// <summary>Whether a dropship is over the navigation's map or a copy of it; kept as a call for the tests.</summary>
        internal static Func<MapNavigation, bool> ShipsOver { get; set; } = DropshipsOver;

        private static readonly object Sync = new object();
        private static readonly List<MapNavigation> Noted = new List<MapNavigation>();
        private static long _pass;

        /// <summary>Whether any map's files wait to be read when the map is played on.</summary>
        public static bool Active { get; private set; }

        /// <summary>Since the start: maps read, reads somebody had to wait for on the spot, maps let go of, MapLoadeds that waited.</summary>
        public static long Reads { get; private set; }
        public static long Demands { get; private set; }
        public static long Unloads { get; private set; }
        public static long Holds { get; private set; }

        /// <summary>The maps noted, for whoever lists them (a test, the memory report).</summary>
        public static MapNavigation[] All()
        {
            lock (Sync)
                return Noted.ToArray();
        }

        /// <summary>Forgets every map noted and every count: NavMeshInit begins anew.</summary>
        internal static void Reset()
        {
            lock (Sync)
            {
                foreach (var navigation in Noted)
                    if (ReferenceEquals(navigation.Map.NavigationFiles, navigation))
                        navigation.Map.NavigationFiles = null;

                Noted.Clear();
                Active = false;
                _pass = 0;
                Reads = Demands = Unloads = Holds = 0;
            }
        }

        /// <summary>NavMeshInit with the key on: the map has files, to be read when somebody comes.</summary>
        internal static MapNavigation Note(MapChannel map, string directory)
        {
            var navigation = new MapNavigation(map, directory);

            lock (Sync)
            {
                Noted.Add(navigation);
                Active = true;
            }

            map.UseNavigation(navigation);

            return navigation;
        }

        /// <summary>Somebody is on the map, or on the way to it: its files are read from now, unless they are or are being read.</summary>
        internal static void Want(MapNavigation navigation)
        {
            if (navigation == null)
                return;

            lock (Sync)
            {
                Busy(navigation);
                Begin(navigation);
            }
        }

        /// <summary>
        /// A client's MapLoaded has come: whether it has to wait for its map's files. If so it is
        /// kept, and answered by the pass that finds them read (<see cref="Worker"/>).
        /// </summary>
        internal static bool Hold(Client client)
        {
            var navigation = client?.Player?.MapChannel?.NavigationFiles;

            if (navigation == null)
                return false;

            lock (Sync)
            {
                Busy(navigation);

                if (navigation.Settled)
                    return false;

                // Nothing has begun the read - no pass of the worker since the client was sent
                // here, which the client's own loading screen outlasts: read now (Demand), and the
                // arrival goes ahead. What is held is only ever a read already under way.
                if (navigation.Reading == null)
                {
                    Demand(navigation);
                    return false;
                }

                if (!navigation.Held.Any(held => ReferenceEquals(held.Client, client)))
                {
                    navigation.Held.Add((client, Stopwatch.GetTimestamp()));
                    Holds++;
                }

                return true;
            }
        }

        /// <summary>
        /// The navigation is asked for and is not read: it is read here and now, and the asker
        /// waits. Not the ordinary way - the read begins when a client is on the way to the map and
        /// nobody is let into its world before it is done - but what makes a "no navmesh" always
        /// mean that there is none.
        /// </summary>
        internal static void Demand(MapNavigation navigation)
        {
            List<Action> whenRead;

            lock (Sync)
            {
                if (navigation.Settled)
                    return;

                var from = Stopwatch.GetTimestamp();

                Busy(navigation);
                Begin(navigation);
                whenRead = Adopt(navigation);
                Demands++;

                Logger.WriteLog(LogType.Initialize,
                    $"Navigation of map {navigation.Map.MapInfo?.MapContextId} ({navigation.MapName}) was asked for before it was read: " +
                    $"the asker waited {Stopwatch.GetElapsedTime(from).TotalMilliseconds:0} ms for it");
            }

            Run(whenRead, navigation);
        }

        /// <summary>
        /// Once a pass, on the loop's thread: every client's map and destination is wanted; the
        /// reads that are done are taken in; the clients whose MapLoaded waited for one are
        /// answered (<paramref name="answer"/>); and files nobody has needed for
        /// Navigation.UnloadAfterSeconds are let go of.
        /// </summary>
        internal static void Worker(long nowMs, IEnumerable<Client> clients, IEnumerable<MapChannel> maps, Action<Client> answer)
        {
            if (!Active)
                return;

            List<(Client Client, long From, MapNavigation Navigation)> waited = null;
            List<(List<Action> Actions, MapNavigation Navigation)> read = null;

            lock (Sync)
            {
                _pass++;

                foreach (var client in clients)
                {
                    Want(client?.Player?.MapChannel?.NavigationFiles);
                    Want(client?.PendingTransfer?.DestinationMap?.NavigationFiles);
                }

                foreach (var map in maps)
                    if (map?.NavigationFiles != null && (map.ClientList?.Count > 0 || map.QueuedClients.Count > 0))
                        Want(map.NavigationFiles);

                var unloadAfterMs = (long)(Config?.UnloadAfterSeconds ?? 0) * 1000;

                foreach (var navigation in Noted)
                {
                    if (navigation.Reading != null && navigation.Reading.IsCompleted)
                    {
                        var actions = Adopt(navigation);

                        if (actions != null)
                            (read ??= new List<(List<Action>, MapNavigation)>()).Add((actions, navigation));
                    }

                    if (navigation.Held.Count > 0 && navigation.Settled)
                    {
                        waited ??= new List<(Client, long, MapNavigation)>();

                        foreach (var (client, from) in navigation.Held)
                            waited.Add((client, from, navigation));

                        navigation.Held.Clear();
                    }

                    if (!navigation.IsRead)
                        continue;

                    if (unloadAfterMs <= 0 || navigation.BusyPass >= _pass || navigation.Held.Count > 0)
                    {
                        navigation.IdleSinceMs = null;
                        continue;
                    }

                    if (navigation.IdleSinceMs == null)
                    {
                        navigation.IdleSinceMs = nowMs;
                        continue;
                    }

                    if (nowMs - navigation.IdleSinceMs.Value < unloadAfterMs || ShipsOver(navigation))
                        continue;

                    navigation.Unload();
                    Unloads++;

                    Logger.WriteLog(LogType.Initialize,
                        $"Navigation of map {navigation.Map.MapInfo?.MapContextId} ({navigation.MapName}) let go of: nobody on the map or a copy of it for {Config.UnloadAfterSeconds} s");
                }
            }

            if (read != null)
                foreach (var (actions, navigation) in read)
                    Run(actions, navigation);

            if (waited == null)
                return;

            // Outside the lock: an answer puts a player into the world, and reaches into every manager.
            foreach (var (client, from, navigation) in waited)
            {
                // Dropped, or sent elsewhere, while it waited: there is nothing to answer.
                if (client.State == Data.ClientState.Disconnected || !client.AwaitingMapLoaded ||
                    !ReferenceEquals(client.Player?.MapChannel?.NavigationFiles, navigation))
                    continue;

                Logger.WriteLog(LogType.Initialize,
                    $"MapLoaded of account {client.AccountEntry?.Id} waited {Stopwatch.GetElapsedTime(from).TotalMilliseconds:0} ms for the navigation of map {navigation.Map.MapInfo?.MapContextId}");

                try
                {
                    answer(client);
                }
                catch (Exception e)
                {
                    Logger.WriteLog(LogType.Error, $"Answering a MapLoaded that waited for the navigation of map {navigation.Map.MapInfo?.MapContextId} threw: {e}");
                }
            }
        }

        /// <summary>
        /// Where <paramref name="position"/> stands on the map's ground, as NavMeshManager.SnapToGround
        /// has it - now, when the map's navmesh is read or the map has none to read, and otherwise
        /// once it is: <paramref name="snapped"/> is then called with the place, on the loop's thread,
        /// before any player is let into the map, and the position is handed back as it is for now.
        /// For what the start puts down on every map (control points, a battleground's), which would
        /// otherwise read every map's navmesh at the start only to put them on it.
        /// </summary>
        public static Vector3 SnapToGroundOnceRead(MapChannel map, Vector3 position, Action<Vector3> snapped)
        {
            var navigation = map?.WaitingNavigation;

            if (navigation == null)
                return NavMeshManager.SnapToGround(map, position);

            lock (Sync)
            {
                if (!navigation.Settled)
                {
                    navigation.WhenRead.Add(() => snapped(NavMeshManager.SnapToGround(map, position)));
                    return position;
                }
            }

            return NavMeshManager.SnapToGround(map, position);
        }

        private static void Run(List<Action> actions, MapNavigation navigation)
        {
            if (actions == null)
                return;

            foreach (var action in actions)
            {
                try
                {
                    action();
                }
                catch (Exception e)
                {
                    Logger.WriteLog(LogType.Error, $"Something waiting for the navigation of map {navigation.Map.MapInfo?.MapContextId} threw: {e}");
                }
            }
        }

        /// <summary>Somebody needs the files in this pass: they are not idle. With the lock held.</summary>
        private static void Busy(MapNavigation navigation)
        {
            navigation.BusyPass = _pass;
            navigation.IdleSinceMs = null;
        }

        /// <summary>Begins the read on a thread of its own, unless the files are read, or are being read. With the lock held.</summary>
        private static void Begin(MapNavigation navigation)
        {
            if (navigation.Settled || navigation.Reading != null)
                return;

            var reader = Reader;
            var directory = navigation.Directory;
            var name = navigation.MapName;

            navigation.Reading = Task.Run(() =>
            {
                var from = Stopwatch.GetTimestamp();
                var files = reader(directory, name);

                return (files, Stopwatch.GetElapsedTime(from).TotalMilliseconds);
            });
        }

        /// <summary>
        /// Takes a read in, waiting for it when it is not done: the map and its copies have their
        /// navigation from here on. What failed is said and recorded as NavMeshInit says it at the
        /// start, and the map runs without it. With the lock held; hands back what waited for the read.
        /// </summary>
        private static List<Action> Adopt(MapNavigation navigation)
        {
            var reading = navigation.Reading;
            navigation.Reading = null;

            NavMeshManager.MapFiles files;
            double ms;

            try
            {
                (files, ms) = reading.GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                files = new NavMeshManager.MapFiles { NavMeshError = e.Message, HasNavMesh = true };
                ms = 0;
            }

            navigation.Take(files);
            navigation.LastReadMs = ms;
            Reads++;

            var mapId = navigation.Map.MapInfo?.MapContextId ?? 0;

            if (files.CoverError != null)
            {
                Logger.WriteLog(LogType.Error, $"Could not load cover {files.CoverPath}: {files.CoverError}");
                MapErrorManager.Instance.Record(mapId, $"cover {files.CoverPath} could not be loaded: {files.CoverError}");
            }

            if (files.NavMeshError != null)
            {
                Logger.WriteLog(LogType.Error, $"Could not load navmesh {files.NavMeshPath}: {files.NavMeshError}");
                MapErrorManager.Instance.Record(mapId, $"navmesh {files.NavMeshPath} could not be loaded: {files.NavMeshError}");
            }

            Logger.WriteLog(LogType.Initialize,
                $"Navigation of map {mapId} ({navigation.MapName}) read in {ms:0} ms: navmesh {(files.NavMesh != null ? "yes" : "no")}, cover {(files.Cover != null ? "yes" : "no")}");

            if (navigation.WhenRead.Count == 0)
                return null;

            var actions = navigation.WhenRead.ToList();
            navigation.WhenRead.Clear();

            return actions;
        }

        private static bool DropshipsOver(MapNavigation navigation)
        {
            foreach (var dropship in DynamicObjectManager.Instance.Dropships.Values)
                if (ReferenceEquals(dropship?.RuntimeMapChannel?.NavigationFiles, navigation))
                    return true;

            return false;
        }
    }

    /// <summary>
    /// One map's navmesh and cover under Navigation.Lazy: where they are, and what has been read
    /// of them. Held by the map's own channel and every copy of it (MapChannel.NavigationFiles), so that
    /// all of them have the one navmesh. Kept by <see cref="LazyNavigation"/>.
    /// </summary>
    public sealed class MapNavigation
    {
        private volatile NavMeshQuery _navMesh;
        private volatile CoverMesh _cover;
        private volatile bool _settled;

        internal MapNavigation(MapChannel map, string directory)
        {
            Map = map;
            Directory = directory;
            MapName = map.MapInfo?.MapName;
        }

        /// <summary>The map's own channel.</summary>
        public MapChannel Map { get; }

        public string Directory { get; }
        public string MapName { get; }

        /// <summary>Whether the files have been read, and what was in them taken in (or found to be missing or broken).</summary>
        public bool Settled => _settled;

        /// <summary>Whether something read is held now: a navmesh or a cover.</summary>
        public bool IsRead => _settled && (_navMesh != null || _cover != null);

        /// <summary>How many times the files were read, and let go of; how long the last read took.</summary>
        public int ReadCount { get; private set; }
        public int UnloadCount { get; private set; }
        public double LastReadMs { get; internal set; }

        /// <summary>The navmesh, read now if it is not (LazyNavigation.Demand); null when the map has none.</summary>
        public NavMeshQuery NavMesh
        {
            get
            {
                if (!_settled)
                    LazyNavigation.Demand(this);

                return _navMesh;
            }
        }

        /// <summary>The cover, read now if it is not; null when the map has none.</summary>
        public CoverMesh Cover
        {
            get
            {
                if (!_settled)
                    LazyNavigation.Demand(this);

                return _cover;
            }
        }

        /// <summary>The navmesh if it is read, without reading it: for a report over every map.</summary>
        public NavMeshQuery NavMeshIfRead => _settled ? _navMesh : null;

        /// <summary>The cover if it is read, without reading it.</summary>
        public CoverMesh CoverIfRead => _settled ? _cover : null;

        /// <summary>
        /// The map's safety floor and highest walkable ground (MapChannel.SafetyFloorY), from its
        /// navmesh: read for them the first time, and kept when the navmesh is let go of - two numbers.
        /// </summary>
        public float? SafetyFloorY
        {
            get
            {
                if (!_settled && !_heightsKnown)
                    LazyNavigation.Demand(this);

                return _safetyFloorY;
            }
        }

        public float? TopWalkableY
        {
            get
            {
                if (!_settled && !_heightsKnown)
                    LazyNavigation.Demand(this);

                return _topWalkableY;
            }
        }

        private float? _safetyFloorY, _topWalkableY;
        private bool _heightsKnown;

        internal System.Threading.Tasks.Task<(NavMeshManager.MapFiles Files, double Ms)> Reading { get; set; }
        internal readonly List<(Client Client, long From)> Held = new List<(Client, long)>();
        internal readonly List<Action> WhenRead = new List<Action>();
        internal long BusyPass { get; set; }
        internal long? IdleSinceMs { get; set; }

        /// <summary>What a read found, taken in. With LazyNavigation's lock held.</summary>
        internal void Take(NavMeshManager.MapFiles files)
        {
            _cover = files.CoverError == null ? files.Cover : null;
            _navMesh = files.NavMeshError == null ? files.NavMesh : null;

            if (_navMesh != null && files.HeightRange is var (lowest, highest))
            {
                _safetyFloorY = SafetyFloor.FloorFor(lowest);
                _topWalkableY = highest;
            }

            _heightsKnown = true;
            ReadCount++;
            _settled = true;
        }

        /// <summary>Lets go of what was read; the next to ask has it read again. With LazyNavigation's lock held.</summary>
        internal void Unload()
        {
            _settled = false;
            _navMesh = null;
            _cover = null;
            IdleSinceMs = null;
            UnloadCount++;
        }
    }
}
