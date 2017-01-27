using System.Collections.Generic;

using MySql.Data.MySqlClient;

namespace Rasa.Database.Tables.World
{
    using Structures;
    public class WeaponTemplateTable
    {
        private static readonly MySqlCommand GetWeaponTemplatesCommand = new MySqlCommand("SELECT * FROM itemtemplate_weapon");

        public static void Initialize()
        {
            GetWeaponTemplatesCommand.Connection = GameDatabaseAccess.WorldConnection;
            GetWeaponTemplatesCommand.Prepare();
        }

        public static List<WeaponTemplateEntry> GetWeaponTemplates()
        {
            lock (GameDatabaseAccess.WorldLock)
            {
                var weaponTemplates = new List<WeaponTemplateEntry>();
                using (var reader = GetWeaponTemplatesCommand.ExecuteReader())
                    while (reader.Read())
                        weaponTemplates.Add(WeaponTemplateEntry.Read(reader));

                return weaponTemplates;
            }
        }
    }
}
