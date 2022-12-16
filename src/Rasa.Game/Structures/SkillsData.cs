namespace Rasa.Structures
{
    using Data;

    public class SkillsData
    {
        public SkillId SkillId { get; set; }
        public int AbilityId { get; set; }
        public int SkillLevel { get; set; }

        public SkillsData(SkillId skillId, int abilityId, int skillLevel)
        {
            SkillId = skillId;
            AbilityId = abilityId;
            SkillLevel = skillLevel;
        }

        // used for itemTemplate
        public SkillsData(SkillId skillId, int skillLevel)
        {
            SkillId = skillId;
            SkillLevel = skillLevel;
        }
    }
}
