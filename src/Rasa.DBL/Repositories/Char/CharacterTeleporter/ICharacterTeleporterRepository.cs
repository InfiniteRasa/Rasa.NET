using System.Collections.Generic;

namespace Rasa.Repositories.Char.CharacterTeleporter
{
    public interface ICharacterTeleporterRepository
    {
        void Add(uint characterId, uint waypointId);
        List<uint> Get(uint characterId);
    }
}
