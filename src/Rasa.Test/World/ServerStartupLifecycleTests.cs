using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Game;

    [TestClass]
    [DoNotParallelize]
    public class ServerStartupLifecycleTests
    {
        [TestMethod]
        public void MissionValidationFailureStopsBeforeLoopNetworkAcceptAndReadyPublication()
        {
            var calls = new List<string>();
            var lifecycle = new ServerStartupLifecycle(
                validateMissionReadiness: () =>
                {
                    calls.Add("validate-missions");
                    return false;
                },
                validateLootReadiness: () =>
                {
                    calls.Add("validate-loot");
                    return true;
                },
                startLoop: () => calls.Add("start-loop"),
                setupCommunicator: () => calls.Add("setup-communicator"),
                createListener: () => calls.Add("create-listener"),
                registerLoginAndQueue: () => calls.Add("register-login-queue"),
                beginAccept: () => calls.Add("begin-accept"),
                registerTimers: () => calls.Add("register-timers"),
                loadRemainingData: () => calls.Add("load-remaining"),
                publishReady: () => calls.Add("publish-ready"),
                cleanup: () => calls.Add("cleanup"));

            Assert.IsFalse(lifecycle.Start());
            CollectionAssert.AreEqual(
                new[] { "validate-missions" },
                calls.ToArray());
        }

        [TestMethod]
        public void SuccessfulStartupLoadsTheWorldBeforeAcceptLoopAndCommunicator()
        {
            var calls = new List<string>();
            var lifecycle = new ServerStartupLifecycle(
                validateMissionReadiness: () =>
                {
                    calls.Add("validate-missions");
                    return true;
                },
                validateLootReadiness: () =>
                {
                    calls.Add("validate-loot");
                    return true;
                },
                startLoop: () => calls.Add("start-loop"),
                setupCommunicator: () => calls.Add("setup-communicator"),
                createListener: () => calls.Add("create-listener"),
                registerLoginAndQueue: () => calls.Add("register-login-queue"),
                beginAccept: () => calls.Add("begin-accept"),
                registerTimers: () => calls.Add("register-timers"),
                loadRemainingData: () => calls.Add("load-remaining"),
                publishReady: () => calls.Add("publish-ready"),
                cleanup: () => calls.Add("cleanup"));

            Assert.IsTrue(lifecycle.Start());
            CollectionAssert.AreEqual(
                new[]
                {
                    "validate-missions",
                    "validate-loot",
                    "load-remaining",
                    "create-listener",
                    "register-login-queue",
                    "register-timers",
                    "begin-accept",
                    "start-loop",
                    "setup-communicator",
                    "publish-ready"
                },
                calls.ToArray());
        }

        [TestMethod]
        public void StartupFailureAfterResourcesBeginCleansUpAndNeverPublishesReady()
        {
            var calls = new List<string>();
            var lifecycle = new ServerStartupLifecycle(
                validateMissionReadiness: () =>
                {
                    calls.Add("validate-missions");
                    return true;
                },
                validateLootReadiness: () =>
                {
                    calls.Add("validate-loot");
                    return true;
                },
                startLoop: () => calls.Add("start-loop"),
                setupCommunicator: () => calls.Add("setup-communicator"),
                createListener: () =>
                {
                    calls.Add("create-listener");
                    throw new InvalidOperationException("boom");
                },
                registerLoginAndQueue: () => calls.Add("register-login-queue"),
                beginAccept: () => calls.Add("begin-accept"),
                registerTimers: () => calls.Add("register-timers"),
                loadRemainingData: () => calls.Add("load-remaining"),
                publishReady: () => calls.Add("publish-ready"),
                cleanup: () => calls.Add("cleanup"));

            Assert.IsFalse(lifecycle.Start());
            CollectionAssert.AreEqual(
                new[]
                {
                    "validate-missions",
                    "validate-loot",
                    "load-remaining",
                    "create-listener",
                    "cleanup"
                },
                calls.ToArray());
        }

        [TestMethod]
        public void AFailedStartReportsTheStepAndTheException()
        {
            var calls = new List<string>();
            var reports = new List<(string Step, Exception Error)>();
            var boom = new InvalidOperationException("Address already in use");
            var lifecycle = Lifecycle(calls, reports, createListener: () => throw boom);

            Assert.IsFalse(lifecycle.Start());
            Assert.HasCount(1, reports);
            Assert.AreEqual("opening the client port", reports[0].Step);
            Assert.AreSame(boom, reports[0].Error);
            CollectionAssert.AreEqual(new[] { "validate-missions", "validate-loot", "load-remaining", "cleanup" }, calls.ToArray());
        }

        [TestMethod]
        public void AnExceptionFromTheMissionCheckIsReportedAndCleanedUpNotThrown()
        {
            // The mission check is the first read of the world database: on MySQL, a table a
            // migration was never run for fails here. It was outside the try, so the exception
            // left Start, ended the host's ExecuteAsync and was never shown.
            var calls = new List<string>();
            var reports = new List<(string Step, Exception Error)>();
            var missing = new InvalidOperationException("Table 'world.entityclass' doesn't exist");
            var lifecycle = Lifecycle(calls, reports, validate: () => throw missing);

            Assert.IsFalse(lifecycle.Start());
            Assert.HasCount(1, reports);
            Assert.AreEqual("loading the entity classes and checking the missions", reports[0].Step);
            Assert.AreSame(missing, reports[0].Error);
            CollectionAssert.AreEqual(new[] { "cleanup" }, calls.ToArray());
        }

        [TestMethod]
        public void ACleanupThatFailsIsReportedAfterTheFailureThatStartedIt()
        {
            var calls = new List<string>();
            var reports = new List<(string Step, Exception Error)>();
            var first = new InvalidOperationException("navmesh path not found");
            var second = new ObjectDisposedException("socket");
            var lifecycle = Lifecycle(calls, reports, loadRemaining: () => throw first, cleanup: () => throw second);

            Assert.IsFalse(lifecycle.Start());
            Assert.HasCount(2, reports);
            Assert.AreEqual("loading the world", reports[0].Step);
            Assert.AreSame(first, reports[0].Error);
            Assert.AreEqual("cleaning up after the failed start", reports[1].Step);
            Assert.AreSame(second, reports[1].Error);
        }

        [TestMethod]
        public void EachStepIsNamedForWhatFailed()
        {
            var steps = new Dictionary<string, string>
            {
                ["load-remaining"] = "loading the world",
                ["create-listener"] = "opening the client port",
                ["register-login-queue"] = "setting up the login queue",
                ["register-timers"] = "starting the timers",
                ["begin-accept"] = "accepting clients",
                ["start-loop"] = "starting the world loop",
                ["setup-communicator"] = "setting up the link to the auth server",
                ["publish-ready"] = "announcing the server ready",
            };

            foreach (var (failing, expected) in steps)
            {
                var calls = new List<string>();
                var reports = new List<(string Step, Exception Error)>();
                var lifecycle = Lifecycle(calls, reports, failing: failing);

                Assert.IsFalse(lifecycle.Start(), failing);
                Assert.AreEqual(expected, reports.Single().Step, failing);
                Assert.AreEqual("cleanup", calls.Last(), failing);
                Assert.IsFalse(calls.Contains("publish-ready") && failing != "publish-ready", failing);
            }
        }

        /// <summary>A lifecycle that records its calls and reports, with the named step throwing, or the actions given.</summary>
        private static ServerStartupLifecycle Lifecycle(List<string> calls, List<(string Step, Exception Error)> reports,
            string failing = null, Func<bool> validate = null, Action createListener = null, Action loadRemaining = null, Action cleanup = null)
        {
            Action Step(string name) => () =>
            {
                calls.Add(name);

                if (name == failing)
                    throw new InvalidOperationException(name);
            };

            return new ServerStartupLifecycle(
                validateMissionReadiness: validate ?? (() =>
                {
                    calls.Add("validate-missions");
                    return true;
                }),
                validateLootReadiness: () =>
                {
                    calls.Add("validate-loot");
                    return true;
                },
                startLoop: Step("start-loop"),
                setupCommunicator: Step("setup-communicator"),
                createListener: createListener ?? Step("create-listener"),
                registerLoginAndQueue: Step("register-login-queue"),
                beginAccept: Step("begin-accept"),
                registerTimers: Step("register-timers"),
                loadRemainingData: loadRemaining ?? Step("load-remaining"),
                publishReady: Step("publish-ready"),
                cleanup: cleanup ?? (() => calls.Add("cleanup")),
                reportFailure: (step, error) => reports.Add((step, error)));
        }
    }
}
