using System.Collections.Generic;

namespace Rasa.Repositories.Char.CharacterActionReuse
{
    using Structures.Char;

    public interface ICharacterActionReuseRepository
    {
        /// <summary>A character's saved cooldowns, removed as they are read: they are the server's again.</summary>
        List<CharacterActionReuseEntry> Take(uint characterId);

        /// <summary>Replaces a character's saved cooldowns with these.</summary>
        void Replace(uint characterId, IEnumerable<CharacterActionReuseEntry> entries);

        /// <summary>Marks a deleted character's rows for removal; saved with the unit of work.</summary>
        void DeleteForCharacter(uint characterId);
    }
}
