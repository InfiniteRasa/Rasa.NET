using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Structures;
    using Rasa.Test.Missions;
    using Rasa.Test.Missions.Wilderness;

    /// <summary>
    /// A squad, a camp and a mission's escorts keep a few metres from each other as they spawn,
    /// chase and follow (Spacing).
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class CreatureSpacingTests
    {
        /// <summary>Thrax Soldiers by Alia Das: three, 12 m round - here made a group at one point, as a scene's or a dropship's are.</summary>
        private const uint Thrax = 580019;

        /// <summary>Alia Das: eight Forean Gunners and Archers, friendly, at one point.</summary>
        private const uint Foreans = 66;

        /// <summary>The Young Forest Boargar pack west of Alia Das: two to four, 15 m round.</summary>
        private const uint Pack = 580064;

        [TestMethod]
        public void AHostileGroupAtOnePointComesOutASpacingApart()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pool = harness.Map.SpawnPools.Single(p => p.DbId == Thrax);
            pool.Radius = 0;
            foreach (var slot in pool.SpawnSlot)
                slot.CountMin = slot.CountMax;
            harness.SpawnWorld(Thrax);
            var group = Of(harness, Thrax);

            Assert.IsTrue(group.Count >= 3, $"{group.Count}");
            Assert.IsTrue(group.All(creature => creature.TargetCategory == TargetCategory.Hostile));
            AssertApart(group, Spacing.Distance - 0.01f);
            Assert.IsTrue(group.All(creature => Spacing.AcrossGround(creature.Position, pool.Position) < 4f),
                "on the point or the ring round it: " + string.Join(", ", group.Select(c => c.Position)));
            Assert.IsTrue(group.All(creature => harness.Map.NavMesh.Nearest(creature.Position) is Vector3 ground &&
                Vector3.Distance(ground, creature.Position) < 0.5f), "each on the ground");
        }

        [TestMethod]
        public void AFriendlyGroupStandsWhereItIsPutAsBefore()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            harness.SpawnWorld(Foreans);
            var group = Of(harness, Foreans);
            var pool = harness.Map.SpawnPools.Single(p => p.DbId == Foreans);

            Assert.HasCount(8, group);
            Assert.IsTrue(group.All(creature => creature.TargetCategory == TargetCategory.Friendly));
            Assert.IsTrue(group.All(creature => Spacing.AcrossGround(creature.Position, pool.Position) <= 3f),
                "within the two metres each way of its point it always had");
        }

        [TestMethod]
        public void ACampWithARadiusComesOutClearOfItself()
        {
            using var harness = WildernessRuntimeTestHarness.Create();

            for (var round = 0; round < 5; round++)
            {
                foreach (var creature in Of(harness, Pack))
                    CreatureManager.Instance.HandleCreatureKill(harness.Map, creature, null);
                harness.SpawnWorldAfter(1_000_000, Pack);

                var pack = Of(harness, Pack);
                Assert.IsTrue(pack.Count >= 2, $"round {round}: {pack.Count}");
                AssertApart(pack, Spacing.Distance - 0.01f);
            }
        }

        [TestMethod]
        public void ChasersComingTogetherTakeTheirOwnPlacesRoundTheirTarget()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            harness.SpawnWorld(Pack);
            var pack = Of(harness, Pack);
            var pool = harness.Map.SpawnPools.Single(p => p.DbId == Pack);

            // Three of them: round a target within their 3 m bites there is room for about four
            // a spacing apart, and the pack is two to four.
            Assert.IsTrue(pack.Count >= 2);
            foreach (var extra in pack.Skip(3))
                CreatureManager.Instance.HandleCreatureKill(harness.Map, extra, null);
            pack = pack.Take(3).ToList();

            // All of them on one spot, 14 m from the player, all after them.
            var start = harness.Map.NavMesh.Nearest(pool.Position).Value;
            var player = harness.Map.NavMesh.Nearest(start + new Vector3(14, 0, 0)).Value;
            harness.MoveTo(player);
            harness.Client.Player.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 1_000_000, 1_000_000, 1_000_000, 0, 0);
            foreach (var creature in pack)
            {
                // Their charge carries them right up to their target, wherever they stood
                // (KaelRushingBlow): their bites alone, so where they stand is the spacing's.
                creature.Actions.RemoveAll(KaelRushingBlow.Is);
                Place(harness, creature, start);
                Assert.IsTrue(BehaviorManager.Instance.TrySetActionFighting(creature, harness.Client.Player.EntityId));
            }

            for (var tick = 0; tick < 120; tick++)
            {
                harness.MoveTo(player);
                harness.Client.Player.Attributes[Attributes.Health].Current = 1_000_000;
                BehaviorManager.Instance.MapChannelThink(harness.Map, 250);
            }

            foreach (var creature in pack)
                Assert.IsTrue(Spacing.AcrossGround(creature.Position, player) < 6f,
                    $"{creature.EntityId} came to the player: {Spacing.AcrossGround(creature.Position, player):F1} m");
            AssertApart(pack, Spacing.Crowded);
        }

        [TestMethod]
        public void EscortsComingTogetherTakeTheirOwnPlacesRoundTheirOwner()
        {
            using var harness = CreateEscorts();
            var escorts = Escorts(harness);
            Assert.IsTrue(escorts.Length >= 3, $"{escorts.Length} escorts");
            foreach (var escort in escorts)
                escort.AggroRange = 0;

            // All on one spot, 12 m from their owner.
            var start = Ground(harness, new Vector3(380, 120, 158));
            foreach (var escort in escorts)
                Place(harness, escort, start);
            var owner = Ground(harness, start + new Vector3(12, 0, 0));
            harness.MovePlayerTo(owner);
            CellManager.Instance.UpdateVisibility(harness.Client);

            for (var tick = 0; tick < 60; tick++)
                BehaviorManager.Instance.MapChannelThink(harness.BootcampMap, 250);

            foreach (var escort in escorts)
                Assert.IsTrue(Spacing.AcrossGround(escort.Position, owner) <= 4.5f,
                    $"{escort.EntityId} by its owner: {Spacing.AcrossGround(escort.Position, owner):F1} m");
            AssertApart(escorts, Spacing.Crowded);

            // Their places go with their owner, and stay apart.
            var places = escorts.Select(escort => BehaviorManager.EscortPlace(harness.BootcampMap, escort, owner)).ToList();
            AssertApart(places, Spacing.Distance - 0.01f);
            var moved = Ground(harness, owner + new Vector3(0, 0, -8));
            var after = escorts.Select(escort => BehaviorManager.EscortPlace(harness.BootcampMap, escort, moved)).ToList();
            for (var i = 0; i < places.Count; i++)
                Assert.AreEqual(Spacing.AcrossGround(places[i], owner), Spacing.AcrossGround(after[i], moved), 0.6f, "the same place round the owner");
        }

        [TestMethod]
        public void AroundClearTurnsRoundTheCentreUntilClearAndOnRingsFillsTheRings()
        {
            var centre = new Vector3(0, 0, 0);
            var taken = new List<Vector3> { new(3, 0, 0) };

            // Turned off the taken one, still 3 m from the centre.
            var spot = Spacing.AroundClear(null, centre, new Vector3(3, 0, 0), taken).Value;
            Assert.AreEqual(3f, Spacing.AcrossGround(spot, centre), 0.01f);
            Assert.IsTrue(Spacing.AcrossGround(spot, taken[0]) >= Spacing.Distance);

            // Nineteen on a point and two rings: none closer than a spacing.
            var placed = new List<Vector3>();
            for (var i = 0; i < 19; i++)
                placed.Add(Spacing.OnRings(null, centre, placed).Value);
            AssertApart(placed, Spacing.Distance);
            Assert.IsNull(Spacing.OnRings(null, centre, placed), "the rings are full");
        }

        private static void AssertApart(IReadOnlyList<Creature> creatures, float least) =>
            AssertApart(creatures.Select(creature => creature.Position).ToList(), least);

        private static void AssertApart(IReadOnlyList<Vector3> spots, float least)
        {
            for (var i = 0; i < spots.Count; i++)
                for (var j = i + 1; j < spots.Count; j++)
                    Assert.IsTrue(Spacing.AcrossGround(spots[i], spots[j]) >= least,
                        $"{spots[i]} and {spots[j]} are {Spacing.AcrossGround(spots[i], spots[j]):F2} m apart, under {least} m");
        }

        private static List<Creature> Of(WildernessRuntimeTestHarness harness, uint poolId) =>
            harness.Map.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList).Distinct()
                .Where(creature => creature.SpawnPool?.DbId == poolId && CreatureManager.IsLivingOnMap(harness.Map, creature))
                .ToList();

        private static void Place(WildernessRuntimeTestHarness harness, Creature creature, Vector3 position)
        {
            CreatureManager.Instance.SetLocation(creature, position, 0, harness.Map.MapInfo.MapContextId);
            var seed = CellManager.Instance.GetCellSeed(creature.Position);
            CreatureManager.Instance.CellUpdateLocation(harness.Map, creature, seed & 0xffff, seed >> 16);
        }

        private static BootcampRuntimeTestHarness.Harness CreateEscorts()
        {
            var harness = BootcampRuntimeTestHarness.Create(useWorldContent: true, worldMigration: BootcampCaveInBreachTests.BridgeThraxStood);
            SpawnPoolManager.Instance.SpawnPoolWorker(harness.BootcampMap, 0);
            harness.SeedMission(harness.Client.Player.Id, 1992, (uint)MissionState.Completed, true);
            var deSimone = Actors(harness).Single(actor => actor.DbId == 510206);
            harness.MovePlayerTo(deSimone);
            CellManager.Instance.UpdateVisibility(harness.Client);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, deSimone.EntityId, 1994));
            return harness;
        }

        private static Creature[] Actors(BootcampRuntimeTestHarness.Harness harness) =>
            harness.BootcampMap.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList).Distinct().ToArray();

        private static Creature[] Escorts(BootcampRuntimeTestHarness.Harness harness) =>
            Actors(harness).Where(actor => actor.SpawnPool?.FollowOwnerCharacterId == harness.Client.Player.Id).ToArray();

        private static Vector3 Ground(BootcampRuntimeTestHarness.Harness harness, Vector3 position) =>
            harness.BootcampMap.NavMesh.Nearest(position) ?? throw new AssertFailedException($"No navmesh near {position}.");

        private static void Place(BootcampRuntimeTestHarness.Harness harness, Creature creature, Vector3 position)
        {
            CreatureManager.Instance.SetLocation(creature, Ground(harness, position), 0, 1985);
            var seed = CellManager.Instance.GetCellSeed(creature.Position);
            CreatureManager.Instance.CellUpdateLocation(harness.BootcampMap, creature, seed & 0xffff, seed >> 16);
        }
    }
}
