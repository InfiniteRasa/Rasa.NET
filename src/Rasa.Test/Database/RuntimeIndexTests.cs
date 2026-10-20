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
    using Rasa.Context.World;

    /// <summary>
    /// Indexes for queries the game server runs while it is up that read their whole table
    /// (Add_runtime_indexes on the character database, Index_creature_appearance on the world's).
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class RuntimeIndexTests
    {
        private const string CharMigration = "20270119000000_Add_runtime_indexes";
        private const string CharBefore = "20261226000000_Add_character_xp_boost";
        private const string WorldMigration = "20270119000000_Index_creature_appearance";
        private const string WorldBefore = "20270117000000_Unplaced_named_npcs";

        /// <summary>Each index, its columns, and a query the repositories make that should use it.</summary>
        private static readonly (string Index, string[] Columns, string Query)[] CharIndexes =
        {
            ("character_inventory_index_character_type", new[] { "character_id", "invenotry_type" },
                "SELECT * FROM character_inventory WHERE invenotry_type = 7 AND character_id IN (1, 2)"),
            ("mission_scene_index_assignment", new[] { "assignment_id" },
                "SELECT * FROM mission_scene WHERE assignment_id = 'a'"),
            ("mission_scene_participant_index_assignment", new[] { "assignment_id" },
                "SELECT * FROM mission_scene_participant WHERE assignment_id = 'a'"),
            ("petition_index_account_notice", new[] { "account_id", "notice_pending" },
                "SELECT * FROM petition WHERE account_id = 1 AND notice_pending = 1 ORDER BY id"),
            ("pvp_match_index_kind_ended", new[] { "kind", "ended_at" },
                "SELECT * FROM pvp_match WHERE kind = 1 AND ended_at IS NULL ORDER BY id"),
            ("squad_instance_index_owner_map", new[] { "owner_character_id", "map_context_id" },
                "SELECT * FROM squad_instance WHERE map_context_id = 1 AND owner_character_id = 2"),
            ("squad_instance_visitor_index_instance", new[] { "instance_id" },
                "SELECT * FROM squad_instance_visitor WHERE instance_id = 3"),
        };

        [TestMethod]
        public void TheCharacterIndexesAreMadeAreUsedAndDownDropsThem()
        {
            WithDatabase(typeof(SqliteCharContext), context =>
            {
                context.Database.Migrate();

                foreach (var (index, columns, query) in CharIndexes)
                {
                    CollectionAssert.AreEqual(columns, Columns(context, index), index);
                    StringAssert.Contains(Plan(context, query), index, query);
                }

                // The inventory's own index still serves what it did.
                StringAssert.Contains(Plan(context, "SELECT * FROM character_inventory WHERE account_id = 1 AND character_id = 2"),
                    "character_inventory_index_account_character");

                context.GetService<IMigrator>().Migrate(CharBefore);
                foreach (var (index, _, _) in CharIndexes)
                    Assert.IsEmpty(Columns(context, index), index);

                context.GetService<IMigrator>().Migrate(CharMigration);
                foreach (var (index, columns, _) in CharIndexes)
                    Assert.HasCount(columns.Length, Columns(context, index), index);

                Assert.IsFalse(context.Database.HasPendingModelChanges(), "the model and its snapshot carry them too");
            });
        }

        [TestMethod]
        public void TheCreatureAppearanceIndexIsMadeIsUsedAndDownDropsIt()
        {
            WithDatabase(typeof(SqliteWorldContext), context =>
            {
                context.Database.Migrate();

                CollectionAssert.AreEqual(new[] { "id", "slot_id" }, Columns(context, "creature_appearance_index_creature_slot"));
                StringAssert.Contains(Plan(context, "SELECT * FROM creature_appearance WHERE id = 5"), "creature_appearance_index_creature_slot");
                StringAssert.Contains(Plan(context, "UPDATE creature_appearance SET color = 1 WHERE id = 5 AND slot_id = 2"), "creature_appearance_index_creature_slot");
                Assert.AreEqual(0L, context.Database.SqlQueryRaw<long>(
                    "SELECT \"unique\" AS Value FROM pragma_index_list('creature_appearance') WHERE name = 'creature_appearance_index_creature_slot'").Single(), "not unique");

                context.GetService<IMigrator>().Migrate(WorldBefore);
                Assert.IsEmpty(Columns(context, "creature_appearance_index_creature_slot"));

                context.GetService<IMigrator>().Migrate(WorldMigration);
                Assert.HasCount(2, Columns(context, "creature_appearance_index_creature_slot"));
                Assert.IsFalse(context.Database.HasPendingModelChanges());
            });
        }

        [TestMethod]
        public void OnMySqlTheSameIndexesAreMade()
        {
            using (var chars = PersistenceIntegrationTests.CreateContext(typeof(MySqlCharContext), "unused"))
            {
                var migrator = chars.GetService<IMigrator>();
                var up = migrator.GenerateScript(CharBefore, CharMigration);
                var down = migrator.GenerateScript(CharMigration, CharBefore);

                foreach (var (index, columns, _) in CharIndexes)
                {
                    var table = CharIndexes.First(i => i.Index == index).Query.Split(" FROM ")[1].Split(' ')[0];
                    StringAssert.Contains(up, $"CREATE INDEX `{index}` ON `{table}` ({string.Join(", ", columns.Select(c => $"`{c}`"))});");
                    StringAssert.Contains(down, $"ALTER TABLE `{table}` DROP INDEX `{index}`;");
                }

                Assert.IsFalse(chars.Database.HasPendingModelChanges());
            }

            using var world = PersistenceIntegrationTests.CreateContext(typeof(MySqlWorldContext), "unused");
            var worldUp = world.GetService<IMigrator>().GenerateScript(WorldBefore, WorldMigration);
            StringAssert.Contains(worldUp, "CREATE INDEX `creature_appearance_index_creature_slot` ON `creature_appearance` (`id`, `slot_id`);");
            Assert.IsFalse(world.Database.HasPendingModelChanges());
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

        private static void WithDatabase(Type contextType, Action<DbContext> body)
        {
            var path = Path.Combine(AppContext.BaseDirectory, "TestDatabases", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);

            try
            {
                using var context = PersistenceIntegrationTests.CreateContext(contextType, Path.Combine(path, "database"));
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
