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
using Rasa.Packets.MapChannel.Client;
using Rasa.Packets.MapChannel.Server;
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
    using I = CrucibleIncurablesWardNpcsV1;
    using SS = CrucibleSadisticSabotageV1;
    using L = CrucibleLostInTheAsylumV1;
    using R = CrucibleRehabilitationAtGunpointV1;

    /// <summary>
    /// Crucible set 11a, played through as a player plays it: Viddia Sigal Nema's Sadistic Sabotage (1445: down into the
    /// Incurables Ward, squad instance 2141, to report to Supervisor Sheira), Sheira's Lost in the Asylum (1444: Mechanic Shanul,
    /// Technician Paliv's body and Engineer Dhimand found in the Ward) and Larai Sigal Tempa's Rehabilitation at Gunpoint (1402:
    /// Jaikev, Parvatee and 30 Incurables killed in the Ward, back to Tempa on the Crucible). With them the Ward's guards (as
    /// CrucibleGuardTests holds the Crucible's): its NPCs, the body, the pools and markers on the navmesh and a client floor,
    /// walked to from the arrival with no climb the client fails, clear of its door and of the hostile pools, the talk targets
    /// seen from the walks' ends; and the Ward's navmesh islands, joined for the client at the north airlock only (upstream).
    ///
    /// Why these tests: every NPC and creature of the Ward is new or moved; 1445 goes from the Crucible into a squad instance,
    /// 1444 is played in it, 1402 is taken on the Crucible and played in it; 1444 talks to a scene object; 1402 counts two
    /// classes to 30 and names two bosses that come back on their own time in the copy. A target off the navmesh or the walks,
    /// an offer before its prerequisite, a kill before the accept counted, the 29th kill counted as the 30th, progress lost at a
    /// reload or kept after an abandon, or a turn-in that pays twice would each break them.
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class CrucibleIncurablesWardTests
    {
        /// <summary>The Crucible markers in the Ward: 1445's on Sheira, 1444's three, 1402's on the bosses and the nine pools, 1398's five, 1401's six.</summary>
        internal const int WardMarkers = 1 + 3 + 2 + 9 + 5 + 6;

        private const uint Ward = I.Ward;
        private const uint GameWardens = CrucibleGameWardensV1.MissionId;
        private const uint Sabotage = SS.MissionId, Asylum = L.MissionId, Rehab = R.MissionId;
        private const uint ConsumableFiller = 45060;       // Class VII Basic Med Pack (as CrucibleGrupaChiefsTests): fills the consumables bag
        private const uint ArmorFiller = 12748;            // Armor_T3_Bio_V01_CMN_Gloves_30_to_34 (inventory category 1)
        private const uint ModuleFiller = 122983;          // Armor Module: Regen Bonus [1] (inventory category 3, as CrucibleMinefieldAndSalvageTests)

        private static readonly Vector3 Arrival = ToVector(I.ArrivalAt);
        private static readonly Vector3 AirlockSouth = ToVector(I.AirlockSouthAt), AirlockNorth = ToVector(I.AirlockNorthAt);
        private static readonly uint[] IncurablePools = I.IncurablePools.Select(pool => pool.Pool).ToArray();
        private static readonly uint[] WardPools = IncurablePools.Append(I.JaikevPool).Append(I.ParvateePool).ToArray();

        [TestInitialize]
        public void Clock() => PlateauGiantKillerSquadTests.UseClock();

        [TestCleanup]
        public void Restore() => PlateauGiantKillerSquadTests.RestoreClock();

        /// <summary>
        /// In the squad's copy: Sheira, Shanul and Dhimand stand with the client's names and packages, friendly, on the navmesh,
        /// ReachClearance or more outside every hostile pool's circle; the Incurables (two Brutes and two Slayers a pool),
        /// Jaikev and Parvatee come out of their pools hostile, of their classes, levels and hit points; Paliv's body and every
        /// one of these places is walked to from the arrival: Sheira and Shanul on the arrival's island, the rest only through
        /// the north airlock (upstream's navmesh does not join it, N13; the local N15 rebake does: WalkFromArrival); none within 5 m of the door's circle,
        /// no hostile pool within ReachClearance of it.
        /// </summary>
        [TestMethod]
        public void TheWardsNpcsCreaturesAndTheBodyStandWhereTheyAreWalkedTo()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            C.EnterCrucible(harness);
            var copy = IntoWard(harness);
            Assert.IsTrue(copy.IsSquadInstance);
            var nav = copy.NavMesh;

            var links = harness.World.Set<MapLinkEntry>().AsNoTracking().Where(link => link.Enabled != 0 && link.MapContextId == Ward).ToList();
            Assert.IsNotEmpty(links, "the Ward's door");
            float LinkGap(Vector3 at, float radius = 0) => links.Min(link =>
                Vector2.Distance(new(at.X, at.Z), new((float)link.PosX, (float)link.PosZ)) - (float)link.Radius - radius);
            var hostile = harness.World.SpawnPoolEntries.AsNoTracking().Where(pool => pool.MapContextId == Ward && WardPools.Contains(pool.Id)).ToList();
            Assert.HasCount(WardPools.Length, hostile);

            foreach (var (pool, name, package, north) in new (uint, uint, uint, bool)[]
            {
                (I.Sheira, 9517, I.SheiraPackage, false), (I.Shanul, I.ShanulName, I.ShanulPackage, false), (I.Dhimand, I.DhimandName, I.DhimandPackage, true)
            })
            {
                var npc = Giver(copy, pool);
                Assert.AreEqual((name, package), (npc.NameId, npc.Npc?.NpcPackageId), $"pool {pool}");
                Assert.AreNotEqual(TargetCategory.Hostile, npc.TargetCategory, $"pool {pool} friendly");
                Assert.AreEqual(ToVector(I.At(pool)).X, npc.Position.X, 0.01f, $"pool {pool} where set 11a put it");
                Assert.AreEqual(ToVector(I.At(pool)).Z, npc.Position.Z, 0.01f, $"pool {pool} where set 11a put it");
                AssertOnGround(copy, npc.Position, $"pool {pool}");
                WalkFromArrival(nav, npc.Position, north, $"pool {pool}");
                Assert.IsGreaterThanOrEqualTo(5f, LinkGap(npc.Position), $"pool {pool} clear of the door");
                foreach (var other in hostile)
                    Assert.IsGreaterThanOrEqualTo(MarshesNpcsStandStillV1.ReachClearance,
                        Vector2.Distance(new(npc.Position.X, npc.Position.Z), new((float)other.PosX, (float)other.PosZ)) - (float)other.Radius,
                        $"NPC pool {pool} outside hostile pool {other.Id}'s reach");
            }
            Assert.IsTrue(BehaviorManager.NeverMoves(CreatureManager.Instance.LoadedCreatures[I.Shanul]), "Shanul stands still");
            // Set 11b: Dhimand walks (1398's escort copy follows); the public Dhimand keeps his post (pose Standing).
            Assert.IsFalse(BehaviorManager.NeverMoves(CreatureManager.Instance.LoadedCreatures[I.Dhimand]), "Dhimand can walk (1398)");
            Assert.IsTrue(NpcPoses.HoldsPost(Giver(copy, I.Dhimand)), "the public Dhimand keeps his post (pose Standing)");

            var corpse = ToVector(L.CorpseAt);
            AssertOnGround(copy, corpse, "Paliv's body");
            WalkFromArrival(nav, corpse, true, "Paliv's body");
            Assert.IsGreaterThanOrEqualTo(5f, LinkGap(corpse), "Paliv's body clear of the door");

            Spawn(copy, WardPools);
            foreach (var (pool, at) in I.IncurablePools)
            {
                var here = Here(copy).Where(creature => creature.SpawnPool?.DbId == pool).ToArray();
                Assert.AreEqual((2, 2), (here.Count(one => one.DbId == I.Brute), here.Count(one => one.DbId == I.Slayer)), $"pool {pool}");
                foreach (var one in here)
                {
                    var brute = one.DbId == I.Brute;
                    Assert.AreEqual((I.IncurableLevel, TargetCategory.Hostile, brute ? I.BruteClass : I.SlayerClass),
                        ((uint)one.Level, one.TargetCategory, (uint)one.EntityClass), $"pool {pool}");
                    Assert.AreEqual((int)I.IncurableHitPoints, (int)one.Attributes[Attributes.Health].NormalMax, $"pool {pool}: its hit points");
                }
                var centre = ToVector(at);
                AssertOnGround(copy, centre, $"pool {pool}");
                WalkFromArrival(nav, centre, true, $"pool {pool}");
                Assert.IsGreaterThanOrEqualTo(MarshesNpcsStandStillV1.ReachClearance, LinkGap(centre, I.RadiusOf(pool)), $"pool {pool} clear of the door");
            }
            foreach (var (pool, creature, cls, at) in new[] { (I.JaikevPool, I.Jaikev, I.JaikevClass, I.JaikevAt), (I.ParvateePool, I.Parvatee, I.ParvateeClass, I.ParvateeAt) })
            {
                var boss = Here(copy).Single(one => one.SpawnPool?.DbId == pool);
                Assert.AreEqual((creature, I.BossLevel, TargetCategory.Hostile, cls, (int)I.BossHitPoints),
                    (boss.DbId, (uint)boss.Level, boss.TargetCategory, (uint)boss.EntityClass, (int)boss.Attributes[Attributes.Health].NormalMax), $"pool {pool}");
                Assert.AreEqual(0.0, harness.World.SpawnPoolEntries.AsNoTracking().Single(row => row.Id == pool).Radius, $"pool {pool}: a point");
                AssertOnGround(copy, ToVector(at), $"pool {pool}");
                WalkFromArrival(nav, ToVector(at), true, $"pool {pool}");
                Assert.IsGreaterThanOrEqualTo(MarshesNpcsStandStillV1.ReachClearance, LinkGap(ToVector(at)), $"pool {pool} clear of the door");
            }
        }

        /// <summary>
        /// The client's floors (MarshesClientFloorTests; inconclusive without the client's files): Sheira, Shanul, Dhimand,
        /// Paliv's body, the pools and the Ward's markers each on a client floor with none just over it; the four talk targets
        /// seen from 16 of 16 places 2 m round each and from each point of the walk from the arrival 5 to 2 m from it; every
        /// walk from the arrival to a Ward place climbs within CrucibleGuardTests.MaxClientStep and MaxClientGrade; the north
        /// airlock a client floor all the way (no step over MaxClientStep, nothing in the way at eye height); no lava in the Ward.
        /// </summary>
        [TestMethod]
        public void TheWardsPlacesAndMarkersStandOnAClientFloorAndAreWalkedAndSeen()
        {
            const float MarkerHeight = 2;
            var dataDir = F.ClientData();
            using var harness = WildernessRuntimeTestHarness.Create();
            C.EnterCrucible(harness);
            var copy = IntoWard(harness);
            var nav = copy.NavMesh;
            using var meshes = new MeshLibrary(dataDir, F.MeshCsv());
            var name = harness.World.MapInfoEntries.AsNoTracking().Single(row => row.Id == Ward).MapName;
            var geometry = MapGeometry.Load(Path.Combine(dataDir, "maps", name), meshes, 1);

            var talks = new List<(string Label, Vector3 At, bool North, bool Npc)>
            {
                ("Sheira", ToVector(I.SheiraAt), false, true), ("Shanul", ToVector(I.ShanulAt), false, true),
                ("Dhimand", ToVector(I.DhimandAt), true, true), ("Paliv's body", ToVector(L.CorpseAt), true, false)
            };
            var places = talks.Select(talk => (talk.Label, talk.At, talk.North, talk.Npc)).ToList();
            places.AddRange(I.IncurablePools.Select(pool => ($"pool {pool.Pool}", ToVector(pool.At), true, true)));
            places.Add(("Jaikev", ToVector(I.JaikevAt), true, true));
            places.Add(("Parvatee", ToVector(I.ParvateeAt), true, true));
            var (floors, ceilings) = F.Surfaces(geometry, places.Select(place => (place.At, place.Npc ? F.Walkable : F.NotVertical)).ToList());
            var failures = new List<string>();
            for (var i = 0; i < places.Count; i++)
                if (places[i].At.Y - floors[i] > F.MaxOver || !float.IsPositiveInfinity(ceilings[i]))
                    failures.Add($"{places[i].Label} at {places[i].At}: floor {floors[i]}, ceiling {ceilings[i]}");

            // The markers in the Ward (MissionMarkerProjection.ResolveMap 2141).
            var catalog = new MissionContentCatalog(harness, new Dictionary<uint, Mission>(), null);
            catalog.Load();
            var markers = harness.World.MissionIndicatorEntries.AsNoTracking().Where(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision).ToList()
                .Where(marker => MissionMarkerProjection.ResolveMap(catalog, marker.MissionId, marker.ObjectiveId, new MissionIndicator { IndicatorId = marker.IndicatorId }) == Ward)
                .Select(marker => (Label: $"{marker.MissionId}.{marker.ObjectiveId}/{marker.IndicatorId}", At: new Vector3((float)marker.PosX, (float)marker.PosY, (float)marker.PosZ)))
                .ToList();
            Assert.HasCount(WardMarkers, markers, "the Ward's markers");
            var (markerFloors, _) = F.Surfaces(geometry, markers.Select(marker => (marker.At + new Vector3(0, MarkerHeight - F.MaxUnder, 0), F.Walkable)).ToList());
            for (var i = 0; i < markers.Count; i++)
            {
                var ground = nav.GroundHeight(markers[i].At + new Vector3(0, MarkerHeight, 0));
                if (ground is not { } y || MathF.Abs(y - markers[i].At.Y) > MarkerHeight)
                    failures.Add($"{markers[i].Label}: navmesh {ground}");
                if (markers[i].At.Y - markerFloors[i] > MarkerHeight)
                    failures.Add($"{markers[i].Label}: client floor {markerFloors[i]}");
            }

            // Each walk from the arrival is one the client climbs; each talk target is seen round it and from its walk's end.
            foreach (var (label, at, north, npc) in places)
            {
                var walk = WalkFromArrival(nav, at, north, label);
                var (step, stepAt, grade, gradeAt) = CrucibleGuardTests.ClientClimb(nav, geometry, walk);
                if (step > CrucibleGuardTests.MaxClientStep)
                    failures.Add(FormattableString.Invariant($"the arrival to {label}: a {step:0.00} m step at ({stepAt.X:0.0}, {stepAt.Y:0.0}, {stepAt.Z:0.0})"));
                if (grade > CrucibleGuardTests.MaxClientGrade)
                    failures.Add(FormattableString.Invariant($"the arrival to {label}: {grade:0.00} a metre over 12 m from ({gradeAt.X:0.0}, {gradeAt.Y:0.0}, {gradeAt.Z:0.0})"));
                // The walk back to the arrival (the Ward's door), through the airlock for the north places.
                var back = WalkToArrival(nav, at, north, label);
                var (backStep, backStepAt, backGrade, backGradeAt) = CrucibleGuardTests.ClientClimb(nav, geometry, back);
                if (backStep > CrucibleGuardTests.MaxClientStep)
                    failures.Add(FormattableString.Invariant($"{label} to the arrival: a {backStep:0.00} m step at ({backStepAt.X:0.0}, {backStepAt.Y:0.0}, {backStepAt.Z:0.0})"));
                if (backGrade > CrucibleGuardTests.MaxClientGrade)
                    failures.Add(FormattableString.Invariant($"{label} to the arrival: {backGrade:0.00} a metre over 12 m from ({backGradeAt.X:0.0}, {backGradeAt.Y:0.0}, {backGradeAt.Z:0.0})"));
                if (!talks.Any(talk => talk.Label == label))
                    continue;
                var near = F.Near(geometry, new List<Vector3> { at }, 8)[0];
                var aim = at + new Vector3(0, npc ? F.NpcMiddle : F.ObjectMiddle, 0);
                for (var k = 0; k < F.RingPlaces; k++)
                {
                    var angle = k * 2 * MathF.PI / F.RingPlaces;
                    var stand = at + new Vector3(MathF.Sin(angle) * F.Ring, 0, MathF.Cos(angle) * F.Ring);
                    if (F.FloorAt(near, stand, at.Y - F.MaxOver, at.Y + F.MaxUnder) is not { } fy || !F.Clear(near, stand with { Y = fy + F.Eye }, aim))
                        failures.Add($"{label} not seen from ({stand.X:F1}, {stand.Z:F1})");
                }
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

            // The north airlock: the client's floor goes on across the server navmesh's gap, nothing in the way.
            var across = F.Near(geometry, new List<Vector3> { Vector3.Lerp(AirlockSouth, AirlockNorth, 0.5f) }, 8)[0];
            float? last = null;
            for (var s = 0; s <= 18; s++)
            {
                var at = Vector3.Lerp(AirlockSouth, AirlockNorth, s / 18f);
                if (F.FloorAt(across, at, at.Y - 1.5f, at.Y + 1f) is not { } floor)
                {
                    failures.Add($"no client floor in the airlock at ({at.X:F1}, {at.Z:F1})");
                    continue;
                }
                if (last is { } before && MathF.Abs(floor - before) > CrucibleGuardTests.MaxClientStep)
                    failures.Add($"a step in the airlock at ({at.X:F1}, {at.Z:F1})");
                last = floor;
            }
            if (!F.Clear(across, AirlockSouth + new Vector3(0, F.Eye, 0), AirlockNorth + new Vector3(0, F.Eye, 0)))
                failures.Add("something in the way through the airlock");
            // The server navmesh: upstream's has a 1 m gap in the airlock (N13, NOTES-upstream-bugs.md; the walks go across it in
            // segments), the local N15 rebake joins it (the walks go direct). Either way each side is walked to its end.
            var south = nav.FindPath(nav.Nearest(Arrival).Value, nav.Nearest(AirlockSouth).Value, out var southWhole);
            var northSide = nav.FindPath(nav.Nearest(AirlockNorth).Value, nav.Nearest(ToVector(I.DhimandAt)).Value, out var northWhole);
            Assert.IsTrue(south != null && southWhole && northSide != null && northWhole, "the airlock's two sides are walked to (joined or not)");

            // No lava in the Ward: round the point pools 20 m, and every place. NOTE: LavaSurfaces has no entry for the Ward's map
            // (adv_arieki_ligo_crucible_incurablesward), so LavaDamage.InLava is false everywhere here and this check is vacuous
            // (the probe saw no lava model in the Ward); it holds the place should the map gain lava data.
            Assert.IsFalse(Rasa.Data.LavaSurfaces.ByMap.ContainsKey(name), "the Ward has no lava data (the check below is vacuous)");
            foreach (var (label, at, _, _) in places)
                for (var dx = -20f; dx <= 20; dx += 2)
                    for (var dz = -20f; dz <= 20; dz += 2)
                        if (dx * dx + dz * dz <= 400 && nav.Nearest(at + new Vector3(dx, 0, dz)) is { } ground && LavaDamage.InLava(name, ground))
                            failures.Add(FormattableString.Invariant($"lava at ({ground.X:0.0}, {ground.Y:0.0}, {ground.Z:0.0}) by {label}"));
            Assert.IsEmpty(failures, string.Join("; ", failures));
        }

        /// <summary>
        /// Every place the spawn draw of an Incurable pool (radius I.RadiusOf: 5; the PointPools 0, their radius-5 draw reached 20-40 m off) can set a creature down
        /// (CrucibleGuardTests.SpawnReach, Detour's draw over the polygons the circle touches, upstream C11) is in the pool's own
        /// room: on its navmesh island (a whole walk from the centre), no further than ReachLimit across, and seen from the
        /// centre at eye height through no client wall. A reach out of the room or the island would set an Incurable where the
        /// player cannot come, or by a friendly.
        /// </summary>
        [TestMethod]
        public void TheIncurablePoolsSetTheirCreaturesDownInTheirRoom()
        {
            const float ReachLimit = 20;
            var dataDir = F.ClientData();
            using var harness = WildernessRuntimeTestHarness.Create();
            C.EnterCrucible(harness);
            var copy = IntoWard(harness);
            var nav = copy.NavMesh;
            using var meshes = new MeshLibrary(dataDir, F.MeshCsv());
            var name = harness.World.MapInfoEntries.AsNoTracking().Single(row => row.Id == Ward).MapName;
            var geometry = MapGeometry.Load(Path.Combine(dataDir, "maps", name), meshes, 1);
            var failures = new List<string>();
            foreach (var (pool, raw) in I.IncurablePools)
            {
                var centre = nav.Nearest(ToVector(raw)).Value;
                // A point pool has no draw (SpawnPoolManager: pool.Radius <= 0 sets it on its point): its reach is the centre. The
                // radius as the World DB has it (I.RadiusOf, and set 15 made 770048 a point: CrucibleBloodFireSteelV1.PointedPool).
                var radius = (float)harness.World.SpawnPoolEntries.AsNoTracking().Single(row => row.Id == pool).Radius;
                Assert.AreEqual(pool == CrucibleBloodFireSteelV1.PointedPool ? 0f : I.RadiusOf(pool), radius, $"pool {pool}: its radius");
                var reach = radius > 0 ? CrucibleGuardTests.SpawnReach(nav, centre, radius) : new List<Vector3> { centre };
                Assert.IsNotEmpty(reach, $"pool {pool}: a reach");
                var near = F.Near(geometry, new List<Vector3> { centre }, ReachLimit + 2)[0];
                foreach (var at in reach)
                {
                    var across = Vector2.Distance(new(at.X, at.Z), new(centre.X, centre.Z));
                    if (across > ReachLimit)
                        failures.Add(FormattableString.Invariant($"pool {pool}: reach ({at.X:0.0}, {at.Y:0.0}, {at.Z:0.0}) {across:0.0} m off"));
                    else if (nav.FindPath(centre, at, out var complete) == null || !complete)
                        failures.Add(FormattableString.Invariant($"pool {pool}: reach ({at.X:0.0}, {at.Y:0.0}, {at.Z:0.0}) off its island"));
                    else if (across > 1 && !F.Clear(near, centre + new Vector3(0, F.Eye, 0), at + new Vector3(0, F.Eye, 0)))
                        failures.Add(FormattableString.Invariant($"pool {pool}: reach ({at.X:0.0}, {at.Y:0.0}, {at.Z:0.0}) behind a wall"));
                }
            }
            Assert.IsEmpty(failures, "pools " + string.Join(", ", failures.Select(failure => failure.Split(':')[0]).Distinct()) + ": " +
                string.Join("; ", failures.Take(20)));
        }

        /// <summary>
        /// On the Crucible: Tempa (moved to TaRapedia's place by the Larai Arena) and the marker before the Ward's door are
        /// walked to from the Staal waypoint with no climb the client fails; the door marker (fix job I) 5 to 10 m outside the
        /// door's circle and on the walk from Tempa to the door, its own circle 5 m or more outside the door's; Tempa seen from 16 of 16 places 2 m round him (his floor, sight and hostile clearance on the Crucible are
        /// CrucibleGuardTests').
        /// </summary>
        [TestMethod]
        public void TempaAndTheWardDoorMarkerAreWalkedToOnTheCrucible()
        {
            var dataDir = F.ClientData();
            using var harness = WildernessRuntimeTestHarness.Create();
            var crucible = C.EnterCrucible(harness, C.StaalWaypoint);
            var nav = crucible.NavMesh;
            using var meshes = new MeshLibrary(dataDir, F.MeshCsv());
            var geometry = MapGeometry.Load(Path.Combine(dataDir, "maps", crucible.MapInfo.MapName), meshes, 1);
            var tempa = Npc(crucible, I.Tempa);
            Assert.AreEqual(9067U, tempa.NameId);
            Assert.AreEqual(ToVector(I.TempaAt).X, tempa.Position.X, 0.01f);
            Assert.AreEqual(ToVector(I.TempaAt).Z, tempa.Position.Z, 0.01f);
            AssertOnGround(crucible, tempa.Position, "Tempa");
            var failures = new List<string>();
            foreach (var (label, at) in new[] { ("Tempa", tempa.Position), ("the Ward door marker", ToVector(CrucibleFixIV1.DoorWayNow)) })
            {
                var start = nav.Nearest(C.StaalWaypoint) ?? C.StaalWaypoint;
                var walk = nav.FindPath(start, nav.Nearest(at) ?? at, out var complete);
                Assert.IsTrue(walk != null && complete, $"{label}: a walk from the Staal waypoint");
                var (step, _, grade, _) = CrucibleGuardTests.ClientClimb(nav, geometry, walk.Prepend(start).ToList());
                if (step > CrucibleGuardTests.MaxClientStep || grade > CrucibleGuardTests.MaxClientGrade)
                    failures.Add(FormattableString.Invariant($"{label}: step {step:0.00}, grade {grade:0.00}"));
            }
            var door = harness.World.Set<MapLinkEntry>().AsNoTracking().Single(link => link.MapContextId == CrucibleMissionDataV1.MapContextId &&
                link.DestMapContextId == Ward && link.Enabled != 0);
            // Fix job I (to-fix row 69): the marker stood 13.7 m outside the door's circle; now 5 to 10 m outside it (8.5), its own circle
            // still 5 m or more outside the door's, and on the navmesh walk from Tempa to the door (within 1 m of it).
            var doorWay = ToVector(CrucibleFixIV1.DoorWayNow);
            var outside = Vector2.Distance(new((float)door.PosX, (float)door.PosZ), new(doorWay.X, doorWay.Z)) - (float)door.Radius;
            Assert.IsGreaterThanOrEqualTo(5f, outside, "the door marker 5 m or more outside the door's circle");
            Assert.IsLessThanOrEqualTo(10f, outside, "the door marker 10 m or less outside the door's circle");
            Assert.IsGreaterThanOrEqualTo(5f, outside - (float)CrucibleFixIV1.DoorRadiusNow, "the door marker's circle clear of the door");
            var tempaStart = nav.Nearest(tempa.Position) ?? tempa.Position;
            var toDoor = nav.FindPath(tempaStart, nav.Nearest(new Vector3((float)door.PosX, (float)door.PosY, (float)door.PosZ)).Value, out var doorComplete);
            Assert.IsTrue(toDoor != null && doorComplete, "a walk from Tempa to the door");
            var doorWalk = toDoor.Prepend(tempaStart).ToList();
            var offWalk = float.MaxValue;
            for (var i = 0; i + 1 < doorWalk.Count; i++)
                for (var s = 0; s <= 50; s++)
                    offWalk = MathF.Min(offWalk, Vector3.Distance(Vector3.Lerp(doorWalk[i], doorWalk[i + 1], s / 50f), doorWay));
            Assert.IsLessThanOrEqualTo(1f, offWalk, "the door marker on the walk from Tempa to the door");
            var near = F.Near(geometry, new List<Vector3> { tempa.Position }, 8)[0];
            var aim = tempa.Position + new Vector3(0, F.NpcMiddle, 0);
            for (var k = 0; k < F.RingPlaces; k++)
            {
                var angle = k * 2 * MathF.PI / F.RingPlaces;
                var stand = tempa.Position + new Vector3(MathF.Sin(angle) * F.Ring, 0, MathF.Cos(angle) * F.Ring);
                if (F.FloorAt(near, stand, tempa.Position.Y - F.MaxOver, tempa.Position.Y + F.MaxUnder) is not { } fy ||
                    !F.Clear(near, stand with { Y = fy + F.Eye }, aim))
                    failures.Add($"Tempa not seen from ({stand.X:F1}, {stand.Z:F1})");
            }
            Assert.IsEmpty(failures, string.Join("; ", failures));
        }

        /// <summary>
        /// Sadistic Sabotage after Game Wardens: only Nema offers it, on the Crucible; Sheira hears 1 only in the squad's Ward
        /// (only that), which completes it; a reload keeps it; a full bag waits, a failed save pays nothing; Sheira pays one of
        /// the four stacks, once; Lost in the Asylum follows.
        /// </summary>
        [TestMethod]
        public void SadisticSabotageReportsToSheiraInTheWard()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var crucible = C.EnterCrucible(harness, C.StaalWaypoint);
            var nema = Npc(crucible, SS.Nema);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, nema, Sabotage), "it waits for Game Wardens");
            C.SeedCompleted(harness, GameWardens);
            crucible = C.EnterCrucible(harness, C.StaalWaypoint);
            nema = Npc(crucible, SS.Nema);
            var offer = WildernessRanjaGorgeTests.Offer(harness, nema, Sabotage);
            CollectionAssert.AreEqual(new[] { SS.BasicMedPack, SS.FragGrenade, SS.EmpBomb, SS.CryoGrenade },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Accept(harness, nema, Sabotage);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Sabotage, SS.Report));
            Assert.IsEmpty(Topics(harness, nema, Sabotage), "Nema hears no objective");

            var copy = IntoWard(harness);
            var sheira = Giver(copy, I.Sheira);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, sheira, Asylum), "Lost in the Asylum waits for it");
            WildernessRanjaGorgeTests.Reload(harness);
            harness.Drain();
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Sabotage, SS.Report), "kept at a reload");
            Talk(harness, sheira, Sabotage, SS.Report);
            Assert.IsTrue(harness.Client.Player.Missions[Sabotage].Completeable);
            Assert.IsEmpty(Topics(harness, sheira, Sabotage), "heard once");

            FullBagWaits(harness, sheira, Sabotage, 1, ConsumableFiller);
            FailedTurnInPaysNothing(harness, sheira, Sabotage, 1);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, sheira, Sabotage, 1, SS.Experience, SS.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Sabotage), "MissionRewarded once");
            Assert.AreEqual(SS.FragGrenades, WildernessRanjaGorgeTests.Held(harness, SS.FragGrenade), "the three Fragmentation Grenades");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, sheira, Asylum), "Lost in the Asylum follows");
            crucible = C.EnterCrucible(harness, C.StaalWaypoint);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, Npc(crucible, SS.Nema), Sabotage), "done once");
        }

        /// <summary>
        /// Lost in the Asylum after Sadistic Sabotage: Sheira offers it in the Ward; Paliv's body is the player's own and only in
        /// the Ward; Shanul, the body and Dhimand each hear their objective only, in any order (a reload in the middle of the
        /// scene keeps the talks and the body); two of three are not enough; the third makes it completeable; a full bag waits,
        /// a failed save pays nothing; Sheira pays once; the body goes with the mission.
        /// </summary>
        [TestMethod]
        public void LostInTheAsylumFindsTheThreeMen()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            C.SeedCompleted(harness, GameWardens, Sabotage);
            var crucible = C.EnterCrucible(harness);
            var copy = IntoWard(harness);
            var sheira = Giver(copy, I.Sheira);
            var offer = WildernessRanjaGorgeTests.Offer(harness, sheira, Asylum);
            CollectionAssert.AreEqual(new[] { L.MechGloves, L.StealthHelmet, L.HazmatGloves, L.GravitonBoots },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Accept(harness, sheira, Asylum);
            Tick(harness, copy, 250);
            Assert.AreEqual((MissionObjectiveState.Incomplete, MissionObjectiveState.Incomplete, MissionObjectiveState.Incomplete),
                (Objective(harness, Asylum, L.FindShanul), Objective(harness, Asylum, L.FindPaliv), Objective(harness, Asylum, L.FindDhimand)), "all three open");
            Assert.IsFalse(crucible.DynamicObjects.Any(obj => obj.SceneMissionId == Asylum && obj.IsInWorld), "no body on the Crucible");
            var body = Body(harness, copy);

            var shanul = Giver(copy, I.Shanul);
            var dhimand = Giver(copy, I.Dhimand);
            Assert.IsFalse(Topics(harness, shanul, Asylum).Contains(L.FindDhimand), "Shanul hears only his");
            Talk(harness, dhimand, Asylum, L.FindDhimand);
            Talk(harness, shanul, Asylum, L.FindShanul);
            Assert.IsFalse(harness.Client.Player.Missions[Asylum].Completeable, "two of three");

            ReloadAndRecover(harness, copy);
            Assert.AreEqual((MissionObjectiveState.Completed, MissionObjectiveState.Incomplete, MissionObjectiveState.Completed),
                (Objective(harness, Asylum, L.FindShanul), Objective(harness, Asylum, L.FindPaliv), Objective(harness, Asylum, L.FindDhimand)),
                "kept at a reload in the middle of the scene");
            body = Body(harness, copy);
            AssertTalks(harness, body, L.FindPaliv);
            var read = TalkTo(harness, body, L.FindPaliv);
            Assert.AreEqual(1, read.OfType<MissionCompleteablePacket>().Count(packet => packet.MissionId == Asylum && packet.IsCompleteable), "MissionCompleteable once");
            Assert.IsTrue(harness.Client.Player.Missions[Asylum].Completeable);
            Assert.IsNull(OpenBody(harness, body), "read once");

            FullBagWaits(harness, sheira, Asylum, 2, ArmorFiller);
            FailedTurnInPaysNothing(harness, sheira, Asylum, 2);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, sheira, Asylum, 2, L.Experience, L.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Asylum), "MissionRewarded once");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, L.HazmatGloves), "the gloves chosen");
            for (var tick = 0; tick < 4; tick++)        // RetireRewardedScene retries: up to 1 s
                Tick(harness, copy, 250);
            Assert.IsNull(FindSceneObject(harness, copy, Asylum, L.CorpseRole), "the body goes with the mission");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, sheira, Asylum), "done once");
        }

        /// <summary>An abandon of Lost in the Asylum after the body and Shanul are heard: taken again, both must be heard again.</summary>
        [TestMethod]
        public void LostInTheAsylumAbandonedStartsOver()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            C.SeedCompleted(harness, GameWardens, Sabotage);
            C.EnterCrucible(harness);
            var copy = IntoWard(harness);
            var sheira = Giver(copy, I.Sheira);
            Accept(harness, sheira, Asylum);
            Tick(harness, copy, 250);
            var body = Body(harness, copy);
            AssertTalks(harness, body, L.FindPaliv);
            TalkTo(harness, body, L.FindPaliv);
            Talk(harness, Giver(copy, I.Shanul), Asylum, L.FindShanul);

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Asylum));
            harness.Drain();
            Accept(harness, sheira, Asylum);
            Tick(harness, copy, 250);
            Assert.AreEqual((MissionObjectiveState.Incomplete, MissionObjectiveState.Incomplete, MissionObjectiveState.Incomplete),
                (Objective(harness, Asylum, L.FindShanul), Objective(harness, Asylum, L.FindPaliv), Objective(harness, Asylum, L.FindDhimand)), "all three again");
            AssertTalks(harness, Body(harness, copy), L.FindPaliv);
        }

        /// <summary>
        /// Rehabilitation at Gunpoint: Tempa offers it on the Crucible (no prerequisite); Jaikev killed before the accept is not
        /// counted; in the squad's Ward 29 Incurables of the two classes leave 3 open, the 30th completes it (the bosses' own
        /// classes do not count); Jaikev and Parvatee each complete theirs; a reload keeps the counts; back on the Crucible a full
        /// bag waits, a failed save pays nothing; Tempa pays one of the four modules, once.
        /// </summary>
        [TestMethod]
        public void RehabilitationAtGunpointThinsOutTheIncurables()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var crucible = C.EnterCrucible(harness, ToVector(I.TempaAt));
            var tempa = Npc(crucible, I.Tempa);
            var offer = WildernessRanjaGorgeTests.Offer(harness, tempa, Rehab);
            CollectionAssert.AreEqual(new[] { R.HealthBonus, R.ResistPhysical, R.ResistPhotonic, R.StealPower },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());

            var copy = IntoWard(harness);
            Stand(copy, I.JaikevPool);
            Kill(harness, copy, I.Jaikev);
            harness.Drain();
            harness.Manager.Scenes.Detach(harness.Client, copy);
            crucible = C.EnterCrucible(harness, ToVector(I.TempaAt));
            tempa = Npc(crucible, I.Tempa);
            Accept(harness, tempa, Rehab);
            Assert.AreEqual((MissionObjectiveState.Incomplete, MissionObjectiveState.Incomplete, MissionObjectiveState.Incomplete),
                (Objective(harness, Rehab, R.KillJaikev), Objective(harness, Rehab, R.KillParvatee), Objective(harness, Rehab, R.Execute)),
                "a kill before the accept is not counted");
            Assert.AreEqual(0U, Counter(harness, Rehab, R.Execute));

            copy = IntoWard(harness);
            Tick(harness, copy, 250);
            var killed = KillIncurables(harness, copy, 29);
            Assert.AreEqual(29, killed);
            Assert.AreEqual((29U, MissionObjectiveState.Incomplete), (Counter(harness, Rehab, R.Execute), Objective(harness, Rehab, R.Execute)), "29 of 30");
            ReloadAndRecover(harness, copy);
            Assert.AreEqual(29U, Counter(harness, Rehab, R.Execute), "kept at a reload");

            Stand(copy, I.ParvateePool);
            Kill(harness, copy, I.Parvatee);
            harness.Drain();
            Assert.AreEqual((MissionObjectiveState.Completed, 29U), (Objective(harness, Rehab, R.KillParvatee), Counter(harness, Rehab, R.Execute)),
                "Parvatee's own class does not count");
            KillIncurables(harness, copy, 1);
            Assert.AreEqual((30U, MissionObjectiveState.Completed), (Counter(harness, Rehab, R.Execute), Objective(harness, Rehab, R.Execute)), "the 30th");
            Assert.IsFalse(harness.Client.Player.Missions[Rehab].Completeable, "Jaikev still to die");
            Stand(copy, I.JaikevPool);
            Kill(harness, copy, I.Jaikev);
            harness.Drain();
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Rehab, R.KillJaikev));
            Assert.IsTrue(harness.Client.Player.Missions[Rehab].Completeable);

            harness.Manager.Scenes.Detach(harness.Client, copy);
            crucible = C.EnterCrucible(harness, ToVector(I.TempaAt));
            tempa = Npc(crucible, I.Tempa);
            FullBagWaits(harness, tempa, Rehab, 3, ModuleFiller);
            FailedTurnInPaysNothing(harness, tempa, Rehab, 3);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, tempa, Rehab, 3, R.Experience, R.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Rehab), "MissionRewarded once");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, R.StealPower), "the module chosen");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, tempa, Rehab), "done once");
        }

        /// <summary>An abandon of Rehabilitation at Gunpoint with 5 Incurables and Parvatee dead: taken again, the count starts at 0.</summary>
        [TestMethod]
        public void RehabilitationAtGunpointAbandonedStartsTheCountOver()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var crucible = C.EnterCrucible(harness, ToVector(I.TempaAt));
            Accept(harness, Npc(crucible, I.Tempa), Rehab);
            var copy = IntoWard(harness);
            Tick(harness, copy, 250);
            KillIncurables(harness, copy, 5);
            Stand(copy, I.ParvateePool);
            Kill(harness, copy, I.Parvatee);
            harness.Drain();
            Assert.AreEqual((5U, MissionObjectiveState.Completed), (Counter(harness, Rehab, R.Execute), Objective(harness, Rehab, R.KillParvatee)));

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Rehab));
            harness.Drain();
            harness.Manager.Scenes.Detach(harness.Client, copy);
            crucible = C.EnterCrucible(harness, ToVector(I.TempaAt));
            Accept(harness, Npc(crucible, I.Tempa), Rehab);
            Assert.AreEqual((0U, MissionObjectiveState.Incomplete), (Counter(harness, Rehab, R.Execute), Objective(harness, Rehab, R.KillParvatee)), "all over again");
        }

        /// <summary>
        /// In the squad's copy, Jaikev and Parvatee come back on their pools' own 90 s (1402's scene names them public-spawn
        /// actors), not after the copy's 45 minutes: not back at 80 s, back by 100 s. An Incurable pool (not named) is not back at 100 s.
        /// </summary>
        [TestMethod]
        public void JaikevAndParvateeComeBackOnTheirOwnTimeInTheCopy()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            C.EnterCrucible(harness);
            var copy = IntoWard(harness);
            Assert.IsNotNull(copy.SquadState, "a squad's copy");
            foreach (var pool in new[] { I.JaikevPool, I.ParvateePool })
                Assert.IsTrue(SpawnPoolManager.Instance.IsNamedPublicSpawn(pool), $"pool {pool} named by a scene");
            foreach (var pool in IncurablePools)
                Assert.IsFalse(SpawnPoolManager.Instance.IsNamedPublicSpawn(pool), $"pool {pool} not named");
            var pools = new[] { I.JaikevPool, I.ParvateePool, IncurablePools[0] };
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
            Assert.IsFalse(Standing(I.JaikevPool) || Standing(I.ParvateePool), "not back at 80 s");
            Run(20);
            Assert.IsTrue(Standing(I.JaikevPool), "Jaikev back by 100 s");
            Assert.IsTrue(Standing(I.ParvateePool), "Parvatee back by 100 s");
            Assert.IsFalse(Standing(IncurablePools[0]), "the Incurables come back with the copy");
        }

        /// <summary>Into the squad's copy of the Ward through its door on the Crucible, its pools back as after their time, the scenes resumed.</summary>
        internal static MapChannel IntoWard(WildernessRuntimeTestHarness harness)
        {
            if (!harness.Maps.MapChannelArray.ContainsKey(Ward))
                LoadMap(harness, Ward);
            var copy = ThroughTheDoor(harness, Ward);
            foreach (var pool in copy.SpawnPools.Where(pool => WardPools.Contains(pool.DbId) || CrucibleBloodFireSteelTests.Pools.Contains(pool.DbId)))
                pool.ClearedAtUtcMs = 0;
            // A squad copy outlives a test: an earlier test's 1398 Dhimand copy or wave (a run this test's database does not hold)
            // would still hide the public Dhimand (MissionNpcVisibility, ReplaceWhilePresent). Its pool goes (as IntoChaukas).
            // So does 1401's Headsman group.
            foreach (var mission in new[] { CrucibleLeakyPipesV1.MissionId, CrucibleBloodFireSteelV1.MissionId })
            {
                var runs = Runs(harness, mission);
                copy.SpawnPools.RemoveAll(pool => pool.SceneRunId != null && pool.ScenarioMissionId == mission && !runs.Contains(pool.SceneRunId));
            }
            harness.Manager.Scenes.Resume(harness.Client);
            harness.Drain();
            return copy;
        }

        /// <summary>
        /// The navmesh walk from the arrival to <paramref name="to"/>: direct on the arrival's island, or (north) direct if the
        /// navmesh joins the airlock (the local N15 rebake), else to the airlock's south end, straight across its gap (upstream
        /// N13), and on from its north end.
        /// </summary>
        internal static List<Vector3> WalkFromArrival(NavMeshQuery nav, Vector3 to, bool north, string label)
        {
            var start = nav.Nearest(Arrival) ?? Arrival;
            var end = nav.Nearest(to) ?? to;
            if (!north)
            {
                var path = nav.FindPath(start, end, out var complete);
                Assert.IsTrue(path != null && complete, $"{label}: a walk from the arrival");
                return path.Prepend(start).ToList();
            }
            var direct = nav.FindPath(start, end, out var whole);
            if (direct != null && whole)
                return direct.Prepend(start).ToList();
            var south = nav.FindPath(start, nav.Nearest(AirlockSouth).Value, out var southComplete);
            Assert.IsTrue(south != null && southComplete, $"{label}: a walk from the arrival to the airlock");
            var from = nav.Nearest(AirlockNorth).Value;
            var on = nav.FindPath(from, end, out var onComplete);
            Assert.IsTrue(on != null && onComplete, $"{label}: a walk from the airlock");
            return south.Prepend(start).Concat(on.Prepend(from)).ToList();
        }

        /// <summary>The navmesh walk back from <paramref name="from"/> to the arrival (north: direct if the airlock is joined, else to its north end, across, on from its south end).</summary>
        internal static List<Vector3> WalkToArrival(NavMeshQuery nav, Vector3 from, bool north, string label)
        {
            var start = nav.Nearest(from) ?? from;
            var end = nav.Nearest(Arrival) ?? Arrival;
            if (!north)
            {
                var path = nav.FindPath(start, end, out var complete);
                Assert.IsTrue(path != null && complete, $"{label}: a walk back to the arrival");
                return path.Prepend(start).ToList();
            }
            var direct = nav.FindPath(start, end, out var whole);
            if (direct != null && whole)
                return direct.Prepend(start).ToList();
            var to = nav.FindPath(start, nav.Nearest(AirlockNorth).Value, out var toComplete);
            Assert.IsTrue(to != null && toComplete, $"{label}: a walk back to the airlock");
            var south = nav.Nearest(AirlockSouth).Value;
            var on = nav.FindPath(south, end, out var onComplete);
            Assert.IsTrue(on != null && onComplete, $"{label}: a walk from the airlock back to the arrival");
            return to.Prepend(start).Concat(on.Prepend(south)).ToList();
        }

        /// <summary>The boss of <paramref name="pool"/> standing in the copy: spawned, or back after its own time if an earlier kill left it dead.</summary>
        internal static void Stand(MapChannel copy, uint pool)
        {
            Spawn(copy, pool);
            if (!Here(copy).Any(one => one.SpawnPool?.DbId == pool && one.State != CharacterState.Dead))
                Respawn(copy, pool);
            Assert.IsTrue(Here(copy).Any(one => one.SpawnPool?.DbId == pool && one.State != CharacterState.Dead), $"pool {pool} stands");
        }

        /// <summary><paramref name="count"/> Incurables of the pools killed by the player, Brutes and Slayers in turn; how many died.</summary>
        private static int KillIncurables(WildernessRuntimeTestHarness harness, MapChannel copy, int count)
        {
            Spawn(copy, IncurablePools);
            var killed = 0;
            while (killed < count)
            {
                var kind = killed % 2 == 0 ? I.Slayer : I.Brute;
                var one = Here(copy).FirstOrDefault(creature => creature.DbId == kind && IncurablePools.Contains(creature.SpawnPool?.DbId ?? 0)) ??
                          Here(copy).FirstOrDefault(creature => (creature.DbId == I.Brute || creature.DbId == I.Slayer) && IncurablePools.Contains(creature.SpawnPool?.DbId ?? 0));
                Assert.IsNotNull(one, $"an Incurable left after {killed}");
                Kill(harness, copy, one);
                killed++;
            }
            return killed;
        }

        private static uint Counter(WildernessRuntimeTestHarness harness, uint mission, uint objective) =>
            harness.Client.Player.Missions[mission].Objectives[objective].Counters[0];

        /// <summary>Paliv's body: the player's own, of the client's class and package, at its place, on the ground.</summary>
        private static DynamicObject Body(WildernessRuntimeTestHarness harness, MapChannel copy)
        {
            harness.Manager.Scenes.Tick(copy);
            var body = SceneObject(harness, copy, Asylum, L.CorpseRole);
            Assert.AreEqual(L.CorpseClass, (uint)body.EntityClassId);
            Assert.AreEqual(ToVector(L.CorpseAt), body.Position);
            Assert.AreEqual(L.CorpsePackage, body.MissionConversation.NpcPackageId);
            Assert.AreEqual(harness.Client.Player.Id, body.SceneOwnerCharacterId, "the player's own");
            AssertOnGround(copy, body.Position, "Paliv's body");
            return body;
        }

        /// <summary>The body talks this objective, and only it.</summary>
        private static void AssertTalks(WildernessRuntimeTestHarness harness, DynamicObject body, uint objective)
        {
            var talk = OpenBody(harness, body);
            Assert.IsNotNull(talk, "the body talks");
            Assert.IsTrue(talk.ConvoDataDict.TryGetValue(ConversationType.ObjectiveComplete, out var topics));
            Assert.AreEqual(((int)Asylum, (int)objective, 1),
                ((List<CompleteableObjectives>)topics).Select(topic => (topic.MissionId, topic.ObjectiveId, topic.PlayerFlagId)).Single());
        }

        /// <summary>The body's objective done (the client opens its conversation first: AssertTalks); the packets it sent.</summary>
        private static List<object> TalkTo(WildernessRuntimeTestHarness harness, DynamicObject body, uint objective)
        {
            harness.MoveTo(body.Position + new Vector3(0, 0, 1));
            harness.Drain();
            new NpcManager(harness, harness.Manager).CompleteNPCObjective(harness.Client, new CompleteNPCObjectivePacket
            {
                EntityId = body.EntityId, MissionId = Asylum, ObjectiveId = objective, PlayerFlagId = 1
            });
            var read = harness.Drain().Cast<object>().ToList();
            Assert.AreEqual(1, read.OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == Asylum && packet.ObjectiveId == objective),
                $"{Asylum}.{objective} ObjectiveCompleted once");
            return read;
        }

        /// <summary>The body's conversation, as CruciblePrometheusOutpostTests.OpenTerminal.</summary>
        private static ConversePacket OpenBody(WildernessRuntimeTestHarness harness, DynamicObject body)
        {
            harness.MoveTo(body.Position + new Vector3(0, 0, 1));
            harness.Drain();
            new NpcManager(harness, harness.Manager).RequestNpcConverse(harness.Client,
                new RequestNPCConversePacket { EntityId = body.EntityId, ActionId = ActionId.Converse, ActionArgId = 1 });
            return harness.Drain().OfType<ConversePacket>().LastOrDefault();
        }
    }
}
