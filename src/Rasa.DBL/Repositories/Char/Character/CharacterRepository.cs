using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.EntityFrameworkCore;

namespace Rasa.Repositories.Char.Character
{
    using Context.Char;
    using Structures.Char;

    public class CharacterRepository : ICharacterRepository
    {
        private const uint DefaultMapContextId = 1220;
        private const double DefaultCoordX = 894.9d;
        private const double DefaultCoordY = 307.9d;
        private const double DefaultCoordZ = 347.1d;

        private readonly CharContext _charContext;

        public CharacterRepository(CharContext charContext)
        {
            _charContext = charContext;
        }

        public CharacterEntry Create(GameAccountEntry account, byte slot, string characterName, byte race, double scale, byte gender)
        {
            var entry = new CharacterEntry
            {
                AccountId = account.Id,
                Slot = slot,
                Name = characterName,
                Race = race,
                Scale = scale,
                Gender = gender,
                Class = 1,
                MapContextId = DefaultMapContextId,
                CoordX = DefaultCoordX,
                CoordY = DefaultCoordY,
                CoordZ = DefaultCoordZ,
                Rotation = 0
            };

            try
            {
                _charContext.CharacterEntries.Add(entry);
                _charContext.SaveChanges();
                return Get(entry.Id);
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, "Error creating character:");
                Logger.WriteLog(LogType.Error, e);
                return null;
            }
        }

        public CharacterEntry Get(uint id)
        {
            var query = CreateCharacterQuery();
            return _charContext.FindEnsuring(query, id);
        }

        public IDictionary<byte, CharacterEntry> GetByAccountId(uint accountEntryId)
        {
            var query = CreateCharacterQuery();
            var characters = query.Where(e => e.AccountId == accountEntryId);
            return characters.ToDictionary(c => c.Slot, c => c);
        }

        public CharacterEntry GetByAccountId(uint accountEntryId, byte slot)
        {
            var query = CreateCharacterQuery();
            var character = query.FirstOrDefault(e => e.AccountId == accountEntryId && e.Slot == slot);
            if (character == null)
            {
                throw new EntityNotFoundException(nameof(CharacterEntry), $"{nameof(CharacterEntry.AccountId)}.{nameof(CharacterEntry.Slot)}", $"{accountEntryId}-{slot}");
            }
            return character;
        }

        private IQueryable<CharacterEntry> CreateCharacterQuery()
        {

            var query = _charContext.CreateNoTrackingQuery(_charContext.CharacterEntries);
            query = query
                .Include(e => e.GameAccount)
                .Include(e => e.CharacterAppearance)
                .Include(e => e.MemberOfClan)
                .ThenInclude(e => e.Clan);
            return query;
        }

        public void Delete(uint id)
        {
            var entry = _charContext.GetWritableEnsuring(_charContext.CharacterEntries, id);
            _charContext.Remove(entry);
        }

        public void UpdateLoginData(uint id)
        {
            var entry = _charContext.GetWritableEnsuring(_charContext.CharacterEntries, id);
            entry.LastLogin = DateTime.UtcNow;
        }
    }
}