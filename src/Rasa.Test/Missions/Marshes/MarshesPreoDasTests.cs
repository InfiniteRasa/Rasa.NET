using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Managers;
using Rasa.Missions.Scenes;
using Rasa.Packets.MapChannel.Client;
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
    using static PlateauInstanceMaps;
    using N = MarshesPreoDasNpcsV1;
    using C = MarshesTheChoicesWeMakeV1;

    /// <summary>
    /// Marshes set 14, P'reo Das in the Village Ruins (squad instance 1743), played through as a player plays it: Lieutenant
    /// Morrison's The Lost Ones (1076: the travel point, the signal spike, Headquarters by radio), Headquarters' Needle in a Bane
    /// Camp (893: the hidden encampment's elder) and The Choices We Make (1078: Myrquar on a 25 minute clock, the Disperse Logos,
    /// the control room's equipment and toxin sample, Toida in Paludos), one closing the other, and Forean Elder Qunle's Dirty
    /// Secrets (1077: his key, the Logos, his own lab, Toida).
    ///
    /// Why these tests: the scenes are bound to a squad copy that outlives a test (the run's own actors are looked for); 893
    /// and 1078 are radio offers with a fork between them; 1078 runs on a deadline that ends its scene; the Logos objectives
    /// take a Logos the player already has; 1077's and 1078's labs are each the player's own; three of the turn-ins are off the
    /// instance. A spot that takes no spike, an offer that comes back after the other path, a Myrquar who stays after the clock,
    /// a sample counted twice, or progress lost at a reload would each break them.
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class MarshesPreoDasTests
    {
        private const uint Ruins = N.MapContextId;
        private const uint LostOnes = MarshesTheLostOnesV1.MissionId;
        private const uint Needle = MarshesNeedleInABaneCampV1.MissionId;
        private const uint Choices = C.MissionId;
        private const uint Secrets = MarshesDirtySecretsV1.MissionId;

        /// <summary>
        /// The main navmesh's edge by the arrival: the arrival itself (the map_link's destination, -138.5, 63, -360) is a small
        /// island of its own, about 4 m short of the main mesh (probe of 2026-11-03), so paths start here.
        /// </summary>
        internal static readonly Vector3 DoorEdge = new(-120f, 61.17f, -357f);

        /// <summary>
        /// Qunle and the encampment's elder stand in the copy with the client's names, their levels and packages, walked to
        /// from the arrival; Qunle offers 1077 with no mission before it, 893 and 1078 need 1076; Morrison offers 1076 on the
        /// Marshes; the copy's door leads back to the Marshes door.
        /// </summary>
        [TestMethod]
        public void TheNpcsStandAtTheirPlaces()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, Npc(marshes, MarshesTheLostOnesV1.Morrison), LostOnes), "Morrison offers 1076");

            var copy = IntoRuins(harness);
            Assert.IsTrue(copy.IsSquadInstance);
            foreach (var (pool, name, level, package) in new[]
            {
                (N.Qunle, N.QunleName, N.QunleLevel, N.QunlePackage), (N.Elder, N.ElderName, N.ElderLevel, N.ElderPackage)
            })
            {
                var npc = Giver(copy, pool);
                Assert.AreEqual((name, level, package), (npc.NameId, npc.Level, npc.Npc?.NpcPackageId), $"{pool}");
                AssertOnGround(copy, npc.Position, $"{pool}");
                AssertPath(copy, DoorEdge, npc.Position, $"{pool} from the door");
                Assert.AreNotEqual(TargetCategory.Hostile, npc.TargetCategory);
            }
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, Giver(copy, N.Qunle), Secrets), "Qunle offers 1077 with nothing before it");
            Assert.IsFalse(harness.Manager.TryAcceptRadioMission(harness.Client, Needle), "893 needs 1076");
            Assert.IsFalse(harness.Manager.TryAcceptRadioMission(harness.Client, Choices), "1078 needs 1076");

            var exit = harness.World.MapLinkEntries.AsNoTracking().Single(row => row.MapContextId == Ruins);
            Assert.AreEqual(MarshesMissionDataV1.MapContextId, exit.DestMapContextId, "the way out leads to the Marshes");
            Assert.IsLessThan(3f, Vector3.Distance(ToVector(MarshesTheLostOnesV1.DoorAt),
                new Vector3((float)exit.DestPosX, (float)exit.DestPosY, (float)exit.DestPosZ)), "at the Marshes door");
        }

        /// <summary>
        /// 1076: accepted at Morrison on the Marshes with the spike; in the copy no spot before the travel point; the travel
        /// point completes 2 and sets down the spot; its use takes the spike and completes 1, once, a reload before; Headquarters
        /// pays by radio once, and calls with 893 and 1078.
        /// </summary>
        [TestMethod]
        public void TheLostOnesPlantsTheSpikeInPreoDas()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            Accept(harness, Npc(marshes, MarshesTheLostOnesV1.Morrison), LostOnes);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, MarshesTheLostOnesV1.SpikeTemplate), "the spike at the accept");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, LostOnes, MarshesTheLostOnesV1.Travel));
            Assert.AreEqual(MissionObjectiveState.Inactive, Objective(harness, LostOnes, MarshesTheLostOnesV1.Plant));

            var copy = IntoRuins(harness);
            Tick(harness, copy, 250);
            Assert.IsNull(FindSceneObject(harness, copy, LostOnes, MarshesTheLostOnesV1.SpotRole), "no spot before the travel point");
            AssertPath(copy, DoorEdge, ToVector(MarshesTheLostOnesV1.TravelAt), "the travel point from the door");
            Assert.IsGreaterThan((float)MarshesTheLostOnesV1.TravelRadius,
                Vector3.Distance(harness.Client.Player.Position, ToVector(MarshesTheLostOnesV1.TravelAt)), "the arrival is outside it");
            Enter(harness, ToVector(MarshesTheLostOnesV1.TravelAt));
            Tick(harness, copy, 250);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, LostOnes, MarshesTheLostOnesV1.Travel));
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, LostOnes, MarshesTheLostOnesV1.Plant));

            var spot = SceneObject(harness, copy, LostOnes, MarshesTheLostOnesV1.SpotRole);
            Assert.AreEqual(MarshesTheLostOnesV1.SpotClass, (uint)spot.EntityClassId);
            AssertOnGround(copy, spot.Position, "the spot");
            AssertPath(copy, DoorEdge, spot.Position, "the spot from the door");
            ReloadAndRecover(harness, copy);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, LostOnes, MarshesTheLostOnesV1.Travel), "kept at a reload");
            spot = SceneObject(harness, copy, LostOnes, MarshesTheLostOnesV1.SpotRole);
            Use(harness, copy, spot, MarshesTheLostOnesV1.UseArgument);
            Use(harness, copy, spot, MarshesTheLostOnesV1.UseArgument);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, LostOnes, MarshesTheLostOnesV1.Plant));
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, MarshesTheLostOnesV1.SpikeTemplate), "the spike planted");
            Assert.AreEqual(MarshesTheLostOnesV1.PlacedState, (uint)spot.StateId);
            Assert.IsTrue(harness.Client.Player.Missions[LostOnes].Completeable);

            var before = harness.Context.ReadRewardTotals();
            Assert.IsTrue(harness.Manager.TryCompleteRadioMission(harness.Client, LostOnes, 2, null));
            var rewarded = harness.Drain();
            Assert.AreEqual(1, rewarded.OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == LostOnes), "paid once");
            var after = harness.Context.ReadRewardTotals();
            Assert.AreEqual((before.Experience + MarshesTheLostOnesV1.Experience, before.Credits + (int)MarshesTheLostOnesV1.Credits),
                (after.Experience, after.Credits));
            Assert.AreEqual(MarshesTheLostOnesV1.CryoGrenades, WildernessRanjaGorgeTests.Held(harness, MarshesTheLostOnesV1.CryoGrenade), "the chosen stack");
            Assert.IsFalse(harness.Manager.TryCompleteRadioMission(harness.Client, LostOnes, 2, null), "and only once");
            foreach (var next in new[] { Needle, Choices })
                Assert.AreEqual(1, rewarded.OfType<DispenseRadioMissionPacket>().Count(packet => packet.MissionId == next), $"{next} by radio");
        }

        /// <summary>1076 abandoned in the copy: the spike goes; taken again, one spike.</summary>
        [TestMethod]
        public void TheLostOnesAbandonedTakesItsSpike()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            var morrison = Npc(marshes, MarshesTheLostOnesV1.Morrison);
            Accept(harness, morrison, LostOnes);
            var copy = IntoRuins(harness);
            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, LostOnes));
            harness.Manager.Scenes.Tick(copy);
            harness.Drain();
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, MarshesTheLostOnesV1.SpikeTemplate), "the spike goes");
            marshes = EnterMarshes(harness);
            Accept(harness, Npc(marshes, MarshesTheLostOnesV1.Morrison), LostOnes);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, MarshesTheLostOnesV1.SpikeTemplate), "one spike again");
        }

        /// <summary>
        /// 893 and 1078 both held (offered by radio on arrival in the copy): the elder's flag-1 talk completes 893 and its
        /// signal fails 1078 (Myrquar with it); the elder pays; neither is offered again, 1078 not once 893 is done.
        /// </summary>
        [TestMethod]
        public void NeedleInABaneCampWithTheChoicesHeldFailsThem()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            EnterMarshes(harness);
            SeedCompletedMarshes(harness, LostOnes);
            var copy = IntoRuins(harness);
            harness.Manager.OfferArrivalMissions(harness.Client);
            var offers = harness.Drain();
            foreach (var mission in new[] { Needle, Choices })
                Assert.IsTrue(offers.OfType<DispenseRadioMissionPacket>().Any(packet => packet.MissionId == mission), $"{mission} on arrival");
            TakeByRadio(harness, copy, Needle, Choices);

            var elder = Giver(copy, N.Elder);
            ForkTalk(harness, elder, Needle, MarshesNeedleInABaneCampV1.Contact, 1);
            Tick(harness, copy, 250);
            Assert.AreEqual(MissionState.Failed, harness.Client.Player.Missions[Choices].State, "the convoy is gone");
            Assert.IsNull(SceneCreature(harness, copy, Choices, C.MyrquarRole), "Myrquar with it");
            PayNeedle(harness, elder);

            Assert.IsTrue(harness.Manager.TryClear(harness.Client, Choices), "the failed 1078 cleared");
            harness.Drain();
            harness.Manager.OfferArrivalMissions(harness.Client);
            offers = harness.Drain();
            Assert.IsFalse(offers.OfType<DispenseRadioMissionPacket>().Any(packet => packet.MissionId == Choices), "no 1078 after 893");
            Assert.IsFalse(harness.Manager.TryAcceptRadioMission(harness.Client, Choices), "1078 is closed");
            Assert.IsFalse(harness.Manager.TryAcceptRadioMission(harness.Client, Needle), "893 done once");
        }

        /// <summary>893 alone, 1078 declined (never taken): the elder's flag-1 talk completes it and he pays; 1078 is then closed.</summary>
        [TestMethod]
        public void NeedleInABaneCampAloneAfterTheChoicesAreDeclined()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            EnterMarshes(harness);
            SeedCompletedMarshes(harness, LostOnes);
            var copy = IntoRuins(harness);
            harness.Manager.OfferArrivalMissions(harness.Client);
            harness.Drain();
            TakeByRadio(harness, copy, Needle);
            Assert.IsFalse(harness.Client.Player.Missions.ContainsKey(Choices), "1078 not taken");

            var elder = Giver(copy, N.Elder);
            ForkTalk(harness, elder, Needle, MarshesNeedleInABaneCampV1.Contact, 1);
            PayNeedle(harness, elder);
            Assert.IsFalse(harness.Manager.TryAcceptRadioMission(harness.Client, Choices), "1078 is closed");
        }

        /// <summary>
        /// 1078 and 893 both held: Myrquar and his four guards south of the landing pads, walked to; his death completes 1 (once)
        /// and gives the key, and he ships out no more; the Disperse Logos completes 2 and sets down the lab, which completes 3;
        /// his death fails 893 (the
        /// choice); Toida on the Marshes (the client's flag-1 talk) takes the sample (4) and pays; the key and the sample go.
        /// </summary>
        [TestMethod]
        public void TheChoicesWeMakeKillsMyrquarAndBringsTheSampleToToida()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            EnterMarshes(harness);
            SeedCompletedMarshes(harness, LostOnes);
            var copy = IntoRuins(harness);
            harness.Manager.OfferArrivalMissions(harness.Client);
            harness.Drain();
            TakeByRadio(harness, copy, Choices, Needle);
            var toida = PlayTheChoicesToToida(harness, copy, true);
            ForkTalk(harness, toida, Choices, C.Return, 1);
            Assert.AreEqual(MissionState.Failed, harness.Client.Player.Missions[Needle].State, "the village stays lost");
            PayTheChoices(harness, toida);
        }

        /// <summary>1078 alone, 893 declined (never taken): Toida's flag-1 talk takes the sample and she pays; 893 is then closed.</summary>
        [TestMethod]
        public void TheChoicesWeMakeAloneAfterNeedleIsDeclined()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            EnterMarshes(harness);
            SeedCompletedMarshes(harness, LostOnes);
            var copy = IntoRuins(harness);
            harness.Manager.OfferArrivalMissions(harness.Client);
            harness.Drain();
            TakeByRadio(harness, copy, Choices);
            var toida = PlayTheChoicesToToida(harness, copy, false);
            Assert.IsFalse(harness.Client.Player.Missions.ContainsKey(Needle), "893 not taken");
            ForkTalk(harness, toida, Choices, C.Return, 1);
            PayTheChoices(harness, toida);
            Assert.IsFalse(harness.Manager.TryAcceptRadioMission(harness.Client, Needle), "893 is closed");
        }

        /// <summary>
        /// The choice is the player's for good (the choice flag): 1078 taken alone, Myrquar killed; 893 then cannot be taken (its
        /// arrival offer still pending), not even after 1078 is abandoned (the choice stays, by design).
        /// </summary>
        [TestMethod]
        public void TheChoiceAtMyrquarClosesNeedleTakenAfter()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            EnterMarshes(harness);
            SeedCompletedMarshes(harness, LostOnes);
            var copy = IntoRuins(harness);
            harness.Manager.OfferArrivalMissions(harness.Client);
            harness.Drain();
            TakeByRadio(harness, copy, Choices);
            Kill(harness, copy, SceneCreature(harness, copy, Choices, C.MyrquarRole));
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Choices, C.Myrquar));
            Assert.IsFalse(harness.Manager.TryAcceptRadioMission(harness.Client, Needle), "893 is closed by the choice at Myrquar");
            harness.Drain();
            harness.Manager.OfferArrivalMissions(harness.Client);
            Assert.IsFalse(harness.Drain().OfType<DispenseRadioMissionPacket>().Any(packet => packet.MissionId == Needle), "nor offered");
            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Choices));
            harness.Manager.Scenes.Tick(copy);
            harness.Drain();
            Assert.IsFalse(harness.Manager.TryAcceptRadioMission(harness.Client, Needle), "the choice stays after an abandon");
        }

        /// <summary>
        /// The mirror: 893 taken alone, the elder's talk done but not yet paid; 1078 (its arrival offer pending) cannot be taken
        /// then, nor after the pay.
        /// </summary>
        [TestMethod]
        public void TheChoiceAtTheElderClosesTheChoicesTakenAfter()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            EnterMarshes(harness);
            SeedCompletedMarshes(harness, LostOnes);
            var copy = IntoRuins(harness);
            harness.Manager.OfferArrivalMissions(harness.Client);
            harness.Drain();
            TakeByRadio(harness, copy, Needle);
            var elder = Giver(copy, N.Elder);
            ForkTalk(harness, elder, Needle, MarshesNeedleInABaneCampV1.Contact, 1);
            Assert.IsFalse(harness.Manager.TryAcceptRadioMission(harness.Client, Choices), "1078 is closed between the talk and the pay");
            harness.Drain();
            PayNeedle(harness, elder);
            Assert.IsFalse(harness.Manager.TryAcceptRadioMission(harness.Client, Choices), "and after it");
        }

        /// <summary>
        /// 893 and 1078 taken on the Marshes (offered at 1076's radio turn-in): their scenes are bound to the squad's Village
        /// Ruins, so the elder's talk there fails 1078 and sends Myrquar away.
        /// </summary>
        [TestMethod]
        public void NeedleTakenOnTheMarshesStillClosesTheChoicesInTheRuins()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            Accept(harness, Npc(marshes, MarshesTheLostOnesV1.Morrison), LostOnes);
            var copy = IntoRuins(harness);
            Enter(harness, ToVector(MarshesTheLostOnesV1.TravelAt));
            Tick(harness, copy, 250);
            Use(harness, copy, SceneObject(harness, copy, LostOnes, MarshesTheLostOnesV1.SpotRole), MarshesTheLostOnesV1.UseArgument);
            Assert.IsTrue(harness.Client.Player.Missions[LostOnes].Completeable);
            harness.Manager.Scenes.Detach(harness.Client, copy);
            EnterMarshes(harness);
            Assert.IsTrue(harness.Manager.TryCompleteRadioMission(harness.Client, LostOnes, 2, null));
            harness.Drain();
            Assert.IsTrue(harness.Manager.TryAcceptRadioMission(harness.Client, Needle), "893 taken on the Marshes");
            Assert.IsTrue(harness.Manager.TryAcceptRadioMission(harness.Client, Choices), "1078 taken on the Marshes");
            harness.Drain();

            copy = IntoRuins(harness);
            Tick(harness, copy, 250);
            Assert.IsNotNull(SceneCreature(harness, copy, Choices, C.MyrquarRole), "Myrquar stands in the Ruins");
            ForkTalk(harness, Giver(copy, N.Elder), Needle, MarshesNeedleInABaneCampV1.Contact, 1);
            Tick(harness, copy, 250);
            Assert.AreEqual(MissionState.Failed, harness.Client.Player.Missions[Choices].State, "1078 fails");
            Assert.IsNull(SceneCreature(harness, copy, Choices, C.MyrquarRole), "Myrquar goes");
        }

        /// <summary>
        /// 1078: 25 minutes after the accept Myrquar ships out: 1 fails, the mission with it, and he and his guards go. The
        /// failed 1078, once cleared from the journal, is offered again on the next arrival (a retry); 893 (a failed 1078 is not the other path done) is taken
        /// alone, the elder's flag-1 talk completes it, and he pays.
        /// </summary>
        [TestMethod]
        public void TheChoicesWeMakeFailsWhenMyrquarShipsOutAndNeedleStillPays()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            EnterMarshes(harness);
            SeedCompletedMarshes(harness, LostOnes);
            var copy = IntoRuins(harness);
            harness.Manager.OfferArrivalMissions(harness.Client);
            harness.Drain();
            TakeByRadio(harness, copy, Choices);
            Assert.IsNotNull(SceneCreature(harness, copy, Choices, C.MyrquarRole));

            for (var second = 0; second < C.DeadlineSeconds - 20; second += 20)
                Tick(harness, copy, 20000);
            harness.Manager.EvaluateDeadlines(harness.Client);
            harness.Drain();
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Choices, C.Myrquar), "not before 25 minutes");
            for (var second = 0; second < 40; second++)
                Tick(harness, copy, 1000);
            harness.Manager.EvaluateDeadlines(harness.Client);
            harness.Drain();
            Tick(harness, copy, 250);
            Tick(harness, copy, 250);
            Assert.AreEqual(MissionObjectiveState.Failed, Objective(harness, Choices, C.Myrquar), "shipped out");
            Assert.AreEqual(MissionState.Failed, harness.Client.Player.Missions[Choices].State);
            Assert.IsNull(SceneCreature(harness, copy, Choices, C.MyrquarRole), "Myrquar is gone");
            for (var guard = 0; guard < C.GuardsAt.Length; guard++)
                Assert.IsNull(SceneCreature(harness, copy, Choices, C.GuardRole(guard)), $"guard {guard + 1} with him");
            // Still in the journal, it is not offered; cleared from it, it is, on arrival (a retry).
            harness.Manager.OfferArrivalMissions(harness.Client);
            Assert.IsFalse(harness.Drain().OfType<DispenseRadioMissionPacket>().Any(packet => packet.MissionId == Choices),
                "not while the failed 1078 is in the journal");
            Assert.IsTrue(harness.Manager.TryClear(harness.Client, Choices));
            harness.Drain();
            harness.Manager.OfferArrivalMissions(harness.Client);
            Assert.IsTrue(harness.Drain().OfType<DispenseRadioMissionPacket>().Any(packet => packet.MissionId == Choices),
                "the cleared 1078 is offered again on arrival");

            // 893's arrival offer (made with 1078's) still stands.
            TakeByRadio(harness, copy, Needle);
            var elder = Giver(copy, N.Elder);
            ForkTalk(harness, elder, Needle, MarshesNeedleInABaneCampV1.Contact, 1);
            PayNeedle(harness, elder);
        }

        /// <summary>
        /// 1077 with the Disperse Logos already learned: Qunle's key at the accept, 1 completes at once (an existing fact); his
        /// lab, the player's own and apart from 1078's, stands from the accept (2 open with 1); done, Toida takes the sample and
        /// pays; the key goes.
        /// </summary>
        [TestMethod]
        public void DirtySecretsCountsAnOwnedDisperseAndBringsTheSampleToToida()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            EnterMarshes(harness);
            var copy = IntoRuins(harness);
            PlateauTrinityBridgeLogosTests.UseShrine(harness, copy, Disperse(copy));
            Assert.IsTrue(harness.Client.Player.Logos.Contains(C.DisperseLogos), "Disperse learned");

            Accept(harness, Giver(copy, N.Qunle), Secrets);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, C.KeyTemplate), "Qunle's key");
            Tick(harness, copy, 250);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Secrets, MarshesDirtySecretsV1.Element), "the Logos counts");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Secrets, MarshesDirtySecretsV1.Equipment));
            foreach (var at in MarshesDirtySecretsV1.PiecesAt.Append(MarshesDirtySecretsV1.CrateAt))
                foreach (var other in C.PiecesAt.Append(C.CrateAt))
                    Assert.IsGreaterThan(3f, Vector3.Distance(ToVector(at), ToVector(other)), "apart from 1078's lab");

            PlayTheLab(harness, copy, Secrets, MarshesDirtySecretsV1.PiecesAt, MarshesDirtySecretsV1.Equipment, MarshesDirtySecretsV1.LabTally);
            Tick(harness, copy, 250);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Secrets, MarshesDirtySecretsV1.Return));

            harness.Manager.Scenes.Detach(harness.Client, copy);
            var marshes = EnterMarshes(harness);
            var toida = Npc(marshes, N.Toida);
            Talk(harness, toida, Secrets, MarshesDirtySecretsV1.Return);
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, C.SampleTemplate), "Toida takes the sample");
            Assert.IsTrue(harness.Client.Player.Missions[Secrets].Completeable);
            FailedTurnInPaysNothing(harness, toida, Secrets, null);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, toida, Secrets, null,
                MarshesDirtySecretsV1.Experience, MarshesDirtySecretsV1.Credits);
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, C.KeyTemplate), "the key goes");
        }

        /// <summary>
        /// The lab of <paramref name="mission"/>: its three pieces (a piece counted once, at its destruction) and the crate
        /// (one sample, once) in the order crate, piece 3, 1, 2, with a reload after the crate; the hidden tally then the lab
        /// objective complete.
        /// </summary>
        private static void PlayTheLab(WildernessRuntimeTestHarness harness, MapChannel copy, uint mission, ScenePosition[] pieces,
            uint equipment, uint tally)
        {
            var crate = SceneObject(harness, copy, mission, C.CrateRole);
            Assert.AreEqual(C.CrateClass, (uint)crate.EntityClassId);
            AssertOnGround(copy, crate.Position, "the crate");
            AssertPath(copy, DoorEdge, crate.Position, "the crate from the door");
            Use(harness, copy, crate, C.CrateArgument);
            Use(harness, copy, crate, C.CrateArgument);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, C.SampleTemplate), "one sample");
            Assert.AreEqual(1U, Counter(harness, mission, tally));
            ReloadAndRecover(harness, copy);
            Assert.AreEqual(1U, Counter(harness, mission, tally), "kept at a reload");
            var order = new[] { 2, 0, 1 };
            for (var step = 0; step < order.Length; step++)
            {
                var piece = SceneObject(harness, copy, mission, C.PieceRole(order[step]));
                Assert.AreEqual(C.EquipmentClass, (uint)piece.EntityClassId);
                Assert.IsLessThan(0.5f, Vector3.Distance(ToVector(pieces[order[step]]), piece.Position), "at its place");
                AssertOnGround(copy, piece.Position, piece.SceneActorRole);
                AssertPath(copy, DoorEdge, piece.Position, $"{piece.SceneActorRole} from the door");
                Damage(harness, copy, piece, 60);
                Assert.AreEqual((uint)step + 1, Counter(harness, mission, tally), "not at 60 of 100");
                Damage(harness, copy, piece, 60);
                Damage(harness, copy, piece, 60);
                Assert.AreEqual((uint)step + 2, Counter(harness, mission, tally), $"piece {order[step] + 1}, once");
                Assert.AreEqual(C.DestroyedState, (uint)piece.StateId);
                if (step < order.Length - 1)
                    Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, mission, equipment));
            }
            Tick(harness, copy, 250);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, mission, tally));
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, mission, equipment));
        }

        /// <summary>The radio missions taken (their offers already made), the scene run on.</summary>
        private static void TakeByRadio(WildernessRuntimeTestHarness harness, MapChannel copy, params uint[] missions)
        {
            foreach (var mission in missions)
                Assert.IsTrue(harness.Manager.TryAcceptRadioMission(harness.Client, mission), $"{mission} taken");
            harness.Drain();
            Tick(harness, copy, 250);
        }

        /// <summary>
        /// The path's last talk: the NPC offers this objective's hand-in once, under the client's flag <paramref name="flag"/>
        /// (whether or not the other path is held), and it is done.
        /// </summary>
        private static void ForkTalk(WildernessRuntimeTestHarness harness, Creature npc, uint mission, uint objective, uint flag)
        {
            var conversation = Conversation(harness, npc);
            Assert.IsTrue(conversation.TryGetValue(ConversationType.ObjectiveComplete, out var value), $"hand-in of {mission}.{objective}");
            var topics = ((List<CompleteableObjectives>)value).Where(topic => topic.MissionId == mission && topic.ObjectiveId == objective).ToArray();
            Assert.AreEqual(1, topics.Length, $"one {mission}.{objective} topic");
            Assert.AreEqual((int)flag, topics[0].PlayerFlagId, "the client's flag");
            harness.MoveTo(npc.Position);
            harness.Drain();
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, npc.EntityId, mission, objective, flag), $"{mission}.{objective}");
            harness.Drain();
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, mission, objective));
            Assert.IsTrue(harness.Client.Player.Missions[mission].Completeable);
        }

        private static void PayNeedle(WildernessRuntimeTestHarness harness, Creature elder)
        {
            FailedTurnInPaysNothing(harness, elder, Needle, 0);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, elder, Needle, 0,
                MarshesNeedleInABaneCampV1.Experience, MarshesNeedleInABaneCampV1.Credits);
            Assert.AreEqual(MarshesNeedleInABaneCampV1.EmpBombs, WildernessRanjaGorgeTests.Held(harness, MarshesNeedleInABaneCampV1.EmpBomb), "the chosen stack");
        }

        private static void PayTheChoices(WildernessRuntimeTestHarness harness, Creature toida)
        {
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, C.SampleTemplate), "Toida takes the sample");
            FailedTurnInPaysNothing(harness, toida, Choices, 1);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, toida, Choices, 1, C.Experience, C.Credits);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, C.PhotonicBlade), "the chosen blade");
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, C.KeyTemplate), "the key goes");
        }

        /// <summary>
        /// 1078 played to Toida: Myrquar and his guards, his death (1, the key; with <paramref name="pastTheDeadline"/> the 25
        /// minutes then pass with no failure and his guards stay), the Disperse Logos (2), the lab (3); back on the Marshes.
        /// </summary>
        private static Creature PlayTheChoicesToToida(WildernessRuntimeTestHarness harness, MapChannel copy, bool pastTheDeadline)
        {
            var myrquar = SceneCreature(harness, copy, Choices, C.MyrquarRole);
            Assert.IsNotNull(myrquar, "Myrquar stands");
            Assert.AreEqual((N.Myrquar, N.MyrquarName, N.MyrquarLevel), (myrquar.DbId, myrquar.NameId, myrquar.Level));
            Assert.AreEqual((int)N.MyrquarHitPoints, (int)myrquar.Attributes[Attributes.Health].Current);
            Assert.AreEqual(TargetCategory.Hostile, myrquar.TargetCategory);
            AssertOnGround(copy, myrquar.Position, "Myrquar");
            AssertPath(copy, DoorEdge, myrquar.Position, "Myrquar from the door");
            for (var guard = 0; guard < C.GuardsAt.Length; guard++)
            {
                var thrax = SceneCreature(harness, copy, Choices, C.GuardRole(guard));
                Assert.IsNotNull(thrax, $"guard {guard + 1}");
                AssertOnGround(copy, thrax.Position, $"guard {guard + 1}");
            }
            Assert.IsNull(FindSceneObject(harness, copy, Choices, C.PieceRole(0)), "no lab before the element");

            Kill(harness, copy, myrquar);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Choices, C.Myrquar));
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, C.KeyTemplate), "his key");
            if (harness.Client.Player.Missions.TryGetValue(Needle, out var needle))
                Assert.AreEqual(MissionState.Failed, needle.State, "the choice made at Myrquar: the village is lost");
            if (pastTheDeadline)
            {
                // Dead, he ships out no more: past the 25 minutes the mission goes on and his guards stay.
                for (var second = 0; second < C.DeadlineSeconds + 20; second += 20)
                    Tick(harness, copy, 20000);
                harness.Manager.EvaluateDeadlines(harness.Client);
                harness.Drain();
                Assert.AreEqual(MissionState.Active, harness.Client.Player.Missions[Choices].State, "no deadline once he is dead");
                Assert.IsNotNull(SceneCreature(harness, copy, Choices, C.GuardRole(0)), "his guards stay");
            }
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Choices, C.Element));
            AssertPath(copy, DoorEdge, ToVector(C.DisperseAt) with { Y = 63.77f }, "the Logos from the door");
            PlateauTrinityBridgeLogosTests.UseShrine(harness, copy, Disperse(copy));
            Tick(harness, copy, 250);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Choices, C.Element));
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Choices, C.Equipment));

            PlayTheLab(harness, copy, Choices, C.PiecesAt, C.Equipment, C.LabTally);
            Tick(harness, copy, 250);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Choices, C.Return));
            Assert.IsFalse(harness.Client.Player.Missions[Choices].Completeable, "not before Toida");

            harness.Manager.Scenes.Detach(harness.Client, copy);
            var marshes = EnterMarshes(harness);
            return Npc(marshes, N.Toida);
        }

        private static Logos Disperse(MapChannel copy) =>
            copy.DynamicObjects.OfType<Logos>().Single(entry => entry.Id == C.DisperseLogos);

        /// <summary>Into the squad's copy of the Village Ruins through its door on the Marshes, the scenes resumed (as MapLoaded).</summary>
        internal static MapChannel IntoRuins(WildernessRuntimeTestHarness harness)
        {
            if (!harness.Maps.MapChannelArray.ContainsKey(Ruins))
                LoadMap(harness, Ruins);
            var copy = ThroughTheDoor(harness, Ruins);
            harness.Manager.Scenes.Resume(harness.Client);
            harness.Drain();
            return copy;
        }

        /// <summary>Into a mission area at <paramref name="at"/>, as MissionAreaService takes a step from far off.</summary>
        internal static void Enter(WildernessRuntimeTestHarness harness, Vector3 at)
        {
            var from = at + new Vector3(60, 0, 60);
            harness.MoveTo(at);
            new MissionAreaService(() => harness.Manager).RecordAcceptedMovement(harness.Client, from, at);
            harness.Drain();
        }

        /// <summary>The mission state as the database holds it again, and the scene out and back in (MarshesEscortsTests).</summary>
        internal static void ReloadAndRecover(WildernessRuntimeTestHarness harness, MapChannel copy)
        {
            WildernessRanjaGorgeTests.Reload(harness);
            harness.Manager.Scenes.Detach(harness.Client, copy);
            harness.Manager.Scenes.Resume(harness.Client);
            harness.Drain();
            Tick(harness, copy, 250);
            Tick(harness, copy, 250);
        }

        /// <summary>The living creatures of this copy only (PoolsTurpisRefineryTests.Here).</summary>
        internal static IEnumerable<Creature> Here(MapChannel map) =>
            Living(map).Where(creature => ReferenceEquals(creature.RuntimeMapChannel, map));

        internal static Creature Giver(MapChannel copy, uint pool)
        {
            if (!Here(copy).Any(creature => creature.SpawnPool?.DbId == pool))
                Spawn(copy, pool);
            var giver = Here(copy).SingleOrDefault(creature => creature.SpawnPool?.DbId == pool && creature.Npc != null);
            Assert.IsNotNull(giver, $"pool {pool} stands in the copy");
            return giver;
        }

        internal static HashSet<string> Runs(WildernessRuntimeTestHarness harness, uint mission)
        {
            using var unit = harness.CreateChar();
            return unit.CharacterMissions.Runtime.Scenes(harness.Client.Player.Id, mission).Select(scene => scene.RunId).ToHashSet();
        }

        /// <summary>The object of this test's own run, if it is there: a squad copy can still hold an earlier test's.</summary>
        internal static DynamicObject FindSceneObject(WildernessRuntimeTestHarness harness, MapChannel map, uint mission, string role)
        {
            var runs = Runs(harness, mission);
            return map.DynamicObjects.SingleOrDefault(candidate => candidate.SceneMissionId == mission && candidate.SceneActorRole == role &&
                candidate.IsInWorld && runs.Contains(candidate.SceneRunId));
        }

        internal static DynamicObject SceneObject(WildernessRuntimeTestHarness harness, MapChannel map, uint mission, string role)
        {
            var obj = FindSceneObject(harness, map, mission, role);
            Assert.IsNotNull(obj, $"{role} is there");
            return obj;
        }

        internal static Creature SceneCreature(WildernessRuntimeTestHarness harness, MapChannel map, uint mission, string role)
        {
            var runs = Runs(harness, mission);
            return Here(map).SingleOrDefault(creature => creature.SpawnPool?.SceneActorRole == role && runs.Contains(creature.SpawnPool.SceneRunId) &&
                creature.State != CharacterState.Dead);
        }

        /// <summary>Kills the creature as the player and runs the scene on.</summary>
        internal static void Kill(WildernessRuntimeTestHarness harness, MapChannel map, Creature creature)
        {
            Assert.IsNotNull(creature);
            harness.MoveTo(creature.Position);
            creature.Attributes[Attributes.Health].Current = 0;
            harness.Creatures.HandleCreatureKill(map, creature, harness.Client.Player);
            harness.Drain();
            Tick(harness, map, 250);
            Tick(harness, map, 250);
        }

        /// <summary>The clock on (PoolsTurpisRefineryTests.Tick), the player kept at full health (MarshesLogosResearchTests.Steady).</summary>
        internal static void Tick(WildernessRuntimeTestHarness harness, MapChannel map, long milliseconds)
        {
            Steady(harness);
            harness.UtcNow = harness.UtcNow.AddMilliseconds(milliseconds);
            BehaviorManager.Instance.MapChannelThink(map, System.Math.Min(milliseconds, 1000));
            harness.Manager.Scenes.Tick(map);
            harness.Drain();
        }

        /// <summary>A use of the object from beside it, and its recovery if one is due (PoolsTurpisRefineryTests.Use).</summary>
        internal static void Use(WildernessRuntimeTestHarness harness, MapChannel map, DynamicObject obj, uint argument)
        {
            harness.MoveTo(obj.Position + new Vector3(0, 0, 1));
            harness.Drain();
            var count = map.PerformRecovery.Count;
            harness.Objects.RequestUseObjectPacket(harness.Client, new RequestUseObjectPacket
            {
                EntityId = obj.EntityId, ActionId = ActionId.UseObject, ActionArgId = argument
            });
            if (map.PerformRecovery.Count > count)
            {
                var recovery = map.PerformRecovery.Last();
                map.PerformRecovery.Remove(recovery);
                ActorActionManager.Instance.PerformRecovery(map, recovery);
            }
            harness.Drain();
            harness.Manager.Scenes.Tick(map);
            harness.Drain();
        }

        /// <summary>The player kept standing: Myrquar's guards kill the test's low-level player (MarshesLogosResearchTests.Steady).</summary>
        internal static void Steady(WildernessRuntimeTestHarness harness)
        {
            var health = harness.Client.Player.Attributes[Attributes.Health];
            health.NormalMax = System.Math.Max(health.NormalMax, 1_000_000);
            health.Current = health.NormalMax;
        }

        /// <summary>A weapon hit on the object (PoolsTurpisRefineryTests.Damage).</summary>
        private static void Damage(WildernessRuntimeTestHarness harness, MapChannel map, DynamicObject obj, int damage)
        {
            Steady(harness);
            harness.MoveTo(obj.Position + new Vector3(0, 0, 1));
            var action = new ActionData(harness.Client.Player, ActionId.WeaponAttack, 133, 0) { TargetId = obj.EntityId };
            MissileManager.Instance.MissileLaunch(map, action, damage);
            MissileManager.Instance.DoWork(map, 1000);
            harness.Manager.Scenes.Tick(map);
            harness.Drain();
        }

        private static uint Counter(WildernessRuntimeTestHarness harness, uint mission, uint objective) =>
            harness.Client.Player.Missions[mission].Objectives[objective].Counters[0];

        /// <summary>A complete path on the navmesh from <paramref name="from"/> to <paramref name="to"/>.</summary>
        internal static void AssertPath(MapChannel map, Vector3 from, Vector3 to, string what) =>
            Assert.IsTrue(map.NavMesh.FindPath(from, to, out var complete) != null && complete, $"{what}: a path");

        internal static Vector3 ToVector((double X, double Y, double Z) at) => new((float)at.X, (float)at.Y, (float)at.Z);

        internal static Vector3 ToVector(ScenePosition at) => new(at.X, at.Y, at.Z);
    }
}
