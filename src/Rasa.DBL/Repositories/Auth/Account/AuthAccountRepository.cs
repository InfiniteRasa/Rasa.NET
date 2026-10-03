using System;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;

using Microsoft.EntityFrameworkCore;

namespace Rasa.Repositories.Auth.Account
{
    using Context.Auth;
    using Services.Passwords;
    using Services.Random;
    using Structures.Auth;

    public class AuthAccountRepository : IAuthAccountRepository
    {
        private readonly AuthContext _dbContext;
        private readonly IRandomNumberService _randomNumberService;
        private readonly IPasswordHashSettings _passwordHashSettings;

        public AuthAccountRepository(AuthContext dbContext, IRandomNumberService randomNumberService, IPasswordHashSettings passwordHashSettings)
        {
            _dbContext = dbContext;
            _randomNumberService = randomNumberService;
            _passwordHashSettings = passwordHashSettings;
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
            var hashedPassword = PasswordHasher.Create(password ?? string.Empty, salt, _passwordHashSettings);

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

        public AuthAccountEntry FindByUserNameOrEmail(string userName, string email)
        {
            // lower() on both sides: Sqlite compares with BINARY collation, and "Bob" beside
            // "bob" is two accounts there and a broken unique index on MySql.
            var name = (userName ?? string.Empty).ToLower();
            var address = (email ?? string.Empty).ToLower();

            return _dbContext.AuthAccountEntries.AsNoTracking().FirstOrDefault(e => e.Username.ToLower() == name)
                   ?? _dbContext.AuthAccountEntries.AsNoTracking().FirstOrDefault(e => e.Email.ToLower() == address);
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
                PasswordHasher.Burn(password, _passwordHashSettings);

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

            // The password is known to be right, which is the only time an account's hash can
            // be brought up to the current settings: the old SHA-256 format, an older iteration
            // count, or a pepper configured since.
            if (PasswordHasher.NeedsRehash(entry.Password, _passwordHashSettings))
            {
                try
                {
                    entry.Password = Rehash(entry.Id, password, entry.Salt);
                }
                catch (DbUpdateException e)
                {
                    // The old hash still verifies; the login goes ahead and the next one tries again.
                    Logger.WriteLog(LogType.Error, $"Could not rehash the password of account {entry.Id}: {e.Message}");
                }
            }

            return entry;
        }

        private string Rehash(uint id, string password, string salt)
        {
            var hashed = PasswordHasher.Create(password ?? string.Empty, salt, _passwordHashSettings);
            var writable = _dbContext.GetWritableEnsuring(_dbContext.AuthAccountEntries, id);

            writable.Password = hashed;
            _dbContext.SaveChanges();

            return hashed;
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

        public bool CheckPassword(AuthAccountEntry entry, string password)
        {
            return PasswordHasher.Verify(password, entry.Salt, entry.Password, _passwordHashSettings);
        }

        /// <summary>The hash format before PBKDF2 (<see cref="PasswordHasher.LegacyHash"/>); verified, never written.</summary>
        public static string Hash(string password, string salt) => PasswordHasher.LegacyHash(password, salt);
    }
}