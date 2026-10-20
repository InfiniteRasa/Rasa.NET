using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using DotRecast.Core;
using DotRecast.Core.Numerics;
using DotRecast.Detour;

namespace Rasa.Navigation
{
    /// <summary>
    /// The handful of Detour queries the game needs, on one map's navmesh: a walkable path between
    /// two points, a random walkable point near another, the ground under a point, and whether a
    /// straight walk is clear. Positions are game coordinates (metres, y up), the same frame the
    /// navmesh was built in.
    ///
    /// <see cref="DtNavMeshQuery"/> is not thread safe; each map worker uses its own instance.
    /// </summary>
    public sealed class NavMeshQuery
    {
        /// <summary>How far from a point to look for the polygon under it: a few metres sideways, well above and below.</summary>
        public static readonly RcVec3f SearchExtents = new RcVec3f(4f, 8f, 4f);

        private const int MaxPolys = 4096;
        private const int MaxStraightPath = 256;

        /// <summary>
        /// How many nodes <see cref="FindPathBounded"/> searches for a goal no route reaches. A
        /// search for one walks every polygon of the start's region before it gives up: up to 30,554
        /// on Wilderness, 49,385 on the Divide. A route that arrives does not need it - one of up to
        /// 60 m took at most 714 nodes on the four largest maps - and a goal in the start's own
        /// region is searched in full anyway, so this is only how far a creature looks for the
        /// nearest it can get to one it cannot reach. Ours: three times the most a route of up to
        /// 150 m took there (1,432, on the Divide).
        /// </summary>
        public const int UnreachableSearchNodes = 4096;

        /// <summary>The walkable regions of each mesh (<see cref="Regions"/>), worked out once and shared by every query on it.</summary>
        private static readonly ConditionalWeakTable<DtNavMesh, int[][]> RegionsOf = new();

        private readonly DtNavMesh _navMesh;
        private readonly DtNavMeshQuery _query;
        private readonly IDtQueryFilter _filter = new DtQueryDefaultFilter();
        private readonly IRcRand _random = new RcRand();
        private readonly long[] _polys = new long[MaxPolys];
        private readonly DtStraightPath[] _straight = new DtStraightPath[MaxStraightPath];

        public NavMeshQuery(DtNavMesh navMesh)
        {
            _navMesh = navMesh;
            _query = new DtNavMeshQuery(navMesh);
        }

        public DtNavMesh NavMesh => _navMesh;

        /// <summary>How many nodes the last path search visited (FindPath, FindPathBounded): what it cost.</summary>
        public int LastSearchNodes { get; private set; }

        /// <summary>How many path searches this query has run.</summary>
        public long Searches { get; private set; }

        /// <summary>
        /// How low and how high the map goes, as far as the navmesh knows. Lowest is the lowest
        /// of any walkable vertex and of the tiles' bounds, which are the bounds of all the
        /// geometry the mesh was built from, walkable or not; highest is the highest walkable
        /// vertex. Null for a mesh with no tiles.
        /// </summary>
        public (float Lowest, float HighestWalkable)? HeightRange()
        {
            var lowest = float.MaxValue;
            var highest = float.MinValue;

            for (var i = 0; i < _navMesh.GetMaxTiles(); i++)
            {
                var data = _navMesh.GetTile(i)?.data;

                if (data?.header == null)
                    continue;

                lowest = Math.Min(lowest, data.header.bmin.Y);

                for (var k = 1; k < data.header.vertCount * 3; k += 3)
                {
                    lowest = Math.Min(lowest, data.verts[k]);
                    highest = Math.Max(highest, data.verts[k]);
                }
            }

            return lowest <= highest ? (lowest, highest) : null;
        }

        /// <summary>The nearest walkable point to <paramref name="position"/>, or null when nothing is within the search extents.</summary>
        public Vector3? Nearest(Vector3 position)
        {
            return FindPoly(position, out var point) != 0 ? ToVector(point) : (Vector3?)null;
        }

