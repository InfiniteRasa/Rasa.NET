using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Managers;
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
    /// Marshes set 5, the sentry tower of Far West Watch, played through as a player plays it: Sergeant McDonald's Mortar
    /// Madness (1874: three Bane mortars of the eastern bases), Warrior Dikah's Under the Radar (1875: the downed dropship's
    /// black box, to Lt. Kiang), and Kiang's two versions of Under the Radar - Part II (1876: the Bane computer outside the
    /// eastern control point; 1877: Overseer Nelg's troop data).
    ///
    /// Why these tests: McDonald, Dikah, Kiang and Nelg are new on the server, 1876 and 1877 follow 1875 and are offered side
    /// by side, both give the same Bane Troop Data, and either sets the flag set 6 waits for. An NPC off the navmesh, an offer
    /// before its requirement, a turn-in at the wrong NPC, a mortar kill before the accept or of the wrong creature counted, a
    /// giver used with another argument than the client's or used twice, an item given with no room or kept after an abandon,
    /// one version's data counted for the other, a kill counted as a pickup, progress lost at a reload, or a turn-in that pays
    /// twice would each break them. Nelg's data has a 100% chance: no roll is made, so the highest roll still drops it (the
    /// only side of that boundary).
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class MarshesSentryTowerTests
    {
        private const uint Mortars = MarshesMortarMadnessV1.MissionId;
        private const uint Radar = MarshesUnderTheRadarV1.MissionId;
        private const uint Network = MarshesUnderTheRadarPartTwoV1.MissionId;
        private const uint Overseer = MarshesUnderTheRadarNelgV1.MissionId;
        private const uint McDonald = MarshesSentryTowerNpcsV1.McDonald, Dikah = MarshesSentryTowerNpcsV1.Dikah;
        private const uint Kiang = MarshesSentryTowerNpcsV1.Kiang, Nelg = MarshesSentryTowerNpcsV1.Nelg;
        private const uint BaneMortar = MarshesMortarMadnessV1.BaneMortar;
        private const uint PartTwoDone = MarshesUnderTheRadarPartTwoV1.PartTwoDoneFlag;
        private const uint ThraxSoldierPool = 531300, ThraxSoldier = 531045;   // World DB: a Thrax Soldier pool on the Marshes
        private const uint MissionItemFiller = 11519;      // as PlateauCampResistanceTests
        private const uint ConsumableFiller = 45060;       // Class VII Basic Med Pack: fills the consumables bag

        /// <summary>The Research Point waypoint (World DB teleporter 295).</summary>
        private static readonly Vector3 ResearchPointWaypoint = new(-343.45f, 216.15f, -279.6f);
        /// <summary>The tower teleporter at the sentry tower's foot (World DB teleporter 573).</summary>
        private static readonly Vector3 TowerTeleporter = new(-117.6f, 217.3f, -323.9f);
        /// <summary>The deck east of Nelg's post, walked to from the Bane Assertion Camp waypoint (navmesh probe).</summary>
        private static readonly Vector3 NelgDeck = new(176.8f, 224.68f, 41.2f);
        private static readonly Vector3 BaneAssertionCampWaypoint = new(130.77f, 218.3f, -312.8f);

        [TestInitialize]
        public void Clock() => PlateauGiantKillerSquadTests.UseClock();

        [TestCleanup]
        public void Restore() => PlateauGiantKillerSquadTests.RestoreClock();

        [TestMethod]
        public void McDonaldDikahAndKiangStandAtTheirPlaces()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            foreach (var (pool, name, package, at) in new[]
            {
                (McDonald, MarshesSentryTowerNpcsV1.McDonaldName, MarshesSentryTowerNpcsV1.McDonaldPackage, MarshesSentryTowerNpcsV1.McDonaldAt),
                (Dikah, MarshesSentryTowerNpcsV1.DikahName, MarshesSentryTowerNpcsV1.DikahPackage, MarshesNpcsStandStillV1.At(Dikah)),   // to-fix 4
                (Kiang, MarshesSentryTowerNpcsV1.KiangName, MarshesSentryTowerNpcsV1.KiangPackage, MarshesNpcsInTheOpenV1.KiangAt)   // to-fix 22
            })
            {
                var npc = Npc(marshes, pool);
                Assert.AreEqual(name, npc.NameId);
                Assert.AreEqual(package, npc.Npc.NpcPackageId, $"{pool}: his server-only package");
                Assert.AreNotEqual(TargetCategory.Hostile, npc.TargetCategory, $"{pool} is no foe");
                Assert.IsLessThan(0.1f, Vector2.Distance(new((float)at.X, (float)at.Z), new(npc.Position.X, npc.Position.Z)), $"{pool} at his place");
                AssertOnGround(marshes, npc.Position, $"{pool}");
            }
            // All three are walked to (to-fix 22: Kiang at the tower's foot; its room is a navmesh island with no way up).
            foreach (var pool in new[] { McDonald, Dikah, Kiang })
                Assert.IsTrue(marshes.NavMesh.FindPath(ResearchPointWaypoint, RequireNpc(marshes, pool).Position, out var reached) != null && reached,
                    $"{pool} is walked to");
            var kiang = RequireNpc(marshes, Kiang);
            Assert.IsTrue(marshes.NavMesh.FindPath(BaneAssertionCampWaypoint, kiang.Position, out var fromCamp) != null && fromCamp, "Kiang from the camp");
            Assert.IsGreaterThan(7f, Vector2.Distance(new(TowerTeleporter.X, TowerTeleporter.Z), new(kiang.Position.X, kiang.Position.Z)),
                "Kiang 5 m clear of the tower pad's 2 m");
            Assert.IsLessThan(2f, kiang.Position.Y - TowerTeleporter.Y, "at the tower's foot");
            var over = marshes.NavMesh.GroundHeight(kiang.Position + new Vector3(0, 2.5f, 0));
            Assert.IsNotNull(over);
            Assert.IsLessThan(0.5f, over.Value - kiang.Position.Y, "no floor over Kiang");
            Assert.IsLessThan(10f, Vector2.Distance(new(-116, -319), new(kiang.Position.X, kiang.Position.Z)), "Kiang under Ten Ton Hammer's point");
            Assert.IsLessThan(5f, Vector2.Distance(new(-125, -311), new(RequireNpc(marshes, McDonald).Position.X, RequireNpc(marshes, McDonald).Position.Z)),
                "McDonald by Ten Ton Hammer's point");
        }

        [TestMethod]
        public void MortarMadnessTakesThreeBaneMortars()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            var mcdonald = Npc(marshes, McDonald);
            var kiang = Npc(marshes, Kiang);
            var pools = MarshesMortarMadnessV1.WallMortars.Select(mortar => mortar.Pool).ToArray();
            Spawn(marshes, pools.Append(ThraxSoldierPool).ToArray());
            foreach (var pool in pools)
            {
                var mortar = Living(marshes).Single(creature => creature.SpawnPool?.DbId == pool);
                Assert.AreEqual(BaneMortar, mortar.DbId, $"pool {pool}");
                Assert.AreEqual(TargetCategory.Hostile, mortar.TargetCategory, "a mortar is a foe");
            }

            // A mortar killed before the mission is not counted.
            KillMortar(harness, marshes);
            var offer = WildernessRanjaGorgeTests.Offer(harness, mcdonald, Mortars);
            CollectionAssert.AreEqual(new[]
                {
                    MarshesMortarMadnessV1.MotorAssistHelmet, MarshesMortarMadnessV1.StealthLegs,
                    MarshesMortarMadnessV1.HazmatHelmet, MarshesMortarMadnessV1.ReflectiveHelmet
                },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, kiang, Mortars), "not Kiang's to give");
            Accept(harness, mcdonald, Mortars);
            Assert.AreEqual(0U, Count(harness), "the old kill does not count");
            Assert.IsEmpty(Topics(harness, mcdonald, Mortars), "McDonald hears no objective");

            // A Thrax is no mortar. Two mortars, a reload, the third.
            Kill(harness, marshes, ThraxSoldier);
            harness.Drain();
            Assert.AreEqual(0U, Count(harness), "a Thrax is no mortar");
            KillMortar(harness, marshes);
            KillMortar(harness, marshes);
            Assert.AreEqual(2U, Count(harness));
            WildernessRanjaGorgeTests.Reload(harness);
            Assert.AreEqual(2U, Count(harness), "kept at a reload");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Mortars, MarshesMortarMadnessV1.Destroy));
            harness.Drain();
            var last = KillMortar(harness, marshes);
            Assert.AreEqual(1, last.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Mortars), "ObjectiveCompleted once");
            Assert.AreEqual(1, last.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Mortars && packet.IsCompleteable),
                "MissionCompleteable once");
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Mortars, MarshesMortarMadnessV1.Destroy));

            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, kiang.EntityId, Mortars, 1), "not Kiang's to take");
            FullBagWaits(harness, mcdonald, Mortars, 1, MarshesMortarMadnessV1.HazmatHelmet);
            FailedTurnInPaysNothing(harness, mcdonald, Mortars, 1);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, mcdonald, Mortars, 1,
                MarshesMortarMadnessV1.Experience, MarshesMortarMadnessV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Mortars), "MissionRewarded once");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, MarshesMortarMadnessV1.StealthLegs), "the selected legs");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, mcdonald, Mortars), "done once");
        }

        /// <summary>An abandon takes the kills back: offered again, and taken again the count starts at nought.</summary>
        [TestMethod]
        public void MortarMadnessAbandonedStartsOver()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            var mcdonald = Npc(marshes, McDonald);
            Spawn(marshes, MarshesMortarMadnessV1.WallMortars.Select(mortar => mortar.Pool).ToArray());
            Accept(harness, mcdonald, Mortars);
            KillMortar(harness, marshes);
            KillMortar(harness, marshes);
            Assert.AreEqual(2U, Count(harness));

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Mortars));
            harness.Drain();
            Assert.IsFalse(harness.Client.Player.Missions.ContainsKey(Mortars), "gone from the log");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, mcdonald, Mortars), "offered again after an abandon");
            Accept(harness, mcdonald, Mortars);
            Assert.AreEqual(0U, Count(harness), "the old kills do not count");
            KillMortar(harness, marshes);
            Assert.AreEqual(1U, Count(harness));
        }

        [TestMethod]
        public void UnderTheRadarTakesTheBlackBoxToKiang()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            var dikah = Npc(marshes, Dikah);
            var kiang = Npc(marshes, Kiang);
            Assert.IsFalse(marshes.DynamicObjects.Any(obj => obj.SceneMissionId == Radar), "no black box before the mission");

            var offer = WildernessRanjaGorgeTests.Offer(harness, dikah, Radar);
            CollectionAssert.AreEqual(new[]
                {
                    MarshesUnderTheRadarV1.PulseChaingun, MarshesUnderTheRadarV1.Shotgun,
                    MarshesUnderTheRadarV1.VirulentInjectorGun, MarshesUnderTheRadarV1.SonicCannon
                },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, kiang, Radar), "not Kiang's to give");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, kiang, Network), "Part II waits for Under the Radar");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, kiang, Overseer), "the other Part II too");
            Accept(harness, dikah, Radar);
            Assert.IsEmpty(Topics(harness, dikah, Radar), "Dikah hears no objective");
            Assert.IsEmpty(Topics(harness, kiang, Radar), "Kiang hears no objective: the box counts as it is taken");

            var box = BlackBox(harness, marshes);
            WildernessRanjaGorgeTests.Reload(harness);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Radar, MarshesUnderTheRadarV1.Gather), "kept at a reload");
            harness.Drain();
            box = BlackBox(harness, marshes);

            // Another argument than the client's is refused. No room: the giver waits. One slot freed, it gives the box.
            Near(harness, box);
            Use(harness, marshes, box, 3, recovers: false);
            Assert.AreEqual(0U, Boxes(harness), "argument 3 is not the client's");
            WildernessAliaBranchesTests.FillCategory(harness, MissionItemFiller, 50);
            Take(harness, marshes, box, MarshesUnderTheRadarV1.UseArgument);
            Assert.AreEqual(0U, Boxes(harness), "no room");
            Assert.IsTrue(box.IsEnabled, "the giver waits");
            Assert.AreEqual((UseObjectState)MarshesUnderTheRadarV1.ReadyState, box.StateId);
            WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
            harness.Drain();

            Take(harness, marshes, box, MarshesUnderTheRadarV1.UseArgument);
            var taken = harness.Drain();
            Assert.AreEqual(1U, Boxes(harness), "the black box in the bag");
            Assert.AreEqual((UseObjectState)MarshesUnderTheRadarV1.TakenState, box.StateId, "taken");
            Assert.IsFalse(box.IsEnabled, "closed");
            Assert.AreEqual(1, taken.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Radar), "ObjectiveCompleted once");
            Assert.AreEqual(1, taken.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Radar && packet.IsCompleteable),
                "MissionCompleteable once");
            Near(harness, box);
            Use(harness, marshes, box, MarshesUnderTheRadarV1.UseArgument, recovers: false);
            Assert.AreEqual(1U, Boxes(harness), "a taken giver gives no second");

            harness.MoveTo(dikah.Position);
            harness.Drain();
            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, dikah.EntityId, Radar, 0), "not Dikah's to take");
            FullBagWaits(harness, kiang, Radar, 0, MarshesUnderTheRadarV1.Shotgun);
            FailedTurnInPaysNothing(harness, kiang, Radar, 0);
            Assert.AreEqual(1U, Boxes(harness), "kept at the failed turn-in");
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, kiang, Radar, 0,
                MarshesUnderTheRadarV1.Experience, MarshesUnderTheRadarV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Radar), "MissionRewarded once");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, MarshesUnderTheRadarV1.PulseChaingun), "the selected chaingun");
            Assert.AreEqual(0U, Boxes(harness), "Kiang keeps the box");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, dikah, Radar), "done once");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, kiang, Network), "Part II follows at Kiang");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, kiang, Overseer), "both versions of it");
        }

        /// <summary>An abandon takes the box and its giver away; taken again, a new giver stands ready.</summary>
        [TestMethod]
        public void UnderTheRadarAbandonedTakesTheBoxBack()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            var dikah = Npc(marshes, Dikah);
            Accept(harness, dikah, Radar);
            Take(harness, marshes, BlackBox(harness, marshes), MarshesUnderTheRadarV1.UseArgument);
            Assert.AreEqual(1U, Boxes(harness));

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Radar));
            harness.Drain();
            Assert.AreEqual(0U, Boxes(harness), "the box goes with the mission");
            Assert.IsFalse(marshes.DynamicObjects.Any(obj => obj.SceneMissionId == Radar && obj.IsInWorld), "the giver goes with it");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, dikah, Radar), "offered again after an abandon");
            Accept(harness, dikah, Radar);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Radar, MarshesUnderTheRadarV1.Gather));
            Assert.IsTrue(BlackBox(harness, marshes).IsEnabled, "a new giver, ready");
        }

        [TestMethod]
        public void PartTwoHacksTheBaneNetworkOutsideTheControlPoint()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            var kiang = Npc(marshes, Kiang);
            var mcdonald = Npc(marshes, McDonald);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, kiang, Network), "it waits for Under the Radar");
            SeedCompletedMarshes(harness, Radar);
            marshes = EnterMarshes(harness);
            Assert.IsFalse(marshes.DynamicObjects.Any(obj => obj.SceneMissionId == Network), "no computer before the mission");

            var offer = WildernessRanjaGorgeTests.Offer(harness, kiang, Network);
            CollectionAssert.AreEqual(new[]
                {
                    MarshesUnderTheRadarPartTwoV1.BioBoots, MarshesUnderTheRadarPartTwoV1.GravitonBoots,
                    MarshesUnderTheRadarPartTwoV1.StealthBoots, MarshesUnderTheRadarPartTwoV1.MechBoots
                },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, mcdonald, Network), "not McDonald's to give");
            Accept(harness, kiang, Network);
            Assert.IsEmpty(Topics(harness, kiang, Network), "Kiang hears no objective: the data counts as it is taken");
            Assert.IsFalse(harness.Client.Player.PlayerFlags.ContainsKey(PartTwoDone), "no flag yet");

            var computer = Computer(harness, marshes);
            WildernessRanjaGorgeTests.Reload(harness);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Network, MarshesUnderTheRadarPartTwoV1.Engage), "kept at a reload");
            harness.Drain();
            computer = Computer(harness, marshes);

            Near(harness, computer);
            Use(harness, marshes, computer, 3, recovers: false);
            Assert.AreEqual(0U, Data(harness), "argument 3 is not the client's");
            WildernessAliaBranchesTests.FillCategory(harness, MissionItemFiller, 50);
            Take(harness, marshes, computer, MarshesUnderTheRadarPartTwoV1.UseArgument);
            Assert.AreEqual(0U, Data(harness), "no room");
            Assert.IsTrue(computer.IsEnabled, "the computer waits");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Network, MarshesUnderTheRadarPartTwoV1.Engage));
            WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
            harness.Drain();

            Take(harness, marshes, computer, MarshesUnderTheRadarPartTwoV1.UseArgument);
            var hacked = harness.Drain();
            Assert.AreEqual(1U, Data(harness), "the troop data in the bag");
            Assert.IsFalse(computer.IsEnabled, "used");
            Assert.AreEqual(1, hacked.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Network), "ObjectiveCompleted once");
            Assert.AreEqual(1, hacked.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Network && packet.IsCompleteable),
                "MissionCompleteable once");
            Assert.AreEqual(1U, harness.Client.Player.PlayerFlags[PartTwoDone], "Part II done");

            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, mcdonald.EntityId, Network, 2), "not McDonald's to take");
            FullBagWaits(harness, kiang, Network, 2, MarshesUnderTheRadarPartTwoV1.MechBoots);
            FailedTurnInPaysNothing(harness, kiang, Network, 2);
            Assert.AreEqual(1U, Data(harness), "kept at the failed turn-in");
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, kiang, Network, 2,
                MarshesUnderTheRadarPartTwoV1.Experience, MarshesUnderTheRadarPartTwoV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Network), "MissionRewarded once");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, MarshesUnderTheRadarPartTwoV1.StealthBoots), "the selected boots");
            Assert.AreEqual(0U, Data(harness), "Kiang keeps the data");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, kiang, Network), "done once");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, kiang, Overseer), "the other version is still offered");
        }

        /// <summary>An abandon takes the data and the computer away; taken again, a new computer stands ready.</summary>
        [TestMethod]
        public void PartTwoAbandonedTakesTheDataBack()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            SeedCompletedMarshes(harness, Radar);
            var marshes = EnterMarshes(harness);
            var kiang = Npc(marshes, Kiang);
            Accept(harness, kiang, Network);
            Take(harness, marshes, Computer(harness, marshes), MarshesUnderTheRadarPartTwoV1.UseArgument);
            Assert.AreEqual(1U, Data(harness));

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Network));
            harness.Drain();
            Assert.AreEqual(0U, Data(harness), "the data goes with the mission");
            Assert.IsFalse(marshes.DynamicObjects.Any(obj => obj.SceneMissionId == Network && obj.IsInWorld), "the computer goes with it");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, kiang, Network), "offered again after an abandon");
            Accept(harness, kiang, Network);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Network, MarshesUnderTheRadarPartTwoV1.Engage));
            Assert.IsTrue(Computer(harness, marshes).IsEnabled, "a new computer, ready");
        }

        [TestMethod]
        public void PartTwoTakesOverseerNelgsTroopData()
        {
            using var harness = WildernessRuntimeTestHarness.Create(lootRoll: (minimum, maximum) => maximum == 100 ? 99 : minimum);
            var marshes = EnterMarshes(harness);
            var kiang = Npc(marshes, Kiang);
            var mcdonald = Npc(marshes, McDonald);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, kiang, Overseer), "it waits for Under the Radar");
            SeedCompletedMarshes(harness, Radar);
            marshes = EnterMarshes(harness);
            Assert.IsNull(SceneNelg(marshes), "no Nelg before the mission");

            var offer = WildernessRanjaGorgeTests.Offer(harness, kiang, Overseer);
            CollectionAssert.AreEqual(new[]
                {
                    MarshesUnderTheRadarNelgV1.BioBoots, MarshesUnderTheRadarNelgV1.GravitonBoots,
                    MarshesUnderTheRadarNelgV1.StealthBoots, MarshesUnderTheRadarNelgV1.MechBoots
                },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, mcdonald, Overseer), "not McDonald's to give");
            Accept(harness, kiang, Overseer);
            Tick(harness, marshes, 250);
            Assert.IsEmpty(Topics(harness, kiang, Overseer), "Kiang hears no objective: the data counts as it is picked up");

            var nelg = AssertNelg(harness, marshes);
            WildernessRanjaGorgeTests.Reload(harness);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Overseer, MarshesUnderTheRadarNelgV1.Intercept), "kept at a reload");
            harness.Drain();
            Tick(harness, marshes, 250);
            nelg = AssertNelg(harness, marshes);

            // His death is not a pickup. No room: the data stays on the corpse. One slot freed, it is picked up.
            var loot = KillNelg(harness, marshes, nelg);
            Assert.IsTrue(Holds(loot, MarshesUnderTheRadarNelgV1.DataTemplate), "Nelg's troop data, at the highest roll");
            Assert.AreEqual(0U, Data(harness), "a death is not a pickup");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Overseer, MarshesUnderTheRadarNelgV1.Intercept));
            WildernessAliaBranchesTests.FillCategory(harness, MissionItemFiller, 50);
            PickUp(harness, loot);
            Assert.AreEqual(0U, Data(harness), "no room");
            Assert.IsTrue(Holds(loot, MarshesUnderTheRadarNelgV1.DataTemplate), "still on the corpse");
            WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
            harness.Drain();
            PickUp(harness, loot);
            var picked = harness.Drain();
            Assert.AreEqual(1U, Data(harness));
            Assert.AreEqual(1, picked.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Overseer), "ObjectiveCompleted once");
            Assert.AreEqual(1, picked.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Overseer && packet.IsCompleteable),
                "MissionCompleteable once");
            Assert.AreEqual(1U, harness.Client.Player.PlayerFlags[PartTwoDone], "Part II done");

            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, mcdonald.EntityId, Overseer, 3), "not McDonald's to take");
            FullBagWaits(harness, kiang, Overseer, 3, MarshesUnderTheRadarNelgV1.BioBoots);
            FailedTurnInPaysNothing(harness, kiang, Overseer, 3);
            Assert.AreEqual(1U, Data(harness), "kept at the failed turn-in");
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, kiang, Overseer, 3,
                MarshesUnderTheRadarNelgV1.Experience, MarshesUnderTheRadarNelgV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Overseer), "MissionRewarded once");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, MarshesUnderTheRadarNelgV1.MechBoots), "the selected boots");
            Assert.AreEqual(0U, Data(harness), "Kiang takes the data");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, kiang, Overseer), "done once");
        }

        /// <summary>
        /// An abandon takes Nelg away and keeps a picked-up data (a corpse drop is the player's own); taken again, a new Nelg
        /// stands in his post and the data held counts at the accept.
        /// </summary>
        [TestMethod]
        public void PartTwoNelgAbandonedKeepsHisDataAndSetsANewNelg()
        {
            using var harness = WildernessRuntimeTestHarness.Create(lootRoll: (minimum, maximum) => maximum == 100 ? 99 : minimum);
            SeedCompletedMarshes(harness, Radar);
            var marshes = EnterMarshes(harness);
            var kiang = Npc(marshes, Kiang);
            Accept(harness, kiang, Overseer);
            Tick(harness, marshes, 250);
            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Overseer));
            harness.Drain();
            Tick(harness, marshes, 250);
            Assert.IsNull(SceneNelg(marshes), "Nelg goes with the mission");

            Accept(harness, kiang, Overseer);
            Tick(harness, marshes, 250);
            PickUp(harness, KillNelg(harness, marshes, AssertNelg(harness, marshes)));
            harness.Drain();
            Assert.AreEqual(1U, Data(harness));
            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Overseer));
            harness.Drain();
            Assert.AreEqual(1U, Data(harness), "a picked-up data stays the player's (CharacterOwned, Retain)");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, kiang, Overseer), "offered again after an abandon");
            Accept(harness, kiang, Overseer);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Overseer, MarshesUnderTheRadarNelgV1.Intercept),
                "the data held counts at the accept");
        }

        /// <summary>
        /// Both versions at once: the computer's data is 1876's own and does not count for 1877; Nelg's data then completes
        /// 1877; each turn-in takes its own data. Done, neither version is offered again, and the flag stays set.
        /// </summary>
        [TestMethod]
        public void BothVersionsOfPartTwoKeepTheirOwnData()
        {
            using var harness = WildernessRuntimeTestHarness.Create(lootRoll: (minimum, maximum) => maximum == 100 ? 99 : minimum);
            SeedCompletedMarshes(harness, Radar);
            var marshes = EnterMarshes(harness);
            var kiang = Npc(marshes, Kiang);
            Accept(harness, kiang, Network);
            Accept(harness, kiang, Overseer);
            Tick(harness, marshes, 250);

            Take(harness, marshes, Computer(harness, marshes), MarshesUnderTheRadarPartTwoV1.UseArgument);
            harness.Drain();
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Network, MarshesUnderTheRadarPartTwoV1.Engage));
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Overseer, MarshesUnderTheRadarNelgV1.Intercept),
                "the computer's data is not Nelg's");

            PickUp(harness, KillNelg(harness, marshes, AssertNelg(harness, marshes)));
            harness.Drain();
            Assert.AreEqual(2U, Data(harness));
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Overseer, MarshesUnderTheRadarNelgV1.Intercept));

            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, kiang, Network, 0,
                MarshesUnderTheRadarPartTwoV1.Experience, MarshesUnderTheRadarPartTwoV1.Credits);
            harness.Drain();
            Assert.AreEqual(1U, Data(harness), "1876 takes one");
            Assert.IsTrue(harness.Client.Player.Missions[Overseer].Completeable, "1877 still to be turned in");
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, kiang, Overseer, 0,
                MarshesUnderTheRadarNelgV1.Experience, MarshesUnderTheRadarNelgV1.Credits);
            harness.Drain();
            Assert.AreEqual(0U, Data(harness), "1877 takes the other");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, kiang, Network));
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, kiang, Overseer));
            Assert.AreEqual(1U, harness.Client.Player.PlayerFlags[PartTwoDone]);
        }

        /// <summary>
        /// The other order: 1876's computer data taken first, then 1877 accepted. The data held is 1876's own (mission-owned,
        /// AssignmentIssued), so it does not count at 1877's accept: 1877 stays open until Nelg's data is picked up.
        /// </summary>
        [TestMethod]
        public void ComputerDataTakenBeforeNelgsMissionDoesNotCountForIt()
        {
            using var harness = WildernessRuntimeTestHarness.Create(lootRoll: (minimum, maximum) => maximum == 100 ? 99 : minimum);
            SeedCompletedMarshes(harness, Radar);
            var marshes = EnterMarshes(harness);
            var kiang = Npc(marshes, Kiang);
            Accept(harness, kiang, Network);
            Take(harness, marshes, Computer(harness, marshes), MarshesUnderTheRadarPartTwoV1.UseArgument);
            harness.Drain();
            Assert.AreEqual(1U, Data(harness));
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Network, MarshesUnderTheRadarPartTwoV1.Engage));

            Accept(harness, kiang, Overseer);
            Tick(harness, marshes, 250);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Overseer, MarshesUnderTheRadarNelgV1.Intercept),
                "1876's data does not count at 1877's accept");
            Assert.IsFalse(harness.Client.Player.Missions[Overseer].Completeable);
            WildernessRanjaGorgeTests.Reload(harness);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Overseer, MarshesUnderTheRadarNelgV1.Intercept),
                "nor after a reload");
            harness.Drain();
            Tick(harness, marshes, 250);

            PickUp(harness, KillNelg(harness, marshes, AssertNelg(harness, marshes)));
            harness.Drain();
            Assert.AreEqual(2U, Data(harness));
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Overseer, MarshesUnderTheRadarNelgV1.Intercept));
        }

        /// <summary>
        /// Nelg's corpse rots with the data on it (LootDispenserManager.LootableCorpseMs, 120 s): no data. Neither a reload nor
        /// a map made again sets a new Nelg: the run plays its desired actors again, but skips one recorded defeated
        /// (SceneApplication.Reconcile), as a control with a living Nelg shows. Only an abandon and a new accept give a new
        /// Nelg. After a pickup no second Nelg appears until the turn-in.
        /// </summary>
        [TestMethod]
        public void NelgsCorpseRotsAndOnlyANewAcceptSetsHimDownAgain()
        {
            using var harness = WildernessRuntimeTestHarness.Create(lootRoll: (minimum, maximum) => maximum == 100 ? 99 : minimum);
            SeedCompletedMarshes(harness, Radar);
            var marshes = EnterMarshes(harness);
            var kiang = Npc(marshes, Kiang);
            Accept(harness, kiang, Overseer);
            Tick(harness, marshes, 250);
            AssertNelg(harness, marshes);
            // Control: a living Nelg is played again on a map made again.
            marshes = Remade(harness);
            var first = AssertNelg(harness, marshes);
            var loot = KillNelg(harness, marshes, first);
            Assert.IsTrue(Holds(loot, MarshesUnderTheRadarNelgV1.DataTemplate));

            // 125 s pass with nobody at the corpse: it rots, the data with it.
            harness.MoveTo(TowerTeleporter);
            for (var second = 0; second < 125; second++)
                Tick(harness, marshes, 1000);
            Assert.IsNull(SceneNelg(marshes), "no living Nelg");
            PickUp(harness, loot);
            harness.Drain();
            Assert.AreEqual(0U, Data(harness), "a rotted corpse gives no data");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Overseer, MarshesUnderTheRadarNelgV1.Intercept));

            // A reload, and the map made again (a server start): the run is played again (SceneApplication Reconcile).
            WildernessRanjaGorgeTests.Reload(harness);
            harness.Drain();
            Tick(harness, marshes, 250);
            Assert.IsNull(SceneNelg(marshes), "a reload sets no new Nelg: he is recorded defeated");
            marshes = Remade(harness);
            Assert.IsNull(SceneNelg(marshes), "nor a map made again (Reconcile skips a defeated actor)");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Overseer, MarshesUnderTheRadarNelgV1.Intercept),
                "stuck until an abandon");

            // Abandon and accept again: a new Nelg.
            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Overseer));
            harness.Drain();
            Accept(harness, kiang = Npc(marshes, Kiang), Overseer);
            Tick(harness, marshes, 250);
            PickUp(harness, KillNelg(harness, marshes, AssertNelg(harness, marshes)));
            harness.Drain();
            Assert.AreEqual(1U, Data(harness));
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Overseer, MarshesUnderTheRadarNelgV1.Intercept));

            WildernessRanjaGorgeTests.Reload(harness);
            harness.Drain();
            Tick(harness, marshes, 250);
            Assert.IsNull(SceneNelg(marshes), "no second Nelg after a pickup and a reload");
            marshes = Remade(harness);
            Assert.IsNull(SceneNelg(marshes), "nor on a map made again");
            Assert.AreEqual(1U, Data(harness), "the data kept");
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, Npc(marshes, Kiang), Overseer, 0,
                MarshesUnderTheRadarNelgV1.Experience, MarshesUnderTheRadarNelgV1.Credits);
        }

        /// <summary>The Marshes made again, as at a server start: a new channel from the World DB, the player back on it.</summary>
        private static MapChannel Remade(WildernessRuntimeTestHarness harness)
        {
            harness.Manager.Scenes.Detach(harness.Client, harness.Client.Player.MapChannel);
            harness.Maps.MapChannelArray.Remove(MarshesMissionDataV1.MapContextId);
            PlateauInstanceMaps.LoadMap(harness, MarshesMissionDataV1.MapContextId);
            var marshes = EnterMarshes(harness);
            WildernessRanjaGorgeTests.Reload(harness);
            harness.Manager.Scenes.Resume(harness.Client);
            harness.Drain();
            Tick(harness, marshes, 250);
            return marshes;
        }

        /// <summary>Kills a living Bane Mortar as the player; returns the packets of the kill.</summary>
        private static System.Collections.Generic.List<object> KillMortar(WildernessRuntimeTestHarness harness, MapChannel marshes)
        {
            var mortar = Living(marshes).FirstOrDefault(creature => creature.DbId == BaneMortar);
            Assert.IsNotNull(mortar, "a living mortar");
            harness.MoveTo(mortar.Position);
            mortar.Attributes[Attributes.Health].Current = 0;
            harness.Creatures.HandleCreatureKill(marshes, mortar, harness.Client.Player);
            return harness.Drain().Cast<object>().ToList();
        }

        private static uint Count(WildernessRuntimeTestHarness harness) =>
            harness.Client.Player.Missions[Mortars].Objectives[MarshesMortarMadnessV1.Destroy].Counters[0];

        /// <summary>The black box giver: the player's own, of its class, at Ten Ton Hammer's point, on the navmesh, walked to.</summary>
        private static DynamicObject BlackBox(WildernessRuntimeTestHarness harness, MapChannel marshes)
        {
            var giver = marshes.DynamicObjects.Single(obj => obj.SceneMissionId == Radar &&
                obj.SceneActorRole == MarshesUnderTheRadarV1.GiverRole && obj.IsInWorld);
            var at = MarshesUnderTheRadarV1.GiverAt;
            Assert.AreEqual(MarshesUnderTheRadarV1.GiverClass, (uint)giver.EntityClassId);
            Assert.AreEqual(new Vector3(at.X, at.Y, at.Z), giver.Position);
            Assert.AreEqual(harness.Client.Player.Id, giver.SceneOwnerCharacterId, "the player's own");
            Assert.AreEqual((UseObjectState)MarshesUnderTheRadarV1.ReadyState, giver.StateId);
            AssertOnGround(marshes, giver.Position, "the black box");
            Assert.IsLessThan(10f, Vector2.Distance(new(-54.4f, -359.2f), new(giver.Position.X, giver.Position.Z)), "by the wreck");
            Assert.IsTrue(marshes.NavMesh.FindPath(ResearchPointWaypoint, giver.Position, out var complete) != null && complete, "the box is walked to");
            return giver;
        }

        /// <summary>The Bane computer: the player's own, of its class, at Ten Ton Hammer's point, on the navmesh, walked to.</summary>
        private static DynamicObject Computer(WildernessRuntimeTestHarness harness, MapChannel marshes)
        {
            var computer = marshes.DynamicObjects.Single(obj => obj.SceneMissionId == Network &&
                obj.SceneActorRole == MarshesUnderTheRadarPartTwoV1.ComputerRole && obj.IsInWorld);
            var at = MarshesUnderTheRadarPartTwoV1.ComputerAt;
            Assert.AreEqual(MarshesUnderTheRadarPartTwoV1.ComputerClass, (uint)computer.EntityClassId);
            Assert.AreEqual(new Vector3(at.X, at.Y, at.Z), computer.Position);
            Assert.AreEqual(harness.Client.Player.Id, computer.SceneOwnerCharacterId, "the player's own");
            Assert.IsTrue(computer.IsEnabled);
            AssertOnGround(marshes, computer.Position, "the computer");
            Assert.IsTrue(marshes.NavMesh.FindPath(BaneAssertionCampWaypoint, computer.Position, out var complete) != null && complete,
                "the computer is walked to");
            return computer;
        }

        private static Creature SceneNelg(MapChannel marshes) => Living(marshes).SingleOrDefault(creature =>
            creature.DbId == Nelg && creature.SpawnPool?.SceneActorRole == MarshesUnderTheRadarNelgV1.NelgRole);

        /// <summary>The player's Nelg: hostile, in his post at Ten Ton Hammer's point, in reach from the walked-to deck.</summary>
        private static Creature AssertNelg(WildernessRuntimeTestHarness harness, MapChannel marshes)
        {
            var nelg = SceneNelg(marshes);
            Assert.IsNotNull(nelg, "Nelg is set down");
            Assert.AreEqual(MarshesSentryTowerNpcsV1.NelgName, nelg.NameId);
            Assert.AreEqual(TargetCategory.Hostile, nelg.TargetCategory, "Nelg is a foe");
            Assert.IsNull(nelg.Npc, "Nelg does not talk");
            var at = MarshesUnderTheRadarNelgV2.NelgAt;
            Assert.IsLessThan(8f, Vector2.Distance(new(at.X, at.Z), new(nelg.Position.X, nelg.Position.Z)), "in his post (he may step out at the player)");
            AssertOnGround(marshes, new Vector3(at.X, at.Y, at.Z), "Nelg's post");
            Assert.IsTrue(marshes.NavMesh.FindPath(BaneAssertionCampWaypoint, NelgDeck, out var complete) != null && complete, "the deck is walked to");
            Assert.IsLessThan(20f, Vector3.Distance(NelgDeck, new Vector3(at.X, at.Y, at.Z)), "Nelg in reach from the deck");
            // To-fix 25: on his old post (an island) the client walked to 8 m of his corpse, over the 6 m loot reach.
            var walk = marshes.NavMesh.FindPath(BaneAssertionCampWaypoint, new Vector3(at.X, at.Y, at.Z), out var toNelg);
            Assert.IsTrue(walk != null && toNelg, "Nelg is walked to from the camp, over the deck");
            Assert.IsLessThan(6f, Vector3.Distance(walk[^1], new Vector3(at.X, at.Y, at.Z)), "the walk ends within the corpse loot reach");
            return nelg;
        }

        private static LootDispenser KillNelg(WildernessRuntimeTestHarness harness, MapChannel marshes, Creature nelg)
        {
            harness.MoveTo(nelg.Position);
            nelg.Attributes[Attributes.Health].Current = 0;
            harness.Creatures.HandleCreatureKill(marshes, nelg, harness.Client.Player);
            Tick(harness, marshes, 250);
            Assert.IsTrue(marshes.LootDispensers.TryGetValue(nelg.CorpseLootEntityId, out var loot), "Nelg's corpse");
            return loot;
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

        /// <summary>As PoolsRetreadCavesTests.Tick.</summary>
        private static void Tick(WildernessRuntimeTestHarness harness, MapChannel map, long milliseconds)
        {
            harness.UtcNow = harness.UtcNow.AddMilliseconds(milliseconds);
            BehaviorManager.Instance.MapChannelThink(map, System.Math.Min(milliseconds, 1000));
            harness.Manager.Scenes.Tick(map);
            harness.Drain();
        }

        private static uint Boxes(WildernessRuntimeTestHarness harness) =>
            WildernessRanjaGorgeTests.Held(harness, MarshesUnderTheRadarV1.BoxTemplate);

        private static uint Data(WildernessRuntimeTestHarness harness) =>
            WildernessRanjaGorgeTests.Held(harness, MarshesUnderTheRadarPartTwoV1.DataTemplate);
    }
}
