using System.Collections.Generic;
using System.Linq;

namespace Rasa.Repositories.World
{
    using Context.World;
    using Structures.World;
    public interface IItemTemplateRequirementSkillRepository
    {
        List<ItemTemplateRequirementSkillEntry> Get();
    }
    public class ItemTemplateRequirementSkillRepository : IItemTemplateRequirementSkillRepository
    {
        private readonly WorldContext _worldContext;

        public ItemTemplateRequirementSkillRepository(WorldContext worldContext)
        {
            _worldContext = worldContext;
        }

        public List<ItemTemplateRequirementSkillEntry> Get()
        {
            var query = _worldContext.CreateNoTrackingQuery(_worldContext.ItemTemplateRequirementSkillEntries);
            var requirementSkillEntries = query.ToList();

            return requirementSkillEntries;
        }
    }
}
