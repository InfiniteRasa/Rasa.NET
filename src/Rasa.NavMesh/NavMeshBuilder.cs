using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotRecast.Core.Numerics;
using DotRecast.Detour;
using DotRecast.Recast;
using DotRecast.Recast.Geom;
using Rasa.ClientData;
using Rasa.Navigation;

namespace Rasa.NavMesh
{
    /// <summary>
    /// Turns a map's triangle soup into a tiled Detour navmesh, the way recastnavigation's tiled
    /// sample does it: Recast rasterizes each tile (heightfield, regions, contours, poly mesh, detail
    /// mesh), Detour packs the tile, all tiles go into one <see cref="DtNavMesh"/>.
    /// </summary>
    public static class NavMeshBuilder
    {
        // Area values written into the polygons; the flags are NavMeshFlags. The server's query
        // filter includes everything, so neither restricts pathing.
        public const int AreaGround = 0;
        public const int AreaUnderground = 1;
        public const int AreaWalkableInput = 0x3f;

        public static DtNavMesh Build(MapGeometry geometry, BuildSettings s, Action<string> log)
        {
            var geom = new RcSampleInputGeomProvider(geometry.Vertices.ToArray(), geometry.Triangles.ToArray());

            var cfg = new RcConfig(
                true, s.TileSize, s.TileSize,
                RcConfig.CalcBorder(s.AgentRadius, s.CellSize),
                RcPartition.WATERSHED,
                s.CellSize, s.CellHeight,
                s.AgentMaxSlope, s.AgentHeight, s.AgentRadius, s.AgentMaxClimb,
                s.RegionMinSize * s.RegionMinSize * s.CellSize * s.CellSize,
                s.RegionMergeSize * s.RegionMergeSize * s.CellSize * s.CellSize,
                s.EdgeMaxLength, s.EdgeMaxError,
                s.VertsPerPoly,
                s.DetailSampleDistance, s.DetailSampleMaxError,
                true, true, true,
                new RcAreaModification(AreaWalkableInput), true);

            var bmin = geom.GetMeshBoundsMin();
            var bmax = geom.GetMeshBoundsMax();
            RcRecast.CalcGridSize(bmin, bmax, s.CellSize, out var gridW, out var gridH);
            var tilesX = (gridW + s.TileSize - 1) / s.TileSize;
            var tilesZ = (gridH + s.TileSize - 1) / s.TileSize;
            log($"  grid {gridW} x {gridH} cells, {tilesX} x {tilesZ} tiles of {s.TileSize * s.CellSize:0.#} m");

            // Recast's detail-mesh step writes "delaunayHull: Removing dangling face" to stderr
            // for every triangle its per-polygon height triangulation could not close - a known
            // and harmless recastnavigation message (the polygon keeps its walkable area, the
            // height detail in that corner is a little coarser). On a 2 km map it is thousands
            // of lines, so both streams are captured for the duration and summarized.
            var stdout = Console.Out;
            var stderr = Console.Error;
            var captured = new CountingWriter();
            Console.SetOut(captured);
            Console.SetError(captured);
            List<RcBuilderResult> results;

            try
            {
                results = new RcBuilder().BuildTiles(geom, cfg, false, true, Math.Max(1, s.Threads), Task.Factory);
            }
            finally
            {
                Console.SetOut(stdout);
                Console.SetError(stderr);
            }

            if (captured.DanglingFaces > 0)
                log($"  {captured.DanglingFaces} dangling detail-mesh faces dropped by Recast (harmless)");

            foreach (var line in captured.Other.Take(5))
                log($"  recast: {line}");

            var tileBits = Math.Min(DtUtils.Ilog2(DtUtils.NextPow2(tilesX * tilesZ)), 14);
            var navMeshParams = new DtNavMeshParams
            {
                orig = bmin,
                tileWidth = s.TileSize * s.CellSize,
                tileHeight = s.TileSize * s.CellSize,
                maxTiles = 1 << tileBits,
                maxPolys = 1 << (22 - tileBits)
            };

            var navMesh = new DtNavMesh();
            navMesh.Init(navMeshParams, s.VertsPerPoly);

            var added = 0;
            var polys = 0;
            var underground = 0;

            foreach (var result in results)
            {
                var pmesh = result.Mesh;

                if (pmesh == null || pmesh.npolys == 0)
                    continue;

                for (var i = 0; i < pmesh.npolys; i++)
                {
                    pmesh.areas[i] = AreaGround;
                    pmesh.flags[i] = NavMeshFlags.Walk;

                    if (geometry.Terrain != null && IsUnderTerrain(pmesh, i, geometry.Terrain))
                    {
                        pmesh.areas[i] = AreaUnderground;
                        pmesh.flags[i] |= NavMeshFlags.Underground;
                        underground++;
                    }
                }

                var option = new DtNavMeshCreateParams
                {
                    verts = pmesh.verts,
                    vertCount = pmesh.nverts,
                    polys = pmesh.polys,
                    polyAreas = pmesh.areas,
                    polyFlags = pmesh.flags,
                    polyCount = pmesh.npolys,
                    nvp = pmesh.nvp,
                    walkableHeight = s.AgentHeight,
                    walkableRadius = s.AgentRadius,
                    walkableClimb = s.AgentMaxClimb,
                    bmin = pmesh.bmin,
                    bmax = pmesh.bmax,
                    cs = s.CellSize,
                    ch = s.CellHeight,
                    buildBvTree = true,
                    tileX = result.TileX,
                    tileZ = result.TileZ,
                    offMeshConCount = 0,
                    offMeshConVerts = Array.Empty<float>(),
                    offMeshConRad = Array.Empty<float>(),
                    offMeshConDir = Array.Empty<int>(),
                    offMeshConAreas = Array.Empty<int>(),
                    offMeshConFlags = Array.Empty<int>(),
                    offMeshConUserID = Array.Empty<int>()
                };

                var dmesh = result.MeshDetail;

                if (dmesh != null)
                {
                    option.detailMeshes = dmesh.meshes;
                    option.detailVerts = dmesh.verts;
                    option.detailVertsCount = dmesh.nverts;
                    option.detailTris = dmesh.tris;
                    option.detailTriCount = dmesh.ntris;
                }

                var data = DtNavMeshBuilder.CreateNavMeshData(option);

                if (data == null)
                    continue;

                if (navMesh.AddTile(data, 0, 0, out _).Succeeded())
                {
                    added++;
                    polys += pmesh.npolys;
                }
            }

            log($"  {added} tiles with walkable surface, {polys} polygons, {underground} of them under the terrain");

            return navMesh;
        }

