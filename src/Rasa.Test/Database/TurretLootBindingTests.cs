using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Services.Preloader;

namespace Rasa.Test.Database
{
    // The Wilderness Bane turret of Add_control_point_turrets, bound to a loot profile as
    // Wilderness_ordinary_loot_v1 requires of every hostile garrison creature on map 1220
    // (Bind_control_point_turret_loot), on a fresh database and on one that had it by hand.
    [TestClass]
    public class TurretLootBindingTests
    {
        private const string Migration = "20261216000000_Bind_control_point_turret_loot";
        private const string Previous = "20261215000000_Alia_Das_arena_link";

        [TestMethod]
        public void AFreshWorldBindsTheTurretToTheMortarsProfileAndDownTakesItAway()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate();
            CollectionAssert.AreEqual(new[] { ControlPointTurretLoot.Profile }, Bindings(database));

            migrator.Migrate(Previous);
            Assert.AreEqual(0, Bindings(database).Length);

            migrator.Migrate(Migration);
            CollectionAssert.AreEqual(new[] { ControlPointTurretLoot.Profile }, Bindings(database));
        }

        [TestMethod]
        public void AWorldBoundByHandIsLeftWithItsOneRow()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            database.Context.Database.ExecuteSqlRaw(ControlPointTurretLoot.Up);
            database.Context.Database.ExecuteSqlRaw(ControlPointTurretLoot.Up);
            CollectionAssert.AreEqual(new[] { ControlPointTurretLoot.Profile }, Bindings(database), "the statement is guarded");

            migrator.Migrate();
            CollectionAssert.AreEqual(new[] { ControlPointTurretLoot.Profile }, Bindings(database));
        }

        private static string[] Bindings(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                $"SELECT profile_key AS Value FROM creature_loot_binding WHERE map_context_id = {ControlPointTurretLoot.Map} " +
                $"AND creature_id = {ControlPointTurretLoot.Creature}").ToArray();
    }
}
