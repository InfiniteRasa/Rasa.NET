using System;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Database
{
    using Rasa.Configuration;
    using Rasa.Configuration.ConnectionStrings;
    using Rasa.Configuration.ContextSetup;
    using Rasa.Context.Char;
    using Rasa.Repositories.Char.Character;
    using Rasa.Repositories.Char.CharacterMission;
    using Rasa.Repositories.Char.CharacterMissionDeadline;
    using Rasa.Repositories.Char.CharacterMissionScenario;
    using Rasa.Services.DbContext;
    using Rasa.Structures.Char;

    [TestClass]
    [DoNotParallelize]
    public class MissionDeadlinePersistenceTests
    {
        [TestMethod]
        public void MissionDeadlinesAndScenarioStepsRoundTripAcrossReconnect()
        {
            var dueAtUtc = new DateTime(2026, 9, 19, 6, 30, 0, DateTimeKind.Utc);

            WithDisposableSqlite((context, database) =>
            {
                context.Database.Migrate();
                SeedCharacter(context, 17, 123, 1);
                SeedMission(context, 123, 429);

                var deadlines = new CharacterMissionDeadlineRepository(context);
                var scenarios = new CharacterMissionScenarioRepository(context);

                deadlines.Add(new CharacterMissionDeadlineEntry(
                    123,
                    429,
                    dueAtUtc,
                    CharacterMissionDeadlineState.Active));
                scenarios.Add(new CharacterMissionScenarioStepEntry(
                    123,
                    429,
                    "spawn.wave-1"));
                scenarios.Add(new CharacterMissionScenarioStepEntry(
                    123,
                    429,
                    "cleanup.wave-1"));

                using var reopened = Open(database);
                var savedDeadline = new CharacterMissionDeadlineRepository(reopened).Get(123, 429);
                Assert.IsNotNull(savedDeadline);
                Assert.AreEqual(dueAtUtc, savedDeadline.DueAtUtc);
                Assert.AreEqual(DateTimeKind.Utc, savedDeadline.DueAtUtc.Kind);
                Assert.AreEqual(CharacterMissionDeadlineState.Active, savedDeadline.State);

                var savedSteps = new CharacterMissionScenarioRepository(reopened).Get(123, 429);
                CollectionAssert.AreEquivalent(
                    new[] { "spawn.wave-1", "cleanup.wave-1" },
                    savedSteps.Select(entry => entry.StepKey).ToArray());
            });
        }

        [TestMethod]
        public void DuplicateDeadlineAndScenarioStepWritesAreRejectedWithoutResettingState()
        {
            var originalDeadline = new DateTime(2026, 9, 19, 6, 30, 0, DateTimeKind.Utc);

            WithDisposableSqlite((context, database) =>
            {
                context.Database.Migrate();
                SeedCharacter(context, 17, 123, 1);
                SeedMission(context, 123, 429);

                new CharacterMissionDeadlineRepository(context).Add(new CharacterMissionDeadlineEntry(
                    123,
                    429,
                    originalDeadline,
                    CharacterMissionDeadlineState.Active));
                new CharacterMissionScenarioRepository(context).Add(new CharacterMissionScenarioStepEntry(
                    123,
                    429,
                    "spawn.wave-1"));

                using (var duplicate = Open(database))
                {
                    var deadlines = new CharacterMissionDeadlineRepository(duplicate);
                    var scenarios = new CharacterMissionScenarioRepository(duplicate);

                    Assert.ThrowsExactly<DbUpdateException>(() =>
                        deadlines.Add(new CharacterMissionDeadlineEntry(
                            123,
                            429,
                            originalDeadline.AddHours(4),
                            CharacterMissionDeadlineState.Expired)));
                    Assert.ThrowsExactly<DbUpdateException>(() =>
                        scenarios.Add(new CharacterMissionScenarioStepEntry(
                            123,
                            429,
                            "spawn.wave-1")));
                }

                using var reopened = Open(database);
                var savedDeadline = new CharacterMissionDeadlineRepository(reopened).Get(123, 429);
                Assert.IsNotNull(savedDeadline);
                Assert.AreEqual(originalDeadline, savedDeadline.DueAtUtc);
                Assert.AreEqual(CharacterMissionDeadlineState.Active, savedDeadline.State);
                Assert.AreEqual(1, new CharacterMissionScenarioRepository(reopened).Get(123, 429).Count);
            });
        }

        [TestMethod]
        public void DeletingCharacterCleansMissionDeadlinesAndScenarioSteps()
        {
            WithDisposableSqlite((context, database) =>
            {
                context.Database.Migrate();
                SeedCharacter(context, 17, 123, 1);
                SeedMission(context, 123, 429);
                new CharacterMissionDeadlineRepository(context).Add(new CharacterMissionDeadlineEntry(
                    123,
                    429,
                    new DateTime(2026, 9, 19, 6, 30, 0, DateTimeKind.Utc),
                    CharacterMissionDeadlineState.Active));
                new CharacterMissionScenarioRepository(context).Add(new CharacterMissionScenarioStepEntry(
                    123,
                    429,
                    "spawn.wave-1"));

                using (var deleteContext = Open(database))
                {
                    var characters = new CharacterRepository(deleteContext);
                    characters.Delete(123);
                    deleteContext.SaveChanges();
                }

                using var reopened = Open(database);
                Assert.AreEqual(0, reopened.CharacterEntries.Count(entry => entry.Id == 123));
                Assert.AreEqual(0, reopened.CharacterMissionEntries.Count(entry => entry.CharacterId == 123));
                Assert.AreEqual(0, reopened.Set<CharacterMissionDeadlineEntry>().Count(entry => entry.CharacterId == 123));
                Assert.AreEqual(0, reopened.Set<CharacterMissionScenarioStepEntry>().Count(entry => entry.CharacterId == 123));
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
            byte slot)
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
                "1, 1, 0, 1, 4000, 9, 100, 50, 0, 0, 0, 1220, 1, 2, 3, 0)");
        }

        private static void SeedMission(SqliteCharContext context, uint characterId, uint missionId)
        {
            new CharacterMissionRepository(context).Add(new CharacterMissionEntry(characterId, missionId, 0));
        }
    }
}
