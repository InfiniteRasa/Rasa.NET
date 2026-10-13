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
        private const string Migration = "20261217000000_Wilderness_mortar_launchers";
        private const string Previous = "20261216000000_Bind_control_point_turret_loot";

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

        private static long[] Slot(OrdinaryLootTestDatabase database, uint id) =>
            Query(database, $"SELECT Class_id AS Value FROM creature_appearance WHERE id = {id} AND slot_id = 13");

        private static long[] Query(OrdinaryLootTestDatabase database, string sql) =>
            database.Context.Database.SqlQueryRaw<long>(sql).ToArray();
    }
}
