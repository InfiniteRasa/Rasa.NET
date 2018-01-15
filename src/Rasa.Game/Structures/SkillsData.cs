namespace Rasa.Structures
{
    public class SkillsData
    {
        public int SkillId { get; set; }
        public int AbilityId { get; set; }
        public int SkillLevel { get; set; }

        public SkillsData(int skillId, int abilityId, int skillLevel)
        {
            SkillId = skillId;
            AbilityId = abilityId;
            SkillLevel = skillLevel;
        }

        // used for itemTemplate
        public SkillsData(int skillId, int skillLevel)
        {
            SkillId = skillId;
            SkillLevel = skillLevel;
        }
    }
}
