using System.Collections.Generic;
using System.Linq;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;

namespace Rasa.Game.Missions.Integration
{
    using World;

    internal static class MissionRuntimeComposition
    {
        internal static void Bind(MissionContentCatalog catalog, SceneApplication scenes,
            PublicActorLeaseService actors, ActorPolicyCatalog policies)
        {
            scenes.ClearBindings();
            policies.Clear();
            foreach (var binding in catalog.SceneBindings)
            {
                var document = binding.Value;
                if (document.Script != null)
                    scenes.Bind(binding.Key, document.Script, document.Bindings(catalog.Missions[binding.Key].ContentRevision));
                if (document.PublicEncounter != null)
                    actors.Bind(document.PublicEncounter, document.Actors[document.PublicEncounter.Role].GameplayPolicy);
            }
            foreach (var experience in catalog.Experiences)
            {
                scenes.BindExperience(experience);
                foreach (var policy in experience.ActorPolicies)
                    policies.Add(experience.MapContextId, policy.Key, policy.Value);
            }
            foreach (var mission in catalog.Missions.Values.Where(mission => mission.IsOperational &&
                mission.Objectives.Values.Any(objective => objective.GetExecutableTransitionsOrLegacyDefault()
                    .Any(transition => transition.ProgressRule?.Kind == MissionProgressEventKind.DeadlineElapsed))))
                if (!scenes.Owns(mission.MissionId))
                    scenes.Bind(mission.MissionId, "data.sequence", new SceneBindings(mission.ContentRevision,
                        new Dictionary<string, SceneActorDefinition>(), new Dictionary<string, SceneRoute>(),
                        new Dictionary<uint, SceneSequence>()));
        }
    }
}
