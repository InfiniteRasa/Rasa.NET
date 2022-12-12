using System.Collections.Generic;

using MySql.Data.MySqlClient;

namespace Rasa.Database.Tables.World
{
    using MySql.Data.MySqlClient;
    using Structures;

    public class LogosTable
    {
        private static readonly MySqlCommand LoadLogosCommand = new MySqlCommand("SELECT * FROM logos");

        public static void Initialize()
        {

            LoadLogosCommand.Connection = GameDatabaseAccess.WorldConnection;
            LoadLogosCommand.Prepare();
        }

        public static List<LogosEntry> LoadLogos()
        {
            lock (GameDatabaseAccess.WorldLock)
            {
                var logos = new List<LogosEntry>();

                using (var reader = LoadLogosCommand.ExecuteReader())
                    while (reader.Read())
                        logos.Add(LogosEntry.Read(reader));

                return logos;
            }
        }
    }
}
