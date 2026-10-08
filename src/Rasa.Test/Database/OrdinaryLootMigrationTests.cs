using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Context.World;
using Rasa.Repositories.World;
using Rasa.Structures.World;

namespace Rasa.Test.Database
{
    [TestClass]
    public class OrdinaryLootMigrationTests
    {
        internal const string PreviousMigration = FlatLootArchiveMigrationTests.ArchiveMigration;
        internal const string SchemaMigration = "20261202000100_Add_ordinary_loot_tables";
        private static readonly string[] Tables =
        {
            "loot_profile", "ordinary_loot_group", "loot_item", "creature_loot_binding", "loot_map_policy"
        };

        [TestMethod]
        [DataRow(typeof(SqliteWorldContext))]
        [DataRow(typeof(MySqlWorldContext))]
        public void ModelDefinesOrdinaryLootKeysRestrictedForeignKeysAndPortableChecks(Type contextType)
        {
            using var context = PersistenceIntegrationTests.CreateContext(contextType, "unused");
            var model = context.GetService<IDesignTimeModel>().Model;
            var profile = RequireEntity(model, "LootProfileEntry", "loot_profile", "ProfileKey");
            var group = RequireEntity(model, "OrdinaryLootGroupEntry", "ordinary_loot_group", "ProfileKey", "GroupKey");
            var item = RequireEntity(model, "LootItemEntry", "loot_item",
                "ProfileKey", "GroupKey", "ItemTemplateId");
            var binding = RequireEntity(model, "CreatureLootBindingEntry", "creature_loot_binding",
                "MapContextId", "CreatureId");
            var policy = RequireEntity(model, "LootMapPolicyEntry", "loot_map_policy", "MapContextId");

            AssertColumns(profile, "profile_key", "description", "content_revision", "provenance", "evidence_notes");
            AssertColumns(group, "profile_key", "group_key", "kind", "chance_basis_points",
                "minimum_rolls", "maximum_rolls", "sort_order");
            AssertColumns(item, "profile_key", "group_key", "item_template_id", "weight",
                "minimum_quantity", "maximum_quantity", "sort_order");
            AssertColumns(binding, "map_context_id", "creature_id", "profile_key");
            AssertColumns(policy, "map_context_id", "require_profiles");

            foreach (var entity in new[] { profile, group, item, binding })
                Assert.AreEqual(64, entity.FindProperty("ProfileKey")!.GetMaxLength());
            foreach (var entity in new[] { group, item })
                Assert.AreEqual(32, entity.FindProperty("GroupKey")!.GetMaxLength());
            foreach (var entity in new[] { profile, group, item, binding, policy })
                Assert.IsTrue(entity.FindPrimaryKey()!.Properties.All(property =>
                    property.ValueGenerated == ValueGenerated.Never), entity.Name);

            AssertForeignKeys(group, ("loot_profile", new[] { "ProfileKey" }));
            AssertForeignKeys(item,
                ("ordinary_loot_group", new[] { "ProfileKey", "GroupKey" }),
                ("itemtemplate", new[] { "ItemTemplateId" }));
            AssertForeignKeys(binding,
                ("loot_profile", new[] { "ProfileKey" }),
                ("map_info", new[] { "MapContextId" }),
                ("creature", new[] { "CreatureId" }));
            AssertForeignKeys(policy, ("map_info", new[] { "MapContextId" }));

            AssertChecks(profile, "LENGTH(profile_key) BETWEEN 1 AND 64");
            AssertChecks(group, "LENGTH(group_key) BETWEEN 1 AND 32",
                "kind IN (1, 2, 3, 4)",
                "chance_basis_points BETWEEN 0 AND 10000",
                "minimum_rolls >= 0 AND maximum_rolls >= minimum_rolls");
            AssertChecks(item, "weight > 0",
                "minimum_quantity > 0 AND maximum_quantity >= minimum_quantity");
            var kind = group.FindProperty("Kind")!.ClrType;
            Assert.IsTrue(kind.IsEnum);
            CollectionAssert.AreEqual(new[] { "Junk", "Ammunition", "Consumable", "Equipment" }, Enum.GetNames(kind));
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, Enum.GetValues(kind).Cast<object>()
                .Select(Convert.ToInt32).ToArray());
        }

