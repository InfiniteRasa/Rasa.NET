using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.ClientData;
using Rasa.Config;
using Rasa.Data;
using Rasa.Game;
using Rasa.Managers;
using Rasa.Packets.Mission.Server;
using Rasa.Services.Preloader.Missions.Ashen;
using Rasa.Structures;
using Rasa.Structures.World;
using Rasa.Test.Missions.Marshes;
using Rasa.Test.Missions.Plateau;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions.Ashen
{
    using static MarshesFalconHoldTests;
    using static MarshesPaludosTests;
    using static PlateauCampResistanceTests;
    using W = AshenWhiteOasisNpcsV1;
    using Harvesters = AshenAnalyzingHarvestersV1;
    using Murky = AshenMurkyWatersV1;
    using Prying = AshenPryingEyesV1;
    using Gas = AshenGasShortagesV1;
    using P = MarshesPreoDasTests;
    using E = MarshesEscortsTests;
    using FC = AshenFormalComplaintsTests;
    using F = MarshesClientFloorTests;

    /// <summary>
    /// Ashen Desert set 11, White Oasis Post, played through as a player plays it: Captain Daris Linier's Analyzing the
    /// Harvesters (1295: Doctor Meeks' diagnostic tool on the four Harvester Control Panels of Maji Field, turned in at Meeks)
    /// and Prying Eyes (1303: Overseer Ulovik), Meeks' Murky Waters (1298: Agent Hale's choice, player flag 257 the poison or
    /// 258 the repair) and Lieutenant Dania Fontecilla's Gas Shortages (1356: three fuel pumps on the Sheitan Embankment blown
    /// with her shaped charges). With them the set's places: Meeks, Hale and Fontecilla (moved off the control point's
    /// console), the panels, the pumps and their devices, Ulovik, the walks to them, and the White Oasis control point's
    /// hold on its givers.
    ///
    /// Why these tests: the choice is for good and opens set 12's branches by its flag; the panels and pumps are the player's
    /// own; the givers stand within 130 m of control point 17 and serve only while the AFS hold it. A choice that sets the
    /// wrong flag or both, a talk heard in the wrong variant, a panel or device that counts twice or not at all, a tool or charge
    /// not given or not taken, a follow-up offered too early, a place the client's floor does not reach or whose walk passes a
    /// hostile camp (the set 10 playtest, to-fix row 17), or a turn-in that pays twice or on a full bag would each break them.
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class AshenWhiteOasisTests
    {
        private const uint WeaponFiller = 3869;            // inventory category 1 (quest-test-pattern)
        private const uint ConsumableFiller = 45060;       // Class VII Basic Med Pack (as AshenFormalComplaintsTests)
        private const uint MissionItemFiller = 11519;      // as PlateauCampResistanceTests
        private const uint ClientPanelClass = 9478;        // UsableTwoStateHumComputerV01V06
        private const uint ClientPanelName = 69;           // usablenameoverride LIGO_ASHEN_MAJI_DIAGNOSTIC "Harvester Control Panel"
        private const uint ClientPanelUseArgument = 1;     // usabledata lookup[9478][0]
        private const uint PanelOff = 55, PanelOn = 56;
        private const uint ClientToolTemplate = 97477;     // client item class 25476 "Harvester Diagnostic Tool"
        private const uint ClientChargeTemplate = 111185;  // client item class 26442 "Shaped Charge"
        // Fix F (to-fix row 23): the V02 pump 21007 (UsableInertDestBaneRawFuelPumpHouseV02), set 11's V01 10190 showed no wreck;
        // the two models' bounds are the same.
        private const uint ClientPumpClass = AshenFixFV1.PumpClass;
        private const uint ClientDeviceClass = 7334;       // UsableItemAccHumBombV01 "Detonation Device"
        private const uint ClientDeviceUseArgument = 1;    // usabledata lookup[7334][0]
        private const uint DeviceReady = 119, DevicePlanted = 121, Intact = 110, Destroyed = 2;
        private const uint MajiFithikPool = 540075;        // the Fithik pool nearest Ulovik (World DB)
        private const uint Fithik = 540018;
        private const uint WhiteOasisPoint = 17;           // ControlPointSeed / control_point 17 "White Oasis Post"

        /// <summary>The client's model bounds (MeshLibrary, entity_meshes.csv, 2026-11-10): the control point 3814 x +-6.0, z +-8.6; the fuel pump 10190 (and fix F's 21007, the same bounds) x +-4.9, z +-2.95.</summary>
        private const float PointHalfX = 6.0f, PointHalfZ = 8.6f, PumpHalfX = 4.9f, PumpHalfZ = 2.95f;
        private const int ClientPadClass = 20750;          // ArchBaneObjLandingpadv02, the Sheitan Embankment's three pads (client statics)
        private const float DeviceClear = 3;               // a Detonation Device stands this far outside every pump's model
        private const float PadClear = 2f;                 // a pump's model and a device this far outside every landing pad's placed model (the garrison's dropship)
        private const float NpcClear = 1.5f;               // an NPC this far outside the control point's model
        /// <summary>A walk to a mission place keeps this far (2D) outside every hostile pool's circle that is not the mission's own (the set 10 playtest, to-fix row 17).</summary>
        internal const float HostileClearance = 30;
        private const int SeenAtLeast = 16;                // the places round a panel, pump or device a player sees it from (of 16)


        private static readonly Vector3 WhiteOasis = AshenLeadInsTests.WhiteOasisWaypoint;

        [TestInitialize]
        public void Clock() => PlateauGiantKillerSquadTests.UseClock();

        [TestCleanup]
        public void Restore() => PlateauGiantKillerSquadTests.RestoreClock();

        /// <summary>
        /// Daris Linier offers Analyzing the Harvesters and Prying Eyes at once (no requirement named), Fontecilla Gas Shortages;
        /// Murky Waters waits for 1295 and is Meeks' to give (not Hale's, not Daris'); Hale and Meeks offer nothing else. After
        /// 1295 Meeks offers Murky Waters, after a reload too.
        /// </summary>
        [TestMethod]
        public void TheWhiteOasisOffersComeInTurn()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var desert = AshenLeadInsTests.EnterAshen(harness, WhiteOasis);
            var daris = Npc(desert, W.DarisLinier);
            var fontecilla = Npc(desert, W.Fontecilla);
            var meeks = Npc(desert, W.Meeks);
            var hale = Npc(desert, W.Hale);
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, daris, Harvesters.MissionId), "1295 at once");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, daris, Prying.MissionId), "1303 at once");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, fontecilla, Gas.MissionId), "1356 at once");
            foreach (var npc in new[] { daris, fontecilla, meeks, hale })
                Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, npc, Murky.MissionId), $"{npc.DbId}: 1298 not before 1295");
            foreach (var npc in new[] { meeks, hale })
                foreach (var mission in new[] { Harvesters.MissionId, Prying.MissionId, Gas.MissionId })
                    Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, npc, mission), $"{mission} is not {npc.DbId}'s to give");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, fontecilla, Harvesters.MissionId), "1295 is not Fontecilla's");

            AshenRuntime.SeedCompleted(harness, Harvesters.MissionId);
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, meeks, Murky.MissionId), "1295 done: Meeks offers 1298");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, hale, Murky.MissionId), "not Hale's to give");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, daris, Murky.MissionId), "not Daris' to give");
            AshenRuntime.Reload(harness);
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, meeks, Murky.MissionId), "after a reload too");
        }

        /// <summary>
        /// Analyzing the Harvesters: Daris offers the four weapons; a bag with no room for the tool refuses the accept (no tool,
        /// no panel); accepted, the tool comes at once, a marker on each panel on Ashen Desert, nobody hears an objective. The
        /// player's own four panels (the client's monitor, named "Harvester Control Panel", off) each turn on at a use and
        /// complete their own objective once, in any order (3, 1, 4, 2); a used panel does nothing more; a reload keeps them;
        /// the fourth makes the mission completeable once. Daris cannot take it; a full bag waits, a failed save pays nothing;
        /// Meeks pays the weapon chosen, once, and takes the tool; then he offers Murky Waters.
        /// </summary>
        [TestMethod]
        public void AnalyzingTheHarvestersRunsTheToolOnTheFourPanels()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var desert = AshenLeadInsTests.EnterAshen(harness, WhiteOasis);
            var daris = Npc(desert, W.DarisLinier);
            var meeks = Npc(desert, W.Meeks);
            var offer = WildernessRanjaGorgeTests.Offer(harness, daris, Harvesters.MissionId);
            CollectionAssert.AreEqual(new[] { Harvesters.FlameThrower, Harvesters.IncendiaryNetGun, Harvesters.CryogenicLeechGun, Harvesters.PulseRifle },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());

            WildernessAliaBranchesTests.FillCategory(harness, MissionItemFiller, 50);
            harness.MoveTo(daris.Position);
            harness.Drain();
            Assert.IsFalse(harness.Manager.AcceptOfferedMission(harness.Client, daris.EntityId, Harvesters.MissionId), "no room for the tool");
            harness.Manager.Scenes.Tick(desert);
            Assert.AreEqual(0U, Tool(harness), "no tool");
            Assert.IsFalse(desert.DynamicObjects.Any(obj => obj.SceneMissionId == Harvesters.MissionId && obj.IsInWorld), "no panel");
            WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);

            Accept(harness, daris, Harvesters.MissionId);
            Assert.AreEqual(1U, Tool(harness), "the diagnostic tool at the accept");
            Assert.IsEmpty(Topics(harness, daris, Harvesters.MissionId), "Daris hears no objective");
            Assert.IsEmpty(Topics(harness, meeks, Harvesters.MissionId), "Meeks hears no objective");
            var snapshot = harness.Manager.BuildStatusSnapshot(harness.Client.Player)[Harvesters.MissionId].ObjectivesList;
            Assert.HasCount(Harvesters.PanelsAt.Length, snapshot);
            foreach (var objective in snapshot)
            {
                var marker = objective.IndicatorList.Single();
                var at = Harvesters.PanelsAt[objective.ObjectiveId - 1];
                Assert.IsLessThan(0.1f, Vector2.Distance(new(marker.Position.X, marker.Position.Z), new(at.X, at.Z)), $"objective {objective.ObjectiveId}'s marker on its panel");
                Assert.AreEqual(AshenMissionDataV1.MapContextId, marker.MapContextId, "on Ashen Desert");
            }

            var panels = Panels(harness, desert);
            var log = new List<object>();
            var done = new List<int>();
            foreach (var number in new[] { 3, 1, 4, 2 })
            {
                var panel = panels[number - 1];
                harness.MoveTo(panel.Position + new Vector3(0, 0, 1));
                harness.Drain();
                Use(harness, desert, panel, ClientPanelUseArgument, recovers: true);
                var packets = harness.Drain().Cast<object>().Concat(E.Tick(harness, desert, 250)).ToList();
                log.AddRange(packets);
                done.Add(number);
                Assert.AreEqual(((UseObjectState)PanelOn, false), (panel.StateId, panel.IsEnabled), $"panel {number} on and closed");
                Assert.AreEqual(1, packets.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Harvesters.MissionId && packet.ObjectiveId == number),
                    $"panel {number}: its objective, once");
                Assert.AreEqual(done.Count, Analyzed(harness), $"panel {number}: only its own objective");
                Assert.AreEqual(1U, Tool(harness), "the tool stays");
                if (number == 3)
                {
                    TryUse(harness, desert, panel);
                    Assert.AreEqual(1, Analyzed(harness), "a used panel does nothing more");
                    P.ReloadAndRecover(harness, desert);
                    Assert.AreEqual(1, Analyzed(harness), "kept at a reload");
                    panels = Panels(harness, desert, done: new[] { 3 });
                }
                if (done.Count < Harvesters.PanelsAt.Length)
                    Assert.IsFalse(harness.Client.Player.Missions[Harvesters.MissionId].Completeable, $"{done.Count} of 4: not completeable");
            }
            Assert.IsTrue(harness.Client.Player.Missions[Harvesters.MissionId].Completeable, "four panels");
            Assert.AreEqual(1, log.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Harvesters.MissionId && packet.IsCompleteable), "MissionCompleteable once");

            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, daris.EntityId, Harvesters.MissionId, 0), "not Daris' to take");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, meeks, Murky.MissionId), "1298 not before 1295 is turned in");
            FullBagWaits(harness, meeks, Harvesters.MissionId, 0, WeaponFiller);
            FailedTurnInPaysNothing(harness, meeks, Harvesters.MissionId, 0);
            Assert.AreEqual(1U, Tool(harness), "kept at the failed turn-in");
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, meeks, Harvesters.MissionId, 0, Harvesters.Experience, Harvesters.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Harvesters.MissionId), "MissionRewarded once");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, Harvesters.FlameThrower), "the weapon chosen");
            Assert.AreEqual(0U, Tool(harness), "Meeks takes the tool");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, daris, Harvesters.MissionId), "done once");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, meeks, Murky.MissionId), "Meeks offers Murky Waters");
        }

        /// <summary>
        /// An abandon of Analyzing the Harvesters after one panel takes the tool and the panels; offered again; taken again, the
        /// tool again and four panels off, all four open.
        /// </summary>
        [TestMethod]
        public void AnalyzingTheHarvestersAbandonedTakesTheToolAndThePanels()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var desert = AshenLeadInsTests.EnterAshen(harness, WhiteOasis);
            var daris = Npc(desert, W.DarisLinier);
            Accept(harness, daris, Harvesters.MissionId);
            var panels = Panels(harness, desert);
            harness.MoveTo(panels[1].Position + new Vector3(0, 0, 1));
            harness.Drain();
            Use(harness, desert, panels[1], ClientPanelUseArgument, recovers: true);
            E.Tick(harness, desert, 250);
            harness.Drain();
            Assert.AreEqual(1, Analyzed(harness));
            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Harvesters.MissionId));
            harness.Manager.Scenes.Tick(desert);
            harness.Drain();
            Assert.AreEqual(0U, Tool(harness), "the tool goes with the mission");
            Assert.IsFalse(desert.DynamicObjects.Any(obj => obj.SceneMissionId == Harvesters.MissionId && obj.IsInWorld), "the panels go with the abandon");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, daris, Harvesters.MissionId), "offered again after an abandon");
            Accept(harness, daris, Harvesters.MissionId);
            Assert.AreEqual(1U, Tool(harness), "the tool again");
            Panels(harness, desert);
            Assert.AreEqual(0, Analyzed(harness), "all open again");
        }

        /// <summary>
        /// Murky Waters, choice 2 (the poison). Meeks offers the four stacks; accepted, Hale asks the client's choice (objective 1,
        /// variant 1) and hears nothing else, Meeks hears nothing; only 1 shows, a marker at Hale on Ashen Desert. Choice 2
        /// completes 1 once, sets flag 257 to 1 and 258 to 0, opens 2 (3 stays shut); a reload keeps it; Hale then hears 2 in the
        /// client's variant 257, which makes the mission completeable once. Meeks cannot take it; a full bag waits, a failed save
        /// pays nothing; Hale pays the stack chosen, once.
        /// </summary>
        [TestMethod]
        public void AgreeingWithHaleSetsThePoisonFlag()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            AshenRuntime.SeedCompleted(harness, Harvesters.MissionId);
            var desert = AshenLeadInsTests.EnterAshen(harness, WhiteOasis);
            var meeks = Npc(desert, W.Meeks);
            var hale = Npc(desert, W.Hale);
            var offer = WildernessRanjaGorgeTests.Offer(harness, meeks, Murky.MissionId);
            CollectionAssert.AreEqual(new[] { Murky.BasicMedPack, Murky.EmpBomb, Murky.FragGrenade, Murky.CryogenicGrenade },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());

            Accept(harness, meeks, Murky.MissionId);
            CollectionAssert.AreEqual(new[] { (Murky.Speak, 1U) }, Choices(harness, hale, Murky.MissionId), "Hale asks the choice");
            Assert.IsEmpty(Topics(harness, hale, Murky.MissionId), "and hears nothing yet");
            Assert.IsEmpty(Choices(harness, meeks, Murky.MissionId), "Meeks asks nothing");
            var snapshot = harness.Manager.BuildStatusSnapshot(harness.Client.Player)[Murky.MissionId].ObjectivesList;
            Assert.AreEqual(Murky.Speak, snapshot.Single().ObjectiveId, "only 1 shows (4 is hidden, 2 and 3 shut)");
            var marker = snapshot.Single().IndicatorList.Single();
            Assert.AreEqual(AshenMissionDataV1.MapContextId, marker.MapContextId, "a marker on Ashen Desert");
            Assert.IsLessThan(1f, Vector2.Distance(new(marker.Position.X, marker.Position.Z), new(hale.Position.X, hale.Position.Z)), "at Hale");

            var packets = FC.Choose(harness, hale, Murky.MissionId, Murky.Speak, Murky.AgreeWithHale);
            Assert.AreEqual(1, packets.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Murky.MissionId && packet.ObjectiveId == Murky.Speak), "1 once");
            Assert.IsFalse(packets.OfType<MissionCompleteablePacket>().Any(packet => packet.MissionId == Murky.MissionId && packet.IsCompleteable), "not yet completeable");
            Assert.AreEqual((1U, 0U), (FC.Flag(harness, Murky.PoisonFlag), FC.Flag(harness, Murky.RepairFlag)), "flag 257 set, 258 not");
            Assert.AreEqual((MissionObjectiveState.Incomplete, MissionObjectiveState.Inactive),
                (Objective(harness, Murky.MissionId, Murky.Poison), Objective(harness, Murky.MissionId, Murky.Repair)), "2 opens, 3 stays shut");
            AshenRuntime.Reload(harness);
            harness.Drain();
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Murky.MissionId, Murky.Poison), "kept at a reload");
            Assert.IsEmpty(Choices(harness, hale, Murky.MissionId), "no second choice");
            CollectionAssert.AreEqual(new[] { (Murky.Poison, Murky.PoisonFlag) }, FC.TopicVariants(harness, hale, Murky.MissionId), "2 in variant 257");
            packets = FC.Say(harness, hale, Murky.MissionId, Murky.Poison, Murky.PoisonFlag);
            Assert.AreEqual(1, packets.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Murky.MissionId && packet.IsCompleteable), "MissionCompleteable once");
            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, meeks.EntityId, Murky.MissionId, 1), "not Meeks' to take");
            FullBagWaits(harness, hale, Murky.MissionId, 1, ConsumableFiller);
            FailedTurnInPaysNothing(harness, hale, Murky.MissionId, 1);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, hale, Murky.MissionId, 1, Murky.Experience, Murky.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Murky.MissionId), "MissionRewarded once");
            Assert.AreEqual(Murky.EmpBombs, WildernessRanjaGorgeTests.Held(harness, Murky.EmpBomb), "the EMP bomb chosen");
            Assert.AreEqual((1U, 0U), (FC.Flag(harness, Murky.PoisonFlag), FC.Flag(harness, Murky.RepairFlag)), "the side stays after the turn-in");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, meeks, Murky.MissionId), "done once");
        }

        /// <summary>
        /// Murky Waters, choice 1 (the repair), after an abandon of choice 2: the abandon keeps flag 257; taken again, the choice
        /// is asked again and choice 1 turns the flags round (258 to 1, 257 to 0) and opens 3 (2 stays shut); Hale hears 3 in the
        /// client's variant 258 and pays: only one side is ever set.
        /// </summary>
        [TestMethod]
        public void RefusingHaleSetsTheRepairFlag()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            AshenRuntime.SeedCompleted(harness, Harvesters.MissionId);
            var desert = AshenLeadInsTests.EnterAshen(harness, WhiteOasis);
            var meeks = Npc(desert, W.Meeks);
            var hale = Npc(desert, W.Hale);
            Accept(harness, meeks, Murky.MissionId);
            FC.Choose(harness, hale, Murky.MissionId, Murky.Speak, Murky.AgreeWithHale);
            Assert.AreEqual((1U, 0U), (FC.Flag(harness, Murky.PoisonFlag), FC.Flag(harness, Murky.RepairFlag)), "choice 2 first");
            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Murky.MissionId));
            harness.Drain();
            Assert.AreEqual((1U, 0U), (FC.Flag(harness, Murky.PoisonFlag), FC.Flag(harness, Murky.RepairFlag)), "kept at an abandon");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, meeks, Murky.MissionId), "offered again");

            Accept(harness, meeks, Murky.MissionId);
            CollectionAssert.AreEqual(new[] { (Murky.Speak, 1U) }, Choices(harness, hale, Murky.MissionId), "asked again");
            var packets = FC.Choose(harness, hale, Murky.MissionId, Murky.Speak, Murky.RefuseHale);
            Assert.AreEqual(1, packets.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Murky.MissionId && packet.ObjectiveId == Murky.Speak), "1 once");
            Assert.AreEqual((0U, 1U), (FC.Flag(harness, Murky.PoisonFlag), FC.Flag(harness, Murky.RepairFlag)), "flag 258 set, 257 back to 0");
            Assert.AreEqual((MissionObjectiveState.Inactive, MissionObjectiveState.Incomplete),
                (Objective(harness, Murky.MissionId, Murky.Poison), Objective(harness, Murky.MissionId, Murky.Repair)), "3 opens, 2 stays shut");
            CollectionAssert.AreEqual(new[] { (Murky.Repair, Murky.RepairFlag) }, FC.TopicVariants(harness, hale, Murky.MissionId), "3 in variant 258");
            packets = FC.Say(harness, hale, Murky.MissionId, Murky.Repair, Murky.RepairFlag);
            Assert.AreEqual(1, packets.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Murky.MissionId && packet.IsCompleteable), "MissionCompleteable once");
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, hale, Murky.MissionId, 0, Murky.Experience, Murky.Credits);
            Assert.IsGreaterThanOrEqualTo(Murky.BasicMedPacks, WildernessRanjaGorgeTests.Held(harness, Murky.BasicMedPack), "the med packs chosen");
            Assert.AreEqual((0U, 1U), (FC.Flag(harness, Murky.PoisonFlag), FC.Flag(harness, Murky.RepairFlag)), "one side only");
        }

        /// <summary>
        /// Prying Eyes: Daris offers the four stacks; accepted, nobody hears an objective, a marker on Ulovik on Ashen Desert. A
        /// Maji Field Fithik counts on nothing; Ulovik (the upstream row: Overseer Ulovik 9363, level 44, hostile) completes 1
        /// once and makes the mission completeable once; a reload keeps it; a second Ulovik kill changes nothing. A full bag
        /// waits, a failed save pays nothing; Daris pays the stack chosen, once.
        /// </summary>
        [TestMethod]
        public void PryingEyesKillsOverseerUlovik()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var desert = AshenLeadInsTests.EnterAshen(harness, WhiteOasis);
            var daris = Npc(desert, W.DarisLinier);
            var offer = WildernessRanjaGorgeTests.Offer(harness, daris, Prying.MissionId);
            CollectionAssert.AreEqual(new[] { Prying.EmpGrenade, Prying.ResTraumaKit, Prying.ConcussionGrenade, Prying.BasicMedPack },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Accept(harness, daris, Prying.MissionId);
            Assert.IsEmpty(Topics(harness, daris, Prying.MissionId), "Daris hears no objective");
            var marker = harness.Manager.BuildStatusSnapshot(harness.Client.Player)[Prying.MissionId].ObjectivesList.Single().IndicatorList.Single();
            Assert.AreEqual(AshenMissionDataV1.MapContextId, marker.MapContextId, "on Ashen Desert");

            var log = new List<object>();
            Kill(harness, desert, Fithik, MajiFithikPool);
            log.AddRange(harness.Drain());
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Prying.MissionId, Prying.Kill), "a Fithik counts on nothing");
            var ulovik = Kill(harness, desert, Prying.Ulovik, Prying.Ulovik);
            Assert.AreEqual((9363U, 44U, TargetCategory.Hostile), (ulovik.NameId, ulovik.Level, ulovik.TargetCategory), "Overseer Ulovik, as upstream");
            Assert.IsLessThanOrEqualTo((float)Prying.MarkerRadius, Vector2.Distance(new(marker.Position.X, marker.Position.Z), new(ulovik.Position.X, ulovik.Position.Z)), "inside his marker");
            log.AddRange(harness.Drain());
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Prying.MissionId, Prying.Kill), "Ulovik dead");
            AshenRuntime.Reload(harness);
            log.AddRange(harness.Drain());
            Assert.IsTrue(harness.Client.Player.Missions[Prying.MissionId].Completeable, "kept at a reload");
            Kill(harness, desert, Prying.Ulovik, Prying.Ulovik);
            log.AddRange(harness.Drain());
            Assert.AreEqual(1, log.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Prying.MissionId), "1 once");
            Assert.AreEqual(1, log.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Prying.MissionId && packet.IsCompleteable), "MissionCompleteable once");

            FullBagWaits(harness, daris, Prying.MissionId, 2, ConsumableFiller);
            FailedTurnInPaysNothing(harness, daris, Prying.MissionId, 2);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, daris, Prying.MissionId, 2, Prying.Experience, Prying.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Prying.MissionId), "MissionRewarded once");
            Assert.IsGreaterThanOrEqualTo(Prying.ConcussionGrenades, WildernessRanjaGorgeTests.Held(harness, Prying.ConcussionGrenade), "the grenades chosen");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, daris, Prying.MissionId), "done once");
        }

        /// <summary>
        /// Gas Shortages: Fontecilla offers the four stacks (not Daris); a bag with no room for the charges refuses the accept (no
        /// charge, no pump); with one slot free, three charges at the accept. An abandon after one pump takes the charges and the
        /// pumps; taken again, three charges and three whole pumps, a marker on each, nobody hears an objective. The pumps
        /// cannot be used, aimed at by the server or hit; each device takes one charge, plants it and blows its pump, completing
        /// its own objective once (3, 1, 2); a used device takes no second; a reload keeps the blown pump; the last makes the
        /// mission completeable once, no charge is left. Daris cannot take it; a full bag waits, a failed save pays nothing;
        /// Fontecilla pays the stack chosen, once.
        /// </summary>
        [TestMethod]
        public void GasShortagesBlowsTheThreeFuelPumps()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var desert = AshenLeadInsTests.EnterAshen(harness, WhiteOasis);
            var fontecilla = Npc(desert, W.Fontecilla);
            var daris = Npc(desert, W.DarisLinier);
            var offer = WildernessRanjaGorgeTests.Offer(harness, fontecilla, Gas.MissionId);
            CollectionAssert.AreEqual(new[] { Gas.EmpGrenade, Gas.ResTraumaKit, Gas.ConcussionGrenade, Gas.BasicMedPack },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, daris, Gas.MissionId), "not Daris' to give");

            WildernessAliaBranchesTests.FillCategory(harness, MissionItemFiller, 50);
            harness.MoveTo(fontecilla.Position);
            harness.Drain();
            Assert.IsFalse(harness.Manager.AcceptOfferedMission(harness.Client, fontecilla.EntityId, Gas.MissionId), "no room for the charges");
            harness.Manager.Scenes.Tick(desert);
            Assert.AreEqual(0U, Charges(harness), "none given");
            Assert.IsFalse(desert.DynamicObjects.Any(obj => obj.SceneMissionId == Gas.MissionId && obj.IsInWorld), "no pump");
            WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);

            Accept(harness, fontecilla, Gas.MissionId);
            Assert.AreEqual(Gas.Pumps, Charges(harness), "three charges at the accept");
            var (_, devices) = Pumps(harness, desert);
            Plant(harness, desert, devices[0]);
            Assert.AreEqual((1, Gas.Pumps - 1), (PumpsDone(harness), Charges(harness)));
            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Gas.MissionId));
            harness.Manager.Scenes.Tick(desert);
            harness.Drain();
            Assert.AreEqual(0U, Charges(harness), "the charges go with the mission");
            Assert.IsFalse(desert.DynamicObjects.Any(obj => obj.SceneMissionId == Gas.MissionId && obj.IsInWorld), "the pumps go with the abandon");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, fontecilla, Gas.MissionId), "offered again after an abandon");

            Accept(harness, fontecilla, Gas.MissionId);
            Assert.AreEqual(Gas.Pumps, Charges(harness), "three again");
            Assert.IsEmpty(Topics(harness, fontecilla, Gas.MissionId), "Fontecilla hears no objective");
            var snapshot = harness.Manager.BuildStatusSnapshot(harness.Client.Player)[Gas.MissionId].ObjectivesList;
            Assert.HasCount((int)Gas.Pumps, snapshot);
            foreach (var objective in snapshot)
            {
                var marker = objective.IndicatorList.Single();
                var at = Gas.PumpsAt[objective.ObjectiveId - 1];
                Assert.IsLessThan(0.1f, Vector2.Distance(new(marker.Position.X, marker.Position.Z), new(at.X, at.Z)), $"objective {objective.ObjectiveId}'s marker on its pump");
                Assert.AreEqual(AshenMissionDataV1.MapContextId, marker.MapContextId, "on Ashen Desert");
            }

            var (pumps, pumpDevices) = Pumps(harness, desert);
            Assert.AreEqual(0, PumpsDone(harness), "all open again");
            foreach (var pump in pumps)
            {
                Assert.IsFalse(pump.IsEnabled, $"{pump.SceneActorRole}: no use");
                Assert.IsFalse(PracticeTargetManager.TryGetTarget(desert, pump.EntityId, out _), $"{pump.SceneActorRole}: no gun destroys it");
                Assert.IsFalse(WorldDestructibles.TryGetTarget(desert, pump.EntityId, out _), $"{pump.SceneActorRole}: no world destructible");
            }
            var log = new List<object>();
            var blown = 0U;
            foreach (var number in new[] { 3, 1, 2 })
            {
                var device = pumpDevices[number - 1];
                harness.MoveTo(device.Position + new Vector3(0, 0, 1));
                harness.Drain();
                Use(harness, desert, device, ClientDeviceUseArgument, recovers: true);
                var packets = harness.Drain().Cast<object>().Concat(E.Tick(harness, desert, 250)).ToList();
                log.AddRange(packets);
                blown++;
                Assert.AreEqual(Gas.Pumps - blown, Charges(harness), $"pump {number} takes one charge");
                Assert.AreEqual(((UseObjectState)DevicePlanted, false), (device.StateId, device.IsEnabled), $"device {number} planted and closed");
                Assert.AreEqual((UseObjectState)Destroyed, pumps[number - 1].StateId, $"pump {number} blown up");
                Assert.AreEqual(1, packets.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Gas.MissionId && packet.ObjectiveId == Gas.Destroy(number)),
                    $"pump {number}: its objective, once");
                Assert.AreEqual((int)blown, PumpsDone(harness), $"pump {number}: only its own objective");
                if (number == 3)
                {
                    TryUse(harness, desert, device);
                    Assert.AreEqual(Gas.Pumps - 1, Charges(harness), "a used device takes no second charge");
                    P.ReloadAndRecover(harness, desert);
                    Assert.AreEqual((1, Gas.Pumps - 1), (PumpsDone(harness), Charges(harness)), "kept at a reload");
                    (pumps, pumpDevices) = Pumps(harness, desert, blown: new[] { 3 });
                }
                if (blown < Gas.Pumps)
                    Assert.IsFalse(harness.Client.Player.Missions[Gas.MissionId].Completeable, $"{blown} of 3: not completeable");
            }
            Assert.AreEqual(0U, Charges(harness), "no charge left");
            Assert.IsTrue(harness.Client.Player.Missions[Gas.MissionId].Completeable, "three pumps");
            Assert.AreEqual(1, log.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Gas.MissionId && packet.IsCompleteable), "MissionCompleteable once");

            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, daris.EntityId, Gas.MissionId, 3), "not Daris' to take");
            FullBagWaits(harness, fontecilla, Gas.MissionId, 3, ConsumableFiller);
            FailedTurnInPaysNothing(harness, fontecilla, Gas.MissionId, 3);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, fontecilla, Gas.MissionId, 3, Gas.Experience, Gas.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Gas.MissionId), "MissionRewarded once");
            Assert.IsGreaterThanOrEqualTo(Gas.BasicMedPacks, WildernessRanjaGorgeTests.Held(harness, Gas.BasicMedPack), "the med packs chosen");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, fontecilla, Gas.MissionId), "done once");
        }

        /// <summary>
        /// The new and moved NPCs: Meeks and Hale copy their World DB rows (Harris', Emmons'), friendly, with their names and
        /// packages; Fontecilla where this set put her, off the White Oasis control point's model (where upstream had her: its
        /// console's spot) and her row as upstream's (no package); each comes down at its point.
        /// </summary>
        [TestMethod]
        public void TheWhiteOasisNpcsStandOffTheControlPoint()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            var point = world.ControlPointEntries.AsNoTracking().Single(row => row.Id == WhiteOasisPoint);
            float Outside(double x, double z) => new Vector2(MathF.Max(0, MathF.Abs((float)(x - point.PosX)) - PointHalfX),
                MathF.Max(0, MathF.Abs((float)(z - point.PosZ)) - PointHalfZ)).Length();
            var fontecilla = world.SpawnPoolEntries.AsNoTracking().Single(row => row.Id == W.Fontecilla);
            Assert.IsLessThan(0.01, Math.Abs(fontecilla.PosX - W.FontecillaAt.X) + Math.Abs(fontecilla.PosY - W.FontecillaAt.Y) + Math.Abs(fontecilla.PosZ - W.FontecillaAt.Z), $"Fontecilla at {W.FontecillaAt}");
            Assert.IsLessThan(0.5f, Outside(W.FontecillaBefore.X, W.FontecillaBefore.Z), "where upstream had her: on the control point's console");
            foreach (var (id, at) in new[] { (W.Fontecilla, W.FontecillaAt), (W.Meeks, W.MeeksAt), (W.Hale, W.HaleAt) })
                Assert.IsGreaterThanOrEqualTo(NpcClear, Outside(at.X, at.Z), $"{id} off the control point's model");
            var row = world.CreatureEntries.AsNoTracking().Single(entry => entry.Id == W.Fontecilla);
            Assert.AreEqual((9474U, 1U), (row.NameId, row.Faction), "Dania Fontecilla, friendly");
            Assert.IsFalse(world.NpcPackageEntries.AsNoTracking().Any(entry => entry.Id == W.Fontecilla), "no package (she gives by id)");

            var creatures = world.CreatureEntries.AsNoTracking().ToDictionary(entry => entry.Id);
            foreach (var (id, _, _, copy, name, level, package, _) in W.New)
            {
                var npc = creatures[id];
                var source = creatures[copy];
                Assert.AreEqual((source.ClassId, name, level, 1U, W.HitPoints), (npc.ClassId, npc.NameId, npc.Level, npc.Faction, npc.MaxHitPoints), $"{id} copies {copy}");
                Assert.AreEqual(package, world.NpcPackageEntries.AsNoTracking().Single(entry => entry.Id == id).PackageId, $"{id}'s package");
                CollectionAssert.AreEqual(world.CreatureAppearanceEntries.AsNoTracking().Where(entry => entry.Id == copy).ToList().Select(entry => (entry.SlotId, entry.ClassId, entry.Color)).ToList(),
                    world.CreatureAppearanceEntries.AsNoTracking().Where(entry => entry.Id == id).ToList().Select(entry => (entry.SlotId, entry.ClassId, entry.Color)).ToList(), $"{id} looks like {copy}");
            }

            var desert = AshenLeadInsTests.EnterAshen(harness, WhiteOasis);
            foreach (var id in new[] { W.Fontecilla, W.Meeks, W.Hale })
            {
                var npc = Npc(desert, id);
                var at = W.At(id);
                Assert.IsLessThan(1f, Vector2.Distance(new(npc.Position.X, npc.Position.Z), new((float)at.X, (float)at.Z)), $"{id} stands at its point");
            }
        }

        /// <summary>
        /// The White Oasis control point holds its givers (ControlPointServices, the existing rule): with point 17 loaded from the
        /// World DB and held by the AFS, Daris, Fontecilla, Meeks and Hale stand and offer; the Bane take it, all four go (their
        /// pools closed: nothing spawns, nothing is offered); the AFS take it back, all four stand again and offer.
        /// </summary>
        [TestMethod]
        public void TheGiversServeOnlyWhileTheAfsHoldWhiteOasis()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            AshenRuntime.SeedCompleted(harness, Harvesters.MissionId);
            var desert = AshenLeadInsTests.EnterAshen(harness, WhiteOasis);
            var givers = new[] { (W.DarisLinier, Prying.MissionId), (W.Fontecilla, Gas.MissionId), (W.Meeks, Murky.MissionId), (W.Hale, 0U) };
            var pools = givers.Select(giver => giver.Item1).ToArray();
            var points = ControlPoints.Instance;
            var config = points.Config;
            try
            {
                points.Config = new ControlPointConfig();
                points.Load(harness.World.ControlPointEntries.AsNoTracking().Where(row => row.Id == WhiteOasisPoint).ToList(),
                    harness.World.ControlPointLinkEntries.AsNoTracking().Where(row => row.ControlPointId == WhiteOasisPoint).ToList(), null);
                points.Place(desert);
                var white = points.ById(WhiteOasisPoint);
                Assert.IsNotNull(white, "control point 17 loaded");
                points.SetOwner(white, ControlPoints.Afs, null);
                Respawn(desert, pools);
                foreach (var (pool, mission) in givers)
                {
                    var npc = RequireNpc(desert, pool);
                    Assert.AreEqual(WhiteOasisPoint, desert.SpawnPools.Single(loaded => loaded.DbId == pool).ServiceControlPointId, $"{pool} served by point 17");
                    if (mission != 0)
                        Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, npc, mission), $"AFS: {pool} offers {mission}");
                }

                points.SetOwner(white, ControlPoints.Bane, null);
                Respawn(desert, pools);
                foreach (var (pool, _) in givers)
                {
                    Assert.IsTrue(desert.SpawnPools.Single(loaded => loaded.DbId == pool).ServiceSuspended, $"Bane: {pool} closed");
                    Assert.IsFalse(Alive(desert).Any(creature => creature.SpawnPool?.DbId == pool), $"Bane: {pool} gone");
                }

                points.SetOwner(white, ControlPoints.Afs, null);
                Respawn(desert, pools);
                foreach (var (pool, mission) in givers)
                {
                    var npc = RequireNpc(desert, pool);
                    if (mission != 0)
                        Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, npc, mission), $"AFS again: {pool} offers {mission}");
                }
            }
            finally
            {
                points.Load(new List<ControlPointEntry>(), new List<ControlPointLinkEntry>(), null);
                points.Config = config;
            }
        }

        /// <summary>
        /// The set's objects stand where a player uses them: each panel, pump and device on the navmesh ground and the client's
        /// floor (within 1 m), seen (eye to its middle, nothing of the placed meshes between) from 16 of 16 places 2 m round it;
        /// each pump on flat ground over its whole model (heights within 0.5 m of its point); each pump's model and each device
        /// PadClear (2 m) or more outside every Sheitan landing pad's model as the client places it (its own rotation, the
        /// footprint of all its vertices across x and z: the garrison's dropship lands there, the deck stands 3 m up); each
        /// device 3 m or more outside every pump's model, inside its objective's marker.
        /// </summary>
        [TestMethod]
        public void ThePanelsAndPumpsStandWherePlayersUseThem()
        {
            var dataDir = F.ClientData();
            using var harness = WildernessRuntimeTestHarness.Create();
            var desert = AshenLeadInsTests.EnterAshen(harness, WhiteOasis);
            var nav = desert.NavMesh;
            using var meshes = new MeshLibrary(dataDir, F.MeshCsv());
            var geometry = MapGeometry.Load(Path.Combine(dataDir, "maps", harness.World.MapInfoEntries.AsNoTracking()
                .Single(map => map.Id == AshenMissionDataV1.MapContextId).MapName), meshes, 1);
            Accept(harness, Npc(desert, W.DarisLinier), Harvesters.MissionId);
            Accept(harness, Npc(desert, W.Fontecilla), Gas.MissionId);
            var panels = Panels(harness, desert);
            var (pumps, devices) = Pumps(harness, desert);
            var markers = harness.World.MissionIndicatorEntries.AsEnumerable().Where(row => row.ContentRevision == AshenMissionDataV1.Revision && row.MissionId == Gas.MissionId).ToList();
            float Outside(Vector2 centre, float halfX, float halfZ, Vector2 at) =>
                new Vector2(MathF.Max(0, MathF.Abs(at.X - centre.X) - halfX), MathF.Max(0, MathF.Abs(at.Y - centre.Y) - halfZ)).Length();
            var pads = Pads(harness, dataDir, meshes);
            Assert.HasCount(3, pads, "the Sheitan Embankment's three landing pads");
            var failures = new List<string>();
            foreach (var obj in panels.Concat(pumps).Concat(devices))
            {
                var at = obj.Position;
                var near = F.Near(geometry, new List<Vector3> { at }, F.Ring + 2)[0];
                if (F.FloorAt(near, at, at.Y - 2, at.Y + 1.5f) is not { } floor || MathF.Abs(floor - at.Y) > 1)
                    failures.Add($"{obj.SceneActorRole}: no client floor under it");
                var seen = Enumerable.Range(0, F.RingPlaces).Count(k =>
                {
                    var angle = k * 2 * MathF.PI / F.RingPlaces;
                    var stand = at + new Vector3(MathF.Sin(angle) * F.Ring, 0, MathF.Cos(angle) * F.Ring);
                    return F.FloorAt(near, stand, at.Y - F.MaxOver, at.Y + F.MaxUnder) is { } y && F.Clear(near, stand with { Y = y + F.Eye }, at + new Vector3(0, F.ObjectMiddle, 0));
                });
                if (seen < SeenAtLeast)
                    failures.Add($"{obj.SceneActorRole}: seen from {seen} of {F.RingPlaces}");
            }
            for (var number = 1; number <= Gas.Pumps; number++)
            {
                var pump = pumps[number - 1].Position;
                var centre = new Vector2(pump.X, pump.Z);
                for (var x = -PumpHalfX; x <= PumpHalfX + 0.01f; x += PumpHalfX / 4)
                    for (var z = -PumpHalfZ; z <= PumpHalfZ + 0.01f; z += PumpHalfZ / 3)
                        if (nav.GroundHeight(new Vector3(pump.X + x, pump.Y + 1, pump.Z + z)) is not { } y || MathF.Abs(y - pump.Y) > 0.5f)
                            failures.Add(FormattableString.Invariant($"pump {number}'s model over uneven ground at ({pump.X + x:0.0}, {pump.Z + z:0.0})"));
                var pumpModel = Hull(meshes.GetForClass((int)ClientPumpClass).Vertices.Select(v => new Vector2(pump.X + v.X, pump.Z + v.Z)));
                var device = devices[number - 1].Position;
                foreach (var (padAt, pad) in pads)
                {
                    var gap = Gap(pumpModel, pad);
                    if (gap < PadClear)
                        failures.Add(FormattableString.Invariant($"pump {number}'s model {gap:0.0} m off the pad at ({padAt.X:0.0}, {padAt.Z:0.0})"));
                    var deviceGap = Gap(new List<Vector2> { new(device.X, device.Z) }, pad);
                    if (deviceGap < PadClear)
                        failures.Add(FormattableString.Invariant($"device {number} {deviceGap:0.0} m off the pad at ({padAt.X:0.0}, {padAt.Z:0.0})"));
                }
                for (var other = 0; other < Gas.Pumps; other++)
                {
                    var gap = Outside(new(pumps[other].Position.X, pumps[other].Position.Z), PumpHalfX, PumpHalfZ, new(device.X, device.Z));
                    if (gap < DeviceClear)
                        failures.Add(FormattableString.Invariant($"device {number} {gap:0.0} m outside pump {other + 1}'s model"));
                }
                var marker = markers.Single(row => row.ObjectiveId == Gas.Destroy(number));
                if (Vector2.Distance(new((float)marker.PosX, (float)marker.PosZ), new(devices[number - 1].Position.X, devices[number - 1].Position.Z)) > marker.Radius)
                    failures.Add($"device {number} outside its marker");
            }
            Assert.IsEmpty(failures, string.Join("; ", failures));
        }

        /// <summary>
        /// The walks on the client's floor (AshenFixBTests.ClientFloorLine with a player's half width: every half metre, never
        /// more than 1.1 m up over a metre, nothing of the placed meshes in the way at knee, chest or head height), there and
        /// back, from the White Oasis Post waypoint to every place of the set (Places): Meeks, Hale, Fontecilla, the four panels,
        /// Ulovik and the three pump devices, by the corners Places names where the navmesh's straight walk takes a step the
        /// client's floor does not or passes a camp (the probe of 2026-11-10; ESTIMATE of a player's line). Every walk keeps
        /// HostileClearance (30 m, 2D) outside the circle of every automatic hostile Ashen Desert pool that is not its mission's
        /// own (Places names those: Ulovik's own pool, the Embankment's garrison 531032 the pumps stand in).
        /// </summary>
        [TestMethod]
        public void TheWhiteOasisPlacesAreWalkedToOnTheClientsFloorAwayFromOtherCamps()
        {
            var dataDir = F.ClientData();
            using var harness = WildernessRuntimeTestHarness.Create();
            var desert = AshenLeadInsTests.EnterAshen(harness);
            var nav = desert.NavMesh;
            using var meshes = new MeshLibrary(dataDir, F.MeshCsv());
            var geometry = MapGeometry.Load(Path.Combine(dataDir, "maps", harness.World.MapInfoEntries.AsNoTracking()
                .Single(map => map.Id == AshenMissionDataV1.MapContextId).MapName), meshes, 1);
            var hostile = HostilePools(harness);
            Assert.IsGreaterThan(50, hostile.Count, "the Ashen hostile pools are read");
            var failures = new List<string>();
            var walked = 0;
            foreach (var (label, target, way, own) in Places)
            {
                var at = nav.Nearest(target).Value;
                foreach (var (legs, direction) in new[] { (way.Prepend(WhiteOasis).Append(at).ToList(), "there"), (Enumerable.Reverse(way).Prepend(at).Append(WhiteOasis).ToList(), "back") })
                {
                    var line = new List<Vector3>();
                    for (var leg = 0; leg + 1 < legs.Count; leg++)
                    {
                        walked++;
                        if (AshenDesignerHidesTests.Walk(nav, geometry, legs[leg], legs[leg + 1]) is { } why)
                            failures.Add(FormattableString.Invariant($"{label} {direction}, leg {leg + 1} ({legs[leg].X:0.0}, {legs[leg].Z:0.0}) to ({legs[leg + 1].X:0.0}, {legs[leg + 1].Z:0.0}): {why}"));
                        var start = nav.Nearest(legs[leg]) ?? legs[leg];
                        var path = nav.FindPath(start, nav.Nearest(legs[leg + 1]) ?? legs[leg + 1], out _);
                        if (path != null)
                            line.AddRange(path.Prepend(start));
                    }
                    if (Clearance(line, hostile.Where(row => !own.Contains(row.Id)).ToList()) is { } close)
                        failures.Add(FormattableString.Invariant($"{label} {direction}: {close.Gap:0.0} m outside hostile pool {close.Pool}'s circle at ({close.At.X:0.0}, {close.At.Y:0.0})"));
                }
            }
            Assert.IsGreaterThanOrEqualTo(2 * Places.Length, walked, "the legs are walked");
            Assert.IsEmpty(failures, $"{failures.Count}: " + string.Join("; ", failures));
        }

        /// <summary>The Bane landing pads (class 20750) the client places on Ashen Desert within 150 m of the Embankment garrison: each one's point and the convex footprint (x, z) of its model placed as the client places it (position, rotation, scale, as MapGeometry).</summary>
        private static List<(Vector3 At, List<Vector2> Footprint)> Pads(WildernessRuntimeTestHarness harness, string dataDir, MeshLibrary meshes)
        {
            var mapName = harness.World.MapInfoEntries.AsNoTracking().Single(map => map.Id == AshenMissionDataV1.MapContextId).MapName;
            var map = MapFile.Load(Path.Combine(dataDir, "maps", mapName, mapName + ".map"));
            var mesh = meshes.GetForClass(ClientPadClass);
            Assert.IsNotNull(mesh, "the pad's client model");
            var garrison = harness.World.SpawnPoolEntries.AsNoTracking().Single(row => row.Id == Gas.EmbankmentPool);
            return map.Entities.Where(entity => entity.ClassId == ClientPadClass &&
                    Vector2.Distance(new(entity.Position.X, entity.Position.Z), new((float)garrison.PosX, (float)garrison.PosZ)) < 150)
                .Select(entity => (entity.Position, Hull(mesh.Vertices.Select(v => Vector3.Transform(v * (entity.Scale > 0 ? entity.Scale : 1f), entity.Rotation) + entity.Position)
                    .Select(v => new Vector2(v.X, v.Z))))).ToList();
        }

        /// <summary>The convex hull of points (Andrew's monotone chain), counter-clockwise.</summary>
        private static List<Vector2> Hull(IEnumerable<Vector2> points)
        {
            var sorted = points.Distinct().OrderBy(p => p.X).ThenBy(p => p.Y).ToList();
            if (sorted.Count < 3)
                return sorted;
            static float Cross(Vector2 o, Vector2 a, Vector2 b) => (a.X - o.X) * (b.Y - o.Y) - (a.Y - o.Y) * (b.X - o.X);
            var hull = new List<Vector2>();
            foreach (var pass in new[] { sorted, Enumerable.Reverse(sorted).ToList() })
            {
                var start = hull.Count;
                foreach (var p in pass)
                {
                    while (hull.Count >= start + 2 && Cross(hull[^2], hull[^1], p) <= 0)
                        hull.RemoveAt(hull.Count - 1);
                    hull.Add(p);
                }
                hull.RemoveAt(hull.Count - 1);
            }
            return hull;
        }

        /// <summary>The distance (x, z) between a point or a convex hull and a convex footprint (both counter-clockwise): 0 when they overlap.</summary>
        private static float Gap(List<Vector2> points, List<Vector2> footprint)
        {
            static bool In(List<Vector2> hull, Vector2 p) => hull.Count >= 3 && Enumerable.Range(0, hull.Count).All(i =>
            {
                var a = hull[i];
                var b = hull[(i + 1) % hull.Count];
                return (b.X - a.X) * (p.Y - a.Y) - (b.Y - a.Y) * (p.X - a.X) >= 0;
            });
            static float ToSegment(Vector2 p, Vector2 a, Vector2 b)
            {
                var ab = b - a;
                var t = ab.LengthSquared() == 0 ? 0 : Math.Clamp(Vector2.Dot(p - a, ab) / ab.LengthSquared(), 0, 1);
                return Vector2.Distance(p, a + t * ab);
            }
            static float Edges(List<Vector2> from, List<Vector2> polygon) => from.Min(p => polygon.Count == 1 ? Vector2.Distance(p, polygon[0])
                : Enumerable.Range(0, polygon.Count).Min(i => ToSegment(p, polygon[i], polygon[(i + 1) % polygon.Count])));
            if (points.Any(p => In(footprint, p)) || footprint.Any(p => In(points, p)))
                return 0;
            return MathF.Min(Edges(points, footprint), Edges(footprint, points));
        }

        /// <summary>The automatic (mode 0) Ashen Desert pools that hold a hostile creature (World DB).</summary>
        internal static List<SpawnPoolEntry> HostilePools(WildernessRuntimeTestHarness harness) =>
            harness.World.SpawnPoolEntries.AsNoTracking().Where(row => row.MapContextId == AshenMissionDataV1.MapContextId && row.Mode == 0).AsEnumerable()
                .Where(row => new[] { row.Creature1Id, row.Creature2Id, row.Creature3Id, row.Creature4Id, row.Creature5Id, row.Creature6Id }
                    .Any(id => id != 0 && CreatureManager.Instance.LoadedCreatures.TryGetValue(id, out var creature) && creature.TargetCategory == TargetCategory.Hostile))
                .ToList();

        /// <summary>The nearest a walk (its line, sampled every metre) comes to a pool's circle (2D, centre less radius), when nearer than HostileClearance; null otherwise.</summary>
        internal static (uint Pool, float Gap, Vector2 At)? Clearance(List<Vector3> line, List<SpawnPoolEntry> pools)
        {
            (uint Pool, float Gap, Vector2 At)? worst = null;
            for (var k = 0; k + 1 < line.Count; k++)
            {
                var from = new Vector2(line[k].X, line[k].Z);
                var to = new Vector2(line[k + 1].X, line[k + 1].Z);
                var length = Vector2.Distance(from, to);
                for (var s = 0f; s <= length; s += 1)
                {
                    var at = length == 0 ? from : Vector2.Lerp(from, to, s / length);
                    foreach (var pool in pools)
                    {
                        var gap = Vector2.Distance(at, new((float)pool.PosX, (float)pool.PosZ)) - (float)pool.Radius;
                        if (gap < HostileClearance && (worst == null || gap < worst.Value.Gap))
                            worst = (pool.Id, gap, at);
                    }
                }
            }
            return worst;
        }

        private static uint Tool(WildernessRuntimeTestHarness harness) => WildernessRanjaGorgeTests.Held(harness, ClientToolTemplate);

        private static uint Charges(WildernessRuntimeTestHarness harness) => WildernessRanjaGorgeTests.Held(harness, ClientChargeTemplate);

        private static int Analyzed(WildernessRuntimeTestHarness harness) =>
            Enumerable.Range(1, Harvesters.PanelsAt.Length).Count(number => Objective(harness, Harvesters.MissionId, (uint)number) == MissionObjectiveState.Completed);

        private static int PumpsDone(WildernessRuntimeTestHarness harness) =>
            Enumerable.Range(1, (int)Gas.Pumps).Count(number => Objective(harness, Gas.MissionId, Gas.Destroy(number)) == MissionObjectiveState.Completed);

        private static DynamicObject Actor(MapChannel desert, uint mission, string role) =>
            desert.DynamicObjects.Single(obj => obj.SceneMissionId == mission && obj.SceneActorRole == role && obj.IsInWorld);

        /// <summary>The four panels: the player's own, the client's monitor named "Harvester Control Panel", at their places, on the ground; off and open (or, for <paramref name="done"/>, on and closed), with the client's use argument.</summary>
        private static DynamicObject[] Panels(WildernessRuntimeTestHarness harness, MapChannel desert, int[] done = null)
        {
            harness.Manager.Scenes.Tick(desert);
            var panels = new DynamicObject[Harvesters.PanelsAt.Length];
            for (var number = 1; number <= panels.Length; number++)
            {
                var isDone = done?.Contains(number) == true;
                var panel = Actor(desert, Harvesters.MissionId, Harvesters.PanelRole(number));
                Assert.AreEqual((ClientPanelClass, ClientPanelName, P.ToVector(Harvesters.PanelsAt[number - 1]), ClientPanelUseArgument),
                    ((uint)panel.EntityClassId, panel.NameOverrideId, panel.Position, panel.MissionUseAction.ActionArgId), $"panel {number}");
                Assert.AreEqual((isDone ? (UseObjectState)PanelOn : (UseObjectState)PanelOff, !isDone), (panel.StateId, panel.IsEnabled), $"panel {number} state");
                Assert.AreEqual(harness.Client.Player.Id, panel.SceneOwnerCharacterId, "the player's own");
                AssertOnGround(desert, panel.Position, panel.SceneActorRole);
                panels[number - 1] = panel;
            }
            return panels;
        }

        /// <summary>The three pumps and their devices: the player's own, of the client's classes, at their places, on the ground; a pump whole (or blown) and never interactable; its device ready and open (or planted and closed).</summary>
        private static (DynamicObject[] Pumps, DynamicObject[] Devices) Pumps(WildernessRuntimeTestHarness harness, MapChannel desert, int[] blown = null)
        {
            harness.Manager.Scenes.Tick(desert);
            var pumps = new DynamicObject[Gas.Pumps];
            var devices = new DynamicObject[Gas.Pumps];
            for (var number = 1; number <= Gas.Pumps; number++)
            {
                var isBlown = blown?.Contains(number) == true;
                var pump = Actor(desert, Gas.MissionId, Gas.PumpRole(number));
                var device = Actor(desert, Gas.MissionId, Gas.DeviceRole(number));
                Assert.AreEqual((ClientPumpClass, P.ToVector(Gas.PumpsAt[number - 1])), ((uint)pump.EntityClassId, pump.Position), $"pump {number}");
                Assert.AreEqual((isBlown ? (UseObjectState)Destroyed : (UseObjectState)Intact, false), (pump.StateId, pump.IsEnabled), $"pump {number} state");
                Assert.AreEqual((ClientDeviceClass, P.ToVector(Gas.DevicesAt[number - 1]), ClientDeviceUseArgument), ((uint)device.EntityClassId, device.Position, device.MissionUseAction.ActionArgId), $"device {number}");
                Assert.AreEqual((isBlown ? (UseObjectState)DevicePlanted : (UseObjectState)DeviceReady, !isBlown), (device.StateId, device.IsEnabled), $"device {number} state");
                foreach (var obj in new[] { pump, device })
                {
                    Assert.AreEqual(harness.Client.Player.Id, obj.SceneOwnerCharacterId, "the player's own");
                    AssertOnGround(desert, obj.Position, obj.SceneActorRole);
                }
                pumps[number - 1] = pump;
                devices[number - 1] = device;
            }
            return (pumps, devices);
        }

        /// <summary>A use of a Detonation Device by a player beside it, with its recovery and a tick.</summary>
        private static void Plant(WildernessRuntimeTestHarness harness, MapChannel desert, DynamicObject device)
        {
            harness.MoveTo(device.Position + new Vector3(0, 0, 1));
            harness.Drain();
            Use(harness, desert, device, ClientDeviceUseArgument, recovers: true);
            E.Tick(harness, desert, 250);
            harness.Drain();
        }

        /// <summary>A use the server may refuse at once or after its recovery: the caller checks that nothing came of it.</summary>
        private static void TryUse(WildernessRuntimeTestHarness harness, MapChannel desert, DynamicObject obj)
        {
            harness.MoveTo(obj.Position + new Vector3(0, 0, 1));
            harness.Drain();
            var count = desert.PerformRecovery.Count;
            harness.Objects.RequestUseObjectPacket(harness.Client, new Rasa.Packets.MapChannel.Client.RequestUseObjectPacket
            {
                EntityId = obj.EntityId, ActionId = ActionId.UseObject, ActionArgId = obj.MissionUseAction.ActionArgId
            });
            if (desert.PerformRecovery.Count > count)
            {
                var recovery = desert.PerformRecovery.Last();
                desert.PerformRecovery.Remove(recovery);
                ActorActionManager.Instance.PerformRecovery(desert, recovery);
            }
            E.Tick(harness, desert, 250);
            harness.Drain();
        }

        /// <summary>A kill of a living creature of this row in this pool (the pool spawned or respawned first); the creature.</summary>
        private static Creature Kill(WildernessRuntimeTestHarness harness, MapChannel desert, uint creature, uint pool)
        {
            Spawn(desert, pool);
            if (!Alive(desert).Any(candidate => candidate.DbId == creature && candidate.SpawnPool?.DbId == pool))
                Respawn(desert, pool);
            var one = Alive(desert).First(candidate => candidate.DbId == creature && candidate.SpawnPool?.DbId == pool);
            harness.MoveTo(one.Position);
            one.Attributes[Attributes.Health].Current = 0;
            harness.Creatures.HandleCreatureKill(desert, one, harness.Client.Player);
            return one;
        }

        /// <summary>The choice topics of <paramref name="mission"/> that <paramref name="npc"/> offers now: objective and variant (as AshenFormalComplaintsTests).</summary>
        private static (uint, uint)[] Choices(WildernessRuntimeTestHarness harness, Creature npc, uint mission) =>
            Conversation(harness, npc).TryGetValue(ConversationType.ObjectiveChoice, out var value)
                ? ((List<ChoiceObjectives>)value).Where(topic => topic.MissionId == mission)
                    .Select(topic => ((uint)topic.ObjectiveId, (uint)topic.PlayerFlagId)).ToArray()
                : Array.Empty<(uint, uint)>();

        private static readonly uint[] Garrison = { Gas.EmbankmentPool };

        /// <summary>
        /// The places a player walks to from the White Oasis Post waypoint, the corners of the walk there (back: the same, the
        /// other way round) and the mission's own hostile pools the clearance does not count (the probe of 2026-11-10).
        /// </summary>
        internal static readonly (string Label, Vector3 Target, Vector3[] Way, uint[] Own)[] Places =
        {
            ("Meeks", P.ToVector(W.MeeksAt), Array.Empty<Vector3>(), Array.Empty<uint>()),
            ("Hale", P.ToVector(W.HaleAt), Array.Empty<Vector3>(), Array.Empty<uint>()),
            ("Fontecilla", P.ToVector(W.FontecillaAt), Array.Empty<Vector3>(), Array.Empty<uint>()),
            ("Harvester Control Panel 1", P.ToVector(Harvesters.PanelsAt[0]), Array.Empty<Vector3>(), Array.Empty<uint>()),
            ("Harvester Control Panel 2", P.ToVector(Harvesters.PanelsAt[1]), new[] { new Vector3(-305.7f, 284f, 198.1f) }, Array.Empty<uint>()),
            ("Harvester Control Panel 3", P.ToVector(Harvesters.PanelsAt[2]), new[] { new Vector3(-439.1f, 284.34f, 204.8f), new Vector3(-430f, 285f, 300f) }, Array.Empty<uint>()),
            ("Harvester Control Panel 4", P.ToVector(Harvesters.PanelsAt[3]),
                new[] { new Vector3(-439.1f, 284.34f, 204.8f), new Vector3(-430f, 285f, 300f), new Vector3(-425f, 290f, 470f) }, Array.Empty<uint>()),
            ("Overseer Ulovik", P.ToVector(Prying.UlovikAt), Array.Empty<Vector3>(), new[] { Prying.Ulovik }),
            ("Fuel Pump 1's device", P.ToVector(Gas.DevicesAt[0]), Embankment, Garrison),
            ("Fuel Pump 2's device", P.ToVector(Gas.DevicesAt[1]), Embankment, Garrison),
            ("Fuel Pump 3's device", P.ToVector(Gas.DevicesAt[2]), Embankment, Garrison)
        };

        /// <summary>The way east to the Sheitan Embankment, south of Maji Field's camps.</summary>
        private static Vector3[] Embankment => new[] { new Vector3(-260f, 283f, 200f), new Vector3(-160f, 283f, 200f), new Vector3(-100f, 283f, 180f) };
    }
}
