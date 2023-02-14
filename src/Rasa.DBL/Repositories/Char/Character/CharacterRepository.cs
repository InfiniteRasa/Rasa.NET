using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

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
        private const byte DefaultRunState = 1;

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
                RunState = DefaultRunState,
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

        public void SaveCharacter(ICharacterChange characterChange)
        {
            var entry = _charContext.GetWritableEnsuring(_charContext.CharacterEntries, characterChange.Id);
            entry.RunState = characterChange.IsRunning ? (byte)1 : (byte)0;
            entry.CrouchState = characterChange.IsCrouching ? (byte)1 : (byte)0;

            entry.CoordX = characterChange.Position.X;
            entry.CoordY = characterChange.Position.Y;
            entry.CoordZ = characterChange.Position.Z;
            entry.Rotation = characterChange.Rotation;
            entry.MapContextId= characterChange.MapContextId;
        }

        public void UpdateCharacterAttributes(uint id, int spentBody, int spentMind, int spentSpirit)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.CharacterEntries);
            var entry = query.Where(e => e.Id == id).FirstOrDefault();

            entry.Body = spentBody;
            entry.Mind = spentMind;
            entry.Spirit = spentSpirit;

            _charContext.CharacterEntries.Update(entry);
            _charContext.SaveChanges();
        }

        public void UpdateCharacterClass(uint id, uint classId)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.CharacterEntries);
            var entry = query.Where(e => e.Id == id).FirstOrDefault();

            entry.Class = classId;
 
            _charContext.CharacterEntries.Update(entry);
            _charContext.SaveChanges();
        }

        public void UpdateCharacterCloneCredits(uint id, uint cloneCredits)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.CharacterEntries);
            var entry = query.Where(e => e.Id == id).FirstOrDefault();

            entry.CloneCredits = cloneCredits;

            _charContext.CharacterEntries.Update(entry);
            _charContext.SaveChanges();
        }

        public void UpdateCharacterCredits(uint id, int credits)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.CharacterEntries);
            var entry = query.Where(e => e.Id == id).FirstOrDefault();

            entry.Credit = credits;

            _charContext.CharacterEntries.Update(entry);
            _charContext.SaveChanges();
        }

        public void UpdateCharacterExpirience(uint id, uint experience)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.CharacterEntries);
            var entry = query.Where(e => e.Id == id).FirstOrDefault();

            entry.Experience = experience;

            _charContext.CharacterEntries.Update(entry);
            _charContext.SaveChanges();
        }

        public void UpdateCharacterLevel(uint id, byte level)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.CharacterEntries);
            var entry = query.Where(e => e.Id == id).FirstOrDefault();

            entry.Level = level;

            _charContext.CharacterEntries.Update(entry);
            _charContext.SaveChanges();
        }

        public void UpdateCharacterLogin(uint id, uint totalTimePlayed, uint numLogins)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.CharacterEntries);
            var entry = query.Where(e => e.Id == id).FirstOrDefault();

            entry.LastLogin = DateTime.UtcNow;
            entry.TotalTimePlayed = totalTimePlayed;
            entry.NumLogins = numLogins;

            _charContext.CharacterEntries.Update(entry);
            _charContext.SaveChanges();
        }

        public void UpdateCharacterPosition(uint id, double x, double y, double z, double rotation, uint mapContextId)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.CharacterEntries);
            var entry = query.Where(e => e.Id == id).FirstOrDefault();

            entry.CoordX = x;
            entry.CoordY = y;
            entry.CoordZ = z;
            entry.Rotation = rotation;
            entry.MapContextId = mapContextId;

            _charContext.CharacterEntries.Update(entry);
            _charContext.SaveChanges();
        }

        public void UpdateCharacterActiveWeapon(uint id, byte activeWeapon)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.CharacterEntries);
            var entry = query.Where(e => e.Id == id).FirstOrDefault();

            entry.ActiveWeapon = activeWeapon;

            _charContext.CharacterEntries.Update(entry);
            _charContext.SaveChanges();
        }
    }
}