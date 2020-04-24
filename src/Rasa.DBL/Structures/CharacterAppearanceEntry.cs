using MySql.Data.MySqlClient;

namespace Rasa.Structures
{
    public class CharacterAppearanceEntry
    {
        public uint CharacterId { get; set; }
        public uint Slot { get; set; }
        public uint Class { get; set; }
        public uint Color { get; set; }

        public static CharacterAppearanceEntry Read(MySqlDataReader reader, bool newReader = true)
        {
            if (newReader && !reader.Read())
                return null;

            return new CharacterAppearanceEntry
            {
                CharacterId = reader.GetUInt32("character_id"),
                Slot = reader.GetUInt32("slot"),
                Class = reader.GetUInt32("class"),
                Color = reader.GetUInt32("color")
            };
        }
    }
}
