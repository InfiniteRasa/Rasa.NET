using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Missions.Content;
using Rasa.Navigation;
using Rasa.Packets.MapChannel.Client;
using Rasa.Structures;
using Rasa.Structures.Char;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions
{
    using K = Rasa.Services.Preloader.KardashAttaColonyContent;
    using W = Rasa.Services.Preloader.KardashWaypointsAndMysteries;
    using static Wilderness.WildernessTwinPillarsTests;

    /// <summary>
    /// The Kardash Atta Colony's research outpost waypoints, Captain Reyko's Underground Mysteries
    /// and Atta Control offered at the console (KardashWaypointsAndMysteries).
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class KardashWaypointsAndMysteriesTests
    {
        private const string Migration = "20271017000000_Kardash_waypoints_mysteries";
        private const string Colony = "20271015000000_Kardash_atta_colony";

        [TestInitialize]
        public void Clock() => KardashAttaColony.Reset();

        [TestCleanup]
        public void Restore() => KardashAttaColony.Reset();

        [TestMethod]
        public void TheMigrationFillsTheFivePlaceholdersAndDownPutsThemBack()
        {
            using var database = new Rasa.Test.Database.OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();
            migrator.Migrate(Colony);
            var before = Teleporters(database);
            foreach (var w in W.Waypoints)
                Assert.AreEqual("0 0 0 0.00 0.00 0.00 0", before[w.Id], $"{w.Id} a placeholder before");
            var sourcesBefore = Sources(database);
            Assert.AreEqual(W.AttaControlSourcesBefore(), sourcesBefore);
            Assert.AreEqual(0, Count(database, $"SELECT COUNT(*) AS Value FROM mission_content_definition WHERE mission_id = {K.UndergroundMysteries}"));

            migrator.Migrate(Migration);
            var after = Teleporters(database);
            foreach (var w in W.Waypoints)
                Assert.AreEqual($"{W.WaypointPad} {W.WaypointType} {K.Colony} {w.At.X:F2} {w.At.Y:F2} {w.At.Z:F2} 0", after[w.Id], $"{w.Id}");
            Assert.AreEqual(W.AttaControlSourcesAfter(), Sources(database));
            Assert.AreEqual(1, Count(database, $"SELECT COUNT(*) AS Value FROM mission_content_definition WHERE mission_id = {K.UndergroundMysteries} AND content_revision = '{K.Revision}' AND enabled = 1"));
            Assert.AreEqual(K.Missions.Length, Count(database, $"SELECT COUNT(*) AS Value FROM mission_content_definition WHERE content_revision = '{K.Revision}' AND enabled = 1"));

            // A row a GM moved since is left alone by the Down.
            database.Context.Database.ExecuteSqlRaw($"UPDATE teleporter SET pos_x = 1 WHERE id = {W.Waypoints[4].Id}");
            migrator.Migrate(Colony);
            var down = Teleporters(database);
            foreach (var w in W.Waypoints.Take(4))
                Assert.AreEqual(before[w.Id], down[w.Id], $"{w.Id} put back");
            Assert.AreNotEqual(before[W.Waypoints[4].Id], down[W.Waypoints[4].Id], "the moved one kept");
            Assert.AreEqual(sourcesBefore, Sources(database));
            Assert.AreEqual(0, Count(database, $"SELECT COUNT(*) AS Value FROM mission_content_definition WHERE mission_id = {K.UndergroundMysteries}"));
            Assert.AreEqual(K.OwnMissions.Length, Count(database, $"SELECT COUNT(*) AS Value FROM mission_content_definition WHERE content_revision = '{K.Revision}' AND enabled = 1"));

            database.Context.Database.ExecuteSqlRaw($"UPDATE teleporter SET pos_x = 0 WHERE id = {W.Waypoints[4].Id}");
            migrator.Migrate(Migration);
            Assert.AreEqual(1, Count(database, $"SELECT COUNT(*) AS Value FROM mission_content_definition WHERE mission_id = {K.UndergroundMysteries} AND enabled = 1"));
        }

        /// <summary>Each pad on the colony's floor at its outpost, near the client's marker and clear of the people and pools there.</summary>
        [TestMethod]
        public void EachWaypointStandsOnTheFloorAtItsOutpost()
        {
            using var database = new Rasa.Test.Database.OrdinaryLootTestDatabase();
            database.Context.GetService<IMigrator>().Migrate(Migration);
            var nav = TestNavMeshes.Query(NavMeshFile.PathFor(NavmeshDirectory(), "adv_arieki_torden_plains_attacolony"));
            var pools = database.Context.SpawnPoolEntries.AsNoTracking().Where(pool => pool.MapContextId == K.Colony).ToList();
            var rows = database.Context.TeleporterEntries.AsNoTracking().ToDictionary(row => row.Id);

            foreach (var w in W.Waypoints)
            {
                var row = rows[w.Id];
                var pad = new Vector3((float)row.PosX, (float)row.PosY, (float)row.PosZ);
                var floor = nav.Nearest(pad);
                Assert.IsTrue(floor.HasValue, $"{w.Id} off the navmesh");
                Assert.IsLessThan(0.1f, Vector2.Distance(new Vector2(pad.X, pad.Z), new Vector2(floor.Value.X, floor.Value.Z)), $"{w.Id} over walkable ground");
                Assert.IsLessThan(0.3f, Math.Abs(floor.Value.Y - pad.Y), $"{w.Id}: the floor {floor.Value.Y - pad.Y} m from it");
                Assert.IsLessThan(4.5f, Vector3.Distance(pad, w.Marker), $"{w.Id} at the client's marker");
                foreach (var pool in pools)
                    Assert.IsFalse(Vector2.Distance(new Vector2(pad.X, pad.Z), new Vector2((float)pool.PosX, (float)pool.PosZ)) < 3f &&
                        Math.Abs(pad.Y - pool.PosY) < 3, $"{w.Id} on pool {pool.Id}");
            }

            // The artifact: in reach of the Eloh Cache's floor.
            var ground = nav.Nearest(W.ArtifactAt);
            Assert.IsTrue(ground.HasValue);
            Assert.IsLessThan(1f, Vector3.Distance(W.ArtifactAt, ground.Value));
        }

        /// <summary>The server puts a pad at each outpost in the colony, gained by walking onto it.</summary>
        [TestMethod]
        public void TheColonyHasItsFivePadsAndOneIsGainedOnArrival()
        {
            using var harness = Create(K.Colony);
            foreach (var w in W.Waypoints)
            {
                Assert.IsTrue(harness.Map.Teleporters.TryGetValue(w.Id, out var pad), $"{w.Id} in the colony");
                Assert.AreEqual(DynamicObjectType.Waypoint, pad.DynamicObjectType);
                Assert.IsLessThan(0.1f, Vector3.Distance(pad.Position, w.At));
            }

            var beta = harness.Map.Teleporters[W.Waypoints[1].Id];
            harness.MoveTo(beta.Position);
            harness.Drain();
            harness.Objects.DynamicObjectProximityWorker(harness.Map, beta, 1000);
            harness.Drain();
            Assert.IsTrue(harness.Client.Player.GainedWaypoints.Any(g => g.WaypointId == W.Waypoints[1].Id && g.WaypointType == W.WaypointType),
                "gained on arrival");
        }

        /// <summary>
        /// Taken from Reyko away from the colony (on the Plains in game; here the harness' own map), the
        /// artifact is the colony's, not put down where he stands: the scene is bound to the colony.
        /// </summary>
        [TestMethod]
        public void UndergroundMysteriesIsPlayedInTheColonyWhereverReykoIs()
        {
            using var database = new Rasa.Test.Database.OrdinaryLootTestDatabase();
            database.Context.GetService<IMigrator>().Migrate(Migration);
            var binding = database.Context.Database.SqlQueryRaw<string>(
                $"SELECT bindings AS Value FROM mission_scene_binding WHERE mission_id = {K.UndergroundMysteries} AND content_revision = '{K.Revision}'").ToArray().Single();
            var scene = System.Text.Json.JsonSerializer.Deserialize<MissionSceneDefinition>(binding, Rasa.Missions.Content.MissionContentCodec.Options);
            Assert.AreEqual(K.Colony, scene.BoundMapContextId);

            using var harness = Create(1220);
            var reyko = Put(harness, K.Reyko, harness.Client.Player.Position + new Vector3(2, 0, 0));
            Accept(harness, reyko, K.UndergroundMysteries);
            harness.Drain();
            Assert.AreEqual(MissionState.Active, harness.Client.Player.Missions[K.UndergroundMysteries].State);
            Assert.IsFalse(harness.Map.DynamicObjects.Any(obj => obj.SceneMissionId == K.UndergroundMysteries), "nothing where Reyko is");
        }

        /// <summary>Reyko's Underground Mysteries: the Eloh artifact in the Eloh Cache, then back to him.</summary>
        [TestMethod]
        public void TheElohArtifactIsFoundAndReykoTakesTheReport()
        {
            using var harness = Create(K.Colony);
            var reyko = Put(harness, K.Reyko, K.Arrival + new Vector3(2, 0, 0));
            Accept(harness, reyko, K.UndergroundMysteries);
            harness.Drain();

            var artifact = harness.Map.DynamicObjects.SingleOrDefault(obj => obj.SceneMissionId == K.UndergroundMysteries && obj.SceneActorRole == "eloh-artifact");
            Assert.IsNotNull(artifact, "the Eloh artifact");
            Assert.AreEqual((uint)W.ArtifactClass, (uint)artifact.EntityClassId);
            Assert.IsLessThan(0.5f, Vector3.Distance(artifact.Position, W.ArtifactAt));

            var npcs = new NpcManager(harness, harness.Manager);
            harness.MoveTo(artifact.Position + new Vector3(2, 0, 0));
            harness.Drain();
            npcs.RequestNpcConverse(harness.Client, new RequestNPCConversePacket { EntityId = artifact.EntityId, ActionId = ActionId.Converse, ActionArgId = 1 });
            npcs.CompleteNPCObjective(harness.Client, new CompleteNPCObjectivePacket
            {
                EntityId = artifact.EntityId, MissionId = K.UndergroundMysteries, ObjectiveId = 1, PlayerFlagId = 1
            });
            harness.Drain();
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, K.UndergroundMysteries, 1));

            reyko = Put(harness, K.Reyko, harness.Client.Player.Position + new Vector3(0, 0, 2));
            Reward(harness, reyko, K.UndergroundMysteries, null, null, (int)W.MysteriesXp, (int)W.MysteriesCredits);
        }

        /// <summary>Into the Hive turned in at the console: Atta Control is offered there and then, before the colony's own radio call.</summary>
        [TestMethod]
        public void AttaControlIsOfferedTheMomentIntoTheHiveIsTurnedIn()
        {
            using var harness = Create(K.Colony);
            var reyko = Put(harness, K.Reyko, K.Arrival + new Vector3(2, 0, 0));
            Accept(harness, reyko, K.IntoTheHive);
            KardashAttaColony.Worker(harness.Map);
            harness.Drain();

            var console = KardashAttaColony.Console(harness.Map);
            Assert.IsNotNull(console);
            var npcs = new NpcManager(harness, harness.Manager);
            harness.MoveTo(console.Position + new Vector3(-2, 0, 0));
            harness.Drain();
            npcs.RequestNpcConverse(harness.Client, new RequestNPCConversePacket { EntityId = console.EntityId, ActionId = ActionId.Converse, ActionArgId = 1 });
            npcs.CompleteNPCObjective(harness.Client, new CompleteNPCObjectivePacket
            {
                EntityId = console.EntityId, MissionId = K.IntoTheHive, ObjectiveId = 1, PlayerFlagId = 1
            });
            harness.Drain();
            Assert.IsFalse(harness.Manager.TryAcceptRadioMission(harness.Client, K.AttaControl), "not before the turn-in");
            harness.Drain();

            Assert.IsTrue(harness.Manager.TryCompleteRadioMission(harness.Client, K.IntoTheHive, null, null), "radioed in");
            harness.Drain();

            // No colony worker tick in between: the offer is the turn-in's.
            Assert.IsTrue(harness.Manager.TryAcceptRadioMission(harness.Client, K.AttaControl), "offered at the turn-in");
            harness.Drain();
            Assert.AreEqual(MissionState.Active, harness.Client.Player.Missions[K.AttaControl].State);
        }

        #region Helpers

        private static long Count(Rasa.Test.Database.OrdinaryLootTestDatabase database, string sql) =>
            database.Context.Database.SqlQueryRaw<long>(sql).ToArray().Single();

        private static string Sources(Rasa.Test.Database.OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>(
                $"SELECT radio_sources AS Value FROM mission_channel_policy WHERE mission_id = {K.AttaControl} AND content_revision = '{K.Revision}'").ToArray().Single();

        private static Dictionary<uint, string> Teleporters(Rasa.Test.Database.OrdinaryLootTestDatabase database) =>
            database.Context.TeleporterEntries.AsNoTracking().Where(row => row.Id >= 331 && row.Id <= 335).ToList()
                .ToDictionary(row => row.Id, row => $"{row.ClassId} {row.Type} {row.MapContextId} {row.PosX:F2} {row.PosY:F2} {row.PosZ:F2} {row.Rotation:0}");

        private static WildernessRuntimeTestHarness Create(uint map)
        {
            var harness = WildernessRuntimeTestHarness.Create(mapContextId: map);
            try
            {
                using var unit = harness.CreateChar();
                unit.ExecuteTransaction(() => unit.CharacterStartingExperience.Add(
                    new CharacterStartingExperienceEntry(harness.Client.Player.Id, K.Revision, CharacterStartingExperienceState.Skipped)));
                harness.Client.Player.StartingExperienceCompleted = true;
                harness.Drain();
                return harness;
            }
            catch
            {
                harness.Dispose();
                throw;
            }
        }

        private static MissionObjectiveState Objective(WildernessRuntimeTestHarness harness, uint missionId, uint objectiveId) =>
            harness.Client.Player.Missions.TryGetValue(missionId, out var log) && log.Objectives.TryGetValue(objectiveId, out var objective)
                ? objective.State
                : MissionObjectiveState.NotAssigned;

        private static Creature Put(WildernessRuntimeTestHarness harness, uint dbId, Vector3 position)
        {
            var creature = CreatureManager.Instance.CreateCreature(dbId, null);
            CreatureManager.Instance.SetLocation(creature, NavMeshManager.SnapToGround(harness.Map, position), 0, harness.Map.MapInfo.MapContextId);
            CellManager.Instance.AddToWorld(harness.Map, creature);
            return creature;
        }

        private static string NavmeshDirectory()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "navmesh")))
                directory = directory.Parent;

            Assert.IsNotNull(directory, "navmesh not found");
            return Path.Combine(directory.FullName, "navmesh");
        }

        #endregion
    }
}
