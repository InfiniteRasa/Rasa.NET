using System.Collections.Generic;

using MySql.Data.MySqlClient;

namespace Rasa.Database.Tables.World
{
    using Structures;

    public class ArmorTemplateTable
    {
        private static readonly MySqlCommand GetArmorTemplatesCommand = new MySqlCommand("SELECT * FROM itemtemplate_armor");

        public static void Initialize()
        {
            GetArmorTemplatesCommand.Connection = GameDatabaseAccess.WorldConnection;
            GetArmorTemplatesCommand.Prepare();
        }

        public static List<ArmorTemplateEntry> GetArmorTemplates()
        {
            lock (GameDatabaseAccess.WorldLock)
            {
                var armorTemplates = new List<ArmorTemplateEntry>();
                using (var reader = GetArmorTemplatesCommand.ExecuteReader())
                    while (reader.Read())
                        armorTemplates.Add(ArmorTemplateEntry.Read(reader));

                return armorTemplates;
            }
        }
    }
}
