using System.Collections.Generic;
using Rasa.Structures.Char;

namespace Rasa.Repositories.Char.CharacterFlag
{
    public interface ICharacterFlagRepository
    {
        IReadOnlyDictionary<uint, uint> Get(uint characterId);
        uint? GetValue(uint characterId, uint flagId);
        bool HasValue(uint characterId, uint flagId, uint value = 1);
        void Set(uint characterId, uint flagId, uint value);
        void Add(CharacterFlagEntry entry);
        void Remove(uint characterId, uint flagId);
    }
}
