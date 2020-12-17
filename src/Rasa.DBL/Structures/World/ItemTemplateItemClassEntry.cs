using MySql.Data.MySqlClient;

namespace Rasa.Structures.World
{
    public class ItemTemplateItemClassEntry
    {
        public uint ItemTemplateId { get; set; }
        public uint ItemClass { get; set; }

        public static ItemTemplateItemClassEntry Read(MySqlDataReader reader)
        {
            return new ItemTemplateItemClassEntry
            {
                ItemTemplateId = reader.GetUInt32("itemTemplateid"),
                ItemClass = reader.GetUInt32("itemClassId")
            };
        }
    }
}
