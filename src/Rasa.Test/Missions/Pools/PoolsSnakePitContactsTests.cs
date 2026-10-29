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
using Rasa.Packets.MapChannel.Server;
using Rasa.Packets.Mission.Server;
using Rasa.Services.Preloader.Missions.Pools;
using Rasa.Structures;
using Rasa.Test.Missions.Plateau;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions.Pools
{
    using static PlateauCampResistanceTests;

    /// <summary>
    /// The missions of Spymaster Timlin, Specialist Allen and Lieutenant Feldman, played through as a player plays them:
    /// Looking For Logos? (1308: Scientist Suresh wants to see the Far Logos, in Turpis Refinery), Mapping The Pools (1025:
    /// a pack of GPS beacons from Timlin's cases, five planted, Timlin told) and Bane Investigation (932: a Bane terminal
    /// at the western fort).
    ///
    /// Why these tests: the three NPCs are new on the server; the Logos is taken in a squad instance and one already owned
    /// counts; the beacons are the player's own scene objects, planted in any order, each counted once on the tracker; the
    /// terminal is talked to, not used. A beacon planted twice, a pack taken twice, beacons kept after an abandon, a wrong
    /// NPC that completes an objective, or progress lost at a reload would each break them.
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class PoolsSnakePitContactsTests
    {
        private const uint LookingForLogos = PoolsLookingForLogosV1.MissionId;
        private const uint Mapping = PoolsMappingThePoolsV1.MissionId;
        private const uint Investigation = PoolsBaneInvestigationV1.MissionId;
        private const uint Timlin = PoolsSnakePitContactsV1.Timlin, Allen = PoolsSnakePitContactsV1.Allen;
        private const uint Feldman = PoolsSnakePitContactsV1.Feldman, Suresh = PoolsLookingForLogosV1.Suresh;
        private const uint MissionItemFiller = 11519;      // as PlateauCampResistanceTests
        private const uint ArmorFiller = PoolsLookingForLogosV1.StealthGloves;

        /// <summary>The Snake Pit's waypoint (World DB teleporter 48), where a player arrives.</summary>
        private static readonly Vector3 SnakePitWaypoint = new(-941.7f, 919.94f, 367.4f);

        [TestMethod]
        public void TimlinAllenAndFeldmanStandAtTheirPlacesAndAreWalkedTo()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pools = PlateauValverdePoolsTests.OntoPools(harness);
            foreach (var (pool, name, package, at) in new[]
            {
                (Timlin, PoolsSnakePitContactsV1.TimlinName, PoolsSnakePitContactsV1.TimlinPackage, PoolsSnakePitContactsV1.TimlinAt),
                (Allen, PoolsSnakePitContactsV1.AllenName, PoolsSnakePitContactsV1.AllenPackage, PoolsSnakePitContactsV1.AllenAt),
                (Feldman, PoolsSnakePitContactsV1.FeldmanName, PoolsSnakePitContactsV1.FeldmanPackage, PoolsSnakePitContactsV1.FeldmanAt)
            })
            {
                var npc = Npc(pools, pool);
                Assert.AreEqual(name, npc.NameId, $"{pool} name");
                Assert.AreEqual(package, npc.Npc.NpcPackageId, $"{pool} package");
                Assert.AreNotEqual(TargetCategory.Hostile, npc.TargetCategory);
                Assert.AreEqual(new Vector3((float)at.X, (float)at.Y, (float)at.Z), npc.Position);
                AssertOnTheGround(pools, npc.Position, $"{pool}");
                Assert.IsTrue(pools.NavMesh.FindPath(SnakePitWaypoint, npc.Position, out var complete) != null && complete, $"{pool} is walked to");
            }
        }

        [TestMethod]
        public void LookingForLogosTakesTheFarLogosFromTurpisRefineryToSuresh()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pools = PlateauValverdePoolsTests.OntoPools(harness);
            var timlin = Npc(pools, Timlin);
            var suresh = Npc(pools, Suresh);

            var offer = WildernessRanjaGorgeTests.Offer(harness, timlin, LookingForLogos);
            WildernessRanjaGorgeTests.AssertSelectable(offer, LookingForLogos,
                (PoolsLookingForLogosV1.BioGloves, 23375), (PoolsLookingForLogosV1.GravitonHelmet, 15089),
                (PoolsLookingForLogosV1.MechGloves, 17960), (PoolsLookingForLogosV1.StealthGloves, 19853));
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, suresh, LookingForLogos), "Timlin gives it, not Suresh");
            Accept(harness, timlin, LookingForLogos);
            Assert.IsFalse(Conversation(harness, timlin).ContainsKey(ConversationType.ObjectiveComplete), "Timlin has no topic");
            Refused(harness, timlin, LookingForLogos, PoolsLookingForLogosV1.Speak);

            // 1: Suresh; then 3 opens, 2 waits.
            Talk(harness, suresh, LookingForLogos, PoolsLookingForLogosV1.Speak);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, LookingForLogos, PoolsLookingForLogosV1.Find));
            Assert.AreEqual(MissionObjectiveState.Inactive, Objective(harness, LookingForLogos, PoolsLookingForLogosV1.Show));
            Assert.IsFalse(Conversation(harness, suresh).ContainsKey(ConversationType.ObjectiveComplete), "nothing to show yet");
            Refused(harness, suresh, LookingForLogos, PoolsLookingForLogosV1.Show);
            WildernessRanjaGorgeTests.Reload(harness);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, LookingForLogos, PoolsLookingForLogosV1.Find), "kept at a reload");
            harness.Drain();

            // 3: the Far Logos, in the squad's own copy of Turpis Refinery.
            PlateauInstanceMaps.LoadMap(harness, PoolsLookingForLogosV1.TurpisRefinery);
            var refinery = PlateauInstanceMaps.ThroughTheDoor(harness, PoolsLookingForLogosV1.TurpisRefinery);
            Assert.IsTrue(refinery.IsSquadInstance);
            harness.Drain();
            PlateauTrinityBridgeLogosTests.UseShrine(harness, refinery,
                refinery.DynamicObjects.OfType<Logos>().Single(entry => entry.Id == PoolsLookingForLogosV1.FarLogos));
            Assert.IsTrue(harness.Client.Player.Logos.Contains(PoolsLookingForLogosV1.FarLogos));
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, LookingForLogos, PoolsLookingForLogosV1.Find));
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, LookingForLogos, PoolsLookingForLogosV1.Show));
            Assert.IsFalse(harness.Client.Player.Missions[LookingForLogos].Completeable, "not before Suresh sees it");

            // 2: back on the Pools, Suresh sees it.
            pools = PlateauValverdePoolsTests.OntoPools(harness);
            suresh = Npc(pools, Suresh);
            AssertHandInTopic(Conversation(harness, suresh), LookingForLogos, PoolsLookingForLogosV1.Show);
            harness.MoveTo(suresh.Position);
            harness.Drain();
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, suresh.EntityId, LookingForLogos, PoolsLookingForLogosV1.Show, 1));
            var packets = harness.Drain();
            Assert.AreEqual(1, packets.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == LookingForLogos &&
                packet.ObjectiveId == PoolsLookingForLogosV1.Show), "ObjectiveCompleted once");
            Assert.AreEqual(1, packets.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == LookingForLogos), "MissionCompleteable once");
            Assert.IsTrue(harness.Client.Player.Missions[LookingForLogos].Completeable);

            FailedTurnInPaysNothing(harness, suresh, LookingForLogos, 1);
            FullBagWaits(harness, suresh, LookingForLogos, 1, ArmorFiller);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, suresh, LookingForLogos, 1,
                PoolsLookingForLogosV1.Experience, PoolsLookingForLogosV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == LookingForLogos));
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PoolsLookingForLogosV1.GravitonHelmet), "the selected helmet");
        }

        /// <summary>A Far Logos the player already has counts once Suresh has heard about it; an abandon offers it again.</summary>
        [TestMethod]
        public void LookingForLogosCountsAFarLogosAlreadyLearned()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pools = PlateauValverdePoolsTests.OntoPools(harness);
            PlateauInstanceMaps.LoadMap(harness, PoolsLookingForLogosV1.TurpisRefinery);
            var refinery = PlateauInstanceMaps.ThroughTheDoor(harness, PoolsLookingForLogosV1.TurpisRefinery);
            PlateauTrinityBridgeLogosTests.UseShrine(harness, refinery,
                refinery.DynamicObjects.OfType<Logos>().Single(entry => entry.Id == PoolsLookingForLogosV1.FarLogos));
            pools = PlateauValverdePoolsTests.OntoPools(harness);
            var timlin = Npc(pools, Timlin);
            var suresh = Npc(pools, Suresh);

            Accept(harness, timlin, LookingForLogos);
            Assert.AreEqual(MissionObjectiveState.Inactive, Objective(harness, LookingForLogos, PoolsLookingForLogosV1.Find), "not before Suresh");
            Talk(harness, suresh, LookingForLogos, PoolsLookingForLogosV1.Speak);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, LookingForLogos, PoolsLookingForLogosV1.Find), "the Logos counts");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, LookingForLogos, PoolsLookingForLogosV1.Show));

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, LookingForLogos));
            harness.Drain();
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, timlin, LookingForLogos), "offered again after an abandon");
        }

        [TestMethod]
        public void MappingThePoolsPlantsFiveBeaconsInAnyOrderAndTimlinHearsOfIt()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pools = PlateauValverdePoolsTests.OntoPools(harness);
            var allen = Npc(pools, Allen);
            var timlin = Npc(pools, Timlin);
            Assert.IsFalse(pools.DynamicObjects.Any(obj => obj.SceneMissionId == Mapping), "no pack or beacon spot before the mission");

            var offer = WildernessRanjaGorgeTests.Offer(harness, allen, Mapping);
            Assert.AreEqual(0, offer.SelectableReward.Count);
            CollectionAssert.AreEquivalent(AllenRewards, offer.FixedReward.FixedItems.Select(item => (item.ItemTemplateId, item.Quantity)).ToArray());
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, timlin, Mapping), "Allen gives it, not Timlin");
            Accept(harness, allen, Mapping);

            var pack = Pack(harness, pools);
            var spots = Spots(harness, pools);
            Use(harness, pools, pack, recovers: false);
            Assert.AreEqual(0U, Beacons(harness), "the pack waits for Timlin");
            harness.MoveTo(spots[0].Position + new Vector3(0, 0, 1));
            Use(harness, pools, spots[0], recovers: false);
            Assert.IsTrue(spots[0].IsEnabled, "no planting before the pack");
            Refused(harness, allen, Mapping, PoolsMappingThePoolsV1.Speak);
            Assert.IsFalse(Conversation(harness, allen).ContainsKey(ConversationType.ObjectiveComplete), "Allen has no topic");

            // 1: Timlin; 2: the pack, once.
            Talk(harness, timlin, Mapping, PoolsMappingThePoolsV1.Speak);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Mapping, PoolsMappingThePoolsV1.PickUp));
            Use(harness, pools, pack, recovers: true);
            Assert.AreEqual(PoolsMappingThePoolsV1.Beacons, Beacons(harness), "five beacons");
            Assert.AreEqual((UseObjectState)PoolsMappingThePoolsV1.PackTakenState, pack.StateId);
            Assert.IsFalse(pack.IsEnabled, "the pack is taken once");
            Use(harness, pools, pack, recovers: false);
            Assert.AreEqual(PoolsMappingThePoolsV1.Beacons, Beacons(harness));
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Mapping, PoolsMappingThePoolsV1.PickUp));
            for (var number = 1; number <= 5; number++)
                Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Mapping, PoolsMappingThePoolsV1.Plant(number)), $"#{number} open");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Mapping, PoolsMappingThePoolsV1.Tracker));
            harness.Drain();

            // 3-7 in any order; 8 counts each once.
            var completed = new List<uint>();
            var planted = 0U;
            foreach (var number in new[] { 3, 1, 5, 2, 4 })
            {
                var spot = spots[number - 1];
                harness.MoveTo(spot.Position + new Vector3(0, 0, 1));
                Use(harness, pools, spot, recovers: true);
                planted++;
                Assert.AreEqual(PoolsMappingThePoolsV1.Beacons - planted, Beacons(harness), $"#{number} takes one");
                Assert.AreEqual((UseObjectState)PoolsMappingThePoolsV1.SpotPlantedState, spot.StateId);
                Assert.IsFalse(spot.IsEnabled, $"#{number} planted once");
                Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Mapping, PoolsMappingThePoolsV1.Plant(number)));
                Assert.AreEqual(planted, Tracked(harness), "Beacons Placed");
                completed.AddRange(harness.Drain().OfType<ObjectiveCompletedPacket>()
                    .Where(packet => packet.MissionId == Mapping).Select(packet => packet.ObjectiveId));
                if (number == 3)
                {
                    Use(harness, pools, spot, recovers: false);
                    Assert.AreEqual(PoolsMappingThePoolsV1.Beacons - 1, Beacons(harness), "one beacon a spot");
                    Assert.AreEqual(1U, Tracked(harness));
                }
                if (planted == 2)
                {
                    WildernessRanjaGorgeTests.Reload(harness);
                    Assert.AreEqual(3U, Beacons(harness), "kept at a reload");
                    Assert.AreEqual(2U, Tracked(harness), "counted at a reload");
                }
                if (planted < 5)
                    Assert.AreEqual(MissionObjectiveState.Inactive, Objective(harness, Mapping, PoolsMappingThePoolsV1.Report));
            }
            CollectionAssert.AreEquivalent(new uint[] { 5, 3, 7, 4, 6, PoolsMappingThePoolsV1.Tracker }, completed.ToArray(),
                "one ObjectiveCompleted each");
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Mapping, PoolsMappingThePoolsV1.Tracker));
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Mapping, PoolsMappingThePoolsV1.Report));
            Assert.IsFalse(harness.Client.Player.Missions[Mapping].Completeable, "Timlin first");

            // 9: Timlin's two choices; Allen does not hear it.
            Assert.IsFalse(Conversation(harness, allen).ContainsKey(ConversationType.ObjectiveChoice), "Allen has no choice");
            var choice = Conversation(harness, timlin);
            Assert.IsTrue(choice.TryGetValue(ConversationType.ObjectiveChoice, out var choices));
            Assert.AreEqual(((int)Mapping, (int)PoolsMappingThePoolsV1.Report, 1),
                ((List<ChoiceObjectives>)choices).Select(entry => (entry.MissionId, entry.ObjectiveId, entry.PlayerFlagId)).Single());
            Choose(harness, timlin, PoolsMappingThePoolsV1.TakeCredit);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Mapping, PoolsMappingThePoolsV1.Report));
            Assert.IsTrue(harness.Client.Player.Missions[Mapping].Completeable);

            FailedTurnInPaysNothing(harness, allen, Mapping, null);
            var held = AllenRewards.Select(reward => WildernessRanjaGorgeTests.Held(harness, reward.Template)).ToArray();
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, allen, Mapping, null,
                PoolsMappingThePoolsV1.Experience, PoolsMappingThePoolsV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Mapping));
            for (var i = 0; i < AllenRewards.Length; i++)
                Assert.AreEqual(held[i] + AllenRewards[i].Quantity, WildernessRanjaGorgeTests.Held(harness, AllenRewards[i].Template));
        }

        /// <summary>"Praise Specialist Allen" ends it as "Take Credit" does, with the same reward.</summary>
        [TestMethod]
        public void MappingThePoolsPraisingAllenPaysTheSame()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pools = PlateauValverdePoolsTests.OntoPools(harness);
            var allen = Npc(pools, Allen);
            var timlin = Npc(pools, Timlin);
            Accept(harness, allen, Mapping);
            Talk(harness, timlin, Mapping, PoolsMappingThePoolsV1.Speak);
            Use(harness, pools, Pack(harness, pools), recovers: true);
            foreach (var spot in Spots(harness, pools))
            {
                harness.MoveTo(spot.Position + new Vector3(0, 0, 1));
                Use(harness, pools, spot, recovers: true);
            }
            Assert.AreEqual(0U, Beacons(harness));

            Choose(harness, timlin, PoolsMappingThePoolsV1.Praise);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Mapping, PoolsMappingThePoolsV1.Report));
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, allen, Mapping, null,
                PoolsMappingThePoolsV1.Experience, PoolsMappingThePoolsV1.Credits);
        }

        /// <summary>A full bag keeps the pack closed; an abandon takes the beacons and the spots; taken again, all stand anew.</summary>
        [TestMethod]
        public void MappingThePoolsPackWaitsForRoomAndAnAbandonTakesTheBeacons()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pools = PlateauValverdePoolsTests.OntoPools(harness);
            var allen = Npc(pools, Allen);
            var timlin = Npc(pools, Timlin);
            Accept(harness, allen, Mapping);
            Talk(harness, timlin, Mapping, PoolsMappingThePoolsV1.Speak);

            WildernessAliaBranchesTests.FillCategory(harness, MissionItemFiller, 50);
            var pack = Pack(harness, pools);
            Use(harness, pools, pack, recovers: true);
            Assert.AreEqual(0U, Beacons(harness), "no room");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Mapping, PoolsMappingThePoolsV1.PickUp));
            Assert.IsTrue(pack.IsEnabled, "the pack waits");
            WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
            Use(harness, pools, pack, recovers: true);
            Assert.AreEqual(PoolsMappingThePoolsV1.Beacons, Beacons(harness));

            var spot = Spots(harness, pools)[0];
            harness.MoveTo(spot.Position + new Vector3(0, 0, 1));
            Use(harness, pools, spot, recovers: true);
            Assert.AreEqual(4U, Beacons(harness));

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Mapping));
            harness.Drain();
            Assert.AreEqual(0U, Beacons(harness), "the beacons go with the mission");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, allen, Mapping), "offered again after an abandon");
            Accept(harness, allen, Mapping);
            Assert.IsTrue(Pack(harness, pools).IsEnabled, "a new pack");
            Assert.IsTrue(Spots(harness, pools).All(candidate => candidate.IsEnabled), "five new spots");
        }

        [TestMethod]
        public void BaneInvestigationIsOneTalkAtTheTerminalThenFeldman()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pools = PlateauValverdePoolsTests.OntoPools(harness);
            var feldman = Npc(pools, Feldman);
            Assert.IsFalse(pools.DynamicObjects.Any(obj => obj.SceneMissionId == Investigation), "no terminal before the mission");

            var offer = WildernessRanjaGorgeTests.Offer(harness, feldman, Investigation);
            CollectionAssert.AreEqual(new[]
                {
                    PoolsBaneInvestigationV1.CritHitBonus, PoolsBaneInvestigationV1.ArmorAbsorption,
                    PoolsBaneInvestigationV1.ThreatReduction, PoolsBaneInvestigationV1.ArmorPiercing
                },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Accept(harness, feldman, Investigation);
            Assert.IsFalse(Conversation(harness, feldman).ContainsKey(ConversationType.ObjectiveComplete), "Feldman has no topic");
            Refused(harness, feldman, Investigation, PoolsBaneInvestigationV1.Search);

            var terminal = pools.DynamicObjects.Single(obj => obj.SceneMissionId == Investigation && obj.SceneActorRole == "terminal");
            Assert.AreEqual(PoolsBaneInvestigationV1.TerminalClass, (uint)terminal.EntityClassId);
            var at = PoolsBaneInvestigationV1.TerminalAt;
            Assert.AreEqual(new Vector3(at.X, at.Y, at.Z), terminal.Position);
            Assert.AreEqual(PoolsBaneInvestigationV1.TerminalPackage, terminal.MissionConversation.NpcPackageId);
            Assert.AreEqual(harness.Client.Player.Id, terminal.SceneOwnerCharacterId, "the player's own");
            AssertOnTheGround(pools, terminal.Position, "terminal");
            Assert.IsTrue(pools.NavMesh.FindPath(SnakePitWaypoint, terminal.Position, out var reached) != null && reached, "walked to");

            new NpcManager(harness, harness.Manager).CompleteNPCObjective(harness.Client, new CompleteNPCObjectivePacket
            {
                EntityId = terminal.EntityId, MissionId = Investigation, ObjectiveId = PoolsBaneInvestigationV1.Access, PlayerFlagId = 1
            });
            harness.Drain();
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Investigation, PoolsBaneInvestigationV1.Access),
                "1 is not talked to on its own");
            var talk = OpenTerminal(harness, terminal);
            Assert.IsNotNull(talk, "the terminal talks");
            Assert.IsTrue(talk.ConvoDataDict.TryGetValue(ConversationType.ObjectiveComplete, out var topics));
            Assert.AreEqual(((int)Investigation, (int)PoolsBaneInvestigationV1.Search, 1),
                ((List<CompleteableObjectives>)topics).Select(topic => (topic.MissionId, topic.ObjectiveId, topic.PlayerFlagId)).Single());
            new NpcManager(harness, harness.Manager).CompleteNPCObjective(harness.Client, new CompleteNPCObjectivePacket
            {
                EntityId = terminal.EntityId, MissionId = Investigation, ObjectiveId = PoolsBaneInvestigationV1.Search, PlayerFlagId = 1
            });
            var completed = harness.Drain().OfType<ObjectiveCompletedPacket>().Where(packet => packet.MissionId == Investigation)
                .Select(packet => packet.ObjectiveId).ToArray();
            CollectionAssert.AreEquivalent(new[] { PoolsBaneInvestigationV1.Access, PoolsBaneInvestigationV1.Search }, completed,
                "one talk does 1 and 2");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Investigation, PoolsBaneInvestigationV1.Return));
            Assert.IsNull(OpenTerminal(harness, terminal), "read once");
            Assert.IsTrue(harness.Client.Player.Missions[Investigation].Completeable);

            WildernessRanjaGorgeTests.Reload(harness);
            Assert.IsTrue(harness.Client.Player.Missions[Investigation].Completeable, "kept at a reload");
            harness.Drain();

            FailedTurnInPaysNothing(harness, feldman, Investigation, 2);
            FullBagWaits(harness, feldman, Investigation, 2, PoolsBaneInvestigationV1.CritHitBonus);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, feldman, Investigation, 2,
                PoolsBaneInvestigationV1.Experience, PoolsBaneInvestigationV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Investigation));
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PoolsBaneInvestigationV1.ThreatReduction), "the selected module");
        }

        /// <summary>An abandon takes the terminal; taken again, a new one stands.</summary>
        [TestMethod]
        public void BaneInvestigationAbandonedTakesTheTerminal()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pools = PlateauValverdePoolsTests.OntoPools(harness);
            var feldman = Npc(pools, Feldman);
            Accept(harness, feldman, Investigation);
            Assert.IsTrue(pools.DynamicObjects.Any(obj => obj.SceneMissionId == Investigation && obj.SceneActorRole == "terminal"));

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Investigation));
            harness.Drain();
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, feldman, Investigation), "offered again after an abandon");
            Accept(harness, feldman, Investigation);
            Assert.IsNotNull(OpenTerminal(harness, pools.DynamicObjects.Single(obj =>
                obj.SceneMissionId == Investigation && obj.SceneActorRole == "terminal")), "a new terminal talks");
        }

        private static readonly (uint Template, uint Quantity)[] AllenRewards =
        {
            (PoolsMappingThePoolsV1.EmpBomb, PoolsMappingThePoolsV1.EmpBombs),
            (PoolsMappingThePoolsV1.AdrenalineBooster, PoolsMappingThePoolsV1.AdrenalineBoosters),
            (PoolsMappingThePoolsV1.IncendiaryGrenade, PoolsMappingThePoolsV1.IncendiaryGrenades),
            (PoolsMappingThePoolsV1.EmpGrenade, PoolsMappingThePoolsV1.EmpGrenades)
        };

        /// <summary>The pack: the player's own, at Timlin's cases, on the navmesh.</summary>
        private static DynamicObject Pack(WildernessRuntimeTestHarness harness, MapChannel pools)
        {
            var pack = pools.DynamicObjects.Single(obj => obj.SceneMissionId == Mapping && obj.SceneActorRole == "pack" &&
                obj.SceneOwnerCharacterId == harness.Client.Player.Id && obj.IsEnabled);
            Assert.AreEqual(PoolsMappingThePoolsV1.PackClass, (uint)pack.EntityClassId);
            var at = PoolsMappingThePoolsV1.PackAt;
            Assert.AreEqual(new Vector3(at.X, at.Y, at.Z), pack.Position);
            AssertOnTheGround(pools, pack.Position, "pack");
            harness.MoveTo(pack.Position + new Vector3(0, 0, 1));
            return pack;
        }

        /// <summary>Beacon spots #1-#5: the player's own, at TaRapedia's places, on the navmesh, walked to.</summary>
        private static DynamicObject[] Spots(WildernessRuntimeTestHarness harness, MapChannel pools)
        {
            var spots = Enumerable.Range(1, PoolsMappingThePoolsV1.Spots.Length).Select(number => pools.DynamicObjects.Single(obj =>
                obj.SceneMissionId == Mapping && obj.SceneActorRole == $"beacon-{number}" && obj.IsEnabled)).ToArray();
            for (var index = 0; index < spots.Length; index++)
            {
                var at = PoolsMappingThePoolsV1.Spots[index];
                Assert.AreEqual(PoolsMappingThePoolsV1.SpotClass, (uint)spots[index].EntityClassId);
                Assert.AreEqual(new Vector3(at.X, at.Y, at.Z), spots[index].Position);
                Assert.AreEqual(harness.Client.Player.Id, spots[index].SceneOwnerCharacterId, "the player's own");
                AssertOnTheGround(pools, spots[index].Position, $"beacon #{index + 1}");
                Assert.IsTrue(pools.NavMesh.FindPath(SnakePitWaypoint, spots[index].Position, out var complete) != null && complete,
                    $"beacon #{index + 1} is walked to");
            }
            return spots;
        }

        private static uint Beacons(WildernessRuntimeTestHarness harness) =>
            WildernessRanjaGorgeTests.Held(harness, PoolsMappingThePoolsV1.BeaconTemplate);

        /// <summary>Objective 8's "Beacons Placed".</summary>
        private static uint Tracked(WildernessRuntimeTestHarness harness) =>
            harness.Client.Player.Missions[Mapping].Objectives[PoolsMappingThePoolsV1.Tracker].Counters.Values.Single();

        private static void AssertOnTheGround(MapChannel pools, Vector3 position, string what)
        {
            var ground = pools.NavMesh.GroundHeight(position);
            Assert.IsNotNull(ground, $"navmesh under {what}");
            Assert.AreEqual(ground.Value, position.Y, 0.5f, $"{what} on the ground");
        }

        /// <summary>A turn-in whose save fails pays nothing; the mission stays to be turned in.</summary>
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

        /// <summary>The wrong NPC: the server refuses the objective, and it stays open.</summary>
        private static void Refused(WildernessRuntimeTestHarness harness, Creature npc, uint mission, uint objective)
        {
            harness.MoveTo(npc.Position);
            harness.Drain();
            var before = Objective(harness, mission, objective);
            Assert.IsFalse(harness.Manager.CompleteOfferedObjective(harness.Client, npc.EntityId, mission, objective, 1), $"{npc.DbId} refuses {mission}.{objective}");
            harness.Drain();
            Assert.AreEqual(before, Objective(harness, mission, objective));
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

        private static void Accept(WildernessRuntimeTestHarness harness, Creature npc, uint mission)
        {
            harness.MoveTo(npc.Position);
            harness.Drain();
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, npc.EntityId, mission), $"{mission} accepted");
            harness.Drain();
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

        /// <summary>Timlin's choice at objective 9, as PlateauCidTests.Choose picks Pendrik's.</summary>
        private static void Choose(WildernessRuntimeTestHarness harness, Creature npc, uint choice)
        {
            var npcs = new NpcManager(harness, harness.Manager);
            harness.MoveTo(npc.Position);
            harness.Drain();
            npcs.RequestNpcConverse(harness.Client, new RequestNPCConversePacket { EntityId = npc.EntityId });
            npcs.PerformNPCChoice(harness.Client, new PerformNPCChoicePacket
            {
                EntityId = npc.EntityId, MissionId = Mapping, ObjectiveId = PoolsMappingThePoolsV1.Report, PlayerFlagId = 1,
                ChoiceIdx = (int)choice
            });
            harness.Drain();
        }

        /// <summary>The terminal's conversation, as PlateauWedgeRockTests.Open (private) opens a Watchtowers terminal's.</summary>
        private static ConversePacket OpenTerminal(WildernessRuntimeTestHarness harness, DynamicObject terminal)
        {
            harness.MoveTo(terminal.Position + new Vector3(0, 0, 1));
            harness.Drain();
            new NpcManager(harness, harness.Manager).RequestNpcConverse(harness.Client,
                new RequestNPCConversePacket { EntityId = terminal.EntityId, ActionId = ActionId.Converse, ActionArgId = 1 });
            return harness.Drain().OfType<ConversePacket>().LastOrDefault();
        }

        private static Creature Npc(MapChannel pools, uint pool)
        {
            Spawn(pools, pool);
            return RequireNpc(pools, pool);
        }

        /// <summary>A use of the object: the request, then its recovery if one is due (as PoolsSnakePitTests).</summary>
        private static void Use(WildernessRuntimeTestHarness harness, MapChannel map, DynamicObject obj, bool recovers)
        {
            var count = map.PerformRecovery.Count;
            harness.Objects.RequestUseObjectPacket(harness.Client, new RequestUseObjectPacket
            {
                EntityId = obj.EntityId, ActionId = ActionId.UseObject, ActionArgId = PoolsMappingThePoolsV1.UseArgument
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
