using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions.Scenes
{
    using Rasa.Missions.Scenes;

    [TestClass]
    public class SceneRuntimeTests
    {
        [TestMethod]
        [DataRow(null, true)]
        [DataRow("enemy", true)]
        [DataRow("guide", false)]
        [DataRow("missing", false)]
        public void CombatIntentsValidateTheirActorRoles(string target, bool accepted)
        {
            var bindings = new SceneBindings("test",
                new Dictionary<string, SceneActorDefinition>
                {
                    ["guide"] = new("guide", SceneActorKind.Creature, 77, new ScenePosition(0, 0, 0)),
                    ["enemy"] = new("enemy", SceneActorKind.Creature, 78, new ScenePosition(1, 0, 0))
                }, new Dictionary<string, SceneRoute>(),
                new Dictionary<uint, SceneSequence>
                { [1] = new(new WorldIntent[] { new AttackActorIntent("engage", "guide", target) }) });

            var result = new SceneRuntime(new SceneScriptRegistry()).Evaluate(Run("data.sequence"), bindings,
                new SceneObservation(SceneEventKind.Signal, 1, SequenceId: 1), DateTime.UnixEpoch);

            Assert.AreEqual(accepted, result.Accepted);
        }

        [TestMethod]
        public void TypedScriptAndDataSequenceUseTheSameValidatedDecisionBoundary()
        {
            var registry = new SceneScriptRegistry();
            registry.Register("test.wait", 1, new WaitScript());
            var runtime = new SceneRuntime(registry);
            var run = Run("test.wait");
            var decision = runtime.Evaluate(run, Bindings(),
                new SceneObservation(SceneEventKind.Started, 1), DateTime.UnixEpoch);
            Assert.IsTrue(decision.Accepted);
            Assert.AreEqual(2, decision.Decision.Timers.Count);
            Assert.AreEqual("waiting", JsonDocument.Parse(decision.Decision.Checkpoint)
                .RootElement.GetProperty("phase").GetString());

            var sequence = runtime.Evaluate(Run("data.sequence"), Bindings(),
                new SceneObservation(SceneEventKind.Signal, 1, SequenceId: 1), DateTime.UnixEpoch);
            Assert.IsTrue(sequence.Accepted);
            Assert.IsInstanceOfType<EnsureActorIntent>(sequence.Decision.WorldIntents[0]);
        }

        [TestMethod]
        public void MissingVersionAndStaleCallbackCannotAdvanceTheRun()
        {
            var runtime = new SceneRuntime(new SceneScriptRegistry());
            Assert.IsFalse(runtime.Evaluate(Run("data.sequence") with { StateVersion = 2 }, Bindings(),
                new SceneObservation(SceneEventKind.Started, 1), DateTime.UnixEpoch).Accepted);
            Assert.IsFalse(runtime.Evaluate(Run("data.sequence"), Bindings(),
                new SceneObservation(SceneEventKind.RouteCompleted, 0), DateTime.UnixEpoch).Accepted);
        }

        [TestMethod]
        public void ScriptFaultsAreExplicitAndDoNotEscapeToOtherRuns()
        {
            var registry = new SceneScriptRegistry();
            registry.Register("test.throw", 1, new ThrowScript());
            var runtime = new SceneRuntime(registry);
            var result = runtime.Evaluate(Run("test.throw"), Bindings(),
                new SceneObservation(SceneEventKind.Started, 1), DateTime.UnixEpoch);
            Assert.AreEqual(SceneStatus.Faulted, result.Decision.Status);
            StringAssert.Contains(result.Decision.Fault, "test script failure");
            Assert.IsTrue(runtime.Evaluate(Run("data.sequence"), Bindings(),
                new SceneObservation(SceneEventKind.Signal, 1, SequenceId: 1), DateTime.UnixEpoch).Accepted);
        }

        [TestMethod]
        public void DuplicateScriptKeysAndUnknownActorRolesAreRejected()
        {
            var registry = new SceneScriptRegistry();
            Assert.ThrowsExactly<ArgumentException>(() => registry.Register("data.sequence", 1, new WaitScript()));
            var invalid = new SceneBindings("test", new Dictionary<string, SceneActorDefinition>(),
                new Dictionary<string, SceneRoute>(), new Dictionary<uint, SceneSequence>
                {
                    [1] = new(new WorldIntent[] { new EnsureActorIntent("spawn", "missing") })
                });
            var result = new SceneRuntime(registry).Evaluate(Run("data.sequence"), invalid,
                new SceneObservation(SceneEventKind.Signal, 1, SequenceId: 1), DateTime.UnixEpoch);
            Assert.IsFalse(result.Accepted);
            StringAssert.Contains(result.Rejection, "missing");
        }

        private static SceneRun Run(string script) =>
            new(Guid.NewGuid().ToString("N"), "test", script, 1, 1, 0, "{}", SceneStatus.Running, 1);

        private static SceneBindings Bindings() => new("test",
            new Dictionary<string, SceneActorDefinition> { ["guide"] = new("guide", SceneActorKind.PublicSpawn, 77) },
            new Dictionary<string, SceneRoute>(),
            new Dictionary<uint, SceneSequence>
            {
                [1] = new(new WorldIntent[] { new EnsureActorIntent("spawn", "guide") })
            });

        private sealed class WaitScript : ISceneScript
        {
            public SceneDecision Handle(SceneContext context, SceneObservation observation) =>
                new("{\"phase\":\"waiting\"}", timers: new[]
                {
                    new SceneTimerChange("bomb", SceneClockPolicy.WallClock, context.UtcNow.AddSeconds(600)),
                    new SceneTimerChange("dialogue", SceneClockPolicy.ActiveScene, context.UtcNow.AddSeconds(2))
                });
        }

        private sealed class ThrowScript : ISceneScript
        {
            public SceneDecision Handle(SceneContext context, SceneObservation observation) =>
                throw new InvalidOperationException("test script failure");
        }
    }
}
