using System.Collections.Generic;

using MySql.Data.MySqlClient;

namespace Rasa.Database.Tables.World
{
    using Structures;

    public class ControlPointTable
    {
        private static readonly MySqlCommand GetControlPointsCommand = new MySqlCommand("SELECT * FROM control_point");

        public static void Initialize()
        {

            GetControlPointsCommand.Connection = GameDatabaseAccess.WorldConnection;
            GetControlPointsCommand.Prepare();
        }

        public static List<ControlPointEntry> GetControlPoints()
        {
            lock (GameDatabaseAccess.WorldLock)
            {
                var controlPoints = new List<ControlPointEntry>();

                using (var reader = GetControlPointsCommand.ExecuteReader())
                    while (reader.Read())
                        controlPoints.Add(ControlPointEntry.Read(reader));

                return controlPoints;
            }
        }
    }
}
