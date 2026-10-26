using System;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Database
{
    using Rasa.Context.Char;
    using Rasa.Services.Preloader;

    /// <summary>
    /// Items are made white, the client's colour for an unpainted piece, and Item_default_color
    /// gives the ones made in the old grey, and the appearance rows that show them, the white.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class ItemDefaultColorTests
    {
        private const string Migration = "20270130000000_Item_default_color";
        private const string Before = "20270119000000_Add_runtime_indexes";

        private const uint Paint = 4281940849;      // a paint swatch, (113, 57, 57, 255)

        [TestMethod]
        public void TheGameServersDefaultIsTheMigrationsNewColourAndWhite()
        {
            Assert.AreEqual(ItemDefaultColor.New, Rasa.Structures.Color.ItemDefault);

            var white = new Rasa.Structures.Color(Rasa.Structures.Color.ItemDefault);
            Assert.AreEqual((255, 255, 255, 255), (white.Red, white.Green, white.Blue, white.Alpha), "kDefaultColorArmor");
        }

        [TestMethod]
        public void TheGreyItemsAndTheirAppearanceAreMadeWhiteAndDownPutsThemBack()
        {
            WithDatabase(context =>
            {
                var migrator = context.GetService<IMigrator>();
                migrator.Migrate(Before);

                // Rows of their own, without the characters and templates they would belong to.
                context.Database.OpenConnection();
                context.Database.ExecuteSqlRaw("PRAGMA foreign_keys = OFF");
                context.Database.ExecuteSqlRaw(
                    "insert into items (item_id, item_template_id, stack_size, current_hp, color, ammo_count, bound_character_id, crafter_name, created_at) values " +
                    $"(1, 28, 1, 55, {ItemDefaultColor.Old}, 0, 0, '', '2026-01-01 00:00:00'), " +
                    $"(2, 28, 1, 55, {Paint}, 0, 0, '', '2026-01-01 00:00:00'), " +
                    "(3, 28, 1, 55, 0, 0, 0, '', '2026-01-01 00:00:00')");
                context.Database.ExecuteSqlRaw(
                    "insert into character_appearance (character_id, slot, class, color) values " +
                    $"(1, 3, 10000068, {ItemDefaultColor.Old}), (1, 2, 4021, 4280300658), (1, 13, 27120, {Paint})");

                migrator.Migrate(Migration);

                Assert.AreEqual(ItemDefaultColor.New, ItemColor(context, 1), "made in the grey");
                Assert.AreEqual(Paint, ItemColor(context, 2), "painted");
                Assert.AreEqual(0u, ItemColor(context, 3), "not the grey");
                Assert.AreEqual(ItemDefaultColor.New, AppearanceColor(context, 3));
                Assert.AreEqual(4280300658u, AppearanceColor(context, 2), "a face's colour is the skin's");
                Assert.AreEqual(Paint, AppearanceColor(context, 13));

                // A second run of the statements changes nothing.
                foreach (var statement in ItemDefaultColor.CharUp)
                    Assert.AreEqual(0, context.Database.ExecuteSqlRaw(statement), statement);

                migrator.Migrate(Before);

                Assert.AreEqual(ItemDefaultColor.Old, ItemColor(context, 1));
                Assert.AreEqual(Paint, ItemColor(context, 2));
                Assert.AreEqual(ItemDefaultColor.Old, AppearanceColor(context, 3));
                Assert.AreEqual(4280300658u, AppearanceColor(context, 2));
            });
        }

        [TestMethod]
        public void OnMySqlTheSameStatementsAreMade()
        {
            using var chars = PersistenceIntegrationTests.CreateContext(typeof(MySqlCharContext), "unused");
            var migrator = chars.GetService<IMigrator>();
            var up = migrator.GenerateScript(Before, Migration);
            var down = migrator.GenerateScript(Migration, Before);

            StringAssert.Contains(up, $"update items set color = {ItemDefaultColor.New} where color = {ItemDefaultColor.Old};");
            StringAssert.Contains(up, $"update character_appearance set color = {ItemDefaultColor.New} where color = {ItemDefaultColor.Old};");
            StringAssert.Contains(down, $"update items set color = {ItemDefaultColor.Old} where color = {ItemDefaultColor.New};");
            Assert.IsFalse(chars.Database.HasPendingModelChanges());
        }

        private static uint ItemColor(DbContext context, uint id) =>
            context.Database.SqlQueryRaw<uint>($"SELECT color AS Value FROM items WHERE item_id = {id}").Single();

        private static uint AppearanceColor(DbContext context, uint slot) =>
            context.Database.SqlQueryRaw<uint>($"SELECT color AS Value FROM character_appearance WHERE character_id = 1 AND slot = {slot}").Single();

        private static void WithDatabase(Action<DbContext> body)
        {
            var path = Path.Combine(AppContext.BaseDirectory, "TestDatabases", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);

            try
            {
                using var context = PersistenceIntegrationTests.CreateContext(typeof(SqliteCharContext), Path.Combine(path, "database"));
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
