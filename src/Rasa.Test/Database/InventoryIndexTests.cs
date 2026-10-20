using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Database
{
    using Rasa.Context.Char;

    /// <summary>
    /// The two inventory tables had no index but their key, the item: a character's inventory,
    /// the account lockbox and a clan's lockbox were each read with a scan of every item on the
    /// server (Index_character_and_clan_inventory).
    /// </summary>
    [TestClass]
    public class InventoryIndexTests
    {
        private const string Migration = "20261215000000_Index_character_and_clan_inventory";
        private const string Before = "20261214000000_Clan_name_unique";

        [TestMethod]
        public void TheIndexesAreMadeAreUsedAndDownDropsThem()
        {
            WithDatabase(characters =>
            {
                characters.Database.Migrate();

                CollectionAssert.AreEqual(new[] { "account_id", "character_id" }, Columns(characters, "character_inventory_index_account_character"));
                CollectionAssert.AreEqual(new[] { "clan_id" }, Columns(characters, "clan_inventory_index_clan_id"));

                // What the repositories ask: a character's inventory, the account's lockbox, a clan's lockbox.
                StringAssert.Contains(Plan(characters, "SELECT * FROM character_inventory WHERE account_id = 1 AND character_id = 2"), "character_inventory_index_account_character");
                StringAssert.Contains(Plan(characters, "SELECT * FROM character_inventory WHERE account_id = 1"), "character_inventory_index_account_character");
                StringAssert.Contains(Plan(characters, "SELECT * FROM clan_inventory WHERE clan_id = 3"), "clan_inventory_index_clan_id");

                characters.GetService<IMigrator>().Migrate(Before);

                Assert.IsEmpty(Columns(characters, "character_inventory_index_account_character"));
                Assert.IsEmpty(Columns(characters, "clan_inventory_index_clan_id"));

                characters.GetService<IMigrator>().Migrate(Migration);

                Assert.HasCount(2, Columns(characters, "character_inventory_index_account_character"));
                Assert.IsFalse(characters.Database.HasPendingModelChanges(), "the model and its snapshot carry them too");
            });
        }

        [TestMethod]
        public void OnMySqlTheSameIndexesAreMade()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(MySqlCharContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(Before, Migration);
            var down = migrator.GenerateScript(Migration, Before);

            StringAssert.Contains(up, "CREATE INDEX `character_inventory_index_account_character` ON `character_inventory` (`account_id`, `character_id`);");
            StringAssert.Contains(up, "CREATE INDEX `clan_inventory_index_clan_id` ON `clan_inventory` (`clan_id`);");
            StringAssert.Contains(down, "ALTER TABLE `character_inventory` DROP INDEX `character_inventory_index_account_character`;");
            StringAssert.Contains(down, "ALTER TABLE `clan_inventory` DROP INDEX `clan_inventory_index_clan_id`;");
            Assert.IsFalse(context.Database.HasPendingModelChanges());
        }

        private static List<string> Columns(DbContext context, string index) =>
            context.Database.SqlQueryRaw<string>($"SELECT name AS Value FROM pragma_index_info('{index}') ORDER BY seqno").ToList();

        private static string Plan(DbContext context, string sql)
        {
            var connection = context.Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open)
                connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "EXPLAIN QUERY PLAN " + sql;

            using var reader = command.ExecuteReader();
            var lines = new List<string>();

            while (reader.Read())
                lines.Add(reader.GetString(reader.FieldCount - 1));

            return string.Join(" | ", lines);
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
    }
}
