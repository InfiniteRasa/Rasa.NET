using MySql.Data.MySqlClient;

namespace Rasa.Database.Tables.World
{
    public class StarterItemsTable
    {
        private static readonly MySqlCommand GetItemTemplateIdCommand = new MySqlCommand("SELECT classId FROM starter_items WHERE itemTemplateid = @ItemTemplateid");

        public static void Initialize()
        {
            GetItemTemplateIdCommand.Connection = GameDatabaseAccess.WorldConnection;
            GetItemTemplateIdCommand.Parameters.Add("@ItemTemplateid", MySqlDbType.Int32);
            GetItemTemplateIdCommand.Prepare();            
        }

        public static int GetClassId(int itemTemplateId)
        {
            lock (GameDatabaseAccess.WorldLock)
            {
                GetItemTemplateIdCommand.Parameters["@ItemTemplateid"].Value = itemTemplateId;

                using (var reader = GetItemTemplateIdCommand.ExecuteReader())
                    if (reader.Read())
                        return reader.GetInt32("classId");
               
            }

            return 0;
        }       
    }
}
