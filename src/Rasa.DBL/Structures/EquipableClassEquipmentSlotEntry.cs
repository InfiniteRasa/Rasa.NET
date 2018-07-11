using MySql.Data.MySqlClient;

namespace Rasa.Structures
{
    public class EquipableClassEntry
    {
        public uint ClassId { get; set; }
        public uint SlotId { get; set; }

        public static EquipableClassEntry Read(MySqlDataReader reader)
        {
            return new EquipableClassEntry
            {
                ClassId = reader.GetUInt32("classId"),
                SlotId = reader.GetUInt32("slotId")
            };
        }
    }
}
