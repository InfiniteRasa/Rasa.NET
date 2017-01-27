using System.Collections.Generic;

using MySql.Data.MySqlClient;

namespace Rasa.Database.Tables.World
{
    using Structures;

    public class ItemTemplateTable
    {
        private static readonly MySqlCommand GetClassIdCommand = new MySqlCommand("SELECT classId FROM itemtemplate WHERE itemTemplateId = @ItemTemplateId");
        private static readonly MySqlCommand GetItemTemplatesCommand = new MySqlCommand("SELECT * FROM itemtemplate");

        public static void Initialize()
        {
            GetClassIdCommand.Connection = GameDatabaseAccess.WorldConnection;
            GetClassIdCommand.Parameters.Add("@ItemTemplateId", MySqlDbType.UInt32);
            GetClassIdCommand.Prepare();

            GetItemTemplatesCommand.Connection = GameDatabaseAccess.WorldConnection;
            GetItemTemplatesCommand.Prepare();
        }

        public static int GetClassId(int itemTemplateId)
        {
            lock (GameDatabaseAccess.WorldLock)
            {
                GetClassIdCommand.Parameters["@ItemTemplateId"].Value = itemTemplateId;

                using (var reader = GetClassIdCommand.ExecuteReader())
                    if (reader.Read())
                        return reader.GetInt32("classId");
            }

            return 0;
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
