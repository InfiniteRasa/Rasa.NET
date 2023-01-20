using System.Collections.Generic;
using System.Linq;

namespace Rasa.Repositories.World
{
    using Context.World;
    using Structures.World;

    public interface INpcPackageRepository
    {
        List<NpcPackageEntry> Get();
    }
    public class NpcPackageRepository : INpcPackageRepository
    {
        private readonly WorldContext _worldContext;

        public NpcPackageRepository(WorldContext worldContext)
        {
            _worldContext = worldContext;
        }

        public List<NpcPackageEntry> Get()
        {
            var query = _worldContext.CreateNoTrackingQuery(_worldContext.NpcPackageEntries);
            var npcPackageEntries = query.ToList();

            return npcPackageEntries;
        }
    }
}