        /// <summary>
        /// The walkable point nearest to <paramref name="position"/> anywhere in the column above
        /// and below it, up to <paramref name="height"/> each way: for a point whose height is only
        /// a guess - a map label, the average height of a camp's props - where the ordinary query's
        /// 8 m either way finds nothing. Of several floors in the column, the nearest in 3D wins,
        /// so a guess near the surface lands on the surface and not in the cave under it.
        /// </summary>
        public Vector3? NearestInColumn(Vector3 position, float height)
        {
            var extents = new RcVec3f(SearchExtents.X, height, SearchExtents.Z);
            var status = _query.FindNearestPoly(ToRc(position), extents, _filter, out var poly, out var point, out _);

            return status.Succeeded() && poly != 0 ? ToVector(point) : (Vector3?)null;
        }

        /// <summary>Whether there is walkable surface within the search extents of the point.</summary>
        public bool IsOnMesh(Vector3 position)
        {
            return FindPoly(position, out _) != 0;
        }

        /// <summary>
        /// Whether the walkable surface nearest the point was built under the terrain heightmap
        /// (<see cref="NavMeshFlags.Underground"/>): inside a cave or tunnel rather than on the
        /// surface above it. False off the mesh, and everywhere on a mesh built without the flag.
        /// </summary>
        public bool IsUnderground(Vector3 position)
        {
            var poly = FindPoly(position, out _);

            if (poly == 0)
                return false;

            return _navMesh.GetTileAndPolyByRef(poly, out _, out var p).Succeeded() && (p.flags & NavMeshFlags.Underground) != 0;
        }

        /// <summary>The height of the walkable surface under (x, z) nearest to the point's y, or null.</summary>
        public float? GroundHeight(Vector3 position)
        {
            var poly = FindPoly(position, out var point);

            if (poly == 0)
                return null;

            return _query.GetPolyHeight(poly, point, out var height).Succeeded() ? height : point.Y;
        }

        /// <summary>
        /// A walkable route from <paramref name="start"/> to <paramref name="end"/>: the corner points
        /// to walk through in order, ending at the reachable point nearest <paramref name="end"/>.
        /// The start itself is not included. Null when either point is off the mesh.
        /// <paramref name="complete"/> is false when the end could not be reached and the path stops short.
        /// </summary>
        public List<Vector3> FindPath(Vector3 start, Vector3 end, out bool complete)
        {
            complete = false;

            var startRef = FindPoly(start, out var startPoint);
            var endRef = FindPoly(end, out var endPoint);

            if (startRef == 0 || endRef == 0)
                return null;

            var status = _query.FindPath(startRef, endRef, startPoint, endPoint, _filter, _polys, out var polyCount, MaxPolys);
            Searched();

            return Route(status, polyCount, endRef, startPoint, endPoint, out complete);
        }

        /// <summary>
        /// As <see cref="FindPath"/>, but a goal no route reaches - in another walkable region of the
        /// mesh than the start (<see cref="Reachable"/>) - is searched for no further than
        /// <paramref name="maxNodes"/> nodes, and the path goes as near the goal as those got. A goal
        /// that can be reached is searched in full, as FindPath does, and gets the same route. For a
        /// creature chasing what it may not be able to reach.
        /// </summary>
        public List<Vector3> FindPathBounded(Vector3 start, Vector3 end, out bool complete, int maxNodes = UnreachableSearchNodes)
        {
            complete = false;

            var startRef = FindPoly(start, out var startPoint);
            var endRef = FindPoly(end, out var endPoint);

            if (startRef == 0 || endRef == 0)
                return null;

            if (RegionOf(startRef) == RegionOf(endRef))
                return FindPath(start, end, out complete);

            // The sliced search, which can be stopped. It is not used for a route that arrives: it
            // does not always choose the one the whole search does, and now and then a much longer.
            var status = _query.InitSlicedFindPath(startRef, endRef, startPoint, endPoint, _filter, 0);

            if (!status.Failed())
                status = _query.UpdateSlicedFindPath(Math.Max(1, maxNodes), out _);

            var polyCount = 0;

            if (!status.Failed())
                status = _query.FinalizeSlicedFindPath(_polys, out polyCount, MaxPolys);

            Searched();

            return Route(status, polyCount, endRef, startPoint, endPoint, out complete);
        }

