using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Context.World;
using Rasa.Repositories.World;
using Rasa.Structures.World;

namespace Rasa.Test.Database
{
    [TestClass]
    public class FlatLootArchiveMigrationTests
    {
        internal const string IncomingTail = "20261202000000_Place_bootcamp_base_npcs";
        internal const string ArchiveMigration = "20261203000000_Archive_flat_loot_pools";
        private static readonly string[] FlatTables = { "loot_group", "loot_group_item", "creature_loot_group" };

        [TestMethod]
        [DataRow(typeof(SqliteWorldContext))]
        [DataRow(typeof(MySqlWorldContext))]
        public void ForwardTransitionOnlyRenamesNumericConfigurationAndHasCorrectPhasedModels(Type contextType)
        {
            using var context = PersistenceIntegrationTests.CreateContext(contextType, "unused");
            var assembly = context.GetService<IMigrationsAssembly>();
            Assert.IsTrue(assembly.Migrations.ContainsKey(ArchiveMigration), "Missing explicit flat archive migration.");
            var ids = assembly.Migrations.Keys.ToArray();
            Assert.AreEqual(ids.Length, ids.Distinct().Count());
            var archiveIndex = Array.IndexOf(ids, ArchiveMigration);
            Assert.AreEqual(IncomingTail, ids[archiveIndex - 1]);
            Assert.AreEqual(OrdinaryLootMigrationTests.SchemaMigration, ids[archiveIndex + 1]);
            Assert.AreEqual(WildernessOrdinaryLootMigrationTests.ContentMigration, ids[archiveIndex + 2]);
            var archive = assembly.CreateMigration(assembly.Migrations[ArchiveMigration], context.Database.ProviderName);
            Assert.IsTrue(archive.UpOperations.All(operation => operation is RenameTableOperation));
            CollectionAssert.AreEquivalent(FlatTables, archive.UpOperations.Cast<RenameTableOperation>()
                .Select(operation => operation.Name).ToArray());
            foreach (var rename in archive.UpOperations.Cast<RenameTableOperation>())
                Assert.AreEqual("legacy_flat_" + rename.Name, rename.NewName);
            foreach (var rename in archive.DownOperations.Cast<RenameTableOperation>())
                Assert.AreEqual("legacy_flat_" + rename.NewName, rename.Name);
            Assert.IsNull(archive.TargetModel.FindEntityType("Rasa.Structures.World.OrdinaryLootGroupEntry"),
                "An archive-phase designer must not contain the later weighted schema.");
            var numeric = archive.TargetModel.FindEntityType("Rasa.Structures.World.LootGroupEntry");
            Assert.IsNotNull(numeric);
            Assert.AreEqual("legacy_flat_loot_group", numeric.GetTableName());
            CollectionAssert.AreEqual(new[] { "Id" }, numeric.FindPrimaryKey().Properties.Select(p => p.Name).ToArray());
            foreach (var entity in new[] { "ModuleClassEntry", "ModuleItemEntry", "AmbientNpcEntry", "NpcGreetingEntry" })
                Assert.IsNotNull(archive.TargetModel.FindEntityType("Rasa.Structures.World." + entity), entity);
            var before = assembly.CreateMigration(assembly.Migrations["20261130000000_Add_bootcamp_ambient_guards"],
                context.Database.ProviderName).TargetModel;
            Assert.AreEqual("loot_group", before.FindEntityType("Rasa.Structures.World.LootGroupEntry").GetTableName());
            Assert.IsNull(before.FindEntityType("Rasa.Structures.World.OrdinaryLootGroupEntry"));
        }

