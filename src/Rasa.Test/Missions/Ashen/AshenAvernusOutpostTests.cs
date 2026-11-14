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
    using N = AshenAvernusOutpostNpcsV1;
    using M = AshenFixMV1;
    using Overly = AshenOverlySensitiveV1;
    using Last = AshenPetrovichsLastStandV1;
    using Run = AshenPetrovichsRunV1;
    using Facility = AshenIndraFacilityTests;
    using F = MarshesClientFloorTests;
    using WO = AshenWhiteOasisTests;

    /// <summary>
    /// Ashen Desert set 17, Avernus Outpost (squad instance 2110), the outpost, played through as a player plays it: Karter's
    /// Overly Sensitive (1473: the courier bag, the prototype ammo, the tavern corpse's diary, four servers, the data backup
    /// machine, the landing strip corpse's card, the teleporter into the prison, the secret stash), Drazen's Petrovich's Last
    /// Stand (1533: through the Atta Teleporter to Petrovich in the AFS Graveyard) and Petrovich's Run (1535: the graveyard
    /// cleared, the tunnel with Petrovich and his squad, the rest talk, the ambush, the top). With them the set's places and
    /// walks on the client's floor, Petrovich's routes on the navmesh, and the Atta Teleporter's pads.
    ///
    /// Why these tests: the set gives each squad member his own sample, diary and card from his own objects; the stash takes no
    /// hit before its objective is open; the teleporter needs the card; a pick-up given twice or to the wrong member, an item
    /// kept after the mission, a squad that does not come along, an ambush that does not come or never ends, or a pad that does
    /// not take the player to the hive would each break it.
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class AshenAvernusOutpostTests
    {
        private const uint Map = N.Outpost;

        /// <summary>The set's missions.</summary>
        internal static readonly uint[] Missions = { Overly.MissionId, Last.MissionId, Run.MissionId };

        /// <summary>The pools set 17 adds.</summary>
        internal static readonly uint[] Pools = { N.Karter, N.Drazen, N.Petrovich, N.PetrovichTop };

        /// <summary>A player's half width on the client's floor (the real-client lesson of set 16: 0.5 m, not 0.33).</summary>
        internal const float PlayerHalfWidth = 0.5f;

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
            yield return (1473, Overly.ArmorSample, Overly.BagAt.X, Overly.BagAt.Z, Overly.ObjectMarkerRadius);
            yield return (1473, Overly.DestroyAmmo, Overly.AmmoAt.X, Overly.AmmoAt.Z, Overly.ObjectMarkerRadius);
            yield return (1473, Overly.Diary, Overly.DiaryAt.X, Overly.DiaryAt.Z, Overly.ObjectMarkerRadius);
            yield return (1473, Overly.Servers, Overly.ServersMarkerAt.X, Overly.ServersMarkerAt.Z, Overly.ServersMarkerRadius);
            yield return (1473, Overly.SecretStash, Overly.StashAt.X, Overly.StashAt.Z, Overly.ObjectMarkerRadius);
            yield return (1473, Overly.SecretStash, Overly.TeleporterAt.X, Overly.TeleporterAt.Z, Overly.ObjectMarkerRadius);
            yield return (1473, Overly.SecretStash, Overly.CardAt.X, Overly.CardAt.Z, Overly.ObjectMarkerRadius);
            yield return (1473, Overly.DataBackup, Overly.BackupAt.X, Overly.BackupAt.Z, Overly.ObjectMarkerRadius);
            // Fix P: 790701's marker round its farthest draw (14.0 m) and 4 m.
            foreach (var (pool, at, radius) in AshenFixOV1.CavePools)
                yield return (1535, Run.ClearTheCave, at.X, at.Z, pool == AshenFixOV1.EastPool ? AshenFixPV1.EastMarkerNow : radius + Run.PoolMarkerPadding);
            yield return (1535, Run.TunnelRun, Run.RestAt.X, Run.RestAt.Z, Run.RestRadius);
            yield return (1535, Run.Rest, Run.RestAt.X, Run.RestAt.Z, Run.NpcMarkerRadius);
            yield return (1535, Run.FendOff, Run.RestAt.X, Run.RestAt.Z, Run.AmbushMarkerRadius);
            yield return (1535, Run.ToTheTop, Run.TopAt.X, Run.TopAt.Z, Run.TopRadius);
        }

        /// <summary>
        /// The rows of the three missions: givers and receivers (Karter both of 1473; Drazen gives 1533, Petrovich in the graveyard
        /// takes it in and gives 1535, his second row at the top takes 1535 in), levels, the Ashen category, the client's texts and
        /// talks (1535,3 package 1803 Petrovich), no prerequisite; every marker on Avernus Outpost, unnamed (the client names none
        /// of the set's); each mission operational.
        /// </summary>
        [TestMethod]
        public void TheSetsRowsAreTheClients()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            const string revision = AshenMissionDataV1.Revision;
            var expected = new (uint Mission, uint? Giver, uint? Receiver, uint Level, uint Name, (uint, uint?)[] Talks)[]
            {
                (Overly.MissionId, N.Karter, N.Karter, 41, 13094, Array.Empty<(uint, uint?)>()),
                (Last.MissionId, N.Drazen, N.Petrovich, 41, 13696, Array.Empty<(uint, uint?)>()),
                (Run.MissionId, N.Petrovich, N.PetrovichTop, 42, 13754, new (uint, uint?)[] { (3, 1803) })
            };
            var catalog = new MissionContentCatalog(harness, new Dictionary<uint, Mission>(), null);
            catalog.Load();
            foreach (var (mission, giver, receiver, level, name, talks) in expected)
            {
                var definition = world.MissionContentDefinitionEntries.AsNoTracking().Single(entry => entry.MissionId == mission && entry.ContentRevision == revision);
                Assert.AreEqual((giver, receiver, (uint?)level, (uint?)AshenMissionDataV1.CategoryId, name, true, false),
                    (definition.GiverId, definition.ReceiverId, definition.Level, definition.CategoryId, definition.ClientNameTextId, definition.Enabled,
                        definition.RadioCompleteable), $"{mission}");
                var talkRows = world.Set<MissionTriggerEntry>().AsNoTracking()
                    .Where(entry => entry.MissionId == mission && entry.ContentRevision == revision && entry.Kind == MissionTriggerKind.Conversation)
                    .AsEnumerable().Select(entry => (entry.ObjectiveId, entry.NpcPackageId)).OrderBy(entry => entry.ObjectiveId).ToArray();
                CollectionAssert.AreEqual(talks, talkRows, $"{mission}'s talks");
                Assert.IsFalse(world.MissionPrerequisiteEntries.AsNoTracking().Any(entry => entry.MissionId == mission && entry.ContentRevision == revision),
                    $"{mission}: no prerequisite");
                Assert.IsTrue(harness.Manager.LoadedMissions[mission].IsOperational, harness.Manager.LoadedMissions[mission].OperationalDiagnostic);
            }

            var markers = world.MissionIndicatorEntries.AsNoTracking()
                .Where(entry => entry.ContentRevision == revision && Missions.Contains(entry.MissionId)).AsEnumerable()
                .Select(entry => (entry.MissionId, entry.ObjectiveId, entry.IndicatorId,
                    Map: MissionMarkerProjection.ResolveMap(catalog, entry.MissionId, entry.ObjectiveId, new MissionIndicator { IndicatorId = entry.IndicatorId })))
                .ToArray();
            Assert.HasCount(8 + 0 + 8, markers, "the set's markers");
            foreach (var (mission, objective, indicator, map) in markers)
            {
                Assert.AreEqual(Map, map, $"{mission}.{objective}: on Avernus Outpost");
                Assert.IsGreaterThan(MissionIndicator.UnnamedFrom, indicator, $"{mission}.{objective}: unnamed");
                Assert.IsNull(AshenMissionTests.NamedMarker(mission, objective), $"{mission}.{objective}: no client name");
            }
        }

        /// <summary>
        /// The set's creatures in the id block 790640-790679: Karter, Drazen and Petrovich's two rows friendly with the client's
        /// names and packages (9281/790640, 9671/790641, 9672/1803), each alone in his pool on Avernus Outpost; the squad's two rows
        /// (9674 the officer, 9673 the soldier) friendly, with no pool. The Atta Teleporter's pads: two local teleporters (type 1,
        /// class 25302) on Avernus Outpost linked both ways and to nothing else, each pad's arrival off the other's trigger. The
        /// NPCs and pads 34 m or more outside every hostile pool's circle. Every Avernus Outpost pool holds the warcry's Atta
        /// Harvester (531016): the 100 m warcry rule cannot hold for the hive's NPCs (docs/ashen-playtest.md).
        /// </summary>
        [TestMethod]
        public void TheSetsCreaturesAndPadsStandApart()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            var rows = world.CreatureEntries.AsNoTracking().Where(row => row.Id >= 790640 && row.Id <= 790649).ToList();
            CollectionAssert.AreEquivalent(N.Creatures, rows.Select(row => row.Id).ToArray(), "the seven");
            foreach (var (id, name, package) in new[] { (N.Karter, 9281U, 790640U), (N.Drazen, 9671U, 790641U), (N.Petrovich, 9672U, 1803U), (N.PetrovichTop, 9672U, 1803U) })
            {
                var row = rows.Single(entry => entry.Id == id);
                Assert.AreEqual((1U, name), ((uint)row.Faction, row.NameId), $"{id}: friendly, the client's name");
                Assert.AreEqual(package, world.NpcPackageEntries.AsNoTracking().Single(entry => entry.Id == id).PackageId, $"{id}'s package");
            }
            foreach (var (id, name) in new[] { (N.Officer, 9674U), (N.Soldier, 9673U) })
            {
                var row = rows.Single(entry => entry.Id == id);
                Assert.AreEqual((1U, name, N.SquadLevel), ((uint)row.Faction, row.NameId, row.Level), $"{id}: friendly, the client's name");
                Assert.IsFalse(world.NpcPackageEntries.AsNoTracking().Any(entry => entry.Id == id), $"{id}: no package");
            }

            var all = world.SpawnPoolEntries.AsNoTracking().Where(pool => pool.Id >= 790640 && pool.Id <= 790649).ToList();
            CollectionAssert.AreEquivalent(Pools.Concat(N.InvaderPools.Select(pool => pool.Pool)).ToArray(), all.Select(pool => pool.Id).ToArray(), "the set's pools");
            var pools = all.Where(pool => Pools.Contains(pool.Id)).ToList();
            foreach (var pool in pools)
                Assert.AreEqual((Map, pool.Id, (byte)1, (byte)1, 0.0), (pool.MapContextId, pool.Creature1Id, pool.Creature1MinCount, pool.Creature1MaxCount, pool.Radius),
                    $"{pool.Id}: alone, on Avernus Outpost");
            var hostile = HostilePools(harness);
            foreach (var pool in pools)
                Assert.IsGreaterThanOrEqualTo(34f, hostile.Min(other => Facility.Gap(new((float)pool.PosX, (float)pool.PosY, (float)pool.PosZ), other)),
                    $"{pool.Id}: 34 m+ outside every hostile pool's circle");
            var criers = AshenFixFTests.WarcryCreatures(harness);

            // The Avernus Invaders: the Atta Soldier's row (class, actions, stats) under the client's name 9675, level 41, hostile, no warcry.
            var soldier = world.CreatureEntries.AsNoTracking().Single(row => row.Id == N.AttaSoldier);
            var invader = rows.Single(row => row.Id == N.Invader);
            Assert.AreEqual((soldier.ClassId, soldier.Faction, N.InvaderLevel, N.InvaderName, soldier.MaxHitPoints, soldier.Action1, soldier.Action2, soldier.Action3),
                (invader.ClassId, invader.Faction, invader.Level, invader.NameId, invader.MaxHitPoints, invader.Action1, invader.Action2, invader.Action3), "the Invader: the Atta Soldier's row");
            Assert.IsFalse(criers.Contains(N.Invader), "no warcry");
            var invaders = all.Where(pool => !Pools.Contains(pool.Id)).ToList();
            Assert.HasCount(3, invaders);
            foreach (var pool in invaders)
            {
                Assert.AreEqual((Map, N.Invader, (byte)2, (byte)3, 5.0, 1200U), (pool.MapContextId, pool.Creature1Id, pool.Creature1MinCount, pool.Creature1MaxCount, pool.Radius, (uint)pool.RespawnTime),
                    $"{pool.Id}: two or three Invaders on Avernus Outpost");
                Assert.IsTrue(hostile.Any(other => other.Id == pool.Id), $"{pool.Id}: hostile");
                var at = new Vector3((float)pool.PosX, (float)pool.PosY, (float)pool.PosZ);
                foreach (var other in hostile.Where(other => other.Id != pool.Id))
                    Assert.IsGreaterThanOrEqualTo(60f, Facility.Gap(at, other) - (float)pool.Radius, $"{pool.Id}: 60 m+ outside hostile pool {other.Id}'s circle");
            }
            // Set 18's Bane pool (790658, AshenAvernusHiveTests) is no warcry pool: 100 m+ outside every one.
            Assert.IsTrue(hostile.Where(pool => !invaders.Any(one => one.Id == pool.Id) && pool.Id != AshenAvernusHiveNpcsV1.BanePool).All(pool => new[] { pool.Creature1Id, pool.Creature2Id, pool.Creature3Id, pool.Creature4Id, pool.Creature5Id, pool.Creature6Id }.Any(criers.Contains)),
                "every upstream hostile pool a warcry pool (the rule break)");

            var pads = world.TeleporterEntries.AsNoTracking().Where(row => row.Id == N.CavernPad || row.Id == N.GraveyardPad).OrderBy(row => row.Id).ToList();
            Assert.HasCount(2, pads);
            foreach (var (row, at) in pads.Zip(new[] { V(M.CavernPadAt), V(M.GraveyardPadAt) }))
            {
                Assert.AreEqual((Map, N.PadClass, N.LocalTeleporter, 0.0), (row.MapContextId, row.ClassId, (uint)row.Type, row.Rotation), $"{row.Id}");
                var place = new Vector3((float)row.PosX, (float)row.PosY, (float)row.PosZ);
                Assert.IsLessThan(0.01f, Vector3.Distance(at, place), $"{row.Id} place");
                // Fix M: pad 915 on the flat floor south-west of Drazen, 26.8 m (3D) outside 531061's circle (a rule break, docs; far
                // from every pool's spawn draws, AshenFixMTests).
                var rule = row.Id == N.CavernPad ? CavernPadClearance : 34f;
                Assert.IsGreaterThanOrEqualTo(rule, hostile.Min(other => Facility.Gap(place, other)), $"pad {row.Id}: {rule} m+ outside every hostile pool's circle");
            }
            int Links(string where) => world.Database.SqlQueryRaw<int>($"SELECT COUNT(*) AS Value FROM local_teleporter_link WHERE {where}").AsEnumerable().Single();
            Assert.AreEqual(1, Links($"from_teleporter_id = {N.CavernPad} AND to_teleporter_id = {N.GraveyardPad}"), "cavern to graveyard");
            Assert.AreEqual(1, Links($"from_teleporter_id = {N.GraveyardPad} AND to_teleporter_id = {N.CavernPad}"), "graveyard to cavern");
            Assert.AreEqual(2, Links($"from_teleporter_id IN ({N.CavernPad}, {N.GraveyardPad}) OR to_teleporter_id IN ({N.CavernPad}, {N.GraveyardPad})"), "no other link");
            Assert.IsFalse(world.TeleporterEntries.AsNoTracking().Where(row => row.MapContextId == Map && row.Id != N.CavernPad && row.Id != N.GraveyardPad).AsEnumerable().Any(row =>
                (uint)row.Type == 1 && (Vector2.Distance(new((float)row.PosX, (float)row.PosZ), new((float)M.CavernPadAt.X, (float)M.CavernPadAt.Z)) < 10 ||
                    Vector2.Distance(new((float)row.PosX, (float)row.PosZ), new((float)M.GraveyardPadAt.X, (float)M.GraveyardPadAt.Z)) < 10)), "no other pad by them");
        }

        /// <summary>
        /// In the squad's copy, entered by its door from Ashen Desert (map_link 59): Karter, Drazen and Petrovich's two rows come out
        /// friendly, each on the navmesh's ground, and the player can talk to them; the Invaders' pools give two or three hostile ones.
        /// </summary>
        [TestMethod]
        public void TheSetsCreaturesStandInTheSquadsCopy()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var copy = IntoOutpost(harness);
            Spawn(copy, Pools);
            foreach (var id in Pools)
            {
                var npc = Here(copy).Single(one => one.SpawnPool?.DbId == id);
                Assert.AreEqual((id, TargetCategory.Friendly), (npc.DbId, npc.TargetCategory), $"{id}");
                Assert.IsNotNull(npc.Npc, $"{id} talks");
                AssertOnGround(copy, npc.Position, $"{id}");
            }
            // The Avernus Invaders: two or three hostile ones by each pool, on the navmesh's ground.
            var invaders = N.InvaderPools.Select(pool => pool.Pool).ToArray();
            Spawn(copy, invaders);
            foreach (var id in invaders)
            {
                var group = Here(copy).Where(one => one.SpawnPool?.DbId == id).ToList();
                Assert.IsTrue(group.Count is >= 2 and <= 3, $"{id}: {group.Count} Invaders");
                foreach (var one in group)
                {
                    Assert.AreEqual((N.Invader, TargetCategory.Hostile), (one.DbId, one.TargetCategory), $"{id}");
                    AssertOnGround(copy, one.Position, $"{id}'s Invader");
                }
            }
        }

        /// <summary>
        /// The Atta Teleporter: a player who steps onto Drazen's pad is held and set down on the graveyard's, not sent straight back;
        /// off the pad (the walk to Petrovich's first steps) and on again, he is taken back to Drazen's cavern.
        /// </summary>
        [TestMethod]
        public void TheAttaTeleporterTakesThePlayerToTheGraveyardAndBack()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var copy = IntoOutpost(harness);
            Pads(harness, copy);
            Assert.IsTrue(copy.Teleporters.TryGetValue(N.CavernPad, out var cavern), "Drazen's pad");
            Assert.IsTrue(copy.Teleporters.TryGetValue(N.GraveyardPad, out var graveyard), "the graveyard's pad");

            Teleport(harness, copy, cavern);
            Assert.IsLessThan(3f, Vector3.Distance(V(M.GraveyardPadAt), harness.Client.Player.Position), "in the graveyard");
            harness.Objects.DynamicObjectProximityWorker(copy, graveyard, 0);
            Assert.IsNull(harness.Client.PendingTransfer, "not sent straight back");
            harness.MoveTo(V(M.PetrovichAt));
            harness.Drain();
            harness.Objects.DynamicObjectProximityWorker(copy, graveyard, 0);
            Assert.IsNull(harness.Client.PendingTransfer, "off the pad");
            Teleport(harness, copy, graveyard);
            Assert.IsLessThan(3f, Vector3.Distance(V(M.CavernPadAt), harness.Client.Player.Position), "back in Drazen's cavern");
            var up = new Vector3(0, 1, 0);
            Assert.IsGreaterThan(2f, Vector3.Distance(V(M.GraveyardPadAt) + up, V(M.CavernPadAt)), "the arrivals off the other trigger");
        }

        /// <summary>
        /// The set's places on the client's floor: each on the navmesh's ground (within 1 m; the prison's inside, the Food Storage
        /// shelf and the tunnel's turn as the navmesh has them) and a client floor (within 1 m), with no client surface over its head
        /// within 2.2 m; the NPCs and the objects one uses or shoots seen from all 16 places 2 m round it (line of sight: to-fix row
        /// 26); each 34 m or more outside every hostile pool's circle (a pool on another floor, 12 m and more above or below, in
        /// 3D). The markers on the navmesh and a client floor within 2 m.
        /// </summary>
        [TestMethod]
        public void ThePlacesStandOnTheClientsFloor()
        {
            var dataDir = F.ClientData();
            using var harness = WildernessRuntimeTestHarness.Create();
            var copy = IntoOutpost(harness);
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
                .Select(marker => (Label: $"{marker.MissionId}.{marker.ObjectiveId}/{marker.IndicatorId}", At: new Vector3((float)marker.PosX, (float)marker.PosY, (float)marker.PosZ),
                    Reach: 2f))
                .ToList();
            Assert.HasCount(16, markers, "the set's markers");
            // A cave pool's marker stands at its pool's centre (upstream: 11-14 m over the cave's floor for some): checked on the floor below.
            var poolMarkers = AshenFixOV1.CavePools.Where(pool => pool.Pool < AshenFixOV1.WestPool).Select(pool => V(pool.At)).ToList();
            var (markerFloors, _) = F.Surfaces(geometry, markers.Select(marker => (marker.At + new Vector3(0, 2 - F.MaxUnder, 0), F.Walkable)).ToList());
            for (var i = 0; i < markers.Count; i++)
            {
                if (poolMarkers.Any(pool => Vector3.Distance(pool, markers[i].At) < 0.01f))
                    continue;
                if (nav.GroundHeight(markers[i].At + new Vector3(0, 2, 0)) is not { } y || MathF.Abs(y - markers[i].At.Y) > markers[i].Reach)
                    failures.Add($"{markers[i].Label}: navmesh {nav.GroundHeight(markers[i].At + new Vector3(0, 2, 0))}");
                if (markers[i].At.Y - markerFloors[i] > markers[i].Reach)
                    failures.Add($"{markers[i].Label}: client floor {markerFloors[i]}");
            }
            Assert.HasCount(27, places);
            Assert.IsEmpty(failures, $"{failures.Count}: " + string.Join("; ", failures));
        }

        /// <summary>
        /// The walks on the client's floor (AshenFixBTests.ClientFloorLine with a player's half width, PlayerHalfWidth 0.5 m, fix
        /// G's 55-degree climb rule, fix J's rampFoot rule since fix M), every leg there and back by the corners Legs names (a client-floor flood's way, 2026-11-12),
        /// also with its start moved 0.13, 0.27 and 0.4 m along its first stretch (StartShifts). No leg goes into or out of a room
        /// the real client keeps closed (ClientClosedRooms: the prison, fix L; the server's collision has it open).
        /// Every walk keeps HostileClearance (30 m, 2D) outside the circle of every hostile pool but those it names (the upstream
        /// pools on the only way: 531047 in the tunnel from the outpost to Drazen's cavern and 531061 by the ramp down into it, 531042 over the hive tunnel's top half;
        /// docs/ashen-playtest.md). The outpost and Drazen's cavern are one walk area; the graveyard is reached only by the pad.
        /// </summary>
        [TestMethod]
        public void ThePlacesAreWalkedToOnTheClientsFloor()
        {
            var dataDir = F.ClientData();
            using var harness = WildernessRuntimeTestHarness.Create();
            using var meshes = new MeshLibrary(dataDir, F.MeshCsv());
            var geometry = MapGeometry.Load(Path.Combine(dataDir, "maps", Facility.MapName(harness, Map)), meshes, 1);
            var onPads = AshenFixMTests.AddPads(harness, geometry, meshes);
            var hostile = HostilePools(harness);
            var failures = new List<string>();
            var walked = 0;
            var expected = 0;
            foreach (var (leg, corners0) in Legs)
            {
                var corners = onPads(corners0);
                // Each leg as given and with its start moved StartShifts along its first stretch: the samples (every 0.5 m) fall
                // elsewhere, so a step or a wall the samples happen to straddle is not missed (audit S-B follow-up).
                var first = Vector2.Distance(new(corners[0].X, corners[0].Z), new(corners[1].X, corners[1].Z));
                foreach (var shift in StartShifts.Where(shift => shift == 0 || first > shift + 0.05f))
                {
                    expected += 2;
                    var there = corners.ToList();
                    there[0] = Vector3.Lerp(corners[0], corners[1], shift / first);
                    var back = Enumerable.Reverse(there).ToList();
                    foreach (var (line, direction) in new[] { (there, "there"), (back, "back") })
                    {
                        walked++;
                        if (AshenFixBTests.ClientFloorLine(geometry, line, line[^1], null, PlayerHalfWidth, rampFoot: true) is { } why)
                            failures.Add(FormattableString.Invariant($"{leg} {direction} (start +{shift} m): {why}"));
                        if (ClosedRoomCrossing(line) is { } wall)
                            failures.Add(FormattableString.Invariant($"{leg} {direction} (start +{shift} m): {wall}"));
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
        /// Petrovich's two routes (1535) as the scene's route controller starts them (SceneRouteController.Start): each waypoint on
        /// the navmesh by a complete path from the one before (from his place in the graveyard; from the turn), its end within
        /// 0.5 m of the waypoint and its height within 0.5 m. The squad's places by Petrovich on the navmesh's ground.
        /// </summary>
        [TestMethod]
        public void PetrovichsRoutesAreOnTheNavmesh()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var copy = IntoOutpost(harness);
            var nav = copy.NavMesh;
            var failures = new List<string>();
            foreach (var (name, from, points) in new[] { ("up the tunnel", V(M.PetrovichAt), Run.TunnelWay), ("to the top", ToVector(Run.RestAt), Run.TopWay) })
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
            foreach (var at in M.SoldiersAt.Append(M.OfficerAt).Select(point => ToVector(point)))
                if (nav.GroundHeight(at + new Vector3(0, 1, 0)) is not { } ground || MathF.Abs(ground - at.Y) > 1f)
                    failures.Add($"the squad's place {at}: navmesh {nav.GroundHeight(at + new Vector3(0, 1, 0))}");
            Assert.IsEmpty(failures, string.Join("; ", failures));
        }

        /// <summary>
        /// Overly Sensitive: Karter offers it in the copy with its four stacks; at the accept the player's own objects stand (the bag
        /// ready, the corpses closed, the servers on, the ammo, black box and stash intact, the teleporter locked to the card); 1-4
        /// and 6 open, 5 waits, 7 hidden. The stash takes no hit before 5 is open; the teleporter refuses the player without the card.
        /// The bag gives the sample once (1); the ammo shot to pieces (2); the tavern corpse gives the diary (3) and opens 5; three
        /// servers leave 4 open at 3 of 4 (a reload keeps them), the fourth completes it; the black box (6); the strip corpse gives the
        /// card (7); the teleporter takes the player into the prison (a move on this map); the stash shot there completes 5. The
        /// turn-in at Karter waits with a full bag, pays the stack chosen once and takes the sample, the diary and the card; the
        /// objects go with the mission.
        /// </summary>
        [TestMethod]
        public void OverlySensitiveTakesTheSecretsOutOfTheOutpost()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var copy = IntoOutpost(harness);
            var karter = Giver(copy, N.Karter);
            var offer = WildernessRanjaGorgeTests.Offer(harness, karter, Overly.MissionId);
            CollectionAssert.AreEqual(new[] { Overly.CryogenicGrenade, Overly.StandardMedPack, Overly.IncendiaryGrenade, Overly.AdvancedMedPack },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Accept(harness, karter, Overly.MissionId);
            Tick(harness, copy, 250);
            Assert.AreEqual((MissionObjectiveState.Incomplete, MissionObjectiveState.Incomplete, MissionObjectiveState.Incomplete, MissionObjectiveState.Incomplete,
                    MissionObjectiveState.Inactive, MissionObjectiveState.Incomplete),
                (Objective(harness, Overly.MissionId, Overly.ArmorSample), Objective(harness, Overly.MissionId, Overly.DestroyAmmo), Objective(harness, Overly.MissionId, Overly.Diary),
                    Objective(harness, Overly.MissionId, Overly.Servers), Objective(harness, Overly.MissionId, Overly.SecretStash), Objective(harness, Overly.MissionId, Overly.DataBackup)),
                "1-4 and 6 open, 5 waits");
            Assert.IsFalse(harness.Manager.BuildStatusSnapshot(harness.Client.Player)[Overly.MissionId].ObjectivesList.Any(objective => objective.ObjectiveId == Overly.KeyCard), "7 is hidden");

            foreach (var (role, objectClass, state) in new[]
                     {
                         (Overly.BagRole, Overly.BagClass, Overly.BagReady), (Overly.DiaryRole, Overly.CorpseClass, Overly.CorpseClosed),
                         (Overly.CardRole, Overly.CorpseClass, Overly.CorpseClosed), (Overly.AmmoRole, Overly.AmmoClass, Overly.IntactState),
                         (Overly.BackupRole, Overly.BlackBoxClass, Overly.IntactState), (Overly.StashRole, Overly.StashClass, Overly.IntactState),
                         (Overly.TeleporterRole, Overly.TeleporterClass, Overly.TeleporterState), (Overly.ServerRole(0), Overly.ServerClass, Overly.ServerOn)
                     })
            {
                var obj = SceneObject(harness, copy, Overly.MissionId, role);
                Assert.AreEqual((objectClass, (UseObjectState)state, harness.Client.Player.Id), ((uint)obj.EntityClassId, obj.StateId, obj.SceneOwnerCharacterId), $"{role}: the player's own");
            }
            var teleporter = SceneObject(harness, copy, Overly.MissionId, Overly.TeleporterRole);
            Assert.AreEqual((Overly.CardTemplate, Overly.TeleporterName), (teleporter.Lock?.KeyItemTemplateId ?? 0, teleporter.NameOverrideId), "the teleporter locked to the card, named");
            Assert.AreEqual(PlayerMessage.PmMissingReqItem, Facility.Refused(harness, copy, teleporter, Overly.UseArgument), "no card: refused");

            // The stash before 5: no hit counts.
            Shoot(harness, copy, SceneObject(harness, copy, Overly.MissionId, Overly.StashRole), (int)Overly.HitPoints + 100);
            Tick(harness, copy, 250);
            Assert.AreEqual(((UseObjectState)Overly.IntactState, MissionObjectiveState.Inactive),
                (SceneObject(harness, copy, Overly.MissionId, Overly.StashRole).StateId, Objective(harness, Overly.MissionId, Overly.SecretStash)), "the stash takes no hit before 5");

            // 1: the bag.
            var bag = SceneObject(harness, copy, Overly.MissionId, Overly.BagRole);
            Use(harness, copy, bag, Overly.BagUseArgument);
            Tick(harness, copy, 250);
            Use(harness, copy, bag, Overly.BagUseArgument);
            Tick(harness, copy, 250);
            Assert.AreEqual((1U, (UseObjectState)Overly.BagTaken, false, MissionObjectiveState.Completed),
                (Facility.Held(harness, Overly.SampleTemplate), bag.StateId, bag.IsEnabled, Objective(harness, Overly.MissionId, Overly.ArmorSample)), "the sample, once; 1 done");

            // 2: the ammo.
            Shoot(harness, copy, SceneObject(harness, copy, Overly.MissionId, Overly.AmmoRole), (int)Overly.HitPoints + 100);
            Tick(harness, copy, 250);
            Assert.AreEqual(((UseObjectState)Overly.DestroyedState, MissionObjectiveState.Completed),
                (SceneObject(harness, copy, Overly.MissionId, Overly.AmmoRole).StateId, Objective(harness, Overly.MissionId, Overly.DestroyAmmo)), "the ammo destroyed, 2 done");

            // 3: the tavern corpse; 5 opens.
            Use(harness, copy, SceneObject(harness, copy, Overly.MissionId, Overly.DiaryRole), Overly.UseArgument);
            Tick(harness, copy, 250);
            Assert.AreEqual((1U, MissionObjectiveState.Completed, MissionObjectiveState.Incomplete),
                (Facility.Held(harness, Overly.DiaryTemplate), Objective(harness, Overly.MissionId, Overly.Diary), Objective(harness, Overly.MissionId, Overly.SecretStash)),
                "the diary, 3 done, 5 open");

            // 4: the servers.
            for (var index = 0; index < Overly.ServersAt.Length; index++)
            {
                Use(harness, copy, SceneObject(harness, copy, Overly.MissionId, Overly.ServerRole(index)), Overly.UseArgument);
                Tick(harness, copy, 250);
                var server = SceneObject(harness, copy, Overly.MissionId, Overly.ServerRole(index));
                Assert.AreEqual(((UseObjectState)Overly.ServerOff, false), (server.StateId, server.IsEnabled), $"server {index + 1} shut down");
                Assert.AreEqual(index == Overly.ServersAt.Length - 1 ? MissionObjectiveState.Completed : MissionObjectiveState.Incomplete,
                    Objective(harness, Overly.MissionId, Overly.Servers), $"4 after server {index + 1}");
                if (index == 2)
                {
                    Assert.AreEqual(3U, Counter(harness, Overly.MissionId, Overly.Servers), "3 of 4");
                    ReloadAndRecover(harness, copy);
                    Facility.Restart(harness, copy, Overly.MissionId);
                    Assert.AreEqual((3U, (UseObjectState)Overly.ServerOff), (Counter(harness, Overly.MissionId, Overly.Servers),
                        SceneObject(harness, copy, Overly.MissionId, Overly.ServerRole(2)).StateId), "kept at a reload and a restart");
                }
            }

            // 6: the black box.
            Shoot(harness, copy, SceneObject(harness, copy, Overly.MissionId, Overly.BackupRole), (int)Overly.HitPoints + 100);
            Tick(harness, copy, 250);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Overly.MissionId, Overly.DataBackup), "6 done");

            // 7: the strip corpse; the teleporter into the prison; 5.
            Use(harness, copy, SceneObject(harness, copy, Overly.MissionId, Overly.CardRole), Overly.UseArgument);
            Tick(harness, copy, 250);
            Assert.AreEqual((1U, MissionObjectiveState.Completed), (Facility.Held(harness, Overly.CardTemplate), Objective(harness, Overly.MissionId, Overly.KeyCard)), "the card");
            Use(harness, copy, SceneObject(harness, copy, Overly.MissionId, Overly.TeleporterRole), Overly.UseArgument);
            Tick(harness, copy, 250);
            Teleported(harness, ToVector(Overly.PrisonArrival), "taken into the prison");
            Assert.AreEqual(1U, Facility.Held(harness, Overly.CardTemplate), "the card kept");
            Shoot(harness, copy, SceneObject(harness, copy, Overly.MissionId, Overly.StashRole), (int)Overly.HitPoints + 100);
            Tick(harness, copy, 250);
            Assert.AreEqual(((UseObjectState)Overly.DestroyedState, MissionObjectiveState.Completed),
                (SceneObject(harness, copy, Overly.MissionId, Overly.StashRole).StateId, Objective(harness, Overly.MissionId, Overly.SecretStash)), "the stash destroyed, 5 done");
            Assert.IsTrue(harness.Client.Player.Missions[Overly.MissionId].Completeable, "to be turned in");

            // Fix L (to-fix row 46): the prison is closed in the client; its way out (923) sets the player down by the inbound teleporter (924).
            Pads(harness, copy);
            Teleport(harness, copy, copy.Teleporters[AshenFixLV1.PrisonPad]);
            Assert.IsLessThan(1.01f, Vector3.Distance(V(AshenFixLV1.OutsidePadAt), harness.Client.Player.Position), "out of the prison, by the inbound teleporter");

            karter = Giver(copy, N.Karter);
            FullBagWaits(harness, karter, Overly.MissionId, 0, ConsumableFiller);
            FailedTurnInPaysNothing(harness, karter, Overly.MissionId, 0);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, karter, Overly.MissionId, 0, Overly.Experience, Overly.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Overly.MissionId), "MissionRewarded once");
            Assert.AreEqual((Overly.CryogenicGrenades, 0U, 0U, 0U), (Facility.Held(harness, Overly.CryogenicGrenade), Facility.Held(harness, Overly.SampleTemplate),
                Facility.Held(harness, Overly.DiaryTemplate), Facility.Held(harness, Overly.CardTemplate)), "the three grenades; the sample, diary and card taken");
            for (var tick = 0; tick < 4; tick++)
                Tick(harness, copy, 250);
            foreach (var role in Overly.ObjectRoles)
                Assert.IsNull(FindSceneObject(harness, copy, Overly.MissionId, role), $"{role} goes with the mission");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, Giver(copy, N.Karter), Overly.MissionId), "done once");
        }

        /// <summary>
        /// Overly Sensitive in a squad of three on it: the player's bag, tavern corpse and strip corpse give each member his own
        /// sample, diary and card (each member's 1, 3 and 7); the player's server use counts for the mates. A mate away at the
        /// player's searches keeps his objectives open and gets his items from his own objects; a member without 1473 is shown
        /// none of it and gets nothing.
        /// </summary>
        [TestMethod]
        public void OverlySensitivesItemsComeToEverySquadMateOnIt()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var copy = IntoOutpost(harness);
            var karter = Giver(copy, N.Karter);
            Accept(harness, karter, Overly.MissionId);
            Tick(harness, copy, 250);
            using var mate = new Facility.SquadMate(harness, copy, karter, Overly.MissionId, Array.Empty<uint>());
            using var late = new Facility.SquadMate(harness, copy, karter, Overly.MissionId, Array.Empty<uint>(), 3, mate.PartyId);
            using var stranger = new Facility.SquadMate(harness, copy, karter, 0, Array.Empty<uint>(), 4, mate.PartyId);
            Assert.IsFalse(DynamicObjectManager.IsShownTo(stranger.Client, SceneObject(harness, copy, Overly.MissionId, Overly.BagRole)), "a member without 1473: no bag");

            late.Away();
            Use(harness, copy, SceneObject(harness, copy, Overly.MissionId, Overly.BagRole), Overly.BagUseArgument);
            Tick(harness, copy, 250);
            Use(harness, copy, SceneObject(harness, copy, Overly.MissionId, Overly.DiaryRole), Overly.UseArgument);
            Tick(harness, copy, 250);
            Use(harness, copy, SceneObject(harness, copy, Overly.MissionId, Overly.CardRole), Overly.UseArgument);
            Tick(harness, copy, 250);
            Use(harness, copy, SceneObject(harness, copy, Overly.MissionId, Overly.ServerRole(0)), Overly.UseArgument);
            Tick(harness, copy, 250);
            Assert.AreEqual((MissionObjectiveState.Completed, MissionObjectiveState.Completed, MissionObjectiveState.Completed, MissionObjectiveState.Incomplete),
                (mate.Of(Overly.ArmorSample), mate.Of(Overly.Diary), mate.Of(Overly.KeyCard), mate.Of(Overly.SecretStash)), "the mate's 1, 3 and 7 done, his 5 open");
            Assert.AreEqual((1U, 1U, 1U), (mate.Held(Overly.SampleTemplate), mate.Held(Overly.DiaryTemplate), mate.Held(Overly.CardTemplate)), "the mate's own sample, diary and card");
            Assert.AreEqual((1U, 1U, 1U), (Facility.Held(harness, Overly.SampleTemplate), Facility.Held(harness, Overly.DiaryTemplate), Facility.Held(harness, Overly.CardTemplate)),
                "the player's own, one each");
            Assert.AreEqual(1U, mate.Client.Player.Missions[Overly.MissionId].Objectives[Overly.Servers].Counters[0], "the player's server counts for the mate");
            Assert.AreEqual((0U, 0U, 0U), (stranger.Held(Overly.SampleTemplate), stranger.Held(Overly.DiaryTemplate), stranger.Held(Overly.CardTemplate)), "nothing for the stranger");

            late.Back();
            Assert.AreEqual((MissionObjectiveState.Incomplete, 0U), (late.Of(Overly.ArmorSample), late.Held(Overly.SampleTemplate)), "away at the searches: his 1 open, no sample");
            late.Use(late.Door(Overly.BagRole), Overly.BagUseArgument);
            late.Use(late.Door(Overly.DiaryRole));
            late.Use(late.Door(Overly.CardRole));
            Assert.AreEqual((MissionObjectiveState.Completed, MissionObjectiveState.Completed, MissionObjectiveState.Completed),
                (late.Of(Overly.ArmorSample), late.Of(Overly.Diary), late.Of(Overly.KeyCard)), "his own objects complete his own 1, 3 and 7");
            Assert.AreEqual((1U, 1U, 1U), (late.Held(Overly.SampleTemplate), late.Held(Overly.DiaryTemplate), late.Held(Overly.CardTemplate)), "his own items");
            Assert.AreEqual((1U, 1U, 1U), (mate.Held(Overly.SampleTemplate), mate.Held(Overly.DiaryTemplate), mate.Held(Overly.CardTemplate)), "the mate's not given twice");
        }

        /// <summary>
        /// The card holder leaves: the player abandons 1473 with the card held; his card and objects go, the mate keeps his own card
        /// and is taken into the prison by his own teleporter.
        /// </summary>
        [TestMethod]
        public void OverlySensitivesMateGetsInWhenThePlayerAbandons()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var copy = IntoOutpost(harness);
            var karter = Giver(copy, N.Karter);
            Accept(harness, karter, Overly.MissionId);
            Tick(harness, copy, 250);
            using var mate = new Facility.SquadMate(harness, copy, karter, Overly.MissionId, Array.Empty<uint>());
            Use(harness, copy, SceneObject(harness, copy, Overly.MissionId, Overly.DiaryRole), Overly.UseArgument);
            Tick(harness, copy, 250);
            Use(harness, copy, SceneObject(harness, copy, Overly.MissionId, Overly.CardRole), Overly.UseArgument);
            Tick(harness, copy, 250);
            Assert.AreEqual((1U, 1U), (Facility.Held(harness, Overly.CardTemplate), mate.Held(Overly.CardTemplate)), "a card each");
            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Overly.MissionId));
            harness.Drain();
            for (var tick = 0; tick < 4; tick++)
                Tick(harness, copy, 250);
            Assert.AreEqual((0U, 0U, 1U), (Facility.Held(harness, Overly.CardTemplate), Facility.Held(harness, Overly.DiaryTemplate), mate.Held(Overly.CardTemplate)),
                "the player's card and diary taken at the abandon, the mate's card kept");
            Assert.IsNull(FindSceneObject(harness, copy, Overly.MissionId, Overly.TeleporterRole), "the player's teleporter goes with the abandon");
            mate.Use(mate.Door(Overly.TeleporterRole));
            var transfer = mate.Client.PendingTransfer;
            Assert.IsNotNull(transfer, "the mate's own teleporter takes him");
            Assert.IsLessThan(0.01f, Vector3.Distance(transfer.DestinationPosition, ToVector(Overly.PrisonArrival)), "into the prison");
            mate.Client.PendingTransfer = null;
        }

        /// <summary>Overly Sensitive with a full mission-item bag: the courier bag's search waits (no sample, 1 open); a slot free, it gives the sample.</summary>
        [TestMethod]
        public void OverlySensitivesFullBagWaitsAtTheCourierBag()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var copy = IntoOutpost(harness);
            Accept(harness, Giver(copy, N.Karter), Overly.MissionId);
            Tick(harness, copy, 250);
            Facility.FullBagWaitsAtASearch(harness, copy, Overly.MissionId, Overly.BagRole, Overly.SampleTemplate, Overly.ArmorSample,
                (UseObjectState)Overly.BagReady, (UseObjectState)Overly.BagTaken, Overly.BagUseArgument);
        }

        /// <summary>
        /// Petrovich's Last Stand: Drazen offers it with its four stacks; taken, it is completeable at once (no client objective:
        /// the hidden one is not required) and the client is sent none; the Atta Teleporter takes the player to the graveyard; the
        /// walk to Petrovich completes the hidden one; the turn-in at Petrovich pays the stack chosen once; Petrovich offers 1535.
        /// </summary>
        [TestMethod]
        public void PetrovichsLastStandSendsThePlayerThroughTheAttaTeleporter()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var copy = IntoOutpost(harness);
            Pads(harness, copy);
            var drazen = Giver(copy, N.Drazen);
            var offer = WildernessRanjaGorgeTests.Offer(harness, drazen, Last.MissionId);
            CollectionAssert.AreEqual(new[] { Last.EmpGrenade, Last.ResTraumaKit, Last.ConcussionGrenade, Last.BasicMedPack },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Accept(harness, drazen, Last.MissionId);
            Tick(harness, copy, 250);
            Assert.IsTrue(harness.Client.Player.Missions[Last.MissionId].Completeable, "completeable at the accept");
            Assert.IsEmpty(harness.Manager.BuildStatusSnapshot(harness.Client.Player)[Last.MissionId].ObjectivesList, "no objective for the client");

            Teleport(harness, copy, copy.Teleporters[N.CavernPad]);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Last.MissionId, Last.InTheGraveyard), "the pad's arrival is short of Petrovich");
            Enter(harness, V(M.PetrovichAt));
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Last.MissionId, Last.InTheGraveyard), "at Petrovich");
            var petrovich = Giver(copy, N.Petrovich);
            FullBagWaits(harness, petrovich, Last.MissionId, 1, ConsumableFiller);
            FailedTurnInPaysNothing(harness, petrovich, Last.MissionId, 1);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, petrovich, Last.MissionId, 1, Last.Experience, Last.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Last.MissionId), "MissionRewarded once");
            Assert.AreEqual(Last.ResTraumaKits, Facility.Held(harness, Last.ResTraumaKit), "the kit chosen");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, Giver(copy, N.Drazen), Last.MissionId), "done once");
            Assert.IsTrue(PlateauRetreadCampTests.Offers(harness, Giver(copy, N.Petrovich), Run.MissionId), "Petrovich offers his run");
        }

        /// <summary>
        /// Petrovich's Run: Petrovich in the graveyard offers it with its four stacks; at the accept his copy (the player's own, the
        /// public one hidden) and his squad stand by him, unhurt by anything; 1 open, 2-5 wait, 6 and 7 hidden. Three of the cave's
        /// four pools dead leave 1 open (a reload keeps it); the fourth completes it and opens 2: Petrovich leads up the tunnel, the
        /// squad follows the player. The turn completes 2 and opens 3; Petrovich arrives there; his talk opens 4 and brings two Atta
        /// Soldiers at the player; both dead bring two more (never four at once); those dead complete 4 and open 5; Petrovich leads
        /// on to the top. The top completes 5: the copy and the squad go, the public Petrovich is shown again; the turn-in at his
        /// second row pays the stack chosen once.
        /// </summary>
        [TestMethod]
        public void PetrovichsRunTakesTheSquadUpTheTunnel()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var copy = IntoOutpost(harness);
            ResetCave(copy);
            var petrovich = Giver(copy, N.Petrovich);
            var offer = WildernessRanjaGorgeTests.Offer(harness, petrovich, Run.MissionId);
            CollectionAssert.AreEqual(new[] { Run.FragGrenade, Run.EmpBomb, Run.CryogenicGrenade, Run.StandardMedPack },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Accept(harness, petrovich, Run.MissionId);
            Tick(harness, copy, 250);
            Assert.AreEqual((MissionObjectiveState.Incomplete, MissionObjectiveState.Inactive, MissionObjectiveState.Inactive, MissionObjectiveState.Inactive, MissionObjectiveState.Inactive),
                (Objective(harness, Run.MissionId, Run.ClearTheCave), Objective(harness, Run.MissionId, Run.TunnelRun), Objective(harness, Run.MissionId, Run.Rest),
                    Objective(harness, Run.MissionId, Run.FendOff), Objective(harness, Run.MissionId, Run.ToTheTop)), "1 open, 2-5 wait");
            var visible = harness.Manager.BuildStatusSnapshot(harness.Client.Player)[Run.MissionId].ObjectivesList.Select(objective => objective.ObjectiveId).ToArray();
            Assert.IsFalse(new[] { Run.FirstWaveDown, Run.SecondWaveDown, Run.Ready, Run.Tracker, Run.Leads }.Any(visible.Contains), "6-10 hidden");

            var leader = SceneCreature(harness, copy, Run.MissionId, Run.PetrovichRole);
            Assert.IsNotNull(leader, "Petrovich's copy");
            Assert.AreEqual((N.Petrovich, N.PetrovichPackage), (leader.DbId, leader.Npc?.NpcPackageId), "a copy of his row");
            Assert.IsTrue(MissionNpcVisibility.IsShownTo(harness.Client, leader), "the copy is shown to the player");
            Assert.IsFalse(MissionNpcVisibility.IsShownTo(harness.Client, petrovich), "the public Petrovich hidden while the copy lives");
            var squad = new[] { Run.OfficerRole, Run.SoldierRole(0), Run.SoldierRole(1) };
            foreach (var role in squad.Append(Run.PetrovichRole))
            {
                var member = SceneCreature(harness, copy, Run.MissionId, role);
                Assert.IsNotNull(member, $"{role} stands");
                Assert.AreEqual(TargetCategory.Friendly, member.TargetCategory, $"{role}: friendly");
                Assert.IsTrue(Rasa.Game.Missions.World.CreatureGameplayRules.IsInvulnerable(member), $"{role}: unhurt by anything");
            }

            // 1: the cave's four pools (fix O: 790700 and 790701 for upstream 531049 and 531060, whose islands no longer count).
            CollectionAssert.AreEqual(new uint[] { AshenFixOV1.WestPool, 531050, AshenFixOV1.EastPool, 531063 }, AshenFixOV1.CavePools.Select(pool => pool.Pool).ToArray(), "the four pools");
            // Set 17's four (the islands, 531050, 531063) dead at once: they would have completed 1; fix O's triggers do not.
            KillPools(harness, copy, new uint[] { AshenFixOV1.WestIsland, AshenFixOV1.EastIsland, 531050, 531063 });
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Run.MissionId, Run.ClearTheCave), "the islands' pools no longer count");
            // 531063 back (a pool counts while it is dead), so that the next three kills leave one of the four standing.
            foreach (var pool in copy.SpawnPools.Where(pool => pool.DbId == 531063))
                pool.ClearedAtUtcMs = 0;
            Spawn(copy, new uint[] { 531063 });
            Assert.IsTrue(Here(copy).Any(creature => creature.SpawnPool?.DbId == 531063 && creature.State != CharacterState.Dead), "531063 stands again");
            KillPools(harness, copy, AshenFixOV1.CavePools.Take(3).Select(pool => pool.Pool).ToArray());
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Run.MissionId, Run.ClearTheCave), "three pools of four");
            ReloadAndRecover(harness, copy);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Run.MissionId, Run.ClearTheCave), "kept at a reload");
            KillPools(harness, copy, AshenFixOV1.CavePools.Skip(3).Select(pool => pool.Pool).ToArray());
            for (var tick = 0; tick < 12; tick++)       // the squad's ready timer (2 s) and the lead's start
                Tick(harness, copy, 250);
            Assert.AreEqual((MissionObjectiveState.Completed, MissionObjectiveState.Incomplete),
                (Objective(harness, Run.MissionId, Run.ClearTheCave), Objective(harness, Run.MissionId, Run.TunnelRun)), "the cave cleared, 2 open");
            foreach (var role in squad)
                Assert.AreEqual(harness.Client.Player.Id, SceneCreature(harness, copy, Run.MissionId, role).SpawnPool.FollowOwnerCharacterId, $"{role} follows the player");

            // 2: up the tunnel to the turn.
            harness.MoveTo(V(M.PetrovichAt) + new Vector3(0, 0, 3));
            for (var second = 0; second < 120 && Vector3.Distance(SceneCreature(harness, copy, Run.MissionId, Run.PetrovichRole).Position, ToVector(Run.RestAt)) > 1.5f; second++)
                Tick(harness, copy, 1000);
            Assert.IsLessThanOrEqualTo(1.5f, Vector3.Distance(SceneCreature(harness, copy, Run.MissionId, Run.PetrovichRole).Position, ToVector(Run.RestAt)), "Petrovich at the turn");
            Enter(harness, ToVector(Run.RestAt));
            Assert.AreEqual((MissionObjectiveState.Completed, MissionObjectiveState.Incomplete),
                (Objective(harness, Run.MissionId, Run.TunnelRun), Objective(harness, Run.MissionId, Run.Rest)), "at the turn: 2 done, 3 open");
            Assert.IsNull(SceneCreature(harness, copy, Run.MissionId, Run.AmbushRole(1, 1)), "no ambush before the talk");

            // 3: the talk; 4: the ambush.
            leader = SceneCreature(harness, copy, Run.MissionId, Run.PetrovichRole);
            CollectionAssert.AreEqual(new[] { Run.Rest }, Topics(harness, leader, Run.MissionId), "Petrovich hears 3");
            Talk(harness, leader, Run.MissionId, Run.Rest);
            Tick(harness, copy, 250);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Run.MissionId, Run.FendOff), "4 open");
            for (var wave = 1; wave <= 2; wave++)
            {
                var attackers = Enumerable.Range(1, Run.AmbushAt.Length).Select(member => SceneCreature(harness, copy, Run.MissionId, Run.AmbushRole(wave, member))).ToArray();
                Assert.IsTrue(attackers.All(attacker => attacker != null), $"wave {wave}: two Atta Soldiers");
                Assert.IsNull(SceneCreature(harness, copy, Run.MissionId, Run.AmbushRole(3 - wave, 1)), $"wave {wave}: the other wave not there");
                foreach (var attacker in attackers)
                    Assert.AreEqual((Run.AttaSoldier, TargetCategory.Hostile), (attacker.DbId, attacker.TargetCategory), $"wave {wave}");
                Kill(harness, copy, attackers[0]);
                Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Run.MissionId, Run.FendOff), $"wave {wave}: one of two");
                if (wave == 1)
                {
                    ReloadAndRecover(harness, copy);
                    Assert.IsNotNull(SceneCreature(harness, copy, Run.MissionId, Run.AmbushRole(1, 2)), "the second of the first wave still there after a reload");
                    Assert.IsNull(SceneCreature(harness, copy, Run.MissionId, Run.AmbushRole(2, 1)), "the second wave not yet");
                }
                Kill(harness, copy, SceneCreature(harness, copy, Run.MissionId, Run.AmbushRole(wave, 2)));
                Tick(harness, copy, 250);
            }
            Assert.AreEqual((MissionObjectiveState.Completed, MissionObjectiveState.Incomplete),
                (Objective(harness, Run.MissionId, Run.FendOff), Objective(harness, Run.MissionId, Run.ToTheTop)), "the ambush held off: 4 done, 5 open");

            // 5: on to the top.
            for (var second = 0; second < 120 && Vector3.Distance(SceneCreature(harness, copy, Run.MissionId, Run.PetrovichRole).Position, ToVector(Run.TopWay[^1])) > 1.5f; second++)
                Tick(harness, copy, 1000);
            Assert.IsLessThanOrEqualTo(1.5f, Vector3.Distance(SceneCreature(harness, copy, Run.MissionId, Run.PetrovichRole).Position, ToVector(Run.TopWay[^1])), "Petrovich at the top");
            Enter(harness, V(Run.TopAt));
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Run.MissionId, Run.ToTheTop), "at the top: 5 done");
            Assert.IsTrue(harness.Client.Player.Missions[Run.MissionId].Completeable);
            for (var tick = 0; tick < 4; tick++)
                Tick(harness, copy, 250);
            foreach (var role in squad.Append(Run.PetrovichRole))
                Assert.IsNull(SceneCreature(harness, copy, Run.MissionId, role), $"{role} goes at the top");
            Assert.IsTrue(MissionNpcVisibility.IsShownTo(harness.Client, Giver(copy, N.Petrovich)), "the public Petrovich shown again");

            var top = Giver(copy, N.PetrovichTop);
            FullBagWaits(harness, top, Run.MissionId, 1, ConsumableFiller);
            FailedTurnInPaysNothing(harness, top, Run.MissionId, 1);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, top, Run.MissionId, 1, Run.Experience, Run.Credits);
            Assert.AreEqual(1, harness.Drain().OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == Run.MissionId), "MissionRewarded once");
            Assert.AreEqual(Run.EmpBombs, Facility.Held(harness, Run.EmpBomb), "the bomb chosen");
            Assert.IsFalse(PlateauRetreadCampTests.Offers(harness, Giver(copy, N.Petrovich), Run.MissionId), "done once");
        }

        /// <summary>
        /// Petrovich's Run in a squad of two on it: the cave cleared counts for the mate too (his 1 done, his own Petrovich leads);
        /// the cave cleared before a member's accept counts at his accept (ExistingFactObjectiveIds).
        /// </summary>
        [TestMethod]
        public void PetrovichsRunsCaveCountsForEverySquadMateOnIt()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var copy = IntoOutpost(harness);
            ResetCave(copy);
            var petrovich = Giver(copy, N.Petrovich);
            Accept(harness, petrovich, Run.MissionId);
            Tick(harness, copy, 250);
            using var mate = new Facility.SquadMate(harness, copy, petrovich, Run.MissionId, Array.Empty<uint>());
            KillPools(harness, copy, AshenFixOV1.CavePools.Select(pool => pool.Pool).ToArray());
            for (var tick = 0; tick < 12; tick++)
                Tick(harness, copy, 250);
            Assert.AreEqual((MissionObjectiveState.Completed, MissionObjectiveState.Incomplete), (mate.Of(Run.ClearTheCave), mate.Of(Run.TunnelRun)),
                "the mate's 1 done, his 2 open");
            using var late = new Facility.SquadMate(harness, copy, petrovich, Run.MissionId, Array.Empty<uint>(), 3, mate.PartyId);
            Assert.AreEqual((MissionObjectiveState.Completed, MissionObjectiveState.Inactive), (late.Of(Run.ClearTheCave), late.Of(Run.TunnelRun)),
                "the cave cleared before his accept counts at it; the lead waits for his squad");
            for (var tick = 0; tick < 12; tick++)
                Tick(harness, copy, 250);
            Assert.AreEqual(MissionObjectiveState.Incomplete, late.Of(Run.TunnelRun), "then his 2 opens");
        }

        /// <summary>
        /// Petrovich's Run's ambush in a squad of two on it (audit S-A): the player's talk and his two waves complete his 4; the
        /// mate's hidden 6 and 7 stay Inactive through them (a wave kill reaches every member in the copy), so his 4 still comes
        /// with his own talk at the turn after the player's ambush is over, and his own two waves.
        /// </summary>
        [TestMethod]
        public void PetrovichsRunsAmbushCountsForEachSquadMateOnHisOwn()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var copy = IntoOutpost(harness);
            ResetCave(copy);
            var petrovich = Giver(copy, N.Petrovich);
            Accept(harness, petrovich, Run.MissionId);
            Tick(harness, copy, 250);
            using var mate = new Facility.SquadMate(harness, copy, petrovich, Run.MissionId, Array.Empty<uint>());
            KillPools(harness, copy, AshenFixOV1.CavePools.Select(pool => pool.Pool).ToArray());
            for (var tick = 0; tick < 12; tick++)
                Tick(harness, copy, 250);
            Assert.AreEqual((MissionObjectiveState.Incomplete, MissionObjectiveState.Incomplete), (Objective(harness, Run.MissionId, Run.TunnelRun), mate.Of(Run.TunnelRun)),
                "both 2 open");
            Assert.AreEqual((MissionObjectiveState.Inactive, MissionObjectiveState.Inactive), (mate.Of(Run.FirstWaveDown), mate.Of(Run.SecondWaveDown)), "the mate's 6 and 7 wait");

            // The player's ambush.
            Enter(harness, ToVector(Run.RestAt));
            var leader = SceneCreature(harness, copy, Run.MissionId, Run.PetrovichRole);
            harness.MoveTo(leader.Position);
            harness.Drain();
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, leader.EntityId, Run.MissionId, Run.Rest, 1), "the player's rest talk");
            harness.Drain();
            Tick(harness, copy, 250);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Run.MissionId, Run.FirstWaveDown), "the player's 6 opens with his talk");
            for (var wave = 1; wave <= 2; wave++)
                for (var member = 1; member <= Run.AmbushAt.Length; member++)
                    Kill(harness, copy, SceneCreature(harness, copy, Run.MissionId, Run.AmbushRole(wave, member)));
            Tick(harness, copy, 250);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Run.MissionId, Run.FendOff), "the player's 4 done");
            Assert.AreEqual((MissionObjectiveState.Inactive, MissionObjectiveState.Inactive, MissionObjectiveState.Inactive),
                (mate.Of(Run.FirstWaveDown), mate.Of(Run.SecondWaveDown), mate.Of(Run.FendOff)), "the player's waves do not count for the mate");

            // The mate's own talk and waves.
            mate.Beside(ToVector(Run.RestAt));
            new MissionAreaService(() => harness.Manager).RecordAcceptedMovement(mate.Client, ToVector(Run.RestAt) + new Vector3(60, 0, 60), ToVector(Run.RestAt));
            harness.Drain();
            Assert.AreEqual((MissionObjectiveState.Completed, MissionObjectiveState.Incomplete), (mate.Of(Run.TunnelRun), mate.Of(Run.Rest)), "the mate at the turn: his 3 open");
            var hisLeader = MateSceneCreature(harness, copy, mate, Run.PetrovichRole);
            Assert.IsNotNull(hisLeader, "the mate's own Petrovich");
            mate.Beside(hisLeader.Position);
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(mate.Client, hisLeader.EntityId, Run.MissionId, Run.Rest, 1), "the mate's rest talk");
            harness.Drain();
            Tick(harness, copy, 250);
            Assert.AreEqual((MissionObjectiveState.Incomplete, MissionObjectiveState.Incomplete), (mate.Of(Run.FendOff), mate.Of(Run.FirstWaveDown)), "his 4 and 6 open");
            for (var wave = 1; wave <= 2; wave++)
            {
                for (var member = 1; member <= Run.AmbushAt.Length; member++)
                {
                    var attacker = MateSceneCreature(harness, copy, mate, Run.AmbushRole(wave, member));
                    Assert.IsNotNull(attacker, $"the mate's wave {wave}, {member}");
                    mate.Beside(attacker.Position);
                    attacker.Attributes[Attributes.Health].Current = 0;
                    harness.Creatures.HandleCreatureKill(copy, attacker, mate.Client.Player);
                    harness.Drain();
                    Tick(harness, copy, 250);
                    Tick(harness, copy, 250);
                }
                if (wave == 1)
                    Assert.AreEqual(MissionObjectiveState.Incomplete, mate.Of(Run.SecondWaveDown), "his 7 opens with his second wave");
            }
            Assert.AreEqual((MissionObjectiveState.Completed, MissionObjectiveState.Incomplete), (mate.Of(Run.FendOff), mate.Of(Run.ToTheTop)), "the mate's 4 done, his 5 open");
        }

        /// <summary>
        /// Petrovich's Run's ambush in a squad of two who both talk at the turn before the player's waves die (audit follow-up 3,
        /// as built): a scene event in a squad copy counts for every member on the mission (GroupCreditService, to-fix 29), so the
        /// player's first wave dead completes the mate's 6 too and brings the mate's second wave while his own first still stands
        /// (four of his at once); the player's second wave dead completes the mate's 7 and 4. The mate's own waves stay, no longer
        /// counted; both go on to 5.
        /// </summary>
        [TestMethod]
        public void PetrovichsRunsAmbushIsSharedWhenTheSquadTalksAtOnce()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var copy = IntoOutpost(harness);
            ResetCave(copy);
            var petrovich = Giver(copy, N.Petrovich);
            Accept(harness, petrovich, Run.MissionId);
            Tick(harness, copy, 250);
            using var mate = new Facility.SquadMate(harness, copy, petrovich, Run.MissionId, Array.Empty<uint>());
            KillPools(harness, copy, AshenFixOV1.CavePools.Select(pool => pool.Pool).ToArray());
            for (var tick = 0; tick < 12; tick++)
                Tick(harness, copy, 250);

            Enter(harness, ToVector(Run.RestAt));
            var leader = SceneCreature(harness, copy, Run.MissionId, Run.PetrovichRole);
            harness.MoveTo(leader.Position);
            harness.Drain();
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, leader.EntityId, Run.MissionId, Run.Rest, 1), "the player's rest talk");
            harness.Drain();
            mate.Beside(ToVector(Run.RestAt));
            new MissionAreaService(() => harness.Manager).RecordAcceptedMovement(mate.Client, ToVector(Run.RestAt) + new Vector3(60, 0, 60), ToVector(Run.RestAt));
            harness.Drain();
            var hisLeader = MateSceneCreature(harness, copy, mate, Run.PetrovichRole);
            mate.Beside(hisLeader.Position);
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(mate.Client, hisLeader.EntityId, Run.MissionId, Run.Rest, 1), "the mate's rest talk");
            harness.Drain();
            Tick(harness, copy, 250);
            Assert.AreEqual((MissionObjectiveState.Incomplete, MissionObjectiveState.Incomplete), (Objective(harness, Run.MissionId, Run.FirstWaveDown), mate.Of(Run.FirstWaveDown)), "both 6 open");
            int MatesAttackers() => Enumerable.Range(1, 2).SelectMany(wave => Enumerable.Range(1, Run.AmbushAt.Length).Select(member => MateSceneCreature(harness, copy, mate, Run.AmbushRole(wave, member))))
                .Count(one => one != null);
            Assert.AreEqual(2, MatesAttackers(), "the mate's first wave");

            for (var member = 1; member <= Run.AmbushAt.Length; member++)
                Kill(harness, copy, SceneCreature(harness, copy, Run.MissionId, Run.AmbushRole(1, member)));
            Tick(harness, copy, 250);
            Assert.AreEqual((MissionObjectiveState.Completed, MissionObjectiveState.Completed), (Objective(harness, Run.MissionId, Run.FirstWaveDown), mate.Of(Run.FirstWaveDown)),
                "the player's first wave completes both 6 (shared squad credit)");
            Assert.AreEqual(4, MatesAttackers(), "the mate's second wave comes while his own first still stands: four of his at once");

            for (var member = 1; member <= Run.AmbushAt.Length; member++)
                Kill(harness, copy, SceneCreature(harness, copy, Run.MissionId, Run.AmbushRole(2, member)));
            Tick(harness, copy, 250);
            Assert.AreEqual((MissionObjectiveState.Completed, MissionObjectiveState.Completed, MissionObjectiveState.Incomplete, MissionObjectiveState.Incomplete),
                (Objective(harness, Run.MissionId, Run.FendOff), mate.Of(Run.FendOff), Objective(harness, Run.MissionId, Run.ToTheTop), mate.Of(Run.ToTheTop)),
                "the player's second wave completes both 4; both 5 open");
            Assert.AreEqual(4, MatesAttackers(), "the mate's own four stay, no longer counted");
        }

        /// <summary>
        /// Overly Sensitive in a squad of two: the mate away at the player's card search has no card: his own teleporter refuses him
        /// (no transfer, his 5 open), the player's takes the player.
        /// </summary>
        [TestMethod]
        public void OverlySensitivesTeleporterRefusesAMateWithoutTheCard()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var copy = IntoOutpost(harness);
            var karter = Giver(copy, N.Karter);
            Accept(harness, karter, Overly.MissionId);
            Tick(harness, copy, 250);
            using var mate = new Facility.SquadMate(harness, copy, karter, Overly.MissionId, Array.Empty<uint>());
            Use(harness, copy, SceneObject(harness, copy, Overly.MissionId, Overly.DiaryRole), Overly.UseArgument);
            Tick(harness, copy, 250);
            mate.Away();
            Use(harness, copy, SceneObject(harness, copy, Overly.MissionId, Overly.CardRole), Overly.UseArgument);
            Tick(harness, copy, 250);
            mate.Back();
            Assert.AreEqual((MissionObjectiveState.Incomplete, 0U, 1U), (mate.Of(Overly.SecretStash), mate.Held(Overly.CardTemplate), Facility.Held(harness, Overly.CardTemplate)),
                "the mate's 5 open, no card for him; the player's card");
            mate.Client.PendingTransfer = null;
            mate.Use(mate.Door(Overly.TeleporterRole));
            Assert.IsNull(mate.Client.PendingTransfer, "no card: the mate's teleporter refuses him");
            Assert.AreEqual(MissionObjectiveState.Incomplete, mate.Of(Overly.SecretStash), "his 5 still open");
            Assert.AreEqual((UseObjectState)Overly.TeleporterState, mate.Door(Overly.TeleporterRole).StateId, "his teleporter still locked");
            Use(harness, copy, SceneObject(harness, copy, Overly.MissionId, Overly.TeleporterRole), Overly.UseArgument);
            Assert.IsNotNull(harness.Client.PendingTransfer, "the player's card opens his own");
            harness.Client.PendingTransfer = null;
        }

        /// <summary>
        /// Overly Sensitive in a squad of two: the mate's mission-item bag full at the player's courier bag search: the player gets
        /// his sample; the mate gets none and his 1 stays open; a slot free, the mate's own courier bag gives him his.
        /// </summary>
        [TestMethod]
        public void OverlySensitivesMatesFullBagWaitsAtThePlayersSearch()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var copy = IntoOutpost(harness);
            var karter = Giver(copy, N.Karter);
            Accept(harness, karter, Overly.MissionId);
            Tick(harness, copy, 250);
            using var mate = new Facility.SquadMate(harness, copy, karter, Overly.MissionId, Array.Empty<uint>());
            for (var slot = 0; slot < 60; slot++)
            {
                try { GrantTo(harness, mate.Client, Facility.MissionItemFiller); }
                catch (Exception) { break; }
            }
            Use(harness, copy, SceneObject(harness, copy, Overly.MissionId, Overly.BagRole), Overly.BagUseArgument);
            Tick(harness, copy, 250);
            Assert.AreEqual((MissionObjectiveState.Completed, 1U), (Objective(harness, Overly.MissionId, Overly.ArmorSample), Facility.Held(harness, Overly.SampleTemplate)),
                "the player's sample");
            Assert.AreEqual((MissionObjectiveState.Incomplete, 0U), (mate.Of(Overly.ArmorSample), mate.Held(Overly.SampleTemplate)), "the mate's bag full: no sample, his 1 open");
            var bag = mate.Door(Overly.BagRole);
            Assert.AreEqual(((UseObjectState)Overly.BagReady, true), (bag.StateId, bag.IsEnabled), "his own courier bag still to search");
            FreeFrom(harness, mate.Client, Facility.MissionItemFiller);
            mate.Use(bag, Overly.BagUseArgument);
            Assert.AreEqual((MissionObjectiveState.Completed, 1U), (mate.Of(Overly.ArmorSample), mate.Held(Overly.SampleTemplate)), "a slot free: his own bag gives his sample");
        }

        /// <summary>The mate's own scene creature of <c>role</c> (of his run).</summary>
        private static Creature MateSceneCreature(WildernessRuntimeTestHarness harness, MapChannel copy, Facility.SquadMate mate, string role)
        {
            using var unit = harness.CreateChar();
            var runs = unit.CharacterMissions.Runtime.Scenes(mate.Client.Player.Id, Run.MissionId).Select(scene => scene.RunId).ToHashSet();
            return Here(copy).SingleOrDefault(creature => creature.SpawnPool?.SceneActorRole == role && runs.Contains(creature.SpawnPool.SceneRunId) &&
                creature.State != CharacterState.Dead);
        }

        /// <summary>One stack of <paramref name="template"/> into another client's bag (WildernessAliaBranchesTests.FillCategory's grant).</summary>
        private static void GrantTo(WildernessRuntimeTestHarness harness, Client client, uint template)
        {
            var stack = EntityClassManager.Instance.GetClassInfo(ItemManager.Instance.GetItemTemplateById(template).Class).ItemClassInfo.StackSize;
            using var grant = new InventoryManager.InventoryGrant();
            using (var unit = harness.CreateChar())
                unit.ExecuteTransaction(() => grant.PlanAndSave(client, new[] { new InventoryManager.InventoryItemGrant(template, stack) }, unit));
            grant.Publish(client);
        }

        private static void FreeFrom(WildernessRuntimeTestHarness harness, Client client, uint template)
        {
            var item = client.Player.Inventory.PersonalInventory.Where(id => id != 0).Select(EntityManager.Instance.GetItem)
                .First(one => one.ItemTemplate.ItemTemplateId == template);
            InventoryManager.Instance.PersonalInventory_DestroyItem(client, new Rasa.Packets.Inventory.Client.PersonalInventory_DestroyItemPacket { EntityId = item.EntityId, Quantity = item.StackSize });
            harness.Drain();
        }

        /// <summary>
        /// An abandon of Petrovich's Run after the cave: the copy and the squad go and the public Petrovich is shown again; taken
        /// again, the copy stands by him again and 1 counts the cave cleared before.
        /// </summary>
        [TestMethod]
        public void PetrovichsRunAbandonedStartsOver()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var copy = IntoOutpost(harness);
            ResetCave(copy);
            var petrovich = Giver(copy, N.Petrovich);
            Accept(harness, petrovich, Run.MissionId);
            Tick(harness, copy, 250);
            KillPools(harness, copy, AshenFixOV1.CavePools.Select(pool => pool.Pool).ToArray());
            for (var tick = 0; tick < 12; tick++)
                Tick(harness, copy, 250);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Run.MissionId, Run.TunnelRun), "on the way");
            Assert.IsTrue(harness.Manager.TryAbandon(harness.Client, Run.MissionId));
            harness.Drain();
            for (var tick = 0; tick < 4; tick++)
                Tick(harness, copy, 250);
            Assert.IsNull(SceneCreature(harness, copy, Run.MissionId, Run.PetrovichRole), "the copy goes with the abandon");
            Assert.IsNull(SceneCreature(harness, copy, Run.MissionId, Run.OfficerRole), "the squad goes with the abandon");
            petrovich = Giver(copy, N.Petrovich);
            Assert.IsTrue(MissionNpcVisibility.IsShownTo(harness.Client, petrovich), "the public Petrovich shown again");
            Accept(harness, petrovich, Run.MissionId);
            Tick(harness, copy, 250);
            Assert.IsNotNull(SceneCreature(harness, copy, Run.MissionId, Run.PetrovichRole), "the copy again");
            Assert.AreEqual((MissionObjectiveState.Completed, MissionObjectiveState.Inactive),
                (Objective(harness, Run.MissionId, Run.ClearTheCave), Objective(harness, Run.MissionId, Run.TunnelRun)), "the cave cleared before counts at the accept");
            for (var tick = 0; tick < 12; tick++)
                Tick(harness, copy, 250);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Run.MissionId, Run.TunnelRun), "then 2 opens and Petrovich leads");
        }

        /// <summary>
        /// Ashen_avernus_outpost takes out 1473, 1533 and 1535, the set's six creatures (creature, stats, look, packages) with their
        /// four pools, and the pads 915/916 with their links; the rows they copy (Harris, Ida Hayner) stay; it updates nothing. Every
        /// table the migration writes is counted (set 17's ids and missions only). Nothing else changes: the other pools, pads,
        /// missions and markers as a database migrated only up to Ashen_avernus_outpost holds them. Up again, all of it is back; the
        /// NPC SQL run again changes nothing.
        /// </summary>
        [TestMethod]
        public void AvernusOutpostRollsBackWithItsMigration()
        {
            using var harness = WildernessRuntimeTestHarness.Create(targetWorldMigration: AshenMissionTests.AvernusOutpost);
            var world = harness.World;
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking()
                .Any(entry => entry.MissionId == mission && entry.ContentRevision == AshenMissionDataV1.Revision);
            int Count(string sql) => world.Database.SqlQueryRaw<int>(sql).AsEnumerable().Single();
            const string ids = "(mission_id IN (1473, 1533, 1535) AND content_revision = 'ashen_1_16_5')";
            var npcs = string.Join(", ", N.Creatures.Concat(N.InvaderPools.Select(pool => pool.Pool)));
            var tables = new[]
            {
                "mission_content_definition", "mission_objective_definition", "mission_objective_transition", "mission_trigger", "mission_action",
                "mission_prerequisite", "mission_reward_definition", "mission_reward_item", "mission_indicator", "mission_scene_binding", "mission_evidence",
                "mission_scenario", "mission_scenario_step", "mission_channel_policy", "mission_repeat_policy", "mission_spawn", "mission_spawn_group", "mission_area"
            };
            string All() => string.Join(", ", tables.Select(table => $"{table} {Count($"SELECT COUNT(*) AS Value FROM {table} WHERE {ids}")}")
                .Concat(new[] { "creature", "creature_stat", "creature_appearance", "npc_package", "spawnpool" }
                    .Select(table => $"{table} {Count($"SELECT COUNT(*) AS Value FROM {table} WHERE id IN ({npcs})")}"))
                .Append($"teleporter {Count($"SELECT COUNT(*) AS Value FROM teleporter WHERE id IN ({N.CavernPad}, {N.GraveyardPad})")}")
                .Append($"local_teleporter_link {Count($"SELECT COUNT(*) AS Value FROM local_teleporter_link WHERE from_teleporter_id IN ({N.CavernPad}, {N.GraveyardPad})")}")
                .Append($"kept {Count($"SELECT COUNT(*) AS Value FROM creature WHERE id IN ({N.Harris}, {N.IdaHayner})")}"));
            static string Others(DbContext db) => string.Join("; ", db.Set<SpawnPoolEntry>().AsNoTracking()
                    .Where(entry => (entry.MapContextId == AshenMissionDataV1.MapContextId || entry.MapContextId == N.Outpost) &&
                        (entry.Id < 790640 || entry.Id > 790679)).OrderBy(entry => entry.Id).AsEnumerable()
                    .Select(entry => FormattableString.Invariant($"{entry.Id}:{entry.PosX:R},{entry.PosY:R},{entry.PosZ:R},{entry.Radius:R},{entry.Creature1Id},{entry.Creature1MaxCount}")))
                + " | " + string.Join("; ", db.Set<TeleporterEntry>().AsNoTracking().Where(entry => entry.Id != N.CavernPad && entry.Id != N.GraveyardPad)
                    .OrderBy(entry => entry.Id).AsEnumerable().Select(entry => FormattableString.Invariant($"{entry.Id}:{entry.MapContextId},{entry.PosX:R},{entry.PosZ:R}")))
                + " | " + string.Join("; ", db.Set<MissionContentDefinitionEntry>().AsNoTracking()
                    .Where(entry => entry.MissionId != 1473 && entry.MissionId != 1533 && entry.MissionId != 1535)
                    .OrderBy(entry => entry.MissionId).ThenBy(entry => entry.ContentRevision)
                    .AsEnumerable().Select(entry => $"{entry.MissionId}/{entry.ContentRevision}"))
                + " | " + string.Join("; ", db.Set<MissionIndicatorEntry>().AsNoTracking()
                    .Where(entry => (entry.ContentRevision == AshenMissionDataV1.Revision || entry.ContentRevision == CrucibleMissionDataV1.Revision)
                        && entry.MissionId != 1473 && entry.MissionId != 1533 && entry.MissionId != 1535)
                    .OrderBy(entry => entry.MissionId).ThenBy(entry => entry.ObjectiveId).ThenBy(entry => entry.IndicatorId).AsEnumerable()
                    .Select(entry => FormattableString.Invariant($"{entry.MissionId}.{entry.ObjectiveId}/{entry.IndicatorId}:{entry.PosX:R},{entry.PosZ:R}")));
            string others;
            using (var atOutpost = WildernessRuntimeTestHarness.Create(targetWorldMigration: AshenMissionTests.AvernusOutpost))
                others = Others(atOutpost.World);
            // 1473: 7 objectives (7 hidden), 7 transitions, 10 triggers (1, 2, 3, 5, 6, 7 one each; 4 four), actions: the sample
            // and the reward (1), the diary, reveal and activate (3), the card (7); scenarios 1-7 (4 four steps, the others one); no
            // area. 1533: one objective, transition, area trigger and reward action; one area. 1535: 10 objectives (6-10 hidden; 9
            // an aggregate), 9 transitions, triggers: four pools (1), the two areas (2, 5), the talk (3), 7's state (4), two counts
            // each (6, 7), the ready event (8), 9's state (10); actions: reveal, activate and a scenario (10, 4), reveal, activate, a scenario, reveal and activate 6 (3), reveal and
            // activate (2), a scenario, reveal and activate 7 (6), a scenario and the reward (5); scenarios 1-8 (6 and 7 two steps each, 8 one); two areas. Each one reward (four items), three evidence rows, no
            // prerequisite; markers 8, 0, 8; three scenes. Six creatures with stats and Harris' or Hayner's look rows, four packages
            // and four pools; the Avernus Invader (stats, no look row: the Atta Soldier has none) and its three pools; two pads and two links.
            const string up = "mission_content_definition 3, mission_objective_definition 18, mission_objective_transition 17, mission_trigger 25, " +
                "mission_action 25, mission_prerequisite 0, mission_reward_definition 3, mission_reward_item 12, mission_indicator 16, " +
                "mission_scene_binding 3, mission_evidence 9, mission_scenario 15, mission_scenario_step 15, mission_channel_policy 0, " +
                "mission_repeat_policy 0, mission_spawn 0, mission_spawn_group 0, mission_area 3, creature 7, creature_stat 7, " +
                "creature_appearance LOOK, npc_package 4, spawnpool 7, teleporter 2, local_teleporter_link 2, kept 2";
            const string down = "mission_content_definition 0, mission_objective_definition 0, mission_objective_transition 0, mission_trigger 0, " +
                "mission_action 0, mission_prerequisite 0, mission_reward_definition 0, mission_reward_item 0, mission_indicator 0, " +
                "mission_scene_binding 0, mission_evidence 0, mission_scenario 0, mission_scenario_step 0, mission_channel_policy 0, " +
                "mission_repeat_policy 0, mission_spawn 0, mission_spawn_group 0, mission_area 0, creature 0, creature_stat 0, " +
                "creature_appearance 0, npc_package 0, spawnpool 0, teleporter 0, local_teleporter_link 0, kept 2";
            var look = Count($"SELECT COUNT(*) AS Value FROM creature_appearance WHERE id = {N.Harris}") * 5 + Count($"SELECT COUNT(*) AS Value FROM creature_appearance WHERE id = {N.IdaHayner}");
            var upNow = up.Replace("LOOK", look.ToString(System.Globalization.CultureInfo.InvariantCulture));
            void AllUp(string when)
            {
                Assert.IsTrue(Missions.All(Has), $"{when}: the three missions");
                var all = All();
                Assert.AreEqual(upNow, all, $"{when}: {all}");
                Assert.AreEqual(others, Others(world), $"{when}: nothing else changes");
            }

            AllUp("at its migration");
            world.GetService<IMigrator>().Migrate(AshenMissionTests.Previous(world, AshenMissionTests.AvernusOutpost));
            world.ChangeTracker.Clear();
            Assert.IsFalse(Missions.Any(Has));
            Assert.AreEqual(down, All());
            Assert.AreEqual(others, Others(world), "Down: nothing else changes");
            Assert.IsTrue(AshenMissionTests.MissionIds.Where(id => !Missions.Contains(id) && !AshenAvernusHiveTests.Missions.Contains(id) &&
                !AshenConscriptionEntryTests.Missions.Contains(id) && !AshenConscriptionQueenTests.Missions.Contains(id)).All(Has), "sets 1-16 stay");
            foreach (var sql in N.PadsDown())
                world.Database.ExecuteSqlRaw(sql);
            foreach (var sql in N.Down())
                world.Database.ExecuteSqlRaw(CrucibleSql.Guard(sql));
            Assert.AreEqual(down, All(), "a second Down changes nothing");

            world.GetService<IMigrator>().Migrate(AshenMissionTests.AvernusOutpost);
            world.ChangeTracker.Clear();
            AllUp("Up again");
            foreach (var sql in N.Up())
                world.Database.ExecuteSqlRaw(CrucibleSql.Guard(sql));
            foreach (var sql in N.PadsUp())
                world.Database.ExecuteSqlRaw(sql);
            AllUp("the NPC and pad rows run again");
        }

        /// <summary>
        /// The squad's copy of Avernus Outpost, entered through its door from Ashen Desert (map_link 59): its pools' dead and cleared
        /// marks of an earlier test gone (a squad copy outlives a test), and an earlier test's scene objects too.
        /// </summary>
        internal static MapChannel IntoOutpost(WildernessRuntimeTestHarness harness)
        {
            if (!harness.Maps.MapChannelArray.ContainsKey(AshenMissionDataV1.MapContextId))
                PlateauInstanceMaps.LoadMap(harness, AshenMissionDataV1.MapContextId);
            if (!harness.Maps.MapChannelArray.ContainsKey(Map))
                PlateauInstanceMaps.LoadMap(harness, Map);
            var copy = PlateauInstanceMaps.ThroughTheDoor(harness, Map);
            foreach (var pool in copy.SpawnPools.Where(pool => Pools.Contains(pool.DbId)))
                pool.ClearedAtUtcMs = 0;
            // A squad copy outlives a test: an earlier test's Petrovich copy (the same character id, a run this test's database
            // does not hold) would still hide the public Petrovich (MissionNpcVisibility, ReplaceWhilePresent). Its pool goes.
            var runs = Missions.SelectMany(mission => Runs(harness, mission)).ToHashSet();
            copy.SpawnPools.RemoveAll(pool => pool.SceneRunId != null && Missions.Contains(pool.ScenarioMissionId) && !runs.Contains(pool.SceneRunId));
            foreach (var creature in Here(copy).Where(one => Pools.Contains(one.SpawnPool?.DbId ?? 0)).ToArray())
                if (creature.State == CharacterState.Dead)
                    Respawn(copy, creature.SpawnPool.DbId);
            harness.Manager.Scenes.Resume(harness.Client);
            harness.Drain();
            return copy;
        }

        /// <summary>The hostile spawn pools of Avernus Outpost (as AshenIndraFacilityTests.HostilePools on Indra Caverns).</summary>
        internal static List<SpawnPoolEntry> HostilePools(WildernessRuntimeTestHarness harness) =>
            harness.World.SpawnPoolEntries.AsNoTracking().Where(row => row.MapContextId == Map && row.Mode == 0).AsEnumerable()
                .Where(row => new[] { row.Creature1Id, row.Creature2Id, row.Creature3Id, row.Creature4Id, row.Creature5Id, row.Creature6Id }
                    .Any(id => id != 0 && CreatureManager.Instance.LoadedCreatures.TryGetValue(id, out var creature) && creature.TargetCategory == TargetCategory.Hostile))
                .ToList();

        /// <summary>The set's places: label, place, whether it is seen from the ring, the height looked at, its clearance.</summary>
        internal static List<(string Label, Vector3 At, bool Ring, float Middle, float Clearance)> Places()
        {
            var places = new List<(string, Vector3, bool, float, float)>
            {
                ("Captain Karter", V(N.KarterAt), true, F.NpcMiddle, Clearance), ("Commander Drazen", V(N.DrazenAt), true, F.NpcMiddle, Clearance),
                ("Petrovich in the graveyard", V(M.PetrovichAt), true, F.NpcMiddle, Clearance), ("Petrovich at the top", V(N.PetrovichTopAt), true, F.NpcMiddle, Clearance),
                ("Drazen's pad", V(M.CavernPadAt), false, 0, CavernPadClearance), ("the graveyard's pad", V(M.GraveyardPadAt), false, 0, Clearance),
                ("the courier bag", ToVector(Overly.BagAt), true, F.ObjectMiddle, Clearance), ("the prototype ammo", ToVector(Overly.AmmoAt), true, F.ObjectMiddle, Clearance),
                ("the tavern corpse", ToVector(Overly.DiaryAt), true, F.ObjectMiddle, Clearance), ("the data backup", ToVector(Overly.BackupAt), true, F.ObjectMiddle, Clearance),
                ("the strip corpse", ToVector(Overly.CardAt), true, F.ObjectMiddle, Clearance), ("the prison teleporter", ToVector(Overly.TeleporterAt), true, F.ObjectMiddle, Clearance),
                ("the secret stash", ToVector(Overly.StashAt), true, F.ObjectMiddle, Clearance), ("the prison arrival", ToVector(Overly.PrisonArrival), false, 0, Clearance),
                ("the prison's way out (fix L)", V(AshenFixLV1.PrisonPadAt), false, 0, Clearance), ("the way out's arrival (fix L)", V(AshenFixLV1.OutsidePadAt), false, 0, Clearance),
                ("the officer", ToVector(M.OfficerAt), false, 0, Clearance), ("soldier 1", ToVector(M.SoldiersAt[0]), false, 0, Clearance),
                ("soldier 2", ToVector(M.SoldiersAt[1]), false, 0, Clearance), ("the turn", ToVector(Run.RestAt), true, F.NpcMiddle, Clearance)
            };
            for (var index = 0; index < Overly.ServersAt.Length; index++)
                places.Add(($"server {index + 1}", ToVector(Overly.ServersAt[index]), true, F.ObjectMiddle, Clearance));
            for (var member = 0; member < Run.AmbushAt.Length; member++)
                places.Add(($"ambush {member + 1}", ToVector(M.AmbushAt[member]), false, 0, Clearance));
            places.Add(("the tunnel's top", ToVector(Run.TopWay[^1]), false, 0, Clearance));
            return places.Select(place => ((string Label, Vector3 At, bool Ring, float Middle, float Clearance))place).ToList();
        }

        /// <summary>
        /// The rooms the real client keeps closed where the client collision the server reads (MapGeometry) has a way in: label, the
        /// walkable box measured in the client (x, z), its floor's height. The prison (to-fix row 46, AshenFixLV1.PrisonBox): its
        /// east stair, corridor and ramp are open in arch_hum_fort_prison_v02.geo, and ClientFloorLine walks them; the real client
        /// stops at the stair's foot. ClientFloorLine cannot see this wall, so the walk checks take the measured box as one.
        /// </summary>
        internal static readonly (string Label, (double MinX, double MaxX, double MinZ, double MaxZ, double Y) Box)[] ClientClosedRooms =
        {
            ("the prison", AshenFixLV1.PrisonBox)
        };

        /// <summary>How far over or under a closed room's floor a point still counts as in the room (a floor above or below is another).</summary>
        private const float ClosedRoomReach = 3;

        /// <summary>
        /// Where <paramref name="line"/> (corners, sampled every 0.25 m) goes into or out of a room of ClientClosedRooms, or null when it
        /// stays inside one or outside all.
        /// </summary>
        internal static string ClosedRoomCrossing(IReadOnlyList<Vector3> line)
        {
            foreach (var (label, box) in ClientClosedRooms)
            {
                bool In(Vector3 at) => at.X >= box.MinX && at.X <= box.MaxX && at.Z >= box.MinZ && at.Z <= box.MaxZ && MathF.Abs(at.Y - (float)box.Y) <= ClosedRoomReach;
                var was = In(line[0]);
                for (var k = 1; k < line.Count; k++)
                {
                    var steps = Math.Max(1, (int)MathF.Ceiling(Vector3.Distance(line[k - 1], line[k]) / 0.25f));
                    for (var s = 1; s <= steps; s++)
                    {
                        var at = Vector3.Lerp(line[k - 1], line[k], s / (float)steps);
                        if (In(at) == was)
                            continue;
                        return FormattableString.Invariant($"goes {(was ? "out of" : "into")} {label} at ({at.X:0.0}, {at.Y:0.00}, {at.Z:0.0}), a room the real client keeps closed");
                    }
                }
            }
            return null;
        }

        /// <summary>The start shifts (m along a leg's first stretch) each leg is also walked at (ThePlacesAreWalkedToOnTheClientsFloor).</summary>
        private static readonly float[] StartShifts = { 0, 0.13f, 0.27f, 0.4f };

        /// <summary>How far outside every hostile pool's circle a place stands (2D; 30 m walk clearance and a margin).</summary>
        private const float Clearance = 34;

        /// <summary>Fix M's pad 915 outside 531061's circle (26.8 m in 3D; a rule break of the 34 m place rule, docs/ashen-playtest.md).</summary>
        private const float CavernPadClearance = 26.5f;

        /// <summary>The upstream pools a walk passes (rule breaks, docs/ashen-playtest.md): 531047 in the tunnel from the outpost to Drazen's cavern and 531061 by the ramp down into it (21.8 m outside its circle: the ramp is the only way down); 531055 south of pad 915 (24.7 m outside
        /// its circle, 38.5 m from its draws: the way round the pad's walls, fix M follow-up); 531042 by the hive tunnel's top half.</summary>
        private static readonly uint[] DrazenTunnelPools = { 531047, 531061, 531055 };
        private static readonly uint[] HiveTunnelPools = { 531042 };

        /// <summary>
        /// The legs of the set's walks on the client's floor, each walked there and back on its own (corners: a client-floor
        /// flood's way, 2026-11-12, the climb rule's, ESTIMATE of a player's line). A walk is a run of legs.
        /// </summary>
        internal static readonly Dictionary<string, Vector3[]> Legs = new()
        {
            ["to Karter"] = new Vector3[]
            {
                new(83.99f, 528.5f, 107.67f), new(80.8f, 528.5f, 99.5f)
            },
            // Audit S-B: east of the 1.2 m block at (95-99, 73-74) on the client floor, not over it.
            ["the entrance"] = new Vector3[]
            {
                new(83.99f, 528.5f, 107.67f), new(89f, 528.5f, 89f), new(95f, 528.5f, 85f), new(101f, 528.5f, 76f),
                new(101f, 528.5f, 71f)
            },
            ["to the armour shop"] = new Vector3[]
            {
                new(101f, 528.5f, 71f), new(99f, 528.5f, 66f),
                new(99f, 527.12f, 57f), new(101f, 527.12f, 38f), new(91f, 527.12f, 28f), new(84f, 527.12f, 23f),
                new(77f, 526.23f, 3f), new(76.65f, 525.96f, 3.35f), new(75.65f, 525.71f, 2.35f), new(76f, 525.37f, 1f),
                new(75f, 524.37f, -3f), new(75f, 523.12f, -8f), new(75f, 514.37f, -47f), new(90f, 514.12f, -50f),
                new(90f, 514.12f, -68f), new(95f, 514.12f, -68f), new(102.9f, 514.25f, -56.7f)
            },
            // Audit S-B: through the middle of the weapon shop's door (x 135-137), off its posts.
            ["to the weapon shop"] = new Vector3[]
            {
                new(101f, 528.5f, 71f), new(99f, 528.5f, 66f), new(99f, 527.12f, 57f), new(101f, 527.12f, 38f),
                new(91f, 527.12f, 28f), new(84f, 527.12f, 23f), new(77f, 526.23f, 3f), new(76.65f, 525.96f, 3.35f),
                new(75.65f, 525.71f, 2.35f), new(76f, 525.37f, 1f), new(75f, 524.37f, -3f), new(75f, 523.12f, -8f),
                new(75f, 514.37f, -47f), new(90f, 514.12f, -50f), new(90f, 514.12f, -68f),
                new(131f, 514.12f, -68f), new(136f, 514.37f, -66f), new(136f, 514.56f, -62f), new(136f, 514.29f, -56f)
            },
            ["to the ammo"] = new Vector3[]
            {
                new(136f, 514.29f, -56f), new(141.4f, 514.57f, -49.1f)
            },
            ["to the tavern"] = new Vector3[]
            {
                new(101f, 528.5f, 71f), new(99f, 528.5f, 66f),
                new(99f, 527.12f, 57f), new(101f, 527.12f, 38f), new(91f, 527.12f, 28f), new(84f, 527.12f, 23f),
                new(77f, 526.23f, 3f), new(76.65f, 525.96f, 3.35f), new(75.65f, 525.71f, 2.35f), new(76f, 525.37f, 1f),
                new(75f, 524.37f, -3f), new(75f, 523.12f, -8f), new(75f, 514.37f, -47f), new(90f, 514.12f, -50f),
                new(90f, 514.12f, -68f), new(189f, 517.87f, -68f), new(192f, 519.12f, -65f),
                new(197f, 518.89f, -43f), new(194.7f, 519.01f, -31f)
            },
            ["to the plaza"] = new Vector3[]
            {
                new(101f, 528.5f, 71f), new(99f, 528.5f, 66f),
                new(99f, 527.12f, 57f), new(101f, 527.12f, 38f), new(91f, 527.12f, 28f), new(84f, 527.12f, 23f),
                new(77f, 526.23f, 3f), new(76.65f, 525.96f, 3.35f), new(75.65f, 525.71f, 2.35f), new(76f, 525.37f, 1f),
                new(75f, 524.37f, -3f), new(75f, 523.12f, -8f), new(75f, 514.37f, -72f), new(88f, 514.12f, -85f),
                // Row 45, the real client's walked way up the ramp (93.6, -105.2) -> (102, -110.4) (2026-11-13), not fix M's east stair.
                new(92.8f, 516.07f, -104.4f), new(93.6f, 516.97f, -105.2f), new(102f, 525.37f, -110.4f), new(103.6f, 527.12f, -112.4f),
                new(103f, 528f, -119f)
            },
            ["across to the landing strip"] = new Vector3[]
            {
                new(103f, 528f, -119f), new(98f, 528f, -120f), new(53f, 528f, -120f), new(31f, 528.12f, -120f)
            },
            ["across the plaza to the HQ"] = new Vector3[]
            {
                new(103f, 528f, -119f), new(104f, 528.1f, -123f), new(125f, 528.1f, -123f), new(130f, 528.1f, -108f),
                new(145f, 528.1f, -107f), new(149f, 528.5f, -111f)
            },
            // Audit S-B: up the HQ's door ramp along its length (east, 22 degrees), not off its side.
            ["into the HQ"] = new Vector3[]
            {
                new(149f, 528.5f, -111f), new(158f, 529.66f, -121f), new(169f, 530.99f, -121f), new(175f, 532.66f, -121f),
                new(175f, 532.66f, -123f)
            },
            ["to server 1"] = new Vector3[]
            {
                new(175f, 532.66f, -123f), new(176.4f, 532.66f, -126.8f)
            },
            ["to server 2"] = new Vector3[]
            {
                new(175f, 532.66f, -123f), new(174f, 532.66f, -127f), new(176.4f, 532.66f, -128.8f)
            },
            ["to server 3"] = new Vector3[]
            {
                new(175f, 532.66f, -123f), new(174f, 532.66f, -129f), new(176.4f, 532.66f, -130.8f)
            },
            ["to server 4"] = new Vector3[]
            {
                new(175f, 532.66f, -123f), new(174f, 532.66f, -131f), new(176.4f, 532.66f, -132.8f)
            },
            ["to the black box"] = new Vector3[]
            {
                new(175f, 532.66f, -123f), new(174f, 532.66f, -133f), new(175.8f, 532.66f, -135.1f)
            },
            // Audit S-B: onto the strip's platform by its south ramp (19 degrees), not up its 52-degree side.
            ["to the strip corpse"] = new Vector3[]
            {
                new(31f, 528.12f, -120f), new(14f, 529.87f, -129.5f), new(8f, 531.16f, -129.5f), new(8.9f, 531.16f, -124.9f)
            },
            ["to the prison teleporter"] = new Vector3[]
            {
                new(101f, 528.5f, 71f), new(99f, 528.5f, 66f),
                new(99f, 527.12f, 57f), new(101f, 527.12f, 38f), new(91f, 527.12f, 28f), new(84f, 527.12f, 23f),
                new(77f, 526.23f, 3f), new(76.65f, 525.96f, 3.35f), new(75.65f, 525.71f, 2.35f), new(76f, 525.37f, 1f),
                new(75f, 524.37f, -3f), new(75f, 523.12f, -8f), new(75f, 514.37f, -72f), new(88f, 514.12f, -85f),
                // Row 45, the real client's walked way up the ramp (93.6, -105.2) -> (102, -110.4) (2026-11-13), not fix M's east stair.
                new(92.8f, 516.07f, -104.4f), new(93.6f, 516.97f, -105.2f), new(102f, 525.37f, -110.4f), new(103.6f, 527.12f, -112.4f),
                new(103f, 528f, -119f),
                new(98f, 528f, -120f), new(42f, 528f, -120f), new(28f, 528.12f, -106f)
            },
            // Fix L (to-fix row 46): the prison is closed in the client (ClientClosedRooms), so it is reached by the teleporter
            // only, and left by its way out (923): these legs stay inside it. Set 17's leg in by the east stair, which the server's
            // collision has open, is gone (the real client stops at the stair's foot).
            ["in the prison to the stash"] = new Vector3[]
            {
                new(-8f, 520.01f, -46f), new(-8f, 520.01f, -49f), new(-6.5f, 520.01f, -50.2f)
            },
            // Onto 923 from the east along its local x axis (fix L audit R1): over its low rim (0.18 m, 24 degrees), not its north
            // side's 0.59 m step; to its centre, 2 m inside its trigger's edge (AshenFixLTests.ThePrisonPadIsWalkedOntoOverItsLowRim).
            ["in the prison to the way out"] = new Vector3[]
            {
                new(-8f, 520.01f, -46f), new(-11f, 520.01f, -44f), new(-14f, 520.01f, -44f)
            },
            // From the way out's arrival (924) by the inbound teleporter onto the strip road and to the plaza (set 17's way back).
            ["from the way out's arrival"] = new Vector3[]
            {
                new(31f, 528.12f, -109f), new(42f, 528f, -120f), new(98f, 528f, -120f), new(103f, 528f, -119f)
            },
            ["to the south road"] = new Vector3[]
            {
                new(101f, 528.5f, 71f), new(99f, 528.5f, 66f),
                new(99f, 527.12f, 57f), new(101f, 527.12f, 38f), new(91f, 527.12f, 28f), new(84f, 527.12f, 23f),
                new(77f, 526.23f, 3f), new(76.65f, 525.96f, 3.35f), new(75.65f, 525.71f, 2.35f), new(76f, 525.37f, 1f),
                new(75f, 524.37f, -3f), new(75f, 523.12f, -8f), new(75f, 514.12f, -100f), new(78f, 514.12f, -103f)
            },
            // Audit S-B: east of the open frame over the south road (its 2 m end panel at z -129.5 closes the road from x 55.7 to
            // 80.3; the client map's statics), then down the 14-19 degree slope east of the fallen slab, not up the slab and its
            // steps from the side.
            ["down the south road"] = new Vector3[]
            {
                new(78f, 514.12f, -103f), new(84f, 513.37f, -103f), new(84f, 513.12f, -131f), new(84.5f, 511.87f, -139f),
                new(82f, 511.37f, -142f), new(77.5f, 511.49f, -143f)
            },
            // Audit S-B: under the terrain's lip where the cave mesh opens (57-59, -170), and west of the rubble at (36-42, -241).
            ["down the cavern tunnel"] = new Vector3[]
            {
                new(77.5f, 511.49f, -143f), new(77f, 511.12f, -144f), new(62f, 508.4f, -166f), new(58.5f, 508.6f, -170f),
                new(57.5f, 508.8f, -171.5f), new(55f, 500.47f, -212f), new(42f, 496.53f, -229f), new(33f, 492.1f, -236f),
                new(32f, 492f, -246f), new(40f, 494.55f, -262f), new(37f, 493.94f, -271f)
            },
            // Fix M follow-up: round pad 915's walls (its horseshoe, AshenFixMTests.AddPads) by their west side and the ramp's
            // south, not through (47, -311) inside them (the real client stopped at (44.7, -310.2), 2026-11-13).
            ["into the cavern"] = new Vector3[]
            {
                new(37f, 493.94f, -271f), new(34f, 492.11f, -290f), new(43f, 479.55f, -308f), new(42f, 476.6f, -316f),
                new(42f, 476.9f, -324f), new(54f, 479.1f, -326f), new(57.5f, 478.62f, -315f)
            },
            ["to Drazen"] = new Vector3[]
            {
                new(57.5f, 478.62f, -315f), new(57.5f, 477.97f, -313f)
            },
            // Fix M (row 48): from Petrovich's place on the graveyard's north-west shelf, south-west round the stone at (860.6,
            // -223.7), onto set 17's line at (850, -245.4).
            ["the graveyard to the tunnel"] = new Vector3[]
            {
                new(873f, 284.11f, -219f), new(862f, 285.36f, -236f), new(850f, 286f, -245.44f), new(804f, 288.71f, -245f),
                new(793f, 297.02f, -233f), new(790f, 296.52f, -231f)
            },
            ["up the tunnel to the turn"] = new Vector3[]
            {
                new(790f, 296.52f, -231f), new(789f, 296.41f, -229f), new(777f, 316.59f, -114f), new(775f, 317.37f, -112.5f)
            },
            // Audit S-B: south of the 38-degree bump at (704-716, -250).
            ["the turn to the top"] = new Vector3[]
            {
                new(775f, 317.37f, -112.5f), new(771f, 319.04f, -108f), new(758f, 320.1f, -111f), new(754f, 321.26f, -115f),
                new(746f, 330.12f, -128f), new(745f, 329.91f, -141f), new(740f, 328.93f, -147f), new(733f, 338.19f, -198f),
                new(735f, 343.9f, -234f), new(719f, 352.39f, -249f), new(714f, 352.46f, -254.5f), new(693f, 353.63f, -253f),
                new(678f, 353.25f, -246f), new(648f, 352.99f, -245f), new(645.5f, 352.67f, -246.5f)
            },
            ["from Drazen"] = new Vector3[]
            {
                new(57.5f, 477.97f, -313f), new(57.5f, 478.62f, -315f)
            },
            // Fix M: pad 915 on the flat floor south-west of Drazen, walked onto up its ramp from the south (the client lets no one
            // off its west side, the tester 2026-11-12).
            // Fix M follow-up: south first, clear of pad 915's east wall, then west to the ramp's foot and north up it.
            ["to his pad"] = new Vector3[]
            {
                new(57.5f, 478.62f, -315f), new(56f, 479.62f, -325f), new(48f, 477.49f, -324.5f), new(48f, 477.12f, -318f)
            },
            // Fix M (row 48): down pad 916's ramp to the south, then west to Petrovich.
            ["the graveyard pad to Petrovich"] = new Vector3[]
            {
                new(888f, 281.86f, -212f), new(888f, 281.36f, -218.5f), new(873f, 284.11f, -219f)
            },
        };

        /// <summary>The walks: label, its legs, the pools it passes (rule breaks), its target.</summary>
        internal static List<(string Label, string[] Legs, uint[] Passes, Vector3 Target)> Walks()
        {
            var none = System.Array.Empty<uint>();
            // Fix O: the walk also passes the upstream islands 531049 and 531060 (no longer 1, their creatures cannot step off them).
            var cave = AshenFixOV1.CavePools.Select(pool => pool.Pool).Append(AshenFixOV1.WestIsland).Append(AshenFixOV1.EastIsland).ToArray();
            return new List<(string, string[], uint[], Vector3)>
            {
                ("the entrance to Karter", new[] { "to Karter" }, none, V(N.KarterAt)),
                ("the entrance to the courier bag", new[] { "the entrance", "to the armour shop" }, none, ToVector(Overly.BagAt)),
                ("the entrance to the prototype ammo", new[] { "the entrance", "to the weapon shop", "to the ammo" }, none, ToVector(Overly.AmmoAt)),
                ("the entrance to the tavern corpse", new[] { "the entrance", "to the tavern" }, none, ToVector(Overly.DiaryAt)),
                ("the entrance to server 1", new[] { "the entrance", "to the plaza", "across the plaza to the HQ", "into the HQ", "to server 1" }, none, ToVector(Overly.ServersAt[0])),
                ("the entrance to server 2", new[] { "the entrance", "to the plaza", "across the plaza to the HQ", "into the HQ", "to server 2" }, none, ToVector(Overly.ServersAt[1])),
                ("the entrance to server 3", new[] { "the entrance", "to the plaza", "across the plaza to the HQ", "into the HQ", "to server 3" }, none, ToVector(Overly.ServersAt[2])),
                ("the entrance to server 4", new[] { "the entrance", "to the plaza", "across the plaza to the HQ", "into the HQ", "to server 4" }, none, ToVector(Overly.ServersAt[3])),
                ("the entrance to the data backup", new[] { "the entrance", "to the plaza", "across the plaza to the HQ", "into the HQ", "to the black box" }, none, ToVector(Overly.BackupAt)),
                ("the entrance to the strip corpse", new[] { "the entrance", "to the plaza", "across to the landing strip", "to the strip corpse" }, none, ToVector(Overly.CardAt)),
                ("the entrance to the prison teleporter", new[] { "the entrance", "to the prison teleporter" }, none, ToVector(Overly.TeleporterAt)),
                ("the prison arrival to the secret stash", new[] { "in the prison to the stash" }, none, ToVector(Overly.StashAt)),
                ("the prison arrival to its way out (fix L)", new[] { "in the prison to the way out" }, none, V(AshenFixLV1.PrisonPadAt)),
                ("the way out's arrival to the plaza (fix L)", new[] { "from the way out's arrival" }, none, new Vector3(103f, 528f, -119f)),
                ("the entrance to Drazen", new[] { "the entrance", "to the south road", "down the south road", "down the cavern tunnel", "into the cavern", "to Drazen" }, DrazenTunnelPools, V(N.DrazenAt)),
                ("Drazen to his pad", new[] { "from Drazen", "to his pad" }, new uint[] { 531055, 531061 }, V(M.CavernPadAt)),
                ("the graveyard's pad to Petrovich", new[] { "the graveyard pad to Petrovich" }, none, V(M.PetrovichAt)),
                ("Petrovich to the turn (1535; the cave's pools cleared by 1)", new[] { "the graveyard to the tunnel", "up the tunnel to the turn" }, cave, ToVector(Run.RestAt)),
                ("the turn to the top (1535)", new[] { "the turn to the top" }, HiveTunnelPools, V(N.PetrovichTopAt))
            };
        }

        private static List<Vector3> Line(IEnumerable<string> legs) =>
            legs.SelectMany((leg, index) => index == 0 ? Legs[leg] : Legs[leg].Skip(1)).ToList();

        /// <summary>The cave's pools standing again (an earlier test's kills and cleared marks gone).</summary>
        private static void ResetCave(MapChannel copy)
        {
            var cave = AshenFixOV1.CavePools.Select(pool => pool.Pool).ToArray();
            foreach (var pool in copy.SpawnPools.Where(pool => cave.Contains(pool.DbId)))
                pool.ClearedAtUtcMs = 0;
        }

        /// <summary>The pools standing in the copy (an earlier kill's dead put back), every creature of them killed, the pools' pass after (CrucibleChaukasTests.KillPools).</summary>
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
            Assert.IsGreaterThanOrEqualTo(pools.Length, standing.Length, "the pools stand");
            foreach (var creature in standing)
                Kill(harness, copy, creature);
            SpawnPoolManager.Instance.SpawnPoolWorker(copy, 0);
            harness.Drain();
        }

        /// <summary>The pad rows on the copy (set 17's, fix L's), as InitTeleporters puts them on a loaded map (AshenFixHTests.Pads), and the links.</summary>
        internal static void Pads(WildernessRuntimeTestHarness harness, MapChannel copy)
        {
            var ids = new[] { N.CavernPad, N.GraveyardPad, AshenFixLV1.PrisonPad, AshenFixLV1.OutsidePad };
            foreach (var row in harness.World.TeleporterEntries.AsNoTracking().Where(row => ids.Contains(row.Id)).ToList())
            {
                if (copy.Teleporters.TryGetValue(row.Id, out var standing))
                {
                    // A squad copy outlives a test: its pads (an earlier test's player off them), known to this test's manager.
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

        /// <summary>Through the pad onto the far one (AshenFixHTests.Teleport).</summary>
        internal static void Teleport(WildernessRuntimeTestHarness harness, MapChannel map, DynamicObject pad)
        {
            // Off the pad first: a player set down on it (an earlier test's arrival, the same character) is not taken until he
            // has stepped off it (PlayerTransfer.ArrivalPad).
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

        /// <summary>A use's move on this map (AshenGothaPlantTests.Teleported): a passage to the destination asked for; the arrival.</summary>
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

        /// <summary>A weapon hit on the object (AshenGothaPlantTests.Shoot).</summary>
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

        private static uint Counter(WildernessRuntimeTestHarness harness, uint mission, uint objective) =>
            harness.Client.Player.Missions[mission].Objectives[objective].Counters[0];

        private const uint ConsumableFiller = 45060;       // Class VII Basic Med Pack: fills the consumables bag (as AshenWhiteOasisTests)

        private static Vector3 V((double X, double Y, double Z) at) => new((float)at.X, (float)at.Y, (float)at.Z);
    }
}
