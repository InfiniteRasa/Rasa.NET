using MySql.Data.MySqlClient;

namespace Rasa.Database.Tables.World
{
    public class StarterItemsTable
    {
        private static readonly MySqlCommand GetItemTemplateIdCommand = new MySqlCommand("SELECT itemTemplateId FROM starter_items WHERE classId = @ClassId");

        public static void Initialize()
        {
            GetItemTemplateIdCommand.Connection = GameDatabaseAccess.WorldConnection;
            GetItemTemplateIdCommand.Parameters.Add("@ClassId", MySqlDbType.Int32);
            GetItemTemplateIdCommand.Prepare();            
        }

        public static int GetItemTemplateId(int classId)
        {
            lock (GameDatabaseAccess.WorldLock)
            {
                GetItemTemplateIdCommand.Parameters["@ClassId"].Value = classId;
                using (var reader = GetItemTemplateIdCommand.ExecuteReader())
                    if (reader.Read())
                        return reader.GetInt32("itemTemplateId");
               
            }

            return 0;
        }       
    }
}
