using System.Collections.Generic;
using System.Linq;

namespace Rasa.Database.Tables.World
{
    using Structures;

    public class CreatureActionTable
    {

        public static List<CreatureActionEntry> GetCreatureActions()
        {
            lock (GameDatabaseAccess.WorldLock)
            {
                return GameDatabaseAccess.WorldConnection.CreatureAction.Where(_ => true).ToList();
            }
        }
    }
}