        /// <summary>
        /// Whether a walkable route joins <paramref name="start"/> and <paramref name="end"/>: whether
        /// they are in the same walkable region of the mesh, known at once (<see cref="Regions"/>).
        /// Null when either is off the mesh.
        /// </summary>
        public bool? Reachable(Vector3 start, Vector3 end)
        {
            var startRef = FindPoly(start, out _);
            var endRef = FindPoly(end, out _);

            if (startRef == 0 || endRef == 0)
                return null;

            return RegionOf(startRef) == RegionOf(endRef);
        }

        private void Searched()
        {
            Searches++;
            LastSearchNodes = _query.GetNodePool().GetNodeCount();
        }

        /// <summary>The corners of the corridor a search found, ending as near the goal as it got; null for none.</summary>
        private List<Vector3> Route(DtStatus status, int polyCount, long endRef, RcVec3f startPoint, RcVec3f endPoint, out bool complete)
        {
            complete = false;

            if (status.Failed() || polyCount == 0)
                return null;

            // A partial path ends on the last polygon found; walk to the point on it nearest the goal.
            var target = endPoint;

            if (_polys[polyCount - 1] != endRef)
                _query.ClosestPointOnPoly(_polys[polyCount - 1], endPoint, out target, out _);
            else
                complete = true;

            status = _query.FindStraightPath(startPoint, target, _polys, polyCount, _straight, out var straightCount, MaxStraightPath, 0);

            if (status.Failed() || straightCount == 0)
                return null;

            var result = new List<Vector3>(straightCount);

            // straight[0] is the start position itself.
            for (var i = 1; i < straightCount; i++)
                result.Add(ToVector(_straight[i].pos));

            if (result.Count == 0)
                result.Add(ToVector(target));

            return result;
        }

        /// <summary>
        /// Whether there is a walkable route from <paramref name="start"/> to <paramref name="end"/>:
        /// true or false, or null when either is off the mesh or the search ran out of room before
        /// it could tell.
        /// </summary>
        public bool? Connected(Vector3 start, Vector3 end)
        {
            var startRef = FindPoly(start, out var startPoint);
            var endRef = FindPoly(end, out var endPoint);

            if (startRef == 0 || endRef == 0)
                return null;

            var status = _query.FindPath(startRef, endRef, startPoint, endPoint, _filter, _polys, out var polyCount, MaxPolys);

            if (status.Failed() || polyCount == 0 || status.Has(DtStatus.DT_OUT_OF_NODES))
                return null;

            return _polys[polyCount - 1] == endRef;
        }

        /// <summary>
        /// The walkable regions of the mesh: each polygon the default filter lets a search through,
        /// numbered by the set of them joined to one another by the mesh's links, and -1 for one it
        /// does not. A route joins two polygons exactly when they have the same number. Worked out
        /// once per mesh, by walking its links (tens of milliseconds for the largest), and indexed by
        /// tile and polygon.
        /// </summary>
        private int[][] Regions => RegionsOf.GetValue(_navMesh, Number);

        private int RegionOf(long polyRef)
        {
            DtDetour.DecodePolyId(polyRef, out _, out var tile, out var poly);
            var regions = Regions;

            return tile < regions.Length && regions[tile] != null && poly < regions[tile].Length ? regions[tile][poly] : -1;
        }

