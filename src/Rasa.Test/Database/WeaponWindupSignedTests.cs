using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Database
{
    using Rasa.Context.World;
    using Rasa.Structures.World;

    /// <summary>
    /// itemtemplate_weapon.windup made signed (Weapon_windup_signed), so a weapon can carry the -1
    /// the client's tooltip prints "Autofire" for (ItemTemplateWeaponEntry.AutofireWindup).
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class WeaponWindupSignedTests
    {
        private const string Before = "20270123000000_Thunderhead_base_teleporters";
        private const string Migration = "20270124000000_Weapon_windup_signed";

        [TestMethod]
        public void AWorldHoldsAndReadsBackAnAutofireWindupAndEveryOtherWindupIsKept()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Before);
            var before = Windups(database);
            Assert.IsNotEmpty(before);

            migrator.Migrate(Migration);
            CollectionAssert.AreEqual(before, Windups(database), "no row changes");

            var id = database.Context.Database.SqlQueryRaw<long>("SELECT MIN(id) AS Value FROM itemtemplate_weapon").AsEnumerable().Single();
            database.Context.Database.ExecuteSqlRaw($"UPDATE itemtemplate_weapon SET windup = {ItemTemplateWeaponEntry.AutofireWindup} WHERE id = {id};");

            var row = ((WorldContext)database.Context).ItemTemplateWeaponEntries.AsNoTracking().Single(entry => entry.Id == (uint)id);
            Assert.AreEqual(-1, row.Windup);
            Assert.IsFalse(database.Context.Database.HasPendingModelChanges());

            // Down: unsigned again, so the autofire mark becomes no windup.
            migrator.Migrate(Before);
            Assert.AreEqual(0L, database.Context.Database.SqlQueryRaw<long>($"SELECT windup AS Value FROM itemtemplate_weapon WHERE id = {id}").AsEnumerable().Single());
        }

        [TestMethod]
        public void OnMySqlTheColumnBecomesSignedAndBack()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(Before, Migration);
            var down = migrator.GenerateScript(Migration, Before);

            StringAssert.Contains(up, "ALTER TABLE `itemtemplate_weapon` MODIFY COLUMN `windup` int NOT NULL;");
            StringAssert.Contains(up, $"'{Migration}'");
            StringAssert.Contains(down, "UPDATE itemtemplate_weapon SET windup = 0 WHERE windup < 0;");
            StringAssert.Contains(down, "ALTER TABLE `itemtemplate_weapon` MODIFY COLUMN `windup` int unsigned NOT NULL;");
            Assert.IsLessThan(down.IndexOf("ALTER TABLE `itemtemplate_weapon`"), down.IndexOf("UPDATE itemtemplate_weapon"), "the -1s go before the column is unsigned");
        }

        private static long[] Windups(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<long>("SELECT windup AS Value FROM itemtemplate_weapon ORDER BY id").AsEnumerable().ToArray();
    }
}
