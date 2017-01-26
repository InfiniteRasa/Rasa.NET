using MySql.Data.MySqlClient;

namespace Rasa.Structures
{
    public class ItemsEntry
    {
        public int EntityClassId { get; set; }
        public int StackSize { get; set; }
        public int Color { get; set; }
        public int AmmoCount { get; set; }
        public string CrafterName { get; set; }

        public static ItemsEntry Read(MySqlDataReader reader)
        {
            if (!reader.Read())
                return null;

            return new ItemsEntry
            {
                EntityClassId = reader.GetInt32("entityClassId"),
                StackSize = reader.GetInt32("stackSize"),
                Color = reader.GetInt32("color"),
                AmmoCount = reader.GetInt32("ammoCount"),
                CrafterName = reader.GetString("crafterName")
            };
        }
    }
}
