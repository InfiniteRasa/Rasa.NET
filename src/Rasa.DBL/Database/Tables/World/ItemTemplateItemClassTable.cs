using System.Collections.Generic;

using MySql.Data.MySqlClient;

namespace Rasa.Database.Tables.World
{
    using Structures;

    public class ItemTemplateItemClassTable
    {
        private static readonly MySqlCommand GetItemTemplateItemClassCommand = new MySqlCommand("SELECT * FROM itemtemplate_itemclass");

        public static void Initialize()
        {

            GetItemTemplateItemClassCommand.Connection = GameDatabaseAccess.WorldConnection;
            GetItemTemplateItemClassCommand.Prepare();
        }

        public static List<ItemTemplateItemClassEntry> GetItemTemplateItemClass()
        {
            lock (GameDatabaseAccess.WorldLock)
            {
                var itemTemplateItemClass = new List<ItemTemplateItemClassEntry>();

                using (var reader = GetItemTemplateItemClassCommand.ExecuteReader())
                    while (reader.Read())
                        itemTemplateItemClass.Add(ItemTemplateItemClassEntry.Read(reader));

                return itemTemplateItemClass;
            }
        }
    }
}
