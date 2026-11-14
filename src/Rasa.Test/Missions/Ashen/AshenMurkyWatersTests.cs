using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.ClientData;
using Rasa.Data;
using Rasa.Game;
using Rasa.Managers;
using Rasa.Packets.Mission.Server;
using Rasa.Services.Preloader.Missions.Ashen;
using Rasa.Structures;
using Rasa.Test.Missions.Marshes;
using Rasa.Test.Missions.Plateau;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions.Ashen
{
    using static MarshesFalconHoldTests;
    using static MarshesPaludosTests;
    using static PlateauCampResistanceTests;
    using W = AshenWhiteOasisNpcsV1;
    using D = AshenDesignerHidesNpcsV1;
    using R = AshenMurkyWatersNpcsV1;
    using Harvesters = AshenAnalyzingHarvestersV1;
    using Murky = AshenMurkyWatersV1;
    using Purification = AshenWaterPurificationV1;
    using Neuro = AshenNeurotoxinsV1;
    using Wellwater = AshenPoisoningTheWellwaterV1;
    using P = MarshesPreoDasTests;
    using E = MarshesEscortsTests;
    using FC = AshenFormalComplaintsTests;
    using WO = AshenWhiteOasisTests;
    using F = MarshesClientFloorTests;

    /// <summary>
    /// Ashen Desert set 12, the two sides of Murky Waters (1298), played through as a player plays them: Doctor Meeks' Water
    /// Purification (1299: 12 Granitour Water Glands off set 10's Coheri Pools Granitour, the repair side, flag 258) and Agent
    /// Hale's Neurotoxins (1300: 12 Lasher Poison Glands off the Lashers, the poison side, flag 257) and Poisoning the Wellwater
    /// (1302: four Water Filtration Systems installed through the player's own panels at Maji Field's four harvesters). With them
    /// the set's places: the two new Lasher pools on the ridge west of White Oasis Post and the walks to every place the
    /// missions send a player.
    ///
    /// Why these tests: the side is for good and the flags stay at an abandon of 1298, so a gate on the flag alone opens a side
    /// before 1298 is done and a gate on 1298 alone opens both; a gland dropped by the wrong creature, before the mission, past
    /// its count or at the wrong chance, a filter not given, not taken or taken twice, two panels at one place (1295's and
    /// 1302's), progress lost at a reload, a turn-in that pays twice or on a full bag, or a place the client's floor does not
    /// reach or whose walk passes another camp would each break them.
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class AshenMurkyWatersTests
    {
        private const uint ArmorFiller = 12748;            // Armor_T3_Bio_V01_CMN_Gloves_30_to_34 (inventory category 1)
        private const uint ConsumableFiller = 45060;       // Class VII Basic Med Pack (as AshenFormalComplaintsTests)
        private const uint MissionItemFiller = 11519;      // as PlateauCampResistanceTests (inventory category 4)
        private const uint ModuleFiller = 122983;          // Armor Module: Regen Bonus [1] (inventory category 3, as AshenLogisticianTests)
        private const uint ClientPanelClass = 9478;        // UsableTwoStateHumComputerV01V06
        private const uint ClientPanelName = 69;           // usablenameoverride LIGO_ASHEN_MAJI_DIAGNOSTIC "Harvester Control Panel" (TaRapedia's 1302 name)
        private const uint ClientPanelUseArgument = 1;     // usabledata lookup[9478][0]
        private const uint PanelOff = 55, PanelOn = 56;
        private const uint ClientGranitourGland = 97479;   // client item class 25478 "Granitour Water Gland", stack 12
        private const uint ClientLasherGland = 97480;      // client item class 25479 "Lasher Poison Gland", stack 12
        private const uint ClientFilterTemplate = 97495;   // client item class 25494 "Water Filtration System", stack 4
        private const uint ClientPoisonFlag = 257, ClientRepairFlag = 258;   // objectiveconversation 1298,2 / 1298,3 variants
        private const int Respawns = 4;                    // the rounds of respawns sampled after the first spawn
        private const float FloorReach = 1, PoolHeight = 2;
        private const float ChestHeight = 1.3f;             // a player's chest over the floor (ESTIMATE)
        private const float LasherMiddle = 1.0f;            // a Lasher's middle over its ground (ESTIMATE)
        /// <summary>The ridge pools the line-of-sight sample finds hidden from the walk up (TheRidgeLashersAreSeenFromTheWalkUp): a playtest watch point, reported 2026-11-10.</summary>
        private static readonly uint[] KnownHidden = { 790440 };

        private static readonly Vector3 WhiteOasis = AshenLeadInsTests.WhiteOasisWaypoint;
        private static readonly uint[] GranitourPools = D.GranitourPools.Select(pool => pool.Pool).ToArray();
        private static readonly uint[] AshokaLasherPools = D.LasherPools.Select(pool => pool.Pool).ToArray();
        private static readonly uint[] RidgePools = R.RidgePools.Select(pool => pool.Pool).ToArray();
        private static readonly uint[] LasherPools = AshokaLasherPools.Concat(RidgePools).ToArray();

        [TestInitialize]
        public void Clock() => PlateauGiantKillerSquadTests.UseClock();

        [TestCleanup]
        public void Restore() => PlateauGiantKillerSquadTests.RestoreClock();

        /// <summary>
        /// The gates. Before Murky Waters nobody offers the three; while Murky Waters is open after choice 2 (flag 257) neither;
        /// after choice 2 and an abandon (the flag stays: a gate on the flag alone fails here) neither, after a reload too. Murky
        /// Waters done by choice 1 (258 at 1, 257 back to 0): Meeks offers Water Purification (Hale and Daris do not), nobody
        /// offers Neurotoxins (a gate on 1298 alone fails here) or Poisoning the Wellwater; after a reload the same.
        /// </summary>
        [TestMethod]
        public void TheRepairSideOpensOnlyWaterPurification()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            AshenRuntime.SeedCompleted(harness, Harvesters.MissionId);
            var desert = AshenLeadInsTests.EnterAshen(harness, WhiteOasis);
            var meeks = Npc(desert, W.Meeks);
            var hale = Npc(desert, W.Hale);
            var daris = Npc(desert, W.DarisLinier);
            void NoneOffered(string when)
            {
                foreach (var npc in new[] { meeks, hale, daris })
                    foreach (var mission in new[] { Purification.MissionId, Neuro.MissionId, Wellwater.MissionId })
                        Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, npc, mission), $"{when}: {npc.DbId} does not offer {mission}");
            }
            NoneOffered("before Murky Waters");

            Accept(harness, meeks, Murky.MissionId);
            FC.Choose(harness, hale, Murky.MissionId, Murky.Speak, Murky.AgreeWithHale);
            Assert.AreEqual((1U, 0U), (FC.Flag(harness, ClientPoisonFlag), FC.Flag(harness, ClientRepairFlag)), "choice 2: flag 257");
            NoneOffered("1298 open after choice 2");
            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Murky.MissionId));
            harness.Drain();
            Assert.AreEqual(1U, FC.Flag(harness, ClientPoisonFlag), "the flag stays at an abandon");
            NoneOffered("choice 2, 1298 abandoned");
            AshenRuntime.Reload(harness);
            harness.Drain();
            NoneOffered("choice 2, 1298 abandoned, after a reload");

            Accept(harness, meeks, Murky.MissionId);
            FC.Choose(harness, hale, Murky.MissionId, Murky.Speak, Murky.RefuseHale);
            FC.Say(harness, hale, Murky.MissionId, Murky.Repair, ClientRepairFlag);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, hale, Murky.MissionId, 0, Murky.Experience, Murky.Credits);
            harness.Drain();
            Assert.AreEqual((0U, 1U), (FC.Flag(harness, ClientPoisonFlag), FC.Flag(harness, ClientRepairFlag)), "choice 1 done: 258, 257 back to 0");
            foreach (var when in new[] { "1298 done by choice 1", "after a reload" })
            {
                Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, meeks, Purification.MissionId), $"{when}: Meeks offers Water Purification");
                Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, hale, Purification.MissionId), $"{when}: not Hale's to give");
                Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, daris, Purification.MissionId), $"{when}: not Daris' to give");
                foreach (var npc in new[] { meeks, hale, daris })
                    foreach (var mission in new[] { Neuro.MissionId, Wellwater.MissionId })
                        Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, npc, mission), $"{when}: {npc.DbId} does not offer {mission} on the repair side");
                AshenRuntime.Reload(harness);
                harness.Drain();
            }
        }

        /// <summary>
        /// The poison side: after choice 1 and an abandon (258 stays) nobody offers Water Purification. Murky Waters done by
        /// choice 2: Hale offers Neurotoxins (Meeks does not), nobody offers Water Purification or, before Neurotoxins is done,
        /// Poisoning the Wellwater. Neurotoxins done: Hale offers Poisoning the Wellwater, after a reload too. With Neurotoxins in
        /// the history but flag 257 at 0 (258 at 1), Hale does not (the flag gate of 1302).
        /// </summary>
        [TestMethod]
        public void ThePoisonSideOpensNeurotoxinsThenTheWellwater()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            AshenRuntime.SeedCompleted(harness, Harvesters.MissionId);
            var desert = AshenLeadInsTests.EnterAshen(harness, WhiteOasis);
            var meeks = Npc(desert, W.Meeks);
            var hale = Npc(desert, W.Hale);
            Accept(harness, meeks, Murky.MissionId);
            FC.Choose(harness, hale, Murky.MissionId, Murky.Speak, Murky.RefuseHale);
            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Murky.MissionId));
            harness.Drain();
            Assert.AreEqual(1U, FC.Flag(harness, ClientRepairFlag), "258 stays at an abandon");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, meeks, Purification.MissionId), "choice 1, 1298 abandoned: no Water Purification");

            Accept(harness, meeks, Murky.MissionId);
            FC.Choose(harness, hale, Murky.MissionId, Murky.Speak, Murky.AgreeWithHale);
            FC.Say(harness, hale, Murky.MissionId, Murky.Poison, ClientPoisonFlag);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, hale, Murky.MissionId, 0, Murky.Experience, Murky.Credits);
            harness.Drain();
            Assert.AreEqual((1U, 0U), (FC.Flag(harness, ClientPoisonFlag), FC.Flag(harness, ClientRepairFlag)), "choice 2 done: 257, 258 back to 0");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, hale, Neuro.MissionId), "Hale offers Neurotoxins");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, meeks, Neuro.MissionId), "not Meeks' to give");
            foreach (var npc in new[] { meeks, hale })
            {
                Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, npc, Purification.MissionId), $"{npc.DbId}: no Water Purification on the poison side");
                Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, npc, Wellwater.MissionId), $"{npc.DbId}: 1302 not before 1300");
            }

            AshenRuntime.SeedCompleted(harness, Harvesters.MissionId, Neuro.MissionId);
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, hale, Wellwater.MissionId), "1300 done: Hale offers 1302");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, meeks, Wellwater.MissionId), "not Meeks' to give");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, hale, Neuro.MissionId), "1300 done once");
            AshenRuntime.Reload(harness);
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, hale, Wellwater.MissionId), "after a reload too");
            FC.SeedFlag(harness, ClientPoisonFlag, 0);
            FC.SeedFlag(harness, ClientRepairFlag, 1);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, hale, Wellwater.MissionId), "1300 done but flag 257 at 0: no 1302");
        }

        [TestMethod]
        public void WaterPurificationComesOffTheCoheriPoolsGranitour() => Glands(new(Purification.MissionId, W.Meeks, ClientRepairFlag, new[] { D.Granitour },
            GranitourPools, D.Wendy, D.WendyPool, ClientGranitourGland,
            new[] { Purification.EmpGrenade, Purification.ResTraumaKit, Purification.ConcussionGrenade, Purification.BasicMedPack },
            Purification.EmpGrenade, Purification.Experience, Purification.Credits, ConsumableFiller, 2, null, Abandon: true));

        [TestMethod]
        public void NeurotoxinsComeOffTheLashers() => Glands(new(Neuro.MissionId, W.Hale, ClientPoisonFlag, new[] { D.Lasher },
            LasherPools, D.Molly, D.MollyPool, ClientLasherGland,
            new[] { Neuro.MechLegs, Neuro.GravitonVest, Neuro.BioVest, Neuro.StealthVest },
            Neuro.MechLegs, Neuro.Experience, Neuro.Credits, ArmorFiller, 4, Wellwater.MissionId, Abandon: false));

        /// <summary>One of the gland missions: the mission, its giver, its side's flag, the creatures and their pools, the boss of their kind (no gland), the gland, the items, the first item, the pay, a filler of the items' bag, the markers, the follow-up; with an abandon or not.</summary>
        internal sealed record GlandOrder(uint Mission, uint Giver, uint Flag, uint[] Normals, uint[] Pools, uint Boss, uint BossPool, uint Gland,
            uint[] Items, uint First, uint Experience, uint Credits, uint Filler, int Markers, uint? FollowUp, bool Abandon);

        /// <summary>
        /// A gland mission played through: with Murky Waters done on its side (the flag set), the giver offers it with the four
        /// items; no gland drops before it. Accepted, the giver hears no objective; a marker on each pool, on Ashen Desert. A
        /// normal drops the gland, under 75 (75 misses, 74 drops); the boss of the kind drops none; a death is not a pickup; a full
        /// bag leaves it on the corpse, one slot free and it is picked up. Twelve glands, from every pool of the kind: the twelfth
        /// completes 1 once, a thirteenth kill drops none; a reload halfway keeps the count; the mission is completeable once.
        /// With an abandon: the glands are kept and count again at once. A full bag waits, a failed save pays nothing and keeps the
        /// glands; the giver takes all twelve and pays the item chosen, once; then the follow-up (if any) is offered.
        /// </summary>
        private static void Glands(GlandOrder order)
        {
            var percentile = 0;
            using var harness = WildernessRuntimeTestHarness.Create(lootRoll: (minimum, maximum) => maximum == 100 ? percentile : minimum);
            AshenRuntime.SeedCompleted(harness, Harvesters.MissionId, Murky.MissionId);
            FC.SeedFlag(harness, order.Flag, 1);
            var desert = AshenLeadInsTests.EnterAshen(harness, WhiteOasis);
            var giver = Npc(desert, order.Giver);
            Spawn(desert, order.Pools.Append(order.BossPool).ToArray());

            var offer = WildernessRanjaGorgeTests.Offer(harness, giver, order.Mission);
            CollectionAssert.AreEqual(order.Items, offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray(), "the four items to choose");
            Assert.IsFalse(Holds(AshenScavengerTests.Kill(harness, desert, order.Normals[0], order.Pools), order.Gland), "no gland before the mission");

            Accept(harness, giver, order.Mission);
            Assert.IsEmpty(Topics(harness, giver, order.Mission), "the giver hears no objective: the glands count as they are picked up");
            var markers = harness.Manager.BuildStatusSnapshot(harness.Client.Player)[order.Mission].ObjectivesList.Single().IndicatorList;
            Assert.HasCount(order.Markers, markers, "a marker on each pool");
            Assert.IsTrue(markers.All(marker => marker.MapContextId == AshenMissionDataV1.MapContextId), "on Ashen Desert");

            percentile = 75;                                  // the rate written out (ESTIMATE 75 %)
            Assert.IsFalse(Holds(AshenScavengerTests.Kill(harness, desert, order.Normals[0], order.Pools), order.Gland), "75 misses");
            percentile = 74;
            Assert.IsFalse(Holds(AshenScavengerTests.Kill(harness, desert, order.Boss, new[] { order.BossPool }), order.Gland), "the boss drops no gland");
            var first = AshenScavengerTests.Kill(harness, desert, order.Normals[0], order.Pools);
            Assert.IsTrue(Holds(first, order.Gland), "74: a normal drops the gland");
            Assert.AreEqual(0U, Held(harness, order.Gland), "a death is not a pickup");
            WildernessAliaBranchesTests.FillCategory(harness, MissionItemFiller, 50);
            PickUp(harness, first);
            Assert.AreEqual(0U, Held(harness, order.Gland), "no room");
            Assert.IsTrue(Holds(first, order.Gland), "still on the corpse");
            WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
            PickUp(harness, first);
            Assert.AreEqual(1U, Held(harness, order.Gland), "picked up once there is room");

            percentile = 0;
            var packets = harness.Drain().Cast<object>().ToList();
            for (var count = 2; count <= (int)Purification.Count; count++)
            {
                var pool = new[] { order.Pools[count % order.Pools.Length] };
                var loot = AshenScavengerTests.Kill(harness, desert, order.Normals[0], pool);
                Assert.IsTrue(Holds(loot, order.Gland), $"gland {count} dropped (pool {pool[0]})");
                PickUp(harness, loot);
                packets.AddRange(harness.Drain());
                if (count == 6)
                {
                    AshenRuntime.Reload(harness);
                    harness.Drain();
                    Assert.AreEqual(6U, Held(harness, order.Gland), "kept at a reload");
                    Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, order.Mission, 1), "six of twelve");
                }
                if (order.Abandon && count == 11)
                {
                    Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, order.Mission));
                    harness.Drain();
                    Assert.AreEqual(11U, Held(harness, order.Gland), "kept at an abandon");
                    Assert.IsFalse(Holds(AshenScavengerTests.Kill(harness, desert, order.Normals[0], order.Pools), order.Gland), "no gland while it is dropped");
                    Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, giver, order.Mission), "offered again after an abandon");
                    Accept(harness, giver, order.Mission);
                    Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, order.Mission, 1), "eleven held count again, not done");
                    packets = harness.Drain().Cast<object>().ToList();
                }
            }
            Assert.AreEqual(12U, Held(harness, order.Gland), "twelve glands");
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, order.Mission, 1), "1 done");
            Assert.AreEqual(1, packets.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == order.Mission && packet.ObjectiveId == 1), "1: ObjectiveCompleted once");
            Assert.AreEqual(1, packets.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == order.Mission && packet.IsCompleteable), "MissionCompleteable once");
            Assert.IsFalse(Holds(AshenScavengerTests.Kill(harness, desert, order.Normals[0], order.Pools), order.Gland), "a thirteenth kill drops none");

            FullBagWaits(harness, giver, order.Mission, 0, order.Filler);
            FailedTurnInPaysNothing(harness, giver, order.Mission, 0);
            Assert.AreEqual(12U, Held(harness, order.Gland), "kept at the failed turn-in");
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, giver, order.Mission, 0, order.Experience, order.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == order.Mission), "MissionRewarded once");
            Assert.IsGreaterThanOrEqualTo(1U, Held(harness, order.First), "the item chosen");
            Assert.AreEqual(0U, Held(harness, order.Gland), "the giver takes all twelve");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, giver, order.Mission), "done once");
            if (order.FollowUp is { } next)
                Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, giver, next), $"the giver offers {next}");
            Assert.IsFalse(Holds(AshenScavengerTests.Kill(harness, desert, order.Normals[0], order.Pools), order.Gland), "after the turn-in no gland drops");
        }

        /// <summary>
        /// Poisoning the Wellwater: Hale offers the four modules; a bag with no room for the systems refuses the accept (no system,
        /// no panel); with one slot free, four systems at the accept. An abandon after one panel takes the systems and the panels;
        /// taken again, four systems and four panels off, a marker on each, nobody hears an objective. The player's own four
        /// panels (the client's monitor, named "Harvester Control Panel", off, on Analyzing the Harvesters' places) each take one
        /// system at a use, turn on and complete their own objective once, in any order (2, 4, 1, 3); a used panel takes no
        /// second; a reload keeps them; the fourth makes the mission completeable once, no system is left. Meeks cannot take it;
        /// a full bag waits, a failed save pays nothing; Hale pays the module chosen, once.
        /// </summary>
        [TestMethod]
        public void PoisoningTheWellwaterInstallsTheFourFilters()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            AshenRuntime.SeedCompleted(harness, Harvesters.MissionId, Murky.MissionId, Neuro.MissionId);
            FC.SeedFlag(harness, ClientPoisonFlag, 1);
            var desert = AshenLeadInsTests.EnterAshen(harness, WhiteOasis);
            var hale = Npc(desert, W.Hale);
            var meeks = Npc(desert, W.Meeks);
            var offer = WildernessRanjaGorgeTests.Offer(harness, hale, Wellwater.MissionId);
            CollectionAssert.AreEqual(new[] { Wellwater.ResistFire, Wellwater.ResistIce, Wellwater.ResistVirulent, Wellwater.ResistElectric },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());

            WildernessAliaBranchesTests.FillCategory(harness, MissionItemFiller, 50);
            harness.MoveTo(hale.Position);
            harness.Drain();
            Assert.IsFalse(harness.Manager.AcceptOfferedMission(harness.Client, hale.EntityId, Wellwater.MissionId), "no room for the systems");
            harness.Manager.Scenes.Tick(desert);
            Assert.AreEqual(0U, Filters(harness), "none given");
            Assert.IsFalse(desert.DynamicObjects.Any(obj => obj.SceneMissionId == Wellwater.MissionId && obj.IsInWorld), "no panel");
            WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);

            Accept(harness, hale, Wellwater.MissionId);
            Assert.AreEqual(Wellwater.Filters, Filters(harness), "four systems at the accept");
            var panels = Panels(harness, desert);
            Install(harness, desert, panels[0]);
            Assert.AreEqual((1, Wellwater.Filters - 1), (Installed(harness), Filters(harness)));
            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Wellwater.MissionId));
            harness.Manager.Scenes.Tick(desert);
            harness.Drain();
            Assert.AreEqual(0U, Filters(harness), "the systems go with the mission");
            Assert.IsFalse(desert.DynamicObjects.Any(obj => obj.SceneMissionId == Wellwater.MissionId && obj.IsInWorld), "the panels go with the abandon");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, hale, Wellwater.MissionId), "offered again after an abandon");

            Accept(harness, hale, Wellwater.MissionId);
            Assert.AreEqual(Wellwater.Filters, Filters(harness), "four again");
            Assert.IsEmpty(Topics(harness, hale, Wellwater.MissionId), "Hale hears no objective");
            var snapshot = harness.Manager.BuildStatusSnapshot(harness.Client.Player)[Wellwater.MissionId].ObjectivesList;
            Assert.HasCount(Wellwater.PanelsAt.Length, snapshot);
            foreach (var objective in snapshot)
            {
                var marker = objective.IndicatorList.Single();
                var at = Wellwater.PanelsAt[objective.ObjectiveId - 1];
                Assert.IsLessThan(0.1f, Vector2.Distance(new(marker.Position.X, marker.Position.Z), new(at.X, at.Z)), $"objective {objective.ObjectiveId}'s marker on its panel");
                Assert.AreEqual(AshenMissionDataV1.MapContextId, marker.MapContextId, "on Ashen Desert");
            }

            panels = Panels(harness, desert);
            Assert.AreEqual(0, Installed(harness), "all open again");
            var log = new List<object>();
            var done = new List<int>();
            foreach (var number in new[] { 2, 4, 1, 3 })
            {
                var panel = panels[number - 1];
                harness.MoveTo(panel.Position + new Vector3(0, 0, 1));
                harness.Drain();
                Use(harness, desert, panel, ClientPanelUseArgument, recovers: true);
                var packets = harness.Drain().Cast<object>().Concat(E.Tick(harness, desert, 250)).ToList();
                log.AddRange(packets);
                done.Add(number);
                Assert.AreEqual(Wellwater.Filters - (uint)done.Count, Filters(harness), $"panel {number} takes one system");
                Assert.AreEqual(((UseObjectState)PanelOn, false), (panel.StateId, panel.IsEnabled), $"panel {number} on and closed");
                Assert.AreEqual(1, packets.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Wellwater.MissionId && packet.ObjectiveId == Wellwater.Sabotage(number)),
                    $"panel {number}: its objective, once");
                Assert.AreEqual(done.Count, Installed(harness), $"panel {number}: only its own objective");
                if (number == 2)
                {
                    WO_TryUse(harness, desert, panel);
                    Assert.AreEqual((1, Wellwater.Filters - 1), (Installed(harness), Filters(harness)), "a used panel takes no second system");
                    P.ReloadAndRecover(harness, desert);
                    Assert.AreEqual((1, Wellwater.Filters - 1), (Installed(harness), Filters(harness)), "kept at a reload");
                    panels = Panels(harness, desert, done: new[] { 2 });
                }
                if (done.Count < Wellwater.PanelsAt.Length)
                    Assert.IsFalse(harness.Client.Player.Missions[Wellwater.MissionId].Completeable, $"{done.Count} of 4: not completeable");
            }
            Assert.AreEqual(0U, Filters(harness), "no system left");
            Assert.IsTrue(harness.Client.Player.Missions[Wellwater.MissionId].Completeable, "four panels");
            Assert.AreEqual(1, log.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Wellwater.MissionId && packet.IsCompleteable), "MissionCompleteable once");

            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, meeks.EntityId, Wellwater.MissionId, 0), "not Meeks' to take");
            FullBagWaits(harness, hale, Wellwater.MissionId, 0, ModuleFiller);
            FailedTurnInPaysNothing(harness, hale, Wellwater.MissionId, 0);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, hale, Wellwater.MissionId, 0, Wellwater.Experience, Wellwater.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Wellwater.MissionId), "MissionRewarded once");
            Assert.IsGreaterThanOrEqualTo(1U, WildernessRanjaGorgeTests.Held(harness, Wellwater.ResistFire), "the module chosen");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, hale, Wellwater.MissionId), "done once");
        }

        /// <summary>
        /// One panel at a place: Analyzing the Harvesters played to its turn-in leaves none of its panels in the world; then, on
        /// the poison side after Neurotoxins, Poisoning the Wellwater's four panels stand on the same four places, each the only
        /// object of a mission there (TaRapedia names 1302's panels by 1295's spots; set 11's triage risk "two scene objects at
        /// one place").
        /// </summary>
        [TestMethod]
        public void TheWellwaterPanelsStandAloneWhereTheDiagnosticPanelsStood()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var desert = AshenLeadInsTests.EnterAshen(harness, WhiteOasis);
            var daris = Npc(desert, W.DarisLinier);
            var meeks = Npc(desert, W.Meeks);
            Accept(harness, daris, Harvesters.MissionId);
            harness.Manager.Scenes.Tick(desert);
            var diagnostic = desert.DynamicObjects.Where(obj => obj.SceneMissionId == Harvesters.MissionId && obj.IsInWorld).ToList();
            Assert.HasCount(Harvesters.PanelsAt.Length, diagnostic, "1295's four panels");
            foreach (var panel in diagnostic)
            {
                harness.MoveTo(panel.Position + new Vector3(0, 0, 1));
                harness.Drain();
                Use(harness, desert, panel, ClientPanelUseArgument, recovers: true);
                E.Tick(harness, desert, 250);
                harness.Drain();
            }
            Assert.IsTrue(harness.Client.Player.Missions[Harvesters.MissionId].Completeable, "1295 done at the panels");
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, meeks, Harvesters.MissionId, 0, Harvesters.Experience, Harvesters.Credits);
            harness.Manager.Scenes.Tick(desert);
            harness.Drain();
            Assert.IsFalse(desert.DynamicObjects.Any(obj => obj.SceneMissionId == Harvesters.MissionId && obj.IsInWorld), "1295's panels go at its turn-in");

            AshenRuntime.SeedCompleted(harness, Harvesters.MissionId, Murky.MissionId, Neuro.MissionId);
            FC.SeedFlag(harness, ClientPoisonFlag, 1);
            Accept(harness, Npc(desert, W.Hale), Wellwater.MissionId);
            var panels = Panels(harness, desert);
            for (var number = 1; number <= panels.Length; number++)
            {
                var at = Harvesters.PanelsAt[number - 1];
                var here = desert.DynamicObjects.Where(obj => obj.IsInWorld && obj.SceneMissionId != 0 &&
                    Vector2.Distance(new(obj.Position.X, obj.Position.Z), new(at.X, at.Z)) < 3).ToList();
                Assert.HasCount(1, here, $"harvester {number}: one mission object within 3 m of the place");
                Assert.AreSame(panels[number - 1], here[0], $"harvester {number}: 1302's panel");
            }
        }

        /// <summary>
        /// The ridge pools: each puts down three of set 10's Lasher (790362), hostile, with the row's level, name and hit points.
        /// Round each point every navmesh floor is one floor (AshenFixAV1.ClearRadius), joined to the White Oasis Post waypoint's
        /// ground, within 1 m of a client floor and under open sky; at the first spawn and after each of four rounds of kills and
        /// respawns every Lasher stands on the navmesh, on the point or its rings, within 1 m of the client's floor and 2 m of
        /// its pool's height, and inside its 1300 marker. Each pool stands 60 m or more outside the circle of every Ashen Desert pool of
        /// another hostile kind (the Lasher pools of set 10 and Molly aside) and 85 m or more from every friendly NPC's pool.
        /// </summary>
        [TestMethod]
        public void TheRidgeLashersComeDownAndBackOnTheFloorPlayersWalk()
        {
            var dataDir = F.ClientData();
            using var harness = WildernessRuntimeTestHarness.Create();
            var desert = AshenLeadInsTests.EnterAshen(harness);
            var nav = desert.NavMesh;
            using var meshes = new MeshLibrary(dataDir, F.MeshCsv());
            var geometry = MapGeometry.Load(Path.Combine(dataDir, "maps", harness.World.MapInfoEntries.AsNoTracking()
                .Single(map => map.Id == AshenMissionDataV1.MapContextId).MapName), meshes, 1);
            var markers = harness.World.MissionIndicatorEntries.AsEnumerable()
                .Where(row => row.ContentRevision == AshenMissionDataV1.Revision && row.MissionId == Neuro.MissionId).ToList();
            Spawn(desert, RidgePools);
            var region = nav.RegionArea(nav.Nearest(WhiteOasis).Value);
            var failures = new List<string>();
            foreach (var poolId in RidgePools)
            {
                var pool = desert.SpawnPools.Single(loaded => loaded.DbId == poolId);
                Assert.AreEqual(0f, pool.Radius, $"{poolId} a point pool");
                Assert.IsTrue(SpawnPoolManager.HasWalkableGround(desert, pool), $"{poolId} has walkable ground");
                var ground = Ground(desert, pool.Position, (float)AshenFixAV1.ClearRadius, out var stacked);
                Assert.IsGreaterThan(500, ground.Count, $"{poolId}: the floors round the point are read");
                if (stacked.Count > 0)
                    failures.Add($"pool {poolId}: more than one navmesh floor at " + string.Join("; ", stacked.Take(3)));
                var off = ground.Where(at => nav.RegionArea(at) != region).ToList();
                if (off.Count > 0)
                    failures.Add($"pool {poolId}: {off.Count} floors not joined to the White Oasis Post waypoint, " + string.Join("; ", off.Take(3)));
                var round = F.Near(geometry, ground, 0.5f);
                for (var i = 0; i < ground.Count; i++)
                {
                    var at = ground[i];
                    if (F.FloorAt(round[i], at, at.Y - 2, at.Y + 1.5f) is not { } y || y - at.Y > 1 || at.Y - y > 1.5f)
                        failures.Add(FormattableString.Invariant($"pool {poolId}: no client floor under the navmesh at ({at.X:0.0}, {at.Y:0.00}, {at.Z:0.0})"));
                    if (round[i].Any(t => F.Hit(at + new Vector3(0, 1, 0), Vector3.UnitY, t.A, t.B, t.C) is { } up && up < 40))
                        failures.Add(FormattableString.Invariant($"pool {poolId}: a client surface over ({at.X:0.0}, {at.Y:0.00}, {at.Z:0.0})"));
                }
            }

            var lasher = harness.World.CreatureEntries.AsNoTracking().Single(row => row.Id == D.Lasher);
            var health = harness.World.CreatureStatEntries.AsNoTracking().Single(row => row.Id == D.Lasher).Health;
            for (var round = 0; round <= Respawns; round++)
            {
                var here = Alive(desert).Where(candidate => candidate.SpawnPool != null && RidgePools.Contains(candidate.SpawnPool.DbId)).Distinct().ToList();
                foreach (var poolId in RidgePools)
                    Assert.AreEqual((int)R.PerPool, here.Count(one => one.SpawnPool.DbId == poolId && one.DbId == D.Lasher), $"round {round}: pool {poolId} has three Lashers and nothing else");
                Assert.HasCount(RidgePools.Length * (int)R.PerPool, here, $"round {round}: nothing else");
                var near = F.Near(geometry, here.Select(one => one.Position).ToList(), 1);
                for (var i = 0; i < here.Count; i++)
                {
                    var one = here[i];
                    var label = FormattableString.Invariant($"round {round}: pool {one.SpawnPool.DbId}'s Lasher at {one.Position}");
                    Assert.AreEqual((TargetCategory.Hostile, lasher.Level, lasher.NameId, health),
                        (one.TargetCategory, one.Level, one.NameId, (int)one.Attributes[Attributes.Health].NormalMax), label);
                    if (!nav.IsOnMesh(one.Position))
                        failures.Add($"{label}: off the navmesh");
                    var floor = F.FloorAt(near[i], one.Position, one.Position.Y - 3, one.Position.Y + 8);
                    if (floor is not { } y || MathF.Abs(y - one.Position.Y) > FloorReach)
                        failures.Add(FormattableString.Invariant($"{label}: the client's floor at {floor}"));
                    if (Vector2.Distance(new(one.Position.X, one.Position.Z), new(one.SpawnPool.Position.X, one.SpawnPool.Position.Z)) > AshenFixAV1.PointSpread + 0.5f)
                        failures.Add($"{label}: off the point and its rings");
                    if (MathF.Abs(one.Position.Y - one.SpawnPool.Position.Y) > PoolHeight)
                        failures.Add(FormattableString.Invariant($"{label}: {one.Position.Y - one.SpawnPool.Position.Y:0.00} m from its pool's height"));
                    if (!markers.Any(marker => Vector2.Distance(new((float)marker.PosX, (float)marker.PosZ), new(one.SpawnPool.Position.X, one.SpawnPool.Position.Z)) < 0.1f &&
                                               Vector2.Distance(new((float)marker.PosX, (float)marker.PosZ), new(one.Position.X, one.Position.Z)) <= marker.Radius))
                        failures.Add($"{label}: outside its 1300 marker");
                }
                if (round == Respawns)
                    break;
                foreach (var one in here)
                    AshenScavengerTests.KillForGood(harness, desert, one);
                Respawn(desert, RidgePools);
            }

            var pools = harness.World.SpawnPoolEntries.AsNoTracking().Where(row => row.MapContextId == AshenMissionDataV1.MapContextId).ToList();
            bool HoldsKind(Rasa.Structures.World.SpawnPoolEntry row, Func<Creature, bool> what) =>
                new[] { row.Creature1Id, row.Creature2Id, row.Creature3Id, row.Creature4Id, row.Creature5Id, row.Creature6Id }
                    .Any(id => id != 0 && CreatureManager.Instance.LoadedCreatures.TryGetValue(id, out var creature) && what(creature));
            var hostile = pools.Where(row => HoldsKind(row, creature => creature.TargetCategory == TargetCategory.Hostile)).ToList();
            var npcs = pools.Where(row => HoldsKind(row, creature => creature.TargetCategory != TargetCategory.Hostile && creature.Npc != null)).ToList();
            Assert.IsTrue(npcs.Any(row => row.Id == W.Hale), "Hale among the NPCs");
            var family = LasherPools.Append(D.MollyPool).ToArray();
            foreach (var poolId in RidgePools)
            {
                var pool = pools.Single(row => row.Id == poolId);
                // To the other pool's circle (centre less radius, 2D), as AshenWhiteOasisTests.Clearance measures.
                float Across(Rasa.Structures.World.SpawnPoolEntry other) =>
                    Vector2.Distance(new((float)pool.PosX, (float)pool.PosZ), new((float)other.PosX, (float)other.PosZ)) - (float)other.Radius;
                var nearestHostile = hostile.Where(row => !family.Contains(row.Id)).MinBy(Across);
                if (Across(nearestHostile) < R.HostileGap)
                    failures.Add(FormattableString.Invariant($"pool {poolId}: {Across(nearestHostile):0.0} m outside hostile pool {nearestHostile.Id}'s circle"));
                var nearestNpc = npcs.MinBy(Across);
                if (Across(nearestNpc) < R.NpcGap)
                    failures.Add(FormattableString.Invariant($"pool {poolId}: {Across(nearestNpc):0.0} m outside NPC pool {nearestNpc.Id}'s circle"));
            }
            Assert.IsEmpty(failures, string.Join("; ", failures));
        }

        /// <summary>
        /// Line of sight to the ridge Lashers (the set 11 review: a target in a dip or behind a slope edge cannot be hit from
        /// below or above; the client refuses the ability without a chat line). From the walk up from the White Oasis Post
        /// waypoint (the navmesh's path), where it comes 30, 25 and 20 m (2D) from the pool's point, a player's chest (ChestHeight
        /// over the client's floor) sees each Lasher's middle (LasherMiddle over the navmesh ground) at the point and at eight
        /// places on the ring of AshenFixAV1.PointSpread round it: no triangle of the client's terrain or placed meshes on the
        /// line (ESTIMATE heights). Finding of 2026-11-10 (reported to the coordinator, no data change): pool 790440 is hidden
        /// from all three places (21 of 27 rays; each ray meets the client's surfaces 3-9 m in front of the chest and leaves them
        /// again further on), so it stands in KnownHidden and the test pins that it still is; 790441 is seen from all three.
        /// </summary>
        [TestMethod]
        public void TheRidgeLashersAreSeenFromTheWalkUp()
        {
            var dataDir = F.ClientData();
            using var harness = WildernessRuntimeTestHarness.Create();
            var desert = AshenLeadInsTests.EnterAshen(harness);
            var nav = desert.NavMesh;
            using var meshes = new MeshLibrary(dataDir, F.MeshCsv());
            var geometry = MapGeometry.Load(Path.Combine(dataDir, "maps", harness.World.MapInfoEntries.AsNoTracking()
                .Single(map => map.Id == AshenMissionDataV1.MapContextId).MapName), meshes, 1);
            var failures = new List<string>();
            var hidden = new Dictionary<uint, int>();
            var rays = 0;
            foreach (var (pool, at) in R.RidgePools)
            {
                var centre = P.ToVector(at);
                var start = nav.Nearest(WhiteOasis).Value;
                var path = nav.FindPath(start, nav.Nearest(centre).Value, out var complete);
                Assert.IsTrue(path != null && complete, $"pool {pool}: a walk up");
                var line = new List<Vector3>();
                var corners = path.Prepend(start).ToList();
                for (var k = 0; k + 1 < corners.Count; k++)
                    for (var step = 0f; step < 1; step += 0.02f)
                        line.Add(Vector3.Lerp(corners[k], corners[k + 1], step));
                var targets = new List<Vector3> { centre };
                for (var k = 0; k < 8; k++)
                {
                    var angle = k * MathF.PI / 4;
                    var ring = centre + new Vector3(MathF.Sin(angle) * (float)AshenFixAV1.PointSpread, 0, MathF.Cos(angle) * (float)AshenFixAV1.PointSpread);
                    targets.Add(nav.Nearest(ring) ?? ring);
                }
                foreach (var distance in new[] { 30f, 25f, 20f })
                {
                    // The first place of the walk (from the waypoint) that comes within this distance of the point.
                    var stand = line.First(point => Vector2.Distance(new(point.X, point.Z), new(centre.X, centre.Z)) <= distance);
                    var near = F.Near(geometry, new List<Vector3> { stand }, distance + (float)AshenFixAV1.PointSpread + 2)[0];
                    if (F.FloorAt(near, stand, stand.Y - 2, stand.Y + 2) is not { } floor)
                    {
                        failures.Add(FormattableString.Invariant($"pool {pool}, {distance} m: no client floor at ({stand.X:0.0}, {stand.Z:0.0})"));
                        continue;
                    }
                    var eye = stand with { Y = floor + ChestHeight };
                    foreach (var target in targets)
                    {
                        rays++;
                        if (F.Clear(near, eye, target + new Vector3(0, LasherMiddle, 0)))
                            continue;
                        hidden[pool] = hidden.GetValueOrDefault(pool) + 1;
                        if (!KnownHidden.Contains(pool))
                            failures.Add(FormattableString.Invariant($"pool {pool}, {distance} m from ({eye.X:0.0}, {eye.Y:0.0}, {eye.Z:0.0}): ({target.X:0.0}, {target.Y:0.0}, {target.Z:0.0}) hidden"));
                    }
                }
            }
            Assert.AreEqual(R.RidgePools.Length * 3 * 9, rays, "the rays are cast");
            foreach (var pool in KnownHidden)
                Assert.IsGreaterThan(0, hidden.GetValueOrDefault(pool), $"pool {pool} is seen now: take it out of KnownHidden");
            Assert.IsEmpty(failures, $"{failures.Count}: " + string.Join("; ", failures));
        }

        /// <summary>
        /// One kill, two missions: with All the Latest Rage! (1293) and Water Purification (1299) both open, one Coheri Pools
        /// Granitour's corpse holds a Shiny Granitour Carapace and a Granitour Water Gland (each its own binding, each rolled at
        /// 74 under 75); one pickup gives both, each counts on its own mission's objective 1 once; a kill at 75 drops neither.
        /// </summary>
        [TestMethod]
        public void OneGranitourDropsForBothOpenMissions()
        {
            var percentile = 74;
            using var harness = WildernessRuntimeTestHarness.Create(lootRoll: (minimum, maximum) => maximum == 100 ? percentile : minimum);
            AshenRuntime.SeedCompleted(harness, AshenDesignerJewelryV1.MissionId, Harvesters.MissionId, Murky.MissionId);
            FC.SeedFlag(harness, ClientRepairFlag, 1);
            var desert = AshenLeadInsTests.EnterAshen(harness, WhiteOasis);
            Accept(harness, Npc(desert, W.Meeks), Purification.MissionId);
            Accept(harness, Npc(desert, D.IdaHayner), AshenLatestRageV1.MissionId);
            Spawn(desert, GranitourPools);
            var loot = AshenScavengerTests.Kill(harness, desert, D.Granitour, GranitourPools);
            Assert.IsTrue(Holds(loot, ClientGranitourGland) && Holds(loot, AshenLatestRageV1.ShinyTemplate), "one corpse: the gland and the carapace");
            PickUp(harness, loot);
            harness.Drain();
            Assert.AreEqual((1U, 1U), (Held(harness, ClientGranitourGland), Held(harness, AshenLatestRageV1.ShinyTemplate)), "both picked up");
            percentile = 75;
            var miss = AshenScavengerTests.Kill(harness, desert, D.Granitour, GranitourPools);
            Assert.IsFalse(Holds(miss, ClientGranitourGland) || Holds(miss, AshenLatestRageV1.ShinyTemplate), "75: neither");
            Assert.AreEqual((MissionObjectiveState.Incomplete, MissionObjectiveState.Incomplete),
                (Objective(harness, Purification.MissionId, 1), Objective(harness, AshenLatestRageV1.MissionId, 1)), "each counts on its own");
            percentile = 74;
            for (var count = 2; count <= 10; count++)
            {
                var next = AshenScavengerTests.Kill(harness, desert, D.Granitour, GranitourPools);
                PickUp(harness, next);
                harness.Drain();
            }
            Assert.AreEqual((10U, 10U), (Held(harness, ClientGranitourGland), Held(harness, AshenLatestRageV1.ShinyTemplate)), "ten of each");
            Assert.AreEqual((MissionObjectiveState.Incomplete, MissionObjectiveState.Completed),
                (Objective(harness, Purification.MissionId, 1), Objective(harness, AshenLatestRageV1.MissionId, 1)), "1293's ten done, 1299's twelve not");
        }

        /// <summary>
        /// The walks on the client's floor (AshenFixBTests.ClientFloorLine with a player's half width: every half metre, never
        /// more than 1.1 m up over a metre, nothing of the placed meshes in the way at knee, chest or head height), there and
        /// back, from each place's waypoint to every place the set sends a player (Places): the two ridge Lasher pools and set
        /// 10's two Lasher pools west of Ashoka (1300), set 10's two Coheri Pools Granitour pools (1299), the four panels (1302);
        /// by the corners Places names (set 10's and set 11's, the probe of 2026-11-10; ESTIMATE of a player's line). Every walk
        /// keeps HostileClearance (30 m, 2D) outside the circle of every automatic hostile Ashen Desert pool that is not its
        /// mission's own (1299's the Granitour pools, 1300's the Lasher pools).
        /// </summary>
        [TestMethod]
        public void TheBranchesPlacesAreWalkedToOnTheClientsFloorAwayFromOtherCamps()
        {
            var dataDir = F.ClientData();
            using var harness = WildernessRuntimeTestHarness.Create();
            var desert = AshenLeadInsTests.EnterAshen(harness);
            var nav = desert.NavMesh;
            using var meshes = new MeshLibrary(dataDir, F.MeshCsv());
            var geometry = MapGeometry.Load(Path.Combine(dataDir, "maps", harness.World.MapInfoEntries.AsNoTracking()
                .Single(map => map.Id == AshenMissionDataV1.MapContextId).MapName), meshes, 1);
            var hostile = WO.HostilePools(harness);
            Assert.IsGreaterThan(50, hostile.Count, "the Ashen hostile pools are read");
            Assert.IsTrue(RidgePools.All(pool => hostile.Any(row => row.Id == pool)), "the ridge pools among them");
            var failures = new List<string>();
            var walked = 0;
            foreach (var (label, from, target, there, back, own) in Places())
            {
                var at = nav.Nearest(target).Value;
                foreach (var (legs, direction) in new[] { (there.Prepend(from).Append(at).ToList(), "there"), (back.Prepend(at).Append(from).ToList(), "back") })
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
                    if (WO.Clearance(line, hostile.Where(row => !own.Contains(row.Id)).ToList()) is { } close)
                        failures.Add(FormattableString.Invariant($"{label} {direction}: {close.Gap:0.0} m outside hostile pool {close.Pool}'s circle at ({close.At.X:0.0}, {close.At.Y:0.0})"));
                }
            }
            Assert.IsGreaterThanOrEqualTo(2 * Places().Count, walked, "the legs are walked");
            Assert.IsEmpty(failures, $"{failures.Count}: " + string.Join("; ", failures));
        }

        /// <summary>
        /// The places a player walks to: label, the waypoint, the place, the corners there and back, the mission's own hostile
        /// pools. The ridge pools straight from White Oasis Post; set 10's pools by set 10's corners (AshenDesignerHidesTests.Places);
        /// the panels by set 11's (AshenWhiteOasisTests.Places).
        /// </summary>
        internal static List<(string Label, Vector3 From, Vector3 Target, Vector3[] There, Vector3[] Back, uint[] Own)> Places()
        {
            var places = new List<(string, Vector3, Vector3, Vector3[], Vector3[], uint[])>();
            foreach (var (pool, at) in R.RidgePools)
                {
                var way = R.WayTo(pool).Select(P.ToVector).ToArray();
                places.Add(($"the ridge Lasher pool {pool}", WhiteOasis, P.ToVector(at), way, Enumerable.Reverse(way).ToArray(), LasherPools));
            }
            foreach (var pool in AshokaLasherPools)
            {
                var place = AshenDesignerHidesTests.Places.Single(entry => entry.Pool == pool);
                places.Add((place.Label, place.From, P.ToVector(D.LasherPools.Single(entry => entry.Pool == pool).At), place.Way.There, place.Way.Back,
                    LasherPools.Append(D.MollyPool).ToArray()));
            }
            // The Coheri Pools Granitour: set 10's way by the west ridge, but round the west side of Maji Field first (set 11's
            // way to panel 4): fix E's straight leg from (-439.1, 204.8) to the ridge passes 2.2 m outside the Xanx pool
            // 540076's circle and 2.4 m outside the Tree Mite pool 540078's; to the first pool by the west of Coheri Pools
            // (CoheriWest), as the straight way from the ridge passes 16 m outside Wendy's pool 790377 (probe 2026-11-10).
            foreach (var pool in GranitourPools)
            {
                var place = AshenDesignerHidesTests.Places.Single(entry => entry.Pool == pool);
                var last = pool == GranitourPools[0] ? new[] { AshenDesignerHidesTests.CoheriWest } : Array.Empty<Vector3>();
                var there = AshenDesignerHidesTests.ByTheWestRidge.Concat(last).ToArray();
                var back = last.Concat(AshenDesignerHidesTests.BackByTheWestRidge).ToArray();
                places.Add((place.Label, WhiteOasis, P.ToVector(D.GranitourPools.Single(entry => entry.Pool == pool).At), there, back, GranitourPools));
            }
            for (var number = 1; number <= Wellwater.PanelsAt.Length; number++)
            {
                var place = WO.Places.Single(entry => entry.Label == $"Harvester Control Panel {number}");
                places.Add(($"1302's panel {number}", WhiteOasis, P.ToVector(Wellwater.PanelsAt[number - 1]), place.Way, Enumerable.Reverse(place.Way).ToArray(), Array.Empty<uint>()));
            }
            return places;
        }


        private static uint Held(WildernessRuntimeTestHarness harness, uint template) => WildernessRanjaGorgeTests.Held(harness, template);

        private static uint Filters(WildernessRuntimeTestHarness harness) => WildernessRanjaGorgeTests.Held(harness, ClientFilterTemplate);

        private static int Installed(WildernessRuntimeTestHarness harness) =>
            Enumerable.Range(1, Wellwater.PanelsAt.Length).Count(number => Objective(harness, Wellwater.MissionId, Wellwater.Sabotage(number)) == MissionObjectiveState.Completed);

        /// <summary>The four panels: the player's own, the client's monitor named "Harvester Control Panel", at 1295's places, on the ground; off and open (or, for <paramref name="done"/>, on and closed), with the client's use argument.</summary>
        private static DynamicObject[] Panels(WildernessRuntimeTestHarness harness, MapChannel desert, int[] done = null)
        {
            harness.Manager.Scenes.Tick(desert);
            var panels = new DynamicObject[Wellwater.PanelsAt.Length];
            for (var number = 1; number <= panels.Length; number++)
            {
                var isDone = done?.Contains(number) == true;
                var panel = desert.DynamicObjects.Single(obj => obj.SceneMissionId == Wellwater.MissionId && obj.SceneActorRole == Wellwater.PanelRole(number) && obj.IsInWorld);
                Assert.AreEqual((ClientPanelClass, ClientPanelName, P.ToVector(Harvesters.PanelsAt[number - 1]), ClientPanelUseArgument),
                    ((uint)panel.EntityClassId, panel.NameOverrideId, panel.Position, panel.MissionUseAction.ActionArgId), $"panel {number}");
                Assert.AreEqual((isDone ? (UseObjectState)PanelOn : (UseObjectState)PanelOff, !isDone), (panel.StateId, panel.IsEnabled), $"panel {number} state");
                Assert.AreEqual(harness.Client.Player.Id, panel.SceneOwnerCharacterId, "the player's own");
                AssertOnGround(desert, panel.Position, panel.SceneActorRole);
                panels[number - 1] = panel;
            }
            return panels;
        }

        /// <summary>A use of a panel by a player beside it, with its recovery and a tick.</summary>
        private static void Install(WildernessRuntimeTestHarness harness, MapChannel desert, DynamicObject panel)
        {
            harness.MoveTo(panel.Position + new Vector3(0, 0, 1));
            harness.Drain();
            Use(harness, desert, panel, ClientPanelUseArgument, recovers: true);
            E.Tick(harness, desert, 250);
            harness.Drain();
        }

        /// <summary>A use the server may refuse at once or after its recovery (as AshenWhiteOasisTests.TryUse): the caller checks that nothing came of it.</summary>
        private static void WO_TryUse(WildernessRuntimeTestHarness harness, MapChannel desert, DynamicObject obj)
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

        /// <summary>The navmesh floors on a 1 m grid within <paramref name="reach"/> of <paramref name="centre"/>, and the spots with more than one (as AshenDesignerHidesTests.Ground).</summary>
        private static List<Vector3> Ground(MapChannel desert, Vector3 centre, float reach, out List<string> stacked)
        {
            var ground = new List<Vector3>();
            stacked = new List<string>();
            var steps = (int)Math.Ceiling(reach);
            for (var dz = -steps; dz <= steps; dz++)
                for (var dx = -steps; dx <= steps; dx++)
                {
                    if (dx * dx + dz * dz > reach * reach)
                        continue;
                    var heights = new SortedSet<float>();
                    for (var h = -8; h <= 12; h++)
                    {
                        var probe = new Vector3(centre.X + dx, centre.Y + h, centre.Z + dz);
                        if (desert.NavMesh.Nearest(probe) is { } nearest && MathF.Abs(nearest.X - probe.X) < 0.3f && MathF.Abs(nearest.Z - probe.Z) < 0.3f &&
                            desert.NavMesh.GroundHeight(probe) is { } y)
                            heights.Add(MathF.Round(y, 1));
                    }
                    var column = new List<Vector3>();
                    foreach (var y in heights)
                        if (column.Count == 0 || y - column[^1].Y >= 2)
                            column.Add(new Vector3(centre.X + dx, y, centre.Z + dz));
                    if (column.Count > 1)
                        stacked.Add(string.Join(" / ", column));
                    ground.AddRange(column);
                }
            return ground;
        }
    }
}
