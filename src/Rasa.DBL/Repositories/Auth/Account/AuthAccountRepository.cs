using System;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;

using Microsoft.EntityFrameworkCore;

namespace Rasa.Repositories.Auth.Account
{
    using Context.Auth;
    using Services.Random;
    using Structures.Auth;

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
            var entry = _dbContext
                .AuthAccountEntries
                .AsNoTracking()
                .FirstOrDefault(e => e.Username == name);
            if (entry == null)
            {
                // The same work as a wrong password, so an unknown name does not answer faster than
                // a known one: that difference told anyone timing the reply which names existed.
                Hash(password ?? string.Empty, UnknownAccountSalt);

                throw new EntityNotFoundException(AuthAccountEntry.TableName, nameof(AuthAccountEntry.Username), name);
            }

            if (!CheckPassword(entry, password))
            {
                throw new PasswordCheckFailedException(entry);
            }

            if (entry.Locked)
            {
                throw new AccountLockedException(entry);
            }

            return entry;
        }

        public void UpdateLoginData(uint id, IPAddress remoteAddress)
        {
            var entry = _dbContext.GetWritableEnsuring(_dbContext.AuthAccountEntries, id);
            entry.LastIp = remoteAddress.ToString();
            entry.LastLogin = DateTime.UtcNow;
            _dbContext.SaveChanges();
        }

        public void UpdateLastServer(uint id, byte lastServerId)
        {
            var entry = _dbContext.GetWritableEnsuring(_dbContext.AuthAccountEntries, id);
            entry.LastServerId = lastServerId;
            _dbContext.SaveChanges();
        }

        public AuthAccountEntry SetLocked(string userName, bool locked)
        {
            if (string.IsNullOrEmpty(userName))
                return null;

            // Tracked query: the entry is modified and saved below. Sqlite compares with BINARY
            // collation, so a lower()-to-lower() pass catches a differently-cased username.
            var lowered = userName.ToLower();
            var entry = _dbContext.AuthAccountEntries.FirstOrDefault(e => e.Username == userName)
                        ?? _dbContext.AuthAccountEntries.FirstOrDefault(e => e.Username.ToLower() == lowered);

            if (entry == null)
                return null;

            entry.Locked = locked;
            _dbContext.SaveChanges();

            return entry;
        }

        private string CreateSalt()
        {
            var salt = _randomNumberService.CreateRandomBytes(20);
            return BitConverter.ToString(salt)
                .Replace("-", "")
                .ToLower();
        }

        /// <summary>A salt of the usual shape, hashed against when the name is unknown.</summary>
        private const string UnknownAccountSalt = "0000000000000000000000000000000000000000";

        public bool CheckPassword(AuthAccountEntry entry, string password)
        {
            var expectedPasswordHash = Hash(password ?? string.Empty, entry.Salt);

            // In time that does not depend on how much of the hash matched.
            return entry.Password != null && CryptographicOperations.FixedTimeEquals(
                Encoding.ASCII.GetBytes(expectedPasswordHash), Encoding.ASCII.GetBytes(entry.Password));
        }

        public static string Hash(string password, string salt)
        {
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes($"{salt}:{password}"))).Replace("-", "").ToLower();
        }
    }
}