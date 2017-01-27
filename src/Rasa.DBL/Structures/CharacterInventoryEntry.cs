using MySql.Data.MySqlClient;

namespace Rasa.Structures
{
    public class CharacterInventoryEntry
    {
        public uint ItemId { get; set; }
        public int SlotId { get; set; }

        public static CharacterInventoryEntry Read(MySqlDataReader reader)
        {
            return new CharacterInventoryEntry
            {
                ItemId = reader.GetUInt32("itemId"),
                SlotId = reader.GetInt32("slotId")
            };
        }
    }
}
