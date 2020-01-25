using System.Collections.Generic;
using System.Linq;

namespace Rasa.Database.Tables.World
{
    using Structures;

    public class ArmorTemplateTable
    {
        public static List<ArmorTemplateEntry> GetArmorTemplates()
        {
            lock (GameDatabaseAccess.WorldLock)
            {
                return GameDatabaseAccess.WorldConnection.ItemtemplateArmor.Where(_ => true).ToList();
            }
        }
    }
}
