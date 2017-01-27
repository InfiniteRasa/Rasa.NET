using MySql.Data.MySqlClient;

namespace Rasa.Structures
{
    public class EquipmentTemplateEntry
    {
        public int ItemTemplateId { get; set; }
        public int SlotType { get; set; }
        public int RequiredSkillId { get; set; }
        public int RequiredSkillMinVal { get; set; }

        public static EquipmentTemplateEntry Read(MySqlDataReader reader)
        {
            return new EquipmentTemplateEntry
            {
                ItemTemplateId = reader.GetInt32("itemTemplateId"),
                SlotType = reader.GetInt32("slotType"),
                RequiredSkillId = reader.GetInt32("requiredSkillId"),
                RequiredSkillMinVal = reader.GetInt32("requiredSkillMinVal")
            };
        }
    }
}
