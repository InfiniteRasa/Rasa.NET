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

        public void Replace(uint characterId, IReadOnlyCollection<(uint OptionId, string Value)> options)
        {
            var wanted = options.ToDictionary(option => option.OptionId, option => option.Value ?? string.Empty);
            var existing = _charContext.CharacterOptionEntries.Where(e => e.CharacterId == characterId).ToList();

            foreach (var entry in existing)
            {
                if (wanted.Remove(entry.OptionId, out var value))
                    entry.Value = value;
                else
                    _charContext.CharacterOptionEntries.Remove(entry);
            }

            foreach (var (optionId, value) in wanted)
                _charContext.CharacterOptionEntries.Add(new CharacterOptionEntry { CharacterId = characterId, OptionId = optionId, Value = value });
        }

        public List<CharacterOptionEntry> Get(uint characterId)
        {
            // This ignored its argument and returned the whole table, which went unnoticed
            // only because the table stayed empty: nothing ever completed the unit of work
            // that wrote to it. Now that saves land, every character would have been sent
            // every other character's options.
            var query = _charContext.CreateNoTrackingQuery(_charContext.CharacterOptionEntries);

            return query.Where(e => e.CharacterId == characterId).ToList();
        }
    }
}
