using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Numerics;

namespace Rasa.Navigation
{
    /// <summary>
    /// What stands between two points on a map: the collision triangles of every entity placed on
    /// it (walls, sandbags, rocks, buildings - the same meshes the navmesh is built from) in a
    /// bounding volume hierarchy, and the terrain as a height grid. Built offline by Rasa.NavMesh
    /// next to the navmesh and read by the game server for cover (Managers.Cover).
    ///
    /// <c>&lt;directory&gt;/&lt;map name&gt;.cover</c>, gzip-compressed:
    /// <code>
    /// "RCOV" int32 version
    /// int32 triangleCount, float[triangleCount * 9]      triangle corners, in BVH leaf order
    /// int32 nodeCount, nodeCount * (float[6] min/max, int32 first, int32 count)
    ///                                                     count &gt; 0: a leaf of triangles first..first+count
    ///                                                     count == 0: children at first and first + 1
    /// float originX, originZ, spacing, int32 columns, rows, float[columns * rows]   terrain heights, NaN for none
    /// </code>
    /// </summary>
    public sealed class CoverMesh
    {
        public const string Extension = ".cover";
        private const int Version = 1;
        private const int LeafSize = 4;

        /// <summary>How far the ground has to rise above a ray, between grid samples, to block it.</summary>
        public const float TerrainSlack = 0.25f;

        private float[] _triangles = Array.Empty<float>();
        private Node[] _nodes = Array.Empty<Node>();

        private float _terrainOriginX, _terrainOriginZ, _terrainSpacing;
        private int _terrainColumns, _terrainRows;
        private float[] _terrain = Array.Empty<float>();

        private struct Node
        {
            public Vector3 Min, Max;
            public int First, Count;
        }

        public int TriangleCount => _triangles.Length / 9;
        public int NodeCount => _nodes.Length;
        public bool HasTerrain => _terrain.Length > 0;

        public static string PathFor(string directory, string mapName)
        {
            return Path.Combine(directory, mapName.ToLowerInvariant() + Extension);
        }

        #region Building

        /// <summary>
        /// Builds the hierarchy over the triangles <paramref name="firstTriangle"/> onward of an
        /// indexed triangle list (x, y, z vertex triples). Terrain goes in separately, through
        /// <see cref="SetTerrain"/>: as triangles it would be most of the file for what a height
        /// lookup answers as well.
        /// </summary>
        public static CoverMesh Build(IReadOnlyList<float> vertices, IReadOnlyList<int> indices, int firstTriangle = 0)
        {
            var count = Math.Max(0, indices.Count / 3 - firstTriangle);
            var corners = new Vector3[count * 3];
            var centres = new Vector3[count];
            var order = new int[count];

            for (var t = 0; t < count; t++)
            {
                for (var k = 0; k < 3; k++)
                {
                    var i = indices[(firstTriangle + t) * 3 + k] * 3;
                    corners[t * 3 + k] = new Vector3(vertices[i], vertices[i + 1], vertices[i + 2]);
                }

                centres[t] = (corners[t * 3] + corners[t * 3 + 1] + corners[t * 3 + 2]) / 3f;
                order[t] = t;
            }

            var nodes = new List<Node>();

            if (count > 0)
            {
                nodes.Add(default);
                Split(nodes, 0, order, 0, count, corners, centres);
            }

            var mesh = new CoverMesh { _nodes = nodes.ToArray(), _triangles = new float[count * 9] };

            for (var t = 0; t < count; t++)
                for (var k = 0; k < 3; k++)
                {
                    var p = corners[order[t] * 3 + k];
                    mesh._triangles[t * 9 + k * 3] = p.X;
                    mesh._triangles[t * 9 + k * 3 + 1] = p.Y;
                    mesh._triangles[t * 9 + k * 3 + 2] = p.Z;
                }

            return mesh;
        }

