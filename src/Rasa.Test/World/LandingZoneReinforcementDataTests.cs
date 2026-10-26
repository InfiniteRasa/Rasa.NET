using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Services.Preloader;
using Rasa.Test.Database;

namespace Rasa.Test.World
{
    [TestClass, DoNotParallelize]
    public class LandingZoneReinforcementDataTests
    {
        const string Previous = "20270911000000_Childhoods_end_personal_encounter";
        const string Migration = "20270912000000_Landing_zone_assault_reinforcements";
        [TestMethod]
        public void MigrationAddsMatchingLevelElevenWeaponsAndRollsBackWithoutChangingTheGarrison()
        {
            using var db = new OrdinaryLootTestDatabase();
            var migrator = db.Context.GetService<IMigrator>();
            migrator.Migrate(Previous);
            var original = db.Context.Database.SqlQueryRaw<string>("SELECT id || ':' || level || ':' || max_hp || ':' || action1 AS Value FROM creature WHERE id BETWEEN 680001 AND 680002 ORDER BY id").ToArray();
            migrator.Migrate(Migration);
            Assert.AreEqual(3, db.Context.Database.SqlQueryRaw<int>("SELECT count(*) AS Value FROM creature c JOIN creature_stat s ON s.id=c.id JOIN creature_appearance a ON a.id=c.id AND a.slot_id=13 JOIN creature_action ca ON ca.id=c.action1 WHERE c.id BETWEEN 680011 AND 680013 AND c.level=11 AND c.faction=1 AND c.max_hp=650 AND s.health=650 AND s.armor=120 AND ca.max_damage>0").Single());
            var guns = db.Context.Database.SqlQueryRaw<string>("SELECT c.id || ':' || a.class_id || ':' || ca.action_id || ':' || ca.action_arg_id AS Value FROM creature c JOIN creature_appearance a ON a.id=c.id AND a.slot_id=13 JOIN creature_action ca ON ca.id=c.action1 WHERE c.id BETWEEN 680011 AND 680013 ORDER BY c.id").ToArray();
            CollectionAssert.AreEqual(new[] { "680011:6271:1:133", "680012:20560:1:121", "680013:20535:149:1" }, guns);
            migrator.Migrate(Previous);
            Assert.AreEqual(0, db.Context.Database.SqlQueryRaw<int>("SELECT count(*) AS Value FROM creature WHERE id BETWEEN 680011 AND 680013").Single());
            Assert.AreEqual(0, db.Context.Database.SqlQueryRaw<int>("SELECT count(*) AS Value FROM creature_action WHERE id BETWEEN 680011 AND 680013").Single());
            CollectionAssert.AreEqual(original, db.Context.Database.SqlQueryRaw<string>("SELECT id || ':' || level || ':' || max_hp || ':' || action1 AS Value FROM creature WHERE id BETWEEN 680001 AND 680002 ORDER BY id").ToArray());
        }
        [TestMethod]
        public void MySqlWorldProducesTheNewDataMigrationAndItsRollback()
        {
            using var db = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrator = db.GetService<IMigrator>();
            var up = migrator.GenerateScript(Previous, Migration);
            var down = migrator.GenerateScript(Migration, Previous);
            foreach (var sql in LandingZoneReinforcementContent.Up()) StringAssert.Contains(up, sql);
            foreach (var sql in LandingZoneReinforcementContent.Down()) StringAssert.Contains(down, sql);
        }
    }
}
