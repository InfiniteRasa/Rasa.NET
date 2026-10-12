using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Hosting;

    // A server that cannot start ends the process with an exit code that says so. It used to log
    // "Unable to start server!" and stay up with nothing running, and an exception out of Start
    // ended the host's ExecuteAsync where the host, with no logging set up, showed nothing and
    // exited 0.
    [TestClass]
    [DoNotParallelize]
    public class RasaHostStartTests
    {
        [TestMethod]
        public async Task AServerThatDoesNotStartStopsTheHostWithAnExitCode()
        {
            await AssertStopped(new FakeServer { Starts = () => false });
        }

        [TestMethod]
        public async Task AServerWhoseStartThrowsStopsTheHostWithAnExitCode()
        {
            await AssertStopped(new FakeServer { Starts = () => throw new InvalidOperationException("Table 'world.entityclass' doesn't exist") });
        }

        [TestMethod]
        public async Task AServerThatStartsLeavesTheHostAndTheExitCodeAlone()
        {
            var lifetime = new FakeLifetime();
            var server = new FakeServer { Starts = () => true };
            var previous = Environment.ExitCode;

            try
            {
                Environment.ExitCode = 0;
                using var host = new RasaHost(server, lifetime);

                await host.StartAsync(CancellationToken.None);
                await host.ExecuteTask!;

                Assert.AreEqual(1, server.StartCalls);
                Assert.IsFalse(lifetime.StopRequested, "a server that started is not stopped");
                Assert.AreEqual(0, Environment.ExitCode);
            }
            finally
            {
                Environment.ExitCode = previous;
            }
        }

        private static async Task AssertStopped(FakeServer server)
        {
            var lifetime = new FakeLifetime();
            var previous = Environment.ExitCode;

            try
            {
                Environment.ExitCode = 0;
                using var host = new RasaHost(server, lifetime);

                await host.StartAsync(CancellationToken.None);
                await host.ExecuteTask!;

                Assert.AreEqual(1, server.StartCalls);
                Assert.IsTrue(host.ExecuteTask.IsCompletedSuccessfully, "nothing thrown out of the host");
                Assert.IsTrue(lifetime.StopRequested, "the host is stopped");
                Assert.AreEqual(RasaHost.StartFailedExitCode, Environment.ExitCode);
            }
            finally
            {
                Environment.ExitCode = previous;
            }
        }

        private sealed class FakeServer : IRasaServer
        {
            public Func<bool> Starts { get; set; }

            public int StartCalls { get; private set; }

            public string ServerType => "Test";

            // Not running once started: the console loop ends at once.
            public bool Running => false;

            public bool Start()
            {
                StartCalls++;
                return Starts();
            }
        }

        private sealed class FakeLifetime : IHostApplicationLifetime
        {
            public bool StopRequested { get; private set; }

            public CancellationToken ApplicationStarted => CancellationToken.None;

            public CancellationToken ApplicationStopping => CancellationToken.None;

            public CancellationToken ApplicationStopped => CancellationToken.None;

            public void StopApplication() => StopRequested = true;
        }
    }
}
