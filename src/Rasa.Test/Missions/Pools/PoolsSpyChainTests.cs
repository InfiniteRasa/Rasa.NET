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
    /// The spy chain, played through as a player plays it: Spy Networking (1331: Sheen sent away, then the AFS console's
    /// menu walked to the canister data, radio turn-in), and the fork between Poison Control (1368, for Amee: the three
    /// canisters destroyed) and Taking the Poison (1590, for Sheen: dispensed and collected).
    ///
    /// Why these tests: the console is a menu of 40 pages kept in a character flag, each choice a transition that leaves the
    /// objective open (both engine changes of this set); the canister consoles show one page or another as the other
    /// mission is taken or not, and a command fails the other mission. A page that does not open, a choice that goes to
    /// the wrong page or ends the mission, the console open while Sheen watches, a side that can still be taken after the
    /// other was chosen, or an item kept after an abandon would each break them.
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class PoolsSpyChainTests
    {
        private const uint Spy = PoolsSpyNetworkingV1.MissionId;
        private const uint Control = PoolsPoisonControlV1.MissionId;
        private const uint Taking = PoolsTakingThePoisonV1.MissionId;
        private const uint MissionItemFiller = 11519;      // as PlateauCampResistanceTests

        private static readonly Vector3 Gate = new(545.6f, 680f, 464f);
        private static readonly Vector3 Post = new(996f, 657.5f, 318f);

        [TestMethod]
        public void SheenWatchesUntilSentAwayThenTheMenuLeadsToTheDownload()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pools = PlateauValverdePoolsTests.OntoPools(harness);
            var amee = Npc(pools, PoolsRetreadCampNpcsV1.Amee);
            var sheen = Npc(pools, PoolsSpyNpcsV1.Sheen);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, amee, Spy), "not before Retread Planning Part II");
            SeedCompleted(harness, PoolsRetreadPlanningPartTwoV1.MissionId);

            var offer = WildernessRanjaGorgeTests.Offer(harness, amee, Spy);
            WildernessRanjaGorgeTests.AssertSelectable(offer, Spy,
                (PoolsSpyNetworkingV1.GravitonGloves, 15058), (PoolsSpyNetworkingV1.HazmatHelmet, 13970),
                (PoolsSpyNetworkingV1.MotorAssistHelmet, 16826), (PoolsSpyNetworkingV1.ReflectiveHelmet, 18764));
            Accept(harness, amee, Spy);
            Tick(harness, pools, 250);

            var console = Console(pools);
            var at = PoolsSpyNetworkingV1.ConsoleAt;
            Assert.AreEqual(PoolsSpyNetworkingV1.ConsoleClass, (uint)console.EntityClassId);
            Assert.AreEqual(new Vector3(at.X, at.Y, at.Z), console.Position);
            Assert.AreEqual(harness.Client.Player.Id, console.SceneOwnerCharacterId, "the player's own");
            AssertOnTheGround(pools, console.Position, "the console");
            AssertWalkedTo(pools, Post, console.Position, "the console");
            var standIn = StandIn(pools);
            Assert.IsNotNull(standIn, "Sheen at her console");
            Assert.AreEqual(PoolsSpyNpcsV1.StandIn, standIn.DbId);
            Assert.IsLessThan(6f, Vector3.Distance(standIn.Position, console.Position), "by the console");
            Assert.IsTrue(Living(pools).Contains(sheen), "the public Sheen stays");

            // While she watches, the console only says so.
            Assert.AreEqual((PoolsSpyNetworkingV1.WatchingObjective, PoolsSpyNetworkingV1.SheenFlag), Ambient(OpenConsole(harness, console)));
            CollectionAssert.AreEqual(new[] { (PoolsSpyNetworkingV1.Steal, PoolsSpyNetworkingV1.SheenFlag) },
                Choices(harness, standIn, Spy), "she asks what she can do");
            Assert.IsEmpty(Choices(harness, sheen, Spy), "the public Sheen has no part in it");

            // Goodbye: she stays. Sent away: she walks to the hospital and is gone.
            Choose(harness, standIn.EntityId, standIn.Position, Spy, PoolsSpyNetworkingV1.Steal, PoolsSpyNetworkingV1.SheenFlag, 2);
            Assert.IsNotNull(StandIn(pools), "goodbye: she stays");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Spy, PoolsSpyNetworkingV1.Steal));
            Choose(harness, standIn.EntityId, standIn.Position, Spy, PoolsSpyNetworkingV1.Steal, PoolsSpyNetworkingV1.SheenFlag, 1);
            Assert.AreEqual(1U, Flag(harness, PoolsSpyNetworkingV1.SheenGoneFlag));
            var start = standIn.Position;
            Tick(harness, pools, 3000);
            Assert.IsLessThan(Vector3.Distance(start, PoolsHospital), Vector3.Distance(standIn.Position, PoolsHospital), "she walks to the hospital");
            Tick(harness, pools, (int)PoolsSpyNetworkingV1.LeaveMs);
            Assert.IsNull(StandIn(pools), "and is gone");

            // The menu: Thrax Barracks, Communications, Poison Canisters; a new search; the same again and the download.
            Assert.AreEqual((PoolsSpyNetworkingV1.RootObjective, 1U), Page(OpenConsole(harness, console)), "the root");
            var page = Choose(harness, console.EntityId, console.Position, Spy, 5, 1, 2);
            Assert.AreEqual((7U, 317U), Page(page), "Thrax Barracks");
            page = Choose(harness, console.EntityId, console.Position, Spy, 7, 317, 2);
            Assert.AreEqual((23U, 320U), Page(page), "Communications");
            WildernessRanjaGorgeTests.Reload(harness);
            Assert.AreEqual(9U, Flag(harness, PoolsSpyNetworkingV1.PageFlag), "the page kept at a reload");
            harness.Drain();
            Assert.AreEqual((23U, 320U), Page(OpenConsole(harness, console)), "the same page after it");
            page = Choose(harness, console.EntityId, console.Position, Spy, 23, 320, 3);
            Assert.AreEqual((18U, 324U), Page(page), "Poison Canisters");
            page = Choose(harness, console.EntityId, console.Position, Spy, 18, 324, 1);
            Assert.AreEqual((5U, 1U), Page(page), "new search: the root");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Spy, PoolsSpyNetworkingV1.Steal), "no choice ends it");
            foreach (var (dialog, flag, choice) in new[] { (5U, 1U, 2), (7U, 317U, 2), (23U, 320U, 3) })
                Choose(harness, console.EntityId, console.Position, Spy, dialog, flag, choice);
            Choose(harness, console.EntityId, console.Position, Spy, 18, 324, 2);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Spy, PoolsSpyNetworkingV1.Steal), "downloaded");
            Assert.IsTrue(harness.Client.Player.Missions[Spy].Completeable);

            // Amee radios in.
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, amee, Spy));
            Assert.IsTrue(harness.Manager.TryCompleteRadioMission(harness.Client, Spy, 1, null));
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Spy), "paid once");
            Assert.IsFalse(harness.Manager.TryCompleteRadioMission(harness.Client, Spy, 1, null), "and only once");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PoolsSpyNetworkingV1.HazmatHelmet), "the selected helmet");
        }

        /// <summary>Every page opens from its parent with its own text, and every leaf's first choice goes back to the root.</summary>
        [TestMethod]
        public void EveryPageOpensAndEveryLeafLeadsBackToTheRoot()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pools = PlateauValverdePoolsTests.OntoPools(harness);
            var console = SpyUnderWay(harness, pools);
            var pages = PoolsSpyNetworkingV1.AllPages().ToDictionary(page => page.Code);
            Assert.HasCount((int)PoolsSpyNetworkingV1.Pages, pages);

            var reached = new HashSet<uint> { PoolsSpyNetworkingV1.Root };
            var root = pages[PoolsSpyNetworkingV1.Root];
            for (var first = 0; first < 3; first++)
            {
                var menu = pages[root.Choices[first]];
                for (var second = 0; second < 3; second++)
                {
                    var sub = pages[menu.Choices[second]];
                    for (var third = 0; third < 3; third++)
                    {
                        var leaf = pages[sub.Choices[third]];
                        var shown = Choose(harness, console.EntityId, console.Position, Spy, root.DialogObjective, root.Flag, first + 1);
                        Assert.AreEqual((menu.DialogObjective, menu.Flag), Page(shown), $"page {menu.Code}");
                        shown = Choose(harness, console.EntityId, console.Position, Spy, menu.DialogObjective, menu.Flag, second + 1);
                        Assert.AreEqual((sub.DialogObjective, sub.Flag), Page(shown), $"page {sub.Code}");
                        shown = Choose(harness, console.EntityId, console.Position, Spy, sub.DialogObjective, sub.Flag, third + 1);
                        Assert.AreEqual((leaf.DialogObjective, leaf.Flag), Page(shown), $"page {leaf.Code}");
                        reached.UnionWith(new[] { menu.Code, sub.Code, leaf.Code });
                        shown = Choose(harness, console.EntityId, console.Position, Spy, leaf.DialogObjective, leaf.Flag, 1);
                        Assert.AreEqual((root.DialogObjective, root.Flag), Page(shown), $"page {leaf.Code}: new search");
                    }
                }
            }
            Assert.HasCount((int)PoolsSpyNetworkingV1.Pages, reached, "all 40 pages");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Spy, PoolsSpyNetworkingV1.Steal), "no page ends it");
        }

        /// <summary>An abandon puts Sheen back at her console and the menu at its root; the flags start again.</summary>
        [TestMethod]
        public void AnAbandonPutsSheenBackAndTheMenuAtItsRoot()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pools = PlateauValverdePoolsTests.OntoPools(harness);
            var amee = Npc(pools, PoolsRetreadCampNpcsV1.Amee);
            var console = SpyUnderWay(harness, pools);
            Choose(harness, console.EntityId, console.Position, Spy, 5, 1, 3);
            Assert.AreEqual(4U, Flag(harness, PoolsSpyNetworkingV1.PageFlag));

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Spy));
            Tick(harness, pools, 250);
            Assert.IsFalse(pools.DynamicObjects.Any(obj => obj.SceneMissionId == Spy && obj.IsInWorld), "the console goes");
            Accept(harness, amee, Spy);
            Tick(harness, pools, 250);
            Assert.IsNotNull(StandIn(pools), "Sheen is back");
            Assert.AreEqual(PoolsSpyNetworkingV1.Root, Flag(harness, PoolsSpyNetworkingV1.PageFlag), "the menu at its root");
            Assert.AreEqual(0U, Flag(harness, PoolsSpyNetworkingV1.SheenGoneFlag), "Sheen not gone");
            Assert.AreEqual((PoolsSpyNetworkingV1.WatchingObjective, PoolsSpyNetworkingV1.SheenFlag), Ambient(OpenConsole(harness, Console(pools))));
        }

        /// <summary>Only Poison Control: each console has its one command; Amee pays; Sheen offers nothing after.</summary>
        [TestMethod]
        public void PoisonControlAloneDestroysTheCanistersAndClosesTheAfsSide()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pools = PlateauValverdePoolsTests.OntoPools(harness);
            var amee = Npc(pools, PoolsRetreadCampNpcsV1.Amee);
            var sheen = Npc(pools, PoolsSpyNpcsV1.Sheen);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, amee, Control), "not before Spy Networking");
            SeedCompleted(harness, Spy);
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, sheen, Taking), "Sheen offers hers");

            var offer = WildernessRanjaGorgeTests.Offer(harness, amee, Control);
            WildernessRanjaGorgeTests.AssertSelectable(offer, Control,
                (PoolsPoisonControlV1.BioBoots, 23368), (PoolsPoisonControlV1.GravitonBoots, 15022),
                (PoolsPoisonControlV1.MechBoots, 17929), (PoolsPoisonControlV1.StealthBoots, 19822));
            Accept(harness, amee, Control);
            Tick(harness, pools, 250);

            var consoles = Canisters(pools, Control);
            for (var index = 0; index < consoles.Length; index++)
            {
                var (objective, _, at) = PoolsPoisonCanistersV1.Canisters[index];
                Assert.AreEqual(PoolsPoisonCanistersV1.ConsoleClass, (uint)consoles[index].EntityClassId);
                Assert.AreEqual(new Vector3(at.X, at.Y, at.Z), consoles[index].Position);
                Assert.AreEqual(harness.Client.Player.Id, consoles[index].SceneOwnerCharacterId);
                AssertOnTheGround(pools, consoles[index].Position, $"canister {index + 1}");
                AssertWalkedTo(pools, Gate, consoles[index].Position, $"canister {index + 1}");
                Assert.AreEqual((objective, PoolsPoisonCanistersV1.SoloFlag), Page(OpenConsole(harness, consoles[index])), "its one command");
                Choose(harness, consoles[index].EntityId, consoles[index].Position, Control, objective, PoolsPoisonCanistersV1.SoloFlag, 1);
                Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Control, objective), $"canister {index + 1} destroyed");
                Assert.IsNull(OpenConsole(harness, consoles[index]), "done");
                if (index == 0)
                {
                    Assert.AreEqual(PoolsPoisonCanistersV1.Retreads, Flag(harness, PoolsPoisonCanistersV1.ForkFlag), "the Retreads' side");
                    Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, sheen, Taking), "Sheen offers it no more");
                }
            }
            Assert.IsTrue(harness.Client.Player.Missions[Control].Completeable);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, amee, Control, 2,
                PoolsPoisonControlV1.Experience, PoolsPoisonControlV1.Credits);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PoolsPoisonControlV1.MechBoots));
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, sheen, Taking), "nor after");
        }

        /// <summary>Only Taking the Poison: each console dispenses its canister; Sheen takes them and pays; Amee offers nothing after.</summary>
        [TestMethod]
        public void TakingThePoisonAloneCollectsTheCanistersAndClosesTheRetreadSide()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pools = PlateauValverdePoolsTests.OntoPools(harness);
            var amee = Npc(pools, PoolsRetreadCampNpcsV1.Amee);
            var sheen = Npc(pools, PoolsSpyNpcsV1.Sheen);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, sheen, Taking), "not before Spy Networking");
            SeedCompleted(harness, Spy);

            var offer = WildernessRanjaGorgeTests.Offer(harness, sheen, Taking);
            WildernessRanjaGorgeTests.AssertSelectable(offer, Taking,
                (PoolsTakingThePoisonV1.DebuffElectric, 29879), (PoolsTakingThePoisonV1.DebuffFire, 29879),
                (PoolsTakingThePoisonV1.DebuffIce, 29879), (PoolsTakingThePoisonV1.DebuffVirulent, 29879));
            Accept(harness, sheen, Taking);
            Tick(harness, pools, 250);

            var consoles = Canisters(pools, Taking);
            for (var index = 0; index < consoles.Length; index++)
            {
                var objective = PoolsPoisonCanistersV1.Canisters[index].Objective;
                var at = PoolsPoisonCanistersV1.ConsoleAt(index, afs: true);
                Assert.AreEqual(new Vector3(at.X, at.Y, at.Z), consoles[index].Position);
                AssertOnTheGround(pools, consoles[index].Position, $"canister {index + 1}");
                Choose(harness, consoles[index].EntityId, consoles[index].Position, Taking, objective, PoolsPoisonCanistersV1.SoloFlag, 1);
                Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Taking, objective));
                Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PoolsTakingThePoisonV1.Canisters[index].Template), $"canister {index + 1}");
                if (index == 0)
                {
                    WildernessRanjaGorgeTests.Reload(harness);
                    Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Taking, objective), "kept at a reload");
                    harness.Drain();
                    Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, amee, Control), "Amee offers it no more");
                }
            }
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, sheen, Taking, 1,
                PoolsTakingThePoisonV1.Experience, PoolsTakingThePoisonV1.Credits);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PoolsTakingThePoisonV1.DebuffFire));
            foreach (var canister in PoolsTakingThePoisonV1.Canisters)
                Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, canister.Template), "Sheen takes them");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, amee, Control), "nor after");
        }

        /// <summary>
        /// Both taken: each console shows both commands. Self Destruct on Poison Control's console completes its canister
        /// and fails Taking the Poison, whose consoles go.
        /// </summary>
        [TestMethod]
        public void WithBothTakenSelfDestructFailsTakingThePoison()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pools = PlateauValverdePoolsTests.OntoPools(harness);
            var sheen = Npc(pools, PoolsSpyNpcsV1.Sheen);
            BothTaken(harness, pools);
            var consoles = Canisters(pools, Control);
            Assert.AreEqual((1U, PoolsPoisonCanistersV1.BothFlag), Page(OpenConsole(harness, consoles[0])), "both commands");
            Assert.AreEqual((1U, PoolsPoisonCanistersV1.BothFlag), Page(OpenConsole(harness, Canisters(pools, Taking)[0])), "on hers too");

            Choose(harness, consoles[0].EntityId, consoles[0].Position, Control, 1, PoolsPoisonCanistersV1.BothFlag, 1);
            Tick(harness, pools, 250);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Control, 1));
            Assert.IsFalse(harness.Client.Player.Missions.TryGetValue(Taking, out var taking) && taking.State == MissionState.Active,
                "Taking the Poison fails");
            Assert.IsFalse(pools.DynamicObjects.Any(obj => obj.SceneMissionId == Taking && obj.IsInWorld), "its consoles go");
            Assert.AreEqual(PoolsPoisonCanistersV1.Retreads, Flag(harness, PoolsPoisonCanistersV1.ForkFlag));
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, sheen, Taking), "and is not offered again");
            Assert.AreEqual((3U, PoolsPoisonCanistersV1.SoloFlag), Page(OpenConsole(harness, consoles[1])), "the next shows its one command");
        }

        /// <summary>
        /// Both taken: Dispense on Poison Control's console completes Taking the Poison's canister (with its item) and fails
        /// Poison Control.
        /// </summary>
        [TestMethod]
        public void WithBothTakenDispenseOnAmeesConsoleFailsPoisonControl()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pools = PlateauValverdePoolsTests.OntoPools(harness);
            var amee = Npc(pools, PoolsRetreadCampNpcsV1.Amee);
            BothTaken(harness, pools);
            var console = Canisters(pools, Control)[1];

            var after = Choose(harness, console.EntityId, console.Position, Control, 3, PoolsPoisonCanistersV1.BothFlag, 2);
            Assert.IsNull(after, "the signal runs in the choice: Poison Control fails at once, its page does not show again");
            Assert.AreEqual(MissionState.Failed, harness.Client.Player.Missions[Control].State);
            Tick(harness, pools, 250);
            ChooseOnly(harness, console.EntityId, Control, 3, PoolsPoisonCanistersV1.BothFlag, 1);
            Assert.AreEqual(PoolsPoisonCanistersV1.Afs, Flag(harness, PoolsPoisonCanistersV1.ForkFlag), "a stale Self Destruct is refused");
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Taking, 3), "Taking the Poison's canister II");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PoolsTakingThePoisonV1.Canisters[1].Template));
            Assert.IsFalse(harness.Client.Player.Missions.TryGetValue(Control, out var control) && control.State == MissionState.Active,
                "Poison Control fails");
            Assert.AreEqual(PoolsPoisonCanistersV1.Afs, Flag(harness, PoolsPoisonCanistersV1.ForkFlag));
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, amee, Control), "and is not offered again");
        }

        /// <summary>Both taken: Self Destruct on Taking the Poison's console completes Poison Control's canister and fails Taking the Poison.</summary>
        [TestMethod]
        public void WithBothTakenSelfDestructOnSheensConsoleFailsTakingThePoison()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pools = PlateauValverdePoolsTests.OntoPools(harness);
            BothTaken(harness, pools);
            var console = Canisters(pools, Taking)[2];

            Choose(harness, console.EntityId, console.Position, Taking, 5, PoolsPoisonCanistersV1.BothFlag, 1);
            Tick(harness, pools, 250);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Control, 5), "Poison Control's canister III");
            Assert.IsFalse(harness.Client.Player.Missions.TryGetValue(Taking, out var taking) && taking.State == MissionState.Active,
                "Taking the Poison fails");
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, PoolsTakingThePoisonV1.Canisters[2].Template), "no canister");
            Assert.AreEqual(PoolsPoisonCanistersV1.Retreads, Flag(harness, PoolsPoisonCanistersV1.ForkFlag));
        }

        /// <summary>
        /// A full bag: a dispense waits (the choice is refused, the objective stays); with room it gives. An abandon takes the
        /// canisters but keeps the side chosen (the fork does not open again); a turn-in whose save fails pays nothing.
        /// </summary>
        [TestMethod]
        public void FullBagsAbandonsAndFailedSavesWait()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pools = PlateauValverdePoolsTests.OntoPools(harness);
            var sheen = Npc(pools, PoolsSpyNpcsV1.Sheen);
            SeedCompleted(harness, Spy);
            Accept(harness, sheen, Taking);
            Tick(harness, pools, 250);
            var consoles = Canisters(pools, Taking);

            WildernessAliaBranchesTests.FillCategory(harness, MissionItemFiller, 50);
            Choose(harness, consoles[0].EntityId, consoles[0].Position, Taking, 1, PoolsPoisonCanistersV1.SoloFlag, 1);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Taking, 1), "no room: it waits");
            Assert.AreEqual(0U, Flag(harness, PoolsPoisonCanistersV1.ForkFlag), "no side chosen");
            WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
            Choose(harness, consoles[0].EntityId, consoles[0].Position, Taking, 1, PoolsPoisonCanistersV1.SoloFlag, 1);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Taking, 1), "with room it gives");

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Taking));
            Tick(harness, pools, 250);
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, PoolsTakingThePoisonV1.Canisters[0].Template), "the canister goes");
            Assert.IsFalse(pools.DynamicObjects.Any(obj => obj.SceneMissionId == Taking && obj.IsInWorld), "the consoles go");
            Assert.AreEqual(PoolsPoisonCanistersV1.Afs, Flag(harness, PoolsPoisonCanistersV1.ForkFlag), "the side stays chosen");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, Npc(pools, PoolsRetreadCampNpcsV1.Amee), Control),
                "Amee offers hers no more");

            WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
            WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
            Accept(harness, sheen, Taking);
            Tick(harness, pools, 250);
            consoles = Canisters(pools, Taking);
            for (var index = 0; index < consoles.Length; index++)
                Choose(harness, consoles[index].EntityId, consoles[index].Position, Taking,
                    PoolsPoisonCanistersV1.Canisters[index].Objective, PoolsPoisonCanistersV1.SoloFlag, 1);
            Assert.IsTrue(harness.Client.Player.Missions[Taking].Completeable);
            FailedTurnInPaysNothing(harness, sheen, Taking, 0);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, sheen, Taking, 0,
                PoolsTakingThePoisonV1.Experience, PoolsTakingThePoisonV1.Credits);
        }

        private static readonly Vector3 PoolsHospital = new(PoolsSpyNetworkingV1.HospitalAt.X, PoolsSpyNetworkingV1.HospitalAt.Y,
            PoolsSpyNetworkingV1.HospitalAt.Z);

        /// <summary>Spy Networking accepted and Sheen sent away: the console at its root.</summary>
        private static DynamicObject SpyUnderWay(WildernessRuntimeTestHarness harness, MapChannel pools)
        {
            var amee = Npc(pools, PoolsRetreadCampNpcsV1.Amee);
            SeedCompleted(harness, PoolsRetreadPlanningPartTwoV1.MissionId);
            Accept(harness, amee, Spy);
            Tick(harness, pools, 250);
            var standIn = StandIn(pools);
            Choose(harness, standIn.EntityId, standIn.Position, Spy, PoolsSpyNetworkingV1.Steal, PoolsSpyNetworkingV1.SheenFlag, 1);
            var console = Console(pools);
            Assert.AreEqual((PoolsSpyNetworkingV1.RootObjective, 1U), Page(OpenConsole(harness, console)));
            return console;
        }

        private static void BothTaken(WildernessRuntimeTestHarness harness, MapChannel pools)
        {
            SeedCompleted(harness, Spy);
            Accept(harness, Npc(pools, PoolsRetreadCampNpcsV1.Amee), Control);
            Accept(harness, Npc(pools, PoolsSpyNpcsV1.Sheen), Taking);
            Tick(harness, pools, 250);
        }

        private static void Tick(WildernessRuntimeTestHarness harness, MapChannel pools, int milliseconds)
        {
            for (var left = milliseconds; left > 0; left -= 250)
            {
                harness.UtcNow = harness.UtcNow.AddMilliseconds(250);
                BehaviorManager.Instance.MapChannelThink(pools, 250);
                harness.Manager.Scenes.Tick(pools);
            }
            harness.Drain();
        }

        private static DynamicObject Console(MapChannel pools) =>
            pools.DynamicObjects.Single(obj => obj.SceneMissionId == Spy && obj.IsInWorld && obj.SceneActorRole == "console");

        private static DynamicObject[] Canisters(MapChannel pools, uint mission) =>
            Enumerable.Range(0, PoolsPoisonCanistersV1.Canisters.Length).Select(index => pools.DynamicObjects.Single(obj =>
                obj.SceneMissionId == mission && obj.IsInWorld && obj.SceneActorRole == PoolsPoisonCanistersV1.ConsoleRole(index))).ToArray();

        private static Creature StandIn(MapChannel pools) => Living(pools)
            .SingleOrDefault(creature => creature.SpawnPool?.SceneRunId != null && creature.SpawnPool.SceneActorRole == "sheen");

        private static ConversePacket OpenConsole(WildernessRuntimeTestHarness harness, DynamicObject console)
        {
            harness.MoveTo(console.Position + new Vector3(0, 0, 1));
            harness.Drain();
            new NpcManager(harness, harness.Manager).RequestNpcConverse(harness.Client,
                new RequestNPCConversePacket { EntityId = console.EntityId, ActionId = ActionId.Converse, ActionArgId = 1 });
            return harness.Drain().OfType<ConversePacket>().LastOrDefault(packet =>
                packet.ConvoDataDict.ContainsKey(ConversationType.ObjectiveChoice) ||
                packet.ConvoDataDict.ContainsKey(ConversationType.ObjectiveAmbient));
        }

        /// <summary>A choice; the page the console shows after it, if it shows one.</summary>
        private static ConversePacket Choose(WildernessRuntimeTestHarness harness, ulong entityId, Vector3 at, uint mission,
            uint dialogObjective, uint flag, int choice)
        {
            var npcs = new NpcManager(harness, harness.Manager);
            harness.MoveTo(at + new Vector3(0, 0, 1));
            harness.Drain();
            npcs.RequestNpcConverse(harness.Client, new RequestNPCConversePacket { EntityId = entityId, ActionId = ActionId.Converse, ActionArgId = 1 });
            harness.Drain();
            npcs.PerformNPCChoice(harness.Client, new PerformNPCChoicePacket
            {
                EntityId = entityId, MissionId = mission, ObjectiveId = dialogObjective, PlayerFlagId = flag, ChoiceIdx = choice
            });
            return harness.Drain().OfType<ConversePacket>().LastOrDefault(packet =>
                packet.ConvoDataDict.ContainsKey(ConversationType.ObjectiveChoice) ||
                packet.ConvoDataDict.ContainsKey(ConversationType.ObjectiveAmbient));
        }

        /// <summary>A choice the client sends from a page it still shows, without a new talk first.</summary>
        private static void ChooseOnly(WildernessRuntimeTestHarness harness, ulong entityId, uint mission, uint dialogObjective,
            uint flag, int choice)
        {
            new NpcManager(harness, harness.Manager).PerformNPCChoice(harness.Client, new PerformNPCChoicePacket
            {
                EntityId = entityId, MissionId = mission, ObjectiveId = dialogObjective, PlayerFlagId = flag, ChoiceIdx = choice
            });
            harness.Drain();
        }

        private static (uint, uint) Page(ConversePacket packet)
        {
            Assert.IsNotNull(packet, "a page shown");
            var page = ((List<ChoiceObjectives>)packet.ConvoDataDict[ConversationType.ObjectiveChoice]).Single();
            return ((uint)page.ObjectiveId, (uint)page.PlayerFlagId);
        }

        private static (uint, uint) Ambient(ConversePacket packet)
        {
            Assert.IsNotNull(packet, "a line shown");
            var line = ((List<AmbientObjectives>)packet.ConvoDataDict[ConversationType.ObjectiveAmbient]).Single();
            return ((uint)line.ObjectiveId, (uint)line.PlayerFlagId);
        }

        /// <summary>The choice topics of <paramref name="mission"/> that <paramref name="npc"/> offers now.</summary>
        private static (uint, uint)[] Choices(WildernessRuntimeTestHarness harness, Creature npc, uint mission) =>
            Conversation(harness, npc).TryGetValue(ConversationType.ObjectiveChoice, out var value)
                ? ((List<ChoiceObjectives>)value).Where(topic => topic.MissionId == mission)
                    .Select(topic => ((uint)topic.ObjectiveId, (uint)topic.PlayerFlagId)).ToArray()
                : System.Array.Empty<(uint, uint)>();

        private static uint Flag(WildernessRuntimeTestHarness harness, uint flag)
        {
            using var unit = harness.CreateChar();
            return unit.CharacterFlags.Get(harness.Client.Player.Id).TryGetValue(flag, out var value) ? value : 0;
        }

        /// <summary>A turn-in whose save fails pays nothing; the mission stays to be turned in (as PoolsRetreadChainTests).</summary>
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

        private static void AssertOnTheGround(MapChannel pools, Vector3 position, string what)
        {
            var ground = pools.NavMesh.GroundHeight(position);
            Assert.IsNotNull(ground, $"navmesh under {what}");
            Assert.AreEqual(ground.Value, position.Y, 0.5f, $"{what} on the ground");
        }

        private static void AssertWalkedTo(MapChannel pools, Vector3 from, Vector3 position, string what) =>
            Assert.IsTrue(pools.NavMesh.FindPath(from, position, out var complete) != null && complete, $"{what} is walked to");

        private static void Accept(WildernessRuntimeTestHarness harness, Creature npc, uint mission)
        {
            harness.MoveTo(npc.Position);
            harness.Drain();
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, npc.EntityId, mission), $"{mission} accepted");
            harness.Drain();
        }

        private static Creature Npc(MapChannel pools, uint pool)
        {
            Spawn(pools, pool);
            var npc = Living(pools).SingleOrDefault(creature => creature.SpawnPool?.DbId == pool && creature.Npc != null);
            Assert.IsNotNull(npc, $"Pool {pool} must stand a real public NPC in the Pools.");
            return npc;
        }

        private static MissionObjectiveState Objective(WildernessRuntimeTestHarness harness, uint mission, uint objective) =>
            harness.Client.Player.Missions[mission].Objectives[objective].State;
    }
}
