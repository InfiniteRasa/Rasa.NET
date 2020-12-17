using System.Collections.Generic;

using MySql.Data.MySqlClient;

namespace Rasa.Database.Tables.World
{
    using Structures;
    using Structures.World;

    public class ItemTemplateItemClassTable
    {
        private static readonly MySqlCommand GetItemClassIdCommand = new MySqlCommand("SELECT itemclassid FROM itemtemplate_itemclass WHERE itemtemplateid = @Id");
        private static readonly MySqlCommand GetItemTemplateItemClassCommand = new MySqlCommand("SELECT * FROM itemtemplate_itemclass");

        public static void Initialize()
        {
            GetItemClassIdCommand.Connection = GameDatabaseAccess.WorldConnection;
            GetItemClassIdCommand.Parameters.Add("@Id", MySqlDbType.Int32);
            GetItemClassIdCommand.Prepare();

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

        public static uint GetItemClassId(uint id)
        {
            lock (GameDatabaseAccess.WorldLock)
            {
                GetItemClassIdCommand.Parameters["@Id"].Value = id;

                using (var reader = GetItemClassIdCommand.ExecuteReader())
                    if (reader.Read())
                        return reader.GetUInt32("itemclassid");

                return 0;
            }
        }
    }
}
