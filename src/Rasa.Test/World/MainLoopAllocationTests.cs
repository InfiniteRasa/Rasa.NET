using System.Diagnostics;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Threading;

    /// <summary>
    /// The loop's metrics count what its passes allocate (MainLoop.LoopMetrics.AllocatedBytes):
    /// in all, and the most in one pass, as 'perf' shows them.
    /// </summary>
    [TestClass]
    public class MainLoopAllocationTests
    {
        private sealed class Allocating : ILoopable
        {
            public volatile int Passes;
            public byte[] Last;

            public void MainLoop(long delta)
            {
                Last = new byte[64 * 1024];
                Passes++;
            }
        }

        [TestMethod]
        public void WhatThePassesAllocateIsCounted()
        {
            var work = new Allocating();
            var loop = new MainLoop(work, 10);

            loop.TakeMetrics();
            loop.Start();

            var waited = Stopwatch.StartNew();
            while (work.Passes < 5 && waited.ElapsedMilliseconds < 5000)
                Thread.Sleep(10);

            loop.Stop();
            Thread.Sleep(50);

            var metrics = loop.TakeMetrics();

            Assert.IsGreaterThanOrEqualTo(5, metrics.Loops);
            Assert.IsGreaterThanOrEqualTo(64L * 1024 * metrics.Loops, metrics.AllocatedBytes);
            Assert.IsGreaterThanOrEqualTo(64L * 1024, metrics.PeakAllocatedBytes);
            Assert.IsGreaterThanOrEqualTo(64.0 * 1024, metrics.AllocatedBytesPerLoop);

            var next = loop.TakeMetrics();
            Assert.AreEqual(0L, next.AllocatedBytes);
        }
    }
}
