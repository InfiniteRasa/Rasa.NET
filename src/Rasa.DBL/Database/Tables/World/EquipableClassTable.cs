using System.Collections.Generic;

using MySql.Data.MySqlClient;

namespace Rasa.Database.Tables.World
{
    using Structures;

    public class EquipableClassTable
    {
        private static readonly MySqlCommand LoadEquipableClassCommand = new MySqlCommand("SELECT * FROM equipableclass");  // this is actaly 'equipableClassEquipmentSlot' from eqipmentdata.pyo

        public static void Initialize()
        {

            LoadEquipableClassCommand.Connection = GameDatabaseAccess.WorldConnection;
            LoadEquipableClassCommand.Prepare();
        }

        public static List<EquipableClassEntry> LoadEquipableClasses()
        {
            lock (GameDatabaseAccess.WorldLock)
            {
                var equipableClasses = new List<EquipableClassEntry>();

                using (var reader = LoadEquipableClassCommand.ExecuteReader())
                    while (reader.Read())
                        equipableClasses.Add(EquipableClassEntry.Read(reader));

                return equipableClasses;
            }
        }
    }
}
