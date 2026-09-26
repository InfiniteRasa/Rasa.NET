using System;
using System.Linq;

namespace Rasa.Repositories.Char.CharacterStartingExperience
{
    using Context.Char;
    using Structures.Char;

    public class CharacterStartingExperienceRepository : ICharacterStartingExperienceRepository
    {
        private readonly CharContext _charContext;

        public CharacterStartingExperienceRepository(CharContext charContext)
        {
            _charContext = charContext;
        }

        public CharacterStartingExperienceEntry Get(uint characterId) =>
            _charContext.CharacterStartingExperienceEntries.SingleOrDefault(entry =>
                entry.CharacterId == characterId);

        public CharacterStartingExperienceState? ReadState(uint characterId) =>
            _charContext.CreateNoTrackingQuery(_charContext.CharacterStartingExperienceEntries)
                .Where(entry => entry.CharacterId == characterId)
                .Select(entry => (CharacterStartingExperienceState?)entry.State).SingleOrDefault();

        public void Add(CharacterStartingExperienceEntry entry)
        {
            _charContext.CharacterStartingExperienceEntries.Add(entry);
            _charContext.SaveChanges();
        }

        public void SetState(uint characterId, CharacterStartingExperienceState state)
        {
            var entry = Get(characterId);
            if (entry == null)
                throw new InvalidOperationException("Character starting experience does not exist.");

            entry.State = state;
            _charContext.SaveChanges();
        }

        public bool TrySetState(
            uint characterId,
            CharacterStartingExperienceState expectedState,
            CharacterStartingExperienceState state)
        {
            var entry = Get(characterId);
            if (entry == null)
                throw new InvalidOperationException("Character starting experience does not exist.");

            if (entry.State != expectedState)
                return false;

            entry.State = state;
            _charContext.SaveChanges();
            return true;
        }
    }
}
