using System.Collections.Generic;
using System.Linq;

namespace Rasa.Repositories.World
{
    using Context.World;
    using Structures.World;

    public interface IArmorClassRepository
    {
        List<ArmorClassEntry> Get();
    }
    public class ArmorClassRepository : IArmorClassRepository
    {
        private readonly WorldContext _worldContext;

        public ArmorClassRepository(WorldContext worldContext)
        {
            _worldContext = worldContext;
        }

        public List<ArmorClassEntry> Get()
        {
            var query = _worldContext.CreateNoTrackingQuery(_worldContext.ArmorClassEntries);
            var armorClassEntries = query.ToList();

            return armorClassEntries;
        }
    }
}
