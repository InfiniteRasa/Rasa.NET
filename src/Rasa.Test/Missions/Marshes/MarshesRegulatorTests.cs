using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
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
    /// Marshes set 4, the Regulator, played through as a player plays it: Retread McCormick's The Regulator: Part I (925:
    /// scrap metal from Retread Wilson), Wilson's Part II (1028: the nanovirus from Conscript Kaav, the Bane Traitor outside
    /// the Bane Supply Depot), Kaav's Part III (1029: a Strider's power core, to Wilson) and Wilson's Part IV (1030: a viral
    /// scrapper from the Paludos Ruins, to McCormick, who gives the Nanoviral Regulator).
    ///
    /// Why these tests: Wilson, Kaav and the Strider are new on the server, the four parts follow each other, two talks hand
    /// over a mission item, the core drops only from a Strider and the scrapper is the player's own item giver. An NPC off the
    /// navmesh, an offer out of the chain's order, a talk or turn-in at the wrong NPC, an item given with no room or kept
    /// after an abandon, a core from the wrong kill or counted before its pickup, a giver used twice, progress lost at a
    /// reload, or a turn-in that pays twice would each break them. The core's chance is 100%: no roll is made, so the
    /// highest roll still drops it (the only side of that boundary).
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class MarshesRegulatorTests
    {
        private const uint PartOne = MarshesRegulatorPartOneV1.MissionId;
        private const uint PartTwo = MarshesRegulatorPartTwoV1.MissionId;
        private const uint PartThree = MarshesRegulatorPartThreeV1.MissionId;
        private const uint PartFour = MarshesRegulatorPartFourV1.MissionId;
        private const uint McCormick = MarshesRegulatorNpcsV1.McCormick, Wilson = MarshesRegulatorNpcsV1.Wilson;
        private const uint Kaav = MarshesRegulatorNpcsV1.Kaav, StriderPool = MarshesRegulatorNpcsV1.StriderPool;
        private const uint Strider = MarshesRegulatorNpcsV1.Strider, DaddyLongLegs = MarshesRegulatorPartThreeV1.DaddyLongLegs;
        private const uint ThraxSoldierPool = 531300, ThraxSoldier = 531045;   // World DB: a Thrax Soldier pool by the Depot
        private const uint MissionItemFiller = 11519;      // as PlateauCampResistanceTests
        private const uint ConsumableFiller = 45060;       // Class VII Basic Med Pack: fills the consumables bag

        /// <summary>The Retread City waypoint (World DB teleporter 134).</summary>
        private static readonly Vector3 RetreadCityWaypoint = new(-250.45f, 216.8f, -830.56f);
        /// <summary>The Bane Supply Depot door on the Marshes (World DB map_link 41).</summary>
        private static readonly Vector3 DepotDoor = new(451.56f, 223.61f, -581.94f);

        [TestInitialize]
        public void Clock() => PlateauGiantKillerSquadTests.UseClock();

        [TestCleanup]
        public void Restore() => PlateauGiantKillerSquadTests.RestoreClock();

        [TestMethod]
        public void WilsonKaavAndTheStriderStandOnTheNavmeshAndAreWalkedTo()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            foreach (var (pool, name, package, at) in new[]
            {
                (Wilson, MarshesRegulatorNpcsV1.WilsonName, MarshesRegulatorNpcsV1.WilsonPackage, MarshesRegulatorNpcsV1.WilsonAt),
                (Kaav, MarshesRegulatorNpcsV1.KaavName, MarshesRegulatorNpcsV1.KaavPackage, MarshesOnTheClientFloorV1.KaavAt)
            })
            {
                var npc = Npc(marshes, pool);
                Assert.AreEqual(name, npc.NameId);
                Assert.AreEqual(package, npc.Npc.NpcPackageId, $"{pool}: the client's package");
                Assert.AreNotEqual(TargetCategory.Hostile, npc.TargetCategory, $"{pool} is no foe");
                Assert.IsLessThan(0.1f, Vector2.Distance(new((float)at.X, (float)at.Z), new(npc.Position.X, npc.Position.Z)), $"{pool} at his place");
                AssertOnGround(marshes, npc.Position, $"{pool}");
                // To-fix 21: Kaav stood on a lower navmesh layer under the depot's floor, half in a pipe. Nothing walkable over him.
                var over = marshes.NavMesh.GroundHeight(npc.Position + new Vector3(0, 2.5f, 0));
                Assert.IsNotNull(over);
                Assert.IsLessThan(0.5f, over.Value - npc.Position.Y, $"{pool}: no floor over his head");
                Assert.IsTrue(marshes.NavMesh.FindPath(RetreadCityWaypoint, npc.Position, out var reached) != null && reached, $"{pool} is walked to");
            }
            var kaav = RequireNpc(marshes, Kaav);
            Assert.IsLessThan(15f, Vector3.Distance(DepotDoor, kaav.Position), "Kaav outside the Depot door");
            Assert.IsTrue(marshes.NavMesh.FindPath(DepotDoor, kaav.Position, out var fromDoor) != null && fromDoor, "Kaav is walked to from the door");
            Assert.IsLessThan(5f, Vector2.Distance(new(-138, -468), new(RequireNpc(marshes, Wilson).Position.X, RequireNpc(marshes, Wilson).Position.Z)),
                "Wilson by Ten Ton Hammer's point");

            Spawn(marshes, StriderPool);
            var strider = Living(marshes).Single(creature => creature.DbId == Strider);
            Assert.IsNull(strider.Npc, "the Strider does not talk");
            Assert.AreEqual(TargetCategory.Hostile, strider.TargetCategory, "the Strider is a foe");
            AssertOnGround(marshes, strider.Position, "the Strider");
            Assert.IsTrue(marshes.NavMesh.FindPath(RetreadCityWaypoint, strider.Position, out var walked) != null && walked, "the Strider is walked to");
        }

        [TestMethod]
        public void PartOneTakesTheScrapFromWilson()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            var mccormick = Npc(marshes, McCormick);
            var wilson = Npc(marshes, Wilson);

            var offer = WildernessRanjaGorgeTests.Offer(harness, mccormick, PartOne);
            CollectionAssert.AreEqual(new[]
                {
                    (MarshesRegulatorPartOneV1.AdvancedMedPack, MarshesRegulatorPartOneV1.AdvancedMedPacks),
                    (MarshesRegulatorPartOneV1.PowerCharger, MarshesRegulatorPartOneV1.PowerChargers),
                    (MarshesRegulatorPartOneV1.FragGrenade, MarshesRegulatorPartOneV1.FragGrenades),
                    (MarshesRegulatorPartOneV1.CryoGrenade, MarshesRegulatorPartOneV1.CryoGrenades)
                },
                offer.SelectableReward.Select(item => (item.ItemTemplateId, item.Quantity)).ToArray());
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, wilson, PartOne), "not Wilson's to give");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, wilson, PartTwo), "Part II waits for Part I");
            Accept(harness, mccormick, PartOne);

            // McCormick hears nothing of it; Wilson hears the scrap, and only that.
            Assert.IsEmpty(Topics(harness, mccormick, PartOne), "McCormick has no topic of it");
            Assert.IsFalse(harness.Manager.CompleteOfferedObjective(harness.Client, mccormick.EntityId, PartOne, MarshesRegulatorPartOneV1.Scrap, 1),
                "not McCormick's to hear");
            WildernessRanjaGorgeTests.Reload(harness);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, PartOne, MarshesRegulatorPartOneV1.Scrap), "kept at a reload");
            harness.Drain();

            // No room for the scrap: Wilson's talk does not go through, and nothing is given.
            WildernessAliaBranchesTests.FillCategory(harness, MissionItemFiller, 50);
            harness.MoveTo(wilson.Position);
            harness.Drain();
            Assert.IsFalse(harness.Manager.CompleteOfferedObjective(harness.Client, wilson.EntityId, PartOne, MarshesRegulatorPartOneV1.Scrap, 1),
                "no room for the scrap");
            Assert.AreEqual(0U, Scrap(harness));
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, PartOne, MarshesRegulatorPartOneV1.Scrap));
            WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
            harness.Drain();

            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, wilson.EntityId, PartOne, MarshesRegulatorPartOneV1.Scrap, 1));
            var talk = harness.Drain();
            Assert.AreEqual(1, talk.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == PartOne), "ObjectiveCompleted once");
            Assert.AreEqual(1, talk.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == PartOne && packet.IsCompleteable),
                "MissionCompleteable once");
            Assert.AreEqual(MarshesRegulatorPartOneV1.ScrapCount, Scrap(harness), "the scrap in the bag");
            Assert.IsEmpty(Topics(harness, wilson, PartOne), "heard once");
            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, mccormick.EntityId, PartOne, 1), "not McCormick's to take");

            FullBagWaits(harness, wilson, PartOne, 1, ConsumableFiller);
            FailedTurnInPaysNothing(harness, wilson, PartOne, 1);
            Assert.AreEqual(MarshesRegulatorPartOneV1.ScrapCount, Scrap(harness), "kept at the failed turn-in");
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, wilson, PartOne, 1,
                MarshesRegulatorPartOneV1.Experience, MarshesRegulatorPartOneV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == PartOne), "MissionRewarded once");
            Assert.AreEqual(MarshesRegulatorPartOneV1.PowerChargers, WildernessRanjaGorgeTests.Held(harness, MarshesRegulatorPartOneV1.PowerCharger),
                "the selected two Power Chargers");
            Assert.AreEqual(0U, Scrap(harness), "Wilson keeps the scrap");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, mccormick, PartOne), "done once");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, wilson, PartTwo), "Part II follows at Wilson");
        }

        /// <summary>An abandon takes the scrap back; taken again, Wilson hands it over again.</summary>
        [TestMethod]
        public void PartOneAbandonedTakesTheScrapBack()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            var mccormick = Npc(marshes, McCormick);
            var wilson = Npc(marshes, Wilson);
            Accept(harness, mccormick, PartOne);
            Talk(harness, wilson, PartOne, MarshesRegulatorPartOneV1.Scrap);
            Assert.AreEqual(1U, Scrap(harness));

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, PartOne));
            harness.Drain();
            Assert.IsFalse(harness.Client.Player.Missions.ContainsKey(PartOne), "gone from the log");
            Assert.AreEqual(0U, Scrap(harness), "the scrap goes with the mission");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, mccormick, PartOne), "offered again after an abandon");
            Accept(harness, mccormick, PartOne);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, PartOne, MarshesRegulatorPartOneV1.Scrap), "the old talk does not count");
            Talk(harness, wilson, PartOne, MarshesRegulatorPartOneV1.Scrap);
            Assert.AreEqual(1U, Scrap(harness));
        }

        [TestMethod]
        public void PartTwoTakesTheNanovirusFromTheBaneTraitor()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            var wilson = Npc(marshes, Wilson);
            var kaav = Npc(marshes, Kaav);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, wilson, PartTwo), "it waits for Part I");
            SeedCompletedMarshes(harness, PartOne);
            marshes = EnterMarshes(harness);

            var offer = WildernessRanjaGorgeTests.Offer(harness, wilson, PartTwo);
            CollectionAssert.AreEqual(new[]
                {
                    (MarshesRegulatorPartTwoV1.AdvancedMedPack, MarshesRegulatorPartTwoV1.AdvancedMedPacks),
                    (MarshesRegulatorPartTwoV1.ArmorCharger, MarshesRegulatorPartTwoV1.ArmorChargers),
                    (MarshesRegulatorPartTwoV1.ConcussionGrenade, MarshesRegulatorPartTwoV1.ConcussionGrenades),
                    (MarshesRegulatorPartTwoV1.FragGrenade, MarshesRegulatorPartTwoV1.FragGrenades)
                },
                offer.SelectableReward.Select(item => (item.ItemTemplateId, item.Quantity)).ToArray());
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, kaav, PartTwo), "not Kaav's to give");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, kaav, PartThree), "Part III waits for Part II");
            Accept(harness, wilson, PartTwo);

            Assert.IsEmpty(Topics(harness, wilson, PartTwo), "Wilson has no topic of it");
            Assert.IsFalse(harness.Manager.CompleteOfferedObjective(harness.Client, wilson.EntityId, PartTwo, MarshesRegulatorPartTwoV1.Tracker, 1),
                "not Wilson's to hear");
            WildernessRanjaGorgeTests.Reload(harness);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, PartTwo, MarshesRegulatorPartTwoV1.Tracker), "kept at a reload");
            harness.Drain();

            WildernessAliaBranchesTests.FillCategory(harness, MissionItemFiller, 50);
            harness.MoveTo(kaav.Position);
            harness.Drain();
            Assert.IsFalse(harness.Manager.CompleteOfferedObjective(harness.Client, kaav.EntityId, PartTwo, MarshesRegulatorPartTwoV1.Tracker, 1),
                "no room for the nanovirus");
            Assert.AreEqual(0U, Nanovirus(harness));
            WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
            harness.Drain();

            AssertHandInTopic(Conversation(harness, kaav), PartTwo, MarshesRegulatorPartTwoV1.Tracker);
            harness.MoveTo(kaav.Position);
            harness.Drain();
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, kaav.EntityId, PartTwo, MarshesRegulatorPartTwoV1.Tracker, 1));
            var talk = harness.Drain();
            Assert.AreEqual(1, talk.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == PartTwo), "ObjectiveCompleted once");
            Assert.AreEqual(1, talk.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == PartTwo && packet.IsCompleteable),
                "MissionCompleteable once");
            Assert.AreEqual(1U, Nanovirus(harness), "the nanovirus in the bag");
            Assert.IsEmpty(Topics(harness, kaav, PartTwo), "heard once");
            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, wilson.EntityId, PartTwo, 2), "not Wilson's to take");

            FullBagWaits(harness, kaav, PartTwo, 2, ConsumableFiller);
            FailedTurnInPaysNothing(harness, kaav, PartTwo, 2);
            Assert.AreEqual(1U, Nanovirus(harness), "kept at the failed turn-in");
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, kaav, PartTwo, 2,
                MarshesRegulatorPartTwoV1.Experience, MarshesRegulatorPartTwoV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == PartTwo), "MissionRewarded once");
            Assert.AreEqual(MarshesRegulatorPartTwoV1.ConcussionGrenades,
                WildernessRanjaGorgeTests.Held(harness, MarshesRegulatorPartTwoV1.ConcussionGrenade), "the selected four Concussion Grenades");
            Assert.AreEqual(0U, Nanovirus(harness), "the Traitor takes it back for Wilson");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, wilson, PartTwo), "done once");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, kaav, PartThree), "Part III follows at Kaav");
        }

        /// <summary>An abandon takes the nanovirus back; the mission is offered again.</summary>
        [TestMethod]
        public void PartTwoAbandonedTakesTheNanovirusBack()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            SeedCompletedMarshes(harness, PartOne);
            var marshes = EnterMarshes(harness);
            var wilson = Npc(marshes, Wilson);
            var kaav = Npc(marshes, Kaav);
            Accept(harness, wilson, PartTwo);
            Talk(harness, kaav, PartTwo, MarshesRegulatorPartTwoV1.Tracker);
            Assert.AreEqual(1U, Nanovirus(harness));

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, PartTwo));
            harness.Drain();
            Assert.AreEqual(0U, Nanovirus(harness), "the nanovirus goes with the mission");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, wilson, PartTwo), "offered again after an abandon");
            Accept(harness, wilson, PartTwo);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, PartTwo, MarshesRegulatorPartTwoV1.Tracker), "the old talk does not count");
        }

        [TestMethod]
        public void PartThreeTakesAStridersPowerCoreToWilson()
        {
            var percentile = 0;
            using var harness = WildernessRuntimeTestHarness.Create(
                lootRoll: (minimum, maximum) => maximum == 100 ? percentile : minimum);
            var marshes = EnterMarshes(harness);
            var kaav = Npc(marshes, Kaav);
            var wilson = Npc(marshes, Wilson);
            Spawn(marshes, StriderPool, ThraxSoldierPool);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, kaav, PartThree), "it waits for Part II");
            Assert.IsFalse(Holds(Kill(harness, marshes, Strider), MarshesRegulatorPartThreeV1.CoreTemplate), "no core before the mission");
            SeedCompletedMarshes(harness, PartOne, PartTwo);
            marshes = EnterMarshes(harness);

            var offer = WildernessRanjaGorgeTests.Offer(harness, kaav, PartThree);
            CollectionAssert.AreEqual(new[]
                {
                    MarshesRewardBlocksV1.Current(MarshesRegulatorPartThreeV1.MissionId, MarshesRegulatorPartThreeV1.IncendiaryInjectorGun), MarshesRewardBlocksV1.Current(MarshesRegulatorPartThreeV1.MissionId, MarshesRegulatorPartThreeV1.ElectricChaingun),
                    MarshesRewardBlocksV1.Current(MarshesRegulatorPartThreeV1.MissionId, MarshesRegulatorPartThreeV1.PulsePistol), MarshesRewardBlocksV1.Current(MarshesRegulatorPartThreeV1.MissionId, MarshesRegulatorPartThreeV1.PulseCannon)
                },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, wilson, PartThree), "not Wilson's to give");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, wilson, PartFour), "Part IV waits for Part III");
            Accept(harness, kaav, PartThree);
            Assert.IsEmpty(Topics(harness, kaav, PartThree), "Kaav hears no objective: the core counts as it is picked up");
            Assert.IsEmpty(Topics(harness, wilson, PartThree), "Wilson hears no objective");

            // A Thrax has no core, whatever the roll; a Strider has one at the highest roll (100%: no roll is made).
            percentile = 0;
            Assert.IsFalse(Holds(Kill(harness, marshes, ThraxSoldier), MarshesRegulatorPartThreeV1.CoreTemplate), "a Thrax has no core");
            Respawn(marshes, StriderPool);
            percentile = 99;
            var loot = Kill(harness, marshes, Strider);
            Assert.IsTrue(Holds(loot, MarshesRegulatorPartThreeV1.CoreTemplate), "the Strider's core");
            Assert.AreEqual(0U, Core(harness), "a death is not a pickup");
            WildernessRanjaGorgeTests.Reload(harness);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, PartThree, MarshesRegulatorPartThreeV1.Core), "kept at a reload");
            harness.Drain();

            // No room: the core stays on the corpse. One slot freed, it is picked up.
            WildernessAliaBranchesTests.FillCategory(harness, MissionItemFiller, 50);
            PickUp(harness, loot);
            Assert.AreEqual(0U, Core(harness), "no room");
            Assert.IsTrue(Holds(loot, MarshesRegulatorPartThreeV1.CoreTemplate), "still on the corpse");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, PartThree, MarshesRegulatorPartThreeV1.Core));
            WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
            harness.Drain();
            PickUp(harness, loot);
            Assert.AreEqual(1U, Core(harness));
            var picked = harness.Drain();
            Assert.AreEqual(1, picked.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == PartThree), "ObjectiveCompleted once");
            Assert.AreEqual(1, picked.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == PartThree && packet.IsCompleteable),
                "MissionCompleteable once");
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, PartThree, MarshesRegulatorPartThreeV1.Core));

            // One core is enough: the next Strider has none.
            Respawn(marshes, StriderPool);
            Assert.IsFalse(Holds(Kill(harness, marshes, Strider), MarshesRegulatorPartThreeV1.CoreTemplate), "one core held, no second");

            harness.MoveTo(kaav.Position);
            harness.Drain();
            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, kaav.EntityId, PartThree, 3), "not Kaav's to take");
            FullBagWaits(harness, wilson, PartThree, 3, MarshesRewardBlocksV1.Current(MarshesRegulatorPartThreeV1.MissionId, MarshesRegulatorPartThreeV1.ElectricChaingun));
            FailedTurnInPaysNothing(harness, wilson, PartThree, 3);
            Assert.AreEqual(1U, Core(harness), "kept at the failed turn-in");
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, wilson, PartThree, 3,
                MarshesRegulatorPartThreeV1.Experience, MarshesRegulatorPartThreeV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == PartThree), "MissionRewarded once");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, MarshesRewardBlocksV1.Current(MarshesRegulatorPartThreeV1.MissionId, MarshesRegulatorPartThreeV1.PulseCannon)), "the selected Pulse Cannon");
            Assert.AreEqual(0U, Core(harness), "Wilson keeps the core");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, kaav, PartThree), "done once");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, wilson, PartFour), "Part IV follows at Wilson");
        }

        /// <summary>
        /// Daddy Long-Legs, the Marshes' Strider boss, has a core too; an abandon keeps the picked-up core (a drop is the
        /// player's own), and taken again the mission counts the core already held.
        /// </summary>
        [TestMethod]
        public void TheStriderBossHasACoreAndAnAbandonKeepsIt()
        {
            using var harness = WildernessRuntimeTestHarness.Create(
                lootRoll: (minimum, maximum) => maximum == 100 ? 99 : minimum);
            SeedCompletedMarshes(harness, PartOne, PartTwo);
            var marshes = EnterMarshes(harness);
            var kaav = Npc(marshes, Kaav);
            Spawn(marshes, DaddyLongLegs);
            Accept(harness, kaav, PartThree);

            var loot = Kill(harness, marshes, DaddyLongLegs);
            Assert.IsTrue(Holds(loot, MarshesRegulatorPartThreeV1.CoreTemplate), "Daddy Long-Legs's core");
            PickUp(harness, loot);
            Assert.AreEqual(1U, Core(harness));
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, PartThree, MarshesRegulatorPartThreeV1.Core));

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, PartThree));
            harness.Drain();
            Assert.IsFalse(harness.Client.Player.Missions.ContainsKey(PartThree), "gone from the log");
            Assert.AreEqual(1U, Core(harness), "a picked-up core stays the player's (CharacterOwned, Retain)");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, kaav, PartThree), "offered again after an abandon");
            Accept(harness, kaav, PartThree);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, PartThree, MarshesRegulatorPartThreeV1.Core),
                "the core held counts at the accept");
        }

        [TestMethod]
        public void PartFourTakesTheViralScrapperToMcCormick()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            var wilson = Npc(marshes, Wilson);
            var mccormick = Npc(marshes, McCormick);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, wilson, PartFour), "it waits for Part III");
            SeedCompletedMarshes(harness, PartOne, PartTwo, PartThree);
            marshes = EnterMarshes(harness);
            Assert.IsFalse(marshes.DynamicObjects.Any(obj => obj.SceneMissionId == PartFour), "no scrapper before the mission");

            var offer = WildernessRanjaGorgeTests.Offer(harness, wilson, PartFour);
            CollectionAssert.AreEqual(new[]
                {
                    MarshesRegulatorPartFourV1.BioBoots, MarshesRegulatorPartFourV1.GravitonBoots,
                    MarshesRegulatorPartFourV1.MechHelmet, MarshesRegulatorPartFourV1.StealthVest
                },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            CollectionAssert.AreEqual(new[] { MarshesRegulatorPartFourV1.Regulator },
                offer.FixedReward.FixedItems.Select(item => item.ItemTemplateId).ToArray(), "the Regulator with every choice");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, mccormick, PartFour), "not McCormick's to give");
            Accept(harness, wilson, PartFour);
            Assert.IsEmpty(Topics(harness, wilson, PartFour), "Wilson hears no objective");
            Assert.IsEmpty(Topics(harness, mccormick, PartFour), "McCormick hears no objective: the scrapper counts as it is taken");

            var scrapper = Scrapper(harness, marshes);
            WildernessRanjaGorgeTests.Reload(harness);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, PartFour, MarshesRegulatorPartFourV1.Locate), "kept at a reload");
            harness.Drain();
            scrapper = Scrapper(harness, marshes);

            // No room: the giver waits. One slot freed, it gives the scrapper.
            WildernessAliaBranchesTests.FillCategory(harness, MissionItemFiller, 50);
            Take(harness, marshes, scrapper, recovers: true);
            Assert.AreEqual(0U, Scrappers(harness), "no room");
            Assert.IsTrue(scrapper.IsEnabled, "the giver waits");
            Assert.AreEqual((UseObjectState)MarshesRegulatorPartFourV1.ReadyState, scrapper.StateId);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, PartFour, MarshesRegulatorPartFourV1.Locate));
            WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
            harness.Drain();

            Take(harness, marshes, scrapper, recovers: true);
            var taken = harness.Drain();
            Assert.AreEqual(1U, Scrappers(harness), "the scrapper in the bag");
            Assert.AreEqual((UseObjectState)MarshesRegulatorPartFourV1.TakenState, scrapper.StateId, "taken");
            Assert.IsFalse(scrapper.IsEnabled, "closed");
            Assert.AreEqual(1, taken.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == PartFour), "ObjectiveCompleted once");
            Assert.AreEqual(1, taken.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == PartFour && packet.IsCompleteable),
                "MissionCompleteable once");
            Take(harness, marshes, scrapper, recovers: false);
            Assert.AreEqual(1U, Scrappers(harness), "a taken giver gives no second");

            harness.MoveTo(wilson.Position);
            harness.Drain();
            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, wilson.EntityId, PartFour, 0), "not Wilson's to take");
            FullBagWaits(harness, mccormick, PartFour, 0, MarshesRegulatorPartFourV1.GravitonBoots);
            FailedTurnInPaysNothing(harness, mccormick, PartFour, 0);
            Assert.AreEqual(1U, Scrappers(harness), "kept at the failed turn-in");
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, mccormick, PartFour, 0,
                MarshesRegulatorPartFourV1.Experience, MarshesRegulatorPartFourV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == PartFour), "MissionRewarded once");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, MarshesRegulatorPartFourV1.BioBoots), "the selected boots");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, MarshesRegulatorPartFourV1.Regulator), "and the Nanoviral Regulator");
            Assert.AreEqual(0U, Scrappers(harness), "McCormick keeps the scrapper");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, wilson, PartFour), "done once");
        }

        /// <summary>An abandon takes the scrapper and the giver away; taken again, a new giver stands ready.</summary>
        [TestMethod]
        public void PartFourAbandonedTakesTheScrapperBack()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            SeedCompletedMarshes(harness, PartOne, PartTwo, PartThree);
            var marshes = EnterMarshes(harness);
            var wilson = Npc(marshes, Wilson);
            Accept(harness, wilson, PartFour);
            var scrapper = Scrapper(harness, marshes);
            Take(harness, marshes, scrapper, recovers: true);
            Assert.AreEqual(1U, Scrappers(harness));

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, PartFour));
            harness.Drain();
            Assert.AreEqual(0U, Scrappers(harness), "the scrapper goes with the mission");
            Assert.IsFalse(marshes.DynamicObjects.Any(obj => obj.SceneMissionId == PartFour && obj.IsInWorld), "the giver goes with it");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, wilson, PartFour), "offered again after an abandon");
            Accept(harness, wilson, PartFour);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, PartFour, MarshesRegulatorPartFourV1.Locate));
            Assert.IsTrue(Scrapper(harness, marshes).IsEnabled, "a new giver, ready");
        }

        /// <summary>The scrapper's giver: the player's own, of its class, at TaRapedia's /loc, on the navmesh, walked to; ready.</summary>
        private static DynamicObject Scrapper(WildernessRuntimeTestHarness harness, MapChannel marshes)
        {
            var giver = marshes.DynamicObjects.Single(obj => obj.SceneMissionId == PartFour &&
                obj.SceneActorRole == MarshesRegulatorPartFourV1.GiverRole && obj.IsInWorld);
            var at = MarshesRegulatorPartFourV1.GiverAt;
            Assert.AreEqual(MarshesRegulatorPartFourV1.GiverClass, (uint)giver.EntityClassId);
            Assert.AreEqual(new Vector3(at.X, at.Y, at.Z), giver.Position);
            Assert.AreEqual(harness.Client.Player.Id, giver.SceneOwnerCharacterId, "the player's own");
            Assert.AreEqual((UseObjectState)MarshesRegulatorPartFourV1.ReadyState, giver.StateId);
            Assert.IsTrue(giver.IsEnabled);
            AssertOnGround(marshes, giver.Position, "the scrapper");
            Assert.IsTrue(marshes.NavMesh.FindPath(RetreadCityWaypoint, giver.Position, out var complete) != null && complete, "the scrapper is walked to");
            return giver;
        }

        private static void Take(WildernessRuntimeTestHarness harness, MapChannel marshes, DynamicObject giver, bool recovers)
        {
            harness.MoveTo(giver.Position + new Vector3(0, 0, 1));
            harness.Drain();
            Use(harness, marshes, giver, MarshesRegulatorPartFourV1.UseArgument, recovers);
        }

        private static uint Scrap(WildernessRuntimeTestHarness harness) =>
            WildernessRanjaGorgeTests.Held(harness, MarshesRegulatorPartOneV1.ScrapTemplate);

        private static uint Nanovirus(WildernessRuntimeTestHarness harness) =>
            WildernessRanjaGorgeTests.Held(harness, MarshesRegulatorPartTwoV1.NanovirusTemplate);

        private static uint Core(WildernessRuntimeTestHarness harness) =>
            WildernessRanjaGorgeTests.Held(harness, MarshesRegulatorPartThreeV1.CoreTemplate);

        private static uint Scrappers(WildernessRuntimeTestHarness harness) =>
            WildernessRanjaGorgeTests.Held(harness, MarshesRegulatorPartFourV1.ScrapperTemplate);
    }
}
