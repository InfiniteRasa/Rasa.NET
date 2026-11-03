using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Services.Preloader;
using Rasa.Test.Database;

namespace Rasa.Test.Missions
{
    /// <summary>The Reason Why's scene counts a known Logos (Donn_known_logos, DonnKnownLogos).</summary>
    [TestClass]
    [DoNotParallelize]
    public class DonnKnownLogosTests
    {
        private const string Migration = "20270924000000_Donn_known_logos";
        private const string Previous = "20270923000000_Daghdas_urn_building_regions";

        [TestMethod]
        public void TheSceneGainsTheThreeLogosObjectivesAndDownTakesThemOff()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            var before = Bindings(database);
            StringAssert.DoesNotMatch(before, new System.Text.RegularExpressions.Regex("existingFactObjectiveIds", System.Text.RegularExpressions.RegexOptions.IgnoreCase));

            migrator.Migrate(Migration);
            var after = Bindings(database);
            Assert.AreNotEqual(before, after);
            StringAssert.Contains(after.Replace(" ", ""), "[11,12,13]");

            foreach (var sql in DonnKnownLogos.Up())
                Assert.AreEqual(0, database.Context.Database.ExecuteSqlRaw(sql.Replace("{", "{{").Replace("}", "}}")), "twice");

            migrator.Migrate(Previous);
            Assert.AreEqual(before, Bindings(database), "Down");
        }

        [TestMethod]
        public void ASceneChangedSinceIsLeftAlone()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            database.Context.Database.ExecuteSqlRaw(
                $"UPDATE mission_scene_binding SET bindings = REPLACE(bindings, 'data.sequence', 'data.other') WHERE mission_id = {DonnKnownLogos.MissionId}");
            var edited = Bindings(database);

            migrator.Migrate(Migration);
            Assert.AreEqual(edited, Bindings(database));
        }

        [TestMethod]
        public void OnMySqlTheSceneChangesAndChangesBack()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();

            foreach (var sql in DonnKnownLogos.Up())
                StringAssert.Contains(migrator.GenerateScript(Previous, Migration), sql);
            foreach (var sql in DonnKnownLogos.Down())
                StringAssert.Contains(migrator.GenerateScript(Migration, Previous), sql);
        }

        private static string Bindings(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                    $"SELECT bindings AS Value FROM mission_scene_binding WHERE mission_id = {DonnKnownLogos.MissionId} AND content_revision = '{DonnKnownLogos.Revision}'")
                .AsEnumerable().Single();
    }
}
