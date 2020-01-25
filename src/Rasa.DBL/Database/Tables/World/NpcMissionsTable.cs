using System.Collections.Generic;
using System.Linq;

namespace Rasa.Database.Tables.World
{
    using Structures;

    public class NpcMissionsTable
    {
       public static List<NpcMissionEntry> GetNpcMissions()
        {
            lock (GameDatabaseAccess.WorldLock)
            {
                return GameDatabaseAccess.WorldConnection.NpcMissions.Where(_ => true).ToList();
            }
        }
    }
}
