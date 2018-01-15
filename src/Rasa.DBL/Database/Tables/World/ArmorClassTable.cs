using System.Collections.Generic;

using MySql.Data.MySqlClient;

namespace Rasa.Database.Tables.World
{
    using Structures;

    public class ArmorClassTable
    {
        private static readonly MySqlCommand LoadArmorClassesCommand = new MySqlCommand("SELECT * FROM armorClass");

        public static void Initialize()
        {

            LoadArmorClassesCommand.Connection = GameDatabaseAccess.WorldConnection;
            LoadArmorClassesCommand.Prepare();
        }

        public static List<ArmorClassEntry> LoadArmorClasses()
        {
            lock (GameDatabaseAccess.WorldLock)
            {
                var armorClasses = new List<ArmorClassEntry>();

                using (var reader = LoadArmorClassesCommand.ExecuteReader())
                    while (reader.Read())
                        armorClasses.Add(ArmorClassEntry.Read(reader));

                return armorClasses;
            }
        }
    }
}