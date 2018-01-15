using System.Collections.Generic;

using MySql.Data.MySqlClient;

namespace Rasa.Database.Tables.World
{
    using Structures;

    public class EntityClassTable
    {
        private static readonly MySqlCommand LoadEntityClassCommand = new MySqlCommand("SELECT * FROM entityClass");

        public static void Initialize()
        {

            LoadEntityClassCommand.Connection = GameDatabaseAccess.WorldConnection;
            LoadEntityClassCommand.Prepare();
        }

        public static List<EntityClassEntry> LoadEntityClass()
        {
            lock (GameDatabaseAccess.WorldLock)
            {
                var entityClasses = new List<EntityClassEntry>();

                using (var reader = LoadEntityClassCommand.ExecuteReader())
                    while (reader.Read())
                        entityClasses.Add(EntityClassEntry.Read(reader));

                return entityClasses;
            }
        }
    }
}
