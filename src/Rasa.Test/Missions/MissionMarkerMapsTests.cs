using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Missions.Content;
using Rasa.Services.Preloader;
using Rasa.Services.Preloader.Missions.Pools;
using Rasa.Structures;
using Rasa.Structures.World;
using Rasa.Test.Database;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions
{
    /// <summary>The mission markers the server put on the wrong map, each on its own (MissionMarkerMaps).</summary>
    [TestClass]
    [DoNotParallelize]
    public class MissionMarkerMapsTests
    {
        private const string Migration = "20271103000000_Mission_marker_maps";

        /// <summary>The migration directly before Mission_marker_maps, on the context's provider.</summary>
        private static string Previous(DbContext context)
        {
            var ids = context.GetService<IMigrationsAssembly>().Migrations.Keys.OrderBy(id => id, System.StringComparer.Ordinal).ToList();
            var index = ids.IndexOf(Migration);
            Assert.IsGreaterThan(0, index, "the migration is there, after another");
            return ids[index - 1];
        }

        /// <summary>The 25 markers and the refinery's, as ResolveMap places them on the migrated content.</summary>
        [TestMethod]
        public void EachMarkerResolvesToItsMap()
        {
            using var harness = WildernessAliaBranchesTests.CreateHarness();
            var catalog = new MissionContentCatalog(harness, new System.Collections.Generic.Dictionary<uint, Mission>(), null);
            catalog.Load();
            Assert.HasCount(26, MissionMarkerMaps.Markers);
            foreach (var (missionId, _, objectiveId, indicatorId, map) in MissionMarkerMaps.Markers)
            {
                var pin = catalog.Missions[missionId].Objectives[objectiveId].Indicators.Single(i => i.IndicatorId == indicatorId);
                Assert.AreEqual(map, MissionMarkerProjection.ResolveMap(catalog, missionId, objectiveId, pin), $"{missionId}.{objectiveId}/{indicatorId}");
            }

            // The data-only scenes change nothing but the markers' maps.
            foreach (var missionId in MissionMarkerMaps.NewScenes)
            {
                var scene = catalog.SceneBindings[missionId];
                Assert.IsNull(scene.Script, $"{missionId} no script");
                Assert.AreEqual(0U, scene.MapContextId, $"{missionId} no scene map");
                Assert.IsEmpty(scene.Actors);
                Assert.IsEmpty(scene.Sequences);
                Assert.IsEmpty(scene.Credit);
                Assert.IsEmpty(scene.ObjectiveRequirements);
                Assert.IsNull(scene.Requirement);
                Assert.IsNull(scene.Items);
                Assert.IsNull(scene.Dialogue);
                Assert.IsNull(scene.BoundMapContextId);
            }

            // Looking For Logos? objective 3: the door on the Pools, the Far shrine in the refinery.
            foreach (var (map, expected) in new[] { (PoolsMissionDataV1.MapContextId, MissionIndicator.UnnamedFrom + 2), (PoolsLookingForLogosV1.TurpisRefinery, MissionMarkerMaps.RefineryIndicator) })
            {
                var objective = new MissionObjective { ObjectiveId = PoolsLookingForLogosV1.Find, State = MissionObjectiveState.Incomplete, IsRequired = true };
                objective.IndicatorList.AddRange(catalog.Missions[PoolsLookingForLogosV1.MissionId].Objectives[PoolsLookingForLogosV1.Find].Indicators
                    .Select(i => new MissionIndicator { IndicatorId = i.IndicatorId, MapContextId = i.MapContextId, Position = i.Position, Radius = i.Radius }));
                var info = new MissionInfo { MissionState = MissionState.Active, ObjectivesList = new() { objective } };
                MissionMarkerProjection.Apply(new Manifestation { MapContextId = map }, PoolsLookingForLogosV1.MissionId, info, catalog);
                Assert.AreEqual(expected, info.ObjectivesList.Single().IndicatorList.Single().IndicatorId, $"on {map}");
            }
            var shrine = catalog.Missions[PoolsLookingForLogosV1.MissionId].Objectives[PoolsLookingForLogosV1.Find].Indicators
                .Single(i => i.IndicatorId == MissionMarkerMaps.RefineryIndicator);
            Assert.IsLessThan(0.01f, System.Numerics.Vector3.Distance(new System.Numerics.Vector3(40f, 111.8f, -224f), shrine.Position), "on the Far shrine (logos 64)");
            Assert.AreEqual(MissionMarkerMaps.MissionMarkerRadius, shrine.Radius, 0.001);
        }

        [TestMethod]
        public void UpAddsTheMapsOnceAndDownLeavesTheRowsAsTheyWere()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();
            var previous = Previous(database.Context);
            migrator.Migrate(previous);
            var appended = MissionMarkerMaps.AppendedScenes().ToList();
            var inserted = MissionMarkerMaps.InsertedScenes().ToList();
            Assert.HasCount(11, appended);
            Assert.HasCount(2, inserted);
            var before = appended.ToDictionary(s => s.MissionId, s => Bindings(database, s.MissionId, s.Revision));
            foreach (var (missionId, json) in before)
                Assert.IsNull(Scene(json).IndicatorMaps, $"{missionId} none before");
            foreach (var (missionId, revision) in inserted)
                Assert.IsEmpty(Rows(database, missionId, revision), $"{missionId} no scene before");
            Assert.AreEqual(0, RefineryMarkers(database));

            // Each marker given a map is one the mission has.
            var indicators = database.Context.MissionIndicatorEntries.AsNoTracking().ToList();
            foreach (var (missionId, revision, objectiveId, indicatorId, _) in MissionMarkerMaps.Markers.Where(m => m.IndicatorId != MissionMarkerMaps.RefineryIndicator))
                Assert.IsTrue(indicators.Any(r => r.MissionId == missionId && r.ContentRevision == revision && r.ObjectiveId == objectiveId && r.IndicatorId == indicatorId),
                    $"{missionId}.{objectiveId}/{indicatorId}");

            migrator.Migrate(Migration);
            foreach (var (missionId, revision) in appended)
            {
                var after = Bindings(database, missionId, revision);
                Assert.AreEqual(before[missionId].Substring(0, before[missionId].Length - 1) + "," + MissionMarkerMaps.Property(missionId) + "}", after);
                Assert.AreEqual(Scene(before[missionId]).Actors.Count, Scene(after).Actors.Count, "the rest as it was");
            }
            foreach (var (missionId, revision) in inserted)
                Assert.AreEqual(MissionMarkerMaps.NewScene(missionId), Bindings(database, missionId, revision));
            foreach (var (missionId, revision, objectiveId, indicatorId, map) in MissionMarkerMaps.Markers)
                Assert.AreEqual(map, Scene(Bindings(database, missionId, revision)).IndicatorMaps[objectiveId][indicatorId], $"{missionId}.{objectiveId}");
            Assert.AreEqual(1, RefineryMarkers(database));

            // Twice: nothing more. ExecuteSqlRaw formats its text: the JSON's braces are doubled for it.
            foreach (var sql in MissionMarkerMaps.Up(mySql: false))
                database.Context.Database.ExecuteSqlRaw(sql.Replace("{", "{{").Replace("}", "}}"));
            foreach (var (missionId, revision) in MissionMarkerMaps.Scenes())
            {
                Assert.HasCount(1, Rows(database, missionId, revision), $"{missionId} one scene");
                Assert.AreEqual(1, Bindings(database, missionId, revision).Split(MissionMarkerMaps.Key).Length - 1, $"{missionId} once");
            }
            Assert.AreEqual(1, RefineryMarkers(database));

            // A scene edited by hand after Up is left to its editor.
            var (edited, editedRevision) = inserted[1];
            var handEdit = MissionMarkerMaps.NewScene(edited).Replace(":2136", ":1497");
            database.Context.Database.ExecuteSqlRaw(
                $"UPDATE mission_scene_binding SET bindings = '{handEdit.Replace("{", "{{").Replace("}", "}}")}' WHERE mission_id = {edited}");

            migrator.Migrate(previous);
            foreach (var (missionId, revision) in appended)
                Assert.AreEqual(before[missionId], Bindings(database, missionId, revision), $"{missionId} as it was");
            Assert.IsEmpty(Rows(database, inserted[0].MissionId, inserted[0].Revision), $"{inserted[0].MissionId} scene gone");
            Assert.AreEqual(handEdit, Bindings(database, edited, editedRevision), "the hand-edited scene stays");
            Assert.AreEqual(0, RefineryMarkers(database));

            // Down twice: nothing more is taken.
            foreach (var sql in MissionMarkerMaps.Down())
                database.Context.Database.ExecuteSqlRaw(sql.Replace("{", "{{").Replace("}", "}}"));
            foreach (var (missionId, revision) in appended)
                Assert.AreEqual(before[missionId], Bindings(database, missionId, revision), $"{missionId} as it was, twice");
            Assert.AreEqual(handEdit, Bindings(database, edited, editedRevision), "the hand-edited scene stays, twice");
        }

        [TestMethod]
        public void OnMySqlTheJsonIsJoinedWithConcat()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var previous = Previous(context);
            var up = migrator.GenerateScript(previous, Migration);
            StringAssert.Contains(up, $"CONCAT(SUBSTR(bindings, 1, LENGTH(bindings) - 1), ',{MissionMarkerMaps.Property(DevilsDenContent.HakarisBand)}}}')");
            Assert.IsFalse(up.Contains("|| '"), "no || on MySQL");
            StringAssert.Contains(up, MissionMarkerMaps.NewScene(PoolsTurpisTransportV1.MissionId));
            StringAssert.Contains(migrator.GenerateScript(Migration, previous), $"REPLACE(bindings, ',{MissionMarkerMaps.Property(PoolsLookingForLogosV1.MissionId)}}}', '}}')");
        }

        private static System.Collections.Generic.List<string> Rows(OrdinaryLootTestDatabase database, uint missionId, string revision) =>
            database.Context.Database.SqlQueryRaw<string>(
                $"SELECT bindings AS Value FROM mission_scene_binding WHERE mission_id = {missionId} AND content_revision = '{revision}'").ToList();

        private static string Bindings(OrdinaryLootTestDatabase database, uint missionId, string revision) => Rows(database, missionId, revision).Single();

        private static int RefineryMarkers(OrdinaryLootTestDatabase database) =>
            database.Context.MissionIndicatorEntries.AsNoTracking().Count(r => r.MissionId == MissionMarkerMaps.RefineryMission &&
                r.ObjectiveId == MissionMarkerMaps.RefineryObjective && r.IndicatorId == MissionMarkerMaps.RefineryIndicator);

        private static MissionSceneDefinition Scene(string json) =>
            System.Text.Json.JsonSerializer.Deserialize<MissionSceneDefinition>(json, MissionContentCodec.Options);
    }
}
