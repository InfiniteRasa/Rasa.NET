using System.Collections.Generic;
using System.Linq;

namespace Rasa.Repositories.World
{
    using Context.World;
    using Structures.World;

    public interface IVendorItemRepository
    {
        List<VendorItemEntry> Get();
    }
    public class VendorItemRepository : IVendorItemRepository
    {
        private readonly WorldContext _worldContext;

        public VendorItemRepository(WorldContext worldContext)
        {
            _worldContext = worldContext;
        }

        public List<VendorItemEntry> Get()
        {
            var query = _worldContext.CreateNoTrackingQuery(_worldContext.VendorItemEntries);
            var vendorItemEntries = query.ToList();

            return vendorItemEntries;
        }
    }
}
