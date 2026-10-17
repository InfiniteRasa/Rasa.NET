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

        /// <summary>The lootable containers placed on maps (world_container), by id.</summary>
        List<WorldContainerEntry> GetContainers() => new List<WorldContainerEntry>();

        /// <summary>The fixed items of every container (world_container_item).</summary>
        List<WorldContainerItemEntry> GetContainerItems() => new List<WorldContainerItemEntry>();
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

        public List<WorldContainerEntry> GetContainers() =>
            _worldContext.CreateNoTrackingQuery(_worldContext.WorldContainerEntries).OrderBy(e => e.Id).ToList();

        public List<WorldContainerItemEntry> GetContainerItems() =>
            _worldContext.CreateNoTrackingQuery(_worldContext.WorldContainerItemEntries)
                .OrderBy(e => e.ContainerId).ThenBy(e => e.ItemTemplateId).ToList();
    }
}
