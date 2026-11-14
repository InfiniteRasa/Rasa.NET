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
    using static PlateauCampResistanceTests;
    using S = AshenScavengerNpcsV1;
    using K = AshenFormalComplaintsNpcsV1;
    using Complaints = AshenFormalComplaintsV1;
    using Human = AshenHumanPerspectiveV1;
    using Bribe = AshenBribeForYourThoughtsV1;
    using Court = AshenCourtMartialArtsV1;
    using Departed = AshenDearlyDepartedV1;
    using Latans = AshenRoboticSalvageV1;
    using Robotic = AshenRoboticAcquisitionsV1;
    using Weapon = AshenWeaponAcquisitionsV1;
    using Heavy = AshenHeavyAcquisitionsV1;
    using FC = AshenFormalComplaintsTests;
    using SC = AshenScavengerTests;
    using F = MarshesClientFloorTests;

    /// <summary>
    /// Ashen Desert set 8, the AFS side of Formal Complaints, played through as a player plays it: Shinichiro Ito's Robotic
    /// Salvage Acquisitions (1267: 15 salvage off the Boneyard's Nitroglazers and 15 off its Hominis Machina), Weapon Salvage
    /// Acquisitions (1269: 15 rifles off Thrax Soldiers and 15 grenade launchers off Thrax Ravagers) and Heavy Salvage
    /// Acquisitions (1268: one salvage off a Desert Stalker and one off a Desert Predator). With them the walks on the client's
    /// floor to Ito and to every place the three send a player.
    ///
    /// Why these tests: the side is for good (TaRapedia 1580: Scavenger or Logistician, only one) and the flags stay at an
    /// abandon, so a gate on Dearly Departed alone, on Formal Complaints alone or on the flag alone would each open the chain
    /// to a player it is not for; the twins share Latan's item classes, so a salvage that drops for the wrong mission, twice,
    /// at the wrong chance or past its count, progress lost at a reload or an abandon, a turn-in that pays twice or on a full
    /// bag, or a place the client's floor does not reach would each break the chain.
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class AshenLogisticianTests
    {
        private const uint WeaponFiller = 3869;            // inventory category 1 (quest-test-pattern)
        private const uint ModuleFiller = 122983;          // Armor Module: Regen Bonus [1] (inventory category 3, as AshenLeadInsTests)
        private const uint ConsumableFiller = 45060;       // Class VII Basic Med Pack: fills the consumables bag
        private const uint MissionItemFiller = 11519;      // as PlateauCampResistanceTests (inventory category 4)

        private static readonly Vector3 Ashoka = AshenLeadInsTests.AshokaWaypoint;
        private static readonly Vector3 ShadowsEdge = AshenLeadInsTests.ShadowsEdgeWaypoint;
        /// <summary>The Ashoka Upper teleporter (613) on Ashoka's lower level (AshenFormalComplaintsTests' start).</summary>
        private static readonly Vector3 AshokaUpper = new(495.27f, 264.5f, -443.25f);

        /// <summary>The Boneyard Thrax pool in a dip (set 6's point) and where the real client walked out of it to (2026-11-09).</summary>
        private const uint BoneyardDip = 531041;
        private static readonly Vector3 TesterOutOfTheDip = new(439.0f, 288.3f, -298.7f), WestOfTheHump = new(439.5f, 288.5f, -271.1f);
        private static readonly uint[] NitroPools = S.NitroglazerPools.Select(pool => pool.Pool).ToArray();
        private static readonly uint[] MachinaPools = S.MachinaPools.Select(pool => pool.Pool).ToArray();
        private static readonly uint[] BoneyardThrax = Departed.Pools.Select(pool => pool.Pool).ToArray();
        private static readonly uint[] RavagerPools = AshenWeaponSalvageV1.RavagerPools;
        private static readonly uint[] StalkerPools = AshenBoneyardNpcsV1.StalkerPools.Select(pool => pool.Pool).ToArray();
        private static readonly uint[] PredatorPools = AshenSheitanHighwayNpcsV1.PredatorPools.Select(pool => pool.Pool).ToArray();

        [TestInitialize]
        public void Clock() => PlateauGiantKillerSquadTests.UseClock();

        [TestCleanup]
        public void Restore() => PlateauGiantKillerSquadTests.RestoreClock();

        /// <summary>
        /// The triage's D1 gate with the chain before it seeded done (The Human Perspective to Dearly Departed), so only Formal
        /// Complaints and the flag stand in the way. Robotic Salvage Acquisitions is not offered before Formal Complaints; nor
        /// after choice 2 while it is open; nor after choice 2 and an abandon (flag 251 stays at 1: a gate without Formal
        /// Complaints fails here), not after a reload either; nor after Formal Complaints is done by choice 1 (251 back to 0, 250
        /// at 1: a gate without the flag fails here), when Latan offers Robotic Salvage Recovery instead.
        /// </summary>
        [TestMethod]
        public void TheRequisitionOrdersAreNotOfferedOffTheAfsSide()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            AshenRuntime.SeedCompleted(harness, Human.MissionId, Bribe.MissionId, Court.MissionId, Departed.MissionId);
            var desert = AshenLeadInsTests.EnterAshen(harness, FC.AshokaHospital);
            var latan = Npc(desert, K.Latan);
            var ito = Npc(desert, K.Ito);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, ito, Robotic.MissionId), "not before Formal Complaints");

            Accept(harness, latan, Complaints.MissionId);
            FC.Choose(harness, latan, Complaints.MissionId, Complaints.Discuss, Complaints.HearYamamoto);
            Assert.AreEqual(1U, FC.Flag(harness, Complaints.YamamotoFlag), "choice 2: flag 251");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, ito, Robotic.MissionId), "not while Formal Complaints is open");
            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Complaints.MissionId));
            harness.Drain();
            Assert.AreEqual(1U, FC.Flag(harness, Complaints.YamamotoFlag), "the flag stays at an abandon");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, ito, Robotic.MissionId), "choice 2, abandon 1254: 1267 not offered");
            AshenRuntime.Reload(harness);
            harness.Drain();
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, ito, Robotic.MissionId), "nor after a reload");

            Accept(harness, latan, Complaints.MissionId);
            FC.Choose(harness, latan, Complaints.MissionId, Complaints.Discuss, Complaints.SideWithLatan);
            FC.Say(harness, latan, Complaints.MissionId, Complaints.LatanSide, Complaints.LatanFlag);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, latan, Complaints.MissionId, 0, Complaints.Experience, Complaints.Credits);
            harness.Drain();
            Assert.AreEqual((1U, 0U), (FC.Flag(harness, Complaints.LatanFlag), FC.Flag(harness, Complaints.YamamotoFlag)), "choice 1: 250, 251 back to 0");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, ito, Robotic.MissionId), "choice 1 completed: 1267 not offered");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, latan, Latans.MissionId), "Latan's side instead");
            AshenRuntime.Reload(harness);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, ito, Robotic.MissionId), "nor after a reload");
        }

        /// <summary>
        /// Formal Complaints done by choice 2: Ito offers The Human Perspective, not Robotic Salvage Acquisitions; nor with the
        /// chain done up to Court Martial Arts (a gate without Dearly Departed fails here). Dearly Departed done too: Ito offers
        /// it (not Latan; Latan's Robotic Salvage Recovery is not offered); Weapon and Heavy Salvage Acquisitions wait for their
        /// turn; a reload keeps the offer.
        /// </summary>
        [TestMethod]
        public void HearingYamamotoFirstOpensTheOrdersAfterDearlyDeparted()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var desert = AshenLeadInsTests.EnterAshen(harness, FC.AshokaHospital);
            var latan = Npc(desert, K.Latan);
            var ito = Npc(desert, K.Ito);
            HearYamamotoFirst(harness, latan);
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, ito, Human.MissionId), "Ito offers The Human Perspective");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, ito, Robotic.MissionId), "not before Dearly Departed");

            AshenRuntime.SeedCompleted(harness, Human.MissionId, Bribe.MissionId, Court.MissionId);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, ito, Robotic.MissionId), "Dearly Departed not done: 1267 not offered");

            AshenRuntime.SeedCompleted(harness, Departed.MissionId);
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, ito, Robotic.MissionId), "Ito offers Robotic Salvage Acquisitions");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, latan, Robotic.MissionId), "not Latan's to give");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, latan, Latans.MissionId), "no Latan side");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, ito, Weapon.MissionId), "Weapon Salvage Acquisitions waits for Robotic");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, ito, Heavy.MissionId), "Heavy Salvage Acquisitions waits for Weapon");
            AshenRuntime.Reload(harness);
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, ito, Robotic.MissionId), "after a reload too");
        }

        /// <summary>
        /// The walks on the client's floor (AshenFixBTests.ClientFloorWalk: every half metre, never more than 1.1 m up over a
        /// metre, nothing of the placed meshes in the way at knee, chest or head height), there and back: Ito from the Ashoka
        /// Settlement hospital on his floor and from the Ashoka Upper teleporter (613) on that floor, and from the Ashoka
        /// Settlement waypoint through the Brann corridor under the dome (fix C, AshenFormalComplaintsTests.AshokaWalk: the
        /// straight line to the market ramp is walled off; the navmesh does not join the two, upstream N18); from the Ashoka
        /// Settlement waypoint the Boneyard's places (the robot pools, the three Thrax pools, the Desert Stalker pools; 531041
        /// climbs 0.99 m in and 1.01 m out over a metre, walked in the real client 2026-11-09); from the Shadow's Edge Post waypoint the Sheitan
        /// Highway's (the Ravager pools and every step of the Desert Predators' beats).
        /// </summary>
        [TestMethod]
        public void ItoAndTheOrdersPlacesAreWalkedToOnTheClientsFloor()
        {
            var dataDir = F.ClientData();
            using var harness = WildernessRuntimeTestHarness.Create();
            var desert = AshenLeadInsTests.EnterAshen(harness);
            var nav = desert.NavMesh;
            using var meshes = new MeshLibrary(dataDir, F.MeshCsv());
            var geometry = MapGeometry.Load(Path.Combine(dataDir, "maps", harness.World.MapInfoEntries.AsNoTracking()
                .Single(map => map.Id == AshenMissionDataV1.MapContextId).MapName), meshes, 1);
            var failures = new List<string>();
            void Both(string name, Vector3 from, string fromName, Vector3 target)
            {
                var point = nav.Nearest(target) ?? target;
                if (AshenFixBTests.ClientFloorWalk(nav, geometry, from, point) is { } there)
                    failures.Add($"{name} from {fromName}: {there}");
                if (AshenFixBTests.ClientFloorWalk(nav, geometry, point, from) is { } back)
                    failures.Add($"{name} back to {fromName}: {back}");
            }

            var itoAt = K.At(K.Ito);
            var ito = nav.Nearest(new Vector3((float)itoAt.X, (float)itoAt.Y, (float)itoAt.Z)).Value;
            Both("Ito", FC.AshokaHospital, "the Ashoka Settlement hospital", ito);
            Both("Ito", AshokaUpper, "the Ashoka Upper teleporter (613)", ito);

            // From the waypoint through the corridor (fix C) and back.
            if (FC.AshokaWalk(nav, geometry, ito, down: true) is { } down)
                failures.Add($"Ito from the Ashoka Settlement waypoint through the corridor: {down}");
            if (FC.AshokaWalk(nav, geometry, ito, down: false) is { } up)
                failures.Add($"Ito back to the Ashoka Settlement waypoint through the corridor: {up}");

            foreach (var (pool, at) in S.NitroglazerPools.Concat(S.MachinaPools))
                Both($"robot pool {pool}", Ashoka, "the Ashoka Settlement waypoint", V(at));
            Assert.HasCount(3, Departed.Pools, "the Boneyard's three Thrax pools, 531041 with them");
            foreach (var pool in Departed.Pools.Where(pool => pool.Pool != BoneyardDip))
                Both($"Thrax pool {pool.Pool}", Ashoka, "the Ashoka Settlement waypoint", V(pool.At));
            // 531041 (set 6's point in a dip): in from the waypoint; out the way the real client walked it (tester 2026-11-09
            // 23:41-23:54: from the pool to (439.0, 288.3, -298.7), then on to the waypoint). The navmesh's own way out crosses
            // the hump at (447.5, -269.1), where a placed mesh stands at knee height; the walk passes west of it, at (439.5,
            // -271.1) (ESTIMATE: the tester's own line is not logged; the corners 2 m round it pass as well).
            var dip = nav.Nearest(V(Departed.Pools.Single(pool => pool.Pool == BoneyardDip).At)).Value;
            var testerOut = nav.Nearest(TesterOutOfTheDip).Value;
            if (AshenFixBTests.ClientFloorWalk(nav, geometry, Ashoka, dip) is { } into)
                failures.Add($"Thrax pool {BoneyardDip} from the Ashoka Settlement waypoint: {into}");
            var west = nav.Nearest(WestOfTheHump).Value;
            if (AshenFixBTests.ClientFloorWalk(nav, geometry, dip, west) is { } outOf)
                failures.Add($"Thrax pool {BoneyardDip} out to (439.5, -271.1): {outOf}");
            if (AshenFixBTests.ClientFloorWalk(nav, geometry, west, testerOut) is { } on)
                failures.Add($"(439.5, -271.1) on to the tester's (439.0, -298.7): {on}");
            if (AshenFixBTests.ClientFloorWalk(nav, geometry, testerOut, Ashoka) is { } home)
                failures.Add($"the tester's (439.0, -298.7) back to the Ashoka Settlement waypoint: {home}");
            foreach (var (pool, at) in AshenBoneyardNpcsV1.StalkerPools)
                Both($"Desert Stalker pool {pool}", Ashoka, "the Ashoka Settlement waypoint", V(at));
            // The Ravager pools where fix E put them, on the camp's edge (AshenFixETests).
            foreach (var pool in RavagerPools)
                Both($"Ravager pool {pool}", ShadowsEdge, "the Shadow's Edge Post waypoint",
                    V(Array.Find(AshenFixEV1.RavagerPools, entry => entry.Pool == pool).Now));
            foreach (var (pool, _, way) in AshenSheitanHighwayNpcsV1.PredatorPools)
                for (var step = 0; step < way.Length; step++)
                    Both($"Desert Predator beat {pool} step {step}", ShadowsEdge, "the Shadow's Edge Post waypoint", V(way[step]));
            Assert.IsEmpty(failures, $"{failures.Count}: " + string.Join("; ", failures));
        }

        /// <summary>
        /// Robotic Salvage Acquisitions: Ito offers it with the four armour modules; no salvage drops before it. Accepted, Ito
        /// hears no objective; two markers on each objective, on Ashen Desert. A Nitroglazer drops one Nitroglazer salvage only and
        /// a Hominis Machina one Machina salvage only, under 75 (75 misses, 74 drops); a death is not a pickup; a full bag leaves it
        /// on the corpse, one slot free and it is picked up. Fifteen of each: each objective completes once, a sixteenth kill drops
        /// none; a reload halfway keeps the count; the mission is completeable once. A full bag waits, a failed save pays nothing
        /// and keeps the salvage; Ito takes all thirty and pays the module chosen, once; then he offers Weapon Salvage
        /// Acquisitions, and Latan offers nothing of hers.
        /// </summary>
        [TestMethod]
        public void RoboticAcquisitionsComeOffTheBoneyardRobots()
        {
            var percentile = 0;
            using var harness = WildernessRuntimeTestHarness.Create(lootRoll: (minimum, maximum) => maximum == 100 ? percentile : minimum);
            harness.LoadAbilities(); // the Machina's revive (as WildernessTwinPillarsTests)
            var desert = AshenLeadInsTests.EnterAshen(harness, FC.AshokaHospital);
            var latan = Npc(desert, K.Latan);
            var ito = Npc(desert, K.Ito);
            HearYamamotoFirst(harness, latan);
            AshenRuntime.SeedCompleted(harness, Human.MissionId, Bribe.MissionId, Court.MissionId, Departed.MissionId);
            Spawn(desert, NitroPools.Concat(MachinaPools).ToArray());

            var offer = WildernessRanjaGorgeTests.Offer(harness, ito, Robotic.MissionId);
            CollectionAssert.AreEqual(new uint[] { 123016, 123026, 123031, 123071 }, offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray(),
                "Armor Module: Resist Electric, Fire, Ice, Virulent [4]");
            Assert.IsFalse(Holds(SC.Kill(harness, desert, S.Nitroglazer, NitroPools), 97465), "no salvage before the mission");

            Accept(harness, ito, Robotic.MissionId);
            Assert.IsEmpty(Topics(harness, ito, Robotic.MissionId), "Ito hears no objective: the salvage counts as it is picked up");
            var objectives = harness.Manager.BuildStatusSnapshot(harness.Client.Player)[Robotic.MissionId].ObjectivesList;
            foreach (var objective in new[] { 1U, 2U })
            {
                var markers = objectives.Single(entry => entry.ObjectiveId == objective).IndicatorList;
                Assert.HasCount(2, markers, $"{objective}: a marker on each of its two pools");
                Assert.IsTrue(markers.All(marker => marker.MapContextId == AshenMissionDataV1.MapContextId), "on Ashen Desert");
            }

            percentile = 75;                                  // the rate written out (ESTIMATE 75 %)
            Assert.IsFalse(Holds(SC.Kill(harness, desert, S.Nitroglazer, NitroPools), 97465), "75 misses");
            percentile = 74;
            var nitro = SC.Kill(harness, desert, S.Nitroglazer, NitroPools);
            Assert.AreEqual((1U, false), (Stack(nitro, 97465), Holds(nitro, 97466)), "74: a Nitroglazer drops one Nitroglazer salvage only");
            Assert.AreEqual(0U, Held(harness, 97465), "a death is not a pickup");
            WildernessAliaBranchesTests.FillCategory(harness, MissionItemFiller, 50);
            PickUp(harness, nitro);
            Assert.AreEqual(0U, Held(harness, 97465), "no room");
            Assert.IsTrue(Holds(nitro, 97465), "still on the corpse");
            WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
            PickUp(harness, nitro);
            var machina = SC.Kill(harness, desert, S.Machina, MachinaPools);
            Assert.AreEqual((1U, false), (Stack(machina, 97466), Holds(machina, 97465)), "a Hominis Machina drops one Machina salvage only");
            WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
            PickUp(harness, machina);
            var packets = harness.Drain().Cast<object>().ToList();
            Assert.AreEqual((1U, 1U), (Held(harness, 97465), Held(harness, 97466)), "picked up once there is room");

            for (var count = 2; count <= 15; count++)
            {
                PickUp(harness, SC.Kill(harness, desert, S.Nitroglazer, NitroPools));
                PickUp(harness, SC.Kill(harness, desert, S.Machina, MachinaPools));
                packets.AddRange(harness.Drain());
                if (count == 7)
                {
                    AshenRuntime.Reload(harness);
                    harness.Drain();
                    Assert.AreEqual((7U, 7U), (Held(harness, 97465), Held(harness, 97466)), "kept at a reload");
                    Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Robotic.MissionId, 1), "seven of fifteen");
                }
            }
            Assert.AreEqual((15U, 15U), (Held(harness, 97465), Held(harness, 97466)), "fifteen of each");
            foreach (var objective in new[] { 1U, 2U })
            {
                Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Robotic.MissionId, objective), $"{objective} done");
                Assert.AreEqual(1, packets.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Robotic.MissionId && packet.ObjectiveId == objective), $"{objective}: ObjectiveCompleted once");
            }
            Assert.AreEqual(1, packets.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Robotic.MissionId && packet.IsCompleteable), "MissionCompleteable once");
            Assert.IsFalse(Holds(SC.Kill(harness, desert, S.Nitroglazer, NitroPools), 97465), "a sixteenth Nitroglazer drops none");
            Assert.IsFalse(Holds(SC.Kill(harness, desert, S.Machina, MachinaPools), 97466), "a sixteenth Machina drops none");

            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, latan.EntityId, Robotic.MissionId, 0), "not Latan's to take");
            FullBagWaits(harness, ito, Robotic.MissionId, 0, ModuleFiller);
            FailedTurnInPaysNothing(harness, ito, Robotic.MissionId, 0);
            Assert.AreEqual((15U, 15U), (Held(harness, 97465), Held(harness, 97466)), "kept at the failed turn-in");
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, ito, Robotic.MissionId, 0, 95000, 6300);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Robotic.MissionId), "MissionRewarded once");
            Assert.AreEqual(1U, Held(harness, 123016), "the Resist Electric module chosen");
            Assert.AreEqual((0U, 0U), (Held(harness, 97465), Held(harness, 97466)), "Ito takes all thirty");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, ito, Robotic.MissionId), "done once");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, ito, Weapon.MissionId), "Ito offers Weapon Salvage Acquisitions");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, latan, Latans.MissionId), "Latan offers nothing of hers");
            Assert.IsFalse(Holds(SC.Kill(harness, desert, S.Nitroglazer, NitroPools), 97465), "after the turn-in a Nitroglazer drops none");
        }

        /// <summary>
        /// Weapon Salvage Acquisitions: a Thrax Soldier drops one rifle only and a Thrax Ravager one grenade launcher only, under
        /// 75 (75 misses, 74 drops); markers on the Boneyard's Thrax pools (rifles) and the highway's Ravager pools (launchers). A
        /// full bag leaves a rifle on the corpse. Five of each, an abandon keeps them; taken again, the five held count and the
        /// rest come; fifteen of each make it completeable once; a reload keeps them; a full bag waits, a failed save pays
        /// nothing; Ito takes them and pays the blade chosen, once; then he offers Heavy Salvage Acquisitions.
        /// </summary>
        [TestMethod]
        public void WeaponAcquisitionsComeOffThraxSoldiersAndRavagers()
        {
            var percentile = 0;
            using var harness = WildernessRuntimeTestHarness.Create(lootRoll: (minimum, maximum) => maximum == 100 ? percentile : minimum);
            AshenRuntime.SeedCompleted(harness, Complaints.MissionId, Robotic.MissionId);
            var desert = AshenLeadInsTests.EnterAshen(harness, Ashoka);
            var ito = Npc(desert, K.Ito);
            var thrax = BoneyardThrax.Concat(RavagerPools).ToArray();
            Spawn(desert, thrax);

            var offer = WildernessRanjaGorgeTests.Offer(harness, ito, Weapon.MissionId);
            CollectionAssert.AreEqual(new uint[] { 12101, 119882, 14452, 15220 }, offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray(),
                "Cryogenic Blade, Area Healing Disc, Sonic Pistol, Pulse Staff");
            Assert.IsFalse(Holds(SC.Kill(harness, desert, 531008, BoneyardThrax), 97469), "no rifle before the mission");
            Accept(harness, ito, Weapon.MissionId);
            Assert.IsEmpty(Topics(harness, ito, Weapon.MissionId), "Ito hears no objective");
            var objectives = harness.Manager.BuildStatusSnapshot(harness.Client.Player)[Weapon.MissionId].ObjectivesList;
            Assert.HasCount(3, objectives.Single(entry => entry.ObjectiveId == 1).IndicatorList, "rifles: the Boneyard's three Thrax pools");
            Assert.HasCount(2, objectives.Single(entry => entry.ObjectiveId == 2).IndicatorList, "launchers: the highway's two Ravager pools");

            percentile = 75;
            Assert.IsFalse(Holds(SC.Kill(harness, desert, 531008, BoneyardThrax), 97469), "75 misses");
            percentile = 74;
            var soldier = SC.Kill(harness, desert, 531008, BoneyardThrax);
            Assert.AreEqual((1U, false), (Stack(soldier, 97469), Holds(soldier, 97470)), "74: a Soldier drops one rifle only");
            Assert.AreEqual(0U, Held(harness, 97469), "a death is not a pickup");
            WildernessAliaBranchesTests.FillCategory(harness, MissionItemFiller, 50);
            PickUp(harness, soldier);
            Assert.AreEqual(0U, Held(harness, 97469), "no room");
            Assert.IsTrue(Holds(soldier, 97469), "still on the corpse");
            WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
            PickUp(harness, soldier);
            var ravager = SC.Kill(harness, desert, 531009, RavagerPools);
            Assert.AreEqual((1U, false), (Stack(ravager, 97470), Holds(ravager, 97469)), "a Ravager drops one launcher only");
            WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
            PickUp(harness, ravager);
            for (var count = 2; count <= 5; count++)
            {
                WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
                PickUp(harness, SC.Kill(harness, desert, 531008, BoneyardThrax));
                WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
                PickUp(harness, SC.Kill(harness, desert, 531009, RavagerPools));
            }
            harness.Drain();
            Assert.AreEqual((5U, 5U), (Held(harness, 97469), Held(harness, 97470)), "five of each");

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Weapon.MissionId));
            harness.Drain();
            Assert.AreEqual((5U, 5U), (Held(harness, 97469), Held(harness, 97470)), "kept at an abandon");
            Assert.IsFalse(Holds(SC.Kill(harness, desert, 531008, BoneyardThrax), 97469), "none while the mission is not held");
            Accept(harness, ito, Weapon.MissionId);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Weapon.MissionId, 1), "five of fifteen held");
            var packets = harness.Drain().Cast<object>().ToList();
            for (var count = 6; count <= 15; count++)
            {
                WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
                var rifle = SC.Kill(harness, desert, 531008, BoneyardThrax);
                Assert.IsTrue(Holds(rifle, 97469), $"rifle {count} dropped");
                PickUp(harness, rifle);
                WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
                PickUp(harness, SC.Kill(harness, desert, 531009, RavagerPools));
                packets.AddRange(harness.Drain());
                Assert.AreEqual(((uint)count, (uint)count), (Held(harness, 97469), Held(harness, 97470)), $"after {count}");
            }
            Assert.AreEqual(1, packets.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Weapon.MissionId && packet.IsCompleteable), "MissionCompleteable once");
            Assert.IsFalse(Holds(SC.Kill(harness, desert, 531008, BoneyardThrax), 97469), "a sixteenth Soldier drops no rifle");
            AshenRuntime.Reload(harness);
            harness.Drain();
            Assert.IsTrue(harness.Client.Player.Missions[Weapon.MissionId].Completeable, "kept at a reload");

            FullBagWaits(harness, ito, Weapon.MissionId, 0, WeaponFiller);
            FailedTurnInPaysNothing(harness, ito, Weapon.MissionId, 0);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, ito, Weapon.MissionId, 0, 95000, 6300);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Weapon.MissionId), "MissionRewarded once");
            Assert.AreEqual(1U, Held(harness, 12101), "the Cryogenic Blade");
            Assert.AreEqual((0U, 0U), (Held(harness, 97469), Held(harness, 97470)), "Ito takes them");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, ito, Heavy.MissionId), "Ito offers Heavy Salvage Acquisitions");
        }

        /// <summary>
        /// Heavy Salvage Acquisitions: a Desert Stalker drops the Stalker salvage and a Desert Predator the Predator salvage, every
        /// kill (100%), one each, only while the mission is open; the markers on the Stalker pools and the Predator beats. A full
        /// bag leaves the Predator salvage on the corpse. An abandon keeps the salvage; taken again, it counts at once. A full bag
        /// waits, a failed save pays nothing; Ito takes both and pays the stack chosen, once; the chain ends.
        /// </summary>
        [TestMethod]
        public void HeavyAcquisitionsComeOffAStalkerAndAPredator()
        {
            using var harness = WildernessRuntimeTestHarness.Create(lootRoll: (minimum, maximum) => maximum == 100 ? 99 : minimum);
            AshenRuntime.SeedCompleted(harness, Complaints.MissionId, Robotic.MissionId, Weapon.MissionId);
            var desert = AshenLeadInsTests.EnterAshen(harness, Ashoka);
            var ito = Npc(desert, K.Ito);
            Spawn(desert, StalkerPools.Concat(PredatorPools).ToArray());

            Assert.IsFalse(Holds(SC.Kill(harness, desert, Heavy.Stalker, StalkerPools), 97467), "none before the mission");
            Accept(harness, ito, Heavy.MissionId);
            var objectives = harness.Manager.BuildStatusSnapshot(harness.Client.Player)[Heavy.MissionId].ObjectivesList;
            Assert.HasCount(2, objectives.Single(entry => entry.ObjectiveId == 1).IndicatorList, "the two Stalker pools");
            Assert.HasCount(2, objectives.Single(entry => entry.ObjectiveId == 2).IndicatorList, "the two Predator beats");

            var stalker = SC.Kill(harness, desert, Heavy.Stalker, StalkerPools);
            Assert.AreEqual((1U, false), (Stack(stalker, 97467), Holds(stalker, 97468)), "99: a Stalker drops its salvage (100%)");
            PickUp(harness, stalker);
            harness.Drain();
            Assert.AreEqual(1U, Held(harness, 97467));
            Assert.IsFalse(Holds(SC.Kill(harness, desert, Heavy.Stalker, StalkerPools), 97467), "one: the next Stalker has none");

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Heavy.MissionId));
            harness.Drain();
            Assert.AreEqual(1U, Held(harness, 97467), "kept at an abandon");
            Accept(harness, ito, Heavy.MissionId);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Heavy.MissionId, 1), "the salvage held counts");

            var predator = SC.Kill(harness, desert, Heavy.Predator, PredatorPools);
            Assert.AreEqual((1U, false), (Stack(predator, 97468), Holds(predator, 97467)), "a Predator drops its salvage");
            WildernessAliaBranchesTests.FillCategory(harness, MissionItemFiller, 50);
            PickUp(harness, predator);
            Assert.AreEqual(0U, Held(harness, 97468), "no room");
            Assert.IsTrue(Holds(predator, 97468), "still on the corpse");
            WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
            PickUp(harness, predator);
            var packets = harness.Drain();
            Assert.AreEqual(1, packets.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Heavy.MissionId && packet.IsCompleteable), "MissionCompleteable once");

            FullBagWaits(harness, ito, Heavy.MissionId, 3, ConsumableFiller);
            FailedTurnInPaysNothing(harness, ito, Heavy.MissionId, 3);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, ito, Heavy.MissionId, 3, 200000, 6300);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Heavy.MissionId), "MissionRewarded once");
            Assert.AreEqual(6U, Held(harness, 45063), "the six Basic Med Packs chosen, no more");
            Assert.AreEqual((0U, 0U), (Held(harness, 97467), Held(harness, 97468)), "Ito takes both");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, ito, Heavy.MissionId), "done once");
        }

        private static uint Held(WildernessRuntimeTestHarness harness, uint template) => WildernessRanjaGorgeTests.Held(harness, template);

        /// <summary>How many of <paramref name="template"/> lie on the corpse (rows times their quantity).</summary>
        private static uint Stack(LootDispenser loot, uint template) =>
            (uint)loot.LootItems.Where(item => item.ItemTemplateId == template).Sum(item => (long)item.ItemQuantity);

        private static Vector3 V((double X, double Y, double Z) at) => new((float)at.X, (float)at.Y, (float)at.Z);

        /// <summary>
        /// Formal Complaints played to its end on the AFS side (choice 2: flag 251). Played, not seeded: the flag is the choice's
        /// own write (AshenRuntime.Reload reads it back from the character database).
        /// </summary>
        private static void HearYamamotoFirst(WildernessRuntimeTestHarness harness, Creature latan)
        {
            Accept(harness, latan, Complaints.MissionId);
            FC.Choose(harness, latan, Complaints.MissionId, Complaints.Discuss, Complaints.HearYamamoto);
            FC.Say(harness, latan, Complaints.MissionId, Complaints.YamamotoSide, Complaints.YamamotoFlag);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, latan, Complaints.MissionId, 3, Complaints.Experience, Complaints.Credits);
            harness.Drain();
            Assert.AreEqual((0U, 1U), (FC.Flag(harness, Complaints.LatanFlag), FC.Flag(harness, Complaints.YamamotoFlag)), "flag 251, 250 at 0");
        }
    }
}