        [TestMethod]
        public void PairedMigrationsOnlyAddFiveTablesWithMatchingLogicalOperationsAndSafeRollbackOrder()
        {
            using var sqlite = PersistenceIntegrationTests.CreateContext(typeof(SqliteWorldContext), "unused");
            using var mysql = PersistenceIntegrationTests.CreateContext(typeof(MySqlWorldContext), "unused");
            var left = RequireSchemaMigration(sqlite);
            var right = RequireSchemaMigration(mysql);
            foreach (var migration in new[] { left, right })
            {
                Assert.IsTrue(migration.UpOperations.All(operation =>
                    operation is CreateTableOperation or CreateIndexOperation));
                var creates = migration.UpOperations.OfType<CreateTableOperation>().ToArray();
                CollectionAssert.AreEquivalent(Tables, creates.Select(table => table.Name).ToArray());
                var created = new HashSet<string>();
                foreach (var table in creates)
                {
                    Assert.IsTrue(table.ForeignKeys.All(key => !Tables.Contains(key.PrincipalTable) ||
                        created.Contains(key.PrincipalTable)), table.Name);
                    created.Add(table.Name);
                }
                Assert.IsTrue(migration.DownOperations.All(operation => operation is DropTableOperation));
                CollectionAssert.AreEquivalent(Tables, migration.DownOperations.Cast<DropTableOperation>()
                    .Select(operation => operation.Name).ToArray());
                var dropped = new HashSet<string>();
                foreach (var table in migration.DownOperations.Cast<DropTableOperation>())
                {
                    Assert.IsTrue(creates.Where(child => child.ForeignKeys.Any(key =>
                        key.PrincipalTable == table.Name)).All(child => dropped.Contains(child.Name)), table.Name);
                    dropped.Add(table.Name);
                }
            }
            CollectionAssert.AreEqual(DescribeOperations(left), DescribeOperations(right));
            Assert.IsFalse(sqlite.Database.HasPendingModelChanges());
            Assert.IsFalse(mysql.Database.HasPendingModelChanges());
        }

