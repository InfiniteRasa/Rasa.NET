using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Services.Preloader;
    using Rasa.Test.Database;

    /// <summary>The AFS turrets turned to face as their mounts do (Turret_facings, TurretFacings).</summary>
    [TestClass]
    [DoNotParallelize]
    public class TurretFacingsTests
    {
        private const string Migration = "20270103000000_Turret_facings";
        private const string Previous = "20270102000000_Wilderness_post_npcs";

        [TestMethod]
        public void EachTurretFacesItsMountsWayWhichTheOldReadingHadAsPiLessIt()
        {
            Assert.HasCount(98, TurretFacings.Turrets);
            Assert.AreEqual(98, TurretFacings.Turrets.Select(t => t.Pool).Distinct().Count());

            // Turned elsewhere (550082-550084), or on a mount the map tips over: not here.
            foreach (var pool in new uint[] { 550082, 550083, 550084, 550037, 550038, 550081, 550120, 550127 })
                Assert.IsFalse(TurretFacings.Turrets.Any(t => t.Pool == pool), $"pool {pool}");

            foreach (var turret in TurretFacings.Turrets)
            {
                Assert.AreEqual(0, Wrap(Math.PI - turret.Was - turret.Is), 0.002, $"pool {turret.Pool}: (x, y, z, w) read pi less the facing");
                Assert.IsTrue(Math.Abs(Wrap(turret.Is - turret.Was)) > 0.002, $"pool {turret.Pool} turns");
            }

            using var database = new OrdinaryLootTestDatabase();
            database.Context.GetService<IMigrator>().Migrate();

            foreach (var turret in TurretFacings.Turrets)
                Assert.AreEqual(turret.Is, Rotation(database, turret.Pool), 1e-6, $"pool {turret.Pool}");
        }

        [TestMethod]
        public void TheUpLeavesWhatAGmTurnedAndTheDownTurnsBackWhatItTurned()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();
            var first = TurretFacings.Turrets[0];
            var second = TurretFacings.Turrets[1];

            migrator.Migrate(Previous);
            database.Context.Database.ExecuteSqlRaw($"UPDATE spawnpool SET rotation = 1.25 WHERE id = {first.Pool}");
            var before = Snapshot(database);

            migrator.Migrate(Migration);
            Assert.AreEqual(1.25, Rotation(database, first.Pool), 1e-9, "the GM's facing kept");
            Assert.AreEqual(second.Is, Rotation(database, second.Pool), 1e-6);

            var once = Snapshot(database);
            foreach (var sql in TurretFacings.Up())
                database.Context.Database.ExecuteSqlRaw(sql);
            CollectionAssert.AreEqual(once, Snapshot(database), "twice");

            migrator.Migrate(Previous);
            CollectionAssert.AreEqual(before, Snapshot(database), "Down turns back what it turned");
        }

        [TestMethod]
        public void OnMySqlTheTurnsGoInAndComeOut()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(Previous, Migration);
            var down = migrator.GenerateScript(Migration, Previous);

            foreach (var sql in TurretFacings.Up())
                StringAssert.Contains(up, sql);
            StringAssert.Contains(up, $"'{Migration}'");
            foreach (var sql in TurretFacings.Down())
                StringAssert.Contains(down, sql);
        }

        private static double Wrap(double angle)
        {
            while (angle > Math.PI) angle -= 2 * Math.PI;
            while (angle <= -Math.PI) angle += 2 * Math.PI;
            return angle;
        }

        private static double Rotation(OrdinaryLootTestDatabase database, uint pool) =>
            database.Context.Database.SqlQueryRaw<double>($"SELECT rotation AS Value FROM spawnpool WHERE id = {pool}").AsEnumerable().Single();

        private static List<string> Snapshot(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    $"SELECT id || ' ' || printf('%.6f', rotation) AS Value FROM spawnpool WHERE id IN ({string.Join(", ", TurretFacings.Turrets.Select(t => t.Pool))}) ORDER BY id")
                .AsEnumerable().ToList();
    }
}
