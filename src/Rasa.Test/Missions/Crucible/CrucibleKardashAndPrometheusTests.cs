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
using Rasa.Managers;
using Rasa.Packets.Mission.Server;
using Rasa.Services.Preloader.Missions.Crucible;
using Rasa.Services.Preloader.Missions.Marshes;
using Rasa.Structures;
using Rasa.Structures.World;
using Rasa.Test.Missions.Marshes;
using Rasa.Test.Missions.Plateau;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions.Crucible
{
    using static MarshesFalconHoldTests;
    using static MarshesPaludosTests;
    using static MarshesPreoDasTests;
    using static PlateauCampResistanceTests;
    using C = CrucibleOutpostIntrepidTests;
    using F = MarshesClientFloorTests;
    using K = CrucibleKardashAndPrometheusNpcsV1;
    using R = CruciblePrometheusRisingV1;
    using Q = CrucibleConfirmedKillsV1;
    using P = CruciblePointOfImpactV1;
    using E = CrucibleItCameFromErdasV1;

    /// <summary>
    /// Crucible set 8a, played through as a player plays it: Captain DeLisi's Prometheus Rising (1210: a talk at Major
    /// Keplinger at Prometheus Outpost), Larai Zupa Madias' Confirmed Kills (1212: 15 Bane foot soldiers), Awol Captain Cheung's
    /// Point Of Impact (1217: Professor Tiernan at the Eloh Crash Site, then the walk up to the Kardash Eloh Obelisk, turned in
    /// by radio) and It Came From Erdas (1218: offered by radio at 1217's reward, the Eloh Memory Capsule to Tiernan, then to
    /// Captain Amandla Botha at the Ashoka Settlement on Ashen Desert). With them the set's guards (as CrucibleGuardTests holds
    /// the Crucible's and CrucibleStaalJunkyardTests the Junkyard's): Tiernan, Keplinger, Madias, the obelisk and the trench
    /// markers on the navmesh, walked to from the Prometheus Outpost waypoint with no lava on the walk; Botha on Ashen Desert
    /// on the navmesh and a client floor, seen, walked to from the Ashoka Settlement waypoint, clear of its hostiles and links.
    ///
    /// Why these tests: Tiernan and Botha are new, Keplinger and Madias moved; 1217 has no receiver (the obelisk is no
    /// creature) and 1218 no giver; 1218 ends on another map. An offer before its prerequisite, a kill before the accept
    /// counted, a count off by one, progress lost at a reload or kept after an abandon, a capsule kept from the turn-in, a
    /// turn-in that pays twice or on a full bag would each break them.
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class CrucibleKardashAndPrometheusTests
    {
        /// <summary>The Crucible markers on Ashen Desert: 1218's on Botha.</summary>
        internal const int AshenDesertMarkers = 1;

        private const uint AshenDesert = K.AshenDesert;
        private const uint Business = CrucibleBusinessProposalV1.MissionId, Junk = CrucibleNotMyJunkV1.MissionId;
        private const uint Rising = R.MissionId, Kills = Q.MissionId, Impact = P.MissionId, Erdas = E.MissionId;
        private const uint ConsumableFiller = 45060;       // Class VII Basic Med Pack: fills the consumables bag (as MarshesRetreadArenaTests)
        private const uint ArmorFiller = 12748;            // Armor_T3_Bio_V01_CMN_Gloves_30_to_34 (inventory category 1)
        private static readonly uint[] TrenchPools = { 531099, 531101 };
        private const uint ClientKills = 15;               // client missionobjective 1212,1 "Kill 15 Bane Soldiers." (written out, not the data class's)

        private static readonly Vector3 Prometheus = ToVector(K.PrometheusWaypoint);
        private static readonly Vector3 Ashoka = ToVector(K.AshokaWaypoint);

        [TestInitialize]
        public void Clock() => PlateauGiantKillerSquadTests.UseClock();

        [TestCleanup]
        public void Restore() => PlateauGiantKillerSquadTests.RestoreClock();

        /// <summary>
        /// On the Crucible: Tiernan with the client's name 9365, level 40 and package 1483, Keplinger (1337) and Madias where
        /// set 8a puts them, friendly, on the navmesh, 2 m or more from a wall, walked to from the Prometheus Outpost waypoint
        /// with no lava on the walk; the escape pod's deck (the obelisk's place) on the navmesh, walked to from Tiernan; each
        /// trench marker on the navmesh, walked to with no lava (the north one from the waypoint, the west one from the north
        /// one: the straight walk from the waypoint to the west one crosses lava), its centre off the trench pool's (so the
        /// pool-walk guard does not take it for a pool marker).
        /// </summary>
        [TestMethod]
        public void TheCruciblePlacesStandWhereTheyAreWalkedTo()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var crucible = C.EnterCrucible(harness, Prometheus);
            var name = crucible.MapInfo.MapName;
            var tiernan = Npc(crucible, K.Tiernan);
            Assert.AreEqual((K.TiernanName, K.TiernanLevel, K.TiernanPackage), (tiernan.NameId, (uint)tiernan.Level, tiernan.Npc?.NpcPackageId));
            var keplinger = Npc(crucible, K.Keplinger);
            Assert.AreEqual(K.KeplingerPackage, keplinger.Npc?.NpcPackageId);
            var madias = Npc(crucible, K.Madias);
            foreach (var (label, npc) in new[] { ("Tiernan", tiernan), ("Keplinger", keplinger), ("Madias", madias) })
            {
                Assert.AreNotEqual(TargetCategory.Hostile, npc.TargetCategory, label);
                AssertOnGround(crucible, npc.Position, label);
                Assert.IsGreaterThanOrEqualTo(2f, crucible.NavMesh.DistanceToWall(npc.Position, 20) ?? 0, $"{label} 2 m or more from a wall");
                Assert.IsLessThan(0.1f, Vector2.Distance(new(npc.Position.X, npc.Position.Z), new((float)K.At(npc.DbId).X, (float)K.At(npc.DbId).Z)), $"{label} at set 8a's place");
                AssertWalkWithoutLava(crucible, name, Prometheus, npc.Position, $"{label} from the Prometheus Outpost waypoint");
            }

            var deck = ToVector(K.PodDeckAt);
            AssertOnGround(crucible, deck, "the pod's deck");
            AssertWalkWithoutLava(crucible, name, tiernan.Position, deck, "the pod's deck from Tiernan");
            Assert.IsLessThan(30f, Vector3.Distance(tiernan.Position, deck), "Tiernan by the escape pod");

            // The walk: the waypoint to the north trench, then the north trench to the west one. The navmesh walk from the
            // waypoint straight to the west trench crosses lava at (398.4, 164.1, 238) (CrucibleConfirmedKillsV1).
            var trenches = Q.TrenchesAt.Select(ToVector).ToArray();
            var starts = new[] { ("the north trench marker", trenches[1]), ("the Prometheus Outpost waypoint", Prometheus) };
            foreach (var ((at, pool), (startLabel, start)) in trenches.Zip(TrenchPools).Zip(starts))
            {
                var ground = crucible.NavMesh.GroundHeight(at + new Vector3(0, 2, 0));
                Assert.IsTrue(ground is { } y && MathF.Abs(y - at.Y) <= 2, $"the trench marker at {at} on the navmesh ({ground})");
                AssertWalkWithoutLava(crucible, name, start, at, $"the trench marker at {at} from {startLabel}");
                var row = harness.World.SpawnPoolEntries.AsNoTracking().Single(entry => entry.Id == pool);
                var gap = Vector2.Distance(new(at.X, at.Z), new((float)row.PosX, (float)row.PosZ));
                Assert.IsTrue(gap > 0.5f && gap < Q.MarkerRadius - row.Radius / 2, $"the marker by pool {pool} ({gap:0.0} m), its centre off the pool's");
            }
        }

        /// <summary>
        /// On Ashen Desert (1734): Botha with the client's name 9278, level 40 and package 1410, friendly, standing still, on the
        /// navmesh, 2 m or more from a wall, no floor over his head, walked to from the Ashoka Settlement waypoint with no lava,
        /// ReachClearance or more outside every hostile automatic pool's circle on the map, 5 m or more outside every map link's
        /// circle; his marker on the navmesh and clear of every link circle.
        /// </summary>
        [TestMethod]
        public void BothaStandsAtTheAshokaSettlement()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var desert = EnterAshenDesert(harness);
            var botha = Npc(desert, K.Botha);
            Assert.AreEqual((K.BothaName, K.BothaLevel, K.BothaPackage), (botha.NameId, (uint)botha.Level, botha.Npc?.NpcPackageId));
            Assert.AreNotEqual(TargetCategory.Hostile, botha.TargetCategory);
            Assert.IsTrue(BehaviorManager.NeverMoves(CreatureManager.Instance.LoadedCreatures[K.Botha]), "Botha stands still");
            AssertOnGround(desert, botha.Position, "Botha");
            Assert.IsGreaterThanOrEqualTo(2f, desert.NavMesh.DistanceToWall(botha.Position, 20) ?? 0, "Botha 2 m or more from a wall");
            var over = desert.NavMesh.GroundHeight(botha.Position + new Vector3(0, 2.5f, 0));
            Assert.IsTrue(over is { } top && top - botha.Position.Y < 0.5f, $"no floor over Botha's head ({over})");
            AssertWalkWithoutLava(desert, desert.MapInfo.MapName, Ashoka, botha.Position, "Botha from the Ashoka Settlement waypoint");

            bool Hostile(uint creature) => CreatureManager.Instance.LoadedCreatures.TryGetValue(creature, out var c) && c.TargetCategory == TargetCategory.Hostile;
            var hostiles = SpawnPoolManager.Instance.LoadedSpawnPools.Values.Where(pool => pool.MapContextId == AshenDesert && pool.Mode == 0 &&
                pool.SpawnSlot.Exists(slot => slot.CountMax > 0) && pool.SpawnSlot.TrueForAll(slot => Hostile(slot.CreatureId))).ToList();
            Assert.IsNotEmpty(hostiles, "Ashen Desert's hostile pools");
            foreach (var hostile in hostiles)
                Assert.IsGreaterThanOrEqualTo(MarshesNpcsStandStillV1.ReachClearance,
                    Vector2.Distance(new(botha.Position.X, botha.Position.Z), new(hostile.Position.X, hostile.Position.Z)) - hostile.Radius,
                    $"Botha outside hostile pool {hostile.DbId}'s reach");

            var links = harness.World.Set<MapLinkEntry>().AsNoTracking().Where(link => link.Enabled != 0 && link.MapContextId == AshenDesert).ToList();
            Assert.IsNotEmpty(links, "Ashen Desert's map links");
            var marker = harness.World.MissionIndicatorEntries.AsNoTracking().Single(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision &&
                entry.MissionId == Erdas && entry.ObjectiveId == E.BringToBotha);
            var markerAt = new Vector3((float)marker.PosX, (float)marker.PosY, (float)marker.PosZ);
            foreach (var link in links)
            {
                var gap = Vector2.Distance(new(botha.Position.X, botha.Position.Z), new((float)link.PosX, (float)link.PosZ)) - (float)link.Radius;
                Assert.IsGreaterThanOrEqualTo(5f, gap, $"Botha clear of link {link.Id}");
                Assert.IsGreaterThanOrEqualTo(5f, gap - (float)marker.Radius, $"Botha's marker clear of link {link.Id}");
            }
            var ground = desert.NavMesh.GroundHeight(markerAt + new Vector3(0, 2, 0));
            Assert.IsTrue(ground is { } y && MathF.Abs(y - markerAt.Y) <= 2, $"Botha's marker on the navmesh ({ground})");
            Assert.IsLessThanOrEqualTo((float)marker.Radius, Vector2.Distance(new(markerAt.X, markerAt.Z), new(botha.Position.X, botha.Position.Z)), "Botha inside his marker");
        }

        /// <summary>
        /// The client's floors (MarshesClientFloorTests; inconclusive without the client's files): Botha and his marker on a
        /// client floor of Ashen Desert with none just over him; Botha seen from 16 of 16 places 2 m round him and from each point
        /// of the walk from the Ashoka Settlement waypoint 5 to 2 m from him.
        /// </summary>
        [TestMethod]
        public void BothaStandsOnAClientFloorAndIsSeen()
        {
            const float MarkerHeight = 2;
            var dataDir = F.ClientData();
            using var harness = WildernessRuntimeTestHarness.Create();
            var desert = EnterAshenDesert(harness);
            using var meshes = new MeshLibrary(dataDir, F.MeshCsv());
            var geometry = MapGeometry.Load(Path.Combine(dataDir, "maps", desert.MapInfo.MapName), meshes, 1);
            var botha = ToVector(K.BothaAt);
            var (floors, ceilings) = F.Surfaces(geometry, new List<(Vector3, float)> { (botha, F.Walkable), (botha + new Vector3(0, MarkerHeight - F.MaxUnder, 0), F.Walkable) });
            var failures = new List<string>();
            if (botha.Y - floors[0] > F.MaxOver || !float.IsPositiveInfinity(ceilings[0]))
                failures.Add($"Botha at {botha}: floor {floors[0]}, ceiling {ceilings[0]}");
            if (botha.Y - floors[1] > MarkerHeight)
                failures.Add($"Botha's marker: client floor {floors[1]}");

            var near = F.Near(geometry, new List<Vector3> { botha }, 8)[0];
            var aim = botha + new Vector3(0, F.NpcMiddle, 0);
            for (var k = 0; k < F.RingPlaces; k++)
            {
                var angle = k * 2 * MathF.PI / F.RingPlaces;
                var stand = botha + new Vector3(MathF.Sin(angle) * F.Ring, 0, MathF.Cos(angle) * F.Ring);
                if (F.FloorAt(near, stand, botha.Y - F.MaxOver, botha.Y + F.MaxUnder) is not { } fy || !F.Clear(near, stand with { Y = fy + F.Eye }, aim))
                    failures.Add($"Botha not seen from ({stand.X:F1}, {stand.Z:F1})");
            }
            var walk = desert.NavMesh.FindPath(desert.NavMesh.Nearest(Ashoka).Value, botha, out var complete);
            Assert.IsTrue(walk != null && complete, "the walk to Botha");
            var points = walk.Prepend(desert.NavMesh.Nearest(Ashoka).Value).ToList();
            var ends = 0;
            foreach (var distance in new[] { 5f, 4.5f, 4f, 3.5f, 3f, 2.5f, 2f })
                for (var i = 0; i + 1 < points.Count; i++)
                {
                    Vector3? hit = null;
                    for (var step = 0; step <= 50 && hit == null; step++)
                    {
                        var at = Vector3.Lerp(points[i], points[i + 1], step / 50f);
                        if (Vector3.Distance(at, botha) <= distance)
                            hit = at;
                    }
                    if (hit is not { } end)
                        continue;
                    ends++;
                    var floor = F.FloorAt(near, end, end.Y - 1.5f, end.Y + 1f) ?? end.Y;
                    if (!F.Clear(near, end with { Y = floor + F.Eye }, aim))
                        failures.Add($"Botha not seen from the walk's point ({end.X:F1}, {end.Z:F1}) {distance} m off");
                    break;
                }
            Assert.AreEqual(7, ends, "the walk's points 5 to 2 m from Botha");
            Assert.IsEmpty(failures, string.Join("; ", failures));
        }

        /// <summary>
        /// Prometheus Rising after Business Proposal: DeLisi offers it (only she, not before); Keplinger hears 1 (only that) and
        /// the mission is to be turned in; a full bag waits, a failed save pays nothing; Keplinger pays the stack chosen, once.
        /// </summary>
        [TestMethod]
        public void PrometheusRisingReportsToKeplinger()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var crucible = C.EnterCrucible(harness);
            var delisi = Npc(crucible, CrucibleOutpostIntrepidNpcsV1.DeLisi);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, delisi, Rising), "it waits for Business Proposal");
            C.SeedCompleted(harness, Business);
            crucible = C.EnterCrucible(harness);
            var offer = WildernessRanjaGorgeTests.Offer(harness, delisi, Rising);
            CollectionAssert.AreEqual(new[] { R.ResTraumaKit, R.ConcussionGrenade, R.BasicMedPack, R.FragGrenade },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Accept(harness, delisi, Rising);
            var marker = harness.Manager.BuildStatusSnapshot(harness.Client.Player)[Rising].ObjectivesList.Single().IndicatorList.Single();
            Assert.IsLessThan(0.1f, Vector2.Distance(new(marker.Position.X, marker.Position.Z), new((float)K.At(K.Keplinger).X, (float)K.At(K.Keplinger).Z)), "on Keplinger");

            crucible = C.EnterCrucible(harness, Prometheus);
            var keplinger = Npc(crucible, K.Keplinger);
            Assert.IsFalse(harness.Client.Player.Missions[Rising].Completeable);
            Talk(harness, keplinger, Rising, R.Report);
            Assert.IsTrue(harness.Client.Player.Missions[Rising].Completeable);
            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, delisi.EntityId, Rising, 2), "not DeLisi's to take");
            harness.Drain();
            FullBagWaits(harness, keplinger, Rising, 2, ConsumableFiller);
            FailedTurnInPaysNothing(harness, keplinger, Rising, 2);
            var held = WildernessRanjaGorgeTests.Held(harness, R.BasicMedPack);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, keplinger, Rising, 2, R.Experience, R.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Rising), "MissionRewarded once");
            Assert.AreEqual(held + R.BasicMedPacks, WildernessRanjaGorgeTests.Held(harness, R.BasicMedPack), "the six med packs chosen");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, delisi, Rising), "done once");
        }

        /// <summary>
        /// Confirmed Kills after Business Proposal: Madias offers it (not before); a foot soldier killed before the accept does
        /// not count; both kinds count (Soldier Mark IX and Grenadier); 14 kills leave it open (a reload at 8 kept), the 15th
        /// completes it once, a 16th counts no more; its two markers in the trenches; Madias pays the item chosen, once.
        /// </summary>
        [TestMethod]
        public void ConfirmedKillsCountsFifteenFootSoldiersForMadias()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var crucible = C.EnterCrucible(harness, Prometheus);
            var madias = Npc(crucible, K.Madias);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, madias, Kills), "it waits for Business Proposal");
            C.SeedCompleted(harness, Business);
            crucible = C.EnterCrucible(harness, Prometheus);
            var offer = WildernessRanjaGorgeTests.Offer(harness, madias, Kills);
            CollectionAssert.AreEqual(new[] { Q.StealthLegs, Q.BioBoots, Q.ReflectiveLegs, Q.MotorAssistVest },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Spawn(crucible, TrenchPools);
            KillFootSoldier(harness, crucible, Q.FootSoldiers[0]);
            Accept(harness, madias, Kills);
            Assert.AreEqual(0U, Count(harness), "a kill before the accept does not count");
            Assert.IsEmpty(Topics(harness, madias, Kills), "Madias hears no objective");
            var markers = harness.Manager.BuildStatusSnapshot(harness.Client.Player)[Kills].ObjectivesList.Single().IndicatorList;
            CollectionAssert.AreEquivalent(Q.TrenchesAt.Select(at => ((float)at.X, (float)at.Z)).ToArray(),
                markers.Select(one => (one.Position.X, one.Position.Z)).ToArray(), "the two trench markers");

            var completed = 0;
            for (var kill = 1; kill <= ClientKills; kill++)
            {
                if (kill == 8)
                {
                    WildernessRanjaGorgeTests.Reload(harness);
                    harness.Drain();
                    Assert.AreEqual(7U, Count(harness), "kept at a reload");
                }
                Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Kills, Q.Kill), $"open before kill {kill}");
                completed += KillFootSoldier(harness, crucible, Q.FootSoldiers[kill % 2])
                    .OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Kills);
                if (kill == ClientKills - 1)
                    Assert.AreEqual((ClientKills - 1, MissionObjectiveState.Incomplete), (Count(harness), Objective(harness, Kills, Q.Kill)), "14 of 15");
            }
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Kills, Q.Kill), "fifteen");
            Assert.AreEqual(1, completed, "ObjectiveCompleted once");
            KillFootSoldier(harness, crucible, Q.FootSoldiers[0]);
            Assert.IsLessThanOrEqualTo(ClientKills, Count(harness), "a 16th counts no more");

            FullBagWaits(harness, madias, Kills, 1, ArmorFiller);
            FailedTurnInPaysNothing(harness, madias, Kills, 1);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, madias, Kills, 1, Q.Experience, Q.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Kills), "MissionRewarded once");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, Q.BioBoots), "the boots chosen");
        }

        /// <summary>An abandon of Confirmed Kills at 3 kills starts the count over; taken again, the count is 0 and a kill counts.</summary>
        [TestMethod]
        public void ConfirmedKillsAbandonedStartsTheCountOver()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            C.SeedCompleted(harness, Business);
            var crucible = C.EnterCrucible(harness, Prometheus);
            var madias = Npc(crucible, K.Madias);
            Spawn(crucible, TrenchPools);
            Accept(harness, madias, Kills);
            for (var kill = 0; kill < 3; kill++)
                KillFootSoldier(harness, crucible, Q.FootSoldiers[kill % 2]);
            Assert.AreEqual(3U, Count(harness));
            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Kills));
            harness.Drain();
            Accept(harness, madias, Kills);
            Assert.AreEqual(0U, Count(harness), "the count starts over");
            KillFootSoldier(harness, crucible, Q.FootSoldiers[1]);
            Assert.AreEqual(1U, Count(harness));
        }

        /// <summary>
        /// Point Of Impact after Not My Junk: Cheung offers it (not before); the accept sets down the player's own obelisk on
        /// the pod's deck (not interactable; a reload in the middle of the scene keeps it); Tiernan hears 1 (only that), which
        /// opens the hidden 2; the deck's foot outside the area leaves 2 open, the walk up to the obelisk completes it once; no NPC
        /// takes the turn-in; by radio it pays the stack chosen once, and It Came From Erdas is offered by radio with it.
        /// </summary>
        [TestMethod]
        public void PointOfImpactWalksUpToTheObeliskAndRadiosIn()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var crucible = C.EnterCrucible(harness, CrucibleGuardTests.AwolCampWaypoint);
            var cheung = Npc(crucible, CrucibleAwolCampNpcsV1.Cheung);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, cheung, Impact), "it waits for Not My Junk");
            Assert.IsFalse(harness.Manager.TryAcceptRadioMission(harness.Client, Erdas), "1218 waits for 1217");
            C.SeedCompleted(harness, Junk);
            crucible = C.EnterCrucible(harness, CrucibleGuardTests.AwolCampWaypoint);
            var offer = WildernessRanjaGorgeTests.Offer(harness, cheung, Impact);
            CollectionAssert.AreEqual(new[] { P.EmpBomb, P.CryogenicGrenade, P.StandardMedPack, P.IncendiaryGrenade },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Accept(harness, cheung, Impact);
            Assert.AreEqual((MissionObjectiveState.Incomplete, MissionObjectiveState.Inactive),
                (Objective(harness, Impact, P.SpeakToTiernan), Objective(harness, Impact, P.Approach)));
            var visible = harness.Manager.BuildStatusSnapshot(harness.Client.Player)[Impact].ObjectivesList;
            CollectionAssert.AreEqual(new[] { P.SpeakToTiernan }, visible.Select(objective => objective.ObjectiveId).ToArray(), "2 is hidden");
            Assert.IsLessThan(0.1f, Vector2.Distance(new(visible.Single().IndicatorList.Single().Position.X, visible.Single().IndicatorList.Single().Position.Z),
                new((float)K.TiernanAt.X, (float)K.TiernanAt.Z)), "on Tiernan");

            crucible = C.EnterCrucible(harness, Prometheus);
            Tick(harness, crucible, 250);
            Assert.IsNotNull(FindSceneObject(harness, crucible, Impact, P.ObeliskRole), "the obelisk from the accept");
            var deck = ToVector(K.PodDeckAt);
            Enter(harness, deck);
            Assert.AreEqual(MissionObjectiveState.Inactive, Objective(harness, Impact, P.Approach), "the obelisk before Tiernan is nothing");
            var tiernan = Npc(crucible, K.Tiernan);
            Talk(harness, tiernan, Impact, P.SpeakToTiernan);
            Tick(harness, crucible, 250);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Impact, P.Approach), "the talk opens 2");
            var obelisk = SceneObject(harness, crucible, Impact, P.ObeliskRole);
            Assert.AreEqual(P.ObeliskClass, (uint)obelisk.EntityClassId);
            Assert.IsLessThan(0.1f, Vector3.Distance(obelisk.Position, deck), "on the pod's deck");
            Assert.IsFalse(obelisk.IsEnabled, "the obelisk takes no use");

            ReloadAndRecover(harness, crucible);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Impact, P.Approach), "kept at a reload in the middle of the scene");
            Assert.IsNotNull(FindSceneObject(harness, crucible, Impact, P.ObeliskRole), "the obelisk is back");
            Enter(harness, tiernan.Position);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Impact, P.Approach), "Tiernan's place is outside the area");
            Assert.IsFalse(harness.Client.Player.Missions[Impact].Completeable);
            Enter(harness, deck);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Impact, P.Approach), "the walk up to the obelisk");
            Assert.IsTrue(harness.Client.Player.Missions[Impact].Completeable);
            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, tiernan.EntityId, Impact, 1), "not Tiernan's to take");
            harness.Drain();

            var before = harness.Context.ReadRewardTotals();
            var held = WildernessRanjaGorgeTests.Held(harness, P.CryogenicGrenade);
            Assert.IsTrue(harness.Manager.TryCompleteRadioMission(harness.Client, Impact, 1, null), "radioed in");
            var rewarded = harness.Drain();
            Assert.AreEqual(1, rewarded.OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Impact), "paid once");
            var after = harness.Context.ReadRewardTotals();
            Assert.AreEqual((before.Experience + P.Experience, before.Credits + (int)P.Credits), (after.Experience, after.Credits));
            Assert.AreEqual(held + P.CryogenicGrenades, WildernessRanjaGorgeTests.Held(harness, P.CryogenicGrenade), "the grenades chosen");
            Assert.IsFalse(harness.Manager.TryCompleteRadioMission(harness.Client, Impact, 1, null), "and only once");
            Assert.AreEqual(1, rewarded.OfType<DispenseRadioMissionPacket>().Count(packet => packet.MissionId == Erdas), "1218 by radio");
            for (var tick = 0; tick < 4; tick++)        // RetireRewardedScene retries: up to 1 s
                Tick(harness, crucible, 250);
            Assert.IsNull(FindSceneObject(harness, crucible, Impact, P.ObeliskRole), "the obelisk goes with the mission");
        }

        /// <summary>
        /// An abandon of Point Of Impact after Tiernan's talk takes the obelisk; taken again, the talk must be had again and the
        /// walk up to the deck before it is nothing.
        /// </summary>
        [TestMethod]
        public void PointOfImpactAbandonedStartsOver()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            C.SeedCompleted(harness, Junk);
            var crucible = C.EnterCrucible(harness, CrucibleGuardTests.AwolCampWaypoint);
            var cheung = Npc(crucible, CrucibleAwolCampNpcsV1.Cheung);
            Accept(harness, cheung, Impact);
            crucible = C.EnterCrucible(harness, Prometheus);
            Talk(harness, Npc(crucible, K.Tiernan), Impact, P.SpeakToTiernan);
            Tick(harness, crucible, 250);
            Assert.IsNotNull(FindSceneObject(harness, crucible, Impact, P.ObeliskRole));

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Impact));
            harness.Manager.Scenes.Tick(crucible);
            harness.Drain();
            Assert.IsNull(FindSceneObject(harness, crucible, Impact, P.ObeliskRole), "the obelisk goes with the abandon");
            crucible = C.EnterCrucible(harness, CrucibleGuardTests.AwolCampWaypoint);
            Accept(harness, Npc(crucible, CrucibleAwolCampNpcsV1.Cheung), Impact);
            crucible = C.EnterCrucible(harness, Prometheus);
            Tick(harness, crucible, 250);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Impact, P.SpeakToTiernan), "the talk again");
            Enter(harness, ToVector(K.PodDeckAt));
            Assert.AreEqual(MissionObjectiveState.Inactive, Objective(harness, Impact, P.Approach), "the deck before the talk is nothing");
        }

        /// <summary>
        /// It Came From Erdas: offered by radio once Point Of Impact is done (and again on arrival on the Crucible while not
        /// taken); no NPC offers it; taken, it gives the Eloh Memory Capsule once (a reload keeps it); Tiernan hears 1 (only
        /// that), which opens 2, its marker on Botha on Ashen Desert; Botha hears 2 and takes the turn-in (not Tiernan); a full
        /// bag waits, a failed save pays nothing and keeps the capsule; Botha takes the capsule and pays the item chosen, once.
        /// </summary>
        [TestMethod]
        public void ItCameFromErdasBringsTheCapsuleToBotha()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            C.SeedCompleted(harness, Junk, Impact);
            var crucible = C.EnterCrucible(harness, Prometheus);
            harness.Manager.OfferArrivalMissions(harness.Client);
            Assert.AreEqual(1, harness.Drain().OfType<DispenseRadioMissionPacket>().Count(packet => packet.MissionId == Erdas), "1218 on arrival");
            var tiernan = Npc(crucible, K.Tiernan);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, tiernan, Erdas), "not Tiernan's to give");
            Assert.IsTrue(harness.Manager.TryAcceptRadioMission(harness.Client, Erdas), "taken by radio");
            harness.Drain();
            Tick(harness, crucible, 250);
            Assert.AreEqual(1U, Capsules(harness), "the capsule at the accept");
            Assert.AreEqual((MissionObjectiveState.Incomplete, MissionObjectiveState.Inactive),
                (Objective(harness, Erdas, E.TellTiernan), Objective(harness, Erdas, E.BringToBotha)));
            Assert.IsFalse(harness.Manager.TryAcceptRadioMission(harness.Client, Erdas), "held once");

            WildernessRanjaGorgeTests.Reload(harness);
            harness.Drain();
            Assert.AreEqual(1U, Capsules(harness), "kept at a reload");
            Talk(harness, tiernan, Erdas, E.TellTiernan);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Erdas, E.BringToBotha), "the talk opens 2");
            Assert.IsFalse(harness.Client.Player.Missions[Erdas].Completeable);
            Assert.IsEmpty(harness.Manager.BuildStatusSnapshot(harness.Client.Player)[Erdas].ObjectivesList
                .Single(objective => objective.ObjectiveId == E.BringToBotha).IndicatorList, "on the Crucible, no marker (the map's markers only)");

            var desert = EnterAshenDesert(harness);
            var bring = harness.Manager.BuildStatusSnapshot(harness.Client.Player)[Erdas].ObjectivesList
                .Single(objective => objective.ObjectiveId == E.BringToBotha).IndicatorList.Single();
            Assert.AreEqual(AshenDesert, bring.MapContextId, "the marker on Ashen Desert");
            Assert.IsLessThan(0.1f, Vector2.Distance(new(bring.Position.X, bring.Position.Z), new((float)K.BothaAt.X, (float)K.BothaAt.Z)), "on Botha");

            var botha = Npc(desert, K.Botha);
            Talk(harness, botha, Erdas, E.BringToBotha);
            Assert.IsTrue(harness.Client.Player.Missions[Erdas].Completeable);
            Assert.AreEqual(1U, Capsules(harness), "held to the turn-in");
            FullBagWaits(harness, botha, Erdas, 3, ArmorFiller);
            FailedTurnInPaysNothing(harness, botha, Erdas, 3);
            Assert.AreEqual(1U, Capsules(harness), "kept at the failed turn-in");
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, botha, Erdas, 3, E.Experience, E.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Erdas), "MissionRewarded once");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, E.MotorAssistHelmet), "the helmet chosen");
            Assert.AreEqual(0U, Capsules(harness), "Botha takes the capsule");
            Assert.IsFalse(harness.Manager.TryAcceptRadioMission(harness.Client, Erdas), "done once");
        }

        /// <summary>An abandon of It Came From Erdas takes the capsule; taken again by radio, one capsule and Tiernan's talk again.</summary>
        [TestMethod]
        public void ItCameFromErdasAbandonedTakesTheCapsule()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            C.SeedCompleted(harness, Junk, Impact);
            var crucible = C.EnterCrucible(harness, Prometheus);
            harness.Manager.OfferArrivalMissions(harness.Client);
            harness.Drain();
            Assert.IsTrue(harness.Manager.TryAcceptRadioMission(harness.Client, Erdas));
            harness.Drain();
            Tick(harness, crucible, 250);
            Talk(harness, Npc(crucible, K.Tiernan), Erdas, E.TellTiernan);
            Assert.AreEqual(1U, Capsules(harness));

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Erdas));
            harness.Manager.Scenes.Tick(crucible);
            harness.Drain();
            Assert.AreEqual(0U, Capsules(harness), "the capsule goes");
            harness.Manager.OfferArrivalMissions(harness.Client);
            harness.Drain();
            Assert.IsTrue(harness.Manager.TryAcceptRadioMission(harness.Client, Erdas), "taken again");
            harness.Drain();
            Tick(harness, crucible, 250);
            Assert.AreEqual(1U, Capsules(harness), "one capsule again");
            Assert.AreEqual((MissionObjectiveState.Incomplete, MissionObjectiveState.Inactive),
                (Objective(harness, Erdas, E.TellTiernan), Objective(harness, Erdas, E.BringToBotha)), "Tiernan's talk again");
        }

        /// <summary>The player on Ashen Desert at the Ashoka Settlement waypoint (as CrucibleOutpostIntrepidTests.EnterCrucible on the Crucible).</summary>
        private static MapChannel EnterAshenDesert(WildernessRuntimeTestHarness harness)
        {
            var map = harness.Maps.MapChannelArray.TryGetValue(AshenDesert, out var loaded) ? loaded : PlateauInstanceMaps.LoadMap(harness, AshenDesert);
            var client = harness.Client;
            CellManager.Instance.RemoveFromWorld(client);
            client.Player.MapChannel.ClientList.Remove(client);
            client.Player.MapChannel = map;
            client.Player.RuntimeMapChannel = map;
            client.Player.MapContextId = AshenDesert;
            map.ClientList.Add(client);
            harness.MoveTo(Ashoka);
            CellManager.Instance.AddToWorld(client);
            harness.Drain();
            return map;
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

        /// <summary>A Bane foot soldier of this kind killed (the trench pools back if none stands); the packets it sent.</summary>
        private static List<object> KillFootSoldier(WildernessRuntimeTestHarness harness, MapChannel crucible, uint kind)
        {
            if (!Alive(crucible).Any(candidate => candidate.DbId == kind && candidate.Npc == null))
                Respawn(crucible, TrenchPools);
            Kill(harness, crucible, kind);
            return harness.Drain().Cast<object>().ToList();
        }

        /// <summary>The kill counter of Confirmed Kills.</summary>
        private static uint Count(WildernessRuntimeTestHarness harness) =>
            harness.Client.Player.Missions[Kills].Objectives[Q.Kill].Counters[0];

        private static uint Capsules(WildernessRuntimeTestHarness harness) => WildernessRanjaGorgeTests.Held(harness, E.CapsuleTemplate);
    }
}
