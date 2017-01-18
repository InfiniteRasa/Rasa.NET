using MySql.Data.MySqlClient;

namespace Rasa.Database.Tables.World
{
    using Structures;

    public class ItemTemplateTable
    {
        private static readonly MySqlCommand GetClassIdCommand = new MySqlCommand("SELECT classId FROM itemtemplate WHERE itemTemplateId = @ItemTemplateId");
        private static readonly MySqlCommand GetDbRowsCommand = new MySqlCommand("SELECT COUNT(*) FROM itemtemplate");
        /*private static readonly MySqlCommand GetItemTemplatesCommand = new MySqlCommand("SELECT itemTemplateId, classId, qualityId, `type`, hasSellableFlag, notTradeableFlag, hasCharacterUniqueFlag, "+
                                                                                        "hasAccountUniqueFlag, hasBoEFlag, boundToCharacterFlag, notPlaceableInLockBoxFlag, inventoryCategory, buyPrice, "+
                                                                                        "sellPrice,reqLevel,stacksize FROM itemTemplate");
        */
        private static readonly MySqlCommand GetItemTemplatesCommand = new MySqlCommand("SELECT * FROM itemtemplate LIMIT @Row, 1");

        public static void Initialize()
        {
            GetClassIdCommand.Connection = GameDatabaseAccess.WorldConnection;
            GetClassIdCommand.Parameters.Add("@ItemTemplateId", MySqlDbType.UInt32);
            GetClassIdCommand.Prepare();

            GetDbRowsCommand.Connection = GameDatabaseAccess.WorldConnection;
            GetDbRowsCommand.Prepare();

            GetItemTemplatesCommand.Connection = GameDatabaseAccess.WorldConnection;
            GetItemTemplatesCommand.Parameters.Add("@Row", MySqlDbType.Int32);
            GetItemTemplatesCommand.Prepare();
        }

        public static int GetClassId(int itemTemplateId)
        {
            lock (GameDatabaseAccess.WorldLock)
            {
                GetClassIdCommand.Parameters["@ItemTemplateId"].Value = (uint)itemTemplateId;

                using (var reader = GetClassIdCommand.ExecuteReader())
                    if (reader.Read())
                        return reader.GetInt32("classId");
            }

            return 0;
        }

        public static long GetDbRows()
        {
            lock ( GameDatabaseAccess.WorldLock)
            {
                return (long)GetDbRowsCommand.ExecuteScalar();
            }
        }

        public static ItemTemplateEntry GetItemTemplates(int row)
        {
            lock (GameDatabaseAccess.WorldLock)
            {
                GetItemTemplatesCommand.Parameters["@Row"].Value = row;

                using (var reader = GetItemTemplatesCommand.ExecuteReader())
                    return ItemTemplateEntry.Read(reader);
            }
        }
    }
}
