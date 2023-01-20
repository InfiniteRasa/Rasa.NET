using System.Collections.Generic;

namespace Rasa.Repositories.Char.CharacterSkills
{
    using Structures.Char;
    public interface ICharacterSkillsRepository
    {
        void AddOrUpdate(uint characterId, uint skillId, int abilityId, int skillLevel);
        List<CharacterSkillsEntry> GetCharacterSkills(uint characterId);
    }
}
