using System.Collections.Generic;
using System.Linq;

namespace Rasa.Database.Tables.World
{
    using Structures;

    public class ItemClassTable
    {
        public static List<ItemClassEntry> LoadItemClasses()
        {
            lock (GameDatabaseAccess.WorldLock)
            {
                return GameDatabaseAccess.WorldConnection.ItemClass.Where(_ => true).ToList();
            }
        }
    }
}
