extern alias RasaGame;

using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Managers;
using Rasa.Packets.MapChannel.Client;
using Rasa.Packets.Mission.Server;
using Rasa.Services.Preloader.Missions.Pools;
using Rasa.Structures;
using Rasa.Test.Missions.Plateau;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions.Pools
{
    using static PlateauCampResistanceTests;

    /// <summary>
    /// The Bane chain at the western fort, played through as a player plays it: Sabotage the Tower (937: Feldman's six
    /// generators, ten lockers and four Linkers round the Eloh obelisk), Bane Jamming (938: four machines on the obelisk,
    /// six jammers planted) and Elimination (941: Washington's Overseer Prysiam).
    ///
    /// Why these tests: the obelisk is a plain model the server now puts down; 937's objective shows two counters at once
    /// (the engine's counter groups); its Linkers are new creatures in the player's own scene; the jammers come on accept
    /// and go in any order. A count on the wrong counter, a completion at the first full count, a piece counted twice, a
    /// jammer kept after an abandon, or progress lost at a reload would each break them.
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class PoolsBaneChainTests
    {
        private const uint Sabotage = PoolsSabotageTheTowerV1.MissionId;
        private const uint Jamming = PoolsBaneJammingV1.MissionId;
        private const uint Elimination = PoolsEliminationV1.MissionId;
        private const uint Feldman = PoolsSnakePitContactsV1.Feldman, Snake = PoolsEliminationV1.Snake;
        private const uint Equipment = PoolsSabotageTheTowerV1.Equipment, Linkers = PoolsSabotageTheTowerV1.Linkers;
        private const uint MissionItemFiller = 11519;      // as PlateauCampResistanceTests

        /// <summary>The Snake Pit's waypoint (World DB teleporter 48), where a player arrives.</summary>
        private static readonly Vector3 SnakePitWaypoint = new(-941.7f, 919.94f, 367.4f);

        [TestMethod]
        public void TheObeliskStandsAtTheFortsRearAsScenery()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pools = PlateauValverdePoolsTests.OntoPools(harness);
            var row = harness.World.WorldDestructibleEntries.AsNoTracking().Single(entry => entry.Id == PoolsObeliskV1.Id);
            Assert.AreEqual((1304U, PoolsObeliskV1.ObeliskClass), (row.MapContextId, row.ClassId));
            var at = PoolsObeliskV1.At;
            Assert.AreEqual(new Vector3((float)at.X, (float)at.Y, (float)at.Z), row.Position);
            Assert.IsNull(PlacedDestructibles.WhyNot((EntityClasses)row.ClassId, out var kind));
            Assert.AreEqual(PlacedDestructibles.Kind.Scenery, kind);
            AssertOnTheGround(pools, row.Position, "obelisk");
            AssertWalkedTo(pools, row.Position, "obelisk");
        }

        [TestMethod]
        public void SabotageTheTowerCountsGeneratorsAndLockersApartAndTheLinkers()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pools = PlateauValverdePoolsTests.OntoPools(harness);
            var feldman = Npc(pools, Feldman);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, feldman, Sabotage), "not before Bane Investigation");
            SeedCompleted(harness, PoolsBaneInvestigationV1.MissionId);
            Assert.IsFalse(pools.DynamicObjects.Any(obj => obj.SceneMissionId == Sabotage), "nothing before the mission");

            var offer = WildernessRanjaGorgeTests.Offer(harness, feldman, Sabotage);
            WildernessRanjaGorgeTests.AssertSelectable(offer, Sabotage,
                (PoolsSabotageTheTowerV1.DirectRepairTool, 28716), (PoolsSabotageTheTowerV1.ElectricPistol, 27096),
                (PoolsSabotageTheTowerV1.VirulentDisperser, 27187), (PoolsSabotageTheTowerV1.EmpTorqueshellRifle, 27375));
            Accept(harness, feldman, Sabotage);

            var generators = Pieces(harness, pools, 1, 6, PoolsSabotageTheTowerV1.GeneratorClass, PoolsSabotageTheTowerV1.Generators);
            var lockers = Pieces(harness, pools, 7, 16, PoolsSabotageTheTowerV1.LockerClass, PoolsSabotageTheTowerV1.Lockers);
            var linkers = LinkersOf(harness, pools);
            Assert.HasCount(4, linkers);
            foreach (var (linker, index) in linkers.Select((linker, index) => (linker, index)))
            {
                Assert.AreEqual(PoolsLinkerV1.Linker, linker.DbId);
                Assert.AreEqual(PoolsLinkerV1.Level, (uint)linker.Level);
                Assert.AreEqual(TargetCategory.Hostile, linker.TargetCategory, "a Linker fights");
                var at = PoolsSabotageTheTowerV1.LinkersAt[index];
                Assert.AreEqual(new Vector3(at.X, at.Y, at.Z), linker.SpawnPool.Position);
                AssertOnTheGround(pools, linker.SpawnPool.Position, linker.SpawnPool.SceneActorRole);
            }
            harness.Drain();

            // A generator: counter 0 only, out of 6.
            Damage(harness, pools, generators[0], 99);
            Assert.AreEqual((0U, 0U), Counts(harness));
            Damage(harness, pools, generators[0], 1);
            var packets = harness.Drain();
            Assert.AreEqual((uint)PoolsSabotageTheTowerV1.DestroyedState, (uint)generators[0].StateId);
            Assert.IsFalse(generators[0].IsEnabled);
            Assert.AreEqual((1U, 0U), Counts(harness));
            var counter = packets.OfType<UpdateObjectiveCounterPacket>().Single(packet => packet.MissionId == Sabotage);
            Assert.AreEqual((Equipment, 0U, 1U, 0U, 6U), (counter.ObjectiveId, counter.CounterId, counter.CounterValue,
                counter.InitialValue, counter.TargetValue), "Vital Bane Equipment Destroyed 1/6");
            Damage(harness, pools, generators[0], 100);
            Assert.AreEqual((1U, 0U), Counts(harness), "one piece counts once");

            // A locker: counter 1 only, out of 10.
            Damage(harness, pools, lockers[0], 100);
            counter = harness.Drain().OfType<UpdateObjectiveCounterPacket>().Single(packet => packet.MissionId == Sabotage);
            Assert.AreEqual((Equipment, 1U, 1U, 0U, 10U), (counter.ObjectiveId, counter.CounterId, counter.CounterValue,
                counter.InitialValue, counter.TargetValue), "Bane Equipment Locker Destroyed 1/10");
            Assert.AreEqual((1U, 1U), Counts(harness));

            // Both counts are saved.
            WildernessRanjaGorgeTests.Reload(harness);
            Assert.AreEqual((1U, 1U), Counts(harness), "kept at a reload");
            harness.Drain();

            // All six generators: objective 1 still waits for the lockers.
            foreach (var generator in generators.Skip(1))
                Damage(harness, pools, generator, 100);
            Assert.AreEqual((6U, 1U), Counts(harness));
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Sabotage, Equipment), "the lockers are not down");

            // 2: the four Linkers.
            for (var index = 0; index < linkers.Length; index++)
            {
                Kill(harness, pools, linkers[index]);
                Assert.AreEqual((uint)index + 1, harness.Client.Player.Missions[Sabotage].Objectives[Linkers].Counters[0], "Linkers eliminated");
            }
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Sabotage, Linkers));
            Assert.IsFalse(harness.Client.Player.Missions[Sabotage].Completeable, "the lockers first");

            foreach (var locker in lockers.Skip(1).Take(8))
                Damage(harness, pools, locker, 100);
            Assert.AreEqual((6U, 9U), Counts(harness));
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Sabotage, Equipment));
            harness.Drain();
            Damage(harness, pools, lockers[9], 100);
            var last = harness.Drain();
            Assert.AreEqual((6U, 10U), Counts(harness));
            Assert.AreEqual(1, last.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Sabotage &&
                packet.ObjectiveId == Equipment), "ObjectiveCompleted once");
            Assert.IsTrue(last.OfType<MissionCompleteablePacket>().Single(packet => packet.MissionId == Sabotage).IsCompleteable);

            FailedTurnInPaysNothing(harness, feldman, Sabotage, 3);
            FullBagWaits(harness, feldman, Sabotage, 3, PoolsSabotageTheTowerV1.ElectricPistol);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, feldman, Sabotage, 3,
                PoolsSabotageTheTowerV1.Experience, PoolsSabotageTheTowerV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Sabotage));
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PoolsSabotageTheTowerV1.EmpTorqueshellRifle), "the selected rifle");
        }

        /// <summary>
        /// A generator and a locker in one batch (as a group's shared kill credit can bring): both counters move, saved and
        /// sent, counter 0 before counter 1; the login snapshot then carries both with their own targets.
        /// </summary>
        [TestMethod]
        public void SabotageTheTowerMovesBothCountsInOneBatch()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pools = PlateauValverdePoolsTests.OntoPools(harness);
            var feldman = Npc(pools, Feldman);
            SeedCompleted(harness, PoolsBaneInvestigationV1.MissionId);
            Accept(harness, feldman, Sabotage);

            Assert.IsTrue(harness.Manager.RecordProgress(harness.Client, new[]
            {
                MissionProgressEvent.Scenario(Sabotage, PoolsSabotageTheTowerV1.EquipmentScenario, 2),
                MissionProgressEvent.Scenario(Sabotage, PoolsSabotageTheTowerV1.EquipmentScenario, 8)
            }));
            var counters = harness.Drain().OfType<UpdateObjectiveCounterPacket>().Where(packet => packet.MissionId == Sabotage)
                .Select(packet => (packet.ObjectiveId, packet.CounterId, packet.CounterValue, packet.TargetValue)).ToArray();
            CollectionAssert.AreEqual(new[] { (Equipment, 0U, 1U, 6U), (Equipment, 1U, 1U, 10U) }, counters, "both, counter 0 first");
            Assert.AreEqual((1U, 1U), Counts(harness));

            WildernessRanjaGorgeTests.Reload(harness);
            Assert.AreEqual((1U, 1U), Counts(harness), "both saved");
            var info = harness.Client.Player.Missions[Sabotage];
            var snapshot = harness.Manager.LoadedMissions[Sabotage].CreateInfo(info.State, info.Completeable, info.Objectives);
            var equipment = snapshot.ObjectivesList.Single(objective => objective.ObjectiveId == Equipment);
            CollectionAssert.AreEquivalent(new[] { (0U, 1U, 6U), (1U, 1U, 10U) },
                equipment.Counters.Select(entry => (entry.Key, entry.Value.CounterValue, entry.Value.TargetValue)).ToArray(),
                "the login snapshot sends both counts");
        }

        /// <summary>The Linkers first, then the equipment: the order does not matter. An abandon takes all; taken again, all stand anew.</summary>
        [TestMethod]
        public void SabotageTheTowerAbandonedTakesItsEquipmentAndLinkers()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pools = PlateauValverdePoolsTests.OntoPools(harness);
            var feldman = Npc(pools, Feldman);
            SeedCompleted(harness, PoolsBaneInvestigationV1.MissionId);
            Accept(harness, feldman, Sabotage);
            Kill(harness, pools, LinkersOf(harness, pools)[2]);
            Damage(harness, pools, Pieces(harness, pools, 7, 16, PoolsSabotageTheTowerV1.LockerClass, PoolsSabotageTheTowerV1.Lockers)[4], 100);
            Assert.AreEqual((0U, 1U), Counts(harness));

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Sabotage));
            harness.Drain();
            Assert.IsFalse(pools.DynamicObjects.Any(obj => obj.SceneMissionId == Sabotage && obj.IsInWorld), "the equipment goes");
            Assert.IsEmpty(LinkersOf(harness, pools, require: false), "the Linkers go");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, feldman, Sabotage), "offered again after an abandon");

            Accept(harness, feldman, Sabotage);
            Assert.AreEqual((0U, 0U), Counts(harness));
            Assert.IsTrue(Pieces(harness, pools, 1, 6, PoolsSabotageTheTowerV1.GeneratorClass, PoolsSabotageTheTowerV1.Generators)
                .All(piece => piece.IsEnabled), "six new generators");
            Assert.HasCount(4, LinkersOf(harness, pools), "four new Linkers");
        }

        [TestMethod]
        public void BaneJammingDestroysTheObelisksMachinesAndPlantsSixJammersInAnyOrder()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pools = PlateauValverdePoolsTests.OntoPools(harness);
            var feldman = Npc(pools, Feldman);
            SeedCompleted(harness, PoolsBaneInvestigationV1.MissionId);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, feldman, Jamming), "not before Sabotage the Tower");
            SeedCompleted(harness, Sabotage);

            var offer = WildernessRanjaGorgeTests.Offer(harness, feldman, Jamming);
            WildernessRanjaGorgeTests.AssertSelectable(offer, Jamming,
                (PoolsBaneJammingV1.GravitonGloves, 15058), (PoolsBaneJammingV1.HazmatLegs, 13786),
                (PoolsBaneJammingV1.MotorAssistLegs, 16376), (PoolsBaneJammingV1.ReflectiveLegs, 18580));
            Accept(harness, feldman, Jamming);
            Assert.AreEqual(PoolsBaneJammingV1.Jammers, Jammers(harness), "six jammers on accept");
            for (var number = 1; number <= 6; number++)
                Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Jamming, PoolsBaneJammingV1.Place(number)), $"jammer {number} open");

            var machines = Enumerable.Range(0, 4).Select(index => pools.DynamicObjects.Single(obj => obj.SceneMissionId == Jamming &&
                obj.SceneActorRole == PoolsBaneJammingV1.MachineryRole(index))).ToArray();
            for (var index = 0; index < machines.Length; index++)
            {
                var at = PoolsBaneJammingV1.Machinery[index];
                Assert.AreEqual(PoolsBaneJammingV1.MachineryClass, (uint)machines[index].EntityClassId);
                Assert.AreEqual(new Vector3(at.X, at.Y, at.Z), machines[index].Position);
                Assert.AreEqual(harness.Client.Player.Id, machines[index].SceneOwnerCharacterId, "the player's own");
                AssertOnTheGround(pools, machines[index].Position, $"machine {index + 1}");
            }
            var spots = Enumerable.Range(1, 6).Select(number => pools.DynamicObjects.Single(obj => obj.SceneMissionId == Jamming &&
                obj.SceneActorRole == PoolsBaneJammingV1.SpotRole(number))).ToArray();
            for (var index = 0; index < spots.Length; index++)
            {
                var at = PoolsBaneJammingV1.Spots[index];
                Assert.AreEqual(PoolsBaneJammingV1.SpotClass, (uint)spots[index].EntityClassId);
                Assert.AreEqual(new Vector3(at.X, at.Y, at.Z), spots[index].Position);
                AssertOnTheGround(pools, spots[index].Position, $"jammer {index + 1}");
                AssertWalkedTo(pools, spots[index].Position, $"jammer {index + 1}");
            }
            harness.Drain();

            // Jammers in any order; a spot takes one once.
            var planted = 0U;
            foreach (var number in new[] { 5, 1, 3, 6, 2, 4 })
            {
                var spot = spots[number - 1];
                harness.MoveTo(spot.Position + new Vector3(0, 0, 1));
                Use(harness, pools, spot, recovers: true);
                planted++;
                Assert.AreEqual(PoolsBaneJammingV1.Jammers - planted, Jammers(harness), $"jammer {number} takes one");
                Assert.AreEqual((UseObjectState)PoolsBaneJammingV1.SpotPlantedState, spot.StateId);
                Assert.IsFalse(spot.IsEnabled);
                Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Jamming, PoolsBaneJammingV1.Place(number)));
                if (number == 5)
                {
                    Use(harness, pools, spot, recovers: false);
                    Assert.AreEqual(5U, Jammers(harness), "one jammer a spot");
                }
                if (planted == 3)
                {
                    WildernessRanjaGorgeTests.Reload(harness);
                    Assert.AreEqual(3U, Jammers(harness), "kept at a reload");
                    Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Jamming, PoolsBaneJammingV1.Place(3)));
                    harness.Drain();
                }
            }
            Assert.IsFalse(harness.Client.Player.Missions[Jamming].Completeable, "the machines first");

            // 1: the four machines, counted once each.
            for (var index = 0; index < machines.Length; index++)
            {
                Damage(harness, pools, machines[index], 100);
                Damage(harness, pools, machines[index], 100);
                Assert.AreEqual((uint)index + 1, harness.Client.Player.Missions[Jamming].Objectives[PoolsBaneJammingV1.Destroy].Counters[0]);
            }
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Jamming, PoolsBaneJammingV1.Destroy));
            Assert.IsTrue(harness.Client.Player.Missions[Jamming].Completeable);

            FailedTurnInPaysNothing(harness, feldman, Jamming, 1);
            FullBagWaits(harness, feldman, Jamming, 1, PoolsBaneJammingV1.GravitonGloves);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, feldman, Jamming, 1,
                PoolsBaneJammingV1.Experience, PoolsBaneJammingV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Jamming));
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PoolsBaneJammingV1.HazmatLegs), "the selected legs");
        }

        /// <summary>A full bag holds the jammers back at the accept; an abandon takes them.</summary>
        [TestMethod]
        public void BaneJammingAbandonedTakesTheJammers()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pools = PlateauValverdePoolsTests.OntoPools(harness);
            var feldman = Npc(pools, Feldman);
            SeedCompleted(harness, PoolsBaneInvestigationV1.MissionId, Sabotage);
            Accept(harness, feldman, Jamming);
            var spot = pools.DynamicObjects.Single(obj => obj.SceneMissionId == Jamming && obj.SceneActorRole == PoolsBaneJammingV1.SpotRole(4));
            harness.MoveTo(spot.Position + new Vector3(0, 0, 1));
            Use(harness, pools, spot, recovers: true);
            Assert.AreEqual(5U, Jammers(harness));

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Jamming));
            harness.Drain();
            Assert.AreEqual(0U, Jammers(harness), "the jammers go with the mission");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, feldman, Jamming), "offered again after an abandon");
            Accept(harness, feldman, Jamming);
            Assert.AreEqual(PoolsBaneJammingV1.Jammers, Jammers(harness), "six again");
        }

        [TestMethod]
        public void EliminationIsWashingtonsAndEndsWithPrysiam()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pools = PlateauValverdePoolsTests.OntoPools(harness);
            var snake = Npc(pools, Snake);
            var feldman = Npc(pools, Feldman);
            SeedCompleted(harness, PoolsBaneInvestigationV1.MissionId);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, snake, Elimination), "not before Sabotage the Tower");
            SeedCompleted(harness, Sabotage);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, feldman, Elimination), "Washington's, not Feldman's");

            var offer = WildernessRanjaGorgeTests.Offer(harness, snake, Elimination);
            WildernessRanjaGorgeTests.AssertSelectable(offer, Elimination,
                (PoolsEliminationV1.Chaingun, 27065), (PoolsEliminationV1.IncendiaryNetGun, 27082),
                (PoolsEliminationV1.ExothermicPolarityGun, 27150), (PoolsEliminationV1.Shotgun, 27325));
            Accept(harness, snake, Elimination);

            Spawn(pools, PoolsEliminationV1.Prysiam);
            var prysiam = Living(pools).Single(creature => creature.SpawnPool?.DbId == PoolsEliminationV1.Prysiam);
            Assert.AreEqual(PoolsEliminationV1.Prysiam, prysiam.DbId);
            harness.Drain();
            Kill(harness, pools, prysiam);
            Assert.AreEqual(1U, harness.Client.Player.Missions[Elimination].Objectives[PoolsEliminationV1.Kill].Counters[0], "Prysiam Killed");
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Elimination, PoolsEliminationV1.Kill));
            Assert.IsTrue(harness.Client.Player.Missions[Elimination].Completeable);

            FailedTurnInPaysNothing(harness, snake, Elimination, 0);
            FullBagWaits(harness, snake, Elimination, 0, PoolsEliminationV1.Shotgun);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, snake, Elimination, 0,
                PoolsEliminationV1.Experience, PoolsEliminationV1.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Elimination));
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PoolsEliminationV1.Chaingun), "the selected chaingun");
        }

        /// <summary>937's pieces <paramref name="first"/>..<paramref name="last"/>: the player's own, intact, on the navmesh.</summary>
        private static DynamicObject[] Pieces(WildernessRuntimeTestHarness harness, MapChannel pools, uint first, uint last, uint classId,
            Rasa.Missions.Scenes.ScenePosition[] places)
        {
            var pieces = Enumerable.Range((int)first, (int)(last - first + 1)).Select(id => pools.DynamicObjects.Single(obj =>
                obj.SceneMissionId == Sabotage && obj.SceneActorRole == PoolsSabotageTheTowerV1.EquipmentRole((uint)id) && obj.IsInWorld))
                .ToArray();
            for (var index = 0; index < pieces.Length; index++)
            {
                var at = places[index];
                Assert.AreEqual(classId, (uint)pieces[index].EntityClassId);
                Assert.AreEqual(new Vector3(at.X, at.Y, at.Z), pieces[index].Position);
                Assert.AreEqual(harness.Client.Player.Id, pieces[index].SceneOwnerCharacterId, "the player's own");
                AssertOnTheGround(pools, pieces[index].Position, pieces[index].SceneActorRole);
                AssertWalkedTo(pools, pieces[index].Position, pieces[index].SceneActorRole);
            }
            return pieces;
        }

        private static Creature[] LinkersOf(WildernessRuntimeTestHarness harness, MapChannel pools, bool require = true)
        {
            var linkers = Enumerable.Range(0, PoolsSabotageTheTowerV1.LinkersAt.Length)
                .Select(index => Living(pools).SingleOrDefault(creature => creature.SpawnPool?.ScenarioMissionId == Sabotage &&
                    creature.SpawnPool.SceneActorRole == PoolsSabotageTheTowerV1.LinkerRole(index) &&
                    creature.SpawnPool.ScenarioOwnerCharacterId == harness.Client.Player.Id))
                .Where(creature => creature != null).ToArray();
            if (require)
                Assert.HasCount(PoolsSabotageTheTowerV1.LinkersAt.Length, linkers, "four Linkers");
            return linkers;
        }

        /// <summary>937's objective 1: (vital equipment, lockers).</summary>
        private static (uint, uint) Counts(WildernessRuntimeTestHarness harness)
        {
            var counters = harness.Client.Player.Missions[Sabotage].Objectives[Equipment].Counters;
            return (counters[0], counters[1]);
        }

        private static uint Jammers(WildernessRuntimeTestHarness harness) =>
            WildernessRanjaGorgeTests.Held(harness, PoolsBaneJammingV1.JammerTemplate);

        /// <summary>A weapon hit on the piece, as PlateauMartyrsCanyonTests.DamagePump (private).</summary>
        private static void Damage(WildernessRuntimeTestHarness harness, MapChannel pools, DynamicObject piece, int damage)
        {
            harness.MoveTo(piece.Position + new Vector3(0, 0, 1));
            var action = new ActionData(harness.Client.Player, ActionId.WeaponAttack, 133, 0) { TargetId = piece.EntityId };
            MissileManager.Instance.MissileLaunch(pools, action, damage);
            MissileManager.Instance.DoWork(pools, 1000);
        }

        /// <summary>The creature dies to the player; the scenes see it (as PlateauHolyVengeanceTests.Kill, private).</summary>
        private static void Kill(WildernessRuntimeTestHarness harness, MapChannel pools, Creature creature)
        {
            harness.MoveTo(creature.Position);
            creature.Attributes[Attributes.Health].Current = 0;
            harness.Creatures.HandleCreatureKill(pools, creature, harness.Client.Player);
            harness.Manager.Scenes.Tick(pools);
            harness.Drain();
        }

        private static void AssertOnTheGround(MapChannel pools, Vector3 position, string what)
        {
            var ground = pools.NavMesh.GroundHeight(position);
            Assert.IsNotNull(ground, $"navmesh under {what}");
            Assert.AreEqual(ground.Value, position.Y, 0.5f, $"{what} on the ground");
        }

        private static void AssertWalkedTo(MapChannel pools, Vector3 position, string what) =>
            Assert.IsTrue(pools.NavMesh.FindPath(SnakePitWaypoint, position, out var complete) != null && complete, $"{what} is walked to");

        /// <summary>A turn-in whose save fails pays nothing; the mission stays to be turned in.</summary>
        private static void FailedTurnInPaysNothing(WildernessRuntimeTestHarness harness, Creature receiver, uint mission, int? choice)
        {
            harness.MoveTo(receiver.Position);
            harness.Drain();
            var before = harness.Context.ReadRewardTotals();
            harness.Context.BeforeSave = _ => throw new DbUpdateException($"Injected {mission} turn-in failure.");
            try
            {
                Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, receiver.EntityId, mission, choice));
            }
            finally
            {
                harness.Context.BeforeSave = null;
            }
            Assert.AreEqual(before, harness.Context.ReadRewardTotals());
            Assert.IsTrue(harness.Client.Player.Missions[mission].Completeable, "still to be turned in");
            harness.Drain();
        }

        /// <summary>A full bag and inbox: the turn-in pays nothing; one slot freed, the caller turns it in.</summary>
        private static void FullBagWaits(WildernessRuntimeTestHarness harness, Creature receiver, uint mission, int? choice, uint filler)
        {
            WildernessAliaBranchesTests.FillCategory(harness, filler, 50);
            harness.FillInbox();
            harness.MoveTo(receiver.Position);
            var before = harness.Context.ReadRewardTotals();
            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, receiver.EntityId, mission, choice), "no room");
            Assert.AreEqual(before, harness.Context.ReadRewardTotals());
            Assert.IsTrue(harness.Client.Player.Missions[mission].Completeable, "still to be turned in");
            WildernessAliaBranchesTests.FreeOneSlot(harness, filler);
            harness.Drain();
        }

        private static void Accept(WildernessRuntimeTestHarness harness, Creature npc, uint mission)
        {
            harness.MoveTo(npc.Position);
            harness.Drain();
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, npc.EntityId, mission), $"{mission} accepted");
            harness.Drain();
        }

        private static Creature Npc(MapChannel pools, uint pool)
        {
            Spawn(pools, pool);
            return RequireNpc(pools, pool);
        }

        /// <summary>A use of the object: the request, then its recovery if one is due (as PoolsSnakePitContactsTests).</summary>
        private static void Use(WildernessRuntimeTestHarness harness, MapChannel map, DynamicObject obj, bool recovers)
        {
            var count = map.PerformRecovery.Count;
            harness.Objects.RequestUseObjectPacket(harness.Client, new RequestUseObjectPacket
            {
                EntityId = obj.EntityId, ActionId = ActionId.UseObject, ActionArgId = PoolsBaneJammingV1.UseArgument
            });
            Assert.AreEqual(count + (recovers ? 1 : 0), map.PerformRecovery.Count, $"use of {obj.SceneActorRole}");
            if (!recovers)
                return;
            var recovery = map.PerformRecovery.Last();
            map.PerformRecovery.Remove(recovery);
            ActorActionManager.Instance.PerformRecovery(map, recovery);
        }

        private static MissionObjectiveState Objective(WildernessRuntimeTestHarness harness, uint mission, uint objective) =>
            harness.Client.Player.Missions[mission].Objectives[objective].State;
    }
}
