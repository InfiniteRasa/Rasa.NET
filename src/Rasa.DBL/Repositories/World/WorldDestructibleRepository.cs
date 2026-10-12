using System.Collections.Generic;
using System.Linq;

namespace Rasa.Repositories.World
{
    using Context.World;
    using Structures.World;

    public interface IWorldDestructibleRepository
    {
        /// <summary>The destructible world objects placed on maps (world_destructible), by id.</summary>
        List<WorldDestructibleEntry> Get();
    }

    public class WorldDestructibleRepository : IWorldDestructibleRepository
    {
        private readonly WorldContext _worldContext;

        public WorldDestructibleRepository(WorldContext worldContext)
        {
            _worldContext = worldContext;
        }

        public List<WorldDestructibleEntry> Get() =>
            _worldContext.CreateNoTrackingQuery(_worldContext.WorldDestructibleEntries).OrderBy(e => e.Id).ToList();
    }
}
