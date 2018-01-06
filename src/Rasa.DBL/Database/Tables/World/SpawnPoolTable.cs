using System.Collections.Generic;

using MySql.Data.MySqlClient;

namespace Rasa.Database.Tables.World
{
    using Structures;

    public class SpawnPoolTable
    {
        private static readonly MySqlCommand LoadSpawnPoolsCommand = new MySqlCommand("SELECT * FROM spawnpool");

        public static void Initialize()
        {

            LoadSpawnPoolsCommand.Connection = GameDatabaseAccess.WorldConnection;
            LoadSpawnPoolsCommand.Prepare();
        }

        public static List<SpawnPoolEntry> LoadSpawnPool()
        {
            lock (GameDatabaseAccess.WorldLock)
            {
                var spawnPool = new List<SpawnPoolEntry>();

                using (var reader = LoadSpawnPoolsCommand.ExecuteReader())
                    while (reader.Read())
                        spawnPool.Add(SpawnPoolEntry.Read(reader));

                return spawnPool;
            }
        }
    }
}
