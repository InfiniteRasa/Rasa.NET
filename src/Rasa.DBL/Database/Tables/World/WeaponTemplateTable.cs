using MySql.Data.MySqlClient;

namespace Rasa.Database.Tables.World
{
    using Structures;
    public class WeaponTemplateTable
    {
        private static readonly MySqlCommand GetDbRowsCommand = new MySqlCommand("SELECT COUNT(*) FROM itemtemplate_weapon");
        private static readonly MySqlCommand GetWeaponTemplatesCommand = new MySqlCommand("SELECT * FROM itemtemplate_weapon LIMIT @Row, 1");

        public static void Initialize()
        {
            GetDbRowsCommand.Connection = GameDatabaseAccess.WorldConnection;
            GetDbRowsCommand.Prepare();

            GetWeaponTemplatesCommand.Connection = GameDatabaseAccess.WorldConnection;
            GetWeaponTemplatesCommand.Parameters.Add("@Row", MySqlDbType.Int32);
            GetWeaponTemplatesCommand.Prepare();
        }

        public static long GetDbRows()
        {
            lock (GameDatabaseAccess.WorldLock)
                return (long)GetDbRowsCommand.ExecuteScalar();
        }

        public static WeaponTemplateEntry GetWeaponTemplates(int row)
        {
            lock (GameDatabaseAccess.WorldLock)
            {
                GetWeaponTemplatesCommand.Parameters["@Row"].Value = row;

                using (var reader = GetWeaponTemplatesCommand.ExecuteReader())
                    return WeaponTemplateEntry.Read(reader);
            }
        }
    }
}
