using MySql.Data.MySqlClient;

namespace Rasa.Structures
{
    public class EquipableClassEntry
    {
        public int ClassId { get; set; }
        public int SlotId { get; set; }

        public static EquipableClassEntry Read(MySqlDataReader reader)
        {
            return new EquipableClassEntry
            {
                ClassId = reader.GetInt32("classId"),
                SlotId = reader.GetInt32("slotId")
            };
        }
    }
}
