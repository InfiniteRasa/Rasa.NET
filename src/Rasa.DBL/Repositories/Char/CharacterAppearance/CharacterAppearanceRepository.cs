using System;
using System.Linq;
using System.Collections.Generic;

namespace Rasa.Repositories.Char.CharacterAppearance
{
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

        public void AddOrUpdate(uint characterId, CharacterAppearanceEntry newEntry)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.CharacterAppearanceEntries);
            var appearanceEntry = query.FirstOrDefault(e => e.CharacterId == characterId && e.Slot == newEntry.Slot);

            if (appearanceEntry != null)
            {
                appearanceEntry.Class = newEntry.Class;
                appearanceEntry.Color = newEntry.Color;

                _charContext.CharacterAppearanceEntries.Update(appearanceEntry);
            }
            else
            {
                newEntry.CharacterId = characterId;

                _charContext.CharacterAppearanceEntries.Add(newEntry);
            }
        }

        public void DeleteForChar(uint characterId)
        {
            var characterAppearances = _charContext.CreateTrackingQuery(_charContext.CharacterAppearanceEntries)
                .Where(e => e.CharacterId == characterId);
            _charContext.CharacterAppearanceEntries.RemoveRange(characterAppearances);
        }

        public List<CharacterAppearanceEntry> GetByCharacterId(uint characterId)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.CharacterAppearanceEntries);
            var characterAppearances = query.Where(e => e.CharacterId == characterId).ToList();
            if (characterAppearances == null)
            {
                throw new EntityNotFoundException(nameof(CharacterAppearanceEntry), $"{nameof(CharacterEntry.Id)}", $"{characterId}");
            }
            return characterAppearances;
        }
    }
}
