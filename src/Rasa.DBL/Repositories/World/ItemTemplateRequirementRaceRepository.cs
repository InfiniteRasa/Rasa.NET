using System.Collections.Generic;
using System.Linq;

namespace Rasa.Repositories.World
{
    using Context.World;
    using Structures.World;

    public interface IItemTemplateRequirementRaceRepository
    {
        List<ItemTemplateRequirementRaceEntry> Get();
    }

    public class ItemTemplateRequirementRaceRepository : IItemTemplateRequirementRaceRepository
    {
        private readonly WorldContext _worldContext;

        public ItemTemplateRequirementRaceRepository(WorldContext worldContext)
        {
            _worldContext = worldContext;
        }

        public List<ItemTemplateRequirementRaceEntry> Get()
        {
            var query = _worldContext.CreateNoTrackingQuery(_worldContext.ItemTemplateRequirementRaceEntries);
            var requirementRaceEntries = query.ToList();

            return requirementRaceEntries;
        }
    }
}