        private static void Split(List<Node> nodes, int index, int[] order, int first, int count, Vector3[] corners, Vector3[] centres)
        {
            var min = new Vector3(float.MaxValue);
            var max = new Vector3(float.MinValue);
            var cmin = new Vector3(float.MaxValue);
            var cmax = new Vector3(float.MinValue);

            for (var i = first; i < first + count; i++)
            {
                var t = order[i];

                for (var k = 0; k < 3; k++)
                {
                    min = Vector3.Min(min, corners[t * 3 + k]);
                    max = Vector3.Max(max, corners[t * 3 + k]);
                }

                cmin = Vector3.Min(cmin, centres[t]);
                cmax = Vector3.Max(cmax, centres[t]);
            }

            if (count <= LeafSize)
            {
                nodes[index] = new Node { Min = min, Max = max, First = first, Count = count };
                return;
            }

            // Split along the longest axis of the centres, at the median.
            var extent = cmax - cmin;
            var axis = extent.X >= extent.Y && extent.X >= extent.Z ? 0 : extent.Y >= extent.Z ? 1 : 2;

            Array.Sort(order, first, count, Comparer<int>.Create((a, b) => Axis(centres[a], axis).CompareTo(Axis(centres[b], axis))));

            var half = count / 2;
            var left = nodes.Count;

            nodes.Add(default);
            nodes.Add(default);
            nodes[index] = new Node { Min = min, Max = max, First = left, Count = 0 };

            Split(nodes, left, order, first, half, corners, centres);
            Split(nodes, left + 1, order, first + half, count - half, corners, centres);
        }

        private static float Axis(Vector3 v, int axis) => axis == 0 ? v.X : axis == 1 ? v.Y : v.Z;

        /// <summary>The terrain, as heights on a grid from (originX, originZ) every spacing metres; NaN where there is none.</summary>
        public void SetTerrain(float originX, float originZ, float spacing, int columns, int rows, float[] heights)
        {
            if (heights == null || heights.Length != columns * rows || spacing <= 0)
                throw new ArgumentException("terrain grid does not match its size");

            _terrainOriginX = originX;
            _terrainOriginZ = originZ;
            _terrainSpacing = spacing;
            _terrainColumns = columns;
            _terrainRows = rows;
            _terrain = heights;
        }

        #endregion

        #region Queries

        /// <summary>The terrain height under a point, or null off the grid.</summary>
        public float? TerrainHeight(float x, float z)
        {
            if (_terrain.Length == 0)
                return null;

            var c = (int)MathF.Floor((x - _terrainOriginX) / _terrainSpacing);
            var r = (int)MathF.Floor((z - _terrainOriginZ) / _terrainSpacing);

            if (c < 0 || r < 0 || c >= _terrainColumns || r >= _terrainRows)
                return null;

            var h = _terrain[r * _terrainColumns + c];

            return float.IsNaN(h) ? (float?)null : h;
        }

        /// <summary>Whether anything - an entity's collision or the ground - lies on the segment between the two points.</summary>
        public bool Blocked(Vector3 from, Vector3 to)
        {
            return TerrainBlocks(from, to) || GeometryBlocks(from, to);
        }

        /// <summary>
        /// Whether the ground rises through the segment: sampled every half grid step, the ends
        /// themselves excepted, which are where two actors stand.
        /// </summary>
        public bool TerrainBlocks(Vector3 from, Vector3 to)
        {
            if (_terrain.Length == 0)
                return false;

            var length = new Vector2(to.X - from.X, to.Z - from.Z).Length();
            var steps = Math.Max(1, (int)(length / (_terrainSpacing * 0.5f)));

            for (var i = 1; i < steps; i++)
            {
                var p = Vector3.Lerp(from, to, i / (float)steps);
                var ground = TerrainHeight(p.X, p.Z);

                if (ground.HasValue && ground.Value > p.Y + TerrainSlack)
                    return true;
            }

            return false;
        }

        /// <summary>Whether a collision triangle crosses the segment.</summary>
        public bool GeometryBlocks(Vector3 from, Vector3 to)
        {
            if (_nodes.Length == 0)
                return false;

            var dir = to - from;
            var length = dir.Length();

            if (length < 1e-4f)
                return false;

            dir /= length;

            var inv = new Vector3(1f / NonZero(dir.X), 1f / NonZero(dir.Y), 1f / NonZero(dir.Z));
            Span<int> stack = stackalloc int[64];
            var top = 0;

            stack[top++] = 0;

            while (top > 0)
            {
                ref var node = ref _nodes[stack[--top]];

                if (!HitsBox(from, inv, length, node.Min, node.Max))
                    continue;

                if (node.Count > 0)
                {
                    for (var t = node.First; t < node.First + node.Count; t++)
                        if (HitsTriangle(from, dir, length, t))
                            return true;
                }
                else if (top + 2 <= stack.Length)
                {
                    stack[top++] = node.First;
                    stack[top++] = node.First + 1;
                }
            }

            return false;
        }

        private static float NonZero(float v) => MathF.Abs(v) < 1e-9f ? (v < 0 ? -1e-9f : 1e-9f) : v;

