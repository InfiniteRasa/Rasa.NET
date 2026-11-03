using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Managers;
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
    /// Marshes set 3, the Harvesters, played through as a player plays it: Researcher Vetter's Reaping the Harvest (881: a
    /// sample from five of the seven gas harvesters east of Falcon Hold), and what follows it: Retread Kline's Hassling the
    /// Harvesters (890: destroy five harvesters), Lieutenant Brazos's Beware: Buried Gas Lines (913: eight detonators at the
    /// four pipeline joints in the east) and Ranger Taavik's A Butcher, a Baker, and a Caretaker (968: kill Fik, Sto and
    /// Barzak).
    ///
    /// Why these tests: Brazos is new on the server, the harvesters, taps and receptacles are the player's own scene
    /// objects, three missions wait for 881, and 881 counts five of seven on a hidden tally. An NPC or object off the
    /// navmesh, an offer before 881, a talk or turn-in at the wrong NPC, a tap or receptacle used twice or after the last,
    /// a sample given with no room, a harvester counted before it falls, a kill that does not count, items kept after an
    /// abandon, progress lost at a reload, or a turn-in that pays twice would each break them. No mission of the set has a
    /// drop, so no drop chance is tested.
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class MarshesHarvestersTests
    {
        private const uint Reaping = MarshesReapingTheHarvestV1.MissionId;
        private const uint Hassling = MarshesHasslingTheHarvestersV1.MissionId;
        private const uint GasLines = MarshesBuriedGasLinesV1.MissionId;
        private const uint Butcher = MarshesButcherBakerCaretakerV1.MissionId;
        private const uint Vetter = MarshesReapingTheHarvestV1.Vetter, Kline = MarshesHasslingTheHarvestersV1.Kline;
        private const uint Taavik = MarshesButcherBakerCaretakerV1.Taavik, Brazos = MarshesHarvestersNpcsV1.Brazos;
        private const uint Morrison = MarshesItLiesInRuinsV1.Morrison;
        private const uint MissionItemFiller = 11519;      // as PlateauCampResistanceTests

        /// <summary>The Falcon Hold Outpost waypoint (World DB teleporter 133).</summary>
        private static readonly Vector3 FalconHoldWaypoint = new(-143.27f, 219.05f, 581.54f);
        /// <summary>The Paludos waypoint (World DB teleporter), as MarshesPaludosTests.</summary>
        private static readonly Vector3 PaludosWaypoint = new(-304.35f, 218.3f, 97.36f);
        /// <summary>The road Plateau Point - Falcon Hold, as MarshesPaludosTests: the way east to the pipelines.</summary>
        private static readonly Vector3 Road = new(-420f, 216.9f, 472.4f);

        [TestInitialize]
        public void Clock() => PlateauGiantKillerSquadTests.UseClock();

        [TestCleanup]
        public void Restore() => PlateauGiantKillerSquadTests.RestoreClock();

        [TestMethod]
        public void BrazosStandsByThePaludosMedTentAndIsWalkedTo()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            var brazos = Npc(marshes, Brazos);
            Assert.AreEqual(MarshesHarvestersNpcsV1.BrazosName, brazos.NameId);
            Assert.AreEqual(MarshesHarvestersNpcsV1.BrazosPackage, brazos.Npc.NpcPackageId);
            Assert.AreNotEqual(TargetCategory.Hostile, brazos.TargetCategory);
            var at = MarshesHarvestersNpcsV1.BrazosAt;
            Assert.IsLessThan(0.1f, Vector2.Distance(new((float)at.X, (float)at.Z), new(brazos.Position.X, brazos.Position.Z)), "Brazos at his place");
            AssertOnGround(marshes, brazos.Position, "Brazos");
            Assert.IsTrue(marshes.NavMesh.FindPath(PaludosWaypoint, brazos.Position, out var reached) != null && reached, "Brazos is walked to");
        }

        [TestMethod]
        public void ReapingTheHarvestTakesFiveSamplesToVetter()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            var vetter = Npc(marshes, Vetter);
            var morrison = Npc(marshes, Morrison);
            Assert.IsFalse(marshes.DynamicObjects.Any(obj => obj.SceneMissionId == Reaping), "no harvester before the mission");

            var offer = WildernessRanjaGorgeTests.Offer(harness, vetter, Reaping);
            CollectionAssert.AreEqual(new[]
                {
                    MarshesReapingTheHarvestV1.BioVest, MarshesReapingTheHarvestV1.StealthBoots,
                    MarshesReapingTheHarvestV1.HazmatLegs, MarshesReapingTheHarvestV1.ReflectiveGloves
                },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, morrison, Reaping), "not Morrison's to give");
            Accept(harness, vetter, Reaping);
            Assert.IsEmpty(Topics(harness, vetter, Reaping), "Vetter hears no objective: the samples count as they are taken");

            var (bodies, taps) = Harvesters(harness, marshes);
            var completed = 0;
            var completeable = 0;
            for (var index = 0; index < MarshesReapingTheHarvestV1.Samples; index++)
            {
                Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Reaping, MarshesReapingTheHarvestV1.Collect), $"open before tap {index + 1}");
                Tap(harness, marshes, taps[index], recovers: true);
                Assert.AreEqual((UseObjectState)MarshesReapingTheHarvestV1.TakenState, taps[index].StateId, $"tap {index + 1} taken");
                Assert.IsFalse(taps[index].IsEnabled, $"tap {index + 1} closed");
                Assert.AreEqual((uint)index + 1, Samples(harness), $"a sample from tap {index + 1}");
                var drained = harness.Drain();
                completed += drained.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Reaping &&
                    packet.ObjectiveId == MarshesReapingTheHarvestV1.Collect);
                completeable += drained.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Reaping && packet.IsCompleteable);
                if (index == 0)
                {
                    Tap(harness, marshes, taps[0], recovers: false);
                    Assert.AreEqual(1U, Samples(harness), "a used tap gives no second sample");
                }
                if (index == 2)
                {
                    WildernessRanjaGorgeTests.Reload(harness);
                    Assert.AreEqual(3U, Samples(harness), "kept at a reload");
                    Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Reaping, MarshesReapingTheHarvestV1.Collect));
                    harness.Drain();
                    (bodies, taps) = Harvesters(harness, marshes, taken: 3);
                }
            }
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Reaping, MarshesReapingTheHarvestV1.Collect), "five sampled");
            Assert.AreEqual((1, 1), (completed, completeable), "ObjectiveCompleted and MissionCompleteable once");
            foreach (var spare in taps.Skip((int)MarshesReapingTheHarvestV1.Samples))
                Tap(harness, marshes, spare, recovers: false);
            Assert.AreEqual(MarshesReapingTheHarvestV1.Samples, Samples(harness), "the sixth and seventh give nothing");
            Assert.IsTrue(bodies.All(body => !body.IsEnabled), "the harvester bodies take no use");

            harness.MoveTo(morrison.Position);
            harness.Drain();
            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, morrison.EntityId, Reaping, 0), "not Morrison's to take");
            FullBagWaits(harness, vetter, Reaping, 0, MarshesReapingTheHarvestV1.StealthBoots);
            FailedTurnInPaysNothing(harness, vetter, Reaping, 0);
            Assert.AreEqual(MarshesReapingTheHarvestV1.Samples, Samples(harness), "kept at the failed turn-in");
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, vetter, Reaping, 0,
                MarshesReapingTheHarvestV1.Experience, MarshesReapingTheHarvestV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Reaping));
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, MarshesReapingTheHarvestV1.BioVest), "the selected vest");
            Assert.AreEqual(0U, Samples(harness), "Vetter keeps them");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, vetter, Reaping), "done once");
        }

        /// <summary>A tap gives nothing while the bag is full and waits; an abandon takes the samples and the harvesters away.</summary>
        [TestMethod]
        public void TheSamplesWaitForRoomAndReapingTheHarvestCanBeAbandoned()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            var vetter = Npc(marshes, Vetter);
            Accept(harness, vetter, Reaping);
            var (_, taps) = Harvesters(harness, marshes);
            WildernessAliaBranchesTests.FillCategory(harness, MissionItemFiller, 50);

            harness.MoveTo(taps[0].Position + new Vector3(0, 0, 1));
            harness.Drain();
            Use(harness, marshes, taps[0], MarshesReapingTheHarvestV1.UseArgument, recovers: true);
            Assert.AreEqual(0U, Samples(harness), "no room");
            Assert.IsTrue(taps[0].IsEnabled, "the tap waits");
            Assert.AreEqual((UseObjectState)MarshesReapingTheHarvestV1.ReadyState, taps[0].StateId);
            WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
            Tap(harness, marshes, taps[0], recovers: true);
            Assert.AreEqual(1U, Samples(harness), "with room it gives");
            // The refused use did not count: the fifth sample, not the fourth, completes the objective.
            for (var index = 1; index < MarshesReapingTheHarvestV1.Samples; index++)
            {
                Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Reaping, MarshesReapingTheHarvestV1.Collect),
                    $"open before sample {index + 1}");
                Tap(harness, marshes, taps[index], recovers: true);
                Assert.AreEqual((uint)index + 1, Samples(harness), $"sample {index + 1}");
            }
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Reaping, MarshesReapingTheHarvestV1.Collect), "complete at the fifth");

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Reaping));
            harness.Drain();
            Assert.AreEqual(0U, Samples(harness), "the samples go with the mission");
            Assert.IsFalse(marshes.DynamicObjects.Any(obj => obj.SceneMissionId == Reaping && obj.IsInWorld), "the harvesters go with it");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, vetter, Reaping), "offered again after an abandon");
            Accept(harness, vetter, Reaping);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Reaping, MarshesReapingTheHarvestV1.Collect), "the old samples do not count");
            Harvesters(harness, marshes);
        }

        [TestMethod]
        public void HasslingTheHarvestersDestroysFiveForKline()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            var kline = Npc(marshes, Kline);
            var vetter = Npc(marshes, Vetter);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, kline, Hassling), "it waits for Reaping the Harvest");
            SeedCompletedMarshes(harness, Reaping);
            marshes = EnterMarshes(harness);
            Assert.IsFalse(marshes.DynamicObjects.Any(obj => obj.SceneMissionId == Hassling), "no harvester before the mission");

            var offer = WildernessRanjaGorgeTests.Offer(harness, kline, Hassling);
            CollectionAssert.AreEqual(new[]
                {
                    MarshesHasslingTheHarvestersV1.BioBoots, MarshesHasslingTheHarvestersV1.GravitonBoots,
                    MarshesHasslingTheHarvestersV1.MechHelmet, MarshesHasslingTheHarvestersV1.StealthVest
                },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, vetter, Hassling), "not Vetter's to give");
            Accept(harness, kline, Hassling);
            Assert.IsEmpty(Topics(harness, kline, Hassling), "Kline hears no objective");

            var targets = Targets(harness, marshes);
            Shoot(harness, marshes, targets[0], (int)MarshesHasslingTheHarvestersV1.HarvesterHitPoints - 1);
            Assert.AreEqual(1U, targets[0].CurrentHitPoints, "hit, not down");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Hassling, MarshesHasslingTheHarvestersV1.Destroy));
            var completed = 0;
            var completeable = 0;
            for (var index = 0; index < targets.Length; index++)
            {
                Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Hassling, MarshesHasslingTheHarvestersV1.Destroy), $"open before harvester {index + 1}");
                Shoot(harness, marshes, targets[index], (int)targets[index].CurrentHitPoints);
                Assert.AreEqual(0U, targets[index].CurrentHitPoints, $"harvester {index + 1} down");
                Assert.AreEqual((UseObjectState)MarshesHasslingTheHarvestersV1.DestroyedState, targets[index].StateId, $"harvester {index + 1} wrecked");
                var drained = harness.Drain();
                completed += drained.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Hassling);
                completeable += drained.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Hassling && packet.IsCompleteable);
                if (index == 1)
                {
                    WildernessRanjaGorgeTests.Reload(harness);
                    Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Hassling, MarshesHasslingTheHarvestersV1.Destroy), "kept at a reload");
                    harness.Drain();
                    targets = Targets(harness, marshes, down: 2);
                    Shoot(harness, marshes, targets[0], 100);
                    Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Hassling, MarshesHasslingTheHarvestersV1.Destroy),
                        "a wreck does not count twice");
                }
            }
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Hassling, MarshesHasslingTheHarvestersV1.Destroy), "five down");
            Assert.AreEqual((1, 1), (completed, completeable), "ObjectiveCompleted and MissionCompleteable once");

            harness.MoveTo(vetter.Position);
            harness.Drain();
            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, vetter.EntityId, Hassling, 1), "not Vetter's to take");
            FullBagWaits(harness, kline, Hassling, 1, MarshesHasslingTheHarvestersV1.BioBoots);
            FailedTurnInPaysNothing(harness, kline, Hassling, 1);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, kline, Hassling, 1,
                MarshesHasslingTheHarvestersV1.Experience, MarshesHasslingTheHarvestersV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Hassling));
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, MarshesHasslingTheHarvestersV1.GravitonBoots), "the selected boots");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, kline, Hassling), "done once");
        }

        /// <summary>An abandon takes the wrecks back: offered again, five whole harvesters stand again.</summary>
        [TestMethod]
        public void HasslingTheHarvestersAbandonedStartsOver()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            SeedCompletedMarshes(harness, Reaping);
            var marshes = EnterMarshes(harness);
            var kline = Npc(marshes, Kline);
            Accept(harness, kline, Hassling);
            var targets = Targets(harness, marshes);
            Shoot(harness, marshes, targets[0], (int)targets[0].CurrentHitPoints);
            Shoot(harness, marshes, targets[1], (int)targets[1].CurrentHitPoints);

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Hassling));
            harness.Drain();
            Assert.IsFalse(harness.Client.Player.Missions.ContainsKey(Hassling), "gone from the log");
            Assert.IsFalse(marshes.DynamicObjects.Any(obj => obj.SceneMissionId == Hassling && obj.IsInWorld), "the harvesters go with it");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, kline, Hassling), "offered again after an abandon");
            Accept(harness, kline, Hassling);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Hassling, MarshesHasslingTheHarvestersV1.Destroy));
            Targets(harness, marshes);
        }

        [TestMethod]
        public void BuriedGasLinesBlowsTheFourJointsForBrazos()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            var brazos = Npc(marshes, Brazos);
            var kline = Npc(marshes, Kline);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, brazos, GasLines), "it waits for Reaping the Harvest");
            SeedCompletedMarshes(harness, Reaping);
            marshes = EnterMarshes(harness);
            Assert.IsFalse(marshes.DynamicObjects.Any(obj => obj.SceneMissionId == GasLines), "no receptacle before the mission");

            var offer = WildernessRanjaGorgeTests.Offer(harness, brazos, GasLines);
            CollectionAssert.AreEqual(new[]
                {
                    MarshesBuriedGasLinesV1.BioBoots, MarshesBuriedGasLinesV1.GravitonBoots,
                    MarshesBuriedGasLinesV1.MechHelmet, MarshesBuriedGasLinesV1.StealthVest
                },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            CollectionAssert.AreEqual(new[] { MarshesBuriedGasLinesV1.MotorAssistBoots },
                offer.FixedReward.FixedItems.Select(item => item.ItemTemplateId).ToArray(), "the boots with every choice");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, kline, GasLines), "not Kline's to give");

            // No room for the detonators: no mission. Two slots freed (eight in stacks of five), they come with it.
            WildernessAliaBranchesTests.FillCategory(harness, MissionItemFiller, 50);
            harness.MoveTo(brazos.Position);
            harness.Drain();
            Assert.IsFalse(harness.Manager.AcceptOfferedMission(harness.Client, brazos.EntityId, GasLines), "no room for the detonators");
            Assert.IsFalse(harness.Client.Player.Missions.ContainsKey(GasLines));
            Assert.AreEqual(0U, Detonators(harness));
            WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
            Assert.IsFalse(harness.Manager.AcceptOfferedMission(harness.Client, brazos.EntityId, GasLines), "one slot holds five of the eight");
            Assert.AreEqual(0U, Detonators(harness), "none of them given");
            WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
            Accept(harness, brazos, GasLines);
            Assert.AreEqual(MarshesBuriedGasLinesV1.Detonators, Detonators(harness), "eight detonators");
            Assert.IsEmpty(Topics(harness, brazos, GasLines), "Brazos hears no objective");

            var receptacles = Receptacles(harness, marshes);
            var completed = 0;
            var completeable = 0;
            var used = 0U;
            // The joints in the order a player walks them from the south (any order counts).
            foreach (var joint in new[] { 0, 1, 2, 3 })
            {
                var objective = MarshesBuriedGasLinesV1.JointRows[joint].Objective;
                for (var side = 0; side < MarshesBuriedGasLinesV1.PerJoint; side++)
                {
                    Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, GasLines, objective), $"joint {joint + 1} open");
                    var receptacle = receptacles[joint][side];
                    Plant(harness, marshes, receptacle, recovers: true);
                    used++;
                    Assert.AreEqual((UseObjectState)MarshesBuriedGasLinesV1.PlantedState, receptacle.StateId, $"joint {joint + 1} side {side + 1} planted");
                    Assert.IsFalse(receptacle.IsEnabled, $"joint {joint + 1} side {side + 1} closed");
                    Assert.AreEqual(MarshesBuriedGasLinesV1.Detonators - used, Detonators(harness), "a detonator each");
                    var drained = harness.Drain();
                    completed += drained.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == GasLines);
                    completeable += drained.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == GasLines && packet.IsCompleteable);
                    if (joint == 0 && side == 0)
                    {
                        Plant(harness, marshes, receptacle, recovers: false);
                        Assert.AreEqual(MarshesBuriedGasLinesV1.Detonators - 1, Detonators(harness), "a receptacle takes one bomb, once");
                        Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, GasLines, objective), "one of two is not the joint");
                    }
                    if (joint == 1 && side == 0)
                    {
                        WildernessRanjaGorgeTests.Reload(harness);
                        Assert.AreEqual(5U, Detonators(harness), "kept at a reload");
                        Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, GasLines, 1), "the first joint kept");
                        Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, GasLines, objective));
                        harness.Drain();
                        receptacles = Receptacles(harness, marshes, planted: 3);
                    }
                }
                Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, GasLines, objective), $"joint {joint + 1} blown");
            }
            Assert.AreEqual((4, 1), (completed, completeable), "ObjectiveCompleted once a joint, MissionCompleteable once");
            Assert.AreEqual(0U, Detonators(harness));

            harness.MoveTo(kline.Position);
            harness.Drain();
            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, kline.EntityId, GasLines, 2), "not Kline's to take");
            FullBagWaits(harness, brazos, GasLines, 2, MarshesBuriedGasLinesV1.BioBoots);
            // The chosen helmet and the fixed boots: a second slot.
            WildernessAliaBranchesTests.FreeOneSlot(harness, MarshesBuriedGasLinesV1.BioBoots);
            FailedTurnInPaysNothing(harness, brazos, GasLines, 2);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, brazos, GasLines, 2,
                MarshesBuriedGasLinesV1.Experience, MarshesBuriedGasLinesV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == GasLines));
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, MarshesBuriedGasLinesV1.MechHelmet), "the selected helmet");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, MarshesBuriedGasLinesV1.MotorAssistBoots), "and the boots");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, brazos, GasLines), "done once");
        }

        /// <summary>An abandon takes the detonators and the receptacles away; taken again, eight new detonators and eight open receptacles.</summary>
        [TestMethod]
        public void BuriedGasLinesAbandonedTakesTheDetonatorsBack()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            SeedCompletedMarshes(harness, Reaping);
            var marshes = EnterMarshes(harness);
            var brazos = Npc(marshes, Brazos);
            Accept(harness, brazos, GasLines);
            var receptacles = Receptacles(harness, marshes);
            Plant(harness, marshes, receptacles[0][0], recovers: true);
            Plant(harness, marshes, receptacles[0][1], recovers: true);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, GasLines, 1));
            Assert.AreEqual(MarshesBuriedGasLinesV1.Detonators - 2, Detonators(harness));

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, GasLines));
            harness.Drain();
            Assert.AreEqual(0U, Detonators(harness), "the detonators go with the mission");
            Assert.IsFalse(marshes.DynamicObjects.Any(obj => obj.SceneMissionId == GasLines && obj.IsInWorld), "the receptacles go with it");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, brazos, GasLines), "offered again after an abandon");

            Accept(harness, brazos, GasLines);
            Assert.AreEqual(MarshesBuriedGasLinesV1.Detonators, Detonators(harness), "eight new detonators");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, GasLines, 1), "the old joint does not count");
            Receptacles(harness, marshes);
        }

        [TestMethod]
        public void AButcherABakerAndACaretakerTakesThreeDeathsToTaavik()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            var taavik = Npc(marshes, Taavik);
            var kline = Npc(marshes, Kline);
            var bosses = MarshesButcherBakerCaretakerV1.Bosses;
            Spawn(marshes, bosses.Select(boss => boss.Boss).ToArray());
            foreach (var boss in bosses)
                AssertOnGround(marshes, Living(marshes).Single(creature => creature.DbId == boss.Boss).Position, $"boss {boss.Boss}");

            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, taavik, Butcher), "it waits for Reaping the Harvest");
            // A boss killed before the mission is not counted.
            Kill(harness, marshes, MarshesButcherBakerCaretakerV1.Fik);
            harness.Drain();
            Respawn(marshes, MarshesButcherBakerCaretakerV1.Fik);
            SeedCompletedMarshes(harness, Reaping);
            marshes = EnterMarshes(harness);

            var offer = WildernessRanjaGorgeTests.Offer(harness, taavik, Butcher);
            CollectionAssert.AreEqual(new[]
                {
                    MarshesRewardBlocksV1.Current(MarshesButcherBakerCaretakerV1.MissionId, MarshesButcherBakerCaretakerV1.ElectricBlade), MarshesRewardBlocksV1.Current(MarshesButcherBakerCaretakerV1.MissionId, MarshesButcherBakerCaretakerV1.HealingDisc),
                    MarshesRewardBlocksV1.Current(MarshesButcherBakerCaretakerV1.MissionId, MarshesButcherBakerCaretakerV1.Shotgun), MarshesRewardBlocksV1.Current(MarshesButcherBakerCaretakerV1.MissionId, MarshesButcherBakerCaretakerV1.IncendiaryStaff)
                },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, kline, Butcher), "not Kline's to give");
            Accept(harness, taavik, Butcher);
            Assert.IsEmpty(Topics(harness, taavik, Butcher), "Taavik hears no objective");
            foreach (var boss in bosses)
                Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Butcher, boss.Objective), $"{boss.Boss}: the old kill does not count");

            // Barzak first, then Sto, then Fik: any order. A kill is no pickup: the corpse is left as it is.
            var completed = 0;
            var completeable = 0;
            foreach (var (objective, boss) in new[] { (3U, MarshesButcherBakerCaretakerV1.Barzak), (2U, MarshesButcherBakerCaretakerV1.Sto),
                (1U, MarshesButcherBakerCaretakerV1.Fik) })
            {
                if (!Alive(marshes).Any(creature => creature.DbId == boss))
                    Respawn(marshes, boss);
                Kill(harness, marshes, boss);
                Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Butcher, objective), $"{boss} dead");
                var drained = harness.Drain();
                completed += drained.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Butcher && packet.ObjectiveId == objective);
                completeable += drained.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Butcher && packet.IsCompleteable);
                if (objective == 3)
                {
                    WildernessRanjaGorgeTests.Reload(harness);
                    Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Butcher, 3), "kept at a reload");
                    Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Butcher, 1));
                    harness.Drain();
                    Assert.IsFalse(harness.Client.Player.Missions[Butcher].Completeable, "one of three");
                }
            }
            Assert.AreEqual((3, 1), (completed, completeable), "ObjectiveCompleted once a boss, MissionCompleteable once");
            Assert.IsTrue(harness.Client.Player.Missions[Butcher].Completeable);

            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, kline.EntityId, Butcher, 3), "not Kline's to take");
            FullBagWaits(harness, taavik, Butcher, 3, MarshesRewardBlocksV1.Current(MarshesButcherBakerCaretakerV1.MissionId, MarshesButcherBakerCaretakerV1.ElectricBlade));
            FailedTurnInPaysNothing(harness, taavik, Butcher, 3);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, taavik, Butcher, 3,
                MarshesButcherBakerCaretakerV1.Experience, MarshesButcherBakerCaretakerV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Butcher));
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, MarshesRewardBlocksV1.Current(MarshesButcherBakerCaretakerV1.MissionId, MarshesButcherBakerCaretakerV1.IncendiaryStaff)), "the selected staff");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, taavik, Butcher), "done once");
        }

        /// <summary>An abandon takes the kills back: offered again, and taken again the bosses must die again.</summary>
        [TestMethod]
        public void AButcherABakerAndACaretakerAbandonedStartsOver()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            SeedCompletedMarshes(harness, Reaping);
            var marshes = EnterMarshes(harness);
            var taavik = Npc(marshes, Taavik);
            Spawn(marshes, MarshesButcherBakerCaretakerV1.Sto);
            Accept(harness, taavik, Butcher);
            Kill(harness, marshes, MarshesButcherBakerCaretakerV1.Sto);
            harness.Drain();
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Butcher, MarshesButcherBakerCaretakerV1.KillSto));

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Butcher));
            harness.Drain();
            Assert.IsFalse(harness.Client.Player.Missions.ContainsKey(Butcher), "gone from the log");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, taavik, Butcher), "offered again after an abandon");
            Accept(harness, taavik, Butcher);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Butcher, MarshesButcherBakerCaretakerV1.KillSto), "the old kill does not count");
        }

        /// <summary>
        /// The seven harvesters: the player's own bodies (no use) and taps (Bane item crates), on the navmesh and walked to
        /// from Falcon Hold; all taps ready but the first <paramref name="taken"/>.
        /// </summary>
        private static (DynamicObject[] Bodies, DynamicObject[] Taps) Harvesters(WildernessRuntimeTestHarness harness, MapChannel marshes, int taken = 0)
        {
            DynamicObject Role(string role) => marshes.DynamicObjects.Single(obj => obj.SceneMissionId == Reaping &&
                obj.SceneActorRole == role && obj.IsInWorld);
            var count = MarshesReapingTheHarvestV1.Harvesters.Length;
            var bodies = Enumerable.Range(0, count).Select(index => Role(MarshesReapingTheHarvestV1.BodyRole(index))).ToArray();
            var taps = Enumerable.Range(0, count).Select(index => Role(MarshesReapingTheHarvestV1.TapRole(index))).ToArray();
            for (var index = 0; index < count; index++)
            {
                var at = MarshesReapingTheHarvestV1.Harvesters[index];
                var tapAt = MarshesReapingTheHarvestV1.Taps[index];
                Assert.AreEqual(MarshesReapingTheHarvestV1.BodyClass, (uint)bodies[index].EntityClassId, $"body {index + 1}");
                Assert.AreEqual(MarshesReapingTheHarvestV1.TapClass, (uint)taps[index].EntityClassId, $"tap {index + 1}");
                Assert.AreEqual(new Vector3(at.X, at.Y, at.Z), bodies[index].Position);
                Assert.AreEqual(new Vector3(tapAt.X, tapAt.Y, tapAt.Z), taps[index].Position);
                Assert.IsLessThan(4f, Vector3.Distance(bodies[index].Position, taps[index].Position), "the tap at the harvester's base");
                Assert.AreEqual(harness.Client.Player.Id, bodies[index].SceneOwnerCharacterId, "the player's own");
                Assert.AreEqual(harness.Client.Player.Id, taps[index].SceneOwnerCharacterId, "the player's own");
                Assert.IsFalse(bodies[index].IsEnabled, $"body {index + 1} takes no use");
                Assert.AreEqual(index < taken ? MarshesReapingTheHarvestV1.TakenState : MarshesReapingTheHarvestV1.ReadyState, (uint)taps[index].StateId,
                    $"tap {index + 1} state");
                Assert.AreEqual(index >= taken, taps[index].IsEnabled, $"tap {index + 1} open");
                foreach (var obj in new[] { bodies[index], taps[index] })
                {
                    AssertOnGround(marshes, obj.Position, $"harvester {index + 1}");
                    Assert.IsTrue(marshes.NavMesh.FindPath(FalconHoldWaypoint, obj.Position, out var complete) != null && complete,
                        $"harvester {index + 1} is walked to");
                }
            }
            return (bodies, taps);
        }

        /// <summary>890's five harvesters: the player's own destructibles at 881's first five places; all whole but the first <paramref name="down"/>.</summary>
        private static DynamicObject[] Targets(WildernessRuntimeTestHarness harness, MapChannel marshes, int down = 0)
        {
            var targets = Enumerable.Range(0, (int)MarshesHasslingTheHarvestersV1.Count).Select(index => marshes.DynamicObjects.Single(obj =>
                obj.SceneMissionId == Hassling && obj.SceneActorRole == MarshesHasslingTheHarvestersV1.HarvesterRole(index) && obj.IsInWorld)).ToArray();
            for (var index = 0; index < targets.Length; index++)
            {
                var target = targets[index];
                var at = MarshesHarvestersNpcsV1.Harvesters[index];
                Assert.AreEqual(MarshesHasslingTheHarvestersV1.HarvesterClass, (uint)target.EntityClassId, $"harvester {index + 1}");
                Assert.AreEqual(new Vector3(at.X, at.Y, at.Z), target.Position);
                Assert.AreEqual(harness.Client.Player.Id, target.SceneOwnerCharacterId, "the player's own");
                Assert.AreEqual(index < down ? 0U : MarshesHasslingTheHarvestersV1.HarvesterHitPoints, target.CurrentHitPoints, $"harvester {index + 1} hit points");
                Assert.AreEqual(index < down ? MarshesHasslingTheHarvestersV1.DestroyedState : MarshesHasslingTheHarvestersV1.IntactState,
                    (uint)target.StateId, $"harvester {index + 1} state");
                AssertOnGround(marshes, target.Position, $"harvester {index + 1}");
                Assert.IsTrue(marshes.NavMesh.FindPath(FalconHoldWaypoint, target.Position, out var complete) != null && complete,
                    $"harvester {index + 1} is walked to");
            }
            return targets;
        }

        /// <summary>913's receptacles, two a joint: the player's own acceptors, on the navmesh and walked to from the road; all ready but the first <paramref name="planted"/>.</summary>
        private static DynamicObject[][] Receptacles(WildernessRuntimeTestHarness harness, MapChannel marshes, int planted = 0)
        {
            var rows = MarshesBuriedGasLinesV1.JointRows;
            var all = new DynamicObject[rows.Length][];
            var number = 0;
            for (var joint = 0; joint < rows.Length; joint++)
            {
                all[joint] = new DynamicObject[rows[joint].Receptacles.Length];
                for (var side = 0; side < all[joint].Length; side++, number++)
                {
                    var role = MarshesBuriedGasLinesV1.ReceptacleRole(joint, side);
                    var obj = marshes.DynamicObjects.Single(candidate => candidate.SceneMissionId == GasLines &&
                        candidate.SceneActorRole == role && candidate.IsInWorld);
                    var at = rows[joint].Receptacles[side];
                    Assert.AreEqual(MarshesBuriedGasLinesV1.ReceptacleClass, (uint)obj.EntityClassId, role);
                    Assert.AreEqual(new Vector3(at.X, at.Y, at.Z), obj.Position);
                    Assert.AreEqual(harness.Client.Player.Id, obj.SceneOwnerCharacterId, "the player's own");
                    Assert.AreEqual(number < planted ? MarshesBuriedGasLinesV1.PlantedState : MarshesBuriedGasLinesV1.ReadyState, (uint)obj.StateId, $"{role} state");
                    Assert.AreEqual(number >= planted, obj.IsEnabled, $"{role} open");
                    Assert.IsLessThan(18f, Vector2.Distance(new(obj.Position.X, obj.Position.Z), new(rows[joint].At.X, rows[joint].At.Z)), $"{role} by its pipeline end");
                    AssertOnGround(marshes, obj.Position, role);
                    Assert.IsTrue(marshes.NavMesh.FindPath(Road, obj.Position, out var complete) != null && complete, $"{role} is walked to");
                    all[joint][side] = obj;
                }
            }
            return all;
        }

        private static void Tap(WildernessRuntimeTestHarness harness, MapChannel marshes, DynamicObject tap, bool recovers)
        {
            harness.MoveTo(tap.Position + new Vector3(0, 0, 1));
            harness.Drain();
            Use(harness, marshes, tap, MarshesReapingTheHarvestV1.UseArgument, recovers);
        }

        private static void Plant(WildernessRuntimeTestHarness harness, MapChannel marshes, DynamicObject receptacle, bool recovers)
        {
            harness.MoveTo(receptacle.Position + new Vector3(0, 0, 1));
            harness.Drain();
            Use(harness, marshes, receptacle, MarshesBuriedGasLinesV1.UseArgument, recovers);
        }

        /// <summary>A weapon hit of <paramref name="damage"/> on the object, as WildernessLandingZoneTests.DamageObject.</summary>
        private static void Shoot(WildernessRuntimeTestHarness harness, MapChannel marshes, DynamicObject obj, int damage)
        {
            harness.MoveTo(obj.Position + new Vector3(0, 0, 1));
            harness.Drain();
            var action = new ActionData(harness.Client.Player, ActionId.WeaponAttack, 133, 0) { TargetId = obj.EntityId };
            MissileManager.Instance.MissileLaunch(marshes, action, damage);
            MissileManager.Instance.DoWork(marshes, 1000);
        }

        private static uint Samples(WildernessRuntimeTestHarness harness) =>
            WildernessRanjaGorgeTests.Held(harness, MarshesReapingTheHarvestV1.SampleTemplate);

        private static uint Detonators(WildernessRuntimeTestHarness harness) =>
            WildernessRanjaGorgeTests.Held(harness, MarshesBuriedGasLinesV1.DetonatorTemplate);
    }
}
