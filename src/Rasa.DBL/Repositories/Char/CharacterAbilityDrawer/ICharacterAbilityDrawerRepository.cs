using System.Collections.Generic;

namespace Rasa.Repositories.Char.CharacterAbilityDrawer
{
    using Structures.Char;
    public interface ICharacterAbilityDrawerRepository
    {
        /// <param name="itemId">The item (item.id) whose action the slot holds; null for a skill's ability.</param>
        void AddOrUpdate(uint characterId, int abilitySlotId, int abilityId, uint abilityLevel, uint? itemId = null);
        List<CharacterAbilityDrawerEntry> GetCharacterAbilities(uint characterId);
    }
}
