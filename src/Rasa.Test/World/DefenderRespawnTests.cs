using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Services.Preloader;
using Rasa.Test.Database;
namespace Rasa.Test.World
{
 [TestClass,DoNotParallelize] public class DefenderRespawnTests
 {
  const string Previous="20270906000000_Landing_zone_breach", Migration="20270907000000_Defender_reinforcement_timers";
  [TestMethod] public void MigrationChangesOnlyDefaultDefendersPreservesCustomTimersAndRollsBack()
  {
   using var db=new OrdinaryLootTestDatabase(); var migrator=db.Context.GetService<IMigrator>(); migrator.Migrate(Previous);
   db.Context.Database.ExecuteSqlRaw("UPDATE spawnpool SET respown_time=4500 WHERE id=640019");
   var before=Snapshot(db); migrator.Migrate(Migration); var after=Snapshot(db);
   Assert.AreEqual(3000u,db.Context.SpawnPoolEntries.AsNoTracking().Single(p=>p.Id==117).RespawnTime);
   Assert.AreEqual(3000u,db.Context.SpawnPoolEntries.AsNoTracking().Single(p=>p.Id==640017).RespawnTime);
   Assert.AreEqual(4500u,db.Context.SpawnPoolEntries.AsNoTracking().Single(p=>p.Id==640019).RespawnTime);
   foreach(var p in DefenderRespawns.Pools) Assert.AreEqual(p.Pool==640019?4500u:3000u,db.Context.SpawnPoolEntries.AsNoTracking().Single(s=>s.Id==p.Pool).RespawnTime,p.Pool.ToString());
   CollectionAssert.AreEqual(before.Where(s=>!DefenderRespawns.Pools.Any(p=>s.StartsWith(p.Pool+" "))).ToArray(),after.Where(s=>!DefenderRespawns.Pools.Any(p=>s.StartsWith(p.Pool+" "))).ToArray());
   foreach(var sql in DefenderRespawns.Up())db.Context.Database.ExecuteSqlRaw(sql);
   CollectionAssert.AreEqual(after,Snapshot(db));
   migrator.Migrate(Previous); CollectionAssert.AreEqual(before,Snapshot(db));
  }
  [TestMethod] public void MySqlMigrationUsesTheSameGuardedUpdates()
  {
   using var db=PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext),"unused");
   var migrator=db.GetService<IMigrator>(); var up=migrator.GenerateScript(Previous,Migration);var down=migrator.GenerateScript(Migration,Previous);
   foreach(var sql in DefenderRespawns.Up())StringAssert.Contains(up,sql);
   foreach(var sql in DefenderRespawns.Down())StringAssert.Contains(down,sql);
  }
  [TestMethod] public void LegacySquadReturnsAfterFiveMinutesRatherThanTwoSeconds()
  {
   using var h=Rasa.Test.Missions.Wilderness.WildernessRuntimeTestHarness.Create(); h.SpawnWorld(117);
   var pool=h.Map.SpawnPools.Single(p=>p.DbId==117);
   Assert.AreEqual(300000L,pool.RespawnTime);
   Rasa.Structures.Creature[] Living()=>Rasa.Managers.EntityManager.Instance.Creatures.Values.Where(c=>c.SpawnPool==pool && Rasa.Managers.CreatureManager.IsLivingOnMap(h.Map,c)).ToArray();
   Assert.IsTrue(Living().Length>=3);
   foreach(var c in Living()){c.Attributes[Rasa.Data.Attributes.Health].Current=0; h.Creatures.HandleCreatureKill(h.Map,c,null);}
   h.SpawnWorldAfter(2000,117);Assert.IsEmpty(Living());
   h.SpawnWorldAfter(297000,117);Assert.IsEmpty(Living());
   h.SpawnWorldAfter(1000,117);Assert.IsTrue(Living().Length>=3);
  }
  static string[] Snapshot(OrdinaryLootTestDatabase db)=>db.Context.Database.SqlQueryRaw<string>("SELECT id||' '||respown_time AS Value FROM spawnpool ORDER BY id").ToArray();
 }
}
