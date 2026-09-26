using Rasa.Missions.Content;
using System;
using System.IO;
using System.Linq;
using Rasa.Missions.Scenes;
using Rasa.Managers;
using Rasa.Services.Preloader.Missions;

namespace Rasa.Game.Missions.Integration
{
    internal static class MissionAssetCheck
    {
        internal static int Run(string applicationDirectory, string workingDirectory)
        {
            var navigation = NavMeshManager.ResolveDirectory(null, workingDirectory, applicationDirectory);
            if (!Directory.Exists(navigation))
                throw new DirectoryNotFoundException("Published navigation assets are missing.");
            var meshes = Directory.GetFiles(navigation, "*.nav").Length;
            if (meshes == 0)
                throw new InvalidOperationException("Published assets have no navigation meshes.");
            var definitions = BootcampMissionDataV1.Scenes().Values
                .Append(BootcampMissionDataV1.Experience().Scene).ToArray();
            var registry = new SceneScriptRegistry();
            foreach (var definition in definitions)
                if (!registry.TryResolve(definition.Script, definition.StateVersion, out _))
                    throw new InvalidOperationException($"Required migrated script {definition.Script}/{definition.StateVersion} is unavailable.");
            Console.WriteLine($"Navigation: {navigation} ({meshes} maps)");
            Console.WriteLine($"Migration-owned mission content: {definitions.Length} compiled scene bindings; scripts available.");
            return 0;
        }
    }
}
