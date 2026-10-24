extern alias RasaGame;

using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Managers;

    /// <summary>
    /// The parts of a pass of the world loop, timed for 'perf' (LoopPhases): nothing kept while
    /// they are off, each guarded map worker by its name while they are on, and a window that
    /// starts again once read.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class LoopPhasesTests
    {
        [TestCleanup]
        public void Cleanup()
        {
            LoopPhases.Enabled = false;
            LoopPhases.Reset();
        }

        [TestMethod]
        public void OffNothingIsKept()
        {
            LoopPhases.Enabled = false;
            LoopPhases.Reset();

            var mark = LoopPhases.Begin();
            LoopPhases.End("Anything", mark);
            LoopPhases.EndPass(mark);

            var window = LoopPhases.Take();

            Assert.AreEqual(0L, window.Passes);
            Assert.IsEmpty(window.Parts);
        }

        [TestMethod]
        public void OnEachPartIsSummedByNameAndTheWindowStartsAgain()
        {
            LoopPhases.Reset();
            LoopPhases.Enabled = true;

            for (var pass = 0; pass < 3; pass++)
            {
                var whole = LoopPhases.Begin();

                for (var i = 0; i < 2; i++)
                {
                    var part = LoopPhases.Begin();
                    _ = new byte[1024];
                    LoopPhases.End("Allocates", part);
                }

                LoopPhases.End("Nothing", LoopPhases.Begin());
                LoopPhases.EndPass(whole);
            }

            var window = LoopPhases.Take();

            Assert.AreEqual(3L, window.Passes);
            Assert.HasCount(2, window.Parts);

            var allocates = window.Parts.Single(part => part.Name == "Allocates");
            Assert.AreEqual(6L, allocates.Calls);
            Assert.IsGreaterThanOrEqualTo(6L * 1024, allocates.Bytes);
            Assert.IsGreaterThanOrEqualTo(6L * 1024, window.PassBytes);
            Assert.AreEqual(3L, window.Parts.Single(part => part.Name == "Nothing").Calls);

            var lines = LoopPhases.Describe(window);
            Assert.IsTrue(lines[0].StartsWith("Phases over"), lines[0]);
            Assert.IsTrue(lines.Any(line => line.Contains("Allocates")));

            var next = LoopPhases.Take();
            Assert.AreEqual(0L, next.Passes);
            Assert.IsEmpty(next.Parts);
        }

        [TestMethod]
        public void TheMapWorkersAreTimedByName()
        {
            using var world = new WorldTestContext();
            world.CreateClient(0, 0);

            var maps = new MapChannelManager(null);
            maps.MapChannelArray.Add(world.Map.MapInfo.MapContextId, world.Map);

            LoopPhases.Reset();
            LoopPhases.Enabled = true;

            maps.MapChannelWorker(100);

            var names = LoopPhases.Take().Parts.Select(part => part.Name).ToList();

            CollectionAssert.Contains(names, "BehaviorManager.MapChannelThink");
            CollectionAssert.Contains(names, "ManifestationManager.AutoFireTimerDoWork");
            CollectionAssert.Contains(names, "MapChannelManager.RemoveFlaggedPlayers");
        }
    }
}
