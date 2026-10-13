using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

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

        /// <summary>Loads a navmesh for every map channel that has one. Runs after MapChannelInit.</summary>
        public void NavMeshInit(string directory)
        {
            Directory = ResolveDirectory(directory, Environment.CurrentDirectory, AppContext.BaseDirectory);
            LoadedMaps = 0;

            if (!System.IO.Directory.Exists(Directory))
            {
                Logger.WriteLog(LogType.Initialize, $"No navmesh folder at {Path.GetFullPath(Directory)}; creatures will move in straight lines");
                return;
            }

            var missing = new List<string>();
            var covers = 0;

            foreach (var mapChannel in MapChannelManager.Instance.MapChannelArray.Values)
            {
                var path = NavMeshFile.PathFor(Directory, mapChannel.MapInfo.MapName);
                var coverPath = CoverMesh.PathFor(Directory, mapChannel.MapInfo.MapName);

                // Cover rides along with the navmesh but stands on its own: a map may have one
                // without the other.
                if (File.Exists(coverPath))
                {
                    try
                    {
                        mapChannel.Cover = CoverMesh.Read(coverPath);
                        covers++;
                    }
                    catch (Exception e)
                    {
                        Logger.WriteLog(LogType.Error, $"Could not load cover {coverPath}: {e.Message}");
                        MapErrorManager.Instance.Record(mapChannel.MapInfo.MapContextId, $"cover {coverPath} could not be loaded: {e.Message}");
                    }
                }

                if (!File.Exists(path))
                {
                    missing.Add(mapChannel.MapInfo.MapName);
                    continue;
                }

                try
                {
                    mapChannel.NavMesh = new NavMeshQuery(NavMeshFile.Read(path));
                    LoadedMaps++;

                    // The floor under the map, for players who fall out of the world (SafetyFloor).
                    if (mapChannel.NavMesh.HeightRange() is var (lowest, highest))
                    {
                        mapChannel.SafetyFloorY = SafetyFloor.FloorFor(lowest);
                        mapChannel.TopWalkableY = highest;
                    }
                }
                catch (Exception e)
                {
                    Logger.WriteLog(LogType.Error, $"Could not load navmesh {path}: {e.Message}");
                    MapErrorManager.Instance.Record(mapChannel.MapInfo.MapContextId, $"navmesh {path} could not be loaded: {e.Message}");
                }
            }

            Logger.WriteLog(LogType.Initialize, $"Loaded navmeshes for {LoadedMaps} of {MapChannelManager.Instance.MapChannelArray.Count} maps from {Path.GetFullPath(Directory)}");
            Logger.WriteLog(LogType.Initialize, covers > 0
                ? $"Loaded cover for {covers} maps"
                : "No cover files (<map>.cover, Rasa.NavMesh --cover-only); ranged attacks ignore cover");

            if (missing.Count > 0 && missing.Count <= 12)
                Logger.WriteLog(LogType.Initialize, $"  no navmesh for: {string.Join(", ", missing)}");
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
        /// The corners to walk through from <paramref name="start"/> to <paramref name="end"/>,
        /// ending as near to <paramref name="end"/> as the mesh allows. Null when the map has no
        /// navmesh or either point is off it: the caller walks straight.
        /// </summary>
        public static List<Vector3> FindPath(MapChannel mapChannel, Vector3 start, Vector3 end)
        {
            return mapChannel?.NavMesh?.FindPath(start, end, out _);
        }
    }
}
