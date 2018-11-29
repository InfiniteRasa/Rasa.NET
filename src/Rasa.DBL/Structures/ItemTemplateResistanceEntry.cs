using MySql.Data.MySqlClient;

namespace Rasa.Structures
{
    public class ItemTemplateResistanceEntry
    {
        public uint ItemTemplateId { get; set; }
        public short ResistanceType { get; set; }
        public int ResistanceValue { get; set; }

        public static ItemTemplateResistanceEntry Read(MySqlDataReader reader)
        {
            return new ItemTemplateResistanceEntry
            {
                ItemTemplateId = reader.GetUInt32("itemtemplate_id"),
                ResistanceType = reader.GetInt16("resistance_type"),
                ResistanceValue = reader.GetInt32("resistance_value")
            };
        }
    }
}
