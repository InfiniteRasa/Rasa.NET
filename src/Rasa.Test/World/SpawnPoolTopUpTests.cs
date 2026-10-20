using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Managers;
    using Rasa.Structures;
    using Rasa.Test.Missions.Wilderness;

    /// <summary>
    /// A pool with survivors gets back what it lost (SpawnPoolManager.TopUp): its respawn time
    /// after its first loss, by creature row against its last full spawn, and not while one of the
    /// survivors is fighting. A garrison and a squad's instance still come back only whole.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class SpawnPoolTopUpTests
    {
        /// <summary>Alia Das: four of creature 51 and four of 53, exactly, back 2.5 s after.</summary>
        private const uint Pool = 66;
        private const uint First = 51, Second = 53;

        private static SpawnPool PoolOf(WildernessRuntimeTestHarness harness) =>
            harness.Map.SpawnPools.Single(pool => pool.DbId == Pool);

        private static List<Creature> Standing(WildernessRuntimeTestHarness harness, SpawnPool pool) =>
            EntityManager.Instance.Creatures.Values
                .Where(creature => ReferenceEquals(creature.SpawnPool, pool) && CreatureManager.IsLivingOnMap(harness.Map, creature))
                .ToList();

        private static (int First, int Second) Rows(List<Creature> creatures) =>
            (creatures.Count(creature => creature.DbId == First), creatures.Count(creature => creature.DbId == Second));

        private static SpawnPool Spawned(WildernessRuntimeTestHarness harness)
        {
            harness.SpawnWorld(Pool);
            var pool = PoolOf(harness);

            Assert.AreEqual(8, pool.AliveCreatures);
            Assert.AreEqual((4, 4), Rows(Standing(harness, pool)));
            CollectionAssert.AreEqual(new[] { (First, 4), (Second, 4) }, pool.Complement.ToArray());
            Assert.AreEqual(2500L, pool.RespawnTime);

            return pool;
        }

        private static void Kill(WildernessRuntimeTestHarness harness, SpawnPool pool, uint creatureId, int count)
        {
            foreach (var creature in Standing(harness, pool).Where(creature => creature.DbId == creatureId).Take(count).ToList())
                CreatureManager.Instance.HandleCreatureKill(harness.Map, creature, null);
        }

        [TestMethod]
        public void APoolWithSurvivorsGetsBackWhatItLostItsRespawnTimeAfterItsFirstLoss()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pool = Spawned(harness);

            Kill(harness, pool, First, 1);
            Assert.AreEqual(0L, pool.UpdateTimer, "the first loss starts the time");

            harness.SpawnWorldAfter(1000, Pool);
            Kill(harness, pool, First, 1);
            Assert.AreEqual(1000L, pool.UpdateTimer, "a second loss does not start it again");
            Assert.AreEqual(6, pool.AliveCreatures);

            harness.SpawnWorldAfter(1499, Pool);
            Assert.AreEqual(6, pool.AliveCreatures, "not yet");

            harness.SpawnWorldAfter(1, Pool);
            Assert.AreEqual(8, pool.AliveCreatures);
            Assert.AreEqual((4, 4), Rows(Standing(harness, pool)), "what it lost, by row");

            // Whole again: nothing more comes.
            harness.SpawnWorldAfter(10_000, Pool);
            Assert.AreEqual(8, pool.AliveCreatures);
        }

        [TestMethod]
        public void NothingComesBackWhileASurvivorIsFighting()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pool = Spawned(harness);

            Kill(harness, pool, Second, 3);
            var fighter = Standing(harness, pool).First();
            var was = fighter.Controller.CurrentAction;
            fighter.Controller.CurrentAction = BehaviorManager.BehaviorActionFighting;

            harness.SpawnWorldAfter(2500, Pool);
            Assert.AreEqual(5, pool.AliveCreatures, "held back by the fight");
            Assert.AreEqual(System.Math.Max(0, pool.RespawnTime - SpawnPoolManager.TopUpRecheckMs), pool.UpdateTimer, "looked at again later");

            fighter.Controller.CurrentAction = was;
            harness.SpawnWorldAfter(2500, Pool);
            Assert.AreEqual(8, pool.AliveCreatures);
            Assert.AreEqual((4, 4), Rows(Standing(harness, pool)));
        }

        [TestMethod]
        public void AGarrisonStillComesBackOnlyWhole()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pool = Spawned(harness);
            pool.IsGarrison = true;

            try
            {
                Kill(harness, pool, First, 2);
                harness.SpawnWorldAfter(60_000, Pool);
                Assert.AreEqual(6, pool.AliveCreatures);
            }
            finally
            {
                pool.IsGarrison = false;
            }
        }

        [TestMethod]
        public void WhatIsMissingIsTakenByRowAndNoMoreThanThePoolIsShort()
        {
            var definitions = new Dictionary<uint, Creature>
            {
                [First] = new Creature { DbId = First },
                [Second] = new Creature { DbId = Second }
            };
            var complement = new[] { (First, 4), (Second, 4) };
            List<Creature> Of(uint id, int count) => Enumerable.Range(0, count).Select(_ => new Creature { DbId = id }).ToList();

            var missing = SpawnPoolManager.Missing(complement, Of(First, 2).Concat(Of(Second, 3)), 3, definitions);
            CollectionAssert.AreEqual(new[] { First, First, Second }, missing.Select(creature => creature.DbId).ToArray());
            Assert.IsTrue(missing.All(creature => ReferenceEquals(creature, definitions[creature.DbId])), "the loaded templates, as a full spawn takes");

            // One of the pool stands for another (a grub's adult): counted by the pool, not by a
            // row, so the cap is what holds it to its size.
            missing = SpawnPoolManager.Missing(complement, Of(First, 3).Concat(Of(Second, 4)).Concat(Of(999, 1)), 0, definitions);
            Assert.IsEmpty(missing);

            missing = SpawnPoolManager.Missing(complement, Of(First, 3).Concat(Of(Second, 3)).Concat(Of(999, 1)), 1, definitions);
            CollectionAssert.AreEqual(new[] { First }, missing.Select(creature => creature.DbId).ToArray());
        }
    }
}
