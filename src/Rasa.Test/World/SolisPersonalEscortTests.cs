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
    [TestClass,DoNotParallelize]
    public class SolisPersonalEscortTests
    {
        const string Previous="20270908000000_Cache_of_the_day_containers", Migration="20270909000000_Solis_personal_escort";
        [TestMethod]
        public void MigrationChangesOnlyRangerOwnershipPreservesTheAdjustedRouteAndRollsBack()
        {
            using var db=new OrdinaryLootTestDatabase();var migrator=db.Context.GetService<IMigrator>();migrator.Migrate(Previous);
            var before=Binding(db);var pools=Pools(db);
            using var original=JsonDocument.Parse(before);
            migrator.Migrate(Migration);
            var after=Binding(db);using var personal=JsonDocument.Parse(after);
            Assert.AreEqual("Creature",personal.RootElement.GetProperty("actors").GetProperty("ranger").GetProperty("kind").GetString());
            Assert.AreEqual(JsonValueKind.Null,personal.RootElement.GetProperty("publicEncounter").ValueKind);
            Assert.AreEqual(original.RootElement.GetProperty("routes").GetRawText(),personal.RootElement.GetProperty("routes").GetRawText());
            Assert.AreEqual("2 0 0",db.Context.Database.SqlQueryRaw<string>("SELECT mode||' '||creature_1_min_count||' '||creature_1_max_count AS Value FROM spawnpool WHERE id=630001").Single());
            CollectionAssert.AreEqual(pools.Where(s=>!s.StartsWith("630001 ")).ToArray(),Pools(db).Where(s=>!s.StartsWith("630001 ")).ToArray());
            db.Context.Database.OpenConnection();
            foreach(var sql in SolisPersonalEscort.Up()) { using var command=db.Context.Database.GetDbConnection().CreateCommand();command.CommandText=sql;command.ExecuteNonQuery(); }
            db.Context.Database.CloseConnection();
            Assert.AreEqual(after,Binding(db));
            migrator.Migrate(Previous);Assert.AreEqual(before,Binding(db));CollectionAssert.AreEqual(pools,Pools(db));
        }
        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void CustomPublicActorDeclarationDoesNotLoseItsSpawn(bool deathBinding)
        {
            using var db=new OrdinaryLootTestDatabase();var migrator=db.Context.GetService<IMigrator>();migrator.Migrate(Previous);
            var before=Binding(db);using var json=JsonDocument.Parse(before);
            var actor=json.RootElement.GetProperty("actors").GetProperty("ranger").GetRawText();
            var custom=deathBinding ? before.Substring(0,before.Length-1)+",\"defeatSequences\":{\"ranger\":1}}" :
                before.Replace(actor,actor.Replace("\"orientation\":0","\"orientation\":1"));
            Assert.AreNotEqual(before,custom);
            db.Context.Database.ExecuteSqlRaw("UPDATE mission_scene_binding SET bindings={0} WHERE mission_id=1407",custom);
            var pools=Pools(db);migrator.Migrate(Migration);
            Assert.AreEqual(custom,Binding(db));CollectionAssert.AreEqual(pools,Pools(db));
        }
        [TestMethod]
        public void MySqlUpgradeAndRollbackUseTheGuardedStatements()
        {
            using var db=PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext),"unused");
            var migrator=db.GetService<IMigrator>();var up=migrator.GenerateScript(Previous,Migration);var down=migrator.GenerateScript(Migration,Previous);
            foreach(var sql in SolisPersonalEscort.Up())StringAssert.Contains(up,sql);
            foreach(var sql in SolisPersonalEscort.Down())StringAssert.Contains(down,sql);
        }
        static string Binding(OrdinaryLootTestDatabase db)=>db.Context.Database.SqlQueryRaw<string>("SELECT bindings AS Value FROM mission_scene_binding WHERE mission_id=1407 AND script_key='wilderness.solis-escort'").Single();
        static string[] Pools(OrdinaryLootTestDatabase db)=>db.Context.Database.SqlQueryRaw<string>("SELECT id||' '||mode||' '||creature_1_min_count||' '||creature_1_max_count AS Value FROM spawnpool ORDER BY id").ToArray();
    }
}