        private static bool HitsBox(Vector3 origin, Vector3 inv, float length, Vector3 min, Vector3 max)
        {
            var t1 = (min - origin) * inv;
            var t2 = (max - origin) * inv;
            var tmin = Vector3.Min(t1, t2);
            var tmax = Vector3.Max(t1, t2);
            var enter = MathF.Max(MathF.Max(tmin.X, tmin.Y), MathF.Max(tmin.Z, 0f));
            var exit = MathF.Min(MathF.Min(tmax.X, tmax.Y), MathF.Min(tmax.Z, length));

            return enter <= exit;
        }

        // Möller-Trumbore, both faces.
        private bool HitsTriangle(Vector3 origin, Vector3 dir, float length, int t)
        {
            var i = t * 9;
            var a = new Vector3(_triangles[i], _triangles[i + 1], _triangles[i + 2]);
            var b = new Vector3(_triangles[i + 3], _triangles[i + 4], _triangles[i + 5]);
            var c = new Vector3(_triangles[i + 6], _triangles[i + 7], _triangles[i + 8]);
            var e1 = b - a;
            var e2 = c - a;
            var p = Vector3.Cross(dir, e2);
            var det = Vector3.Dot(e1, p);

            if (MathF.Abs(det) < 1e-8f)
                return false;

            var invDet = 1f / det;
            var s = origin - a;
            var u = Vector3.Dot(s, p) * invDet;

            if (u < 0f || u > 1f)
                return false;

            var q = Vector3.Cross(s, e1);
            var v = Vector3.Dot(dir, q) * invDet;

            if (v < 0f || u + v > 1f)
                return false;

            var distance = Vector3.Dot(e2, q) * invDet;

            return distance > 1e-3f && distance < length - 1e-3f;
        }

        #endregion

        #region File

        public void Write(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));

            using var file = File.Create(path);
            using var gzip = new GZipStream(file, CompressionLevel.Optimal);
            using var w = new BinaryWriter(gzip);

            w.Write((byte)'R'); w.Write((byte)'C'); w.Write((byte)'O'); w.Write((byte)'V');
            w.Write(Version);

            w.Write(TriangleCount);
            foreach (var f in _triangles)
                w.Write(f);

            w.Write(_nodes.Length);
            foreach (var n in _nodes)
            {
                w.Write(n.Min.X); w.Write(n.Min.Y); w.Write(n.Min.Z);
                w.Write(n.Max.X); w.Write(n.Max.Y); w.Write(n.Max.Z);
                w.Write(n.First); w.Write(n.Count);
            }

            w.Write(_terrainOriginX); w.Write(_terrainOriginZ); w.Write(_terrainSpacing);
            w.Write(_terrainColumns); w.Write(_terrainRows);
            foreach (var h in _terrain)
                w.Write(h);
        }

        public static CoverMesh Read(string path)
        {
            using var file = File.OpenRead(path);
            using var gzip = new GZipStream(file, CompressionMode.Decompress);
            using var r = new BinaryReader(gzip);

            if (r.ReadByte() != 'R' || r.ReadByte() != 'C' || r.ReadByte() != 'O' || r.ReadByte() != 'V')
                throw new InvalidDataException($"{path} is not a cover file");

            var version = r.ReadInt32();

            if (version != Version)
                throw new InvalidDataException($"{path} is cover version {version}; this server reads {Version}");

            var mesh = new CoverMesh();

            mesh._triangles = new float[r.ReadInt32() * 9];
            for (var i = 0; i < mesh._triangles.Length; i++)
                mesh._triangles[i] = r.ReadSingle();

            mesh._nodes = new Node[r.ReadInt32()];
            for (var i = 0; i < mesh._nodes.Length; i++)
                mesh._nodes[i] = new Node
                {
                    Min = new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle()),
                    Max = new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle()),
                    First = r.ReadInt32(),
                    Count = r.ReadInt32()
                };

            mesh._terrainOriginX = r.ReadSingle();
            mesh._terrainOriginZ = r.ReadSingle();
            mesh._terrainSpacing = r.ReadSingle();
            mesh._terrainColumns = r.ReadInt32();
            mesh._terrainRows = r.ReadInt32();
            mesh._terrain = new float[mesh._terrainColumns * mesh._terrainRows];
            for (var i = 0; i < mesh._terrain.Length; i++)
                mesh._terrain[i] = r.ReadSingle();

            return mesh;
        }

        #endregion
    }
}
