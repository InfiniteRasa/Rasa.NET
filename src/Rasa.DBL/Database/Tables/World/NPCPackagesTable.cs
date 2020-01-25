using System.Collections.Generic;
using System.Linq;

namespace Rasa.Database.Tables.World
{
    using Structures;

    public class NPCPackagesTable
    {
        public static List<NPCPackagesEntry> LoadNPCPackages()
        {
            lock (GameDatabaseAccess.WorldLock)
            {
                return GameDatabaseAccess.WorldConnection.NpcPackages.Where(_ => true).ToList();
            }
        }
    }
}
