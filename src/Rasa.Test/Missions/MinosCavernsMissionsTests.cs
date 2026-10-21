using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Missions.Runtime;
using Rasa.Packets.LootDispenser.Client;
using Rasa.Services.Preloader;
using Rasa.Structures;
using Rasa.Structures.Char;
using Rasa.Structures.World;
using Rasa.Test.Database;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions
{
    using C = MinosCavernsContent;
    using M = MinosCavernsMissions;

    /// <summary>
    /// The four Minos Caverns missions (MinosCavernsMissions, Minos_caverns_missions): their rows,
    /// Sebastian's and Foster's giving, Tyler found, the datapad, the captives walked out
    /// (MinosCaverns), the databanks, Tyler walked out (MinosCaveExtractionScene), and the nests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class MinosCavernsMissionsTests
    {
        private const string Migration = "20270310000000_Minos_caverns_missions";
        private const string Previous = "20270309000000_Minos_caverns_content";

        private long _now = 1_000_000;

        [TestInitialize]
        public void Clock()
        {
            MinosCaverns.Reset();
            MinosCaverns.Now = () => _now;
            PlacedDestructibles.Now = () => _now;
        }

        [TestCleanup]
        public void Restore()
        {
            MinosCaverns.Reset();
            MinosCaverns.Now = () => Environment.TickCount64;
            PlacedDestructibles.Load(null);
            PlacedDestructibles.Reset();
        }

        [TestMethod]
        public void TheMigrationWritesTheFourMissionsAndTakesThemOutAgain()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();
            migrator.Migrate(Previous);
            Assert.AreEqual(0L, Count(database, $"SELECT run_speed AS Value FROM creature WHERE id = {C.Tyler}"));

            migrator.Migrate(Migration);
            Assert.AreEqual(4L, Count(database, $"SELECT COUNT(*) AS Value FROM mission_content_definition WHERE content_revision = '{M.Revision}' AND enabled = 1 AND category_id = {M.Category}"));
            Assert.AreEqual(10L, Count(database, $"SELECT COUNT(*) AS Value FROM mission_reward_item WHERE content_revision = '{M.Revision}' AND module_1 <> 0 AND kind = 2"), "every reward carries its brand's module, and is a choice");
            Assert.AreEqual(4L, Count(database, $"SELECT COUNT(*) AS Value FROM mission_reward_definition WHERE content_revision = '{M.Revision}' AND selection_count = 1"));
            Assert.AreEqual(1607L, Count(database, $"SELECT client_counter_0_text_id AS Value FROM mission_objective_definition WHERE mission_id = {M.WarnetBarrier} AND objective_id = 1"));
            Assert.AreEqual(2698L, Count(database, $"SELECT client_counter_0_text_id AS Value FROM mission_objective_definition WHERE mission_id = {M.DataThieves} AND objective_id = 4"));
            Assert.AreEqual((long)M.TylerRun, Count(database, $"SELECT run_speed AS Value FROM creature WHERE id = {C.Tyler} AND walk_speed = {M.TylerWalk}"));
            Assert.AreEqual(2L, Count(database, $"SELECT COUNT(*) AS Value FROM creature WHERE id IN ({C.Karik}, {C.Zola}) AND run_speed = {M.CaptiveRun} AND walk_speed = {M.CaptiveWalk}"));

            migrator.Migrate(Previous);
            foreach (var table in new[] { "mission_content_definition", "mission_objective_definition", "mission_trigger", "mission_action", "mission_reward_item", "mission_scene_binding", "mission_evidence", "mission_indicator", "mission_scenario" })
                Assert.AreEqual(0L, Count(database, $"SELECT COUNT(*) AS Value FROM {table} WHERE content_revision = '{M.Revision}'"), table);
            Assert.AreEqual(0L, Count(database, $"SELECT run_speed + walk_speed AS Value FROM creature WHERE id = {C.Tyler}"));
        }

        [TestMethod]
        public void TheFourAreOperationalWithTheirGiversRewardsAndRequirements()
        {
            using var harness = Create();

            foreach (var missionId in M.Missions)
            {
                Assert.IsTrue(harness.Manager.TryGetOperationalMission(missionId, out var mission), $"mission {missionId}");
                Assert.AreEqual(M.Category, mission.CategoryId);
            }

            var definitions = harness.World.Set<MissionContentDefinitionEntry>().Where(row => row.ContentRevision == M.Revision).ToDictionary(row => row.MissionId);
            Assert.AreEqual((C.Sebastian, C.Tyler), (definitions[M.RetrievalForRecon].GiverId, definitions[M.RetrievalForRecon].ReceiverId));
            Assert.AreEqual((C.Tyler, C.Tyler), (definitions[M.DataThieves].GiverId, definitions[M.DataThieves].ReceiverId));
            Assert.AreEqual((C.Tyler, C.Tyler), (definitions[M.CaveExtraction].GiverId, definitions[M.CaveExtraction].ReceiverId));
            Assert.AreEqual((C.Foster, C.Foster), (definitions[M.WarnetBarrier].GiverId, definitions[M.WarnetBarrier].ReceiverId));

            // Eight nests in the caverns alone: a nest on another map is no nest of Foster's.
            Assert.IsTrue(harness.Manager.TryGetOperationalMission(M.WarnetBarrier, out var barrier));
            Assert.IsTrue(barrier.ObjectiveRequirements != null && barrier.ObjectiveRequirements.TryGetValue(1, out var requirement) &&
                requirement is MapRequirement { Maps: { Count: 1 } maps } && maps[0] == C.Map, "counted in Minos Caverns only");

            // The escort's public encounter is on Tyler's own spawn, and Tyler and the captives can walk.
            Assert.IsTrue(harness.Manager.PublicActors.HasBinding(M.CaveExtraction));
            Assert.IsTrue(harness.Manager.PublicActors.StagesSpawn(M.TylerPool));
            var creatures = harness.Creatures.LoadedCreatures;
            Assert.IsTrue(creatures[C.Tyler].RunSpeed > 0 && creatures[C.Tyler].WalkSpeed > 0, "Tyler walks");
            Assert.IsTrue(creatures[C.Karik].RunSpeed > 0 && creatures[C.Zola].WalkSpeed > 0, "the captives walk");
        }

        [TestMethod]
        public void SebastianSendsThePlayerAfterTylerWhoIsFoundAndTakesTheTurnIn()
        {
            using var harness = Create();
            var tyler = SpawnTyler(harness);
            var sebastian = Put(harness, C.Sebastian, M.TylerSpot + new Vector3(6, 0, 0));

            WildernessTwinPillarsTests.Accept(harness, sebastian, M.RetrievalForRecon);
            Assert.IsFalse(harness.Client.Player.Missions[M.RetrievalForRecon].Completeable);

            WildernessTwinPillarsTests.Talk(harness, tyler, M.RetrievalForRecon, 1);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, M.RetrievalForRecon, 1));
            WildernessTwinPillarsTests.Reward(harness, tyler, M.RetrievalForRecon, 0, M.BioHelmet, 17000, 2550);
        }

        [TestMethod]
        public void DataThievesIsTheDatapadTheCaptivesWalkedOutAndTheDatabanks()
        {
            using var harness = Create();
            var map = harness.Map;
            Completed(harness, M.RetrievalForRecon);
            var tyler = SpawnTyler(harness);
            harness.SpawnWorld(M.TylerPool, MinosCaverns.KarikPool, MinosCaverns.ZolaPool);
            var karik = WildernessTwinPillarsTests.RequireNpc(harness, MinosCaverns.KarikPool, C.Karik);
            var zola = WildernessTwinPillarsTests.RequireNpc(harness, MinosCaverns.ZolaPool, C.Zola);
            Assert.AreEqual(NpcPose.Crouched, karik.Pose);

            WildernessTwinPillarsTests.Accept(harness, tyler, M.DataThieves);
            Assert.AreEqual((long)M.Detonators, WildernessTwinPillarsTests.Held(harness, C.DetonatorTemplate), "Tyler's detonators");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, M.DataThieves, 1));
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, M.DataThieves, 2));
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, M.DataThieves, 4));

            // The datapad: off the Technician General's corpse, not his death.
            var general = Put(harness, C.TechnicianGeneral, M.SideRoom);
            WildernessDaghdasUrnTests.Kill(harness, general);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, M.DataThieves, 1), "a kill is not a datapad");
            var corpse = map.LootDispensers[general.CorpseLootEntityId];
            Assert.AreEqual(1, corpse.LootItems.Count(item => item.ItemTemplateId == M.DatapadTemplate));
            LootDispenserManager.Instance.RequestLootAllFromCorpse(harness.Client, new RequestLootAllFromCorpsePacket { EntityId = corpse.EntityId });
            harness.Drain();
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, M.DataThieves, 1));
            Assert.AreEqual(1L, WildernessTwinPillarsTests.Held(harness, M.DatapadTemplate));

            // The captives: Karik's word frees them both; they follow whoever freed them and stands by them.
            Work(harness);
            Assert.AreEqual(0u, karik.SpawnPool.FollowOwnerCharacterId, "not before the rescue");
            WildernessTwinPillarsTests.Talk(harness, karik, M.DataThieves, 2);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, M.DataThieves, 2));
            Assert.IsTrue(MinosCaverns.Freed(harness.Client.Player));
            harness.MoveTo(karik.Position + new Vector3(3, 0, 0));
            Work(harness);
            Assert.AreEqual(harness.Client.Player.Id, karik.SpawnPool.FollowOwnerCharacterId, "Karik follows");
            Assert.AreEqual(harness.Client.Player.Id, zola.SpawnPool.FollowOwnerCharacterId, "and Zola");
            Assert.IsTrue(BehaviorManager.IsMissionEscort(karik) && BehaviorManager.IsMissionEscort(zola));
            Assert.IsFalse(karik.PoseShown, "up off the floor");

            // Led to the entrance, they leave: the escort is over and the pool follows nobody.
            foreach (var captive in new[] { karik, zola })
                captive.Position = MinosCaverns.Entrance + new Vector3(2, 0, 2);
            harness.MoveTo(MinosCaverns.Entrance);
            Work(harness);
            Assert.AreEqual(0u, karik.SpawnPool.FollowOwnerCharacterId);
            Assert.AreEqual(0u, zola.SpawnPool.FollowOwnerCharacterId);
            Assert.IsTrue(MinosCaverns.StateOf(map).LedOut.Contains(harness.Client.Player.Id));
            Assert.IsEmpty(MinosCaverns.StateOf(map).Following);
            harness.MoveTo(karik.Position + new Vector3(2, 0, 0));
            Work(harness);
            Assert.AreEqual(0u, karik.SpawnPool.FollowOwnerCharacterId, "once per player");

            // The databanks, the base's own rows: four down for 'Monitoring Machines Destroyed.'
            Assert.AreEqual(17, PlacedDestructibles.Load(harness.World.WorldDestructibleEntries.AsNoTracking().Where(row => row.MapContextId == C.Map).ToList()));
            PlacedDestructibles.Place(map);
            var databanks = PlacedDestructibles.On(map).Where(live => (uint)live.Object.EntityClassId == C.DatabankClass).ToList();
            Assert.AreEqual(4, databanks.Count);
            for (var i = 0; i < databanks.Count; i++)
            {
                harness.MoveTo(databanks[i].Object.Position + new Vector3(2, 0, 0));
                Assert.IsTrue(PlacedDestructibles.Destroy(databanks[i], harness.Client.Player));
                harness.Drain();
                Assert.AreEqual((uint)(i + 1), harness.Client.Player.Missions[M.DataThieves].Objectives[4].Counters[0]);
            }
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, M.DataThieves, 4));
            Assert.IsTrue(harness.Client.Player.Missions[M.DataThieves].Completeable);

            // Back to Tyler: the datapad and the detonators go, the helmet comes.
            WildernessTwinPillarsTests.Reward(harness, tyler, M.DataThieves, 0, M.HazmatHelmet, 13000, 1950);
            Assert.AreEqual(0L, WildernessTwinPillarsTests.Held(harness, M.DatapadTemplate));
            Assert.AreEqual(0L, WildernessTwinPillarsTests.Held(harness, C.DetonatorTemplate));
        }

        [TestMethod]
        public void FosterCountsEightNestsSmashedInTheCaverns()
        {
            using var harness = Create();
            var map = harness.Map;
            var foster = Put(harness, C.Foster, MinosCaverns.Entrance + new Vector3(3, 0, 0));

            WildernessTwinPillarsTests.Accept(harness, foster, M.WarnetBarrier);
            Assert.AreEqual(17, PlacedDestructibles.Load(harness.World.WorldDestructibleEntries.AsNoTracking().Where(row => row.MapContextId == C.Map).ToList()));
            PlacedDestructibles.Place(map);
            var nests = PlacedDestructibles.On(map).Where(live => (uint)live.Object.EntityClassId == C.WarnetNestClass).ToList();
            Assert.AreEqual(9, nests.Count);

            // A camera is not a nest; a nest nobody smashed counts for nobody.
            var camera = PlacedDestructibles.On(map).First(live => (uint)live.Object.EntityClassId == C.CameraClass);
            Assert.IsTrue(PlacedDestructibles.Destroy(camera, harness.Client.Player));
            Assert.IsTrue(PlacedDestructibles.Destroy(nests[8], null));
            harness.Drain();
            Assert.AreEqual(0u, harness.Client.Player.Missions[M.WarnetBarrier].Objectives[1].Counters[0]);

            for (var i = 0; i < 8; i++)
            {
                harness.MoveTo(nests[i].Object.Position + new Vector3(2, 0, 0));
                Assert.IsTrue(PlacedDestructibles.Destroy(nests[i], harness.Client.Player));
                harness.Drain();
                Assert.AreEqual((uint)(i + 1), harness.Client.Player.Missions[M.WarnetBarrier].Objectives[1].Counters[0]);
            }
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, M.WarnetBarrier, 1));
            WildernessTwinPillarsTests.Reward(harness, foster, M.WarnetBarrier, 1, M.LaserChaingun, 17000, 2550);
        }

        [TestMethod]
        public void TylerWalksThePlayerOutAndTakesTheTurnInAtTheEntrance()
        {
            using var harness = Create();
            var map = harness.Map;
            Completed(harness, M.DataThieves);
            var tyler = SpawnTyler(harness);
            var start = tyler.Position;

            // The route is a walk the navmesh allows, leg by leg, from his spot to the entrance.
            var route = M.TylerRoute();
            Assert.IsTrue(Vector3.Distance(start, new Vector3(route.Points[0].Position.X, route.Points[0].Position.Y, route.Points[0].Position.Z)) < 1f);
            for (var i = 1; i < route.Points.Count; i++)
            {
                var from = new Vector3(route.Points[i - 1].Position.X, route.Points[i - 1].Position.Y, route.Points[i - 1].Position.Z);
                var to = new Vector3(route.Points[i].Position.X, route.Points[i].Position.Y, route.Points[i].Position.Z);
                var path = map.NavMesh.FindPath(from, to, out var complete);
                Assert.IsTrue(complete && path != null && path.Count > 0 && Vector3.Distance(path[^1], to) < 2f, $"leg {i}: {from} to {to} cannot be walked");
            }

            WildernessTwinPillarsTests.Accept(harness, tyler, M.CaveExtraction);
            var lease = harness.Manager.PublicActors.Handle(map, M.TylerPool);
            Assert.IsNotNull(lease, "the public Tyler is reserved for the escort");
            Assert.IsFalse(tyler.IsInteractable, "busy walking");

            for (var tick = 0; tick < 4000 && Objective(harness, M.CaveExtraction, 1) == MissionObjectiveState.Incomplete; tick++)
            {
                var previous = tyler.Position;
                harness.MoveTo(tyler.Position + new Vector3(0, 0, 1));
                harness.Tick();
                Assert.IsTrue(Vector3.Distance(previous, tyler.Position) < 10, "walked, not teleported");
            }

            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, M.CaveExtraction, 1), $"Tyler at {tyler.Position}");
            Assert.IsTrue(Vector3.Distance(tyler.Position, MinosCaverns.Entrance) < 10, $"at the entrance: {tyler.Position}");
            Assert.IsTrue(harness.Client.Player.Missions[M.CaveExtraction].Completeable);
            harness.Tick();
            Assert.IsTrue(tyler.IsInteractable, "his conversation open again for the turn-in");
            Assert.IsNotNull(harness.Manager.PublicActors.Handle(map, M.TylerPool), "still his escort's until the reward");

            WildernessTwinPillarsTests.Reward(harness, tyler, M.CaveExtraction, 1, M.MotorAssistLegs, 14000, 2100);

            // Rewarded, he goes home.
            for (var tick = 0; tick < 4000 && harness.Manager.PublicActors.Handle(map, M.TylerPool) != null; tick++)
                harness.Tick();
            Assert.IsNull(harness.Manager.PublicActors.Handle(map, M.TylerPool), "released");
        }

        #region Helpers

        private static WildernessRuntimeTestHarness Create()
        {
            var harness = WildernessRuntimeTestHarness.Create(mapContextId: C.Map);
            try
            {
                using var unit = harness.CreateChar();
                unit.ExecuteTransaction(() => unit.CharacterStartingExperience.Add(
                    new CharacterStartingExperienceEntry(harness.Client.Player.Id, M.Revision, CharacterStartingExperienceState.Skipped)));
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

        private static Creature SpawnTyler(WildernessRuntimeTestHarness harness)
        {
            harness.SpawnWorld(M.TylerPool);
            return WildernessTwinPillarsTests.RequireNpc(harness, M.TylerPool, C.Tyler);
        }

        private static Creature Put(WildernessRuntimeTestHarness harness, uint dbId, Vector3 position)
        {
            var creature = CreatureManager.Instance.CreateCreature(dbId, null);
            CreatureManager.Instance.SetLocation(creature, NavMeshManager.SnapToGround(harness.Map, position), 0, C.Map);
            CellManager.Instance.AddToWorld(harness.Map, creature);
            return creature;
        }

        private void Work(WildernessRuntimeTestHarness harness)
        {
            _now += MinosCaverns.EveryMs;
            MinosCaverns.Worker(harness.Map);
            harness.Drain();
        }

        private static MissionObjectiveState Objective(WildernessRuntimeTestHarness harness, uint missionId, uint objectiveId) =>
            harness.Client.Player.Missions[missionId].Objectives[objectiveId].State;

        /// <summary>The missions before this one done, as history rows the server keeps.</summary>
        private static void Completed(WildernessRuntimeTestHarness harness, uint upTo)
        {
            foreach (var earlier in new[] { M.RetrievalForRecon, M.DataThieves }.Where(id => id <= upTo))
            {
                using var unit = harness.CreateChar();
                unit.ExecuteTransaction(() => unit.CharacterMissions.Runtime.Archive(
                    new CharacterMissionEntry(harness.Client.Player.Id, earlier, (uint)MissionState.Completed)
                    {
                        AssignmentId = Guid.NewGuid().ToString("N"),
                        Generation = 1,
                        ContentRevision = M.Revision
                    }, harness.UtcNow));
                harness.Client.Player.MissionHistory[earlier] = MissionState.Completed;
                harness.Client.Player.MissionSuccessHistory.Add(earlier);
            }
        }

        private static long Count(OrdinaryLootTestDatabase database, string sql) =>
            database.Context.Database.SqlQueryRaw<long>(sql).AsEnumerable().Single();

        #endregion
    }
}
