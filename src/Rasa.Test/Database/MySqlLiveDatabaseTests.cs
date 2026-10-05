using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Database
{
    using Rasa.Context;
    using Rasa.Context.Auth;
    using Rasa.Context.Char;
    using Rasa.Context.World;
    using Rasa.Repositories.Auth.Account;
    using Rasa.Repositories.Char.Character;
    using Rasa.Repositories.Char.CharacterMission;
    using Rasa.Repositories.Char.GameAccount;
    using Rasa.Services.Passwords;
    using Rasa.Services.Random;
    using Rasa.Structures.Char;

    /// <summary>
    /// The MySQL contexts against a real server with every migration applied by
    /// `dotnet ef database update`, the way a MySQL deployment gets its schema. The other database
    /// tests build the MySQL model offline against a faked server version; these are the ones that
    /// find a migration MySQL rejects, seed data that differs from Sqlite's, or a constraint only
    /// one provider enforces. See <see cref="LiveMySql"/> for how they find the server.
    /// </summary>
    [TestClass]
    [TestCategory(LiveMySql.Category)]
    [DoNotParallelize]
    public class MySqlLiveDatabaseTests
    {
        [TestMethod]
        [DataRow(typeof(MySqlAuthContext))]
        [DataRow(typeof(MySqlCharContext))]
        [DataRow(typeof(MySqlWorldContext))]
        public void EveryMigrationIsAppliedAndNoneIsPending(Type contextType)
        {
            using var context = LiveMySql.CreateContext(contextType);

            CollectionAssert.AreEqual(
                context.Database.GetMigrations().ToArray(),
                context.Database.GetAppliedMigrations().ToArray());
            Assert.IsFalse(context.Database.GetPendingMigrations().Any());
        }

        /// <summary>
        /// Every table holds as many rows after MySQL's migrations as after Sqlite's: the seed data
        /// and the data migrations are written per provider, and this is what notices when one of
        /// them drifts. The tests here that write rows remove them again.
        /// </summary>
        [TestMethod]
        [DataRow(typeof(MySqlAuthContext), typeof(SqliteAuthContext))]
        [DataRow(typeof(MySqlCharContext), typeof(SqliteCharContext))]
        [DataRow(typeof(MySqlWorldContext), typeof(SqliteWorldContext))]
        public void MigratedTablesHoldTheSameRowsAsSqlite(Type mySqlType, Type sqliteType)
        {
            using var mySql = LiveMySql.CreateContext(mySqlType);
            var path = Path.Combine(AppContext.BaseDirectory, "TestDatabases", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            try
            {
                using var sqlite = PersistenceIntegrationTests.CreateContext(sqliteType, Path.Combine(path, "database"));
                sqlite.Database.Migrate();

                var tables = mySql.Model.GetEntityTypes()
                    .Select(entity => entity.GetTableName())
                    .Where(table => table != null)
                    .Distinct()
                    .OrderBy(table => table, StringComparer.Ordinal)
                    .ToArray();
                Assert.IsTrue(tables.Length > 0);

                var differences = new List<string>();
                foreach (var table in tables)
                {
                    var expected = CountRows(sqlite, table);
                    var actual = CountRows(mySql, table);
                    if (expected != actual)
                        differences.Add($"{table}: Sqlite {expected}, MySQL {actual}");
                }

                Assert.AreEqual(0, differences.Count, string.Join(Environment.NewLine, differences));
            }
            finally
            {
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                Directory.Delete(path, true);
            }
        }

        [TestMethod]
        public void AuthAccountsRoundTripAndUsernamesAreUniqueWithoutCase()
        {
            var name = "Live" + Guid.NewGuid().ToString("N")[..12];
            using var context = (AuthContext)LiveMySql.CreateContext(typeof(MySqlAuthContext));
            using var random = new RandomNumberService();
            var accounts = new AuthAccountRepository(context, random, new HashSettings());
            try
            {
                accounts.Create(name + "@example.invalid", name, "password123");

                using var reopened = (AuthContext)LiveMySql.CreateContext(typeof(MySqlAuthContext));
                var found = new AuthAccountRepository(reopened, random, new HashSettings())
                    .FindByUserNameOrEmail(name.ToUpperInvariant(), null);
                Assert.IsNotNull(found);
                Assert.AreEqual(name, found.Username);
                Assert.AreEqual("0.0.0.0", found.LastIp);
                Assert.IsFalse(found.Locked);
                Assert.IsTrue(accounts.CheckPassword(found, "password123"));
                Assert.IsFalse(accounts.CheckPassword(found, "password124"));

                // MySQL's default collation compares without case, so the unique index already
                // refuses "Bob" beside "bob"; Sqlite's BINARY collation would store both.
                using var duplicate = (AuthContext)LiveMySql.CreateContext(typeof(MySqlAuthContext));
                Assert.ThrowsExactly<DbUpdateException>(() =>
                    new AuthAccountRepository(duplicate, random, new HashSettings())
                        .Create("other-" + name + "@example.invalid", name.ToUpperInvariant(), "password123"));
            }
            finally
            {
                using var cleanup = LiveMySql.CreateContext(typeof(MySqlAuthContext));
                cleanup.Database.ExecuteSql($"DELETE FROM `account` WHERE username = {name}");
            }
        }

        [TestMethod]
        public void CharactersAndMissionsRoundTripAndForeignKeysHold()
        {
            var accountId = 4_000_000_000u + (uint)Random.Shared.Next(1, 100_000_000);
            var name = "Live" + accountId;
            try
            {
                uint characterId;
                using (var context = (CharContext)LiveMySql.CreateContext(typeof(MySqlCharContext)))
                {
                    var accounts = new GameAccountRepository(context);
                    accounts.CreateOrUpdate(accountId, name, name + "@example.invalid");
                    var character = new CharacterRepository(context)
                        .Create(accounts.Get(accountId), 1, name, 1, 1.0, 0);
                    Assert.IsNotNull(character, "The character was not created; the error is in the log above.");
                    characterId = character.Id;
                    new CharacterMissionRepository(context).Add(new CharacterMissionEntry(characterId, 429, 2));
                }

                using (var reopened = (CharContext)LiveMySql.CreateContext(typeof(MySqlCharContext)))
                {
                    var character = new CharacterRepository(reopened).GetByAccountId(accountId, 1);
                    Assert.IsNotNull(character);
                    Assert.AreEqual(characterId, character.Id);
                    Assert.AreEqual(name, character.GameAccount.Name);
                    Assert.AreEqual(string.Empty, character.GameAccount.FamilyName);
                    var mission = new CharacterMissionRepository(reopened).GetByCharacterAndMission(characterId, 429);
                    Assert.IsNotNull(mission);
                    Assert.AreEqual(2u, mission.MissionState);
                    Assert.AreEqual(32, mission.AssignmentId.Length);

                    // InnoDB enforces both keys: an account can't go while it has a character
                    // (Restrict), and a character takes its missions with it (Cascade).
                    reopened.Remove(reopened.GameAccountEntries.Single(e => e.Id == accountId));
                    Assert.ThrowsExactly<DbUpdateException>(() => reopened.SaveChanges());
                }

                using (var context = (CharContext)LiveMySql.CreateContext(typeof(MySqlCharContext)))
                {
                    context.Database.ExecuteSql($"DELETE FROM `character` WHERE id = {characterId}");
                    Assert.AreEqual(0, context.CharacterMissionEntries.Count(e => e.CharacterId == characterId));
                }
            }
            finally
            {
                using var cleanup = LiveMySql.CreateContext(typeof(MySqlCharContext));
                cleanup.Database.ExecuteSql($"DELETE FROM `character` WHERE account_id = {accountId}");
                cleanup.Database.ExecuteSql($"DELETE FROM `account` WHERE id = {accountId}");
            }
        }

        // Backticks quote an identifier in both MySQL and Sqlite. An identifier can't be a
        // parameter; the table names come from the model.
        private static int CountRows(RasaDbContextBase context, string table) =>
            context.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM `" + table + "`").Single();

        private sealed class HashSettings : IPasswordHashSettings
        {
            public string Pepper => string.Empty;
            public int Iterations => PasswordHasher.MinimumIterations;
        }
    }
}
