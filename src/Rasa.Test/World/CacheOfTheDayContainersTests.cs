using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Services.Preloader;
using Rasa.Test.Database;
namespace Rasa.Test.World
{
    [TestClass,DoNotParallelize]
    public class CacheOfTheDayContainersTests
    {
        const string Previous="20270907000000_Defender_reinforcement_timers", Migration="20270908000000_Cache_of_the_day_containers";
        [TestMethod]
        public void MigrationRemovesOnlyDuplicateSceneryPreservesCustomChangesAndRollsBack()
        {
            using var db=new OrdinaryLootTestDatabase();var migrator=db.Context.GetService<IMigrator>();migrator.Migrate(Previous);
            Assert.AreEqual(15,db.Context.WorldDestructibleEntries.Count(x=>x.MapContextId==1220 && x.ClassId==9260));
            db.Context.Database.ExecuteSqlRaw("UPDATE world_destructible SET pos_x=pos_x+1 WHERE id=640001");
            db.Context.Database.ExecuteSqlRaw("INSERT INTO world_destructible (id,map_context_id,class_id,pos_x,pos_y,pos_z,rotation,hit_points,creature_id,creature_count,side,comment) SELECT 990001,map_context_id,class_id,pos_x,pos_y,pos_z,rotation,hit_points,creature_id,creature_count,side,comment FROM world_destructible WHERE id=640001");
            var before=Snapshot(db);migrator.Migrate(Migration);
            Assert.AreEqual(2,db.Context.WorldDestructibleEntries.Count(x=>x.MapContextId==1220 && x.ClassId==9260),"custom relocation and unrelated barrel survive");
            Assert.AreEqual(1,db.Context.WorldDestructibleEntries.Count(x=>x.Id==WildernessPostNpcs.GateFieldId),"keep Landing Zone's field");
            var after=Snapshot(db);
            var changed=Enumerable.Range(1,14).Select(i=>WildernessPostNpcs.BarrelId(i)+" ").ToArray();
            CollectionAssert.AreEqual(before.Where(s=>!changed.Any(s.StartsWith)).ToArray(),after);
            foreach(var sql in CacheOfTheDayContainers.Up())db.Context.Database.ExecuteSqlRaw(sql);
            CollectionAssert.AreEqual(after,Snapshot(db));
            migrator.Migrate(Previous);CollectionAssert.AreEqual(before,Snapshot(db));
        }
        [TestMethod]
        public void MySqlUpgradeAndRollbackUseTheSameGuardedStatements()
        {
            using var db=PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext),"unused");
            var migrator=db.GetService<IMigrator>();var up=migrator.GenerateScript(Previous,Migration);var down=migrator.GenerateScript(Migration,Previous);
            foreach(var sql in CacheOfTheDayContainers.Up())StringAssert.Contains(up,sql);
            foreach(var sql in CacheOfTheDayContainers.Down())StringAssert.Contains(down,sql);
        }
        static string[] Snapshot(OrdinaryLootTestDatabase db)=>db.Context.Database.SqlQueryRaw<string>(
            "SELECT id||' '||map_context_id||' '||class_id||' '||pos_x||' '||pos_y||' '||pos_z||' '||rotation||' '||hit_points||' '||creature_id||' '||creature_count||' '||side||' '||comment AS Value FROM world_destructible ORDER BY id").ToArray();
    }
}
