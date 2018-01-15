using System.Collections.Generic;

using MySql.Data.MySqlClient;

namespace Rasa.Database.Tables.World
{
    using Structures;

    public class ItemTemplateRaceRequiremenTable
    {
        private static readonly MySqlCommand GetItemTemplateRaceRequiremenCommand = new MySqlCommand("SELECT * FROM itemtemplate_racerequirement");

        public static void Initialize()
        {

            GetItemTemplateRaceRequiremenCommand.Connection = GameDatabaseAccess.WorldConnection;
            GetItemTemplateRaceRequiremenCommand.Prepare();
        }

        public static List<ItemTemplateRaceRequirementEntry> GetItemTemplateRaceRequirement()
        {
            lock (GameDatabaseAccess.WorldLock)
            {
                var itemTemplateRaceRequirement = new List<ItemTemplateRaceRequirementEntry>();

                using (var reader = GetItemTemplateRaceRequiremenCommand.ExecuteReader())
                    while (reader.Read())
                        itemTemplateRaceRequirement.Add(ItemTemplateRaceRequirementEntry.Read(reader));

                return itemTemplateRaceRequirement;
            }
        }
    }
}
