using System.Collections.Generic;

namespace Rasa.Repositories.Char.CharacterActionReuse
{
    using Structures.Char;

    public interface ICharacterActionReuseRepository
    {
        /// <summary>
        /// A character's saved cooldowns. They stay in the table until the next save replaces
        /// them: a character that is loaded and never saved again - its connection gone before
        /// the world had it, the server stopped without its shutdown - has them still.
        /// </summary>
        List<CharacterActionReuseEntry> Get(uint characterId);

        /// <summary>Replaces a character's saved cooldowns with these.</summary>
        void Replace(uint characterId, IEnumerable<CharacterActionReuseEntry> entries);

        /// <summary>Marks a deleted character's rows for removal; saved with the unit of work.</summary>
        void DeleteForCharacter(uint characterId);
    }
}
