using System.Collections.Generic;

using MySql.Data.MySqlClient;

namespace Rasa.Database.Tables.World
{
    using Structures;

    public class TeleporterTable
    {
        private static readonly MySqlCommand GetTeleportersCommand = new MySqlCommand("SELECT * FROM teleporter");

        public static void Initialize()
        {

            GetTeleportersCommand.Connection = GameDatabaseAccess.WorldConnection;
            GetTeleportersCommand.Prepare();
        }

        public static List<TeleporterEntry> GetTeleporters()
        { 
            lock (GameDatabaseAccess.WorldLock)
            {
                var teleporters = new List<TeleporterEntry>();

                using (var reader = GetTeleportersCommand.ExecuteReader())
                    while (reader.Read())
                        teleporters.Add(TeleporterEntry.Read(reader));

                return teleporters;
            }
        }
    }
}
