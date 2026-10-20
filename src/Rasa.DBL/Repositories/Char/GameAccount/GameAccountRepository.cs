using System;
using System.Linq;
using System.Net;

using Microsoft.EntityFrameworkCore;

namespace Rasa.Repositories.Char.GameAccount
{
    using Context.Char;
    using Structures.Char;

    public class GameAccountRepository : IGameAccountRepository
    {
        private readonly CharContext _charContext;

        public GameAccountRepository(CharContext charContext)
        {
            _charContext = charContext;
        }

        public void CreateOrUpdate(uint id, string name, string email)
        {
            var existingGameAccount = _charContext.GetWritable(_charContext.GameAccountEntries, id);
            if (existingGameAccount != null)
            {
                existingGameAccount.Name = name;
                existingGameAccount.Email = email;
            }
            else
            {
                var newEntry = new GameAccountEntry
                {
                    Id = id,
                    Name = name,
                    Email = email
                };
                _charContext.GameAccountEntries.Add(newEntry);
                _charContext.SaveChanges();
            }
        }

        public GameAccountEntry Get(uint id)
        {
            var query =_charContext.CreateNoTrackingQuery(_charContext.GameAccountEntries);
            query = query
                .Include(e => e.Characters);
            return _charContext.FindEnsuring(query, id);
        }

        public GameAccountEntry Get(string name)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.GameAccountEntries);
            var entry = query.Where(e => e.FamilyName == name).FirstOrDefault();

            return entry;
        }

        public GameAccountEntry Find(uint id)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.GameAccountEntries);
            query = query
                .Include(e => e.Characters);
            return _charContext.Find(query, id);
        }

        public GameAccountEntry FindByFamilyName(string familyName)
        {
            if (string.IsNullOrEmpty(familyName))
                return null;

            // Sqlite compares with BINARY collation, so a plain == is case-sensitive there
            // while MySQL's default collation is not. Lowering both sides behaves the same on
            // both providers for A to Z, all a name may have now (CharacterManager.IsValidName);
            // the exact pass first finds an older name with other letters, which SQLite's lower()
            // leaves as they are. Lowered here without the server's culture, which in Turkish
            // would make "I" a dotless "ı" that no stored name has.
            var exact = Get(familyName);
            if (exact != null)
                return exact;

            var lowered = familyName.ToLowerInvariant();
            var query = _charContext.CreateNoTrackingQuery(_charContext.GameAccountEntries);
            return query.Where(e => e.FamilyName.ToLower() == lowered).FirstOrDefault();
        }

        /// <summary>The unique index on the family name, case-blind (Family_name_unique).</summary>
        public const string FamilyNameIndex = "account_index_family_name";

        /// <summary>
        /// Whether a save was refused because another account has the family name: by
        /// <see cref="FamilyNameIndex"/>, which MySQL names in its error and SQLite reports by its
        /// column, account.family_name, as it does any index on a plain column.
        /// </summary>
        public static bool RefusedFamilyName(Exception error)
        {
            var message = error?.GetBaseException().Message ?? string.Empty;

            return message.Contains(FamilyNameIndex, StringComparison.Ordinal)
                || message.Contains("UNIQUE constraint failed: account.family_name", StringComparison.Ordinal);
        }

        public bool CanChangeFamilyName(uint id, string newFamilyName)
        {
            if (string.IsNullOrEmpty(newFamilyName))
                return false;

            // Compared without case, like FindByFamilyName above and like every lookup of a player
            // by the name someone typed. With Sqlite's case-sensitive ==, "Smith", "smith" and
            // "SMITH" could all be taken by different accounts, and a whisper or invite for one
            // went to whichever of them was found first. The unique index (Family_name_unique)
            // holds the same rule in the database, for a name taken between this and the write.
            var lowered = newFamilyName.ToLowerInvariant();
            var hasOtherAccountWithName = _charContext.GameAccountEntries.Any(e => e.Id != id && e.FamilyName.ToLower() == lowered);
            return !hasOtherAccountWithName;
        }

        public void UpdateFamilyName(uint id, string newFamilyName)
        {
            var entry = _charContext.GetWritableEnsuring(_charContext.GameAccountEntries, id);
            entry.FamilyName = newFamilyName;
        }

        public void UpdateLoginData(uint id, IPAddress remoteAddress)
        {
            var entry = _charContext.GetWritableEnsuring(_charContext.GameAccountEntries, id);
            entry.LastIp = remoteAddress.ToString();
            entry.LastLogin = DateTime.UtcNow;
        }

        public void UpdateSelectedSlot(uint id, byte selectedSlot)
        {
            var entry = _charContext.GetWritableEnsuring(_charContext.GameAccountEntries, id);
            entry.SelectedSlot = selectedSlot;
        }

        public void UpdateAccountLevel(uint id, byte level)
        {
            var entry = _charContext.GetWritableEnsuring(_charContext.GameAccountEntries, id);
            entry.Level = level;

            _charContext.SaveChanges();
        }

        public void UpdateMutedUntil(uint id, long mutedUntil)
        {
            var entry = _charContext.GetWritableEnsuring(_charContext.GameAccountEntries, id);
            entry.MutedUntil = mutedUntil;

            _charContext.SaveChanges();
        }

        public byte AddHybridUnlocks(uint id, byte unlocks)
        {
            var entry = _charContext.GetWritableEnsuring(_charContext.GameAccountEntries, id);
            var added = (byte)(unlocks & ~entry.HybridUnlocks);

            if (added == 0)
                return 0;

            entry.HybridUnlocks |= added;
            _charContext.SaveChanges();
            return added;
        }

        public void UpdateCanSkipBootcamp(uint id, bool canSkipBootcamp)
        {
            var entry = _charContext.GetWritableEnsuring(_charContext.GameAccountEntries, id);
            entry.CanSkipBootcamp = canSkipBootcamp;
            _charContext.SaveChanges();
        }

        public bool TryUpdateCanSkipBootcamp(uint id, bool expectedValue, bool canSkipBootcamp)
        {
            var entry = _charContext.GetWritableEnsuring(_charContext.GameAccountEntries, id);
            if (entry.CanSkipBootcamp != expectedValue)
                return false;

            entry.CanSkipBootcamp = canSkipBootcamp;
            _charContext.SaveChanges();
            return true;
        }
    }
}
