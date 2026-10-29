extern alias RasaGame;

using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Managers;
using Rasa.Services.Preloader.Missions.Plateau;
using Rasa.Structures;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions.Plateau
{
    using static PlateauCampResistanceTests;
    using static PlateauInstanceMaps;

    /// <summary>
    /// Colonel Bosley's two follow-ups to One Way Trip, played through as a player plays them: South Of The Border (1068:
    /// the crate of medical supplies to Colonel "Snake" Washington at the Snake Pit) and New Orders From The General
    /// (1541: Amee Corman at the Retread Camp), both in Valverde Pools.
    ///
    /// Why these tests: both are taken on the Plateau and done on another map, at an NPC the server did not have (Snake)
    /// or one no mission used (Amee). An NPC off the navmesh or out of reach, a crate that was not given or not taken, a
    /// talk that the wrong NPC also took, or a crate kept after an abandon would each break them.
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class PlateauValverdePoolsTests
    {
        private const uint Pools = PlateauSnakePitV1.MapContextId;
        private const uint SouthOfTheBorder = PlateauSouthOfTheBorderV1.MissionId;
        private const uint NewOrders = PlateauNewOrdersFromTheGeneralV1.MissionId;

        /// <summary>The Snake Pit's waypoint (World DB teleporter 48), where a player arrives.</summary>
        private static readonly Vector3 SnakePitWaypoint = new(-941.7f, 919.94f, 367.4f);

        [TestMethod]
        public void SouthOfTheBorderTakesBosleysCrateToSnakeAtTheSnakePit()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var plateau = EnterPlateau(harness);
            var bosley = Bosley(harness, plateau);

            Assert.IsFalse(Offers(harness, bosley, SouthOfTheBorder), "it waits for One Way Trip");
            SeedCompleted(harness, PlateauOneWayTripV1.MissionId);
            var offer = WildernessRanjaGorgeTests.Offer(harness, bosley, SouthOfTheBorder);
            Assert.AreEqual(0, offer.SelectableReward.Count);
            CollectionAssert.AreEquivalent(new[]
                {
                    (PlateauSouthOfTheBorderV1.MedPack, PlateauSouthOfTheBorderV1.MedPacks),
                    (PlateauSouthOfTheBorderV1.PowerCharger, PlateauSouthOfTheBorderV1.PowerChargers),
                    (PlateauSouthOfTheBorderV1.ConcussionGrenade, PlateauSouthOfTheBorderV1.ConcussionGrenades),
                    (PlateauSouthOfTheBorderV1.EmpGrenade, PlateauSouthOfTheBorderV1.EmpGrenades)
                },
                offer.FixedReward.FixedItems.Select(item => (item.ItemTemplateId, item.Quantity)).ToArray());
            Accept(harness, bosley, SouthOfTheBorder);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PlateauSouthOfTheBorderV1.Supplies), "Bosley's crate");
            Assert.IsFalse(harness.Manager.CompleteOfferedObjective(harness.Client, bosley.EntityId, SouthOfTheBorder,
                PlateauSouthOfTheBorderV1.Deliver, 1), "not Bosley's to take");

            var pools = OntoPools(harness);
            var snake = Snake(harness, pools);
            var amee = Amee(pools);
            Assert.IsFalse(harness.Manager.CompleteOfferedObjective(harness.Client, amee.EntityId, SouthOfTheBorder,
                PlateauSouthOfTheBorderV1.Deliver, 1), "not Amee's to take");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PlateauSouthOfTheBorderV1.Supplies));

            AssertOnlyMarker(harness, SouthOfTheBorder, PlateauSouthOfTheBorderV1.Deliver, snake.Position, "Snake");
            Talk(harness, snake, SouthOfTheBorder, PlateauSouthOfTheBorderV1.Deliver);
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, PlateauSouthOfTheBorderV1.Supplies), "Snake has the crate");
            Assert.IsTrue(harness.Client.Player.Missions[SouthOfTheBorder].Completeable);

            var held = new[]
            {
                PlateauSouthOfTheBorderV1.MedPack, PlateauSouthOfTheBorderV1.PowerCharger,
                PlateauSouthOfTheBorderV1.ConcussionGrenade, PlateauSouthOfTheBorderV1.EmpGrenade
            }.Select(template => WildernessRanjaGorgeTests.Held(harness, template)).ToArray();
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, snake, SouthOfTheBorder, null,
                PlateauSouthOfTheBorderV1.Experience, PlateauSouthOfTheBorderV1.Credits);
            Assert.AreEqual(held[0] + PlateauSouthOfTheBorderV1.MedPacks, WildernessRanjaGorgeTests.Held(harness, PlateauSouthOfTheBorderV1.MedPack));
            Assert.AreEqual(held[1] + PlateauSouthOfTheBorderV1.PowerChargers, WildernessRanjaGorgeTests.Held(harness, PlateauSouthOfTheBorderV1.PowerCharger));
            Assert.AreEqual(held[2] + PlateauSouthOfTheBorderV1.ConcussionGrenades, WildernessRanjaGorgeTests.Held(harness, PlateauSouthOfTheBorderV1.ConcussionGrenade));
            Assert.AreEqual(held[3] + PlateauSouthOfTheBorderV1.EmpGrenades, WildernessRanjaGorgeTests.Held(harness, PlateauSouthOfTheBorderV1.EmpGrenade));
        }

        /// <summary>An abandon takes Bosley's crate back; taken again, the mission gives a new one.</summary>
        [TestMethod]
        public void SouthOfTheBorderAbandonedTakesTheCrateBack()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var plateau = EnterPlateau(harness);
            var bosley = Bosley(harness, plateau);
            SeedCompleted(harness, PlateauOneWayTripV1.MissionId);
            Accept(harness, bosley, SouthOfTheBorder);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PlateauSouthOfTheBorderV1.Supplies));

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, SouthOfTheBorder));
            harness.Drain();
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, PlateauSouthOfTheBorderV1.Supplies), "the crate goes with the mission");

            Accept(harness, bosley, SouthOfTheBorder);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PlateauSouthOfTheBorderV1.Supplies), "a new crate");
        }

        [TestMethod]
        public void NewOrdersFromTheGeneralIsAQuestionForAmeeCorman()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var plateau = EnterPlateau(harness);
            var bosley = Bosley(harness, plateau);

            Assert.IsFalse(Offers(harness, bosley, NewOrders), "it waits for One Way Trip");
            SeedCompleted(harness, PlateauOneWayTripV1.MissionId);
            var offer = WildernessRanjaGorgeTests.Offer(harness, bosley, NewOrders);
            CollectionAssert.AreEqual(new[]
                {
                    PlateauNewOrdersFromTheGeneralV1.BioGloves, PlateauNewOrdersFromTheGeneralV1.MechGloves,
                    PlateauNewOrdersFromTheGeneralV1.ReflectiveGloves, PlateauNewOrdersFromTheGeneralV1.StealthLegs
                },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Accept(harness, bosley, NewOrders);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, NewOrders, PlateauNewOrdersFromTheGeneralV1.Question));

            var pools = OntoPools(harness);
            var snake = Snake(harness, pools);
            Assert.IsFalse(harness.Manager.CompleteOfferedObjective(harness.Client, snake.EntityId, NewOrders,
                PlateauNewOrdersFromTheGeneralV1.Question, 1), "not Snake's to hear");
            var amee = Amee(pools);
            Assert.AreEqual(PlateauNewOrdersFromTheGeneralV1.AmeePackage, amee.Npc.NpcPackageId);
            AssertOnlyMarker(harness, NewOrders, PlateauNewOrdersFromTheGeneralV1.Question, amee.Position, "Amee Corman");
            Talk(harness, amee, NewOrders, PlateauNewOrdersFromTheGeneralV1.Question);
            Assert.IsTrue(harness.Client.Player.Missions[NewOrders].Completeable);

            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, amee, NewOrders, 3,
                PlateauNewOrdersFromTheGeneralV1.Experience, PlateauNewOrdersFromTheGeneralV1.Credits);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PlateauNewOrdersFromTheGeneralV1.StealthLegs), "the fourth choice");
        }

        private static Creature Bosley(WildernessRuntimeTestHarness harness, MapChannel plateau)
        {
            Spawn(plateau, PlateauGiantKillerV1.Bosley);
            return RequireNpc(plateau, PlateauGiantKillerV1.Bosley);
        }

        /// <summary>Snake at the Snake Pit: his name and package, on the navmesh, walked to from the waypoint.</summary>
        private static Creature Snake(WildernessRuntimeTestHarness harness, MapChannel pools)
        {
            Spawn(pools, PlateauSnakePitV1.Snake);
            var snake = Living(pools).Single(creature => creature.SpawnPool?.DbId == PlateauSnakePitV1.Snake && creature.Npc != null);
            Assert.AreEqual(PlateauSnakePitV1.SnakeName, snake.NameId);
            Assert.AreEqual(PlateauSnakePitV1.SnakePackage, snake.Npc.NpcPackageId);
            Assert.AreNotEqual(TargetCategory.Hostile, snake.TargetCategory);
            AssertStandsOnNavMesh(pools, snake.Position);
            Assert.IsTrue(pools.NavMesh.FindPath(SnakePitWaypoint, snake.Position, out var complete) != null && complete, "Snake is walked to");
            Assert.IsTrue(Vector3.Distance(SnakePitWaypoint, snake.Position) < 40f, "Snake in the Snake Pit");
            return snake;
        }

        internal static Creature Amee(MapChannel pools)
        {
            Spawn(pools, PlateauNewOrdersFromTheGeneralV1.Amee);
            return RequireNpc(pools, PlateauNewOrdersFromTheGeneralV1.Amee);
        }

        /// <summary>Valverde Pools as the server loads it, the player on it (as Return puts them on the Plateau).</summary>
        internal static MapChannel OntoPools(WildernessRuntimeTestHarness harness)
        {
            var pools = harness.Maps.MapChannelArray.TryGetValue(Pools, out var loaded) ? loaded : LoadMap(harness, Pools);
            var client = harness.Client;
            CellManager.Instance.RemoveFromWorld(client);
            client.Player.MapChannel.ClientList.Remove(client);
            client.Player.MapChannel = pools;
            client.Player.RuntimeMapChannel = pools;
            client.Player.MapContextId = Pools;
            pools.ClientList.Add(client);
            harness.MoveTo(SnakePitWaypoint);
            CellManager.Instance.AddToWorld(client);
            harness.Drain();
            return pools;
        }

        /// <summary>The NPC hears the objective (only that one), and it is done.</summary>
        private static void Talk(WildernessRuntimeTestHarness harness, Creature npc, uint mission, uint objective)
        {
            AssertHandInTopic(Conversation(harness, npc), mission, objective);
            harness.MoveTo(npc.Position);
            harness.Drain();
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, npc.EntityId, mission, objective, 1), $"{mission}.{objective}");
            harness.Drain();
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, mission, objective));
        }

        private static bool Offers(WildernessRuntimeTestHarness harness, Creature npc, uint mission) =>
            Conversation(harness, npc).TryGetValue(ConversationType.MissionDispense, out var offered) &&
            ((System.Collections.Generic.Dictionary<uint, MissionInfo>)offered).ContainsKey(mission);

        private static void Accept(WildernessRuntimeTestHarness harness, Creature npc, uint mission)
        {
            harness.MoveTo(npc.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, npc.EntityId, mission));
            harness.Drain();
        }

        private static MissionObjectiveState Objective(WildernessRuntimeTestHarness harness, uint mission, uint objective) =>
            harness.Client.Player.Missions[mission].Objectives[objective].State;

        private static void AssertStandsOnNavMesh(MapChannel map, Vector3 position, float tolerance = 1f)
        {
            var ground = map.NavMesh.GroundHeight(position);
            Assert.IsNotNull(ground, $"navmesh under {position}");
            Assert.AreEqual(ground.Value, position.Y, tolerance, $"{position} on the ground");
        }

        /// <summary>The mission's one marker, on <paramref name="objective"/>, at <paramref name="at"/> (flat, 2 m), on Valverde Pools.</summary>
        private static void AssertOnlyMarker(WildernessRuntimeTestHarness harness, uint mission, uint objective, Vector3 at, string what)
        {
            var markers = harness.World.MissionIndicatorEntries.Where(row => row.MissionId == mission).ToArray();
            Assert.HasCount(1, markers, $"{mission}'s markers");
            Assert.AreEqual(objective, markers[0].ObjectiveId);
            Assert.IsLessThan(2f, Vector2.Distance(new((float)markers[0].PosX, (float)markers[0].PosZ), new(at.X, at.Z)), $"the marker at {what}");
        }
    }
}
