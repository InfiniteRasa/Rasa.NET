using System.Collections.Generic;
using System.Linq;

namespace Rasa.Database.Tables.World
{
    using Structures;

    public class ItemTemplateRequirementsTable
    {
        public static List<ItemTemplateRequirementsEntry> GetItemTemplateRequirements()
        {
            lock (GameDatabaseAccess.WorldLock)
            {
                return GameDatabaseAccess.WorldConnection.ItemtemplateRequirements.Where(_ => true).ToList();
            }
        }
    }
}
