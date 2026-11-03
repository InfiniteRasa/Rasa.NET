using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Managers;
using Rasa.Packets.MapChannel.Client;
using Rasa.Packets.MapChannel.Server;
using Rasa.Packets.Mission.Server;
using Rasa.Services.Preloader.Missions.Marshes;
using Rasa.Structures;
using Rasa.Test.Missions.Plateau;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions.Marshes
{
    using static MarshesFalconHoldTests;
    using static MarshesPaludosTests;
    using static PlateauCampResistanceTests;

    /// <summary>
    /// Marshes set 7, Stalker Woods (High Point Retreat), played through as a player plays it: Retread Jeska's Missing in
    /// Action (1863: Lt. Koffman's body east of the camp), Retread Dudley's Light the Fires (1864: five signal fires) and Now
    /// Can You Hear Me? (1869: three communication spikes), and Retread Duvall's Stop and Smell the Maw Flowers (1871: five
    /// shed Maw flowers).
    ///
    /// Why these tests: Jeska, Dudley and Duvall are new on the server, the body, fires, spike spots and flowers are the
    /// player's own scene objects, 1869 gives its spikes at the accept, and 1871 counts on a hidden tally. An NPC or object
    /// off the navmesh, an offer at the wrong NPC, a talk or turn-in at the wrong NPC, an object used with another argument
    /// than the client's or used twice, an item given with no room or kept after an abandon, progress lost at a reload, or a
    /// turn-in that pays twice would each break them. No mission of the set has a drop, so no drop chance is tested.
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class MarshesStalkerWoodsTests
    {
        private const uint Missing = MarshesMissingInActionV1.MissionId;
        private const uint Fires = MarshesLightTheFiresV2.MissionId;
        private const uint Spikes = MarshesNowCanYouHearMeV1.MissionId;
        private const uint Flowers = MarshesMawFlowersV1.MissionId;
        private const uint Jeska = MarshesStalkerWoodsNpcsV1.Jeska, Dudley = MarshesStalkerWoodsNpcsV1.Dudley;
        private const uint Duvall = MarshesStalkerWoodsNpcsV1.Duvall;
        private const uint MissionItemFiller = 11519;      // as PlateauCampResistanceTests
        private const uint ConsumableFiller = 45060;       // Class VII Basic Med Pack: fills the consumables bag

        /// <summary>The Stalker Woods (High Point Retreat) waypoint (World DB teleporter 137).</summary>
        private static readonly Vector3 HighPointRetreatWaypoint = new(-784.272f, 241.4954f, -547.8776f);

        [TestInitialize]
        public void Clock() => PlateauGiantKillerSquadTests.UseClock();

        [TestCleanup]
        public void Restore() => PlateauGiantKillerSquadTests.RestoreClock();

        [TestMethod]
        public void JeskaDudleyDuvallAndTheMawStandAtTheirPlaces()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            foreach (var (pool, name, package, at) in new[]
            {
                (Jeska, MarshesStalkerWoodsNpcsV1.JeskaName, Jeska, MarshesStalkerWoodsNpcsV1.JeskaAt),
                (Dudley, MarshesStalkerWoodsNpcsV1.DudleyName, Dudley, MarshesStalkerWoodsNpcsV1.DudleyAt),
                (Duvall, MarshesStalkerWoodsNpcsV1.DuvallName, 950U, MarshesStalkerWoodsNpcsV1.DuvallAt)
            })
            {
                var npc = Npc(marshes, pool);
                Assert.AreEqual(name, npc.NameId);
                Assert.AreEqual(package, npc.Npc.NpcPackageId, $"{pool}: Jeska's and Dudley's server-only (their ids), Duvall's the client's 950");
                Assert.AreNotEqual(TargetCategory.Hostile, npc.TargetCategory, $"{pool} is no foe");
                Assert.IsLessThan(0.1f, Vector2.Distance(new((float)at.X, (float)at.Z), new(npc.Position.X, npc.Position.Z)), $"{pool} at the place");
                AssertOnGround(marshes, npc.Position, $"{pool}");
                Assert.IsTrue(marshes.NavMesh.FindPath(HighPointRetreatWaypoint, npc.Position, out var reached) != null && reached, $"{pool} is walked to");
            }
            Spawn(marshes, MarshesStalkerWoodsNpcsV1.MawPool);
            // The Maws lie under the ground until someone comes near (MawLurk): Alive counts them too.
            var maws = Alive(marshes).Where(creature => creature.SpawnPool?.DbId == MarshesStalkerWoodsNpcsV1.MawPool).Distinct().ToArray();
            Assert.HasCount(2, maws, "two Maws");
            Assert.IsTrue(maws.All(maw => maw.DbId == MarshesStalkerWoodsNpcsV1.Maw && maw.TargetCategory == TargetCategory.Hostile));
            var field = MarshesMawFlowersV1.FieldAt;
            Assert.IsLessThan(MarshesMawFlowersV1.FieldRadius, Vector2.Distance(new((float)field.X, (float)field.Z),
                new((float)MarshesStalkerWoodsNpcsV1.MawAt.X, (float)MarshesStalkerWoodsNpcsV1.MawAt.Z)), "the Maws among the flowers");
        }

        [TestMethod]
        public void MissingInActionFindsKoffmansBody()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            var jeska = Npc(marshes, Jeska);
            var dudley = Npc(marshes, Dudley);
            Assert.IsFalse(marshes.DynamicObjects.Any(obj => obj.SceneMissionId == Missing), "no body before the mission");

            var offer = WildernessRanjaGorgeTests.Offer(harness, jeska, Missing);
            CollectionAssert.AreEqual(new[]
                {
                    (MarshesMissingInActionV1.MedPack, MarshesMissingInActionV1.MedPacks),
                    (MarshesMissingInActionV1.ArmorCharger, MarshesMissingInActionV1.ArmorChargers),
                    (MarshesMissingInActionV1.CryoGrenade, MarshesMissingInActionV1.CryoGrenades),
                    (MarshesMissingInActionV1.FragGrenade, MarshesMissingInActionV1.FragGrenades)
                },
                offer.SelectableReward.Select(item => (item.ItemTemplateId, item.Quantity)).ToArray());
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, dudley, Missing), "not Dudley's to give");
            Accept(harness, jeska, Missing);
            Assert.IsEmpty(Topics(harness, jeska, Missing), "Jeska hears no objective: the body does");

            var body = Body(harness, marshes);
            WildernessRanjaGorgeTests.Reload(harness);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Missing, MarshesMissingInActionV1.Locate), "kept at a reload");
            harness.Drain();
            body = Body(harness, marshes);

            var talk = OpenTalk(harness, body);
            Assert.IsNotNull(talk, "the body talks");
            Assert.IsTrue(talk.ConvoDataDict.TryGetValue(ConversationType.ObjectiveComplete, out var topics));
            Assert.AreEqual(((int)Missing, (int)MarshesMissingInActionV1.Locate, 1),
                ((List<CompleteableObjectives>)topics).Select(topic => (topic.MissionId, topic.ObjectiveId, topic.PlayerFlagId)).Single());
            new NpcManager(harness, harness.Manager).CompleteNPCObjective(harness.Client, new CompleteNPCObjectivePacket
            {
                EntityId = body.EntityId, MissionId = Missing, ObjectiveId = MarshesMissingInActionV1.Locate, PlayerFlagId = 1
            });
            var found = harness.Drain();
            Assert.AreEqual(1, found.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Missing), "ObjectiveCompleted once");
            Assert.AreEqual(1, found.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Missing && packet.IsCompleteable),
                "MissionCompleteable once");
            Assert.IsNull(OpenTalk(harness, body), "read once");

            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, dudley.EntityId, Missing, 2), "not Dudley's to take");
            FullBagWaits(harness, jeska, Missing, 2, ConsumableFiller);
            FailedTurnInPaysNothing(harness, jeska, Missing, 2);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, jeska, Missing, 2,
                MarshesMissingInActionV1.Experience, MarshesMissingInActionV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Missing), "MissionRewarded once");
            Assert.AreEqual(MarshesMissingInActionV1.CryoGrenades, WildernessRanjaGorgeTests.Held(harness, MarshesMissingInActionV1.CryoGrenade),
                "the selected grenades");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, jeska, Missing), "done once");
        }

        /// <summary>An abandon takes the body away; taken again, a new one talks.</summary>
        [TestMethod]
        public void MissingInActionAbandonedTakesTheBody()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            var jeska = Npc(marshes, Jeska);
            Accept(harness, jeska, Missing);
            Assert.IsNotNull(OpenTalk(harness, Body(harness, marshes)));

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Missing));
            harness.Drain();
            Assert.IsFalse(marshes.DynamicObjects.Any(obj => obj.SceneMissionId == Missing && obj.IsInWorld), "the body goes with it");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, jeska, Missing), "offered again after an abandon");
            Accept(harness, jeska, Missing);
            Assert.IsNotNull(OpenTalk(harness, Body(harness, marshes)), "a new body talks");
        }

        [TestMethod]
        public void LightTheFiresLightsTheFiveSignalFires()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            var dudley = Npc(marshes, Dudley);
            var jeska = Npc(marshes, Jeska);

            var offer = WildernessRanjaGorgeTests.Offer(harness, dudley, Fires);
            CollectionAssert.AreEqual(new[]
                {
                    (MarshesLightTheFiresV2.EmpBomb, MarshesLightTheFiresV2.EmpBombs),
                    (MarshesLightTheFiresV2.PowerCharger, MarshesLightTheFiresV2.PowerChargers),
                    (MarshesLightTheFiresV2.ConcussionGrenade, MarshesLightTheFiresV2.ConcussionGrenades),
                    (MarshesLightTheFiresV2.FragGrenade, MarshesLightTheFiresV2.FragGrenades)
                },
                offer.SelectableReward.Select(item => (item.ItemTemplateId, item.Quantity)).ToArray());
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, jeska, Fires), "not Jeska's to give");
            Accept(harness, dudley, Fires);
            Assert.IsEmpty(Topics(harness, dudley, Fires), "Dudley hears no objective");

            var fires = FirePits(harness, marshes);
            Near(harness, fires[4]);
            Use(harness, marshes, fires[4], 3, recovers: false);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Fires, 5), "argument 3 is not the client's");

            // Counterclockwise: the camp's own fire first.
            Take(harness, marshes, fires[4], MarshesLightTheFiresV2.UseArgument);
            var lit = harness.Drain();
            Assert.AreEqual(1, lit.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Fires && packet.ObjectiveId == 5),
                "ObjectiveCompleted once");
            Assert.AreEqual((UseObjectState)MarshesLightTheFiresV2.LitState, fires[4].StateId, "lit");
            // To-fix 23: the client shows 6137's fire in state 55 (usabledata specialFX and animation (6137, 55, 97)); the pit
            // starts at 56 and the light is a Use to 55.
            Assert.AreEqual((UseObjectState.TsState1, UseObjectState.TsState0),
                ((UseObjectState)MarshesLightTheFiresV2.ColdState, (UseObjectState)MarshesLightTheFiresV2.LitState), "cold 56, lit 55");
            Assert.IsTrue(lit.OfType<Rasa.Packets.MapChannel.Server.UsePacket>().Any(packet => packet.CurState == UseObjectState.TsState0),
                "the client is told the fire's state");
            Assert.IsFalse(fires[4].IsEnabled, "done");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Fires, 1), "the others wait");
            WildernessRanjaGorgeTests.Reload(harness);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Fires, 5), "kept at a reload");
            harness.Drain();
            fires = FirePits(harness, marshes, lit: 1);

            foreach (var index in new[] { 3, 2, 1 })
            {
                Take(harness, marshes, fires[index], MarshesLightTheFiresV2.UseArgument);
                harness.Drain();
                Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Fires, (uint)index + 1));
            }
            Assert.IsFalse(harness.Client.Player.Missions[Fires].Completeable, "one fire left");
            Take(harness, marshes, fires[0], MarshesLightTheFiresV2.UseArgument);
            var last = harness.Drain();
            Assert.AreEqual(1, last.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Fires && packet.IsCompleteable),
                "MissionCompleteable once");

            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, jeska.EntityId, Fires, 1), "not Jeska's to take");
            FullBagWaits(harness, dudley, Fires, 1, ConsumableFiller);
            FailedTurnInPaysNothing(harness, dudley, Fires, 1);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, dudley, Fires, 1,
                MarshesLightTheFiresV2.Experience, MarshesLightTheFiresV2.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Fires), "MissionRewarded once");
            Assert.AreEqual(MarshesLightTheFiresV2.PowerChargers, WildernessRanjaGorgeTests.Held(harness, MarshesLightTheFiresV2.PowerCharger),
                "the selected chargers");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, dudley, Fires), "done once");
        }

        /// <summary>An abandon takes the fires away; taken again, five cold ones stand and nothing counts.</summary>
        [TestMethod]
        public void LightTheFiresAbandonedStartsOver()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            var dudley = Npc(marshes, Dudley);
            Accept(harness, dudley, Fires);
            Take(harness, marshes, FirePits(harness, marshes)[4], MarshesLightTheFiresV2.UseArgument);
            harness.Drain();
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Fires, 5));

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Fires));
            harness.Drain();
            Assert.IsFalse(marshes.DynamicObjects.Any(obj => obj.SceneMissionId == Fires && obj.IsInWorld), "the fires go with it");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, dudley, Fires), "offered again after an abandon");
            Accept(harness, dudley, Fires);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Fires, 5), "nothing counts");
            Assert.IsTrue(FirePits(harness, marshes).All(fire => fire.IsEnabled), "five cold fires");
        }

        [TestMethod]
        public void NowCanYouHearMePlacesTheThreeSpikes()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            var dudley = Npc(marshes, Dudley);
            var jeska = Npc(marshes, Jeska);

            var offer = WildernessRanjaGorgeTests.Offer(harness, dudley, Spikes);
            CollectionAssert.AreEqual(new[]
                {
                    MarshesNowCanYouHearMeV1.HealingDisc, MarshesNowCanYouHearMeV1.CryogenicStaff,
                    MarshesNowCanYouHearMeV1.SonicPistol, MarshesNowCanYouHearMeV1.CryogenicBlade
                },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, jeska, Spikes), "not Jeska's to give");

            // No room for the spikes: no mission. One slot freed (three in a stack of five), they come with it.
            WildernessAliaBranchesTests.FillCategory(harness, MissionItemFiller, 50);
            harness.MoveTo(dudley.Position);
            harness.Drain();
            Assert.IsFalse(harness.Manager.AcceptOfferedMission(harness.Client, dudley.EntityId, Spikes), "no room for the spikes");
            Assert.IsFalse(harness.Client.Player.Missions.ContainsKey(Spikes));
            Assert.AreEqual(0U, SpikesHeld(harness));
            WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
            Accept(harness, dudley, Spikes);
            Assert.AreEqual(MarshesNowCanYouHearMeV1.Spikes, SpikesHeld(harness), "three spikes");
            Assert.IsEmpty(Topics(harness, dudley, Spikes), "Dudley hears no objective");

            var spots = SpikeSpots(harness, marshes);
            Near(harness, spots[0]);
            Use(harness, marshes, spots[0], 1, recovers: false);
            Assert.AreEqual(3U, SpikesHeld(harness), "argument 1 is not the client's");
            Take(harness, marshes, spots[0], MarshesNowCanYouHearMeV1.UseArgument);
            var placed = harness.Drain();
            Assert.AreEqual(2U, SpikesHeld(harness), "a spike placed");
            Assert.AreEqual(1, placed.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Spikes && packet.ObjectiveId == 1),
                "ObjectiveCompleted once");
            Assert.AreEqual((UseObjectState)MarshesNowCanYouHearMeV1.PlacedState, spots[0].StateId);
            Assert.IsFalse(spots[0].IsEnabled, "done");
            TryUse(harness, marshes, spots[0], MarshesNowCanYouHearMeV1.UseArgument);
            Assert.AreEqual(2U, SpikesHeld(harness), "a used spot takes no second");
            WildernessRanjaGorgeTests.Reload(harness);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Spikes, 1), "kept at a reload");
            Assert.AreEqual(2U, SpikesHeld(harness));
            harness.Drain();
            spots = SpikeSpots(harness, marshes, placed: 1);

            Take(harness, marshes, spots[2], MarshesNowCanYouHearMeV1.UseArgument);
            harness.Drain();
            Take(harness, marshes, spots[1], MarshesNowCanYouHearMeV1.UseArgument);
            var last = harness.Drain();
            Assert.AreEqual(0U, SpikesHeld(harness));
            Assert.AreEqual(1, last.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Spikes && packet.IsCompleteable),
                "MissionCompleteable once");

            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, jeska.EntityId, Spikes, 3), "not Jeska's to take");
            FullBagWaits(harness, dudley, Spikes, 3, MarshesNowCanYouHearMeV1.HealingDisc);
            FailedTurnInPaysNothing(harness, dudley, Spikes, 3);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, dudley, Spikes, 3,
                MarshesNowCanYouHearMeV1.Experience, MarshesNowCanYouHearMeV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Spikes), "MissionRewarded once");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, MarshesNowCanYouHearMeV1.CryogenicBlade), "the selected blade");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, dudley, Spikes), "done once");
        }

        /// <summary>An abandon takes the spikes left and the spots away; taken again, three new spikes and spots.</summary>
        [TestMethod]
        public void NowCanYouHearMeAbandonedTakesTheSpikesBack()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            var dudley = Npc(marshes, Dudley);
            Accept(harness, dudley, Spikes);
            Take(harness, marshes, SpikeSpots(harness, marshes)[0], MarshesNowCanYouHearMeV1.UseArgument);
            harness.Drain();
            Assert.AreEqual(2U, SpikesHeld(harness));

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Spikes));
            harness.Drain();
            Assert.AreEqual(0U, SpikesHeld(harness), "the spikes go with the mission");
            Assert.IsFalse(marshes.DynamicObjects.Any(obj => obj.SceneMissionId == Spikes && obj.IsInWorld), "the spots go with it");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, dudley, Spikes), "offered again after an abandon");
            Accept(harness, dudley, Spikes);
            Assert.AreEqual(MarshesNowCanYouHearMeV1.Spikes, SpikesHeld(harness), "three new spikes");
            Assert.IsTrue(SpikeSpots(harness, marshes).All(spot => spot.IsEnabled), "three new spots");
        }

        [TestMethod]
        public void StopAndSmellTheMawFlowersGathersFive()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            var duvall = Npc(marshes, Duvall);
            var dudley = Npc(marshes, Dudley);
            Assert.IsFalse(marshes.DynamicObjects.Any(obj => obj.SceneMissionId == Flowers), "no flower before the mission");

            var offer = WildernessRanjaGorgeTests.Offer(harness, duvall, Flowers);
            CollectionAssert.AreEqual(new[]
                {
                    MarshesMawFlowersV1.AreaRepairTool, MarshesMawFlowersV1.SonicTorqueshellRifle,
                    MarshesMawFlowersV1.Rifle, MarshesMawFlowersV1.SonicDisperser
                },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, dudley, Flowers), "not Dudley's to give");
            Accept(harness, duvall, Flowers);
            Assert.IsEmpty(Topics(harness, duvall, Flowers), "Duvall hears no objective: the flowers count as they are taken");

            var flowers = MawFlowers(harness, marshes);
            Near(harness, flowers[0]);
            Use(harness, marshes, flowers[0], 1, recovers: false);
            Assert.AreEqual(0U, FlowersHeld(harness), "argument 1 is not the client's");
            WildernessAliaBranchesTests.FillCategory(harness, MissionItemFiller, 50);
            Take(harness, marshes, flowers[0], MarshesMawFlowersV1.UseArgument);
            Assert.AreEqual(0U, FlowersHeld(harness), "no room");
            Assert.IsTrue(flowers[0].IsEnabled, "the flower waits");
            WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
            harness.Drain();

            Take(harness, marshes, flowers[0], MarshesMawFlowersV1.UseArgument);
            Take(harness, marshes, flowers[1], MarshesMawFlowersV1.UseArgument);
            harness.Drain();
            Assert.AreEqual(2U, FlowersHeld(harness));
            Assert.AreEqual((UseObjectState)MarshesMawFlowersV1.TakenState, flowers[0].StateId, "picked");
            TryUse(harness, marshes, flowers[1], MarshesMawFlowersV1.UseArgument);
            Assert.AreEqual(2U, FlowersHeld(harness), "a picked flower gives no second");
            WildernessRanjaGorgeTests.Reload(harness);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Flowers, MarshesMawFlowersV1.Gather), "kept at a reload");
            Assert.AreEqual(2U, FlowersHeld(harness));
            harness.Drain();
            flowers = MawFlowers(harness, marshes, picked: 2);

            Take(harness, marshes, flowers[2], MarshesMawFlowersV1.UseArgument);
            Take(harness, marshes, flowers[3], MarshesMawFlowersV1.UseArgument);
            harness.Drain();
            Assert.IsFalse(harness.Client.Player.Missions[Flowers].Completeable, "four of five");
            Take(harness, marshes, flowers[4], MarshesMawFlowersV1.UseArgument);
            var last = harness.Drain();
            Assert.AreEqual(5U, FlowersHeld(harness));
            Assert.AreEqual(1, last.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Flowers &&
                packet.ObjectiveId == MarshesMawFlowersV1.Gather), "ObjectiveCompleted once");
            Assert.AreEqual(1, last.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Flowers && packet.IsCompleteable),
                "MissionCompleteable once");

            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, dudley.EntityId, Flowers, 0), "not Dudley's to take");
            FullBagWaits(harness, duvall, Flowers, 0, MarshesMawFlowersV1.Rifle);
            FailedTurnInPaysNothing(harness, duvall, Flowers, 0);
            Assert.AreEqual(5U, FlowersHeld(harness), "kept at the failed turn-in");
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, duvall, Flowers, 0,
                MarshesMawFlowersV1.Experience, MarshesMawFlowersV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Flowers), "MissionRewarded once");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, MarshesMawFlowersV1.AreaRepairTool), "the selected repair tool");
            Assert.AreEqual(0U, FlowersHeld(harness), "Duvall takes the flowers");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, duvall, Flowers), "done once");
        }

        /// <summary>An abandon takes the flowers and the givers away; taken again, five new flowers lie ready.</summary>
        [TestMethod]
        public void StopAndSmellTheMawFlowersAbandonedTakesThemBack()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            var duvall = Npc(marshes, Duvall);
            Accept(harness, duvall, Flowers);
            Take(harness, marshes, MawFlowers(harness, marshes)[0], MarshesMawFlowersV1.UseArgument);
            harness.Drain();
            Assert.AreEqual(1U, FlowersHeld(harness));

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Flowers));
            harness.Drain();
            Assert.AreEqual(0U, FlowersHeld(harness), "the flower goes with the mission");
            Assert.IsFalse(marshes.DynamicObjects.Any(obj => obj.SceneMissionId == Flowers && obj.IsInWorld), "the givers go with it");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, duvall, Flowers), "offered again after an abandon");
            Accept(harness, duvall, Flowers);
            Assert.IsTrue(MawFlowers(harness, marshes).All(flower => flower.IsEnabled), "five new flowers");
        }

        private static DynamicObject[] SceneObjects(MapChannel marshes, uint mission, string prefix) =>
            marshes.DynamicObjects.Where(obj => obj.SceneMissionId == mission && obj.IsInWorld &&
                obj.SceneActorRole.StartsWith(prefix)).OrderBy(obj => obj.SceneActorRole).ToArray();

        /// <summary>Checks the player's own objects of a class at their places, on the navmesh and walked to; returns them in role order.</summary>
        private static DynamicObject[] Placed(WildernessRuntimeTestHarness harness, MapChannel marshes, uint mission, string prefix,
            IReadOnlyList<Vector3> places, int used, string what)
        {
            var objects = SceneObjects(marshes, mission, prefix);
            Assert.HasCount(places.Count, objects, what);
            for (var index = 0; index < objects.Length; index++)
            {
                Assert.AreEqual(places[index], objects[index].Position, $"{objects[index].SceneActorRole} at its place");
                Assert.AreEqual(harness.Client.Player.Id, objects[index].SceneOwnerCharacterId, "the player's own");
                AssertOnGround(marshes, objects[index].Position, objects[index].SceneActorRole);
                Assert.IsTrue(marshes.NavMesh.FindPath(HighPointRetreatWaypoint, objects[index].Position, out var complete) != null && complete,
                    $"{objects[index].SceneActorRole} is walked to");
            }
            Assert.AreEqual(objects.Length - used, objects.Count(obj => obj.IsEnabled), $"{what}: {used} used");
            return objects;
        }

        private static DynamicObject[] FirePits(WildernessRuntimeTestHarness harness, MapChannel marshes, int lit = 0)
        {
            var fires = Placed(harness, marshes, Fires, "signal-fire-",
                MarshesLightTheFiresV2.Fires.Select(fire => new Vector3(fire.At.X, fire.At.Y, fire.At.Z)).ToArray(), lit, "five fires");
            Assert.IsTrue(fires.All(fire => (uint)fire.EntityClassId == MarshesLightTheFiresV2.FireClass));
            return fires;
        }

        private static DynamicObject[] SpikeSpots(WildernessRuntimeTestHarness harness, MapChannel marshes, int placed = 0)
        {
            var spots = Placed(harness, marshes, Spikes, "spike-spot-",
                MarshesNowCanYouHearMeV1.Spots.Select(spot => new Vector3(spot.At.X, spot.At.Y, spot.At.Z)).ToArray(), placed, "three spots");
            Assert.IsTrue(spots.All(spot => (uint)spot.EntityClassId == MarshesNowCanYouHearMeV1.SpotClass));
            Assert.IsTrue(spots.All(spot => spot.NameOverrideId == MarshesNowCanYouHearMeV1.SpotName), "the client's \"AFS Communication Spike\"");
            var camp = new Vector2(HighPointRetreatWaypoint.X, HighPointRetreatWaypoint.Z);
            Assert.IsGreaterThan(camp.Y, spots[0].Position.Z, "the first north of the camp");
            Assert.IsGreaterThan(camp.X, spots[1].Position.X, "the second east...");
            Assert.IsGreaterThan(camp.Y, spots[1].Position.Z, "...and north of it");
            Assert.IsLessThan(5f, System.Math.Abs(spots[2].Position.Z - camp.Y), "the third east of it");
            return spots;
        }

        private static DynamicObject[] MawFlowers(WildernessRuntimeTestHarness harness, MapChannel marshes, int picked = 0)
        {
            var flowers = Placed(harness, marshes, Flowers, "maw-flower-",
                MarshesMawFlowersV1.FlowersAt.Select(at => new Vector3(at.X, at.Y, at.Z)).ToArray(), picked, "five flowers");
            for (var index = 0; index < flowers.Length; index++)
                Assert.AreEqual(MarshesMawFlowersV1.GiverClasses[index % MarshesMawFlowersV1.GiverClasses.Length], (uint)flowers[index].EntityClassId);
            Assert.IsTrue(flowers.All(flower => flower.Position.Z > HighPointRetreatWaypoint.Z), "north of the camp");
            return flowers;
        }

        /// <summary>Koffman's body: the player's own, of its class, talking with the client's package, east of the camp, walked to.</summary>
        private static DynamicObject Body(WildernessRuntimeTestHarness harness, MapChannel marshes)
        {
            var body = marshes.DynamicObjects.Single(obj => obj.SceneMissionId == Missing &&
                obj.SceneActorRole == MarshesMissingInActionV1.CorpseRole && obj.IsInWorld);
            var at = MarshesMissingInActionV1.CorpseAt;
            Assert.AreEqual(MarshesMissingInActionV1.CorpseClass, (uint)body.EntityClassId);
            Assert.AreEqual(new Vector3(at.X, at.Y, at.Z), body.Position);
            Assert.AreEqual(MarshesMissingInActionV1.CorpsePackage, body.MissionConversation.NpcPackageId);
            Assert.AreEqual(harness.Client.Player.Id, body.SceneOwnerCharacterId, "the player's own");
            AssertOnGround(marshes, body.Position, "the body");
            Assert.IsTrue(marshes.NavMesh.FindPath(HighPointRetreatWaypoint, body.Position, out var complete) != null && complete,
                "the body is walked to");
            Assert.IsGreaterThan(HighPointRetreatWaypoint.X + 50, body.Position.X, "east of the camp");
            return body;
        }

        /// <summary>An object's conversation, as PoolsSnakePitContactsTests.OpenTerminal.</summary>
        private static ConversePacket OpenTalk(WildernessRuntimeTestHarness harness, DynamicObject obj)
        {
            harness.MoveTo(obj.Position + new Vector3(0, 0, 1));
            harness.Drain();
            new NpcManager(harness, harness.Manager).RequestNpcConverse(harness.Client,
                new RequestNPCConversePacket { EntityId = obj.EntityId, ActionId = ActionId.Converse, ActionArgId = 1 });
            return harness.Drain().OfType<ConversePacket>().LastOrDefault();
        }

        private static void Near(WildernessRuntimeTestHarness harness, DynamicObject obj)
        {
            harness.MoveTo(obj.Position + new Vector3(0, 0, 1));
            harness.Drain();
        }

        private static void Take(WildernessRuntimeTestHarness harness, MapChannel marshes, DynamicObject obj, uint argument)
        {
            Near(harness, obj);
            Use(harness, marshes, obj, argument, recovers: true);
        }

        /// <summary>A use the server may refuse at once or after its recovery: either way the caller checks that nothing came of it.</summary>
        private static void TryUse(WildernessRuntimeTestHarness harness, MapChannel marshes, DynamicObject obj, uint argument)
        {
            Near(harness, obj);
            var count = marshes.PerformRecovery.Count;
            harness.Objects.RequestUseObjectPacket(harness.Client, new RequestUseObjectPacket
            {
                EntityId = obj.EntityId, ActionId = ActionId.UseObject, ActionArgId = argument
            });
            if (marshes.PerformRecovery.Count > count)
            {
                var recovery = marshes.PerformRecovery.Last();
                marshes.PerformRecovery.Remove(recovery);
                ActorActionManager.Instance.PerformRecovery(marshes, recovery);
            }
            harness.Drain();
        }

        private static uint SpikesHeld(WildernessRuntimeTestHarness harness) =>
            WildernessRanjaGorgeTests.Held(harness, MarshesNowCanYouHearMeV1.SpikeTemplate);

        private static uint FlowersHeld(WildernessRuntimeTestHarness harness) =>
            WildernessRanjaGorgeTests.Held(harness, MarshesMawFlowersV1.FlowerTemplate);
    }
}
