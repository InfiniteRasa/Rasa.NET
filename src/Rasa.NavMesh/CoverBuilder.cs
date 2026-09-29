using System;
using Rasa.ClientData;
using Rasa.Navigation;

namespace Rasa.NavMesh
{
    /// <summary>
    /// A map's <see cref="CoverMesh"/> from the geometry the navmesh is built from: the entity
    /// collision triangles (everything after the terrain's) in a hierarchy, and the terrain as a
    /// height grid at the navmesh's terrain step - the whole heightmap, cliff faces included, since
    /// a hillside hides whoever is behind it however steep it is.
    /// </summary>
    public static class CoverBuilder
    {
        public static CoverMesh Build(MapGeometry geometry, BuildSettings settings)
        {
            var cover = CoverMesh.Build(geometry.Vertices, geometry.Triangles, geometry.TerrainTriangles);
            var terrain = geometry.Terrain;

            if (terrain == null)
                return cover;

            var spacing = terrain.Spacing * Math.Max(1, settings.TerrainStep);
            var columns = Math.Max(1, (int)Math.Ceiling((terrain.BoundsMax.X - terrain.BoundsMin.X) / spacing));
            var rows = Math.Max(1, (int)Math.Ceiling((terrain.BoundsMax.Z - terrain.BoundsMin.Z) / spacing));
            var heights = new float[columns * rows];

            for (var r = 0; r < rows; r++)
                for (var c = 0; c < columns; c++)
                {
                    // The sample at the cell's centre; CoverMesh.TerrainBlocks allows a little
                    // slack for the ground between samples.
                    var h = terrain.Height(terrain.BoundsMin.X + (c + 0.5f) * spacing, terrain.BoundsMin.Z + (r + 0.5f) * spacing);
                    var best = h ?? float.NaN;

                    heights[r * columns + c] = best;
                }

            cover.SetTerrain(terrain.BoundsMin.X, terrain.BoundsMin.Z, spacing, columns, rows, heights);

            return cover;
        }
    }
}
