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
using Rasa.Navigation;
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
    using N = CrucibleChaukasNpcsV1;
    using G = CrucibleGetToChaukasV1;
    using B = CrucibleControlTheBrainV1;
    using X = CrucibleFixTheFactoryV1;

    /// <summary>
    /// Crucible set 12a, played through as a player plays it: Information Analyst Ruben's Get to Chaukas (1577: into the
    /// Chaukas Robotics Facility, squad instance 2146, to find Cipher Specialist Brian), Brian's Control the Brain (1521: Brian
    /// walks with the player to the Brain room, the room cleared, the spare parts found, Brian kept safe from three Clavigers)
    /// and Brian's Fix the Factory (1522: the factory's two switches thrown in turn). With them the facility's guards (as
    /// CrucibleGuardTests holds the Crucible's): Brian, the scene places and the markers on the navmesh and a client floor,
    /// walked to from the arrival and back with no climb the client fails, clear of the door and of the hostile pools, the talk
    /// and use targets seen from the walks' ends; the east room's island (upstream, pinned); the Clavigers' immunities (none).
    ///
    /// Why these tests: Ruben and Brian are new; 1577 goes from the Crucible into a squad instance; 1521 has the first Crucible
    /// escort (a copy of the receiver that follows, then walks a route), a pool-cleared set, a hidden opener, a use object and
    /// a defence counted by scene defeats; 1522 two use objects in turn. A target off the navmesh or the walks, an offer before
    /// its prerequisite, a use before its objective is open, two of three counted as three, a follow or a count lost at a
    /// reload or kept after an abandon, the public Brian shown beside his copy, or a turn-in that pays twice would each break
    /// them.
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class CrucibleChaukasTests
    {
        /// <summary>The Crucible markers in Chaukas: 1577's on Brian; 1521's mouth, three pools, the container, Brian three times; 1522's two switches.</summary>
        internal const int ChaukasMarkers = 1 + (1 + 3 + 1 + 3) + 2;

        private const uint Chaukas = N.Chaukas;
        private const uint GetTo = G.MissionId, Brain = B.MissionId, Factory = X.MissionId;
        private const uint ArmorFiller = 12748;            // Armor_T3_Bio_V01_CMN_Gloves_30_to_34 (inventory category 1)
        private const uint WeaponFiller = 3869;            // inventory category 1 (quest-test-pattern)
        private const uint ModuleFiller = 122983;          // Armor Module: Regen Bonus [1] (inventory category 3)

        /// <summary>A point inside the east (Robotics Assembly) room, on a navmesh island apart from the arrival's (probe 2026-11-07, upstream).</summary>
        internal static readonly Vector3 EastRoomInside = new(83.5f, 16.04f, -94.1f);

        private static readonly Vector3 Arrival = ToVector(N.ArrivalAt);
        private static readonly uint[] ClavigerPools = Enumerable.Range(531119, 15).Select(id => (uint)id).ToArray();
        private static readonly uint[] BrainRoomPools = N.BrainRoomPools.Select(pool => pool.Pool).ToArray();

        [TestInitialize]
        public void Clock() => PlateauGiantKillerSquadTests.UseClock();

        [TestCleanup]
        public void Restore() => PlateauGiantKillerSquadTests.RestoreClock();

        /// <summary>
        /// In the squad's copy: Brian stands with the client's name and package, friendly, at his place, on the navmesh, at his
        /// post (posed: he does not stroll) yet able to walk (the escort), ReachClearance or more outside every Claviger pool's
        /// circle (and so are Brian at the Brain and the use targets), 5 m or more outside the door's; the scene's places (the Brain room's mouth, Brian's place at the Brain, the
        /// container, the two switches, the three attackers) on the navmesh and walked to from the arrival; the attackers 20 m or
        /// more from Brian at the Brain and clear of the door; Brian's route from the mouth to the Brain complete; the east room's
        /// inside not joined to the arrival (upstream, pinned) while the factory switch at its entrance is; the Clavigers immune
        /// to nothing (class 6961 has no IMMUNE_ flag); Ruben on the Crucible with his name and package, friendly, standing still.
        /// </summary>
        [TestMethod]
        public void TheFacilitysNpcsAndPlacesStandWhereTheyAreWalkedTo()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var crucible = C.EnterCrucible(harness, ToVector(N.RubenAt));
            var ruben = Npc(crucible, N.Ruben);
            Assert.AreEqual((N.RubenName, N.RubenPackage), (ruben.NameId, ruben.Npc?.NpcPackageId), "Ruben");
            Assert.AreNotEqual(TargetCategory.Hostile, ruben.TargetCategory, "Ruben friendly");
            Assert.IsTrue(BehaviorManager.NeverMoves(ruben), "Ruben stands still");

            var copy = IntoChaukas(harness);
            Assert.IsTrue(copy.IsSquadInstance);
            var nav = copy.NavMesh;
            var links = harness.World.Set<MapLinkEntry>().AsNoTracking().Where(link => link.Enabled != 0 && link.MapContextId == Chaukas).ToList();
            Assert.IsNotEmpty(links, "Chaukas's door");
            float LinkGap(Vector3 at, float radius = 0) => links.Min(link =>
                Vector2.Distance(new(at.X, at.Z), new((float)link.PosX, (float)link.PosZ)) - (float)link.Radius - radius);
            var hostile = harness.World.SpawnPoolEntries.AsNoTracking().Where(pool => pool.MapContextId == Chaukas && ClavigerPools.Contains(pool.Id)).ToList();
            Assert.HasCount(ClavigerPools.Length, hostile, "the Claviger pools");
            void Clear(Vector3 at, string what)
            {
                foreach (var other in hostile)
                    Assert.IsGreaterThanOrEqualTo(MarshesNpcsStandStillV1.ReachClearance,
                        Vector2.Distance(new(at.X, at.Z), new((float)other.PosX, (float)other.PosZ)) - (float)other.Radius,
                        $"{what} outside hostile pool {other.Id}'s reach");
            }

            var brian = Giver(copy, N.Brian);
            Assert.AreEqual((N.BrianName, N.BrianPackage), (brian.NameId, brian.Npc?.NpcPackageId), "Brian");
            Assert.AreNotEqual(TargetCategory.Hostile, brian.TargetCategory, "Brian friendly");
            Assert.AreEqual(ToVector(N.BrianAt).X, brian.Position.X, 0.01f, "Brian where set 12a put him");
            Assert.AreEqual(ToVector(N.BrianAt).Z, brian.Position.Z, 0.01f, "Brian where set 12a put him");
            Assert.IsTrue(NpcPoses.HoldsPost(brian), "the public Brian keeps his post (pose Standing)");
            Assert.IsFalse(BehaviorManager.NeverMoves(brian), "Brian can walk (1521's copy follows)");
            AssertOnGround(copy, brian.Position, "Brian");
            WalkFromArrival(nav, brian.Position, "Brian");
            Assert.IsGreaterThanOrEqualTo(5f, LinkGap(brian.Position), "Brian clear of the door");
            Clear(brian.Position, "Brian");
            Clear(ToVector(B.BrainAt), "Brian at the Brain");
            foreach (var (label, at) in UseTargets())
                Clear(at, label);

            foreach (var (label, at) in Places())
            {
                AssertOnGround(copy, at, label);
                WalkFromArrival(nav, at, label);
                Assert.IsGreaterThanOrEqualTo(5f, LinkGap(at), $"{label} clear of the door");
            }
            foreach (var at in B.AttackersAt.Select(ToVector))
            {
                Assert.IsGreaterThanOrEqualTo(20f, Vector3.Distance(at, ToVector(B.BrainAt)), $"attacker at {at} 20 m or more from Brian");
                Assert.IsGreaterThanOrEqualTo(MarshesNpcsStandStillV1.ReachClearance + 10, LinkGap(at), $"attacker at {at} clear of the door");
            }
            AssertPath(copy, nav.Nearest(ToVector(B.MouthAt)).Value, nav.Nearest(ToVector(B.BrainAt)).Value, "Brian's route to the Brain");

            // NOTE: a local rebake of the Chaukas navmesh for set 12b (as N1-N3) joins the east room and breaks this pin: then make
            // it Assert.Inconclusive (the island is upstream's shipped file only), not a failure.
            Assert.IsFalse(nav.FindPath(nav.Nearest(Arrival).Value, nav.Nearest(EastRoomInside).Value, out var east) != null && east,
                "the east room's inside is not joined to the arrival (upstream, NOTES-upstream-bugs.md): if it is now, 1523 can play inside it");

            Spawn(copy, BrainRoomPools[0]);
            var claviger = Here(copy).First(one => one.SpawnPool?.DbId == BrainRoomPools[0]);
            Assert.AreEqual((N.Claviger, 6961U, TargetCategory.Hostile), (claviger.DbId, (uint)claviger.EntityClass, claviger.TargetCategory));
            foreach (var type in CreatureImmunities.Flags.Keys)
                Assert.IsFalse(CreatureImmunities.IsImmune(claviger, type), $"a Claviger is not immune to {type}");
        }

        /// <summary>
        /// Every place the spawn draw of a radius Claviger pool (531119-531133) can set a creature down (CrucibleGuardTests.SpawnReach,
        /// Detour's draw over the polygons the circle touches, upstream C11: up to 57 m off for 531129) is ReachClearance or more
        /// from Brian at the entrance, Brian at the Brain and each use target (the container, the two switches): a Claviger set
        /// down by one would pull it into a fight, or stand on a target the player must use (as set 11a's
        /// TheIncurablePoolsSetTheirCreaturesDownInTheirRoom).
        /// </summary>
        [TestMethod]
        public void NoClavigerPoolSetsACreatureDownNearBrianOrAUseTarget()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            C.EnterCrucible(harness);
            var copy = IntoChaukas(harness);
            var nav = copy.NavMesh;
            var pools = harness.World.SpawnPoolEntries.AsNoTracking().Where(pool => pool.MapContextId == Chaukas && ClavigerPools.Contains(pool.Id)).ToList();
            Assert.HasCount(ClavigerPools.Length, pools, "the Claviger pools");
            Assert.IsTrue(pools.All(pool => pool.Radius > 0), "all fifteen are radius pools (upstream)");
            var targets = new List<(string Label, Vector3 At)> { ("Brian", ToVector(N.BrianAt)), ("Brian at the Brain", ToVector(B.BrainAt)) };
            targets.AddRange(UseTargets());
            var failures = new List<string>();
            foreach (var pool in pools)
            {
                var centre = nav.Nearest(new Vector3((float)pool.PosX, (float)pool.PosY, (float)pool.PosZ)).Value;
                var reach = CrucibleGuardTests.SpawnReach(nav, centre, (float)pool.Radius);
                foreach (var (label, at) in targets)
                {
                    var near = reach.Min(point => Vector2.Distance(new(point.X, point.Z), new(at.X, at.Z)));
                    if (near < MarshesNpcsStandStillV1.ReachClearance)
                        failures.Add(FormattableString.Invariant($"pool {pool.Id} (r{pool.Radius}) reaches {near:0.0} m from {label}"));
                }
            }
            Assert.IsEmpty(failures, string.Join("; ", failures));
        }

        /// <summary>
        /// The client's floors (MarshesClientFloorTests; inconclusive without the client's files): Brian, the scene's places and
        /// the 12a markers in Chaukas on a client floor with none just over them; Brian (both places), the container and the
        /// switches seen from 16 of 16 places 2 m round each (Brian at the entrance 15: a wall 2.2 m off) and from each point of
        /// the walk from the arrival 5 to 2 m from them; every walk from the arrival, and back, climbs within MaxClientStep and
        /// MaxClientGrade, and so does the walk from Brian at the Brain to the container and back; no lava.
        /// </summary>
        [TestMethod]
        public void TheFacilitysPlacesAndMarkersStandOnAClientFloorAndAreWalkedAndSeen()
        {
            const float MarkerHeight = 2;
            var dataDir = F.ClientData();
            using var harness = WildernessRuntimeTestHarness.Create();
            C.EnterCrucible(harness);
            var copy = IntoChaukas(harness);
            var nav = copy.NavMesh;
            using var meshes = new MeshLibrary(dataDir, F.MeshCsv());
            var name = harness.World.MapInfoEntries.AsNoTracking().Single(row => row.Id == Chaukas).MapName;
            var geometry = MapGeometry.Load(Path.Combine(dataDir, "maps", name), meshes, 1);

            var talks = new List<(string Label, Vector3 At, bool Npc, int Seen)>
            {
                ("Brian", ToVector(N.BrianAt), true, 15), ("Brian at the Brain", ToVector(B.BrainAt), true, F.RingPlaces),
                ("the container", ToVector(B.ContainerAt), false, F.RingPlaces), ("the factory switch", ToVector(X.FactorySwitchAt), false, F.RingPlaces),
                ("the ore switch", ToVector(X.OreSwitchAt), false, F.RingPlaces)
            };
            var places = talks.Select(talk => (talk.Label, talk.At, talk.Npc)).ToList();
            places.AddRange(Places().Where(place => talks.All(talk => talk.At != place.At)).Select(place => (place.Label, place.At, true)));
            var (floors, ceilings) = F.Surfaces(geometry, places.Select(place => (place.At, place.Npc ? F.Walkable : F.NotVertical)).ToList());
            var failures = new List<string>();
            for (var i = 0; i < places.Count; i++)
                if (places[i].At.Y - floors[i] > F.MaxOver || !float.IsPositiveInfinity(ceilings[i]))
                    failures.Add($"{places[i].Label} at {places[i].At}: floor {floors[i]}, ceiling {ceilings[i]}");

            // The markers in Chaukas (MissionMarkerProjection.ResolveMap 2146).
            var catalog = new MissionContentCatalog(harness, new Dictionary<uint, Mission>(), null);
            catalog.Load();
            var markers = harness.World.MissionIndicatorEntries.AsNoTracking().Where(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision).ToList()
                .Where(marker => MissionMarkerProjection.ResolveMap(catalog, marker.MissionId, marker.ObjectiveId, new MissionIndicator { IndicatorId = marker.IndicatorId }) == Chaukas)
                .Select(marker => (Label: $"{marker.MissionId}.{marker.ObjectiveId}/{marker.IndicatorId}", At: new Vector3((float)marker.PosX, (float)marker.PosY, (float)marker.PosZ)))
                .ToList();
            Assert.HasCount(ChaukasMarkers, markers, "Chaukas's markers");
            var (markerFloors, _) = F.Surfaces(geometry, markers.Select(marker => (marker.At + new Vector3(0, MarkerHeight - F.MaxUnder, 0), F.Walkable)).ToList());
            for (var i = 0; i < markers.Count; i++)
            {
                var ground = nav.GroundHeight(markers[i].At + new Vector3(0, MarkerHeight, 0));
                if (ground is not { } y || MathF.Abs(y - markers[i].At.Y) > MarkerHeight)
                    failures.Add($"{markers[i].Label}: navmesh {ground}");
                if (markers[i].At.Y - markerFloors[i] > MarkerHeight)
                    failures.Add($"{markers[i].Label}: client floor {markerFloors[i]}");
            }

            void Climb(string what, List<Vector3> walk)
            {
                var (step, stepAt, grade, gradeAt) = CrucibleGuardTests.ClientClimb(nav, geometry, walk);
                if (step > CrucibleGuardTests.MaxClientStep)
                    failures.Add(FormattableString.Invariant($"{what}: a {step:0.00} m step at ({stepAt.X:0.0}, {stepAt.Y:0.0}, {stepAt.Z:0.0})"));
                if (grade > CrucibleGuardTests.MaxClientGrade)
                    failures.Add(FormattableString.Invariant($"{what}: {grade:0.00} a metre over 12 m from ({gradeAt.X:0.0}, {gradeAt.Y:0.0}, {gradeAt.Z:0.0})"));
            }
            List<Vector3> Walk(Vector3 from, Vector3 to, string what)
            {
                var start = nav.Nearest(from) ?? from;
                var path = nav.FindPath(start, nav.Nearest(to) ?? to, out var complete);
                Assert.IsTrue(path != null && complete, $"{what}: a walk");
                return path.Prepend(start).ToList();
            }

            // Each walk from the arrival, and back, is one the client climbs; each talk and use target is seen round it and
            // from its walk's end.
            foreach (var (label, at, npc) in places)
            {
                var walk = WalkFromArrival(nav, at, label);
                Climb($"the arrival to {label}", walk);
                Climb($"{label} to the arrival", Walk(at, Arrival, $"{label} to the arrival"));
                var talk = talks.FirstOrDefault(entry => entry.Label == label);
                if (talk.Label == null)
                    continue;
                var near = F.Near(geometry, new List<Vector3> { at }, 8)[0];
                var aim = at + new Vector3(0, npc ? F.NpcMiddle : F.ObjectMiddle, 0);
                var seen = 0;
                for (var k = 0; k < F.RingPlaces; k++)
                {
                    var angle = k * 2 * MathF.PI / F.RingPlaces;
                    var stand = at + new Vector3(MathF.Sin(angle) * F.Ring, 0, MathF.Cos(angle) * F.Ring);
                    if (F.FloorAt(near, stand, at.Y - F.MaxOver, at.Y + F.MaxUnder) is { } fy && F.Clear(near, stand with { Y = fy + F.Eye }, aim))
                        seen++;
                }
                if (seen < talk.Seen)
                    failures.Add($"{label} seen from {seen} of {F.RingPlaces} places round it (want {talk.Seen})");
                var ends = 0;
                foreach (var distance in new[] { 5f, 4.5f, 4f, 3.5f, 3f, 2.5f, 2f })
                {
                    Vector3? hit = null;
                    for (var i = 0; i + 1 < walk.Count && hit == null; i++)
                        for (var s = 0; s <= 50 && hit == null; s++)
                        {
                            var point = Vector3.Lerp(walk[i], walk[i + 1], s / 50f);
                            if (Vector3.Distance(point, at) <= distance)
                                hit = point;
                        }
                    if (hit is not { } end)
                        continue;
                    ends++;
                    var floor = F.FloorAt(near, end, end.Y - 1.5f, end.Y + 1f) ?? end.Y;
                    if (!F.Clear(near, end with { Y = floor + F.Eye }, aim))
                        failures.Add($"{label} not seen from the walk's point ({end.X:F1}, {end.Z:F1}) {distance} m off");
                }
                Assert.AreEqual(7, ends, $"the walk's points 5 to 2 m from {label}");
            }
            // 1521: from Brian at the Brain down to the container on the lower level, and back up.
            Climb("Brian at the Brain to the container", Walk(ToVector(B.BrainAt), ToVector(B.ContainerAt), "the Brain to the container"));
            Climb("the container to Brian at the Brain", Walk(ToVector(B.ContainerAt), ToVector(B.BrainAt), "the container to the Brain"));

            // No lava in Chaukas: 20 m round every place. NOTE: LavaSurfaces may have no entry for this map, so the check can be
            // vacuous (the probe saw no lava model here); it holds the place should the map gain lava data.
            foreach (var (label, at, _) in places)
                for (var dx = -20f; dx <= 20; dx += 2)
                    for (var dz = -20f; dz <= 20; dz += 2)
                        if (dx * dx + dz * dz <= 400 && nav.Nearest(at + new Vector3(dx, 0, dz)) is { } ground && LavaDamage.InLava(name, ground))
                            failures.Add(FormattableString.Invariant($"lava at ({ground.X:0.0}, {ground.Y:0.0}, {ground.Z:0.0}) by {label}"));
            Assert.IsEmpty(failures, string.Join("; ", failures));
        }

        /// <summary>
        /// On the Crucible: Ruben is walked to from the Outpost Intrepid waypoint and the marker before the Chaukas door from the
        /// Awol Camp waypoint, with no climb the client fails; the door marker's circle 5 m or more outside the door's; Ruben
        /// seen from 16 of 16 places 2 m round him (his floor, sight and hostile clearance on the Crucible are CrucibleGuardTests').
        /// </summary>
        [TestMethod]
        public void RubenAndTheChaukasDoorMarkerAreWalkedToOnTheCrucible()
        {
            var dataDir = F.ClientData();
            using var harness = WildernessRuntimeTestHarness.Create();
            var crucible = C.EnterCrucible(harness, ToVector(N.RubenAt));
            var nav = crucible.NavMesh;
            using var meshes = new MeshLibrary(dataDir, F.MeshCsv());
            var geometry = MapGeometry.Load(Path.Combine(dataDir, "maps", crucible.MapInfo.MapName), meshes, 1);
            var ruben = Npc(crucible, N.Ruben);
            Assert.AreEqual(ToVector(N.RubenAt).X, ruben.Position.X, 0.01f);
            Assert.AreEqual(ToVector(N.RubenAt).Z, ruben.Position.Z, 0.01f);
            AssertOnGround(crucible, ruben.Position, "Ruben");
            var failures = new List<string>();
            foreach (var (label, from, at) in new[]
            {
                ("Ruben", C.OutpostWaypoint, ruben.Position), ("the Chaukas door marker", CrucibleGuardTests.AwolCampWaypoint, ToVector(N.DoorWayAt))
            })
            {
                var start = nav.Nearest(from) ?? from;
                var end = nav.Nearest(at) ?? at;
                var walk = nav.FindPath(start, end, out var complete);
                Assert.IsTrue(walk != null && complete, $"{label}: a walk from the waypoint");
                var back = nav.FindPath(end, start, out var backComplete);
                Assert.IsTrue(back != null && backComplete, $"{label}: a walk back to the waypoint");
                foreach (var (way, points) in new[] { ("there", walk.Prepend(start).ToList()), ("back", back.Prepend(end).ToList()) })
                {
                    var (step, _, grade, _) = CrucibleGuardTests.ClientClimb(nav, geometry, points);
                    if (step > CrucibleGuardTests.MaxClientStep || grade > CrucibleGuardTests.MaxClientGrade)
                        failures.Add(FormattableString.Invariant($"{label} {way}: step {step:0.00}, grade {grade:0.00}"));
                }
            }
            var door = harness.World.Set<MapLinkEntry>().AsNoTracking().Single(link => link.MapContextId == CrucibleMissionDataV1.MapContextId &&
                link.DestMapContextId == Chaukas && link.Enabled != 0);
            Assert.IsGreaterThanOrEqualTo(5f, Vector2.Distance(new((float)door.PosX, (float)door.PosZ), new((float)N.DoorWayAt.X, (float)N.DoorWayAt.Z)) -
                (float)door.Radius - (float)G.DoorMarkerRadius, "the door marker clear of the door");
            var near = F.Near(geometry, new List<Vector3> { ruben.Position }, 8)[0];
            var aim = ruben.Position + new Vector3(0, F.NpcMiddle, 0);
            for (var k = 0; k < F.RingPlaces; k++)
            {
                var angle = k * 2 * MathF.PI / F.RingPlaces;
                var stand = ruben.Position + new Vector3(MathF.Sin(angle) * F.Ring, 0, MathF.Cos(angle) * F.Ring);
                if (F.FloorAt(near, stand, ruben.Position.Y - F.MaxOver, ruben.Position.Y + F.MaxUnder) is not { } fy ||
                    !F.Clear(near, stand with { Y = fy + F.Eye }, aim))
                    failures.Add($"Ruben not seen from ({stand.X:F1}, {stand.Z:F1})");
            }
            Assert.IsEmpty(failures, string.Join("; ", failures));
        }

        /// <summary>
        /// Get to Chaukas: only Ruben offers it, on the Crucible (no prerequisite); Brian offers nothing before it is done; Ruben
        /// hears no objective; Brian hears 1 only in the squad's copy (only that), which completes it; a reload keeps it; a full
        /// bag waits, a failed save pays nothing; Brian pays one of the four items, once; Control the Brain follows.
        /// </summary>
        [TestMethod]
        public void GetToChaukasFindsBrianInTheFacility()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var crucible = C.EnterCrucible(harness, ToVector(N.RubenAt));
            var ruben = Npc(crucible, N.Ruben);
            var offer = WildernessRanjaGorgeTests.Offer(harness, ruben, GetTo);
            CollectionAssert.AreEqual(new[] { G.MechVest, G.GravitonVest, G.HazmatVest, G.MotorAssistHelmet },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            var copy = IntoChaukas(harness);
            var brian = Giver(copy, N.Brian);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, brian, GetTo), "only Ruben offers it");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, brian, Brain), "Control the Brain waits for it");

            harness.Manager.Scenes.Detach(harness.Client, copy);
            crucible = C.EnterCrucible(harness, ToVector(N.RubenAt));
            ruben = Npc(crucible, N.Ruben);
            Accept(harness, ruben, GetTo);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, GetTo, G.GetThere));
            Assert.IsEmpty(Topics(harness, ruben, GetTo), "Ruben hears no objective");

            copy = IntoChaukas(harness);
            brian = Giver(copy, N.Brian);
            WildernessRanjaGorgeTests.Reload(harness);
            harness.Drain();
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, GetTo, G.GetThere), "kept at a reload");
            Talk(harness, brian, GetTo, G.GetThere);
            Assert.IsTrue(harness.Client.Player.Missions[GetTo].Completeable);
            Assert.IsEmpty(Topics(harness, brian, GetTo), "heard once");

            FullBagWaits(harness, brian, GetTo, 2, ArmorFiller);
            FailedTurnInPaysNothing(harness, brian, GetTo, 2);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, brian, GetTo, 2, G.Experience, G.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == GetTo), "MissionRewarded once");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, G.HazmatVest), "the vest chosen");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, brian, Brain), "Control the Brain follows");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, brian, Factory), "Fix the Factory waits for Control the Brain");
            harness.Manager.Scenes.Detach(harness.Client, copy);
            crucible = C.EnterCrucible(harness, ToVector(N.RubenAt));
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, Npc(crucible, N.Ruben), GetTo), "done once");
        }

        /// <summary>
        /// Control the Brain after Get to Chaukas: Brian offers it in the copy; at the accept his copy (the player's own, the public
        /// Brian hidden from the player) stands at his place, and follows 3 s later, not before; the container takes no use
        /// before 4 is open; the walk to the Brain room's mouth (Brian with the player) completes 3; two of the room's three
        /// pools dead leave 8 open, a reload in the middle keeps 3 and the follow; the third pool opens 7 (9 and 10 hidden) and
        /// Brian walks to the Brain; his talk opens 4; the container gives once and opens 5; his talk brings the three Clavigers
        /// (20 m or more off, hostile, at him); two dead leave 6 open (2 of 3, a reload kept), the third makes it completeable;
        /// a full bag waits, a failed save pays nothing; Brian's copy pays once; then the copy goes, the public Brian is shown
        /// again, and Fix the Factory follows.
        /// </summary>
        [TestMethod]
        public void ControlTheBrainTakesBrianToTheBrainAndKeepsHimSafe()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            C.SeedCompleted(harness, GetTo);
            C.EnterCrucible(harness);
            var copy = IntoChaukas(harness);
            var brian = Giver(copy, N.Brian);
            var offer = WildernessRanjaGorgeTests.Offer(harness, brian, Brain);
            CollectionAssert.AreEqual(new[] { B.FilamentBlade, B.ExothermicPolarityGun, B.PulseRpgLauncher, B.Shotgun },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Accept(harness, brian, Brain);
            Tick(harness, copy, 250);
            Assert.AreEqual((MissionObjectiveState.Incomplete, MissionObjectiveState.Incomplete, MissionObjectiveState.Inactive, MissionObjectiveState.Inactive),
                (Objective(harness, Brain, B.Escort), Objective(harness, Brain, B.ClearRoom), Objective(harness, Brain, B.TalkToBrian),
                    Objective(harness, Brain, B.FindParts)), "3 and 8 open at the accept");
            var visible = harness.Manager.BuildStatusSnapshot(harness.Client.Player)[Brain].ObjectivesList.Select(objective => objective.ObjectiveId).ToArray();
            Assert.IsFalse(visible.Contains(B.Tracker) || visible.Contains(B.Opened) || visible.Contains(B.AttackersTally), "the tracker and the tally are hidden");

            var follower = SceneCreature(harness, copy, Brain, B.BrianRole);
            Assert.IsNotNull(follower, "Brian's copy");
            Assert.AreEqual((N.Brian, N.BrianPackage), (follower.DbId, follower.Npc?.NpcPackageId), "a copy of Brian's row");
            Assert.AreEqual(ToVector(N.BrianAt).X, follower.Position.X, 0.5f, "at Brian's place");
            Assert.AreEqual(ToVector(N.BrianAt).Z, follower.Position.Z, 0.5f, "at Brian's place");
            Assert.IsTrue(MissionNpcVisibility.IsShownTo(harness.Client, follower), "the copy is shown to the player");
            Assert.IsFalse(MissionNpcVisibility.IsShownTo(harness.Client, brian), "the public Brian is hidden while the copy lives");
            Assert.AreEqual(0U, follower.SpawnPool.FollowOwnerCharacterId, "not before the timer");

            // The container takes no use before 4 is open.
            var container = SceneObject(harness, copy, Brain, B.ContainerRole);
            Assert.AreEqual((B.ContainerClass, B.ContainerName), ((uint)container.EntityClassId, container.NameOverrideId));
            Use(harness, copy, container, B.UseArgument);
            Assert.AreEqual(MissionObjectiveState.Inactive, Objective(harness, Brain, B.FindParts), "no use before 4");
            Assert.IsTrue(container.IsEnabled, "the container stays closed");

            harness.MoveTo(ToVector(N.BrianAt) + new Vector3(2, 0, 0));
            harness.Drain();
            for (var second = 0; second < 4; second++)
                Tick(harness, copy, 1000);
            follower = SceneCreature(harness, copy, Brain, B.BrianRole);
            Assert.AreEqual(harness.Client.Player.Id, follower.SpawnPool.FollowOwnerCharacterId, "Brian follows after the timer");

            // 3: the walk to the Brain room's mouth, Brian with the player.
            var nav = copy.NavMesh;
            var start = nav.Nearest(ToVector(N.BrianAt)).Value;
            var mouth = nav.Nearest(ToVector(B.MouthAt)).Value;
            var path = nav.FindPath(start, mouth, out var whole);
            Assert.IsTrue(path != null && whole, "a walk to the Brain room");
            var walk = path.Prepend(start).ToList();
            for (var i = 1; i < walk.Count; i++)
            {
                var steps = Math.Max(1, (int)MathF.Ceiling(Vector3.Distance(walk[i - 1], walk[i]) / 4));
                for (var s = 1; s <= steps; s++)
                {
                    harness.MoveTo(Vector3.Lerp(walk[i - 1], walk[i], s / (float)steps));
                    Tick(harness, copy, 1000);
                }
            }
            for (var second = 0; second < 5; second++)
                Tick(harness, copy, 1000);
            follower = SceneCreature(harness, copy, Brain, B.BrianRole);
            Assert.IsLessThan(12f, Vector3.Distance(follower.Position, harness.Client.Player.Position), "Brian came along");
            Enter(harness, ToVector(B.MouthAt));
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Brain, B.Escort), "at the Brain room");
            Assert.AreEqual(MissionObjectiveState.Inactive, Objective(harness, Brain, B.TalkToBrian), "the room still to clear");

            // 8: two of three pools are not the room cleared.
            KillPools(harness, copy, BrainRoomPools.Take(2).ToArray());
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Brain, B.ClearRoom), "two pools of three");
            ReloadAndRecover(harness, copy);
            Assert.AreEqual((MissionObjectiveState.Completed, MissionObjectiveState.Incomplete),
                (Objective(harness, Brain, B.Escort), Objective(harness, Brain, B.ClearRoom)), "kept at a reload in the middle of the scene");
            follower = SceneCreature(harness, copy, Brain, B.BrianRole);
            Assert.IsNotNull(follower, "Brian's copy after the reload");
            Assert.AreEqual(harness.Client.Player.Id, follower.SpawnPool.FollowOwnerCharacterId, "and he follows still");
            KillPools(harness, copy, BrainRoomPools.Skip(2).ToArray());
            Tick(harness, copy, 250);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Brain, B.ClearRoom), "the room cleared");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Brain, B.TalkToBrian), "3 and 8 open 7");

            // Brian walks to the Brain.
            for (var second = 0; second < 40 && Vector3.Distance(SceneCreature(harness, copy, Brain, B.BrianRole).Position, ToVector(B.BrainAt)) > 1.5f; second++)
                Tick(harness, copy, 1000);
            follower = SceneCreature(harness, copy, Brain, B.BrianRole);
            Assert.IsLessThanOrEqualTo(1.5f, Vector3.Distance(follower.Position, ToVector(B.BrainAt)), "Brian at the Brain");
            Assert.AreEqual(0U, follower.SpawnPool.FollowOwnerCharacterId, "he follows no longer");

            // 7, 4, 5.
            Talk(harness, follower, Brain, B.TalkToBrian);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Brain, B.FindParts));
            container = SceneObject(harness, copy, Brain, B.ContainerRole);
            Use(harness, copy, container, B.UseArgument);
            Use(harness, copy, container, B.UseArgument);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Brain, B.FindParts), "the parts found");
            Assert.IsFalse(container.IsEnabled, "the container opened, once");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Brain, B.TakeParts));
            Assert.IsNull(SceneCreature(harness, copy, Brain, B.AttackerRole(0)), "no Claviger before the parts are at Brian");
            follower = SceneCreature(harness, copy, Brain, B.BrianRole);
            Talk(harness, follower, Brain, B.TakeParts);
            Tick(harness, copy, 250);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Brain, B.KeepSafe));

            // 6: the three Clavigers at Brian.
            var attackers = Enumerable.Range(0, B.AttackersAt.Length).Select(index => SceneCreature(harness, copy, Brain, B.AttackerRole(index))).ToArray();
            Assert.IsTrue(attackers.All(attacker => attacker != null), "the three Clavigers");
            foreach (var attacker in attackers)
            {
                Assert.AreEqual((N.Claviger, TargetCategory.Hostile), (attacker.DbId, attacker.TargetCategory));
                Assert.IsGreaterThanOrEqualTo(20f, Vector3.Distance(attacker.Position, ToVector(B.BrainAt)), "20 m or more from Brian");
            }
            Kill(harness, copy, attackers[0]);
            Kill(harness, copy, attackers[1]);
            Assert.AreEqual((2U, MissionObjectiveState.Incomplete), (Counter(harness, Brain, B.AttackersTally), Objective(harness, Brain, B.KeepSafe)), "2 of 3");
            ReloadAndRecover(harness, copy);
            Assert.AreEqual((2U, MissionObjectiveState.Incomplete), (Counter(harness, Brain, B.AttackersTally), Objective(harness, Brain, B.KeepSafe)), "kept at a reload");
            var last = SceneCreature(harness, copy, Brain, B.AttackerRole(2));
            Assert.IsNotNull(last, "the third Claviger is still there");
            Kill(harness, copy, last);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Brain, B.KeepSafe), "the third");
            Assert.IsTrue(harness.Client.Player.Missions[Brain].Completeable);

            follower = SceneCreature(harness, copy, Brain, B.BrianRole);
            FullBagWaits(harness, follower, Brain, 1, WeaponFiller);
            FailedTurnInPaysNothing(harness, follower, Brain, 1);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, follower, Brain, 1, B.Experience, B.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Brain), "MissionRewarded once");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, B.ExothermicPolarityGun), "the gun chosen");
            for (var tick = 0; tick < 4; tick++)        // RetireRewardedScene retries: up to 1 s
                Tick(harness, copy, 250);
            Assert.IsNull(SceneCreature(harness, copy, Brain, B.BrianRole), "the copy goes with the mission");
            Assert.IsNull(FindSceneObject(harness, copy, Brain, B.ContainerRole), "the container goes with the mission");
            brian = Giver(copy, N.Brian);
            Assert.IsTrue(MissionNpcVisibility.IsShownTo(harness.Client, brian), "the public Brian is shown again");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, brian, Brain), "done once");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, brian, Factory), "Fix the Factory follows");
        }

        /// <summary>The Brain room cleared before the accept counts: 8 complete at the accept (ExistingFactObjectiveIds), 3 still to walk.</summary>
        [TestMethod]
        public void ControlTheBrainCountsTheRoomClearedBeforeTheAccept()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            C.SeedCompleted(harness, GetTo);
            C.EnterCrucible(harness);
            var copy = IntoChaukas(harness);
            KillPools(harness, copy, BrainRoomPools);
            var brian = Giver(copy, N.Brian);
            Accept(harness, brian, Brain);
            Tick(harness, copy, 250);
            Assert.AreEqual((MissionObjectiveState.Incomplete, MissionObjectiveState.Completed),
                (Objective(harness, Brain, B.Escort), Objective(harness, Brain, B.ClearRoom)), "the room was cleared before");
            Enter(harness, ToVector(B.MouthAt));
            Tick(harness, copy, 250);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Brain, B.TalkToBrian), "3 opens 7 now");
        }

        /// <summary>
        /// An abandon of Control the Brain after the walk to the room: Brian's copy goes and the public Brian is shown again;
        /// taken again, the copy stands at his place again and 3 is open again.
        /// </summary>
        [TestMethod]
        public void ControlTheBrainAbandonedStartsOver()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            C.SeedCompleted(harness, GetTo);
            C.EnterCrucible(harness);
            var copy = IntoChaukas(harness);
            var brian = Giver(copy, N.Brian);
            Accept(harness, brian, Brain);
            for (var second = 0; second < 4; second++)
                Tick(harness, copy, 1000);
            Enter(harness, ToVector(B.MouthAt));
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Brain, B.Escort));

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Brain));
            harness.Drain();
            for (var tick = 0; tick < 4; tick++)
                Tick(harness, copy, 250);
            Assert.IsNull(SceneCreature(harness, copy, Brain, B.BrianRole), "the copy goes");
            Assert.IsTrue(MissionNpcVisibility.IsShownTo(harness.Client, brian), "the public Brian is shown again");
            Accept(harness, brian, Brain);
            Tick(harness, copy, 250);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Brain, B.Escort), "3 open again");
            var follower = SceneCreature(harness, copy, Brain, B.BrianRole);
            Assert.IsNotNull(follower, "the copy again");
            Assert.AreEqual(ToVector(N.BrianAt).X, follower.Position.X, 0.5f, "at Brian's place again");
            Assert.AreEqual(ToVector(N.BrianAt).Z, follower.Position.Z, 0.5f, "at Brian's place again");
        }

        /// <summary>
        /// Fix the Factory after Control the Brain: Brian offers it; the two switches are the player's own and only in the copy;
        /// the ore switch takes no use before 1 is done; the factory switch, used twice, is thrown once and opens 2; a reload
        /// keeps 1 and the thrown switch; the ore switch makes it completeable; a full bag waits, a failed save pays nothing;
        /// Brian pays one of the four modules, once.
        /// </summary>
        [TestMethod]
        public void FixTheFactoryThrowsBothSwitchesInTurn()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            C.SeedCompleted(harness, GetTo);
            var crucible = C.EnterCrucible(harness);
            var copy = IntoChaukas(harness);
            var brian = Giver(copy, N.Brian);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, brian, Factory), "it waits for Control the Brain");
            C.SeedCompleted(harness, Brain);
            copy = IntoChaukas(harness);
            brian = Giver(copy, N.Brian);
            var offer = WildernessRanjaGorgeTests.Offer(harness, brian, Factory);
            CollectionAssert.AreEqual(new[] { X.ArmorAbsorption, X.RegenBonus, X.HealthRegenBonus, X.PowerRegenBonus },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Accept(harness, brian, Factory);
            Tick(harness, copy, 250);
            Assert.AreEqual((MissionObjectiveState.Incomplete, MissionObjectiveState.Inactive),
                (Objective(harness, Factory, X.RestartMachine), Objective(harness, Factory, X.OreProcessor)));
            Assert.IsFalse(crucible.DynamicObjects.Any(obj => obj.SceneMissionId == Factory && obj.IsInWorld), "no switch on the Crucible");

            var ore = SceneObject(harness, copy, Factory, X.OreRole);
            Assert.AreEqual((X.SwitchClass, X.OreName), ((uint)ore.EntityClassId, ore.NameOverrideId));
            Assert.AreEqual(harness.Client.Player.Id, ore.SceneOwnerCharacterId, "the player's own");
            Use(harness, copy, ore, X.UseArgument);
            Assert.AreEqual(MissionObjectiveState.Inactive, Objective(harness, Factory, X.OreProcessor), "no use before 1 is done");
            Assert.IsTrue(ore.IsEnabled, "the ore switch stays");

            var factory = SceneObject(harness, copy, Factory, X.FactoryRole);
            Assert.AreEqual((X.SwitchClass, X.FactoryName), ((uint)factory.EntityClassId, factory.NameOverrideId));
            Use(harness, copy, factory, X.UseArgument);
            Use(harness, copy, factory, X.UseArgument);
            Assert.AreEqual((MissionObjectiveState.Completed, MissionObjectiveState.Incomplete),
                (Objective(harness, Factory, X.RestartMachine), Objective(harness, Factory, X.OreProcessor)), "the machine restarted, once");
            Assert.IsFalse(factory.IsEnabled, "the factory switch thrown");
            Assert.IsFalse(harness.Client.Player.Missions[Factory].Completeable);

            ReloadAndRecover(harness, copy);
            Assert.AreEqual((MissionObjectiveState.Completed, MissionObjectiveState.Incomplete),
                (Objective(harness, Factory, X.RestartMachine), Objective(harness, Factory, X.OreProcessor)), "kept at a reload in the middle of the scene");
            Assert.IsFalse(SceneObject(harness, copy, Factory, X.FactoryRole).IsEnabled, "the thrown switch stays thrown");
            ore = SceneObject(harness, copy, Factory, X.OreRole);
            Use(harness, copy, ore, X.UseArgument);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Factory, X.OreProcessor), "the ore processor on");
            Assert.IsTrue(harness.Client.Player.Missions[Factory].Completeable);

            FullBagWaits(harness, brian, Factory, 1, ModuleFiller);
            FailedTurnInPaysNothing(harness, brian, Factory, 1);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, brian, Factory, 1, X.Experience, X.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Factory), "MissionRewarded once");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, X.RegenBonus), "the module chosen");
            for (var tick = 0; tick < 4; tick++)
                Tick(harness, copy, 250);
            Assert.IsNull(FindSceneObject(harness, copy, Factory, X.OreRole), "the switches go with the mission");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, brian, Factory), "done once");
        }

        /// <summary>An abandon of Fix the Factory after the factory switch: taken again, both switches stand ready and 1 is open again.</summary>
        [TestMethod]
        public void FixTheFactoryAbandonedStartsOver()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            C.SeedCompleted(harness, GetTo, Brain);
            C.EnterCrucible(harness);
            var copy = IntoChaukas(harness);
            var brian = Giver(copy, N.Brian);
            Accept(harness, brian, Factory);
            Tick(harness, copy, 250);
            Use(harness, copy, SceneObject(harness, copy, Factory, X.FactoryRole), X.UseArgument);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Factory, X.RestartMachine));

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Factory));
            harness.Drain();
            Accept(harness, brian, Factory);
            Tick(harness, copy, 250);
            Assert.AreEqual((MissionObjectiveState.Incomplete, MissionObjectiveState.Inactive),
                (Objective(harness, Factory, X.RestartMachine), Objective(harness, Factory, X.OreProcessor)), "all over again");
            Assert.IsTrue(SceneObject(harness, copy, Factory, X.FactoryRole).IsEnabled, "the factory switch ready again");
            Assert.IsTrue(SceneObject(harness, copy, Factory, X.OreRole).IsEnabled, "the ore switch ready");
        }

        /// <summary>The use targets: 1521's container, 1522's two switches.</summary>
        private static IEnumerable<(string Label, Vector3 At)> UseTargets()
        {
            yield return ("the container", ToVector(B.ContainerAt));
            yield return ("the factory switch", ToVector(X.FactorySwitchAt));
            yield return ("the ore switch", ToVector(X.OreSwitchAt));
        }

        /// <summary>The scene's places in Chaukas (Brian's own is apart).</summary>
        private static IEnumerable<(string Label, Vector3 At)> Places()
        {
            yield return ("the Brain room's mouth", ToVector(B.MouthAt));
            yield return ("Brian at the Brain", ToVector(B.BrainAt));
            yield return ("the container", ToVector(B.ContainerAt));
            yield return ("the factory switch", ToVector(X.FactorySwitchAt));
            yield return ("the ore switch", ToVector(X.OreSwitchAt));
            for (var index = 0; index < B.AttackersAt.Length; index++)
                yield return ($"attacker {index + 1}", ToVector(B.AttackersAt[index]));
        }

        /// <summary>Into the squad's copy of Chaukas through its door on the Crucible, its pools back as after their time, the scenes resumed.</summary>
        private static MapChannel IntoChaukas(WildernessRuntimeTestHarness harness)
        {
            if (!harness.Maps.MapChannelArray.ContainsKey(Chaukas))
                LoadMap(harness, Chaukas);
            var copy = ThroughTheDoor(harness, Chaukas);
            foreach (var pool in copy.SpawnPools.Where(pool => ClavigerPools.Contains(pool.DbId)))
                pool.ClearedAtUtcMs = 0;
            // A squad copy outlives a test: an earlier test's Brian copy (the same character id, a run this test's database
            // does not hold) would still hide the public Brian (MissionNpcVisibility, ReplaceWhilePresent). Its pool goes.
            var runs = new[] { GetTo, Brain, Factory }.SelectMany(mission => Runs(harness, mission)).ToHashSet();
            copy.SpawnPools.RemoveAll(pool => pool.SceneRunId != null && pool.ScenarioMissionId is GetTo or Brain or Factory &&
                !runs.Contains(pool.SceneRunId));
            harness.Manager.Scenes.Resume(harness.Client);
            harness.Drain();
            return copy;
        }

        /// <summary>The navmesh walk from the arrival to <paramref name="to"/> (one island: complete).</summary>
        private static List<Vector3> WalkFromArrival(NavMeshQuery nav, Vector3 to, string label)
        {
            var start = nav.Nearest(Arrival) ?? Arrival;
            var path = nav.FindPath(start, nav.Nearest(to) ?? to, out var complete);
            Assert.IsTrue(path != null && complete, $"{label}: a walk from the arrival");
            return path.Prepend(start).ToList();
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
            Assert.IsGreaterThanOrEqualTo(pools.Length * 2, standing.Length, "the pools stand (two or three a pool)");
            foreach (var creature in standing)
                Kill(harness, copy, creature);
            SpawnPoolManager.Instance.SpawnPoolWorker(copy, 0);
            harness.Drain();
        }

        private static uint Counter(WildernessRuntimeTestHarness harness, uint mission, uint objective) =>
            harness.Client.Player.Missions[mission].Objectives[objective].Counters[0];
    }
}
