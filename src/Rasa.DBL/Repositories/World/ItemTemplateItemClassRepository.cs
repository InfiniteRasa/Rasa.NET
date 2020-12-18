namespace Rasa.Repositories.World
{
    using System.Linq;
    using Context.World;
    using Structures.World;

    public class ItemTemplateItemClassRepository : IItemTemplateItemClassRepository
    {
        private readonly WorldContext _worldContext;

        public ItemTemplateItemClassRepository(WorldContext worldContext)
        {
            _worldContext = worldContext;
        }

        public uint GetItemClass(uint itemTemplateId)
        {
            var query = _worldContext.CreateNoTrackingQuery(_worldContext.ItemTemplateItemClassEntries);
            var result = query.FirstOrDefault(e => e.ItemTemplateId == itemTemplateId);
            if (result == null)
            {
                throw new EntityNotFoundException(nameof(ItemTemplateItemClassEntry), nameof(ItemTemplateItemClassEntry.ItemTemplateId), itemTemplateId);
            }
            return result.ItemClass;
        }
    }
}