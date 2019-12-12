using System.Collections.Generic;

using MySql.Data.MySqlClient;

namespace Rasa.Database.Tables.World
{
    using Structures;

    public class FootlockersTable
    {
        private static readonly MySqlCommand LoadFootlockersCommand = new MySqlCommand("SELECT * FROM footlockers");

        public static void Initialize()
        {

            LoadFootlockersCommand.Connection = GameDatabaseAccess.WorldConnection;
            LoadFootlockersCommand.Prepare();
        }

        public static List<FootlockerEntry> LoadFootlockers()
        {
            lock (GameDatabaseAccess.WorldLock)
            {
                var footlockers = new List<FootlockerEntry>();

                using (var reader = LoadFootlockersCommand.ExecuteReader())
                    while (reader.Read())
                        footlockers.Add(FootlockerEntry.Read(reader));

                return footlockers;
            }
        }
    }
}
