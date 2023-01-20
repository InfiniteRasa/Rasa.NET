using System.Collections.Generic;

namespace Rasa.Repositories.Char.UserOption
{
    using Structures.Char;
    public interface IUserOptionRepository
    {
        void AddOrUpdate(uint characterId, uint optionId, string value);
        List<UserOptionEntry> Get(uint id);
    }
}
