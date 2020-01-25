using System.Collections.Generic;

namespace Rasa.Database.Tables.World
{
    using Structures;

    public class ControlPointTable
    {
        public static List<ControlPointEntry> GetControlPoints()
        {
            lock (GameDatabaseAccess.WorldLock)
            {
                // TODO
                //return GameDatabaseAccess.WorldConnection.ControlPoint.Where(_ => true).ToList();
                return null;
            }
        }
    }
}
