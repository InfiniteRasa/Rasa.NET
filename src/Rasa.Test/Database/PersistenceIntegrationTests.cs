using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Database
{
    using Rasa.Configuration;
    using Rasa.Configuration.ConnectionStrings;
    using Rasa.Configuration.ContextSetup;
    using Rasa.Context;
    using Rasa.Context.Auth;
    using Rasa.Context.Char;
    using Rasa.Context.World;
    using Rasa.Repositories.Char;
    using Rasa.Repositories.Char.Auction;
    using Rasa.Repositories.Char.Character;
    using Rasa.Repositories.Char.CharacterAbilityDrawer;
    using Rasa.Repositories.Char.CharacterMissionDeadline;
    using Rasa.Repositories.Char.CharacterInventory;
    using Rasa.Repositories.Char.CharacterMission;
    using Rasa.Repositories.Char.CharacterMissionProgress;
    using Rasa.Repositories.Char.CharacterMissionScenario;
    using Rasa.Repositories.Char.CharacterFlag;
    using Rasa.Repositories.Char.CharacterStartingExperience;
    using Rasa.Repositories.Char.ClanInventory;
    using Rasa.Repositories.Char.ClanLockboxLog;
    using Rasa.Repositories.Char.Items;
    using Rasa.Services.DbContext;
    using Rasa.Structures.Char;

    [TestClass]
    [DoNotParallelize]
    public class PersistenceIntegrationTests
    {
        private static readonly Type[] ContextTypes =
        {
            typeof(SqliteAuthContext),
            typeof(SqliteCharContext),
            typeof(SqliteWorldContext),
            typeof(MySqlAuthContext),
            typeof(MySqlCharContext),
            typeof(MySqlWorldContext)
        };

        [TestMethod]
        public void CombinedCharacterPersistenceContractsAreExposed()
        {
            var accountSlotLookup = typeof(ICharacterMissionRepository).GetMethod(
                "Get",
                new[] { typeof(uint), typeof(uint) });
            Assert.IsNotNull(accountSlotLookup);
            Assert.AreEqual(typeof(List<CharacterMissionEntry>), accountSlotLookup.ReturnType);
            Assert.IsNotNull(typeof(ICharacterMissionRepository).GetMethod(
                "GetByCharacterAndMission",
                new[] { typeof(uint), typeof(uint) }));
            Assert.IsNotNull(typeof(Rasa.Structures.Char.CharacterEntry)
                .GetProperty("CurrentAbilitySlot"));
            Assert.IsNotNull(typeof(ICharacterRepository).GetMethod(
                "UpdateCharacterCurrencies",
                new[] { typeof(uint), typeof(int), typeof(int) }));
            Assert.IsNotNull(typeof(ICharacterRepository).GetMethod(
                "UpdateCharacterProgression",
                new[] { typeof(uint), typeof(uint), typeof(byte) }));
            Assert.IsNotNull(typeof(ICharacterRepository).GetMethod(
                "UpdateCharacterAbilitySlot",
                new[] { typeof(uint), typeof(byte) }));
            Assert.IsNotNull(typeof(ICharacterMissionDeadlineRepository).GetMethod(
                "Get",
                new[] { typeof(uint), typeof(uint) }));
            Assert.IsNotNull(typeof(ICharacterMissionScenarioRepository).GetMethod(
                "HasStep",
                new[] { typeof(uint), typeof(uint), typeof(string) }));
            Assert.IsNotNull(typeof(ICharacterStartingExperienceRepository).GetMethod(
                "Get",
                new[] { typeof(uint) }));
            Assert.IsNotNull(typeof(ICharacterFlagRepository).GetMethod(
                "HasValue",
                new[] { typeof(uint), typeof(uint), typeof(uint) }));
            Assert.IsNotNull(typeof(ICharUnitOfWork).GetProperty("CharacterMissionProgress"));
            Assert.IsNotNull(typeof(ICharUnitOfWork).GetProperty("CharacterMissionDeadlines"));
            Assert.IsNotNull(typeof(ICharUnitOfWork).GetProperty("CharacterMissionScenario"));
            Assert.IsNotNull(typeof(ICharUnitOfWork).GetProperty("CharacterStartingExperience"));
            Assert.IsNotNull(typeof(ICharUnitOfWork).GetProperty("CharacterFlags"));
            Assert.IsNotNull(typeof(ICharUnitOfWork).GetMethod(
                "ExecuteTransaction",
                new[] { typeof(Action) }));
        }

        [TestMethod]
        public void CombinedCharModelIncludesAbilitySelectionAndMissionProgress()
        {
            using var context = CreateContext(typeof(SqliteCharContext), "unused");
            var model = context.Model;
            var character = RequireEntity(model, "Rasa.Structures.Char.CharacterEntry");
            var mission = RequireEntity(model, "Rasa.Structures.Char.CharacterMissionEntry");
            var objective = RequireEntity(model, "Rasa.Structures.Char.CharacterMissionObjectiveEntry");
            var counter = RequireEntity(model, "Rasa.Structures.Char.CharacterMissionObjectiveCounterEntry");
            var itemCounter = RequireEntity(
                model,
                "Rasa.Structures.Char.CharacterMissionObjectiveItemCounterEntry");

            Assert.AreEqual("current_ability_slot",
                character.FindProperty("CurrentAbilitySlot")?.GetColumnName());
            CollectionAssert.AreEqual(
                new[] { "CharacterId", "MissionId" },
                mission.FindPrimaryKey()?.Properties.Select(property => property.Name).ToArray());
            Assert.AreEqual(false, mission.FindProperty("Completeable")?.GetDefaultValue());
            Assert.AreEqual(
                DeleteBehavior.Cascade,
                mission.GetForeignKeys().Single(foreignKey =>
                    foreignKey.PrincipalEntityType.ClrType == typeof(CharacterEntry)).DeleteBehavior);
            CollectionAssert.AreEqual(
                new[] { "CharacterId", "MissionId", "ObjectiveId" },
                objective.FindPrimaryKey()?.Properties.Select(property => property.Name).ToArray());
            CollectionAssert.AreEqual(
                new[] { "CharacterId", "MissionId", "ObjectiveId", "CounterId" },
                counter.FindPrimaryKey()?.Properties.Select(property => property.Name).ToArray());
            CollectionAssert.AreEqual(
                new[] { "CharacterId", "MissionId", "ObjectiveId", "ItemClassId" },
                itemCounter.FindPrimaryKey()?.Properties.Select(property => property.Name).ToArray());
            Assert.IsTrue(objective.FindProperty("ObjectiveState")!.IsConcurrencyToken);
            Assert.IsTrue(counter.FindProperty("CounterValue")!.IsConcurrencyToken);
            Assert.IsTrue(itemCounter.FindProperty("CounterValue")!.IsConcurrencyToken);

            var auction = RequireEntity(model, "Rasa.Structures.Char.AuctionEntry");
            var lockboxLog = RequireEntity(model, "Rasa.Structures.Char.ClanLockboxLogEntry");
            var petition = RequireEntity(model, "Rasa.Structures.Char.PetitionEntry");
            Assert.IsTrue(auction.GetIndexes().Any(index =>
                index.GetDatabaseName() == "auction_index_seller_id"));
            Assert.IsTrue(lockboxLog.GetIndexes().Any(index =>
                index.GetDatabaseName() == "clan_lockbox_log_index_clan_id_time"));
            Assert.AreEqual("double", petition.FindProperty("PosX")?.GetColumnType());
            Assert.AreEqual("double", petition.FindProperty("PosY")?.GetColumnType());
            Assert.AreEqual("double", petition.FindProperty("PosZ")?.GetColumnType());
        }

        [TestMethod]
        [DataRow(typeof(SqliteCharContext), "TEXT", "tinyint(3)", "int(11)")]
        [DataRow(typeof(MySqlCharContext), "datetime(6)", "tinyint(3) unsigned", "int(11) unsigned")]
        public void CombinedCharModelIncludesMissionDurabilityStartingExperienceAndFlags(
            Type contextType,
            string dueAtColumnType,
            string stateColumnType,
            string qualificationColumnType)
        {
            using var context = CreateContext(contextType, "unused");
            var model = context.GetService<IDesignTimeModel>().Model;
            var deadline = RequireEntity(model, "Rasa.Structures.Char.CharacterMissionDeadlineEntry");
            var scenarioStep = RequireEntity(model, "Rasa.Structures.Char.CharacterMissionScenarioStepEntry");
            var startingExperience = RequireEntity(model, "Rasa.Structures.Char.CharacterStartingExperienceEntry");
            var qualification = RequireEntity(model, "Rasa.Structures.Char.CharacterFlagEntry");

            CollectionAssert.AreEqual(
                new[] { "CharacterId", "MissionId" },
                deadline.FindPrimaryKey()?.Properties.Select(property => property.Name).ToArray());
            CollectionAssert.AreEqual(
                new[] { "CharacterId", "MissionId", "StepKey" },
                scenarioStep.FindPrimaryKey()?.Properties.Select(property => property.Name).ToArray());
            CollectionAssert.AreEqual(
                new[] { "CharacterId" },
                startingExperience.FindPrimaryKey()?.Properties.Select(property => property.Name).ToArray());
            CollectionAssert.AreEqual(
                new[] { "CharacterId", "FlagId" },
                qualification.FindPrimaryKey()?.Properties.Select(property => property.Name).ToArray());

            Assert.AreEqual("due_at_utc", deadline.FindProperty("DueAtUtc")?.GetColumnName());
            Assert.AreEqual(dueAtColumnType, deadline.FindProperty("DueAtUtc")?.GetColumnType());
            Assert.AreEqual("state", deadline.FindProperty("State")?.GetColumnName());
            Assert.AreEqual(stateColumnType, deadline.FindProperty("State")?.GetColumnType());
            Assert.AreEqual("step_key", scenarioStep.FindProperty("StepKey")?.GetColumnName());
            Assert.AreEqual("varchar(64)", scenarioStep.FindProperty("StepKey")?.GetColumnType());
            Assert.AreEqual("content_revision", startingExperience.FindProperty("ContentRevision")?.GetColumnName());
            Assert.AreEqual("varchar(32)", startingExperience.FindProperty("ContentRevision")?.GetColumnType());
            Assert.AreEqual(stateColumnType, startingExperience.FindProperty("State")?.GetColumnType());
            Assert.AreEqual("flag_id", qualification.FindProperty("FlagId")?.GetColumnName());
            Assert.AreEqual(qualificationColumnType, qualification.FindProperty("FlagId")?.GetColumnType());
            Assert.AreEqual("value", qualification.FindProperty("Value")?.GetColumnName());
            Assert.AreEqual(qualificationColumnType, qualification.FindProperty("Value")?.GetColumnType());
            Assert.IsNull(model.FindEntityType("Rasa.Structures.Char.CharacterQualificationEntry"));

            Assert.AreEqual(
                DeleteBehavior.Cascade,
                deadline.GetForeignKeys().Single().DeleteBehavior);
            Assert.AreEqual(
                DeleteBehavior.Cascade,
                scenarioStep.GetForeignKeys().Single().DeleteBehavior);
            Assert.AreEqual(
                DeleteBehavior.Cascade,
                startingExperience.GetForeignKeys().Single().DeleteBehavior);
            Assert.AreEqual(
                DeleteBehavior.Cascade,
                qualification.GetForeignKeys().Single().DeleteBehavior);

            Assert.IsTrue(deadline.GetCheckConstraints().Any(constraint =>
                constraint.Name == "CK_character_mission_deadline_state"));
            Assert.IsTrue(startingExperience.GetCheckConstraints().Any(constraint =>
                constraint.Name == "CK_character_starting_experience_state"));
            Assert.IsTrue(qualification.GetCheckConstraints().Any(constraint =>
                constraint.Name == "CK_character_flag_id"));

            var dueAtConverter = deadline.FindProperty("DueAtUtc")?.GetTypeMapping().Converter;
            Assert.IsNotNull(dueAtConverter);
            Assert.AreEqual(
                DateTimeKind.Utc,
                ((DateTime)dueAtConverter!.ConvertFromProvider(new DateTime(2026, 9, 19, 6, 30, 0))).Kind);
            AssertEnumConversion(
                deadline,
                "State",
                CharacterMissionDeadlineState.Cancelled,
                (byte)CharacterMissionDeadlineState.Cancelled);
            AssertEnumConversion(
                startingExperience,
                "State",
                CharacterStartingExperienceState.Legacy,
                (byte)CharacterStartingExperienceState.Legacy);
            Assert.AreEqual(typeof(uint), qualification.FindProperty("FlagId").ClrType);
            Assert.AreEqual(typeof(uint), qualification.FindProperty("Value").ClrType);
        }

        [TestMethod]
        public void Pr95WorldModelRetainsIndexesAndColumnContracts()
        {
            using var context = CreateContext(typeof(SqliteWorldContext), "unused");
            var model = context.Model;
            var marker = RequireEntity(model, "Rasa.Structures.World.MapMarkerEntry");
            var action = RequireEntity(model, "Rasa.Structures.World.ActionEntry");
            var skillCharacter = RequireEntity(model, "Rasa.Structures.World.SkillCharacterEntry");

            Assert.IsTrue(marker.GetIndexes().Any(index =>
                index.GetDatabaseName() == "map_marker_index_map_context_id"));
            Assert.AreEqual("varchar(64)", action.FindProperty("Name")?.GetColumnType());
            Assert.AreEqual("varchar(64)", action.FindProperty("Module")?.GetColumnType());
            Assert.AreEqual(ValueGenerated.Never,
                skillCharacter.FindProperty("Id")?.ValueGenerated);
        }

        [TestMethod]
        public void MigrationSnapshotsMatchCombinedModels()
        {
            foreach (var contextType in ContextTypes)
            {
                using var context = CreateContext(contextType, "unused");
                Assert.IsFalse(context.Database.HasPendingModelChanges(), contextType.Name);
            }
        }

        [TestMethod]
        [DataRow(typeof(SqliteAuthContext), 0)]
        [DataRow(typeof(MySqlAuthContext), 0)]
        [DataRow(typeof(SqliteCharContext), 1)]
        [DataRow(typeof(MySqlCharContext), 1)]
        [DataRow(typeof(SqliteWorldContext), 2)]
        [DataRow(typeof(MySqlWorldContext), 2)]
        public void BranchMigrationsAreConsolidatedByDatabase(Type contextType, int expectedCount)
        {
            using var context = CreateContext(contextType, "unused");
            var added = context.Database.GetMigrations()
                .Where(id => string.CompareOrdinal(id, "202609") >= 0).ToArray();

            Assert.AreEqual(expectedCount, added.Length, contextType.Name);
            if (expectedCount == 1)
                StringAssert.EndsWith(added[0], "_ConsolidatedCharacterSchema");
            if (expectedCount == 2)
            {
                StringAssert.EndsWith(added[0], "_ConsolidatedWorldSchema");
                StringAssert.EndsWith(added[1], "_SeedWorldContent");
            }
        }

        [TestMethod]
        public void MigrationIdsAreUniqueAndOrdered()
        {
            foreach (var contextType in ContextTypes)
            {
                using var context = CreateContext(contextType, "unused");
                var migrations = context.Database.GetMigrations().ToArray();
                Assert.AreEqual(migrations.Length, migrations.Distinct().Count(), contextType.Name);
                CollectionAssert.AreEqual(
                    migrations.OrderBy(id => id, StringComparer.Ordinal).ToArray(),
                    migrations, contextType.Name);
            }
        }

        [TestMethod]
        public void MySqlCharacterSchemaPreservesIdentityWhenChangingPrimaryKeys()
        {
            var migration = new Rasa.Migrations.MySqlChar.ConsolidatedCharacterSchema();
            foreach (var table in new[] { "character_mission", "friend", "ignored" })
            {
                var alterUp = migration.UpOperations.Select((operation, index) => (operation, index))
                    .Single(item => item.operation is AlterColumnOperation column && column.Table == table).index;
                var dropKeyUp = migration.UpOperations.Select((operation, index) => (operation, index))
                    .Single(item => item.operation is DropPrimaryKeyOperation key && key.Table == table).index;
                Assert.IsTrue(alterUp < dropKeyUp, table);
                var addKeyDown = migration.DownOperations.Select((operation, index) => (operation, index))
                    .Single(item => item.operation is AddPrimaryKeyOperation key && key.Table == table).index;
                var alterDown = migration.DownOperations.Select((operation, index) => (operation, index))
                    .Single(item => item.operation is AlterColumnOperation column && column.Table == table).index;
                Assert.IsTrue(addKeyDown < alterDown, table);
            }
            Assert.AreEqual(3, migration.UpOperations.OfType<AlterColumnOperation>().Count());
            Assert.AreEqual(3, migration.DownOperations.OfType<AlterColumnOperation>().Count());
        }

        [TestMethod]
        [DataRow(typeof(MySqlAuthContext))]
        [DataRow(typeof(MySqlCharContext))]
        [DataRow(typeof(MySqlWorldContext))]
        public void MySqlMigrationsGenerateOfflineUpgradeSql(Type contextType)
        {
            using var context = CreateContext(contextType, "unused");
            var script = context.GetService<IMigrator>().GenerateScript();

            StringAssert.Contains(script, "CREATE TABLE");
            StringAssert.Contains(script, "__EFMigrationsHistory");
            if (contextType == typeof(MySqlCharContext))
            {
                StringAssert.Contains(script, "ConsolidatedCharacterSchema");
            }
        }

        [TestMethod]
        [DataRow(typeof(SqliteCharContext))]
        [DataRow(typeof(MySqlCharContext))]
        public void CharMigrationsGenerateOfflineSqlForMissionDurabilityAndStartingExperience(Type contextType)
        {
            using var context = CreateContext(contextType, "unused");
            var script = context.GetService<IMigrator>().GenerateScript();

            StringAssert.Contains(script, "character_mission_deadline");
            StringAssert.Contains(script, "character_mission_scenario_step");
            StringAssert.Contains(script, "character_starting_experience");
            StringAssert.Contains(script, "character_flag");
            StringAssert.Contains(script, "CK_character_mission_deadline_state");
            StringAssert.Contains(script, "CK_character_starting_experience_state");
            StringAssert.Contains(script, "CK_character_flag_id");
        }

        [TestMethod]
        public void MySqlMissionDurabilityOfflineSqlCapturesDatabaseParity()
        {
            using var context = CreateContext(typeof(MySqlCharContext), "unused");
            var sql = NormalizeSql(context.GetService<IMigrator>().GenerateScript());

            StringAssert.Contains(sql, "create table character_mission_deadline");
            StringAssert.Contains(sql, "primary key (character_id, mission_id)");
            StringAssert.Contains(sql, "create table character_mission_scenario_step");
            StringAssert.Contains(sql, "primary key (character_id, mission_id, step_key)");
            StringAssert.Contains(sql, "step_key varchar(64)");
            StringAssert.Contains(sql, "due_at_utc datetime(6) not null");
            StringAssert.Contains(sql, "constraint ck_character_mission_deadline_state check (state in (1, 2, 3, 4))");
            StringAssert.Contains(sql, "constraint ck_character_starting_experience_state check (state in (1, 2, 3, 4, 5))");
            StringAssert.Contains(sql, "constraint ck_character_flag_id check (flag_id between 1 and 4294967295)");
            StringAssert.Contains(sql, "foreign key (character_id, mission_id) references character_mission (character_id, mission_id) on delete cascade");
            StringAssert.Contains(sql, "foreign key (character_id) references character (id) on delete cascade");
        }

        [TestMethod]
        [DataRow(typeof(SqliteAuthContext))]
        [DataRow(typeof(SqliteCharContext))]
        [DataRow(typeof(SqliteWorldContext))]
        public void SqliteContextsMigrateCleanDatabaseAndReopen(Type contextType)
        {
            var path = Path.Combine(
                AppContext.BaseDirectory,
                "TestDatabases",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            try
            {
                var database = Path.Combine(path, "database");
                using (var context = CreateContext(contextType, database))
                {
                    context.Database.Migrate();
                    CollectionAssert.AreEqual(
                        context.Database.GetMigrations().ToArray(),
                        context.Database.GetAppliedMigrations().ToArray());
                }

                using var reopened = CreateContext(contextType, database);
                reopened.Database.Migrate();
                Assert.IsFalse(reopened.Database.GetPendingMigrations().Any());
            }
            finally
            {
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                Directory.Delete(path, true);
            }
        }

        [TestMethod]
        public void SqliteCharSchemaPersistsMissionProgressAndAbilitySelection()
        {
            WithDisposableSqlite((context, database) =>
            {
                context.Database.Migrate();
                context.Database.ExecuteSqlRaw(
                    "INSERT INTO account (id, email, name, family_name) " +
                    "VALUES (17, 'task4@example.invalid', 'Task4Account', 'Task4Family')");
                context.Database.ExecuteSqlRaw(
                    "INSERT INTO character (id, account_id, slot, name, race, class, gender, scale, " +
                    "experience, level, credit, body, mind, spirit, map_context_id, coord_x, coord_y, " +
                    "coord_z, rotation) VALUES " +
                    "(123, 17, 1, 'Task4Preserved', 1, 1, 0, 1, 4000, 9, 100, 0, 0, 0, 7777, 1, 2, 3, 0)");
                context.Database.ExecuteSqlRaw(
                    "INSERT INTO character_mission (character_id, mission_id, mission_state, assignment_id) " +
                    "VALUES (123, 321, 0, 'aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa')");

                context.Database.Migrate();
                context.Database.ExecuteSqlRaw(
                    "UPDATE character SET current_ability_slot = 24 WHERE id = 123");
                context.Database.ExecuteSqlRaw(
                    "INSERT INTO character_mission " +
                    "(character_id, mission_id, mission_state, completeable, assignment_id) " +
                    "VALUES (123, 429, 4, 1, 'bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb')");
                var orphan = Assert.ThrowsExactly<Microsoft.Data.Sqlite.SqliteException>(() =>
                    context.Database.ExecuteSqlRaw(
                        "INSERT INTO character_mission (character_id, mission_id, mission_state, assignment_id) " +
                        "VALUES (999, 654, 0, 'cccccccccccccccccccccccccccccccc')"));
                Assert.AreEqual(787, orphan.SqliteExtendedErrorCode);
                context.Database.ExecuteSqlRaw(
                    "INSERT INTO character_mission_objective " +
                    "(character_id, mission_id, objective_id, objective_state) " +
                    "VALUES (123, 429, 5, 1)");
                context.Database.ExecuteSqlRaw(
                    "INSERT INTO character_mission_objective_counter " +
                    "(character_id, mission_id, objective_id, counter_id, counter_value) " +
                    "VALUES (123, 429, 5, 3, 7)");
                context.Database.ExecuteSqlRaw(
                    "INSERT INTO character_mission_objective_item_counter " +
                    "(character_id, mission_id, objective_id, item_class_id, counter_value) " +
                    "VALUES (123, 429, 5, 200, 8)");

                using var reopened = (SqliteCharContext)CreateContext(
                    typeof(SqliteCharContext),
                    database);
                Assert.AreEqual(24, reopened.Database.SqlQueryRaw<int>(
                    "SELECT current_ability_slot AS Value FROM character WHERE id = 123").Single());
                Assert.AreEqual(2, reopened.Database.SqlQueryRaw<int>(
                    "SELECT COUNT(*) AS Value FROM character_mission WHERE character_id = 123").Single());
                Assert.AreEqual(0, reopened.Database.SqlQueryRaw<int>(
                    "SELECT COUNT(*) AS Value FROM character_mission WHERE character_id = 999").Single());
                Assert.AreEqual(7, reopened.Database.SqlQueryRaw<int>(
                    "SELECT counter_value AS Value FROM character_mission_objective_counter " +
                    "WHERE character_id = 123 AND mission_id = 429 AND objective_id = 5 " +
                    "AND counter_id = 3").Single());
                Assert.AreEqual(8, reopened.Database.SqlQueryRaw<int>(
                    "SELECT counter_value AS Value FROM character_mission_objective_item_counter " +
                    "WHERE character_id = 123 AND mission_id = 429 AND objective_id = 5 " +
                    "AND item_class_id = 200").Single());
                Assert.AreEqual(7777, reopened.Database.SqlQueryRaw<int>(
                    "SELECT map_context_id AS Value FROM character WHERE id = 123").Single());
                Assert.IsEmpty(reopened.CharacterStartingExperienceEntries.ToArray());
                Assert.IsFalse(reopened.Database.GetPendingMigrations().Any());
            });
        }

        [TestMethod]
        public void RepositoriesRoundTripCombinedCharacterAndMissionState()
        {
            WithDisposableSqlite((context, database) =>
            {
                context.Database.Migrate();
                SeedCharacter(context, 17, 123, 1);
                SeedCharacter(context, 18, 456, 2);

                using (var unit = CreateUnit(context))
                {
                    unit.ExecuteTransaction(() =>
                    {
                        unit.Characters.UpdateCharacterProgression(123, 4500, 10);
                        unit.Characters.UpdateCharacterCurrencies(123, 107, 53);
                        unit.Characters.UpdateCharacterAbilitySlot(123, 24);
                        unit.CharacterAbilityDrawers.AddOrUpdate(123, 24, 401, 3);
                        unit.CharacterMissions.Add(new CharacterMissionEntry(123, 321, 0));
                        unit.CharacterMissions.Add(new CharacterMissionEntry(123, 429, 4)
                        {
                            Completeable = true
                        });
                        unit.CharacterMissions.Add(new CharacterMissionEntry(456, 321, 3));
                        unit.CharacterMissionProgress.AddObjectives(new[]
                        {
                            new CharacterMissionObjectiveEntry(123, 429, 5, 1)
                            {
                                Counters =
                                {
                                    new CharacterMissionObjectiveCounterEntry(123, 429, 5, 3, 7)
                                },
                                ItemCounters =
                                {
                                    new CharacterMissionObjectiveItemCounterEntry(123, 429, 5, 200, 8)
                                }
                            }
                        });
                    });
                }

                using var reopenedContext = (SqliteCharContext)CreateContext(
                    typeof(SqliteCharContext),
                    database);
                using var reopened = CreateUnit(reopenedContext);
                var character = reopened.Characters.Get(123);
                Assert.AreEqual(4500U, character.Experience);
                Assert.AreEqual((byte)10, character.Level);
                Assert.AreEqual(107, character.Credit);
                Assert.AreEqual(53, character.Prestige);
                Assert.AreEqual((byte)24, character.CurrentAbilitySlot);
                Assert.AreEqual((24, 401, 3U), reopened.CharacterAbilityDrawers
                    .GetCharacterAbilities(123)
                    .Select(entry => (entry.AbilitySlot, entry.AbilityId, entry.AbilityLevel))
                    .Single());
                CollectionAssert.AreEquivalent(
                    new uint[] { 321, 429 },
                    reopened.CharacterMissions.Get(123)
                        .Select(entry => entry.MissionId)
                        .ToArray());
                CollectionAssert.AreEquivalent(
                    new uint[] { 321, 429 },
                    reopened.CharacterMissions.Get(17, 1)
                        .Select(entry => entry.MissionId)
                        .ToArray());
                var objective = reopened.CharacterMissionProgress.Get(123, 429)
                    .Missions[429].Objectives[5];
                Assert.AreEqual((byte)1, objective.State);
                Assert.AreEqual(7U, objective.Counters[3]);
                Assert.AreEqual(8U, objective.ItemCounters[200]);
            });
        }

        [TestMethod]
        public void CharacterDeletionCascadesMissionAndObjectiveProgressAtomically()
        {
            WithDisposableSqlite((context, database) =>
            {
                context.Database.Migrate();
                SeedCharacter(context, 17, 123, 1);
                using (var seed = CreateUnit(context))
                {
                    seed.CharacterMissions.Add(new CharacterMissionEntry(123, 429, 0));
                    seed.CharacterMissionProgress.AddObjectives(new[]
                    {
                        new CharacterMissionObjectiveEntry(123, 429, 5, 1)
                        {
                            Counters =
                            {
                                new CharacterMissionObjectiveCounterEntry(123, 429, 5, 3, 4)
                            },
                            ItemCounters =
                            {
                                new CharacterMissionObjectiveItemCounterEntry(123, 429, 5, 200, 6)
                            }
                        }
                    });
                }

                using (var deleteContext = (SqliteCharContext)CreateContext(
                    typeof(SqliteCharContext),
                    database))
                using (var delete = CreateUnit(deleteContext))
                {
                    delete.Characters.Delete(123);
                    delete.Complete();
                }

                using var reopened = (SqliteCharContext)CreateContext(
                    typeof(SqliteCharContext),
                    database);
                Assert.AreEqual(0, reopened.CharacterEntries.Count(entry => entry.Id == 123));
                Assert.AreEqual(0, reopened.CharacterMissionEntries.Count(
                    entry => entry.CharacterId == 123));
                Assert.AreEqual(0, reopened.CharacterMissionObjectiveEntries.Count(
                    entry => entry.CharacterId == 123));
                Assert.AreEqual(0, reopened.CharacterMissionObjectiveCounterEntries.Count(
                    entry => entry.CharacterId == 123));
                Assert.AreEqual(0, reopened.CharacterMissionObjectiveItemCounterEntries.Count(
                    entry => entry.CharacterId == 123));
            });
        }

        [TestMethod]
        public void FailedRewardTransactionRollsBackAllRepositoryWrites()
        {
            WithDisposableSqlite((context, database) =>
            {
                context.Database.Migrate();
                SeedCharacter(context, 17, 123, 1);
                using (var seed = CreateUnit(context))
                    seed.CharacterMissions.Add(new CharacterMissionEntry(123, 429, 0)
                    {
                        Completeable = true
                    });

                using (var unitContext = (SqliteCharContext)CreateContext(
                    typeof(SqliteCharContext),
                    database))
                using (var unit = CreateUnit(unitContext))
                {
                    Assert.ThrowsExactly<InvalidOperationException>(() =>
                        unit.ExecuteTransaction(() =>
                        {
                            unit.Characters.UpdateCharacterProgression(123, 4100, 10);
                            unit.Characters.UpdateCharacterCurrencies(123, 107, 53);
                            var itemId = unit.Items.CreateItem(new TestItemChange
                            {
                                ItemTemplateId = 28,
                                StackSize = 3
                            });
                            unit.CharacterInventories.AddInvItem(17, 123, 1, 50, itemId);
                            var mission = unit.CharacterMissions.GetByCharacterAndMission(123, 429);
                            mission.MissionState = 4;
                            mission.Completeable = false;
                            throw new InvalidOperationException("Injected reward failure.");
                        }));
                }

                using var reopenedContext = (SqliteCharContext)CreateContext(
                    typeof(SqliteCharContext),
                    database);
                using var reopened = CreateUnit(reopenedContext);
                var character = reopened.Characters.Get(123);
                Assert.AreEqual(4000U, character.Experience);
                Assert.AreEqual((byte)9, character.Level);
                Assert.AreEqual(100, character.Credit);
                Assert.AreEqual(50, character.Prestige);
                Assert.AreEqual(0, reopened.CharacterInventories.GetItems(17).Count);
                Assert.AreEqual(0, reopenedContext.ItemEntries.Count());
                var mission = reopened.CharacterMissions.GetByCharacterAndMission(123, 429);
                Assert.AreEqual(0U, mission.MissionState);
                Assert.IsTrue(mission.Completeable);
            });
        }

        [TestMethod]
        public void MissionProgressRejectsStaleUpdatesAndCascadesWithMission()
        {
            WithDisposableSqlite((context, database) =>
            {
                context.Database.Migrate();
                SeedCharacter(context, 17, 123, 1);
                using (var seed = CreateUnit(context))
                {
                    seed.CharacterMissions.Add(new CharacterMissionEntry(123, 429, 0));
                    seed.CharacterMissionProgress.AddObjectives(new[]
                    {
                        new CharacterMissionObjectiveEntry(123, 429, 5, 1)
                        {
                            Counters =
                            {
                                new CharacterMissionObjectiveCounterEntry(123, 429, 5, 3, 4)
                            },
                            ItemCounters =
                            {
                                new CharacterMissionObjectiveItemCounterEntry(123, 429, 5, 200, 6)
                            }
                        }
                    });
                }

                using (var currentContext = (SqliteCharContext)CreateContext(
                    typeof(SqliteCharContext),
                    database))
                using (var staleContext = (SqliteCharContext)CreateContext(
                    typeof(SqliteCharContext),
                    database))
                using (var current = CreateUnit(currentContext))
                using (var stale = CreateUnit(staleContext))
                {
                    current.CharacterMissionProgress.Get(123, 429, 5);
                    stale.CharacterMissionProgress.Get(123, 429, 5);
                    current.CharacterMissionProgress.SetCounter(123, 429, 5, 3, 4, 7);
                    Assert.ThrowsExactly<DbUpdateConcurrencyException>(() =>
                        stale.CharacterMissionProgress.SetCounter(123, 429, 5, 3, 4, 9));
                }

                using (var removeContext = (SqliteCharContext)CreateContext(
                    typeof(SqliteCharContext),
                    database))
                using (var remove = CreateUnit(removeContext))
                    remove.CharacterMissions.Remove(123, 429);

                using var reopenedContext = (SqliteCharContext)CreateContext(
                    typeof(SqliteCharContext),
                    database);
                using var reopened = CreateUnit(reopenedContext);
                Assert.AreEqual(0, reopened.CharacterMissionProgress.Get(123).Missions.Count);
            });
        }

        [TestMethod]
        public void CompetingRewardTransactionsCommitAtMostOnce()
        {
            WithDisposableSqlite((context, database) =>
            {
                context.Database.Migrate();
                SeedCharacter(context, 17, 123, 1);
                using (var seed = CreateUnit(context))
                    seed.CharacterMissions.Add(new CharacterMissionEntry(123, 429, 0)
                    {
                        Completeable = true
                    });

                using var start = new ManualResetEventSlim();

                bool TryGrant()
                {
                    start.Wait();
                    using var contenderContext = (SqliteCharContext)CreateContext(
                        typeof(SqliteCharContext),
                        database);
                    using var contender = CreateUnit(contenderContext);
                    var granted = false;
                    try
                    {
                        contender.ExecuteTransaction(() =>
                        {
                            var mission = contender.CharacterMissions.GetByCharacterAndMission(123, 429);
                            if (mission?.MissionState != 0 || !mission.Completeable)
                                return;

                            contender.Characters.UpdateCharacterProgression(123, 4100, 10);
                            contender.Characters.UpdateCharacterCurrencies(123, 107, 53);
                            mission.MissionState = 4;
                            mission.Completeable = false;
                            granted = true;
                        });
                        return granted;
                    }
                    catch (DbUpdateException)
                    {
                        return false;
                    }
                }

                var results = Task.WhenAll(
                    Task.Run(TryGrant),
                    Task.Run(TryGrant));
                start.Set();
                var completed = results.GetAwaiter().GetResult();

                Assert.AreEqual(1, completed.Count(result => result));
                using var reopenedContext = (SqliteCharContext)CreateContext(
                    typeof(SqliteCharContext),
                    database);
                using var reopened = CreateUnit(reopenedContext);
                var character = reopened.Characters.Get(123);
                Assert.AreEqual(4100U, character.Experience);
                Assert.AreEqual((byte)10, character.Level);
                Assert.AreEqual(107, character.Credit);
                Assert.AreEqual(53, character.Prestige);
                var mission = reopened.CharacterMissions.GetByCharacterAndMission(123, 429);
                Assert.AreEqual(4U, mission.MissionState);
                Assert.IsFalse(mission.Completeable);
            });
        }

        [TestMethod]
        public void CharUnitOfWorkKeepsPr95RepositoriesAlongsideMissionProgress()
        {
            using var context = (SqliteCharContext)CreateContext(
                typeof(SqliteCharContext),
                "unused");
            var auctions = new AuctionRepository(context);
            var clanInventories = new ClanInventoryRepository(context);
            var clanLockboxLogs = new ClanLockboxLogRepository(context);
            var items = new ItemRepository(context);
            var deadlines = new CharacterMissionDeadlineRepository(context);
            var progress = new CharacterMissionProgressRepository(context);
            var scenario = new CharacterMissionScenarioRepository(context);
            var qualifications = new CharacterFlagRepository(context);
            var startingExperience = new CharacterStartingExperienceRepository(context);
            using var unit = CreateUnit(
                context,
                auctions,
                clanInventories,
                clanLockboxLogs,
                items,
                deadlines,
                progress,
                scenario,
                qualifications,
                startingExperience);

            Assert.AreSame(auctions, unit.Auctions);
            Assert.AreSame(clanInventories, unit.ClanInventories);
            Assert.AreSame(clanLockboxLogs, unit.ClanLockboxLogs);
            Assert.AreSame(items, unit.Items);
            Assert.AreSame(deadlines, unit.CharacterMissionDeadlines);
            Assert.AreSame(progress, unit.CharacterMissionProgress);
            Assert.AreSame(scenario, unit.CharacterMissionScenario);
            Assert.AreSame(qualifications, unit.CharacterFlags);
            Assert.AreSame(startingExperience, unit.CharacterStartingExperience);
        }

        private static IEntityType RequireEntity(IModel model, string name)
        {
            var entity = model.FindEntityType(name);
            Assert.IsNotNull(entity, name);
            return entity;
        }

        private static void AssertEnumConversion<TEnum>(
            IEntityType entity,
            string propertyName,
            TEnum value,
            byte expected)
            where TEnum : struct
        {
            var converter = entity.FindProperty(propertyName)?.GetTypeMapping().Converter;
            Assert.IsNotNull(converter, propertyName);
            Assert.AreEqual(expected, converter!.ConvertToProvider(value));
        }

        private static string NormalizeSql(string sql)
        {
            sql = sql.ToLowerInvariant()
                .Replace("`", string.Empty)
                .Replace("\"", string.Empty)
                .Replace("[", string.Empty)
                .Replace("]", string.Empty)
                .Replace("\r", " ")
                .Replace("\n", " ")
                .Replace("\t", " ");
            return Regex.Replace(sql, "\\s+", " ").Trim();
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
                using var context = (SqliteCharContext)CreateContext(
                    typeof(SqliteCharContext),
                    database);
                action(context, database);
            }
            finally
            {
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                Directory.Delete(path, true);
            }
        }

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

        private static CharUnitOfWork CreateUnit(
            SqliteCharContext context,
            AuctionRepository auctions = null,
            ClanInventoryRepository clanInventories = null,
            ClanLockboxLogRepository clanLockboxLogs = null,
            ItemRepository items = null,
            CharacterMissionDeadlineRepository deadlines = null,
            CharacterMissionProgressRepository progress = null,
            CharacterMissionScenarioRepository scenario = null,
            CharacterFlagRepository qualifications = null,
            CharacterStartingExperienceRepository startingExperience = null)
        {
            items ??= new ItemRepository(context);
            deadlines ??= new CharacterMissionDeadlineRepository(context);
            progress ??= new CharacterMissionProgressRepository(context);
            scenario ??= new CharacterMissionScenarioRepository(context);
            qualifications ??= new CharacterFlagRepository(context);
            startingExperience ??= new CharacterStartingExperienceRepository(context);
            return new CharUnitOfWork(
                context,
                gameAccounts: null,
                censoredWords: null,
                characters: new CharacterRepository(context),
                characterAbilityDrawers: new CharacterAbilityDrawerRepository(context),
                characterAppearances: null,
                characterInventories: new CharacterInventoryRepository(context),
                characterLockboxes: null,
                characterLogoses: null,
                characterMissions: new CharacterMissionRepository(context),
                characterMissionDeadlines: deadlines,
                characterMissionProgress: progress,
                characterMissionScenario: scenario,
                characterOptions: null,
                characterFlags: qualifications,
                characterSkills: null,
                characterStartingExperience: startingExperience,
                characterTeleporters: null,
                characterTitles: null,
                auctions: auctions,
                clans: null,
                clanInventories: clanInventories,
                clanMembers: null,
                clanLockboxLogs: clanLockboxLogs,
                friends: null,
                ignoreds: null,
                items: items,
                petitions: null,
                userOptions: null);
        }

        internal static RasaDbContextBase CreateContext(Type contextType, string database)
        {
            var connection = new DatabaseConnectionConfiguration { Database = database };
            var isSqlite = contextType.Name.StartsWith("Sqlite", StringComparison.Ordinal);
            var options = Options.Create(new DatabaseConfiguration
            {
                Provider = isSqlite ? "Sqlite" : "MySql",
                Auth = connection,
                Char = connection,
                World = connection
            });
            IDbContextConfigurationService configuration = isSqlite
                ? new SqliteDbContextConfigurationService(new SqliteConnectionStringFactory())
                : new OfflineMySqlConfigurationService();
            IDbContextPropertyModifier modifier = isSqlite
                ? new SqliteDbContextPropertyModifier()
                : new MySqlDbContextPropertyModifier();
            return (RasaDbContextBase)Activator.CreateInstance(
                contextType,
                options,
                configuration,
                modifier);
        }

        private sealed class OfflineMySqlConfigurationService : IDbContextConfigurationService
        {
            public void Configure(
                DbContextOptionsBuilder builder,
                DatabaseConnectionConfiguration configuration)
            {
                builder.UseMySql(
                    "Server=127.0.0.1;Database=unused",
                    new MySqlServerVersion(new Version(8, 4, 0)));
            }
        }

        private sealed class TestItemChange : IItemChange
        {
            public uint Id { get; set; }
            public uint ItemTemplateId { get; set; }
            public uint StackSize { get; set; }
            public int CurrentHitPoints { get; set; }
            public uint Color { get; set; }
            public uint CurrentAmmo { get; set; }
            public string Crafter { get; set; } = string.Empty;
        }
    }
}
