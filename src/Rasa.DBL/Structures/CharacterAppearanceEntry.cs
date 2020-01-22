using MySql.Data.MySqlClient;

namespace Rasa.Structures
{
    public class CharacterAppearanceEntry
    {
        public uint CharacterId { get; set; }
        public uint Slot { get; set; }
        public uint Class { get; set; }
        public uint Color { get; set; }

        public CharacterAppearanceEntry(uint characterId, uint slot, uint classId, uint color)
        {
            CharacterId = characterId;
            Slot = slot;
            Class = classId;
            Color = color;
        }

        public static CharacterAppearanceEntry Read(MySqlDataReader reader, bool newReader = true)
        {
            if (newReader && !reader.Read())
                return null;

            return new CharacterAppearanceEntry(
                reader.GetUInt32("character_id"),
                reader.GetUInt32("slot"),
                reader.GetUInt32("class"),
                reader.GetUInt32("color")
            );
        }
    }
}
