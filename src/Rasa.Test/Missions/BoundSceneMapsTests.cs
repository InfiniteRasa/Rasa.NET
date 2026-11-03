using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Missions.Content;
using Rasa.Services.Preloader;
using Rasa.Test.Database;

namespace Rasa.Test.Missions
{
    /// <summary>Mining for Information and Kill the Light part II played in their instance wherever they are taken (BoundSceneMaps).</summary>
    [TestClass]
    [DoNotParallelize]
    public class BoundSceneMapsTests
    {
        private const string Migration = "20271019000000_Bound_scene_maps";
        private const string Previous = "20271018000000_Control_tower_teleporter_ids";

        [TestMethod]
        public void EachSceneIsBoundToItsInstanceAndDownTakesItOff()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();
            migrator.Migrate(Previous);
            var before = BoundSceneMaps.Scenes.ToDictionary(s => s.MissionId, s => Bindings(database, s.MissionId, s.Revision));
            foreach (var (missionId, revision, _) in BoundSceneMaps.Scenes)
                Assert.IsNull(Scene(before[missionId]).BoundMapContextId, $"{missionId} unbound before");

            migrator.Migrate(Migration);
            foreach (var (missionId, revision, map) in BoundSceneMaps.Scenes)
            {
                var after = Bindings(database, missionId, revision);
                Assert.AreEqual(before[missionId].Substring(0, before[missionId].Length - 1) + $",\"boundMapContextId\":{map}}}", after);
                var scene = Scene(after);
                Assert.AreEqual(map, scene.BoundMapContextId, $"{missionId}");
                Assert.AreEqual(Scene(before[missionId]).Actors.Count, scene.Actors.Count, "the rest as it was");
            }
            Assert.AreEqual(WarnetCavernsContent.Map, Scene(Bindings(database, WarnetCavernsMissions.KillTheLightII, WarnetCavernsMissions.Revision)).MapContextId);

            // Once is enough: run again, nothing more is added.
            // ExecuteSqlRaw formats its text: the JSON's braces are doubled for it.
            foreach (var sql in BoundSceneMaps.Up(mySql: false))
                database.Context.Database.ExecuteSqlRaw(sql.Replace("{", "{{").Replace("}", "}}"));
            foreach (var (missionId, revision, _) in BoundSceneMaps.Scenes)
                Assert.AreEqual(1, Bindings(database, missionId, revision).Split(BoundSceneMaps.Key).Length - 1, $"{missionId} bound once");

            migrator.Migrate(Previous);
            foreach (var (missionId, revision, _) in BoundSceneMaps.Scenes)
                Assert.AreEqual(before[missionId], Bindings(database, missionId, revision), $"{missionId} as it was");
        }

        [TestMethod]
        public void OnMySqlTheJsonIsJoinedWithConcat()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(Previous, Migration);
            StringAssert.Contains(up, $"SET bindings = CONCAT(SUBSTR(bindings, 1, LENGTH(bindings) - 1), ',\"boundMapContextId\":{TimoraMinesContent.Map}}}')");
            Assert.IsFalse(up.Contains("|| '"), "no || on MySQL");
            StringAssert.Contains(migrator.GenerateScript(Migration, Previous), $"REPLACE(bindings, ',\"boundMapContextId\":{WarnetCavernsContent.Map}}}', '}}')");
        }

        private static string Bindings(OrdinaryLootTestDatabase database, uint missionId, string revision) =>
            database.Context.Database.SqlQueryRaw<string>(
                $"SELECT bindings AS Value FROM mission_scene_binding WHERE mission_id = {missionId} AND content_revision = '{revision}'").ToArray().Single();

        private static MissionSceneDefinition Scene(string json) =>
            System.Text.Json.JsonSerializer.Deserialize<MissionSceneDefinition>(json, MissionContentCodec.Options);
    }
}
