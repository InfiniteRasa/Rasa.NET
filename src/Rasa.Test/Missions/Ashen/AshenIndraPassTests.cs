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
using Rasa.Packets.MapChannel.Client;
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
    using N = AshenIndraPassNpcsV1;
    using Missing = AshenMissingScientistsV1;
    using Storms = AshenGodOfStormsV1;
    using Bonds = AshenBondsOfBloodV1;
    using P = MarshesPreoDasTests;
    using E = MarshesEscortsTests;
    using WO = AshenWhiteOasisTests;
    using F = MarshesClientFloorTests;
    using G = AshenFixGV1;

    /// <summary>
    /// Ashen Desert set 13, Indra Pass, played through as a player plays it: Surveyor Odesha's Missing Scientists (1280: the
    /// codes off Researcher Veyon's body in the pass) and its two follow-ups, one per player: Odesha's God of Storms (1281: the
    /// ten Indra Lightning Resistors) and Researcher Endala Berish's The Bonds of Blood (1282: her brother Maiek, found dead by
    /// the Conscription Base's generators). With them the set's places: the two NPCs by the Indra Pass waypoint, the bodies and
    /// the resistors, each walked to on the client's floor.
    ///
    /// Why these tests: the follow-ups are exclusive (TaRapedia: once God of Storms is taken, Endala's is not seen; the client
    /// logs: the tenth resistor loses Maiek, finding Maiek loses the resistors), so an offer to the wrong side, a fork that does
    /// not close, a resistor or body that counts twice or for the wrong objective, codes not given or not taken, progress lost
    /// at a reload, a turn-in that pays twice or on a full bag, or a place the client's floor does not reach or whose walk
    /// passes a camp would each break them.
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class AshenIndraPassTests
    {
        private const uint ArmorFiller = 12748;            // Armor_T3_Bio_V01_CMN_Gloves_30_to_34 (inventory category 1)
        private const uint WeaponFiller = 3869;            // inventory category 1 (quest-test-pattern)
        private const uint MissionItemFiller = 11519;      // as PlateauCampResistanceTests (inventory category 4)
        private const uint ClientCorpseClass = 7928;       // UsableTreasureDispBrannCorpseMaleV01
        private const uint ClientVeyonName = 67;           // usablenameoverride LIGO_ASHEN_DEAD_ISHIN_VEYON "Corpse of Researcher Veyon"
        private const uint ClientResistorClass = 10196;    // UsableTwoStateBrannLightningResistorV01
        private const uint ClientResistorName = 68;        // usablenameoverride LIGO_ASHEN_INDRA_RESISTOR "Indra Lightning Resistor"
        private const uint ClientUseArgument = 1;          // usabledata lookup[7928][0] and lookup[10196][0] are None
        private const uint Closed = 200, Opened = 201;     // USE_TD_STATE_CLOSED / OPENED
        private const uint Off = 55, On = 56;              // USE_TS_STATE_0 / 1
        private const uint ClientCodesTemplate = 97476;    // client item class 25475 "Indra Lightning Resistor Codes"
        private const uint ClientEndalaPackage = 1475;     // objectiveconversation 1282,2
        private const uint ClientEndalaTalk = 10333;       // "He was already dead?"

        internal static readonly Vector3 IndraWaypoint = new(262.19f, 315.10f, 344.38f);
        private static readonly Vector3 Ashoka = AshenLeadInsTests.AshokaWaypoint;

        /// <summary>Where a player comes in: the valley's south entry, the end of set 10's way from the White Oasis Post waypoint (fix G: the Indra Pass waypoint's floor is joined to no way in, N23).</summary>
        internal static readonly Vector3 Arrival = new(152.82f, 312.80f, 459.30f);

        /// <summary>The ten resistors where the server has them (set 13's, four moved by fix G).</summary>
        private static readonly Rasa.Missions.Scenes.ScenePosition[] ResistorsAt = G.ResistorsAt();

        [TestInitialize]
        public void Clock() => PlateauGiantKillerSquadTests.UseClock();

        [TestCleanup]
        public void Restore() => PlateauGiantKillerSquadTests.RestoreClock();

        /// <summary>
        /// The fork: before Missing Scientists neither follow-up is offered; after it Odesha offers God of Storms and Endala The
        /// Bonds of Blood (each only by its own NPC). God of Storms taken, Endala does not offer hers (after a reload neither);
        /// abandoned, she does again. The Bonds of Blood taken, Odesha does not offer God of Storms; done, never again (after a
        /// reload too). Missing Scientists is Odesha's only.
        /// </summary>
        [TestMethod]
        public void TheForkOffersOneSideAtATime()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var desert = AshenLeadInsTests.EnterAshen(harness, Arrival);
            var odesha = Npc(desert, N.Odesha);
            var endala = Npc(desert, N.Endala);
            bool Offers(Creature npc, uint mission) => PlateauRetreadCampTests.Offers(harness, npc, mission);
            Assert.IsTrue(Offers(odesha, Missing.MissionId), "Odesha offers Missing Scientists");
            Assert.IsFalse(Offers(endala, Missing.MissionId), "not Endala's");
            foreach (var (npc, mission) in new[] { (odesha, Storms.MissionId), (endala, Storms.MissionId), (odesha, Bonds.MissionId), (endala, Bonds.MissionId) })
                Assert.IsFalse(Offers(npc, mission), $"{npc.DbId} does not offer {mission} before 1280");

            AshenRuntime.SeedCompleted(harness, Missing.MissionId);
            Assert.IsTrue(Offers(odesha, Storms.MissionId), "Odesha offers God of Storms");
            Assert.IsTrue(Offers(endala, Bonds.MissionId), "Endala offers The Bonds of Blood");
            Assert.IsFalse(Offers(endala, Storms.MissionId), "not Endala's");
            Assert.IsFalse(Offers(odesha, Bonds.MissionId), "not Odesha's");

            Accept(harness, odesha, Storms.MissionId);
            Assert.IsFalse(Offers(endala, Bonds.MissionId), "God of Storms held: Endala's not offered");
            Assert.IsFalse(harness.Manager.AcceptOfferedMission(harness.Client, endala.EntityId, Bonds.MissionId), "nor taken");
            AshenRuntime.Reload(harness);
            harness.Drain();
            Assert.IsFalse(Offers(endala, Bonds.MissionId), "after a reload neither");
            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Storms.MissionId));
            harness.Drain();
            Assert.IsTrue(Offers(endala, Bonds.MissionId), "God of Storms abandoned: Endala's again");

            Accept(harness, endala, Bonds.MissionId);
            Assert.IsFalse(Offers(odesha, Storms.MissionId), "The Bonds of Blood held: God of Storms not offered");
            Assert.IsFalse(harness.Manager.AcceptOfferedMission(harness.Client, odesha.EntityId, Storms.MissionId), "nor taken");
            FindMaiek(harness, desert);
            Talk(harness, endala, Bonds.MissionId, Bonds.Talk);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, endala, Bonds.MissionId, 0, Bonds.Experience, Bonds.Credits);
            harness.Drain();
            foreach (var when in new[] { "The Bonds of Blood done", "after a reload" })
            {
                Assert.IsFalse(Offers(odesha, Storms.MissionId), $"{when}: God of Storms not offered");
                Assert.IsFalse(Offers(endala, Bonds.MissionId), $"{when}: done once");
                AshenRuntime.Reload(harness);
                harness.Drain();
            }
        }

        /// <summary>
        /// Missing Scientists: Odesha offers the four armour pieces; accepted, she hears no objective; a marker on Veyon's body,
        /// on Ashen Desert. No body before the mission; the player's own body (the client's Brann corpse named "Corpse of
        /// Researcher Veyon", closed, on the ground, the client's use argument); a full bag leaves the codes on the body (it
        /// waits); one search gives the codes, completes 1 once and makes the mission completeable once; a second search
        /// nothing; kept at a reload. A full bag waits at the turn-in, a failed save pays nothing and keeps the codes; Odesha
        /// pays the piece chosen, once, and takes the codes.
        /// </summary>
        [TestMethod]
        public void MissingScientistsTakesVeyonsCodesToOdesha()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var desert = AshenLeadInsTests.EnterAshen(harness, Arrival);
            var odesha = Npc(desert, N.Odesha);
            var endala = Npc(desert, N.Endala);
            Assert.IsFalse(desert.DynamicObjects.Any(obj => obj.SceneMissionId == Missing.MissionId), "no body before the mission");
            var offer = WildernessRanjaGorgeTests.Offer(harness, odesha, Missing.MissionId);
            CollectionAssert.AreEqual(new[] { Missing.BioVest, Missing.StealthGloves, Missing.GravitonGloves, Missing.HazmatHelmet },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Accept(harness, odesha, Missing.MissionId);
            Assert.IsEmpty(Topics(harness, odesha, Missing.MissionId), "Odesha hears no objective");
            var marker = harness.Manager.BuildStatusSnapshot(harness.Client.Player)[Missing.MissionId].ObjectivesList.Single().IndicatorList.Single();
            Assert.AreEqual(AshenMissionDataV1.MapContextId, marker.MapContextId, "on Ashen Desert");

            var body = Body(harness, desert, Missing.MissionId, Missing.CorpseRole, Missing.VeyonAt, ClientVeyonName);
            Assert.IsLessThan(0.1f, Vector2.Distance(new(marker.Position.X, marker.Position.Z), new(body.Position.X, body.Position.Z)), "the marker on the body");
            WildernessAliaBranchesTests.FillCategory(harness, MissionItemFiller, 50);
            Search(harness, desert, body);
            Assert.AreEqual(0U, Codes(harness), "no room");
            Assert.AreEqual(((UseObjectState)Closed, true), (body.StateId, body.IsEnabled), "the body waits");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Missing.MissionId, Missing.Recover));
            WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);

            harness.MoveTo(body.Position + new Vector3(0, 0, 1));
            harness.Drain();
            Use(harness, desert, body, ClientUseArgument, recovers: true);
            var packets = harness.Drain().Cast<object>().Concat(E.Tick(harness, desert, 250)).ToList();
            Assert.AreEqual(1U, Codes(harness), "the codes");
            Assert.AreEqual(((UseObjectState)Opened, false), (body.StateId, body.IsEnabled), "searched once");
            Assert.AreEqual(1, packets.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Missing.MissionId && packet.ObjectiveId == Missing.Recover), "ObjectiveCompleted once");
            Assert.AreEqual(1, packets.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Missing.MissionId && packet.IsCompleteable), "MissionCompleteable once");
            TrySearch(harness, desert, body);
            Assert.AreEqual(1U, Codes(harness), "a body gives the codes once");
            P.ReloadAndRecover(harness, desert);
            Assert.AreEqual((1U, MissionObjectiveState.Completed), (Codes(harness), Objective(harness, Missing.MissionId, Missing.Recover)), "kept at a reload");

            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, endala.EntityId, Missing.MissionId, 0), "not Endala's to take");
            FullBagWaits(harness, odesha, Missing.MissionId, 0, ArmorFiller);
            FailedTurnInPaysNothing(harness, odesha, Missing.MissionId, 0);
            Assert.AreEqual(1U, Codes(harness), "kept at the failed turn-in");
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, odesha, Missing.MissionId, 0, Missing.Experience, Missing.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Missing.MissionId), "MissionRewarded once");
            Assert.IsGreaterThanOrEqualTo(1U, WildernessRanjaGorgeTests.Held(harness, Missing.BioVest), "the vest chosen");
            Assert.AreEqual(0U, Codes(harness), "Odesha takes the codes");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, odesha, Missing.MissionId), "done once");
        }

        /// <summary>An abandon of Missing Scientists takes the codes and the body; taken again, a whole body and no codes.</summary>
        [TestMethod]
        public void MissingScientistsAbandonedTakesTheCodesAndTheBody()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var desert = AshenLeadInsTests.EnterAshen(harness, Arrival);
            var odesha = Npc(desert, N.Odesha);
            Accept(harness, odesha, Missing.MissionId);
            Search(harness, desert, Body(harness, desert, Missing.MissionId, Missing.CorpseRole, Missing.VeyonAt, ClientVeyonName));
            Assert.AreEqual(1U, Codes(harness));
            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Missing.MissionId));
            harness.Manager.Scenes.Tick(desert);
            harness.Drain();
            Assert.AreEqual(0U, Codes(harness), "the codes go");
            Assert.IsFalse(desert.DynamicObjects.Any(obj => obj.SceneMissionId == Missing.MissionId && obj.IsInWorld), "the body goes");
            Accept(harness, odesha, Missing.MissionId);
            Assert.IsTrue(Body(harness, desert, Missing.MissionId, Missing.CorpseRole, Missing.VeyonAt, ClientVeyonName).IsEnabled, "a whole body");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Missing.MissionId, Missing.Recover));
        }

        /// <summary>
        /// God of Storms: Odesha offers the four weapons; accepted, she hears no objective; ten markers, each on its resistor, on
        /// Ashen Desert; the player's own ten resistors (the client's class, named "Indra Lightning Resistor", off, on the ground,
        /// the client's use argument). In the order 7, 2, 10, 4, 1, 9, 3, 8, 5, 6 each use turns its resistor on, completes its
        /// own objective once and no other; a used one gives nothing more; a reload after five keeps them; the tenth makes the
        /// mission completeable once. Nothing of the hidden 11 reaches the client (no ObjectiveCompleted, no counter). Endala
        /// cannot take it; a full bag waits, a failed save pays nothing; Odesha pays the weapon chosen, once; an abandon before
        /// (a second run) takes the resistors and starts over.
        /// </summary>
        [TestMethod]
        public void GodOfStormsReprogramsTheTenResistors()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            AshenRuntime.SeedCompleted(harness, Missing.MissionId);
            var desert = AshenLeadInsTests.EnterAshen(harness, Arrival);
            var odesha = Npc(desert, N.Odesha);
            var endala = Npc(desert, N.Endala);
            var offer = WildernessRanjaGorgeTests.Offer(harness, odesha, Storms.MissionId);
            CollectionAssert.AreEqual(new[] { Storms.RocketLauncher, Storms.ElectricStaff, Storms.IncendiaryInjectorGun, Storms.Shotgun },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Accept(harness, odesha, Storms.MissionId);
            var resistors = Resistors(harness, desert);
            Program(harness, desert, resistors[0]);
            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Storms.MissionId), "abandoned after one");
            harness.Manager.Scenes.Tick(desert);
            harness.Drain();
            Assert.IsFalse(desert.DynamicObjects.Any(obj => obj.SceneMissionId == Storms.MissionId && obj.IsInWorld), "the resistors go with the abandon");

            Accept(harness, odesha, Storms.MissionId);
            Assert.IsEmpty(Topics(harness, odesha, Storms.MissionId), "Odesha hears no objective");
            var snapshot = harness.Manager.BuildStatusSnapshot(harness.Client.Player)[Storms.MissionId].ObjectivesList;
            Assert.HasCount(ResistorsAt.Length, snapshot, "the ten visible objectives");
            foreach (var objective in snapshot)
            {
                var marker = objective.IndicatorList.Single();
                var at = ResistorsAt[objective.ObjectiveId - 1];
                Assert.IsLessThan(0.1f, Vector2.Distance(new(marker.Position.X, marker.Position.Z), new(at.X, at.Z)), $"objective {objective.ObjectiveId}'s marker on its resistor");
                Assert.AreEqual(AshenMissionDataV1.MapContextId, marker.MapContextId, "on Ashen Desert");
            }
            resistors = Resistors(harness, desert);
            Assert.IsTrue(resistors.All(resistor => resistor.IsEnabled), "all ten off again");
            var log = new List<object>();
            var done = new List<int>();
            foreach (var number in new[] { 7, 2, 10, 4, 1, 9, 3, 8, 5, 6 })
            {
                var resistor = resistors[number - 1];
                harness.MoveTo(resistor.Position + new Vector3(0, 0, 1));
                harness.Drain();
                Use(harness, desert, resistor, ClientUseArgument, recovers: true);
                var packets = harness.Drain().Cast<object>().Concat(E.Tick(harness, desert, 250)).ToList();
                log.AddRange(packets);
                done.Add(number);
                Assert.AreEqual(((UseObjectState)On, false), (resistor.StateId, resistor.IsEnabled), $"resistor {number} on and closed");
                Assert.AreEqual(1, packets.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Storms.MissionId && packet.ObjectiveId == number),
                    $"resistor {number}: its objective, once");
                Assert.AreEqual(done.Count, Programmed(harness), $"resistor {number}: only its own objective");
                if (number == 7)
                {
                    TrySearch(harness, desert, resistor);
                    Assert.AreEqual(1, Programmed(harness), "a used resistor counts once");
                }
                if (done.Count == 5)
                {
                    P.ReloadAndRecover(harness, desert);
                    Assert.AreEqual(5, Programmed(harness), "kept at a reload");
                    resistors = Resistors(harness, desert, done.ToArray());
                }
                if (done.Count < ResistorsAt.Length)
                    Assert.IsFalse(harness.Client.Player.Missions[Storms.MissionId].Completeable, $"{done.Count} of 10: not completeable");
            }
            Assert.IsTrue(harness.Client.Player.Missions[Storms.MissionId].Completeable, "ten resistors");
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Storms.MissionId, Storms.Programmed), "the hidden 11 counted ten");
            Assert.AreEqual(1, log.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Storms.MissionId && packet.IsCompleteable), "MissionCompleteable once");
            Assert.AreEqual(0, log.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Storms.MissionId && packet.ObjectiveId == Storms.Programmed) +
                log.OfType<UpdateObjectiveCounterPacket>().Count(packet => packet.MissionId == Storms.MissionId && packet.ObjectiveId == Storms.Programmed) +
                log.OfType<UpdateObjectiveItemCounterPacket>().Count(packet => packet.MissionId == Storms.MissionId && packet.ObjectiveId == Storms.Programmed),
                "nothing of the hidden 11 goes to the client");

            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, endala.EntityId, Storms.MissionId, 0), "not Endala's to take");
            FullBagWaits(harness, odesha, Storms.MissionId, 0, WeaponFiller);
            FailedTurnInPaysNothing(harness, odesha, Storms.MissionId, 0);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, odesha, Storms.MissionId, 0, Storms.Experience, Storms.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Storms.MissionId), "MissionRewarded once");
            Assert.IsGreaterThanOrEqualTo(1U, WildernessRanjaGorgeTests.Held(harness, Storms.RocketLauncher), "the launcher chosen");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, odesha, Storms.MissionId), "done once");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, endala, Bonds.MissionId), "God of Storms done: Endala's never");
        }

        /// <summary>
        /// The Bonds of Blood: Endala offers the four weapons; accepted, she hears no objective (2 waits), a marker on Maiek's body
        /// (on Ashen Desert) and none yet at her; the player's own body (the client's Brann corpse with its own name, closed, on
        /// the ground); a full bag does not stop the search (no item); the search completes 1 once and opens 2, a marker at
        /// Endala; a reload keeps it; she hears 2 in her package's variant 1, the mission is completeable once. Odesha cannot
        /// take it; a full bag waits, a failed save pays nothing; Endala pays the weapon chosen, once.
        /// </summary>
        [TestMethod]
        public void TheBondsOfBloodFindsMaiekForEndala()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            AshenRuntime.SeedCompleted(harness, Missing.MissionId);
            var desert = AshenLeadInsTests.EnterAshen(harness, Arrival);
            var odesha = Npc(desert, N.Odesha);
            var endala = Npc(desert, N.Endala);
            var offer = WildernessRanjaGorgeTests.Offer(harness, endala, Bonds.MissionId);
            CollectionAssert.AreEqual(new[] { Bonds.LeechGun, Bonds.ElectricNetGun, Bonds.Pistol, Bonds.IncendiaryRpgLauncher },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Accept(harness, endala, Bonds.MissionId);
            Assert.IsEmpty(Topics(harness, endala, Bonds.MissionId), "Endala hears nothing before Maiek is found");
            Assert.AreEqual(MissionObjectiveState.Inactive, Objective(harness, Bonds.MissionId, Bonds.Talk), "2 waits");
            var visible = harness.Manager.BuildStatusSnapshot(harness.Client.Player)[Bonds.MissionId].ObjectivesList;
            var marker = visible.Single(objective => objective.ObjectiveId == Bonds.Find).IndicatorList.Single();
            Assert.IsLessThan(0.1f, Vector2.Distance(new(marker.Position.X, marker.Position.Z), new(Bonds.MaiekAt.X, Bonds.MaiekAt.Z)), "the marker on Maiek");
            Assert.AreEqual(AshenMissionDataV1.MapContextId, marker.MapContextId, "on Ashen Desert");

            WildernessAliaBranchesTests.FillCategory(harness, MissionItemFiller, 50);
            var packets = FindMaiek(harness, desert);
            Assert.AreEqual(1, packets.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Bonds.MissionId && packet.ObjectiveId == Bonds.Find), "1 once");
            Assert.AreEqual(0, packets.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Bonds.MissionId && packet.ObjectiveId == Bonds.FlagReader), "nothing of the hidden 3");
            WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Bonds.MissionId, Bonds.Talk), "2 open");
            P.ReloadAndRecover(harness, desert);
            Assert.AreEqual((MissionObjectiveState.Completed, MissionObjectiveState.Incomplete),
                (Objective(harness, Bonds.MissionId, Bonds.Find), Objective(harness, Bonds.MissionId, Bonds.Talk)), "kept at a reload");
            var talkMarker = harness.Manager.BuildStatusSnapshot(harness.Client.Player)[Bonds.MissionId].ObjectivesList
                .Single(objective => objective.ObjectiveId == Bonds.Talk).IndicatorList.Single();
            Assert.IsLessThan(0.1f, Vector2.Distance(new(talkMarker.Position.X, talkMarker.Position.Z), new((float)G.EndalaAt.X, (float)G.EndalaAt.Z)), "the marker at Endala");
            Assert.IsEmpty(Topics(harness, odesha, Bonds.MissionId), "Odesha hears nothing");
            CollectionAssert.AreEqual(new[] { (Bonds.Talk, 1U) }, AshenFormalComplaintsTests.TopicVariants(harness, endala, Bonds.MissionId), "Endala hears 2, variant 1");
            var log = AshenFormalComplaintsTests.Say(harness, endala, Bonds.MissionId, Bonds.Talk, 1);
            Assert.AreEqual(1, log.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Bonds.MissionId && packet.IsCompleteable), "MissionCompleteable once");

            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, odesha.EntityId, Bonds.MissionId, 0), "not Odesha's to take");
            FullBagWaits(harness, endala, Bonds.MissionId, 0, WeaponFiller);
            FailedTurnInPaysNothing(harness, endala, Bonds.MissionId, 0);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, endala, Bonds.MissionId, 0, Bonds.Experience, Bonds.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Bonds.MissionId), "MissionRewarded once");
            Assert.IsGreaterThanOrEqualTo(1U, WildernessRanjaGorgeTests.Held(harness, Bonds.LeechGun), "the leech gun chosen");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, endala, Bonds.MissionId), "done once");
        }

        /// <summary>
        /// The Bonds of Blood abandoned after Maiek is found: Odesha offers God of Storms again; taken again, Maiek's body is whole
        /// (closed, usable) and 2 waits again (Inactive), 1 open.
        /// </summary>
        [TestMethod]
        public void TheBondsOfBloodAbandonedAfterMaiekStartsOver()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            AshenRuntime.SeedCompleted(harness, Missing.MissionId);
            var desert = AshenLeadInsTests.EnterAshen(harness, Arrival);
            var odesha = Npc(desert, N.Odesha);
            var endala = Npc(desert, N.Endala);
            Accept(harness, endala, Bonds.MissionId);
            FindMaiek(harness, desert);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Bonds.MissionId, Bonds.Talk), "2 open");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, odesha, Storms.MissionId), "held: God of Storms not offered");
            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Bonds.MissionId));
            harness.Manager.Scenes.Tick(desert);
            harness.Drain();
            Assert.IsFalse(desert.DynamicObjects.Any(obj => obj.SceneMissionId == Bonds.MissionId && obj.IsInWorld), "the body goes");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, odesha, Storms.MissionId), "abandoned: Odesha offers God of Storms again");
            Accept(harness, endala, Bonds.MissionId);
            var body = Body(harness, desert, Bonds.MissionId, Bonds.CorpseRole, Bonds.MaiekAt, 0);
            Assert.AreEqual(((UseObjectState)Closed, true), (body.StateId, body.IsEnabled), "a whole body");
            Assert.AreEqual((MissionObjectiveState.Incomplete, MissionObjectiveState.Inactive),
                (Objective(harness, Bonds.MissionId, Bonds.Find), Objective(harness, Bonds.MissionId, Bonds.Talk)), "1 open, 2 waits again");
        }

        /// <summary>
        /// The closing both ways, with both missions held (a GM's gift: the offers keep a player from holding both): nine
        /// resistors leave an open The Bonds of Blood alone; the tenth fails it (the client's log: "you will lose the opportunity
        /// to find Maiek"), a search of its body (kept for good: a failed mission cannot be abandoned) then counts nothing, and God of Storms stays completeable. The other way: Maiek found fails an open God of
        /// Storms ("you will lose the opportunity to reprogram the Lightning Resistors") and The Bonds of Blood goes on. A closing
        /// signal to a mission not held changes nothing (God of Storms done alone above).
        /// </summary>
        [TestMethod]
        public void EachSidesEndClosesTheOther()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            AshenRuntime.SeedCompleted(harness, Missing.MissionId);
            var desert = AshenLeadInsTests.EnterAshen(harness, Arrival);
            var odesha = Npc(desert, N.Odesha);
            Accept(harness, Npc(desert, N.Endala), Bonds.MissionId);
            Assert.IsTrue(harness.Manager.TryGiveMission(harness.Client, Storms.MissionId, "test"), "a GM gives God of Storms too");
            harness.Drain();
            var resistors = Resistors(harness, desert);
            for (var number = 1; number <= ResistorsAt.Length; number++)
            {
                Program(harness, desert, resistors[number - 1]);
                if (number < ResistorsAt.Length)
                    Assert.AreEqual(MissionState.Active, harness.Client.Player.Missions[Bonds.MissionId].State, $"{number} resistors: The Bonds of Blood open");
            }
            harness.Manager.Scenes.Tick(desert);
            harness.Drain();
            Assert.IsTrue(!harness.Client.Player.Missions.TryGetValue(Bonds.MissionId, out var bonds) || bonds.State == MissionState.Failed, "the tenth fails The Bonds of Blood");
            // A failed mission keeps its scene for good (SceneApplication.Execute admits Failed; TryAbandon takes only an Active
            // mission, MissionApplication.TryAbandon): a search then counts nothing. Only a GM's gift holds both sides.
            var maiek = desert.DynamicObjects.Single(obj => obj.SceneMissionId == Bonds.MissionId && obj.SceneActorRole == Bonds.CorpseRole && obj.IsInWorld);
            TrySearch(harness, desert, maiek);
            Assert.AreEqual((MissionObjectiveState.Failed, MissionObjectiveState.Failed), (Objective(harness, Bonds.MissionId, Bonds.Find), Objective(harness, Bonds.MissionId, Bonds.FlagReader)),
                "a search of Maiek after the fork counts nothing");
            Assert.IsTrue(harness.Client.Player.Missions[Storms.MissionId].Completeable, "God of Storms completeable");
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, odesha, Storms.MissionId, 0, Storms.Experience, Storms.Credits);

            using var other = WildernessRuntimeTestHarness.Create();
            AshenRuntime.SeedCompleted(other, Missing.MissionId);
            var desert2 = AshenLeadInsTests.EnterAshen(other, Arrival);
            Accept(other, Npc(desert2, N.Odesha), Storms.MissionId);
            Assert.IsTrue(other.Manager.TryGiveMission(other.Client, Bonds.MissionId, "test"), "a GM gives The Bonds of Blood too");
            other.Drain();
            Program(other, desert2, Resistors(other, desert2)[0]);
            FindMaiek(other, desert2);
            other.Manager.Scenes.Tick(desert2);
            other.Drain();
            Assert.IsTrue(!other.Client.Player.Missions.TryGetValue(Storms.MissionId, out var storms) || storms.State == MissionState.Failed, "Maiek found fails God of Storms");
            var second = desert2.DynamicObjects.Single(obj => obj.SceneMissionId == Storms.MissionId && obj.SceneActorRole == Storms.ResistorRole(2) && obj.IsInWorld);
            TrySearch(other, desert2, second);
            Assert.AreEqual((MissionObjectiveState.Failed, MissionObjectiveState.Failed), (Objective(other, Storms.MissionId, 2), Objective(other, Storms.MissionId, Storms.Programmed)),
                "a resistor used after the fork counts nothing");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(other, Bonds.MissionId, Bonds.Talk), "The Bonds of Blood goes on");
        }

        /// <summary>
        /// The set's creatures (mission-creature immunities, to-fix row 20 of the set 10 playtest): the only creature rows in set
        /// 13's id block 790480-790519 are Odesha and Endala, friendly; their class (7776, NPC_Brann_Swapset_Male) carries none of
        /// the client's IMMUNE_* damage flags (creature_class_flag 125-131 and 135). The set adds no creature a player fights.
        /// </summary>
        [TestMethod]
        public void TheSetsCreaturesAreTheTwoFriendlyNpcsWithNoDamageImmunity()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var rows = harness.World.CreatureEntries.AsNoTracking().Where(row => row.Id >= 790480 && row.Id <= 790519).ToList();
            CollectionAssert.AreEquivalent(new[] { N.Odesha, N.Endala }, rows.Select(row => row.Id).ToArray(), "only the two NPCs");
            Assert.IsTrue(rows.All(row => row.Faction == 1 && row.ClassId == 7776), "friendly, class 7776");
            var immune = new uint[] { 125, 126, 127, 128, 129, 130, 131, 135 };
            var flags = harness.World.CreatureClassFlagEntries.AsNoTracking().Where(flag => flag.ClassId == 7776).Select(flag => flag.FlagId).ToList();
            CollectionAssert.AreEqual(Array.Empty<uint>(), flags.Where(immune.Contains).OrderBy(flag => flag).ToArray(), "no damage immunity");
            Assert.IsFalse(harness.World.SpawnPoolEntries.AsNoTracking().Any(pool => pool.Id >= 790482 && pool.Id <= 790519), "no pool but the two NPCs'");
        }

        /// <summary>
        /// The set's places on the client's floor: the two NPCs, Veyon, the ten resistors and Maiek each on the navmesh's ground
        /// and a client floor (within 1 m), with one navmesh floor within 3 m round it (no second level), nothing of the client's
        /// surfaces over it within 40 m (open sky), seen from all 16 places 2 m round it on a floor near its own (as
        /// AshenGuardTests' ring: a player's eye to the NPC's or object's middle); the resistors 25 m or
        /// more apart and the bodies and resistors 15 m or more from the NPCs; the NPCs 2 m or more from a navmesh wall; every object and NPC 34 m or more outside every
        /// automatic hostile Ashen Desert pool's circle.
        /// </summary>
        [TestMethod]
        public void ThePlacesStandOnTheClientsFloorUnderOpenSky()
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
            var places = Spots();
            var near = F.Near(geometry, places.Select(place => place.At).ToList(), 45);
            var failures = new List<string>();
            for (var i = 0; i < places.Count; i++)
            {
                var (label, at) = places[i];
                if (nav.GroundHeight(at) is not { } ground || MathF.Abs(ground - at.Y) > 0.5f)
                    failures.Add(FormattableString.Invariant($"{label}: the navmesh ground {nav.GroundHeight(at)} at {at}"));
                if (F.FloorAt(near[i], at, at.Y - 2, at.Y + 1.5f) is not { } floor || MathF.Abs(floor - at.Y) > 1)
                    failures.Add(FormattableString.Invariant($"{label}: the client's floor at {at}"));
                if (near[i].Any(t => F.Hit(at + new Vector3(0, 1, 0), Vector3.UnitY, t.A, t.B, t.C) is { } up && up < 40))
                    failures.Add($"{label}: a client surface over it");
                for (var h = 3; h <= 12; h++)
                    foreach (var y in new[] { at.Y - h, at.Y + h })
                        if (nav.Nearest(at with { Y = y }) is { } other && Vector2.Distance(new(other.X, other.Z), new(at.X, at.Z)) < 0.3f && MathF.Abs(other.Y - at.Y) > 2.5f)
                            failures.Add(FormattableString.Invariant($"{label}: a second navmesh floor at {other.Y:0.00}"));
                var seen = 0;
                for (var k = 0; k < F.RingPlaces; k++)
                {
                    var angle = k * 2 * MathF.PI / F.RingPlaces;
                    var ring = at + new Vector3(MathF.Sin(angle) * F.Ring, 0, MathF.Cos(angle) * F.Ring);
                    var aim = at + new Vector3(0, i < 2 ? F.NpcMiddle : F.ObjectMiddle, 0);
                    if (F.FloorAt(near[i], ring, at.Y - F.MaxOver, at.Y + F.MaxUnder) is { } y && F.Clear(near[i], ring with { Y = y + F.Eye }, aim))
                        seen++;
                }
                if (seen < F.RingPlaces)
                    failures.Add($"{label}: seen from {seen} of {F.RingPlaces} ring places");
                if (i < 2 && (nav.DistanceToWall(at, 20) ?? 0) < 2)
                    failures.Add(FormattableString.Invariant($"{label}: {nav.DistanceToWall(at, 20) ?? 0:0.00} m from a navmesh wall"));
                var gap = hostile.Min(pool => Vector2.Distance(new(at.X, at.Z), new((float)pool.PosX, (float)pool.PosZ)) - (float)pool.Radius);
                if (gap < PlaceClearance)
                    failures.Add(FormattableString.Invariant($"{label}: {gap:0.0} m outside a hostile pool's circle"));
            }
            for (var a = 0; a < ResistorsAt.Length; a++)
                for (var b = a + 1; b < ResistorsAt.Length; b++)
                    if (Vector2.Distance(new(ResistorsAt[a].X, ResistorsAt[a].Z), new(ResistorsAt[b].X, ResistorsAt[b].Z)) < 25)
                        failures.Add($"resistors {a + 1} and {b + 1} under 25 m apart");
            foreach (var (label, at) in places.Where(place => !place.Label.StartsWith("Surveyor") && !place.Label.StartsWith("Researcher")))
                foreach (var npc in new[] { G.OdeshaAt, G.EndalaAt })
                    if (Vector2.Distance(new(at.X, at.Z), new((float)npc.X, (float)npc.Z)) < 15)
                        failures.Add($"{label} under 15 m from an NPC");
            Assert.HasCount(2 + 1 + 10 + 1, places);
            Assert.IsEmpty(failures, $"{failures.Count}: " + string.Join("; ", failures));
        }

        /// <summary>
        /// The walks on the client's floor (AshenFixBTests.ClientFloorLine with a player's half width: every half metre, never
        /// more than 1.1 m up over a metre, no two climbing samples within a metre on a floor steeper than the client's 55 degrees
        /// (fix G, to-fix row 24), nothing of the placed meshes in the way at knee, chest or head height), there and back by the
        /// corners Walks names (the client-floor floods of fix G: the Indra Pass waypoint's floor is joined to no way in, upstream
        /// N23, so the set's places are in the valley): the two NPCs from the valley's south entry (the end of set 10's way from
        /// the White Oasis Post waypoint, which AshenDesignerHidesTests walks); Veyon and the ten resistors from Odesha; Maiek
        /// from the Ashoka Settlement waypoint. Every walk keeps HostileClearance (30 m, 2D) outside the circle of every
        /// automatic hostile Ashen Desert pool (none is a set 13 mission's own).
        /// </summary>
        [TestMethod]
        public void ThePlacesAreWalkedToOnTheClientsFloorAwayFromEveryCamp()
        {
            var dataDir = F.ClientData();
            using var harness = WildernessRuntimeTestHarness.Create();
            using var meshes = new MeshLibrary(dataDir, F.MeshCsv());
            var geometry = MapGeometry.Load(Path.Combine(dataDir, "maps", harness.World.MapInfoEntries.AsNoTracking()
                .Single(map => map.Id == AshenMissionDataV1.MapContextId).MapName), meshes, 1);
            var hostile = WO.HostilePools(harness);
            Assert.IsGreaterThan(50, hostile.Count, "the Ashen hostile pools are read");
            var failures = new List<string>();
            var walked = 0;
            foreach (var (label, from, target, corners) in Walks())
            {
                var there = corners.Prepend(from).Append(target).ToList();
                var back = Enumerable.Reverse(there).ToList();
                foreach (var (line, direction) in new[] { (there, "there"), (back, "back") })
                {
                    walked++;
                    if (AshenFixBTests.ClientFloorLine(geometry, line, line[^1], null, AshenFormalComplaintsTests.PlayerHalfWidth) is { } why)
                        failures.Add($"{label} {direction}: {why}");
                    if (WO.Clearance(line, hostile) is { } close)
                        failures.Add(FormattableString.Invariant($"{label} {direction}: {close.Gap:0.0} m outside hostile pool {close.Pool}'s circle at ({close.At.X:0.0}, {close.At.Y:0.0})"));
                }
            }
            Assert.AreEqual(2 * Walks().Count, walked, "every walk both ways");
            Assert.HasCount(2 + 1 + 10 + 1, Walks());
            Assert.IsEmpty(failures, $"{failures.Count}: " + string.Join("; ", failures));
        }

        /// <summary>How far outside every hostile pool's circle the set's places stand (2D; 30 m walk clearance and a margin).</summary>
        private const float PlaceClearance = 34;

        /// <summary>The set's places: label, place.</summary>
        internal static List<(string Label, Vector3 At)> Spots()
        {
            var spots = new List<(string, Vector3)>
            {
                ("Surveyor Malya Odesha", V(G.OdeshaAt)), ("Researcher Endala Berish", V(G.EndalaAt)),
                ("Veyon's body", E.ToVector(Missing.VeyonAt))
            };
            for (var number = 1; number <= ResistorsAt.Length; number++)
                spots.Add(($"Indra Lightning Resistor {number}", E.ToVector(ResistorsAt[number - 1])));
            spots.Add(("Maiek's body", E.ToVector(Bonds.MaiekAt)));
            return spots;
        }

        /// <summary>The walks: label, start, place, the corners between (the client-floor floods of fix G, simplified to straight client-floor legs; ESTIMATE of a player's line).</summary>
        internal static List<(string Label, Vector3 From, Vector3 Target, Vector3[] Corners)> Walks()
        {
            var odesha = V(G.OdeshaAt);
            var walks = new List<(string, Vector3, Vector3, Vector3[])>
            {
                ("Odesha from the valley's south entry (set 10's way from White Oasis Post)", Arrival, odesha, WayToOdesha),
                ("Endala from the valley's south entry (set 10's way from White Oasis Post)", Arrival, V(G.EndalaAt), WayToEndala),
                ("Veyon from Odesha", odesha, E.ToVector(Missing.VeyonAt), WayToVeyon),
                ("Maiek from the Ashoka Settlement waypoint", Ashoka, E.ToVector(Bonds.MaiekAt), WayToMaiek)
            };
            for (var number = 1; number <= ResistorsAt.Length; number++)
                walks.Add(($"resistor {number} from Odesha", odesha, E.ToVector(ResistorsAt[number - 1]), WayToResistor[number - 1]));
            return walks;
        }

        // The corners: the client-floor floods of fix G (a 1 m grid, each step by AshenFixBTests.ClientFloorLine's rules both
        // ways, 31 m off every hostile pool's circle; the path then cut to straight legs that pass ClientFloorLine with a
        // player's half width as a whole, both ways). From Odesha the ways go up east to the valley's south rim by (139, 450),
        // (145, 456), then along it and down.
        private static readonly Vector3[] WayToOdesha = { new(126.0f, 307.43f, 451.0f) };
        private static readonly Vector3[] WayToEndala = { new(128.0f, 308.16f, 452.0f), new(126.0f, 307.43f, 451.0f), new(117.0f, 304.50f, 451.0f) };
        private static readonly Vector3[] WayToVeyon = { new(139.0f, 310.48f, 450.0f), new(178.0f, 306.09f, 487.0f) };
        private static readonly Vector3[] Rim = { new(139.0f, 310.48f, 450.0f), new(145.0f, 310.73f, 456.0f), new(170.0f, 308.53f, 479.0f) };
        private static readonly Vector3[] DownWest = Rim.Append(new(171.0f, 304.01f, 494.0f)).ToArray();
        private static readonly Vector3[] WayToMaiek =
        {
            new(389.60f, 280.45f, -457.60f), new(387.60f, 280.65f, -456.00f), new(387.60f, 281.25f, -454.80f), new(388.40f, 283.45f, -430.80f),
            new(402f, 290.95f, -401f), new(405f, 290.95f, -352f), new(395f, 298.15f, -290f), new(395f, 292.17f, -283f),
            new(394f, 300.60f, -208f), new(396f, 300.84f, -204f), new(402f, 296.20f, -200f), new(402f, 295.35f, -188f), new(402f, 296.44f, -187f),
            new(396f, 304.50f, -177f), new(352f, 300.96f, -137f), new(352f, 288.75f, -84f)
        };   // fix G: off the waypoint pad to the west and north (the navmesh's way; set 13's step at x 400 failed the knee line
             // coming back), north up the road, then east of the hump where the real client stopped at (394.2, 301.2, -185.5)
             // (the flood kept 9 m off it) and over the rise to the generators
        private static readonly Vector3[][] WayToResistor =
        {
            Rim.Concat(new Vector3[] { new(172.0f, 314.02f, 527.0f), new(172.0f, 313.78f, 529.0f), new(158.0f, 313.29f, 533.0f) }).ToArray(),
            DownWest.Concat(new Vector3[] { new(97.0f, 296.08f, 516.0f), new(92.0f, 292.78f, 535.0f), new(86.0f, 293.51f, 539.0f) }).ToArray(),
            new Vector3[] { new(139.0f, 310.48f, 450.0f), new(184.0f, 305.36f, 493.0f), new(186.0f, 307.31f, 500.0f) },
            new Vector3[] { new(140.0f, 309.87f, 452.0f), new(150.0f, 311.83f, 458.0f) },
            new Vector3[] { new(139.0f, 310.48f, 450.0f), new(145.0f, 310.73f, 456.0f), new(170.0f, 308.90f, 478.0f), new(171.0f, 304.99f, 493.0f), new(166.0f, 300.96f, 495.0f) },
            new Vector3[] { new(140.0f, 309.87f, 452.0f), new(176.0f, 306.94f, 484.0f) },
            DownWest.Concat(new Vector3[] { new(97.0f, 296.08f, 516.0f), new(81.0f, 296.69f, 582.0f) }).ToArray(),
            DownWest.Concat(new Vector3[] { new(103.0f, 294.13f, 495.0f), new(103.0f, 292.90f, 483.0f) }).ToArray(),
            Rim.Concat(new Vector3[] { new(171.0f, 306.58f, 491.0f), new(171.0f, 304.01f, 494.0f), new(124.0f, 291.07f, 508.0f) }).ToArray(),
            DownWest.Concat(new Vector3[] { new(97.0f, 296.08f, 516.0f) }).ToArray()
        };

        private static Vector3 V((double X, double Y, double Z) at) => new((float)at.X, (float)at.Y, (float)at.Z);

        private static uint Codes(WildernessRuntimeTestHarness harness) => WildernessRanjaGorgeTests.Held(harness, ClientCodesTemplate);

        private static int Programmed(WildernessRuntimeTestHarness harness) =>
            Enumerable.Range(1, ResistorsAt.Length).Count(number => Objective(harness, Storms.MissionId, (uint)number) == MissionObjectiveState.Completed);

        /// <summary>A body: the player's own, the client's Brann corpse with <paramref name="name"/>, at its place on the ground, closed and open, with the client's use argument.</summary>
        private static DynamicObject Body(WildernessRuntimeTestHarness harness, MapChannel desert, uint mission, string role, Rasa.Missions.Scenes.ScenePosition at, uint name)
        {
            harness.Manager.Scenes.Tick(desert);
            var body = desert.DynamicObjects.Single(obj => obj.SceneMissionId == mission && obj.SceneActorRole == role && obj.IsInWorld);
            Assert.AreEqual((ClientCorpseClass, name, new Vector3(at.X, at.Y, at.Z), ClientUseArgument),
                ((uint)body.EntityClassId, body.NameOverrideId, body.Position, body.MissionUseAction.ActionArgId), role);
            Assert.AreEqual(harness.Client.Player.Id, body.SceneOwnerCharacterId, "the player's own");
            AssertOnGround(desert, body.Position, role);
            return body;
        }

        /// <summary>The ten resistors: the player's own, the client's class named "Indra Lightning Resistor", at their places, on the ground; off and open (or, for <paramref name="done"/>, on and closed).</summary>
        private static DynamicObject[] Resistors(WildernessRuntimeTestHarness harness, MapChannel desert, int[] done = null)
        {
            harness.Manager.Scenes.Tick(desert);
            var resistors = new DynamicObject[ResistorsAt.Length];
            for (var number = 1; number <= resistors.Length; number++)
            {
                var isDone = done?.Contains(number) == true;
                var resistor = desert.DynamicObjects.Single(obj => obj.SceneMissionId == Storms.MissionId && obj.SceneActorRole == Storms.ResistorRole(number) && obj.IsInWorld);
                Assert.AreEqual((ClientResistorClass, ClientResistorName, E.ToVector(ResistorsAt[number - 1]), ClientUseArgument),
                    ((uint)resistor.EntityClassId, resistor.NameOverrideId, resistor.Position, resistor.MissionUseAction.ActionArgId), $"resistor {number}");
                Assert.AreEqual(isDone ? ((UseObjectState)On, false) : ((UseObjectState)Off, true), (resistor.StateId, resistor.IsEnabled), $"resistor {number} state");
                Assert.AreEqual(harness.Client.Player.Id, resistor.SceneOwnerCharacterId, "the player's own");
                AssertOnGround(desert, resistor.Position, resistor.SceneActorRole);
                resistors[number - 1] = resistor;
            }
            return resistors;
        }

        /// <summary>A use of a resistor by a player beside it, with its recovery and a tick.</summary>
        private static void Program(WildernessRuntimeTestHarness harness, MapChannel desert, DynamicObject resistor)
        {
            harness.MoveTo(resistor.Position + new Vector3(0, 0, 1));
            harness.Drain();
            Use(harness, desert, resistor, ClientUseArgument, recovers: true);
            E.Tick(harness, desert, 250);
            harness.Drain();
        }

        /// <summary>Maiek's body found (its use, recovery and a tick); the packets.</summary>
        private static List<object> FindMaiek(WildernessRuntimeTestHarness harness, MapChannel desert)
        {
            var body = Body(harness, desert, Bonds.MissionId, Bonds.CorpseRole, Bonds.MaiekAt, 0);
            harness.MoveTo(body.Position + new Vector3(0, 0, 1));
            harness.Drain();
            Use(harness, desert, body, ClientUseArgument, recovers: true);
            var packets = harness.Drain().Cast<object>().Concat(E.Tick(harness, desert, 250)).ToList();
            Assert.AreEqual(((UseObjectState)Opened, false), (body.StateId, body.IsEnabled), "Maiek found once");
            return packets;
        }

        /// <summary>A search of a body by a player beside it, with its recovery and a tick.</summary>
        private static void Search(WildernessRuntimeTestHarness harness, MapChannel desert, DynamicObject body)
        {
            harness.MoveTo(body.Position + new Vector3(0, 0, 1));
            harness.Drain();
            Use(harness, desert, body, ClientUseArgument, recovers: true);
            E.Tick(harness, desert, 250);
            harness.Drain();
        }

        /// <summary>A use the server may refuse at once or after its recovery (as AshenBoneyardTests.TrySearch): the caller checks that nothing came of it.</summary>
        private static void TrySearch(WildernessRuntimeTestHarness harness, MapChannel desert, DynamicObject obj)
        {
            harness.MoveTo(obj.Position + new Vector3(0, 0, 1));
            harness.Drain();
            var count = desert.PerformRecovery.Count;
            harness.Objects.RequestUseObjectPacket(harness.Client, new RequestUseObjectPacket
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
    }
}
