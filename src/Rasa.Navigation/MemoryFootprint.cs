using System;

namespace Rasa.Navigation
{
    using DotRecast.Detour;

    /// <summary>
    /// How many bytes of managed heap a map's navigation holds, worked out from what it holds
    /// rather than measured: cheap enough to run for every map at startup, with no profiler.
    ///
    /// Sizes are 64-bit .NET's: an object is a 16-byte header (sync block and method table)
    /// plus its fields, padded to 8; an array adds a length, so starts at 24. The per-object
    /// sizes below were checked against a heap dump of a running server (October 2026,
    /// DotRecast 2026.3.1): DtMeshTile 56, DtMeshData 80, DtMeshHeader 120, DtPoly 56,
    /// DtLink 32, DtBVNode 48. Small fixed parts (the DtNavMesh itself, its tile lookup) are
    /// left out, so the totals are a little low, not high.
    /// </summary>
    public static class MemoryFootprint
    {
        private const int ArrayHeader = 24;
        private const int Reference = 8;

        private const int MeshTileBytes = 56;
        private const int MeshDataBytes = 80;
        private const int MeshHeaderBytes = 120;
        private const int PolyBytes = 56;
        private const int LinkBytes = 32;
        private const int BvNodeBytes = 48;
        private const int PolyDetailBytes = 12; // a struct: two ints and two bytes, padded
        private const int OffMeshConnectionBytes = 48;

        /// <summary>An array of <paramref name="length"/> elements of <paramref name="elementSize"/> bytes.</summary>
        public static long ArrayBytes(long length, int elementSize)
        {
            return Align(ArrayHeader + length * elementSize);
        }

        private static long Align(long bytes) => (bytes + 7) & ~7L;

        private static long Of(float[] array) => array == null ? 0 : ArrayBytes(array.Length, sizeof(float));
        private static long Of(int[] array) => array == null ? 0 : ArrayBytes(array.Length, sizeof(int));

        /// <summary>What one map's navmesh holds, and roughly how many bytes that is.</summary>
        public readonly struct NavMeshFootprint : IEquatable<NavMeshFootprint>
        {
            public int Tiles { get; }
            public long Polys { get; }
            public long Links { get; }
            public long BvNodes { get; }
            public long Bytes { get; }

            public NavMeshFootprint(int tiles, long polys, long links, long bvNodes, long bytes)
            {
                Tiles = tiles;
                Polys = polys;
                Links = links;
                BvNodes = bvNodes;
                Bytes = bytes;
            }

            public static NavMeshFootprint operator +(NavMeshFootprint a, NavMeshFootprint b) =>
                new NavMeshFootprint(a.Tiles + b.Tiles, a.Polys + b.Polys, a.Links + b.Links, a.BvNodes + b.BvNodes, a.Bytes + b.Bytes);

            public bool Equals(NavMeshFootprint other) =>
                Tiles == other.Tiles && Polys == other.Polys && Links == other.Links && BvNodes == other.BvNodes && Bytes == other.Bytes;

            public override bool Equals(object obj) => obj is NavMeshFootprint other && Equals(other);

            public override int GetHashCode() => HashCode.Combine(Tiles, Polys, Links, BvNodes, Bytes);
        }

        public static NavMeshFootprint Of(DtNavMesh navMesh)
        {
            if (navMesh == null)
                return default;

            var tiles = 0;
            long polys = 0, links = 0, bvNodes = 0, bytes = 0;

            for (var i = 0; i < navMesh.GetMaxTiles(); i++)
            {
                var tile = navMesh.GetTile(i);

                if (tile == null)
                    continue;

                // Every slot up to the maximum is a tile object, with data or not.
                bytes += MeshTileBytes;

                if (tile.links != null)
                {
                    bytes += ArrayBytes(tile.links.Length, Reference);

                    foreach (var link in tile.links)
                        if (link != null)
                            links++;
                }

                var data = tile.data;

                if (data == null)
                    continue;

                tiles++;
                bytes += MeshDataBytes + (data.header != null ? MeshHeaderBytes : 0);

                if (data.polys != null)
                {
                    bytes += ArrayBytes(data.polys.Length, Reference);

                    foreach (var poly in data.polys)
                    {
                        if (poly == null)
                            continue;

                        polys++;
                        bytes += PolyBytes + Of(poly.verts) + Of(poly.neis);
                    }
                }

                bytes += Of(data.verts) + Of(data.detailVerts) + Of(data.detailTris);

                if (data.detailMeshes != null)
                    bytes += ArrayBytes(data.detailMeshes.Length, PolyDetailBytes);

                if (data.bvTree != null)
                {
                    bytes += ArrayBytes(data.bvTree.Length, Reference);

                    foreach (var node in data.bvTree)
                        if (node != null)
                            bvNodes++;
                }

                if (data.offMeshCons != null)
                {
                    bytes += ArrayBytes(data.offMeshCons.Length, Reference);

                    foreach (var connection in data.offMeshCons)
                        if (connection != null)
                            bytes += OffMeshConnectionBytes + (connection.pos != null ? ArrayBytes(connection.pos.Length, 3 * sizeof(float)) : 0);
                }
            }

            // Links and BV nodes are counted above; they are objects of their own.
            bytes += links * LinkBytes + bvNodes * BvNodeBytes;

            return new NavMeshFootprint(tiles, polys, links, bvNodes, bytes);
        }

        /// <summary>A byte count for the log: "1.24 GB", "583.1 MB", "12.0 KB".</summary>
        public static string Format(long bytes)
        {
            const double KB = 1024, MB = KB * 1024, GB = MB * 1024;

            return bytes >= GB ? $"{bytes / GB:0.00} GB"
                : bytes >= MB ? $"{bytes / MB:0.0} MB"
                : $"{bytes / KB:0.0} KB";
        }
    }
}
