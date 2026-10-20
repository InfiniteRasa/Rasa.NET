using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Services.Preloader.Missions.Wilderness;
using Rasa.Structures;

namespace Rasa.Test.Missions.Wilderness
{
    using Rasa.Test.Database;
    using static WildernessAliaBranchesTests;

    /// <summary>
    /// Lurking In The Shadows (427), "Kill Proctor Fulgor": a map pin and radar arrow on Fulgor's
    /// spawn (Wilderness_fulgor_indicator, WildernessFulgorIndicator).
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class WildernessFulgorIndicatorTests
    {
        private const string Before = "20270125000000_Tool_templates_no_alt_fire";
        private const string Migration = "20270126000000_Wilderness_fulgor_indicator";

        [TestMethod]
        public void OnceOliverSendsThePlayerAfterFulgorTheObjectiveHasAPinOnHim()
        {
            using var harness = CreateHarness();
            harness.SpawnWorld(210, 180, 580010);
            SeedCompletedHistory(harness, 1407, 1069, 479);
            var npcs = new NpcManager(harness, harness.Manager);
            Accept(harness, npcs, 210, 427);

            // Once Oliver has sent the player after Fulgor, the objective is active: what the
            // client is told of the mission's objectives, whose pins its map and radar draw.
            CompleteObjective(harness, npcs, 180, 427, 1);
            Assert.AreEqual(MissionObjectiveState.Incomplete, harness.Client.Player.Missions[427].Objectives[6].State);

            var objectives = harness.Manager.BuildStatusSnapshot(harness.Client.Player)[427].ObjectivesList
                .ToDictionary(objective => objective.ObjectiveId);
            Assert.IsEmpty(objectives[1].IndicatorList, "Speak to Oliver: none, as before");

            var pin = objectives[6].IndicatorList.Single();
            var spawn = harness.Map.SpawnPools.Single(pool => pool.DbId == WildernessFulgorIndicator.FulgorSpawn);
            Assert.IsLessThan(0.01f, Vector3.Distance(pin.Position, spawn.Position), "on Fulgor's spawn");
            Assert.AreEqual(WildernessFulgorIndicator.Radius, pin.Radius);
            Assert.IsNull(pin.ClientNameId, "named by its objective, 'Kill Proctor Fulgor.'");
            Assert.IsFalse(pin.Show3DEffect);
        }

        [TestMethod]
        public void ThePinStandsOnTheSpawnTheUpRunsTwiceAndTheDownTakesItOut()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Before);
            Assert.AreEqual(0L, Rows(database));

            migrator.Migrate(Migration);
            Assert.AreEqual(1L, Rows(database));
            Assert.AreEqual(
                Single(database, "SELECT printf('%.3f %.3f %.3f', pos_x, pos_y, pos_z) AS Value FROM spawnpool WHERE id = 580010"),
                Single(database, "SELECT printf('%.3f %.3f %.3f', pos_x, pos_y, pos_z) AS Value FROM mission_indicator " +
                    $"WHERE mission_id = 427 AND objective_id = 6 AND indicator_id = {WildernessFulgorIndicator.IndicatorId}"));

            foreach (var sql in WildernessFulgorIndicator.Up())
                database.Context.Database.ExecuteSqlRaw(sql);
            Assert.AreEqual(1L, Rows(database), "twice");

            migrator.Migrate(Before);
            Assert.AreEqual(0L, Rows(database));
        }

        [TestMethod]
        public void OnMySqlTheRowGoesInAndComesOut()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(Before, Migration);
            var down = migrator.GenerateScript(Migration, Before);

            foreach (var sql in WildernessFulgorIndicator.Up())
                StringAssert.Contains(up, sql);
            StringAssert.Contains(up, $"'{Migration}'");
            foreach (var sql in WildernessFulgorIndicator.Down())
                StringAssert.Contains(down, sql);
        }

        private static long Rows(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<long>(
                "SELECT COUNT(*) AS Value FROM mission_indicator WHERE mission_id = 427 AND objective_id = 6").AsEnumerable().Single();

        private static string Single(OrdinaryLootTestDatabase database, string sql) =>
            database.Context.Database.SqlQueryRaw<string>(sql).AsEnumerable().Single();
    }
}
