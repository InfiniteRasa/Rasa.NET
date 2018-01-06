using System.Collections.Generic;

using MySql.Data.MySqlClient;

namespace Rasa.Database.Tables.World
{
    using Structures;

    public class CreatureTypesTable
    {
        private static readonly MySqlCommand LoadCreaturesTypesCommand = new MySqlCommand("SELECT * FROM creature_types");

        public static void Initialize()
        {

            LoadCreaturesTypesCommand.Connection = GameDatabaseAccess.WorldConnection;
            LoadCreaturesTypesCommand.Prepare();
        }

        public static List<CreatureTypesEntry> LoadCreatureTypes()
        {
            lock (GameDatabaseAccess.WorldLock)
            {
                var creatures = new List<CreatureTypesEntry>();

                using (var reader = LoadCreaturesTypesCommand.ExecuteReader())
                    while (reader.Read())
                        creatures.Add(CreatureTypesEntry.Read(reader));

                return creatures;
            }
        }
    }
}
