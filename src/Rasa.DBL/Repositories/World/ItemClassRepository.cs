using System.Collections.Generic;
using System.Linq;

namespace Rasa.Repositories.World
{
    using Context.World;
    using Structures.World;

    public interface IItemClassRepository
    {
        List<ItemClassEntry> Get();
    }
    public class ItemClassRepository : IItemClassRepository
    {
        private readonly WorldContext _worldContext;
        public ItemClassRepository(WorldContext worldContext)
        {
            _worldContext = worldContext;
        }

        public List<ItemClassEntry> Get()
        {
            var query = _worldContext.CreateNoTrackingQuery(_worldContext.ItemClassEntries);
            var itemClassEntries = query.ToList();

            return itemClassEntries;
        }
    }
}
