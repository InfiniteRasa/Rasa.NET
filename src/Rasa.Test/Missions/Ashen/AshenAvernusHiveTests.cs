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
    using N = AshenAvernusHiveNpcsV1;
    using M = AshenFixMV1;
    using R = AshenFixRV1;
    using Outpost = AshenAvernusOutpostNpcsV1;
    using Atta = AshenAssessingTheAttaV1;
    using Queen = AshenGodSaveTheQueenV1;
    using Secret = AshenTheBigSecretV1;
    using Facility = AshenIndraFacilityTests;
    using F = MarshesClientFloorTests;
    using WO = AshenWhiteOasisTests;

    /// <summary>
    /// Ashen Desert set 18, Avernus Outpost (squad instance 2110), the hive, played through as a player plays it: Petrovich's
    /// Assessing The Atta (1536: Frogga's men go down the northern tunnel, Tananda at the Cave In, back to Petrovich), God Save the
    /// Queen (1537: the southern tunnel's Atta Teleporter to the Queen's Chamber, Vandeweghe's lead down, the Bane swept, his last
    /// talk) and The Big Secret (1581: the Food Storage's west teleporter to the Eloh Sanctuary, Zuriel, the turn-in at Yamamoto in
    /// Ashen Desert). With them the set's places and walks on the client's floor, the leads on the navmesh, and the two pad pairs.
    ///
    /// Why these tests: a lead that never starts or never ends, a sweep counted for a squad mate before his own sweep is open (he
    /// could then never finish: the coordinator's set 17 lesson, GroupCreditService), a sweep lost for good, a pad that does not
    /// take the player both ways, or a turn-in at the wrong NPC would each break the chain.
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class AshenAvernusHiveTests
    {
        private const uint Map = N.Outpost;

        /// <summary>The set's missions.</summary>
        internal static readonly uint[] Missions = { Atta.MissionId, Queen.MissionId, Secret.MissionId };

        /// <summary>The NPC pools set 18 adds (the Bane's pool on its own).</summary>
        internal static readonly uint[] Pools = N.Pooled;

        /// <summary>The pads set 18 adds.</summary>
        internal static readonly uint[] Pads = N.Pads.Select(pad => pad.Id).ToArray();

        /// <summary>The pads where they stand now: the queen pad 918 at fix M's place (AshenFixMV1), the others set 18's.</summary>
        internal static readonly (uint Id, string Description, (double X, double Y, double Z) At, uint To)[] LivePads =
            N.Pads.Select(pad => pad.Id == M.QueenPad ? pad with { At = M.QueenPadAt } : pad).ToArray();

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
            var tananda = N.TanandaAt;
            var top = Outpost.PetrovichTopAt;
            yield return (1536, Atta.FindingTananda, tananda.X, tananda.Z, Atta.TanandaRadius);
            yield return (1536, Atta.TanandasStory, tananda.X, tananda.Z, Atta.NpcMarkerRadius);
            yield return (1536, Atta.ReturnToPetrovich, top.X, top.Z, Atta.NpcMarkerRadius);
            var pad = N.TunnelPadAt;
            var colonel = M.VandewegheAt;
            var pool = R.BaneAt;
            yield return (1537, Queen.FindTheTeleporter, pad.X, pad.Z, Queen.PadRadius);
            yield return (1537, Queen.TeleportToTheChamber, colonel.X, colonel.Z, Queen.ColonelRadius);
            yield return (1537, Queen.Colonel, colonel.X, colonel.Z, Queen.NpcMarkerRadius);
            yield return (1537, Queen.FollowVandeweghe, Queen.BottomAt.X, Queen.BottomAt.Z, Queen.BottomRadius);
            yield return (1537, Queen.TalkToVandeweghe, Queen.BottomAt.X, Queen.BottomAt.Z, Queen.NpcMarkerRadius);
            yield return (1537, Queen.SweepTheRoom, pool.X, pool.Z, N.BaneRadius + Queen.PoolMarkerPadding);
            yield return (1537, Queen.TalkAgain, Queen.BottomAt.X, Queen.BottomAt.Z, Queen.NpcMarkerRadius);
            yield return (1581, Secret.FinalChamber, Secret.ChamberAt.X, Secret.ChamberAt.Z, Secret.ChamberRadius);
            yield return (1581, Secret.SpeakWithZuriel, N.ZurielAt.X, N.ZurielAt.Z, Secret.NpcMarkerRadius);
        }

        /// <summary>
        /// The rows of the three missions: givers and receivers (Petrovich's second row gives all three and takes 1536 and 1537 in;
        /// Yamamoto in Ashen Desert takes 1581 in), levels, the Ashen category, the client's texts and talks (1536,2 Tananda 1914,
        /// 1536,3 Petrovich 1803; 1537,3, 5, 7 Vandeweghe 1925; 1581,2 Zuriel 2075), the chain's requirements (1535 → 1536 → 1537
        /// → 1581); every marker on Avernus Outpost, unnamed (the client names none of the set's); each mission operational.
        /// </summary>
        [TestMethod]
        public void TheSetsRowsAreTheClients()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            const string revision = AshenMissionDataV1.Revision;
            var expected = new (uint Mission, uint? Giver, uint? Receiver, uint Level, uint Name, uint Requires, (uint, uint?)[] Talks)[]
            {
                (Atta.MissionId, Outpost.PetrovichTop, Outpost.PetrovichTop, 43, 13804, AshenPetrovichsRunV1.MissionId,
                    new (uint, uint?)[] { (2, 1914), (3, 1803) }),
                (Queen.MissionId, Outpost.PetrovichTop, Outpost.PetrovichTop, 43, 13820, Atta.MissionId,
                    new (uint, uint?)[] { (3, 1925), (5, 1925), (7, 1925) }),
                (Secret.MissionId, Outpost.PetrovichTop, AshenLeadInNpcsV1.Yamamoto, 44, 14557, Queen.MissionId,
                    new (uint, uint?)[] { (2, 2075) })
            };
            var catalog = new MissionContentCatalog(harness, new Dictionary<uint, Mission>(), null);
            catalog.Load();
            foreach (var (mission, giver, receiver, level, name, requires, talks) in expected)
            {
                var definition = world.MissionContentDefinitionEntries.AsNoTracking().Single(entry => entry.MissionId == mission && entry.ContentRevision == revision);
                Assert.AreEqual((giver, receiver, (uint?)level, (uint?)AshenMissionDataV1.CategoryId, name, true, false),
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
            Assert.HasCount(3 + 7 + 2, markers, "the set's markers");
            foreach (var (mission, objective, indicator, map) in markers)
            {
                Assert.AreEqual(Map, map, $"{mission}.{objective}: on Avernus Outpost");
                Assert.IsGreaterThan(MissionIndicator.UnnamedFrom, indicator, $"{mission}.{objective}: unnamed");
                Assert.IsNull(AshenMissionTests.NamedMarker(mission, objective), $"{mission}.{objective}: no client name");
            }
        }

        /// <summary>
        /// The set's creatures in the id block 790650-790658: Tananda, Frogga, Vandeweghe and Zuriel friendly with the client's names,
        /// levels and packages (9728/1914, 9731/790651, 9733/1925, 9676/2075), each but Vandeweghe (1537's copy only: no public pool)
        /// alone in his pool on Avernus Outpost; the Bane's four (the Caretaker, Fithik, Miasma and Thrax rows' class, faction,
        /// stats and actions; level 43; 9037 "Mutated Miasma" and 9892 "Thrax Tunneler" by name) one each in the Bane's pool, 60 m+ outside every hostile pool's circle and 100 m+
        /// outside every warcry pool's. The pads: four local teleporters (type 1, class 25302) on Avernus Outpost linked in pairs
        /// both ways and to nothing else. The places' clearances as Places gives them (the rule breaks: docs/ashen-playtest.md).
        /// </summary>
        [TestMethod]
        public void TheSetsCreaturesAndPadsStandApart()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            var rows = world.CreatureEntries.AsNoTracking().Where(row => row.Id >= 790650 && row.Id <= 790658).ToList();
            CollectionAssert.AreEquivalent(N.Creatures, rows.Select(row => row.Id).ToArray(), "the eight");
            foreach (var (id, _, _, name, level, package, _) in N.Friends)
            {
                var row = rows.Single(entry => entry.Id == id);
                Assert.AreEqual((1U, name, level), ((uint)row.Faction, row.NameId, row.Level), $"{id}: friendly, the client's name");
                Assert.AreEqual(package, world.NpcPackageEntries.AsNoTracking().Single(entry => entry.Id == id).PackageId, $"{id}'s package");
            }
            var criers = AshenFixFTests.WarcryCreatures(harness);
            foreach (var (id, _, copy, name) in N.Bane)
            {
                var row = rows.Single(entry => entry.Id == id);
                var source = world.CreatureEntries.AsNoTracking().Single(entry => entry.Id == copy);
                Assert.AreEqual((source.ClassId, source.Faction, N.BaneLevel, name ?? source.NameId, source.MaxHitPoints, source.Action1, source.Action2, source.Action3),
                    (row.ClassId, row.Faction, row.Level, row.NameId, row.MaxHitPoints, row.Action1, row.Action2, row.Action3), $"{id}: {copy}'s row");
                Assert.IsFalse(world.NpcPackageEntries.AsNoTracking().Any(entry => entry.Id == id), $"{id}: no package");
            }

            var all = world.SpawnPoolEntries.AsNoTracking().Where(pool => pool.Id >= 790650 && pool.Id <= 790658).ToList();
            CollectionAssert.AreEquivalent(Pools.Append(N.BanePool).ToArray(), all.Select(pool => pool.Id).ToArray(), "the set's pools (none for Vandeweghe)");
            foreach (var pool in all.Where(pool => Pools.Contains(pool.Id)))
            {
                Assert.AreEqual((Map, pool.Id, (byte)1, (byte)1, 0.0), (pool.MapContextId, pool.Creature1Id, pool.Creature1MinCount, pool.Creature1MaxCount, pool.Radius),
                    $"{pool.Id}: alone, on Avernus Outpost");
                Assert.IsLessThan(0.01f, Vector3.Distance(V(N.At(pool.Id)), new((float)pool.PosX, (float)pool.PosY, (float)pool.PosZ)), $"{pool.Id}'s place");
            }
            var hostile = HostilePools(harness);
            var bane = all.Single(pool => pool.Id == N.BanePool);
            Assert.AreEqual((Map, N.Caretaker, N.Fithik, N.Miasma, N.Tunneler, N.BaneRadius, N.BaneRespawnTenths),
                (bane.MapContextId, bane.Creature1Id, bane.Creature2Id, bane.Creature3Id, bane.Creature4Id, (uint)bane.Radius, (uint)bane.RespawnTime), "the Bane's pool");
            Assert.AreEqual(((byte)1, (byte)1, (byte)1, (byte)1, 0U, 0U), (bane.Creature1MaxCount, bane.Creature2MaxCount, bane.Creature3MaxCount, bane.Creature4MaxCount, bane.Creature5Id, bane.Creature6Id), "one each");
            Assert.IsTrue(hostile.Any(pool => pool.Id == N.BanePool), "the Bane's pool hostile");
            // Fix R: the Bane's pool on the chamber's floor, 60 m+ outside every hostile pool's circle; 100 m+ outside every warcry
            // pool's but 531043's, the chamber's Atta, and 531044's, an island 29 m above (rule breaks, docs/ashen-playtest.md: no
            // place on the chamber's floor but the bottom keeps 100 m from 531043).
            var at = V(R.BaneAt);
            foreach (var other in hostile.Where(other => other.Id != N.BanePool))
            {
                var crier = new[] { other.Creature1Id, other.Creature2Id, other.Creature3Id, other.Creature4Id, other.Creature5Id, other.Creature6Id }.Any(criers.Contains)
                    && !WarcryBreaks.Contains(other.Id);
                Assert.IsGreaterThanOrEqualTo(crier ? 100f : 60f, Facility.Gap(at, other) - N.BaneRadius,
                    $"the Bane's pool {(crier ? 100 : 60)} m+ outside hostile pool {other.Id}'s circle");
            }
            foreach (var id in WarcryBreaks)
                Assert.IsLessThan(100f, Facility.Gap(at, hostile.Single(other => other.Id == id)) - N.BaneRadius, $"{id}'s break is not stale");

            var pads = world.TeleporterEntries.AsNoTracking().Where(row => Pads.Contains(row.Id)).OrderBy(row => row.Id).ToList();
            Assert.HasCount(4, pads);
            foreach (var (row, (id, _, place, _)) in pads.Zip(LivePads))
            {
                Assert.AreEqual((id, Map, N.PadClass, N.LocalTeleporter, 0.0), (row.Id, row.MapContextId, row.ClassId, (uint)row.Type, row.Rotation), $"{row.Id}");
                Assert.IsLessThan(0.01f, Vector3.Distance(V(place), new Vector3((float)row.PosX, (float)row.PosY, (float)row.PosZ)), $"{row.Id} place");
            }
            int Links(string where) => world.Database.SqlQueryRaw<int>($"SELECT COUNT(*) AS Value FROM local_teleporter_link WHERE {where}").AsEnumerable().Single();
            foreach (var (id, _, _, to) in N.Pads)
                Assert.AreEqual(1, Links($"from_teleporter_id = {id} AND to_teleporter_id = {to}"), $"{id} to {to}");
            var ids = string.Join(", ", Pads);
            Assert.AreEqual(4, Links($"from_teleporter_id IN ({ids}) OR to_teleporter_id IN ({ids})"), "no other link");
            Assert.IsFalse(world.TeleporterEntries.AsNoTracking().Where(row => row.MapContextId == Map && !Pads.Contains(row.Id)).AsEnumerable().Any(row =>
                (uint)row.Type == 1 && LivePads.Any(pad => Vector2.Distance(new((float)row.PosX, (float)row.PosZ), new((float)pad.At.X, (float)pad.At.Z)) < 10)), "no other pad by them");

            var failures = new List<string>();
            foreach (var (label, place, _, _, clearance) in Places())
                foreach (var pool in hostile.Where(pool => pool.Id != N.BanePool))
                    if (Facility.Gap(place, pool) < clearance)
                        failures.Add(FormattableString.Invariant($"{label}: {Facility.Gap(place, pool):0.0} m outside hostile pool {pool.Id}'s circle (its rule {clearance})"));
            Assert.IsEmpty(failures, string.Join("; ", failures));
        }

        /// <summary>
        /// In the squad's copy, entered by its door from Ashen Desert: Tananda, Frogga, Vandeweghe and Zuriel come out friendly, each
        /// on the navmesh's ground, and the player can talk to them; the Bane's pool gives its four, hostile, on the ground.
        /// </summary>
        [TestMethod]
        public void TheSetsCreaturesStandInTheSquadsCopy()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var copy = IntoHive(harness);
            Spawn(copy, Pools);
            foreach (var id in Pools)
            {
                var npc = Here(copy).Single(one => one.SpawnPool?.DbId == id);
                Assert.AreEqual((id, TargetCategory.Friendly), (npc.DbId, npc.TargetCategory), $"{id}");
                Assert.IsNotNull(npc.Npc, $"{id} talks");
                AssertOnGround(copy, npc.Position, $"{id}");
            }
            ResetBane(harness, copy);
            Spawn(copy, N.BanePool);
            var group = Here(copy).Where(one => one.SpawnPool?.DbId == N.BanePool).ToList();
            CollectionAssert.AreEquivalent(new[] { N.Caretaker, N.Fithik, N.Miasma, N.Tunneler }, group.Select(one => one.DbId).ToArray(), "one of each");
            foreach (var one in group)
            {
                Assert.AreEqual(TargetCategory.Hostile, one.TargetCategory, $"{one.DbId}");
                AssertOnGround(copy, one.Position, $"{one.DbId}");
            }
        }

        /// <summary>
        /// The two pad pairs: a player who steps onto the southern tunnel's pad is set down over the Queen's Chamber and not sent
        /// straight back; off it and on again, he is taken back; the same for the Food Storage's west pad and the Eloh Sanctuary's.
        /// Each arrival stands on its pad's trigger and is not sent straight back.
        /// </summary>
        [TestMethod]
        public void TheAttaTeleportersTakeThePlayerThereAndBack()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var copy = IntoHive(harness);
            LoadPads(harness, copy);
            foreach (var (from, to, beyond) in new[] { (N.TunnelPad, N.QueenPad, V(M.VandewegheAt)), (N.WestPad, N.SanctuaryPad, V(N.ZurielAt)) })
            {
                var there = copy.Teleporters[from];
                var back = copy.Teleporters[to];
                Teleport(harness, copy, there);
                Assert.IsLessThan(3f, Vector3.Distance(back.Position, harness.Client.Player.Position), $"{from}: at {to}");
                harness.Objects.DynamicObjectProximityWorker(copy, back, 0);
                Assert.IsNull(harness.Client.PendingTransfer, $"{to}: not sent straight back");
                harness.MoveTo(beyond);
                harness.Drain();
                harness.Objects.DynamicObjectProximityWorker(copy, back, 0);
                Assert.IsNull(harness.Client.PendingTransfer, "off the pad");
                Teleport(harness, copy, back);
                Assert.IsLessThan(3f, Vector3.Distance(there.Position, harness.Client.Player.Position), $"{to}: back at {from}");
                harness.Objects.DynamicObjectProximityWorker(copy, there, 0);
                Assert.IsNull(harness.Client.PendingTransfer, $"{from}: the arrival on its trigger, not sent straight back");
            }
        }

        /// <summary>
        /// The set's places on the client's floor: each on the navmesh's ground (within 1 m) and a client floor (within 1 m), with no
        /// client surface over its head within 2.2 m; the NPCs seen from all 16 places 2 m round them (line of sight: to-fix row 26);
        /// each outside every hostile pool's circle by its clearance (Places; a pool on another floor, 12 m and more above or below,
        /// in 3D). The markers on the navmesh and a client floor within 2 m.
        /// </summary>
        [TestMethod]
        public void ThePlacesStandOnTheClientsFloor()
        {
            var dataDir = F.ClientData();
            using var harness = WildernessRuntimeTestHarness.Create();
            var copy = IntoHive(harness);
            var nav = copy.NavMesh;
            using var meshes = new MeshLibrary(dataDir, F.MeshCsv());
            var geometry = MapGeometry.Load(Path.Combine(dataDir, "maps", Facility.MapName(harness, Map)), meshes, 1);
            var hostile = HostilePools(harness).Where(pool => pool.Id != N.BanePool).ToList();
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
                .Select(marker => (Label: $"{marker.MissionId}.{marker.ObjectiveId}/{marker.IndicatorId}", At: new Vector3((float)marker.PosX, (float)marker.PosY, (float)marker.PosZ)))
                .ToList();
            Assert.HasCount(12, markers, "the set's markers");
            var (markerFloors, _) = F.Surfaces(geometry, markers.Select(marker => (marker.At + new Vector3(0, 2 - F.MaxUnder, 0), F.Walkable)).ToList());
            for (var i = 0; i < markers.Count; i++)
            {
                if (nav.GroundHeight(markers[i].At + new Vector3(0, 2, 0)) is not { } y || MathF.Abs(y - markers[i].At.Y) > 2f)
                    failures.Add($"{markers[i].Label}: navmesh {nav.GroundHeight(markers[i].At + new Vector3(0, 2, 0))}");
                if (markers[i].At.Y - markerFloors[i] > 2f)
                    failures.Add($"{markers[i].Label}: client floor {markerFloors[i]}");
            }
            Assert.HasCount(15, places);
            Assert.IsEmpty(failures, $"{failures.Count}: " + string.Join("; ", failures));
        }

        /// <summary>
        /// The walks on the client's floor (AshenFixBTests.ClientFloorLine with a player's half width, 0.5 m, fix G's 55-degree climb
        /// rule; a step or hump climbed along the walk's line), every leg there and back by the corners Legs names (a client-floor
        /// flood's way, 2026-11-12), also with its start moved 0.13, 0.27 and 0.4 m along its first stretch (StartShifts). Every
        /// walk keeps 30 m (2D) outside the circle of every hostile pool but those it names (the upstream warcry pools on the only
        /// way; docs/ashen-playtest.md); the walk to the Bane ends at their pool.
        /// </summary>
        [TestMethod]
        public void ThePlacesAreWalkedToOnTheClientsFloor()
        {
            var dataDir = F.ClientData();
            using var harness = WildernessRuntimeTestHarness.Create();
            using var meshes = new MeshLibrary(dataDir, F.MeshCsv());
            var geometry = MapGeometry.Load(Path.Combine(dataDir, "maps", Facility.MapName(harness, Map)), meshes, 1);
            var onPads = AshenFixMTests.AddPads(harness, geometry, meshes);
            var hostile = HostilePools(harness).Where(pool => pool.Id != N.BanePool).ToList();
            var failures = new List<string>();
            var walked = 0;
            var expected = 0;
            foreach (var (leg, corners0) in Legs)
            {
                var corners = onPads(corners0);
                // Each leg, there and back, as given and with its start moved StartShifts along its first stretch: the samples
                // (every 0.5 m) fall elsewhere, so a step or a wall the samples happen to straddle is not missed (set 17's audit
                // follow-up). The way back starts at the last corner, moved along the last stretch.
                foreach (var (line0, direction) in new[] { (corners.ToList(), "there"), (Enumerable.Reverse(corners).ToList(), "back") })
                {
                    var first = Vector2.Distance(new(line0[0].X, line0[0].Z), new(line0[1].X, line0[1].Z));
                    foreach (var shift in StartShifts.Where(shift => shift == 0 || first > shift + 0.05f))
                    {
                        expected++;
                        walked++;
                        var line = line0.ToList();
                        line[0] = Vector3.Lerp(line0[0], line0[1], shift / first);
                        if (AshenFixBTests.ClientFloorLine(geometry, line, line[^1], null, PlayerHalfWidth, rampFoot: true) is { } why)
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
            Assert.AreEqual(expected, walked, "every leg both ways, at each start shift");
            Assert.IsGreaterThanOrEqualTo(2 * Legs.Count * 3, walked, "most legs at three shifts or more");
            Assert.IsEmpty(failures, $"{failures.Count}: " + string.Join("; ", failures));
        }

        /// <summary>
        /// The pads' body (prop_atta_teleporter_v01.geo, class 25302: a platform 1.1-1.22 m up, its only way up a ramp on its south
        /// side, local -z, about 5.6 m long; the probe, 2026-11-12): each of 917-920 stands at rotation 0, and every walk to or from
        /// one goes up or down that ramp along its length: the walk's corner next to the pad within 0.75 m of its x (the ramp's
        /// 1.25 m half width less a player's half width) and 5.5-7.5 m south of it, past the ramp's foot.
        /// </summary>
        [TestMethod]
        public void ThePadsAreWalkedUpTheirRamps()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var rows = harness.World.TeleporterEntries.AsNoTracking().Where(row => Pads.Contains(row.Id)).ToList();
            Assert.HasCount(4, rows);
            foreach (var row in rows)
                Assert.AreEqual((row.Id, 0f), (row.Id, (float)row.Rotation), $"{row.Id}: rotation 0, the ramp to the south");
            var failures = new List<string>();
            var reached = new HashSet<uint>();
            foreach (var (label, legs, _, _) in Walks())
            {
                var line = Line(legs);
                foreach (var (end, next) in new[] { (line[0], line[1]), (line[^1], line[^2]) })
                    foreach (var (id, _, at, _) in LivePads.Where(pad => Vector3.Distance(V(pad.At), end) < 0.01f))
                    {
                        reached.Add(id);
                        var pad = V(at);
                        if (MathF.Abs(next.X - pad.X) > 0.75f || pad.Z - next.Z is < 5.5f or > 7.5f)
                            failures.Add(FormattableString.Invariant($"{label}: pad {id}'s neighbour corner {next} is not on its ramp's line"));
                    }
            }
            CollectionAssert.AreEquivalent(Pads, reached.ToArray(), "every pad walked to or from");
            Assert.IsEmpty(failures, string.Join("; ", failures));
        }

        /// <summary>
        /// The leads as the scene's route controller starts them (SceneRouteController.Start): Frogga's two men from their places by
        /// him down the northern tunnel, Vandeweghe from his place down to the Queen's Chamber; each waypoint on the navmesh by a
        /// complete path from the one before, its end within 0.5 m of the waypoint and its height within 0.5 m.
        /// </summary>
        [TestMethod]
        public void TheLeadsAreOnTheNavmesh()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var copy = IntoHive(harness);
            var nav = copy.NavMesh;
            var failures = new List<string>();
            foreach (var (name, from, points) in new[]
                     {
                         ("the first man", ToVector(Atta.MenAt[0]), Atta.TunnelWay), ("the second man", ToVector(Atta.MenAt[1]), Atta.TunnelWay[..^1]),
                         ("Vandeweghe", V(M.VandewegheAt), M.DownWay)
                     })
            {
                var previous = nav.Nearest(from) ?? from;
                foreach (var point in points.Select(point => ToVector(point)))
                {
                    var path = nav.FindPath(previous, point, out var complete);
                    if (!complete || path == null || path.Count == 0 || Vector3.Distance(path[^1], point) > 0.5f || MathF.Abs(path[^1].Y - point.Y) >= 0.5f)
                        failures.Add(FormattableString.Invariant($"{name}: no grounded path to {point} (ends {(path is { Count: > 0 } ? path[^1] : Vector3.Zero)})"));
                    previous = point;
                }
            }
            foreach (var at in Atta.MenAt.Select(point => ToVector(point)))
                if (nav.GroundHeight(at + new Vector3(0, 1, 0)) is not { } ground || MathF.Abs(ground - at.Y) > 1f)
                    failures.Add($"a man's place {at}: navmesh {nav.GroundHeight(at + new Vector3(0, 1, 0))}");
            Assert.IsEmpty(failures, string.Join("; ", failures));
        }

        /// <summary>
        /// Assessing The Atta: not offered before Petrovich's Run is done; then Petrovich at the top offers it with its four pieces;
        /// at the accept Frogga's two men (the player's own, unhurt by anything) stand by Frogga; 1 open, 2 and 3 wait. Two seconds
        /// on they go down the northern tunnel and stop at the Cave In. Tananda hears nothing before 1; the walk to her completes 1,
        /// her talk 2, Petrovich's talk 3; the turn-in at Petrovich waits with a full bag, pays the piece chosen once; the men go
        /// with the mission; Petrovich then offers God Save the Queen.
        /// </summary>
        [TestMethod]
        public void AssessingTheAttaFollowsFroggasMenToTananda()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var copy = IntoHive(harness);
            var top = Giver(copy, Outpost.PetrovichTop);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, top, Atta.MissionId), "not before 1535");
            AshenRuntime.SeedCompleted(harness, AshenPetrovichsRunV1.MissionId);
            top = Giver(copy, Outpost.PetrovichTop);
            var offer = WildernessRanjaGorgeTests.Offer(harness, top, Atta.MissionId);
            CollectionAssert.AreEqual(new[] { Atta.MechLegs, Atta.HazmatLegs, Atta.ReflectiveVest, Atta.MotorAssistVest },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Accept(harness, top, Atta.MissionId);
            Tick(harness, copy, 250);
            Assert.AreEqual((MissionObjectiveState.Incomplete, MissionObjectiveState.Inactive, MissionObjectiveState.Inactive),
                (Objective(harness, Atta.MissionId, Atta.FindingTananda), Objective(harness, Atta.MissionId, Atta.TanandasStory),
                    Objective(harness, Atta.MissionId, Atta.ReturnToPetrovich)), "1 open, 2 and 3 wait");
            for (var index = 0; index < Atta.MenAt.Length; index++)
            {
                var man = SceneCreature(harness, copy, Atta.MissionId, Atta.ManRole(index));
                Assert.IsNotNull(man, $"man {index + 1} stands");
                Assert.AreEqual((Outpost.Soldier, TargetCategory.Friendly), (man.DbId, man.TargetCategory), $"man {index + 1}");
                Assert.IsTrue(Rasa.Game.Missions.World.CreatureGameplayRules.IsInvulnerable(man), $"man {index + 1}: unhurt by anything");
                Assert.IsLessThan(1.5f, Vector3.Distance(ToVector(Atta.MenAt[index]), man.Position), $"man {index + 1} by Frogga");
            }

            // The men go down the tunnel to the Cave In.
            harness.MoveTo(V(Outpost.PetrovichTopAt) + new Vector3(0, 0, 3));
            var ends = new[] { ToVector(Atta.TunnelWay[^1]), ToVector(Atta.TunnelWay[^2]) };
            bool Arrived() => Enumerable.Range(0, ends.Length).All(index => Vector3.Distance(SceneCreature(harness, copy, Atta.MissionId, Atta.ManRole(index)).Position, ends[index]) <= 1.5f);
            for (var second = 0; second < 300 && !Arrived(); second++)
                Tick(harness, copy, 1000);
            Assert.IsTrue(Arrived(), "the men at the Cave In");

            var tananda = Npc(copy, N.Tananda);
            Assert.IsEmpty(Topics(harness, tananda, Atta.MissionId), "Tananda hears nothing before 1");
            Enter(harness, V(N.TanandaAt));
            Assert.AreEqual((MissionObjectiveState.Completed, MissionObjectiveState.Incomplete),
                (Objective(harness, Atta.MissionId, Atta.FindingTananda), Objective(harness, Atta.MissionId, Atta.TanandasStory)), "at Tananda: 1 done, 2 open");
            Talk(harness, tananda, Atta.MissionId, Atta.TanandasStory);
            Tick(harness, copy, 250);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Atta.MissionId, Atta.ReturnToPetrovich), "3 open");
            ReloadAndRecover(harness, copy);
            Assert.IsNotNull(SceneCreature(harness, copy, Atta.MissionId, Atta.ManRole(0)), "the men after a reload");
            top = Giver(copy, Outpost.PetrovichTop);
            Talk(harness, top, Atta.MissionId, Atta.ReturnToPetrovich);
            Tick(harness, copy, 250);
            Assert.IsTrue(harness.Client.Player.Missions[Atta.MissionId].Completeable, "to be turned in");

            FullBagWaits(harness, top, Atta.MissionId, 0, ArmorFiller);
            FailedTurnInPaysNothing(harness, top, Atta.MissionId, 0);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, top, Atta.MissionId, 0, Atta.Experience, Atta.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Atta.MissionId), "MissionRewarded once");
            Assert.AreEqual(1U, Facility.Held(harness, Atta.MechLegs), "the legs chosen");
            for (var tick = 0; tick < 4; tick++)
                Tick(harness, copy, 250);
            for (var index = 0; index < Atta.MenAt.Length; index++)
                Assert.IsNull(SceneCreature(harness, copy, Atta.MissionId, Atta.ManRole(index)), $"man {index + 1} goes with the mission");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, Giver(copy, Outpost.PetrovichTop), Atta.MissionId), "done once");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, Giver(copy, Outpost.PetrovichTop), Queen.MissionId), "Petrovich offers 1537");
        }

        /// <summary>
        /// God Save the Queen: Petrovich offers it after 1536 with its four stacks; at the accept Vandeweghe's copy (the player's own,
        /// unhurt by anything; there is no public one) stands at his place; 1 open, 2-7 wait. The walk onto the southern tunnel's pad
        /// completes 1; the pad's arrival is short of Vandeweghe, the walk to him completes 2; his talk opens 4 and he leads down the
        /// tunnel to the Queen's Chamber; the walk to the bottom completes 4; his talk there opens 6 (he stays there after a reload);
        /// the Bane's pool cleared completes 6; his last talk completes 7 and he goes; the turn-in at
        /// Petrovich waits with a full bag and pays the stack chosen once.
        /// </summary>
        [TestMethod]
        public void GodSaveTheQueenSweepsTheQueensChamber()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var copy = IntoHive(harness);
            LoadPads(harness, copy);
            ResetBane(harness, copy);
            var top = Giver(copy, Outpost.PetrovichTop);
            AshenRuntime.SeedCompleted(harness, AshenPetrovichsRunV1.MissionId);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, Giver(copy, Outpost.PetrovichTop), Queen.MissionId), "not before 1536");
            AshenRuntime.SeedCompleted(harness, Atta.MissionId);
            top = Giver(copy, Outpost.PetrovichTop);
            var offer = WildernessRanjaGorgeTests.Offer(harness, top, Queen.MissionId);
            CollectionAssert.AreEqual(new[] { Queen.IncendiaryGrenade, Queen.AdvancedMedPack, Queen.EmpGrenade, Queen.ResTraumaKit },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Accept(harness, top, Queen.MissionId);
            Tick(harness, copy, 250);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Queen.MissionId, Queen.FindTheTeleporter), "1 open");
            foreach (var objective in new[] { Queen.TeleportToTheChamber, Queen.Colonel, Queen.FollowVandeweghe, Queen.TalkToVandeweghe, Queen.SweepTheRoom, Queen.TalkAgain })
                Assert.AreEqual(MissionObjectiveState.Inactive, Objective(harness, Queen.MissionId, objective), $"{objective} waits");
            var colonel = SceneCreature(harness, copy, Queen.MissionId, Queen.ColonelRole);
            Assert.IsNotNull(colonel, "Vandeweghe's copy");
            Assert.AreEqual((N.Vandeweghe, N.VandeweghePackage), (colonel.DbId, colonel.Npc?.NpcPackageId), "a copy of his row");
            Assert.IsTrue(Rasa.Game.Missions.World.CreatureGameplayRules.IsInvulnerable(colonel), "unhurt by anything");
            Assert.IsTrue(MissionNpcVisibility.IsShownTo(harness.Client, colonel), "the copy is shown to the player");
            Assert.IsFalse(Here(copy).Any(one => one.SpawnPool?.DbId == N.Vandeweghe), "no public Vandeweghe");

            // 1, 2: the pad, the far side, Vandeweghe.
            Enter(harness, V(N.TunnelPadAt));
            Assert.AreEqual((MissionObjectiveState.Completed, MissionObjectiveState.Incomplete),
                (Objective(harness, Queen.MissionId, Queen.FindTheTeleporter), Objective(harness, Queen.MissionId, Queen.TeleportToTheChamber)), "at the pad: 1 done, 2 open");
            Teleport(harness, copy, copy.Teleporters[N.TunnelPad]);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Queen.MissionId, Queen.TeleportToTheChamber), "the pad's arrival is short of Vandeweghe");
            Enter(harness, V(M.VandewegheAt));
            Assert.AreEqual((MissionObjectiveState.Completed, MissionObjectiveState.Incomplete),
                (Objective(harness, Queen.MissionId, Queen.TeleportToTheChamber), Objective(harness, Queen.MissionId, Queen.Colonel)), "by Vandeweghe: 2 done, 3 open");

            // 3: the talk; the lead; 4: the bottom.
            colonel = SceneCreature(harness, copy, Queen.MissionId, Queen.ColonelRole);
            CollectionAssert.AreEqual(new[] { Queen.Colonel }, Topics(harness, colonel, Queen.MissionId), "Vandeweghe hears 3");
            Talk(harness, colonel, Queen.MissionId, Queen.Colonel);
            Tick(harness, copy, 250);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Queen.MissionId, Queen.FollowVandeweghe), "4 open");
            var bottom = ToVector(Queen.BottomAt);
            for (var second = 0; second < 300 && Vector3.Distance(SceneCreature(harness, copy, Queen.MissionId, Queen.ColonelRole).Position, bottom) > 1.5f; second++)
                Tick(harness, copy, 1000);
            Assert.IsLessThanOrEqualTo(1.5f, Vector3.Distance(SceneCreature(harness, copy, Queen.MissionId, Queen.ColonelRole).Position, bottom), "Vandeweghe at the bottom");
            Enter(harness, bottom);
            Assert.AreEqual((MissionObjectiveState.Completed, MissionObjectiveState.Incomplete),
                (Objective(harness, Queen.MissionId, Queen.FollowVandeweghe), Objective(harness, Queen.MissionId, Queen.TalkToVandeweghe)), "at the bottom: 4 done, 5 open");

            // 5: "Bane!"; 6 opens.
            Talk(harness, SceneCreature(harness, copy, Queen.MissionId, Queen.ColonelRole), Queen.MissionId, Queen.TalkToVandeweghe);
            Tick(harness, copy, 250);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Queen.MissionId, Queen.SweepTheRoom), "6 open");
            ReloadAndRecover(harness, copy);
            Assert.IsLessThanOrEqualTo(1.5f, Vector3.Distance(SceneCreature(harness, copy, Queen.MissionId, Queen.ColonelRole).Position, bottom), "still at the bottom after a reload");

            // 6: the sweep.
            KillPools(harness, copy, N.BanePool);
            Tick(harness, copy, 250);
            Assert.AreEqual((MissionObjectiveState.Completed, MissionObjectiveState.Incomplete),
                (Objective(harness, Queen.MissionId, Queen.SweepTheRoom), Objective(harness, Queen.MissionId, Queen.TalkAgain)), "the room swept: 6 done, 7 open");

            // 7: the last talk; he goes.
            Talk(harness, SceneCreature(harness, copy, Queen.MissionId, Queen.ColonelRole), Queen.MissionId, Queen.TalkAgain);
            Tick(harness, copy, 250);
            Assert.IsTrue(harness.Client.Player.Missions[Queen.MissionId].Completeable, "to be turned in");
            for (var tick = 0; tick < 4; tick++)
                Tick(harness, copy, 250);
            Assert.IsNull(SceneCreature(harness, copy, Queen.MissionId, Queen.ColonelRole), "Vandeweghe's copy goes");

            top = Giver(copy, Outpost.PetrovichTop);
            FullBagWaits(harness, top, Queen.MissionId, 1, ConsumableFiller);
            FailedTurnInPaysNothing(harness, top, Queen.MissionId, 1);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, top, Queen.MissionId, 1, Queen.Experience, Queen.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Queen.MissionId), "MissionRewarded once");
            Assert.AreEqual(Queen.AdvancedMedPacks, Facility.Held(harness, Queen.AdvancedMedPack), "the med packs chosen");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, Giver(copy, Outpost.PetrovichTop), Queen.MissionId), "done once");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, Giver(copy, Outpost.PetrovichTop), Secret.MissionId), "Petrovich offers 1581");
        }

        /// <summary>
        /// The sweep's two ways round: the Bane's pool cleared before 6 is open leaves 6 waiting, and counts when 6 opens while the pool
        /// is still down; a pool come back before 6 opens leaves 6 open until it is cleared again (a sweep is never lost for good).
        /// </summary>
        [TestMethod]
        public void GodSaveTheQueensSweepCountsAtItsOpeningOrAgain()
        {
            foreach (var back in new[] { false, true })
            {
                using var harness = WildernessRuntimeTestHarness.Create();
                var copy = IntoHive(harness);
                LoadPads(harness, copy);
                ResetBane(harness, copy);
                AshenRuntime.SeedCompleted(harness, AshenPetrovichsRunV1.MissionId, Atta.MissionId);
                Accept(harness, Giver(copy, Outpost.PetrovichTop), Queen.MissionId);
                Tick(harness, copy, 250);
                KillPools(harness, copy, N.BanePool);
                Tick(harness, copy, 250);
                Assert.AreEqual(MissionObjectiveState.Inactive, Objective(harness, Queen.MissionId, Queen.SweepTheRoom), $"{back}: 6 waits; the early sweep not counted yet");
                if (back)
                    ResetBane(harness, copy);
                ToTheSweep(harness, copy);
                Assert.AreEqual(back ? MissionObjectiveState.Incomplete : MissionObjectiveState.Completed, Objective(harness, Queen.MissionId, Queen.SweepTheRoom),
                    back ? "the pool back: 6 open" : "the pool still down: 6 done at its opening");
                if (back)
                {
                    KillPools(harness, copy, N.BanePool);
                    Tick(harness, copy, 250);
                    Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Queen.MissionId, Queen.SweepTheRoom), "cleared again: 6 done");
                }
                Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Queen.MissionId, Queen.TalkAgain), "7 open");
            }
        }

        /// <summary>
        /// God Save the Queen in a squad of two on it, the mate behind (the coordinator's set 17 lesson: GroupCreditService credits a
        /// squad signal to every member whose objective is Incomplete). The player walks to the pad, Vandeweghe and the bottom, talks
        /// and clears the Bane: his 6 done; the mate, still at 1, has his 2-7 waiting, none of them credited by the player's walk,
        /// talk or sweep. The mate then walks the same way and talks to his own Vandeweghe: at his 5's talk his 6 opens, and the pool
        /// still down counts for him; his own last talk makes his mission completeable.
        /// </summary>
        [TestMethod]
        public void GodSaveTheQueensSweepWaitsForAMateBehind()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var copy = IntoHive(harness);
            LoadPads(harness, copy);
            ResetBane(harness, copy);
            AshenRuntime.SeedCompleted(harness, AshenPetrovichsRunV1.MissionId, Atta.MissionId);
            var top = Giver(copy, Outpost.PetrovichTop);
            Accept(harness, top, Queen.MissionId);
            Tick(harness, copy, 250);
            using var mate = new Facility.SquadMate(harness, copy, top, Queen.MissionId, new[] { AshenPetrovichsRunV1.MissionId, Atta.MissionId });
            Assert.IsNotNull(MateCreature(harness, copy, mate, Queen.MissionId, Queen.ColonelRole), "the mate's own Vandeweghe");

            ToTheSweep(harness, copy);
            KillPools(harness, copy, N.BanePool);
            Tick(harness, copy, 250);
            Assert.AreEqual((MissionObjectiveState.Completed, MissionObjectiveState.Incomplete),
                (Objective(harness, Queen.MissionId, Queen.SweepTheRoom), Objective(harness, Queen.MissionId, Queen.TalkAgain)), "the player's 6 done, 7 open");
            Assert.AreEqual(MissionObjectiveState.Incomplete, mate.Of(Queen.FindTheTeleporter), "the mate's 1 still open");
            foreach (var objective in new[] { Queen.TeleportToTheChamber, Queen.Colonel, Queen.FollowVandeweghe, Queen.TalkToVandeweghe, Queen.SweepTheRoom, Queen.TalkAgain })
                Assert.AreEqual(MissionObjectiveState.Inactive, mate.Of(objective), $"the mate's {objective} waits: not credited by the player's");

            // The mate catches up.
            MateEnters(harness, mate, V(N.TunnelPadAt) + new Vector3(30, 0, 0), V(N.TunnelPadAt));
            MateEnters(harness, mate, V(M.QueenPadAt), V(M.VandewegheAt));
            Assert.AreEqual((MissionObjectiveState.Completed, MissionObjectiveState.Completed, MissionObjectiveState.Incomplete),
                (mate.Of(Queen.FindTheTeleporter), mate.Of(Queen.TeleportToTheChamber), mate.Of(Queen.Colonel)), "the mate's 1, 2 done, 3 open");
            MateTalks(harness, copy, mate, MateCreature(harness, copy, mate, Queen.MissionId, Queen.ColonelRole), Queen.MissionId, Queen.Colonel);
            MateEnters(harness, mate, V(M.VandewegheAt), ToVector(Queen.BottomAt));
            Assert.AreEqual(MissionObjectiveState.Incomplete, mate.Of(Queen.TalkToVandeweghe), "the mate's 5 open");
            Assert.AreEqual(MissionObjectiveState.Inactive, mate.Of(Queen.SweepTheRoom), "the mate's 6 still waits");
            MateTalks(harness, copy, mate, MateCreature(harness, copy, mate, Queen.MissionId, Queen.ColonelRole), Queen.MissionId, Queen.TalkToVandeweghe);
            Tick(harness, copy, 250);
            Assert.AreEqual((MissionObjectiveState.Completed, MissionObjectiveState.Incomplete), (mate.Of(Queen.SweepTheRoom), mate.Of(Queen.TalkAgain)),
                "the pool still down counts at the mate's 6's opening");
            MateTalks(harness, copy, mate, MateCreature(harness, copy, mate, Queen.MissionId, Queen.ColonelRole), Queen.MissionId, Queen.TalkAgain);
            Assert.IsTrue(mate.Client.Player.Missions[Queen.MissionId].Completeable, "the mate's to be turned in");
        }

        /// <summary>
        /// An abandon of God Save the Queen while Vandeweghe leads: his copy goes; taken again, the
        /// copy stands at his place again and 1 is open, 2-7 wait.
        /// </summary>
        [TestMethod]
        public void GodSaveTheQueenAbandonedStartsOver()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var copy = IntoHive(harness);
            LoadPads(harness, copy);
            AshenRuntime.SeedCompleted(harness, AshenPetrovichsRunV1.MissionId, Atta.MissionId);
            Accept(harness, Giver(copy, Outpost.PetrovichTop), Queen.MissionId);
            Tick(harness, copy, 250);
            Enter(harness, V(N.TunnelPadAt));
            Teleport(harness, copy, copy.Teleporters[N.TunnelPad]);
            Enter(harness, V(M.VandewegheAt));
            Talk(harness, SceneCreature(harness, copy, Queen.MissionId, Queen.ColonelRole), Queen.MissionId, Queen.Colonel);
            for (var tick = 0; tick < 8; tick++)
                Tick(harness, copy, 1000);
            Assert.IsGreaterThan(3f, Vector3.Distance(SceneCreature(harness, copy, Queen.MissionId, Queen.ColonelRole).Position, V(M.VandewegheAt)), "on his way");
            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Queen.MissionId));
            harness.Drain();
            for (var tick = 0; tick < 4; tick++)
                Tick(harness, copy, 250);
            Assert.IsNull(SceneCreature(harness, copy, Queen.MissionId, Queen.ColonelRole), "the copy goes with the abandon");
            Accept(harness, Giver(copy, Outpost.PetrovichTop), Queen.MissionId);
            Tick(harness, copy, 250);
            var colonel = SceneCreature(harness, copy, Queen.MissionId, Queen.ColonelRole);
            Assert.IsNotNull(colonel, "the copy again");
            Assert.IsLessThan(1.5f, Vector3.Distance(colonel.Position, V(M.VandewegheAt)), "at his place");
            Assert.AreEqual((MissionObjectiveState.Incomplete, MissionObjectiveState.Inactive, MissionObjectiveState.Inactive),
                (Objective(harness, Queen.MissionId, Queen.FindTheTeleporter), Objective(harness, Queen.MissionId, Queen.TeleportToTheChamber),
                    Objective(harness, Queen.MissionId, Queen.Colonel)), "1 open, 2 and 3 wait");
        }

        /// <summary>
        /// An abandon of God Save the Queen mid-sweep (6 open, one of the four Bane down): Vandeweghe's copy goes; taken again, his
        /// copy stands at his place, 1 is open and 2-7 wait; the Bane killed before do not count, the pool not cleared.
        /// </summary>
        [TestMethod]
        public void GodSaveTheQueenAbandonedMidSweepStartsOver()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var copy = IntoHive(harness);
            LoadPads(harness, copy);
            ResetBane(harness, copy);
            AshenRuntime.SeedCompleted(harness, AshenPetrovichsRunV1.MissionId, Atta.MissionId);
            Accept(harness, Giver(copy, Outpost.PetrovichTop), Queen.MissionId);
            Tick(harness, copy, 250);
            ToTheSweep(harness, copy);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Queen.MissionId, Queen.SweepTheRoom), "6 open");
            var bane = Here(copy).Where(creature => creature.SpawnPool?.DbId == N.BanePool && creature.State != CharacterState.Dead).ToList();
            Assert.HasCount(4, bane, "the four Bane stand");
            Kill(harness, copy, bane[0]);
            Tick(harness, copy, 250);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Queen.MissionId, Queen.SweepTheRoom), "one down: 6 still open");

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Queen.MissionId));
            harness.Drain();
            for (var tick = 0; tick < 4; tick++)
                Tick(harness, copy, 250);
            Assert.IsNull(SceneCreature(harness, copy, Queen.MissionId, Queen.ColonelRole), "the copy goes with the abandon");
            Accept(harness, Giver(copy, Outpost.PetrovichTop), Queen.MissionId);
            Tick(harness, copy, 250);
            var colonel = SceneCreature(harness, copy, Queen.MissionId, Queen.ColonelRole);
            Assert.IsNotNull(colonel, "the copy again");
            Assert.IsLessThan(1.5f, Vector3.Distance(colonel.Position, V(M.VandewegheAt)), "at his place");
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Queen.MissionId, Queen.FindTheTeleporter), "1 open");
            foreach (var objective in new[] { Queen.TeleportToTheChamber, Queen.Colonel, Queen.FollowVandeweghe, Queen.TalkToVandeweghe, Queen.SweepTheRoom, Queen.TalkAgain })
                Assert.AreEqual(MissionObjectiveState.Inactive, Objective(harness, Queen.MissionId, objective), $"{objective} waits");
        }

        /// <summary>
        /// An abandon of Assessing The Atta after the men set off: the men go; taken again, they stand by Frogga again, 1 is open, 2
        /// and 3 wait, and Tananda hears nothing.
        /// </summary>
        [TestMethod]
        public void AssessingTheAttaAbandonedStartsOver()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var copy = IntoHive(harness);
            AshenRuntime.SeedCompleted(harness, AshenPetrovichsRunV1.MissionId);
            Accept(harness, Giver(copy, Outpost.PetrovichTop), Atta.MissionId);
            harness.MoveTo(V(Outpost.PetrovichTopAt) + new Vector3(0, 0, 3));
            for (var tick = 0; tick < 8; tick++)
                Tick(harness, copy, 1000);
            Assert.IsGreaterThan(3f, Vector3.Distance(SceneCreature(harness, copy, Atta.MissionId, Atta.ManRole(0)).Position, ToVector(Atta.MenAt[0])), "on their way");
            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Atta.MissionId));
            harness.Drain();
            for (var tick = 0; tick < 4; tick++)
                Tick(harness, copy, 250);
            for (var index = 0; index < Atta.MenAt.Length; index++)
                Assert.IsNull(SceneCreature(harness, copy, Atta.MissionId, Atta.ManRole(index)), $"man {index + 1} goes with the abandon");
            Accept(harness, Giver(copy, Outpost.PetrovichTop), Atta.MissionId);
            Tick(harness, copy, 250);
            for (var index = 0; index < Atta.MenAt.Length; index++)
            {
                var man = SceneCreature(harness, copy, Atta.MissionId, Atta.ManRole(index));
                Assert.IsNotNull(man, $"man {index + 1} again");
                Assert.IsLessThan(1.5f, Vector3.Distance(ToVector(Atta.MenAt[index]), man.Position), $"man {index + 1} by Frogga again");
            }
            Assert.AreEqual((MissionObjectiveState.Incomplete, MissionObjectiveState.Inactive, MissionObjectiveState.Inactive),
                (Objective(harness, Atta.MissionId, Atta.FindingTananda), Objective(harness, Atta.MissionId, Atta.TanandasStory),
                    Objective(harness, Atta.MissionId, Atta.ReturnToPetrovich)), "1 open, 2 and 3 wait");
            Assert.IsEmpty(Topics(harness, Npc(copy, N.Tananda), Atta.MissionId), "Tananda hears nothing");
        }

        /// <summary>
        /// Tananda killed in the squad's copy (she stands 20 m from the warcry pool 531051) comes back on her pool's own respawn time
        /// (20 tenths, 2 s), not after the copy's 45 minutes: 1536's scene names her pool as a public spawn. 1536 is then done with
        /// her as before (2's talk).
        /// </summary>
        [TestMethod]
        public void TanandaKilledInTheSquadCopyComesBackOnHerOwnTime()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var copy = IntoHive(harness);
            Assert.IsNotNull(copy.SquadState, "a squad's copy");
            Assert.IsTrue(SpawnPoolManager.Instance.IsNamedPublicSpawn(N.Tananda), "1536's scene names her pool");
            Spawn(copy, N.Tananda);
            Kill(harness, copy, Npc(copy, N.Tananda));
            Creature Standing() => Here(copy).FirstOrDefault(creature => creature.SpawnPool?.DbId == N.Tananda &&
                creature.State != CharacterState.Dead && creature.Npc != null);
            Assert.IsNull(Standing(), "Tananda down");
            var all = copy.SpawnPools.ToArray();
            try
            {
                copy.SpawnPools.Clear();
                copy.SpawnPools.Add(all.Single(candidate => candidate.DbId == N.Tananda));
                for (var second = 1; second <= 10; second++)
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
            var tananda = Standing();
            Assert.IsNotNull(tananda, "Tananda back in the squad's copy on her own respawn time");

            AshenRuntime.SeedCompleted(harness, AshenPetrovichsRunV1.MissionId);
            Accept(harness, Giver(copy, Outpost.PetrovichTop), Atta.MissionId);
            Tick(harness, copy, 250);
            Enter(harness, V(N.TanandaAt));
            Talk(harness, tananda, Atta.MissionId, Atta.TanandasStory);
            Tick(harness, copy, 250);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Atta.MissionId, Atta.ReturnToPetrovich), "her talk done: 3 open");
        }

        /// <summary>
        /// An abandon of The Big Secret in the Eloh Sanctuary (1 done): taken again, 1 is open and 2 waits; Zuriel hears nothing.
        /// </summary>
        [TestMethod]
        public void TheBigSecretAbandonedStartsOver()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var copy = IntoHive(harness);
            LoadPads(harness, copy);
            AshenRuntime.SeedCompleted(harness, AshenPetrovichsRunV1.MissionId, Atta.MissionId, Queen.MissionId);
            Accept(harness, Giver(copy, Outpost.PetrovichTop), Secret.MissionId);
            Tick(harness, copy, 250);
            Teleport(harness, copy, copy.Teleporters[N.WestPad]);
            Enter(harness, ToVector(Secret.ChamberAt));
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Secret.MissionId, Secret.FinalChamber), "1 done");
            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Secret.MissionId));
            harness.Drain();
            Tick(harness, copy, 250);
            Accept(harness, Giver(copy, Outpost.PetrovichTop), Secret.MissionId);
            Tick(harness, copy, 250);
            Assert.AreEqual((MissionObjectiveState.Incomplete, MissionObjectiveState.Inactive),
                (Objective(harness, Secret.MissionId, Secret.FinalChamber), Objective(harness, Secret.MissionId, Secret.SpeakWithZuriel)), "1 open, 2 waits");
            Assert.IsEmpty(Topics(harness, Npc(copy, N.Zuriel), Secret.MissionId), "Zuriel hears nothing");
        }

        /// <summary>
        /// The Big Secret: Petrovich offers it after 1537 with its four stacks; the west pad's arrival in the Eloh Sanctuary is short
        /// of the round room; the walk into it completes 1; Zuriel hears 2 and his talk makes it completeable; Petrovich does not take
        /// it in; Yamamoto in Ashen Desert does, waits with a full bag and pays the stack chosen once.
        /// </summary>
        [TestMethod]
        public void TheBigSecretGoesThroughTheEloHSanctuaryToYamamoto()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var copy = IntoHive(harness);
            LoadPads(harness, copy);
            AshenRuntime.SeedCompleted(harness, AshenPetrovichsRunV1.MissionId, Atta.MissionId);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, Giver(copy, Outpost.PetrovichTop), Secret.MissionId), "not before 1537");
            AshenRuntime.SeedCompleted(harness, Queen.MissionId);
            var top = Giver(copy, Outpost.PetrovichTop);
            var offer = WildernessRanjaGorgeTests.Offer(harness, top, Secret.MissionId);
            CollectionAssert.AreEqual(new[] { Secret.FragGrenade, Secret.EmpBomb, Secret.CryogenicGrenade, Secret.StandardMedPack },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Accept(harness, top, Secret.MissionId);
            Tick(harness, copy, 250);
            Assert.AreEqual((MissionObjectiveState.Incomplete, MissionObjectiveState.Inactive),
                (Objective(harness, Secret.MissionId, Secret.FinalChamber), Objective(harness, Secret.MissionId, Secret.SpeakWithZuriel)), "1 open, 2 waits");

            Teleport(harness, copy, copy.Teleporters[N.WestPad]);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Secret.MissionId, Secret.FinalChamber), "the pad's arrival is short of the round room");
            var zuriel = Npc(copy, N.Zuriel);
            Assert.IsEmpty(Topics(harness, zuriel, Secret.MissionId), "Zuriel hears nothing before 1");
            Enter(harness, ToVector(Secret.ChamberAt));
            Assert.AreEqual((MissionObjectiveState.Completed, MissionObjectiveState.Incomplete),
                (Objective(harness, Secret.MissionId, Secret.FinalChamber), Objective(harness, Secret.MissionId, Secret.SpeakWithZuriel)), "in the round room: 1 done, 2 open");
            Talk(harness, zuriel, Secret.MissionId, Secret.SpeakWithZuriel);
            Tick(harness, copy, 250);
            Assert.IsTrue(harness.Client.Player.Missions[Secret.MissionId].Completeable, "to be turned in");
            top = Giver(copy, Outpost.PetrovichTop);
            harness.MoveTo(top.Position);
            harness.Drain();
            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, top.EntityId, Secret.MissionId, 0), "not Petrovich's to take in");
            harness.Drain();

            harness.Manager.Scenes.Detach(harness.Client, copy);
            var desert = AshenLeadInsTests.EnterAshen(harness, V(AshenLeadInNpcsV1.YamamotoAt) + new Vector3(0, 0, 2));
            var yamamoto = Npc(desert, AshenLeadInNpcsV1.Yamamoto);
            FullBagWaits(harness, yamamoto, Secret.MissionId, 2, ConsumableFiller);
            FailedTurnInPaysNothing(harness, yamamoto, Secret.MissionId, 2);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, yamamoto, Secret.MissionId, 2, Secret.Experience, Secret.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Secret.MissionId), "MissionRewarded once");
            Assert.AreEqual(Secret.CryogenicGrenades, Facility.Held(harness, Secret.CryogenicGrenade), "the grenades chosen");
        }

        /// <summary>
        /// Ashen_avernus_hive takes out 1536, 1537 and 1581, the set's eight creatures (creature, stats, look, packages) with their
        /// five pools, and the pads 917-920 with their links; the rows they copy stay; it updates nothing. Every table the migration
        /// writes is counted (set 18's ids and missions only). Nothing else changes: the other pools, pads, missions and markers as a
        /// database migrated only up to Ashen_avernus_hive holds them. Up again, all of it is back; the NPC SQL run again changes
        /// nothing.
        /// </summary>
        [TestMethod]
        public void AvernusHiveRollsBackWithItsMigration()
        {
            using var harness = WildernessRuntimeTestHarness.Create(targetWorldMigration: AshenMissionTests.AvernusHive);
            var world = harness.World;
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking()
                .Any(entry => entry.MissionId == mission && entry.ContentRevision == AshenMissionDataV1.Revision);
            int Count(string sql) => world.Database.SqlQueryRaw<int>(sql).AsEnumerable().Single();
            const string ids = "(mission_id IN (1536, 1537, 1581) AND content_revision = 'ashen_1_16_5')";
            var npcs = string.Join(", ", N.Creatures.Append(N.BanePool));
            var pads = string.Join(", ", Pads);
            var copied = string.Join(", ", N.Friends.Select(npc => npc.Copy).Concat(N.Bane.Select(npc => npc.Copy)).Distinct());
            var tables = new[]
            {
                "mission_content_definition", "mission_objective_definition", "mission_objective_transition", "mission_trigger", "mission_action",
                "mission_prerequisite", "mission_reward_definition", "mission_reward_item", "mission_indicator", "mission_scene_binding", "mission_evidence",
                "mission_scenario", "mission_scenario_step", "mission_channel_policy", "mission_repeat_policy", "mission_spawn", "mission_spawn_group", "mission_area"
            };
            string All() => string.Join(", ", tables.Select(table => $"{table} {Count($"SELECT COUNT(*) AS Value FROM {table} WHERE {ids}")}")
                .Concat(new[] { "creature", "creature_stat", "creature_appearance", "npc_package", "spawnpool" }
                    .Select(table => $"{table} {Count($"SELECT COUNT(*) AS Value FROM {table} WHERE id IN ({npcs})")}"))
                .Append($"teleporter {Count($"SELECT COUNT(*) AS Value FROM teleporter WHERE id IN ({pads})")}")
                .Append($"local_teleporter_link {Count($"SELECT COUNT(*) AS Value FROM local_teleporter_link WHERE from_teleporter_id IN ({pads}) OR to_teleporter_id IN ({pads})")}")
                .Append($"kept {Count($"SELECT COUNT(*) AS Value FROM creature WHERE id IN ({copied})")}"));
            static string Others(DbContext db) => string.Join("; ", db.Set<SpawnPoolEntry>().AsNoTracking()
                    .Where(entry => (entry.MapContextId == AshenMissionDataV1.MapContextId || entry.MapContextId == N.Outpost) &&
                        (entry.Id < 790650 || entry.Id > 790659)).OrderBy(entry => entry.Id).AsEnumerable()
                    .Select(entry => FormattableString.Invariant($"{entry.Id}:{entry.PosX:R},{entry.PosY:R},{entry.PosZ:R},{entry.Radius:R},{entry.Creature1Id},{entry.Creature1MaxCount}")))
                + " | " + string.Join("; ", db.Set<TeleporterEntry>().AsNoTracking().Where(entry => entry.Id < 917 || entry.Id > 920)
                    .OrderBy(entry => entry.Id).AsEnumerable().Select(entry => FormattableString.Invariant($"{entry.Id}:{entry.MapContextId},{entry.PosX:R},{entry.PosZ:R}")))
                + " | " + string.Join("; ", db.Set<MissionContentDefinitionEntry>().AsNoTracking()
                    .Where(entry => entry.MissionId != 1536 && entry.MissionId != 1537 && entry.MissionId != 1581)
                    .OrderBy(entry => entry.MissionId).ThenBy(entry => entry.ContentRevision)
                    .AsEnumerable().Select(entry => $"{entry.MissionId}/{entry.ContentRevision}"))
                + " | " + string.Join("; ", db.Set<MissionIndicatorEntry>().AsNoTracking()
                    .Where(entry => (entry.ContentRevision == AshenMissionDataV1.Revision || entry.ContentRevision == CrucibleMissionDataV1.Revision)
                        && entry.MissionId != 1536 && entry.MissionId != 1537 && entry.MissionId != 1581)
                    .OrderBy(entry => entry.MissionId).ThenBy(entry => entry.ObjectiveId).ThenBy(entry => entry.IndicatorId).AsEnumerable()
                    .Select(entry => FormattableString.Invariant($"{entry.MissionId}.{entry.ObjectiveId}/{entry.IndicatorId}:{entry.PosX:R},{entry.PosZ:R}")));
            string others;
            using (var atHive = WildernessRuntimeTestHarness.Create(targetWorldMigration: AshenMissionTests.AvernusHive))
                others = Others(atHive.World);
            // 1536: 3 objectives and transitions, triggers the area (1) and two talks (2, 3); actions reveal and activate (1, 2),
            // the reward (3); one area. 1537: 7 objectives and transitions, triggers three areas (1, 2, 4), three talks (3, 5, 7)
            // and the pool (6); actions reveal and activate (1-6), the lead (3), the leave and the reward (7); scenarios 1-2 (no
            // step: neither is counted); three areas. 1581: 2 objectives and transitions, the area and the talk; reveal, activate, the reward; one
            // area. Each one prerequisite, one reward (four items), three evidence rows; markers 3, 7, 2; three scenes. Four
            // friendly creatures with stats, packages, pools (not Vandeweghe: 1537's copy only) and their copies' look rows (Ida
            // Hayner's for Tananda, Harris' for Frogga and Vandeweghe, none for Zuriel: the hologram Eloh has none); the four Bane
            // with stats, no look row, no package; their pool; four pads and four links.
            const string up = "mission_content_definition 3, mission_objective_definition 12, mission_objective_transition 12, mission_trigger 12, " +
                "mission_action 23, mission_prerequisite 3, mission_reward_definition 3, mission_reward_item 12, mission_indicator 12, " +
                "mission_scene_binding 3, mission_evidence 9, mission_scenario 2, mission_scenario_step 0, mission_channel_policy 0, " +
                "mission_repeat_policy 0, mission_spawn 0, mission_spawn_group 0, mission_area 5, creature 8, creature_stat 8, " +
                "creature_appearance LOOK, npc_package 4, spawnpool 4, teleporter 4, local_teleporter_link 4, kept 7";
            const string down = "mission_content_definition 0, mission_objective_definition 0, mission_objective_transition 0, mission_trigger 0, " +
                "mission_action 0, mission_prerequisite 0, mission_reward_definition 0, mission_reward_item 0, mission_indicator 0, " +
                "mission_scene_binding 0, mission_evidence 0, mission_scenario 0, mission_scenario_step 0, mission_channel_policy 0, " +
                "mission_repeat_policy 0, mission_spawn 0, mission_spawn_group 0, mission_area 0, creature 0, creature_stat 0, " +
                "creature_appearance 0, npc_package 0, spawnpool 0, teleporter 0, local_teleporter_link 0, kept 7";
            var look = Count($"SELECT COUNT(*) AS Value FROM creature_appearance WHERE id = {N.Harris}") * 2 + Count($"SELECT COUNT(*) AS Value FROM creature_appearance WHERE id = {N.IdaHayner}")
                + Count($"SELECT COUNT(*) AS Value FROM creature_appearance WHERE id = {N.Jumna}");
            var upNow = up.Replace("LOOK", look.ToString(System.Globalization.CultureInfo.InvariantCulture));
            void AllUp(string when)
            {
                Assert.IsTrue(Missions.All(Has), $"{when}: the three missions");
                var all = All();
                Assert.AreEqual(upNow, all, $"{when}: {all}");
                Assert.AreEqual(others, Others(world), $"{when}: nothing else changes");
            }

            AllUp("at its migration");
            world.GetService<IMigrator>().Migrate(AshenMissionTests.Previous(world, AshenMissionTests.AvernusHive));
            world.ChangeTracker.Clear();
            Assert.IsFalse(Missions.Any(Has));
            Assert.AreEqual(down, All());
            Assert.AreEqual(others, Others(world), "Down: nothing else changes");
            Assert.IsTrue(AshenMissionTests.MissionIds.Where(id => !Missions.Contains(id) && !AshenConscriptionEntryTests.Missions.Contains(id) &&
                !AshenConscriptionQueenTests.Missions.Contains(id)).All(Has), "sets 1-17 stay");
            foreach (var sql in N.PadsDown())
                world.Database.ExecuteSqlRaw(sql);
            foreach (var sql in N.Down())
                world.Database.ExecuteSqlRaw(CrucibleSql.Guard(sql));
            Assert.AreEqual(down, All(), "a second Down changes nothing");

            world.GetService<IMigrator>().Migrate(AshenMissionTests.AvernusHive);
            world.ChangeTracker.Clear();
            AllUp("Up again");
            foreach (var sql in N.Up())
                world.Database.ExecuteSqlRaw(CrucibleSql.Guard(sql));
            foreach (var sql in N.PadsUp())
                world.Database.ExecuteSqlRaw(sql);
            AllUp("the NPC and pad rows run again");
        }

        /// <summary>
        /// The squad's copy of Avernus Outpost (AshenAvernusOutpostTests.IntoOutpost), and set 18's leftovers of an earlier test gone:
        /// its NPC pools' cleared marks, an earlier test's scene copies (a squad copy outlives a test).
        /// </summary>
        internal static MapChannel IntoHive(WildernessRuntimeTestHarness harness)
        {
            var copy = AshenAvernusOutpostTests.IntoOutpost(harness);
            foreach (var pool in copy.SpawnPools.Where(pool => Pools.Contains(pool.DbId)))
                pool.ClearedAtUtcMs = 0;
            var runs = Missions.SelectMany(mission => Runs(harness, mission)).ToHashSet();
            copy.SpawnPools.RemoveAll(pool => pool.SceneRunId != null && Missions.Contains(pool.ScenarioMissionId) && !runs.Contains(pool.SceneRunId));
            foreach (var creature in Here(copy).Where(one => Pools.Contains(one.SpawnPool?.DbId ?? 0)).ToArray())
                if (creature.State == CharacterState.Dead)
                    Respawn(copy, creature.SpawnPool.DbId);
            harness.Manager.Scenes.Resume(harness.Client);
            harness.Drain();
            return copy;
        }

        /// <summary>The hostile spawn pools of Avernus Outpost.</summary>
        internal static List<SpawnPoolEntry> HostilePools(WildernessRuntimeTestHarness harness) => AshenAvernusOutpostTests.HostilePools(harness);

        /// <summary>
        /// The set's places: label, place, whether it is seen from the ring, the height looked at, its clearance (34 m, 2D, outside
        /// every hostile pool's circle; the breaks, each the nearest upstream warcry pool on the only floor there: Tananda 20.0 m
        /// and the men's stop 20.0 m from 531051 at the Cave In, fix M: Vandeweghe 18.8 m and the queen pad 22.4 m from 531062 in the hollow down the
        /// Queen's Chamber; docs/ashen-playtest.md).
        /// </summary>
        internal static List<(string Label, Vector3 At, bool Ring, float Middle, float Clearance)> Places() => new()
        {
            ("Tananda", V(N.TanandaAt), true, F.NpcMiddle, 19), ("Frogga", V(N.FroggaAt), true, F.NpcMiddle, Clearance),
            ("Vandeweghe", V(M.VandewegheAt), true, F.NpcMiddle, 18), ("Zuriel", V(N.ZurielAt), true, F.NpcMiddle, Clearance),
            ("the southern tunnel's pad", V(N.TunnelPadAt), false, 0, Clearance), ("the queen pad", V(M.QueenPadAt), false, 0, 22),
            ("the west pad", V(N.WestPadAt), false, 0, Clearance), ("the sanctuary pad", V(N.SanctuaryPadAt), false, 0, Clearance),
            ("the man 1's place", ToVector(Atta.MenAt[0]), false, 0, Clearance), ("the man 2's place", ToVector(Atta.MenAt[1]), false, 0, Clearance),
            ("the men's stop", ToVector(Atta.TunnelWay[^1]), false, 0, 19), ("the tunnel's bottom", ToVector(Queen.BottomAt), true, F.NpcMiddle, Clearance),
            ("the round room", ToVector(Secret.ChamberAt), false, 0, Clearance), ("the Bane's pool", V(R.BaneAt), false, 0, 60),
            ("Petrovich at the top", V(Outpost.PetrovichTopAt), true, F.NpcMiddle, Clearance)
        };

        private const float Clearance = 34;

        /// <summary>The upstream warcry pools under 100 m from the Bane's pool since fix R (breaks): 531043, the Queen's Chamber's Atta,
        /// 70.9 m outside both circles; 531044, an island 29 m above the chamber's floor (not on the bottom's navmesh region), 89.8 m.</summary>
        internal static readonly uint[] WarcryBreaks = { 531043, 531044 };

        /// <summary>How far each leg's start is moved along its first stretch for the walk's recheck (as AshenAvernusOutpostTests).</summary>
        private static readonly float[] StartShifts = { 0, 0.13f, 0.27f, 0.4f };

        /// <summary>The upstream pools a walk passes (rule breaks, docs/ashen-playtest.md): on the northern tunnel, the southern tunnel, the Food Storage's west side and the Queen's Chamber tunnel the only way.</summary>
        private static readonly uint[] NorthernTunnelPools = { 531045, 531051 };
        private static readonly uint[] SouthernTunnelPools = { 531046, 531057, 531059 };
        private static readonly uint[] WestSidePools = { 531048, 531057, 531059 };
        private static readonly uint[] QueenPadPools = { 531062 };
        private static readonly uint[] ChamberTunnelPools = { 531044, 531056, 531058, 531062 };

        /// <summary>
        /// The legs of the set's walks on the client's floor, each walked there and back on its own (corners: a client-floor
        /// flood's way, 2026-11-12, the climb rule's, ESTIMATE of a player's line). A walk is a run of legs.
        /// </summary>
        internal static readonly Dictionary<string, Vector3[]> Legs = new()
        {
            ["from Petrovich"] = new Vector3[]
            {
                new(645.5f, 352.67f, -246.5f), new(646f, 352.76f, -246f)
            },
            ["to Frogga"] = new Vector3[]
            {
                new(645.5f, 352.67f, -246.5f), new(652f, 353.32f, -238f)
            },
            // Fix M (row 48): the queen pad and Vandeweghe in the hollow down the tunnel, off 531058's spawn ground.
            ["the queen pad to Vandeweghe"] = new Vector3[]
            {
                new(306f, 218.11f, -326f), new(306f, 218.36f, -332.5f), new(294f, 216.61f, -330f)
            },
            ["the sanctuary pad to Zuriel"] = new Vector3[]
            {
                new(628f, 275.98f, -639f), new(628f, 276.03f, -645f), new(622f, 275.99f, -660f), new(619.5f, 276f, -675f),
                new(628.6f, 276f, -677.8f)
            },
            ["to Tananda 1"] = new Vector3[]
            {
                new(646f, 352.76f, -246f), new(606f, 351.36f, -246f), new(577f, 357.11f, -244f), new(561f, 354.36f, -220f),
                new(561f, 351.99f, -180f), new(557f, 352.12f, -144f), new(557f, 359f, -132f), new(540f, 363.52f, -126f),
                new(531f, 367.02f, -131f), new(530f, 367.92f, -131f), new(530f, 367.81f, -132f)
            },
            ["to Tananda 2"] = new Vector3[]
            {
                new(530f, 367.81f, -132f), new(513f, 372.55f, -132f), new(513f, 373.18f, -139f), new(503f, 378.13f, -150f),
                new(503f, 376.97f, -156f), new(498f, 378.48f, -161f), new(488f, 379.78f, -166f), new(463f, 382.41f, -163f),
                new(463f, 385.66f, -123f), new(462f, 386.61f, -87f)
            },
            ["to Tananda 3"] = new Vector3[]
            {
                new(462f, 386.61f, -87f), new(460f, 386.77f, -81f), new(457f, 387.01f, -75f), new(449f, 390.04f, -65f),
                new(449f, 391.46f, -45f)
            },
            ["to the southern pad 1"] = new Vector3[]
            {
                new(646f, 352.76f, -246f), new(606f, 351.36f, -246f), new(595f, 353.61f, -275f), new(595f, 358.61f, -292f),
                new(567f, 368.36f, -313f), new(557f, 370.62f, -312f), new(557f, 372.16f, -352f),
                new(557f, 372.33f, -392f), new(557f, 373.25f, -413f), new(537f, 369.72f, -425f), new(520f, 363.49f, -433f),
                new(520f, 358.36f, -447f), new(522f, 361.61f, -469f), new(521f, 362.36f, -483f), new(520f, 364.61f, -490f)
            },
            ["to the southern pad 2"] = new Vector3[]
            {
                // Fix M follow-up: clear of pad 917's east wall and its ramp's end (AshenFixMTests.AddPads), onto the ramp from the south.
                new(520f, 364.61f, -490f), new(521f, 367.61f, -501f), new(516f, 367.4f, -504f), new(514f, 367.9f, -508.5f),
                new(512.5f, 368.4f, -511.9f), new(509f, 368.9f, -512f), new(509f, 368.37f, -506f)
            },
            ["down to the bottom 1"] = new Vector3[]
            {
                new(294f, 216.61f, -330f), new(290f, 216.9f, -336f), new(290f, 218.5f, -340.33f), new(282f, 219.86f, -342f),
                new(257f, 216.36f, -327f), new(231f, 217.11f, -315f), new(205f, 219.61f, -323f),
                new(208f, 218.36f, -326f), new(208f, 216.61f, -341f), new(210f, 215.86f, -348f), new(210f, 215.86f, -351f),
                new(203f, 216.41f, -378f), new(215f, 217.71f, -388f), new(214f, 217.14f, -395f), new(208f, 214.7f, -411f),
                new(168f, 208.89f, -411f)
            },
            ["down to the bottom 2"] = new Vector3[]
            {
                new(168f, 208.89f, -411f), new(158f, 206.21f, -401f), new(156f, 203.1f, -380f), new(150f, 201.02f, -379f),
                new(128f, 196.22f, -373f), new(118f, 184.55f, -398f), new(118f, 179.66f, -408f), new(119f, 176.31f, -411f),
                new(144f, 168.28f, -418f), new(184f, 159.17f, -418f), new(224f, 153.26f, -418f), new(264f, 150.83f, -418f),
                new(273f, 144.52f, -449f), new(273f, 144.25f, -462f)
            },
            ["the bottom to the Bane"] = new Vector3[]
            {
                // Fix R: the Bane's pool on the chamber's floor (set 18's way on to the west ledge climbed a 48-50 degree slope at
                // (240, -668) the real client did not climb, onto a navmesh island; AshenFixRTests).
                new(273f, 144.25f, -462f), new(257f, 151.86f, -473f), new(256f, 153.39f, -482f), new(251f, 154.11f, -488f),
                new(225f, 158.75f, -502f)
            },
            ["to the west pad"] = new Vector3[]
            {
                new(646f, 352.76f, -246f), new(606f, 351.36f, -246f), new(595f, 353.61f, -275f), new(595f, 358.61f, -292f),
                new(567f, 368.36f, -313f), new(557f, 370.62f, -312f), new(522f, 375.12f, -309f),
                // Fix M follow-up: clear of pad 919's east wall (AshenFixMTests.AddPads), 20.2 m from 531052's draws.
                new(521f, 375.12f, -311f), new(481f, 380.12f, -311f), new(477f, 382.87f, -321f), new(474f, 385.1f, -327.8f),
                new(470f, 386.08f, -330.5f), new(470f, 385.83f, -324f)
            },
        };

        /// <summary>The walks: label, its legs, the pools it passes (rule breaks), its target.</summary>
        internal static List<(string Label, string[] Legs, uint[] Passes, Vector3 Target)> Walks()
        {
            var none = Array.Empty<uint>();
            return new List<(string, string[], uint[], Vector3)>
            {
                ("Petrovich to Frogga", new[] { "to Frogga" }, none, V(N.FroggaAt)),
                ("Petrovich to Tananda (1536)", new[] { "from Petrovich", "to Tananda 1", "to Tananda 2", "to Tananda 3" }, NorthernTunnelPools, V(N.TanandaAt)),
                ("Petrovich to the southern tunnel's pad (1537)", new[] { "from Petrovich", "to the southern pad 1", "to the southern pad 2" }, SouthernTunnelPools, V(N.TunnelPadAt)),
                ("the queen pad to Vandeweghe (1537)", new[] { "the queen pad to Vandeweghe" }, QueenPadPools, V(M.VandewegheAt)),
                ("Vandeweghe down to the bottom (1537)", new[] { "down to the bottom 1", "down to the bottom 2" }, ChamberTunnelPools, ToVector(Queen.BottomAt) with { Y = 144.25f }),
                ("the bottom to the Bane (1537)", new[] { "the bottom to the Bane" }, none, V(R.BaneAt)),
                ("Petrovich to the west pad (1581)", new[] { "from Petrovich", "to the west pad" }, WestSidePools, V(N.WestPadAt)),
                ("the sanctuary pad to Zuriel (1581)", new[] { "the sanctuary pad to Zuriel" }, none, V(N.ZurielAt))
            };
        }

        private static List<Vector3> Line(IEnumerable<string> legs) =>
            legs.SelectMany((leg, index) => index == 0 ? Legs[leg] : Legs[leg].Skip(1)).ToList();

        /// <summary>The player from the accept through 1-5 (the pad, the far side, the talk, the bottom, the talk); 6 then opens.</summary>
        private static void ToTheSweep(WildernessRuntimeTestHarness harness, MapChannel copy)
        {
            Enter(harness, V(N.TunnelPadAt));
            Teleport(harness, copy, copy.Teleporters[N.TunnelPad]);
            Enter(harness, V(M.VandewegheAt));
            Talk(harness, SceneCreature(harness, copy, Queen.MissionId, Queen.ColonelRole), Queen.MissionId, Queen.Colonel);
            Tick(harness, copy, 250);
            Enter(harness, ToVector(Queen.BottomAt));
            var colonel = SceneCreature(harness, copy, Queen.MissionId, Queen.ColonelRole);
            harness.MoveTo(colonel.Position);
            Talk(harness, colonel, Queen.MissionId, Queen.TalkToVandeweghe);
            Tick(harness, copy, 250);
            Tick(harness, copy, 250);
        }

        /// <summary>The Bane's pool standing again (an earlier test's or step's kills and cleared mark gone).</summary>
        private static void ResetBane(WildernessRuntimeTestHarness harness, MapChannel copy)
        {
            foreach (var pool in copy.SpawnPools.Where(pool => pool.DbId == N.BanePool))
            {
                foreach (var straggler in Here(copy).Where(creature => creature.SpawnPool == pool).ToArray())
                    Kill(harness, copy, straggler);
                pool.ClearedAtUtcMs = 0;
                pool.UpdateTimer = pool.RespawnTime;
            }
            Spawn(copy, N.BanePool);
            harness.Drain();
        }

        /// <summary>The pool standing in the copy, every creature of it killed, the pools' pass after (AshenAvernusOutpostTests.KillPools).</summary>
        private static void KillPools(WildernessRuntimeTestHarness harness, MapChannel copy, params uint[] pools)
        {
            foreach (var pool in copy.SpawnPools.Where(pool => pools.Contains(pool.DbId)))
            {
                foreach (var straggler in Here(copy).Where(creature => creature.SpawnPool == pool).ToArray())
                    Kill(harness, copy, straggler);
                pool.ClearedAtUtcMs = 0;
            }
            Spawn(copy, pools);
            var standing = Here(copy).Where(creature => pools.Contains(creature.SpawnPool?.DbId ?? 0)).ToArray();
            Assert.IsGreaterThanOrEqualTo(pools.Length, standing.Length, "the pools stand");
            foreach (var creature in standing)
                Kill(harness, copy, creature);
            SpawnPoolManager.Instance.SpawnPoolWorker(copy, 0);
            harness.Drain();
        }

        /// <summary>The mate's own scene creature of <paramref name="role"/> (of the mate's run).</summary>
        private static Creature MateCreature(WildernessRuntimeTestHarness harness, MapChannel copy, Facility.SquadMate mate, uint mission, string role)
        {
            using var unit = harness.CreateChar();
            var runs = unit.CharacterMissions.Runtime.Scenes(mate.Client.Player.Id, mission).Select(scene => scene.RunId).ToHashSet();
            return Here(copy).SingleOrDefault(creature => creature.SpawnPool?.SceneActorRole == role && runs.Contains(creature.SpawnPool.SceneRunId) &&
                creature.State != CharacterState.Dead);
        }

        /// <summary>The mate's walk from <paramref name="from"/> into the area at <paramref name="to"/> (MissionAreaService).</summary>
        private static void MateEnters(WildernessRuntimeTestHarness harness, Facility.SquadMate mate, Vector3 from, Vector3 to)
        {
            mate.Beside(to);
            mate.Client.MissionAreaService.RecordAcceptedMovement(mate.Client, from, to);
            harness.Drain();
        }

        /// <summary>The mate's talk: the objective is done for him.</summary>
        private static void MateTalks(WildernessRuntimeTestHarness harness, MapChannel copy, Facility.SquadMate mate, Creature npc, uint mission, uint objective)
        {
            Assert.IsNotNull(npc, $"{mission}.{objective}: the mate's NPC");
            mate.Beside(npc.Position + new Vector3(0, 0, 1));
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(mate.Client, npc.EntityId, mission, objective, 1), $"the mate's {mission}.{objective}");
            harness.Drain();
            Tick(harness, copy, 250);
            Assert.AreEqual(MissionObjectiveState.Completed, mate.Of(objective), $"the mate's {objective} done");
        }

        /// <summary>The set's pad rows on the copy, as InitTeleporters puts them on a loaded map (AshenAvernusOutpostTests.Pads), and the links.</summary>
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

        /// <summary>Through the pad onto the far one (AshenAvernusOutpostTests.Teleport).</summary>
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
        private const uint ArmorFiller = 12748;            // Armor_T3_Bio_V01_CMN_Gloves_30_to_34 (inventory category 1, as AshenIndraPassTests)

        private static Vector3 V((double X, double Y, double Z) at) => new((float)at.X, (float)at.Y, (float)at.Z);
    }
}
