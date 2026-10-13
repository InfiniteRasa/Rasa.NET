using System.Collections.Generic;
using System.Linq;

namespace Rasa.Repositories.World
{
    using Rasa.Context.World;
    using Structures.World;
    public interface ITeleporterRepository
    {
        List<TeleporterEntry> GetTeleporters();

        /// <summary>The conditions on travelling through the wormhole network (wormhole_lock).</summary>
        List<WormholeLockEntry> GetWormholeLocks();
    }
    public class TeleporterRepository : ITeleporterRepository
    {
        private readonly WorldContext _worldContext;

        public TeleporterRepository(WorldContext worldContext)
        {
            _worldContext = worldContext;
        }

        public List<TeleporterEntry> GetTeleporters()
        {
            var query = _worldContext.CreateNoTrackingQuery(_worldContext.TeleporterEntries);
            var teleporterEntries = query.ToList();

            return teleporterEntries;
        }

        public List<WormholeLockEntry> GetWormholeLocks()
        {
            return _worldContext.CreateNoTrackingQuery(_worldContext.WormholeLockEntries).ToList();
        }
    }
}
