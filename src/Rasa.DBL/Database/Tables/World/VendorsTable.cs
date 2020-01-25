using System.Collections.Generic;
using System.Linq;

namespace Rasa.Database.Tables.World
{
    using Structures;

    public class VendorsTable
    {

        public static List<VendorsEntry> LoadVendors()
        {
            lock (GameDatabaseAccess.WorldLock)
            {
                return GameDatabaseAccess.WorldConnection.Vendors.Where(_ => true).ToList();
            }
        }
    }
}
