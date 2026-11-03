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
    /// Marshes set 6, the sentry tower's second part, played through as a player plays it: Sergeant McDonald's The Core of the
    /// Matter (1878: two Bane power cores from the crates before the Bane Supply Depot) and Data Disaster (1870: the Bane
    /// CommLink terminal by Nelg's post), Lt. Kiang's This Calls For Destruction (1879: a virus into each of three Bane
    /// computers, then the main generator blown) and Archer Thoung's Need a Little Help Here (1873: four marsh sickness boosts
    /// from crates at Nelg's post, one injected into each of four soldiers at the tower's foot).
    ///
    /// Why these tests: Thoung and the four soldiers are new on the server, 1878 waits for Under the Radar and 1870 and 1879
    /// for the "Part II done" flag of either version, the crates, consoles, generator, charge and terminal are the player's
    /// own scene objects, 1878 and 1873 count on hidden tallies, 1879 gives its viruses at the accept and opens 2 only after 1,
    /// and 1873 opens its four talks only after the packs. An NPC or object off the navmesh, an offer before its requirement,
    /// a talk or turn-in at the wrong NPC, an object used with another argument than the client's or used twice, an item given
    /// with no room or kept after an abandon, the generator blown before the uploads, a talk that takes no pack, progress lost
    /// at a reload, or a turn-in that pays twice would each break them. No mission of the set has a drop, so no drop chance
    /// is tested.
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class MarshesSentryTowerTwoTests
    {
        private const uint Core = MarshesCoreOfTheMatterV3.MissionId;
        private const uint Destruction = MarshesThisCallsForDestructionV1.MissionId;
        private const uint Disaster = MarshesDataDisasterV1.MissionId;
        private const uint Help = MarshesNeedALittleHelpHereV1.MissionId;
        private const uint Radar = MarshesUnderTheRadarV1.MissionId;
        private const uint McDonald = MarshesSentryTowerNpcsV1.McDonald, Kiang = MarshesSentryTowerNpcsV1.Kiang;
        private const uint Thoung = MarshesSentryTowerTwoNpcsV1.Thoung;
        private const uint PartTwoDone = MarshesUnderTheRadarPartTwoV1.PartTwoDoneFlag;
        private const uint MissionItemFiller = 11519;      // as PlateauCampResistanceTests

        /// <summary>The Research Point waypoint (World DB teleporter 295), as MarshesSentryTowerTests.</summary>
        private static readonly Vector3 ResearchPointWaypoint = new(-343.45f, 216.15f, -279.6f);
        /// <summary>The Bane Assertion Camp waypoint, as MarshesSentryTowerTests.</summary>
        private static readonly Vector3 BaneAssertionCampWaypoint = new(130.77f, 218.3f, -312.8f);

        [TestInitialize]
        public void Clock() => PlateauGiantKillerSquadTests.UseClock();

        [TestCleanup]
        public void Restore() => PlateauGiantKillerSquadTests.RestoreClock();

        [TestMethod]
        public void ThoungAndTheFourSoldiersStandAtTheirPlaces()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            foreach (var (pool, name, package, at) in new[]
            {
                (Thoung, MarshesSentryTowerTwoNpcsV1.ThoungName, MarshesSentryTowerTwoNpcsV1.ThoungPackage, MarshesSentryTowerTwoNpcsV1.ThoungAt),
                (MarshesSentryTowerTwoNpcsV1.Brady, MarshesSentryTowerTwoNpcsV1.BradyName, 2375U, MarshesSentryTowerTwoNpcsV1.BradyAt),
                (MarshesSentryTowerTwoNpcsV1.Zui, MarshesSentryTowerTwoNpcsV1.ZuiName, 2376U, MarshesSentryTowerTwoNpcsV1.ZuiAt),
                (MarshesSentryTowerTwoNpcsV1.Dahn, MarshesSentryTowerTwoNpcsV1.DahnName, 2377U, MarshesSentryTowerTwoNpcsV1.DahnAt),
                (MarshesSentryTowerTwoNpcsV1.Nez, MarshesSentryTowerTwoNpcsV1.NezName, 2378U, MarshesSentryTowerTwoNpcsV1.NezAt)
            })
            {
                var npc = Npc(marshes, pool);
                Assert.AreEqual(name, npc.NameId);
                Assert.AreEqual(package, npc.Npc.NpcPackageId, $"{pool}: the client's package (Thoung: her server-only one)");
                Assert.AreNotEqual(TargetCategory.Hostile, npc.TargetCategory, $"{pool} is no foe");
                Assert.IsLessThan(0.1f, Vector2.Distance(new((float)at.X, (float)at.Z), new(npc.Position.X, npc.Position.Z)), $"{pool} at the place");
                AssertOnGround(marshes, npc.Position, $"{pool}");
                Assert.IsTrue(marshes.NavMesh.FindPath(ResearchPointWaypoint, npc.Position, out var reached) != null && reached, $"{pool} is walked to");
            }
            var thoung = RequireNpc(marshes, Thoung);
            Assert.IsLessThan(5f, Vector2.Distance(new(-127, -341), new(thoung.Position.X, thoung.Position.Z)), "Thoung by Ten Ton Hammer's point");
            foreach (var (_, _, _, _, npc, _) in MarshesNeedALittleHelpHereV1.Soldiers)
                Assert.IsLessThan(20f, Vector2.Distance(new(-117.6f, -323.9f), new(RequireNpc(marshes, npc).Position.X, RequireNpc(marshes, npc).Position.Z)),
                    $"{npc} at the tower's foot");
        }

        [TestMethod]
        public void TheCoreOfTheMatterTakesTwoCoresFromTheCrates()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            var mcdonald = Npc(marshes, McDonald);
            var kiang = Npc(marshes, Kiang);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, mcdonald, Core), "it waits for Under the Radar");
            SeedCompletedMarshes(harness, Radar);
            marshes = EnterMarshes(harness);
            Assert.IsFalse(marshes.DynamicObjects.Any(obj => obj.SceneMissionId == Core), "no crate before the mission");

            var offer = WildernessRanjaGorgeTests.Offer(harness, mcdonald, Core);
            CollectionAssert.AreEqual(new[]
                {
                    MarshesCoreOfTheMatterV3.BioGloves, MarshesCoreOfTheMatterV3.GravitonHelmet,
                    MarshesCoreOfTheMatterV3.MechGloves, MarshesCoreOfTheMatterV3.StealthGloves
                },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, kiang, Core), "not Kiang's to give");
            Accept(harness, mcdonald, Core);
            Assert.IsEmpty(Topics(harness, mcdonald, Core), "McDonald hears no objective: the cores count as they are taken");
            // To-fix 14: 1's marker reaches the client on the Marshes, round all three crates.
            var marker = harness.Manager.BuildStatusSnapshot(harness.Client.Player)[Core].ObjectivesList
                .Single(objective => objective.ObjectiveId == MarshesCoreOfTheMatterV3.Gather).IndicatorList.Single();
            foreach (var crate in MarshesCoreOfTheMatterV3.Crates)
                Assert.IsLessThan((float)MarshesCoreOfTheMatterV3.BaseRadius, System.Numerics.Vector2.Distance(new(marker.Position.X, marker.Position.Z), new(crate.X, crate.Z)), "a crate in the marker");

            var crates = CoreCrates(harness, marshes);
            WildernessRanjaGorgeTests.Reload(harness);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Core, MarshesCoreOfTheMatterV3.Gather), "kept at a reload");
            harness.Drain();
            crates = CoreCrates(harness, marshes);

            // Another argument than the client's is refused. No room: the crate waits. One slot freed, it gives a core.
            Near(harness, crates[0]);
            Use(harness, marshes, crates[0], 1, recovers: false);
            Assert.AreEqual(0U, Cores(harness), "argument 1 is not the client's");
            WildernessAliaBranchesTests.FillCategory(harness, MissionItemFiller, 50);
            Take(harness, marshes, crates[0], MarshesCoreOfTheMatterV3.UseArgument);
            Assert.AreEqual(0U, Cores(harness), "no room");
            Assert.IsTrue(crates[0].IsEnabled, "the crate waits");
            WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
            harness.Drain();

            Take(harness, marshes, crates[0], MarshesCoreOfTheMatterV3.UseArgument);
            harness.Drain();
            Assert.AreEqual(1U, Cores(harness), "a core");
            Assert.AreEqual((UseObjectState)MarshesCoreOfTheMatterV3.TakenState, crates[0].StateId, "opened");
            Assert.IsFalse(crates[0].IsEnabled, "closed");
            TryUse(harness, marshes, crates[0], MarshesCoreOfTheMatterV3.UseArgument);
            Assert.AreEqual(1U, Cores(harness), "an opened crate gives no second");
            WildernessRanjaGorgeTests.Reload(harness);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Core, MarshesCoreOfTheMatterV3.Gather), "one of two");
            Assert.AreEqual(1U, Cores(harness), "kept at a reload");
            harness.Drain();
            crates = CoreCrates(harness, marshes, opened: 1);

            Take(harness, marshes, crates[2], MarshesCoreOfTheMatterV3.UseArgument);
            var second = harness.Drain();
            Assert.AreEqual(2U, Cores(harness));
            Assert.AreEqual(1, second.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Core &&
                packet.ObjectiveId == MarshesCoreOfTheMatterV3.Gather), "ObjectiveCompleted once");
            Assert.AreEqual(1, second.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Core && packet.IsCompleteable),
                "MissionCompleteable once");
            TryUse(harness, marshes, crates[1], MarshesCoreOfTheMatterV3.UseArgument);
            Assert.AreEqual(2U, Cores(harness), "the third crate gives none: the objective is done");

            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, kiang.EntityId, Core, 1), "not Kiang's to take");
            FullBagWaits(harness, mcdonald, Core, 1, MarshesCoreOfTheMatterV3.BioGloves);
            FailedTurnInPaysNothing(harness, mcdonald, Core, 1);
            Assert.AreEqual(2U, Cores(harness), "kept at the failed turn-in");
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, mcdonald, Core, 1,
                MarshesCoreOfTheMatterV3.Experience, MarshesCoreOfTheMatterV3.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Core), "MissionRewarded once");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, MarshesCoreOfTheMatterV3.GravitonHelmet), "the selected helmet");
            Assert.AreEqual(0U, Cores(harness), "McDonald takes the cores");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, mcdonald, Core), "done once");
        }

        /// <summary>An abandon takes the cores and the crates away; taken again, three new crates stand ready.</summary>
        [TestMethod]
        public void TheCoreOfTheMatterAbandonedTakesTheCoresBack()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            SeedCompletedMarshes(harness, Radar);
            var marshes = EnterMarshes(harness);
            var mcdonald = Npc(marshes, McDonald);
            Accept(harness, mcdonald, Core);
            Take(harness, marshes, CoreCrates(harness, marshes)[0], MarshesCoreOfTheMatterV3.UseArgument);
            harness.Drain();
            Assert.AreEqual(1U, Cores(harness));

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Core));
            harness.Drain();
            Assert.AreEqual(0U, Cores(harness), "the core goes with the mission");
            Assert.IsFalse(marshes.DynamicObjects.Any(obj => obj.SceneMissionId == Core && obj.IsInWorld), "the crates go with it");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, mcdonald, Core), "offered again after an abandon");
            Accept(harness, mcdonald, Core);
            Assert.IsTrue(CoreCrates(harness, marshes).All(crate => crate.IsEnabled), "three new crates, ready");
        }

        [TestMethod]
        public void ThisCallsForDestructionUploadsThreeVirusesThenBlowsTheGenerator()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            var kiang = Npc(marshes, Kiang);
            var mcdonald = Npc(marshes, McDonald);
            SeedCompletedMarshes(harness, Radar);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, kiang, Destruction), "it waits for Part II, not Under the Radar");
            PartTwoIsDone(harness);
            marshes = EnterMarshes(harness);

            var offer = WildernessRanjaGorgeTests.Offer(harness, kiang, Destruction);
            CollectionAssert.AreEqual(new[]
                {
                    MarshesThisCallsForDestructionV1.StealthVest, MarshesThisCallsForDestructionV1.BioVest,
                    MarshesThisCallsForDestructionV1.MechLegs, MarshesThisCallsForDestructionV1.GravitonVest
                },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, mcdonald, Destruction), "not McDonald's to give");

            // No room for the viruses: no mission. One slot freed (three in a stack of five), they come with it.
            WildernessAliaBranchesTests.FillCategory(harness, MissionItemFiller, 50);
            harness.MoveTo(kiang.Position);
            harness.Drain();
            Assert.IsFalse(harness.Manager.AcceptOfferedMission(harness.Client, kiang.EntityId, Destruction), "no room for the viruses");
            Assert.IsFalse(harness.Client.Player.Missions.ContainsKey(Destruction));
            Assert.AreEqual(0U, Viruses(harness));
            WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
            Accept(harness, kiang, Destruction);
            Assert.AreEqual(MarshesThisCallsForDestructionV1.Viruses, Viruses(harness), "three viruses");
            Assert.IsEmpty(Topics(harness, kiang, Destruction), "Kiang hears no objective");
            Assert.AreEqual(MissionObjectiveState.Inactive, Objective(harness, Destruction, MarshesThisCallsForDestructionV1.Destroy), "2 waits");

            var consoles = Consoles(harness, marshes);
            var (generator, charge) = Generator(harness, marshes);

            // The charge does nothing before the uploads.
            Near(harness, charge);
            TryUse(harness, marshes, charge, MarshesThisCallsForDestructionV1.UseArgument);
            Assert.AreEqual((UseObjectState)MarshesThisCallsForDestructionV1.IntactState, generator.StateId, "not blown before the uploads");

            // Another argument than the client's is refused; one console takes one virus, once.
            Near(harness, consoles[0]);
            Use(harness, marshes, consoles[0], 3, recovers: false);
            Assert.AreEqual(3U, Viruses(harness), "argument 3 is not the client's");
            Take(harness, marshes, consoles[0], MarshesThisCallsForDestructionV1.UseArgument);
            harness.Drain();
            Assert.AreEqual(2U, Viruses(harness), "a virus uploaded");
            Assert.AreEqual(1U, Uploads(harness));
            Assert.AreEqual((UseObjectState)MarshesThisCallsForDestructionV1.ConsoleSetState, consoles[0].StateId);
            Assert.IsFalse(consoles[0].IsEnabled, "done");
            TryUse(harness, marshes, consoles[0], MarshesThisCallsForDestructionV1.UseArgument);
            Assert.AreEqual(2U, Viruses(harness), "a used console takes no second");
            WildernessRanjaGorgeTests.Reload(harness);
            Assert.AreEqual(1U, Uploads(harness), "kept at a reload");
            Assert.AreEqual(2U, Viruses(harness));
            harness.Drain();
            consoles = Consoles(harness, marshes, used: 1);

            Take(harness, marshes, consoles[1], MarshesThisCallsForDestructionV1.UseArgument);
            harness.Drain();
            Take(harness, marshes, consoles[2], MarshesThisCallsForDestructionV1.UseArgument);
            var uploaded = harness.Drain();
            Assert.AreEqual(0U, Viruses(harness));
            Assert.AreEqual(1, uploaded.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Destruction &&
                packet.ObjectiveId == MarshesThisCallsForDestructionV1.Upload), "ObjectiveCompleted once");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Destruction, MarshesThisCallsForDestructionV1.Destroy), "2 opens");
            Assert.IsFalse(harness.Client.Player.Missions[Destruction].Completeable);

            // The charge: another argument refused; used, it blows the generator.
            (generator, charge) = Generator(harness, marshes);
            Near(harness, charge);
            Use(harness, marshes, charge, 3, recovers: false);
            Assert.AreEqual((UseObjectState)MarshesThisCallsForDestructionV1.IntactState, generator.StateId, "argument 3 is not the client's");
            Take(harness, marshes, charge, MarshesThisCallsForDestructionV1.UseArgument);
            var blown = harness.Drain();
            Assert.AreEqual((UseObjectState)MarshesThisCallsForDestructionV1.DestroyedState, generator.StateId, "blown");
            Assert.IsFalse(charge.IsEnabled, "the charge is set");
            Assert.AreEqual(1, blown.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Destruction &&
                packet.ObjectiveId == MarshesThisCallsForDestructionV1.Destroy), "ObjectiveCompleted once");
            Assert.AreEqual(1, blown.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Destruction && packet.IsCompleteable),
                "MissionCompleteable once");

            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, mcdonald.EntityId, Destruction, 2), "not McDonald's to take");
            FullBagWaits(harness, kiang, Destruction, 2, MarshesThisCallsForDestructionV1.StealthVest);
            FailedTurnInPaysNothing(harness, kiang, Destruction, 2);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, kiang, Destruction, 2,
                MarshesThisCallsForDestructionV1.Experience, MarshesThisCallsForDestructionV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Destruction), "MissionRewarded once");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, MarshesThisCallsForDestructionV1.MechLegs), "the selected legs");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, kiang, Destruction), "done once");
        }

        /// <summary>An abandon takes the viruses left and the objects away; taken again, three new viruses and consoles.</summary>
        [TestMethod]
        public void ThisCallsForDestructionAbandonedTakesTheVirusesBack()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            PartTwoIsDone(harness);
            var marshes = EnterMarshes(harness);
            var kiang = Npc(marshes, Kiang);
            Accept(harness, kiang, Destruction);
            Take(harness, marshes, Consoles(harness, marshes)[0], MarshesThisCallsForDestructionV1.UseArgument);
            harness.Drain();
            Assert.AreEqual(2U, Viruses(harness));

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Destruction));
            harness.Drain();
            Assert.AreEqual(0U, Viruses(harness), "the viruses go with the mission");
            Assert.IsFalse(marshes.DynamicObjects.Any(obj => obj.SceneMissionId == Destruction && obj.IsInWorld), "the objects go with it");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, kiang, Destruction), "offered again after an abandon");
            Accept(harness, kiang, Destruction);
            Assert.AreEqual(MarshesThisCallsForDestructionV1.Viruses, Viruses(harness), "three new viruses");
            Assert.IsTrue(Consoles(harness, marshes).All(console => console.IsEnabled), "three new consoles, ready");
            Assert.AreEqual(0U, Uploads(harness), "the count starts at nought");
        }

        [TestMethod]
        public void DataDisasterReadsTheCommLinkTerminal()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            var mcdonald = Npc(marshes, McDonald);
            var kiang = Npc(marshes, Kiang);
            SeedCompletedMarshes(harness, Radar);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, mcdonald, Disaster), "it waits for Part II");
            PartTwoIsDone(harness);
            marshes = EnterMarshes(harness);
            Assert.IsFalse(marshes.DynamicObjects.Any(obj => obj.SceneMissionId == Disaster), "no terminal before the mission");

            var offer = WildernessRanjaGorgeTests.Offer(harness, mcdonald, Disaster);
            CollectionAssert.AreEqual(new[]
                {
                    MarshesDataDisasterV1.ReflectiveLegs, MarshesDataDisasterV1.HazmatLegs,
                    MarshesDataDisasterV1.MotorAssistLegs, MarshesDataDisasterV1.MechVest
                },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, kiang, Disaster), "not Kiang's to give");
            Accept(harness, mcdonald, Disaster);
            Assert.IsEmpty(Topics(harness, mcdonald, Disaster), "McDonald hears no objective: the terminal does");

            var terminal = Terminal(harness, marshes);
            WildernessRanjaGorgeTests.Reload(harness);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Disaster, MarshesDataDisasterV1.Recover), "kept at a reload");
            harness.Drain();
            terminal = Terminal(harness, marshes);

            var talk = OpenTerminal(harness, terminal);
            Assert.IsNotNull(talk, "the terminal talks");
            Assert.IsTrue(talk.ConvoDataDict.TryGetValue(ConversationType.ObjectiveComplete, out var topics));
            Assert.AreEqual(((int)Disaster, (int)MarshesDataDisasterV1.Recover, 1),
                ((List<CompleteableObjectives>)topics).Select(topic => (topic.MissionId, topic.ObjectiveId, topic.PlayerFlagId)).Single());
            new NpcManager(harness, harness.Manager).CompleteNPCObjective(harness.Client, new CompleteNPCObjectivePacket
            {
                EntityId = terminal.EntityId, MissionId = Disaster, ObjectiveId = MarshesDataDisasterV1.Recover, PlayerFlagId = 1
            });
            var read = harness.Drain();
            Assert.AreEqual(1, read.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Disaster), "ObjectiveCompleted once");
            Assert.AreEqual(1, read.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Disaster && packet.IsCompleteable),
                "MissionCompleteable once");
            Assert.IsNull(OpenTerminal(harness, terminal), "read once");

            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, kiang.EntityId, Disaster, 3), "not Kiang's to take");
            FullBagWaits(harness, mcdonald, Disaster, 3, MarshesDataDisasterV1.ReflectiveLegs);
            FailedTurnInPaysNothing(harness, mcdonald, Disaster, 3);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, mcdonald, Disaster, 3,
                MarshesDataDisasterV1.Experience, MarshesDataDisasterV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Disaster), "MissionRewarded once");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, MarshesDataDisasterV1.MechVest), "the selected vest");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, mcdonald, Disaster), "done once");
        }

        /// <summary>An abandon takes the terminal away; taken again, a new one talks.</summary>
        [TestMethod]
        public void DataDisasterAbandonedTakesTheTerminal()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            PartTwoIsDone(harness);
            var marshes = EnterMarshes(harness);
            var mcdonald = Npc(marshes, McDonald);
            Accept(harness, mcdonald, Disaster);
            Assert.IsNotNull(OpenTerminal(harness, Terminal(harness, marshes)));

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Disaster));
            harness.Drain();
            Assert.IsFalse(marshes.DynamicObjects.Any(obj => obj.SceneMissionId == Disaster && obj.IsInWorld), "the terminal goes with it");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, mcdonald, Disaster), "offered again after an abandon");
            Accept(harness, mcdonald, Disaster);
            Assert.IsNotNull(OpenTerminal(harness, Terminal(harness, marshes)), "a new terminal talks");
        }

        [TestMethod]
        public void NeedALittleHelpHereInjectsTheFourSoldiers()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            var thoung = Npc(marshes, Thoung);
            var mcdonald = Npc(marshes, McDonald);
            var soldiers = MarshesNeedALittleHelpHereV1.Soldiers.Select(soldier => Npc(marshes, soldier.Npc)).ToArray();
            Assert.IsFalse(marshes.DynamicObjects.Any(obj => obj.SceneMissionId == Help), "no crate before the mission");

            var offer = WildernessRanjaGorgeTests.Offer(harness, thoung, Help);
            CollectionAssert.AreEqual(new[]
                {
                    MarshesNeedALittleHelpHereV1.CritHitBonus, MarshesNeedALittleHelpHereV1.ArmorAbsorption,
                    MarshesNeedALittleHelpHereV1.ThreatReduction, MarshesNeedALittleHelpHereV1.ArmorPiercing
                },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, mcdonald, Help), "not McDonald's to give");
            Accept(harness, thoung, Help);
            Assert.IsEmpty(Topics(harness, thoung, Help), "Thoung hears no objective");
            foreach (var soldier in soldiers)
                Assert.IsEmpty(Topics(harness, soldier, Help), $"{soldier.DbId}: no talk before the packs");
            foreach (var (objective, _, _, _, _, _) in MarshesNeedALittleHelpHereV1.Soldiers)
                Assert.AreEqual(MissionObjectiveState.Inactive, Objective(harness, Help, objective), $"{objective} waits");

            var crates = SupplyCrates(harness, marshes);
            Near(harness, crates[0]);
            Use(harness, marshes, crates[0], 1, recovers: false);
            Assert.AreEqual(0U, Packs(harness), "argument 1 is not the client's");
            WildernessAliaBranchesTests.FillCategory(harness, MissionItemFiller, 50);
            Take(harness, marshes, crates[0], MarshesNeedALittleHelpHereV1.UseArgument);
            Assert.AreEqual(0U, Packs(harness), "no room");
            Assert.IsTrue(crates[0].IsEnabled, "the crate waits");
            WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
            harness.Drain();

            Take(harness, marshes, crates[0], MarshesNeedALittleHelpHereV1.UseArgument);
            Take(harness, marshes, crates[1], MarshesNeedALittleHelpHereV1.UseArgument);
            harness.Drain();
            Assert.AreEqual(2U, Packs(harness));
            TryUse(harness, marshes, crates[1], MarshesNeedALittleHelpHereV1.UseArgument);
            Assert.AreEqual(2U, Packs(harness), "an opened crate gives no second");
            WildernessRanjaGorgeTests.Reload(harness);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Help, MarshesNeedALittleHelpHereV1.Find), "kept at a reload");
            Assert.AreEqual(2U, Packs(harness));
            harness.Drain();
            crates = SupplyCrates(harness, marshes, opened: 2);
            Take(harness, marshes, crates[2], MarshesNeedALittleHelpHereV1.UseArgument);
            harness.Drain();
            Take(harness, marshes, crates[3], MarshesNeedALittleHelpHereV1.UseArgument);
            var found = harness.Drain();
            Assert.AreEqual(4U, Packs(harness), "four packs");
            Assert.AreEqual(1, found.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Help &&
                packet.ObjectiveId == MarshesNeedALittleHelpHereV1.Find), "ObjectiveCompleted once");
            foreach (var (objective, _, _, _, _, _) in MarshesNeedALittleHelpHereV1.Soldiers)
                Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Help, objective), $"{objective} opens");

            // Each soldier hears his own objective only; each talk takes a pack. A reload halfway keeps them.
            Talk(harness, soldiers[0], Help, MarshesNeedALittleHelpHereV1.Brady);
            Assert.AreEqual(3U, Packs(harness), "Brady takes one");
            Assert.IsEmpty(Topics(harness, soldiers[0], Help), "Brady once");
            Assert.IsEmpty(Topics(harness, thoung, Help), "Thoung hears none");
            Talk(harness, soldiers[1], Help, MarshesNeedALittleHelpHereV1.Zui);
            WildernessRanjaGorgeTests.Reload(harness);
            Assert.AreEqual(2U, Packs(harness), "kept at a reload");
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Help, MarshesNeedALittleHelpHereV1.Zui));
            harness.Drain();
            Talk(harness, soldiers[2], Help, MarshesNeedALittleHelpHereV1.Dahn);
            Assert.IsFalse(harness.Client.Player.Missions[Help].Completeable, "Nez is left");

            AssertHandInTopic(Conversation(harness, soldiers[3]), Help, MarshesNeedALittleHelpHereV1.Nez);
            harness.MoveTo(soldiers[3].Position);
            harness.Drain();
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, soldiers[3].EntityId, Help, MarshesNeedALittleHelpHereV1.Nez, 1));
            var last = harness.Drain();
            Assert.AreEqual(1, last.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Help &&
                packet.ObjectiveId == MarshesNeedALittleHelpHereV1.Nez), "ObjectiveCompleted once");
            Assert.AreEqual(1, last.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Help && packet.IsCompleteable),
                "MissionCompleteable once");
            Assert.AreEqual(0U, Packs(harness), "all four injected");

            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, mcdonald.EntityId, Help, 0), "not McDonald's to take");
            FullBagWaits(harness, thoung, Help, 0, MarshesNeedALittleHelpHereV1.ArmorAbsorption);
            FailedTurnInPaysNothing(harness, thoung, Help, 0);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, thoung, Help, 0,
                MarshesNeedALittleHelpHereV1.Experience, MarshesNeedALittleHelpHereV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Help), "MissionRewarded once");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, MarshesNeedALittleHelpHereV1.CritHitBonus), "the selected module");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, thoung, Help), "done once");
        }

        /// <summary>An abandon after one injection takes the packs and crates away; taken again, the talks wait for new packs.</summary>
        [TestMethod]
        public void NeedALittleHelpHereAbandonedStartsOver()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            var thoung = Npc(marshes, Thoung);
            var brady = Npc(marshes, MarshesSentryTowerTwoNpcsV1.Brady);
            Accept(harness, thoung, Help);
            foreach (var crate in SupplyCrates(harness, marshes))
            {
                Take(harness, marshes, crate, MarshesNeedALittleHelpHereV1.UseArgument);
                harness.Drain();
            }
            Talk(harness, brady, Help, MarshesNeedALittleHelpHereV1.Brady);
            Assert.AreEqual(3U, Packs(harness));

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Help));
            harness.Drain();
            Assert.AreEqual(0U, Packs(harness), "the packs go with the mission");
            Assert.IsFalse(marshes.DynamicObjects.Any(obj => obj.SceneMissionId == Help && obj.IsInWorld), "the crates go with it");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, thoung, Help), "offered again after an abandon");
            Accept(harness, thoung, Help);
            Assert.AreEqual(MissionObjectiveState.Inactive, Objective(harness, Help, MarshesNeedALittleHelpHereV1.Brady), "Brady waits again");
            Assert.IsEmpty(Topics(harness, brady, Help), "no talk before the packs");
            Assert.IsTrue(SupplyCrates(harness, marshes).All(crate => crate.IsEnabled), "four new crates, ready");
        }

        /// <summary>Mission flag "Part II done" (as 1876 or 1877 sets it), then a reload.</summary>
        private static void PartTwoIsDone(WildernessRuntimeTestHarness harness)
        {
            using (var unit = harness.CreateChar())
                unit.ExecuteTransaction(() => unit.CharacterFlags.Set(harness.Client.Player.Id, PartTwoDone, 1));
            harness.Client.Player.PlayerFlags[PartTwoDone] = 1;   // as PlateauCampResistanceScroungerTests.SetFlag
            WildernessRanjaGorgeTests.Reload(harness);
        }

        private static DynamicObject[] SceneObjects(MapChannel marshes, uint mission, string prefix) =>
            marshes.DynamicObjects.Where(obj => obj.SceneMissionId == mission && obj.IsInWorld &&
                obj.SceneActorRole.StartsWith(prefix)).OrderBy(obj => obj.SceneActorRole).ToArray();

        /// <summary>The core crates: the player's own, of their class, at their places, on the navmesh, walked to; all but <paramref name="opened"/> ready.</summary>
        private static DynamicObject[] CoreCrates(WildernessRuntimeTestHarness harness, MapChannel marshes, int opened = 0)
        {
            var crates = SceneObjects(marshes, Core, "core-crate-");
            Assert.HasCount(MarshesCoreOfTheMatterV3.Crates.Length, crates);
            for (var index = 0; index < crates.Length; index++)
            {
                var crate = crates[index];
                var at = MarshesCoreOfTheMatterV3.Crates[index];
                Assert.AreEqual(MarshesCoreOfTheMatterV3.CrateRole(index), crate.SceneActorRole);
                Assert.AreEqual(MarshesCoreOfTheMatterV3.CrateClass, (uint)crate.EntityClassId);
                Assert.AreEqual(new Vector3(at.X, at.Y, at.Z), crate.Position);
                Assert.AreEqual(harness.Client.Player.Id, crate.SceneOwnerCharacterId, "the player's own");
                AssertOnGround(marshes, crate.Position, crate.SceneActorRole);
                Assert.IsTrue(marshes.NavMesh.FindPath(BaneAssertionCampWaypoint, crate.Position, out var complete) != null && complete,
                    $"{crate.SceneActorRole} is walked to");
                Assert.IsLessThan(35f, Vector2.Distance(new(452, -582), new(at.X, at.Z)), "before the depot door");
            }
            Assert.AreEqual(crates.Length - opened, crates.Count(crate => crate.IsEnabled));
            return crates;
        }

        private static DynamicObject[] SupplyCrates(WildernessRuntimeTestHarness harness, MapChannel marshes, int opened = 0)
        {
            var crates = SceneObjects(marshes, Help, "supply-crate-");
            Assert.HasCount(MarshesNeedALittleHelpHereV1.Crates.Length, crates);
            for (var index = 0; index < crates.Length; index++)
            {
                var crate = crates[index];
                var at = MarshesNeedALittleHelpHereV1.Crates[index];
                Assert.AreEqual(MarshesNeedALittleHelpHereV1.CrateRole(index), crate.SceneActorRole);
                Assert.AreEqual(MarshesNeedALittleHelpHereV1.CrateClass, (uint)crate.EntityClassId);
                Assert.AreEqual(new Vector3(at.X, at.Y, at.Z), crate.Position);
                Assert.AreEqual(harness.Client.Player.Id, crate.SceneOwnerCharacterId, "the player's own");
                AssertOnGround(marshes, crate.Position, crate.SceneActorRole);
                Assert.IsTrue(marshes.NavMesh.FindPath(BaneAssertionCampWaypoint, crate.Position, out var complete) != null && complete,
                    $"{crate.SceneActorRole} is walked to");
                Assert.IsLessThan(20f, Vector2.Distance(new(170, 55), new(at.X, at.Z)), "by Nelg's post (Ten Ton Hammer)");
            }
            Assert.AreEqual(crates.Length - opened, crates.Count(crate => crate.IsEnabled));
            return crates;
        }

        private static DynamicObject[] Consoles(WildernessRuntimeTestHarness harness, MapChannel marshes, int used = 0)
        {
            var consoles = SceneObjects(marshes, Destruction, "bane-computer-");
            Assert.HasCount(MarshesThisCallsForDestructionV1.Consoles.Length, consoles);
            for (var index = 0; index < consoles.Length; index++)
            {
                var console = consoles[index];
                var at = MarshesThisCallsForDestructionV1.Consoles[index];
                Assert.AreEqual(MarshesThisCallsForDestructionV1.ConsoleClass, (uint)console.EntityClassId);
                Assert.AreEqual(new Vector3(at.X, at.Y, at.Z), console.Position);
                Assert.AreEqual(harness.Client.Player.Id, console.SceneOwnerCharacterId, "the player's own");
                AssertOnGround(marshes, console.Position, console.SceneActorRole);
                Assert.IsTrue(marshes.NavMesh.FindPath(BaneAssertionCampWaypoint, console.Position, out var complete) != null && complete,
                    $"{console.SceneActorRole} is walked to");
            }
            Assert.AreEqual(consoles.Length - used, consoles.Count(console => console.IsEnabled));
            return consoles;
        }

        private static (DynamicObject Generator, DynamicObject Charge) Generator(WildernessRuntimeTestHarness harness, MapChannel marshes)
        {
            var generator = marshes.DynamicObjects.Single(obj => obj.SceneMissionId == Destruction &&
                obj.SceneActorRole == MarshesThisCallsForDestructionV1.GeneratorRole && obj.IsInWorld);
            var charge = marshes.DynamicObjects.Single(obj => obj.SceneMissionId == Destruction &&
                obj.SceneActorRole == MarshesThisCallsForDestructionV1.ChargeRole && obj.IsInWorld);
            Assert.AreEqual(MarshesThisCallsForDestructionV1.GeneratorClass, (uint)generator.EntityClassId);
            Assert.AreEqual(MarshesThisCallsForDestructionV1.ChargeClass, (uint)charge.EntityClassId);
            Assert.AreEqual(harness.Client.Player.Id, generator.SceneOwnerCharacterId, "the player's own");
            Assert.AreEqual(harness.Client.Player.Id, charge.SceneOwnerCharacterId, "the player's own");
            AssertOnGround(marshes, generator.Position, "the generator");
            AssertOnGround(marshes, charge.Position, "the charge");
            Assert.IsTrue(marshes.NavMesh.FindPath(BaneAssertionCampWaypoint, charge.Position, out var complete) != null && complete,
                "the charge is walked to");
            Assert.IsLessThan(MarshesThisCallsForDestructionV1.GeneratorRadius, Vector3.Distance(generator.Position, charge.Position),
                "the charge in the generator's marker");
            return (generator, charge);
        }

        /// <summary>The CommLink terminal: the player's own, of its class, talking with the client's package, on the walked-to deck.</summary>
        private static DynamicObject Terminal(WildernessRuntimeTestHarness harness, MapChannel marshes)
        {
            var terminal = marshes.DynamicObjects.Single(obj => obj.SceneMissionId == Disaster &&
                obj.SceneActorRole == MarshesDataDisasterV1.TerminalRole && obj.IsInWorld);
            var at = MarshesDataDisasterV1.TerminalAt;
            Assert.AreEqual(MarshesDataDisasterV1.TerminalClass, (uint)terminal.EntityClassId);
            Assert.AreEqual(new Vector3(at.X, at.Y, at.Z), terminal.Position);
            Assert.AreEqual(MarshesDataDisasterV1.TerminalPackage, terminal.MissionConversation.NpcPackageId);
            Assert.AreEqual(harness.Client.Player.Id, terminal.SceneOwnerCharacterId, "the player's own");
            AssertOnGround(marshes, terminal.Position, "the terminal");
            Assert.IsTrue(marshes.NavMesh.FindPath(BaneAssertionCampWaypoint, terminal.Position, out var complete) != null && complete,
                "the terminal is walked to");
            var point = MarshesDataDisasterV1.TenTonHammerAt;
            Assert.IsLessThan(20f, Vector2.Distance(new((float)point.X, (float)point.Z), new(at.X, at.Z)), "by Ten Ton Hammer's point");
            return terminal;
        }

        /// <summary>The terminal's conversation, as PoolsSnakePitContactsTests.OpenTerminal.</summary>
        private static ConversePacket OpenTerminal(WildernessRuntimeTestHarness harness, DynamicObject terminal)
        {
            harness.MoveTo(terminal.Position + new Vector3(0, 0, 1));
            harness.Drain();
            new NpcManager(harness, harness.Manager).RequestNpcConverse(harness.Client,
                new RequestNPCConversePacket { EntityId = terminal.EntityId, ActionId = ActionId.Converse, ActionArgId = 1 });
            return harness.Drain().OfType<ConversePacket>().LastOrDefault();
        }

        private static void Near(WildernessRuntimeTestHarness harness, DynamicObject obj)
        {
            harness.MoveTo(obj.Position + new Vector3(0, 0, 1));
            harness.Drain();
        }

        private static void Take(WildernessRuntimeTestHarness harness, MapChannel marshes, DynamicObject obj, uint argument)
        {
            Near(harness, obj);
            Use(harness, marshes, obj, argument, recovers: true);
        }

        /// <summary>A use the server may refuse at once or after its recovery: either way the caller checks that nothing came of it.</summary>
        private static void TryUse(WildernessRuntimeTestHarness harness, MapChannel marshes, DynamicObject obj, uint argument)
        {
            Near(harness, obj);
            var count = marshes.PerformRecovery.Count;
            harness.Objects.RequestUseObjectPacket(harness.Client, new RequestUseObjectPacket
            {
                EntityId = obj.EntityId, ActionId = ActionId.UseObject, ActionArgId = argument
            });
            if (marshes.PerformRecovery.Count > count)
            {
                var recovery = marshes.PerformRecovery.Last();
                marshes.PerformRecovery.Remove(recovery);
                ActorActionManager.Instance.PerformRecovery(marshes, recovery);
            }
            harness.Drain();
        }

        private static uint Cores(WildernessRuntimeTestHarness harness) =>
            WildernessRanjaGorgeTests.Held(harness, MarshesCoreOfTheMatterV3.CoreTemplate);

        private static uint Viruses(WildernessRuntimeTestHarness harness) =>
            WildernessRanjaGorgeTests.Held(harness, MarshesThisCallsForDestructionV1.VirusTemplate);

        private static uint Packs(WildernessRuntimeTestHarness harness) =>
            WildernessRanjaGorgeTests.Held(harness, MarshesNeedALittleHelpHereV1.PackTemplate);

        private static uint Uploads(WildernessRuntimeTestHarness harness) =>
            harness.Client.Player.Missions[Destruction].Objectives[MarshesThisCallsForDestructionV1.Upload].Counters[0];
    }
}
