using MySql.Data.MySqlClient;

namespace Rasa.Structures
{
    public class ClanInventoryEntry
    {
        public uint ItemId { get; set; }
        public uint SlotId { get; set; }

        public static ClanInventoryEntry Read(MySqlDataReader reader)
        {
            return new ClanInventoryEntry
            {
                ItemId = reader.GetUInt32("itemId"),
                SlotId = reader.GetUInt32("slotId")
            };
        }
    }
}
