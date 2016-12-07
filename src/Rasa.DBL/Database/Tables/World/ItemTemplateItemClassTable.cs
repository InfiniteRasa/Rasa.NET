using MySql.Data.MySqlClient;

namespace Rasa.Database.Tables.World
{
    public class ItemTemplateItemClassTable
    {
        private static readonly MySqlCommand GetClassIdCommand= new MySqlCommand("SELECT itemClass FROM itemtemplate_itemclass WHERE `itemTemplate` = @ItemTemplate");

        public static void Initialize()
        {
            GetClassIdCommand.Connection = GameDatabaseAccess.WorldConnection;
            GetClassIdCommand.Parameters.Add("@ItemTemplate", MySqlDbType.Int32);
            GetClassIdCommand.Prepare();
        }

        public static int GetClassId(int itemTemplate)
        {
            lock (GameDatabaseAccess.WorldLock)
            {
                GetClassIdCommand.Parameters["@ItemTemplate"].Value = itemTemplate;

                using (var reader = GetClassIdCommand.ExecuteReader())
                    if (reader.Read())
                        return reader.GetInt32("itemClass");
            }

            return 0;
        }
    }
}
