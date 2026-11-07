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
    using static PlateauInstanceMaps;
    using C = CrucibleOutpostIntrepidTests;
    using F = MarshesClientFloorTests;
    using G = CrucibleGrupaChiefsNpcsV1;
    using J = CrucibleStaalJunkyardNpcsV1;
    using H = CrucibleHeadsWillRollV1;
    using K = CrucibleCarefulNegotiationV1;
    using B = CruciblePartOfTheBargainV1;
    using M = CrucibleNotMyJunkV1;
    using S = Rasa.Managers.NpcSquads;

    /// <summary>
    /// Crucible set 7, played through as a player plays it: Sergeant Melching's Heads Will Roll (1206: the three Thrax officers
    /// of one NPC squad on the Crucible), Captain DeLisi's Careful Negotiation (1519: Kalamit's sack on the Crucible, then Cirkin
    /// in the Staal Junkyard, squad instance 2138), Cirkin's Your Part of the Bargain (1529: the Technician pools, five Dolomite
    /// Blocks and the Energy Matrix in the Junkyard) and Awol Captain Cheung's Not My Junk (1538: Cirkin's talk, Lamontix's
    /// keycard, the Bane Lockbox in the Junkyard, back to Cheung). With them the Junkyard's guards (as CrucibleGuardTests holds
    /// the Crucible's): its NPC, objects, pools and markers on the navmesh and a client floor, walked to from its arrival, clear
    /// of its door, Cirkin seen from the end of the walk to him; and the officers' beat on the Crucible.
    ///
    /// Why these tests: every NPC and creature of the set is new; two missions go from the Crucible into a squad instance and
    /// back, one is bound to it; 1206 and 1538 are offered on an either-or; 1529 clears pools at once and counts a scene's
    /// uses; 1538 opens a lockbox only with the keycard. A creature or object off the navmesh or the walks, an offer before its
    /// prerequisite, a drop on the wrong side of its objective, a kill before the accept counted, a use before its objective,
    /// progress lost at a reload or kept after an abandon, an item kept from a turn-in, or a turn-in that pays twice would each
    /// break them.
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class CrucibleStaalJunkyardTests
    {
        /// <summary>The Crucible markers in the Junkyard: 1519's on Cirkin, 1529's ten, 1538's on Cirkin, Lamontix and the lockbox.</summary>
        internal const int JunkyardMarkers = 1 + 10 + 3;

        private const uint Junkyard = J.Junkyard;
        private const uint Business = CrucibleBusinessProposalV1.MissionId;
        private const uint OtherSide = CrucibleOtherSideOfTheCoinV1.MissionId;
        private const uint Amends = CrucibleMakingAmendsV1.MissionId, Revenge = CrucibleMakingAmendsV1.RevengeMissionId;
        private const uint Heads = H.MissionId, Negotiation = K.MissionId, Bargain = B.MissionId, Junk = M.MissionId;
        private const uint MissionItemFiller = 11519;      // as PlateauCampResistanceTests (inventory category 4)
        private const uint WeaponFiller = 3869;            // inventory category 1 (quest-test-pattern)
        private const uint ArmorFiller = 12748;            // Armor_T3_Bio_V01_CMN_Gloves_30_to_34 (inventory category 1)

        private static readonly Vector3 Arrival = ToVector(J.ArrivalAt);
        private static readonly Vector3 AwolWaypoint = CrucibleGuardTests.AwolCampWaypoint;
        private static readonly uint[] TechnicianPools = J.TechnicianPools.Select(pool => pool.Pool).ToArray();
        private static readonly uint[] JunkyardPools = TechnicianPools.Append(J.Lamontix).Append(J.UltraReconstructor).ToArray();

        [TestInitialize]
        public void Clock() => PlateauGiantKillerSquadTests.UseClock();

        [TestCleanup]
        public void Restore() => PlateauGiantKillerSquadTests.RestoreClock();

        /// <summary>
        /// In the squad's copy: Cirkin stands with the client's name, level 42 and package 1742, friendly, on the navmesh, 2 m or
        /// more from a wall, walked to from the arrival, ReachClearance or more outside every hostile pool's circle; the
        /// Technicians (three a pool), Lamontix and the Ultra Reconstructor come out of their pools hostile, of their classes,
        /// levels and hit points, walked to from the arrival; the six Dolomite Blocks and the lockbox stand on the navmesh,
        /// walked to; none of these within 5 m of the door's circle, no hostile pool within ReachClearance of it.
        /// </summary>
        [TestMethod]
        public void TheJunkyardsNpcCreaturesAndObjectsStandWhereTheyAreWalkedTo()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            C.EnterCrucible(harness);
            var copy = IntoJunkyard(harness);
            Assert.IsTrue(copy.IsSquadInstance);
            var cirkin = Giver(copy, J.Cirkin);
            Assert.AreEqual((J.CirkinName, J.CirkinLevel, J.CirkinPackage), (cirkin.NameId, (uint)cirkin.Level, cirkin.Npc?.NpcPackageId));
            Assert.AreNotEqual(TargetCategory.Hostile, cirkin.TargetCategory);
            AssertOnGround(copy, cirkin.Position, "Cirkin");
            AssertPath(copy, Arrival, cirkin.Position, "Cirkin from the arrival");
            Assert.IsGreaterThanOrEqualTo(2f, copy.NavMesh.DistanceToWall(cirkin.Position, 20) ?? 0, "Cirkin 2 m or more from a wall");

            var links = harness.World.Set<MapLinkEntry>().AsNoTracking().Where(link => link.Enabled != 0 && link.MapContextId == Junkyard).ToList();
            Assert.IsNotEmpty(links, "the Junkyard's door");
            float LinkGap(Vector3 at, float radius = 0) => links.Min(link =>
                Vector2.Distance(new(at.X, at.Z), new((float)link.PosX, (float)link.PosZ)) - (float)link.Radius - radius);
            Assert.IsGreaterThanOrEqualTo(5f, LinkGap(cirkin.Position), "Cirkin clear of the door");
            var hostile = harness.World.SpawnPoolEntries.AsNoTracking().Where(pool => pool.MapContextId == Junkyard && pool.Id != J.Cirkin &&
                pool.Id != 500038).ToList();
            foreach (var pool in hostile)
                Assert.IsGreaterThanOrEqualTo(MarshesNpcsStandStillV1.ReachClearance,
                    Vector2.Distance(new(cirkin.Position.X, cirkin.Position.Z), new((float)pool.PosX, (float)pool.PosZ)) - (float)pool.Radius,
                    $"Cirkin outside hostile pool {pool.Id}'s reach");

            Spawn(copy, JunkyardPools);
            foreach (var (pool, creature, cls, level, health, count) in new (uint, uint, uint, uint, uint, int)[]
            {
                (TechnicianPools[0], J.Technician, 7043, J.TechnicianLevel, J.TechnicianHitPoints, 3),
                (TechnicianPools[1], J.Technician, 7043, J.TechnicianLevel, J.TechnicianHitPoints, 3),
                (J.Lamontix, J.Lamontix, 10504, J.LamontixLevel, J.BossHitPoints, 1),
                (J.UltraReconstructor, J.UltraReconstructor, J.UltraReconstructorClass, J.UltraReconstructorLevel, J.BossHitPoints, 1)
            })
            {
                var at = ToVector(J.At(pool));
                AssertOnGround(copy, at, $"pool {pool}");
                AssertPath(copy, Arrival, at, $"pool {pool} from the arrival");
                Assert.IsGreaterThanOrEqualTo(MarshesNpcsStandStillV1.ReachClearance, LinkGap(at, J.TechnicianPoolRadius), $"pool {pool} clear of the door");
                var here = Here(copy).Where(candidate => candidate.SpawnPool?.DbId == pool && candidate.DbId == creature).ToArray();
                Assert.HasCount(count, here, $"pool {pool}");
                foreach (var one in here)
                {
                    Assert.AreEqual((level, TargetCategory.Hostile, cls), ((uint)one.Level, one.TargetCategory, (uint)one.EntityClass), $"pool {pool}");
                    Assert.AreEqual((int)health, (int)one.Attributes[Attributes.Health].NormalMax, $"pool {pool}: its hit points");
                }
            }
            foreach (var (label, at) in B.BlocksAt.Select((at, index) => ($"block {index + 1}", ToVector(at))).Append(("the lockbox", ToVector(M.LockboxAt))))
            {
                AssertOnGround(copy, at, label);
                AssertPath(copy, Arrival, at, $"{label} from the arrival");
                Assert.IsGreaterThanOrEqualTo(5f, LinkGap(at), $"{label} clear of the door");
            }
        }

        /// <summary>
        /// The client's floors (MarshesClientFloorTests; inconclusive without the client's files): Cirkin, the blocks, the
        /// lockbox, the Junkyard's new hostile pools and its markers each on a client floor with none just over it; Cirkin seen
        /// from 16 of 16 places 2 m round him and from each point of the walk from the arrival 5 to 2 m from him.
        /// </summary>
        [TestMethod]
        public void TheJunkyardsPlacesAndMarkersStandOnAClientFloorAndCirkinIsSeen()
        {
            const float MarkerHeight = 2;
            var dataDir = F.ClientData();
            using var harness = WildernessRuntimeTestHarness.Create();
            C.EnterCrucible(harness);
            var copy = IntoJunkyard(harness);
            using var meshes = new MeshLibrary(dataDir, F.MeshCsv());
            var name = harness.World.MapInfoEntries.AsNoTracking().Single(row => row.Id == Junkyard).MapName;
            var geometry = MapGeometry.Load(Path.Combine(dataDir, "maps", name), meshes, 1);

            var cirkin = ToVector(J.CirkinAt);
            var places = new List<(string Label, Vector3 At)> { ("Cirkin", cirkin), ("the lockbox", ToVector(M.LockboxAt)) };
            places.AddRange(B.BlocksAt.Select((at, index) => ($"block {index + 1}", ToVector(at))));
            places.AddRange(JunkyardPools.Select(pool => ($"pool {pool}", ToVector(J.At(pool)))));
            var (floors, ceilings) = F.Surfaces(geometry, places.Select(place => (place.At, F.Walkable)).ToList());
            var failures = new List<string>();
            for (var i = 0; i < places.Count; i++)
                if (places[i].At.Y - floors[i] > F.MaxOver || !float.IsPositiveInfinity(ceilings[i]))
                    failures.Add($"{places[i].Label} at {places[i].At}: floor {floors[i]}, ceiling {ceilings[i]}");

            var catalog = new MissionContentCatalog(harness, new Dictionary<uint, Mission>(), null);
            catalog.Load();
            var markers = harness.World.MissionIndicatorEntries.AsNoTracking().Where(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision).ToList()
                .Where(marker => MissionMarkerProjection.ResolveMap(catalog, marker.MissionId, marker.ObjectiveId, new MissionIndicator { IndicatorId = marker.IndicatorId }) == Junkyard)
                .Select(marker => (Label: $"{marker.MissionId}.{marker.ObjectiveId}/{marker.IndicatorId}", At: new Vector3((float)marker.PosX, (float)marker.PosY, (float)marker.PosZ)))
                .ToList();
            Assert.HasCount(JunkyardMarkers, markers, "the Junkyard's markers");
            var (markerFloors, _) = F.Surfaces(geometry, markers.Select(marker => (marker.At + new Vector3(0, MarkerHeight - F.MaxUnder, 0), F.Walkable)).ToList());
            for (var i = 0; i < markers.Count; i++)
            {
                var ground = copy.NavMesh.GroundHeight(markers[i].At + new Vector3(0, MarkerHeight, 0));
                if (ground is not { } y || MathF.Abs(y - markers[i].At.Y) > MarkerHeight)
                    failures.Add($"{markers[i].Label}: navmesh {ground}");
                if (markers[i].At.Y - markerFloors[i] > MarkerHeight)
                    failures.Add($"{markers[i].Label}: client floor {markerFloors[i]}");
            }

            var near = F.Near(geometry, new List<Vector3> { cirkin }, 8)[0];
            var aim = cirkin + new Vector3(0, F.NpcMiddle, 0);
            for (var k = 0; k < F.RingPlaces; k++)
            {
                var angle = k * 2 * MathF.PI / F.RingPlaces;
                var stand = cirkin + new Vector3(MathF.Sin(angle) * F.Ring, 0, MathF.Cos(angle) * F.Ring);
                if (F.FloorAt(near, stand, cirkin.Y - F.MaxOver, cirkin.Y + F.MaxUnder) is not { } fy || !F.Clear(near, stand with { Y = fy + F.Eye }, aim))
                    failures.Add($"Cirkin not seen from ({stand.X:F1}, {stand.Z:F1})");
            }
            var walk = copy.NavMesh.FindPath(copy.NavMesh.Nearest(Arrival).Value, cirkin, out var complete);
            Assert.IsTrue(walk != null && complete, "the walk to Cirkin");
            var ends = 0;
            foreach (var distance in new[] { 5f, 4.5f, 4f, 3.5f, 3f, 2.5f, 2f })
                for (var i = 0; i + 1 < walk.Count; i++)
                {
                    Vector3? hit = null;
                    for (var step = 0; step <= 50 && hit == null; step++)
                    {
                        var at = Vector3.Lerp(walk[i], walk[i + 1], step / 50f);
                        if (Vector3.Distance(at, cirkin) <= distance)
                            hit = at;
                    }
                    if (hit is not { } end)
                        continue;
                    ends++;
                    var floor = F.FloorAt(near, end, end.Y - 1.5f, end.Y + 1f) ?? end.Y;
                    if (!F.Clear(near, end with { Y = floor + F.Eye }, aim))
                        failures.Add($"Cirkin not seen from the walk's point ({end.X:F1}, {end.Z:F1}) {distance} m off");
                    break;
                }
            Assert.AreEqual(7, ends, "the walk's points 5 to 2 m from Cirkin");
            Assert.IsEmpty(failures, string.Join("; ", failures));
        }

        /// <summary>
        /// On the Crucible: the officers' beat, each point on the navmesh and a client floor, each leg walked, every point 10 m
        /// or more outside every map link's circle, the first walked to from the Awol Camp waypoint; the squad comes out at its
        /// start, Krubb leading Mungrel, Sedge and a Shield Drone, all hostile, and walks; Kalamit's pool on a client floor,
        /// walked to from the Labbna Quarter waypoint, ReachClearance or more outside every link circle, 20 m or more from every
        /// Crucible NPC; Kalamit and the Ultra Reconstructor attack with damage_type 0 (the client's weapon table decides).
        /// </summary>
        [TestMethod]
        public void TheOfficersBeatAndKalamitStandOnTheCrucible()
        {
            var dataDir = F.ClientData();
            using var harness = WildernessRuntimeTestHarness.Create();
            var crucible = C.EnterCrucible(harness);
            var links = harness.World.Set<MapLinkEntry>().AsNoTracking().Where(link => link.Enabled != 0 && link.MapContextId == CrucibleMissionDataV1.MapContextId).ToList();
            float LinkGap(Vector3 at) => links.Min(link => Vector2.Distance(new(at.X, at.Z), new((float)link.PosX, (float)link.PosZ)) - (float)link.Radius);
            var beat = J.Beat.Select(ToVector).ToArray();
            AssertPath(crucible, AwolWaypoint, beat[0], "the beat's start from the Awol Camp");
            for (var step = 0; step < beat.Length; step++)
            {
                AssertOnGround(crucible, beat[step], $"beat point {step}");
                Assert.IsGreaterThanOrEqualTo(10f, LinkGap(beat[step]), $"beat point {step} clear of the links");
                if (step + 1 < beat.Length)
                    AssertPath(crucible, beat[step], beat[step + 1], $"beat leg {step}");
            }
            var kalamit = ToVector(J.KalamitAt);
            AssertOnGround(crucible, kalamit, "Kalamit's pool");
            AssertPath(crucible, C.LabbnaWaypoint, kalamit, "Kalamit from the Labbna Quarter");
            Assert.IsGreaterThanOrEqualTo(MarshesNpcsStandStillV1.ReachClearance, LinkGap(kalamit) - J.KalamitPoolRadius, "Kalamit clear of the links");
            var friendly = harness.World.SpawnPoolEntries.AsNoTracking().Where(pool => pool.MapContextId == CrucibleMissionDataV1.MapContextId &&
                (pool.Id >= 510000 && pool.Id < 520000 || pool.Id >= 770000 && pool.Id <= 770030)).ToList()
                .Where(pool => harness.World.CreatureEntries.AsNoTracking().Any(row => row.Id == pool.Creature1Id && row.Faction == 1)).ToList();
            Assert.IsNotEmpty(friendly);
            foreach (var pool in friendly)
                Assert.IsGreaterThanOrEqualTo(20f + J.KalamitPoolRadius, Vector2.Distance(new(kalamit.X, kalamit.Z), new((float)pool.PosX, (float)pool.PosZ)),
                    $"Kalamit far from NPC pool {pool.Id}");
            foreach (var attack in new[] { J.LasherAttack, J.ReconstructorAttack })
                Assert.AreEqual(0U, (uint)harness.World.Set<CreatureActionEntry>().AsNoTracking().Single(row => row.Id == attack).DamageType,
                    $"attack {attack}: damage_type 0");

            using (var meshes = new MeshLibrary(dataDir, F.MeshCsv()))
            {
                var name = harness.World.MapInfoEntries.AsNoTracking().Single(row => row.Id == CrucibleMissionDataV1.MapContextId).MapName;
                var geometry = MapGeometry.Load(Path.Combine(dataDir, "maps", name), meshes, 1);
                var points = beat.Append(kalamit).ToList();
                var (floors, ceilings) = F.Surfaces(geometry, points.Select(at => (at, F.Walkable)).ToList());
                var failures = points.Where((at, i) => at.Y - floors[i] > F.MaxOver || !float.IsPositiveInfinity(ceilings[i])).Select(at => at.ToString()).ToList();
                Assert.IsEmpty(failures, string.Join("; ", failures));
            }

            var (krubb, others) = Officers(harness, crucible);
            Assert.IsLessThan(8f, Vector3.Distance(krubb.Position, beat[0]), "the squad comes out at the beat's start");
            CollectionAssert.AreEquivalent(new[] { J.Mungrel, J.Sedge, J.ShieldDrone }, others.Select(one => one.DbId).ToArray());
            foreach (var one in others.Append(krubb))
                Assert.AreEqual(TargetCategory.Hostile, one.TargetCategory, $"{one.DbId} hostile");
            Assert.AreEqual(S.PathMode.Back, S.Paths[J.Squad].Mode, "there and back");
            var start = krubb.Position;
            for (var tick = 0; tick < 120; tick++)
                PlateauGiantKillerSquadTests.Step(harness, crucible, 250);
            Assert.IsGreaterThan(10f, Vector3.Distance(start, krubb.Position), "the officers walk their beat");
        }

        /// <summary>
        /// Heads Will Roll: not offered before The Other Side Of The Coin or Making Amends; after either, Melching offers it (only
        /// he); an officer killed before the accept does not count; each kill completes its own objective only (a reload
        /// halfway kept); the third completes the mission once; a marker for each at the rock bridge; Melching pays the weapon
        /// chosen, once. An abandon halfway starts it over.
        /// </summary>
        [TestMethod]
        public void HeadsWillRollKillsTheThreeOfficersForMelching()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var crucible = C.EnterCrucible(harness, AwolWaypoint);
            var melching = Npc(crucible, G.Melching);
            var cheung = Npc(crucible, CrucibleAwolCampNpcsV1.Cheung);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, melching, Heads), "it waits for 1153 or 1155");
            C.SeedCompleted(harness, Amends);
            crucible = C.EnterCrucible(harness, AwolWaypoint);
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, melching, Heads), "Making Amends opens it");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, cheung, Heads), "not Cheung's to give");
            var offer = WildernessRanjaGorgeTests.Offer(harness, melching, Heads);
            CollectionAssert.AreEqual(new[] { H.LaserBlade, H.FireInjectionGun, H.ElectricMachineGun, H.IceShotgun },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());

            var (krubb, others) = Officers(harness, crucible);
            KillOfficer(harness, crucible, others.Single(one => one.DbId == J.Sedge));
            Accept(harness, melching, Heads);
            Assert.IsTrue(H.Officers.All(officer => Objective(harness, Heads, officer.Objective) == MissionObjectiveState.Incomplete),
                "the kill before the accept does not count");
            var markers = harness.Manager.BuildStatusSnapshot(harness.Client.Player)[Heads].ObjectivesList;
            Assert.HasCount(3, markers);
            foreach (var objective in markers)
                Assert.IsLessThan(0.1f, Vector2.Distance(new(objective.IndicatorList.Single().Position.X, objective.IndicatorList.Single().Position.Z),
                    new((float)J.RockBridgeAt.X, (float)J.RockBridgeAt.Z)), "the marker at the rock bridge");

            KillOfficer(harness, crucible, others.Single(one => one.DbId == J.ShieldDrone));
            Assert.IsTrue(H.Officers.All(officer => Objective(harness, Heads, officer.Objective) == MissionObjectiveState.Incomplete), "the drone counts for none");
            KillOfficer(harness, crucible, krubb);
            Assert.AreEqual((MissionObjectiveState.Completed, MissionObjectiveState.Incomplete, MissionObjectiveState.Incomplete),
                (Objective(harness, Heads, H.KillKrubb), Objective(harness, Heads, H.KillMungrel), Objective(harness, Heads, H.KillSedge)));

            // An abandon takes the kill; taken again, each officer counts anew.
            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Heads));
            harness.Drain();
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, melching, Heads), "offered again after an abandon");
            Accept(harness, melching, Heads);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Heads, H.KillKrubb), "Krubb again");

            KillOfficer(harness, crucible, others.Single(one => one.DbId == J.Mungrel));
            WildernessRanjaGorgeTests.Reload(harness);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Heads, H.KillMungrel), "kept at a reload");
            harness.Drain();
            var (krubbAgain, othersAgain) = Officers(harness, crucible, sendAgain: true);
            var log = new List<object>();
            log.AddRange(KillOfficer(harness, crucible, othersAgain.Single(one => one.DbId == J.Sedge)));
            Assert.IsFalse(harness.Client.Player.Missions[Heads].Completeable, "Krubb still stands");
            log.AddRange(KillOfficer(harness, crucible, krubbAgain));
            Assert.IsTrue(H.Officers.All(officer => Objective(harness, Heads, officer.Objective) == MissionObjectiveState.Completed), "all three");
            Assert.AreEqual(1, log.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Heads && packet.IsCompleteable), "MissionCompleteable once");

            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, cheung.EntityId, Heads, 0), "not Cheung's to take");
            FullBagWaits(harness, melching, Heads, 0, WeaponFiller);
            FailedTurnInPaysNothing(harness, melching, Heads, 0);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, melching, Heads, 0, H.Experience, H.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Heads), "MissionRewarded once");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, H.LaserBlade), "the blade chosen");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, melching, Heads), "done once");
        }

        /// <summary>The other way to Heads Will Roll and to Not My Junk: The Other Side Of The Coin opens 1206; Sweet Revenge opens 1538.</summary>
        [TestMethod]
        public void HeadsWillRollAndNotMyJunkTakeEitherBranch()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var crucible = C.EnterCrucible(harness, AwolWaypoint);
            var melching = Npc(crucible, G.Melching);
            var cheung = Npc(crucible, CrucibleAwolCampNpcsV1.Cheung);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, cheung, Junk), "it waits for 1205 or 1155");
            C.SeedCompleted(harness, OtherSide);
            crucible = C.EnterCrucible(harness, AwolWaypoint);
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, melching, Heads), "The Other Side Of The Coin opens 1206");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, cheung, Junk), "1153 does not open 1538");
            C.SeedCompleted(harness, Revenge);
            crucible = C.EnterCrucible(harness, AwolWaypoint);
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, cheung, Junk), "Sweet Revenge opens 1538");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, melching, Junk), "not Melching's to give");
        }

        /// <summary>
        /// Careful Negotiation after Business Proposal: DeLisi offers it (only she); Kalamit killed before it drops no sack, nor a
        /// Lasher of set 3 after it; Kalamit drops the sack (chance 100: no roll) and a death is not a pickup; the sack completes 1
        /// and opens 2, its markers on the bridge road and on Cirkin in the Junkyard; a reload keeps it; through the Junkyard's
        /// door, the walk up to Cirkin completes 2 once; Cirkin (not DeLisi) takes the sack and pays the weapon chosen, once.
        /// </summary>
        [TestMethod]
        public void CarefulNegotiationTakesKalamitsSackToCirkin()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var crucible = C.EnterCrucible(harness);
            var delisi = Npc(crucible, CrucibleOutpostIntrepidNpcsV1.DeLisi);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, delisi, Negotiation), "it waits for Business Proposal");
            C.SeedCompleted(harness, Business);
            crucible = C.EnterCrucible(harness);
            var offer = WildernessRanjaGorgeTests.Offer(harness, delisi, Negotiation);
            CollectionAssert.AreEqual(new[] { K.SonicRifle, K.SonicRocketLauncher, K.VirulentLeechGun, K.ElectricMachineGun },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Spawn(crucible, J.KalamitPool, CrucibleWardensAndLashersNpcsV1.LasherPools[0].Pool);
            Assert.IsFalse(Holds(Kill(harness, crucible, J.Kalamit), K.SackTemplate), "no sack before the mission");
            Respawn(crucible, J.KalamitPool);
            Accept(harness, delisi, Negotiation);
            Assert.AreEqual((MissionObjectiveState.Incomplete, MissionObjectiveState.Inactive),
                (Objective(harness, Negotiation, K.KillKalamit), Objective(harness, Negotiation, K.MeetCirkin)));
            var marker = harness.Manager.BuildStatusSnapshot(harness.Client.Player)[Negotiation].ObjectivesList.Single().IndicatorList.Single();
            Assert.IsLessThan(0.1f, Vector2.Distance(new(marker.Position.X, marker.Position.Z), new((float)J.KalamitAt.X, (float)J.KalamitAt.Z)), "on Kalamit");

            Assert.IsFalse(Holds(Kill(harness, crucible, CrucibleWardensAndLashersNpcsV1.Lasher), K.SackTemplate), "a Lasher has no sack");
            var sack = Kill(harness, crucible, J.Kalamit);
            Assert.IsTrue(Holds(sack, K.SackTemplate), "Kalamit drops it (chance 100: no roll)");
            Assert.AreEqual(0U, Sacks(harness), "a death is not a pickup");
            PickUp(harness, sack);
            var packets = harness.Drain();
            Assert.AreEqual(1U, Sacks(harness));
            Assert.AreEqual(1, packets.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Negotiation && packet.ObjectiveId == K.KillKalamit));
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Negotiation, K.MeetCirkin), "the sack opens 2");
            var meet = harness.Manager.BuildStatusSnapshot(harness.Client.Player)[Negotiation].ObjectivesList
                .Single(objective => objective.ObjectiveId == K.MeetCirkin).IndicatorList;
            CollectionAssert.AreEquivalent(new[] { (CrucibleMissionDataV1.MapContextId, (float)J.BridgeAt.X) },
                meet.Select(one => (one.MapContextId, one.Position.X)).ToArray(), "on the Crucible, the bridge road (the map's markers only)");
            Enter(harness, ToVector(J.BridgeAt));
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Negotiation, K.MeetCirkin), "the bridge road is not Cirkin");

            WildernessRanjaGorgeTests.Reload(harness);
            Assert.AreEqual((1U, MissionObjectiveState.Incomplete), (Sacks(harness), Objective(harness, Negotiation, K.MeetCirkin)), "kept at a reload");
            harness.Drain();

            var copy = IntoJunkyard(harness);
            var cirkin = Giver(copy, J.Cirkin);
            meet = harness.Manager.BuildStatusSnapshot(harness.Client.Player)[Negotiation].ObjectivesList
                .Single(objective => objective.ObjectiveId == K.MeetCirkin).IndicatorList;
            CollectionAssert.AreEquivalent(new[] { (Junkyard, (float)J.CirkinAt.X) },
                meet.Select(one => (one.MapContextId, one.Position.X)).ToArray(), "in the Junkyard, Cirkin");
            Assert.IsFalse(harness.Client.Player.Missions[Negotiation].Completeable);
            Enter(harness, ToVector(J.CirkinAt));
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Negotiation, K.MeetCirkin), "Cirkin met");
            Assert.IsTrue(harness.Client.Player.Missions[Negotiation].Completeable);

            FullBagWaits(harness, cirkin, Negotiation, 1, WeaponFiller);
            FailedTurnInPaysNothing(harness, cirkin, Negotiation, 1);
            Assert.AreEqual(1U, Sacks(harness), "kept at the failed turn-in");
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, cirkin, Negotiation, 1, K.Experience, K.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Negotiation), "MissionRewarded once");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, K.SonicRocketLauncher), "the launcher chosen");
            Assert.AreEqual(0U, Sacks(harness), "Cirkin takes the sack");
        }

        /// <summary>An abandon with the sack held keeps it; taken again, the held sack completes 1 at once and opens 2.</summary>
        [TestMethod]
        public void CarefulNegotiationAbandonedKeepsTheSack()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            C.SeedCompleted(harness, Business);
            var crucible = C.EnterCrucible(harness);
            var delisi = Npc(crucible, CrucibleOutpostIntrepidNpcsV1.DeLisi);
            Spawn(crucible, J.KalamitPool);
            Accept(harness, delisi, Negotiation);
            PickUp(harness, Kill(harness, crucible, J.Kalamit));
            Assert.AreEqual(1U, Sacks(harness));
            harness.Drain();
            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Negotiation));
            harness.Drain();
            Assert.AreEqual(1U, Sacks(harness), "the sack is kept");
            Accept(harness, delisi, Negotiation);
            Assert.AreEqual((MissionObjectiveState.Completed, MissionObjectiveState.Incomplete),
                (Objective(harness, Negotiation, K.KillKalamit), Objective(harness, Negotiation, K.MeetCirkin)), "the held sack counts");
        }

        /// <summary>
        /// Your Part of the Bargain after Careful Negotiation: only Cirkin offers it, in the Junkyard; the Technicians of one pool
        /// dead leave 1 open, both pools dead complete it; the six blocks are the player's own, each gives one block once (a
        /// reload in the middle of the scene kept), the fifth completes 2, the sixth stays and gives nothing; the Ultra Reconstructor killed
        /// before the accept drops no Energy Matrix, after it (3 open from the accept) drops one (chance 100: no roll); 1, 2 and
        /// 3 done open 4 (the tracker hidden); the walk back to Cirkin completes it once; Cirkin takes the blocks and the matrix
        /// and pays.
        /// </summary>
        [TestMethod]
        public void YourPartOfTheBargainPaysCirkinsPrice()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            C.SeedCompleted(harness, Business);
            C.EnterCrucible(harness);
            var copy = IntoJunkyard(harness);
            var cirkin = Giver(copy, J.Cirkin);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, cirkin, Bargain), "it waits for Careful Negotiation");
            C.SeedCompleted(harness, Negotiation);
            copy = IntoJunkyard(harness);
            cirkin = Giver(copy, J.Cirkin);
            var offer = WildernessRanjaGorgeTests.Offer(harness, cirkin, Bargain);
            CollectionAssert.AreEqual(new[] { B.GravitonHelmet, B.HazmatHelmet, B.ReflectiveBoots, B.BioHelmet },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Stand(copy, J.UltraReconstructor);
            Assert.IsFalse(Holds(Kill(harness, copy, J.UltraReconstructor), B.EnergyMatrixTemplate), "no matrix before the mission");
            Accept(harness, cirkin, Bargain);
            Tick(harness, copy, 250);
            Assert.AreEqual((MissionObjectiveState.Incomplete, MissionObjectiveState.Incomplete, MissionObjectiveState.Incomplete, MissionObjectiveState.Inactive),
                (Objective(harness, Bargain, B.Technicians), Objective(harness, Bargain, B.Dolomite), Objective(harness, Bargain, B.EnergyMatrix),
                    Objective(harness, Bargain, B.ReturnToCirkin)));
            var visible = harness.Manager.BuildStatusSnapshot(harness.Client.Player)[Bargain].ObjectivesList.Select(objective => objective.ObjectiveId).ToArray();
            Assert.IsFalse(visible.Contains(B.Tracker) || visible.Contains(B.Opened) || visible.Contains(B.DolomiteCount), "the tracker is hidden");

            // 1: one pool dead is not all of them.
            KillPools(harness, copy, new[] { TechnicianPools[0] });
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Bargain, B.Technicians), "one pool of two");
            KillPools(harness, copy, TechnicianPools);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Bargain, B.Technicians), "both pools dead");

            // 2: the blocks.
            for (var index = 0; index < B.BlocksAt.Length - 1; index++)
            {
                if (index == 2)
                {
                    ReloadAndRecover(harness, copy);
                    Assert.AreEqual(2U, Blocks(harness), "kept at a reload in the middle of the scene");
                    Assert.IsFalse(SceneObject(harness, copy, Bargain, B.BlockRole(0)).IsEnabled, "a taken block stays taken");
                }
                var block = SceneObject(harness, copy, Bargain, B.BlockRole(index));
                Assert.AreEqual((B.BlockClass, B.BlockName), ((uint)block.EntityClassId, block.NameOverrideId));
                Use(harness, copy, block, B.UseArgument);
                Use(harness, copy, block, B.UseArgument);
                Assert.AreEqual((uint)index + 1, Blocks(harness), $"block {index + 1}, once");
                Assert.IsFalse(block.IsEnabled, $"block {index + 1} closed");
                Assert.AreEqual(index < 4 ? MissionObjectiveState.Incomplete : MissionObjectiveState.Completed, Objective(harness, Bargain, B.Dolomite));
            }
            Tick(harness, copy, 250);
            var sixth = SceneObject(harness, copy, Bargain, B.BlockRole(5));
            Assert.IsTrue(sixth.IsEnabled, "the sixth block stays");
            Use(harness, copy, sixth, B.UseArgument);
            Assert.AreEqual(B.Blocks, Blocks(harness), "a sixth use gives nothing: 2 is done and the binding holds five");
            Assert.AreEqual(MissionObjectiveState.Inactive, Objective(harness, Bargain, B.ReturnToCirkin), "the matrix still to come");

            // 3: the Energy Matrix.
            Stand(copy, J.UltraReconstructor);
            var matrix = Kill(harness, copy, J.UltraReconstructor);
            Assert.IsTrue(Holds(matrix, B.EnergyMatrixTemplate), "the Ultra Reconstructor drops it (chance 100: no roll)");
            Assert.AreEqual(0U, Matrices(harness), "a death is not a pickup");
            PickUp(harness, matrix);
            harness.Drain();
            Tick(harness, copy, 250);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Bargain, B.EnergyMatrix));
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Bargain, B.ReturnToCirkin), "1, 2 and 3 open the return");
            Assert.IsFalse(harness.Client.Player.Missions[Bargain].Completeable);

            Enter(harness, ToVector(J.CirkinAt));
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Bargain, B.ReturnToCirkin));
            Assert.IsTrue(harness.Client.Player.Missions[Bargain].Completeable);
            FullBagWaits(harness, cirkin, Bargain, 2, ArmorFiller);
            FailedTurnInPaysNothing(harness, cirkin, Bargain, 2);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, cirkin, Bargain, 2, B.Experience, B.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Bargain), "MissionRewarded once");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, B.ReflectiveBoots), "the boots chosen");
            Assert.AreEqual((0U, 0U), (Blocks(harness), Matrices(harness)), "Cirkin takes the blocks and the matrix");
        }

        /// <summary>
        /// An abandon of Your Part of the Bargain with two blocks and the matrix held takes the blocks, keeps the matrix; taken
        /// again, all six blocks stand and give again, the held matrix completes 3 at once.
        /// </summary>
        [TestMethod]
        public void YourPartOfTheBargainAbandonedStartsTheBlocksOver()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            C.SeedCompleted(harness, Business, Negotiation);
            C.EnterCrucible(harness);
            var copy = IntoJunkyard(harness);
            var cirkin = Giver(copy, J.Cirkin);
            Accept(harness, cirkin, Bargain);
            Tick(harness, copy, 250);
            Use(harness, copy, SceneObject(harness, copy, Bargain, B.BlockRole(0)), B.UseArgument);
            Use(harness, copy, SceneObject(harness, copy, Bargain, B.BlockRole(1)), B.UseArgument);
            Stand(copy, J.UltraReconstructor);
            PickUp(harness, Kill(harness, copy, J.UltraReconstructor));
            harness.Drain();
            Assert.AreEqual((2U, 1U), (Blocks(harness), Matrices(harness)));

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Bargain));
            harness.Drain();
            Assert.AreEqual((0U, 1U), (Blocks(harness), Matrices(harness)), "the blocks go, the matrix stays");
            Accept(harness, cirkin, Bargain);
            Tick(harness, copy, 250);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Bargain, B.EnergyMatrix), "the held matrix counts");
            for (var index = 0; index < B.BlocksAt.Length; index++)
                Assert.IsTrue(SceneObject(harness, copy, Bargain, B.BlockRole(index)).IsEnabled, $"block {index + 1} stands again");
            Use(harness, copy, SceneObject(harness, copy, Bargain, B.BlockRole(0)), B.UseArgument);
            Assert.AreEqual(1U, Blocks(harness), "the first block gives again");
        }

        /// <summary>
        /// Not My Junk after Making Amends: Cheung offers it (only he); taken on the Crucible, its lockbox stands only in the
        /// squad's Junkyard; Cirkin hears 1 (only that), which opens 2; Lamontix killed drops the keycard (chance 100; none
        /// before 2 is open); the lockbox takes no use before the keycard is held; with it, one use gives the converter once and
        /// takes the keycard (a reload kept), and opens 3; back on the Crucible the walk up to Cheung completes 3; a full bag
        /// does not stop the Eloh Resonator (it takes the converter's slot); Cheung takes the converter and pays the Eloh Resonator, once.
        /// </summary>
        [TestMethod]
        public void NotMyJunkBringsCheungThePlasmaPowerConverter()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            C.SeedCompleted(harness, Amends);
            var crucible = C.EnterCrucible(harness, AwolWaypoint);
            var cheung = Npc(crucible, CrucibleAwolCampNpcsV1.Cheung);
            var melching = Npc(crucible, G.Melching);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, melching, Junk), "not Melching's to give");
            var offer = WildernessRanjaGorgeTests.Offer(harness, cheung, Junk);
            Assert.IsEmpty(offer.SelectableReward, "nothing to choose");
            CollectionAssert.AreEqual(new[] { M.ElohResonator }, offer.FixedReward.FixedItems.Select(item => item.ItemTemplateId).ToArray());
            Accept(harness, cheung, Junk);
            Assert.AreEqual((MissionObjectiveState.Incomplete, MissionObjectiveState.Inactive, MissionObjectiveState.Inactive, MissionObjectiveState.Inactive),
                (Objective(harness, Junk, M.SpeakToCirkin), Objective(harness, Junk, M.KillLamontix), Objective(harness, Junk, M.Retrieve),
                    Objective(harness, Junk, M.ReturnToCheung)));
            Assert.IsNull(FindSceneObject(harness, crucible, Junk, M.LockboxRole), "no lockbox on the Crucible");

            var copy = IntoJunkyard(harness);
            Tick(harness, copy, 250);
            var lockbox = SceneObject(harness, copy, Junk, M.LockboxRole);
            Assert.AreEqual((M.LockboxClass, M.LockboxName), ((uint)lockbox.EntityClassId, lockbox.NameOverrideId));
            Stand(copy, J.Lamontix);
            Assert.IsFalse(Holds(Kill(harness, copy, J.Lamontix), M.KeycardTemplate), "no keycard before Cirkin's talk");
            Stand(copy, J.Lamontix);
            Use(harness, copy, lockbox, M.UseArgument);
            Assert.AreEqual(0U, Converters(harness), "the lockbox takes no use before the keycard");

            var cirkin = Giver(copy, J.Cirkin);
            Talk(harness, cirkin, Junk, M.SpeakToCirkin);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Junk, M.KillLamontix), "the talk opens 2");
            Use(harness, copy, lockbox, M.UseArgument);
            Assert.AreEqual(0U, Converters(harness), "no use without the keycard");
            Assert.IsTrue(lockbox.IsEnabled);
            var card = Kill(harness, copy, J.Lamontix);
            Assert.IsTrue(Holds(card, M.KeycardTemplate), "Lamontix drops it");
            Assert.AreEqual(0U, Keycards(harness), "a death is not a pickup");
            PickUp(harness, card);
            harness.Drain();
            Assert.AreEqual((1U, MissionObjectiveState.Completed, MissionObjectiveState.Incomplete),
                (Keycards(harness), Objective(harness, Junk, M.KillLamontix), Objective(harness, Junk, M.Retrieve)), "the keycard opens 4");

            ReloadAndRecover(harness, copy);
            lockbox = SceneObject(harness, copy, Junk, M.LockboxRole);
            Assert.AreEqual(1U, Keycards(harness), "kept at a reload");
            Use(harness, copy, lockbox, M.UseArgument);
            Use(harness, copy, lockbox, M.UseArgument);
            Assert.AreEqual((1U, 0U), (Converters(harness), Keycards(harness)), "the converter once; the keycard taken");
            Assert.IsFalse(lockbox.IsEnabled, "the lockbox closed");
            Assert.AreEqual((MissionObjectiveState.Completed, MissionObjectiveState.Incomplete),
                (Objective(harness, Junk, M.Retrieve), Objective(harness, Junk, M.ReturnToCheung)));

            harness.Manager.Scenes.Detach(harness.Client, copy);
            crucible = C.EnterCrucible(harness, AwolWaypoint);
            cheung = Npc(crucible, CrucibleAwolCampNpcsV1.Cheung);
            Assert.IsFalse(harness.Client.Player.Missions[Junk].Completeable);
            Enter(harness, ToVector(CrucibleAwolCampNpcsV1.At(CrucibleAwolCampNpcsV1.Cheung)));
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Junk, M.ReturnToCheung));
            Assert.IsTrue(harness.Client.Player.Missions[Junk].Completeable);
            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, melching.EntityId, Junk, null), "not Melching's to take");
            FailedTurnInPaysNothing(harness, cheung, Junk, null);
            // A full bag: the converter's slot, freed by the turn-in, takes the Eloh Resonator (both inventory category 4).
            WildernessAliaBranchesTests.FillCategory(harness, MissionItemFiller, 50);
            harness.FillInbox();
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, cheung, Junk, null, M.Experience, M.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Junk), "MissionRewarded once");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, M.ElohResonator), "the Eloh Resonator");
            Assert.AreEqual(0U, Converters(harness), "Cheung takes the converter");
        }

        /// <summary>
        /// An abandon of Not My Junk with the converter held takes the converter (the keycard is gone); taken again, the talk
        /// must be had again and the lockbox stands closed to a use until the keycard is held again.
        /// </summary>
        [TestMethod]
        public void NotMyJunkAbandonedStartsOver()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            C.SeedCompleted(harness, Revenge);
            var crucible = C.EnterCrucible(harness, AwolWaypoint);
            var cheung = Npc(crucible, CrucibleAwolCampNpcsV1.Cheung);
            Accept(harness, cheung, Junk);
            var copy = IntoJunkyard(harness);
            Tick(harness, copy, 250);
            Talk(harness, Giver(copy, J.Cirkin), Junk, M.SpeakToCirkin);
            Stand(copy, J.Lamontix);
            PickUp(harness, Kill(harness, copy, J.Lamontix));
            harness.Drain();
            Use(harness, copy, SceneObject(harness, copy, Junk, M.LockboxRole), M.UseArgument);
            Assert.AreEqual(1U, Converters(harness));

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Junk));
            harness.Drain();
            Assert.AreEqual((0U, 0U), (Converters(harness), Keycards(harness)), "the converter goes with the abandon");
            harness.Manager.Scenes.Detach(harness.Client, copy);
            crucible = C.EnterCrucible(harness, AwolWaypoint);
            Accept(harness, Npc(crucible, CrucibleAwolCampNpcsV1.Cheung), Junk);
            copy = IntoJunkyard(harness);
            Tick(harness, copy, 250);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Junk, M.SpeakToCirkin), "the talk again");
            var lockbox = SceneObject(harness, copy, Junk, M.LockboxRole);
            Assert.IsTrue(lockbox.IsEnabled, "a new lockbox");
            Use(harness, copy, lockbox, M.UseArgument);
            Assert.AreEqual(0U, Converters(harness), "closed until the keycard");
        }

        /// <summary>
        /// In the squad's copy, Lamontix and the Ultra Reconstructor come back on their pools' own 90 s (1538's and 1529's scenes
        /// name them public-spawn actors), not after the copy's 45 minutes: not back at 80 s, back by 100 s. A Technician pool
        /// (not named) is not back at 100 s.
        /// </summary>
        [TestMethod]
        public void LamontixAndTheUltraReconstructorComeBackOnTheirOwnTimeInTheCopy()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            C.EnterCrucible(harness);
            var copy = IntoJunkyard(harness);
            Assert.IsNotNull(copy.SquadState, "a squad's copy");
            foreach (var pool in new[] { J.Lamontix, J.UltraReconstructor })
                Assert.IsTrue(SpawnPoolManager.Instance.IsNamedPublicSpawn(pool), $"pool {pool} named by a scene");
            foreach (var pool in TechnicianPools)
                Assert.IsFalse(SpawnPoolManager.Instance.IsNamedPublicSpawn(pool), $"pool {pool} not named");
            var pools = new[] { J.Lamontix, J.UltraReconstructor, TechnicianPools[0] };
            Spawn(copy, pools);
            foreach (var creature in Here(copy).Where(one => pools.Contains(one.SpawnPool?.DbId ?? 0)).ToArray())
                Kill(harness, copy, creature);
            bool Standing(uint pool) => Here(copy).Any(one => one.SpawnPool?.DbId == pool && one.State != CharacterState.Dead);
            Assert.IsFalse(pools.Any(Standing), "all down");

            var all = copy.SpawnPools.ToArray();
            void Run(int seconds)
            {
                try
                {
                    copy.SpawnPools.Clear();
                    copy.SpawnPools.AddRange(all.Where(pool => pools.Contains(pool.DbId)));
                    for (var second = 0; second < seconds; second++)
                    {
                        harness.UtcNow = harness.UtcNow.AddSeconds(1);
                        SpawnPoolManager.Instance.SpawnPoolWorker(copy, 1000);
                    }
                }
                finally
                {
                    copy.SpawnPools.Clear();
                    copy.SpawnPools.AddRange(all);
                }
                harness.Drain();
            }
            Run(80);
            Assert.IsFalse(Standing(J.Lamontix) || Standing(J.UltraReconstructor), "not back at 80 s");
            Run(20);
            Assert.IsTrue(Standing(J.Lamontix), "Lamontix back by 100 s");
            Assert.IsTrue(Standing(J.UltraReconstructor), "the Ultra Reconstructor back by 100 s");
            Assert.IsFalse(Standing(TechnicianPools[0]), "the Technicians come back with the copy");
        }

        /// <summary>
        /// To-fix row 55: the keycard gone (destroyed) after 2 is done, the lockbox's use is refused or gives nothing that
        /// sticks, and the mission is not stuck for good: an abandon and a new accept give a new lockbox and the way back.
        /// </summary>
        [TestMethod]
        public void NotMyJunkWithTheKeycardGoneRecoversByAnAbandon()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            C.SeedCompleted(harness, Amends);
            var crucible = C.EnterCrucible(harness, AwolWaypoint);
            Accept(harness, Npc(crucible, CrucibleAwolCampNpcsV1.Cheung), Junk);
            var copy = IntoJunkyard(harness);
            Tick(harness, copy, 250);
            Talk(harness, Giver(copy, J.Cirkin), Junk, M.SpeakToCirkin);
            Stand(copy, J.Lamontix);
            PickUp(harness, Kill(harness, copy, J.Lamontix));
            harness.Drain();
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Junk, M.Retrieve));
            var card = harness.Client.Player.Inventory.PersonalInventory.Where(id => id != 0).Select(EntityManager.Instance.GetItem)
                .Single(item => item.ItemTemplate.ItemTemplateId == M.KeycardTemplate);
            InventoryManager.Instance.PersonalInventory_DestroyItem(harness.Client,
                new Rasa.Packets.Inventory.Client.PersonalInventory_DestroyItemPacket { EntityId = card.EntityId, Quantity = card.StackSize });
            harness.Drain();
            Assert.AreEqual(0U, Keycards(harness), "the keycard is gone");
            try
            {
                Use(harness, copy, SceneObject(harness, copy, Junk, M.LockboxRole), M.UseArgument);
            }
            catch (GameplayRejectionException)
            {
                // The keycard's ConsumeMissionItem refuses the use (InventoryManager.Missions Consume).
            }
            harness.Drain();
            Assert.AreNotEqual(MissionObjectiveState.Completed, Objective(harness, Junk, M.Retrieve), "no keycard, no converter objective");

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Junk));
            harness.Drain();
            Assert.AreEqual(0U, Converters(harness), "nothing left of the run");
            harness.Manager.Scenes.Detach(harness.Client, copy);
            crucible = C.EnterCrucible(harness, AwolWaypoint);
            Accept(harness, Npc(crucible, CrucibleAwolCampNpcsV1.Cheung), Junk);
            copy = IntoJunkyard(harness);
            Tick(harness, copy, 250);
            Talk(harness, Giver(copy, J.Cirkin), Junk, M.SpeakToCirkin);
            Stand(copy, J.Lamontix);
            PickUp(harness, Kill(harness, copy, J.Lamontix));
            harness.Drain();
            var lockbox = SceneObject(harness, copy, Junk, M.LockboxRole);
            Use(harness, copy, lockbox, M.UseArgument);
            Assert.AreEqual((1U, 0U, MissionObjectiveState.Completed), (Converters(harness), Keycards(harness), Objective(harness, Junk, M.Retrieve)),
                "taken again, the lockbox gives the converter");
        }

        /// <summary>Into the squad's copy of the Junkyard through its door on the Crucible, its pools back as after their time, the scenes resumed.</summary>
        private static MapChannel IntoJunkyard(WildernessRuntimeTestHarness harness)
        {
            if (!harness.Maps.MapChannelArray.ContainsKey(Junkyard))
                LoadMap(harness, Junkyard);
            var copy = ThroughTheDoor(harness, Junkyard);
            foreach (var pool in copy.SpawnPools.Where(pool => JunkyardPools.Contains(pool.DbId)))
                pool.ClearedAtUtcMs = 0;
            harness.Manager.Scenes.Resume(harness.Client);
            harness.Drain();
            return copy;
        }

        /// <summary>The boss of <paramref name="pool"/> standing in the copy: spawned, or back after its own time if an earlier kill left it dead.</summary>
        private static void Stand(MapChannel copy, uint pool)
        {
            Spawn(copy, pool);
            if (!Here(copy).Any(one => one.SpawnPool?.DbId == pool && one.State != CharacterState.Dead))
                Respawn(copy, pool);
            Assert.IsTrue(Here(copy).Any(one => one.SpawnPool?.DbId == pool && one.State != CharacterState.Dead), $"pool {pool} stands");
        }

        /// <summary>The pools standing in the copy (an earlier kill's dead put back), every creature of them killed, the pools' pass after.</summary>
        private static void KillPools(WildernessRuntimeTestHarness harness, MapChannel copy, uint[] pools)
        {
            foreach (var pool in copy.SpawnPools.Where(pool => pools.Contains(pool.DbId)))
            {
                foreach (var straggler in Here(copy).Where(creature => creature.SpawnPool == pool).ToArray())
                    Kill(harness, copy, straggler);
                pool.ClearedAtUtcMs = 0;
            }
            Spawn(copy, pools);
            var standing = Here(copy).Where(creature => pools.Contains(creature.SpawnPool?.DbId ?? 0)).ToArray();
            Assert.HasCount(pools.Length * (int)J.TechniciansPerPool, standing, "the pools stand");
            foreach (var creature in standing)
                Kill(harness, copy, creature);
            SpawnPoolManager.Instance.SpawnPoolWorker(copy, 0);
            harness.Drain();
        }

        /// <summary>The officers' squad on the Crucible (sent again if asked or if none of it lives): Krubb and the others.</summary>
        private static (Creature Krubb, Creature[] Others) Officers(WildernessRuntimeTestHarness harness, MapChannel crucible, bool sendAgain = false)
        {
            S.Init(harness);
            Assert.IsTrue(S.Spawns.ContainsKey(J.Squad), "the officers' squad is in the world database");
            if (sendAgain && S.On(crucible).TryGetValue(J.Squad, out var old))
                old.RespawnAt = 1;
            S.Worker(crucible);
            harness.Drain();
            var pack = S.On(crucible)[J.Squad].Members.Select(member => member.Creature).Where(creature => creature.State != CharacterState.Dead).ToArray();
            var krubb = pack.SingleOrDefault(creature => creature.DbId == J.Krubb);
            Assert.IsNotNull(krubb, "Krubb leads the squad");
            return (krubb, pack.Where(creature => creature != krubb).ToArray());
        }

        /// <summary>The officer killed by the player; the scenes and the squads see it.</summary>
        private static IReadOnlyList<object> KillOfficer(WildernessRuntimeTestHarness harness, MapChannel crucible, Creature officer)
        {
            harness.MoveTo(officer.Position);
            officer.Attributes[Attributes.Health].Current = 0;
            harness.Creatures.HandleCreatureKill(crucible, officer, harness.Client.Player);
            harness.Manager.Scenes.Tick(crucible);
            return harness.Drain().Cast<object>().ToList();
        }

        private static uint Sacks(WildernessRuntimeTestHarness harness) => WildernessRanjaGorgeTests.Held(harness, K.SackTemplate);
        private static uint Blocks(WildernessRuntimeTestHarness harness) => WildernessRanjaGorgeTests.Held(harness, B.DolomiteTemplate);
        private static uint Matrices(WildernessRuntimeTestHarness harness) => WildernessRanjaGorgeTests.Held(harness, B.EnergyMatrixTemplate);
        private static uint Keycards(WildernessRuntimeTestHarness harness) => WildernessRanjaGorgeTests.Held(harness, M.KeycardTemplate);
        private static uint Converters(WildernessRuntimeTestHarness harness) => WildernessRanjaGorgeTests.Held(harness, M.ConverterTemplate);
    }
}
