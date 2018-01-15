using System.Collections.Generic;

using MySql.Data.MySqlClient;

namespace Rasa.Database.Tables.World
{
    using Structures;

    public class ItemTemplateRequirementsTable
    {
        private static readonly MySqlCommand GetItemTemplateRequirementsCommand = new MySqlCommand("SELECT * FROM itemtemplate_requirements");

        public static void Initialize()
        {

            GetItemTemplateRequirementsCommand.Connection = GameDatabaseAccess.WorldConnection;
            GetItemTemplateRequirementsCommand.Prepare();
        }

        public static List<ItemTemplateRequirementsEntry> GetItemTemplateRequirements()
        {
            lock (GameDatabaseAccess.WorldLock)
            {
                var ItemTemplateRequirements = new List<ItemTemplateRequirementsEntry>();

                using (var reader = GetItemTemplateRequirementsCommand.ExecuteReader())
                    while (reader.Read())
                        ItemTemplateRequirements.Add(ItemTemplateRequirementsEntry.Read(reader));

                return ItemTemplateRequirements;
            }
        }
    }
}
