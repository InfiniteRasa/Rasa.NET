using System;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Database
{
    using Rasa.Configuration;
    using Rasa.Configuration.ConnectionStrings;
    using Rasa.Configuration.ContextSetup;
    using Rasa.Context.Char;
    using Rasa.Repositories.Char.Character;
    using Rasa.Repositories.Char.CharacterFlag;
    using Rasa.Repositories.Char.CharacterStartingExperience;
    using Rasa.Services.DbContext;
    using Rasa.Structures.Char;

    [TestClass]
    [DoNotParallelize]
    public class StartingExperiencePersistenceTests
    {
        [TestMethod]
        public void StartingExperienceAndQualificationsRoundTripAcrossReconnect()
        {
            WithDisposableSqlite((context, database) =>
            {
                context.Database.Migrate();
                SeedCharacter(context, 17, 123, 1, 1220);

                var experience = new CharacterStartingExperienceRepository(context);
                var qualifications = new CharacterFlagRepository(context);

                experience.Add(new CharacterStartingExperienceEntry(
                    123,
                    "deployment_11",
                    CharacterStartingExperienceState.Pending));
                qualifications.Add(new CharacterFlagEntry(
                    123,
                    CharacterFlagIds.BootcampComplete));

                using var reopened = Open(database);
                var savedExperience = new CharacterStartingExperienceRepository(reopened).Get(123);
                Assert.IsNotNull(savedExperience);
                Assert.AreEqual("deployment_11", savedExperience.ContentRevision);
                Assert.AreEqual(CharacterStartingExperienceState.Pending, savedExperience.State);

                Assert.IsTrue(new CharacterFlagRepository(reopened)
                    .HasValue(123, CharacterFlagIds.BootcampComplete));
            });
        }

        [TestMethod]
        public void FreshSchemaDoesNotInventLegacyStartingExperience()
        {
            WithDisposableSqlite((context, database) =>
            {
                context.Database.Migrate();
                SeedCharacter(context, 17, 123, 1, 7777);
                context.Initialize();

                using var reopened = Open(database);
                Assert.AreEqual(7777U, new CharacterRepository(reopened).Get(123).MapContextId);
                Assert.IsNull(new CharacterStartingExperienceRepository(reopened).Get(123));
                Assert.IsFalse(reopened.Database.GetMigrations().Any(id => id.Contains("LegacyBackfill")));
            });
        }

        [TestMethod]
        public void DeletingCharacterCleansStartingExperienceAndQualifications()
        {
            WithDisposableSqlite((context, database) =>
            {
                context.Database.Migrate();
                SeedCharacter(context, 17, 123, 1, 1220);
                new CharacterStartingExperienceRepository(context).Add(new CharacterStartingExperienceEntry(
                    123,
                    "deployment_11",
                    CharacterStartingExperienceState.Pending));
                new CharacterFlagRepository(context).Add(new CharacterFlagEntry(
                    123,
                    CharacterFlagIds.BootcampComplete));

                using (var deleteContext = Open(database))
                {
                    var characters = new CharacterRepository(deleteContext);
                    characters.Delete(123);
                    deleteContext.SaveChanges();
                }

                using var reopened = Open(database);
                Assert.AreEqual(0, reopened.CharacterEntries.Count(entry => entry.Id == 123));
                Assert.AreEqual(0, reopened.Set<CharacterStartingExperienceEntry>().Count(entry => entry.CharacterId == 123));
                Assert.AreEqual(0, reopened.Set<CharacterFlagEntry>().Count(entry => entry.CharacterId == 123));
            });
        }

        private static void WithDisposableSqlite(Action<SqliteCharContext, string> action)
        {
            var path = Path.Combine(
                AppContext.BaseDirectory,
                "TestDatabases",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            try
            {
                var database = Path.Combine(path, "database");
                using var context = Open(database);
                action(context, database);
            }
            finally
            {
                SqliteConnection.ClearAllPools();
                Directory.Delete(path, true);
            }
        }

        private static SqliteCharContext Open(string database) =>
            new(
                Options.Create(new DatabaseConfiguration
                {
                    Provider = "Sqlite",
                    Char = new DatabaseConnectionConfiguration { Database = database }
                }),
                new SqliteDbContextConfigurationService(new SqliteConnectionStringFactory()),
                new SqliteDbContextPropertyModifier());

        private static void SeedCharacter(
            SqliteCharContext context,
            uint accountId,
            uint characterId,
            byte slot,
            uint mapContextId)
        {
            context.Database.ExecuteSqlRaw(
                "INSERT INTO account (id, email, name, family_name) VALUES " +
                $"({accountId}, 'task4-{accountId}@example.invalid', " +
                $"'Task4Account{accountId}', 'Task4Family{accountId}')");
            context.Database.ExecuteSqlRaw(
                "INSERT INTO character (id, account_id, slot, name, race, class, gender, scale, " +
                "experience, level, credit, prestige, body, mind, spirit, map_context_id, coord_x, " +
                "coord_y, coord_z, rotation) VALUES " +
                $"({characterId}, {accountId}, {slot}, 'Task4Character{characterId}', " +
                $"1, 1, 0, 1, 4000, 9, 100, 50, 0, 0, 0, {mapContextId}, 1, 2, 3, 0)");
        }
    }
}
