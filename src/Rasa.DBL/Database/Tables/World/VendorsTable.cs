using System.Collections.Generic;

using MySql.Data.MySqlClient;

namespace Rasa.Database.Tables.World
{
    using Structures;

    public class VendorsTable
    {
        private static readonly MySqlCommand LoadVendorsCommand = new MySqlCommand("SELECT * FROM vendors");

        public static void Initialize()
        {

            LoadVendorsCommand.Connection = GameDatabaseAccess.WorldConnection;
            LoadVendorsCommand.Prepare();
        }

        public static List<VendorsEntry> LoadVendors()
        {
            lock (GameDatabaseAccess.WorldLock)
            {
                var vendors = new List<VendorsEntry>();

                using (var reader = LoadVendorsCommand.ExecuteReader())
                    while (reader.Read())
                        vendors.Add(VendorsEntry.Read(reader));

                return vendors;
            }
        }
    }
}