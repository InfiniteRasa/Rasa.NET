using System;
using System.IO;
using System.Linq;
using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Auth
{
    using Rasa.Context;
    using Rasa.Context.Auth;
    using Rasa.Repositories.Auth.Account;
    using Rasa.Services.Passwords;
    using Rasa.Services.Random;
    using Rasa.Structures.Auth;
    using Rasa.Test.Database;

    /// <summary>
    /// A legacy password is rehashed on its first right login (AuthAccountRepository.Rehash), to a
    /// hash of 87 characters. On a MySql account.password still varchar(64) that write fails in
    /// strict mode, and is cut to 64 characters without an error out of it (BR-155). Sqlite does
    /// not hold a varchar to its length, so triggers stand in for the two MySql behaviours.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class AccountRehashTests
    {
        private const string Name = "legacy";
        private const string Password = "password123";
        private const string Salt = "fixture-salt";

        private sealed class Settings : IPasswordHashSettings
        {
            public string Pepper { get; set; } = string.Empty;
            public int Iterations { get; set; } = PasswordHasher.MinimumIterations;
        }

        [TestMethod]
        public void AWideColumnTakesTheNewHash()
        {
            WithLegacyAccount(null, (context, accounts, id) =>
            {
                accounts.GetByUserName(Name, Password);

                Assert.IsFalse(PasswordHasher.NeedsRehash(StoredPassword(context, id), new Settings()), "rehashed");
                accounts.GetByUserName(Name, Password);
            });
        }

        [TestMethod]
        public void ARefusedRehashLeavesTheOldHashAndTheLoginGoesOn()
        {
            // MySql in strict mode: "Data too long for column 'password'".
            const string Strict = "CREATE TRIGGER narrow BEFORE UPDATE OF password ON account WHEN length(NEW.password) > 64 " +
                                  "BEGIN SELECT RAISE(ABORT, 'Data too long for column password'); END;";

            WithLegacyAccount(Strict, (context, accounts, id) =>
            {
                accounts.GetByUserName(Name, Password);

                // The same login's next write: it used to send the refused hash again with it, and fail.
                accounts.UpdateLoginData(id, IPAddress.Loopback);

                Assert.AreEqual(PasswordHasher.LegacyHash(Password, Salt), StoredPassword(context, id));
                Assert.AreEqual("127.0.0.1", context.AuthAccountEntries.AsNoTracking().Single(e => e.Id == id).LastIp);

                // And the next login, as the first.
                accounts.GetByUserName(Name, Password);
            });
        }

        [TestMethod]
        public void ARehashCutDownByTheColumnIsUndoneAndTheAccountStillLogsIn()
        {
            // MySql out of strict mode: the value is cut to the column's length, with a warning only.
            const string Lenient = "CREATE TRIGGER narrow AFTER UPDATE OF password ON account WHEN length(NEW.password) > 64 " +
                                   "BEGIN UPDATE account SET password = substr(NEW.password, 1, 64) WHERE id = NEW.id; END;";

            WithLegacyAccount(Lenient, (context, accounts, id) =>
            {
                accounts.GetByUserName(Name, Password);
                accounts.UpdateLoginData(id, IPAddress.Loopback);

                Assert.AreEqual(PasswordHasher.LegacyHash(Password, Salt), StoredPassword(context, id), "the old hash, whole");

                // It used to be the first 64 characters of the new one, which no password matches.
                accounts.GetByUserName(Name, Password);
            });
        }

        [TestMethod]
        public void TheStartupCheckNamesTheMigrationsAMySqlDatabaseHasNotHad()
        {
            var one = RasaDbContextBase.PendingMigrationsReport("MySqlAuthContext", new[] { "20261001000000_Widen_password_hash" });

            StringAssert.Contains(one, "has not had 1 migration: 20261001000000_Widen_password_hash.");

            var many = Enumerable.Range(1, 12).Select(i => $"2026{i:D2}01000000_Step{i}").ToList();
            var report = RasaDbContextBase.PendingMigrationsReport("MySqlCharContext", many);

            StringAssert.Contains(report, "has not had 12 migrations: 20260101000000_Step1,");
            StringAssert.Contains(report, "and 2 more, up to 20261201000000_Step12.");
            Assert.IsFalse(report.Contains("Step11,"), "ten named, then the count");
        }

        private static void WithLegacyAccount(string trigger, Action<AuthContext, AuthAccountRepository, uint> test)
        {
            var path = Path.Combine(AppContext.BaseDirectory, "TestDatabases", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);

            try
            {
                using var context = (AuthContext)PersistenceIntegrationTests.CreateContext(typeof(SqliteAuthContext), Path.Combine(path, "auth"));
                using var random = new RandomNumberService();

                context.Database.Migrate();

                var account = new AuthAccountEntry
                {
                    Email = "legacy@example.invalid",
                    Username = Name,
                    Password = PasswordHasher.LegacyHash(Password, Salt),
                    Salt = Salt
                };

                context.AuthAccountEntries.Add(account);
                context.SaveChanges();
                context.ChangeTracker.Clear();

                Assert.AreEqual(64, account.Password.Length, "a legacy hash fits varchar(64)");

                if (trigger != null)
                    context.Database.ExecuteSqlRaw(trigger);

                test(context, new AuthAccountRepository(context, random, new Settings()), account.Id);
            }
            finally
            {
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                Directory.Delete(path, true);
            }
        }

        private static string StoredPassword(AuthContext context, uint id) =>
            context.AuthAccountEntries.AsNoTracking().Where(e => e.Id == id).Select(e => e.Password).Single();
    }
}
