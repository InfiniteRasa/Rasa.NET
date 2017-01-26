using MySql.Data.MySqlClient;

namespace Rasa.Database.Tables.World
{
    using Structures;

    public class ArmorTemplateTable
    {
        private static readonly MySqlCommand GetDbRowsCommand = new MySqlCommand("SELECT COUNT(*) FROM itemtemplate_armor");
        private static readonly MySqlCommand GetArmorTemplatesCommand = new MySqlCommand("SELECT * FROM itemtemplate_armor LIMIT @Row, 1");

        public static void Initialize()
        {
            GetDbRowsCommand.Connection = GameDatabaseAccess.WorldConnection;
            GetDbRowsCommand.Prepare();

            GetArmorTemplatesCommand.Connection = GameDatabaseAccess.WorldConnection;
            GetArmorTemplatesCommand.Parameters.Add("@Row", MySqlDbType.Int32);
            GetArmorTemplatesCommand.Prepare();
        }

        public static long GetDbRows()
        {
            lock (GameDatabaseAccess.WorldLock)
                return (long)GetDbRowsCommand.ExecuteScalar();
        }

        public static ArmorTemplateEntry GetArmorTemplates(int row)
        {
            lock (GameDatabaseAccess.WorldLock)
            {
                GetArmorTemplatesCommand.Parameters["@Row"].Value = row;

                using (var reader = GetArmorTemplatesCommand.ExecuteReader())
                    return ArmorTemplateEntry.Read(reader);
            }
        }
    }
}
