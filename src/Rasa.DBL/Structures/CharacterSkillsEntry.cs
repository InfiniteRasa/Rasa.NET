using MySql.Data.MySqlClient;

namespace Rasa.Structures
{
    public class CharacterSkillsEntry
    {
        public int SkillId { get; set; }
        public int AbilityId { get; set; }
        public int SkillLevel { get; set; }

        public static CharacterSkillsEntry Read(MySqlDataReader reader)
        {
            return new CharacterSkillsEntry
            {
                SkillId = reader.GetInt32("skillId"),
                AbilityId = reader.GetInt32("abilityId"),
                SkillLevel = reader.GetInt32("skillLevel")
            };
        }
    }
}
