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
using Rasa.Services.Preloader.Missions.Crucible;
using Rasa.Structures;
using Rasa.Test.Missions.Marshes;
using Rasa.Test.Missions.Plateau;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions.Crucible
{
    using static MarshesFalconHoldTests;
    using static MarshesPaludosTests;
    using static PlateauCampResistanceTests;
    using A = CrucibleAwolCampNpcsV1;
    using C = CrucibleOutpostIntrepidTests;
    using G = CrucibleGrupaChiefsNpcsV1;
    using N = CrucibleOutpostIntrepidNpcsV1;
    using Cal = CrucibleCalendarGirlV1;
    using Cats = CrucibleStrayCatsV1;
    using Coin = CrucibleOtherSideOfTheCoinV1;
    using M = CrucibleMakingAmendsV1;

    /// <summary>
    /// Crucible set 6, the AWOL Camp, played through as a player plays it: Lieutenant Sherer's Calendar Girl (1144: Deirdre
    /// helped, ratted out with the truth or a lie, or threatened; the end sets the flag Melching's Smoking Guns text reads),
    /// Intelligence Officer D'Ambrose's Stray Cats (1152: the camp found and betrayed) and Sergeant Breiburg's The Other Side
    /// Of The Coin (1153: the secret kept or refused at Cheung), the fork between them, and its two ends, Making Amends (1155)
    /// and Sweet Revenge (1205: Munit's ledger for the crate, DeLisi, Breiburg).
    ///
    /// Why these tests: Cheung, Deirdre, Sherer and D'Ambrose are moved out of upstream's placeholder clusters; 1144 branches
    /// three ways on three choice dialogues with a hidden required objective; 1152 and 1153 shut each other by a scene signal
    /// (the client's 1153 log: "If you do, your mission for D'Ambrose will fail"), the designed result and not the patch
    /// notes' known issue (triage decision 1); 1153 is offered only while 1152 is in the log; 1155 and 1205 swap a crate for a
    /// ledger at a talk. A wrong branch, a flag that Melching cannot read, a fork that lets a player take both ends, an offer
    /// before its prerequisite or at the wrong NPC, progress lost at a reload or kept after an abandon, an item kept from a
    /// turn-in, or a turn-in that pays twice would each break them.
    ///
    /// The map: Ligo Crucible (1993), loaded as CrucibleOutpostIntrepidTests.EnterCrucible does (no upstream test does this
    /// for the Crucible). No mission of set 6 has a scene creature or a scene object (so no reload in the middle of a scene);
    /// the fork's signal is a scene sequence, so the tests tick the map after it, and reload between the talk and the tick.
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class CrucibleAwolCampTests
    {
        private const uint LastMen = CrucibleLastMenStandingV1.MissionId;
        private const uint Fort = CrucibleFortCalledIntrepidV1.MissionId;
        private const uint Wire = CrucibleWireV1.MissionId;
        private const uint Boss = CrucibleBossOfAllBossesV1.MissionId;
        private const uint Guns = CrucibleSmokingGunsV1.MissionId;
        private const uint Calendar = Cal.MissionId;
        private const uint StrayCats = Cats.MissionId;
        private const uint OtherSide = Coin.MissionId;
        private const uint Amends = M.MissionId;
        private const uint Revenge = M.RevengeMissionId;
        private const uint MissionItemFiller = 11519;      // as PlateauCampResistanceTests (inventory category 4)
        private const uint WeaponFiller = 3869;            // inventory category 1 (quest-test-pattern)
        private const uint ArmorFiller = 12748;            // Armor_T3_Bio_V01_CMN_Gloves_30_to_34 (inventory category 1)

        /// <summary>The waypoint of the Awol Camp (World DB teleporter 192).</summary>
        private static readonly Vector3 AwolWaypoint = new(-945.54f, 309.45f, 244.47f);
        private static readonly Vector3 LowerPad = new((float)G.LowerPadAt.X, (float)G.LowerPadAt.Y, (float)G.LowerPadAt.Z);

        [TestInitialize]
        public void Clock() => PlateauGiantKillerSquadTests.UseClock();

        [TestCleanup]
        public void Restore() => PlateauGiantKillerSquadTests.RestoreClock();

        /// <summary>
        /// Cheung at the Awol Camp, Deirdre in the Staal Centrum, Sherer and D'Ambrose at Outpost Intrepid (moved), Breiburg
        /// where upstream has him: on the navmesh with the client's names and packages, friendly, walked to from their area's
        /// waypoint (Deirdre also from the lower office teleporter).
        /// </summary>
        [TestMethod]
        public void TheAwolNpcsStandAtTheirPlaces()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var crucible = C.EnterCrucible(harness);
            foreach (var (id, name, package, from) in new[]
            {
                (A.Cheung, 9154U, 1292U, new[] { AwolWaypoint }), (A.Deirdre, 9163U, 1304U, new[] { C.StaalWaypoint, LowerPad }),
                (A.Sherer, 9158U, 1297U, new[] { C.OutpostWaypoint }), (A.DAmbrose, 9161U, 1301U, new[] { C.OutpostWaypoint }),
                (A.Breiburg, 9153U, 1293U, new[] { AwolWaypoint })
            })
            {
                var npc = Npc(crucible, id);
                Assert.AreEqual((name, package), (npc.NameId, npc.Npc.NpcPackageId), $"{id}");
                Assert.AreNotEqual(TargetCategory.Hostile, npc.TargetCategory);
                var at = A.At(id);
                Assert.IsLessThan(0.1f, Vector2.Distance(new((float)at.X, (float)at.Z), new(npc.Position.X, npc.Position.Z)), $"{id} at its place");
                AssertOnGround(crucible, npc.Position, $"{id}");
                foreach (var start in from)
                    Assert.IsTrue(crucible.NavMesh.FindPath(crucible.NavMesh.Nearest(start).Value, npc.Position, out var walked) != null && walked,
                        $"{id} is walked to from {start}");
            }
            var cheung = Npc(crucible, A.Cheung).Position;
            foreach (var friend in new[] { A.Breiburg, G.Melching })
                Assert.IsGreaterThan(10f, Vector2.Distance(new(cheung.X, cheung.Z), new(Npc(crucible, friend).Position.X, Npc(crucible, friend).Position.Z)),
                    $"Cheung clear of {friend}");
        }

        /// <summary>
        /// Calendar Girl, Deirdre helped: Sherer offers it (only he), the calendar comes with the accept (not with a full
        /// bag); Deirdre's choice "agree" opens D'Ambrose only; his choice "dead" sets the flag to Helped and opens Deirdre
        /// again; a reload keeps it; she signs; Sherer takes the calendar, 7 completes once and the mission is completeable
        /// once; Sherer pays once. Then Melching's Smoking Guns text is 178 ("the one that helped Deirdre"), and Stray Cats is
        /// D'Ambrose's to offer.
        /// </summary>
        [TestMethod]
        public void CalendarGirlHelpsDeirdreFakeHerDeath()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var crucible = C.EnterCrucible(harness);
            var (sherer, dambrose, deirdre) = (Npc(crucible, A.Sherer), Npc(crucible, A.DAmbrose), Npc(crucible, A.Deirdre));
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, dambrose, StrayCats), "Stray Cats waits for Calendar Girl");

            var offer = WildernessRanjaGorgeTests.Offer(harness, sherer, Calendar);
            CollectionAssert.AreEqual(new[] { Cal.EmpTorqueshellRifle, Cal.ElectricStaff, Cal.IcePolarityGun, Cal.FireShotgun },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, dambrose, Calendar), "not D'Ambrose's to give");
            WildernessAliaBranchesTests.FillCategory(harness, MissionItemFiller, 50);
            harness.MoveTo(sherer.Position);
            harness.Drain();
            Assert.IsFalse(harness.Manager.AcceptOfferedMission(harness.Client, sherer.EntityId, Calendar), "no room for the calendar");
            WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
            Accept(harness, sherer, Calendar);
            Assert.AreEqual(1U, CalendarsHeld(harness), "the calendar");
            Assert.AreEqual((MissionObjectiveState.Incomplete, MissionObjectiveState.Incomplete), (Objective(harness, Calendar, Cal.FindDeirdre),
                Objective(harness, Calendar, Cal.Outcome)), "1 and the hidden 7 open");
            Assert.IsEmpty(Topics(harness, sherer, Calendar), "Sherer hears nothing yet");
            Assert.IsEmpty(Choices(harness, dambrose, Calendar), "D'Ambrose has nothing to ask yet");
            CollectionAssert.AreEqual(new[] { (Cal.FindDeirdre, 1U) }, Choices(harness, deirdre, Calendar), "Deirdre's offer");
            var markers = harness.Manager.BuildStatusSnapshot(harness.Client.Player)[Calendar].ObjectivesList;
            Assert.IsTrue(markers.Single(objective => objective.ObjectiveId == Cal.FindDeirdre).IndicatorList
                .Any(marker => Vector2.Distance(new(marker.Position.X, marker.Position.Z), new(deirdre.Position.X, deirdre.Position.Z)) < 0.1f),
                "the marker on Deirdre");

            var refused = Choose(harness, deirdre, Calendar, Cal.FindDeirdre, Cal.Agree);
            Assert.AreEqual((MissionObjectiveState.Incomplete, 0U, 0U), (Objective(harness, Calendar, Cal.FindDeirdre), Held(harness, Cal.DogTagsTemplate),
                Flag(harness)), "a full bag refuses the dog tags: nothing changes");
            Assert.IsFalse(refused.OfType<ObjectiveCompletedPacket>().Any(packet => packet.MissionId == Calendar));
            WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
            var agreed = Choose(harness, deirdre, Calendar, Cal.FindDeirdre, Cal.Agree);
            Assert.AreEqual(1, agreed.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Calendar && packet.ObjectiveId == Cal.FindDeirdre));
            Assert.AreEqual((MissionObjectiveState.Incomplete, MissionObjectiveState.Inactive), (Objective(harness, Calendar, Cal.SpeakToDAmbrose),
                Objective(harness, Calendar, Cal.GiveCalendar)), "agree opens D'Ambrose, not the threat's signing");
            Assert.IsEmpty(Choices(harness, deirdre, Calendar), "asked once");
            Assert.AreEqual(0U, Flag(harness), "nothing decided yet");
            Assert.AreEqual(1U, Held(harness, Cal.DogTagsTemplate), "Deirdre's dog tags");

            Choose(harness, dambrose, Calendar, Cal.SpeakToDAmbrose, Cal.Dead);
            Assert.AreEqual(0U, Held(harness, Cal.DogTagsTemplate), "D'Ambrose takes the dog tags");
            Assert.AreEqual(Cal.Helped, Flag(harness), "helped Deirdre");
            Assert.AreEqual((MissionObjectiveState.Incomplete, MissionObjectiveState.Inactive), (Objective(harness, Calendar, Cal.ReturnToDeirdre),
                Objective(harness, Calendar, Cal.UnsignedCalendar)), "dead opens Deirdre again");
            WildernessRanjaGorgeTests.Reload(harness);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Calendar, Cal.ReturnToDeirdre), "kept at a reload");
            Assert.AreEqual(1U, CalendarsHeld(harness));
            harness.Drain();

            Talk(harness, deirdre, Calendar, Cal.ReturnToDeirdre);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Calendar, Cal.ReturnToSherer));
            Assert.IsFalse(harness.Client.Player.Missions[Calendar].Completeable);
            AssertHandInTopic(Conversation(harness, sherer), Calendar, Cal.ReturnToSherer);
            harness.MoveTo(sherer.Position);
            harness.Drain();
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, sherer.EntityId, Calendar, Cal.ReturnToSherer, 1));
            var signed = harness.Drain();
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Calendar, Cal.Outcome), "7 completes");
            Assert.IsFalse(signed.OfType<ObjectiveCompletedPacket>().Any(packet => packet.MissionId == Calendar && packet.ObjectiveId == Cal.Outcome),
                "7 is hidden: the client hears nothing of it");
            Assert.AreEqual(1, signed.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Calendar && packet.IsCompleteable),
                "MissionCompleteable once");
            Assert.AreEqual(0U, CalendarsHeld(harness), "Sherer takes the calendar");
            Assert.AreEqual(MissionObjectiveState.Inactive, Objective(harness, Calendar, Cal.ComfortSherer), "no comfort needed");

            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, dambrose.EntityId, Calendar, 0), "not D'Ambrose's to pay");
            FullBagWaits(harness, sherer, Calendar, 0, WeaponFiller);
            FailedTurnInPaysNothing(harness, sherer, Calendar, 0);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, sherer, Calendar, 0, Cal.Experience, Cal.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Calendar), "MissionRewarded once");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, Cal.EmpTorqueshellRifle), "the rifle chosen");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, sherer, Calendar), "done once");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, dambrose, StrayCats), "Stray Cats follows");

            for (var i = 0; i < 3; i++)
                WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);    // room for Smoking Guns' three shipments
            Assert.AreEqual(CrucibleSmokingGunsV1.HelpedVariant, MelchingVariant(harness), "Melching: \"the one that helped Deirdre\"");
        }

        /// <summary>
        /// Calendar Girl, Deirdre ratted out: D'Ambrose's "alive" sets the flag to RattedOut and opens the unsigned calendar;
        /// at Sherer the truth takes the calendar and ends it (7 by the scene step, once), the lie takes it and opens 9, whose
        /// talk ends it. Sherer pays. Melching's text is then 177.
        /// </summary>
        [TestMethod]
        [DataRow(1U, DisplayName = "the truth")]
        [DataRow(2U, DisplayName = "the lie")]
        public void CalendarGirlRatsOutDeirdre(uint answer)
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var crucible = C.EnterCrucible(harness);
            var (sherer, dambrose, deirdre) = (Npc(crucible, A.Sherer), Npc(crucible, A.DAmbrose), Npc(crucible, A.Deirdre));
            Accept(harness, sherer, Calendar);
            Choose(harness, deirdre, Calendar, Cal.FindDeirdre, Cal.Agree);
            Choose(harness, dambrose, Calendar, Cal.SpeakToDAmbrose, Cal.Alive);
            Assert.AreEqual(1U, Held(harness, Cal.DogTagsTemplate), "ratted out: the player keeps the dog tags");
            Assert.AreEqual(Cal.RattedOut, Flag(harness), "ratted out");
            Assert.AreEqual((MissionObjectiveState.Incomplete, MissionObjectiveState.Inactive), (Objective(harness, Calendar, Cal.UnsignedCalendar),
                Objective(harness, Calendar, Cal.ReturnToDeirdre)), "alive opens the unsigned calendar");
            Assert.IsEmpty(Topics(harness, deirdre, Calendar), "Deirdre has nothing more");
            Assert.IsEmpty(Topics(harness, sherer, Calendar), "Sherer's is a choice");
            CollectionAssert.AreEqual(new[] { (Cal.UnsignedCalendar, 1U) }, Choices(harness, sherer, Calendar));
            WildernessRanjaGorgeTests.Reload(harness);
            harness.Drain();

            var told = Choose(harness, sherer, Calendar, Cal.UnsignedCalendar, answer);
            Assert.AreEqual(0U, CalendarsHeld(harness), "Sherer takes the unsigned calendar");
            if (answer == Cal.Truth)
            {
                Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Calendar, Cal.Outcome), "the truth ends it");
                Assert.AreEqual(MissionObjectiveState.Inactive, Objective(harness, Calendar, Cal.ComfortSherer), "no comfort");
                Assert.AreEqual(1, told.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Calendar && packet.IsCompleteable),
                    "MissionCompleteable once");
            }
            else
            {
                Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Calendar, Cal.Outcome), "the lie: not yet");
                Assert.IsFalse(harness.Client.Player.Missions[Calendar].Completeable);
                Talk(harness, sherer, Calendar, Cal.ComfortSherer);
                Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Calendar, Cal.Outcome), "comforted");
            }
            Assert.IsTrue(harness.Client.Player.Missions[Calendar].Completeable);
            Assert.AreEqual(MissionObjectiveState.Inactive, Objective(harness, Calendar, Cal.ReturnToSherer), "no signed calendar");
            FailedTurnInPaysNothing(harness, sherer, Calendar, 1);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, sherer, Calendar, 1, Cal.Experience, Cal.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Calendar), "MissionRewarded once");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, Cal.ElectricStaff), "the staff chosen");
            Assert.AreEqual(0U, Held(harness, Cal.DogTagsTemplate), "the kept dog tags go at the turn-in");

            Assert.AreEqual(CrucibleSmokingGunsV1.RattedOutVariant, MelchingVariant(harness), "Melching: \"ratted out Deridre\"");
        }

        /// <summary>
        /// Calendar Girl, Deirdre threatened: her choice "threaten" sets the flag to Threatened and opens her signing (8), not
        /// D'Ambrose; she signs; Sherer takes the calendar. Melching's text is then 203 ("Who the hell are you?").
        /// </summary>
        [TestMethod]
        public void CalendarGirlThreatensDeirdre()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var crucible = C.EnterCrucible(harness);
            var (sherer, dambrose, deirdre) = (Npc(crucible, A.Sherer), Npc(crucible, A.DAmbrose), Npc(crucible, A.Deirdre));
            Accept(harness, sherer, Calendar);
            Choose(harness, deirdre, Calendar, Cal.FindDeirdre, Cal.Threaten);
            Assert.AreEqual(Cal.Threatened, Flag(harness), "threatened");
            Assert.AreEqual((MissionObjectiveState.Incomplete, MissionObjectiveState.Inactive), (Objective(harness, Calendar, Cal.GiveCalendar),
                Objective(harness, Calendar, Cal.SpeakToDAmbrose)), "the threat opens the signing, not D'Ambrose");
            Assert.IsEmpty(Choices(harness, dambrose, Calendar), "D'Ambrose has nothing to ask");
            Talk(harness, deirdre, Calendar, Cal.GiveCalendar);
            Talk(harness, sherer, Calendar, Cal.ReturnToSherer);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Calendar, Cal.Outcome));
            Assert.AreEqual(0U, CalendarsHeld(harness));
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, sherer, Calendar, 3, Cal.Experience, Cal.Credits);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, Cal.FireShotgun), "the shotgun chosen");

            Assert.AreEqual(CrucibleSmokingGunsV1.NeitherVariant, MelchingVariant(harness), "Melching: \"Who the hell are you?\"");
        }

        /// <summary>An abandon takes the calendar and keeps the flag; taken again, a new calendar, Deirdre's choice again, and the new deed sets the flag again.</summary>
        [TestMethod]
        public void CalendarGirlAbandonedTakesTheCalendar()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var crucible = C.EnterCrucible(harness);
            var (sherer, dambrose, deirdre) = (Npc(crucible, A.Sherer), Npc(crucible, A.DAmbrose), Npc(crucible, A.Deirdre));
            Accept(harness, sherer, Calendar);
            Choose(harness, deirdre, Calendar, Cal.FindDeirdre, Cal.Agree);
            Choose(harness, dambrose, Calendar, Cal.SpeakToDAmbrose, Cal.Alive);
            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Calendar));
            harness.Drain();
            Assert.AreEqual(0U, CalendarsHeld(harness), "the calendar goes");
            Assert.AreEqual(0U, Held(harness, Cal.DogTagsTemplate), "the dog tags go");
            Assert.AreEqual(Cal.RattedOut, Flag(harness), "the deed stays");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, sherer, Calendar), "offered again after an abandon");
            Accept(harness, sherer, Calendar);
            Assert.AreEqual(1U, CalendarsHeld(harness), "a new calendar");
            CollectionAssert.AreEqual(new[] { (Cal.FindDeirdre, 1U) }, Choices(harness, deirdre, Calendar), "Deirdre asks again");
            Choose(harness, deirdre, Calendar, Cal.FindDeirdre, Cal.Agree);
            Choose(harness, dambrose, Calendar, Cal.SpeakToDAmbrose, Cal.Dead);
            Assert.AreEqual(Cal.Helped, Flag(harness), "the new deed");
        }

        /// <summary>
        /// Stray Cats after Calendar Girl: D'Ambrose offers it (only he); Cheung's talk opens the report; Breiburg offers The
        /// Other Side Of The Coin while Stray Cats is in the log, and it is taken; the report to D'Ambrose (reload between the
        /// talk and the tick) fails The Other Side Of The Coin once the map ticks; D'Ambrose pays once. Then Breiburg offers
        /// Making Amends, not Sweet Revenge, and not The Other Side Of The Coin again (cleared).
        /// </summary>
        [TestMethod]
        public void StrayCatsBetraysTheCampAndClosesTheOtherSide()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            C.SeedCompleted(harness, Calendar);
            var crucible = C.EnterCrucible(harness);
            var (dambrose, sherer) = (Npc(crucible, A.DAmbrose), Npc(crucible, A.Sherer));
            var offer = WildernessRanjaGorgeTests.Offer(harness, dambrose, StrayCats);
            CollectionAssert.AreEqual(new[] { Cats.TorqueshellRifle, Cats.ElectricPolarityGun, Cats.PulseCannon, Cats.LaserRifle },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, sherer, StrayCats), "not Sherer's to give");
            Accept(harness, dambrose, StrayCats);
            Assert.IsEmpty(Topics(harness, dambrose, StrayCats), "D'Ambrose waits for the camp");

            crucible = C.EnterCrucible(harness, AwolWaypoint);
            var (cheung, breiburg) = (Npc(crucible, A.Cheung), Npc(crucible, A.Breiburg));
            Assert.IsEmpty(Topics(harness, breiburg, StrayCats), "Breiburg is not the leader");
            Talk(harness, cheung, StrayCats, Cats.LocateLeader);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, StrayCats, Cats.Report));
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, breiburg, OtherSide), "Breiburg asks, Stray Cats in the log");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, cheung, OtherSide), "not Cheung's to give");
            Accept(harness, breiburg, OtherSide);
            CollectionAssert.AreEqual(new[] { (Coin.Decision, 1U) }, Choices(harness, cheung, OtherSide), "Cheung asks");

            crucible = C.EnterCrucible(harness);
            dambrose = Npc(crucible, A.DAmbrose);
            Talk(harness, dambrose, StrayCats, Cats.Report);
            Assert.IsTrue(harness.Client.Player.Missions[StrayCats].Completeable);
            WildernessRanjaGorgeTests.Reload(harness);
            harness.Drain();
            MarshesPreoDasTests.Tick(harness, crucible, 250);
            Assert.AreEqual(MissionState.Failed, harness.Client.Player.Missions[OtherSide].State, "the camp is betrayed");
            Assert.AreEqual(MissionObjectiveState.Failed, Objective(harness, OtherSide, Coin.Decision));

            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, sherer.EntityId, StrayCats, 2), "not Sherer's to pay");
            FullBagWaits(harness, dambrose, StrayCats, 2, WeaponFiller);
            FailedTurnInPaysNothing(harness, dambrose, StrayCats, 2);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, dambrose, StrayCats, 2, Cats.Experience, Cats.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == StrayCats), "MissionRewarded once");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, Cats.PulseCannon), "the cannon chosen");

            Assert.IsTrue(harness.Manager.TryClear(harness.Client, OtherSide), "the failed 1153 cleared");
            harness.Drain();
            crucible = C.EnterCrucible(harness, AwolWaypoint);
            breiburg = Npc(crucible, A.Breiburg);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, breiburg, OtherSide), "the camp is betrayed for good");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, breiburg, Amends), "Making Amends");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, breiburg, Revenge), "not Sweet Revenge");
        }

        /// <summary>
        /// The Other Side Of The Coin: not offered before Stray Cats is taken; Cheung's refusal fails it at once; cleared, Breiburg
        /// offers it again; the promise completes it (and only that) and fails Stray Cats once the map ticks; Breiburg (not
        /// Cheung) pays once. Then D'Ambrose does not offer Stray Cats again (cleared), and Breiburg offers Sweet Revenge, not
        /// Making Amends.
        /// </summary>
        [TestMethod]
        public void TheOtherSideOfTheCoinKeepsTheSecretAndFailsStrayCats()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            C.SeedCompleted(harness, Calendar);
            var crucible = C.EnterCrucible(harness, AwolWaypoint);
            var (cheung, breiburg) = (Npc(crucible, A.Cheung), Npc(crucible, A.Breiburg));
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, breiburg, OtherSide), "it waits for Stray Cats in the log");
            crucible = C.EnterCrucible(harness);
            Accept(harness, Npc(crucible, A.DAmbrose), StrayCats);
            crucible = C.EnterCrucible(harness, AwolWaypoint);
            (cheung, breiburg) = (Npc(crucible, A.Cheung), Npc(crucible, A.Breiburg));

            var offer = WildernessRanjaGorgeTests.Offer(harness, breiburg, OtherSide);
            CollectionAssert.AreEqual(new[] { Coin.ElectricBlade, Coin.ElectricInjectorGun, Coin.FirePolarityGun, Coin.RocketLauncher },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Accept(harness, breiburg, OtherSide);
            Assert.IsEmpty(Topics(harness, breiburg, OtherSide), "Breiburg waits for the decision");

            // The refusal fails it; cleared, it is offered again (Stray Cats still in the log).
            var refused = Choose(harness, cheung, OtherSide, Coin.Decision, Coin.Refuse);
            Assert.AreEqual(1, refused.OfType<ObjectiveFailedPacket>().Count(packet => packet.MissionId == OtherSide && packet.ObjectiveId == Coin.Decision),
                "ObjectiveFailed once");
            Assert.AreEqual(MissionState.Failed, harness.Client.Player.Missions[OtherSide].State, "refused: it fails");
            MarshesPreoDasTests.Tick(harness, crucible, 250);
            Assert.AreEqual(MissionState.Active, harness.Client.Player.Missions[StrayCats].State, "Stray Cats goes on");
            Assert.IsTrue(harness.Manager.TryClear(harness.Client, OtherSide));
            harness.Drain();
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, breiburg, OtherSide), "offered again");
            Accept(harness, breiburg, OtherSide);

            var promised = Choose(harness, cheung, OtherSide, Coin.Decision, Coin.Promise);
            Assert.AreEqual(1, promised.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == OtherSide && packet.IsCompleteable),
                "MissionCompleteable once");
            Assert.IsEmpty(Choices(harness, cheung, OtherSide), "asked once");
            WildernessRanjaGorgeTests.Reload(harness);
            harness.Drain();
            MarshesPreoDasTests.Tick(harness, crucible, 250);
            Assert.AreEqual(MissionState.Failed, harness.Client.Player.Missions[StrayCats].State, "the mission for D'Ambrose fails");
            Assert.AreEqual(MissionObjectiveState.Failed, Objective(harness, StrayCats, Cats.LocateLeader));
            Assert.IsTrue(harness.Client.Player.Missions[OtherSide].Completeable, "the promise stands");

            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, cheung.EntityId, OtherSide, 3), "Breiburg debriefs");
            FullBagWaits(harness, breiburg, OtherSide, 3, WeaponFiller);
            FailedTurnInPaysNothing(harness, breiburg, OtherSide, 3);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, breiburg, OtherSide, 3, Coin.Experience, Coin.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == OtherSide), "MissionRewarded once");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, Coin.RocketLauncher), "the launcher chosen");

            Assert.IsTrue(harness.Manager.TryClear(harness.Client, StrayCats), "the failed 1152 cleared");
            harness.Drain();
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, breiburg, Revenge), "Sweet Revenge");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, breiburg, Amends), "not Making Amends");
            crucible = C.EnterCrucible(harness);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, Npc(crucible, A.DAmbrose), StrayCats), "Stray Cats cannot be regained");
            Assert.IsFalse(harness.Manager.AcceptOfferedMission(harness.Client, Npc(crucible, A.DAmbrose).EntityId, StrayCats), "nor taken");
        }

        /// <summary>
        /// An abandon of The Other Side Of The Coin before the decision changes nothing on Stray Cats; taken again, it is the
        /// same choice. An abandon of Stray Cats closes The Other Side Of The Coin's offer (Stray Cats no longer in the log).
        /// </summary>
        [TestMethod]
        public void TheForkAbandonedLeavesBothWaysOpen()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            C.SeedCompleted(harness, Calendar);
            var crucible = C.EnterCrucible(harness);
            Accept(harness, Npc(crucible, A.DAmbrose), StrayCats);
            crucible = C.EnterCrucible(harness, AwolWaypoint);
            var (cheung, breiburg) = (Npc(crucible, A.Cheung), Npc(crucible, A.Breiburg));
            Accept(harness, breiburg, OtherSide);
            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, OtherSide));
            harness.Drain();
            Assert.AreEqual(MissionState.Active, harness.Client.Player.Missions[StrayCats].State, "Stray Cats stays");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, breiburg, OtherSide), "offered again after an abandon");
            Accept(harness, breiburg, OtherSide);
            CollectionAssert.AreEqual(new[] { (Coin.Decision, 1U) }, Choices(harness, cheung, OtherSide), "the same choice");
            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, OtherSide));
            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, StrayCats));
            harness.Drain();
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, breiburg, OtherSide), "no Stray Cats in the log");
            crucible = C.EnterCrucible(harness);
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, Npc(crucible, A.DAmbrose), StrayCats), "Stray Cats offered again");
        }

        /// <summary>
        /// Making Amends (after Stray Cats) and Sweet Revenge (after The Other Side Of The Coin): Breiburg offers the one of the
        /// player's end only; the crate comes with the accept; Munit hears the trade only (DeLisi and Breiburg wait), takes the
        /// crate and gives the ledger; a reload keeps it; DeLisi keeps the ledger; Breiburg hears it; the mission is completeable
        /// once and Breiburg pays once.
        /// </summary>
        [TestMethod]
        [DataRow(1155U, DisplayName = "Making Amends")]
        [DataRow(1205U, DisplayName = "Sweet Revenge")]
        public void BreiburgsLedgerDiscreditsDAmbrose(uint id)
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var other = id == Amends ? Revenge : Amends;
            C.SeedCompleted(harness, LastMen, Fort, Wire, Boss, Calendar);
            var crucible = C.EnterCrucible(harness, AwolWaypoint);
            var breiburg = Npc(crucible, A.Breiburg);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, breiburg, id), "it waits for its end of the fork");
            C.SeedCompleted(harness, id == Amends ? StrayCats : OtherSide);
            crucible = C.EnterCrucible(harness, AwolWaypoint);
            breiburg = Npc(crucible, A.Breiburg);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, breiburg, other), "not the other end");
            var offer = WildernessRanjaGorgeTests.Offer(harness, breiburg, id);
            CollectionAssert.AreEqual(M.Missions.Single(entry => entry.Mission == id).Items,
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, Npc(crucible, A.Cheung), id), "not Cheung's to give");
            Accept(harness, breiburg, id);
            Assert.AreEqual((1U, 0U), (Held(harness, M.CrateTemplate), Held(harness, M.LedgerTemplate)), "the crate");
            Assert.IsEmpty(Topics(harness, breiburg, id), "Breiburg waits");

            crucible = C.EnterCrucible(harness, C.StaalWaypoint);
            var munit = Npc(crucible, A.Munit);
            CollectionAssert.AreEqual(new[] { M.Trade }, Topics(harness, munit, id), "Munit hears the trade");
            Talk(harness, munit, id, M.Trade);
            Assert.AreEqual((0U, 1U), (Held(harness, M.CrateTemplate), Held(harness, M.LedgerTemplate)), "the crate for the ledger");
            WildernessRanjaGorgeTests.Reload(harness);
            Assert.AreEqual(1U, Held(harness, M.LedgerTemplate), "kept at a reload");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, id, M.ShowLedger));
            harness.Drain();

            crucible = C.EnterCrucible(harness);
            var delisi = Npc(crucible, A.DeLisi);
            Talk(harness, delisi, id, M.ShowLedger);
            Assert.AreEqual(0U, Held(harness, M.LedgerTemplate), "DeLisi keeps the ledger");

            crucible = C.EnterCrucible(harness, AwolWaypoint);
            breiburg = Npc(crucible, A.Breiburg);
            AssertHandInTopic(Conversation(harness, breiburg), id, M.InformBreiburg);
            harness.MoveTo(breiburg.Position);
            harness.Drain();
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, breiburg.EntityId, id, M.InformBreiburg, 1));
            Assert.AreEqual(1, harness.Drain().OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == id && packet.IsCompleteable),
                "MissionCompleteable once");
            FullBagWaits(harness, breiburg, id, 2, ArmorFiller);
            FailedTurnInPaysNothing(harness, breiburg, id, 2);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, breiburg, id, 2, M.Experience, M.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == id), "MissionRewarded once");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, M.Missions.Single(entry => entry.Mission == id).Items[2]), "the third item chosen");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, breiburg, id), "done once");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, breiburg, other), "and never the other");
        }

        /// <summary>
        /// The main line of Stray Cats: 1153 is never taken; Cheung, the report to D'Ambrose, a tick (the signal to a 1153 not
        /// held changes nothing); D'Ambrose pays. Then Breiburg offers Making Amends, not Sweet Revenge, and not The Other Side
        /// Of The Coin (Stray Cats no longer in the log).
        /// </summary>
        [TestMethod]
        public void StrayCatsMainLineLeadsToMakingAmends()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            C.SeedCompleted(harness, LastMen, Fort, Wire, Boss, Calendar);
            var crucible = C.EnterCrucible(harness);
            Accept(harness, Npc(crucible, A.DAmbrose), StrayCats);
            crucible = C.EnterCrucible(harness, AwolWaypoint);
            Talk(harness, Npc(crucible, A.Cheung), StrayCats, Cats.LocateLeader);
            crucible = C.EnterCrucible(harness);
            var dambrose = Npc(crucible, A.DAmbrose);
            Talk(harness, dambrose, StrayCats, Cats.Report);
            MarshesPreoDasTests.Tick(harness, crucible, 250);
            Assert.IsFalse(harness.Client.Player.Missions.ContainsKey(OtherSide), "1153 never taken");
            Assert.AreEqual(MissionState.Active, harness.Client.Player.Missions[StrayCats].State, "the signal does not touch Stray Cats");
            Assert.IsTrue(harness.Client.Player.Missions[StrayCats].Completeable);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, dambrose, StrayCats, 0, Cats.Experience, Cats.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == StrayCats), "MissionRewarded once");

            crucible = C.EnterCrucible(harness, AwolWaypoint);
            var breiburg = Npc(crucible, A.Breiburg);
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, breiburg, Amends), "Making Amends");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, breiburg, Revenge), "not Sweet Revenge");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, breiburg, OtherSide), "Stray Cats is no longer in the log");
        }

        /// <summary>
        /// The mirror: Stray Cats taken (so Breiburg offers 1153), 1153 taken, then Stray Cats abandoned; the promise completes
        /// 1153 (the signal to a 1152 not held changes nothing); Breiburg pays. Then he offers Sweet Revenge, not Making Amends,
        /// and D'Ambrose does not offer Stray Cats again.
        /// </summary>
        [TestMethod]
        public void TheOtherSideOfTheCoinAfterStrayCatsAbandonedLeadsToSweetRevenge()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            C.SeedCompleted(harness, LastMen, Fort, Wire, Boss, Calendar);
            var crucible = C.EnterCrucible(harness);
            Accept(harness, Npc(crucible, A.DAmbrose), StrayCats);
            crucible = C.EnterCrucible(harness, AwolWaypoint);
            var (cheung, breiburg) = (Npc(crucible, A.Cheung), Npc(crucible, A.Breiburg));
            Accept(harness, breiburg, OtherSide);
            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, StrayCats));
            harness.Drain();
            Assert.AreEqual(MissionState.Active, harness.Client.Player.Missions[OtherSide].State, "1153 stays in the log");

            Choose(harness, cheung, OtherSide, Coin.Decision, Coin.Promise);
            MarshesPreoDasTests.Tick(harness, crucible, 250);
            Assert.IsFalse(harness.Client.Player.Missions.ContainsKey(StrayCats), "no Stray Cats to fail");
            Assert.IsTrue(harness.Client.Player.Missions[OtherSide].Completeable, "the promise stands");
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, breiburg, OtherSide, 0, Coin.Experience, Coin.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == OtherSide), "MissionRewarded once");

            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, breiburg, Revenge), "Sweet Revenge");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, breiburg, Amends), "not Making Amends");
            crucible = C.EnterCrucible(harness);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, Npc(crucible, A.DAmbrose), StrayCats), "Stray Cats cannot be regained");
        }

        /// <summary>
        /// The trade at Munit with a bag full except the crate: the crate goes and the ledger comes in the one talk, so the
        /// crate's slot holds the ledger; nothing is lost.
        /// </summary>
        [TestMethod]
        public void MunitsTradeFitsABagFullExceptTheCrate()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            C.SeedCompleted(harness, LastMen, Fort, Wire, Boss, Calendar, StrayCats);
            var crucible = C.EnterCrucible(harness, AwolWaypoint);
            WildernessAliaBranchesTests.FillCategory(harness, MissionItemFiller, 50);
            WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
            Accept(harness, Npc(crucible, A.Breiburg), Amends);
            Assert.AreEqual(1U, Held(harness, M.CrateTemplate), "the crate takes the last slot");
            crucible = C.EnterCrucible(harness, C.StaalWaypoint);
            Talk(harness, Npc(crucible, A.Munit), Amends, M.Trade);
            Assert.AreEqual((0U, 1U), (Held(harness, M.CrateTemplate), Held(harness, M.LedgerTemplate)), "the crate for the ledger");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Amends, M.ShowLedger));
        }

        /// <summary>An abandon after the trade takes the ledger (the crate is gone); taken again, a new crate and the trade again.</summary>
        [TestMethod]
        public void MakingAmendsAbandonedTakesTheLedger()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            C.SeedCompleted(harness, LastMen, Fort, Wire, Boss, Calendar, StrayCats);
            var crucible = C.EnterCrucible(harness, AwolWaypoint);
            var breiburg = Npc(crucible, A.Breiburg);
            WildernessAliaBranchesTests.FillCategory(harness, MissionItemFiller, 50);
            harness.MoveTo(breiburg.Position);
            harness.Drain();
            Assert.IsFalse(harness.Manager.AcceptOfferedMission(harness.Client, breiburg.EntityId, Amends), "no room for the crate");
            WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
            Accept(harness, breiburg, Amends);
            crucible = C.EnterCrucible(harness, C.StaalWaypoint);
            var munit = Npc(crucible, A.Munit);
            Talk(harness, munit, Amends, M.Trade);
            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Amends));
            harness.Drain();
            Assert.AreEqual((0U, 0U), (Held(harness, M.CrateTemplate), Held(harness, M.LedgerTemplate)), "nothing kept");
            crucible = C.EnterCrucible(harness, AwolWaypoint);
            breiburg = Npc(crucible, A.Breiburg);
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, breiburg, Amends), "offered again after an abandon");
            Accept(harness, breiburg, Amends);
            Assert.AreEqual(1U, Held(harness, M.CrateTemplate), "a new crate");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Amends, M.Trade), "the old trade does not count");
        }

        /// <summary>The choice <paramref name="choice"/> of <paramref name="mission"/>'s objective at <paramref name="npc"/> (as MarshesGunControlTests.Choose); its packets.</summary>
        private static List<object> Choose(WildernessRuntimeTestHarness harness, Creature npc, uint mission, uint objective, uint choice)
        {
            var npcs = new NpcManager(harness, harness.Manager);
            harness.MoveTo(npc.Position);
            harness.Drain();
            npcs.RequestNpcConverse(harness.Client, new RequestNPCConversePacket { EntityId = npc.EntityId });
            npcs.PerformNPCChoice(harness.Client, new PerformNPCChoicePacket
            {
                EntityId = npc.EntityId, MissionId = mission, ObjectiveId = objective, PlayerFlagId = 1, ChoiceIdx = (int)choice
            });
            return harness.Drain().Cast<object>().ToList();
        }

        /// <summary>The choice topics of <paramref name="mission"/> that <paramref name="npc"/> offers now (as MarshesGunControlTests).</summary>
        private static (uint, uint)[] Choices(WildernessRuntimeTestHarness harness, Creature npc, uint mission) =>
            Conversation(harness, npc).TryGetValue(ConversationType.ObjectiveChoice, out var value)
                ? ((List<ChoiceObjectives>)value).Where(topic => topic.MissionId == mission)
                    .Select(topic => ((uint)topic.ObjectiveId, (uint)topic.PlayerFlagId)).ToArray()
                : System.Array.Empty<(uint, uint)>();

        /// <summary>Calendar Girl's flag 1144001 as the character database holds it (0: none).</summary>
        private static uint Flag(WildernessRuntimeTestHarness harness)
        {
            using var unit = harness.CreateChar();
            return unit.CharacterFlags.Get(harness.Client.Player.Id).TryGetValue(Cal.Flag, out var value) ? value : 0;
        }

        /// <summary>The variant of Melching's Smoking Guns topic: Smoking Guns taken (its chain seeded) at Munit, Melching asked.</summary>
        private static uint MelchingVariant(WildernessRuntimeTestHarness harness)
        {
            C.SeedCompleted(harness, LastMen, Fort, Wire, Boss);
            var crucible = C.EnterCrucible(harness, C.StaalWaypoint);
            Accept(harness, Npc(crucible, N.Munit), Guns);
            crucible = C.EnterCrucible(harness, AwolWaypoint);
            var melching = Npc(crucible, G.Melching);
            Assert.IsTrue(Conversation(harness, melching).TryGetValue(ConversationType.ObjectiveComplete, out var value), "Melching has a topic");
            var topic = ((List<CompleteableObjectives>)value).Single(topic => topic.MissionId == Guns);
            Assert.AreEqual(CrucibleSmokingGunsV1.Awol, (uint)topic.ObjectiveId);
            return (uint)topic.PlayerFlagId;
        }

        private static uint CalendarsHeld(WildernessRuntimeTestHarness harness) => Held(harness, Cal.CalendarTemplate);

        private static uint Held(WildernessRuntimeTestHarness harness, uint template) => WildernessRanjaGorgeTests.Held(harness, template);
    }
}
