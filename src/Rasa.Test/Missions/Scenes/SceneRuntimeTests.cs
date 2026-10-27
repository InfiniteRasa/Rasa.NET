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
        [DataRow("enemy", true)]
        [DataRow("door", false)]
        [DataRow("ranger", false)]
        [DataRow("missing", false)]
        public void AKillIntentNamesACreatureActor(string role, bool accepted)
        {
            var bindings = new SceneBindings("test",
                new Dictionary<string, SceneActorDefinition>
                {
                    ["enemy"] = new("enemy", SceneActorKind.Creature, 78, new ScenePosition(1, 0, 0)),
                    ["door"] = new("door", SceneActorKind.Object, 79, new ScenePosition(2, 0, 0)),
                    ["ranger"] = new("ranger", SceneActorKind.PublicSpawn, 80, new ScenePosition(3, 0, 0))
                }, new Dictionary<string, SceneRoute>(),
                new Dictionary<uint, SceneSequence>
                { [1] = new(new WorldIntent[] { new KillActorIntent("fell", role) }) });

            var result = new SceneRuntime(new SceneScriptRegistry()).Evaluate(Run("data.sequence"), bindings,
                new SceneObservation(SceneEventKind.Signal, 1, SequenceId: 1), DateTime.UnixEpoch);

            Assert.AreEqual(accepted, result.Accepted);

            // The scene JSON carries it.
            var json = System.Text.Json.JsonSerializer.Serialize<WorldIntent>(new KillActorIntent("fell", "enemy"));
            StringAssert.Contains(json, "\"$kind\":\"kill\"");
            Assert.AreEqual(new KillActorIntent("fell", "enemy"), System.Text.Json.JsonSerializer.Deserialize<WorldIntent>(json));
        }

        /// <summary>
        /// The opt-in attack and escort options (AttackActorIntent.HoldTarget and PastLeash, RunRouteIntent.FightWhenHit)
        /// are off unless set, are left out of the effect journal's JSON when off (journals written before them read the
        /// same), and come back from it when on: a map made again applies the order as given.
        /// </summary>
        [TestMethod]
        public void OptInAttackAndEscortOptionsAreOffByDefaultAndSurviveTheJournal()
        {
            var plainAttack = new AttackActorIntent("fight", "enemy", "victim");
            var plainRoute = new RunRouteIntent("walk", "escort", "out", ResumeAfterCombat: true);
            Assert.IsFalse(plainAttack.HoldTarget || plainAttack.PastLeash);
            Assert.IsFalse(plainRoute.FightWhenHit);
            var plainAttackJson = System.Text.Json.JsonSerializer.Serialize<WorldIntent>(plainAttack);
            var plainRouteJson = System.Text.Json.JsonSerializer.Serialize<WorldIntent>(plainRoute);
            StringAssert.DoesNotMatch(plainAttackJson, new System.Text.RegularExpressions.Regex("HoldTarget|PastLeash"));
            StringAssert.DoesNotMatch(plainRouteJson, new System.Text.RegularExpressions.Regex("FightWhenHit"));
            Assert.AreEqual(plainAttack, System.Text.Json.JsonSerializer.Deserialize<WorldIntent>(plainAttackJson));
            Assert.AreEqual(plainRoute, System.Text.Json.JsonSerializer.Deserialize<WorldIntent>(plainRouteJson));

            WorldIntent[] set =
            {
                new AttackActorIntent("hold", "enemy", "victim", HoldTarget: true),
                new AttackActorIntent("chase", "enemy", PastLeash: true),
                new RunRouteIntent("walk", "escort", "out", ResumeAfterCombat: true, FightWhenHit: true),
            };
            foreach (var intent in set)
                Assert.AreEqual(intent, System.Text.Json.JsonSerializer.Deserialize<WorldIntent>(
                    System.Text.Json.JsonSerializer.Serialize(intent)), intent.OperationKey);
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
