﻿using System;
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

        public bool CanChangeFamilyName(uint id, string newFamilyName)
        {
            var hasOtherAccountWithName = _charContext.GameAccountEntries.Any(e => e.Id != id && e.FamilyName == newFamilyName);
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
    }
}
