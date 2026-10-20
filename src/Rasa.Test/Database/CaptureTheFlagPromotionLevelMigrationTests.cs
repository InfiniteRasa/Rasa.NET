using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Database
{
    using Rasa.Data;
    using Rasa.Services.Preloader;
    using Rasa.Structures.World;

    /// <summary>
    /// Capture the Flag's promotion at Corporal DeSimone pays 1750 experience, taking the recruit
    /// to level 2 (CaptureTheFlagPromotionLevel).
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class CaptureTheFlagPromotionLevelMigrationTests
    {
        private const string Migration = "20270117000000_Capture_the_flag_promotion_level";
        private const string Before = "20270116000000_Wilderness_reward_modules";

        [TestMethod]
        public void ThePromotionPays1750AndTheCompletionRewardIsUnchanged()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Before);
            Assert.AreEqual(500U, Reward(database, 1994, CaptureTheFlagPromotionLevel.RewardId).Experience);

            migrator.Migrate(Migration);
            Assert.AreEqual(1750U, Reward(database, 1994, CaptureTheFlagPromotionLevel.RewardId).Experience);
            Assert.AreEqual(0U, Reward(database, 1994, CaptureTheFlagPromotionLevel.RewardId).Credits);

            var completion = Reward(database, 1994, 1);
            Assert.AreEqual(5000U, completion.Experience);
            Assert.AreEqual(200U, completion.Credits);

            migrator.Migrate(Before);
            Assert.AreEqual(500U, Reward(database, 1994, CaptureTheFlagPromotionLevel.RewardId).Experience);
        }

        /// <summary>
        /// What the recruit has from Initiation and Gearing Up, the missions before Capture the
        /// Flag, and the promotion on top of it, is level 2; with the 500 it paid, it was not.
        /// </summary>
        [TestMethod]
        public void WithWhatTheMissionsBeforePaidThePromotionIsLevelTwo()
        {
            using var database = new OrdinaryLootTestDatabase();
            database.Context.GetService<IMigrator>().Migrate(Migration);

            var earned = Reward(database, 1990, 1).Experience + Reward(database, 1992, 1).Experience;
            var levelTwo = (uint)ExpPerLevel.ExpRequred[1];

            Assert.AreEqual(1350U, earned, "Initiation's 100 and Gearing Up's 1250");
            Assert.IsGreaterThanOrEqualTo(levelTwo, earned + Reward(database, 1994, CaptureTheFlagPromotionLevel.RewardId).Experience, "level 2 with the promotion");
            Assert.IsLessThan(levelTwo, earned + CaptureTheFlagPromotionLevel.ExperienceWas, "and it was not with 500");
            Assert.IsLessThan((uint)ExpPerLevel.ExpRequred[2], earned + CaptureTheFlagPromotionLevel.Experience, "level 2, not 3");
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
            Assert.AreEqual(1234U, Reward(database, 1994, CaptureTheFlagPromotionLevel.RewardId).Experience);

            migrator.Migrate(Before);
            Assert.AreEqual(1234U, Reward(database, 1994, CaptureTheFlagPromotionLevel.RewardId).Experience);
        }

        [TestMethod]
        public void OnMySqlTheRewardGoesInAndComesOut()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();

            StringAssert.Contains(migrator.GenerateScript(Before, Migration),
                "UPDATE mission_reward_definition SET experience = 1750 WHERE mission_id = 1994 AND content_revision = 'deployment_11' AND reward_id = 59 AND experience = 500;");
            StringAssert.Contains(migrator.GenerateScript(Migration, Before),
                "UPDATE mission_reward_definition SET experience = 500 WHERE mission_id = 1994 AND content_revision = 'deployment_11' AND reward_id = 59 AND experience = 1750;");
        }

        private static MissionRewardDefinitionEntry Reward(OrdinaryLootTestDatabase database, uint missionId, uint rewardId) =>
            database.Context.Set<MissionRewardDefinitionEntry>().AsNoTracking()
                .Single(row => row.MissionId == missionId && row.RewardId == rewardId);
    }
}
