using System.Collections.Generic;
using System.Linq;

namespace Rasa.Repositories.Char.CensorWord
{
    using Context.Char;

    public class CensoredWordRepository : ICensoredWordRepository
    {
        private readonly CharContext _charContext;

        public CensoredWordRepository(CharContext charContext)
        {
            _charContext = charContext;
        }

        public List<string> GetCensoredWords()
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.CensorWordsEntries);
            var entries = query.Select(e => e.Word).ToList();

            return entries;
        }
    }
}
