using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Database
{
    using Rasa.Services.Preloader;
    using Rasa.Structures.World;

    /// <summary>Capture the Flag's promotion at Corporal DeSimone pays 500 experience (CaptureTheFlagPromotion).</summary>
    [TestClass]
    [DoNotParallelize]
    public class CaptureTheFlagPromotionMigrationTests
    {
        private const string Migration = "20270109000000_Capture_the_flag_promotion_xp";
        private const string Before = "20270108000000_Meadow_boargar";

        [TestMethod]
        public void ThePromotionPays500AndTheCompletionRewardIsUnchanged()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Before);
            Assert.AreEqual(43000U, Reward(database, CaptureTheFlagPromotion.RewardId).Experience);

            migrator.Migrate(Migration);
            Assert.AreEqual(500U, Reward(database, CaptureTheFlagPromotion.RewardId).Experience);
            Assert.AreEqual(0U, Reward(database, CaptureTheFlagPromotion.RewardId).Credits);

            var completion = Reward(database, 1);
            Assert.AreEqual(5000U, completion.Experience);
            Assert.AreEqual(200U, completion.Credits);

            migrator.Migrate(Before);
            Assert.AreEqual(43000U, Reward(database, CaptureTheFlagPromotion.RewardId).Experience);
        }

        [TestMethod]
        public void APromotionAGmHasChangedIsLeftAsTheyMadeIt()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Before);
            database.Context.Database.ExecuteSqlRaw(
                "UPDATE mission_reward_definition SET experience = 1234 WHERE mission_id = 1994 AND reward_id = 59");

            migrator.Migrate(Migration);
            Assert.AreEqual(1234U, Reward(database, CaptureTheFlagPromotion.RewardId).Experience);

            migrator.Migrate(Before);
            Assert.AreEqual(1234U, Reward(database, CaptureTheFlagPromotion.RewardId).Experience);
        }

        [TestMethod]
        public void OnMySqlTheRewardGoesInAndComesOut()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();

            StringAssert.Contains(migrator.GenerateScript(Before, Migration),
                "UPDATE mission_reward_definition SET experience = 500 WHERE mission_id = 1994 AND content_revision = 'deployment_11' AND reward_id = 59 AND experience = 43000;");
            StringAssert.Contains(migrator.GenerateScript(Migration, Before),
                "UPDATE mission_reward_definition SET experience = 43000 WHERE mission_id = 1994 AND content_revision = 'deployment_11' AND reward_id = 59 AND experience = 500;");
        }

        private static MissionRewardDefinitionEntry Reward(OrdinaryLootTestDatabase database, uint rewardId) =>
            database.Context.Set<MissionRewardDefinitionEntry>().AsNoTracking()
                .Single(row => row.MissionId == CaptureTheFlagPromotion.MissionId && row.RewardId == rewardId);
    }
}