        /// <summary>
        /// Whether most of the polygon's vertices are more than <see cref="NavMeshFlags.UndergroundDepth"/>
        /// below the heightmap: the floor of a cave or tunnel the terrain surface runs over. The
        /// heightmap is the client's ground everywhere it exists, so walkable surface well below it
        /// can only be inside something. Vertices are tested one by one because a polygon can be
        /// tens of metres across and its centre averaged well below a curved hillside.
        /// </summary>
        private static bool IsUnderTerrain(RcPolyMesh pmesh, int poly, TerrainHeightmap terrain)
        {
            var p = poly * pmesh.nvp * 2;
            var count = 0;
            var under = 0;

            for (var j = 0; j < pmesh.nvp; j++)
            {
                var v = pmesh.polys[p + j];

                if (v == RcRecast.RC_MESH_NULL_IDX)
                    break;

                var x = pmesh.bmin.X + pmesh.verts[v * 3] * pmesh.cs;
                var y = pmesh.bmin.Y + pmesh.verts[v * 3 + 1] * pmesh.ch;
                var z = pmesh.bmin.Z + pmesh.verts[v * 3 + 2] * pmesh.cs;
                var ground = terrain.Height(x, z);

                count++;

                if (ground != null && y < ground.Value - NavMeshFlags.UndergroundDepth)
                    under++;
            }

            return count > 0 && under * 2 > count;
        }

        /// <summary>Swallows Recast's console chatter, counting the one message it emits in bulk and keeping the rest.</summary>
        private sealed class CountingWriter : System.IO.TextWriter
        {
            private readonly System.Text.StringBuilder _line = new System.Text.StringBuilder();
            public int DanglingFaces;
            public readonly List<string> Other = new List<string>();

            public override System.Text.Encoding Encoding => System.Text.Encoding.UTF8;

            public override void Write(char value)
            {
                if (value == '\r')
                    return;

                if (value != '\n')
                {
                    _line.Append(value);
                    return;
                }

                var text = _line.ToString();
                _line.Clear();

                lock (Other)
                {
                    if (text.Contains("Removing dangling face"))
                        DanglingFaces++;
                    else if (text.Length > 0 && Other.Count < 100)
                        Other.Add(text);
                }
            }

            public override void Write(string value)
            {
                if (value == null)
                    return;

                foreach (var c in value)
                    Write(c);
            }
        }
    }
}
