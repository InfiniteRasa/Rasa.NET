using MySql.Data.MySqlClient;

namespace Rasa.Structures
{
    public class CreatureAppearanceEntry
    {
        public uint CreatureId { get; set; }
        public uint Slot { get; set; }
        public uint Class { get; set; }
        public uint Color { get; set; }

        public static CreatureAppearanceEntry Read(MySqlDataReader reader)
        {
            return new CreatureAppearanceEntry
            {
                CreatureId = reader.GetUInt32("creature_id"),
                Slot = reader.GetUInt32("slot_id"),
                Class = reader.GetUInt32("class_id"),
                Color = reader.GetUInt32("color")
            };
        }
    }
}
