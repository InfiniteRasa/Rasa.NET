using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using DotRecast.Core;
using DotRecast.Core.Numerics;
using DotRecast.Detour;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Navigation;

    /// <summary>
    /// What creature AI's navigation costs on the real maps: how long a path search takes for the
    /// goals the AI asks for, and what reading every map's navmesh and cover costs at startup.
    /// Benchmarks, not tests: they print numbers and take minutes, so they run only with
    /// RASA_BENCH=1 in the environment and are inconclusive otherwise.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class AiCostBenchmarks
    {
        internal static void RequireBench()
        {
            if (Environment.GetEnvironmentVariable("RASA_BENCH") != "1")
                Assert.Inconclusive("benchmark: set RASA_BENCH=1 to run");
        }

        internal static readonly string[] LargestMaps =
        {
            "adv_foreas_concordia_wilderness", "adv_foreas_concordia_divide",
            "adv_foreas_concordia_palisades", "adv_foreas_howlingmaw1", "adv_arieki_torden_abyss"
        };

        /// <summary>
        /// Searches toward goals no route reaches (in another walkable region than the start), the
        /// whole search and the bounded one, and toward goals that can be reached, by distance.
        /// </summary>
        [TestMethod]
        public void PathSearchCostOnTheLargestMaps()
        {
            RequireBench();

            foreach (var name in LargestMaps)
            {
                var query = TestNavMeshes.Query(NavMeshFile.PathFor(Navmesh(), name));
                var random = new Random(11);
                var unreachable = new List<(double Ms, int Nodes)>();
                var unreachableBounded = new List<(double Ms, int Nodes)>();
                var near = new List<(double Ms, int Nodes)>();
                var far = new List<(double Ms, int Nodes)>();

                foreach (var (from, to) in RandomPairs(query, 4000, 7))
                {
                    var reachable = query.Reachable(from, to);

                    if (reachable == false && unreachable.Count < 40)
                    {
                        unreachable.Add(Time(query, () => query.FindPath(from, to, out _)));
                        unreachableBounded.Add(Time(query, () => query.FindPathBounded(from, to, out _)));
                    }
                    else if (reachable == true && far.Count < 200)
                        far.Add(Time(query, () => query.FindPath(from, to, out _)));
                }

                foreach (var (from, to) in RandomPairs(query, 300, 9, within: 40))
                    near.Add(Time(query, () => query.FindPath(from, to, out _)));

                Console.WriteLine($"{name}:");
                Print("  unreachable goal, whole search  ", unreachable);
                Print("  unreachable goal, bounded search", unreachableBounded);
                Print("  reachable goal anywhere         ", far);
                Print("  reachable goal within 40 m      ", near);
            }
        }

        /// <summary>Reading every navmesh and cover file, as NavMeshInit does: time and managed memory.</summary>
        [TestMethod]
        public void LoadingEveryNavmeshAndCover()
        {
            RequireBench();

            var directory = Navmesh();
            var navs = Directory.GetFiles(directory, "*" + NavMeshFile.Extension).OrderBy(p => p).ToArray();
            var covers = Directory.GetFiles(directory, "*" + CoverMesh.Extension).OrderBy(p => p).ToArray();
            var kept = new List<object>();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            var heapBefore = GC.GetTotalMemory(true);
            var watch = Stopwatch.StartNew();
            long navBytes = 0, coverBytes = 0;
            double navMs = 0, coverMs = 0;

            foreach (var path in navs)
            {
                var from = Stopwatch.GetTimestamp();
                var query = new NavMeshQuery(NavMeshFile.Read(path));
                query.HeightRange();
                navMs += Stopwatch.GetElapsedTime(from).TotalMilliseconds;
                navBytes += MemoryFootprint.Of(query.NavMesh).Bytes;
                kept.Add(query);
            }

            var heapNav = GC.GetTotalMemory(true);

            foreach (var path in covers)
            {
                var from = Stopwatch.GetTimestamp();
                var cover = CoverMesh.Read(path);
                coverMs += Stopwatch.GetElapsedTime(from).TotalMilliseconds;
                coverBytes += cover.MemoryBytes;
                kept.Add(cover);
            }

            var heapAll = GC.GetTotalMemory(true);

            Console.WriteLine($"one after another: {navs.Length} navmeshes {navMs:0} ms, heap +{(heapNav - heapBefore) / 1048576.0:0} MB (counted {navBytes / 1048576.0:0} MB); " +
                              $"{covers.Length} covers {coverMs:0} ms, heap +{(heapAll - heapNav) / 1048576.0:0} MB (counted {coverBytes / 1048576.0:0} MB); wall {watch.ElapsedMilliseconds} ms");

            kept.Clear();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // As NavMeshInit: side by side, the largest first.
            var heapBeforeParallel = GC.GetTotalMemory(true);
            var parallel = new object[navs.Length + covers.Length];
            var all = navs.Concat(covers).ToArray();
            watch.Restart();
            Parallel.ForEach(Enumerable.Range(0, all.Length).OrderByDescending(i => new FileInfo(all[i]).Length), i =>
            {
                if (all[i].EndsWith(CoverMesh.Extension, StringComparison.Ordinal))
                    parallel[i] = CoverMesh.Read(all[i]);
                else
                {
                    var query = new NavMeshQuery(NavMeshFile.Read(all[i]));
                    query.HeightRange();
                    parallel[i] = query;
                }
            });
            var parallelMs = watch.ElapsedMilliseconds;
            var heapParallel = GC.GetTotalMemory(true);
            Console.WriteLine($"side by side ({Environment.ProcessorCount} cores): {parallelMs} ms, heap +{(heapParallel - heapBeforeParallel) / 1048576.0:0} MB, " +
                              $"process working set {Process.GetCurrentProcess().WorkingSet64 / 1048576.0:0} MB");
            GC.KeepAlive(parallel);
        }

        internal static (double Ms, int Nodes) Time(NavMeshQuery query, Action search)
        {
            var from = Stopwatch.GetTimestamp();
            search();
            return (Stopwatch.GetElapsedTime(from).TotalMilliseconds, query.LastSearchNodes);
        }

        internal static void Print(string label, List<(double Ms, int Nodes)> samples)
        {
            if (samples.Count == 0)
            {
                Console.WriteLine($"{label}: none");
                return;
            }

            var ms = samples.Select(s => s.Ms).OrderBy(x => x).ToArray();
            var nodes = samples.Select(s => s.Nodes).OrderBy(x => x).ToArray();
            Console.WriteLine($"{label}: n={samples.Count} ms median {ms[ms.Length / 2]:0.00} p95 {ms[(int)(ms.Length * 0.95)]:0.00} max {ms[^1]:0.00}; " +
                              $"nodes median {nodes[nodes.Length / 2]} max {nodes[^1]}");
        }

        internal static IEnumerable<(Vector3 From, Vector3 To)> RandomPairs(NavMeshQuery query, int count, int seed, float? within = null)
        {
            var detour = new DtNavMeshQuery(query.NavMesh);
            var filter = new DtQueryDefaultFilter();
            var random = new RcRand(new Random(seed));

            for (var i = 0; i < count; i++)
            {
                detour.FindRandomPoint(filter, random, out var start, out var from);

                long end;
                RcVec3f to;

                if (within == null)
                    detour.FindRandomPoint(filter, random, out end, out to);
                else
                    detour.FindRandomPointAroundCircle(start, from, within.Value, filter, random, out end, out to);

                if (start != 0 && end != 0)
                    yield return (new Vector3(from.X, from.Y, from.Z), new Vector3(to.X, to.Y, to.Z));
            }
        }

        internal static string Navmesh()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "navmesh")))
                directory = directory.Parent;

            return Path.Combine(directory?.FullName ?? throw new DirectoryNotFoundException("navmesh"), "navmesh");
        }
    }
}
