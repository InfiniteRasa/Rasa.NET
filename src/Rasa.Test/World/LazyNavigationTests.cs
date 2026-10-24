extern alias RasaGame;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using ClientState = RasaGame::Rasa.Data.ClientState;
    using Rasa.Config;
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Navigation;
    using Rasa.Structures;

    /// <summary>
    /// A map's navmesh and cover read when a player is first on the way to it, not at the start
    /// (Navigation.Lazy, LazyNavigation): nothing is read until someone needs it, an arrival waits
    /// for the read, every copy of a map has the map's one navmesh, and a map nobody has been on
    /// for Navigation.UnloadAfterSeconds lets go of it, to be read again.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class LazyNavigationTests
    {
        private const string Arena = "adv_afs_arena";

        private NavigationConfig _config;
        private Func<string, string, NavMeshManager.MapFiles> _reader;
        private Func<MapNavigation, bool> _shipsOver;

        [TestInitialize]
        public void Begin()
        {
            _config = LazyNavigation.Config;
            _reader = LazyNavigation.Reader;
            _shipsOver = LazyNavigation.ShipsOver;
            LazyNavigation.Reset();
            LazyNavigation.Config = new NavigationConfig { Lazy = true, UnloadAfterSeconds = 60 };
            LazyNavigation.ShipsOver = _ => false;

            if (Logger.Config == null)
                Logger.UpdateConfig(new Logger.LoggerConfig());
        }

        [TestCleanup]
        public void End()
        {
            LazyNavigation.Reset();
            LazyNavigation.Config = _config;
            LazyNavigation.Reader = _reader;
            LazyNavigation.ShipsOver = _shipsOver;
        }

        private static MapChannel NewMap(string name = Arena, uint id = 991001) => new MapChannel
        {
            MapInfo = new MapInfo(id, name, 1, 0),
            ClientList = new List<Client>()
        };

        [TestMethod]
        public void NothingIsReadUntilSomebodyAsksAndThenItIsTheMapsOwn()
        {
            var map = NewMap();
            var reads = 0;
            LazyNavigation.Reader = (directory, name) => { Interlocked.Increment(ref reads); return NavMeshManager.LoadFiles(directory, name); };
            var navigation = LazyNavigation.Note(map, AiCostBenchmarks.Navmesh());

            Assert.AreEqual(0, reads);
            Assert.IsNull(map.NavMeshIfRead);
            Assert.IsNull(map.CoverIfRead);
            Assert.IsFalse(navigation.Settled);

            // Asked for on the spot: read there and then, and it is what an eager read gives.
            var own = NavMeshManager.LoadFiles(AiCostBenchmarks.Navmesh(), Arena);
            Assert.IsNotNull(map.NavMesh);
            Assert.AreEqual(1, reads);
            Assert.AreEqual(1, LazyNavigation.Demands);
            Assert.AreEqual(own.NavMesh.HeightRange(), map.NavMesh.HeightRange());
            Assert.AreEqual(SafetyFloor.FloorFor(own.HeightRange.Value.Lowest), map.SafetyFloorY);
            Assert.AreEqual(own.HeightRange.Value.HighestWalkable, map.TopWalkableY);
            Assert.IsTrue(map.Cover.SameAs(own.Cover));
            Assert.AreSame(map.NavMesh, map.NavMeshIfRead);
            Assert.AreEqual(1, reads, "read once");
        }

        [TestMethod]
        public void ACopyHasTheMapsOneNavmeshWithoutReadingIt()
        {
            var map = NewMap();
            LazyNavigation.Note(map, AiCostBenchmarks.Navmesh());
            var shared = new MapChannel { MapInfo = map.MapInfo, ClientList = new List<Client>() };
            var personal = new MapChannel { MapInfo = map.MapInfo, ClientList = new List<Client>() };

            shared.ShareNavigationOf(map, heights: true);
            personal.ShareNavigationOf(map, heights: false);

            Assert.IsFalse(map.NavigationFiles.Settled, "making the copies read nothing");
            Assert.AreSame(map.NavMesh, shared.NavMesh);
            Assert.AreSame(map.NavMesh, personal.NavMesh);
            Assert.AreSame(map.Cover, shared.Cover);
            Assert.AreEqual(map.SafetyFloorY, shared.SafetyFloorY);
            Assert.IsNull(personal.SafetyFloorY, "a private instance never had a safety floor of its own");
        }

        [TestMethod]
        public void ACopyOfAMapReadAtTheStartTakesWhatItTookBefore()
        {
            var map = NewMap();
            var files = NavMeshManager.LoadFiles(AiCostBenchmarks.Navmesh(), Arena);
            map.NavMesh = files.NavMesh;
            map.Cover = files.Cover;
            map.SafetyFloorY = -5;
            map.TopWalkableY = 40;
            var shared = new MapChannel { MapInfo = map.MapInfo };
            var personal = new MapChannel { MapInfo = map.MapInfo };

            shared.ShareNavigationOf(map, heights: true);
            personal.ShareNavigationOf(map, heights: false);

            Assert.AreSame(files.NavMesh, shared.NavMesh);
            Assert.AreSame(files.Cover, personal.Cover);
            Assert.AreEqual(-5f, shared.SafetyFloorY);
            Assert.AreEqual(40f, shared.TopWalkableY);
            Assert.IsNull(personal.SafetyFloorY);
            Assert.IsNull(personal.NavigationFiles);
        }

        [TestMethod]
        public void AnArrivalWaitsForTheReadAndIsAnsweredWhenItIsDone()
        {
            var map = NewMap();
            using var gate = new ManualResetEventSlim(false);
            LazyNavigation.Reader = (directory, name) => { gate.Wait(TimeSpan.FromSeconds(30)); return NavMeshManager.LoadFiles(directory, name); };
            LazyNavigation.Note(map, AiCostBenchmarks.Navmesh());

            using var world = new WorldTestContext();
            var client = world.CreateClient();
            world.Map.ClientList.Remove(client);
            client.Player.MapChannel = map;
            client.State = ClientState.Loading;
            client.AwaitingMapLoaded = true;

            var answered = new List<Client>();
            var clients = new List<Client> { client };
            var maps = new[] { map };

            // The client on its way: the read begins, and its MapLoaded is held.
            LazyNavigation.Worker(0, clients, maps, answered.Add);
            Assert.IsNotNull(map.NavigationFiles.Reading, "begun while the client loads");
            Assert.IsTrue(LazyNavigation.Hold(client));
            Assert.IsTrue(LazyNavigation.Hold(client), "held once, however often it asks");
            Assert.AreEqual(1, LazyNavigation.Holds);

            LazyNavigation.Worker(100, clients, maps, answered.Add);
            Assert.AreEqual(0, answered.Count, "not before the read is done");

            gate.Set();
            SpinWait.SpinUntil(() => map.NavigationFiles.Reading.IsCompleted, TimeSpan.FromSeconds(30));
            LazyNavigation.Worker(200, clients, maps, answered.Add);

            CollectionAssert.AreEqual(new[] { client }, answered);
            Assert.IsTrue(map.NavigationFiles.Settled);
            Assert.IsNotNull(map.NavMeshIfRead);
            Assert.IsFalse(LazyNavigation.Hold(client), "read: not held");
            Assert.AreEqual(0, LazyNavigation.Demands, "nobody had to wait on the spot");
        }

        [TestMethod]
        public void AClientThatLeftWhileItWaitedIsNotAnswered()
        {
            var map = NewMap();
            using var gate = new ManualResetEventSlim(false);
            LazyNavigation.Reader = (directory, name) => { gate.Wait(TimeSpan.FromSeconds(30)); return NavMeshManager.LoadFiles(directory, name); };
            LazyNavigation.Note(map, AiCostBenchmarks.Navmesh());

            using var world = new WorldTestContext();
            var client = world.CreateClient();
            client.Player.MapChannel = map;
            client.AwaitingMapLoaded = true;

            LazyNavigation.Want(map.NavigationFiles);
            Assert.IsTrue(LazyNavigation.Hold(client));
            client.State = ClientState.Disconnected;
            gate.Set();
            SpinWait.SpinUntil(() => map.NavigationFiles.Reading.IsCompleted, TimeSpan.FromSeconds(30));

            var answered = new List<Client>();
            LazyNavigation.Worker(0, new List<Client>(), new[] { map }, answered.Add);
            Assert.AreEqual(0, answered.Count);
        }

        [TestMethod]
        public void AMapNobodyIsOnLetsGoOfItsNavigationAndReadsItAgain()
        {
            var map = NewMap();
            var copy = new MapChannel { MapInfo = map.MapInfo, ClientList = new List<Client>() };
            var ships = false;
            LazyNavigation.ShipsOver = _ => ships;
            var navigation = LazyNavigation.Note(map, AiCostBenchmarks.Navmesh());
            copy.ShareNavigationOf(map, heights: true);

            using var world = new WorldTestContext();
            var client = world.CreateClient();
            client.Player.MapChannel = copy;
            var clients = new List<Client> { client };
            var maps = new[] { map };
            var first = map.NavMesh;
            var floor = map.SafetyFloorY;

            // Somebody on a copy keeps it, however long.
            for (var now = 0L; now <= 120_000; now += 10_000)
                LazyNavigation.Worker(now, clients, maps, _ => { });

            Assert.IsTrue(navigation.IsRead);

            // Nobody, but a dropship still over it: kept.
            clients.Clear();
            ships = true;

            for (var now = 130_000L; now <= 260_000; now += 10_000)
                LazyNavigation.Worker(now, clients, maps, _ => { });

            Assert.IsTrue(navigation.IsRead);

            // Nobody and nothing for a minute: let go of.
            ships = false;

            for (var now = 270_000L; now <= 340_000; now += 10_000)
                LazyNavigation.Worker(now, clients, maps, _ => { });

            Assert.IsFalse(navigation.IsRead);
            Assert.AreEqual(1, LazyNavigation.Unloads);
            Assert.IsNull(copy.NavMeshIfRead);
            Assert.AreEqual(floor, map.SafetyFloorY, "the two heights are kept, without reading it again");
            Assert.IsFalse(navigation.Settled);

            // The next to come has it read again: another navmesh, the same ground.
            var again = copy.NavMesh;
            Assert.IsNotNull(again);
            Assert.AreNotSame(first, again);
            Assert.AreEqual(first.HeightRange(), again.HeightRange());
            Assert.AreEqual(2, navigation.ReadCount);
        }

        [TestMethod]
        public void KeptOnceReadWithoutAnUnloadTime()
        {
            LazyNavigation.Config = new NavigationConfig { Lazy = true, UnloadAfterSeconds = 0 };
            var map = NewMap();
            var navigation = LazyNavigation.Note(map, AiCostBenchmarks.Navmesh());
            Assert.IsNotNull(map.NavMesh);

            for (var now = 0L; now <= 3_600_000; now += 60_000)
                LazyNavigation.Worker(now, new List<Client>(), new[] { map }, _ => { });

            Assert.IsTrue(navigation.IsRead);
        }

        [TestMethod]
        public void WhatTheStartPutsOnTheGroundIsPutThereOnceTheNavmeshIsRead()
        {
            var map = NewMap();
            LazyNavigation.Note(map, AiCostBenchmarks.Navmesh());
            var files = NavMeshManager.LoadFiles(AiCostBenchmarks.Navmesh(), Arena);
            var ground = files.NavMesh.Nearest(Vector3.Zero) ?? FirstWalkable(files.NavMesh);
            var above = new Vector3(ground.X, ground.Y + 1.5f, ground.Z);
            var placed = above;

            var now = LazyNavigation.SnapToGroundOnceRead(map, above, snapped => placed = snapped);

            Assert.AreEqual(above, now, "as it is, for now");
            Assert.IsFalse(map.NavigationFiles.Settled, "and nothing read for it");

            // The read, as the worker takes it in when a player comes.
            Assert.IsNotNull(map.NavMesh);
            Assert.AreEqual(NavMeshManager.SnapToGround(map, above), placed);
            Assert.AreNotEqual(above, placed);

            // Read: at once.
            Assert.AreEqual(NavMeshManager.SnapToGround(map, above), LazyNavigation.SnapToGroundOnceRead(map, above, _ => Assert.Fail("not later")));
        }

        [TestMethod]
        public void AMapWhoseFilesWillNotReadRunsWithoutThemAndSaysSo()
        {
            var map = NewMap(id: 991077);
            LazyNavigation.Reader = (directory, name) => throw new InvalidDataException("broken");
            LazyNavigation.Note(map, AiCostBenchmarks.Navmesh());

            Assert.IsNull(map.NavMesh);
            Assert.IsNull(map.Cover);
            Assert.IsTrue(map.NavigationFiles.Settled);
        }

        [TestMethod]
        public void AnExplicitNavmeshIsTheMapsWhateverItsFiles()
        {
            var map = NewMap();
            LazyNavigation.Note(map, AiCostBenchmarks.Navmesh());
            map.NavMesh = null;

            Assert.IsNull(map.NavMesh, "set to none: none, and nothing read for it");
            Assert.IsFalse(map.NavigationFiles.Settled);
            Assert.IsNull(map.WaitingNavigation);
        }

        [TestMethod]
        public void TheStartNotesTheMapsAndReadsNone()
        {
            using var harness = Missions.BootcampRuntimeTestHarness.Create();
            var navmesh = AiCostBenchmarks.Navmesh();
            var maps = new (uint Id, string Name)[] { (991001, Arena), (991002, "adv_arieki_ligo_crucible_wbfacility"), (991009, "no_such_map") };

            foreach (var (id, name) in maps)
                harness.Maps.MapChannelArray[id] = new MapChannel { MapInfo = new MapInfo(id, name, 1, 0), ClientList = new List<Client>() };

            var reads = 0;
            LazyNavigation.Reader = (directory, name) => { Interlocked.Increment(ref reads); return NavMeshManager.LoadFiles(directory, name); };

            try
            {
                ((NavMeshManager)Activator.CreateInstance(typeof(NavMeshManager), nonPublic: true)).NavMeshInit(navmesh);

                Assert.AreEqual(0, reads, "nothing read at the start");
                Assert.IsTrue(LazyNavigation.Active);
                Assert.IsNotNull(harness.Maps.MapChannelArray[991001].NavigationFiles);
                Assert.IsNull(harness.Maps.MapChannelArray[991009].NavigationFiles, "no files, nothing to note");
                Assert.IsNull(harness.Maps.MapChannelArray[991009].NavMesh);

                var own = new NavMeshQuery(NavMeshFile.Read(NavMeshFile.PathFor(navmesh, "adv_arieki_ligo_crucible_wbfacility")));
                Assert.AreEqual(own.HeightRange(), harness.Maps.MapChannelArray[991002].NavMesh.HeightRange());
                Assert.AreEqual(1, reads);
            }
            finally
            {
                foreach (var (id, _) in maps)
                    harness.Maps.MapChannelArray.Remove(id);
            }
        }

        [TestMethod]
        public void OffTheStartReadsEveryMapAsItAlwaysDid()
        {
            LazyNavigation.Config = new NavigationConfig { Lazy = false };
            using var harness = Missions.BootcampRuntimeTestHarness.Create();
            var reads = 0;
            LazyNavigation.Reader = (directory, name) => { Interlocked.Increment(ref reads); return NavMeshManager.LoadFiles(directory, name); };
            harness.Maps.MapChannelArray[991001] = new MapChannel { MapInfo = new MapInfo(991001, Arena, 1, 0), ClientList = new List<Client>() };

            try
            {
                ((NavMeshManager)Activator.CreateInstance(typeof(NavMeshManager), nonPublic: true)).NavMeshInit(AiCostBenchmarks.Navmesh());

                var map = harness.Maps.MapChannelArray[991001];
                Assert.IsFalse(LazyNavigation.Active);
                Assert.IsNull(map.NavigationFiles);
                Assert.IsNotNull(map.NavMeshIfRead, "read at the start");
                Assert.IsNotNull(map.CoverIfRead);
                Assert.AreEqual(0, reads, "not through LazyNavigation");
            }
            finally
            {
                harness.Maps.MapChannelArray.Remove(991001);
            }
        }

        private static Vector3 FirstWalkable(NavMeshQuery query) =>
            AiCostBenchmarks.RandomPairs(query, 1, 1).First().From;
    }
}