        [TestMethod]
        public void IncomingTailUpgradePreservesEveryFlatValueAndBlocksImplicitWeightedActivation()
        {
            using var database = new OrdinaryLootTestDatabase();
            var context = database.Context;
            var migrator = context.GetService<IMigrator>();
            migrator.Migrate(IncomingTail);
            context.Database.ExecuteSqlRaw("INSERT INTO loot_group VALUES (17, 'Old pool', 'Keep 5 percent verbatim')");
            context.Database.ExecuteSqlRaw("INSERT INTO loot_group_item VALUES (17, 28, 5.0001, 2, 3)");
            context.Database.ExecuteSqlRaw("INSERT INTO creature_loot_group VALUES (1, 17)");
            migrator.Migrate();
            Assert.AreEqual(1L, Count(context, "legacy_flat_loot_group"));
            Assert.AreEqual("Keep 5 percent verbatim", context.Database.SqlQueryRaw<string>(
                "SELECT comment AS Value FROM legacy_flat_loot_group WHERE id = 17").Single());
            Assert.AreEqual("Old pool", context.Database.SqlQueryRaw<string>(
                "SELECT name AS Value FROM legacy_flat_loot_group WHERE id = 17").Single());
            Assert.AreEqual(5.0001, context.Database.SqlQueryRaw<double>(
                "SELECT chance AS Value FROM legacy_flat_loot_group_item WHERE group_id = 17").Single());
            Assert.AreEqual(2L, context.Database.SqlQueryRaw<long>(
                "SELECT min_quantity AS Value FROM legacy_flat_loot_group_item WHERE group_id = 17").Single());
            Assert.AreEqual(3L, context.Database.SqlQueryRaw<long>(
                "SELECT max_quantity AS Value FROM legacy_flat_loot_group_item WHERE group_id = 17").Single());
            Assert.AreEqual(1L, context.Database.SqlQueryRaw<long>(
                "SELECT creature_id AS Value FROM legacy_flat_creature_loot_group WHERE group_id = 17").Single());
            foreach (var table in FlatTables)
                Assert.AreEqual(0L, context.Database.SqlQueryRaw<long>(
                    "SELECT COUNT(*) AS Value FROM sqlite_master WHERE type = 'table' AND name = {0}", table).Single());
            Assert.AreEqual(83L, Count(context, "ordinary_loot_group"), "No percentage-to-weight conversion.");
            var error = Assert.ThrowsExactly<LegacyFlatLootArchiveException>(() => new OrdinaryLootRepository(context).Get());
            StringAssert.Contains(error.Message, "legacy_flat");
            StringAssert.Contains(error.Message, "author");
            StringAssert.Contains(error.Message, "percent");
            migrator.Migrate(IncomingTail);
            Assert.AreEqual(1L, Count(context, "loot_group"));
            Assert.AreEqual(5.0001, context.Database.SqlQueryRaw<double>(
                "SELECT chance AS Value FROM loot_group_item WHERE group_id = 17").Single());
            Assert.AreEqual(0L, context.Database.SqlQueryRaw<long>(
                "SELECT COUNT(*) AS Value FROM pragma_foreign_key_check").Single());
        }

        [TestMethod]
        [DataRow("legacy_flat_loot_group", "INSERT INTO legacy_flat_loot_group VALUES (17, 'Orphan', '')")]
        [DataRow("legacy_flat_loot_group_item", "INSERT INTO legacy_flat_loot_group_item VALUES (17, 28, 0.125, 1, 1)")]
        [DataRow("legacy_flat_creature_loot_group", "INSERT INTO legacy_flat_creature_loot_group VALUES (1, 17)")]
        public void AnyNonemptyArchiveRequiresAnExplicitAuthoringDecisionIncludingOrphanRows(string table, string sql)
        {
            using var database = new OrdinaryLootTestDatabase();
            database.Context.Database.EnsureCreated();
            database.Context.Database.ExecuteSqlRaw(sql);
            var error = Assert.ThrowsExactly<LegacyFlatLootArchiveException>(() =>
                new OrdinaryLootRepository(database.Context).Get());
            StringAssert.Contains(error.Message, table);
            Assert.AreEqual(1L, Count(database.Context, table), "Diagnostics must not delete configuration.");
        }

        [TestMethod]
        public void MySqlArchiveAndFullUpgradeScriptsGenerateOfflineAndPreserveNativeHistory()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var archive = migrator.GenerateScript(IncomingTail, ArchiveMigration);
            var down = migrator.GenerateScript(ArchiveMigration, IncomingTail);
            foreach (var table in FlatTables)
            {
                StringAssert.Contains(archive, $"ALTER TABLE `{table}` RENAME `legacy_flat_{table}`");
                StringAssert.Contains(down, $"ALTER TABLE `legacy_flat_{table}` RENAME `{table}`");
            }
            foreach (var sql in new[] { archive, down })
                foreach (var table in FlatTables.SelectMany(table => new[] { table, "legacy_flat_" + table }))
                    Assert.IsFalse(sql.Contains($"DELETE FROM `{table}`", StringComparison.OrdinalIgnoreCase));
            var fresh = migrator.GenerateScript();
            StringAssert.Contains(fresh, "CREATE TABLE `loot_group`");
            StringAssert.Contains(fresh, "CREATE TABLE `ordinary_loot_group`");
            StringAssert.Contains(fresh, "CREATE TABLE `module_class`");
            var rollback = migrator.GenerateScript(WildernessOrdinaryLootMigrationTests.ContentMigration, IncomingTail);
            StringAssert.Contains(rollback, "DROP TABLE `ordinary_loot_group`");
            StringAssert.Contains(rollback, "ALTER TABLE `legacy_flat_loot_group` RENAME `loot_group`");
            Assert.AreEqual(System.Data.ConnectionState.Closed, context.Database.GetDbConnection().State);
        }

        private static long Count(DbContext context, string table) =>
            context.Database.SqlQueryRaw<long>($"SELECT COUNT(*) AS Value FROM {table}").Single();
    }
}
