using System.Collections.Generic;

namespace Rasa.Repositories.Char.Character
{
    using Structures.Char;

    public interface ICharacterRepository
    {
        CharacterEntry Create(GameAccountEntry account, byte slot, string characterName, byte race, double scale, byte gender);

        CharacterEntry Get(uint id);

        IDictionary<byte, CharacterEntry> GetByAccountId(uint accountEntryId);

        CharacterEntry GetByAccountId(uint accountEntryId, byte slot);

        void Delete(uint id);

        void UpdateLoginData(uint id);

        void SaveCharacter(ICharacterChange characterChange);
    }
}
    }
}