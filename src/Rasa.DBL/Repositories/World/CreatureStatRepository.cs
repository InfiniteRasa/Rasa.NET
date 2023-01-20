using System.Linq;

namespace Rasa.Repositories.World
{
    using Context.World;
    using Structures.World;

    public interface ICreatureStatRepository
    {
        CreatureStatEntry GetCreatureStats(uint creatureId);
    }

    public class CreatureStatRepository : ICreatureStatRepository
    {
        private readonly WorldContext _worldContext;

        public CreatureStatRepository(WorldContext worldContext)
        {
            _worldContext = worldContext;
        }

        public CreatureStatEntry GetCreatureStats(uint creatureId)
        {
            var query = _worldContext.CreateNoTrackingQuery(_worldContext.CreatureStatEntries);
            var creatureStat = query.FirstOrDefault(e => e.Id == creatureId);

            return creatureStat;
        }
    }
}
