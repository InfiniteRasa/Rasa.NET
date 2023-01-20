using System.Collections.Generic;
using System.Linq;

namespace Rasa.Repositories.World
{
    using Rasa.Context.World;
    using Structures.World;
    public interface ITeleporterRepository
    {
        List<TeleporterEntry> GetTeleporters();
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
    }
}
