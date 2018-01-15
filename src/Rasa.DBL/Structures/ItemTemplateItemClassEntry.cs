using MySql.Data.MySqlClient;

namespace Rasa.Structures
{
    public class ItemTemplateItemClassEntry
    {
        public int ItemTemplateId { get; set; }
        public int ItemClassId { get; set; }

        public static ItemTemplateItemClassEntry Read(MySqlDataReader reader)
        {
            return new ItemTemplateItemClassEntry
            {
                ItemTemplateId = reader.GetInt32("itemTemplateid"),
                ItemClassId = reader.GetInt32("itemClassId")
            };
        }
    }
}
