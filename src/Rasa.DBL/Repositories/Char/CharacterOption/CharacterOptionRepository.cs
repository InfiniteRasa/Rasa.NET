using Rasa.Structures.Char;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rasa.Repositories.Char.CharacterOption
{
    using Context.Char;
    using Structures.Char;

    public class CharacterOptionRepository : ICharacterOptionRepository
    {
        private readonly CharContext _charContext; 

        public CharacterOptionRepository(CharContext charContext)
        {
            _charContext = charContext;
        }

        public void AddOrUpdate(uint characterId, uint optionId, string value)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.CharacterOptionEntries);
            var entry = query.Where(e => e.CharacterId == characterId && e.OptionId == optionId).FirstOrDefault();

            if (entry != null)
            {
                entry.Value = value;
                _charContext.CharacterOptionEntries.Update(entry);
            }
            else
            {
                var newEntry = new CharacterOptionEntry
                {
                    CharacterId = characterId,
                    OptionId = optionId,
                    Value = value
                };

                _charContext.CharacterOptionEntries.Add(newEntry);
            }
        }

        public List<CharacterOptionEntry> Get(uint accountIdid)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.CharacterOptionEntries);
            var characterOptionEntries = query.ToList();

            return characterOptionEntries;
        }
    }
}
