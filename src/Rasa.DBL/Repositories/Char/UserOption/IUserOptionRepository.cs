using System.Collections.Generic;

namespace Rasa.Repositories.Char.UserOption
{
    using Structures.Char;
    public interface IUserOptionRepository
    {
        void AddOrUpdate(uint characterId, uint optionId, string value);
        List<UserOptionEntry> Get(uint id);

        /// <summary>
        /// The account's options become exactly these: rows for options not among them are
        /// removed, the rest added or updated. The client sends the whole set of options that
        /// differ from their defaults each time, so one set back to its default is one missing
        /// from the save.
        /// </summary>
        void Replace(uint accountId, IReadOnlyCollection<(uint OptionId, string Value)> options);
    }
}
