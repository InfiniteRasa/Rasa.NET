using System.Collections.Generic;

using MySql.Data.MySqlClient;

namespace Rasa.Database.Tables.World
{
    using Structures;

    public class VendorItemsTable
    {
        private static readonly MySqlCommand LoadVendorItemsCommand = new MySqlCommand("SELECT * FROM vendor_items");

        public static void Initialize()
        {

            LoadVendorItemsCommand.Connection = GameDatabaseAccess.WorldConnection;
            LoadVendorItemsCommand.Prepare();
        }

        public static List<VendorItemsEntry> LoadVendorItems()
        {
            lock (GameDatabaseAccess.WorldLock)
            {
                var vendorItems = new List<VendorItemsEntry>();

                using (var reader = LoadVendorItemsCommand.ExecuteReader())
                    while (reader.Read())
                        vendorItems.Add(VendorItemsEntry.Read(reader));

                return vendorItems;
            }
        }
    }
}