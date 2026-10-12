using System.Collections.Generic;
using System.Linq;

namespace Rasa.Repositories.World
{
    using Context.World;
    using Structures.World;

    public interface IWorldUsableRepository
    {
        /// <summary>The usable world objects placed on maps (world_usable), by id.</summary>
        List<WorldUsableEntry> Get();
    }

    public class WorldUsableRepository : IWorldUsableRepository
    {
        private readonly WorldContext _worldContext;

        public WorldUsableRepository(WorldContext worldContext)
        {
            _worldContext = worldContext;
        }

        public List<WorldUsableEntry> Get() =>
            _worldContext.CreateNoTrackingQuery(_worldContext.WorldUsableEntries).OrderBy(e => e.Id).ToList();
    }
}
