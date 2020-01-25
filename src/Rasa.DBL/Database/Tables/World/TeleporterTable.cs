using System.Collections.Generic;
using System.Linq;

namespace Rasa.Database.Tables.World
{
    using Structures;

    public class TeleporterTable
    {
        public static List<TeleporterEntry> GetTeleporters()
        { 
            lock (GameDatabaseAccess.WorldLock)
            {
                return GameDatabaseAccess.WorldConnection.Teleporter.Where(_ => true).ToList();
            }
        }
    }
}
