using System.Collections.Generic;
using System.Linq;

namespace Rasa.Database.Tables.World
{
    using Structures;

    public class EntityClassTable
    {
        public static List<EntityClassEntry> LoadEntityClass()
        {
            lock (GameDatabaseAccess.WorldLock)
            {
                return GameDatabaseAccess.WorldConnection.EntityClass.Where(_ => true).ToList();
            }
        }
    }
}
