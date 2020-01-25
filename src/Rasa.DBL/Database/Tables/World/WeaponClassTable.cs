using System.Collections.Generic;
using System.Linq;

namespace Rasa.Database.Tables.World
{
    using Structures;

    public class WeaponClassTable
    {
        public static List<WeaponClassEntry> LoadWeaponClasses()
        {
            lock (GameDatabaseAccess.WorldLock)
            {
                return GameDatabaseAccess.WorldConnection.WeaponClass.Where(_ => true).ToList();
            }
        }
    }
}
