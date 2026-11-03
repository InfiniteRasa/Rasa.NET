using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Packets.Mission.Server;
using Rasa.Services.Preloader.Missions.Marshes;
using Rasa.Services.Preloader.Missions.Plateau;
using Rasa.Structures;
using Rasa.Structures.Char;
using Rasa.Test.Missions.Plateau;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions.Marshes
{
    using static MarshesPaludosTests;
    using static PlateauCampResistanceTests;

    /// <summary>
    /// Marshes set 2 at Falcon Hold, played through as a player plays it: Retread Vincent's It Lies in Ruins (1673: report to
    /// Lieutenant Morrison), Research Chemist Horsh's Tastes Like Grubber (959: ten steaks from the Swamp Grubbers) and
    /// Fixing the Food Chain (961: ten bags of feed in ten of the twelve Mox feeders).
    ///
    /// Why these tests: Horsh and two Grubber pools are new on the server, 1673 is a Marshes mission on the Plateau
    /// revision after Retreads to Retreads, the steaks drop only from Grubbers at their chance, and the feeders are the
    /// player's own scene objects that each take one bag. An NPC off the navmesh, an offer before its prerequisite, a talk
    /// at the wrong NPC, a drop on the wrong side of its chance, a kill counted as a pickup, a feeder fed twice or after the
    /// tenth, bags kept after an abandon, progress lost at a reload, or a turn-in that pays twice would each break them.
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class MarshesFalconHoldTests
    {
        private const uint Ruins = MarshesItLiesInRuinsV1.MissionId;
        private const uint Grubber = MarshesTastesLikeGrubberV1.MissionId;
        private const uint FoodChain = MarshesFixingTheFoodChainV1.MissionId;
        private const uint Vincent = MarshesItLiesInRuinsV1.Vincent, Morrison = MarshesItLiesInRuinsV1.Morrison;
        private const uint Horsh = MarshesFalconHoldNpcsV1.Horsh;
        private const uint Mox = MarshesPaludosNpcsV1.Mox;
        private const uint MissionItemFiller = 11519;      // as PlateauCampResistanceTests

        /// <summary>The Falcon Hold Outpost waypoint (World DB teleporter 133).</summary>
        private static readonly Vector3 FalconHoldWaypoint = new(-143.27f, 219.05f, 581.54f);

        [TestInitialize]
        public void Clock() => PlateauGiantKillerSquadTests.UseClock();

        [TestCleanup]
        public void Restore() => PlateauGiantKillerSquadTests.RestoreClock();

        [TestMethod]
        public void HorshAndTheSteakPoolsStandOnTheNavmeshAndAreWalkedTo()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            var horsh = Npc(marshes, Horsh);
            Assert.AreEqual(MarshesFalconHoldNpcsV1.HorshName, horsh.NameId);
            Assert.AreEqual(MarshesFalconHoldNpcsV1.HorshPackage, horsh.Npc.NpcPackageId);
            Assert.AreNotEqual(TargetCategory.Hostile, horsh.TargetCategory);
            var at = MarshesFalconHoldNpcsV1.HorshAt;
            Assert.IsLessThan(0.1f, Vector2.Distance(new((float)at.X, (float)at.Z), new(horsh.Position.X, horsh.Position.Z)), "Horsh at TaRapedia's place");
            AssertOnGround(marshes, horsh.Position, "Horsh");
            Assert.IsTrue(marshes.NavMesh.FindPath(FalconHoldWaypoint, horsh.Position, out var reached) != null && reached, "Horsh is walked to");

            foreach (var (id, x, y, z) in MarshesFalconHoldNpcsV1.SteakPools)
            {
                var pool = new Vector3((float)x, (float)y, (float)z);
                AssertOnGround(marshes, pool, $"pool {id}");
                Assert.IsTrue(marshes.NavMesh.FindPath(FalconHoldWaypoint, pool, out var complete) != null && complete, $"pool {id} is walked to");
            }
            Spawn(marshes, MarshesFalconHoldNpcsV1.SteakPools.Select(pool => pool.Id).ToArray());
            Assert.AreEqual(6, Living(marshes).Count(creature => creature.DbId == MarshesFalconHoldNpcsV1.SwampGrubber), "two pools of three");
        }

        [TestMethod]
        public void ItLiesInRuinsSendsThePlayerFromVincentToMorrison()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            var vincent = Npc(marshes, Vincent);
            var morrison = Npc(marshes, Morrison);

            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, vincent, Ruins), "it waits for Retreads to Retreads");
            SeedCompleted(harness, PlateauRetreadsToRetreadsV1.MissionId);
            marshes = EnterMarshes(harness);
            var offer = WildernessRanjaGorgeTests.Offer(harness, vincent, Ruins);
            CollectionAssert.AreEqual(new[]
                {
                    MarshesRewardBlocksV1.Current(MarshesItLiesInRuinsV1.MissionId, MarshesItLiesInRuinsV1.HazmatVest), MarshesRewardBlocksV1.Current(MarshesItLiesInRuinsV1.MissionId, MarshesItLiesInRuinsV1.MechVest),
                    MarshesRewardBlocksV1.Current(MarshesItLiesInRuinsV1.MissionId, MarshesItLiesInRuinsV1.MotorAssistVest), MarshesRewardBlocksV1.Current(MarshesItLiesInRuinsV1.MissionId, MarshesItLiesInRuinsV1.ReflectiveVest)
                },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, morrison, Ruins), "not Morrison's to give");
            Accept(harness, vincent, Ruins);

            // Vincent hears nothing of it; Morrison hears the report, and only that.
            Assert.IsEmpty(Topics(harness, vincent, Ruins), "Vincent has no topic of it");
            Assert.IsFalse(harness.Manager.CompleteOfferedObjective(harness.Client, vincent.EntityId, Ruins, MarshesItLiesInRuinsV1.Report, 1),
                "not Vincent's to hear");
            WildernessRanjaGorgeTests.Reload(harness);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Ruins, MarshesItLiesInRuinsV1.Report), "kept at a reload");
            harness.Drain();

            harness.MoveTo(morrison.Position);
            harness.Drain();
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, morrison.EntityId, Ruins, MarshesItLiesInRuinsV1.Report, 1));
            var talk = harness.Drain();
            Assert.AreEqual(1, talk.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Ruins));
            Assert.AreEqual(1, talk.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Ruins && packet.IsCompleteable));
            Assert.IsEmpty(Topics(harness, morrison, Ruins), "heard once");
            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, vincent.EntityId, Ruins, 1), "not Vincent's to take");

            FullBagWaits(harness, morrison, Ruins, 1, MarshesRewardBlocksV1.Current(MarshesItLiesInRuinsV1.MissionId, MarshesItLiesInRuinsV1.HazmatVest));
            FailedTurnInPaysNothing(harness, morrison, Ruins, 1);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, morrison, Ruins, 1,
                MarshesItLiesInRuinsV1.Experience, MarshesItLiesInRuinsV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Ruins));
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, MarshesRewardBlocksV1.Current(MarshesItLiesInRuinsV1.MissionId, MarshesItLiesInRuinsV1.MechVest)), "the selected vest");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, vincent, Ruins), "done once");
        }

        /// <summary>The MissionGained of an accept comes once; an abandon takes the report back and the mission is offered again.</summary>
        [TestMethod]
        public void ItLiesInRuinsAbandonedStartsOver()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            SeedCompleted(harness, PlateauRetreadsToRetreadsV1.MissionId);
            var marshes = EnterMarshes(harness);
            var vincent = Npc(marshes, Vincent);
            var morrison = Npc(marshes, Morrison);
            Accept(harness, vincent, Ruins);
            Talk(harness, morrison, Ruins, MarshesItLiesInRuinsV1.Report);
            Assert.IsTrue(harness.Client.Player.Missions[Ruins].Completeable);

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Ruins));
            harness.Drain();
            Assert.IsFalse(harness.Client.Player.Missions.ContainsKey(Ruins), "gone from the log");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, vincent, Ruins), "offered again after an abandon");
            Accept(harness, vincent, Ruins);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Ruins, MarshesItLiesInRuinsV1.Report), "the old report does not count");
            Talk(harness, morrison, Ruins, MarshesItLiesInRuinsV1.Report);
        }

        [TestMethod]
        public void TastesLikeGrubberTakesTenSteaksToHorsh()
        {
            var percentile = 0;
            using var harness = WildernessRuntimeTestHarness.Create(
                lootRoll: (minimum, maximum) => maximum == 100 ? percentile : minimum);
            var marshes = EnterMarshes(harness);
            var horsh = Npc(marshes, Horsh);
            var morrison = Npc(marshes, Morrison);
            var pools = MarshesFalconHoldNpcsV1.SteakPools.Select(pool => pool.Id).ToArray();
            Spawn(marshes, pools.Append(MarshesPaludosNpcsV1.GasPools.First(pool => pool.Creature == Mox).Id).ToArray());

            var offer = WildernessRanjaGorgeTests.Offer(harness, horsh, Grubber);
            CollectionAssert.AreEqual(new[]
                {
                    MarshesTastesLikeGrubberV1.ResistElectric, MarshesTastesLikeGrubberV1.ResistFire,
                    MarshesTastesLikeGrubberV1.ResistIce, MarshesTastesLikeGrubberV1.ResistVirulent
                },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, horsh, FoodChain), "Fixing the Food Chain waits for it");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, morrison, Grubber), "not Morrison's to give");
            Assert.IsFalse(Holds(Kill(harness, marshes, MarshesTastesLikeGrubberV1.SwampGrubber), MarshesTastesLikeGrubberV1.SteakTemplate),
                "no drop before the mission");
            Accept(harness, horsh, Grubber);
            Assert.IsEmpty(Topics(harness, horsh, Grubber), "Horsh hears no objective: the steaks count as they are picked up");

            // A Grubber gives a steak only under 60; a Mox none.
            percentile = MarshesTastesLikeGrubberV1.SteakChance - 1;
            Assert.IsFalse(Holds(Kill(harness, marshes, Mox), MarshesTastesLikeGrubberV1.SteakTemplate), "a Mox has no steak");
            percentile = MarshesTastesLikeGrubberV1.SteakChance;
            Assert.IsFalse(Holds(Kill(harness, marshes, MarshesTastesLikeGrubberV1.SwampGrubber), MarshesTastesLikeGrubberV1.SteakTemplate), "60 misses");

            percentile = MarshesTastesLikeGrubberV1.SteakChance - 1;
            var completed = 0;
            var completeable = 0;
            while (WildernessRanjaGorgeTests.Held(harness, MarshesTastesLikeGrubberV1.SteakTemplate) < MarshesTastesLikeGrubberV1.SteakCount)
            {
                Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Grubber, MarshesTastesLikeGrubberV1.Gather));
                if (!Alive(marshes).Any(candidate => candidate.DbId == MarshesTastesLikeGrubberV1.SwampGrubber && candidate.Npc == null))
                    Respawn(marshes, pools);
                var held = WildernessRanjaGorgeTests.Held(harness, MarshesTastesLikeGrubberV1.SteakTemplate);
                var loot = Kill(harness, marshes, MarshesTastesLikeGrubberV1.SwampGrubber);
                Assert.IsTrue(Holds(loot, MarshesTastesLikeGrubberV1.SteakTemplate), "59 drops");
                Assert.AreEqual(held, WildernessRanjaGorgeTests.Held(harness, MarshesTastesLikeGrubberV1.SteakTemplate), "a death is not a pickup");
                PickUp(harness, loot);
                Assert.AreEqual(held + 1, WildernessRanjaGorgeTests.Held(harness, MarshesTastesLikeGrubberV1.SteakTemplate));
                var drained = harness.Drain();
                completed += drained.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Grubber);
                completeable += drained.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Grubber && packet.IsCompleteable);
                if (held + 1 == 5)
                {
                    WildernessRanjaGorgeTests.Reload(harness);
                    Assert.AreEqual(5U, WildernessRanjaGorgeTests.Held(harness, MarshesTastesLikeGrubberV1.SteakTemplate), "kept at a reload");
                    Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Grubber, MarshesTastesLikeGrubberV1.Gather));
                    harness.Drain();
                }
            }
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Grubber, MarshesTastesLikeGrubberV1.Gather));
            Assert.AreEqual((1, 1), (completed, completeable), "ObjectiveCompleted and MissionCompleteable once");
            harness.MoveTo(morrison.Position);
            harness.Drain();
            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, morrison.EntityId, Grubber, 2), "not Morrison's to take");

            FailedTurnInPaysNothing(harness, horsh, Grubber, 2);
            Assert.AreEqual(MarshesTastesLikeGrubberV1.SteakCount, WildernessRanjaGorgeTests.Held(harness, MarshesTastesLikeGrubberV1.SteakTemplate),
                "kept at the failed turn-in");
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, horsh, Grubber, 2,
                MarshesTastesLikeGrubberV1.Experience, MarshesTastesLikeGrubberV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Grubber));
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, MarshesTastesLikeGrubberV1.ResistIce), "the selected module");
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, MarshesTastesLikeGrubberV1.SteakTemplate), "Horsh keeps them");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, horsh, Grubber), "done once");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, horsh, FoodChain), "Fixing the Food Chain follows");
        }

        /// <summary>A steak stays on the corpse while the bag is full; an abandon keeps the steaks and the mission is offered again.</summary>
        [TestMethod]
        public void TheSteaksWaitForRoomAndTastesLikeGrubberCanBeAbandoned()
        {
            using var harness = WildernessRuntimeTestHarness.Create(
                lootRoll: (minimum, maximum) => maximum == 100 ? 0 : minimum);
            var marshes = EnterMarshes(harness);
            var horsh = Npc(marshes, Horsh);
            Spawn(marshes, MarshesFalconHoldNpcsV1.SteakPools.Select(pool => pool.Id).ToArray());
            Accept(harness, horsh, Grubber);
            WildernessAliaBranchesTests.FillCategory(harness, MissionItemFiller, 50);

            var loot = Kill(harness, marshes, MarshesTastesLikeGrubberV1.SwampGrubber);
            PickUp(harness, loot);
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, MarshesTastesLikeGrubberV1.SteakTemplate), "no room");
            Assert.IsTrue(Holds(loot, MarshesTastesLikeGrubberV1.SteakTemplate), "still on the corpse");
            WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
            PickUp(harness, loot);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, MarshesTastesLikeGrubberV1.SteakTemplate));

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Grubber));
            harness.Drain();
            // The binding retains the steaks at an abandon (Abandonment Retain, as set 1's parts).
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, MarshesTastesLikeGrubberV1.SteakTemplate), "kept at the abandon");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, horsh, Grubber), "offered again after an abandon");
            Accept(harness, horsh, Grubber);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Grubber, MarshesTastesLikeGrubberV1.Gather), "one steak is not ten");
        }

        [TestMethod]
        public void FixingTheFoodChainPutsTheFeedInTenFeeders()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            var horsh = Npc(marshes, Horsh);
            var morrison = Npc(marshes, Morrison);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, horsh, FoodChain), "it waits for Tastes Like Grubber");
            SeedCompletedMarshes(harness, Grubber);
            marshes = EnterMarshes(harness);
            Assert.IsFalse(marshes.DynamicObjects.Any(obj => obj.SceneMissionId == FoodChain), "no feeder before the mission");

            var offer = WildernessRanjaGorgeTests.Offer(harness, horsh, FoodChain);
            CollectionAssert.AreEqual(new[]
                {
                    (MarshesFixingTheFoodChainV1.EmpBomb, MarshesFixingTheFoodChainV1.EmpBombs),
                    (MarshesFixingTheFoodChainV1.AdrenalineBooster, MarshesFixingTheFoodChainV1.AdrenalineBoosters),
                    (MarshesFixingTheFoodChainV1.FragGrenade, MarshesFixingTheFoodChainV1.FragGrenades),
                    (MarshesFixingTheFoodChainV1.CryoGrenade, MarshesFixingTheFoodChainV1.CryoGrenades)
                },
                offer.SelectableReward.Select(item => (item.ItemTemplateId, item.Quantity)).ToArray());
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, morrison, FoodChain), "not Morrison's to give");

            // No room for the bags: no mission. One slot freed, the ten bags come with it.
            WildernessAliaBranchesTests.FillCategory(harness, MissionItemFiller, 50);
            harness.MoveTo(horsh.Position);
            harness.Drain();
            Assert.IsFalse(harness.Manager.AcceptOfferedMission(harness.Client, horsh.EntityId, FoodChain), "no room for the feed");
            Assert.IsFalse(harness.Client.Player.Missions.ContainsKey(FoodChain));
            Assert.AreEqual(0U, Bags(harness));
            WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
            Accept(harness, horsh, FoodChain);
            Assert.AreEqual(MarshesFixingTheFoodChainV1.Bags, Bags(harness), "ten bags");
            Assert.IsEmpty(Topics(harness, horsh, FoodChain), "Horsh hears no objective");

            var feeders = Feeders(harness, marshes);
            var completed = 0;
            var completeable = 0;
            for (var index = 0; index < MarshesFixingTheFoodChainV1.Bags; index++)
            {
                Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, FoodChain, MarshesFixingTheFoodChainV1.Deliver), $"open before feeder {index + 1}");
                Feed(harness, marshes, feeders[index], recovers: true);
                Assert.AreEqual((UseObjectState)MarshesFixingTheFoodChainV1.FedState, feeders[index].StateId, $"feeder {index + 1} fed");
                Assert.IsFalse(feeders[index].IsEnabled, $"feeder {index + 1} closed");
                Assert.AreEqual(MarshesFixingTheFoodChainV1.Bags - 1 - (uint)index, Bags(harness), $"a bag for feeder {index + 1}");
                var drained = harness.Drain();
                completed += drained.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == FoodChain);
                completeable += drained.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == FoodChain && packet.IsCompleteable);
                if (index == 0)
                {
                    Feed(harness, marshes, feeders[0], recovers: false);
                    Assert.AreEqual(MarshesFixingTheFoodChainV1.Bags - 1, Bags(harness), "a fed feeder takes no second bag");
                }
                if (index == 4)
                {
                    WildernessRanjaGorgeTests.Reload(harness);
                    Assert.AreEqual(5U, Bags(harness), "kept at a reload");
                    Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, FoodChain, MarshesFixingTheFoodChainV1.Deliver));
                    harness.Drain();
                    feeders = Feeders(harness, marshes, fed: 5);
                }
            }
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, FoodChain, MarshesFixingTheFoodChainV1.Deliver), "ten fed");
            Assert.AreEqual((1, 1), (completed, completeable), "ObjectiveCompleted and MissionCompleteable once");
            Assert.AreEqual(0U, Bags(harness));
            foreach (var spare in feeders.Skip((int)MarshesFixingTheFoodChainV1.Bags))
                Feed(harness, marshes, spare, recovers: false);
            harness.MoveTo(morrison.Position);
            harness.Drain();
            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, morrison.EntityId, FoodChain, 3), "not Morrison's to take");

            FailedTurnInPaysNothing(harness, horsh, FoodChain, 3);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, horsh, FoodChain, 3,
                MarshesFixingTheFoodChainV1.Experience, MarshesFixingTheFoodChainV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == FoodChain));
            Assert.AreEqual(MarshesFixingTheFoodChainV1.CryoGrenades, WildernessRanjaGorgeTests.Held(harness, MarshesFixingTheFoodChainV1.CryoGrenade),
                "the selected four Cryogenic Grenades");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, horsh, FoodChain), "done once");
        }

        /// <summary>An abandon takes the bags and the feeders away; taken again, ten new bags and twelve unfed feeders.</summary>
        [TestMethod]
        public void FixingTheFoodChainAbandonedTakesTheFeedBack()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            SeedCompletedMarshes(harness, Grubber);
            var marshes = EnterMarshes(harness);
            var horsh = Npc(marshes, Horsh);
            Accept(harness, horsh, FoodChain);
            var feeders = Feeders(harness, marshes);
            Feed(harness, marshes, feeders[0], recovers: true);
            Feed(harness, marshes, feeders[1], recovers: true);
            Assert.AreEqual(MarshesFixingTheFoodChainV1.Bags - 2, Bags(harness));

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, FoodChain));
            harness.Drain();
            Assert.AreEqual(0U, Bags(harness), "the bags go with the mission");
            Assert.IsFalse(marshes.DynamicObjects.Any(obj => obj.SceneMissionId == FoodChain && obj.IsInWorld), "the feeders go with it");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, horsh, FoodChain), "offered again after an abandon");

            Accept(harness, horsh, FoodChain);
            Assert.AreEqual(MarshesFixingTheFoodChainV1.Bags, Bags(harness), "ten new bags");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, FoodChain, MarshesFixingTheFoodChainV1.Deliver));
            Feeders(harness, marshes);
        }

        /// <summary>
        /// The twelve feeders: the player's own acceptors, named "Mox Feeders", on the map's Treeback Feeders, on the
        /// navmesh and walked to from Falcon Hold; all ready but <paramref name="fed"/> (the first, fed and closed).
        /// </summary>
        private static DynamicObject[] Feeders(WildernessRuntimeTestHarness harness, MapChannel marshes, int fed = 0)
        {
            var feeders = Enumerable.Range(0, MarshesFixingTheFoodChainV1.Feeders.Length).Select(index => marshes.DynamicObjects.Single(obj =>
                obj.SceneMissionId == FoodChain && obj.SceneActorRole == MarshesFixingTheFoodChainV1.FeederRole(index) && obj.IsInWorld)).ToArray();
            for (var index = 0; index < feeders.Length; index++)
            {
                var feeder = feeders[index];
                var at = MarshesFixingTheFoodChainV1.Feeders[index];
                Assert.AreEqual((MarshesFixingTheFoodChainV1.FeederClass, MarshesFixingTheFoodChainV1.FeederName),
                    ((uint)feeder.EntityClassId, feeder.NameOverrideId), $"feeder {index + 1}");
                Assert.AreEqual(new Vector3(at.X, at.Y, at.Z), feeder.Position);
                Assert.AreEqual(harness.Client.Player.Id, feeder.SceneOwnerCharacterId, "the player's own");
                Assert.AreEqual(index < fed ? MarshesFixingTheFoodChainV1.FedState : MarshesFixingTheFoodChainV1.ReadyState, (uint)feeder.StateId,
                    $"feeder {index + 1} state");
                Assert.AreEqual(index >= fed, feeder.IsEnabled, $"feeder {index + 1} open");
                AssertOnGround(marshes, feeder.Position, $"feeder {index + 1}");
                Assert.IsTrue(marshes.NavMesh.FindPath(FalconHoldWaypoint, feeder.Position, out var complete) != null && complete,
                    $"feeder {index + 1} is walked to");
            }
            return feeders;
        }

        private static void Feed(WildernessRuntimeTestHarness harness, MapChannel marshes, DynamicObject feeder, bool recovers)
        {
            harness.MoveTo(feeder.Position + new Vector3(0, 0, 1));
            harness.Drain();
            Use(harness, marshes, feeder, MarshesFixingTheFoodChainV1.UseArgument, recovers);
        }

        private static uint Bags(WildernessRuntimeTestHarness harness) =>
            WildernessRanjaGorgeTests.Held(harness, MarshesFixingTheFoodChainV1.FeedTemplate);

        /// <summary>The objectives of <paramref name="mission"/> that <paramref name="npc"/> offers to hear now (as PoolsRetreadChainTests).</summary>
        internal static uint[] Topics(WildernessRuntimeTestHarness harness, Creature npc, uint mission) =>
            Conversation(harness, npc).TryGetValue(ConversationType.ObjectiveComplete, out var value)
                ? ((List<CompleteableObjectives>)value).Where(topic => topic.MissionId == mission)
                    .Select(topic => (uint)topic.ObjectiveId).ToArray()
                : Array.Empty<uint>();

        /// <summary>A full bag and inbox: the turn-in pays nothing; one slot freed, the caller turns it in (as PoolsRetreadChainTests).</summary>
        internal static void FullBagWaits(WildernessRuntimeTestHarness harness, Creature receiver, uint mission, int? choice, uint filler)
        {
            WildernessAliaBranchesTests.FillCategory(harness, filler, 50);
            harness.FillInbox();
            harness.MoveTo(receiver.Position);
            var before = harness.Context.ReadRewardTotals();
            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, receiver.EntityId, mission, choice), "no room");
            Assert.AreEqual(before, harness.Context.ReadRewardTotals());
            Assert.IsTrue(harness.Client.Player.Missions[mission].Completeable, "still to be turned in");
            WildernessAliaBranchesTests.FreeOneSlot(harness, filler);
            harness.Drain();
        }

        /// <summary>Completed Marshes missions in the character's history, as PlateauCampResistanceTests.SeedCompleted the Plateau's.</summary>
        internal static void SeedCompletedMarshes(WildernessRuntimeTestHarness harness, params uint[] completed)
        {
            using (var unit = harness.CreateChar())
                unit.ExecuteTransaction(() =>
                {
                    foreach (var mission in completed)
                        unit.CharacterMissions.Runtime.Add(new CharacterMissionHistoryEntry
                        {
                            CharacterId = harness.Client.Player.Id,
                            MissionId = mission,
                            AssignmentId = Guid.NewGuid().ToString("N"),
                            AssignmentGeneration = 1,
                            ContentRevision = MarshesMissionDataV1.Revision,
                            CompletedAtUtc = harness.UtcNow,
                            RewardedAtUtc = harness.UtcNow,
                            Outcome = (uint)MissionState.Completed,
                            Rewarded = true
                        });
                });
            WildernessRanjaGorgeTests.Reload(harness);
        }
    }
}
