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
    }
}
