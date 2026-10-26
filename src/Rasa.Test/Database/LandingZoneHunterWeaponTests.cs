using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Services.Preloader;
namespace Rasa.Test.Database
{
    [TestClass, DoNotParallelize]
    public class LandingZoneHunterWeaponTests
    {
        const string Previous = "20270911000000_Childhoods_end_personal_encounter";
        const string Migration = "20270913000000_Landing_zone_hunter_weapon";
        [TestMethod]
        public void ForwardMigrationEquipsThePreviouslyUnarmedAssaultHunterOnceAndRollsBack()
        {
            using var db = new OrdinaryLootTestDatabase();
            var migrator = db.Context.GetService<IMigrator>();
            migrator.Migrate(Previous);
            Assert.AreEqual(0, Weapons(db).Length);
            migrator.Migrate(Migration);
            CollectionAssert.AreEqual(new long[] { 10171 }, Weapons(db));
            db.Context.Database.ExecuteSqlRaw(LandingZoneHunterWeapon.Up);
            CollectionAssert.AreEqual(new long[] { 10171 }, Weapons(db));
            migrator.Migrate(Previous);
            Assert.AreEqual(0, Weapons(db).Length);
        }
        [TestMethod]
        [DataRow(3782, 7)]
        [DataRow(10171, 7)]
        public void ExistingCustomWeaponsArePreserved(int weapon, int color)
        {
            using var db = new OrdinaryLootTestDatabase();
            var migrator = db.Context.GetService<IMigrator>();
            migrator.Migrate(Previous);
            db.Context.Database.ExecuteSqlRaw("INSERT INTO creature_appearance (id,slot_id,Class_id,color) VALUES (620004,13,{0},{1})", weapon, color);
            migrator.Migrate(Migration);
            CollectionAssert.AreEqual(new long[] { weapon }, Weapons(db));
            migrator.Migrate(Previous);
            CollectionAssert.AreEqual(new long[] { weapon }, Weapons(db));
        }
        [TestMethod]
        public void MySqlGeneratesTheForwardRepairAndGuardedRollback()
        {
            using var db = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrator = db.GetService<IMigrator>();
            StringAssert.Contains(migrator.GenerateScript(Previous, Migration), LandingZoneHunterWeapon.Up);
            StringAssert.Contains(migrator.GenerateScript(Migration, Previous), LandingZoneHunterWeapon.Down);
        }
        static long[] Weapons(OrdinaryLootTestDatabase db) => db.Context.Database.SqlQueryRaw<long>(
            "SELECT Class_id AS Value FROM creature_appearance WHERE id=620004 AND slot_id=13").ToArray();
    }
}
