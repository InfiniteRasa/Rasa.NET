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

        /// <summary>
        /// Writes the account's new hash, and makes sure it is the one stored. Throws a
        /// DbUpdateException, with the old hash left in place, when it could not be.
        ///
        /// A MySql account.password still varchar(64) (from before 20261001000000_Widen_password_hash)
        /// cannot hold a hash in the current format (87 characters). In strict mode the UPDATE
        /// fails, and the entry used to stay Modified on this context: the next SaveChanges,
        /// UpdateLoginData's on the same login, sent it again and failed with it. Out of strict
        /// mode the server cut the hash to 64 characters without an error, and from then on no
        /// password matched it: the account could log in once more and never again.
        /// </summary>
        private string Rehash(uint id, string password, string salt)
        {
            var hashed = PasswordHasher.Create(password ?? string.Empty, salt, _passwordHashSettings);
            var writable = _dbContext.GetWritableEnsuring(_dbContext.AuthAccountEntries, id);
            var tracked = _dbContext.Entry(writable);
            var previous = writable.Password;

            writable.Password = hashed;

            try
            {
                _dbContext.SaveChanges();
            }
            catch (DbUpdateException)
            {
                // Back as it was read, so the next SaveChanges does not send it again.
                writable.Password = previous;
                tracked.State = EntityState.Unchanged;
                throw;
            }

            var stored = _dbContext.AuthAccountEntries.AsNoTracking().Where(e => e.Id == id).Select(e => e.Password).FirstOrDefault();

            if (stored == hashed)
                return hashed;

            // Not what was written: cut down by a column too narrow for it. The old hash fits
            // where it came from, and still verifies.
            writable.Password = previous;
            _dbContext.SaveChanges();

            throw new DbUpdateException($"account.password kept {stored?.Length ?? 0} of the new hash's {hashed.Length} characters; " +
                                        "the column is narrower than varchar(255) (migration 20261001000000_Widen_password_hash), so the old hash stays");
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