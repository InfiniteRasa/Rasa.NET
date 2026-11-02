using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Managers;
using Rasa.Packets;
using Rasa.Packets.MapChannel.Client;
using Rasa.Packets.Mission.Server;
using Rasa.Services.Preloader.Missions.Marshes;
using Rasa.Structures;
using Rasa.Test.Missions.Plateau;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions.Marshes
{
    using static MarshesEscortsTests;
    using static MarshesFalconHoldTests;
    using static MarshesPaludosTests;
    using static PlateauCampResistanceTests;

    /// <summary>
    /// Marshes set 11, the Retread Arena, played through as a player plays it: Retread Karl's Beat Street (1085: Withers and
    /// Ingram), Smackdown (1087: King and Baker) and Legend or Fall (1088: Toro, or a fall taken at Retread Foster's choice),
    /// and Retread Vincent's Calling all Parts (1084: a part each from a Stalker, a Caretaker and a Forean Machina).
    ///
    /// Why these tests: the arena is the dome's navmesh island, so Foster's talk must set the player down inside and the
    /// round's end outside; the fighters must be the player's own, hostile and sent at the player; the 5-minute fight and
    /// the 3 minutes to Foster are the scene's timer and the mission's deadline; 1088's choice must give the right way to
    /// finish; the parts must drop only while their objective is open. A reload, a full bag, a failed save and an abandon
    /// must each leave the run as it was.
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class MarshesRetreadArenaTests
    {
        private const uint BeatStreet = MarshesRetreadArenaV1.BeatStreet, Smackdown = MarshesRetreadArenaV1.Smackdown;
        private const uint LegendOrFall = MarshesRetreadArenaV1.LegendOrFall, Parts = MarshesCallingAllPartsV1.MissionId;
        private const uint Karl = MarshesRetreadArenaNpcsV1.Karl, Foster = MarshesRetreadArenaNpcsV1.Foster;
        private const uint Vincent = MarshesStalkerWoodsNpcsV1.Vincent;
        private const uint ConsumableFiller = 45060;       // Class VII Basic Med Pack: fills the consumables bag

        private static readonly Vector3 RetreadCityWaypoint = new(-250.45f, 216.8f, -830.56f);
        private static readonly Vector3 HighPointRetreatWaypoint = new(-784.272f, 241.4954f, -547.8776f);

        [TestInitialize]
        public void Clock() => PlateauGiantKillerSquadTests.UseClock();

        [TestCleanup]
        public void Restore() => PlateauGiantKillerSquadTests.RestoreClock();

        [TestMethod]
        public void KarlAndFosterStandAtTheDomeAndItsFloorIsAnIsland()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            foreach (var (id, name, package) in new[]
            {
                (Karl, MarshesRetreadArenaNpcsV1.KarlName, MarshesRetreadArenaNpcsV1.KarlPackage),
                (Foster, MarshesRetreadArenaNpcsV1.FosterName, MarshesRetreadArenaNpcsV1.FosterPackage)
            })
            {
                var npc = Npc(marshes, id);
                Assert.AreEqual((name, package), (npc.NameId, npc.Npc.NpcPackageId), $"{id}: name and package");
                Assert.AreNotEqual(TargetCategory.Hostile, npc.TargetCategory, $"{id} is no foe");
                AssertOnGround(marshes, npc.Position, $"{id}");
                Assert.IsTrue(marshes.NavMesh.FindPath(RetreadCityWaypoint, npc.Position, out var walked) != null && walked, $"{id} is walked to");
            }
            var karl = Npc(marshes, Karl);
            var foster = Npc(marshes, Foster);
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, karl, BeatStreet), "Beat Street needs nothing");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, karl, Smackdown), "Smackdown waits for Beat Street");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, karl, LegendOrFall));
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, foster, BeatStreet), "not Foster's to give");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, Npc(marshes, Vincent), Parts), "Calling all Parts waits for the arena");

            var arrival = ToVector(MarshesRetreadArenaNpcsV1.ArenaArrival);
            var exit = ToVector(MarshesRetreadArenaNpcsV1.ArenaExit);
            AssertOnGround(marshes, arrival, "the arena arrival");
            AssertOnGround(marshes, exit, "the arena exit");
            Assert.IsTrue(marshes.NavMesh.FindPath(RetreadCityWaypoint, exit, out var outside) != null && outside, "the exit is walked to");
            Assert.IsLessThan(10f, Vector3.Distance(exit, foster.Position), "set down by Foster");
            Assert.IsFalse(marshes.NavMesh.FindPath(RetreadCityWaypoint, arrival, out var inside) != null && inside, "no walk into the dome");
            Assert.IsFalse(marshes.NavMesh.FindPath(arrival, exit, out var outOf) != null && outOf, "no walk out of it");
            foreach (var round in MarshesRetreadArenaV1.Rounds)
                foreach (var (_, at) in round.Fighters)
                {
                    AssertOnGround(marshes, ToVector(at), $"{round.MissionId} fighter at {at}");
                    Assert.IsTrue(marshes.NavMesh.FindPath(arrival, ToVector(at), out var reach) != null && reach, $"{at} on the dome's floor");
                }
        }

        /// <summary>
        /// A player left in the dome (no mission, no scene: called off) walks to its pad and is taken out to the exit; the
        /// exit's pad takes nobody back in.
        /// </summary>
        [TestMethod]
        public void APlayerLeftInTheDomeTakesItsPadOut()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            var arrival = ToVector(MarshesRetreadArenaNpcsV1.ArenaArrival);
            var exit = ToVector(MarshesRetreadArenaNpcsV1.ArenaExit);
            var rows = harness.World.TeleporterEntries.AsNoTracking()
                .Where(row => row.Id == MarshesRetreadArenaNpcsV1.DomePad || row.Id == MarshesRetreadArenaNpcsV1.ExitPad).ToDictionary(row => row.Id);
            Assert.HasCount(2, rows);
            foreach (var row in rows.Values)
                Assert.AreEqual((MarshesRetreadArenaNpcsV1.MapContextId, MarshesRetreadArenaNpcsV1.PadClass, MarshesRetreadArenaNpcsV1.LocalTeleporter),
                    (row.MapContextId, row.ClassId, (uint)row.Type), $"{row.Id}");
            CollectionAssert.AreEquivalent(new[] { (MarshesRetreadArenaNpcsV1.DomePad, MarshesRetreadArenaNpcsV1.ExitPad) },
                harness.World.LocalTeleporterLinkEntries.AsNoTracking().ToArray().Where(link => rows.ContainsKey(link.FromTeleporterId))
                    .Select(link => (link.FromTeleporterId, link.ToTeleporterId)).ToArray(), "one way out");

            // The pads as InitTeleporters reads them (the Marshes is loaded after it here) and the link as InitLocalTeleporters does.
            foreach (var row in rows.Values)
            {
                var loaded = new DynamicObject
                {
                    Position = new Vector3((float)row.PosX, (float)row.PosY, (float)row.PosZ),
                    Rotation = row.Rotation,
                    MapContextId = row.MapContextId,
                    RuntimeMapChannel = marshes,
                    EntityClassId = (EntityClasses)row.ClassId,
                    Comment = row.Description,
                    ObjectData = new WaypointInfo(row.Id, false, WaypointType.LocalTeleporter),
                    DynamicObjectType = DynamicObjectType.LocalTeleporter
                };
                marshes.Teleporters.Add(row.Id, loaded);
                harness.Objects.Teleporters.TryAdd(row.Id, loaded);
            }
            harness.Objects.InitLocalTeleporters();
            harness.Objects.DynamicObjectWorker(marshes, 0);
            var pad = marshes.Teleporters[MarshesRetreadArenaNpcsV1.DomePad];
            var outside = marshes.Teleporters[MarshesRetreadArenaNpcsV1.ExitPad];
            AssertOnGround(marshes, pad.Position, "the dome's pad");
            Assert.IsTrue(marshes.NavMesh.FindPath(arrival, pad.Position, out var reach) != null && reach, "walked to from the arrival");
            Assert.IsLessThan(1f, Vector3.Distance(exit, outside.Position), "the outside pad at the exit");
            foreach (var round in MarshesRetreadArenaV1.Rounds)
                foreach (var (_, at) in round.Fighters)
                    Assert.IsGreaterThan(8f, Vector3.Distance(ToVector(at), pad.Position), $"clear of the fighter at {at}");
            Assert.IsGreaterThan(8f, Vector3.Distance(arrival, pad.Position), "clear of the arrival");

            harness.MoveTo(arrival);
            harness.Drain();
            harness.MoveTo(pad.Position);
            harness.Drain();
            harness.Objects.DynamicObjectProximityWorker(marshes, pad, 0);
            Assert.IsNotNull(harness.Client.PendingTransfer, "taken by the pad");
            System.Threading.Thread.Sleep(DynamicObjectManager.PassageHoldMs + 50);
            harness.Objects.PassagesWorker(marshes);
            harness.Objects.TeleportAcknowledge(harness.Client);
            harness.Drain();
            Assert.IsNull(harness.Client.PendingTransfer);
            Assert.IsLessThan(2f, Vector3.Distance(exit, harness.Client.Player.Position), "out at the exit");
            Assert.IsTrue(marshes.NavMesh.FindPath(harness.Client.Player.Position, RetreadCityWaypoint, out var home) != null && home,
                "free to walk away");

            harness.Objects.DynamicObjectProximityWorker(marshes, outside, 0);
            Assert.IsNull(harness.Client.PendingTransfer, "the exit's pad takes nobody back in");
        }

        /// <summary>
        /// 1085: Foster's talk takes the player into the dome with Withers and Ingram (the player's own, hostile, sent at the
        /// player); a reload in the fight keeps it; both beaten, 1 completes once and the player is set down outside; Karl pays,
        /// then offers Smackdown.
        /// </summary>
        [TestMethod]
        public void BeatStreetBeatsWithersAndIngram()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            var karl = Npc(marshes, Karl);
            var foster = Npc(marshes, Foster);
            var offer = WildernessRanjaGorgeTests.Offer(harness, karl, BeatStreet);
            CollectionAssert.AreEqual(new[]
                {
                    MarshesRetreadArenaV1.EmpBomb, MarshesRetreadArenaV1.PowerCharger, MarshesRetreadArenaV1.EmpGrenade,
                    MarshesRetreadArenaV1.ConcussionGrenade
                },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Begin(harness, marshes, karl, BeatStreet, MarshesRetreadArenaV1.Enter);
            Assert.AreEqual(MissionObjectiveState.Inactive, Objective(harness, BeatStreet, MarshesRetreadArenaV1.Fight), "1 hidden");
            Assert.IsEmpty(Topics(harness, karl, BeatStreet), "Karl hears nothing");
            Assert.IsFalse(Fighters(harness, marshes, BeatStreet).Any(), "no fighter before the talk");

            var log = new List<PythonPacket>();
            var fighters = EnterArena(harness, marshes, foster, BeatStreet, MarshesRetreadArenaV1.Rounds[0], log);
            ReloadAndRecover(harness, marshes);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, BeatStreet, MarshesRetreadArenaV1.Fight), "kept at a reload");
            fighters = Fighters(harness, marshes, BeatStreet).ToArray();
            Assert.HasCount(2, fighters, "both fighters after the reload");

            log.AddRange(Die(harness, marshes, fighters[0]));
            Assert.AreEqual(1U, Counter(harness, BeatStreet), "one beaten");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, BeatStreet, MarshesRetreadArenaV1.Fight));
            log.AddRange(Die(harness, marshes, fighters[1]));
            AssertWon(harness, marshes, BeatStreet, log);

            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, foster.EntityId, BeatStreet, 0), "not Foster's to take");
            FullBagWaits(harness, karl, BeatStreet, 0, ConsumableFiller);
            FailedTurnInPaysNothing(harness, karl, BeatStreet, 0);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, karl, BeatStreet, 3,
                MarshesRetreadArenaV1.Experience, MarshesRetreadArenaV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == BeatStreet), "MissionRewarded once");
            Assert.AreEqual(4U, WildernessRanjaGorgeTests.Held(harness, MarshesRetreadArenaV1.ConcussionGrenade), "the selected stack");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, karl, BeatStreet), "done once");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, karl, Smackdown), "Smackdown next");
        }

        /// <summary>The 5 minutes out with a fighter standing: 1 fails, the mission fails, the player is set down outside; cleared, Karl offers it again.</summary>
        /// <summary>
        /// To-fix row 8: the fighters killed with a GM .kill (GmSupportCommands.Kill: a blow from the GM's player, so the kill
        /// is the player's) count as the round's defeats, and the second completes 1. The bot's loss was a kill while its own
        /// .tele was still in flight: an upstream scene path drops a signal for an owner in a transfer (to-fix row 8, watch).
        /// </summary>
        [TestMethod]
        public void BeatStreetCountsAGmKill()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            var karl = Npc(marshes, Karl);
            var foster = Npc(marshes, Foster);
            Begin(harness, marshes, karl, BeatStreet, MarshesRetreadArenaV1.Enter);
            var log = new List<PythonPacket>();
            var fighters = EnterArena(harness, marshes, foster, BeatStreet, MarshesRetreadArenaV1.Rounds[0], log);
            for (var index = 0; index < fighters.Length; index++)
            {
                GmSupportCommands.Kill(harness.Client, new[] { ".kill", "#" + fighters[index].EntityId });
                log.AddRange(harness.Drain());
                log.AddRange(Tick(harness, marshes, 250));
                Assert.AreEqual(CharacterState.Dead, fighters[index].State, $"fighter {index + 1} killed");
                if (index == 0)
                    Assert.AreEqual(1U, Counter(harness, BeatStreet), "one beaten by the GM kill");
            }
            AssertWon(harness, marshes, BeatStreet, log);
        }

        [TestMethod]
        public void BeatStreetFailsWhenTheFightRunsOut()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            var karl = Npc(marshes, Karl);
            var foster = Npc(marshes, Foster);
            Begin(harness, marshes, karl, BeatStreet, MarshesRetreadArenaV1.Enter);
            var log = new List<PythonPacket>();
            var fighters = EnterArena(harness, marshes, foster, BeatStreet, MarshesRetreadArenaV1.Rounds[0], log);
            Die(harness, marshes, fighters[0]);

            Advance(harness, marshes, MarshesRetreadArenaV1.FightMilliseconds - 5000, BeatStreet, log);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, BeatStreet, MarshesRetreadArenaV1.Fight), "not before 5:00");
            Advance(harness, marshes, 7000, BeatStreet, log);
            Assert.AreEqual(MissionObjectiveState.Failed, Objective(harness, BeatStreet, MarshesRetreadArenaV1.Fight), "the time is up");
            Assert.AreEqual(MissionState.Failed, harness.Client.Player.Missions[BeatStreet].State);
            Assert.AreEqual(1, log.OfType<ObjectiveFailedPacket>().Count(packet => packet.MissionId == BeatStreet), "ObjectiveFailed once");
            AssertSetDownOutside(harness);
            Assert.IsFalse(Fighters(harness, marshes, BeatStreet).Any(), "Ingram goes");

            Assert.IsTrue(harness.Manager.TryClear(harness.Client, BeatStreet), "the failed mission cleared");
            Tick(harness, marshes, 250);
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, karl, BeatStreet), "offered again");
        }

        /// <summary>Foster not spoken to in 3 minutes: 2 fails and the mission with it; abandoned instead (a timed mission), it fails too and is offered again once cleared.</summary>
        [TestMethod]
        public void BeatStreetFailsWithoutFosterInThreeMinutes()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            var karl = Npc(marshes, Karl);
            var foster = Npc(marshes, Foster);
            Begin(harness, marshes, karl, BeatStreet, MarshesRetreadArenaV1.Enter);
            for (var second = 0; second < MarshesRetreadArenaV1.EnterSeconds - 5; second++)
                Tick(harness, marshes, 1000);
            harness.Manager.EvaluateDeadlines(harness.Client);
            harness.Drain();
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, BeatStreet, MarshesRetreadArenaV1.Enter), "not yet at 2:55");
            for (var second = 0; second < 10; second++)
                Tick(harness, marshes, 1000);
            harness.Manager.EvaluateDeadlines(harness.Client);
            harness.Drain();
            Tick(harness, marshes, 250);
            Assert.AreEqual(MissionObjectiveState.Failed, Objective(harness, BeatStreet, MarshesRetreadArenaV1.Enter), "past 3 minutes");
            Assert.AreEqual(MissionState.Failed, harness.Client.Player.Missions[BeatStreet].State);
            Assert.IsEmpty(Topics(harness, foster, BeatStreet), "Foster lets no one in now");

            Assert.IsTrue(harness.Manager.TryClear(harness.Client, BeatStreet));
            Tick(harness, marshes, 250);
            Begin(harness, marshes, karl, BeatStreet, MarshesRetreadArenaV1.Enter);
            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, BeatStreet));
            harness.Drain();
            Assert.IsTrue(harness.Manager.TryClear(harness.Client, BeatStreet), "the abandoned timed mission cleared");
            Tick(harness, marshes, 250);
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, karl, BeatStreet), "offered again");
        }

        /// <summary>1087 after 1085: King and Baker, as Beat Street's; Karl pays and offers Legend or Fall.</summary>
        [TestMethod]
        public void SmackdownBeatsKingAndBaker()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            SeedCompletedMarshes(harness, BeatStreet);
            var marshes = EnterMarshes(harness);
            var karl = Npc(marshes, Karl);
            var foster = Npc(marshes, Foster);
            var offer = WildernessRanjaGorgeTests.Offer(harness, karl, Smackdown);
            CollectionAssert.AreEqual(new[]
                {
                    MarshesRetreadArenaV1.EmpBomb, MarshesRetreadArenaV1.AdrenalineBooster, MarshesRetreadArenaV1.ConcussionGrenade,
                    MarshesRetreadArenaV1.FragGrenade
                },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Begin(harness, marshes, karl, Smackdown, MarshesRetreadArenaV1.Enter);
            var log = new List<PythonPacket>();
            var fighters = EnterArena(harness, marshes, foster, Smackdown, MarshesRetreadArenaV1.Rounds[1], log);
            log.AddRange(Die(harness, marshes, fighters[1]));
            log.AddRange(Die(harness, marshes, fighters[0]));
            AssertWon(harness, marshes, Smackdown, log);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, karl, Smackdown, 1,
                MarshesRetreadArenaV1.Experience, MarshesRetreadArenaV1.Credits);
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, karl, LegendOrFall), "Legend or Fall next");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, Npc(marshes, Vincent), Parts), "Calling all Parts waits for 1088");
        }

        /// <summary>1088 fought (choice 2): flag 1088001 = 2, 3 stays hidden; Toro beaten, 1 done, out; Karl pays; Vincent offers 1084.</summary>
        [TestMethod]
        public void LegendOrFallFoughtOut()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            SeedCompletedMarshes(harness, BeatStreet, Smackdown);
            var marshes = EnterMarshes(harness);
            var karl = Npc(marshes, Karl);
            var foster = Npc(marshes, Foster);
            Begin(harness, marshes, karl, LegendOrFall, MarshesRetreadArenaV1.Enter);
            CollectionAssert.AreEqual(new[] { (MarshesRetreadArenaV1.Enter, 1U) }, Choices(harness, foster, LegendOrFall), "Foster's choice");

            var log = new List<PythonPacket>();
            var toro = EnterArena(harness, marshes, foster, LegendOrFall, MarshesRetreadArenaV1.Rounds[2], log,
                MarshesRetreadArenaV1.FightChoice).Single();
            Assert.AreEqual(MarshesRetreadArenaV1.FightChoice, Flag(harness));
            Assert.AreEqual(MissionObjectiveState.Inactive, Objective(harness, LegendOrFall, MarshesRetreadArenaV1.Fall), "no fall offered");
            Assert.AreEqual(15000, (int)toro.Attributes[Attributes.Health].NormalMax, "the champion's hit points");
            log.AddRange(Die(harness, marshes, toro));
            AssertWon(harness, marshes, LegendOrFall, log);
            Assert.AreEqual(MissionObjectiveState.Inactive, Objective(harness, LegendOrFall, MarshesRetreadArenaV1.Fall));

            FullBagWaits(harness, karl, LegendOrFall, 2, ConsumableFiller);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, karl, LegendOrFall, 2,
                MarshesRetreadArenaV1.Experience, MarshesRetreadArenaV1.Credits);
            Assert.AreEqual(2U, WildernessRanjaGorgeTests.Held(harness, MarshesRetreadArenaV1.CryoGrenade), "the selected stack");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, Npc(marshes, Vincent), Parts), "Calling all Parts next");
        }

        /// <summary>1088 fought (choice 2) and the 5 minutes run out with Toro standing: 1 fails, the mission fails.</summary>
        [TestMethod]
        public void LegendOrFallFoughtAndLostOnTime()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            SeedCompletedMarshes(harness, BeatStreet, Smackdown);
            var marshes = EnterMarshes(harness);
            var karl = Npc(marshes, Karl);
            var foster = Npc(marshes, Foster);
            Begin(harness, marshes, karl, LegendOrFall, MarshesRetreadArenaV1.Enter);
            var log = new List<PythonPacket>();
            EnterArena(harness, marshes, foster, LegendOrFall, MarshesRetreadArenaV1.Rounds[2], log, MarshesRetreadArenaV1.FightChoice);
            Advance(harness, marshes, MarshesRetreadArenaV1.FightMilliseconds + 2000, LegendOrFall, log);
            Assert.AreEqual(MissionObjectiveState.Failed, Objective(harness, LegendOrFall, MarshesRetreadArenaV1.Fight));
            Assert.AreEqual(MissionState.Failed, harness.Client.Player.Missions[LegendOrFall].State);
            AssertSetDownOutside(harness);
        }

        /// <summary>
        /// 1088 with a fall (choice 1): flag 1088001 = 1, 1 and 3 open; the 5 minutes out with Toro standing complete both
        /// once, the player is set down outside and Toro goes; Karl pays the same.
        /// </summary>
        [TestMethod]
        public void LegendOrFallTakesAFall()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            SeedCompletedMarshes(harness, BeatStreet, Smackdown);
            var marshes = EnterMarshes(harness);
            var karl = Npc(marshes, Karl);
            var foster = Npc(marshes, Foster);
            Begin(harness, marshes, karl, LegendOrFall, MarshesRetreadArenaV1.Enter);
            var log = new List<PythonPacket>();
            EnterArena(harness, marshes, foster, LegendOrFall, MarshesRetreadArenaV1.Rounds[2], log, MarshesRetreadArenaV1.FallChoice);
            Assert.AreEqual(MarshesRetreadArenaV1.FallChoice, Flag(harness));
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, LegendOrFall, MarshesRetreadArenaV1.Fall), "the fall offered");

            Advance(harness, marshes, MarshesRetreadArenaV1.FightMilliseconds - 5000, LegendOrFall, log);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, LegendOrFall, MarshesRetreadArenaV1.Fall), "not before 5:00");
            Advance(harness, marshes, 7000, LegendOrFall, log);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, LegendOrFall, MarshesRetreadArenaV1.Fall), "the fall taken");
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, LegendOrFall, MarshesRetreadArenaV1.Fight), "1 done by the fall");
            Assert.AreEqual(1, log.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == LegendOrFall && packet.IsCompleteable),
                "MissionCompleteable once");
            Assert.IsEmpty(log.OfType<ObjectiveFailedPacket>().Where(packet => packet.MissionId == LegendOrFall), "nothing fails");
            AssertSetDownOutside(harness);
            Tick(harness, marshes, 250);
            Assert.IsFalse(Fighters(harness, marshes, LegendOrFall).Any(), "Toro goes");

            FailedTurnInPaysNothing(harness, karl, LegendOrFall, 0);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, karl, LegendOrFall, 0,
                MarshesRetreadArenaV1.Experience, MarshesRetreadArenaV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == LegendOrFall), "MissionRewarded once");
        }

        /// <summary>
        /// 1084: no part drops before the mission; a Stalker, a Caretaker and a Forean Machina each drop theirs (a reload
        /// between); MissionCompleteable once; Vincent takes the parts and pays.
        /// </summary>
        [TestMethod]
        public void CallingAllPartsTakesAPartFromEachKind()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            SeedCompletedMarshes(harness, BeatStreet, Smackdown, LegendOrFall);
            var marshes = EnterMarshes(harness);
            var vincent = Npc(marshes, Vincent);
            var karl = Npc(marshes, Karl);
            Spawn(marshes, MarshesCallingAllPartsV1.Stalker, 531283, MarshesRetreadArenaNpcsV1.MachinaPool);
            var machinas = Alive(marshes).Where(creature => creature.DbId == MarshesRetreadArenaNpcsV1.Machina).ToArray();
            Assert.HasCount((int)MarshesRetreadArenaNpcsV1.Machinas, machinas, "the Machina pool");
            foreach (var machina in machinas)
            {
                Assert.AreEqual(TargetCategory.Hostile, machina.TargetCategory);
                Assert.AreEqual((int)MarshesRetreadArenaNpcsV1.MachinaHitPoints, (int)machina.Attributes[Attributes.Health].NormalMax);
                AssertOnGround(marshes, machina.Position, "a Machina");
            }
            Assert.IsTrue(marshes.NavMesh.FindPath(HighPointRetreatWaypoint, machinas[0].Position, out var walked) != null && walked, "walked to");

            Assert.IsFalse(Holds(KillMachina(harness, marshes), 45456), "no implant before the mission");
            harness.Drain();
            var offer = WildernessRanjaGorgeTests.Offer(harness, vincent, Parts);
            CollectionAssert.AreEqual(new[]
                {
                    MarshesCallingAllPartsV1.VirulentInjectorGun, MarshesCallingAllPartsV1.SonicCannon,
                    MarshesCallingAllPartsV1.ElectricRifle, MarshesCallingAllPartsV1.LaserChaingun
                },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, karl, Parts), "not Karl's to give");
            Accept(harness, vincent, Parts);

            var completeable = 0;
            foreach (var (objective, _, _, template, _, _, creature) in MarshesCallingAllPartsV1.Parts)
            {
                if (!Alive(marshes).Any(candidate => candidate.DbId == creature && candidate.Npc == null))
                    Respawn(marshes, PoolsOf(harness, creature));
                var loot = creature == MarshesCallingAllPartsV1.Machina ? KillMachina(harness, marshes) : Kill(harness, marshes, creature);
                Assert.IsTrue(Holds(loot, template), $"{creature} drops {template}");
                PickUp(harness, loot);
                Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, template));
                Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Parts, objective), $"part {objective}");
                completeable += harness.Drain().OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Parts && packet.IsCompleteable);
                if (objective == MarshesCallingAllPartsV1.Motherboard)
                {
                    WildernessRanjaGorgeTests.Reload(harness);
                    Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Parts, objective), "kept at a reload");
                    // A second Stalker drops no second motherboard: the objective is done.
                    Respawn(marshes, PoolsOf(harness, creature));
                    Assert.IsFalse(Holds(Kill(harness, marshes, creature), template), "one motherboard");
                    harness.Drain();
                }
            }
            Assert.AreEqual(1, completeable, "MissionCompleteable once");

            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, karl.EntityId, Parts, 1), "not Karl's to take");
            FullBagWaits(harness, vincent, Parts, 1, MarshesCallingAllPartsV1.SonicCannon);
            FailedTurnInPaysNothing(harness, vincent, Parts, 1);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, vincent, Parts, 1,
                MarshesCallingAllPartsV1.Experience, MarshesCallingAllPartsV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Parts), "MissionRewarded once");
            foreach (var (_, _, _, template, _, _, _) in MarshesCallingAllPartsV1.Parts)
                Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, template), $"Vincent takes {template}");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, vincent, Parts), "done once");
        }

        /// <summary>1084 abandoned with a part held: offered again, the part kept counts at once.</summary>
        [TestMethod]
        public void CallingAllPartsAbandonedKeepsThePart()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            SeedCompletedMarshes(harness, BeatStreet, Smackdown, LegendOrFall);
            var marshes = EnterMarshes(harness);
            var vincent = Npc(marshes, Vincent);
            Spawn(marshes, MarshesRetreadArenaNpcsV1.MachinaPool);
            Accept(harness, vincent, Parts);
            PickUp(harness, KillMachina(harness, marshes));
            harness.Drain();
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Parts, MarshesCallingAllPartsV1.BrainImplant));

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Parts));
            harness.Drain();
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, 45456), "the implant kept");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, vincent, Parts), "offered again");
            Accept(harness, vincent, Parts);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Parts, MarshesCallingAllPartsV1.BrainImplant), "the kept part counts");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Parts, MarshesCallingAllPartsV1.Motherboard));
        }

        /// <summary>
        /// Foster's talk (1088: the choice <paramref name="choice"/>): 2 done once, 1 open, the player taken into the dome and
        /// set down; the round's fighters there, the player's own, hostile, at their places and sent at the player.
        /// </summary>
        private static Creature[] EnterArena(WildernessRuntimeTestHarness harness, MapChannel marshes, Creature foster, uint mission,
            MarshesRetreadArenaV1.Round round, List<PythonPacket> log, uint? choice = null)
        {
            harness.MoveTo(foster.Position);
            harness.Drain();
            if (choice is { } picked)
            {
                var npcs = new NpcManager(harness, harness.Manager);
                npcs.RequestNpcConverse(harness.Client, new RequestNPCConversePacket { EntityId = foster.EntityId });
                npcs.PerformNPCChoice(harness.Client, new PerformNPCChoicePacket
                {
                    EntityId = foster.EntityId, MissionId = mission, ObjectiveId = MarshesRetreadArenaV1.Enter, PlayerFlagId = 1,
                    ChoiceIdx = (int)picked
                });
                log.AddRange(harness.Drain());
                Assert.AreEqual(1, log.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == mission &&
                    packet.ObjectiveId == MarshesRetreadArenaV1.Enter), "2 ObjectiveCompleted once");
            }
            else
                Talk(harness, foster, mission, MarshesRetreadArenaV1.Enter);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, mission, MarshesRetreadArenaV1.Fight));
            log.AddRange(Tick(harness, marshes, 250));

            var transfer = harness.Client.PendingTransfer;
            Assert.IsNotNull(transfer, "Foster sends the player in");
            Assert.IsLessThan(2f, Vector3.Distance(ToVector(MarshesRetreadArenaNpcsV1.ArenaArrival), transfer.DestinationPosition), "onto the dome's floor");
            harness.Client.PendingTransfer = null;
            harness.MoveTo(ToVector(MarshesRetreadArenaNpcsV1.ArenaArrival));
            harness.Drain();
            // The fighters' orders wait while the passage holds the player and are tried again a second later.
            log.AddRange(Tick(harness, marshes, 1000));
            log.AddRange(Tick(harness, marshes, 1000));

            var fighters = Fighters(harness, marshes, mission).ToArray();
            Assert.HasCount(round.Fighters.Length, fighters, $"{mission}'s fighters");
            for (var index = 0; index < fighters.Length; index++)
            {
                var (creature, at) = round.Fighters[index];
                Assert.AreEqual(creature, fighters[index].DbId);
                Assert.AreEqual(TargetCategory.Hostile, fighters[index].TargetCategory, "a foe");
                Assert.IsTrue(fighters[index].SceneAttackPastLeash, $"{fighters[index].DbId} is sent at the player");
                Assert.IsLessThan(12f, Vector3.Distance(fighters[index].Position, ToVector(at)), "on the dome's floor");
            }
            return fighters;
        }

        /// <summary>The round won: 1 done once, MissionCompleteable once, the player set down outside, the fighters gone.</summary>
        private static void AssertWon(WildernessRuntimeTestHarness harness, MapChannel marshes, uint mission, List<PythonPacket> log)
        {
            log.AddRange(Tick(harness, marshes, 250));
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, mission, MarshesRetreadArenaV1.Fight));
            Assert.AreEqual(1, log.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == mission &&
                packet.ObjectiveId == MarshesRetreadArenaV1.Fight), "ObjectiveCompleted once");
            Assert.AreEqual(1, log.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == mission && packet.IsCompleteable),
                "MissionCompleteable once");
            AssertSetDownOutside(harness);
            Tick(harness, marshes, 250);
            Assert.IsFalse(Fighters(harness, marshes, mission).Any(), "the fighters go");
        }

        /// <summary>The player is taken out of the dome to Foster's door; the passage lands there.</summary>
        private static void AssertSetDownOutside(WildernessRuntimeTestHarness harness)
        {
            var transfer = harness.Client.PendingTransfer;
            Assert.IsNotNull(transfer, "set down outside");
            Assert.IsLessThan(2f, Vector3.Distance(ToVector(MarshesRetreadArenaNpcsV1.ArenaExit), transfer.DestinationPosition), "by Foster");
            harness.Client.PendingTransfer = null;
            harness.MoveTo(ToVector(MarshesRetreadArenaNpcsV1.ArenaExit));
            harness.Drain();
        }

        /// <summary>The clock on by <paramref name="milliseconds"/>, a second at a time; the player kept alive (only the timer is under test).</summary>
        private static void Advance(WildernessRuntimeTestHarness harness, MapChannel marshes, uint milliseconds, uint mission, List<PythonPacket> log)
        {
            for (var elapsed = 0U; elapsed < milliseconds; elapsed += 1000)
            {
                var health = harness.Client.Player.Attributes[Attributes.Health];
                health.Current = health.NormalMax;
                log.AddRange(Tick(harness, marshes, 1000));
            }
        }

        private static IEnumerable<Creature> Fighters(WildernessRuntimeTestHarness harness, MapChannel marshes, uint mission) =>
            Enumerable.Range(0, 2).Select(index => SceneCreature(harness, marshes, mission, MarshesRetreadArenaV1.FighterRole(index)))
                .Where(creature => creature != null);

        private static uint Counter(WildernessRuntimeTestHarness harness, uint mission) =>
            harness.Client.Player.Missions[mission].Objectives[MarshesRetreadArenaV1.Fight].Counters[0];

        /// <summary>Accepted at Karl or Vincent, its scene started.</summary>
        private static void Begin(WildernessRuntimeTestHarness harness, MapChannel marshes, Creature giver, uint mission, uint first)
        {
            Accept(harness, giver, mission);
            harness.Manager.Scenes.Resume(harness.Client);
            harness.Drain();
            Tick(harness, marshes, 250);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, mission, first));
        }

        /// <summary>
        /// A living Machina killed as the player: an ordinary first death is a self revive (CreatureSupport: no kill, no loot;
        /// it stands up a few seconds later on the wall clock), so the test finishes it (a crit kill: no revive); its corpse.
        /// </summary>
        /// <summary>
        /// To-fix row 12: the bot GM-killed the three 1084 Machina and saw no loot. They did get up (the bot's events show
        /// "Revived" 3 s after each .kill), but the bot did not kill them again. Why this test: a Machina's first death (a GM
        /// .kill or a player's ordinary kill) is its self revive: it goes down with no corpse and stands up 3 s later
        /// (CreatureSupport.Worker); its second death is the real one, with the implant in its corpse.
        /// </summary>
        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void CallingAllPartsMachinaGivesItsImplantAtItsSecondDeath(bool gm)
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            harness.LoadAbilities();   // the self revive's level (CR_MACHINA_REVIVE 429), as the server loads it
            SeedCompletedMarshes(harness, BeatStreet, Smackdown, LegendOrFall);
            var marshes = EnterMarshes(harness);
            Spawn(marshes, MarshesRetreadArenaNpcsV1.MachinaPool);
            Accept(harness, Npc(marshes, Vincent), Parts);
            if (!Alive(marshes).Any(candidate => candidate.DbId == MarshesCallingAllPartsV1.Machina))
                Respawn(marshes, MarshesRetreadArenaNpcsV1.MachinaPool);
            var machina = Alive(marshes).First(candidate => candidate.DbId == MarshesCallingAllPartsV1.Machina);
            harness.MoveTo(machina.Position + new Vector3(0, 0, 5));
            void Die()
            {
                if (gm)
                    GmSupportCommands.Kill(harness.Client, new[] { ".kill", "#" + machina.EntityId });
                else
                {
                    machina.Attributes[Attributes.Health].Current = 0;
                    harness.Creatures.HandleCreatureKill(marshes, machina, harness.Client.Player);
                }
                harness.Drain();
            }

            Die();
            Assert.AreEqual(CharacterState.Dead, machina.State, "down");
            Assert.IsFalse(marshes.LootDispensers.ContainsKey(machina.CorpseLootEntityId), "no corpse at the first death");
            CreatureSupport.Worker(marshes, System.Environment.TickCount64 + 4000);
            harness.Drain();
            Assert.AreNotEqual(CharacterState.Dead, machina.State, "it gets up");
            Assert.IsGreaterThan(0, (int)machina.Attributes[Attributes.Health].Current);

            Die();
            Assert.AreEqual(CharacterState.Dead, machina.State, "the real death");
            Assert.IsTrue(marshes.LootDispensers.TryGetValue(machina.CorpseLootEntityId, out var loot), "a corpse");
            Assert.IsTrue(Holds(loot, MarshesCallingAllPartsV1.Parts.Single(part => part.Objective == MarshesCallingAllPartsV1.BrainImplant).Template), "the implant");
            PickUp(harness, loot);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Parts, MarshesCallingAllPartsV1.BrainImplant));
        }

        /// <summary>
        /// To-fix row 18: the bot reported that a Machina downed by an earlier character gave a later character with objective 3
        /// open no part at its real death. Its events show otherwise: each of the three Machina sent "Revived" (health 5203 of
        /// 10406) 3 s after the .kill, and the bot still held them dead and did not kill them again (as row 12). Why this test:
        /// the self revive does not care who downed the Machina, and its real death is the killer's, with the implant for a
        /// killer who has objective 3 open; another character without the mission downs it first (its revive), then the holder
        /// kills it once, its real death: the implant in its corpse, 3 completed.
        /// </summary>
        [TestMethod]
        public void CallingAllPartsMachinaDownedByAnotherCharacterGivesTheHolderItsImplant()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            harness.LoadAbilities();
            SeedCompletedMarshes(harness, BeatStreet, Smackdown, LegendOrFall);
            var marshes = EnterMarshes(harness);
            Spawn(marshes, MarshesRetreadArenaNpcsV1.MachinaPool);
            Accept(harness, Npc(marshes, Vincent), Parts);
            if (!Alive(marshes).Any(candidate => candidate.DbId == MarshesCallingAllPartsV1.Machina))
                Respawn(marshes, MarshesRetreadArenaNpcsV1.MachinaPool);
            var machina = Alive(marshes).First(candidate => candidate.DbId == MarshesCallingAllPartsV1.Machina);
            harness.MoveTo(machina.Position + new Vector3(0, 0, 5));
            var other = harness.Context.CreateAdditionalClient(2, manager: harness.Manager);
            try
            {
                CellManager.Instance.RemoveFromWorld(other);
                other.Player.MapChannel.ClientList.Remove(other);
                other.Player.MapChannel = marshes;
                other.Player.RuntimeMapChannel = marshes;
                other.Player.MapContextId = MarshesMissionDataV1.MapContextId;
                marshes.ClientList.Add(other);
                other.SetWorldPosition(machina.Position + new Vector3(0, 0, -5), 0);
                CellManager.Instance.AddToWorld(other);
                Assert.IsFalse(other.Player.Missions.ContainsKey(Parts), "the other character has no 1084");

                GmSupportCommands.Kill(other, new[] { ".kill", "#" + machina.EntityId });
                harness.Drain();
                Assert.AreEqual(CharacterState.Dead, machina.State, "down, by the other character");
                Assert.IsFalse(marshes.LootDispensers.ContainsKey(machina.CorpseLootEntityId), "no corpse at its revive");
                CreatureSupport.Worker(marshes, System.Environment.TickCount64 + 4000);
                harness.Drain();
                Assert.AreNotEqual(CharacterState.Dead, machina.State, "it gets up");

                GmSupportCommands.Kill(harness.Client, new[] { ".kill", "#" + machina.EntityId });
                harness.Drain();
                Assert.AreEqual(CharacterState.Dead, machina.State, "the real death, the holder's single kill");
                Assert.IsTrue(marshes.LootDispensers.TryGetValue(machina.CorpseLootEntityId, out var loot), "a corpse");
                Assert.IsTrue(Holds(loot, MarshesCallingAllPartsV1.Parts.Single(part => part.Objective == MarshesCallingAllPartsV1.BrainImplant).Template), "the implant");
                PickUp(harness, loot);
                Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Parts, MarshesCallingAllPartsV1.BrainImplant));
            }
            finally
            {
                CellManager.Instance.RemoveFromWorld(other);
                marshes.ClientList.Remove(other);
            }
        }

        private static LootDispenser KillMachina(WildernessRuntimeTestHarness harness, MapChannel marshes)
        {
            if (!Alive(marshes).Any(candidate => candidate.DbId == MarshesCallingAllPartsV1.Machina))
                Respawn(marshes, MarshesRetreadArenaNpcsV1.MachinaPool);
            var machina = Alive(marshes).First(candidate => candidate.DbId == MarshesCallingAllPartsV1.Machina);
            harness.MoveTo(machina.Position);
            machina.Attributes[Attributes.Health].Current = 0;
            harness.Creatures.HandleCreatureKill(marshes, machina, harness.Client.Player, CritKill.Own);
            Assert.IsTrue(marshes.LootDispensers.TryGetValue(machina.CorpseLootEntityId, out var loot), "a corpse");
            return loot;
        }

        /// <summary>The choice flag 1088001 as the character database holds it (0: none).</summary>
        private static uint Flag(WildernessRuntimeTestHarness harness)
        {
            using var unit = harness.CreateChar();
            return unit.CharacterFlags.Get(harness.Client.Player.Id).TryGetValue(MarshesRetreadArenaV1.ChoiceFlag, out var value) ? value : 0;
        }

        /// <summary>The choice topics of <paramref name="mission"/> the NPC offers now (as MarshesGunControlTests.Choices).</summary>
        private static (uint, uint)[] Choices(WildernessRuntimeTestHarness harness, Creature npc, uint mission) =>
            Conversation(harness, npc).TryGetValue(ConversationType.ObjectiveChoice, out var value)
                ? ((List<ChoiceObjectives>)value).Where(topic => topic.MissionId == mission)
                    .Select(topic => ((uint)topic.ObjectiveId, (uint)topic.PlayerFlagId)).ToArray()
                : System.Array.Empty<(uint, uint)>();
    }
}
