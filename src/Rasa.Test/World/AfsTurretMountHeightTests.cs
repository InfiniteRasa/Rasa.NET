using System;
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
    public class AfsTurretMountHeightTests
    {
        private const string Previous = "20270913000000_Landing_zone_hunter_weapon";
        private const string Migration = "20270914000000_Afs_turret_mount_heights";

        [TestMethod]
        public void ForwardUpgradeRaisesOnlyStandardAfsFenceMountsAndRollbackRestoresThem()
        {
            using var db = new OrdinaryLootTestDatabase();
            var migrator = db.Context.GetService<IMigrator>();
            migrator.Migrate(Previous);
            var before = db.Context.SpawnPoolEntries.AsNoTracking().ToDictionary(p => p.Id);
            migrator.Migrate(Migration);
            var after = db.Context.SpawnPoolEntries.AsNoTracking().ToDictionary(p => p.Id);
            var changed = after.Values.Where(p => Math.Abs(p.PosY - before[p.Id].PosY) > 0.001).ToArray();
            Assert.AreEqual(84, changed.Length);
            Assert.AreEqual(15, ControlPointTurrets.Spots.Where(s => changed.Any(p => p.Id == s.AfsPool)).Select(s => s.Point).Distinct().Count());
            Assert.AreEqual(6, changed.Count(p => p.MapContextId == 1220));
            foreach (var pool in changed)
            {
                Assert.AreEqual(before[pool.Id].PosY + 2.56, pool.PosY, 0.001);
                Assert.AreEqual(4064U, db.Context.CreatureEntries.AsNoTracking().Single(c => c.Id == pool.Creature1Id).ClassId);
            }
            foreach (var pool in after.Values)
            {
                var old = before[pool.Id];
                Assert.AreEqual((old.PosX, old.PosZ, old.Rotation, old.Mode, old.RespawnTime, old.Creature1Id),
                    (pool.PosX, pool.PosZ, pool.Rotation, pool.Mode, pool.RespawnTime, pool.Creature1Id), $"Pool {pool.Id}");
            }
            foreach (var (id, y) in new[] { (551121U, 171.52), (551123U, 171.50), (551125U, 174.96),
                (551127U, 174.96), (551129U, 175.16), (551131U, 174.96) })
                Assert.AreEqual(y, after[id].PosY, 0.001, $"Landing Zone {id}");
            Assert.AreEqual(before.Count, after.Count);
            var once = Snapshot(db);
            foreach (var sql in AfsTurretMountHeights.Up()) db.Context.Database.ExecuteSqlRaw(sql);
            CollectionAssert.AreEqual(once, Snapshot(db), "Up is idempotent.");
            migrator.Migrate(Previous);
            foreach (var pool in db.Context.SpawnPoolEntries.AsNoTracking())
                Assert.AreEqual(before[pool.Id].PosY, pool.PosY, 0.001, $"Rollback {pool.Id}");
        }

        [TestMethod]
        public void CustomPositionsAndReplacedCreaturesSurviveBothDirections()
        {
            using var db = new OrdinaryLootTestDatabase();
            var migrator = db.Context.GetService<IMigrator>();
            migrator.Migrate(Previous);
            db.Context.Database.ExecuteSqlRaw("UPDATE spawnpool SET pos_y=pos_y+1 WHERE id=551121;");
            db.Context.Database.ExecuteSqlRaw("UPDATE spawnpool SET pos_x=pos_x+1 WHERE id=551123;");
            db.Context.Database.ExecuteSqlRaw("UPDATE spawnpool SET pos_z=pos_z+1 WHERE id=551125;");
            db.Context.Database.ExecuteSqlRaw("UPDATE spawnpool SET creature_1_Id=550013 WHERE id=551127;");
            db.Context.Database.ExecuteSqlRaw("UPDATE spawnpool SET pos_y=175.16 WHERE id=551129;");
            var before = Snapshot(db);
            migrator.Migrate(Migration);
            foreach (var id in new uint[] { 551121, 551123, 551125, 551127, 551129 })
                Assert.AreEqual(before.Single(p => p.StartsWith(id + " ")), Snapshot(db).Single(p => p.StartsWith(id + " ")));
            migrator.Migrate(Previous);
            CollectionAssert.AreEqual(before, Snapshot(db));
        }

        [TestMethod]
        public void AHeightChangedAfterUpgradeIsNotOverwrittenByRollback()
        {
            using var db = new OrdinaryLootTestDatabase();
            var migrator = db.Context.GetService<IMigrator>();
            migrator.Migrate(Migration);
            db.Context.Database.ExecuteSqlRaw("UPDATE spawnpool SET pos_y=180 WHERE id=551121;");
            migrator.Migrate(Previous);
            Assert.AreEqual(180.0, db.Context.SpawnPoolEntries.AsNoTracking().Single(p => p.Id == 551121).PosY);
        }

        [TestMethod]
        public void MySqlGeneratesTheSameGuardedHeightUpdates()
        {
            using var db = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrator = db.GetService<IMigrator>();
            var up = migrator.GenerateScript(Previous, Migration);
            var down = migrator.GenerateScript(Migration, Previous);
            foreach (var sql in AfsTurretMountHeights.Up()) StringAssert.Contains(up, sql);
            foreach (var sql in AfsTurretMountHeights.Down()) StringAssert.Contains(down, sql);
        }

        private static string[] Snapshot(OrdinaryLootTestDatabase db) => db.Context.Database.SqlQueryRaw<string>(
            "SELECT id || ' ' || printf('%.4f %.4f %.4f',pos_x,pos_y,pos_z) || ' ' || creature_1_Id AS Value FROM spawnpool ORDER BY id").ToArray();
    }
}