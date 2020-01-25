using System.Collections.Generic;
using System.Linq;

namespace Rasa.Database.Tables.World
{
    using Structures;

    public class ItemTemplateResistanceTable
    {
        public static List<ItemTemplateResistanceEntry> GetItemTemplateResistance()
        {
            lock (GameDatabaseAccess.WorldLock)
            {
                return GameDatabaseAccess.WorldConnection.ItemtemplateResistance.Where(_ => true).ToList();
            }
        }
    }
}
