using System.Collections.Generic;
using System.Linq;

namespace Rasa.Repositories.Char.CharacterTeleporter
{
    using Context.Char;
    using Rasa.Structures.Char;

    public class CharacterTeleporterRepository : ICharacterTeleporterRepository
    {
        private readonly CharContext _charContext;

        public CharacterTeleporterRepository(CharContext charContext)
        {
            _charContext = charContext;
        }

        public void Add(uint characterId, uint waypointId)
        {
            var entry = new CharacterTeleporterEntry(characterId, waypointId);

            _charContext.CharacterTeleporterEntries.Add(entry);
            _charContext.SaveChanges();
        }

        public List<uint> Get(uint characterId)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.CharacterTeleporterEntries);
            var entries = query.Where(e => e.CharacterId == characterId).Select(e => e.WaypointId).ToList();

            return entries;
        }
    }
}
