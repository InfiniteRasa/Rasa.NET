using System.Collections.Generic;

namespace Rasa.Repositories.Char.CharacterAppearance
{
    using Structures;

    public interface ICharacterAppearanceRepository
    {
        bool Add(CharacterEntry character, IEnumerable<CharacterAppearanceEntry> newEntries);
    }
}