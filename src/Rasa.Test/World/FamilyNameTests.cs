using System;
using System.IO;
using System.Linq;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Context.Char;
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Packets.Communicator.Server;
    using Rasa.Repositories.Char.GameAccount;
    using Rasa.Structures.Char;
    using Rasa.Test.Database;
    using Rasa.Test.Missions;

    /// <summary>
    /// A family name is one account's whatever its case, and the database holds it so (the
    /// account_index_family_name unique index): before it a name was checked and then written,
    /// with nothing to stop another account taking it in between. Names are a capital and then
    /// letters A to Z: any script's letters were taken, so a name could be spelt with Cyrillic or
    /// Greek letters that look like someone else's, and SQLite, which folds only A to Z, took
    /// "Élan" and "élan" for two names where MySQL took them for one.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class FamilyNameTests
    {
        private const string Before = "20261214000000_Clan_name_unique";
        private const string Migration = "20261215000000_Family_name_unique";

        [TestMethod]
        [DataRow("Smith", true)]
        [DataRow("Mcgee", true)]
        [DataRow("smith", false)]
        [DataRow("Élan", false)]                  // É
        [DataRow("Müller", false)]                // ü
        [DataRow("Аlice", false)]                 // Cyrillic А
        [DataRow("Smіth", false)]                 // Cyrillic і
        [DataRow("Αthena", false)]                // Greek Α
        [DataRow("O\"Brien", false)]
        [DataRow("Smith2", false)]
        public void ANameIsACapitalAndThenLettersAToZ(string name, bool valid)
        {
            Assert.AreEqual(valid, CharacterManager.IsValidName(name, out _));
        }

        [TestMethod]
        public void AFamilyNameIsTakenInAnyCaseAndTheDatabaseRefusesASecond()
        {
            using var context = MissionTestContext.WithCompletableMission(429);

            Account(context, 50, "Smith");
            Account(context, 51, "");
            Account(context, 52, "");

            using (var unit = context.CreateChar())
            {
                Assert.AreEqual(50u, unit.GameAccounts.FindByFamilyName("sMITH")?.Id);
                Assert.IsFalse(unit.GameAccounts.CanChangeFamilyName(52, "SMITH"));
                Assert.IsTrue(unit.GameAccounts.CanChangeFamilyName(50, "SMITH"), "its own name in other capitals");
            }

            var refused = Assert.ThrowsExactly<DbUpdateException>(() => Account(context, 53, "SMITH"));
            Assert.IsTrue(GameAccountRepository.RefusedFamilyName(refused), refused.GetBaseException().Message);

            Account(context, 54, "");
            Account(context, 55, "Jones");

            using var verify = context.Open();
            Assert.AreEqual(3, verify.GameAccountEntries.Count(account => account.FamilyName == ""), "an account with no family name yet is not held to it");
        }

        [TestMethod]
        public void ARenameIntoANameTakenSinceTheCheckIsRefusedAsTaken()
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            var client = context.Client;
            Account(context, 60, "");

            string before;
            using (var read = context.Open())
                before = read.GameAccountEntries.AsNoTracking().Single(account => account.Id == client.AccountEntry.Id).FamilyName;

            client.Player.FamilyName = before;
            context.Drain();

            var taken = false;
            context.BeforeSave = characters =>
            {
                if (taken)
                    return;

                taken = true;
                characters.Database.ExecuteSqlRaw("UPDATE account SET family_name = 'SMITH' WHERE id = 60");
            };

            try
            {
                Assert.IsFalse(new CharacterManager(context).Rename(client, client, "Smith", true));
            }
            finally
            {
                context.BeforeSave = null;
            }

            Assert.IsTrue(taken);
            Assert.AreEqual(PlayerMessage.PmFamilyNameReserved, context.Drain().OfType<DisplayClientMessagePacket>().Single().MsgId);
            Assert.AreEqual(before, client.Player.FamilyName, "not changed on the player either");

            using var verify = context.Open();
            Assert.AreEqual(before, verify.GameAccountEntries.AsNoTracking().Single(account => account.Id == client.AccountEntry.Id).FamilyName);
        }

        [TestMethod]
        public void TheMigrationRenamesTheLaterOfTwoFamilyNamesThatDifferOnlyInCase()
        {
            WithDatabase(characters =>
            {
                characters.GetService<IMigrator>().Migrate(Before);

                Add(characters, 1, "Smith");
                Add(characters, 2, "SMITH");
                Add(characters, 3, "");
                Add(characters, 4, "");
                Add(characters, 5, "Jones");
                Add(characters, 6, "smith");
                characters.ChangeTracker.Clear();

                characters.Database.Migrate();

                var names = characters.GameAccountEntries.AsNoTracking().ToDictionary(account => account.Id, account => account.FamilyName);
                Assert.AreEqual("Smith", names[1], "the oldest keeps it");
                Assert.AreEqual("SMITH2", names[2]);
                Assert.AreEqual("", names[3], "no family name yet is not a duplicate");
                Assert.AreEqual("", names[4]);
                Assert.AreEqual("Jones", names[5]);
                Assert.AreEqual("smith6", names[6]);

                Assert.ThrowsExactly<DbUpdateException>(() => Add(characters, 7, "JONES"), "the index is there");
                characters.ChangeTracker.Clear();
                Add(characters, 8, "");
                characters.ChangeTracker.Clear();

                // Back: the index goes, the names stay.
                characters.GetService<IMigrator>().Migrate(Before);
                Add(characters, 9, "JONES");
                Assert.AreEqual("SMITH2", characters.GameAccountEntries.AsNoTracking().Single(account => account.Id == 2).FamilyName);
            });
        }

        [TestMethod]
        public void OnMySqlTheIndexLeavesOutTheEmptyNameAndIsTheColumnsOwnCollation()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(MySqlCharContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(Before, Migration);
            var down = migrator.GenerateScript(Migration, Before);

            StringAssert.Contains(up, "SET `account`.family_name = CONCAT(`account`.family_name, `account`.id);");
            StringAssert.Contains(up, "WHERE later.family_name <> ''");
            StringAssert.Contains(up, "CREATE UNIQUE INDEX account_index_family_name ON `account` ((NULLIF(family_name, '')));");
            StringAssert.Contains(down, "DROP INDEX account_index_family_name ON `account`;");
        }

        #region Fixture

        private static void Account(MissionTestContext context, uint id, string familyName)
        {
            using var write = context.Open();
            Add(write, id, familyName);
        }

        private static void Add(CharContext characters, uint id, string familyName)
        {
            characters.GameAccountEntries.Add(new GameAccountEntry
            {
                Id = id,
                Email = $"family-{id}@example.invalid",
                Name = $"FamilyAccount{id}",
                FamilyName = familyName,
                SelectedSlot = 0,
                CanSkipBootcamp = false,
                CreatedAt = DateTime.UtcNow,
                LastLogin = DateTime.UtcNow,
                LastIp = "127.0.0.1",
                Level = 0
            });
            characters.SaveChanges();
        }

        private static void WithDatabase(Action<SqliteCharContext> body)
        {
            var path = Path.Combine(AppContext.BaseDirectory, "TestDatabases", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);

            try
            {
                using var context = (SqliteCharContext)PersistenceIntegrationTests.CreateContext(typeof(SqliteCharContext), Path.Combine(path, "database"));
                body(context);
            }
            finally
            {
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

                try
                {
                    Directory.Delete(path, true);
                }
                catch (IOException)
                {
                }
            }
        }

        #endregion
    }
}
