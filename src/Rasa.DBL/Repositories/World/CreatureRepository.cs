using System.Collections.Generic;
using System.Linq;

namespace Rasa.Repositories.World
{
    using Context.World;
    using Structures.World;

    public interface ICreatureRepository
    {
        List<CreatureEntry> Get();
    }

    public class CreatureRepository : ICreatureRepository
    {
        private readonly WorldContext _worldContext;

        public CreatureRepository(WorldContext worldContext)
        {
            _worldContext = worldContext;
        }

        public List<CreatureEntry> Get()
        {
            var query = _worldContext.CreateNoTrackingQuery(_worldContext.CreatureEntries);
            var creatureEntries = query.ToList();

            return creatureEntries;
        }
    }
}
