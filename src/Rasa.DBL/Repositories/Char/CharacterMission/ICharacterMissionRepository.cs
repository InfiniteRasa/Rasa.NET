using Rasa.Structures.Char;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rasa.Repositories.Char.CharacterMission
{
    public interface ICharacterMissionRepository
    {
        List<CharacterMissionEntry> Get(uint accountId, uint characterSlot);
    }
}
