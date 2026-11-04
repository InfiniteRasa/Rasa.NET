using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Managers;
using Rasa.Missions.Content.Marshes;
using Rasa.Packets;
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
    /// Marshes set 10, Retread City, played through as a player plays them: Retread Vincent's Highway Robbery (914: the AFS's
    /// medical supplies stolen from a supply crate at Paludos or Falcon Hold, or the request told to Commander Figgins; Vincent
    /// answers the choice), the two Production Destructions that follow it (916 Vincent's after the theft, 1033 Lieutenant
    /// Holloway's after the telling, with an AFS Mech squadmate: five kiosks, Overseers Vesh and Koorva) and Trapped! (1052:
    /// Retread Samuel's crew found at the Bane Refueling Station and held against three waves of Thrax).
    ///
    /// Why these tests: the choice must close the other way (crates or Figgins' talk), set the flag that picks the follow-up
    /// and give Vincent's answer of that choice only; the kiosks are the player's own destructibles; the mech follows but his
    /// death fails nothing; the defence is timed by the scene (MarshesDefenceScene), its waves come on time, Samuel's death
    /// fails it, and the public Samuel is hidden while his stand-in stands. A reload in the middle, a full bag, a failed save
    /// and an abandon must each leave the run as it was.
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class MarshesRetreadCityTests
    {
        private const uint Robbery = MarshesHighwayRobberyV1.MissionId;
        private const uint ForVincent = MarshesProductionDestructionV1.VincentMissionId;
        private const uint ForHolloway = MarshesProductionDestructionV1.HollowayMissionId;
        private const uint Trapped = MarshesTrappedV1.MissionId;
        private const uint Vincent = MarshesStalkerWoodsNpcsV1.Vincent, Figgins = MarshesRetreadCityNpcsV1.Figgins;
        private const uint Holloway = MarshesRetreadCityNpcsV1.Holloway, Duvall = MarshesStalkerWoodsNpcsV1.Duvall;
        private const uint Vesh = MarshesProductionDestructionV1.Vesh, Koorva = MarshesProductionDestructionV1.Koorva;
        private const uint ConsumableFiller = 45060;       // Class VII Basic Med Pack: fills the consumables bag

        private static readonly Vector3 FalconHoldWaypoint = new(-143.27f, 219.05f, 581.54f);
        private static readonly Vector3 PaludosWaypoint = new(-304.35f, 218.3f, 97.36f);
        private static readonly Vector3 RetreadCityWaypoint = new(-250.45f, 216.8f, -830.56f);
        private static readonly Vector3 BaneAssertionCampWaypoint = new(130.77f, 218.3f, -312.8f);

        [TestInitialize]
        public void Clock() => PlateauGiantKillerSquadTests.UseClock();

        [TestCleanup]
        public void Restore() => PlateauGiantKillerSquadTests.RestoreClock();

        [TestMethod]
        public void FigginsAndHollowayStandAtTheirPlacesAndGiveNothingFirst()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            foreach (var (id, name, package, from) in new[]
            {
                (Figgins, MarshesRetreadCityNpcsV1.FigginsName, MarshesRetreadCityNpcsV1.FigginsPackage, FalconHoldWaypoint),
                (Holloway, MarshesRetreadCityNpcsV1.HollowayName, MarshesRetreadCityNpcsV1.HollowayPackage, RetreadCityWaypoint)
            })
            {
                var npc = Npc(marshes, id);
                Assert.AreEqual((name, package), (npc.NameId, npc.Npc.NpcPackageId), $"{id}: name and package");
                Assert.AreNotEqual(TargetCategory.Hostile, npc.TargetCategory, $"{id} is no foe");
                AssertOnGround(marshes, npc.Position, $"{id}");
                Assert.IsTrue(marshes.NavMesh.FindPath(from, npc.Position, out var walked) != null && walked, $"{id} is walked to");
                foreach (var mission in new[] { Robbery, ForVincent, ForHolloway, Trapped })
                    Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, npc, mission), $"{id} does not give {mission}");
            }
            var vincent = Npc(marshes, Vincent);
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, vincent, Robbery), "Highway Robbery needs nothing");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, vincent, Trapped), "Trapped! needs nothing");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, vincent, ForVincent), "916 waits for 914");
            foreach (var at in MarshesProductionDestructionV1.Kiosks.Append(MarshesTrappedV3.SamuelAt).Concat(MarshesTrappedV3.RetreadsAt)
                .Concat(MarshesHighwayRobberyV1.Crates))
                AssertOnGround(marshes, ToVector(at), $"{at}");
        }

        /// <summary>
        /// 914 stolen: a wrong use argument takes nothing; the Paludos crate gives the supplies and completes 1; Figgins' talk
        /// is failed and the crates go; the flag says "stolen"; Vincent hears only the theft's answer (variant 131), takes the
        /// supplies and pays; then he offers 916 and Holloway no 1033.
        /// </summary>
        [TestMethod]
        public void HighwayRobberyStealsTheSuppliesForVincent()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            var vincent = Npc(marshes, Vincent);
            var figgins = Npc(marshes, Figgins);
            var holloway = Npc(marshes, Holloway);
            var offer = WildernessRanjaGorgeTests.Offer(harness, vincent, Robbery);
            CollectionAssert.AreEqual(new[]
                {
                    MarshesHighwayRobberyV1.AdvancedMedPack, MarshesHighwayRobberyV1.AdrenalineBooster,
                    MarshesHighwayRobberyV1.IncendiaryGrenade, MarshesHighwayRobberyV1.EmpGrenade
                },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, figgins, Robbery), "not Figgins' to give");
            Begin(harness, marshes, vincent, Robbery);
            Assert.AreEqual(MissionObjectiveState.Inactive, Objective(harness, Robbery, MarshesHighwayRobberyV1.Report), "4 hidden");
            Assert.IsEmpty(Topics(harness, vincent, Robbery), "Vincent hears nothing yet");
            CollectionAssert.AreEqual(new[] { MarshesHighwayRobberyV1.RatOut }, Topics(harness, figgins, Robbery), "Figgins can be told");

            var crates = Crates(harness, marshes);
            harness.MoveTo(crates[0].Position + new Vector3(0, 0, 1));
            harness.Drain();
            Use(harness, marshes, crates[0], 1, recovers: false);
            Assert.AreEqual(0U, Supplies(harness), "a wrong argument takes nothing");
            Use(harness, marshes, crates[0], MarshesHighwayRobberyV1.UseArgument, recovers: true);
            var taken = harness.Drain().ToList();
            taken.AddRange(Tick(harness, marshes, 250));
            taken.AddRange(Tick(harness, marshes, 250));
            Assert.AreEqual(1U, Supplies(harness), "the AFS medical supplies");
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Robbery, MarshesHighwayRobberyV1.Steal));
            Assert.AreEqual(1, taken.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Robbery &&
                packet.ObjectiveId == MarshesHighwayRobberyV1.Steal), "ObjectiveCompleted once");
            Assert.AreEqual(MissionObjectiveState.Failed, Objective(harness, Robbery, MarshesHighwayRobberyV1.RatOut), "no telling now");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Robbery, MarshesHighwayRobberyV1.Report));
            Assert.AreEqual(MissionState.Active, harness.Client.Player.Missions[Robbery].State, "an optional way failed: the mission goes on");
            Assert.IsFalse(marshes.DynamicObjects.Any(obj => obj.SceneMissionId == Robbery && obj.IsInWorld), "the crates go");
            Assert.IsEmpty(Topics(harness, figgins, Robbery), "Figgins hears nothing now");
            Assert.AreEqual(MarshesHighwayRobberyV1.StoleFlag, Flag(harness));

            ReloadAndRecover(harness, marshes);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Robbery, MarshesHighwayRobberyV1.Report), "kept at a reload");
            Assert.IsFalse(marshes.DynamicObjects.Any(obj => obj.SceneMissionId == Robbery && obj.IsInWorld), "no crate comes back");
            CollectionAssert.AreEqual(new[] { (MarshesHighwayRobberyV1.Report, MarshesHighwayRobberyV1.StolenVariant) },
                Variants(harness, vincent, Robbery), "only the theft's answer");
            harness.MoveTo(vincent.Position);
            harness.Drain();
            Assert.IsFalse(harness.Manager.CompleteOfferedObjective(harness.Client, vincent.EntityId, Robbery, MarshesHighwayRobberyV1.Report,
                MarshesHighwayRobberyV1.ToldVariant), "not the telling's answer");
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, vincent.EntityId, Robbery, MarshesHighwayRobberyV1.Report,
                MarshesHighwayRobberyV1.StolenVariant));
            var answered = harness.Drain();
            Assert.AreEqual(1, answered.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Robbery &&
                packet.ObjectiveId == MarshesHighwayRobberyV1.Report), "ObjectiveCompleted once");
            Assert.AreEqual(1, answered.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Robbery && packet.IsCompleteable),
                "MissionCompleteable once");
            Assert.AreEqual(0U, Supplies(harness), "Vincent takes the supplies");

            harness.MoveTo(figgins.Position);
            harness.Drain();
            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, figgins.EntityId, Robbery, 0), "not Figgins' to take");
            FullBagWaits(harness, vincent, Robbery, 0, ConsumableFiller);
            FailedTurnInPaysNothing(harness, vincent, Robbery, 0);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, vincent, Robbery, 3,
                MarshesHighwayRobberyV1.Experience, MarshesHighwayRobberyV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Robbery), "MissionRewarded once");
            Assert.AreEqual(MarshesHighwayRobberyV1.EmpGrenades, WildernessRanjaGorgeTests.Held(harness, MarshesHighwayRobberyV1.EmpGrenade),
                "the selected stack");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, vincent, Robbery), "done once");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, vincent, ForVincent), "the theft: Vincent's Production Destruction");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, holloway, ForHolloway), "not Holloway's");
        }

        /// <summary>
        /// 914 told: Figgins hears it, the crates go and the theft is failed; the flag says "told"; Vincent hears only the
        /// telling's answer (variant 130) and pays; then Holloway offers 1033 and Vincent no 916.
        /// </summary>
        [TestMethod]
        public void HighwayRobberyToldToFigginsGivesHollowaysFollowUp()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            var vincent = Npc(marshes, Vincent);
            var figgins = Npc(marshes, Figgins);
            var holloway = Npc(marshes, Holloway);
            Begin(harness, marshes, vincent, Robbery);
            Crates(harness, marshes);

            Talk(harness, figgins, Robbery, MarshesHighwayRobberyV1.RatOut);
            Tick(harness, marshes, 250);
            Tick(harness, marshes, 250);
            Assert.AreEqual(MissionObjectiveState.Failed, Objective(harness, Robbery, MarshesHighwayRobberyV1.Steal), "no theft now");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Robbery, MarshesHighwayRobberyV1.Report));
            Assert.IsFalse(marshes.DynamicObjects.Any(obj => obj.SceneMissionId == Robbery && obj.IsInWorld), "the crates go");
            Assert.AreEqual(MarshesHighwayRobberyV1.ToldFlag, Flag(harness));
            Assert.AreEqual(0U, Supplies(harness));

            CollectionAssert.AreEqual(new[] { (MarshesHighwayRobberyV1.Report, MarshesHighwayRobberyV1.ToldVariant) },
                Variants(harness, vincent, Robbery), "only the telling's answer");
            harness.MoveTo(vincent.Position);
            harness.Drain();
            Assert.IsFalse(harness.Manager.CompleteOfferedObjective(harness.Client, vincent.EntityId, Robbery, MarshesHighwayRobberyV1.Report,
                MarshesHighwayRobberyV1.StolenVariant), "not the theft's answer");
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, vincent.EntityId, Robbery, MarshesHighwayRobberyV1.Report,
                MarshesHighwayRobberyV1.ToldVariant));
            Assert.AreEqual(1, harness.Drain().OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Robbery && packet.IsCompleteable),
                "MissionCompleteable once");
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, vincent, Robbery, 0,
                MarshesHighwayRobberyV1.Experience, MarshesHighwayRobberyV1.Credits);
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, holloway, ForHolloway), "the telling: Holloway's Production Destruction");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, vincent, ForVincent), "not Vincent's");
        }

        /// <summary>The supplies taken, 914 abandoned: they go, the flag stays unset by the run's own, Vincent offers it again with both crates.</summary>
        [TestMethod]
        public void HighwayRobberyAbandonedTakesTheSuppliesBack()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            var vincent = Npc(marshes, Vincent);
            Begin(harness, marshes, vincent, Robbery);
            var crates = Crates(harness, marshes);
            harness.MoveTo(crates[1].Position + new Vector3(0, 0, 1));
            harness.Drain();
            Use(harness, marshes, crates[1], MarshesHighwayRobberyV1.UseArgument, recovers: true);
            harness.Drain();
            Tick(harness, marshes, 250);
            Assert.AreEqual(1U, Supplies(harness), "from the Falcon Hold crate");
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Robbery, MarshesHighwayRobberyV1.Steal));

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Robbery));
            harness.Drain();
            Tick(harness, marshes, 250);
            Assert.AreEqual(0U, Supplies(harness), "the supplies go with the mission");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, vincent, Robbery), "offered again");
            Begin(harness, marshes, vincent, Robbery);
            Crates(harness, marshes);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Robbery, MarshesHighwayRobberyV1.Steal), "from the start");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Robbery, MarshesHighwayRobberyV1.RatOut));
        }

        /// <summary>
        /// 916: five kiosks (the player's own, 500 hit points each, a reload between) and the two Overseers (any order, a kill
        /// before the mission not counted); MissionCompleteable once; Vincent pays.
        /// </summary>
        [TestMethod]
        public void ProductionDestructionForVincentDestroysTheKiosksAndTheOverseers()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            ChoiceMade(harness, MarshesHighwayRobberyV1.StoleFlag);
            var marshes = EnterMarshes(harness);
            var vincent = Npc(marshes, Vincent);
            var holloway = Npc(marshes, Holloway);
            Spawn(marshes, Vesh, Koorva);
            foreach (var boss in new[] { Vesh, Koorva })
                AssertOnGround(marshes, Living(marshes).Single(creature => creature.DbId == boss).Position, $"overseer {boss}");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, holloway, ForHolloway), "the theft: not Holloway's");
            var offer = WildernessRanjaGorgeTests.Offer(harness, vincent, ForVincent);
            CollectionAssert.AreEqual(MarshesProductionDestructionV1.VincentRewards,
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Kill(harness, marshes, Vesh);
            harness.Drain();
            Respawn(marshes, Vesh);
            Begin(harness, marshes, vincent, ForVincent);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, ForVincent, MarshesProductionDestructionV1.KillVesh),
                "the old kill does not count");

            var log = new List<PythonPacket>();
            DestroyAll(harness, marshes, ForVincent, log);
            Kill(harness, marshes, Koorva);
            log.AddRange(harness.Drain());
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, ForVincent, MarshesProductionDestructionV1.KillKoorva));
            Assert.IsFalse(harness.Client.Player.Missions[ForVincent].Completeable, "Vesh is left");
            Kill(harness, marshes, Vesh);
            log.AddRange(harness.Drain());
            Assert.AreEqual(1, log.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == ForVincent && packet.IsCompleteable),
                "MissionCompleteable once");

            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, holloway.EntityId, ForVincent, 1), "not Holloway's to take");
            FullBagWaits(harness, vincent, ForVincent, 1, MarshesProductionDestructionV1.VincentRewards[0]);
            FailedTurnInPaysNothing(harness, vincent, ForVincent, 1);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, vincent, ForVincent, 1,
                MarshesProductionDestructionV1.Experience, MarshesProductionDestructionV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == ForVincent), "MissionRewarded once");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, vincent, ForVincent), "done once");
        }

        /// <summary>
        /// 1033: the mech comes up by Holloway and follows (kept at a reload); the kiosks and Overseers as 916's; at the turn-in
        /// the mech goes.
        /// </summary>
        [TestMethod]
        public void ProductionDestructionForHollowayBringsTheMechAlong()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            ChoiceMade(harness, MarshesHighwayRobberyV1.ToldFlag);
            var marshes = EnterMarshes(harness);
            var vincent = Npc(marshes, Vincent);
            var holloway = Npc(marshes, Holloway);
            Spawn(marshes, Vesh, Koorva);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, vincent, ForVincent), "the telling: not Vincent's");
            var offer = WildernessRanjaGorgeTests.Offer(harness, holloway, ForHolloway);
            CollectionAssert.AreEqual(MarshesProductionDestructionV1.HollowayRewards,
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Begin(harness, marshes, holloway, ForHolloway);

            var mech = Follower(harness, marshes, ForHolloway, MarshesProductionDestructionV1.MechRole);
            Assert.AreEqual(MarshesRetreadCityNpcsV1.Mech, mech.DbId);
            Assert.AreNotEqual(TargetCategory.Hostile, mech.TargetCategory);
            Assert.IsLessThan(3f, Vector3.Distance(mech.Position, ToVector(MarshesProductionDestructionV1.MechAt)), "beside Holloway");
            AssertOnGround(marshes, mech.Position, "the mech");
            ReloadAndRecover(harness, marshes);
            mech = Follower(harness, marshes, ForHolloway, MarshesProductionDestructionV1.MechRole);

            var log = new List<PythonPacket>();
            DestroyAll(harness, marshes, ForHolloway, log);
            Kill(harness, marshes, Vesh);
            Kill(harness, marshes, Koorva);
            log.AddRange(harness.Drain());
            Assert.AreEqual(1, log.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == ForHolloway && packet.IsCompleteable),
                "MissionCompleteable once");
            Assert.IsNotNull(SceneCreature(harness, marshes, ForHolloway, MarshesProductionDestructionV1.MechRole), "the mech stays till the turn-in");

            FullBagWaits(harness, holloway, ForHolloway, 2, MarshesProductionDestructionV1.HollowayRewards[0]);
            FailedTurnInPaysNothing(harness, holloway, ForHolloway, 2);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, holloway, ForHolloway, 2,
                MarshesProductionDestructionV1.Experience, MarshesProductionDestructionV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == ForHolloway), "MissionRewarded once");
            Tick(harness, marshes, 250);
            Tick(harness, marshes, 250);
            Assert.IsNull(SceneCreature(harness, marshes, ForHolloway, MarshesProductionDestructionV1.MechRole), "the mech goes");
            Assert.IsFalse(Living(marshes).Contains(mech));
        }

        /// <summary>The mech killed: nothing fails ("you are on your own"); a kiosk still counts; abandoned, the kiosks go; offered again with a new mech.</summary>
        [TestMethod]
        public void ProductionDestructionGoesOnWithoutTheMechAndCanBeAbandoned()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            ChoiceMade(harness, MarshesHighwayRobberyV1.ToldFlag);
            var marshes = EnterMarshes(harness);
            var holloway = Npc(marshes, Holloway);
            Begin(harness, marshes, holloway, ForHolloway);
            var dead = Die(harness, marshes, Follower(harness, marshes, ForHolloway, MarshesProductionDestructionV1.MechRole));
            Assert.IsEmpty(dead.OfType<ObjectiveFailedPacket>().Where(packet => packet.MissionId == ForHolloway), "nothing fails");
            Assert.AreEqual(MissionState.Active, harness.Client.Player.Missions[ForHolloway].State);
            var kiosks = Kiosks(harness, marshes, ForHolloway);
            Shoot(harness, marshes, kiosks[0], (int)MarshesProductionDestructionV1.KioskHitPoints);
            Tick(harness, marshes, 250);
            Kiosks(harness, marshes, ForHolloway, down: 1);
            Assert.AreEqual(1U, harness.Client.Player.Missions[ForHolloway].Objectives[MarshesProductionDestructionV1.Destroy].Counters[0]);

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, ForHolloway));
            harness.Drain();
            Tick(harness, marshes, 250);
            Tick(harness, marshes, 250);
            Assert.IsFalse(marshes.DynamicObjects.Any(obj => obj.SceneMissionId == ForHolloway && obj.IsInWorld), "the kiosks go");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, holloway, ForHolloway), "offered again");
            Begin(harness, marshes, holloway, ForHolloway);
            Assert.IsNotNull(Follower(harness, marshes, ForHolloway, MarshesProductionDestructionV1.MechRole), "a new mech");
            Kiosks(harness, marshes, ForHolloway);
            Assert.AreEqual(0U, harness.Client.Player.Missions[ForHolloway].Objectives[MarshesProductionDestructionV1.Destroy].Counters[0],
                "the old kiosk does not count");
        }

        /// <summary>
        /// 1052: Samuel's stand-in and three Retreads stand at the station (the public Samuel hidden from the player); his talk
        /// starts the defence; the waves come at 5 s, 75 s and 150 s (a reload between, a Retread lost changing nothing); at
        /// 240 s 2 completes once, the crew and the Thrax go, the public Samuel is back; Vincent pays.
        /// </summary>
        [TestMethod]
        public void TrappedHoldsSamuelsCrewThroughThreeWaves()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            var vincent = Npc(marshes, Vincent);
            var duvall = Npc(marshes, Duvall);
            var publicSamuel = Npc(marshes, MarshesTrappedV1.PublicSamuelPool);
            Assert.IsFalse(HiddenCreatures.IsHiddenFrom(publicSamuel, harness.Client), "the public Samuel before the mission");
            var offer = WildernessRanjaGorgeTests.Offer(harness, vincent, Trapped);
            CollectionAssert.AreEqual(MarshesTrappedV1.Rewards, offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Begin(harness, marshes, vincent, Trapped);
            Assert.AreEqual(MissionObjectiveState.Inactive, Objective(harness, Trapped, MarshesTrappedV1.Defend), "2 hidden");
            // To-fix 10: no crew while no player is near (nothing at the station can kill it), the public Samuel there.
            for (var second = 0; second < 120; second++)
                Tick(harness, marshes, 1000);
            Assert.IsNull(SceneCreature(harness, marshes, Trapped, MarshesDefenceScene.Leader), "no stand-in before the player comes near");
            Assert.IsNull(SceneCreature(harness, marshes, Trapped, MarshesDefenceScene.Ward(2)), "no Retread before");
            Assert.IsFalse(HiddenCreatures.IsHiddenFrom(publicSamuel, harness.Client), "the public Samuel until then");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Trapped, MarshesTrappedV1.Locate), "1 still open");
            Approach(harness, marshes);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Trapped, MarshesTrappedV2.Near), "the hidden approach");

            var samuel = SceneCreature(harness, marshes, Trapped, MarshesDefenceScene.Leader);
            Assert.IsNotNull(samuel, "the stand-in stands once the player is near");
            Assert.AreEqual((MarshesRetreadCityNpcsV1.SamuelStandIn, MarshesRetreadCityNpcsV1.SamuelName, MarshesRetreadCityNpcsV1.SamuelPackage),
                (samuel.DbId, samuel.NameId, samuel.Npc?.NpcPackageId));
            Assert.IsLessThan(0.5f, Vector3.Distance(samuel.Position, ToVector(MarshesTrappedV3.SamuelAt)), "at Samuel's place");
            Assert.IsTrue(marshes.NavMesh.FindPath(BaneAssertionCampWaypoint, samuel.Position, out var walked) != null && walked, "walked to");
            Assert.IsTrue(HiddenCreatures.IsHiddenFrom(publicSamuel, harness.Client), "the public Samuel hidden");
            var crew = Enumerable.Range(2, MarshesTrappedV3.RetreadsAt.Length)
                .Select(member => SceneCreature(harness, marshes, Trapped, MarshesDefenceScene.Ward(member))).ToArray();
            foreach (var retread in crew)
            {
                Assert.IsNotNull(retread);
                Assert.AreEqual(MarshesRetreadCityNpcsV1.Retread, retread.DbId);
                Assert.AreEqual(9005U, retread.NameId, "the client's name 'Retread' (creaturenamelanguage 9005), not 'Human' (to-fix 29)");
                Assert.AreNotEqual(TargetCategory.Hostile, retread.TargetCategory);
                Assert.AreEqual(4000, (int)retread.Attributes[Attributes.Health].NormalMax, "the escort's hit points");
                AssertOnGround(marshes, retread.Position, "a Retread");
            }
            Assert.IsFalse(Wave(harness, marshes, 1).Any(), "no Thrax before the talk");
            Assert.IsEmpty(Topics(harness, duvall, Trapped), "Duvall hears nothing");

            Talk(harness, samuel, Trapped, MarshesTrappedV1.Locate);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Trapped, MarshesTrappedV1.Defend));
            Tick(harness, marshes, 250);
            var log = new List<PythonPacket>();
            for (var wave = 1; wave <= MarshesTrappedSmallerWaveV1.Waves.Length; wave++)
            {
                var (after, members) = MarshesTrappedSmallerWaveV1.Waves[wave - 1];
                var before = after - (wave == 1 ? 1000U : 5000U);
                AdvanceTo(harness, marshes, before, log);
                Assert.IsFalse(Wave(harness, marshes, wave).Any(), $"wave {wave} not before {before} ms");
                AdvanceTo(harness, marshes, after + 1000, log);
                var thrax = Wave(harness, marshes, wave).ToArray();
                Assert.HasCount(members.Length, thrax, $"wave {wave}");
                for (var member = 0; member < thrax.Length; member++)
                {
                    Assert.AreEqual(members[member].Creature, thrax[member].DbId);
                    Assert.AreEqual(TargetCategory.Hostile, thrax[member].TargetCategory, "a foe");
                    Assert.IsTrue(thrax[member].SceneAttackPastLeash, $"wave {wave} member {member + 1} is sent at the crew");
                    AssertOnGround(marshes, thrax[member].Position, $"wave {wave} member {member + 1}");
                    Assert.IsTrue(marshes.NavMesh.FindPath(thrax[member].Position, samuel.Position, out var reaches) != null && reaches,
                        $"wave {wave} member {member + 1} reaches the crew");
                }
                if (wave == 1)
                {
                    Die(harness, marshes, crew[0]);
                    Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Trapped, MarshesTrappedV1.Defend), "a Retread lost");
                    ReloadAndRecover(harness, marshes);
                    Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Trapped, MarshesTrappedV1.Defend), "kept at a reload");
                    Assert.IsNotNull(SceneCreature(harness, marshes, Trapped, MarshesDefenceScene.Leader), "Samuel after the reload");
                    Assert.IsNull(SceneCreature(harness, marshes, Trapped, MarshesDefenceScene.Ward(2)), "the dead Retread stays dead");
                }
            }
            AdvanceTo(harness, marshes, MarshesTrappedV1.HoldMilliseconds - 5000, log);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Trapped, MarshesTrappedV1.Defend), "not before the hold");
            AdvanceTo(harness, marshes, MarshesTrappedV1.HoldMilliseconds + 2000, log);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Trapped, MarshesTrappedV1.Defend), "held out");
            Assert.AreEqual(1, log.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Trapped &&
                packet.ObjectiveId == MarshesTrappedV1.Defend), "ObjectiveCompleted once");
            Assert.AreEqual(1, log.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Trapped && packet.IsCompleteable),
                "MissionCompleteable once");
            Assert.IsNull(SceneCreature(harness, marshes, Trapped, MarshesDefenceScene.Leader), "the crew slips away");
            for (var wave = 1; wave <= MarshesTrappedSmallerWaveV1.Waves.Length; wave++)
                Assert.IsFalse(Wave(harness, marshes, wave).Any(), $"wave {wave} goes");
            Assert.IsFalse(HiddenCreatures.IsHiddenFrom(publicSamuel, harness.Client), "the public Samuel is back");

            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, duvall.EntityId, Trapped, 1), "not Duvall's to take");
            FullBagWaits(harness, vincent, Trapped, 1, MarshesTrappedV1.Rewards[0]);
            FailedTurnInPaysNothing(harness, vincent, Trapped, 1);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, vincent, Trapped, 1, MarshesTrappedV1.Experience, MarshesTrappedV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Trapped), "MissionRewarded once");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, vincent, Trapped), "done once");
        }

        /// <summary>
        /// To-fix 29: the waves go for the Retreads, not for Samuel, while a Retread stands; with the three down they go for
        /// him. Samuel has three times the escort's hit points.
        /// </summary>
        [TestMethod]
        public void TrappedWavesSpareSamuelWhileARetreadStands()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            var vincent = Npc(marshes, Vincent);
            Begin(harness, marshes, vincent, Trapped);
            Approach(harness, marshes);
            var samuel = SceneCreature(harness, marshes, Trapped, MarshesDefenceScene.Leader);
            Assert.AreEqual((int)MarshesRetreadCityNpcsV1.SamuelHealth, (int)samuel.Attributes[Attributes.Health].NormalMax, "Samuel's hit points");
            Assert.AreEqual(12000U, MarshesRetreadCityNpcsV1.SamuelHealth);
            var crew = Enumerable.Range(2, MarshesTrappedV3.RetreadsAt.Length)
                .Select(member => SceneCreature(harness, marshes, Trapped, MarshesDefenceScene.Ward(member))).ToArray();
            Talk(harness, samuel, Trapped, MarshesTrappedV1.Locate);
            Tick(harness, marshes, 250);
            var log = new List<PythonPacket>();
            static ulong Target(Creature creature) => creature.Controller.CurrentAction == BehaviorManager.BehaviorActionFighting
                ? creature.Controller.ActionFighting.TargetEntityId : 0;
            for (var wave = 1; wave <= MarshesTrappedSmallerWaveV1.Waves.Length; wave++)
            {
                AdvanceTo(harness, marshes, MarshesTrappedSmallerWaveV1.Waves[wave - 1].After + 1000, log);
                var thrax = Wave(harness, marshes, wave).ToArray();
                Assert.HasCount(MarshesTrappedSmallerWaveV1.Waves[wave - 1].Members.Length, thrax, $"wave {wave}");
                for (var member = 0; member < thrax.Length; member++)
                {
                    Assert.AreNotEqual(samuel.EntityId, Target(thrax[member]), $"wave {wave} member {member + 1} spares Samuel");
                    Assert.IsTrue(crew.Any(retread => retread.EntityId == Target(thrax[member])), $"wave {wave} member {member + 1} at a Retread");
                }
            }
            foreach (var retread in crew)
                Die(harness, marshes, retread);
            Tick(harness, marshes, 1000);
            var left = Enumerable.Range(1, MarshesTrappedSmallerWaveV1.Waves.Length).SelectMany(wave => Wave(harness, marshes, wave)).ToArray();
            Assert.IsNotEmpty(left);
            foreach (var thrax in left)
                Assert.AreEqual(samuel.EntityId, Target(thrax), "the crew down: at Samuel");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Trapped, MarshesTrappedV1.Defend), "the Retreads' deaths change nothing");
        }

        /// <summary>Samuel killed before his talk fails 1, during the defence 2; abandoned, the crew goes and the public Samuel is back.</summary>
        [TestMethod]
        public void TrappedFailsWithSamuelDeadAndCanBeAbandoned()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = EnterMarshes(harness);
            var vincent = Npc(marshes, Vincent);
            var publicSamuel = Npc(marshes, MarshesTrappedV1.PublicSamuelPool);
            Begin(harness, marshes, vincent, Trapped);
            Approach(harness, marshes);
            var failed = Die(harness, marshes, SceneCreature(harness, marshes, Trapped, MarshesDefenceScene.Leader));
            Assert.AreEqual(MissionObjectiveState.Failed, Objective(harness, Trapped, MarshesTrappedV1.Locate), "lost before he is found");
            Assert.AreEqual(MissionState.Failed, harness.Client.Player.Missions[Trapped].State);
            Assert.AreEqual(1, failed.OfType<ObjectiveFailedPacket>().Count(packet => packet.MissionId == Trapped), "ObjectiveFailed once");
            Assert.IsNull(SceneCreature(harness, marshes, Trapped, MarshesDefenceScene.Ward(2)), "the crew goes");

            Assert.IsTrue(harness.Manager.TryClear(harness.Client, Trapped), "the failed mission cleared");
            Tick(harness, marshes, 250);
            Begin(harness, marshes, vincent, Trapped);
            Approach(harness, marshes);
            Talk(harness, SceneCreature(harness, marshes, Trapped, MarshesDefenceScene.Leader), Trapped, MarshesTrappedV1.Locate);
            var log = new List<PythonPacket>();
            AdvanceTo(harness, marshes, MarshesTrappedSmallerWaveV1.Waves[0].After + 1000, log);
            Assert.IsTrue(Wave(harness, marshes, 1).Any(), "the first wave");
            Die(harness, marshes, SceneCreature(harness, marshes, Trapped, MarshesDefenceScene.Leader));
            Assert.AreEqual(MissionObjectiveState.Failed, Objective(harness, Trapped, MarshesTrappedV1.Defend), "lost in the defence");
            Assert.AreEqual(MissionState.Failed, harness.Client.Player.Missions[Trapped].State);
            Assert.IsFalse(Wave(harness, marshes, 1).Any(), "the Thrax go");
            AdvanceTo(harness, marshes, MarshesTrappedV1.HoldMilliseconds + 2000, log);
            Assert.AreEqual(MissionObjectiveState.Failed, Objective(harness, Trapped, MarshesTrappedV1.Defend), "the hold's end changes nothing");

            Assert.IsTrue(harness.Manager.TryClear(harness.Client, Trapped), "the failed mission cleared");
            Tick(harness, marshes, 250);
            Begin(harness, marshes, vincent, Trapped);
            Approach(harness, marshes);
            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Trapped), "abandoned with the crew waiting");
            harness.Drain();
            Tick(harness, marshes, 250);
            Tick(harness, marshes, 250);
            Assert.IsNull(SceneCreature(harness, marshes, Trapped, MarshesDefenceScene.Leader), "the crew goes");
            Assert.IsFalse(HiddenCreatures.IsHiddenFrom(publicSamuel, harness.Client), "the public Samuel is back");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, vincent, Trapped), "offered again");
        }

        /// <summary>The player 30 m west of Samuel, inside 1052's approach (40 m): the crew stands.</summary>
        private static void Approach(WildernessRuntimeTestHarness harness, MapChannel marshes)
        {
            MarshesPreoDasTests.Enter(harness, ToVector(MarshesTrappedV3.SamuelAt) + new Vector3(-30, 0, 0));
            Tick(harness, marshes, 250);
            Tick(harness, marshes, 250);
        }

        /// <summary>914 done with this ending (history and flag), as PartTwoIsDone sets Under the Radar's.</summary>
        private static void ChoiceMade(WildernessRuntimeTestHarness harness, uint value)
        {
            SeedCompletedMarshes(harness, Robbery);
            using (var unit = harness.CreateChar())
                unit.ExecuteTransaction(() => unit.CharacterFlags.Set(harness.Client.Player.Id, MarshesHighwayRobberyV1.ChoiceFlag, value));
            harness.Client.Player.PlayerFlags[MarshesHighwayRobberyV1.ChoiceFlag] = value;
            WildernessRanjaGorgeTests.Reload(harness);
        }

        /// <summary>The supply crates: the player's own, of their class and name, ready, at their places, walked to from their waypoints.</summary>
        private static DynamicObject[] Crates(WildernessRuntimeTestHarness harness, MapChannel marshes)
        {
            var crates = marshes.DynamicObjects.Where(obj => obj.SceneMissionId == Robbery && obj.IsInWorld)
                .OrderBy(obj => obj.SceneActorRole).ToArray();
            Assert.HasCount(MarshesHighwayRobberyV1.Crates.Length, crates);
            var from = new[] { PaludosWaypoint, FalconHoldWaypoint };
            for (var index = 0; index < crates.Length; index++)
            {
                var crate = crates[index];
                var at = MarshesHighwayRobberyV1.Crates[index];
                Assert.AreEqual(MarshesHighwayRobberyV1.CrateRole(index), crate.SceneActorRole);
                Assert.AreEqual(MarshesHighwayRobberyV1.CrateClass, (uint)crate.EntityClassId);
                Assert.AreEqual(MarshesHighwayRobberyV1.CrateName, crate.NameOverrideId, "AFS Supply Crate");
                Assert.AreEqual(ToVector(at), crate.Position);
                Assert.AreEqual(harness.Client.Player.Id, crate.SceneOwnerCharacterId, "the player's own");
                Assert.AreEqual(MarshesHighwayRobberyV1.ReadyState, (uint)crate.StateId);
                Assert.IsTrue(crate.IsEnabled);
                AssertOnGround(marshes, crate.Position, crate.SceneActorRole);
                Assert.IsTrue(marshes.NavMesh.FindPath(from[index], crate.Position, out var complete) != null && complete,
                    $"{crate.SceneActorRole} is walked to");
            }
            return crates;
        }

        /// <summary>The kiosks of <paramref name="mission"/>: the player's own destructibles, the first <paramref name="down"/> destroyed.</summary>
        private static DynamicObject[] Kiosks(WildernessRuntimeTestHarness harness, MapChannel marshes, uint mission, int down = 0)
        {
            var kiosks = Enumerable.Range(0, MarshesProductionDestructionV1.Kiosks.Length).Select(index => marshes.DynamicObjects.Single(obj =>
                obj.SceneMissionId == mission && obj.SceneActorRole == MarshesProductionDestructionV1.KioskRole(index) && obj.IsInWorld)).ToArray();
            for (var index = 0; index < kiosks.Length; index++)
            {
                var kiosk = kiosks[index];
                Assert.AreEqual(MarshesProductionDestructionV1.KioskClass, (uint)kiosk.EntityClassId, $"kiosk {index + 1}");
                Assert.AreEqual(MarshesProductionDestructionV1.KioskName, kiosk.NameOverrideId, "Production Kiosk");
                Assert.AreEqual(ToVector(MarshesProductionDestructionV1.Kiosks[index]), kiosk.Position);
                Assert.AreEqual(harness.Client.Player.Id, kiosk.SceneOwnerCharacterId, "the player's own");
                Assert.AreEqual(index < down ? 0U : MarshesProductionDestructionV1.KioskHitPoints, kiosk.CurrentHitPoints, $"kiosk {index + 1} hit points");
                Assert.AreEqual(index < down ? MarshesProductionDestructionV1.DestroyedState : MarshesProductionDestructionV1.IntactState,
                    (uint)kiosk.StateId, $"kiosk {index + 1} state");
                AssertOnGround(marshes, kiosk.Position, $"kiosk {index + 1}");
                Assert.IsTrue(marshes.NavMesh.FindPath(BaneAssertionCampWaypoint, kiosk.Position, out var complete) != null && complete,
                    $"kiosk {index + 1} is walked to");
                Assert.IsLessThan(MarshesProductionDestructionV1.KioskRadius, Vector2.Distance(new(kiosk.Position.X, kiosk.Position.Z),
                    new((float)MarshesProductionDestructionV1.KiosksCentre.X, (float)MarshesProductionDestructionV1.KiosksCentre.Z)), "in the marker");
            }
            return kiosks;
        }

        /// <summary>The five kiosks shot down in turn (a reload after the second, a dead one shot again not counted); objective 1 done once.</summary>
        private static void DestroyAll(WildernessRuntimeTestHarness harness, MapChannel marshes, uint mission, List<PythonPacket> log)
        {
            var kiosks = Kiosks(harness, marshes, mission);
            for (var index = 0; index < kiosks.Length; index++)
            {
                Shoot(harness, marshes, kiosks[index], (int)MarshesProductionDestructionV1.KioskHitPoints);
                log.AddRange(harness.Drain());
                log.AddRange(Tick(harness, marshes, 250));
                Assert.AreEqual((uint)index + 1, harness.Client.Player.Missions[mission].Objectives[MarshesProductionDestructionV1.Destroy].Counters[0],
                    $"kiosk {index + 1} counted");
                if (index == 1)
                {
                    Shoot(harness, marshes, kiosks[index], 100);
                    log.AddRange(Tick(harness, marshes, 250));
                    Assert.AreEqual(2U, harness.Client.Player.Missions[mission].Objectives[MarshesProductionDestructionV1.Destroy].Counters[0], "once each");
                    ReloadAndRecover(harness, marshes);
                    kiosks = Kiosks(harness, marshes, mission, down: 2);
                }
            }
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, mission, MarshesProductionDestructionV1.Destroy));
            Assert.AreEqual(1, log.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == mission &&
                packet.ObjectiveId == MarshesProductionDestructionV1.Destroy), "ObjectiveCompleted once");
        }

        /// <summary>A weapon hit of <paramref name="damage"/> on the object, as MarshesHarvestersTests.Shoot.</summary>
        private static void Shoot(WildernessRuntimeTestHarness harness, MapChannel marshes, DynamicObject obj, int damage)
        {
            harness.MoveTo(obj.Position + new Vector3(0, 0, 1));
            harness.Drain();
            var action = new ActionData(harness.Client.Player, ActionId.WeaponAttack, 133, 0) { TargetId = obj.EntityId };
            MissileManager.Instance.MissileLaunch(marshes, action, damage);
            MissileManager.Instance.DoWork(marshes, 1000);
        }

        /// <summary>Wave <paramref name="wave"/>'s living Thrax of this run, in member order.</summary>
        private static IEnumerable<Creature> Wave(WildernessRuntimeTestHarness harness, MapChannel marshes, int wave) =>
            Enumerable.Range(1, 8).Select(member => SceneCreature(harness, marshes, Trapped, MarshesDefenceScene.WaveRole(wave, member)))
                .Where(creature => creature != null);

        /// <summary>The clock on to <paramref name="milliseconds"/> after the talk (counted from the first call), a second at a time.</summary>
        private void AdvanceTo(WildernessRuntimeTestHarness harness, MapChannel marshes, uint milliseconds, List<PythonPacket> log)
        {
            _talkedAt ??= harness.UtcNow;
            while ((harness.UtcNow - _talkedAt.Value).TotalMilliseconds < milliseconds)
            {
                // The Thrax are held where they come in: only the scene's clock is under test, not the fight.
                foreach (var creature in Enumerable.Range(1, MarshesTrappedSmallerWaveV1.Waves.Length).SelectMany(wave => Wave(harness, marshes, wave)))
                    creature.Attributes[Attributes.Health].Current = creature.Attributes[Attributes.Health].NormalMax;
                log.AddRange(Tick(harness, marshes, 1000));
            }
        }

        private System.DateTime? _talkedAt;

        private static uint Supplies(WildernessRuntimeTestHarness harness) =>
            WildernessRanjaGorgeTests.Held(harness, MarshesHighwayRobberyV1.SuppliesTemplate);

        /// <summary>The choice flag 914001 as the character database holds it (0: none).</summary>
        private static uint Flag(WildernessRuntimeTestHarness harness)
        {
            using var unit = harness.CreateChar();
            return unit.CharacterFlags.Get(harness.Client.Player.Id).TryGetValue(MarshesHighwayRobberyV1.ChoiceFlag, out var value) ? value : 0;
        }

        /// <summary>The hand-in topics of <paramref name="mission"/> the NPC offers now, with their variants.</summary>
        private static (uint, uint)[] Variants(WildernessRuntimeTestHarness harness, Creature npc, uint mission) =>
            Conversation(harness, npc).TryGetValue(ConversationType.ObjectiveComplete, out var value)
                ? ((List<CompleteableObjectives>)value).Where(topic => topic.MissionId == mission)
                    .Select(topic => ((uint)topic.ObjectiveId, (uint)topic.PlayerFlagId)).ToArray()
                : System.Array.Empty<(uint, uint)>();
    }
}
