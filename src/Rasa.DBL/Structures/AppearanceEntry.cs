using MySql.Data.MySqlClient;

namespace Rasa.Structures
{
    public class AppearanceEntry
    {
        public int SlotId { get; set; }
        public int ClassId { get; set; }
        public int Color { get; set; }

        public static AppearanceEntry Read(MySqlDataReader reader)
        {
            return new AppearanceEntry
            {
                SlotId = reader.GetInt32("slotId"),
                ClassId = reader.GetInt32("classId"),
                Color = reader.GetInt32("color")
            };
        }
    }
}
