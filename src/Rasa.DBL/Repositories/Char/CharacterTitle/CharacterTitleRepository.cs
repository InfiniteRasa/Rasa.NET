using System.Collections.Generic;
using System.Linq;

namespace Rasa.Repositories.Char.CharacterTitle
{
    using Context.Char;

    public class CharacterTitleRepository : ICharacterTitleRepository
    {
        private readonly CharContext _charContext;

        public CharacterTitleRepository(CharContext charContext)
        {
            _charContext = charContext;
        }

        public List<uint> Get(uint characterId)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.CharacterTitleEntries);
            var entries = query.Where(e => e.CharacterId == characterId).Select(e => e.TitleId).ToList();

            return entries;
        }
    }
}
