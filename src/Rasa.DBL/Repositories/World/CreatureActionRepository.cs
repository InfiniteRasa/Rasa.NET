using System.Collections.Generic;
using System.Linq;

namespace Rasa.Repositories.World
{
    using Context.World;
    using Structures.World;

    public interface ICreatureActionRepository
    {
        CreatureActionEntry Get(uint id);
        Dictionary<uint,CreatureActionEntry> Get();
    }

    public class CreatureActionRepository : ICreatureActionRepository
    {
        private readonly WorldContext _worldContext;

        public CreatureActionRepository(WorldContext worldContext)
        {
            _worldContext = worldContext;
        }

        public CreatureActionEntry Get(uint id)
        {
            var query = _worldContext.CreateNoTrackingQuery(_worldContext.CreatureActionEntries);
            var entry = query.Where(e => e.Id == id).FirstOrDefault();

            return entry;
        }

        public Dictionary<uint, CreatureActionEntry> Get()
        {
            var query = _worldContext.CreateNoTrackingQuery(_worldContext.CreatureActionEntries);
            var entres = query.ToDictionary(e => e.Id, e => e);

            return entres;
        }
    }
}
