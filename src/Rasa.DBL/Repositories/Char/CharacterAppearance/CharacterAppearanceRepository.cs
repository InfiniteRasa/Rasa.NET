using System.Collections.Generic;

namespace Rasa.Repositories.Char.CharacterAppearance
{
    using System;
    using System.Linq;
    using Context.Char;
    using Structures.Char;

    public class CharacterAppearanceRepository : ICharacterAppearanceRepository
    {
        private readonly CharContext _charContext;

        public CharacterAppearanceRepository(CharContext charContext)
        {
            _charContext = charContext;
        }

        public bool Add(CharacterEntry character, IEnumerable<CharacterAppearanceEntry> newEntries)
        {
            if (!_charContext.CharacterEntries.Contains(character))
            {
                throw new InvalidOperationException("Error while adding character appearances. Character entry not yet added to database.");
            }
            try
            {
                foreach (var characterAppearanceEntry in newEntries)
                {
                    characterAppearanceEntry.CharacterId = character.Id;
                    _charContext.Add(characterAppearanceEntry);
                }
                return true;
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, "Error adding appearances:");
                Logger.WriteLog(LogType.Error, e);
                _charContext.ChangeTracker.Clear();
                return false;
            }
        }

        public void DeleteForChar(uint characterId)
        {
            var characterAppearances = _charContext.CreateTrackingQuery(_charContext.CharacterAppearanceEntries)
                .Where(e => e.CharacterId == characterId);
            _charContext.CharacterAppearanceEntries.RemoveRange(characterAppearances);
        }
    }
}