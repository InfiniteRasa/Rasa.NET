using System;
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
using Rasa.Structures.Char;
using Rasa.Test.Missions.Marshes;
using Rasa.Test.Missions.Plateau;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions.Crucible
{
    using static MarshesFalconHoldTests;
    using static MarshesPaludosTests;
    using static PlateauCampResistanceTests;
    using N = CrucibleOutpostIntrepidNpcsV1;

    /// <summary>
    /// Crucible set 1 at Outpost Intrepid, played through as a player plays it: Corporal Shandy's The Last Men Standing
    /// (1142: the transfer orders to DeLisi), Captain DeLisi's A Fort Called Intrepid (1150: the Black Ice program into a
    /// terminal in the ruins) and The Wire (1145: Mullins at Convict Pass and the four Grupas at Staal), and Private
    /// Drake's The Departed (1143: twelve sets of dogtags from the fallen of Fort Intrepid).
    ///
    /// Why these tests: three NPCs are new and five upstream ones moved, every item is issued at the accept and taken at a
    /// talk or the turn-in, the terminal and the fallen are the player's own scene objects, and the chain gates each
    /// follow-up. An NPC off the navmesh, an offer before its prerequisite, a talk at the wrong NPC (D'Ambrose had Munit's
    /// package), a corpse searched twice or counted past twelve, progress lost at a reload, a full bag that loses an item,
    /// or a turn-in that pays twice would each break them.
    ///
    /// The map: Ligo Crucible (1993) is loaded into the harness as the Marshes tests load theirs (PlateauInstanceMaps.LoadMap;
    /// no upstream test does this for the Crucible). Its navmesh has three walkable regions for set 1: the open world, the
    /// Staal interior and the Labbna Quarter (NOTES-crucible-to-fix row 2), so each NPC is walked to from its own area's
    /// waypoint.
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class CrucibleOutpostIntrepidTests
    {
        private const uint LastMen = CrucibleLastMenStandingV1.MissionId;
        private const uint Departed = CrucibleDepartedV1.MissionId;
        private const uint Fort = CrucibleFortCalledIntrepidV1.MissionId;
        private const uint Wire = CrucibleWireV1.MissionId;
        private const uint MissionItemFiller = 11519;      // as PlateauCampResistanceTests (inventory category 4)
        private const uint ConsumableFiller = 45060;       // Class VII Basic Med Pack (as MarshesStalkerWoodsTests): fills the consumables bag

        /// <summary>The waypoints (World DB teleporter 191, 313, 193, 204, 269), where a player arrives.</summary>
        internal static readonly Vector3 OutpostWaypoint = new(943.68f, 144.08f, 60.53f);
        internal static readonly Vector3 FortWaypoint = new(717.81f, 168.6f, 540.13f);
        internal static readonly Vector3 StaalWaypoint = new(-233.28f, 180.75f, 316.8f);
        internal static readonly Vector3 InfirmaryWaypoint = new(-206.12f, 195.68f, 482.21f);
        internal static readonly Vector3 LabbnaWaypoint = new(-384.18f, 249.68f, 257.07f);

        [TestInitialize]
        public void Clock() => PlateauGiantKillerSquadTests.UseClock();

        [TestCleanup]
        public void Restore() => PlateauGiantKillerSquadTests.RestoreClock();

        /// <summary>
        /// Every NPC of set 1 stands on the navmesh at its place, with the client's name and package, friendly, and is walked
        /// to from its area's waypoint: Shandy, DeLisi and Drake from the Outpost Intrepid waypoint, Mullins from the Fort
        /// Intrepid one, Vitto and Munit from Staal's, Donal from the Staal Infirmary's, Rigs from the Labbna Quarter's.
        /// </summary>
        [TestMethod]
        public void TheNpcsStandOnTheNavmeshAndAreWalkedTo()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var crucible = EnterCrucible(harness);
            foreach (var (pool, name, package, from) in new (uint, uint, uint, Vector3)[]
            {
                (N.Shandy, 9060, N.ShandyPackage, OutpostWaypoint), (N.DeLisi, 9059, 1219, OutpostWaypoint), (N.Drake, 9061, 0, OutpostWaypoint),
                (N.Mullins, 9328, 1454, FortWaypoint), (N.Vitto, 9066, 1225, StaalWaypoint), (N.Munit, 9062, 1229, StaalWaypoint),
                (N.Donal, 9068, 1227, InfirmaryWaypoint), (N.Rigs, 9064, 1231, LabbnaWaypoint)
            })
            {
                var npc = Npc(crucible, pool);
                Assert.AreEqual(name, npc.NameId, $"{pool} name");
                if (package != 0)
                    Assert.AreEqual(package, npc.Npc.NpcPackageId, $"{pool} package");
                Assert.AreNotEqual(TargetCategory.Hostile, npc.TargetCategory);
                var at = CrucibleStaalWalksV1.Now(pool) ?? N.At(pool);
                Assert.IsLessThan(0.1f, Vector2.Distance(new((float)at.X, (float)at.Z), new(npc.Position.X, npc.Position.Z)), $"{pool} at its place");
                AssertOnGround(crucible, npc.Position, $"{pool}");
                Assert.IsTrue(crucible.NavMesh.FindPath(from, npc.Position, out var complete) != null && complete, $"{pool} is walked to");
            }
        }

        [TestMethod]
        public void TheLastMenStandingTakesTheOrdersToDeLisi()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var crucible = EnterCrucible(harness);
            var shandy = Npc(crucible, N.Shandy);
            var delisi = Npc(crucible, N.DeLisi);
            var drake = Npc(crucible, N.Drake);

            var offer = WildernessRanjaGorgeTests.Offer(harness, shandy, LastMen);
            CollectionAssert.AreEqual(new[] { 111232U, 118907U, 111038U, 111028U },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, delisi, LastMen), "not DeLisi's to give");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, drake, LastMen), "not Drake's to give");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, delisi, Fort), "A Fort Called Intrepid waits for it");

            // No room for the orders: no mission. One slot freed, they come with it.
            WildernessAliaBranchesTests.FillCategory(harness, MissionItemFiller, 50);
            harness.MoveTo(shandy.Position);
            harness.Drain();
            Assert.IsFalse(harness.Manager.AcceptOfferedMission(harness.Client, shandy.EntityId, LastMen), "no room for the orders");
            Assert.IsFalse(harness.Client.Player.Missions.ContainsKey(LastMen));
            Assert.AreEqual(0U, Orders(harness));
            WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
            Accept(harness, shandy, LastMen);
            Assert.AreEqual(1U, Orders(harness), "the transfer orders");
            Assert.IsEmpty(Topics(harness, shandy, LastMen), "Shandy hears no objective");
            Assert.IsEmpty(Topics(harness, drake, LastMen), "nor Drake");

            WildernessRanjaGorgeTests.Reload(harness);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, LastMen, CrucibleLastMenStandingV1.Report), "kept at a reload");
            Assert.AreEqual(1U, Orders(harness), "the orders kept at a reload");
            harness.Drain();

            AssertHandInTopic(Conversation(harness, delisi), LastMen, CrucibleLastMenStandingV1.Report);
            harness.MoveTo(delisi.Position);
            harness.Drain();
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, delisi.EntityId, LastMen, CrucibleLastMenStandingV1.Report, 1));
            var packets = harness.Drain();
            Assert.AreEqual(1, packets.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == LastMen), "ObjectiveCompleted once");
            Assert.AreEqual(1, packets.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == LastMen && packet.IsCompleteable),
                "MissionCompleteable once");
            Assert.AreEqual(0U, Orders(harness), "DeLisi takes the orders");
            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, shandy.EntityId, LastMen, 0), "not Shandy's to take");

            FullBagWaits(harness, delisi, LastMen, 2, ConsumableFiller);
            FailedTurnInPaysNothing(harness, delisi, LastMen, 2);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, delisi, LastMen, 2,
                CrucibleLastMenStandingV1.Experience, CrucibleLastMenStandingV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == LastMen), "MissionRewarded once");
            Assert.IsGreaterThanOrEqualTo(CrucibleLastMenStandingV1.IncendiaryGrenades,
                WildernessRanjaGorgeTests.Held(harness, CrucibleLastMenStandingV1.IncendiaryGrenade), "the selected grenades");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, shandy, LastMen), "done once");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, delisi, Fort), "A Fort Called Intrepid follows");
        }

        /// <summary>An abandon takes the orders back; the mission is offered again, and taken again gives a new set.</summary>
        [TestMethod]
        public void TheLastMenStandingAbandonedTakesTheOrders()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var crucible = EnterCrucible(harness);
            var shandy = Npc(crucible, N.Shandy);
            Accept(harness, shandy, LastMen);
            Assert.AreEqual(1U, Orders(harness));
            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, LastMen));
            harness.Drain();
            Assert.AreEqual(0U, Orders(harness), "the orders go");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, shandy, LastMen), "offered again after an abandon");
            Accept(harness, shandy, LastMen);
            Assert.AreEqual(1U, Orders(harness), "new orders");
        }

        [TestMethod]
        public void AFortCalledIntrepidUploadsTheProgramAtTheTerminal()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var crucible = EnterCrucible(harness);
            var delisi = Npc(crucible, N.DeLisi);
            var shandy = Npc(crucible, N.Shandy);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, delisi, Fort), "it waits for The Last Men Standing");
            SeedCompleted(harness, LastMen);
            crucible = EnterCrucible(harness);
            Assert.IsFalse(crucible.DynamicObjects.Any(obj => obj.SceneMissionId == Fort), "no terminal before the mission");

            var offer = WildernessRanjaGorgeTests.Offer(harness, delisi, Fort);
            CollectionAssert.AreEqual(new[]
                {
                    CrucibleFortCalledIntrepidV1.StealthVest, CrucibleFortCalledIntrepidV1.GravitonHelmet,
                    CrucibleFortCalledIntrepidV1.HazmatBoots, CrucibleFortCalledIntrepidV1.MotorAssistVest
                },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, shandy, Fort), "not Shandy's to give");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, delisi, Wire), "The Wire waits for it");
            Accept(harness, delisi, Fort);
            Assert.AreEqual(1U, Program(harness), "the Black Ice program");
            Assert.IsEmpty(Topics(harness, delisi, Fort), "DeLisi hears no objective: the terminal does");

            var terminal = Terminal(harness, crucible);
            WildernessRanjaGorgeTests.Reload(harness);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Fort, CrucibleFortCalledIntrepidV1.Upload), "kept at a reload");
            harness.Drain();
            terminal = Terminal(harness, crucible);

            var talk = OpenTerminal(harness, terminal);
            Assert.IsNotNull(talk, "the terminal talks");
            Assert.IsTrue(talk.ConvoDataDict.TryGetValue(ConversationType.ObjectiveComplete, out var topics));
            Assert.AreEqual(((int)Fort, (int)CrucibleFortCalledIntrepidV1.Upload, 1),
                ((List<CompleteableObjectives>)topics).Select(topic => (topic.MissionId, topic.ObjectiveId, topic.PlayerFlagId)).Single());
            new NpcManager(harness, harness.Manager).CompleteNPCObjective(harness.Client, new CompleteNPCObjectivePacket
            {
                EntityId = terminal.EntityId, MissionId = Fort, ObjectiveId = CrucibleFortCalledIntrepidV1.Upload, PlayerFlagId = 1
            });
            var read = harness.Drain();
            Assert.AreEqual(1, read.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Fort), "ObjectiveCompleted once");
            Assert.AreEqual(1, read.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Fort && packet.IsCompleteable),
                "MissionCompleteable once");
            Assert.AreEqual(0U, Program(harness), "the program is uploaded");
            Assert.IsNull(OpenTerminal(harness, terminal), "uploaded once");

            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, shandy.EntityId, Fort, 1), "not Shandy's to take");
            FullBagWaits(harness, delisi, Fort, 1, CrucibleFortCalledIntrepidV1.GravitonHelmet);
            FailedTurnInPaysNothing(harness, delisi, Fort, 1);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, delisi, Fort, 1,
                CrucibleFortCalledIntrepidV1.Experience, CrucibleFortCalledIntrepidV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Fort), "MissionRewarded once");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, delisi, Fort), "done once");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, delisi, Wire), "The Wire follows");
        }

        /// <summary>An abandon takes the terminal and the program away; taken again, a new terminal talks.</summary>
        [TestMethod]
        public void AFortCalledIntrepidAbandonedTakesTheTerminal()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            SeedCompleted(harness, LastMen);
            var crucible = EnterCrucible(harness);
            var delisi = Npc(crucible, N.DeLisi);
            Accept(harness, delisi, Fort);
            Assert.IsNotNull(OpenTerminal(harness, Terminal(harness, crucible)));

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Fort));
            harness.Drain();
            Assert.IsFalse(crucible.DynamicObjects.Any(obj => obj.SceneMissionId == Fort && obj.IsInWorld), "the terminal goes with it");
            Assert.AreEqual(0U, Program(harness), "the program goes with it");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, delisi, Fort), "offered again after an abandon");
            Accept(harness, delisi, Fort);
            Assert.IsNotNull(OpenTerminal(harness, Terminal(harness, crucible)), "a new terminal talks");
        }

        /// <summary>
        /// The Departed: twenty fallen of the player's own in the ruins; a full bag leaves the tags on the body; one use each,
        /// a set each; the twelfth set completes the tracker and 1, and then all twenty go; Drake takes the twelve.
        /// </summary>
        [TestMethod]
        public void TheDepartedTakesTwelveSetsOfDogtagsToDrake()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var crucible = EnterCrucible(harness);
            var drake = Npc(crucible, N.Drake);
            var delisi = Npc(crucible, N.DeLisi);
            Assert.IsFalse(crucible.DynamicObjects.Any(obj => obj.SceneMissionId == Departed), "no fallen before the mission");

            var offer = WildernessRanjaGorgeTests.Offer(harness, drake, Departed);
            CollectionAssert.AreEqual(new[]
                {
                    CrucibleDepartedV1.BioLegs, CrucibleDepartedV1.MechGloves, CrucibleDepartedV1.ReflectiveGloves, CrucibleDepartedV1.GravitonHelmet
                },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, delisi, Departed), "not DeLisi's to give");
            Accept(harness, drake, Departed);
            Assert.IsEmpty(Topics(harness, drake, Departed), "Drake hears no objective");

            var corpses = Fallen(harness, crucible);
            WildernessAliaBranchesTests.FillCategory(harness, MissionItemFiller, 50);
            Search(harness, crucible, corpses[0]);
            Assert.AreEqual(0U, Tags(harness), "no room");
            Assert.IsTrue(corpses[0].IsEnabled, "the body waits");
            Assert.AreEqual((UseObjectState)CrucibleDepartedV1.ClosedState, corpses[0].StateId);
            WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);

            var completed = new List<uint>();
            var completeable = 0;
            for (var index = 0; index < CrucibleDepartedV1.Tags; index++)
            {
                if (index == 6)
                {
                    WildernessRanjaGorgeTests.Reload(harness);
                    harness.Drain();
                    Assert.AreEqual(6U, Tags(harness), "kept at a reload");
                    corpses = Fallen(harness, crucible);
                    Assert.IsFalse(corpses[5].IsEnabled, "a searched body stays searched");
                }
                Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Departed, CrucibleDepartedV1.Collect), $"open before set {index + 1}");
                Search(harness, crucible, corpses[index]);
                Assert.AreEqual((uint)index + 1, Tags(harness), $"set {index + 1}");
                Assert.AreEqual((UseObjectState)CrucibleDepartedV1.OpenedState, corpses[index].StateId);
                Assert.IsFalse(corpses[index].IsEnabled, "searched once");
                if (index == 0)
                {
                    TrySearch(harness, crucible, corpses[0]);
                    Assert.AreEqual(1U, Tags(harness), "a body gives one set");
                }
                var drained = harness.Drain();
                completed.AddRange(drained.OfType<ObjectiveCompletedPacket>().Where(packet => packet.MissionId == Departed).Select(packet => packet.ObjectiveId));
                completeable += drained.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Departed && packet.IsCompleteable);
            }
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Departed, CrucibleDepartedV1.Collect), "twelve sets");
            Assert.AreEqual(1, completed.Count(objective => objective == CrucibleDepartedV1.Collect), "ObjectiveCompleted once for 1");
            Assert.AreEqual(1, completeable, "MissionCompleteable once");
            harness.Manager.Scenes.Tick(crucible);
            Assert.IsFalse(crucible.DynamicObjects.Any(obj => obj.SceneMissionId == Departed && obj.IsInWorld && obj.IsEnabled),
                "the fallen left go: no thirteenth set");

            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, delisi.EntityId, Departed, 0), "not DeLisi's to take");
            FullBagWaits(harness, drake, Departed, 0, CrucibleDepartedV1.BioLegs);
            FailedTurnInPaysNothing(harness, drake, Departed, 0);
            Assert.AreEqual(CrucibleDepartedV1.Tags, Tags(harness), "kept at the failed turn-in");
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, drake, Departed, 0,
                CrucibleDepartedV1.Experience, CrucibleDepartedV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Departed), "MissionRewarded once");
            Assert.AreEqual(0U, Tags(harness), "Drake takes the tags");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, drake, Departed), "done once");
        }

        /// <summary>An abandon takes the tags and the fallen; taken again, twenty whole bodies and no set counted.</summary>
        [TestMethod]
        public void TheDepartedAbandonedTakesTheTagsAndTheFallen()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var crucible = EnterCrucible(harness);
            var drake = Npc(crucible, N.Drake);
            Accept(harness, drake, Departed);
            var corpses = Fallen(harness, crucible);
            Search(harness, crucible, corpses[0]);
            Search(harness, crucible, corpses[1]);
            Assert.AreEqual(2U, Tags(harness));

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Departed));
            harness.Manager.Scenes.Tick(crucible);
            harness.Drain();
            Assert.AreEqual(0U, Tags(harness), "the tags go");
            Assert.IsFalse(crucible.DynamicObjects.Any(obj => obj.SceneMissionId == Departed && obj.IsInWorld), "the fallen go");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, drake, Departed), "offered again after an abandon");
            Accept(harness, drake, Departed);
            Assert.IsTrue(Fallen(harness, crucible).All(corpse => corpse.IsEnabled), "twenty whole bodies");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Departed, CrucibleDepartedV1.Collect));
        }

        /// <summary>
        /// The Wire after A Fort Called Intrepid: the bug at the accept; Mullins, Vitto, Donal, Munit and Rigs each hear only
        /// their own objective (D'Ambrose, who had Munit's package, none); a reload halfway; DeLisi takes the bug at the turn-in.
        /// </summary>
        [TestMethod]
        public void TheWireRecordsTheFourGrupas()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var crucible = EnterCrucible(harness);
            var delisi = Npc(crucible, N.DeLisi);
            SeedCompleted(harness, LastMen);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, delisi, Wire), "it waits for A Fort Called Intrepid");
            SeedCompleted(harness, Fort);
            crucible = EnterCrucible(harness);
            var talkers = CrucibleWireV1.Talks.Select(talk => (talk.Objective, Npc: Npc(crucible, talk.Npc))).ToArray();
            var dambrose = Npc(crucible, N.DAmbrose);

            var offer = WildernessRanjaGorgeTests.Offer(harness, delisi, Wire);
            CollectionAssert.AreEqual(new[]
                {
                    CrucibleWireV1.MechLegs, CrucibleWireV1.HazmatLegs, CrucibleWireV1.GravitonBoots, CrucibleWireV1.MotorAssistBoots
                },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Accept(harness, delisi, Wire);
            Assert.AreEqual(1U, Bug(harness), "the audio surveillance bug");
            Assert.IsEmpty(Topics(harness, delisi, Wire), "DeLisi hears no objective");
            Assert.IsEmpty(Topics(harness, dambrose, Wire), "nor D'Ambrose (package 1301, not Munit's 1229)");
            foreach (var (objective, npc) in talkers)
                CollectionAssert.AreEqual(new[] { objective }, Topics(harness, npc, Wire), $"{npc.DbId} hears only {objective}");

            var packets = new List<object>();
            for (var index = 0; index < talkers.Length; index++)
            {
                var (objective, npc) = talkers[index];
                if (index == 2)
                {
                    WildernessRanjaGorgeTests.Reload(harness);
                    Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Wire, talkers[1].Objective), "kept at a reload");
                    Assert.AreEqual(1U, Bug(harness), "the bug kept at a reload");
                    harness.Drain();
                }
                Assert.IsFalse(harness.Client.Player.Missions[Wire].Completeable, $"not ready before talk {index + 1}");
                harness.MoveTo(npc.Position);
                harness.Drain();
                var other = talkers[(index + 1) % talkers.Length].Objective;
                Assert.IsFalse(harness.Manager.CompleteOfferedObjective(harness.Client, npc.EntityId, Wire, other, 1), $"{npc.DbId} does not hear {other}");
                Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, npc.EntityId, Wire, objective, 1), $"{npc.DbId} hears {objective}");
                packets.AddRange(harness.Drain());
                Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Wire, objective));
            }
            CollectionAssert.AreEquivalent(new[] { 6U, 1U, 2U, 3U, 5U }, packets.OfType<ObjectiveCompletedPacket>()
                .Where(packet => packet.MissionId == Wire).Select(packet => packet.ObjectiveId).ToArray(), "ObjectiveCompleted once each");
            Assert.AreEqual(1, packets.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Wire && packet.IsCompleteable),
                "MissionCompleteable once");

            FullBagWaits(harness, delisi, Wire, 3, CrucibleWireV1.MotorAssistBoots);
            FailedTurnInPaysNothing(harness, delisi, Wire, 3);
            Assert.AreEqual(1U, Bug(harness), "kept at the failed turn-in");
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, delisi, Wire, 3, CrucibleWireV1.Experience, CrucibleWireV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Wire), "MissionRewarded once");
            Assert.AreEqual(0U, Bug(harness), "DeLisi takes the bug");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, delisi, Wire), "done once");
        }

        /// <summary>An abandon takes the bug and the talks back; taken again, Mullins talks again.</summary>
        [TestMethod]
        public void TheWireAbandonedStartsOver()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            SeedCompleted(harness, LastMen, Fort);
            var crucible = EnterCrucible(harness);
            var delisi = Npc(crucible, N.DeLisi);
            var mullins = Npc(crucible, N.Mullins);
            Accept(harness, delisi, Wire);
            Talk(harness, mullins, Wire, CrucibleWireV1.Mullins);

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Wire));
            harness.Drain();
            Assert.AreEqual(0U, Bug(harness), "the bug goes");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, delisi, Wire), "offered again after an abandon");
            Accept(harness, delisi, Wire);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Wire, CrucibleWireV1.Mullins), "the old talk does not count");
            Talk(harness, mullins, Wire, CrucibleWireV1.Mullins);
        }

        /// <summary>Ligo Crucible as the server loads it (as MarshesPaludosTests.EnterMarshes), the player at <paramref name="at"/> (the Outpost Intrepid waypoint).</summary>
        internal static MapChannel EnterCrucible(WildernessRuntimeTestHarness harness, Vector3? at = null)
        {
            var map = harness.Maps.MapChannelArray.TryGetValue(CrucibleMissionDataV1.MapContextId, out var loaded)
                ? loaded
                : PlateauInstanceMaps.LoadMap(harness, CrucibleMissionDataV1.MapContextId);
            var client = harness.Client;
            CellManager.Instance.RemoveFromWorld(client);
            client.Player.MapChannel.ClientList.Remove(client);
            client.Player.MapChannel = map;
            client.Player.RuntimeMapChannel = map;
            client.Player.MapContextId = CrucibleMissionDataV1.MapContextId;
            map.ClientList.Add(client);
            harness.MoveTo(at ?? OutpostWaypoint);
            CellManager.Instance.AddToWorld(client);
            harness.Drain();
            return map;
        }

        /// <summary>Completed Crucible missions in the character's history (as MarshesFalconHoldTests.SeedCompletedMarshes).</summary>
        internal static void SeedCompleted(WildernessRuntimeTestHarness harness, params uint[] completed)
        {
            using (var unit = harness.CreateChar())
                unit.ExecuteTransaction(() =>
                {
                    foreach (var mission in completed)
                        unit.CharacterMissions.Runtime.Add(new CharacterMissionHistoryEntry
                        {
                            CharacterId = harness.Client.Player.Id,
                            MissionId = mission,
                            AssignmentId = Guid.NewGuid().ToString("N"),
                            AssignmentGeneration = 1,
                            ContentRevision = CrucibleMissionDataV1.Revision,
                            CompletedAtUtc = harness.UtcNow,
                            RewardedAtUtc = harness.UtcNow,
                            Outcome = (uint)MissionState.Completed,
                            Rewarded = true
                        });
                });
            WildernessRanjaGorgeTests.Reload(harness);
        }

        /// <summary>The terminal: the player's own, of its class and package, at its place, on the ground, walked to from the Fort Intrepid waypoint.</summary>
        private static DynamicObject Terminal(WildernessRuntimeTestHarness harness, MapChannel crucible)
        {
            harness.Manager.Scenes.Tick(crucible);
            var terminal = crucible.DynamicObjects.Single(obj => obj.SceneMissionId == Fort &&
                obj.SceneActorRole == CrucibleFortCalledIntrepidV1.TerminalRole && obj.IsInWorld);
            var at = CrucibleFortCalledIntrepidV1.TerminalAt;
            Assert.AreEqual(CrucibleFortCalledIntrepidV1.TerminalClass, (uint)terminal.EntityClassId);
            Assert.AreEqual(new Vector3(at.X, at.Y, at.Z), terminal.Position);
            Assert.AreEqual(CrucibleFortCalledIntrepidV1.TerminalPackage, terminal.MissionConversation.NpcPackageId);
            Assert.AreEqual(harness.Client.Player.Id, terminal.SceneOwnerCharacterId, "the player's own");
            AssertOnGround(crucible, terminal.Position, "the terminal");
            Assert.IsTrue(crucible.NavMesh.FindPath(FortWaypoint, terminal.Position, out var complete) != null && complete, "the terminal is walked to");
            Assert.IsLessThan(30f, Vector2.Distance(new(790, 664), new(at.X, at.Z)), "by the ruined headquarters");
            return terminal;
        }

        /// <summary>The terminal's conversation, as MarshesSentryTowerTwoTests.OpenTerminal.</summary>
        private static ConversePacket OpenTerminal(WildernessRuntimeTestHarness harness, DynamicObject terminal)
        {
            harness.MoveTo(terminal.Position + new Vector3(0, 0, 1));
            harness.Drain();
            new NpcManager(harness, harness.Manager).RequestNpcConverse(harness.Client,
                new RequestNPCConversePacket { EntityId = terminal.EntityId, ActionId = ActionId.Converse, ActionArgId = 1 });
            return harness.Drain().OfType<ConversePacket>().LastOrDefault();
        }

        /// <summary>The twenty fallen, in their order: the player's own, of their classes and name, on the ground, walked to from the Fort Intrepid waypoint.</summary>
        private static DynamicObject[] Fallen(WildernessRuntimeTestHarness harness, MapChannel crucible)
        {
            harness.Manager.Scenes.Tick(crucible);
            var corpses = new DynamicObject[CrucibleDepartedV1.CorpsesAt.Length];
            for (var index = 0; index < corpses.Length; index++)
            {
                var corpse = crucible.DynamicObjects.Single(obj => obj.SceneMissionId == Departed &&
                    obj.SceneActorRole == CrucibleDepartedV1.CorpseRole(index) && obj.IsInWorld);
                var at = CrucibleDepartedV1.CorpsesAt[index];
                Assert.AreEqual(CrucibleDepartedV1.CorpseClasses[index % CrucibleDepartedV1.CorpseClasses.Length], (uint)corpse.EntityClassId);
                Assert.AreEqual(new Vector3(at.X, at.Y, at.Z), corpse.Position);
                Assert.AreEqual(harness.Client.Player.Id, corpse.SceneOwnerCharacterId, "the player's own");
                AssertOnGround(crucible, corpse.Position, $"corpse {index + 1}");
                Assert.IsTrue(crucible.NavMesh.FindPath(FortWaypoint, corpse.Position, out var complete) != null && complete, $"corpse {index + 1} is walked to");
                corpses[index] = corpse;
            }
            return corpses;
        }

        private static void Search(WildernessRuntimeTestHarness harness, MapChannel crucible, DynamicObject corpse)
        {
            harness.MoveTo(corpse.Position + new Vector3(0, 0, 1));
            harness.Drain();
            Use(harness, crucible, corpse, CrucibleDepartedV1.UseArgument, recovers: true);
        }

        /// <summary>A use the server may refuse at once or after its recovery: the caller checks that nothing came of it.</summary>
        private static void TrySearch(WildernessRuntimeTestHarness harness, MapChannel crucible, DynamicObject corpse)
        {
            harness.MoveTo(corpse.Position + new Vector3(0, 0, 1));
            harness.Drain();
            var count = crucible.PerformRecovery.Count;
            harness.Objects.RequestUseObjectPacket(harness.Client, new RequestUseObjectPacket
            {
                EntityId = corpse.EntityId, ActionId = ActionId.UseObject, ActionArgId = CrucibleDepartedV1.UseArgument
            });
            if (crucible.PerformRecovery.Count > count)
            {
                var recovery = crucible.PerformRecovery.Last();
                crucible.PerformRecovery.Remove(recovery);
                ActorActionManager.Instance.PerformRecovery(crucible, recovery);
            }
            harness.Drain();
        }

        private static uint Orders(WildernessRuntimeTestHarness harness) => WildernessRanjaGorgeTests.Held(harness, CrucibleLastMenStandingV1.OrdersTemplate);
        private static uint Program(WildernessRuntimeTestHarness harness) => WildernessRanjaGorgeTests.Held(harness, CrucibleFortCalledIntrepidV1.ProgramTemplate);
        private static uint Bug(WildernessRuntimeTestHarness harness) => WildernessRanjaGorgeTests.Held(harness, CrucibleWireV1.BugTemplate);
        private static uint Tags(WildernessRuntimeTestHarness harness) => WildernessRanjaGorgeTests.Held(harness, CrucibleDepartedV1.TagsTemplate);
    }
}
