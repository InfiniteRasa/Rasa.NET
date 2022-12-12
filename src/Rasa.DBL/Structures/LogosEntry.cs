using MySql.Data.MySqlClient;
using System.Numerics;

namespace Rasa.Structures
{
    public class LogosEntry
    {
        public uint Id { get; set; }
        public uint ClassId { get; set; }
        public uint MapContextId { get; set; }
        public Vector3 Position { get; set; }
        public string Name { get; set; }

        public static LogosEntry Read(MySqlDataReader reader)
        {
            return new LogosEntry
            {
                Id = reader.GetUInt32("id"),
                ClassId = reader.GetUInt32("classid"),
                MapContextId = reader.GetUInt32("mapcontextid"),
                Position = new Vector3(reader.GetFloat("posx"), reader.GetFloat("posy") + 2.0f, reader.GetFloat("posz")),
                Name = reader.GetString("name")
            };
        }
    }
}
