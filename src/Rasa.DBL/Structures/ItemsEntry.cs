using MySql.Data.MySqlClient;

namespace Rasa.Structures
{
    public class ItemsEntry
    {
        public uint ItemTemplateId { get; set; }
        public int StackSize { get; set; }
        public int CurrentHitPoints { get; set; }
        public uint Color { get; set; }
        public int AmmoCount { get; set; }
        public string CrafterName { get; set; }

        public static ItemsEntry Read(MySqlDataReader reader)
        {
            if (!reader.Read())
                return null;

            return new ItemsEntry
            {
                ItemTemplateId = reader.GetUInt32("itemTemplateId"),
                StackSize = reader.GetInt32("stackSize"),
                CurrentHitPoints = reader.GetInt32("currentHitPoints"),
                Color = reader.GetUInt32("color"),
                AmmoCount = reader.GetInt32("ammoCount"),
                CrafterName = reader.GetString("crafterName")
            };
        }
    }
}
