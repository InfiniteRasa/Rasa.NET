using System.Collections.Generic;
using System.Linq;

namespace Rasa.Database.Tables.World
{
    using Structures;

    public class WeaponTemplateTable
    {
        public static List<WeaponTemplateEntry> GetWeaponTemplates()
        {
            lock (GameDatabaseAccess.WorldLock)
            {
                return GameDatabaseAccess.WorldConnection.ItemtemplateWeapon.Where(_ => true).ToList();
            }
        }
    }
}
