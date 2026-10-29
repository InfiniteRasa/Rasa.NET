extern alias RasaGame;

using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Managers;
using Rasa.Packets.MapChannel.Client;
using Rasa.Packets.Mission.Server;
using Rasa.Services.Preloader.Missions.Plateau;
using Rasa.Services.Preloader.Missions.Pools;
using Rasa.Structures;
using Rasa.Test.Missions.Plateau;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions.Pools
{
    using static PlateauCampResistanceTests;

    /// <summary>
    /// The Retread chain at the Retread Camp, played through as a player plays it: A Cult Divided (560: the Hermit's token
    /// to McKormic), Testing the Pools (561: three water samples for Venma), One Man's Trash (562: Thrax ammo and ration
    /// packs for Buckley), Retread Planning (563: by radio, fifteen talks with the five at the camp, then the Hermit) and
    /// Retread Planning Part II (1330: the Hermit sends the player to Amee).
    ///
    /// Why these tests: four new NPCs stand at the camp; 561's last objective is an aggregate; 563 opens three topics on each
    /// of five NPCs at once, counts them on three aggregates and opens its last objective only through a hidden aggregate
    /// and an objective-state gate. A topic missing or doubled, a count on the wrong topic, the Hermit heard too early, an
    /// item kept after an abandon, or progress lost at a reload would each break them.
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class PoolsRetreadChainTests
    {
        private const uint CultDivided = PoolsCultDividedV1.MissionId;
        private const uint Testing = PoolsTestingThePoolsV1.MissionId;
        private const uint Trash = PoolsOneMansTrashV1.MissionId;
        private const uint Planning = PoolsRetreadPlanningV1.MissionId;
        private const uint PartTwo = PoolsRetreadPlanningPartTwoV1.MissionId;
        private const uint MissionItemFiller = 11519;      // as PlateauCampResistanceTests

        /// <summary>The Retread Camp's waypoint area: Amee's place.</summary>
        private static readonly Vector3 Camp = new(-145, 861.93f, 521);

        private static readonly (uint Npc, uint Name, uint Package, (double X, double Y, double Z) At)[] Retreads =
        {
            (PoolsRetreadCampNpcsV1.McKormic, 4367, 326, PoolsRetreadCampNpcsV1.McKormicAt),
            (PoolsRetreadCampNpcsV1.Venma, 4368, 327, PoolsRetreadCampNpcsV1.VenmaAt),
            (PoolsRetreadCampNpcsV1.Buckley, 4369, 328, PoolsRetreadCampNpcsV1.BuckleyAt),
            (PoolsRetreadCampNpcsV1.Rickert, 4374, 333, PoolsRetreadCampNpcsV1.RickertAt),
        };

        [TestMethod]
        public void TheFourRetreadsStandRoundAmee()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pools = PlateauValverdePoolsTests.OntoPools(harness);
            foreach (var (id, _, _, at) in Retreads)
            {
                var npc = Npc(pools, id);
                // Names, level and packages: PoolsMissionTests pins the rows.
                Assert.AreEqual(id, npc.DbId);
                var place = new Vector3((float)at.X, (float)at.Y, (float)at.Z);
                Assert.AreEqual(place, npc.SpawnPool.Position);
                var ground = pools.NavMesh.GroundHeight(place);
                Assert.IsNotNull(ground, $"navmesh under {id}");
                Assert.AreEqual(ground.Value + 0.2f, place.Y, 0.3f, $"{id} stands on the ground");
                Assert.IsLessThan(8f, Vector2.Distance(new(place.X, place.Z), new(Camp.X, Camp.Z)), $"{id} stands by Amee");
                Assert.IsTrue(pools.NavMesh.FindPath(Camp, new Vector3(place.X, ground.Value, place.Z), out var complete) != null && complete,
                    $"{id} is walked to");
            }
        }

        [TestMethod]
        public void ACultDividedTakesTheHermitsTokenToMcKormic()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pools = PlateauValverdePoolsTests.OntoPools(harness);
            var hermit = Npc(pools, PlateauBaileyNpcsV1.HermitPool);
            var mckormic = Npc(pools, PoolsRetreadCampNpcsV1.McKormic);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, hermit, CultDivided), "not before The Thirsty Hermit");
            SeedCompleted(harness, PoolsThirstyHermitV1.MissionId);

            var offer = WildernessRanjaGorgeTests.Offer(harness, hermit, CultDivided);
            CollectionAssert.AreEquivalent(new[]
            {
                (PoolsCultDividedV1.MedPack, 2U), (PoolsCultDividedV1.ArmorCharger, 2U), (PoolsCultDividedV1.IceGrenade, 4U),
                (PoolsCultDividedV1.FireGrenade, 4U)
            }, offer.FixedReward.FixedItems.Select(item => (item.ItemTemplateId, item.Quantity)).ToArray());
            Accept(harness, hermit, CultDivided);
            Assert.AreEqual(1U, Token(harness), "the Hermit's token");

            WildernessRanjaGorgeTests.Reload(harness);
            Assert.AreEqual(1U, Token(harness), "kept at a reload");
            harness.Drain();

            Talk(harness, mckormic, CultDivided, PoolsCultDividedV1.FindCamp);
            Assert.AreEqual(0U, Token(harness), "McKormic takes it");
            Assert.IsTrue(harness.Client.Player.Missions[CultDivided].Completeable);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, mckormic, CultDivided, null,
                PoolsCultDividedV1.Experience, PoolsCultDividedV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == CultDivided));
            Assert.AreEqual(4U, WildernessRanjaGorgeTests.Held(harness, PoolsCultDividedV1.FireGrenade));
        }

        [TestMethod]
        public void ACultDividedAbandonedTakesTheToken()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pools = PlateauValverdePoolsTests.OntoPools(harness);
            var hermit = Npc(pools, PlateauBaileyNpcsV1.HermitPool);
            SeedCompleted(harness, PoolsThirstyHermitV1.MissionId);
            Accept(harness, hermit, CultDivided);
            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, CultDivided));
            harness.Drain();
            Assert.AreEqual(0U, Token(harness), "the token goes with the mission");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, hermit, CultDivided), "offered again");
        }

        [TestMethod]
        public void TestingThePoolsFillsThreeVialsInAnyOrder()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pools = PlateauValverdePoolsTests.OntoPools(harness);
            var venma = Npc(pools, PoolsRetreadCampNpcsV1.Venma);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, venma, Testing), "not before A Cult Divided");
            SeedCompleted(harness, CultDivided);

            var offer = WildernessRanjaGorgeTests.Offer(harness, venma, Testing);
            WildernessRanjaGorgeTests.AssertSelectable(offer, Testing,
                (PoolsTestingThePoolsV1.BioHelmet, 23384), (PoolsTestingThePoolsV1.HazmatVest, 13832),
                (PoolsTestingThePoolsV1.MotorAssistVest, 16426), (PoolsTestingThePoolsV1.ReflectiveVest, 18626));
            Accept(harness, venma, Testing);
            Assert.AreEqual(PoolsTestingThePoolsV1.Vials, Vials(harness), "three vials on accept");

            var spots = Enumerable.Range(0, 3).Select(index => pools.DynamicObjects.Single(obj => obj.SceneMissionId == Testing &&
                obj.SceneActorRole == PoolsTestingThePoolsV1.SpotRole(index))).ToArray();
            for (var index = 0; index < spots.Length; index++)
            {
                var at = PoolsTestingThePoolsV1.Points[index].At;
                Assert.AreEqual(PoolsTestingThePoolsV1.SpotClass, (uint)spots[index].EntityClassId);
                Assert.AreEqual(new Vector3(at.X, at.Y, at.Z), spots[index].Position);
                Assert.AreEqual(harness.Client.Player.Id, spots[index].SceneOwnerCharacterId, "the player's own");
                AssertOnTheGround(pools, spots[index].Position, $"sample {index + 1}");
                AssertWalkedTo(pools, spots[index].Position, $"sample {index + 1}");
            }
            harness.Drain();

            // West, north, east: each spot a vial for its sample, once.
            var filled = 0U;
            foreach (var index in new[] { 2, 0, 1 })
            {
                var (objective, template, _, _, _) = PoolsTestingThePoolsV1.Points[index];
                harness.MoveTo(spots[index].Position + new Vector3(0, 0, 1));
                Use(harness, pools, spots[index], recovers: true);
                filled++;
                Assert.AreEqual(PoolsTestingThePoolsV1.Vials - filled, Vials(harness), $"spot {index + 1} takes a vial");
                Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, template), $"sample {template}");
                Assert.IsFalse(spots[index].IsEnabled);
                Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Testing, objective));
                if (filled == 1)
                {
                    Use(harness, pools, spots[index], recovers: false);
                    Assert.AreEqual(2U, Vials(harness), "one vial a spot");
                    WildernessRanjaGorgeTests.Reload(harness);
                    Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Testing, objective), "kept at a reload");
                    harness.Drain();
                }
                Assert.AreEqual(filled == 3 ? MissionObjectiveState.Completed : MissionObjectiveState.Incomplete,
                    Objective(harness, Testing, PoolsTestingThePoolsV1.Samples), "5 waits for all three");
            }
            Assert.IsTrue(harness.Client.Player.Missions[Testing].Completeable);

            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, venma, Testing, 2,
                PoolsTestingThePoolsV1.Experience, PoolsTestingThePoolsV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Testing));
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PoolsTestingThePoolsV1.MotorAssistVest), "the selected vest");
            foreach (var point in PoolsTestingThePoolsV1.Points)
                Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, point.Template), "Venma keeps the samples");
        }

        [TestMethod]
        public void TestingThePoolsAbandonedTakesVialsAndSamples()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pools = PlateauValverdePoolsTests.OntoPools(harness);
            var venma = Npc(pools, PoolsRetreadCampNpcsV1.Venma);
            SeedCompleted(harness, CultDivided);
            Accept(harness, venma, Testing);
            var spot = pools.DynamicObjects.Single(obj => obj.SceneMissionId == Testing && obj.SceneActorRole == PoolsTestingThePoolsV1.SpotRole(1));
            harness.MoveTo(spot.Position + new Vector3(0, 0, 1));
            Use(harness, pools, spot, recovers: true);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PoolsTestingThePoolsV1.Points[1].Template));

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Testing));
            harness.Drain();
            Assert.AreEqual(0U, Vials(harness), "the vials go");
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, PoolsTestingThePoolsV1.Points[1].Template), "the sample goes");
            Assert.IsFalse(pools.DynamicObjects.Any(obj => obj.SceneMissionId == Testing && obj.IsInWorld), "the spots go");
            Accept(harness, venma, Testing);
            Assert.AreEqual(3U, Vials(harness), "three again");
        }

        [TestMethod]
        public void OneMansTrashTakesTheAmmoAndTheRationsInEitherOrder()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pools = PlateauValverdePoolsTests.OntoPools(harness);
            var buckley = Npc(pools, PoolsRetreadCampNpcsV1.Buckley);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, buckley, Trash), "not before A Cult Divided");
            SeedCompleted(harness, CultDivided);

            var offer = WildernessRanjaGorgeTests.Offer(harness, buckley, Trash);
            WildernessRanjaGorgeTests.AssertSelectable(offer, Trash,
                (PoolsOneMansTrashV1.HazmatHelmet, 13740), (PoolsOneMansTrashV1.MechVest, 18073),
                (PoolsOneMansTrashV1.MotorAssistHelmet, 16326), (PoolsOneMansTrashV1.ReflectiveHelmet, 18534));
            Accept(harness, buckley, Trash);

            var crates = Enumerable.Range(0, 2).Select(index => pools.DynamicObjects.Single(obj => obj.SceneMissionId == Trash &&
                obj.SceneActorRole == PoolsOneMansTrashV1.CrateRole(index))).ToArray();
            for (var index = 0; index < crates.Length; index++)
            {
                var (_, _, _, _, crateClass, at) = PoolsOneMansTrashV1.Crates[index];
                Assert.AreEqual(crateClass, (uint)crates[index].EntityClassId);
                Assert.AreEqual(new Vector3(at.X, at.Y, at.Z), crates[index].Position);
                Assert.AreEqual(harness.Client.Player.Id, crates[index].SceneOwnerCharacterId, "the player's own");
                AssertOnTheGround(pools, crates[index].Position, crates[index].SceneActorRole);
                AssertWalkedTo(pools, crates[index].Position, crates[index].SceneActorRole);
            }
            harness.Drain();

            // The rations first, then the ammo.
            foreach (var index in new[] { 1, 0 })
            {
                var (objective, template, _, _, _, _) = PoolsOneMansTrashV1.Crates[index];
                harness.MoveTo(crates[index].Position + new Vector3(0, 0, 1));
                Use(harness, pools, crates[index], recovers: true);
                Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, template), $"item {template}");
                Assert.IsFalse(crates[index].IsEnabled, "opened once");
                Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Trash, objective));
                Use(harness, pools, crates[index], recovers: false);
                Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, template), "one of each");
                if (index == 1)
                {
                    Assert.IsFalse(harness.Client.Player.Missions[Trash].Completeable, "the ammo too");
                    WildernessRanjaGorgeTests.Reload(harness);
                    Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Trash, objective), "kept at a reload");
                    harness.Drain();
                }
            }
            Assert.IsTrue(harness.Client.Player.Missions[Trash].Completeable);

            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, buckley, Trash, 1,
                PoolsOneMansTrashV1.Experience, PoolsOneMansTrashV1.Credits);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PoolsOneMansTrashV1.MechVest), "the selected vest");
            foreach (var crate in PoolsOneMansTrashV1.Crates)
                Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, crate.Template), "Buckley keeps them");
        }

        /// <summary>
        /// 563 comes by radio once 562 is turned in. Each of the five offers its three topics at once; the counts move on
        /// their own topic only; the Hermit hears nothing until all fifteen are done; then 1330 sends the player to Amee.
        /// </summary>
        [TestMethod]
        public void RetreadPlanningHearsFifteenTalksThenTheHermitAndPartTwoEndsAtAmee()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pools = PlateauValverdePoolsTests.OntoPools(harness);
            var hermit = Npc(pools, PlateauBaileyNpcsV1.HermitPool);
            var amee = Npc(pools, PoolsRetreadCampNpcsV1.Amee);
            var buckley = Npc(pools, PoolsRetreadCampNpcsV1.Buckley);
            var talkers = new Dictionary<uint, Creature>
            {
                [PoolsRetreadCampNpcsV1.AmeePackage] = amee,
                [PoolsRetreadCampNpcsV1.BuckleyPackage] = buckley,
                [PoolsRetreadCampNpcsV1.VenmaPackage] = Npc(pools, PoolsRetreadCampNpcsV1.Venma),
                [PoolsRetreadCampNpcsV1.McKormicPackage] = Npc(pools, PoolsRetreadCampNpcsV1.McKormic),
                [PoolsRetreadCampNpcsV1.RickertPackage] = Npc(pools, PoolsRetreadCampNpcsV1.Rickert),
            };

            // One Man's Trash turned in: Headquarters calls.
            SeedCompleted(harness, CultDivided);
            Assert.IsFalse(harness.Manager.TryAcceptRadioMission(harness.Client, Planning), "not before One Man's Trash");
            Accept(harness, buckley, Trash);
            Assert.IsTrue(harness.Manager.RecordProgress(harness.Client, new[]
            {
                MissionProgressEvent.Scenario(Trash, PoolsOneMansTrashV1.CrateScenario, 1),
                MissionProgressEvent.Scenario(Trash, PoolsOneMansTrashV1.CrateScenario, 2)
            }));
            harness.Drain();
            Assert.IsTrue(harness.Manager.CompleteOfferedMission(harness.Client, buckley.EntityId, Trash, 0));
            harness.Drain();
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, hermit, Planning), "a radio mission, not the Hermit's offer");
            Assert.IsTrue(harness.Manager.TryAcceptRadioMission(harness.Client, Planning));
            harness.Drain();
            Assert.AreEqual(MissionObjectiveState.Inactive, Objective(harness, Planning, PoolsRetreadPlanningV1.ReturnToHermit));
            Assert.IsFalse(harness.Manager.CompleteOfferedObjective(harness.Client, hermit.EntityId, Planning,
                PoolsRetreadPlanningV1.ReturnToHermit, 1), "the Hermit waits");

            // Each NPC has its three topics.
            foreach (var (package, npc) in talkers)
            {
                var index = System.Array.IndexOf(PoolsRetreadPlanningV1.Packages, package);
                CollectionAssert.AreEquivalent(PoolsRetreadPlanningV1.Topics.Select(topic => topic.FirstTalk + (uint)index).ToArray(),
                    Topics(harness, npc, Planning), $"package {package}: who, what, where");
            }

            // Who from all five, then What and Where mixed; a topic heard once.
            var heard = new Dictionary<uint, uint> { [PoolsRetreadPlanningV1.Who] = 0, [PoolsRetreadPlanningV1.What] = 0, [PoolsRetreadPlanningV1.Where] = 0 };
            var order = PoolsRetreadPlanningV1.Talks(PoolsRetreadPlanningV1.Who)
                .Concat(PoolsRetreadPlanningV1.Talks(PoolsRetreadPlanningV1.What).Zip(PoolsRetreadPlanningV1.Talks(PoolsRetreadPlanningV1.Where).Reverse(),
                    (what, where) => new[] { where, what }).SelectMany(pair => pair)).ToArray();
            Assert.HasCount(15, order);
            foreach (var (talk, step) in order.Select((talk, step) => (talk, step)))
            {
                var topic = PoolsRetreadPlanningV1.Topics.Single(entry => talk >= entry.FirstTalk && talk < entry.FirstTalk + 5);
                var npc = talkers[PoolsRetreadPlanningV1.Packages[talk - topic.FirstTalk]];
                Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, npc.EntityId, Planning, talk, 1), $"talk {talk}");
                harness.Drain();
                heard[topic.Topic]++;
                Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Planning, talk));
                foreach (var (counting, count) in heard)
                    Assert.AreEqual(count, harness.Client.Player.Missions[Planning].Objectives[counting].Counters[0], $"after {talk}: {counting}");
                Assert.AreEqual(heard[topic.Topic] == 5 ? MissionObjectiveState.Completed : MissionObjectiveState.Incomplete,
                    Objective(harness, Planning, topic.Topic));
                Assert.IsFalse(harness.Manager.CompleteOfferedObjective(harness.Client, npc.EntityId, Planning, talk, 1), "heard once");
                if (step == 6)
                {
                    WildernessRanjaGorgeTests.Reload(harness);
                    Assert.AreEqual(5U, harness.Client.Player.Missions[Planning].Objectives[PoolsRetreadPlanningV1.Who].Counters[0], "kept at a reload");
                    harness.Drain();
                }
                if (step < 14)
                    Assert.AreEqual(MissionObjectiveState.Inactive, Objective(harness, Planning, PoolsRetreadPlanningV1.ReturnToHermit),
                        $"after {talk} the Hermit still waits");
            }
            Assert.AreEqual((MissionObjectiveState.Completed, MissionObjectiveState.Completed, MissionObjectiveState.Incomplete),
                (Objective(harness, Planning, PoolsRetreadPlanningV1.AllKnown), Objective(harness, Planning, PoolsRetreadPlanningV1.Gate),
                    Objective(harness, Planning, PoolsRetreadPlanningV1.ReturnToHermit)));
            Assert.IsFalse(harness.Client.Player.Missions[Planning].Completeable);
            Assert.IsEmpty(Topics(harness, amee, Planning), "Amee has nothing more for 563");

            Talk(harness, hermit, Planning, PoolsRetreadPlanningV1.ReturnToHermit);
            Assert.IsTrue(harness.Client.Player.Missions[Planning].Completeable);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, hermit, Planning, null,
                PoolsRetreadPlanningV1.Experience, PoolsRetreadPlanningV1.Credits);
            Assert.AreEqual(2U, WildernessRanjaGorgeTests.Held(harness, PoolsRetreadPlanningV1.EmpBomb));

            // 1330: the Hermit, then Amee.
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, amee, PartTwo), "the Hermit's");
            Accept(harness, hermit, PartTwo);
            Talk(harness, amee, PartTwo, PoolsRetreadPlanningPartTwoV1.Speak);
            Assert.IsTrue(harness.Client.Player.Missions[PartTwo].Completeable);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, amee, PartTwo, null,
                PoolsRetreadPlanningPartTwoV1.Experience, PoolsRetreadPlanningPartTwoV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == PartTwo));
        }

        /// <summary>
        /// A full bag: the vials wait at the accept, a spot or a crate waits at its use, and a turn-in pays nothing until a
        /// slot is free; a turn-in whose save fails pays nothing and can be done again.
        /// </summary>
        [TestMethod]
        public void FullBagsAndFailedSavesWait()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pools = PlateauValverdePoolsTests.OntoPools(harness);
            var venma = Npc(pools, PoolsRetreadCampNpcsV1.Venma);
            var buckley = Npc(pools, PoolsRetreadCampNpcsV1.Buckley);
            SeedCompleted(harness, CultDivided);

            WildernessAliaBranchesTests.FillCategory(harness, MissionItemFiller, 50);
            harness.MoveTo(venma.Position);
            harness.Drain();
            Assert.IsFalse(harness.Manager.AcceptOfferedMission(harness.Client, venma.EntityId, Testing), "no room for the vials");
            Assert.AreEqual(0U, Vials(harness));
            Assert.IsFalse(harness.Client.Player.Missions.ContainsKey(Testing));
            WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
            Accept(harness, venma, Testing);
            Assert.AreEqual(3U, Vials(harness));

            // The vials took the freed slot: the bag is full again. A spot waits for room for its sample.
            Accept(harness, buckley, Trash);
            var spot = pools.DynamicObjects.Single(obj => obj.SceneMissionId == Testing && obj.SceneActorRole == PoolsTestingThePoolsV1.SpotRole(0));
            harness.MoveTo(spot.Position + new Vector3(0, 0, 1));
            Use(harness, pools, spot, recovers: true);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Testing, PoolsTestingThePoolsV1.North));
            Assert.IsTrue(spot.IsEnabled, "the spot waits");
            Assert.AreEqual(3U, Vials(harness), "the vial is kept");
            var crate = pools.DynamicObjects.Single(obj => obj.SceneMissionId == Trash && obj.SceneActorRole == PoolsOneMansTrashV1.CrateRole(0));
            harness.MoveTo(crate.Position + new Vector3(0, 0, 1));
            Use(harness, pools, crate, recovers: true);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Trash, PoolsOneMansTrashV1.Ammo));
            Assert.IsTrue(crate.IsEnabled, "the crate waits");
            WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
            Use(harness, pools, crate, recovers: true);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Trash, PoolsOneMansTrashV1.Ammo), "with room it gives");
        }

        /// <summary>The turn-ins of 561 and 562: a failed save and a full bag pay nothing; then each pays once.</summary>
        [TestMethod]
        public void FailedAndFullTurnInsWait()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pools = PlateauValverdePoolsTests.OntoPools(harness);
            var venma = Npc(pools, PoolsRetreadCampNpcsV1.Venma);
            var buckley = Npc(pools, PoolsRetreadCampNpcsV1.Buckley);
            SeedCompleted(harness, CultDivided);
            Accept(harness, venma, Testing);
            Accept(harness, buckley, Trash);
            Assert.IsTrue(harness.Manager.RecordProgress(harness.Client, new[]
            {
                MissionProgressEvent.Scenario(Testing, PoolsTestingThePoolsV1.SampleScenario, 1),
                MissionProgressEvent.Scenario(Testing, PoolsTestingThePoolsV1.SampleScenario, 2),
                MissionProgressEvent.Scenario(Testing, PoolsTestingThePoolsV1.SampleScenario, 3),
                MissionProgressEvent.Scenario(Trash, PoolsOneMansTrashV1.CrateScenario, 1),
                MissionProgressEvent.Scenario(Trash, PoolsOneMansTrashV1.CrateScenario, 2)
            }));
            harness.Drain();
            Assert.IsTrue(harness.Client.Player.Missions[Testing].Completeable && harness.Client.Player.Missions[Trash].Completeable);

            FailedTurnInPaysNothing(harness, venma, Testing, 0);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, venma, Testing, 0,
                PoolsTestingThePoolsV1.Experience, PoolsTestingThePoolsV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Testing));
            FailedTurnInPaysNothing(harness, buckley, Trash, 3);
            FullBagWaits(harness, buckley, Trash, 3, PoolsOneMansTrashV1.ReflectiveHelmet);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, buckley, Trash, 3,
                PoolsOneMansTrashV1.Experience, PoolsOneMansTrashV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Trash));
            Assert.AreEqual(50U, WildernessRanjaGorgeTests.Held(harness, PoolsOneMansTrashV1.ReflectiveHelmet), "49 fillers and the selected helmet");
        }

        [TestMethod]
        public void OneMansTrashAbandonedTakesItsItemsAndCrates()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pools = PlateauValverdePoolsTests.OntoPools(harness);
            var buckley = Npc(pools, PoolsRetreadCampNpcsV1.Buckley);
            SeedCompleted(harness, CultDivided);
            Accept(harness, buckley, Trash);
            var rations = pools.DynamicObjects.Single(obj => obj.SceneMissionId == Trash && obj.SceneActorRole == PoolsOneMansTrashV1.CrateRole(1));
            harness.MoveTo(rations.Position + new Vector3(0, 0, 1));
            Use(harness, pools, rations, recovers: true);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PoolsOneMansTrashV1.Crates[1].Template));

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Trash));
            harness.Drain();
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, PoolsOneMansTrashV1.Crates[1].Template), "the rations go");
            Assert.IsFalse(pools.DynamicObjects.Any(obj => obj.SceneMissionId == Trash && obj.IsInWorld), "the crates go");
            Accept(harness, buckley, Trash);
            Assert.IsTrue(Enumerable.Range(0, 2).All(index => pools.DynamicObjects.Single(obj => obj.SceneMissionId == Trash &&
                obj.SceneActorRole == PoolsOneMansTrashV1.CrateRole(index) && obj.IsInWorld).IsEnabled), "two new crates");
        }

        /// <summary>
        /// The open gate is saved: after a reload the Hermit still hears 24. An abandon resets the talks, counts and gate; the
        /// radio offers it again. Part II is not offered before 563.
        /// </summary>
        [TestMethod]
        public void RetreadPlanningKeepsItsGateAtAReloadAndAnAbandonResetsIt()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pools = PlateauValverdePoolsTests.OntoPools(harness);
            var hermit = Npc(pools, PlateauBaileyNpcsV1.HermitPool);
            var byPackage = Retreads.ToDictionary(r => r.Package, r => r.Npc);
            byPackage[PoolsRetreadCampNpcsV1.AmeePackage] = PoolsRetreadCampNpcsV1.Amee;
            var talkers = PoolsRetreadPlanningV1.Packages.Select(package => Npc(pools, byPackage[package])).ToArray();
            var buckley = Npc(pools, PoolsRetreadCampNpcsV1.Buckley);
            SeedCompleted(harness, PoolsThirstyHermitV1.MissionId, CultDivided);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, hermit, PartTwo), "not before Retread Planning");
            Accept(harness, buckley, Trash);
            Assert.IsTrue(harness.Manager.RecordProgress(harness.Client, new[]
            {
                MissionProgressEvent.Scenario(Trash, PoolsOneMansTrashV1.CrateScenario, 1),
                MissionProgressEvent.Scenario(Trash, PoolsOneMansTrashV1.CrateScenario, 2)
            }));
            harness.Drain();
            Assert.IsTrue(harness.Manager.CompleteOfferedMission(harness.Client, buckley.EntityId, Trash, 0));
            harness.Drain();
            Assert.IsTrue(harness.Manager.TryAcceptRadioMission(harness.Client, Planning));
            harness.Drain();

            foreach (var talk in PoolsRetreadPlanningV1.AllTalks)
            {
                var npc = talkers[(talk - PoolsRetreadPlanningV1.Topics.Single(entry => talk >= entry.FirstTalk && talk < entry.FirstTalk + 5).FirstTalk)];
                Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, npc.EntityId, Planning, talk, 1), $"talk {talk}");
                harness.Drain();
            }
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Planning, PoolsRetreadPlanningV1.ReturnToHermit));

            WildernessRanjaGorgeTests.Reload(harness);
            Assert.AreEqual((MissionObjectiveState.Completed, MissionObjectiveState.Completed, MissionObjectiveState.Incomplete),
                (Objective(harness, Planning, PoolsRetreadPlanningV1.AllKnown), Objective(harness, Planning, PoolsRetreadPlanningV1.Gate),
                    Objective(harness, Planning, PoolsRetreadPlanningV1.ReturnToHermit)), "kept at a reload");
            harness.Drain();
            CollectionAssert.AreEqual(new[] { PoolsRetreadPlanningV1.ReturnToHermit }, Topics(harness, hermit, Planning), "the Hermit still hears it");

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Planning));
            harness.Drain();
            harness.Manager.OfferArrivalMissions(harness.Client);
            Assert.IsTrue(harness.Drain().OfType<DispenseRadioMissionPacket>().Any(packet => packet.MissionId == Planning), "offered again on arrival");
            Assert.IsTrue(harness.Manager.TryAcceptRadioMission(harness.Client, Planning));
            harness.Drain();
            Assert.AreEqual(MissionObjectiveState.Inactive, Objective(harness, Planning, PoolsRetreadPlanningV1.ReturnToHermit));
            foreach (var topic in new[] { PoolsRetreadPlanningV1.Who, PoolsRetreadPlanningV1.What, PoolsRetreadPlanningV1.Where })
                Assert.AreEqual(0U, harness.Client.Player.Missions[Planning].Objectives[topic].Counters[0], $"{topic} from 0");
            Assert.IsTrue(PoolsRetreadPlanningV1.AllTalks.All(talk => Objective(harness, Planning, talk) == MissionObjectiveState.Incomplete));
            Assert.HasCount(3, Topics(harness, talkers[0], Planning), "Amee's three again");
        }

        /// <summary>A turn-in whose save fails pays nothing; the mission stays to be turned in (as PoolsBaneChainTests).</summary>
        private static void FailedTurnInPaysNothing(WildernessRuntimeTestHarness harness, Creature receiver, uint mission, int? choice)
        {
            harness.MoveTo(receiver.Position);
            harness.Drain();
            var before = harness.Context.ReadRewardTotals();
            harness.Context.BeforeSave = _ => throw new DbUpdateException($"Injected {mission} turn-in failure.");
            try
            {
                Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, receiver.EntityId, mission, choice));
            }
            finally
            {
                harness.Context.BeforeSave = null;
            }
            Assert.AreEqual(before, harness.Context.ReadRewardTotals());
            Assert.IsTrue(harness.Client.Player.Missions[mission].Completeable, "still to be turned in");
            harness.Drain();
        }

        /// <summary>A full bag and inbox: the turn-in pays nothing; one slot freed, the caller turns it in.</summary>
        private static void FullBagWaits(WildernessRuntimeTestHarness harness, Creature receiver, uint mission, int? choice, uint filler)
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

        /// <summary>The objectives of <paramref name="mission"/> that <paramref name="npc"/> offers to hear now.</summary>
        private static uint[] Topics(WildernessRuntimeTestHarness harness, Creature npc, uint mission) =>
            Conversation(harness, npc).TryGetValue(ConversationType.ObjectiveComplete, out var value)
                ? ((List<CompleteableObjectives>)value).Where(topic => topic.MissionId == mission)
                    .Select(topic => (uint)topic.ObjectiveId).ToArray()
                : System.Array.Empty<uint>();

        private static uint Token(WildernessRuntimeTestHarness harness) =>
            WildernessRanjaGorgeTests.Held(harness, PoolsCultDividedV1.TokenTemplate);

        private static uint Vials(WildernessRuntimeTestHarness harness) =>
            WildernessRanjaGorgeTests.Held(harness, PoolsTestingThePoolsV1.VialTemplate);

        private static void AssertOnTheGround(MapChannel pools, Vector3 position, string what)
        {
            var ground = pools.NavMesh.GroundHeight(position);
            Assert.IsNotNull(ground, $"navmesh under {what}");
            Assert.AreEqual(ground.Value, position.Y, 0.5f, $"{what} on the ground");
        }

        private static void AssertWalkedTo(MapChannel pools, Vector3 position, string what) =>
            Assert.IsTrue(pools.NavMesh.FindPath(Camp, position, out var complete) != null && complete, $"{what} is walked to from the camp");

        private static void Accept(WildernessRuntimeTestHarness harness, Creature npc, uint mission)
        {
            harness.MoveTo(npc.Position);
            harness.Drain();
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, npc.EntityId, mission), $"{mission} accepted");
            harness.Drain();
        }

        /// <summary>The NPC hears the objective, and it is done.</summary>
        private static void Talk(WildernessRuntimeTestHarness harness, Creature npc, uint mission, uint objective)
        {
            CollectionAssert.AreEqual(new[] { objective }, Topics(harness, npc, mission), $"{mission}.{objective} topic");
            harness.MoveTo(npc.Position);
            harness.Drain();
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, npc.EntityId, mission, objective, 1), $"{mission}.{objective}");
            harness.Drain();
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, mission, objective));
        }

        /// <summary>The NPC of the pool (the Hermit's creature id is not his pool's).</summary>
        private static Creature Npc(MapChannel pools, uint pool)
        {
            Spawn(pools, pool);
            var npc = Living(pools).SingleOrDefault(creature => creature.SpawnPool?.DbId == pool && creature.Npc != null);
            Assert.IsNotNull(npc, $"Pool {pool} must stand a real public NPC in the Pools.");
            return npc;
        }

        /// <summary>A use of the object: the request, then its recovery if one is due (as PoolsBaneChainTests).</summary>
        private static void Use(WildernessRuntimeTestHarness harness, MapChannel map, DynamicObject obj, bool recovers)
        {
            var count = map.PerformRecovery.Count;
            harness.Objects.RequestUseObjectPacket(harness.Client, new RequestUseObjectPacket
            {
                EntityId = obj.EntityId, ActionId = ActionId.UseObject, ActionArgId = PoolsTestingThePoolsV1.UseArgument
            });
            Assert.AreEqual(count + (recovers ? 1 : 0), map.PerformRecovery.Count, $"use of {obj.SceneActorRole}");
            if (!recovers)
                return;
            var recovery = map.PerformRecovery.Last();
            map.PerformRecovery.Remove(recovery);
            ActorActionManager.Instance.PerformRecovery(map, recovery);
        }

        private static MissionObjectiveState Objective(WildernessRuntimeTestHarness harness, uint mission, uint objective) =>
            harness.Client.Player.Missions[mission].Objectives[objective].State;
    }
}
