using System.Collections.Generic;

using MySql.Data.MySqlClient;

namespace Rasa.Database.Tables.World
{
    using Structures;

    public class EquipmentTemplateTable
    {
        private static readonly MySqlCommand GetEquipmentTemplatesCommand = new MySqlCommand("SELECT * FROM itemtemplate_equipment");

        public static void Initialize()
        {

            GetEquipmentTemplatesCommand.Connection = GameDatabaseAccess.WorldConnection;
            GetEquipmentTemplatesCommand.Prepare();
        }

        public static List<EquipmentTemplateEntry> GetEquipmentTemplates()
        {
            lock (GameDatabaseAccess.WorldLock)
            {
                var equiopmentTemplates = new List<EquipmentTemplateEntry>();
                using (var reader = GetEquipmentTemplatesCommand.ExecuteReader())
                    while (reader.Read())
                        equiopmentTemplates.Add(EquipmentTemplateEntry.Read(reader));

                return equiopmentTemplates;
            }
        }
    }
}
