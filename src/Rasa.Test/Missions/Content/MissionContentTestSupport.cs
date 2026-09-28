using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Rasa.Missions.Content;

namespace Rasa.Test.Missions.Content
{
    using Rasa.Context.World;
    using Rasa.Services.Preloader.Missions;
    using Rasa.Structures.World;

    internal static class MissionContentTestSupport
    {
        internal static MissionExperienceDefinition ReadExperience() => BootcampMissionDataV1.Experience();

        internal static MissionSceneDefinition PreserveItemMetadata(MissionSceneDefinition replacement, MissionSceneDefinition current)
        {
            replacement.Items = current.Items;
            replacement.AcceptanceItems = current.AcceptanceItems;
            return replacement;
        }

        internal static Dictionary<uint, MissionSceneDefinition> ReadScenes(WorldContext context) =>
            context.Set<MissionSceneBindingEntry>().AsNoTracking().ToArray().ToDictionary(entry => entry.MissionId,
                entry => JsonSerializer.Deserialize<MissionSceneDefinition>(entry.Bindings, MissionContentCodec.Options)
                    ?? throw new InvalidOperationException("Missing migrated scene definition."));

        internal static void ConfigureScenes(WorldContext context, Action<IDictionary<uint, MissionSceneDefinition>> configure)
        {
            if (configure == null)
                return;
            var scenes = ReadScenes(context);
            configure(scenes);
            foreach (var row in context.Set<MissionSceneBindingEntry>())
            {
                if (!scenes.TryGetValue(row.MissionId, out var scene))
                    throw new InvalidOperationException("Test scene overrides must retain the migrated mission IDs.");
                row.ScriptKey = scene.Script;
                row.StateVersion = scene.StateVersion;
                row.Bindings = JsonSerializer.Serialize(scene, MissionContentCodec.Options);
            }
            context.SaveChanges();
            context.ChangeTracker.Clear();
        }
    }
}
