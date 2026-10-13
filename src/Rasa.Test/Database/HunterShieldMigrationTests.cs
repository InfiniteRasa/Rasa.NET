using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Services.Preloader;

namespace Rasa.Test.Database
{
    // Every Bane Hunter has its shield, Weapon_Creature_Hunter, in its weapon slot (Hunter_shields):
    // unarmed, a Hunter never drew it, and had no shield to reflect with.
    [TestClass]
    public class HunterShieldMigrationTests
    {
        private const string Migration = "20261219000000_Hunter_shields";
        private const string Previous = "20261218000000_Wilderness_mortar_launchers";

        [TestMethod]
        public void EveryHunterHasItsShieldInItsWeaponSlotOnce()
        {
            using var database = new OrdinaryLootTestDatabase();
            database.Context.GetService<IMigrator>().Migrate();

            var hunters = Query(database, "SELECT id AS Value FROM creature WHERE class_id IN (10166, 10327) ORDER BY id");

            CollectionAssert.IsSubsetOf(HunterShields.Hunters.Select(id => (long)id).ToArray(), hunters);
            foreach (var id in hunters)
                CollectionAssert.AreEqual(new long[] { HunterShields.Shield }, Slot(database, (uint)id), $"creature {id}");
        }

        [TestMethod]
        public void DownTakesTheShieldAwayAndAWeaponAlreadyThereIsKept()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();
            var first = HunterShields.Hunters[0];
            var second = HunterShields.Hunters[1];

            migrator.Migrate(Previous);
            Assert.AreEqual(0, Slot(database, first).Length);
            CollectionAssert.AreEqual(new long[] { HunterShields.Shield }, Slot(database, 81), "the boss had it already");

            database.Context.Database.ExecuteSqlRaw(
                $"INSERT INTO creature_appearance (id, slot_id, Class_id, color) VALUES ({second}, 13, 3782, 7)");

            migrator.Migrate(Migration);
            CollectionAssert.AreEqual(new long[] { HunterShields.Shield }, Slot(database, first));
            CollectionAssert.AreEqual(new long[] { 3782 }, Slot(database, second));

            database.Context.Database.ExecuteSqlRaw(HunterShields.Up);
            CollectionAssert.AreEqual(new long[] { HunterShields.Shield }, Slot(database, first), "guarded");

            migrator.Migrate(Previous);
            Assert.AreEqual(0, Slot(database, first).Length);
            CollectionAssert.AreEqual(new long[] { 3782 }, Slot(database, second));
            CollectionAssert.AreEqual(new long[] { HunterShields.Shield }, Slot(database, 81), "the boss keeps its own");
        }

        // A World that ran it as 20261218000000_Hunter_shields, the timestamp
        // Wilderness_mortar_launchers has: it runs again under its own, adds nothing, and the
        // old row goes.
        [TestMethod]
        public void AWorldThatRanItUnderTheOldIdEndsWithOneShieldAndOnlyTheNewHistoryRow()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();
            var first = HunterShields.Hunters[0];

            migrator.Migrate(Previous);
            database.Context.Database.ExecuteSqlRaw(HunterShields.Up);
            database.Context.Database.ExecuteSqlRaw(
                $"INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('{HunterShields.OldId}', '9.0.20')");

            CollectionAssert.Contains(database.Context.Database.GetPendingMigrations().ToList(), Migration);

            migrator.Migrate();

            CollectionAssert.AreEqual(new long[] { HunterShields.Shield }, Slot(database, first), "one shield, not two");
            var history = database.Context.Database.GetAppliedMigrations().ToList();
            CollectionAssert.Contains(history, Migration);
            CollectionAssert.DoesNotContain(history, HunterShields.OldId);
        }

        private static long[] Slot(OrdinaryLootTestDatabase database, uint id) =>
            Query(database, $"SELECT Class_id AS Value FROM creature_appearance WHERE id = {id} AND slot_id = 13");

        private static long[] Query(OrdinaryLootTestDatabase database, string sql) =>
            database.Context.Database.SqlQueryRaw<long>(sql).ToArray();
    }
}
