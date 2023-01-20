using System.Collections.Generic;
using System.Linq;

namespace Rasa.Repositories.World
{
    using Context.World;
    using Structures.World;

    public interface IItemTemplateRequirementRepository
    {
        List<ItemTemplateRequirementEntry> Get();
    }
    public class ItemTemplateRequirementRepository : IItemTemplateRequirementRepository
    {
        private readonly WorldContext _worldContext;

        public ItemTemplateRequirementRepository(WorldContext worldContext)
        {
            _worldContext = worldContext;
        }

        public List<ItemTemplateRequirementEntry> Get()
        {
            var query = _worldContext.CreateNoTrackingQuery(_worldContext.ItemTemplateRequirementEntries);
            var requirementEntries = query.ToList();

            return requirementEntries;
        }
    }
}
