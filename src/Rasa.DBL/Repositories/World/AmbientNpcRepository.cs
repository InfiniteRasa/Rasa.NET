using System.Collections.Generic;
using System.Linq;

namespace Rasa.Repositories.World
{
    using Context.World;
    using Structures.World;

    public interface IAmbientNpcRepository
    {
        /// <summary>The ambient figures placed on maps (ambient_npc), by id.</summary>
        List<AmbientNpcEntry> Get();
    }

    public class AmbientNpcRepository : IAmbientNpcRepository
    {
        private readonly WorldContext _worldContext;

        public AmbientNpcRepository(WorldContext worldContext)
        {
            _worldContext = worldContext;
        }

        public List<AmbientNpcEntry> Get() =>
            _worldContext.CreateNoTrackingQuery(_worldContext.AmbientNpcEntries).OrderBy(e => e.Id).ToList();
    }
}
