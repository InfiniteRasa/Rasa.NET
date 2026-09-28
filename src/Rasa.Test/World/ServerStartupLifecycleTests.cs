using System;
using System.Collections.Generic;

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
        public void SuccessfulStartupValidatesMissionContentBeforeLoopCommunicatorAndAccept()
        {
            var calls = new List<string>();
            var lifecycle = new ServerStartupLifecycle(
                validateMissionReadiness: () =>
                {
                    calls.Add("validate-missions");
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
                    "start-loop",
                    "setup-communicator",
                    "create-listener",
                    "register-login-queue",
                    "begin-accept",
                    "register-timers",
                    "load-remaining",
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
                    "start-loop",
                    "setup-communicator",
                    "create-listener",
                    "cleanup"
                },
                calls.ToArray());
        }
    }
}
