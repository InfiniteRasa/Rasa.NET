using System;
using System.IO;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Packets.Game.Server;
    using Rasa.Structures;

    [TestClass]
    [DoNotParallelize]
    public class BootcampReportedNavigationTests
    {
        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void DefaultNavmeshLoadsFromGameLaunchDirectoriesAndReachesTheReportedPosition(bool binaryDirectory)
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var template = harness.Maps.MapChannelArray[1985];
            template.MapInfo = new MapInfo(1985, "adv_bootcamp", 1556, 0);
            template.NavMesh = null;
            var loader = (NavMeshManager)Activator.CreateInstance(typeof(NavMeshManager), nonPublic: true);
            var previousDirectory = Environment.CurrentDirectory;
            try
            {
                var gameDirectory = Path.Combine(RepositoryRoot(), "src", "Rasa.Game");
                Environment.CurrentDirectory = binaryDirectory
                    ? AppContext.BaseDirectory
                    : gameDirectory;
                loader.NavMeshInit(null);
            }
            finally
            {
                Environment.CurrentDirectory = previousDirectory;
            }

            Assert.IsNotNull(template.NavMesh,
                "Launching the Game project must load the shipped navigation instead of silently running without it.");
            var instance = harness.Maps.GetOrCreatePrivateInstance(1985, 999);
            Assert.IsNotNull(instance.NavMesh, "The actual private Bootcamp instance must inherit loaded navigation.");
            var destination = new Vector3(347.66016f, 121.69922f, 64.625f);
            foreach (var start in new[]
                     {
                         new Vector3(368, 120.21479f, 158),
                         new Vector3(372, 119.956856f, 158),
                         new Vector3(374, 119.74777f, 164)
                     })
            {
                var route = instance.NavMesh.FindPath(start, destination, out var complete);
                Assert.IsNotNull(route);
                Assert.IsTrue(complete, $"The reported escort destination must be reachable from {start}.");
            }
        }

        [TestMethod]
        public void AlisterDoesNotRunThroughWallsWhenTheMapHasNoNavigation()
        {
            using var harness = BootcampRuntimeTestHarness.Create(useWorldContent: true);
            SpawnPoolManager.Instance.SpawnPoolWorker(harness.BootcampMap, 0);
            var alister = harness.BootcampMap.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList)
                .Single(creature => creature.DbId == BootcampRuntimeTestHarness.MajorMcAllisterCreatureId);
            harness.BootcampMap.NavMesh = null;
            var start = alister.Position;

            Assert.IsFalse(BehaviorManager.Instance.SetActionScriptedMove(
                harness.BootcampMap, alister, new Vector3(400, 120, 150), 2.175),
                "A required scripted route cannot turn into a straight line through geometry.");
            Assert.AreEqual(start, alister.Position);
        }

        [TestMethod]
        public void ThraxCannotReplaceAFailedNavmeshQueryWithAStraightLine()
        {
            using var harness = BootcampRuntimeTestHarness.Create(useWorldContent: true);
            SpawnPoolManager.Instance.SpawnPoolWorker(harness.BootcampMap, 0);
            var thrax = harness.BootcampMap.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList)
                .First(creature => creature.DbId == 510216);
            var destination = thrax.Position;
            thrax.Position += new Vector3(12, 100, 0);
            var start = thrax.Position;
            BehaviorManager.Instance.SetActionAnchor(thrax, destination);

            BehaviorManager.Instance.MapChannelThink(harness.BootcampMap, 250);

            Assert.AreEqual(start, thrax.Position,
                "An actor outside the loaded mesh must not walk an unchecked segment through terrain.");
        }

        [TestMethod]
        public void DefaultAssetResolutionSupportsPublishedAndRepositoryLayoutsWithoutOverridingConfiguration()
        {
            var root = RepositoryRoot();
            var game = Path.Combine(root, "src", "Rasa.Game");
            var expected = Path.Combine(root, "navmesh");
            Assert.AreEqual(expected, NavMeshManager.ResolveDirectory(null, game, root));
            Assert.AreEqual(expected, NavMeshManager.ResolveDirectory("navmesh", game, AppContext.BaseDirectory));
            Assert.AreEqual(expected, NavMeshManager.ResolveDirectory(expected, game, AppContext.BaseDirectory));
            Assert.AreEqual(Path.Combine(game, "missing-configured-navmesh"),
                NavMeshManager.ResolveDirectory("missing-configured-navmesh", game, root));
        }

        [TestMethod]
        public void ForeansAppearAndFollowToTheReportedBridgePositionAfterNormalNavigationStartup()
        {
            using var harness = BootcampRuntimeTestHarness.Create(
                useWorldContent: true, initializeMaps: LoadDefaultNavigation);
            SpawnPoolManager.Instance.SpawnPoolWorker(harness.BootcampMap, 0);
            harness.SeedMission(harness.Client.Player.Id, 1992, (uint)MissionState.Completed, true);
            var deSimone = BootcampRuntimeTestHarness.FindCreature(
                harness.BootcampMap, BootcampRuntimeTestHarness.CorporalDeSimoneCreatureId);
            harness.MovePlayerTo(deSimone);
            CellManager.Instance.UpdateVisibility(harness.Client);
            var foreans = Actors(harness).Where(creature => creature.DbId is >= 510213 and <= 510215).ToArray();
            Assert.AreEqual(3, foreans.Length);
            var introduced = harness.Drain().OfType<CreatePhysicalEntityPacket>().Select(packet => packet.EntityId).ToArray();
            Assert.IsTrue(foreans.All(creature => introduced.Contains(creature.EntityId)));
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, deSimone.EntityId, 1994));
            BootcampRuntimeTestHarness.PrepareDirectDamageClient(harness.Client);
            harness.Client.Player.GmFlagAlwaysFriendly = true;
            var destination = new Vector3(347.66016f, 121.69922f, 64.625f);
            harness.MovePlayerTo(destination);
            CellManager.Instance.UpdateVisibility(harness.Client);

            for (var tick = 0; tick < 200 && foreans.Any(creature => Vector3.Distance(creature.Position, destination) >= 20); tick++)
            {
                BehaviorManager.Instance.MapChannelThink(harness.BootcampMap, 250);
                MissileManager.Instance.DoWork(harness.BootcampMap, 250);
            }

            foreach (var forean in foreans)
            {
                Assert.AreNotEqual(CharacterState.Dead, forean.State);
                Assert.IsTrue(Vector3.Distance(forean.Position, destination) < 20,
                    $"Escort {forean.DbId} remained at {forean.Position} instead of following to {destination}.");
                AssertGrounded(harness, forean);
            }
        }

        [TestMethod]
        public void BothBridgeSoldiersAndThraxRemainGroundedAndFightAcrossRespawns()
        {
            using var harness = BootcampRuntimeTestHarness.Create(
                useWorldContent: true, initializeMaps: LoadDefaultNavigation);
            SpawnPoolManager.Instance.SpawnPoolWorker(harness.BootcampMap, 0);
            var initial = Actors(harness).Where(creature => creature.SpawnPool?.DbId is >= 510216 and <= 510220).ToArray();
            Assert.AreEqual(2, initial.Count(creature => creature.DbId == 510217));
            Assert.AreEqual(3, initial.Count(creature => creature.DbId == 510216));
            var sawAfsAttack = false;
            var sawThraxAttack = false;
            var sawRespawn = false;
            for (var tick = 0; tick < 240; tick++)
            {
                BehaviorManager.Instance.MapChannelThink(harness.BootcampMap, 250);
                MissileManager.Instance.DoWork(harness.BootcampMap, 250);
                SpawnPoolManager.Instance.SpawnPoolWorker(harness.BootcampMap, 250);
                foreach (var creature in Actors(harness).Where(creature =>
                             creature.SpawnPool?.DbId is >= 510216 and <= 510220 && creature.State != CharacterState.Dead))
                {
                    AssertGrounded(harness, creature);
                    if (creature.Controller.CurrentAction == BehaviorManager.BehaviorActionFighting)
                    {
                        sawAfsAttack |= creature.DbId == 510217;
                        sawThraxAttack |= creature.DbId == 510216;
                    }
                    sawRespawn |= initial.All(previous => previous.EntityId != creature.EntityId);
                }
            }
            Assert.IsTrue(sawAfsAttack && sawThraxAttack, "Both factions must participate in the bridge battle.");
            Assert.IsTrue(sawRespawn, "The recurring bridge battle must replace casualties.");
        }

        private static void LoadDefaultNavigation(MapChannelManager maps)
        {
            maps.MapChannelArray[1985].MapInfo.MapName = "adv_bootcamp";
            maps.MapChannelArray[1985].NavMesh = null;
            var previous = Environment.CurrentDirectory;
            try
            {
                Environment.CurrentDirectory = Path.Combine(RepositoryRoot(), "src", "Rasa.Game");
                ((NavMeshManager)Activator.CreateInstance(typeof(NavMeshManager), nonPublic: true)).NavMeshInit(null);
            }
            finally
            {
                Environment.CurrentDirectory = previous;
            }
        }

        private static Creature[] Actors(BootcampRuntimeTestHarness.Harness harness) =>
            harness.BootcampMap.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList).Distinct().ToArray();

        private static void AssertGrounded(BootcampRuntimeTestHarness.Harness harness, Creature creature)
        {
            var ground = harness.BootcampMap.NavMesh.Nearest(creature.Position);
            Assert.IsTrue(ground.HasValue && Vector3.Distance(ground.Value, creature.Position) < 0.5f,
                $"Actor {creature.DbId} left walkable ground at {creature.Position}.");
        }

        private static string RepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Rasa.NET.sln")))
                directory = directory.Parent;
            return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
        }
    }
}
