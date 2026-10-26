using System.Linq;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Services.Preloader;
using Rasa.Test.Database;

namespace Rasa.Test.World
{
    [TestClass, DoNotParallelize]
    public class PierrePersonalEscortTests
    {
        const string Previous = "20270909000000_Solis_personal_escort", Migration = "20270910000000_Pierre_personal_escort";
        [TestMethod]
        public void MigrationKeepsOfferNpcAndDeadlineChangesBothRoutesAndRollsBackExactly()
        {
            using var db = new OrdinaryLootTestDatabase();
            var migrator = db.Context.GetService<IMigrator>();
            migrator.Migrate(Previous);
            var before = Binding(db);
            var pools = Pools(db);
            migrator.Migrate(Migration);
            var after = Binding(db);
            using var json = JsonDocument.Parse(after);
            var actor = json.RootElement.GetProperty("actors").GetProperty("pierre");
            Assert.AreEqual("Creature", actor.GetProperty("kind").GetString());
            Assert.AreEqual(630070U, actor.GetProperty("templateId").GetUInt32());
            Assert.IsTrue(actor.GetProperty("ownerOnly").GetBoolean());
            Assert.IsTrue(actor.GetProperty("holdCombatUntilRoute").GetBoolean());
            Assert.AreEqual(630070U, actor.GetProperty("replacesPublicSpawnId").GetUInt32());
            Assert.AreEqual(JsonValueKind.Null, json.RootElement.GetProperty("publicEncounter").ValueKind);
            foreach (var route in json.RootElement.GetProperty("routes").EnumerateObject())
                Assert.AreEqual(6f, route.Value.GetProperty("speed").GetSingle());
            CollectionAssert.AreEqual(pools, Pools(db), "The offer NPC and all ordinary spawn pools must stay intact.");
            db.Context.Database.OpenConnection();
            foreach (var sql in PierrePersonalEscort.Up())
            {
                using var command = db.Context.Database.GetDbConnection().CreateCommand();
                command.CommandText = sql;
                command.ExecuteNonQuery();
            }
            db.Context.Database.CloseConnection();
            Assert.AreEqual(after, Binding(db), "Idempotent upgrade");
            migrator.Migrate(Previous);
            Assert.AreEqual(before, Binding(db));
            CollectionAssert.AreEqual(pools, Pools(db));
        }
        [TestMethod]
        public void CustomRouteIsNotOverwritten()
        {
            using var db = new OrdinaryLootTestDatabase();
            var migrator = db.Context.GetService<IMigrator>();
            migrator.Migrate(Previous);
            var before = Binding(db);
            var custom = before.Replace("\"speed\":2.5", "\"speed\":3");
            Assert.AreNotEqual(before, custom);
            db.Context.Database.ExecuteSqlRaw("UPDATE mission_scene_binding SET bindings={0} WHERE mission_id=666", custom);
            migrator.Migrate(Migration);
            Assert.AreEqual(custom, Binding(db));
        }
        [TestMethod]
        public void MySqlUpgradeAndRollbackUseGuardedStatements()
        {
            using var db = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrator = db.GetService<IMigrator>();
            var up = migrator.GenerateScript(Previous, Migration);
            var down = migrator.GenerateScript(Migration, Previous);
            foreach (var sql in PierrePersonalEscort.Up()) StringAssert.Contains(up, sql);
            foreach (var sql in PierrePersonalEscort.Down()) StringAssert.Contains(down, sql);
        }
        static string Binding(OrdinaryLootTestDatabase db) => db.Context.Database.SqlQueryRaw<string>(
            "SELECT bindings AS Value FROM mission_scene_binding WHERE mission_id=666 AND script_key='wilderness.escape-velocity'").Single();
        static string[] Pools(OrdinaryLootTestDatabase db) => db.Context.Database.SqlQueryRaw<string>(
            "SELECT id||' '||mode||' '||creature_1_min_count||' '||creature_1_max_count AS Value FROM spawnpool ORDER BY id").ToArray();
    }
}
