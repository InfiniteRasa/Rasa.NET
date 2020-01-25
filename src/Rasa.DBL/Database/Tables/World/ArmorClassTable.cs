using System.Collections.Generic;
using System.Linq;

namespace Rasa.Database.Tables.World
{
    using Structures;

    public class ArmorClassTable
    {

        public static List<ArmorClassEntry> LoadArmorClasses()
        {
            lock (GameDatabaseAccess.WorldLock)
            {
                return GameDatabaseAccess.WorldConnection.ArmorClass.Where(_ => true).ToList();
            }
        }
    }
}
