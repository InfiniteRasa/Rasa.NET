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

    /// <summary>
    /// Crucible set 3, wardens and lashers, played through as a player plays it: Viddia Grupa Donal's Game Wardens (1147:
    /// Warden Bot parts from the new Warden Bots by the Chaukas Robotics Facility's door), Medical Technician Halprin's
    /// Whiplash (1157: Lasher venom glands from the new Lashers north-east of the Awol Camp), and Kappa Grupa Lohen's Earning
    /// Respect (1170: a talk with each of the four Grupas and each Grupa's token mission done) and Business Proposal (1229:
    /// Lohen's proposal to Captain DeLisi).
    ///
    /// Why these tests: Halprin, the Warden Bots and the Lashers are new on the server; 1147 drops three parts from one kind
    /// of creature at three chances; 1170 counts token missions done before it, during it, and across a reload; 1229 gives
    /// its item at the accept and takes it at a talk at another NPC than its giver. An NPC or a pool off the navmesh, an offer
    /// before its prerequisite or at the wrong NPC, a drop on the wrong side of its chance or before the mission, a kill taken
    /// for a pickup, progress lost at a reload, a token mission not counted, a talk at the wrong Grupa, an item given with no
    /// room or kept after an abandon, or a turn-in that pays twice would each break them.
    ///
    /// The map: Ligo Crucible (1993), loaded as CrucibleOutpostIntrepidTests.EnterCrucible does (no upstream test does this
    /// for the Crucible). No mission of set 3 has a scene creature or a local teleporter.
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class CrucibleWardensAndLashersTests
    {
        private const uint LastMen = CrucibleLastMenStandingV1.MissionId;
        private const uint Fort = CrucibleFortCalledIntrepidV1.MissionId;
        private const uint Wire = CrucibleWireV1.MissionId;
        private const uint Boss = CrucibleBossOfAllBossesV1.MissionId;
        private const uint Racket = CrucibleProtectionRacketV1.MissionId;
        private const uint Guns = CrucibleSmokingGunsV1.MissionId;
        private const uint Dogs = CrucibleJunkyardDogsV1.MissionId;
        private const uint Wardens = CrucibleGameWardensV1.MissionId;
        private const uint Whiplash = CrucibleWhiplashV1.MissionId;
        private const uint Respect = CrucibleEarningRespectV1.MissionId;
        private const uint Proposal = CrucibleBusinessProposalV1.MissionId;
        private const uint MissionItemFiller = 11519;      // as PlateauCampResistanceTests (inventory category 4)
        private const uint ConsumableFiller = 45060;       // Class VII Basic Med Pack: fills the consumables bag
        private const uint WeaponFiller = 3869;            // inventory category 1 (quest-test-pattern)
        private const uint ArmorFiller = 12748;            // Armor_T3_Bio_V01_CMN_Gloves_30_to_34 (inventory category 1)

        /// <summary>The waypoint of the Awol Camp (World DB teleporter 192), where a player arrives.</summary>
        private static readonly Vector3 AwolWaypoint = new(-945.54f, 309.45f, 244.47f);

        private static readonly uint[] WardenPools = W.WardenPools.Select(pool => pool.Pool).ToArray();
        private static readonly uint[] LasherPools = W.LasherPools.Select(pool => pool.Pool).ToArray();

        [TestInitialize]
        public void Clock() => PlateauGiantKillerSquadTests.UseClock();

        [TestCleanup]
        public void Restore() => PlateauGiantKillerSquadTests.RestoreClock();

        /// <summary>
        /// Halprin stands on the navmesh by the hospital with the client's name, friendly, walked to from the Outpost Intrepid
        /// waypoint; three Warden Bot Screws come out of each pool by the Chaukas Robotics Facility's door and three Lashers
        /// out of each nest pool, hostile, of their classes, levels and hit points, on the navmesh, walked to from the Labbna
        /// Quarter's and the Awol Camp's waypoints.
        /// </summary>
        [TestMethod]
        public void HalprinTheWardenBotsAndTheLashersStandAtTheirPlaces()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var crucible = C.EnterCrucible(harness);
            var halprin = Npc(crucible, W.Halprin);
            Assert.AreEqual(W.HalprinName, halprin.NameId);
            Assert.AreEqual(W.HalprinPackage, halprin.Npc.NpcPackageId, "his server-only package");
            Assert.AreNotEqual(TargetCategory.Hostile, halprin.TargetCategory);
            Assert.IsLessThan(0.1f, Vector2.Distance(new((float)W.HalprinAt.X, (float)W.HalprinAt.Z), new(halprin.Position.X, halprin.Position.Z)),
                "Halprin at his place");
            AssertOnGround(crucible, halprin.Position, "Halprin");
            Assert.IsTrue(crucible.NavMesh.FindPath(crucible.NavMesh.Nearest(C.OutpostWaypoint).Value, halprin.Position, out var walked) != null && walked,
                "Halprin is walked to from the Outpost Intrepid waypoint");

            foreach (var (pools, creature, cls, level, health, from) in new[]
            {
                (W.WardenPools, W.WardenBot, W.WardenBotClass, W.WardenBotLevel, W.WardenBotHitPoints, C.LabbnaWaypoint),
                (W.LasherPools.Select(pool => (pool.Pool, At: CrucibleFixFV1.Now(pool.Pool) ?? pool.At)).ToArray(), W.Lasher, W.LasherClass, W.LasherLevel,
                    W.LasherHitPoints, AwolWaypoint)   // fix job F moved the Lasher pools off the lava
            })
            {
                Spawn(crucible, pools.Select(pool => pool.Pool).ToArray());
                foreach (var (pool, at) in pools)
                {
                    var place = new Vector3((float)at.X, (float)at.Y, (float)at.Z);
                    AssertOnGround(crucible, place, $"pool {pool}");
                    Assert.IsTrue(crucible.NavMesh.FindPath(crucible.NavMesh.Nearest(from).Value, place, out var reached) != null && reached,
                        $"pool {pool} is walked to");
                    var here = Alive(crucible).Where(candidate => candidate.SpawnPool?.DbId == pool).Distinct().ToArray();
                    Assert.HasCount((int)W.PoolCreatures, here, $"pool {pool}: three");
                    foreach (var one in here)
                    {
                        Assert.AreEqual((creature, level, TargetCategory.Hostile, cls), (one.DbId, one.Level, one.TargetCategory, (uint)one.EntityClass),
                            $"pool {pool}");
                        Assert.AreEqual((int)health, (int)one.Attributes[Attributes.Health].NormalMax, $"pool {pool}: its own hit points");
                    }
                }
            }
        }

        /// <summary>
        /// Game Wardens after The Boss Of All Bosses: Donal offers it (only he); no part drops before it; each part drops under
        /// its chance only (power supply and neural processor 50, servo-motor 75), and a death is not a pickup; the counts kept
        /// at a reload; each objective completes once, the last part makes it completeable once; Donal takes the parts and pays
        /// the Viddia token with the item chosen.
        /// </summary>
        [TestMethod]
        public void GameWardensTakesTheWardenBotPartsToDonal()
        {
            var percentile = 0;
            using var harness = WildernessRuntimeTestHarness.Create(lootRoll: (minimum, maximum) => maximum == 100 ? percentile : minimum);
            C.SeedCompleted(harness, LastMen, Fort, Wire);
            var crucible = C.EnterCrucible(harness, C.InfirmaryWaypoint);
            var donal = Npc(crucible, N.Donal);
            var vitto = Npc(crucible, N.Vitto);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, donal, Wardens), "it waits for The Boss Of All Bosses");
            C.SeedCompleted(harness, Boss);
            crucible = C.EnterCrucible(harness, C.InfirmaryWaypoint);
            Spawn(crucible, WardenPools);

            var offer = WildernessRanjaGorgeTests.Offer(harness, donal, Wardens);
            CollectionAssert.AreEqual(new[] { CrucibleGameWardensV1.BioBoots, CrucibleGameWardensV1.StealthHelmet },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            CollectionAssert.AreEqual(new[] { CrucibleGameWardensV1.ViddiaToken },
                offer.FixedReward.FixedItems.Select(item => item.ItemTemplateId).ToArray(), "the Viddia token with every choice");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, vitto, Wardens), "not Vitto's to give");
            percentile = 0;
            var early = Kill(harness, crucible, W.WardenBot);
            Assert.IsFalse(Part(early, 0) || Part(early, 1) || Part(early, 2), "no part before the mission");
            Accept(harness, donal, Wardens);
            Assert.IsEmpty(Topics(harness, donal, Wardens), "Donal hears no objective: the parts count as they are picked up");
            MarkersOnTheWardenPools(harness, 1, 2, 3);

            // 75: none; 74 and 50: only a servo-motor; 49: all three. A death is not a pickup.
            percentile = 75;
            var none = Kill(harness, crucible, W.WardenBot);
            Assert.IsFalse(Part(none, 0) || Part(none, 1) || Part(none, 2), "75 misses all three");
            percentile = 74;
            var servoAt74 = Kill(harness, crucible, W.WardenBot);
            Assert.IsFalse(Part(servoAt74, 0) || Part(servoAt74, 1), "74 misses a power supply and a neural processor");
            Assert.IsTrue(Part(servoAt74, 2), "74 drops a servo-motor");
            Assert.AreEqual(0U, Held(harness, 2), "a death is not a pickup");
            // A pickup needs the player near the corpse: pick up each one where it falls.
            PickUp(harness, servoAt74);
            Assert.AreEqual((0U, 0U, 1U), (Held(harness, 0), Held(harness, 1), Held(harness, 2)), "the servo-motor at 74");
            percentile = 50;
            var servo = Kill(harness, crucible, W.WardenBot);
            Assert.IsFalse(Part(servo, 0) || Part(servo, 1), "50 misses a power supply and a neural processor");
            Assert.IsTrue(Part(servo, 2), "50 drops a servo-motor");
            PickUp(harness, servo);
            percentile = 49;
            var all = Kill(harness, crucible, W.WardenBot);
            Assert.IsTrue(Part(all, 0) && Part(all, 1) && Part(all, 2), "49 drops all three");
            Assert.AreEqual((0U, 0U, 2U), (Held(harness, 0), Held(harness, 1), Held(harness, 2)), "a death is not a pickup");
            PickUp(harness, all);
            Assert.AreEqual((1U, 1U, 3U), (Held(harness, 0), Held(harness, 1), Held(harness, 2)));
            harness.Drain();

            percentile = 0;
            var completed = new List<uint>();
            var completeable = 0;
            var kills = 0;
            var thirdChecked = false;
            while (Held(harness, 2) < 15)
            {
                if (kills++ == 6)
                {
                    WildernessRanjaGorgeTests.Reload(harness);
                    Assert.AreEqual((7U, 7U, 9U), (Held(harness, 0), Held(harness, 1), Held(harness, 2)), "kept at a reload");
                    Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Wardens, CrucibleGameWardensV1.PowerSupplies));
                    harness.Drain();
                }
                if (!Alive(crucible).Any(candidate => candidate.DbId == W.WardenBot && candidate.Npc == null))
                    Respawn(crucible, WardenPools);
                var loot = Kill(harness, crucible, W.WardenBot);
                Assert.IsTrue(Part(loot, 2), "a servo-motor at 0");
                PickUp(harness, loot);
                var drained = harness.Drain();
                completed.AddRange(drained.OfType<ObjectiveCompletedPacket>().Where(packet => packet.MissionId == Wardens).Select(packet => packet.ObjectiveId));
                if (completed.Contains(1) && completed.Contains(2) && !completed.Contains(3) && !thirdChecked)
                {
                    // The bot (2026-11-07): objective 3 showed no marker. With the first two done, the third keeps its three.
                    MarkersOnTheWardenPools(harness, 3);
                    thirdChecked = true;
                }
                completeable += drained.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Wardens && packet.IsCompleteable);
            }
            Assert.AreEqual((10U, 10U, 15U), (Held(harness, 0), Held(harness, 1), Held(harness, 2)), "no part over its count");
            CollectionAssert.AreEquivalent(new[] { 1U, 2U, 3U }, completed, "ObjectiveCompleted once each");
            Assert.IsTrue(thirdChecked, "objective 3's markers checked with the first two done");
            Assert.AreEqual(1, completeable, "MissionCompleteable once");

            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, vitto.EntityId, Wardens, 0), "not Vitto's to take");
            FullBagWaits(harness, donal, Wardens, 0, ArmorFiller);
            FailedTurnInPaysNothing(harness, donal, Wardens, 0);
            Assert.AreEqual(15U, Held(harness, 2), "kept at the failed turn-in");
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, donal, Wardens, 0, CrucibleGameWardensV1.Experience, CrucibleGameWardensV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Wardens), "MissionRewarded once");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, CrucibleGameWardensV1.BioBoots), "the boots chosen");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, CrucibleGameWardensV1.ViddiaToken), "the Viddia token");
            Assert.AreEqual((0U, 0U, 0U), (Held(harness, 0), Held(harness, 1), Held(harness, 2)), "Donal takes the parts");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, donal, Wardens), "done once");
        }

        /// <summary>
        /// To-fix row 25 and the bot's "1147 objective 3 has no marker": each open objective named shows the client its three
        /// markers (CrucibleMarkersClearOfLinksV2.WardenMarkers), one on each Warden Bot pool, on the Crucible.
        /// </summary>
        private static void MarkersOnTheWardenPools(WildernessRuntimeTestHarness harness, params uint[] objectives)
        {
            var info = harness.Manager.BuildStatusSnapshot(harness.Client.Player)[Wardens];
            foreach (var objective in objectives)
            {
                var markers = info.ObjectivesList.Single(o => o.ObjectiveId == objective).IndicatorList;
                CollectionAssert.AreEquivalent(CrucibleMarkersClearOfLinksV2.WardenMarkers.Where(m => m.Objective == objective).Select(m => m.Indicator).ToArray(),
                    markers.Select(m => m.IndicatorId).ToArray(), $"objective {objective}'s markers");
                foreach (var marker in markers)
                {
                    Assert.AreEqual(CrucibleMissionDataV1.MapContextId, marker.MapContextId, $"objective {objective} marker {marker.IndicatorId}'s map");
                    Assert.IsTrue(W.WardenPools.Select(pool => CrucibleFixFV1.Now(pool.Pool) ?? CrucibleWardenPoolsAndVargoV1.Now(pool.Pool) ?? pool.At)
                        .Any(at => Vector2.Distance(new(marker.Position.X, marker.Position.Z), new((float)at.X, (float)at.Z)) < 0.1f),
                        $"objective {objective} marker {marker.IndicatorId} on a Warden Bot pool");
                }
            }
        }

        /// <summary>A part stays on the corpse while the bag is full; an abandon keeps the parts, and taken again they count.</summary>
        [TestMethod]
        public void TheWardenBotPartsWaitForRoomAndGameWardensCanBeAbandoned()
        {
            using var harness = WildernessRuntimeTestHarness.Create(lootRoll: (minimum, maximum) => maximum == 100 ? 0 : minimum);
            C.SeedCompleted(harness, LastMen, Fort, Wire, Boss);
            var crucible = C.EnterCrucible(harness, C.InfirmaryWaypoint);
            var donal = Npc(crucible, N.Donal);
            Spawn(crucible, WardenPools);
            Accept(harness, donal, Wardens);
            WildernessAliaBranchesTests.FillCategory(harness, MissionItemFiller, 50);

            var loot = Kill(harness, crucible, W.WardenBot);
            PickUp(harness, loot);
            Assert.AreEqual(0U, Held(harness, 0) + Held(harness, 1) + Held(harness, 2), "no room");
            Assert.IsTrue(Part(loot, 0), "still on the corpse");
            for (var slot = 0; slot < 3; slot++)
                WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
            PickUp(harness, loot);
            Assert.AreEqual((1U, 1U, 1U), (Held(harness, 0), Held(harness, 1), Held(harness, 2)), "picked up once there is room");
            harness.Drain();

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Wardens));
            harness.Drain();
            Assert.AreEqual((1U, 1U, 1U), (Held(harness, 0), Held(harness, 1), Held(harness, 2)), "the parts are kept");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, donal, Wardens), "offered again after an abandon");
            Accept(harness, donal, Wardens);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Wardens, CrucibleGameWardensV1.PowerSupplies), "one of ten");
            harness.Drain();

            // The held part counts after the re-accept: nine more complete the power supplies and the neural processors.
            for (var kill = 0; kill < 9; kill++)
            {
                if (!Alive(crucible).Any(candidate => candidate.DbId == W.WardenBot && candidate.Npc == null))
                    Respawn(crucible, WardenPools);
                PickUp(harness, Kill(harness, crucible, W.WardenBot));
            }
            Assert.AreEqual((10U, 10U, 10U), (Held(harness, 0), Held(harness, 1), Held(harness, 2)), "the held part and nine more");
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Wardens, CrucibleGameWardensV1.PowerSupplies), "ten power supplies");
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Wardens, CrucibleGameWardensV1.NeuralProcessors), "ten neural processors");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Wardens, CrucibleGameWardensV1.ServoMotors), "ten of fifteen servo-motors");
        }

        /// <summary>
        /// Whiplash, with no prerequisite: Halprin offers it (only he); a gland drops from a Lasher under 60 only, not from a
        /// Warden Bot, and a death is not a pickup; the count kept at a reload; the tenth completes it once; Halprin takes the
        /// glands and pays with the item chosen.
        /// </summary>
        [TestMethod]
        public void WhiplashTakesTenVenomGlandsToHalprin()
        {
            var percentile = 0;
            using var harness = WildernessRuntimeTestHarness.Create(lootRoll: (minimum, maximum) => maximum == 100 ? percentile : minimum);
            var crucible = C.EnterCrucible(harness);
            var halprin = Npc(crucible, W.Halprin);
            var drake = Npc(crucible, N.Drake);
            Spawn(crucible, LasherPools.Concat(WardenPools).ToArray());

            var offer = WildernessRanjaGorgeTests.Offer(harness, halprin, Whiplash);
            CollectionAssert.AreEqual(new[]
                {
                    CrucibleWhiplashV1.RocketLauncher, CrucibleWhiplashV1.ElectricNetGun, CrucibleWhiplashV1.VirulentLeechGun,
                    CrucibleWhiplashV1.PulseShotgun
                },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, drake, Whiplash), "not Drake's to give");
            Assert.IsFalse(Holds(Kill(harness, crucible, W.Lasher), CrucibleWhiplashV1.GlandTemplate), "no gland before the mission");
            Accept(harness, halprin, Whiplash);
            Assert.IsEmpty(Topics(harness, halprin, Whiplash), "Halprin hears no objective");

            percentile = 0;
            Assert.IsFalse(Holds(Kill(harness, crucible, W.WardenBot), CrucibleWhiplashV1.GlandTemplate), "a Warden Bot has no gland");
            percentile = CrucibleWhiplashV1.GlandChance;
            Assert.IsFalse(Holds(Kill(harness, crucible, W.Lasher), CrucibleWhiplashV1.GlandTemplate), "60 misses");
            percentile = CrucibleWhiplashV1.GlandChance - 1;
            var gland = Kill(harness, crucible, W.Lasher);
            Assert.IsTrue(Holds(gland, CrucibleWhiplashV1.GlandTemplate), "59 drops a gland");
            Assert.AreEqual(0U, Glands(harness), "a death is not a pickup");
            PickUp(harness, gland);
            Assert.AreEqual(1U, Glands(harness));
            harness.Drain();

            // An abandon keeps the gland; offered again, and taken again the gland counts.
            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Whiplash));
            harness.Drain();
            Assert.AreEqual(1U, Glands(harness), "the gland is kept");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, halprin, Whiplash), "offered again after an abandon");
            Accept(harness, halprin, Whiplash);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Whiplash, CrucibleWhiplashV1.Glands), "one of ten");
            harness.Drain();

            var completed = 0;
            var completeable = 0;
            while (Glands(harness) < CrucibleWhiplashV1.GlandCount)
            {
                if (Glands(harness) == 5)
                {
                    WildernessRanjaGorgeTests.Reload(harness);
                    Assert.AreEqual(5U, Glands(harness), "kept at a reload");
                    Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Whiplash, CrucibleWhiplashV1.Glands));
                    harness.Drain();
                }
                if (!Alive(crucible).Any(candidate => candidate.DbId == W.Lasher && candidate.Npc == null))
                    Respawn(crucible, LasherPools);
                var loot = Kill(harness, crucible, W.Lasher);
                Assert.IsTrue(Holds(loot, CrucibleWhiplashV1.GlandTemplate), "59 drops");
                PickUp(harness, loot);
                var drained = harness.Drain();
                completed += drained.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Whiplash);
                completeable += drained.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Whiplash && packet.IsCompleteable);
            }
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Whiplash, CrucibleWhiplashV1.Glands), "ten");
            Assert.AreEqual((1, 1), (completed, completeable), "ObjectiveCompleted once, MissionCompleteable once");

            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, drake.EntityId, Whiplash, 1), "not Drake's to take");
            FullBagWaits(harness, halprin, Whiplash, 1, WeaponFiller);
            FailedTurnInPaysNothing(harness, halprin, Whiplash, 1);
            Assert.AreEqual(CrucibleWhiplashV1.GlandCount, Glands(harness), "kept at the failed turn-in");
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, halprin, Whiplash, 1, CrucibleWhiplashV1.Experience, CrucibleWhiplashV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Whiplash), "MissionRewarded once");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, CrucibleWhiplashV1.ElectricNetGun), "the net gun chosen");
            Assert.AreEqual(0U, Glands(harness), "Halprin takes the glands");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, halprin, Whiplash), "done once");
        }

        /// <summary>
        /// Earning Respect after The Boss Of All Bosses: Lohen offers it (only he). Smoking Guns done before it counts at the
        /// accept; Game Wardens done while it is open counts at its turn-in; The Protection Racket and Junkyard Dogs, in the
        /// history while it is open, count at a reload. Each Grupa hears only his own talk, each once; a reload halfway; the
        /// last makes it completeable once; Lohen pays with the item chosen, and the tokens stay with the player. Business
        /// Proposal follows.
        /// </summary>
        [TestMethod]
        public void EarningRespectTalksToTheFourGrupasAndCountsTheirTokens()
        {
            using var harness = WildernessRuntimeTestHarness.Create(lootRoll: (minimum, maximum) => maximum == 100 ? 0 : minimum);
            C.SeedCompleted(harness, LastMen, Fort, Wire);
            var crucible = C.EnterCrucible(harness, C.StaalWaypoint);
            var lohen = Npc(crucible, G.Lohen);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, lohen, Respect), "it waits for The Boss Of All Bosses");
            C.SeedCompleted(harness, Boss, Guns);
            crucible = C.EnterCrucible(harness, C.StaalWaypoint);
            var grupas = CrucibleEarningRespectV1.Grupas.ToDictionary(grupa => grupa.Talk, grupa => Npc(crucible, grupa.Npc));
            var munit = grupas[1];
            var vitto = grupas[3];
            var donal = grupas[5];
            var rigs = grupas[7];

            var offer = WildernessRanjaGorgeTests.Offer(harness, lohen, Respect);
            CollectionAssert.AreEqual(new[]
                {
                    CrucibleEarningRespectV1.EmpTorqueshellRifle, CrucibleEarningRespectV1.ElectricPolarityGun,
                    CrucibleEarningRespectV1.RocketLauncher, CrucibleEarningRespectV1.VirulentLeechGun
                },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, munit, Respect), "not Munit's to give");
            Accept(harness, lohen, Respect);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Respect, 2), "Smoking Guns, done before, gives the Top Khana token");
            foreach (var token in new[] { 4U, 6U, 8U })
                Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Respect, token), $"token {token} waits for its mission");
            Assert.IsEmpty(Topics(harness, lohen, Respect), "Lohen hears no objective");
            foreach (var (talk, npc) in grupas)
                CollectionAssert.AreEqual(new[] { talk }, Topics(harness, npc, Respect), $"{npc.DbId} hears only {talk}");

            var packets = new List<object>();
            Assert.IsFalse(harness.Manager.CompleteOfferedObjective(harness.Client, vitto.EntityId, Respect, 1, 1), "Vitto does not hear Munit's");
            packets.AddRange(Say(harness, munit, Respect, 1));
            packets.AddRange(Say(harness, vitto, Respect, 3));
            Assert.IsEmpty(Topics(harness, munit, Respect), "heard once");

            WildernessRanjaGorgeTests.Reload(harness);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Respect, 3), "kept at a reload");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Respect, 5));
            harness.Drain();

            packets.AddRange(Say(harness, donal, Respect, 5));
            packets.AddRange(Say(harness, rigs, Respect, 7));
            Assert.IsFalse(harness.Client.Player.Missions[Respect].Completeable, "not before the tokens");

            // Game Wardens done while Earning Respect is open: its turn-in counts the Viddia token.
            Spawn(crucible, WardenPools);
            Accept(harness, donal, Wardens);
            while (Held(harness, 2) < 15)
            {
                if (!Alive(crucible).Any(candidate => candidate.DbId == W.WardenBot && candidate.Npc == null))
                    Respawn(crucible, WardenPools);
                PickUp(harness, Kill(harness, crucible, W.WardenBot));
                harness.Drain();
            }
            harness.Drain();
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, donal, Wardens, 1, CrucibleGameWardensV1.Experience, CrucibleGameWardensV1.Credits);
            packets.AddRange(harness.Drain());
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Respect, 6), "Game Wardens gives the Viddia token");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Respect, 4));

            // The Protection Racket and Junkyard Dogs in the history: counted at the next login (PublishInitialState).
            C.SeedCompleted(harness, Racket, Dogs);
            WildernessRanjaGorgeTests.Reload(harness);
            harness.Manager.PublishInitialState(harness.Client);
            harness.Drain();
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Respect, 4), "the Larai token");
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Respect, 8), "the Labbna token");
            Assert.IsTrue(harness.Client.Player.Missions[Respect].Completeable);
            CollectionAssert.AreEquivalent(new[] { 1U, 3U, 5U, 6U, 7U }, packets.OfType<ObjectiveCompletedPacket>()
                .Where(packet => packet.MissionId == Respect).Select(packet => packet.ObjectiveId).ToArray(),
                "ObjectiveCompleted once each of the live ones (2 came at the accept, 4 and 8 at the login)");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, lohen, Proposal), "Business Proposal waits for Earning Respect");

            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, munit.EntityId, Respect, 0), "not Munit's to take");
            FullBagWaits(harness, lohen, Respect, 0, WeaponFiller);
            FailedTurnInPaysNothing(harness, lohen, Respect, 0);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, lohen, Respect, 0, CrucibleEarningRespectV1.Experience, CrucibleEarningRespectV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Respect), "MissionRewarded once");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, CrucibleEarningRespectV1.EmpTorqueshellRifle), "the rifle chosen");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, CrucibleGameWardensV1.ViddiaToken), "the Viddia token stays");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, lohen, Respect), "done once");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, lohen, Proposal), "Business Proposal follows");
        }

        /// <summary>An abandon takes the talks back; taken again, Munit's talk counts again and Smoking Guns still does.</summary>
        [TestMethod]
        public void EarningRespectAbandonedStartsTheTalksAgain()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            C.SeedCompleted(harness, LastMen, Fort, Wire, Boss, Guns);
            var crucible = C.EnterCrucible(harness, C.StaalWaypoint);
            var lohen = Npc(crucible, G.Lohen);
            var munit = Npc(crucible, N.Munit);
            Accept(harness, lohen, Respect);
            Talk(harness, munit, Respect, 1);

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Respect));
            harness.Drain();
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, lohen, Respect), "offered again after an abandon");
            Accept(harness, lohen, Respect);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Respect, 1), "the old talk does not count");
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Respect, 2), "Smoking Guns still counts");
            Talk(harness, munit, Respect, 1);
        }

        /// <summary>
        /// Business Proposal after Earning Respect: Lohen offers it (only he); a full bag: no mission, a slot freed: the proposal
        /// with it; DeLisi hears it (only he), a reload halfway, and takes the proposal; DeLisi pays once.
        /// </summary>
        [TestMethod]
        public void BusinessProposalTakesLohensProposalToDeLisi()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            C.SeedCompleted(harness, LastMen, Fort, Wire, Boss);
            var crucible = C.EnterCrucible(harness, C.StaalWaypoint);
            var lohen = Npc(crucible, G.Lohen);
            var delisi = Npc(crucible, N.DeLisi);
            var drake = Npc(crucible, N.Drake);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, lohen, Proposal), "it waits for Earning Respect");
            C.SeedCompleted(harness, Respect);
            crucible = C.EnterCrucible(harness, C.StaalWaypoint);

            var offer = WildernessRanjaGorgeTests.Offer(harness, lohen, Proposal);
            CollectionAssert.AreEqual(new[]
                {
                    (CrucibleBusinessProposalV1.EmpGrenade, CrucibleBusinessProposalV1.EmpGrenades),
                    (CrucibleBusinessProposalV1.ResTraumaKit, CrucibleBusinessProposalV1.ResTraumaKits),
                    (CrucibleBusinessProposalV1.ConcussionGrenade, CrucibleBusinessProposalV1.ConcussionGrenades),
                    (CrucibleBusinessProposalV1.BasicMedPack, CrucibleBusinessProposalV1.BasicMedPacks)
                },
                offer.SelectableReward.Select(item => (item.ItemTemplateId, item.Quantity)).ToArray());
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, delisi, Proposal), "not DeLisi's to give");

            // No room for the proposal: no mission. A slot freed, it comes with it.
            WildernessAliaBranchesTests.FillCategory(harness, MissionItemFiller, 50);
            harness.MoveTo(lohen.Position);
            harness.Drain();
            Assert.IsFalse(harness.Manager.AcceptOfferedMission(harness.Client, lohen.EntityId, Proposal), "no room for the proposal");
            Assert.IsFalse(harness.Client.Player.Missions.ContainsKey(Proposal));
            WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
            Accept(harness, lohen, Proposal);
            Assert.AreEqual(1U, ProposalHeld(harness), "the proposal");
            Assert.IsEmpty(Topics(harness, lohen, Proposal), "Lohen hears no objective");
            Assert.IsEmpty(Topics(harness, drake, Proposal), "nor Drake");
            Assert.IsFalse(harness.Manager.CompleteOfferedObjective(harness.Client, drake.EntityId, Proposal, CrucibleBusinessProposalV1.Deliver, 1),
                "not Drake's to hear");

            WildernessRanjaGorgeTests.Reload(harness);
            Assert.AreEqual(1U, ProposalHeld(harness), "kept at a reload");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Proposal, CrucibleBusinessProposalV1.Deliver));
            harness.Drain();

            AssertHandInTopic(Conversation(harness, delisi), Proposal, CrucibleBusinessProposalV1.Deliver);
            var packets = Say(harness, delisi, Proposal, CrucibleBusinessProposalV1.Deliver);
            Assert.AreEqual(1, packets.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Proposal), "ObjectiveCompleted once");
            Assert.AreEqual(1, packets.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Proposal && packet.IsCompleteable),
                "MissionCompleteable once");
            Assert.AreEqual(0U, ProposalHeld(harness), "DeLisi takes the proposal");
            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, lohen.EntityId, Proposal, 0), "not Lohen's to take");

            FullBagWaits(harness, delisi, Proposal, 0, ConsumableFiller);
            FailedTurnInPaysNothing(harness, delisi, Proposal, 0);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, delisi, Proposal, 0,
                CrucibleBusinessProposalV1.Experience, CrucibleBusinessProposalV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Proposal), "MissionRewarded once");
            Assert.AreEqual(CrucibleBusinessProposalV1.EmpGrenades,
                WildernessRanjaGorgeTests.Held(harness, CrucibleBusinessProposalV1.EmpGrenade), "the three EMP Grenades");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, lohen, Proposal), "done once");
        }

        /// <summary>An abandon takes the proposal; taken again, a new one.</summary>
        [TestMethod]
        public void BusinessProposalAbandonedTakesTheProposal()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            C.SeedCompleted(harness, LastMen, Fort, Wire, Boss, Respect);
            var crucible = C.EnterCrucible(harness, C.StaalWaypoint);
            var lohen = Npc(crucible, G.Lohen);
            Accept(harness, lohen, Proposal);
            Assert.AreEqual(1U, ProposalHeld(harness));

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Proposal));
            harness.Drain();
            Assert.AreEqual(0U, ProposalHeld(harness), "the proposal goes");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, lohen, Proposal), "offered again after an abandon");
            Accept(harness, lohen, Proposal);
            Assert.AreEqual(1U, ProposalHeld(harness), "a new proposal");
        }

        /// <summary>A talk the NPC hears, done; the packets it sent.</summary>
        private static List<object> Say(WildernessRuntimeTestHarness harness, Creature npc, uint mission, uint objective)
        {
            harness.MoveTo(npc.Position);
            harness.Drain();
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, npc.EntityId, mission, objective, 1),
                $"{npc.DbId} hears {mission}.{objective}");
            var packets = harness.Drain().Cast<object>().ToList();
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, mission, objective));
            return packets;
        }

        /// <summary>The corpse holds part <paramref name="index"/> of Game Wardens (CrucibleGameWardensV1.Parts).</summary>
        private static bool Part(LootDispenser loot, int index) => Holds(loot, CrucibleGameWardensV1.Parts[index].Template);

        /// <summary>How many of part <paramref name="index"/> the player holds.</summary>
        private static uint Held(WildernessRuntimeTestHarness harness, int index) =>
            WildernessRanjaGorgeTests.Held(harness, CrucibleGameWardensV1.Parts[index].Template);

        private static uint Glands(WildernessRuntimeTestHarness harness) => WildernessRanjaGorgeTests.Held(harness, CrucibleWhiplashV1.GlandTemplate);

        private static uint ProposalHeld(WildernessRuntimeTestHarness harness) =>
            WildernessRanjaGorgeTests.Held(harness, CrucibleBusinessProposalV1.ProposalTemplate);
    }
}
