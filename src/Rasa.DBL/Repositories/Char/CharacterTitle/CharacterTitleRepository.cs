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

        public bool Add(uint characterId, uint titleId)
        {
            if (characterId == 0 || titleId == 0)
                throw new System.ArgumentOutOfRangeException(characterId == 0 ? nameof(characterId) : nameof(titleId));

            if (_charContext.CreateNoTrackingQuery(_charContext.CharacterTitleEntries)
                .Any(e => e.CharacterId == characterId && e.TitleId == titleId))
                return false;

            _charContext.CharacterTitleEntries.Add(new Structures.Char.CharacterTitleEntry(characterId, titleId));
            _charContext.SaveChanges();
            return true;
        }
    }
}
