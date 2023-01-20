using System.Collections.Generic;

namespace Rasa.Repositories.World
{
    using Context.World;
    using Structures.World;
    using System.Linq;

    public interface IFootlockerRepository
    {
        List<FootlockerEntry> GetFootlockers();
    }
    public class FootlockerRepository : IFootlockerRepository
    {
        private readonly WorldContext _worldContext;

        public FootlockerRepository(WorldContext worldContext)
        {
            _worldContext = worldContext;
        }

        public List<FootlockerEntry> GetFootlockers()
        {
            var query = _worldContext.CreateNoTrackingQuery(_worldContext.FootlockerEntries);
            var entries = query.ToList();


            return entries;
        }
    }
}
