using System.Collections.Generic;

namespace Rasa.Repositories.Char.CharacterOption
{
    using Structures.Char;

    public interface ICharacterOptionRepository
    {
        void AddOrUpdate(uint accountId, uint optionId, string value);
        List<CharacterOptionEntry> Get(uint id);

        /// <summary>
        /// The character's options become exactly these: rows for options not among them are
        /// removed, the rest added or updated. The client sends the whole set of options that
        /// differ from their defaults each time, so one set back to its default is one missing
        /// from the save.
        /// </summary>
        void Replace(uint characterId, IReadOnlyCollection<(uint OptionId, string Value)> options);
    }
}
