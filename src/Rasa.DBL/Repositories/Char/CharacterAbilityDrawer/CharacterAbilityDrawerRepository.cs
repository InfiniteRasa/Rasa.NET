using System.Collections.Generic;
using System.Linq;

namespace Rasa.Repositories.Char.CharacterAbilityDrawer
{
    using Context.Char;
    using Structures.Char;
    public class CharacterAbilityDrawerRepository : ICharacterAbilityDrawerRepository
    {
        private readonly CharContext _charContext;

        public CharacterAbilityDrawerRepository(CharContext charContext)
        {
            _charContext = charContext;
        }

        public void AddOrUpdate(uint characterId, int abilitySlot, int abilityId, uint abilityLevel)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.CharacterAbilityDrawerEntries);
            var entry = query.Where(e => e.CharacterId == characterId && e.AbilitySlot == abilitySlot).FirstOrDefault();

            if (entry != null)
            {
                entry.AbilityId = abilityId;
                entry.AbilityLevel = abilityLevel;
                _charContext.CharacterAbilityDrawerEntries.Update(entry);
            }
            else
            {
                var newEntry = new CharacterAbilityDrawerEntry(characterId, abilitySlot, abilityId, abilityLevel);

                _charContext.CharacterAbilityDrawerEntries.Add(newEntry);
            }

            _charContext.SaveChanges();
        }

        public List<CharacterAbilityDrawerEntry> GetCharacterAbilities(uint characterId)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.CharacterAbilityDrawerEntries);
            var characterAbilityDrawerEntries = query.Where(e => e.CharacterId == characterId).ToList();

            return characterAbilityDrawerEntries;
        }
    }
}
