using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Services.Preloader;

namespace Rasa.Test.Database
{
    // A creature that attacks with WEAPON_GROUNDTARGET fires Weapon_Creature_Bane_Mortar_Launcher,
    // and the client has nothing to fire it from unless the launcher is in the creature's weapon
    // slot (Wilderness_mortar_launchers): the Wilderness mortars hit with no shell and no blast.
    [TestClass]
    public class MortarLauncherTests
    {
        private const string Migration = "20261218000000_Wilderness_mortar_launchers";
        private const string Previous = "20261217000000_Fithik_bites";

        [TestMethod]
        public void EveryCreatureThatFiresAMortarHasTheLauncherInItsWeaponSlotOnce()
        {
            using var database = new OrdinaryLootTestDatabase();
            database.Context.GetService<IMigrator>().Migrate();

            var mortars = Query(database,
                "SELECT DISTINCT c.id AS Value FROM creature c JOIN creature_action a ON a.action_id = 411 AND " +
                "a.id IN (c.action1, c.action2, c.action3, c.action4, c.action5, c.action6, c.action7, c.action8) ORDER BY c.id");

            CollectionAssert.IsSubsetOf(WildernessMortarLaunchers.Mortars.Select(id => (long)id).ToArray(), mortars);
            foreach (var id in mortars)
                CollectionAssert.AreEqual(new long[] { WildernessMortarLaunchers.Launcher },
                    Query(database, $"SELECT Class_id AS Value FROM creature_appearance WHERE id = {id} AND slot_id = 13"), $"creature {id}");
        }

        [TestMethod]
        public void DownTakesTheLauncherAwayAndAWeaponAlreadyThereIsKept()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();
            var first = WildernessMortarLaunchers.Mortars[0];
            var second = WildernessMortarLaunchers.Mortars[1];

            migrator.Migrate(Previous);
            Assert.AreEqual(0, Slot(database, first).Length);

            // One given a weapon by hand keeps it.
            database.Context.Database.ExecuteSqlRaw(
                $"INSERT INTO creature_appearance (id, slot_id, Class_id, color) VALUES ({second}, 13, 3782, 7)");

            migrator.Migrate(Migration);
            CollectionAssert.AreEqual(new long[] { WildernessMortarLaunchers.Launcher }, Slot(database, first));
            CollectionAssert.AreEqual(new long[] { 3782 }, Slot(database, second));

            database.Context.Database.ExecuteSqlRaw(WildernessMortarLaunchers.Up);
            CollectionAssert.AreEqual(new long[] { WildernessMortarLaunchers.Launcher }, Slot(database, first), "guarded");

            migrator.Migrate(Previous);
            Assert.AreEqual(0, Slot(database, first).Length);
            CollectionAssert.AreEqual(new long[] { 3782 }, Slot(database, second), "not the launcher: left");
        }

        // A World that ran it as 20261217000000_Wilderness_mortar_launchers, the timestamp
        // Fithik_bites has: it runs again under its own, adds nothing, and the old row goes.
        [TestMethod]
        public void AWorldThatRanItUnderTheOldIdEndsWithOneLauncherAndOnlyTheNewHistoryRow()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();
            var first = WildernessMortarLaunchers.Mortars[0];

            migrator.Migrate(Previous);
            database.Context.Database.ExecuteSqlRaw(WildernessMortarLaunchers.Up);
            database.Context.Database.ExecuteSqlRaw(
                $"INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('{WildernessMortarLaunchers.OldId}', '9.0.20')");

            CollectionAssert.Contains(database.Context.Database.GetPendingMigrations().ToList(), Migration);

            migrator.Migrate();

            CollectionAssert.AreEqual(new long[] { WildernessMortarLaunchers.Launcher }, Slot(database, first), "one launcher, not two");
            var history = database.Context.Database.GetAppliedMigrations().ToList();
            CollectionAssert.Contains(history, Migration);
            CollectionAssert.DoesNotContain(history, WildernessMortarLaunchers.OldId);
        }

        // Six pairs older than this shared a timestamp before it, and are run on every World
        // already, several with table changes that are not guarded, so they keep their ids. So do
        // two later pairs, each of two migrations that touch nothing of the other's, run on Worlds
        // since: Alia_Das_cellar_link and Weapon_template_rows (20261230000000), Add_npc_barks and
        // Alia_das_npcs (20261231000000); Add_npc_barks creates a table, so it cannot run twice.
        // A new pair is a mistake: EF runs both, ordered by the rest of the id, whatever was meant.
        [TestMethod]
        public void NoNewMigrationSharesATimestamp()
        {
            var known = new[] { "20260927000000", "20260930000000", "20261012000000", "20261104000000", "20261111000000", "20261203000000",
                "20261230000000", "20261231000000" };

            using var database = new OrdinaryLootTestDatabase();
            var shared = database.Context.Database.GetMigrations()
                .GroupBy(id => id.Substring(0, 14)).Where(stamp => stamp.Count() > 1).Select(stamp => stamp.Key).ToList();

            CollectionAssert.AreEquivalent(known, shared, string.Join(", ", shared));
        }

        [TestMethod]
        public void TheMigrationForgetsTheOldIdOnMySql()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(Previous, Migration);

            StringAssert.Contains(up, WildernessMortarLaunchers.Up);
            StringAssert.Contains(up, "DELETE FROM __EFMigrationsHistory WHERE MigrationId = '20261217000000_Wilderness_mortar_launchers';");
            StringAssert.Contains(up, $"'{Migration}'");
        }

        private static long[] Slot(OrdinaryLootTestDatabase database, uint id) =>
            Query(database, $"SELECT Class_id AS Value FROM creature_appearance WHERE id = {id} AND slot_id = 13");

        private static long[] Query(OrdinaryLootTestDatabase database, string sql) =>
            database.Context.Database.SqlQueryRaw<long>(sql).ToArray();
    }
}
