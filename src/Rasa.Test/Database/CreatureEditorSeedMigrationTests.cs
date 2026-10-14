using System;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Services.Preloader;

namespace Rasa.Test.Database
{
    [TestClass]
    public class CreatureEditorSeedMigrationTests
    {
        [TestMethod]
        public void SqliteColumnRollbacksUseSupportedSqlInsteadOfDropColumnOperations()
        {
            // SharedMapInstanceTests rolls back the entire World migration chain to
            // 20261105000000_Add_control_points. EF Core's SQLite generator rejects
            // DropColumnOperation in Down(), even though native SQLite DROP COLUMN works.
            Migration[] migrations =
            {
                new Rasa.Migrations.SqliteWorld.Add_creature_damage_type(),
                new Rasa.Migrations.SqliteWorld.Add_spawn_areas(),
                new Rasa.Migrations.SqliteWorld.Add_npc_greeting_important(),
                new Rasa.Migrations.SqliteWorld.Creature_editor_colors()
            };
            foreach (var migration in migrations)
            {
                var builder = new MigrationBuilder("Microsoft.EntityFrameworkCore.Sqlite");
                var down = migration.GetType().GetMethod("Down", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(down, $"Missing Down() for {migration.GetType().Name}");
                down.Invoke(migration, new object[] { builder });

                Assert.IsFalse(builder.Operations.OfType<DropColumnOperation>().Any(),
                    $"{migration.GetType().Name} emits unsupported SQLite DropColumnOperation");
                Assert.IsTrue(builder.Operations.OfType<SqlOperation>().Any(operation =>
                        operation.Sql.Contains("DROP COLUMN", StringComparison.OrdinalIgnoreCase)),
                    $"{migration.GetType().Name} should remove its column with native SQLite SQL");
            }
        }

        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void HistoricalCreatureSeedsUseColumnsFromTheirOriginalSchema(bool appearance)
        {
            var builder = new MigrationBuilder("Microsoft.EntityFrameworkCore.Sqlite");
            if (appearance)
                new CreatureAppearancePreloader().Preload(builder);
            else
                new CreaturePreloader().Preload(builder);

            var insert = builder.Operations.OfType<InsertDataOperation>().Single();
            var originalWidth = appearance ? 4 : 17;
            Assert.AreEqual(originalWidth, insert.Columns.Length);
            Assert.AreEqual(originalWidth, insert.Values.GetLength(1));
            Assert.IsFalse(insert.Columns.Contains("hue2"));
            Assert.IsFalse(insert.Columns.Contains("body_hue"));
            Assert.IsFalse(insert.Columns.Contains("body_hue2"));
        }
        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void BootcampHistoricalCreatureSeedsUseTheirOriginalColumnWidths(bool appearance)
        {
            var builder = new MigrationBuilder("Microsoft.EntityFrameworkCore.Sqlite");
            if (appearance)
                new BootcampMissionNpcAppearancePreloader().Preload(builder);
            else
                new BootcampMissionNpcCreaturePreloader().Preload(builder);

            var table = appearance ? "creature_appearance" : "creature";
            var originalWidth = appearance ? 4 : 17;
            var prefix = "insert into " + table + " (";
            var inserts = builder.Operations.OfType<SqlOperation>()
                .Where(operation => operation.Sql.StartsWith(prefix)).ToArray();
            Assert.IsTrue(inserts.Length > 0, "Bootcamp seed did not insert " + table);
            foreach (var insert in inserts)
            {
                var afterPrefix = insert.Sql.Substring(prefix.Length);
                var closing = afterPrefix.IndexOf(')');
                Assert.IsTrue(closing > 0);
                var columns = afterPrefix.Substring(0, closing).Split(',')
                    .Select(column => column.Trim()).ToArray();
                Assert.AreEqual(originalWidth, columns.Length);
                CollectionAssert.DoesNotContain(columns, "body_hue");
                CollectionAssert.DoesNotContain(columns, "body_hue2");
                CollectionAssert.DoesNotContain(columns, "hue2");
            }
        }
    }
}
