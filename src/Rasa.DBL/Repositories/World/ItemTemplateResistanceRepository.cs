using System.Collections.Generic;
using System.Linq;

namespace Rasa.Repositories.World
{
    using Context.World;
    using Structures.World;
    public interface IItemTemplateResistanceRepository
    {
        List<ItemTemplateResistanceEntry> Get();
    }
    public class ItemTemplateResistanceRepository : IItemTemplateResistanceRepository
    {
        private readonly WorldContext _worldContext;

        public ItemTemplateResistanceRepository(WorldContext worldContext)
        {
            _worldContext = worldContext;
        }

        public List<ItemTemplateResistanceEntry> Get()
        {
            var query = _worldContext.CreateNoTrackingQuery(_worldContext.ItemTemplateResistanceEntries);
            var requirementResistanceEntries = query.ToList();

            return requirementResistanceEntries;
        }
    }
}
