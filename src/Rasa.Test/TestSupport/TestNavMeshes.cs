using System;
using System.Collections.Concurrent;
using System.IO;
using DotRecast.Detour;
using Rasa.Navigation;

namespace Rasa.Test
{
    /// <summary>
    /// Navmeshes for tests, read from disk once per test process and shared. A Concordia Wilderness
    /// mesh is about 15 MB on disk and far more in memory, and every runtime harness used to read its
    /// own copy, so a full run in one process held hundreds of copies of the same mesh. Nothing in the
    /// game adds, removes or edits tiles after a load, so the mesh itself is safe to share; each caller
    /// still gets its own <see cref="NavMeshQuery"/>, because a Detour query keeps per-query state.
    /// </summary>
    internal static class TestNavMeshes
    {
        private static readonly ConcurrentDictionary<string, Lazy<DtNavMesh>> Meshes =
            new(StringComparer.OrdinalIgnoreCase);

        internal static NavMeshQuery Query(string path)
        {
            var mesh = Meshes.GetOrAdd(Path.GetFullPath(path),
                fullPath => new Lazy<DtNavMesh>(() => NavMeshFile.Read(fullPath))).Value;
            return new NavMeshQuery(mesh);
        }
    }
}
