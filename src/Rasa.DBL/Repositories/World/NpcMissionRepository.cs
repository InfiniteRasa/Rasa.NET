using System.Collections.Generic;
using System.Linq;

namespace Rasa.Repositories.World
{
    using Context.World;
    using Structures.World;

    public interface INpcMissionRepository
    {
        List<NpcMissionEntry> Get();
    }
    public class NpcMissionRepository : INpcMissionRepository
    {
        private readonly WorldContext _worldContext;

        public NpcMissionRepository(WorldContext worldContext)
        {
            _worldContext = worldContext;
        }

        public List<NpcMissionEntry> Get()
        {
            var query = _worldContext.CreateNoTrackingQuery(_worldContext.NpcMissionEntries);
            var npcMissionEntries = query.ToList();

            return npcMissionEntries;
        }
    }
}
