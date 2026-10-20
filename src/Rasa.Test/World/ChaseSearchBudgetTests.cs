using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using DotRecast.Core;
using DotRecast.Core.Numerics;
using DotRecast.Detour;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Navigation;
    using Rasa.Structures;

    /// <summary>
    /// A creature chasing a target no route reaches searched the navmesh on every think, and each
    /// search walked the whole of the creature's walkable region before it gave up - 30,554
    /// polygons on Wilderness. The mesh's regions are worked out once (NavMeshQuery.Reachable); a
    /// chase to a goal in another region is searched only so far (FindPathBounded), and searched
    /// again only when the path update lock is up (BehaviorManager, RouteIncomplete). A goal that can
    /// be reached gets the search, and the route, it always did.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class ChaseSearchBudgetTests
    {
        private const long Tick = 100;

        /// <summary>On Wilderness's main walkable region, south of a patch of ground cut off from it.</summary>
        private static readonly Vector3 Mainland = new Vector3(75.39f, 173.32f, 245.75f);

        /// <summary>That patch, 7.1 m away: 92 polygons no route from the rest of the map reaches.</summary>
        private static readonly Vector3 Island = new Vector3(75.77f, 173.12f, 238.62f);

        private static NavMeshQuery Wilderness() => TestNavMeshes.Query(
            NavMeshFile.PathFor(Path.Combine(RepositoryRoot(), "navmesh"), "adv_foreas_concordia_wilderness"));

        private static IEnumerable<(Vector3 From, Vector3 To)> Pairs(NavMeshQuery query, int count, int seed, float? within = null)
        {
            var detour = new DtNavMeshQuery(query.NavMesh);
            var filter = new DtQueryDefaultFilter();
            var random = new RcRand(new Random(seed));

            for (var i = 0; i < count; i++)
            {
                detour.FindRandomPoint(filter, random, out var start, out var from);

                long end;
                RcVec3f to;

                if (within == null)
                    detour.FindRandomPoint(filter, random, out end, out to);
                else
                    detour.FindRandomPointAroundCircle(start, from, within.Value, filter, random, out end, out to);

                if (start != 0 && end != 0)
                    yield return (new Vector3(from.X, from.Y, from.Z), new Vector3(to.X, to.Y, to.Z));
            }
        }

        [TestMethod]
        public void ARouteJoinsTwoPointsExactlyWhenTheyAreInOneRegion()
        {
            var query = Wilderness();
            var reachable = 0;

            foreach (var (from, to) in Pairs(query, 400, 3))
            {
                query.FindPath(from, to, out var complete);
                Assert.AreEqual(complete, query.Reachable(from, to), $"{from} to {to}");
                reachable += complete ? 1 : 0;
            }

            Assert.IsTrue(reachable > 20, "some of each");
            Assert.IsFalse(query.Reachable(Mainland, Island).Value);
            Assert.IsNull(query.Reachable(Mainland, new Vector3(Mainland.X, Mainland.Y + 500, Mainland.Z)), "off the mesh");
        }

        [TestMethod]
        public void AGoalThatCanBeReachedGetsTheSameRouteBoundedOrNot()
        {
            var query = Wilderness();
            var compared = 0;

            foreach (var (from, to) in Pairs(query, 400, 5, within: 60))
            {
                var whole = query.FindPath(from, to, out var completeWhole);
                var bounded = query.FindPathBounded(from, to, out var completeBounded);

                Assert.AreEqual(completeWhole, completeBounded);
                CollectionAssert.AreEqual(whole, bounded, $"{from} to {to}");
                compared++;
            }

            Assert.IsTrue(compared > 300);
        }

        [TestMethod]
        public void AGoalNoRouteReachesIsSearchedOnlySoFar()
        {
            var query = Wilderness();

            var whole = query.FindPath(Mainland, Island, out var completeWhole);
            var wholeNodes = query.LastSearchNodes;

            var bounded = query.FindPathBounded(Mainland, Island, out var completeBounded);
            var boundedNodes = query.LastSearchNodes;

            Console.WriteLine($"whole search {wholeNodes} nodes, bounded {boundedNodes}");

            Assert.IsFalse(completeWhole);
            Assert.IsFalse(completeBounded);
            Assert.IsTrue(wholeNodes > 25_000, $"the whole search walks the region: {wholeNodes}");
            Assert.IsTrue(boundedNodes <= 2 * NavMeshQuery.UnreachableSearchNodes, $"{boundedNodes}");
            Assert.IsNotNull(bounded, "a way towards it still");
            Assert.IsTrue(bounded.Count > 0);

            // As near as it gets: no further from the goal than the whole search took it.
            Assert.IsTrue(Vector3.Distance(bounded[^1], Island) <= Vector3.Distance(whole[^1], Island) + 1f,
                $"{bounded[^1]} against {whole[^1]}");

            // A smaller allowance, a smaller search.
            query.FindPathBounded(Mainland, Island, out _, 256);
            Assert.IsTrue(query.LastSearchNodes < boundedNodes);
        }

        [TestMethod]
        public void ACreatureChasingWhatItCannotReachSearchesOnlyWhenItsPathLockIsUp()
        {
            using var world = new WorldTestContext();
            world.Map.NavMesh = Wilderness();
            var enemy = Watch(world, Island);
            var creature = Spawn(world, Mainland);

            Assert.IsTrue(BehaviorManager.Instance.TrySetActionFighting(creature, enemy.Player.EntityId));
            creature.Controller.ActionFighting.Opened = true;

            var before = world.Map.NavMesh.Searches;
            var nodes = new List<int>();

            for (var ms = 0L; ms < 3000; ms += Tick)
            {
                var searches = world.Map.NavMesh.Searches;
                BehaviorManager.Instance.MapChannelThink(world.Map, Tick);

                if (world.Map.NavMesh.Searches != searches)
                    nodes.Add(world.Map.NavMesh.LastSearchNodes);
            }

            var made = world.Map.NavMesh.Searches - before;
            Console.WriteLine($"{made} searches in 3 s: {string.Join(", ", nodes)}");

            Assert.AreEqual(BehaviorManager.BehaviorActionFighting, creature.Controller.CurrentAction, "still in the fight");
            Assert.IsTrue(creature.Controller.ActionFighting.RouteIncomplete);
            Assert.IsTrue(made >= 1 && made <= 3000 / 500 + 1, $"{made} searches in 3 s; the old chase made ten, each walking the whole region");
            Assert.IsTrue(nodes.All(count => count <= 2 * NavMeshQuery.UnreachableSearchNodes), string.Join(", ", nodes));
        }

        private static Creature Spawn(WorldTestContext world, Vector3 at)
        {
            var creature = new Creature
            {
                Name = "Chaser",
                TargetCategory = TargetCategory.Hostile,
                MapContextId = world.Map.MapInfo.MapContextId,
                RuntimeMapChannel = world.Map,
                Position = at,
                EntityClass = EntityClasses.HumanBaseMale,
                State = CharacterState.Idle,
                Level = 1,
                AggroRange = 0,
                RunSpeed = 6,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>()
            };

            creature.HomePos.Position = creature.Position;
            creature.HomePos.MapContextid = creature.MapContextId;
            creature.Attributes[Attributes.Health] = CreatureHealth.NewAttribute(1000);
            creature.Actions.Add(new CreatureAction { ActionId = ActionId.CrFilcherMelee, ActionArgId = 1, RangeMin = 0.5, RangeMax = 2.0, MinDamage = 1, MaxDamage = 1, Cooldown = 4000 });
            BehaviorManager.StartWandering(creature, false);
            EntityManager.Instance.RegisterEntity(creature.EntityId, EntityType.Creature);
            EntityManager.Instance.RegisterCreature(creature);
            EntityManager.Instance.RegisterActor(creature.EntityId, creature);
            var seed = CellManager.Instance.GetCellSeed(creature.Position);
            creature.Cells = CellsAt(world, creature.Position);
            CellManager.Instance.GetCell(world.Map, seed & 0xFFFF, seed >> 16).CreatureList.Add(creature);
            return creature;
        }

        private static Client Watch(WorldTestContext world, Vector3 at)
        {
            var client = world.CreateClient(x: at.X);
            client.Player.Position = at;
            var seed = CellManager.Instance.GetCellSeed(client.Player.Position);
            client.Player.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 1000, 1000, 1000, 0, 0);
            client.Player.Cells = CellsAt(world, client.Player.Position);
            CellManager.Instance.GetCell(world.Map, seed & 0xFFFF, seed >> 16).ClientList.Add(client);
            return client;
        }

        private static uint[,] CellsAt(WorldTestContext world, Vector3 position)
        {
            var seed = CellManager.Instance.GetCellSeed(position);
            return CellManager.Instance.CreateCellMatrix(world.Map, seed & 0xFFFF, seed >> 16);
        }

        private static string RepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "navmesh")))
                directory = directory.Parent;

            return directory?.FullName ?? throw new DirectoryNotFoundException("navmesh");
        }
    }
}
