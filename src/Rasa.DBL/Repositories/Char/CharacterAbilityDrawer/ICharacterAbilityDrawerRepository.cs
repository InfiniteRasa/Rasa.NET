using System.Collections.Generic;

namespace Rasa.Repositories.Char.CharacterAbilityDrawer
{
    using Structures.Char;
    public interface ICharacterAbilityDrawerRepository
    {
        void AddOrUpdate(uint characterId, int abilitySlotId, int abilityId, uint abilityLevel);
        List<CharacterAbilityDrawerEntry> GetCharacterAbilities(uint characterId);
    }
}
