using System.Collections.Generic;
using System.Linq;

namespace Rasa.Repositories.Char.CharacterBossKill
{
    using Context.Char;
    using Structures.Char;

    /// <summary>The bosses a character has killed, by creature name id. Every call saves at once.</summary>
    public interface ICharacterBossKillRepository
    {
        List<uint> Get(uint characterId);

        /// <summary>Records the character's kill of the boss; false when it is recorded already.</summary>
        bool Add(uint characterId, uint creatureNameId);
    }

    public class CharacterBossKillRepository : ICharacterBossKillRepository
    {
        private readonly CharContext _charContext;

        public CharacterBossKillRepository(CharContext charContext)
        {
            _charContext = charContext;
        }

        public List<uint> Get(uint characterId) =>
            _charContext.CreateNoTrackingQuery(_charContext.CharacterBossKillEntries)
                .Where(e => e.CharacterId == characterId).Select(e => e.CreatureNameId).ToList();

        public bool Add(uint characterId, uint creatureNameId)
        {
            if (characterId == 0 || creatureNameId == 0)
                throw new System.ArgumentOutOfRangeException(characterId == 0 ? nameof(characterId) : nameof(creatureNameId));

            if (_charContext.CreateNoTrackingQuery(_charContext.CharacterBossKillEntries)
                .Any(e => e.CharacterId == characterId && e.CreatureNameId == creatureNameId))
                return false;

            _charContext.CharacterBossKillEntries.Add(new CharacterBossKillEntry(characterId, creatureNameId));
            _charContext.SaveChanges();
            return true;
        }
    }
}
