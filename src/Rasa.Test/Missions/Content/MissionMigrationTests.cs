using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.Options;
using Rasa.Configuration;
using Rasa.Configuration.ConnectionStrings;
using Rasa.Configuration.ContextSetup;
using Rasa.Context.World;
using Rasa.Services.DbContext;
using Rasa.Missions.Content;
using Rasa.Missions.Runtime;
using Rasa.Missions.Scenes;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions.Content
{
    using Rasa.Services.Preloader.Missions;
    using Rasa.Structures.World;

    [TestClass]
    [DoNotParallelize]
    public class MissionMigrationTests
    {
        [TestMethod]
        public void ConsolidatedWorldDataMatchesAcrossProvidersAndKeepsTheNativeExtractionObjectiveIds()
        {
            var sqlite = new Rasa.Migrations.SqliteWorld.SeedWorldContent().UpOperations;
            var mysql = new Rasa.Migrations.MySqlWorld.SeedWorldContent().UpOperations;
            CollectionAssert.AreEqual(sqlite.OfType<SqlOperation>().Select(operation => operation.Sql).ToArray(),
                mysql.OfType<SqlOperation>().Select(operation => operation.Sql).ToArray());
            CollectionAssert.AreEqual(sqlite.OfType<UpdateDataOperation>().SelectMany(operation =>
                    operation.Values.Cast<object>().Select(value => value?.ToString())).ToArray(),
                mysql.OfType<UpdateDataOperation>().SelectMany(operation =>
                    operation.Values.Cast<object>().Select(value => value?.ToString())).ToArray());
            foreach (var mission in new[] { 1995U, 2005U })
            {
                var scene = BootcampExtractionDataV5.Scene(mission);
                Assert.AreEqual(6, scene.DefeatSequences.Count);
                Assert.IsTrue(scene.DefeatSequences.Keys.All(role =>
                    scene.Actors[role].TemplateId == BootcampExtractionDataV5.AssaultTemplate));
                Assert.IsTrue(scene.Sequences.Values.SelectMany(sequence => sequence.Character)
                    .OfType<ObjectiveIntent>().All(intent => intent.ObjectiveId is 1 or 4));
            }
        }

        [TestMethod]
        public void InvalidActorDefeatSequenceIsRejectedAtContentLoad()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            MissionContentTestSupport.ConfigureScenes(harness.WorldContext, scenes =>
                scenes[1995].DefeatSequences["missing-actor"] = 9);
            var error = Assert.ThrowsExactly<MissionRuleException>(() => harness.Manager.LoadMissions());
            StringAssert.Contains(error.Message, "actor defeat sequence");
        }

        [TestMethod]
        public void SeedWorldBaselineEnablesExactlyTheProtectedBootcampDefinitions()
        {
            using var harness = global::Rasa.Test.Missions.Wilderness.WildernessRuntimeTestHarness.Create(
                targetWorldMigration: "20260926190153_SeedWorldContent");
            CollectionAssert.AreEquivalent(new uint[] { 1990, 1992, 1994, 1995, 2005 },
                harness.World.MissionContentDefinitionEntries.Where(entry => entry.Enabled)
                    .Select(entry => entry.MissionId).ToArray());
            Assert.IsTrue(harness.Manager.Scenes.OwnsExperience(1985));
        }

        [TestMethod]
        public void FreshSqliteInitializationInstallsRunnableBootcampWithoutPublishing()
        {
            using var harness = BootcampRuntimeTestHarness.Create(useWorldContent: true);
            var expected = WildernessMissionCases.ProtectedBootcampMissionIds.Concat(
                WildernessMissionCases.All.Where(entry => entry.Disposition == WildernessDisposition.OutdoorRelease)
                    .Select(entry => entry.MissionId)).ToArray();
            CollectionAssert.AreEquivalent(expected,
                harness.WorldContext.MissionContentDefinitionEntries.Where(entry => entry.Enabled)
                    .Select(entry => entry.MissionId).ToArray());
            Assert.IsTrue(expected.All(id => harness.Manager.LoadedMissions[id].IsOperational));
            CollectionAssert.AreEquivalent(new uint[] { 1990, 1992, 1994, 1995, 2005 },
                harness.WorldContext.MissionContentDefinitionEntries.Where(entry => entry.Enabled &&
                    entry.ContentRevision == "deployment_11")
                    .Select(entry => entry.MissionId).ToArray());
            Assert.AreEqual(5, harness.WorldContext.Set<MissionSceneBindingEntry>()
                .Count(entry => entry.ContentRevision == "deployment_11"));
            Assert.AreEqual(1, harness.WorldContext.Set<MissionExperienceBindingEntry>().Count(entry => entry.Enabled));
            harness.WorldContext.Database.OpenConnection();
            using (var command = harness.WorldContext.Database.GetDbConnection().CreateCommand())
            {
                command.CommandText =
                    "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name IN ('mission_active_release', 'mission_release_member')";
                Assert.AreEqual(0L, Convert.ToInt64(command.ExecuteScalar()));
            }
            harness.WorldContext.Database.CloseConnection();
            Assert.IsFalse(harness.Manager.LoadMissions().BlocksReadiness);
            Assert.IsTrue(harness.Manager.Scenes.OwnsExperience(1985));
            var before = harness.WorldContext.Database.GetAppliedMigrations().ToArray();

            harness.WorldContext.Initialize();

            CollectionAssert.AreEqual(before, harness.WorldContext.Database.GetAppliedMigrations().ToArray());
            Assert.IsFalse(harness.WorldContext.Database.GetPendingMigrations().Any());
            var conrad = harness.WorldContext.MissionIndicatorEntries.Single(entry =>
                entry.MissionId == 1995 && entry.ObjectiveId == 3 && entry.IndicatorId == 436);
            Assert.AreEqual(-99, conrad.PosX);
            Assert.AreEqual(74, conrad.PosZ);
            Assert.IsTrue(harness.WorldContext.Set<ItemTemplateWeaponEntry>().Any(entry => entry.Id == 17131));
        }

        [TestMethod]
        [DataRow(typeof(Rasa.Migrations.SqliteWorld.SeedWorldContent), typeof(Rasa.Migrations.MySqlWorld.SeedWorldContent))]
        [DataRow(typeof(Rasa.Migrations.SqliteWorld.WildernessSupportedRewards), typeof(Rasa.Migrations.MySqlWorld.WildernessSupportedRewards))]
        [DataRow(typeof(Rasa.Migrations.SqliteWorld.WildernessTwinPillars), typeof(Rasa.Migrations.MySqlWorld.WildernessTwinPillars))]
        [DataRow(typeof(Rasa.Migrations.SqliteWorld.WildernessRanjaGorge), typeof(Rasa.Migrations.MySqlWorld.WildernessRanjaGorge))]
        [DataRow(typeof(Rasa.Migrations.SqliteWorld.WildernessDaghdasUrn), typeof(Rasa.Migrations.MySqlWorld.WildernessDaghdasUrn))]
        public void PairedWorldDataUsesTheSameOperationsForSqliteAndMySql(Type sqliteType, Type mySqlType)
        {
            var sqlite = (Migration)Activator.CreateInstance(sqliteType);
            var mysql = (Migration)Activator.CreateInstance(mySqlType);
            foreach (var (left, right) in new[]
            {
                (sqlite.UpOperations, mysql.UpOperations),
                (sqlite.DownOperations, mysql.DownOperations)
            })
            {
                Assert.AreEqual(left.Count, right.Count);
                for (var index = 0; index < left.Count; index++)
                {
                    Assert.AreEqual(left[index].GetType(), right[index].GetType());
                    switch (left[index])
                    {
                        case SqlOperation sql:
                            Assert.AreEqual(sql.Sql, ((SqlOperation)right[index]).Sql);
                            break;
                        case InsertDataOperation insert:
                            var otherInsert = (InsertDataOperation)right[index];
                            Assert.AreEqual(insert.Table, otherInsert.Table);
                            CollectionAssert.AreEqual(insert.Columns, otherInsert.Columns);
                            AssertValues(insert.Values, otherInsert.Values);
                            break;
                        case UpdateDataOperation update:
                            var otherUpdate = (UpdateDataOperation)right[index];
                            Assert.AreEqual(update.Table, otherUpdate.Table);
                            CollectionAssert.AreEqual(update.Columns, otherUpdate.Columns);
                            CollectionAssert.AreEqual(update.KeyColumns, otherUpdate.KeyColumns);
                            AssertValues(update.Values, otherUpdate.Values);
                            AssertValues(update.KeyValues, otherUpdate.KeyValues);
                            break;
                        case DeleteDataOperation delete:
                            var otherDelete = (DeleteDataOperation)right[index];
                            Assert.AreEqual(delete.Table, otherDelete.Table);
                            CollectionAssert.AreEqual(delete.KeyColumns, otherDelete.KeyColumns);
                            AssertValues(delete.KeyValues, otherDelete.KeyValues);
                            break;
                        default:
                            Assert.Fail($"Unexpected data migration operation: {left[index].GetType().Name}");
                            break;
                    }
                }
            }
        }

        private static void AssertValues(object[,] expected, object[,] actual)
        {
            Assert.AreEqual(expected.GetLength(0), actual.GetLength(0));
            Assert.AreEqual(expected.GetLength(1), actual.GetLength(1));
            CollectionAssert.AreEqual(expected.Cast<object>().ToArray(), actual.Cast<object>().ToArray());
        }

        [TestMethod]
        public void MySqlEvidenceMigrationValuesFitTheColumnAtTheirInsertionBoundary()
        {
            using var context = Database.PersistenceIntegrationTests.CreateContext(typeof(MySqlWorldContext), "unused");
            var assembly = context.GetService<IMigrationsAssembly>();
            var maximum = int.MaxValue;
            var failures = new List<string>();
            var migrations = assembly.Migrations.OrderBy(entry => entry.Key).Select(entry =>
                (entry.Key, Migration: assembly.CreateMigration(entry.Value, context.Database.ProviderName))).ToArray();
            var steps = migrations.Select(entry => (entry.Key, Direction: "Up", Operations: entry.Migration.UpOperations))
                .Concat(migrations.Reverse().Select(entry =>
                    (entry.Key, Direction: "Down", Operations: entry.Migration.DownOperations)));
            foreach (var step in steps)
                foreach (var operation in step.Operations)
                {
                    ColumnOperation column = operation switch
                    {
                        CreateTableOperation table when table.Name == "mission_evidence" =>
                            table.Columns.Single(entry => entry.Name == "reconstruction_note"),
                        AlterColumnOperation alter when alter.Table == "mission_evidence" &&
                            alter.Name == "reconstruction_note" => alter,
                        _ => null
                    };
                    if (column != null)
                    {
                        var width = Regex.Match(column.ColumnType ?? string.Empty, @"^varchar\((\d+)\)$",
                            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                        var nextMaximum = width.Success ? int.Parse(width.Groups[1].Value) : int.MaxValue;
                        if (step.Direction == "Down" && operation is AlterColumnOperation && nextMaximum < maximum)
                            failures.Add($"{step.Key}: rollback must not reduce capacity for surviving evidence.");
                        maximum = nextMaximum;
                    }
                    if (operation is not InsertDataOperation insert || insert.Table != "mission_evidence")
                        continue;
                    var note = Array.IndexOf(insert.Columns, "reconstruction_note");
                    var mission = Array.IndexOf(insert.Columns, "mission_id");
                    var evidence = Array.IndexOf(insert.Columns, "evidence_id");
                    if (note < 0)
                        continue;
                    for (var row = 0; row < insert.Values.GetLength(0); row++)
                        if (insert.Values[row, note] is string value && value.Length > maximum)
                            failures.Add($"{step.Key}/{step.Direction}: mission{insert.Values[row, mission]} " +
                                $"evidence{insert.Values[row, evidence]} has {value.Length} characters, column limit {maximum}.");
                }
            Assert.IsEmpty(failures, string.Join(Environment.NewLine, failures));
        }

        [TestMethod]
        [DataRow(typeof(Rasa.Migrations.SqliteWorld.WildernessEvidenceCapacity))]
        [DataRow(typeof(Rasa.Migrations.MySqlWorld.WildernessEvidenceCapacity))]
        public void EvidenceCapacityMigrationOnlyWidensNotesAndNeverShrinksOnRollback(Type migrationType)
        {
            var migration = (Migration)Activator.CreateInstance(migrationType);
            var operation = migration.UpOperations.Single();
            Assert.IsInstanceOfType<AlterColumnOperation>(operation);
            var column = (AlterColumnOperation)operation;
            Assert.AreEqual("mission_evidence", column.Table);
            Assert.AreEqual("reconstruction_note", column.Name);
            Assert.AreEqual("varchar(256)", column.OldColumn.ColumnType);
            Assert.AreEqual("text", column.ColumnType);
            Assert.IsFalse(column.IsNullable);
            Assert.IsEmpty(migration.DownOperations, "Existing long evidence cannot be truncated by rollback.");
        }

        [TestMethod]
        public void MigratedMissingScriptIsRejectedBeforeGameplay()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            MissionContentTestSupport.ConfigureScenes(harness.WorldContext, scenes => scenes[1990].Script = "missing.script");
            Assert.ThrowsExactly<MissionRuleException>(() => harness.Manager.LoadMissions());
        }

        [TestMethod]
        public void MissingEnabledDataReportsTheNormalMigrationRequirement()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            foreach (var definition in harness.WorldContext.MissionContentDefinitionEntries.Where(entry => entry.Enabled))
                definition.Enabled = false;
            harness.WorldContext.SaveChanges();
            var error = Assert.ThrowsExactly<InvalidOperationException>(() => harness.Manager.LoadMissions());
            StringAssert.Contains(error.Message, "World data migrations");
        }

        [TestMethod]
        public void MissingMigratedPublicSpawnIsRejectedAtStartup()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            MissionContentTestSupport.ConfigureScenes(harness.WorldContext, scenes =>
                scenes[1990].Actors["missing"] = new SceneActorDefinition("missing", SceneActorKind.PublicSpawn, 999999));
            var error = Assert.ThrowsExactly<MissionRuleException>(() => harness.Manager.LoadMissions());
            StringAssert.Contains(error.Message, "public spawn 999999");
        }

        [TestMethod]
        public void MissingRequiredSceneBindingIsRejectedAtStartup()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var binding = harness.WorldContext.Set<MissionSceneBindingEntry>().Single(entry => entry.MissionId == 1992);
            harness.WorldContext.Remove(binding);
            harness.WorldContext.SaveChanges();
            var error = Assert.ThrowsExactly<MissionRuleException>(() => harness.Manager.LoadMissions());
            StringAssert.Contains(error.Message, "requires a migrated scene script binding");
        }

        [TestMethod]
        public void MySqlStartupDoesNotApplyMigrationsOrOpenAConnection()
        {
            var configured = 0;
            using var context = new MySqlWorldContext(
                Options.Create(new DatabaseConfiguration
                {
                    Provider = "MySql",
                    World = new DatabaseConnectionConfiguration { Database = "not-created-by-startup" }
                }),
                new MySqlDbContextConfigurationService(new MySqlConnectionStringFactory(), _ =>
                {
                    configured++;
                    throw new InvalidOperationException("Startup must not connect to migrate MySQL.");
                }),
                new MySqlDbContextPropertyModifier());

            context.Initialize();

            Assert.AreEqual(0, configured);
        }

        [TestMethod]
        public void MigrationCanUpdateSceneDataWithoutAPublishOrNewRelease()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            MissionContentTestSupport.ConfigureScenes(harness.WorldContext, scenes =>
                scenes[1995].Actors["bootcamp-conrad-corpse"] = scenes[1995].Actors["bootcamp-conrad-corpse"]
                    with { Position = new ScenePosition(-98, 86.4f, 74) });
            Assert.IsFalse(harness.Manager.LoadMissions().BlocksReadiness);
            var row = harness.WorldContext.Set<MissionSceneBindingEntry>().AsNoTracking()
                .Single(entry => entry.MissionId == 1995);
            var scene = JsonSerializer.Deserialize<MissionSceneDefinition>(row.Bindings, MissionContentCodec.Options);
            Assert.AreEqual(-98, scene.Actors["bootcamp-conrad-corpse"].Position.X);
        }

        [TestMethod]
        public void SharedSceneSerializationRejectsUnknownFieldsAndPreservesTypedIntents()
        {
            var scene = BootcampMissionDataV1.Mission1995();
            var decoded = JsonSerializer.Deserialize<MissionSceneDefinition>(
                JsonSerializer.Serialize(scene, MissionContentCodec.Options), MissionContentCodec.Options);
            Assert.IsInstanceOfType<EnsureActorIntent>(decoded.Sequences[1].World[0]);
            Assert.ThrowsExactly<JsonException>(() => JsonSerializer.Deserialize<MissionSceneDefinition>(
                "{\"scrpit\":\"data.sequence\"}", MissionContentCodec.Options));
        }
    }
}
