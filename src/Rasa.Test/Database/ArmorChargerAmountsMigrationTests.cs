using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Services.Preloader;

namespace Rasa.Test.Database
{
    // The Armor Charger's Class VI and VII (Armor_charger_amounts) restore 440 and 680 a second,
    // the 4400 and 6800 over 10 seconds their item text says, not Class VII's and VIII's.
    [TestClass]
    public class ArmorChargerAmountsMigrationTests
    {
        private const string Migration = "20261224000000_Armor_charger_amounts";
        private const string Previous = "20261222000000_Boss_weapons";

        /// <summary>Each class's total over its 10 seconds, from its item text.</summary>
        private static readonly int[] Totals = { 500, 750, 1200, 1850, 2850, 4400, 6800, 10500, 16200, 25000 };

        [TestMethod]
        public void EveryClassRestoresWhatItsTextSays()
        {
            using var database = new OrdinaryLootTestDatabase();
            database.Context.GetService<IMigrator>().Migrate();

            for (var level = 1; level <= 10; level++)
            {
                Assert.AreEqual(Totals[level - 1] / 10, Amount(database, level, ArmorChargerAmounts.HealAmountMin), $"Class {level}, min");
                Assert.AreEqual(Totals[level - 1] / 10, Amount(database, level, ArmorChargerAmounts.HealAmountMax), $"Class {level}, max");
            }
        }

        [TestMethod]
        public void DownPutsTheClientDataBackAndARowChangedByHandIsLeftAlone()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            Assert.AreEqual(680, Amount(database, 6, ArmorChargerAmounts.HealAmountMin));
            Assert.AreEqual(1050, Amount(database, 7, ArmorChargerAmounts.HealAmountMax));

            // Class VII's max set by hand: left as it is.
            database.Context.Database.ExecuteSqlRaw($"UPDATE action_property SET value = 999 WHERE action_id = 467 AND level = 7 AND property_id = {ArmorChargerAmounts.HealAmountMax}");

            migrator.Migrate(Migration);
            Assert.AreEqual(440, Amount(database, 6, ArmorChargerAmounts.HealAmountMax));
            Assert.AreEqual(680, Amount(database, 7, ArmorChargerAmounts.HealAmountMin));
            Assert.AreEqual(999, Amount(database, 7, ArmorChargerAmounts.HealAmountMax));
            Assert.AreEqual(1050, Amount(database, 8, ArmorChargerAmounts.HealAmountMin), "Class VIII as it was");

            migrator.Migrate(Previous);
            Assert.AreEqual(680, Amount(database, 6, ArmorChargerAmounts.HealAmountMin));
            Assert.AreEqual(1050, Amount(database, 7, ArmorChargerAmounts.HealAmountMin));
        }

        // A World that ran it as 20261223000000_Armor_charger_amounts, the timestamp
        // Random_name_quotes has: it runs again under its own, changes nothing, and the old row goes.
        [TestMethod]
        public void AWorldThatRanItUnderTheOldIdKeepsItsAmountsAndOnlyTheNewHistoryRow()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate();
            database.Context.Database.ExecuteSqlRaw(
                $"DELETE FROM __EFMigrationsHistory WHERE MigrationId = '{Migration}'; " +
                $"INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('{ArmorChargerAmounts.OldId}', '9.0.20');");

            CollectionAssert.Contains(database.Context.Database.GetPendingMigrations().ToList(), Migration);

            migrator.Migrate();

            Assert.AreEqual(440, Amount(database, 6, ArmorChargerAmounts.HealAmountMin));
            Assert.AreEqual(680, Amount(database, 7, ArmorChargerAmounts.HealAmountMax));
            var history = database.Context.Database.GetAppliedMigrations().ToList();
            CollectionAssert.Contains(history, Migration);
            CollectionAssert.DoesNotContain(history, ArmorChargerAmounts.OldId);
        }

        private static long Amount(OrdinaryLootTestDatabase database, int level, uint property) =>
            database.Context.Database.SqlQueryRaw<long>(
                $"SELECT value AS Value FROM action_property WHERE action_id = 467 AND level = {level} AND property_id = {property}").Single();
    }
}
