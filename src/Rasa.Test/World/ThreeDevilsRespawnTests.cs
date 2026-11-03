using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Services.Preloader;
    using Rasa.Structures;
    using Rasa.Test.Database;
    using Rasa.Test.Missions.Wilderness;

    /// <summary>
    /// The report: on Herbal Remedy's Kill the Three Devils, Horntail and Archfiend were killed and
    /// Old Scratch was waited on for over an hour. Archfiend and Old Scratch now come back in 50 s, as
    /// Horntail does (Three_devils_respawn, ThreeDevilsRespawn).
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class ThreeDevilsRespawnTests
    {
        private const string Migration = "20271009000000_Three_devils_respawn";
        private const string Previous = "20271007000000_Marshes_logos_element";

        [TestMethod]
        public void AllThreeDevilsComeBackFiftySecondsAfterTheyDie()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pools = new uint[] { 580009, 580013, ThreeDevils.HorntailPool };
            harness.SpawnWorld(pools);

            foreach (var (pool, creature) in new[] { (580009u, 75u), (580013u, 80u), (ThreeDevils.HorntailPool, 79u) })
            {
                Assert.AreEqual(50_000L, harness.Map.SpawnPools.Single(p => p.DbId == pool).RespawnTime, $"pool {pool}");
                var devil = Living(harness, pool).Single();
                Assert.AreEqual(creature, devil.DbId);
                devil.Attributes[Attributes.Health].Current = 0;
                harness.Creatures.HandleCreatureKill(harness.Map, devil, null);
            }

            harness.SpawnWorldAfter(49_000, pools);
            foreach (var pool in pools)
                Assert.AreEqual(0, Living(harness, pool).Length, $"pool {pool} not before 50 s");

            harness.SpawnWorldAfter(1_000, pools);
            foreach (var pool in pools)
                Assert.AreEqual(1, Living(harness, pool).Length, $"pool {pool} back at 50 s");
        }

        [TestMethod]
        public void UpSetsTheTwoToHorntailsTimeAndDownPutsThemBack()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            Assert.AreEqual((5000L, 5000L, 500L), (Respawn(database, 580009), Respawn(database, 580013), Respawn(database, ThreeDevils.HorntailPool)));

            migrator.Migrate(Migration);
            Assert.AreEqual((500L, 500L, 500L), (Respawn(database, 580009), Respawn(database, 580013), Respawn(database, ThreeDevils.HorntailPool)));

            foreach (var sql in ThreeDevilsRespawn.Up())
                database.Context.Database.ExecuteSqlRaw(sql);
            Assert.AreEqual(500L, Respawn(database, 580013), "Up twice");

            migrator.Migrate(Previous);
            Assert.AreEqual((5000L, 5000L, 500L), (Respawn(database, 580009), Respawn(database, 580013), Respawn(database, ThreeDevils.HorntailPool)));
        }

        [TestMethod]
        public void ATimeSetByHandIsLeftAloneBothWays()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            database.Context.Database.ExecuteSqlRaw("UPDATE spawnpool SET respown_time = 1200 WHERE id = 580013");
            migrator.Migrate(Migration);
            Assert.AreEqual((500L, 1200L), (Respawn(database, 580009), Respawn(database, 580013)));

            database.Context.Database.ExecuteSqlRaw("UPDATE spawnpool SET respown_time = 300 WHERE id = 580009");
            migrator.Migrate(Previous);
            Assert.AreEqual((300L, 1200L), (Respawn(database, 580009), Respawn(database, 580013)));
        }

        [TestMethod]
        public void OnMySqlTheTimesChangeAndChangeBack()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(Previous, Migration);
            var down = migrator.GenerateScript(Migration, Previous);

            foreach (var sql in ThreeDevilsRespawn.Up())
                StringAssert.Contains(up, sql);
            StringAssert.Contains(up, $"'{Migration}'");
            foreach (var sql in ThreeDevilsRespawn.Down())
                StringAssert.Contains(down, sql);
        }

        private static Creature[] Living(WildernessRuntimeTestHarness harness, uint pool) =>
            EntityManager.Instance.Creatures.Values
                .Where(creature => creature.SpawnPool?.DbId == pool && CreatureManager.IsLivingOnMap(harness.Map, creature)).ToArray();

        private static long Respawn(OrdinaryLootTestDatabase database, uint pool) =>
            database.Context.Database.SqlQueryRaw<long>($"SELECT respown_time AS Value FROM spawnpool WHERE id = {pool}").Single();
    }
}
