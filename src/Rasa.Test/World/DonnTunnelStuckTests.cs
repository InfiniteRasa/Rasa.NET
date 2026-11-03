using System;
using System.IO;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Managers;
    using Rasa.Navigation;
    using Rasa.Structures;
    using D = Rasa.Services.Preloader.CavesOfDonnContent;

    /// <summary>
    /// /stuck in the Caves of Donn's tunnels, and a fall out of the world from where a rescue
    /// put the player (NavMeshManager.UnstickDestination, SafetyFloor). The map is all caves under
    /// a shell of terrain; /stuck at the Fairway's cave-in took the floor under it for a pocket
    /// inside a hill and put the player on the shell, 14 m up, outside the map. They fell through
    /// it, were put back on it, and fell again for as long as they watched.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class DonnTunnelStuckTests
    {
        /// <summary>The tunnel from Undone Fairway to the Vitiated Shrine, either side of its cave-in, and the Chantry tunnel's.</summary>
        private static readonly Vector3[] TunnelFloor =
        {
            new(78f, 21f, -246f), new(80f, 21f, -246f), new(82f, 21f, -246.5f), new(83f, 20.7f, -247f),
            new(85f, 20.8f, -246.5f), new(87f, 20f, -247f), new(89f, 19.3f, -248f), new(-97.2f, 16.65f, -291.2f)
        };

        /// <summary>The terrain over the Fairway's cave-in, where /stuck put the player.</summary>
        private static readonly Vector3 Shell = new(82f, 34.52f, -246.5f);

        private long _now = 1_000_000;

        [TestInitialize]
        public void Clock() => SafetyFloor.Now = () => _now;

        [TestCleanup]
        public void Restore() => SafetyFloor.Now = () => Environment.TickCount64;

        [TestMethod]
        public void StuckInATunnelStaysOnItsFloor()
        {
            var map = Donn();
            var nav = map.NavMesh;

            foreach (var stuck in TunnelFloor)
            {
                var here = NavMeshManager.NearestWalkable(map, stuck).Value;
                var shell = SafetyFloor.TopOfColumn(map, stuck).Value;

                Assert.IsTrue(nav.IsUnderground(here) && shell.Y > here.Y + 10f && nav.Reachable(here, shell) == false,
                    $"{stuck}: a floor under the shell ({shell}) and joined to nothing on it, as a pocket is");
                Assert.IsGreaterThan(NavMeshManager.UnstickPocketArea, nav.RegionArea(here) ?? 0f, $"{stuck}: no pocket, a floor to walk");

                var destination = NavMeshManager.UnstickDestination(map, stuck).Value;
                Assert.IsTrue(nav.IsUnderground(destination), $"{stuck}: still in the tunnel, not up on the shell: {destination}");
                Assert.IsLessThan(2f, Math.Abs(destination.Y - here.Y), $"{stuck}: on its floor: {destination}");
                Assert.IsLessThan(NavMeshManager.UnstickFarthest + 0.1f, Vector3.Distance(here, destination), $"{stuck}: {destination}");
            }
        }

        /// <summary>Lower Eloh Creek's pocket is small; a floor people walk is not.</summary>
        [TestMethod]
        public void APocketIsSmall()
        {
            var nav = TestNavMeshes.Query(NavMeshFile.PathFor(NavMeshDirectory(), "adv_foreas_concordia_wilderness"));

            Assert.IsLessThan(NavMeshManager.UnstickPocketArea, nav.RegionArea(nav.Nearest(new Vector3(343.52344f, 226f, 194.98828f)).Value).Value);
            Assert.IsGreaterThan(NavMeshManager.UnstickPocketArea, nav.RegionArea(nav.Nearest(new Vector3(615f, 284f, 737f)).Value).Value);
            Assert.IsNull(nav.RegionArea(new Vector3(0f, -5000f, 0f)), "off the mesh");
        }

        [TestMethod]
        public void OutAgainFromWhereTheRescuePutThemTheyGoToTheEntrance()
        {
            using var world = new WorldTestContext();
            Load(world.Map);
            var links = InstanceEntrances.MapLinks;
            world.Map.IsSquadInstance = true;
            InstanceEntrances.MapLinks = () => new[]
            {
                new MapLink { Id = 1, MapContextId = 1148, DestMapContextId = world.Map.MapInfo.MapContextId, DestPosition = D.Entrance }
            };

            try
            {
                var client = world.CreateClient();
                var player = client.Player;
                var below = new Vector3(Shell.X, world.Map.SafetyFloorY.Value - 5f, Shell.Z);

                // Stood on the shell - the navmesh has it walkable - and fell through it.
                player.PlaceAt(Shell);
                SafetyFloor.NoteStanding(client, _now);
                Assert.AreEqual(Shell, player.LastSafePosition);

                _now += 2_000;
                player.Position = below;
                Assert.IsTrue(SafetyFloor.OnMove(client, below, _now));
                Assert.AreEqual(Shell, player.Position, "put back where they last stood, the first time");

                // There again a moment, and through it again: not back onto it.
                _now += 1_000;
                SafetyFloor.NoteStanding(client, _now);
                _now += 2_000;
                player.Position = below;
                Assert.IsTrue(SafetyFloor.OnMove(client, below, _now));
                Assert.AreEqual(D.Entrance, player.Position, "the instance's entrance, where a dead player would get up");

                // Walked off from there, and a fall later on: back where they last stood, as ever.
                _now += 1_000;
                var walked = NavMeshManager.NearestWalkable(world.Map, D.Entrance + new Vector3(0f, 0f, 6f)).Value;
                player.PlaceAt(walked);
                SafetyFloor.NoteStanding(client, _now);
                Assert.IsNull(player.RescuedTo, "it held them");

                _now += SafetyFloor.RescueRepeatMs;
                player.Position = below;
                Assert.IsTrue(SafetyFloor.OnMove(client, below, _now));
                Assert.AreEqual(walked, player.Position);
            }
            finally
            {
                InstanceEntrances.MapLinks = links;
                world.Map.IsSquadInstance = false;
                world.Map.NavMesh = null;
                world.Map.Cover = null;
                world.Map.SafetyFloorY = null;
                world.Map.TopWalkableY = null;
            }
        }

        /// <summary>The same fall a minute and more after the rescue is a fall of its own: back where they last stood.</summary>
        [TestMethod]
        public void AFallLongAfterARescueGoesBackWhereTheyStood()
        {
            using var world = new WorldTestContext();
            Load(world.Map);

            try
            {
                var client = world.CreateClient();
                var player = client.Player;
                var stood = NavMeshManager.NearestWalkable(world.Map, D.Fairway).Value;
                var below = new Vector3(stood.X, world.Map.SafetyFloorY.Value - 5f, stood.Z);

                player.PlaceAt(stood);
                SafetyFloor.NoteStanding(client, _now);
                player.Position = below;
                Assert.IsTrue(SafetyFloor.OnMove(client, below, _now));

                _now += SafetyFloor.RescueRepeatMs;
                player.Position = below;
                Assert.IsTrue(SafetyFloor.OnMove(client, below, _now));
                Assert.AreEqual(stood, player.Position);
            }
            finally
            {
                world.Map.NavMesh = null;
                world.Map.Cover = null;
                world.Map.SafetyFloorY = null;
                world.Map.TopWalkableY = null;
            }
        }

        private static MapChannel Donn()
        {
            var map = new MapChannel();
            Load(map);
            return map;
        }

        /// <summary>The Caves of Donn's navmesh and cover, and its height range as NavMeshManager sets it.</summary>
        private static void Load(MapChannel map)
        {
            var directory = NavMeshDirectory();
            map.NavMesh = TestNavMeshes.Query(NavMeshFile.PathFor(directory, "adv_foreas_concordia_wilderness_cavesofdonn02"));
            map.Cover = CoverMesh.Read(CoverMesh.PathFor(directory, "adv_foreas_concordia_wilderness_cavesofdonn02"));
            var (lowest, highest) = map.NavMesh.HeightRange().Value;
            map.SafetyFloorY = SafetyFloor.FloorFor(lowest);
            map.TopWalkableY = highest;
        }

        private static string NavMeshDirectory()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Rasa.NET.sln")))
                directory = directory.Parent;
            return Path.Combine(directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found."), "navmesh");
        }
    }
}
