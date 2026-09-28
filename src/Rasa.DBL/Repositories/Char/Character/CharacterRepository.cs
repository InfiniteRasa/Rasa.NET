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

        /// <summary>
        /// The character with this id, or null when there is no such row. Get throws instead,
        /// which is right for a caller holding an id it has just read off a live session, and
        /// wrong for one holding an id that outlived the character: a deleted character leaves
        /// rows behind that still name it, and those callers need an answer rather than an
        /// exception thrown through them.
        /// </summary>
        public CharacterEntry Find(uint id)
        {
            var query = CreateCharacterQuery();
            return _charContext.Find(query, id);
        }

        public IDictionary<byte, CharacterEntry> GetByAccountId(uint accountEntryId)
        {
            var query = CreateCharacterQuery();
            var characters = query.Where(e => e.AccountId == accountEntryId).OrderBy(e => e.Id);

            // Not ToDictionary: that throws on a duplicate slot, and this runs inside the login
            // handler, so one bad row used to disconnect the account at every login. Rows written
            // before the slot check and the unique index can still be duplicated; the oldest
            // character keeps the pod and the rest are reported so they can be moved by hand.
            var bySlot = new Dictionary<byte, CharacterEntry>();

            foreach (var character in characters)
            {
                if (bySlot.TryAdd(character.Slot, character))
                    continue;

                Logger.WriteLog(LogType.Error,
                    $"Account {accountEntryId} has more than one character in slot {character.Slot}; "
                    + $"character {character.Id} ({character.Name}) is hidden behind {bySlot[character.Slot].Id}. "
                    + "Move it to a free slot: UPDATE `character` SET slot = <n> WHERE id = " + character.Id + ";");
            }

            return bySlot;
        }

        /// <summary>
        /// The character in one of an account's pods, or null when the pod is empty. An empty
        /// pod is an ordinary answer - an account with no character in its selected slot, a
        /// switch to a slot nothing was created in - so this does not throw; it used to, and
        /// every caller checked for null instead, so the check never ran and the throw took
        /// the connection down.
        /// </summary>
        public CharacterEntry GetByAccountId(uint accountEntryId, byte slot)
        {
            var query = CreateCharacterQuery();

            return query.FirstOrDefault(e => e.AccountId == accountEntryId && e.Slot == slot);
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

        /// <summary>
        /// The tracked row for one character, or null. The update methods below used to load a
        /// no-tracking snapshot, change a field and hand the whole object to Update(), which
        /// marks every column modified: a row that had been changed by another thread in the
        /// meantime (SaveCharacter from a socket close, the console gm command) was overwritten
        /// with the stale snapshot, and a missing row was a NullReferenceException in whichever
        /// packet handler asked. A tracked row writes only the columns that changed.
        /// </summary>
        private CharacterEntry GetWritable(uint id)
        {
            var entry = _charContext.GetWritable(_charContext.CharacterEntries, id);

            if (entry == null)
                Logger.WriteLog(LogType.Error, $"Character {id} does not exist; update skipped.");

            return entry;
        }

        public void UpdateCharacterAttributes(uint id, int spentBody, int spentMind, int spentSpirit)
        {
            var entry = GetWritable(id);

            if (entry == null)
                return;

            entry.Body = spentBody;
            entry.Mind = spentMind;
            entry.Spirit = spentSpirit;

            _charContext.SaveChanges();
        }

        public void UpdateCharacterClass(uint id, uint classId)
        {
            var entry = GetWritable(id);

            if (entry == null)
                return;

            entry.Class = classId;

            _charContext.SaveChanges();
        }

        public void UpdateCharacterCloneCredits(uint id, uint cloneCredits)
        {
            var entry = GetWritable(id);

            if (entry == null)
                return;

            entry.CloneCredits = cloneCredits;

            _charContext.SaveChanges();
        }

        public void UpdateCharacterCredits(uint id, int credits)
        {
            var entry = GetWritable(id);

            if (entry == null)
                return;

            entry.Credit = credits;

            _charContext.SaveChanges();
        }

        public void UpdateCharacterCurrencies(uint id, int credits, int prestige)
        {
            var entry = _charContext.GetWritableEnsuring(_charContext.CharacterEntries, id);
            entry.Credit = credits;
            entry.Prestige = prestige;
            _charContext.SaveChanges();
        }

        public void UpdateCharacterPrestige(uint id, int prestige)
        {
            var entry = GetWritable(id);

            if (entry == null)
                return;

            entry.Prestige = prestige;

            _charContext.SaveChanges();
        }

        public void UpdateCharacterExpirience(uint id, uint experience)
        {
            var entry = GetWritable(id);

            if (entry == null)
                return;

            entry.Experience = experience;

            _charContext.SaveChanges();
        }

        public void UpdateCharacterProgression(uint id, uint experience, byte level)
        {
            var entry = _charContext.GetWritableEnsuring(_charContext.CharacterEntries, id);
            entry.Experience = experience;
            entry.Level = level;
            _charContext.SaveChanges();
        }

        public void ReconcileBootcampCharacter(
            uint id,
            uint experience,
            byte level,
            uint classId,
            double x,
            double y,
            double z,
            double rotation,
            uint mapContextId)
        {
            var entry = _charContext.GetWritableEnsuring(_charContext.CharacterEntries, id);
            entry.Experience = experience;
            entry.Level = level;
            entry.Class = classId;
            entry.CoordX = x;
            entry.CoordY = y;
            entry.CoordZ = z;
            entry.Rotation = rotation;
            entry.MapContextId = mapContextId;
            _charContext.SaveChanges();
        }

        public void UpdateCharacterLevel(uint id, byte level)
        {
            var entry = GetWritable(id);

            if (entry == null)
                return;

            entry.Level = level;

            _charContext.SaveChanges();
        }

        public void UpdateCharacterLogin(uint id, uint totalTimePlayed, uint numLogins)
        {
            var entry = GetWritable(id);

            if (entry == null)
                return;

            entry.LastLogin = DateTime.UtcNow;
            entry.TotalTimePlayed = totalTimePlayed;
            entry.NumLogins = numLogins;

            _charContext.SaveChanges();
        }

        public void UpdateCharacterPosition(uint id, double x, double y, double z, double rotation, uint mapContextId)
        {
            var entry = GetWritable(id);

            if (entry == null)
                return;

            entry.CoordX = x;
            entry.CoordY = y;
            entry.CoordZ = z;
            entry.Rotation = rotation;
            entry.MapContextId = mapContextId;

            _charContext.SaveChanges();
        }

        public void UpdateCharacterActiveWeapon(uint id, byte activeWeapon)
        {
            var entry = GetWritable(id);

            if (entry == null)
                return;

            entry.ActiveWeapon = activeWeapon;

            _charContext.SaveChanges();
        }

        public void UpdateCharacterAbilitySlot(uint id, byte slot)
        {
            var entry = _charContext.GetWritableEnsuring(_charContext.CharacterEntries, id);
            entry.CurrentAbilitySlot = slot;
            _charContext.SaveChanges();
        }

        public void UpdateCharacterName(uint id, string name)
        {
            var entry = GetWritable(id);

            if (entry == null)
                return;

            entry.Name = name;

            _charContext.SaveChanges();
        }

        public bool IsCharacterNameTaken(string name, uint exceptCharacterId)
        {
            // ToLower on both sides: SQLite compares strings with BINARY collation, so = is
            // case-sensitive there while MySQL's default collation is not.
            var lowered = name.ToLower();

            return _charContext.CharacterEntries.Any(e => e.Id != exceptCharacterId && e.Name.ToLower() == lowered);
        }
    }
}