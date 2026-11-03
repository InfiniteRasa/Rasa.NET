using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Services.Preloader;

namespace Rasa.Test.Database
{
    /// <summary>The Landing Zone assault's Thrax given the Bane rifle (LandingZoneThraxWeapons).</summary>
    [TestClass, DoNotParallelize]
    public class LandingZoneThraxWeaponsTests
    {
        private const string Previous = "20270919000000_Afs_turret_bases";
        private const string Migration = "20270920000000_Landing_zone_thrax_weapons";

        [TestMethod]
        public void EveryAssaultThraxCarriesTheBaneRifleAndDownTakesOnlyThatAway()
        {
            using var db = new OrdinaryLootTestDatabase();
            var migrator = db.Context.GetService<IMigrator>();
            migrator.Migrate(Previous);
            Assert.AreEqual(0, Weapons(db).Length, "made with empty hands");

            migrator.Migrate(Migration);
            CollectionAssert.AreEqual(new[] { "620001:3878", "620002:3878", "620003:3878", "620006:3878" }, Weapons(db));
            CollectionAssert.AreEqual(new long[] { 10171 }, db.Context.Database.SqlQueryRaw<long>(
                "SELECT Class_id AS Value FROM creature_appearance WHERE id=620004 AND slot_id=13").ToArray(), "the Hunter keeps its own");
            Assert.AreEqual(0, db.Context.Database.SqlQueryRaw<long>("SELECT COUNT(*) AS Value FROM creature_appearance WHERE id=620005").Single(), "a Linker has none");

            foreach (var sql in LandingZoneThraxWeapons.Up())
                Assert.AreEqual(0, db.Context.Database.ExecuteSqlRaw(sql), "once");

            // Weapon 1/79 is the attack the rows shoot.
            Assert.AreEqual(4L, db.Context.Database.SqlQueryRaw<long>(
                "SELECT COUNT(*) AS Value FROM creature c JOIN creature_action a ON a.id=c.action1 WHERE c.id IN (620001,620002,620003,620006) AND a.action_id=1 AND a.action_arg_id=79").Single());

            migrator.Migrate(Previous);
            Assert.AreEqual(0, Weapons(db).Length);
        }

        [TestMethod]
        public void AWeaponAlreadyGivenIsKept()
        {
            using var db = new OrdinaryLootTestDatabase();
            var migrator = db.Context.GetService<IMigrator>();
            migrator.Migrate(Previous);
            db.Context.Database.ExecuteSqlRaw("INSERT INTO creature_appearance (id,slot_id,Class_id,color) VALUES (620003,13,29885,1)");
            migrator.Migrate(Migration);
            CollectionAssert.Contains(Weapons(db), "620003:29885");
            migrator.Migrate(Previous);
            CollectionAssert.AreEqual(new[] { "620003:29885" }, Weapons(db));
        }

        [TestMethod]
        public void MySqlGeneratesTheSameStatements()
        {
            using var db = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrator = db.GetService<IMigrator>();
            var up = migrator.GenerateScript(Previous, Migration);
            var down = migrator.GenerateScript(Migration, Previous);
            foreach (var sql in LandingZoneThraxWeapons.Up()) StringAssert.Contains(up, sql);
            foreach (var sql in LandingZoneThraxWeapons.Down()) StringAssert.Contains(down, sql);
        }

        private static string[] Weapons(OrdinaryLootTestDatabase db) => db.Context.Database.SqlQueryRaw<string>(
            "SELECT CAST(id AS TEXT) || ':' || CAST(Class_id AS TEXT) AS Value FROM creature_appearance WHERE id IN (620001,620002,620003,620006) AND slot_id=13 ORDER BY id").ToArray();
    }
}
