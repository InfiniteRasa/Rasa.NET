using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using Rasa.ClientData;
using Rasa.Navigation;

namespace Rasa.NavMesh
{
    /// <summary>
    /// Builds one Detour navmesh per map from the client's own data - the terrain heightmap in
    /// <c>data/maps/&lt;map&gt;/t*_terrain.glm</c> and the collision meshes of every entity placed by
    /// <c>data/maps/&lt;map&gt;/&lt;map&gt;.map</c>, read out of <c>data/mesh*.glm</c> - and writes
    /// <c>&lt;out&gt;/&lt;map&gt;.nav</c> for Rasa.Game's NavMeshManager.
    ///
    /// <code>
    /// Rasa.NavMesh --client "C:\Games\Tabula Rasa" --out navmesh [--map adv_foreas_concordia_wilderness]...
    ///              [--terrain-step 2] [--cell 0.4] [--threads 8] [--obj]
    /// Rasa.NavMesh --path navmesh\adv_foreas_concordia_wilderness.nav  x1 y1 z1  x2 y2 z2
    /// </code>
    /// </summary>
    public static class Program
    {
        public static int Main(string[] args)
        {
            try
            {
                return Run(args);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
                return 1;
            }
        }

        private static int Run(string[] args)
        {
            string client = null, output = "navmesh", pathTest = null;
            var maps = new List<string>();
            var settings = new BuildSettings();
            var writeObj = false;
            var positional = new List<string>();

            for (var i = 0; i < args.Length; i++)
            {
                string Next() => ++i < args.Length ? args[i] : throw new ArgumentException($"{args[i - 1]} needs a value");

                switch (args[i])
                {
                    case "--client": client = Next(); break;
                    case "--out": output = Next(); break;
                    case "--map": maps.Add(Next()); break;
                    case "--terrain-step": settings.TerrainStep = int.Parse(Next()); break;
                    case "--cell": settings.CellSize = float.Parse(Next(), CultureInfo.InvariantCulture); break;
                    case "--cell-height": settings.CellHeight = float.Parse(Next(), CultureInfo.InvariantCulture); break;
                    case "--detail-distance": settings.DetailSampleDistance = float.Parse(Next(), CultureInfo.InvariantCulture); break;
                    case "--radius": settings.AgentRadius = float.Parse(Next(), CultureInfo.InvariantCulture); break;
                    case "--climb": settings.AgentMaxClimb = float.Parse(Next(), CultureInfo.InvariantCulture); break;
                    case "--slope": settings.AgentMaxSlope = float.Parse(Next(), CultureInfo.InvariantCulture); break;
                    case "--tile": settings.TileSize = int.Parse(Next()); break;
                    case "--threads": settings.Threads = int.Parse(Next()); break;
                    case "--obj": writeObj = true; break;
                    case "--path": pathTest = Next(); break;
                    case "-h": case "--help": Usage(); return 0;
                    default: positional.Add(args[i]); break;
                }
            }

            if (pathTest != null)
                return PathTest(pathTest, positional);

            if (client == null)
            {
                Usage();
                return 2;
            }

            var dataDirectory = Directory.Exists(Path.Combine(client, "data")) ? Path.Combine(client, "data") : client;
            var mapsDirectory = Path.Combine(dataDirectory, "maps");

            if (!Directory.Exists(mapsDirectory))
                throw new DirectoryNotFoundException($"{mapsDirectory} - is --client the Tabula Rasa install folder?");

            var csv = Path.Combine(AppContext.BaseDirectory, "data", "entity_meshes.csv");

            if (!File.Exists(csv))
                csv = Path.Combine("src", "Rasa.NavMesh", "data", "entity_meshes.csv");

            var stopwatch = Stopwatch.StartNew();
            Console.WriteLine($"Indexing mesh archives in {dataDirectory} ...");
            using var meshes = new MeshLibrary(dataDirectory, csv);
            Console.WriteLine($"  {meshes.MeshCount} meshes, {meshes.ClassCount} entity classes, {stopwatch.Elapsed.TotalSeconds:0.0} s");

            var mapDirectories = Directory.EnumerateDirectories(mapsDirectory)
                .Where(d => File.Exists(Path.Combine(d, Path.GetFileName(d) + ".map")))
                .Where(d => maps.Count == 0 || maps.Any(m => string.Equals(m, Path.GetFileName(d), StringComparison.OrdinalIgnoreCase)))
                .OrderBy(d => d, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (mapDirectories.Count == 0)
            {
                Console.Error.WriteLine("no maps matched");
                return 3;
            }

            Directory.CreateDirectory(output);
            var failures = 0;

            foreach (var mapDirectory in mapDirectories)
            {
                var name = Path.GetFileName(mapDirectory).ToLowerInvariant();
                var mapWatch = Stopwatch.StartNew();
                Console.WriteLine();
                Console.WriteLine($"{name}");

                try
                {
                    var geometry = MapGeometry.Load(mapDirectory, meshes, settings.TerrainStep, settings.TerrainMaxSlope);
                    Console.WriteLine($"  terrain {geometry.TerrainTriangles:n0} tris; {geometry.EntitiesWithCollision:n0} entities with collision ({geometry.EntityTriangles:n0} tris), "
                                      + $"{geometry.EntitiesWithoutCollision:n0} without, {geometry.EntitiesWithoutMesh:n0} with no mesh; "
                                      + $"bounds ({geometry.BoundsMin.X:0}, {geometry.BoundsMin.Y:0}, {geometry.BoundsMin.Z:0}) - ({geometry.BoundsMax.X:0}, {geometry.BoundsMax.Y:0}, {geometry.BoundsMax.Z:0})");

                    if (geometry.TriangleCount == 0)
                    {
                        Console.WriteLine("  nothing to walk on; skipped");
                        continue;
                    }

                    if (writeObj)
                        geometry.WriteObj(Path.Combine(output, name + ".obj"));

                    var navMesh = NavMeshBuilder.Build(geometry, settings, Console.WriteLine);
                    var path = NavMeshFile.PathFor(output, name);
                    NavMeshFile.Write(path, navMesh);
                    Console.WriteLine($"  wrote {path} ({new FileInfo(path).Length / 1024:n0} KB) in {mapWatch.Elapsed.TotalSeconds:0.0} s");
                }
                catch (Exception e)
                {
                    failures++;
                    Console.Error.WriteLine($"  FAILED: {e.Message}");
                }
            }

            Console.WriteLine();
            Console.WriteLine($"{mapDirectories.Count - failures} of {mapDirectories.Count} maps built in {stopwatch.Elapsed.TotalMinutes:0.0} min");

            return failures == 0 ? 0 : 4;
        }

        /// <summary>Loads a .nav and prints the path between two points - the same query the server runs.</summary>
        private static int PathTest(string navPath, List<string> coords)
        {
            if (coords.Count != 6)
            {
                Console.Error.WriteLine("--path needs six numbers: x1 y1 z1 x2 y2 z2");
                return 2;
            }

            var v = coords.Select(c => float.Parse(c, CultureInfo.InvariantCulture)).ToArray();
            var start = new Vector3(v[0], v[1], v[2]);
            var end = new Vector3(v[3], v[4], v[5]);

            var query = new NavMeshQuery(NavMeshFile.Read(navPath));
            Console.WriteLine($"start on mesh: {query.Nearest(start)}   ground: {query.GroundHeight(start)}   underground: {query.IsUnderground(start)}");
            Console.WriteLine($"end on mesh:   {query.Nearest(end)}   ground: {query.GroundHeight(end)}   underground: {query.IsUnderground(end)}");

            var path = query.FindPath(start, end, out var complete);

            if (path == null)
            {
                Console.WriteLine("no path");
                return 1;
            }

            var length = 0f;
            var previous = start;

            foreach (var p in path)
            {
                length += Vector3.Distance(previous, p);
                previous = p;
            }

            Console.WriteLine($"{(complete ? "complete" : "PARTIAL")} path, {path.Count} corners, {length:0.0} m:");

            foreach (var p in path)
                Console.WriteLine($"  {p.X:0.0} {p.Y:0.0} {p.Z:0.0}");

            return complete ? 0 : 1;
        }

        private static void Usage()
        {
            Console.WriteLine("Rasa.NavMesh --client <Tabula Rasa folder> [--out navmesh] [--map <name>]... [--terrain-step 2] [--cell 0.4] [--cell-height 0.2] [--detail-distance 6] [--threads N] [--obj]");
            Console.WriteLine("Rasa.NavMesh --path <file.nav> x1 y1 z1 x2 y2 z2");
        }
    }
}
