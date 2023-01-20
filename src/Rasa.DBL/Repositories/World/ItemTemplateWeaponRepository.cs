using System.Collections.Generic;
using System.Linq;

namespace Rasa.Repositories.World
{
    using Context.World;
    using Structures.World;

    public interface IItemTemplateWeaponRepository
    {
        List<ItemTemplateWeaponEntry> Get();
    }
    public class ItemTemplateWeaponRepository : IItemTemplateWeaponRepository
    {
        private readonly WorldContext _worldContext;

        public ItemTemplateWeaponRepository(WorldContext worldContext)
        {
            _worldContext = worldContext;
        }

        public List<ItemTemplateWeaponEntry> Get()
        {
            var query = _worldContext.CreateNoTrackingQuery(_worldContext.ItemTemplateWeaponEntries);
            var weaponEntries = query.ToList();

            return weaponEntries;
        }
    }
}
