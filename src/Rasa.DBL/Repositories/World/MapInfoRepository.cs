using System.Collections.Generic;
using System.Linq;

namespace Rasa.Repositories.World
{
    using Context.World;
    using Structures.World;
    public interface IMapInfoRepository
    {
        List<MapInfoEntry> Get();
    }
    public class MapInfoRepository : IMapInfoRepository
    {
        private readonly WorldContext _worldContext;

        public MapInfoRepository(WorldContext worldContext)
        {
            _worldContext = worldContext;
        }

        public List<MapInfoEntry> Get()
        {
            var query = _worldContext.CreateNoTrackingQuery(_worldContext.MapInfoEntries);
            var mapInfoEntries = query.ToList();

            return mapInfoEntries;
        }
    }
}
