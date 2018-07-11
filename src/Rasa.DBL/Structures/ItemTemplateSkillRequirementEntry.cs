using MySql.Data.MySqlClient;

namespace Rasa.Structures
{
    public class ItemTemplateSkillRequirementEntry
    {
        public uint ItemTemplateId { get; set; }
        public short SkillId { get; set; }
        public short SkillLevel { get; set; }

        public static ItemTemplateSkillRequirementEntry Read(MySqlDataReader reader)
        {
            return new ItemTemplateSkillRequirementEntry
            {
                ItemTemplateId = reader.GetUInt32("itemTemplateid"),
                SkillId = reader.GetInt16("skillId"),
                SkillLevel = reader.GetInt16("skillLevel")
            };
        }
    }
}
