using System.Collections.Generic;

using MySql.Data.MySqlClient;

namespace Rasa.Database.Tables.World
{
    using Structures;

    public class ItemTemplateTable
    {
        private static readonly MySqlCommand GetItemTemplatesCommand = new MySqlCommand("SELECT * FROM itemtemplate");

        public static void Initialize()
        {
            GetItemTemplatesCommand.Connection = GameDatabaseAccess.WorldConnection;
            GetItemTemplatesCommand.Prepare();
        }

        public static List<ItemTemplateEntry> GetItemTemplates()
        {
            lock (GameDatabaseAccess.WorldLock)
            {
                var itemTemplateList = new List<ItemTemplateEntry>();
                using (var reader = GetItemTemplatesCommand.ExecuteReader())

                    while (reader.Read())
                        itemTemplateList.Add(ItemTemplateEntry.Read(reader));

                return itemTemplateList;
            }
        }
    }
}
