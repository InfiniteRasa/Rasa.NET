using MySql.Data.MySqlClient;

namespace Rasa.Structures
{
    public class MapInfoEntry
    {
        public uint MapContextId { get; set; }
        public string MapName { get; set; }
        public uint MapVersion { get; set; }
        public int BaseRegion { get; set; }

        public static MapInfoEntry Read(MySqlDataReader reader)
        {
            return new MapInfoEntry
            {
                MapContextId = reader.GetUInt32("map_context_id"),
                MapName = reader.GetString("map_name"),
                MapVersion = reader.GetUInt32("map_version"),
                BaseRegion = reader.GetInt32("base_region")

            };
        }
    }
}
