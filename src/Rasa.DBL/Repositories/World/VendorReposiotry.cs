using System.Collections.Generic;
using System.Linq;

namespace Rasa.Repositories.World
{
    using Context.World;
    using Structures.World;

    public interface IVendorReposiotry
    {
        List<VendorEntry> Get();
    }
    public class VendorReposiotry : IVendorReposiotry
    {
        private readonly WorldContext _worldContext;

        public VendorReposiotry(WorldContext worldContext)
        {
            _worldContext = worldContext;
        }

        public List<VendorEntry> Get()
        {
            var query = _worldContext.CreateNoTrackingQuery(_worldContext.VendorEntries);
            var vendorEntries = query.ToList();

            return vendorEntries;
        }
    }
}
