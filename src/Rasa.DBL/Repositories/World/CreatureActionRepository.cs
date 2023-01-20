using System.Collections.Generic;
using System.Linq;

namespace Rasa.Repositories.World
{
    using Context.World;
    using Structures.World;

    public interface ICreatureActionRepository
    {
        List<CreatureActionEntry> Get();
    }

    public class CreatureActionRepository : ICreatureActionRepository
    {
        private readonly WorldContext _worldContext;

        public CreatureActionRepository(WorldContext worldContext)
        {
            _worldContext = worldContext;
        }

        public List<CreatureActionEntry> Get()
        {
            var query = _worldContext.CreateNoTrackingQuery(_worldContext.CreatureActionEntries);
            var creatureActionEntries = query.ToList();

            return creatureActionEntries;
        }
    }
}
