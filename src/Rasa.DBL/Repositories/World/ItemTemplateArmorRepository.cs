using System.Collections.Generic;
using System.Linq;

namespace Rasa.Repositories.World
{
    using Context.World;
    using Structures.World;

    public interface IItemTemplateArmorRepository
    {
        List<ItemTemplateArmorEntry> Get();
    }
    public class ItemTemplateArmorRepository : IItemTemplateArmorRepository
    {
        private readonly WorldContext _worldContext;

        public ItemTemplateArmorRepository(WorldContext worldContext)
        {
            _worldContext = worldContext;
        }

        public List<ItemTemplateArmorEntry> Get()
        {
            var query = _worldContext.CreateNoTrackingQuery(_worldContext.ItemTemplateArmorEntries);
            var itemTemplateArmorEntries = query.ToList();

            return itemTemplateArmorEntries;
        }
    }
}
