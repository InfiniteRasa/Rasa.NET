using System.Collections.Generic;
using System.Linq;

namespace Rasa.Database.Tables.World
{
    using Structures;

    public class EquipableClassTable
    {
        public static List<EquipableClassEntry> LoadEquipableClasses()
        {
            lock (GameDatabaseAccess.WorldLock)
            {
                return GameDatabaseAccess.WorldConnection.Equipableclass.Where(_ => true).ToList();
            }
        }
    }
}
