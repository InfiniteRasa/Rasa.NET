using System.Collections.Generic;
using System.Linq;

namespace Rasa.Repositories.Char.CharacterMission
{
    using Context.Char;
    using Structures.Char;
    public class CharacterMissionRepository : ICharacterMissionRepository
    {
        private readonly CharContext _charContext;

        public CharacterMissionRepository(CharContext charContext)
        {
            _charContext = charContext;
        }

        public List<CharacterMissionEntry> Get(uint accountId, uint characterSlot)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.CharacterMissionEntries);
            var missions = query.ToList();

            return missions;
        }
    }
}
