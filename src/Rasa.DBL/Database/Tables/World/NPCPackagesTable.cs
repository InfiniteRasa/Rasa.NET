using System.Collections.Generic;

using MySql.Data.MySqlClient;

namespace Rasa.Database.Tables.World
{
    using Structures;

    public class NPCPackagesTable
    {
        private static readonly MySqlCommand LoadNPCPackagesCommand = new MySqlCommand("SELECT * FROM npc_packages");

        public static void Initialize()
        {

            LoadNPCPackagesCommand.Connection = GameDatabaseAccess.WorldConnection;
            LoadNPCPackagesCommand.Prepare();
        }

        public static List<NPCPackagesEntry> LoadNPCPackages()
        {
            lock (GameDatabaseAccess.WorldLock)
            {
                var packages = new List<NPCPackagesEntry>();

                using (var reader = LoadNPCPackagesCommand.ExecuteReader())
                    while (reader.Read())
                        packages.Add(NPCPackagesEntry.Read(reader));

                return packages;
            }
        }
    }
}
