using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Managers;
using Rasa.Packets.MapChannel.Server;
using Rasa.Packets.Mission.Server;
using Rasa.Services.Preloader.Missions.Crucible;
using Rasa.Structures;
using Rasa.Structures.Char;
using Rasa.Test.Missions.Marshes;
using Rasa.Test.Missions.Plateau;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions.Crucible
{
    using static MarshesFalconHoldTests;
    using static MarshesPaludosTests;
    using static PlateauCampResistanceTests;
    using G = CrucibleGrupaChiefsNpcsV1;
    using N = CrucibleOutpostIntrepidNpcsV1;
    using C = CrucibleOutpostIntrepidTests;

    /// <summary>
    /// Crucible set 2, the Grupa chiefs, played through as a player plays it: Captain DeLisi's The Boss Of All Bosses (1161:
    /// Kappa Grupa Lohen in his office at the top of Staal, by the office's teleporters), Larai Grupa Vitto's The Protection
    /// Racket (1146: the four merchants' money; Vargo's pet Flare Gasher Roscoe), Top Khana Grupa Munit's Smoking Guns (1148:
    /// three arms shipments to Fiorella, Haggus and Melching, whose text follows Calendar Girl's flag) and Labbna Grupa Rigs'
    /// Junkyard Dogs (1149: twenty Flare Gashers).
    ///
    /// Why these tests: two NPCs and Roscoe are new, four upstream ones moved, the office is reached only by its two new
    /// teleporters, the money and the shipments are issued at talks or the accept and taken at talks or the turn-in, Roscoe
    /// is the player's own and only Vargo's call brings him, Melching has three texts, and the chain gates each mission. A
    /// target off the navmesh, a teleporter that goes nowhere, an offer before its prerequisite, a talk at the wrong NPC or
    /// with the wrong variant, money paid before Roscoe dies, a kill not counted or lost at a reload, a full bag that loses an
    /// item, a token not paid, or a turn-in that pays twice would each break them.
    ///
    /// The map: Ligo Crucible (1993), loaded as CrucibleOutpostIntrepidTests.EnterCrucible does (no upstream test does this
    /// for the Crucible).
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class CrucibleGrupaChiefsTests
    {
        private const uint LastMen = CrucibleLastMenStandingV1.MissionId;
        private const uint Fort = CrucibleFortCalledIntrepidV1.MissionId;
        private const uint Wire = CrucibleWireV1.MissionId;
        private const uint Boss = CrucibleBossOfAllBossesV1.MissionId;
        private const uint Racket = CrucibleProtectionRacketV1.MissionId;
        private const uint Guns = CrucibleSmokingGunsV1.MissionId;
        private const uint Dogs = CrucibleJunkyardDogsV1.MissionId;
        private const uint MissionItemFiller = 11519;      // as PlateauCampResistanceTests (inventory category 4)
        private const uint ConsumableFiller = 45060;       // Class VII Basic Med Pack (as MarshesStalkerWoodsTests): fills the consumables bag
        private const uint WeaponFiller = 3869;            // inventory category 1 (quest-test-pattern)
        private const uint ArmorFiller = 12748;            // Armor_T3_Bio_V01_CMN_Gloves_30_to_34 (inventory category 1)

        /// <summary>The waypoint of the Awol Camp (World DB teleporter 192), where a player arrives.</summary>
        private static readonly Vector3 AwolWaypoint = new(-945.54f, 309.45f, 244.47f);
        private static readonly Vector3 LowerPad = new(-206.738f, 183.3619f, 317.945f), UpperPad = new(-214.899f, 244.4991f, 319.7714f);

        [TestInitialize]
        public void Clock() => PlateauGiantKillerSquadTests.UseClock();

        [TestCleanup]
        public void Restore() => PlateauGiantKillerSquadTests.RestoreClock();

        /// <summary>
        /// Every NPC set 2 talks to stands on the navmesh at its place, with the client's name and package, friendly, and is
        /// walked to from its area's waypoint: the merchants from Staal's, Lohen from the office's teleporter, Fiorella from
        /// Outpost Intrepid's, Haggus from the Labbna Quarter's, Melching from the Awol Camp's.
        /// </summary>
        [TestMethod]
        public void TheNpcsStandOnTheNavmeshAndAreWalkedTo()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var crucible = C.EnterCrucible(harness);
            foreach (var (pool, name, package, from) in new (uint, uint, uint, Vector3)[]
            {
                (G.Bunko, 9070, 1224, C.StaalWaypoint), (G.Yeg, 9136, 1272, C.StaalWaypoint), (G.Cugin, 9137, 1269, C.StaalWaypoint),
                (G.Vargo, 9138, 1271, C.StaalWaypoint), (G.Lohen, 9071, 1222, UpperPad), (G.Fiorella, 9157, 1296, C.OutpostWaypoint),
                (G.Haggus, 9065, 1232, C.LabbnaWaypoint), (G.Melching, 9156, 1295, AwolWaypoint)
            })
            {
                var npc = Npc(crucible, pool);
                Assert.AreEqual(name, npc.NameId, $"{pool} name");
                Assert.AreEqual(package, npc.Npc.NpcPackageId, $"{pool} package");
                Assert.AreNotEqual(TargetCategory.Hostile, npc.TargetCategory);
                var at = CrucibleStaalWalksV1.Now(pool) ?? G.At(pool);
                Assert.IsLessThan(0.1f, Vector2.Distance(new((float)at.X, (float)at.Z), new(npc.Position.X, npc.Position.Z)), $"{pool} at its place");
                AssertOnGround(crucible, npc.Position, $"{pool}");
                var start = crucible.NavMesh.Nearest(from);
                Assert.IsNotNull(start, $"{pool}: the navmesh at its waypoint");
                Assert.IsTrue(crucible.NavMesh.FindPath(start.Value, npc.Position, out var complete) != null && complete, $"{pool} is walked to");
            }
            Assert.IsFalse(crucible.NavMesh.FindPath(C.StaalWaypoint, Npc(crucible, G.Lohen).Position, out var walked) != null && walked,
                "the office is not walked to from Staal: the teleporter takes the player there");
        }

        /// <summary>
        /// The Kappa Grupa's office: the lower teleporter by the Staal waypoint takes a player who steps onto it to the office,
        /// and the office's takes him back; neither takes him again until he has stepped off.
        /// </summary>
        [TestMethod]
        public void TheOfficeTeleportersTakeThePlayerUpAndBack()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var crucible = C.EnterCrucible(harness, C.StaalWaypoint);
            OfficePads(harness, crucible);
            Assert.IsTrue(crucible.Teleporters.TryGetValue(G.LowerPad, out var lower), "the lower teleporter");
            Assert.IsTrue(crucible.Teleporters.TryGetValue(G.UpperPad, out var upper), "the office's teleporter");
            Assert.IsLessThan(0.01f, Vector3.Distance(LowerPad, lower.Position));
            Assert.IsLessThan(0.01f, Vector3.Distance(UpperPad, upper.Position));
            var lowerStand = crucible.NavMesh.Nearest(LowerPad);
            Assert.IsTrue(crucible.NavMesh.FindPath(crucible.NavMesh.Nearest(C.StaalWaypoint).Value, lowerStand.Value, out var complete) != null && complete,
                "the lower teleporter is walked to from the Staal waypoint");

            Teleport(harness, crucible, lower);
            Assert.IsLessThan(3f, Vector3.Distance(UpperPad, harness.Client.Player.Position), "in the office");
            harness.Objects.DynamicObjectProximityWorker(crucible, upper, 0);
            Assert.IsNull(harness.Client.PendingTransfer, "not sent straight back");

            harness.MoveTo(Npc(crucible, G.Lohen).Position);
            harness.Drain();
            harness.Objects.DynamicObjectProximityWorker(crucible, upper, 0);
            Assert.IsNull(harness.Client.PendingTransfer, "off the pad");
            Teleport(harness, crucible, upper);
            Assert.IsLessThan(3f, Vector3.Distance(LowerPad, harness.Client.Player.Position), "back by the Staal waypoint");
        }

        /// <summary>
        /// The Boss Of All Bosses after The Wire: DeLisi offers it, Lohen in his office hears it (only he), a reload halfway;
        /// Lohen pays at the turn-in, once; then Vitto, Munit and Rigs offer their token missions.
        /// </summary>
        [TestMethod]
        public void TheBossOfAllBossesFindsLohenInHisOffice()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var crucible = C.EnterCrucible(harness);
            var delisi = Npc(crucible, N.DeLisi);
            C.SeedCompleted(harness, LastMen, Fort);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, delisi, Boss), "it waits for The Wire");
            C.SeedCompleted(harness, Wire);
            crucible = C.EnterCrucible(harness);
            var lohen = Npc(crucible, G.Lohen);
            var vitto = Npc(crucible, N.Vitto);
            var munit = Npc(crucible, N.Munit);
            var rigs = Npc(crucible, N.Rigs);
            foreach (var (npc, mission) in new[] { (vitto, Racket), (munit, Guns), (rigs, Dogs) })
                Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, npc, mission), $"{mission} waits for The Boss Of All Bosses");

            var offer = WildernessRanjaGorgeTests.Offer(harness, delisi, Boss);
            CollectionAssert.AreEqual(new[] { (111018U, 3U), (45060U, 6U), (45446U, 3U), (111232U, 1U) },
                offer.SelectableReward.Select(item => (item.ItemTemplateId, item.Quantity)).ToArray());
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, lohen, Boss), "not Lohen's to give");
            Accept(harness, delisi, Boss);
            Assert.IsEmpty(Topics(harness, delisi, Boss), "DeLisi hears no objective");
            Assert.IsEmpty(Topics(harness, vitto, Boss), "nor Vitto");
            Assert.IsFalse(harness.Manager.CompleteOfferedObjective(harness.Client, vitto.EntityId, Boss, CrucibleBossOfAllBossesV1.Locate, 1),
                "not Vitto's to hear");

            WildernessRanjaGorgeTests.Reload(harness);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Boss, CrucibleBossOfAllBossesV1.Locate), "kept at a reload");
            harness.Drain();

            AssertHandInTopic(Conversation(harness, lohen), Boss, CrucibleBossOfAllBossesV1.Locate);
            var packets = Say(harness, lohen, Boss, CrucibleBossOfAllBossesV1.Locate, 1);
            Assert.AreEqual(1, packets.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Boss), "ObjectiveCompleted once");
            Assert.AreEqual(1, packets.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Boss && packet.IsCompleteable),
                "MissionCompleteable once");
            Assert.IsEmpty(Topics(harness, lohen, Boss), "heard once");
            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, delisi.EntityId, Boss, 0), "not DeLisi's to take");

            FullBagWaits(harness, lohen, Boss, 0, ConsumableFiller);
            FailedTurnInPaysNothing(harness, lohen, Boss, 0);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, lohen, Boss, 0,
                CrucibleBossOfAllBossesV1.Experience, CrucibleBossOfAllBossesV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Boss), "MissionRewarded once");
            Assert.AreEqual(CrucibleBossOfAllBossesV1.ConcussionGrenades,
                WildernessRanjaGorgeTests.Held(harness, CrucibleBossOfAllBossesV1.ConcussionGrenade), "the three Concussion Grenades");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, delisi, Boss), "done once");
            foreach (var (npc, mission) in new[] { (vitto, Racket), (munit, Guns), (rigs, Dogs) })
                Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, npc, mission), $"{mission} follows");
        }

        /// <summary>
        /// The Protection Racket: Bunko, Cugin and Yeg pay (a full bag leaves the money with the merchant); Vargo refuses and
        /// Roscoe, the player's own Flare Gasher, comes out and attacks; only with him dead does Vargo pay; a reload halfway;
        /// Vitto takes the four payments and gives the Larai token with the weapon chosen.
        /// </summary>
        [TestMethod]
        public void TheProtectionRacketTakesTheMoneyAndRoscoe()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            C.SeedCompleted(harness, LastMen, Fort, Wire, Boss);
            var crucible = C.EnterCrucible(harness, C.StaalWaypoint);
            var vitto = Npc(crucible, N.Vitto);
            var merchants = CrucibleProtectionRacketV1.Payments.ToDictionary(payment => payment.Objective, payment => Npc(crucible, payment.Npc));
            var bunko = merchants[CrucibleProtectionRacketV1.Bunko];
            var vargo = merchants[CrucibleProtectionRacketV1.Vargo];

            var offer = WildernessRanjaGorgeTests.Offer(harness, vitto, Racket);
            CollectionAssert.AreEqual(new[] { CrucibleProtectionRacketV1.SonicPistol, CrucibleProtectionRacketV1.Shotgun },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            CollectionAssert.AreEqual(new[] { CrucibleProtectionRacketV1.LaraiToken },
                offer.FixedReward.FixedItems.Select(item => item.ItemTemplateId).ToArray(), "the Larai token with every choice");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, bunko, Racket), "not Bunko's to give");
            Accept(harness, vitto, Racket);
            Assert.IsNull(Roscoe(harness, crucible), "no Roscoe before Vargo's call");
            Assert.IsEmpty(Topics(harness, vitto, Racket), "Vitto hears no objective");
            foreach (var (objective, npc) in merchants)
                CollectionAssert.AreEqual(new[] { objective }, Topics(harness, npc, Racket), $"{npc.DbId} hears only {objective}");

            // No room for Bunko's money: his talk does not go through, and nothing is given.
            WildernessAliaBranchesTests.FillCategory(harness, MissionItemFiller, 50);
            harness.MoveTo(bunko.Position);
            harness.Drain();
            Assert.IsFalse(harness.Manager.CompleteOfferedObjective(harness.Client, bunko.EntityId, Racket, CrucibleProtectionRacketV1.Bunko, 1),
                "no room for the money");
            Assert.AreEqual(0U, Money(harness, 0));
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Racket, CrucibleProtectionRacketV1.Bunko));
            for (var slot = 0; slot < CrucibleProtectionRacketV1.Payments.Length; slot++)
                WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
            harness.Drain();

            var packets = new List<object>();
            packets.AddRange(Say(harness, bunko, Racket, CrucibleProtectionRacketV1.Bunko, 1));
            Assert.IsFalse(harness.Manager.CompleteOfferedObjective(harness.Client, bunko.EntityId, Racket, CrucibleProtectionRacketV1.Cugin, 1),
                "Bunko does not hear Cugin's");
            packets.AddRange(Say(harness, merchants[CrucibleProtectionRacketV1.Cugin], Racket, CrucibleProtectionRacketV1.Cugin, 1));
            Assert.AreEqual((1U, 1U), (Money(harness, 0), Money(harness, 1)), "Bunko's and Cugin's money");

            WildernessRanjaGorgeTests.Reload(harness);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Racket, CrucibleProtectionRacketV1.Cugin), "kept at a reload");
            Assert.AreEqual((1U, 1U), (Money(harness, 0), Money(harness, 1)), "the money kept at a reload");
            harness.Drain();

            // Vargo will not pay: his pet comes for the player.
            Assert.IsFalse(harness.Manager.CompleteOfferedObjective(harness.Client, vargo.EntityId, Racket, CrucibleProtectionRacketV1.VargoPays, 1),
                "no money before Roscoe");
            packets.AddRange(Say(harness, vargo, Racket, CrucibleProtectionRacketV1.Vargo, 1));
            Assert.AreEqual(0U, Money(harness, 2), "Vargo pays nothing yet");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Racket, CrucibleProtectionRacketV1.Roscoe), "Kill Roscoe opens");
            Assert.AreEqual(MissionObjectiveState.Inactive, Objective(harness, Racket, CrucibleProtectionRacketV1.VargoPays));
            Assert.IsEmpty(Topics(harness, vargo, Racket), "Vargo has nothing more to say while Roscoe lives");
            MarshesPreoDasTests.Tick(harness, crucible, 250);
            var roscoe = Roscoe(harness, crucible);
            Assert.IsNotNull(roscoe, "Roscoe comes out");
            Assert.AreEqual(G.Roscoe, roscoe.DbId);
            Assert.AreEqual(TargetCategory.Hostile, roscoe.TargetCategory);
            Assert.IsLessThan(3f, Vector2.Distance(new((float)G.RoscoeAt.X, (float)G.RoscoeAt.Z), new(roscoe.Position.X, roscoe.Position.Z)),
                $"at its place west of Vargo: {roscoe.Position}");
            AssertOnGround(crucible, roscoe.Position, "Roscoe");
            Assert.AreEqual(harness.Client.Player.EntityId, roscoe.Controller.ActionFighting.TargetEntityId, "Roscoe fights the player, not Vargo");

            // A reload with Roscoe alive: he is there again (or still), and Kill Roscoe stays open.
            WildernessRanjaGorgeTests.Reload(harness);
            MarshesPreoDasTests.Tick(harness, crucible, 250);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Racket, CrucibleProtectionRacketV1.Roscoe), "Kill Roscoe kept at a reload");
            roscoe = Roscoe(harness, crucible);
            Assert.IsNotNull(roscoe, "Roscoe is there after a reload");
            Assert.AreEqual(G.Roscoe, roscoe.DbId);
            Assert.AreNotEqual(CharacterState.Dead, roscoe.State);
            Assert.IsLessThan(3f, Vector2.Distance(new((float)G.RoscoeAt.X, (float)G.RoscoeAt.Z), new(roscoe.Position.X, roscoe.Position.Z)),
                $"at its place after a reload: {roscoe.Position}");
            Assert.AreEqual(1, Here(crucible).Count(creature => creature.DbId == G.Roscoe && creature.State != CharacterState.Dead), "one Roscoe");
            harness.Drain();

            packets.AddRange(Say(harness, merchants[CrucibleProtectionRacketV1.Yeg], Racket, CrucibleProtectionRacketV1.Yeg, 1));
            Assert.IsFalse(harness.Client.Player.Missions[Racket].Completeable, "not before Vargo pays");
            // As MarshesPreoDasTests.Kill, with the packets kept.
            harness.MoveTo(roscoe.Position);
            roscoe.Attributes[Attributes.Health].Current = 0;
            harness.Creatures.HandleCreatureKill(crucible, roscoe, harness.Client.Player);
            packets.AddRange(harness.Drain());
            MarshesPreoDasTests.Tick(harness, crucible, 250);
            MarshesPreoDasTests.Tick(harness, crucible, 250);
            packets.AddRange(harness.Drain());
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Racket, CrucibleProtectionRacketV1.Roscoe), "Roscoe is dead");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Racket, CrucibleProtectionRacketV1.VargoPays));
            CollectionAssert.AreEqual(new[] { CrucibleProtectionRacketV1.VargoPays }, Topics(harness, vargo, Racket), "now Vargo pays");
            packets.AddRange(Say(harness, vargo, Racket, CrucibleProtectionRacketV1.VargoPays, 1));
            Assert.AreEqual(1U, Money(harness, 2), "Vargo's money");
            CollectionAssert.AreEquivalent(new[] { 1U, 2U, 3U, 4U, 5U, 6U }, packets.OfType<ObjectiveCompletedPacket>()
                .Where(packet => packet.MissionId == Racket).Select(packet => packet.ObjectiveId).ToArray(), "ObjectiveCompleted once each");
            Assert.AreEqual(1, packets.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Racket && packet.IsCompleteable),
                "MissionCompleteable once");

            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, vargo.EntityId, Racket, 0), "not Vargo's to take");
            FullBagWaits(harness, vitto, Racket, 0, WeaponFiller);
            FailedTurnInPaysNothing(harness, vitto, Racket, 0);
            Assert.AreEqual(4U, Enumerable.Range(0, 4).Select(index => Money(harness, index)).Aggregate((a, b) => a + b), "kept at the failed turn-in");
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, vitto, Racket, 0,
                CrucibleProtectionRacketV1.Experience, CrucibleProtectionRacketV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Racket), "MissionRewarded once");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, CrucibleProtectionRacketV1.SonicPistol), "the Sonic Pistol chosen");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, CrucibleProtectionRacketV1.LaraiToken), "the Larai token");
            Assert.AreEqual(0U, Enumerable.Range(0, 4).Select(index => Money(harness, index)).Aggregate((a, b) => a + b), "Vitto takes the money");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, vitto, Racket), "done once");
        }

        /// <summary>An abandon takes the money and Roscoe away; taken again, Bunko pays again and Roscoe waits for Vargo.</summary>
        [TestMethod]
        public void TheProtectionRacketAbandonedTakesTheMoneyAndRoscoe()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            C.SeedCompleted(harness, LastMen, Fort, Wire, Boss);
            var crucible = C.EnterCrucible(harness, C.StaalWaypoint);
            var vitto = Npc(crucible, N.Vitto);
            var bunko = Npc(crucible, G.Bunko);
            var vargo = Npc(crucible, G.Vargo);
            Accept(harness, vitto, Racket);
            Talk(harness, bunko, Racket, CrucibleProtectionRacketV1.Bunko);
            Talk(harness, vargo, Racket, CrucibleProtectionRacketV1.Vargo);
            MarshesPreoDasTests.Tick(harness, crucible, 250);
            Assert.IsNotNull(Roscoe(harness, crucible), "Roscoe is out");

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Racket));
            MarshesPreoDasTests.Tick(harness, crucible, 250);
            Assert.AreEqual(0U, Money(harness, 0), "the money goes");
            Assert.IsFalse(Here(crucible).Any(creature => creature.DbId == G.Roscoe && creature.State != CharacterState.Dead), "Roscoe goes");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, vitto, Racket), "offered again after an abandon");
            Accept(harness, vitto, Racket);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Racket, CrucibleProtectionRacketV1.Bunko), "the old payment does not count");
            Assert.IsNull(Roscoe(harness, crucible), "no Roscoe before Vargo's call");
            Talk(harness, bunko, Racket, CrucibleProtectionRacketV1.Bunko);
            Assert.AreEqual(1U, Money(harness, 0));
        }

        /// <summary>
        /// Smoking Guns: the three shipments at the accept (a full bag: no mission, three slots freed: the shipments);
        /// Fiorella, Haggus and Melching each take theirs (only theirs), in any order; a reload halfway; Munit pays the token.
        /// </summary>
        [TestMethod]
        public void SmokingGunsDeliversTheThreeShipments()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            C.SeedCompleted(harness, LastMen, Fort, Wire, Boss);
            var crucible = C.EnterCrucible(harness, C.StaalWaypoint);
            var munit = Npc(crucible, N.Munit);
            var receivers = CrucibleSmokingGunsV1.Deliveries.ToDictionary(delivery => delivery.Objective, delivery => Npc(crucible, delivery.Npc));
            var fiorella = receivers[CrucibleSmokingGunsV1.Intrepid];
            var haggus = receivers[CrucibleSmokingGunsV1.Labbna];
            var melching = receivers[CrucibleSmokingGunsV1.Awol];

            var offer = WildernessRanjaGorgeTests.Offer(harness, munit, Guns);
            CollectionAssert.AreEqual(new[] { CrucibleSmokingGunsV1.BioGloves, CrucibleSmokingGunsV1.GravitonVest },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            CollectionAssert.AreEqual(new[] { CrucibleSmokingGunsV1.TopKhanaToken },
                offer.FixedReward.FixedItems.Select(item => item.ItemTemplateId).ToArray(), "the Top Khana token with every choice");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, fiorella, Guns), "not Fiorella's to give");

            // No room for the three shipments: no mission. Three slots freed, they come with it.
            WildernessAliaBranchesTests.FillCategory(harness, MissionItemFiller, 50);
            harness.MoveTo(munit.Position);
            harness.Drain();
            Assert.IsFalse(harness.Manager.AcceptOfferedMission(harness.Client, munit.EntityId, Guns), "no room for the shipments");
            Assert.IsFalse(harness.Client.Player.Missions.ContainsKey(Guns));
            for (var slot = 0; slot < 3; slot++)
                WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
            Accept(harness, munit, Guns);
            Assert.AreEqual((1U, 1U, 1U), (Shipment(harness, 0), Shipment(harness, 1), Shipment(harness, 2)), "the three shipments");
            Assert.IsEmpty(Topics(harness, munit, Guns), "Munit hears no objective");
            CollectionAssert.AreEqual(new[] { CrucibleSmokingGunsV1.Intrepid }, Topics(harness, fiorella, Guns));
            CollectionAssert.AreEqual(new[] { CrucibleSmokingGunsV1.Labbna }, Topics(harness, haggus, Guns));
            Assert.AreEqual((CrucibleSmokingGunsV1.Awol, CrucibleSmokingGunsV1.NeitherVariant), MelchingTopic(harness, melching),
                "Melching: \"Who the hell are you?\" (no Calendar Girl yet)");

            var packets = new List<object>();
            packets.AddRange(Say(harness, haggus, Guns, CrucibleSmokingGunsV1.Labbna, 1));
            Assert.IsFalse(harness.Manager.CompleteOfferedObjective(harness.Client, fiorella.EntityId, Guns, CrucibleSmokingGunsV1.Awol, 1),
                "Fiorella does not take Melching's");
            Assert.AreEqual(0U, Shipment(harness, 1), "Haggus takes the Labbna shipment");

            WildernessRanjaGorgeTests.Reload(harness);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Guns, CrucibleSmokingGunsV1.Labbna), "kept at a reload");
            Assert.AreEqual((1U, 0U, 1U), (Shipment(harness, 0), Shipment(harness, 1), Shipment(harness, 2)), "kept at a reload");
            harness.Drain();

            Assert.IsFalse(harness.Manager.CompleteOfferedObjective(harness.Client, melching.EntityId, Guns, CrucibleSmokingGunsV1.Awol,
                CrucibleSmokingGunsV1.RattedOutVariant), "not the text of a player who ratted Deirdre out");
            packets.AddRange(Say(harness, melching, Guns, CrucibleSmokingGunsV1.Awol, CrucibleSmokingGunsV1.NeitherVariant));
            Assert.IsFalse(harness.Client.Player.Missions[Guns].Completeable, "not before the third");
            packets.AddRange(Say(harness, fiorella, Guns, CrucibleSmokingGunsV1.Intrepid, 1));
            Assert.AreEqual((0U, 0U, 0U), (Shipment(harness, 0), Shipment(harness, 1), Shipment(harness, 2)), "all three delivered");
            CollectionAssert.AreEquivalent(new[] { 1U, 2U, 3U }, packets.OfType<ObjectiveCompletedPacket>()
                .Where(packet => packet.MissionId == Guns).Select(packet => packet.ObjectiveId).ToArray(), "ObjectiveCompleted once each");
            Assert.AreEqual(1, packets.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Guns && packet.IsCompleteable),
                "MissionCompleteable once");

            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, fiorella.EntityId, Guns, 1), "not Fiorella's to take");
            FullBagWaits(harness, munit, Guns, 1, ArmorFiller);
            FailedTurnInPaysNothing(harness, munit, Guns, 1);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, munit, Guns, 1, CrucibleSmokingGunsV1.Experience, CrucibleSmokingGunsV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Guns), "MissionRewarded once");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, CrucibleSmokingGunsV1.GravitonVest), "the vest chosen");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, CrucibleSmokingGunsV1.TopKhanaToken), "the Top Khana token");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, munit, Guns), "done once");
        }

        /// <summary>
        /// Melching's text follows what the player did in Calendar Girl (flag 1144001): 1 ratted out (variant 177), 2 helped
        /// (178), none or another value neither (203). He takes the shipment with that text only.
        /// </summary>
        [TestMethod]
        [DataRow(0U, 203U, DisplayName = "no Calendar Girl: neither")]
        [DataRow(1U, 177U, DisplayName = "ratted out Deirdre")]
        [DataRow(2U, 178U, DisplayName = "helped Deirdre")]
        [DataRow(7U, 203U, DisplayName = "another value: neither")]
        public void MelchingSpeaksAsCalendarGirlEnded(uint flag, uint variant)
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            C.SeedCompleted(harness, LastMen, Fort, Wire, Boss);
            if (flag != 0)
            {
                using (var unit = harness.CreateChar())
                    unit.ExecuteTransaction(() => unit.CharacterFlags.Set(harness.Client.Player.Id, CrucibleSmokingGunsV1.CalendarGirlFlag, flag));
                harness.Client.Player.PlayerFlags[CrucibleSmokingGunsV1.CalendarGirlFlag] = flag;
                WildernessRanjaGorgeTests.Reload(harness);
            }
            var crucible = C.EnterCrucible(harness, C.StaalWaypoint);
            var munit = Npc(crucible, N.Munit);
            var melching = Npc(crucible, G.Melching);
            Accept(harness, munit, Guns);

            Assert.AreEqual((CrucibleSmokingGunsV1.Awol, variant), MelchingTopic(harness, melching), "the one text");
            foreach (var other in new[] { 177U, 178U, 203U }.Where(other => other != variant))
                Assert.IsFalse(harness.Manager.CompleteOfferedObjective(harness.Client, melching.EntityId, Guns, CrucibleSmokingGunsV1.Awol, other),
                    $"not variant {other}");
            Say(harness, melching, Guns, CrucibleSmokingGunsV1.Awol, variant);
            Assert.AreEqual(0U, Shipment(harness, 2), "Melching takes the Awol shipment");
        }

        /// <summary>An abandon takes the shipments; taken again, three new ones.</summary>
        [TestMethod]
        public void SmokingGunsAbandonedTakesTheShipments()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            C.SeedCompleted(harness, LastMen, Fort, Wire, Boss);
            var crucible = C.EnterCrucible(harness, C.StaalWaypoint);
            var munit = Npc(crucible, N.Munit);
            var haggus = Npc(crucible, G.Haggus);
            Accept(harness, munit, Guns);
            Talk(harness, haggus, Guns, CrucibleSmokingGunsV1.Labbna);

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Guns));
            harness.Drain();
            Assert.AreEqual((0U, 0U, 0U), (Shipment(harness, 0), Shipment(harness, 1), Shipment(harness, 2)), "the shipments go");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, munit, Guns), "offered again after an abandon");
            Accept(harness, munit, Guns);
            Assert.AreEqual((1U, 1U, 1U), (Shipment(harness, 0), Shipment(harness, 1), Shipment(harness, 2)), "three new shipments");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Guns, CrucibleSmokingGunsV1.Labbna), "the old delivery does not count");
        }

        /// <summary>
        /// Junkyard Dogs: Rigs offers it after The Boss Of All Bosses; twenty Flare Gashers of the pools by the Staal Junkyard,
        /// the count kept at a reload at ten; the twentieth completes it once; Rigs pays the Labbna token.
        /// </summary>
        [TestMethod]
        public void JunkyardDogsKillsTwentyFlareGashers()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            C.SeedCompleted(harness, LastMen, Fort, Wire);
            var crucible = C.EnterCrucible(harness, C.LabbnaWaypoint);
            var rigs = Npc(crucible, N.Rigs);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, rigs, Dogs), "it waits for The Boss Of All Bosses");
            C.SeedCompleted(harness, Boss);
            crucible = C.EnterCrucible(harness, C.LabbnaWaypoint);

            var offer = WildernessRanjaGorgeTests.Offer(harness, rigs, Dogs);
            CollectionAssert.AreEqual(new[]
                {
                    CrucibleJunkyardDogsV1.HazmatBoots, CrucibleJunkyardDogsV1.MotorAssistBoots, CrucibleJunkyardDogsV1.MechVest,
                    CrucibleJunkyardDogsV1.ReflectiveBoots
                },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            CollectionAssert.AreEqual(new[] { CrucibleJunkyardDogsV1.LabbnaToken },
                offer.FixedReward.FixedItems.Select(item => item.ItemTemplateId).ToArray(), "the Labbna token with every choice");
            Accept(harness, rigs, Dogs);
            Assert.IsEmpty(Topics(harness, rigs, Dogs), "Rigs hears no objective");
            // To-fix rows 23 and 24: on the Crucible the client gets objective 1's three markers, on the moved pools before the
            // bridge, each clear of the Staal Junkyard door (map link 96).
            var markers = harness.Manager.BuildStatusSnapshot(harness.Client.Player)[Dogs].ObjectivesList
                .Single(objective => objective.ObjectiveId == CrucibleJunkyardDogsV1.Kill).IndicatorList;
            CollectionAssert.AreEqual(CrucibleMarkersClearOfLinksV2.GasherMarkers.Select(marker => marker.Indicator).ToArray(),
                markers.Select(marker => marker.IndicatorId).ToArray(), "objective 1's markers");
            foreach (var marker in markers)
            {
                Assert.AreEqual(CrucibleMissionDataV1.MapContextId, marker.MapContextId);
                Assert.IsGreaterThan(CrucibleMarkersClearOfLinksV2.GasherMarkerRadius + 6 + 5,
                    Vector2.Distance(new(marker.Position.X, marker.Position.Z), new(-492.13f, -149.06f)), $"marker {marker.IndicatorId} clear of the door");
            }

            var pools = CrucibleMarkersClearOfLinksV2.Pools.Take(3).Select(pool => pool.Pool).ToArray();
            Spawn(crucible, pools);
            var completed = 0;
            var completeable = 0;
            for (var kill = 1; kill <= CrucibleJunkyardDogsV1.Kills; kill++)
            {
                if (kill == 11)
                {
                    WildernessRanjaGorgeTests.Reload(harness);
                    harness.Drain();
                }
                Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Dogs, CrucibleJunkyardDogsV1.Kill), $"open before kill {kill}");
                if (!Gashers(crucible).Any())
                    Respawn(crucible, pools);
                var gasher = Gashers(crucible).First();
                harness.MoveTo(gasher.Position);
                gasher.Attributes[Attributes.Health].Current = 0;
                harness.Creatures.HandleCreatureKill(crucible, gasher, harness.Client.Player);
                var drained = harness.Drain();
                completed += drained.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Dogs);
                completeable += drained.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Dogs && packet.IsCompleteable);
            }
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Dogs, CrucibleJunkyardDogsV1.Kill), "twenty");
            Assert.AreEqual(1, completed, "ObjectiveCompleted once");
            Assert.AreEqual(1, completeable, "MissionCompleteable once");

            FullBagWaits(harness, rigs, Dogs, 2, ArmorFiller);
            FailedTurnInPaysNothing(harness, rigs, Dogs, 2);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, rigs, Dogs, 2, CrucibleJunkyardDogsV1.Experience, CrucibleJunkyardDogsV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Dogs), "MissionRewarded once");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, CrucibleJunkyardDogsV1.MechVest), "the vest chosen");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, CrucibleJunkyardDogsV1.LabbnaToken), "the Labbna token");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, rigs, Dogs), "done once");
        }

        /// <summary>
        /// The office's two teleporter rows on the test's Crucible channel, as InitTeleporters puts them on a loaded map (the
        /// harness loads the Crucible after InitTeleporters; as MarshesLogosResearchTests does for 907/908), and the links.
        /// </summary>
        private static void OfficePads(WildernessRuntimeTestHarness harness, MapChannel crucible)
        {
            var rows = harness.World.TeleporterEntries.AsNoTracking().Where(row => row.Id == G.LowerPad || row.Id == G.UpperPad).ToList();
            Assert.HasCount(2, rows);
            foreach (var row in rows)
            {
                Assert.AreEqual((CrucibleMissionDataV1.MapContextId, G.PadClass, G.LocalTeleporter), (row.MapContextId, row.ClassId, (uint)row.Type), $"{row.Id}");
                var loaded = new DynamicObject
                {
                    Position = new Vector3((float)row.PosX, (float)row.PosY, (float)row.PosZ),
                    Rotation = row.Rotation,
                    MapContextId = row.MapContextId,
                    RuntimeMapChannel = crucible,
                    EntityClassId = (EntityClasses)row.ClassId,
                    Comment = row.Description,
                    ObjectData = new WaypointInfo(row.Id, false, WaypointType.LocalTeleporter),
                    DynamicObjectType = DynamicObjectType.LocalTeleporter
                };
                crucible.Teleporters.TryAdd(row.Id, loaded);
                harness.Objects.Teleporters.TryAdd(row.Id, loaded);
            }
            harness.Objects.InitLocalTeleporters();
            harness.Objects.DynamicObjectWorker(crucible, 0);
        }

        /// <summary>The living Flare Gashers of the map's pools (not Roscoe).</summary>
        private static IEnumerable<Creature> Gashers(MapChannel crucible) =>
            Living(crucible).Where(creature => creature.DbId == G.Flaregasher && creature.Npc == null);

        /// <summary>The map's creatures, the dead too (MarshesPreoDasTests.Here).</summary>
        private static IEnumerable<Creature> Here(MapChannel crucible) => MarshesPreoDasTests.Here(crucible);

        /// <summary>The player's own Roscoe, living, or null.</summary>
        private static Creature Roscoe(WildernessRuntimeTestHarness harness, MapChannel crucible) =>
            MarshesPreoDasTests.SceneCreature(harness, crucible, Racket, CrucibleProtectionRacketV1.RoscoeRole);

        /// <summary>A talk the NPC hears (with this variant), done; the packets it sent.</summary>
        private static List<object> Say(WildernessRuntimeTestHarness harness, Creature npc, uint mission, uint objective, uint variant)
        {
            harness.MoveTo(npc.Position);
            harness.Drain();
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, npc.EntityId, mission, objective, variant),
                $"{npc.DbId} hears {mission}.{objective} (variant {variant})");
            var packets = harness.Drain().Cast<object>().ToList();
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, mission, objective));
            return packets;
        }

        /// <summary>Melching's one topic of Smoking Guns: objective and variant.</summary>
        private static (uint Objective, uint Variant) MelchingTopic(WildernessRuntimeTestHarness harness, Creature melching)
        {
            Assert.IsTrue(Conversation(harness, melching).TryGetValue(ConversationType.ObjectiveComplete, out var value), "Melching has a topic");
            var topic = ((List<CompleteableObjectives>)value).Single(topic => topic.MissionId == Guns);
            return ((uint)topic.ObjectiveId, (uint)topic.PlayerFlagId);
        }

        /// <summary>Through the pad onto the far one (MarshesLogosResearchTests.Teleport).</summary>
        private static void Teleport(WildernessRuntimeTestHarness harness, MapChannel map, DynamicObject pad)
        {
            harness.MoveTo(pad.Position);
            harness.Drain();
            harness.Objects.DynamicObjectProximityWorker(map, pad, 0);
            Assert.IsNotNull(harness.Client.PendingTransfer, "taken by the pad");
            System.Threading.Thread.Sleep(DynamicObjectManager.PassageHoldMs + 50);
            harness.Objects.PassagesWorker(map);
            harness.Objects.TeleportAcknowledge(harness.Client);
            harness.Drain();
            Assert.IsNull(harness.Client.PendingTransfer);
        }

        private static uint Money(WildernessRuntimeTestHarness harness, int index) =>
            WildernessRanjaGorgeTests.Held(harness, CrucibleProtectionRacketV1.Payments[index].Template);

        private static uint Shipment(WildernessRuntimeTestHarness harness, int index) =>
            WildernessRanjaGorgeTests.Held(harness, CrucibleSmokingGunsV1.Deliveries[index].Template);
    }
}
