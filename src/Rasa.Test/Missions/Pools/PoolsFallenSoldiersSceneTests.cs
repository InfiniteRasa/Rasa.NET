using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Missions.Content.Pools;
using Rasa.Missions.Scenes;

namespace Rasa.Test.Missions.Pools
{
    /// <summary>
    /// The tag draw of Death Patrol's and More Clips and Tags' bodies (PoolsFallenSoldiersScene).
    ///
    /// Why these tests: the user's choice is random tags with no body the player must find. A draw that gives fewer than
    /// 8 tags by the 16th body leaves the mission stuck (the 16th clip closes the bodies), one that gives a 9th tag or
    /// the same bodies every run breaks the choice, and one that changes on a retry of the same search splits a
    /// player's items from their progress.
    /// </summary>
    [TestClass]
    public class PoolsFallenSoldiersSceneTests
    {
        [TestMethod]
        public void EveryRunFindsEightTagsByTheSixteenthBody()
        {
            for (var run = 0; run < 2000; run++)
            {
                var id = $"run-{run}";
                var tags = 0U;
                for (var searched = 0U; searched < PoolsFallenSoldiersScene.Clips; searched++)
                {
                    if (PoolsFallenSoldiersScene.HasTag(id, searched + 3, searched, tags))
                        tags++;
                    Assert.IsTrue(tags + PoolsFallenSoldiersScene.Clips - searched - 1 >= PoolsFallenSoldiersScene.Tags,
                        $"{id}: tags out of reach after {searched + 1}");
                }
                Assert.AreEqual(PoolsFallenSoldiersScene.Tags, tags, id);
            }
        }

        [TestMethod]
        public void NoTagAfterTheEighthOrTheSixteenthBody()
        {
            for (var body = 1U; body <= 24; body++)
            {
                Assert.IsFalse(PoolsFallenSoldiersScene.HasTag("run", body, 3, PoolsFallenSoldiersScene.Tags), "eight found");
                Assert.IsFalse(PoolsFallenSoldiersScene.HasTag("run", body, PoolsFallenSoldiersScene.Clips, 0), "search over");
            }
        }

        [TestMethod]
        public void TheTagsLieOnOtherBodiesEachRun()
        {
            var patterns = new HashSet<string>();
            var perPosition = new int[PoolsFallenSoldiersScene.Clips];
            const int runs = 2000;
            for (var run = 0; run < runs; run++)
            {
                var id = Guid.NewGuid().ToString("N");
                var tags = 0U;
                var pattern = new char[PoolsFallenSoldiersScene.Clips];
                for (var searched = 0U; searched < PoolsFallenSoldiersScene.Clips; searched++)
                {
                    var drawn = PoolsFallenSoldiersScene.HasTag(id, searched + 1, searched, tags);
                    pattern[searched] = drawn ? 'T' : '-';
                    if (!drawn)
                        continue;
                    tags++;
                    perPosition[searched]++;
                }
                patterns.Add(new string(pattern));
            }
            Assert.IsTrue(patterns.Count > runs / 2, $"{patterns.Count} patterns in {runs} runs");

            // Each search has the same 8-in-16 chance of a tag: half the runs, within a wide margin.
            foreach (var count in perPosition)
                Assert.IsTrue(count > runs * 0.4 && count < runs * 0.6, string.Join(",", perPosition));
        }

        /// <summary>The same search handled twice from the same checkpoint decides the same: a retry gives what the first try gave.</summary>
        [TestMethod]
        public void ARetryOfASearchDecidesTheSame()
        {
            var sequences = new Dictionary<uint, SceneSequence> { [0] = new SceneSequence() };
            for (var body = 1U; body <= 22; body++)
            {
                sequences[body] = new SceneSequence(signals: new[] { new SceneMissionSignal(1315, 1, body) });
                sequences[PoolsFallenSoldiersScene.TagFrom + body] =
                    new SceneSequence(signals: new[] { new SceneMissionSignal(1315, 2, body) });
            }
            var bindings = new SceneBindings("pools_1_16_5", new Dictionary<string, SceneActorDefinition>(),
                new Dictionary<string, SceneRoute>(), sequences);
            var script = new PoolsFallenSoldiersScene();
            var tags = 0;
            for (var run = 0; run < 50; run++)
            {
                var checkpoint = "{}";
                var context = Context(bindings, $"run-{run}", checkpoint);
                checkpoint = script.Handle(context, new SceneObservation(SceneEventKind.Started, 1)).Checkpoint;
                for (var body = 1U; body <= PoolsFallenSoldiersScene.Clips; body++)
                {
                    context = Context(bindings, $"run-{run}", checkpoint);
                    var use = new SceneObservation(SceneEventKind.Signal, 1, SequenceId: body);
                    var first = script.Handle(context, use);
                    var retry = script.Handle(context, use);
                    Assert.AreEqual(first.Checkpoint, retry.Checkpoint, $"run {run} body {body}");
                    CollectionAssert.AreEqual(first.Signals.ToArray(), retry.Signals.ToArray(), $"run {run} body {body}");
                    tags += first.Signals.Count(signal => signal.SequenceId == 2);
                    checkpoint = first.Checkpoint;

                    // The same body again, once searched, decides nothing.
                    var again = script.Handle(Context(bindings, $"run-{run}", checkpoint), use);
                    Assert.AreEqual(checkpoint, again.Checkpoint);
                    Assert.AreEqual(0, again.Signals.Count);
                }
            }
            Assert.AreEqual(50 * (int)PoolsFallenSoldiersScene.Tags, tags, "eight tag signals a run");
        }

        private static SceneContext Context(SceneBindings bindings, string runId, string checkpoint) =>
            new(new SceneRun(runId, "pools_1_16_5", "pools.fallen-soldiers", 1, 1, 1, checkpoint, SceneStatus.Running, 7, 1315),
                bindings, new DateTime(2026, 10, 25, 0, 0, 0, DateTimeKind.Utc));
    }
}
