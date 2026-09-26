using System.Collections.Generic;
using Rasa.Structures.World;

namespace Rasa.Repositories.World
{
    public interface IMigratedMissionContentRepository : IMissionContentRepository
    {
        List<MissionContentDefinitionEntry> GetEnabledDefinitions();
        List<MissionSceneBindingEntry> GetSceneBindings();
        List<MissionExperienceBindingEntry> GetExperiences();
    }
}
