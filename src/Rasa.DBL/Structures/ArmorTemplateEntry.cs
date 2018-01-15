using MySql.Data.MySqlClient;

namespace Rasa.Structures
{
    public class ArmorTemplateEntry
    {
        public int ItemTemplateId { get; set; }
        public int ArmorValue { get; set; }

        public static ArmorTemplateEntry Read(MySqlDataReader reader)
        {
            return new ArmorTemplateEntry
            {
                ItemTemplateId = reader.GetInt32("itemTemplateId"),
                ArmorValue = reader.GetInt32("armorValue")
            };
        }
    }
}
