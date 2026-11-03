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
    /// <summary>The mission markers the server could not place, each on its map (MarkerMapScopes).</summary>
    [TestClass]
    [DoNotParallelize]
    public class MarkerMapScopesTests
    {
        private const string Migration = "20271020000000_Marker_map_scopes";
        private const string Previous = "20271019000000_Bound_scene_maps";

        [TestMethod]
        public void EachMarkerGetsItsMapAndDownTakesThemOff()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();
            migrator.Migrate(Previous);
            var scenes = MarkerMapScopes.Scenes().ToList();
            var before = scenes.ToDictionary(s => s.MissionId, s => Bindings(database, s.MissionId, s.Revision));
            foreach (var (missionId, json) in before)
                Assert.IsNull(Scene(json).IndicatorMaps, $"{missionId} none before");

            // Each marker given a map is one the mission has, and every marker of a scene is given one.
            var rows = database.Context.MissionIndicatorEntries.AsNoTracking().ToList();
            foreach (var (missionId, revision) in scenes)
                CollectionAssert.AreEquivalent(
                    rows.Where(r => r.MissionId == missionId && r.ContentRevision == revision &&
                        !(missionId == CraterLakeContent.ReclaimingWhatsOurs && r.ObjectiveId is CraterLakeContent.AroundFromPen or CraterLakeContent.ChaosFromPen))
                        .Select(r => (r.ObjectiveId, r.IndicatorId)).ToList(),
                    MarkerMapScopes.Markers.Where(m => m.MissionId == missionId).Select(m => (m.ObjectiveId, m.IndicatorId)).ToList(),
                    $"{missionId}");

            migrator.Migrate(Migration);
            foreach (var (missionId, revision) in scenes)
            {
                var after = Bindings(database, missionId, revision);
                Assert.AreEqual(before[missionId].Substring(0, before[missionId].Length - 1) + "," + MarkerMapScopes.Property(missionId) + "}", after);
                Assert.AreEqual(Scene(before[missionId]).Actors.Count, Scene(after).Actors.Count, "the rest as it was");
            }
            foreach (var (missionId, revision, objectiveId, indicatorId, map) in MarkerMapScopes.Markers)
                Assert.AreEqual(map, Scene(Bindings(database, missionId, revision)).IndicatorMaps[objectiveId][indicatorId], $"{missionId}.{objectiveId}");

            // ExecuteSqlRaw formats its text: the JSON's braces are doubled for it.
            foreach (var sql in MarkerMapScopes.Up(mySql: false))
                database.Context.Database.ExecuteSqlRaw(sql.Replace("{", "{{").Replace("}", "}}"));
            foreach (var (missionId, revision) in scenes)
                Assert.AreEqual(1, Bindings(database, missionId, revision).Split(MarkerMapScopes.Key).Length - 1, $"{missionId} once");

            migrator.Migrate(Previous);
            foreach (var (missionId, revision) in scenes)
                Assert.AreEqual(before[missionId], Bindings(database, missionId, revision), $"{missionId} as it was");
        }

        [TestMethod]
        public void OnMySqlTheJsonIsJoinedWithConcat()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(Previous, Migration);
            StringAssert.Contains(up, $"CONCAT(SUBSTR(bindings, 1, LENGTH(bindings) - 1), ',{MarkerMapScopes.Property(CraterLakeContent.TheDeadLive)}}}')");
            Assert.IsFalse(up.Contains("|| '"), "no || on MySQL");
            StringAssert.Contains(migrator.GenerateScript(Migration, Previous), $"REPLACE(bindings, ',{MarkerMapScopes.Property(MagmaCavernsContent.FinalChance)}}}', '}}')");
        }

        private static string Bindings(OrdinaryLootTestDatabase database, uint missionId, string revision) =>
            database.Context.Database.SqlQueryRaw<string>(
                $"SELECT bindings AS Value FROM mission_scene_binding WHERE mission_id = {missionId} AND content_revision = '{revision}'").ToArray().Single();

        private static MissionSceneDefinition Scene(string json) =>
            System.Text.Json.JsonSerializer.Deserialize<MissionSceneDefinition>(json, MissionContentCodec.Options);
    }
}