        private static int[][] Number(DtNavMesh navMesh)
        {
            var filter = new DtQueryDefaultFilter();
            var regions = new int[navMesh.GetMaxTiles()][];

            for (var i = 0; i < regions.Length; i++)
            {
                var data = navMesh.GetTile(i)?.data;

                if (data?.header != null)
                    regions[i] = new int[data.header.polyCount];
            }

            var region = 0;
            var open = new Stack<long>();

            for (var i = 0; i < regions.Length; i++)
            {
                var tile = navMesh.GetTile(i);

                if (regions[i] == null)
                    continue;

                var baseRef = navMesh.GetPolyRefBase(tile);

                for (var j = 0; j < regions[i].Length; j++)
                {
                    if (regions[i][j] != 0)
                        continue;

                    var first = baseRef | (long)j;

                    if (!filter.PassFilter(first, tile, tile.data.polys[j]))
                    {
                        regions[i][j] = -1;
                        continue;
                    }

                    regions[i][j] = ++region;
                    open.Push(first);

                    while (open.Count > 0)
                    {
                        navMesh.GetTileAndPolyByRefUnsafe(open.Pop(), out var currentTile, out var current);

                        for (var k = current.firstLink; k != DtDetour.DT_NULL_LINK; k = currentTile.links[k].next)
                        {
                            var next = currentTile.links[k].refs;

                            if (next == 0)
                                continue;

                            DtDetour.DecodePolyId(next, out _, out var nextTileIndex, out var nextPolyIndex);

                            if (regions[nextTileIndex] == null || regions[nextTileIndex][nextPolyIndex] != 0)
                                continue;

                            navMesh.GetTileAndPolyByRefUnsafe(next, out var nextTile, out var nextPoly);

                            if (!filter.PassFilter(next, nextTile, nextPoly))
                            {
                                regions[nextTileIndex][nextPolyIndex] = -1;
                                continue;
                            }

                            regions[nextTileIndex][nextPolyIndex] = region;
                            open.Push(next);
                        }
                    }
                }
            }

            return regions;
        }

        /// <summary>A random walkable point within <paramref name="radius"/> of <paramref name="centre"/>, or null.</summary>
        public Vector3? RandomPointAround(Vector3 centre, float radius)
        {
            var centreRef = FindPoly(centre, out var centrePoint);

            if (centreRef == 0)
                return null;

            var status = _query.FindRandomPointAroundCircle(centreRef, centrePoint, radius, _filter, _random, out _, out var point);

            return status.Succeeded() ? ToVector(point) : (Vector3?)null;
        }

        /// <summary>
        /// Whether a straight walk from <paramref name="start"/> to <paramref name="end"/> stays on the
        /// mesh (no wall, no drop). False when the start is off the mesh.
        /// </summary>
        public bool IsWalkClear(Vector3 start, Vector3 end)
        {
            var startRef = FindPoly(start, out var startPoint);

            if (startRef == 0)
                return false;

            var status = _query.Raycast(startRef, startPoint, ToRc(end), _filter, out var t, out _, _polys, out _, MaxPolys);

            return status.Succeeded() && t >= float.MaxValue; // t == MAX means nothing was hit
        }

        /// <summary>
        /// Where a straight walk from <paramref name="start"/> toward <paramref name="end"/> over the
        /// mesh arrives: the point over (x, z) of the end on the polygon the walk ends on, at its
        /// height - not whatever other floor lies nearest the end, above or below. Null when the
        /// walk is stopped (a wall, a drop) or the start is off the mesh.
        /// </summary>
        public Vector3? WalkTo(Vector3 start, Vector3 end)
        {
            var startRef = FindPoly(start, out var startPoint);

            if (startRef == 0)
                return null;

            var target = ToRc(end);
            var status = _query.Raycast(startRef, startPoint, target, _filter, out var t, out _, _polys, out var count, MaxPolys);

            if (status.Failed() || t < float.MaxValue || count == 0)
                return null;

            return _query.ClosestPointOnPoly(_polys[count - 1], target, out var point, out _).Succeeded()
                ? ToVector(point)
                : (Vector3?)null;
        }

        // The netstandard build of DotRecast has no Vector3 conversions.
        private static RcVec3f ToRc(Vector3 v) => new RcVec3f(v.X, v.Y, v.Z);
        private static Vector3 ToVector(RcVec3f v) => new Vector3(v.X, v.Y, v.Z);

        private long FindPoly(Vector3 position, out RcVec3f point)
        {
            var status = _query.FindNearestPoly(ToRc(position), SearchExtents, _filter, out var poly, out point, out _);

            return status.Succeeded() ? poly : 0;
        }
    }
}
