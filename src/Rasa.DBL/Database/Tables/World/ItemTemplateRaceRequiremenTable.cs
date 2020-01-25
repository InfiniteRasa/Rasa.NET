using System.Collections.Generic;
using System.Linq;

namespace Rasa.Database.Tables.World
{
    using Structures;

    public class ItemTemplateRaceRequiremenTable
    {
        public static List<ItemTemplateRaceRequirementEntry> GetItemTemplateRaceRequirement()
        {
            lock (GameDatabaseAccess.WorldLock)
            {
                return GameDatabaseAccess.WorldConnection.ItemtemplateRacerequirement.Where(_ => true).ToList();
            }
        }
    }
}
