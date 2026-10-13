using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Structures;

    /// <summary>
    /// BR-160: the once-a-second workers - the spawn pools' respawn clocks among them - are given
    /// the time that has passed since they last ran on the map, not the loop's 100 ms delta, which
    /// made every respawn take ten times as long.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class SpawnClockTests
    {
        private long _now = 1_000_000;

        [TestMethod]
        public void AWorkerIsGivenTheTimeSinceItLastRanOnThatMap()
        {
            using var world = new WorldTestContext();
            var maps = Maps(world);
            var other = new MapChannel { MapInfo = new MapInfo(1300, "adv_foreas_fixture_other", 1, 0), ClientList = new List<Client>() };

            Assert.AreEqual(0L, maps.SinceLastRun(world.Map, "CheckForCreatures"), "nothing to count the first time");

            _now += 1_000;
            Assert.AreEqual(1_000L, maps.SinceLastRun(world.Map, "CheckForCreatures"));
            Assert.AreEqual(0L, maps.SinceLastRun(world.Map, "CheckForObjects"), "each worker its own clock");
            Assert.AreEqual(0L, maps.SinceLastRun(other, "CheckForCreatures"), "and each map");

            // A map that stood empty is given the whole time when it has someone again.
            _now += 600_000;
            Assert.AreEqual(600_000L, maps.SinceLastRun(world.Map, "CheckForCreatures"));

            // A clock that goes back gives nothing, not a negative time.
            _now -= 5_000;
            Assert.AreEqual(0L, maps.SinceLastRun(world.Map, "CheckForCreatures"));
        }

        [TestMethod]
        public void APoolsRespawnClockRunsAtTheTimeThatPassed()
        {
            using var world = new WorldTestContext();
            var maps = Maps(world);
            var client = world.CreateClient();

            if (!world.Map.ClientList.Contains(client))
                world.Map.ClientList.Add(client);

            // Respawn 900: ninety seconds, killed a moment ago.
            var pool = new SpawnPool
            {
                DbId = 990160,
                MapContextId = world.Map.MapInfo.MapContextId,
                Mode = SpawnPoolManager.ModeAutomatic,
                AnimType = 0,
                RespawnTime = 900 * 100L,
                UpdateTimer = 0,
                SpawnSlot = new List<SpawnPoolSlot>()
            };

            world.Map.SpawnPools.Add(pool);

            try
            {
                maps.MapChannelWorker(1_000);
                Assert.AreEqual(0L, pool.UpdateTimer, "the first pass starts the clock");

                // A pass a second, each with the loop's delta: thirty seconds on the clock.
                _now += 30_000;
                maps.MapChannelWorker(1_000);

                Assert.AreEqual(30_000L, pool.UpdateTimer, "thirty seconds of the ninety, not one tick's worth");

                _now += 60_000;
                maps.MapChannelWorker(1_000);

                Assert.AreEqual(pool.RespawnTime, pool.UpdateTimer, "ninety seconds: due");
            }
            finally
            {
                world.Map.SpawnPools.Remove(pool);
            }
        }

        private MapChannelManager Maps(WorldTestContext world)
        {
            var maps = new MapChannelManager(null, () => _now, (_, _, _) => { },
                refreshStats: (_, _) => { }, assignPlayer: _ => { }, enterMapChannels: _ => { });

            maps.MapChannelArray.Add(world.Map.MapInfo.MapContextId, world.Map);

            // As MapChannelInit sets it, which needs the world database: once a second.
            maps.Timer.Add("CheckForCreatures", 1000, true, null);

            return maps;
        }
    }
}
