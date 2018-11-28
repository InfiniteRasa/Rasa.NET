using MySql.Data.MySqlClient;

namespace Rasa.Structures
{
    public class CharacterInventoryEntry
    {
        public uint CharacterSlot { get; set; }
        public int InventoryType { get; set; }
        public uint ItemId { get; set; }
        public uint SlotId { get; set; }

        public static CharacterInventoryEntry Read(MySqlDataReader reader)
        {
            return new CharacterInventoryEntry
            {
                CharacterSlot = reader.GetUInt32("characterSlot"),
                InventoryType = reader.GetInt32("inventoryType"),
                ItemId = reader.GetUInt32("itemId"),
                SlotId = reader.GetUInt32("slotId")
            };
        }
    }
}
