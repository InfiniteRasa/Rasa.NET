using MySql.Data.MySqlClient;

namespace Rasa.Structures
{
    public class FootlockerEntry
    {
        public uint Id { get; set; }
        public uint EntityClassId { get; set; }
        public uint MapContextId { get; set; }
        public float CoordX { get; set; }
        public float CoordY { get; set; }
        public float CoordZ { get; set; }
        public float Orientation { get; set; }
        public string Comment { get; set; }

        public static FootlockerEntry Read(MySqlDataReader reader)
        {
            return new FootlockerEntry
            {
                Id = reader.GetUInt32("id"),
                EntityClassId = reader.GetUInt32("entity_class_id"),
                MapContextId = reader.GetUInt32("map_context_id"),
                CoordX = reader.GetFloat("coord_x"),
                CoordY = reader.GetFloat("coord_y"),
                CoordZ = reader.GetFloat("coord_z"),
                Orientation = reader.GetFloat("orientation"),
                Comment = reader.GetString("comment")
            };
        }
    }
}
