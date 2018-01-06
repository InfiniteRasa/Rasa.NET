using System.Collections.Generic;

using MySql.Data.MySqlClient;

namespace Rasa.Database.Tables.World
{
    using Structures;

    public class CreatureTable
    {
        private static readonly MySqlCommand LoadCreaturesCommand = new MySqlCommand("SELECT * FROM creatures");

        public static void Initialize()
        {

            LoadCreaturesCommand.Connection = GameDatabaseAccess.WorldConnection;
            LoadCreaturesCommand.Prepare();
        }

        public static List<CreaturesEntry> LoadCreatures()
        {
            lock (GameDatabaseAccess.WorldLock)
            {
                var creatures = new List<CreaturesEntry>();

                using (var reader = LoadCreaturesCommand.ExecuteReader())
                    while (reader.Read())
                        creatures.Add(CreaturesEntry.Read(reader));

                return creatures;
            }
        }
    }
}