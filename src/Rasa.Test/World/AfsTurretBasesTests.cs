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
    public class AfsTurretBasesTests
    {
        private const string Previous = "20270918000000_Solis_escort_staff";
        private const string Migration = "20270919000000_Afs_turret_bases";

        [TestMethod]
        public void TheManifestCoversEveryRaisedFenceGunOnceAndEachBaseIsUnderItsSpot()
        {
            var bases = AfsTurretBases.Bases.Select(b => b.AfsPool);
            var walls = AfsTurretBases.Walls.Select(w => w.Pool);
            CollectionAssert.AreEquivalent(AfsTurretMountHeights.Mounts.Select(m => m.Pool).ToArray(), bases.Concat(walls).ToArray());
            Assert.HasCount(30, AfsTurretBases.Bases);
            Assert.HasCount(54, AfsTurretBases.Walls);

            foreach (var b in AfsTurretBases.Bases)
            {
                var spot = ControlPointTurrets.Spots.Single(s => s.AfsPool == b.AfsPool);
                Assert.AreEqual((spot.BanePool, spot.Map, spot.AfsTurret, spot.BaneTurret), (b.BanePool, b.Map, b.AfsCreature, b.BaneCreature));
                Assert.AreEqual((spot.X, spot.Y, spot.Z), (b.SeedX, b.SeedY, b.SeedZ));
                var off = Math.Sqrt(Math.Pow(b.X - b.SeedX, 2) + Math.Pow(b.Z - b.SeedZ, 2));
                Assert.IsTrue(off > 1.4 && off < 2.2, $"{b.AfsPool}: the seed was {off:0.00} m off its base's centre");
                Assert.IsTrue(b.Y - b.SeedY > 0.27 && b.Y - b.SeedY < 0.32, $"{b.AfsPool}: the base's floor is just over the seed");
            }

            foreach (var w in AfsTurretBases.Walls)
            {
                var mount = AfsTurretMountHeights.Mounts.Single(m => m.Pool == w.Pool);
                Assert.AreEqual((mount.Map, mount.Creature, mount.X, mount.SurfaceY, mount.Z), (w.Map, w.Creature, w.X, w.Y, w.Z));
            }
        }

        [TestMethod]
        public void UpPutsTheGunsOnTheirBasesAndWallTopsAndDownPutsThemBack()
        {
            using var db = new OrdinaryLootTestDatabase();
            var migrator = db.Context.GetService<IMigrator>();
            migrator.Migrate(Previous);
            var before = db.Context.SpawnPoolEntries.AsNoTracking().ToDictionary(p => p.Id);
            migrator.Migrate(Migration);
            var after = db.Context.SpawnPoolEntries.AsNoTracking().ToDictionary(p => p.Id);

            Assert.AreEqual(before.Count, after.Count);
            var moved = after.Values.Where(p => Math.Abs(p.PosX - before[p.Id].PosX) > 0.0005 || Math.Abs(p.PosY - before[p.Id].PosY) > 0.0005
                || Math.Abs(p.PosZ - before[p.Id].PosZ) > 0.0005).Select(p => p.Id).ToHashSet();
            Assert.HasCount(114, moved, "30 AFS and 30 Bane pools on the bases, 54 AFS pools on the walls");

            foreach (var b in AfsTurretBases.Bases)
            {
                var afs = after[b.AfsPool];
                var bane = after[b.BanePool];
                Assert.AreEqual(b.X, afs.PosX, 0.001); Assert.AreEqual(b.Y, afs.PosY, 0.001); Assert.AreEqual(b.Z, afs.PosZ, 0.001);
                Assert.AreEqual((afs.PosX, afs.PosZ, afs.Rotation), (bane.PosX, bane.PosZ, bane.Rotation), "both sides' guns on one base");
                Assert.AreEqual(b.Y + AfsTurretBases.BaneOverBase, bane.PosY, 0.001);
                Assert.AreEqual(before[b.AfsPool].Rotation, afs.Rotation);
            }

            foreach (var w in AfsTurretBases.Walls)
            {
                Assert.AreEqual(w.Y, after[w.Pool].PosY, 0.001, $"{w.Pool} on its wall top");
                Assert.AreEqual(before[w.Pool].PosY - AfsTurretBases.RaisedBy, after[w.Pool].PosY, 0.001);
            }

            // The Landing Zone: the two gate guns on the bases' centres, the four rear ones on the wall.
            foreach (var (id, x, y, z) in new[] { (551121U, 158.112, 169.241, -27.012), (551123U, 237.988, 169.241, -27.112),
                (551125U, 145.74, 172.40, -79.10), (551127U, 254.34, 172.40, -75.23), (551129U, 144.70, 172.60, -127.19), (551131U, 254.74, 172.40, -110.69) })
            {
                Assert.AreEqual(x, after[id].PosX, 0.001, $"Landing Zone {id}");
                Assert.AreEqual(y, after[id].PosY, 0.001, $"Landing Zone {id}");
                Assert.AreEqual(z, after[id].PosZ, 0.001, $"Landing Zone {id}");
            }

            var once = Snapshot(db);
            foreach (var sql in AfsTurretBases.Up()) db.Context.Database.ExecuteSqlRaw(sql);
            CollectionAssert.AreEqual(once, Snapshot(db), "Up is idempotent.");

            migrator.Migrate(Previous);
            foreach (var pool in db.Context.SpawnPoolEntries.AsNoTracking())
            {
                var old = before[pool.Id];
                Assert.AreEqual((old.PosX, old.PosY, old.PosZ), (pool.PosX, pool.PosY, pool.PosZ), $"Rollback {pool.Id}");
            }
        }

        [TestMethod]
        public void PoolsMovedOrReplacedByHandStayPutBothWays()
        {
            using var db = new OrdinaryLootTestDatabase();
            var migrator = db.Context.GetService<IMigrator>();
            migrator.Migrate(Previous);
            db.Context.Database.ExecuteSqlRaw("UPDATE spawnpool SET pos_x=pos_x+1 WHERE id=551121;");
            db.Context.Database.ExecuteSqlRaw("UPDATE spawnpool SET pos_y=pos_y-1 WHERE id=551122;");
            db.Context.Database.ExecuteSqlRaw("UPDATE spawnpool SET creature_1_Id=550013 WHERE id=551125;");
            db.Context.Database.ExecuteSqlRaw("UPDATE spawnpool SET pos_y=172.4 WHERE id=551127;");
            var before = Snapshot(db);
            migrator.Migrate(Migration);
            var after = Snapshot(db);
            foreach (var id in new uint[] { 551121, 551122, 551125, 551127 })
                Assert.AreEqual(before.Single(p => p.StartsWith(id + " ")), after.Single(p => p.StartsWith(id + " ")), $"{id} untouched");
            Assert.AreNotEqual(before.Single(p => p.StartsWith("551123 ")), after.Single(p => p.StartsWith("551123 ")), "the others still move");

            migrator.Migrate(Previous);
            CollectionAssert.AreEqual(before, Snapshot(db));
        }

        [TestMethod]
        public void APoolMovedAfterUpgradeIsNotOverwrittenByRollback()
        {
            using var db = new OrdinaryLootTestDatabase();
            var migrator = db.Context.GetService<IMigrator>();
            migrator.Migrate(Migration);
            db.Context.Database.ExecuteSqlRaw("UPDATE spawnpool SET pos_y=180 WHERE id=551121;");
            migrator.Migrate(Previous);
            Assert.AreEqual(180.0, db.Context.SpawnPoolEntries.AsNoTracking().Single(p => p.Id == 551121).PosY);
        }

        [TestMethod]
        public void MySqlGeneratesTheSameGuardedMoves()
        {
            using var db = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrator = db.GetService<IMigrator>();
            var up = migrator.GenerateScript(Previous, Migration);
            var down = migrator.GenerateScript(Migration, Previous);
            foreach (var sql in AfsTurretBases.Up()) StringAssert.Contains(up, sql);
            foreach (var sql in AfsTurretBases.Down()) StringAssert.Contains(down, sql);
            StringAssert.Contains(up, "UPDATE spawnpool SET pos_x=158.112, pos_y=169.241, pos_z=-27.012 WHERE id=551121 ");
            StringAssert.Contains(up, $"'{Migration}'");
        }

        private static string[] Snapshot(OrdinaryLootTestDatabase db) => db.Context.Database.SqlQueryRaw<string>(
            "SELECT id || ' ' || printf('%.4f %.4f %.4f',pos_x,pos_y,pos_z) || ' ' || creature_1_Id AS Value FROM spawnpool ORDER BY id").ToArray();
    }
}
