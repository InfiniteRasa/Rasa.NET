using System;
using System.IO;
using System.Numerics;
using Rasa.Navigation;
using Rasa.Structures;

namespace Rasa.Test.Missions.Encounters
{
    internal static class SceneNavigationFixture
    {
        internal static Vector3 Attach(MapChannel map)
        {
            var root = new DirectoryInfo(AppContext.BaseDirectory);
            while (root != null && !File.Exists(Path.Combine(root.FullName, "Rasa.NET.sln")))
                root = root.Parent;
            if (root == null)
                throw new DirectoryNotFoundException("Repository navmesh assets were not found.");
            map.NavMesh = new NavMeshQuery(NavMeshFile.Read(Path.Combine(root.FullName, "navmesh", "adv_bootcamp.nav")));
            return map.NavMesh.Nearest(new Vector3(391.5f, 120.059f, 164.8f))
                ?? throw new InvalidOperationException("Fixture start is not on the real mesh.");
        }
    }
}