        [TestMethod]
        public void MySqlSchemaSqlGeneratesOfflineWithConstraintsAndNoBalanceData()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(MySqlWorldContext), "unused");
            RequireSchemaMigration(context);
            var sql = context.GetService<IMigrator>().GenerateScript(PreviousMigration, SchemaMigration);
            foreach (var table in Tables)
                StringAssert.Contains(sql, $"CREATE TABLE `{table}`");
            StringAssert.Contains(sql, "CHECK (chance_basis_points BETWEEN 0 AND 10000)");
            StringAssert.Contains(sql, "CHECK (weight > 0)");
            StringAssert.Contains(sql, "ON DELETE RESTRICT");
            StringAssert.Contains(sql, "varchar(64)");
            StringAssert.Contains(sql, "varchar(32)");
            Assert.IsFalse(sql.Contains("AUTO_INCREMENT", StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(sql.Contains("INSERT INTO `loot_", StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(sql.Contains("UPDATE `", StringComparison.OrdinalIgnoreCase));
            Assert.AreEqual(System.Data.ConnectionState.Closed, context.Database.GetDbConnection().State);
            var rollback = context.GetService<IMigrator>().GenerateScript(SchemaMigration, PreviousMigration);
            foreach (var table in Tables)
                StringAssert.Contains(rollback, $"DROP TABLE `{table}`");
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void FreshAndUpgradedSqliteInstallEmptyLootSchemaAndRollbackWithoutChangingNativeContent(bool upgrade)
        {
            using var database = new OrdinaryLootTestDatabase();
            var context = database.Context;
            var migrator = context.GetService<IMigrator>();
            Dictionary<string, long> baseline = null;
            if (upgrade)
            {
                migrator.Migrate(PreviousMigration);
                Assert.AreEqual(PreviousMigration, context.Database.GetAppliedMigrations().Last());
                baseline = NativeCounts(context);
                Assert.AreEqual(0, ExistingLootTables(context).Length);
            }
            migrator.Migrate(SchemaMigration);
            Assert.AreEqual(SchemaMigration, context.Database.GetAppliedMigrations().Last());
            Assert.IsFalse(context.Database.GetPendingMigrations().Any(id => string.CompareOrdinal(id, SchemaMigration) <= 0));
            CollectionAssert.AreEquivalent(Tables, ExistingLootTables(context));
            var snapshot = new OrdinaryLootRepository(context).Get();
            Assert.AreEqual(0, snapshot.Profiles.Count);
            Assert.AreEqual(0, snapshot.Groups.Count);
            Assert.AreEqual(0, snapshot.Items.Count);
            Assert.AreEqual(0, snapshot.Bindings.Count);
            Assert.AreEqual(0, snapshot.MapPolicies.Count);
            var native = NativeCounts(context);
            if (baseline != null)
                CollectionAssert.AreEquivalent(baseline.ToArray(), native.ToArray());
            Assert.IsTrue(native.Values.All(count => count > 0));
            Assert.AreEqual(1L, context.Database.SqlQueryRaw<long>(
                "SELECT COUNT(*) AS Value FROM pragma_table_info('spawnpool') WHERE name = 'radius'").Single());
            Assert.AreEqual(0L, context.Database.SqlQueryRaw<long>("SELECT COUNT(*) AS Value FROM pragma_foreign_key_check").Single());

            context.LootProfileEntries.Add(new LootProfileEntry { ProfileKey = "test" });
            context.OrdinaryLootGroupEntries.Add(new OrdinaryLootGroupEntry
            {
                ProfileKey = "test", GroupKey = "ammo", Kind = LootGroupKind.Ammunition,
                ChanceBasisPoints = 10000, MinimumRolls = 1, MaximumRolls = 1
            });
            context.LootItemEntries.Add(new LootItemEntry
            {
                ProfileKey = "test", GroupKey = "ammo", ItemTemplateId = 28,
                Weight = 1, MinimumQuantity = 1, MaximumQuantity = 1
            });
            context.CreatureLootBindingEntries.Add(new CreatureLootBindingEntry
            {
                MapContextId = 1220, CreatureId = context.CreatureEntries.Select(entry => entry.Id).First(),
                ProfileKey = "test"
            });
            context.LootMapPolicyEntries.Add(new LootMapPolicyEntry { MapContextId = 1220, RequireProfiles = true });
            context.SaveChanges();
            context.ChangeTracker.Clear();

            migrator.Migrate(PreviousMigration);
            Assert.AreEqual(0, ExistingLootTables(context).Length);
            CollectionAssert.AreEquivalent(native.ToArray(), NativeCounts(context).ToArray());
            migrator.Migrate(SchemaMigration);
            Assert.AreEqual(0, new OrdinaryLootRepository(context).Get().Profiles.Count);
        }

        [TestMethod]
        [DataRow("INSERT INTO loot_profile VALUES ('', '', '', '', '')", 275)]
        [DataRow("INSERT INTO loot_profile VALUES ('aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa', '', '', '', '')", 275)]
        [DataRow("INSERT INTO ordinary_loot_group VALUES ('alpha', '', 1, 0, 0, 0, 0)", 275)]
        [DataRow("INSERT INTO ordinary_loot_group VALUES ('alpha', 'aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa', 1, 0, 0, 0, 0)", 275)]
        [DataRow("UPDATE ordinary_loot_group SET chance_basis_points = -1", 275)]
        [DataRow("UPDATE ordinary_loot_group SET kind = 0", 275)]
        [DataRow("UPDATE ordinary_loot_group SET kind = 5", 275)]
        [DataRow("UPDATE ordinary_loot_group SET kind = 257", 275)]
        [DataRow("UPDATE ordinary_loot_group SET chance_basis_points = 10001", 275)]
        [DataRow("UPDATE ordinary_loot_group SET minimum_rolls = -1", 275)]
        [DataRow("UPDATE ordinary_loot_group SET minimum_rolls = 2, maximum_rolls = 1", 275)]
        [DataRow("UPDATE loot_item SET weight = 0", 275)]
        [DataRow("UPDATE loot_item SET weight = -1", 275)]
        [DataRow("UPDATE loot_item SET minimum_quantity = 0", 275)]
        [DataRow("UPDATE loot_item SET minimum_quantity = -1", 275)]
        [DataRow("UPDATE loot_item SET minimum_quantity = 2, maximum_quantity = 1", 275)]
        [DataRow("INSERT INTO ordinary_loot_group VALUES ('absent', 'ammo', 2, 0, 0, 0, 0)", 787)]
        [DataRow("INSERT INTO loot_item VALUES ('absent', 'ammo', 1, 1, 1, 1, 0)", 787)]
        [DataRow("INSERT INTO loot_item VALUES ('alpha', 'absent', 1, 1, 1, 1, 0)", 787)]
        [DataRow("INSERT INTO loot_item VALUES ('zeta', 'ammo', 1, 1, 1, 1, 0)", 787)]
        [DataRow("INSERT INTO loot_item VALUES ('alpha', 'ammo', 4294967295, 1, 1, 1, 0)", 787)]
        [DataRow("INSERT INTO creature_loot_binding VALUES (10, 3, 'alpha')", 787)]
        [DataRow("INSERT INTO creature_loot_binding VALUES (30, 1, 'alpha')", 787)]
        [DataRow("INSERT INTO creature_loot_binding VALUES (20, 2, 'absent')", 787)]
        [DataRow("INSERT INTO loot_map_policy VALUES (30, 1)", 787)]
        [DataRow("INSERT INTO loot_profile SELECT * FROM loot_profile WHERE profile_key = 'alpha'", 1555)]
        [DataRow("INSERT INTO ordinary_loot_group SELECT * FROM ordinary_loot_group WHERE profile_key = 'alpha' AND group_key = 'ammo'", 1555)]
        [DataRow("INSERT INTO loot_item SELECT * FROM loot_item WHERE item_template_id = 1", 1555)]
        [DataRow("INSERT INTO creature_loot_binding SELECT * FROM creature_loot_binding WHERE map_context_id = 20", 1555)]
        [DataRow("INSERT INTO loot_map_policy SELECT * FROM loot_map_policy WHERE map_context_id = 10", 1555)]
        [DataRow("DELETE FROM loot_profile WHERE profile_key = 'alpha'", 1811)]
        [DataRow("DELETE FROM ordinary_loot_group WHERE profile_key = 'alpha' AND group_key = 'ammo'", 1811)]
        [DataRow("DELETE FROM itemtemplate WHERE id = 1", 1811)]
        [DataRow("DELETE FROM map_info WHERE map_context_id = 10", 1811)]
        [DataRow("DELETE FROM creature WHERE id = 1", 1811)]
        public void SqliteEnforcesChecksForeignKeysCompositeKeysAndRestrictedDeletes(string sql, int extendedErrorCode)
        {
            using var database = new OrdinaryLootTestDatabase();
            database.Context.Database.EnsureCreated();
            OrdinaryLootRepositoryTests.Seed(database.Context);
            var error = Assert.ThrowsExactly<SqliteException>(() => database.Context.Database.ExecuteSqlRaw(sql));
            Assert.AreEqual(extendedErrorCode, error.SqliteExtendedErrorCode);
        }

        [TestMethod]
        public void SqliteAcceptsBoundaryKeysChancesRollsAndQuantities()
        {
            using var database = new OrdinaryLootTestDatabase();
            var context = database.Context;
            context.Database.EnsureCreated();
            OrdinaryLootRepositoryTests.Seed(context);
            var profile = new string('a', 64);
            var group = new string('b', 32);
            context.LootProfileEntries.Add(new LootProfileEntry { ProfileKey = profile });
            context.OrdinaryLootGroupEntries.Add(new OrdinaryLootGroupEntry
            {
                ProfileKey = profile, GroupKey = group, Kind = LootGroupKind.Junk,
                ChanceBasisPoints = 10000, MinimumRolls = 0, MaximumRolls = 0
            });
            context.LootItemEntries.Add(new LootItemEntry
            {
                ProfileKey = profile, GroupKey = group, ItemTemplateId = 1, Weight = 1,
                MinimumQuantity = 1, MaximumQuantity = 1
            });
            context.SaveChanges();
            context.ChangeTracker.Clear();
            Assert.AreEqual(64, context.LootProfileEntries.Single(entry => entry.ProfileKey == profile).ProfileKey.Length);
            Assert.AreEqual(32, context.OrdinaryLootGroupEntries.Single(entry => entry.ProfileKey == profile).GroupKey.Length);
            context.Database.ExecuteSqlRaw("UPDATE ordinary_loot_group SET chance_basis_points = 0");
            Assert.IsTrue(context.OrdinaryLootGroupEntries.AsNoTracking().All(entry => entry.ChanceBasisPoints == 0));
        }

        private static Dictionary<string, long> NativeCounts(WorldContext context) => new()
        {
            ["map_info"] = context.MapInfoEntries.LongCount(),
            ["creature"] = context.CreatureEntries.LongCount(),
            ["itemtemplate"] = context.ItemTemplateEntries.LongCount()
        };

        private static string[] ExistingLootTables(WorldContext context) => context.Database.SqlQueryRaw<string>(
            "SELECT name AS Value FROM sqlite_master WHERE type = 'table' AND name IN " +
            "('loot_profile', 'ordinary_loot_group', 'loot_item', 'creature_loot_binding', 'loot_map_policy')").ToArray();

        internal static Migration RequireSchemaMigration(DbContext context)
        {
            var assembly = context.GetService<IMigrationsAssembly>();
            Assert.IsTrue(assembly.Migrations.ContainsKey(SchemaMigration),
                $"Missing schema migration {SchemaMigration}.");
            var ids = assembly.Migrations.Keys.ToArray();
            Assert.AreEqual(PreviousMigration, ids[Array.IndexOf(ids, SchemaMigration) - 1]);
            return assembly.CreateMigration(assembly.Migrations[SchemaMigration], context.Database.ProviderName!);
        }

        private static string[] DescribeOperations(Migration migration) => migration.UpOperations
            .Select(operation => operation switch
            {
                CreateTableOperation table => string.Join("|", table.Name,
                    string.Join(",", table.Columns.Select(column => $"{column.Name}:{column.ClrType}:{column.IsNullable}:{column.MaxLength}")),
                    string.Join(",", table.PrimaryKey!.Columns),
                    string.Join(",", table.ForeignKeys.Select(key =>
                        $"{key.Name}:{string.Join("/", key.Columns)}:{key.PrincipalTable}:{string.Join("/", key.PrincipalColumns!)}:{key.OnDelete}")),
                    string.Join(",", table.CheckConstraints.Select(check => $"{check.Name}:{check.Sql}"))),
                CreateIndexOperation index => $"{index.Table}:{index.Name}:{string.Join(",", index.Columns)}:{index.IsUnique}",
                _ => operation.GetType().Name
            }).ToArray();

        private static IEntityType RequireEntity(IModel model, string name, string table, params string[] key)
        {
            var entity = model.FindEntityType("Rasa.Structures.World." + name);
            Assert.IsNotNull(entity, $"Missing ordinary loot entity {name}.");
            Assert.AreEqual(table, entity.GetTableName());
            CollectionAssert.AreEqual(key, entity.FindPrimaryKey()!.Properties.Select(property => property.Name).ToArray());
            return entity;
        }

        private static void AssertColumns(IEntityType entity, params string[] columns) =>
            CollectionAssert.AreEquivalent(columns, entity.GetProperties().Select(property => property.GetColumnName()).ToArray());

        private static void AssertForeignKeys(IEntityType entity, params (string Table, string[] Properties)[] expected)
        {
            var keys = entity.GetForeignKeys().ToArray();
            Assert.AreEqual(expected.Length, keys.Length);
            foreach (var (table, properties) in expected)
            {
                var key = keys.Single(foreignKey => foreignKey.PrincipalEntityType.GetTableName() == table);
                Assert.AreEqual(DeleteBehavior.Restrict, key.DeleteBehavior);
                CollectionAssert.AreEqual(properties, key.Properties.Select(property => property.Name).ToArray());
                CollectionAssert.AreEqual(key.PrincipalEntityType.FindPrimaryKey()!.Properties
                    .Select(property => property.Name).ToArray(), key.PrincipalKey.Properties
                    .Select(property => property.Name).ToArray());
            }
        }

        private static void AssertChecks(IEntityType entity, params string[] checks)
        {
            var actual = entity.GetCheckConstraints().Select(check => check.Sql).ToArray();
            foreach (var check in checks)
                CollectionAssert.Contains(actual, check);
        }
    }
}
