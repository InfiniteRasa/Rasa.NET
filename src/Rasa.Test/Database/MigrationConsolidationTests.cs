using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Context;
using Rasa.Context.Char;
using Rasa.Context.World;

namespace Rasa.Test.Database
{
    [TestClass]
    [DoNotParallelize]
    public class MigrationConsolidationTests
    {
        [TestMethod]
        [DataRow(typeof(SqliteCharContext))]
        [DataRow(typeof(MySqlCharContext))]
        [DataRow(typeof(SqliteWorldContext))]
        [DataRow(typeof(MySqlWorldContext))]
        public void ConsolidatedSchemasContainOnlySchemaOperations(Type contextType)
        {
            using var context = PersistenceIntegrationTests.CreateContext(contextType, "unused");
            var assembly = context.GetService<IMigrationsAssembly>();
            var schema = assembly.Migrations.Values
                .Select(type => assembly.CreateMigration(type, context.Database.ProviderName))
                .Single(migration => migration.GetType().Name.StartsWith("Consolidated", StringComparison.Ordinal));

            Assert.IsTrue(schema.UpOperations.Count > 0);
            Assert.IsTrue(schema.DownOperations.Count > 0);
            Assert.IsTrue(schema.UpOperations.Concat(schema.DownOperations).All(operation =>
                operation is not SqlOperation and not InsertDataOperation and not UpdateDataOperation and not DeleteDataOperation));
            Assert.IsFalse(schema.UpOperations.OfType<CreateTableOperation>().Any(table =>
                table.Name is "character_qualification" or "mission_active_release" or "mission_release_member"));
        }

        [TestMethod]
        [DataRow(typeof(SqliteCharContext), "20230202081214_edited_character_teleporter")]
        [DataRow(typeof(SqliteWorldContext), "20230119210707_Add_data_to_world")]
        public void SqliteConsolidationRoundTripsEveryRowThroughThePreservedBoundary(
            Type contextType, string baseline)
        {
            var directory = Path.Combine(AppContext.BaseDirectory, "TestDatabases", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                using var context = PersistenceIntegrationTests.CreateContext(contextType, Path.Combine(directory, "database"));
                var migrator = context.GetService<IMigrator>();
                migrator.Migrate(baseline);
                var original = ReadTableHashes(context);

                migrator.Migrate();
                var final = ReadTableHashes(context);
                Assert.IsFalse(context.Database.HasPendingModelChanges());

                if (contextType == typeof(SqliteWorldContext))
                {
                    migrator.Migrate("ConsolidatedWorldSchema");
                    var schemaOnly = ReadTableHashes(context);
                    foreach (var (table, hash) in original)
                        Assert.AreEqual(hash, schemaOnly[table], $"World data rollback changed preserved table {table}.");
                    migrator.Migrate();
                    CollectionAssert.AreEqual(final.ToArray(), ReadTableHashes(context).ToArray());
                }

                migrator.Migrate(baseline);
                CollectionAssert.AreEqual(original.ToArray(), ReadTableHashes(context).ToArray());
                Assert.AreEqual(baseline, context.Database.GetAppliedMigrations().Last());

                migrator.Migrate();
                CollectionAssert.AreEqual(final.ToArray(), ReadTableHashes(context).ToArray());
                Assert.IsFalse(context.Database.GetPendingMigrations().Any());
            }
            finally
            {
                SqliteConnection.ClearAllPools();
                Directory.Delete(directory, true);
            }
        }

        [TestMethod]
        public void SqliteCharacterSchemaPreservesMissionColumnDefaults()
        {
            using var context = new Missions.MissionTestContext();
            using var database = context.Open();
            foreach (var (table, column, value) in new[]
            {
                ("character_mission", "assignment_id", "''"),
                ("character_mission", "content_revision", "''"),
                ("character_mission", "generation", "0"),
                ("character_mission", "version", "0"),
                ("mission_scene", "assignment_id", "''"),
                ("mission_timer", "sequence_id", "0")
            })
                Assert.AreEqual(value, database.Database.SqlQueryRaw<string>(
                    $"SELECT dflt_value AS Value FROM pragma_table_info('{table}') WHERE name = '{column}'").Single(),
                    $"{table}.{column}");
        }

        private static SortedDictionary<string, string> ReadTableHashes(RasaDbContextBase context)
        {
            context.Database.OpenConnection();
            try
            {
                var connection = context.Database.GetDbConnection();
                var tables = new List<string>();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' ORDER BY name";
                    using var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        var name = reader.GetString(0);
                        if (!name.StartsWith("sqlite_", StringComparison.Ordinal) &&
                            !name.StartsWith("__EF", StringComparison.Ordinal))
                            tables.Add(name);
                    }
                }

                var hashes = new SortedDictionary<string, string>(StringComparer.Ordinal);
                foreach (var table in tables)
                {
                    var columns = ReadColumns(connection, table);
                    using var command = connection.CreateCommand();
                    command.CommandText = $"SELECT {string.Join(", ", columns.Select(Quote))} FROM {Quote(table)}";
                    using var reader = command.ExecuteReader();
                    var rows = new List<string>();
                    while (reader.Read())
                        rows.Add(JsonSerializer.Serialize(Enumerable.Range(0, reader.FieldCount)
                            .Select(index => reader.IsDBNull(index) ? null : reader.GetValue(index)).ToArray()));
                    rows.Sort(StringComparer.Ordinal);
                    hashes.Add(table, Convert.ToHexString(SHA256.HashData(
                        Encoding.UTF8.GetBytes(string.Join("\n", rows)))));
                }
                return hashes;
            }
            finally
            {
                context.Database.CloseConnection();
            }
        }

        private static string[] ReadColumns(DbConnection connection, string table)
        {
            using var command = connection.CreateCommand();
            command.CommandText = $"PRAGMA table_info({Quote(table)})";
            using var reader = command.ExecuteReader();
            var columns = new List<string>();
            while (reader.Read())
                columns.Add(reader.GetString(1));
            return columns.OrderBy(name => name, StringComparer.Ordinal).ToArray();
        }

        private static string Quote(string identifier) => "\"" + identifier.Replace("\"", "\"\"") + "\"";
    }
}
