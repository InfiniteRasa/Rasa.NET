using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.ClientData;
using Rasa.Data;
using Rasa.Game;
using Rasa.Managers;
using Rasa.Missions.Scenes;
using Rasa.Packets.Mission.Server;
using Rasa.Services.Preloader.Missions.Ashen;
using Rasa.Services.Preloader.Missions.Crucible;
using Rasa.Structures;
using Rasa.Structures.World;
using Rasa.Test.Missions.Marshes;
using Rasa.Test.Missions.Plateau;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions.Ashen
{
    using static MarshesFalconHoldTests;
    using static MarshesPaludosTests;
    using static MarshesPreoDasTests;
    using static PlateauCampResistanceTests;
    using N = AshenGothaPlantNpcsV1;
    using F15 = AshenIndraFacilityNpcsV1;
    using Digging = AshenDiggingForTheTruthV1;
    using Meet = AshenMeetSergeantDrofV1;
    using Subversion = AshenSubversionV1;
    using Desistance = AshenDesistanceV1;
    using Facility = AshenIndraFacilityTests;
    using F = MarshesClientFloorTests;
    using WO = AshenWhiteOasisTests;

    /// <summary>
    /// Ashen Desert set 16, Indra Caverns (squad instance 2055) and the Gotha Processing Plant, played through as a player plays
    /// it: Drof's Digging for the Truth (1447: the Power Supply Box, the teleporters to the rear exit, the rear caves, the
    /// tunnel, six Articulated Drills; by radio), Meet Sergeant Drof (1532: by radio, Drof at the plant's entrance), Drof's
    /// Subversion (1451: two valves, four life support systems; turned in at Drof) and Naimi's Desistance (1452: the bay's door console, a
    /// Juggernaut console, the landing pad, the generator bombed, Central Administration, Overlord Kretas, his hand on his
    /// teleporter, Major Diren). With them the set's places and walks on the client's floor.
    ///
    /// Why these tests: the set gives each squad member his own power supply, bomb and hand and turns each member's own power,
    /// door and teleporters on; a member left behind a shut door or without a key, an item given twice or kept after its use, an
    /// object that can be used before its time, or a move that sends a member elsewhere would each break it.
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class AshenGothaPlantTests
    {
        private const uint Map = N.Caverns;
        private const uint WeaponFiller = 3869;            // inventory category 1 (quest-test-pattern)

        /// <summary>The set's missions.</summary>
        internal static readonly uint[] Missions = { Digging.MissionId, Meet.MissionId, Subversion.MissionId, Desistance.MissionId };

        /// <summary>The pools set 16 adds.</summary>
        internal static readonly uint[] Pools = { N.Drof, N.Naimi, N.KretasPool };

        /// <summary>The client's names of the set's console switches (UsableTwoStateBaneConsoleSwitchV01 7468, usablenameoverride).</summary>
        internal static readonly Dictionary<string, uint> SwitchNames = new()
        {
            [Digging.BoxRole] = Digging.BoxName,
            [Subversion.ValveRole(1)] = Subversion.ValveName, [Subversion.ValveRole(2)] = Subversion.ValveName,
            [Subversion.SupportRole(1)] = Subversion.SupportName, [Subversion.SupportRole(2)] = Subversion.SupportName,
            [Subversion.SupportRole(3)] = Subversion.SupportName, [Subversion.SupportRole(4)] = Subversion.SupportName,
            [Desistance.DoorConsoleRole] = Desistance.DoorConsoleName, [Desistance.ProgrammingRole] = Desistance.ProgrammingName,
            [Desistance.SystemsRole] = Desistance.SystemsName, [Desistance.ConsoleRole] = Desistance.TeleportName,
            // Set 20: 1499's Cell Door Control (usablenameoverridelanguage 140).
            [AshenIntoTheLionsDenV1.SwitchRole] = AshenIntoTheLionsDenV1.SwitchName
        };

        private Func<MissionApplication> _missions;

        [TestInitialize]
        public void Clock()
        {
            PlateauGiantKillerSquadTests.UseClock();
            _missions = SpawnPoolClearedProgress.Missions;
        }

        [TestCleanup]
        public void Restore()
        {
            PlateauGiantKillerSquadTests.RestoreClock();
            SpawnPoolClearedProgress.Missions = _missions;
        }

        /// <summary>The set's markers: mission, objective, x, z, radius (AshenMissionTests.TheMarkersStandAtTheirObjectivesTargets).</summary>
        internal static IEnumerable<(uint Mission, uint Objective, double X, double Z, double Radius)> Markers()
        {
            yield return (1447, Digging.DestroyDrills, AshenFixJV1.MiningMarkerAt.X, AshenFixJV1.MiningMarkerAt.Z, Digging.MiningMarkerRadius);   // fix J's place
            yield return (1447, Digging.RearCaveEntrance, Digging.EntranceAt.X, Digging.EntranceAt.Z, Digging.EntranceRadius);
            yield return (1447, Digging.ExploreTunnel, Digging.TunnelExitAt.X, Digging.TunnelExitAt.Z, Digging.TunnelExitRadius);
            yield return (1447, Digging.RestorePower, Digging.BoxAt.X, Digging.BoxAt.Z, Digging.ObjectMarkerRadius);
            for (var number = 1; number <= Digging.Drills; number++)
                yield return (1447, Digging.DrillObjective(number), DrillAt(number).X, DrillAt(number).Z, Digging.ObjectMarkerRadius);
            yield return (1532, Meet.Proceed, N.DrofAt.X, N.DrofAt.Z, Meet.NpcMarkerRadius);
            yield return (1451, Subversion.Valves, Subversion.ValvesMarkerAt.X, Subversion.ValvesMarkerAt.Z, Subversion.RoomMarkerRadius);
            yield return (1451, Subversion.LifeSupport, Subversion.BarracksMarkerAt.X, Subversion.BarracksMarkerAt.Z, Subversion.RoomMarkerRadius);
            for (var number = 1; number <= Subversion.ValveCount; number++)
                yield return (1451, Subversion.ValveObjective(number), Subversion.ValvesAt[number - 1].X, Subversion.ValvesAt[number - 1].Z, Subversion.ObjectMarkerRadius);
            for (var number = 1; number <= Subversion.SupportCount; number++)
                yield return (1451, Subversion.SupportObjective(number), Subversion.SupportsAt[number - 1].X, Subversion.SupportsAt[number - 1].Z, Subversion.ObjectMarkerRadius);
            yield return (1452, Desistance.BayDoors, Desistance.DoorConsoleAt.X, Desistance.DoorConsoleAt.Z, Desistance.ObjectMarkerRadius);
            yield return (1452, Desistance.Juggernauts, Desistance.OperationsMarkerAt.X, Desistance.OperationsMarkerAt.Z, Desistance.AreaMarkerRadius);
            yield return (1452, Desistance.Generator, AshenFixJV1.GeneratorMarkerAt.X, AshenFixJV1.GeneratorMarkerAt.Z, Desistance.ObjectMarkerRadius);
            yield return (1452, Desistance.LandingPad, Desistance.PadAt.X, Desistance.PadAt.Z, Desistance.PadRadius);
            yield return (1452, Desistance.Administration, Desistance.AdministrationAt.X, Desistance.AdministrationAt.Z, Desistance.AdministrationRadius);
            yield return (1452, Desistance.DefeatKretas, N.KretasAt.X, N.KretasAt.Z, Desistance.BossMarkerRadius);
            yield return (1452, Desistance.Teleporter, Desistance.ConsoleAt.X, Desistance.ConsoleAt.Z, Desistance.ObjectMarkerRadius);
            yield return (1452, Desistance.ReportToDiren, F15.DirenAt.X, F15.DirenAt.Z, Desistance.NpcMarkerRadius);
        }

        /// <summary>
        /// The rows of the four missions: givers and receivers (1447 by radio, 1532 offered by radio, 1451 turned in at Drof), levels, the Ashen
        /// category, the client's texts, objectives and talks (1532,1 package 1544 Drof, 1452,8 package 1802 Diren), the chain
        /// 1395, 1447, 1532, 1451, 1452; the radio channels; every marker on Indra Caverns; each mission operational.
        /// </summary>
        [TestMethod]
        public void TheSetsRowsAreTheClients()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            const string revision = AshenMissionDataV1.Revision;
            var expected = new (uint Mission, uint? Giver, uint? Receiver, uint Level, uint Name, (uint, uint?)[] Talks, uint Prerequisite, bool Radio)[]
            {
                (Digging.MissionId, F15.Drof, null, 43, 12708, Array.Empty<(uint, uint?)>(), AshenVindicationV1.MissionId, true),
                (Meet.MissionId, null, N.Drof, 43, 13691, new (uint, uint?)[] { (1, 1544) }, Digging.MissionId, false),
                (Subversion.MissionId, N.Drof, N.Drof, 44, 12889, Array.Empty<(uint, uint?)>(), Meet.MissionId, false),
                (Desistance.MissionId, N.Naimi, F15.Diren, 44, 12902, new (uint, uint?)[] { (8, 1802) }, Subversion.MissionId, false)
            };
            var catalog = new MissionContentCatalog(harness, new Dictionary<uint, Mission>(), null);
            catalog.Load();
            foreach (var (mission, giver, receiver, level, name, talks, prerequisite, radio) in expected)
            {
                var definition = world.MissionContentDefinitionEntries.AsNoTracking().Single(entry => entry.MissionId == mission && entry.ContentRevision == revision);
                Assert.AreEqual((giver, receiver, (uint?)level, (uint?)AshenMissionDataV1.CategoryId, name, true, radio),
                    (definition.GiverId, definition.ReceiverId, definition.Level, definition.CategoryId, definition.ClientNameTextId, definition.Enabled,
                        definition.RadioCompleteable), $"{mission}");
                var talkRows = world.Set<MissionTriggerEntry>().AsNoTracking()
                    .Where(entry => entry.MissionId == mission && entry.ContentRevision == revision && entry.Kind == MissionTriggerKind.Conversation)
                    .AsEnumerable().Select(entry => (entry.ObjectiveId, entry.NpcPackageId)).OrderBy(entry => entry.ObjectiveId).ToArray();
                CollectionAssert.AreEqual(talks, talkRows, $"{mission}'s talks");
                Assert.AreEqual((uint?)prerequisite, world.MissionPrerequisiteEntries.AsNoTracking()
                    .Single(entry => entry.MissionId == mission && entry.ContentRevision == revision).RequiredMissionId, $"{mission}'s prerequisite");
                Assert.IsTrue(harness.Manager.LoadedMissions[mission].IsOperational, harness.Manager.LoadedMissions[mission].OperationalDiagnostic);
            }

            var markers = world.MissionIndicatorEntries.AsNoTracking()
                .Where(entry => entry.ContentRevision == revision && Missions.Contains(entry.MissionId)).AsEnumerable()
                .Select(entry => (entry.MissionId, entry.ObjectiveId, entry.IndicatorId,
                    Map: MissionMarkerProjection.ResolveMap(catalog, entry.MissionId, entry.ObjectiveId, new MissionIndicator { IndicatorId = entry.IndicatorId })))
                .ToArray();
            Assert.HasCount(10 + 1 + 8 + 8, markers, "the set's markers");
            foreach (var (mission, objective, indicator, map) in markers)
            {
                Assert.AreEqual(Map, map, $"{mission}.{objective}: on Indra Caverns");
                Assert.AreEqual(AshenMissionTests.NamedMarker(mission, objective), (uint?)indicator, $"{mission}.{objective}: the client's name");
            }
        }

        /// <summary>
        /// The set's creatures in the id block 790600-790639: Drof's and Naimi's second rows friendly with the client's names and
        /// packages (9427/1544, 9530/1651); Kretas hostile, the Overseers' class 10504 (no client IMMUNE_* damage flag,
        /// creature_class_flag 125-135), level 44, 4900 hit points, the client's name 9407, alone in his pool (one, radius 0, 300 s).
        /// Kretas' pool stands 35 m or more outside every other hostile pool's circle (fix F's boss rule; the 60 m rule broken in
        /// Central Administration, docs/ashen-playtest.md) and 100 m or more from every pool of a creature with the warcry.
        /// </summary>
        [TestMethod]
        public void TheSetsCreaturesCanBeBeatenAndStandApart()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            var rows = world.CreatureEntries.AsNoTracking().Where(row => row.Id >= 790600 && row.Id <= 790639).ToList();
            CollectionAssert.AreEquivalent(new[] { N.Drof, N.Naimi, N.Kretas }, rows.Select(row => row.Id).ToArray(), "the three");
            foreach (var (id, name, package) in new[] { (N.Drof, 9427U, 1544U), (N.Naimi, 9530U, 1651U) })
            {
                var row = rows.Single(entry => entry.Id == id);
                Assert.AreEqual((1U, name), ((uint)row.Faction, row.NameId), $"{id}: friendly, the client's name");
                Assert.AreEqual(package, world.NpcPackageEntries.AsNoTracking().Single(entry => entry.Id == id).PackageId, $"{id}'s package");
            }
            var kretas = rows.Single(row => row.Id == N.Kretas);
            Assert.AreEqual((0U, 10504U, 44U, 9407U, 4900U), ((uint)kretas.Faction, kretas.ClassId, kretas.Level, kretas.NameId, kretas.MaxHitPoints),
                "Kretas: hostile, the Overseers' class, 44, the client's name");
            var immune = new uint[] { 125, 126, 127, 128, 129, 130, 131, 132, 133, 134, 135 };
            CollectionAssert.AreEqual(Array.Empty<uint>(), world.CreatureClassFlagEntries.AsNoTracking().Where(flag => flag.ClassId == kretas.ClassId)
                .Select(flag => flag.FlagId).AsEnumerable().Where(immune.Contains).ToArray(), "no damage immunity");

            var pools = world.SpawnPoolEntries.AsNoTracking().Where(pool => pool.Id >= 790600 && pool.Id <= 790639).ToList();
            CollectionAssert.AreEquivalent(Pools, pools.Select(pool => pool.Id).ToArray(), "the set's pools");
            Assert.IsTrue(pools.All(pool => pool.MapContextId == Map), "all on Indra Caverns");
            var own = pools.Single(entry => entry.Id == N.KretasPool);
            Assert.AreEqual((N.Kretas, (byte)1, (byte)1, 3000U, 0.0, 0U), (own.Creature1Id, own.Creature1MinCount, own.Creature1MaxCount, own.RespawnTime, own.Radius,
                own.Creature2Id), "Kretas alone");
            var hostile = Facility.HostilePools(harness);
            var nearest = hostile.Where(other => other.Id != N.KretasPool)
                .Min(other => Vector2.Distance(new((float)own.PosX, (float)own.PosZ), new((float)other.PosX, (float)other.PosZ)) - (float)other.Radius);
            Assert.IsGreaterThanOrEqualTo(35f, nearest, "Kretas 35 m+ outside every other hostile pool's circle");
            var criers = AshenFixFTests.WarcryCreatures(harness);
            foreach (var cry in world.SpawnPoolEntries.AsNoTracking().Where(pool => pool.MapContextId == Map).AsEnumerable()
                         .Where(pool => new[] { pool.Creature1Id, pool.Creature2Id, pool.Creature3Id, pool.Creature4Id, pool.Creature5Id, pool.Creature6Id }.Any(criers.Contains)))
                Assert.IsGreaterThan(100f, Vector2.Distance(new((float)own.PosX, (float)own.PosZ), new((float)cry.PosX, (float)cry.PosZ)), $"warcry pool {cry.Id}");
        }

        /// <summary>
        /// In the squad's copy: Drof and Naimi at the plant's entrance come out friendly, the player can talk to them; Kretas (a
        /// named public-spawn actor of 1452's scene: he comes back on his own time) hostile, of his kind, level and hit points;
        /// each on the navmesh's ground.
        /// </summary>
        [TestMethod]
        public void TheSetsCreaturesStandInTheSquadsCopy()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var copy = IntoGotha(harness);
            Assert.IsTrue(SpawnPoolManager.Instance.IsNamedPublicSpawn(N.KretasPool), "Kretas named by 1452's scene");
            Spawn(copy, Pools);
            foreach (var id in new[] { N.Drof, N.Naimi })
            {
                var npc = Here(copy).Single(one => one.SpawnPool?.DbId == id);
                Assert.AreEqual((id, TargetCategory.Friendly), (npc.DbId, npc.TargetCategory), $"{id}");
                Assert.IsNotNull(npc.Npc, $"{id} talks");
                AssertOnGround(copy, npc.Position, $"{id}");
            }
            var kretas = Here(copy).Single(one => one.SpawnPool?.DbId == N.KretasPool);
            Assert.AreEqual((N.Kretas, N.KretasLevel, TargetCategory.Hostile, N.KretasClass, (int)N.KretasHitPoints),
                (kretas.DbId, (uint)kretas.Level, kretas.TargetCategory, (uint)kretas.EntityClass, (int)kretas.Attributes[Attributes.Health].NormalMax));
            AssertOnGround(copy, kretas.Position, "Kretas");
        }

        /// <summary>
        /// The set's places on the client's floor: each on the navmesh's ground (within 1 m) and a client floor (within 1 m), with
        /// no client surface over its head within 2.2 m; the NPCs, Kretas and the objects one uses or shoots seen from all 16 places
        /// 2 m round it (line of sight: to-fix row 26); each its own clearance or more outside every hostile pool's circle (its own
        /// left out; a pool on another floor, 12 m and more above or below, in 3D): 34 m, less inside the Gotha plant where its
        /// rooms hold the plant's own pools (each written down in docs/ashen-playtest.md). The markers on the navmesh and a client
        /// floor within 2 m (the drills' mining marker within 4 m of a floor: it marks the cavern).
        /// </summary>
        [TestMethod]
        public void ThePlacesStandOnTheClientsFloor()
        {
            var dataDir = F.ClientData();
            using var harness = WildernessRuntimeTestHarness.Create();
            var copy = IntoGotha(harness);
            var nav = copy.NavMesh;
            using var meshes = new MeshLibrary(dataDir, F.MeshCsv());
            var geometry = MapGeometry.Load(Path.Combine(dataDir, "maps", Facility.MapName(harness, Map)), meshes, 1);
            var hostile = Facility.HostilePools(harness);
            var places = Places();
            var near = F.Near(geometry, places.Select(place => place.At).ToList(), 12);
            var failures = new List<string>();
            for (var i = 0; i < places.Count; i++)
            {
                var (label, at, ring, middle, own, clearance) = places[i];
                if (nav.GroundHeight(at + new Vector3(0, 1, 0)) is not { } ground || MathF.Abs(ground - at.Y) > 1f)
                    failures.Add(FormattableString.Invariant($"{label}: the navmesh ground {nav.GroundHeight(at + new Vector3(0, 1, 0))} at {at}"));
                if (F.FloorAt(near[i], at, at.Y - 2, at.Y + 1.5f) is not { } floor || MathF.Abs(floor - at.Y) > 1)
                {
                    failures.Add(FormattableString.Invariant($"{label}: the client's floor at {at}"));
                    continue;
                }
                if (near[i].Any(t => F.Hit(new Vector3(at.X, floor + 0.3f, at.Z), Vector3.UnitY, t.A, t.B, t.C) is { } up && up < 2.2f))
                    failures.Add($"{label}: a client surface over its head");
                if (ring)
                {
                    var seen = 0;
                    for (var k = 0; k < F.RingPlaces; k++)
                    {
                        var angle = k * 2 * MathF.PI / F.RingPlaces;
                        var place = at + new Vector3(MathF.Sin(angle) * F.Ring, 0, MathF.Cos(angle) * F.Ring);
                        if (F.FloorAt(near[i], place, at.Y - F.MaxOver, at.Y + F.MaxUnder) is { } y && F.Clear(near[i], place with { Y = y + F.Eye }, at + new Vector3(0, middle, 0)))
                            seen++;
                    }
                    if (seen < F.RingPlaces)
                        failures.Add($"{label}: seen from {seen} of {F.RingPlaces} ring places");
                }
                foreach (var pool in hostile.Where(pool => pool.Id != own))
                    if (Facility.Gap(at, pool) < clearance)
                        failures.Add(FormattableString.Invariant($"{label}: {Facility.Gap(at, pool):0.0} m outside hostile pool {pool.Id}'s circle (its rule {clearance})"));
            }

            var markers = harness.World.MissionIndicatorEntries.AsNoTracking()
                .Where(entry => entry.ContentRevision == AshenMissionDataV1.Revision && Missions.Contains(entry.MissionId)).AsEnumerable()
                .Select(marker => (Label: $"{marker.MissionId}.{marker.ObjectiveId}/{marker.IndicatorId}", At: new Vector3((float)marker.PosX, (float)marker.PosY, (float)marker.PosZ),
                    Reach: marker.MissionId == Digging.MissionId && marker.ObjectiveId == Digging.DestroyDrills ? 4f : 2f))
                .ToList();
            Assert.HasCount(10 + 1 + 8 + 8, markers, "the set's markers");
            var (markerFloors, _) = F.Surfaces(geometry, markers.Select(marker => (marker.At + new Vector3(0, 2 - F.MaxUnder, 0), F.Walkable)).ToList());
            for (var i = 0; i < markers.Count; i++)
            {
                if (nav.GroundHeight(markers[i].At + new Vector3(0, 2, 0)) is not { } y || MathF.Abs(y - markers[i].At.Y) > markers[i].Reach)
                    failures.Add($"{markers[i].Label}: navmesh {nav.GroundHeight(markers[i].At + new Vector3(0, 2, 0))}");
                if (markers[i].At.Y - markerFloors[i] > markers[i].Reach)
                    failures.Add($"{markers[i].Label}: client floor {markerFloors[i]}");
            }
            Assert.HasCount(28, places);
            Assert.IsEmpty(failures, $"{failures.Count}: " + string.Join("; ", failures));
        }

        /// <summary>
        /// The walks on the client's floor (AshenFixBTests.ClientFloorLine with a player's half width, fix G's 55-degree climb
        /// rule), every leg there and back by the corners Legs names (a client-floor flood's way, 2026-11-11). Every walk keeps
        /// HostileClearance (30 m, 2D) outside the circle of every hostile pool of the caverns but those it names (the upstream
        /// pools on the only way: the tunnel's and the plant's rooms, written down in docs/ashen-playtest.md). The walk out of
        /// the facility crosses the rear exit door's leaf.
        /// </summary>
        [TestMethod]
        public void ThePlacesAreWalkedToOnTheClientsFloor()
        {
            var dataDir = F.ClientData();
            using var harness = WildernessRuntimeTestHarness.Create();
            using var meshes = new MeshLibrary(dataDir, F.MeshCsv());
            var geometry = MapGeometry.Load(Path.Combine(dataDir, "maps", Facility.MapName(harness, Map)), meshes, 1);
            var hostile = Facility.HostilePools(harness);
            var failures = new List<string>();
            var walked = 0;
            foreach (var (leg, corners) in Legs)
            {
                var there = corners.ToList();
                var back = Enumerable.Reverse(there).ToList();
                foreach (var (line, direction) in new[] { (there, "there"), (back, "back") })
                {
                    walked++;
                    if (KnownGaps.Contains(leg))
                        continue;
                    if (AshenFixBTests.ClientFloorLine(geometry, line, line[^1], null, WalkHalfWidth, rampFoot: true) is { } why)
                        failures.Add($"{leg} {direction}: {why}");
                }
            }
            foreach (var (label, legs, passes, target) in Walks())
            {
                var line = Line(legs);
                if (WO.Clearance(line, hostile.Where(pool => !passes.Contains(pool.Id)).ToList()) is { } close)
                    failures.Add(FormattableString.Invariant($"{label}: {close.Gap:0.0} m outside hostile pool {close.Pool}'s circle at ({close.At.X:0.0}, {close.At.Y:0.0})"));
                if (Vector3.Distance(line[^1], target) > 0.01f)
                    failures.Add($"{label} ends at {line[^1]}, not {target}");
            }
            Assert.IsTrue(Facility.Crosses(Legs["the rear ramp to outside the rear exit"].ToList(), ToVector(Digging.DoorAt), Digging.DoorOrientation),
                "the walk out crosses the rear exit door's leaf");
            Assert.AreEqual(2 * Legs.Count, walked, "every leg both ways");
            Assert.IsEmpty(failures, $"{failures.Count}: " + string.Join("; ", failures));
        }

        /// <summary>
        /// Digging for the Truth: Drof (set 15's, in the Control Room) offers it after Vindication only; at the accept the Power
        /// Supply comes, the player's own box (locked to it), rear exit door (shut, not usable), two teleporters (off, not usable)
        /// and six drills stand; 1 and 5 (and the six drills' objectives) open, 2 and 3 wait; the helpers 12-14 reach the client
        /// in no form. The box takes the supply: set, 12 done, the door open, the teleporters on; a reload and a restart keep them.
        /// A teleporter takes the player just outside the rear exit (a move on this map); the arrival completes 13 and 5, and 2
        /// opens; the cave entrance completes 2, the tunnel's exit 3. Each drill shot to pieces completes its own objective and
        /// counts for 1; with the sixth the mission is completeable; turned in by radio it pays the item chosen once, 1532 is
        /// offered by radio, the objects go with the mission.
        /// </summary>
        [TestMethod]
        public void DiggingForTheTruthTurnsThePowerOnAndBreaksTheDrills()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            AshenRuntime.SeedCompleted(harness, AshenMissingScientistsV1.MissionId, AshenIndraCavernsV1.MissionId, AshenThresholdV1.MissionId);
            var copy = IntoGotha(harness);
            var drof = Giver(copy, F15.Drof);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, drof, Digging.MissionId), "not before Vindication");
            AshenRuntime.SeedCompleted(harness, AshenVindicationV1.MissionId);
            drof = Giver(copy, F15.Drof);
            Accept(harness, drof, Digging.MissionId);
            Tick(harness, copy, 250);
            Assert.AreEqual(1U, Facility.Held(harness, Digging.PowerSupplyTemplate), "the power supply");
            Assert.AreEqual((MissionObjectiveState.Incomplete, MissionObjectiveState.Inactive, MissionObjectiveState.Inactive, MissionObjectiveState.Incomplete),
                (Objective(harness, Digging.MissionId, Digging.DestroyDrills), Objective(harness, Digging.MissionId, Digging.RearCaveEntrance),
                    Objective(harness, Digging.MissionId, Digging.ExploreTunnel), Objective(harness, Digging.MissionId, Digging.RestorePower)));
            var visible = harness.Manager.BuildStatusSnapshot(harness.Client.Player)[Digging.MissionId].ObjectivesList.Select(objective => objective.ObjectiveId).ToArray();
            foreach (var hidden in new[] { Digging.PowerOn, Digging.OutsideTheRearExit, Digging.Opener })
                Assert.IsFalse(visible.Contains(hidden), $"{hidden} is hidden");

            var box = SceneObject(harness, copy, Digging.MissionId, Digging.BoxRole);
            Assert.AreEqual((Digging.SwitchClass, (UseObjectState)Digging.SwitchReady, Digging.PowerSupplyTemplate, ToVector(Digging.BoxAt), harness.Client.Player.Id),
                ((uint)box.EntityClassId, box.StateId, box.Lock?.KeyItemTemplateId ?? 0, box.Position, box.SceneOwnerCharacterId), "the player's own box, locked to the power supply");
            var door = SceneObject(harness, copy, Digging.MissionId, Digging.DoorRole);
            Assert.AreEqual((Digging.DoorClass, UseObjectState.DoorStateClosed, false), ((uint)door.EntityClassId, door.StateId, door.IsEnabled), "the rear exit door shut");
            for (var number = 1; number <= 2; number++)
            {
                var teleporter = SceneObject(harness, copy, Digging.MissionId, Digging.TeleporterRole(number));
                Assert.AreEqual(((UseObjectState)Digging.TeleporterOff, false), (teleporter.StateId, teleporter.IsEnabled), $"teleporter {number} off, not usable");
            }

            // 12: the box.
            Use(harness, copy, box, Digging.UseArgument);
            Tick(harness, copy, 250);
            Tick(harness, copy, 250);
            Assert.AreEqual((0U, MissionObjectiveState.Completed, MissionObjectiveState.Incomplete), (Facility.Held(harness, Digging.PowerSupplyTemplate),
                Objective(harness, Digging.MissionId, Digging.PowerOn), Objective(harness, Digging.MissionId, Digging.RestorePower)), "the supply taken, 12 done, 5 waits for 13");
            AssertPowerOn(harness, copy, "the box used");
            ReloadAndRecover(harness, copy);
            AssertPowerOn(harness, copy, "at a reload");
            Facility.Restart(harness, copy, Digging.MissionId);
            AssertPowerOn(harness, copy, "at a restart");

            // 13 and 5: a teleporter.
            Use(harness, copy, SceneObject(harness, copy, Digging.MissionId, Digging.TeleporterRole(1)), Digging.UseArgument);
            Tick(harness, copy, 250);
            Teleported(harness, ToVector(Digging.RearArrival), "taken just outside the rear exit");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Digging.MissionId, Digging.OutsideTheRearExit), "arrived short of the rear area");
            Enter(harness, ToVector(Digging.RearArrival), V(Digging.RearAt));
            Assert.AreEqual((MissionObjectiveState.Completed, MissionObjectiveState.Completed, MissionObjectiveState.Incomplete),
                (Objective(harness, Digging.MissionId, Digging.OutsideTheRearExit), Objective(harness, Digging.MissionId, Digging.RestorePower),
                    Objective(harness, Digging.MissionId, Digging.RearCaveEntrance)), "13 and 5 done, 2 open");

            // 2 and 3: the cave entrance, the tunnel's exit.
            Enter(harness, V(Digging.RearAt), V(Digging.EntranceAt));
            Assert.AreEqual((MissionObjectiveState.Completed, MissionObjectiveState.Incomplete),
                (Objective(harness, Digging.MissionId, Digging.RearCaveEntrance), Objective(harness, Digging.MissionId, Digging.ExploreTunnel)), "2 done, 3 open");
            Enter(harness, V(Digging.EntranceAt), V(Digging.TunnelExitAt));
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Digging.MissionId, Digging.ExploreTunnel), "3 done");

            // 6-11 and 1: the drills.
            for (var number = 1; number <= Digging.Drills; number++)
            {
                var drill = SceneObject(harness, copy, Digging.MissionId, Digging.DrillRole(number));
                Assert.AreEqual((Digging.DrillClass, (UseObjectState)Digging.IntactState), ((uint)drill.EntityClassId, drill.StateId), $"drill {number} intact");
                Shoot(harness, copy, drill, (int)Digging.DrillHitPoints + 100);
                Tick(harness, copy, 250);
                Assert.AreEqual(((UseObjectState)Digging.DestroyedState, MissionObjectiveState.Completed),
                    (SceneObject(harness, copy, Digging.MissionId, Digging.DrillRole(number)).StateId, Objective(harness, Digging.MissionId, Digging.DrillObjective(number))),
                    $"drill {number} destroyed, its objective done");
                Assert.AreEqual(number == Digging.Drills ? MissionObjectiveState.Completed : MissionObjectiveState.Incomplete,
                    Objective(harness, Digging.MissionId, Digging.DestroyDrills), $"1 after drill {number}");
            }
            Assert.IsTrue(harness.Client.Player.Missions[Digging.MissionId].Completeable, "to be radioed in");
            ReloadAndRecover(harness, copy);
            Assert.IsTrue(harness.Client.Player.Missions[Digging.MissionId].Completeable, "kept at a reload");

            var before = harness.Context.ReadRewardTotals();
            Assert.IsTrue(harness.Manager.TryCompleteRadioMission(harness.Client, Digging.MissionId, 0, null), "radioed in");
            var rewarded = harness.Drain();
            Assert.AreEqual(1, rewarded.OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Digging.MissionId), "paid once");
            var after = harness.Context.ReadRewardTotals();
            Assert.AreEqual((before.Experience + Digging.Experience, before.Credits + (int)Digging.Credits), (after.Experience, after.Credits));
            Assert.AreEqual(1U, Facility.Held(harness, Digging.TorqueshellRifle), "the rifle chosen");
            Assert.IsFalse(harness.Manager.TryCompleteRadioMission(harness.Client, Digging.MissionId, 0, null), "and only once");
            Assert.AreEqual(1, rewarded.OfType<DispenseRadioMissionPacket>().Count(packet => packet.MissionId == Meet.MissionId), "1532 by radio");
            for (var tick = 0; tick < 4; tick++)
                Tick(harness, copy, 250);
            foreach (var role in new[] { Digging.BoxRole, Digging.DoorRole, Digging.TeleporterRole(1), Digging.TeleporterRole(2), Digging.DrillRole(1) })
                Assert.IsNull(FindSceneObject(harness, copy, Digging.MissionId, role), $"{role} goes with the mission");
        }

        /// <summary>
        /// Digging for the Truth in a squad of two on it: a power supply each; the player's box use completes the mate's 12 too:
        /// each one's supply taken, each one's own door open and own teleporters on. The mate walks out through his own door: his
        /// 13 and 5 done (an area is a member's own). A member without 1447 is shown none of it and gets no supply.
        /// </summary>
        [TestMethod]
        public void DiggingsPowerComesOnForEverySquadMateOnIt()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            AshenRuntime.SeedCompleted(harness, AshenMissingScientistsV1.MissionId, AshenIndraCavernsV1.MissionId, AshenThresholdV1.MissionId, AshenVindicationV1.MissionId);
            var copy = IntoGotha(harness);
            var drof = Giver(copy, F15.Drof);
            Accept(harness, drof, Digging.MissionId);
            Tick(harness, copy, 250);
            using var mate = new Facility.SquadMate(harness, copy, drof, Digging.MissionId,
                new[] { AshenIndraCavernsV1.MissionId, AshenThresholdV1.MissionId, AshenVindicationV1.MissionId });
            using var stranger = new Facility.SquadMate(harness, copy, drof, 0, Array.Empty<uint>(), 3, mate.PartyId);
            Assert.AreEqual((1U, 0U), (mate.Held(Digging.PowerSupplyTemplate), stranger.Held(Digging.PowerSupplyTemplate)), "the mate's own supply; none for the stranger");
            Assert.IsFalse(DynamicObjectManager.IsShownTo(stranger.Client, SceneObject(harness, copy, Digging.MissionId, Digging.BoxRole)), "a member without 1447: no box");

            Use(harness, copy, SceneObject(harness, copy, Digging.MissionId, Digging.BoxRole), Digging.UseArgument);
            for (var tick = 0; tick < 2; tick++)
                Tick(harness, copy, 250);
            Assert.AreEqual((MissionObjectiveState.Completed, 0U, UseObjectState.DoorStateOpen, (UseObjectState)Digging.TeleporterOn, true),
                (mate.Of(Digging.PowerOn), mate.Held(Digging.PowerSupplyTemplate), mate.Door(Digging.DoorRole).StateId,
                    mate.Door(Digging.TeleporterRole(1)).StateId, mate.Door(Digging.TeleporterRole(1)).IsEnabled),
                "the mate's 12 done, his supply taken, his own door open and teleporters on");
            Assert.AreEqual(MissionObjectiveState.Incomplete, mate.Of(Digging.RestorePower), "the mate's 5 waits for his own walk out");
            mate.Beside(V(Digging.RearAt));
            Assert.IsTrue(mate.Client.MissionAreaService.RecordAcceptedMovement(mate.Client, ToVector(Digging.DoorAt), V(Digging.RearAt)), "the rear area");
            harness.Drain();
            Assert.AreEqual((MissionObjectiveState.Completed, MissionObjectiveState.Completed, MissionObjectiveState.Incomplete),
                (mate.Of(Digging.OutsideTheRearExit), mate.Of(Digging.RestorePower), mate.Of(Digging.RearCaveEntrance)), "the mate's walk out: his 13 and 5, his 2 open");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Digging.MissionId, Digging.RestorePower), "the player's 5 waits for the player's own");
        }

        /// <summary>
        /// The audit's lock-out: a mate away from the copy at the player's box use keeps his supply and his shut door, his 12
        /// open; back, his own box takes his own supply and turns his power on.
        /// </summary>
        [TestMethod]
        public void DiggingsMateAwayTurnsHisOwnPowerOn()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            AshenRuntime.SeedCompleted(harness, AshenMissingScientistsV1.MissionId, AshenIndraCavernsV1.MissionId, AshenThresholdV1.MissionId, AshenVindicationV1.MissionId);
            var copy = IntoGotha(harness);
            var drof = Giver(copy, F15.Drof);
            Accept(harness, drof, Digging.MissionId);
            Tick(harness, copy, 250);
            using var mate = new Facility.SquadMate(harness, copy, drof, Digging.MissionId,
                new[] { AshenIndraCavernsV1.MissionId, AshenThresholdV1.MissionId, AshenVindicationV1.MissionId });
            using var late = new Facility.SquadMate(harness, copy, drof, Digging.MissionId,
                new[] { AshenIndraCavernsV1.MissionId, AshenThresholdV1.MissionId, AshenVindicationV1.MissionId }, 3, mate.PartyId);

            late.Away();
            Use(harness, copy, SceneObject(harness, copy, Digging.MissionId, Digging.BoxRole), Digging.UseArgument);
            for (var tick = 0; tick < 2; tick++)
                Tick(harness, copy, 250);
            late.Back();
            Assert.AreEqual((MissionObjectiveState.Incomplete, 1U, UseObjectState.DoorStateClosed),
                (late.Of(Digging.PowerOn), late.Held(Digging.PowerSupplyTemplate), late.Door(Digging.DoorRole).StateId), "away at the use: his 12 open, his supply kept, his door shut");
            late.Use(late.Door(Digging.BoxRole));
            Assert.AreEqual((MissionObjectiveState.Completed, 0U, UseObjectState.DoorStateOpen),
                (late.Of(Digging.PowerOn), late.Held(Digging.PowerSupplyTemplate), late.Door(Digging.DoorRole).StateId), "his own supply turns his own power on");

        }

        /// <summary>
        /// The supply holder leaves: the player abandons 1447 before the box is used; his supply and his objects go, the mate
        /// keeps his own supply and turns his own power on with it.
        /// </summary>
        [TestMethod]
        public void DiggingsMateGetsInWhenThePlayerAbandons()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            AshenRuntime.SeedCompleted(harness, AshenMissingScientistsV1.MissionId, AshenIndraCavernsV1.MissionId, AshenThresholdV1.MissionId, AshenVindicationV1.MissionId);
            var copy = IntoGotha(harness);
            var drof = Giver(copy, F15.Drof);
            Accept(harness, drof, Digging.MissionId);
            Tick(harness, copy, 250);
            using var mate = new Facility.SquadMate(harness, copy, drof, Digging.MissionId,
                new[] { AshenIndraCavernsV1.MissionId, AshenThresholdV1.MissionId, AshenVindicationV1.MissionId });
            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Digging.MissionId));
            harness.Drain();
            for (var tick = 0; tick < 4; tick++)
                Tick(harness, copy, 250);
            Assert.AreEqual((0U, 1U), (Facility.Held(harness, Digging.PowerSupplyTemplate), mate.Held(Digging.PowerSupplyTemplate)), "the player's supply taken at the abandon, the mate's kept");
            Assert.IsNull(FindSceneObject(harness, copy, Digging.MissionId, Digging.BoxRole), "the player's box goes with the abandon");
            mate.Use(mate.Door(Digging.BoxRole));
            Assert.AreEqual((MissionObjectiveState.Completed, UseObjectState.DoorStateOpen), (mate.Of(Digging.PowerOn), mate.Door(Digging.DoorRole).StateId),
                "the mate's own supply turns his own power on");
        }

        /// <summary>Digging for the Truth with a full mission-item bag: no accept (no room for the power supply); a slot free, it is.</summary>
        [TestMethod]
        public void DiggingWaitsForRoomForThePowerSupply()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            AshenRuntime.SeedCompleted(harness, AshenMissingScientistsV1.MissionId, AshenIndraCavernsV1.MissionId, AshenThresholdV1.MissionId, AshenVindicationV1.MissionId);
            var copy = IntoGotha(harness);
            FullBagWaitsAtTheAccept(harness, Giver(copy, F15.Drof), Digging.MissionId, Digging.PowerSupplyTemplate);
        }

        /// <summary>
        /// Meet Sergeant Drof: offered by radio on arriving in Indra Caverns once 1447 is done (one who let the call pass), not
        /// before; no NPC offers it; taken, Drof's second row at the plant's entrance hears 1 (and set 15's Drof too: the same
        /// package), the talk completes it; the turn-in at the second row pays the stack chosen once; Drof offers 1451 next.
        /// </summary>
        [TestMethod]
        public void MeetSergeantDrofAtThePlantsEntrance()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            AshenRuntime.SeedCompleted(harness, AshenMissingScientistsV1.MissionId, AshenIndraCavernsV1.MissionId, AshenThresholdV1.MissionId, AshenVindicationV1.MissionId);
            var copy = IntoGotha(harness);
            harness.Manager.OfferArrivalMissions(harness.Client);
            Assert.AreEqual(0, harness.Drain().OfType<DispenseRadioMissionPacket>().Count(packet => packet.MissionId == Meet.MissionId), "not before 1447");
            AshenRuntime.SeedCompleted(harness, Digging.MissionId);
            harness.Manager.OfferArrivalMissions(harness.Client);
            Assert.AreEqual(1, harness.Drain().OfType<DispenseRadioMissionPacket>().Count(packet => packet.MissionId == Meet.MissionId), "1532 on arrival");
            var drof = Giver(copy, N.Drof);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, drof, Meet.MissionId), "not Drof's to give");
            Assert.IsTrue(harness.Manager.TryAcceptRadioMission(harness.Client, Meet.MissionId), "taken by radio");
            harness.Drain();
            CollectionAssert.AreEqual(new[] { Meet.Proceed }, Topics(harness, drof, Meet.MissionId), "Drof hears 1");
            Talk(harness, drof, Meet.MissionId, Meet.Proceed);
            Tick(harness, copy, 250);
            Assert.IsTrue(harness.Client.Player.Missions[Meet.MissionId].Completeable, "to be turned in");
            drof = Giver(copy, N.Drof);
            FailedTurnInPaysNothing(harness, drof, Meet.MissionId, 0);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, drof, Meet.MissionId, 0, Meet.Experience, Meet.Credits);
            Assert.AreEqual(3U, Facility.Held(harness, Meet.IncendiaryGrenade), "the three grenades");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, Giver(copy, N.Drof), Subversion.MissionId), "Drof offers Subversion next");
        }

        /// <summary>
        /// Subversion: Drof's second row offers it after 1532; at the accept the player's own two valves and four life support
        /// systems stand (named, ready); each use sets it, used no more, completes its own objective and counts for 1 or 2; a
        /// reload and a restart keep them set; with the sixth the mission is completeable; not by radio: turned in at Drof it pays once; Naimi
        /// offers 1452 next; the switches go with the mission.
        /// </summary>
        [TestMethod]
        public void SubversionSabotagesTheValvesAndTheLifeSupport()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            AshenRuntime.SeedCompleted(harness, AshenMissingScientistsV1.MissionId, AshenIndraCavernsV1.MissionId, AshenThresholdV1.MissionId,
                AshenVindicationV1.MissionId, Digging.MissionId);
            var copy = IntoGotha(harness);
            var drof = Giver(copy, N.Drof);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, drof, Subversion.MissionId), "not before 1532");
            AshenRuntime.SeedCompleted(harness, Meet.MissionId);
            drof = Giver(copy, N.Drof);
            Accept(harness, drof, Subversion.MissionId);
            Tick(harness, copy, 250);
            var switches = Enumerable.Range(1, (int)Subversion.ValveCount).Select(number => (Role: Subversion.ValveRole(number), Objective: Subversion.ValveObjective(number), Counter: Subversion.Valves))
                .Concat(Enumerable.Range(1, (int)Subversion.SupportCount).Select(number => (Role: Subversion.SupportRole(number), Objective: Subversion.SupportObjective(number), Counter: Subversion.LifeSupport)))
                .ToArray();
            foreach (var (role, objective, counter) in switches)
            {
                var obj = SceneObject(harness, copy, Subversion.MissionId, role);
                Assert.AreEqual((Subversion.SwitchClass, (UseObjectState)Subversion.SwitchReady, true, MissionObjectiveState.Incomplete),
                    ((uint)obj.EntityClassId, obj.StateId, obj.IsEnabled, Objective(harness, Subversion.MissionId, objective)), $"{role} ready");
            }
            for (var i = 0; i < switches.Length; i++)
            {
                var (role, objective, counter) = switches[i];
                Use(harness, copy, SceneObject(harness, copy, Subversion.MissionId, role), Subversion.UseArgument);
                Tick(harness, copy, 250);
                var obj = SceneObject(harness, copy, Subversion.MissionId, role);
                Assert.AreEqual(((UseObjectState)Subversion.SwitchSet, false, MissionObjectiveState.Completed),
                    (obj.StateId, obj.IsEnabled, Objective(harness, Subversion.MissionId, objective)), $"{role} set, its objective done");
                var lastOfItsKind = i == Subversion.ValveCount - 1 || i == switches.Length - 1;
                Assert.AreEqual(lastOfItsKind ? MissionObjectiveState.Completed : MissionObjectiveState.Incomplete, Objective(harness, Subversion.MissionId, counter),
                    $"{counter} after {role}");
                if (i == 0)
                {
                    ReloadAndRecover(harness, copy);
                    Facility.Restart(harness, copy, Subversion.MissionId);
                    Assert.AreEqual((UseObjectState)Subversion.SwitchSet, SceneObject(harness, copy, Subversion.MissionId, role).StateId, "set at a reload and a restart");
                }
            }
            Assert.IsTrue(harness.Client.Player.Missions[Subversion.MissionId].Completeable, "to be turned in at Drof");
            Assert.IsFalse(harness.Manager.TryCompleteRadioMission(harness.Client, Subversion.MissionId, 0, null), "not by radio (the client's 12890: speak to Drof)");
            drof = Giver(copy, N.Drof);
            FailedTurnInPaysNothing(harness, drof, Subversion.MissionId, 0);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, drof, Subversion.MissionId, 0, Subversion.Experience, Subversion.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Subversion.MissionId), "paid once");
            for (var tick = 0; tick < 4; tick++)
                Tick(harness, copy, 250);
            foreach (var (role, _, _) in switches)
                Assert.IsNull(FindSceneObject(harness, copy, Subversion.MissionId, role), $"{role} goes with the mission");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, Giver(copy, N.Naimi), Desistance.MissionId), "Naimi offers Desistance next");
        }

        /// <summary>
        /// Subversion in a squad: the player's use of a valve completes a mate's objective too (a scene result, GroupCreditService);
        /// a mate away from the copy at the use keeps his objective open and completes it with his own valve.
        /// </summary>
        [TestMethod]
        public void SubversionsSwitchesCountForEverySquadMateOnIt()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var done = new[] { AshenIndraCavernsV1.MissionId, AshenThresholdV1.MissionId, AshenVindicationV1.MissionId, Digging.MissionId, Meet.MissionId };
            AshenRuntime.SeedCompleted(harness, done.Prepend(AshenMissingScientistsV1.MissionId).ToArray());
            var copy = IntoGotha(harness);
            var drof = Giver(copy, N.Drof);
            Accept(harness, drof, Subversion.MissionId);
            Tick(harness, copy, 250);
            using var mate = new Facility.SquadMate(harness, copy, drof, Subversion.MissionId, done);
            using var late = new Facility.SquadMate(harness, copy, drof, Subversion.MissionId, done, 3, mate.PartyId);
            late.Away();
            Use(harness, copy, SceneObject(harness, copy, Subversion.MissionId, Subversion.ValveRole(1)), Subversion.UseArgument);
            for (var tick = 0; tick < 2; tick++)
                Tick(harness, copy, 250);
            late.Back();
            Assert.AreEqual((MissionObjectiveState.Completed, MissionObjectiveState.Incomplete), (mate.Of(Subversion.ValveObjective(1)), late.Of(Subversion.ValveObjective(1))),
                "the mate's 3 done with the player's use; the late mate's open");
            late.Use(late.Door(Subversion.ValveRole(1)));
            Assert.AreEqual(MissionObjectiveState.Completed, late.Of(Subversion.ValveObjective(1)), "his own valve completes his own 3");
        }

        /// <summary>
        /// Desistance: Naimi's second row offers it after 1451; at the accept the Bomb comes; 1 open, the rest wait. The door
        /// console completes 1 and opens 2; either Juggernaut console completes 2 (the other is used no more) and opens 4; the pad's
        /// area completes 4 and opens 3. The charge's use takes the bomb, completes 3, then blows the generator and opens 5 and 6 (fix J); Central Administration's area completes 5. The console is refused without the hand; Kretas'
        /// kill completes 6 and drops the hand (a death is not a pickup); a reload keeps the picked hand; the console takes it,
        /// completes 7, opens 8 and turns the teleporter on (also at a restart); the teleporter takes the player beside Diren;
        /// Diren hears 8; the turn-in at Diren pays once and takes nothing more; the objects go with the mission.
        /// </summary>
        [TestMethod]
        public void DesistanceBombsTheGeneratorAndTakesKretasHand()
        {
            using var harness = WildernessRuntimeTestHarness.Create(lootRoll: (minimum, maximum) => maximum == 100 ? 99 : minimum);
            AshenRuntime.SeedCompleted(harness, AshenMissingScientistsV1.MissionId, AshenIndraCavernsV1.MissionId, AshenThresholdV1.MissionId,
                AshenVindicationV1.MissionId, Digging.MissionId, Meet.MissionId);
            var copy = IntoGotha(harness);
            var naimi = Giver(copy, N.Naimi);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, naimi, Desistance.MissionId), "not before 1451");
            AshenRuntime.SeedCompleted(harness, Subversion.MissionId);
            naimi = Giver(copy, N.Naimi);
            Accept(harness, naimi, Desistance.MissionId);
            Tick(harness, copy, 250);
            Assert.AreEqual(1U, Facility.Held(harness, Desistance.BombTemplate), "the bomb");
            Assert.AreEqual((MissionObjectiveState.Incomplete, MissionObjectiveState.Inactive, MissionObjectiveState.Inactive),
                (Objective(harness, Desistance.MissionId, Desistance.BayDoors), Objective(harness, Desistance.MissionId, Desistance.Juggernauts),
                    Objective(harness, Desistance.MissionId, Desistance.ReportToDiren)));
            Assert.IsFalse(harness.Manager.BuildStatusSnapshot(harness.Client.Player)[Desistance.MissionId].ObjectivesList.Any(objective => objective.ObjectiveId == Desistance.HandTaken),
                "9 is hidden");

            UseAndTick(harness, copy, Desistance.DoorConsoleRole);
            Assert.AreEqual((MissionObjectiveState.Completed, MissionObjectiveState.Incomplete),
                (Objective(harness, Desistance.MissionId, Desistance.BayDoors), Objective(harness, Desistance.MissionId, Desistance.Juggernauts)), "1 done, 2 open");
            UseAndTick(harness, copy, Desistance.SystemsRole);
            Assert.AreEqual((MissionObjectiveState.Completed, MissionObjectiveState.Incomplete, false),
                (Objective(harness, Desistance.MissionId, Desistance.Juggernauts), Objective(harness, Desistance.MissionId, Desistance.LandingPad),
                    SceneObject(harness, copy, Desistance.MissionId, Desistance.ProgrammingRole).IsEnabled), "2 done, 4 open, the other console used no more");
            Enter(harness, ToVector(Desistance.DoorConsoleAt), V(Desistance.PadAt));
            Assert.AreEqual((MissionObjectiveState.Completed, MissionObjectiveState.Incomplete),
                (Objective(harness, Desistance.MissionId, Desistance.LandingPad), Objective(harness, Desistance.MissionId, Desistance.Generator)), "4 done, 3 open");

            var charge = SceneObject(harness, copy, Desistance.MissionId, Desistance.ChargeRole);
            Assert.IsNull(charge.Lock, "the charge has no lock (a lock in state 0 locks nothing): 3's take of the bomb gates it");
            UseAndTick(harness, copy, Desistance.ChargeRole);
            Assert.AreEqual((0U, (UseObjectState)Desistance.DestroyedState, MissionObjectiveState.Completed, MissionObjectiveState.Incomplete, MissionObjectiveState.Incomplete),
                (Facility.Held(harness, Desistance.BombTemplate), SceneObject(harness, copy, Desistance.MissionId, Desistance.GeneratorRole).StateId,
                    Objective(harness, Desistance.MissionId, Desistance.Generator), Objective(harness, Desistance.MissionId, Desistance.Administration),
                    Objective(harness, Desistance.MissionId, Desistance.DefeatKretas)),
                "the bomb taken, the generator blown, 3 done, 5 and 6 open (fix J)");
            Enter(harness, V(Desistance.PadAt), V(Desistance.AdministrationAt));
            Assert.AreEqual((MissionObjectiveState.Completed, MissionObjectiveState.Incomplete),
                (Objective(harness, Desistance.MissionId, Desistance.Administration), Objective(harness, Desistance.MissionId, Desistance.DefeatKretas)), "5 done, 6 open");

            var console = SceneObject(harness, copy, Desistance.MissionId, Desistance.ConsoleRole);
            Assert.AreEqual(Desistance.HandTemplate, console.Lock?.KeyItemTemplateId ?? 0, "the console locked to the hand");
            Assert.AreEqual(PlayerMessage.PmMissingReqItem, Facility.Refused(harness, copy, console, Desistance.UseArgument), "no hand: refused");
            Facility.Stand(copy, N.KretasPool);
            var corpse = Kill(harness, copy, N.Kretas);
            Assert.IsTrue(Holds(corpse, Desistance.HandTemplate), "Kretas drops the hand");
            Assert.AreEqual((0U, MissionObjectiveState.Completed, MissionObjectiveState.Inactive),
                (Facility.Held(harness, Desistance.HandTemplate), Objective(harness, Desistance.MissionId, Desistance.DefeatKretas),
                    Objective(harness, Desistance.MissionId, Desistance.ReportToDiren)), "6 done; a death is not a pickup");
            PickUp(harness, corpse);
            harness.Drain();
            ReloadAndRecover(harness, copy);
            Assert.AreEqual(1U, Facility.Held(harness, Desistance.HandTemplate), "the hand, kept at a reload");
            var teleporter = SceneObject(harness, copy, Desistance.MissionId, Desistance.TeleporterRole);
            Assert.AreEqual(((UseObjectState)Desistance.TeleporterOff, false), (teleporter.StateId, teleporter.IsEnabled), "the teleporter off");

            UseAndTick(harness, copy, Desistance.ConsoleRole);
            Assert.AreEqual((0U, MissionObjectiveState.Completed, MissionObjectiveState.Incomplete),
                (Facility.Held(harness, Desistance.HandTemplate), Objective(harness, Desistance.MissionId, Desistance.Teleporter),
                    Objective(harness, Desistance.MissionId, Desistance.ReportToDiren)), "the hand taken, 7 done, 8 open");
            Facility.Restart(harness, copy, Desistance.MissionId);
            teleporter = SceneObject(harness, copy, Desistance.MissionId, Desistance.TeleporterRole);
            Assert.AreEqual(((UseObjectState)Desistance.TeleporterOn, true), (teleporter.StateId, teleporter.IsEnabled), "the teleporter on, also at a restart");
            Use(harness, copy, teleporter, Desistance.UseArgument);
            Tick(harness, copy, 250);
            Teleported(harness, ToVector(Desistance.DirenArrival), "taken beside Diren");

            var diren = Giver(copy, F15.Diren);
            CollectionAssert.AreEqual(new[] { Desistance.ReportToDiren }, Topics(harness, diren, Desistance.MissionId), "Diren hears 8");
            Talk(harness, diren, Desistance.MissionId, Desistance.ReportToDiren);
            Tick(harness, copy, 250);
            Assert.IsTrue(harness.Client.Player.Missions[Desistance.MissionId].Completeable);
            diren = Giver(copy, F15.Diren);
            FullBagWaits(harness, diren, Desistance.MissionId, 0, WeaponFiller);
            FailedTurnInPaysNothing(harness, diren, Desistance.MissionId, 0);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, diren, Desistance.MissionId, 0, Desistance.Experience, Desistance.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Desistance.MissionId), "MissionRewarded once");
            for (var tick = 0; tick < 4; tick++)
                Tick(harness, copy, 250);
            foreach (var role in new[] { Desistance.DoorConsoleRole, Desistance.ChargeRole, Desistance.GeneratorRole, Desistance.ConsoleRole, Desistance.TeleporterRole })
                Assert.IsNull(FindSceneObject(harness, copy, Desistance.MissionId, role), $"{role} goes with the mission");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, Giver(copy, N.Naimi), Desistance.MissionId), "done once");
        }

        /// <summary>
        /// Desistance in a squad of two on it: a bomb each; the player's charge completes the mate's 3 and takes the mate's bomb;
        /// Kretas' kill leaves each member a hand of his own and completes the mate's 6; the player's console use completes the
        /// mate's 7, takes his hand and turns his own teleporter on. A mate away at the console use keeps his hand and his 7;
        /// back, his own console takes it.
        /// </summary>
        [TestMethod]
        public void DesistancesHandWorksForEverySquadMateOnIt()
        {
            using var harness = WildernessRuntimeTestHarness.Create(lootRoll: (minimum, maximum) => maximum == 100 ? 0 : minimum);
            var done = new[] { AshenIndraCavernsV1.MissionId, AshenThresholdV1.MissionId, AshenVindicationV1.MissionId, Digging.MissionId, Meet.MissionId, Subversion.MissionId };
            AshenRuntime.SeedCompleted(harness, done.Prepend(AshenMissingScientistsV1.MissionId).ToArray());
            var copy = IntoGotha(harness);
            var naimi = Giver(copy, N.Naimi);
            Accept(harness, naimi, Desistance.MissionId);
            Tick(harness, copy, 250);
            using var mate = new Facility.SquadMate(harness, copy, naimi, Desistance.MissionId, done);
            using var late = new Facility.SquadMate(harness, copy, naimi, Desistance.MissionId, done, 3, mate.PartyId);
            Assert.AreEqual((1U, 1U), (mate.Held(Desistance.BombTemplate), late.Held(Desistance.BombTemplate)), "a bomb each");

            UseAndTick(harness, copy, Desistance.DoorConsoleRole);
            UseAndTick(harness, copy, Desistance.ProgrammingRole);
            Enter(harness, ToVector(Desistance.DoorConsoleAt), V(Desistance.PadAt));
            foreach (var member in new[] { mate, late })
            {
                member.Beside(V(Desistance.PadAt) + new Vector3(0, 0, 3));
                Assert.IsTrue(member.Client.MissionAreaService.RecordAcceptedMovement(member.Client, ToVector(Desistance.DoorConsoleAt), V(Desistance.PadAt)), "the pad");
                harness.Drain();
            }
            UseAndTick(harness, copy, Desistance.ChargeRole);
            Assert.AreEqual((MissionObjectiveState.Completed, 0U), (mate.Of(Desistance.Generator), mate.Held(Desistance.BombTemplate)), "the mate's 3 done, his bomb taken");
            Enter(harness, V(Desistance.PadAt), V(Desistance.AdministrationAt));
            foreach (var member in new[] { mate, late })
            {
                member.Beside(V(Desistance.AdministrationAt));
                Assert.IsTrue(member.Client.MissionAreaService.RecordAcceptedMovement(member.Client, V(Desistance.PadAt), V(Desistance.AdministrationAt)), "Central Administration");
                harness.Drain();
            }

            Facility.Stand(copy, N.KretasPool);
            var kretas = Here(copy).First(one => one.SpawnPool?.DbId == N.KretasPool && one.State != CharacterState.Dead);
            mate.Beside(kretas.Position + new Vector3(0, 0, 3));
            late.Beside(kretas.Position + new Vector3(0, 0, -3));
            var corpse = Kill(harness, copy, N.Kretas);
            var hands = corpse.LootItems.Where(item => item.ItemTemplateId == Desistance.HandTemplate).ToArray();
            Assert.AreEqual((1, 1, 1), (hands.Count(item => item.ReservedFor == harness.Client.Player.EntityId), hands.Count(item => item.ReservedFor == mate.Client.Player.EntityId),
                hands.Count(item => item.ReservedFor == late.Client.Player.EntityId)), "a hand each");
            mate.Loot(corpse);
            late.Loot(corpse);
            PickUp(harness, corpse);
            harness.Drain();
            Tick(harness, copy, 250);
            Assert.AreEqual((1U, 1U, 1U), (Facility.Held(harness, Desistance.HandTemplate), mate.Held(Desistance.HandTemplate), late.Held(Desistance.HandTemplate)), "held each");

            late.Away();
            UseAndTick(harness, copy, Desistance.ConsoleRole);
            Assert.AreEqual((MissionObjectiveState.Completed, 0U, (UseObjectState)Desistance.TeleporterOn),
                (mate.Of(Desistance.Teleporter), mate.Held(Desistance.HandTemplate), mate.Door(Desistance.TeleporterRole).StateId),
                "the mate's 7 done, his hand taken, his own teleporter on");
            late.Back();
            Assert.AreEqual((MissionObjectiveState.Incomplete, 1U), (late.Of(Desistance.Teleporter), late.Held(Desistance.HandTemplate)), "away at the use: his 7 open, his hand kept");
            late.Use(late.Door(Desistance.ConsoleRole));
            Assert.AreEqual((MissionObjectiveState.Completed, 0U, (UseObjectState)Desistance.TeleporterOn),
                (late.Of(Desistance.Teleporter), late.Held(Desistance.HandTemplate), late.Door(Desistance.TeleporterRole).StateId), "his own hand, his own teleporter");
        }

        /// <summary>
        /// An abandon of Desistance with the hand held: the objects go, the bomb is taken, the hand kept (as 1395's); taken again
        /// the bomb comes again and 7 waits for its turn.
        /// </summary>
        [TestMethod]
        public void DesistanceAbandonedKeepsTheHand()
        {
            using var harness = WildernessRuntimeTestHarness.Create(lootRoll: (minimum, maximum) => maximum == 100 ? 0 : minimum);
            AshenRuntime.SeedCompleted(harness, AshenMissingScientistsV1.MissionId, AshenIndraCavernsV1.MissionId, AshenThresholdV1.MissionId,
                AshenVindicationV1.MissionId, Digging.MissionId, Meet.MissionId, Subversion.MissionId);
            var copy = IntoGotha(harness);
            Accept(harness, Giver(copy, N.Naimi), Desistance.MissionId);
            Tick(harness, copy, 250);
            Facility.Stand(copy, N.KretasPool);
            PickUp(harness, Kill(harness, copy, N.Kretas));
            harness.Drain();
            Tick(harness, copy, 250);
            Assert.AreEqual((1U, 1U), (Facility.Held(harness, Desistance.BombTemplate), Facility.Held(harness, Desistance.HandTemplate)), "the bomb and the hand");
            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Desistance.MissionId));
            harness.Drain();
            for (var tick = 0; tick < 4; tick++)
                Tick(harness, copy, 250);
            foreach (var role in new[] { Desistance.DoorConsoleRole, Desistance.ChargeRole, Desistance.ConsoleRole, Desistance.TeleporterRole })
                Assert.IsNull(FindSceneObject(harness, copy, Desistance.MissionId, role), $"{role} goes with the abandon");
            Assert.AreEqual((0U, 1U), (Facility.Held(harness, Desistance.BombTemplate), Facility.Held(harness, Desistance.HandTemplate)), "the bomb taken, the hand kept");
            Accept(harness, Giver(copy, N.Naimi), Desistance.MissionId);
            Tick(harness, copy, 250);
            Assert.AreEqual((1U, MissionObjectiveState.Inactive), (Facility.Held(harness, Desistance.BombTemplate), Objective(harness, Desistance.MissionId, Desistance.Teleporter)),
                "the bomb again; 7 waits");
        }

        /// <summary>
        /// Full bags: Desistance is not taken without room for the bomb; Kretas' hand waits on his body with a full bag (no hand,
        /// 9 open), one slot freed, the pickup gives it.
        /// </summary>
        [TestMethod]
        public void DesistancesFullBagWaitsForTheBombAndTheHand()
        {
            using var harness = WildernessRuntimeTestHarness.Create(lootRoll: (minimum, maximum) => maximum == 100 ? 0 : minimum);
            AshenRuntime.SeedCompleted(harness, AshenMissingScientistsV1.MissionId, AshenIndraCavernsV1.MissionId, AshenThresholdV1.MissionId,
                AshenVindicationV1.MissionId, Digging.MissionId, Meet.MissionId, Subversion.MissionId);
            var copy = IntoGotha(harness);
            FullBagWaitsAtTheAccept(harness, Giver(copy, N.Naimi), Desistance.MissionId, Desistance.BombTemplate);
            Tick(harness, copy, 250);
            Facility.Stand(copy, N.KretasPool);
            var corpse = Kill(harness, copy, N.Kretas);
            for (var slot = 0; slot < 60; slot++)
            {
                try { WildernessAliaBranchesTests.FillCategory(harness, Facility.MissionItemFiller, 1); }
                catch (Exception) { break; }
            }
            PickUp(harness, corpse);
            harness.Drain();
            Assert.AreEqual((0U, MissionObjectiveState.Incomplete, true), (Facility.Held(harness, Desistance.HandTemplate),
                Objective(harness, Desistance.MissionId, Desistance.HandTaken), Holds(corpse, Desistance.HandTemplate)), "a full bag: the hand waits on the body");
            WildernessAliaBranchesTests.FreeOneSlot(harness, Facility.MissionItemFiller);
            harness.Drain();
            PickUp(harness, corpse);
            harness.Drain();
            Assert.AreEqual((1U, MissionObjectiveState.Completed), (Facility.Held(harness, Desistance.HandTemplate), Objective(harness, Desistance.MissionId, Desistance.HandTaken)),
                "a slot free: the hand");
        }

        /// <summary>
        /// The audit's S2/S3: the charge has no lock, so 3's take of the bomb is the gate. Without his bomb (the take fails) the
        /// player's own 3 stays open, his generator stands, his charge stays usable (alone, the use does nothing). In a squad the player's use
        /// still completes the 3 of a mate who has his bomb (his bomb taken, his own generator blown); the player's own stays.
        /// </summary>
        [TestMethod]
        public void DesistancesChargeIsRefusedWithoutTheBomb()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var done = new[] { AshenIndraCavernsV1.MissionId, AshenThresholdV1.MissionId, AshenVindicationV1.MissionId, Digging.MissionId, Meet.MissionId, Subversion.MissionId };
            AshenRuntime.SeedCompleted(harness, done.Prepend(AshenMissingScientistsV1.MissionId).ToArray());
            var copy = IntoGotha(harness);
            var naimi = Giver(copy, N.Naimi);
            Accept(harness, naimi, Desistance.MissionId);
            Tick(harness, copy, 250);
            UseAndTick(harness, copy, Desistance.DoorConsoleRole);
            UseAndTick(harness, copy, Desistance.ProgrammingRole);
            Enter(harness, ToVector(Desistance.DoorConsoleAt), V(Desistance.PadAt));
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Desistance.MissionId, Desistance.Generator), "3 open");

            Lose(harness.Client, Desistance.BombTemplate);
            UseAndTick(harness, copy, Desistance.ChargeRole);
            Assert.AreEqual((MissionObjectiveState.Incomplete, (UseObjectState)Desistance.IntactState, true),
                (Objective(harness, Desistance.MissionId, Desistance.Generator), SceneObject(harness, copy, Desistance.MissionId, Desistance.GeneratorRole).StateId,
                    SceneObject(harness, copy, Desistance.MissionId, Desistance.ChargeRole).IsEnabled), "solo, no bomb: refused; the generator stands, the charge usable");

            using var mate = new Facility.SquadMate(harness, copy, naimi, Desistance.MissionId, done);
            mate.Beside(V(Desistance.PadAt) + new Vector3(0, 0, 3));
            harness.Drain();
            mate.Use(mate.Door(Desistance.DoorConsoleRole));
            mate.Use(mate.Door(Desistance.ProgrammingRole));
            Assert.IsTrue(mate.Client.MissionAreaService.RecordAcceptedMovement(mate.Client, ToVector(Desistance.DoorConsoleAt), V(Desistance.PadAt)), "the mate's pad");
            harness.Drain();
            Assert.AreEqual((MissionObjectiveState.Incomplete, 1U), (mate.Of(Desistance.Generator), mate.Held(Desistance.BombTemplate)), "the mate's 3 open, his bomb");
            UseAndTick(harness, copy, Desistance.ChargeRole);
            Assert.AreEqual((MissionObjectiveState.Completed, 0U, (UseObjectState)Desistance.DestroyedState, false),
                (mate.Of(Desistance.Generator), mate.Held(Desistance.BombTemplate), mate.Door(Desistance.GeneratorRole).StateId, mate.Door(Desistance.ChargeRole).IsEnabled),
                "in a squad: the mate's 3 done, his bomb taken, his own generator blown, his charge set");
            Assert.AreEqual((MissionObjectiveState.Incomplete, (UseObjectState)Desistance.IntactState, true),
                (Objective(harness, Desistance.MissionId, Desistance.Generator), SceneObject(harness, copy, Desistance.MissionId, Desistance.GeneratorRole).StateId,
                    SceneObject(harness, copy, Desistance.MissionId, Desistance.ChargeRole).IsEnabled), "the player's own 3 still open: his generator stands, his charge usable");
        }

        /// <summary>
        /// The audit's S3 for the box: a mate whose power supply can not be taken at the player's box use keeps his own box
        /// usable and ready, his door shut and his 12 open; the player's own power comes on.
        /// </summary>
        [TestMethod]
        public void DiggingsMateWhoseSupplyCanNotBeTakenKeepsHisBox()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            AshenRuntime.SeedCompleted(harness, AshenMissingScientistsV1.MissionId, AshenIndraCavernsV1.MissionId, AshenThresholdV1.MissionId, AshenVindicationV1.MissionId);
            var copy = IntoGotha(harness);
            var drof = Giver(copy, F15.Drof);
            Accept(harness, drof, Digging.MissionId);
            Tick(harness, copy, 250);
            using var mate = new Facility.SquadMate(harness, copy, drof, Digging.MissionId,
                new[] { AshenIndraCavernsV1.MissionId, AshenThresholdV1.MissionId, AshenVindicationV1.MissionId });
            Lose(mate.Client, Digging.PowerSupplyTemplate);
            Use(harness, copy, SceneObject(harness, copy, Digging.MissionId, Digging.BoxRole), Digging.UseArgument);
            for (var tick = 0; tick < 2; tick++)
                Tick(harness, copy, 250);
            AssertPowerOn(harness, copy, "the player's own power");
            var box = mate.Door(Digging.BoxRole);
            Assert.AreEqual((MissionObjectiveState.Incomplete, (UseObjectState)Digging.SwitchReady, true, UseObjectState.DoorStateClosed),
                (mate.Of(Digging.PowerOn), box.StateId, box.IsEnabled, mate.Door(Digging.DoorRole).StateId), "the mate's take failed: his box ready and usable, his door shut");
        }

        /// <summary>
        /// The re-audit's box refusal: alone and without his power supply (gone from the bag), the player's box (locked to the
        /// supply) refuses the use; his 12 stays open, his box ready and usable, his door shut and his teleporters off.
        /// </summary>
        [TestMethod]
        public void DiggingsBoxIsRefusedWithoutTheSupply()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            AshenRuntime.SeedCompleted(harness, AshenMissingScientistsV1.MissionId, AshenIndraCavernsV1.MissionId, AshenThresholdV1.MissionId, AshenVindicationV1.MissionId);
            var copy = IntoGotha(harness);
            Accept(harness, Giver(copy, F15.Drof), Digging.MissionId);
            Tick(harness, copy, 250);
            Lose(harness.Client, Digging.PowerSupplyTemplate);
            var box = SceneObject(harness, copy, Digging.MissionId, Digging.BoxRole);
            Assert.AreEqual(PlayerMessage.PmMissingReqItem, Facility.Refused(harness, copy, box, Digging.UseArgument), "no supply: refused");
            for (var tick = 0; tick < 2; tick++)
                Tick(harness, copy, 250);
            box = SceneObject(harness, copy, Digging.MissionId, Digging.BoxRole);
            Assert.AreEqual((MissionObjectiveState.Incomplete, (UseObjectState)Digging.SwitchReady, true, UseObjectState.DoorStateClosed),
                (Objective(harness, Digging.MissionId, Digging.PowerOn), box.StateId, box.IsEnabled, SceneObject(harness, copy, Digging.MissionId, Digging.DoorRole).StateId),
                "12 open, the box ready and usable, the door shut");
            for (var number = 1; number <= 2; number++)
                Assert.AreEqual((UseObjectState)Digging.TeleporterOff, SceneObject(harness, copy, Digging.MissionId, Digging.TeleporterRole(number)).StateId, $"teleporter {number} off");
        }

        /// <summary>
        /// The re-audit's console failed take: in a squad of two with a hand each, the mate's hand can not be taken at the
        /// player's console use (gone from his bag): the player's 7 completes, his hand is taken and his teleporter comes on; the
        /// mate's 7 stays open, his console ready and usable, his teleporter off.
        /// </summary>
        [TestMethod]
        public void DesistancesMateWhoseHandCanNotBeTakenKeepsHisConsole()
        {
            using var harness = WildernessRuntimeTestHarness.Create(lootRoll: (minimum, maximum) => maximum == 100 ? 0 : minimum);
            var done = new[] { AshenIndraCavernsV1.MissionId, AshenThresholdV1.MissionId, AshenVindicationV1.MissionId, Digging.MissionId, Meet.MissionId, Subversion.MissionId };
            AshenRuntime.SeedCompleted(harness, done.Prepend(AshenMissingScientistsV1.MissionId).ToArray());
            var copy = IntoGotha(harness);
            var naimi = Giver(copy, N.Naimi);
            Accept(harness, naimi, Desistance.MissionId);
            Tick(harness, copy, 250);
            using var mate = new Facility.SquadMate(harness, copy, naimi, Desistance.MissionId, done);
            UseAndTick(harness, copy, Desistance.DoorConsoleRole);
            UseAndTick(harness, copy, Desistance.ProgrammingRole);
            Enter(harness, ToVector(Desistance.DoorConsoleAt), V(Desistance.PadAt));
            mate.Beside(V(Desistance.PadAt) + new Vector3(0, 0, 3));
            Assert.IsTrue(mate.Client.MissionAreaService.RecordAcceptedMovement(mate.Client, ToVector(Desistance.DoorConsoleAt), V(Desistance.PadAt)), "the mate's pad");
            harness.Drain();
            UseAndTick(harness, copy, Desistance.ChargeRole);
            Facility.Stand(copy, N.KretasPool);
            var kretas = Here(copy).First(one => one.SpawnPool?.DbId == N.KretasPool && one.State != CharacterState.Dead);
            mate.Beside(kretas.Position + new Vector3(0, 0, 3));
            var corpse = Kill(harness, copy, N.Kretas);
            mate.Loot(corpse);
            PickUp(harness, corpse);
            harness.Drain();
            Tick(harness, copy, 250);
            Assert.AreEqual((1U, 1U), (Facility.Held(harness, Desistance.HandTemplate), mate.Held(Desistance.HandTemplate)), "a hand each");

            Lose(mate.Client, Desistance.HandTemplate);
            UseAndTick(harness, copy, Desistance.ConsoleRole);
            Assert.AreEqual((0U, MissionObjectiveState.Completed, (UseObjectState)Desistance.TeleporterOn),
                (Facility.Held(harness, Desistance.HandTemplate), Objective(harness, Desistance.MissionId, Desistance.Teleporter),
                    SceneObject(harness, copy, Desistance.MissionId, Desistance.TeleporterRole).StateId), "the player's hand taken, his 7 done, his teleporter on");
            var console = mate.Door(Desistance.ConsoleRole);
            Assert.AreEqual((MissionObjectiveState.Incomplete, (UseObjectState)Desistance.SwitchReady, true, (UseObjectState)Desistance.TeleporterOff),
                (mate.Of(Desistance.Teleporter), console.StateId, console.IsEnabled, mate.Door(Desistance.TeleporterRole).StateId),
                "the mate's take failed: his 7 open, his console ready and usable, his teleporter off");
        }

        /// <summary>
        /// Fix J (set 16 re-audit 1): 3 opens 6 together with 5. A member who has not walked into Central Administration (as one
        /// who shoots Kretas from outside its 16 m) gets the kill: his 6 completes, 7 opens, the hand drops (hidden 9 open); his 5
        /// stays open until he walks in, and the walk in completes 5 and leaves 6 done (5's own opening of 6 does nothing then);
        /// the console then takes the hand and completes 7. The same for a squad mate who has not walked in.
        /// </summary>
        [TestMethod]
        public void DesistancesKretasCountsBeforeCentralAdministration()
        {
            using var harness = WildernessRuntimeTestHarness.Create(lootRoll: (minimum, maximum) => maximum == 100 ? 0 : minimum);
            var done = new[] { AshenIndraCavernsV1.MissionId, AshenThresholdV1.MissionId, AshenVindicationV1.MissionId, Digging.MissionId, Meet.MissionId, Subversion.MissionId };
            AshenRuntime.SeedCompleted(harness, done.Prepend(AshenMissingScientistsV1.MissionId).ToArray());
            var copy = IntoGotha(harness);
            var naimi = Giver(copy, N.Naimi);
            Accept(harness, naimi, Desistance.MissionId);
            Tick(harness, copy, 250);
            using var mate = new Facility.SquadMate(harness, copy, naimi, Desistance.MissionId, done);
            UseAndTick(harness, copy, Desistance.DoorConsoleRole);
            UseAndTick(harness, copy, Desistance.ProgrammingRole);
            Enter(harness, ToVector(Desistance.DoorConsoleAt), V(Desistance.PadAt));
            mate.Beside(V(Desistance.PadAt) + new Vector3(0, 0, 3));
            Assert.IsTrue(mate.Client.MissionAreaService.RecordAcceptedMovement(mate.Client, ToVector(Desistance.DoorConsoleAt), V(Desistance.PadAt)), "the mate's pad");
            harness.Drain();
            UseAndTick(harness, copy, Desistance.ChargeRole);
            Assert.AreEqual((MissionObjectiveState.Completed, MissionObjectiveState.Incomplete, MissionObjectiveState.Incomplete, MissionObjectiveState.Inactive),
                (Objective(harness, Desistance.MissionId, Desistance.Generator), Objective(harness, Desistance.MissionId, Desistance.Administration),
                    Objective(harness, Desistance.MissionId, Desistance.DefeatKretas), Objective(harness, Desistance.MissionId, Desistance.Teleporter)), "3 done: 5 and 6 open, 7 waits");
            Assert.AreEqual((MissionObjectiveState.Incomplete, MissionObjectiveState.Incomplete), (mate.Of(Desistance.Administration), mate.Of(Desistance.DefeatKretas)), "the mate's 5 and 6 open");

            // Neither has walked into Central Administration (an area is entered by an accepted walk into it, MissionAreaService;
            // a shot from outside its 16 m is that case): the kill still counts for both (the mate in kill-credit reach).
            Facility.Stand(copy, N.KretasPool);
            var kretas = Here(copy).First(one => one.SpawnPool?.DbId == N.KretasPool && one.State != CharacterState.Dead);
            mate.Beside(kretas.Position + new Vector3(0, 0, 3));
            var corpse = Kill(harness, copy, N.Kretas);
            Assert.IsTrue(Holds(corpse, Desistance.HandTemplate), "the hand drops");
            Assert.AreEqual((MissionObjectiveState.Incomplete, MissionObjectiveState.Completed, MissionObjectiveState.Incomplete),
                (Objective(harness, Desistance.MissionId, Desistance.Administration), Objective(harness, Desistance.MissionId, Desistance.DefeatKretas),
                    Objective(harness, Desistance.MissionId, Desistance.Teleporter)), "the kill from outside counts: 6 done, 7 open, 5 still open");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Desistance.MissionId, Desistance.HandTaken), "9 open until the pickup");
            PickUp(harness, corpse);
            harness.Drain();
            Tick(harness, copy, 250);
            Assert.AreEqual((MissionObjectiveState.Incomplete, MissionObjectiveState.Completed, MissionObjectiveState.Incomplete),
                (mate.Of(Desistance.Administration), mate.Of(Desistance.DefeatKretas), mate.Of(Desistance.Teleporter)), "the mate's kill counts too");
            Assert.AreEqual((1U, MissionObjectiveState.Completed), (Facility.Held(harness, Desistance.HandTemplate), Objective(harness, Desistance.MissionId, Desistance.HandTaken)), "the hand");

            Enter(harness, V(Desistance.PadAt), V(Desistance.AdministrationAt));
            Tick(harness, copy, 250);
            Assert.AreEqual((MissionObjectiveState.Completed, MissionObjectiveState.Completed, MissionObjectiveState.Incomplete),
                (Objective(harness, Desistance.MissionId, Desistance.Administration), Objective(harness, Desistance.MissionId, Desistance.DefeatKretas),
                    Objective(harness, Desistance.MissionId, Desistance.Teleporter)), "the walk in: 5 done, 6 stays done, 7 open");
            UseAndTick(harness, copy, Desistance.ConsoleRole);
            Assert.AreEqual((0U, MissionObjectiveState.Completed, MissionObjectiveState.Incomplete),
                (Facility.Held(harness, Desistance.HandTemplate), Objective(harness, Desistance.MissionId, Desistance.Teleporter),
                    Objective(harness, Desistance.MissionId, Desistance.ReportToDiren)), "the console takes the hand: 7 done, 8 open");
        }

        /// <summary>
        /// Fix J's side effect (audit 2026-11-12): with 6 open at 3, a member can kill Kretas, use the console (7), take his
        /// teleporter to Diren and talk to him (8) with 5 still open (never walked into Central Administration); the mission is
        /// then not completeable and Diren does not pay; a walk back into Central Administration completes 5, and the turn-in
        /// pays (docs/ashen-playtest.md, Deviations).
        /// </summary>
        [TestMethod]
        public void DesistancesTurnInWaitsForCentralAdministration()
        {
            using var harness = WildernessRuntimeTestHarness.Create(lootRoll: (minimum, maximum) => maximum == 100 ? 0 : minimum);
            AshenRuntime.SeedCompleted(harness, AshenMissingScientistsV1.MissionId, AshenIndraCavernsV1.MissionId, AshenThresholdV1.MissionId,
                AshenVindicationV1.MissionId, Digging.MissionId, Meet.MissionId, Subversion.MissionId);
            var copy = IntoGotha(harness);
            Accept(harness, Giver(copy, N.Naimi), Desistance.MissionId);
            Tick(harness, copy, 250);
            UseAndTick(harness, copy, Desistance.DoorConsoleRole);
            UseAndTick(harness, copy, Desistance.ProgrammingRole);
            Enter(harness, ToVector(Desistance.DoorConsoleAt), V(Desistance.PadAt));
            UseAndTick(harness, copy, Desistance.ChargeRole);
            Facility.Stand(copy, N.KretasPool);
            var corpse = Kill(harness, copy, N.Kretas);
            PickUp(harness, corpse);
            harness.Drain();
            Tick(harness, copy, 250);
            UseAndTick(harness, copy, Desistance.ConsoleRole);
            var teleporter = SceneObject(harness, copy, Desistance.MissionId, Desistance.TeleporterRole);
            Use(harness, copy, teleporter, Desistance.UseArgument);
            Tick(harness, copy, 250);
            Teleported(harness, ToVector(Desistance.DirenArrival), "taken beside Diren");
            var diren = Giver(copy, F15.Diren);
            CollectionAssert.AreEqual(new[] { Desistance.ReportToDiren }, Topics(harness, diren, Desistance.MissionId), "Diren hears 8");
            Talk(harness, diren, Desistance.MissionId, Desistance.ReportToDiren);
            Tick(harness, copy, 250);
            Assert.AreEqual((MissionObjectiveState.Incomplete, MissionObjectiveState.Completed, MissionObjectiveState.Completed, MissionObjectiveState.Completed, false),
                (Objective(harness, Desistance.MissionId, Desistance.Administration), Objective(harness, Desistance.MissionId, Desistance.DefeatKretas),
                    Objective(harness, Desistance.MissionId, Desistance.Teleporter), Objective(harness, Desistance.MissionId, Desistance.ReportToDiren),
                    harness.Client.Player.Missions[Desistance.MissionId].Completeable), "6, 7, 8 done with 5 open: not completeable");
            diren = Giver(copy, F15.Diren);
            harness.MoveTo(diren.Position);
            harness.Drain();
            var paid = harness.Context.ReadRewardTotals();
            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, diren.EntityId, Desistance.MissionId, 0), "Diren does not pay with 5 open");
            Assert.AreEqual(paid, harness.Context.ReadRewardTotals(), "nothing paid");
            harness.Drain();

            Enter(harness, V(Desistance.PadAt), V(Desistance.AdministrationAt));
            Tick(harness, copy, 250);
            Assert.AreEqual((MissionObjectiveState.Completed, true),
                (Objective(harness, Desistance.MissionId, Desistance.Administration), harness.Client.Player.Missions[Desistance.MissionId].Completeable),
                "the walk back in: 5 done, completeable");
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, Giver(copy, F15.Diren), Desistance.MissionId, 0, Desistance.Experience, Desistance.Credits);
        }

        /// <summary>A take that fails: the item gone from the bag in memory only (a mission item can not be destroyed by a player).</summary>
        private static void Lose(Client client, uint template)
        {
            var inventory = client.Player.Inventory.PersonalInventory;
            for (var slot = 0; slot < inventory.Count; slot++)
                if (inventory[slot] != 0 && EntityManager.Instance.GetItem(inventory[slot])?.ItemTemplate.ItemTemplateId == template)
                {
                    inventory[slot] = 0;
                    return;
                }
            Assert.Fail($"{template} not held");
        }

        /// <summary>
        /// Ashen_gotha_plant takes out 1447, 1532, 1451 and 1452, and Drof's and Naimi's second rows and Kretas (creature, stats,
        /// look, packages) with the set's three pools; the rows they copy (Harris, Mitecht) stay; it updates nothing. Every
        /// table the migration writes is counted (set 16's ids and missions only). Nothing else changes: the other pools,
        /// missions and markers as a database migrated only up to Ashen_gotha_plant holds them. Up again, all of it is back; the
        /// NPC SQL run again changes nothing.
        /// </summary>
        [TestMethod]
        public void GothaPlantRollsBackWithItsMigration()
        {
            using var harness = WildernessRuntimeTestHarness.Create(targetWorldMigration: AshenMissionTests.GothaPlant);
            var world = harness.World;
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking()
                .Any(entry => entry.MissionId == mission && entry.ContentRevision == AshenMissionDataV1.Revision);
            int Count(string sql) => world.Database.SqlQueryRaw<int>(sql).AsEnumerable().Single();
            const string ids = "(mission_id IN (1447, 1532, 1451, 1452) AND content_revision = 'ashen_1_16_5')";
            const string npcs = "790600, 790601, 790602, 790603";
            var tables = new[]
            {
                "mission_content_definition", "mission_objective_definition", "mission_objective_transition", "mission_trigger", "mission_action",
                "mission_prerequisite", "mission_reward_definition", "mission_reward_item", "mission_indicator", "mission_scene_binding", "mission_evidence",
                "mission_scenario", "mission_scenario_step", "mission_channel_policy", "mission_repeat_policy", "mission_spawn", "mission_spawn_group", "mission_area"
            };
            string All() => string.Join(", ", tables.Select(table => $"{table} {Count($"SELECT COUNT(*) AS Value FROM {table} WHERE {ids}")}")
                .Concat(new[] { "creature", "creature_stat", "creature_appearance", "npc_package", "spawnpool" }
                    .Select(table => $"{table} {Count($"SELECT COUNT(*) AS Value FROM {table} WHERE id IN ({npcs})")}"))
                .Append($"kept {Count($"SELECT COUNT(*) AS Value FROM creature WHERE id IN ({N.Harris}, {N.Mitecht})")}"));
            static string Others(DbContext db) => string.Join("; ", db.Set<SpawnPoolEntry>().AsNoTracking()
                    .Where(entry => (entry.MapContextId == AshenMissionDataV1.MapContextId || entry.MapContextId == N.Caverns) &&
                        (entry.Id < 790600 || entry.Id > 790639)).OrderBy(entry => entry.Id).AsEnumerable()
                    .Select(entry => FormattableString.Invariant($"{entry.Id}:{entry.PosX:R},{entry.PosY:R},{entry.PosZ:R},{entry.Radius:R},{entry.Creature1Id},{entry.Creature1MaxCount}")))
                + " | " + string.Join("; ", db.Set<MissionContentDefinitionEntry>().AsNoTracking()
                    .Where(entry => entry.MissionId != 1447 && entry.MissionId != 1532 && entry.MissionId != 1451 && entry.MissionId != 1452)
                    .OrderBy(entry => entry.MissionId).ThenBy(entry => entry.ContentRevision)
                    .AsEnumerable().Select(entry => $"{entry.MissionId}/{entry.ContentRevision}"))
                + " | " + string.Join("; ", db.Set<MissionIndicatorEntry>().AsNoTracking()
                    .Where(entry => (entry.ContentRevision == AshenMissionDataV1.Revision || entry.ContentRevision == CrucibleMissionDataV1.Revision)
                        && entry.MissionId != 1447 && entry.MissionId != 1532 && entry.MissionId != 1451 && entry.MissionId != 1452)
                    .OrderBy(entry => entry.MissionId).ThenBy(entry => entry.ObjectiveId).ThenBy(entry => entry.IndicatorId).AsEnumerable()
                    .Select(entry => FormattableString.Invariant($"{entry.MissionId}.{entry.ObjectiveId}/{entry.IndicatorId}:{entry.PosX:R},{entry.PosZ:R}")));
            string others;
            using (var atGotha = WildernessRuntimeTestHarness.Create(targetWorldMigration: AshenMissionTests.GothaPlant))
                others = Others(atGotha.World);
            // 1447: 13 objectives (12-14 hidden; 1 and 5 aggregates), 11 transitions; triggers: the box, the area out, the opener's
            // ObjectiveState, the two areas, the six drills; actions: the supply taken and the power's scenario (12), reveal and
            // activate (14, 2), the reward (3, 6-11); scenarios 1-3 (1 one step, 3 six); three areas; radio completion. 1532: one
            // objective, transition, talk and reward action; the radio offer. 1451: 8 objectives (1, 2 aggregates), six
            // transitions, triggers and reward actions; scenario 1 with six steps; turned in at Drof (no channel row). 1452: 9 objectives, nine
            // transitions and triggers; actions: reveal and activate after 1-7 (14), the bomb and the hand taken, the generator's and
            // the teleporter's scenarios, the reward; scenarios 1-6 (1-4 one step); two areas. Each one prerequisite, one reward (four items), three
            // evidence rows; markers 10, 1, 8, 8; four scenes (1532's holds its marker's map). Drof and Naimi: a creature, stats,
            // Harris' six look rows and a package each; Kretas a creature and stats; three pools.
            const string up = "mission_content_definition 4, mission_objective_definition 31, mission_objective_transition 27, mission_trigger 27, " +
                "mission_action 39, mission_prerequisite 4, mission_reward_definition 4, mission_reward_item 16, mission_indicator 27, " +
                "mission_scene_binding 4, mission_evidence 12, mission_scenario 10, mission_scenario_step 17, mission_channel_policy 2, " +
                "mission_repeat_policy 0, mission_spawn 0, mission_spawn_group 0, mission_area 5, creature 3, creature_stat 3, " +
                "creature_appearance 12, npc_package 2, spawnpool 3, kept 2";
            const string down = "mission_content_definition 0, mission_objective_definition 0, mission_objective_transition 0, mission_trigger 0, " +
                "mission_action 0, mission_prerequisite 0, mission_reward_definition 0, mission_reward_item 0, mission_indicator 0, " +
                "mission_scene_binding 0, mission_evidence 0, mission_scenario 0, mission_scenario_step 0, mission_channel_policy 0, " +
                "mission_repeat_policy 0, mission_spawn 0, mission_spawn_group 0, mission_area 0, creature 0, creature_stat 0, " +
                "creature_appearance 0, npc_package 0, spawnpool 0, kept 2";
            void AllUp(string when)
            {
                Assert.IsTrue(Missions.All(Has), $"{when}: the four missions");
                Assert.AreEqual(up, All(), when);
                Assert.AreEqual(others, Others(world), $"{when}: nothing else changes");
            }

            AllUp("at its migration");
            world.GetService<IMigrator>().Migrate(AshenMissionTests.Previous(world, AshenMissionTests.GothaPlant));
            world.ChangeTracker.Clear();
            Assert.IsFalse(Missions.Any(Has));
            Assert.AreEqual(down, All());
            Assert.AreEqual(others, Others(world), "Down: nothing else changes");
            Assert.IsTrue(AshenMissionTests.MissionIds.Where(id => !Missions.Contains(id) && !AshenAvernusOutpostTests.Missions.Contains(id) &&
                !AshenAvernusHiveTests.Missions.Contains(id) && !AshenConscriptionEntryTests.Missions.Contains(id) &&
                !AshenConscriptionQueenTests.Missions.Contains(id)).All(Has), "sets 1-15 stay");
            foreach (var sql in N.Down())
                world.Database.ExecuteSqlRaw(CrucibleSql.Guard(sql));
            Assert.AreEqual(down, All(), "a second Down changes nothing");

            world.GetService<IMigrator>().Migrate(AshenMissionTests.GothaPlant);
            world.ChangeTracker.Clear();
            AllUp("Up again");
            foreach (var sql in N.Down())
                world.Database.ExecuteSqlRaw(CrucibleSql.Guard(sql));
            foreach (var sql in N.Up())
                world.Database.ExecuteSqlRaw(CrucibleSql.Guard(sql));
            AllUp("the NPC rows run again");
        }

        /// <summary>
        /// The squad's copy of Indra Caverns, entered through its door: its pools' dead and cleared marks of an earlier test gone
        /// (set 15's and set 16's), and an earlier test's scene objects too.
        /// </summary>
        internal static MapChannel IntoGotha(WildernessRuntimeTestHarness harness)
        {
            var copy = Facility.IntoCaverns(harness);
            foreach (var pool in copy.SpawnPools.Where(pool => Pools.Contains(pool.DbId)))
                pool.ClearedAtUtcMs = 0;
            foreach (var creature in Here(copy).Where(one => Pools.Contains(one.SpawnPool?.DbId ?? 0)).ToArray())
                if (creature.State == CharacterState.Dead)
                    Respawn(copy, creature.SpawnPool.DbId);
            harness.Drain();
            return copy;
        }

        /// <summary>The set's places: label, place, whether it is seen from the ring, the height looked at, its own pool (0: none), its clearance.</summary>
        internal static List<(string Label, Vector3 At, bool Ring, float Middle, uint Own, float Clearance)> Places()
        {
            var places = new List<(string, Vector3, bool, float, uint, float)>
            {
                ("Drof at the plant", V(N.DrofAt), true, F.NpcMiddle, 0, Clearance), ("Naimi at the plant", V(N.NaimiAt), true, F.NpcMiddle, 0, Clearance),
                ("Overlord Kretas", V(N.KretasAt), true, F.NpcMiddle, N.KretasPool, KretasClearance),
                ("the Power Supply Box", ToVector(Digging.BoxAt), true, F.ObjectMiddle, 0, Clearance),
                ("teleporter 1", ToVector(Digging.TeleportersAt[0]), true, F.ObjectMiddle, 0, Clearance),
                ("teleporter 2", ToVector(Digging.TeleportersAt[1]), true, F.ObjectMiddle, 0, Clearance),
                ("the rear exit door", ToVector(Digging.DoorAt), false, 0, 0, Clearance),
                ("the rear arrival", ToVector(Digging.RearArrival), false, 0, 0, Clearance)
            };
            for (var number = 1; number <= Digging.Drills; number++)
                places.Add(($"drill {number}", ToVector(DrillAt(number)), true, F.ObjectMiddle, 0, Clearance));
            for (var number = 1; number <= Subversion.ValveCount; number++)
                places.Add(($"valve {number}", ToVector(Subversion.ValvesAt[number - 1]), true, F.ObjectMiddle, 0, ValveClearances[number - 1]));
            for (var number = 1; number <= Subversion.SupportCount; number++)
                places.Add(($"life support {number}", ToVector(Subversion.SupportsAt[number - 1]), true, F.ObjectMiddle, 0, SupportClearance));
            places.Add(("the door console", ToVector(Desistance.DoorConsoleAt), true, F.ObjectMiddle, 0, Clearance));
            places.Add(("the programming console", ToVector(Desistance.ProgrammingAt), true, F.ObjectMiddle, 0, OperationsClearance));
            places.Add(("the systems console", ToVector(Desistance.SystemsAt), true, F.ObjectMiddle, 0, OperationsClearance));
            places.Add(("the generator", ToVector(Desistance.GeneratorAt), false, 0, 0, PadClearance));
            // Fix J (row 43): on the generator's east face, 5.8 m outside pad pool 531085's circle (a rule break: the generator's
            // body leaves no place by it further off).
            places.Add(("the generator's charge", ToVector(AshenFixJV1.ChargeAt), true, F.ObjectMiddle, 0, 5.5f));
            places.Add(("the Teleportation Console", ToVector(Desistance.ConsoleAt), true, F.ObjectMiddle, 0, AdministrationClearance));
            places.Add(("Kretas' teleporter", ToVector(Desistance.TeleporterAt), true, F.ObjectMiddle, 0, AdministrationClearance));
            places.Add(("the arrival beside Diren", ToVector(Desistance.DirenArrival), false, 0, 0, Clearance));
            return places.Select(place => ((string Label, Vector3 At, bool Ring, float Middle, uint Own, float Clearance))place).ToList();
        }

        /// <summary>How far outside every hostile pool's circle a place stands (2D; 30 m walk clearance and a margin).</summary>
        private const float Clearance = 34;
        /// <summary>
        /// The places inside the Gotha plant whose rooms hold the plant's own pools (rule breaks, docs/ashen-playtest.md): Kretas
        /// (fix F's 35 m boss rule), the valves (the processing rooms' pools 531088 and 531091), the life support systems (the
        /// barracks' 531087), Juggernaut Operations (the bay's 531085), the pad deck (531085 under it, 531092 and 531093 on it),
        /// Central Administration's west end (the Command Center's 531095).
        /// </summary>
        private const float KretasClearance = 35, SupportClearance = 25, OperationsClearance = 20, PadClearance = 13, AdministrationClearance = 20;
        private static readonly float[] ValveClearances = { 18, 12 };

        /// <summary>
        /// The legs of the set's walks on the client's floor, each walked there and back on its own (corners: a client-floor
        /// flood's way, 2026-11-11, the climb rule's, ESTIMATE of a player's line). A walk is a run of legs.
        /// </summary>
        internal static readonly Dictionary<string, Vector3[]> Legs = new()
        {
            ["Drof to the box"] = new Vector3[] { new(-5, 0.58f, -221), new(-9, 0.5f, -221) },
            // Fix J (row 43): the teleporters' bodies (prop_brann_teleporter_v01) are solid to the client; the walks end beside them.
            ["Drof to teleporter 1"] = new Vector3[] { new(-5, 0.58f, -221), new(-9.5f, 0.5f, -216) },
            ["Drof to teleporter 2"] = new Vector3[] { new(-5, 0.58f, -221), new(-3.5f, 0.5f, -224.5f) },
            ["the Control Room to the east hall"] = new Vector3[] { new(-5, 0.58f, -221), new(-2, 0.5f, -209), new(1.25f, 0.5f, -205.25f), new(3, 0.5f, -204), new(21, 4.5f, -204), new(23, 4.5f, -205), new(26, 5.01f, -211), new(26.5f, 8.5f, -226.34f), new(29, 8.5f, -232), new(43.5f, 8.5f, -228.5f), new(46, 8.5f, -226), new(45.75f, 8.5f, -224.75f), new(43, 8.5f, -221), new(43, 8.5f, -218.75f), new(51, 8.5f, -218), new(63, 8.5f, -218) },
            ["the east hall to the rear ramp, part 1"] = new Vector3[] { new(63, 8.5f, -218), new(68, 8.5f, -215) },
            ["the east hall to the rear ramp, part 2"] = new Vector3[] { new(68, 8.5f, -215), new(74.53f, 8.5f, -216.63f), new(90.6f, 8.5f, -220.6f), new(96.57f, 8.5f, -226.75f) },
            ["the east hall to the rear ramp, part 3"] = new Vector3[] { new(96.57f, 8.5f, -226.75f), new(98, 9.59f, -228) },
            ["the east hall to the rear ramp, part 4"] = new Vector3[] { new(98, 9.59f, -228), new(99.13f, 8.5f, -229.61f), new(103.25f, 8.5f, -237.17f), new(104, 8.5f, -239) },
            ["the east hall to the rear ramp, part 5"] = new Vector3[] { new(104, 8.5f, -239), new(105.5f, 8.5f, -241), new(111, 8.5f, -255), new(115, 8.5f, -256) },
            ["the rear ramp to outside the rear exit"] = new Vector3[] { new(115, 8.5f, -256), new(137, 12.5f, -253), new(141.25f, 12.5f, -249), new(137.88f, 16.5f, -216.78f), new(139, 16.5f, -211), new(140, 16.02f, -205) },
            ["outside the rear exit to the rear caves"] = new Vector3[] { new(140, 16.02f, -205), new(139, 15.93f, -198), new(131, 15.93f, -191), new(127, 16.45f, -190), new(86, 25.56f, -145), new(82.75f, 26.24f, -142.5f), new(79, 26.68f, -140), new(72, 28.43f, -139) },
            ["the rear caves to the cave entrance"] = new Vector3[]
            {
                new(72, 28.43f, -139), new(48, 33.9f, -120), new(33, 41.7f, -108), new(24, 45.56f, -104), new(4, 46.18f, -94)
            },
            ["the cave entrance to the tunnel"] = new Vector3[] { new(4, 46.18f, -94), new(-7, 52.68f, -94), new(-31, 61.93f, -106), new(-42.13f, 63.57f, -110.22f), new(-65, 60.94f, -114), new(-78.55f, 63.1f, -116.8f), new(-85, 64.87f, -118), new(-95.67f, 62.36f, -114.5f), new(-101, 63.61f, -115) },
            ["the tunnel to its bend, part 1"] = new Vector3[] { new(-101, 63.61f, -115), new(-133, 53.59f, -111), new(-157, 54.65f, -121), new(-181, 56.63f, -126), new(-191, 55.16f, -123), new(-196, 54.31f, -117), new(-232, 56.34f, -106), new(-238.5f, 56.09f, -101), new(-255.5f, 57.75f, -93.87f), new(-262, 61.55f, -84), new(-262, 64.15f, -68), new(-253, 62.31f, -60), new(-250.83f, 63.12f, -49.33f), new(-252, 63.19f, -44), new(-251.13f, 61.06f, -36.83f), new(-253.5f, 63.3f, -20), new(-255.43f, 63.65f, -16.25f), new(-257, 65.36f, 8) },
            ["the tunnel to its bend, part 2"] = new Vector3[] { new(-257, 65.36f, 8), new(-262, 66.56f, 12), new(-296.78f, 63.14f, 21.13f), new(-302, 63.31f, 23) },
            ["the tunnel's last turn to its exit"] = new Vector3[] { new(-302, 63.31f, 23), new(-312.92f, 63.79f, 22.59f), new(-325, 64.12f, 24) },
            ["the tunnel's exit to the north cavern"] = new Vector3[]
            {
                new(-325, 64.12f, 24), new(-345, 66.5f, 32), new(-345, 67.5f, 44), new(-345, 70.1f, 53), new(-350, 71.43f, 58)
            },
            ["the north cavern to drill 1"] = new Vector3[] { new(-350, 71.43f, 58), new(-371.65f, 81.74f, 114.47f), new(-393.29f, 86.81f, 170.94f), new(-399, 87.43f, 183), new(-399.22f, 86.96f, 198.13f), new(-389, 90.18f, 253), new(-368.88f, 92.85f, 273.22f), new(-358, 90.93f, 278), new(-354, 92.43f, 276), new(-327, 90.31f, 239) },
            ["drill 1 to drill 2"] = new Vector3[] { new(-327, 90.31f, 239), new(-326, 90.43f, 259) },
            ["drill 2 to drill 3"] = new Vector3[] { new(-326, 90.43f, 259), new(-338, 90.43f, 270) },
            // Fix J (to-fix row 38): drills 4-6 out of the lava river, each walked to from drill 1 on the client floor west of it.
            ["drill 1 to drill 4"] = new Vector3[] { new(-327, 90.31f, 239), new(-337, 90.43f, 264) },
            ["drill 1 to drill 5"] = new Vector3[] { new(-327, 90.31f, 239), new(-339, 90.43f, 276) },
            ["drill 1 to drill 6"] = new Vector3[] { new(-327, 90.31f, 239), new(-341, 90.56f, 282) },
            ["the north cavern to the plant's entrance"] = new Vector3[] { new(-350, 71.43f, 58), new(-371.65f, 81.74f, 114.47f), new(-393.29f, 86.81f, 170.94f), new(-399, 87.43f, 183), new(-399.73f, 85.74f, 241.18f), new(-373.33f, 90.81f, 289.67f), new(-356.71f, 90.56f, 304.59f), new(-352, 92.08f, 311), new(-346.55f, 91.99f, 313.89f), new(-336, 92.77f, 319), new(-324, 94.18f, 322), new(-285, 115.31f, 282), new(-281.75f, 117.28f, 276), new(-270.36f, 136.45f, 217.82f), new(-266, 136.56f, 196), new(-260.46f, 134.43f, 134.46f), new(-257, 128.81f, 96), new(-256.33f, 128.93f, 37.33f), new(-278.5f, 128.56f, -14.5f), new(-301, 128.93f, -37), new(-304.13f, 129.48f, -40.78f), new(-309, 129.55f, -53) },
            ["Drof to Naimi at the plant"] = new Vector3[] { new(-309, 129.55f, -53), new(-306, 129.55f, -57) },
            ["the plant 01: (-309, -53) to (-309, -93)"] = new Vector3[] { new(-309, 129.55f, -53), new(-309, 129.6f, -93) },
            ["the plant 02: (-309, -93) to (-143, -167)"] = new Vector3[] { new(-309, 129.6f, -93), new(-294, 129.61f, -108), new(-274, 129.61f, -116), new(-264, 129.61f, -126), new(-263.66f, 129.61f, -150.14f), new(-263.31f, 129.61f, -174.28f), new(-261.42f, 129.61f, -197.75f), new(-245.58f, 129.61f, -215.25f), new(-243.33f, 129.61f, -218.62f), new(-220.25f, 129.61f, -216.38f), new(-202.89f, 129.61f, -212.67f), new(-191.5f, 129.61f, -201), new(-187.36f, 129.15f, -189.18f), new(-181.5f, 127.62f, -169.3f), new(-175.5f, 128.18f, -162.5f), new(-171.5f, 130, -164), new(-168, 132.67f, -173), new(-162.61f, 134.81f, -175.35f), new(-160.5f, 135.52f, -176), new(-146.33f, 137.59f, -168.33f), new(-143, 137.59f, -167) },
            ["the plant 03: (-309, -53) to (-347, -181)"] = new Vector3[] { new(-309, 129.55f, -53), new(-309, 129.58f, -77.12f), new(-312.17f, 129.6f, -97.33f), new(-313.4f, 129.6f, -99.35f), new(-329.25f, 129.6f, -116.08f), new(-335.5f, 129.6f, -126.25f), new(-338.45f, 129.6f, -150.36f), new(-341.41f, 129.6f, -174.23f), new(-343.5f, 129.72f, -180), new(-345.71f, 129.85f, -181.71f), new(-347, 130.1f, -181) },
            ["the plant 04: (-347, -181) to (-394, -159)"] = new Vector3[] { new(-347, 130.1f, -181), new(-348.9f, 129.6f, -182.73f), new(-372.57f, 129.1f, -182.14f), new(-383.5f, 128.18f, -180.33f), new(-391, 124.6f, -172), new(-394, 122.2f, -167), new(-394.65f, 121.72f, -163), new(-394, 121, -152), new(-394.35f, 121, -150), new(-397, 121, -142), new(-408, 120.85f, -132.5f), new(-409.5f, 120.78f, -130.75f), new(-410.95f, 121.47f, -117.64f), new(-411, 122.4f, -104), new(-403.67f, 126.67f, -96.33f), new(-397.38f, 128.6f, -91.08f), new(-387, 129.1f, -89), new(-382.75f, 130.22f, -89.8f), new(-377, 131.9f, -91), new(-371, 135.43f, -97.67f), new(-366.96f, 136.57f, -103.62f), new(-365.95f, 136.78f, -114), new(-370, 137.2f, -131), new(-375.5f, 137.15f, -135.5f), new(-382.67f, 137.08f, -147.33f), new(-384.08f, 137.07f, -148.67f), new(-391, 137, -154), new(-393.1f, 136.85f, -156.77f), new(-394, 136.7f, -159) },
            ["the plant 05: (-394, -159) to (-394.5, -165)"] = new Vector3[] { new(-394, 136.7f, -159), new(-393.88f, 136.81f, -163), new(-394.5f, 136.85f, -165) },
            ["the plant 06: (-394.5, -165) to (-415, -237)"] = new Vector3[] { new(-394.5f, 136.85f, -165), new(-395.09f, 136.88f, -166.5f), new(-397.5f, 137, -186.5f), new(-398.75f, 137, -188.25f), new(-407, 136.9f, -194.5f), new(-408.5f, 136.85f, -196.25f), new(-410.32f, 136.91f, -199.46f), new(-415, 137.59f, -237) },
            ["the plant 07: (-309, -53) to (-339, -197)"] = new Vector3[] { new(-309, 129.55f, -53), new(-310, 129.6f, -94.5f), new(-313.43f, 129.6f, -99.57f), new(-327.79f, 129.6f, -113.72f), new(-336, 129.6f, -127), new(-336, 129.6f, -157), new(-333.5f, 130.28f, -160.25f), new(-333, 130.5f, -162), new(-337.65f, 129.62f, -178.4f), new(-339, 129.8f, -197) },
            ["the plant 08: (-339, -197) to (-330, -206)"] = new Vector3[] { new(-339, 129.8f, -197), new(-336, 129.5f, -201.25f), new(-331.5f, 128.65f, -205.25f), new(-330, 128.4f, -206) },
            ["the plant 09: (-330, -206) to (-157, -382)"] = new Vector3[] { new(-330, 128.4f, -206), new(-277.25f, 129.61f, -259), new(-265, 129.61f, -268), new(-255, 129.61f, -278), new(-247, 127.62f, -298), new(-237.25f, 127.82f, -307), new(-217, 129.61f, -314), new(-190.5f, 129.61f, -341.75f), new(-177, 127.98f, -342), new(-162, 127.62f, -357), new(-161.25f, 128, -359), new(-158.75f, 129.61f, -377), new(-157, 129.6f, -382) },
            // Fix J (to-fix row 42, the real client 2026-11-12 10:5x): life support 1 stands on the upper deck (137.11), over life
            // support 2's (131.58). Set 16's legs went (-157, -414.5) > (-172, -416): that start lies 1.9 m north of the ramp's foot,
            // beside the ramp, and the client stayed on the lower floor. The way up is the ramp along z -416 from its foot at x
            // -154.7 (floor 129.8), west to the deck at x -166 (the tester's way, LS1 used 10:51); the legs go round its east end.
            ["the plant 10: (-157, -382) to the ramp's foot (-152, -416.4)"] = new Vector3[] { new(-157, 129.6f, -382), new(-152, 129.61f, -410), new(-152, 129.61f, -416.4f) },
            ["the plant 11: up the ramp from its foot (-152, -416.4) to (-172, -416)"] = new Vector3[] { new(-152, 129.61f, -416.4f), new(-154.71f, 129.82f, -416.4f), new(-158, 132.4f, -416.4f), new(-162, 135.2f, -416.4f), new(-166, 137.1f, -416.4f), new(-172, 137.11f, -416) },
            ["the plant 12: (-309, -53) to (-172, -416)"] = new Vector3[] { new(-309, 129.55f, -53), new(-310, 129.61f, -96), new(-328, 129.61f, -114.75f), new(-336, 129.61f, -126), new(-336, 129.61f, -156), new(-333, 130.46f, -161), new(-337.5f, 129.61f, -177.75f), new(-339, 129.81f, -196), new(-336, 129.57f, -201), new(-277.25f, 129.61f, -259), new(-266, 129.61f, -267), new(-256, 129.61f, -277), new(-254.88f, 128.07f, -281.78f), new(-256, 128.53f, -287), new(-243, 127.62f, -302), new(-238.22f, 127.62f, -306.13f), new(-218, 129.61f, -313), new(-190.72f, 129.61f, -340.13f), new(-188, 129.61f, -341), new(-177.88f, 127.98f, -340.78f), new(-160, 127.62f, -359), new(-157, 129.61f, -386), new(-168.75f, 131.58f, -411), new(-171.13f, 131.58f, -412.78f), new(-172, 131.58f, -416) },
            ["the plant 13: (-309, -93) to (-338, -193)"] = new Vector3[] { new(-309, 129.6f, -93), new(-312, 129.6f, -98), new(-327, 129.6f, -113), new(-335.44f, 129.69f, -125.81f), new(-335.5f, 129.7f, -126.5f), new(-336, 129.6f, -153), new(-333.5f, 130.05f, -160.5f), new(-333.45f, 130.01f, -160.75f), new(-338, 129.6f, -178), new(-338, 129.6f, -193) },
            ["the plant 14: (-338, -193) to (-339, -198)"] = new Vector3[] { new(-338, 129.6f, -193), new(-339, 129.6f, -198) },
            ["the plant 15: (-339, -198) to (-116, -343)"] = new Vector3[] { new(-339, 129.6f, -198), new(-333.58f, 129.13f, -203.43f), new(-288.27f, 128.02f, -247.91f), new(-277.25f, 129.61f, -259), new(-264, 129.61f, -269), new(-254.35f, 129.61f, -278.63f), new(-246, 127.62f, -299), new(-241, 127.62f, -304), new(-236.43f, 128, -307.25f), new(-216, 129.61f, -315), new(-191, 129.61f, -340), new(-187.25f, 129.02f, -341), new(-167.25f, 127.62f, -338.75f), new(-161, 127.62f, -336.5f), new(-153.5f, 127.62f, -339.5f), new(-146, 127.62f, -341), new(-139.21f, 127.99f, -340.78f), new(-116, 129.6f, -343) },
            ["the plant 16: (-116, -343) to (-86, -342)"] = new Vector3[] { new(-116, 129.6f, -343), new(-108.13f, 129.6f, -341.75f), new(-89.75f, 129.6f, -341.75f), new(-86, 129.6f, -342) },
            ["the plant 17: (-86, -342) to (-87, -362)"] = new Vector3[] { new(-86, 129.6f, -342), new(-85, 131.2f, -344.5f), new(-85, 137.1f, -357), new(-87, 137.11f, -362) },
            ["the plant 18: (-116, -343) to (-87, -359)"] = new Vector3[] { new(-116, 129.6f, -343), new(-91, 131, -354), new(-87, 131.58f, -359) },
            ["the plant 19: (-330, -206) to (-300, -224)"] = new Vector3[] { new(-330, 128.4f, -206), new(-300, 128.01f, -224) },
            ["the plant 20: (-338, -193) to (-395, -246)"] = new Vector3[] { new(-338, 129.6f, -193), new(-343.5f, 129.4f, -200.5f), new(-346, 129.2f, -203), new(-383.5f, 128, -235.75f), new(-391, 128.01f, -244.5f), new(-395, 128.01f, -246) },
            ["the plant 21: (-339, -198) to (-236, -324)"] = new Vector3[] { new(-339, 129.6f, -198), new(-337.57f, 128.51f, -248.34f), new(-337, 128.52f, -270.5f), new(-337, 128.51f, -278), new(-333, 128.03f, -293), new(-337, 129.61f, -338), new(-327, 129.61f, -348), new(-312, 131.7f, -360), new(-307, 133.61f, -364), new(-294.5f, 137.61f, -354), new(-277, 143, -345), new(-272, 146, -346), new(-270, 148.74f, -351), new(-271, 152, -356), new(-273, 153.42f, -357.25f), new(-276, 155.64f, -358), new(-279.75f, 157.78f, -356.5f), new(-281.5f, 158.79f, -355), new(-283, 161.45f, -350), new(-281, 163.01f, -348), new(-278.13f, 165.26f, -345.78f), new(-258, 169.61f, -349), new(-248, 169.61f, -339), new(-236, 171.7f, -324) },
            // Fix J (row 44, the real client 2026-11-12): from the ramp's top the client climbs to the landing at its east end
            // ((-231.9, -320.8), a 0.54 m edge), not over its side; the tester's way.
            ["the plant 22: (-236, -324) to (-232, -319)"] = new Vector3[] { new(-236, 171.7f, -324), new(-233.11f, 172.7f, -321.2f), new(-232.71f, 172.87f, -321.2f), new(-231.91f, 173.1f, -320.8f), new(-231.91f, 173.6f, -319.2f) },
            // Fix K (row 44, the real client 2026-11-12, deaths 7 and 8): fix J's way south round the trench ran 2-7 m outside pool
            // 531092's circle, through its spawn ground. The way to the pad goes north round the landing pad's dish instead, 51.2 m
            // and more from 531092's middle (25 m outside its circle, AshenFixKTests): from the bridge's west end (-289, -287) onto
            // the deck, north along the dish's east rim, west along its north rim, south along its west rim to the pad.
            ["the plant 23: (-232, -319) to (-289, -287)"] = new Vector3[] { new(-231.91f, 173.6f, -319.2f), new(-241.91f, 177.5f, -307.2f), new(-247.11f, 177.61f, -301.2f), new(-250.18f, 177.61f, -298.88f), new(-257.28f, 177.61f, -291.88f), new(-260, 177.61f, -291), new(-264.78f, 177.61f, -289.88f), new(-289.13f, 177.61f, -286.65f) },
            // Fix N (row 50): fix K's way kept the pit's trench rim 0.25-0.5 m from the pit (5 m down) at (-362, -265) and (-378, -274..-296);
            // the real client fell in. From (-351, -260) (fix K's corner) the way now keeps 2.25 m and more from the pit's edge on the
            // rim (AshenFixBTests.PadDeckEdgeMargin 2 m, checked; a 0.5 m grid search, 2026-11-13, found a way 3 m from the edge at
            // every grid point) round its north-west corner, then south on the terrain west of the rim (x -391, 2 m east of the
            // generator's body, 14 m from the pit). Pool 531093's spawn ground is the deck north of the rim: the way passes it as
            // close as fix K's did (4.8 m at (-351, -260)); a way 8 m from it squeezes past the pit's edge at (-347, -268.5).
            // Fix J (row 43): the Outpost Generator's body (arch_bane_outpost_generator_v01) is solid from x -409 to -393; the pad walk
            // ends east of it on the floor the client uses (175.81), 4.5 m from the pad's middle (r14), the client's stops at x -392.1.
            ["the plant 24: (-289, -287) north round the landing pad to the pad (-390.8, -297)"] = new Vector3[] { new(-289.13f, 177.61f, -286.65f), new(-329, 175.43f, -287), new(-341, 175.93f, -265), new(-351, 176.37f, -260), new(-367, 176.52f, -264),
                new(-380.5f, 176.37f, -273), new(-381.5f, 176.61f, -274.5f), new(-384, 176.37f, -274), new(-387.5f, 176.06f, -274), new(-391, 175.81f, -276), new(-391, 175.81f, -296),
                new(-390.8f, 175.81f, -297) },
            ["the plant 29: (-309, -53) to (-338, -180)"] = new Vector3[] { new(-309, 129.55f, -53), new(-309, 129.6f, -95), new(-328.15f, 129.6f, -114.57f), new(-335.75f, 129.6f, -126.5f), new(-336, 129.6f, -155), new(-333, 130.5f, -160), new(-337.59f, 129.68f, -177.84f), new(-338, 129.6f, -180) },
            ["the plant 30: (-338, -180) to (-337.5, -248.5)"] = new Vector3[] { new(-338, 129.6f, -180), new(-338.58f, 129.31f, -194.39f), new(-337.71f, 128.83f, -219.14f), new(-337.54f, 128.35f, -243.61f), new(-337.5f, 128.25f, -248.5f) },
            ["the plant 31: (-337.5, -248.5) to (-308.5, -363.5), part 1"] = new Vector3[] { new(-337.5f, 128.25f, -248.5f), new(-337, 128.52f, -264.25f), new(-337, 128.51f, -280), new(-335, 128.02f, -283.5f), new(-333.42f, 128.03f, -294.5f) },
            ["the plant 31: (-337.5, -248.5) to (-308.5, -363.5), part 2"] = new Vector3[] { new(-333.42f, 128.03f, -294.5f), new(-335.4f, 128.8f, -321), new(-337, 129.61f, -335), new(-336.22f, 129.61f, -338.38f) },
            ["the plant 31: (-337.5, -248.5) to (-308.5, -363.5), part 3"] = new Vector3[] { new(-336.22f, 129.61f, -338.38f), new(-326.75f, 129.61f, -348), new(-308.5f, 132.92f, -363.5f) },
            ["the plant 31: (-337.5, -248.5) to (-308.5, -363.5), part 4"] = new Vector3[] { new(-308.5f, 132.92f, -363.5f), new(-305, 133.61f, -365.25f), new(-294.5f, 137.61f, -354), new(-275, 144.12f, -344), new(-271, 147.14f, -348), new(-270, 150.09f, -353), new(-273, 153.49f, -358), new(-278, 156.47f, -358), new(-283.08f, 159.29f, -353.63f), new(-281, 163.01f, -348), new(-278.5f, 164.94f, -346), new(-266, 169.5f, -346), new(-259.88f, 169.61f, -350.95f), new(-247.75f, 169.61f, -338.5f) },
            ["the plant 31: (-337.5, -248.5) to (-308.5, -363.5), part 5"] = new Vector3[] { new(-247.75f, 169.61f, -338.5f), new(-232.5f, 173, -320.5f) },
            ["the plant 34: (-232.5, -320.5) to (-363, -371), part 1"] = new Vector3[] { new(-232.5f, 173, -320.5f), new(-230.75f, 173.61f, -317), new(-257, 177.61f, -292), new(-264.25f, 177.61f, -290), new(-288.58f, 177.61f, -286.7f), new(-306.5f, 177.5f, -292) },
            ["the plant 34: (-232.5, -320.5) to (-363, -371), part 2"] = new Vector3[] { new(-306.5f, 177.5f, -292), new(-311, 175.81f, -295.75f), new(-353.67f, 176.11f, -340.44f), new(-360.63f, 177.98f, -347.78f), new(-363, 178.09f, -356), new(-363, 177.6f, -371) },
            ["the plant 35: (-363, -371) to (-352, -384.5)"] = new Vector3[] { new(-363, 177.6f, -371), new(-352, 173.5f, -384.5f) },
            ["the plant 36: (-352, -384.5) to (-361, -401)"] = new Vector3[] { new(-352, 173.5f, -384.5f), new(-360.5f, 169.75f, -398), new(-361, 168.8f, -401) },
            ["the plant 37: (-361, -401) to (-364, -406)"] = new Vector3[] { new(-361, 168.8f, -401), new(-362, 168.78f, -400.5f), new(-364, 168.71f, -406) },
            // Fix J (row 43 check): round the south of Kretas' teleporter (its body is solid) to the console.
            ["the plant 38: (-361, -401) to (-392, -432)"] = new Vector3[] { new(-361, 168.8f, -401), new(-360, 168.4f, -406), new(-372, 167.6f, -421), new(-377, 167.6f, -426), new(-386, 167.6f, -436), new(-392.5f, 169.0f, -434.5f), new(-392, 169.01f, -432) },
            // Fix J (row 42 deck check): Central Administration's middle (-364, 168.71, -398) lies 0.9 m under the client floor
            // there (169.61, a raised floor over the room's 167.6); the walk ends on that floor (the area: radius 16).
            ["the plant 39: (-361, -401) to (-364, -398)"] = new Vector3[] { new(-361, 168.8f, -401), new(-364, 169.61f, -398) },
            ["the plant 40: (-172, -416) to (-177, -416)"] = new Vector3[] { new(-172, 131.58f, -416), new(-177, 131.21f, -416) },
            ["the plant 41: (-87, -362) to (-81, -363)"] = new Vector3[] { new(-87, 137.11f, -362), new(-81, 137.11f, -363) },
            ["the plant 42: (-87, -359) to (-82, -369)"] = new Vector3[] { new(-87, 131.58f, -359), new(-83.5f, 131.15f, -362), new(-82, 130.23f, -369) },
            ["the plant 43: (-395, -246) to (-398, -253)"] = new Vector3[] { new(-395, 128.01f, -246), new(-398, 128.01f, -253) },
            ["the plant 44: the pad (-390.8, -297) to beside the charge (-390.8, -301)"] = new Vector3[] { new(-390.8f, 175.81f, -297), new(-390.8f, 175.81f, -301) },
            // Fix J (row 43 check): Kretas' teleporter's body is solid; the walk ends beside it, 2.7 m from its middle.
            ["the plant 45: (-392, -432) to beside the teleporter (-391.5, -432)"] = new Vector3[] { new(-392, 169.01f, -432), new(-391.5f, 168.93f, -432) },
            // DirenArrival (-51, 7.6, -407) is 0.6 m over the client floor (7.0) there: the walk starts on the floor (fix J deck check).
            ["the teleporter's arrival to Diren"] = new Vector3[] { new(-51, 7.0f, -407), new(-53.46f, 7.8f, -401.81f), new(-53, 8.6f, -395) },
        };

        /// <summary>
        /// The upstream pools a walk passes (rule breaks, docs/ashen-playtest.md): the tunnel's 531094 (in it), 531088 and 531089
        /// (over it: their creatures stand 30-45 m above the tunnel's floor).
        /// </summary>
        /// <summary>
        /// The body's half width the legs are checked at: 0.5 m, not the 0.33 m of the earlier sets (audit S5: the real client
        /// stopped on a prop a leg cleared by 0.1 m at 0.33).
        /// </summary>
        internal const float WalkHalfWidth = 0.5f;

        /// <summary>
        /// Fix J (row 43): where the walks to scene objects with a solid body end (the client cannot stand in them; the walk check
        /// sees the map's own geometry only, so the scene's bodies are kept out by these places, AshenFixJTests.SceneBodies):
        /// beside the 1447 teleporters (2.7 m and 3.5 m from their middles), on the pad east of the generator (4.5 m from the pad's
        /// middle, inside its r14), 1.5 m east of the charge and 2.7 m from Kretas' teleporter's middle.
        /// </summary>
        internal static readonly Vector3 BesideTeleporter1 = new(-9.5f, 0.5f, -216), BesideTeleporter2 = new(-3.5f, 0.5f, -224.5f),
            OnThePad = new(-390.8f, 175.81f, -297), BesideTheCharge = new(-390.8f, 175.81f, -301), BesideKretasTeleporter = new(-391.5f, 168.93f, -432);

        /// <summary>
        /// Legs the check does not walk: none since fix K (fix J's loop round the pad's trench, not walked, is gone: row 44). Known
        /// gap: the 1447 drill walks end in the drills' bodies (the drills are shot, not walked to; their stand places are a follow-up).
        /// </summary>
        internal static readonly HashSet<string> KnownGaps = new();

        /// <summary>Fix K (row 44): the legs from the ramp's landing over the pad deck to the pad, north round the landing pad's dish.</summary>
        internal static readonly string[] PadDeck = { "the plant 23: (-232, -319) to (-289, -287)", "the plant 24: (-289, -287) north round the landing pad to the pad (-390.8, -297)" };

        private static readonly uint[] TunnelPools = { 531094, 531088, 531089 };

        /// <summary>The walks: label, its legs, the pools it passes (rule breaks), its target.</summary>
        internal static List<(string Label, string[] Legs, uint[] Passes, Vector3 Target)> Walks()
        {
            string[] Out = { "the Control Room to the east hall", "the east hall to the rear ramp, part 1", "the east hall to the rear ramp, part 2", "the east hall to the rear ramp, part 3", "the east hall to the rear ramp, part 4", "the east hall to the rear ramp, part 5", "the rear ramp to outside the rear exit" };
            string[] Caves = { "outside the rear exit to the rear caves", "the rear caves to the cave entrance" };
            string[] Tunnel = { "the cave entrance to the tunnel", "the tunnel to its bend, part 1", "the tunnel to its bend, part 2",
                "the tunnel's last turn to its exit" };
            string[] North = { "the tunnel's exit to the north cavern" };
            string[] Drill1 = North.Append("the north cavern to drill 1").ToArray();
            var walks = new List<(string, string[], uint[], Vector3)>
            {
                ("the box from Drof", new[] { "Drof to the box" }, Array.Empty<uint>(), ToVector(Digging.BoxAt)),
                ("teleporter 1 from Drof", new[] { "Drof to teleporter 1" }, Array.Empty<uint>(), BesideTeleporter1),
                ("teleporter 2 from Drof", new[] { "Drof to teleporter 2" }, Array.Empty<uint>(), BesideTeleporter2),
                ("outside the rear exit from Drof", Out, Array.Empty<uint>(), ToVector(Digging.RearArrival)),
                ("the cave entrance from the rear exit", Caves, Array.Empty<uint>(), V(Digging.EntranceAt)),
                ("the tunnel's exit from the cave entrance", Tunnel, TunnelPools, V(Digging.TunnelExitAt)),
                ("drill 1 from the tunnel's exit", Drill1, Array.Empty<uint>(), ToVector(Digging.DrillsAt[0])),
                ("drill 3 from the tunnel's exit", Drill1.Concat(new[] { "drill 1 to drill 2", "drill 2 to drill 3" }).ToArray(), Array.Empty<uint>(), ToVector(Digging.DrillsAt[2])),
                ("drill 5 from the tunnel's exit", Drill1.Append("drill 1 to drill 5").ToArray(), Array.Empty<uint>(), ToVector(DrillAt(5))),
                ("drill 4 from the tunnel's exit", Drill1.Append("drill 1 to drill 4").ToArray(), Array.Empty<uint>(), ToVector(DrillAt(4))),
                ("drill 6 from the tunnel's exit", Drill1.Append("drill 1 to drill 6").ToArray(), Array.Empty<uint>(), ToVector(DrillAt(6))),
                ("Drof at the plant from the tunnel's exit", North.Append("the north cavern to the plant's entrance").ToArray(), new uint[] { 531089 }, V(N.DrofAt)),
                ("Naimi at the plant from Drof", new[] { "Drof to Naimi at the plant" }, new uint[] { 531089 }, V(N.NaimiAt)),
                ("valve 1 (Alpha) from Drof at the plant", new[] { "the plant 01: (-309, -53) to (-309, -93)", "the plant 02: (-309, -93) to (-143, -167)" }, new uint[] { 531086, 531088, 531089 }, ToVector(Subversion.ValvesAt[0])),
                ("valve 2 (Beta) from Drof at the plant", new[] { "the plant 03: (-309, -53) to (-347, -181)", "the plant 04: (-347, -181) to (-394, -159)", "the plant 05: (-394, -159) to (-394.5, -165)", "the plant 06: (-394.5, -165) to (-415, -237)" }, new uint[] { 531091, 531093, 531086, 531089 }, ToVector(Subversion.ValvesAt[1])),
                ("life support 1 from Drof at the plant", new[] { "the plant 07: (-309, -53) to (-339, -197)", "the plant 08: (-339, -197) to (-330, -206)", "the plant 09: (-330, -206) to (-157, -382)", "the plant 10: (-157, -382) to the ramp's foot (-152, -416.4)", "the plant 11: up the ramp from its foot (-152, -416.4) to (-172, -416)" }, new uint[] { 531087, 531090, 531093, 531086, 531089 }, ToVector(Subversion.SupportsAt[0])),
                ("life support 2 from Drof at the plant", new[] { "the plant 12: (-309, -53) to (-172, -416)", "the plant 40: (-172, -416) to (-177, -416)" }, new uint[] { 531087, 531090, 531093, 531086, 531089 }, ToVector(Subversion.SupportsAt[1])),
                ("life support 3 from Drof at the plant", new[] { "the plant 01: (-309, -53) to (-309, -93)", "the plant 13: (-309, -93) to (-338, -193)", "the plant 14: (-338, -193) to (-339, -198)", "the plant 15: (-339, -198) to (-116, -343)", "the plant 16: (-116, -343) to (-86, -342)", "the plant 17: (-86, -342) to (-87, -362)", "the plant 41: (-87, -362) to (-81, -363)" }, new uint[] { 531087, 531090, 531093, 531086, 531089 }, ToVector(Subversion.SupportsAt[2])),
                ("life support 4 from Drof at the plant", new[] { "the plant 01: (-309, -53) to (-309, -93)", "the plant 13: (-309, -93) to (-338, -193)", "the plant 14: (-338, -193) to (-339, -198)", "the plant 15: (-339, -198) to (-116, -343)", "the plant 18: (-116, -343) to (-87, -359)", "the plant 42: (-87, -359) to (-82, -369)" }, new uint[] { 531087, 531090, 531093, 531086, 531089 }, ToVector(Subversion.SupportsAt[3])),
                ("the door console from Drof at the plant", new[] { "the plant 07: (-309, -53) to (-339, -197)", "the plant 08: (-339, -197) to (-330, -206)", "the plant 19: (-330, -206) to (-300, -224)" }, new uint[] { 531093, 531086, 531089 }, ToVector(Desistance.DoorConsoleAt)),
                ("the programming console from Drof at the plant", new[] { "the plant 01: (-309, -53) to (-309, -93)", "the plant 13: (-309, -93) to (-338, -193)", "the plant 20: (-338, -193) to (-395, -246)" }, new uint[] { 531093, 531085, 531091, 531086, 531089 }, ToVector(Desistance.ProgrammingAt)),
                ("the systems console from Drof at the plant", new[] { "the plant 01: (-309, -53) to (-309, -93)", "the plant 13: (-309, -93) to (-338, -193)", "the plant 20: (-338, -193) to (-395, -246)", "the plant 43: (-395, -246) to (-398, -253)" }, new uint[] { 531093, 531091, 531085, 531086, 531089 }, ToVector(Desistance.SystemsAt)),
                ("the landing pad from Drof at the plant", new[] { "the plant 01: (-309, -53) to (-309, -93)", "the plant 13: (-309, -93) to (-338, -193)", "the plant 14: (-338, -193) to (-339, -198)", "the plant 21: (-339, -198) to (-236, -324)", "the plant 22: (-236, -324) to (-232, -319)", PadDeck[0], PadDeck[1] }, new uint[] { 531085, 531092, 531090, 531093, 531086, 531091, 531089 }, OnThePad),
                ("the explosive charge from Drof at the plant", new[] { "the plant 01: (-309, -53) to (-309, -93)", "the plant 13: (-309, -93) to (-338, -193)", "the plant 14: (-338, -193) to (-339, -198)", "the plant 21: (-339, -198) to (-236, -324)", "the plant 22: (-236, -324) to (-232, -319)", PadDeck[0], PadDeck[1], "the plant 44: the pad (-390.8, -297) to beside the charge (-390.8, -301)" }, new uint[] { 531085, 531092, 531090, 531093, 531086, 531091, 531089 }, BesideTheCharge),
                ("Central Administration from Drof at the plant", new[] { "the plant 29: (-309, -53) to (-338, -180)", "the plant 30: (-338, -180) to (-337.5, -248.5)", "the plant 31: (-337.5, -248.5) to (-308.5, -363.5), part 1", "the plant 31: (-337.5, -248.5) to (-308.5, -363.5), part 2", "the plant 31: (-337.5, -248.5) to (-308.5, -363.5), part 3", "the plant 31: (-337.5, -248.5) to (-308.5, -363.5), part 4", "the plant 31: (-337.5, -248.5) to (-308.5, -363.5), part 5", "the plant 34: (-232.5, -320.5) to (-363, -371), part 1", "the plant 34: (-232.5, -320.5) to (-363, -371), part 2", "the plant 35: (-363, -371) to (-352, -384.5)", "the plant 36: (-352, -384.5) to (-361, -401)", "the plant 39: (-361, -401) to (-364, -398)" }, new uint[] { 531085, 531092, 531090, 531093, 790603, 531086, 531089 }, V(Desistance.AdministrationAt) with { Y = 169.61f }),
                ("Kretas from Drof at the plant", new[] { "the plant 29: (-309, -53) to (-338, -180)", "the plant 30: (-338, -180) to (-337.5, -248.5)", "the plant 31: (-337.5, -248.5) to (-308.5, -363.5), part 1", "the plant 31: (-337.5, -248.5) to (-308.5, -363.5), part 2", "the plant 31: (-337.5, -248.5) to (-308.5, -363.5), part 3", "the plant 31: (-337.5, -248.5) to (-308.5, -363.5), part 4", "the plant 31: (-337.5, -248.5) to (-308.5, -363.5), part 5", "the plant 34: (-232.5, -320.5) to (-363, -371), part 1", "the plant 34: (-232.5, -320.5) to (-363, -371), part 2", "the plant 35: (-363, -371) to (-352, -384.5)", "the plant 36: (-352, -384.5) to (-361, -401)", "the plant 37: (-361, -401) to (-364, -406)" }, new uint[] { 531085, 531092, 531090, 790603, 531093, 531086, 531089 }, V(N.KretasAt)),
                ("the teleportation console from Drof at the plant", new[] { "the plant 29: (-309, -53) to (-338, -180)", "the plant 30: (-338, -180) to (-337.5, -248.5)", "the plant 31: (-337.5, -248.5) to (-308.5, -363.5), part 1", "the plant 31: (-337.5, -248.5) to (-308.5, -363.5), part 2", "the plant 31: (-337.5, -248.5) to (-308.5, -363.5), part 3", "the plant 31: (-337.5, -248.5) to (-308.5, -363.5), part 4", "the plant 31: (-337.5, -248.5) to (-308.5, -363.5), part 5", "the plant 34: (-232.5, -320.5) to (-363, -371), part 1", "the plant 34: (-232.5, -320.5) to (-363, -371), part 2", "the plant 35: (-363, -371) to (-352, -384.5)", "the plant 36: (-352, -384.5) to (-361, -401)", "the plant 38: (-361, -401) to (-392, -432)" }, new uint[] { 531085, 531092, 531090, 531093, 790603, 531086, 531095, 531089 }, ToVector(Desistance.ConsoleAt)),
                ("Kretas' teleporter from Drof at the plant", new[] { "the plant 29: (-309, -53) to (-338, -180)", "the plant 30: (-338, -180) to (-337.5, -248.5)", "the plant 31: (-337.5, -248.5) to (-308.5, -363.5), part 1", "the plant 31: (-337.5, -248.5) to (-308.5, -363.5), part 2", "the plant 31: (-337.5, -248.5) to (-308.5, -363.5), part 3", "the plant 31: (-337.5, -248.5) to (-308.5, -363.5), part 4", "the plant 31: (-337.5, -248.5) to (-308.5, -363.5), part 5", "the plant 34: (-232.5, -320.5) to (-363, -371), part 1", "the plant 34: (-232.5, -320.5) to (-363, -371), part 2", "the plant 35: (-363, -371) to (-352, -384.5)", "the plant 36: (-352, -384.5) to (-361, -401)", "the plant 38: (-361, -401) to (-392, -432)", "the plant 45: (-392, -432) to beside the teleporter (-391.5, -432)" }, new uint[] { 531085, 531092, 531090, 531093, 790603, 531086, 531095, 531089 }, BesideKretasTeleporter),
                ("Diren from the teleporter's arrival", new[] { "the teleporter's arrival to Diren" }, Array.Empty<uint>(), V(F15.DirenAt))
            };
            return walks;
        }

        internal static List<Vector3> Line(IEnumerable<string> legs) =>
            legs.SelectMany((leg, index) => index == 0 ? Legs[leg] : Legs[leg].Skip(1)).ToList();

        private static void AssertPowerOn(WildernessRuntimeTestHarness harness, MapChannel copy, string when)
        {
            var box = SceneObject(harness, copy, Digging.MissionId, Digging.BoxRole);
            var door = SceneObject(harness, copy, Digging.MissionId, Digging.DoorRole);
            Assert.AreEqual(((UseObjectState)Digging.SwitchSet, false, UseObjectState.DoorStateOpen), (box.StateId, box.IsEnabled, door.StateId), $"{when}: the box set, the door open");
            for (var number = 1; number <= 2; number++)
            {
                var teleporter = SceneObject(harness, copy, Digging.MissionId, Digging.TeleporterRole(number));
                Assert.AreEqual(((UseObjectState)Digging.TeleporterOn, true), (teleporter.StateId, teleporter.IsEnabled), $"{when}: teleporter {number} on");
            }
        }

        /// <summary>
        /// A teleporter's move on this map (DynamicObjectManager.TakePassage): the passage is held for its effect
        /// (PassageHoldMs, on the real clock), its destination the one asked for; the arrival as the passage ends it.
        /// </summary>
        private static void Teleported(WildernessRuntimeTestHarness harness, Vector3 destination, string what)
        {
            var transfer = harness.Client.PendingTransfer;
            Assert.IsNotNull(transfer, $"{what}: a passage");
            Assert.AreEqual((true, ReferenceEquals(transfer.OriginMap, transfer.DestinationMap)), (transfer.IsPassage, true), $"{what}: on this map");
            Assert.IsLessThan(0.01f, Vector3.Distance(transfer.DestinationPosition, destination), what);
            harness.Client.PendingTransfer = null;
            harness.MoveTo(destination);
            harness.Drain();
        }

        /// <summary>A walk from <paramref name="from"/> into the area at <paramref name="to"/> (MissionAreaService, as a movement packet tells it).</summary>
        private static void Enter(WildernessRuntimeTestHarness harness, Vector3 from, Vector3 to)
        {
            harness.MoveTo(to);
            harness.Client.MissionAreaService.RecordAcceptedMovement(harness.Client, from, to);
            harness.Drain();
        }

        /// <summary>A drill's place as the database holds it: set 16's, but drills 4-6 fix J's (AshenFixJV1, to-fix row 38).</summary>
        internal static ScenePosition DrillAt(int number) => AshenFixJV1.DrillAt(number);

        private static void UseAndTick(WildernessRuntimeTestHarness harness, MapChannel copy, string role)
        {
            Use(harness, copy, SceneObject(harness, copy, Desistance.MissionId, role), Desistance.UseArgument);
            Tick(harness, copy, 250);
            Tick(harness, copy, 250);
        }

        /// <summary>A weapon hit on the object (MarshesPreoDasTests.Damage).</summary>
        private static void Shoot(WildernessRuntimeTestHarness harness, MapChannel map, DynamicObject obj, int damage)
        {
            Steady(harness);
            harness.MoveTo(obj.Position + new Vector3(0, 0, 3));
            var action = new ActionData(harness.Client.Player, ActionId.WeaponAttack, 133, 0) { TargetId = obj.EntityId };
            MissileManager.Instance.MissileLaunch(map, action, damage);
            MissileManager.Instance.DoWork(map, 1000);
            harness.Manager.Scenes.Tick(map);
            harness.Drain();
        }

        /// <summary>A full mission-item bag at the accept: no mission, no item; one slot freed, the accept gives one.</summary>
        private static void FullBagWaitsAtTheAccept(WildernessRuntimeTestHarness harness, Creature giver, uint mission, uint template)
        {
            WildernessAliaBranchesTests.FillCategory(harness, Facility.MissionItemFiller, 50);
            harness.MoveTo(giver.Position);
            harness.Drain();
            Assert.IsFalse(harness.Manager.AcceptOfferedMission(harness.Client, giver.EntityId, mission), "no room for its item");
            Assert.IsFalse(harness.Client.Player.Missions.ContainsKey(mission));
            Assert.AreEqual(0U, Facility.Held(harness, template));
            WildernessAliaBranchesTests.FreeOneSlot(harness, Facility.MissionItemFiller);
            Accept(harness, giver, mission);
            Assert.AreEqual(1U, Facility.Held(harness, template), "a slot free: the item");
        }

        private static Vector3 V((double X, double Y, double Z) at) => new((float)at.X, (float)at.Y, (float)at.Z);
    }
}
