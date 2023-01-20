using System.Collections.Generic;
using System.Linq;

namespace Rasa.Repositories.Char.CharacterSkills
{
    using Context.Char;
    using Structures.Char;


    public class CharacterSkillsRepository : ICharacterSkillsRepository
    {
        private readonly CharContext _charContext;

        public CharacterSkillsRepository(CharContext charContext)
        {
            _charContext = charContext;
        }

        public List<CharacterSkillsEntry> GetCharacterSkills(uint characterId)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.CharacterSkillsEntries);
            var characterSkills = query.Where(e => e.CharacterId == characterId).ToList();

            return characterSkills;
        }

        public void AddOrUpdate(uint characterId, uint skillId, int abilityId, int skillLevel)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.CharacterSkillsEntries);
            var existingSkill = query.Where(e => e.CharacterId == characterId && e.SkillId == skillId).FirstOrDefault();

            if (existingSkill != null)
            {
                existingSkill.AbilityId = abilityId;
                existingSkill.SkillLevel = skillLevel;
                _charContext.CharacterSkillsEntries.Update(existingSkill);
            }
            else
            {
                var skill = new CharacterSkillsEntry(characterId, skillId, abilityId, skillLevel);

                _charContext.CharacterSkillsEntries.Add(skill);
            }

            _charContext.SaveChanges();
        }
    }
}
