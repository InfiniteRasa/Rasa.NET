using System.Collections.Generic;

namespace Rasa.Repositories.Char.CharacterTitle
{
    public interface ICharacterTitleRepository
    {
        List<uint> Get(uint characterId);
    }
}
