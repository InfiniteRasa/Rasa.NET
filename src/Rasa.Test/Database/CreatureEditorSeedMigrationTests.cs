using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Services.Preloader;

namespace Rasa.Test.Database
{
    [TestClass]
    public class CreatureEditorSeedMigrationTests
    {
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
