using System.Collections.Generic;

namespace Rasa.Repositories.Char.CharacterLogos
{
    public interface ICharacterLogosRepository
    {
        List<uint> GetLogos(uint characterId);
        void SetLogos(uint characterId, uint logosId);
    }
}
