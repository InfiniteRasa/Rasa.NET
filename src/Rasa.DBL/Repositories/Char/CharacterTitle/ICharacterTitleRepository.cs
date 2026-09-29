using System.Collections.Generic;

namespace Rasa.Repositories.Char.CharacterTitle
{
    public interface ICharacterTitleRepository
    {
        List<uint> Get(uint characterId);

        /// <summary>Gives the character the title; false when they have it already.</summary>
        bool Add(uint characterId, uint titleId);
    }
}
