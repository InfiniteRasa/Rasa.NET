using System.Collections.Generic;
using System.Linq;

namespace Rasa.Database.Tables.World
{
    using Structures;

    public class FootlockersTable
    {
        public static List<FootlockerEntry> LoadFootlockers()
        {
            lock (GameDatabaseAccess.WorldLock)
            {
                return GameDatabaseAccess.WorldConnection.Footlockers.Where(_ => true).ToList();
            }
        }
    }
}
