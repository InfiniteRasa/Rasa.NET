extern alias RasaGame;

using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Managers;
using Rasa.Packets.Mission.Server;
using Rasa.Services.Preloader.Missions.Plateau;
using Rasa.Structures;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions.Plateau
{
    using static PlateauCampResistanceTests;

    /// <summary>
    /// Amee Corman's missions at the Retread Camp, Valverde Pools, played through as a player plays them: Retreads to
    /// Retreads (1751: a talk with Retread Vincent in Retread City, Valverde Marshes).
    ///
    /// Why these tests: 1751 is taken in Valverde Pools and done on another map, at an NPC no mission used before. A
    /// mission offered before Weapons Trafficking, a talk that Amee also took, a turn-in that a failed save or a full bag
    /// still paid, or progress lost at a reload would each break it.
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class PlateauRetreadCampTests
    {
        private const uint Marshes = 1454;     // adv_foreas_valverde_marshes
        private const uint RetreadsToRetreads = PlateauRetreadsToRetreadsV1.MissionId;
        private const uint Speak = PlateauRetreadsToRetreadsV1.Speak;
        private const uint ConsumableFiller = 44921;   // as WildernessRanjaGorgeTests' consumable bag

        [TestMethod]
        public void RetreadsToRetreadsSendsThePlayerToRetreadVincentInTheMarshes()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pools = PlateauValverdePoolsTests.OntoPools(harness);
            var amee = PlateauValverdePoolsTests.Amee(pools);

            Assert.IsFalse(Offers(harness, amee, RetreadsToRetreads), "it waits for Weapons Trafficking");
            SeedCompleted(harness, PlateauRetreadsToRetreadsV1.WeaponsTrafficking);
            var offer = WildernessRanjaGorgeTests.Offer(harness, amee, RetreadsToRetreads);
            Assert.AreEqual(0, offer.SelectableReward.Count);
            CollectionAssert.AreEquivalent(Rewards, offer.FixedReward.FixedItems.Select(item => (item.ItemTemplateId, item.Quantity)).ToArray());

            harness.MoveTo(amee.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, amee.EntityId, RetreadsToRetreads));
            Assert.AreEqual(1, harness.Drain().OfType<MissionGainedPacket>().Count(packet => packet.MissionId == RetreadsToRetreads));
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness));
            Assert.IsFalse(harness.Manager.CompleteOfferedObjective(harness.Client, amee.EntityId, RetreadsToRetreads, Speak, 1),
                "not Amee's to hear");

            WildernessRanjaGorgeTests.Reload(harness);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness), "the mission survives a reload");

            var vincent = Vincent(harness);
            AssertHandInTopic(Conversation(harness, vincent), RetreadsToRetreads, Speak);
            harness.MoveTo(vincent.Position);
            harness.Drain();
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, vincent.EntityId, RetreadsToRetreads, Speak, 1));
            var talk = harness.Drain();
            Assert.AreEqual(1, talk.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == RetreadsToRetreads && packet.ObjectiveId == Speak));
            Assert.IsTrue(talk.OfType<MissionCompleteablePacket>().Single(packet => packet.MissionId == RetreadsToRetreads).IsCompleteable);

            // A turn-in whose save fails pays nothing.
            var before = harness.Context.ReadRewardTotals();
            harness.Context.BeforeSave = _ => throw new DbUpdateException("Injected Retreads to Retreads turn-in failure.");
            try
            {
                Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, vincent.EntityId, RetreadsToRetreads, null));
            }
            finally
            {
                harness.Context.BeforeSave = null;
            }
            Assert.AreEqual(before, harness.Context.ReadRewardTotals());
            harness.Drain();

            var held = Rewards.Select(reward => WildernessRanjaGorgeTests.Held(harness, reward.Template)).ToArray();
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, vincent, RetreadsToRetreads, null,
                PlateauRetreadsToRetreadsV1.Experience, PlateauRetreadsToRetreadsV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == RetreadsToRetreads));
            for (var i = 0; i < Rewards.Length; i++)
                Assert.AreEqual(held[i] + Rewards[i].Quantity, WildernessRanjaGorgeTests.Held(harness, Rewards[i].Template), $"{Rewards[i].Template}");
            PlateauValverdePoolsTests.OntoPools(harness);
            Assert.IsFalse(Offers(harness, amee, RetreadsToRetreads), "done once");
        }

        /// <summary>A full bag keeps the turn-in back until the four rewards fit; an abandoned mission is offered again.</summary>
        [TestMethod]
        public void RetreadsToRetreadsWaitsForRoomAndCanBeAbandoned()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pools = PlateauValverdePoolsTests.OntoPools(harness);
            var amee = PlateauValverdePoolsTests.Amee(pools);
            SeedCompleted(harness, PlateauRetreadsToRetreadsV1.WeaponsTrafficking);
            harness.MoveTo(amee.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, amee.EntityId, RetreadsToRetreads));
            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, RetreadsToRetreads));
            harness.Drain();
            Assert.IsTrue(Offers(harness, amee, RetreadsToRetreads), "offered again after an abandon");
            harness.MoveTo(amee.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, amee.EntityId, RetreadsToRetreads));
            harness.Drain();

            var vincent = Vincent(harness);
            harness.MoveTo(vincent.Position);
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, vincent.EntityId, RetreadsToRetreads, Speak, 1));
            harness.Drain();
            WildernessAliaBranchesTests.FillCategory(harness, ConsumableFiller, 50);
            harness.FillInbox();
            var before = harness.Context.ReadRewardTotals();
            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, vincent.EntityId, RetreadsToRetreads, null));
            Assert.AreEqual(before, harness.Context.ReadRewardTotals());
            Assert.IsTrue(harness.Client.Player.Missions[RetreadsToRetreads].Completeable, "still to be turned in");

            for (var i = 0; i < Rewards.Length; i++)
                WildernessAliaBranchesTests.FreeOneSlot(harness, ConsumableFiller);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, vincent, RetreadsToRetreads, null,
                PlateauRetreadsToRetreadsV1.Experience, PlateauRetreadsToRetreadsV1.Credits);
        }

        private static readonly (uint Template, uint Quantity)[] Rewards =
        {
            (PlateauRetreadsToRetreadsV1.EmpBomb, PlateauRetreadsToRetreadsV1.EmpBombs),
            (PlateauRetreadsToRetreadsV1.PowerCharger, PlateauRetreadsToRetreadsV1.PowerChargers),
            (PlateauRetreadsToRetreadsV1.ConcussionGrenade, PlateauRetreadsToRetreadsV1.ConcussionGrenades),
            (PlateauRetreadsToRetreadsV1.FragGrenade, PlateauRetreadsToRetreadsV1.FragGrenades)
        };

        /// <summary>Retread Vincent in Retread City, Valverde Marshes, the player beside him: his package, on the navmesh.</summary>
        private static Creature Vincent(WildernessRuntimeTestHarness harness)
        {
            var marshes = Onto(harness, Marshes);
            Spawn(marshes, PlateauRetreadsToRetreadsV1.Vincent);
            var vincent = RequireNpc(marshes, PlateauRetreadsToRetreadsV1.Vincent);
            Assert.AreEqual(PlateauRetreadsToRetreadsV1.VincentPackage, vincent.Npc.NpcPackageId);
            Assert.AreNotEqual(TargetCategory.Hostile, vincent.TargetCategory);
            var ground = marshes.NavMesh.GroundHeight(vincent.Position);
            Assert.IsNotNull(ground, $"navmesh under {vincent.Position}");
            Assert.AreEqual(ground.Value, vincent.Position.Y, 1f, "Vincent on the ground");
            return vincent;
        }

        /// <summary>The map as the server loads it, the player on it (as PlateauValverdePoolsTests.OntoPools).</summary>
        internal static MapChannel Onto(WildernessRuntimeTestHarness harness, uint mapContextId)
        {
            var map = harness.Maps.MapChannelArray.TryGetValue(mapContextId, out var loaded)
                ? loaded
                : PlateauInstanceMaps.LoadMap(harness, mapContextId);
            var client = harness.Client;
            CellManager.Instance.RemoveFromWorld(client);
            client.Player.MapChannel.ClientList.Remove(client);
            client.Player.MapChannel = map;
            client.Player.RuntimeMapChannel = map;
            client.Player.MapContextId = mapContextId;
            map.ClientList.Add(client);
            CellManager.Instance.AddToWorld(client);
            harness.Drain();
            return map;
        }

        internal static bool Offers(WildernessRuntimeTestHarness harness, Creature npc, uint mission) =>
            Conversation(harness, npc).TryGetValue(ConversationType.MissionDispense, out var offered) &&
            ((Dictionary<uint, MissionInfo>)offered).ContainsKey(mission);

        private static MissionObjectiveState Objective(WildernessRuntimeTestHarness harness) =>
            harness.Client.Player.Missions[RetreadsToRetreads].Objectives[Speak].State;
    }
}
