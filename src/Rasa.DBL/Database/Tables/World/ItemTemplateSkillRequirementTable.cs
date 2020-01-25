using System.Collections.Generic;
using System.Linq;

namespace Rasa.Database.Tables.World
{
    using Structures;

    public class ItemTemplateSkillRequirementTable
    {
        public static List<ItemTemplateSkillRequirementEntry> GetItemTemplateSkillRequirement()
        {
            lock (GameDatabaseAccess.WorldLock)
            {
                return GameDatabaseAccess.WorldConnection.ItemtemplateSkillrequirement.Where(_ => true).ToList();
            }
        }
    }
}
