using System.Collections.Generic;

using MySql.Data.MySqlClient;

namespace Rasa.Database.Tables.World
{
    using Structures;

    public class ItemTemplateResistanceTable
    {
        private static readonly MySqlCommand GetItemTemplateResistanceCommand = new MySqlCommand("SELECT * FROM itemtemplate_resistance");

        public static void Initialize()
        {

            GetItemTemplateResistanceCommand.Connection = GameDatabaseAccess.WorldConnection;
            GetItemTemplateResistanceCommand.Prepare();
        }

        public static List<ItemTemplateResistanceEntry> GetItemTemplateResistance()
        {
            lock (GameDatabaseAccess.WorldLock)
            {
                var itemTemplateResistance = new List<ItemTemplateResistanceEntry>();

                using (var reader = GetItemTemplateResistanceCommand.ExecuteReader())
                    while (reader.Read())
                        itemTemplateResistance.Add(ItemTemplateResistanceEntry.Read(reader));

                return itemTemplateResistance;
            }
        }
    }
}
