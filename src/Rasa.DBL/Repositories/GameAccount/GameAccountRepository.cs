using System;
using System.Linq;
using System.Net;

namespace Rasa.Repositories.GameAccount
{
    using Context.Char;
    using Structures;

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
                    Name = name,
                    Email = email
                };
                _charContext.GameAccountEntries.Add(newEntry);
            }

            _charContext.SaveChanges();
        }

        public GameAccountEntry Get(uint id)
        {
            return _charContext.GetReadableEnsuring(_charContext.GameAccountEntries, id);
        }

        public bool CanChangeFamilyName(uint id, string newFamilyName)
        {
            var hasOtherAccountWithName = _charContext.GameAccountEntries.Any(e => e.Id != id && e.FamilyName == newFamilyName);
            return !hasOtherAccountWithName;
        }

        public void UpdateFamilyName(uint id, string newFamilyName)
        {
            var entry = _charContext.GetWritableEnsuring(_charContext.GameAccountEntries, id);
            entry.FamilyName = newFamilyName;
            _charContext.SaveChanges();
        }

        public void UpdateLoginData(uint id, IPAddress remoteAddress)
        {
            var entry = _charContext.GetWritableEnsuring(_charContext.GameAccountEntries, id);
            entry.LastIp = remoteAddress.ToString();
            entry.LastLogin = DateTime.UtcNow;
            _charContext.SaveChanges();
        }

        public void IncrementCharacterCount(uint id)
        {
            var entry = _charContext.GetWritableEnsuring(_charContext.GameAccountEntries, id);
            entry.CharacterCount++;
            _charContext.SaveChanges();
        }

        public void DecrementCharacterCount(uint id)
        {
            var entry = _charContext.GetWritableEnsuring(_charContext.GameAccountEntries, id);
            if (entry.CharacterCount <= 0)
            {
                return;
            }
            entry.CharacterCount--;
            _charContext.SaveChanges();
        }
    }
}