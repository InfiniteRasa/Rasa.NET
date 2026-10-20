using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions
{
    using Rasa.Context.World;
    using Rasa.Missions.Content;
    using Rasa.Missions.Scenes;
    using Rasa.Services.Preloader.Missions;
    using Rasa.Test.Database;

    /// <summary>
    /// Capture the Flag's scene and the bootcamp experience with Youngblood's squad and the
    /// cave-in's view radius both (BootcampCaveInFortSquadV11, Bootcamp_cave_in_with_fort_squad),
    /// whichever of the two migrations that each wrote them whole a database ran second.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class BootcampCaveInFortSquadTests
    {
        /// <summary>The last of the migrations sharing 20270109000000: every one of them run.</summary>
        private const string Before = "20270109000000_Youngblood_fort_squad";
        private const string Migration = "20270110000000_Bootcamp_cave_in_with_fort_squad";

        [TestMethod]
        public void BothScenesAreTheSquadsWithTheRocksRadius()
        {
            var capture = BootcampCaveInFortSquadV11.CaptureTheFlag();
            var experience = BootcampCaveInFortSquadV11.Experience();

            Assert.AreEqual(BootcampCaveInLandmarkV10.CaveInViewRadius, capture.Actors[BootcampCaveInBreachV9.CaveIn].ViewRadius);
            Assert.AreEqual(BootcampCaveInLandmarkV10.CaveInViewRadius, experience.Scene.Actors[BootcampCaveInBreachV9.CaveIn].ViewRadius);

            // Otherwise the squad's, to the letter.
            var squadCapture = BootcampFortSquadV10.CaptureTheFlag();
            var squadExperience = BootcampFortSquadV10.Experience();
            squadCapture.Actors[BootcampCaveInBreachV9.CaveIn] = BootcampCaveInLandmarkV10.CaveInActor();
            squadExperience.Scene.Actors[BootcampCaveInBreachV9.CaveIn] = BootcampCaveInLandmarkV10.CaveInActor();
            Assert.AreEqual(Json(squadCapture), Json(capture));
            Assert.AreEqual(Json(squadExperience), Json(experience));
        }

        /// <summary>
        /// Fresh, the squad's migration runs after the cave-in's and the radius was lost; on a
        /// database that had the squad before the cave-in's migration came, the squad was. Either
        /// way this puts both, and Down leaves the squad's.
        /// </summary>
        [TestMethod]
        public void WhicheverRanSecondTheMigrationPutsBothAndDownLeavesTheSquad()
        {
            var directory = Path.Combine(AppContext.BaseDirectory, "TestDatabases", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);

            try
            {
                var database = Path.Combine(directory, "world");

                using (var context = PersistenceIntegrationTests.CreateContext(typeof(SqliteWorldContext), database))
                    MigratedDatabaseTemplates.Migrate(context, () => context.Database.Migrate());

                using var world = (WorldContext)PersistenceIntegrationTests.CreateContext(typeof(SqliteWorldContext), database);
                var migrator = world.GetService<IMigrator>();

                // Fresh: the squad, no radius.
                migrator.Migrate(Before);
                AssertBindings(world, squad: true, radius: false);
                migrator.Migrate(Migration);
                AssertBindings(world, squad: true, radius: true);

                // The squad's first, the cave-in's after: the radius, no squad.
                migrator.Migrate(Before);
                Execute(world, BootcampFortSquadV10.SceneSql(BootcampCaveInLandmarkV10.CaptureTheFlag()));
                Execute(world, BootcampFortSquadV10.ExperienceSql(BootcampCaveInLandmarkV10.Experience()));
                AssertBindings(world, squad: false, radius: true);
                migrator.Migrate(Migration);
                AssertBindings(world, squad: true, radius: true);

                migrator.Migrate(Before);
                AssertBindings(world, squad: true, radius: false);
            }
            finally
            {
                try { Directory.Delete(directory, true); } catch (IOException) { }
            }
        }

        [TestMethod]
        public void TheMigrationWritesBothBindingsOnMySql()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(Before, Migration);

            StringAssert.Contains(up, "mission_scene_binding");
            StringAssert.Contains(up, "mission_experience_binding");
            StringAssert.Contains(up, "\"fort-quiet-while-held\"");
            StringAssert.Contains(up, "\"viewRadius\":150");
        }

        private static void AssertBindings(WorldContext world, bool squad, bool radius)
        {
            var capture = JsonSerializer.Deserialize<MissionSceneDefinition>(Read(world,
                "SELECT bindings AS Value FROM mission_scene_binding WHERE mission_id = 1994"), MissionContentCodec.Options);
            var experience = JsonSerializer.Deserialize<MissionExperienceDefinition>(Read(world,
                "SELECT bindings AS Value FROM mission_experience_binding WHERE experience_key = 'bootcamp'"), MissionContentCodec.Options);

            foreach (var scene in new[] { capture, experience.Scene })
            {
                Assert.AreEqual(squad, BootcampFortSquadV10.Squad.All(soldier => scene.Actors.ContainsKey(soldier.Role)), "the squad");
                Assert.AreEqual(radius ? BootcampCaveInLandmarkV10.CaveInViewRadius : 0f, scene.Actors[BootcampCaveInBreachV9.CaveIn].ViewRadius, "the rocks' radius");
            }

            Assert.AreEqual(squad, capture.Sequences.Values.Any(sequence => sequence.World?.OfType<QuietSpawnPoolsIntent>().Any() == true), "the quiet fort");
            Assert.AreEqual(squad, experience.ActorPolicies.ContainsKey(BootcampFortSquadV10.RiflemanTemplate), "the squad's posts");
        }

        /// <summary>As the migration ran it: the JSON's braces are not format items.</summary>
        private static void Execute(WorldContext world, string sql)
        {
            var connection = world.Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open)
                connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            Assert.AreEqual(1, command.ExecuteNonQuery());
        }

        private static string Read(WorldContext world, string sql) =>
            world.Database.SqlQueryRaw<string>(sql).AsEnumerable().Single();

        private static string Json<T>(T document) => JsonSerializer.Serialize(document, MissionContentCodec.Options);
    }
}
