using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Rasa.Managers
{
    using Game;
    using Navigation;
    using Structures;

    /// <summary>
    /// Loads each map's navmesh (built offline by Rasa.NavMesh from the client's terrain and
    /// collision meshes) and answers the ground and path questions creature AI asks. Ordinary
    /// creatures on maps without navigation retain legacy movement; scripted routes require it.
    ///
    /// The queries are only made from the world loop (BehaviorManager, spawning) and from GM
    /// commands on the packet thread; the two never run concurrently, since packets are processed
    /// on the loop too. A <see cref="NavMeshQuery"/> per map is therefore enough.
    /// </summary>
    public class NavMeshManager
    {
        private static NavMeshManager _instance;
        private static readonly object InstanceLock = new object();

        /// <summary>The default asset folder, resolved from the working directory, deployment or source checkout.</summary>
        public const string DefaultDirectory = "navmesh";

        /// <summary>
        /// How far the navmesh ground may be from a position we snap to it. A creature standing on
        /// a bridge must not be pulled down to the road beneath, nor one under it lifted up.
        /// </summary>
        public const float SnapTolerance = 3.0f;

        /// <summary>The fewest directions probed on a ring when looking out from a point that is off the mesh.</summary>
        public const int RingSamples = 8;

        /// <summary>
        /// The most ring between two probes, in metres. A probe sees SearchExtents either side of
        /// itself - 4 m, so 8 m across - and eight probes on a 32 m ring are 25 m apart: ground
        /// between them was never seen, and a pool beside a 30 m stretch of ground found none
        /// (SpawnPoolManager.Anchor). Rings further out get the probes their length needs.
        /// </summary>
        public const float RingSpacing = 6f;

        /// <summary>How many probes a ring of this radius gets, so that no two are further apart than RingSpacing.</summary>
        public static int RingSamplesFor(float radius) =>
            Math.Max(RingSamples, (int)Math.Ceiling(2.0 * Math.PI * radius / RingSpacing));

        public static NavMeshManager Instance
        {
            get
            {
                // ReSharper disable once InvertIf
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                            _instance = new NavMeshManager();
                    }
                }

                return _instance;
            }
        }

        private NavMeshManager()
        {
        }

        public string Directory { get; private set; }
        public int LoadedMaps { get; private set; }

        /// <summary>
        /// Loads a navmesh and a cover for every map channel that has them. Runs after
        /// MapChannelInit.
        ///
        /// Each map's files are read on their own (<see cref="LoadFiles"/>), several maps at once,
        /// the largest first so that the longest reads do not start last: nothing a map's files
        /// become is shared with another map. What was read is then given to the map channels,
        /// and anything that failed logged, one map after another in the order they always were.
        /// </summary>
        public void NavMeshInit(string directory)
        {
            Directory = ResolveDirectory(directory, Environment.CurrentDirectory, AppContext.BaseDirectory);
            LoadedMaps = 0;

            if (!System.IO.Directory.Exists(Directory))
            {
                Logger.WriteLog(LogType.Initialize, $"No navmesh folder at {Path.GetFullPath(Directory)}; creatures will move in straight lines");
                return;
            }

            var mapChannels = MapChannelManager.Instance.MapChannelArray.Values.ToList();
            var loaded = new MapFiles[mapChannels.Count];

            Parallel.ForEach(
                Enumerable.Range(0, mapChannels.Count).OrderByDescending(i => FileSize(Directory, mapChannels[i].MapInfo.MapName)),
                i => loaded[i] = LoadFiles(Directory, mapChannels[i].MapInfo.MapName));

            var missing = new List<string>();
            var covers = 0;

            for (var i = 0; i < mapChannels.Count; i++)
            {
                var mapChannel = mapChannels[i];
                var files = loaded[i];

                if (files.CoverError != null)
                {
                    Logger.WriteLog(LogType.Error, $"Could not load cover {files.CoverPath}: {files.CoverError}");
                    MapErrorManager.Instance.Record(mapChannel.MapInfo.MapContextId, $"cover {files.CoverPath} could not be loaded: {files.CoverError}");
                }
                else if (files.Cover != null)
                {
                    mapChannel.Cover = files.Cover;
                    covers++;
                }

                if (!files.HasNavMesh)
                {
                    missing.Add(mapChannel.MapInfo.MapName);
                    continue;
                }

                if (files.NavMeshError != null)
                {
                    Logger.WriteLog(LogType.Error, $"Could not load navmesh {files.NavMeshPath}: {files.NavMeshError}");
                    MapErrorManager.Instance.Record(mapChannel.MapInfo.MapContextId, $"navmesh {files.NavMeshPath} could not be loaded: {files.NavMeshError}");
                    continue;
                }

                mapChannel.NavMesh = files.NavMesh;
                LoadedMaps++;

                // The floor under the map, for players who fall out of the world (SafetyFloor).
                if (files.HeightRange is var (lowest, highest))
                {
                    mapChannel.SafetyFloorY = SafetyFloor.FloorFor(lowest);
                    mapChannel.TopWalkableY = highest;
                }
            }

            Logger.WriteLog(LogType.Initialize, $"Loaded navmeshes for {LoadedMaps} of {MapChannelManager.Instance.MapChannelArray.Count} maps from {Path.GetFullPath(Directory)}");
            Logger.WriteLog(LogType.Initialize, covers > 0
                ? $"Loaded cover for {covers} maps"
                : "No cover files (<map>.cover, Rasa.NavMesh --cover-only); ranged attacks ignore cover");

            if (missing.Count > 0 && missing.Count <= 12)
                Logger.WriteLog(LogType.Initialize, $"  no navmesh for: {string.Join(", ", missing)}");
        }

        /// <summary>What one map's navmesh and cover files held, or why they could not be read.</summary>
        internal sealed class MapFiles
        {
            public string NavMeshPath, CoverPath;
            public bool HasNavMesh;
            public NavMeshQuery NavMesh;
            public (float Lowest, float HighestWalkable)? HeightRange;
            public string NavMeshError;
            public CoverMesh Cover;
            public string CoverError;
        }

        /// <summary>
        /// Reads one map's files. Cover rides along with the navmesh but stands on its own: a map
        /// may have one without the other, and one failing to load leaves the other.
        /// </summary>
        internal static MapFiles LoadFiles(string directory, string mapName)
        {
            var files = new MapFiles
            {
                NavMeshPath = NavMeshFile.PathFor(directory, mapName),
                CoverPath = CoverMesh.PathFor(directory, mapName)
            };

            if (File.Exists(files.CoverPath))
            {
                try
                {
                    files.Cover = CoverMesh.Read(files.CoverPath);
                }
                catch (Exception e)
                {
                    files.CoverError = e.Message;
                }
            }

            files.HasNavMesh = File.Exists(files.NavMeshPath);

            if (files.HasNavMesh)
            {
                try
                {
                    files.NavMesh = new NavMeshQuery(NavMeshFile.Read(files.NavMeshPath));
                    files.HeightRange = files.NavMesh.HeightRange();
                }
                catch (Exception e)
                {
                    files.NavMesh = null;
                    files.NavMeshError = e.Message;
                }
            }

            return files;
        }

        /// <summary>The size of a map's navmesh and cover together, 0 for files that are not there.</summary>
        private static long FileSize(string directory, string mapName)
        {
            long Size(string path) => File.Exists(path) ? new FileInfo(path).Length : 0;

            return Size(NavMeshFile.PathFor(directory, mapName)) + Size(CoverMesh.PathFor(directory, mapName));
        }

        internal static string ResolveDirectory(string configuredDirectory, string workingDirectory, string applicationDirectory)
        {
            var directory = string.IsNullOrWhiteSpace(configuredDirectory) ? DefaultDirectory : configuredDirectory;
            var configuredPath = Path.GetFullPath(directory, workingDirectory);
            if (System.IO.Directory.Exists(configuredPath) ||
                !string.Equals(directory, DefaultDirectory, StringComparison.Ordinal))
                return configuredPath;
            var deployedPath = Path.Combine(applicationDirectory, DefaultDirectory);
            if (System.IO.Directory.Exists(deployedPath))
                return deployedPath;
            for (var candidate = new DirectoryInfo(applicationDirectory); candidate != null; candidate = candidate.Parent)
                if (File.Exists(Path.Combine(candidate.FullName, "Rasa.NET.sln")))
                {
                    var repositoryPath = Path.Combine(candidate.FullName, DefaultDirectory);
                    if (System.IO.Directory.Exists(repositoryPath))
                        return repositoryPath;
                    break;
                }
            return configuredPath;
        }

        /// <summary>
        /// The position moved onto the walkable surface nearest to it, when the map has a navmesh
        /// and that surface is within <see cref="SnapTolerance"/> vertically; otherwise unchanged.
        /// </summary>
        public static Vector3 SnapToGround(MapChannel mapChannel, Vector3 position)
        {
            var navMesh = mapChannel?.NavMesh;

            if (navMesh == null)
                return position;

            var ground = navMesh.GroundHeight(position);

            if (ground == null || Math.Abs(ground.Value - position.Y) > SnapTolerance)
                return position;

            return new Vector3(position.X, ground.Value, position.Z);
        }

        /// <summary>
        /// Whether the walkable surface under the position was built under the terrain: inside a
        /// cave or tunnel. False on a map with no navmesh, or one built before the flag existed.
        /// </summary>
        public static bool IsUnderground(MapChannel mapChannel, Vector3 position)
        {
            return mapChannel?.NavMesh?.IsUnderground(position) ?? false;
        }

        /// <summary>
        /// A place to stand within <paramref name="radius"/> of <paramref name="centre"/>, on the
        /// navmesh when there is one and the centre is near it; null when there is no navmesh or
        /// the centre is off it, so the caller can fall back to picking a point itself.
        /// </summary>
        public static Vector3? RandomPointAround(MapChannel mapChannel, Vector3 centre, float radius)
        {
            return mapChannel?.NavMesh?.RandomPointAround(centre, radius);
        }

        /// <summary>
        /// The walkable point nearest to the position anywhere within <paramref name="height"/>
        /// above or below it: for a position whose height is a guess. Null without a navmesh.
        /// </summary>
        public static Vector3? NearestInColumn(MapChannel mapChannel, Vector3 position, float height = 200f)
        {
            return mapChannel?.NavMesh?.NearestInColumn(position, height);
        }

        /// <summary>
        /// Somewhere walkable to put a player who is stuck. Tries the point itself first, which
        /// answers for anyone wedged against geometry; a player buried deeper than the query's
        /// own search extents is looked for from further out, in rings, and the candidate nearest
        /// to where they actually are wins. Null when the map has no navmesh, or when nothing
        /// walkable is within <paramref name="maxRadius"/>.
        /// </summary>
        public static Vector3? NearestWalkable(MapChannel mapChannel, Vector3 position, float maxRadius = 32f)
        {
            var navMesh = mapChannel?.NavMesh;

            if (navMesh == null)
                return null;

            var here = navMesh.Nearest(position);

            if (here != null)
                return here;

            Vector3? best = null;
            var bestDistance = float.MaxValue;

            for (var radius = 8f; radius <= maxRadius; radius *= 2f)
            {
                var samples = RingSamplesFor(radius);

                for (var step = 0; step < samples; step++)
                {
                    var angle = step * 2.0 * Math.PI / samples;
                    var probe = new Vector3(
                        position.X + (float)(Math.Cos(angle) * radius),
                        position.Y,
                        position.Z + (float)(Math.Sin(angle) * radius));

                    var candidate = navMesh.Nearest(probe);

                    if (candidate == null)
                        continue;

                    var distance = Vector3.Distance(position, candidate.Value);

                    if (distance >= bestDistance)
                        continue;

                    bestDistance = distance;
                    best = candidate;
                }

                // The first ring that finds anything is already the closest one that can, so
                // there is no reason to search further out.
                if (best != null)
                    return best;
            }

            return best;
        }

        /// <summary>
        /// Where /stuck puts a player: the nearest walkable point (<see cref="NearestWalkable"/>),
        /// unless that is under the terrain (NavMeshFlags.Underground) with walkable ground over it.
        /// Then the ground: the highest walkable surface over them (SafetyFloor.TopOfColumn).
        ///
        /// The navmesh has mesh built inside hillsides, under the terrain heightmap: in Lower Eloh
        /// Creek whole layers of it lie 8 m under the ground. A player thrown into it is inside the
        /// hill, with the client's ground above them, and its nearest walkable point is more of the
        /// same, a step from where they are: "unstuck from (343.52, 226, 194.99) to (343.54, 226.12,
        /// 194.64) (0.4 m)". A player in a real cave under the open world who uses /stuck goes up
        /// to the ground over it too; the caves are instances of their own almost everywhere, and
        /// stuck is stuck. Null when the map has no navmesh or nothing walkable is within reach.
        /// </summary>
        public static Vector3? UnstickDestination(MapChannel mapChannel, Vector3 position, float maxRadius = 32f)
        {
            var navMesh = mapChannel?.NavMesh;
            var here = NearestWalkable(mapChannel, position, maxRadius);

            if (navMesh == null || here == null || !navMesh.IsUnderground(here.Value))
                return here;

            var ground = SafetyFloor.TopOfColumn(mapChannel, position);

            return ground != null && ground.Value.Y > here.Value.Y && !navMesh.IsUnderground(ground.Value) ? ground : here;
        }

        /// <summary>
        /// The corners to walk through from <paramref name="start"/> to <paramref name="end"/>,
        /// ending as near to <paramref name="end"/> as the mesh allows. Null when the map has no
        /// navmesh or either point is off it: the caller walks straight.
        /// </summary>
        public static List<Vector3> FindPath(MapChannel mapChannel, Vector3 start, Vector3 end)
        {
            return mapChannel?.NavMesh?.FindPath(start, end, out _);
        }

        /// <summary>
        /// As <see cref="FindPath(MapChannel, Vector3, Vector3)"/>, for a goal that may be where no
        /// route goes: one in another walkable region than the start is searched only so far
        /// (NavMeshQuery.FindPathBounded). <paramref name="complete"/> is false when the path stops
        /// short of the goal; with no navmesh, or either end off it, the path is null and complete false.
        /// </summary>
        public static List<Vector3> FindPathBounded(MapChannel mapChannel, Vector3 start, Vector3 end, out bool complete)
        {
            complete = false;

            return mapChannel?.NavMesh?.FindPathBounded(start, end, out complete);
        }
    }
}
