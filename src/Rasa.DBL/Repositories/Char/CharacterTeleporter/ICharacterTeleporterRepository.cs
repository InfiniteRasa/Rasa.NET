using System.Collections.Generic;

namespace Rasa.Repositories.Char.CharacterTeleporter
{
    using Structures.Char;

    public interface ICharacterTeleporterRepository
    {
        void Add(CharacterTeleporterEntry teleporter);
        List<CharacterTeleporterEntry> Get(uint characterId);
    }
}
