using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Services.Preloader;
using Rasa.Test.Database;

namespace Rasa.Test.Missions.Wilderness
{
    /// <summary>Orders from High Command's public encounter frees Skeev when no run holds him (Skeev_free_when_unreserved, SkeevFreeWhenUnreserved).</summary>
    [TestClass]
    [DoNotParallelize]
    public class SkeevFreeWhenUnreservedTests
    {
        private const string Migration = "20270930000000_Skeev_free_when_unreserved";
        private const string Previous = "20270928000000_Minos_warnet_nests_hung";

        [TestMethod]
        public void TheBindingFreesHimAndDownHoldsHimAgain()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            var before = Bindings(database);
            StringAssert.Contains(before, "\"manualCombat\":true");
            Assert.IsFalse(before.Contains("freeWhenUnreserved"));

            migrator.Migrate(Migration);
            var after = Bindings(database);
            StringAssert.Contains(after, "\"manualCombat\":true,\"freeWhenUnreserved\":true");
            Assert.AreEqual(before.Length + ",\"freeWhenUnreserved\":true".Length, after.Length, "nothing else changes");

            foreach (var sql in SkeevFreeWhenUnreserved.Up())
                Assert.AreEqual(0, database.Context.Database.ExecuteSqlRaw(sql.Replace("{", "{{").Replace("}", "}}")), "twice");

            migrator.Migrate(Previous);
            Assert.AreEqual(before, Bindings(database), "Down");
        }

        [TestMethod]
        public void OnMySqlTheBindingChangesAndChangesBack()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();

            foreach (var sql in SkeevFreeWhenUnreserved.Up())
                StringAssert.Contains(migrator.GenerateScript(Previous, Migration), sql);
            foreach (var sql in SkeevFreeWhenUnreserved.Down())
                StringAssert.Contains(migrator.GenerateScript(Migration, Previous), sql);
        }

        private static string Bindings(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    $"SELECT bindings AS Value FROM mission_scene_binding WHERE mission_id = {SkeevFreeWhenUnreserved.MissionId} AND script_key = '{SkeevFreeWhenUnreserved.ScriptKey}'")
                .AsEnumerable().Single();
    }
}
