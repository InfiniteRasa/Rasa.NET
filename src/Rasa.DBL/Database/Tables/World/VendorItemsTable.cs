using System.Collections.Generic;
using System.Linq;

namespace Rasa.Database.Tables.World
{
    using Structures;

    public class VendorItemsTable
    {
        public static List<VendorItemsEntry> LoadVendorItems()
        {
            lock (GameDatabaseAccess.WorldLock)
            {
                return GameDatabaseAccess.WorldConnection.VendorItems.Where(_ => true).ToList();
            }
        }
    }
}
