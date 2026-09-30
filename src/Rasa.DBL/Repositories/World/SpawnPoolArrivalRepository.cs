using System.Collections.Generic;
using System.Linq;

namespace Rasa.Repositories.World
{
    using Context.World;
    using Structures.World;

    public interface ISpawnPoolArrivalRepository
    {
        List<SpawnPoolArrivalEntry> GetArrivals();
    }

    public class SpawnPoolArrivalRepository : ISpawnPoolArrivalRepository
    {
        private readonly WorldContext _worldContext;

        public SpawnPoolArrivalRepository(WorldContext worldContext)
        {
            _worldContext = worldContext;
        }

        public List<SpawnPoolArrivalEntry> GetArrivals()
        {
            var query = _worldContext.CreateNoTrackingQuery(_worldContext.SpawnPoolArrivalEntries);

            return query.ToList();
        }
    }
}
