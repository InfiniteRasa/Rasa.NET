﻿using System.Collections.Generic;

namespace Rasa.Repositories.Char.CharacterAppearance
{
    using Structures.Char;

    public interface ICharacterAppearanceRepository
    {
        bool Add(CharacterEntry character, IEnumerable<CharacterAppearanceEntry> newEntries);
        void AddOrUpdate(uint characterId, CharacterAppearanceEntry newEntry);
        void DeleteForChar(uint characterId);
        List<CharacterAppearanceEntry> GetByCharacterId(uint characterId);
    }
}
