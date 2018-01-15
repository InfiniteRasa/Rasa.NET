using MySql.Data.MySqlClient;

namespace Rasa.Structures
{
    public class ItemTemplateRequirementsEntry
    {
        public int ItemTemplateId { get; set; }
        public short ReqType { get; set; }
        public short ReqValue { get; set; }

        public static ItemTemplateRequirementsEntry Read(MySqlDataReader reader)
        {
            return new ItemTemplateRequirementsEntry
            {
                ItemTemplateId = reader.GetInt32("itemTemplateid"),
                ReqType = reader.GetInt16("reqType"),
                ReqValue  = reader.GetInt16("reqValue")
            };
        }
    }
}
