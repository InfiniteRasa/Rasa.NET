using System.Collections.Generic;
using System.Linq;

namespace Rasa.Repositories.World
{
    using Context.World;
    using Structures.World;
    public interface IEntityClassRepository
    {
        List<EntityClassEntry> Get();
    }
    public class EntityClassRepository : IEntityClassRepository
    {
        private readonly WorldContext _worldContext;

        public EntityClassRepository(WorldContext worldContext)
        {
            _worldContext = worldContext;
        }

        public List<EntityClassEntry> Get()
        {
            var query = _worldContext.CreateNoTrackingQuery(_worldContext.EntityClassEntries);
            var entityClassEntries = query.ToList();

            return entityClassEntries;
        }
    }
}
