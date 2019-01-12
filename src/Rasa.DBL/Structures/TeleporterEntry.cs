using MySql.Data.MySqlClient;

namespace Rasa.Structures
{
    public class TeleporterEntry
    {
        public uint Id { get; set; }
        public uint EntityClassId { get; set; }
        public uint Type { get; set; }
        public string Description { get; set; }
        public float CoordX { get; set; }
        public float CoordY { get; set; }
        public float CoordZ { get; set; }
        public float Rotation { get; set; }
        public uint MapContextId { get; set; }

        public static TeleporterEntry Read(MySqlDataReader reader)
        {
            return new TeleporterEntry
            {
                Id = reader.GetUInt32("id"),
                EntityClassId = reader.GetUInt32("entity_class_id"),
                Type = reader.GetUInt32("type"),
                Description = reader.GetString("description"),
                CoordX = reader.GetFloat("coord_x"),
                CoordY = reader.GetFloat("coord_y"),
                CoordZ = reader.GetFloat("coord_z"),
                Rotation = reader.GetFloat("rotation"),
                MapContextId = reader.GetUInt32("map_context_id")
            };
        }
    }
}
