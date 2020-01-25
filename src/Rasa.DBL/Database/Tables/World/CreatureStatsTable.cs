using System.Linq;

namespace Rasa.Database.Tables.World
{
    using Structures;

    public class CreatureStatsTable
    {
        public static CreatureStatsEntry GetCreatureStats(uint creatureDbId)
        {
            lock (GameDatabaseAccess.WorldLock)
            {
                return GameDatabaseAccess.WorldConnection.CreatureStats.FirstOrDefault(cs => cs.CreatureDbId == creatureDbId);
            }
        }
    }
}