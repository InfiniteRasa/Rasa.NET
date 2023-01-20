using Rasa.Structures.World;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Repositories.World
{
    using Context.World;
    public interface IItemTemplateRepository
    {
        List<ItemTemplateEntry> Get();
    }
    public class ItemTemplateRepository : IItemTemplateRepository
    {
        private readonly WorldContext _worldContext;

        public ItemTemplateRepository(WorldContext worldContext)
        {
            _worldContext = worldContext;
        }

        public List<ItemTemplateEntry> Get()
        {
            var query = _worldContext.CreateNoTrackingQuery(_worldContext.ItemTemplateEntries);
            var itemTemplateList = query.ToList();

            return itemTemplateList;
        }
    }
}
