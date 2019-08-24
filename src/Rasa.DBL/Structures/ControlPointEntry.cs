using MySql.Data.MySqlClient;

namespace Rasa.Structures
{
    public class ControlPointEntry
    {
        public uint ControlPointId { get; set; }

        public static ControlPointEntry Read(MySqlDataReader reader)
        {
            return new ControlPointEntry
            {
                ControlPointId = reader.GetUInt32("control_point_id")
            };
        }
    }
}
