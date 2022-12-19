using System.Collections.Generic;

namespace Rasa.Database.Tables.World
{
    using MySql.Data.MySqlClient;
    using Structures;

    public class MapInfoTable
    {
        private static readonly MySqlCommand LoadMapInfoCommand = new MySqlCommand("SELECT * FROM map_info");

        public static void Initialize()
        {

            LoadMapInfoCommand.Connection = GameDatabaseAccess.WorldConnection;
            LoadMapInfoCommand.Prepare();
        }

        public static List<MapInfoEntry> LoadMapInfo()
        {
            lock (GameDatabaseAccess.WorldLock)
            {
                var mapInfo = new List<MapInfoEntry>();

                using (var reader = LoadMapInfoCommand.ExecuteReader())
                    while (reader.Read())
                        mapInfo.Add(MapInfoEntry.Read(reader));

                return mapInfo;
            }
        }
    }
}
