using System;
using System.Collections.Generic;
using System.Linq;
using DotRecast.Detour;

namespace Rasa.NavMesh
{
    /// <summary>Replace the seven tiles covering the reported escort seams and hospital ramp; retain every other tile and reference.</summary>
    public static class VelonHollowNavMesh
    {
        // Seam 2 crosses x=102.4; seam 3 crosses x=0. Both sides must be rebuilt.
        private static readonly HashSet<(int X, int Z)> Tiles = new()
        {
            (23, 9), (23, 10), (21, 11), (22, 11), (19, 10), (20, 10), (20, 12)
        };

        public static DtNavMesh MergeSeams(DtNavMesh baseline, DtNavMesh rebuilt)
        {
            var original = baseline.GetParams();
            var fine = rebuilt.GetParams();
            if (original.orig != fine.orig || original.tileWidth != fine.tileWidth ||
                original.tileHeight != fine.tileHeight || original.maxTiles != fine.maxTiles ||
                original.maxPolys != fine.maxPolys || Math.Abs(original.tileWidth - 51.2f) > .001f)
                throw new ArgumentException("Velon Hollow seam replacement requires matching 51.2 m tile grids and origins.");
            var replacements = Enumerable.Range(0, rebuilt.GetMaxTiles()).Select(rebuilt.GetTile)
                .Where(t => t?.data?.header != null && Tiles.Contains((t.data.header.x, t.data.header.y)))
                .ToDictionary(t => (t.data.header.x, t.data.header.y, t.data.header.layer), t => t.data);
            var result = new DtNavMesh();
            result.Init(original, 6);
            var replaced = new HashSet<(int, int)>();
            for (var i = 0; i < baseline.GetMaxTiles(); i++)
            {
                var tile = baseline.GetTile(i);
                if (tile?.data?.header == null) continue;
                var header = tile.data.header;
                var data = tile.data;
                if (Tiles.Contains((header.x, header.y)))
                {
                    if (!replacements.TryGetValue((header.x, header.y, header.layer), out data))
                        throw new InvalidOperationException($"Missing rebuilt Velon Hollow tile {header.x},{header.y},{header.layer}.");
                    if (Math.Abs(data.header.walkableHeight - 2f) > .001f ||
                        Math.Abs(data.header.walkableRadius - .6f) > .001f || Math.Abs(data.header.walkableClimb - .9f) > .001f)
                        throw new InvalidOperationException("Velon Hollow seam tiles must retain the original physical agent.");
                    replaced.Add((header.x, header.y));
                }
                if (result.AddTile(data, 0, baseline.GetTileRef(tile), out _).Failed())
                    throw new InvalidOperationException($"Could not preserve Velon Hollow tile {header.x},{header.y},{header.layer}.");
            }
            if (!replaced.SetEquals(Tiles))
                throw new InvalidOperationException("The base navmesh does not contain all seven Velon Hollow escort tiles.");
            return result;
        }
    }
}
