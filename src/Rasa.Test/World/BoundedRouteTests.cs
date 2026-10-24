using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Config;
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Navigation;
    using Rasa.Structures;

    /// <summary>
    /// Every route a creature asks for searched as a chase's is (CreatureAi.BoundedPathSearches):
    /// a goal no route reaches only so far, and the corridor found toward it kept by the map's
    /// navmesh for the next to ask between the same polygons. A goal a route reaches gets the
    /// route it always did; only the ones that are never reached are searched differently.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class BoundedRouteTests
    {
        private const long Tick = 100;

        /// <summary>On Wilderness's main walkable region, south of a patch of ground cut off from it (ChaseSearchBudgetTests).</summary>
        private static readonly Vector3 Mainland = new Vector3(75.39f, 173.32f, 245.75f);

        /// <summary>That patch, 7.1 m away: no route from the rest of the map reaches it.</summary>
        private static readonly Vector3 Island = new Vector3(75.77f, 173.12f, 238.62f);

        private static NavMeshQuery Map(string name) => TestNavMeshes.Query(NavMeshFile.PathFor(AiCostBenchmarks.Navmesh(), name));

        /// <summary>
        /// The decisions replayed both ways, on the two largest open maps: strolls and returns of up
        /// to 60 m, and goals anywhere on the map. Where the goal can be reached, the whole search
        /// and the bounded one remembering what it found give the same route; where it cannot,
        /// both say so, and the bounded one asked again hands back what it found the first time.
        /// </summary>
        [TestMethod]
        public void EveryRouteThatArrivesIsTheSameBoundedOrNot()
        {
            foreach (var name in new[] { "adv_foreas_concordia_wilderness", "adv_foreas_concordia_divide" })
            {
                var whole = Map(name);
                var bounded = Map(name);
                var reachable = 0;
                var unreachable = 0;
                var pairs = AiCostBenchmarks.RandomPairs(whole, 600, 21, within: 60)
                    .Concat(AiCostBenchmarks.RandomPairs(whole, 120, 23));

                foreach (var (from, to) in pairs)
                {
                    var wholePath = whole.FindPath(from, to, out var wholeComplete);
                    var boundedPath = bounded.FindPathBounded(from, to, out var boundedComplete, remember: true);

                    Assert.AreEqual(wholeComplete, boundedComplete, $"{name}: {from} to {to}");

                    if (wholeComplete)
                    {
                        CollectionAssert.AreEqual(wholePath, boundedPath, $"{name}: {from} to {to}");
                        reachable++;
                        continue;
                    }

                    unreachable++;
                    var again = bounded.FindPathBounded(from, to, out var againComplete, remember: true);

                    Assert.IsFalse(againComplete);
                    Assert.AreEqual(0, bounded.LastSearchNodes, "asked again, it is not searched again");
                    CollectionAssert.AreEqual(boundedPath, again, $"{name}: {from} to {to}, asked again");
                }

                Console.WriteLine($"{name}: {reachable} routes that arrive, {unreachable} that cannot, {bounded.UnreachableRecalled} recalled");
                Assert.IsTrue(reachable > 500, $"{reachable}");
            }
        }

        [TestMethod]
        public void AGoalNoRouteReachesIsRememberedAndRecalled()
        {
            var query = Map("adv_foreas_concordia_wilderness");
            var searches = query.Searches;

            var first = query.FindPathBounded(Mainland, Island, out var complete, remember: true);
            var nodes = query.LastSearchNodes;
            var second = query.FindPathBounded(Mainland, Island, out var completeAgain, remember: true);

            Assert.IsFalse(complete);
            Assert.IsFalse(completeAgain);
            Assert.IsTrue(nodes > 0 && nodes <= 2 * NavMeshQuery.UnreachableSearchNodes, $"{nodes}");
            Assert.AreEqual(searches + 1, query.Searches, "one search for both");
            Assert.AreEqual(1, query.UnreachableRecalled);
            CollectionAssert.AreEqual(first, second);

            // Not remembered unless asked to, and a different budget is a different search.
            query.FindPathBounded(Mainland, Island, out _);
            query.FindPathBounded(Mainland, Island, out _, 256, remember: true);
            Assert.AreEqual(searches + 3, query.Searches);
        }

        [TestMethod]
        public void TheMemoryKeepsOnlySoManyCorridors()
        {
            var query = Map("adv_foreas_concordia_wilderness");
            var asked = 0;

            foreach (var (from, to) in AiCostBenchmarks.RandomPairs(query, 20000, 31))
            {
                if (query.Reachable(from, to) != false)
                    continue;

                query.FindPathBounded(from, to, out _, 64, remember: true);

                if (++asked > NavMeshQuery.UnreachableMemory + 20)
                    break;
            }

            Assert.IsTrue(asked > NavMeshQuery.UnreachableMemory, $"{asked} goals no route reaches");
            Assert.IsTrue(query.UnreachableRemembered <= NavMeshQuery.UnreachableMemory);
        }

        /// <summary>
        /// A minion following its master onto ground no route reaches re-plans every 500 ms. Each
        /// re-plan walked the whole of its region; bounded, it searches only so far, and once it
        /// stands at the end of the way it found, not at all.
        /// </summary>
        [TestMethod]
        public void AFollowerOfSomeoneItCannotReachDoesNotWalkTheWholeMap()
        {
            // A master standing 15 m or more away, on ground no route from the follower reaches.
            var query = Map("adv_foreas_concordia_wilderness");
            var master = AiCostBenchmarks.RandomPairs(query, 20000, 41)
                .Select(pair => pair.To)
                .First(to => query.Reachable(Mainland, to) == false && Vector3.Distance(Mainland, to) is > 15 and < 80);

            var bounded = Follow(boundedSearches: true, master);
            var whole = Follow(boundedSearches: false, master);

            Console.WriteLine($"master at {master}; bounded: {bounded.Count} asks, {string.Join(",", bounded)}; whole: {whole.Count} asks, {string.Join(",", whole)}");

            Assert.IsTrue(whole.Any(nodes => nodes > 25_000), "the whole search walks the region");
            Assert.IsTrue(bounded.All(nodes => nodes <= 2 * NavMeshQuery.UnreachableSearchNodes), string.Join(",", bounded));
            Assert.IsTrue(bounded.Sum() * 4 < whole.Sum(), $"{bounded.Sum()} nodes against {whole.Sum()}");
        }

        [TestMethod]
        public void AScriptedMoveToWhereNoRouteGoesIsStillRefused()
        {
            using var world = new WorldTestContext();
            world.Map.NavMesh = Map("adv_foreas_concordia_wilderness");
            var creature = Spawn(world, Mainland);
            var searches = world.Map.NavMesh.Searches;

            Assert.IsFalse(BehaviorManager.Instance.SetActionScriptedMove(world.Map, creature, Island, 0));
            Assert.IsTrue(world.Map.NavMesh.LastSearchNodes <= 2 * NavMeshQuery.UnreachableSearchNodes, $"{world.Map.NavMesh.LastSearchNodes}");
            Assert.AreEqual(searches + 1, world.Map.NavMesh.Searches);

            var reachable = new Vector3(Mainland.X + 6, Mainland.Y, Mainland.Z + 6);
            var expected = world.Map.NavMesh.FindPath(creature.Position, reachable, out var complete);
            Assume(complete);
            Assert.IsTrue(BehaviorManager.Instance.SetActionScriptedMove(world.Map, creature, expected[^1], 0));
            CollectionAssert.AreEqual(world.Map.NavMesh.FindPath(creature.Position, expected[^1], out _), creature.Controller.Path);
        }

        private static void Assume(bool condition)
        {
            if (!condition)
                Assert.Inconclusive("the fixture's point is not reachable on this navmesh");
        }

        /// <summary>The nodes of each search a follower made over 6 s of passes, its master standing where no route goes.</summary>
        private static List<int> Follow(bool boundedSearches, Vector3 at)
        {
            var previous = BehaviorManager.Config;
            BehaviorManager.Config = new CreatureAiConfig { BoundedPathSearches = boundedSearches };

            try
            {
                using var world = new WorldTestContext();
                world.Map.NavMesh = Map("adv_foreas_concordia_wilderness");
                var master = world.CreateClient(x: at.X);
                master.Player.Position = at;
                var creature = Spawn(world, Mainland);
                BehaviorManager.Instance.SetActionFollow(creature, master.Player.EntityId);

                var nodes = new List<int>();

                for (var ms = 0L; ms < 6000; ms += Tick)
                {
                    var searches = world.Map.NavMesh.Searches;
                    var recalled = world.Map.NavMesh.UnreachableRecalled;
                    BehaviorManager.Instance.MapChannelThink(world.Map, Tick);

                    if (world.Map.NavMesh.Searches != searches)
                        nodes.Add(world.Map.NavMesh.LastSearchNodes);
                    else if (world.Map.NavMesh.UnreachableRecalled != recalled)
                        nodes.Add(0);
                }

                Assert.AreEqual(BehaviorManager.BehaviorActionFollow, creature.Controller.CurrentAction);
                return nodes;
            }
            finally
            {
                BehaviorManager.Config = previous;
            }
        }

        private static Creature Spawn(WorldTestContext world, Vector3 at)
        {
            var creature = new Creature
            {
                Name = "Follower",
                TargetCategory = TargetCategory.Hostile,
                MapContextId = world.Map.MapInfo.MapContextId,
                RuntimeMapChannel = world.Map,
                Position = at,
                EntityClass = EntityClasses.HumanBaseMale,
                State = CharacterState.Idle,
                Level = 1,
                AggroRange = 0,
                WalkSpeed = 5,
                RunSpeed = 6,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>()
            };

            creature.HomePos.Position = creature.Position;
            creature.HomePos.MapContextid = creature.MapContextId;
            creature.Attributes[Attributes.Health] = CreatureHealth.NewAttribute(1000);
            BehaviorManager.StartWandering(creature, false);
            EntityManager.Instance.RegisterEntity(creature.EntityId, EntityType.Creature);
            EntityManager.Instance.RegisterCreature(creature);
            EntityManager.Instance.RegisterActor(creature.EntityId, creature);
            var seed = CellManager.Instance.GetCellSeed(creature.Position);
            creature.Cells = CellManager.Instance.CreateCellMatrix(world.Map, seed & 0xFFFF, seed >> 16);
            CellManager.Instance.GetCell(world.Map, seed & 0xFFFF, seed >> 16).CreatureList.Add(creature);
            return creature;
        }
    }
}
