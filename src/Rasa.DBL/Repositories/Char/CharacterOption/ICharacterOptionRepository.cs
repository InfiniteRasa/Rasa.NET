using System.Collections.Generic;

namespace Rasa.Repositories.Char.CharacterOption
{
    using Structures.Char;

    public interface ICharacterOptionRepository
    {
        void AddOrUpdate(uint accountId, uint optionId, string value);
        List<CharacterOptionEntry> Get(uint id);
    }
}
