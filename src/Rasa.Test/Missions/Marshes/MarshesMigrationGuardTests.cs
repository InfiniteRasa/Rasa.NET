using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Services.Preloader.Missions.Marshes;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions.Marshes
{
    /// <summary>
    /// The Marshes World migrations' Up and Down are safe to run twice (after #205, whose Down broke once another
    /// migration sorted before it): each insert is guarded on its table's key (MarshesSql.Guard), the updates and deletes
    /// are the same twice, and each Down takes the database back to what it was before the Up.
    /// </summary>
    [TestClass]
    public class MarshesMigrationGuardTests
    {
        /// <summary>
        /// MarshesSql.Keys is each table's primary key in WorldContext's model; the keyless creature_appearance has its
        /// index's columns (id, slot_id).
        /// </summary>
        [TestMethod]
        public void TheGuardKeysAreTheTablesKeys()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var model = harness.World.Model;
            foreach (var (table, keys) in MarshesSql.Keys)
            {
                var entity = model.GetEntityTypes().SingleOrDefault(type => type.GetTableName() == table);
                Assert.IsNotNull(entity, $"{table} is a table of the model");
                var store = StoreObjectIdentifier.Table(table, entity.GetSchema());
                var key = entity.FindPrimaryKey();
                if (key != null)
                    CollectionAssert.AreEqual(key.Properties.Select(property => property.GetColumnName(store)).ToArray(), keys, $"{table}: the primary key");
                else
                    Assert.IsTrue(entity.GetIndexes().Any(index => index.Properties.Select(property => property.GetColumnName(store)).SequenceEqual(keys)),
                        $"{table} has no primary key: an index on {string.Join(", ", keys)}");
            }
        }

        /// <summary>One row VALUES (upstream's FROM (SELECT 1 AS one) form) and INSERT ... SELECT (a CTE) both come back guarded on the key; an update comes back as it is.</summary>
        [TestMethod]
        public void TheGuardWrapsEachInsertOnItsKey()
        {
            Assert.AreEqual(
                "INSERT INTO creature_stat (id, body, mind, spirit, health, armor) SELECT 760300, 15, 15, 15, 4500, 300 FROM (SELECT 1 AS one) d " +
                "WHERE NOT EXISTS (SELECT 1 FROM creature_stat g WHERE g.id = 760300);",
                MarshesSql.Guard("INSERT INTO creature_stat (id, body, mind, spirit, health, armor) VALUES (760300, 15, 15, 15, 4500, 300);"));
            Assert.AreEqual(
                "INSERT INTO creature_appearance (id, slot_id, Class_id, color) WITH s (id, slot_id, Class_id, color) AS (SELECT 760301, slot_id, Class_id, color FROM creature_appearance WHERE id = 5) " +
                "SELECT id, slot_id, Class_id, color FROM s WHERE NOT EXISTS (SELECT 1 FROM creature_appearance g WHERE g.id = s.id AND g.slot_id = s.slot_id);",
                MarshesSql.Guard("INSERT INTO creature_appearance (id, slot_id, Class_id, color) SELECT 760301, slot_id, Class_id, color FROM creature_appearance WHERE id = 5;"));
            Assert.AreEqual("INSERT INTO mission_evidence (mission_id, content_revision, evidence_id, reconstruction_note) " +
                "SELECT 541, 'marshes_1_16_5', 2, 'a (b), ''c''' FROM (SELECT 1 AS one) d " +
                "WHERE NOT EXISTS (SELECT 1 FROM mission_evidence g WHERE g.mission_id = 541 AND g.content_revision = 'marshes_1_16_5' AND g.evidence_id = 2);",
                MarshesSql.Guard("INSERT INTO mission_evidence (mission_id, content_revision, evidence_id, reconstruction_note) VALUES (541, 'marshes_1_16_5', 2, 'a (b), ''c''');"));
            const string update = "UPDATE spawnpool SET pos_x = 1 WHERE id = 2;";
            Assert.AreEqual(update, MarshesSql.Guard(update));
            Assert.ThrowsExactly<System.ArgumentException>(() => MarshesSql.Guard("INSERT INTO npc_package (id, package_id, comment) VALUES (1, 2, 'a'), (3, 4, 'b');"));
            Assert.ThrowsExactly<System.ArgumentException>(() => MarshesSql.Guard("INSERT INTO no_such_table (id) VALUES (1);"));
            Assert.ThrowsExactly<System.ArgumentException>(() => MarshesSql.Guard("INSERT INTO npc_squad_member (type_id, creature_id) VALUES (1, 2);"));
        }

        /// <summary>
        /// For each Marshes World migration of this batch (20271104..., not the earlier upstream Marshes_region_sweep and
        /// Marshes_logos_element), in order: Up, Up again (its operations run once more) and the tables it writes
        /// are the same; Down (to the migration before it, IMigrationsAssembly) and those tables are as before the Up; Down
        /// again and they still are; Up, and they are as after the first Up. Its operations are all plain SQL.
        /// </summary>
        [TestMethod]
        public void EachMarshesMigrationRunsUpTwiceAndDownTwice()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            var assembly = world.GetService<IMigrationsAssembly>();
            var migrator = world.GetService<IMigrator>();
            var ids = assembly.Migrations.Keys.OrderBy(id => id, System.StringComparer.Ordinal).ToList();
            var marshes = ids.Where(id => id.StartsWith("20271104", System.StringComparison.Ordinal) && id.Contains("_Marshes_")).ToList();
            Assert.IsGreaterThanOrEqualTo(30, marshes.Count, "the Marshes World migrations");

            migrator.Migrate(ids[ids.IndexOf(marshes[0]) - 1]);
            foreach (var id in marshes)
            {
                var migration = assembly.CreateMigration(assembly.Migrations[id], world.Database.ProviderName);
                var operations = migration.UpOperations.Concat(migration.DownOperations).ToList();
                Assert.IsTrue(operations.All(operation => operation is SqlOperation), $"{id}: plain SQL only");
                var tables = operations.Cast<SqlOperation>()
                    .SelectMany(operation => Regex.Matches(operation.Sql, @"\b(?:INSERT\s+INTO|UPDATE|DELETE\s+FROM)\s+(\w+)", RegexOptions.IgnoreCase))
                    .Select(match => match.Groups[1].Value).Distinct().OrderBy(table => table, System.StringComparer.Ordinal).ToList();
                Assert.IsNotEmpty(tables, $"{id} writes rows");

                var before = Dump(world, tables);
                migrator.Migrate(id);
                var after = Dump(world, tables);
                Run(world, migration.UpOperations);
                AssertSame(after, Dump(world, tables), $"{id}: Up twice");
                migrator.Migrate(ids[ids.IndexOf(id) - 1]);
                AssertSame(before, Dump(world, tables), $"{id}: Down after Up");
                Run(world, migration.DownOperations);
                AssertSame(before, Dump(world, tables), $"{id}: Down twice");
                migrator.Migrate(id);
                AssertSame(after, Dump(world, tables), $"{id}: Up after Down");
            }
        }

        private static void Run(DbContext world, IReadOnlyList<MigrationOperation> operations)
        {
            var connection = world.GetService<IRelationalConnection>();
            foreach (var command in world.GetService<IMigrationsSqlGenerator>().Generate(operations))
                command.ExecuteNonQuery(connection);
        }

        /// <summary>Every row of each table, each value with its type, sorted: the table's content in any row order.</summary>
        private static Dictionary<string, string[]> Dump(DbContext world, IEnumerable<string> tables)
        {
            var connection = world.Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open)
                connection.Open();
            var dump = new Dictionary<string, string[]>();
            foreach (var table in tables)
            {
                using var command = connection.CreateCommand();
                command.CommandText = $"SELECT * FROM {table}";
                using var reader = command.ExecuteReader();
                var rows = new List<string>();
                while (reader.Read())
                    rows.Add(string.Join("|", Enumerable.Range(0, reader.FieldCount).Select(i => Value(reader, i))));
                rows.Sort(System.StringComparer.Ordinal);
                dump[table] = rows.ToArray();
            }
            return dump;
        }

        private static string Value(DbDataReader reader, int i)
        {
            if (reader.IsDBNull(i))
                return "NULL";
            var value = reader.GetValue(i);
            return value switch
            {
                double number => "R:" + number.ToString("R", CultureInfo.InvariantCulture),
                byte[] bytes => "B:" + System.Convert.ToHexString(bytes),
                string text => "T:" + text,
                _ => value.GetType().Name + ":" + System.Convert.ToString(value, CultureInfo.InvariantCulture)
            };
        }

        private static void AssertSame(Dictionary<string, string[]> expected, Dictionary<string, string[]> actual, string step)
        {
            foreach (var (table, rows) in expected)
            {
                var now = actual[table];
                var gone = rows.Except(now).Take(3).ToArray();
                var added = now.Except(rows).Take(3).ToArray();
                Assert.IsTrue(rows.SequenceEqual(now),
                    $"{step}: {table} has {now.Length} rows, {rows.Length} expected; missing {string.Join(" ; ", gone)}; extra {string.Join(" ; ", added)}");
            }
        }
    }
}
