using System.Collections.Generic;
using System.Linq;

namespace Rasa.Database.Tables.World
{
    using Structures;

    public class SpawnPoolTable
    {
        public static List<SpawnPoolEntry> LoadSpawnPool()
        {
            lock (GameDatabaseAccess.WorldLock)
            {
                return GameDatabaseAccess.WorldConnection.Spawnpool.Where(_ => true).ToList();
            }
        }
    }
}
