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
    public class LightningFieldsWreckTurretTests
    {
        private const string Previous = "20270920000000_Landing_zone_thrax_weapons";
        private const string Migration = "20270921000000_Lightning_fields_wreck_turret";

        [TestMethod]
        public void UpTakesTheTwoGunsOffTheWreckAndDownPutsThemBackAsTheyWere()
        {
            using var db = new OrdinaryLootTestDatabase();
            var migrator = db.Context.GetService<IMigrator>();
            migrator.Migrate(Previous);
            var pools = Pools(db);
            var links = Links(db);
            Assert.AreEqual(2, pools.Count(p => p.StartsWith("551245 ") || p.StartsWith("551246 ")));
            Assert.AreEqual(12, TurretLinks(links), "Lightning Fields: six turret spots, two pools each");

            migrator.Migrate(Migration);
            CollectionAssert.AreEqual(pools.Where(p => !p.StartsWith("551245 ") && !p.StartsWith("551246 ")).ToArray(), Pools(db));
            CollectionAssert.AreEqual(links.Where(l => !l.EndsWith(" 551245") && !l.EndsWith(" 551246")).ToArray(), Links(db));
            Assert.AreEqual(10, TurretLinks(Links(db)), "Lightning Fields keeps its other five turret spots");

            var once = (Pools(db), Links(db));
            foreach (var sql in LightningFieldsWreckTurret.Up()) db.Context.Database.ExecuteSqlRaw(sql);
            CollectionAssert.AreEqual(once.Item1, Pools(db), "Up is idempotent");
            CollectionAssert.AreEqual(once.Item2, Links(db));

            migrator.Migrate(Previous);
            CollectionAssert.AreEqual(pools, Pools(db));
            CollectionAssert.AreEqual(links, Links(db));
        }

        [TestMethod]
        public void APoolMovedByHandIsKeptAndNotPutBackTwice()
        {
            using var db = new OrdinaryLootTestDatabase();
            var migrator = db.Context.GetService<IMigrator>();
            migrator.Migrate(Previous);
            db.Context.Database.ExecuteSqlRaw("UPDATE spawnpool SET pos_x=-176 WHERE id=551245;");
            var pools = Pools(db);
            var links = Links(db);

            migrator.Migrate(Migration);
            Assert.IsTrue(Pools(db).Any(p => p.StartsWith("551245 ")), "a moved gun is the owner's to keep");
            Assert.IsFalse(Pools(db).Any(p => p.StartsWith("551246 ")));
            Assert.IsTrue(Links(db).Contains("23 2 551245"));

            migrator.Migrate(Previous);
            CollectionAssert.AreEqual(pools, Pools(db));
            CollectionAssert.AreEqual(links, Links(db));
        }

        [TestMethod]
        public void MySqlGeneratesTheSameGuardedStatements()
        {
            using var db = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrator = db.GetService<IMigrator>();
            var up = migrator.GenerateScript(Previous, Migration);
            var down = migrator.GenerateScript(Migration, Previous);
            foreach (var sql in LightningFieldsWreckTurret.Up()) StringAssert.Contains(up, sql);
            foreach (var sql in LightningFieldsWreckTurret.Down()) StringAssert.Contains(down, sql);
            StringAssert.Contains(down, "SELECT 551245, 0, 0, 3000, -184.0, 438.86, 388.0, 0.36, 1764, 550011, 1, 1, ");
            StringAssert.Contains(up, $"'{Migration}'");
        }

        private static int TurretLinks(string[] links) => links.Count(l => l.StartsWith("23 ") &&
            uint.Parse(l.Split(' ')[2]) is >= ControlPointTurrets.PoolIdMin and <= ControlPointTurrets.PoolIdMax);

        private static string[] Pools(OrdinaryLootTestDatabase db) => db.Context.Database.SqlQueryRaw<string>(
            "SELECT id || ' ' || mode || ' ' || anim_type || ' ' || respown_time || ' ' || printf('%.4f %.4f %.4f %.4f', pos_x, pos_y, pos_z, rotation) || ' ' || " +
            "map_context_id || ' ' || creature_1_Id || ' ' || creature_1_min_count || ' ' || creature_1_max_count || ' ' || creature_2_Id || ' ' || radius AS Value " +
            "FROM spawnpool ORDER BY id").ToArray();

        private static string[] Links(OrdinaryLootTestDatabase db) => db.Context.Database.SqlQueryRaw<string>(
            "SELECT control_point_id || ' ' || kind || ' ' || object_id AS Value FROM control_point_link ORDER BY control_point_id, kind, object_id").ToArray();
    }
}
