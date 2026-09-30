using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Repositories.Char.CharacterActionReuse
{
    using Context.Char;
    using Structures.Char;

    public class CharacterActionReuseRepository : ICharacterActionReuseRepository
    {
        private readonly CharContext _charContext;

        public CharacterActionReuseRepository(CharContext charContext)
        {
            _charContext = charContext;
        }

        public List<CharacterActionReuseEntry> Take(uint characterId)
        {
            try
            {
                var rows = _charContext.CreateTrackingQuery(_charContext.CharacterActionReuseEntries)
                    .Where(e => e.CharacterId == characterId)
                    .ToList();

                if (rows.Count == 0)
                    return rows;

                _charContext.CharacterActionReuseEntries.RemoveRange(rows);
                _charContext.SaveChanges();

                return rows;
            }
            catch (Exception e)
            {
                // A cooldown that cannot be read back is one the player gets early; the login goes on.
                Logger.WriteLog(LogType.Error, $"Could not read the saved cooldowns of character {characterId}: {e}");
                return new List<CharacterActionReuseEntry>();
            }
        }

        public void Replace(uint characterId, IEnumerable<CharacterActionReuseEntry> entries)
        {
            try
            {
                var saved = _charContext.CreateTrackingQuery(_charContext.CharacterActionReuseEntries)
                    .Where(e => e.CharacterId == characterId)
                    .ToDictionary(e => e.ActionId);
                var wanted = entries.ToDictionary(e => e.ActionId);

                // Row by row rather than delete-all-then-add, so no key is ever both removed and
                // added in the one save.
                foreach (var (actionId, row) in saved)
                {
                    if (wanted.TryGetValue(actionId, out var entry))
                        row.ReadyAt = entry.ReadyAt;
                    else
                        _charContext.CharacterActionReuseEntries.Remove(row);
                }

                foreach (var (actionId, entry) in wanted)
                    if (!saved.ContainsKey(actionId))
                        _charContext.CharacterActionReuseEntries.Add(new CharacterActionReuseEntry(characterId, actionId, entry.ReadyAt));

                _charContext.SaveChanges();
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, $"Could not save the cooldowns of character {characterId}: {e}");
            }
        }

        public void DeleteForCharacter(uint characterId)
        {
            var rows = _charContext.CreateTrackingQuery(_charContext.CharacterActionReuseEntries)
                .Where(e => e.CharacterId == characterId);

            _charContext.CharacterActionReuseEntries.RemoveRange(rows);
        }
    }
}
