using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Managers;
using Rasa.Packets.MapChannel.Client;
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
    using P = MarshesPreoDasTests;
    using E = MarshesEscortsTests;
    using static PlateauCampResistanceTests;
    using C = CrucibleOutpostIntrepidTests;
    using K = CrucibleKardashAndPrometheusNpcsV1;
    using T = CrucibleKhulagoStrikesNpcsV1;
    using J = CruciblePumpUpTheJamV1;
    using Z = CrucibleStrikeZoneV1;

    /// <summary>
    /// Crucible set 9, played through as a player plays it: Captain Tiersky's Pump Up The Jam (1224: six Bane jamming towers on
    /// the rim of Khulago Base) and Major Keplinger's Strike Zone (1214: three GPS Beacons placed inside the base). With them
    /// the set's places: Tiersky moved off the Prometheus Outpost control point, the towers and the beacon spots on the
    /// navmesh, walked to with no lava on the walk.
    ///
    /// Why these tests: Tiersky moved; the towers are six personal destructible objects (one objective each) and the spots
    /// three personal item acceptors, with the beacons a mission item. A tower counted twice or on the wrong objective, an
    /// offer before its prerequisite, a destroyed tower back at a reload, a beacon kept after an abandon or a spot that takes
    /// two, a turn-in that pays twice or on a full bag would each break them.
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class CrucibleKhulagoStrikesTests
    {
        private const uint Jam = J.MissionId, Strike = Z.MissionId;
        private const uint ArmorFiller = 12748;            // Armor_T3_Bio_V01_CMN_Gloves_30_to_34 (inventory category 1)
        private const uint ClientTowerClass = 10282;       // UsableInertDestBaneSonicTowerV01 (written out, not the data class's)
        private const uint ClientTowerName = 4;            // usablenameoverride GUARDPROM_JAMMINGTOWER "Biomechanical Jamming Tower"
        private const uint ClientBeaconTemplate = 50714;   // client item class 25258 MisCrucibleItemGPSbeacons "GPS Beacon"
        private const uint ClientSpotClass = 7847;         // UsableItemAccHumGpsBeacon "GPS Beacon"
        private const uint ClientSpotUseArgument = 3;      // usabledata lookup[7847][0]
        private const uint ClientTowers = 6, ClientBeacons = 3;   // client missionobjective 1224,1-6 and 1214,1-3
        private const uint Intact = 110, Destroyed = 2, SpotReady = 119, SpotPlanted = 121;

        private static readonly Vector3 Prometheus = P.ToVector(K.PrometheusWaypoint);
        /// <summary>Confirmed Kills' north trench marker: the way into Khulago Base with no lava from the waypoint (CrucibleKardashAndPrometheusTests).</summary>
        private static readonly Vector3 NorthTrench = P.ToVector(CrucibleConfirmedKillsV1.TrenchesAt[1]);

        [TestInitialize]
        public void Clock() => PlateauGiantKillerSquadTests.UseClock();

        [TestCleanup]
        public void Restore() => PlateauGiantKillerSquadTests.RestoreClock();

        /// <summary>
        /// Tiersky friendly at his new place, out of the control point's model (12 m or more from its centre), on the navmesh, 5 m
        /// or more from a wall, walked to from the Prometheus Outpost waypoint with no lava; each tower and beacon spot on the
        /// navmesh, 5 m or more from a wall, walked to from the north trench marker with no lava, no navmesh lava within 20 m,
        /// 20 m or more outside every hostile pool's circle.
        /// </summary>
        [TestMethod]
        public void TheKhulagoStrikePlacesStandWhereTheyAreWalkedTo()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var crucible = C.EnterCrucible(harness, Prometheus);
            var name = crucible.MapInfo.MapName;
            var tiersky = Npc(crucible, T.Tiersky);
            Assert.AreNotEqual(TargetCategory.Hostile, tiersky.TargetCategory);
            Assert.AreEqual(9214U, tiersky.NameId, "the client's Captain Tiersky");
            AssertOnGround(crucible, tiersky.Position, "Tiersky");
            Assert.IsLessThan(0.1f, Vector2.Distance(new(tiersky.Position.X, tiersky.Position.Z), new((float)T.TierskyAt.X, (float)T.TierskyAt.Z)), "Tiersky at set 9's place");
            Assert.IsGreaterThanOrEqualTo(5f, crucible.NavMesh.DistanceToWall(tiersky.Position, 20) ?? 0, "Tiersky 5 m or more from a wall");
            Assert.IsGreaterThanOrEqualTo(12f, Vector2.Distance(new(tiersky.Position.X, tiersky.Position.Z), new(400.1f, 387.9f)),
                "Tiersky out of the control point's model (12 x 17 m round (400.1, 387.9))");
            AssertWalkWithoutLava(crucible, name, Prometheus, tiersky.Position, "Tiersky from the Prometheus Outpost waypoint");

            var hostiles = harness.World.SpawnPoolEntries.AsEnumerable().Where(row => row.MapContextId == CrucibleMissionDataV1.MapContextId)
                .Where(row => harness.Creatures.LoadedCreatures.TryGetValue(row.Creature1Id, out var c) && c.TargetCategory == TargetCategory.Hostile).ToList();
            AssertWalkWithoutLava(crucible, name, Prometheus, NorthTrench, "the north trench marker from the Prometheus Outpost waypoint");
            foreach (var (label, at) in J.TowersAt.Select((at, index) => ($"tower {index + 1}", P.ToVector(at)))
                         .Concat(Z.Spots.Select((at, index) => ($"beacon spot {index + 1}", P.ToVector(at)))))
            {
                AssertOnGround(crucible, at, label);
                Assert.IsGreaterThanOrEqualTo(5f, crucible.NavMesh.DistanceToWall(at, 20) ?? 0, $"{label} 5 m or more from a wall");
                AssertWalkWithoutLava(crucible, name, NorthTrench, at, $"{label} from the north trench marker");
                for (var dx = -20f; dx <= 20f; dx += 2)
                    for (var dz = -20f; dz <= 20f; dz += 2)
                        if (dx * dx + dz * dz <= 400 && crucible.NavMesh.Nearest(at + new Vector3(dx, 0, dz)) is { } ground &&
                            Vector2.Distance(new(ground.X, ground.Z), new(at.X + dx, at.Z + dz)) < 1)
                            Assert.IsFalse(LavaDamage.InLava(name, ground), FormattableString.Invariant($"{label}: lava at ({ground.X:0.0}, {ground.Y:0.0}, {ground.Z:0.0})"));
                Assert.IsGreaterThanOrEqualTo(20f, hostiles.Min(row => Vector2.Distance(new((float)row.PosX, (float)row.PosZ), new(at.X, at.Z)) - (float)row.Radius),
                    $"{label} 20 m or more outside every hostile pool's circle");
            }
        }

        /// <summary>
        /// Pump Up The Jam with no prerequisite: Tiersky offers it (and Strike Zone waits for it); a tower is no one's before the
        /// accept; at the accept six towers of the client's class and name, the player's own, intact, one marker on each, Tiersky
        /// hears no objective; each tower destroyed completes its own objective once (any order: 5, 1, 3, 6, 2, 4), a tower
        /// shot again counts no more; a reload after three keeps the three done and their towers down; the sixth makes the
        /// mission completeable once; a full bag waits, a failed save pays nothing; Tiersky pays the item chosen, once; the
        /// towers go with the mission; Keplinger offers Strike Zone.
        /// </summary>
        [TestMethod]
        public void PumpUpTheJamDestroysSixJammingTowersForTiersky()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var crucible = C.EnterCrucible(harness, Prometheus);
            var tiersky = Npc(crucible, T.Tiersky);
            var keplinger = Npc(crucible, K.Keplinger);
            Assert.IsFalse(crucible.DynamicObjects.Any(obj => obj.SceneMissionId == Jam && obj.IsInWorld), "no tower before the mission");
            var offer = WildernessRanjaGorgeTests.Offer(harness, tiersky, Jam);
            CollectionAssert.AreEqual(new[] { J.ElectricStaff, J.RocketLauncher, J.DirectRepairTool, J.SonicPistol },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, keplinger, Strike), "Strike Zone waits for it");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, keplinger, Jam), "only Tiersky offers it");
            Accept(harness, tiersky, Jam);
            for (var number = 1; number <= ClientTowers; number++)
                Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Jam, J.Destroy(number)), $"tower {number} open");
            Assert.IsEmpty(Topics(harness, tiersky, Jam), "Tiersky hears no objective");
            var snapshot = harness.Manager.BuildStatusSnapshot(harness.Client.Player)[Jam].ObjectivesList;
            Assert.HasCount((int)ClientTowers, snapshot);
            foreach (var objective in snapshot)
            {
                var tower = J.TowersAt[objective.ObjectiveId - 1];
                Assert.IsLessThan(0.1f, Vector2.Distance(new(objective.IndicatorList.Single().Position.X, objective.IndicatorList.Single().Position.Z),
                    new(tower.X, tower.Z)), $"objective {objective.ObjectiveId}'s marker on its tower");
            }

            var towers = Towers(harness, crucible);
            var log = new List<object>();
            var done = 0;
            foreach (var number in new[] { 5, 1, 3, 6, 2, 4 })
            {
                Shoot(harness, crucible, towers[number - 1], (int)J.TowerHitPoints);
                var packets = harness.Drain().Cast<object>().Concat(E.Tick(harness, crucible, 250)).ToList();
                log.AddRange(packets);
                done++;
                Assert.AreEqual(1, packets.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Jam && packet.ObjectiveId == J.Destroy(number)),
                    $"tower {number}: its objective, once");
                Assert.AreEqual(done, Enumerable.Range(1, (int)ClientTowers).Count(other => Objective(harness, Jam, J.Destroy(other)) == MissionObjectiveState.Completed),
                    $"tower {number}: only its own objective");
                if (done == ClientTowers - 1)
                    Assert.IsFalse(harness.Client.Player.Missions[Jam].Completeable, "5 of 6: not completeable");
                if (number == 5)
                {
                    Shoot(harness, crucible, towers[number - 1], 100);
                    Assert.AreEqual(0, harness.Drain().Cast<object>().Concat(E.Tick(harness, crucible, 250)).OfType<ObjectiveCompletedPacket>()
                        .Count(packet => packet.MissionId == Jam), "shot again, counted no more");
                }
                if (done == 3)
                {
                    P.ReloadAndRecover(harness, crucible);
                    towers = Towers(harness, crucible, down: new[] { 5, 1, 3 });
                    foreach (var kept in new[] { 5, 1, 3 })
                        Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Jam, J.Destroy(kept)), $"tower {kept} kept at a reload");
                    Assert.IsFalse(harness.Client.Player.Missions[Jam].Completeable);
                }
            }
            Assert.IsTrue(harness.Client.Player.Missions[Jam].Completeable, "six towers");
            Assert.AreEqual(1, log.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Jam && packet.IsCompleteable), "MissionCompleteable once");
            Assert.IsEmpty(Topics(harness, tiersky, Jam), "Tiersky hears no objective");

            FullBagWaits(harness, tiersky, Jam, 2, ArmorFiller);
            FailedTurnInPaysNothing(harness, tiersky, Jam, 2);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, tiersky, Jam, 2, J.Experience, J.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Jam), "MissionRewarded once");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, J.DirectRepairTool), "the repair tool chosen");
            for (var tick = 0; tick < 4; tick++)        // RetireRewardedScene retries: up to 1 s
                E.Tick(harness, crucible, 250);
            Assert.IsFalse(crucible.DynamicObjects.Any(obj => obj.SceneMissionId == Jam && obj.IsInWorld), "the towers go with the mission");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, tiersky, Jam), "done once");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, keplinger, Strike), "Keplinger offers Strike Zone");
        }

        /// <summary>An abandon of Pump Up The Jam after two towers starts it over: taken again, six intact towers and nothing done.</summary>
        [TestMethod]
        public void PumpUpTheJamAbandonedStartsOver()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var crucible = C.EnterCrucible(harness, Prometheus);
            var tiersky = Npc(crucible, T.Tiersky);
            Accept(harness, tiersky, Jam);
            var towers = Towers(harness, crucible);
            Shoot(harness, crucible, towers[0], (int)J.TowerHitPoints);
            Shoot(harness, crucible, towers[1], (int)J.TowerHitPoints);
            E.Tick(harness, crucible, 250);
            harness.Drain();
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Jam, J.Destroy(2)));

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Jam));
            harness.Manager.Scenes.Tick(crucible);
            harness.Drain();
            Assert.IsFalse(crucible.DynamicObjects.Any(obj => obj.SceneMissionId == Jam && obj.IsInWorld), "the towers go with the abandon");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, tiersky, Jam), "offered again after an abandon");
            Accept(harness, tiersky, Jam);
            Towers(harness, crucible);
            for (var number = 1; number <= ClientTowers; number++)
                Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Jam, J.Destroy(number)), $"tower {number} open again");
        }

        /// <summary>
        /// Strike Zone after Pump Up The Jam: Keplinger offers it (not before) and gives three GPS Beacons at the accept; three
        /// spots of the client's class, the player's own, ready, one marker on each; Keplinger hears no objective; each spot
        /// takes one beacon and completes its own objective once (any order: 2, 3, 1), a used spot takes no second one; a reload
        /// after one keeps the spot planted and the beacons left; the third makes the mission completeable once, no beacon is
        /// left; a full bag waits, a failed save pays nothing; Keplinger pays the item chosen, once; the spots go with the mission.
        /// </summary>
        [TestMethod]
        public void StrikeZonePlacesThreeBeaconsForKeplinger()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var crucible = C.EnterCrucible(harness, Prometheus);
            var keplinger = Npc(crucible, K.Keplinger);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, keplinger, Strike), "it waits for Pump Up The Jam");
            C.SeedCompleted(harness, Jam);
            crucible = C.EnterCrucible(harness, Prometheus);
            keplinger = Npc(crucible, K.Keplinger);
            Assert.IsFalse(crucible.DynamicObjects.Any(obj => obj.SceneMissionId == Strike && obj.IsInWorld), "no spot before the mission");
            var offer = WildernessRanjaGorgeTests.Offer(harness, keplinger, Strike);
            CollectionAssert.AreEqual(new[] { Z.StealthVest, Z.GravitonHelmet, Z.HazmatHelmet, Z.MotorAssistBoots },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, Npc(crucible, T.Tiersky), Strike), "only Keplinger offers it");
            Accept(harness, keplinger, Strike);
            Assert.AreEqual(ClientBeacons, Beacons(harness), "three GPS Beacons at the accept");
            Assert.IsEmpty(Topics(harness, keplinger, Strike), "Keplinger hears no objective");
            var snapshot = harness.Manager.BuildStatusSnapshot(harness.Client.Player)[Strike].ObjectivesList;
            Assert.HasCount((int)ClientBeacons, snapshot);
            foreach (var objective in snapshot)
            {
                var spot = Z.Spots[objective.ObjectiveId - 1];
                Assert.IsLessThan(0.1f, Vector2.Distance(new(objective.IndicatorList.Single().Position.X, objective.IndicatorList.Single().Position.Z),
                    new(spot.X, spot.Z)), $"objective {objective.ObjectiveId}'s marker on its spot");
            }

            var spots = Spots(harness, crucible);
            var log = new List<object>();
            var placed = 0U;
            foreach (var number in new[] { 2, 3, 1 })
            {
                var spot = spots[number - 1];
                harness.MoveTo(spot.Position + new Vector3(0, 0, 1));
                harness.Drain();
                Use(harness, crucible, spot, recovers: true);
                var packets = harness.Drain().Cast<object>().Concat(E.Tick(harness, crucible, 250)).ToList();
                log.AddRange(packets);
                placed++;
                Assert.AreEqual(ClientBeacons - placed, Beacons(harness), $"beacon {number} takes one");
                Assert.AreEqual((UseObjectState)SpotPlanted, spot.StateId, $"beacon {number} planted");
                Assert.IsFalse(spot.IsEnabled, $"spot {number} closed");
                Assert.AreEqual(1, packets.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Strike && packet.ObjectiveId == Z.Place(number)),
                    $"beacon {number}: its objective, once");
                Assert.AreEqual((int)placed, Enumerable.Range(1, (int)ClientBeacons).Count(other => Objective(harness, Strike, Z.Place(other)) == MissionObjectiveState.Completed),
                    $"beacon {number}: only its own objective");
                if (placed == ClientBeacons - 1)
                    Assert.IsFalse(harness.Client.Player.Missions[Strike].Completeable, "2 of 3: not completeable");
                if (number == 2)
                {
                    Use(harness, crucible, spot, recovers: false);
                    Assert.AreEqual(2U, Beacons(harness), "one beacon a spot");
                    P.ReloadAndRecover(harness, crucible);
                    Assert.AreEqual((2U, MissionObjectiveState.Completed), (Beacons(harness), Objective(harness, Strike, Z.Place(2))), "kept at a reload");
                    spots = Spots(harness, crucible, planted: new[] { 2 });
                    Assert.IsFalse(harness.Client.Player.Missions[Strike].Completeable);
                }
            }
            Assert.AreEqual(0U, Beacons(harness), "no beacon left");
            Assert.IsTrue(harness.Client.Player.Missions[Strike].Completeable, "three beacons");
            Assert.AreEqual(1, log.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Strike && packet.IsCompleteable), "MissionCompleteable once");
            Assert.IsEmpty(Topics(harness, keplinger, Strike), "Keplinger hears no objective");

            FullBagWaits(harness, keplinger, Strike, 3, ArmorFiller);
            FailedTurnInPaysNothing(harness, keplinger, Strike, 3);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, keplinger, Strike, 3, Z.Experience, Z.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Strike), "MissionRewarded once");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, Z.MotorAssistBoots), "the boots chosen");
            for (var tick = 0; tick < 4; tick++)        // RetireRewardedScene retries: up to 1 s
                E.Tick(harness, crucible, 250);
            Assert.IsFalse(crucible.DynamicObjects.Any(obj => obj.SceneMissionId == Strike && obj.IsInWorld), "the spots go with the mission");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, keplinger, Strike), "done once");
        }

        /// <summary>
        /// An abandon of Strike Zone after one beacon takes the beacons left and the spots; taken again, three new beacons (not
        /// five) and three ready spots, nothing done.
        /// </summary>
        [TestMethod]
        public void StrikeZoneAbandonedTakesTheBeacons()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            C.SeedCompleted(harness, Jam);
            var crucible = C.EnterCrucible(harness, Prometheus);
            var keplinger = Npc(crucible, K.Keplinger);
            Accept(harness, keplinger, Strike);
            var spot = Spots(harness, crucible)[0];
            harness.MoveTo(spot.Position + new Vector3(0, 0, 1));
            harness.Drain();
            Use(harness, crucible, spot, recovers: true);
            E.Tick(harness, crucible, 250);
            harness.Drain();
            Assert.AreEqual(2U, Beacons(harness));

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Strike));
            harness.Manager.Scenes.Tick(crucible);
            harness.Drain();
            Assert.AreEqual(0U, Beacons(harness), "the beacons go with the mission");
            Assert.IsFalse(crucible.DynamicObjects.Any(obj => obj.SceneMissionId == Strike && obj.IsInWorld), "the spots go with the abandon");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, keplinger, Strike), "offered again after an abandon");
            Accept(harness, keplinger, Strike);
            Assert.AreEqual(ClientBeacons, Beacons(harness), "three again");
            Spots(harness, crucible);
            for (var number = 1; number <= ClientBeacons; number++)
                Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Strike, Z.Place(number)), $"beacon {number} open again");
        }

        /// <summary>
        /// The six towers: the player's own, of the client's class and name, at their places on the ground, intact (full hit
        /// points) or, for <paramref name="down"/>, destroyed (none left).
        /// </summary>
        private static DynamicObject[] Towers(WildernessRuntimeTestHarness harness, MapChannel crucible, int[] down = null)
        {
            harness.Manager.Scenes.Tick(crucible);
            var towers = Enumerable.Range(1, (int)ClientTowers).Select(number => crucible.DynamicObjects.Single(obj =>
                obj.SceneMissionId == Jam && obj.SceneActorRole == J.TowerRole(number) && obj.IsInWorld)).ToArray();
            for (var number = 1; number <= towers.Length; number++)
            {
                var tower = towers[number - 1];
                var isDown = down?.Contains(number) == true;
                Assert.AreEqual((ClientTowerClass, ClientTowerName), ((uint)tower.EntityClassId, tower.NameOverrideId), $"tower {number}");
                Assert.AreEqual(P.ToVector(J.TowersAt[number - 1]), tower.Position, $"tower {number} at its place");
                Assert.AreEqual(harness.Client.Player.Id, tower.SceneOwnerCharacterId, "the player's own");
                Assert.AreEqual(isDown ? 0U : J.TowerHitPoints, tower.CurrentHitPoints, $"tower {number} hit points");
                Assert.AreEqual(isDown ? Destroyed : Intact, (uint)tower.StateId, $"tower {number} state");
                AssertOnGround(crucible, tower.Position, $"tower {number}");
            }
            return towers;
        }

        /// <summary>The three beacon spots: the player's own, of the client's class and use argument, at their places, ready or, for <paramref name="planted"/>, planted.</summary>
        private static DynamicObject[] Spots(WildernessRuntimeTestHarness harness, MapChannel crucible, int[] planted = null)
        {
            harness.Manager.Scenes.Tick(crucible);
            var spots = Enumerable.Range(1, (int)ClientBeacons).Select(number => crucible.DynamicObjects.Single(obj =>
                obj.SceneMissionId == Strike && obj.SceneActorRole == Z.SpotRole(number) && obj.IsInWorld)).ToArray();
            for (var number = 1; number <= spots.Length; number++)
            {
                var spot = spots[number - 1];
                var isPlanted = planted?.Contains(number) == true;
                Assert.AreEqual(ClientSpotClass, (uint)spot.EntityClassId, $"spot {number}");
                Assert.AreEqual(ClientSpotUseArgument, spot.MissionUseAction.ActionArgId, $"spot {number}: the client's use argument");
                Assert.AreEqual(P.ToVector(Z.Spots[number - 1]), spot.Position, $"spot {number} at its place");
                Assert.AreEqual(harness.Client.Player.Id, spot.SceneOwnerCharacterId, "the player's own");
                Assert.AreEqual(isPlanted ? SpotPlanted : SpotReady, (uint)spot.StateId, $"spot {number} state");
                Assert.AreEqual(!isPlanted, spot.IsEnabled, $"spot {number} open");
                AssertOnGround(crucible, spot.Position, $"spot {number}");
            }
            return spots;
        }

        private static uint Beacons(WildernessRuntimeTestHarness harness) => WildernessRanjaGorgeTests.Held(harness, ClientBeaconTemplate);

        /// <summary>A weapon hit of <paramref name="damage"/> on the object, as MarshesRetreadCityTests.Shoot.</summary>
        private static void Shoot(WildernessRuntimeTestHarness harness, MapChannel crucible, DynamicObject obj, int damage)
        {
            harness.MoveTo(obj.Position + new Vector3(0, 0, 1));
            harness.Drain();
            var action = new ActionData(harness.Client.Player, ActionId.WeaponAttack, 133, 0) { TargetId = obj.EntityId };
            MissileManager.Instance.MissileLaunch(crucible, action, damage);
            MissileManager.Instance.DoWork(crucible, 1000);
        }

        /// <summary>A use of the spot: the request, then its recovery if one is due (as PoolsBaneChainTests.Use).</summary>
        private static void Use(WildernessRuntimeTestHarness harness, MapChannel crucible, DynamicObject obj, bool recovers)
        {
            var count = crucible.PerformRecovery.Count;
            harness.Objects.RequestUseObjectPacket(harness.Client, new RequestUseObjectPacket
            {
                EntityId = obj.EntityId, ActionId = ActionId.UseObject, ActionArgId = ClientSpotUseArgument
            });
            Assert.AreEqual(count + (recovers ? 1 : 0), crucible.PerformRecovery.Count, $"use of {obj.SceneActorRole}");
            if (!recovers)
                return;
            var recovery = crucible.PerformRecovery.Last();
            crucible.PerformRecovery.Remove(recovery);
            ActorActionManager.Instance.PerformRecovery(crucible, recovery);
        }

        /// <summary>A complete navmesh walk (from the nearest navmesh points), no point of it on the client's lava (LavaDamage.InLava).</summary>
        private static void AssertWalkWithoutLava(MapChannel map, string mapName, Vector3 from, Vector3 to, string what)
        {
            var start = map.NavMesh.Nearest(from) ?? from;
            var path = map.NavMesh.FindPath(start, map.NavMesh.Nearest(to) ?? to, out var complete);
            Assert.IsTrue(path != null && complete, $"{what}: a path");
            var points = path.Prepend(start).ToList();
            for (var i = 0; i + 1 < points.Count; i++)
                for (var step = 0; step <= 20; step++)
                {
                    var at = Vector3.Lerp(points[i], points[i + 1], step / 20f);
                    Assert.IsFalse(LavaDamage.InLava(mapName, at), FormattableString.Invariant($"{what}: lava at ({at.X:0.0}, {at.Y:0.0}, {at.Z:0.0})"));
                }
        }
    }
}
