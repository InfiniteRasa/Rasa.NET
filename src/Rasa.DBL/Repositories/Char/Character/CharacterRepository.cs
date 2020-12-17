using System;
using Microsoft.EntityFrameworkCore;

namespace Rasa.Repositories.Char.Character
{
    using Context.Char;
    using Structures.Char;

    public class CharacterRepository : ICharacterRepository
    {
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
                Gender = gender
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
            var query = _charContext.CreateNoTrackingQuery(_charContext.CharacterEntries);
            query = query
                .Include(e => e.GameAccount)
                .Include(e => e.CharacterAppearance)
                .Include(e => e.MemberOfClan)
                .ThenInclude(e => e.Clan);
            return _charContext.FindEnsuring(query, id);
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