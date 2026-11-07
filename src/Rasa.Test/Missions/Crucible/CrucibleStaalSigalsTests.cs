using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Managers;
using Rasa.Packets.Mission.Server;
using Rasa.Services.Preloader.Missions.Crucible;
using Rasa.Structures;
using Rasa.Test.Missions.Marshes;
using Rasa.Test.Missions.Plateau;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions.Crucible
{
    using static MarshesFalconHoldTests;
    using static MarshesPaludosTests;
    using static PlateauCampResistanceTests;
    using C = CrucibleOutpostIntrepidTests;
    using N = CrucibleOutpostIntrepidNpcsV1;
    using G = CrucibleGrupaChiefsNpcsV1;
    using W = CrucibleWardensAndLashersNpcsV1;
    using S = CrucibleMinefieldAndSalvageNpcsV1;
    using V = CrucibleStaalSigalsNpcsV1;
    using A = CrucibleArchitectsOfDestructionV1;
    using O = CrucibleOutOfSightV1;
    using I = CrucibleIrreconcilableDifferencesV1;

    /// <summary>
    /// Crucible set 5, the Staal Sigals, played through as a player plays it: Viddia Sigal Nema's Architects Of Destruction
    /// (1202: twenty Reconstructor Bots by the Chaukas Robotics Facility), Top Khana Sigal Pico's Out Of Sight (1208: six
    /// Optical Processors from the new Shield Drones north of Khulago Base) and Trista's Irreconcilable Differences (1185:
    /// Lohen's challenge, Preceptor Molitor's necklace, Lohen again, then Trista).
    ///
    /// Why these tests: Nema, Pico, Trista, the Shield Drones and Molitor are new on the server, and the Staal floor is holed
    /// and split on the navmesh (NOTES-crucible-staal-access.md); 1202 counts kills of a creature class only; 1208 drops at a
    /// chance from one creature; 1185 opens its objectives one by one, drops the necklace only while objective 2 is open, and
    /// gives the reward at another NPC than the talks. An NPC or a pool off the navmesh or off the walk routes, an offer before
    /// its prerequisite or at the wrong NPC, a kill of another creature counted, a drop on the wrong side of its chance, before
    /// its objective or from the wrong creature, a kill taken for a pickup, progress lost at a reload or kept after an abandon,
    /// an item kept from a turn-in, or a turn-in that pays twice would each break them.
    ///
    /// The map: Ligo Crucible (1993), loaded as CrucibleOutpostIntrepidTests.EnterCrucible does (no upstream test does this
    /// for the Crucible). No mission of set 5 has a scene creature (so no reload in the middle of a scene) or a scene object.
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class CrucibleStaalSigalsTests
    {
        private const uint LastMen = CrucibleLastMenStandingV1.MissionId;
        private const uint Fort = CrucibleFortCalledIntrepidV1.MissionId;
        private const uint Wire = CrucibleWireV1.MissionId;
        private const uint Boss = CrucibleBossOfAllBossesV1.MissionId;
        private const uint Guns = CrucibleSmokingGunsV1.MissionId;
        private const uint Wardens = CrucibleGameWardensV1.MissionId;
        private const uint Respect = CrucibleEarningRespectV1.MissionId;
        private const uint Architects = A.MissionId;
        private const uint Sight = O.MissionId;
        private const uint Differences = I.MissionId;
        private const uint MissionItemFiller = 11519;      // as PlateauCampResistanceTests (inventory category 4)
        private const uint ConsumableFiller = 45060;       // Class VII Basic Med Pack: fills the consumables bag
        private const uint WeaponFiller = 3869;            // inventory category 1 (quest-test-pattern)
        private const uint ArmorFiller = 12748;            // Armor_T3_Bio_V01_CMN_Gloves_30_to_34 (inventory category 1)

        /// <summary>The waypoint of the Awol Camp (World DB teleporter 192), and the Prometheus Outpost control point's.</summary>
        private static readonly Vector3 AwolWaypoint = new(-945.54f, 309.45f, 244.47f);
        private static readonly Vector3 PrometheusWaypoint = new(435f, 168.4f, 407f);
        private static readonly Vector3 LowerPad = new((float)G.LowerPadAt.X, (float)G.LowerPadAt.Y, (float)G.LowerPadAt.Z);
        private static readonly Vector3 UpperPad = new((float)G.UpperPadAt.X, (float)G.UpperPadAt.Y, (float)G.UpperPadAt.Z);

        private static readonly uint[] DronePools = V.DronePools.Select(pool => pool.Pool).ToArray();
        private static readonly uint[] ReconstructorPools = A.Pools.Select(pool => pool.Pool).ToArray();
        private static readonly uint[] WardenPools = W.WardenPools.Select(pool => pool.Pool).ToArray();

        [TestInitialize]
        public void Clock() => PlateauGiantKillerSquadTests.UseClock();

        [TestCleanup]
        public void Restore() => PlateauGiantKillerSquadTests.RestoreClock();

        /// <summary>
        /// Nema, Pico and Trista stand on the navmesh with the client's names, friendly, 4 m from their Grupa, walked to from the
        /// Staal routes (NOTES-crucible-staal-access.md: Nema and Pico from the lower office teleporter and their Grupa, Trista
        /// from the upper one and Lohen); three Shield Drones come out of each pool north of Khulago Base, three Reconstructor
        /// Bots out of each new pool, and Molitor with three Thrax Soldiers and a Grenadier out of his, hostile, of their
        /// classes, levels and hit points, on the navmesh, walked to from the Prometheus Outpost, Labbna Quarter and Awol Camp
        /// waypoints.
        /// </summary>
        [TestMethod]
        public void TheSigalsTristaTheDronesTheBotsAndMolitorStandAtTheirPlaces()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var crucible = C.EnterCrucible(harness);
            foreach (var (id, name, grupa, from) in new[]
            {
                (V.Nema, V.NemaName, N.Donal, new[] { LowerPad, C.InfirmaryWaypoint }),
                (V.Pico, V.PicoName, N.Munit, new[] { LowerPad, C.StaalWaypoint }),
                (V.Trista, V.TristaName, G.Lohen, new[] { UpperPad })
            })
            {
                var npc = Npc(crucible, id);
                var chief = Npc(crucible, grupa);
                Assert.AreEqual(name, npc.NameId);
                Assert.AreEqual(id, npc.Npc.NpcPackageId, "a server-only package");
                Assert.AreNotEqual(TargetCategory.Hostile, npc.TargetCategory);
                var at = V.At(id);
                Assert.IsLessThan(0.1f, Vector2.Distance(new((float)at.X, (float)at.Z), new(npc.Position.X, npc.Position.Z)), $"{id} at its place");
                Assert.AreEqual(4f, Vector2.Distance(new(npc.Position.X, npc.Position.Z), new(chief.Position.X, chief.Position.Z)), 0.6f,
                    $"{id} next to {grupa}");
                AssertOnGround(crucible, npc.Position, $"{id}");
                foreach (var start in from.Append(chief.Position))
                    Assert.IsTrue(crucible.NavMesh.FindPath(crucible.NavMesh.Nearest(start).Value, npc.Position, out var walked) != null && walked,
                        $"{id} is walked to from {start}");
            }

            foreach (var (pools, creature, cls, level, health, count, from) in new[]
            {
                (V.DronePools, V.ShieldDrone, V.ShieldDroneClass, V.ShieldDroneLevel, V.ShieldDroneHitPoints, V.DronesPerPool, PrometheusWaypoint),
                (V.ReconstructorPools, S.Reconstructor, S.ReconstructorClass, S.ReconstructorLevel, S.ReconstructorHitPoints,
                    S.ReconstructorsPerPool, C.LabbnaWaypoint),
                (new[] { (Pool: V.MolitorPool, At: V.MolitorAt) }, V.Molitor, 10504U, V.MolitorLevel, V.MolitorHitPoints, 1U, AwolWaypoint)
            })
            {
                Spawn(crucible, pools.Select(pool => pool.Pool).ToArray());
                foreach (var (pool, at) in pools)
                {
                    var place = new Vector3((float)at.X, (float)at.Y, (float)at.Z);
                    AssertOnGround(crucible, place, $"pool {pool}");
                    Assert.IsTrue(crucible.NavMesh.FindPath(crucible.NavMesh.Nearest(from).Value, place, out var reached) != null && reached,
                        $"pool {pool} is walked to");
                    var here = Alive(crucible).Where(candidate => candidate.SpawnPool?.DbId == pool && candidate.DbId == creature).Distinct().ToArray();
                    Assert.HasCount((int)count, here, $"pool {pool}");
                    foreach (var one in here)
                    {
                        Assert.AreEqual((level, TargetCategory.Hostile, cls), (one.Level, one.TargetCategory, (uint)one.EntityClass), $"pool {pool}");
                        Assert.AreEqual((int)health, (int)one.Attributes[Attributes.Health].NormalMax, $"pool {pool}: its hit points");
                    }
                }
            }
            // Molitor's pack: three Crucible Thrax Soldiers and a Thrax Grenadier with him.
            var pack = Alive(crucible).Where(candidate => candidate.SpawnPool?.DbId == V.MolitorPool).Distinct().ToArray();
            Assert.AreEqual((1, 3, 1), (pack.Count(one => one.DbId == V.Molitor), pack.Count(one => one.DbId == V.ThraxSoldier),
                pack.Count(one => one.DbId == V.ThraxGrenadier)), "Molitor travels with a pack of his kind");
            Assert.IsTrue(pack.All(one => one.TargetCategory == TargetCategory.Hostile));
        }

        /// <summary>
        /// Architects Of Destruction after Game Wardens: Nema offers it (only she); a Reconstructor Bot destroyed before it does
        /// not count, nor a Warden Bot; the client gets a marker on each of the four Reconstructor pools; twenty Reconstructor
        /// Bots from all four pools with a reload halfway complete it once; Nema pays with the item chosen, once. An abandon
        /// halfway starts the count again.
        /// </summary>
        [TestMethod]
        public void ArchitectsOfDestructionDestroysTwentyReconstructorBotsForNema()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            C.SeedCompleted(harness, LastMen, Fort, Wire, Boss);
            var crucible = C.EnterCrucible(harness, C.InfirmaryWaypoint);
            var nema = Npc(crucible, V.Nema);
            var donal = Npc(crucible, N.Donal);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, nema, Architects), "it waits for Game Wardens");
            C.SeedCompleted(harness, Wardens);
            crucible = C.EnterCrucible(harness, C.InfirmaryWaypoint);
            Spawn(crucible, ReconstructorPools.Concat(WardenPools).ToArray());

            var offer = WildernessRanjaGorgeTests.Offer(harness, nema, Architects);
            CollectionAssert.AreEqual(new[] { A.FilamentBlade, A.CryogenicInjectorGun, A.PulseCannon, A.PulsePistol },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Assert.IsEmpty(offer.FixedReward.FixedItems, "no fixed item");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, donal, Architects), "not Donal's to give");
            Kill(harness, crucible, S.Reconstructor);
            Accept(harness, nema, Architects);
            Assert.IsEmpty(Topics(harness, nema, Architects), "Nema hears no objective");
            var markers = harness.Manager.BuildStatusSnapshot(harness.Client.Player)[Architects].ObjectivesList.Single().IndicatorList;
            CollectionAssert.AreEquivalent(A.Pools.Select(pool => ((float)pool.At.X, (float)pool.At.Z)).ToArray(),
                markers.Select(marker => (marker.Position.X, marker.Position.Z)).ToArray(), "a marker on each Reconstructor pool");
            Assert.IsTrue(markers.All(marker => marker.MapContextId == CrucibleMissionDataV1.MapContextId));

            Kill(harness, crucible, W.WardenBot);
            for (var bot = 0; bot < 4; bot++)
                KillReconstructor(harness, crucible);
            Assert.AreEqual(4U, Count(harness, Architects), "the bot before the mission and the Warden Bot do not count");

            // An abandon takes the count; taken again, it starts at none.
            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Architects));
            harness.Drain();
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, nema, Architects), "offered again after an abandon");
            Accept(harness, nema, Architects);
            Assert.AreEqual(0U, Count(harness, Architects), "the count starts again");

            var completed = 0;
            var completeable = 0;
            var pools = new HashSet<uint>();
            for (var bot = 1; bot <= A.Bots; bot++)
            {
                if (bot == 11)
                {
                    WildernessRanjaGorgeTests.Reload(harness);
                    Assert.AreEqual(10U, Count(harness, Architects), "kept at a reload");
                    harness.Drain();
                }
                Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Architects, A.Destroy), $"open before bot {bot}");
                var (drained, pool) = KillReconstructor(harness, crucible);
                pools.Add(pool);
                completed += drained.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Architects);
                completeable += drained.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Architects && packet.IsCompleteable);
            }
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Architects, A.Destroy), "twenty");
            Assert.AreEqual((1, 1), (completed, completeable), "ObjectiveCompleted once, MissionCompleteable once");
            CollectionAssert.AreEquivalent(ReconstructorPools, pools.ToArray(), "the bots of all four pools count");

            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, donal.EntityId, Architects, 0), "not Donal's to take");
            FullBagWaits(harness, nema, Architects, 3, WeaponFiller);
            FailedTurnInPaysNothing(harness, nema, Architects, 3);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, nema, Architects, 3, A.Experience, A.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Architects), "MissionRewarded once");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, A.PulsePistol), "the pistol chosen");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, nema, Architects), "done once");
        }

        /// <summary>
        /// Out Of Sight after Smoking Guns: Pico offers it (only he); no processor drops before it; a processor drops from a
        /// Shield Drone under 50 only, not from a Reconstructor Bot, and a death is not a pickup; the count kept at a reload; the
        /// sixth completes it once; Pico takes the processors and pays with the item chosen.
        /// </summary>
        [TestMethod]
        public void OutOfSightTakesSixOpticalProcessorsToPico()
        {
            var percentile = 0;
            using var harness = WildernessRuntimeTestHarness.Create(lootRoll: (minimum, maximum) => maximum == 100 ? percentile : minimum);
            C.SeedCompleted(harness, LastMen, Fort, Wire, Boss);
            var crucible = C.EnterCrucible(harness, C.StaalWaypoint);
            var pico = Npc(crucible, V.Pico);
            var munit = Npc(crucible, N.Munit);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, pico, Sight), "it waits for Smoking Guns");
            C.SeedCompleted(harness, Guns);
            crucible = C.EnterCrucible(harness, C.StaalWaypoint);
            Spawn(crucible, DronePools.Concat(ReconstructorPools).ToArray());

            var offer = WildernessRanjaGorgeTests.Offer(harness, pico, Sight);
            CollectionAssert.AreEqual(new[] { O.MechGloves, O.StealthVest, O.HazmatGloves, O.MotorAssistGloves },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, munit, Sight), "not Munit's to give");
            Assert.IsFalse(Holds(Kill(harness, crucible, V.ShieldDrone), O.ProcessorTemplate), "no processor before the mission");
            Accept(harness, pico, Sight);
            Assert.IsEmpty(Topics(harness, pico, Sight), "Pico hears no objective");
            var markers = harness.Manager.BuildStatusSnapshot(harness.Client.Player)[Sight].ObjectivesList.Single().IndicatorList;
            CollectionAssert.AreEquivalent(V.DronePools.Select(pool => ((float)pool.At.X, (float)pool.At.Z)).ToArray(),
                markers.Select(marker => (marker.Position.X, marker.Position.Z)).ToArray(), "a marker on each drone pool");

            percentile = 0;
            Assert.IsFalse(Holds(Kill(harness, crucible, S.Reconstructor), O.ProcessorTemplate), "a Reconstructor Bot has no processor");
            percentile = O.ProcessorChance;
            Assert.IsFalse(Holds(Kill(harness, crucible, V.ShieldDrone), O.ProcessorTemplate), "50 misses");
            percentile = O.ProcessorChance - 1;
            var processor = Kill(harness, crucible, V.ShieldDrone);
            Assert.IsTrue(Holds(processor, O.ProcessorTemplate), "49 drops a processor");
            Assert.AreEqual(0U, Processors(harness), "a death is not a pickup");
            PickUp(harness, processor);
            Assert.AreEqual(1U, Processors(harness));
            harness.Drain();

            var completed = 0;
            var completeable = 0;
            while (Processors(harness) < O.ProcessorCount)
            {
                if (Processors(harness) == 3)
                {
                    WildernessRanjaGorgeTests.Reload(harness);
                    Assert.AreEqual(3U, Processors(harness), "kept at a reload");
                    Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Sight, O.Processors));
                    harness.Drain();
                }
                if (!Alive(crucible).Any(candidate => candidate.DbId == V.ShieldDrone && candidate.Npc == null))
                    Respawn(crucible, DronePools);
                var loot = Kill(harness, crucible, V.ShieldDrone);
                Assert.IsTrue(Holds(loot, O.ProcessorTemplate), "49 drops");
                PickUp(harness, loot);
                var drained = harness.Drain();
                completed += drained.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Sight);
                completeable += drained.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Sight && packet.IsCompleteable);
            }
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Sight, O.Processors), "six");
            Assert.AreEqual((1, 1), (completed, completeable), "ObjectiveCompleted once, MissionCompleteable once");

            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, munit.EntityId, Sight, 1), "not Munit's to take");
            FullBagWaits(harness, pico, Sight, 1, ArmorFiller);
            FailedTurnInPaysNothing(harness, pico, Sight, 1);
            Assert.AreEqual(O.ProcessorCount, Processors(harness), "kept at the failed turn-in");
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, pico, Sight, 1, O.Experience, O.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Sight), "MissionRewarded once");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, O.StealthVest), "the vest chosen");
            Assert.AreEqual(0U, Processors(harness), "Pico takes the processors");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, pico, Sight), "done once");
        }

        /// <summary>A processor stays on the corpse while the bag is full; an abandon keeps the processors, and taken again they count.</summary>
        [TestMethod]
        public void TheProcessorsWaitForRoomAndOutOfSightCanBeAbandoned()
        {
            using var harness = WildernessRuntimeTestHarness.Create(lootRoll: (minimum, maximum) => maximum == 100 ? 0 : minimum);
            C.SeedCompleted(harness, LastMen, Fort, Wire, Boss, Guns);
            var crucible = C.EnterCrucible(harness, C.StaalWaypoint);
            var pico = Npc(crucible, V.Pico);
            Spawn(crucible, DronePools);
            Accept(harness, pico, Sight);
            WildernessAliaBranchesTests.FillCategory(harness, MissionItemFiller, 50);

            var loot = Kill(harness, crucible, V.ShieldDrone);
            PickUp(harness, loot);
            Assert.AreEqual(0U, Processors(harness), "no room");
            Assert.IsTrue(Holds(loot, O.ProcessorTemplate), "still on the corpse");
            WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
            PickUp(harness, loot);
            Assert.AreEqual(1U, Processors(harness), "picked up once there is room");
            harness.Drain();

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Sight));
            harness.Drain();
            Assert.AreEqual(1U, Processors(harness), "the processor is kept");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, pico, Sight), "offered again after an abandon");
            Accept(harness, pico, Sight);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Sight, O.Processors), "one of six");
            harness.Drain();
            for (var kill = 0; kill < 5; kill++)
            {
                if (!Alive(crucible).Any(candidate => candidate.DbId == V.ShieldDrone && candidate.Npc == null))
                    Respawn(crucible, DronePools);
                PickUp(harness, Kill(harness, crucible, V.ShieldDrone));
            }
            Assert.AreEqual(O.ProcessorCount, Processors(harness), "the held processor and five more");
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Sight, O.Processors), "six processors");
        }

        /// <summary>
        /// Irreconcilable Differences after Earning Respect: Trista offers it (only she, not Lohen). Lohen hears only the
        /// challenge first; Molitor killed before it drops no necklace (objective 2 is not open), nor does a Thrax Soldier of his
        /// pack after it; Molitor drops the necklace (chance 100: no roll, MissionLootPlanner); a death is not a pickup; the necklace
        /// completes 2 and opens 3; a reload keeps it; Lohen hears the return and takes the necklace; the mission is completeable
        /// once, Trista (not Lohen) pays the stack chosen, once.
        /// </summary>
        [TestMethod]
        public void IrreconcilableDifferencesKillsMolitorForTristasFreedom()
        {
            var percentile = 0;
            using var harness = WildernessRuntimeTestHarness.Create(lootRoll: (minimum, maximum) => maximum == 100 ? percentile : minimum);
            C.SeedCompleted(harness, LastMen, Fort, Wire, Boss);
            var crucible = C.EnterCrucible(harness, C.StaalWaypoint);
            var trista = Npc(crucible, V.Trista);
            var lohen = Npc(crucible, G.Lohen);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, trista, Differences), "it waits for Earning Respect");
            C.SeedCompleted(harness, Respect);
            crucible = C.EnterCrucible(harness, C.StaalWaypoint);
            Spawn(crucible, V.MolitorPool);

            var offer = WildernessRanjaGorgeTests.Offer(harness, trista, Differences);
            CollectionAssert.AreEqual(new[]
                {
                    (I.CryogenicGrenade, I.CryogenicGrenades), (I.StandardMedPack, I.StandardMedPacks),
                    (I.IncendiaryGrenade, I.IncendiaryGrenades), (I.AdvancedMedPack, I.AdvancedMedPacks)
                },
                offer.SelectableReward.Select(item => (item.ItemTemplateId, item.Quantity)).ToArray());
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, lohen, Differences), "not Lohen's to give");
            percentile = 0;
            Assert.IsFalse(Holds(Kill(harness, crucible, V.Molitor), I.NecklaceTemplate), "no necklace before the mission");
            Respawn(crucible, V.MolitorPool);
            Accept(harness, trista, Differences);
            Assert.AreEqual((MissionObjectiveState.Incomplete, MissionObjectiveState.Inactive, MissionObjectiveState.Inactive),
                (Objective(harness, Differences, I.Challenge), Objective(harness, Differences, I.Necklace), Objective(harness, Differences, I.ReturnToLohen)));
            Assert.IsEmpty(Topics(harness, trista, Differences), "Trista hears no objective");
            CollectionAssert.AreEqual(new[] { I.Challenge }, Topics(harness, lohen, Differences), "Lohen hears the challenge only");
            Assert.IsFalse(harness.Manager.CompleteOfferedObjective(harness.Client, trista.EntityId, Differences, I.Challenge, 1), "not Trista's to hear");
            Assert.IsFalse(Holds(Kill(harness, crucible, V.Molitor), I.NecklaceTemplate), "no necklace before the challenge");
            Respawn(crucible, V.MolitorPool);

            Talk(harness, lohen, Differences, I.Challenge);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Differences, I.Necklace), "the challenge opens the necklace");
            var marker = harness.Manager.BuildStatusSnapshot(harness.Client.Player)[Differences].ObjectivesList
                .Single(objective => objective.ObjectiveId == I.Necklace).IndicatorList.Single();
            Assert.IsLessThan(0.1f, Vector2.Distance(new(marker.Position.X, marker.Position.Z), new((float)V.MolitorAt.X, (float)V.MolitorAt.Z)),
                "the marker on Molitor's pool");
            Assert.IsEmpty(Topics(harness, lohen, Differences), "Lohen waits for the necklace");
            Assert.IsFalse(Holds(Kill(harness, crucible, V.ThraxSoldier), I.NecklaceTemplate), "a Thrax Soldier of the pack has none");
            var necklace = Kill(harness, crucible, V.Molitor);
            Assert.IsTrue(Holds(necklace, I.NecklaceTemplate), "Molitor drops it (chance 100: no roll)");
            Assert.AreEqual(0U, Necklaces(harness), "a death is not a pickup");
            PickUp(harness, necklace);
            var packets = harness.Drain();
            Assert.AreEqual(1U, Necklaces(harness));
            Assert.AreEqual(1, packets.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Differences && packet.ObjectiveId == I.Necklace),
                "ObjectiveCompleted once");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Differences, I.ReturnToLohen), "the necklace opens the return");

            WildernessRanjaGorgeTests.Reload(harness);
            Assert.AreEqual(1U, Necklaces(harness), "kept at a reload");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Differences, I.ReturnToLohen));
            harness.Drain();

            Assert.IsEmpty(Topics(harness, trista, Differences), "Trista does not hear the return");
            CollectionAssert.AreEqual(new[] { I.ReturnToLohen }, Topics(harness, lohen, Differences), "Lohen hears the return");
            AssertHandInTopic(Conversation(harness, lohen), Differences, I.ReturnToLohen);
            harness.MoveTo(lohen.Position);
            harness.Drain();
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, lohen.EntityId, Differences, I.ReturnToLohen, 1));
            packets = harness.Drain();
            Assert.AreEqual(1, packets.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Differences && packet.ObjectiveId == I.ReturnToLohen),
                "ObjectiveCompleted once");
            Assert.AreEqual(1, packets.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Differences && packet.IsCompleteable),
                "MissionCompleteable once");
            Assert.AreEqual(0U, Necklaces(harness), "Lohen takes the necklace");

            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, lohen.EntityId, Differences, 3), "not Lohen's to pay");
            FullBagWaits(harness, trista, Differences, 3, ConsumableFiller);
            FailedTurnInPaysNothing(harness, trista, Differences, 3);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, trista, Differences, 3, I.Experience, I.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Differences), "MissionRewarded once");
            Assert.AreEqual(I.AdvancedMedPacks, WildernessRanjaGorgeTests.Held(harness, I.AdvancedMedPack), "the two Advanced Med Packs");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, trista, Differences), "done once");
        }

        /// <summary>
        /// An abandon with the necklace held keeps it; taken again, the challenge must be heard again, and then the held
        /// necklace completes objective 2 at once.
        /// </summary>
        [TestMethod]
        public void IrreconcilableDifferencesAbandonedKeepsTheNecklace()
        {
            using var harness = WildernessRuntimeTestHarness.Create(lootRoll: (minimum, maximum) => maximum == 100 ? 0 : minimum);
            C.SeedCompleted(harness, LastMen, Fort, Wire, Boss, Respect);
            var crucible = C.EnterCrucible(harness, C.StaalWaypoint);
            var trista = Npc(crucible, V.Trista);
            var lohen = Npc(crucible, G.Lohen);
            Spawn(crucible, V.MolitorPool);
            Accept(harness, trista, Differences);
            Talk(harness, lohen, Differences, I.Challenge);
            PickUp(harness, Kill(harness, crucible, V.Molitor));
            Assert.AreEqual(1U, Necklaces(harness));
            harness.Drain();

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Differences));
            harness.Drain();
            Assert.AreEqual(1U, Necklaces(harness), "the necklace is kept");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, trista, Differences), "offered again after an abandon");
            Accept(harness, trista, Differences);
            Assert.AreEqual((MissionObjectiveState.Incomplete, MissionObjectiveState.Inactive),
                (Objective(harness, Differences, I.Challenge), Objective(harness, Differences, I.Necklace)), "the old talk does not count");
            Talk(harness, lohen, Differences, I.Challenge);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Differences, I.Necklace), "the held necklace counts");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Differences, I.ReturnToLohen));
        }

        /// <summary>A Reconstructor Bot killed (its pools refilled when none is left); the packets it sent and its pool.</summary>
        private static (List<object> Packets, uint Pool) KillReconstructor(WildernessRuntimeTestHarness harness, MapChannel crucible)
        {
            if (!Alive(crucible).Any(candidate => candidate.DbId == S.Reconstructor && candidate.Npc == null))
                Respawn(crucible, ReconstructorPools);
            var pool = Alive(crucible).First(candidate => candidate.DbId == S.Reconstructor && candidate.Npc == null).SpawnPool.DbId;
            Kill(harness, crucible, S.Reconstructor);
            return (harness.Drain().Cast<object>().ToList(), pool);
        }

        /// <summary>The kill counter of the mission's first objective.</summary>
        private static uint Count(WildernessRuntimeTestHarness harness, uint mission) =>
            harness.Client.Player.Missions[mission].Objectives.Values.Single().Counters[0];

        private static uint Processors(WildernessRuntimeTestHarness harness) => WildernessRanjaGorgeTests.Held(harness, O.ProcessorTemplate);

        private static uint Necklaces(WildernessRuntimeTestHarness harness) => WildernessRanjaGorgeTests.Held(harness, I.NecklaceTemplate);
    }
}
