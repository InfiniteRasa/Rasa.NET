using System.Collections.Generic;
using System.Linq;

namespace Rasa.Repositories.Char.CharacterGreetingRead
{
    using Context.Char;
    using Structures.Char;

    /// <summary>
    /// The important lines a character has read, by the NPC's creature row
    /// (character_greeting_read). Every call saves at once.
    /// </summary>
    public interface ICharacterGreetingReadRepository
    {
        /// <summary>Creature row to the line of it the character read.</summary>
        Dictionary<uint, uint> Get(uint characterId);

        /// <summary>
        /// Records that the character has read this line of the NPC, in place of any line of
        /// it read before. False when that is what was recorded already.
        /// </summary>
        bool Set(uint characterId, uint creatureId, uint greetingId);

        /// <summary>Forgets that the character has read the NPC. False if it had not.</summary>
        bool Remove(uint characterId, uint creatureId);
    }

    public class CharacterGreetingReadRepository : ICharacterGreetingReadRepository
    {
        private readonly CharContext _charContext;

        public CharacterGreetingReadRepository(CharContext charContext)
        {
            _charContext = charContext;
        }

        public Dictionary<uint, uint> Get(uint characterId) =>
            _charContext.CreateNoTrackingQuery(_charContext.CharacterGreetingReadEntries)
                .Where(e => e.CharacterId == characterId).ToDictionary(e => e.CreatureId, e => e.GreetingId);

        public bool Set(uint characterId, uint creatureId, uint greetingId)
        {
            if (characterId == 0 || creatureId == 0 || greetingId == 0)
                throw new System.ArgumentOutOfRangeException(
                    characterId == 0 ? nameof(characterId) : creatureId == 0 ? nameof(creatureId) : nameof(greetingId));

            var row = _charContext.CreateTrackingQuery(_charContext.CharacterGreetingReadEntries)
                .FirstOrDefault(e => e.CharacterId == characterId && e.CreatureId == creatureId);

            if (row == null)
                _charContext.CharacterGreetingReadEntries.Add(new CharacterGreetingReadEntry(characterId, creatureId, greetingId));
            else if (row.GreetingId == greetingId)
                return false;
            else
                row.GreetingId = greetingId;

            _charContext.SaveChanges();
            return true;
        }

        public bool Remove(uint characterId, uint creatureId)
        {
            var row = _charContext.CreateTrackingQuery(_charContext.CharacterGreetingReadEntries)
                .FirstOrDefault(e => e.CharacterId == characterId && e.CreatureId == creatureId);

            if (row == null)
                return false;

            _charContext.CharacterGreetingReadEntries.Remove(row);
            _charContext.SaveChanges();
            return true;
        }
    }
}
