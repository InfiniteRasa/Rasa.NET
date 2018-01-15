using System.Collections.Generic;

using MySql.Data.MySqlClient;

namespace Rasa.Database.Tables.World
{
    using Structures;

    public class ItemClassTable
    {
        private static readonly MySqlCommand LoadItemClassesCommand = new MySqlCommand("SELECT * FROM itemClass");

        public static void Initialize()
        {

            LoadItemClassesCommand.Connection = GameDatabaseAccess.WorldConnection;
            LoadItemClassesCommand.Prepare();
        }

        public static List<ItemClassEntry> LoadItemClasses()
        {
            lock (GameDatabaseAccess.WorldLock)
            {
                var itemClasses = new List<ItemClassEntry>();

                using (var reader = LoadItemClassesCommand.ExecuteReader())
                    while (reader.Read())
                        itemClasses.Add(ItemClassEntry.Read(reader));

                return itemClasses;
            }
        }
    }
}
