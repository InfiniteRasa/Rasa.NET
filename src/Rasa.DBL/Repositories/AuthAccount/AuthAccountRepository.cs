using System.Linq;
using System.Net;
using Microsoft.EntityFrameworkCore;

namespace Rasa.Repositories.AuthAccount
{
    using System;
    using Context;
    using Services;
    using Structures;

    public class AuthAccountRepository : IAuthAccountRepository
    {
        private readonly AuthContext _dbContext;
        private readonly IRandomNumberService _randomNumberService;

        public AuthAccountRepository(AuthContext dbContext, IRandomNumberService randomNumberService)
        {
            _dbContext = dbContext;
            _randomNumberService = randomNumberService;
        }

        public void Create(string email, string userName, string password)
        {
            var entry = new AuthAccountEntry
            {
                Email = email,
                Username = userName,
                Password = password,
                Salt = CreateSalt()
            };

            _dbContext.AuthAccountEntries.Add(entry);
            _dbContext.SaveChanges();
        }

        public AuthAccountEntry GetByUserName(string name)
        {
            return _dbContext.AuthAccountEntries.AsNoTracking()
                .FirstOrDefault(e => e.Username == name);
        }

        public void UpdateLoginData(uint id, IPAddress remoteAddress)
        {
            var entry = GetWritable(id);
            entry.LastIp = remoteAddress.ToString();
            _dbContext.SaveChanges();
        }

        public void UpdateLastServer(uint id, byte lastServerId)
        {
            var entry = GetWritable(id);
            entry.LastServerId = lastServerId;
            _dbContext.SaveChanges();
        }

        private string CreateSalt()
        {
            var salt = _randomNumberService.CreateRandomBytes(20);
            return BitConverter.ToString(salt)
                .Replace("-", "")
                .ToLower();
        }

        private AuthAccountEntry GetWritable(uint id)
        {
            var entry = _dbContext.AuthAccountEntries
                .AsTracking()
                .FirstOrDefault(e => e.Id == id);

            if (entry == null)
            {
                throw new EntityNotFoundException(nameof(AuthAccountEntry), id);
            }
            return entry;
        }
    }
}