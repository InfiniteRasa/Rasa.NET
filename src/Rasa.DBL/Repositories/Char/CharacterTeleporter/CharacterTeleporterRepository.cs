using System.Collections.Generic;
using System.Linq;

namespace Rasa.Repositories.Char.CharacterTeleporter
{
    using Context.Char;
    using Structures.Char;

    public class CharacterTeleporterRepository : ICharacterTeleporterRepository
    {
        private readonly CharContext _charContext;

        public CharacterTeleporterRepository(CharContext charContext)
        {
            _charContext = charContext;
        }

        public void Add(CharacterTeleporterEntry teleporter)
        {
            _charContext.CharacterTeleporterEntries.Add(teleporter);
            _charContext.SaveChanges();
        }

        public List<CharacterTeleporterEntry> Get(uint characterId)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.CharacterTeleporterEntries);
            var entries = query.Where(e => e.CharacterId == characterId).ToList();

            return entries;
        }
    }
}
