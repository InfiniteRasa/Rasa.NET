using System.Collections.Generic;
using System.Linq;

namespace Rasa.Database.Tables.World
{
    using Structures;

    public class CreatureTable
    {
        public static List<CreaturesEntry> LoadCreatures()
        {
            lock (GameDatabaseAccess.WorldLock)
            {
                return GameDatabaseAccess.WorldConnection.Creatures.Where(_ => true).ToList();
            }
        }
    }
}