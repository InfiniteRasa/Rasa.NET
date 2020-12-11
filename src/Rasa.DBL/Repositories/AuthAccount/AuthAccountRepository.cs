using System;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;

using Microsoft.EntityFrameworkCore;

namespace Rasa.Repositories.AuthAccount
{
    using Context;
    using Context.Auth;
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
            if (string.IsNullOrEmpty(email))
            {
                throw new ArgumentNullException(nameof(email));
            }
            if (string.IsNullOrEmpty(userName))
            {
                throw new ArgumentNullException(nameof(userName));
            }

            var salt = CreateSalt();
            var hashedPassword = Hash(password ?? string.Empty, salt);

            var entry = new AuthAccountEntry
            {
                Email = email,
                Username = userName,
                Password = hashedPassword,
                Salt = salt
            };

            _dbContext.AuthAccountEntries.Add(entry);
            _dbContext.SaveChanges();
        }

        public AuthAccountEntry GetByUserName(string name, string password)
        {
            var entry = _dbContext.AuthAccountEntries.AsNoTracking()
                .FirstOrDefault(e => e.Username == name);
            if (entry == null)
            {
                throw new EntityNotFoundException(AuthAccountEntry.TableName, nameof(AuthAccountEntry.Username), name);
            }

            if (CheckPassword(entry, password))
            {
                throw new PasswordCheckFailedException(name);
            }

            if (entry.Locked)
            {
                throw new AccountLockedException(name);
            }

            return entry;
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
                throw new EntityNotFoundException(AuthAccountEntry.TableName, nameof(AuthAccountEntry.Id), id);
            }
            return entry;
        }

        public bool CheckPassword(AuthAccountEntry entry, string password)
        {
            var expectedPasswordHash = Hash(password, entry.Salt);
            return expectedPasswordHash == entry.Password;
        }

        public static string Hash(string password, string salt)
        {
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes($"{salt}:{password}"))).Replace("-", "").ToLower();
        }
    }
}