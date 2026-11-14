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
using Rasa.Missions.Scenes;
using Rasa.Packets.LootDispenser.Client;
using Rasa.Packets.MapChannel.Client;
using Rasa.Packets.MapChannel.Server;
using Rasa.Packets.Mission.Server;
using Rasa.Services.Preloader.Missions.Ashen;
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
    using N = AshenIndraFacilityNpcsV1;
    using Caverns = AshenIndraCavernsV1;
    using Threshold = AshenThresholdV1;
    using Vindication = AshenVindicationV1;
    using F = MarshesClientFloorTests;
    using WO = AshenWhiteOasisTests;

    /// <summary>
    /// Ashen Desert set 15, Indra Caverns (squad instance 2055) and its Brann facility, played through as a player plays it:
    /// Surveyor Odesha's Indra Caverns (1559: through the door, Major Diren just inside), Diren's Threshold (1360: Guard Bendal's
    /// key card, the Indra Watch Post taken back, the facility door opened with the card, Sergeant Drof's talk) and Drof's
    /// Vindication (1395: the key pass's doors, Scout Aksrah's radio, Overseer Kuutk's Thrax Hand, Scout Naimi's cell, Naimi's
    /// talk, the power supply). With them the set's places and walks on the client's floor.
    ///
    /// Why these tests: the set is the first Ashen squad instance, with keycard doors (the facility's gates the whole facility,
    /// the key pass's the three rooms, the hand Naimi's cell) and items a scene gives: a door opened without its key or for
    /// another player, a key kept after its door, a squad mate left behind a shut door with the event counted, a door shut
    /// again at a reload or a restart, or an item a squad mate is given twice would each break it.
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class AshenIndraFacilityTests
    {
        private const uint Map = N.Caverns;
        private const uint WeaponFiller = 3869;            // inventory category 1 (quest-test-pattern)

        /// <summary>How far outside every hostile pool's circle the set's places stand (2D; 30 m walk clearance and a margin).</summary>
        internal const float PlaceClearance = 34;

        /// <summary>A place this much higher or lower than a pool is on another floor of the caverns: the gap is measured in 3D.</summary>
        internal const float OtherFloor = 12;

        /// <summary>The pools set 15 adds.</summary>
        internal static readonly uint[] Pools = { N.Diren, N.Drof, N.Naimi, N.KuutkPool, N.WatchPostWest, N.WatchPostEast };

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

        /// <summary>
        /// The rows of the three missions: givers and receivers, levels, the Ashen category, the client's texts, objectives and
        /// talks (objectiveconversation 1360,2 package 1544 Drof, 1395,3 package 1651 Naimi), the chain 1280, 1559, 1360, 1395;
        /// the markers' maps (1559's first on Ashen Desert, every other on Indra Caverns: MissionMarkerProjection) and the
        /// client's names for them; each mission operational.
        /// </summary>
        [TestMethod]
        public void TheSetsRowsAreTheClients()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            const string revision = AshenMissionDataV1.Revision;
            var expected = new (uint Mission, uint Giver, uint Receiver, uint Level, uint Name, (uint, uint, uint)[] Objectives, (uint, uint?)[] Talks, uint Prerequisite)[]
            {
                (Caverns.MissionId, AshenIndraPassNpcsV1.Odesha, N.Diren, 42, 14171, new (uint, uint, uint)[] { (1, 14207, 14208), (2, 14209, 14210) },
                    Array.Empty<(uint, uint?)>(), AshenMissingScientistsV1.MissionId),
                (Threshold.MissionId, N.Diren, N.Drof, 43, 11183, new (uint, uint, uint)[] { (1, 11331, 11332), (2, 11339, 11340), (3, 11707, 11708),
                    (4, 11331, 11332), (5, 11331, 11332), (6, 11331, 11332) }, new (uint, uint?)[] { (2, 1544) }, Caverns.MissionId),
                (Vindication.MissionId, N.Drof, N.Drof, 43, 12023, new (uint, uint, uint)[] { (2, 12176, 12177), (3, 12178, 12179), (4, 12180, 12181),
                    (5, 12191, 12192), (6, 12241, 12242), (7, 12176, 12177), (8, 12180, 12181) }, new (uint, uint?)[] { (3, 1651) }, Threshold.MissionId)
            };
            var catalog = new MissionContentCatalog(harness, new Dictionary<uint, Mission>(), null);
            catalog.Load();
            foreach (var (mission, giver, receiver, level, name, objectives, talks, prerequisite) in expected)
            {
                var definition = world.MissionContentDefinitionEntries.AsNoTracking().Single(entry => entry.MissionId == mission && entry.ContentRevision == revision);
                Assert.AreEqual(((uint?)giver, (uint?)receiver, (uint?)level, (uint?)AshenMissionDataV1.CategoryId, name, true),
                    (definition.GiverId, definition.ReceiverId, definition.Level, definition.CategoryId, definition.ClientNameTextId, definition.Enabled), $"{mission}");
                var rows = world.MissionObjectiveDefinitionEntries.AsNoTracking().Where(entry => entry.MissionId == mission && entry.ContentRevision == revision)
                    .AsEnumerable().Select(entry => (entry.ObjectiveId, entry.ClientNameTextId, entry.ClientBodyTextId)).OrderBy(entry => entry.ObjectiveId).ToArray();
                CollectionAssert.AreEqual(objectives, rows, $"{mission}'s objectives");
                var talkRows = world.Set<MissionTriggerEntry>().AsNoTracking()
                    .Where(entry => entry.MissionId == mission && entry.ContentRevision == revision && entry.Kind == MissionTriggerKind.Conversation)
                    .AsEnumerable().Select(entry => (entry.ObjectiveId, entry.NpcPackageId)).OrderBy(entry => entry.ObjectiveId).ToArray();
                CollectionAssert.AreEqual(talks, talkRows, $"{mission}'s talks");
                Assert.AreEqual((uint?)prerequisite, world.MissionPrerequisiteEntries.AsNoTracking()
                    .Single(entry => entry.MissionId == mission && entry.ContentRevision == revision).RequiredMissionId, $"{mission}'s prerequisite");
                Assert.IsTrue(harness.Manager.LoadedMissions[mission].IsOperational, harness.Manager.LoadedMissions[mission].OperationalDiagnostic);
            }

            var markers = world.MissionIndicatorEntries.AsNoTracking()
                .Where(entry => entry.ContentRevision == revision && (entry.MissionId == Caverns.MissionId || entry.MissionId == Threshold.MissionId ||
                    entry.MissionId == Vindication.MissionId)).AsEnumerable()
                .Select(entry => (entry.MissionId, entry.ObjectiveId, entry.IndicatorId,
                    Map: MissionMarkerProjection.ResolveMap(catalog, entry.MissionId, entry.ObjectiveId, new MissionIndicator { IndicatorId = entry.IndicatorId })))
                .OrderBy(entry => entry.MissionId).ThenBy(entry => entry.ObjectiveId).ThenBy(entry => entry.IndicatorId).ToArray();
            CollectionAssert.AreEqual(new (uint, uint, uint, uint)[]
            {
                (1360, 1, 224, Map), (1360, 2, 222, Map), (1360, 3, 223, Map),
                (1395, 2, 225, Map), (1395, 3, 226, Map), (1395, 4, 227, Map), (1395, 5, 228, Map), (1395, 6, MissionIndicator.UnnamedFrom + 1, Map),
                (1559, 1, 261, AshenMissionDataV1.MapContextId),
                // Fix H: 1559.1's second marker, on the Indra Pass valley's lift pad 913 (unnamed).
                (1559, 1, MissionIndicator.UnnamedFrom + AshenFixHV1.Marker, AshenMissionDataV1.MapContextId), (1559, 2, 260, Map)
            }, markers, "the markers, their client names and maps");
        }

        /// <summary>
        /// The set's creatures (to-fix rows 20 and 26) in the id block 790560-790599: Diren, Drof and Naimi friendly with the
        /// client's names and packages (9727/1802, 9427/1544, 9530/1651), Kuutk hostile, the Overseers' class 10504, level 44, the
        /// client's name 9408, alone in his pool (one, radius 0, 300 s); the Watch Post's two pools one Thrax each (531008, 531009),
        /// radius 0, 120 s. No class of theirs carries a client IMMUNE_* damage flag (creature_class_flag 125-135). The set's
        /// hostile pools stand 60 m or more outside every other hostile pool's circle of Indra Caverns, Kuutk too (stricter
        /// than fix F's 35 m boss rule), and 100 m or more from every pool of a creature with the warcry (action 477).
        /// </summary>
        [TestMethod]
        public void TheSetsCreaturesCanBeBeatenAndStandApart()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            var rows = world.CreatureEntries.AsNoTracking().Where(row => row.Id >= 790560 && row.Id <= 790599).ToList();
            CollectionAssert.AreEquivalent(new[] { N.Diren, N.Drof, N.Naimi, N.Kuutk }, rows.Select(row => row.Id).ToArray(), "the four");
            foreach (var (id, name, package) in new[] { (N.Diren, 9727U, 1802U), (N.Drof, 9427U, 1544U), (N.Naimi, 9530U, 1651U) })
            {
                var row = rows.Single(entry => entry.Id == id);
                Assert.AreEqual((1U, name), ((uint)row.Faction, row.NameId), $"{id}: friendly, the client's name");
                Assert.AreEqual(package, world.NpcPackageEntries.AsNoTracking().Single(entry => entry.Id == id).PackageId, $"{id}'s package");
            }
            var kuutk = rows.Single(row => row.Id == N.Kuutk);
            Assert.AreEqual((0U, 10504U, 44U, 9408U), ((uint)kuutk.Faction, kuutk.ClassId, kuutk.Level, kuutk.NameId), "Kuutk: hostile, the Overseers' class, 44, the client's name");
            var immune = new uint[] { 125, 126, 127, 128, 129, 130, 131, 132, 133, 134, 135 };
            var classes = rows.Select(row => row.ClassId)
                .Concat(world.CreatureEntries.AsNoTracking().Where(row => row.Id == N.ThraxSoldier || row.Id == N.ThraxRavager).Select(row => row.ClassId).AsEnumerable())
                .Distinct().ToArray();
            CollectionAssert.IsSubsetOf(new[] { 20757U, 9354U, 10504U }, classes, "the Thrax classes read");
            foreach (var cls in classes)
                CollectionAssert.AreEqual(Array.Empty<uint>(), world.CreatureClassFlagEntries.AsNoTracking().Where(flag => flag.ClassId == cls).Select(flag => flag.FlagId).AsEnumerable()
                    .Where(immune.Contains).ToArray(), $"class {cls}: no damage immunity");

            var pools = world.SpawnPoolEntries.AsNoTracking().Where(pool => pool.Id >= 790560 && pool.Id <= 790599).ToList();
            CollectionAssert.AreEquivalent(Pools, pools.Select(pool => pool.Id).ToArray(), "the set's pools");
            Assert.IsTrue(pools.All(pool => pool.MapContextId == Map), "all on Indra Caverns");
            foreach (var (pool, creature, respawn) in new[] { (N.KuutkPool, N.Kuutk, 3000U), (N.WatchPostWest, N.ThraxSoldier, 1200U), (N.WatchPostEast, N.ThraxRavager, 1200U) })
            {
                var own = pools.Single(entry => entry.Id == pool);
                Assert.AreEqual((creature, (byte)1, (byte)1, respawn, 0.0, 0U), (own.Creature1Id, own.Creature1MinCount, own.Creature1MaxCount, own.RespawnTime, own.Radius,
                    own.Creature2Id), $"pool {pool}: one alone");
            }

            var hostile = HostilePools(harness);
            Assert.IsGreaterThan(10, hostile.Count, "the Indra Caverns hostile pools are read");
            var criers = AshenFixFTests.WarcryCreatures(harness);
            var crying = world.SpawnPoolEntries.AsNoTracking().Where(pool => pool.MapContextId == Map).AsEnumerable()
                .Where(pool => new[] { pool.Creature1Id, pool.Creature2Id, pool.Creature3Id, pool.Creature4Id, pool.Creature5Id, pool.Creature6Id }.Any(criers.Contains)).ToList();
            foreach (var pool in new[] { N.KuutkPool, N.WatchPostWest, N.WatchPostEast })
            {
                var own = pools.Single(entry => entry.Id == pool);
                var nearest = hostile.Where(other => other.Id != pool)
                    .Min(other => Vector2.Distance(new((float)own.PosX, (float)own.PosZ), new((float)other.PosX, (float)other.PosZ)) - (float)other.Radius);
                Assert.IsGreaterThanOrEqualTo(60f, nearest, $"pool {pool}: 60 m+ outside every other hostile pool's circle");
                foreach (var cry in crying)
                    Assert.IsGreaterThan(100f, Vector2.Distance(new((float)own.PosX, (float)own.PosZ), new((float)cry.PosX, (float)cry.PosZ)), $"pool {pool}: warcry pool {cry.Id}");
            }
        }

        /// <summary>
        /// In the squad's copy (Indra Caverns is a squad instance): Diren, Drof and Naimi come out of their pools friendly, the
        /// player can talk to them; Kuutk (a named public-spawn actor of 1395's scene: he comes back on his own time) and the
        /// Watch Post's two Thrax hostile, of their kinds, levels and hit points; each of them on the navmesh's ground.
        /// </summary>
        [TestMethod]
        public void TheSetsCreaturesStandInTheSquadsCopy()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var copy = IntoCaverns(harness);
            Assert.IsTrue(copy.IsSquadInstance, "a squad's copy");
            Assert.IsTrue(SpawnPoolManager.Instance.IsNamedPublicSpawn(N.KuutkPool), "Kuutk named by 1395's scene");
            Spawn(copy, Pools);
            foreach (var id in new[] { N.Diren, N.Drof, N.Naimi })
            {
                var npc = Here(copy).Single(one => one.SpawnPool?.DbId == id);
                Assert.AreEqual((id, TargetCategory.Friendly), (npc.DbId, npc.TargetCategory), $"{id}");
                Assert.IsNotNull(npc.Npc, $"{id} talks");
                AssertOnGround(copy, npc.Position, $"{id}");
            }
            var kuutk = Here(copy).Single(one => one.SpawnPool?.DbId == N.KuutkPool);
            Assert.AreEqual((N.Kuutk, N.KuutkLevel, TargetCategory.Hostile, N.KuutkClass, (int)N.KuutkHitPoints),
                (kuutk.DbId, (uint)kuutk.Level, kuutk.TargetCategory, (uint)kuutk.EntityClass, (int)kuutk.Attributes[Attributes.Health].NormalMax));
            AssertOnGround(copy, kuutk.Position, "Kuutk");
            foreach (var (pool, kind) in new[] { (N.WatchPostWest, N.ThraxSoldier), (N.WatchPostEast, N.ThraxRavager) })
            {
                var bane = Here(copy).Single(one => one.SpawnPool?.DbId == pool);
                Assert.AreEqual((kind, TargetCategory.Hostile), (bane.DbId, bane.TargetCategory), $"pool {pool}");
                AssertOnGround(copy, bane.Position, $"pool {pool}");
            }
        }

        /// <summary>
        /// The set's places on the client's floor (inconclusive without the client's files): the NPCs, Kuutk, the Watch Post's
        /// pools, Bendal's and Aksrah's bodies, the crate, the four doors and Naimi's cell each on the navmesh's ground (within
        /// 1 m) and a client floor (within 1 m), with no client surface over its head within 2.5 m (the caverns have no open sky);
        /// Diren, Drof, Kuutk, the bodies and the crate seen from all 16 places 2 m round it, Naimi from before her cell (line of
        /// sight: to-fix row 26); every one 34 m or more outside every hostile pool's circle (its own left out; a pool on another
        /// floor, 12 m and more above or below, in 3D). The markers of 1360 and 1395 on the navmesh and a client floor within 2 m;
        /// 1559's door marker the same on Ashen Desert, 5 m or more outside the door's circle.
        /// </summary>
        [TestMethod]
        public void ThePlacesStandOnTheClientsFloor()
        {
            var dataDir = F.ClientData();
            using var harness = WildernessRuntimeTestHarness.Create();
            var copy = IntoCaverns(harness);
            var nav = copy.NavMesh;
            using var meshes = new MeshLibrary(dataDir, F.MeshCsv());
            var geometry = MapGeometry.Load(Path.Combine(dataDir, "maps", MapName(harness, Map)), meshes, 1);
            var hostile = HostilePools(harness);
            var places = Places();
            var near = F.Near(geometry, places.Select(place => place.At).Append(V(Vindication.CellMarkerAt)).ToList(), 12);
            var failures = new List<string>();
            for (var i = 0; i < places.Count; i++)
            {
                var (label, at, ring, middle, own) = places[i];
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
                    if (Gap(at, pool) < PlaceClearance)
                        failures.Add(FormattableString.Invariant($"{label}: {Gap(at, pool):0.0} m outside hostile pool {pool.Id}'s circle"));
            }
            var cellFront = V(Vindication.CellMarkerAt);
            var frontFloor = F.FloorAt(near[^1], cellFront, cellFront.Y - 1, cellFront.Y + 1);
            Assert.IsNotNull(frontFloor, "a floor before the cell");
            if (!F.Clear(near[^1], cellFront with { Y = frontFloor.Value + F.Eye }, V(N.NaimiAt) + new Vector3(0, F.NpcMiddle, 0)))
                failures.Add("Naimi not seen from before her cell");

            var markers = harness.World.MissionIndicatorEntries.AsNoTracking()
                .Where(entry => entry.ContentRevision == AshenMissionDataV1.Revision && (entry.MissionId == Threshold.MissionId || entry.MissionId == Vindication.MissionId ||
                    (entry.MissionId == Caverns.MissionId && entry.ObjectiveId == Caverns.FindDiren))).AsEnumerable()
                .Select(marker => (Label: $"{marker.MissionId}.{marker.ObjectiveId}/{marker.IndicatorId}", At: new Vector3((float)marker.PosX, (float)marker.PosY, (float)marker.PosZ)))
                .ToList();
            Assert.HasCount(3 + 5 + 1, markers, "the Indra Caverns markers");
            var (markerFloors, _) = F.Surfaces(geometry, markers.Select(marker => (marker.At + new Vector3(0, 2 - F.MaxUnder, 0), F.Walkable)).ToList());
            for (var i = 0; i < markers.Count; i++)
            {
                if (nav.GroundHeight(markers[i].At + new Vector3(0, 2, 0)) is not { } y || MathF.Abs(y - markers[i].At.Y) > 2)
                    failures.Add($"{markers[i].Label}: navmesh {nav.GroundHeight(markers[i].At + new Vector3(0, 2, 0))}");
                if (markers[i].At.Y - markerFloors[i] > 2)
                    failures.Add($"{markers[i].Label}: client floor {markerFloors[i]}");
            }
            Assert.HasCount(13, places);
            Assert.IsEmpty(failures, $"{failures.Count}: " + string.Join("; ", failures));

            // 1559's door marker, on Ashen Desert.
            var desert = AshenLeadInsTests.EnterAshen(harness, ToVector(AshenIndraPassNpcsV1.OdeshaAt));
            var desertGeometry = MapGeometry.Load(Path.Combine(dataDir, "maps", MapName(harness, AshenMissionDataV1.MapContextId)), meshes, 1);
            var door = V(Caverns.DoorMarkerAt);
            var (doorFloors, _) = F.Surfaces(desertGeometry, new List<(Vector3, float)> { (door + new Vector3(0, 2 - F.MaxUnder, 0), F.Walkable) });
            Assert.IsTrue(desert.NavMesh.GroundHeight(door + new Vector3(0, 2, 0)) is { } doorGround && MathF.Abs(doorGround - door.Y) <= 2, "1559.1: the Ashen navmesh");
            Assert.IsLessThanOrEqualTo(2f, door.Y - doorFloors[0], "1559.1: a client floor");
            var link = harness.World.Set<MapLinkEntry>().AsNoTracking().Single(entry => entry.MapContextId == AshenMissionDataV1.MapContextId && entry.DestMapContextId == Map);
            Assert.IsGreaterThanOrEqualTo(5f, Vector2.Distance(new(door.X, door.Z), new((float)link.PosX, (float)link.PosZ)) - (float)link.Radius, "1559.1 outside the door's circle");
        }

        /// <summary>
        /// The walks on the client's floor (AshenFixBTests.ClientFloorLine with a player's half width, fix G's 55-degree climb
        /// rule), there and back by the corners Walks names (a client-floor flood's way, 2026-11-11), also with its start moved 0.13,
        /// 0.27 and 0.4 m along its first stretch (StartShifts, as AshenConscriptionEntryTests; fix U): from the arrival to Diren,
        /// to Bendal and to before the facility door; from behind the door to Aksrah, Kuutk, Drof, before Naimi's cell and the
        /// crate. Every walk keeps HostileClearance (30 m, 2D) outside the circle of every hostile pool of the caverns but the
        /// Watch Post's two (they stand on the only way in: 1360's own fight, written down in docs/ashen-playtest.md) and, for
        /// the walk to him, Kuutk's. The walk in crosses the facility door's leaf (the way's only door, the flood with it shut
        /// reaches nothing past it), the walks to Aksrah, Kuutk and the crate their rooms' doors' leaves.
        /// </summary>
        [TestMethod]
        public void ThePlacesAreWalkedToOnTheClientsFloor()
        {
            var dataDir = F.ClientData();
            using var harness = WildernessRuntimeTestHarness.Create();
            using var meshes = new MeshLibrary(dataDir, F.MeshCsv());
            var geometry = MapGeometry.Load(Path.Combine(dataDir, "maps", MapName(harness, Map)), meshes, 1);
            // Fix J: the east pool keeps the walk clearance (to-fix row 36); only the west one stands on the way in.
            var hostile = HostilePools(harness).Where(pool => pool.Id != N.WatchPostWest).ToList();
            var failures = new List<string>();
            var walked = 0;
            var expected = 0;
            foreach (var (leg, corners) in Legs)
            {
                // Each leg, there and back, as given and with its start moved StartShifts along its first stretch (the way back
                // from the last corner along the last stretch): the samples (every 0.5 m) fall elsewhere, so a step the samples
                // happen to straddle is not missed (fix U: "Diren to the Watch Post" stepped 1.1 m and more at (-26, -353) at
                // +0.13 to +0.4 m).
                foreach (var (given, direction) in new[] { (corners.ToList(), "there"), (Enumerable.Reverse(corners).ToList(), "back") })
                {
                    var first = Vector2.Distance(new(given[0].X, given[0].Z), new(given[1].X, given[1].Z));
                    foreach (var shift in StartShifts.Where(shift => shift == 0 || first > shift + 0.05f))
                    {
                        expected++;
                        walked++;
                        var line = given.ToList();
                        line[0] = Vector3.Lerp(given[0], given[1], shift / first);
                        if (AshenFixBTests.ClientFloorLine(geometry, line, line[^1], null, AshenFormalComplaintsTests.PlayerHalfWidth) is { } why)
                            failures.Add(FormattableString.Invariant($"{leg} {direction} (start +{shift} m): {why}"));
                    }
                }
            }
            foreach (var (label, legs, own, door) in Walks())
            {
                var line = Line(legs);
                if (WO.Clearance(line, hostile.Where(pool => pool.Id != own).ToList()) is { } close)
                    failures.Add(FormattableString.Invariant($"{label}: {close.Gap:0.0} m outside hostile pool {close.Pool}'s circle at ({close.At.X:0.0}, {close.At.Y:0.0})"));
                if (door is { } shut && !Crosses(line, shut.At, shut.Yaw, shut.Leaf, shut.Half))
                    failures.Add($"{label} does not cross its door's leaf");
            }
            var ends = new (string Walk, Vector3 Target)[]
            {
                ("Diren from the arrival", V(N.DirenAt)), ("Bendal from the arrival", ToVector(AshenFixIV1.BendalAt)),
                ("Aksrah from the facility door", ToVector(Vindication.AksrahAt)), ("Kuutk from the facility door", V(N.KuutkAt)),
                ("Drof from the facility door", V(N.DrofAt)), ("before Naimi's cell from the facility door", V(Vindication.CellMarkerAt)),
                ("the Supply Crate from the facility door", ToVector(Vindication.CrateAt))
            };
            foreach (var (walk, target) in ends)
                Assert.IsLessThan(0.01f, Vector3.Distance(Line(Walks().Single(entry => entry.Label == walk).Legs)[^1], target), $"{walk} ends at its target");
            Assert.AreEqual(V(N.ArrivalAt).X, Legs["the arrival to Diren"][0].X, 0.01f, "from the arrival");
            Assert.AreEqual(expected, walked, "every leg both ways, at each start shift");
            Assert.IsGreaterThanOrEqualTo(2 * Legs.Count * 3, walked, "most legs at three shifts or more");
            Assert.HasCount(8, Walks());
            Assert.HasCount(11, Legs);
            Assert.IsEmpty(failures, $"{failures.Count}: " + string.Join("; ", failures));
        }

        /// <summary>
        /// The door walk on Ashen Desert: from fix H's island pad 914 (AshenFixHV1; set 13's old Odesha place, which the valley
        /// pad 913 by Odesha takes a player to: the island has no walk-in, upstream N23, to-fix row 33) east by (278, 337),
        /// (286, 331), (293, 324), (306, 322) into the door's circle (map link 58).
        /// </summary>
        internal static readonly Vector3[] DoorWalk =
        {
            AshenFixHTests.UpperPad, new(278f, 318.87f, 337f), new(286f, 318.31f, 331f), new(293f, 319.86f, 324f),
            new(306f, 318.97f, 322f), new(319f, 320.15f, 320f), new(320f, 319.1f, 320.5f)
        };

        /// <summary>
        /// 1559's way in on Ashen Desert: from fix H's island pad (DoorWalk) to inside the Indra Caverns door's circle (map link
        /// 58), there and back on the client's floor (AshenFixBTests.ClientFloorLine with a player's half width), past 1559.1's
        /// marker before the door, HostileClearance (30 m, 2D) outside every hostile Ashen Desert pool's circle. The Ashen navmesh
        /// does not join the door's pocket (upstream N26), so the guard's navmesh walk (AshenGuardTests.InTheDoorPocket) is this
        /// one instead. Set 15 started it at set 13's old Odesha place, which fix G had moved off (to-fix row 33).
        /// </summary>
        [TestMethod]
        public void TheCavernsDoorIsWalkedToFromTheUpperPad()
        {
            var dataDir = F.ClientData();
            using var harness = WildernessRuntimeTestHarness.Create();
            using var meshes = new MeshLibrary(dataDir, F.MeshCsv());
            var geometry = MapGeometry.Load(Path.Combine(dataDir, "maps", MapName(harness, AshenMissionDataV1.MapContextId)), meshes, 1);
            var line = DoorWalk.ToList();
            var failures = new List<string>();
            var back = Enumerable.Reverse(line).ToList();
            foreach (var (walk, direction) in new[] { (line, "there"), (back, "back") })
                if (AshenFixBTests.ClientFloorLine(geometry, walk, walk[^1], null, AshenFormalComplaintsTests.PlayerHalfWidth) is { } why)
                    failures.Add($"{direction}: {why}");
            if (WO.Clearance(line, WO.HostilePools(harness)) is { } close)
                failures.Add(FormattableString.Invariant($"{close.Gap:0.0} m outside hostile pool {close.Pool}'s circle at ({close.At.X:0.0}, {close.At.Y:0.0})"));
            var link = harness.World.Set<MapLinkEntry>().AsNoTracking().Single(entry => entry.MapContextId == AshenMissionDataV1.MapContextId && entry.DestMapContextId == Map);
            Assert.IsLessThan((float)link.Radius, Vector2.Distance(new(line[^1].X, line[^1].Z), new((float)link.PosX, (float)link.PosZ)), "the walk ends in the door's circle");
            var marker = V(Caverns.DoorMarkerAt);
            Assert.IsTrue(Enumerable.Range(0, line.Count - 1).Any(k => Seg(line[k], line[k + 1], marker) <= (float)Caverns.DoorMarkerRadius), "the walk passes 1559.1's marker");
            Assert.IsEmpty(failures, $"{failures.Count}: " + string.Join("; ", failures));

            static float Seg(Vector3 a, Vector3 b, Vector3 p)
            {
                var a2 = new Vector2(a.X, a.Z); var ab = new Vector2(b.X, b.Z) - a2; var p2 = new Vector2(p.X, p.Z);
                var t = Math.Clamp(Vector2.Dot(p2 - a2, ab) / Math.Max(ab.LengthSquared(), 1e-6f), 0, 1);
                return Vector2.Distance(p2, a2 + ab * t);
            }
        }

        /// <summary>
        /// Indra Caverns: Odesha offers it after Missing Scientists only; accepted, 1 is open with its marker before the door on
        /// Ashen Desert, 2 waits. Through the door the arrival completes 1 and opens 2 (its marker on Diren, on Indra Caverns);
        /// a walk to Diren completes 2, the mission is completeable; a reload keeps it; the turn-in at Diren pays the stack chosen
        /// once; done once.
        /// </summary>
        [TestMethod]
        public void IndraCavernsFindsDirenJustInside()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var desert = AshenLeadInsTests.EnterAshen(harness, ToVector(AshenIndraPassNpcsV1.OdeshaAt));
            var odesha = Npc(desert, AshenIndraPassNpcsV1.Odesha);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, odesha, Caverns.MissionId), "not before Missing Scientists");
            AshenRuntime.SeedCompleted(harness, AshenMissingScientistsV1.MissionId);
            odesha = Npc(desert, AshenIndraPassNpcsV1.Odesha);
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, odesha, Caverns.MissionId), "Odesha offers 1559");
            var offer = WildernessRanjaGorgeTests.Offer(harness, odesha, Caverns.MissionId);
            CollectionAssert.AreEqual(new[] { (Caverns.ConcussionGrenade, 3U), (Caverns.BasicMedPack, 2U), (Caverns.FragGrenade, 3U), (Caverns.EmpBomb, 1U) },
                offer.SelectableReward.Select(item => (item.ItemTemplateId, item.Quantity)).ToArray(), "the four stacks");
            Accept(harness, odesha, Caverns.MissionId);
            Assert.AreEqual((MissionObjectiveState.Incomplete, MissionObjectiveState.Inactive),
                (Objective(harness, Caverns.MissionId, Caverns.Locate), Objective(harness, Caverns.MissionId, Caverns.FindDiren)));

            var copy = IntoCaverns(harness);
            Assert.IsTrue(harness.Client.MissionAreaService.RecordArrival(harness.Client), "arrived, as ManifestationManager.MissionArrival tells it");
            harness.Drain();
            Assert.AreEqual((MissionObjectiveState.Completed, MissionObjectiveState.Incomplete),
                (Objective(harness, Caverns.MissionId, Caverns.Locate), Objective(harness, Caverns.MissionId, Caverns.FindDiren)), "the arrival: 1 done, 2 open");
            var diren = Giver(copy, N.Diren);
            var arrival = harness.Client.Player.Position;
            harness.MoveTo(diren.Position + new Vector3(2, 0, 0));
            Assert.IsTrue(harness.Client.MissionAreaService.RecordAcceptedMovement(harness.Client, arrival, diren.Position + new Vector3(2, 0, 0)), "Diren's area");
            harness.Drain();
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Caverns.MissionId, Caverns.FindDiren));
            Assert.IsTrue(harness.Client.Player.Missions[Caverns.MissionId].Completeable, "to be turned in");
            ReloadAndRecover(harness, copy);
            Assert.IsTrue(harness.Client.Player.Missions[Caverns.MissionId].Completeable, "kept at a reload");
            diren = Giver(copy, N.Diren);
            FailedTurnInPaysNothing(harness, diren, Caverns.MissionId, 0);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, diren, Caverns.MissionId, 0, Caverns.Experience, Caverns.Credits);
            Assert.AreEqual(3U, WildernessRanjaGorgeTests.Held(harness, Caverns.ConcussionGrenade), "the three grenades");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, Giver(copy, N.Diren), Threshold.MissionId), "Diren offers Threshold next");
        }

        /// <summary>
        /// Threshold: Diren offers it in the squad's copy after Indra Caverns; at the accept the Supply Package comes, the player's
        /// own Bendal and facility door stand (closed, locked to the key card, not shown to another player), 1 and 3 open, 2
        /// waits, the helpers 4-6 reach the client in no form. The door is refused without the card (PmMissingReqItem). Bendal's
        /// search gives the card and completes 1, once; a reload and a restart keep the card and the door shut. The door opens
        /// with the card, takes it, is used no more, stays open at a reload and a restart. The Watch Post's two Thrax killed
        /// complete 3; 2 opens; Drof hears 2 (1360's, not 1395's) and takes the package; the turn-in at Drof pays once; the door
        /// and Bendal go with the mission.
        /// </summary>
        [TestMethod]
        public void ThresholdOpensTheFacility()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            AshenRuntime.SeedCompleted(harness, AshenMissingScientistsV1.MissionId);
            var copy = IntoCaverns(harness);
            var diren = Giver(copy, N.Diren);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, diren, Threshold.MissionId), "not before Indra Caverns");
            AshenRuntime.SeedCompleted(harness, Caverns.MissionId);
            diren = Giver(copy, N.Diren);
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, diren, Threshold.MissionId), "Diren offers 1360");
            Accept(harness, diren, Threshold.MissionId);
            Tick(harness, copy, 250);
            Assert.AreEqual(1U, Held(harness, Threshold.PackageTemplate), "the Supply Package");
            Assert.AreEqual((MissionObjectiveState.Incomplete, MissionObjectiveState.Inactive, MissionObjectiveState.Incomplete, MissionObjectiveState.Incomplete),
                (Objective(harness, Threshold.MissionId, Threshold.FindBendal), Objective(harness, Threshold.MissionId, Threshold.FindReconTeam),
                    Objective(harness, Threshold.MissionId, Threshold.WatchPost), Objective(harness, Threshold.MissionId, Threshold.DoorUsed)));
            var visible = harness.Manager.BuildStatusSnapshot(harness.Client.Player)[Threshold.MissionId].ObjectivesList.Select(objective => objective.ObjectiveId).ToArray();
            foreach (var hidden in new[] { Threshold.DoorUsed, Threshold.Tracker, Threshold.Opener })
                Assert.IsFalse(visible.Contains(hidden), $"{hidden} is hidden");

            var door = SceneObject(harness, copy, Threshold.MissionId, Threshold.DoorRole);
            // Fix I: the entrance door (23160) at the Security Check Point entrance's door place, turned as the entrance.
            Assert.AreEqual((AshenFixIV1.DoorClass, UseObjectState.DoorStateClosed, Threshold.CardTemplate, ToVector(AshenFixIV1.DoorAt), harness.Client.Player.Id),
                ((uint)door.EntityClassId, door.StateId, door.Lock?.KeyItemTemplateId ?? 0, door.Position, door.SceneOwnerCharacterId), "the player's own door, locked to the card");
            Assert.AreEqual(AshenFixIV1.DoorOrientation, door.Rotation, 1e-6, "the door turned as the entrance");
            var bendal = SceneObject(harness, copy, Threshold.MissionId, Threshold.CorpseRole);
            Assert.AreEqual((Threshold.CorpseClass, (UseObjectState)Threshold.CorpseClosed, ToVector(AshenFixIV1.BendalAt)), ((uint)bendal.EntityClassId, bendal.StateId, bendal.Position));
            var other = harness.Context.CreateAdditionalClient(2, manager: harness.Manager);
            try
            {
                Assert.IsFalse(DynamicObjectManager.IsShownTo(other, door), "another player is not shown the door");
            }
            finally
            {
                CellManager.Instance.RemoveFromWorld(other);
                other.Player.MapChannel?.ClientList.Remove(other);
            }
            Assert.AreEqual(PlayerMessage.PmMissingReqItem, Refused(harness, copy, door, Threshold.UseArgument), "no card: refused");

            // 1: Bendal searched.
            Use(harness, copy, bendal, Threshold.UseArgument);
            Tick(harness, copy, 250);
            Assert.AreEqual((1U, MissionObjectiveState.Completed, (UseObjectState)Threshold.CorpseOpened, false),
                (Held(harness, Threshold.CardTemplate), Objective(harness, Threshold.MissionId, Threshold.FindBendal),
                    SceneObject(harness, copy, Threshold.MissionId, Threshold.CorpseRole).StateId, SceneObject(harness, copy, Threshold.MissionId, Threshold.CorpseRole).IsEnabled),
                "the card, 1 done, the body searched");
            Assert.AreEqual(MissionObjectiveState.Inactive, Objective(harness, Threshold.MissionId, Threshold.FindReconTeam), "2 waits for 3");
            ReloadAndRecover(harness, copy);
            Assert.AreEqual((1U, UseObjectState.DoorStateClosed), (Held(harness, Threshold.CardTemplate),
                SceneObject(harness, copy, Threshold.MissionId, Threshold.DoorRole).StateId), "the card held and the door shut at a reload");
            Restart(harness, copy, Threshold.MissionId);
            Assert.AreEqual((1U, UseObjectState.DoorStateClosed, (UseObjectState)Threshold.CorpseOpened), (Held(harness, Threshold.CardTemplate),
                SceneObject(harness, copy, Threshold.MissionId, Threshold.DoorRole).StateId, SceneObject(harness, copy, Threshold.MissionId, Threshold.CorpseRole).StateId),
                "the card held, the door shut, Bendal searched at a restart");

            // The door: opens with the card and takes it.
            Use(harness, copy, SceneObject(harness, copy, Threshold.MissionId, Threshold.DoorRole), Threshold.UseArgument);
            Tick(harness, copy, 250);
            door = SceneObject(harness, copy, Threshold.MissionId, Threshold.DoorRole);
            Assert.AreEqual((UseObjectState.DoorStateOpen, false, 0U, MissionObjectiveState.Completed),
                (door.StateId, door.IsEnabled, Held(harness, Threshold.CardTemplate), Objective(harness, Threshold.MissionId, Threshold.DoorUsed)), "open, the card taken");
            ReloadAndRecover(harness, copy);
            Assert.AreEqual(UseObjectState.DoorStateOpen, SceneObject(harness, copy, Threshold.MissionId, Threshold.DoorRole).StateId, "open at a reload");
            Restart(harness, copy, Threshold.MissionId);
            door = SceneObject(harness, copy, Threshold.MissionId, Threshold.DoorRole);
            Assert.AreEqual((UseObjectState.DoorStateOpen, false), (door.StateId, door.IsEnabled), "open, used no more, at a restart");

            // 3: the Watch Post.
            KillWatchPost(harness, copy, N.WatchPostWest);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Threshold.MissionId, Threshold.WatchPost), "one of two");
            KillWatchPost(harness, copy, N.WatchPostEast);
            Tick(harness, copy, 250);
            Assert.AreEqual((MissionObjectiveState.Completed, MissionObjectiveState.Incomplete),
                (Objective(harness, Threshold.MissionId, Threshold.WatchPost), Objective(harness, Threshold.MissionId, Threshold.FindReconTeam)), "3 done, 2 open");

            // 2: Drof.
            var drof = Giver(copy, N.Drof);
            CollectionAssert.AreEqual(new[] { Threshold.FindReconTeam }, Topics(harness, drof, Threshold.MissionId), "Drof hears 2");
            Assert.IsEmpty(Topics(harness, drof, Vindication.MissionId), "and nothing of 1395 (not held)");
            Talk(harness, drof, Threshold.MissionId, Threshold.FindReconTeam);
            Tick(harness, copy, 250);
            Assert.AreEqual((0U, true), (Held(harness, Threshold.PackageTemplate), harness.Client.Player.Missions[Threshold.MissionId].Completeable), "the package taken");
            drof = Giver(copy, N.Drof);
            FailedTurnInPaysNothing(harness, drof, Threshold.MissionId, 0);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, drof, Threshold.MissionId, 0, Threshold.Experience, Threshold.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Threshold.MissionId), "MissionRewarded once");
            for (var tick = 0; tick < 4; tick++)        // RetireRewardedScene retries: up to 1 s
                Tick(harness, copy, 250);
            foreach (var role in new[] { Threshold.DoorRole, Threshold.CorpseRole })
                Assert.IsNull(FindSceneObject(harness, copy, Threshold.MissionId, role), $"{role} goes with the mission");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, Giver(copy, N.Drof), Vindication.MissionId), "Drof offers Vindication next");
        }

        /// <summary>
        /// The Watch Post's Thrax killed before the accept count (an existing fact: TaRapedia's "do NOT kill ahead" the server's
        /// pools can not keep, docs/ashen-playtest.md): with both dead, 3 is done at the accept, 2 waits for 1.
        /// </summary>
        [TestMethod]
        public void ThresholdCountsTheWatchPostKilledAhead()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            AshenRuntime.SeedCompleted(harness, AshenMissingScientistsV1.MissionId, Caverns.MissionId);
            var copy = IntoCaverns(harness);
            KillWatchPost(harness, copy, N.WatchPostWest);
            KillWatchPost(harness, copy, N.WatchPostEast);
            Accept(harness, Giver(copy, N.Diren), Threshold.MissionId);
            Tick(harness, copy, 250);
            Assert.AreEqual((MissionObjectiveState.Completed, MissionObjectiveState.Incomplete, MissionObjectiveState.Inactive),
                (Objective(harness, Threshold.MissionId, Threshold.WatchPost), Objective(harness, Threshold.MissionId, Threshold.FindBendal),
                    Objective(harness, Threshold.MissionId, Threshold.FindReconTeam)), "3 done at the accept, 2 waits for 1");
        }

        /// <summary>
        /// Threshold in a squad of two on it (GroupCreditService.SharedInSquadInstance): the player's search of the player's
        /// Bendal completes the mate's 1 too (a scene event) and gives each of them a card of his own (1's action, as Crucible
        /// 1401's cards); the player's kills of the Watch Post complete the mate's 3; the player's use of the player's door
        /// completes the mate's 4 too: each one's card taken, each one's own door open, so the mate is not left behind it; the
        /// mate's 2 opens. A member without 1360 is shown neither door and gets no card.
        /// </summary>
        [TestMethod]
        public void ThresholdsDoorOpensForEverySquadMateOnIt()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            AshenRuntime.SeedCompleted(harness, AshenMissingScientistsV1.MissionId, Caverns.MissionId);
            var copy = IntoCaverns(harness);
            var diren = Giver(copy, N.Diren);
            Accept(harness, diren, Threshold.MissionId);
            Tick(harness, copy, 250);
            using var mate = new SquadMate(harness, copy, diren, Threshold.MissionId, new[] { Caverns.MissionId });
            using var stranger = new SquadMate(harness, copy, diren, 0, Array.Empty<uint>(), 3, mate.PartyId);
            Assert.AreEqual(1U, mate.Held(Threshold.PackageTemplate), "the mate's own package");
            Assert.IsFalse(DynamicObjectManager.IsShownTo(stranger.Client, SceneObject(harness, copy, Threshold.MissionId, Threshold.DoorRole)), "a member without 1360: no door");

            Use(harness, copy, SceneObject(harness, copy, Threshold.MissionId, Threshold.CorpseRole), Threshold.UseArgument);
            Tick(harness, copy, 250);
            Assert.AreEqual((MissionObjectiveState.Completed, 1U, 1U, 0U), (mate.Of(Threshold.FindBendal), Held(harness, Threshold.CardTemplate),
                mate.Held(Threshold.CardTemplate), stranger.Held(Threshold.CardTemplate)), "the mate's 1 done by the player's search; a card each on 1360");
            Assert.AreEqual(UseObjectState.DoorStateClosed, mate.Door(Threshold.DoorRole).StateId, "the mate's door shut");
            Assert.AreEqual((UseObjectState)Threshold.CorpseClosed, mate.Door(Threshold.CorpseRole).StateId, "the mate's Bendal unsearched");

            KillWatchPost(harness, copy, N.WatchPostWest);
            KillWatchPost(harness, copy, N.WatchPostEast);
            Tick(harness, copy, 250);
            Assert.AreEqual((MissionObjectiveState.Completed, MissionObjectiveState.Incomplete), (mate.Of(Threshold.WatchPost), mate.Of(Threshold.FindReconTeam)),
                "the player's kills: the mate's 3 done, 2 open");

            Use(harness, copy, SceneObject(harness, copy, Threshold.MissionId, Threshold.DoorRole), Threshold.UseArgument);
            for (var tick = 0; tick < 2; tick++)
                Tick(harness, copy, 250);
            Assert.AreEqual((UseObjectState.DoorStateOpen, 0U), (SceneObject(harness, copy, Threshold.MissionId, Threshold.DoorRole).StateId, Held(harness, Threshold.CardTemplate)),
                "the player's door open, the card taken");
            Assert.AreEqual((UseObjectState.DoorStateOpen, false, MissionObjectiveState.Completed, 0U),
                (mate.Door(Threshold.DoorRole).StateId, mate.Door(Threshold.DoorRole).IsEnabled, mate.Of(Threshold.DoorUsed), mate.Held(Threshold.CardTemplate)),
                "the mate's own door opens with the player's use, the mate's card taken");
        }

        /// <summary>
        /// The audit's lock-out (7bf850aa): a mate away from the copy (not on its client list) at the player's door use keeps his
        /// card and his shut door, his 4 open; back, his own door opens with his own card. A mate away at the player's search
        /// finds his own Bendal unsearched and his 1 open: his own search gives his card.
        /// </summary>
        [TestMethod]
        public void ThresholdsMateAwayAtTheDoorGetsInWithHisOwnCard()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            AshenRuntime.SeedCompleted(harness, AshenMissingScientistsV1.MissionId, Caverns.MissionId);
            var copy = IntoCaverns(harness);
            var diren = Giver(copy, N.Diren);
            Accept(harness, diren, Threshold.MissionId);
            Tick(harness, copy, 250);
            using var mate = new SquadMate(harness, copy, diren, Threshold.MissionId, new[] { Caverns.MissionId });
            using var late = new SquadMate(harness, copy, diren, Threshold.MissionId, new[] { Caverns.MissionId }, 3, mate.PartyId);

            late.Away();
            Use(harness, copy, SceneObject(harness, copy, Threshold.MissionId, Threshold.CorpseRole), Threshold.UseArgument);
            Tick(harness, copy, 250);
            Assert.AreEqual((1U, 1U), (Held(harness, Threshold.CardTemplate), mate.Held(Threshold.CardTemplate)), "a card each for those here");
            late.Back();
            Assert.AreEqual((MissionObjectiveState.Incomplete, 0U, (UseObjectState)Threshold.CorpseClosed),
                (late.Of(Threshold.FindBendal), late.Held(Threshold.CardTemplate), late.Door(Threshold.CorpseRole).StateId), "away at the search: his 1 open, his Bendal unsearched");
            late.Use(late.Door(Threshold.CorpseRole));
            Assert.AreEqual((MissionObjectiveState.Completed, 1U), (late.Of(Threshold.FindBendal), late.Held(Threshold.CardTemplate)), "his own search, his own card");

            mate.Away();
            late.Away();
            Use(harness, copy, SceneObject(harness, copy, Threshold.MissionId, Threshold.DoorRole), Threshold.UseArgument);
            for (var tick = 0; tick < 2; tick++)
                Tick(harness, copy, 250);
            Assert.AreEqual((UseObjectState.DoorStateOpen, 0U), (SceneObject(harness, copy, Threshold.MissionId, Threshold.DoorRole).StateId, Held(harness, Threshold.CardTemplate)),
                "the player's door open, the player's card taken");
            mate.Back();
            late.Back();
            foreach (var (member, name) in new[] { (mate, "mate"), (late, "late mate") })
                Assert.AreEqual((MissionObjectiveState.Incomplete, UseObjectState.DoorStateClosed, 1U),
                    (member.Of(Threshold.DoorUsed), member.Door(Threshold.DoorRole).StateId, member.Held(Threshold.CardTemplate)), $"{name}: away at the use, his door shut, his card kept");
            // Each in turn, the other away (a use here shares too: the one here gets in with it).
            foreach (var (member, other, name) in new[] { (mate, late, "mate"), (late, mate, "late mate") })
            {
                other.Away();
                member.Use(member.Door(Threshold.DoorRole));
                other.Back();
                Assert.AreEqual((MissionObjectiveState.Completed, UseObjectState.DoorStateOpen, 0U),
                    (member.Of(Threshold.DoorUsed), member.Door(Threshold.DoorRole).StateId, member.Held(Threshold.CardTemplate)), $"{name}: his own card opens his own door");
            }
        }

        /// <summary>
        /// The card holder leaves: the player searches Bendal (a card each) and abandons 1360; the mate keeps his card, his 1
        /// done, and his own door opens with it.
        /// </summary>
        [TestMethod]
        public void ThresholdsMateGetsInWhenTheCardHolderAbandons()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            AshenRuntime.SeedCompleted(harness, AshenMissingScientistsV1.MissionId, Caverns.MissionId);
            var copy = IntoCaverns(harness);
            var diren = Giver(copy, N.Diren);
            Accept(harness, diren, Threshold.MissionId);
            Tick(harness, copy, 250);
            using var mate = new SquadMate(harness, copy, diren, Threshold.MissionId, new[] { Caverns.MissionId });
            Use(harness, copy, SceneObject(harness, copy, Threshold.MissionId, Threshold.CorpseRole), Threshold.UseArgument);
            Tick(harness, copy, 250);
            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Threshold.MissionId));
            harness.Drain();
            for (var tick = 0; tick < 4; tick++)
                Tick(harness, copy, 250);
            Assert.AreEqual((0U, 0U), (Held(harness, Threshold.CardTemplate), Held(harness, Threshold.PackageTemplate)), "the player's card and package taken at the abandon");
            Assert.AreEqual((MissionObjectiveState.Completed, 1U, UseObjectState.DoorStateClosed),
                (mate.Of(Threshold.FindBendal), mate.Held(Threshold.CardTemplate), mate.Door(Threshold.DoorRole).StateId), "the mate's 1, card and shut door stay");
            mate.Use(mate.Door(Threshold.DoorRole));
            Assert.AreEqual((MissionObjectiveState.Completed, UseObjectState.DoorStateOpen, 0U),
                (mate.Of(Threshold.DoorUsed), mate.Door(Threshold.DoorRole).StateId, mate.Held(Threshold.CardTemplate)), "the mate's own card opens his own door");
        }

        /// <summary>
        /// An abandon of 1360 inside the facility (door open): the door and Bendal go, the package goes (the card was spent);
        /// taken again at Diren the package comes again, the Watch Post cleared counts at once (an existing fact), a new
        /// Bendal and a new shut door stand; the search gives a new card, the new door opens with it; Drof hears 2.
        /// </summary>
        [TestMethod]
        public void ThresholdAbandonedInsideTheFacility()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            AshenRuntime.SeedCompleted(harness, AshenMissingScientistsV1.MissionId, Caverns.MissionId);
            var copy = IntoCaverns(harness);
            Accept(harness, Giver(copy, N.Diren), Threshold.MissionId);
            Tick(harness, copy, 250);
            KillWatchPost(harness, copy, N.WatchPostWest);
            KillWatchPost(harness, copy, N.WatchPostEast);
            Use(harness, copy, SceneObject(harness, copy, Threshold.MissionId, Threshold.CorpseRole), Threshold.UseArgument);
            Tick(harness, copy, 250);
            Use(harness, copy, SceneObject(harness, copy, Threshold.MissionId, Threshold.DoorRole), Threshold.UseArgument);
            Tick(harness, copy, 250);
            Assert.AreEqual(UseObjectState.DoorStateOpen, SceneObject(harness, copy, Threshold.MissionId, Threshold.DoorRole).StateId, "inside");
            harness.MoveTo(V(N.DrofAt) + new Vector3(0, 0, 3));
            harness.Drain();

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Threshold.MissionId));
            harness.Drain();
            for (var tick = 0; tick < 4; tick++)
                Tick(harness, copy, 250);
            foreach (var role in new[] { Threshold.DoorRole, Threshold.CorpseRole })
                Assert.IsNull(FindSceneObject(harness, copy, Threshold.MissionId, role), $"{role} goes with the abandon");
            Assert.AreEqual((0U, 0U), (Held(harness, Threshold.PackageTemplate), Held(harness, Threshold.CardTemplate)), "the package taken");
            Assert.IsEmpty(Topics(harness, Giver(copy, N.Drof), Threshold.MissionId), "Drof hears nothing of an abandoned 1360");

            Accept(harness, Giver(copy, N.Diren), Threshold.MissionId);
            Tick(harness, copy, 250);
            Assert.AreEqual((1U, MissionObjectiveState.Completed, MissionObjectiveState.Incomplete, UseObjectState.DoorStateClosed, (UseObjectState)Threshold.CorpseClosed),
                (Held(harness, Threshold.PackageTemplate), Objective(harness, Threshold.MissionId, Threshold.WatchPost), Objective(harness, Threshold.MissionId, Threshold.FindBendal),
                    SceneObject(harness, copy, Threshold.MissionId, Threshold.DoorRole).StateId, SceneObject(harness, copy, Threshold.MissionId, Threshold.CorpseRole).StateId),
                "taken again: the package, 3 at once, a new Bendal and a shut door");
            Use(harness, copy, SceneObject(harness, copy, Threshold.MissionId, Threshold.CorpseRole), Threshold.UseArgument);
            Tick(harness, copy, 250);
            Use(harness, copy, SceneObject(harness, copy, Threshold.MissionId, Threshold.DoorRole), Threshold.UseArgument);
            Tick(harness, copy, 250);
            Assert.AreEqual((UseObjectState.DoorStateOpen, 0U, MissionObjectiveState.Incomplete),
                (SceneObject(harness, copy, Threshold.MissionId, Threshold.DoorRole).StateId, Held(harness, Threshold.CardTemplate),
                    Objective(harness, Threshold.MissionId, Threshold.FindReconTeam)), "the new card opens the new door; 2 open");
            CollectionAssert.AreEqual(new[] { Threshold.FindReconTeam }, Topics(harness, Giver(copy, N.Drof), Threshold.MissionId), "Drof hears 2");
        }

        /// <summary>
        /// A full mission-item bag at Bendal's search: no card, the body waits (closed, usable), 1 open; one slot freed, the
        /// search gives the card and completes 1.
        /// </summary>
        [TestMethod]
        public void ThresholdsFullBagLeavesTheCardOnBendal()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            AshenRuntime.SeedCompleted(harness, AshenMissingScientistsV1.MissionId, Caverns.MissionId);
            var copy = IntoCaverns(harness);
            Accept(harness, Giver(copy, N.Diren), Threshold.MissionId);
            Tick(harness, copy, 250);
            FullBagWaitsAtASearch(harness, copy, Threshold.MissionId, Threshold.CorpseRole, Threshold.CardTemplate, Threshold.FindBendal,
                (UseObjectState)Threshold.CorpseClosed, (UseObjectState)Threshold.CorpseOpened, Threshold.UseArgument);
        }

        /// <summary>
        /// A full mission-item bag at Aksrah's and the crate's searches: no item, the object waits, its objective open; one slot
        /// freed, the search gives the item and completes it.
        /// </summary>
        [TestMethod]
        public void VindicationsFullBagWaitsAtAksrahAndTheCrate()
        {
            using var harness = WildernessRuntimeTestHarness.Create(lootRoll: (minimum, maximum) => maximum == 100 ? 0 : minimum);
            AshenRuntime.SeedCompleted(harness, AshenMissingScientistsV1.MissionId, Caverns.MissionId, Threshold.MissionId);
            var copy = IntoCaverns(harness);
            Accept(harness, Giver(copy, N.Drof), Vindication.MissionId);
            Tick(harness, copy, 250);
            FullBagWaitsAtASearch(harness, copy, Vindication.MissionId, Vindication.CorpseRole, Vindication.RadioTemplate, Vindication.FindAksrah,
                (UseObjectState)Vindication.ChestClosed, (UseObjectState)Vindication.ChestOpened, Vindication.UseArgument);
            Stand(copy, N.KuutkPool);
            PickUp(harness, Kill(harness, copy, N.Kuutk));
            harness.Drain();
            Use(harness, copy, SceneObject(harness, copy, Vindication.MissionId, Vindication.CellRole), Vindication.UseArgument);
            Tick(harness, copy, 250);
            Talk(harness, Giver(copy, N.Naimi), Vindication.MissionId, Vindication.FindNaimi);
            Tick(harness, copy, 250);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Vindication.MissionId, Vindication.PowerSupply), "5 open");
            FullBagWaitsAtASearch(harness, copy, Vindication.MissionId, Vindication.CrateRole, Vindication.PowerSupplyTemplate, Vindication.PowerSupply,
                (UseObjectState)Vindication.ChestClosed, (UseObjectState)Vindication.ChestOpened, Vindication.UseArgument);
        }

        /// <summary>
        /// Vindication: Drof offers it after Threshold; at the accept the Key Pass comes and the player's own three room doors
        /// (locked to the key pass), Aksrah, the crate and Naimi's cell (locked to the Thrax Hand) stand; 2, 4 and 6 open, 3 and 5
        /// wait. Each room door opens with the key pass and keeps it. Aksrah's search gives the radio, 2 done. The cell is
        /// refused without the hand; Kuutk's kill completes 4 and drops the hand (a death is not a pickup); the pickup; a reload
        /// with the hand held keeps it; the cell opens with the hand, takes it, 6 done, 3 open; open at a restart. Naimi hears
        /// 3 and opens 5; the crate gives the power supply, 5 done; the turn-in at Drof pays once, and takes the key pass, the
        /// radio and the power supply; the doors go with the mission.
        /// </summary>
        [TestMethod]
        public void VindicationFreesNaimi()
        {
            using var harness = WildernessRuntimeTestHarness.Create(lootRoll: (minimum, maximum) => maximum == 100 ? 99 : minimum);
            AshenRuntime.SeedCompleted(harness, AshenMissingScientistsV1.MissionId, Caverns.MissionId);
            var copy = IntoCaverns(harness);
            var drof = Giver(copy, N.Drof);
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, drof, Vindication.MissionId), "not before Threshold");
            AshenRuntime.SeedCompleted(harness, Threshold.MissionId);
            drof = Giver(copy, N.Drof);
            Accept(harness, drof, Vindication.MissionId);
            Tick(harness, copy, 250);
            Assert.AreEqual(1U, Held(harness, Vindication.KeyPassTemplate), "the key pass");
            Assert.AreEqual((MissionObjectiveState.Incomplete, MissionObjectiveState.Inactive, MissionObjectiveState.Incomplete, MissionObjectiveState.Inactive,
                    MissionObjectiveState.Incomplete),
                (Objective(harness, Vindication.MissionId, Vindication.FindAksrah), Objective(harness, Vindication.MissionId, Vindication.FindNaimi),
                    Objective(harness, Vindication.MissionId, Vindication.DefeatKuutk), Objective(harness, Vindication.MissionId, Vindication.PowerSupply),
                    Objective(harness, Vindication.MissionId, Vindication.FreeThePrisoner)));
            var visible = harness.Manager.BuildStatusSnapshot(harness.Client.Player)[Vindication.MissionId].ObjectivesList.Select(objective => objective.ObjectiveId).ToArray();
            foreach (var hidden in new[] { Vindication.KeyPassGate, Vindication.HandTaken })
                Assert.IsFalse(visible.Contains(hidden), $"{hidden} is hidden");

            foreach (var (role, at, yaw) in Vindication.Doors)
            {
                var door = SceneObject(harness, copy, Vindication.MissionId, role);
                Assert.AreEqual((Vindication.DoorClass, UseObjectState.DoorStateClosed, Vindication.KeyPassTemplate, ToVector(at), harness.Client.Player.Id),
                    ((uint)door.EntityClassId, door.StateId, door.Lock?.KeyItemTemplateId ?? 0, door.Position, door.SceneOwnerCharacterId), role);
                Use(harness, copy, door, Vindication.UseArgument);
                Tick(harness, copy, 250);
                door = SceneObject(harness, copy, Vindication.MissionId, role);
                Assert.AreEqual((UseObjectState.DoorStateOpen, false, 1U), (door.StateId, door.IsEnabled, Held(harness, Vindication.KeyPassTemplate)), $"{role}: open, the pass kept");
            }

            // 2: Aksrah.
            Use(harness, copy, SceneObject(harness, copy, Vindication.MissionId, Vindication.CorpseRole), Vindication.UseArgument);
            Tick(harness, copy, 250);
            Assert.AreEqual((1U, MissionObjectiveState.Completed), (Held(harness, Vindication.RadioTemplate), Objective(harness, Vindication.MissionId, Vindication.FindAksrah)), "the radio, 2 done");

            // 4 and the hand.
            var cell = SceneObject(harness, copy, Vindication.MissionId, Vindication.CellRole);
            Assert.AreEqual((Vindication.CellClass, UseObjectState.DoorStateClosed, Vindication.HandTemplate), ((uint)cell.EntityClassId, cell.StateId, cell.Lock?.KeyItemTemplateId ?? 0), "the cell");
            Assert.AreEqual(PlayerMessage.PmMissingReqItem, Refused(harness, copy, cell, Vindication.UseArgument), "no hand: refused");
            Stand(copy, N.KuutkPool);
            var corpse = Kill(harness, copy, N.Kuutk);
            Assert.IsTrue(Holds(corpse, Vindication.HandTemplate), "Kuutk drops the hand");
            Assert.AreEqual((0U, MissionObjectiveState.Completed), (Held(harness, Vindication.HandTemplate), Objective(harness, Vindication.MissionId, Vindication.DefeatKuutk)),
                "4 done; a death is not a pickup");
            PickUp(harness, corpse);
            harness.Drain();
            Assert.AreEqual(1U, Held(harness, Vindication.HandTemplate), "the hand");
            ReloadAndRecover(harness, copy);
            Assert.AreEqual((1U, UseObjectState.DoorStateClosed), (Held(harness, Vindication.HandTemplate),
                SceneObject(harness, copy, Vindication.MissionId, Vindication.CellRole).StateId), "the hand held, the cell shut at a reload");

            // 6: the cell.
            Use(harness, copy, SceneObject(harness, copy, Vindication.MissionId, Vindication.CellRole), Vindication.UseArgument);
            Tick(harness, copy, 250);
            cell = SceneObject(harness, copy, Vindication.MissionId, Vindication.CellRole);
            Assert.AreEqual((UseObjectState.DoorStateOpen, false, 0U, MissionObjectiveState.Completed, MissionObjectiveState.Incomplete),
                (cell.StateId, cell.IsEnabled, Held(harness, Vindication.HandTemplate), Objective(harness, Vindication.MissionId, Vindication.FreeThePrisoner),
                    Objective(harness, Vindication.MissionId, Vindication.FindNaimi)), "the cell open, the hand taken, 6 done, 3 open");
            Restart(harness, copy, Vindication.MissionId);
            Assert.AreEqual(UseObjectState.DoorStateOpen, SceneObject(harness, copy, Vindication.MissionId, Vindication.CellRole).StateId, "open at a restart");
            foreach (var (role, _, _) in Vindication.Doors)
                Assert.AreEqual(UseObjectState.DoorStateOpen, SceneObject(harness, copy, Vindication.MissionId, role).StateId, $"{role} open at a restart");

            // 3: Naimi; 5: the crate.
            var naimi = Giver(copy, N.Naimi);
            CollectionAssert.AreEqual(new[] { Vindication.FindNaimi }, Topics(harness, naimi, Vindication.MissionId), "Naimi hears 3");
            Talk(harness, naimi, Vindication.MissionId, Vindication.FindNaimi);
            Tick(harness, copy, 250);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Vindication.MissionId, Vindication.PowerSupply), "5 open");
            Use(harness, copy, SceneObject(harness, copy, Vindication.MissionId, Vindication.CrateRole), Vindication.UseArgument);
            Tick(harness, copy, 250);
            Assert.AreEqual((1U, MissionObjectiveState.Completed, true), (Held(harness, Vindication.PowerSupplyTemplate),
                Objective(harness, Vindication.MissionId, Vindication.PowerSupply), harness.Client.Player.Missions[Vindication.MissionId].Completeable), "the power supply, 5 done");

            drof = Giver(copy, N.Drof);
            Assert.IsEmpty(Topics(harness, drof, Vindication.MissionId), "Drof hears no objective");
            FullBagWaits(harness, drof, Vindication.MissionId, 0, WeaponFiller);
            FailedTurnInPaysNothing(harness, drof, Vindication.MissionId, 0);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, drof, Vindication.MissionId, 0, Vindication.Experience, Vindication.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Vindication.MissionId), "MissionRewarded once");
            Assert.AreEqual((0U, 0U, 0U), (Held(harness, Vindication.KeyPassTemplate), Held(harness, Vindication.RadioTemplate), Held(harness, Vindication.PowerSupplyTemplate)),
                "the key pass, the radio and the power supply taken at the end");
            for (var tick = 0; tick < 4; tick++)
                Tick(harness, copy, 250);
            foreach (var role in Vindication.Doors.Select(door => door.Role).Concat(new[] { Vindication.CellRole, Vindication.CorpseRole, Vindication.CrateRole }))
                Assert.IsNull(FindSceneObject(harness, copy, Vindication.MissionId, role), $"{role} goes with the mission");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, Giver(copy, N.Drof), Vindication.MissionId), "done once");
        }

        /// <summary>
        /// Vindication in a squad of two on it: Kuutk's kill by the player leaves each member a hand of their own (a drop to
        /// every member on 1395, MissionLootSharers) and completes the mate's 4; a member without 1395 gets none. The player's
        /// use of the player's cell completes the mate's 6 too: each one's hand taken (6's action, as Crucible 1401's cards),
        /// each one's own cell open, the mate's 3 opens. The mate's room doors stay shut until the mate's own key pass opens them.
        /// </summary>
        [TestMethod]
        public void VindicationsCellOpensForEverySquadMateOnIt()
        {
            using var harness = WildernessRuntimeTestHarness.Create(lootRoll: (minimum, maximum) => maximum == 100 ? 0 : minimum);
            AshenRuntime.SeedCompleted(harness, AshenMissingScientistsV1.MissionId, Caverns.MissionId, Threshold.MissionId);
            var copy = IntoCaverns(harness);
            var drof = Giver(copy, N.Drof);
            Accept(harness, drof, Vindication.MissionId);
            Tick(harness, copy, 250);
            using var mate = new SquadMate(harness, copy, drof, Vindication.MissionId, new[] { Caverns.MissionId, Threshold.MissionId });
            using var stranger = new SquadMate(harness, copy, drof, 0, Array.Empty<uint>(), 3, mate.PartyId);
            Assert.AreEqual(1U, mate.Held(Vindication.KeyPassTemplate), "the mate's own key pass");

            Stand(copy, N.KuutkPool);
            var kuutk = Here(copy).First(one => one.SpawnPool?.DbId == N.KuutkPool && one.State != CharacterState.Dead);
            mate.Beside(kuutk.Position + new Vector3(0, 0, 3));
            stranger.Beside(kuutk.Position + new Vector3(0, 0, -3));
            var corpse = Kill(harness, copy, N.Kuutk);
            var hands = corpse.LootItems.Where(item => item.ItemTemplateId == Vindication.HandTemplate).ToArray();
            Assert.AreEqual((1, 1, 0), (hands.Count(item => item.ReservedFor == harness.Client.Player.EntityId), hands.Count(item => item.ReservedFor == mate.Client.Player.EntityId),
                hands.Count(item => item.ReservedFor == stranger.Client.Player.EntityId)), "a hand each for the members on 1395");
            mate.Loot(corpse);
            PickUp(harness, corpse);
            harness.Drain();
            Tick(harness, copy, 250);
            Assert.AreEqual((1U, 1U, MissionObjectiveState.Completed), (Held(harness, Vindication.HandTemplate), mate.Held(Vindication.HandTemplate), mate.Of(Vindication.DefeatKuutk)),
                "a hand each, the mate's 4 done");

            Use(harness, copy, SceneObject(harness, copy, Vindication.MissionId, Vindication.CellRole), Vindication.UseArgument);
            for (var tick = 0; tick < 2; tick++)
                Tick(harness, copy, 250);
            Assert.AreEqual((UseObjectState.DoorStateOpen, 0U), (SceneObject(harness, copy, Vindication.MissionId, Vindication.CellRole).StateId, Held(harness, Vindication.HandTemplate)),
                "the player's cell, the player's hand taken");
            Assert.AreEqual((UseObjectState.DoorStateOpen, MissionObjectiveState.Completed, MissionObjectiveState.Incomplete, 0U),
                (mate.Door(Vindication.CellRole).StateId, mate.Of(Vindication.FreeThePrisoner), mate.Of(Vindication.FindNaimi), mate.Held(Vindication.HandTemplate)),
                "the mate's own cell opens with the player's use, the mate's 3 open, the mate's hand taken");
            var (dorm, _, _) = Vindication.Doors[0];
            Assert.AreEqual(UseObjectState.DoorStateClosed, mate.Door(dorm).StateId, "the mate's room door shut");
            mate.Use(mate.Door(dorm));
            Assert.AreEqual((UseObjectState.DoorStateOpen, 1U), (mate.Door(dorm).StateId, mate.Held(Vindication.KeyPassTemplate)), "the mate's own key pass opens it");
        }

        /// <summary>
        /// A mate away from the copy at the player's cell use keeps his hand and his shut cell, his 6 open; back, his own hand
        /// opens his own cell and is taken; his 3 opens.
        /// </summary>
        [TestMethod]
        public void VindicationsMateAwayOpensHisCellWithHisOwnHand()
        {
            using var harness = WildernessRuntimeTestHarness.Create(lootRoll: (minimum, maximum) => maximum == 100 ? 0 : minimum);
            AshenRuntime.SeedCompleted(harness, AshenMissingScientistsV1.MissionId, Caverns.MissionId, Threshold.MissionId);
            var copy = IntoCaverns(harness);
            var drof = Giver(copy, N.Drof);
            Accept(harness, drof, Vindication.MissionId);
            Tick(harness, copy, 250);
            using var mate = new SquadMate(harness, copy, drof, Vindication.MissionId, new[] { Caverns.MissionId, Threshold.MissionId });
            Stand(copy, N.KuutkPool);
            var kuutk = Here(copy).First(one => one.SpawnPool?.DbId == N.KuutkPool && one.State != CharacterState.Dead);
            mate.Beside(kuutk.Position + new Vector3(0, 0, 3));
            var corpse = Kill(harness, copy, N.Kuutk);
            mate.Loot(corpse);
            PickUp(harness, corpse);
            harness.Drain();
            Tick(harness, copy, 250);
            Assert.AreEqual((1U, 1U), (Held(harness, Vindication.HandTemplate), mate.Held(Vindication.HandTemplate)), "a hand each");

            mate.Away();
            Use(harness, copy, SceneObject(harness, copy, Vindication.MissionId, Vindication.CellRole), Vindication.UseArgument);
            for (var tick = 0; tick < 2; tick++)
                Tick(harness, copy, 250);
            Assert.AreEqual((UseObjectState.DoorStateOpen, 0U), (SceneObject(harness, copy, Vindication.MissionId, Vindication.CellRole).StateId, Held(harness, Vindication.HandTemplate)),
                "the player's cell open, the player's hand taken");
            mate.Back();
            Assert.AreEqual((MissionObjectiveState.Incomplete, UseObjectState.DoorStateClosed, 1U),
                (mate.Of(Vindication.FreeThePrisoner), mate.Door(Vindication.CellRole).StateId, mate.Held(Vindication.HandTemplate)), "away at the use: his cell shut, his hand kept");
            mate.Use(mate.Door(Vindication.CellRole));
            Assert.AreEqual((MissionObjectiveState.Completed, UseObjectState.DoorStateOpen, 0U, MissionObjectiveState.Incomplete),
                (mate.Of(Vindication.FreeThePrisoner), mate.Door(Vindication.CellRole).StateId, mate.Held(Vindication.HandTemplate), mate.Of(Vindication.FindNaimi)),
                "his own hand opens his own cell and is taken; his 3 open");
        }

        /// <summary>
        /// An abandon of Vindication with the hand held: the doors, bodies and cell go with the scene, the key pass and the radio
        /// are taken, the hand is kept; taken again the key pass comes again and the new doors stand shut; the kept hand opens
        /// the new cell, but 4 waits for Kuutk.
        /// </summary>
        [TestMethod]
        public void VindicationAbandonedKeepsTheHand()
        {
            using var harness = WildernessRuntimeTestHarness.Create(lootRoll: (minimum, maximum) => maximum == 100 ? 0 : minimum);
            AshenRuntime.SeedCompleted(harness, AshenMissingScientistsV1.MissionId, Caverns.MissionId, Threshold.MissionId);
            var copy = IntoCaverns(harness);
            Accept(harness, Giver(copy, N.Drof), Vindication.MissionId);
            Tick(harness, copy, 250);
            Use(harness, copy, SceneObject(harness, copy, Vindication.MissionId, Vindication.CorpseRole), Vindication.UseArgument);
            Stand(copy, N.KuutkPool);
            PickUp(harness, Kill(harness, copy, N.Kuutk));
            harness.Drain();
            Tick(harness, copy, 250);
            Assert.AreEqual((1U, 1U, 1U), (Held(harness, Vindication.KeyPassTemplate), Held(harness, Vindication.RadioTemplate), Held(harness, Vindication.HandTemplate)));

            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Vindication.MissionId));
            harness.Drain();
            for (var tick = 0; tick < 4; tick++)
                Tick(harness, copy, 250);
            foreach (var role in Vindication.Doors.Select(door => door.Role).Concat(new[] { Vindication.CellRole, Vindication.CorpseRole, Vindication.CrateRole }))
                Assert.IsNull(FindSceneObject(harness, copy, Vindication.MissionId, role), $"{role} goes with the abandon");
            Assert.AreEqual((0U, 0U, 1U), (Held(harness, Vindication.KeyPassTemplate), Held(harness, Vindication.RadioTemplate), Held(harness, Vindication.HandTemplate)),
                "the key pass and the radio taken, the hand kept");

            Accept(harness, Giver(copy, N.Drof), Vindication.MissionId);
            Tick(harness, copy, 250);
            Assert.AreEqual(1U, Held(harness, Vindication.KeyPassTemplate), "the key pass again");
            foreach (var (role, _, _) in Vindication.Doors)
                Assert.AreEqual(UseObjectState.DoorStateClosed, SceneObject(harness, copy, Vindication.MissionId, role).StateId, $"{role} shut");
            Use(harness, copy, SceneObject(harness, copy, Vindication.MissionId, Vindication.CellRole), Vindication.UseArgument);
            Tick(harness, copy, 250);
            Assert.AreEqual((UseObjectState.DoorStateOpen, MissionObjectiveState.Completed, MissionObjectiveState.Incomplete),
                (SceneObject(harness, copy, Vindication.MissionId, Vindication.CellRole).StateId, Objective(harness, Vindication.MissionId, Vindication.FreeThePrisoner),
                    Objective(harness, Vindication.MissionId, Vindication.DefeatKuutk)), "the kept hand opens the cell; 4 waits for Kuutk");
        }

        /// <summary>
        /// The squad's copy of Indra Caverns, entered through its door from Ashen Desert (map_link 58): its pools' dead and
        /// cleared marks of an earlier test are gone (a squad copy outlives a test), and an earlier test's scene objects too.
        /// </summary>
        internal static MapChannel IntoCaverns(WildernessRuntimeTestHarness harness)
        {
            if (!harness.Maps.MapChannelArray.ContainsKey(AshenMissionDataV1.MapContextId))
                PlateauInstanceMaps.LoadMap(harness, AshenMissionDataV1.MapContextId);
            if (!harness.Maps.MapChannelArray.ContainsKey(Map))
                PlateauInstanceMaps.LoadMap(harness, Map);
            var copy = PlateauInstanceMaps.ThroughTheDoor(harness, Map);
            foreach (var pool in copy.SpawnPools.Where(pool => Pools.Contains(pool.DbId)))
                pool.ClearedAtUtcMs = 0;
            foreach (var creature in Here(copy).Where(one => Pools.Contains(one.SpawnPool?.DbId ?? 0)).ToArray())
                if (creature.State == CharacterState.Dead)
                    Respawn(copy, creature.SpawnPool.DbId);
            harness.Manager.Scenes.Resume(harness.Client);
            harness.Drain();
            return copy;
        }

        /// <summary>The hostile spawn pools of Indra Caverns (as AshenWhiteOasisTests.HostilePools on Ashen Desert).</summary>
        internal static List<SpawnPoolEntry> HostilePools(WildernessRuntimeTestHarness harness) =>
            harness.World.SpawnPoolEntries.AsNoTracking().Where(row => row.MapContextId == Map && row.Mode == 0).AsEnumerable()
                .Where(row => new[] { row.Creature1Id, row.Creature2Id, row.Creature3Id, row.Creature4Id, row.Creature5Id, row.Creature6Id }
                    .Any(id => id != 0 && CreatureManager.Instance.LoadedCreatures.TryGetValue(id, out var creature) && creature.TargetCategory == TargetCategory.Hostile))
                .ToList();

        /// <summary>The gap from a place to a pool's circle: 2D, or 3D for a pool on another floor (OtherFloor).</summary>
        internal static float Gap(Vector3 at, SpawnPoolEntry pool)
        {
            var pole = new Vector3((float)pool.PosX, (float)pool.PosY, (float)pool.PosZ);
            var flat = Vector2.Distance(new(at.X, at.Z), new(pole.X, pole.Z));
            return (MathF.Abs(at.Y - pole.Y) >= OtherFloor ? Vector3.Distance(at, pole) : flat) - (float)pool.Radius;
        }

        /// <summary>
        /// The set's places on Indra Caverns: label, place, whether it is seen from the ring, the height looked at, its own pool
        /// (0: none). Fix I: Bendal's body in the pillbox (AshenFixIV1.BendalAt); the facility door at the Security Check Point
        /// entrance's door place, in a tunnel the navmesh does not cover (upstream N29), is checked on its own
        /// (AshenFixITests.TheFacilityDoorClosesTheEntranceTunnel).
        /// </summary>
        internal static List<(string Label, Vector3 At, bool Ring, float Middle, uint Own)> Places() => new()
        {
            ("Major Diren", V(N.DirenAt), true, F.NpcMiddle, 0), ("Sergeant Drof", V(N.DrofAt), true, F.NpcMiddle, 0),
            ("Scout Naimi", V(N.NaimiAt), false, F.NpcMiddle, 0), ("Overseer Kuutk", V(N.KuutkAt), true, F.NpcMiddle, N.KuutkPool),
            ("the Watch Post's west Thrax", V(N.WatchPostWestAt), false, 0, N.WatchPostWest), ("the Watch Post's east Thrax (fix J's place)", V(AshenFixJV1.PoolAt), false, 0, N.WatchPostEast),
            ("Bendal's body", ToVector(AshenFixIV1.BendalAt), true, F.ObjectMiddle, 0), ("Aksrah's body", ToVector(Vindication.AksrahAt), true, F.ObjectMiddle, 0),
            ("the Supply Crate", ToVector(Vindication.CrateAt), true, F.ObjectMiddle, 0),
            ("the dorm 152 door", ToVector(Vindication.Doors[0].At), false, 0, 0), ("the Science Lab door", ToVector(Vindication.Doors[1].At), false, 0, 0),
            ("the dorm 32 door", ToVector(Vindication.Doors[2].At), false, 0, 0), ("Naimi's cell", ToVector(Vindication.CellAt), false, 0, 0)
        };

        /// <summary>How far each leg's start is moved along its first stretch for the walk's recheck (as AshenConscriptionEntryTests).</summary>
        private static readonly float[] StartShifts = { 0, 0.13f, 0.27f, 0.4f };

        /// <summary>
        /// The legs of the set's walks on the client's floor, each walked there and back on its own (corners: a client-floor
        /// flood's way, 2026-11-11, the climb rule's, ESTIMATE of a player's line). A walk is a run of legs.
        /// </summary>
        internal static readonly Dictionary<string, Vector3[]> Legs = new()
        {
            ["the arrival to Diren"] = new Vector3[]
            {
                new(-40.689f, 8.9327f, -421.6495f), new(-47, 8.5f, -417), new(-50, 7.6f, -412), new(-51, 7.6f, -407), new(-53, 7.8f, -402), new(-53, 8.6f, -395)
            },
            ["Diren to the Watch Post"] = new Vector3[]
            {
                new(-53, 8.6f, -395), new(-53, 8.4f, -393), new(-52, 7.86f, -391), new(-45, 6.64f, -383), new(-41, 7.83f, -375), new(-41, 6.65f, -362),
                new(-40, 6.18f, -356), new(-36, 6.3f, -355), new(-31, 5.93f, -353), new(-25, 8.8f, -354), new(-25, 9.05f, -353), new(-5, 10.68f, -351)
            },
            // Fix I: Bendal in the pillbox, by the way to the facility door past the pillbox's south side, then in at its open
            // east side (the real client walked into the pillbox from the north on the navmesh's path, to-fix row 34).
            ["the Watch Post to Bendal"] = new Vector3[]
            {
                new(-5, 10.68f, -351), new(15, 18.8f, -350), new(23, 18.61f, -345), new(24, 19f, -345), new(28, 16.43f, -337), new(9.5f, 18.52f, -336)
            },
            ["the Watch Post to behind the facility door"] = new Vector3[]
            {
                new(-5, 10.68f, -351), new(15, 18.8f, -350), new(23, 18.61f, -345), new(24, 19f, -345), new(32, 18.8f, -341), new(37, 17.93f, -341),
                new(50, 16.05f, -330), new(52, 16.07f, -329), new(78, 16.5f, -328), new(86, 16.5f, -328)
            },
            ["behind the door to the east corridor"] = new Vector3[]
            {
                new(86, 16.5f, -328), new(91, 16.5f, -329), new(99, 16.5f, -328), new(119, 9.87f, -328), new(127, 8.5f, -328), new(130, 8.5f, -331),
                new(137.5f, 8.5f, -331), new(137.5f, 8.5f, -322), new(137, 8.5f, -317)
            },
            ["the east corridor to Aksrah"] = new Vector3[]
            {
                new(137, 8.5f, -317), new(137, 8.5f, -309), new(145, 8.5f, -301), new(150, 8.5f, -298), new(153, 8.5f, -296)
            },
            ["the east corridor to the hall"] = new Vector3[]
            {
                new(137, 8.5f, -317), new(132, 8.5f, -312), new(126, 8.5f, -304), new(125, 8.5f, -303), new(125, 8.5f, -289), new(124, 8.5f, -288),
                new(123, 8.5f, -287), new(121, 8.5f, -287), new(120, 8.5f, -286), new(119, 8.5f, -285), new(116, 8.5f, -283), new(108, 8.5f, -281),
                new(88, 8.5f, -280)
            },
            ["the hall to Kuutk"] = new Vector3[]
            {
                new(88, 8.5f, -280), new(75, 8.5f, -288), new(74, 8.5f, -291), new(76, 8.5f, -304), new(76, 1.49f, -324), new(76, 0.5f, -332),
                new(72, 0.5f, -336), new(40, -7.5f, -340), new(36, -7.5f, -345), new(37, -7.5f, -365), new(37, -7.5f, -373), new(57, -7.5f, -393),
                new(57, -7.5f, -398), new(59, -7.42f, -403)
            },
            ["the hall to Drof"] = new Vector3[]
            {
                new(88, 8.5f, -280), new(75, 8.5f, -287), new(70, 8.5f, -292), new(67, 8.5f, -293), new(62, 8.5f, -290), new(60, 8.5f, -287),
                new(59, 8.5f, -286), new(51, 8.5f, -283), new(48, 8.5f, -280), new(47, 8.5f, -260), new(45, 8.5f, -257), new(32, 8.5f, -248),
                new(29, 8.5f, -245), new(29, 8.5f, -232), new(26, 8.5f, -224), new(26, 5.01f, -211), new(24, 4.5f, -206), new(21, 4.5f, -203),
                new(8, 1.31f, -203), new(3, 0.5f, -203), new(-2, 0.5f, -209), new(-5, 0.58f, -221)
            },
            ["the hall to before Naimi's cell"] = new Vector3[]
            {
                new(88, 8.5f, -280), new(83, 8.5f, -279), new(81, 8.5f, -277), new(79, 8.5f, -272), new(77, 8.5f, -252), new(76, 8.5f, -250),
                new(77, 13.66f, -237), new(77, 16.5f, -232), new(70, 16.5f, -224), new(50, 16.5f, -224), new(45, 16.67f, -226), new(40, 16.04f, -221),
                new(39, 16.04f, -220), new(34, 16.04f, -221), new(29, 16.04f, -226), new(27, 16.28f, -228), new(33, 22.88f, -236), new(35, 24.42f, -238),
                new(27, 24.37f, -242), new(22, 24.52f, -243), new(21, 24.52f, -244), new(16, 24.52f, -244), new(12.5f, 24.8f, -243)
            },
            ["the hall to the Supply Crate"] = new Vector3[]
            {
                new(88, 8.5f, -280), new(75, 8.5f, -287), new(70, 8.5f, -292), new(67, 8.5f, -293), new(62, 8.5f, -290), new(60, 8.5f, -287),
                new(59, 8.5f, -286), new(51, 8.5f, -283), new(31, 8.5f, -282), new(24, 8.5f, -284)
            }
        };

        /// <summary>
        /// The walks: label, its legs, the target's own pool (0: none), the door it crosses (null: none): its place, yaw, its
        /// leaf's plane along its local z and the leaf's half width (the 4 m corridor door 10749: 0, 3.52; fix I's entrance
        /// door 23160: the middle of its box's z -4.2..-2.3, 1.7).
        /// </summary>
        internal static List<(string Label, string[] Legs, uint Own, (Vector3 At, double Yaw, float Leaf, float Half)? Door)> Walks()
        {
            const string Way = "the arrival to Diren", Post = "Diren to the Watch Post", In = "the Watch Post to behind the facility door";
            const string East = "behind the door to the east corridor", Hall = "the east corridor to the hall";
            (Vector3, double, float, float)? Door(int index) => (ToVector(Vindication.Doors[index].At), Vindication.Doors[index].Orientation, 0f, 3.52f);
            return new()
            {
                ("Diren from the arrival", new[] { Way }, 0, null),
                ("Bendal from the arrival", new[] { Way, Post, "the Watch Post to Bendal" }, 0, null),
                ("behind the facility door from the arrival", new[] { Way, Post, In }, 0, (ToVector(AshenFixIV1.DoorAt), AshenFixIV1.DoorOrientation,
                    (AshenFixIV1.LeafNear + AshenFixIV1.LeafFar) / 2, AshenFixIV1.LeafHalfWidth)),
                ("Aksrah from the facility door", new[] { East, "the east corridor to Aksrah" }, 0, Door(0)),
                ("Kuutk from the facility door", new[] { East, Hall, "the hall to Kuutk" }, N.KuutkPool, Door(1)),
                ("Drof from the facility door", new[] { East, Hall, "the hall to Drof" }, 0, null),
                ("before Naimi's cell from the facility door", new[] { East, Hall, "the hall to before Naimi's cell" }, 0, null),
                ("the Supply Crate from the facility door", new[] { East, Hall, "the hall to the Supply Crate" }, 0, Door(2))
            };
        }

        /// <summary>A walk's legs as one line.</summary>
        internal static List<Vector3> Line(IEnumerable<string> legs) =>
            legs.SelectMany((leg, index) => index == 0 ? Legs[leg] : Legs[leg].Skip(1)).ToList();

        /// <summary>
        /// Whether the line crosses a door's leaf (local x +-<paramref name="half"/>, in the plane local z <paramref name="leaf"/>)
        /// placed at <paramref name="door"/> with yaw <paramref name="yaw"/>: a 4 m Brann door's 3.52 m at z 0 by default.
        /// </summary>
        internal static bool Crosses(List<Vector3> line, Vector3 door, double yaw, float leaf = 0, float half = 3.52f)
        {
            var (sin, cos) = ((float)Math.Sin(yaw), (float)Math.Cos(yaw));
            // The door's local z axis (its normal) in the world (yaw about y): (sin, 0, cos); its local x: (cos, 0, -sin).
            float Along(Vector3 p) => (p.X - door.X) * sin + (p.Z - door.Z) * cos - leaf;
            float Across(Vector3 p) => (p.X - door.X) * cos - (p.Z - door.Z) * sin;
            for (var i = 0; i + 1 < line.Count; i++)
            {
                var (a, b) = (Along(line[i]), Along(line[i + 1]));
                if (a * b > 0 || MathF.Abs(a - b) < 0.001f)
                    continue;
                var at = Vector3.Lerp(line[i], line[i + 1], a / (a - b));
                if (MathF.Abs(Across(at)) <= half && MathF.Abs(at.Y - door.Y) < 3)
                    return true;
            }
            return false;
        }

        /// <summary>The pool's Thrax killed by the player, then the spawn pools' pass tells the copy's players (SpawnPoolClearedProgress).</summary>
        private static void KillWatchPost(WildernessRuntimeTestHarness harness, MapChannel copy, uint pool)
        {
            SpawnPoolClearedProgress.Missions = () => harness.Manager;
            Stand(copy, pool);
            var bane = Here(copy).First(one => one.SpawnPool?.DbId == pool && one.State != CharacterState.Dead);
            MarshesPreoDasTests.Kill(harness, copy, bane);
            SpawnPoolManager.Instance.SpawnPoolWorker(copy, 0);
            harness.Drain();
        }

        /// <summary>The pool's creature standing in the copy: spawned, or back after its own time if an earlier kill left it dead.</summary>
        internal static void Stand(MapChannel copy, uint pool)
        {
            Spawn(copy, pool);
            if (!Here(copy).Any(one => one.SpawnPool?.DbId == pool && one.State != CharacterState.Dead))
                Respawn(copy, pool);
            Assert.IsTrue(Here(copy).Any(one => one.SpawnPool?.DbId == pool && one.State != CharacterState.Dead), $"pool {pool} stands");
        }

        internal static string MapName(WildernessRuntimeTestHarness harness, uint map) =>
            harness.World.MapInfoEntries.AsNoTracking().Single(row => row.Id == map).MapName;

        internal static Vector3 V((double X, double Y, double Z) at) => new((float)at.X, (float)at.Y, (float)at.Z);

        internal static uint Held(WildernessRuntimeTestHarness harness, uint template) => WildernessRanjaGorgeTests.Held(harness, template);

        internal static uint HeldBy(WildernessRuntimeTestHarness harness, Client client, uint template)
        {
            using var unit = harness.CreateChar();
            return unit.CharacterInventories.GetItems(client.AccountEntry.Id)
                .Where(row => row.CharacterId == client.Player.Id)
                .Select(row => unit.Items.GetItem(row.ItemId))
                .Where(item => item.ItemTemplateId == template)
                .Aggregate(0U, (count, item) => count + item.StackSize);
        }

        /// <summary>
        /// A server restart as far as the scene's objects go (CrucibleBloodFireSteelTests.Restart): the reload, the mission's
        /// objects dropped from the copy and its cells, the resume makes them again from the checkpoint.
        /// </summary>
        internal static void Restart(WildernessRuntimeTestHarness harness, MapChannel copy, uint mission)
        {
            WildernessRanjaGorgeTests.Reload(harness);
            harness.Manager.Scenes.Detach(harness.Client, copy);
            foreach (var obj in copy.DynamicObjects.Where(obj => obj.SceneMissionId == mission).ToArray())
            {
                CellManager.Instance.RemoveFromWorld(copy, obj);
                copy.DynamicObjects.Remove(obj);
                obj.IsInWorld = false;
            }
            harness.Manager.Scenes.Resume(harness.Client);
            harness.Drain();
            Tick(harness, copy, 250);
            Tick(harness, copy, 250);
        }

        /// <summary>
        /// A full mission-item bag (every free slot of category 4 filled) at the player's search of <c>role</c>: no item, the
        /// object closed and usable, the objective open; one slot freed, the search gives the item, opens the object and
        /// completes the objective.
        /// </summary>
        internal static void FullBagWaitsAtASearch(WildernessRuntimeTestHarness harness, MapChannel copy, uint mission, string role, uint template,
            uint objective, UseObjectState closed, UseObjectState opened, uint argument)
        {
            for (var slot = 0; slot < 60; slot++)
            {
                try { WildernessAliaBranchesTests.FillCategory(harness, MissionItemFiller, 1); }
                catch (Exception) { break; }
            }
            Use(harness, copy, SceneObject(harness, copy, mission, role), argument);
            Tick(harness, copy, 250);
            var obj = SceneObject(harness, copy, mission, role);
            Assert.AreEqual((0U, closed, true, MissionObjectiveState.Incomplete), (Held(harness, template), obj.StateId, obj.IsEnabled, Objective(harness, mission, objective)),
                $"{role}: a full bag, the search waits");
            WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
            harness.Drain();
            Use(harness, copy, SceneObject(harness, copy, mission, role), argument);
            Tick(harness, copy, 250);
            obj = SceneObject(harness, copy, mission, role);
            Assert.AreEqual((1U, opened, false, MissionObjectiveState.Completed), (Held(harness, template), obj.StateId, obj.IsEnabled, Objective(harness, mission, objective)),
                $"{role}: a slot free, the search gives the item");
            while (harness.Client.Player.Inventory.PersonalInventory.Where(id => id != 0).Select(EntityManager.Instance.GetItem)
                .Any(item => item.ItemTemplate.ItemTemplateId == MissionItemFiller))
                WildernessAliaBranchesTests.FreeOneSlot(harness, MissionItemFiller);
            harness.Drain();
        }

        internal const uint MissionItemFiller = 11519;      // as PlateauCampResistanceTests (inventory category 4)

        /// <summary>A use of the object from beside it that the server refuses: no recovery, the refusal's message.</summary>
        internal static PlayerMessage? Refused(WildernessRuntimeTestHarness harness, MapChannel copy, DynamicObject obj, uint argument)
        {
            harness.MoveTo(obj.Position + new Vector3(0, 0, 1));
            harness.Drain();
            var count = copy.PerformRecovery.Count;
            harness.Objects.RequestUseObjectPacket(harness.Client, new RequestUseObjectPacket
            {
                EntityId = obj.EntityId, ActionId = ActionId.UseObject, ActionArgId = argument
            });
            Assert.AreEqual(count, copy.PerformRecovery.Count, $"{obj.SceneActorRole}: no use");
            return harness.Drain().OfType<UserActionFailedPacket>().LastOrDefault()?.MsgId;
        }

        /// <summary>
        /// A second (or third) member of the player's squad in the squad's copy beside the player: alive, with the stats a share
        /// of a kill's experience updates, resumed as one who came in by its door, on <c>mission</c> (taken from the NPC) if it
        /// is not 0, with <c>done</c> in its history. Disposed: out of the squad and the copy.
        /// </summary>
        internal sealed class SquadMate : IDisposable
        {
            private readonly WildernessRuntimeTestHarness _harness;
            private readonly MapChannel _copy;
            private readonly uint _mission;
            private readonly bool _ownsParty;
            internal uint PartyId { get; }
            internal Client Client { get; }

            internal SquadMate(WildernessRuntimeTestHarness harness, MapChannel copy, Creature giver, uint mission, uint[] done, uint characterIndex = 2, uint partyId = 0)
            {
                (_harness, _copy, _mission) = (harness, copy, mission);
                copy.ClientList.RemoveAll(client => !ReferenceEquals(client, harness.Client) && client.Player?.Id == harness.Client.Player.Id);
                var member = Client = harness.Context.CreateAdditionalClient(characterIndex, manager: harness.Manager);
                foreach (var attribute in new[] { Attributes.Body, Attributes.Mind, Attributes.Spirit, Attributes.Chi, Attributes.Aware, Attributes.Speed, Attributes.Regen })
                    member.Player.Attributes[attribute] = new ActorAttributes(attribute, 10, 10, 10, 0, 0);
                member.Player.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 1000, 1000, 1000, 0, 0);
                member.Player.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, 100, 100, 100, 0, 0);
                member.Player.Attributes[Attributes.Power] = new ActorAttributes(Attributes.Power, 50, 50, 50, 0, 0);
                if (partyId == 0)
                {
                    _ownsParty = true;
                    PartyId = PartyManager.Instance.GetPartyId;
                    PartyManager.Instance.Parties.Add(PartyId, new Party(PartyId, harness.Client.AccountEntry.Id, new List<PartyMember> { new(harness.Client), new(member) })
                    {
                        LootMethod = PartyLootMethod.FreeForAll, LootThreshold = PartyLootThreshold.Prototype
                    });
                    harness.Client.Player.PartyId = PartyId;
                }
                else
                {
                    PartyId = partyId;
                    PartyManager.Instance.Parties[partyId].Members.Add(new PartyMember(member));
                }
                CellManager.Instance.RemoveFromWorld(member);
                member.Player.MapChannel?.ClientList.Remove(member);
                member.Player.MapChannel = copy;
                member.Player.RuntimeMapChannel = copy;
                member.Player.MapContextId = copy.MapInfo.MapContextId;
                copy.ClientList.Add(member);
                member.SetWorldPosition(giver.Position + new Vector3(0, 0, 1), 0);
                CellManager.Instance.AddToWorld(member);
                member.Player.PartyId = PartyId;
                lock (Server.Clients)
                    Server.Clients.Add(member);
                if (done.Length > 0)
                {
                    using (var unit = harness.CreateChar())
                        unit.ExecuteTransaction(() =>
                        {
                            foreach (var history in done.Prepend(AshenMissingScientistsV1.MissionId))
                                unit.CharacterMissions.Runtime.Add(new Rasa.Structures.Char.CharacterMissionHistoryEntry
                                {
                                    CharacterId = member.Player.Id,
                                    MissionId = history,
                                    AssignmentId = Guid.NewGuid().ToString("N"),
                                    AssignmentGeneration = 1,
                                    ContentRevision = AshenMissionDataV1.Revision,
                                    CompletedAtUtc = harness.UtcNow,
                                    RewardedAtUtc = harness.UtcNow,
                                    Outcome = (uint)MissionState.Completed,
                                    Rewarded = true
                                });
                        });
                    using var reader = harness.CreateChar();
                    MissionJournalAdapter.ApplyHistory(member.Player, reader.CharacterMissions.Runtime.History(member.Player.Id));
                }
                harness.Manager.Scenes.Resume(member);     // in the copy as one who came in by its door
                if (mission != 0)
                    Assert.IsTrue(harness.Manager.AcceptOfferedMission(member, giver.EntityId, mission), $"the mate takes {mission}");
                harness.Drain();
                Tick(harness, copy, 250);
            }

            internal MissionObjectiveState Of(uint objective) => Client.Player.Missions[_mission].Objectives[objective].State;

            internal uint Held(uint template) => HeldBy(_harness, Client, template);

            /// <summary>The mate's own scene object of <c>role</c> (of the mate's run; the copy can still hold an earlier test's).</summary>
            internal DynamicObject Door(string role)
            {
                using var unit = _harness.CreateChar();
                var runs = unit.CharacterMissions.Runtime.Scenes(Client.Player.Id, _mission).Select(scene => scene.RunId).ToHashSet();
                return _copy.DynamicObjects.Single(obj => obj.SceneMissionId == _mission && obj.SceneActorRole == role &&
                    obj.SceneOwnerCharacterId == Client.Player.Id && obj.IsInWorld && runs.Contains(obj.SceneRunId));
            }

            /// <summary>Away from the copy: off its client list (a squad signal reaches only those on it).</summary>
            internal void Away()
            {
                CellManager.Instance.RemoveFromWorld(Client);
                _copy.ClientList.RemoveAll(client => ReferenceEquals(client, Client));
                Assert.IsFalse(_copy.ClientList.Contains(Client), "away");
                _harness.Drain();
            }

            /// <summary>Back in the copy (as Away undone).</summary>
            internal void Back()
            {
                if (!_copy.ClientList.Contains(Client))
                    _copy.ClientList.Add(Client);
                CellManager.Instance.AddToWorld(Client);
                _harness.Drain();
                Tick(_harness, _copy, 250);
            }

            internal void Beside(Vector3 at)
            {
                Client.SetWorldPosition(at, 0);
                _harness.Drain();
            }

            internal void Loot(LootDispenser loot)
            {
                LootDispenserManager.Instance.RequestLootAllFromCorpse(Client, new RequestLootAllFromCorpsePacket { EntityId = loot.EntityId });
                _harness.Drain();
            }

            /// <summary>The mate's use of an object from beside it, and its recovery if one is due.</summary>
            internal void Use(DynamicObject obj, uint argument = 1)
            {
                Beside(obj.Position + new Vector3(0, 0, 1));
                var count = _copy.PerformRecovery.Count;
                _harness.Objects.RequestUseObjectPacket(Client, new RequestUseObjectPacket
                {
                    EntityId = obj.EntityId, ActionId = ActionId.UseObject, ActionArgId = argument
                });
                if (_copy.PerformRecovery.Count > count)
                {
                    var recovery = _copy.PerformRecovery.Last();
                    _copy.PerformRecovery.Remove(recovery);
                    ActorActionManager.Instance.PerformRecovery(_copy, recovery);
                }
                _harness.Drain();
                Tick(_harness, _copy, 250);
                Tick(_harness, _copy, 250);
            }

            public void Dispose()
            {
                _harness.Manager.Scenes.Detach(Client, _copy);
                lock (Server.Clients)
                    Server.Clients.Remove(Client);
                Client.Player.PartyId = 0;
                if (_ownsParty)
                {
                    _harness.Client.Player.PartyId = 0;
                    PartyManager.Instance.Parties.Remove(PartyId);
                    PartyManager.Instance.FreePartyId(PartyId);
                }
                CellManager.Instance.RemoveFromWorld(Client);
                _copy.ClientList.Remove(Client);
            }
        }
    }
}
