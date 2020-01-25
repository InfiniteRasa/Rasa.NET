using System.Collections.Generic;
using System.Linq;

namespace Rasa.Database.Tables.World
{
    using Structures;

    public class ItemTemplateItemClassTable
    {
        public static List<ItemTemplateItemClassEntry> GetItemTemplateItemClass()
        {
            lock (GameDatabaseAccess.WorldLock)
            {
                return GameDatabaseAccess.WorldConnection.ItemtemplateItemclass.Where(_ => true).ToList();
            }
        }
    }
}
