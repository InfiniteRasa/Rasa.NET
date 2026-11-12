using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Managers;
using Rasa.Packets.MapChannel.Server;
using Rasa.Packets.Mission.Server;
using Rasa.Services.Preloader.Missions.Crucible;
using Rasa.Structures;
using Rasa.Structures.World;
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
    using D = CrucibleSalvageDutyV1;

    /// <summary>
    /// Crucible set 4, the minefield and the salvage, played through as a player plays it: Private Drake's Minesweeper (1232:
    /// twenty Smart Mines in the trench west of the Fort Intrepid ruins), Captain DeLisi's Funeral Derge (1241: Executioner
    /// Derge on the Fort Intrepid rampart), and Labbna Sigal Haggus' Salvage Duty (1203) and More Salvage Duty (1204): parts
    /// from the Warden Bots and the new Reconstructor Bots by the Chaukas Robotics Facility.
    ///
    /// Why these tests: the Smart Mines and the Reconstructor Bots are new on the server and Derge is moved; 1232 counts kills
    /// of one creature only; 1203 and 1204 drop five parts from two kinds of creature at five chances, and 1204's counts are
    /// not the ones its objective names say (triage decision 2). A pool off the navmesh, an offer before its prerequisite or at
    /// the wrong NPC, a kill of another creature counted, a drop on the wrong side of its chance, from the wrong creature or
    /// before the mission, a kill taken for a pickup, progress lost at a reload or kept after an abandon, a part kept from a
    /// turn-in, or a turn-in that pays twice would each break them.
    ///
    /// The map: Ligo Crucible (1993), loaded as CrucibleOutpostIntrepidTests.EnterCrucible does (no upstream test does this
    /// for the Crucible). No mission of set 4 has a scene creature or a local teleporter.
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class CrucibleMinefieldAndSalvageTests
    {
        private const uint LastMen = CrucibleLastMenStandingV1.MissionId;
        private const uint Fort = CrucibleFortCalledIntrepidV1.MissionId;
        private const uint Wire = CrucibleWireV1.MissionId;
        private const uint Boss = CrucibleBossOfAllBossesV1.MissionId;
        private const uint Dogs = CrucibleJunkyardDogsV1.MissionId;
        private const uint Minesweeper = CrucibleMinesweeperV1.MissionId;
        private const uint Derge = CrucibleFuneralDergeV1.MissionId;
        private const uint Salvage = D.MissionId;
        private const uint MoreSalvage = D.MoreMissionId;
        private const uint MissionItemFiller = 11519;      // as PlateauCampResistanceTests (inventory category 4)
        private const uint WeaponFiller = 3869;            // inventory category 1 (quest-test-pattern)
        private const uint ArmorFiller = 12748;            // Armor_T3_Bio_V01_CMN_Gloves_30_to_34 (inventory category 1)
        private const uint ModuleFiller = 122983;          // Armor Module: Regen Bonus [1] (inventory category 3)

        private static readonly uint[] MinePools = S.MinePools.Select(pool => pool.Pool).ToArray();
        private static readonly uint[] BotPools = D.BotPools.Select(pool => pool.Pool).ToArray();
        private static readonly uint[] WardenPools = W.WardenPools.Select(pool => pool.Pool).ToArray();
        private static readonly uint[] ReconstructorPools = S.ReconstructorPools.Select(pool => pool.Pool).ToArray();

        [TestInitialize]
        public void Clock() => PlateauGiantKillerSquadTests.UseClock();

        [TestCleanup]
        public void Restore() => PlateauGiantKillerSquadTests.RestoreClock();

        /// <summary>
        /// Four Smart Mines come out of each trench pool and three Reconstructor Bots out of each pool by the Chaukas Robotics
        /// Facility, hostile, of their classes, levels and hit points, on the navmesh, walked to from the Fort Intrepid and the
        /// Labbna Quarter waypoints; Derge stands on the rampart, walked to up the stairs from the Fort Intrepid waypoint.
        /// </summary>
        [TestMethod]
        public void TheMinesTheReconstructorsAndDergeStandAtTheirPlaces()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var crucible = C.EnterCrucible(harness);
            foreach (var (pools, creature, cls, level, health, count, from) in new[]
            {
                (S.MinePools, S.SmartMine, S.SmartMineClass, S.SmartMineLevel, S.SmartMineHitPoints, S.MinesPerPool, C.FortWaypoint),
                (S.ReconstructorPools, S.Reconstructor, S.ReconstructorClass, S.ReconstructorLevel, S.ReconstructorHitPoints,
                    S.ReconstructorsPerPool, C.LabbnaWaypoint),
                (new[] { (Pool: S.DergePool, At: S.DergeAt) }, S.Derge, 10504U, 47U, 5200U, 1U, C.FortWaypoint)
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
                    Assert.HasCount((int)count, here, $"pool {pool}");
                    foreach (var one in here)
                    {
                        Assert.AreEqual((creature, level, TargetCategory.Hostile, cls), (one.DbId, one.Level, one.TargetCategory, (uint)one.EntityClass),
                            $"pool {pool}");
                        Assert.AreEqual((int)health, (int)one.Attributes[Attributes.Health].NormalMax, $"pool {pool}: its hit points");
                    }
                }
            }
        }

        /// <summary>
        /// Minesweeper after A Fort Called Intrepid: Drake offers it (only he); a mine destroyed before it does not count, nor
        /// another creature; the client gets a marker on each mine pool; twenty mines with a reload halfway complete it once;
        /// Drake pays with the item chosen, once. An abandon halfway starts the count again.
        /// </summary>
        [TestMethod]
        public void MinesweeperDestroysTwentySmartMinesForDrake()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            C.SeedCompleted(harness, LastMen);
            var crucible = C.EnterCrucible(harness, C.FortWaypoint);
            var drake = Npc(crucible, N.Drake);
            var delisi = Npc(crucible, N.DeLisi);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, drake, Minesweeper), "it waits for A Fort Called Intrepid");
            C.SeedCompleted(harness, Fort);
            crucible = C.EnterCrucible(harness, C.FortWaypoint);
            Spawn(crucible, MinePools.Concat(ReconstructorPools).ToArray());

            var offer = WildernessRanjaGorgeTests.Offer(harness, drake, Minesweeper);
            CollectionAssert.AreEqual(new[]
                {
                    CrucibleMinesweeperV1.MechBoots, CrucibleMinesweeperV1.StealthBoots, CrucibleMinesweeperV1.GravitonLegs,
                    CrucibleMinesweeperV1.MotorAssistHelmet
                },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Assert.IsEmpty(offer.FixedReward.FixedItems, "no fixed item");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, delisi, Minesweeper), "not DeLisi's to give");
            Kill(harness, crucible, S.SmartMine);
            Accept(harness, drake, Minesweeper);
            Assert.IsEmpty(Topics(harness, drake, Minesweeper), "Drake hears no objective");
            var markers = harness.Manager.BuildStatusSnapshot(harness.Client.Player)[Minesweeper].ObjectivesList.Single().IndicatorList;
            CollectionAssert.AreEquivalent(S.MinePools.Select(pool => ((float)pool.At.X, (float)pool.At.Z)).ToArray(),
                markers.Select(marker => (marker.Position.X, marker.Position.Z)).ToArray(), "a marker on each mine pool");
            Assert.IsTrue(markers.All(marker => marker.MapContextId == CrucibleMissionDataV1.MapContextId));

            Kill(harness, crucible, S.Reconstructor);
            for (var mine = 0; mine < 5; mine++)
                KillMine(harness, crucible);
            Assert.AreEqual(5U, Count(harness, Minesweeper), "the mine before the mission and the Reconstructor do not count");

            // An abandon takes the count; taken again, it starts at none.
            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Minesweeper));
            harness.Drain();
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, drake, Minesweeper), "offered again after an abandon");
            Accept(harness, drake, Minesweeper);
            Assert.AreEqual(0U, Count(harness, Minesweeper), "the count starts again");

            var completed = 0;
            var completeable = 0;
            for (var mine = 1; mine <= CrucibleMinesweeperV1.Mines; mine++)
            {
                if (mine == 11)
                {
                    WildernessRanjaGorgeTests.Reload(harness);
                    Assert.AreEqual(10U, Count(harness, Minesweeper), "kept at a reload");
                    harness.Drain();
                }
                Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Minesweeper, CrucibleMinesweeperV1.Destroy), $"open before mine {mine}");
                var drained = KillMine(harness, crucible);
                completed += drained.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Minesweeper);
                completeable += drained.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Minesweeper && packet.IsCompleteable);
            }
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Minesweeper, CrucibleMinesweeperV1.Destroy), "twenty");
            Assert.AreEqual((1, 1), (completed, completeable), "ObjectiveCompleted once, MissionCompleteable once");

            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, delisi.EntityId, Minesweeper, 0), "not DeLisi's to take");
            FullBagWaits(harness, drake, Minesweeper, 2, ArmorFiller);
            FailedTurnInPaysNothing(harness, drake, Minesweeper, 2);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, drake, Minesweeper, 2, CrucibleMinesweeperV1.Experience, CrucibleMinesweeperV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Minesweeper), "MissionRewarded once");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, CrucibleMinesweeperV1.GravitonLegs), "the legs chosen");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, drake, Minesweeper), "done once");
        }

        /// <summary>
        /// Funeral Derge after A Fort Called Intrepid: DeLisi offers it (only he); the client's marker on Derge on the rampart;
        /// another Thrax Commander (520014) does not count; an abandon and a reload keep it open; Derge's kill completes it once;
        /// DeLisi pays with the item chosen, once.
        /// </summary>
        [TestMethod]
        public void FuneralDergeKillsDergeOnTheRampartForDeLisi()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            C.SeedCompleted(harness, LastMen);
            var crucible = C.EnterCrucible(harness, C.FortWaypoint);
            var delisi = Npc(crucible, N.DeLisi);
            var drake = Npc(crucible, N.Drake);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, delisi, Derge), "it waits for A Fort Called Intrepid");
            C.SeedCompleted(harness, Fort);
            crucible = C.EnterCrucible(harness, C.FortWaypoint);
            Spawn(crucible, S.DergePool, 520014);

            var offer = WildernessRanjaGorgeTests.Offer(harness, delisi, Derge);
            CollectionAssert.AreEqual(new[]
                {
                    CrucibleFuneralDergeV1.BioHelmet, CrucibleFuneralDergeV1.MechHelmet, CrucibleFuneralDergeV1.GravitonBoots,
                    CrucibleFuneralDergeV1.ReflectiveVest
                },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, drake, Derge), "not Drake's to give");
            Accept(harness, delisi, Derge);
            Assert.IsEmpty(Topics(harness, delisi, Derge), "DeLisi hears no objective");
            var marker = harness.Manager.BuildStatusSnapshot(harness.Client.Player)[Derge].ObjectivesList.Single().IndicatorList.Single();
            var derge = Alive(crucible).Single(candidate => candidate.DbId == S.Derge);
            Assert.IsLessThan(1f, Vector2.Distance(new(marker.Position.X, marker.Position.Z), new(derge.Position.X, derge.Position.Z)),
                "the marker on Derge");

            Kill(harness, crucible, 520014);
            harness.Drain();
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Derge, CrucibleFuneralDergeV1.Kill), "another Thrax Commander");
            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Derge));
            harness.Drain();
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, delisi, Derge), "offered again after an abandon");
            Accept(harness, delisi, Derge);
            WildernessRanjaGorgeTests.Reload(harness);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Derge, CrucibleFuneralDergeV1.Kill), "open after a reload");
            harness.Drain();

            Kill(harness, crucible, S.Derge);
            var drained = harness.Drain();
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Derge, CrucibleFuneralDergeV1.Kill));
            Assert.AreEqual(1, drained.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Derge), "ObjectiveCompleted once");
            Assert.AreEqual(1, drained.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Derge && packet.IsCompleteable),
                "MissionCompleteable once");

            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, drake.EntityId, Derge, 0), "not Drake's to take");
            FullBagWaits(harness, delisi, Derge, 3, ArmorFiller);
            FailedTurnInPaysNothing(harness, delisi, Derge, 3);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, delisi, Derge, 3, CrucibleFuneralDergeV1.Experience, CrucibleFuneralDergeV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Derge), "MissionRewarded once");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, CrucibleFuneralDergeV1.ReflectiveVest), "the vest chosen");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, delisi, Derge), "done once");
        }

        /// <summary>
        /// Salvage Duty after Junkyard Dogs: Haggus offers it (only he); no part drops before it; each part drops under its
        /// chance only (sequencer 40, pressure booster 50), from a Warden Bot and from a Reconstructor Bot, and not from a Smart
        /// Mine; a death is not a pickup; the counts kept at a reload; each objective completes once; Haggus takes the parts and
        /// pays with the item chosen; More Salvage Duty follows.
        /// </summary>
        [TestMethod]
        public void SalvageDutyTakesTheBotPartsToHaggus()
        {
            var percentile = 0;
            using var harness = WildernessRuntimeTestHarness.Create(lootRoll: (minimum, maximum) => maximum == 100 ? percentile : minimum);
            C.SeedCompleted(harness, LastMen, Fort, Wire, Boss);
            var crucible = C.EnterCrucible(harness, C.LabbnaWaypoint);
            var haggus = Npc(crucible, G.Haggus);
            var rigs = Npc(crucible, N.Rigs);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, haggus, Salvage), "it waits for Junkyard Dogs");
            C.SeedCompleted(harness, Dogs);
            crucible = C.EnterCrucible(harness, C.LabbnaWaypoint);
            Spawn(crucible, BotPools.Concat(MinePools).ToArray());

            var offer = WildernessRanjaGorgeTests.Offer(harness, haggus, Salvage);
            CollectionAssert.AreEqual(new[] { D.ElectricBlade, D.VirulentStaff, D.RocketLauncher, D.Rifle },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, rigs, Salvage), "not Rigs' to give");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, haggus, MoreSalvage), "More Salvage Duty waits for it");
            var early = Kill(harness, crucible, W.WardenBot);
            Assert.IsFalse(Part(early, 0) || Part(early, 1), "no part before the mission");
            Accept(harness, haggus, Salvage);
            Assert.IsEmpty(Topics(harness, haggus, Salvage), "Haggus hears no objective");
            Assert.HasCount(10, harness.Manager.BuildStatusSnapshot(harness.Client.Player)[Salvage].ObjectivesList.SelectMany(objective => objective.IndicatorList),
                "a marker per objective on each of the five bot pools");

            // 50: none; 49 and 40: a pressure booster only; 39: both. A Smart Mine has none.
            percentile = 0;
            var mine = Kill(harness, crucible, S.SmartMine);
            Assert.IsFalse(Part(mine, 0) || Part(mine, 1), "a Smart Mine has no part");
            percentile = 50;
            var none = Kill(harness, crucible, W.WardenBot);
            Assert.IsFalse(Part(none, 0) || Part(none, 1), "50 misses both");
            percentile = 49;
            var boosterAt49 = Kill(harness, crucible, S.Reconstructor);
            Assert.IsFalse(Part(boosterAt49, 0), "49 misses a sequencer");
            Assert.IsTrue(Part(boosterAt49, 1), "49 drops a pressure booster, from a Reconstructor Bot");
            Assert.AreEqual(0U, Held(harness, 1), "a death is not a pickup");
            PickUp(harness, boosterAt49);
            percentile = 40;
            var boosterAt40 = Kill(harness, crucible, W.WardenBot);
            Assert.IsFalse(Part(boosterAt40, 0), "40 misses a sequencer");
            Assert.IsTrue(Part(boosterAt40, 1), "40 drops a pressure booster, from a Warden Bot");
            PickUp(harness, boosterAt40);
            percentile = 39;
            var both = Kill(harness, crucible, S.Reconstructor);
            Assert.IsTrue(Part(both, 0) && Part(both, 1), "39 drops both");
            PickUp(harness, both);
            Assert.AreEqual((1U, 3U), (Held(harness, 0), Held(harness, 1)));
            harness.Drain();

            percentile = 0;
            var completed = new List<uint>();
            var completeable = 0;
            var kills = 0;
            while (Held(harness, 1) < 8)
            {
                if (kills++ == 2)
                {
                    WildernessRanjaGorgeTests.Reload(harness);
                    Assert.AreEqual((3U, 5U), (Held(harness, 0), Held(harness, 1)), "kept at a reload");
                    Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Salvage, 1));
                    harness.Drain();
                }
                var kind = kills % 2 == 0 ? W.WardenBot : S.Reconstructor;
                if (!Alive(crucible).Any(candidate => candidate.DbId == kind && candidate.Npc == null))
                    Respawn(crucible, BotPools);
                PickUp(harness, Kill(harness, crucible, kind));
                var drained = harness.Drain();
                completed.AddRange(drained.OfType<ObjectiveCompletedPacket>().Where(packet => packet.MissionId == Salvage).Select(packet => packet.ObjectiveId));
                completeable += drained.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Salvage && packet.IsCompleteable);
            }
            Assert.AreEqual((6U, 8U), (Held(harness, 0), Held(harness, 1)), "no part over its count");
            CollectionAssert.AreEquivalent(new[] { 1U, 2U }, completed, "ObjectiveCompleted once each");
            Assert.AreEqual(1, completeable, "MissionCompleteable once");

            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, rigs.EntityId, Salvage, 0), "not Rigs' to take");
            FullBagWaits(harness, haggus, Salvage, 1, WeaponFiller);
            FailedTurnInPaysNothing(harness, haggus, Salvage, 1);
            Assert.AreEqual((6U, 8U), (Held(harness, 0), Held(harness, 1)), "kept at the failed turn-in");
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, haggus, Salvage, 1, D.Experience, D.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Salvage), "MissionRewarded once");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, D.VirulentStaff), "the staff chosen");
            Assert.AreEqual((0U, 0U), (Held(harness, 0), Held(harness, 1)), "Haggus takes the parts");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, haggus, Salvage), "done once");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, haggus, MoreSalvage), "More Salvage Duty follows");
        }

        /// <summary>A part stays on the corpse while the bag is full; an abandon keeps the parts, and taken again they count.</summary>
        [TestMethod]
        public void TheSalvagePartsWaitForRoomAndSalvageDutyCanBeAbandoned()
        {
            using var harness = WildernessRuntimeTestHarness.Create(lootRoll: (minimum, maximum) => maximum == 100 ? 0 : minimum);
            C.SeedCompleted(harness, LastMen, Fort, Wire, Boss, Dogs);
            var crucible = C.EnterCrucible(harness, C.LabbnaWaypoint);
            var haggus = Npc(crucible, G.Haggus);
            Spawn(crucible, BotPools);
            Accept(harness, haggus, Salvage);
            WildernessAliaBranchesTests.FillCategory(harness, MissionItemFiller, 50);

            var loot = Kill(harness, crucible, S.Reconstructor);
            PickUp(harness, loot);
            Assert.AreEqual(0U, Held(harness, 0) + Held(harness, 1), "no room");
            Assert.IsTrue(Part(loot, 0), "still on the corpse");
            for (var slot = 0; slot < 2; slot++)
                WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
            PickUp(harness, loot);
            Assert.AreEqual((1U, 1U), (Held(harness, 0), Held(harness, 1)), "picked up once there is room");
            harness.Drain();

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Salvage));
            harness.Drain();
            Assert.AreEqual((1U, 1U), (Held(harness, 0), Held(harness, 1)), "the parts are kept");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, haggus, Salvage), "offered again after an abandon");
            Accept(harness, haggus, Salvage);
            harness.Drain();
            for (var kill = 0; kill < 5; kill++)
            {
                if (!Alive(crucible).Any(candidate => candidate.DbId == W.WardenBot && candidate.Npc == null))
                    Respawn(crucible, WardenPools);
                PickUp(harness, Kill(harness, crucible, W.WardenBot));
            }
            Assert.AreEqual((6U, 6U), (Held(harness, 0), Held(harness, 1)), "the held parts and five more");
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Salvage, 1), "six sequencers");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Salvage, 2), "six of eight pressure boosters");
        }

        /// <summary>
        /// More Salvage Duty after Salvage Duty: Haggus offers it; the rarer parts drop under their chances only (process
        /// controller 25, solenoid valve and vacuum pump 30); the counts are 6, 10 and 10 (the client objective names, triage
        /// decision 2); a reload keeps them; an abandon keeps the parts; Haggus takes them and pays with the module chosen.
        /// </summary>
        [TestMethod]
        public void MoreSalvageDutyTakesTheRarerPartsToHaggus()
        {
            var percentile = 0;
            using var harness = WildernessRuntimeTestHarness.Create(lootRoll: (minimum, maximum) => maximum == 100 ? percentile : minimum);
            C.SeedCompleted(harness, LastMen, Fort, Wire, Boss, Dogs);
            var crucible = C.EnterCrucible(harness, C.LabbnaWaypoint);
            var haggus = Npc(crucible, G.Haggus);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, haggus, MoreSalvage), "it waits for Salvage Duty");
            C.SeedCompleted(harness, Salvage);
            crucible = C.EnterCrucible(harness, C.LabbnaWaypoint);
            Spawn(crucible, BotPools);

            var offer = WildernessRanjaGorgeTests.Offer(harness, haggus, MoreSalvage);
            CollectionAssert.AreEqual(new[] { D.ResistPhotonic, D.RegenBonus, D.ArmorRecharge, D.DebuffFire },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Accept(harness, haggus, MoreSalvage);
            CollectionAssert.AreEquivalent(new[] { 3U, 4U, 5U }, harness.Client.Player.Missions[MoreSalvage].Objectives.Keys.ToArray(),
                "the client's objectives 3, 4 and 5");

            // 30: none; 29 and 25: a valve and a pump; 24: all three.
            percentile = 30;
            var none = Kill(harness, crucible, W.WardenBot);
            Assert.IsFalse(Part(none, 2) || Part(none, 3) || Part(none, 4), "30 misses all three");
            percentile = 29;
            var two = Kill(harness, crucible, S.Reconstructor);
            Assert.IsFalse(Part(two, 2), "29 misses a process controller");
            Assert.IsTrue(Part(two, 3) && Part(two, 4), "29 drops a valve and a pump");
            PickUp(harness, two);
            percentile = 25;
            var twoAt25 = Kill(harness, crucible, W.WardenBot);
            Assert.IsFalse(Part(twoAt25, 2), "25 misses a process controller");
            Assert.IsTrue(Part(twoAt25, 3) && Part(twoAt25, 4), "25 drops a valve and a pump");
            PickUp(harness, twoAt25);
            percentile = 24;
            var all = Kill(harness, crucible, S.Reconstructor);
            Assert.IsTrue(Part(all, 2) && Part(all, 3) && Part(all, 4), "24 drops all three");
            Assert.AreEqual((0U, 2U, 2U), (Held(harness, 2), Held(harness, 3), Held(harness, 4)), "a death is not a pickup");
            PickUp(harness, all);
            harness.Drain();

            // An abandon keeps the parts; taken again they count.
            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, MoreSalvage));
            harness.Drain();
            Assert.AreEqual((1U, 3U, 3U), (Held(harness, 2), Held(harness, 3), Held(harness, 4)), "the parts are kept");
            Accept(harness, haggus, MoreSalvage);
            harness.Drain();

            percentile = 0;
            var completed = new List<uint>();
            var completeable = 0;
            var kills = 0;
            while (Held(harness, 3) < 10)
            {
                if (kills++ == 2)
                {
                    WildernessRanjaGorgeTests.Reload(harness);
                    Assert.AreEqual((3U, 5U, 5U), (Held(harness, 2), Held(harness, 3), Held(harness, 4)), "kept at a reload");
                    harness.Drain();
                }
                if (!Alive(crucible).Any(candidate => candidate.DbId == S.Reconstructor && candidate.Npc == null))
                    Respawn(crucible, ReconstructorPools);
                PickUp(harness, Kill(harness, crucible, S.Reconstructor));
                var drained = harness.Drain();
                completed.AddRange(drained.OfType<ObjectiveCompletedPacket>().Where(packet => packet.MissionId == MoreSalvage).Select(packet => packet.ObjectiveId));
                completeable += drained.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == MoreSalvage && packet.IsCompleteable);
            }
            Assert.AreEqual((6U, 10U, 10U), (Held(harness, 2), Held(harness, 3), Held(harness, 4)), "6, 10 and 10, no part over its count");
            CollectionAssert.AreEquivalent(new[] { 3U, 4U, 5U }, completed, "ObjectiveCompleted once each");
            Assert.AreEqual(1, completeable, "MissionCompleteable once");

            FullBagWaits(harness, haggus, MoreSalvage, 0, ModuleFiller);
            FailedTurnInPaysNothing(harness, haggus, MoreSalvage, 0);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, haggus, MoreSalvage, 0, D.MoreExperience, D.MoreCredits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == MoreSalvage), "MissionRewarded once");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, D.ResistPhotonic), "the module chosen");
            Assert.AreEqual((0U, 0U, 0U), (Held(harness, 2), Held(harness, 3), Held(harness, 4)), "Haggus takes the parts");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, haggus, MoreSalvage), "done once");
        }

        /// <summary>
        /// Fix job J (real client 2026-11-08, to-fix row 75: the mines rose, hit for 80-431 Physical and went off by themselves
        /// 5-10 s up, none shot down, the count stayed 3/20). TaRapedia: "The mines don't actually explode"; a Necromite follows
        /// the player. A mine risen 2 m from the player fights for 30 s and does not go off: it has no self-destruct, death
        /// action or corpse explosion, it winds up nothing but its decay (450/1), it is alive at the end, and it goes after the
        /// player who walks off. The player's kill of it counts 1/20 for 1232. The test does not run MissileManager's worker,
        /// so it does not show the decay land; ASmartMinesHitIsASmallVirulentDamageOverTime covers the DECAY on the player.
        /// </summary>
        [TestMethod]
        public void ARisenSmartMineFollowsThePlayerAndDoesNotGoOff()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            harness.LoadAbilities();
            C.SeedCompleted(harness, LastMen);
            C.SeedCompleted(harness, Fort);
            var crucible = C.EnterCrucible(harness, C.FortWaypoint);
            Spawn(crucible, MinePools);
            Accept(harness, Npc(crucible, N.Drake), Minesweeper);
            harness.Drain();

            var mine = Alive(crucible).First(candidate => candidate.DbId == S.SmartMine);
            Assert.IsTrue(MawLurk.Lurks(mine) && MawLurk.IsBuried(mine), "a minefield mine lies buried");
            var attack = mine.Actions.Single();
            Assert.AreEqual((CrucibleFixJV1.ThraxDecay, CrucibleFixJV1.ThraxDecayArg), ((uint)attack.ActionId, attack.ActionArgId), "its one attack, the Thrax's decay");
            Assert.AreEqual(CreatureEffectAttacks.Kind.DamageOverTime, CreatureEffectAttacks.KindOf(attack.ActionId, attack.ActionArgId), "a damage over time");
            Assert.IsFalse(mine.Actions.Any(action => action.ActionId == CreatureBombs.NecromiteSelfDestruct || CreatureBombs.IsCorpseExplosion(action)
                || CreatureBombs.IsDeathAction(action)), "nothing that goes off");

            var now = System.Environment.TickCount64;
            MawLurk.Clock = () => now;
            var windups = new List<PerformWindupPacket>();
            void Fight(int milliseconds)
            {
                for (var passed = 0; passed < milliseconds; passed += 250)
                {
                    now += 250;
                    BehaviorManager.Instance.MapChannelThink(crucible, 250);
                    // A windup runs on the wall clock (CreatureWindups.Begin: Environment.TickCount64 + 500), and the
                    // creature thinks nothing while it lasts: wait it out, as the server would, so the fight goes on.
                    while (CreatureWindups.IsWindingUp(mine))
                        System.Threading.Thread.Sleep(50);
                    CreatureWindups.Worker(crucible);
                    CreatureBombs.Worker(crucible);
                    windups.AddRange(harness.Drain().OfType<PerformWindupPacket>());
                }
            }
            try
            {
                harness.MoveTo(mine.Position + new Vector3(2, 0, 0));
                MawLurk.Emerge(crucible, mine, harness.Client.Player.EntityId);
                Assert.IsFalse(MawLurk.IsBuried(mine), "up at the player");
                Fight(30000);

                Assert.AreNotEqual(CharacterState.Dead, mine.State, "30 s next to the player: it has not gone off");
                Assert.IsGreaterThan(0, mine.Attributes[Attributes.Health].Current, "whole");
                Assert.IsFalse(CreatureBombs.IsSelfDestructing(mine));
                Assert.IsFalse(windups.Any(windup => windup.ActionId == CreatureBombs.NecromiteSelfDestruct), "no self-destruct wound up");
                Assert.IsTrue(windups.Any(windup => windup.ActionId == attack.ActionId && windup.ActionArgId == attack.ActionArgId), "its decay, at the player");
                Assert.AreEqual(harness.Client.Player.EntityId, mine.Controller.ActionFighting.TargetEntityId, "still at the player");

                // The player walks 8 m off: the mine goes after them.
                var player = harness.Client.Player.Position + new Vector3(8, 0, 0);
                harness.MoveTo(player);
                var before = Vector3.Distance(mine.Position, player);
                Fight(3000);
                Assert.IsLessThan(before - 1, Vector3.Distance(mine.Position, player), "it follows the player");
                Assert.AreNotEqual(CharacterState.Dead, mine.State, "and does not go off");
            }
            finally
            {
                MawLurk.Clock = () => System.Environment.TickCount64;
            }

            mine.Attributes[Attributes.Health].Current = 0;
            harness.Creatures.HandleCreatureKill(crucible, mine, harness.Client.Player);
            harness.Drain();
            Assert.AreEqual(1U, Count(harness, Minesweeper), "the player's kill counts: 1/20");
        }

        /// <summary>
        /// Fix job J: a Smart Mine's hit is a small Virulent damage over time (TaRapedia Necromite: "a small virulent DOT"; the
        /// walkthrough: "very little damage"), not a Physical hit: the Thrax's DECAY 82 on the player, from the mine, a tick of
        /// 20-30 Virulent every 2 s for 10 s (client 450/1: DURATION 10, INTERVAL 2, DAMAGE_TYPE 4; ESTIMATE: 20-30). A tick
        /// lands as Virulent, 30 or less. The mine stays up and whole.
        /// </summary>
        [TestMethod]
        public void ASmartMinesHitIsASmallVirulentDamageOverTime()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            harness.LoadAbilities();
            var crucible = C.EnterCrucible(harness, C.FortWaypoint);
            Spawn(crucible, MinePools);
            var mine = Alive(crucible).First(candidate => candidate.DbId == S.SmartMine);
            var player = harness.Client.Player;
            harness.MoveTo(mine.Position + new Vector3(2, 0, 0));
            MawLurk.Emerge(crucible, mine, player.EntityId);
            harness.Drain();

            var attack = mine.Actions.Single();
            Assert.AreEqual((CrucibleFixJV1.Now.DamageMin, CrucibleFixJV1.Now.DamageMax, (uint)DamageType.Virulent), (attack.MinDamage, attack.MaxDamage,
                (uint)CreatureAttacks.DamageTypeOf(attack)), "the row: 20-30 Virulent a tick");
            Assert.IsTrue(AbilityManager.Instance.TryGetLevel(attack.ActionId, attack.ActionArgId, out var level));
            Assert.AreEqual((10, 2, (int)DamageType.Virulent), (level.Get(AbilityProperty.Duration), level.Get(AbilityProperty.Interval),
                level.Get(AbilityProperty.DamageType)), "the client's 450/1: 10 s, a tick every 2 s, Virulent");

            var start = System.Environment.TickCount64;
            CreatureEffectAttacks.OnHit(crucible, mine, player, new Missile
            {
                ActionId = attack.ActionId, ActionArgId = attack.ActionArgId, Source = mine, CreatureAction = attack
            });
            var dot = player.ActiveEffects.Values.Single(effect => effect.TypeId == CreatureEffectAttacks.DecayTypeId);
            Assert.AreEqual((mine.EntityId, 20, 30, DamageType.Virulent, 2000L, 0), (dot.SourceId, dot.TickDamageMin, dot.TickDamageMax,
                dot.TickDamageType, dot.TickIntervalMs, dot.TickScaleType), "DECAY from the mine: 20-30 Virulent every 2 s, as the row says");
            Assert.IsTrue(dot.ExpiresTick - start is >= 10000 and <= 10250 + 1000, "for 10 s");

            // Armour takes a tick first (ActorManager.Damage): the tick is what armour and health lose together.
            int ArmourAndHealth() => player.Attributes[Attributes.Health].Current
                + (player.Attributes.TryGetValue(Attributes.Armor, out var armour) ? armour.Current : 0);
            var before = ArmourAndHealth();
            dot.NextTickTick = System.Environment.TickCount64 - 1;
            GameEffectManager.Instance.DoWork(crucible, 0);
            var ticks = harness.Drain().OfType<GameEffectTickPacket>().Where(tick => tick.EffectId == dot.EffectId).SelectMany(tick => tick.Entries).ToList();
            Assert.HasCount(1, ticks, "one tick");
            Assert.AreEqual((player.EntityId, DamageType.Virulent), (ticks[0].EntityId, ticks[0].DamageType), "on the player, Virulent");
            Assert.IsTrue(ticks[0].Amount is > 0 and <= 30, $"small: {ticks[0].Amount}");
            Assert.AreEqual(before - ticks[0].Amount, ArmourAndHealth(), "the tick taken off armour and health");
            Assert.AreNotEqual(CharacterState.Dead, mine.State, "the mine stays up");
        }

        /// <summary>
        /// Fix job F's guard, kept by fix job J: no Smart Mine has a self-destruct now, but should one be spent (a World DB
        /// rolled back to set 4's rows under this code), a lurking mine that goes off is still nobody's kill
        /// (CreatureBombs.Spend): no count for 1232, no experience. A mine the player shoots down counts.
        /// </summary>
        [TestMethod]
        public void ASpentLurkingMineIsStillNobodysKill()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            C.SeedCompleted(harness, LastMen);
            C.SeedCompleted(harness, Fort);
            var crucible = C.EnterCrucible(harness, C.FortWaypoint);
            Spawn(crucible, MinePools);
            Accept(harness, Npc(crucible, N.Drake), Minesweeper);
            harness.Drain();

            var mine = Alive(crucible).First(candidate => candidate.DbId == S.SmartMine);
            harness.MoveTo(mine.Position + new Vector3(2, 0, 0));
            MawLurk.Emerge(crucible, mine, harness.Client.Player.EntityId);
            var experience = harness.Client.Player.Experience;

            CreatureBombs.Spend(crucible, mine, harness.Client.Player);
            harness.Drain();

            Assert.AreEqual(CharacterState.Dead, mine.State, "spent");
            Assert.AreEqual(0U, Count(harness, Minesweeper), "not destroyed: no count");
            Assert.AreEqual(experience, harness.Client.Player.Experience, "no experience");

            KillMine(harness, crucible);
            Assert.AreEqual(1U, Count(harness, Minesweeper), "one shot down counts");
        }

        /// <summary>
        /// Fix job F review: the four mines of a point pool are set down one after the other, each buried at once (MawLurk, in no
        /// cell). The places they are kept apart by (Spacing.Taken) take the buried pool-mates too, so they lie on different ring
        /// places round the point (Spacing.OnRings: the point, then 3.3 m out), not all four on the point to rise and go off
        /// together; each within the 8 m lurk range of the point.
        /// </summary>
        [TestMethod]
        public void TheMinesOfAPointPoolLieApartOnItsRings()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var crucible = C.EnterCrucible(harness, C.FortWaypoint);
            Spawn(crucible, MinePools);
            foreach (var (pool, at) in S.MinePools)
            {
                var point = new Vector2((float)at.X, (float)at.Z);
                var mines = Alive(crucible).Where(creature => creature.SpawnPool?.DbId == pool).Distinct().ToList();
                Assert.HasCount((int)S.MinesPerPool, mines, $"pool {pool}");
                Assert.IsTrue(mines.All(MawLurk.IsBuried), $"pool {pool}: buried");
                var places = mines.Select(mine => new Vector2(mine.Position.X, mine.Position.Z)).ToList();
                var where = string.Join("; ", places.Select(place => System.FormattableString.Invariant($"({place.X:0.0}, {place.Y:0.0})")));
                for (var a = 0; a < places.Count; a++)
                {
                    Assert.IsLessThan(MawLurk.LurkRangeOf(mines[a]), Vector2.Distance(places[a], point), $"pool {pool}: within the lurk range of the point: {where}");
                    for (var b = a + 1; b < places.Count; b++)
                        Assert.IsGreaterThanOrEqualTo(Spacing.Distance, Vector2.Distance(places[a], places[b]), $"pool {pool}: apart: {where}");
                }
            }
        }

        /// <summary>
        /// Fix job F (real client 2026-11-07: pools 1 and 3 looked empty three hours after the bot cleared them): a mine pool
        /// cleared (shot down) comes back whole after its respawn time, under the ground (MawLurk: in no cell, on no
        /// client, so the client sees an empty trench). The cause of the "empty" pools: with radius 6 the navmesh draw set the
        /// mines 16-22 m from the point, out of the 8 m lurk range from the marker's centre; fix job F made the pools points, so
        /// every mine lies within 8 m of it. A player 20 m off brings none up; at the pool's point all four come up.
        /// </summary>
        [TestMethod]
        public void AClearedMinePoolComesBackBuriedAndRisesAtItsMarker()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var crucible = C.EnterCrucible(harness, C.FortWaypoint);
            Spawn(crucible, MinePools);
            var (pool, at) = S.MinePools[0];
            var point = new Vector3((float)at.X, (float)at.Y, (float)at.Z);
            List<Creature> Mines() => Alive(crucible).Where(creature => creature.SpawnPool?.DbId == pool).Distinct().ToList();
            Assert.HasCount((int)S.MinesPerPool, Mines());
            Assert.IsTrue(Mines().All(MawLurk.IsBuried), "they lie buried");
            Assert.AreEqual((float)CrucibleFixFV1.PointRadius, crucible.SpawnPools.Single(entry => entry.DbId == pool).Radius, "a point pool (fix job F)");
            bool NearThePoint() => Mines().All(mine => Vector2.Distance(new(mine.Position.X, mine.Position.Z), new(point.X, point.Z)) < MawLurk.LurkRangeOf(mine));
            Assert.IsTrue(NearThePoint(), "each mine lies within its lurk range of the pool's point");

            // Cleared: all four shot down (since fix job J a mine does not go off).
            var mines = Mines();
            foreach (var mine in mines)
            {
                harness.MoveTo(mine.Position + new Vector3(3, 0, 0));
                MawLurk.Emerge(crucible, mine, harness.Client.Player.EntityId);
                mine.Attributes[Attributes.Health].Current = 0;
                harness.Creatures.HandleCreatureKill(crucible, mine, harness.Client.Player);
                Assert.AreEqual(CharacterState.Dead, mine.State);
            }
            harness.Drain();
            Assert.IsEmpty(Mines(), "cleared");
            var spawnPool = crucible.SpawnPools.Single(entry => entry.DbId == pool);
            Assert.AreEqual(0, spawnPool.AliveCreatures + spawnPool.QueuedCreatures + spawnPool.DropshipQueue, "nothing of it left or on its way");

            // Before its respawn time nothing comes; after it, all four, buried.
            harness.MoveTo(C.FortWaypoint);
            var others = crucible.SpawnPools.ToArray();
            crucible.SpawnPools.Clear();
            crucible.SpawnPools.Add(spawnPool);
            try
            {
                SpawnPoolManager.Instance.SpawnPoolWorker(crucible, S.RespawnTenths * 100L - 1000);
                Assert.IsEmpty(Mines(), "not before its 120 s");
                SpawnPoolManager.Instance.SpawnPoolWorker(crucible, 1000);
                SpawnPoolManager.Instance.SpawnPoolWorker(crucible, 0);
            }
            finally
            {
                crucible.SpawnPools.Clear();
                crucible.SpawnPools.AddRange(others);
            }
            Assert.HasCount((int)S.MinesPerPool, Mines(), "back after 120 s");
            Assert.IsTrue(Mines().All(MawLurk.IsBuried), "back under the ground");
            Assert.IsFalse(Living(crucible).Any(creature => creature.SpawnPool?.DbId == pool), "in no cell: the client sees none");

            Assert.IsTrue(NearThePoint(), "each lies within its lurk range of the pool's point again");

            // 20 m off the point on the road east: out of every mine's 8 m.
            harness.MoveTo(point + new Vector3(20, 0, 0));
            MawLurk.Worker(crucible);
            Assert.IsTrue(Mines().All(MawLurk.IsBuried), "20 m off the pool's point: none comes up");

            harness.MoveTo(point);
            MawLurk.Worker(crucible);
            Assert.IsFalse(Mines().Any(MawLurk.IsBuried), "at the marker's centre all four come up: " + string.Join("; ", Mines().Select(mine =>
                System.FormattableString.Invariant($"{mine.EntityId} at ({mine.Position.X:0.0}, {mine.Position.Z:0.0}), {Vector3.Distance(mine.Position, point):0.0} m"))));
            Assert.IsTrue(Mines().All(mine => mine.Controller.ActionFighting.TargetEntityId == harness.Client.Player.EntityId), "at the player");
        }

        /// <summary>
        /// Crucible_fix_j (fix job J, to-fix row 75) puts the Smart Mine back as set 4 wrote it - its weapon 1/109 (row 770014)
        /// and its self-destruct (action 2, row 770016) - and takes out 1232's evidence row 5, and nothing else: every other
        /// Crucible creature and creature_action row and evidence row stays. Up and Down are each safe twice; Up again, the
        /// decay is back. Sets 12b and 15 (20271108002300 and 002400, later migrations: creature 770063 and up, missions 1523, 1525
        /// and 1401) are rolled back with the fix and not put back: their rows are not compared; nor is fix job K's evidence row
        /// (20271108002500, 1208/6).
        /// </summary>
        [TestMethod]
        public void FixJRollsBackWithItsMigration()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            var migrator = world.GetService<IMigrator>();
            Assert.AreEqual(CreatureTints, CrucibleMissionTests.Previous(world, FixJ), "the fix follows set 14");
            Assert.AreEqual(new CrucibleFixJV1.Row("Smart Mine Crucible weapon 1/109", 1, 109, 0.5, 3, 3000, 0, 80, 120, 0), CrucibleFixJV1.Before,
                "set 4's row 770014 (CrucibleMinefieldAndSalvageNpcsV1.Up)");

            List<string> Actions() => world.Set<CreatureActionEntry>().AsNoTracking().Where(row => row.Id >= 770000 && row.Id < SetTwelveBFirstId)
                .OrderBy(row => row.Id).AsEnumerable()
                .Select(row => System.FormattableString.Invariant(
                    $"{row.Id} {row.Description} {row.ActionId} {row.ActionArgId} {row.RangeMin} {row.RangeMax} {row.Cooldown} {row.Windup} {row.MinDamage} {row.MaxDamage} {row.DamageType}"))
                .ToList();
            List<string> Creatures() => world.CreatureEntries.AsNoTracking().Where(row => row.Id >= 770000 && row.Id < SetTwelveBFirstId)
                .OrderBy(row => row.Id).AsEnumerable()
                .Select(row => $"{row.Id} {row.ClassId} {row.Level} {row.MaxHitPoints} {row.Action1} {row.Action2} {row.Action3}").ToList();
            List<(uint, uint)> Evidence() => world.MissionEvidenceEntries.AsNoTracking()
                .Where(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision && !SetTwelveB.Contains(entry.MissionId) &&
                    !(entry.MissionId == CrucibleOutOfSightV1.MissionId && entry.EvidenceId == CrucibleFixKV1.Evidence) &&
                    !(entry.MissionId == CrucibleFixLV1.MissionId && entry.EvidenceId == CrucibleFixLV1.Evidence)).AsEnumerable()
                .OrderBy(entry => entry.MissionId).ThenBy(entry => entry.EvidenceId).Select(entry => (entry.MissionId, entry.EvidenceId)).ToList();
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking()
                .Any(entry => entry.MissionId == mission && entry.ContentRevision == CrucibleMissionDataV1.Revision);
            bool Bot() => world.CreatureEntries.AsNoTracking().Any(row => row.Id == SetTwelveBFirstId);
            CreatureActionEntry Attack() => world.Set<CreatureActionEntry>().AsNoTracking().Single(row => row.Id == CrucibleFixJV1.MineAttack);
            uint Action2() => world.CreatureEntries.AsNoTracking().Single(row => row.Id == S.SmartMine).Action2;
            CrucibleFixJV1.Row Of(CreatureActionEntry row) => new(row.Description, row.ActionId, row.ActionArgId, row.RangeMin, row.RangeMax,
                row.Cooldown, row.Windup, row.MinDamage, row.MaxDamage, row.DamageType);
            void Run(System.Action<MigrationBuilder> step)
            {
                var builder = new MigrationBuilder(world.Database.ProviderName);
                step(builder);
                foreach (var sql in builder.Operations.OfType<SqlOperation>())
                    world.Database.ExecuteSqlRaw(sql.Sql);
                world.ChangeTracker.Clear();
            }

            Assert.AreEqual((CrucibleFixJV1.Now, 0U), (Of(Attack()), Action2()), "the decay, no self-destruct");
            var row5 = world.MissionEvidenceEntries.AsNoTracking().Single(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision &&
                entry.MissionId == Minesweeper && entry.EvidenceId == CrucibleFixJV1.Evidence);
            Assert.AreEqual((MissionEvidenceSourceKind.Reconstruction, CrucibleFixJV1.Source, CrucibleFixJV1.Note, 0.3),
                (row5.SourceKind, row5.SourceUri, row5.ReconstructionNote, row5.Confidence));
            var actions = Actions();
            var creatures = Creatures();
            var evidence = Evidence();
            string Changed(List<string> rows, uint id) => rows.Single(row => row.StartsWith(id + " ", System.StringComparison.Ordinal));

            Assert.AreEqual((true, true), (SetTwelveB.Any(Has), Bot()), "sets 12b and 15 are in");

            // Up twice: nothing more.
            Run(CrucibleFixJV1.Up);
            CollectionAssert.AreEqual(actions, Actions());
            CollectionAssert.AreEqual(creatures, Creatures());
            CollectionAssert.AreEqual(evidence, Evidence());

            migrator.Migrate(CreatureTints);
            world.ChangeTracker.Clear();
            Assert.AreEqual((CrucibleFixJV1.Before, CrucibleFixJV1.Action2Before), (Of(Attack()), Action2()), "set 4's weapon and self-destruct back");
            Assert.AreEqual((false, false), (SetTwelveB.Any(Has), Bot()), "sets 12b and 15, later migrations, are out");
            var downActions = Actions();
            var downCreatures = Creatures();
            CollectionAssert.AreEqual(actions.Where(row => row != Changed(actions, CrucibleFixJV1.MineAttack)).ToList(),
                downActions.Where(row => row != Changed(downActions, CrucibleFixJV1.MineAttack)).ToList(), "every other action row stays (770016 too)");
            CollectionAssert.AreEqual(creatures.Where(row => row != Changed(creatures, S.SmartMine)).ToList(),
                downCreatures.Where(row => row != Changed(downCreatures, S.SmartMine)).ToList(), "every other creature stays");
            CollectionAssert.AreEqual(evidence.Where(entry => entry != (Minesweeper, CrucibleFixJV1.Evidence)).ToList(), Evidence(), "only the fix's row goes");

            // Down twice: nothing more.
            Run(CrucibleFixJV1.Down);
            CollectionAssert.AreEqual(downActions, Actions());
            CollectionAssert.AreEqual(downCreatures, Creatures());

            migrator.Migrate(FixJ);
            world.ChangeTracker.Clear();
            CollectionAssert.AreEqual(actions, Actions());
            CollectionAssert.AreEqual(creatures, Creatures());
            CollectionAssert.AreEqual(evidence, Evidence());
            Assert.AreEqual((false, false), (SetTwelveB.Any(Has), Bot()), "sets 12b and 15 stay out");
        }

        /// <summary>Sets 12b (20271108002300) and 15 (002400), later migrations: missions 1523, 1525 and 1401, creature rows from 770063.</summary>
        private static readonly uint[] SetTwelveB = { CrucibleFactoryFortificationV1.MissionId, CrucibleInformationIsKeyV1.MissionId,
            CrucibleBloodFireSteelV1.MissionId };
        private const uint SetTwelveBFirstId = 770063;
        private const string CreatureTints = "20271108002100_Crucible_creature_tints";
        private const string FixJ = "20271108002200_Crucible_fix_j";

        /// <summary>A Smart Mine killed; the packets it sent.</summary>
        private static List<object> KillMine(WildernessRuntimeTestHarness harness, MapChannel crucible)
        {
            if (!Alive(crucible).Any(candidate => candidate.DbId == S.SmartMine && candidate.Npc == null))
                Respawn(crucible, MinePools);
            Kill(harness, crucible, S.SmartMine);
            return harness.Drain().Cast<object>().ToList();
        }

        /// <summary>The kill counter of the mission's first objective.</summary>
        private static uint Count(WildernessRuntimeTestHarness harness, uint mission) =>
            harness.Client.Player.Missions[mission].Objectives.Values.Single().Counters[0];

        /// <summary>The corpse holds part <paramref name="index"/> (CrucibleSalvageDutyV1.Parts).</summary>
        private static bool Part(LootDispenser loot, int index) => Holds(loot, D.Parts[index].Template);

        /// <summary>How many of part <paramref name="index"/> the player holds.</summary>
        private static uint Held(WildernessRuntimeTestHarness harness, int index) => WildernessRanjaGorgeTests.Held(harness, D.Parts[index].Template);
    }
}
