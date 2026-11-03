using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Missions.Scenes;
using Rasa.Test.Database;

namespace Rasa.Test.Missions
{
    using K = Rasa.Services.Preloader.TorcastraKeymasterComputer;
    using M = Rasa.Services.Preloader.Missions.Torcastra.TorcastraMissionsV1;

    /// <summary>
    /// Keymaster of Torcastra's computer banks and their marker moved from behind the pipe to the
    /// alcove's floor in front of it, and back (Torcastra_keymaster_computer).
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class TorcastraKeymasterComputerTests
    {
        private const string Migration = "20271003000000_Torcastra_keymaster_computer";
        private const string Previous = "20271002000000_Fence_turret_bases";

        [TestMethod]
        public void TheBanksAndTheirMarkerMoveAndDownPutsThemBack()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            var before = Binding(database);
            StringAssert.Contains(before, K.ActorJson(K.Was), "where Torcastra_prison put them");
            AssertMarker(database, K.Was);

            migrator.Migrate(Migration);
            var after = Binding(database);
            StringAssert.Contains(after, K.ActorJson(K.Now));
            Assert.IsFalse(after.Contains(K.ActorJson(K.Was)), "not behind the pipe any more");
            Assert.AreEqual(before.Replace(K.ActorJson(K.Was), K.ActorJson(K.Now)), after, "nothing else of the binding changes");
            AssertMarker(database, K.Now);

            // Twice is once.
            foreach (var sql in K.Up())
                database.Context.Database.ExecuteSqlRaw(sql.Replace("{", "{{").Replace("}", "}}"));
            Assert.AreEqual(after, Binding(database));
            AssertMarker(database, K.Now);

            migrator.Migrate(Previous);
            Assert.AreEqual(before, Binding(database), "Down puts the binding back");
            AssertMarker(database, K.Was);
        }

        [TestMethod]
        public void OnMySqlTheyMoveAndMoveBack()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(Previous, Migration);
            var down = migrator.GenerateScript(Migration, Previous);

            foreach (var sql in K.Up())
                StringAssert.Contains(up, sql);
            StringAssert.Contains(up, $"'{Migration}'");
            foreach (var sql in K.Down())
                StringAssert.Contains(down, sql);
        }

        private static string Binding(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    $"SELECT bindings AS Value FROM mission_scene_binding WHERE mission_id = {M.Keymaster} AND content_revision = '{M.Revision}'")
                .ToArray().Single();

        private static void AssertMarker(OrdinaryLootTestDatabase database, ScenePosition at)
        {
            var marker = database.Context.MissionIndicatorEntries.AsNoTracking()
                .Single(row => row.MissionId == M.Keymaster && row.ContentRevision == M.Revision && row.ObjectiveId == 4);
            Assert.AreEqual(at.X, marker.PosX, 0.01);
            Assert.AreEqual(at.Y, marker.PosY, 0.01);
            Assert.AreEqual(at.Z, marker.PosZ, 0.01);
        }
    }
}
