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
    public class ChildhoodsEndPersonalEncounterTests
    {
        const string Previous = "20270910000000_Pierre_personal_escort", Migration = "20270911000000_Childhoods_end_personal_encounter";
        [TestMethod]
        public void MigrationMakesTheDefensePersonalKeepsPublicSpawnsAndRollsBackExactly()
        {
            using var db = new OrdinaryLootTestDatabase();
            var migrator = db.Context.GetService<IMigrator>();
            migrator.Migrate(Previous);
            var before = Binding(db); var pools = Pools(db);
            migrator.Migrate(Migration);
            var after = Binding(db);
            using var json = JsonDocument.Parse(after);
            foreach (var actor in json.RootElement.GetProperty("actors").EnumerateObject())
            {
                Assert.AreEqual("Creature", actor.Value.GetProperty("kind").GetString());
                Assert.IsTrue(actor.Value.GetProperty("ownerOnly").GetBoolean());
            }
            var ranger = json.RootElement.GetProperty("actors").GetProperty("anjuhi");
            Assert.AreEqual(98U, ranger.GetProperty("templateId").GetUInt32());
            Assert.AreEqual(176U, ranger.GetProperty("replacesPublicSpawnId").GetUInt32());
            Assert.IsTrue(ranger.GetProperty("replaceWhilePresent").GetBoolean());
            Assert.AreEqual(JsonValueKind.Null, json.RootElement.GetProperty("publicEncounter").ValueKind);
            Assert.AreEqual(14U, json.RootElement.GetProperty("defeatSequences").GetProperty("anjuhi").GetUInt32());
            CollectionAssert.AreEqual(pools, Pools(db));
            db.Context.Database.OpenConnection();
            foreach (var sql in ChildhoodsEndPersonalEncounter.Up())
            {
                using var command = db.Context.Database.GetDbConnection().CreateCommand();
                command.CommandText = sql; command.ExecuteNonQuery();
            }
            db.Context.Database.CloseConnection();
            Assert.AreEqual(after, Binding(db));
            migrator.Migrate(Previous);
            Assert.AreEqual(before, Binding(db));
            CollectionAssert.AreEqual(pools, Pools(db));
        }
        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void CustomActorOrDeathBindingsArePreserved(bool death)
        {
            using var db = new OrdinaryLootTestDatabase();
            var migrator = db.Context.GetService<IMigrator>(); migrator.Migrate(Previous);
            var before = Binding(db);
            var custom = death ? before.Replace("\"xanx-1\":11", "\"xanx-1\":99") :
                before.Replace("\"templateId\":176", "\"templateId\":177");
            Assert.AreNotEqual(before, custom);
            db.Context.Database.ExecuteSqlRaw("UPDATE mission_scene_binding SET bindings={0} WHERE mission_id=682", custom);
            migrator.Migrate(Migration);
            Assert.AreEqual(custom, Binding(db));
        }
        [TestMethod]
        public void MySqlUpgradeAndRollbackUseGuardedStatements()
        {
            using var db = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrator = db.GetService<IMigrator>();
            var up = migrator.GenerateScript(Previous, Migration); var down = migrator.GenerateScript(Migration, Previous);
            foreach (var sql in ChildhoodsEndPersonalEncounter.Up()) StringAssert.Contains(up, sql);
            foreach (var sql in ChildhoodsEndPersonalEncounter.Down()) StringAssert.Contains(down, sql);
        }
        static string Binding(OrdinaryLootTestDatabase db) => db.Context.Database.SqlQueryRaw<string>(
            "SELECT bindings AS Value FROM mission_scene_binding WHERE mission_id=682 AND script_key='wilderness.daghdas-rangers'").Single();
        static string[] Pools(OrdinaryLootTestDatabase db) => db.Context.Database.SqlQueryRaw<string>(
            "SELECT id||' '||mode||' '||creature_1_min_count||' '||creature_1_max_count AS Value FROM spawnpool ORDER BY id").ToArray();
    }
}
