using System.Collections.Generic;
using System.Linq;

namespace Rasa.Repositories.World
{
    using Context.World;
    using Structures.World;
    public interface ILogosRepository
    {
        List<LogosEntry> Get();
    }
    public class LogosRepository : ILogosRepository
    {
        private readonly WorldContext _worldContext;

        public LogosRepository(WorldContext worldContext)
        {
            _worldContext = worldContext;
        }
        public List<LogosEntry> Get()
        {
            var query = _worldContext.CreateNoTrackingQuery(_worldContext.LogosEntries);
            var logosEntries = query.ToList();

            return logosEntries;
        }
    }
}
