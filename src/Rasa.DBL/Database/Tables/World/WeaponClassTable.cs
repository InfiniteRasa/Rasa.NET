using System.Collections.Generic;

using MySql.Data.MySqlClient;

namespace Rasa.Database.Tables.World
{
    using Structures;

    public class WeaponClassTable
    {
        private static readonly MySqlCommand LoadWeaponClassesCommand = new MySqlCommand("SELECT * FROM weaponClass");

        public static void Initialize()
        {

            LoadWeaponClassesCommand.Connection = GameDatabaseAccess.WorldConnection;
            LoadWeaponClassesCommand.Prepare();
        }

        public static List<WeaponClassEntry> LoadWeaponClasses()
        {
            lock (GameDatabaseAccess.WorldLock)
            {
                var weaponClasses = new List<WeaponClassEntry>();

                using (var reader = LoadWeaponClassesCommand.ExecuteReader())
                    while (reader.Read())
                        weaponClasses.Add(WeaponClassEntry.Read(reader));

                return weaponClasses;
            }
        }
    }
}
