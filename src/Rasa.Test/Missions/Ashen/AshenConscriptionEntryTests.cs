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
using Rasa.Packets.MapChannel.Client;
using Rasa.Packets.MapChannel.Server;
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
    using N = AshenConscriptionEntryNpcsV1;
    using Lockdown = AshenLockdownTheConscriptionFacilityV1;
    using Raid = AshenRaidTheConscriptionFacilityV1;
    using Blast = AshenBlastTheBiolabV1;
    using Facility = AshenIndraFacilityTests;
    using F = MarshesClientFloorTests;
    using WO = AshenWhiteOasisTests;

    /// <summary>
    /// Ashen Desert set 19, the Bane Conscription Facility (squad instance 1988), the entry and the biolab, played through as a
    /// player plays it: Yamamoto's Lockdown the Conscription Facility (1500: through the door, Peacock's talk and turn-in just
    /// inside), Peacock's Raid the Conscription Facility (1497: the Detonator on the front field's charge, the field down, the
    /// Bane CommLink Terminal's talk, the turn-in by radio) and Blast the Biolab (1498, offered by radio: five bio materials and
    /// five machine parts from the Bane, the Bane teleporter in the HQ hall brings Slocombe, his talk takes them and gives the
    /// bomb, the hall's pad to the biolab's service room, the charge at the Atta Laboratory's door, the turn-in by radio). With
    /// them the set's places and walks on the client's floor, the pads and the field.
    ///
    /// Why these tests: a field that does not stand, or does not go down for each member at his own charge; a charge that works
    /// without a Detonator or takes a mate's twice; a drop that is not each member's own; a teleporter credited to a mate whose
    /// step is not open (GroupCreditService credits a squad signal to every member whose objective is Incomplete); Slocombe's
    /// talk taking short counts; a radio offer one member never gets; a pad that does not take the player both ways would each
    /// break the chain.
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class AshenConscriptionEntryTests
    {
        private const uint Map = N.Facility;

        /// <summary>The set's missions.</summary>
        internal static readonly uint[] Missions = { Lockdown.MissionId, Raid.MissionId, Blast.MissionId };

        /// <summary>The pads set 19 adds.</summary>
        internal static readonly uint[] Pads = N.Pads.Select(pad => pad.Id).ToArray();

        private static readonly uint[] Pools = { N.Peacock };

        private const float PlayerHalfWidth = AshenAvernusOutpostTests.PlayerHalfWidth;

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
            yield return (Lockdown.MissionId, Lockdown.Rendezvous, Lockdown.DoorMarkerAt.X, Lockdown.DoorMarkerAt.Z, Lockdown.DoorMarkerRadius);
            yield return (Lockdown.MissionId, Lockdown.Rendezvous, N.PeacockAt.X, N.PeacockAt.Z, Lockdown.NpcMarkerRadius);
            yield return (Raid.MissionId, Raid.BombTheField, Raid.ChargeAt.X, Raid.ChargeAt.Z, Raid.FieldMarkerRadius);
            yield return (Raid.MissionId, Raid.HackTheTerminal, AshenFixSV1.TerminalAt.X, AshenFixSV1.TerminalAt.Z, Raid.ObjectMarkerRadius);
            yield return (Blast.MissionId, Blast.BioMaterials, Blast.BioPoolAt.X, Blast.BioPoolAt.Z, AshenFixTV1.BioRadius + Blast.PoolMarkerPadding);
            yield return (Blast.MissionId, Blast.MechanicalParts, Blast.PartsPoolAt.X, Blast.PartsPoolAt.Z, AshenFixTV1.PartsRadius + Blast.PoolMarkerPadding);
            yield return (Blast.MissionId, Blast.Teleporter, AshenFixS2V1.TeleporterAt.X, AshenFixS2V1.TeleporterAt.Z, Blast.ObjectMarkerRadius);
            yield return (Blast.MissionId, Blast.GiveToSlocombe, AshenFixS2V1.SlocombeAt.X, AshenFixS2V1.SlocombeAt.Z, Blast.NpcMarkerRadius);
            yield return (Blast.MissionId, Blast.DestroyTheLab, (float)AshenFixMV1.LabChargeAt.X, (float)AshenFixMV1.LabChargeAt.Z, Blast.ObjectMarkerRadius);
        }

        /// <summary>The map a set 19 marker is on: 1500's first before the door on Ashen Desert, all others in the facility.</summary>
        internal static uint MarkerMap(uint mission, uint indicator) =>
            mission == Lockdown.MissionId && indicator == MissionIndicator.UnnamedFrom + 1 ? AshenMissionDataV1.MapContextId : Map;

        /// <summary>
        /// The rows of the three missions: givers and receivers (Yamamoto gives 1500, Peacock takes it in and gives 1497; 1497 and
        /// 1498 turned in by radio, 1498 offered by radio), levels, the Ashen category, the client's texts and talks (1500,1
        /// Peacock 1928; 1497,2 the terminal 1929; 1498,4 Slocombe 1933), the chain's requirements (1581 → 1500 → 1497 → 1498); the
        /// markers on their maps (1500's first before the door on Ashen Desert), named where the client names them (1497,2 251;
        /// 1498,3 287); each mission operational.
        /// </summary>
        [TestMethod]
        public void TheSetsRowsAreTheClients()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            const string revision = AshenMissionDataV1.Revision;
            var expected = new (uint Mission, uint? Giver, uint? Receiver, uint Level, uint Name, uint Requires, bool Radio, (uint, uint?)[] Talks)[]
            {
                (Lockdown.MissionId, AshenLeadInNpcsV1.Yamamoto, N.Peacock, 42, 13394, AshenTheBigSecretV1.MissionId, false, new (uint, uint?)[] { (1, 1928) }),
                (Raid.MissionId, N.Peacock, null, 42, 13379, Lockdown.MissionId, true, new (uint, uint?)[] { (2, 1929) }),
                (Blast.MissionId, null, null, 43, 13384, Raid.MissionId, true, new (uint, uint?)[] { (4, 1933) })
            };
            var catalog = new MissionContentCatalog(harness, new Dictionary<uint, Mission>(), null);
            catalog.Load();
            foreach (var (mission, giver, receiver, level, name, requires, radio, talks) in expected)
            {
                var definition = world.MissionContentDefinitionEntries.AsNoTracking().Single(entry => entry.MissionId == mission && entry.ContentRevision == revision);
                Assert.AreEqual((giver, receiver, (uint?)level, (uint?)AshenMissionDataV1.CategoryId, name, true, radio),
                    (definition.GiverId, definition.ReceiverId, definition.Level, definition.CategoryId, definition.ClientNameTextId, definition.Enabled,
                        definition.RadioCompleteable), $"{mission}");
                var talkRows = world.Set<MissionTriggerEntry>().AsNoTracking()
                    .Where(entry => entry.MissionId == mission && entry.ContentRevision == revision && entry.Kind == MissionTriggerKind.Conversation)
                    .AsEnumerable().Select(entry => (entry.ObjectiveId, entry.NpcPackageId)).OrderBy(entry => entry.ObjectiveId).ToArray();
                CollectionAssert.AreEqual(talks, talkRows, $"{mission}'s talks");
                var prerequisites = world.MissionPrerequisiteEntries.AsNoTracking().Where(entry => entry.MissionId == mission && entry.ContentRevision == revision).ToList();
                Assert.AreEqual(requires, prerequisites.Single().RequiredMissionId, $"{mission} requires {requires}");
                Assert.IsTrue(harness.Manager.LoadedMissions[mission].IsOperational, harness.Manager.LoadedMissions[mission].OperationalDiagnostic);
            }

            var markers = world.MissionIndicatorEntries.AsNoTracking()
                .Where(entry => entry.ContentRevision == revision && Missions.Contains(entry.MissionId)).AsEnumerable()
                .Select(entry => (entry.MissionId, entry.ObjectiveId, entry.IndicatorId,
                    Map: MissionMarkerProjection.ResolveMap(catalog, entry.MissionId, entry.ObjectiveId, new MissionIndicator { IndicatorId = entry.IndicatorId })))
                .ToArray();
            Assert.HasCount(2 + 2 + 5, markers, "the set's markers");
            foreach (var (mission, objective, indicator, map) in markers)
            {
                Assert.AreEqual(MarkerMap(mission, indicator), map, $"{mission}.{objective}/{indicator}: its map");
                if (AshenMissionTests.NamedMarker(mission, objective) is { } named)
                    Assert.AreEqual(named, indicator, $"{mission}.{objective}: the client's name");
                else
                    Assert.IsGreaterThan(MissionIndicator.UnnamedFrom, indicator, $"{mission}.{objective}: unnamed");
            }
            Assert.AreEqual(((uint?)251, (uint?)287), (AshenMissionTests.NamedMarker(1497, 2), AshenMissionTests.NamedMarker(1498, 3)), "the two the client names");
        }

        /// <summary>
        /// The set's rows in the id block 790659-790660: Peacock and Slocombe friendly with the client's names, levels and packages
        /// (9740/1928, 9745/1933), Harris' class; Peacock alone in his pool on the facility, 60 m+ outside every hostile pool's
        /// circle and 100 m+ outside every warcry pool's; Slocombe without a pool (1498's copy only). No world field on the
        /// facility (the front field is each member's own 1497 scene object). The pads: two local teleporters (type 1, class
        /// 8044) on the facility linked both ways and to nothing else. The places' clearances as Places gives them.
        /// </summary>
        [TestMethod]
        public void TheSetsCreaturesFieldAndPadsStandApart()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            var rows = world.CreatureEntries.AsNoTracking().Where(row => row.Id >= 790659 && row.Id <= 790660).ToList();
            CollectionAssert.AreEquivalent(N.Creatures, rows.Select(row => row.Id).ToArray(), "the two");
            var harris = world.CreatureEntries.AsNoTracking().Single(row => row.Id == N.Harris);
            foreach (var (id, _, name, level, package) in N.Friends)
            {
                var row = rows.Single(entry => entry.Id == id);
                Assert.AreEqual((1U, name, level, harris.ClassId), ((uint)row.Faction, row.NameId, row.Level, row.ClassId), $"{id}: friendly, the client's name, Harris' class");
                Assert.AreEqual(package, world.NpcPackageEntries.AsNoTracking().Single(entry => entry.Id == id).PackageId, $"{id}'s package");
            }

            var pools = world.SpawnPoolEntries.AsNoTracking().Where(pool => pool.Id >= 790659 && pool.Id <= 790660).ToList();
            Assert.AreEqual(N.Peacock, pools.Single().Id, "Peacock's pool only");
            var peacock = pools.Single();
            Assert.AreEqual((Map, N.Peacock, (byte)1, (byte)1, 0.0), (peacock.MapContextId, peacock.Creature1Id, peacock.Creature1MinCount, peacock.Creature1MaxCount, peacock.Radius),
                "Peacock alone, on the facility");
            Assert.IsLessThan(0.01f, Vector3.Distance(V(N.PeacockAt), new((float)peacock.PosX, (float)peacock.PosY, (float)peacock.PosZ)), "his place");
            var criers = AshenFixFTests.WarcryCreatures(harness);
            foreach (var other in HostilePools(harness))
            {
                var crier = new[] { other.Creature1Id, other.Creature2Id, other.Creature3Id, other.Creature4Id, other.Creature5Id, other.Creature6Id }.Any(criers.Contains);
                Assert.IsGreaterThanOrEqualTo(crier ? 100f : 60f, Facility.Gap(V(N.PeacockAt), other), $"Peacock's pool {(crier ? 100 : 60)} m+ outside hostile pool {other.Id}'s circle");
            }

            Assert.IsFalse(world.WorldDestructibleEntries.AsNoTracking().Any(row => row.MapContextId == Map), "no public field in the facility (1497's is each member's own)");

            var pads = world.TeleporterEntries.AsNoTracking().Where(row => Pads.Contains(row.Id)).OrderBy(row => row.Id).ToList();
            Assert.HasCount(2, pads);
            foreach (var (row, (id, _, place, _)) in pads.Zip(N.Pads))
            {
                Assert.AreEqual((id, Map, N.PadClass, N.LocalTeleporter, 0.0), (row.Id, row.MapContextId, row.ClassId, (uint)row.Type, row.Rotation), $"{row.Id}");
                Assert.IsLessThan(0.01f, Vector3.Distance(V(AshenFixS2V1.Moved(id, place)), new Vector3((float)row.PosX, (float)row.PosY, (float)row.PosZ)), $"{row.Id} place");
            }
            int Links(string where) => world.Database.SqlQueryRaw<int>($"SELECT COUNT(*) AS Value FROM local_teleporter_link WHERE {where}").AsEnumerable().Single();
            foreach (var (id, _, _, to) in N.Pads)
                Assert.AreEqual(1, Links($"from_teleporter_id = {id} AND to_teleporter_id = {to}"), $"{id} to {to}");
            var ids = string.Join(", ", Pads);
            Assert.AreEqual(2, Links($"from_teleporter_id IN ({ids}) OR to_teleporter_id IN ({ids})"), "no other link");
            Assert.IsFalse(world.TeleporterEntries.AsNoTracking().Where(row => row.MapContextId == Map && !Pads.Contains(row.Id)).AsEnumerable().Any(row =>
                (uint)row.Type == 1 && N.Pads.Any(pad => Vector2.Distance(new((float)row.PosX, (float)row.PosZ), new((float)AshenFixS2V1.Moved(pad.Id, pad.At).X, (float)AshenFixS2V1.Moved(pad.Id, pad.At).Z)) < 5)),
                "no other pad by them (5 m: 2 m triggers; S2's 921 and 925 stand 7 m apart in Central Operations)");

            var failures = new List<string>();
            foreach (var (label, place, _, _, clearance) in Places())
                foreach (var pool in HostilePools(harness))
                    if (Facility.Gap(place, pool) < clearance)
                        failures.Add(FormattableString.Invariant($"{label}: {Facility.Gap(place, pool):0.0} m outside hostile pool {pool.Id}'s circle (its rule {clearance})"));
            Assert.IsEmpty(failures, string.Join("; ", failures));
        }

        /// <summary>
        /// In the squad's copy, entered by its door from Ashen Desert: Peacock comes out friendly, on the navmesh's ground, and the
        /// player can talk to him; there is no public Slocombe and no public field (nothing stops a player in the HQ tunnel on the
        /// server: 1497's field is each member's own scene object).
        /// </summary>
        [TestMethod]
        public void TheSetsCreaturesAndFieldStandInTheSquadsCopy()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var copy = IntoFacility(harness);
            var npc = Giver(copy, N.Peacock);
            Assert.AreEqual((N.Peacock, TargetCategory.Friendly), (npc.DbId, npc.TargetCategory), "Peacock");
            Assert.IsNotNull(npc.Npc, "Peacock talks");
            AssertOnGround(copy, npc.Position, "Peacock");
            Assert.IsFalse(Here(copy).Any(one => one.DbId == N.Slocombe), "no public Slocombe");
            Assert.IsFalse(ForceFields.StopsPlayer(copy, Tunnel[0], Tunnel[1]), "no public field in the tunnel");
            Assert.IsEmpty(Fields(copy, harness.Client), "no field shown before 1497");
        }

        /// <summary>
        /// The pad pair: a player who steps onto the HQ hall's pad is set down over the biolab's service room's and not sent
        /// straight back; off it and on again, he is taken back. Each arrival over its pad, about 1 m up (the pad's arrival height).
        /// </summary>
        [TestMethod]
        public void TheBaneTeleportersTakeThePlayerThereAndBack()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var copy = IntoFacility(harness);
            LoadPads(harness, copy);
            var there = copy.Teleporters[N.HallPad];
            var back = copy.Teleporters[N.ServicePad];
            Teleport(harness, copy, there);
            Assert.IsLessThan(3f, Vector3.Distance(back.Position, harness.Client.Player.Position), "at the service room's pad");
            Assert.AreEqual(1f, harness.Client.Player.Position.Y - back.Position.Y, 0.15f, "set down about 1 m over the service pad");
            harness.Objects.DynamicObjectProximityWorker(copy, back, 0);
            Assert.IsNull(harness.Client.PendingTransfer, "not sent straight back");
            harness.MoveTo(new Vector3(-163, 105.81f, -205));
            harness.Drain();
            harness.Objects.DynamicObjectProximityWorker(copy, back, 0);
            Assert.IsNull(harness.Client.PendingTransfer, "off the pad");
            Teleport(harness, copy, back);
            Assert.IsLessThan(3f, Vector3.Distance(there.Position, harness.Client.Player.Position), "back in the hall");
            Assert.AreEqual(1f, harness.Client.Player.Position.Y - there.Position.Y, 0.15f, "set down about 1 m over the hall pad");
        }

        /// <summary>
        /// The set's places on the client's floor: each on the navmesh's ground (within 1 m) and a client floor (within 1 m), with no
        /// client surface over its head within 2.2 m; the NPCs and the objects one uses seen from all 16 places 2 m round them; each
        /// outside every hostile pool's circle by its clearance (Places). The facility's markers on the navmesh and a client floor
        /// within 2 m (1500's first, on Ashen Desert: AshenGuardTests.EveryAshenMarkerStandsOnTheNavmeshAndAClientFloor).
        /// </summary>
        [TestMethod]
        public void ThePlacesStandOnTheClientsFloor()
        {
            var dataDir = F.ClientData();
            using var harness = WildernessRuntimeTestHarness.Create();
            var copy = IntoFacility(harness);
            var nav = copy.NavMesh;
            using var meshes = new MeshLibrary(dataDir, F.MeshCsv());
            var geometry = MapGeometry.Load(Path.Combine(dataDir, "maps", Facility.MapName(harness, Map)), meshes, 1);
            var hostile = HostilePools(harness);
            var places = Places();
            var near = F.Near(geometry, places.Select(place => place.At).ToList(), 12);
            var failures = new List<string>();
            for (var i = 0; i < places.Count; i++)
            {
                var (label, at, ring, middle, clearance) = places[i];
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
                foreach (var pool in hostile)
                    if (Facility.Gap(at, pool) < clearance)
                        failures.Add(FormattableString.Invariant($"{label}: {Facility.Gap(at, pool):0.0} m outside hostile pool {pool.Id}'s circle (its rule {clearance})"));
            }

            var markers = harness.World.MissionIndicatorEntries.AsNoTracking()
                .Where(entry => entry.ContentRevision == AshenMissionDataV1.Revision && Missions.Contains(entry.MissionId)).AsEnumerable()
                .Where(marker => MarkerMap(marker.MissionId, marker.IndicatorId) == Map)
                .Select(marker => (Label: $"{marker.MissionId}.{marker.ObjectiveId}/{marker.IndicatorId}", At: new Vector3((float)marker.PosX, (float)marker.PosY, (float)marker.PosZ)))
                .ToList();
            Assert.HasCount(8, markers, "the facility's markers");
            var (markerFloors, _) = F.Surfaces(geometry, markers.Select(marker => (marker.At + new Vector3(0, 2 - F.MaxUnder, 0), F.Walkable)).ToList());
            for (var i = 0; i < markers.Count; i++)
            {
                if (nav.GroundHeight(markers[i].At + new Vector3(0, 2, 0)) is not { } y || MathF.Abs(y - markers[i].At.Y) > 2f)
                    failures.Add($"{markers[i].Label}: navmesh {nav.GroundHeight(markers[i].At + new Vector3(0, 2, 0))}");
                if (markers[i].At.Y - markerFloors[i] > 2f)
                    failures.Add($"{markers[i].Label}: client floor {markerFloors[i]}");
            }
            Assert.HasCount(8, places);
            Assert.IsEmpty(failures, $"{failures.Count}: " + string.Join("; ", failures));
        }

        /// <summary>
        /// The walks on the client's floor (AshenFixBTests.ClientFloorLine with a player's half width, 0.5 m), with the collision of
        /// the set's objects (the charges, the terminal, the teleporter and the pads; the field down: the walk through it is after
        /// 1), every leg there and back by the corners Legs names (a client-floor flood's way, 2026-11-12), also with its start
        /// moved 0.13, 0.27 and 0.4 m along its first stretch (StartShifts). Every walk keeps 30 m (2D) outside the circle of
        /// every hostile pool but those it names (docs/ashen-playtest.md). With the field up, the walk through the tunnel is
        /// stopped.
        /// </summary>
        [TestMethod]
        public void ThePlacesAreWalkedToOnTheClientsFloor()
        {
            var dataDir = F.ClientData();
            using var harness = WildernessRuntimeTestHarness.Create();
            using var meshes = new MeshLibrary(dataDir, F.MeshCsv());
            var geometry = MapGeometry.Load(Path.Combine(dataDir, "maps", Facility.MapName(harness, Map)), meshes, 1);
            foreach (var (classId, at, yaw) in Objects())
                AddObject(geometry, meshes, classId, at, yaw);
            var hostile = HostilePools(harness);
            var failures = new List<string>();
            var walked = 0;
            var expected = 0;
            foreach (var (leg, corners) in Legs)
            {
                // Each leg, there and back, as given and with its start moved StartShifts along its first stretch (the way back
                // from the last corner along the last stretch): the samples (every 0.5 m) fall elsewhere (set 18's rule).
                foreach (var (given, direction) in new[] { (corners.ToList(), "there"), (Enumerable.Reverse(corners).ToList(), "back") })
                {
                    var first = Vector2.Distance(new(given[0].X, given[0].Z), new(given[1].X, given[1].Z));
                    foreach (var shift in StartShifts.Where(shift => shift == 0 || first > shift + 0.05f))
                    {
                        expected++;
                        walked++;
                        var line = given.ToList();
                        line[0] = Vector3.Lerp(given[0], given[1], shift / first);
                        if (AshenFixBTests.ClientFloorLine(geometry, line, line[^1], AshenFixS2Tests.OverTheRamp, PlayerHalfWidth, rampFoot: true) is { } why)
                            failures.Add(FormattableString.Invariant($"{leg} {direction} (start +{shift} m): {why}"));
                    }
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
            // Beside the charges, not on them: every leg keeps the charge's body (its collision's reach) + TerrainEdge + a half width.
            var reach = meshes.GetForClass((int)Raid.ChargeClass).Vertices.Max(point => MathF.Sqrt(point.X * point.X + point.Z * point.Z));
            var keep = reach + AshenFixBTests.TerrainEdge + PlayerHalfWidth;
            foreach (var (leg, corners) in Legs)
                foreach (var charge in new[] { ToVector(Raid.ChargeAt), V(AshenFixMV1.LabChargeAt) })
                    for (var k = 1; k < corners.Length; k++)
                        if (Flat(corners[k - 1], corners[k], charge) < keep)
                            failures.Add(FormattableString.Invariant($"{leg} passes {Flat(corners[k - 1], corners[k], charge):0.00} m from the charge at ({charge.X}, {charge.Z}), {keep:0.00} wanted"));
            Assert.AreEqual(expected, walked, "every leg both ways, at each start shift");
            Assert.IsGreaterThanOrEqualTo(2 * Legs.Count * 3, walked, "most legs at three shifts or more");

            // The field across the tunnel's full width, wall to wall: each line along the tunnel through the field's place (every
            // 0.25 m across, a point's walk, no half width) that is open with the field down is stopped with it up, both ways.
            static List<Vector3> Across(float x) => new() { new(x, 137.61f, -408.5f), new(x, 137.61f, -401.8f) };
            var open = Enumerable.Range(0, 81).Select(k => -3.5f + k * 0.25f)
                .Where(x => AshenFixBTests.ClientFloorLine(geometry, Across(x), Across(x)[^1]) == null).ToList();
            Assert.IsGreaterThanOrEqualTo(12, open.Count, $"the tunnel walked along at {open.Count} places across");
            Assert.IsTrue(open.Min() > -3.5f && open.Max() < 16.5f, "walls on both sides of the swept width");

            // The field up: the walk from the charge through the tunnel is stopped (both ways).
            AddObject(geometry, meshes, (int)N.FieldClass, V(AshenFixMV1.FieldAt), (float)N.FieldYaw);
            foreach (var x in open)
            {
                Assert.IsNotNull(AshenFixBTests.ClientFloorLine(geometry, Across(x), Across(x)[^1]), FormattableString.Invariant($"the field stops the walk in at x {x}"));
                var back = Enumerable.Reverse(Across(x)).ToList();
                Assert.IsNotNull(AshenFixBTests.ClientFloorLine(geometry, back, back[^1]), FormattableString.Invariant($"the field stops the walk out at x {x}"));
            }
            var tunnel = Legs["the charge to the terminal"].Take(3).ToList();
            Assert.IsNotNull(AshenFixBTests.ClientFloorLine(geometry, tunnel, tunnel[^1], null, PlayerHalfWidth), "the field stops the walk in");
            tunnel.Reverse();
            Assert.IsNotNull(AshenFixBTests.ClientFloorLine(geometry, tunnel, tunnel[^1], null, PlayerHalfWidth), "the field stops the walk out");
            Assert.IsEmpty(failures, $"{failures.Count}: " + string.Join("; ", failures));
        }

        /// <summary>
        /// The pads' body (UsableTwoStateBaneTeleporterV01, class 8044: a disc 0.68 m up, 2.2-2.6 m across its rim, its arch on local
        /// +x; its rim faces 42 degrees only on local -z and +z within local x -0.56..0.49, steeper (55-64 degrees) elsewhere, the
        /// probe's mesh dump, 2026-11-12): each of 921-922 stands at rotation 0, and every walk to or from one leaves its centre
        /// toward a neighbour corner whose line crosses the rim (2.2 m out) within local x -0.45..0.45, on the -z or +z side; a walk
        /// that ends inside a pad's 2 m trigger short of its centre (the hall pad's) comes up the rim there: its last stretch
        /// within local x -0.45..0.45.
        /// </summary>
        [TestMethod]
        public void ThePadsAreWalkedOverTheirShallowRim()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var rows = harness.World.TeleporterEntries.AsNoTracking().Where(row => Pads.Contains(row.Id)).ToList();
            Assert.HasCount(2, rows);
            foreach (var row in rows)
                Assert.AreEqual((row.Id, 0f), (row.Id, (float)row.Rotation), $"{row.Id}: rotation 0");
            var failures = new List<string>();
            var reached = new HashSet<uint>();
            foreach (var (label, legs, _, _) in Walks())
            {
                var line = Line(legs);
                foreach (var (end, next) in new[] { (line[0], line[1]), (line[^1], line[^2]) })
                    foreach (var (id, _, at, _) in N.Pads.Select(pad => (pad.Id, pad.Description, AshenFixS2V1.Moved(pad.Id, pad.At), pad.To)).Where(pad => Vector3.Distance(V(pad.Item3), end) < 2f))
                    {
                        reached.Add(id);
                        if (Vector2.Distance(new((float)at.X, (float)at.Z), new(end.X, end.Z)) < 0.01f)
                        {
                            var way = Vector2.Normalize(new Vector2(next.X - end.X, next.Z - end.Z));
                            if (MathF.Abs(way.X * 2.2f) > 0.45f)
                                failures.Add(FormattableString.Invariant($"{label}: pad {id}'s rim crossed at local x {way.X * 2.2f:0.00}"));
                        }
                        else
                            foreach (var corner in new[] { end, next })
                                if (MathF.Abs(corner.X - (float)at.X) > 0.45f)
                                    failures.Add(FormattableString.Invariant($"{label}: pad {id}'s rim come up at local x {corner.X - (float)at.X:0.00}"));
                    }
            }
            CollectionAssert.AreEquivalent(Pads, reached.ToArray(), "every pad walked to or from");
            Assert.IsEmpty(failures, string.Join("; ", failures));
        }

        /// <summary>
        /// Lockdown the Conscription Facility: Yamamoto in Ashen Desert does not offer it before The Big Secret; then he offers it with
        /// its four stacks; its marker before the door. Through the door Peacock hears 1 and his talk makes it completeable; the
        /// turn-in at Peacock waits with a full bag, pays the stack chosen once; Peacock then offers Raid the Conscription Facility.
        /// </summary>
        [TestMethod]
        public void LockdownMeetsPeacockJustInside()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var desert = AshenLeadInsTests.EnterAshen(harness, V(AshenLeadInNpcsV1.YamamotoAt) + new Vector3(0, 0, 2));
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, Npc(desert, AshenLeadInNpcsV1.Yamamoto), Lockdown.MissionId), "not before 1581");
            AshenRuntime.SeedCompleted(harness, AshenTheBigSecretV1.MissionId);
            var yamamoto = Npc(desert, AshenLeadInNpcsV1.Yamamoto);
            var offer = WildernessRanjaGorgeTests.Offer(harness, yamamoto, Lockdown.MissionId);
            CollectionAssert.AreEqual(new[] { Lockdown.FragGrenade, Lockdown.EmpBomb, Lockdown.CryogenicGrenade, Lockdown.StandardMedPack },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Accept(harness, yamamoto, Lockdown.MissionId);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Lockdown.MissionId, Lockdown.Rendezvous), "1 open");

            var copy = IntoFacility(harness);
            var peacock = Giver(copy, N.Peacock);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, peacock, Raid.MissionId), "not before 1500");
            Talk(harness, peacock, Lockdown.MissionId, Lockdown.Rendezvous);
            Tick(harness, copy, 250);
            Assert.IsTrue(harness.Client.Player.Missions[Lockdown.MissionId].Completeable, "to be turned in");
            peacock = Giver(copy, N.Peacock);
            FullBagWaits(harness, peacock, Lockdown.MissionId, 1, ConsumableFiller);
            FailedTurnInPaysNothing(harness, peacock, Lockdown.MissionId, 1);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, peacock, Lockdown.MissionId, 1, Lockdown.Experience, Lockdown.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Lockdown.MissionId), "MissionRewarded once");
            Assert.AreEqual(Lockdown.EmpBombs, Facility.Held(harness, Lockdown.EmpBomb), "the EMP bomb chosen");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, Giver(copy, N.Peacock), Lockdown.MissionId), "done once");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, Giver(copy, N.Peacock), Raid.MissionId), "Peacock offers 1497");
        }

        /// <summary>
        /// Raid the Conscription Facility: Peacock offers it after 1500 with its four pieces; the accept gives the Detonator and puts
        /// the player's own front field (class 9572, intact) across the HQ tunnel, his own charge before it and his own terminal
        /// in the HQ hall; 1 open, 2 waits; the terminal talks nothing yet. The charge's use takes the Detonator, takes his field
        /// away and sets the charge; 1 done, 2 open; a reload keeps them (no field again). The terminal hears 2 and its talk makes it completeable; radioed in it pays once, and
        /// the radio offers Blast the Biolab; the charge and the terminal go with the mission.
        /// </summary>
        [TestMethod]
        public void RaidBombsTheFrontFieldAndHacksTheTerminal()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            AshenRuntime.SeedCompleted(harness, AshenTheBigSecretV1.MissionId);
            var copy = IntoFacility(harness);
            {
                Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, Giver(copy, N.Peacock), Raid.MissionId), "not before 1500");
                AshenRuntime.SeedCompleted(harness, Lockdown.MissionId);
                var peacock = Giver(copy, N.Peacock);
                var offer = WildernessRanjaGorgeTests.Offer(harness, peacock, Raid.MissionId);
                CollectionAssert.AreEqual(new[] { Raid.BioBoots, Raid.StealthHelmet, Raid.GravitonLegs, Raid.MotorAssistGloves },
                    offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
                Accept(harness, peacock, Raid.MissionId);
                Tick(harness, copy, 250);
                Assert.AreEqual(1U, Facility.Held(harness, Raid.DetonatorTemplate), "the Detonator");
                Assert.AreEqual((MissionObjectiveState.Incomplete, MissionObjectiveState.Inactive),
                    (Objective(harness, Raid.MissionId, Raid.BombTheField), Objective(harness, Raid.MissionId, Raid.HackTheTerminal)), "1 open, 2 waits");
                var charge = SceneObject(harness, copy, Raid.MissionId, Raid.ChargeRole);
                Assert.AreEqual((Raid.ChargeClass, ToVector(Raid.ChargeAt), harness.Client.Player.Id, true),
                    ((uint)charge.EntityClassId, charge.Position, charge.SceneOwnerCharacterId, charge.IsEnabled), "the player's own charge");
                var terminal = SceneObject(harness, copy, Raid.MissionId, Raid.TerminalRole);
                Assert.AreEqual((Raid.TerminalClass, ToVector(AshenFixSV1.TerminalAt), Raid.TerminalPackage, harness.Client.Player.Id, Raid.TerminalName),
                    ((uint)terminal.EntityClassId, terminal.Position, terminal.MissionConversation.NpcPackageId, terminal.SceneOwnerCharacterId, terminal.NameOverrideId),
                    "the player's own terminal, named Conscription Database Interface");
                Assert.IsEmpty(TerminalTopics(harness, harness.Client, terminal), "the terminal talks nothing before 2");
                AssertFieldUp(harness, copy, harness.Client, "at the accept");

                Use(harness, copy, charge, Raid.UseArgument);
                Tick(harness, copy, 250);
                Assert.AreEqual((0U, MissionObjectiveState.Completed, MissionObjectiveState.Incomplete, false),
                    (Facility.Held(harness, Raid.DetonatorTemplate), Objective(harness, Raid.MissionId, Raid.BombTheField),
                        Objective(harness, Raid.MissionId, Raid.HackTheTerminal), SceneObject(harness, copy, Raid.MissionId, Raid.ChargeRole).IsEnabled),
                    "the Detonator taken, 1 done, 2 open, the charge set");
                Assert.IsEmpty(Fields(copy, harness.Client), "his field gone");
                ReloadAndRecover(harness, copy);
                Assert.AreEqual((MissionObjectiveState.Incomplete, false),
                    (Objective(harness, Raid.MissionId, Raid.HackTheTerminal), SceneObject(harness, copy, Raid.MissionId, Raid.ChargeRole).IsEnabled), "kept at a reload");
                Assert.IsEmpty(Fields(copy, harness.Client), "no field after a reload");

                terminal = SceneObject(harness, copy, Raid.MissionId, Raid.TerminalRole);
                CollectionAssert.AreEqual(new[] { Raid.HackTheTerminal }, TerminalTopics(harness, harness.Client, terminal), "the terminal hears 2");
                TalkToTerminal(harness, harness.Client, terminal, Raid.MissionId, Raid.HackTheTerminal);
                Tick(harness, copy, 250);
                Assert.IsTrue(harness.Client.Player.Missions[Raid.MissionId].Completeable, "to be radioed in");
                Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, Giver(copy, N.Peacock).EntityId, Raid.MissionId, 0), "not Peacock's to take in");
                harness.Drain();

                var before = harness.Context.ReadRewardTotals();
                Assert.IsTrue(harness.Manager.TryCompleteRadioMission(harness.Client, Raid.MissionId, 0, null), "radioed in");
                var rewarded = harness.Drain();
                Assert.AreEqual(1, rewarded.OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Raid.MissionId), "paid once");
                var after = harness.Context.ReadRewardTotals();
                Assert.AreEqual((before.Experience + Raid.Experience, before.Credits + (int)Raid.Credits), (after.Experience, after.Credits));
                Assert.AreEqual(1U, Facility.Held(harness, Raid.BioBoots), "the boots chosen");
                Assert.IsFalse(harness.Manager.TryCompleteRadioMission(harness.Client, Raid.MissionId, 0, null), "and only once");
                Assert.AreEqual(1, rewarded.OfType<DispenseRadioMissionPacket>().Count(packet => packet.MissionId == Blast.MissionId), "1498 by radio");
                for (var tick = 0; tick < 4; tick++)
                    Tick(harness, copy, 250);
                foreach (var role in new[] { Raid.ChargeRole, Raid.TerminalRole })
                    Assert.IsNull(FindSceneObject(harness, copy, Raid.MissionId, role), $"{role} goes with the mission");
                Assert.IsEmpty(Fields(copy, harness.Client), "no field after 1497");
            }
        }

        /// <summary>
        /// Raid's charge without the Detonator, and in a squad of two on it (the audit's S2/S3 of Desistance: the charge has no
        /// lock, 1's take of the Detonator is the gate). Alone without his Detonator the player's use is refused: his 1 open, his
        /// field standing, his charge usable. With a mate on it who has his own Detonator and field (each shown only his own), the
        /// player's use still sends the event: the mate's 1 done, his Detonator taken, his charge set, his field gone; the
        /// player's own 1 stays open, his field standing, his charge usable (mixed states in one copy). Each one's own terminal; the mate's talk and his radio turn-in give him the radio offer of 1498 too.
        /// </summary>
        [TestMethod]
        public void RaidsChargeWorksForEachSquadMateWithHisOwnDetonator()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var done = new[] { AshenTheBigSecretV1.MissionId, Lockdown.MissionId };
            AshenRuntime.SeedCompleted(harness, done);
            var copy = IntoFacility(harness);
            {
                var peacock = Giver(copy, N.Peacock);
                Accept(harness, peacock, Raid.MissionId);
                Tick(harness, copy, 250);
                Lose(harness.Client, Raid.DetonatorTemplate);
                Use(harness, copy, SceneObject(harness, copy, Raid.MissionId, Raid.ChargeRole), Raid.UseArgument);
                Tick(harness, copy, 250);
                Assert.AreEqual((MissionObjectiveState.Incomplete, true),
                    (Objective(harness, Raid.MissionId, Raid.BombTheField), SceneObject(harness, copy, Raid.MissionId, Raid.ChargeRole).IsEnabled),
                    "alone, no Detonator: refused; the charge usable");
                AssertFieldUp(harness, copy, harness.Client, "refused");

                using var mate = new Facility.SquadMate(harness, copy, peacock, Raid.MissionId, done);
                Assert.AreEqual((1U, MissionObjectiveState.Incomplete), (mate.Held(Raid.DetonatorTemplate), mate.Of(Raid.BombTheField)), "the mate's own Detonator, his 1 open");
                AssertFieldUp(harness, copy, mate.Client, "the mate's own");
                Use(harness, copy, SceneObject(harness, copy, Raid.MissionId, Raid.ChargeRole), Raid.UseArgument);
                Tick(harness, copy, 250);
                Assert.AreEqual((MissionObjectiveState.Completed, 0U, false, MissionObjectiveState.Incomplete),
                    (mate.Of(Raid.BombTheField), mate.Held(Raid.DetonatorTemplate), mate.Door(Raid.ChargeRole).IsEnabled, mate.Of(Raid.HackTheTerminal)),
                    "in a squad: the mate's 1 done, his Detonator taken, his charge set, his 2 open");
                Assert.IsEmpty(Fields(copy, mate.Client), "the mate's field gone: he walks through");
                AssertFieldUp(harness, copy, harness.Client, "the player's still stands");
                Assert.AreEqual((MissionObjectiveState.Incomplete, MissionObjectiveState.Inactive, true),
                    (Objective(harness, Raid.MissionId, Raid.BombTheField), Objective(harness, Raid.MissionId, Raid.HackTheTerminal),
                        SceneObject(harness, copy, Raid.MissionId, Raid.ChargeRole).IsEnabled), "the player's own still refused: 1 open, 2 waits, his charge usable");

                var terminal = mate.Door(Raid.TerminalRole);
                CollectionAssert.AreEqual(new[] { Raid.HackTheTerminal }, TerminalTopics(harness, mate.Client, terminal), "the mate's own terminal hears his 2");
                TalkToTerminal(harness, mate.Client, terminal, Raid.MissionId, Raid.HackTheTerminal);
                Tick(harness, copy, 250);
                Assert.AreEqual(MissionObjectiveState.Inactive, Objective(harness, Raid.MissionId, Raid.HackTheTerminal), "the player's 2 not credited by the mate's talk");
                Assert.IsTrue(mate.Client.Player.Missions[Raid.MissionId].Completeable, "the mate's to be radioed in");
                MissionTestContext.Drain(mate.Client);
                Assert.IsTrue(harness.Manager.TryCompleteRadioMission(mate.Client, Raid.MissionId, 0, null), "the mate radios in");
                var read = MissionTestContext.Drain(mate.Client);
                Assert.AreEqual(1, read.OfType<DispenseRadioMissionPacket>().Count(packet => packet.MissionId == Blast.MissionId), "the mate's 1498 by radio");
                Assert.AreEqual(0, harness.Drain().OfType<DispenseRadioMissionPacket>().Count(packet => packet.MissionId == Blast.MissionId), "none to the player for the mate's");
            }
        }

        /// <summary>
        /// The front field is each member's own while his 1497 objective 1 is open (set 20's way through the HQ tunnel stays open):
        /// a player who never took 1497 has none (ESTIMATE: retail likely barred him; no mission, no object of his); in a fresh
        /// copy a player who completed 1497 has none; with a mate who completed 1497 the player's own field stands for him alone
        /// and the mate is shown none; an abandon takes the player's field away, and taken again it stands again (the fresh copy after 1497: next test).
        /// </summary>
        [TestMethod]
        public void TheFrontFieldStandsOnlyForAMemberWithHisOneOpen()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            AshenRuntime.SeedCompleted(harness, AshenTheBigSecretV1.MissionId, Lockdown.MissionId);
            var copy = IntoFacility(harness);
            Assert.IsEmpty(Fields(copy, harness.Client), "never took 1497: no field (ESTIMATE)");
            Assert.IsFalse(ForceFields.StopsPlayer(copy, Tunnel[0], Tunnel[1]), "nothing public in the tunnel");

            var peacock = Giver(copy, N.Peacock);
            Accept(harness, peacock, Raid.MissionId);
            Tick(harness, copy, 250);
            AssertFieldUp(harness, copy, harness.Client, "1 open");
            using var mate = new Facility.SquadMate(harness, copy, peacock, 0, new[] { AshenTheBigSecretV1.MissionId, Lockdown.MissionId, Raid.MissionId });
            Assert.IsEmpty(Fields(copy, mate.Client), "the mate who did 1497: none shown to him, not even the player's");

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Raid.MissionId));
            harness.Drain();
            for (var tick = 0; tick < 4; tick++)
                Tick(harness, copy, 250);
            Assert.IsEmpty(Fields(copy, harness.Client), "abandoned: his field goes");
            Accept(harness, Giver(copy, N.Peacock), Raid.MissionId);
            Tick(harness, copy, 250);
            AssertFieldUp(harness, copy, harness.Client, "taken again");
        }

        /// <summary>A fresh copy for a player who completed 1497: no field of his, nothing in the tunnel (set 20's way).</summary>
        [TestMethod]
        public void AFreshCopyAfterTheRaidHasNoFrontField()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            AshenRuntime.SeedCompleted(harness, AshenTheBigSecretV1.MissionId, Lockdown.MissionId, Raid.MissionId);
            var copy = IntoFacility(harness);
            Tick(harness, copy, 250);
            Assert.IsEmpty(Fields(copy, harness.Client), "1497 done: no field in a new copy");
            Assert.IsNull(FindSceneObject(harness, copy, Raid.MissionId, Raid.FieldRole), "no 1497 scene");
            Assert.IsFalse(ForceFields.StopsPlayer(copy, Tunnel[0], Tunnel[1]), "the tunnel open on the server");
        }

        /// <summary>A full bag at Raid's accept: no mission, no Detonator; one slot freed, the accept gives it.</summary>
        [TestMethod]
        public void RaidsFullBagWaitsForTheDetonator()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            AshenRuntime.SeedCompleted(harness, AshenTheBigSecretV1.MissionId, Lockdown.MissionId);
            var copy = IntoFacility(harness);
            var peacock = Giver(copy, N.Peacock);
            WildernessAliaBranchesTests.FillCategory(harness, Facility.MissionItemFiller, 50);
            harness.MoveTo(peacock.Position);
            harness.Drain();
            Assert.IsFalse(harness.Manager.AcceptOfferedMission(harness.Client, peacock.EntityId, Raid.MissionId), "no room for the Detonator");
            Assert.IsFalse(harness.Client.Player.Missions.ContainsKey(Raid.MissionId));
            Assert.AreEqual(0U, Facility.Held(harness, Raid.DetonatorTemplate));
            WildernessAliaBranchesTests.FreeOneSlot(harness, Facility.MissionItemFiller);
            Accept(harness, peacock, Raid.MissionId);
            Assert.AreEqual(1U, Facility.Held(harness, Raid.DetonatorTemplate), "a slot free: the Detonator");
        }

        /// <summary>
        /// Blast the Biolab: no radio offer before Raid; with Raid done the arrival in the facility offers it by radio (not Peacock's
        /// to give). Taken: 1 and 2 open, 3-5 wait, 6 and 7 hidden; the player's teleporter dark and not usable, his lab charge not
        /// usable, no Slocombe. A Bane kill at a roll of 50 drops nothing, at 49 one bio material or machine part each (each only
        /// while its objective wants more); five of each complete 1 and 2, and the hidden 6 and 7 open 3: the teleporter usable.
        /// Its use completes 3: it comes on, used no more, and the player's own Slocombe stands by it (also after a reload). He hears
        /// 4; his talk takes the five and five and gives the bomb; 5 opens and the lab's charge is usable; the charge's use takes
        /// the bomb and sets the charge; radioed in it pays once; everything goes with the mission.
        /// </summary>
        [TestMethod]
        public void BlastTheBiolabGathersBringsSlocombeAndBlowsTheLab()
        {
            var roll = 0;
            using var harness = WildernessRuntimeTestHarness.Create(lootRoll: (minimum, maximum) => maximum == 100 ? roll : minimum);
            AshenRuntime.SeedCompleted(harness, AshenTheBigSecretV1.MissionId, Lockdown.MissionId);
            var copy = IntoFacility(harness);
            harness.Manager.OfferArrivalMissions(harness.Client);
            Assert.AreEqual(0, harness.Drain().OfType<DispenseRadioMissionPacket>().Count(packet => packet.MissionId == Blast.MissionId), "not before 1497");
            AshenRuntime.SeedCompleted(harness, Raid.MissionId);
            harness.Manager.OfferArrivalMissions(harness.Client);
            Assert.AreEqual(1, harness.Drain().OfType<DispenseRadioMissionPacket>().Count(packet => packet.MissionId == Blast.MissionId), "1498 on arrival");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, Giver(copy, N.Peacock), Blast.MissionId), "not Peacock's to give");
            Assert.IsTrue(harness.Manager.TryAcceptRadioMission(harness.Client, Blast.MissionId), "taken by radio");
            harness.Drain();
            Tick(harness, copy, 250);
            Assert.AreEqual((MissionObjectiveState.Incomplete, MissionObjectiveState.Incomplete, MissionObjectiveState.Inactive, MissionObjectiveState.Inactive,
                    MissionObjectiveState.Inactive),
                (Objective(harness, Blast.MissionId, Blast.BioMaterials), Objective(harness, Blast.MissionId, Blast.MechanicalParts),
                    Objective(harness, Blast.MissionId, Blast.Teleporter), Objective(harness, Blast.MissionId, Blast.GiveToSlocombe),
                    Objective(harness, Blast.MissionId, Blast.DestroyTheLab)), "1, 2 open; 3-5 wait");
            var visible = harness.Manager.BuildStatusSnapshot(harness.Client.Player)[Blast.MissionId].ObjectivesList.Select(objective => objective.ObjectiveId).ToArray();
            foreach (var hidden in new[] { Blast.Gathered, Blast.Opener })
                Assert.IsFalse(visible.Contains(hidden), $"{hidden} is hidden");
            Assert.IsNull(FindSceneObject(harness, copy, Blast.MissionId, Blast.TeleporterRole), "no teleporter before 3");
            var charge = SceneObject(harness, copy, Blast.MissionId, Blast.ChargeRole);
            Assert.AreEqual((Blast.ChargeClass, false, V(AshenFixMV1.LabChargeAt)), ((uint)charge.EntityClassId, charge.IsEnabled, charge.Position), "the lab's charge, not usable yet");
            Assert.IsNull(SceneCreature(harness, copy, Blast.MissionId, Blast.SlocombeRole), "no Slocombe yet");

            // 1, 2: the drops.
            roll = Blast.DropChance;
            var none = KillOne(harness, copy, Blast.BioPool, Blast.BioCreatures);
            Assert.IsFalse(Holds(none, Blast.BioTemplate), "a roll of 50: no bio material");
            roll = Blast.DropChance - 1;
            Gather(harness, copy, Blast.BioPool, Blast.BioCreatures, Blast.BioTemplate, () => Objective(harness, Blast.MissionId, Blast.BioMaterials));
            Assert.AreEqual((Blast.EachCount, MissionObjectiveState.Completed, MissionObjectiveState.Incomplete, MissionObjectiveState.Inactive),
                (Facility.Held(harness, Blast.BioTemplate), Objective(harness, Blast.MissionId, Blast.BioMaterials), Objective(harness, Blast.MissionId, Blast.Gathered),
                    Objective(harness, Blast.MissionId, Blast.Teleporter)), "five bio materials: 1 done; 6 and 3 wait for 2");
            Assert.IsFalse(Holds(KillOne(harness, copy, Blast.BioPool, Blast.BioCreatures), Blast.BioTemplate), "no sixth");
            Gather(harness, copy, Blast.PartsPool, Blast.PartsCreatures, Blast.PartsTemplate, () => Objective(harness, Blast.MissionId, Blast.MechanicalParts));
            Assert.AreEqual((Blast.EachCount, MissionObjectiveState.Completed, MissionObjectiveState.Completed, MissionObjectiveState.Incomplete, true),
                (Facility.Held(harness, Blast.PartsTemplate), Objective(harness, Blast.MissionId, Blast.MechanicalParts), Objective(harness, Blast.MissionId, Blast.Gathered),
                    Objective(harness, Blast.MissionId, Blast.Teleporter), SceneObject(harness, copy, Blast.MissionId, Blast.TeleporterRole).IsEnabled),
                "five machine parts: 2 and 6 done, 3 open, the teleporter usable");
            var teleporter = SceneObject(harness, copy, Blast.MissionId, Blast.TeleporterRole);
            Assert.AreEqual((Blast.TeleporterClass, (UseObjectState)Blast.TeleporterOff, true, ToVector(AshenFixS2V1.TeleporterAt), harness.Client.Player.Id),
                ((uint)teleporter.EntityClassId, teleporter.StateId, teleporter.IsEnabled, teleporter.Position, teleporter.SceneOwnerCharacterId), "the player's own teleporter, dark, usable");

            // 3: the teleporter; Slocombe comes.
            Use(harness, copy, SceneObject(harness, copy, Blast.MissionId, Blast.TeleporterRole), Blast.UseArgument);
            Tick(harness, copy, 250);
            teleporter = SceneObject(harness, copy, Blast.MissionId, Blast.TeleporterRole);
            Assert.AreEqual((MissionObjectiveState.Completed, MissionObjectiveState.Incomplete, (UseObjectState)Blast.TeleporterOn, false),
                (Objective(harness, Blast.MissionId, Blast.Teleporter), Objective(harness, Blast.MissionId, Blast.GiveToSlocombe), teleporter.StateId, teleporter.IsEnabled),
                "3 done, 4 open; the teleporter on, used no more");
            ReloadAndRecover(harness, copy);
            var slocombe = SceneCreature(harness, copy, Blast.MissionId, Blast.SlocombeRole);
            Assert.IsNotNull(slocombe, "Slocombe stands by the teleporter, also after a reload");
            Assert.AreEqual((N.Slocombe, N.SlocombePackage, TargetCategory.Friendly), (slocombe.DbId, slocombe.Npc?.NpcPackageId, slocombe.TargetCategory), "a copy of his row");
            Assert.IsLessThan(1.5f, Vector3.Distance(ToVector(AshenFixS2V1.SlocombeAt), slocombe.Position), "at his place");
            Assert.IsTrue(Rasa.Game.Missions.World.CreatureGameplayRules.IsInvulnerable(slocombe), "unhurt by anything");
            Assert.AreEqual(((UseObjectState)Blast.TeleporterOn, false),
                (SceneObject(harness, copy, Blast.MissionId, Blast.TeleporterRole).StateId, SceneObject(harness, copy, Blast.MissionId, Blast.TeleporterRole).IsEnabled), "on after a reload");

            // 4: Slocombe's talk; 5: the lab's charge.
            CollectionAssert.AreEqual(new[] { Blast.GiveToSlocombe }, Topics(harness, slocombe, Blast.MissionId), "Slocombe hears 4");
            Talk(harness, slocombe, Blast.MissionId, Blast.GiveToSlocombe);
            Tick(harness, copy, 250);
            Assert.AreEqual((0U, 0U, 1U, MissionObjectiveState.Incomplete, true),
                (Facility.Held(harness, Blast.BioTemplate), Facility.Held(harness, Blast.PartsTemplate), Facility.Held(harness, Blast.BombTemplate),
                    Objective(harness, Blast.MissionId, Blast.DestroyTheLab), SceneObject(harness, copy, Blast.MissionId, Blast.ChargeRole).IsEnabled),
                "the five and five taken, the bomb given, 5 open, the lab's charge usable");
            Use(harness, copy, SceneObject(harness, copy, Blast.MissionId, Blast.ChargeRole), Blast.UseArgument);
            Tick(harness, copy, 250);
            Assert.AreEqual((0U, MissionObjectiveState.Completed, false),
                (Facility.Held(harness, Blast.BombTemplate), Objective(harness, Blast.MissionId, Blast.DestroyTheLab), SceneObject(harness, copy, Blast.MissionId, Blast.ChargeRole).IsEnabled),
                "the bomb taken, 5 done, the charge set");
            Assert.IsTrue(harness.Client.Player.Missions[Blast.MissionId].Completeable, "to be radioed in");

            var before = harness.Context.ReadRewardTotals();
            Assert.IsTrue(harness.Manager.TryCompleteRadioMission(harness.Client, Blast.MissionId, 0, null), "radioed in");
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Blast.MissionId), "paid once");
            var after = harness.Context.ReadRewardTotals();
            Assert.AreEqual((before.Experience + Blast.Experience, before.Credits + (int)Blast.Credits), (after.Experience, after.Credits));
            Assert.AreEqual(1U, Facility.Held(harness, Blast.IncendiaryStaff), "the staff chosen");
            Assert.IsFalse(harness.Manager.TryCompleteRadioMission(harness.Client, Blast.MissionId, 0, null), "and only once");
            for (var tick = 0; tick < 4; tick++)
                Tick(harness, copy, 250);
            foreach (var role in new[] { Blast.TeleporterRole, Blast.ChargeRole })
                Assert.IsNull(FindSceneObject(harness, copy, Blast.MissionId, role), $"{role} goes with the mission");
            Assert.IsNull(SceneCreature(harness, copy, Blast.MissionId, Blast.SlocombeRole), "Slocombe goes with the mission");
        }

        /// <summary>
        /// Slocombe's talk with short counts: the bio materials gone from the bag after 3, his talk is refused: 4 open, the machine
        /// parts kept, no bomb, the lab's charge not usable.
        /// </summary>
        [TestMethod]
        public void SlocombesTalkIsRefusedWithShortCounts()
        {
            using var harness = WildernessRuntimeTestHarness.Create(lootRoll: (minimum, maximum) => maximum == 100 ? 0 : minimum);
            AshenRuntime.SeedCompleted(harness, AshenTheBigSecretV1.MissionId, Lockdown.MissionId, Raid.MissionId);
            var copy = IntoFacility(harness);
            TakeBlast(harness, harness.Client);
            Tick(harness, copy, 250);
            Gather(harness, copy, Blast.BioPool, Blast.BioCreatures, Blast.BioTemplate, () => Objective(harness, Blast.MissionId, Blast.BioMaterials));
            Gather(harness, copy, Blast.PartsPool, Blast.PartsCreatures, Blast.PartsTemplate, () => Objective(harness, Blast.MissionId, Blast.MechanicalParts));
            Use(harness, copy, SceneObject(harness, copy, Blast.MissionId, Blast.TeleporterRole), Blast.UseArgument);
            Tick(harness, copy, 250);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Blast.MissionId, Blast.GiveToSlocombe), "4 open");
            Lose(harness.Client, Blast.BioTemplate);
            var slocombe = SceneCreature(harness, copy, Blast.MissionId, Blast.SlocombeRole);
            harness.MoveTo(slocombe.Position);
            harness.Drain();
            harness.Manager.CompleteOfferedObjective(harness.Client, slocombe.EntityId, Blast.MissionId, Blast.GiveToSlocombe, 1);
            harness.Drain();
            Tick(harness, copy, 250);
            Assert.AreEqual((MissionObjectiveState.Incomplete, Blast.EachCount, 0U, false),
                (Objective(harness, Blast.MissionId, Blast.GiveToSlocombe), Facility.Held(harness, Blast.PartsTemplate), Facility.Held(harness, Blast.BombTemplate),
                    SceneObject(harness, copy, Blast.MissionId, Blast.ChargeRole).IsEnabled), "short: refused; 4 open, the parts kept, no bomb, the charge not usable");
        }

        /// <summary>
        /// Blast the Biolab in a squad of two on it, the mate away at the player's kills (the coordinator's set 17 lesson): the
        /// mate gets the radio offer on his own arrival and takes it. A kill with both by the body leaves each member his own bio
        /// material. The player gathers the rest with the mate away, opens 3 and uses his teleporter: the mate's 1 and 2 still
        /// open, his 3-5 waiting and his hidden 6 open, none of them credited, and the player's Slocombe not shown to him. The mate
        /// then gathers his own: his 3 opens and his own teleporter is usable; its use brings his own Slocombe; his talk takes his
        /// own five and five and gives him his own bomb.
        /// </summary>
        [TestMethod]
        public void BlastTheBiolabWaitsForAMateBehind()
        {
            using var harness = WildernessRuntimeTestHarness.Create(lootRoll: (minimum, maximum) => maximum == 100 ? 0 : minimum);
            var done = new[] { AshenTheBigSecretV1.MissionId, Lockdown.MissionId, Raid.MissionId };
            AshenRuntime.SeedCompleted(harness, done);
            var copy = IntoFacility(harness);
            TakeBlast(harness, harness.Client);
            Tick(harness, copy, 250);
            using var mate = new Facility.SquadMate(harness, copy, Giver(copy, N.Peacock), 0, done);
            TakeBlast(harness, mate.Client);
            Tick(harness, copy, 250);
            MateObjectives(mate, out var mateOf);
            Assert.AreEqual((MissionObjectiveState.Incomplete, MissionObjectiveState.Inactive), (mateOf(Blast.BioMaterials), mateOf(Blast.Teleporter)), "the mate's 1 open, 3 waits");
            Assert.IsNull(MateObject(harness, copy, mate.Client, Blast.MissionId, Blast.TeleporterRole), "no teleporter for the mate before his 3");

            // A kill with both by: each his own bio material.
            var corpse = KillOne(harness, copy, Blast.BioPool, new[] { Blast.BioCreatures[0] }, mate);
            var bio = corpse.LootItems.Where(item => item.ItemTemplateId == Blast.BioTemplate).ToArray();
            Assert.AreEqual((1, 1), (bio.Count(item => item.ReservedFor == harness.Client.Player.EntityId), bio.Count(item => item.ReservedFor == mate.Client.Player.EntityId)),
                "a bio material each");
            mate.Loot(corpse);
            PickUp(harness, corpse);
            harness.Drain();
            Tick(harness, copy, 250);
            Assert.AreEqual((1U, 1U), (Facility.Held(harness, Blast.BioTemplate), mate.Held(Blast.BioTemplate)), "held each");

            // The player on alone.
            mate.Away();
            Gather(harness, copy, Blast.BioPool, Blast.BioCreatures, Blast.BioTemplate, () => Objective(harness, Blast.MissionId, Blast.BioMaterials));
            Gather(harness, copy, Blast.PartsPool, Blast.PartsCreatures, Blast.PartsTemplate, () => Objective(harness, Blast.MissionId, Blast.MechanicalParts));
            mate.Back();
            Use(harness, copy, SceneObject(harness, copy, Blast.MissionId, Blast.TeleporterRole), Blast.UseArgument);
            Tick(harness, copy, 250);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Blast.MissionId, Blast.Teleporter), "the player's 3 done");
            Assert.AreEqual((MissionObjectiveState.Incomplete, MissionObjectiveState.Incomplete, MissionObjectiveState.Incomplete),
                (mateOf(Blast.BioMaterials), mateOf(Blast.MechanicalParts), mateOf(Blast.Gathered)), "the mate's 1, 2 and hidden 6 open");
            foreach (var objective in new[] { Blast.Teleporter, Blast.GiveToSlocombe, Blast.DestroyTheLab })
                Assert.AreEqual(MissionObjectiveState.Inactive, mateOf(objective), $"the mate's {objective} waits: not credited by the player's");
            var mine = SceneCreature(harness, copy, Blast.MissionId, Blast.SlocombeRole);
            Assert.IsNotNull(mine, "the player's Slocombe");
            Assert.IsFalse(MissionNpcVisibility.IsShownTo(mate.Client, mine), "not shown to the mate");
            Assert.IsNull(MateObject(harness, copy, mate.Client, Blast.MissionId, Blast.TeleporterRole), "still no teleporter for the mate (his 3 waits)");

            // The mate catches up.
            for (var kill = 0; kill < 40 && (mate.Held(Blast.BioTemplate) < Blast.EachCount || mate.Held(Blast.PartsTemplate) < Blast.EachCount); kill++)
            {
                var bioShort = mate.Held(Blast.BioTemplate) < Blast.EachCount;
                var body = KillOne(harness, copy, bioShort ? Blast.BioPool : Blast.PartsPool, bioShort ? Blast.BioCreatures : Blast.PartsCreatures, mate);
                mate.Loot(body);
                Tick(harness, copy, 250);
            }
            Assert.AreEqual((Blast.EachCount, Blast.EachCount, MissionObjectiveState.Incomplete),
                (mate.Held(Blast.BioTemplate), mate.Held(Blast.PartsTemplate), mateOf(Blast.Teleporter)), "the mate's five and five: his 3 open");
            var his = MateObject(harness, copy, mate.Client, Blast.MissionId, Blast.TeleporterRole);
            Assert.IsTrue(his.IsEnabled, "his teleporter usable");
            mate.Use(his, Blast.UseArgument);
            Assert.AreEqual((MissionObjectiveState.Completed, MissionObjectiveState.Incomplete), (mateOf(Blast.Teleporter), mateOf(Blast.GiveToSlocombe)), "his 3 done, 4 open");
            var hisSlocombe = MateSlocombe(harness, copy, mate.Client);
            Assert.IsNotNull(hisSlocombe, "his own Slocombe");
            Assert.AreNotSame(mine, hisSlocombe, "not the player's");
            mate.Beside(hisSlocombe.Position + new Vector3(0, 0, 1));
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(mate.Client, hisSlocombe.EntityId, Blast.MissionId, Blast.GiveToSlocombe, 1), "the mate's talk");
            harness.Drain();
            Tick(harness, copy, 250);
            Assert.AreEqual((0U, 0U, 1U, MissionObjectiveState.Incomplete),
                (mate.Held(Blast.BioTemplate), mate.Held(Blast.PartsTemplate), mate.Held(Blast.BombTemplate), mateOf(Blast.DestroyTheLab)), "his own taken, his own bomb, his 5 open");
            Assert.AreEqual((Blast.EachCount, Blast.EachCount, MissionObjectiveState.Incomplete),
                (Facility.Held(harness, Blast.BioTemplate), Facility.Held(harness, Blast.PartsTemplate), Objective(harness, Blast.MissionId, Blast.GiveToSlocombe)),
                "the player's own untouched, his 4 still open");
        }

        /// <summary>
        /// An abandon of Blast the Biolab after Slocombe came: the teleporter, the charge and Slocombe go; the bio materials and
        /// machine parts are kept (each a member's own drop). Taken again by radio, the held ones count at once: 1, 2 done, 3 open,
        /// the teleporter usable again.
        /// </summary>
        [TestMethod]
        public void BlastTheBiolabAbandonedKeepsTheGatheredOnes()
        {
            using var harness = WildernessRuntimeTestHarness.Create(lootRoll: (minimum, maximum) => maximum == 100 ? 0 : minimum);
            AshenRuntime.SeedCompleted(harness, AshenTheBigSecretV1.MissionId, Lockdown.MissionId, Raid.MissionId);
            var copy = IntoFacility(harness);
            TakeBlast(harness, harness.Client);
            Tick(harness, copy, 250);
            Gather(harness, copy, Blast.BioPool, Blast.BioCreatures, Blast.BioTemplate, () => Objective(harness, Blast.MissionId, Blast.BioMaterials));
            Gather(harness, copy, Blast.PartsPool, Blast.PartsCreatures, Blast.PartsTemplate, () => Objective(harness, Blast.MissionId, Blast.MechanicalParts));
            Use(harness, copy, SceneObject(harness, copy, Blast.MissionId, Blast.TeleporterRole), Blast.UseArgument);
            Tick(harness, copy, 250);
            Assert.IsNotNull(SceneCreature(harness, copy, Blast.MissionId, Blast.SlocombeRole), "Slocombe came");
            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Blast.MissionId));
            harness.Drain();
            for (var tick = 0; tick < 4; tick++)
                Tick(harness, copy, 250);
            foreach (var role in new[] { Blast.TeleporterRole, Blast.ChargeRole })
                Assert.IsNull(FindSceneObject(harness, copy, Blast.MissionId, role), $"{role} goes with the abandon");
            Assert.IsNull(SceneCreature(harness, copy, Blast.MissionId, Blast.SlocombeRole), "Slocombe goes with the abandon");
            Assert.AreEqual((Blast.EachCount, Blast.EachCount), (Facility.Held(harness, Blast.BioTemplate), Facility.Held(harness, Blast.PartsTemplate)), "kept");

            TakeBlast(harness, harness.Client);
            Tick(harness, copy, 250);
            Tick(harness, copy, 250);
            Assert.AreEqual((MissionObjectiveState.Completed, MissionObjectiveState.Completed, MissionObjectiveState.Incomplete, true),
                (Objective(harness, Blast.MissionId, Blast.BioMaterials), Objective(harness, Blast.MissionId, Blast.MechanicalParts),
                    Objective(harness, Blast.MissionId, Blast.Teleporter), SceneObject(harness, copy, Blast.MissionId, Blast.TeleporterRole).IsEnabled),
                "taken again: the held ones count, 3 open, the teleporter usable");

            // Rebuilt from the journal (a restart, a fresh or a squad copy): the teleporter's last Ensure is before its
            // SetInteraction (one operation key for both), so it comes back usable.
            ReloadAndRecover(harness, copy);
            Assert.AreEqual((MissionObjectiveState.Incomplete, true),
                (Objective(harness, Blast.MissionId, Blast.Teleporter), SceneObject(harness, copy, Blast.MissionId, Blast.TeleporterRole).IsEnabled),
                "rebuilt: 3 open, the teleporter usable");
            harness.Manager.Scenes.Detach(harness.Client, copy);
            foreach (var obj in copy.DynamicObjects.Where(obj => obj.SceneMissionId == Blast.MissionId).ToArray())
            {
                harness.Objects.DynamicObjectDestroy(copy, obj);
                copy.DynamicObjects.Remove(obj);
            }
            Assert.IsNull(FindSceneObject(harness, copy, Blast.MissionId, Blast.TeleporterRole), "a fresh copy: no teleporter yet");
            harness.Manager.Scenes.Resume(harness.Client);
            harness.Drain();
            Tick(harness, copy, 250);
            Tick(harness, copy, 250);
            Assert.IsTrue(SceneObject(harness, copy, Blast.MissionId, Blast.TeleporterRole).IsEnabled, "made again in a fresh copy: the teleporter usable");
        }

        /// <summary>
        /// Ashen_conscription_entry takes out 1500, 1497 and 1498, Peacock and Slocombe (creature, stats, look, packages) with
        /// Peacock's pool and the pads 921-922 with their links (no world field: none on the facility before or after); the row they copy (Harris) stays; it updates
        /// nothing. Every table the migration writes is counted (set 19's ids and missions only). Nothing else changes: the other
        /// pools, pads, fields, missions and markers as a database migrated only up to Ashen_conscription_entry holds them. Up
        /// again, all of it is back; the NPC SQL run again changes nothing.
        /// </summary>
        [TestMethod]
        public void ConscriptionEntryRollsBackWithItsMigration()
        {
            using var harness = WildernessRuntimeTestHarness.Create(targetWorldMigration: AshenMissionTests.ConscriptionEntry);
            var world = harness.World;
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking()
                .Any(entry => entry.MissionId == mission && entry.ContentRevision == AshenMissionDataV1.Revision);
            int Count(string sql) => world.Database.SqlQueryRaw<int>(sql).AsEnumerable().Single();
            const string ids = "(mission_id IN (1500, 1497, 1498) AND content_revision = 'ashen_1_16_5')";
            var npcs = string.Join(", ", N.Creatures);
            var pads = string.Join(", ", Pads);
            var tables = new[]
            {
                "mission_content_definition", "mission_objective_definition", "mission_objective_transition", "mission_trigger", "mission_action",
                "mission_prerequisite", "mission_reward_definition", "mission_reward_item", "mission_indicator", "mission_scene_binding", "mission_evidence",
                "mission_scenario", "mission_scenario_step", "mission_channel_policy", "mission_repeat_policy", "mission_spawn", "mission_spawn_group", "mission_area"
            };
            string All() => string.Join(", ", tables.Select(table => $"{table} {Count($"SELECT COUNT(*) AS Value FROM {table} WHERE {ids}")}")
                .Concat(new[] { "creature", "creature_stat", "creature_appearance", "npc_package", "spawnpool" }
                    .Select(table => $"{table} {Count($"SELECT COUNT(*) AS Value FROM {table} WHERE id IN ({npcs})")}"))
                .Append($"world_destructible {Count($"SELECT COUNT(*) AS Value FROM world_destructible WHERE map_context_id = {Map}")}")
                .Append($"teleporter {Count($"SELECT COUNT(*) AS Value FROM teleporter WHERE id IN ({pads})")}")
                .Append($"local_teleporter_link {Count($"SELECT COUNT(*) AS Value FROM local_teleporter_link WHERE from_teleporter_id IN ({pads}) OR to_teleporter_id IN ({pads})")}")
                .Append($"kept {Count($"SELECT COUNT(*) AS Value FROM creature WHERE id = {N.Harris}")}"));
            static string Others(DbContext db) => string.Join("; ", db.Set<SpawnPoolEntry>().AsNoTracking()
                    .Where(entry => (entry.MapContextId == AshenMissionDataV1.MapContextId || entry.MapContextId == N.Facility) && entry.Id != N.Peacock)
                    .OrderBy(entry => entry.Id).AsEnumerable()
                    .Select(entry => FormattableString.Invariant($"{entry.Id}:{entry.PosX:R},{entry.PosY:R},{entry.PosZ:R},{entry.Radius:R},{entry.Creature1Id},{entry.Creature1MaxCount}")))
                + " | " + string.Join("; ", db.Set<TeleporterEntry>().AsNoTracking().Where(entry => entry.Id != N.HallPad && entry.Id != N.ServicePad)
                    .OrderBy(entry => entry.Id).AsEnumerable().Select(entry => FormattableString.Invariant($"{entry.Id}:{entry.MapContextId},{entry.PosX:R},{entry.PosZ:R}")))
                + " | " + string.Join("; ", db.Set<WorldDestructibleEntry>().AsNoTracking()
                    .OrderBy(entry => entry.Id).AsEnumerable().Select(entry => FormattableString.Invariant($"{entry.Id}:{entry.MapContextId},{entry.ClassId},{entry.PosX:R},{entry.PosZ:R}")))
                + " | " + string.Join("; ", db.Set<MissionContentDefinitionEntry>().AsNoTracking()
                    .Where(entry => entry.MissionId != 1500 && entry.MissionId != 1497 && entry.MissionId != 1498)
                    .OrderBy(entry => entry.MissionId).ThenBy(entry => entry.ContentRevision)
                    .AsEnumerable().Select(entry => $"{entry.MissionId}/{entry.ContentRevision}"))
                + " | " + string.Join("; ", db.Set<MissionIndicatorEntry>().AsNoTracking()
                    .Where(entry => (entry.ContentRevision == AshenMissionDataV1.Revision || entry.ContentRevision == CrucibleMissionDataV1.Revision)
                        && entry.MissionId != 1500 && entry.MissionId != 1497 && entry.MissionId != 1498)
                    .OrderBy(entry => entry.MissionId).ThenBy(entry => entry.ObjectiveId).ThenBy(entry => entry.IndicatorId).AsEnumerable()
                    .Select(entry => FormattableString.Invariant($"{entry.MissionId}.{entry.ObjectiveId}/{entry.IndicatorId}:{entry.PosX:R},{entry.PosZ:R}")));
            string others;
            using (var atEntry = WildernessRuntimeTestHarness.Create(targetWorldMigration: AshenMissionTests.ConscriptionEntry))
                others = Others(atEntry.World);
            // 1500: 1 objective and transition, the talk; the reward; two markers. 1497: 2 objectives and transitions, the charge's
            // event and the terminal's talk; actions: 1 the Detonator's take, the field's scenario, 2's reveal and activate; 2 the
            // reward; scenarios 1-2 (1's counted: one step), the radio turn-in's channel. 1498: 7 objectives, 6 transitions (6 is the
            // scene's aggregation); triggers: 1, 2 the items, 7 the state of 6, 3 and 5 the
            // events, 4 the talk; actions (7: open 3, its scenario; 3: Slocombe's scenario, open 4; 4: two takes, the bomb, open 5,
            // its scenario; 5: the take, its scenario, the reward); scenarios 1-6 (1 and 2 counted: two steps), the radio's channel;
            // five markers. Each one prerequisite, one reward
            // (four items), three evidence rows; three scenes (1500's for its marker on the facility). Peacock and Slocombe with
            // stats, packages, Harris' look rows; Peacock's pool; no world field; two pads and two links.
            const string up = "mission_content_definition 3, mission_objective_definition 10, mission_objective_transition 9, mission_trigger 9, " +
                "mission_action 21, mission_prerequisite 3, mission_reward_definition 3, mission_reward_item 12, mission_indicator 9, " +
                "mission_scene_binding 3, mission_evidence 9, mission_scenario 8, mission_scenario_step 3, mission_channel_policy 2, " +
                "mission_repeat_policy 0, mission_spawn 0, mission_spawn_group 0, mission_area 0, creature 2, creature_stat 2, " +
                "creature_appearance LOOK, npc_package 2, spawnpool 1, world_destructible 0, teleporter 2, local_teleporter_link 2, kept 1";
            const string down = "mission_content_definition 0, mission_objective_definition 0, mission_objective_transition 0, mission_trigger 0, " +
                "mission_action 0, mission_prerequisite 0, mission_reward_definition 0, mission_reward_item 0, mission_indicator 0, " +
                "mission_scene_binding 0, mission_evidence 0, mission_scenario 0, mission_scenario_step 0, mission_channel_policy 0, " +
                "mission_repeat_policy 0, mission_spawn 0, mission_spawn_group 0, mission_area 0, creature 0, creature_stat 0, " +
                "creature_appearance 0, npc_package 0, spawnpool 0, world_destructible 0, teleporter 0, local_teleporter_link 0, kept 1";
            var look = Count($"SELECT COUNT(*) AS Value FROM creature_appearance WHERE id = {N.Harris}") * 2;
            var upNow = up.Replace("LOOK", look.ToString(System.Globalization.CultureInfo.InvariantCulture));
            void AllUp(string when)
            {
                Assert.IsTrue(Missions.All(Has), $"{when}: the three missions");
                var all = All();
                Assert.AreEqual(upNow, all, $"{when}: {all}");
                Assert.AreEqual(others, Others(world), $"{when}: nothing else changes");
            }

            AllUp("at its migration");
            world.GetService<IMigrator>().Migrate(AshenMissionTests.Previous(world, AshenMissionTests.ConscriptionEntry));
            world.ChangeTracker.Clear();
            Assert.IsFalse(Missions.Any(Has));
            Assert.AreEqual(down, All());
            Assert.AreEqual(others, Others(world), "Down: nothing else changes");
            Assert.IsTrue(AshenMissionTests.MissionIds.Where(id => !Missions.Contains(id) && !AshenConscriptionQueenTests.Missions.Contains(id)).All(Has), "sets 1-18 stay");
            foreach (var sql in N.PadsDown())
                world.Database.ExecuteSqlRaw(sql);
            foreach (var sql in N.Down())
                world.Database.ExecuteSqlRaw(CrucibleSql.Guard(sql));
            Assert.AreEqual(down, All(), "a second Down changes nothing");

            world.GetService<IMigrator>().Migrate(AshenMissionTests.ConscriptionEntry);
            world.ChangeTracker.Clear();
            AllUp("Up again");
            foreach (var sql in N.Up())
                world.Database.ExecuteSqlRaw(CrucibleSql.Guard(sql));
            foreach (var sql in N.PadsUp())
                world.Database.ExecuteSqlRaw(sql);
            AllUp("the NPC and pad rows run again");
        }

        /// <summary>
        /// The squad's copy of the facility, entered by its door from Ashen Desert (as AshenAvernusOutpostTests.IntoOutpost), and an
        /// earlier test's leftovers gone: Peacock's cleared mark, the scene copies of runs this test's database does not hold.
        /// </summary>
        internal static MapChannel IntoFacility(WildernessRuntimeTestHarness harness)
        {
            if (!harness.Maps.MapChannelArray.ContainsKey(AshenMissionDataV1.MapContextId))
                PlateauInstanceMaps.LoadMap(harness, AshenMissionDataV1.MapContextId);
            if (!harness.Maps.MapChannelArray.ContainsKey(Map))
                PlateauInstanceMaps.LoadMap(harness, Map);
            var copy = PlateauInstanceMaps.ThroughTheDoor(harness, Map);
            foreach (var pool in copy.SpawnPools.Where(pool => Pools.Contains(pool.DbId)))
                pool.ClearedAtUtcMs = 0;
            var runs = Missions.SelectMany(mission => Runs(harness, mission)).ToHashSet();
            copy.SpawnPools.RemoveAll(pool => pool.SceneRunId != null && Missions.Contains(pool.ScenarioMissionId) && !runs.Contains(pool.SceneRunId));
            // An earlier test's scene objects stay in the shared copy (the same character id): drop those of no live run.
            copy.DynamicObjects.RemoveAll(obj => obj.SceneRunId != null && Missions.Contains(obj.SceneMissionId) && !runs.Contains(obj.SceneRunId));
            foreach (var creature in Here(copy).Where(one => Pools.Contains(one.SpawnPool?.DbId ?? 0)).ToArray())
                if (creature.State == CharacterState.Dead)
                    Respawn(copy, creature.SpawnPool.DbId);
            harness.Manager.Scenes.Resume(harness.Client);
            harness.Drain();
            return copy;
        }

        /// <summary>The hostile spawn pools of the facility (as AshenAvernusOutpostTests.HostilePools on Avernus Outpost).</summary>
        internal static List<SpawnPoolEntry> HostilePools(WildernessRuntimeTestHarness harness) =>
            harness.World.SpawnPoolEntries.AsNoTracking().Where(row => row.MapContextId == Map && row.Mode == 0).AsEnumerable()
                .Where(row => new[] { row.Creature1Id, row.Creature2Id, row.Creature3Id, row.Creature4Id, row.Creature5Id, row.Creature6Id }
                    .Any(id => id != 0 && CreatureManager.Instance.LoadedCreatures.TryGetValue(id, out var creature) && creature.TargetCategory == TargetCategory.Hostile))
                .ToList();

        /// <summary>
        /// The set's places: label, place, whether it is seen from the ring, the height looked at, its clearance (34 m, 2D, outside
        /// every hostile pool's circle; the breaks, each the nearest upstream pool on the only floor there: the field's charge
        /// 20.8 m from 531081 (the HQ tunnel's mouth), the lab's charge (fix M's place, 9 m south of the Atta Laboratory's door)
        /// 20.6 and 20.4 m outside 531064's and 531066's circles and 31.4 m from the boss pool 520002 (the Atta Imperial Guard, 35 m rule); docs/ashen-playtest.md).
        /// </summary>
        internal static List<(string Label, Vector3 At, bool Ring, float Middle, float Clearance)> Places() => new()
        {
            ("Peacock", V(N.PeacockAt), true, F.NpcMiddle, Clearance), ("the field's charge", ToVector(Raid.ChargeAt), true, F.ObjectMiddle, 20),
            ("the terminal", ToVector(AshenFixSV1.TerminalAt), true, F.ObjectMiddle, Clearance), ("the teleporter", ToVector(AshenFixS2V1.TeleporterAt), true, F.ObjectMiddle, Clearance),
            ("Slocombe", ToVector(AshenFixS2V1.SlocombeAt), true, F.NpcMiddle, Clearance), ("the hall pad", V(AshenFixS2V1.PadAt(N.HallPad)), false, 0, Clearance),
            ("the service pad", V(N.ServicePadAt), false, 0, Clearance), ("the lab's charge", V(AshenFixMV1.LabChargeAt), true, F.ObjectMiddle, 20)
        };

        private const float Clearance = 34;

        /// <summary>How far each leg's start is moved along its first stretch for the walk's recheck (as AshenAvernusHiveTests).</summary>
        private static readonly float[] StartShifts = { 0, 0.13f, 0.27f, 0.4f };

        /// <summary>The upstream pools a walk passes (rule breaks, docs/ashen-playtest.md): the yard's and the HQ tunnel's mouth's, and the biolab's own.</summary>
        private static readonly uint[] TunnelMouthPools = { 531067, 531081 };
        private static readonly uint[] YardPools = { 531067, 531081, 531083 };
        private static readonly uint[] LabPools = { 531064, 531066, 520002 };

        /// <summary>
        /// The legs of the set's walks on the client's floor, each walked there and back on its own (corners: a client-floor
        /// flood's way, 2026-11-12, ESTIMATE of a player's line). A walk is a run of legs.
        /// </summary>
        internal static readonly Dictionary<string, Vector3[]> Legs = new()
        {
            ["the door to Peacock"] = new Vector3[]
            {
                new(7.25f, 153.5f, -668f), new(6f, 153.36f, -650f), new(6f, 153.36f, -646f)
            },
            ["Peacock to the field's charge"] = new Vector3[]
            {
                new(6f, 153.36f, -646f), new(4f, 151.4f, -606f), new(3f, 145.78f, -566f), new(-3f, 143.6f, -540f), new(-2f, 137.09f, -495f),
                new(0f, 136.09f, -455f), new(6f, 137.39f, -440f), new(6.5f, 137.61f, -412f)
            },
            // Fix S: to the terminal on the HQ tunnel's end deck (set 19's way on into the "hall" ran on terrain the real client has
            // no floor on: AshenFixSTests), mid-tunnel (x 7-7.5, the tunnel x 4-10) from the charge's east side, first on the tester's line (9.5, -410) > (5, -390).
            ["the charge to the terminal"] = new Vector3[]
            {
                new(6.5f, 137.61f, -412f), new(9.5f, 137.61f, -410f), new(7.25f, 137.61f, -400f), new(7.5f, 137.61f, -386f), new(9.5f, 137.61f, -380f)
            },
            // Fix S2: down the spiral tunnel ramp (class 4153) to Central Operations' north door, on the ramp's mesh (the "hall" above
            // it is terrain the client has no floor on); its first loop the real client's own (2026-11-13 20:02, 1 m further out
            // at x 14), the rest a mesh-only flood's way (AshenFixS2Tests): it keeps 2 m from every drop, both ways.
            ["the terminal to Central Operations"] = new Vector3[]
            {
                new(9.5f, 137.61f, -380f), new(12f, 137.5f, -378f), new(14f, 134.9f, -369f), new(14f, 132.92f, -366f), new(12f, 130.42f, -361f),
                new(1.5f, 126.69f, -361.5f), new(-2f, 125.6f, -362f), new(-2f, 121.92f, -371.5f), new(-1f, 120.3f, -374.5f), new(3.65f, 119.36f, -374.85f),
                new(12f, 114.49f, -374.5f), new(13.5f, 112.02f, -369.5f), new(13.5f, 111.14f, -368.5f), new(7.5f, 105.27f, -351f), new(7f, 103.97f, -343f),
                new(7f, 103.81f, -340f)
            },
            ["Central Operations to Slocombe"] = new Vector3[]
            {
                new(7f, 103.81f, -340f), new(15.5f, 103.62f, -327.5f), new(18.3f, 103.65f, -328.2f)
            },
            // Onto pad 921 over its shallow north rim (its frame stands on its east side).
            ["Central Operations to the hall pad"] = new Vector3[]
            {
                new(7f, 103.81f, -340f), new(-4.5f, 103.62f, -321f), new(-7f, 103.62f, -321f), new(-7f, 104.27f, -323.4f)
            },
            ["the service pad to the lab's charge"] = new Vector3[]
            {
                new(-161.5f, 89.5f, -245f), new(-163f, 105.81f, -205f), new(-163f, 105.58f, -181f), new(-160f, 105.59f, -157f), new(-162f, 105.98f, -156f),
                new(-176f, 106f, -156f), new(-177f, 105.78f, -155f), new(-194f, 96.63f, -175f), new(-196f, 96f, -178f), new(-211.41f, 96.01f, -169.84f)
            },
        };


        /// <summary>
        /// Where the hall pad's walk ends (fix S2, Central Operations): on its disc 1.6 m north of its centre, inside the pad's 2 m
        /// trigger (DynamicObjectManager.LocalTeleporterWorker, IsNear2m).
        /// </summary>
        private static readonly Vector3 HallPadFoot = new(-7f, 104.27f, -323.4f);

        /// <summary>Where the lab's walk ends: beside its charge, 1.8 m back along the way in (not on it).</summary>
        private static readonly Vector3 LabChargeSide = new(-211.41f, 96.01f, -169.84f);

        /// <summary>The walks: label, its legs, the pools it passes (rule breaks), its target.</summary>
        internal static List<(string Label, string[] Legs, uint[] Passes, Vector3 Target)> Walks()
        {
            var none = Array.Empty<uint>();
            return new List<(string, string[], uint[], Vector3)>
            {
                ("the door to Peacock (1500)", new[] { "the door to Peacock" }, none, V(N.PeacockAt)),
                ("Peacock to the field's charge (1497)", new[] { "Peacock to the field's charge" }, YardPools, new Vector3(6.5f, 137.61f, -412f)),
                ("the charge to the terminal (1497)", new[] { "the charge to the terminal" }, TunnelMouthPools, ToVector(AshenFixSV1.TerminalStand)),
                ("the terminal to Slocombe and the teleporter (1498)", new[] { "the terminal to Central Operations", "Central Operations to Slocombe" }, none, ToVector(AshenFixS2V1.Stand)),
                ("Central Operations to the hall pad (1498)", new[] { "Central Operations to the hall pad" }, none, HallPadFoot),
                ("the service pad to the lab's charge (1498)", new[] { "the service pad to the lab's charge" }, LabPools, LabChargeSide)
            };
        }

        /// <summary>The 2D distance from <paramref name="point"/> to the stretch from <paramref name="a"/> to <paramref name="b"/>.</summary>
        private static float Flat(Vector3 a, Vector3 b, Vector3 point)
        {
            var (from, to, at) = (new Vector2(a.X, a.Z), new Vector2(b.X, b.Z), new Vector2(point.X, point.Z));
            var along = to - from;
            var t = along.LengthSquared() == 0 ? 0 : Math.Clamp(Vector2.Dot(at - from, along) / along.LengthSquared(), 0, 1);
            return Vector2.Distance(at, from + along * t);
        }

        private static List<Vector3> Line(IEnumerable<string> legs) =>
            legs.SelectMany((leg, index) => index == 0 ? Legs[leg] : Legs[leg].Skip(1)).ToList();

        /// <summary>The set's objects with collision in the walks: class, place, yaw (the field apart: it is down for the walks past it).</summary>
        private static IEnumerable<(int Class, Vector3 At, float Yaw)> Objects()
        {
            yield return ((int)Raid.ChargeClass, ToVector(Raid.ChargeAt), 0);
            yield return ((int)Raid.TerminalClass, ToVector(AshenFixSV1.TerminalAt), (float)Raid.TerminalOrientation);
            yield return ((int)Blast.TeleporterClass, ToVector(AshenFixS2V1.TeleporterAt), 0);
            yield return ((int)Blast.ChargeClass, V(AshenFixMV1.LabChargeAt), 0);
            foreach (var pad in N.Pads)
                yield return ((int)N.PadClass, V(AshenFixS2V1.Moved(pad.Id, pad.At)), 0);
        }

        /// <summary>An object's collision mesh put into the map's geometry at its place (as MapGeometry.Load places the map's own).</summary>
        internal static void AddObject(MapGeometry geometry, MeshLibrary meshes, int classId, Vector3 at, float yaw)
        {
            var mesh = meshes.GetForClass(classId);
            if (mesh == null || !mesh.HasCollision)
                return;
            var turn = Quaternion.CreateFromAxisAngle(Vector3.UnitY, yaw);
            var first = geometry.Vertices.Count / 3;
            foreach (var point in mesh.Vertices.Select(point => Vector3.Transform(point, turn) + at))
            {
                geometry.Vertices.Add(point.X);
                geometry.Vertices.Add(point.Y);
                geometry.Vertices.Add(point.Z);
            }
            for (var k = 0; k + 2 < mesh.Triangles.Count; k += 3)
                for (var corner = 0; corner < 3; corner++)
                    geometry.Triangles.Add(first + mesh.Triangles[k + corner]);
        }

        /// <summary>1498 offered on the client's arrival and taken by radio.</summary>
        private static void TakeBlast(WildernessRuntimeTestHarness harness, Client client)
        {
            harness.Manager.OfferArrivalMissions(client);
            Assert.AreEqual(1, MissionTestContext.Drain(client).OfType<DispenseRadioMissionPacket>().Count(packet => packet.MissionId == Blast.MissionId), "1498 on arrival");
            Assert.IsTrue(harness.Manager.TryAcceptRadioMission(client, Blast.MissionId), "taken by radio");
            MissionTestContext.Drain(client);
        }

        /// <summary>A Bane of the pool of these kinds killed by the player (the mates beside the body); its body.</summary>
        private static LootDispenser KillOne(WildernessRuntimeTestHarness harness, MapChannel copy, uint pool, uint[] kinds, params Facility.SquadMate[] beside)
        {
            Creature Standing() => Here(copy).FirstOrDefault(one => one.SpawnPool?.DbId == pool && kinds.Contains(one.DbId) && one.Npc == null && one.State != CharacterState.Dead);
            Facility.Stand(copy, pool);
            for (var round = 0; round < 5 && Standing() == null; round++)
            {
                // None of these kinds left standing: the pool's others down too, then the clock runs until the pool comes back on
                // its own (fix L, to-fix row 47: no reset of its cleared mark, no forced respawn; a pool left to the squad copy's
                // 45 min fails here).
                foreach (var other in Here(copy).Where(one => one.SpawnPool?.DbId == pool && one.State != CharacterState.Dead).ToArray())
                    Kill(harness, copy, other);
                var all = copy.SpawnPools.ToArray();
                try
                {
                    copy.SpawnPools.Clear();
                    copy.SpawnPools.AddRange(all.Where(entry => entry.DbId == pool));
                    for (var second = 0; second <= 130 && !Here(copy).Any(one => one.SpawnPool?.DbId == pool && one.State != CharacterState.Dead); second++)
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
            var creature = Standing();
            Assert.IsNotNull(creature, $"a {string.Join("/", kinds)} of pool {pool}");
            harness.MoveTo(creature.Position);
            harness.Drain();
            foreach (var mate in beside)
                mate.Beside(creature.Position + new Vector3(0, 0, 2));
            creature.Attributes[Attributes.Health].Current = 0;
            harness.Creatures.HandleCreatureKill(copy, creature, harness.Client.Player);
            Assert.IsTrue(copy.LootDispensers.TryGetValue(creature.CorpseLootEntityId, out var loot), "a corpse");
            harness.Drain();
            return loot;
        }

        /// <summary>The player's kills of these kinds until the objective is done (each drop picked up).</summary>
        private static void Gather(WildernessRuntimeTestHarness harness, MapChannel copy, uint pool, uint[] kinds, uint template, Func<MissionObjectiveState> objective)
        {
            for (var kill = 0; kill < 20 && objective() != MissionObjectiveState.Completed; kill++)
            {
                var held = Facility.Held(harness, template);
                var body = KillOne(harness, copy, pool, kinds);
                Assert.IsTrue(Holds(body, template), $"kill {kill + 1}: a {template}");
                PickUp(harness, body);
                harness.Drain();
                Tick(harness, copy, 250);
                Assert.AreEqual(held + 1, Facility.Held(harness, template), $"kill {kill + 1}: held");
            }
            Assert.AreEqual(MissionObjectiveState.Completed, objective(), $"{template}: five");
        }

        /// <summary>The HQ tunnel's walk through the field's place (the charge to the hall's mouth).</summary>
        private static readonly Vector3[] Tunnel = { new(6.5f, 138.61f, -412f), new(5f, 138.61f, -390f) };

        /// <summary>The front fields in the copy shown to this client (in the world, class 9572).</summary>
        private static List<DynamicObject> Fields(MapChannel copy, Client client) =>
            copy.DynamicObjects.Where(obj => obj.IsInWorld && (uint)obj.EntityClassId == N.FieldClass && DynamicObjectManager.IsShownTo(client, obj)).ToList();

        /// <summary>The client's own front field stands: one, his, intact, at its place, not usable; shown to him alone.</summary>
        private static void AssertFieldUp(WildernessRuntimeTestHarness harness, MapChannel copy, Client client, string when)
        {
            var field = MateObject(harness, copy, client, Raid.MissionId, Raid.FieldRole);
            Assert.IsNotNull(field, $"{when}: his field");
            Assert.AreEqual((N.FieldClass, (UseObjectState)Raid.FieldUp, false, client.Player.Id),
                ((uint)field.EntityClassId, field.StateId, field.IsEnabled, field.SceneOwnerCharacterId), $"{when}: his own, intact");
            Assert.IsLessThan(0.01f, Vector3.Distance(V(AshenFixMV1.FieldAt), field.Position), $"{when}: at its place");
            CollectionAssert.AreEqual(new[] { field }, Fields(copy, client), $"{when}: the one field he is shown");
        }

        /// <summary>The mate's 1498 objective states.</summary>
        private static void MateObjectives(Facility.SquadMate mate, out Func<uint, MissionObjectiveState> of) =>
            of = objective => mate.Client.Player.Missions[Blast.MissionId].Objectives[objective].State;

        /// <summary>The mate's own scene object of <paramref name="role"/> (of the mate's run).</summary>
        private static DynamicObject MateObject(WildernessRuntimeTestHarness harness, MapChannel copy, Client mate, uint mission, string role)
        {
            using var unit = harness.CreateChar();
            var runs = unit.CharacterMissions.Runtime.Scenes(mate.Player.Id, mission).Select(scene => scene.RunId).ToHashSet();
            return copy.DynamicObjects.SingleOrDefault(obj => obj.SceneMissionId == mission && obj.SceneActorRole == role &&
                obj.SceneOwnerCharacterId == mate.Player.Id && obj.IsInWorld && runs.Contains(obj.SceneRunId));
        }

        /// <summary>The mate's own Slocombe (of the mate's run).</summary>
        private static Creature MateSlocombe(WildernessRuntimeTestHarness harness, MapChannel copy, Client mate)
        {
            using var unit = harness.CreateChar();
            var runs = unit.CharacterMissions.Runtime.Scenes(mate.Player.Id, Blast.MissionId).Select(scene => scene.RunId).ToHashSet();
            return Here(copy).SingleOrDefault(creature => creature.SpawnPool?.SceneActorRole == Blast.SlocombeRole && runs.Contains(creature.SpawnPool.SceneRunId) &&
                creature.State != CharacterState.Dead);
        }

        /// <summary>The terminal's topics for this client (its conversation as the client opens it: CruciblePrometheusOutpostTests.OpenTerminal).</summary>
        private static uint[] TerminalTopics(WildernessRuntimeTestHarness harness, Client client, DynamicObject terminal)
        {
            client.SetWorldPosition(terminal.Position + new Vector3(0, 0, 1), 0);
            MissionTestContext.Drain(client);
            new NpcManager(harness, harness.Manager).RequestNpcConverse(client,
                new RequestNPCConversePacket { EntityId = terminal.EntityId, ActionId = ActionId.Converse, ActionArgId = 1 });
            var talk = MissionTestContext.Drain(client).OfType<ConversePacket>().LastOrDefault();
            return talk != null && talk.ConvoDataDict.TryGetValue(ConversationType.ObjectiveComplete, out var topics)
                ? ((List<CompleteableObjectives>)topics).Where(topic => topic.MissionId == Raid.MissionId).Select(topic => (uint)topic.ObjectiveId).ToArray()
                : Array.Empty<uint>();
        }

        /// <summary>The terminal's objective done for this client (CruciblePrometheusOutpostTests.TalkTo).</summary>
        private static void TalkToTerminal(WildernessRuntimeTestHarness harness, Client client, DynamicObject terminal, uint mission, uint objective)
        {
            client.SetWorldPosition(terminal.Position + new Vector3(0, 0, 1), 0);
            MissionTestContext.Drain(client);
            new NpcManager(harness, harness.Manager).CompleteNPCObjective(client, new CompleteNPCObjectivePacket
            {
                EntityId = terminal.EntityId, MissionId = mission, ObjectiveId = objective, PlayerFlagId = 1
            });
            Assert.AreEqual(1, MissionTestContext.Drain(client).OfType<ObjectiveCompletedPacket>().Count(packet => packet.MissionId == mission && packet.ObjectiveId == objective),
                $"{mission}.{objective} ObjectiveCompleted once");
        }

        /// <summary>A take that fails: the item gone from the bag in memory only (AshenGothaPlantTests.Lose).</summary>
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

        /// <summary>The set's pad rows on the copy, as InitTeleporters puts them on a loaded map (AshenAvernusHiveTests.LoadPads), and the links.</summary>
        private static void LoadPads(WildernessRuntimeTestHarness harness, MapChannel copy)
        {
            foreach (var row in harness.World.TeleporterEntries.AsNoTracking().Where(row => Pads.Contains(row.Id)).ToList())
            {
                if (copy.Teleporters.TryGetValue(row.Id, out var standing))
                {
                    standing.TriggeredByPlayers.Clear();
                    harness.Objects.Teleporters.TryAdd(row.Id, standing);
                    continue;
                }
                var loaded = new DynamicObject
                {
                    Position = new Vector3((float)row.PosX, (float)row.PosY, (float)row.PosZ),
                    Rotation = row.Rotation,
                    MapContextId = row.MapContextId,
                    RuntimeMapChannel = copy,
                    EntityClassId = (EntityClasses)row.ClassId,
                    Comment = row.Description,
                    ObjectData = new WaypointInfo(row.Id, false, WaypointType.LocalTeleporter),
                    DynamicObjectType = DynamicObjectType.LocalTeleporter
                };
                copy.Teleporters.TryAdd(row.Id, loaded);
                harness.Objects.Teleporters.TryAdd(row.Id, loaded);
            }
            harness.Objects.InitLocalTeleporters();
            harness.Objects.DynamicObjectWorker(copy, 0);
        }

        /// <summary>Through the pad onto the far one (AshenAvernusHiveTests.Teleport).</summary>
        private static void Teleport(WildernessRuntimeTestHarness harness, MapChannel map, DynamicObject pad)
        {
            harness.MoveTo(pad.Position + new Vector3(4, 0, 0));
            harness.Drain();
            harness.Objects.DynamicObjectProximityWorker(map, pad, 0);
            harness.MoveTo(pad.Position);
            harness.Drain();
            harness.Objects.DynamicObjectProximityWorker(map, pad, 0);
            Assert.IsNotNull(harness.Client.PendingTransfer, "taken by the pad");
            System.Threading.Thread.Sleep(DynamicObjectManager.PassageHoldMs + 50);
            harness.Objects.PassagesWorker(map);
            harness.Objects.TeleportAcknowledge(harness.Client);
            harness.Drain();
            Assert.IsNull(harness.Client.PendingTransfer);
        }

        private const uint ConsumableFiller = 45060;       // Class VII Basic Med Pack: fills the consumables bag (as AshenWhiteOasisTests)

        private static Vector3 V((double X, double Y, double Z) at) => new((float)at.X, (float)at.Y, (float)at.Z);
    }
}
